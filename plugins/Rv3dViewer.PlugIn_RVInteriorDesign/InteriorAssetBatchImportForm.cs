namespace Rv3dViewer.RVInteriorDesignPlugin;

internal sealed record InteriorAssetImportEntry(string SourcePath, string Name);

internal sealed partial class InteriorAssetBatchImportForm : Form
{
    internal InteriorAssetBatchImportForm(IEnumerable<string> sourcePaths, string initialCategory)
    {
        InitializeComponent();
        categoryComboBox.Items.AddRange(InteriorAssetCatalog.Categories.Cast<object>().ToArray());
        categoryComboBox.SelectedItem = InteriorAssetCatalog.Categories.Contains(initialCategory)
            ? initialCategory
            : "使用者自訂";
        foreach (var path in sourcePaths)
            modelsDataGridView.Rows.Add(path, Path.GetFileNameWithoutExtension(path));
    }

    internal IReadOnlyList<InteriorAssetImportEntry> Entries => modelsDataGridView.Rows
        .Cast<DataGridViewRow>()
        .Where(row => !row.IsNewRow)
        .Select(row => new InteriorAssetImportEntry(
            row.Cells[fileColumn.Index].Value?.ToString() ?? string.Empty,
            row.Cells[nameColumn.Index].Value?.ToString()?.Trim() ?? string.Empty))
        .ToArray();

    internal string AssetCategory => categoryComboBox.SelectedItem?.ToString() ?? "使用者自訂";

    internal string AssetLicense => licenseTextBox.Text.Trim();

    internal string AssetSourceUrl => sourceUrlTextBox.Text.Trim();

    private void ConfirmButton_Click(object? sender, EventArgs e)
    {
        modelsDataGridView.EndEdit();
        if (Entries.Count == 0 || Entries.Any(entry => entry.Name.Length == 0))
        {
            MessageBox.Show(this, "每個模型都必須有名稱。", "批次加入模型",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (AssetLicense.Length == 0)
        {
            MessageBox.Show(this, "請輸入授權或來源說明。", "批次加入模型",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            licenseTextBox.Focus();
            return;
        }
        DialogResult = DialogResult.OK;
    }
}
