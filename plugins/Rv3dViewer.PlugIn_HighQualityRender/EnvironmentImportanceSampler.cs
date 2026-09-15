using System.Numerics;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal sealed class EnvironmentImportanceSampler
{
    private const float Pi = MathF.PI;
    private readonly float[] _cdf;
    private readonly float[] _mass;

    private EnvironmentImportanceSampler(int width, int height, float[] cdf, float[] mass)
    {
        Width = width;
        Height = height;
        _cdf = cdf;
        _mass = mass;
    }

    public int Width { get; }
    public int Height { get; }
    public ReadOnlySpan<float> Cdf => _cdf;
    public ReadOnlySpan<float> Mass => _mass;

    public static EnvironmentImportanceSampler? TryLoad(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !string.Equals(Path.GetExtension(path), ".hdr", StringComparison.OrdinalIgnoreCase)) return null;
        try { return Create(RadianceHdrLoader.Load(path)); }
        catch { return null; }
    }

    internal static EnvironmentImportanceSampler Create(HdrImage image)
    {
        var count = checked(image.Width * image.Height);
        var cdf = new float[count];
        var mass = new float[count];
        var total = 0D;
        for (var y = 0; y < image.Height; y++)
        {
            var sinTheta = Math.Sin(Math.PI * (y + 0.5) / image.Height);
            for (var x = 0; x < image.Width; x++)
            {
                var index = y * image.Width + x;
                var pixel = index * 3;
                var luminance = Math.Max(0D, image.Pixels[pixel] * 0.2126D +
                    image.Pixels[pixel + 1] * 0.7152D + image.Pixels[pixel + 2] * 0.0722D);
                total += luminance * sinTheta;
                cdf[index] = (float)total;
            }
        }
        if (total <= 1E-12)
        {
            for (var index = 0; index < count; index++)
            {
                mass[index] = 1F / count;
                cdf[index] = (index + 1F) / count;
            }
        }
        else
        {
            var previous = 0F;
            for (var index = 0; index < count; index++)
            {
                cdf[index] = (float)(cdf[index] / total);
                mass[index] = Math.Max(0F, cdf[index] - previous);
                previous = cdf[index];
            }
            cdf[^1] = 1F;
        }
        return new EnvironmentImportanceSampler(image.Width, image.Height, cdf, mass);
    }

    public EnvironmentSample Sample(float selector, float jitterX, float jitterY, float rotation)
    {
        var index = Array.BinarySearch(_cdf, Math.Clamp(selector, 0F, 0.99999994F));
        if (index < 0) index = ~index;
        index = Math.Clamp(index, 0, _cdf.Length - 1);
        var x = index % Width;
        var y = index / Width;
        var u = (x + jitterX) / Width;
        var v = (y + jitterY) / Height;
        var phi = (u - 0.5F) * 2F * Pi;
        var latitude = (0.5F - v) * Pi;
        var radial = MathF.Cos(latitude);
        var mapDirection = new Vector3(MathF.Cos(phi) * radial, MathF.Sin(latitude), MathF.Sin(phi) * radial);
        // Realtime/GPU lookup rotates world directions into map space by -rotation,
        // so sampled map directions use the inverse (+rotation) to return to world space.
        var worldDirection = EnvironmentDirectionTransform.MapToWorld(mapDirection, rotation);
        return new EnvironmentSample(Vector3.Normalize(worldDirection), PdfForBin(index));
    }

    public float Pdf(Vector3 worldDirection, float rotation)
    {
        var direction = EnvironmentDirectionTransform.WorldToMap(worldDirection, rotation);
        var u = MathF.Atan2(direction.Z, direction.X) / (2F * Pi) + 0.5F;
        var v = 0.5F - MathF.Asin(Math.Clamp(direction.Y, -1F, 1F)) / Pi;
        var x = Math.Clamp((int)(u * Width), 0, Width - 1);
        var y = Math.Clamp((int)(v * Height), 0, Height - 1);
        return PdfForBin(y * Width + x);
    }

    private float PdfForBin(int index)
    {
        var y = index / Width;
        var theta0 = Pi * y / Height;
        var theta1 = Pi * (y + 1F) / Height;
        var solidAngle = 2F * Pi / Width * MathF.Max(MathF.Cos(theta0) - MathF.Cos(theta1), 0.00000001F);
        return _mass[index] / solidAngle;
    }
}

internal readonly record struct EnvironmentSample(Vector3 Direction, float Pdf);
