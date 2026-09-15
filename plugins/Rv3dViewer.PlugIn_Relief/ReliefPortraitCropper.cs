using SkiaSharp;

namespace Rv3dViewer.ReliefPlugin;

internal static class ReliefPortraitCropper
{
    public static bool TryGetCropBounds(ReliefBodyAnalysis? body, out SKRectI bounds)
    {
        bounds = default;
        if (body?.Bounds is not SKRectI person || person.Width < 16 || person.Height < 32)
            return false;
        var marginX = Math.Max(4f, person.Width * 0.06f);
        var marginTop = Math.Max(4f, person.Height * 0.04f);
        var marginBottom = Math.Max(4f, person.Height * 0.025f);
        bounds = new SKRectI(
            Math.Max(0, (int)MathF.Floor(person.Left - marginX)),
            Math.Max(0, (int)MathF.Floor(person.Top - marginTop)),
            Math.Min(body.Width, (int)MathF.Ceiling(person.Right + marginX)),
            Math.Min(body.Height, (int)MathF.Ceiling(person.Bottom + marginBottom)));
        return bounds.Width >= 32 && bounds.Height >= 32;
    }

    public static bool TryGetCropBounds(ReliefPortraitAnalysis? analysis, out SKRectI bounds)
    {
        bounds = default;
        if (analysis?.FaceBounds is not SKRectI face || face.Width < 8 || face.Height < 8)
            return false;

        // 保留頭髮、肩部與胸像下緣，但避免合成圖中相鄰的參考影像進入模型。
        var left = face.MidX - face.Width * 0.78f;
        var right = face.MidX + face.Width * 0.78f;
        var top = face.Top - face.Height * 0.28f;
        var bottom = face.Bottom + face.Height * 0.92f;
        left = Math.Clamp(left, 0f, analysis.Width);
        right = Math.Clamp(right, 0f, analysis.Width);
        top = Math.Clamp(top, 0f, analysis.Height);
        bottom = Math.Clamp(bottom, 0f, analysis.Height);
        var candidate = new SKRectI(
            (int)MathF.Floor(left),
            (int)MathF.Floor(top),
            (int)MathF.Ceiling(right),
            (int)MathF.Ceiling(bottom));
        if (candidate.Width < 32 || candidate.Height < 32 ||
            candidate.Width >= analysis.Width * 0.98f && candidate.Height >= analysis.Height * 0.98f)
            return false;
        bounds = candidate;
        return true;
    }

    public static SKBitmap CropBitmap(SKBitmap source, SKRectI bounds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cropped = new SKBitmap(bounds.Width, bounds.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(cropped);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(
            source,
            bounds,
            new SKRect(0f, 0f, bounds.Width, bounds.Height));
        cancellationToken.ThrowIfCancellationRequested();
        return cropped;
    }
}
