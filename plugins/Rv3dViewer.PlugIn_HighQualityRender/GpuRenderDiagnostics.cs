using System.Numerics;
using System.Text.Json;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal readonly record struct GpuRenderDiagnosticPaths(
    string RawPath,
    string DenoisedPath,
    string VariancePath,
    string SummaryPath)
{
    internal static GpuRenderDiagnosticPaths FromOutputPath(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath) ?? string.Empty;
        var stem = Path.GetFileNameWithoutExtension(outputPath);
        string FileName(string suffix, string extension) => Path.Combine(directory, $"{stem}_{suffix}{extension}");
        return new(FileName("raw", ".png"), FileName("denoised", ".png"),
            FileName("variance", ".png"), FileName("noise", ".json"));
    }
}

internal sealed record GpuRenderNoiseSummary(
    int Width,
    int Height,
    int SamplesPerPixel,
    double MeanVariance,
    double Percentile95Variance,
    double MaximumVariance,
    double RawDenoisedMeanAbsoluteDifference,
    int InvalidPixelCount,
    int FireflyClampedPixelCount,
    string DenoiserBackend,
    double AverageSamplesPerPixel = 0D,
    int MinimumSamplesPerPixel = 0,
    int MaximumSamplesPerPixel = 0);

internal static class GpuRenderDiagnostics
{
    internal static void WriteSummary(string path, GpuRenderNoiseSummary summary)
    {
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        writer.WriteNumber(nameof(summary.Width), summary.Width);
        writer.WriteNumber(nameof(summary.Height), summary.Height);
        writer.WriteNumber(nameof(summary.SamplesPerPixel), summary.SamplesPerPixel);
        writer.WriteNumber(nameof(summary.MeanVariance), FiniteOrZero(summary.MeanVariance));
        writer.WriteNumber(nameof(summary.Percentile95Variance), FiniteOrZero(summary.Percentile95Variance));
        writer.WriteNumber(nameof(summary.MaximumVariance), FiniteOrZero(summary.MaximumVariance));
        writer.WriteNumber(nameof(summary.RawDenoisedMeanAbsoluteDifference),
            FiniteOrZero(summary.RawDenoisedMeanAbsoluteDifference));
        writer.WriteNumber(nameof(summary.InvalidPixelCount), summary.InvalidPixelCount);
        writer.WriteNumber(nameof(summary.FireflyClampedPixelCount), summary.FireflyClampedPixelCount);
        writer.WriteString(nameof(summary.DenoiserBackend), summary.DenoiserBackend);
        writer.WriteNumber(nameof(summary.AverageSamplesPerPixel), FiniteOrZero(summary.AverageSamplesPerPixel));
        writer.WriteNumber(nameof(summary.MinimumSamplesPerPixel), summary.MinimumSamplesPerPixel);
        writer.WriteNumber(nameof(summary.MaximumSamplesPerPixel), summary.MaximumSamplesPerPixel);
        writer.WriteEndObject();
    }

    internal static (GpuRenderNoiseSummary Summary, float[] VarianceHeatmap) Analyze(
        float[] raw, float[] denoised, float[] denoiseGuides,
        int width, int height, int samplesPerPixel, string denoiserBackend, uint[]? sampleCounts = null)
    {
        var pixelCount = checked(width * height);
        if (raw.Length != pixelCount * 4 || denoised.Length != raw.Length || denoiseGuides.Length != raw.Length)
            throw new ArgumentException("GPU diagnostic buffer dimensions must match.");
        if (sampleCounts is not null && sampleCounts.Length != pixelCount)
            throw new ArgumentException("GPU sample-count buffer dimensions must match.");

        var variances = new float[pixelCount];
        var varianceSum = 0D;
        var differenceSum = 0D;
        var validDifferenceComponents = 0;
        var invalidPixelCount = 0;
        var fireflyClampedPixelCount = 0;
        for (var pixel = 0; pixel < pixelCount; pixel++)
        {
            var offset = pixel * 4;
            var mean = denoiseGuides[offset + 1];
            var variance = MathF.Max(0F, denoiseGuides[offset + 2] - mean * mean);
            variances[pixel] = float.IsFinite(variance) ? variance : 0F;
            varianceSum += variances[pixel];
            var diagnosticFlag = denoiseGuides[offset + 3];
            var invalidPixel = diagnosticFlag > 0F ||
                !float.IsFinite(mean) || !float.IsFinite(denoiseGuides[offset + 2]);
            if (diagnosticFlag < 0F) fireflyClampedPixelCount++;
            for (var channel = 0; channel < 3; channel++)
            {
                var rawValue = raw[offset + channel];
                var denoisedValue = denoised[offset + channel];
                if (!float.IsFinite(rawValue) || !float.IsFinite(denoisedValue))
                {
                    invalidPixel = true;
                    continue;
                }

                differenceSum += Math.Abs(rawValue - denoisedValue);
                validDifferenceComponents++;
            }

            if (invalidPixel) invalidPixelCount++;
        }

        var sorted = (float[])variances.Clone();
        Array.Sort(sorted);
        var percentileIndex = Math.Clamp((int)MathF.Ceiling(pixelCount * 0.95F) - 1, 0, pixelCount - 1);
        var percentile95 = sorted[percentileIndex];
        var maximum = sorted[^1];
        var scale = Math.Max(percentile95, maximum * 0.01F);
        scale = Math.Max(scale, 0.000001F);
        var heatmap = new float[raw.Length];
        for (var pixel = 0; pixel < pixelCount; pixel++)
        {
            var normalized = Math.Clamp(MathF.Log(1F + variances[pixel] * 9F / scale) / MathF.Log(10F), 0F, 1F);
            var color = HeatColor(normalized);
            var offset = pixel * 4;
            heatmap[offset] = color.X;
            heatmap[offset + 1] = color.Y;
            heatmap[offset + 2] = color.Z;
            heatmap[offset + 3] = 1F;
        }

        var averageSamples = sampleCounts is null ? samplesPerPixel : sampleCounts.Average(static count => (double)count);
        var minimumSamples = sampleCounts is null ? samplesPerPixel : (int)sampleCounts.Min();
        var maximumSamples = sampleCounts is null ? samplesPerPixel : (int)sampleCounts.Max();
        return (new GpuRenderNoiseSummary(
            width, height, samplesPerPixel,
            varianceSum / Math.Max(pixelCount, 1), percentile95, maximum,
            differenceSum / Math.Max(validDifferenceComponents, 1), invalidPixelCount, fireflyClampedPixelCount,
            denoiserBackend, averageSamples, minimumSamples, maximumSamples), heatmap);
    }

    private static double FiniteOrZero(double value) => double.IsFinite(value) ? value : 0D;

    private static Vector3 HeatColor(float value) => value < 0.5F
        ? Vector3.Lerp(new Vector3(0F, 0F, 0.15F), new Vector3(0F, 0.8F, 1F), value * 2F)
        : Vector3.Lerp(new Vector3(0F, 0.8F, 1F), new Vector3(1F, 0.05F, 0F), (value - 0.5F) * 2F);
}
