namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using Rv3dViewer.Core;
using Rv3dViewer.Rendering.OpenGL;

internal sealed partial class InteriorPreviewViewportControl : UserControl
{
    private readonly ViewerProject _previewProject = new()
    {
        Name = "RV室內設計 PBR 預覽"
    };
    private OpenGlRenderer? _renderer;
    private bool _rendererDisposed;
    private Guid? _selectedModelId;
    private Guid? _preparedDollyTargetModelId;
    private int? _selectedMeshIndex;
    private InteriorViewportSelectionMode _selectionMode;
    private Point _selectionMouseDown;
    private Point _selectionMouseCurrent;
    private bool _selectionPointerDown;
    private Rectangle? _reversibleSelectionFrame;

    public InteriorPreviewViewportControl()
    {
        InitializeComponent();
    }

    internal event EventHandler? MaximizeRequested;
    internal event EventHandler? CameraChanged;
    internal event EventHandler<string>? PreviewError;
    internal event EventHandler<InteriorModelPickedEventArgs>? ModelPicked;
    internal event DragEventHandler? AssetDragEnter;
    internal event DragEventHandler? AssetDragOver;
    internal event DragEventHandler? AssetDragDrop;

    internal CameraState Camera => _previewProject.Camera;

    internal void EnableRuntimeContext()
    {
        if (previewGlControl.RuntimeContextEnabled)
            return;
        previewGlControl.EnableRuntimeContext();
        // Subscribe the renderer to GLControl.Load before the native handle is created.
        // This is invoked only from the containing Form.Shown event.
        EnsureRendererInitialized();
        previewGlControl.EnsureRuntimeControlCreated();
        _renderer?.InvalidateScene();
    }

    internal bool IsMaximized
    {
        get => maximizeButton.Text == "❐";
        set => maximizeButton.Text = value ? "❐" : "□";
    }

    internal void SetSceneModels(IReadOnlyList<SceneModel> models)
    {
        _previewProject.Models.Clear();
        _previewProject.Models.AddRange(models);
        try
        {
            _renderer?.InvalidateScene();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            PreviewError?.Invoke(this, $"PBR 場景更新失敗：{exception.GetBaseException().Message}");
        }
    }

    internal bool TryGetPlacementPointerFromScreen(Point screenLocation,
        out InteriorPlacementPointEventArgs placement)
    {
        placement = null!;
        if (!previewGlControl.IsHandleCreated || previewGlControl.ClientSize.Width <= 1 ||
            previewGlControl.ClientSize.Height <= 1)
            return false;
        var location = previewGlControl.PointToClient(screenLocation);
        if (!previewGlControl.ClientRectangle.Contains(location))
            return false;
        Camera.Validate();
        var forward = Vector3.Normalize(Camera.To - Camera.From);
        var right = Vector3.Normalize(Vector3.Cross(forward, Camera.Up));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var focalLength = previewGlControl.ClientSize.Height /
                          (2f * MathF.Tan(Camera.FieldOfViewDegrees * MathF.PI / 360f));
        var centerX = previewGlControl.ClientSize.Width / 2f;
        var centerY = previewGlControl.ClientSize.Height / 2f;
        var direction = Vector3.Normalize(forward +
                                          right * ((location.X - centerX) / focalLength) -
                                          up * ((location.Y - centerY) / focalLength));
        var hasGroundPoint = MathF.Abs(direction.Y) >= .00001f;
        var distance = hasGroundPoint ? -Camera.From.Y / direction.Y : -1f;
        hasGroundPoint &= distance > 0f;
        var point = hasGroundPoint ? Camera.From + direction * distance : Camera.To;
        if (hasGroundPoint)
            point.Y = 0f;
        placement = new InteriorPlacementPointEventArgs(point, Camera.From, direction, hasGroundPoint);
        return true;
    }

    internal void SetSelection(Guid? modelId, int? meshIndex)
    {
        if (_selectedModelId != modelId)
            _preparedDollyTargetModelId = null;
        _selectedModelId = modelId;
        _selectedMeshIndex = modelId is null ? null : meshIndex;
        var model = modelId is Guid id
            ? _previewProject.Models.FirstOrDefault(item => item.Id == id)
            : null;
        _renderer?.SetSelectedMeshHighlight(model, meshIndex);
        _renderer?.InvalidateScene();
    }

    internal void SetSelection(IEnumerable<Guid> modelIds, Guid? primaryModelId, int? meshIndex)
    {
        var selectedIds = modelIds.ToHashSet();
        SetSelection(primaryModelId is Guid id && selectedIds.Contains(id) ? id : null, meshIndex);
    }

    internal InteriorViewportSelectionMode SelectionMode
    {
        get => _selectionMode;
        set
        {
            _selectionMode = value;
            if (_renderer is not null)
                _renderer.NavigationEnabled = value == InteriorViewportSelectionMode.Navigate;
            previewGlControl.Cursor = value == InteriorViewportSelectionMode.Navigate ? Cursors.Default : Cursors.Cross;
        }
    }

    internal void SetCamera(CameraState source)
    {
        CopyCamera(source, _previewProject.Camera);
        _renderer?.InvalidateScene();
    }

    internal void SetLightingScale(float scale)
    {
        scale = Math.Clamp(scale, 0f, 4f);
        _previewProject.Lights = SceneLight.CreateDefaultRig();
        foreach (var light in _previewProject.Lights)
            light.Intensity *= scale;
        _renderer?.InvalidateScene();
    }

    internal void SetEnvironment(string? path, float intensity, bool showBackground)
    {
        _previewProject.Environment.Enabled = !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        _previewProject.Environment.Path = path ?? string.Empty;
        _previewProject.Environment.Intensity = Math.Clamp(intensity, 0f, 20f);
        _previewProject.Environment.ShowBackground = showBackground;
        _previewProject.Environment.Validate();
        _renderer?.InvalidateScene();
    }

    internal void SetEnvironmentIntensity(float intensity)
    {
        _previewProject.Environment.Intensity = Math.Clamp(intensity, 0f, 20f);
        _renderer?.InvalidateScene();
    }

    internal void SetBackgroundColor(Color color)
    {
        _previewProject.RenderSettings.BackgroundColor = new Vector4(
            color.R / 255f, color.G / 255f, color.B / 255f, 1f);
        _renderer?.InvalidateScene();
    }

    internal void SetGridVisible(bool visible)
    {
        _previewProject.RenderSettings.ShowGrid = visible;
        _previewProject.RenderSettings.ShowWorldAxes = visible;
        _renderer?.InvalidateScene();
    }

    private void MaximizeButton_Click(object? sender, EventArgs e) =>
        MaximizeRequested?.Invoke(this, EventArgs.Empty);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // The containing Form is fully constructed when this handle is created, while the
        // child GLControl has not raised Load yet. Subscribe the renderer now so it receives
        // the GLControl.Load event that initializes shaders, viewport and input rendering.
        if (previewGlControl.RuntimeContextEnabled)
            EnsureRendererInitialized();
    }

    private void EnsureRendererInitialized()
    {
        if (_renderer is not null || _rendererDisposed || IsDisposed || !IsHandleCreated ||
            previewGlControl.IsDisposed || FindForm() is not { IsHandleCreated: true })
            return;
        try
        {
            var renderer = new OpenGlRenderer(previewGlControl);
            renderer.SetProject(_previewProject);
            renderer.SetPreviewMode(ViewportPreviewMode.Preview2);
            renderer.SetPreviewOverlaysEnabled(true);
            renderer.SetPluginDisplayOptions(false, true, ViewportModelDisplayMode.Solid);
            renderer.SetInputOverlayEnabled(true);
            renderer.CameraChanged += (_, _) => CameraChanged?.Invoke(this, EventArgs.Empty);
            renderer.RendererError += (_, message) => PreviewError?.Invoke(this, message);
            _renderer = renderer;
            renderer.NavigationEnabled = _selectionMode == InteriorViewportSelectionMode.Navigate;
            var selectedModel = _selectedModelId is Guid id
                ? _previewProject.Models.FirstOrDefault(item => item.Id == id)
                : null;
            renderer.SetSelectedMeshHighlight(selectedModel, _selectedMeshIndex);
            renderer.InvalidateScene();
        }
        catch (Exception exception)
        {
            PreviewError?.Invoke(this, $"PBR 預覽初始化失敗：{exception.Message}");
        }
    }

    private void DisposeRenderer()
    {
        if (_rendererDisposed)
            return;
        _rendererDisposed = true;
        var renderer = _renderer;
        _renderer = null;
        if (renderer is null)
            return;
        try
        {
            renderer.Dispose();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            // OpenTK may try to recreate a GLControl when MakeCurrent is called after the
            // containing Form has started destroying its handles. Closing the Plugin must
            // never surface that renderer cleanup failure as a Plugin execution failure.
            PreviewError?.Invoke(this, $"PBR 預覽關閉時略過 OpenGL 清理錯誤：{exception.Message}");
            if (!previewGlControl.IsDisposed)
                previewGlControl.Dispose();
        }
    }

    internal void PrepareForFormClose()
    {
        CancelSelectionDrag();
        DisposeRenderer();
    }

    private void PreviewGlControl_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _selectionMode != InteriorViewportSelectionMode.Navigate)
        {
            _selectionMouseDown = e.Location;
            _selectionMouseCurrent = e.Location;
            _selectionPointerDown = true;
            previewGlControl.Capture = true;
        }
    }

    private void PreviewGlControl_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (e.Delta <= 0 || ModifierKeys.HasFlag(Keys.Control) || ModifierKeys.HasFlag(Keys.Shift) ||
            _selectionMode != InteriorViewportSelectionMode.Navigate ||
            _selectedModelId is not Guid selectedId || _preparedDollyTargetModelId == selectedId)
            return;
        var model = _previewProject.Models.FirstOrDefault(item => item.Id == selectedId && item.IsVisible);
        if (model is null || !SceneTraversal.TryCalculateBounds(model, out var bounds))
            return;

        var targetOffset = bounds.Center - Camera.To;
        Camera.From += targetOffset;
        Camera.To = bounds.Center;
        _preparedDollyTargetModelId = selectedId;
    }

    private void PreviewGlControl_DragEnter(object? sender, DragEventArgs e) => AssetDragEnter?.Invoke(this, e);

    private void PreviewGlControl_DragOver(object? sender, DragEventArgs e) => AssetDragOver?.Invoke(this, e);

    private void PreviewGlControl_DragDrop(object? sender, DragEventArgs e) => AssetDragDrop?.Invoke(this, e);

    private void PreviewGlControl_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_selectionPointerDown)
            return;
        EraseReversibleSelectionFrame();
        _selectionMouseCurrent = e.Location;
        var rectangle = NormalizeSelectionRectangle(_selectionMouseDown, _selectionMouseCurrent);
        if (rectangle.Width <= 2 && rectangle.Height <= 2)
            return;
        var topLeft = previewGlControl.PointToScreen(rectangle.Location);
        _reversibleSelectionFrame = new Rectangle(topLeft, rectangle.Size);
        ControlPaint.DrawReversibleFrame(_reversibleSelectionFrame.Value, Color.DeepSkyBlue, FrameStyle.Dashed);
    }

    private void PreviewGlControl_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !_selectionPointerDown ||
            _selectionMode == InteriorViewportSelectionMode.Navigate)
            return;
        EraseReversibleSelectionFrame();
        _selectionPointerDown = false;
        previewGlControl.Capture = false;
        _selectionMouseCurrent = e.Location;
        var rectangle = NormalizeSelectionRectangle(_selectionMouseDown, e.Location);
        var ids = rectangle.Width <= 4 && rectangle.Height <= 4
            ? PickModel(e.Location) is Guid id ? new[] { id } : []
            : PickModels(rectangle);
        ModelPicked?.Invoke(this, new InteriorModelPickedEventArgs(ids, GetSelectionOperation(ModifierKeys)));
    }

    internal void CancelSelectionDrag()
    {
        EraseReversibleSelectionFrame();
        _selectionPointerDown = false;
        previewGlControl.Capture = false;
    }

    private void EraseReversibleSelectionFrame()
    {
        if (_reversibleSelectionFrame is not Rectangle frame)
            return;
        ControlPaint.DrawReversibleFrame(frame, Color.DeepSkyBlue, FrameStyle.Dashed);
        _reversibleSelectionFrame = null;
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

    private static Rectangle NormalizeSelectionRectangle(Point first, Point second) => Rectangle.FromLTRB(
        Math.Min(first.X, second.X), Math.Min(first.Y, second.Y),
        Math.Max(first.X, second.X), Math.Max(first.Y, second.Y));

    private Guid? PickModel(Point location)
    {
        var candidates = new List<(Guid Id, float Depth)>();
        foreach (var model in _previewProject.Models.Where(model => model.IsVisible))
        {
            if (!TryGetProjectedModelBounds(model, out var rectangle, out var depth))
                continue;
            if (rectangle.Contains(location))
                candidates.Add((model.Id, depth));
        }
        return candidates.OrderBy(candidate => candidate.Depth).FirstOrDefault().Id is var id && id != Guid.Empty
            ? id
            : null;
    }

    private Guid[] PickModels(Rectangle selectionRectangle) => _previewProject.Models
        .Where(model => model.IsVisible && TryGetProjectedModelBounds(model, out var bounds, out _) &&
                        bounds.IntersectsWith(selectionRectangle))
        .Select(model => model.Id)
        .ToArray();

    private bool TryGetProjectedModelBounds(SceneModel model, out RectangleF rectangle, out float minimumDepth)
    {
        rectangle = RectangleF.Empty;
        minimumDepth = float.PositiveInfinity;
        if (!SceneTraversal.TryCalculateBounds(model, out var bounds))
            return false;
        var camera = _previewProject.Camera;
        var forward = Vector3.Normalize(camera.To - camera.From);
        var right = Vector3.Normalize(Vector3.Cross(forward, camera.Up));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var height = Math.Max(1, previewGlControl.ClientSize.Height);
        var width = Math.Max(1, previewGlControl.ClientSize.Width);
        var focalLength = height / (2f * MathF.Tan(camera.FieldOfViewDegrees * MathF.PI / 360f));
        var corners = new[]
        {
            new Vector3(bounds.Minimum.X, bounds.Minimum.Y, bounds.Minimum.Z),
            new Vector3(bounds.Maximum.X, bounds.Minimum.Y, bounds.Minimum.Z),
            new Vector3(bounds.Minimum.X, bounds.Maximum.Y, bounds.Minimum.Z),
            new Vector3(bounds.Maximum.X, bounds.Maximum.Y, bounds.Minimum.Z),
            new Vector3(bounds.Minimum.X, bounds.Minimum.Y, bounds.Maximum.Z),
            new Vector3(bounds.Maximum.X, bounds.Minimum.Y, bounds.Maximum.Z),
            new Vector3(bounds.Minimum.X, bounds.Maximum.Y, bounds.Maximum.Z),
            new Vector3(bounds.Maximum.X, bounds.Maximum.Y, bounds.Maximum.Z)
        };
        var projected = corners.Select(corner =>
        {
            var relative = corner - camera.From;
            var depth = Vector3.Dot(relative, forward);
            return depth <= camera.NearPlane
                ? (Point: PointF.Empty, Depth: depth, Valid: false)
                : (Point: new PointF(width / 2f + Vector3.Dot(relative, right) * focalLength / depth,
                    height / 2f - Vector3.Dot(relative, up) * focalLength / depth), Depth: depth, Valid: true);
        }).Where(item => item.Valid).ToArray();
        if (projected.Length == 0)
            return false;
        rectangle = RectangleF.FromLTRB(projected.Min(item => item.Point.X), projected.Min(item => item.Point.Y),
            projected.Max(item => item.Point.X), projected.Max(item => item.Point.Y));
        minimumDepth = projected.Min(item => item.Depth);
        return true;
    }

    internal static void CopyCamera(CameraState source, CameraState target)
    {
        target.From = source.From;
        target.To = source.To;
        target.Up = source.Up;
        target.FieldOfViewDegrees = source.FieldOfViewDegrees;
        target.RollDegrees = source.RollDegrees;
        target.NearPlane = source.NearPlane;
        target.FarPlane = source.FarPlane;
    }
}
