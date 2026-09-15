namespace Rv3dViewer.Plugin.WinForms;

public partial class ViewportColorSettingsForm : Form
{
    private readonly IReadOnlyList<ViewportColorDefinition> _definitions;

    public ViewportColorSettingsForm()
        : this(ViewportColorPreferences.Definitions)
    {
    }

    public ViewportColorSettingsForm(
        IEnumerable<ViewportColorDefinition> definitions,
        string? title = null)
    {
        InitializeComponent();
        _definitions = definitions.ToArray();
        if (!string.IsNullOrWhiteSpace(title)) Text = title;
        RebuildRows();
        ViewportColorPreferences.Changed += ViewportColorPreferences_Changed;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        ViewportColorPreferences.Changed -= ViewportColorPreferences_Changed;
        base.OnFormClosed(e);
    }

    private void ViewportColorPreferences_Changed(object? sender, EventArgs e) => RebuildRows();

    private void RebuildRows()
    {
        var selectedKey = colorGrid.CurrentRow?.Tag as string;
        colorGrid.Rows.Clear();
        foreach (var definition in _definitions)
        {
            var color = ViewportColorPreferences.Get(definition.Key);
            var index = colorGrid.Rows.Add(definition.Category, definition.DisplayName, ColorText(color));
            var row = colorGrid.Rows[index];
            row.Tag = definition.Key;
            row.Cells[colorColumn.Index].Style.BackColor = Color.FromArgb(255, color);
            row.Cells[colorColumn.Index].Style.ForeColor = GetContrastColor(color);
            if (definition.Key == selectedKey) row.Selected = true;
        }
    }

    private void EditSelectedColor()
    {
        if (colorGrid.CurrentRow?.Tag is not string key) return;
        var committedColor = ViewportColorPreferences.Get(key);
        using var dialog = new RichColorPickerForm(committedColor);
        dialog.PreviewColorChanged += (_, _) =>
            ViewportColorPreferences.Preview(key, dialog.SelectedColor);
        dialog.ApplyRequested += (_, _) =>
        {
            ViewportColorPreferences.Set(key, dialog.SelectedColor);
            committedColor = dialog.SelectedColor;
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            ViewportColorPreferences.Set(key, dialog.SelectedColor);
        else
            ViewportColorPreferences.Preview(key, committedColor);
        RebuildRows();
        colorGrid.Refresh();
    }

    private void EditButton_Click(object? sender, EventArgs e) => EditSelectedColor();

    private void ColorGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0) EditSelectedColor();
    }

    private void ResetButton_Click(object? sender, EventArgs e)
    {
        if (colorGrid.CurrentRow?.Tag is string key) ViewportColorPreferences.Reset(key);
    }

    private void ResetAllButton_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this, "確定將所有 ViewPort 顏色恢復預設值？", "恢復預設顏色",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            ViewportColorPreferences.Reset(_definitions.Select(definition => definition.Key));
    }

    private static string ColorText(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static Color GetContrastColor(Color color) =>
        color.R * 299 + color.G * 587 + color.B * 114 > 150000 ? Color.Black : Color.White;
}
