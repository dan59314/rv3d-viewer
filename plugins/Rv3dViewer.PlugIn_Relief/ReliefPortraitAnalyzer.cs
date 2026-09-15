using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace Rv3dViewer.ReliefPlugin;

internal sealed record ReliefPortraitAnalysis(
    int Width,
    int Height,
    byte[] Labels,
    SKRectI? FaceBounds,
    IReadOnlyDictionary<string, SKPoint> Features)
{
    public ReliefPortraitAnalysis Copy() => new(
        Width,
        Height,
        (byte[])Labels.Clone(),
        FaceBounds,
        new Dictionary<string, SKPoint>(Features));

    public SKBitmap CreateOverlay(SKBitmap source, CancellationToken cancellationToken)
    {
        var overlayPixels = new SKColor[Labels.Length];
        for (var index = 0; index < Labels.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            overlayPixels[index] = SemanticColor(Labels[index]);
        }
        using var overlay = new SKBitmap(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        overlay.Pixels = overlayPixels;
        var output = source.Copy();
        using var canvas = new SKCanvas(output);
        using var overlayPaint = new SKPaint { Color = SKColors.White.WithAlpha(105), BlendMode = SKBlendMode.SrcOver };
        canvas.DrawBitmap(overlay, 0, 0, overlayPaint);
        if (FaceBounds is { } bounds)
        {
            using var boxPaint = new SKPaint
            {
                Color = new SKColor(0, 235, 255),
                IsStroke = true,
                StrokeWidth = Math.Max(2f, Math.Min(Width, Height) / 250f),
                IsAntialias = true
            };
            canvas.DrawRect(bounds, boxPaint);
        }
        using var pointPaint = new SKPaint { Color = SKColors.Yellow, IsAntialias = true };
        var radius = Math.Max(3f, Math.Min(Width, Height) / 180f);
        foreach (var point in Features.Values) canvas.DrawCircle(point, radius, pointPaint);
        canvas.Flush();
        return output;
    }

    private static SKColor SemanticColor(byte label) => label switch
    {
        1 => new SKColor(255, 170, 120, 210),
        2 or 3 => new SKColor(255, 220, 0, 230),
        4 or 5 => new SKColor(40, 220, 255, 230),
        6 => new SKColor(220, 40, 255, 235),
        7 or 8 or 9 => new SKColor(255, 120, 40, 220),
        10 => new SKColor(255, 50, 50, 235),
        11 or 12 or 13 => new SKColor(255, 40, 150, 235),
        14 or 15 => new SKColor(80, 255, 130, 210),
        16 => new SKColor(60, 120, 255, 200),
        17 => new SKColor(120, 70, 255, 210),
        18 => new SKColor(40, 200, 100, 210),
        _ => SKColors.Transparent
    };
}

internal sealed class ReliefPortraitAnalyzer : IDisposable
{
    internal const string ModelFileName = "face-parsing-resnet18.onnx";
    private const int InputSize = 512;
    private static readonly float[] ImageMean = [0.485f, 0.456f, 0.406f];
    private static readonly float[] ImageStandardDeviation = [0.229f, 0.224f, 0.225f];
    private readonly string _modelPath;
    private readonly object _sessionLock = new();
    private InferenceSession? _session;

    public ReliefPortraitAnalyzer(string modelPath) => _modelPath = modelPath;

    public bool IsModelAvailable => File.Exists(_modelPath);

    public ReliefPortraitAnalysis Analyze(SKBitmap source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var resized = Resize(source);
        var tensor = CreateInputTensor(resized, cancellationToken);
        float[] outputValues;
        int[] dimensions;
        try
        {
            lock (_sessionLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var session = GetOrCreateSession();
                var inputName = session.InputMetadata.Keys.Single();
                var input = NamedOnnxValue.CreateFromTensor(inputName, tensor);
                using var runOptions = new RunOptions();
                using var registration = cancellationToken.Register(() => runOptions.Terminate = true);
                using var outputs = session.Run([input], session.OutputMetadata.Keys.ToArray(), runOptions);
                cancellationToken.ThrowIfCancellationRequested();
                var output = outputs.First().AsTensor<float>();
                dimensions = output.Dimensions.ToArray();
                outputValues = output.ToArray();
            }
        }
        catch (OnnxRuntimeException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (dimensions.Length != 4 || dimensions[0] != 1 || dimensions[1] != 19)
            throw new InvalidDataException($"人像分析輸出維度不受支援：{string.Join('x', dimensions)}");
        var outputHeight = dimensions[2];
        var outputWidth = dimensions[3];
        var compactLabels = ArgMax(outputValues, outputWidth, outputHeight, cancellationToken);
        var labels = ResizeLabels(compactLabels, outputWidth, outputHeight, source.Width, source.Height, cancellationToken);
        return BuildAnalysis(source.Width, source.Height, labels, cancellationToken);
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
        if (!IsModelAvailable) throw new FileNotFoundException("找不到離線人像分析模型。", _modelPath);
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

    private static SKBitmap Resize(SKBitmap source)
    {
        var target = new SKBitmap(InputSize, InputSize, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(target);
        canvas.DrawBitmap(source, new SKRect(0f, 0f, InputSize, InputSize));
        canvas.Flush();
        return target;
    }

    private static DenseTensor<float> CreateInputTensor(SKBitmap bitmap, CancellationToken cancellationToken)
    {
        var tensor = new DenseTensor<float>([1, 3, InputSize, InputSize]);
        var pixels = bitmap.Pixels;
        for (var y = 0; y < InputSize; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < InputSize; x++)
            {
                var color = pixels[y * InputSize + x];
                tensor[0, 0, y, x] = (color.Red / 255f - ImageMean[0]) / ImageStandardDeviation[0];
                tensor[0, 1, y, x] = (color.Green / 255f - ImageMean[1]) / ImageStandardDeviation[1];
                tensor[0, 2, y, x] = (color.Blue / 255f - ImageMean[2]) / ImageStandardDeviation[2];
            }
        }
        return tensor;
    }

    private static byte[] ArgMax(float[] values, int width, int height, CancellationToken cancellationToken)
    {
        var labels = new byte[width * height];
        var planeSize = width * height;
        for (var index = 0; index < planeSize; index++)
        {
            if ((index & 0xFFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var bestClass = 0;
            var bestValue = values[index];
            for (var classIndex = 1; classIndex < 19; classIndex++)
            {
                var value = values[classIndex * planeSize + index];
                if (value <= bestValue) continue;
                bestValue = value;
                bestClass = classIndex;
            }
            labels[index] = (byte)bestClass;
        }
        return labels;
    }

    private static byte[] ResizeLabels(
        byte[] labels,
        int sourceWidth,
        int sourceHeight,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        var output = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceY = Math.Min(sourceHeight - 1, (int)((long)y * sourceHeight / height));
            for (var x = 0; x < width; x++)
            {
                var sourceX = Math.Min(sourceWidth - 1, (int)((long)x * sourceWidth / width));
                output[y * width + x] = labels[sourceY * sourceWidth + sourceX];
            }
        }
        return output;
    }

    private static ReliefPortraitAnalysis BuildAnalysis(
        int width,
        int height,
        byte[] labels,
        CancellationToken cancellationToken)
    {
        var dominantRegion = FindDominantFaceRegion(width, height, labels, cancellationToken);
        if (dominantRegion is null)
            return new ReliefPortraitAnalysis(width, height, new byte[labels.Length], null, new Dictionary<string, SKPoint>());
        FilterToDominantPortrait(width, height, labels, dominantRegion.Value, cancellationToken);
        RefineFaceBoundary(width, height, labels, cancellationToken);
        var minimumX = width;
        var minimumY = height;
        var maximumX = -1;
        var maximumY = -1;
        for (var y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < width; x++)
            {
                var label = labels[y * width + x];
                if (label is 0 or 15 or 16) continue;
                minimumX = Math.Min(minimumX, x);
                minimumY = Math.Min(minimumY, y);
                maximumX = Math.Max(maximumX, x);
                maximumY = Math.Max(maximumY, y);
            }
        }
        SKRectI? bounds = maximumX >= minimumX && maximumY >= minimumY
            ? new SKRectI(minimumX, minimumY, maximumX + 1, maximumY + 1)
            : null;
        var features = new Dictionary<string, SKPoint>();
        AddFeature("左眉", [2]);
        AddFeature("右眉", [3]);
        AddFeature("左眼", [4]);
        AddFeature("右眼", [5]);
        AddFeature("鼻子", [10]);
        AddFeature("嘴巴", [11, 12, 13]);
        AddFeature("左耳", [7]);
        AddFeature("右耳", [8]);
        var skinBounds = GetClassBounds(1);
        if (skinBounds is { } face)
        {
            AddFallback("左眼", "左眉", 0.07f);
            AddFallback("右眼", "右眉", 0.07f);
            features.TryAdd("鼻子", new SKPoint(face.MidX, face.Top + face.Height * 0.52f));
            features.TryAdd("嘴巴", new SKPoint(face.MidX, face.Top + face.Height * 0.72f));
            features.TryAdd("下巴", new SKPoint(face.MidX, face.Bottom - face.Height * 0.03f));
            StabilizeFacialLayout(face, features);
        }
        return new ReliefPortraitAnalysis(width, height, labels, bounds, features);

        void AddFeature(string name, byte[] classes)
        {
            long xSum = 0;
            long ySum = 0;
            var count = 0;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (!classes.Contains(labels[y * width + x])) continue;
                xSum += x;
                ySum += y;
                count++;
            }
            if (count > Math.Max(4, width * height / 200000))
                features[name] = new SKPoint(xSum / (float)count, ySum / (float)count);
        }

        SKRectI? GetClassBounds(byte targetClass)
        {
            var left = width;
            var top = height;
            var right = -1;
            var bottom = -1;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (labels[y * width + x] != targetClass) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
            return right >= left && bottom >= top
                ? new SKRectI(left, top, right + 1, bottom + 1)
                : null;
        }

        void AddFallback(string featureName, string browName, float verticalOffset)
        {
            if (features.ContainsKey(featureName) || !features.TryGetValue(browName, out var brow) || skinBounds is null)
                return;
            features[featureName] = new SKPoint(brow.X, brow.Y + skinBounds.Value.Height * verticalOffset);
        }

        static void StabilizeFacialLayout(SKRectI face, Dictionary<string, SKPoint> points)
        {
            var expectedLeftEye = new SKPoint(face.Left + face.Width * 0.31f, face.Top + face.Height * 0.39f);
            var expectedRightEye = new SKPoint(face.Left + face.Width * 0.69f, face.Top + face.Height * 0.39f);
            var leftEye = points.GetValueOrDefault("左眼", expectedLeftEye);
            var rightEye = points.GetValueOrDefault("右眼", expectedRightEye);
            if (leftEye.X > rightEye.X) (leftEye, rightEye) = (rightEye, leftEye);
            var eyeDistance = rightEye.X - leftEye.X;
            if (eyeDistance < face.Width * 0.20f || eyeDistance > face.Width * 0.65f)
                (leftEye, rightEye) = (expectedLeftEye, expectedRightEye);
            points["左眼"] = leftEye;
            points["右眼"] = rightEye;
            var eyeMidX = (leftEye.X + rightEye.X) * 0.5f;
            var nose = points.GetValueOrDefault("鼻子", new SKPoint(eyeMidX, face.Top + face.Height * 0.57f));
            points["鼻子"] = new SKPoint(nose.X * 0.35f + eyeMidX * 0.65f, nose.Y);
            var mouth = points.GetValueOrDefault("嘴巴", new SKPoint(eyeMidX, face.Top + face.Height * 0.74f));
            points["嘴巴"] = new SKPoint(mouth.X * 0.45f + points["鼻子"].X * 0.55f, mouth.Y);
        }
    }

    private static void RefineFaceBoundary(int width, int height, byte[] labels, CancellationToken cancellationToken)
    {
        // 僅清理皮膚／背景交界；五官、眼鏡、頭髮與衣服標籤保持原樣。
        for (var pass = 0; pass < 2; pass++)
        {
            var source = (byte[])labels.Clone();
            for (var y = 1; y < height - 1; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (var x = 1; x < width - 1; x++)
                {
                    var index = y * width + x;
                    if (source[index] is > 1 and <= 18) continue;
                    var faceNeighbors = 0;
                    for (var offsetY = -1; offsetY <= 1; offsetY++)
                    for (var offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0) continue;
                        if (IsFacialLabel(source[(y + offsetY) * width + x + offsetX])) faceNeighbors++;
                    }
                    if (source[index] == 0 && faceNeighbors >= 6)
                        labels[index] = 1;
                    else if (source[index] == 1 && faceNeighbors <= 1)
                        labels[index] = 0;
                }
            }
        }
    }

    private static bool IsFacialLabel(byte label) => label is >= 1 and <= 13;

    private static SKRectI? FindDominantFaceRegion(
        int width,
        int height,
        byte[] labels,
        CancellationToken cancellationToken)
    {
        var visited = new bool[labels.Length];
        var queue = new int[labels.Length];
        var largestCount = 0;
        var largestBounds = new SKRectI();
        for (var start = 0; start < labels.Length; start++)
        {
            if ((start & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            if (visited[start] || !IsFaceClass(labels[start])) continue;
            var head = 0;
            var tail = 0;
            queue[tail++] = start;
            visited[start] = true;
            var minimumX = width;
            var minimumY = height;
            var maximumX = -1;
            var maximumY = -1;
            while (head < tail)
            {
                var index = queue[head++];
                var x = index % width;
                var y = index / width;
                minimumX = Math.Min(minimumX, x);
                minimumY = Math.Min(minimumY, y);
                maximumX = Math.Max(maximumX, x);
                maximumY = Math.Max(maximumY, y);
                Visit(index - 1, x > 0);
                Visit(index + 1, x + 1 < width);
                Visit(index - width, y > 0);
                Visit(index + width, y + 1 < height);
            }
            if (tail <= largestCount) continue;
            largestCount = tail;
            largestBounds = new SKRectI(minimumX, minimumY, maximumX + 1, maximumY + 1);

            void Visit(int index, bool valid)
            {
                if (!valid || visited[index] || !IsFaceClass(labels[index])) return;
                visited[index] = true;
                queue[tail++] = index;
            }
        }

        if (largestCount < width * height / 500) return null;
        var horizontalPadding = Math.Max(8, (int)(largestBounds.Width * 0.32f));
        var topPadding = Math.Max(8, (int)(largestBounds.Height * 0.35f));
        var bottomPadding = Math.Max(8, (int)(largestBounds.Height * 0.20f));
        return new SKRectI(
            Math.Max(0, largestBounds.Left - horizontalPadding),
            Math.Max(0, largestBounds.Top - topPadding),
            Math.Min(width, largestBounds.Right + horizontalPadding),
            Math.Min(height, largestBounds.Bottom + bottomPadding));
    }

    private static void FilterToDominantPortrait(
        int width,
        int height,
        byte[] labels,
        SKRectI faceRegion,
        CancellationToken cancellationToken)
    {
        var personPadding = faceRegion.Width / 4;
        var personLeft = Math.Max(0, faceRegion.Left - personPadding);
        var personRight = Math.Min(width, faceRegion.Right + personPadding);
        for (var y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                var label = labels[index];
                var inFace = faceRegion.Contains(x, y);
                var inLowerPerson = label is 14 or 15 or 16 &&
                    y >= faceRegion.Top && x >= personLeft && x < personRight;
                if (!inFace && !inLowerPerson) labels[index] = 0;
            }
        }
    }

    private static bool IsFaceClass(byte label) => label is >= 1 and <= 14 or 17 or 18;
}
