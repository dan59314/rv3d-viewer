namespace Rv3dViewer.Plugin.WinForms;

public partial class InputOverlaySettingsForm : Form
{
    private Color _textColor;

    public InputOverlaySettingsForm(InputOverlayStyle style)
    {
        InitializeComponent();
        _textColor = style.TextColor;
        fontSizeNumericUpDown.Value = Math.Clamp((decimal)style.FontSize,
            fontSizeNumericUpDown.Minimum, fontSizeNumericUpDown.Maximum);
        UpdateColorPreview();
    }

    public InputOverlayStyle Style => new(_textColor, (float)fontSizeNumericUpDown.Value);

    private void TextColorButton_Click(object? sender, EventArgs e)
    {
        var committedColor = _textColor;
        using var dialog = new RichColorPickerForm(committedColor);
        dialog.PreviewColorChanged += (_, _) =>
        {
            _textColor = dialog.SelectedColor;
            ViewportColorPreferences.Preview("OverlayText", _textColor);
            UpdateColorPreview();
        };
        dialog.ApplyRequested += (_, _) =>
        {
            _textColor = dialog.SelectedColor;
            committedColor = _textColor;
            ViewportColorPreferences.Set("OverlayText", _textColor);
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _textColor = dialog.SelectedColor;
            ViewportColorPreferences.Set("OverlayText", _textColor);
        }
        else
        {
            _textColor = committedColor;
            ViewportColorPreferences.Preview("OverlayText", committedColor);
        }
        UpdateColorPreview();
    }

    private void UpdateColorPreview() => colorPreviewPanel.BackColor = _textColor;
}
