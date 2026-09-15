using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Numerics;
using Rv3dViewer.Plugin.WinForms;

namespace Rv3dViewer.HighQualityRenderPlugin;

// Legacy preview retained for builds that define UseOldRender in HighQualityRenderForm.cs.
[ToolboxItem(true)]
internal sealed class RenderPreviewControl : Control
{
    private RenderSceneSnapshot? _scene;
    private Vector3 _target;
    private Vector3 _up = Vector3.UnitY;
    private float _distance = 5F;
    private float _yaw;
    private float _pitch;
    private float _fieldOfView = 60F;
    private Point _lastMouse;
    private MouseButtons _dragButton;
    private readonly System.Windows.Forms.Timer _gpuPreviewTimer;
    private CancellationTokenSource? _gpuPreviewCancellation;
    private Bitmap? _gpuPreview;
    private int _gpuPreviewGeneration;
    private readonly InputActivityOverlay _inputOverlay;

    public RenderPreviewControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        _gpuPreviewTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _gpuPreviewTimer.Tick += GpuPreviewTimer_Tick;
        _inputOverlay = new InputActivityOverlay(this);
    }

    public bool InputOverlayEnabled { get => _inputOverlay.Enabled; set => _inputOverlay.Enabled = value; }
    public void ShowInputActivity(string text) => _inputOverlay.Show(text);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderSceneSnapshot? Scene
    {
        get => _scene;
        set
        {
            _scene = value;
            if (value is not null) SetCamera(value.Camera);
            ScheduleGpuPreview();
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderCamera Camera
    {
        get
        {
            var offset = new Vector3(MathF.Sin(_yaw) * MathF.Cos(_pitch), MathF.Sin(_pitch),
                MathF.Cos(_yaw) * MathF.Cos(_pitch)) * _distance;
            return new RenderCamera(_target + offset, _target, _up, _fieldOfView);
        }
    }

    public event EventHandler? CameraChanged;

    public void ShowRenderedImage(string path)
    {
        if (!File.Exists(path)) return;
        using var image = Image.FromFile(path);
        var rendered = new Bitmap(image);
        _gpuPreviewTimer.Stop();
        _gpuPreviewCancellation?.Cancel();
        _gpuPreview?.Dispose();
        _gpuPreview = rendered;
        Invalidate();
    }

    public void SetCamera(RenderCamera camera)
    {
        _target = camera.To;
        _up = RenderSceneSnapshot.SafeNormalize(camera.Up, Vector3.UnitY);
        _fieldOfView = Math.Clamp(camera.FieldOfViewDegrees, 5F, 120F);
        var offset = camera.From - camera.To;
        _distance = Math.Max(offset.Length(), 0.01F);
        var direction = offset / _distance;
        _pitch = MathF.Asin(Math.Clamp(direction.Y, -1F, 1F));
        _yaw = MathF.Atan2(direction.X, direction.Z);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _inputOverlay.Show(InputActivityFormatter.Mouse("MouseDn", e.Button));
        if (e.Button is not (MouseButtons.Left or MouseButtons.Middle or MouseButtons.Right)) return;
        _lastMouse = e.Location;
        _dragButton = e.Button;
        Capture = true;
        Focus();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (InputActivityFormatter.ShouldShowMove(e.Button))
            _inputOverlay.Show(InputActivityFormatter.Mouse(e.Button == MouseButtons.None ? "Mouse Move" : "Mouse Drag", e.Button));
        if (_dragButton == MouseButtons.None) return;
        var dx = e.X - _lastMouse.X;
        var dy = e.Y - _lastMouse.Y;
        _lastMouse = e.Location;
        if (_dragButton == MouseButtons.Left)
        {
            _yaw += dx * 0.008F;
            _pitch = Math.Clamp(_pitch - dy * 0.008F, -1.553F, 1.553F);
        }
        else if (_dragButton == MouseButtons.Middle)
        {
            var camera = Camera;
            var forward = RenderSceneSnapshot.SafeNormalize(camera.To - camera.From, -Vector3.UnitZ);
            var right = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(forward, camera.Up), Vector3.UnitX);
            var up = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(right, forward), Vector3.UnitY);
            var delta = (-right * dx + up * dy) * (_distance * 0.0015F);
            SetCamera(new RenderCamera(camera.From, camera.To + delta, camera.Up, camera.FieldOfViewDegrees));
        }
        else
        {
            var camera = Camera;
            var forward = RenderSceneSnapshot.SafeNormalize(camera.To - camera.From, -Vector3.UnitZ);
            var right = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(forward, camera.Up), Vector3.UnitX);
            var up = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(right, forward), Vector3.UnitY);
            _target += (-right * dx + up * dy) * (_distance * 0.0015F);
        }
        CameraChanged?.Invoke(this, EventArgs.Empty);
        ScheduleGpuPreview();
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _inputOverlay.Show(InputActivityFormatter.Mouse("MouseUp", e.Button));
        if (e.Button != _dragButton) return;
        _dragButton = MouseButtons.None;
        Capture = false;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        _inputOverlay.Show(InputActivityFormatter.Mouse(e.Delta >= 0 ? "Mouse Wheel Up" : "Mouse Wheel Down", MouseButtons.None));
        _fieldOfView = Math.Clamp(_fieldOfView - e.Delta / 120F * 2F, 5F, 120F);
        CameraChanged?.Invoke(this, EventArgs.Empty);
        ScheduleGpuPreview();
        Invalidate();
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); ScheduleGpuPreview(); }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); ScheduleGpuPreview(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (_gpuPreview is not null)
        {
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(_gpuPreview, ClientRectangle);
            DrawNavigationHelp(e.Graphics);
            _inputOverlay.Draw(e.Graphics, ClientRectangle);
            return;
        }
        if (_scene is null || _scene.Triangles.Length == 0 || ClientSize.Width <= 1 || ClientSize.Height <= 1)
        {
            TextRenderer.DrawText(e.Graphics, "沒有可預覽的場景", Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            _inputOverlay.Draw(e.Graphics, ClientRectangle);
            return;
        }

        var camera = Camera;
        var forward = RenderSceneSnapshot.SafeNormalize(camera.To - camera.From, -Vector3.UnitZ);
        var right = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(forward, camera.Up), Vector3.UnitX);
        var up = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(right, forward), Vector3.UnitY);
        var focal = Height * 0.5F / MathF.Tan(camera.FieldOfViewDegrees * MathF.PI / 360F);
        var center = new Vector2(Width * 0.5F, Height * 0.5F);
        var triangles = new List<PreviewTriangle>();
        var step = Math.Max(1, _scene.Triangles.Length / 60000);
        for (var index = 0; index < _scene.Triangles.Length; index += step)
        {
            var triangle = _scene.Triangles[index];
            if (!TryProject(triangle.P0, camera.From, right, up, forward, focal, center, out var p0, out var z0) ||
                !TryProject(triangle.P1, camera.From, right, up, forward, focal, center, out var p1, out var z1) ||
                !TryProject(triangle.P2, camera.From, right, up, forward, focal, center, out var p2, out var z2)) continue;
            var normal = RenderSceneSnapshot.SafeNormalize(
                Vector3.Cross(triangle.P1 - triangle.P0, triangle.P2 - triangle.P0), Vector3.UnitY);
            var light = 0.22F + 0.78F * MathF.Abs(Vector3.Dot(normal,
                RenderSceneSnapshot.SafeNormalize(new Vector3(-0.4F, 0.75F, 0.55F), Vector3.UnitY)));
            var material = _scene.Materials[triangle.MaterialIndex];
            var baseColor = Vector3.Clamp(new Vector3(material.BaseColor.X, material.BaseColor.Y,
                material.BaseColor.Z) * light, Vector3.Zero, Vector3.One);
            var color = Color.FromArgb(ComputePreviewAlpha(material), ToByte(baseColor.X),
                ToByte(baseColor.Y), ToByte(baseColor.Z));
            triangles.Add(new PreviewTriangle((z0 + z1 + z2) / 3F, color, [p0, p1, p2]));
        }
        foreach (var triangle in triangles.OrderByDescending(item => item.Depth))
        {
            using var brush = new SolidBrush(triangle.Color);
            e.Graphics.FillPolygon(brush, triangle.Points);
        }
        DrawNavigationHelp(e.Graphics);
        _inputOverlay.Draw(e.Graphics, ClientRectangle);
    }

    private void DrawNavigationHelp(Graphics graphics) =>
        TextRenderer.DrawText(graphics, "左鍵旋轉　右鍵平移　滾輪縮放", Font,
            new Rectangle(10, Height - Font.Height - 12, Width - 20, Font.Height + 4), ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

    private void ScheduleGpuPreview()
    {
        if (!IsHandleCreated || IsDisposed || _scene is null || ClientSize.Width < 64 || ClientSize.Height < 64) return;
        _gpuPreviewTimer.Stop();
        _gpuPreviewCancellation?.Cancel();
        _gpuPreview?.Dispose();
        _gpuPreview = null;
        _gpuPreviewTimer.Start();
    }

    private async void GpuPreviewTimer_Tick(object? sender, EventArgs e)
    {
        _gpuPreviewTimer.Stop();
        var scene = _scene;
        if (scene is null || IsDisposed) return;
        _gpuPreviewCancellation?.Dispose();
        _gpuPreviewCancellation = new CancellationTokenSource();
        var token = _gpuPreviewCancellation.Token;
        var generation = ++_gpuPreviewGeneration;
        var path = Path.Combine(Path.GetTempPath(), $"rv3d-gpu-preview-{Guid.NewGuid():N}.png");
        try
        {
            var options = new RenderOptions(Math.Clamp(ClientSize.Width, 64, 960),
                Math.Clamp(ClientSize.Height, 64, 960), 128, 4, false, true, path);
            var result = await GpuPathTracer.TryRenderToPngAsync(scene.WithCamera(Camera), options, null, token);
            if (result.Status != GpuRenderStatus.Completed || token.IsCancellationRequested ||
                generation != _gpuPreviewGeneration || !File.Exists(path)) return;
            using var image = Image.FromFile(path);
            var rendered = new Bitmap(image);
            if (generation != _gpuPreviewGeneration || IsDisposed) { rendered.Dispose(); return; }
            _gpuPreview?.Dispose();
            _inputOverlay.Dispose();
            _gpuPreview = rendered;
            Invalidate();
        }
        catch (OperationCanceledException) { }
        catch { }
        finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gpuPreviewTimer.Stop();
            _gpuPreviewTimer.Dispose();
            _gpuPreviewCancellation?.Cancel();
            _gpuPreviewCancellation?.Dispose();
            _gpuPreview?.Dispose();
        }
        base.Dispose(disposing);
    }

    private static bool TryProject(Vector3 world, Vector3 camera, Vector3 right, Vector3 up, Vector3 forward,
        float focal, Vector2 center, out PointF point, out float depth)
    {
        var relative = world - camera;
        depth = Vector3.Dot(relative, forward);
        if (depth <= 0.001F) { point = default; return false; }
        point = new PointF(center.X + Vector3.Dot(relative, right) * focal / depth,
            center.Y - Vector3.Dot(relative, up) * focal / depth);
        return float.IsFinite(point.X) && float.IsFinite(point.Y);
    }

    private static int ToByte(float value) => Math.Clamp((int)MathF.Round(value * 255F), 0, 255);

    internal static int ComputePreviewAlpha(RenderMaterial material)
    {
        if (material.RenderMode == Rv3dViewer.Core.MaterialRenderMode.Glass)
        {
            var ior = MathF.Max(material.IndexOfRefraction, 1.0001F);
            var f0 = MathF.Pow((ior - 1F) / (ior + 1F), 2F);
            return Math.Clamp((int)MathF.Round(Math.Clamp(f0 + 1F - material.Transmission, 0F, 1F) * 255F), 12, 220);
        }
        return material.RenderMode is Rv3dViewer.Core.MaterialRenderMode.Transparent or Rv3dViewer.Core.MaterialRenderMode.Auto &&
               material.Opacity < 0.999F
            ? Math.Clamp((int)MathF.Round(material.Opacity * material.BaseColor.W * 255F), 24, 220)
            : 255;
    }

    private sealed record PreviewTriangle(float Depth, Color Color, PointF[] Points);
}
