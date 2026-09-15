namespace Rv3dViewer.Plugin.WinForms;

public partial class RichColorPickerForm : Form
{
    private bool _updating;
    private bool _ready;

    public RichColorPickerForm(Color color)
    {
        InitializeComponent();
        SelectedColor = color;
        LoadColor(color);
        _ready = true;
    }

    public Color SelectedColor { get; private set; }
    public event EventHandler? PreviewColorChanged;
    public event EventHandler? ApplyRequested;

    private void OkButton_Click(object? sender, EventArgs e)
    {
        // Commit only from this explicit action. The owner updates its setting
        // and redraws the original ViewPort after ShowDialog returns OK.
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ApplyButton_Click(object? sender, EventArgs e) =>
        ApplyRequested?.Invoke(this, EventArgs.Empty);

    private void Rgba_ValueChanged(object? sender, EventArgs e)
    {
        if (_updating) return;
        SetColor(Color.FromArgb((int)alphaNumericUpDown.Value, (int)redNumericUpDown.Value,
            (int)greenNumericUpDown.Value, (int)blueNumericUpDown.Value), updateHsv: true);
    }

    private void Hsv_ValueChanged(object? sender, EventArgs e)
    {
        if (_updating) return;
        var rgb = FromHsv((double)hueNumericUpDown.Value, (double)saturationNumericUpDown.Value / 100d,
            (double)valueNumericUpDown.Value / 100d);
        SetColor(Color.FromArgb((int)alphaNumericUpDown.Value, rgb), updateHsv: false);
        UpdateVisualPicker();
    }

    private void ColorWheel_SelectionChanged(object? sender, EventArgs e)
    {
        if (_updating) return;
        _updating = true;
        hueNumericUpDown.Value = Math.Clamp((decimal)colorWheel.Hue, 0, 360);
        saturationNumericUpDown.Value = Math.Clamp((decimal)(colorWheel.Saturation * 100d), 0, 100);
        _updating = false;
        var rgb = FromHsv(colorWheel.Hue, colorWheel.Saturation, (double)valueNumericUpDown.Value / 100d);
        SetColor(Color.FromArgb((int)alphaNumericUpDown.Value, rgb), updateHsv: false);
        UpdateVisualPicker();
    }

    private void ValueSlider_ComponentChanged(object? sender, EventArgs e)
    {
        if (_updating) return;
        valueNumericUpDown.Value = Math.Clamp((decimal)(valueSlider.ComponentValue * 100d), 0, 100);
    }

    private void AlphaSlider_ComponentChanged(object? sender, EventArgs e)
    {
        if (_updating) return;
        alphaNumericUpDown.Value = Math.Clamp((decimal)(alphaSlider.ComponentValue * 255d), 0, 255);
    }

    private void ColorPicker_InteractionCompleted(object? sender, EventArgs e) =>
        RaisePreviewColorChanged(force: true);

    private void HexTextBox_Validated(object? sender, EventArgs e)
    {
        var text = hexTextBox.Text.Trim().TrimStart('#');
        if (text.Length is not (6 or 8) || !uint.TryParse(text, System.Globalization.NumberStyles.HexNumber,
                null, out var value))
        {
            LoadColor(SelectedColor);
            return;
        }
        var color = text.Length == 8
            ? Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value)
            : Color.FromArgb(255, (byte)(value >> 16), (byte)(value >> 8), (byte)value);
        LoadColor(color);
    }

    private void SetColor(Color color, bool updateHsv)
    {
        SelectedColor = color;
        previewPanel.BackColor = color;
        hexTextBox.Text = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        if (!updateHsv)
        {
            RaisePreviewColorChanged();
            return;
        }
        _updating = true;
        var (hue, saturation, value) = ToHsv(color);
        hueNumericUpDown.Value = Math.Clamp((decimal)hue, 0, 360);
        saturationNumericUpDown.Value = Math.Clamp((decimal)(saturation * 100d), 0, 100);
        valueNumericUpDown.Value = Math.Clamp((decimal)(value * 100d), 0, 100);
        colorWheel.SetSelection(hue, saturation);
        valueSlider.SetColor(hue, saturation, value, value);
        alphaSlider.SetColor(hue, saturation, value, color.A / 255d);
        _updating = false;
        RaisePreviewColorChanged();
    }

    private void LoadColor(Color color)
    {
        _updating = true;
        SelectedColor = color;
        alphaNumericUpDown.Value = color.A;
        redNumericUpDown.Value = color.R;
        greenNumericUpDown.Value = color.G;
        blueNumericUpDown.Value = color.B;
        var (hue, saturation, value) = ToHsv(color);
        hueNumericUpDown.Value = Math.Clamp((decimal)hue, 0, 360);
        saturationNumericUpDown.Value = Math.Clamp((decimal)(saturation * 100d), 0, 100);
        valueNumericUpDown.Value = Math.Clamp((decimal)(value * 100d), 0, 100);
        colorWheel.SetSelection(hue, saturation);
        valueSlider.SetColor(hue, saturation, value, value);
        alphaSlider.SetColor(hue, saturation, value, color.A / 255d);
        previewPanel.BackColor = color;
        hexTextBox.Text = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        _updating = false;
        RaisePreviewColorChanged();
    }

    private void RaisePreviewColorChanged(bool force = false)
    {
        if (!_ready) return;
        if (!force && (colorWheel.IsDragging || valueSlider.IsDragging || alphaSlider.IsDragging))
            return;
        PreviewColorChanged?.Invoke(this, EventArgs.Empty);
    }

    private static Color FromHsv(double hue, double saturation, double value)
        => ColorPickerMath.FromHsv(hue, saturation, value);

    private void UpdateVisualPicker()
    {
        var hue = (double)hueNumericUpDown.Value;
        var saturation = (double)saturationNumericUpDown.Value / 100d;
        var value = (double)valueNumericUpDown.Value / 100d;
        colorWheel.SetSelection(hue, saturation);
        valueSlider.SetColor(hue, saturation, value, value);
        alphaSlider.SetColor(hue, saturation, value, (double)alphaNumericUpDown.Value / 255d);
    }

    private static (double Hue, double Saturation, double Value) ToHsv(Color color)
    {
        var red = color.R / 255d;
        var green = color.G / 255d;
        var blue = color.B / 255d;
        var maximum = Math.Max(red, Math.Max(green, blue));
        var minimum = Math.Min(red, Math.Min(green, blue));
        var delta = maximum - minimum;
        var hue = delta == 0d
            ? 0d
            : maximum == red
                ? 60d * (((green - blue) / delta) % 6d)
                : maximum == green
                    ? 60d * (((blue - red) / delta) + 2d)
                    : 60d * (((red - green) / delta) + 4d);
        if (hue < 0d) hue += 360d;
        return (hue, maximum == 0d ? 0d : delta / maximum, maximum);
    }
}
