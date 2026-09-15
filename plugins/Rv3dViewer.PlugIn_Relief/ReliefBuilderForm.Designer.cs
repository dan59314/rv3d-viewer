#nullable enable

namespace Rv3dViewer.ReliefPlugin;

partial class ReliefBuilderForm
{
    private System.ComponentModel.IContainer? components = null;
    private Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip unifiedMenuStrip = null!;
    private TableLayoutPanel rootLayoutPanel = null!;
    private SplitContainer workspaceSplitContainer = null!;
    private Panel parameterScrollPanel = null!;
    private TableLayoutPanel parameterTableLayoutPanel = null!;
    private CheckBox expandAllGroupsCheckBox = null!;
    private CollapsibleGroupBox sourceGroupBox = null!;
    private TableLayoutPanel sourceTableLayoutPanel = null!;
    private TextBox sourcePathTextBox = null!;
    private Button openImageButton = null!;
    private Label parameterProfileLabel = null!;
    private ComboBox parameterProfileComboBox = null!;
    private FlowLayoutPanel parameterProfileButtonFlowLayoutPanel = null!;
    private Button updateParameterProfileButton = null!;
    private Button addParameterProfileButton = null!;
    private Button renameParameterProfileButton = null!;
    private CollapsibleGroupBox geometryGroupBox = null!;
    private TableLayoutPanel geometryTableLayoutPanel = null!;
    private Label widthLabel = null!;
    private Panel widthEditorPanel = null!;
    private NumericUpDown widthNumericUpDown = null!;
    private Label thicknessLabel = null!;
    private Panel thicknessEditorPanel = null!;
    private NumericUpDown thicknessNumericUpDown = null!;
    private Label borderWidthLabel = null!;
    private Panel borderWidthEditorPanel = null!;
    private NumericUpDown borderWidthNumericUpDown = null!;
    private Label reliefHeightLabel = null!;
    private Panel reliefHeightEditorPanel = null!;
    private NumericUpDown reliefHeightNumericUpDown = null!;
    private Label qualityLabel = null!;
    private ComboBox qualityComboBox = null!;
    private Label modelPlaneLabel = null!;
    private FlowLayoutPanel modelPlaneFlowLayoutPanel = null!;
    private RadioButton xyPlaneRadioButton = null!;
    private RadioButton yzPlaneRadioButton = null!;
    private RadioButton xzPlaneRadioButton = null!;
    private Label modelRotationAngleLabel = null!;
    private NumericUpDown modelRotationAngleNumericUpDown = null!;
    private Label modelAlignmentLabel = null!;
    private FlowLayoutPanel modelAlignmentFlowLayoutPanel = null!;
    private RadioButton centerAlignmentRadioButton = null!;
    private RadioButton leftBottomAlignmentRadioButton = null!;
    private CheckBox textureCheckBox = null!;
    private CheckBox simplifyModelCheckBox = null!;
    private Label simplificationTargetLabel = null!;
    private NumericUpDown simplificationTargetNumericUpDown = null!;
    private Label simplificationNormalAngleLabel = null!;
    private NumericUpDown simplificationNormalAngleNumericUpDown = null!;
    private CollapsibleGroupBox smoothingGroupBox = null!;
    private TableLayoutPanel smoothingTableLayoutPanel = null!;
    private CheckBox smoothingCheckBox = null!;
    private Label smoothingThresholdLabel = null!;
    private Panel smoothingThresholdEditorPanel = null!;
    private NumericUpDown smoothingThresholdNumericUpDown = null!;
    private Label smoothingStrengthLabel = null!;
    private Panel smoothingStrengthEditorPanel = null!;
    private NumericUpDown smoothingStrengthNumericUpDown = null!;
    private Label smoothingIterationsLabel = null!;
    private Panel smoothingIterationsEditorPanel = null!;
    private NumericUpDown smoothingIterationsNumericUpDown = null!;
    private CollapsibleGroupBox processingGroupBox = null!;
    private TableLayoutPanel processingTableLayoutPanel = null!;
    private CheckedListBox imageProcessingCheckedListBox = null!;
    private FlowLayoutPanel imageProcessingOrderButtonPanel = null!;
    private Button moveImageProcessingUpButton = null!;
    private Button moveImageProcessingDownButton = null!;
    private CheckBox grayscaleCheckBox = null!;
    private Label hueLabel = null!;
    private Panel hueEditorPanel = null!;
    private NumericUpDown hueNumericUpDown = null!;
    private Label saturationLabel = null!;
    private Panel saturationEditorPanel = null!;
    private NumericUpDown saturationNumericUpDown = null!;
    private Label valueLabel = null!;
    private Panel valueEditorPanel = null!;
    private NumericUpDown valueNumericUpDown = null!;
    private CheckBox invertCheckBox = null!;
    private CheckBox reduceColorsCheckBox = null!;
    private Label colorLevelsLabel = null!;
    private Panel colorLevelsEditorPanel = null!;
    private NumericUpDown colorLevelsNumericUpDown = null!;
    private Label edgeThresholdLabel = null!;
    private NumericUpDown edgeThresholdNumericUpDown = null!;
    private Label edgeStrengthLabel = null!;
    private NumericUpDown edgeStrengthNumericUpDown = null!;
    private Label edgeSmoothingLabel = null!;
    private NumericUpDown edgeSmoothingNumericUpDown = null!;
    private Label binarizationThresholdLabel = null!;
    private NumericUpDown binarizationThresholdNumericUpDown = null!;
    private CheckBox binarizationInvertCheckBox = null!;
    private Label gaussianBlurRadiusLabel = null!;
    private NumericUpDown gaussianBlurRadiusNumericUpDown = null!;
    private CollapsibleGroupBox depthGroupBox = null!;
    private TableLayoutPanel depthTableLayoutPanel = null!;
    private CheckBox useDepthImageFileCheckBox = null!;
    private FlowLayoutPanel depthImageFileFlowLayoutPanel = null!;
    private TextBox depthImageFilePathTextBox = null!;
    private Button browseDepthImageFileButton = null!;
    private CheckBox aiDepthCheckBox = null!;
    private CheckBox blendDepthCheckBox = null!;
    private Label aiDepthWeightLabel = null!;
    private Panel aiDepthWeightEditorPanel = null!;
    private NumericUpDown aiDepthWeightNumericUpDown = null!;
    private Label depthCurveLabel = null!;
    private Panel depthCurveEditorPanel = null!;
    private NumericUpDown depthCurveNumericUpDown = null!;
    private Label localDetailLabel = null!;
    private Panel localDetailEditorPanel = null!;
    private NumericUpDown localDetailNumericUpDown = null!;
    private Label portraitGeometryLabel = null!;
    private Panel portraitGeometryEditorPanel = null!;
    private NumericUpDown portraitGeometryNumericUpDown = null!;
    private CheckBox portraitAnalysisCheckBox = null!;
    private CheckBox symmetryCheckBox = null!;
    private Label symmetryAxisLabel = null!;
    private Panel symmetryAxisEditorPanel = null!;
    private NumericUpDown symmetryAxisNumericUpDown = null!;
    private Label depthNoticeLabel = null!;
    private CollapsibleGroupBox portraitLayersGroupBox = null!;
    private TableLayoutPanel portraitLayersTableLayoutPanel = null!;
    private Label portraitLevelLabel = null!;
    private ComboBox portraitLevelComboBox = null!;
    private Label glassesReliefLabel = null!;
    private Panel glassesReliefEditorPanel = null!;
    private NumericUpDown glassesReliefNumericUpDown = null!;
    private Label hairDetailLabel = null!;
    private Panel hairDetailEditorPanel = null!;
    private NumericUpDown hairDetailNumericUpDown = null!;
    private Label surfaceNormalDetailLabel = null!;
    private Panel surfaceNormalDetailEditorPanel = null!;
    private NumericUpDown surfaceNormalDetailNumericUpDown = null!;
    private Label facialFeatureContourLabel = null!;
    private Panel facialFeatureContourEditorPanel = null!;
    private NumericUpDown facialFeatureContourNumericUpDown = null!;
    private Label facialDepthContrastLabel = null!;
    private Panel facialDepthContrastEditorPanel = null!;
    private NumericUpDown facialDepthContrastNumericUpDown = null!;
    private Label facialMicroDetailLabel = null!;
    private Panel facialMicroDetailEditorPanel = null!;
    private NumericUpDown facialMicroDetailNumericUpDown = null!;
    private CheckBox autoPortraitCropCheckBox = null!;
    private CheckBox bodyAnalysisCheckBox = null!;
    private CheckBox fullBodySegmentationCheckBox = null!;
    private Label bodyLevelLabel = null!;
    private ComboBox bodyLevelComboBox = null!;
    private Label fullBodyDepthLabel = null!;
    private Panel fullBodyDepthEditorPanel = null!;
    private NumericUpDown fullBodyDepthNumericUpDown = null!;
    private Label bodyMaskCleanupLabel = null!;
    private Panel bodyMaskCleanupEditorPanel = null!;
    private NumericUpDown bodyMaskCleanupNumericUpDown = null!;
    private Label backgroundSuppressionLabel = null!;
    private Panel backgroundSuppressionEditorPanel = null!;
    private NumericUpDown backgroundSuppressionNumericUpDown = null!;
    private CheckBox bustSilhouetteCheckBox = null!;
    private TabControl previewTabControl = null!;
    private TabPage originalImageTabPage = null!;
    private Panel originalImagePanel = null!;
    private PictureBox originalImagePictureBox = null!;
    private Label originalImagePlaceholderLabel = null!;
    private TabPage processedImageTabPage = null!;
    private TableLayoutPanel processedImageLayoutPanel = null!;
    private FlowLayoutPanel processedImageToolPanel = null!;
    private Label depthPreviewModeLabel = null!;
    private ComboBox depthPreviewModeComboBox = null!;
    private Panel processedImagePanel = null!;
    private PictureBox processedImagePictureBox = null!;
    private Label processedImagePlaceholderLabel = null!;
    private Button saveDepthMapButton = null!;
    private TabPage modelPreviewTabPage = null!;
    private TableLayoutPanel modelPreviewLayoutPanel = null!;
    private FlowLayoutPanel environmentLightPanel = null!;
    private Label environmentLightLabel = null!;
    private TrackBar environmentLightTrackBar = null!;
    private NumericUpDown environmentLightNumericUpDown = null!;
    private CheckBox wireframeCheckBox = null!;
    private OpenTK.GLControl.GLControl previewGlControl = null!;
    private FlowLayoutPanel commandFlowLayoutPanel = null!;
    private Button resetButton = null!;
    private Button regenerateButton = null!;
    private Button closeButton = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private OpenFileDialog imageOpenFileDialog = null!;
    private OpenFileDialog depthImageOpenFileDialog = null!;
    private SaveFileDialog depthMapSaveFileDialog = null!;
    private OpenFileDialog parameterProfileOpenFileDialog = null!;
    private SaveFileDialog parameterProfileSaveFileDialog = null!;
    private System.Windows.Forms.Timer processingDebounceTimer = null!;
    private System.Windows.Forms.Timer modelGenerationDebounceTimer = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        unifiedMenuStrip = new Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip();
        rootLayoutPanel = new TableLayoutPanel();
        workspaceSplitContainer = new SplitContainer();
        parameterScrollPanel = new Panel();
        parameterTableLayoutPanel = new TableLayoutPanel();
        expandAllGroupsCheckBox = new CheckBox();
        sourceGroupBox = new CollapsibleGroupBox();
        sourceTableLayoutPanel = new TableLayoutPanel();
        sourcePathTextBox = new TextBox();
        openImageButton = new Button();
        parameterProfileLabel = new Label();
        parameterProfileComboBox = new ComboBox();
        parameterProfileButtonFlowLayoutPanel = new FlowLayoutPanel();
        updateParameterProfileButton = new Button();
        addParameterProfileButton = new Button();
        renameParameterProfileButton = new Button();
        geometryGroupBox = new CollapsibleGroupBox();
        geometryTableLayoutPanel = new TableLayoutPanel();
        widthLabel = new Label();
        widthEditorPanel = new Panel();
        widthNumericUpDown = new NumericUpDown();
        thicknessLabel = new Label();
        thicknessEditorPanel = new Panel();
        thicknessNumericUpDown = new NumericUpDown();
        borderWidthLabel = new Label();
        borderWidthEditorPanel = new Panel();
        borderWidthNumericUpDown = new NumericUpDown();
        reliefHeightLabel = new Label();
        reliefHeightEditorPanel = new Panel();
        reliefHeightNumericUpDown = new NumericUpDown();
        qualityLabel = new Label();
        qualityComboBox = new ComboBox();
        modelPlaneLabel = new Label();
        modelPlaneFlowLayoutPanel = new FlowLayoutPanel();
        xyPlaneRadioButton = new RadioButton();
        yzPlaneRadioButton = new RadioButton();
        xzPlaneRadioButton = new RadioButton();
        modelRotationAngleLabel = new Label();
        modelRotationAngleNumericUpDown = new NumericUpDown();
        modelAlignmentLabel = new Label();
        modelAlignmentFlowLayoutPanel = new FlowLayoutPanel();
        centerAlignmentRadioButton = new RadioButton();
        leftBottomAlignmentRadioButton = new RadioButton();
        simplifyModelCheckBox = new CheckBox();
        simplificationTargetLabel = new Label();
        simplificationTargetNumericUpDown = new NumericUpDown();
        simplificationNormalAngleLabel = new Label();
        simplificationNormalAngleNumericUpDown = new NumericUpDown();
        smoothingGroupBox = new CollapsibleGroupBox();
        smoothingTableLayoutPanel = new TableLayoutPanel();
        smoothingCheckBox = new CheckBox();
        smoothingThresholdLabel = new Label();
        smoothingThresholdEditorPanel = new Panel();
        smoothingThresholdNumericUpDown = new NumericUpDown();
        smoothingStrengthLabel = new Label();
        smoothingStrengthEditorPanel = new Panel();
        smoothingStrengthNumericUpDown = new NumericUpDown();
        smoothingIterationsLabel = new Label();
        smoothingIterationsEditorPanel = new Panel();
        smoothingIterationsNumericUpDown = new NumericUpDown();
        processingGroupBox = new CollapsibleGroupBox();
        processingTableLayoutPanel = new TableLayoutPanel();
        imageProcessingCheckedListBox = new CheckedListBox();
        imageProcessingOrderButtonPanel = new FlowLayoutPanel();
        moveImageProcessingUpButton = new Button();
        moveImageProcessingDownButton = new Button();
        grayscaleCheckBox = new CheckBox();
        hueLabel = new Label();
        hueEditorPanel = new Panel();
        hueNumericUpDown = new NumericUpDown();
        saturationLabel = new Label();
        saturationEditorPanel = new Panel();
        saturationNumericUpDown = new NumericUpDown();
        valueLabel = new Label();
        valueEditorPanel = new Panel();
        valueNumericUpDown = new NumericUpDown();
        invertCheckBox = new CheckBox();
        reduceColorsCheckBox = new CheckBox();
        colorLevelsLabel = new Label();
        colorLevelsEditorPanel = new Panel();
        colorLevelsNumericUpDown = new NumericUpDown();
        edgeThresholdLabel = new Label();
        edgeThresholdNumericUpDown = new NumericUpDown();
        edgeStrengthLabel = new Label();
        edgeStrengthNumericUpDown = new NumericUpDown();
        edgeSmoothingLabel = new Label();
        edgeSmoothingNumericUpDown = new NumericUpDown();
        binarizationThresholdLabel = new Label();
        binarizationThresholdNumericUpDown = new NumericUpDown();
        binarizationInvertCheckBox = new CheckBox();
        gaussianBlurRadiusLabel = new Label();
        gaussianBlurRadiusNumericUpDown = new NumericUpDown();
        depthGroupBox = new CollapsibleGroupBox();
        depthTableLayoutPanel = new TableLayoutPanel();
        useDepthImageFileCheckBox = new CheckBox();
        depthImageFileFlowLayoutPanel = new FlowLayoutPanel();
        depthImageFilePathTextBox = new TextBox();
        browseDepthImageFileButton = new Button();
        aiDepthCheckBox = new CheckBox();
        blendDepthCheckBox = new CheckBox();
        aiDepthWeightLabel = new Label();
        aiDepthWeightEditorPanel = new Panel();
        aiDepthWeightNumericUpDown = new NumericUpDown();
        depthCurveLabel = new Label();
        depthCurveEditorPanel = new Panel();
        depthCurveNumericUpDown = new NumericUpDown();
        localDetailLabel = new Label();
        localDetailEditorPanel = new Panel();
        localDetailNumericUpDown = new NumericUpDown();
        portraitGeometryLabel = new Label();
        portraitGeometryEditorPanel = new Panel();
        portraitGeometryNumericUpDown = new NumericUpDown();
        portraitAnalysisCheckBox = new CheckBox();
        symmetryCheckBox = new CheckBox();
        symmetryAxisLabel = new Label();
        symmetryAxisEditorPanel = new Panel();
        symmetryAxisNumericUpDown = new NumericUpDown();
        depthNoticeLabel = new Label();
        portraitLayersGroupBox = new CollapsibleGroupBox();
        portraitLayersTableLayoutPanel = new TableLayoutPanel();
        portraitLevelLabel = new Label();
        portraitLevelComboBox = new ComboBox();
        glassesReliefLabel = new Label();
        glassesReliefEditorPanel = new Panel();
        glassesReliefNumericUpDown = new NumericUpDown();
        hairDetailLabel = new Label();
        hairDetailEditorPanel = new Panel();
        hairDetailNumericUpDown = new NumericUpDown();
        surfaceNormalDetailLabel = new Label();
        surfaceNormalDetailEditorPanel = new Panel();
        surfaceNormalDetailNumericUpDown = new NumericUpDown();
        facialFeatureContourLabel = new Label();
        facialFeatureContourEditorPanel = new Panel();
        facialFeatureContourNumericUpDown = new NumericUpDown();
        facialDepthContrastLabel = new Label();
        facialDepthContrastEditorPanel = new Panel();
        facialDepthContrastNumericUpDown = new NumericUpDown();
        facialMicroDetailLabel = new Label();
        facialMicroDetailEditorPanel = new Panel();
        facialMicroDetailNumericUpDown = new NumericUpDown();
        backgroundSuppressionLabel = new Label();
        backgroundSuppressionEditorPanel = new Panel();
        backgroundSuppressionNumericUpDown = new NumericUpDown();
        autoPortraitCropCheckBox = new CheckBox();
        bustSilhouetteCheckBox = new CheckBox();
        fullBodySegmentationCheckBox = new CheckBox();
        bodyLevelLabel = new Label();
        bodyLevelComboBox = new ComboBox();
        fullBodyDepthLabel = new Label();
        fullBodyDepthEditorPanel = new Panel();
        fullBodyDepthNumericUpDown = new NumericUpDown();
        bodyMaskCleanupLabel = new Label();
        bodyMaskCleanupEditorPanel = new Panel();
        bodyMaskCleanupNumericUpDown = new NumericUpDown();
        bodyAnalysisCheckBox = new CheckBox();
        previewTabControl = new TabControl();
        originalImageTabPage = new TabPage();
        originalImagePanel = new Panel();
        originalImagePlaceholderLabel = new Label();
        originalImagePictureBox = new PictureBox();
        processedImageTabPage = new TabPage();
        processedImageLayoutPanel = new TableLayoutPanel();
        processedImageToolPanel = new FlowLayoutPanel();
        saveDepthMapButton = new Button();
        depthPreviewModeLabel = new Label();
        depthPreviewModeComboBox = new ComboBox();
        processedImagePanel = new Panel();
        processedImagePlaceholderLabel = new Label();
        processedImagePictureBox = new PictureBox();
        modelPreviewTabPage = new TabPage();
        modelPreviewLayoutPanel = new TableLayoutPanel();
        environmentLightPanel = new FlowLayoutPanel();
        environmentLightLabel = new Label();
        environmentLightTrackBar = new TrackBar();
        environmentLightNumericUpDown = new NumericUpDown();
        wireframeCheckBox = new CheckBox();
        textureCheckBox = new CheckBox();
        previewGlControl = new OpenTK.GLControl.GLControl();
        commandFlowLayoutPanel = new FlowLayoutPanel();
        closeButton = new Button();
        regenerateButton = new Button();
        resetButton = new Button();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        imageOpenFileDialog = new OpenFileDialog();
        depthImageOpenFileDialog = new OpenFileDialog();
        depthMapSaveFileDialog = new SaveFileDialog();
        parameterProfileOpenFileDialog = new OpenFileDialog();
        parameterProfileSaveFileDialog = new SaveFileDialog();
        processingDebounceTimer = new System.Windows.Forms.Timer(components);
        modelGenerationDebounceTimer = new System.Windows.Forms.Timer(components);
        rootLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)workspaceSplitContainer).BeginInit();
        workspaceSplitContainer.Panel1.SuspendLayout();
        workspaceSplitContainer.Panel2.SuspendLayout();
        workspaceSplitContainer.SuspendLayout();
        parameterScrollPanel.SuspendLayout();
        parameterTableLayoutPanel.SuspendLayout();
        sourceGroupBox.SuspendLayout();
        sourceTableLayoutPanel.SuspendLayout();
        parameterProfileButtonFlowLayoutPanel.SuspendLayout();
        geometryGroupBox.SuspendLayout();
        geometryTableLayoutPanel.SuspendLayout();
        widthEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)widthNumericUpDown).BeginInit();
        thicknessEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)thicknessNumericUpDown).BeginInit();
        borderWidthEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)borderWidthNumericUpDown).BeginInit();
        reliefHeightEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)reliefHeightNumericUpDown).BeginInit();
        modelPlaneFlowLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)modelRotationAngleNumericUpDown).BeginInit();
        modelAlignmentFlowLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)simplificationTargetNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)simplificationNormalAngleNumericUpDown).BeginInit();
        smoothingGroupBox.SuspendLayout();
        smoothingTableLayoutPanel.SuspendLayout();
        smoothingThresholdEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)smoothingThresholdNumericUpDown).BeginInit();
        smoothingStrengthEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)smoothingStrengthNumericUpDown).BeginInit();
        smoothingIterationsEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)smoothingIterationsNumericUpDown).BeginInit();
        processingGroupBox.SuspendLayout();
        processingTableLayoutPanel.SuspendLayout();
        imageProcessingOrderButtonPanel.SuspendLayout();
        hueEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)hueNumericUpDown).BeginInit();
        saturationEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)saturationNumericUpDown).BeginInit();
        valueEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)valueNumericUpDown).BeginInit();
        colorLevelsEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)colorLevelsNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)edgeThresholdNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)edgeStrengthNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)edgeSmoothingNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)binarizationThresholdNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)gaussianBlurRadiusNumericUpDown).BeginInit();
        depthGroupBox.SuspendLayout();
        depthTableLayoutPanel.SuspendLayout();
        depthImageFileFlowLayoutPanel.SuspendLayout();
        aiDepthWeightEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)aiDepthWeightNumericUpDown).BeginInit();
        depthCurveEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)depthCurveNumericUpDown).BeginInit();
        localDetailEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)localDetailNumericUpDown).BeginInit();
        portraitGeometryEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)portraitGeometryNumericUpDown).BeginInit();
        symmetryAxisEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)symmetryAxisNumericUpDown).BeginInit();
        portraitLayersGroupBox.SuspendLayout();
        portraitLayersTableLayoutPanel.SuspendLayout();
        glassesReliefEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)glassesReliefNumericUpDown).BeginInit();
        hairDetailEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)hairDetailNumericUpDown).BeginInit();
        surfaceNormalDetailEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)surfaceNormalDetailNumericUpDown).BeginInit();
        facialFeatureContourEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)facialFeatureContourNumericUpDown).BeginInit();
        facialDepthContrastEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)facialDepthContrastNumericUpDown).BeginInit();
        facialMicroDetailEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)facialMicroDetailNumericUpDown).BeginInit();
        backgroundSuppressionEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)backgroundSuppressionNumericUpDown).BeginInit();
        fullBodyDepthEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)fullBodyDepthNumericUpDown).BeginInit();
        bodyMaskCleanupEditorPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)bodyMaskCleanupNumericUpDown).BeginInit();
        previewTabControl.SuspendLayout();
        originalImageTabPage.SuspendLayout();
        originalImagePanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)originalImagePictureBox).BeginInit();
        processedImageTabPage.SuspendLayout();
        processedImageLayoutPanel.SuspendLayout();
        processedImageToolPanel.SuspendLayout();
        processedImagePanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)processedImagePictureBox).BeginInit();
        modelPreviewTabPage.SuspendLayout();
        modelPreviewLayoutPanel.SuspendLayout();
        environmentLightPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)environmentLightTrackBar).BeginInit();
        ((System.ComponentModel.ISupportInitialize)environmentLightNumericUpDown).BeginInit();
        commandFlowLayoutPanel.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // unifiedMenuStrip
        // 
        unifiedMenuStrip.ImageScalingSize = new Size(24, 24);
        unifiedMenuStrip.Location = new Point(0, 0);
        unifiedMenuStrip.Name = "unifiedMenuStrip";
        unifiedMenuStrip.Size = new Size(1861, 31);
        unifiedMenuStrip.TabIndex = 1;
        // 
        // rootLayoutPanel
        // 
        rootLayoutPanel.ColumnCount = 1;
        rootLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayoutPanel.Controls.Add(workspaceSplitContainer, 0, 0);
        rootLayoutPanel.Controls.Add(commandFlowLayoutPanel, 0, 1);
        rootLayoutPanel.Controls.Add(statusStrip, 0, 2);
        rootLayoutPanel.Dock = DockStyle.Fill;
        rootLayoutPanel.Location = new Point(0, 31);
        rootLayoutPanel.Margin = new Padding(5);
        rootLayoutPanel.Name = "rootLayoutPanel";
        rootLayoutPanel.RowCount = 3;
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.Size = new Size(1861, 1136);
        rootLayoutPanel.TabIndex = 0;
        // 
        // workspaceSplitContainer
        // 
        workspaceSplitContainer.Dock = DockStyle.Fill;
        workspaceSplitContainer.FixedPanel = FixedPanel.Panel1;
        workspaceSplitContainer.Location = new Point(5, 5);
        workspaceSplitContainer.Margin = new Padding(5);
        workspaceSplitContainer.Name = "workspaceSplitContainer";
        // 
        // workspaceSplitContainer.Panel1
        // 
        workspaceSplitContainer.Panel1.Controls.Add(parameterScrollPanel);
        workspaceSplitContainer.Panel1MinSize = 300;
        // 
        // workspaceSplitContainer.Panel2
        // 
        workspaceSplitContainer.Panel2.Controls.Add(previewTabControl);
        workspaceSplitContainer.Panel2MinSize = 480;
        workspaceSplitContainer.Size = new Size(1851, 1022);
        workspaceSplitContainer.SplitterDistance = 519;
        workspaceSplitContainer.SplitterWidth = 6;
        workspaceSplitContainer.TabIndex = 0;
        // 
        // parameterScrollPanel
        // 
        parameterScrollPanel.AutoScroll = true;
        parameterScrollPanel.Controls.Add(parameterTableLayoutPanel);
        parameterScrollPanel.Dock = DockStyle.Fill;
        parameterScrollPanel.Location = new Point(0, 0);
        parameterScrollPanel.Margin = new Padding(5);
        parameterScrollPanel.Name = "parameterScrollPanel";
        parameterScrollPanel.Padding = new Padding(13, 12, 13, 12);
        parameterScrollPanel.Size = new Size(519, 1022);
        parameterScrollPanel.TabIndex = 0;
        // 
        // parameterTableLayoutPanel
        // 
        parameterTableLayoutPanel.AutoSize = true;
        parameterTableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        parameterTableLayoutPanel.ColumnCount = 1;
        parameterTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        parameterTableLayoutPanel.Controls.Add(sourceGroupBox, 0, 0);
        parameterTableLayoutPanel.Controls.Add(expandAllGroupsCheckBox, 0, 1);
        parameterTableLayoutPanel.Controls.Add(geometryGroupBox, 0, 2);
        parameterTableLayoutPanel.Controls.Add(smoothingGroupBox, 0, 3);
        parameterTableLayoutPanel.Controls.Add(processingGroupBox, 0, 4);
        parameterTableLayoutPanel.Controls.Add(depthGroupBox, 0, 5);
        parameterTableLayoutPanel.Controls.Add(portraitLayersGroupBox, 0, 6);
        parameterTableLayoutPanel.Dock = DockStyle.Top;
        parameterTableLayoutPanel.Location = new Point(13, 12);
        parameterTableLayoutPanel.Margin = new Padding(5);
        parameterTableLayoutPanel.Name = "parameterTableLayoutPanel";
        parameterTableLayoutPanel.RowCount = 7;
        parameterTableLayoutPanel.RowStyles.Add(new RowStyle());
        parameterTableLayoutPanel.RowStyles.Add(new RowStyle());
        parameterTableLayoutPanel.RowStyles.Add(new RowStyle());
        parameterTableLayoutPanel.RowStyles.Add(new RowStyle());
        parameterTableLayoutPanel.RowStyles.Add(new RowStyle());
        parameterTableLayoutPanel.RowStyles.Add(new RowStyle());
        parameterTableLayoutPanel.RowStyles.Add(new RowStyle());
        parameterTableLayoutPanel.Size = new Size(467, 2808);
        parameterTableLayoutPanel.TabIndex = 0;
        // 
        // expandAllGroupsCheckBox
        // 
        expandAllGroupsCheckBox.AutoSize = true;
        expandAllGroupsCheckBox.Checked = true;
        expandAllGroupsCheckBox.CheckState = CheckState.Checked;
        expandAllGroupsCheckBox.Location = new Point(5, 282);
        expandAllGroupsCheckBox.Margin = new Padding(5);
        expandAllGroupsCheckBox.Name = "expandAllGroupsCheckBox";
        expandAllGroupsCheckBox.Size = new Size(108, 27);
        expandAllGroupsCheckBox.TabIndex = 1;
        expandAllGroupsCheckBox.Text = "開合全部";
        expandAllGroupsCheckBox.ThreeState = true;
        expandAllGroupsCheckBox.UseVisualStyleBackColor = true;
        expandAllGroupsCheckBox.CheckStateChanged += ExpandAllGroupsCheckBox_CheckStateChanged;
        // 
        // sourceGroupBox
        // 
        sourceGroupBox.AutoSize = true;
        sourceGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        sourceGroupBox.Controls.Add(sourceTableLayoutPanel);
        sourceGroupBox.Dock = DockStyle.Top;
        sourceGroupBox.Expanded = true;
        sourceGroupBox.Location = new Point(5, 5);
        sourceGroupBox.Margin = new Padding(5);
        sourceGroupBox.Name = "sourceGroupBox";
        sourceGroupBox.Padding = new Padding(13, 12, 13, 12);
        sourceGroupBox.Size = new Size(457, 267);
        sourceGroupBox.TabIndex = 0;
        sourceGroupBox.TabStop = false;
        sourceGroupBox.Text = "來源圖檔";
        sourceGroupBox.ExpandedChanged += CollapsibleGroupBox_ExpandedChanged;
        // 
        // sourceTableLayoutPanel
        // 
        sourceTableLayoutPanel.AutoSize = true;
        sourceTableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        sourceTableLayoutPanel.ColumnCount = 1;
        sourceTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        sourceTableLayoutPanel.Controls.Add(sourcePathTextBox, 0, 0);
        sourceTableLayoutPanel.Controls.Add(openImageButton, 0, 1);
        sourceTableLayoutPanel.Controls.Add(parameterProfileLabel, 0, 2);
        sourceTableLayoutPanel.Controls.Add(parameterProfileComboBox, 0, 3);
        sourceTableLayoutPanel.Controls.Add(parameterProfileButtonFlowLayoutPanel, 0, 4);
        sourceTableLayoutPanel.Dock = DockStyle.Top;
        sourceTableLayoutPanel.Location = new Point(13, 35);
        sourceTableLayoutPanel.Margin = new Padding(5);
        sourceTableLayoutPanel.Name = "sourceTableLayoutPanel";
        sourceTableLayoutPanel.RowCount = 5;
        sourceTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        sourceTableLayoutPanel.RowStyles.Add(new RowStyle());
        sourceTableLayoutPanel.RowStyles.Add(new RowStyle());
        sourceTableLayoutPanel.RowStyles.Add(new RowStyle());
        sourceTableLayoutPanel.RowStyles.Add(new RowStyle());
        sourceTableLayoutPanel.Size = new Size(431, 220);
        sourceTableLayoutPanel.TabIndex = 0;
        // 
        // sourcePathTextBox
        // 
        sourcePathTextBox.Dock = DockStyle.Fill;
        sourcePathTextBox.Location = new Point(5, 5);
        sourcePathTextBox.Margin = new Padding(5);
        sourcePathTextBox.Name = "sourcePathTextBox";
        sourcePathTextBox.PlaceholderText = "尚未載入圖檔";
        sourcePathTextBox.ReadOnly = true;
        sourcePathTextBox.Size = new Size(421, 30);
        sourcePathTextBox.TabIndex = 0;
        // 
        // openImageButton
        // 
        openImageButton.Anchor = AnchorStyles.Right;
        openImageButton.AutoSize = true;
        openImageButton.Location = new Point(259, 45);
        openImageButton.Margin = new Padding(5);
        openImageButton.Name = "openImageButton";
        openImageButton.Size = new Size(167, 51);
        openImageButton.TabIndex = 1;
        openImageButton.Text = "讀取圖檔…";
        openImageButton.UseVisualStyleBackColor = true;
        openImageButton.Click += OpenImageButton_Click;
        // 
        // parameterProfileLabel
        // 
        parameterProfileLabel.AutoSize = true;
        parameterProfileLabel.Location = new Point(5, 111);
        parameterProfileLabel.Margin = new Padding(5, 10, 5, 2);
        parameterProfileLabel.Name = "parameterProfileLabel";
        parameterProfileLabel.Size = new Size(82, 23);
        parameterProfileLabel.TabIndex = 2;
        parameterProfileLabel.Text = "設定清單";
        // 
        // parameterProfileComboBox
        // 
        parameterProfileComboBox.Dock = DockStyle.Fill;
        parameterProfileComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        parameterProfileComboBox.Location = new Point(5, 141);
        parameterProfileComboBox.Margin = new Padding(5);
        parameterProfileComboBox.Name = "parameterProfileComboBox";
        parameterProfileComboBox.Size = new Size(421, 31);
        parameterProfileComboBox.TabIndex = 3;
        parameterProfileComboBox.SelectedIndexChanged += ParameterProfileComboBox_SelectedIndexChanged;
        // 
        // parameterProfileButtonFlowLayoutPanel
        // 
        parameterProfileButtonFlowLayoutPanel.AutoSize = true;
        parameterProfileButtonFlowLayoutPanel.Controls.Add(updateParameterProfileButton);
        parameterProfileButtonFlowLayoutPanel.Controls.Add(addParameterProfileButton);
        parameterProfileButtonFlowLayoutPanel.Controls.Add(renameParameterProfileButton);
        parameterProfileButtonFlowLayoutPanel.Dock = DockStyle.Fill;
        parameterProfileButtonFlowLayoutPanel.Location = new Point(0, 177);
        parameterProfileButtonFlowLayoutPanel.Margin = new Padding(0);
        parameterProfileButtonFlowLayoutPanel.Name = "parameterProfileButtonFlowLayoutPanel";
        parameterProfileButtonFlowLayoutPanel.Size = new Size(431, 43);
        parameterProfileButtonFlowLayoutPanel.TabIndex = 4;
        parameterProfileButtonFlowLayoutPanel.WrapContents = false;
        // 
        // updateParameterProfileButton
        // 
        updateParameterProfileButton.AutoSize = true;
        updateParameterProfileButton.Enabled = false;
        updateParameterProfileButton.Location = new Point(5, 5);
        updateParameterProfileButton.Margin = new Padding(5);
        updateParameterProfileButton.Name = "updateParameterProfileButton";
        updateParameterProfileButton.Size = new Size(80, 33);
        updateParameterProfileButton.TabIndex = 0;
        updateParameterProfileButton.Text = "更新";
        updateParameterProfileButton.UseVisualStyleBackColor = true;
        updateParameterProfileButton.Click += UpdateParameterProfileButton_Click;
        // 
        // addParameterProfileButton
        // 
        addParameterProfileButton.AutoSize = true;
        addParameterProfileButton.Location = new Point(95, 5);
        addParameterProfileButton.Margin = new Padding(5);
        addParameterProfileButton.Name = "addParameterProfileButton";
        addParameterProfileButton.Size = new Size(80, 33);
        addParameterProfileButton.TabIndex = 1;
        addParameterProfileButton.Text = "新增";
        addParameterProfileButton.UseVisualStyleBackColor = true;
        addParameterProfileButton.Click += AddParameterProfileButton_Click;
        // 
        // renameParameterProfileButton
        // 
        renameParameterProfileButton.AutoSize = true;
        renameParameterProfileButton.Enabled = false;
        renameParameterProfileButton.Location = new Point(185, 5);
        renameParameterProfileButton.Margin = new Padding(5);
        renameParameterProfileButton.Name = "renameParameterProfileButton";
        renameParameterProfileButton.Size = new Size(80, 33);
        renameParameterProfileButton.TabIndex = 2;
        renameParameterProfileButton.Text = "更名";
        renameParameterProfileButton.UseVisualStyleBackColor = true;
        renameParameterProfileButton.Click += RenameParameterProfileButton_Click;
        // 
        // geometryGroupBox
        // 
        geometryGroupBox.AutoSize = true;
        geometryGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        geometryGroupBox.Controls.Add(geometryTableLayoutPanel);
        geometryGroupBox.Dock = DockStyle.Top;
        geometryGroupBox.Expanded = true;
        geometryGroupBox.Location = new Point(5, 319);
        geometryGroupBox.Margin = new Padding(5);
        geometryGroupBox.Name = "geometryGroupBox";
        geometryGroupBox.Padding = new Padding(13, 12, 13, 12);
        geometryGroupBox.Size = new Size(457, 488);
        geometryGroupBox.TabIndex = 1;
        geometryGroupBox.TabStop = false;
        geometryGroupBox.Text = "模型尺寸";
        geometryGroupBox.ExpandedChanged += CollapsibleGroupBox_ExpandedChanged;
        // 
        // geometryTableLayoutPanel
        // 
        geometryTableLayoutPanel.AutoSize = true;
        geometryTableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        geometryTableLayoutPanel.ColumnCount = 2;
        geometryTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
        geometryTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
        geometryTableLayoutPanel.Controls.Add(widthLabel, 0, 0);
        geometryTableLayoutPanel.Controls.Add(widthEditorPanel, 1, 0);
        geometryTableLayoutPanel.Controls.Add(thicknessLabel, 0, 1);
        geometryTableLayoutPanel.Controls.Add(thicknessEditorPanel, 1, 1);
        geometryTableLayoutPanel.Controls.Add(borderWidthLabel, 0, 2);
        geometryTableLayoutPanel.Controls.Add(borderWidthEditorPanel, 1, 2);
        geometryTableLayoutPanel.Controls.Add(reliefHeightLabel, 0, 3);
        geometryTableLayoutPanel.Controls.Add(reliefHeightEditorPanel, 1, 3);
        geometryTableLayoutPanel.Controls.Add(qualityLabel, 0, 4);
        geometryTableLayoutPanel.Controls.Add(qualityComboBox, 1, 4);
        geometryTableLayoutPanel.Controls.Add(modelPlaneLabel, 0, 5);
        geometryTableLayoutPanel.Controls.Add(modelPlaneFlowLayoutPanel, 1, 5);
        geometryTableLayoutPanel.Controls.Add(modelRotationAngleLabel, 0, 6);
        geometryTableLayoutPanel.Controls.Add(modelRotationAngleNumericUpDown, 1, 6);
        geometryTableLayoutPanel.Controls.Add(modelAlignmentLabel, 0, 7);
        geometryTableLayoutPanel.Controls.Add(modelAlignmentFlowLayoutPanel, 1, 7);
        geometryTableLayoutPanel.Controls.Add(simplifyModelCheckBox, 0, 8);
        geometryTableLayoutPanel.Controls.Add(simplificationTargetLabel, 0, 9);
        geometryTableLayoutPanel.Controls.Add(simplificationTargetNumericUpDown, 1, 9);
        geometryTableLayoutPanel.Controls.Add(simplificationNormalAngleLabel, 0, 10);
        geometryTableLayoutPanel.Controls.Add(simplificationNormalAngleNumericUpDown, 1, 10);
        geometryTableLayoutPanel.Dock = DockStyle.Top;
        geometryTableLayoutPanel.Location = new Point(13, 35);
        geometryTableLayoutPanel.Margin = new Padding(5);
        geometryTableLayoutPanel.Name = "geometryTableLayoutPanel";
        geometryTableLayoutPanel.RowCount = 11;
        geometryTableLayoutPanel.RowStyles.Add(new RowStyle());
        geometryTableLayoutPanel.RowStyles.Add(new RowStyle());
        geometryTableLayoutPanel.RowStyles.Add(new RowStyle());
        geometryTableLayoutPanel.RowStyles.Add(new RowStyle());
        geometryTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66667F));
        geometryTableLayoutPanel.RowStyles.Add(new RowStyle());
        geometryTableLayoutPanel.RowStyles.Add(new RowStyle());
        geometryTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66667F));
        geometryTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66667F));
        geometryTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66667F));
        geometryTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66667F));
        geometryTableLayoutPanel.Size = new Size(431, 441);
        geometryTableLayoutPanel.TabIndex = 0;
        // 
        // widthLabel
        // 
        widthLabel.Anchor = AnchorStyles.Left;
        widthLabel.AutoSize = true;
        widthLabel.Location = new Point(5, 9);
        widthLabel.Margin = new Padding(5, 0, 5, 0);
        widthLabel.Name = "widthLabel";
        widthLabel.Size = new Size(133, 23);
        widthLabel.TabIndex = 0;
        widthLabel.Text = "成品總寬 (mm)";
        // 
        // widthEditorPanel
        // 
        widthEditorPanel.Controls.Add(widthNumericUpDown);
        widthEditorPanel.Dock = DockStyle.Fill;
        widthEditorPanel.Location = new Point(241, 0);
        widthEditorPanel.Margin = new Padding(0);
        widthEditorPanel.Name = "widthEditorPanel";
        widthEditorPanel.Size = new Size(190, 41);
        widthEditorPanel.TabIndex = 1;
        // 
        // widthNumericUpDown
        // 
        widthNumericUpDown.DecimalPlaces = 1;
        widthNumericUpDown.Dock = DockStyle.Top;
        widthNumericUpDown.Location = new Point(0, 0);
        widthNumericUpDown.Margin = new Padding(5);
        widthNumericUpDown.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
        widthNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        widthNumericUpDown.Name = "widthNumericUpDown";
        widthNumericUpDown.Size = new Size(190, 30);
        widthNumericUpDown.TabIndex = 0;
        widthNumericUpDown.Value = new decimal(new int[] { 100, 0, 0, 0 });
        widthNumericUpDown.ValueChanged += GeometryParameter_Changed;
        // 
        // thicknessLabel
        // 
        thicknessLabel.Anchor = AnchorStyles.Left;
        thicknessLabel.AutoSize = true;
        thicknessLabel.Location = new Point(5, 50);
        thicknessLabel.Margin = new Padding(5, 0, 5, 0);
        thicknessLabel.Name = "thicknessLabel";
        thicknessLabel.Size = new Size(133, 23);
        thicknessLabel.TabIndex = 2;
        thicknessLabel.Text = "成品厚度 (mm)";
        // 
        // thicknessEditorPanel
        // 
        thicknessEditorPanel.Controls.Add(thicknessNumericUpDown);
        thicknessEditorPanel.Dock = DockStyle.Fill;
        thicknessEditorPanel.Location = new Point(241, 41);
        thicknessEditorPanel.Margin = new Padding(0);
        thicknessEditorPanel.Name = "thicknessEditorPanel";
        thicknessEditorPanel.Size = new Size(190, 41);
        thicknessEditorPanel.TabIndex = 3;
        // 
        // thicknessNumericUpDown
        // 
        thicknessNumericUpDown.DecimalPlaces = 1;
        thicknessNumericUpDown.Dock = DockStyle.Top;
        thicknessNumericUpDown.Location = new Point(0, 0);
        thicknessNumericUpDown.Margin = new Padding(5);
        thicknessNumericUpDown.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        thicknessNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
        thicknessNumericUpDown.Name = "thicknessNumericUpDown";
        thicknessNumericUpDown.Size = new Size(190, 30);
        thicknessNumericUpDown.TabIndex = 0;
        thicknessNumericUpDown.Value = new decimal(new int[] { 10, 0, 0, 0 });
        thicknessNumericUpDown.ValueChanged += GeometryParameter_Changed;
        // 
        // borderWidthLabel
        // 
        borderWidthLabel.Anchor = AnchorStyles.Left;
        borderWidthLabel.AutoSize = true;
        borderWidthLabel.Location = new Point(5, 91);
        borderWidthLabel.Margin = new Padding(5, 0, 5, 0);
        borderWidthLabel.Name = "borderWidthLabel";
        borderWidthLabel.Size = new Size(133, 23);
        borderWidthLabel.TabIndex = 4;
        borderWidthLabel.Text = "邊框寬度 (mm)";
        // 
        // borderWidthEditorPanel
        // 
        borderWidthEditorPanel.Controls.Add(borderWidthNumericUpDown);
        borderWidthEditorPanel.Dock = DockStyle.Fill;
        borderWidthEditorPanel.Location = new Point(241, 82);
        borderWidthEditorPanel.Margin = new Padding(0);
        borderWidthEditorPanel.Name = "borderWidthEditorPanel";
        borderWidthEditorPanel.Size = new Size(190, 41);
        borderWidthEditorPanel.TabIndex = 5;
        // 
        // borderWidthNumericUpDown
        // 
        borderWidthNumericUpDown.DecimalPlaces = 1;
        borderWidthNumericUpDown.Dock = DockStyle.Top;
        borderWidthNumericUpDown.Location = new Point(0, 0);
        borderWidthNumericUpDown.Margin = new Padding(5);
        borderWidthNumericUpDown.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        borderWidthNumericUpDown.Name = "borderWidthNumericUpDown";
        borderWidthNumericUpDown.Size = new Size(190, 30);
        borderWidthNumericUpDown.TabIndex = 0;
        borderWidthNumericUpDown.Value = new decimal(new int[] { 3, 0, 0, 0 });
        borderWidthNumericUpDown.ValueChanged += GeometryParameter_Changed;
        // 
        // reliefHeightLabel
        // 
        reliefHeightLabel.Anchor = AnchorStyles.Left;
        reliefHeightLabel.AutoSize = true;
        reliefHeightLabel.Location = new Point(5, 131);
        reliefHeightLabel.Margin = new Padding(5, 0, 5, 0);
        reliefHeightLabel.Name = "reliefHeightLabel";
        reliefHeightLabel.Size = new Size(133, 23);
        reliefHeightLabel.TabIndex = 6;
        reliefHeightLabel.Text = "浮雕高度 (mm)";
        // 
        // reliefHeightEditorPanel
        // 
        reliefHeightEditorPanel.Controls.Add(reliefHeightNumericUpDown);
        reliefHeightEditorPanel.Dock = DockStyle.Fill;
        reliefHeightEditorPanel.Location = new Point(241, 123);
        reliefHeightEditorPanel.Margin = new Padding(0);
        reliefHeightEditorPanel.Name = "reliefHeightEditorPanel";
        reliefHeightEditorPanel.Size = new Size(190, 40);
        reliefHeightEditorPanel.TabIndex = 7;
        // 
        // reliefHeightNumericUpDown
        // 
        reliefHeightNumericUpDown.DecimalPlaces = 1;
        reliefHeightNumericUpDown.Dock = DockStyle.Top;
        reliefHeightNumericUpDown.Location = new Point(0, 0);
        reliefHeightNumericUpDown.Margin = new Padding(5);
        reliefHeightNumericUpDown.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        reliefHeightNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
        reliefHeightNumericUpDown.Name = "reliefHeightNumericUpDown";
        reliefHeightNumericUpDown.Size = new Size(190, 30);
        reliefHeightNumericUpDown.TabIndex = 0;
        reliefHeightNumericUpDown.Value = new decimal(new int[] { 6, 0, 0, 0 });
        reliefHeightNumericUpDown.ValueChanged += GeometryParameter_Changed;
        // 
        // qualityLabel
        // 
        qualityLabel.Anchor = AnchorStyles.Left;
        qualityLabel.AutoSize = true;
        qualityLabel.Location = new Point(5, 171);
        qualityLabel.Margin = new Padding(5, 0, 5, 0);
        qualityLabel.Name = "qualityLabel";
        qualityLabel.Size = new Size(82, 23);
        qualityLabel.TabIndex = 8;
        qualityLabel.Text = "網格品質";
        // 
        // qualityComboBox
        // 
        qualityComboBox.Dock = DockStyle.Fill;
        qualityComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        qualityComboBox.Items.AddRange(new object[] { "草稿 (128)", "標準 (256)", "精細 (512)" });
        qualityComboBox.Location = new Point(246, 168);
        qualityComboBox.Margin = new Padding(5);
        qualityComboBox.Name = "qualityComboBox";
        qualityComboBox.Size = new Size(180, 31);
        qualityComboBox.TabIndex = 9;
        qualityComboBox.SelectedIndexChanged += GeometryParameter_Changed;
        // 
        // modelPlaneLabel
        // 
        modelPlaneLabel.Anchor = AnchorStyles.Left;
        modelPlaneLabel.AutoSize = true;
        modelPlaneLabel.Location = new Point(5, 208);
        modelPlaneLabel.Margin = new Padding(5, 0, 5, 0);
        modelPlaneLabel.Name = "modelPlaneLabel";
        modelPlaneLabel.Size = new Size(82, 23);
        modelPlaneLabel.TabIndex = 10;
        modelPlaneLabel.Text = "模型平面";
        // 
        // modelPlaneFlowLayoutPanel
        // 
        modelPlaneFlowLayoutPanel.AutoSize = true;
        modelPlaneFlowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        modelPlaneFlowLayoutPanel.Controls.Add(xyPlaneRadioButton);
        modelPlaneFlowLayoutPanel.Controls.Add(yzPlaneRadioButton);
        modelPlaneFlowLayoutPanel.Controls.Add(xzPlaneRadioButton);
        modelPlaneFlowLayoutPanel.Dock = DockStyle.Fill;
        modelPlaneFlowLayoutPanel.Location = new Point(241, 203);
        modelPlaneFlowLayoutPanel.Margin = new Padding(0);
        modelPlaneFlowLayoutPanel.Name = "modelPlaneFlowLayoutPanel";
        modelPlaneFlowLayoutPanel.Size = new Size(190, 33);
        modelPlaneFlowLayoutPanel.TabIndex = 11;
        modelPlaneFlowLayoutPanel.WrapContents = false;
        // 
        // xyPlaneRadioButton
        // 
        xyPlaneRadioButton.Anchor = AnchorStyles.Left;
        xyPlaneRadioButton.AutoSize = true;
        xyPlaneRadioButton.Location = new Point(3, 3);
        xyPlaneRadioButton.Name = "xyPlaneRadioButton";
        xyPlaneRadioButton.Size = new Size(57, 27);
        xyPlaneRadioButton.TabIndex = 0;
        xyPlaneRadioButton.Text = "XY";
        xyPlaneRadioButton.CheckedChanged += GeometryParameter_Changed;
        // 
        // yzPlaneRadioButton
        // 
        yzPlaneRadioButton.Anchor = AnchorStyles.Left;
        yzPlaneRadioButton.AutoSize = true;
        yzPlaneRadioButton.Location = new Point(66, 3);
        yzPlaneRadioButton.Name = "yzPlaneRadioButton";
        yzPlaneRadioButton.Size = new Size(57, 27);
        yzPlaneRadioButton.TabIndex = 1;
        yzPlaneRadioButton.Text = "YZ";
        yzPlaneRadioButton.CheckedChanged += GeometryParameter_Changed;
        // 
        // xzPlaneRadioButton
        // 
        xzPlaneRadioButton.Anchor = AnchorStyles.Left;
        xzPlaneRadioButton.AutoSize = true;
        xzPlaneRadioButton.Checked = true;
        xzPlaneRadioButton.Location = new Point(129, 3);
        xzPlaneRadioButton.Name = "xzPlaneRadioButton";
        xzPlaneRadioButton.Size = new Size(57, 27);
        xzPlaneRadioButton.TabIndex = 2;
        xzPlaneRadioButton.TabStop = true;
        xzPlaneRadioButton.Text = "XZ";
        xzPlaneRadioButton.CheckedChanged += GeometryParameter_Changed;
        // 
        // modelRotationAngleLabel
        // 
        modelRotationAngleLabel.Anchor = AnchorStyles.Left;
        modelRotationAngleLabel.AutoSize = true;
        modelRotationAngleLabel.Location = new Point(5, 244);
        modelRotationAngleLabel.Margin = new Padding(5, 0, 5, 0);
        modelRotationAngleLabel.Name = "modelRotationAngleLabel";
        modelRotationAngleLabel.Size = new Size(142, 23);
        modelRotationAngleLabel.TabIndex = 12;
        modelRotationAngleLabel.Text = "模型旋轉角度 (°)";
        // 
        // modelRotationAngleNumericUpDown
        // 
        modelRotationAngleNumericUpDown.DecimalPlaces = 1;
        modelRotationAngleNumericUpDown.Dock = DockStyle.Top;
        modelRotationAngleNumericUpDown.Location = new Point(246, 241);
        modelRotationAngleNumericUpDown.Margin = new Padding(5);
        modelRotationAngleNumericUpDown.Maximum = new decimal(new int[] { 180, 0, 0, 0 });
        modelRotationAngleNumericUpDown.Minimum = new decimal(new int[] { 180, 0, 0, int.MinValue });
        modelRotationAngleNumericUpDown.Name = "modelRotationAngleNumericUpDown";
        modelRotationAngleNumericUpDown.Size = new Size(180, 30);
        modelRotationAngleNumericUpDown.TabIndex = 13;
        modelRotationAngleNumericUpDown.ValueChanged += GeometryParameter_Changed;
        // 
        // modelAlignmentLabel
        // 
        modelAlignmentLabel.Anchor = AnchorStyles.Left;
        modelAlignmentLabel.AutoSize = true;
        modelAlignmentLabel.Location = new Point(5, 284);
        modelAlignmentLabel.Margin = new Padding(5, 0, 5, 0);
        modelAlignmentLabel.Name = "modelAlignmentLabel";
        modelAlignmentLabel.Size = new Size(82, 23);
        modelAlignmentLabel.TabIndex = 14;
        modelAlignmentLabel.Text = "模型對齊";
        // 
        // modelAlignmentFlowLayoutPanel
        // 
        modelAlignmentFlowLayoutPanel.AutoSize = true;
        modelAlignmentFlowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        modelAlignmentFlowLayoutPanel.Controls.Add(centerAlignmentRadioButton);
        modelAlignmentFlowLayoutPanel.Controls.Add(leftBottomAlignmentRadioButton);
        modelAlignmentFlowLayoutPanel.Dock = DockStyle.Fill;
        modelAlignmentFlowLayoutPanel.Location = new Point(241, 276);
        modelAlignmentFlowLayoutPanel.Margin = new Padding(0);
        modelAlignmentFlowLayoutPanel.Name = "modelAlignmentFlowLayoutPanel";
        modelAlignmentFlowLayoutPanel.Size = new Size(190, 40);
        modelAlignmentFlowLayoutPanel.TabIndex = 15;
        modelAlignmentFlowLayoutPanel.WrapContents = false;
        // 
        // centerAlignmentRadioButton
        // 
        centerAlignmentRadioButton.Anchor = AnchorStyles.Left;
        centerAlignmentRadioButton.AutoSize = true;
        centerAlignmentRadioButton.Checked = true;
        centerAlignmentRadioButton.Location = new Point(3, 3);
        centerAlignmentRadioButton.Name = "centerAlignmentRadioButton";
        centerAlignmentRadioButton.Size = new Size(71, 27);
        centerAlignmentRadioButton.TabIndex = 0;
        centerAlignmentRadioButton.TabStop = true;
        centerAlignmentRadioButton.Text = "中心";
        centerAlignmentRadioButton.CheckedChanged += GeometryParameter_Changed;
        // 
        // leftBottomAlignmentRadioButton
        // 
        leftBottomAlignmentRadioButton.Anchor = AnchorStyles.Left;
        leftBottomAlignmentRadioButton.AutoSize = true;
        leftBottomAlignmentRadioButton.Location = new Point(80, 3);
        leftBottomAlignmentRadioButton.Name = "leftBottomAlignmentRadioButton";
        leftBottomAlignmentRadioButton.Size = new Size(89, 27);
        leftBottomAlignmentRadioButton.TabIndex = 1;
        leftBottomAlignmentRadioButton.Text = "左下角";
        leftBottomAlignmentRadioButton.CheckedChanged += GeometryParameter_Changed;
        // 
        // simplifyModelCheckBox
        // 
        simplifyModelCheckBox.Anchor = AnchorStyles.Left;
        simplifyModelCheckBox.AutoSize = true;
        geometryTableLayoutPanel.SetColumnSpan(simplifyModelCheckBox, 2);
        simplifyModelCheckBox.Location = new Point(5, 322);
        simplifyModelCheckBox.Margin = new Padding(5);
        simplifyModelCheckBox.Name = "simplifyModelCheckBox";
        simplifyModelCheckBox.Size = new Size(144, 27);
        simplifyModelCheckBox.TabIndex = 11;
        simplifyModelCheckBox.Text = "簡化模型面數";
        simplifyModelCheckBox.CheckedChanged += SimplificationParameter_Changed;
        // 
        // simplificationTargetLabel
        // 
        simplificationTargetLabel.Anchor = AnchorStyles.Left;
        simplificationTargetLabel.AutoSize = true;
        simplificationTargetLabel.Location = new Point(5, 364);
        simplificationTargetLabel.Margin = new Padding(5, 0, 5, 0);
        simplificationTargetLabel.Name = "simplificationTargetLabel";
        simplificationTargetLabel.Size = new Size(151, 23);
        simplificationTargetLabel.TabIndex = 12;
        simplificationTargetLabel.Text = "保留面數目標 (%)";
        // 
        // simplificationTargetNumericUpDown
        // 
        simplificationTargetNumericUpDown.Dock = DockStyle.Top;
        simplificationTargetNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        simplificationTargetNumericUpDown.Location = new Point(246, 361);
        simplificationTargetNumericUpDown.Margin = new Padding(5);
        simplificationTargetNumericUpDown.Maximum = new decimal(new int[] { 95, 0, 0, 0 });
        simplificationTargetNumericUpDown.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
        simplificationTargetNumericUpDown.Name = "simplificationTargetNumericUpDown";
        simplificationTargetNumericUpDown.Size = new Size(180, 30);
        simplificationTargetNumericUpDown.TabIndex = 13;
        simplificationTargetNumericUpDown.Value = new decimal(new int[] { 50, 0, 0, 0 });
        simplificationTargetNumericUpDown.ValueChanged += SimplificationParameter_Changed;
        // 
        // simplificationNormalAngleLabel
        // 
        simplificationNormalAngleLabel.Anchor = AnchorStyles.Left;
        simplificationNormalAngleLabel.AutoSize = true;
        simplificationNormalAngleLabel.Location = new Point(5, 407);
        simplificationNormalAngleLabel.Margin = new Padding(5, 0, 5, 0);
        simplificationNormalAngleLabel.Name = "simplificationNormalAngleLabel";
        simplificationNormalAngleLabel.Size = new Size(160, 23);
        simplificationNormalAngleLabel.TabIndex = 14;
        simplificationNormalAngleLabel.Text = "法向量容許角度 (°)";
        // 
        // simplificationNormalAngleNumericUpDown
        // 
        simplificationNormalAngleNumericUpDown.DecimalPlaces = 1;
        simplificationNormalAngleNumericUpDown.Dock = DockStyle.Top;
        simplificationNormalAngleNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
        simplificationNormalAngleNumericUpDown.Location = new Point(246, 401);
        simplificationNormalAngleNumericUpDown.Margin = new Padding(5);
        simplificationNormalAngleNumericUpDown.Maximum = new decimal(new int[] { 45, 0, 0, 0 });
        simplificationNormalAngleNumericUpDown.Minimum = new decimal(new int[] { 5, 0, 0, 65536 });
        simplificationNormalAngleNumericUpDown.Name = "simplificationNormalAngleNumericUpDown";
        simplificationNormalAngleNumericUpDown.Size = new Size(180, 30);
        simplificationNormalAngleNumericUpDown.TabIndex = 15;
        simplificationNormalAngleNumericUpDown.Value = new decimal(new int[] { 5, 0, 0, 0 });
        simplificationNormalAngleNumericUpDown.ValueChanged += SimplificationParameter_Changed;
        // 
        // smoothingGroupBox
        // 
        smoothingGroupBox.AutoSize = true;
        smoothingGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        smoothingGroupBox.Controls.Add(smoothingTableLayoutPanel);
        smoothingGroupBox.Dock = DockStyle.Top;
        smoothingGroupBox.Enabled = false;
        smoothingGroupBox.Expanded = true;
        smoothingGroupBox.Location = new Point(5, 817);
        smoothingGroupBox.Margin = new Padding(5);
        smoothingGroupBox.Name = "smoothingGroupBox";
        smoothingGroupBox.Padding = new Padding(13, 12, 13, 12);
        smoothingGroupBox.Size = new Size(457, 195);
        smoothingGroupBox.TabIndex = 2;
        smoothingGroupBox.TabStop = false;
        smoothingGroupBox.Text = "高度場平滑";
        smoothingGroupBox.ExpandedChanged += CollapsibleGroupBox_ExpandedChanged;
        // 
        // smoothingTableLayoutPanel
        // 
        smoothingTableLayoutPanel.AutoSize = true;
        smoothingTableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        smoothingTableLayoutPanel.ColumnCount = 2;
        smoothingTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
        smoothingTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
        smoothingTableLayoutPanel.Controls.Add(smoothingCheckBox, 0, 0);
        smoothingTableLayoutPanel.Controls.Add(smoothingThresholdLabel, 0, 1);
        smoothingTableLayoutPanel.Controls.Add(smoothingThresholdEditorPanel, 1, 1);
        smoothingTableLayoutPanel.Controls.Add(smoothingStrengthLabel, 0, 2);
        smoothingTableLayoutPanel.Controls.Add(smoothingStrengthEditorPanel, 1, 2);
        smoothingTableLayoutPanel.Controls.Add(smoothingIterationsLabel, 0, 3);
        smoothingTableLayoutPanel.Controls.Add(smoothingIterationsEditorPanel, 1, 3);
        smoothingTableLayoutPanel.Dock = DockStyle.Top;
        smoothingTableLayoutPanel.Location = new Point(13, 35);
        smoothingTableLayoutPanel.Margin = new Padding(5);
        smoothingTableLayoutPanel.Name = "smoothingTableLayoutPanel";
        smoothingTableLayoutPanel.RowCount = 4;
        smoothingTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        smoothingTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        smoothingTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        smoothingTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        smoothingTableLayoutPanel.Size = new Size(431, 148);
        smoothingTableLayoutPanel.TabIndex = 0;
        // 
        // smoothingCheckBox
        // 
        smoothingCheckBox.Anchor = AnchorStyles.Left;
        smoothingCheckBox.AutoSize = true;
        smoothingTableLayoutPanel.SetColumnSpan(smoothingCheckBox, 2);
        smoothingCheckBox.Location = new Point(5, 5);
        smoothingCheckBox.Margin = new Padding(5);
        smoothingCheckBox.Name = "smoothingCheckBox";
        smoothingCheckBox.Size = new Size(144, 27);
        smoothingCheckBox.TabIndex = 0;
        smoothingCheckBox.Text = "平滑相鄰網格";
        smoothingCheckBox.CheckedChanged += SmoothingParameter_Changed;
        // 
        // smoothingThresholdLabel
        // 
        smoothingThresholdLabel.Anchor = AnchorStyles.Left;
        smoothingThresholdLabel.AutoSize = true;
        smoothingThresholdLabel.Location = new Point(5, 44);
        smoothingThresholdLabel.Margin = new Padding(5, 0, 5, 0);
        smoothingThresholdLabel.Name = "smoothingThresholdLabel";
        smoothingThresholdLabel.Size = new Size(133, 23);
        smoothingThresholdLabel.TabIndex = 1;
        smoothingThresholdLabel.Text = "落差門檻 (mm)";
        // 
        // smoothingThresholdEditorPanel
        // 
        smoothingThresholdEditorPanel.Controls.Add(smoothingThresholdNumericUpDown);
        smoothingThresholdEditorPanel.Dock = DockStyle.Fill;
        smoothingThresholdEditorPanel.Location = new Point(241, 37);
        smoothingThresholdEditorPanel.Margin = new Padding(0);
        smoothingThresholdEditorPanel.Name = "smoothingThresholdEditorPanel";
        smoothingThresholdEditorPanel.Size = new Size(190, 37);
        smoothingThresholdEditorPanel.TabIndex = 2;
        // 
        // smoothingThresholdNumericUpDown
        // 
        smoothingThresholdNumericUpDown.DecimalPlaces = 2;
        smoothingThresholdNumericUpDown.Dock = DockStyle.Top;
        smoothingThresholdNumericUpDown.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        smoothingThresholdNumericUpDown.Location = new Point(0, 0);
        smoothingThresholdNumericUpDown.Margin = new Padding(5);
        smoothingThresholdNumericUpDown.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        smoothingThresholdNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
        smoothingThresholdNumericUpDown.Name = "smoothingThresholdNumericUpDown";
        smoothingThresholdNumericUpDown.Size = new Size(190, 30);
        smoothingThresholdNumericUpDown.TabIndex = 0;
        smoothingThresholdNumericUpDown.Value = new decimal(new int[] { 5, 0, 0, 65536 });
        smoothingThresholdNumericUpDown.ValueChanged += SmoothingParameter_Changed;
        // 
        // smoothingStrengthLabel
        // 
        smoothingStrengthLabel.Anchor = AnchorStyles.Left;
        smoothingStrengthLabel.AutoSize = true;
        smoothingStrengthLabel.Location = new Point(5, 81);
        smoothingStrengthLabel.Margin = new Padding(5, 0, 5, 0);
        smoothingStrengthLabel.Name = "smoothingStrengthLabel";
        smoothingStrengthLabel.Size = new Size(115, 23);
        smoothingStrengthLabel.TabIndex = 3;
        smoothingStrengthLabel.Text = "平滑強度 (%)";
        // 
        // smoothingStrengthEditorPanel
        // 
        smoothingStrengthEditorPanel.Controls.Add(smoothingStrengthNumericUpDown);
        smoothingStrengthEditorPanel.Dock = DockStyle.Fill;
        smoothingStrengthEditorPanel.Location = new Point(241, 74);
        smoothingStrengthEditorPanel.Margin = new Padding(0);
        smoothingStrengthEditorPanel.Name = "smoothingStrengthEditorPanel";
        smoothingStrengthEditorPanel.Size = new Size(190, 37);
        smoothingStrengthEditorPanel.TabIndex = 4;
        // 
        // smoothingStrengthNumericUpDown
        // 
        smoothingStrengthNumericUpDown.Dock = DockStyle.Top;
        smoothingStrengthNumericUpDown.Location = new Point(0, 0);
        smoothingStrengthNumericUpDown.Margin = new Padding(5);
        smoothingStrengthNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        smoothingStrengthNumericUpDown.Name = "smoothingStrengthNumericUpDown";
        smoothingStrengthNumericUpDown.Size = new Size(190, 30);
        smoothingStrengthNumericUpDown.TabIndex = 0;
        smoothingStrengthNumericUpDown.Value = new decimal(new int[] { 50, 0, 0, 0 });
        smoothingStrengthNumericUpDown.ValueChanged += SmoothingParameter_Changed;
        // 
        // smoothingIterationsLabel
        // 
        smoothingIterationsLabel.Anchor = AnchorStyles.Left;
        smoothingIterationsLabel.AutoSize = true;
        smoothingIterationsLabel.Location = new Point(5, 118);
        smoothingIterationsLabel.Margin = new Padding(5, 0, 5, 0);
        smoothingIterationsLabel.Name = "smoothingIterationsLabel";
        smoothingIterationsLabel.Size = new Size(82, 23);
        smoothingIterationsLabel.TabIndex = 5;
        smoothingIterationsLabel.Text = "疊代次數";
        // 
        // smoothingIterationsEditorPanel
        // 
        smoothingIterationsEditorPanel.Controls.Add(smoothingIterationsNumericUpDown);
        smoothingIterationsEditorPanel.Dock = DockStyle.Fill;
        smoothingIterationsEditorPanel.Location = new Point(241, 111);
        smoothingIterationsEditorPanel.Margin = new Padding(0);
        smoothingIterationsEditorPanel.Name = "smoothingIterationsEditorPanel";
        smoothingIterationsEditorPanel.Size = new Size(190, 37);
        smoothingIterationsEditorPanel.TabIndex = 6;
        // 
        // smoothingIterationsNumericUpDown
        // 
        smoothingIterationsNumericUpDown.Dock = DockStyle.Top;
        smoothingIterationsNumericUpDown.Location = new Point(0, 0);
        smoothingIterationsNumericUpDown.Margin = new Padding(5);
        smoothingIterationsNumericUpDown.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        smoothingIterationsNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        smoothingIterationsNumericUpDown.Name = "smoothingIterationsNumericUpDown";
        smoothingIterationsNumericUpDown.Size = new Size(190, 30);
        smoothingIterationsNumericUpDown.TabIndex = 0;
        smoothingIterationsNumericUpDown.Value = new decimal(new int[] { 2, 0, 0, 0 });
        smoothingIterationsNumericUpDown.ValueChanged += SmoothingParameter_Changed;
        // 
        // processingGroupBox
        // 
        processingGroupBox.AutoSize = true;
        processingGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        processingGroupBox.Controls.Add(processingTableLayoutPanel);
        processingGroupBox.Dock = DockStyle.Top;
        processingGroupBox.Expanded = true;
        processingGroupBox.Location = new Point(5, 1022);
        processingGroupBox.Margin = new Padding(5);
        processingGroupBox.Name = "processingGroupBox";
        processingGroupBox.Padding = new Padding(13, 12, 13, 12);
        processingGroupBox.Size = new Size(457, 689);
        processingGroupBox.TabIndex = 2;
        processingGroupBox.TabStop = false;
        processingGroupBox.Text = "影像處理";
        processingGroupBox.ExpandedChanged += CollapsibleGroupBox_ExpandedChanged;
        // 
        // processingTableLayoutPanel
        // 
        processingTableLayoutPanel.AutoSize = true;
        processingTableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        processingTableLayoutPanel.ColumnCount = 2;
        processingTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
        processingTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
        processingTableLayoutPanel.Controls.Add(imageProcessingCheckedListBox, 0, 0);
        processingTableLayoutPanel.Controls.Add(imageProcessingOrderButtonPanel, 0, 1);
        processingTableLayoutPanel.Controls.Add(grayscaleCheckBox, 0, 2);
        processingTableLayoutPanel.Controls.Add(hueLabel, 0, 3);
        processingTableLayoutPanel.Controls.Add(hueEditorPanel, 1, 3);
        processingTableLayoutPanel.Controls.Add(saturationLabel, 0, 4);
        processingTableLayoutPanel.Controls.Add(saturationEditorPanel, 1, 4);
        processingTableLayoutPanel.Controls.Add(valueLabel, 0, 5);
        processingTableLayoutPanel.Controls.Add(valueEditorPanel, 1, 5);
        processingTableLayoutPanel.Controls.Add(invertCheckBox, 0, 6);
        processingTableLayoutPanel.Controls.Add(reduceColorsCheckBox, 0, 7);
        processingTableLayoutPanel.Controls.Add(colorLevelsLabel, 0, 8);
        processingTableLayoutPanel.Controls.Add(colorLevelsEditorPanel, 1, 8);
        processingTableLayoutPanel.Controls.Add(edgeThresholdLabel, 0, 9);
        processingTableLayoutPanel.Controls.Add(edgeThresholdNumericUpDown, 1, 9);
        processingTableLayoutPanel.Controls.Add(edgeStrengthLabel, 0, 10);
        processingTableLayoutPanel.Controls.Add(edgeStrengthNumericUpDown, 1, 10);
        processingTableLayoutPanel.Controls.Add(edgeSmoothingLabel, 0, 11);
        processingTableLayoutPanel.Controls.Add(edgeSmoothingNumericUpDown, 1, 11);
        processingTableLayoutPanel.Controls.Add(binarizationThresholdLabel, 0, 12);
        processingTableLayoutPanel.Controls.Add(binarizationThresholdNumericUpDown, 1, 12);
        processingTableLayoutPanel.Controls.Add(binarizationInvertCheckBox, 0, 13);
        processingTableLayoutPanel.Controls.Add(gaussianBlurRadiusLabel, 0, 14);
        processingTableLayoutPanel.Controls.Add(gaussianBlurRadiusNumericUpDown, 1, 14);
        processingTableLayoutPanel.Dock = DockStyle.Top;
        processingTableLayoutPanel.Enabled = false;
        processingTableLayoutPanel.Location = new Point(13, 35);
        processingTableLayoutPanel.Margin = new Padding(5);
        processingTableLayoutPanel.Name = "processingTableLayoutPanel";
        processingTableLayoutPanel.RowCount = 15;
        processingTableLayoutPanel.RowStyles.Add(new RowStyle());
        processingTableLayoutPanel.RowStyles.Add(new RowStyle());
        processingTableLayoutPanel.RowStyles.Add(new RowStyle());
        processingTableLayoutPanel.RowStyles.Add(new RowStyle());
        processingTableLayoutPanel.RowStyles.Add(new RowStyle());
        processingTableLayoutPanel.RowStyles.Add(new RowStyle());
        processingTableLayoutPanel.RowStyles.Add(new RowStyle());
        processingTableLayoutPanel.RowStyles.Add(new RowStyle());
        processingTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        processingTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        processingTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        processingTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        processingTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        processingTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        processingTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28571F));
        processingTableLayoutPanel.Size = new Size(431, 642);
        processingTableLayoutPanel.TabIndex = 0;
        // 
        // imageProcessingCheckedListBox
        // 
        imageProcessingCheckedListBox.CheckOnClick = true;
        processingTableLayoutPanel.SetColumnSpan(imageProcessingCheckedListBox, 2);
        imageProcessingCheckedListBox.Dock = DockStyle.Top;
        imageProcessingCheckedListBox.FormattingEnabled = true;
        imageProcessingCheckedListBox.IntegralHeight = false;
        imageProcessingCheckedListBox.Items.AddRange(new object[] { "灰階", "邊緣偵測", "二值化", "高斯模糊" });
        imageProcessingCheckedListBox.Location = new Point(3, 3);
        imageProcessingCheckedListBox.MinimumSize = new Size(0, 110);
        imageProcessingCheckedListBox.Name = "imageProcessingCheckedListBox";
        imageProcessingCheckedListBox.Size = new Size(425, 110);
        imageProcessingCheckedListBox.TabIndex = 0;
        imageProcessingCheckedListBox.ItemCheck += ImageProcessingCheckedListBox_ItemCheck;
        imageProcessingCheckedListBox.SelectedIndexChanged += ImageProcessingCheckedListBox_SelectedIndexChanged;
        // 
        // imageProcessingOrderButtonPanel
        // 
        imageProcessingOrderButtonPanel.AutoSize = true;
        processingTableLayoutPanel.SetColumnSpan(imageProcessingOrderButtonPanel, 2);
        imageProcessingOrderButtonPanel.Controls.Add(moveImageProcessingUpButton);
        imageProcessingOrderButtonPanel.Controls.Add(moveImageProcessingDownButton);
        imageProcessingOrderButtonPanel.Dock = DockStyle.Fill;
        imageProcessingOrderButtonPanel.Location = new Point(3, 119);
        imageProcessingOrderButtonPanel.Name = "imageProcessingOrderButtonPanel";
        imageProcessingOrderButtonPanel.Size = new Size(425, 39);
        imageProcessingOrderButtonPanel.TabIndex = 1;
        imageProcessingOrderButtonPanel.WrapContents = false;
        // 
        // moveImageProcessingUpButton
        // 
        moveImageProcessingUpButton.AutoSize = true;
        moveImageProcessingUpButton.Location = new Point(3, 3);
        moveImageProcessingUpButton.Name = "moveImageProcessingUpButton";
        moveImageProcessingUpButton.Size = new Size(75, 33);
        moveImageProcessingUpButton.TabIndex = 0;
        moveImageProcessingUpButton.Text = "上移";
        moveImageProcessingUpButton.Click += MoveImageProcessingUpButton_Click;
        // 
        // moveImageProcessingDownButton
        // 
        moveImageProcessingDownButton.AutoSize = true;
        moveImageProcessingDownButton.Location = new Point(84, 3);
        moveImageProcessingDownButton.Name = "moveImageProcessingDownButton";
        moveImageProcessingDownButton.Size = new Size(75, 33);
        moveImageProcessingDownButton.TabIndex = 1;
        moveImageProcessingDownButton.Text = "下移";
        moveImageProcessingDownButton.Click += MoveImageProcessingDownButton_Click;
        // 
        // grayscaleCheckBox
        // 
        grayscaleCheckBox.AutoSize = true;
        grayscaleCheckBox.Checked = true;
        grayscaleCheckBox.CheckState = CheckState.Checked;
        processingTableLayoutPanel.SetColumnSpan(grayscaleCheckBox, 2);
        grayscaleCheckBox.Location = new Point(5, 166);
        grayscaleCheckBox.Margin = new Padding(5);
        grayscaleCheckBox.Name = "grayscaleCheckBox";
        grayscaleCheckBox.Size = new Size(108, 27);
        grayscaleCheckBox.TabIndex = 0;
        grayscaleCheckBox.Text = "轉為灰階";
        grayscaleCheckBox.CheckedChanged += ImageProcessingParameter_Changed;
        // 
        // hueLabel
        // 
        hueLabel.Anchor = AnchorStyles.Left;
        hueLabel.AutoSize = true;
        hueLabel.Location = new Point(5, 205);
        hueLabel.Margin = new Padding(5, 0, 5, 0);
        hueLabel.Name = "hueLabel";
        hueLabel.Size = new Size(144, 23);
        hueLabel.TabIndex = 1;
        hueLabel.Text = "色相 (-180~180)";
        // 
        // hueEditorPanel
        // 
        hueEditorPanel.Controls.Add(hueNumericUpDown);
        hueEditorPanel.Dock = DockStyle.Fill;
        hueEditorPanel.Location = new Point(241, 198);
        hueEditorPanel.Margin = new Padding(0);
        hueEditorPanel.Name = "hueEditorPanel";
        hueEditorPanel.Size = new Size(190, 37);
        hueEditorPanel.TabIndex = 2;
        // 
        // hueNumericUpDown
        // 
        hueNumericUpDown.Dock = DockStyle.Top;
        hueNumericUpDown.Location = new Point(0, 0);
        hueNumericUpDown.Margin = new Padding(5);
        hueNumericUpDown.Maximum = new decimal(new int[] { 180, 0, 0, 0 });
        hueNumericUpDown.Minimum = new decimal(new int[] { 180, 0, 0, int.MinValue });
        hueNumericUpDown.Name = "hueNumericUpDown";
        hueNumericUpDown.Size = new Size(190, 30);
        hueNumericUpDown.TabIndex = 0;
        hueNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // saturationLabel
        // 
        saturationLabel.Anchor = AnchorStyles.Left;
        saturationLabel.AutoSize = true;
        saturationLabel.Location = new Point(5, 242);
        saturationLabel.Margin = new Padding(5, 0, 5, 0);
        saturationLabel.Name = "saturationLabel";
        saturationLabel.Size = new Size(97, 23);
        saturationLabel.TabIndex = 3;
        saturationLabel.Text = "飽和度 (%)";
        // 
        // saturationEditorPanel
        // 
        saturationEditorPanel.Controls.Add(saturationNumericUpDown);
        saturationEditorPanel.Dock = DockStyle.Fill;
        saturationEditorPanel.Location = new Point(241, 235);
        saturationEditorPanel.Margin = new Padding(0);
        saturationEditorPanel.Name = "saturationEditorPanel";
        saturationEditorPanel.Size = new Size(190, 37);
        saturationEditorPanel.TabIndex = 4;
        // 
        // saturationNumericUpDown
        // 
        saturationNumericUpDown.Dock = DockStyle.Top;
        saturationNumericUpDown.Location = new Point(0, 0);
        saturationNumericUpDown.Margin = new Padding(5);
        saturationNumericUpDown.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
        saturationNumericUpDown.Name = "saturationNumericUpDown";
        saturationNumericUpDown.Size = new Size(190, 30);
        saturationNumericUpDown.TabIndex = 0;
        saturationNumericUpDown.Value = new decimal(new int[] { 100, 0, 0, 0 });
        saturationNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // valueLabel
        // 
        valueLabel.Anchor = AnchorStyles.Left;
        valueLabel.AutoSize = true;
        valueLabel.Location = new Point(5, 279);
        valueLabel.Margin = new Padding(5, 0, 5, 0);
        valueLabel.Name = "valueLabel";
        valueLabel.Size = new Size(79, 23);
        valueLabel.TabIndex = 5;
        valueLabel.Text = "明度 (%)";
        // 
        // valueEditorPanel
        // 
        valueEditorPanel.Controls.Add(valueNumericUpDown);
        valueEditorPanel.Dock = DockStyle.Fill;
        valueEditorPanel.Location = new Point(241, 272);
        valueEditorPanel.Margin = new Padding(0);
        valueEditorPanel.Name = "valueEditorPanel";
        valueEditorPanel.Size = new Size(190, 37);
        valueEditorPanel.TabIndex = 6;
        // 
        // valueNumericUpDown
        // 
        valueNumericUpDown.Dock = DockStyle.Top;
        valueNumericUpDown.Location = new Point(0, 0);
        valueNumericUpDown.Margin = new Padding(5);
        valueNumericUpDown.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
        valueNumericUpDown.Name = "valueNumericUpDown";
        valueNumericUpDown.Size = new Size(190, 30);
        valueNumericUpDown.TabIndex = 0;
        valueNumericUpDown.Value = new decimal(new int[] { 100, 0, 0, 0 });
        valueNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // invertCheckBox
        // 
        invertCheckBox.AutoSize = true;
        processingTableLayoutPanel.SetColumnSpan(invertCheckBox, 2);
        invertCheckBox.Location = new Point(5, 314);
        invertCheckBox.Margin = new Padding(5);
        invertCheckBox.Name = "invertCheckBox";
        invertCheckBox.Size = new Size(180, 27);
        invertCheckBox.TabIndex = 7;
        invertCheckBox.Text = "反相（黑高白低）";
        invertCheckBox.CheckedChanged += ImageProcessingParameter_Changed;
        // 
        // reduceColorsCheckBox
        // 
        reduceColorsCheckBox.AutoSize = true;
        processingTableLayoutPanel.SetColumnSpan(reduceColorsCheckBox, 2);
        reduceColorsCheckBox.Location = new Point(5, 351);
        reduceColorsCheckBox.Margin = new Padding(5);
        reduceColorsCheckBox.Name = "reduceColorsCheckBox";
        reduceColorsCheckBox.Size = new Size(108, 27);
        reduceColorsCheckBox.TabIndex = 8;
        reduceColorsCheckBox.Text = "啟用降色";
        reduceColorsCheckBox.CheckedChanged += ImageProcessingParameter_Changed;
        // 
        // colorLevelsLabel
        // 
        colorLevelsLabel.Anchor = AnchorStyles.Left;
        colorLevelsLabel.AutoSize = true;
        colorLevelsLabel.Location = new Point(5, 390);
        colorLevelsLabel.Margin = new Padding(5, 0, 5, 0);
        colorLevelsLabel.Name = "colorLevelsLabel";
        colorLevelsLabel.Size = new Size(82, 23);
        colorLevelsLabel.TabIndex = 9;
        colorLevelsLabel.Text = "灰階級數";
        // 
        // colorLevelsEditorPanel
        // 
        colorLevelsEditorPanel.Controls.Add(colorLevelsNumericUpDown);
        colorLevelsEditorPanel.Dock = DockStyle.Fill;
        colorLevelsEditorPanel.Location = new Point(241, 383);
        colorLevelsEditorPanel.Margin = new Padding(0);
        colorLevelsEditorPanel.Name = "colorLevelsEditorPanel";
        colorLevelsEditorPanel.Size = new Size(190, 37);
        colorLevelsEditorPanel.TabIndex = 10;
        // 
        // colorLevelsNumericUpDown
        // 
        colorLevelsNumericUpDown.Dock = DockStyle.Top;
        colorLevelsNumericUpDown.Location = new Point(0, 0);
        colorLevelsNumericUpDown.Margin = new Padding(5);
        colorLevelsNumericUpDown.Maximum = new decimal(new int[] { 256, 0, 0, 0 });
        colorLevelsNumericUpDown.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
        colorLevelsNumericUpDown.Name = "colorLevelsNumericUpDown";
        colorLevelsNumericUpDown.Size = new Size(190, 30);
        colorLevelsNumericUpDown.TabIndex = 0;
        colorLevelsNumericUpDown.Value = new decimal(new int[] { 16, 0, 0, 0 });
        colorLevelsNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // edgeThresholdLabel
        // 
        edgeThresholdLabel.AutoSize = true;
        edgeThresholdLabel.Location = new Point(3, 420);
        edgeThresholdLabel.Name = "edgeThresholdLabel";
        edgeThresholdLabel.Size = new Size(115, 23);
        edgeThresholdLabel.TabIndex = 11;
        edgeThresholdLabel.Text = "邊緣門檻 (%)";
        edgeThresholdLabel.Visible = false;
        // 
        // edgeThresholdNumericUpDown
        // 
        edgeThresholdNumericUpDown.Dock = DockStyle.Top;
        edgeThresholdNumericUpDown.Location = new Point(244, 423);
        edgeThresholdNumericUpDown.Maximum = new decimal(new int[] { 95, 0, 0, 0 });
        edgeThresholdNumericUpDown.Name = "edgeThresholdNumericUpDown";
        edgeThresholdNumericUpDown.Size = new Size(184, 30);
        edgeThresholdNumericUpDown.TabIndex = 12;
        edgeThresholdNumericUpDown.Value = new decimal(new int[] { 15, 0, 0, 0 });
        edgeThresholdNumericUpDown.Visible = false;
        edgeThresholdNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // edgeStrengthLabel
        // 
        edgeStrengthLabel.AutoSize = true;
        edgeStrengthLabel.Location = new Point(3, 457);
        edgeStrengthLabel.Name = "edgeStrengthLabel";
        edgeStrengthLabel.Size = new Size(115, 23);
        edgeStrengthLabel.TabIndex = 13;
        edgeStrengthLabel.Text = "邊緣強度 (%)";
        edgeStrengthLabel.Visible = false;
        // 
        // edgeStrengthNumericUpDown
        // 
        edgeStrengthNumericUpDown.Dock = DockStyle.Top;
        edgeStrengthNumericUpDown.Location = new Point(244, 460);
        edgeStrengthNumericUpDown.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
        edgeStrengthNumericUpDown.Name = "edgeStrengthNumericUpDown";
        edgeStrengthNumericUpDown.Size = new Size(184, 30);
        edgeStrengthNumericUpDown.TabIndex = 14;
        edgeStrengthNumericUpDown.Value = new decimal(new int[] { 100, 0, 0, 0 });
        edgeStrengthNumericUpDown.Visible = false;
        edgeStrengthNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // edgeSmoothingLabel
        // 
        edgeSmoothingLabel.AutoSize = true;
        edgeSmoothingLabel.Location = new Point(3, 494);
        edgeSmoothingLabel.Name = "edgeSmoothingLabel";
        edgeSmoothingLabel.Size = new Size(82, 23);
        edgeSmoothingLabel.TabIndex = 15;
        edgeSmoothingLabel.Text = "邊緣平滑";
        edgeSmoothingLabel.Visible = false;
        // 
        // edgeSmoothingNumericUpDown
        // 
        edgeSmoothingNumericUpDown.Dock = DockStyle.Top;
        edgeSmoothingNumericUpDown.Location = new Point(244, 497);
        edgeSmoothingNumericUpDown.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
        edgeSmoothingNumericUpDown.Name = "edgeSmoothingNumericUpDown";
        edgeSmoothingNumericUpDown.Size = new Size(184, 30);
        edgeSmoothingNumericUpDown.TabIndex = 16;
        edgeSmoothingNumericUpDown.Value = new decimal(new int[] { 1, 0, 0, 0 });
        edgeSmoothingNumericUpDown.Visible = false;
        edgeSmoothingNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // binarizationThresholdLabel
        // 
        binarizationThresholdLabel.AutoSize = true;
        binarizationThresholdLabel.Location = new Point(3, 531);
        binarizationThresholdLabel.Name = "binarizationThresholdLabel";
        binarizationThresholdLabel.Size = new Size(133, 23);
        binarizationThresholdLabel.TabIndex = 17;
        binarizationThresholdLabel.Text = "二值化門檻 (%)";
        binarizationThresholdLabel.Visible = false;
        // 
        // binarizationThresholdNumericUpDown
        // 
        binarizationThresholdNumericUpDown.Dock = DockStyle.Top;
        binarizationThresholdNumericUpDown.Location = new Point(244, 534);
        binarizationThresholdNumericUpDown.Name = "binarizationThresholdNumericUpDown";
        binarizationThresholdNumericUpDown.Size = new Size(184, 30);
        binarizationThresholdNumericUpDown.TabIndex = 18;
        binarizationThresholdNumericUpDown.Value = new decimal(new int[] { 50, 0, 0, 0 });
        binarizationThresholdNumericUpDown.Visible = false;
        binarizationThresholdNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // binarizationInvertCheckBox
        // 
        binarizationInvertCheckBox.AutoSize = true;
        processingTableLayoutPanel.SetColumnSpan(binarizationInvertCheckBox, 2);
        binarizationInvertCheckBox.Location = new Point(3, 571);
        binarizationInvertCheckBox.Name = "binarizationInvertCheckBox";
        binarizationInvertCheckBox.Size = new Size(126, 27);
        binarizationInvertCheckBox.TabIndex = 19;
        binarizationInvertCheckBox.Text = "二值化反相";
        binarizationInvertCheckBox.Visible = false;
        binarizationInvertCheckBox.CheckedChanged += ImageProcessingParameter_Changed;
        // 
        // gaussianBlurRadiusLabel
        // 
        gaussianBlurRadiusLabel.AutoSize = true;
        gaussianBlurRadiusLabel.Location = new Point(3, 605);
        gaussianBlurRadiusLabel.Name = "gaussianBlurRadiusLabel";
        gaussianBlurRadiusLabel.Size = new Size(82, 23);
        gaussianBlurRadiusLabel.TabIndex = 20;
        gaussianBlurRadiusLabel.Text = "模糊半徑";
        gaussianBlurRadiusLabel.Visible = false;
        // 
        // gaussianBlurRadiusNumericUpDown
        // 
        gaussianBlurRadiusNumericUpDown.DecimalPlaces = 1;
        gaussianBlurRadiusNumericUpDown.Dock = DockStyle.Top;
        gaussianBlurRadiusNumericUpDown.Location = new Point(244, 608);
        gaussianBlurRadiusNumericUpDown.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
        gaussianBlurRadiusNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
        gaussianBlurRadiusNumericUpDown.Name = "gaussianBlurRadiusNumericUpDown";
        gaussianBlurRadiusNumericUpDown.Size = new Size(184, 30);
        gaussianBlurRadiusNumericUpDown.TabIndex = 21;
        gaussianBlurRadiusNumericUpDown.Value = new decimal(new int[] { 2, 0, 0, 0 });
        gaussianBlurRadiusNumericUpDown.Visible = false;
        gaussianBlurRadiusNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // depthGroupBox
        // 
        depthGroupBox.AutoSize = true;
        depthGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        depthGroupBox.Controls.Add(depthTableLayoutPanel);
        depthGroupBox.Dock = DockStyle.Top;
        depthGroupBox.Expanded = true;
        depthGroupBox.Location = new Point(5, 1721);
        depthGroupBox.Margin = new Padding(5);
        depthGroupBox.Name = "depthGroupBox";
        depthGroupBox.Padding = new Padding(13, 12, 13, 12);
        depthGroupBox.Size = new Size(457, 486);
        depthGroupBox.TabIndex = 3;
        depthGroupBox.TabStop = false;
        depthGroupBox.Text = "人像深度與對稱";
        depthGroupBox.ExpandedChanged += CollapsibleGroupBox_ExpandedChanged;
        // 
        // depthTableLayoutPanel
        // 
        depthTableLayoutPanel.AutoSize = true;
        depthTableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        depthTableLayoutPanel.ColumnCount = 2;
        depthTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
        depthTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
        depthTableLayoutPanel.Controls.Add(useDepthImageFileCheckBox, 0, 0);
        depthTableLayoutPanel.Controls.Add(depthImageFileFlowLayoutPanel, 0, 1);
        depthTableLayoutPanel.Controls.Add(aiDepthCheckBox, 0, 2);
        depthTableLayoutPanel.Controls.Add(blendDepthCheckBox, 0, 3);
        depthTableLayoutPanel.Controls.Add(aiDepthWeightLabel, 0, 4);
        depthTableLayoutPanel.Controls.Add(aiDepthWeightEditorPanel, 1, 4);
        depthTableLayoutPanel.Controls.Add(depthCurveLabel, 0, 5);
        depthTableLayoutPanel.Controls.Add(depthCurveEditorPanel, 1, 5);
        depthTableLayoutPanel.Controls.Add(localDetailLabel, 0, 6);
        depthTableLayoutPanel.Controls.Add(localDetailEditorPanel, 1, 6);
        depthTableLayoutPanel.Controls.Add(portraitGeometryLabel, 0, 7);
        depthTableLayoutPanel.Controls.Add(portraitGeometryEditorPanel, 1, 7);
        depthTableLayoutPanel.Controls.Add(portraitAnalysisCheckBox, 0, 8);
        depthTableLayoutPanel.Controls.Add(symmetryCheckBox, 0, 9);
        depthTableLayoutPanel.Controls.Add(symmetryAxisLabel, 0, 10);
        depthTableLayoutPanel.Controls.Add(symmetryAxisEditorPanel, 1, 10);
        depthTableLayoutPanel.Controls.Add(depthNoticeLabel, 0, 11);
        depthTableLayoutPanel.Dock = DockStyle.Top;
        depthTableLayoutPanel.Location = new Point(13, 35);
        depthTableLayoutPanel.Margin = new Padding(5);
        depthTableLayoutPanel.Name = "depthTableLayoutPanel";
        depthTableLayoutPanel.RowCount = 12;
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.RowStyles.Add(new RowStyle());
        depthTableLayoutPanel.Size = new Size(431, 462);
        depthTableLayoutPanel.TabIndex = 0;
        // 
        // useDepthImageFileCheckBox
        // 
        useDepthImageFileCheckBox.AutoSize = true;
        depthTableLayoutPanel.SetColumnSpan(useDepthImageFileCheckBox, 2);
        useDepthImageFileCheckBox.Location = new Point(5, 5);
        useDepthImageFileCheckBox.Margin = new Padding(5);
        useDepthImageFileCheckBox.Name = "useDepthImageFileCheckBox";
        useDepthImageFileCheckBox.Size = new Size(177, 27);
        useDepthImageFileCheckBox.TabIndex = 0;
        useDepthImageFileCheckBox.Text = "使用 Depth 影像檔";
        useDepthImageFileCheckBox.CheckedChanged += UseDepthImageFileCheckBox_CheckedChanged;
        // 
        // depthImageFileFlowLayoutPanel
        // 
        depthImageFileFlowLayoutPanel.AutoSize = true;
        depthImageFileFlowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        depthTableLayoutPanel.SetColumnSpan(depthImageFileFlowLayoutPanel, 2);
        depthImageFileFlowLayoutPanel.Controls.Add(depthImageFilePathTextBox);
        depthImageFileFlowLayoutPanel.Controls.Add(browseDepthImageFileButton);
        depthImageFileFlowLayoutPanel.Dock = DockStyle.Top;
        depthImageFileFlowLayoutPanel.Location = new Point(5, 42);
        depthImageFileFlowLayoutPanel.Margin = new Padding(5, 0, 5, 5);
        depthImageFileFlowLayoutPanel.Name = "depthImageFileFlowLayoutPanel";
        depthImageFileFlowLayoutPanel.Size = new Size(421, 33);
        depthImageFileFlowLayoutPanel.TabIndex = 1;
        depthImageFileFlowLayoutPanel.WrapContents = false;
        // 
        // depthImageFilePathTextBox
        // 
        depthImageFilePathTextBox.Location = new Point(0, 0);
        depthImageFilePathTextBox.Margin = new Padding(0, 0, 5, 0);
        depthImageFilePathTextBox.Name = "depthImageFilePathTextBox";
        depthImageFilePathTextBox.ReadOnly = true;
        depthImageFilePathTextBox.Size = new Size(328, 30);
        depthImageFilePathTextBox.TabIndex = 0;
        // 
        // browseDepthImageFileButton
        // 
        browseDepthImageFileButton.AutoSize = true;
        browseDepthImageFileButton.Location = new Point(333, 0);
        browseDepthImageFileButton.Margin = new Padding(0);
        browseDepthImageFileButton.Name = "browseDepthImageFileButton";
        browseDepthImageFileButton.Size = new Size(88, 33);
        browseDepthImageFileButton.TabIndex = 1;
        browseDepthImageFileButton.Text = "瀏覽…";
        browseDepthImageFileButton.UseVisualStyleBackColor = true;
        browseDepthImageFileButton.Click += BrowseDepthImageFileButton_Click;
        // 
        // aiDepthCheckBox
        // 
        aiDepthCheckBox.AutoSize = true;
        depthTableLayoutPanel.SetColumnSpan(aiDepthCheckBox, 2);
        aiDepthCheckBox.Enabled = false;
        aiDepthCheckBox.Location = new Point(5, 5);
        aiDepthCheckBox.Margin = new Padding(5);
        aiDepthCheckBox.Name = "aiDepthCheckBox";
        aiDepthCheckBox.Size = new Size(274, 27);
        aiDepthCheckBox.TabIndex = 0;
        aiDepthCheckBox.Text = "產生真實灰階深度（離線 AI）";
        aiDepthCheckBox.CheckedChanged += ImageProcessingParameter_Changed;
        // 
        // blendDepthCheckBox
        // 
        blendDepthCheckBox.AutoSize = true;
        depthTableLayoutPanel.SetColumnSpan(blendDepthCheckBox, 2);
        blendDepthCheckBox.Enabled = false;
        blendDepthCheckBox.Location = new Point(5, 42);
        blendDepthCheckBox.Margin = new Padding(5);
        blendDepthCheckBox.Name = "blendDepthCheckBox";
        blendDepthCheckBox.Size = new Size(225, 27);
        blendDepthCheckBox.TabIndex = 1;
        blendDepthCheckBox.Text = "融合 AI 深度與影像處理";
        blendDepthCheckBox.CheckedChanged += ImageProcessingParameter_Changed;
        // 
        // aiDepthWeightLabel
        // 
        aiDepthWeightLabel.Anchor = AnchorStyles.Left;
        aiDepthWeightLabel.AutoSize = true;
        aiDepthWeightLabel.Enabled = false;
        aiDepthWeightLabel.Location = new Point(5, 77);
        aiDepthWeightLabel.Margin = new Padding(5, 0, 5, 0);
        aiDepthWeightLabel.Name = "aiDepthWeightLabel";
        aiDepthWeightLabel.Size = new Size(137, 23);
        aiDepthWeightLabel.TabIndex = 2;
        aiDepthWeightLabel.Text = "AI 深度比例 (%)";
        // 
        // aiDepthWeightEditorPanel
        // 
        aiDepthWeightEditorPanel.AutoSize = true;
        aiDepthWeightEditorPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        aiDepthWeightEditorPanel.Controls.Add(aiDepthWeightNumericUpDown);
        aiDepthWeightEditorPanel.Dock = DockStyle.Top;
        aiDepthWeightEditorPanel.Location = new Point(241, 74);
        aiDepthWeightEditorPanel.Margin = new Padding(0);
        aiDepthWeightEditorPanel.Name = "aiDepthWeightEditorPanel";
        aiDepthWeightEditorPanel.Size = new Size(190, 30);
        aiDepthWeightEditorPanel.TabIndex = 3;
        // 
        // aiDepthWeightNumericUpDown
        // 
        aiDepthWeightNumericUpDown.Dock = DockStyle.Top;
        aiDepthWeightNumericUpDown.Enabled = false;
        aiDepthWeightNumericUpDown.Location = new Point(0, 0);
        aiDepthWeightNumericUpDown.Margin = new Padding(5);
        aiDepthWeightNumericUpDown.Name = "aiDepthWeightNumericUpDown";
        aiDepthWeightNumericUpDown.Size = new Size(190, 30);
        aiDepthWeightNumericUpDown.TabIndex = 0;
        aiDepthWeightNumericUpDown.Value = new decimal(new int[] { 50, 0, 0, 0 });
        aiDepthWeightNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // depthCurveLabel
        // 
        depthCurveLabel.Anchor = AnchorStyles.Left;
        depthCurveLabel.AutoSize = true;
        depthCurveLabel.Enabled = false;
        depthCurveLabel.Location = new Point(5, 107);
        depthCurveLabel.Margin = new Padding(5, 0, 5, 0);
        depthCurveLabel.Name = "depthCurveLabel";
        depthCurveLabel.Size = new Size(115, 23);
        depthCurveLabel.TabIndex = 4;
        depthCurveLabel.Text = "深度曲線 (%)";
        // 
        // depthCurveEditorPanel
        // 
        depthCurveEditorPanel.AutoSize = true;
        depthCurveEditorPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        depthCurveEditorPanel.Controls.Add(depthCurveNumericUpDown);
        depthCurveEditorPanel.Dock = DockStyle.Top;
        depthCurveEditorPanel.Location = new Point(241, 104);
        depthCurveEditorPanel.Margin = new Padding(0);
        depthCurveEditorPanel.Name = "depthCurveEditorPanel";
        depthCurveEditorPanel.Size = new Size(190, 30);
        depthCurveEditorPanel.TabIndex = 5;
        // 
        // depthCurveNumericUpDown
        // 
        depthCurveNumericUpDown.Dock = DockStyle.Top;
        depthCurveNumericUpDown.Enabled = false;
        depthCurveNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        depthCurveNumericUpDown.Location = new Point(0, 0);
        depthCurveNumericUpDown.Margin = new Padding(5);
        depthCurveNumericUpDown.Maximum = new decimal(new int[] { 400, 0, 0, 0 });
        depthCurveNumericUpDown.Minimum = new decimal(new int[] { 25, 0, 0, 0 });
        depthCurveNumericUpDown.Name = "depthCurveNumericUpDown";
        depthCurveNumericUpDown.Size = new Size(190, 30);
        depthCurveNumericUpDown.TabIndex = 0;
        depthCurveNumericUpDown.Value = new decimal(new int[] { 100, 0, 0, 0 });
        depthCurveNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // localDetailLabel
        // 
        localDetailLabel.Anchor = AnchorStyles.Left;
        localDetailLabel.AutoSize = true;
        localDetailLabel.Enabled = false;
        localDetailLabel.Location = new Point(5, 137);
        localDetailLabel.Margin = new Padding(5, 0, 5, 0);
        localDetailLabel.Name = "localDetailLabel";
        localDetailLabel.Size = new Size(115, 23);
        localDetailLabel.TabIndex = 6;
        localDetailLabel.Text = "局部細節 (%)";
        // 
        // localDetailEditorPanel
        // 
        localDetailEditorPanel.AutoSize = true;
        localDetailEditorPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        localDetailEditorPanel.Controls.Add(localDetailNumericUpDown);
        localDetailEditorPanel.Dock = DockStyle.Top;
        localDetailEditorPanel.Location = new Point(241, 134);
        localDetailEditorPanel.Margin = new Padding(0);
        localDetailEditorPanel.Name = "localDetailEditorPanel";
        localDetailEditorPanel.Size = new Size(190, 30);
        localDetailEditorPanel.TabIndex = 7;
        // 
        // localDetailNumericUpDown
        // 
        localDetailNumericUpDown.Dock = DockStyle.Top;
        localDetailNumericUpDown.Enabled = false;
        localDetailNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        localDetailNumericUpDown.Location = new Point(0, 0);
        localDetailNumericUpDown.Margin = new Padding(5);
        localDetailNumericUpDown.Name = "localDetailNumericUpDown";
        localDetailNumericUpDown.Size = new Size(190, 30);
        localDetailNumericUpDown.TabIndex = 0;
        localDetailNumericUpDown.Value = new decimal(new int[] { 25, 0, 0, 0 });
        localDetailNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // portraitGeometryLabel
        // 
        portraitGeometryLabel.Anchor = AnchorStyles.Left;
        portraitGeometryLabel.AutoSize = true;
        portraitGeometryLabel.Enabled = false;
        portraitGeometryLabel.Location = new Point(5, 167);
        portraitGeometryLabel.Margin = new Padding(5, 0, 5, 0);
        portraitGeometryLabel.Name = "portraitGeometryLabel";
        portraitGeometryLabel.Size = new Size(144, 23);
        portraitGeometryLabel.TabIndex = 8;
        portraitGeometryLabel.Text = "3D 臉型融合 (%)";
        // 
        // portraitGeometryEditorPanel
        // 
        portraitGeometryEditorPanel.AutoSize = true;
        portraitGeometryEditorPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        portraitGeometryEditorPanel.Controls.Add(portraitGeometryNumericUpDown);
        portraitGeometryEditorPanel.Dock = DockStyle.Top;
        portraitGeometryEditorPanel.Location = new Point(241, 164);
        portraitGeometryEditorPanel.Margin = new Padding(0);
        portraitGeometryEditorPanel.Name = "portraitGeometryEditorPanel";
        portraitGeometryEditorPanel.Size = new Size(190, 30);
        portraitGeometryEditorPanel.TabIndex = 9;
        // 
        // portraitGeometryNumericUpDown
        // 
        portraitGeometryNumericUpDown.Dock = DockStyle.Top;
        portraitGeometryNumericUpDown.Enabled = false;
        portraitGeometryNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        portraitGeometryNumericUpDown.Location = new Point(0, 0);
        portraitGeometryNumericUpDown.Margin = new Padding(5);
        portraitGeometryNumericUpDown.Name = "portraitGeometryNumericUpDown";
        portraitGeometryNumericUpDown.Size = new Size(190, 30);
        portraitGeometryNumericUpDown.TabIndex = 0;
        portraitGeometryNumericUpDown.Value = new decimal(new int[] { 60, 0, 0, 0 });
        portraitGeometryNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // portraitAnalysisCheckBox
        // 
        portraitAnalysisCheckBox.AutoSize = true;
        depthTableLayoutPanel.SetColumnSpan(portraitAnalysisCheckBox, 2);
        portraitAnalysisCheckBox.Enabled = false;
        portraitAnalysisCheckBox.Location = new Point(5, 199);
        portraitAnalysisCheckBox.Margin = new Padding(5);
        portraitAnalysisCheckBox.Name = "portraitAnalysisCheckBox";
        portraitAnalysisCheckBox.Size = new Size(234, 27);
        portraitAnalysisCheckBox.TabIndex = 10;
        portraitAnalysisCheckBox.Text = "顯示人像分析遮罩與特徵";
        portraitAnalysisCheckBox.CheckedChanged += PortraitAnalysisCheckBox_CheckedChanged;
        // 
        // symmetryCheckBox
        // 
        symmetryCheckBox.AutoSize = true;
        depthTableLayoutPanel.SetColumnSpan(symmetryCheckBox, 2);
        symmetryCheckBox.Enabled = false;
        symmetryCheckBox.Location = new Point(5, 236);
        symmetryCheckBox.Margin = new Padding(5);
        symmetryCheckBox.Name = "symmetryCheckBox";
        symmetryCheckBox.Size = new Size(144, 27);
        symmetryCheckBox.TabIndex = 11;
        symmetryCheckBox.Text = "左右平均對稱";
        symmetryCheckBox.CheckedChanged += ImageProcessingParameter_Changed;
        // 
        // symmetryAxisLabel
        // 
        symmetryAxisLabel.Anchor = AnchorStyles.Left;
        symmetryAxisLabel.AutoSize = true;
        symmetryAxisLabel.Location = new Point(5, 293);
        symmetryAxisLabel.Margin = new Padding(5, 0, 5, 0);
        symmetryAxisLabel.Name = "symmetryAxisLabel";
        symmetryAxisLabel.Size = new Size(115, 23);
        symmetryAxisLabel.TabIndex = 12;
        symmetryAxisLabel.Text = "對稱中心 (%)";
        // 
        // symmetryAxisEditorPanel
        // 
        symmetryAxisEditorPanel.Controls.Add(symmetryAxisNumericUpDown);
        symmetryAxisEditorPanel.Dock = DockStyle.Fill;
        symmetryAxisEditorPanel.Location = new Point(241, 268);
        symmetryAxisEditorPanel.Margin = new Padding(0);
        symmetryAxisEditorPanel.Name = "symmetryAxisEditorPanel";
        symmetryAxisEditorPanel.Size = new Size(190, 73);
        symmetryAxisEditorPanel.TabIndex = 13;
        // 
        // symmetryAxisNumericUpDown
        // 
        symmetryAxisNumericUpDown.Dock = DockStyle.Top;
        symmetryAxisNumericUpDown.Location = new Point(0, 0);
        symmetryAxisNumericUpDown.Margin = new Padding(5);
        symmetryAxisNumericUpDown.Maximum = new decimal(new int[] { 90, 0, 0, 0 });
        symmetryAxisNumericUpDown.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
        symmetryAxisNumericUpDown.Name = "symmetryAxisNumericUpDown";
        symmetryAxisNumericUpDown.Size = new Size(190, 30);
        symmetryAxisNumericUpDown.TabIndex = 0;
        symmetryAxisNumericUpDown.Value = new decimal(new int[] { 50, 0, 0, 0 });
        symmetryAxisNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // depthNoticeLabel
        // 
        depthNoticeLabel.AutoSize = true;
        depthTableLayoutPanel.SetColumnSpan(depthNoticeLabel, 2);
        depthNoticeLabel.Dock = DockStyle.Fill;
        depthNoticeLabel.ForeColor = SystemColors.GrayText;
        depthNoticeLabel.Location = new Point(5, 341);
        depthNoticeLabel.Margin = new Padding(5, 0, 5, 0);
        depthNoticeLabel.Name = "depthNoticeLabel";
        depthNoticeLabel.Size = new Size(421, 46);
        depthNoticeLabel.TabIndex = 14;
        depthNoticeLabel.Text = "AI 輸出為相對深度，不是毫米級量測；推論不上傳圖檔。";
        // 
        // portraitLayersGroupBox
        // 
        portraitLayersGroupBox.AutoSize = true;
        portraitLayersGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        portraitLayersGroupBox.Controls.Add(portraitLayersTableLayoutPanel);
        portraitLayersGroupBox.Dock = DockStyle.Top;
        portraitLayersGroupBox.Enabled = false;
        portraitLayersGroupBox.Expanded = true;
        portraitLayersGroupBox.Location = new Point(5, 2142);
        portraitLayersGroupBox.Margin = new Padding(5);
        portraitLayersGroupBox.Name = "portraitLayersGroupBox";
        portraitLayersGroupBox.Padding = new Padding(13, 12, 13, 12);
        portraitLayersGroupBox.Size = new Size(457, 661);
        portraitLayersGroupBox.TabIndex = 5;
        portraitLayersGroupBox.TabStop = false;
        portraitLayersGroupBox.Text = "人像語意分層";
        portraitLayersGroupBox.ExpandedChanged += CollapsibleGroupBox_ExpandedChanged;
        // 
        // portraitLayersTableLayoutPanel
        // 
        portraitLayersTableLayoutPanel.AutoSize = true;
        portraitLayersTableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        portraitLayersTableLayoutPanel.ColumnCount = 2;
        portraitLayersTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
        portraitLayersTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
        portraitLayersTableLayoutPanel.Controls.Add(portraitLevelLabel, 0, 0);
        portraitLayersTableLayoutPanel.Controls.Add(portraitLevelComboBox, 1, 0);
        portraitLayersTableLayoutPanel.Controls.Add(glassesReliefLabel, 0, 1);
        portraitLayersTableLayoutPanel.Controls.Add(glassesReliefEditorPanel, 1, 1);
        portraitLayersTableLayoutPanel.Controls.Add(hairDetailLabel, 0, 2);
        portraitLayersTableLayoutPanel.Controls.Add(hairDetailEditorPanel, 1, 2);
        portraitLayersTableLayoutPanel.Controls.Add(surfaceNormalDetailLabel, 0, 3);
        portraitLayersTableLayoutPanel.Controls.Add(surfaceNormalDetailEditorPanel, 1, 3);
        portraitLayersTableLayoutPanel.Controls.Add(facialFeatureContourLabel, 0, 4);
        portraitLayersTableLayoutPanel.Controls.Add(facialFeatureContourEditorPanel, 1, 4);
        portraitLayersTableLayoutPanel.Controls.Add(facialDepthContrastLabel, 0, 5);
        portraitLayersTableLayoutPanel.Controls.Add(facialDepthContrastEditorPanel, 1, 5);
        portraitLayersTableLayoutPanel.Controls.Add(facialMicroDetailLabel, 0, 6);
        portraitLayersTableLayoutPanel.Controls.Add(facialMicroDetailEditorPanel, 1, 6);
        portraitLayersTableLayoutPanel.Controls.Add(backgroundSuppressionLabel, 0, 7);
        portraitLayersTableLayoutPanel.Controls.Add(backgroundSuppressionEditorPanel, 1, 7);
        portraitLayersTableLayoutPanel.Controls.Add(autoPortraitCropCheckBox, 0, 8);
        portraitLayersTableLayoutPanel.Controls.Add(bustSilhouetteCheckBox, 0, 9);
        portraitLayersTableLayoutPanel.Controls.Add(fullBodySegmentationCheckBox, 0, 10);
        portraitLayersTableLayoutPanel.Controls.Add(bodyLevelLabel, 0, 11);
        portraitLayersTableLayoutPanel.Controls.Add(bodyLevelComboBox, 1, 11);
        portraitLayersTableLayoutPanel.Controls.Add(fullBodyDepthLabel, 0, 12);
        portraitLayersTableLayoutPanel.Controls.Add(fullBodyDepthEditorPanel, 1, 12);
        portraitLayersTableLayoutPanel.Controls.Add(bodyMaskCleanupLabel, 0, 13);
        portraitLayersTableLayoutPanel.Controls.Add(bodyMaskCleanupEditorPanel, 1, 13);
        portraitLayersTableLayoutPanel.Controls.Add(bodyAnalysisCheckBox, 0, 14);
        portraitLayersTableLayoutPanel.Dock = DockStyle.Top;
        portraitLayersTableLayoutPanel.Location = new Point(13, 35);
        portraitLayersTableLayoutPanel.Margin = new Padding(5);
        portraitLayersTableLayoutPanel.Name = "portraitLayersTableLayoutPanel";
        portraitLayersTableLayoutPanel.RowCount = 15;
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle());
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.142857F));
        portraitLayersTableLayoutPanel.Size = new Size(431, 614);
        portraitLayersTableLayoutPanel.TabIndex = 0;
        // 
        // portraitLevelLabel
        // 
        portraitLevelLabel.Anchor = AnchorStyles.Left;
        portraitLevelLabel.AutoSize = true;
        portraitLevelLabel.Location = new Point(5, 8);
        portraitLevelLabel.Margin = new Padding(5, 0, 5, 0);
        portraitLevelLabel.Name = "portraitLevelLabel";
        portraitLevelLabel.Size = new Size(130, 23);
        portraitLevelLabel.TabIndex = 0;
        portraitLevelLabel.Text = "臉部分割 Level";
        // 
        // portraitLevelComboBox
        // 
        portraitLevelComboBox.Dock = DockStyle.Fill;
        portraitLevelComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        portraitLevelComboBox.Items.AddRange(new object[] { "柔和", "標準", "清晰", "自訂" });
        portraitLevelComboBox.Location = new Point(246, 5);
        portraitLevelComboBox.Margin = new Padding(5);
        portraitLevelComboBox.Name = "portraitLevelComboBox";
        portraitLevelComboBox.Size = new Size(180, 31);
        portraitLevelComboBox.TabIndex = 1;
        portraitLevelComboBox.SelectedIndexChanged += PortraitLevelComboBox_SelectedIndexChanged;
        // 
        // glassesReliefLabel
        // 
        glassesReliefLabel.Anchor = AnchorStyles.Left;
        glassesReliefLabel.AutoSize = true;
        glassesReliefLabel.Location = new Point(5, 48);
        glassesReliefLabel.Margin = new Padding(5, 0, 5, 0);
        glassesReliefLabel.Name = "glassesReliefLabel";
        glassesReliefLabel.Size = new Size(133, 23);
        glassesReliefLabel.TabIndex = 2;
        glassesReliefLabel.Text = "眼鏡框凸起 (%)";
        // 
        // glassesReliefEditorPanel
        // 
        glassesReliefEditorPanel.Controls.Add(glassesReliefNumericUpDown);
        glassesReliefEditorPanel.Dock = DockStyle.Fill;
        glassesReliefEditorPanel.Location = new Point(241, 40);
        glassesReliefEditorPanel.Margin = new Padding(0);
        glassesReliefEditorPanel.Name = "glassesReliefEditorPanel";
        glassesReliefEditorPanel.Size = new Size(190, 40);
        glassesReliefEditorPanel.TabIndex = 3;
        // 
        // glassesReliefNumericUpDown
        // 
        glassesReliefNumericUpDown.Dock = DockStyle.Top;
        glassesReliefNumericUpDown.Location = new Point(0, 0);
        glassesReliefNumericUpDown.Margin = new Padding(5);
        glassesReliefNumericUpDown.Name = "glassesReliefNumericUpDown";
        glassesReliefNumericUpDown.Size = new Size(190, 30);
        glassesReliefNumericUpDown.TabIndex = 0;
        glassesReliefNumericUpDown.Value = new decimal(new int[] { 35, 0, 0, 0 });
        glassesReliefNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // hairDetailLabel
        // 
        hairDetailLabel.Anchor = AnchorStyles.Left;
        hairDetailLabel.AutoSize = true;
        hairDetailLabel.Location = new Point(5, 88);
        hairDetailLabel.Margin = new Padding(5, 0, 5, 0);
        hairDetailLabel.Name = "hairDetailLabel";
        hairDetailLabel.Size = new Size(115, 23);
        hairDetailLabel.TabIndex = 4;
        hairDetailLabel.Text = "頭髮細節 (%)";
        // 
        // hairDetailEditorPanel
        // 
        hairDetailEditorPanel.Controls.Add(hairDetailNumericUpDown);
        hairDetailEditorPanel.Dock = DockStyle.Fill;
        hairDetailEditorPanel.Location = new Point(241, 80);
        hairDetailEditorPanel.Margin = new Padding(0);
        hairDetailEditorPanel.Name = "hairDetailEditorPanel";
        hairDetailEditorPanel.Size = new Size(190, 40);
        hairDetailEditorPanel.TabIndex = 5;
        // 
        // hairDetailNumericUpDown
        // 
        hairDetailNumericUpDown.Dock = DockStyle.Top;
        hairDetailNumericUpDown.Location = new Point(0, 0);
        hairDetailNumericUpDown.Margin = new Padding(5);
        hairDetailNumericUpDown.Name = "hairDetailNumericUpDown";
        hairDetailNumericUpDown.Size = new Size(190, 30);
        hairDetailNumericUpDown.TabIndex = 0;
        hairDetailNumericUpDown.Value = new decimal(new int[] { 25, 0, 0, 0 });
        hairDetailNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // surfaceNormalDetailLabel
        // 
        surfaceNormalDetailLabel.Anchor = AnchorStyles.Left;
        surfaceNormalDetailLabel.AutoSize = true;
        surfaceNormalDetailLabel.Location = new Point(5, 128);
        surfaceNormalDetailLabel.Margin = new Padding(5, 0, 5, 0);
        surfaceNormalDetailLabel.Name = "surfaceNormalDetailLabel";
        surfaceNormalDetailLabel.Size = new Size(151, 23);
        surfaceNormalDetailLabel.TabIndex = 6;
        surfaceNormalDetailLabel.Text = "曲面法線細節 (%)";
        // 
        // surfaceNormalDetailEditorPanel
        // 
        surfaceNormalDetailEditorPanel.Controls.Add(surfaceNormalDetailNumericUpDown);
        surfaceNormalDetailEditorPanel.Dock = DockStyle.Fill;
        surfaceNormalDetailEditorPanel.Location = new Point(241, 120);
        surfaceNormalDetailEditorPanel.Margin = new Padding(0);
        surfaceNormalDetailEditorPanel.Name = "surfaceNormalDetailEditorPanel";
        surfaceNormalDetailEditorPanel.Size = new Size(190, 40);
        surfaceNormalDetailEditorPanel.TabIndex = 7;
        // 
        // surfaceNormalDetailNumericUpDown
        // 
        surfaceNormalDetailNumericUpDown.Dock = DockStyle.Top;
        surfaceNormalDetailNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        surfaceNormalDetailNumericUpDown.Location = new Point(0, 0);
        surfaceNormalDetailNumericUpDown.Margin = new Padding(5);
        surfaceNormalDetailNumericUpDown.Name = "surfaceNormalDetailNumericUpDown";
        surfaceNormalDetailNumericUpDown.Size = new Size(190, 30);
        surfaceNormalDetailNumericUpDown.TabIndex = 0;
        surfaceNormalDetailNumericUpDown.Value = new decimal(new int[] { 20, 0, 0, 0 });
        surfaceNormalDetailNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // facialFeatureContourLabel
        // 
        facialFeatureContourLabel.Anchor = AnchorStyles.Left;
        facialFeatureContourLabel.AutoSize = true;
        facialFeatureContourLabel.Location = new Point(5, 168);
        facialFeatureContourLabel.Margin = new Padding(5, 0, 5, 0);
        facialFeatureContourLabel.Name = "facialFeatureContourLabel";
        facialFeatureContourLabel.Size = new Size(151, 23);
        facialFeatureContourLabel.TabIndex = 8;
        facialFeatureContourLabel.Text = "五官輪廓強度 (%)";
        // 
        // facialFeatureContourEditorPanel
        // 
        facialFeatureContourEditorPanel.Controls.Add(facialFeatureContourNumericUpDown);
        facialFeatureContourEditorPanel.Dock = DockStyle.Fill;
        facialFeatureContourEditorPanel.Location = new Point(241, 160);
        facialFeatureContourEditorPanel.Margin = new Padding(0);
        facialFeatureContourEditorPanel.Name = "facialFeatureContourEditorPanel";
        facialFeatureContourEditorPanel.Size = new Size(190, 40);
        facialFeatureContourEditorPanel.TabIndex = 9;
        // 
        // facialFeatureContourNumericUpDown
        // 
        facialFeatureContourNumericUpDown.Dock = DockStyle.Top;
        facialFeatureContourNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        facialFeatureContourNumericUpDown.Location = new Point(0, 0);
        facialFeatureContourNumericUpDown.Margin = new Padding(5);
        facialFeatureContourNumericUpDown.Name = "facialFeatureContourNumericUpDown";
        facialFeatureContourNumericUpDown.Size = new Size(190, 30);
        facialFeatureContourNumericUpDown.TabIndex = 0;
        facialFeatureContourNumericUpDown.Value = new decimal(new int[] { 25, 0, 0, 0 });
        facialFeatureContourNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // facialDepthContrastLabel
        // 
        facialDepthContrastLabel.Anchor = AnchorStyles.Left;
        facialDepthContrastLabel.AutoSize = true;
        facialDepthContrastLabel.Location = new Point(5, 208);
        facialDepthContrastLabel.Margin = new Padding(5, 0, 5, 0);
        facialDepthContrastLabel.Name = "facialDepthContrastLabel";
        facialDepthContrastLabel.Size = new Size(151, 23);
        facialDepthContrastLabel.TabIndex = 10;
        facialDepthContrastLabel.Text = "五官深度對比 (%)";
        // 
        // facialDepthContrastEditorPanel
        // 
        facialDepthContrastEditorPanel.Controls.Add(facialDepthContrastNumericUpDown);
        facialDepthContrastEditorPanel.Dock = DockStyle.Fill;
        facialDepthContrastEditorPanel.Location = new Point(241, 200);
        facialDepthContrastEditorPanel.Margin = new Padding(0);
        facialDepthContrastEditorPanel.Name = "facialDepthContrastEditorPanel";
        facialDepthContrastEditorPanel.Size = new Size(190, 40);
        facialDepthContrastEditorPanel.TabIndex = 11;
        // 
        // facialDepthContrastNumericUpDown
        // 
        facialDepthContrastNumericUpDown.Dock = DockStyle.Top;
        facialDepthContrastNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        facialDepthContrastNumericUpDown.Location = new Point(0, 0);
        facialDepthContrastNumericUpDown.Margin = new Padding(5);
        facialDepthContrastNumericUpDown.Name = "facialDepthContrastNumericUpDown";
        facialDepthContrastNumericUpDown.Size = new Size(190, 30);
        facialDepthContrastNumericUpDown.TabIndex = 0;
        facialDepthContrastNumericUpDown.Value = new decimal(new int[] { 35, 0, 0, 0 });
        facialDepthContrastNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // facialMicroDetailLabel
        // 
        facialMicroDetailLabel.Anchor = AnchorStyles.Left;
        facialMicroDetailLabel.AutoSize = true;
        facialMicroDetailLabel.Location = new Point(5, 248);
        facialMicroDetailLabel.Margin = new Padding(5, 0, 5, 0);
        facialMicroDetailLabel.Name = "facialMicroDetailLabel";
        facialMicroDetailLabel.Size = new Size(133, 23);
        facialMicroDetailLabel.TabIndex = 12;
        facialMicroDetailLabel.Text = "五官微細節 (%)";
        // 
        // facialMicroDetailEditorPanel
        // 
        facialMicroDetailEditorPanel.Controls.Add(facialMicroDetailNumericUpDown);
        facialMicroDetailEditorPanel.Dock = DockStyle.Fill;
        facialMicroDetailEditorPanel.Location = new Point(241, 240);
        facialMicroDetailEditorPanel.Margin = new Padding(0);
        facialMicroDetailEditorPanel.Name = "facialMicroDetailEditorPanel";
        facialMicroDetailEditorPanel.Size = new Size(190, 40);
        facialMicroDetailEditorPanel.TabIndex = 13;
        // 
        // facialMicroDetailNumericUpDown
        // 
        facialMicroDetailNumericUpDown.Dock = DockStyle.Top;
        facialMicroDetailNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        facialMicroDetailNumericUpDown.Location = new Point(0, 0);
        facialMicroDetailNumericUpDown.Margin = new Padding(5);
        facialMicroDetailNumericUpDown.Name = "facialMicroDetailNumericUpDown";
        facialMicroDetailNumericUpDown.Size = new Size(190, 30);
        facialMicroDetailNumericUpDown.TabIndex = 0;
        facialMicroDetailNumericUpDown.Value = new decimal(new int[] { 20, 0, 0, 0 });
        facialMicroDetailNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // backgroundSuppressionLabel
        // 
        backgroundSuppressionLabel.Anchor = AnchorStyles.Left;
        backgroundSuppressionLabel.AutoSize = true;
        backgroundSuppressionLabel.Location = new Point(5, 288);
        backgroundSuppressionLabel.Margin = new Padding(5, 0, 5, 0);
        backgroundSuppressionLabel.Name = "backgroundSuppressionLabel";
        backgroundSuppressionLabel.Size = new Size(115, 23);
        backgroundSuppressionLabel.TabIndex = 14;
        backgroundSuppressionLabel.Text = "背景壓低 (%)";
        // 
        // backgroundSuppressionEditorPanel
        // 
        backgroundSuppressionEditorPanel.Controls.Add(backgroundSuppressionNumericUpDown);
        backgroundSuppressionEditorPanel.Dock = DockStyle.Fill;
        backgroundSuppressionEditorPanel.Location = new Point(241, 280);
        backgroundSuppressionEditorPanel.Margin = new Padding(0);
        backgroundSuppressionEditorPanel.Name = "backgroundSuppressionEditorPanel";
        backgroundSuppressionEditorPanel.Size = new Size(190, 40);
        backgroundSuppressionEditorPanel.TabIndex = 15;
        // 
        // backgroundSuppressionNumericUpDown
        // 
        backgroundSuppressionNumericUpDown.Dock = DockStyle.Top;
        backgroundSuppressionNumericUpDown.Location = new Point(0, 0);
        backgroundSuppressionNumericUpDown.Margin = new Padding(5);
        backgroundSuppressionNumericUpDown.Name = "backgroundSuppressionNumericUpDown";
        backgroundSuppressionNumericUpDown.Size = new Size(190, 30);
        backgroundSuppressionNumericUpDown.TabIndex = 0;
        backgroundSuppressionNumericUpDown.Value = new decimal(new int[] { 80, 0, 0, 0 });
        backgroundSuppressionNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // autoPortraitCropCheckBox
        // 
        autoPortraitCropCheckBox.Anchor = AnchorStyles.Left;
        autoPortraitCropCheckBox.AutoSize = true;
        autoPortraitCropCheckBox.Checked = true;
        autoPortraitCropCheckBox.CheckState = CheckState.Checked;
        portraitLayersTableLayoutPanel.SetColumnSpan(autoPortraitCropCheckBox, 2);
        autoPortraitCropCheckBox.Location = new Point(5, 326);
        autoPortraitCropCheckBox.Margin = new Padding(5);
        autoPortraitCropCheckBox.Name = "autoPortraitCropCheckBox";
        autoPortraitCropCheckBox.Size = new Size(234, 27);
        autoPortraitCropCheckBox.TabIndex = 16;
        autoPortraitCropCheckBox.Text = "自動裁切主要人物與貼圖";
        autoPortraitCropCheckBox.CheckedChanged += ImageProcessingParameter_Changed;
        // 
        // bustSilhouetteCheckBox
        // 
        bustSilhouetteCheckBox.Anchor = AnchorStyles.Left;
        bustSilhouetteCheckBox.AutoSize = true;
        bustSilhouetteCheckBox.Checked = true;
        bustSilhouetteCheckBox.CheckState = CheckState.Checked;
        portraitLayersTableLayoutPanel.SetColumnSpan(bustSilhouetteCheckBox, 2);
        bustSilhouetteCheckBox.Location = new Point(5, 366);
        bustSilhouetteCheckBox.Margin = new Padding(5);
        bustSilhouetteCheckBox.Name = "bustSilhouetteCheckBox";
        bustSilhouetteCheckBox.Size = new Size(198, 27);
        bustSilhouetteCheckBox.TabIndex = 17;
        bustSilhouetteCheckBox.Text = "建立胸像輪廓高度場";
        bustSilhouetteCheckBox.CheckedChanged += ImageProcessingParameter_Changed;
        // 
        // fullBodySegmentationCheckBox
        // 
        fullBodySegmentationCheckBox.Anchor = AnchorStyles.Left;
        fullBodySegmentationCheckBox.AutoSize = true;
        portraitLayersTableLayoutPanel.SetColumnSpan(fullBodySegmentationCheckBox, 2);
        fullBodySegmentationCheckBox.Location = new Point(5, 406);
        fullBodySegmentationCheckBox.Margin = new Padding(5);
        fullBodySegmentationCheckBox.Name = "fullBodySegmentationCheckBox";
        fullBodySegmentationCheckBox.Size = new Size(234, 27);
        fullBodySegmentationCheckBox.TabIndex = 18;
        fullBodySegmentationCheckBox.Text = "啟用全身人物辨識與裁切";
        fullBodySegmentationCheckBox.CheckedChanged += ImageProcessingParameter_Changed;
        // 
        // bodyLevelLabel
        // 
        bodyLevelLabel.Anchor = AnchorStyles.Left;
        bodyLevelLabel.AutoSize = true;
        bodyLevelLabel.Location = new Point(3, 448);
        bodyLevelLabel.Name = "bodyLevelLabel";
        bodyLevelLabel.Size = new Size(130, 23);
        bodyLevelLabel.TabIndex = 19;
        bodyLevelLabel.Text = "身體分割 Level";
        // 
        // bodyLevelComboBox
        // 
        bodyLevelComboBox.Dock = DockStyle.Fill;
        bodyLevelComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        bodyLevelComboBox.Items.AddRange(new object[] { "柔和", "標準", "清晰", "自訂" });
        bodyLevelComboBox.Location = new Point(244, 443);
        bodyLevelComboBox.Name = "bodyLevelComboBox";
        bodyLevelComboBox.Size = new Size(184, 31);
        bodyLevelComboBox.TabIndex = 20;
        bodyLevelComboBox.SelectedIndexChanged += BodyLevelComboBox_SelectedIndexChanged;
        // 
        // fullBodyDepthLabel
        // 
        fullBodyDepthLabel.Anchor = AnchorStyles.Left;
        fullBodyDepthLabel.AutoSize = true;
        fullBodyDepthLabel.Location = new Point(5, 488);
        fullBodyDepthLabel.Margin = new Padding(5, 0, 5, 0);
        fullBodyDepthLabel.Name = "fullBodyDepthLabel";
        fullBodyDepthLabel.Size = new Size(151, 23);
        fullBodyDepthLabel.TabIndex = 19;
        fullBodyDepthLabel.Text = "全身輪廓深度 (%)";
        // 
        // fullBodyDepthEditorPanel
        // 
        fullBodyDepthEditorPanel.Controls.Add(fullBodyDepthNumericUpDown);
        fullBodyDepthEditorPanel.Dock = DockStyle.Fill;
        fullBodyDepthEditorPanel.Location = new Point(241, 480);
        fullBodyDepthEditorPanel.Margin = new Padding(0);
        fullBodyDepthEditorPanel.Name = "fullBodyDepthEditorPanel";
        fullBodyDepthEditorPanel.Size = new Size(190, 40);
        fullBodyDepthEditorPanel.TabIndex = 20;
        // 
        // fullBodyDepthNumericUpDown
        // 
        fullBodyDepthNumericUpDown.Dock = DockStyle.Top;
        fullBodyDepthNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        fullBodyDepthNumericUpDown.Location = new Point(0, 0);
        fullBodyDepthNumericUpDown.Margin = new Padding(5);
        fullBodyDepthNumericUpDown.Name = "fullBodyDepthNumericUpDown";
        fullBodyDepthNumericUpDown.Size = new Size(190, 30);
        fullBodyDepthNumericUpDown.TabIndex = 0;
        fullBodyDepthNumericUpDown.Value = new decimal(new int[] { 45, 0, 0, 0 });
        fullBodyDepthNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // bodyMaskCleanupLabel
        // 
        bodyMaskCleanupLabel.Anchor = AnchorStyles.Left;
        bodyMaskCleanupLabel.AutoSize = true;
        bodyMaskCleanupLabel.Location = new Point(5, 528);
        bodyMaskCleanupLabel.Margin = new Padding(5, 0, 5, 0);
        bodyMaskCleanupLabel.Name = "bodyMaskCleanupLabel";
        bodyMaskCleanupLabel.Size = new Size(187, 23);
        bodyMaskCleanupLabel.TabIndex = 21;
        bodyMaskCleanupLabel.Text = "遮罩／附屬物清理 (%)";
        // 
        // bodyMaskCleanupEditorPanel
        // 
        bodyMaskCleanupEditorPanel.Controls.Add(bodyMaskCleanupNumericUpDown);
        bodyMaskCleanupEditorPanel.Dock = DockStyle.Fill;
        bodyMaskCleanupEditorPanel.Location = new Point(241, 520);
        bodyMaskCleanupEditorPanel.Margin = new Padding(0);
        bodyMaskCleanupEditorPanel.Name = "bodyMaskCleanupEditorPanel";
        bodyMaskCleanupEditorPanel.Size = new Size(190, 40);
        bodyMaskCleanupEditorPanel.TabIndex = 22;
        // 
        // bodyMaskCleanupNumericUpDown
        // 
        bodyMaskCleanupNumericUpDown.Dock = DockStyle.Top;
        bodyMaskCleanupNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        bodyMaskCleanupNumericUpDown.Location = new Point(0, 0);
        bodyMaskCleanupNumericUpDown.Margin = new Padding(5);
        bodyMaskCleanupNumericUpDown.Name = "bodyMaskCleanupNumericUpDown";
        bodyMaskCleanupNumericUpDown.Size = new Size(190, 30);
        bodyMaskCleanupNumericUpDown.TabIndex = 0;
        bodyMaskCleanupNumericUpDown.Value = new decimal(new int[] { 35, 0, 0, 0 });
        bodyMaskCleanupNumericUpDown.ValueChanged += ImageProcessingParameter_Changed;
        // 
        // bodyAnalysisCheckBox
        // 
        bodyAnalysisCheckBox.Anchor = AnchorStyles.Left;
        bodyAnalysisCheckBox.AutoSize = true;
        portraitLayersTableLayoutPanel.SetColumnSpan(bodyAnalysisCheckBox, 2);
        bodyAnalysisCheckBox.Enabled = false;
        bodyAnalysisCheckBox.Location = new Point(5, 573);
        bodyAnalysisCheckBox.Margin = new Padding(5);
        bodyAnalysisCheckBox.Name = "bodyAnalysisCheckBox";
        bodyAnalysisCheckBox.Size = new Size(180, 27);
        bodyAnalysisCheckBox.TabIndex = 23;
        bodyAnalysisCheckBox.Text = "顯示全身人物分析";
        bodyAnalysisCheckBox.CheckedChanged += BodyAnalysisCheckBox_CheckedChanged;
        // 
        // previewTabControl
        // 
        previewTabControl.Controls.Add(originalImageTabPage);
        previewTabControl.Controls.Add(processedImageTabPage);
        previewTabControl.Controls.Add(modelPreviewTabPage);
        previewTabControl.Dock = DockStyle.Fill;
        previewTabControl.Location = new Point(0, 0);
        previewTabControl.Margin = new Padding(5);
        previewTabControl.Name = "previewTabControl";
        previewTabControl.SelectedIndex = 2;
        previewTabControl.Size = new Size(1326, 1022);
        previewTabControl.TabIndex = 0;
        // 
        // originalImageTabPage
        // 
        originalImageTabPage.Controls.Add(originalImagePanel);
        originalImageTabPage.Location = new Point(4, 32);
        originalImageTabPage.Margin = new Padding(5);
        originalImageTabPage.Name = "originalImageTabPage";
        originalImageTabPage.Padding = new Padding(5);
        originalImageTabPage.Size = new Size(1318, 986);
        originalImageTabPage.TabIndex = 0;
        originalImageTabPage.Text = "原始影像";
        originalImageTabPage.UseVisualStyleBackColor = true;
        // 
        // originalImagePanel
        // 
        originalImagePanel.BackColor = Color.FromArgb(32, 35, 40);
        originalImagePanel.Controls.Add(originalImagePlaceholderLabel);
        originalImagePanel.Controls.Add(originalImagePictureBox);
        originalImagePanel.Dock = DockStyle.Fill;
        originalImagePanel.Location = new Point(5, 5);
        originalImagePanel.Margin = new Padding(5);
        originalImagePanel.Name = "originalImagePanel";
        originalImagePanel.Size = new Size(1308, 976);
        originalImagePanel.TabIndex = 0;
        // 
        // originalImagePlaceholderLabel
        // 
        originalImagePlaceholderLabel.BackColor = Color.Transparent;
        originalImagePlaceholderLabel.Dock = DockStyle.Fill;
        originalImagePlaceholderLabel.ForeColor = Color.Gainsboro;
        originalImagePlaceholderLabel.Location = new Point(0, 0);
        originalImagePlaceholderLabel.Margin = new Padding(5, 0, 5, 0);
        originalImagePlaceholderLabel.Name = "originalImagePlaceholderLabel";
        originalImagePlaceholderLabel.Size = new Size(1308, 976);
        originalImagePlaceholderLabel.TabIndex = 0;
        originalImagePlaceholderLabel.Text = "尚未載入原始影像\r\n圖檔讀取功能將於第三階段啟用";
        originalImagePlaceholderLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // originalImagePictureBox
        // 
        originalImagePictureBox.Dock = DockStyle.Fill;
        originalImagePictureBox.Location = new Point(0, 0);
        originalImagePictureBox.Margin = new Padding(5);
        originalImagePictureBox.Name = "originalImagePictureBox";
        originalImagePictureBox.Size = new Size(1308, 976);
        originalImagePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        originalImagePictureBox.TabIndex = 1;
        originalImagePictureBox.TabStop = false;
        // 
        // processedImageTabPage
        // 
        processedImageTabPage.Controls.Add(processedImageLayoutPanel);
        processedImageTabPage.Location = new Point(4, 32);
        processedImageTabPage.Margin = new Padding(5);
        processedImageTabPage.Name = "processedImageTabPage";
        processedImageTabPage.Padding = new Padding(5);
        processedImageTabPage.Size = new Size(1318, 986);
        processedImageTabPage.TabIndex = 1;
        processedImageTabPage.Text = "處理後影像";
        processedImageTabPage.UseVisualStyleBackColor = true;
        // 
        // processedImageLayoutPanel
        // 
        processedImageLayoutPanel.ColumnCount = 1;
        processedImageLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        processedImageLayoutPanel.Controls.Add(processedImageToolPanel, 0, 0);
        processedImageLayoutPanel.Controls.Add(processedImagePanel, 0, 1);
        processedImageLayoutPanel.Dock = DockStyle.Fill;
        processedImageLayoutPanel.Location = new Point(5, 5);
        processedImageLayoutPanel.Margin = new Padding(0);
        processedImageLayoutPanel.Name = "processedImageLayoutPanel";
        processedImageLayoutPanel.RowCount = 2;
        processedImageLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        processedImageLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        processedImageLayoutPanel.Size = new Size(1308, 976);
        processedImageLayoutPanel.TabIndex = 0;
        // 
        // processedImageToolPanel
        // 
        processedImageToolPanel.Controls.Add(saveDepthMapButton);
        processedImageToolPanel.Controls.Add(depthPreviewModeLabel);
        processedImageToolPanel.Controls.Add(depthPreviewModeComboBox);
        processedImageToolPanel.Dock = DockStyle.Fill;
        processedImageToolPanel.FlowDirection = FlowDirection.RightToLeft;
        processedImageToolPanel.Location = new Point(5, 5);
        processedImageToolPanel.Margin = new Padding(5);
        processedImageToolPanel.Name = "processedImageToolPanel";
        processedImageToolPanel.Padding = new Padding(8, 5, 8, 5);
        processedImageToolPanel.Size = new Size(1298, 54);
        processedImageToolPanel.TabIndex = 0;
        processedImageToolPanel.WrapContents = false;
        // 
        // saveDepthMapButton
        // 
        saveDepthMapButton.AutoSize = true;
        saveDepthMapButton.Enabled = false;
        saveDepthMapButton.Location = new Point(1107, 10);
        saveDepthMapButton.Margin = new Padding(5);
        saveDepthMapButton.Name = "saveDepthMapButton";
        saveDepthMapButton.Size = new Size(170, 43);
        saveDepthMapButton.TabIndex = 2;
        saveDepthMapButton.Text = "儲存深度圖…";
        saveDepthMapButton.UseVisualStyleBackColor = true;
        saveDepthMapButton.Click += SaveDepthMapButton_Click;
        // 
        // depthPreviewModeLabel
        // 
        depthPreviewModeLabel.Anchor = AnchorStyles.Left;
        depthPreviewModeLabel.AutoSize = true;
        depthPreviewModeLabel.Location = new Point(1053, 22);
        depthPreviewModeLabel.Margin = new Padding(12, 8, 3, 3);
        depthPreviewModeLabel.Name = "depthPreviewModeLabel";
        depthPreviewModeLabel.Size = new Size(46, 23);
        depthPreviewModeLabel.TabIndex = 3;
        depthPreviewModeLabel.Text = "預覽";
        // 
        // depthPreviewModeComboBox
        // 
        depthPreviewModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        depthPreviewModeComboBox.Items.AddRange(new object[] { "影像處理結果", "AI 原始深度", "AI 融合結果", "最終高度場" });
        depthPreviewModeComboBox.Location = new Point(863, 8);
        depthPreviewModeComboBox.Name = "depthPreviewModeComboBox";
        depthPreviewModeComboBox.Size = new Size(175, 31);
        depthPreviewModeComboBox.TabIndex = 4;
        depthPreviewModeComboBox.SelectedIndexChanged += DepthPreviewModeComboBox_SelectedIndexChanged;
        // 
        // processedImagePanel
        // 
        processedImagePanel.BackColor = Color.FromArgb(32, 35, 40);
        processedImagePanel.Controls.Add(processedImagePlaceholderLabel);
        processedImagePanel.Controls.Add(processedImagePictureBox);
        processedImagePanel.Dock = DockStyle.Fill;
        processedImagePanel.Location = new Point(5, 69);
        processedImagePanel.Margin = new Padding(5);
        processedImagePanel.Name = "processedImagePanel";
        processedImagePanel.Size = new Size(1298, 902);
        processedImagePanel.TabIndex = 1;
        // 
        // processedImagePlaceholderLabel
        // 
        processedImagePlaceholderLabel.BackColor = Color.Transparent;
        processedImagePlaceholderLabel.Dock = DockStyle.Fill;
        processedImagePlaceholderLabel.ForeColor = Color.Gainsboro;
        processedImagePlaceholderLabel.Location = new Point(0, 0);
        processedImagePlaceholderLabel.Margin = new Padding(5, 0, 5, 0);
        processedImagePlaceholderLabel.Name = "processedImagePlaceholderLabel";
        processedImagePlaceholderLabel.Size = new Size(1298, 902);
        processedImagePlaceholderLabel.TabIndex = 0;
        processedImagePlaceholderLabel.Text = "尚未產生處理後影像";
        processedImagePlaceholderLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // processedImagePictureBox
        // 
        processedImagePictureBox.Dock = DockStyle.Fill;
        processedImagePictureBox.Location = new Point(0, 0);
        processedImagePictureBox.Margin = new Padding(5);
        processedImagePictureBox.Name = "processedImagePictureBox";
        processedImagePictureBox.Size = new Size(1298, 902);
        processedImagePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        processedImagePictureBox.TabIndex = 1;
        processedImagePictureBox.TabStop = false;
        // 
        // modelPreviewTabPage
        // 
        modelPreviewTabPage.Controls.Add(modelPreviewLayoutPanel);
        modelPreviewTabPage.Location = new Point(4, 32);
        modelPreviewTabPage.Margin = new Padding(5);
        modelPreviewTabPage.Name = "modelPreviewTabPage";
        modelPreviewTabPage.Padding = new Padding(5);
        modelPreviewTabPage.Size = new Size(1318, 986);
        modelPreviewTabPage.TabIndex = 2;
        modelPreviewTabPage.Text = "2.5D 模型";
        modelPreviewTabPage.UseVisualStyleBackColor = true;
        // 
        // modelPreviewLayoutPanel
        // 
        modelPreviewLayoutPanel.ColumnCount = 1;
        modelPreviewLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        modelPreviewLayoutPanel.Controls.Add(environmentLightPanel, 0, 0);
        modelPreviewLayoutPanel.Controls.Add(previewGlControl, 0, 1);
        modelPreviewLayoutPanel.Dock = DockStyle.Fill;
        modelPreviewLayoutPanel.Location = new Point(5, 5);
        modelPreviewLayoutPanel.Margin = new Padding(5);
        modelPreviewLayoutPanel.Name = "modelPreviewLayoutPanel";
        modelPreviewLayoutPanel.RowCount = 2;
        modelPreviewLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        modelPreviewLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        modelPreviewLayoutPanel.Size = new Size(1308, 976);
        modelPreviewLayoutPanel.TabIndex = 0;
        // 
        // environmentLightPanel
        // 
        environmentLightPanel.Controls.Add(environmentLightLabel);
        environmentLightPanel.Controls.Add(environmentLightTrackBar);
        environmentLightPanel.Controls.Add(environmentLightNumericUpDown);
        environmentLightPanel.Controls.Add(wireframeCheckBox);
        environmentLightPanel.Controls.Add(textureCheckBox);
        environmentLightPanel.Dock = DockStyle.Fill;
        environmentLightPanel.Location = new Point(5, 5);
        environmentLightPanel.Margin = new Padding(5);
        environmentLightPanel.Name = "environmentLightPanel";
        environmentLightPanel.Padding = new Padding(13, 8, 13, 5);
        environmentLightPanel.Size = new Size(1298, 54);
        environmentLightPanel.TabIndex = 0;
        environmentLightPanel.WrapContents = false;
        // 
        // environmentLightLabel
        // 
        environmentLightLabel.Anchor = AnchorStyles.Left;
        environmentLightLabel.AutoSize = true;
        environmentLightLabel.Location = new Point(18, 28);
        environmentLightLabel.Margin = new Padding(5, 12, 13, 5);
        environmentLightLabel.Name = "environmentLightLabel";
        environmentLightLabel.Size = new Size(100, 23);
        environmentLightLabel.TabIndex = 0;
        environmentLightLabel.Text = "環境光強度";
        // 
        // environmentLightTrackBar
        // 
        environmentLightTrackBar.AutoSize = false;
        environmentLightTrackBar.LargeChange = 10;
        environmentLightTrackBar.Location = new Point(136, 13);
        environmentLightTrackBar.Margin = new Padding(5);
        environmentLightTrackBar.Maximum = 100;
        environmentLightTrackBar.Name = "environmentLightTrackBar";
        environmentLightTrackBar.Size = new Size(346, 46);
        environmentLightTrackBar.TabIndex = 1;
        environmentLightTrackBar.TickFrequency = 10;
        environmentLightTrackBar.Value = 40;
        environmentLightTrackBar.ValueChanged += EnvironmentLightTrackBar_ValueChanged;
        // 
        // environmentLightNumericUpDown
        // 
        environmentLightNumericUpDown.Location = new Point(492, 13);
        environmentLightNumericUpDown.Margin = new Padding(5);
        environmentLightNumericUpDown.Name = "environmentLightNumericUpDown";
        environmentLightNumericUpDown.Size = new Size(101, 30);
        environmentLightNumericUpDown.TabIndex = 2;
        environmentLightNumericUpDown.Value = new decimal(new int[] { 40, 0, 0, 0 });
        environmentLightNumericUpDown.ValueChanged += EnvironmentLightNumericUpDown_ValueChanged;
        // 
        // wireframeCheckBox
        // 
        wireframeCheckBox.Anchor = AnchorStyles.Left;
        wireframeCheckBox.AutoSize = true;
        wireframeCheckBox.Location = new Point(622, 26);
        wireframeCheckBox.Margin = new Padding(24, 12, 5, 5);
        wireframeCheckBox.Name = "wireframeCheckBox";
        wireframeCheckBox.Size = new Size(108, 27);
        wireframeCheckBox.TabIndex = 3;
        wireframeCheckBox.Text = "線框顯示";
        wireframeCheckBox.CheckedChanged += WireframeCheckBox_CheckedChanged;
        // 
        // textureCheckBox
        // 
        textureCheckBox.Anchor = AnchorStyles.Left;
        textureCheckBox.AutoSize = true;
        textureCheckBox.Checked = true;
        textureCheckBox.CheckState = CheckState.Checked;
        textureCheckBox.Enabled = false;
        textureCheckBox.Location = new Point(759, 26);
        textureCheckBox.Margin = new Padding(24, 12, 5, 5);
        textureCheckBox.Name = "textureCheckBox";
        textureCheckBox.Size = new Size(72, 27);
        textureCheckBox.TabIndex = 10;
        textureCheckBox.Text = "貼圖";
        textureCheckBox.CheckedChanged += TextureCheckBox_CheckedChanged;
        // 
        // previewGlControl
        // 
        previewGlControl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
        previewGlControl.APIVersion = new Version(3, 3, 0, 0);
        previewGlControl.BackColor = Color.Black;
        previewGlControl.Dock = DockStyle.Fill;
        previewGlControl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
        previewGlControl.IsEventDriven = true;
        previewGlControl.Location = new Point(5, 69);
        previewGlControl.Margin = new Padding(5);
        previewGlControl.Name = "previewGlControl";
        previewGlControl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
        previewGlControl.SharedContext = null;
        previewGlControl.Size = new Size(1298, 902);
        previewGlControl.TabIndex = 0;
        // 
        // commandFlowLayoutPanel
        // 
        commandFlowLayoutPanel.Controls.Add(closeButton);
        commandFlowLayoutPanel.Controls.Add(regenerateButton);
        commandFlowLayoutPanel.Controls.Add(resetButton);
        commandFlowLayoutPanel.Dock = DockStyle.Fill;
        commandFlowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
        commandFlowLayoutPanel.Location = new Point(5, 1037);
        commandFlowLayoutPanel.Margin = new Padding(5);
        commandFlowLayoutPanel.Name = "commandFlowLayoutPanel";
        commandFlowLayoutPanel.Padding = new Padding(13, 12, 13, 12);
        commandFlowLayoutPanel.Size = new Size(1851, 64);
        commandFlowLayoutPanel.TabIndex = 1;
        // 
        // closeButton
        // 
        closeButton.AutoSize = true;
        closeButton.Location = new Point(1647, 17);
        closeButton.Margin = new Padding(5);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(173, 51);
        closeButton.TabIndex = 0;
        closeButton.Text = "關閉";
        closeButton.UseVisualStyleBackColor = true;
        closeButton.Click += CloseButton_Click;
        // 
        // regenerateButton
        // 
        regenerateButton.AutoSize = true;
        regenerateButton.Enabled = false;
        regenerateButton.Location = new Point(1492, 17);
        regenerateButton.Margin = new Padding(5);
        regenerateButton.Name = "regenerateButton";
        regenerateButton.Size = new Size(145, 51);
        regenerateButton.TabIndex = 1;
        regenerateButton.Text = "重新產生";
        regenerateButton.UseVisualStyleBackColor = true;
        regenerateButton.Click += RegenerateButton_Click;
        // 
        // resetButton
        // 
        resetButton.AutoSize = true;
        resetButton.Enabled = false;
        resetButton.Location = new Point(1337, 17);
        resetButton.Margin = new Padding(5);
        resetButton.Name = "resetButton";
        resetButton.Size = new Size(145, 51);
        resetButton.TabIndex = 2;
        resetButton.Text = "重設參數";
        resetButton.UseVisualStyleBackColor = true;
        resetButton.Click += ResetButton_Click;
        // 
        // statusStrip
        // 
        statusStrip.ImageScalingSize = new Size(24, 24);
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
        statusStrip.Location = new Point(0, 1106);
        statusStrip.Name = "statusStrip";
        statusStrip.Padding = new Padding(2, 0, 22, 0);
        statusStrip.Size = new Size(1861, 30);
        statusStrip.SizingGrip = false;
        statusStrip.TabIndex = 2;
        // 
        // statusLabel
        // 
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(441, 23);
        statusLabel.Text = "請讀取圖檔；支援 JPG、PNG、BMP、TIFF、WebP。";
        // 
        // imageOpenFileDialog
        // 
        imageOpenFileDialog.Filter = "支援的圖檔|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.webp|JPEG|*.jpg;*.jpeg|PNG|*.png|BMP|*.bmp|TIFF|*.tif;*.tiff|WebP|*.webp|所有檔案|*.*";
        imageOpenFileDialog.Title = "選擇浮雕來源圖檔";
        // 
        // depthImageOpenFileDialog
        // 
        depthImageOpenFileDialog.Filter = "支援的 Depth 圖檔|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.webp|JPEG|*.jpg;*.jpeg|PNG|*.png|BMP|*.bmp|TIFF|*.tif;*.tiff|WebP|*.webp|所有檔案|*.*";
        depthImageOpenFileDialog.Title = "選擇 Depth 影像檔";
        // 
        // depthMapSaveFileDialog
        // 
        depthMapSaveFileDialog.DefaultExt = "png";
        depthMapSaveFileDialog.Filter = "PNG 深度圖|*.png";
        depthMapSaveFileDialog.Title = "儲存浮雕深度圖";
        // 
        // parameterProfileOpenFileDialog
        // 
        parameterProfileOpenFileDialog.DefaultExt = "rlfPar";
        parameterProfileOpenFileDialog.Filter = "Relief 參數設定 (*.rlfPar)|*.rlfPar";
        parameterProfileOpenFileDialog.Title = "讀取 Relief 參數設定";
        // 
        // parameterProfileSaveFileDialog
        // 
        parameterProfileSaveFileDialog.DefaultExt = "rlfPar";
        parameterProfileSaveFileDialog.Filter = "Relief 參數設定 (*.rlfPar)|*.rlfPar";
        parameterProfileSaveFileDialog.Title = "儲存 Relief 參數設定";
        // 
        // processingDebounceTimer
        // 
        processingDebounceTimer.Interval = 500;
        processingDebounceTimer.Tick += ProcessingDebounceTimer_Tick;
        // 
        // modelGenerationDebounceTimer
        // 
        modelGenerationDebounceTimer.Interval = 600;
        modelGenerationDebounceTimer.Tick += ModelGenerationDebounceTimer_Tick;
        // 
        // ReliefBuilderForm
        // 
        AutoScaleDimensions = new SizeF(11F, 23F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = closeButton;
        ClientSize = new Size(1861, 1167);
        Controls.Add(rootLayoutPanel);
        Controls.Add(unifiedMenuStrip);
        MainMenuStrip = unifiedMenuStrip;
        Margin = new Padding(5);
        MinimumSize = new Size(1496, 951);
        Name = "ReliefBuilderForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "建立 2.5D 浮雕模型";
        WindowState = FormWindowState.Maximized;
        FormClosing += ReliefBuilderForm_FormClosing;
        FormClosed += ReliefBuilderForm_FormClosed;
        Load += ReliefBuilderForm_Load;
        Shown += ReliefBuilderForm_Shown;
        rootLayoutPanel.ResumeLayout(false);
        rootLayoutPanel.PerformLayout();
        workspaceSplitContainer.Panel1.ResumeLayout(false);
        workspaceSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)workspaceSplitContainer).EndInit();
        workspaceSplitContainer.ResumeLayout(false);
        parameterScrollPanel.ResumeLayout(false);
        parameterScrollPanel.PerformLayout();
        parameterTableLayoutPanel.ResumeLayout(false);
        parameterTableLayoutPanel.PerformLayout();
        sourceGroupBox.ResumeLayout(false);
        sourceGroupBox.PerformLayout();
        sourceTableLayoutPanel.ResumeLayout(false);
        sourceTableLayoutPanel.PerformLayout();
        parameterProfileButtonFlowLayoutPanel.ResumeLayout(false);
        parameterProfileButtonFlowLayoutPanel.PerformLayout();
        geometryGroupBox.ResumeLayout(false);
        geometryGroupBox.PerformLayout();
        geometryTableLayoutPanel.ResumeLayout(false);
        geometryTableLayoutPanel.PerformLayout();
        widthEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)widthNumericUpDown).EndInit();
        thicknessEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)thicknessNumericUpDown).EndInit();
        borderWidthEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)borderWidthNumericUpDown).EndInit();
        reliefHeightEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)reliefHeightNumericUpDown).EndInit();
        modelPlaneFlowLayoutPanel.ResumeLayout(false);
        modelPlaneFlowLayoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)modelRotationAngleNumericUpDown).EndInit();
        modelAlignmentFlowLayoutPanel.ResumeLayout(false);
        modelAlignmentFlowLayoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)simplificationTargetNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)simplificationNormalAngleNumericUpDown).EndInit();
        smoothingGroupBox.ResumeLayout(false);
        smoothingGroupBox.PerformLayout();
        smoothingTableLayoutPanel.ResumeLayout(false);
        smoothingTableLayoutPanel.PerformLayout();
        smoothingThresholdEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)smoothingThresholdNumericUpDown).EndInit();
        smoothingStrengthEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)smoothingStrengthNumericUpDown).EndInit();
        smoothingIterationsEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)smoothingIterationsNumericUpDown).EndInit();
        processingGroupBox.ResumeLayout(false);
        processingGroupBox.PerformLayout();
        processingTableLayoutPanel.ResumeLayout(false);
        processingTableLayoutPanel.PerformLayout();
        imageProcessingOrderButtonPanel.ResumeLayout(false);
        imageProcessingOrderButtonPanel.PerformLayout();
        hueEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)hueNumericUpDown).EndInit();
        saturationEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)saturationNumericUpDown).EndInit();
        valueEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)valueNumericUpDown).EndInit();
        colorLevelsEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)colorLevelsNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)edgeThresholdNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)edgeStrengthNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)edgeSmoothingNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)binarizationThresholdNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)gaussianBlurRadiusNumericUpDown).EndInit();
        depthGroupBox.ResumeLayout(false);
        depthGroupBox.PerformLayout();
        depthTableLayoutPanel.ResumeLayout(false);
        depthTableLayoutPanel.PerformLayout();
        depthImageFileFlowLayoutPanel.ResumeLayout(false);
        depthImageFileFlowLayoutPanel.PerformLayout();
        aiDepthWeightEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)aiDepthWeightNumericUpDown).EndInit();
        depthCurveEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)depthCurveNumericUpDown).EndInit();
        localDetailEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)localDetailNumericUpDown).EndInit();
        portraitGeometryEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)portraitGeometryNumericUpDown).EndInit();
        symmetryAxisEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)symmetryAxisNumericUpDown).EndInit();
        portraitLayersGroupBox.ResumeLayout(false);
        portraitLayersGroupBox.PerformLayout();
        portraitLayersTableLayoutPanel.ResumeLayout(false);
        portraitLayersTableLayoutPanel.PerformLayout();
        glassesReliefEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)glassesReliefNumericUpDown).EndInit();
        hairDetailEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)hairDetailNumericUpDown).EndInit();
        surfaceNormalDetailEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)surfaceNormalDetailNumericUpDown).EndInit();
        facialFeatureContourEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)facialFeatureContourNumericUpDown).EndInit();
        facialDepthContrastEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)facialDepthContrastNumericUpDown).EndInit();
        facialMicroDetailEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)facialMicroDetailNumericUpDown).EndInit();
        backgroundSuppressionEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)backgroundSuppressionNumericUpDown).EndInit();
        fullBodyDepthEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)fullBodyDepthNumericUpDown).EndInit();
        bodyMaskCleanupEditorPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)bodyMaskCleanupNumericUpDown).EndInit();
        previewTabControl.ResumeLayout(false);
        originalImageTabPage.ResumeLayout(false);
        originalImagePanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)originalImagePictureBox).EndInit();
        processedImageTabPage.ResumeLayout(false);
        processedImageLayoutPanel.ResumeLayout(false);
        processedImageToolPanel.ResumeLayout(false);
        processedImageToolPanel.PerformLayout();
        processedImagePanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)processedImagePictureBox).EndInit();
        modelPreviewTabPage.ResumeLayout(false);
        modelPreviewLayoutPanel.ResumeLayout(false);
        environmentLightPanel.ResumeLayout(false);
        environmentLightPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)environmentLightTrackBar).EndInit();
        ((System.ComponentModel.ISupportInitialize)environmentLightNumericUpDown).EndInit();
        commandFlowLayoutPanel.ResumeLayout(false);
        commandFlowLayoutPanel.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
