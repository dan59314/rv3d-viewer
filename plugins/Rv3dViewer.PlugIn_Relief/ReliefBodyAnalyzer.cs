using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace Rv3dViewer.ReliefPlugin;

internal sealed record ReliefBodyAnalysis(int Width, int Height, float[] Mask, SKRectI? Bounds)
{
    public ReliefBodyAnalysis Copy() => new(Width, Height, (float[])Mask.Clone(), Bounds);

    public SKBitmap CreateOverlay(SKBitmap source, CancellationToken cancellationToken)
    {
        var overlay = source.Copy();
        using var canvas = new SKCanvas(overlay);
        using var paint = new SKPaint { Color = new SKColor(0, 210, 255, 105), IsAntialias = false };
        for (var y = 0; y < Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < Width; x++)
                if (Mask[y * Width + x] >= 0.35f) canvas.DrawPoint(x, y, paint);
        }
        if (Bounds is SKRectI bounds)
        {
            using var boundsPaint = new SKPaint
            {
                Color = SKColors.Lime,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(2f, Math.Min(Width, Height) / 300f)
            };
            canvas.DrawRect(bounds, boundsPaint);
        }
        return overlay;
    }
}

internal static class ReliefBodyMaskRefiner
{
    public static ReliefBodyAnalysis Refine(
        SKBitmap source,
        ReliefBodyAnalysis analysis,
        ReliefPortraitAnalysis? portrait,
        float cleanupPercent,
        CancellationToken token)
    {
        var strength = Math.Clamp(cleanupPercent, 0f, 100f) / 100f;
        if (strength <= float.Epsilon) return analysis.Copy();
        var radius = Math.Max(1, (int)MathF.Round(Math.Clamp(Math.Min(analysis.Width, analysis.Height) / 45f, 8f, 24f) * strength));
        var binary = analysis.Mask.Select(value => value >= 0.20f ? 1 : 0).ToArray();
        var integral = CreateIntegral(binary, analysis.Width, analysis.Height, token);
        var core = new int[binary.Length];
        for (var y = 0; y < analysis.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            for (var x = 0; x < analysis.Width; x++)
            {
                var left = Math.Max(0, x - radius); var right = Math.Min(analysis.Width - 1, x + radius);
                var top = Math.Max(0, y - radius); var bottom = Math.Min(analysis.Height - 1, y + radius);
                var area = (right - left + 1) * (bottom - top + 1);
                core[y * analysis.Width + x] = Sum(integral, analysis.Width, left, top, right, bottom) == area ? 1 : 0;
            }
        }
        var coreIntegral = CreateIntegral(core, analysis.Width, analysis.Height, token);
        var mask = new float[analysis.Mask.Length];
        var face = portrait?.FaceBounds;
        var centerX = face?.MidX ?? analysis.Bounds?.MidX ?? analysis.Width * 0.5f;
        var faceWidth = face?.Width ?? Math.Max(16, (analysis.Bounds?.Width ?? analysis.Width) / 3);
        var rowLeft = Enumerable.Repeat(analysis.Width, analysis.Height).ToArray();
        var rowRight = Enumerable.Repeat(-1, analysis.Height).ToArray();
        for (var y = 0; y < analysis.Height; y++)
        for (var x = 0; x < analysis.Width; x++)
            if (binary[y * analysis.Width + x] != 0)
            {
                rowLeft[y] = Math.Min(rowLeft[y], x);
                rowRight[y] = Math.Max(rowRight[y], x);
            }
        var pixels = source.Pixels;
        for (var y = 0; y < analysis.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            for (var x = 0; x < analysis.Width; x++)
            {
                var index = y * analysis.Width + x;
                var left = Math.Max(0, x - radius); var right = Math.Min(analysis.Width - 1, x + radius);
                var top = Math.Max(0, y - radius); var bottom = Math.Min(analysis.Height - 1, y + radius);
                var opened = Sum(coreIntegral, analysis.Width, left, top, right, bottom) > 0;
                var protectedFace = face is SKRectI bounds && bounds.Contains(x, y);
                var protectedSkin = IsSkin(pixels[index]);
                var leftExtent = Math.Max(1f, centerX - rowLeft[y]);
                var rightExtent = Math.Max(1f, rowRight[y] - centerX);
                var balance = 1.35f - 0.25f * strength;
                var allowance = faceWidth * 0.10f;
                var asymmetricAccessory =
                    x < centerX - rightExtent * balance - allowance ||
                    x > centerX + leftExtent * balance + allowance;
                if (asymmetricAccessory && !protectedSkin && !protectedFace)
                    opened = false;
                if (opened || protectedFace || protectedSkin)
                    mask[index] = analysis.Mask[index];
            }
        }
        ReliefBodyAnalyzer.KeepPrimaryComponentForRefinement(mask, analysis.Width, analysis.Height, portrait, token);
        return new ReliefBodyAnalysis(analysis.Width, analysis.Height, mask,
            ReliefBodyAnalyzer.FindBoundsForRefinement(mask, analysis.Width, analysis.Height));
    }

    private static int[] CreateIntegral(int[] source, int width, int height, CancellationToken token)
    {
        var integral = new int[(width + 1) * (height + 1)];
        for (var y = 0; y < height; y++)
        {
            token.ThrowIfCancellationRequested();
            var rowSum = 0;
            for (var x = 0; x < width; x++)
            {
                rowSum += source[y * width + x];
                integral[(y + 1) * (width + 1) + x + 1] = integral[y * (width + 1) + x + 1] + rowSum;
            }
        }
        return integral;
    }

    private static int Sum(int[] integral, int width, int left, int top, int right, int bottom)
    {
        var stride = width + 1;
        return integral[(bottom + 1) * stride + right + 1] - integral[top * stride + right + 1] -
               integral[(bottom + 1) * stride + left] + integral[top * stride + left];
    }

    private static bool IsSkin(SKColor color)
    {
        var cb = 128f - 0.168736f * color.Red - 0.331264f * color.Green + 0.5f * color.Blue;
        var cr = 128f + 0.5f * color.Red - 0.418688f * color.Green - 0.081312f * color.Blue;
        return color.Red > 60 && color.Red > color.Green + 5 && color.Red > color.Blue + 5 &&
               cb is > 75f and < 135f && cr is > 130f and < 180f;
    }
}

internal sealed class ReliefBodyAnalyzer : IDisposable
{
    public const string ModelFileName = "modnet-photographic.onnx";
    private const int MaximumInputSide = 512;
    private readonly string _modelPath;
    private readonly object _sessionLock = new();
    private InferenceSession? _session;

    public ReliefBodyAnalyzer(string modelPath) => _modelPath = modelPath;
    public bool IsModelAvailable => File.Exists(_modelPath);

    public ReliefBodyAnalysis Analyze(
        SKBitmap source,
        ReliefPortraitAnalysis? portrait,
        CancellationToken cancellationToken)
    {
        var scale = Math.Min(1f, MaximumInputSide / (float)Math.Max(source.Width, source.Height));
        var inputWidth = Math.Max(32, ((int)MathF.Round(source.Width * scale) + 31) / 32 * 32);
        var inputHeight = Math.Max(32, ((int)MathF.Round(source.Height * scale) + 31) / 32 * 32);
        using var resized = new SKBitmap(inputWidth, inputHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(resized))
            canvas.DrawBitmap(source, new SKRect(0, 0, inputWidth, inputHeight));
        var tensor = new DenseTensor<float>([1, 3, inputHeight, inputWidth]);
        var pixels = resized.Pixels;
        for (var y = 0; y < inputHeight; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < inputWidth; x++)
            {
                var color = pixels[y * inputWidth + x];
                tensor[0, 0, y, x] = color.Red / 127.5f - 1f;
                tensor[0, 1, y, x] = color.Green / 127.5f - 1f;
                tensor[0, 2, y, x] = color.Blue / 127.5f - 1f;
            }
        }

        float[] compactMask;
        int outputWidth;
        int outputHeight;
        try
        {
            lock (_sessionLock)
            {
                var session = GetOrCreateSession();
                var input = NamedOnnxValue.CreateFromTensor(session.InputMetadata.Keys.Single(), tensor);
                using var runOptions = new RunOptions();
                using var registration = cancellationToken.Register(() => runOptions.Terminate = true);
                using var outputs = session.Run([input], session.OutputMetadata.Keys.ToArray(), runOptions);
                var output = outputs.First().AsTensor<float>();
                var dimensions = output.Dimensions.ToArray();
                if (dimensions.Length != 4 || dimensions[0] != 1 || dimensions[1] != 1)
                    throw new InvalidDataException($"全身分析輸出維度不受支援：{string.Join('x', dimensions)}");
                outputHeight = dimensions[2];
                outputWidth = dimensions[3];
                compactMask = output.ToArray();
            }
        }
        catch (OnnxRuntimeException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        var mask = ResizeMask(compactMask, outputWidth, outputHeight, source.Width, source.Height, cancellationToken);
        KeepPrimaryComponent(mask, source.Width, source.Height, portrait, cancellationToken);
        return new ReliefBodyAnalysis(source.Width, source.Height, mask, FindBounds(mask, source.Width, source.Height));
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
        if (!IsModelAvailable) throw new FileNotFoundException("找不到離線全身人物分析模型。", _modelPath);
        if (_session is not null) return _session;
        using var options = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
            InterOpNumThreads = 1,
            IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount / 2)
        };
        _session = new InferenceSession(_modelPath, options);
        return _session;
    }

    private static float[] ResizeMask(float[] source, int sourceWidth, int sourceHeight, int width, int height, CancellationToken token)
    {
        var output = new float[width * height];
        for (var y = 0; y < height; y++)
        {
            token.ThrowIfCancellationRequested();
            var sourceY = Math.Clamp((y + 0.5f) * sourceHeight / height - 0.5f, 0f, sourceHeight - 1f);
            var y0 = (int)sourceY;
            var y1 = Math.Min(y0 + 1, sourceHeight - 1);
            var fy = sourceY - y0;
            for (var x = 0; x < width; x++)
            {
                var sourceX = Math.Clamp((x + 0.5f) * sourceWidth / width - 0.5f, 0f, sourceWidth - 1f);
                var x0 = (int)sourceX;
                var x1 = Math.Min(x0 + 1, sourceWidth - 1);
                var fx = sourceX - x0;
                var top = source[y0 * sourceWidth + x0] * (1f - fx) + source[y0 * sourceWidth + x1] * fx;
                var bottom = source[y1 * sourceWidth + x0] * (1f - fx) + source[y1 * sourceWidth + x1] * fx;
                output[y * width + x] = Math.Clamp(top * (1f - fy) + bottom * fy, 0f, 1f);
            }
        }
        return output;
    }

    internal static void KeepPrimaryComponentForRefinement(float[] mask, int width, int height, ReliefPortraitAnalysis? portrait, CancellationToken token) =>
        KeepPrimaryComponent(mask, width, height, portrait, token);

    internal static SKRectI? FindBoundsForRefinement(float[] mask, int width, int height) => FindBounds(mask, width, height);

    private static void KeepPrimaryComponent(float[] mask, int width, int height, ReliefPortraitAnalysis? portrait, CancellationToken token)
    {
        var labels = new int[mask.Length];
        var queue = new int[mask.Length];
        var component = 0;
        var bestComponent = 0;
        var bestScore = -1d;
        var faceCenter = portrait?.FaceBounds is SKRectI face ? new SKPoint(face.MidX, face.MidY) : (SKPoint?)null;
        for (var start = 0; start < mask.Length; start++)
        {
            if (mask[start] < 0.20f || labels[start] != 0) continue;
            component++;
            var head = 0;
            var tail = 0;
            queue[tail++] = start;
            labels[start] = component;
            var count = 0;
            var containsFace = false;
            while (head < tail)
            {
                if ((head & 0xFFFF) == 0) token.ThrowIfCancellationRequested();
                var index = queue[head++];
                count++;
                var x = index % width;
                var y = index / width;
                if (faceCenter is SKPoint point && Math.Abs(x - point.X) <= Math.Max(3, width / 100) &&
                    Math.Abs(y - point.Y) <= Math.Max(3, height / 100)) containsFace = true;
                Add(index - 1, x > 0);
                Add(index + 1, x + 1 < width);
                Add(index - width, y > 0);
                Add(index + width, y + 1 < height);
            }
            var score = count * (containsFace ? 1000d : 1d);
            if (score > bestScore) (bestScore, bestComponent) = (score, component);

            void Add(int neighbor, bool valid)
            {
                if (!valid || mask[neighbor] < 0.20f || labels[neighbor] != 0) return;
                labels[neighbor] = component;
                queue[tail++] = neighbor;
            }
        }
        for (var index = 0; index < mask.Length; index++)
            if (labels[index] != bestComponent) mask[index] = 0f;
    }

    private static SKRectI? FindBounds(float[] mask, int width, int height)
    {
        var left = width; var top = height; var right = -1; var bottom = -1;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            if (mask[y * width + x] < 0.20f) continue;
            left = Math.Min(left, x); top = Math.Min(top, y);
            right = Math.Max(right, x); bottom = Math.Max(bottom, y);
        }
        return right >= left ? new SKRectI(left, top, right + 1, bottom + 1) : null;
    }
}
