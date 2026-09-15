using System.Drawing.Imaging;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.App;

internal sealed partial class PreviewImageExportForm : Form
{
    private readonly OpenGlRenderer _renderer;
    private readonly float _previewAspectRatio;
    private bool _syncingAspectRatio;

    public PreviewImageExportForm(OpenGlRenderer renderer, string? lastOutputPath)
    {
        _renderer = renderer;
        _previewAspectRatio = renderer.PreviewAspectRatio;
        InitializeComponent();
        formatComboBox.SelectedIndex = 2;
        outputPathTextBox.Text = !string.IsNullOrWhiteSpace(lastOutputPath)
            ? lastOutputPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "preview.png");
        // The checkbox is checked by Designer before its event is wired, so
        // explicitly synchronize the initial Height to the active viewport.
        SyncHeightFromWidth();
    }

    public string? SavedOutputPath { get; private set; }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap Image|*.bmp";
        saveFileDialog.FilterIndex = formatComboBox.SelectedIndex + 1;
        saveFileDialog.FileName = Path.GetFileName(outputPathTextBox.Text);
        if (saveFileDialog.ShowDialog(this) == DialogResult.OK) outputPathTextBox.Text = saveFileDialog.FileName;
    }

    private void FormatComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var extension = formatComboBox.SelectedIndex switch { 0 => ".bmp", 1 => ".jpg", _ => ".png" };
        outputPathTextBox.Text = Path.ChangeExtension(outputPathTextBox.Text, extension);
    }

    private void WidthNumericUpDown_ValueChanged(object? sender, EventArgs e) => SyncHeightFromWidth();

    private void HeightNumericUpDown_ValueChanged(object? sender, EventArgs e)
    {
        if (!keepAspectRatioCheckBox.Checked || _syncingAspectRatio) return;
        _syncingAspectRatio = true;
        try
        {
            widthNumericUpDown.Value = Math.Clamp((decimal)Math.Round(heightNumericUpDown.Value * (decimal)_previewAspectRatio),
                widthNumericUpDown.Minimum, widthNumericUpDown.Maximum);
        }
        finally { _syncingAspectRatio = false; }
    }

    private void KeepAspectRatioCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (keepAspectRatioCheckBox.Checked) SyncHeightFromWidth();
    }

    private void SyncHeightFromWidth()
    {
        if (!keepAspectRatioCheckBox.Checked || _syncingAspectRatio) return;
        _syncingAspectRatio = true;
        try
        {
            heightNumericUpDown.Value = Math.Clamp((decimal)Math.Round(widthNumericUpDown.Value / (decimal)_previewAspectRatio),
                heightNumericUpDown.Minimum, heightNumericUpDown.Maximum);
        }
        finally { _syncingAspectRatio = false; }
    }

    private void ExportButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var path = GetAvailableOutputPath(Path.GetFullPath(outputPathTextBox.Text.Trim()));
            if (MessageBox.Show(this, $"確定儲存影像？\n{path}", "確認輸出影像",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var image = _renderer.CapturePreviewImage((int)widthNumericUpDown.Value, (int)heightNumericUpDown.Value);
            if (formatComboBox.SelectedIndex == 0) image.Save(path, ImageFormat.Bmp);
            else if (formatComboBox.SelectedIndex == 1) image.Save(path, ImageFormat.Jpeg);
            else image.Save(path, ImageFormat.Png);
            SavedOutputPath = path;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "輸出影像", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e) => Close();

    private static string GetAvailableOutputPath(string requestedPath)
    {
        if (!File.Exists(requestedPath)) return requestedPath;
        var directory = Path.GetDirectoryName(requestedPath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(requestedPath);
        var extension = Path.GetExtension(requestedPath);
        for (var index = 1; ; index++)
        {
            var candidate = Path.Combine(directory, $"{name}_{index}{extension}");
            if (!File.Exists(candidate)) return candidate;
        }
    }
}
