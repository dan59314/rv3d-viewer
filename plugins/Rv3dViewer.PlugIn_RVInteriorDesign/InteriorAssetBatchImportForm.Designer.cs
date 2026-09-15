#nullable enable

namespace Rv3dViewer.RVInteriorDesignPlugin;

partial class InteriorAssetBatchImportForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel importTableLayoutPanel = null!;
    private DataGridView modelsDataGridView = null!;
    private DataGridViewTextBoxColumn fileColumn = null!;
    private DataGridViewTextBoxColumn nameColumn = null!;
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
        importTableLayoutPanel = new TableLayoutPanel();
        modelsDataGridView = new DataGridView();
        fileColumn = new DataGridViewTextBoxColumn();
        nameColumn = new DataGridViewTextBoxColumn();
        categoryLabel = new Label();
        categoryComboBox = new ComboBox();
        licenseLabel = new Label();
        licenseTextBox = new TextBox();
        sourceUrlLabel = new Label();
        sourceUrlTextBox = new TextBox();
        commandPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        confirmButton = new Button();
        importTableLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)modelsDataGridView).BeginInit();
        commandPanel.SuspendLayout();
        SuspendLayout();
        importTableLayoutPanel.ColumnCount = 2;
        importTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        importTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        importTableLayoutPanel.Controls.Add(modelsDataGridView, 0, 0);
        importTableLayoutPanel.Controls.Add(categoryLabel, 0, 1);
        importTableLayoutPanel.Controls.Add(categoryComboBox, 1, 1);
        importTableLayoutPanel.Controls.Add(licenseLabel, 0, 2);
        importTableLayoutPanel.Controls.Add(licenseTextBox, 1, 2);
        importTableLayoutPanel.Controls.Add(sourceUrlLabel, 0, 3);
        importTableLayoutPanel.Controls.Add(sourceUrlTextBox, 1, 3);
        importTableLayoutPanel.Controls.Add(commandPanel, 0, 4);
        importTableLayoutPanel.Dock = DockStyle.Fill;
        importTableLayoutPanel.Name = "importTableLayoutPanel";
        importTableLayoutPanel.Padding = new Padding(10);
        importTableLayoutPanel.RowCount = 5;
        importTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        importTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        importTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        importTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        importTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        importTableLayoutPanel.SetColumnSpan(modelsDataGridView, 2);
        modelsDataGridView.AllowUserToAddRows = false;
        modelsDataGridView.AllowUserToDeleteRows = false;
        modelsDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        modelsDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        modelsDataGridView.Columns.AddRange(fileColumn, nameColumn);
        modelsDataGridView.Dock = DockStyle.Fill;
        modelsDataGridView.Name = "modelsDataGridView";
        modelsDataGridView.RowHeadersVisible = false;
        modelsDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        fileColumn.FillWeight = 65F;
        fileColumn.HeaderText = "檔案";
        fileColumn.Name = "fileColumn";
        fileColumn.ReadOnly = true;
        nameColumn.FillWeight = 35F;
        nameColumn.HeaderText = "模型名稱（可編輯）";
        nameColumn.Name = "nameColumn";
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
        licenseTextBox.Text = "由使用者確認";
        sourceUrlLabel.Dock = DockStyle.Fill;
        sourceUrlLabel.Name = "sourceUrlLabel";
        sourceUrlLabel.Text = "來源網址";
        sourceUrlLabel.TextAlign = ContentAlignment.MiddleLeft;
        sourceUrlTextBox.Dock = DockStyle.Fill;
        sourceUrlTextBox.Name = "sourceUrlTextBox";
        sourceUrlTextBox.PlaceholderText = "https://...";
        importTableLayoutPanel.SetColumnSpan(commandPanel, 2);
        commandPanel.Controls.Add(cancelButton);
        commandPanel.Controls.Add(confirmButton);
        commandPanel.Dock = DockStyle.Fill;
        commandPanel.FlowDirection = FlowDirection.RightToLeft;
        commandPanel.Name = "commandPanel";
        commandPanel.Padding = new Padding(0, 7, 0, 0);
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(88, 28);
        cancelButton.Text = "取消";
        cancelButton.UseVisualStyleBackColor = true;
        confirmButton.Name = "confirmButton";
        confirmButton.Size = new Size(88, 28);
        confirmButton.Text = "開始加入";
        confirmButton.UseVisualStyleBackColor = true;
        confirmButton.Click += ConfirmButton_Click;
        AcceptButton = confirmButton;
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(680, 480);
        Controls.Add(importTableLayoutPanel);
        MinimizeBox = false;
        Name = "InteriorAssetBatchImportForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "批次加入高品質模型";
        importTableLayoutPanel.ResumeLayout(false);
        importTableLayoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)modelsDataGridView).EndInit();
        commandPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
