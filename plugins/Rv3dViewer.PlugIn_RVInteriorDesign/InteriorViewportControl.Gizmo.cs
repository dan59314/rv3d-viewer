namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using Rv3dViewer.Core;

internal enum InteriorTransformTool { Select, Move, Rotate, Scale }
internal enum InteriorTransformAxis { None, X, Y, Z, ViewPlane }

internal sealed class InteriorTransformChangedEventArgs(string operation) : EventArgs
{
    internal string Operation { get; } = operation;
}

internal sealed partial class InteriorViewportControl
{
    private const float GizmoLengthPixels = 64f;
    private const float GizmoHitTolerancePixels = 9f;
    private const float GizmoCenterHitRadiusPixels = 11f;
    private readonly List<GizmoHandle> _gizmoHandles = [];
    private InteriorTransformTool _transformTool;
    private InteriorTransformAxis _hoveredGizmoAxis;
    private InteriorTransformAxis _draggedGizmoAxis;
    private bool _gizmoDragging;
    private Point _gizmoDragStartMouse;
    private Vector3 _gizmoDragStartValue;
    private Vector2 _gizmoDragScreenDirection;
    private Vector3 _gizmoDragViewRight;
    private Vector3 _gizmoDragViewUp;
    private float _gizmoDragPixelsPerMeter = 1f;
    private bool _gizmoChangeStarted;

    internal event EventHandler<InteriorTransformChangedEventArgs>? TransformChanged;
    internal event EventHandler<InteriorTransformChangedEventArgs>? TransformStarting;
    internal event EventHandler<InteriorTransformChangedEventArgs>? TransformCompleted;

    internal InteriorTransformTool TransformTool
    {
        get => _transformTool;
        set
        {
            if (_transformTool == value) return;
            _transformTool = value;
            _hoveredGizmoAxis = InteriorTransformAxis.None;
            Cursor = Cursors.Default;
            Invalidate();
        }
    }

    internal bool TransformSnapEnabled { get; set; } = true;
    internal bool TransformMoveSnapEnabled { get; set; } = true;
    internal float TransformMoveSnapMeters { get; set; } = .1f;
    internal float TransformRotationSnapDegrees { get; set; } = 5f;

    private void DrawTransformGizmo(Graphics graphics, Rectangle viewport, ProjectionContext projection)
    {
        _gizmoHandles.Clear();
        if (_transformTool == InteriorTransformTool.Select || !TryGetGizmoTarget(out _, out _, out var pivot))
            return;
        var projectedPivot = Project(pivot, viewport, projection);
        if (projectedPivot is null || !viewport.Contains(Point.Round(projectedPivot.Value.Point))) return;
        var pixelsPerMeter = _viewKind == InteriorViewportKind.Perspective
            ? projection.FocalLength / Math.Max(projectedPivot.Value.Depth, Camera.NearPlane)
            : projection.PixelsPerMeter;
        var worldLength = GizmoLengthPixels / Math.Max(pixelsPerMeter, .001f);
        var previousSmoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        if (_transformTool == InteriorTransformTool.Rotate)
            DrawRotationGizmo(graphics, viewport, projection, pivot, worldLength * .8f);
        else
            DrawLinearGizmo(graphics, viewport, projection, pivot, worldLength);
        graphics.SmoothingMode = previousSmoothing;
    }

    private void DrawLinearGizmo(Graphics graphics, Rectangle viewport, ProjectionContext projection,
        Vector3 pivot, float worldLength)
    {
        var projectedPivot = Project(pivot, viewport, projection)!.Value.Point;
        foreach (var (axis, direction, color, label) in AxisDefinitions())
        {
            var endpoint = Project(pivot + direction * worldLength, viewport, projection);
            if (endpoint is null) continue;
            var screenVector = new Vector2(endpoint.Value.Point.X - projectedPivot.X,
                endpoint.Value.Point.Y - projectedPivot.Y);
            if (screenVector.LengthSquared() < 36f) continue;
            var highlighted = axis == _hoveredGizmoAxis || axis == _draggedGizmoAxis;
            using var pen = new Pen(highlighted ? Color.Gold : color, highlighted ? 4f : 3f)
            {
                EndCap = _transformTool == InteriorTransformTool.Move
                    ? System.Drawing.Drawing2D.LineCap.ArrowAnchor
                    : System.Drawing.Drawing2D.LineCap.SquareAnchor
            };
            graphics.DrawLine(pen, projectedPivot, endpoint.Value.Point);
            if (_transformTool == InteriorTransformTool.Scale)
            {
                using var brush = new SolidBrush(highlighted ? Color.Gold : color);
                graphics.FillRectangle(brush, endpoint.Value.Point.X - 5f, endpoint.Value.Point.Y - 5f, 10f, 10f);
            }
            TextRenderer.DrawText(graphics, label, Font,
                new Point((int)endpoint.Value.Point.X + 4, (int)endpoint.Value.Point.Y - 9),
                highlighted ? Color.Gold : color, TextFormatFlags.NoPadding);
            _gizmoHandles.Add(new GizmoHandle(axis, projectedPivot, endpoint.Value.Point, []));
        }
        var centerHighlighted = _hoveredGizmoAxis == InteriorTransformAxis.ViewPlane ||
                                _draggedGizmoAxis == InteriorTransformAxis.ViewPlane;
        var centerRadius = _transformTool == InteriorTransformTool.Move ? 7f : 4f;
        using var centerBrush = new SolidBrush(centerHighlighted ? Color.Gold : Color.FromArgb(235, 238, 241));
        graphics.FillEllipse(centerBrush, projectedPivot.X - centerRadius, projectedPivot.Y - centerRadius,
            centerRadius * 2f, centerRadius * 2f);
        if (_transformTool == InteriorTransformTool.Move)
        {
            using var centerOutline = new Pen(centerHighlighted ? Color.OrangeRed : Color.FromArgb(48, 54, 62), 2f);
            graphics.DrawEllipse(centerOutline, projectedPivot.X - centerRadius, projectedPivot.Y - centerRadius,
                centerRadius * 2f, centerRadius * 2f);
            _gizmoHandles.Add(new GizmoHandle(InteriorTransformAxis.ViewPlane,
                projectedPivot, projectedPivot, []));
        }
    }

    private void DrawRotationGizmo(Graphics graphics, Rectangle viewport, ProjectionContext projection,
        Vector3 pivot, float radius)
    {
        foreach (var (axis, _, color, label) in AxisDefinitions())
        {
            var points = new List<PointF>();
            for (var step = 0; step <= 48; step++)
            {
                var angle = step * MathF.Tau / 48f;
                var offset = axis switch
                {
                    InteriorTransformAxis.X => new Vector3(0f, MathF.Cos(angle), MathF.Sin(angle)),
                    InteriorTransformAxis.Y => new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle)),
                    _ => new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0f)
                } * radius;
                var projected = Project(pivot + offset, viewport, projection);
                if (projected is not null) points.Add(projected.Value.Point);
            }
            if (points.Count < 3) continue;
            var highlighted = axis == _hoveredGizmoAxis || axis == _draggedGizmoAxis;
            using var pen = new Pen(highlighted ? Color.Gold : color, highlighted ? 4f : 2.5f);
            graphics.DrawLines(pen, points.ToArray());
            TextRenderer.DrawText(graphics, label, Font, Point.Round(points[0]),
                highlighted ? Color.Gold : color, TextFormatFlags.NoPadding);
            _gizmoHandles.Add(new GizmoHandle(axis, points[0], points[^1], points));
        }
    }

    private bool BeginGizmoDrag(Point location)
    {
        if (_transformTool == InteriorTransformTool.Select || !TryHitGizmo(location, out var handle) ||
            !TryGetGizmoTarget(out _, out var transform, out _)) return false;
        _gizmoDragging = true;
        _draggedGizmoAxis = handle.Axis;
        _gizmoDragStartMouse = location;
        _gizmoDragStartValue = _transformTool switch
        {
            InteriorTransformTool.Move => transform.Position,
            InteriorTransformTool.Rotate => transform.RotationDegrees,
            _ => transform.Scale
        };
        var direction = new Vector2(handle.End.X - handle.Start.X, handle.End.Y - handle.Start.Y);
        _gizmoDragScreenDirection = direction.LengthSquared() < 1f ? Vector2.UnitX : Vector2.Normalize(direction);
        if (handle.Axis == InteriorTransformAxis.ViewPlane)
        {
            var viewport = new Rectangle(0, headerPanel.Bottom, Width, Math.Max(1, Height - headerPanel.Bottom));
            var projection = CreateProjectionContext(viewport);
            var depth = Project(GetCurrentGizmoPivot(), viewport, projection)?.Depth ?? projection.Distance;
            _gizmoDragPixelsPerMeter = _viewKind == InteriorViewportKind.Perspective
                ? projection.FocalLength / Math.Max(depth, Camera.NearPlane)
                : projection.PixelsPerMeter;
            _gizmoDragViewRight = projection.Right;
            _gizmoDragViewUp = projection.Up;
        }
        _gizmoChangeStarted = false;
        Capture = true;
        Cursor = Cursors.SizeAll;
        Invalidate();
        return true;
    }

    private void UpdateGizmoDrag(Point location)
    {
        if (!_gizmoDragging || !TryGetGizmoTarget(out _, out var transform, out _)) return;
        var mouseDelta = new Vector2(location.X - _gizmoDragStartMouse.X, location.Y - _gizmoDragStartMouse.Y);
        var pixels = Vector2.Dot(mouseDelta, _gizmoDragScreenDirection);
        var axisIndex = _draggedGizmoAxis switch
        {
            InteriorTransformAxis.X => 0,
            InteriorTransformAxis.Y => 1,
            _ => 2
        };
        var operation = _transformTool switch
        {
            InteriorTransformTool.Move when _draggedGizmoAxis == InteriorTransformAxis.ViewPlane => "自由移動",
            InteriorTransformTool.Move => "移動", InteriorTransformTool.Rotate => "旋轉", _ => "縮放"
        };
        if (!_gizmoChangeStarted)
        {
            _gizmoChangeStarted = true;
            TransformStarting?.Invoke(this, new InteriorTransformChangedEventArgs(operation));
        }
        if (_transformTool == InteriorTransformTool.Move)
        {
            if (_draggedGizmoAxis == InteriorTransformAxis.ViewPlane)
            {
                var horizontalMeters = mouseDelta.X / Math.Max(_gizmoDragPixelsPerMeter, .001f);
                var verticalMeters = -mouseDelta.Y / Math.Max(_gizmoDragPixelsPerMeter, .001f);
                if (TransformSnapEnabled && TransformMoveSnapEnabled)
                {
                    horizontalMeters = Snap(horizontalMeters, TransformMoveSnapMeters);
                    verticalMeters = Snap(verticalMeters, TransformMoveSnapMeters);
                }
                transform.Position = _gizmoDragStartValue +
                                     _gizmoDragViewRight * horizontalMeters +
                                     _gizmoDragViewUp * verticalMeters;
            }
            else
            {
                var viewport = new Rectangle(0, headerPanel.Bottom, Width, Math.Max(1, Height - headerPanel.Bottom));
                var projection = CreateProjectionContext(viewport);
                var depth = Project(GetCurrentGizmoPivot(), viewport, projection)?.Depth ?? projection.Distance;
                var pixelsPerMeter = _viewKind == InteriorViewportKind.Perspective
                    ? projection.FocalLength / Math.Max(depth, Camera.NearPlane)
                    : projection.PixelsPerMeter;
                var meters = pixels / Math.Max(pixelsPerMeter, .001f);
                transform.Position = SetAxis(_gizmoDragStartValue, axisIndex,
                    SnapMove(GetAxis(_gizmoDragStartValue, axisIndex) + meters));
            }
        }
        else if (_transformTool == InteriorTransformTool.Rotate)
        {
            transform.RotationDegrees = SetAxis(_gizmoDragStartValue, axisIndex,
                Snap(GetAxis(_gizmoDragStartValue, axisIndex) + pixels * .75f,
                    TransformRotationSnapDegrees));
        }
        else
        {
            var original = GetAxis(_gizmoDragStartValue, axisIndex);
            var sign = original < 0f ? -1f : 1f;
            var magnitude = Math.Max(.01f, MathF.Abs(original) + pixels / 80f);
            transform.Scale = SetAxis(_gizmoDragStartValue, axisIndex, sign * Math.Max(.01f, Snap(magnitude, .1f)));
        }
        TransformChanged?.Invoke(this, new InteriorTransformChangedEventArgs(operation));
    }

    private Vector3 GetCurrentGizmoPivot() => TryGetGizmoTarget(out _, out _, out var pivot) ? pivot : Vector3.Zero;

    private void EndGizmoDrag()
    {
        var completedOperation = _transformTool switch
        {
            InteriorTransformTool.Move when _draggedGizmoAxis == InteriorTransformAxis.ViewPlane => "自由移動",
            InteriorTransformTool.Move => "移動",
            InteriorTransformTool.Rotate => "旋轉",
            _ => "縮放"
        };
        var changed = _gizmoChangeStarted;
        _gizmoDragging = false;
        _draggedGizmoAxis = InteriorTransformAxis.None;
        _gizmoChangeStarted = false;
        Capture = false;
        Cursor = _hoveredGizmoAxis == InteriorTransformAxis.None ? Cursors.Default : Cursors.SizeAll;
        Invalidate();
        if (changed)
            TransformCompleted?.Invoke(this, new InteriorTransformChangedEventArgs(completedOperation));
    }

    internal bool TryCancelGizmoDrag(out bool transformChanged)
    {
        transformChanged = _gizmoChangeStarted;
        if (!_gizmoDragging)
            return false;
        EndGizmoDrag();
        return true;
    }

    private void UpdateGizmoHover(Point location)
    {
        var axis = TryHitGizmo(location, out var handle) ? handle.Axis : InteriorTransformAxis.None;
        if (axis == _hoveredGizmoAxis) return;
        _hoveredGizmoAxis = axis;
        Cursor = axis == InteriorTransformAxis.None ? Cursors.Default : Cursors.SizeAll;
        Invalidate();
    }

    private bool TryHitGizmo(Point point, out GizmoHandle result)
    {
        foreach (var handle in _gizmoHandles.Where(handle => handle.Axis == InteriorTransformAxis.ViewPlane))
        {
            var offset = new Vector2(point.X - handle.Start.X, point.Y - handle.Start.Y);
            if (offset.Length() <= GizmoCenterHitRadiusPixels)
            {
                result = handle;
                return true;
            }
        }
        var bestDistance = float.PositiveInfinity;
        result = default;
        foreach (var handle in _gizmoHandles.Where(handle => handle.Axis != InteriorTransformAxis.ViewPlane))
        {
            var distance = handle.Polyline.Count > 1
                ? PolylineDistance(point, handle.Polyline)
                : SegmentDistance(point, handle.Start, handle.End);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            result = handle;
        }
        return bestDistance <= GizmoHitTolerancePixels;
    }

    private bool TryGetGizmoTarget(out SceneModel model, out TransformState transform, out Vector3 pivot)
    {
        model = _primarySelectedModelId is Guid id ? _sceneModels.FirstOrDefault(item => item.Id == id)! : null!;
        if (model is null)
        {
            transform = null!;
            pivot = default;
            return false;
        }
        if (_selectedMeshIndex is int meshIndex && (uint)meshIndex < (uint)model.Meshes.Count)
        {
            transform = model.GetOrCreateMeshTransform(meshIndex);
            pivot = GetMeshCenter(model.Meshes[meshIndex], transform.Matrix * model.Transform.Matrix);
            return true;
        }
        transform = model.Transform;
        pivot = GetModelCenter(model);
        return true;
    }

    private static Vector3 GetModelCenter(SceneModel model)
    {
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        var found = false;
        for (var meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
        {
            if (model.HiddenMeshIndices.Contains(meshIndex)) continue;
            var meshMatrix = model.MeshTransforms.TryGetValue(meshIndex, out var meshTransform)
                ? meshTransform.Matrix : Matrix4x4.Identity;
            var matrix = meshMatrix * model.Transform.Matrix;
            foreach (var position in model.Meshes[meshIndex].Positions)
            {
                var world = Vector3.Transform(position, matrix);
                minimum = Vector3.Min(minimum, world);
                maximum = Vector3.Max(maximum, world);
                found = true;
            }
        }
        return found ? (minimum + maximum) * .5f : model.Transform.Position;
    }

    private static Vector3 GetMeshCenter(MeshData mesh, Matrix4x4 matrix)
    {
        if (mesh.Positions.Length == 0) return Vector3.Transform(Vector3.Zero, matrix);
        var minimum = mesh.Positions[0];
        var maximum = mesh.Positions[0];
        foreach (var position in mesh.Positions)
        {
            minimum = Vector3.Min(minimum, position);
            maximum = Vector3.Max(maximum, position);
        }
        return Vector3.Transform((minimum + maximum) * .5f, matrix);
    }

    private float Snap(float value, float step) => TransformSnapEnabled ? MathF.Round(value / step) * step : value;
    private float SnapMove(float value) => TransformSnapEnabled && TransformMoveSnapEnabled
        ? MathF.Round(value / TransformMoveSnapMeters) * TransformMoveSnapMeters
        : value;
    private static float GetAxis(Vector3 value, int axis) => axis switch { 0 => value.X, 1 => value.Y, _ => value.Z };
    private static Vector3 SetAxis(Vector3 value, int axis, float component) => axis switch
    {
        0 => value with { X = component }, 1 => value with { Y = component }, _ => value with { Z = component }
    };

    private static float SegmentDistance(Point point, PointF start, PointF end)
    {
        var segment = new Vector2(end.X - start.X, end.Y - start.Y);
        if (segment.LengthSquared() < .0001f)
            return Vector2.Distance(new Vector2(point.X, point.Y), new Vector2(start.X, start.Y));
        var amount = Math.Clamp(Vector2.Dot(new Vector2(point.X - start.X, point.Y - start.Y), segment) /
                                segment.LengthSquared(), 0f, 1f);
        return Vector2.Distance(new Vector2(point.X, point.Y), new Vector2(start.X, start.Y) + segment * amount);
    }

    private static float PolylineDistance(Point point, IReadOnlyList<PointF> points)
    {
        var result = float.PositiveInfinity;
        for (var index = 0; index + 1 < points.Count; index++)
            result = Math.Min(result, SegmentDistance(point, points[index], points[index + 1]));
        return result;
    }

    private static (InteriorTransformAxis Axis, Vector3 Direction, Color Color, string Label)[] AxisDefinitions() =>
    [
        (InteriorTransformAxis.X, Vector3.UnitX, Color.FromArgb(235, 79, 88), "X"),
        (InteriorTransformAxis.Y, Vector3.UnitY, Color.FromArgb(92, 205, 112), "Y"),
        (InteriorTransformAxis.Z, Vector3.UnitZ, Color.FromArgb(78, 145, 238), "Z")
    ];

    private readonly record struct GizmoHandle(InteriorTransformAxis Axis, PointF Start, PointF End,
        IReadOnlyList<PointF> Polyline);
}
