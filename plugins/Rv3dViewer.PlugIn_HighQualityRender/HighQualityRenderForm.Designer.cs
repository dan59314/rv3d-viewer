#nullable enable

namespace Rv3dViewer.HighQualityRenderPlugin;

partial class HighQualityRenderForm
{
    private System.ComponentModel.IContainer? components = null;
    private Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip unifiedMenuStrip = null!;
    private SplitContainer mainSplitContainer = null!;
    private OpenTK.GLControl.GLControl renderPreviewGlControl = null!;
#pragma warning disable CS0414 // Used when HighQualityRenderForm.cs defines UseOldRender.
    private RenderPreviewControl oldRenderPreviewControl = null!;
#pragma warning restore CS0414
    private PictureBox renderedImagePictureBox = null!;
    private TableLayoutPanel rootLayoutPanel = null!;
    private TabControl settingsTabControl = null!;
    private TabPage basicSettingsTabPage = null!;
    private TabPage pipelineSettingsTabPage = null!;
    private RenderPipelineSettingsControl renderPipelineSettingsControl = null!;
    private GroupBox settingsGroupBox = null!;
    private TableLayoutPanel settingsLayoutPanel = null!;
    private Label renderProfileLabel = null!;
    private ComboBox renderProfileComboBox = null!;
    private Label widthLabel = null!;
    private NumericUpDown widthNumericUpDown = null!;
    private Label heightLabel = null!;
    private NumericUpDown heightNumericUpDown = null!;
    private Label samplesLabel = null!;
    private NumericUpDown samplesNumericUpDown = null!;
    private Label bouncesLabel = null!;
    private NumericUpDown bouncesNumericUpDown = null!;
    private Label gpuDeviceLabel = null!;
    private ComboBox gpuDeviceComboBox = null!;
    private CheckBox transparentBackgroundCheckBox = null!;
    private CheckBox environmentCheckBox = null!;
    private GroupBox outputGroupBox = null!;
    private TableLayoutPanel outputLayoutPanel = null!;
    private TextBox outputPathTextBox = null!;
    private Button browseButton = null!;
    private ProgressBar progressBar = null!;
    private Label statusLabel = null!;
    private FlowLayoutPanel commandPanel = null!;
    private Button cancelButton = null!;
    private Button viewButton = null!;
    private Button renderButton = null!;
    private SaveFileDialog saveFileDialog = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        unifiedMenuStrip = new Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HighQualityRenderForm));
        mainSplitContainer = new SplitContainer();
        oldRenderPreviewControl = new RenderPreviewControl();
        renderedImagePictureBox = new PictureBox();
        renderPreviewGlControl = new OpenTK.GLControl.GLControl();
        rootLayoutPanel = new TableLayoutPanel();
        settingsTabControl = new TabControl();
        basicSettingsTabPage = new TabPage();
        settingsGroupBox = new GroupBox();
        settingsLayoutPanel = new TableLayoutPanel();
        renderProfileLabel = new Label();
        renderProfileComboBox = new ComboBox();
        widthLabel = new Label();
        widthNumericUpDown = new NumericUpDown();
        heightLabel = new Label();
        heightNumericUpDown = new NumericUpDown();
        samplesLabel = new Label();
        samplesNumericUpDown = new NumericUpDown();
        bouncesLabel = new Label();
        bouncesNumericUpDown = new NumericUpDown();
        gpuDeviceLabel = new Label();
        gpuDeviceComboBox = new ComboBox();
        transparentBackgroundCheckBox = new CheckBox();
        environmentCheckBox = new CheckBox();
        pipelineSettingsTabPage = new TabPage();
        renderPipelineSettingsControl = new RenderPipelineSettingsControl();
        outputGroupBox = new GroupBox();
        outputLayoutPanel = new TableLayoutPanel();
        outputPathTextBox = new TextBox();
        browseButton = new Button();
        progressBar = new ProgressBar();
        statusLabel = new Label();
        commandPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        viewButton = new Button();
        renderButton = new Button();
        saveFileDialog = new SaveFileDialog();
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
        mainSplitContainer.Panel1.SuspendLayout();
        mainSplitContainer.Panel2.SuspendLayout();
        mainSplitContainer.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)renderedImagePictureBox).BeginInit();
        rootLayoutPanel.SuspendLayout();
        settingsTabControl.SuspendLayout();
        basicSettingsTabPage.SuspendLayout();
        pipelineSettingsTabPage.SuspendLayout();
        settingsGroupBox.SuspendLayout();
        settingsLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)widthNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)heightNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)samplesNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)bouncesNumericUpDown).BeginInit();
        outputGroupBox.SuspendLayout();
        outputLayoutPanel.SuspendLayout();
        commandPanel.SuspendLayout();
        SuspendLayout();
        // 
        // mainSplitContainer
        // 
        mainSplitContainer.Dock = DockStyle.Fill;
        mainSplitContainer.FixedPanel = FixedPanel.Panel2;
        mainSplitContainer.Location = new Point(0, 0);
        mainSplitContainer.Name = "mainSplitContainer";
        // 
        // mainSplitContainer.Panel1
        // 
        mainSplitContainer.Panel1.Controls.Add(renderedImagePictureBox);
        mainSplitContainer.Panel1.Controls.Add(renderPreviewGlControl);
        mainSplitContainer.Panel1.Controls.Add(oldRenderPreviewControl);
        // 
        // mainSplitContainer.Panel2
        // 
        mainSplitContainer.Panel2.Controls.Add(rootLayoutPanel);
        mainSplitContainer.Panel2MinSize = 400;
        mainSplitContainer.Size = new Size(1180, 724);
        mainSplitContainer.SplitterDistance = 776;
        mainSplitContainer.TabIndex = 0;
        // 
        // oldRenderPreviewControl
        // 
        oldRenderPreviewControl.BackColor = Color.Black;
        oldRenderPreviewControl.Dock = DockStyle.Fill;
        oldRenderPreviewControl.Location = new Point(0, 0);
        oldRenderPreviewControl.Name = "oldRenderPreviewControl";
        oldRenderPreviewControl.Size = new Size(776, 724);
        oldRenderPreviewControl.TabIndex = 2;
        oldRenderPreviewControl.Visible = false;
        // 
        // renderedImagePictureBox
        // 
        renderedImagePictureBox.BackColor = Color.Black;
        renderedImagePictureBox.Dock = DockStyle.Fill;
        renderedImagePictureBox.Location = new Point(0, 0);
        renderedImagePictureBox.Name = "renderedImagePictureBox";
        renderedImagePictureBox.Size = new Size(776, 724);
        renderedImagePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        renderedImagePictureBox.TabIndex = 1;
        renderedImagePictureBox.TabStop = false;
        renderedImagePictureBox.Visible = false;
        renderedImagePictureBox.MouseDown += RenderedImagePictureBox_MouseDown;
        // 
        // renderPreviewGlControl
        // 
        renderPreviewGlControl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
        renderPreviewGlControl.APIVersion = new Version(3, 3, 0, 0);
        renderPreviewGlControl.BackColor = Color.Black;
        renderPreviewGlControl.Dock = DockStyle.Fill;
        renderPreviewGlControl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
        renderPreviewGlControl.IsEventDriven = true;
        renderPreviewGlControl.Location = new Point(0, 0);
        renderPreviewGlControl.Name = "renderPreviewGlControl";
        renderPreviewGlControl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
        renderPreviewGlControl.SharedContext = null;
        renderPreviewGlControl.Size = new Size(776, 724);
        renderPreviewGlControl.TabIndex = 0;
        // 
        // rootLayoutPanel
        // 
        rootLayoutPanel.ColumnCount = 1;
        rootLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayoutPanel.Controls.Add(settingsTabControl, 0, 0);
        rootLayoutPanel.Controls.Add(outputGroupBox, 0, 1);
        rootLayoutPanel.Controls.Add(progressBar, 0, 2);
        rootLayoutPanel.Controls.Add(statusLabel, 0, 3);
        rootLayoutPanel.Controls.Add(commandPanel, 0, 4);
        rootLayoutPanel.Dock = DockStyle.Fill;
        rootLayoutPanel.Location = new Point(0, 0);
        rootLayoutPanel.Name = "rootLayoutPanel";
        rootLayoutPanel.Padding = new Padding(12);
        rootLayoutPanel.RowCount = 5;
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 420F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 95F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        rootLayoutPanel.Size = new Size(400, 724);
        rootLayoutPanel.TabIndex = 0;
        // 
        // settingsTabControl
        // 
        settingsTabControl.Controls.Add(basicSettingsTabPage);
        settingsTabControl.Controls.Add(pipelineSettingsTabPage);
        settingsTabControl.Dock = DockStyle.Fill;
        settingsTabControl.Location = new Point(15, 15);
        settingsTabControl.Name = "settingsTabControl";
        settingsTabControl.SelectedIndex = 0;
        settingsTabControl.Size = new Size(370, 414);
        settingsTabControl.TabIndex = 0;
        // 
        // basicSettingsTabPage
        // 
        basicSettingsTabPage.Controls.Add(settingsGroupBox);
        basicSettingsTabPage.Location = new Point(4, 32);
        basicSettingsTabPage.Name = "basicSettingsTabPage";
        basicSettingsTabPage.Padding = new Padding(3);
        basicSettingsTabPage.Size = new Size(362, 378);
        basicSettingsTabPage.TabIndex = 0;
        basicSettingsTabPage.Text = "基本設定";
        basicSettingsTabPage.UseVisualStyleBackColor = true;
        // 
        // settingsGroupBox
        // 
        settingsGroupBox.Controls.Add(settingsLayoutPanel);
        settingsGroupBox.Dock = DockStyle.Fill;
        settingsGroupBox.Location = new Point(3, 3);
        settingsGroupBox.Name = "settingsGroupBox";
        settingsGroupBox.Padding = new Padding(10);
        settingsGroupBox.Size = new Size(356, 372);
        settingsGroupBox.TabIndex = 0;
        settingsGroupBox.TabStop = false;
        settingsGroupBox.Text = "Render 設定";
        // 
        // settingsLayoutPanel
        // 
        settingsLayoutPanel.ColumnCount = 2;
        settingsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        settingsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        settingsLayoutPanel.Controls.Add(renderProfileLabel, 0, 0);
        settingsLayoutPanel.Controls.Add(renderProfileComboBox, 1, 0);
        settingsLayoutPanel.Controls.Add(widthLabel, 0, 1);
        settingsLayoutPanel.Controls.Add(widthNumericUpDown, 1, 1);
        settingsLayoutPanel.Controls.Add(heightLabel, 0, 2);
        settingsLayoutPanel.Controls.Add(heightNumericUpDown, 1, 2);
        settingsLayoutPanel.Controls.Add(samplesLabel, 0, 3);
        settingsLayoutPanel.Controls.Add(samplesNumericUpDown, 1, 3);
        settingsLayoutPanel.Controls.Add(bouncesLabel, 0, 4);
        settingsLayoutPanel.Controls.Add(bouncesNumericUpDown, 1, 4);
        settingsLayoutPanel.Controls.Add(gpuDeviceLabel, 0, 5);
        settingsLayoutPanel.Controls.Add(gpuDeviceComboBox, 1, 5);
        settingsLayoutPanel.Controls.Add(transparentBackgroundCheckBox, 0, 6);
        settingsLayoutPanel.Controls.Add(environmentCheckBox, 1, 6);
        settingsLayoutPanel.Dock = DockStyle.Fill;
        settingsLayoutPanel.Location = new Point(10, 33);
        settingsLayoutPanel.Name = "settingsLayoutPanel";
        settingsLayoutPanel.RowCount = 7;
        settingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        settingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        settingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        settingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        settingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        settingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        settingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        settingsLayoutPanel.Size = new Size(336, 329);
        settingsLayoutPanel.TabIndex = 0;
        // 
        // renderProfileLabel
        // 
        renderProfileLabel.Dock = DockStyle.Fill;
        renderProfileLabel.Location = new Point(3, 0);
        renderProfileLabel.Name = "renderProfileLabel";
        renderProfileLabel.Size = new Size(135, 47);
        renderProfileLabel.TabIndex = 0;
        renderProfileLabel.Text = "Render 設定";
        renderProfileLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // renderProfileComboBox
        // 
        renderProfileComboBox.Dock = DockStyle.Fill;
        renderProfileComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        renderProfileComboBox.FormattingEnabled = true;
        renderProfileComboBox.Items.AddRange(new object[] { "快速草稿", "基本", "產品棚拍", "金屬／高反射", "玻璃／透明材質", "珠寶／寶石", "珠寶-OIDN", "室內設計", "最終高品質", "自訂" });
        renderProfileComboBox.Location = new Point(144, 3);
        renderProfileComboBox.Name = "renderProfileComboBox";
        renderProfileComboBox.Size = new Size(189, 31);
        renderProfileComboBox.TabIndex = 1;
        renderProfileComboBox.SelectedIndexChanged += RenderProfileComboBox_SelectedIndexChanged;
        // 
        // widthLabel
        // 
        widthLabel.Dock = DockStyle.Fill;
        widthLabel.Location = new Point(3, 0);
        widthLabel.Name = "widthLabel";
        widthLabel.Size = new Size(135, 54);
        widthLabel.TabIndex = 0;
        widthLabel.Text = "寬度（px）";
        widthLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // widthNumericUpDown
        // 
        widthNumericUpDown.Dock = DockStyle.Fill;
        widthNumericUpDown.Location = new Point(144, 3);
        widthNumericUpDown.Maximum = new decimal(new int[] { 8192, 0, 0, 0 });
        widthNumericUpDown.Minimum = new decimal(new int[] { 320, 0, 0, 0 });
        widthNumericUpDown.Name = "widthNumericUpDown";
        widthNumericUpDown.Size = new Size(189, 30);
        widthNumericUpDown.TabIndex = 1;
        widthNumericUpDown.Value = new decimal(new int[] { 1024, 0, 0, 0 });
        // 
        // heightLabel
        // 
        heightLabel.Dock = DockStyle.Fill;
        heightLabel.Location = new Point(3, 54);
        heightLabel.Name = "heightLabel";
        heightLabel.Size = new Size(135, 54);
        heightLabel.TabIndex = 2;
        heightLabel.Text = "高度（px）";
        heightLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // heightNumericUpDown
        // 
        heightNumericUpDown.Dock = DockStyle.Fill;
        heightNumericUpDown.Location = new Point(144, 57);
        heightNumericUpDown.Maximum = new decimal(new int[] { 8192, 0, 0, 0 });
        heightNumericUpDown.Minimum = new decimal(new int[] { 240, 0, 0, 0 });
        heightNumericUpDown.Name = "heightNumericUpDown";
        heightNumericUpDown.Size = new Size(189, 30);
        heightNumericUpDown.TabIndex = 3;
        heightNumericUpDown.Value = new decimal(new int[] { 768, 0, 0, 0 });
        // 
        // samplesLabel
        // 
        samplesLabel.Dock = DockStyle.Fill;
        samplesLabel.Location = new Point(3, 108);
        samplesLabel.Name = "samplesLabel";
        samplesLabel.Size = new Size(135, 54);
        samplesLabel.TabIndex = 4;
        samplesLabel.Text = "每像素取樣數";
        samplesLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // samplesNumericUpDown
        // 
        samplesNumericUpDown.Dock = DockStyle.Fill;
        samplesNumericUpDown.Location = new Point(144, 111);
        samplesNumericUpDown.Maximum = new decimal(new int[] { 1024, 0, 0, 0 });
        samplesNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        samplesNumericUpDown.Name = "samplesNumericUpDown";
        samplesNumericUpDown.Size = new Size(189, 30);
        samplesNumericUpDown.TabIndex = 5;
        samplesNumericUpDown.Value = new decimal(new int[] { 256, 0, 0, 0 });
        samplesNumericUpDown.ValueChanged += BasicPipelineSetting_Changed;
        // 
        // bouncesLabel
        // 
        bouncesLabel.Dock = DockStyle.Fill;
        bouncesLabel.Location = new Point(3, 162);
        bouncesLabel.Name = "bouncesLabel";
        bouncesLabel.Size = new Size(135, 54);
        bouncesLabel.TabIndex = 6;
        bouncesLabel.Text = "最大反射次數";
        bouncesLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // bouncesNumericUpDown
        // 
        bouncesNumericUpDown.Dock = DockStyle.Fill;
        bouncesNumericUpDown.Location = new Point(144, 165);
        bouncesNumericUpDown.Maximum = new decimal(new int[] { 16, 0, 0, 0 });
        bouncesNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        bouncesNumericUpDown.Name = "bouncesNumericUpDown";
        bouncesNumericUpDown.Size = new Size(189, 30);
        bouncesNumericUpDown.TabIndex = 7;
        bouncesNumericUpDown.Value = new decimal(new int[] { 6, 0, 0, 0 });
        bouncesNumericUpDown.ValueChanged += BasicPipelineSetting_Changed;
        // 
        // gpuDeviceLabel
        // 
        gpuDeviceLabel.Dock = DockStyle.Fill;
        gpuDeviceLabel.Location = new Point(3, 216);
        gpuDeviceLabel.Name = "gpuDeviceLabel";
        gpuDeviceLabel.Size = new Size(135, 54);
        gpuDeviceLabel.TabIndex = 8;
        gpuDeviceLabel.Text = "GPU";
        gpuDeviceLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // gpuDeviceComboBox
        // 
        gpuDeviceComboBox.Dock = DockStyle.Fill;
        gpuDeviceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        gpuDeviceComboBox.DropDownWidth = 420;
        gpuDeviceComboBox.FormattingEnabled = true;
        gpuDeviceComboBox.Location = new Point(144, 219);
        gpuDeviceComboBox.Name = "gpuDeviceComboBox";
        gpuDeviceComboBox.Size = new Size(189, 31);
        gpuDeviceComboBox.TabIndex = 9;
        gpuDeviceComboBox.SelectedIndexChanged += GpuDeviceComboBox_SelectedIndexChanged;
        // 
        // transparentBackgroundCheckBox
        // 
        transparentBackgroundCheckBox.AutoSize = true;
        transparentBackgroundCheckBox.Dock = DockStyle.Fill;
        transparentBackgroundCheckBox.Location = new Point(3, 273);
        transparentBackgroundCheckBox.Name = "transparentBackgroundCheckBox";
        transparentBackgroundCheckBox.Size = new Size(135, 53);
        transparentBackgroundCheckBox.TabIndex = 10;
        transparentBackgroundCheckBox.Text = "透明背景";
        // 
        // environmentCheckBox
        // 
        environmentCheckBox.AutoSize = true;
        environmentCheckBox.Checked = true;
        environmentCheckBox.CheckState = CheckState.Checked;
        environmentCheckBox.Dock = DockStyle.Fill;
        environmentCheckBox.Location = new Point(144, 273);
        environmentCheckBox.Name = "environmentCheckBox";
        environmentCheckBox.Size = new Size(189, 53);
        environmentCheckBox.TabIndex = 11;
        environmentCheckBox.Text = "使用環境照明";
        // 
        // pipelineSettingsTabPage
        // 
        pipelineSettingsTabPage.Controls.Add(renderPipelineSettingsControl);
        pipelineSettingsTabPage.Location = new Point(4, 32);
        pipelineSettingsTabPage.Name = "pipelineSettingsTabPage";
        pipelineSettingsTabPage.Padding = new Padding(3);
        pipelineSettingsTabPage.Size = new Size(362, 378);
        pipelineSettingsTabPage.TabIndex = 1;
        pipelineSettingsTabPage.Text = "品質／Pipeline";
        pipelineSettingsTabPage.UseVisualStyleBackColor = true;
        // 
        // renderPipelineSettingsControl
        // 
        renderPipelineSettingsControl.Dock = DockStyle.Fill;
        renderPipelineSettingsControl.Location = new Point(3, 3);
        renderPipelineSettingsControl.Name = "renderPipelineSettingsControl";
        renderPipelineSettingsControl.Size = new Size(356, 372);
        renderPipelineSettingsControl.TabIndex = 0;
        // 
        // outputGroupBox
        // 
        outputGroupBox.Controls.Add(outputLayoutPanel);
        outputGroupBox.Dock = DockStyle.Fill;
        outputGroupBox.Location = new Point(15, 435);
        outputGroupBox.Name = "outputGroupBox";
        outputGroupBox.Padding = new Padding(10);
        outputGroupBox.Size = new Size(370, 89);
        outputGroupBox.TabIndex = 1;
        outputGroupBox.TabStop = false;
        outputGroupBox.Text = "輸出 PNG";
        // 
        // outputLayoutPanel
        // 
        outputLayoutPanel.ColumnCount = 2;
        outputLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        outputLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        outputLayoutPanel.Controls.Add(outputPathTextBox, 0, 0);
        outputLayoutPanel.Controls.Add(browseButton, 1, 0);
        outputLayoutPanel.Dock = DockStyle.Fill;
        outputLayoutPanel.Location = new Point(10, 33);
        outputLayoutPanel.Name = "outputLayoutPanel";
        outputLayoutPanel.RowCount = 1;
        outputLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        outputLayoutPanel.Size = new Size(350, 46);
        outputLayoutPanel.TabIndex = 0;
        // 
        // outputPathTextBox
        // 
        outputPathTextBox.Dock = DockStyle.Fill;
        outputPathTextBox.Location = new Point(3, 3);
        outputPathTextBox.Name = "outputPathTextBox";
        outputPathTextBox.Size = new Size(254, 30);
        outputPathTextBox.TabIndex = 0;
        // 
        // browseButton
        // 
        browseButton.Dock = DockStyle.Fill;
        browseButton.Location = new Point(263, 3);
        browseButton.Name = "browseButton";
        browseButton.Size = new Size(84, 40);
        browseButton.TabIndex = 1;
        browseButton.Text = "瀏覽…";
        browseButton.Click += BrowseButton_Click;
        // 
        // progressBar
        // 
        progressBar.Dock = DockStyle.Fill;
        progressBar.Location = new Point(15, 530);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(370, 25);
        progressBar.TabIndex = 2;
        // 
        // statusLabel
        // 
        statusLabel.AutoEllipsis = true;
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.Location = new Point(15, 558);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(370, 84);
        statusLabel.TabIndex = 3;
        statusLabel.Text = "準備 Render";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // commandPanel
        // 
        commandPanel.Controls.Add(cancelButton);
        commandPanel.Controls.Add(viewButton);
        commandPanel.Controls.Add(renderButton);
        commandPanel.Dock = DockStyle.Fill;
        commandPanel.FlowDirection = FlowDirection.RightToLeft;
        commandPanel.Location = new Point(15, 645);
        commandPanel.Name = "commandPanel";
        commandPanel.Padding = new Padding(0, 7, 0, 0);
        commandPanel.Size = new Size(370, 64);
        commandPanel.TabIndex = 4;
        commandPanel.WrapContents = false;
        // 
        // cancelButton
        // 
        cancelButton.AutoSize = true;
        cancelButton.Location = new Point(292, 10);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 33);
        cancelButton.TabIndex = 0;
        cancelButton.Text = "關閉";
        cancelButton.Click += CancelButton_Click;
        // 
        // viewButton
        // 
        viewButton.AutoSize = true;
        viewButton.Enabled = false;
        viewButton.Location = new Point(211, 10);
        viewButton.Name = "viewButton";
        viewButton.Size = new Size(75, 33);
        viewButton.TabIndex = 1;
        viewButton.Text = "檢視";
        viewButton.Click += ViewButton_Click;
        // 
        // renderButton
        // 
        renderButton.AutoSize = true;
        renderButton.Location = new Point(83, 10);
        renderButton.Name = "renderButton";
        renderButton.Size = new Size(122, 33);
        renderButton.TabIndex = 2;
        renderButton.Text = "開始 Render";
        renderButton.Click += RenderButton_Click;
        // 
        // saveFileDialog
        // 
        saveFileDialog.DefaultExt = "png";
        saveFileDialog.Filter = "PNG 圖片 (*.png)|*.png";
        saveFileDialog.Title = "選擇高品質 Render 輸出位置";
        // 
        // HighQualityRenderForm
        // 
        AcceptButton = renderButton;
        CancelButton = cancelButton;
        ClientSize = new Size(1180, 724);
        Controls.Add(mainSplitContainer);
        Controls.Add(unifiedMenuStrip);
        MainMenuStrip = unifiedMenuStrip;
        Icon = resources.GetObject("$this.Icon") as Icon;
        MinimumSize = new Size(900, 600);
        Name = "HighQualityRenderForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "高品質 Render";
        WindowState = FormWindowState.Maximized;
        FormClosing += HighQualityRenderForm_FormClosing;
        mainSplitContainer.Panel1.ResumeLayout(false);
        mainSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
        mainSplitContainer.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)renderedImagePictureBox).EndInit();
        rootLayoutPanel.ResumeLayout(false);
        settingsTabControl.ResumeLayout(false);
        basicSettingsTabPage.ResumeLayout(false);
        pipelineSettingsTabPage.ResumeLayout(false);
        settingsGroupBox.ResumeLayout(false);
        settingsLayoutPanel.ResumeLayout(false);
        settingsLayoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)widthNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)heightNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)samplesNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)bouncesNumericUpDown).EndInit();
        outputGroupBox.ResumeLayout(false);
        outputLayoutPanel.ResumeLayout(false);
        outputLayoutPanel.PerformLayout();
        commandPanel.ResumeLayout(false);
        commandPanel.PerformLayout();
        ResumeLayout(false);
    }
}
