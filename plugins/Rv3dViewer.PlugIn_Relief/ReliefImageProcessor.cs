using System.Drawing;
using System.Drawing.Imaging;
using SkiaSharp;

namespace Rv3dViewer.ReliefPlugin;

internal readonly record struct ReliefImageProcessingSettings(
    float HueDegrees,
    float SaturationPercent,
    float ValuePercent,
    bool Grayscale,
    bool Invert,
    bool Symmetry,
    float SymmetryAxisPercent,
    bool ReduceColors,
    int ColorLevels);

internal static class ReliefImageProcessor
{
    public static SKBitmap ProcessPipeline(
        SKBitmap source,
        ReliefImageProcessingSettings settings,
        IReadOnlyList<string> operations,
        float edgeThreshold,
        float edgeStrength,
        int edgeSmoothing,
        float binaryThreshold,
        bool binaryInvert,
        float gaussianBlurRadius,
        CancellationToken cancellationToken)
    {
        var current = source.Copy();
        try
        {
            foreach (var operation in operations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SKBitmap next;
                if (operation.Equals("Grayscale", StringComparison.OrdinalIgnoreCase))
                {
                    next = Process(current, settings with { Grayscale = true }, cancellationToken);
                }
                else if (operation.Equals("EdgeDetection", StringComparison.OrdinalIgnoreCase))
                {
                    var edgeMap = ReliefDepthMap.CreateEdgeMap(
                        current, edgeThreshold, edgeStrength, edgeSmoothing, cancellationToken);
                    next = edgeMap.ToBitmap(cancellationToken);
                }
                else if (operation.Equals("Binarization", StringComparison.OrdinalIgnoreCase))
                {
                    next = ApplyBinarization(current, binaryThreshold, binaryInvert, cancellationToken);
                }
                else if (operation.Equals("GaussianBlur", StringComparison.OrdinalIgnoreCase))
                {
                    next = ApplyGaussianBlur(current, gaussianBlurRadius);
                }
                else continue;
                current.Dispose();
                current = next;
            }
            return current;
        }
        catch
        {
            current.Dispose();
            throw;
        }
    }

    private static SKBitmap ApplyGaussianBlur(SKBitmap source, float radius)
    {
        var output = new SKBitmap(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(output);
        using var paint = new SKPaint { ImageFilter = SKImageFilter.CreateBlur(Math.Max(0.1f, radius), Math.Max(0.1f, radius)) };
        canvas.DrawBitmap(source, 0, 0, paint);
        canvas.Flush();
        return output;
    }

    private static SKBitmap ApplyBinarization(
        SKBitmap source, float thresholdPercent, bool invert, CancellationToken cancellationToken)
    {
        var threshold = Math.Clamp(thresholdPercent, 0f, 100f) * 2.55f;
        var pixels = source.Pixels;
        for (var index = 0; index < pixels.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var white = ToLuminance(pixels[index]) >= threshold;
            if (invert) white = !white;
            var value = white ? (byte)255 : (byte)0;
            pixels[index] = new SKColor(value, value, value, pixels[index].Alpha);
        }
        var output = new SKBitmap(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        output.Pixels = pixels;
        return output;
    }
    public static SKBitmap LoadAndOrient(string path, CancellationToken cancellationToken)
    {
        if (string.Equals(Path.GetExtension(path), ".tif", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetExtension(path), ".tiff", StringComparison.OrdinalIgnoreCase))
            return LoadTiffAndOrient(path, cancellationToken);

        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream) ?? throw new InvalidDataException("無法辨識此圖檔格式。");
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var decoded = new SKBitmap(info);
        var result = codec.GetPixels(info, decoded.GetPixels());
        if (result is not SKCodecResult.Success and not SKCodecResult.IncompleteInput)
        {
            decoded.Dispose();
            throw new InvalidDataException($"圖檔解碼失敗：{result}");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var oriented = ApplyOrientation(decoded, codec.EncodedOrigin, cancellationToken);
        decoded.Dispose();
        return oriented;
    }

    private static SKBitmap LoadTiffAndOrient(string path, CancellationToken cancellationToken)
    {
        using var source = System.Drawing.Image.FromFile(path);
        const int orientationPropertyId = 0x0112;
        if (source.PropertyIdList.Contains(orientationPropertyId))
        {
            var propertyItem = source.GetPropertyItem(orientationPropertyId);
            var orientation = propertyItem?.Value is { Length: > 0 } values ? values[0] : (byte)1;
            var rotateFlip = orientation switch
            {
                2 => RotateFlipType.RotateNoneFlipX,
                3 => RotateFlipType.Rotate180FlipNone,
                4 => RotateFlipType.RotateNoneFlipY,
                5 => RotateFlipType.Rotate90FlipX,
                6 => RotateFlipType.Rotate90FlipNone,
                7 => RotateFlipType.Rotate270FlipX,
                8 => RotateFlipType.Rotate270FlipNone,
                _ => RotateFlipType.RotateNoneFlipNone
            };
            source.RotateFlip(rotateFlip);
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var converted = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(converted))
            graphics.DrawImage(source, 0, 0, source.Width, source.Height);

        using var stream = new MemoryStream();
        converted.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        cancellationToken.ThrowIfCancellationRequested();
        return SKBitmap.Decode(stream) ?? throw new InvalidDataException("TIFF 圖檔解碼失敗。");
    }

    public static SKBitmap Process(
        SKBitmap source,
        ReliefImageProcessingSettings settings,
        CancellationToken cancellationToken)
    {
        var width = source.Width;
        var height = source.Height;
        var pixels = source.Pixels;

        for (var y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var offset = y * width;
            for (var x = 0; x < width; x++)
            {
                var color = pixels[offset + x];
                RgbToHsv(color.Red, color.Green, color.Blue, out var hue, out var saturation, out var value);
                hue = WrapHue(hue + settings.HueDegrees);
                saturation = Math.Clamp(saturation * settings.SaturationPercent / 100f, 0f, 1f);
                value = Math.Clamp(value * settings.ValuePercent / 100f, 0f, 1f);
                var adjusted = HsvToColor(hue, saturation, value, color.Alpha);
                if (settings.Grayscale)
                {
                    var gray = ToLuminance(adjusted);
                    adjusted = new SKColor(gray, gray, gray, adjusted.Alpha);
                }
                pixels[offset + x] = adjusted;
            }
        }

        NormalizeLuminance(pixels, cancellationToken);

        if (settings.Invert)
        {
            for (var index = 0; index < pixels.Length; index++)
            {
                if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
                var color = pixels[index];
                pixels[index] = new SKColor(
                    (byte)(255 - color.Red),
                    (byte)(255 - color.Green),
                    (byte)(255 - color.Blue),
                    color.Alpha);
            }
        }

        if (settings.Symmetry)
            ApplySymmetry(pixels, width, height, settings.SymmetryAxisPercent, cancellationToken);

        if (settings.ReduceColors)
            Posterize(pixels, settings.ColorLevels, cancellationToken);

        var output = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        output.Pixels = pixels;
        return output;
    }

    public static SKBitmap BlendDepth(
        SKBitmap imageProcessingDepth,
        SKBitmap aiDepth,
        float aiWeight,
        CancellationToken cancellationToken)
    {
        if (imageProcessingDepth.Width != aiDepth.Width || imageProcessingDepth.Height != aiDepth.Height)
            throw new ArgumentException("AI 深度與影像處理結果的尺寸必須相同。");

        aiWeight = Math.Clamp(aiWeight, 0f, 1f);
        var imageProcessingPixels = imageProcessingDepth.Pixels;
        var aiPixels = aiDepth.Pixels;
        var outputPixels = new SKColor[imageProcessingPixels.Length];
        for (var index = 0; index < outputPixels.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var imageProcessingValue = ToLuminance(imageProcessingPixels[index]);
            var depth = ToLuminance(aiPixels[index]);
            var blended = (byte)MathF.Round(imageProcessingValue * (1f - aiWeight) + depth * aiWeight);
            outputPixels[index] = new SKColor(blended, blended, blended, 255);
        }

        var output = new SKBitmap(
            imageProcessingDepth.Width,
            imageProcessingDepth.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Premul);
        output.Pixels = outputPixels;
        return output;
    }

    public static SKBitmap ApplyPostProcessing(
        SKBitmap source,
        ReliefImageProcessingSettings settings,
        CancellationToken cancellationToken)
    {
        var pixels = source.Pixels;
        if (settings.Invert)
        {
            for (var index = 0; index < pixels.Length; index++)
            {
                if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
                var color = pixels[index];
                pixels[index] = new SKColor(
                    (byte)(255 - color.Red),
                    (byte)(255 - color.Green),
                    (byte)(255 - color.Blue),
                    color.Alpha);
            }
        }

        if (settings.Symmetry)
            ApplySymmetry(pixels, source.Width, source.Height, settings.SymmetryAxisPercent, cancellationToken);
        if (settings.ReduceColors)
            Posterize(pixels, settings.ColorLevels, cancellationToken);

        var output = new SKBitmap(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        output.Pixels = pixels;
        return output;
    }

    public static Bitmap CreateDisplayBitmap(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        using var decoded = new Bitmap(stream);
        return new Bitmap(decoded);
    }

    public static void SaveLosslessPng(SKBitmap bitmap, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    private static SKBitmap ApplyOrientation(
        SKBitmap source,
        SKEncodedOrigin origin,
        CancellationToken cancellationToken)
    {
        var swapsAxes = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or
            SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var targetWidth = swapsAxes ? source.Height : source.Width;
        var targetHeight = swapsAxes ? source.Width : source.Height;
        var sourcePixels = source.Pixels;
        var targetPixels = new SKColor[targetWidth * targetHeight];

        for (var y = 0; y < source.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < source.Width; x++)
            {
                var (targetX, targetY) = origin switch
                {
                    SKEncodedOrigin.TopRight => (source.Width - 1 - x, y),
                    SKEncodedOrigin.BottomRight => (source.Width - 1 - x, source.Height - 1 - y),
                    SKEncodedOrigin.BottomLeft => (x, source.Height - 1 - y),
                    SKEncodedOrigin.LeftTop => (y, x),
                    SKEncodedOrigin.RightTop => (source.Height - 1 - y, x),
                    SKEncodedOrigin.RightBottom => (source.Height - 1 - y, source.Width - 1 - x),
                    SKEncodedOrigin.LeftBottom => (y, source.Width - 1 - x),
                    _ => (x, y)
                };
                targetPixels[targetY * targetWidth + targetX] = sourcePixels[y * source.Width + x];
            }
        }

        var output = new SKBitmap(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        output.Pixels = targetPixels;
        return output;
    }

    private static void NormalizeLuminance(SKColor[] pixels, CancellationToken cancellationToken)
    {
        byte minimum = 255;
        byte maximum = 0;
        for (var index = 0; index < pixels.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            if (pixels[index].Alpha == 0) continue;
            var luminance = ToLuminance(pixels[index]);
            minimum = Math.Min(minimum, luminance);
            maximum = Math.Max(maximum, luminance);
        }

        if (maximum <= minimum) return;
        var range = maximum - minimum;
        for (var index = 0; index < pixels.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var color = pixels[index];
            if (color.Alpha == 0) continue;
            var normalizedValue = Math.Clamp((ToLuminance(color) - minimum) / (float)range, 0f, 1f);
            RgbToHsv(color.Red, color.Green, color.Blue, out var hue, out var saturation, out _);
            pixels[index] = HsvToColor(hue, saturation, normalizedValue, color.Alpha);
        }
    }

    private static void ApplySymmetry(
        SKColor[] pixels,
        int width,
        int height,
        float axisPercent,
        CancellationToken cancellationToken)
    {
        var axis = Math.Clamp((int)MathF.Round((width - 1) * axisPercent / 100f), 0, width - 1);
        var pairCount = Math.Min(axis, width - 1 - axis);
        for (var y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = y * width;
            for (var distance = 1; distance <= pairCount; distance++)
            {
                var left = row + axis - distance;
                var right = row + axis + distance;
                var averaged = Average(pixels[left], pixels[right]);
                pixels[left] = averaged;
                pixels[right] = averaged;
            }
        }
    }

    private static void Posterize(SKColor[] pixels, int levels, CancellationToken cancellationToken)
    {
        levels = Math.Clamp(levels, 2, 256);
        for (var index = 0; index < pixels.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var color = pixels[index];
            pixels[index] = new SKColor(
                Quantize(color.Red, levels),
                Quantize(color.Green, levels),
                Quantize(color.Blue, levels),
                color.Alpha);
        }
    }

    private static SKColor Average(SKColor first, SKColor second) => new(
        (byte)((first.Red + second.Red) / 2),
        (byte)((first.Green + second.Green) / 2),
        (byte)((first.Blue + second.Blue) / 2),
        (byte)((first.Alpha + second.Alpha) / 2));

    private static byte Quantize(byte value, int levels) =>
        (byte)Math.Clamp((int)MathF.Round(MathF.Round(value / 255f * (levels - 1)) * 255f / (levels - 1)), 0, 255);

    private static byte ToLuminance(SKColor color) =>
        (byte)Math.Clamp((int)MathF.Round(color.Red * 0.2126f + color.Green * 0.7152f + color.Blue * 0.0722f), 0, 255);

    private static float WrapHue(float hue)
    {
        hue %= 360f;
        return hue < 0f ? hue + 360f : hue;
    }

    private static void RgbToHsv(byte red, byte green, byte blue, out float hue, out float saturation, out float value)
    {
        var r = red / 255f;
        var g = green / 255f;
        var b = blue / 255f;
        var maximum = Math.Max(r, Math.Max(g, b));
        var minimum = Math.Min(r, Math.Min(g, b));
        var delta = maximum - minimum;

        hue = delta == 0f
            ? 0f
            : maximum == r
                ? 60f * (((g - b) / delta) % 6f)
                : maximum == g
                    ? 60f * ((b - r) / delta + 2f)
                    : 60f * ((r - g) / delta + 4f);
        hue = WrapHue(hue);
        saturation = maximum == 0f ? 0f : delta / maximum;
        value = maximum;
    }

    private static SKColor HsvToColor(float hue, float saturation, float value, byte alpha)
    {
        hue = WrapHue(hue);
        var chroma = value * saturation;
        var section = hue / 60f;
        var secondary = chroma * (1f - MathF.Abs(section % 2f - 1f));
        var (r, g, b) = section switch
        {
            < 1f => (chroma, secondary, 0f),
            < 2f => (secondary, chroma, 0f),
            < 3f => (0f, chroma, secondary),
            < 4f => (0f, secondary, chroma),
            < 5f => (secondary, 0f, chroma),
            _ => (chroma, 0f, secondary)
        };
        var match = value - chroma;
        return new SKColor(
            ToByte(r + match),
            ToByte(g + match),
            ToByte(b + match),
            alpha);
    }

    private static byte ToByte(float value) =>
        (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);
}
