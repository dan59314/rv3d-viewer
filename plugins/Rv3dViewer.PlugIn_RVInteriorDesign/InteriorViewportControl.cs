namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Rv3dViewer.Core;

public enum InteriorViewportKind
{
    Top,
    Front,
    Right,
    Perspective
}

internal enum InteriorVisibilityMode
{
    Solid,
    TransparentOccluders,
    AutoHideForegroundWalls
}

internal enum InteriorViewportSelectionMode { Navigate, Select, Subtract }

internal enum InteriorSelectionOperation { Replace, Add, Subtract, Toggle }

internal sealed class InteriorModelPickedEventArgs(
    IEnumerable<Guid> modelIds, InteriorSelectionOperation operation) : EventArgs
{
    internal IReadOnlyList<Guid> ModelIds { get; } = modelIds.Distinct().ToArray();
    internal InteriorSelectionOperation Operation { get; } = operation;
}

internal sealed class InteriorPlacementPointEventArgs(
    Vector3 point, Vector3 rayOrigin, Vector3 rayDirection, bool hasGroundPoint) : EventArgs
{
    internal Vector3 Point { get; } = point;
    internal Vector3 RayOrigin { get; } = rayOrigin;
    internal Vector3 RayDirection { get; } = rayDirection;
    internal bool HasGroundPoint { get; } = hasGroundPoint;
}

internal sealed partial class InteriorViewportControl : UserControl
{
    internal const float GridSpacingMeters = .2f;
    private const int MajorGridInterval = 5;
    private const int PlacementPreviewTriangleBudget = 16000;
    private InteriorViewportKind _viewKind;
    private bool _showScaleBar;
    private Point _lastMouse;
    private bool _mouseCaptured;
    private IReadOnlyList<SceneModel> _sceneModels = [];
    private readonly HashSet<Guid> _selectedModelIds = [];
    private readonly HashSet<Guid> _collisionWarningModelIds = [];
    private Guid? _primarySelectedModelId;
    private Guid? _preparedDollyTargetModelId;
    private int? _selectedMeshIndex;
    private InteriorViewportSelectionMode _selectionMode;
    private Point _selectionMouseDown;
    private Point _selectionMouseCurrent;
    private bool _selectionPointerDown;
    private bool _showModelEdges = true;
    private bool _showModelDimensions;
    private InteriorVisibilityMode _visibilityMode = InteriorVisibilityMode.AutoHideForegroundWalls;
    private Guid? _placementModelId;
    private bool _placementSnapToGrid;
    private bool _placementHasCollision;
    private readonly Dictionary<string, PreviewTextureImage> _previewTextureCache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _failedPreviewTexturePaths = new(StringComparer.OrdinalIgnoreCase);
    private string _lastRenderError = string.Empty;

    public InteriorViewportControl()
    {
        InitializeComponent();
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public event EventHandler? MaximizeRequested;

    public event EventHandler? CameraChanged;

    internal event EventHandler<InteriorModelPickedEventArgs>? ModelPicked;

    internal event EventHandler<InteriorPlacementPointEventArgs>? PlacementPointChanged;

    internal event EventHandler<InteriorPlacementPointEventArgs>? PlacementConfirmed;

    internal event EventHandler? PlacementCanceled;

    internal event EventHandler<string>? RenderError;

    internal CameraState Camera { get; } = CameraState.Default;

    internal string LastNavigationOperation { get; private set; } = string.Empty;

    internal int RenderedModelCount => _sceneModels.Count;

    internal bool ShowModelEdges
    {
        get => _showModelEdges;
        set
        {
            if (_showModelEdges == value)
                return;
            _showModelEdges = value;
            Invalidate();
        }
    }

    internal bool ShowModelDimensions
    {
        get => _showModelDimensions;
        set
        {
            if (_showModelDimensions == value) return;
            _showModelDimensions = value;
            Invalidate();
        }
    }

    internal InteriorVisibilityMode VisibilityMode
    {
        get => _visibilityMode;
        set
        {
            if (_visibilityMode == value)
                return;
            _visibilityMode = value;
            Invalidate();
        }
    }

    internal int GetProjectedTriangleCount(Size viewportSize)
    {
        var top = headerPanel.Height;
        return BuildRenderTriangles(new Rectangle(0, top, viewportSize.Width, Math.Max(0, viewportSize.Height - top))).Count;
    }

    internal Bitmap RenderSceneBitmapForTest(Size viewportSize)
    {
        var top = headerPanel.Height;
        var viewport = new Rectangle(0, top, viewportSize.Width, Math.Max(1, viewportSize.Height - top));
        var projection = CreateProjectionContext(viewport);
        return RasterizeScene(BuildRenderTriangles(viewport, projection), viewport);
    }

    internal void SetSceneModels(IReadOnlyList<SceneModel> models)
    {
        _sceneModels = models;
        TrimPreviewTextureCache(models);
        Invalidate();
    }

    internal void BeginPlacement(Guid modelId, bool snapToGrid)
    {
        _placementModelId = modelId;
        _placementSnapToGrid = snapToGrid;
        _placementHasCollision = false;
        Cursor = Cursors.Cross;
        Focus();
        Invalidate();
    }

    internal void EndPlacement()
    {
        _placementModelId = null;
        _placementHasCollision = false;
        Cursor = _selectionMode == InteriorViewportSelectionMode.Navigate ? Cursors.Default : Cursors.Cross;
        Invalidate();
    }

    internal void SetPlacementCollision(bool hasCollision)
    {
        if (_placementHasCollision == hasCollision)
            return;
        _placementHasCollision = hasCollision;
        Invalidate();
    }

    internal void SetCollisionWarningModels(IEnumerable<Guid> modelIds)
    {
        var incoming = modelIds.ToHashSet();
        if (_collisionWarningModelIds.SetEquals(incoming))
            return;
        _collisionWarningModelIds.Clear();
        _collisionWarningModelIds.UnionWith(incoming);
        Invalidate();
    }

    internal void SetSelectedModel(Guid? modelId)
    {
        SetSelection(modelId, null);
    }

    internal void SetSelection(Guid? modelId, int? meshIndex)
    {
        SetSelection(modelId is Guid id ? [id] : [], modelId, meshIndex);
    }

    internal void SetSelection(IEnumerable<Guid> modelIds, Guid? primaryModelId, int? meshIndex)
    {
        var previousPrimaryModelId = _primarySelectedModelId;
        _selectedModelIds.Clear();
        _selectedModelIds.UnionWith(modelIds);
        _primarySelectedModelId = primaryModelId is Guid id && _selectedModelIds.Contains(id) ? id : null;
        if (_primarySelectedModelId != previousPrimaryModelId)
            _preparedDollyTargetModelId = null;
        _selectedMeshIndex = _primarySelectedModelId is null ? null : meshIndex;
        Invalidate();
    }

    internal InteriorViewportSelectionMode SelectionMode
    {
        get => _selectionMode;
        set
        {
            _selectionMode = value;
            _selectionPointerDown = false;
            Cursor = value == InteriorViewportSelectionMode.Navigate ? Cursors.Default : Cursors.Cross;
        }
    }

    public InteriorViewportKind ViewKind
    {
        get => _viewKind;
        set
        {
            _viewKind = value;
            titleLabel.Text = ViewName;
            ResetCameraForView();
            Invalidate();
        }
    }

    public string ViewName => _viewKind switch
    {
        InteriorViewportKind.Top => "上視圖",
        InteriorViewportKind.Front => "前視圖",
        InteriorViewportKind.Right => "右視圖",
        _ => "透視圖"
    };

    public bool ShowScaleBar
    {
        get => _showScaleBar;
        set
        {
            _showScaleBar = value;
            Invalidate();
        }
    }

    internal bool IsScaleBarVisible => _showScaleBar && _viewKind != InteriorViewportKind.Perspective;

    private void ResetCameraForView()
    {
        Camera.From = new Vector3(4f, 3f, 6f);
        Camera.To = Vector3.Zero;
        Camera.Up = Vector3.UnitY;
        Camera.RollDegrees = 0f;
        Camera.FieldOfViewDegrees = 60f;
        var view = _viewKind switch
        {
            InteriorViewportKind.Top => CameraView.Top,
            InteriorViewportKind.Front => CameraView.Front,
            InteriorViewportKind.Right => CameraView.Right,
            _ => CameraView.Perspective
        };
        CameraController.SetView(Camera, view);
    }

    public bool IsMaximized
    {
        get => maximizeButton.Text == "❐";
        set => maximizeButton.Text = value ? "❐" : "□";
    }

    public bool ShowMaximizeButton
    {
        get => maximizeButton.Visible;
        set => maximizeButton.Visible = value;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        try
        {
            var top = headerPanel.Bottom;
            var viewport = new Rectangle(0, top, Width, Height - top);
            var projection = CreateProjectionContext(viewport);
            DrawWorldGrid(e.Graphics, viewport, projection);
            if (_sceneModels.Count > 0)
                RenderScene(e.Graphics, viewport, projection);
            if (_showModelDimensions)
                DrawSelectedModelDimensions(e.Graphics, viewport, projection);
            if (IsScaleBarVisible)
                DrawScaleBar(e.Graphics, projection.PixelsPerMeter);
            DrawTransformGizmo(e.Graphics, viewport, projection);
            DrawSelectionRectangle(e.Graphics);
            if (_sceneModels.Count == 0)
                TextRenderer.DrawText(e.Graphics, "建立模型後將在此顯示預覽", Font,
                    new Rectangle(0, top, Width, Height - top), Color.FromArgb(180, 210, 218, 226),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            _lastRenderError = string.Empty;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            var message = exception.GetBaseException().Message;
            if (!string.Equals(_lastRenderError, message, StringComparison.Ordinal))
            {
                _lastRenderError = message;
                RenderError?.Invoke(this, message);
            }
            TextRenderer.DrawText(e.Graphics, $"視圖繪製失敗：{message}", Font, ClientRectangle,
                Color.IndianRed, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                 TextFormatFlags.WordBreak);
        }
    }

    private void RenderScene(Graphics graphics, Rectangle viewport, ProjectionContext projection)
    {
        if (viewport.Width <= 1 || viewport.Height <= 1)
            return;
        var triangles = BuildRenderTriangles(viewport, projection);
        using var bitmap = RasterizeScene(triangles, viewport);
        graphics.DrawImageUnscaled(bitmap, viewport.Location);
    }

    private Bitmap RasterizeScene(IReadOnlyList<RenderTriangle> triangles, Rectangle viewport)
    {
        var width = viewport.Width;
        var height = viewport.Height;
        var pixels = new int[width * height];
        var depths = new float[pixels.Length];
        Array.Fill(depths, float.PositiveInfinity);
        foreach (var triangle in triangles.Where(triangle => !triangle.IsTransparent))
            RasterizeTriangleFill(triangle, viewport.Location, width, height, pixels, depths, false);
        foreach (var triangle in triangles.Where(triangle => triangle.IsTransparent)
                     .OrderByDescending(triangle => triangle.AverageDepth))
            RasterizeTriangleFill(triangle, viewport.Location, width, height, pixels, depths, true);
        if (_showModelEdges || triangles.Any(triangle => triangle.IsSelected || triangle.HasCollisionWarning))
            foreach (var triangle in triangles)
                if ((_showModelEdges || triangle.IsSelected || triangle.HasCollisionWarning) &&
                    (!triangle.IsTransparent || triangle.IsSelected || triangle.HasCollisionWarning))
                    RasterizeTriangleEdges(triangle, viewport.Location, width, height, pixels, depths);
        return CreateBitmap(width, height, pixels);
    }

    private void RasterizeTriangleFill(RenderTriangle triangle, Point origin, int width, int height,
        int[] pixels, float[] depths, bool blend)
    {
        var a = new PointF(triangle.A.X - origin.X, triangle.A.Y - origin.Y);
        var b = new PointF(triangle.B.X - origin.X, triangle.B.Y - origin.Y);
        var c = new PointF(triangle.C.X - origin.X, triangle.C.Y - origin.Y);
        var area = Edge(a, b, c.X, c.Y);
        if (MathF.Abs(area) < .00001f)
            return;
        var minX = Math.Clamp((int)MathF.Floor(MathF.Min(a.X, MathF.Min(b.X, c.X))), 0, width - 1);
        var maxX = Math.Clamp((int)MathF.Ceiling(MathF.Max(a.X, MathF.Max(b.X, c.X))), 0, width - 1);
        var minY = Math.Clamp((int)MathF.Floor(MathF.Min(a.Y, MathF.Min(b.Y, c.Y))), 0, height - 1);
        var maxY = Math.Clamp((int)MathF.Ceiling(MathF.Max(a.Y, MathF.Max(b.Y, c.Y))), 0, height - 1);
        var solidColor = triangle.FillColor.ToArgb();
        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var sampleX = x + .5f;
            var sampleY = y + .5f;
            var weightA = Edge(b, c, sampleX, sampleY) / area;
            var weightB = Edge(c, a, sampleX, sampleY) / area;
            var weightC = 1f - weightA - weightB;
            if (weightA < -.00001f || weightB < -.00001f || weightC < -.00001f)
                continue;
            var depth = InterpolateDepth(triangle, weightA, weightB, weightC);
            var pixelIndex = y * width + x;
            var color = solidColor;
            if (triangle.BaseColorTexture is not null)
            {
                var uv = triangle.UvA * weightA + triangle.UvB * weightB + triangle.UvC * weightC;
                color = MultiplyColor(triangle.FillColor, SampleTextureStack(triangle.BaseColorTexture, uv)).ToArgb();
            }
            if (triangle.OpacityTexture is not null)
            {
                var uv = triangle.UvA * weightA + triangle.UvB * weightB + triangle.UvC * weightC;
                var opacitySample = SampleTextureStack(triangle.OpacityTexture, uv);
                var opacity = Math.Clamp((opacitySample.X + opacitySample.Y + opacitySample.Z) / 3f *
                                         opacitySample.W, 0f, 1f);
                var sampled = Color.FromArgb(color);
                color = Color.FromArgb(Math.Clamp((int)MathF.Round(sampled.A * opacity), 0, 255),
                    sampled.R, sampled.G, sampled.B).ToArgb();
            }
            if (triangle.UseAlphaCutoff && Color.FromArgb(color).A < triangle.AlphaCutoff * 255f)
                continue;
            if (blend)
            {
                if (depth > depths[pixelIndex] + Math.Max(.001f, depth * .001f))
                    continue;
                pixels[pixelIndex] = BlendOver(color, pixels[pixelIndex]);
            }
            else
            {
                if (depth >= depths[pixelIndex])
                    continue;
                depths[pixelIndex] = depth;
                pixels[pixelIndex] = color;
            }
        }
    }

    private void RasterizeTriangleEdges(RenderTriangle triangle, Point origin, int width, int height,
        int[] pixels, float[] depths)
    {
        var color = triangle.HasCollisionWarning
            ? Color.FromArgb(255, 245, 62, 62).ToArgb()
            : triangle.IsPlacementPreview
                ? Color.FromArgb(255, 64, 225, 112).ToArgb()
            : triangle.IsSelected
                ? Color.FromArgb(255, 255, 174, 66).ToArgb()
                : Color.FromArgb(255, 26, 31, 36).ToArgb();
        var thickness = triangle.IsPlacementPreview || triangle.HasCollisionWarning ? 2 : 1;
        RasterizeEdge(triangle.A, triangle.DepthA, triangle.B, triangle.DepthB, origin,
            width, height, pixels, depths, color, thickness);
        RasterizeEdge(triangle.B, triangle.DepthB, triangle.C, triangle.DepthC, origin,
            width, height, pixels, depths, color, thickness);
        RasterizeEdge(triangle.C, triangle.DepthC, triangle.A, triangle.DepthA, origin,
            width, height, pixels, depths, color, thickness);
    }

    private void RasterizeEdge(PointF first, float firstDepth, PointF second, float secondDepth, Point origin,
        int width, int height, int[] pixels, float[] depths, int color, int thickness)
    {
        var x0 = first.X - origin.X;
        var y0 = first.Y - origin.Y;
        var x1 = second.X - origin.X;
        var y1 = second.Y - origin.Y;
        var steps = Math.Max(1, (int)MathF.Ceiling(MathF.Max(MathF.Abs(x1 - x0), MathF.Abs(y1 - y0))));
        for (var step = 0; step <= steps; step++)
        {
            var amount = step / (float)steps;
            var x = (int)MathF.Round(x0 + (x1 - x0) * amount);
            var y = (int)MathF.Round(y0 + (y1 - y0) * amount);
            var depth = _viewKind == InteriorViewportKind.Perspective
                ? 1f / ((1f - amount) / firstDepth + amount / secondDepth)
                : firstDepth + (secondDepth - firstDepth) * amount;
            for (var offsetY = -(thickness - 1); offsetY <= thickness - 1; offsetY++)
            for (var offsetX = -(thickness - 1); offsetX <= thickness - 1; offsetX++)
            {
                var targetX = x + offsetX;
                var targetY = y + offsetY;
                if ((uint)targetX >= (uint)width || (uint)targetY >= (uint)height)
                    continue;
                var pixelIndex = targetY * width + targetX;
                var tolerance = Math.Max(.001f, depth * .002f);
                if (depth <= depths[pixelIndex] + tolerance)
                    pixels[pixelIndex] = color;
            }
        }
    }

    private float InterpolateDepth(RenderTriangle triangle, float weightA, float weightB, float weightC)
    {
        if (_viewKind != InteriorViewportKind.Perspective)
            return weightA * triangle.DepthA + weightB * triangle.DepthB + weightC * triangle.DepthC;
        var inverseDepth = weightA / triangle.DepthA + weightB / triangle.DepthB + weightC / triangle.DepthC;
        return inverseDepth <= 0f ? float.PositiveInfinity : 1f / inverseDepth;
    }

    private static float Edge(PointF first, PointF second, float x, float y) =>
        (x - first.X) * (second.Y - first.Y) - (y - first.Y) * (second.X - first.X);

    private static int BlendOver(int sourceArgb, int destinationArgb)
    {
        var source = Color.FromArgb(sourceArgb);
        var destination = Color.FromArgb(destinationArgb);
        var sourceAlpha = source.A / 255f;
        var destinationAlpha = destination.A / 255f;
        var outputAlpha = sourceAlpha + destinationAlpha * (1f - sourceAlpha);
        if (outputAlpha <= .0001f)
            return 0;
        var red = (source.R * sourceAlpha + destination.R * destinationAlpha * (1f - sourceAlpha)) / outputAlpha;
        var green = (source.G * sourceAlpha + destination.G * destinationAlpha * (1f - sourceAlpha)) / outputAlpha;
        var blue = (source.B * sourceAlpha + destination.B * destinationAlpha * (1f - sourceAlpha)) / outputAlpha;
        return Color.FromArgb(
            Math.Clamp((int)MathF.Round(outputAlpha * 255f), 0, 255),
            Math.Clamp((int)MathF.Round(red), 0, 255),
            Math.Clamp((int)MathF.Round(green), 0, 255),
            Math.Clamp((int)MathF.Round(blue), 0, 255)).ToArgb();
    }

    private static Bitmap CreateBitmap(int width, int height, int[] pixels)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            if (data.Stride == width * sizeof(int))
                Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            else
                for (var row = 0; row < height; row++)
                    Marshal.Copy(pixels, row * width, IntPtr.Add(data.Scan0, row * data.Stride), width);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
        return bitmap;
    }

    private List<RenderTriangle> BuildRenderTriangles(Rectangle viewport)
    {
        return BuildRenderTriangles(viewport, CreateProjectionContext(viewport));
    }

    private List<RenderTriangle> BuildRenderTriangles(Rectangle viewport, ProjectionContext projection)
    {
        Camera.Validate();
        var result = new List<RenderTriangle>();
        Span<ClippedTriangle> clippedTriangles = stackalloc ClippedTriangle[2];
        foreach (var model in _sceneModels.Where(item => item.IsVisible))
        {
            var meshInstances = SceneTraversal.GetMeshInstances(model).ToArray();
            var useInteractivePreviewBudget = _placementModelId == model.Id ||
                                              _gizmoDragging && _primarySelectedModelId == model.Id;
            var modelTriangleCount = useInteractivePreviewBudget
                ? meshInstances.Sum(instance => model.Meshes[instance.MeshIndex].Indices.Length / 3)
                : 0;
            var triangleStep = modelTriangleCount > PlacementPreviewTriangleBudget
                ? (int)Math.Ceiling(modelTriangleCount / (double)PlacementPreviewTriangleBudget)
                : 1;
            foreach (var instance in meshInstances)
            {
                var meshIndex = instance.MeshIndex;
                if (model.HiddenMeshIndices.Contains(meshIndex))
                    continue;
                var mesh = model.Meshes[meshIndex];
                var worldMatrix = SceneTraversal.GetMeshWorldTransform(model, instance);
                var baseColor = GetMaterialColor(model, mesh.MaterialIndex);
                var baseColorTexture = GetBaseColorTexture(model, mesh.MaterialIndex);
                var opacityTexture = GetTextureStack(model, mesh.MaterialIndex, TextureSemantic.Opacity);
                var textureMapping = baseColorTexture?.Layers.LastOrDefault(layer => layer.Enabled)?.Mapping;
                var material = (uint)mesh.MaterialIndex < (uint)model.Materials.Count
                    ? model.Materials[mesh.MaterialIndex]
                    : null;
                var isSelected = _selectedModelIds.Contains(model.Id) &&
                                 (model.Id != _primarySelectedModelId ||
                                  _selectedMeshIndex is null || _selectedMeshIndex == meshIndex);
                var isOccludingStructure = IsWallOrCeiling(model, mesh);
                if (_visibilityMode == InteriorVisibilityMode.AutoHideForegroundWalls &&
                    !_selectedModelIds.Contains(model.Id) && isOccludingStructure &&
                    ShouldHideForegroundStructure(mesh, worldMatrix, projection))
                    continue;
                var transparent = material?.RequiresAlphaBlending == true ||
                                  _visibilityMode == InteriorVisibilityMode.TransparentOccluders &&
                                  !_selectedModelIds.Contains(model.Id) && isOccludingStructure;
                if (_placementModelId == model.Id)
                {
                    transparent = true;
                    baseColor = _placementHasCollision
                        ? Color.FromArgb(132, 235, 72, 72)
                        : Color.FromArgb(126, 72, 210, 118);
                    isSelected = true;
                }
                if (transparent)
                    baseColor = Color.FromArgb(_placementModelId == model.Id ? 118 : 58,
                        baseColor.R, baseColor.G, baseColor.B);
                var doubleSided = (uint)mesh.MaterialIndex < (uint)model.Materials.Count &&
                                  model.Materials[mesh.MaterialIndex].DoubleSided;
                for (var index = 0; index + 2 < mesh.Indices.Length; index += 3 * triangleStep)
                {
                    var i0 = mesh.Indices[index];
                    var i1 = mesh.Indices[index + 1];
                    var i2 = mesh.Indices[index + 2];
                    if (i0 >= mesh.Positions.Length || i1 >= mesh.Positions.Length || i2 >= mesh.Positions.Length)
                        continue;
                    var local0 = mesh.Positions[i0];
                    var local1 = mesh.Positions[i1];
                    var local2 = mesh.Positions[i2];
                    var p0 = Vector3.Transform(local0, worldMatrix);
                    var p1 = Vector3.Transform(local1, worldMatrix);
                    var p2 = Vector3.Transform(local2, worldMatrix);
                    var edge1 = p1 - p0;
                    var edge2 = p2 - p0;
                    var geometricNormal = Vector3.Cross(edge1, edge2);
                    var edgeScaleSquared = MathF.Max(edge1.LengthSquared(), edge2.LengthSquared());
                    var relativeAreaTolerance = edgeScaleSquared * edgeScaleSquared * 1e-12f;
                    var geometricNormalLengthSquared = geometricNormal.LengthSquared();
                    if (!float.IsFinite(geometricNormalLengthSquared) ||
                        geometricNormalLengthSquared <= MathF.Max(float.Epsilon, relativeAreaTolerance))
                        continue;
                    var normal = GetVisualNormal(mesh, i0, i1, i2, worldMatrix, geometricNormal);
                    var center = (p0 + p1 + p2) / 3f;
                    // The normal magnitude is proportional to triangle area.  Comparing the dot product
                    // with a fixed epsilon incorrectly culls every face of centimetre-scale detailed models.
                    // Only its sign is relevant for back-face culling.
                    if (!doubleSided && Vector3.Dot(geometricNormal, Camera.From - center) <= 0f)
                        continue;
                    var light = .38f + .62f * MathF.Abs(Vector3.Dot(normal, Vector3.Normalize(new Vector3(.35f, .8f, .45f))));
                    var uv0 = GetPreviewUv(mesh, i0, local0, p0, normal, textureMapping);
                    var uv1 = GetPreviewUv(mesh, i1, local1, p1, normal, textureMapping);
                    var uv2 = GetPreviewUv(mesh, i2, local2, p2, normal, textureMapping);
                    var clippedCount = ClipToNearPlane(p0, p1, p2, uv0, uv1, uv2, projection,
                        clippedTriangles);
                    for (var clippedIndex = 0; clippedIndex < clippedCount; clippedIndex++)
                    {
                        var clipped = clippedTriangles[clippedIndex];
                        var projected0 = Project(clipped.A.World, viewport, projection);
                        var projected1 = Project(clipped.B.World, viewport, projection);
                        var projected2 = Project(clipped.C.World, viewport, projection);
                        if (projected0 is null || projected1 is null || projected2 is null)
                            continue;
                        result.Add(new RenderTriangle(projected0.Value.Point, projected1.Value.Point,
                            projected2.Value.Point, projected0.Value.Depth, projected1.Value.Depth,
                            projected2.Value.Depth, Shade(baseColor, light), isSelected,
                            transparent || baseColor.A < 250, _placementModelId == model.Id,
                            (_placementModelId == model.Id && _placementHasCollision) ||
                            _collisionWarningModelIds.Contains(model.Id), model.Id, baseColorTexture,
                            opacityTexture, material?.RenderMode == MaterialRenderMode.Cutout,
                            material?.AlphaCutoff ?? .5f, clipped.A.Uv, clipped.B.Uv, clipped.C.Uv));
                    }
                }
            }
        }
        return result;
    }

    private int ClipToNearPlane(Vector3 p0, Vector3 p1, Vector3 p2,
        Vector2 uv0, Vector2 uv1, Vector2 uv2, ProjectionContext projection,
        Span<ClippedTriangle> triangles)
    {
        if (_viewKind != InteriorViewportKind.Perspective)
        {
            triangles[0] = new ClippedTriangle(new ClippedVertex(p0, uv0), new ClippedVertex(p1, uv1),
                new ClippedVertex(p2, uv2));
            return 1;
        }

        var threshold = Camera.NearPlane + Math.Max(.00001f, Camera.NearPlane * .0001f);
        Span<ClippedVertex> input = stackalloc ClippedVertex[3];
        input[0] = new ClippedVertex(p0, uv0);
        input[1] = new ClippedVertex(p1, uv1);
        input[2] = new ClippedVertex(p2, uv2);
        Span<ClippedVertex> output = stackalloc ClippedVertex[4];
        var outputCount = 0;
        for (var index = 0; index < input.Length; index++)
        {
            var current = input[index];
            var previous = input[(index + input.Length - 1) % input.Length];
            var currentDepth = Vector3.Dot(current.World - Camera.From, projection.Forward);
            var previousDepth = Vector3.Dot(previous.World - Camera.From, projection.Forward);
            var currentInside = currentDepth >= threshold;
            var previousInside = previousDepth >= threshold;
            if (currentInside != previousInside)
            {
                var amount = (threshold - previousDepth) / (currentDepth - previousDepth);
                output[outputCount++] = new ClippedVertex(Vector3.Lerp(previous.World, current.World, amount),
                    Vector2.Lerp(previous.Uv, current.Uv, amount));
            }
            if (currentInside)
                output[outputCount++] = current;
        }
        if (outputCount == 3)
        {
            triangles[0] = new ClippedTriangle(output[0], output[1], output[2]);
            return 1;
        }
        if (outputCount == 4)
        {
            triangles[0] = new ClippedTriangle(output[0], output[1], output[2]);
            triangles[1] = new ClippedTriangle(output[0], output[2], output[3]);
            return 2;
        }
        return 0;
    }

    private static bool IsWallOrCeiling(SceneModel model, MeshData mesh)
    {
        var documentType = model.Extensions.TryGetValue(RVInteriorDesignTestSceneFactory.ExtensionKey, out var extension) &&
                           extension.TryGetProperty("documentType", out var type)
            ? type.GetString()
            : null;
        return documentType is "parametric-wall" or "parametric-ceiling" ||
               mesh.Name.Contains('牆') || mesh.Name.Contains("天花板", StringComparison.Ordinal);
    }

    private bool ShouldHideForegroundStructure(MeshData mesh, Matrix4x4 worldMatrix,
        ProjectionContext projection)
    {
        if (_viewKind == InteriorViewportKind.Top && !mesh.Name.Contains("天花板", StringComparison.Ordinal))
            return false;
        if (mesh.Positions.Length == 0)
            return false;
        var center = Vector3.Zero;
        foreach (var position in mesh.Positions)
            center += Vector3.Transform(position, worldMatrix);
        center /= mesh.Positions.Length;
        var depth = Vector3.Dot(center - Camera.From, projection.Forward);
        return depth < projection.Distance - .05f;
    }

    private static Vector3 GetVisualNormal(MeshData mesh, uint i0, uint i1, uint i2,
        Matrix4x4 worldMatrix, Vector3 geometricNormal)
    {
        if (i0 < (uint)mesh.Normals.Length && i1 < (uint)mesh.Normals.Length && i2 < (uint)mesh.Normals.Length)
        {
            var localNormal = mesh.Normals[i0] + mesh.Normals[i1] + mesh.Normals[i2];
            if (localNormal.LengthSquared() > .0000001f)
            {
                var normalMatrix = Matrix4x4.Invert(worldMatrix, out var inverse)
                    ? Matrix4x4.Transpose(inverse)
                    : worldMatrix;
                var transformed = Vector3.TransformNormal(localNormal, normalMatrix);
                if (transformed.LengthSquared() > .0000001f)
                    return Vector3.Normalize(transformed);
            }
        }
        return Vector3.Normalize(geometricNormal);
    }

    private ProjectedPoint? Project(Vector3 world, Rectangle viewport, ProjectionContext projection)
    {
        var relative = world - Camera.From;
        var depth = Vector3.Dot(relative, projection.Forward);
        var centerX = viewport.Left + viewport.Width / 2f;
        var centerY = viewport.Top + viewport.Height / 2f;
        if (_viewKind == InteriorViewportKind.Perspective)
        {
            if (depth <= Camera.NearPlane)
                return null;
            return new ProjectedPoint(new PointF(
                centerX + Vector3.Dot(relative, projection.Right) * projection.FocalLength / depth,
                centerY - Vector3.Dot(relative, projection.Up) * projection.FocalLength / depth), depth);
        }

        var fromTarget = world - Camera.To;
        return new ProjectedPoint(new PointF(
            centerX + Vector3.Dot(fromTarget, projection.Right) * projection.PixelsPerMeter,
            centerY - Vector3.Dot(fromTarget, projection.Up) * projection.PixelsPerMeter), depth);
    }

    private ProjectionContext CreateProjectionContext(Rectangle viewport)
    {
        Camera.Validate();
        var forward = Vector3.Normalize(Camera.To - Camera.From);
        var right = Vector3.Normalize(Vector3.Cross(forward, Camera.Up));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var distance = Math.Max(Vector3.Distance(Camera.From, Camera.To), .1f);
        var focalLength = viewport.Height <= 0 ? 1f : viewport.Height /
            (2f * MathF.Tan(Camera.FieldOfViewDegrees * MathF.PI / 360f));
        var pixelsPerMeter = _viewKind == InteriorViewportKind.Perspective
            ? focalLength / distance
            : Math.Min(viewport.Width, viewport.Height) / (distance * 1.25f);
        return new ProjectionContext(forward, right, up, distance, focalLength, Math.Max(pixelsPerMeter, .001f));
    }

    internal GridMetrics GetGridMetrics(Size viewportSize)
    {
        var viewport = new Rectangle(0, headerPanel.Height, viewportSize.Width,
            Math.Max(1, viewportSize.Height - headerPanel.Height));
        var projection = CreateProjectionContext(viewport);
        var origin = Project(Vector3.Zero, viewport, projection)?.Point ?? PointF.Empty;
        return new GridMetrics(projection.PixelsPerMeter, GetAdaptiveGridStep(projection.PixelsPerMeter), origin);
    }

    private void DrawWorldGrid(Graphics graphics, Rectangle viewport, ProjectionContext projection)
    {
        if (viewport.Width <= 1 || viewport.Height <= 1)
            return;
        var step = GetAdaptiveGridStep(projection.PixelsPerMeter);
        var extent = Math.Max(projection.Distance * 2.5f, step * 24f);
        var horizontalCenter = _viewKind == InteriorViewportKind.Right ? Camera.To.Z : Camera.To.X;
        var verticalCenter = _viewKind is InteriorViewportKind.Top or InteriorViewportKind.Perspective
            ? Camera.To.Z
            : Camera.To.Y;
        var horizontalStart = MathF.Floor(horizontalCenter / step) * step;
        var verticalStart = MathF.Floor(verticalCenter / step) * step;
        var halfLines = Math.Min(60, (int)MathF.Ceiling(extent / step));
        using var minorPen = new Pen(Color.FromArgb(42, 111, 128, 146));
        using var majorPen = new Pen(Color.FromArgb(82, 135, 155, 176));
        using var firstAxisPen = new Pen(Color.IndianRed, 2f);
        using var secondAxisPen = new Pen(Color.SeaGreen, 2f);
        var previousClip = graphics.Clip;
        graphics.SetClip(viewport);
        for (var offset = -halfLines; offset <= halfLines; offset++)
        {
            var coordinate = horizontalStart + offset * step;
            var pen = MathF.Abs(coordinate) < step * .1f ? firstAxisPen :
                IsMajorGridLine(coordinate, step) ? majorPen : minorPen;
            DrawGridLine(graphics, viewport, projection, pen, coordinate, verticalCenter - extent,
                coordinate, verticalCenter + extent);
        }
        for (var offset = -halfLines; offset <= halfLines; offset++)
        {
            var coordinate = verticalStart + offset * step;
            var pen = MathF.Abs(coordinate) < step * .1f ? secondAxisPen :
                IsMajorGridLine(coordinate, step) ? majorPen : minorPen;
            DrawGridLine(graphics, viewport, projection, pen, horizontalCenter - extent, coordinate,
                horizontalCenter + extent, coordinate);
        }
        graphics.Clip = previousClip;
    }

    private void DrawGridLine(Graphics graphics, Rectangle viewport, ProjectionContext projection, Pen pen,
        float h1, float v1, float h2, float v2)
    {
        var first = GridPoint(h1, v1);
        var second = GridPoint(h2, v2);
        if (_viewKind == InteriorViewportKind.Perspective &&
            !ClipToNearPlane(ref first, ref second, projection))
            return;
        var projectedFirst = Project(first, viewport, projection);
        var projectedSecond = Project(second, viewport, projection);
        if (projectedFirst is not null && projectedSecond is not null)
            graphics.DrawLine(pen, projectedFirst.Value.Point, projectedSecond.Value.Point);
    }

    private bool ClipToNearPlane(ref Vector3 first, ref Vector3 second, ProjectionContext projection)
    {
        var minimumDepth = Math.Max(Camera.NearPlane * 1.01f, projection.Distance * .05f);
        var firstDepth = Vector3.Dot(first - Camera.From, projection.Forward);
        var secondDepth = Vector3.Dot(second - Camera.From, projection.Forward);
        if (firstDepth < minimumDepth && secondDepth < minimumDepth)
            return false;
        if (firstDepth < minimumDepth)
            first = Vector3.Lerp(first, second, (minimumDepth - firstDepth) / (secondDepth - firstDepth));
        else if (secondDepth < minimumDepth)
            second = Vector3.Lerp(second, first, (minimumDepth - secondDepth) / (firstDepth - secondDepth));
        return true;
    }

    private Vector3 GridPoint(float horizontal, float vertical) => _viewKind switch
    {
        InteriorViewportKind.Front => new Vector3(horizontal, vertical, 0f),
        InteriorViewportKind.Right => new Vector3(0f, vertical, horizontal),
        _ => new Vector3(horizontal, 0f, vertical)
    };

    private static bool IsMajorGridLine(float coordinate, float step)
    {
        var index = (int)MathF.Round(coordinate / step);
        return index % MajorGridInterval == 0;
    }

    private static float GetAdaptiveGridStep(float pixelsPerMeter)
    {
        var desiredWorldStep = 28f / Math.Max(pixelsPerMeter, .001f);
        var magnitude = MathF.Pow(10f, MathF.Floor(MathF.Log10(desiredWorldStep)));
        var normalized = desiredWorldStep / magnitude;
        var nice = normalized <= 1f ? 1f : normalized <= 2f ? 2f : normalized <= 5f ? 5f : 10f;
        return Math.Max(GridSpacingMeters, nice * magnitude);
    }

    private static Color GetMaterialColor(SceneModel model, int materialIndex)
    {
        var material = (uint)materialIndex < (uint)model.Materials.Count
            ? model.Materials[materialIndex]
            : null;
        var color = material?.BaseColor ?? new Vector4(.72f, .76f, .82f, 1f);
        var opacity = material?.Opacity ?? 1f;
        return Color.FromArgb(
            Math.Clamp((int)(color.W * opacity * 255f), 0, 255),
            Math.Clamp((int)(color.X * 255f), 0, 255),
            Math.Clamp((int)(color.Y * 255f), 0, 255),
            Math.Clamp((int)(color.Z * 255f), 0, 255));
    }

    private static TextureStack? GetBaseColorTexture(SceneModel model, int materialIndex)
        => GetTextureStack(model, materialIndex, TextureSemantic.BaseColor);

    private static TextureStack? GetTextureStack(SceneModel model, int materialIndex, TextureSemantic semantic)
    {
        if ((uint)materialIndex >= (uint)model.Materials.Count)
            return null;
        return model.Materials[materialIndex].TextureStacks.TryGetValue(semantic, out var stack) &&
               stack is { Enabled: true } && stack.Layers.Any(layer => layer.Enabled)
            ? stack
            : null;
    }

    private static Vector2 GetPreviewUv(MeshData mesh, uint vertexIndex, Vector3 localPosition,
        Vector3 worldPosition, Vector3 normal, TextureMappingSettings? mapping)
    {
        if (mapping is null || mapping.Mode == TextureMappingMode.Uv)
            return vertexIndex < (uint)mesh.TextureCoordinates.Length
                ? mesh.TextureCoordinates[vertexIndex]
                : Vector2.Zero;
        var position = mapping.Space == TextureProjectionSpace.World ? worldPosition : localPosition;
        var axis = mapping.Axis;
        if (axis == TextureProjectionAxis.Auto)
        {
            var absolute = Vector3.Abs(normal);
            axis = absolute.X > absolute.Y && absolute.X > absolute.Z
                ? TextureProjectionAxis.X
                : absolute.Y > absolute.Z ? TextureProjectionAxis.Y : TextureProjectionAxis.Z;
        }
        return axis switch
        {
            TextureProjectionAxis.X => new Vector2(position.Z, position.Y),
            TextureProjectionAxis.Y => new Vector2(position.X, position.Z),
            _ => new Vector2(position.X, position.Y)
        };
    }

    private Vector4 SampleTextureStack(TextureStack stack, Vector2 uv)
    {
        var destination = Vector4.Zero;
        foreach (var layer in stack.Layers.Where(layer => layer.Enabled).Take(8))
        {
            var transformed = TransformTextureUv(uv, layer.Transform);
            var source = layer.Kind switch
            {
                TextureLayerKind.SolidColor => layer.Color,
                TextureLayerKind.Checker => (((int)MathF.Floor(transformed.X * 8f) +
                                              (int)MathF.Floor(transformed.Y * 8f)) & 1) == 0
                    ? layer.Color
                    : new Vector4(Vector3.One - new Vector3(layer.Color.X, layer.Color.Y, layer.Color.Z),
                        layer.Color.W),
                TextureLayerKind.Gradient => Vector4.Lerp(new Vector4(0f, 0f, 0f, layer.Color.W),
                    layer.Color, WrapTextureCoordinate(transformed.Y)),
                TextureLayerKind.Noise => new Vector4(Vector3.One * TextureNoise(transformed), layer.Color.W) *
                                          layer.Color,
                TextureLayerKind.Image => SampleImageTexture(layer, transformed),
                _ => Vector4.One
            };
            destination = BlendTexture(destination, source,
                Math.Clamp(source.W * layer.Opacity, 0f, 1f), layer.BlendMode);
        }
        return destination.W <= .0001f ? Vector4.One : destination;
    }

    private Vector4 SampleImageTexture(TextureLayer layer, Vector2 uv)
    {
        if (string.IsNullOrWhiteSpace(layer.Path))
            return Vector4.One;
        var path = Path.GetFullPath(layer.Path);
        if (!_previewTextureCache.TryGetValue(path, out var image))
        {
            if (_failedPreviewTexturePaths.Contains(path))
                return Vector4.One;
            image = LoadPreviewTexture(path);
            if (image is null)
            {
                _failedPreviewTexturePaths.Add(path);
                return Vector4.One;
            }
            _previewTextureCache[path] = image;
        }
        var u = WrapTextureCoordinate(uv.X, layer.Sampling.Wrap);
        var v = WrapTextureCoordinate(uv.Y, layer.Sampling.Wrap);
        var x = Math.Clamp((int)MathF.Round(u * (image.Width - 1)), 0, image.Width - 1);
        var y = Math.Clamp((int)MathF.Round((1f - v) * (image.Height - 1)), 0, image.Height - 1);
        var color = Color.FromArgb(image.Pixels[y * image.Width + x]);
        var sampled = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        var tint = layer.Channels.Tint;
        sampled *= tint;
        if (layer.Channels.Invert)
            sampled = new Vector4(Vector3.One - new Vector3(sampled.X, sampled.Y, sampled.Z), sampled.W);
        return Vector4.Clamp(sampled, Vector4.Zero, Vector4.One);
    }

    private static PreviewTextureImage? LoadPreviewTexture(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            var bytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(bytes, writable: false);
            using var decoded = Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: true);
            using var source = new Bitmap(decoded);
            const int maximumDimension = 512;
            var scale = Math.Min(1f, maximumDimension / (float)Math.Max(source.Width, source.Height));
            var width = Math.Max(1, (int)MathF.Round(source.Width * scale));
            var height = Math.Max(1, (int)MathF.Round(source.Height * scale));
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, new Rectangle(0, 0, width, height));
            }
            var pixels = new int[width * height];
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                for (var row = 0; row < height; row++)
                    Marshal.Copy(IntPtr.Add(data.Scan0, row * data.Stride), pixels, row * width, width);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            return new PreviewTextureImage(width, height, pixels);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                           ArgumentException or OutOfMemoryException)
        {
            return null;
        }
    }

    private void TrimPreviewTextureCache(IEnumerable<SceneModel> models)
    {
        var activePaths = models.SelectMany(model => model.Materials)
            .SelectMany(material => material.TextureStacks.Values)
            .SelectMany(stack => stack.Layers)
            .Where(layer => layer.Enabled && layer.Kind == TextureLayerKind.Image &&
                            !string.IsNullOrWhiteSpace(layer.Path))
            .Select(layer => Path.GetFullPath(layer.Path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in _previewTextureCache.Keys.Where(path => !activePaths.Contains(path)).ToArray())
            _previewTextureCache.Remove(path);
        _failedPreviewTexturePaths.RemoveWhere(path => !activePaths.Contains(path));
    }

    private static float WrapTextureCoordinate(float value, TextureWrap wrap) => wrap switch
    {
        TextureWrap.ClampToEdge => Math.Clamp(value, 0f, 1f),
        TextureWrap.MirroredRepeat => MathF.Abs(value % 2f) is var mirrored && mirrored > 1f
            ? 2f - mirrored
            : mirrored,
        _ => value - MathF.Floor(value)
    };

    private static Vector2 TransformTextureUv(Vector2 uv, TextureTransformSettings transform)
    {
        if (transform.SwapUv) uv = new Vector2(uv.Y, uv.X);
        if (transform.FlipX) uv.X = 1f - uv.X;
        if (transform.FlipY) uv.Y = 1f - uv.Y;
        var pivot = new Vector2(transform.PivotX, transform.PivotY);
        uv = (uv - pivot) * new Vector2(transform.ScaleX, transform.ScaleY);
        var radians = transform.RotationDegrees * MathF.PI / 180f;
        var cosine = MathF.Cos(radians);
        var sine = MathF.Sin(radians);
        uv = new Vector2(uv.X * cosine - uv.Y * sine, uv.X * sine + uv.Y * cosine);
        return uv + pivot + new Vector2(transform.OffsetX, transform.OffsetY);
    }

    private static Vector4 BlendTexture(Vector4 destination, Vector4 source, float alpha,
        TextureBlendMode blendMode)
    {
        var destinationRgb = new Vector3(destination.X, destination.Y, destination.Z);
        var sourceRgb = new Vector3(source.X, source.Y, source.Z);
        var mixed = blendMode switch
        {
            TextureBlendMode.Multiply => destinationRgb * sourceRgb,
            TextureBlendMode.Add => Vector3.Min(Vector3.One, destinationRgb + sourceRgb),
            TextureBlendMode.Subtract => Vector3.Max(Vector3.Zero, destinationRgb - sourceRgb),
            TextureBlendMode.Screen => Vector3.One - (Vector3.One - destinationRgb) * (Vector3.One - sourceRgb),
            TextureBlendMode.Overlay => new Vector3(
                OverlayTexture(destinationRgb.X, sourceRgb.X),
                OverlayTexture(destinationRgb.Y, sourceRgb.Y),
                OverlayTexture(destinationRgb.Z, sourceRgb.Z)),
            TextureBlendMode.Lighten => Vector3.Max(destinationRgb, sourceRgb),
            TextureBlendMode.Darken => Vector3.Min(destinationRgb, sourceRgb),
            _ => sourceRgb
        };
        return new Vector4(Vector3.Lerp(destinationRgb, mixed, alpha),
            alpha + destination.W * (1f - alpha));
    }

    private static Color MultiplyColor(Color color, Vector4 texture) => Color.FromArgb(
        Math.Clamp((int)MathF.Round(color.A * texture.W), 0, 255),
        Math.Clamp((int)MathF.Round(color.R * Math.Clamp(texture.X, 0f, 1f)), 0, 255),
        Math.Clamp((int)MathF.Round(color.G * Math.Clamp(texture.Y, 0f, 1f)), 0, 255),
        Math.Clamp((int)MathF.Round(color.B * Math.Clamp(texture.Z, 0f, 1f)), 0, 255));

    private static float WrapTextureCoordinate(float value) => value - MathF.Floor(value);

    private static float OverlayTexture(float destination, float source) => destination < .5f
        ? 2f * destination * source
        : 1f - 2f * (1f - destination) * (1f - source);

    private static float TextureNoise(Vector2 uv)
    {
        var value = MathF.Sin(Vector2.Dot(uv, new Vector2(12.9898f, 78.233f))) * 43758.5453f;
        return value - MathF.Floor(value);
    }

    private static Color Shade(Color color, float intensity) => Color.FromArgb(color.A,
        Math.Clamp((int)(color.R * intensity), 0, 255),
        Math.Clamp((int)(color.G * intensity), 0, 255),
        Math.Clamp((int)(color.B * intensity), 0, 255));

    private readonly record struct ProjectedPoint(PointF Point, float Depth);
    private readonly record struct ClippedVertex(Vector3 World, Vector2 Uv);
    private readonly record struct ClippedTriangle(ClippedVertex A, ClippedVertex B, ClippedVertex C);
    private readonly record struct RenderTriangle(PointF A, PointF B, PointF C,
        float DepthA, float DepthB, float DepthC, Color FillColor, bool IsSelected, bool IsTransparent,
        bool IsPlacementPreview, bool HasCollisionWarning,
        Guid ModelId, TextureStack? BaseColorTexture, TextureStack? OpacityTexture,
        bool UseAlphaCutoff, float AlphaCutoff, Vector2 UvA, Vector2 UvB, Vector2 UvC)
    {
        public float AverageDepth => (DepthA + DepthB + DepthC) / 3f;
    }
    private sealed record PreviewTextureImage(int Width, int Height, int[] Pixels);
    private readonly record struct ProjectionContext(Vector3 Forward, Vector3 Right, Vector3 Up, float Distance,
        float FocalLength, float PixelsPerMeter);
    internal readonly record struct GridMetrics(float PixelsPerMeter, float StepMeters, PointF WorldOrigin);

    private void DrawScaleBar(Graphics graphics, float pixelsPerMeter)
    {
        const int subdivisions = 5;
        var scaleLength = GetNiceScaleLength(100f / pixelsPerMeter);
        var barWidth = Math.Max(30, (int)MathF.Round(scaleLength * pixelsPerMeter));
        var subdivisionWidth = barWidth / (float)subdivisions;
        const int barHeight = 8;
        const int left = 14;
        var top = Math.Max(headerPanel.Bottom + 8, Height - 42);
        using var backgroundBrush = new SolidBrush(Color.FromArgb(190, 20, 24, 29));
        using var lightBrush = new SolidBrush(Color.FromArgb(225, 235, 238, 241));
        using var darkBrush = new SolidBrush(Color.FromArgb(225, 70, 78, 87));
        using var borderPen = new Pen(Color.FromArgb(230, 235, 238, 241));
        using var scaleFont = new Font(Font.FontFamily, Math.Max(8f, Font.Size), FontStyle.Regular);
        graphics.FillRectangle(backgroundBrush, left - 7, top - 18, barWidth + 82, 36);

        for (var index = 0; index < subdivisions; index++)
        {
            var x1 = left + (int)MathF.Round(index * subdivisionWidth);
            var x2 = left + (int)MathF.Round((index + 1) * subdivisionWidth);
            var rectangle = new Rectangle(x1, top, Math.Max(1, x2 - x1), barHeight);
            graphics.FillRectangle(index % 2 == 0 ? lightBrush : darkBrush, rectangle);
        }
        graphics.DrawRectangle(borderPen, left, top, barWidth, barHeight);
        for (var index = 0; index <= subdivisions; index++)
        {
            var x = left + (int)MathF.Round(index * subdivisionWidth);
            graphics.DrawLine(borderPen, x, top - 3, x, top + barHeight + 3);
        }

        TextRenderer.DrawText(graphics, FormatDistance(scaleLength), scaleFont, new Point(left, top - 17),
            Color.FromArgb(235, 238, 241), TextFormatFlags.NoPadding);
        TextRenderer.DrawText(graphics, $"每格 {FormatDistance(scaleLength / subdivisions)}", scaleFont,
            new Point(left + barWidth + 7, top - 4),
            Color.FromArgb(220, 228, 233, 238), TextFormatFlags.NoPadding);
    }

    private static float GetNiceScaleLength(float desiredMeters)
    {
        var magnitude = MathF.Pow(10f, MathF.Floor(MathF.Log10(Math.Max(desiredMeters, .0001f))));
        var normalized = desiredMeters / magnitude;
        var nice = normalized <= 1f ? 1f : normalized <= 2f ? 2f : normalized <= 5f ? 5f : 10f;
        return nice * magnitude;
    }

    private static string FormatDistance(float meters) => meters switch
    {
        >= 1f => $"{meters:0.##} m",
        >= .01f => $"{meters * 100f:0.##} cm",
        _ => $"{meters * 1000f:0.##} mm"
    };

    private void MaximizeButton_Click(object? sender, EventArgs e) => MaximizeRequested?.Invoke(this, EventArgs.Empty);

    private void Viewport_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_placementModelId is not null)
        {
            if (e.Button == MouseButtons.Right)
                PlacementCanceled?.Invoke(this, EventArgs.Empty);
            else if (e.Button == MouseButtons.Left && TryGetPlacementPointer(e.Location, out var placement))
                PlacementConfirmed?.Invoke(this, placement);
            return;
        }
        if (e.Button == MouseButtons.Left && _selectionMode != InteriorViewportSelectionMode.Navigate)
        {
            _selectionMouseDown = e.Location;
            _selectionMouseCurrent = e.Location;
            _selectionPointerDown = true;
            Capture = true;
            Focus();
            return;
        }
        if (e.Button == MouseButtons.Left && BeginGizmoDrag(e.Location))
        {
            Focus();
            return;
        }
        if (!CanNavigateWithButton(e.Button))
            return;
        _lastMouse = e.Location;
        _mouseCaptured = true;
        Capture = true;
        Focus();
    }

    private void Viewport_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_placementModelId is not null)
        {
            if (TryGetPlacementPointer(e.Location, out var placement))
                PlacementPointChanged?.Invoke(this, placement);
            return;
        }
        if (_selectionPointerDown)
        {
            _selectionMouseCurrent = e.Location;
            Invalidate();
            return;
        }
        if (_gizmoDragging)
        {
            UpdateGizmoDrag(e.Location);
            return;
        }
        if (!_mouseCaptured)
        {
            UpdateGizmoHover(e.Location);
            return;
        }
        var dx = e.X - _lastMouse.X;
        var dy = e.Y - _lastMouse.Y;
        _lastMouse = e.Location;
        if (dx == 0 && dy == 0)
            return;
        if (NavigateDrag(e.Button, dx, dy))
            NotifyCameraChanged();
    }

    private void Viewport_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _selectionPointerDown)
        {
            _selectionPointerDown = false;
            Capture = false;
            _selectionMouseCurrent = e.Location;
            var rectangle = NormalizeSelectionRectangle(_selectionMouseDown, e.Location);
            var ids = rectangle.Width <= 4 && rectangle.Height <= 4
                ? PickModel(e.Location) is Guid id ? new[] { id } : []
                : PickModels(rectangle);
            ModelPicked?.Invoke(this, new InteriorModelPickedEventArgs(ids, GetSelectionOperation(ModifierKeys)));
            Invalidate();
            return;
        }
        if (e.Button == MouseButtons.Left && _gizmoDragging)
        {
            EndGizmoDrag();
            return;
        }
        if (e.Button is not (MouseButtons.Left or MouseButtons.Middle or MouseButtons.Right))
            return;
        _mouseCaptured = false;
        Capture = false;
    }

    private void Viewport_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (_placementModelId is not null)
            return;
        if (_mouseCaptured || e.Delta == 0)
            return;
        NavigateWheel(e.Delta, ModifierKeys);
        NotifyCameraChanged();
    }

    private void Viewport_MouseEnter(object? sender, EventArgs e) => Focus();

    internal bool TryGetPlacementPointer(Point location, out InteriorPlacementPointEventArgs placement)
    {
        placement = null!;
        var viewport = new Rectangle(0, headerPanel.Bottom, Width, Height - headerPanel.Bottom);
        if (_viewKind != InteriorViewportKind.Perspective || !viewport.Contains(location))
            return false;
        var projection = CreateProjectionContext(viewport);
        var centerX = viewport.Left + viewport.Width / 2f;
        var centerY = viewport.Top + viewport.Height / 2f;
        var direction = Vector3.Normalize(projection.Forward +
            projection.Right * ((location.X - centerX) / projection.FocalLength) -
            projection.Up * ((location.Y - centerY) / projection.FocalLength));
        var hasGroundPoint = MathF.Abs(direction.Y) >= .00001f;
        var distance = hasGroundPoint ? -Camera.From.Y / direction.Y : -1f;
        hasGroundPoint &= distance > 0f;
        var point = hasGroundPoint ? Camera.From + direction * distance : Camera.To;
        if (hasGroundPoint)
        {
            point.Y = 0f;
            if (_placementSnapToGrid)
            {
                point.X = MathF.Round(point.X / GridSpacingMeters) * GridSpacingMeters;
                point.Z = MathF.Round(point.Z / GridSpacingMeters) * GridSpacingMeters;
            }
        }
        placement = new InteriorPlacementPointEventArgs(point, Camera.From, direction, hasGroundPoint);
        return true;
    }

    internal void CancelSelectionDrag()
    {
        _selectionPointerDown = false;
        Capture = false;
        Invalidate();
    }

    private InteriorSelectionOperation GetSelectionOperation(Keys modifiers)
    {
        if (_selectionMode == InteriorViewportSelectionMode.Subtract || modifiers.HasFlag(Keys.Alt))
            return InteriorSelectionOperation.Subtract;
        if (modifiers.HasFlag(Keys.Shift))
            return InteriorSelectionOperation.Add;
        if (modifiers.HasFlag(Keys.Control))
            return InteriorSelectionOperation.Toggle;
        return InteriorSelectionOperation.Replace;
    }

    private void DrawSelectionRectangle(Graphics graphics)
    {
        if (!_selectionPointerDown)
            return;
        var rectangle = NormalizeSelectionRectangle(_selectionMouseDown, _selectionMouseCurrent);
        if (rectangle.Width <= 2 && rectangle.Height <= 2)
            return;
        using var fill = new SolidBrush(Color.FromArgb(42, 72, 145, 220));
        using var border = new Pen(Color.FromArgb(235, 116, 184, 255)) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        graphics.FillRectangle(fill, rectangle);
        graphics.DrawRectangle(border, rectangle);
    }

    private static Rectangle NormalizeSelectionRectangle(Point first, Point second) => Rectangle.FromLTRB(
        Math.Min(first.X, second.X), Math.Min(first.Y, second.Y),
        Math.Max(first.X, second.X), Math.Max(first.Y, second.Y));

    private Guid? PickModel(Point location)
    {
        var viewport = new Rectangle(0, headerPanel.Bottom, Width, Height - headerPanel.Bottom);
        if (!viewport.Contains(location))
            return null;
        var hit = BuildRenderTriangles(viewport)
            .Where(triangle => PointInTriangle(location, triangle.A, triangle.B, triangle.C))
            .Select(triangle => (triangle.ModelId, Depth: TriangleDepthAt(location, triangle)))
            .OrderBy(candidate => candidate.Depth)
            .FirstOrDefault();
        return hit.ModelId == Guid.Empty ? null : hit.ModelId;
    }

    private Guid[] PickModels(Rectangle rectangle)
    {
        var viewport = new Rectangle(0, headerPanel.Bottom, Width, Height - headerPanel.Bottom);
        if (!viewport.IntersectsWith(rectangle))
            return [];
        return BuildRenderTriangles(viewport)
            .Where(triangle => TriangleIntersectsRectangle(triangle, rectangle))
            .Select(triangle => triangle.ModelId)
            .Distinct()
            .ToArray();
    }

    private static bool TriangleIntersectsRectangle(RenderTriangle triangle, Rectangle rectangle)
    {
        if (rectangle.Contains(Point.Round(triangle.A)) || rectangle.Contains(Point.Round(triangle.B)) ||
            rectangle.Contains(Point.Round(triangle.C)))
            return true;
        var corners = new[]
        {
            new Point(rectangle.Left, rectangle.Top), new Point(rectangle.Right, rectangle.Top),
            new Point(rectangle.Right, rectangle.Bottom), new Point(rectangle.Left, rectangle.Bottom)
        };
        if (corners.Any(point => PointInTriangle(point, triangle.A, triangle.B, triangle.C)))
            return true;
        var edges = new[] { (triangle.A, triangle.B), (triangle.B, triangle.C), (triangle.C, triangle.A) };
        return edges.Any(edge => SegmentIntersectsRectangle(edge.Item1, edge.Item2, rectangle));
    }

    private static bool SegmentIntersectsRectangle(PointF first, PointF second, Rectangle rectangle)
    {
        var corners = new[]
        {
            new PointF(rectangle.Left, rectangle.Top), new PointF(rectangle.Right, rectangle.Top),
            new PointF(rectangle.Right, rectangle.Bottom), new PointF(rectangle.Left, rectangle.Bottom)
        };
        for (var index = 0; index < corners.Length; index++)
            if (SegmentsIntersect(first, second, corners[index], corners[(index + 1) % corners.Length]))
                return true;
        return false;
    }

    private static bool SegmentsIntersect(PointF a, PointF b, PointF c, PointF d)
    {
        static float Cross(PointF p, PointF q, PointF r) =>
            (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
        var first = Cross(a, b, c);
        var second = Cross(a, b, d);
        var third = Cross(c, d, a);
        var fourth = Cross(c, d, b);
        return first * second <= 0f && third * fourth <= 0f;
    }

    private float TriangleDepthAt(Point point, RenderTriangle triangle)
    {
        var area = Edge(triangle.A, triangle.B, triangle.C.X, triangle.C.Y);
        if (MathF.Abs(area) < .00001f)
            return float.PositiveInfinity;
        var weightA = Edge(triangle.B, triangle.C, point.X, point.Y) / area;
        var weightB = Edge(triangle.C, triangle.A, point.X, point.Y) / area;
        return InterpolateDepth(triangle, weightA, weightB, 1f - weightA - weightB);
    }

    private static bool PointInTriangle(Point point, PointF a, PointF b, PointF c)
    {
        var area = Edge(a, b, c.X, c.Y);
        if (MathF.Abs(area) < .00001f)
            return false;
        var first = Edge(b, c, point.X, point.Y) / area;
        var second = Edge(c, a, point.X, point.Y) / area;
        var third = 1f - first - second;
        return first >= -.001f && second >= -.001f && third >= -.001f;
    }

    internal bool NavigateDrag(MouseButtons button, int dx, int dy)
    {
        if (button == MouseButtons.Left)
        {
            if (_viewKind != InteriorViewportKind.Perspective)
                return false;
            CameraController.Orbit(Camera, -dx * .01f, dy * .01f);
            LastNavigationOperation = "Orbit";
        }
        else if (button == MouseButtons.Middle)
        {
            var distance = Math.Max(Vector3.Distance(Camera.From, Camera.To), .001f);
            CameraController.PanTarget(Camera, -dx * distance * .0015f, dy * distance * .0015f);
            LastNavigationOperation = "移動 Camera Target";
        }
        else if (button == MouseButtons.Right)
        {
            var distance = Math.Max(Vector3.Distance(Camera.From, Camera.To), .001f);
            CameraController.Pan(Camera, -dx * distance * .0015f, dy * distance * .0015f);
            LastNavigationOperation = "Pan";
        }
        else
            return false;
        return true;
    }

    private bool CanNavigateWithButton(MouseButtons button) => button switch
    {
        MouseButtons.Left => _viewKind == InteriorViewportKind.Perspective,
        MouseButtons.Middle or MouseButtons.Right => true,
        _ => false
    };

    internal void NavigateWheel(int delta, Keys modifiers)
    {
        var wheelSteps = delta / 120f;
        if (modifiers.HasFlag(Keys.Control))
        {
            CameraController.AdjustFieldOfView(Camera, -wheelSteps * 2f);
            LastNavigationOperation = "調整 FOV";
        }
        else if (modifiers.HasFlag(Keys.Shift))
        {
            CameraController.AdjustRoll(Camera, wheelSteps * 2f);
            LastNavigationOperation = "調整 Roll";
        }
        else
        {
            if (wheelSteps > 0f)
                PrepareDollyTargetForSelectedModel();
            CameraController.Dolly(Camera, -wheelSteps * .12f);
            var distance = Math.Max(Vector3.Distance(Camera.From, Camera.To), .0001f);
            Camera.NearPlane = Math.Clamp(distance * .0025f, .0001f, .01f);
            Camera.FarPlane = Math.Max(Camera.FarPlane, Camera.NearPlane + .01f);
            LastNavigationOperation = "Dolly";
        }
    }

    private void PrepareDollyTargetForSelectedModel()
    {
        if (_primarySelectedModelId is not Guid selectedId || _preparedDollyTargetModelId == selectedId)
            return;
        var model = _sceneModels.FirstOrDefault(item => item.Id == selectedId && item.IsVisible);
        if (model is null || !SceneTraversal.TryCalculateBounds(model, out var bounds))
            return;

        // Keep the current viewing direction and distance, but move its axis through the selected
        // model. This lets centimetre-scale assets be approached without the camera stopping at an
        // unrelated room target.
        var targetOffset = bounds.Center - Camera.To;
        Camera.From += targetOffset;
        Camera.To = bounds.Center;
        _preparedDollyTargetModelId = selectedId;
    }

    private void NotifyCameraChanged()
    {
        Invalidate();
        CameraChanged?.Invoke(this, EventArgs.Empty);
    }
}
