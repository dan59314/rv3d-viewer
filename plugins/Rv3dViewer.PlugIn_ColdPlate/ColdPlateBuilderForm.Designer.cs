#nullable enable

namespace Rv3dViewer.ColdPlatePlugin;

partial class ColdPlateBuilderForm
{
    private Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip unifiedMenuStrip = null!;
    private SplitContainer splitContainer = null!;
    private TabControl parameterTabControl = null!;
    private TabPage settingsTabPage = null!;
    private PropertyGrid _propertyGrid = null!;
    private TabPage polygonTabPage = null!;
    private Panel polygonEditorPanel = null!;
    private Label polygonNoteLabel = null!;
    private DataGridView _polygonGrid = null!;
    private DataGridViewTextBoxColumn polygonXColumn = null!;
    private DataGridViewTextBoxColumn polygonYColumn = null!;
    private TableLayoutPanel previewLayoutPanel = null!;
    private ColdPlatePreviewControl _preview = null!;
    private Label _validationLabel = null!;
    private FlowLayoutPanel commandFlowLayoutPanel = null!;
    private Button cancelButton = null!;
    private Button _createButton = null!;
    private Button resetButton = null!;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ColdPlateBuilderForm));
        unifiedMenuStrip = new Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip();
        splitContainer = new SplitContainer();
        parameterTabControl = new TabControl();
        settingsTabPage = new TabPage();
        _propertyGrid = new PropertyGrid();
        polygonTabPage = new TabPage();
        polygonEditorPanel = new Panel();
        _polygonGrid = new DataGridView();
        polygonXColumn = new DataGridViewTextBoxColumn();
        polygonYColumn = new DataGridViewTextBoxColumn();
        polygonNoteLabel = new Label();
        previewLayoutPanel = new TableLayoutPanel();
        _preview = new ColdPlatePreviewControl();
        _validationLabel = new Label();
        commandFlowLayoutPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        _createButton = new Button();
        resetButton = new Button();
        ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
        splitContainer.Panel1.SuspendLayout();
        splitContainer.Panel2.SuspendLayout();
        splitContainer.SuspendLayout();
        parameterTabControl.SuspendLayout();
        settingsTabPage.SuspendLayout();
        polygonTabPage.SuspendLayout();
        polygonEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_polygonGrid).BeginInit();
        previewLayoutPanel.SuspendLayout();
        commandFlowLayoutPanel.SuspendLayout();
        SuspendLayout();
        // 
        // splitContainer
        // 
        splitContainer.Dock = DockStyle.Fill;
        splitContainer.FixedPanel = FixedPanel.Panel1;
        splitContainer.Location = new Point(0, 0);
        splitContainer.Name = "splitContainer";
        // 
        // splitContainer.Panel1
        // 
        splitContainer.Panel1.Controls.Add(parameterTabControl);
        // 
        // splitContainer.Panel2
        // 
        splitContainer.Panel2.Controls.Add(previewLayoutPanel);
        splitContainer.Size = new Size(1180, 760);
        splitContainer.SplitterDistance = 450;
        splitContainer.TabIndex = 0;
        // 
        // parameterTabControl
        // 
        parameterTabControl.Controls.Add(settingsTabPage);
        parameterTabControl.Controls.Add(polygonTabPage);
        parameterTabControl.Dock = DockStyle.Fill;
        parameterTabControl.Location = new Point(0, 0);
        parameterTabControl.Name = "parameterTabControl";
        parameterTabControl.SelectedIndex = 0;
        parameterTabControl.Size = new Size(450, 760);
        parameterTabControl.TabIndex = 0;
        // 
        // settingsTabPage
        // 
        settingsTabPage.Controls.Add(_propertyGrid);
        settingsTabPage.Location = new Point(4, 34);
        settingsTabPage.Name = "settingsTabPage";
        settingsTabPage.Padding = new Padding(3);
        settingsTabPage.Size = new Size(442, 722);
        settingsTabPage.TabIndex = 0;
        settingsTabPage.Text = "尺寸與參數";
        settingsTabPage.UseVisualStyleBackColor = true;
        // 
        // _propertyGrid
        // 
        _propertyGrid.Dock = DockStyle.Fill;
        _propertyGrid.Location = new Point(3, 3);
        _propertyGrid.Name = "_propertyGrid";
        _propertyGrid.Size = new Size(436, 716);
        _propertyGrid.TabIndex = 0;
        _propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;
        // 
        // polygonTabPage
        // 
        polygonTabPage.Controls.Add(polygonEditorPanel);
        polygonTabPage.Location = new Point(4, 32);
        polygonTabPage.Name = "polygonTabPage";
        polygonTabPage.Padding = new Padding(3);
        polygonTabPage.Size = new Size(442, 724);
        polygonTabPage.TabIndex = 1;
        polygonTabPage.Text = "自訂 Polygon";
        polygonTabPage.UseVisualStyleBackColor = true;
        // 
        // polygonEditorPanel
        // 
        polygonEditorPanel.Controls.Add(_polygonGrid);
        polygonEditorPanel.Controls.Add(polygonNoteLabel);
        polygonEditorPanel.Dock = DockStyle.Fill;
        polygonEditorPanel.Location = new Point(3, 3);
        polygonEditorPanel.Name = "polygonEditorPanel";
        polygonEditorPanel.Size = new Size(436, 718);
        polygonEditorPanel.TabIndex = 0;
        // 
        // _polygonGrid
        // 
        _polygonGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _polygonGrid.ColumnHeadersHeight = 34;
        _polygonGrid.Columns.AddRange(new DataGridViewColumn[] { polygonXColumn, polygonYColumn });
        _polygonGrid.Dock = DockStyle.Fill;
        _polygonGrid.Location = new Point(0, 58);
        _polygonGrid.Name = "_polygonGrid";
        _polygonGrid.RowHeadersWidth = 62;
        _polygonGrid.Size = new Size(436, 660);
        _polygonGrid.TabIndex = 1;
        _polygonGrid.CellValueChanged += PolygonGrid_CellValueChanged;
        _polygonGrid.RowsRemoved += PolygonGrid_RowsRemoved;
        _polygonGrid.UserAddedRow += PolygonGrid_UserAddedRow;
        // 
        // polygonXColumn
        // 
        polygonXColumn.HeaderText = "X";
        polygonXColumn.Name = "polygonXColumn";
        // 
        // polygonYColumn
        // 
        polygonYColumn.HeaderText = "Y";
        polygonYColumn.Name = "polygonYColumn";
        // 
        // polygonNoteLabel
        // 
        polygonNoteLabel.Dock = DockStyle.Top;
        polygonNoteLabel.Location = new Point(0, 0);
        polygonNoteLabel.Name = "polygonNoteLabel";
        polygonNoteLabel.Padding = new Padding(8);
        polygonNoteLabel.Size = new Size(436, 58);
        polygonNoteLabel.TabIndex = 0;
        polygonNoteLabel.Text = "頂點請依輪廓順序輸入，目前支援凸多邊形。邊索引為頂點列至下一列，進出水口以相同索引指定。";
        // 
        // previewLayoutPanel
        // 
        previewLayoutPanel.ColumnCount = 1;
        previewLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        previewLayoutPanel.Controls.Add(_preview, 0, 0);
        previewLayoutPanel.Controls.Add(_validationLabel, 0, 1);
        previewLayoutPanel.Controls.Add(commandFlowLayoutPanel, 0, 2);
        previewLayoutPanel.Dock = DockStyle.Fill;
        previewLayoutPanel.Location = new Point(0, 0);
        previewLayoutPanel.Name = "previewLayoutPanel";
        previewLayoutPanel.RowCount = 3;
        previewLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        previewLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        previewLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        previewLayoutPanel.Size = new Size(726, 760);
        previewLayoutPanel.TabIndex = 0;
        // 
        // _preview
        // 
        _preview.BackColor = Color.FromArgb(28, 31, 36);
        _preview.Dock = DockStyle.Fill;
        _preview.Location = new Point(3, 3);
        _preview.Model = null;
        _preview.Name = "_preview";
        _preview.Size = new Size(720, 658);
        _preview.TabIndex = 0;
        // 
        // _validationLabel
        // 
        _validationLabel.AutoEllipsis = true;
        _validationLabel.Dock = DockStyle.Fill;
        _validationLabel.Location = new Point(3, 664);
        _validationLabel.Name = "_validationLabel";
        _validationLabel.Size = new Size(720, 42);
        _validationLabel.TabIndex = 1;
        _validationLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // commandFlowLayoutPanel
        // 
        commandFlowLayoutPanel.Controls.Add(cancelButton);
        commandFlowLayoutPanel.Controls.Add(_createButton);
        commandFlowLayoutPanel.Controls.Add(resetButton);
        commandFlowLayoutPanel.Dock = DockStyle.Fill;
        commandFlowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
        commandFlowLayoutPanel.Location = new Point(3, 709);
        commandFlowLayoutPanel.Name = "commandFlowLayoutPanel";
        commandFlowLayoutPanel.Padding = new Padding(8);
        commandFlowLayoutPanel.Size = new Size(720, 48);
        commandFlowLayoutPanel.TabIndex = 2;
        commandFlowLayoutPanel.WrapContents = false;
        // 
        // cancelButton
        // 
        cancelButton.AutoSize = true;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(626, 11);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 35);
        cancelButton.TabIndex = 0;
        cancelButton.Text = "取消";
        // 
        // _createButton
        // 
        _createButton.AutoSize = true;
        _createButton.Location = new Point(458, 11);
        _createButton.Name = "_createButton";
        _createButton.Size = new Size(162, 35);
        _createButton.TabIndex = 1;
        _createButton.Text = "建立並加入場景";
        _createButton.Click += CreateButton_Click;
        // 
        // resetButton
        // 
        resetButton.AutoSize = true;
        resetButton.Location = new Point(330, 11);
        resetButton.Name = "resetButton";
        resetButton.Size = new Size(122, 35);
        resetButton.TabIndex = 2;
        resetButton.Text = "重設預設值";
        resetButton.Click += ResetButton_Click;
        // 
        // ColdPlateBuilderForm
        // 
        AcceptButton = _createButton;
        CancelButton = cancelButton;
        ClientSize = new Size(1180, 760);
        Controls.Add(splitContainer);
        Controls.Add(unifiedMenuStrip);
        MainMenuStrip = unifiedMenuStrip;
        Font = new Font("Microsoft JhengHei UI", 10F);
        Icon = resources.GetObject("$this.Icon") as Icon;
        MinimumSize = new Size(960, 650);
        Name = "ColdPlateBuilderForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "建立冷板模型";
        splitContainer.Panel1.ResumeLayout(false);
        splitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
        splitContainer.ResumeLayout(false);
        parameterTabControl.ResumeLayout(false);
        settingsTabPage.ResumeLayout(false);
        polygonTabPage.ResumeLayout(false);
        polygonEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_polygonGrid).EndInit();
        previewLayoutPanel.ResumeLayout(false);
        commandFlowLayoutPanel.ResumeLayout(false);
        commandFlowLayoutPanel.PerformLayout();
        ResumeLayout(false);
    }
}
