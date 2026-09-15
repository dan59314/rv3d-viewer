using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using Rv3dViewer.Core;

namespace Rv3dViewer.Rendering.OpenGL;

public static class TextureStackComposer
{
    public const int PreviewSize = 512;

    public static Bitmap Compose(TextureStack stack, Func<string, string> resolvePath, int size = PreviewSize)
    {
        ArgumentNullException.ThrowIfNull(stack);
        ArgumentNullException.ThrowIfNull(resolvePath);
        size = Math.Clamp(size, 16, 2048);
        var output = new float[size * size * 4];
        var images = new Dictionary<string, ImagePixels>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var layer in stack.Layers.Where(item => item.Enabled).Take(8))
            {
                layer.Validate();
                ImagePixels? source = null;
                ImagePixels? mask = null;
                if (layer.Kind == TextureLayerKind.Image && !string.IsNullOrWhiteSpace(layer.Path))
                    source = Load(resolvePath(layer.Path), images);
                if (layer.Mask is { Enabled: true } && !string.IsNullOrWhiteSpace(layer.Mask.Path))
                    mask = Load(resolvePath(layer.Mask.Path), images);

                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    var transformed = TransformUv(uv, layer.Transform);
                    var color = SampleLayer(layer, source, transformed);
                    color = AdjustChannels(color, layer.Channels);
                    color = ApplySemantic(color, layer, stack.Semantic);
                    var maskValue = 1f;
                    if (layer.Mask.Enabled)
                    {
                        var maskUv = layer.Mask.FollowLayerTransform
                            ? transformed
                            : TransformUv(uv, layer.Mask.Transform);
                        maskValue = mask is null ? 1f : ChannelValue(mask.Sample(maskUv, layer.Sampling), layer.Mask.Channel);
                        if (layer.Mask.Invert) maskValue = 1f - maskValue;
                        maskValue = Math.Clamp(maskValue * layer.Mask.Strength, 0f, 1f);
                    }

                    var index = (y * size + x) * 4;
                    var destination = new Vector4(output[index], output[index + 1], output[index + 2], output[index + 3]);
                    var alpha = Math.Clamp(color.W * layer.Opacity * maskValue, 0f, 1f);
                    var blended = Blend(destination, color, alpha, layer.BlendMode);
                    output[index] = blended.X; output[index + 1] = blended.Y;
                    output[index + 2] = blended.Z; output[index + 3] = blended.W;
                }
            }

            return ToBitmap(output, size);
        }
        finally
        {
            foreach (var image in images.Values) image.Dispose();
        }
    }

    private static ImagePixels? Load(string path, IDictionary<string, ImagePixels> cache)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        if (cache.TryGetValue(path, out var cached)) return cached;
        try { return cache[path] = new ImagePixels(path); }
        catch { return null; }
    }

    private static Vector2 TransformUv(Vector2 uv, TextureTransformSettings transform)
    {
        if (transform.SwapUv) uv = new Vector2(uv.Y, uv.X);
        if (transform.FlipX) uv.X = 1f - uv.X;
        if (transform.FlipY) uv.Y = 1f - uv.Y;
        var pivot = new Vector2(transform.PivotX, transform.PivotY);
        uv = (uv - pivot) * new Vector2(transform.ScaleX, transform.ScaleY);
        var radians = transform.RotationDegrees * MathF.PI / 180f;
        var cosine = MathF.Cos(radians); var sine = MathF.Sin(radians);
        uv = new Vector2(uv.X * cosine - uv.Y * sine, uv.X * sine + uv.Y * cosine);
        return uv + pivot + new Vector2(transform.OffsetX, transform.OffsetY);
    }

    private static Vector4 SampleLayer(TextureLayer layer, ImagePixels? image, Vector2 uv) => layer.Kind switch
    {
        TextureLayerKind.SolidColor => layer.Color,
        TextureLayerKind.Checker => (((int)MathF.Floor(uv.X * 8) + (int)MathF.Floor(uv.Y * 8)) & 1) == 0
            ? layer.Color : new Vector4(Vector3.One - new Vector3(layer.Color.X, layer.Color.Y, layer.Color.Z), layer.Color.W),
        TextureLayerKind.Gradient => Vector4.Lerp(new Vector4(0f, 0f, 0f, layer.Color.W), layer.Color, Wrap01(uv.Y)),
        TextureLayerKind.Noise => new Vector4(Vector3.One * Noise(uv), layer.Color.W) * layer.Color,
        _ => image?.Sample(uv, layer.Sampling) ?? new Vector4(1f, 0f, 1f, 1f)
    };

    private static Vector4 AdjustChannels(Vector4 value, TextureChannelSettings settings)
    {
        if (settings.Source != TextureChannel.Rgba)
        {
            var scalar = ChannelValue(value, settings.Source);
            value = new Vector4(scalar, scalar, scalar, settings.Source == TextureChannel.Alpha ? scalar : value.W);
        }
        if (settings.Invert) value = new Vector4(1f - value.X, 1f - value.Y, 1f - value.Z, value.W);
        var rgb = new Vector3(value.X, value.Y, value.Z);
        var luminance = Vector3.Dot(rgb, new Vector3(0.2126f, 0.7152f, 0.0722f));
        rgb = Vector3.Lerp(new Vector3(luminance), rgb, settings.Saturation);
        if (Math.Abs(settings.HueDegrees) > 0.001f)
        {
            var angle = settings.HueDegrees * MathF.PI / 180f;
            var axis = Vector3.Normalize(Vector3.One);
            rgb = rgb * MathF.Cos(angle) + Vector3.Cross(axis, rgb) * MathF.Sin(angle) +
                  axis * Vector3.Dot(axis, rgb) * (1f - MathF.Cos(angle));
        }
        rgb = (rgb - new Vector3(0.5f)) * settings.Contrast + new Vector3(0.5f + settings.Brightness);
        rgb = new Vector3(MathF.Pow(Math.Clamp(rgb.X, 0f, 1f), 1f / settings.Gamma),
            MathF.Pow(Math.Clamp(rgb.Y, 0f, 1f), 1f / settings.Gamma),
            MathF.Pow(Math.Clamp(rgb.Z, 0f, 1f), 1f / settings.Gamma));
        rgb *= new Vector3(settings.Tint.X, settings.Tint.Y, settings.Tint.Z);
        return new Vector4(Vector3.Clamp(rgb, Vector3.Zero, Vector3.One), Math.Clamp(value.W * settings.Tint.W, 0f, 1f));
    }

    private static Vector4 ApplySemantic(Vector4 value, TextureLayer layer, TextureSemantic semantic)
    {
        if (semantic == TextureSemantic.Normal)
        {
            var x = value.X * 2f - 1f; var y = value.Y * 2f - 1f; var z = value.Z * 2f - 1f;
            if (layer.Normal.Convention == NormalMapConvention.DirectX) y = -y;
            if (layer.Normal.InvertX) x = -x;
            if (layer.Normal.InvertY) y = -y;
            x *= layer.Normal.Strength; y *= layer.Normal.Strength;
            var normal = Vector3.Normalize(new Vector3(x, y, Math.Max(z, 0.0001f)));
            return new Vector4(normal * 0.5f + new Vector3(0.5f), value.W);
        }
        if (semantic == TextureSemantic.Bump)
        {
            var height = ChannelValue(value, layer.Bump.Source);
            if (layer.Bump.Invert) height = 1f - height;
            height = Math.Clamp((height - layer.Bump.Bias) * layer.Bump.Strength + layer.Bump.Bias, 0f, 1f);
            return new Vector4(height, height, height, value.W);
        }
        return value;
    }

    private static float ChannelValue(Vector4 value, TextureChannel channel) => channel switch
    {
        TextureChannel.Red => value.X, TextureChannel.Green => value.Y, TextureChannel.Blue => value.Z,
        TextureChannel.Alpha => value.W,
        TextureChannel.Luminance => Vector3.Dot(new Vector3(value.X, value.Y, value.Z), new Vector3(0.2126f, 0.7152f, 0.0722f)),
        _ => Vector3.Dot(new Vector3(value.X, value.Y, value.Z), new Vector3(0.2126f, 0.7152f, 0.0722f))
    };

    private static Vector4 Blend(Vector4 destination, Vector4 source, float alpha, TextureBlendMode mode)
    {
        var d = new Vector3(destination.X, destination.Y, destination.Z);
        var s = new Vector3(source.X, source.Y, source.Z);
        var mixed = mode switch
        {
            TextureBlendMode.Multiply => d * s,
            TextureBlendMode.Add => Vector3.Min(Vector3.One, d + s),
            TextureBlendMode.Subtract => Vector3.Max(Vector3.Zero, d - s),
            TextureBlendMode.Screen => Vector3.One - (Vector3.One - d) * (Vector3.One - s),
            TextureBlendMode.Overlay => new Vector3(Overlay(d.X, s.X), Overlay(d.Y, s.Y), Overlay(d.Z, s.Z)),
            TextureBlendMode.Lighten => Vector3.Max(d, s),
            TextureBlendMode.Darken => Vector3.Min(d, s),
            _ => s
        };
        var outAlpha = alpha + destination.W * (1f - alpha);
        return new Vector4(Vector3.Lerp(d, mixed, alpha), outAlpha);
    }

    private static float Overlay(float destination, float source) => destination < 0.5f
        ? 2f * destination * source
        : 1f - 2f * (1f - destination) * (1f - source);

    private static float Wrap01(float value) => value - MathF.Floor(value);
    private static float Noise(Vector2 uv)
    {
        var value = MathF.Sin(Vector2.Dot(uv, new Vector2(12.9898f, 78.233f))) * 43758.5453f;
        return value - MathF.Floor(value);
    }

    private static Bitmap ToBitmap(float[] source, int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            var bytes = new byte[data.Stride * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var sourceIndex = (y * size + x) * 4;
                var targetIndex = y * data.Stride + x * 4;
                bytes[targetIndex] = ToByte(source[sourceIndex + 2]);
                bytes[targetIndex + 1] = ToByte(source[sourceIndex + 1]);
                bytes[targetIndex + 2] = ToByte(source[sourceIndex]);
                bytes[targetIndex + 3] = ToByte(source[sourceIndex + 3]);
            }
            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        }
        finally { bitmap.UnlockBits(data); }
        return bitmap;
    }

    private static byte ToByte(float value) => (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);

    private sealed class ImagePixels : IDisposable
    {
        private readonly Bitmap _bitmap;
        private readonly byte[] _pixels;
        private readonly int _stride;
        public int Width => _bitmap.Width;
        public int Height => _bitmap.Height;

        public ImagePixels(string path)
        {
            using var source = new Bitmap(path);
            _bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(_bitmap)) graphics.DrawImageUnscaled(source, 0, 0);
            var data = _bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            _stride = data.Stride;
            _pixels = new byte[_stride * Height];
            Marshal.Copy(data.Scan0, _pixels, 0, _pixels.Length);
            _bitmap.UnlockBits(data);
        }

        public Vector4 Sample(Vector2 uv, TextureSamplingSettings sampling)
        {
            uv = new Vector2(Wrap(uv.X, sampling.Wrap), Wrap(uv.Y, sampling.Wrap));
            var x = Math.Clamp((int)(uv.X * (Width - 1)), 0, Width - 1);
            var y = Math.Clamp((int)((1f - uv.Y) * (Height - 1)), 0, Height - 1);
            var index = y * _stride + x * 4;
            return new Vector4(_pixels[index + 2] / 255f, _pixels[index + 1] / 255f,
                _pixels[index] / 255f, _pixels[index + 3] / 255f);
        }

        private static float Wrap(float value, TextureWrap wrap) => wrap switch
        {
            TextureWrap.ClampToEdge => Math.Clamp(value, 0f, 1f),
            TextureWrap.MirroredRepeat => 1f - MathF.Abs((value - MathF.Floor(value)) * 2f - 1f),
            _ => Wrap01(value)
        };

        public void Dispose() => _bitmap.Dispose();
    }
}
