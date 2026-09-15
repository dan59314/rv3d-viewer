using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using OpenTK.Graphics.OpenGL4;
using Rv3dViewer.Core;
using GLMatrix4 = OpenTK.Mathematics.Matrix4;
using GLVector3 = OpenTK.Mathematics.Vector3;
using NumericsVector3 = System.Numerics.Vector3;
using NumericsVector4 = System.Numerics.Vector4;

namespace Rv3dViewer.Rendering.OpenGL;

public readonly record struct MeshSelection(SceneModel Model, int MeshIndex);

public sealed class BoxSelectionChangedEventArgs(
    IReadOnlyList<SceneModel> models,
    IReadOnlyList<MeshSelection> meshes) : EventArgs
{
    public IReadOnlyList<SceneModel> Models { get; } = models;
    public IReadOnlyList<MeshSelection> Meshes { get; } = meshes;
}

public enum ViewportEditMode
{
    Navigate,
    Select,
    SelectMesh,
    Subtract
}

public enum ViewportSelectionTarget
{
    Model,
    Mesh
}

public enum ViewportModelDisplayMode
{
    Points,
    Wireframe,
    Solid
}

public sealed record ViewportEditorColors(
    Color StudioBackground,
    Color GridMinor,
    Color GridMajor,
    Color AxisX,
    Color AxisY,
    Color AxisZ,
    Color Selection,
    Color CameraGizmo,
    Color DisabledLight,
    Color LightHighlight,
    Color OverlayText,
    Color OverlayBackground)
{
    public static ViewportEditorColors Default { get; } = new(
        Color.FromArgb(115, 115, 115),
        Color.FromArgb(56, 133, 145, 163),
        Color.FromArgb(107, 163, 176, 194),
        Color.FromArgb(242, 46, 46), Color.FromArgb(46, 230, 56), Color.FromArgb(51, 115, 255),
        Color.FromArgb(255, 158, 20), Color.FromArgb(64, 230, 255),
        Color.FromArgb(107, 107, 107), Color.White, Color.White, Color.FromArgb(190, 0, 0, 0));
}

public sealed class OpenGlRenderer : ISceneRenderer
{
    public const int MaximumLights = 16;
    private const int QuickPreviewFrameIntervalMilliseconds = 33;
    private const double InputOverlayDurationSeconds = 1.5;
    private const double InputOverlayFadeSeconds = 0.35;
    private readonly OpenTK.GLControl.GLControl _control;
    private readonly System.Windows.Forms.Timer _renderTimer;
    private readonly System.Windows.Forms.Timer _wheelInteractionTimer;
    private readonly List<GpuMesh> _gpuMeshes = [];
    private readonly List<RenderInstance> _renderInstances = [];
    private readonly Dictionary<string, int> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(PbrMaterial Material, TextureSemantic Semantic), (string Fingerprint, int Id)> _stackTextures = [];
    private readonly HashSet<string> _failedTextures = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<SceneModel> _highlightedModels = [];
    private readonly HashSet<MeshSelection> _highlightedMeshes = [];
    private SceneLight? _highlightedLight;
    private ViewerProject _project = new();
    private bool _loaded;
    private bool _sceneDirty = true;
    private bool _resetEnvironmentPreviewBuffers;
    private bool? _lastEnvironmentEnabled;
    private bool? _lastEnvironmentBackgroundVisible;
    private int _program;
    private int _lineProgram;
    private int _environmentProgram;
    private int _environmentVertexArray;
    private int _environmentTexture;
    private string? _environmentTexturePath;
    private float _environmentMaxLod;
    private int _skyboxProgram;
    private int _skyboxTexture;
    private string? _skyboxTextureSignature;
    private float _skyboxMaxLod;
    private int _sceneColorTexture;
    private int _sceneColorWidth;
    private int _sceneColorHeight;
    private int _glassBackDepthProgram;
    private int _glassBackDepthFramebuffer;
    private int _glassBackDepthTexture;
    private int _glassBackDepthBuffer;
    private int _glassBackDepthWidth;
    private int _glassBackDepthHeight;
    private int _speedPreviewFramebuffer;
    private int _speedPreviewColorTexture;
    private int _speedPreviewDepthBuffer;
    private int _speedPreviewWidth;
    private int _speedPreviewHeight;
    private int _captureFramebuffer;
    private int _captureColorTexture;
    private int _captureDepthBuffer;
    private int _captureWidth;
    private int _captureHeight;
    private int _activeRenderFramebuffer;
    private int _activeRenderWidth;
    private int _activeRenderHeight;
    private int _selectionMaskProgram;
    private int _selectionOutlineProgram;
    private int _inputOverlayProgram;
    private int _inputOverlayVertexArray;
    private int _inputOverlayVertexBuffer;
    private int _inputOverlayTexture;
    private int _inputOverlayTextureWidth;
    private int _inputOverlayTextureHeight;
    private bool _inputOverlayEnabled;
    private bool _inputOverlayTextureDirty;
    private string _inputOverlayText = string.Empty;
    private long _inputOverlayExpiresAt;
    private Color _inputOverlayTextColor = Color.White;
    private Color _inputOverlayBackgroundColor = Color.FromArgb(190, 0, 0, 0);
    private ViewportEditorColors _editorColors = ViewportEditorColors.Default;
    private float _inputOverlayFontSize = (SystemFonts.MessageBoxFont ?? Control.DefaultFont).Size;
    private int _selectionMaskFramebuffer;
    private int _selectionMaskTexture;
    private int _selectionMaskDepthBuffer;
    private int _selectionMaskWidth;
    private int _selectionMaskHeight;
    private int _lightGizmoVertexArray;
    private int _lightGizmoVertexBuffer;
    private GridGeometry? _grid;
    private Point _lastMouse;
    private Point _boxStart;
    private Point _boxCurrent;
    private bool _isBoxSelecting;
    private ViewportPreviewMode _previewMode = ViewportPreviewMode.Preview1;
    private bool? _showTexturesOverride;
    private bool? _showLightGizmosOverride;
    private ViewportModelDisplayMode? _modelDisplayModeOverride;
    private bool _showPreviewOverlays = true;
    private bool _renderingPaused;
    private long _lastQuickPreviewRenderTicks;
    private SelectionOperation _selectionOperation;
    private ViewportEditMode _editMode = ViewportEditMode.Navigate;
    private ViewportSelectionTarget _selectionTarget = ViewportSelectionTarget.Model;
    private bool _pointerCameraInteraction;
    private bool _wheelCameraInteraction;

    public event EventHandler? CameraChanged;
    public event EventHandler? CameraInteractionStarted;
    public event EventHandler? CameraInteractionCompleted;
    public event EventHandler<string>? RendererError;
    public event EventHandler<BoxSelectionChangedEventArgs>? BoxSelectionChanged;

    public IReadOnlyList<SceneModel> HighlightedModels =>
        _project.Models.Where(model => model.IsVisible && _highlightedModels.Contains(model)).ToArray();

    public IReadOnlyList<MeshSelection> HighlightedMeshes => _highlightedMeshes
        .Where(selection =>
            _project.Models.Contains(selection.Model) &&
            selection.Model.IsVisible &&
            (uint)selection.MeshIndex < (uint)selection.Model.Meshes.Count &&
            !selection.Model.HiddenMeshIndices.Contains(selection.MeshIndex))
        .ToArray();

    /// <summary>
    /// When quick preview is enabled, render at half resolution and upscale to
    /// the viewport. Disable this for a full-resolution quick shader pass.
    /// </summary>
    public bool UseSpeedFrameBuffer { get; set; } = true;

    public float PreviewAspectRatio => Math.Max(1, _control.ClientSize.Width) /
                                       (float)Math.Max(1, _control.ClientSize.Height);

    public bool NavigationEnabled { get; set; } = true;

    public void SetInputOverlayEnabled(bool enabled)
    {
        _inputOverlayEnabled = enabled;
        if (enabled) return;
        _inputOverlayText = string.Empty;
        _inputOverlayExpiresAt = 0;
        _inputOverlayTextureDirty = false;
    }

    public void ShowInputActivity(string text)
    {
        if (!_inputOverlayEnabled || string.IsNullOrWhiteSpace(text)) return;
        if (!string.Equals(_inputOverlayText, text, StringComparison.Ordinal))
        {
            _inputOverlayText = text;
            _inputOverlayTextureDirty = true;
        }
        _inputOverlayExpiresAt = Stopwatch.GetTimestamp() +
                                 (long)(InputOverlayDurationSeconds * Stopwatch.Frequency);
    }

    public void SetInputOverlayStyle(Color textColor, float fontSize)
    {
        _inputOverlayTextColor = textColor;
        _inputOverlayFontSize = Math.Clamp(fontSize, 6f, 48f);
        _inputOverlayTextureDirty = true;
    }

    public void SetEditorColors(ViewportEditorColors colors)
    {
        _editorColors = colors;
        _inputOverlayTextColor = colors.OverlayText;
        _inputOverlayBackgroundColor = colors.OverlayBackground;
        _inputOverlayTextureDirty = true;
        InvalidateScene();
    }

    public OpenGlRenderer(OpenTK.GLControl.GLControl control)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
        _control.Load += OnLoad;
        _control.Resize += (_, _) => ResizeViewport();
        _control.MouseDown += OnMouseDown;
        _control.MouseMove += OnMouseMove;
        _control.MouseUp += OnMouseUp;
        _control.MouseWheel += OnMouseWheel;
        _control.MouseCaptureChanged += (_, _) =>
        {
            if (!_control.Capture) EndPointerCameraInteraction();
        };

        _renderTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _renderTimer.Tick += (_, _) => Render();
        _renderTimer.Start();
        _wheelInteractionTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _wheelInteractionTimer.Tick += (_, _) => EndWheelCameraInteraction();
    }

    public void SetProject(ViewerProject project)
    {
        _project = project;
        _highlightedModels.Clear();
        _highlightedMeshes.Clear();
        _highlightedLight = null;
        _isBoxSelecting = false;
        _sceneDirty = true;
        _resetEnvironmentPreviewBuffers = true;
        _lastEnvironmentEnabled = null;
        _lastEnvironmentBackgroundVisible = null;
        _skyboxTextureSignature = null;
    }

    public void InvalidateScene() => _sceneDirty = true;

    public void SetRenderingPaused(bool paused)
    {
        _renderingPaused = paused;
        if (!paused) _control.Invalidate();
    }

    /// <summary>
    /// Invalidates environment-dependent preview buffers. Preview 5 samples a
    /// copy of the already-rendered scene for glass refraction, so changing the
    /// HDR background visibility must discard that copy before the next frame.
    /// HDR lighting and reflections remain enabled by Environment.Enabled.
    /// </summary>
    public void InvalidateEnvironmentAppearance()
    {
        _resetEnvironmentPreviewBuffers = true;
        _sceneDirty = true;
        _control.Invalidate();
    }

    /// <summary>
    /// Enables the low-cost viewport pass. The selected mode remains active
    /// until the user changes it from the View menu.
    /// </summary>
    public void SetQuickPreviewEnabled(bool enabled)
    {
        SetPreviewMode(enabled ? ViewportPreviewMode.Preview1 : ViewportPreviewMode.Preview2);
    }

    public void SetPreviewMode(ViewportPreviewMode mode)
    {
        _previewMode = Enum.IsDefined(mode) ? mode : ViewportPreviewMode.Preview1;
        _lastQuickPreviewRenderTicks = 0;
        _control.Invalidate();
        InvalidateScene();
    }

    /// <summary>Controls editor-only helpers such as grids, axes, gizmos and selection outlines.</summary>
    public void SetPreviewOverlaysEnabled(bool enabled)
    {
        _showPreviewOverlays = enabled;
        InvalidateScene();
    }

    public void SetPluginDisplayOptions(bool showLights, bool showTextures, ViewportModelDisplayMode modelMode)
    {
        _showLightGizmosOverride = showLights;
        _showTexturesOverride = showTextures;
        _modelDisplayModeOverride = modelMode;
        InvalidateScene();
    }

    private PolygonMode ScenePolygonMode => (_modelDisplayModeOverride ?? GetProjectModelDisplayMode()) switch
    {
        ViewportModelDisplayMode.Points => PolygonMode.Point,
        ViewportModelDisplayMode.Wireframe => PolygonMode.Line,
        _ => PolygonMode.Fill
    };

    private ViewportModelDisplayMode GetProjectModelDisplayMode()
    {
        var mode = _project.RenderSettings.ModelDisplayMode;
        if (mode == ModelDisplayMode.Solid && _project.RenderSettings.Wireframe)
            mode = ModelDisplayMode.Wireframe;
        return mode switch
        {
            ModelDisplayMode.Points => ViewportModelDisplayMode.Points,
            ModelDisplayMode.Wireframe => ViewportModelDisplayMode.Wireframe,
            _ => ViewportModelDisplayMode.Solid
        };
    }

    /// <summary>Captures the currently displayed viewport and resizes it for preview-image export.</summary>
    public Bitmap CapturePreviewImage(int width, int height)
    {
        if (!_loaded || _control.IsDisposed || !_control.IsHandleCreated)
            throw new InvalidOperationException("Preview viewport is not ready.");
        width = Math.Clamp(width, 1, 16384);
        height = Math.Clamp(height, 1, 16384);
        _control.MakeCurrent();
        var sourceWidth = Math.Max(1, _control.ClientSize.Width);
        var sourceHeight = Math.Max(1, _control.ClientSize.Height);
        using var source = new Bitmap(sourceWidth, sourceHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var data = source.LockBits(new Rectangle(0, 0, sourceWidth, sourceHeight), ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.ReadBuffer(ReadBufferMode.Front);
            GL.ReadPixels(0, 0, sourceWidth, sourceHeight, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                PixelType.UnsignedByte, data.Scan0);
        }
        finally
        {
            source.UnlockBits(data);
        }
        source.RotateFlip(RotateFlipType.RotateNoneFlipY);
        var output = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(output);
        graphics.Clear(Color.Transparent);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(source, new Rectangle(0, 0, width, height));
        return output;
    }

    /// <summary>Forces a full-resolution frame before capturing the displayed viewport.</summary>
    public Bitmap CaptureRenderedImage(
        int width,
        int height,
        bool includeEditorOverlays = false,
        CameraState? cameraOverride = null,
        bool useQuickPreview = false,
        float? orthographicHeight = null)
    {
        if (!_loaded || _control.IsDisposed || !_control.IsHandleCreated)
            throw new InvalidOperationException("Preview viewport is not ready.");
        width = Math.Clamp(width, 1, 16384);
        height = Math.Clamp(height, 1, 16384);
        var previousPreviewMode = _previewMode;
        var previousOverlays = _showPreviewOverlays;
        try
        {
            _control.MakeCurrent();
            EnsureCaptureFramebuffer(width, height);
            _previewMode = useQuickPreview ? ViewportPreviewMode.Preview1 : ViewportPreviewMode.Preview2;
            _showPreviewOverlays = includeEditorOverlays;
            Render(force: true, targetFramebuffer: _captureFramebuffer,
                targetWidth: width, targetHeight: height, swapBuffers: false,
                cameraOverride: cameraOverride, orthographicHeight: orthographicHeight);

            var output = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var data = output.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _captureFramebuffer);
                GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
                GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                    PixelType.UnsignedByte, data.Scan0);
            }
            finally
            {
                output.UnlockBits(data);
            }
            output.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return output;
        }
        finally
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, Math.Max(1, _control.ClientSize.Width), Math.Max(1, _control.ClientSize.Height));
            _previewMode = previousPreviewMode;
            _showPreviewOverlays = previousOverlays;
            InvalidateScene();
        }
    }

    /// <summary>Highlights the light selected in the editor's light list.</summary>
    public void SetHighlightedLight(SceneLight? light)
    {
        if (ReferenceEquals(_highlightedLight, light)) return;
        _highlightedLight = light;
        InvalidateScene();
    }

    public void SetEditMode(ViewportEditMode mode)
    {
        var newTarget = mode switch
        {
            ViewportEditMode.Select => ViewportSelectionTarget.Model,
            ViewportEditMode.SelectMesh => ViewportSelectionTarget.Mesh,
            _ => _selectionTarget
        };
        if (newTarget != _selectionTarget)
        {
            _selectionTarget = newTarget;
            ClearBoxSelection();
        }
        _editMode = mode;
        if (mode != ViewportEditMode.Navigate) return;
        _isBoxSelecting = false;
        _control.Capture = false;
    }

    public void ClearBoxSelection()
    {
        _highlightedModels.Clear();
        _highlightedMeshes.Clear();
        BoxSelectionChanged?.Invoke(this, new BoxSelectionChangedEventArgs([], []));
    }

    public void SelectAll()
    {
        _highlightedModels.Clear();
        _highlightedMeshes.Clear();
        if (_selectionTarget == ViewportSelectionTarget.Mesh)
        {
            foreach (var model in _project.Models.Where(model => model.IsVisible))
                for (var meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
                    if (!model.HiddenMeshIndices.Contains(meshIndex))
                        _highlightedMeshes.Add(new MeshSelection(model, meshIndex));
            BoxSelectionChanged?.Invoke(this, new BoxSelectionChangedEventArgs([], HighlightedMeshes));
        }
        else
        {
            foreach (var model in _project.Models.Where(model => model.IsVisible))
                _highlightedModels.Add(model);
            BoxSelectionChanged?.Invoke(this, new BoxSelectionChangedEventArgs(HighlightedModels, []));
        }
    }

    public void SetSelectedMeshHighlight(SceneModel? model, int? meshIndex)
    {
        _highlightedMeshes.Clear();
        if (model is not null && meshIndex is int index)
        {
            _highlightedMeshes.Add(new MeshSelection(model, index));
            _highlightedModels.Clear();
        }
    }

    public void SetSelectedMeshHighlights(IEnumerable<MeshSelection> selections)
    {
        _highlightedModels.Clear();
        _highlightedMeshes.Clear();
        foreach (var selection in selections)
            if (_project.Models.Contains(selection.Model) &&
                (uint)selection.MeshIndex < (uint)selection.Model.Meshes.Count)
                _highlightedMeshes.Add(selection);
    }

    public void FrameModel(SceneModel model)
    {
        if (!SceneTraversal.TryCalculateBounds(model, out var bounds)) return;
        FrameBounds(bounds);
    }

    public void FrameMesh(SceneModel model, int meshIndex)
    {
        if (!SceneTraversal.TryCalculateMeshBounds(model, meshIndex, out var bounds)) return;
        FrameBounds(bounds);
    }

    private void FrameBounds(SceneBounds bounds)
    {
        var center = bounds.Center;
        var radius = Math.Max(bounds.Radius, 0.1f);
        var direction = _project.Camera.From - _project.Camera.To;
        if (direction.LengthSquared() < 0.0001f) direction = new Vector3(1f, 0.75f, 1.5f);
        direction = Vector3.Normalize(direction);
        var halfFov = _project.Camera.FieldOfViewDegrees * MathF.PI / 360f;
        _project.Camera.To = center;
        _project.Camera.From = center + direction * (radius / MathF.Sin(halfFov) * 1.15f);
        CameraChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        _control.MakeCurrent();
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _program = CreateProgram(VertexShader, FragmentShader);
        _lineProgram = CreateProgram(LineVertexShader, LineFragmentShader);
        _environmentProgram = CreateProgram(EnvironmentVertexShader, EnvironmentFragmentShader);
        _skyboxProgram = CreateProgram(EnvironmentVertexShader, SkyboxFragmentShader);
        _selectionMaskProgram = CreateProgram(SelectionMaskVertexShader, SelectionMaskFragmentShader);
        _selectionOutlineProgram = CreateProgram(SelectionOutlineVertexShader, SelectionOutlineFragmentShader);
        _inputOverlayProgram = CreateProgram(InputOverlayVertexShader, InputOverlayFragmentShader);
        _glassBackDepthProgram = CreateProgram(GlassBackDepthVertexShader, GlassBackDepthFragmentShader);
        _environmentVertexArray = GL.GenVertexArray();
        _lightGizmoVertexArray = GL.GenVertexArray();
        _lightGizmoVertexBuffer = GL.GenBuffer();
        GL.BindVertexArray(_lightGizmoVertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _lightGizmoVertexBuffer);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.BindVertexArray(0);
        _inputOverlayVertexArray = GL.GenVertexArray();
        _inputOverlayVertexBuffer = GL.GenBuffer();
        GL.BindVertexArray(_inputOverlayVertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _inputOverlayVertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, 16 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.BindVertexArray(0);
        _grid = GridGeometry.Create();
        _loaded = true;
        ResizeViewport();
        _sceneDirty = true;
    }

    private void ResizeViewport()
    {
        if (!_loaded || _control.ClientSize.Width <= 0 || _control.ClientSize.Height <= 0) return;
        _control.MakeCurrent();
        GL.Viewport(0, 0, _control.ClientSize.Width, _control.ClientSize.Height);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        ShowInputActivity(FormatMouseInput("MouseDn", e.Button));
        _lastMouse = e.Location;
        _control.Focus();
        var modifiers = Control.ModifierKeys;
        var startsBoxSelection = e.Button == MouseButtons.Left &&
                                 _editMode != ViewportEditMode.Navigate &&
                                 modifiers.HasFlag(Keys.Control);
        if (e.Button is MouseButtons.Middle or MouseButtons.Right ||
            e.Button == MouseButtons.Left && !startsBoxSelection)
        {
            if (!NavigationEnabled) return;
            BeginPointerCameraInteraction();
            _control.Capture = true;
            return;
        }
        if (!startsBoxSelection) return;

        _boxStart = e.Location;
        _boxCurrent = e.Location;
        _isBoxSelecting = true;
        _control.Capture = true;
        _selectionOperation = modifiers.HasFlag(Keys.Alt) || _editMode == ViewportEditMode.Subtract
            ? SelectionOperation.Subtract
            : SelectionOperation.Add;
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        var modifiers = Control.ModifierKeys;
        if (e.Button != MouseButtons.None || modifiers != Keys.None)
        {
            ShowInputActivity(e.Button == MouseButtons.None
                ? FormatMouseInput("Mouse Move", MouseButtons.None)
                : FormatMouseInput("Mouse Drag", e.Button));
        }
        if (_isBoxSelecting)
        {
            _boxCurrent = e.Location;
            return;
        }

        var dx = e.X - _lastMouse.X;
        var dy = e.Y - _lastMouse.Y;
        _lastMouse = e.Location;
        if (!NavigationEnabled) return;
        if (e.Button == MouseButtons.Left && _pointerCameraInteraction)
            CameraController.Orbit(_project.Camera, -dx * 0.01f, dy * 0.01f);
        else if (e.Button == MouseButtons.Middle)
        {
            var distance = Vector3.Distance(_project.Camera.From, _project.Camera.To);
            CameraController.PanTarget(_project.Camera, -dx * distance * 0.0015f, dy * distance * 0.0015f);
        }
        else if (e.Button == MouseButtons.Right)
        {
            var distance = Vector3.Distance(_project.Camera.From, _project.Camera.To);
            CameraController.Pan(_project.Camera, -dx * distance * 0.0015f, dy * distance * 0.0015f);
        }
        else return;
        CameraChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        ShowInputActivity(FormatMouseInput("MouseUp", e.Button));
        if (!_isBoxSelecting)
        {
            if (e.Button is MouseButtons.Left or MouseButtons.Middle or MouseButtons.Right)
            {
                _control.Capture = false;
                EndPointerCameraInteraction();
            }
            return;
        }
        if (e.Button != MouseButtons.Left) return;

        _boxCurrent = e.Location;
        _isBoxSelecting = false;
        _control.Capture = false;
        ApplyBoxSelection();
    }

    private void ApplyBoxSelection()
    {
        var rectangle = NormalizeRectangle(_boxStart, _boxCurrent);
        var isClick = rectangle.Width < 4 && rectangle.Height < 4;
        var selectionArea = isClick
            ? new RectangleF(_boxCurrent.X - 4f, _boxCurrent.Y - 4f, 8f, 8f)
            : rectangle;

        if (_selectionTarget == ViewportSelectionTarget.Mesh)
        {
            ApplyMeshBoxSelection(selectionArea, isClick);
            return;
        }

        var candidates = new List<(SceneModel Model, float Distance)>();

        foreach (var model in _project.Models.Where(model => model.IsVisible))
        {
            if (!TryGetScreenBounds(model, out var screenBounds, out var distance) ||
                !selectionArea.IntersectsWith(screenBounds))
                continue;
            candidates.Add((model, distance));
        }

        var selectedByBox = isClick
            ? candidates.OrderBy(candidate => candidate.Distance).Take(1).Select(candidate => candidate.Model).ToArray()
            : candidates.Select(candidate => candidate.Model).ToArray();

        switch (_selectionOperation)
        {
            case SelectionOperation.Add:
                _highlightedModels.UnionWith(selectedByBox);
                break;
            case SelectionOperation.Subtract:
                _highlightedModels.ExceptWith(selectedByBox);
                break;
        }

        var orderedSelection = _project.Models.Where(_highlightedModels.Contains).ToArray();
        _highlightedMeshes.Clear();
        BoxSelectionChanged?.Invoke(this, new BoxSelectionChangedEventArgs(orderedSelection, []));
    }

    private void ApplyMeshBoxSelection(RectangleF selectionArea, bool isClick)
    {
        var candidates = new List<(MeshSelection Selection, float Distance)>();
        foreach (var model in _project.Models.Where(model => model.IsVisible))
        {
            for (var meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
            {
                if (model.HiddenMeshIndices.Contains(meshIndex) ||
                    !TryGetMeshScreenBounds(model, meshIndex, out var screenBounds, out var distance) ||
                    !selectionArea.IntersectsWith(screenBounds))
                    continue;
                candidates.Add((new MeshSelection(model, meshIndex), distance));
            }
        }

        var selectedByBox = isClick
            ? candidates.OrderBy(candidate => candidate.Distance).Take(1).Select(candidate => candidate.Selection).ToArray()
            : candidates.Select(candidate => candidate.Selection).ToArray();

        switch (_selectionOperation)
        {
            case SelectionOperation.Add:
                _highlightedMeshes.UnionWith(selectedByBox);
                break;
            case SelectionOperation.Subtract:
                _highlightedMeshes.ExceptWith(selectedByBox);
                break;
        }

        _highlightedModels.Clear();
        BoxSelectionChanged?.Invoke(this, new BoxSelectionChangedEventArgs([], HighlightedMeshes));
    }

    private bool TryGetScreenBounds(SceneModel model, out RectangleF screenBounds, out float distance)
    {
        screenBounds = RectangleF.Empty;
        distance = float.PositiveInfinity;
        return SceneTraversal.TryCalculateBounds(model, out var bounds) &&
               TryGetScreenBounds(bounds, out screenBounds, out distance);
    }

    private bool TryGetMeshScreenBounds(SceneModel model, int meshIndex, out RectangleF screenBounds, out float distance)
    {
        screenBounds = RectangleF.Empty;
        distance = float.PositiveInfinity;
        return SceneTraversal.TryCalculateMeshBounds(model, meshIndex, out var bounds) &&
               TryGetScreenBounds(bounds, out screenBounds, out distance);
    }

    private bool TryGetScreenBounds(SceneBounds bounds, out RectangleF screenBounds, out float distance)
    {
        screenBounds = RectangleF.Empty;
        distance = float.PositiveInfinity;
        if (_control.ClientSize.Width <= 0 || _control.ClientSize.Height <= 0)
            return false;

        var camera = _project.Camera;
        camera.Validate();
        var aspect = (float)_control.ClientSize.Width / _control.ClientSize.Height;
        var viewProjection = Matrix4x4.CreateLookAt(camera.From, camera.To, camera.Up) *
                             Matrix4x4.CreatePerspectiveFieldOfView(
                                 DegreesToRadians(camera.FieldOfViewDegrees),
                                 aspect,
                                 camera.NearPlane,
                                 camera.FarPlane);
        var minimum = bounds.Minimum;
        var maximum = bounds.Maximum;
        var corners = new[]
        {
            new Vector3(minimum.X, minimum.Y, minimum.Z),
            new Vector3(maximum.X, minimum.Y, minimum.Z),
            new Vector3(minimum.X, maximum.Y, minimum.Z),
            new Vector3(maximum.X, maximum.Y, minimum.Z),
            new Vector3(minimum.X, minimum.Y, maximum.Z),
            new Vector3(maximum.X, minimum.Y, maximum.Z),
            new Vector3(minimum.X, maximum.Y, maximum.Z),
            new Vector3(maximum.X, maximum.Y, maximum.Z)
        };

        var left = float.PositiveInfinity;
        var top = float.PositiveInfinity;
        var right = float.NegativeInfinity;
        var bottom = float.NegativeInfinity;
        var projectedAny = false;
        foreach (var corner in corners)
        {
            var clip = Vector4.Transform(new Vector4(corner, 1f), viewProjection);
            if (clip.W <= 0.0001f) continue;
            var inverseW = 1f / clip.W;
            var screenX = (clip.X * inverseW * 0.5f + 0.5f) * _control.ClientSize.Width;
            var screenY = (0.5f - clip.Y * inverseW * 0.5f) * _control.ClientSize.Height;
            left = Math.Min(left, screenX);
            right = Math.Max(right, screenX);
            top = Math.Min(top, screenY);
            bottom = Math.Max(bottom, screenY);
            projectedAny = true;
        }

        if (!projectedAny) return false;
        screenBounds = RectangleF.FromLTRB(left, top, right, bottom);
        distance = Vector3.DistanceSquared(camera.From, bounds.Center);
        return true;
    }

    private Rectangle NormalizeRectangle(Point first, Point second)
    {
        var left = Math.Clamp(Math.Min(first.X, second.X), 0, _control.ClientSize.Width);
        var right = Math.Clamp(Math.Max(first.X, second.X), 0, _control.ClientSize.Width);
        var top = Math.Clamp(Math.Min(first.Y, second.Y), 0, _control.ClientSize.Height);
        var bottom = Math.Clamp(Math.Max(first.Y, second.Y), 0, _control.ClientSize.Height);
        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private void OnMouseWheel(object? sender, MouseEventArgs e)
    {
        if (e.Delta != 0)
            ShowInputActivity(FormatMouseInput(e.Delta > 0 ? "Mouse Wheel Up" : "Mouse Wheel Down", MouseButtons.None));
        if (!NavigationEnabled || e.Delta == 0) return;
        BeginWheelCameraInteraction();
        var wheelSteps = e.Delta / 120f;
        var modifiers = Control.ModifierKeys;
        if (modifiers.HasFlag(Keys.Control))
            CameraController.AdjustFieldOfView(_project.Camera, -wheelSteps * 2f);
        else if (modifiers.HasFlag(Keys.Shift))
            CameraController.AdjustRoll(_project.Camera, wheelSteps * 2f);
        else
            CameraController.Dolly(_project.Camera, -wheelSteps * 0.12f);
        CameraChanged?.Invoke(this, EventArgs.Empty);
        _wheelInteractionTimer.Stop();
        _wheelInteractionTimer.Start();
    }

    private static string FormatMouseInput(string action, MouseButtons button)
    {
        var parts = new List<string>(4);
        var modifiers = Control.ModifierKeys;
        if (modifiers.HasFlag(Keys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(Keys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(Keys.Alt)) parts.Add("Alt");

        var buttonName = button switch
        {
            MouseButtons.Left => "Left",
            MouseButtons.Middle => "Middle",
            MouseButtons.Right => "Right",
            MouseButtons.XButton1 => "X1",
            MouseButtons.XButton2 => "X2",
            _ => string.Empty
        };
        parts.Add(string.IsNullOrEmpty(buttonName) ? action : $"{action} ({buttonName})");
        return string.Join(" + ", parts);
    }

    private void BeginPointerCameraInteraction()
    {
        if (_pointerCameraInteraction) return;
        var wasActive = _wheelCameraInteraction;
        _pointerCameraInteraction = true;
        if (!wasActive) CameraInteractionStarted?.Invoke(this, EventArgs.Empty);
    }

    private void EndPointerCameraInteraction()
    {
        if (!_pointerCameraInteraction) return;
        _pointerCameraInteraction = false;
        if (!_wheelCameraInteraction) CameraInteractionCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void BeginWheelCameraInteraction()
    {
        if (_wheelCameraInteraction) return;
        var wasActive = _pointerCameraInteraction;
        _wheelCameraInteraction = true;
        if (!wasActive) CameraInteractionStarted?.Invoke(this, EventArgs.Empty);
    }

    private void EndWheelCameraInteraction()
    {
        _wheelInteractionTimer.Stop();
        if (!_wheelCameraInteraction) return;
        _wheelCameraInteraction = false;
        if (!_pointerCameraInteraction) CameraInteractionCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void Render(
        bool force = false,
        int targetFramebuffer = 0,
        int? targetWidth = null,
        int? targetHeight = null,
        bool swapBuffers = true,
        CameraState? cameraOverride = null,
        float? orthographicHeight = null)
    {
        if (_renderingPaused || !_loaded || _control.IsDisposed || !_control.IsHandleCreated) return;
        var interactivePreview = _previewMode == ViewportPreviewMode.Preview1;
        var studioPreview = _previewMode is ViewportPreviewMode.Preview3 or ViewportPreviewMode.Preview4 or ViewportPreviewMode.Preview5;
        if (interactivePreview && !force)
        {
            var now = Environment.TickCount64;
            if (now - _lastQuickPreviewRenderTicks < QuickPreviewFrameIntervalMilliseconds) return;
            _lastQuickPreviewRenderTicks = now;
        }
        _control.MakeCurrent();
        var environmentEnabled = _project.Environment.Enabled;
        var environmentBackgroundVisible = _project.Environment.IsBackgroundVisible;
        if (_lastEnvironmentEnabled != environmentEnabled ||
            _lastEnvironmentBackgroundVisible != environmentBackgroundVisible)
        {
            _resetEnvironmentPreviewBuffers = true;
            _lastEnvironmentEnabled = environmentEnabled;
            _lastEnvironmentBackgroundVisible = environmentBackgroundVisible;
        }
        if (_resetEnvironmentPreviewBuffers)
        {
            // Force both Preview 5 buffers to be recreated/cleared on this GL
            // context. This prevents a previously visible HDR background from
            // surviving in the refraction input after ShowBackground is false.
            _sceneColorWidth = 0;
            _sceneColorHeight = 0;
            _glassBackDepthWidth = 0;
            _glassBackDepthHeight = 0;
            _resetEnvironmentPreviewBuffers = false;
        }
        if (_sceneDirty) RebuildScene();

        var viewportWidth = Math.Max(1, targetWidth ?? _control.ClientSize.Width);
        var viewportHeight = Math.Max(1, targetHeight ?? _control.ClientSize.Height);
        var useSpeedFrameBuffer = targetFramebuffer == 0 && interactivePreview && UseSpeedFrameBuffer;
        _activeRenderWidth = useSpeedFrameBuffer ? Math.Max(1, viewportWidth / 2) : viewportWidth;
        _activeRenderHeight = useSpeedFrameBuffer ? Math.Max(1, viewportHeight / 2) : viewportHeight;
        if (useSpeedFrameBuffer)
        {
            EnsureSpeedPreviewFramebuffer(_activeRenderWidth, _activeRenderHeight);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _speedPreviewFramebuffer);
        }
        else
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFramebuffer);
        }
        _activeRenderFramebuffer = useSpeedFrameBuffer ? _speedPreviewFramebuffer : targetFramebuffer;
        GL.Viewport(0, 0, _activeRenderWidth, _activeRenderHeight);

        var bg = studioPreview
            ? ToVector4(_editorColors.StudioBackground)
            : _project.RenderSettings.BackgroundColor;
        var encodedBackground = RenderColorPipeline.EncodeDisplay(
            RenderColorPipeline.SrgbToLinear(new Vector3(bg.X, bg.Y, bg.Z)));
        GL.ClearColor(encodedBackground.X, encodedBackground.Y, encodedBackground.Z, bg.W);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var camera = cameraOverride ?? _project.Camera;
        camera.Validate();
        var aspect = _activeRenderWidth / (float)_activeRenderHeight;
        var view = GLMatrix4.LookAt(ToGl(camera.From), ToGl(camera.To), ToGl(camera.Up));
        var projection = orthographicHeight is > 0f
            ? GLMatrix4.CreateOrthographic(orthographicHeight.Value * aspect, orthographicHeight.Value,
                camera.NearPlane, camera.FarPlane)
            : GLMatrix4.CreatePerspectiveFieldOfView(
                DegreesToRadians(camera.FieldOfViewDegrees), aspect, camera.NearPlane, camera.FarPlane);

        var hasEnvironment = EnsureEnvironmentTexture();
        var activeSkybox = _project.Skyboxes.FirstOrDefault(skybox => skybox.Show);
        var hasSkybox = EnsureSkyboxTexture(activeSkybox);
        if (hasSkybox && activeSkybox is not null)
            DrawSkybox(view, projection, activeSkybox.RotationDegrees);
        else if (hasEnvironment && environmentBackgroundVisible)
            DrawEnvironment(view, projection);

        if (_showPreviewOverlays && !interactivePreview && _project.RenderSettings.ShowGrid)
            DrawGrid(view, projection);

        GL.PolygonMode(TriangleFace.FrontAndBack, ScenePolygonMode);
        GL.PointSize(3f);
        GL.UseProgram(_program);
        SetMatrix("uView", view);
        SetMatrix("uProjection", projection);
        SetVector3("uCameraPosition", camera.From);
        SetInt("uPreviewMode", (int)_previewMode);
        SetLights(interactivePreview);
        var useSkyboxEnvironment = hasSkybox && activeSkybox is { UseAsEnvironment: true };
        SetEnvironment(hasEnvironment, useSkyboxEnvironment, activeSkybox);

        var visibleInstances = _renderInstances.Where(IsVisible).ToArray();

        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);
        foreach (var instance in visibleInstances.Where(instance => !GetMaterial(instance).RequiresAlphaBlending))
            DrawInstance(instance, interactivePreview);

        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);
        var alphaInstances = visibleInstances
            .Where(instance => GetMaterial(instance).RequiresAlphaBlending && !GetMaterial(instance).IsGlass);
        if (!interactivePreview)
            alphaInstances = alphaInstances.OrderByDescending(instance => DistanceSquaredFromCamera(instance, camera.From));
        foreach (var instance in alphaInstances)
            DrawInstance(instance, interactivePreview);

        var glassInstances = visibleInstances.Where(instance => GetMaterial(instance).IsGlass);
        if (!interactivePreview)
            glassInstances = glassInstances.OrderByDescending(instance => DistanceSquaredFromCamera(instance, camera.From));
        var glassInstanceArray = glassInstances.ToArray();
        if (glassInstanceArray.Length > 0)
        {
            if (_previewMode == ViewportPreviewMode.Preview5)
                RenderGlassBackDepth(glassInstanceArray, view, projection);
            // This framebuffer copy is the most expensive preview-only glass
            // operation. Reuse the last completed scene color while dragging.
            if (!interactivePreview || targetFramebuffer != 0) CaptureSceneColor();
            foreach (var instance in glassInstanceArray)
                DrawInstance(instance, interactivePreview);
        }

        GL.BindVertexArray(0);
        GL.DepthMask(true);
        if (_showPreviewOverlays)
            DrawWorldAxes(view, projection, camera);
        if (_showPreviewOverlays)
            DrawCameraGizmo(view, projection, camera);
        if (_showPreviewOverlays || _showLightGizmosOverride == true)
            DrawLightGizmos(view, projection);
        if (useSpeedFrameBuffer)
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _speedPreviewFramebuffer);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.BlitFramebuffer(0, 0, _activeRenderWidth, _activeRenderHeight,
                0, 0, viewportWidth, viewportHeight,
                ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
        if (_showPreviewOverlays && targetFramebuffer == 0)
        {
            // Editor overlays use WinForms client coordinates. Draw them only
            // after a low-resolution preview has been enlarged to the actual
            // viewport so the marquee and its one-pixel border stay aligned.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, viewportWidth, viewportHeight);
            DrawSelection(view, projection);
            DrawBoxRectangle();
        }
        if (targetFramebuffer == 0)
            DrawInputOverlay(viewportWidth, viewportHeight);
        if (swapBuffers) _control.SwapBuffers();
    }

    private bool IsVisible(RenderInstance instance) =>
        instance.Model.IsVisible && !instance.Model.HiddenMeshIndices.Contains(instance.MeshIndex);

    private static PbrMaterial GetMaterial(RenderInstance instance) =>
        instance.Model.Materials.ElementAtOrDefault(instance.Mesh.MaterialIndex) ?? DefaultMaterial;

    private void DrawInstance(RenderInstance instance, bool interactivePreview)
    {
        SetMatrix("uModel", ToGl(SceneTraversal.GetMeshWorldTransform(
            instance.Model,
            new SceneMeshInstance(instance.MeshIndex, instance.NodeTransform))));
        SetMaterial(GetMaterial(instance), interactivePreview);
        GL.BindVertexArray(instance.Geometry.VertexArray);
        GL.DrawElements(PrimitiveType.Triangles, instance.Geometry.IndexCount, DrawElementsType.UnsignedInt, 0);
    }

    private static float DistanceSquaredFromCamera(RenderInstance instance, NumericsVector3 cameraPosition)
    {
        var transform = SceneTraversal.GetMeshWorldTransform(
            instance.Model,
            new SceneMeshInstance(instance.MeshIndex, instance.NodeTransform));
        var worldCenter = NumericsVector3.Transform(instance.LocalCenter, transform);
        return NumericsVector3.DistanceSquared(cameraPosition, worldCenter);
    }

    private void CaptureSceneColor()
    {
        var width = Math.Max(1, _activeRenderWidth);
        var height = Math.Max(1, _activeRenderHeight);
        if (_sceneColorTexture == 0)
            _sceneColorTexture = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture8);
        GL.BindTexture(TextureTarget.Texture2D, _sceneColorTexture);
        if (_sceneColorWidth != width || _sceneColorHeight != height)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            // The scene-color buffer is refreshed every frame. Generating a full
            // mip chain here made the first glass object dramatically reduce FPS,
            // especially at 4K. Linear base-level sampling keeps refraction while
            // avoiding that full-screen GPU synchronization and mip generation.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            _sceneColorWidth = width;
            _sceneColorHeight = height;
        }
        GL.ReadBuffer(_activeRenderFramebuffer == 0 ? ReadBufferMode.Back : ReadBufferMode.ColorAttachment0);
        GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, width, height);
    }

    private void RenderGlassBackDepth(
        IReadOnlyList<RenderInstance> instances,
        GLMatrix4 view,
        GLMatrix4 projection)
    {
        EnsureGlassBackDepthFramebuffer(_activeRenderWidth, _activeRenderHeight);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _glassBackDepthFramebuffer);
        GL.Viewport(0, 0, _activeRenderWidth, _activeRenderHeight);
        GL.ClearColor(0f, 0f, 0f, 0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Front);
        GL.UseProgram(_glassBackDepthProgram);
        GL.UniformMatrix4(GL.GetUniformLocation(_glassBackDepthProgram, "uView"), false, ref view);
        GL.UniformMatrix4(GL.GetUniformLocation(_glassBackDepthProgram, "uProjection"), false, ref projection);
        foreach (var instance in instances)
        {
            var model = ToGl(SceneTraversal.GetMeshWorldTransform(
                instance.Model,
                new SceneMeshInstance(instance.MeshIndex, instance.NodeTransform)));
            GL.UniformMatrix4(GL.GetUniformLocation(_glassBackDepthProgram, "uModel"), false, ref model);
            GL.BindVertexArray(instance.Geometry.VertexArray);
            GL.DrawElements(PrimitiveType.Triangles, instance.Geometry.IndexCount, DrawElementsType.UnsignedInt, 0);
        }

        GL.CullFace(TriangleFace.Back);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _activeRenderFramebuffer);
        GL.Viewport(0, 0, _activeRenderWidth, _activeRenderHeight);
        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);
        GL.UseProgram(_program);
    }

    private void EnsureGlassBackDepthFramebuffer(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (_glassBackDepthFramebuffer == 0) _glassBackDepthFramebuffer = GL.GenFramebuffer();
        if (_glassBackDepthTexture == 0) _glassBackDepthTexture = GL.GenTexture();
        if (_glassBackDepthBuffer == 0) _glassBackDepthBuffer = GL.GenRenderbuffer();
        if (_glassBackDepthWidth == width && _glassBackDepthHeight == height) return;

        GL.BindTexture(TextureTarget.Texture2D, _glassBackDepthTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, width, height, 0,
            OpenTK.Graphics.OpenGL4.PixelFormat.Red, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _glassBackDepthBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _glassBackDepthFramebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _glassBackDepthTexture, 0);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, _glassBackDepthBuffer);
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException("無法建立進階珠寶厚度 Framebuffer。");
        _glassBackDepthWidth = width;
        _glassBackDepthHeight = height;
    }

    private void EnsureCaptureFramebuffer(int width, int height)
    {
        if (_captureFramebuffer == 0)
        {
            _captureFramebuffer = GL.GenFramebuffer();
            _captureColorTexture = GL.GenTexture();
            _captureDepthBuffer = GL.GenRenderbuffer();
        }
        if (_captureWidth == width && _captureHeight == height) return;

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _captureFramebuffer);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _captureColorTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0,
            OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _captureColorTexture, 0);

        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _captureDepthBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, _captureDepthBuffer);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException("無法建立 Camera 動畫輸出緩衝區。");
        _captureWidth = width;
        _captureHeight = height;
    }

    private void EnsureSpeedPreviewFramebuffer(int width, int height)
    {
        if (_speedPreviewFramebuffer == 0)
        {
            _speedPreviewFramebuffer = GL.GenFramebuffer();
            _speedPreviewColorTexture = GL.GenTexture();
            _speedPreviewDepthBuffer = GL.GenRenderbuffer();
        }
        if (_speedPreviewWidth == width && _speedPreviewHeight == height) return;

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _speedPreviewFramebuffer);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _speedPreviewColorTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0,
            OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _speedPreviewColorTexture, 0);

        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _speedPreviewDepthBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, _speedPreviewDepthBuffer);
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException("無法建立快速預覽 Framebuffer。");
        _speedPreviewWidth = width;
        _speedPreviewHeight = height;
    }

    private void DrawGrid(GLMatrix4 view, GLMatrix4 projection)
    {
        if (_grid is null) return;

        GL.UseProgram(_lineProgram);
        SetLineMatrix("uModel", GLMatrix4.Identity);
        SetLineMatrix("uView", view);
        SetLineMatrix("uProjection", projection);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        GL.DepthMask(false);
        GL.BindVertexArray(_grid.VertexArray);
        DrawGridRange(_grid.MinorFirst, _grid.MinorCount, ToVector4(_editorColors.GridMinor));
        DrawGridRange(_grid.MajorFirst, _grid.MajorCount, ToVector4(_editorColors.GridMajor));
        DrawGridRange(_grid.XAxisFirst, 2, ToVector4(_editorColors.AxisX, 0.85f));
        DrawGridRange(_grid.ZAxisFirst, 2, ToVector4(_editorColors.AxisZ, 0.85f));
        GL.BindVertexArray(0);
        GL.DepthMask(true);
    }

    private void DrawWorldAxes(GLMatrix4 view, GLMatrix4 projection, CameraState camera)
    {
        if (!_project.RenderSettings.ShowWorldAxes) return;

        var axisLength = 1f;
        if (SceneTraversal.TryCalculateBounds(_project.Models.Where(model => model.IsVisible), out var bounds))
            axisLength = MathF.Max(MathF.Max(bounds.Size.X, bounds.Size.Y), MathF.Max(bounds.Size.Z, 0.1f)) * 1.25f;
        axisLength = MathF.Max(axisLength, 0.1f);
        var arrowLength = MathF.Max(axisLength * 0.035f, 0.02f);
        var labelSize = MathF.Max(axisLength * 0.055f, 0.015f);
        var forward = NumericsVector3.Normalize(camera.To - camera.From);
        var labelRight = NumericsVector3.Normalize(NumericsVector3.Cross(forward, camera.Up));
        var labelUp = NumericsVector3.Normalize(NumericsVector3.Cross(labelRight, forward));

        GL.UseProgram(_lineProgram);
        SetLineMatrix("uModel", GLMatrix4.Identity);
        SetLineMatrix("uView", view);
        SetLineMatrix("uProjection", projection);
        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        GL.BindVertexArray(_lightGizmoVertexArray);

        DrawAxis(NumericsVector3.UnitX, ToVector4(_editorColors.AxisX), 'X');
        DrawAxis(NumericsVector3.UnitY, ToVector4(_editorColors.AxisY), 'Y');
        DrawAxis(NumericsVector3.UnitZ, ToVector4(_editorColors.AxisZ), 'Z');

        GL.BindVertexArray(0);
        GL.LineWidth(1f);
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        return;

        void DrawAxis(NumericsVector3 direction, NumericsVector4 color, char label)
        {
            var end = direction * axisLength;
            DrawGizmo(CreateAxisArrow(NumericsVector3.Zero, end, arrowLength), PrimitiveType.Lines, color);
            DrawGizmo(CreateAxisLabel(label, end + direction * (arrowLength * 1.65f), labelRight, labelUp, labelSize),
                PrimitiveType.Lines, color);
        }
    }

    private void DrawLightGizmos(GLMatrix4 view, GLMatrix4 projection)
    {
        if (!(_showLightGizmosOverride ?? _project.RenderSettings.ShowLightGizmos)) return;

        GL.UseProgram(_lineProgram);
        SetLineMatrix("uModel", GLMatrix4.Identity);
        SetLineMatrix("uView", view);
        SetLineMatrix("uProjection", projection);
        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);
        // Gizmos are editor overlays. Their light positions are often inside a
        // model (especially directional lights), so depth testing hid them.
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        GL.BindVertexArray(_lightGizmoVertexArray);

        foreach (var light in _project.Lights)
        {
            light.Validate();
            var size = GetLightGizmoWorldRadius(light.Position);
            var intensityAlpha = LightGizmoIntensityAlpha(light.Intensity);
            var isHighlighted = ReferenceEquals(light, _highlightedLight);
            var highlightColor = ToVector3(_editorColors.LightHighlight);
            var disabledColor = ToVector3(_editorColors.DisabledLight);
            var displayColor = !light.Enabled
                ? isHighlighted ? NumericsVector3.Lerp(disabledColor, highlightColor, 0.35f) : disabledColor
                : isHighlighted
                ? NumericsVector3.Lerp(light.Color, highlightColor, 0.35f)
                : light.Color;
            var displayAlpha = light.Enabled ? intensityAlpha : 0.72f;
            var color = new NumericsVector4(displayColor, isHighlighted ? 1f : displayAlpha * 0.72f);
            var lineColor = new NumericsVector4(displayColor, isHighlighted ? 1f : displayAlpha);
            switch (light.Type)
            {
                case SceneLightType.Point:
                    DrawGizmo(CreateSphere(light.Position, size), PrimitiveType.Triangles, color);
                    DrawGizmo(CreateDashedSphereLines(light.Position, light.Range), PrimitiveType.Lines,
                        new NumericsVector4(displayColor, isHighlighted ? 1f : intensityAlpha * 0.58f));
                    break;
                case SceneLightType.Directional:
                    DrawGizmo(CreateCylinder(light.Position, light.Direction, size * 0.7f, size * 2.3f), PrimitiveType.Triangles, color);
                    DrawGizmo(CreateLine(light.Position, light.Position + light.Direction * size * 9f), PrimitiveType.Lines, lineColor);
                    break;
                case SceneLightType.Spot:
                    DrawGizmo(CreateCone(light.Position, light.Direction, size, size * 2.1f), PrimitiveType.Triangles, color);
                    DrawGizmo(CreateConeLines(light, light.FallInDegrees), PrimitiveType.Lines, lineColor);
                    DrawGizmo(CreateConeLines(light, light.FallOffDegrees), PrimitiveType.Lines,
                        new NumericsVector4(displayColor, isHighlighted ? 0.75f : intensityAlpha * 0.58f));
                    break;
            }
        }

        GL.BindVertexArray(0);
        GL.LineWidth(1f);
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    private void DrawCameraGizmo(GLMatrix4 view, GLMatrix4 projection, CameraState camera)
    {
        if (!_project.RenderSettings.ShowCameraGizmo) return;

        var direction = camera.From - camera.To;
        if (direction.LengthSquared() < 1e-8f) direction = NumericsVector3.UnitZ;
        direction = NumericsVector3.Normalize(direction);
        var distance = MathF.Max(NumericsVector3.Distance(camera.From, camera.To), 0.01f);
        var size = MathF.Max(distance * 0.025f, 0.01f);
        var target = camera.To;
        var color = ToVector4(_editorColors.CameraGizmo);

        GL.UseProgram(_lineProgram);
        SetLineMatrix("uModel", GLMatrix4.Identity);
        SetLineMatrix("uView", view);
        SetLineMatrix("uProjection", projection);
        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        GL.BindVertexArray(_lightGizmoVertexArray);

        DrawGizmo(CreateLine(target - NumericsVector3.UnitX * size, target + NumericsVector3.UnitX * size), PrimitiveType.Lines, color);
        DrawGizmo(CreateLine(target - NumericsVector3.UnitY * size, target + NumericsVector3.UnitY * size), PrimitiveType.Lines, color);
        DrawGizmo(CreateLine(target - NumericsVector3.UnitZ * size, target + NumericsVector3.UnitZ * size), PrimitiveType.Lines, color);
        DrawGizmo(CreateLine(target, target + direction * size * 5f), PrimitiveType.Lines, color);

        GL.BindVertexArray(0);
        GL.LineWidth(1f);
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    private void DrawGizmo(float[] vertices, PrimitiveType primitiveType, NumericsVector4 color)
    {
        if (vertices.Length == 0) return;
        GL.BindBuffer(BufferTarget.ArrayBuffer, _lightGizmoVertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StreamDraw);
        SetLineColor(color);
        if (primitiveType == PrimitiveType.Lines) GL.LineWidth(1f);
        GL.DrawArrays(primitiveType, 0, vertices.Length / 3);
    }

    private float GetLightGizmoWorldRadius(NumericsVector3 position)
    {
        var pixels = Math.Clamp(_project.RenderSettings.LightGizmoSizePixels, 12, 96);
        var distance = MathF.Max(NumericsVector3.Distance(_project.Camera.From, position), 0.01f);
        var fovRadians = MathF.PI * _project.Camera.FieldOfViewDegrees / 180f;
        var viewportHeight = Math.Max(1, _control.ClientSize.Height);
        return 0.5f * pixels * (2f * distance * MathF.Tan(fovRadians * 0.5f) / viewportHeight);
    }

    private static float LightGizmoIntensityAlpha(float intensity) =>
        Math.Clamp(0.25f + 0.75f * MathF.Log2(1f + Math.Max(0f, intensity)) / MathF.Log2(11f), 0.25f, 1f);

    private static float[] CreateLine(NumericsVector3 from, NumericsVector3 to) =>
        [from.X, from.Y, from.Z, to.X, to.Y, to.Z];

    private static float[] CreateAxisArrow(NumericsVector3 from, NumericsVector3 to, float headLength)
    {
        var vertices = new List<float>(30);
        AddLine(vertices, from, to);
        var direction = NumericsVector3.Normalize(to - from);
        CreateBasis(direction, out var right, out var up);
        var basePoint = to - direction * headLength;
        AddLine(vertices, to, basePoint + right * headLength * 0.45f);
        AddLine(vertices, to, basePoint - right * headLength * 0.45f);
        AddLine(vertices, to, basePoint + up * headLength * 0.45f);
        AddLine(vertices, to, basePoint - up * headLength * 0.45f);
        return vertices.ToArray();
    }

    private static float[] CreateAxisLabel(char label, NumericsVector3 center, NumericsVector3 right, NumericsVector3 up, float size)
    {
        var vertices = new List<float>(24);
        NumericsVector3 Point(float x, float y) => center + right * (x * size) + up * (y * size);
        switch (label)
        {
            case 'X':
                AddLine(vertices, Point(-0.5f, -0.5f), Point(0.5f, 0.5f));
                AddLine(vertices, Point(-0.5f, 0.5f), Point(0.5f, -0.5f));
                break;
            case 'Y':
                AddLine(vertices, Point(-0.5f, 0.5f), Point(0f, 0f));
                AddLine(vertices, Point(0.5f, 0.5f), Point(0f, 0f));
                AddLine(vertices, Point(0f, 0f), Point(0f, -0.5f));
                break;
            case 'Z':
                AddLine(vertices, Point(-0.5f, 0.5f), Point(0.5f, 0.5f));
                AddLine(vertices, Point(0.5f, 0.5f), Point(-0.5f, -0.5f));
                AddLine(vertices, Point(-0.5f, -0.5f), Point(0.5f, -0.5f));
                break;
        }
        return vertices.ToArray();
    }

    private static float[] CreateSphere(NumericsVector3 center, float radius)
    {
        const int slices = 16, stacks = 10;
        var vertices = new List<float>(slices * stacks * 18);
        for (var stack = 0; stack < stacks; stack++)
        {
            var a0 = -MathF.PI / 2f + MathF.PI * stack / stacks;
            var a1 = -MathF.PI / 2f + MathF.PI * (stack + 1) / stacks;
            for (var slice = 0; slice < slices; slice++)
            {
                var b0 = 2f * MathF.PI * slice / slices;
                var b1 = 2f * MathF.PI * (slice + 1) / slices;
                var p00 = center + radius * new NumericsVector3(MathF.Cos(a0) * MathF.Cos(b0), MathF.Sin(a0), MathF.Cos(a0) * MathF.Sin(b0));
                var p01 = center + radius * new NumericsVector3(MathF.Cos(a0) * MathF.Cos(b1), MathF.Sin(a0), MathF.Cos(a0) * MathF.Sin(b1));
                var p10 = center + radius * new NumericsVector3(MathF.Cos(a1) * MathF.Cos(b0), MathF.Sin(a1), MathF.Cos(a1) * MathF.Sin(b0));
                var p11 = center + radius * new NumericsVector3(MathF.Cos(a1) * MathF.Cos(b1), MathF.Sin(a1), MathF.Cos(a1) * MathF.Sin(b1));
                AddTriangle(vertices, p00, p10, p11);
                AddTriangle(vertices, p00, p11, p01);
            }
        }
        return vertices.ToArray();
    }

    private static float[] CreateCylinder(NumericsVector3 center, NumericsVector3 direction, float radius, float length)
    {
        const int segments = 16;
        CreateBasis(direction, out var right, out var up);
        var half = direction * (length * 0.5f);
        var vertices = new List<float>(segments * 36);
        for (var i = 0; i < segments; i++)
        {
            var a0 = 2f * MathF.PI * i / segments;
            var a1 = 2f * MathF.PI * (i + 1) / segments;
            var r0 = radius * (right * MathF.Cos(a0) + up * MathF.Sin(a0));
            var r1 = radius * (right * MathF.Cos(a1) + up * MathF.Sin(a1));
            var p00 = center - half + r0; var p01 = center - half + r1;
            var p10 = center + half + r0; var p11 = center + half + r1;
            AddTriangle(vertices, p00, p10, p11); AddTriangle(vertices, p00, p11, p01);
            AddTriangle(vertices, center - half, p01, p00); AddTriangle(vertices, center + half, p10, p11);
        }
        return vertices.ToArray();
    }

    private static float[] CreateCone(NumericsVector3 apex, NumericsVector3 direction, float radius, float length)
    {
        const int segments = 16;
        CreateBasis(direction, out var right, out var up);
        var baseCenter = apex + direction * length;
        var vertices = new List<float>(segments * 18);
        for (var i = 0; i < segments; i++)
        {
            var a0 = 2f * MathF.PI * i / segments;
            var a1 = 2f * MathF.PI * (i + 1) / segments;
            var p0 = baseCenter + radius * (right * MathF.Cos(a0) + up * MathF.Sin(a0));
            var p1 = baseCenter + radius * (right * MathF.Cos(a1) + up * MathF.Sin(a1));
            AddTriangle(vertices, apex, p0, p1); AddTriangle(vertices, baseCenter, p1, p0);
        }
        return vertices.ToArray();
    }

    private static float[] CreateConeLines(SceneLight light, float angleDegrees)
    {
        const int segments = 32;
        CreateBasis(light.Direction, out var right, out var up);
        var length = light.Range;
        var center = light.Position + light.Direction * length;
        var radius = MathF.Tan(DegreesToRadians(angleDegrees)) * length;
        var vertices = new List<float>((segments * 2 + 8) * 3);
        for (var i = 0; i < segments; i++)
        {
            var a0 = 2f * MathF.PI * i / segments;
            var a1 = 2f * MathF.PI * (i + 1) / segments;
            var p0 = center + radius * (right * MathF.Cos(a0) + up * MathF.Sin(a0));
            var p1 = center + radius * (right * MathF.Cos(a1) + up * MathF.Sin(a1));
            if (i % 2 == 0) AddLine(vertices, p0, p1);
            if (i % 8 == 0) AddLine(vertices, light.Position, p0);
        }
        return vertices.ToArray();
    }

    private static float[] CreateDashedSphereLines(NumericsVector3 center, float radius)
    {
        const int segments = 32;
        const int rings = 12;
        radius = MathF.Max(radius, 0.001f);
        var vertices = new List<float>(segments * rings * 6);

        // Latitude rings, with every other segment omitted to produce dashes.
        for (var ring = 1; ring < rings; ring++)
        {
            var latitude = -MathF.PI / 2f + MathF.PI * ring / rings;
            for (var segment = 0; segment < segments; segment += 2)
            {
                var a0 = 2f * MathF.PI * segment / segments;
                var a1 = 2f * MathF.PI * (segment + 1) / segments;
                var p0 = center + radius * new NumericsVector3(MathF.Cos(latitude) * MathF.Cos(a0), MathF.Sin(latitude), MathF.Cos(latitude) * MathF.Sin(a0));
                var p1 = center + radius * new NumericsVector3(MathF.Cos(latitude) * MathF.Cos(a1), MathF.Sin(latitude), MathF.Cos(latitude) * MathF.Sin(a1));
                AddLine(vertices, p0, p1);
            }
        }

        // Sparse dashed meridians show the volume without obscuring the scene.
        for (var segment = 0; segment < segments; segment += 4)
        {
            var longitude = 2f * MathF.PI * segment / segments;
            for (var ring = 0; ring < rings; ring += 2)
            {
                var a0 = -MathF.PI / 2f + MathF.PI * ring / rings;
                var a1 = -MathF.PI / 2f + MathF.PI * (ring + 1) / rings;
                var p0 = center + radius * new NumericsVector3(MathF.Cos(a0) * MathF.Cos(longitude), MathF.Sin(a0), MathF.Cos(a0) * MathF.Sin(longitude));
                var p1 = center + radius * new NumericsVector3(MathF.Cos(a1) * MathF.Cos(longitude), MathF.Sin(a1), MathF.Cos(a1) * MathF.Sin(longitude));
                AddLine(vertices, p0, p1);
            }
        }

        return vertices.ToArray();
    }

    private static void CreateBasis(NumericsVector3 direction, out NumericsVector3 right, out NumericsVector3 up)
    {
        direction = NumericsVector3.Normalize(direction);
        var reference = MathF.Abs(direction.Y) < 0.95f ? NumericsVector3.UnitY : NumericsVector3.UnitX;
        right = NumericsVector3.Normalize(NumericsVector3.Cross(direction, reference));
        up = NumericsVector3.Normalize(NumericsVector3.Cross(right, direction));
    }

    private static void AddTriangle(List<float> vertices, NumericsVector3 a, NumericsVector3 b, NumericsVector3 c)
    {
        AddPoint(vertices, a); AddPoint(vertices, b); AddPoint(vertices, c);
    }

    private static void AddLine(List<float> vertices, NumericsVector3 a, NumericsVector3 b)
    {
        AddPoint(vertices, a); AddPoint(vertices, b);
    }

    private static void AddPoint(List<float> vertices, NumericsVector3 point) => vertices.AddRange([point.X, point.Y, point.Z]);

    private void DrawGridRange(int first, int count, NumericsVector4 color)
    {
        if (count == 0) return;
        SetLineColor(color);
        GL.DrawArrays(PrimitiveType.Lines, first, count);
    }

    private void DrawSelection(GLMatrix4 view, GLMatrix4 projection)
    {
        var hasSelectedMesh = HighlightedMeshes.Count > 0;
        if (!_project.RenderSettings.ShowSelectionHighlight || _highlightedModels.Count == 0 && !hasSelectedMesh) return;

        if (_highlightedModels.Count > 0)
        {
            GL.UseProgram(_lineProgram);
            SetLineMatrix("uView", view);
            SetLineMatrix("uProjection", projection);
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonOffset(-1f, -1f);
            GL.LineWidth(1f);  // 選取mesh的輪廓線寬度
            GL.DepthMask(false);
            SetLineColor(ToVector4(_editorColors.Selection));
            foreach (var instance in _renderInstances.Where(instance =>
                         _highlightedModels.Contains(instance.Model) &&
                         instance.Model.IsVisible &&
                         !instance.Model.HiddenMeshIndices.Contains(instance.MeshIndex)))
            {
                SetLineMatrix("uModel", ToGl(SceneTraversal.GetMeshWorldTransform(
                    instance.Model,
                    new SceneMeshInstance(instance.MeshIndex, instance.NodeTransform))));
                GL.BindVertexArray(instance.Geometry.VertexArray);
                GL.DrawElements(PrimitiveType.Triangles, instance.Geometry.IndexCount, DrawElementsType.UnsignedInt, 0);
            }
            GL.BindVertexArray(0);
            GL.DepthMask(true);
            GL.LineWidth(1f);
            GL.Disable(EnableCap.PolygonOffsetLine);
            GL.Enable(EnableCap.CullFace);
            GL.PolygonMode(TriangleFace.FrontAndBack, ScenePolygonMode);
        }

        if (hasSelectedMesh)
            DrawSelectedMeshOutline(view, projection);
    }

    private void DrawSelectedMeshOutline(GLMatrix4 view, GLMatrix4 projection)
    {
        EnsureSelectionMaskResources();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _selectionMaskFramebuffer);
        GL.Viewport(0, 0, _selectionMaskWidth, _selectionMaskHeight);
        GL.ClearColor(0f, 0f, 0f, 0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.UseProgram(_selectionMaskProgram);
        SetSelectionMaskMatrix("uView", view);
        SetSelectionMaskMatrix("uProjection", projection);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.DepthMask(true);
        GL.ColorMask(false, false, false, false);

        foreach (var instance in _renderInstances.Where(IsVisible))
            DrawSelectionMaskInstance(instance);

        GL.ColorMask(true, true, true, true);
        GL.DepthMask(false);
        GL.DepthFunc(DepthFunction.Lequal);
        foreach (var instance in _renderInstances.Where(instance =>
                     _highlightedMeshes.Contains(new MeshSelection(instance.Model, instance.MeshIndex))))
            DrawSelectionMaskInstance(instance);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, Math.Max(1, _control.ClientSize.Width), Math.Max(1, _control.ClientSize.Height));
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.UseProgram(_selectionOutlineProgram);
        GL.Uniform1(GL.GetUniformLocation(_selectionOutlineProgram, "uMask"), 9);
        GL.Uniform2(GL.GetUniformLocation(_selectionOutlineProgram, "uTexelSize"),
            3f / _selectionMaskWidth, 3f / _selectionMaskHeight);  // 設定像素大小，用於輪廓線的模糊效果
        var selectionColor = ToVector4(_editorColors.Selection);
        GL.Uniform4(GL.GetUniformLocation(_selectionOutlineProgram, "uColor"),
            selectionColor.X, selectionColor.Y, selectionColor.Z, selectionColor.W);
        GL.ActiveTexture(TextureUnit.Texture9);
        GL.BindTexture(TextureTarget.Texture2D, _selectionMaskTexture);
        GL.BindVertexArray(_environmentVertexArray);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        GL.BindVertexArray(0);

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.DepthMask(true);
        GL.Enable(EnableCap.CullFace);
        GL.PolygonMode(TriangleFace.FrontAndBack, ScenePolygonMode);
    }

    private void DrawSelectionMaskInstance(RenderInstance instance)
    {
        var model = ToGl(SceneTraversal.GetMeshWorldTransform(
            instance.Model,
            new SceneMeshInstance(instance.MeshIndex, instance.NodeTransform)));
        SetSelectionMaskMatrix("uModel", model);
        GL.BindVertexArray(instance.Geometry.VertexArray);
        GL.DrawElements(PrimitiveType.Triangles, instance.Geometry.IndexCount, DrawElementsType.UnsignedInt, 0);
    }

    private void EnsureSelectionMaskResources()
    {
        var width = Math.Max(1, _control.ClientSize.Width);
        var height = Math.Max(1, _control.ClientSize.Height);
        if (_selectionMaskFramebuffer == 0) _selectionMaskFramebuffer = GL.GenFramebuffer();
        if (_selectionMaskTexture == 0) _selectionMaskTexture = GL.GenTexture();
        if (_selectionMaskDepthBuffer == 0) _selectionMaskDepthBuffer = GL.GenRenderbuffer();
        if (_selectionMaskWidth == width && _selectionMaskHeight == height) return;

        GL.BindTexture(TextureTarget.Texture2D, _selectionMaskTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, width, height, 0,
            OpenTK.Graphics.OpenGL4.PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _selectionMaskDepthBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _selectionMaskFramebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _selectionMaskTexture, 0);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, _selectionMaskDepthBuffer);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException("無法建立 Mesh 選取輪廓緩衝區。");
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _selectionMaskWidth = width;
        _selectionMaskHeight = height;
    }

    private void DrawBoxRectangle()
    {
        if (!_isBoxSelecting) return;
        var rectangle = NormalizeRectangle(_boxStart, _boxCurrent);
        if (rectangle.Width < 1 || rectangle.Height < 1) return;

        var thickness = Math.Min(2, Math.Min(rectangle.Width, rectangle.Height));
        var height = _control.ClientSize.Height;
        GL.Enable(EnableCap.ScissorTest);
        var selectionColor = ToVector4(_editorColors.Selection);
        GL.ClearColor(selectionColor.X, selectionColor.Y, selectionColor.Z, selectionColor.W);
        GL.Scissor(rectangle.Left, height - rectangle.Top - thickness, rectangle.Width, thickness);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Scissor(rectangle.Left, height - rectangle.Bottom, rectangle.Width, thickness);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Scissor(rectangle.Left, height - rectangle.Bottom, thickness, rectangle.Height);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Scissor(rectangle.Right - thickness, height - rectangle.Bottom, thickness, rectangle.Height);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Disable(EnableCap.ScissorTest);
    }

    private void DrawInputOverlay(int viewportWidth, int viewportHeight)
    {
        if (!_inputOverlayEnabled || string.IsNullOrEmpty(_inputOverlayText)) return;
        var now = Stopwatch.GetTimestamp();
        if (now >= _inputOverlayExpiresAt) return;

        if (_inputOverlayTextureDirty || _inputOverlayTexture == 0)
            UpdateInputOverlayTexture();
        if (_inputOverlayTexture == 0 || _inputOverlayTextureWidth <= 0 || _inputOverlayTextureHeight <= 0) return;

        var remainingSeconds = (_inputOverlayExpiresAt - now) / (double)Stopwatch.Frequency;
        var opacity = remainingSeconds < InputOverlayFadeSeconds
            ? (float)Math.Clamp(remainingSeconds / InputOverlayFadeSeconds, 0d, 1d)
            : 1f;
        const float margin = 12f;
        var x0 = -1f + 2f * margin / Math.Max(1, viewportWidth);
        var y0 = -1f + 2f * margin / Math.Max(1, viewportHeight);
        var x1 = x0 + 2f * _inputOverlayTextureWidth / Math.Max(1, viewportWidth);
        var y1 = y0 + 2f * _inputOverlayTextureHeight / Math.Max(1, viewportHeight);
        float[] vertices =
        [
            x0, y0, 0f, 0f,
            x1, y0, 1f, 0f,
            x0, y1, 0f, 1f,
            x1, y1, 1f, 1f
        ];

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, viewportWidth, viewportHeight);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        GL.UseProgram(_inputOverlayProgram);
        GL.Uniform1(GL.GetUniformLocation(_inputOverlayProgram, "uTexture"), 10);
        GL.Uniform1(GL.GetUniformLocation(_inputOverlayProgram, "uOpacity"), opacity);
        GL.ActiveTexture(TextureUnit.Texture10);
        GL.BindTexture(TextureTarget.Texture2D, _inputOverlayTexture);
        GL.BindVertexArray(_inputOverlayVertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _inputOverlayVertexBuffer);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertices.Length * sizeof(float), vertices);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        GL.BindVertexArray(0);
        GL.PolygonMode(TriangleFace.FrontAndBack, ScenePolygonMode);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
    }

    private void UpdateInputOverlayTexture()
    {
        _inputOverlayTextureDirty = false;
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        var baseFont = SystemFonts.MessageBoxFont ?? Control.DefaultFont;
        using var font = new Font(baseFont.FontFamily, _inputOverlayFontSize,
            baseFont.Style, GraphicsUnit.Point);
        var textSize = TextRenderer.MeasureText(_inputOverlayText, font, new Size(int.MaxValue, int.MaxValue), flags);
        var width = Math.Max(1, textSize.Width + 20);
        var height = Math.Max(1, textSize.Height + 12);
        using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var backgroundBrush = new SolidBrush(_inputOverlayBackgroundColor);
            graphics.FillRectangle(backgroundBrush, 0, 0, width, height);
            TextRenderer.DrawText(graphics, _inputOverlayText, font,
                new Point(10, 6), _inputOverlayTextColor, flags);
        }

        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
        var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            if (_inputOverlayTexture == 0) _inputOverlayTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _inputOverlayTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
        _inputOverlayTextureWidth = width;
        _inputOverlayTextureHeight = height;
    }

    private void SetLights(bool interactivePreview)
    {
        var lights = _project.Lights
            .Where(light => light.Enabled)
            .OrderByDescending(light => light.Intensity)
            .Take(MaximumLights)
            .ToArray();
        SetInt("uLightCount", lights.Length);
        for (var index = 0; index < lights.Length; index++)
        {
            var light = lights[index];
            light.Validate();
            SetInt($"uLightTypes[{index}]", (int)light.Type);
            SetVector3($"uLightPositions[{index}]", light.Position);
            SetVector3($"uLightDirections[{index}]", light.Direction);
            SetVector3($"uLightColors[{index}]", light.Color * light.Intensity);
            SetFloat($"uLightRanges[{index}]", light.Range);
            SetFloat($"uLightInnerCos[{index}]", MathF.Cos(DegreesToRadians(light.FallInDegrees)));
            SetFloat($"uLightOuterCos[{index}]", MathF.Cos(DegreesToRadians(light.FallOffDegrees)));
        }
    }

    private bool EnsureEnvironmentTexture()
    {
        var environment = _project.Environment;
        environment.Validate();
        // A new/cleared project can have HDRI enabled before a file is chosen.
        // Never pass that empty value to Path.GetFullPath via ResolveAssetPath.
        if (!environment.Enabled || string.IsNullOrWhiteSpace(environment.Path)) return false;
        var path = ResolveAssetPath(environment.Path);
        if (!File.Exists(path)) return false;
        if (_environmentTexture != 0 && path.Equals(_environmentTexturePath, StringComparison.OrdinalIgnoreCase)) return true;

        try
        {
            var image = RadianceHdrLoader.Load(path);
            if (_environmentTexture != 0) GL.DeleteTexture(_environmentTexture);
            _environmentTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _environmentTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, image.Width, image.Height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Rgb, PixelType.Float, image.Pixels);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            _environmentMaxLod = MathF.Floor(MathF.Log2(Math.Max(image.Width, image.Height)));
            _environmentTexturePath = path;
            _failedTextures.Remove(path);
            return true;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            if (_failedTextures.Add(path))
                RendererError?.Invoke(this, $"無法載入 HDR 環境：{Path.GetFileName(path)}，{ex.Message}");
            return false;
        }
    }

    private void SetEnvironment(bool hdrEnabled, bool useSkyboxEnvironment, SkyboxSettings? activeSkybox)
    {
        SetInt("uHasEnvironment", hdrEnabled || useSkyboxEnvironment ? 1 : 0);
        SetInt("uEnvironmentMap", 7);
        SetInt("uSkyboxEnvironmentMap", 12);
        SetInt("uUseSkyboxEnvironment", useSkyboxEnvironment ? 1 : 0);
        SetFloat("uEnvironmentIntensity", useSkyboxEnvironment ? 1f : _project.Environment.Intensity);
        SetFloat("uEnvironmentRotation", DegreesToRadians(useSkyboxEnvironment
            ? activeSkybox!.RotationDegrees
            : _project.Environment.RotationDegrees));
        SetFloat("uEnvironmentMaxLod", useSkyboxEnvironment ? _skyboxMaxLod : _environmentMaxLod);
        GL.ActiveTexture(TextureUnit.Texture7);
        GL.BindTexture(TextureTarget.Texture2D, hdrEnabled ? _environmentTexture : 0);
        GL.ActiveTexture(TextureUnit.Texture12);
        GL.BindTexture(TextureTarget.TextureCubeMap, useSkyboxEnvironment ? _skyboxTexture : 0);
    }

    private void DrawEnvironment(GLMatrix4 view, GLMatrix4 projection)
    {
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        GL.UseProgram(_environmentProgram);
        GL.UniformMatrix4(GL.GetUniformLocation(_environmentProgram, "uView"), false, ref view);
        GL.UniformMatrix4(GL.GetUniformLocation(_environmentProgram, "uProjection"), false, ref projection);
        GL.Uniform1(GL.GetUniformLocation(_environmentProgram, "uEnvironmentMap"), 7);
        GL.Uniform1(GL.GetUniformLocation(_environmentProgram, "uIntensity"), _project.Environment.Intensity);
        GL.Uniform1(GL.GetUniformLocation(_environmentProgram, "uRotation"), DegreesToRadians(_project.Environment.RotationDegrees));
        GL.Uniform1(GL.GetUniformLocation(_environmentProgram, "uLod"), _project.Environment.BackgroundBlur * _environmentMaxLod);
        GL.ActiveTexture(TextureUnit.Texture7);
        GL.BindTexture(TextureTarget.Texture2D, _environmentTexture);
        GL.BindVertexArray(_environmentVertexArray);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        GL.BindVertexArray(0);
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
    }

    private bool EnsureSkyboxTexture(SkyboxSettings? skybox)
    {
        if (skybox is null) return false;
        if (skybox.FacePaths.Any(string.IsNullOrWhiteSpace)) return false;
        var paths = skybox.FacePaths.Select(ResolveAssetPath).ToArray();
        if (paths.Any(path => !File.Exists(path))) return false;
        var signature = string.Join('|', paths.Select(path => $"{path}:{File.GetLastWriteTimeUtc(path).Ticks}"));
        if (_skyboxTexture != 0 && signature.Equals(_skyboxTextureSignature, StringComparison.Ordinal)) return true;

        try
        {
            if (_skyboxTexture != 0) GL.DeleteTexture(_skyboxTexture);
            _skyboxTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMap, _skyboxTexture);
            var targets = new[]
            {
                TextureTarget.TextureCubeMapPositiveX, TextureTarget.TextureCubeMapNegativeX,
                TextureTarget.TextureCubeMapPositiveY, TextureTarget.TextureCubeMapNegativeY,
                TextureTarget.TextureCubeMapPositiveZ, TextureTarget.TextureCubeMapNegativeZ
            };
            var faceSize = 1;
            for (var index = 0; index < paths.Length; index++)
            {
                using var bitmap = new Bitmap(paths[index]);
                faceSize = Math.Max(faceSize, Math.Max(bitmap.Width, bitmap.Height));
                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    GL.TexImage2D(targets[index], 0, PixelInternalFormat.Srgb8Alpha8, bitmap.Width, bitmap.Height, 0,
                        OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                }
                finally { bitmap.UnlockBits(data); }
            }
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);
            _skyboxMaxLod = MathF.Floor(MathF.Log2(faceSize));
            _skyboxTextureSignature = signature;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            if (_failedTextures.Add(signature))
                RendererError?.Invoke(this, $"無法載入 Skybox：{skybox.Name}，{ex.Message}");
            return false;
        }
    }

    private void DrawSkybox(GLMatrix4 view, GLMatrix4 projection, float rotationDegrees)
    {
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        GL.UseProgram(_skyboxProgram);
        GL.UniformMatrix4(GL.GetUniformLocation(_skyboxProgram, "uView"), false, ref view);
        GL.UniformMatrix4(GL.GetUniformLocation(_skyboxProgram, "uProjection"), false, ref projection);
        GL.Uniform1(GL.GetUniformLocation(_skyboxProgram, "uSkybox"), 12);
        GL.Uniform1(GL.GetUniformLocation(_skyboxProgram, "uRotation"), DegreesToRadians(rotationDegrees));
        GL.ActiveTexture(TextureUnit.Texture12);
        GL.BindTexture(TextureTarget.TextureCubeMap, _skyboxTexture);
        GL.BindVertexArray(_environmentVertexArray);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        GL.BindVertexArray(0);
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
    }

    private void RebuildScene()
    {
        _highlightedModels.RemoveWhere(model => !_project.Models.Contains(model));
        _highlightedMeshes.RemoveWhere(selection =>
            !_project.Models.Contains(selection.Model) ||
            (uint)selection.MeshIndex >= (uint)selection.Model.Meshes.Count);
        foreach (var mesh in _gpuMeshes) mesh.Dispose();
        _gpuMeshes.Clear();
        _renderInstances.Clear();
        foreach (var model in _project.Models)
        {
            var geometry = model.Meshes.Select(GpuMesh.Create).ToArray();
            _gpuMeshes.AddRange(geometry);
            foreach (var instance in SceneTraversal.GetMeshInstances(model))
            {
                _renderInstances.Add(new RenderInstance(
                    model,
                    instance.MeshIndex,
                    model.Meshes[instance.MeshIndex],
                    geometry[instance.MeshIndex],
                    instance.NodeTransform,
                    CalculateMeshCenter(model.Meshes[instance.MeshIndex])));
            }
        }
        _sceneDirty = false;
    }

    private static NumericsVector3 CalculateMeshCenter(MeshData mesh)
    {
        if (mesh.Positions.Length == 0) return NumericsVector3.Zero;
        var minimum = new NumericsVector3(float.PositiveInfinity);
        var maximum = new NumericsVector3(float.NegativeInfinity);
        foreach (var position in mesh.Positions)
        {
            minimum = NumericsVector3.Min(minimum, position);
            maximum = NumericsVector3.Max(maximum, position);
        }
        return (minimum + maximum) * 0.5f;
    }

    private void SetMaterial(PbrMaterial material, bool interactivePreview)
    {
        material.Validate();
        SetVector4("uBaseColor", material.BaseColor);
        SetFloat("uMetallic", material.Metallic);
        SetFloat("uRoughness", Math.Clamp(material.Roughness, 0f, 1f));
        SetFloat("uNormalScale", material.NormalScale);
        var bumpScale = material.TextureStacks.TryGetValue(TextureSemantic.Bump, out var bumpStack)
            ? bumpStack.Layers.LastOrDefault(layer => layer.Enabled)?.Bump.Strength ?? 1f
            : 1f;
        SetFloat("uBumpScale", bumpScale);
        SetFloat("uAo", material.AmbientOcclusion);
        SetVector3("uEmissive", material.Emissive * material.EmissiveStrength);
        SetFloat("uOpacity", material.Opacity);
        SetInt("uMaterialMode", (int)material.RenderMode);
        SetInt("uFastPreview", interactivePreview ? 1 : 0);
        SetFloat("uAlphaCutoff", material.AlphaCutoff);
        SetFloat("uTransmission", material.Transmission);
        SetFloat("uIor", material.IndexOfRefraction);
        SetFloat("uThickness", material.Thickness);
        SetFloat("uRefractionStrength", material.RefractionStrength);
        SetFloat("uDispersion", material.Dispersion);
        SetVector3("uAbsorptionColor", material.AbsorptionColor);
        SetInt("uSceneColorMap", 8);
        SetInt("uGlassBackDepthMap", 11);
        var hasMeasuredThickness = _previewMode == ViewportPreviewMode.Preview5 &&
                                   material.IsGlass && _glassBackDepthTexture != 0;
        SetInt("uHasGlassThicknessMap", hasMeasuredThickness ? 1 : 0);
        GL.Uniform2(GL.GetUniformLocation(_program, "uViewportSize"),
            (float)Math.Max(1, _activeRenderWidth),
            (float)Math.Max(1, _activeRenderHeight));
        if (material.IsGlass && _sceneColorTexture != 0)
        {
            GL.ActiveTexture(TextureUnit.Texture8);
            GL.BindTexture(TextureTarget.Texture2D, _sceneColorTexture);
        }
        if (hasMeasuredThickness)
        {
            GL.ActiveTexture(TextureUnit.Texture11);
            GL.BindTexture(TextureTarget.Texture2D, _glassBackDepthTexture);
        }
        BindTexture(material, TextureSemantic.BaseColor, "uBaseColorMap", "uHasBaseColorMap", "uBaseColorMapping", 0, true);
        BindTexture(material, TextureSemantic.Metallic, "uMetallicMap", "uHasMetallicMap", "uMetallicMapping", 1, false);
        BindTexture(material, TextureSemantic.Roughness, "uRoughnessMap", "uHasRoughnessMap", "uRoughnessMapping", 2, false);
        BindTexture(material, TextureSemantic.Normal, "uNormalMap", "uHasNormalMap", "uNormalMapping", 3, false);
        BindTexture(material, TextureSemantic.AmbientOcclusion, "uAoMap", "uHasAoMap", "uAoMapping", 4, false);
        BindTexture(material, TextureSemantic.Emissive, "uEmissiveMap", "uHasEmissiveMap", "uEmissiveMapping", 5, true);
        BindTexture(material, TextureSemantic.Opacity, "uOpacityMap", "uHasOpacityMap", "uOpacityMapping", 6, false);
        BindTexture(material, TextureSemantic.Bump, "uBumpMap", "uHasBumpMap", "uBumpMapping", 10, false);
        if (material.DoubleSided || _previewMode == ViewportPreviewMode.Preview5 && material.IsGlass)
            GL.Disable(EnableCap.CullFace);
        else
            GL.Enable(EnableCap.CullFace);
    }

    private void BindTexture(PbrMaterial material, TextureSemantic semantic, string sampler, string enabledUniform,
        string mappingUniform, int unit, bool sRgb)
    {
        material.TextureStacks.TryGetValue(semantic, out var stack);
        var showTextures = _showTexturesOverride ?? _project.RenderSettings.ShowTextures;
        var hasStack = showTextures &&
                       stack is { Enabled: true } && stack.Layers.Any(layer => layer.Enabled);
        material.Textures.TryGetValue(semantic, out var slot);
        var hasSlot = !hasStack && showTextures && slot is { Enabled: true };
        var mapping = hasStack ? stack!.Layers.LastOrDefault(layer => layer.Enabled)?.Mapping : null;
        SetInt(mappingUniform + "Mode", (int)(mapping?.Mode ?? TextureMappingMode.Uv));
        SetInt(mappingUniform + "Axis", (int)(mapping?.Axis ?? TextureProjectionAxis.Auto));
        SetInt(mappingUniform + "Space", (int)(mapping?.Space ?? TextureProjectionSpace.Object));
        SetFloat(mappingUniform + "Blend", mapping?.TriplanarBlend ?? 4f);
        var resolvedPath = hasSlot ? ResolveAssetPath(slot!.Path) : string.Empty;
        SetInt(sampler, unit);
        if (!hasStack && (!hasSlot || !File.Exists(resolvedPath)))
        {
            SetInt(enabledUniform, 0);
            return;
        }

        try
        {
            var texture = hasStack ? GetTexture(material, semantic, stack!, sRgb) : GetTexture(resolvedPath, sRgb);
            SetInt(enabledUniform, 1);
            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            if (!string.IsNullOrWhiteSpace(resolvedPath)) _failedTextures.Remove(resolvedPath);
        }
        catch (Exception ex) when (ex is ArgumentException or ExternalException or IOException or UnauthorizedAccessException)
        {
            SetInt(enabledUniform, 0);
            if (_failedTextures.Add(resolvedPath))
                RendererError?.Invoke(this, $"無法載入貼圖 {Path.GetFileName(resolvedPath)}：{ex.Message}");
        }
    }

    private int GetTexture(PbrMaterial material, TextureSemantic semantic, TextureStack stack, bool sRgb)
    {
        var fingerprint = JsonSerializer.Serialize(stack);
        foreach (var layer in stack.Layers)
        {
            if (!string.IsNullOrWhiteSpace(layer.Path)) fingerprint += FingerprintPath(layer.Path);
            if (layer.Mask.Enabled && !string.IsNullOrWhiteSpace(layer.Mask.Path)) fingerprint += FingerprintPath(layer.Mask.Path);
        }

        var key = (material, semantic);
        if (_stackTextures.TryGetValue(key, out var cached) && cached.Fingerprint == fingerprint)
            return cached.Id;

        using var bitmap = TextureStackComposer.Compose(stack, ResolveAssetPath);
        var id = UploadTexture(bitmap, sRgb,
            stack.Layers.LastOrDefault(layer => layer.Enabled)?.Sampling ?? new TextureSamplingSettings());
        if (cached.Id != 0) GL.DeleteTexture(cached.Id);
        _stackTextures[key] = (fingerprint, id);
        return id;
    }

    private string FingerprintPath(string path)
    {
        var resolved = ResolveAssetPath(path);
        return File.Exists(resolved) ? $"|{resolved}:{File.GetLastWriteTimeUtc(resolved).Ticks}" : $"|{resolved}";
    }

    private string ResolveAssetPath(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        var projectDirectory = Path.GetDirectoryName(_project.ProjectFilePath);
        return string.IsNullOrWhiteSpace(projectDirectory) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(projectDirectory, path));
    }

    private int GetTexture(string path, bool sRgb)
    {
        var key = $"{path}|{sRgb}";
        if (_textures.TryGetValue(key, out var id)) return id;
        using var source = new Bitmap(path);
        source.RotateFlip(RotateFlipType.RotateNoneFlipY);
        id = UploadTexture(source, sRgb, new TextureSamplingSettings());
        _textures[key] = id;
        return id;
    }

    private static int UploadTexture(Bitmap source, bool sRgb, TextureSamplingSettings sampling)
    {
        var rect = new Rectangle(0, 0, source.Width, source.Height);
        var data = source.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var id = GL.GenTexture();
        try
        {
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                sRgb ? PixelInternalFormat.Srgb8Alpha8 : PixelInternalFormat.Rgba8,
                source.Width, source.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                PixelType.UnsignedByte, data.Scan0);
        }
        finally { source.UnlockBits(data); }
        var minFilter = sampling.Filter switch
        {
            TextureFilter.Nearest => TextureMinFilter.Nearest,
            TextureFilter.Linear => TextureMinFilter.Linear,
            _ => TextureMinFilter.LinearMipmapLinear
        };
        var magFilter = sampling.Filter == TextureFilter.Nearest ? TextureMagFilter.Nearest : TextureMagFilter.Linear;
        var wrap = sampling.Wrap switch
        {
            TextureWrap.ClampToEdge => TextureWrapMode.ClampToEdge,
            TextureWrap.MirroredRepeat => TextureWrapMode.MirroredRepeat,
            _ => TextureWrapMode.Repeat
        };
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrap);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrap);
        if (sampling.Filter == TextureFilter.LinearMipmapLinear)
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        return id;
    }

    private void SetMatrix(string name, GLMatrix4 value) => GL.UniformMatrix4(GL.GetUniformLocation(_program, name), false, ref value);
    private void SetLineMatrix(string name, GLMatrix4 value) => GL.UniformMatrix4(GL.GetUniformLocation(_lineProgram, name), false, ref value);
    private void SetSelectionMaskMatrix(string name, GLMatrix4 value) => GL.UniformMatrix4(GL.GetUniformLocation(_selectionMaskProgram, name), false, ref value);
    private void SetLineColor(NumericsVector4 value) => GL.Uniform4(GL.GetUniformLocation(_lineProgram, "uColor"), value.X, value.Y, value.Z, value.W);
    private void SetFloat(string name, float value) => GL.Uniform1(GL.GetUniformLocation(_program, name), value);
    private void SetInt(string name, int value) => GL.Uniform1(GL.GetUniformLocation(_program, name), value);
    private void SetVector3(string name, NumericsVector3 value) => GL.Uniform3(GL.GetUniformLocation(_program, name), value.X, value.Y, value.Z);
    private void SetVector4(string name, NumericsVector4 value) => GL.Uniform4(GL.GetUniformLocation(_program, name), value.X, value.Y, value.Z, value.W);
    private static GLVector3 ToGl(NumericsVector3 value) => new(value.X, value.Y, value.Z);

    private static NumericsVector3 ToVector3(Color color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f);

    private static NumericsVector4 ToVector4(Color color, float alphaMultiplier = 1f) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f * alphaMultiplier);

    private static GLMatrix4 ToGl(Matrix4x4 value) => new(
        value.M11, value.M12, value.M13, value.M14,
        value.M21, value.M22, value.M23, value.M24,
        value.M31, value.M32, value.M33, value.M34,
        value.M41, value.M42, value.M43, value.M44);
    private static float DegreesToRadians(float value) => value * MathF.PI / 180f;

    private static int CreateProgram(string vertexSource, string fragmentSource)
    {
        static int Compile(ShaderType type, string source)
        {
            var shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out var ok);
            if (ok == 0) throw new InvalidOperationException(GL.GetShaderInfoLog(shader));
            return shader;
        }
        var vertex = Compile(ShaderType.VertexShader, vertexSource);
        var fragment = Compile(ShaderType.FragmentShader, fragmentSource);
        var program = GL.CreateProgram();
        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var ok);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
        if (ok == 0) throw new InvalidOperationException(GL.GetProgramInfoLog(program));
        return program;
    }

    public void Dispose()
    {
        _renderTimer.Stop();
        if (_loaded && !_control.IsDisposed)
        {
            _control.MakeCurrent();
            foreach (var mesh in _gpuMeshes) mesh.Dispose();
            foreach (var texture in _textures.Values) GL.DeleteTexture(texture);
            foreach (var texture in _stackTextures.Values) GL.DeleteTexture(texture.Id);
            if (_program != 0) GL.DeleteProgram(_program);
            if (_lineProgram != 0) GL.DeleteProgram(_lineProgram);
            if (_environmentProgram != 0) GL.DeleteProgram(_environmentProgram);
            if (_skyboxProgram != 0) GL.DeleteProgram(_skyboxProgram);
            if (_selectionMaskProgram != 0) GL.DeleteProgram(_selectionMaskProgram);
            if (_selectionOutlineProgram != 0) GL.DeleteProgram(_selectionOutlineProgram);
            if (_inputOverlayProgram != 0) GL.DeleteProgram(_inputOverlayProgram);
            if (_glassBackDepthProgram != 0) GL.DeleteProgram(_glassBackDepthProgram);
            if (_environmentVertexArray != 0) GL.DeleteVertexArray(_environmentVertexArray);
            if (_environmentTexture != 0) GL.DeleteTexture(_environmentTexture);
            if (_skyboxTexture != 0) GL.DeleteTexture(_skyboxTexture);
            if (_sceneColorTexture != 0) GL.DeleteTexture(_sceneColorTexture);
            if (_glassBackDepthTexture != 0) GL.DeleteTexture(_glassBackDepthTexture);
            if (_glassBackDepthBuffer != 0) GL.DeleteRenderbuffer(_glassBackDepthBuffer);
            if (_glassBackDepthFramebuffer != 0) GL.DeleteFramebuffer(_glassBackDepthFramebuffer);
            if (_speedPreviewColorTexture != 0) GL.DeleteTexture(_speedPreviewColorTexture);
            if (_speedPreviewDepthBuffer != 0) GL.DeleteRenderbuffer(_speedPreviewDepthBuffer);
            if (_speedPreviewFramebuffer != 0) GL.DeleteFramebuffer(_speedPreviewFramebuffer);
            if (_captureColorTexture != 0) GL.DeleteTexture(_captureColorTexture);
            if (_captureDepthBuffer != 0) GL.DeleteRenderbuffer(_captureDepthBuffer);
            if (_captureFramebuffer != 0) GL.DeleteFramebuffer(_captureFramebuffer);
            if (_selectionMaskTexture != 0) GL.DeleteTexture(_selectionMaskTexture);
            if (_selectionMaskDepthBuffer != 0) GL.DeleteRenderbuffer(_selectionMaskDepthBuffer);
            if (_selectionMaskFramebuffer != 0) GL.DeleteFramebuffer(_selectionMaskFramebuffer);
            if (_lightGizmoVertexBuffer != 0) GL.DeleteBuffer(_lightGizmoVertexBuffer);
            if (_lightGizmoVertexArray != 0) GL.DeleteVertexArray(_lightGizmoVertexArray);
            if (_inputOverlayTexture != 0) GL.DeleteTexture(_inputOverlayTexture);
            if (_inputOverlayVertexBuffer != 0) GL.DeleteBuffer(_inputOverlayVertexBuffer);
            if (_inputOverlayVertexArray != 0) GL.DeleteVertexArray(_inputOverlayVertexArray);
            _grid?.Dispose();
        }
        _control.Dispose();
        _renderTimer.Dispose();
        _wheelInteractionTimer.Dispose();
    }

    private sealed class GpuMesh : IDisposable
    {
        public int VertexArray { get; init; }
        public int VertexBuffer { get; init; }
        public int IndexBuffer { get; init; }
        public int IndexCount { get; init; }

        public static GpuMesh Create(MeshData mesh)
        {
            var vertices = new float[mesh.Positions.Length * 12];
            for (var i = 0; i < mesh.Positions.Length; i++)
            {
                var p = mesh.Positions[i];
                var n = i < mesh.Normals.Length ? mesh.Normals[i] : Vector3.UnitY;
                var uv = i < mesh.TextureCoordinates.Length ? mesh.TextureCoordinates[i] : Vector2.Zero;
                var t = i < mesh.Tangents.Length ? mesh.Tangents[i] : new Vector4(1, 0, 0, 1);
                var o = i * 12;
                vertices[o] = p.X; vertices[o + 1] = p.Y; vertices[o + 2] = p.Z;
                vertices[o + 3] = n.X; vertices[o + 4] = n.Y; vertices[o + 5] = n.Z;
                vertices[o + 6] = uv.X; vertices[o + 7] = uv.Y;
                vertices[o + 8] = t.X; vertices[o + 9] = t.Y; vertices[o + 10] = t.Z; vertices[o + 11] = t.W;
            }
            var vao = GL.GenVertexArray();
            var vbo = GL.GenBuffer();
            var ebo = GL.GenBuffer();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.Indices.Length * sizeof(uint), mesh.Indices, BufferUsageHint.StaticDraw);
            const int stride = 12 * sizeof(float);
            GL.EnableVertexAttribArray(0); GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(1); GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(2); GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(3); GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, stride, 8 * sizeof(float));
            GL.BindVertexArray(0);
            return new GpuMesh { VertexArray = vao, VertexBuffer = vbo, IndexBuffer = ebo, IndexCount = mesh.Indices.Length };
        }

        public void Dispose()
        {
            GL.DeleteBuffer(VertexBuffer);
            GL.DeleteBuffer(IndexBuffer);
            GL.DeleteVertexArray(VertexArray);
        }
    }

    private sealed record RenderInstance(
        SceneModel Model,
        int MeshIndex,
        MeshData Mesh,
        GpuMesh Geometry,
        Matrix4x4 NodeTransform,
        NumericsVector3 LocalCenter);

    private static readonly PbrMaterial DefaultMaterial = new();

    private enum SelectionOperation
    {
        Add,
        Subtract
    }

    private sealed class GridGeometry : IDisposable
    {
        public int VertexArray { get; init; }
        public int VertexBuffer { get; init; }
        public int MinorFirst { get; init; }
        public int MinorCount { get; init; }
        public int MajorFirst { get; init; }
        public int MajorCount { get; init; }
        public int XAxisFirst { get; init; }
        public int ZAxisFirst { get; init; }

        public static GridGeometry Create()
        {
            const int extent = 50;
            var minor = new List<float>();
            var major = new List<float>();
            for (var coordinate = -extent; coordinate <= extent; coordinate++)
            {
                if (coordinate == 0) continue;
                var target = coordinate % 5 == 0 ? major : minor;
                AddLine(target, -extent, 0f, coordinate, extent, 0f, coordinate);
                AddLine(target, coordinate, 0f, -extent, coordinate, 0f, extent);
            }

            var vertices = new List<float>(minor.Count + major.Count + 12);
            vertices.AddRange(minor);
            var minorCount = minor.Count / 3;
            vertices.AddRange(major);
            var majorFirst = minorCount;
            var majorCount = major.Count / 3;
            var xAxisFirst = vertices.Count / 3;
            AddLine(vertices, -extent, 0f, 0f, extent, 0f, 0f);
            var zAxisFirst = vertices.Count / 3;
            AddLine(vertices, 0f, 0f, -extent, 0f, 0f, extent);

            var vao = GL.GenVertexArray();
            var vbo = GL.GenBuffer();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.BindVertexArray(0);

            return new GridGeometry
            {
                VertexArray = vao,
                VertexBuffer = vbo,
                MinorFirst = 0,
                MinorCount = minorCount,
                MajorFirst = majorFirst,
                MajorCount = majorCount,
                XAxisFirst = xAxisFirst,
                ZAxisFirst = zAxisFirst
            };
        }

        private static void AddLine(List<float> vertices, float ax, float ay, float az, float bx, float by, float bz) =>
            vertices.AddRange([ax, ay, az, bx, by, bz]);

        public void Dispose()
        {
            GL.DeleteBuffer(VertexBuffer);
            GL.DeleteVertexArray(VertexArray);
        }
    }

    private const string LineVertexShader = """
        #version 330 core
        layout(location=0) in vec3 aPosition;
        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        void main() { gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0); }
        """;

    private const string LineFragmentShader = """
        #version 330 core
        uniform vec4 uColor;
        out vec4 FragColor;
        void main() { FragColor = uColor; }
        """;

    private const string InputOverlayVertexShader = """
        #version 330 core
        layout(location=0) in vec2 aPosition;
        layout(location=1) in vec2 aUv;
        out vec2 vUv;
        void main() {
            vUv = aUv;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
        """;

    private const string InputOverlayFragmentShader = """
        #version 330 core
        in vec2 vUv;
        out vec4 FragColor;
        uniform sampler2D uTexture;
        uniform float uOpacity;
        void main() {
            vec4 color = texture(uTexture, vUv);
            FragColor = vec4(color.rgb, color.a * uOpacity);
        }
        """;

    private const string VertexShader = """
        #version 330 core
        layout(location=0) in vec3 aPosition;
        layout(location=1) in vec3 aNormal;
        layout(location=2) in vec2 aUv;
        layout(location=3) in vec4 aTangent;
        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        out vec3 vWorldPosition;
        out vec3 vObjectPosition;
        out vec3 vNormal;
        out vec2 vUv;
        out vec3 vTangent;
        void main() {
            vec4 world = uModel * vec4(aPosition, 1.0);
            mat3 normalMatrix = transpose(inverse(mat3(uModel)));
            vWorldPosition = world.xyz;
            vObjectPosition = aPosition;
            vNormal = normalize(normalMatrix * aNormal);
            vTangent = normalize(normalMatrix * aTangent.xyz);
            vUv = aUv;
            gl_Position = uProjection * uView * world;
        }
        """;

    private const string GlassBackDepthVertexShader = """
        #version 330 core
        layout(location=0) in vec3 aPosition;
        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        out float vViewDepth;
        void main() {
            vec4 viewPosition=uView*uModel*vec4(aPosition,1.0);
            vViewDepth=-viewPosition.z;
            gl_Position=uProjection*viewPosition;
        }
        """;

    private const string GlassBackDepthFragmentShader = """
        #version 330 core
        in float vViewDepth;
        out float BackDepth;
        void main() { BackDepth=max(vViewDepth,0.0); }
        """;

    private const string FragmentShader = """
        #version 330 core
        in vec3 vWorldPosition; in vec3 vObjectPosition; in vec3 vNormal; in vec2 vUv; in vec3 vTangent;
        out vec4 FragColor;
        uniform vec4 uBaseColor; uniform float uMetallic; uniform float uRoughness;
        uniform float uNormalScale; uniform float uBumpScale; uniform float uAo; uniform vec3 uEmissive; uniform float uOpacity;
        uniform int uMaterialMode; uniform int uFastPreview; uniform int uPreviewMode; uniform float uAlphaCutoff;
        uniform float uTransmission, uIor, uThickness, uRefractionStrength, uDispersion;
        uniform vec3 uAbsorptionColor; uniform vec2 uViewportSize; uniform mat4 uView;
        const int MAX_LIGHTS=16;
        uniform vec3 uCameraPosition; uniform int uLightCount;
        uniform int uLightTypes[MAX_LIGHTS];
        uniform vec3 uLightPositions[MAX_LIGHTS], uLightDirections[MAX_LIGHTS], uLightColors[MAX_LIGHTS];
        uniform float uLightRanges[MAX_LIGHTS], uLightInnerCos[MAX_LIGHTS], uLightOuterCos[MAX_LIGHTS];
        uniform sampler2D uBaseColorMap, uMetallicMap, uRoughnessMap, uNormalMap, uAoMap, uEmissiveMap, uOpacityMap, uBumpMap;
        uniform sampler2D uSceneColorMap, uGlassBackDepthMap;
        uniform bool uHasGlassThicknessMap;
        uniform sampler2D uEnvironmentMap; uniform samplerCube uSkyboxEnvironmentMap;
        uniform bool uHasEnvironment, uUseSkyboxEnvironment;
        uniform float uEnvironmentIntensity, uEnvironmentRotation, uEnvironmentMaxLod;
        uniform bool uHasBaseColorMap, uHasMetallicMap, uHasRoughnessMap, uHasNormalMap, uHasAoMap, uHasEmissiveMap, uHasOpacityMap, uHasBumpMap;
        uniform int uBaseColorMappingMode,uBaseColorMappingAxis,uBaseColorMappingSpace;
        uniform int uMetallicMappingMode,uMetallicMappingAxis,uMetallicMappingSpace;
        uniform int uRoughnessMappingMode,uRoughnessMappingAxis,uRoughnessMappingSpace;
        uniform int uNormalMappingMode,uNormalMappingAxis,uNormalMappingSpace;
        uniform int uAoMappingMode,uAoMappingAxis,uAoMappingSpace;
        uniform int uEmissiveMappingMode,uEmissiveMappingAxis,uEmissiveMappingSpace;
        uniform int uOpacityMappingMode,uOpacityMappingAxis,uOpacityMappingSpace;
        uniform int uBumpMappingMode,uBumpMappingAxis,uBumpMappingSpace;
        uniform float uBaseColorMappingBlend,uMetallicMappingBlend,uRoughnessMappingBlend,uNormalMappingBlend;
        uniform float uAoMappingBlend,uEmissiveMappingBlend,uOpacityMappingBlend,uBumpMappingBlend;
        const float PI = 3.14159265359;
        vec3 acesToneMap(vec3 color) {
            const float a=2.51, b=0.03, c=2.43, d=0.59, e=0.14;
            color=max(color,vec3(0.0));
            return clamp(color*(a*color+b)/(color*(c*color+d)+e),0.0,1.0);
        }
        vec3 linearToSrgb(vec3 color) {
            color=max(color,vec3(0.0));
            vec3 low=color*12.92;
            vec3 high=1.055*pow(color,vec3(1.0/2.4))-0.055;
            return mix(high,low,lessThanEqual(color,vec3(0.0031308)));
        }
        vec3 srgbToLinear(vec3 color) {
            color=max(color,vec3(0.0));
            vec3 low=color/12.92;
            vec3 high=pow((color+0.055)/1.055,vec3(2.4));
            return mix(high,low,lessThanEqual(color,vec3(0.04045)));
        }
        float inverseAcesChannel(float value) {
            const float a=2.51,b=0.03,c=2.43,d=0.59,e=0.14;
            value=clamp(value,0.0,0.9999);
            float qa=value*c-a,qb=value*d-b,qc=value*e;
            if(abs(qa)<0.000001)return max(0.0,-qc/((qb>=0.0?1.0:-1.0)*max(abs(qb),0.000001)));
            float root=sqrt(max(0.0,qb*qb-4.0*qa*qc));
            return max(0.0,max((-qb+root)/(2.0*qa),(-qb-root)/(2.0*qa)));
        }
        vec3 inverseAcesToneMap(vec3 color) { return vec3(inverseAcesChannel(color.r),inverseAcesChannel(color.g),inverseAcesChannel(color.b)); }
        vec3 inverseStudioToneMap(vec3 color) {
            return -log(max(vec3(0.0001),vec3(1.0)-clamp(color,vec3(0.0),vec3(0.9999))))/0.72;
        }
        float distributionGGX(vec3 n, vec3 h, float roughness) {
            float a=roughness*roughness, a2=a*a, ndh=max(dot(n,h),0.0), d=ndh*ndh*(a2-1.0)+1.0;
            return a2/max(PI*d*d,0.000001);
        }
        float geometrySchlick(float ndv, float roughness) { float r=roughness+1.0, k=r*r/8.0; return ndv/(ndv*(1.0-k)+k); }
        vec3 fresnel(float cosTheta, vec3 f0) { return f0+(1.0-f0)*pow(clamp(1.0-cosTheta,0.0,1.0),5.0); }
        vec2 environmentUv(vec3 direction) {
            float c=cos(uEnvironmentRotation), s=sin(uEnvironmentRotation);
            direction.xz=mat2(c,-s,s,c)*direction.xz;
            return vec2(atan(direction.z,direction.x)/(2.0*PI)+0.5, 0.5-asin(clamp(direction.y,-1.0,1.0))/PI);
        }
        vec3 sampleEnvironment(vec3 direction, float lod) {
            float c=cos(uEnvironmentRotation), s=sin(uEnvironmentRotation);
            vec3 rotatedDirection=direction;
            rotatedDirection.xz=mat2(c,-s,s,c)*rotatedDirection.xz;
            return uUseSkyboxEnvironment
                ? textureLod(uSkyboxEnvironmentMap,rotatedDirection,lod).rgb
                : textureLod(uEnvironmentMap,environmentUv(direction),lod).rgb;
        }
        vec2 projectedUv(vec3 p, vec3 n, int axis) {
            if(axis==1) return p.zy;
            if(axis==2) return p.xz;
            if(axis==3) return p.xy;
            vec3 a=abs(n);
            return a.x>a.y&&a.x>a.z?p.zy:(a.y>a.z?p.xz:p.xy);
        }
        vec4 mappedSample(sampler2D map, int mode, int axis, int space, float blend, vec2 offset) {
            if(mode==0) return texture(map,vUv+offset);
            vec3 p=space==0?vObjectPosition:vWorldPosition;
            if(mode==1||mode==2) return texture(map,projectedUv(p,vNormal,axis)+offset);
            vec3 weights=pow(abs(normalize(vNormal)),vec3(max(blend,0.1)));
            weights/=max(weights.x+weights.y+weights.z,0.0001);
            return texture(map,p.zy+offset)*weights.x+texture(map,p.xz+offset)*weights.y+texture(map,p.xy+offset)*weights.z;
        }
        void main() {
            bool fastPreview=uFastPreview!=0;
            // Keep the authored base color but favor a lower-resolution mip in
            // quick mode to reduce texture bandwidth while navigating.
            vec4 base=uBaseColor*(uHasBaseColorMap?mappedSample(uBaseColorMap,uBaseColorMappingMode,uBaseColorMappingAxis,uBaseColorMappingSpace,uBaseColorMappingBlend,vec2(0.0)):vec4(1.0));
            float alpha=base.a*uOpacity*(fastPreview?1.0:(uHasOpacityMap?mappedSample(uOpacityMap,uOpacityMappingMode,uOpacityMappingAxis,uOpacityMappingSpace,uOpacityMappingBlend,vec2(0.0)).r:1.0));
            if(uMaterialMode==2 && alpha<uAlphaCutoff) discard;
            float metallic=clamp(uMetallic*(fastPreview?1.0:(uHasMetallicMap?mappedSample(uMetallicMap,uMetallicMappingMode,uMetallicMappingAxis,uMetallicMappingSpace,uMetallicMappingBlend,vec2(0.0)).r:1.0)),0.0,1.0);
            float roughness=clamp(uRoughness*(fastPreview?1.0:(uHasRoughnessMap?mappedSample(uRoughnessMap,uRoughnessMappingMode,uRoughnessMappingAxis,uRoughnessMappingSpace,uRoughnessMappingBlend,vec2(0.0)).r:1.0)),0.001,1.0);
            float ao=clamp(uAo*(fastPreview?1.0:(uHasAoMap?mappedSample(uAoMap,uAoMappingMode,uAoMappingAxis,uAoMappingSpace,uAoMappingBlend,vec2(0.0)).r:1.0)),0.0,1.0);
            vec3 n=normalize(vNormal);
            if(!fastPreview&&uHasNormalMap) { vec3 t=normalize(vTangent-n*dot(vTangent,n)); vec3 b=cross(n,t); vec3 map=mappedSample(uNormalMap,uNormalMappingMode,uNormalMappingAxis,uNormalMappingSpace,uNormalMappingBlend,vec2(0.0)).xyz*2.0-1.0; map.xy*=uNormalScale; n=normalize(mat3(t,b,n)*map); }
            if(!fastPreview&&uHasBumpMap) { vec2 texel=1.0/vec2(textureSize(uBumpMap,0)); float h=mappedSample(uBumpMap,uBumpMappingMode,uBumpMappingAxis,uBumpMappingSpace,uBumpMappingBlend,vec2(0.0)).r; float hx=mappedSample(uBumpMap,uBumpMappingMode,uBumpMappingAxis,uBumpMappingSpace,uBumpMappingBlend,vec2(texel.x,0.0)).r-h; float hy=mappedSample(uBumpMap,uBumpMappingMode,uBumpMappingAxis,uBumpMappingSpace,uBumpMappingBlend,vec2(0.0,texel.y)).r-h; vec3 t=normalize(vTangent-n*dot(vTangent,n)); vec3 b=normalize(cross(n,t)); n=normalize(n-t*hx*uBumpScale-b*hy*uBumpScale); }
            bool advancedJewelryPreview=uPreviewMode==5;
            if(advancedJewelryPreview&&!gl_FrontFacing)n=-n;
            vec3 v=normalize(uCameraPosition-vWorldPosition);
            bool isGlass=uMaterialMode==4;
            float dielectricF0=pow((max(uIor,1.0001)-1.0)/(max(uIor,1.0001)+1.0),2.0);
            vec3 f0=isGlass?vec3(dielectricF0):mix(vec3(0.04),base.rgb,metallic);
            vec3 direct=vec3(0.0);
            for(int i=0;i<uLightCount;i++) {
                vec3 toLight=uLightPositions[i]-vWorldPosition;
                float lightDistance=length(toLight);
                vec3 l=uLightTypes[i]==0?normalize(-uLightDirections[i]):toLight/max(lightDistance,0.0001);
                float attenuation=1.0;
                if(uLightTypes[i]!=0) {
                    float lightRange=max(uLightRanges[i],0.0001);
                    float normalizedDistance=lightDistance/lightRange;
                    // Keep most of the requested intensity through the useful part
                    // of the range, then fade smoothly near its boundary. Distance
                    // falloff is normalized by Range because scene units are model
                    // units rather than physical metres/candela.
                    float rangeCutoff=1.0-smoothstep(0.85,1.0,normalizedDistance);
                    float distanceFalloff=1.0/(1.0+4.0*normalizedDistance*normalizedDistance);
                    attenuation=rangeCutoff*distanceFalloff;
                }
                if(uLightTypes[i]==2) {
                    float coneCos=dot(normalize(-l),normalize(uLightDirections[i]));
                    attenuation*=smoothstep(uLightOuterCos[i],uLightInnerCos[i],coneCos);
                }
                vec3 h=normalize(v+l);
                vec3 f=fresnel(max(dot(h,v),0.0),f0);
                if(fastPreview) {
                    float specularPower=mix(96.0,4.0,roughness);
                    vec3 specular=f0*pow(max(dot(n,h),0.0),specularPower);
                    vec3 diffuse=isGlass?vec3(0.0):base.rgb*(1.0-metallic)/PI;
                    direct+=(diffuse+specular)*uLightColors[i]*max(dot(n,l),0.0)*attenuation;
                } else {
                    float ndf=distributionGGX(n,h,roughness);
                    float g=geometrySchlick(max(dot(n,v),0.0),roughness)*geometrySchlick(max(dot(n,l),0.0),roughness);
                    vec3 specular=ndf*g*f/max(4.0*max(dot(n,v),0.0)*max(dot(n,l),0.0),0.001);
                    vec3 kd=isGlass?vec3(0.0):(vec3(1.0)-f)*(1.0-metallic);
                    direct+=(kd*base.rgb/PI+specular)*uLightColors[i]*max(dot(n,l),0.0)*attenuation;
                }
            }
            bool jewelryPreview=uPreviewMode==4;
            bool studioPreview=uPreviewMode==3||jewelryPreview||advancedJewelryPreview;
            vec3 ambient=base.rgb*(studioPreview?0.18:0.05)*ao;
            if(uHasEnvironment) {
                vec3 f=fresnel(max(dot(n,v),0.0),f0);
                vec3 kd=isGlass?vec3(0.0):(vec3(1.0)-f)*(1.0-metallic);
                // Fast preview deliberately uses a single, blurred HDRI lookup rather
                // than the full IBL path.  It must nevertheless retain environment
                // illumination when no analytic lights are enabled.
                float diffuseLod=fastPreview?min(6.0,uEnvironmentMaxLod):min(4.0,uEnvironmentMaxLod);
                vec3 diffuseEnvironment=sampleEnvironment(n,diffuseLod);
                vec3 reflected=reflect(-v,n);
                float reflectionLod=fastPreview?max(roughness,0.35)*uEnvironmentMaxLod:roughness*uEnvironmentMaxLod;
                vec3 specularEnvironment=sampleEnvironment(reflected,reflectionLod);
                float previewScale=fastPreview?0.85:1.0;
                float studioEnvironmentScale=studioPreview?1.15:1.0;
                if(advancedJewelryPreview&&metallic>0.5) {
                    // Polished jewelry needs broad reflection cards to remain readable;
                    // retain a soft studio fill between the bright HDR highlights.
                    specularEnvironment=max(specularEnvironment,diffuseEnvironment*0.22);
                    studioEnvironmentScale=1.42;
                }
                ambient=(kd*base.rgb*diffuseEnvironment/PI+f*specularEnvironment*(1.0-roughness*0.35))*ao*uEnvironmentIntensity*previewScale*studioEnvironmentScale;
                if(advancedJewelryPreview&&metallic>0.5)
                    ambient+=base.rgb*diffuseEnvironment*0.16*ao*uEnvironmentIntensity;
            }
            vec3 emissive=uEmissive*((!fastPreview&&uHasEmissiveMap)?mappedSample(uEmissiveMap,uEmissiveMappingMode,uEmissiveMappingAxis,uEmissiveMappingSpace,uEmissiveMappingBlend,vec2(0.0)).rgb:vec3(1.0));
            vec3 color=direct+ambient+emissive;
            if(advancedJewelryPreview) {
                // A slightly lifted filmic curve keeps polished metal readable while
                // preserving the small, bright reflections that define gem facets.
                color=acesToneMap(color*1.18+vec3(0.012));
            } else if(studioPreview) {
                // Preview 3 uses a neutral studio fill and a gentler highlight rolloff.
                // The authored lights remain present, but extreme values no longer wash
                // out photographs and pale materials as aggressively.
                color=vec3(1.0)-exp(-color*0.72);
            }
            if(uMaterialMode==4) {
                float dielectricF0=pow((uIor-1.0)/(uIor+1.0),2.0);
                float glassFresnel=dielectricF0+(1.0-dielectricF0)*pow(1.0-max(dot(n,v),0.0),5.0);
                if(uFastPreview!=0) {
                    // A blend-only approximation avoids the framebuffer copy
                    // and refracted-scene lookup while the camera is moving.
                    color=mix(base.rgb*0.35,color+base.rgb*0.12,glassFresnel);
                    alpha=clamp(0.12+glassFresnel*0.45+(1.0-uTransmission)*0.22,0.05,0.75);
                } else {
                    vec3 viewNormal=normalize(mat3(uView)*n);
                    float eta=1.0/max(uIor,1.0001);
                    vec2 screenUv=gl_FragCoord.xy/max(uViewportSize,vec2(1.0));
                    float measuredThickness=0.0;
                    if(advancedJewelryPreview&&uHasGlassThicknessMap) {
                        float frontDepth=-(uView*vec4(vWorldPosition,1.0)).z;
                        float backDepth=texture(uGlassBackDepthMap,screenUv).r;
                        measuredThickness=max(backDepth-frontDepth,0.0);
                    }
                    float opticalThickness=max(uThickness,advancedJewelryPreview?0.018:0.002);
                    vec2 offset=viewNormal.xy*uRefractionStrength*opticalThickness*(0.5+eta);
                    if(advancedJewelryPreview) {
                        // Use IOR as the dominant screen-space bend. Authored glTF
                        // thickness is expressed in metres and is otherwise too small
                        // to describe a gem in a model with arbitrary scene units.
                        float relativeThickness=measuredThickness/max(length(uCameraPosition-vWorldPosition),0.001);
                        float gemBend=(1.0-eta)*(0.012+uRefractionStrength*0.08)*
                            (1.0+clamp(relativeThickness*10.0,0.0,1.4));
                        offset=viewNormal.xy*gemBend;
                    }
                    vec2 dispersionOffset=jewelryPreview?viewNormal.xy*uDispersion*0.055:vec2(0.0);
                    vec2 refractedUv=clamp(screenUv+offset,vec2(0.001),vec2(0.999));
                    vec3 sceneDisplay;
                    if(jewelryPreview&&uDispersion>0.0) {
                        vec2 uvR=clamp(refractedUv+dispersionOffset,vec2(0.001),vec2(0.999));
                        vec2 uvB=clamp(refractedUv-dispersionOffset,vec2(0.001),vec2(0.999));
                        sceneDisplay=srgbToLinear(vec3(texture(uSceneColorMap,uvR).r,texture(uSceneColorMap,refractedUv).g,texture(uSceneColorMap,uvB).b));
                    } else {
                        sceneDisplay=srgbToLinear(texture(uSceneColorMap,refractedUv).rgb);
                    }
                    vec3 behind=advancedJewelryPreview?inverseAcesToneMap(sceneDisplay):studioPreview?inverseStudioToneMap(sceneDisplay):inverseAcesToneMap(sceneDisplay);
                    float absorptionAmount=advancedJewelryPreview
                        ? clamp(0.24+max(uThickness,0.0)*18.0,0.0,0.72)
                        : 1.0-exp(-max(uThickness,0.0));
                    vec3 absorption;
                    float baseChroma=max(base.r,max(base.g,base.b))-min(base.r,min(base.g,base.b));
                    float attenuationChroma=max(uAbsorptionColor.r,max(uAbsorptionColor.g,uAbsorptionColor.b))-min(uAbsorptionColor.r,min(uAbsorptionColor.g,uAbsorptionColor.b));
                    if(advancedJewelryPreview&&baseChroma>0.04) {
                        // glTF frequently stores a gemstone's color only in
                        // baseColorFactor.  If volume attenuation is absent (white),
                        // use that authored color as the optical absorption tint.
                        vec3 opticalTint=attenuationChroma>0.025
                            ? clamp(uAbsorptionColor,vec3(0.025),vec3(1.0))
                            : clamp(base.rgb,vec3(0.025),vec3(1.0));
                        vec3 opticalDensity=-log(opticalTint);
                        float relativePath=measuredThickness/max(length(uCameraPosition-vWorldPosition),0.001);
                        float effectivePath=clamp(0.46+uThickness*24.0+relativePath*8.0,0.46,1.15);
                        absorption=exp(-opticalDensity*effectivePath);
                    } else {
                        absorption=mix(vec3(1.0),clamp(uAbsorptionColor,0.0,1.0),absorptionAmount);
                    }
                    vec3 transmitted=behind*absorption*uTransmission;
                    if((jewelryPreview||advancedJewelryPreview)&&uHasEnvironment) {
                        vec3 incident=-v;
                        float spread=uDispersion*(advancedJewelryPreview?0.035:0.12);
                        vec3 refractedR=refract(incident,n,1.0/max(1.0001,uIor-spread));
                        vec3 refractedG=refract(incident,n,1.0/max(1.0001,uIor));
                        vec3 refractedB=refract(incident,n,1.0/max(1.0001,uIor+spread));
                        float glassLod=min(roughness*uEnvironmentMaxLod,2.0);
                        vec3 refractedEnvironment=vec3(
                            sampleEnvironment(refractedR,glassLod).r,
                            sampleEnvironment(refractedG,glassLod).g,
                            sampleEnvironment(refractedB,glassLod).b);
                        if(advancedJewelryPreview) {
                            vec3 reflectedDirection=reflect(incident,n);
                            vec3 internalReflection=sampleEnvironment(reflectedDirection,glassLod);
                            float criticalGlow=smoothstep(0.18,0.72,1.0-abs(dot(n,v)));
                            vec3 gemEnvironment=mix(refractedEnvironment,internalReflection,clamp(glassFresnel+criticalGlow*0.34,0.0,0.86));
                            float facetFlash=pow(max(max(gemEnvironment.r,gemEnvironment.g),gemEnvironment.b),1.35);
                            gemEnvironment+=gemEnvironment*facetFlash*0.22;
                            // Stable per-facet spectral fire: flat gem normals keep the
                            // band inside facets instead of producing an RGB silhouette.
                            float phase=dot(n,normalize(vec3(0.73,0.41,0.55)))*2.7+dot(n,v)*1.9;
                            vec3 spectral=0.5+0.5*cos(6.2831853*(vec3(0.00,0.33,0.67)+phase));
                            float fireMask=clamp(uDispersion*20.0,0.0,1.0)*smoothstep(0.08,0.78,1.0-abs(dot(n,v)));
                            gemEnvironment+=spectral*fireMask*(0.12+facetFlash*0.34);
                            transmitted=mix(transmitted,gemEnvironment*absorption*uEnvironmentIntensity,baseChroma>0.04?0.66:0.76);
                        } else {
                            transmitted=mix(transmitted,refractedEnvironment*absorption*uEnvironmentIntensity,0.38);
                        }
                    }
                    color=mix(transmitted,color,clamp(glassFresnel+(1.0-uTransmission),0.0,1.0));
                    if(advancedJewelryPreview&&baseChroma>0.04) {
                        // ACES rolls saturated highlights toward white. Restore only a
                        // small amount of chroma for colored gems; Fresnel reflections
                        // remain neutral because this happens after reflection mixing.
                        float luminance=dot(color,vec3(0.2126,0.7152,0.0722));
                        color=max(vec3(0.0),mix(vec3(luminance),color,1.16));
                    }
                    alpha=1.0;
                }
            }
            color=linearToSrgb(studioPreview?color:acesToneMap(color));
            FragColor=vec4(color,alpha);
        }
        """;

    private const string EnvironmentVertexShader = """
        #version 330 core
        out vec2 vUv;
        void main() {
            vec2 position=vec2(float((gl_VertexID<<1)&2),float(gl_VertexID&2));
            vUv=position;
            gl_Position=vec4(position*2.0-1.0,0.0,1.0);
        }
        """;

    private const string SelectionMaskVertexShader = """
        #version 330 core
        layout(location=0) in vec3 aPosition;
        uniform mat4 uModel, uView, uProjection;
        void main() { gl_Position=uProjection*uView*uModel*vec4(aPosition,1.0); }
        """;

    private const string SelectionMaskFragmentShader = """
        #version 330 core
        out vec4 FragColor;
        void main() { FragColor=vec4(1.0); }
        """;

    private const string SelectionOutlineVertexShader = """
        #version 330 core
        out vec2 vUv;
        void main() {
            vec2 position=vec2(float((gl_VertexID<<1)&2),float(gl_VertexID&2));
            vUv=position;
            gl_Position=vec4(position*2.0-1.0,0.0,1.0);
        }
        """;

    // FragColor 外框框選線
    private const string SelectionOutlineFragmentShader = """
        #version 330 core
        in vec2 vUv; out vec4 FragColor;
        uniform sampler2D uMask;
        uniform vec2 uTexelSize;
        uniform vec4 uColor;
        void main() {
            float center=texture(uMask,vUv).r;
            float neighbor=0.0;
            neighbor=max(neighbor,texture(uMask,vUv+vec2(uTexelSize.x,0.0)).r);
            neighbor=max(neighbor,texture(uMask,vUv-vec2(uTexelSize.x,0.0)).r);
            neighbor=max(neighbor,texture(uMask,vUv+vec2(0.0,uTexelSize.y)).r);
            neighbor=max(neighbor,texture(uMask,vUv-vec2(0.0,uTexelSize.y)).r);
            float edge=neighbor*(1.0-center);
            if(edge<0.5) discard;
            FragColor=uColor;
        }
        """;

    private const string EnvironmentFragmentShader = """
        #version 330 core
        in vec2 vUv; out vec4 FragColor;
        uniform mat4 uView, uProjection;
        uniform sampler2D uEnvironmentMap;
        uniform float uIntensity, uRotation, uLod;
        const float PI=3.14159265359;
        vec3 acesToneMap(vec3 color) {
            const float a=2.51, b=0.03, c=2.43, d=0.59, e=0.14;
            color=max(color,vec3(0.0));
            return clamp(color*(a*color+b)/(color*(c*color+d)+e),0.0,1.0);
        }
        vec3 linearToSrgb(vec3 color) {
            color=max(color,vec3(0.0));
            vec3 low=color*12.92;
            vec3 high=1.055*pow(color,vec3(1.0/2.4))-0.055;
            return mix(high,low,lessThanEqual(color,vec3(0.0031308)));
        }
        void main() {
            vec2 ndc=vUv*2.0-1.0;
            vec4 eye=inverse(uProjection)*vec4(ndc,1.0,1.0);
            vec3 direction=transpose(mat3(uView))*normalize(eye.xyz/eye.w);
            float c=cos(uRotation), s=sin(uRotation);
            direction.xz=mat2(c,-s,s,c)*direction.xz;
            vec2 uv=vec2(atan(direction.z,direction.x)/(2.0*PI)+0.5,0.5-asin(clamp(direction.y,-1.0,1.0))/PI);
            vec3 color=textureLod(uEnvironmentMap,uv,uLod).rgb*uIntensity;
            color=linearToSrgb(acesToneMap(color));
            FragColor=vec4(color,1.0);
        }
        """;

    private const string SkyboxFragmentShader = """
        #version 330 core
        in vec2 vUv; out vec4 FragColor;
        uniform mat4 uView, uProjection;
        uniform samplerCube uSkybox;
        uniform float uRotation;
        vec3 linearToSrgb(vec3 color) {
            color=max(color,vec3(0.0));
            vec3 low=color*12.92;
            vec3 high=1.055*pow(color,vec3(1.0/2.4))-0.055;
            return mix(high,low,lessThanEqual(color,vec3(0.0031308)));
        }
        void main() {
            vec2 ndc=vUv*2.0-1.0;
            vec4 eye=inverse(uProjection)*vec4(ndc,1.0,1.0);
            vec3 direction=transpose(mat3(uView))*normalize(eye.xyz/eye.w);
            float c=cos(uRotation), s=sin(uRotation);
            direction.xz=mat2(c,-s,s,c)*direction.xz;
            FragColor=vec4(linearToSrgb(texture(uSkybox,direction).rgb),1.0);
        }
        """;
}
