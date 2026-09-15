using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Rv3dViewer.Plugin.WinForms;

public sealed class ColorWheelControl : Control
{
    private double _hue;
    private double _saturation;
    private bool _dragging;
    private Bitmap? _wheelBitmap;
    private Size _wheelBitmapSize;

    public ColorWheelControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
    }

    public event EventHandler? SelectionChanged;
    public event EventHandler? InteractionCompleted;
    public double Hue => _hue;
    public double Saturation => _saturation;
    public bool IsDragging => _dragging;

    public void SetSelection(double hue, double saturation)
    {
        _hue = NormalizeHue(hue);
        _saturation = Math.Clamp(saturation, 0d, 1d);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        _dragging = true;
        UpdateSelection(e.Location);
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging) UpdateSelection(e.Location);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        _dragging = false;
        Capture = false;
        InteractionCompleted?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var bounds = WheelBounds();
        if (bounds.Width < 2 || bounds.Height < 2) return;
        EnsureWheelBitmap(bounds.Size);
        if (_wheelBitmap is not null)
        {
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(_wheelBitmap, bounds);
        }
        var angle = _hue * Math.PI / 180d;
        var radius = bounds.Width * 0.5f;
        var center = new PointF(bounds.Left + radius, bounds.Top + radius);
        var marker = new PointF(center.X + (float)Math.Cos(angle) * radius * (float)_saturation,
            center.Y + (float)Math.Sin(angle) * radius * (float)_saturation);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var outer = new Pen(Color.White, 4f);
        using var inner = new Pen(Color.Black, 2f);
        e.Graphics.DrawEllipse(outer, marker.X - 7f, marker.Y - 7f, 14f, 14f);
        e.Graphics.DrawEllipse(inner, marker.X - 7f, marker.Y - 7f, 14f, 14f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _wheelBitmap?.Dispose();
        base.Dispose(disposing);
    }

    private Rectangle WheelBounds()
    {
        var side = Math.Max(0, Math.Min(ClientSize.Width, ClientSize.Height) - 8);
        return new Rectangle((ClientSize.Width - side) / 2, (ClientSize.Height - side) / 2, side, side);
    }

    private void UpdateSelection(Point point)
    {
        var bounds = WheelBounds();
        var radius = bounds.Width * 0.5;
        if (radius <= 0d) return;
        var dx = point.X - (bounds.Left + radius);
        var dy = point.Y - (bounds.Top + radius);
        _hue = NormalizeHue(Math.Atan2(dy, dx) * 180d / Math.PI);
        _saturation = Math.Clamp(Math.Sqrt(dx * dx + dy * dy) / radius, 0d, 1d);
        Invalidate();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureWheelBitmap(Size size)
    {
        if (_wheelBitmap is not null && _wheelBitmapSize == size) return;
        _wheelBitmap?.Dispose();
        _wheelBitmapSize = size;
        _wheelBitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
        var radius = size.Width * 0.5;
        var center = radius;
        for (var y = 0; y < size.Height; y++)
        for (var x = 0; x < size.Width; x++)
        {
            var dx = x + 0.5 - center;
            var dy = y + 0.5 - center;
            var saturation = Math.Sqrt(dx * dx + dy * dy) / radius;
            if (saturation > 1d)
            {
                _wheelBitmap.SetPixel(x, y, Color.Transparent);
                continue;
            }
            var hue = NormalizeHue(Math.Atan2(dy, dx) * 180d / Math.PI);
            _wheelBitmap.SetPixel(x, y, ColorPickerMath.FromHsv(hue, saturation, 1d));
        }
    }

    private static double NormalizeHue(double hue) => (hue % 360d + 360d) % 360d;
}

public enum ColorSliderComponent { Value, Alpha }

public sealed class ColorComponentSlider : Control
{
    private double _component = 1d;
    private double _hue;
    private double _saturation;
    private double _value = 1d;
    private bool _dragging;

    public ColorComponentSlider()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
    }

    public event EventHandler? ComponentChanged;
    public event EventHandler? InteractionCompleted;
    public ColorSliderComponent Component { get; set; }
    public Orientation Orientation { get; set; } = Orientation.Horizontal;
    public double ComponentValue => _component;
    public bool IsDragging => _dragging;

    public void SetColor(double hue, double saturation, double value, double component)
    {
        _hue = hue;
        _saturation = saturation;
        _value = value;
        _component = Math.Clamp(component, 0d, 1d);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        Capture = true;
        UpdateComponent(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging) UpdateComponent(e.Location);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        _dragging = false;
        Capture = false;
        InteractionCompleted?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var track = new Rectangle(8, 8, Math.Max(1, Width - 16), Math.Max(1, Height - 16));
        using var brush = new LinearGradientBrush(track, Color.Black, Color.White,
            Orientation == Orientation.Horizontal ? LinearGradientMode.Horizontal : LinearGradientMode.Vertical);
        if (Component == ColorSliderComponent.Value)
        {
            var full = ColorPickerMath.FromHsv(_hue, _saturation, 1d);
            brush.LinearColors = Orientation == Orientation.Horizontal
                ? [Color.Black, full]
                : [full, Color.Black];
        }
        else
        {
            var opaque = ColorPickerMath.FromHsv(_hue, _saturation, _value);
            brush.LinearColors = Orientation == Orientation.Horizontal
                ? [Color.FromArgb(0, opaque), Color.FromArgb(255, opaque)]
                : [Color.FromArgb(255, opaque), Color.FromArgb(0, opaque)];
        }
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillRectangle(brush, track);
        using var border = new Pen(SystemColors.ControlDark);
        e.Graphics.DrawRectangle(border, track);
        var position = Orientation == Orientation.Horizontal
            ? new PointF(track.Left + (float)_component * track.Width, track.Top + track.Height / 2f)
            : new PointF(track.Left + track.Width / 2f, track.Bottom - (float)_component * track.Height);
        using var outer = new Pen(Color.White, 4f);
        using var inner = new Pen(Color.Black, 2f);
        e.Graphics.DrawEllipse(outer, position.X - 6f, position.Y - 6f, 12f, 12f);
        e.Graphics.DrawEllipse(inner, position.X - 6f, position.Y - 6f, 12f, 12f);
    }

    private void UpdateComponent(Point point)
    {
        _component = Orientation == Orientation.Horizontal
            ? Math.Clamp((point.X - 8d) / Math.Max(1d, Width - 16d), 0d, 1d)
            : Math.Clamp((Height - 8d - point.Y) / Math.Max(1d, Height - 16d), 0d, 1d);
        Invalidate();
        ComponentChanged?.Invoke(this, EventArgs.Empty);
    }
}

internal static class ColorPickerMath
{
    public static Color FromHsv(double hue, double saturation, double value)
    {
        var chroma = value * saturation;
        var h = ((hue % 360d + 360d) % 360d) / 60d;
        var x = chroma * (1d - Math.Abs(h % 2d - 1d));
        var (r, g, b) = h switch
        {
            < 1d => (chroma, x, 0d), < 2d => (x, chroma, 0d), < 3d => (0d, chroma, x),
            < 4d => (0d, x, chroma), < 5d => (x, 0d, chroma), _ => (chroma, 0d, x)
        };
        var m = value - chroma;
        return Color.FromArgb((int)Math.Round((r + m) * 255d), (int)Math.Round((g + m) * 255d),
            (int)Math.Round((b + m) * 255d));
    }
}
