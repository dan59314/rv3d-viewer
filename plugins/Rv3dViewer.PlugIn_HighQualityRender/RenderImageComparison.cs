using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Rv3dViewer.HighQualityRenderPlugin;

/// <summary>
/// Compares rendered display images without changing either renderer.  The result is intended
/// to make camera, color-pipeline, transparency and edge differences measurable and repeatable.
/// </summary>
public static class RenderImageComparison
{
    private const float ChangedPixelThreshold = 4F / 255F;
    private const float EdgeThreshold = 0.08F;

    public static RenderComparisonReport Compare(
        string referenceImagePath,
        string candidateImagePath,
        string? heatmapPath = null,
        string? reportPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceImagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateImagePath);

        using var referenceSource = new Bitmap(referenceImagePath);
        using var candidateSource = new Bitmap(candidateImagePath);
        using var reference = ConvertToArgb(referenceSource);
        using var candidate = ConvertToArgb(candidateSource);
        var report = ComparePixels(referenceImagePath, candidateImagePath, reference, candidate, heatmapPath);

        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            EnsureOutputDirectory(reportPath);
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }

        return report;
    }

    private static RenderComparisonReport ComparePixels(
        string referenceImagePath,
        string candidateImagePath,
        Bitmap reference,
        Bitmap candidate,
        string? heatmapPath)
    {
        var referencePixels = ReadArgb(reference);
        var candidatePixels = ReadArgb(candidate);
        var overlapWidth = Math.Min(reference.Width, candidate.Width);
        var overlapHeight = Math.Min(reference.Height, candidate.Height);
        var overlapPixelCount = overlapWidth * overlapHeight;

        double displayAbsolute = 0D;
        double linearAbsolute = 0D;
        double squared = 0D;
        double alphaAbsolute = 0D;
        var maximum = 0F;
        var changedPixels = 0;

        for (var y = 0; y < overlapHeight; y++)
        for (var x = 0; x < overlapWidth; x++)
        {
            var referenceOffset = (y * reference.Width + x) * 4;
            var candidateOffset = (y * candidate.Width + x) * 4;
            var pixelMaximum = 0F;
            for (var channel = 0; channel < 3; channel++)
            {
                var referenceDisplay = referencePixels[referenceOffset + channel] / 255F;
                var candidateDisplay = candidatePixels[candidateOffset + channel] / 255F;
                var displayDifference = MathF.Abs(referenceDisplay - candidateDisplay);
                var linearDifference = MathF.Abs(SrgbToLinear(referenceDisplay) - SrgbToLinear(candidateDisplay));
                displayAbsolute += displayDifference;
                linearAbsolute += linearDifference;
                squared += displayDifference * displayDifference;
                pixelMaximum = MathF.Max(pixelMaximum, displayDifference);
                maximum = MathF.Max(maximum, displayDifference);
            }

            alphaAbsolute += Math.Abs(referencePixels[referenceOffset + 3] - candidatePixels[candidateOffset + 3]) / 255D;
            if (pixelMaximum > ChangedPixelThreshold) changedPixels++;
        }

        var edgeMismatch = CalculateEdgeMismatch(referencePixels, reference.Width, candidatePixels, candidate.Width,
            overlapWidth, overlapHeight);
        if (!string.IsNullOrWhiteSpace(heatmapPath))
            SaveHeatmap(referencePixels, reference.Size, candidatePixels, candidate.Size, heatmapPath);

        var rgbSampleCount = Math.Max(1, overlapPixelCount * 3);
        return new RenderComparisonReport(
            referenceImagePath,
            candidateImagePath,
            reference.Width,
            reference.Height,
            candidate.Width,
            candidate.Height,
            reference.Size == candidate.Size,
            overlapPixelCount,
            displayAbsolute / rgbSampleCount,
            linearAbsolute / rgbSampleCount,
            Math.Sqrt(squared / rgbSampleCount),
            maximum,
            alphaAbsolute / Math.Max(1, overlapPixelCount),
            changedPixels / (double)Math.Max(1, overlapPixelCount),
            edgeMismatch,
            heatmapPath,
            DateTimeOffset.UtcNow);
    }

    private static double CalculateEdgeMismatch(
        byte[] reference,
        int referenceWidth,
        byte[] candidate,
        int candidateWidth,
        int width,
        int height)
    {
        if (width < 2 || height < 2) return 0D;

        var mismatched = 0;
        var count = 0;
        for (var y = 0; y < height - 1; y++)
        for (var x = 0; x < width - 1; x++)
        {
            var referenceEdge = IsEdge(reference, referenceWidth, x, y);
            var candidateEdge = IsEdge(candidate, candidateWidth, x, y);
            if (referenceEdge != candidateEdge) mismatched++;
            count++;
        }

        return mismatched / (double)Math.Max(1, count);
    }

    private static bool IsEdge(byte[] pixels, int width, int x, int y)
    {
        var center = Luminance(pixels, (y * width + x) * 4);
        var right = Luminance(pixels, (y * width + x + 1) * 4);
        var below = Luminance(pixels, ((y + 1) * width + x) * 4);
        return MathF.Max(MathF.Abs(center - right), MathF.Abs(center - below)) >= EdgeThreshold;
    }

    private static float Luminance(byte[] pixels, int offset) =>
        0.0722F * (pixels[offset] / 255F) +
        0.7152F * (pixels[offset + 1] / 255F) +
        0.2126F * (pixels[offset + 2] / 255F);

    private static void SaveHeatmap(
        byte[] reference,
        Size referenceSize,
        byte[] candidate,
        Size candidateSize,
        string outputPath)
    {
        var width = Math.Max(referenceSize.Width, candidateSize.Width);
        var height = Math.Max(referenceSize.Height, candidateSize.Height);
        var heatmap = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var outputOffset = (y * width + x) * 4;
            if (x >= referenceSize.Width || y >= referenceSize.Height ||
                x >= candidateSize.Width || y >= candidateSize.Height)
            {
                heatmap[outputOffset] = 255;
                heatmap[outputOffset + 2] = 255;
                heatmap[outputOffset + 3] = 255;
                continue;
            }

            var referenceOffset = (y * referenceSize.Width + x) * 4;
            var candidateOffset = (y * candidateSize.Width + x) * 4;
            var difference = 0F;
            for (var channel = 0; channel < 4; channel++)
                difference = MathF.Max(difference,
                    MathF.Abs(reference[referenceOffset + channel] - candidate[candidateOffset + channel]) / 255F);

            // Black = equal, red = moderate difference, yellow/white = large difference.
            var intensity = Math.Clamp(difference * 4F, 0F, 1F);
            heatmap[outputOffset] = (byte)(32F * intensity);
            heatmap[outputOffset + 1] = (byte)(255F * MathF.Max(0F, intensity * 2F - 1F));
            heatmap[outputOffset + 2] = (byte)(255F * intensity);
            heatmap[outputOffset + 3] = 255;
        }

        EnsureOutputDirectory(outputPath);
        using var bitmap = WriteArgb(heatmap, width, height);
        bitmap.Save(outputPath, ImageFormat.Png);
    }

    private static Bitmap ConvertToArgb(Bitmap source)
    {
        var converted = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(converted);
        graphics.DrawImageUnscaled(source, 0, 0);
        return converted;
    }

    private static byte[] ReadArgb(Bitmap bitmap)
    {
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var pixels = new byte[bitmap.Width * bitmap.Height * 4];
            for (var y = 0; y < bitmap.Height; y++)
                Marshal.Copy(data.Scan0 + y * data.Stride, pixels, y * bitmap.Width * 4, bitmap.Width * 4);
            return pixels;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static Bitmap WriteArgb(byte[] pixels, int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            for (var y = 0; y < height; y++)
                Marshal.Copy(pixels, y * width * 4, data.Scan0 + y * data.Stride, width * 4);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
        return bitmap;
    }

    private static float SrgbToLinear(float value) => value <= 0.04045F
        ? value / 12.92F
        : MathF.Pow((value + 0.055F) / 1.055F, 2.4F);

    private static void EnsureOutputDirectory(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    }
}

public sealed record RenderComparisonReport(
    string ReferenceImagePath,
    string CandidateImagePath,
    int ReferenceWidth,
    int ReferenceHeight,
    int CandidateWidth,
    int CandidateHeight,
    bool DimensionsMatch,
    int ComparedPixelCount,
    double DisplayMeanAbsoluteError,
    double LinearMeanAbsoluteError,
    double DisplayRootMeanSquareError,
    double MaximumChannelError,
    double AlphaMeanAbsoluteError,
    double ChangedPixelFraction,
    double EdgeMismatchFraction,
    string? HeatmapPath,
    DateTimeOffset CreatedUtc);
