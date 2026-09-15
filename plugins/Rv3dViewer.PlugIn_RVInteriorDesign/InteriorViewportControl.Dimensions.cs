namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using Rv3dViewer.Core;

internal sealed partial class InteriorViewportControl
{
    private void DrawSelectedModelDimensions(Graphics graphics, Rectangle viewport, ProjectionContext projection)
    {
        if (_primarySelectedModelId is not Guid selectedId)
            return;
        var model = _sceneModels.FirstOrDefault(item => item.Id == selectedId && item.IsVisible);
        if (model is null || !SceneTraversal.TryCalculateBounds(model, out var bounds))
            return;

        var center = bounds.Center;
        var size = bounds.Size;
        var dimensions = _viewKind switch
        {
            InteriorViewportKind.Top => new[]
            {
                new DimensionEdge(new Vector3(bounds.Minimum.X, center.Y, bounds.Maximum.Z),
                    new Vector3(bounds.Maximum.X, center.Y, bounds.Maximum.Z), size.X, 14f),
                new DimensionEdge(new Vector3(bounds.Maximum.X, center.Y, bounds.Minimum.Z),
                    new Vector3(bounds.Maximum.X, center.Y, bounds.Maximum.Z), size.Z, -14f)
            },
            InteriorViewportKind.Front => new[]
            {
                new DimensionEdge(new Vector3(bounds.Minimum.X, bounds.Minimum.Y, center.Z),
                    new Vector3(bounds.Maximum.X, bounds.Minimum.Y, center.Z), size.X, 14f),
                new DimensionEdge(new Vector3(bounds.Maximum.X, bounds.Minimum.Y, center.Z),
                    new Vector3(bounds.Maximum.X, bounds.Maximum.Y, center.Z), size.Y, -14f)
            },
            InteriorViewportKind.Right => new[]
            {
                new DimensionEdge(new Vector3(center.X, bounds.Minimum.Y, bounds.Minimum.Z),
                    new Vector3(center.X, bounds.Minimum.Y, bounds.Maximum.Z), size.Z, 14f),
                new DimensionEdge(new Vector3(center.X, bounds.Minimum.Y, bounds.Maximum.Z),
                    new Vector3(center.X, bounds.Maximum.Y, bounds.Maximum.Z), size.Y, -14f)
            },
            _ => new[]
            {
                new DimensionEdge(bounds.Minimum,
                    bounds.Minimum with { X = bounds.Maximum.X }, size.X, 13f),
                new DimensionEdge(bounds.Minimum,
                    bounds.Minimum with { Y = bounds.Maximum.Y }, size.Y, -13f),
                new DimensionEdge(bounds.Minimum,
                    bounds.Minimum with { Z = bounds.Maximum.Z }, size.Z, 13f)
            }
        };

        foreach (var dimension in dimensions)
            DrawDimensionEdge(graphics, viewport, projection, dimension);
    }

    private void DrawDimensionEdge(Graphics graphics, Rectangle viewport, ProjectionContext projection,
        DimensionEdge dimension)
    {
        if (dimension.Meters <= .0001f)
            return;
        var projectedStart = Project(dimension.Start, viewport, projection);
        var projectedEnd = Project(dimension.End, viewport, projection);
        if (projectedStart is null || projectedEnd is null)
            return;
        var start = new Vector2(projectedStart.Value.Point.X, projectedStart.Value.Point.Y);
        var end = new Vector2(projectedEnd.Value.Point.X, projectedEnd.Value.Point.Y);
        var direction = end - start;
        if (direction.LengthSquared() < 25f)
            return;
        direction = Vector2.Normalize(direction);
        var normal = new Vector2(-direction.Y, direction.X) * dimension.OffsetPixels;
        var lineStart = start + normal;
        var lineEnd = end + normal;

        using var linePen = new Pen(Color.FromArgb(245, 225, 235, 245), 1f);
        using var extensionPen = new Pen(Color.FromArgb(190, 190, 205, 220), 1f);
        graphics.DrawLine(extensionPen, ToPoint(start), ToPoint(lineStart));
        graphics.DrawLine(extensionPen, ToPoint(end), ToPoint(lineEnd));
        graphics.DrawLine(linePen, ToPoint(lineStart), ToPoint(lineEnd));
        var tick = new Vector2(-direction.Y, direction.X) * 5f;
        graphics.DrawLine(linePen, ToPoint(lineStart - tick), ToPoint(lineStart + tick));
        graphics.DrawLine(linePen, ToPoint(lineEnd - tick), ToPoint(lineEnd + tick));

        var text = $"{dimension.Meters:0.##} m";
        var midpoint = (lineStart + lineEnd) * .5f;
        DrawAxisAlignedDimensionText(graphics, midpoint, direction, text);
    }

    private void DrawAxisAlignedDimensionText(Graphics graphics, Vector2 midpoint, Vector2 direction, string text)
    {
        var angle = MathF.Atan2(direction.Y, direction.X) * 180f / MathF.PI;
        if (angle > 90f) angle -= 180f;
        if (angle < -90f) angle += 180f;
        var textSize = graphics.MeasureString(text, Font);
        var textRectangle = new RectangleF(
            -textSize.Width / 2f - 3f, -textSize.Height / 2f - 1f,
            textSize.Width + 6f, textSize.Height + 2f);
        var state = graphics.Save();
        try
        {
            graphics.TranslateTransform(midpoint.X, midpoint.Y);
            graphics.RotateTransform(angle);
            using var background = new SolidBrush(Color.FromArgb(210, 24, 29, 35));
            using var foreground = new SolidBrush(Color.White);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };
            graphics.FillRectangle(background, textRectangle);
            graphics.DrawString(text, Font, foreground, textRectangle, format);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private static PointF ToPoint(Vector2 value) => new(value.X, value.Y);

    private readonly record struct DimensionEdge(Vector3 Start, Vector3 End, float Meters, float OffsetPixels);
}
