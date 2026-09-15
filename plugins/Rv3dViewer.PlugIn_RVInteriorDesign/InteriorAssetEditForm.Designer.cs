#nullable enable

namespace Rv3dViewer.RVInteriorDesignPlugin;

partial class InteriorAssetEditForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel editTableLayoutPanel = null!;
    private Label nameLabel = null!;
    private TextBox nameTextBox = null!;
    private Label categoryLabel = null!;
    private ComboBox categoryComboBox = null!;
    private Label licenseLabel = null!;
    private TextBox licenseTextBox = null!;
    private Label sourceUrlLabel = null!;
    private TextBox sourceUrlTextBox = null!;
    private FlowLayoutPanel commandPanel = null!;
    private Button cancelButton = null!;
    private Button confirmButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        editTableLayoutPanel = new TableLayoutPanel();
        nameLabel = new Label();
        nameTextBox = new TextBox();
        categoryLabel = new Label();
        categoryComboBox = new ComboBox();
        licenseLabel = new Label();
        licenseTextBox = new TextBox();
        sourceUrlLabel = new Label();
        sourceUrlTextBox = new TextBox();
        commandPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        confirmButton = new Button();
        editTableLayoutPanel.SuspendLayout();
        commandPanel.SuspendLayout();
        SuspendLayout();
        editTableLayoutPanel.ColumnCount = 2;
        editTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
        editTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        editTableLayoutPanel.Controls.Add(nameLabel, 0, 0);
        editTableLayoutPanel.Controls.Add(nameTextBox, 1, 0);
        editTableLayoutPanel.Controls.Add(categoryLabel, 0, 1);
        editTableLayoutPanel.Controls.Add(categoryComboBox, 1, 1);
        editTableLayoutPanel.Controls.Add(licenseLabel, 0, 2);
        editTableLayoutPanel.Controls.Add(licenseTextBox, 1, 2);
        editTableLayoutPanel.Controls.Add(sourceUrlLabel, 0, 3);
        editTableLayoutPanel.Controls.Add(sourceUrlTextBox, 1, 3);
        editTableLayoutPanel.Controls.Add(commandPanel, 0, 4);
        editTableLayoutPanel.Dock = DockStyle.Fill;
        editTableLayoutPanel.Name = "editTableLayoutPanel";
        editTableLayoutPanel.Padding = new Padding(10);
        editTableLayoutPanel.RowCount = 5;
        editTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        editTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        editTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        editTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        editTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        nameLabel.Dock = DockStyle.Fill;
        nameLabel.Name = "nameLabel";
        nameLabel.Text = "名稱";
        nameLabel.TextAlign = ContentAlignment.MiddleLeft;
        nameTextBox.Dock = DockStyle.Fill;
        nameTextBox.Name = "nameTextBox";
        categoryLabel.Dock = DockStyle.Fill;
        categoryLabel.Name = "categoryLabel";
        categoryLabel.Text = "分類";
        categoryLabel.TextAlign = ContentAlignment.MiddleLeft;
        categoryComboBox.Dock = DockStyle.Fill;
        categoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        categoryComboBox.Name = "categoryComboBox";
        licenseLabel.Dock = DockStyle.Fill;
        licenseLabel.Name = "licenseLabel";
        licenseLabel.Text = "授權／來源";
        licenseLabel.TextAlign = ContentAlignment.MiddleLeft;
        licenseTextBox.Dock = DockStyle.Fill;
        licenseTextBox.Multiline = true;
        licenseTextBox.Name = "licenseTextBox";
        licenseTextBox.ScrollBars = ScrollBars.Vertical;
        sourceUrlLabel.Dock = DockStyle.Fill;
        sourceUrlLabel.Name = "sourceUrlLabel";
        sourceUrlLabel.Text = "來源網址";
        sourceUrlLabel.TextAlign = ContentAlignment.MiddleLeft;
        sourceUrlTextBox.Dock = DockStyle.Fill;
        sourceUrlTextBox.Name = "sourceUrlTextBox";
        sourceUrlTextBox.PlaceholderText = "https://...";
        editTableLayoutPanel.SetColumnSpan(commandPanel, 2);
        commandPanel.Controls.Add(cancelButton);
        commandPanel.Controls.Add(confirmButton);
        commandPanel.Dock = DockStyle.Fill;
        commandPanel.FlowDirection = FlowDirection.RightToLeft;
        commandPanel.Name = "commandPanel";
        commandPanel.Padding = new Padding(0, 6, 0, 0);
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(82, 28);
        cancelButton.Text = "取消";
        cancelButton.UseVisualStyleBackColor = true;
        confirmButton.Name = "confirmButton";
        confirmButton.Size = new Size(82, 28);
        confirmButton.Text = "確定";
        confirmButton.UseVisualStyleBackColor = true;
        confirmButton.Click += ConfirmButton_Click;
        AcceptButton = confirmButton;
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(430, 290);
        Controls.Add(editTableLayoutPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "InteriorAssetEditForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "編輯模型庫資產";
        editTableLayoutPanel.ResumeLayout(false);
        editTableLayoutPanel.PerformLayout();
        commandPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
