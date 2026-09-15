#nullable enable

namespace Rv3dViewer.Plugin.WinForms;

partial class ViewportColorSettingsForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel mainLayoutPanel = null!;
    private DataGridView colorGrid = null!;
    private DataGridViewTextBoxColumn categoryColumn = null!;
    private DataGridViewTextBoxColumn nameColumn = null!;
    private DataGridViewTextBoxColumn colorColumn = null!;
    private FlowLayoutPanel buttonsPanel = null!;
    private Button editButton = null!;
    private Button resetButton = null!;
    private Button resetAllButton = null!;
    private Button closeButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        mainLayoutPanel = new TableLayoutPanel();
        colorGrid = new DataGridView();
        categoryColumn = new DataGridViewTextBoxColumn();
        nameColumn = new DataGridViewTextBoxColumn();
        colorColumn = new DataGridViewTextBoxColumn();
        buttonsPanel = new FlowLayoutPanel();
        editButton = new Button();
        resetButton = new Button();
        resetAllButton = new Button();
        closeButton = new Button();
        mainLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)colorGrid).BeginInit();
        buttonsPanel.SuspendLayout();
        SuspendLayout();
        mainLayoutPanel.ColumnCount = 1;
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.Controls.Add(colorGrid, 0, 0);
        mainLayoutPanel.Controls.Add(buttonsPanel, 0, 1);
        mainLayoutPanel.Dock = DockStyle.Fill;
        mainLayoutPanel.Location = new Point(10, 10);
        mainLayoutPanel.Name = "mainLayoutPanel";
        mainLayoutPanel.RowCount = 2;
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        mainLayoutPanel.Size = new Size(674, 650);
        mainLayoutPanel.TabIndex = 0;
        colorGrid.AllowUserToAddRows = false;
        colorGrid.AllowUserToDeleteRows = false;
        colorGrid.AllowUserToResizeRows = false;
        colorGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        colorGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        colorGrid.Columns.AddRange(categoryColumn, nameColumn, colorColumn);
        colorGrid.Dock = DockStyle.Fill;
        colorGrid.MultiSelect = false;
        colorGrid.Name = "colorGrid";
        colorGrid.ReadOnly = true;
        colorGrid.RowHeadersVisible = false;
        colorGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        colorGrid.TabIndex = 0;
        colorGrid.CellDoubleClick += ColorGrid_CellDoubleClick;
        categoryColumn.FillWeight = 34F;
        categoryColumn.HeaderText = "分類";
        categoryColumn.Name = "categoryColumn";
        categoryColumn.ReadOnly = true;
        nameColumn.FillWeight = 42F;
        nameColumn.HeaderText = "用途";
        nameColumn.Name = "nameColumn";
        nameColumn.ReadOnly = true;
        colorColumn.FillWeight = 24F;
        colorColumn.HeaderText = "顏色 (ARGB)";
        colorColumn.Name = "colorColumn";
        colorColumn.ReadOnly = true;
        buttonsPanel.AutoSize = false;
        buttonsPanel.Controls.Add(closeButton);
        buttonsPanel.Controls.Add(resetAllButton);
        buttonsPanel.Controls.Add(resetButton);
        buttonsPanel.Controls.Add(editButton);
        buttonsPanel.Dock = DockStyle.Fill;
        buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Padding = new Padding(0, 8, 0, 0);
        buttonsPanel.TabIndex = 1;
        buttonsPanel.WrapContents = false;
        closeButton.DialogResult = DialogResult.OK;
        closeButton.Margin = new Padding(6, 3, 0, 3);
        closeButton.Size = new Size(82, 32);
        closeButton.Text = "關閉";
        closeButton.UseVisualStyleBackColor = true;
        resetAllButton.Text = "全部預設值";
        resetAllButton.Size = new Size(104, 32);
        resetAllButton.UseVisualStyleBackColor = true;
        resetAllButton.Click += ResetAllButton_Click;
        resetButton.Text = "此項預設值";
        resetButton.Size = new Size(104, 32);
        resetButton.UseVisualStyleBackColor = true;
        resetButton.Click += ResetButton_Click;
        editButton.Text = "選擇顏色…";
        editButton.Size = new Size(104, 32);
        editButton.UseVisualStyleBackColor = true;
        editButton.Click += EditButton_Click;
        AcceptButton = closeButton;
        AutoScaleMode = AutoScaleMode.None;
        CancelButton = closeButton;
        ClientSize = new Size(694, 670);
        Controls.Add(mainLayoutPanel);
        MinimizeBox = false;
        MinimumSize = new Size(710, 709);
        Name = "ViewportColorSettingsForm";
        Padding = new Padding(10);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "ViewPort 顏色設定";
        mainLayoutPanel.ResumeLayout(false);
        mainLayoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)colorGrid).EndInit();
        buttonsPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
