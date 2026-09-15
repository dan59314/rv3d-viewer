namespace Rv3dViewer.CameraAnimationPlugin;

partial class CameraAnimationForm
{
    private System.ComponentModel.IContainer components = null!;
    private Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip unifiedMenuStrip = null!;
    private TableLayoutPanel rootLayoutPanel = null!;
    private FlowLayoutPanel commandFlowLayoutPanel = null!;
    private Button newButton = null!;
    private Button openButton = null!;
    private Button saveButton = null!;
    private Button saveAsButton = null!;
    private Button captureButton = null!;
    private Button insertKeyframeButton = null!;
    private Button updateKeyframeButton = null!;
    private Button deleteButton = null!;
    private Button undoButton = null!;
    private Button redoButton = null!;
    private CheckBox showCameraCheckBox = null!;
    private CheckBox showTrajectoryCheckBox = null!;
    private CheckBox showAllLightsCheckBox = null!;
    private CheckBox showModelTrajectoryCheckBox = null!;
    private CheckBox wheelChangesFovCheckBox = null!;
    private Label editModeLabel = null!;
    private ComboBox editModeComboBox = null!;
    private SplitContainer editorSplitContainer = null!;
    private TabControl sceneTabControl = null!;
    private TabPage objectTreeTabPage = null!;
    private TreeView sceneTreeView = null!;
    private TabPage keyframeListTabPage = null!;
    private ListBox keyframeListBox = null!;
    private SplitContainer previewSplitContainer = null!;
    private TableLayoutPanel fourViewLayoutPanel = null!;
    private Rv3dViewer.Plugin.WinForms.InputOverlayPictureBox frontPictureBox = null!;
    private Rv3dViewer.Plugin.WinForms.InputOverlayPictureBox leftPictureBox = null!;
    private Rv3dViewer.Plugin.WinForms.InputOverlayPictureBox topPictureBox = null!;
    private Rv3dViewer.Plugin.WinForms.InputOverlayPictureBox previewPictureBox = null!;
    private TableLayoutPanel inspectorLayoutPanel = null!;
    private TabControl inspectorTabControl = null!;
    private TabPage objectTabPage = null!;
    private PropertyGrid objectPropertyGrid = null!;
    private TabPage cameraTabPage = null!;
    private TabPage lightTabPage = null!;
    private TableLayoutPanel lightLayoutPanel = null!;
    private ComboBox lightComboBox = null!;
    private PropertyGrid lightPropertyGrid = null!;
    private PropertyGrid cameraPropertyGrid = null!;
    private PropertyGrid keyframePropertyGrid = null!;
    private Button useMainCameraButton = null!;
    private Button applyMainCameraButton = null!;
    private TableLayoutPanel playbackLayoutPanel = null!;
    private TrackBar timelineTrackBar = null!;
    private FlowLayoutPanel playbackFlowLayoutPanel = null!;
    private Button playButton = null!;
    private Button pauseButton = null!;
    private Button stopButton = null!;
    private Label keyframeTimeLabel = null!;
    private NumericUpDown keyframeTimeNumericUpDown = null!;
    private Label durationLabel = null!;
    private NumericUpDown durationNumericUpDown = null!;
    private Label fpsLabel = null!;
    private NumericUpDown fpsNumericUpDown = null!;
    private CheckBox loopCheckBox = null!;
    private CheckBox restoreCameraCheckBox = null!;
    private CheckBox zoomAtMouseCheckBox = null!;
    private Label currentTimeLabel = null!;
    private FlowLayoutPanel exportFlowLayoutPanel = null!;
    private Button exportFramesButton = null!;
    private Button exportVideoButton = null!;
    private Button cancelExportButton = null!;
    private Label exportWidthLabel = null!;
    private NumericUpDown exportWidthNumericUpDown = null!;
    private Label exportHeightLabel = null!;
    private NumericUpDown exportHeightNumericUpDown = null!;
    private ProgressBar exportProgressBar = null!;
    private Label exportStatusLabel = null!;
    private System.Windows.Forms.Timer playbackTimer = null!;
    private System.Windows.Forms.Timer previewRenderTimer = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        unifiedMenuStrip = new Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip();
        components = new System.ComponentModel.Container();
        rootLayoutPanel = new TableLayoutPanel();
        commandFlowLayoutPanel = new FlowLayoutPanel();
        newButton = new Button();
        openButton = new Button();
        saveButton = new Button();
        saveAsButton = new Button();
        captureButton = new Button();
        insertKeyframeButton = new Button();
        updateKeyframeButton = new Button();
        deleteButton = new Button();
        undoButton = new Button();
        redoButton = new Button();
        showCameraCheckBox = new CheckBox();
        showTrajectoryCheckBox = new CheckBox();
        showAllLightsCheckBox = new CheckBox();
        showModelTrajectoryCheckBox = new CheckBox();
        wheelChangesFovCheckBox = new CheckBox();
        editModeLabel = new Label();
        editModeComboBox = new ComboBox();
        editorSplitContainer = new SplitContainer();
        sceneTabControl = new TabControl();
        objectTreeTabPage = new TabPage();
        sceneTreeView = new TreeView();
        keyframeListTabPage = new TabPage();
        keyframeListBox = new ListBox();
        previewSplitContainer = new SplitContainer();
        fourViewLayoutPanel = new TableLayoutPanel();
        frontPictureBox = new Rv3dViewer.Plugin.WinForms.InputOverlayPictureBox();
        leftPictureBox = new Rv3dViewer.Plugin.WinForms.InputOverlayPictureBox();
        topPictureBox = new Rv3dViewer.Plugin.WinForms.InputOverlayPictureBox();
        previewPictureBox = new Rv3dViewer.Plugin.WinForms.InputOverlayPictureBox();
        inspectorTabControl = new TabControl();
        objectTabPage = new TabPage();
        objectPropertyGrid = new PropertyGrid();
        cameraTabPage = new TabPage();
        inspectorLayoutPanel = new TableLayoutPanel();
        cameraPropertyGrid = new PropertyGrid();
        keyframePropertyGrid = new PropertyGrid();
        useMainCameraButton = new Button();
        applyMainCameraButton = new Button();
        lightTabPage = new TabPage();
        lightLayoutPanel = new TableLayoutPanel();
        lightComboBox = new ComboBox();
        lightPropertyGrid = new PropertyGrid();
        playbackLayoutPanel = new TableLayoutPanel();
        timelineTrackBar = new TrackBar();
        playbackFlowLayoutPanel = new FlowLayoutPanel();
        playButton = new Button();
        pauseButton = new Button();
        stopButton = new Button();
        keyframeTimeLabel = new Label();
        keyframeTimeNumericUpDown = new NumericUpDown();
        durationLabel = new Label();
        durationNumericUpDown = new NumericUpDown();
        fpsLabel = new Label();
        fpsNumericUpDown = new NumericUpDown();
        loopCheckBox = new CheckBox();
        restoreCameraCheckBox = new CheckBox();
        zoomAtMouseCheckBox = new CheckBox();
        currentTimeLabel = new Label();
        exportFlowLayoutPanel = new FlowLayoutPanel();
        exportFramesButton = new Button();
        exportVideoButton = new Button();
        cancelExportButton = new Button();
        exportWidthLabel = new Label();
        exportWidthNumericUpDown = new NumericUpDown();
        exportHeightLabel = new Label();
        exportHeightNumericUpDown = new NumericUpDown();
        exportProgressBar = new ProgressBar();
        exportStatusLabel = new Label();
        playbackTimer = new System.Windows.Forms.Timer(components);
        previewRenderTimer = new System.Windows.Forms.Timer(components);
        rootLayoutPanel.SuspendLayout();
        commandFlowLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)editorSplitContainer).BeginInit();
        editorSplitContainer.Panel1.SuspendLayout();
        editorSplitContainer.Panel2.SuspendLayout();
        editorSplitContainer.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)previewSplitContainer).BeginInit();
        previewSplitContainer.Panel1.SuspendLayout();
        previewSplitContainer.Panel2.SuspendLayout();
        previewSplitContainer.SuspendLayout();
        fourViewLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)frontPictureBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)leftPictureBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)topPictureBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).BeginInit();
        inspectorTabControl.SuspendLayout();
        cameraTabPage.SuspendLayout();
        inspectorLayoutPanel.SuspendLayout();
        lightTabPage.SuspendLayout();
        lightLayoutPanel.SuspendLayout();
        playbackLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)timelineTrackBar).BeginInit();
        playbackFlowLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)keyframeTimeNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)durationNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)fpsNumericUpDown).BeginInit();
        exportFlowLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)exportWidthNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)exportHeightNumericUpDown).BeginInit();
        SuspendLayout();
        // 
        // rootLayoutPanel
        // 
        rootLayoutPanel.ColumnCount = 1;
        rootLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayoutPanel.Controls.Add(commandFlowLayoutPanel, 0, 0);
        rootLayoutPanel.Controls.Add(editorSplitContainer, 0, 1);
        rootLayoutPanel.Controls.Add(playbackLayoutPanel, 0, 2);
        rootLayoutPanel.Dock = DockStyle.Fill;
        rootLayoutPanel.Location = new Point(0, 0);
        rootLayoutPanel.Margin = new Padding(4);
        rootLayoutPanel.Name = "rootLayoutPanel";
        rootLayoutPanel.RowCount = 3;
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 189F));
        rootLayoutPanel.Size = new Size(1848, 918);
        rootLayoutPanel.TabIndex = 0;
        // 
        // commandFlowLayoutPanel
        // 
        commandFlowLayoutPanel.Controls.Add(newButton);
        commandFlowLayoutPanel.Controls.Add(openButton);
        commandFlowLayoutPanel.Controls.Add(saveButton);
        commandFlowLayoutPanel.Controls.Add(saveAsButton);
        commandFlowLayoutPanel.Controls.Add(captureButton);
        commandFlowLayoutPanel.Controls.Add(insertKeyframeButton);
        commandFlowLayoutPanel.Controls.Add(updateKeyframeButton);
        commandFlowLayoutPanel.Controls.Add(deleteButton);
        commandFlowLayoutPanel.Controls.Add(undoButton);
        commandFlowLayoutPanel.Controls.Add(redoButton);
        commandFlowLayoutPanel.Controls.Add(showCameraCheckBox);
        commandFlowLayoutPanel.Controls.Add(showTrajectoryCheckBox);
        commandFlowLayoutPanel.Controls.Add(showAllLightsCheckBox);
        commandFlowLayoutPanel.Controls.Add(showModelTrajectoryCheckBox);
        commandFlowLayoutPanel.Controls.Add(wheelChangesFovCheckBox);
        commandFlowLayoutPanel.Controls.Add(editModeLabel);
        commandFlowLayoutPanel.Controls.Add(editModeComboBox);
        commandFlowLayoutPanel.Dock = DockStyle.Fill;
        commandFlowLayoutPanel.Location = new Point(4, 4);
        commandFlowLayoutPanel.Margin = new Padding(4);
        commandFlowLayoutPanel.Name = "commandFlowLayoutPanel";
        commandFlowLayoutPanel.Size = new Size(1840, 88);
        commandFlowLayoutPanel.TabIndex = 0;
        // 
        // newButton
        // 
        newButton.Location = new Point(4, 4);
        newButton.Margin = new Padding(4);
        newButton.Name = "newButton";
        newButton.Size = new Size(84, 36);
        newButton.TabIndex = 0;
        newButton.Text = "新增";
        newButton.UseVisualStyleBackColor = true;
        newButton.Click += NewButton_Click;
        // 
        // openButton
        // 
        openButton.Location = new Point(96, 4);
        openButton.Margin = new Padding(4);
        openButton.Name = "openButton";
        openButton.Size = new Size(96, 36);
        openButton.TabIndex = 1;
        openButton.Text = "開啟…";
        openButton.UseVisualStyleBackColor = true;
        openButton.Click += OpenButton_Click;
        // 
        // saveButton
        // 
        saveButton.Location = new Point(200, 4);
        saveButton.Margin = new Padding(4);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(76, 36);
        saveButton.TabIndex = 2;
        saveButton.Text = "儲存";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += SaveButton_Click;
        // 
        // saveAsButton
        // 
        saveAsButton.Location = new Point(284, 4);
        saveAsButton.Margin = new Padding(4);
        saveAsButton.Name = "saveAsButton";
        saveAsButton.Size = new Size(117, 36);
        saveAsButton.TabIndex = 3;
        saveAsButton.Text = "另存新檔…";
        saveAsButton.UseVisualStyleBackColor = true;
        saveAsButton.Click += SaveAsButton_Click;
        // 
        // captureButton
        // 
        captureButton.Location = new Point(409, 4);
        captureButton.Margin = new Padding(4);
        captureButton.Name = "captureButton";
        captureButton.Size = new Size(144, 36);
        captureButton.TabIndex = 4;
        captureButton.Text = "加入關鍵影格";
        captureButton.UseVisualStyleBackColor = true;
        captureButton.Click += CaptureButton_Click;
        // 
        // insertKeyframeButton
        // 
        insertKeyframeButton.Enabled = false;
        insertKeyframeButton.Location = new Point(561, 4);
        insertKeyframeButton.Margin = new Padding(4);
        insertKeyframeButton.Name = "insertKeyframeButton";
        insertKeyframeButton.Size = new Size(144, 36);
        insertKeyframeButton.TabIndex = 5;
        insertKeyframeButton.Text = "插入關鍵影格";
        insertKeyframeButton.UseVisualStyleBackColor = true;
        insertKeyframeButton.Click += InsertKeyframeButton_Click;
        // 
        // updateKeyframeButton
        // 
        updateKeyframeButton.Enabled = false;
        updateKeyframeButton.Location = new Point(713, 4);
        updateKeyframeButton.Margin = new Padding(4);
        updateKeyframeButton.Name = "updateKeyframeButton";
        updateKeyframeButton.Size = new Size(144, 36);
        updateKeyframeButton.TabIndex = 5;
        updateKeyframeButton.Text = "更新選取影格";
        updateKeyframeButton.UseVisualStyleBackColor = true;
        updateKeyframeButton.Click += UpdateKeyframeButton_Click;
        // 
        // deleteButton
        // 
        deleteButton.Enabled = false;
        deleteButton.Location = new Point(865, 4);
        deleteButton.Margin = new Padding(4);
        deleteButton.Name = "deleteButton";
        deleteButton.Size = new Size(96, 36);
        deleteButton.TabIndex = 6;
        deleteButton.Text = "刪除影格";
        deleteButton.UseVisualStyleBackColor = true;
        deleteButton.Click += DeleteButton_Click;
        // 
        // undoButton
        // 
        undoButton.Location = new Point(969, 4);
        undoButton.Margin = new Padding(4);
        undoButton.Name = "undoButton";
        undoButton.Size = new Size(58, 36);
        undoButton.TabIndex = 8;
        undoButton.Text = "Undo";
        undoButton.UseVisualStyleBackColor = true;
        undoButton.Click += UndoButton_Click;
        // 
        // redoButton
        // 
        redoButton.Location = new Point(1035, 4);
        redoButton.Margin = new Padding(4);
        redoButton.Name = "redoButton";
        redoButton.Size = new Size(58, 36);
        redoButton.TabIndex = 9;
        redoButton.Text = "Redo";
        redoButton.UseVisualStyleBackColor = true;
        redoButton.Click += RedoButton_Click;
        // 
        // showCameraCheckBox
        // 
        showCameraCheckBox.AutoSize = true;
        showCameraCheckBox.Checked = true;
        showCameraCheckBox.CheckState = CheckState.Checked;
        showCameraCheckBox.Location = new Point(1109, 8);
        showCameraCheckBox.Margin = new Padding(12, 8, 4, 4);
        showCameraCheckBox.Name = "showCameraCheckBox";
        showCameraCheckBox.Size = new Size(102, 27);
        showCameraCheckBox.TabIndex = 7;
        showCameraCheckBox.Text = "Camera";
        showCameraCheckBox.UseVisualStyleBackColor = true;
        showCameraCheckBox.CheckedChanged += DisplayOverlayCheckBox_CheckedChanged;
        // 
        // showTrajectoryCheckBox
        // 
        showTrajectoryCheckBox.AutoSize = true;
        showTrajectoryCheckBox.Checked = true;
        showTrajectoryCheckBox.CheckState = CheckState.Checked;
        showTrajectoryCheckBox.Location = new Point(1227, 8);
        showTrajectoryCheckBox.Margin = new Padding(12, 8, 4, 4);
        showTrajectoryCheckBox.Name = "showTrajectoryCheckBox";
        showTrajectoryCheckBox.Size = new Size(144, 27);
        showTrajectoryCheckBox.TabIndex = 8;
        showTrajectoryCheckBox.Text = "所有影格路徑";
        showTrajectoryCheckBox.UseVisualStyleBackColor = true;
        showTrajectoryCheckBox.CheckedChanged += DisplayOverlayCheckBox_CheckedChanged;
        // 
        // showAllLightsCheckBox
        // 
        showAllLightsCheckBox.AutoSize = true;
        showAllLightsCheckBox.Checked = true;
        showAllLightsCheckBox.CheckState = CheckState.Checked;
        showAllLightsCheckBox.Location = new Point(1387, 8);
        showAllLightsCheckBox.Margin = new Padding(12, 8, 4, 4);
        showAllLightsCheckBox.Name = "showAllLightsCheckBox";
        showAllLightsCheckBox.Size = new Size(144, 27);
        showAllLightsCheckBox.TabIndex = 9;
        showAllLightsCheckBox.Text = "顯示所有燈光";
        showAllLightsCheckBox.UseVisualStyleBackColor = true;
        showAllLightsCheckBox.CheckedChanged += ShowAllLightsCheckBox_CheckedChanged;
        // 
        // showModelTrajectoryCheckBox
        // 
        showModelTrajectoryCheckBox.AutoSize = true;
        showModelTrajectoryCheckBox.Checked = true;
        showModelTrajectoryCheckBox.CheckState = CheckState.Checked;
        showModelTrajectoryCheckBox.Margin = new Padding(12, 8, 4, 4);
        showModelTrajectoryCheckBox.Name = "showModelTrajectoryCheckBox";
        showModelTrajectoryCheckBox.Size = new Size(144, 27);
        showModelTrajectoryCheckBox.TabIndex = 10;
        showModelTrajectoryCheckBox.Text = "顯示模型路徑";
        showModelTrajectoryCheckBox.UseVisualStyleBackColor = true;
        showModelTrajectoryCheckBox.CheckedChanged += DisplayOverlayCheckBox_CheckedChanged;
        // 
        // wheelChangesFovCheckBox
        // 
        wheelChangesFovCheckBox.AutoSize = true;
        wheelChangesFovCheckBox.Location = new Point(1547, 8);
        wheelChangesFovCheckBox.Margin = new Padding(12, 8, 4, 4);
        wheelChangesFovCheckBox.Name = "wheelChangesFovCheckBox";
        wheelChangesFovCheckBox.Size = new Size(149, 27);
        wheelChangesFovCheckBox.TabIndex = 9;
        wheelChangesFovCheckBox.Text = "滾輪更改 FOV";
        wheelChangesFovCheckBox.UseVisualStyleBackColor = true;
        // 
        // editModeLabel
        // 
        editModeLabel.AutoSize = true;
        editModeLabel.Location = new Point(1712, 11);
        editModeLabel.Margin = new Padding(12, 11, 4, 0);
        editModeLabel.Name = "editModeLabel";
        editModeLabel.Size = new Size(82, 23);
        editModeLabel.TabIndex = 10;
        editModeLabel.Text = "編輯模式";
        // 
        // editModeComboBox
        // 
        editModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        editModeComboBox.FormattingEnabled = true;
        editModeComboBox.Items.AddRange(new object[] { "非編輯模式", "Camera Position / Target 路徑", "燈光位置", "模型位置 / 路徑" });
        editModeComboBox.Location = new Point(4, 50);
        editModeComboBox.Margin = new Padding(4, 6, 4, 4);
        editModeComboBox.Name = "editModeComboBox";
        editModeComboBox.Size = new Size(190, 31);
        editModeComboBox.TabIndex = 11;
        editModeComboBox.SelectedIndexChanged += EditModeComboBox_SelectedIndexChanged;
        // 
        // editorSplitContainer
        // 
        editorSplitContainer.Dock = DockStyle.Fill;
        editorSplitContainer.Location = new Point(4, 100);
        editorSplitContainer.Margin = new Padding(4);
        editorSplitContainer.Name = "editorSplitContainer";
        // 
        // editorSplitContainer.Panel1
        // 
        editorSplitContainer.Panel1.Controls.Add(sceneTabControl);
        // 
        // editorSplitContainer.Panel2
        // 
        editorSplitContainer.Panel2.Controls.Add(previewSplitContainer);
        editorSplitContainer.Size = new Size(1840, 625);
        editorSplitContainer.SplitterDistance = 319;
        editorSplitContainer.SplitterWidth = 6;
        editorSplitContainer.TabIndex = 1;
        // 
        // sceneTabControl
        // 
        sceneTabControl.Controls.Add(objectTreeTabPage);
        sceneTabControl.Controls.Add(keyframeListTabPage);
        sceneTabControl.Dock = DockStyle.Fill;
        sceneTabControl.Name = "sceneTabControl";
        sceneTabControl.SelectedIndex = 1;
        sceneTabControl.TabIndex = 0;
        // 
        // objectTreeTabPage
        // 
        objectTreeTabPage.Controls.Add(sceneTreeView);
        objectTreeTabPage.Name = "objectTreeTabPage";
        objectTreeTabPage.Padding = new Padding(3);
        objectTreeTabPage.Text = "物件";
        objectTreeTabPage.UseVisualStyleBackColor = true;
        // 
        // sceneTreeView
        // 
        sceneTreeView.CheckBoxes = true;
        sceneTreeView.Dock = DockStyle.Fill;
        sceneTreeView.HideSelection = false;
        sceneTreeView.Name = "sceneTreeView";
        sceneTreeView.AfterCheck += SceneTreeView_AfterCheck;
        sceneTreeView.AfterSelect += SceneTreeView_AfterSelect;
        // 
        // keyframeListTabPage
        // 
        keyframeListTabPage.Controls.Add(keyframeListBox);
        keyframeListTabPage.Name = "keyframeListTabPage";
        keyframeListTabPage.Padding = new Padding(3);
        keyframeListTabPage.Text = "關鍵影格";
        keyframeListTabPage.UseVisualStyleBackColor = true;
        // 
        // keyframeListBox
        // 
        keyframeListBox.Dock = DockStyle.Fill;
        keyframeListBox.Font = new Font("Consolas", 10F);
        keyframeListBox.FormattingEnabled = true;
        keyframeListBox.IntegralHeight = false;
        keyframeListBox.Location = new Point(0, 0);
        keyframeListBox.Margin = new Padding(4);
        keyframeListBox.Name = "keyframeListBox";
        keyframeListBox.Size = new Size(319, 625);
        keyframeListBox.TabIndex = 0;
        keyframeListBox.SelectedIndexChanged += KeyframeListBox_SelectedIndexChanged;
        // 
        // previewSplitContainer
        // 
        previewSplitContainer.Dock = DockStyle.Fill;
        previewSplitContainer.FixedPanel = FixedPanel.Panel2;
        previewSplitContainer.Location = new Point(0, 0);
        previewSplitContainer.Margin = new Padding(4);
        previewSplitContainer.Name = "previewSplitContainer";
        // 
        // previewSplitContainer.Panel1
        // 
        previewSplitContainer.Panel1.Controls.Add(fourViewLayoutPanel);
        // 
        // previewSplitContainer.Panel2
        // 
        previewSplitContainer.Panel2.Controls.Add(inspectorTabControl);
        previewSplitContainer.Size = new Size(1515, 625);
        previewSplitContainer.SplitterDistance = 1098;
        previewSplitContainer.SplitterWidth = 6;
        previewSplitContainer.TabIndex = 0;
        // 
        // fourViewLayoutPanel
        // 
        fourViewLayoutPanel.ColumnCount = 2;
        fourViewLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        fourViewLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        fourViewLayoutPanel.Controls.Add(frontPictureBox, 0, 0);
        fourViewLayoutPanel.Controls.Add(leftPictureBox, 1, 0);
        fourViewLayoutPanel.Controls.Add(topPictureBox, 0, 1);
        fourViewLayoutPanel.Controls.Add(previewPictureBox, 1, 1);
        fourViewLayoutPanel.Dock = DockStyle.Fill;
        fourViewLayoutPanel.Location = new Point(0, 0);
        fourViewLayoutPanel.Margin = new Padding(4);
        fourViewLayoutPanel.Name = "fourViewLayoutPanel";
        fourViewLayoutPanel.RowCount = 2;
        fourViewLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        fourViewLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        fourViewLayoutPanel.Size = new Size(1098, 625);
        fourViewLayoutPanel.TabIndex = 0;
        // 
        // frontPictureBox
        // 
        frontPictureBox.BackColor = Color.Black;
        frontPictureBox.Dock = DockStyle.Fill;
        frontPictureBox.Location = new Point(0, 0);
        frontPictureBox.Margin = new Padding(0, 0, 3, 2);
        frontPictureBox.Name = "frontPictureBox";
        frontPictureBox.Size = new Size(546, 310);
        frontPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        frontPictureBox.TabIndex = 0;
        frontPictureBox.TabStop = false;
        frontPictureBox.MouseDown += OrthographicPictureBox_MouseDown;
        frontPictureBox.MouseMove += OrthographicPictureBox_MouseMove;
        frontPictureBox.MouseUp += OrthographicPictureBox_MouseUp;
        frontPictureBox.MouseWheel += OrthographicPictureBox_MouseWheel;
        frontPictureBox.Resize += PreviewPictureBox_Resize;
        // 
        // leftPictureBox
        // 
        leftPictureBox.BackColor = Color.Black;
        leftPictureBox.Dock = DockStyle.Fill;
        leftPictureBox.Location = new Point(552, 0);
        leftPictureBox.Margin = new Padding(3, 0, 0, 2);
        leftPictureBox.Name = "leftPictureBox";
        leftPictureBox.Size = new Size(546, 310);
        leftPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        leftPictureBox.TabIndex = 1;
        leftPictureBox.TabStop = false;
        leftPictureBox.MouseDown += OrthographicPictureBox_MouseDown;
        leftPictureBox.MouseMove += OrthographicPictureBox_MouseMove;
        leftPictureBox.MouseUp += OrthographicPictureBox_MouseUp;
        leftPictureBox.MouseWheel += OrthographicPictureBox_MouseWheel;
        leftPictureBox.Resize += PreviewPictureBox_Resize;
        // 
        // topPictureBox
        // 
        topPictureBox.BackColor = Color.Black;
        topPictureBox.Dock = DockStyle.Fill;
        topPictureBox.Location = new Point(0, 314);
        topPictureBox.Margin = new Padding(0, 2, 3, 0);
        topPictureBox.Name = "topPictureBox";
        topPictureBox.Size = new Size(546, 311);
        topPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        topPictureBox.TabIndex = 2;
        topPictureBox.TabStop = false;
        topPictureBox.MouseDown += OrthographicPictureBox_MouseDown;
        topPictureBox.MouseMove += OrthographicPictureBox_MouseMove;
        topPictureBox.MouseUp += OrthographicPictureBox_MouseUp;
        topPictureBox.MouseWheel += OrthographicPictureBox_MouseWheel;
        topPictureBox.Resize += PreviewPictureBox_Resize;
        // 
        // previewPictureBox
        // 
        previewPictureBox.BackColor = Color.Black;
        previewPictureBox.Dock = DockStyle.Fill;
        previewPictureBox.Location = new Point(552, 314);
        previewPictureBox.Margin = new Padding(3, 2, 0, 0);
        previewPictureBox.Name = "previewPictureBox";
        previewPictureBox.Size = new Size(546, 311);
        previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        previewPictureBox.TabIndex = 0;
        previewPictureBox.TabStop = false;
        previewPictureBox.MouseDown += PreviewPictureBox_MouseDown;
        previewPictureBox.MouseLeave += PreviewPictureBox_MouseLeave;
        previewPictureBox.MouseMove += PreviewPictureBox_MouseMove;
        previewPictureBox.MouseUp += PreviewPictureBox_MouseUp;
        previewPictureBox.MouseWheel += PreviewPictureBox_MouseWheel;
        previewPictureBox.Resize += PreviewPictureBox_Resize;
        // 
        // inspectorTabControl
        // 
        inspectorTabControl.Controls.Add(objectTabPage);
        inspectorTabControl.Controls.Add(cameraTabPage);
        inspectorTabControl.Controls.Add(lightTabPage);
        inspectorTabControl.Dock = DockStyle.Fill;
        inspectorTabControl.Location = new Point(0, 0);
        inspectorTabControl.Name = "inspectorTabControl";
        inspectorTabControl.SelectedIndex = 0;
        inspectorTabControl.Size = new Size(411, 625);
        inspectorTabControl.TabIndex = 0;
        // 
        // objectTabPage
        // 
        objectTabPage.Controls.Add(objectPropertyGrid);
        objectTabPage.Name = "objectTabPage";
        objectTabPage.Padding = new Padding(3);
        objectTabPage.TabIndex = 0;
        objectTabPage.Text = "物件";
        objectTabPage.UseVisualStyleBackColor = true;
        // 
        // objectPropertyGrid
        // 
        objectPropertyGrid.Dock = DockStyle.Fill;
        objectPropertyGrid.Name = "objectPropertyGrid";
        objectPropertyGrid.TabIndex = 0;
        objectPropertyGrid.PropertyValueChanged += ObjectPropertyGrid_PropertyValueChanged;
        // 
        // cameraTabPage
        // 
        cameraTabPage.Controls.Add(inspectorLayoutPanel);
        cameraTabPage.Location = new Point(4, 32);
        cameraTabPage.Name = "cameraTabPage";
        cameraTabPage.Padding = new Padding(3);
        cameraTabPage.Size = new Size(403, 589);
        cameraTabPage.TabIndex = 0;
        cameraTabPage.Text = "Camera";
        cameraTabPage.UseVisualStyleBackColor = true;
        // 
        // inspectorLayoutPanel
        // 
        inspectorLayoutPanel.ColumnCount = 1;
        inspectorLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        inspectorLayoutPanel.Controls.Add(cameraPropertyGrid, 0, 0);
        inspectorLayoutPanel.Controls.Add(keyframePropertyGrid, 0, 1);
        inspectorLayoutPanel.Controls.Add(useMainCameraButton, 0, 2);
        inspectorLayoutPanel.Controls.Add(applyMainCameraButton, 0, 3);
        inspectorLayoutPanel.Dock = DockStyle.Fill;
        inspectorLayoutPanel.Location = new Point(3, 3);
        inspectorLayoutPanel.Margin = new Padding(4);
        inspectorLayoutPanel.Name = "inspectorLayoutPanel";
        inspectorLayoutPanel.RowCount = 4;
        inspectorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        inspectorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
        inspectorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        inspectorLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        inspectorLayoutPanel.Size = new Size(397, 583);
        inspectorLayoutPanel.TabIndex = 0;
        // 
        // cameraPropertyGrid
        // 
        cameraPropertyGrid.Dock = DockStyle.Fill;
        cameraPropertyGrid.Location = new Point(4, 4);
        cameraPropertyGrid.Margin = new Padding(4);
        cameraPropertyGrid.Name = "cameraPropertyGrid";
        cameraPropertyGrid.Size = new Size(389, 264);
        cameraPropertyGrid.TabIndex = 0;
        cameraPropertyGrid.PropertyValueChanged += CameraPropertyGrid_PropertyValueChanged;
        // 
        // keyframePropertyGrid
        // 
        keyframePropertyGrid.Dock = DockStyle.Fill;
        keyframePropertyGrid.Location = new Point(4, 276);
        keyframePropertyGrid.Margin = new Padding(4);
        keyframePropertyGrid.Name = "keyframePropertyGrid";
        keyframePropertyGrid.Size = new Size(389, 214);
        keyframePropertyGrid.TabIndex = 0;
        keyframePropertyGrid.PropertyValueChanged += KeyframePropertyGrid_PropertyValueChanged;
        // 
        // useMainCameraButton
        // 
        useMainCameraButton.Dock = DockStyle.Fill;
        useMainCameraButton.Location = new Point(4, 498);
        useMainCameraButton.Margin = new Padding(4);
        useMainCameraButton.Name = "useMainCameraButton";
        useMainCameraButton.Size = new Size(389, 36);
        useMainCameraButton.TabIndex = 1;
        useMainCameraButton.Text = "載入主視角至 Preview";
        useMainCameraButton.UseVisualStyleBackColor = true;
        useMainCameraButton.Click += UseMainCameraButton_Click;
        // 
        // applyMainCameraButton
        // 
        applyMainCameraButton.Dock = DockStyle.Fill;
        applyMainCameraButton.Location = new Point(4, 542);
        applyMainCameraButton.Margin = new Padding(4);
        applyMainCameraButton.Name = "applyMainCameraButton";
        applyMainCameraButton.Size = new Size(389, 37);
        applyMainCameraButton.TabIndex = 2;
        applyMainCameraButton.Text = "套用 Preview 至主視角";
        applyMainCameraButton.UseVisualStyleBackColor = true;
        applyMainCameraButton.Click += ApplyMainCameraButton_Click;
        // 
        // lightTabPage
        // 
        lightTabPage.Controls.Add(lightLayoutPanel);
        lightTabPage.Location = new Point(4, 32);
        lightTabPage.Name = "lightTabPage";
        lightTabPage.Padding = new Padding(3);
        lightTabPage.Size = new Size(403, 633);
        lightTabPage.TabIndex = 1;
        lightTabPage.Text = "燈光";
        lightTabPage.UseVisualStyleBackColor = true;
        // 
        // lightLayoutPanel
        // 
        lightLayoutPanel.ColumnCount = 1;
        lightLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        lightLayoutPanel.Controls.Add(lightComboBox, 0, 0);
        lightLayoutPanel.Controls.Add(lightPropertyGrid, 0, 1);
        lightLayoutPanel.Dock = DockStyle.Fill;
        lightLayoutPanel.Location = new Point(3, 3);
        lightLayoutPanel.Name = "lightLayoutPanel";
        lightLayoutPanel.RowCount = 2;
        lightLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        lightLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        lightLayoutPanel.Size = new Size(397, 627);
        lightLayoutPanel.TabIndex = 0;
        // 
        // lightComboBox
        // 
        lightComboBox.Dock = DockStyle.Fill;
        lightComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        lightComboBox.FormattingEnabled = true;
        lightComboBox.Location = new Point(4, 4);
        lightComboBox.Margin = new Padding(4);
        lightComboBox.Name = "lightComboBox";
        lightComboBox.Size = new Size(389, 31);
        lightComboBox.TabIndex = 0;
        lightComboBox.SelectedIndexChanged += LightComboBox_SelectedIndexChanged;
        // 
        // lightPropertyGrid
        // 
        lightPropertyGrid.Dock = DockStyle.Fill;
        lightPropertyGrid.Location = new Point(4, 44);
        lightPropertyGrid.Margin = new Padding(4);
        lightPropertyGrid.Name = "lightPropertyGrid";
        lightPropertyGrid.Size = new Size(389, 579);
        lightPropertyGrid.TabIndex = 1;
        lightPropertyGrid.PropertyValueChanged += LightPropertyGrid_PropertyValueChanged;
        // 
        // playbackLayoutPanel
        // 
        playbackLayoutPanel.ColumnCount = 1;
        playbackLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        playbackLayoutPanel.Controls.Add(timelineTrackBar, 0, 0);
        playbackLayoutPanel.Controls.Add(playbackFlowLayoutPanel, 0, 1);
        playbackLayoutPanel.Controls.Add(exportFlowLayoutPanel, 0, 2);
        playbackLayoutPanel.Dock = DockStyle.Fill;
        playbackLayoutPanel.Location = new Point(4, 733);
        playbackLayoutPanel.Margin = new Padding(4);
        playbackLayoutPanel.Name = "playbackLayoutPanel";
        playbackLayoutPanel.RowCount = 3;
        playbackLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        playbackLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        playbackLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        playbackLayoutPanel.Size = new Size(1840, 181);
        playbackLayoutPanel.TabIndex = 2;
        // 
        // timelineTrackBar
        // 
        timelineTrackBar.Dock = DockStyle.Fill;
        timelineTrackBar.Location = new Point(4, 4);
        timelineTrackBar.Margin = new Padding(4);
        timelineTrackBar.Maximum = 10000;
        timelineTrackBar.Name = "timelineTrackBar";
        timelineTrackBar.Size = new Size(1832, 50);
        timelineTrackBar.TabIndex = 0;
        timelineTrackBar.TickStyle = TickStyle.None;
        timelineTrackBar.Scroll += TimelineTrackBar_Scroll;
        // 
        // playbackFlowLayoutPanel
        // 
        playbackFlowLayoutPanel.Controls.Add(playButton);
        playbackFlowLayoutPanel.Controls.Add(pauseButton);
        playbackFlowLayoutPanel.Controls.Add(stopButton);
        playbackFlowLayoutPanel.Controls.Add(keyframeTimeLabel);
        playbackFlowLayoutPanel.Controls.Add(keyframeTimeNumericUpDown);
        playbackFlowLayoutPanel.Controls.Add(durationLabel);
        playbackFlowLayoutPanel.Controls.Add(durationNumericUpDown);
        playbackFlowLayoutPanel.Controls.Add(fpsLabel);
        playbackFlowLayoutPanel.Controls.Add(fpsNumericUpDown);
        playbackFlowLayoutPanel.Controls.Add(loopCheckBox);
        playbackFlowLayoutPanel.Controls.Add(restoreCameraCheckBox);
        playbackFlowLayoutPanel.Controls.Add(zoomAtMouseCheckBox);
        playbackFlowLayoutPanel.Controls.Add(currentTimeLabel);
        playbackFlowLayoutPanel.Dock = DockStyle.Fill;
        playbackFlowLayoutPanel.Location = new Point(4, 62);
        playbackFlowLayoutPanel.Margin = new Padding(4);
        playbackFlowLayoutPanel.Name = "playbackFlowLayoutPanel";
        playbackFlowLayoutPanel.Size = new Size(1832, 50);
        playbackFlowLayoutPanel.TabIndex = 1;
        playbackFlowLayoutPanel.WrapContents = false;
        // 
        // playButton
        // 
        playButton.Location = new Point(4, 4);
        playButton.Margin = new Padding(4);
        playButton.Name = "playButton";
        playButton.Size = new Size(80, 36);
        playButton.TabIndex = 0;
        playButton.Text = "播放";
        playButton.UseVisualStyleBackColor = true;
        playButton.Click += PlayButton_Click;
        // 
        // pauseButton
        // 
        pauseButton.Enabled = false;
        pauseButton.Location = new Point(92, 4);
        pauseButton.Margin = new Padding(4);
        pauseButton.Name = "pauseButton";
        pauseButton.Size = new Size(80, 36);
        pauseButton.TabIndex = 1;
        pauseButton.Text = "暫停";
        pauseButton.UseVisualStyleBackColor = true;
        pauseButton.Click += PauseButton_Click;
        // 
        // stopButton
        // 
        stopButton.Enabled = false;
        stopButton.Location = new Point(180, 4);
        stopButton.Margin = new Padding(4);
        stopButton.Name = "stopButton";
        stopButton.Size = new Size(80, 36);
        stopButton.TabIndex = 2;
        stopButton.Text = "停止";
        stopButton.UseVisualStyleBackColor = true;
        stopButton.Click += StopButton_Click;
        // 
        // keyframeTimeLabel
        // 
        keyframeTimeLabel.AutoSize = true;
        keyframeTimeLabel.Location = new Point(276, 11);
        keyframeTimeLabel.Margin = new Padding(12, 11, 4, 0);
        keyframeTimeLabel.Name = "keyframeTimeLabel";
        keyframeTimeLabel.Size = new Size(82, 23);
        keyframeTimeLabel.TabIndex = 3;
        keyframeTimeLabel.Text = "影格時間";
        // 
        // keyframeTimeNumericUpDown
        // 
        keyframeTimeNumericUpDown.DecimalPlaces = 3;
        keyframeTimeNumericUpDown.Enabled = false;
        keyframeTimeNumericUpDown.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        keyframeTimeNumericUpDown.Location = new Point(366, 7);
        keyframeTimeNumericUpDown.Margin = new Padding(4, 7, 4, 4);
        keyframeTimeNumericUpDown.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
        keyframeTimeNumericUpDown.Name = "keyframeTimeNumericUpDown";
        keyframeTimeNumericUpDown.Size = new Size(107, 30);
        keyframeTimeNumericUpDown.TabIndex = 4;
        keyframeTimeNumericUpDown.ValueChanged += KeyframeTimeNumericUpDown_ValueChanged;
        // 
        // durationLabel
        // 
        durationLabel.AutoSize = true;
        durationLabel.Location = new Point(489, 11);
        durationLabel.Margin = new Padding(12, 11, 4, 0);
        durationLabel.Name = "durationLabel";
        durationLabel.Size = new Size(76, 23);
        durationLabel.TabIndex = 3;
        durationLabel.Text = "長度(秒)";
        // 
        // durationNumericUpDown
        // 
        durationNumericUpDown.DecimalPlaces = 2;
        durationNumericUpDown.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        durationNumericUpDown.Location = new Point(573, 7);
        durationNumericUpDown.Margin = new Padding(4, 7, 4, 4);
        durationNumericUpDown.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
        durationNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
        durationNumericUpDown.Name = "durationNumericUpDown";
        durationNumericUpDown.Size = new Size(99, 30);
        durationNumericUpDown.TabIndex = 4;
        durationNumericUpDown.Value = new decimal(new int[] { 5, 0, 0, 0 });
        durationNumericUpDown.ValueChanged += DurationNumericUpDown_ValueChanged;
        // 
        // fpsLabel
        // 
        fpsLabel.AutoSize = true;
        fpsLabel.Location = new Point(688, 11);
        fpsLabel.Margin = new Padding(12, 11, 4, 0);
        fpsLabel.Name = "fpsLabel";
        fpsLabel.Size = new Size(40, 23);
        fpsLabel.TabIndex = 5;
        fpsLabel.Text = "FPS";
        // 
        // fpsNumericUpDown
        // 
        fpsNumericUpDown.Location = new Point(736, 7);
        fpsNumericUpDown.Margin = new Padding(4, 7, 4, 4);
        fpsNumericUpDown.Maximum = new decimal(new int[] { 240, 0, 0, 0 });
        fpsNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        fpsNumericUpDown.Name = "fpsNumericUpDown";
        fpsNumericUpDown.Size = new Size(80, 30);
        fpsNumericUpDown.TabIndex = 6;
        fpsNumericUpDown.Value = new decimal(new int[] { 30, 0, 0, 0 });
        fpsNumericUpDown.ValueChanged += FpsNumericUpDown_ValueChanged;
        // 
        // loopCheckBox
        // 
        loopCheckBox.AutoSize = true;
        loopCheckBox.Location = new Point(832, 8);
        loopCheckBox.Margin = new Padding(12, 8, 4, 4);
        loopCheckBox.Name = "loopCheckBox";
        loopCheckBox.Size = new Size(72, 27);
        loopCheckBox.TabIndex = 7;
        loopCheckBox.Text = "循環";
        loopCheckBox.UseVisualStyleBackColor = true;
        loopCheckBox.CheckedChanged += LoopCheckBox_CheckedChanged;
        // 
        // restoreCameraCheckBox
        // 
        restoreCameraCheckBox.AutoSize = true;
        restoreCameraCheckBox.Checked = true;
        restoreCameraCheckBox.CheckState = CheckState.Checked;
        restoreCameraCheckBox.Enabled = false;
        restoreCameraCheckBox.Location = new Point(920, 8);
        restoreCameraCheckBox.Margin = new Padding(12, 8, 4, 4);
        restoreCameraCheckBox.Name = "restoreCameraCheckBox";
        restoreCameraCheckBox.Size = new Size(215, 27);
        restoreCameraCheckBox.TabIndex = 8;
        restoreCameraCheckBox.Text = "Preview 與主視角分離";
        restoreCameraCheckBox.UseVisualStyleBackColor = true;
        // 
        // zoomAtMouseCheckBox
        // 
        zoomAtMouseCheckBox.AutoSize = true;
        zoomAtMouseCheckBox.Checked = true;
        zoomAtMouseCheckBox.CheckState = CheckState.Checked;
        zoomAtMouseCheckBox.Location = new Point(1151, 8);
        zoomAtMouseCheckBox.Margin = new Padding(12, 8, 4, 4);
        zoomAtMouseCheckBox.Name = "zoomAtMouseCheckBox";
        zoomAtMouseCheckBox.Size = new Size(144, 27);
        zoomAtMouseCheckBox.TabIndex = 9;
        zoomAtMouseCheckBox.Text = "縮放滑鼠位置";
        zoomAtMouseCheckBox.UseVisualStyleBackColor = true;
        // 
        // currentTimeLabel
        // 
        currentTimeLabel.AutoSize = true;
        currentTimeLabel.Location = new Point(1311, 11);
        currentTimeLabel.Margin = new Padding(12, 11, 4, 0);
        currentTimeLabel.Name = "currentTimeLabel";
        currentTimeLabel.Size = new Size(139, 23);
        currentTimeLabel.TabIndex = 9;
        currentTimeLabel.Text = "0.000 / 5.000 秒";
        // 
        // exportFlowLayoutPanel
        // 
        exportFlowLayoutPanel.Controls.Add(exportFramesButton);
        exportFlowLayoutPanel.Controls.Add(exportVideoButton);
        exportFlowLayoutPanel.Controls.Add(cancelExportButton);
        exportFlowLayoutPanel.Controls.Add(exportWidthLabel);
        exportFlowLayoutPanel.Controls.Add(exportWidthNumericUpDown);
        exportFlowLayoutPanel.Controls.Add(exportHeightLabel);
        exportFlowLayoutPanel.Controls.Add(exportHeightNumericUpDown);
        exportFlowLayoutPanel.Controls.Add(exportProgressBar);
        exportFlowLayoutPanel.Controls.Add(exportStatusLabel);
        exportFlowLayoutPanel.Dock = DockStyle.Fill;
        exportFlowLayoutPanel.Location = new Point(4, 120);
        exportFlowLayoutPanel.Margin = new Padding(4);
        exportFlowLayoutPanel.Name = "exportFlowLayoutPanel";
        exportFlowLayoutPanel.Size = new Size(1832, 57);
        exportFlowLayoutPanel.TabIndex = 2;
        exportFlowLayoutPanel.WrapContents = false;
        // 
        // exportFramesButton
        // 
        exportFramesButton.Location = new Point(4, 4);
        exportFramesButton.Margin = new Padding(4);
        exportFramesButton.Name = "exportFramesButton";
        exportFramesButton.Size = new Size(168, 36);
        exportFramesButton.TabIndex = 0;
        exportFramesButton.Text = "輸出 PNG 影格…";
        exportFramesButton.UseVisualStyleBackColor = true;
        exportFramesButton.Click += ExportFramesButton_Click;
        // 
        // exportVideoButton
        // 
        exportVideoButton.Location = new Point(180, 4);
        exportVideoButton.Margin = new Padding(4);
        exportVideoButton.Name = "exportVideoButton";
        exportVideoButton.Size = new Size(138, 36);
        exportVideoButton.TabIndex = 1;
        exportVideoButton.Text = "輸出影片…";
        exportVideoButton.UseVisualStyleBackColor = true;
        exportVideoButton.Click += ExportVideoButton_Click;
        // 
        // cancelExportButton
        // 
        cancelExportButton.Enabled = false;
        cancelExportButton.Location = new Point(326, 4);
        cancelExportButton.Margin = new Padding(4);
        cancelExportButton.Name = "cancelExportButton";
        cancelExportButton.Size = new Size(99, 36);
        cancelExportButton.TabIndex = 1;
        cancelExportButton.Text = "取消輸出";
        cancelExportButton.UseVisualStyleBackColor = true;
        cancelExportButton.Click += CancelExportButton_Click;
        // 
        // exportWidthLabel
        // 
        exportWidthLabel.AutoSize = true;
        exportWidthLabel.Location = new Point(441, 11);
        exportWidthLabel.Margin = new Padding(12, 11, 4, 0);
        exportWidthLabel.Name = "exportWidthLabel";
        exportWidthLabel.Size = new Size(46, 23);
        exportWidthLabel.TabIndex = 2;
        exportWidthLabel.Text = "寬度";
        // 
        // exportWidthNumericUpDown
        // 
        exportWidthNumericUpDown.Increment = new decimal(new int[] { 16, 0, 0, 0 });
        exportWidthNumericUpDown.Location = new Point(495, 7);
        exportWidthNumericUpDown.Margin = new Padding(4, 7, 4, 4);
        exportWidthNumericUpDown.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
        exportWidthNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        exportWidthNumericUpDown.Name = "exportWidthNumericUpDown";
        exportWidthNumericUpDown.Size = new Size(104, 30);
        exportWidthNumericUpDown.TabIndex = 3;
        exportWidthNumericUpDown.Value = new decimal(new int[] { 1920, 0, 0, 0 });
        // 
        // exportHeightLabel
        // 
        exportHeightLabel.AutoSize = true;
        exportHeightLabel.Location = new Point(615, 11);
        exportHeightLabel.Margin = new Padding(12, 11, 4, 0);
        exportHeightLabel.Name = "exportHeightLabel";
        exportHeightLabel.Size = new Size(46, 23);
        exportHeightLabel.TabIndex = 4;
        exportHeightLabel.Text = "高度";
        // 
        // exportHeightNumericUpDown
        // 
        exportHeightNumericUpDown.Increment = new decimal(new int[] { 16, 0, 0, 0 });
        exportHeightNumericUpDown.Location = new Point(669, 7);
        exportHeightNumericUpDown.Margin = new Padding(4, 7, 4, 4);
        exportHeightNumericUpDown.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
        exportHeightNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        exportHeightNumericUpDown.Name = "exportHeightNumericUpDown";
        exportHeightNumericUpDown.Size = new Size(104, 30);
        exportHeightNumericUpDown.TabIndex = 5;
        exportHeightNumericUpDown.Value = new decimal(new int[] { 1080, 0, 0, 0 });
        // 
        // exportProgressBar
        // 
        exportProgressBar.Location = new Point(789, 7);
        exportProgressBar.Margin = new Padding(12, 7, 4, 4);
        exportProgressBar.Name = "exportProgressBar";
        exportProgressBar.Size = new Size(261, 33);
        exportProgressBar.TabIndex = 6;
        // 
        // exportStatusLabel
        // 
        exportStatusLabel.AutoSize = true;
        exportStatusLabel.Location = new Point(1066, 11);
        exportStatusLabel.Margin = new Padding(12, 11, 4, 0);
        exportStatusLabel.Name = "exportStatusLabel";
        exportStatusLabel.Size = new Size(46, 23);
        exportStatusLabel.TabIndex = 7;
        exportStatusLabel.Text = "就緒";
        // 
        // playbackTimer
        // 
        playbackTimer.Interval = 15;
        playbackTimer.Tick += PlaybackTimer_Tick;
        // 
        // previewRenderTimer
        // 
        previewRenderTimer.Interval = 33;
        previewRenderTimer.Tick += PreviewRenderTimer_Tick;
        // 
        // CameraAnimationForm
        // 
        AutoScaleDimensions = new SizeF(11F, 23F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1848, 918);
        Controls.Add(rootLayoutPanel);
        Controls.Add(unifiedMenuStrip);
        MainMenuStrip = unifiedMenuStrip;
        Margin = new Padding(4);
        MinimumSize = new Size(1367, 775);
        Name = "CameraAnimationForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Camera 運鏡動畫";
        WindowState = FormWindowState.Maximized;
        FormClosing += CameraAnimationForm_FormClosing;
        rootLayoutPanel.ResumeLayout(false);
        commandFlowLayoutPanel.ResumeLayout(false);
        commandFlowLayoutPanel.PerformLayout();
        editorSplitContainer.Panel1.ResumeLayout(false);
        editorSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)editorSplitContainer).EndInit();
        editorSplitContainer.ResumeLayout(false);
        previewSplitContainer.Panel1.ResumeLayout(false);
        previewSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)previewSplitContainer).EndInit();
        previewSplitContainer.ResumeLayout(false);
        fourViewLayoutPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)frontPictureBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)leftPictureBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)topPictureBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).EndInit();
        inspectorTabControl.ResumeLayout(false);
        cameraTabPage.ResumeLayout(false);
        inspectorLayoutPanel.ResumeLayout(false);
        lightTabPage.ResumeLayout(false);
        lightLayoutPanel.ResumeLayout(false);
        playbackLayoutPanel.ResumeLayout(false);
        playbackLayoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)timelineTrackBar).EndInit();
        playbackFlowLayoutPanel.ResumeLayout(false);
        playbackFlowLayoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)keyframeTimeNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)durationNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)fpsNumericUpDown).EndInit();
        exportFlowLayoutPanel.ResumeLayout(false);
        exportFlowLayoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)exportWidthNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)exportHeightNumericUpDown).EndInit();
        ResumeLayout(false);
    }
}
