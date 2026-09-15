using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace Rv3dViewer.ReliefPlugin;

internal sealed class ReliefDepthEstimator : IDisposable
{
    internal const string ModelFileName = "depth-anything-v2-small-uint8.onnx";
    private const int TargetSize = 518;
    private static readonly float[] ImageMean = [0.485f, 0.456f, 0.406f];
    private static readonly float[] ImageStandardDeviation = [0.229f, 0.224f, 0.225f];
    private readonly string _modelPath;
    private readonly object _sessionLock = new();
    private InferenceSession? _session;

    public ReliefDepthEstimator(string modelPath) => _modelPath = modelPath;

    public bool IsModelAvailable => File.Exists(_modelPath);

    public ReliefDepthMap Estimate(SKBitmap source, CancellationToken cancellationToken)
    {
        var global = EstimateSingleScale(source, cancellationToken);
        if (Math.Min(source.Width, source.Height) < TargetSize * 1.4f)
            return global;

        return FuseHighResolutionTiles(source, global, cancellationToken);
    }

    private ReliefDepthMap EstimateSingleScale(SKBitmap source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (inputWidth, inputHeight) = CalculateInputSize(source.Width, source.Height);
        using var resized = Resize(source, inputWidth, inputHeight);
        var tensor = CreateInputTensor(resized, cancellationToken);
        int[] dimensions;
        float[] depth;
        try
        {
            lock (_sessionLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var session = GetOrCreateSession();
                var inputName = session.InputMetadata.Keys.Single();
                var input = NamedOnnxValue.CreateFromTensor(inputName, tensor);
                using var runOptions = new RunOptions();
                using var cancellationRegistration = cancellationToken.Register(() => runOptions.Terminate = true);
                using var outputs = session.Run([input], session.OutputMetadata.Keys.ToArray(), runOptions);
                cancellationToken.ThrowIfCancellationRequested();
                var output = outputs.First().AsTensor<float>();
                dimensions = output.Dimensions.ToArray();
                depth = output.ToArray();
            }
        }
        catch (OnnxRuntimeException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        var outputHeight = dimensions.Length switch
        {
            3 => dimensions[1],
            4 => dimensions[2],
            _ => throw new InvalidDataException($"AI 深度輸出維度不受支援：{string.Join('x', dimensions)}")
        };
        var outputWidth = dimensions.Length == 3 ? dimensions[2] : dimensions[3];
        var normalized = ReliefDepthMap.FromRawDepth(
            depth,
            outputWidth,
            outputHeight,
            cancellationToken);
        return normalized.ResizeBicubic(source.Width, source.Height, cancellationToken);
    }

    private ReliefDepthMap FuseHighResolutionTiles(
        SKBitmap source,
        ReliefDepthMap global,
        CancellationToken cancellationToken)
    {
        const float tileScale = 0.62f;
        const float localWeight = 0.55f;
        var tileWidth = Math.Clamp((int)MathF.Ceiling(source.Width * tileScale), TargetSize, source.Width);
        var tileHeight = Math.Clamp((int)MathF.Ceiling(source.Height * tileScale), TargetSize, source.Height);
        var tileBounds = new[]
        {
            new SKRectI(0, 0, tileWidth, tileHeight),
            new SKRectI(source.Width - tileWidth, 0, source.Width, tileHeight),
            new SKRectI(0, source.Height - tileHeight, tileWidth, source.Height),
            new SKRectI(source.Width - tileWidth, source.Height - tileHeight, source.Width, source.Height)
        };
        var accumulatedDepth = (float[])global.Values.Clone();
        var accumulatedWeight = Enumerable.Repeat(1f, global.Values.Length).ToArray();

        foreach (var bounds in tileBounds.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var tileBitmap = ExtractTile(source, bounds);
            var tileDepth = EstimateSingleScale(tileBitmap, cancellationToken);
            var (scale, offset) = CalculateDepthAlignment(global, tileDepth, bounds, cancellationToken);
            AccumulateTile(
                accumulatedDepth,
                accumulatedWeight,
                global.Width,
                tileDepth,
                bounds,
                scale,
                offset,
                localWeight,
                cancellationToken);
        }

        for (var index = 0; index < accumulatedDepth.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            accumulatedDepth[index] = Math.Clamp(accumulatedDepth[index] / accumulatedWeight[index], 0f, 1f);
        }
        return new ReliefDepthMap(global.Width, global.Height, accumulatedDepth);
    }

    private static SKBitmap ExtractTile(SKBitmap source, SKRectI bounds)
    {
        var tile = new SKBitmap(bounds.Width, bounds.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(tile);
        canvas.DrawBitmap(source, bounds, new SKRect(0, 0, bounds.Width, bounds.Height));
        canvas.Flush();
        return tile;
    }

    private static (float Scale, float Offset) CalculateDepthAlignment(
        ReliefDepthMap global,
        ReliefDepthMap tile,
        SKRectI bounds,
        CancellationToken cancellationToken)
    {
        double localSum = 0d;
        double globalSum = 0d;
        double localSquaredSum = 0d;
        double productSum = 0d;
        var sampleStep = Math.Max(1, Math.Min(tile.Width, tile.Height) / 160);
        var count = 0;
        for (var y = 0; y < tile.Height; y += sampleStep)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < tile.Width; x += sampleStep)
            {
                var local = tile.Values[y * tile.Width + x];
                var globalValue = global.Values[(bounds.Top + y) * global.Width + bounds.Left + x];
                localSum += local;
                globalSum += globalValue;
                localSquaredSum += local * local;
                productSum += local * globalValue;
                count++;
            }
        }

        var localMean = localSum / count;
        var globalMean = globalSum / count;
        var localVariance = localSquaredSum / count - localMean * localMean;
        var covariance = productSum / count - localMean * globalMean;
        var scale = localVariance <= 1e-8d
            ? 1f
            : Math.Clamp((float)(covariance / localVariance), 0.5f, 2f);
        return (scale, (float)(globalMean - scale * localMean));
    }

    private static void AccumulateTile(
        float[] accumulatedDepth,
        float[] accumulatedWeight,
        int globalWidth,
        ReliefDepthMap tile,
        SKRectI bounds,
        float scale,
        float offset,
        float localWeight,
        CancellationToken cancellationToken)
    {
        var featherWidth = Math.Max(8f, Math.Min(tile.Width, tile.Height) * 0.16f);
        for (var y = 0; y < tile.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var verticalDistance = Math.Min(y + 1f, tile.Height - y);
            for (var x = 0; x < tile.Width; x++)
            {
                var horizontalDistance = Math.Min(x + 1f, tile.Width - x);
                var feather = SmoothStep(Math.Min(horizontalDistance, verticalDistance) / featherWidth);
                var weight = localWeight * feather;
                if (weight <= float.Epsilon) continue;
                var globalIndex = (bounds.Top + y) * globalWidth + bounds.Left + x;
                var aligned = Math.Clamp(tile.Values[y * tile.Width + x] * scale + offset, 0f, 1f);
                accumulatedDepth[globalIndex] += aligned * weight;
                accumulatedWeight[globalIndex] += weight;
            }
        }
    }

    private static float SmoothStep(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    public void Dispose()
    {
        lock (_sessionLock)
        {
            _session?.Dispose();
            _session = null;
        }
    }

    private InferenceSession GetOrCreateSession()
    {
        if (!IsModelAvailable)
            throw new FileNotFoundException("找不到離線 AI 深度模型。一般灰階模式仍可使用。", _modelPath);

        lock (_sessionLock)
        {
            if (_session is not null) return _session;
            var options = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                InterOpNumThreads = 1,
                IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount / 2)
            };
            try
            {
                _session = new InferenceSession(_modelPath, options);
                return _session;
            }
            finally
            {
                options.Dispose();
            }
        }
    }

    private static (int Width, int Height) CalculateInputSize(int width, int height)
    {
        var scale = TargetSize / (float)Math.Min(width, height);
        var scaledWidth = Math.Max(TargetSize, (int)MathF.Round(width * scale));
        var scaledHeight = Math.Max(TargetSize, (int)MathF.Round(height * scale));
        return (RoundToMultiple(scaledWidth, 14), RoundToMultiple(scaledHeight, 14));
    }

    private static int RoundToMultiple(int value, int multiple) =>
        Math.Max(multiple, (int)MathF.Round(value / (float)multiple) * multiple);

    private static SKBitmap Resize(SKBitmap source, int width, int height)
    {
        var target = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Black);
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawBitmap(source, new SKRect(0, 0, width, height), paint);
        canvas.Flush();
        return target;
    }

    private static DenseTensor<float> CreateInputTensor(SKBitmap bitmap, CancellationToken cancellationToken)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var tensor = new DenseTensor<float>([1, 3, height, width]);
        var pixels = bitmap.Pixels;
        for (var y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < width; x++)
            {
                var color = pixels[y * width + x];
                tensor[0, 0, y, x] = (color.Red / 255f - ImageMean[0]) / ImageStandardDeviation[0];
                tensor[0, 1, y, x] = (color.Green / 255f - ImageMean[1]) / ImageStandardDeviation[1];
                tensor[0, 2, y, x] = (color.Blue / 255f - ImageMean[2]) / ImageStandardDeviation[2];
            }
        }
        return tensor;
    }

}
