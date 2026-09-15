using SkiaSharp;

namespace Rv3dViewer.ReliefPlugin;

internal sealed class ReliefDepthMap
{
    public ReliefDepthMap(int width, int height, float[] values)
    {
        if (width <= 0 || height <= 0 || values.Length != width * height)
            throw new ArgumentException("浮點深度圖尺寸無效。", nameof(values));
        Width = width;
        Height = height;
        Values = values;
    }

    public int Width { get; }
    public int Height { get; }
    public float[] Values { get; }

    public ReliefDepthMap Copy() => new(Width, Height, (float[])Values.Clone());

    public static ReliefDepthMap FromBitmap(SKBitmap bitmap, CancellationToken cancellationToken)
    {
        var pixels = bitmap.Pixels;
        var values = new float[pixels.Length];
        for (var index = 0; index < values.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var color = pixels[index];
            values[index] = (color.Red * 0.2126f + color.Green * 0.7152f + color.Blue * 0.0722f) / 255f;
        }
        return new ReliefDepthMap(bitmap.Width, bitmap.Height, values);
    }

    public static ReliefDepthMap FromRawDepth(
        float[] depth,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        var finite = depth.Where(float.IsFinite).ToArray();
        if (finite.Length == 0) throw new InvalidDataException("AI 深度模型未產生有效數值。");
        Array.Sort(finite);
        var lower = Percentile(finite, 0.01f);
        var upper = Percentile(finite, 0.99f);
        if (upper <= lower)
        {
            lower = finite[0];
            upper = finite[^1];
        }
        var range = Math.Max(upper - lower, float.Epsilon);
        var values = new float[depth.Length];
        for (var index = 0; index < values.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var value = float.IsFinite(depth[index]) ? depth[index] : lower;
            values[index] = Math.Clamp((value - lower) / range, 0f, 1f);
        }
        return new ReliefDepthMap(width, height, values);
    }

    public ReliefDepthMap ResizeBicubic(int width, int height, CancellationToken cancellationToken)
    {
        if (width == Width && height == Height) return Copy();
        var output = new float[width * height];
        var scaleX = Width / (float)width;
        var scaleY = Height / (float)height;
        Span<float> rows = stackalloc float[4];
        for (var y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceY = (y + 0.5f) * scaleY - 0.5f;
            var yBase = (int)MathF.Floor(sourceY);
            var yFraction = sourceY - yBase;
            for (var x = 0; x < width; x++)
            {
                var sourceX = (x + 0.5f) * scaleX - 0.5f;
                var xBase = (int)MathF.Floor(sourceX);
                var xFraction = sourceX - xBase;
                for (var sampleY = -1; sampleY <= 2; sampleY++)
                {
                    var row = Math.Clamp(yBase + sampleY, 0, Height - 1);
                    rows[sampleY + 1] = Cubic(
                        Values[row * Width + Math.Clamp(xBase - 1, 0, Width - 1)],
                        Values[row * Width + Math.Clamp(xBase, 0, Width - 1)],
                        Values[row * Width + Math.Clamp(xBase + 1, 0, Width - 1)],
                        Values[row * Width + Math.Clamp(xBase + 2, 0, Width - 1)],
                        xFraction);
                }
                output[y * width + x] = Math.Clamp(Cubic(rows[0], rows[1], rows[2], rows[3], yFraction), 0f, 1f);
            }
        }
        return new ReliefDepthMap(width, height, output);
    }

    public static ReliefDepthMap Blend(
        ReliefDepthMap imageProcessing,
        ReliefDepthMap aiDepth,
        float aiWeight,
        CancellationToken cancellationToken)
    {
        if (imageProcessing.Width != aiDepth.Width || imageProcessing.Height != aiDepth.Height)
            throw new ArgumentException("AI 深度與影像處理結果的尺寸必須相同。");
        aiWeight = Math.Clamp(aiWeight, 0f, 1f);
        var output = new float[imageProcessing.Values.Length];
        for (var index = 0; index < output.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            output[index] = imageProcessing.Values[index] * (1f - aiWeight) + aiDepth.Values[index] * aiWeight;
        }
        return new ReliefDepthMap(imageProcessing.Width, imageProcessing.Height, output);
    }

    public static ReliefDepthMap CreateEdgeMap(
        SKBitmap bitmap, float thresholdPercent, float strengthPercent, int smoothingRadius,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        var width = bitmap.Width;
        var height = bitmap.Height;
        if (width <= 0 || height <= 0)
            throw new ArgumentException("影像尺寸無效。", nameof(bitmap));

        var luminance = CreateGuidanceLuminance(bitmap, cancellationToken);
        var smoothed = BoxMean(luminance, width, height, Math.Clamp(smoothingRadius, 0, 5), cancellationToken);
        var magnitude = new float[smoothed.Length];
        var maximum = 0f;
        for (var y = 1; y < height - 1; y++)
        {
            if ((y & 0xFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            for (var x = 1; x < width - 1; x++)
            {
                var top = (y - 1) * width;
                var middle = y * width;
                var bottom = (y + 1) * width;
                var gx = -smoothed[top + x - 1] + smoothed[top + x + 1]
                         -2f * smoothed[middle + x - 1] + 2f * smoothed[middle + x + 1]
                         -smoothed[bottom + x - 1] + smoothed[bottom + x + 1];
                var gy = -smoothed[top + x - 1] - 2f * smoothed[top + x] - smoothed[top + x + 1]
                         +smoothed[bottom + x - 1] + 2f * smoothed[bottom + x] + smoothed[bottom + x + 1];
                var value = MathF.Sqrt(gx * gx + gy * gy);
                magnitude[middle + x] = value;
                maximum = MathF.Max(maximum, value);
            }
        }

        if (maximum > 1e-6f)
        {
            var inverseMaximum = 1f / maximum;
            for (var index = 0; index < magnitude.Length; index++)
            {
                var normalized = Math.Clamp(magnitude[index] * inverseMaximum, 0f, 1f);
                var threshold = Math.Clamp(thresholdPercent / 100f, 0f, 0.95f);
                normalized = Math.Clamp((normalized - threshold) / Math.Max(0.05f, 1f - threshold), 0f, 1f);
                magnitude[index] = Math.Clamp(MathF.Sqrt(normalized) * strengthPercent / 100f, 0f, 1f);
            }
        }
        return new ReliefDepthMap(width, height, BoxMean(magnitude, width, height, 1, cancellationToken));
    }

    public ReliefDepthMap RefineWithGuidance(
        SKBitmap guidance,
        CancellationToken cancellationToken)
    {
        if (guidance.Width != Width || guidance.Height != Height)
            throw new ArgumentException("引導影像與 AI 深度的尺寸必須相同。", nameof(guidance));

        // Guided filtering smooths interpolation artifacts while retaining depth changes
        // that agree with important boundaries in the original image.
        var radius = Math.Clamp(Math.Min(Width, Height) / 160, 3, 8);
        const float epsilon = 0.0025f;
        const float refinementAmount = 0.85f;
        var guide = CreateGuidanceLuminance(guidance, cancellationToken);
        var guideSquared = new float[Values.Length];
        var guideDepth = new float[Values.Length];
        for (var index = 0; index < Values.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            guideSquared[index] = guide[index] * guide[index];
            guideDepth[index] = guide[index] * Values[index];
        }

        var meanGuide = BoxMean(guide, Width, Height, radius, cancellationToken);
        var meanDepth = BoxMean(Values, Width, Height, radius, cancellationToken);
        var correlationGuide = BoxMean(guideSquared, Width, Height, radius, cancellationToken);
        var correlationGuideDepth = BoxMean(guideDepth, Width, Height, radius, cancellationToken);
        var coefficientA = new float[Values.Length];
        var coefficientB = new float[Values.Length];
        for (var index = 0; index < Values.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var variance = Math.Max(0f, correlationGuide[index] - meanGuide[index] * meanGuide[index]);
            var covariance = correlationGuideDepth[index] - meanGuide[index] * meanDepth[index];
            coefficientA[index] = covariance / (variance + epsilon);
            coefficientB[index] = meanDepth[index] - coefficientA[index] * meanGuide[index];
        }

        var meanA = BoxMean(coefficientA, Width, Height, radius, cancellationToken);
        var meanB = BoxMean(coefficientB, Width, Height, radius, cancellationToken);
        var output = new float[Values.Length];
        for (var index = 0; index < Values.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var refined = meanA[index] * guide[index] + meanB[index];
            output[index] = Math.Clamp(
                Values[index] + (refined - Values[index]) * refinementAmount,
                0f,
                1f);
        }
        return new ReliefDepthMap(Width, Height, output);
    }

    public ReliefDepthMap ApplyPostProcessing(
        ReliefImageProcessingSettings settings,
        CancellationToken cancellationToken)
    {
        var output = (float[])Values.Clone();
        if (settings.Invert)
            for (var index = 0; index < output.Length; index++) output[index] = 1f - output[index];
        if (settings.Symmetry) ApplySymmetry(output, Width, Height, settings.SymmetryAxisPercent, cancellationToken);
        if (settings.ReduceColors) Posterize(output, settings.ColorLevels, cancellationToken);
        return new ReliefDepthMap(Width, Height, output);
    }

    public ReliefDepthMap ApplyDepthShaping(
        float curvePercent,
        float localDetailPercent,
        CancellationToken cancellationToken)
    {
        var exponent = Math.Clamp(curvePercent, 25f, 400f) / 100f;
        var detailStrength = Math.Clamp(localDetailPercent, 0f, 100f) / 100f;
        var curved = new float[Values.Length];
        for (var index = 0; index < Values.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            curved[index] = MathF.Pow(Math.Clamp(Values[index], 0f, 1f), exponent);
        }

        if (detailStrength <= float.Epsilon)
            return new ReliefDepthMap(Width, Height, curved);

        var radius = Math.Clamp(Math.Min(Width, Height) / 240, 2, 6);
        var localMean = BoxMean(curved, Width, Height, radius, cancellationToken);
        var output = new float[curved.Length];
        var maximumAdjustment = 0.08f * detailStrength;
        for (var index = 0; index < output.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var adjustment = Math.Clamp(
                (curved[index] - localMean[index]) * detailStrength,
                -maximumAdjustment,
                maximumAdjustment);
            output[index] = Math.Clamp(curved[index] + adjustment, 0f, 1f);
        }
        return new ReliefDepthMap(Width, Height, output);
    }

    public ReliefDepthMap ApplyPortraitGeometry(
        SKBitmap source,
        float strengthPercent,
        CancellationToken cancellationToken)
    {
        if (source.Width != Width || source.Height != Height)
            throw new ArgumentException("人像來源與深度圖尺寸必須相同。", nameof(source));
        var strength = Math.Clamp(strengthPercent, 0f, 100f) / 100f;
        if (strength <= float.Epsilon) return Copy();

        var pixels = source.Pixels;
        double weightSum = 0d;
        double xSum = 0d;
        double ySum = 0d;
        double xSquaredSum = 0d;
        double ySquaredSum = 0d;
        for (var y = 0; y < Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var normalizedY = y / Math.Max(1f, Height - 1f);
            if (normalizedY > 0.82f) continue;
            for (var x = 0; x < Width; x++)
            {
                var normalizedX = x / Math.Max(1f, Width - 1f);
                var centrality = Math.Clamp(1f - Math.Abs(normalizedX - 0.5f) / 0.48f, 0f, 1f);
                var skin = SkinLikelihood(pixels[y * Width + x]);
                if (skin <= 0f || centrality <= 0f) continue;
                var weight = skin * centrality * (0.45f + Values[y * Width + x] * 0.55f);
                weightSum += weight;
                xSum += x * weight;
                ySum += y * weight;
                xSquaredSum += x * x * weight;
                ySquaredSum += y * y * weight;
            }
        }

        if (weightSum < Width * Height * 0.005d) return Copy();
        var centerX = (float)(xSum / weightSum);
        var centerY = (float)(ySum / weightSum);
        var deviationX = MathF.Sqrt(Math.Max(0f, (float)(xSquaredSum / weightSum) - centerX * centerX));
        var deviationY = MathF.Sqrt(Math.Max(0f, (float)(ySquaredSum / weightSum) - centerY * centerY));
        var faceWidth = Math.Clamp(deviationX * 4f, Width * 0.16f, Width * 0.50f);
        var faceHeight = Math.Clamp(deviationY * 4f, Height * 0.20f, Height * 0.65f);
        var aspect = faceWidth / faceHeight;
        if (aspect is < 0.45f or > 1.15f) return Copy();

        var output = (float[])Values.Clone();
        var halfWidth = faceWidth * 0.5f;
        var halfHeight = faceHeight * 0.5f;
        var minimumX = Math.Max(0, (int)MathF.Floor(centerX - halfWidth));
        var maximumX = Math.Min(Width - 1, (int)MathF.Ceiling(centerX + halfWidth));
        var minimumY = Math.Max(0, (int)MathF.Floor(centerY - halfHeight));
        var maximumY = Math.Min(Height - 1, (int)MathF.Ceiling(centerY + halfHeight));
        for (var y = minimumY; y <= maximumY; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var faceY = (y - centerY) / halfHeight;
            for (var x = minimumX; x <= maximumX; x++)
            {
                var faceX = (x - centerX) / halfWidth;
                var radiusSquared = faceX * faceX + faceY * faceY;
                if (radiusSquared >= 1f) continue;
                var mask = SmoothStep(1f - radiusSquared);
                var dome = MathF.Sqrt(1f - radiusSquared) * 0.045f;
                var nose = Gaussian(faceX, faceY + 0.02f, 0.17f, 0.25f) * 0.065f;
                var cheeks = (Gaussian(faceX - 0.32f, faceY - 0.05f, 0.24f, 0.22f) +
                              Gaussian(faceX + 0.32f, faceY - 0.05f, 0.24f, 0.22f)) * 0.012f;
                var eyeSockets = (Gaussian(faceX - 0.25f, faceY + 0.24f, 0.18f, 0.12f) +
                                  Gaussian(faceX + 0.25f, faceY + 0.24f, 0.18f, 0.12f)) * -0.010f;
                var chin = Gaussian(faceX, faceY - 0.48f, 0.28f, 0.16f) * 0.012f;
                var correction = (dome + nose + cheeks + eyeSockets + chin) * mask * strength;
                var index = y * Width + x;
                output[index] = Math.Clamp(output[index] + correction, 0f, 1f);
            }
        }
        return new ReliefDepthMap(Width, Height, output);
    }

    public ReliefDepthMap ApplyPortraitGeometry(
        SKBitmap source,
        ReliefPortraitAnalysis? analysis,
        float strengthPercent,
        CancellationToken cancellationToken)
    {
        if (analysis is null || analysis.Width != Width || analysis.Height != Height ||
            !TryGetLabelBounds(analysis.Labels, 1, Width, Height, out var skinBounds))
            return ApplyPortraitGeometry(source, strengthPercent, cancellationToken);
        var strength = Math.Clamp(strengthPercent, 0f, 100f) / 100f;
        if (strength <= float.Epsilon) return Copy();

        var centerX = skinBounds.MidX;
        var centerY = skinBounds.MidY;
        var halfWidth = Math.Max(1f, skinBounds.Width * 0.53f);
        var halfHeight = Math.Max(1f, skinBounds.Height * 0.54f);
        var leftEye = FeatureOrDefault(analysis, "左眼", new SKPoint(centerX - halfWidth * 0.42f, centerY - halfHeight * 0.28f));
        var rightEye = FeatureOrDefault(analysis, "右眼", new SKPoint(centerX + halfWidth * 0.42f, centerY - halfHeight * 0.28f));
        if (leftEye.X > rightEye.X) (leftEye, rightEye) = (rightEye, leftEye);
        var nose = FeatureOrDefault(analysis, "鼻子", new SKPoint(centerX, centerY));
        var mouth = FeatureOrDefault(analysis, "嘴巴", new SKPoint(centerX, centerY + halfHeight * 0.38f));
        var chin = FeatureOrDefault(analysis, "下巴", new SKPoint(centerX, centerY + halfHeight * 0.82f));
        var eyeAngle = MathF.Atan2(rightEye.Y - leftEye.Y, rightEye.X - leftEye.X);
        var cosine = MathF.Cos(-eyeAngle);
        var sine = MathF.Sin(-eyeAngle);
        var output = (float[])Values.Clone();
        var minimumX = Math.Max(0, (int)MathF.Floor(centerX - halfWidth));
        var maximumX = Math.Min(Width - 1, (int)MathF.Ceiling(centerX + halfWidth));
        var minimumY = Math.Max(0, (int)MathF.Floor(centerY - halfHeight));
        var maximumY = Math.Min(Height - 1, (int)MathF.Ceiling(centerY + halfHeight));

        for (var y = minimumY; y <= maximumY; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = minimumX; x <= maximumX; x++)
            {
                var deltaX = x - centerX;
                var deltaY = y - centerY;
                var faceX = (deltaX * cosine - deltaY * sine) / halfWidth;
                var faceY = (deltaX * sine + deltaY * cosine) / halfHeight;
                var radiusSquared = faceX * faceX + faceY * faceY;
                if (radiusSquared >= 1f) continue;
                var index = y * Width + x;
                var semanticWeight = SemanticGeometryWeight(analysis.Labels[index]);
                if (semanticWeight <= 0f) continue;
                var mask = SmoothStep(1f - radiusSquared) * semanticWeight;
                var dome = MathF.Sqrt(1f - radiusSquared) * 0.075f;
                var forehead = Gaussian(faceX, faceY + 0.55f, 0.45f, 0.30f) * 0.020f;
                var leftCheek = ToFaceCoordinates(leftEye.X - halfWidth * 0.10f, leftEye.Y + halfHeight * 0.32f);
                var rightCheek = ToFaceCoordinates(rightEye.X + halfWidth * 0.10f, rightEye.Y + halfHeight * 0.32f);
                var cheeks = (Gaussian(faceX - leftCheek.X, faceY - leftCheek.Y, 0.25f, 0.25f) +
                              Gaussian(faceX - rightCheek.X, faceY - rightCheek.Y, 0.25f, 0.25f)) * 0.025f;
                var leftEyeFace = ToFaceCoordinates(leftEye.X, leftEye.Y);
                var rightEyeFace = ToFaceCoordinates(rightEye.X, rightEye.Y);
                var eyeSockets = (Gaussian(faceX - leftEyeFace.X, faceY - leftEyeFace.Y, 0.20f, 0.12f) +
                                  Gaussian(faceX - rightEyeFace.X, faceY - rightEyeFace.Y, 0.20f, 0.12f)) * -0.020f;
                var noseFace = ToFaceCoordinates(nose.X, nose.Y);
                var noseBridge = Gaussian(faceX - noseFace.X, faceY - (noseFace.Y - 0.18f), 0.10f, 0.32f) * 0.045f;
                var noseTip = Gaussian(faceX - noseFace.X, faceY - noseFace.Y, 0.15f, 0.16f) * 0.085f;
                var mouthFace = ToFaceCoordinates(mouth.X, mouth.Y);
                var lips = Gaussian(faceX - mouthFace.X, faceY - mouthFace.Y, 0.26f, 0.10f) * 0.020f;
                var philtrum = Gaussian(faceX - mouthFace.X, faceY - (mouthFace.Y - 0.15f), 0.09f, 0.10f) * -0.010f;
                var chinFace = ToFaceCoordinates(chin.X, chin.Y);
                var chinShape = Gaussian(faceX - chinFace.X, faceY - chinFace.Y, 0.28f, 0.18f) * 0.028f;
                var correction = (dome + forehead + cheeks + eyeSockets + noseBridge + noseTip + lips + philtrum + chinShape) * mask * strength;
                output[index] = Math.Clamp(output[index] + correction, 0f, 1f);
            }
        }
        return new ReliefDepthMap(Width, Height, output);

        SKPoint ToFaceCoordinates(float x, float y)
        {
            var dx = x - centerX;
            var dy = y - centerY;
            return new SKPoint(
                (dx * cosine - dy * sine) / halfWidth,
                (dx * sine + dy * cosine) / halfHeight);
        }
    }

    public ReliefDepthMap ApplyPortraitLayers(
        SKBitmap source,
        ReliefPortraitAnalysis? analysis,
        float glassesReliefPercent,
        float hairDetailPercent,
        float backgroundSuppressionPercent,
        bool createBustSilhouette,
        CancellationToken cancellationToken)
    {
        if (analysis is null || analysis.Width != Width || analysis.Height != Height)
            return Copy();
        var glassesStrength = Math.Clamp(glassesReliefPercent, 0f, 100f) / 100f;
        var hairStrength = Math.Clamp(hairDetailPercent, 0f, 100f) / 100f;
        var backgroundStrength = Math.Clamp(backgroundSuppressionPercent, 0f, 100f) / 100f;
        if (glassesStrength <= float.Epsilon && hairStrength <= float.Epsilon &&
            backgroundStrength <= float.Epsilon && !createBustSilhouette)
            return Copy();

        var personMask = new float[Values.Length];
        var hasFaceBounds = TryGetLabelBounds(analysis.Labels, 1, Width, Height, out var layerFaceBounds);
        for (var index = 0; index < personMask.Length; index++)
        {
            var label = analysis.Labels[index];
            if (label == 0) continue;
            if (!createBustSilhouette || !hasFaceBounds || label != 16)
            {
                personMask[index] = 1f;
                continue;
            }
            var x = index % Width;
            var y = index / Width;
            var centerX = layerFaceBounds.MidX;
            var centerY = layerFaceBounds.Bottom + layerFaceBounds.Height * 0.34f;
            var radiusX = Math.Max(1f, layerFaceBounds.Width * 0.98f);
            var radiusY = Math.Max(1f, layerFaceBounds.Height * 0.58f);
            var normalizedX = (x - centerX) / radiusX;
            var normalizedY = (y - centerY) / radiusY;
            if (normalizedX * normalizedX + normalizedY * normalizedY <= 1f)
                personMask[index] = 1f;
        }
        var featherRadius = Math.Clamp(Math.Min(Width, Height) / 140, 3, 10);
        var softPersonMask = BoxMean(personMask, Width, Height, featherRadius, cancellationToken);
        float[]? luminance = null;
        float[]? localLuminance = null;
        if (hairStrength > float.Epsilon)
        {
            luminance = CreateGuidanceLuminance(source, cancellationToken);
            localLuminance = BoxMean(
                luminance,
                Width,
                Height,
                Math.Clamp(Math.Min(Width, Height) / 180, 3, 8),
                cancellationToken);
        }

        var output = (float[])Values.Clone();
        for (var index = 0; index < output.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var label = analysis.Labels[index];
            if (label == 6 && glassesStrength > float.Epsilon)
                output[index] = Math.Clamp(output[index] + 0.035f * glassesStrength, 0f, 1f);
            else if (label == 17 && hairStrength > float.Epsilon)
            {
                var detail = Math.Clamp(
                    (luminance![index] - localLuminance![index]) * 0.32f * hairStrength,
                    -0.028f * hairStrength,
                    0.028f * hairStrength);
                output[index] = Math.Clamp(output[index] + 0.010f * hairStrength + detail, 0f, 1f);
            }

            var backgroundMask = 1f - softPersonMask[index];
            var suppression = createBustSilhouette
                ? Math.Max(backgroundStrength, 0.92f)
                : backgroundStrength;
            if (personMask[index] <= float.Epsilon && suppression > float.Epsilon && backgroundMask > float.Epsilon)
                output[index] = Math.Clamp(
                    output[index] * (1f - 0.96f * suppression * backgroundMask),
                    0f,
                    1f);
        }
        return new ReliefDepthMap(Width, Height, output);
    }

    public ReliefDepthMap ApplySurfaceNormalDetail(
        SKBitmap source,
        ReliefPortraitAnalysis? analysis,
        float strengthPercent,
        CancellationToken cancellationToken)
    {
        var strength = Math.Clamp(strengthPercent, 0f, 100f) / 100f;
        if (strength <= float.Epsilon || analysis is null ||
            analysis.Width != Width || analysis.Height != Height ||
            source.Width != Width || source.Height != Height)
            return Copy();

        var hardMask = new float[Values.Length];
        for (var index = 0; index < hardMask.Length; index++)
            hardMask[index] = IsSurfaceDetailLabel(analysis.Labels[index]) ? 1f : 0f;
        var softMask = BoxMean(
            hardMask,
            Width,
            Height,
            Math.Clamp(Math.Min(Width, Height) / 240, 2, 5),
            cancellationToken);

        var luminance = CreateGuidanceLuminance(source, cancellationToken);
        var illumination = BoxMean(
            luminance,
            Width,
            Height,
            Math.Clamp(Math.Min(Width, Height) / 24, 12, 40),
            cancellationToken);
        var band = new float[Values.Length];
        for (var index = 0; index < band.Length; index++)
            band[index] = (luminance[index] - illumination[index]) * hardMask[index];
        band = BoxMean(band, Width, Height, 2, cancellationToken);

        var gradientX = new float[Values.Length];
        var gradientY = new float[Values.Length];
        for (var y = 1; y < Height - 1; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 1; x < Width - 1; x++)
            {
                var index = y * Width + x;
                if (hardMask[index] <= 0f) continue;
                gradientX[index] = (band[index + 1] - band[index - 1]) * 0.35f;
                gradientY[index] = (band[index + Width] - band[index - Width]) * 0.35f;
            }
        }

        var divergence = new float[Values.Length];
        for (var y = 1; y < Height - 1; y++)
        for (var x = 1; x < Width - 1; x++)
        {
            var index = y * Width + x;
            if (hardMask[index] <= 0f) continue;
            divergence[index] = gradientX[index] - gradientX[index - 1] +
                                gradientY[index] - gradientY[index - Width];
        }

        var current = new float[Values.Length];
        var next = new float[Values.Length];
        for (var iteration = 0; iteration < 36; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Array.Clear(next);
            for (var y = 1; y < Height - 1; y++)
            for (var x = 1; x < Width - 1; x++)
            {
                var index = y * Width + x;
                if (hardMask[index] <= 0f) continue;
                next[index] = (current[index - 1] + current[index + 1] +
                               current[index - Width] + current[index + Width] -
                               divergence[index]) * 0.25f;
            }
            (current, next) = (next, current);
        }

        var samples = new List<float>();
        double sum = 0d;
        var count = 0;
        for (var index = 0; index < current.Length; index++)
        {
            if (hardMask[index] <= 0f) continue;
            sum += current[index];
            count++;
        }
        if (count == 0) return Copy();
        var mean = (float)(sum / count);
        for (var index = 0; index < current.Length; index++)
            if (hardMask[index] > 0f) samples.Add(Math.Abs(current[index] - mean));
        samples.Sort();
        var robustAmplitude = Percentile(samples.ToArray(), 0.99f);
        if (robustAmplitude <= 1e-6f) return Copy();

        var output = (float[])Values.Clone();
        var scale = 0.04f * strength / robustAmplitude;
        for (var index = 0; index < output.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            if (hardMask[index] <= 0f) continue;
            var detail = Math.Clamp((current[index] - mean) * scale, -0.04f * strength, 0.04f * strength);
            output[index] = Math.Clamp(output[index] + detail * softMask[index], 0f, 1f);
        }
        return new ReliefDepthMap(Width, Height, output);
    }

    public ReliefDepthMap ApplyFacialFeatureContours(
        ReliefPortraitAnalysis? analysis,
        float strengthPercent,
        CancellationToken cancellationToken)
    {
        var strength = Math.Clamp(strengthPercent, 0f, 100f) / 100f;
        if (strength <= float.Epsilon || analysis is null ||
            analysis.Width != Width || analysis.Height != Height)
            return Copy();

        var eyeMask = new float[Values.Length];
        var noseMask = new float[Values.Length];
        var lipMask = new float[Values.Length];
        var upperLipMask = new float[Values.Length];
        var lowerLipMask = new float[Values.Length];
        var mouthOpeningMask = new float[Values.Length];
        var faceMask = new float[Values.Length];
        for (var index = 0; index < Values.Length; index++)
        {
            var label = analysis.Labels[index];
            eyeMask[index] = label is 4 or 5 ? 1f : 0f;
            noseMask[index] = label == 10 ? 1f : 0f;
            lipMask[index] = label is 12 or 13 ? 1f : 0f;
            upperLipMask[index] = label == 12 ? 1f : 0f;
            lowerLipMask[index] = label == 13 ? 1f : 0f;
            mouthOpeningMask[index] = label == 11 ? 1f : 0f;
            faceMask[index] = IsSurfaceDetailLabel(label) || label == 6 ? 1f : 0f;
        }

        var minimumDimension = Math.Min(Width, Height);
        var innerRadius = Math.Clamp(minimumDimension / 220, 2, 5);
        var outerRadius = Math.Clamp(minimumDimension / 75, 7, 16);
        var eyesInner = BoxMean(eyeMask, Width, Height, innerRadius, cancellationToken);
        var eyesOuter = BoxMean(eyeMask, Width, Height, outerRadius, cancellationToken);
        var noseInner = BoxMean(noseMask, Width, Height, innerRadius + 1, cancellationToken);
        var noseOuter = BoxMean(noseMask, Width, Height, outerRadius, cancellationToken);
        var lipsInner = BoxMean(lipMask, Width, Height, innerRadius, cancellationToken);
        var lipsOuter = BoxMean(lipMask, Width, Height, outerRadius, cancellationToken);
        var upperLipInner = BoxMean(upperLipMask, Width, Height, innerRadius, cancellationToken);
        var lowerLipInner = BoxMean(lowerLipMask, Width, Height, innerRadius, cancellationToken);
        var mouthOpening = BoxMean(mouthOpeningMask, Width, Height, innerRadius, cancellationToken);

        var bounds = analysis.FaceBounds ?? new SKRectI(0, 0, Width, Height);
        var centerX = bounds.MidX;
        var centerY = bounds.MidY;
        var halfWidth = Math.Max(1f, bounds.Width * 0.5f);
        var halfHeight = Math.Max(1f, bounds.Height * 0.5f);
        var chin = FeatureOrDefault(
            analysis,
            "下巴",
            new SKPoint(centerX, centerY + halfHeight * 0.78f));
        var leftEye = FeatureOrDefault(
            analysis,
            "左眼",
            new SKPoint(centerX - halfWidth * 0.38f, centerY - halfHeight * 0.25f));
        var rightEye = FeatureOrDefault(
            analysis,
            "右眼",
            new SKPoint(centerX + halfWidth * 0.38f, centerY - halfHeight * 0.25f));
        var nosePoint = FeatureOrDefault(analysis, "鼻子", new SKPoint(centerX, centerY));
        var mouthPoint = FeatureOrDefault(
            analysis,
            "嘴巴",
            new SKPoint(centerX, centerY + halfHeight * 0.38f));
        var output = (float[])Values.Clone();
        for (var y = 0; y < Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < Width; x++)
            {
                var index = y * Width + x;
                if (faceMask[index] <= 0f) continue;

                // 眼窩低於眉骨，眼瞼本身只保留很小的隆起。
                var eyeSocket = -0.028f * eyesOuter[index] + 0.012f * eyesInner[index];
                // 鼻翼與鼻樑形成寬而連續的凸面，避免只浮起鼻孔邊線。
                var nose = 0.050f * noseOuter[index] + 0.018f * noseInner[index];
                // 唇面微凸、唇周微凹，形成可讀但不銳利的嘴形。
                var lips = 0.030f * lipsInner[index] - 0.012f * Math.Max(0f, lipsOuter[index] - lipsInner[index]);
                var chinX = (x - chin.X) / (halfWidth * 0.34f);
                var chinY = (y - chin.Y) / (halfHeight * 0.18f);
                var chinShape = Gaussian(chinX, chinY, 1f, 1f) * 0.018f;
                var leftEyeShape = Gaussian(
                    (x - leftEye.X) / (halfWidth * 0.22f),
                    (y - leftEye.Y) / (halfHeight * 0.13f), 1f, 1f);
                var rightEyeShape = Gaussian(
                    (x - rightEye.X) / (halfWidth * 0.22f),
                    (y - rightEye.Y) / (halfHeight * 0.13f), 1f, 1f);
                var eyeAnchorShape = -(leftEyeShape + rightEyeShape) * 0.018f;
                var leftLidRidge = Gaussian(
                    (x - leftEye.X) / (halfWidth * 0.20f),
                    (y - leftEye.Y) / (halfHeight * 0.075f), 1f, 1f) * 0.016f -
                    Gaussian(
                        (x - leftEye.X) / (halfWidth * 0.13f),
                        (y - leftEye.Y) / (halfHeight * 0.030f), 1f, 1f) * 0.014f;
                var rightLidRidge = Gaussian(
                    (x - rightEye.X) / (halfWidth * 0.20f),
                    (y - rightEye.Y) / (halfHeight * 0.075f), 1f, 1f) * 0.016f -
                    Gaussian(
                        (x - rightEye.X) / (halfWidth * 0.13f),
                        (y - rightEye.Y) / (halfHeight * 0.030f), 1f, 1f) * 0.014f;
                var noseTipShape = Gaussian(
                    (x - nosePoint.X) / (halfWidth * 0.16f),
                    (y - nosePoint.Y) / (halfHeight * 0.17f), 1f, 1f) * 0.030f;
                var noseBridgeShape = Gaussian(
                    (x - nosePoint.X) / (halfWidth * 0.11f),
                    (y - (nosePoint.Y - halfHeight * 0.25f)) / (halfHeight * 0.34f), 1f, 1f) * 0.018f;
                var mouthAnchorShape = Gaussian(
                    (x - mouthPoint.X) / (halfWidth * 0.30f),
                    (y - mouthPoint.Y) / (halfHeight * 0.10f), 1f, 1f) * 0.012f;
                var lipSurfaces = upperLipInner[index] * 0.020f + lowerLipInner[index] * 0.016f -
                                  mouthOpening[index] * 0.015f;
                var mouthCornerY = (y - mouthPoint.Y) / (halfHeight * 0.065f);
                var mouthCorners = -0.010f * (
                    Gaussian((x - (mouthPoint.X - halfWidth * 0.20f)) / (halfWidth * 0.075f), mouthCornerY, 1f, 1f) +
                    Gaussian((x - (mouthPoint.X + halfWidth * 0.20f)) / (halfWidth * 0.075f), mouthCornerY, 1f, 1f));
                var correction = Math.Clamp(
                    (eyeSocket + nose + lips + chinShape + eyeAnchorShape +
                     leftLidRidge + rightLidRidge + noseTipShape + noseBridgeShape +
                     mouthAnchorShape + lipSurfaces + mouthCorners) * strength,
                    -0.03f * strength,
                    0.055f * strength);
                output[index] = Math.Clamp(output[index] + correction, 0f, 1f);
            }
        }
        var transition = BoxMean(
            output,
            Width,
            Height,
            Math.Clamp(bounds.Width / 55, 3, 9),
            cancellationToken);
        var continuityBlend = 0.18f * strength;
        for (var index = 0; index < output.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            if (analysis.Labels[index] is not (1 or 6)) continue;
            var adjustment = Math.Clamp(transition[index] - output[index], -0.018f, 0.018f);
            output[index] = Math.Clamp(output[index] + adjustment * continuityBlend, 0f, 1f);
        }
        return new ReliefDepthMap(Width, Height, output);
    }

    public ReliefDepthMap ApplyBodyGeometry(
        ReliefBodyAnalysis? body,
        ReliefPortraitAnalysis? portrait,
        float strengthPercent,
        float backgroundSuppressionPercent,
        CancellationToken cancellationToken) => ApplyBodyGeometry(
            null,
            body,
            portrait,
            strengthPercent,
            0f,
            backgroundSuppressionPercent,
            cancellationToken);

    public ReliefDepthMap ApplyBodyGeometry(
        SKBitmap? source,
        ReliefBodyAnalysis? body,
        ReliefPortraitAnalysis? portrait,
        float strengthPercent,
        float detailPercent,
        float backgroundSuppressionPercent,
        CancellationToken cancellationToken)
    {
        if (body is null || body.Width != Width || body.Height != Height)
            return Copy();
        var strength = Math.Clamp(strengthPercent, 0f, 100f) / 100f;
        var detailStrength = Math.Clamp(detailPercent, 0f, 100f) / 100f;
        var suppression = Math.Clamp(backgroundSuppressionPercent, 0f, 100f) / 100f;
        if (strength <= float.Epsilon && suppression <= float.Epsilon)
            return Copy();

        var inside = new bool[Values.Length];
        var distance = new float[Values.Length];
        const float infinity = 1_000_000f;
        for (var index = 0; index < Values.Length; index++)
        {
            inside[index] = body.Mask[index] >= 0.20f;
            distance[index] = inside[index] ? infinity : 0f;
        }
        for (var y = 0; y < Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < Width; x++)
            {
                var index = y * Width + x;
                if (!inside[index]) continue;
                var best = distance[index];
                if (x > 0) best = Math.Min(best, distance[index - 1] + 1f);
                if (y > 0) best = Math.Min(best, distance[index - Width] + 1f);
                if (x > 0 && y > 0) best = Math.Min(best, distance[index - Width - 1] + 1.4142f);
                if (x + 1 < Width && y > 0) best = Math.Min(best, distance[index - Width + 1] + 1.4142f);
                distance[index] = best;
            }
        }
        for (var y = Height - 1; y >= 0; y--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = Width - 1; x >= 0; x--)
            {
                var index = y * Width + x;
                if (!inside[index]) continue;
                var best = distance[index];
                if (x + 1 < Width) best = Math.Min(best, distance[index + 1] + 1f);
                if (y + 1 < Height) best = Math.Min(best, distance[index + Width] + 1f);
                if (x + 1 < Width && y + 1 < Height) best = Math.Min(best, distance[index + Width + 1] + 1.4142f);
                if (x > 0 && y + 1 < Height) best = Math.Min(best, distance[index + Width - 1] + 1.4142f);
                distance[index] = best;
            }
        }

        var face = portrait?.FaceBounds;
        var scale = Math.Max(8f, (face?.Width ?? body.Bounds?.Width ?? Width) * 0.20f);
        var bodyBounds = body.Bounds ?? new SKRectI(0, 0, Width, Height);
        var bodyCenterX = bodyBounds.MidX;
        var bodyWidth = Math.Max(1f, bodyBounds.Width);
        var bodyHeight = Math.Max(1f, bodyBounds.Height);
        float[]? guidance = null;
        float[]? guidanceLocal = null;
        float[]? guidanceBroad = null;
        float[]? depthLocal = null;
        float[]? depthBroad = null;
        if (detailStrength > float.Epsilon && source is not null && source.Width == Width && source.Height == Height)
        {
            guidance = CreateGuidanceLuminance(source, cancellationToken);
            guidanceLocal = BoxMean(guidance, Width, Height, Math.Clamp((int)(bodyWidth / 180f), 2, 5), cancellationToken);
            guidanceBroad = BoxMean(guidance, Width, Height, Math.Clamp((int)(bodyWidth / 45f), 7, 18), cancellationToken);
            depthLocal = BoxMean(Values, Width, Height, Math.Clamp((int)(bodyWidth / 70f), 4, 12), cancellationToken);
            depthBroad = BoxMean(Values, Width, Height, Math.Clamp((int)(bodyWidth / 20f), 15, 45), cancellationToken);
        }
        var output = (float[])Values.Clone();
        for (var y = 0; y < Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < Width; x++)
            {
                var index = y * Width + x;
                var alpha = body.Mask[index];
                var protectsFace = face is SKRectI faceBounds &&
                    x >= faceBounds.Left - faceBounds.Width * 0.18f &&
                    x <= faceBounds.Right + faceBounds.Width * 0.18f &&
                    y >= faceBounds.Top - faceBounds.Height * 0.18f &&
                    y <= faceBounds.Bottom + faceBounds.Height * 0.20f;
                if (protectsFace) continue;
                if (!inside[index])
                {
                    output[index] = Math.Clamp(output[index] * (1f - 0.97f * suppression * (1f - alpha)), 0f, 1f);
                    continue;
                }
                if (strength <= float.Epsilon) continue;

                var rounded = MathF.Sqrt(Math.Clamp(distance[index] / scale, 0f, 1f));
                var confidence = SmoothStep(Math.Clamp((alpha - 0.18f) / 0.62f, 0f, 1f));
                var normalizedX = (x - bodyCenterX) / bodyWidth;
                var normalizedY = (y - bodyBounds.Top) / bodyHeight;
                var torsoVolume = Gaussian(
                    normalizedX / 0.29f,
                    (normalizedY - 0.48f) / 0.25f,
                    1f,
                    1f) * 0.030f;
                var hipVolume = Gaussian(
                    normalizedX / 0.25f,
                    (normalizedY - 0.69f) / 0.15f,
                    1f,
                    1f) * 0.018f;
                var legVolume = normalizedY > 0.64f
                    ? (Gaussian((normalizedX - 0.14f) / 0.15f, (normalizedY - 0.82f) / 0.24f, 1f, 1f) +
                       Gaussian((normalizedX + 0.14f) / 0.15f, (normalizedY - 0.82f) / 0.24f, 1f, 1f)) * 0.012f
                    : 0f;
                var neckBlend = face is SKRectI neckFace && y > neckFace.Bottom
                    ? SmoothStep((y - neckFace.Bottom) / Math.Max(1f, neckFace.Height * 0.32f))
                    : 1f;
                var structural = (torsoVolume + hipVolume + legVolume) * neckBlend;
                var surfaceDetail = 0f;
                if (guidance is not null && guidanceLocal is not null && guidanceBroad is not null)
                {
                    var fine = guidance[index] - guidanceLocal[index];
                    var fold = guidanceLocal[index] - guidanceBroad[index];
                    var depthFine = depthLocal is null ? 0f : Values[index] - depthLocal[index];
                    var depthShape = depthLocal is null || depthBroad is null ? 0f : depthLocal[index] - depthBroad[index];
                    surfaceDetail = Math.Clamp(
                        depthFine * 0.34f + depthShape * 0.18f + fine * 0.012f + fold * 0.020f,
                        -0.024f,
                        0.024f) * detailStrength;
                }
                var correction = (rounded * 0.052f + structural) * confidence * strength +
                    surfaceDetail * confidence;
                output[index] = Math.Clamp(output[index] + correction, 0f, 1f);
            }
        }
        return new ReliefDepthMap(Width, Height, output);
    }

    public ReliefDepthMap ApplyFacialDepthContrast(
        ReliefPortraitAnalysis? analysis,
        float contrastPercent,
        CancellationToken cancellationToken)
    {
        var strength = Math.Clamp(contrastPercent, 0f, 100f) / 100f;
        if (strength <= float.Epsilon || analysis?.FaceBounds is not SKRectI bounds ||
            analysis.Width != Width || analysis.Height != Height)
            return Copy();
        var local = BoxMean(Values, Width, Height, Math.Clamp(bounds.Width / 85, 2, 7), cancellationToken);
        var broad = BoxMean(Values, Width, Height, Math.Clamp(bounds.Width / 22, 8, 28), cancellationToken);
        var centerX = bounds.MidX;
        var centerY = bounds.MidY;
        var halfWidth = Math.Max(1f, bounds.Width * 0.5f);
        var halfHeight = Math.Max(1f, bounds.Height * 0.5f);
        var leftEye = FeatureOrDefault(analysis, "左眼", new SKPoint(centerX - halfWidth * 0.38f, centerY - halfHeight * 0.25f));
        var rightEye = FeatureOrDefault(analysis, "右眼", new SKPoint(centerX + halfWidth * 0.38f, centerY - halfHeight * 0.25f));
        var nose = FeatureOrDefault(analysis, "鼻子", new SKPoint(centerX, centerY));
        var mouth = FeatureOrDefault(analysis, "嘴巴", new SKPoint(centerX, centerY + halfHeight * 0.38f));
        var output = (float[])Values.Clone();
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = bounds.Left; x < bounds.Right; x++)
            {
                var index = y * Width + x;
                var label = analysis.Labels[index];
                if (label is < 1 or > 13) continue;
                var eyeWeight = Math.Clamp(
                    Gaussian((x - leftEye.X) / (halfWidth * 0.26f), (y - leftEye.Y) / (halfHeight * 0.17f), 1f, 1f) +
                    Gaussian((x - rightEye.X) / (halfWidth * 0.26f), (y - rightEye.Y) / (halfHeight * 0.17f), 1f, 1f), 0f, 1f);
                var mouthWeight = Gaussian(
                    (x - mouth.X) / (halfWidth * 0.34f),
                    (y - mouth.Y) / (halfHeight * 0.15f), 1f, 1f);
                var noseWeight = Gaussian(
                    (x - nose.X) / (halfWidth * 0.21f),
                    (y - nose.Y) / (halfHeight * 0.30f), 1f, 1f);
                var featureWeight = Math.Clamp(Math.Max(eyeWeight, Math.Max(mouthWeight, noseWeight)), 0f, 1f);
                var localContrast = (Values[index] - local[index]) * (0.28f + 0.72f * featureWeight) * strength;
                var facialContrast = (Values[index] - broad[index]) * 0.16f * strength;
                var correction = Math.Clamp(localContrast + facialContrast, -0.020f * strength, 0.020f * strength);
                output[index] = Math.Clamp(output[index] + correction, 0f, 1f);
            }
        }
        return new ReliefDepthMap(Width, Height, output);
    }

    public ReliefDepthMap ApplyFacialMicroDetails(
        ReliefPortraitAnalysis? analysis,
        float detailPercent,
        CancellationToken cancellationToken)
    {
        var strength = Math.Clamp(detailPercent, 0f, 100f) / 100f;
        if (strength <= float.Epsilon || analysis?.FaceBounds is not SKRectI bounds ||
            analysis.Width != Width || analysis.Height != Height)
            return Copy();
        var centerX = bounds.MidX;
        var centerY = bounds.MidY;
        var halfWidth = Math.Max(1f, bounds.Width * 0.5f);
        var halfHeight = Math.Max(1f, bounds.Height * 0.5f);
        var leftEye = FeatureOrDefault(analysis, "左眼", new SKPoint(centerX - halfWidth * 0.38f, centerY - halfHeight * 0.25f));
        var rightEye = FeatureOrDefault(analysis, "右眼", new SKPoint(centerX + halfWidth * 0.38f, centerY - halfHeight * 0.25f));
        var nose = FeatureOrDefault(analysis, "鼻子", new SKPoint(centerX, centerY));
        var mouth = FeatureOrDefault(analysis, "嘴巴", new SKPoint(centerX, centerY + halfHeight * 0.38f));
        var output = (float[])Values.Clone();
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = bounds.Left; x < bounds.Right; x++)
            {
                var index = y * Width + x;
                var label = analysis.Labels[index];
                if (label is < 1 or > 13) continue;
                // 眼鏡本身由 ApplyPortraitLayers 處理，避免把眼瞼溝槽重複刻到鏡框上。
                var correction = label == 6 ? 0f : EyeDetail(leftEye) + EyeDetail(rightEye);
                if (label is 4 or 5)
                {
                    correction += 0.006f * (
                        Gaussian((x - leftEye.X) / (halfWidth * 0.14f), (y - leftEye.Y) / (halfHeight * 0.055f), 1f, 1f) +
                        Gaussian((x - rightEye.X) / (halfWidth * 0.14f), (y - rightEye.Y) / (halfHeight * 0.055f), 1f, 1f));
                }
                correction += -0.014f * (
                    Gaussian((x - (nose.X - halfWidth * 0.105f)) / (halfWidth * 0.055f), (y - (nose.Y + halfHeight * 0.07f)) / (halfHeight * 0.040f), 1f, 1f) +
                    Gaussian((x - (nose.X + halfWidth * 0.105f)) / (halfWidth * 0.055f), (y - (nose.Y + halfHeight * 0.07f)) / (halfHeight * 0.040f), 1f, 1f));
                correction += 0.009f * (
                    Gaussian((x - (nose.X - halfWidth * 0.15f)) / (halfWidth * 0.075f), (y - nose.Y) / (halfHeight * 0.075f), 1f, 1f) +
                    Gaussian((x - (nose.X + halfWidth * 0.15f)) / (halfWidth * 0.075f), (y - nose.Y) / (halfHeight * 0.075f), 1f, 1f));
                correction += -0.008f * Gaussian(
                    (x - mouth.X) / (halfWidth * 0.055f),
                    (y - (mouth.Y - halfHeight * 0.13f)) / (halfHeight * 0.11f), 1f, 1f);
                correction += 0.009f * (
                    Gaussian((x - (mouth.X - halfWidth * 0.055f)) / (halfWidth * 0.050f), (y - (mouth.Y - halfHeight * 0.025f)) / (halfHeight * 0.035f), 1f, 1f) +
                    Gaussian((x - (mouth.X + halfWidth * 0.055f)) / (halfWidth * 0.050f), (y - (mouth.Y - halfHeight * 0.025f)) / (halfHeight * 0.035f), 1f, 1f));
                if (label == 10)
                    correction += 0.005f * Gaussian(
                        (x - nose.X) / (halfWidth * 0.12f),
                        (y - (nose.Y + halfHeight * 0.015f)) / (halfHeight * 0.10f), 1f, 1f);
                if (label == 11)
                    correction -= 0.007f * Gaussian(
                        (x - mouth.X) / (halfWidth * 0.23f),
                        (y - mouth.Y) / (halfHeight * 0.030f), 1f, 1f);
                else if (label == 12)
                    correction += 0.007f * Gaussian(
                        (x - mouth.X) / (halfWidth * 0.20f),
                        (y - (mouth.Y - halfHeight * 0.030f)) / (halfHeight * 0.040f), 1f, 1f);
                else if (label == 13)
                    correction += 0.0055f * Gaussian(
                        (x - mouth.X) / (halfWidth * 0.21f),
                        (y - (mouth.Y + halfHeight * 0.035f)) / (halfHeight * 0.045f), 1f, 1f);
                correction += -0.007f * (
                    RotatedGaussian(x, y, nose.X - halfWidth * 0.17f, nose.Y + halfHeight * 0.10f, halfWidth * 0.055f, halfHeight * 0.24f, -0.32f) +
                    RotatedGaussian(x, y, nose.X + halfWidth * 0.17f, nose.Y + halfHeight * 0.10f, halfWidth * 0.055f, halfHeight * 0.24f, 0.32f));
                if (label is 7 or 8) correction += 0.008f;
                var faceX = Math.Abs((x - centerX) / halfWidth);
                var faceY = (y - centerY) / halfHeight;
                if (label == 1 && faceX > 0.78f && faceY > 0.12f)
                    correction -= 0.005f * SmoothStep((faceX - 0.78f) / 0.18f);
                correction = Math.Clamp(correction * strength, -0.016f * strength, 0.016f * strength);
                output[index] = Math.Clamp(output[index] + correction, 0f, 1f);

                float EyeDetail(SKPoint eye)
                {
                    var dx = (x - eye.X) / halfWidth;
                    var dy = (y - eye.Y) / halfHeight;
                    var upperLid = Gaussian(dx / 0.19f, (dy + 0.035f) / 0.050f, 1f, 1f) * 0.010f;
                    var lowerLid = Gaussian(dx / 0.18f, (dy - 0.045f) / 0.045f, 1f, 1f) * 0.006f;
                    var eyeSlit = Gaussian(dx / 0.145f, dy / 0.022f, 1f, 1f) * -0.010f;
                    var corners = -0.006f * (
                        Gaussian((dx - 0.18f) / 0.045f, dy / 0.045f, 1f, 1f) +
                        Gaussian((dx + 0.18f) / 0.045f, dy / 0.045f, 1f, 1f));
                    return upperLid + lowerLid + eyeSlit + corners;
                }
            }
        }
        return new ReliefDepthMap(Width, Height, output);
    }

    public SKBitmap ToBitmap(CancellationToken cancellationToken)
    {
        var pixels = new SKColor[Values.Length];
        for (var index = 0; index < pixels.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var gray = (byte)Math.Clamp((int)MathF.Round(Values[index] * 255f), 0, 255);
            pixels[index] = new SKColor(gray, gray, gray, 255);
        }
        var bitmap = new SKBitmap(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        bitmap.Pixels = pixels;
        return bitmap;
    }

    public ReliefDepthMap Crop(SKRectI bounds, CancellationToken cancellationToken)
    {
        if (bounds.Left < 0 || bounds.Top < 0 || bounds.Right > Width || bounds.Bottom > Height ||
            bounds.Width <= 0 || bounds.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(bounds));
        if (bounds.Left == 0 && bounds.Top == 0 && bounds.Right == Width && bounds.Bottom == Height)
            return Copy();
        var cropped = new float[bounds.Width * bounds.Height];
        for (var y = 0; y < bounds.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Array.Copy(
                Values,
                (bounds.Top + y) * Width + bounds.Left,
                cropped,
                y * bounds.Width,
                bounds.Width);
        }
        return new ReliefDepthMap(bounds.Width, bounds.Height, cropped);
    }

    private static float Percentile(float[] sorted, float percentile)
    {
        var position = Math.Clamp(percentile, 0f, 1f) * (sorted.Length - 1);
        var lower = (int)MathF.Floor(position);
        var upper = Math.Min(lower + 1, sorted.Length - 1);
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
    }

    private static float SkinLikelihood(SKColor color)
    {
        var red = color.Red;
        var green = color.Green;
        var blue = color.Blue;
        var maximum = Math.Max(red, Math.Max(green, blue));
        var minimum = Math.Min(red, Math.Min(green, blue));
        if (red < 60 || maximum - minimum < 18 || red <= green + 5 || red <= blue + 5)
            return 0f;
        var cb = 128f - 0.168736f * red - 0.331264f * green + 0.5f * blue;
        var cr = 128f + 0.5f * red - 0.418688f * green - 0.081312f * blue;
        var cbWeight = Math.Clamp(1f - Math.Abs(cb - 105f) / 38f, 0f, 1f);
        var crWeight = Math.Clamp(1f - Math.Abs(cr - 150f) / 38f, 0f, 1f);
        var likelihood = cbWeight * crWeight;
        return likelihood < 0.15f ? 0f : likelihood;
    }

    private static SKPoint FeatureOrDefault(ReliefPortraitAnalysis analysis, string name, SKPoint fallback) =>
        analysis.Features.TryGetValue(name, out var feature) ? feature : fallback;

    private static bool TryGetLabelBounds(byte[] labels, byte target, int width, int height, out SKRectI bounds)
    {
        var left = width;
        var top = height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            if (labels[y * width + x] != target) continue;
            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }
        bounds = right >= left && bottom >= top
            ? new SKRectI(left, top, right + 1, bottom + 1)
            : default;
        return right >= left && bottom >= top;
    }

    private static float SemanticGeometryWeight(byte label) => label switch
    {
        1 => 1f,
        2 or 3 or 4 or 5 or 6 or 10 or 11 or 12 or 13 => 0.92f,
        7 or 8 or 9 => 0.45f,
        _ => 0f
    };

    private static bool IsSurfaceDetailLabel(byte label) => label is
        1 or 2 or 3 or 4 or 5 or 7 or 8 or 9 or 10 or 11 or 12 or 13;

    private static float Gaussian(float x, float y, float sigmaX, float sigmaY) =>
        MathF.Exp(-0.5f * (x * x / (sigmaX * sigmaX) + y * y / (sigmaY * sigmaY)));

    private static float RotatedGaussian(
        float x, float y, float centerX, float centerY, float sigmaX, float sigmaY, float radians)
    {
        var dx = x - centerX;
        var dy = y - centerY;
        var cosine = MathF.Cos(radians);
        var sine = MathF.Sin(radians);
        return Gaussian(dx * cosine - dy * sine, dx * sine + dy * cosine, sigmaX, sigmaY);
    }

    private static float SmoothStep(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    private static float[] CreateGuidanceLuminance(SKBitmap bitmap, CancellationToken cancellationToken)
    {
        var pixels = bitmap.Pixels;
        var values = new float[pixels.Length];
        for (var index = 0; index < values.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var color = pixels[index];
            values[index] = (color.Red * 0.2126f + color.Green * 0.7152f + color.Blue * 0.0722f) / 255f;
        }
        return values;
    }

    private static float[] BoxMean(
        float[] source,
        int width,
        int height,
        int radius,
        CancellationToken cancellationToken)
    {
        var horizontal = new float[source.Length];
        for (var y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = y * width;
            var sum = 0f;
            for (var x = 0; x < width; x++)
            {
                var entering = Math.Min(width - 1, x + radius);
                if (x == 0)
                    for (var sample = 0; sample <= entering; sample++) sum += source[row + sample];
                else
                {
                    var leaving = x - radius - 1;
                    if (leaving >= 0) sum -= source[row + leaving];
                    if (x + radius < width) sum += source[row + x + radius];
                }
                var count = Math.Min(width - 1, x + radius) - Math.Max(0, x - radius) + 1;
                horizontal[row + x] = sum / count;
            }
        }

        var output = new float[source.Length];
        for (var x = 0; x < width; x++)
        {
            if ((x & 31) == 0) cancellationToken.ThrowIfCancellationRequested();
            var sum = 0f;
            for (var y = 0; y < height; y++)
            {
                var entering = Math.Min(height - 1, y + radius);
                if (y == 0)
                    for (var sample = 0; sample <= entering; sample++) sum += horizontal[sample * width + x];
                else
                {
                    var leaving = y - radius - 1;
                    if (leaving >= 0) sum -= horizontal[leaving * width + x];
                    if (y + radius < height) sum += horizontal[(y + radius) * width + x];
                }
                var count = Math.Min(height - 1, y + radius) - Math.Max(0, y - radius) + 1;
                output[y * width + x] = sum / count;
            }
        }
        return output;
    }

    private static float Cubic(float first, float second, float third, float fourth, float amount)
    {
        var a = -0.5f * first + 1.5f * second - 1.5f * third + 0.5f * fourth;
        var b = first - 2.5f * second + 2f * third - 0.5f * fourth;
        var c = -0.5f * first + 0.5f * third;
        return ((a * amount + b) * amount + c) * amount + second;
    }

    private static void ApplySymmetry(float[] values, int width, int height, float axisPercent, CancellationToken cancellationToken)
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
                var average = (values[left] + values[right]) * 0.5f;
                values[left] = average;
                values[right] = average;
            }
        }
    }

    private static void Posterize(float[] values, int levels, CancellationToken cancellationToken)
    {
        levels = Math.Clamp(levels, 2, 256);
        var steps = levels - 1f;
        for (var index = 0; index < values.Length; index++)
        {
            if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            values[index] = MathF.Round(values[index] * steps) / steps;
        }
    }
}
