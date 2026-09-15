#nullable enable
namespace Rv3dViewer.MapPlugin;

partial class MapEditorForm
{
    private System.ComponentModel.IContainer? components = null;
    private Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip unifiedMenuStrip = null!;
    private SplitContainer mainSplitContainer = null!;
    private OpenTK.GLControl.GLControl previewGlControl = null!;
    private TableLayoutPanel editorLayoutPanel = null!;
    private Label targetLabel = null!;
    private ComboBox semanticComboBox = null!;
    private CheckBox stackEnabledCheckBox = null!;
    private ListBox layerListBox = null!;
    private FlowLayoutPanel addLayerFlowPanel = null!;
    private Button addImageButton = null!;
    private Button addColorButton = null!;
    private Button addCheckerButton = null!;
    private Button addNoiseButton = null!;
    private Button loadPbrSetButton = null!;
    private FlowLayoutPanel layerCommandFlowPanel = null!;
    private Button duplicateLayerButton = null!;
    private Button removeLayerButton = null!;
    private Button moveLayerUpButton = null!;
    private Button moveLayerDownButton = null!;
    private PropertyGrid layerPropertyGrid = null!;
    private FlowLayoutPanel assetCommandFlowPanel = null!;
    private Button chooseImageButton = null!;
    private Button chooseColorButton = null!;
    private Button chooseMaskButton = null!;
    private Button clearMaskButton = null!;
    private Button bakeButton = null!;
    private Label navigationLabel = null!;
    private FlowLayoutPanel bottomCommandFlowPanel = null!;
    private Button fitMeshButton = null!;
    private Button undoButton = null!;
    private Button redoButton = null!;
    private Button resetStackButton = null!;
    private Button applyButton = null!;
    private Button okButton = null!;
    private Button cancelButton = null!;
    private OpenFileDialog imageOpenFileDialog = null!;
    private SaveFileDialog bakeSaveFileDialog = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        unifiedMenuStrip = new Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip();
        components = new System.ComponentModel.Container();
        mainSplitContainer = new SplitContainer();
        previewGlControl = new OpenTK.GLControl.GLControl();
        editorLayoutPanel = new TableLayoutPanel();
        targetLabel = new Label();
        semanticComboBox = new ComboBox();
        stackEnabledCheckBox = new CheckBox();
        layerListBox = new ListBox();
        addLayerFlowPanel = new FlowLayoutPanel();
        addImageButton = new Button();
        addColorButton = new Button();
        addCheckerButton = new Button();
        addNoiseButton = new Button();
        loadPbrSetButton = new Button();
        layerCommandFlowPanel = new FlowLayoutPanel();
        duplicateLayerButton = new Button();
        removeLayerButton = new Button();
        moveLayerUpButton = new Button();
        moveLayerDownButton = new Button();
        layerPropertyGrid = new PropertyGrid();
        assetCommandFlowPanel = new FlowLayoutPanel();
        chooseImageButton = new Button();
        chooseColorButton = new Button();
        chooseMaskButton = new Button();
        clearMaskButton = new Button();
        bakeButton = new Button();
        navigationLabel = new Label();
        bottomCommandFlowPanel = new FlowLayoutPanel();
        fitMeshButton = new Button();
        undoButton = new Button();
        redoButton = new Button();
        resetStackButton = new Button();
        applyButton = new Button();
        okButton = new Button();
        cancelButton = new Button();
        imageOpenFileDialog = new OpenFileDialog();
        bakeSaveFileDialog = new SaveFileDialog();
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
        mainSplitContainer.Panel1.SuspendLayout();
        mainSplitContainer.Panel2.SuspendLayout();
        mainSplitContainer.SuspendLayout();
        editorLayoutPanel.SuspendLayout();
        addLayerFlowPanel.SuspendLayout();
        layerCommandFlowPanel.SuspendLayout();
        assetCommandFlowPanel.SuspendLayout();
        bottomCommandFlowPanel.SuspendLayout();
        SuspendLayout();
        // mainSplitContainer
        mainSplitContainer.Dock = DockStyle.Fill;
        mainSplitContainer.FixedPanel = FixedPanel.Panel2;
        mainSplitContainer.Location = new Point(0, 0);
        mainSplitContainer.Name = "mainSplitContainer";
        mainSplitContainer.Panel1.Controls.Add(previewGlControl);
        mainSplitContainer.Panel2.Controls.Add(editorLayoutPanel);
        mainSplitContainer.Panel2MinSize = 430;
        mainSplitContainer.Size = new Size(1280, 800);
        mainSplitContainer.SplitterDistance = 842;
        mainSplitContainer.TabIndex = 0;
        // previewGlControl
        previewGlControl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
        previewGlControl.APIVersion = new Version(3, 3, 0, 0);
        previewGlControl.BackColor = Color.Black;
        previewGlControl.Dock = DockStyle.Fill;
        previewGlControl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
        previewGlControl.IsEventDriven = true;
        previewGlControl.Location = new Point(0, 0);
        previewGlControl.Name = "previewGlControl";
        previewGlControl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
        previewGlControl.SharedContext = null;
        previewGlControl.Size = new Size(842, 800);
        previewGlControl.TabIndex = 0;
        // editorLayoutPanel
        editorLayoutPanel.ColumnCount = 1;
        editorLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        editorLayoutPanel.Controls.Add(targetLabel, 0, 0);
        editorLayoutPanel.Controls.Add(semanticComboBox, 0, 1);
        editorLayoutPanel.Controls.Add(stackEnabledCheckBox, 0, 2);
        editorLayoutPanel.Controls.Add(layerListBox, 0, 3);
        editorLayoutPanel.Controls.Add(addLayerFlowPanel, 0, 4);
        editorLayoutPanel.Controls.Add(layerCommandFlowPanel, 0, 5);
        editorLayoutPanel.Controls.Add(layerPropertyGrid, 0, 6);
        editorLayoutPanel.Controls.Add(assetCommandFlowPanel, 0, 7);
        editorLayoutPanel.Controls.Add(navigationLabel, 0, 8);
        editorLayoutPanel.Controls.Add(bottomCommandFlowPanel, 0, 9);
        editorLayoutPanel.Dock = DockStyle.Fill;
        editorLayoutPanel.Padding = new Padding(8);
        editorLayoutPanel.RowCount = 10;
        editorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        editorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        editorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        editorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
        editorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        editorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        editorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 68F));
        editorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        editorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        editorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        editorLayoutPanel.Size = new Size(434, 800);
        editorLayoutPanel.TabIndex = 0;
        // targetLabel
        targetLabel.AutoEllipsis = true;
        targetLabel.Dock = DockStyle.Fill;
        targetLabel.Name = "targetLabel";
        targetLabel.Text = "Mesh";
        targetLabel.TextAlign = ContentAlignment.MiddleLeft;
        // semanticComboBox
        semanticComboBox.Dock = DockStyle.Fill;
        semanticComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        semanticComboBox.Name = "semanticComboBox";
        semanticComboBox.SelectedIndexChanged += SemanticComboBox_SelectedIndexChanged;
        // stackEnabledCheckBox
        stackEnabledCheckBox.AutoSize = true;
        stackEnabledCheckBox.Dock = DockStyle.Fill;
        stackEnabledCheckBox.Name = "stackEnabledCheckBox";
        stackEnabledCheckBox.Text = "啟用目前貼圖堆疊";
        stackEnabledCheckBox.CheckedChanged += StackEnabledCheckBox_CheckedChanged;
        // layerListBox
        layerListBox.Dock = DockStyle.Fill;
        layerListBox.FormattingEnabled = true;
        layerListBox.Name = "layerListBox";
        layerListBox.SelectedIndexChanged += LayerListBox_SelectedIndexChanged;
        // addLayerFlowPanel
        addLayerFlowPanel.Controls.Add(addImageButton);
        addLayerFlowPanel.Controls.Add(addColorButton);
        addLayerFlowPanel.Controls.Add(addCheckerButton);
        addLayerFlowPanel.Controls.Add(addNoiseButton);
        addLayerFlowPanel.Controls.Add(loadPbrSetButton);
        addLayerFlowPanel.Dock = DockStyle.Fill;
        addLayerFlowPanel.Name = "addLayerFlowPanel";
        addLayerFlowPanel.WrapContents = false;
        // addImageButton
        addImageButton.AutoSize = true;
        addImageButton.Name = "addImageButton";
        addImageButton.Text = "+ 圖片";
        addImageButton.Click += AddImageButton_Click;
        // addColorButton
        addColorButton.AutoSize = true;
        addColorButton.Name = "addColorButton";
        addColorButton.Text = "+ 純色";
        addColorButton.Click += AddColorButton_Click;
        // addCheckerButton
        addCheckerButton.AutoSize = true;
        addCheckerButton.Name = "addCheckerButton";
        addCheckerButton.Text = "+ Checker";
        addCheckerButton.Click += AddCheckerButton_Click;
        // addNoiseButton
        addNoiseButton.AutoSize = true;
        addNoiseButton.Name = "addNoiseButton";
        addNoiseButton.Text = "+ Noise";
        addNoiseButton.Click += AddNoiseButton_Click;
        // loadPbrSetButton
        loadPbrSetButton.AutoSize = true;
        loadPbrSetButton.Name = "loadPbrSetButton";
        loadPbrSetButton.Text = "載入 PBR 組";
        loadPbrSetButton.Click += LoadPbrSetButton_Click;
        // layerCommandFlowPanel
        layerCommandFlowPanel.Controls.Add(duplicateLayerButton);
        layerCommandFlowPanel.Controls.Add(removeLayerButton);
        layerCommandFlowPanel.Controls.Add(moveLayerUpButton);
        layerCommandFlowPanel.Controls.Add(moveLayerDownButton);
        layerCommandFlowPanel.Dock = DockStyle.Fill;
        layerCommandFlowPanel.Name = "layerCommandFlowPanel";
        layerCommandFlowPanel.WrapContents = false;
        // duplicateLayerButton
        duplicateLayerButton.AutoSize = true;
        duplicateLayerButton.Name = "duplicateLayerButton";
        duplicateLayerButton.Text = "複製";
        duplicateLayerButton.Click += DuplicateLayerButton_Click;
        // removeLayerButton
        removeLayerButton.AutoSize = true;
        removeLayerButton.Name = "removeLayerButton";
        removeLayerButton.Text = "刪除";
        removeLayerButton.Click += RemoveLayerButton_Click;
        // moveLayerUpButton
        moveLayerUpButton.AutoSize = true;
        moveLayerUpButton.Name = "moveLayerUpButton";
        moveLayerUpButton.Text = "上移";
        moveLayerUpButton.Click += MoveLayerUpButton_Click;
        // moveLayerDownButton
        moveLayerDownButton.AutoSize = true;
        moveLayerDownButton.Name = "moveLayerDownButton";
        moveLayerDownButton.Text = "下移";
        moveLayerDownButton.Click += MoveLayerDownButton_Click;
        // layerPropertyGrid
        layerPropertyGrid.Dock = DockStyle.Fill;
        layerPropertyGrid.Name = "layerPropertyGrid";
        layerPropertyGrid.PropertySort = PropertySort.Categorized;
        layerPropertyGrid.ToolbarVisible = false;
        layerPropertyGrid.PropertyValueChanged += LayerPropertyGrid_PropertyValueChanged;
        // assetCommandFlowPanel
        assetCommandFlowPanel.Controls.Add(chooseImageButton);
        assetCommandFlowPanel.Controls.Add(chooseColorButton);
        assetCommandFlowPanel.Controls.Add(chooseMaskButton);
        assetCommandFlowPanel.Controls.Add(clearMaskButton);
        assetCommandFlowPanel.Controls.Add(bakeButton);
        assetCommandFlowPanel.Dock = DockStyle.Fill;
        assetCommandFlowPanel.Name = "assetCommandFlowPanel";
        assetCommandFlowPanel.WrapContents = false;
        // chooseImageButton
        chooseImageButton.AutoSize = true;
        chooseImageButton.Name = "chooseImageButton";
        chooseImageButton.Text = "更換圖";
        chooseImageButton.Click += ChooseImageButton_Click;
        // chooseColorButton
        chooseColorButton.AutoSize = true;
        chooseColorButton.Name = "chooseColorButton";
        chooseColorButton.Text = "顏色";
        chooseColorButton.Click += ChooseColorButton_Click;
        // chooseMaskButton
        chooseMaskButton.AutoSize = true;
        chooseMaskButton.Name = "chooseMaskButton";
        chooseMaskButton.Text = "Mask";
        chooseMaskButton.Click += ChooseMaskButton_Click;
        // clearMaskButton
        clearMaskButton.AutoSize = true;
        clearMaskButton.Name = "clearMaskButton";
        clearMaskButton.Text = "清除 Mask";
        clearMaskButton.Click += ClearMaskButton_Click;
        // bakeButton
        bakeButton.AutoSize = true;
        bakeButton.Name = "bakeButton";
        bakeButton.Text = "烘焙";
        bakeButton.Click += BakeButton_Click;
        // navigationLabel
        navigationLabel.Dock = DockStyle.Fill;
        navigationLabel.ForeColor = SystemColors.GrayText;
        navigationLabel.Name = "navigationLabel";
        navigationLabel.Text = "左鍵 Orbit　中鍵 Target　右鍵 Pan　滾輪 FOV";
        navigationLabel.TextAlign = ContentAlignment.MiddleLeft;
        // bottomCommandFlowPanel
        bottomCommandFlowPanel.Controls.Add(fitMeshButton);
        bottomCommandFlowPanel.Controls.Add(undoButton);
        bottomCommandFlowPanel.Controls.Add(redoButton);
        bottomCommandFlowPanel.Controls.Add(resetStackButton);
        bottomCommandFlowPanel.Controls.Add(applyButton);
        bottomCommandFlowPanel.Controls.Add(okButton);
        bottomCommandFlowPanel.Controls.Add(cancelButton);
        bottomCommandFlowPanel.Dock = DockStyle.Fill;
        bottomCommandFlowPanel.FlowDirection = FlowDirection.LeftToRight;
        bottomCommandFlowPanel.Name = "bottomCommandFlowPanel";
        bottomCommandFlowPanel.WrapContents = false;
        // fitMeshButton
        fitMeshButton.Margin = new Padding(1, 3, 1, 3);
        fitMeshButton.Name = "fitMeshButton";
        fitMeshButton.Size = new Size(64, 30);
        fitMeshButton.Text = "Fit Mesh";
        fitMeshButton.Click += FitMeshButton_Click;
        // undoButton
        undoButton.Margin = new Padding(1, 3, 1, 3);
        undoButton.Name = "undoButton";
        undoButton.Size = new Size(48, 30);
        undoButton.Text = "Undo";
        undoButton.Click += UndoButton_Click;
        // redoButton
        redoButton.Margin = new Padding(1, 3, 1, 3);
        redoButton.Name = "redoButton";
        redoButton.Size = new Size(48, 30);
        redoButton.Text = "Redo";
        redoButton.Click += RedoButton_Click;
        // resetStackButton
        resetStackButton.Margin = new Padding(1, 3, 1, 3);
        resetStackButton.Name = "resetStackButton";
        resetStackButton.Size = new Size(50, 30);
        resetStackButton.Text = "重設";
        resetStackButton.Click += ResetStackButton_Click;
        // applyButton
        applyButton.Margin = new Padding(1, 3, 1, 3);
        applyButton.Name = "applyButton";
        applyButton.Size = new Size(50, 30);
        applyButton.Text = "套用";
        applyButton.Click += ApplyButton_Click;
        // okButton
        okButton.DialogResult = DialogResult.OK;
        okButton.Margin = new Padding(1, 3, 1, 3);
        okButton.Name = "okButton";
        okButton.Size = new Size(50, 30);
        okButton.Text = "確定";
        okButton.Click += OkButton_Click;
        // cancelButton
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Margin = new Padding(1, 3, 1, 3);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(50, 30);
        cancelButton.Text = "取消";
        // dialogs
        imageOpenFileDialog.Filter = "圖片 (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|所有檔案 (*.*)|*.*";
        bakeSaveFileDialog.DefaultExt = "png";
        bakeSaveFileDialog.Filter = "PNG 圖片 (*.png)|*.png";
        // MapEditorForm
        AcceptButton = okButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(1280, 800);
        Controls.Add(mainSplitContainer);
        Controls.Add(unifiedMenuStrip);
        MainMenuStrip = unifiedMenuStrip;
        MinimumSize = new Size(1000, 650);
        Name = "MapEditorForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "貼圖編輯器";
        FormClosed += MapEditorForm_FormClosed;
        FormClosing += MapEditorForm_FormClosing;
        mainSplitContainer.Panel1.ResumeLayout(false);
        mainSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
        mainSplitContainer.ResumeLayout(false);
        editorLayoutPanel.ResumeLayout(false);
        editorLayoutPanel.PerformLayout();
        addLayerFlowPanel.ResumeLayout(false);
        addLayerFlowPanel.PerformLayout();
        layerCommandFlowPanel.ResumeLayout(false);
        layerCommandFlowPanel.PerformLayout();
        assetCommandFlowPanel.ResumeLayout(false);
        assetCommandFlowPanel.PerformLayout();
        bottomCommandFlowPanel.ResumeLayout(false);
        bottomCommandFlowPanel.PerformLayout();
        ResumeLayout(false);
    }
}
