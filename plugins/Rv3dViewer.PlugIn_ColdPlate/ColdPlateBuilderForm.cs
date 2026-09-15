using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Numerics;
using Rv3dViewer.Core;
using Rv3dViewer.Plugin.WinForms;

namespace Rv3dViewer.ColdPlatePlugin;

internal sealed partial class ColdPlateBuilderForm : Form
{
    private ColdPlateParameters _parameters = new();

    public ColdPlateBuilderForm()
    {
        InitializeComponent();
        InitializeUnifiedMenu();
        BindParameters(new ColdPlateParameters());
    }

    public SceneModel? GeneratedModel { get; private set; }

    private void PropertyGrid_PropertyValueChanged(object? sender, PropertyValueChangedEventArgs e) => RefreshPreview();

    private void PolygonGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e) => RefreshPreview();

    private void PolygonGrid_RowsRemoved(object? sender, DataGridViewRowsRemovedEventArgs e) => RefreshPreview();

    private void PolygonGrid_UserAddedRow(object? sender, DataGridViewRowEventArgs e) => RefreshPreview();

    private void CreateButton_Click(object? sender, EventArgs e) => AcceptModel();

    private void ResetButton_Click(object? sender, EventArgs e) => BindParameters(new ColdPlateParameters());

    private void BindParameters(ColdPlateParameters parameters)
    {
        _parameters = parameters;
        _propertyGrid.SelectedObject = _parameters;
        _polygonGrid.DataSource = _parameters.PolygonVertices;
        _parameters.PolygonVertices.ListChanged += PolygonVertices_ListChanged;
        RefreshPreview();
    }

    private void PolygonVertices_ListChanged(object? sender, ListChangedEventArgs e) => RefreshPreview();

    private void RefreshPreview()
    {
        if (IsDisposed) return;
        try
        {
            var model = ColdPlateGenerator.Generate(_parameters);
            _preview.Model = model;
            _validationLabel.ForeColor = Color.FromArgb(30, 125, 65);
            _validationLabel.Text = "參數有效。拖曳可旋轉預覽，滾輪可縮放。";
            _createButton.Enabled = true;
        }
        catch (Exception ex)
        {
            _preview.Model = null;
            _validationLabel.ForeColor = Color.Firebrick;
            _validationLabel.Text = ex.Message;
            _createButton.Enabled = false;
        }
    }

    private void AcceptModel()
    {
        try
        {
            GeneratedModel = ColdPlateGenerator.Generate(_parameters);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "無法建立冷板", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

internal sealed class ColdPlatePreviewControl : Control
{
    private SceneModel? _model;
    private float _yaw = -35f;
    private float _pitch = 24f;
    private float _zoom = 1f;
    private Point? _dragStart;
    private readonly InputActivityOverlay _inputOverlay;

    public ColdPlatePreviewControl()
    {
        DoubleBuffered = true;
        BackColor = ViewportColorPreferences.Get("ColdPlateBackground");
        SetStyle(ControlStyles.ResizeRedraw, true);
        _inputOverlay = new InputActivityOverlay(this);
        ViewportColorPreferences.Changed += ViewportColorPreferences_Changed;
    }

    public bool InputOverlayEnabled { get => _inputOverlay.Enabled; set => _inputOverlay.Enabled = value; }
    public void ShowInputActivity(string text) => _inputOverlay.Show(text);

    public SceneModel? Model
    {
        get => _model;
        set { _model = value; Invalidate(); }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _inputOverlay.Show(InputActivityFormatter.Mouse("MouseDn", e.Button));
        if (e.Button == MouseButtons.Left) _dragStart = e.Location;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (InputActivityFormatter.ShouldShowMove(e.Button))
            _inputOverlay.Show(InputActivityFormatter.Mouse(e.Button == MouseButtons.None ? "Mouse Move" : "Mouse Drag", e.Button));
        if (_dragStart is not Point start || e.Button != MouseButtons.Left) return;
        _yaw += e.X - start.X;
        _pitch = Math.Clamp(_pitch + e.Y - start.Y, -89f, 89f);
        _dragStart = e.Location;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _inputOverlay.Show(InputActivityFormatter.Mouse("MouseUp", e.Button)); _dragStart = null; }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        _inputOverlay.Show(InputActivityFormatter.Mouse(e.Delta >= 0 ? "Mouse Wheel Up" : "Mouse Wheel Down", MouseButtons.None));
        _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.12f : 0.89f), 0.25f, 5f);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        DrawGrid(e.Graphics);
        if (_model is null || _model.Meshes.Count == 0)
        {
            TextRenderer.DrawText(e.Graphics, "無法預覽", Font, ClientRectangle,
                ViewportColorPreferences.Get("PreviewHelpText"),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            _inputOverlay.Draw(e.Graphics, ClientRectangle);
            return;
        }

        var rotation = Matrix4x4.CreateRotationY(_yaw * MathF.PI / 180f) *
                       Matrix4x4.CreateRotationX(_pitch * MathF.PI / 180f);
        var points = _model.Meshes.SelectMany(mesh => mesh.Positions).Select(point => Vector3.Transform(point, rotation)).ToArray();
        if (points.Length == 0) { _inputOverlay.Draw(e.Graphics, ClientRectangle); return; }
        var minX = points.Min(p => p.X); var maxX = points.Max(p => p.X);
        var minY = points.Min(p => p.Y); var maxY = points.Max(p => p.Y);
        var span = Math.Max(maxX - minX, maxY - minY);
        var scale = span < 0.001f ? 1f : Math.Min(Width, Height) * 0.78f / span * _zoom;
        var center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        var triangles = new List<PreviewTriangle>();

        for (var meshIndex = 0; meshIndex < _model.Meshes.Count; meshIndex++)
        {
            var mesh = _model.Meshes[meshIndex];
            var materialIndex = Math.Clamp(mesh.MaterialIndex, 0, Math.Max(0, _model.Materials.Count - 1));
            var color = materialIndex < _model.Materials.Count && _model.Materials[materialIndex].IsGlass
                ? Color.FromArgb(105, 135, 210, 235)
                : Color.FromArgb(235, 188, 83, 40);
            for (var i = 0; i + 2 < mesh.Indices.Length; i += 3)
            {
                var a = Vector3.Transform(mesh.Positions[mesh.Indices[i]], rotation);
                var b = Vector3.Transform(mesh.Positions[mesh.Indices[i + 1]], rotation);
                var c = Vector3.Transform(mesh.Positions[mesh.Indices[i + 2]], rotation);
                triangles.Add(new PreviewTriangle((a.Z + b.Z + c.Z) / 3f, color,
                    [Project(a, center, scale), Project(b, center, scale), Project(c, center, scale)]));
            }
        }

        foreach (var triangle in triangles.OrderBy(item => item.Depth))
        {
            using var brush = new SolidBrush(triangle.Color);
            using var pen = new Pen(ViewportColorPreferences.Get("ColdPlateEdge"), 0.6f);
            e.Graphics.FillPolygon(brush, triangle.Points);
            e.Graphics.DrawPolygon(pen, triangle.Points);
        }
        _inputOverlay.Draw(e.Graphics, ClientRectangle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ViewportColorPreferences.Changed -= ViewportColorPreferences_Changed;
            _inputOverlay.Dispose();
        }
        base.Dispose(disposing);
    }

    private PointF Project(Vector3 point, Vector2 center, float scale) =>
        new(Width / 2f + (point.X - center.X) * scale, Height / 2f - (point.Y - center.Y) * scale);

    private void DrawGrid(Graphics graphics)
    {
        using var pen = new Pen(ViewportColorPreferences.Get("ColdPlateGrid"));
        const int spacing = 32;
        for (var x = Width % spacing / 2; x < Width; x += spacing) graphics.DrawLine(pen, x, 0, x, Height);
        for (var y = Height % spacing / 2; y < Height; y += spacing) graphics.DrawLine(pen, 0, y, Width, y);
    }

    private void ViewportColorPreferences_Changed(object? sender, EventArgs e)
    {
        BackColor = ViewportColorPreferences.Get("ColdPlateBackground");
        Invalidate();
    }

    private sealed record PreviewTriangle(float Depth, Color Color, PointF[] Points);
}
