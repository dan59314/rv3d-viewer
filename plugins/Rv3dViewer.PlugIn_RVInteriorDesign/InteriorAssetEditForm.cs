namespace Rv3dViewer.RVInteriorDesignPlugin;

internal sealed partial class InteriorAssetEditForm : Form
{
    internal InteriorAssetEditForm(InteriorAssetDescriptor asset)
    {
        InitializeComponent();
        nameTextBox.Text = asset.Name;
        categoryComboBox.Items.AddRange(InteriorAssetCatalog.Categories.Cast<object>().ToArray());
        categoryComboBox.SelectedItem = InteriorAssetCatalog.Categories.Contains(asset.Category)
            ? asset.Category
            : "使用者自訂";
        licenseTextBox.Text = asset.License;
        sourceUrlTextBox.Text = asset.SourceUrl;
    }

    internal string AssetName => nameTextBox.Text.Trim();

    internal string AssetCategory => categoryComboBox.SelectedItem?.ToString() ?? "使用者自訂";

    internal string AssetLicense => licenseTextBox.Text.Trim();

    internal string AssetSourceUrl => sourceUrlTextBox.Text.Trim();

    private void ConfirmButton_Click(object? sender, EventArgs e)
    {
        if (AssetName.Length == 0)
        {
            MessageBox.Show(this, "請輸入模型名稱。", "編輯模型庫資產",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            nameTextBox.Focus();
            return;
        }
        if (AssetLicense.Length == 0)
        {
            MessageBox.Show(this, "請輸入授權或來源說明。", "編輯模型庫資產",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            licenseTextBox.Focus();
            return;
        }
        DialogResult = DialogResult.OK;
    }
}
