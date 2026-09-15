#nullable enable

namespace Rv3dViewer.App;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;
    private MenuStrip mainMenuStrip = null!;
    private ToolStripMenuItem fileMenuItem = null!;
    private ToolStripMenuItem newProjectMenuItem = null!;
    private ToolStripMenuItem addModelMenuItem = null!;
    private ToolStripMenuItem openProjectMenuItem = null!;
    private ToolStripMenuItem repairAssetsMenuItem = null!;
    private ToolStripMenuItem saveProjectMenuItem = null!;
    private ToolStripMenuItem saveAsProjectMenuItem = null!;
    private ToolStripMenuItem exportModelMenuItem = null!;
    private ToolStripMenuItem exportPreviewImageMenuItem = null!;
    private ToolStripMenuItem exitMenuItem = null!;
    private ToolStripMenuItem editMenuItem = null!;
    private ToolStripMenuItem undoMenuItem = null!;
    private ToolStripSeparator undoSeparator = null!;
    private ToolStripMenuItem exitEditModeMenuItem = null!;
    private ToolStripMenuItem selectionFunctionsMenuItem = null!;
    private ToolStripMenuItem selectModeMenuItem = null!;
    private ToolStripMenuItem selectMeshModeMenuItem = null!;
    private ToolStripMenuItem subtractModeMenuItem = null!;
    private ToolStripSeparator selectionSeparator = null!;
    private ToolStripMenuItem selectAllMenuItem = null!;
    private ToolStripMenuItem clearSelectionMenuItem = null!;
    private ToolStripMenuItem modelEditMenuItem = null!;
    private ToolStripMenuItem deleteUnusedMaterialsMenuItem = null!;
    private ToolStripMenuItem deleteSelectionMenuItem = null!;
    private ToolStripMenuItem viewMenuItem = null!;
    private ToolStripMenuItem frameSelectedMenuItem = null!;
    private ToolStripMenuItem displayMenuItem = null!;
    private ToolStripMenuItem gridMenuItem = null!;
    private ToolStripMenuItem quickPreviewMenuItem = null!;
    private ToolStripMenuItem texturesMenuItem = null!;
    private ToolStripMenuItem previewMode1MenuItem = null!;
    private ToolStripMenuItem previewMode2MenuItem = null!;
    private ToolStripMenuItem previewMode3MenuItem = null!;
    private ToolStripMenuItem previewMode4MenuItem = null!;
    private ToolStripMenuItem previewMode5MenuItem = null!;
    private ToolStripMenuItem worldAxesMenuItem = null!;
    private ToolStripMenuItem cameraGizmoMenuItem = null!;
    private ToolStripMenuItem lightGizmosMenuItem = null!;
    private ToolStripMenuItem inputInfoMenuItem = null!;
    private ToolStripMenuItem inputOverlaySettingsMenuItem = null!;
    private ToolStripMenuItem viewportColorSettingsMenuItem = null!;
    private ToolStripMenuItem selectionHighlightMenuItem = null!;
    private ToolStripMenuItem modelDisplayMenuItem = null!;
    private ToolStripMenuItem modelPointsMenuItem = null!;
    private ToolStripMenuItem modelWireframeMenuItem = null!;
    private ToolStripMenuItem modelSolidMenuItem = null!;
    private ToolStripMenuItem darkModeMenuItem = null!;
    private ToolStripMenuItem pluginMenuItem = null!;
    private ToolStripMenuItem noPluginsMenuItem = null!;
    private ToolStripSeparator pluginSeparator = null!;
    private ToolStripMenuItem reloadPluginsMenuItem = null!;
    private ToolStripMenuItem openPluginFolderMenuItem = null!;
    private ToolStripMenuItem tutorialMenuItem = null!;
    private ToolStripMenuItem updateTutorialMenuItem = null!;
    private ToolStripSeparator tutorialSeparator = null!;
    private ToolStripMenuItem noTutorialContentMenuItem = null!;
    private ToolStripMenuItem fullScreenMenuItem = null!;
    private ToolStripSeparator fileSeparator1 = null!;
    private ToolStripSeparator fileSeparator2 = null!;
    private ToolStripSeparator editSeparator = null!;
    private ToolStripSeparator viewSeparator = null!;
    private ToolStripSeparator toolSeparator = null!;
    private ToolStrip mainToolStrip = null!;
    private ToolStripButton addModelButton = null!;
    private ToolStripButton removeModelButton = null!;
    private ToolStripButton frameSelectedButton = null!;
    private ToolStripDropDownButton cameraViewButton = null!;
    private ToolStripMenuItem perspectiveCameraViewMenuItem = null!;
    private ToolStripMenuItem frontCameraViewMenuItem = null!;
    private ToolStripMenuItem backCameraViewMenuItem = null!;
    private ToolStripMenuItem leftCameraViewMenuItem = null!;
    private ToolStripMenuItem rightCameraViewMenuItem = null!;
    private ToolStripMenuItem topCameraViewMenuItem = null!;
    private ToolStripMenuItem bottomCameraViewMenuItem = null!;
    private ToolStripDropDownButton textureButton = null!;
    private ToolStripMenuItem baseColorTextureMenuItem = null!;
    private ToolStripMenuItem metallicTextureMenuItem = null!;
    private ToolStripMenuItem roughnessTextureMenuItem = null!;
    private ToolStripMenuItem normalTextureMenuItem = null!;
    private ToolStripMenuItem ambientOcclusionTextureMenuItem = null!;
    private ToolStripMenuItem emissiveTextureMenuItem = null!;
    private ToolStripMenuItem opacityTextureMenuItem = null!;
    private Panel editHintPanel = null!;
    private Label editHintLabel = null!;
    private SplitContainer rootSplitContainer = null!;
    private TableLayoutPanel sceneTreeLayoutPanel = null!;
    private CheckBox allModelsCheckBox = null!;
    private TreeView sceneTreeView = null!;
    private SplitContainer workSplitContainer = null!;
    private Panel viewportHostPanel = null!;
    private OpenTK.GLControl.GLControl viewportGlControl = null!;
    private TabControl inspectorTabControl = null!;
    private TabPage objectTabPage = null!;
    private TabPage materialTabPage = null!;
    private TableLayoutPanel materialPageLayoutPanel = null!;
    private MaterialPreviewControl materialPreviewControl = null!;
    private TabPage cameraTabPage = null!;
    private TabPage lightTabPage = null!;
    private TabPage environmentTabPage = null!;
    private TabPage skyboxTabPage = null!;
    private TableLayoutPanel skyboxLayoutPanel = null!;
    private FlowLayoutPanel skyboxButtonsPanel = null!;
    private Button importSkyboxPanoramaButton = null!;
    private Button importSkyboxFolderButton = null!;
    private Button deleteSkyboxButton = null!;
    private Button reloadSkyboxLibraryButton = null!;
    private ListBox skyboxListBox = null!;
    private FlowLayoutPanel skyboxOptionsPanel = null!;
    private CheckBox showSkyboxCheckBox = null!;
    private CheckBox useSkyboxAsEnvironmentCheckBox = null!;
    private Label skyboxRotationLabel = null!;
    private NumericUpDown skyboxRotationNumericUpDown = null!;
    private TabPage viewTabPage = null!;
    private FlowLayoutPanel viewOptionsPanel = null!;
    private CheckBox toggleAllViewGroupsCheckBox = null!;
    private Rv3dViewer.Plugin.WinForms.CollapsibleGroupPanel viewDisplayGroup = null!;
    private Rv3dViewer.Plugin.WinForms.CollapsibleGroupPanel viewColorGroup = null!;
    private ComboBox previewModeComboBox = null!;
    private CheckBox showGridCheckBox = null!;
    private CheckBox showWorldAxesCheckBox = null!;
    private CheckBox showCameraGizmoCheckBox = null!;
    private CheckBox showSelectionHighlightCheckBox = null!;
    private CheckBox wireframeCheckBox = null!;
    private CheckBox showInputInfoCheckBox = null!;
    private CheckBox darkModeCheckBox = null!;
    private Button configureOverlayAppearanceButton = null!;
    private Button configureViewportColorsButton = null!;
    private PropertyGrid objectPropertyGrid = null!;
    private SplitContainer materialSplitContainer = null!;
    private Label materialLibraryLabel = null!;
    private ListBox materialLibraryListBox = null!;
    private FlowLayoutPanel materialLibraryButtonsPanel = null!;
    private Button addLibraryMaterialButton = null!;
    private Button updateLibraryMaterialButton = null!;
    private Button deleteLibraryMaterialButton = null!;
    private Button importProjectMaterialsButton = null!;
    private Button applyLibraryMaterialButton = null!;
    private Button loadMaterialLibraryButton = null!;
    private Button saveMaterialLibraryButton = null!;
    private PropertyGrid materialPropertyGrid = null!;
    private PropertyGrid cameraPropertyGrid = null!;
    private SplitContainer lightSplitContainer = null!;
    private CheckedListBox lightListBox = null!;
    private FlowLayoutPanel lightOptionsPanel = null!;
    private CheckBox selectAllLightsCheckBox = null!;
    private CheckBox showLightGizmosCheckBox = null!;
    private Label lightTypeLabel = null!;
    private ComboBox lightTypeComboBox = null!;
    private Label lightGizmoSizeLabel = null!;
    private NumericUpDown lightGizmoSizeNumericUpDown = null!;
    private FlowLayoutPanel lightButtonsPanel = null!;
    private Button addLightButton = null!;
    private Button removeLightButton = null!;
    private Button loadLightSettingsButton = null!;
    private Button saveLightSettingsButton = null!;
    private PropertyGrid lightPropertyGrid = null!;
    private FlowLayoutPanel environmentButtonsPanel = null!;
    private Button loadEnvironmentButton = null!;
    private Button createDefaultEnvironmentButton = null!;
    private Button clearEnvironmentButton = null!;
    private PropertyGrid environmentPropertyGrid = null!;
    private StatusStrip mainStatusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private ToolStripProgressBar projectProgressBar = null!;
    private ToolStripStatusLabel statisticsLabel = null!;
    private ContextMenuStrip _materialNodeContextMenu = null!;
    private ToolStripMenuItem _addMaterialNodeToLibraryMenuItem = null!;
    private ToolStripMenuItem _addAllMaterialNodesToLibraryMenuItem = null!;
    private ToolStripMenuItem _deleteMaterialNodeMenuItem = null!;
    private ToolStripMenuItem _deleteUnusedMaterialNodesMenuItem = null!;
    private ToolStripSeparator materialNodeSeparator = null!;
    private ToolStripMenuItem _applyMaterialNodeMenuItem = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        mainMenuStrip = new MenuStrip();
        fileMenuItem = new ToolStripMenuItem();
        newProjectMenuItem = new ToolStripMenuItem();
        addModelMenuItem = new ToolStripMenuItem();
        fileSeparator1 = new ToolStripSeparator();
        openProjectMenuItem = new ToolStripMenuItem();
        repairAssetsMenuItem = new ToolStripMenuItem();
        saveProjectMenuItem = new ToolStripMenuItem();
        saveAsProjectMenuItem = new ToolStripMenuItem();
        exportModelMenuItem = new ToolStripMenuItem();
        exportPreviewImageMenuItem = new ToolStripMenuItem();
        fileSeparator2 = new ToolStripSeparator();
        exitMenuItem = new ToolStripMenuItem();
        editMenuItem = new ToolStripMenuItem();
        undoMenuItem = new ToolStripMenuItem();
        undoSeparator = new ToolStripSeparator();
        exitEditModeMenuItem = new ToolStripMenuItem();
        selectionFunctionsMenuItem = new ToolStripMenuItem();
        selectModeMenuItem = new ToolStripMenuItem();
        selectMeshModeMenuItem = new ToolStripMenuItem();
        subtractModeMenuItem = new ToolStripMenuItem();
        selectionSeparator = new ToolStripSeparator();
        selectAllMenuItem = new ToolStripMenuItem();
        clearSelectionMenuItem = new ToolStripMenuItem();
        modelEditMenuItem = new ToolStripMenuItem();
        deleteUnusedMaterialsMenuItem = new ToolStripMenuItem();
        editSeparator = new ToolStripSeparator();
        deleteSelectionMenuItem = new ToolStripMenuItem();
        viewMenuItem = new ToolStripMenuItem();
        frameSelectedMenuItem = new ToolStripMenuItem();
        fullScreenMenuItem = new ToolStripMenuItem();
        viewSeparator = new ToolStripSeparator();
        quickPreviewMenuItem = new ToolStripMenuItem();
        previewMode1MenuItem = new ToolStripMenuItem();
        previewMode2MenuItem = new ToolStripMenuItem();
        previewMode3MenuItem = new ToolStripMenuItem();
        previewMode4MenuItem = new ToolStripMenuItem();
        previewMode5MenuItem = new ToolStripMenuItem();
        displayMenuItem = new ToolStripMenuItem();
        texturesMenuItem = new ToolStripMenuItem();
        gridMenuItem = new ToolStripMenuItem();
        worldAxesMenuItem = new ToolStripMenuItem();
        cameraGizmoMenuItem = new ToolStripMenuItem();
        lightGizmosMenuItem = new ToolStripMenuItem();
        inputInfoMenuItem = new ToolStripMenuItem();
        inputOverlaySettingsMenuItem = new ToolStripMenuItem();
        viewportColorSettingsMenuItem = new ToolStripMenuItem();
        selectionHighlightMenuItem = new ToolStripMenuItem();
        modelDisplayMenuItem = new ToolStripMenuItem();
        modelPointsMenuItem = new ToolStripMenuItem();
        modelWireframeMenuItem = new ToolStripMenuItem();
        modelSolidMenuItem = new ToolStripMenuItem();
        darkModeMenuItem = new ToolStripMenuItem();
        pluginMenuItem = new ToolStripMenuItem();
        noPluginsMenuItem = new ToolStripMenuItem();
        pluginSeparator = new ToolStripSeparator();
        reloadPluginsMenuItem = new ToolStripMenuItem();
        openPluginFolderMenuItem = new ToolStripMenuItem();
        說明ToolStripMenuItem = new ToolStripMenuItem();
        tutorialMenuItem = new ToolStripMenuItem();
        updateTutorialMenuItem = new ToolStripMenuItem();
        tutorialSeparator = new ToolStripSeparator();
        noTutorialContentMenuItem = new ToolStripMenuItem();
        關於ToolStripMenuItem = new ToolStripMenuItem();
        mainToolStrip = new ToolStrip();
        addModelButton = new ToolStripButton();
        removeModelButton = new ToolStripButton();
        toolSeparator = new ToolStripSeparator();
        frameSelectedButton = new ToolStripButton();
        cameraViewButton = new ToolStripDropDownButton();
        perspectiveCameraViewMenuItem = new ToolStripMenuItem();
        frontCameraViewMenuItem = new ToolStripMenuItem();
        backCameraViewMenuItem = new ToolStripMenuItem();
        leftCameraViewMenuItem = new ToolStripMenuItem();
        rightCameraViewMenuItem = new ToolStripMenuItem();
        topCameraViewMenuItem = new ToolStripMenuItem();
        bottomCameraViewMenuItem = new ToolStripMenuItem();
        textureButton = new ToolStripDropDownButton();
        baseColorTextureMenuItem = new ToolStripMenuItem();
        metallicTextureMenuItem = new ToolStripMenuItem();
        roughnessTextureMenuItem = new ToolStripMenuItem();
        normalTextureMenuItem = new ToolStripMenuItem();
        ambientOcclusionTextureMenuItem = new ToolStripMenuItem();
        emissiveTextureMenuItem = new ToolStripMenuItem();
        opacityTextureMenuItem = new ToolStripMenuItem();
        editHintPanel = new Panel();
        editHintLabel = new Label();
        rootSplitContainer = new SplitContainer();
        sceneTreeLayoutPanel = new TableLayoutPanel();
        allModelsCheckBox = new CheckBox();
        sceneTreeView = new TreeView();
        workSplitContainer = new SplitContainer();
        viewportHostPanel = new Panel();
        viewportGlControl = new OpenTK.GLControl.GLControl();
        inspectorTabControl = new TabControl();
        objectTabPage = new TabPage();
        objectPropertyGrid = new PropertyGrid();
        materialTabPage = new TabPage();
        materialPageLayoutPanel = new TableLayoutPanel();
        materialPreviewControl = new MaterialPreviewControl();
        materialSplitContainer = new SplitContainer();
        materialLibraryListBox = new ListBox();
        materialLibraryButtonsPanel = new FlowLayoutPanel();
        addLibraryMaterialButton = new Button();
        updateLibraryMaterialButton = new Button();
        deleteLibraryMaterialButton = new Button();
        importProjectMaterialsButton = new Button();
        loadMaterialLibraryButton = new Button();
        saveMaterialLibraryButton = new Button();
        applyLibraryMaterialButton = new Button();
        materialLibraryLabel = new Label();
        materialPropertyGrid = new PropertyGrid();
        cameraTabPage = new TabPage();
        cameraPropertyGrid = new PropertyGrid();
        lightTabPage = new TabPage();
        lightSplitContainer = new SplitContainer();
        lightListBox = new CheckedListBox();
        lightButtonsPanel = new FlowLayoutPanel();
        addLightButton = new Button();
        removeLightButton = new Button();
        loadLightSettingsButton = new Button();
        saveLightSettingsButton = new Button();
        lightOptionsPanel = new FlowLayoutPanel();
        selectAllLightsCheckBox = new CheckBox();
        showLightGizmosCheckBox = new CheckBox();
        lightTypeLabel = new Label();
        lightTypeComboBox = new ComboBox();
        lightGizmoSizeLabel = new Label();
        lightGizmoSizeNumericUpDown = new NumericUpDown();
        lightPropertyGrid = new PropertyGrid();
        environmentTabPage = new TabPage();
        environmentPropertyGrid = new PropertyGrid();
        environmentButtonsPanel = new FlowLayoutPanel();
        loadEnvironmentButton = new Button();
        createDefaultEnvironmentButton = new Button();
        clearEnvironmentButton = new Button();
        skyboxTabPage = new TabPage();
        skyboxLayoutPanel = new TableLayoutPanel();
        skyboxButtonsPanel = new FlowLayoutPanel();
        importSkyboxPanoramaButton = new Button();
        importSkyboxFolderButton = new Button();
        deleteSkyboxButton = new Button();
        reloadSkyboxLibraryButton = new Button();
        skyboxListBox = new ListBox();
        skyboxOptionsPanel = new FlowLayoutPanel();
        showSkyboxCheckBox = new CheckBox();
        useSkyboxAsEnvironmentCheckBox = new CheckBox();
        skyboxRotationLabel = new Label();
        skyboxRotationNumericUpDown = new NumericUpDown();
        viewTabPage = new TabPage();
        viewOptionsPanel = new FlowLayoutPanel();
        toggleAllViewGroupsCheckBox = new CheckBox();
        viewDisplayGroup = new Rv3dViewer.Plugin.WinForms.CollapsibleGroupPanel();
        previewModeComboBox = new ComboBox();
        showGridCheckBox = new CheckBox();
        showWorldAxesCheckBox = new CheckBox();
        showCameraGizmoCheckBox = new CheckBox();
        showSelectionHighlightCheckBox = new CheckBox();
        wireframeCheckBox = new CheckBox();
        showInputInfoCheckBox = new CheckBox();
        darkModeCheckBox = new CheckBox();
        viewColorGroup = new Rv3dViewer.Plugin.WinForms.CollapsibleGroupPanel();
        configureViewportColorsButton = new Button();
        configureOverlayAppearanceButton = new Button();
        mainStatusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        projectProgressBar = new ToolStripProgressBar();
        statisticsLabel = new ToolStripStatusLabel();
        _materialNodeContextMenu = new ContextMenuStrip(components);
        _addMaterialNodeToLibraryMenuItem = new ToolStripMenuItem();
        _addAllMaterialNodesToLibraryMenuItem = new ToolStripMenuItem();
        _deleteMaterialNodeMenuItem = new ToolStripMenuItem();
        _deleteUnusedMaterialNodesMenuItem = new ToolStripMenuItem();
        materialNodeSeparator = new ToolStripSeparator();
        _applyMaterialNodeMenuItem = new ToolStripMenuItem();
        toolStripSeparator1 = new ToolStripSeparator();
        miClearAll = new ToolStripMenuItem();
        mainMenuStrip.SuspendLayout();
        mainToolStrip.SuspendLayout();
        editHintPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)rootSplitContainer).BeginInit();
        rootSplitContainer.Panel1.SuspendLayout();
        rootSplitContainer.Panel2.SuspendLayout();
        rootSplitContainer.SuspendLayout();
        sceneTreeLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)workSplitContainer).BeginInit();
        workSplitContainer.Panel1.SuspendLayout();
        workSplitContainer.Panel2.SuspendLayout();
        workSplitContainer.SuspendLayout();
        viewportHostPanel.SuspendLayout();
        inspectorTabControl.SuspendLayout();
        objectTabPage.SuspendLayout();
        materialTabPage.SuspendLayout();
        materialPageLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)materialSplitContainer).BeginInit();
        materialSplitContainer.Panel1.SuspendLayout();
        materialSplitContainer.Panel2.SuspendLayout();
        materialSplitContainer.SuspendLayout();
        materialLibraryButtonsPanel.SuspendLayout();
        cameraTabPage.SuspendLayout();
        lightTabPage.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)lightSplitContainer).BeginInit();
        lightSplitContainer.Panel1.SuspendLayout();
        lightSplitContainer.Panel2.SuspendLayout();
        lightSplitContainer.SuspendLayout();
        lightButtonsPanel.SuspendLayout();
        lightOptionsPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)lightGizmoSizeNumericUpDown).BeginInit();
        environmentTabPage.SuspendLayout();
        environmentButtonsPanel.SuspendLayout();
        skyboxTabPage.SuspendLayout();
        skyboxLayoutPanel.SuspendLayout();
        skyboxButtonsPanel.SuspendLayout();
        skyboxOptionsPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)skyboxRotationNumericUpDown).BeginInit();
        viewTabPage.SuspendLayout();
        viewOptionsPanel.SuspendLayout();
        viewDisplayGroup.ContentPanel.SuspendLayout();
        viewColorGroup.ContentPanel.SuspendLayout();
        mainStatusStrip.SuspendLayout();
        _materialNodeContextMenu.SuspendLayout();
        SuspendLayout();
        // 
        // mainMenuStrip
        // 
        mainMenuStrip.GripMargin = new Padding(2);
        mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileMenuItem, editMenuItem, viewMenuItem, pluginMenuItem, 說明ToolStripMenuItem });
        mainMenuStrip.Location = new Point(0, 0);
        mainMenuStrip.Name = "mainMenuStrip";
        mainMenuStrip.Padding = new Padding(0, 1, 0, 1);
        mainMenuStrip.Size = new Size(1049, 24);
        mainMenuStrip.TabIndex = 2;
        mainMenuStrip.ItemClicked += mainMenuStrip_ItemClicked;
        // 
        // fileMenuItem
        // 
        fileMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newProjectMenuItem, addModelMenuItem, fileSeparator1, openProjectMenuItem, repairAssetsMenuItem, saveProjectMenuItem, saveAsProjectMenuItem, toolStripSeparator1, exportModelMenuItem, exportPreviewImageMenuItem, fileSeparator2, miClearAll, exitMenuItem });
        fileMenuItem.Name = "fileMenuItem";
        fileMenuItem.Size = new Size(57, 22);
        fileMenuItem.Text = "檔案(&F)";
        // 
        // newProjectMenuItem
        // 
        newProjectMenuItem.Name = "newProjectMenuItem";
        newProjectMenuItem.Size = new Size(180, 22);
        newProjectMenuItem.Text = "新增專案";
        newProjectMenuItem.Click += NewProject_Click;
        // 
        // addModelMenuItem
        // 
        addModelMenuItem.Name = "addModelMenuItem";
        addModelMenuItem.Size = new Size(180, 22);
        addModelMenuItem.Text = "加入模型...";
        addModelMenuItem.Click += AddModel_Click;
        // 
        // fileSeparator1
        // 
        fileSeparator1.Name = "fileSeparator1";
        fileSeparator1.Size = new Size(177, 6);
        // 
        // openProjectMenuItem
        // 
        openProjectMenuItem.Name = "openProjectMenuItem";
        openProjectMenuItem.Size = new Size(180, 22);
        openProjectMenuItem.Text = "開啟專案...";
        openProjectMenuItem.Click += OpenProject_Click;
        // 
        // repairAssetsMenuItem
        // 
        repairAssetsMenuItem.Name = "repairAssetsMenuItem";
        repairAssetsMenuItem.Size = new Size(180, 22);
        repairAssetsMenuItem.Text = "修復缺失資產...";
        repairAssetsMenuItem.Click += RepairMissingAssets_Click;
        // 
        // saveProjectMenuItem
        // 
        saveProjectMenuItem.Name = "saveProjectMenuItem";
        saveProjectMenuItem.Size = new Size(180, 22);
        saveProjectMenuItem.Text = "儲存專案";
        saveProjectMenuItem.Click += SaveProject_Click;
        // 
        // saveAsProjectMenuItem
        // 
        saveAsProjectMenuItem.Name = "saveAsProjectMenuItem";
        saveAsProjectMenuItem.Size = new Size(180, 22);
        saveAsProjectMenuItem.Text = "另存專案...";
        saveAsProjectMenuItem.Click += SaveAsProject_Click;
        // 
        // exportModelMenuItem
        // 
        exportModelMenuItem.Name = "exportModelMenuItem";
        exportModelMenuItem.Size = new Size(180, 22);
        exportModelMenuItem.Text = "輸出模型...";
        exportModelMenuItem.Click += ExportModel_Click;
        // 
        // exportPreviewImageMenuItem
        // 
        exportPreviewImageMenuItem.Name = "exportPreviewImageMenuItem";
        exportPreviewImageMenuItem.Size = new Size(180, 22);
        exportPreviewImageMenuItem.Text = "輸出影像...";
        exportPreviewImageMenuItem.Click += ExportPreviewImage_Click;
        // 
        // fileSeparator2
        // 
        fileSeparator2.Name = "fileSeparator2";
        fileSeparator2.Size = new Size(177, 6);
        // 
        // exitMenuItem
        // 
        exitMenuItem.Name = "exitMenuItem";
        exitMenuItem.Size = new Size(180, 22);
        exitMenuItem.Text = "結束";
        exitMenuItem.Click += Exit_Click;
        // 
        // editMenuItem
        // 
        editMenuItem.DropDownItems.AddRange(new ToolStripItem[] { undoMenuItem, undoSeparator, exitEditModeMenuItem, selectionFunctionsMenuItem, modelEditMenuItem, editSeparator, deleteSelectionMenuItem });
        editMenuItem.Name = "editMenuItem";
        editMenuItem.Size = new Size(58, 22);
        editMenuItem.Text = "編輯(&E)";
        // 
        // undoMenuItem
        // 
        undoMenuItem.Name = "undoMenuItem";
        undoMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
        undoMenuItem.Size = new Size(180, 22);
        undoMenuItem.Text = "復原";
        undoMenuItem.Click += Undo_Click;
        // 
        // undoSeparator
        // 
        undoSeparator.Name = "undoSeparator";
        undoSeparator.Size = new Size(177, 6);
        // 
        // exitEditModeMenuItem
        // 
        exitEditModeMenuItem.Name = "exitEditModeMenuItem";
        exitEditModeMenuItem.Size = new Size(180, 22);
        exitEditModeMenuItem.Text = "退出編輯";
        exitEditModeMenuItem.Click += EditMode_Click;
        // 
        // selectionFunctionsMenuItem
        // 
        selectionFunctionsMenuItem.DropDownItems.AddRange(new ToolStripItem[] { selectModeMenuItem, selectMeshModeMenuItem, subtractModeMenuItem, selectionSeparator, selectAllMenuItem, clearSelectionMenuItem });
        selectionFunctionsMenuItem.Name = "selectionFunctionsMenuItem";
        selectionFunctionsMenuItem.Size = new Size(180, 22);
        selectionFunctionsMenuItem.Text = "選取功能";
        selectionFunctionsMenuItem.DropDownOpening += SelectionFunctionsMenuItem_DropDownOpening;
        // 
        // selectModeMenuItem
        // 
        selectModeMenuItem.Name = "selectModeMenuItem";
        selectModeMenuItem.Size = new Size(190, 22);
        selectModeMenuItem.Text = "選取模型";
        selectModeMenuItem.Click += EditMode_Click;
        // 
        // selectMeshModeMenuItem
        // 
        selectMeshModeMenuItem.Name = "selectMeshModeMenuItem";
        selectMeshModeMenuItem.Size = new Size(190, 22);
        selectMeshModeMenuItem.Text = "選取 Mesh";
        selectMeshModeMenuItem.Click += EditMode_Click;
        // 
        // subtractModeMenuItem
        // 
        subtractModeMenuItem.Name = "subtractModeMenuItem";
        subtractModeMenuItem.Size = new Size(190, 22);
        subtractModeMenuItem.Text = "減選";
        subtractModeMenuItem.Click += EditMode_Click;
        // 
        // selectionSeparator
        // 
        selectionSeparator.Name = "selectionSeparator";
        selectionSeparator.Size = new Size(187, 6);
        // 
        // selectAllMenuItem
        // 
        selectAllMenuItem.Name = "selectAllMenuItem";
        selectAllMenuItem.ShortcutKeys = Keys.Control | Keys.A;
        selectAllMenuItem.Size = new Size(190, 22);
        selectAllMenuItem.Text = "全部選取";
        selectAllMenuItem.Click += SelectAll_Click;
        // 
        // clearSelectionMenuItem
        // 
        clearSelectionMenuItem.Name = "clearSelectionMenuItem";
        clearSelectionMenuItem.ShortcutKeys = Keys.Control | Keys.U;
        clearSelectionMenuItem.Size = new Size(190, 22);
        clearSelectionMenuItem.Text = "全部取消選取";
        clearSelectionMenuItem.Click += ClearSelection_Click;
        // 
        // modelEditMenuItem
        // 
        modelEditMenuItem.DropDownItems.AddRange(new ToolStripItem[] { deleteUnusedMaterialsMenuItem });
        modelEditMenuItem.Name = "modelEditMenuItem";
        modelEditMenuItem.Size = new Size(180, 22);
        modelEditMenuItem.Text = "模型";
        modelEditMenuItem.DropDownOpening += ModelEditMenuItem_DropDownOpening;
        // 
        // deleteUnusedMaterialsMenuItem
        // 
        deleteUnusedMaterialsMenuItem.Name = "deleteUnusedMaterialsMenuItem";
        deleteUnusedMaterialsMenuItem.Size = new Size(158, 22);
        deleteUnusedMaterialsMenuItem.Text = "刪除未使用材質";
        deleteUnusedMaterialsMenuItem.Click += DeleteUnusedMaterials_Click;
        // 
        // editSeparator
        // 
        editSeparator.Name = "editSeparator";
        editSeparator.Size = new Size(177, 6);
        // 
        // deleteSelectionMenuItem
        // 
        deleteSelectionMenuItem.Name = "deleteSelectionMenuItem";
        deleteSelectionMenuItem.Size = new Size(180, 22);
        deleteSelectionMenuItem.Text = "刪除選取";
        deleteSelectionMenuItem.Click += DeleteSelection_Click;
        // 
        // viewMenuItem
        // 
        viewMenuItem.DropDownItems.AddRange(new ToolStripItem[] { frameSelectedMenuItem, fullScreenMenuItem, viewSeparator, quickPreviewMenuItem, displayMenuItem, modelDisplayMenuItem, viewportColorSettingsMenuItem, selectionHighlightMenuItem, darkModeMenuItem });
        viewMenuItem.Name = "viewMenuItem";
        viewMenuItem.Size = new Size(59, 22);
        viewMenuItem.Text = "檢視(&V)";
        // 
        // frameSelectedMenuItem
        // 
        frameSelectedMenuItem.Name = "frameSelectedMenuItem";
        frameSelectedMenuItem.Size = new Size(146, 22);
        frameSelectedMenuItem.Text = "聚焦模型";
        frameSelectedMenuItem.Click += FrameSelected_Click;
        // 
        // fullScreenMenuItem
        // 
        fullScreenMenuItem.Name = "fullScreenMenuItem";
        fullScreenMenuItem.ShortcutKeys = Keys.F11;
        fullScreenMenuItem.Size = new Size(146, 22);
        fullScreenMenuItem.Text = "全螢幕";
        fullScreenMenuItem.Click += FullScreen_Click;
        // 
        // viewSeparator
        // 
        viewSeparator.Name = "viewSeparator";
        viewSeparator.Size = new Size(143, 6);
        // 
        // quickPreviewMenuItem
        // 
        quickPreviewMenuItem.DropDownItems.AddRange(new ToolStripItem[] { previewMode1MenuItem, previewMode2MenuItem, previewMode3MenuItem, previewMode4MenuItem, previewMode5MenuItem });
        quickPreviewMenuItem.Name = "quickPreviewMenuItem";
        quickPreviewMenuItem.Size = new Size(146, 22);
        quickPreviewMenuItem.Text = "預覽模式";
        // 
        // previewMode1MenuItem
        // 
        previewMode1MenuItem.Name = "previewMode1MenuItem";
        previewMode1MenuItem.Size = new Size(192, 22);
        previewMode1MenuItem.Text = "預覽模式 1－快速";
        previewMode1MenuItem.Click += PreviewModeMenuItem_Click;
        // 
        // previewMode2MenuItem
        // 
        previewMode2MenuItem.Name = "previewMode2MenuItem";
        previewMode2MenuItem.Size = new Size(192, 22);
        previewMode2MenuItem.Text = "預覽模式 2－完整";
        previewMode2MenuItem.Click += PreviewModeMenuItem_Click;
        // 
        // previewMode3MenuItem
        // 
        previewMode3MenuItem.Name = "previewMode3MenuItem";
        previewMode3MenuItem.Size = new Size(192, 22);
        previewMode3MenuItem.Text = "預覽模式 3－攝影棚";
        previewMode3MenuItem.Click += PreviewModeMenuItem_Click;
        // 
        // previewMode4MenuItem
        // 
        previewMode4MenuItem.Name = "previewMode4MenuItem";
        previewMode4MenuItem.Size = new Size(192, 22);
        previewMode4MenuItem.Text = "預覽模式 4－珠寶";
        previewMode4MenuItem.Click += PreviewModeMenuItem_Click;
        // 
        // previewMode5MenuItem
        // 
        previewMode5MenuItem.Name = "previewMode5MenuItem";
        previewMode5MenuItem.Size = new Size(192, 22);
        previewMode5MenuItem.Text = "預覽模式 5－進階珠寶";
        previewMode5MenuItem.Click += PreviewModeMenuItem_Click;
        // 
        // displayMenuItem
        // 
        displayMenuItem.DropDownItems.AddRange(new ToolStripItem[] { texturesMenuItem, gridMenuItem, worldAxesMenuItem, cameraGizmoMenuItem, lightGizmosMenuItem, inputInfoMenuItem, inputOverlaySettingsMenuItem });
        displayMenuItem.Name = "displayMenuItem";
        displayMenuItem.Size = new Size(146, 22);
        displayMenuItem.Text = "顯示";
        // 
        // texturesMenuItem
        // 
        texturesMenuItem.Checked = true;
        texturesMenuItem.CheckOnClick = true;
        texturesMenuItem.CheckState = CheckState.Checked;
        texturesMenuItem.Name = "texturesMenuItem";
        texturesMenuItem.Size = new Size(153, 22);
        texturesMenuItem.Text = "貼圖";
        texturesMenuItem.Click += TexturesMenuItem_Click;
        // 
        // gridMenuItem
        // 
        gridMenuItem.Checked = true;
        gridMenuItem.CheckOnClick = true;
        gridMenuItem.CheckState = CheckState.Checked;
        gridMenuItem.Name = "gridMenuItem";
        gridMenuItem.Size = new Size(153, 22);
        gridMenuItem.Text = "地面格線";
        gridMenuItem.Click += Grid_Click;
        // 
        // worldAxesMenuItem
        // 
        worldAxesMenuItem.Checked = true;
        worldAxesMenuItem.CheckOnClick = true;
        worldAxesMenuItem.CheckState = CheckState.Checked;
        worldAxesMenuItem.Name = "worldAxesMenuItem";
        worldAxesMenuItem.Size = new Size(153, 22);
        worldAxesMenuItem.Text = "XYZ軸";
        worldAxesMenuItem.Click += WorldAxesMenuItem_Click;
        // 
        // cameraGizmoMenuItem
        // 
        cameraGizmoMenuItem.CheckOnClick = true;
        cameraGizmoMenuItem.Name = "cameraGizmoMenuItem";
        cameraGizmoMenuItem.Size = new Size(153, 22);
        cameraGizmoMenuItem.Text = "相機";
        cameraGizmoMenuItem.Click += CameraGizmoMenuItem_Click;
        // 
        // lightGizmosMenuItem
        // 
        lightGizmosMenuItem.CheckOnClick = true;
        lightGizmosMenuItem.Name = "lightGizmosMenuItem";
        lightGizmosMenuItem.Size = new Size(153, 22);
        lightGizmosMenuItem.Text = "燈光";
        lightGizmosMenuItem.Click += LightGizmosMenuItem_Click;
        // 
        // inputInfoMenuItem
        // 
        inputInfoMenuItem.CheckOnClick = true;
        inputInfoMenuItem.Name = "inputInfoMenuItem";
        inputInfoMenuItem.Size = new Size(153, 22);
        inputInfoMenuItem.Text = "滑鼠鍵盤資訊";
        inputInfoMenuItem.Click += InputInfoMenuItem_Click;
        // 
        // inputOverlaySettingsMenuItem
        // 
        inputOverlaySettingsMenuItem.Name = "inputOverlaySettingsMenuItem";
        inputOverlaySettingsMenuItem.Size = new Size(153, 22);
        inputOverlaySettingsMenuItem.Text = "Overlay 設定…";
        inputOverlaySettingsMenuItem.Click += InputOverlaySettingsMenuItem_Click;
        // 
        // viewportColorSettingsMenuItem
        // 
        viewportColorSettingsMenuItem.Name = "viewportColorSettingsMenuItem";
        viewportColorSettingsMenuItem.Size = new Size(146, 22);
        viewportColorSettingsMenuItem.Text = "設定顏色…";
        viewportColorSettingsMenuItem.Click += ViewportColorSettings_Click;
        // 
        // selectionHighlightMenuItem
        // 
        selectionHighlightMenuItem.Checked = true;
        selectionHighlightMenuItem.CheckOnClick = true;
        selectionHighlightMenuItem.CheckState = CheckState.Checked;
        selectionHighlightMenuItem.Name = "selectionHighlightMenuItem";
        selectionHighlightMenuItem.Size = new Size(146, 22);
        selectionHighlightMenuItem.Text = "顯示選取高亮";
        selectionHighlightMenuItem.Click += SelectionHighlight_Click;
        // 
        // modelDisplayMenuItem
        // 
        modelDisplayMenuItem.DropDownItems.AddRange(new ToolStripItem[] { modelPointsMenuItem, modelWireframeMenuItem, modelSolidMenuItem });
        modelDisplayMenuItem.Name = "modelDisplayMenuItem";
        modelDisplayMenuItem.Size = new Size(146, 22);
        modelDisplayMenuItem.Text = "模型";
        // 
        // modelPointsMenuItem
        // 
        modelPointsMenuItem.Name = "modelPointsMenuItem";
        modelPointsMenuItem.Size = new Size(180, 22);
        modelPointsMenuItem.Text = "點雲";
        modelPointsMenuItem.Click += ModelDisplayMode_Click;
        // 
        // modelWireframeMenuItem
        // 
        modelWireframeMenuItem.Name = "modelWireframeMenuItem";
        modelWireframeMenuItem.Size = new Size(180, 22);
        modelWireframeMenuItem.Text = "線框";
        modelWireframeMenuItem.Click += ModelDisplayMode_Click;
        // 
        // modelSolidMenuItem
        // 
        modelSolidMenuItem.Checked = true;
        modelSolidMenuItem.CheckState = CheckState.Checked;
        modelSolidMenuItem.Name = "modelSolidMenuItem";
        modelSolidMenuItem.Size = new Size(180, 22);
        modelSolidMenuItem.Text = "實體";
        modelSolidMenuItem.Click += ModelDisplayMode_Click;
        // 
        // darkModeMenuItem
        // 
        darkModeMenuItem.CheckOnClick = true;
        darkModeMenuItem.Name = "darkModeMenuItem";
        darkModeMenuItem.Size = new Size(146, 22);
        darkModeMenuItem.Text = "黑暗模式";
        darkModeMenuItem.CheckedChanged += DarkModeMenuItem_CheckedChanged;
        // 
        // pluginMenuItem
        // 
        pluginMenuItem.DropDownItems.AddRange(new ToolStripItem[] { noPluginsMenuItem, pluginSeparator, reloadPluginsMenuItem, openPluginFolderMenuItem });
        pluginMenuItem.Name = "pluginMenuItem";
        pluginMenuItem.Size = new Size(43, 22);
        pluginMenuItem.Text = "外掛";
        // 
        // noPluginsMenuItem
        // 
        noPluginsMenuItem.Enabled = false;
        noPluginsMenuItem.Name = "noPluginsMenuItem";
        noPluginsMenuItem.Size = new Size(180, 22);
        noPluginsMenuItem.Text = "（沒有安裝外掛）";
        // 
        // pluginSeparator
        // 
        pluginSeparator.Name = "pluginSeparator";
        pluginSeparator.Size = new Size(177, 6);
        // 
        // reloadPluginsMenuItem
        // 
        reloadPluginsMenuItem.Name = "reloadPluginsMenuItem";
        reloadPluginsMenuItem.Size = new Size(180, 22);
        reloadPluginsMenuItem.Text = "重新掃描外掛";
        reloadPluginsMenuItem.Click += ReloadPlugins_Click;
        // 
        // openPluginFolderMenuItem
        // 
        openPluginFolderMenuItem.Name = "openPluginFolderMenuItem";
        openPluginFolderMenuItem.Size = new Size(180, 22);
        openPluginFolderMenuItem.Text = "開啟外掛資料夾";
        openPluginFolderMenuItem.Click += OpenPluginFolder_Click;
        // 
        // 說明ToolStripMenuItem
        // 
        說明ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { tutorialMenuItem, 關於ToolStripMenuItem });
        說明ToolStripMenuItem.Name = "說明ToolStripMenuItem";
        說明ToolStripMenuItem.Size = new Size(43, 22);
        說明ToolStripMenuItem.Text = "說明";
        // 
        // tutorialMenuItem
        // 
        tutorialMenuItem.DropDownItems.AddRange(new ToolStripItem[] { updateTutorialMenuItem, tutorialSeparator, noTutorialContentMenuItem });
        tutorialMenuItem.Name = "tutorialMenuItem";
        tutorialMenuItem.Size = new Size(180, 22);
        tutorialMenuItem.Text = "教學";
        // 
        // updateTutorialMenuItem
        // 
        updateTutorialMenuItem.Name = "updateTutorialMenuItem";
        updateTutorialMenuItem.Size = new Size(170, 22);
        updateTutorialMenuItem.Text = "更新選單";
        updateTutorialMenuItem.Click += RefreshTutorialMenu_Click;
        // 
        // tutorialSeparator
        // 
        tutorialSeparator.Name = "tutorialSeparator";
        tutorialSeparator.Size = new Size(167, 6);
        // 
        // noTutorialContentMenuItem
        // 
        noTutorialContentMenuItem.Enabled = false;
        noTutorialContentMenuItem.Name = "noTutorialContentMenuItem";
        noTutorialContentMenuItem.Size = new Size(170, 22);
        noTutorialContentMenuItem.Text = "（沒有教學內容）";
        // 
        // 關於ToolStripMenuItem
        // 
        關於ToolStripMenuItem.Name = "關於ToolStripMenuItem";
        關於ToolStripMenuItem.Size = new Size(180, 22);
        關於ToolStripMenuItem.Text = "關於";
        關於ToolStripMenuItem.Click += 關於ToolStripMenuItem_Click;
        // 
        // mainToolStrip
        // 
        mainToolStrip.Items.AddRange(new ToolStripItem[] { addModelButton, removeModelButton, toolSeparator, frameSelectedButton, cameraViewButton, textureButton });
        mainToolStrip.Location = new Point(0, 24);
        mainToolStrip.Name = "mainToolStrip";
        mainToolStrip.Padding = new Padding(0);
        mainToolStrip.Size = new Size(1049, 25);
        mainToolStrip.TabIndex = 1;
        // 
        // addModelButton
        // 
        addModelButton.Margin = new Padding(0, 2, 0, 3);
        addModelButton.Name = "addModelButton";
        addModelButton.Size = new Size(59, 20);
        addModelButton.Text = "加入模型";
        addModelButton.Click += AddModel_Click;
        // 
        // removeModelButton
        // 
        removeModelButton.Margin = new Padding(0, 2, 0, 3);
        removeModelButton.Name = "removeModelButton";
        removeModelButton.Size = new Size(59, 20);
        removeModelButton.Text = "移除模型";
        removeModelButton.Click += RemoveModel_Click;
        // 
        // toolSeparator
        // 
        toolSeparator.Name = "toolSeparator";
        toolSeparator.Size = new Size(6, 25);
        // 
        // frameSelectedButton
        // 
        frameSelectedButton.Margin = new Padding(0, 2, 0, 3);
        frameSelectedButton.Name = "frameSelectedButton";
        frameSelectedButton.Size = new Size(59, 20);
        frameSelectedButton.Text = "聚焦模型";
        frameSelectedButton.Click += FrameSelected_Click;
        // 
        // cameraViewButton
        // 
        cameraViewButton.DropDownItems.AddRange(new ToolStripItem[] { perspectiveCameraViewMenuItem, frontCameraViewMenuItem, backCameraViewMenuItem, leftCameraViewMenuItem, rightCameraViewMenuItem, topCameraViewMenuItem, bottomCameraViewMenuItem });
        cameraViewButton.Margin = new Padding(0, 2, 0, 3);
        cameraViewButton.Name = "cameraViewButton";
        cameraViewButton.Size = new Size(68, 20);
        cameraViewButton.Text = "相機視角";
        // 
        // perspectiveCameraViewMenuItem
        // 
        perspectiveCameraViewMenuItem.Name = "perspectiveCameraViewMenuItem";
        perspectiveCameraViewMenuItem.Size = new Size(138, 22);
        perspectiveCameraViewMenuItem.Tag = Core.CameraView.Perspective;
        perspectiveCameraViewMenuItem.Text = "Perspective";
        perspectiveCameraViewMenuItem.Click += CameraView_Click;
        // 
        // frontCameraViewMenuItem
        // 
        frontCameraViewMenuItem.Name = "frontCameraViewMenuItem";
        frontCameraViewMenuItem.Size = new Size(138, 22);
        frontCameraViewMenuItem.Tag = Core.CameraView.Front;
        frontCameraViewMenuItem.Text = "Front";
        frontCameraViewMenuItem.Click += CameraView_Click;
        // 
        // backCameraViewMenuItem
        // 
        backCameraViewMenuItem.Name = "backCameraViewMenuItem";
        backCameraViewMenuItem.Size = new Size(138, 22);
        backCameraViewMenuItem.Tag = Core.CameraView.Back;
        backCameraViewMenuItem.Text = "Back";
        backCameraViewMenuItem.Click += CameraView_Click;
        // 
        // leftCameraViewMenuItem
        // 
        leftCameraViewMenuItem.Name = "leftCameraViewMenuItem";
        leftCameraViewMenuItem.Size = new Size(138, 22);
        leftCameraViewMenuItem.Tag = Core.CameraView.Left;
        leftCameraViewMenuItem.Text = "Left";
        leftCameraViewMenuItem.Click += CameraView_Click;
        // 
        // rightCameraViewMenuItem
        // 
        rightCameraViewMenuItem.Name = "rightCameraViewMenuItem";
        rightCameraViewMenuItem.Size = new Size(138, 22);
        rightCameraViewMenuItem.Tag = Core.CameraView.Right;
        rightCameraViewMenuItem.Text = "Right";
        rightCameraViewMenuItem.Click += CameraView_Click;
        // 
        // topCameraViewMenuItem
        // 
        topCameraViewMenuItem.Name = "topCameraViewMenuItem";
        topCameraViewMenuItem.Size = new Size(138, 22);
        topCameraViewMenuItem.Tag = Core.CameraView.Top;
        topCameraViewMenuItem.Text = "Top";
        topCameraViewMenuItem.Click += CameraView_Click;
        // 
        // bottomCameraViewMenuItem
        // 
        bottomCameraViewMenuItem.Name = "bottomCameraViewMenuItem";
        bottomCameraViewMenuItem.Size = new Size(138, 22);
        bottomCameraViewMenuItem.Tag = Core.CameraView.Bottom;
        bottomCameraViewMenuItem.Text = "Bottom";
        bottomCameraViewMenuItem.Click += CameraView_Click;
        // 
        // textureButton
        // 
        textureButton.DropDownItems.AddRange(new ToolStripItem[] { baseColorTextureMenuItem, metallicTextureMenuItem, roughnessTextureMenuItem, normalTextureMenuItem, ambientOcclusionTextureMenuItem, emissiveTextureMenuItem, opacityTextureMenuItem });
        textureButton.Margin = new Padding(0, 2, 0, 3);
        textureButton.Name = "textureButton";
        textureButton.Size = new Size(68, 20);
        textureButton.Text = "指定貼圖";
        // 
        // baseColorTextureMenuItem
        // 
        baseColorTextureMenuItem.Name = "baseColorTextureMenuItem";
        baseColorTextureMenuItem.Size = new Size(177, 22);
        baseColorTextureMenuItem.Text = "BaseColor";
        baseColorTextureMenuItem.Click += TextureSemantic_Click;
        // 
        // metallicTextureMenuItem
        // 
        metallicTextureMenuItem.Name = "metallicTextureMenuItem";
        metallicTextureMenuItem.Size = new Size(177, 22);
        metallicTextureMenuItem.Text = "Metallic";
        metallicTextureMenuItem.Click += TextureSemantic_Click;
        // 
        // roughnessTextureMenuItem
        // 
        roughnessTextureMenuItem.Name = "roughnessTextureMenuItem";
        roughnessTextureMenuItem.Size = new Size(177, 22);
        roughnessTextureMenuItem.Text = "Roughness";
        roughnessTextureMenuItem.Click += TextureSemantic_Click;
        // 
        // normalTextureMenuItem
        // 
        normalTextureMenuItem.Name = "normalTextureMenuItem";
        normalTextureMenuItem.Size = new Size(177, 22);
        normalTextureMenuItem.Text = "Normal";
        normalTextureMenuItem.Click += TextureSemantic_Click;
        // 
        // ambientOcclusionTextureMenuItem
        // 
        ambientOcclusionTextureMenuItem.Name = "ambientOcclusionTextureMenuItem";
        ambientOcclusionTextureMenuItem.Size = new Size(177, 22);
        ambientOcclusionTextureMenuItem.Text = "AmbientOcclusion";
        ambientOcclusionTextureMenuItem.Click += TextureSemantic_Click;
        // 
        // emissiveTextureMenuItem
        // 
        emissiveTextureMenuItem.Name = "emissiveTextureMenuItem";
        emissiveTextureMenuItem.Size = new Size(177, 22);
        emissiveTextureMenuItem.Text = "Emissive";
        emissiveTextureMenuItem.Click += TextureSemantic_Click;
        // 
        // opacityTextureMenuItem
        // 
        opacityTextureMenuItem.Name = "opacityTextureMenuItem";
        opacityTextureMenuItem.Size = new Size(177, 22);
        opacityTextureMenuItem.Text = "Opacity";
        opacityTextureMenuItem.Click += TextureSemantic_Click;
        // 
        // rootSplitContainer
        // 
        rootSplitContainer.Dock = DockStyle.Fill;
        rootSplitContainer.FixedPanel = FixedPanel.Panel1;
        rootSplitContainer.Location = new Point(0, 49);
        rootSplitContainer.Margin = new Padding(0, 1, 0, 1);
        rootSplitContainer.Name = "rootSplitContainer";
        // 
        // rootSplitContainer.Panel1
        // 
        rootSplitContainer.Panel1.Controls.Add(sceneTreeLayoutPanel);
        rootSplitContainer.Panel1MinSize = 240;
        // 
        // rootSplitContainer.Panel2
        // 
        rootSplitContainer.Panel2.Controls.Add(workSplitContainer);
        rootSplitContainer.Size = new Size(1049, 628);
        rootSplitContainer.SplitterDistance = 240;
        rootSplitContainer.SplitterWidth = 5;
        rootSplitContainer.TabIndex = 0;
        // 
        // sceneTreeLayoutPanel
        // 
        sceneTreeLayoutPanel.ColumnCount = 1;
        sceneTreeLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        sceneTreeLayoutPanel.Controls.Add(allModelsCheckBox, 0, 0);
        sceneTreeLayoutPanel.Controls.Add(sceneTreeView, 0, 1);
        sceneTreeLayoutPanel.Dock = DockStyle.Fill;
        sceneTreeLayoutPanel.Location = new Point(0, 0);
        sceneTreeLayoutPanel.Margin = new Padding(0);
        sceneTreeLayoutPanel.Name = "sceneTreeLayoutPanel";
        sceneTreeLayoutPanel.RowCount = 2;
        sceneTreeLayoutPanel.RowStyles.Add(new RowStyle());
        sceneTreeLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        sceneTreeLayoutPanel.Size = new Size(240, 628);
        sceneTreeLayoutPanel.TabIndex = 0;
        // 
        // allModelsCheckBox
        // 
        allModelsCheckBox.AutoSize = true;
        allModelsCheckBox.Dock = DockStyle.Fill;
        allModelsCheckBox.Enabled = false;
        allModelsCheckBox.Location = new Point(5, 5);
        allModelsCheckBox.Margin = new Padding(5);
        allModelsCheckBox.Name = "allModelsCheckBox";
        allModelsCheckBox.Size = new Size(230, 22);
        allModelsCheckBox.TabIndex = 0;
        allModelsCheckBox.Text = "所有模型";
        allModelsCheckBox.ThreeState = true;
        allModelsCheckBox.UseVisualStyleBackColor = true;
        allModelsCheckBox.CheckStateChanged += AllModelsCheckBox_CheckStateChanged;
        // 
        // sceneTreeView
        // 
        sceneTreeView.CheckBoxes = true;
        sceneTreeView.Dock = DockStyle.Fill;
        sceneTreeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
        sceneTreeView.HideSelection = false;
        sceneTreeView.Location = new Point(1, 33);
        sceneTreeView.Margin = new Padding(1);
        sceneTreeView.Name = "sceneTreeView";
        sceneTreeView.ShowNodeToolTips = true;
        sceneTreeView.Size = new Size(238, 594);
        sceneTreeView.TabIndex = 1;
        sceneTreeView.BeforeCheck += SceneTreeView_BeforeCheck;
        sceneTreeView.AfterCheck += SceneTreeView_AfterCheck;
        sceneTreeView.DrawNode += SceneTreeView_DrawNode;
        sceneTreeView.AfterSelect += SceneTreeView_AfterSelect;
        sceneTreeView.NodeMouseClick += SceneTreeView_NodeMouseClick;
        // 
        // workSplitContainer
        // 
        workSplitContainer.Dock = DockStyle.Fill;
        workSplitContainer.FixedPanel = FixedPanel.Panel2;
        workSplitContainer.Location = new Point(0, 0);
        workSplitContainer.Margin = new Padding(1);
        workSplitContainer.Name = "workSplitContainer";
        // 
        // workSplitContainer.Panel1
        // 
        workSplitContainer.Panel1.Controls.Add(viewportHostPanel);
        // 
        // workSplitContainer.Panel2
        // 
        workSplitContainer.Panel2.Controls.Add(inspectorTabControl);
        workSplitContainer.Panel2MinSize = 360;
        workSplitContainer.Size = new Size(804, 628);
        workSplitContainer.SplitterDistance = 356;
        workSplitContainer.SplitterWidth = 5;
        workSplitContainer.TabIndex = 0;
        // 
        // viewportHostPanel
        // 
        viewportHostPanel.BackColor = Color.Black;
        viewportHostPanel.Controls.Add(viewportGlControl);
        viewportHostPanel.Dock = DockStyle.Fill;
        viewportHostPanel.Location = new Point(0, 0);
        viewportHostPanel.Margin = new Padding(1);
        viewportHostPanel.Name = "viewportHostPanel";
        viewportHostPanel.Size = new Size(356, 628);
        viewportHostPanel.TabIndex = 0;
        // 
        // viewportGlControl
        // 
        viewportGlControl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
        viewportGlControl.APIVersion = new Version(3, 3, 0, 0);
        viewportGlControl.BackColor = Color.Black;
        viewportGlControl.Dock = DockStyle.Fill;
        viewportGlControl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
        viewportGlControl.IsEventDriven = true;
        viewportGlControl.Location = new Point(0, 0);
        viewportGlControl.Name = "viewportGlControl";
        viewportGlControl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
        viewportGlControl.SharedContext = null;
        viewportGlControl.Size = new Size(356, 628);
        viewportGlControl.TabIndex = 0;
        // 
        // inspectorTabControl
        // 
        inspectorTabControl.Controls.Add(objectTabPage);
        inspectorTabControl.Controls.Add(materialTabPage);
        inspectorTabControl.Controls.Add(cameraTabPage);
        inspectorTabControl.Controls.Add(lightTabPage);
        inspectorTabControl.Controls.Add(environmentTabPage);
        inspectorTabControl.Controls.Add(skyboxTabPage);
        inspectorTabControl.Controls.Add(viewTabPage);
        inspectorTabControl.Dock = DockStyle.Fill;
        inspectorTabControl.Location = new Point(0, 0);
        inspectorTabControl.Margin = new Padding(1);
        inspectorTabControl.Name = "inspectorTabControl";
        inspectorTabControl.SelectedIndex = 0;
        inspectorTabControl.Size = new Size(443, 628);
        inspectorTabControl.TabIndex = 0;
        inspectorTabControl.SelectedIndexChanged += InspectorTabControl_SelectedIndexChanged;
        // 
        // objectTabPage
        // 
        objectTabPage.Controls.Add(objectPropertyGrid);
        objectTabPage.Location = new Point(4, 26);
        objectTabPage.Margin = new Padding(1);
        objectTabPage.Name = "objectTabPage";
        objectTabPage.Size = new Size(435, 598);
        objectTabPage.TabIndex = 0;
        objectTabPage.Text = "物件";
        // 
        // objectPropertyGrid
        // 
        objectPropertyGrid.Dock = DockStyle.Fill;
        objectPropertyGrid.Location = new Point(0, 0);
        objectPropertyGrid.Margin = new Padding(12);
        objectPropertyGrid.Name = "objectPropertyGrid";
        objectPropertyGrid.Size = new Size(435, 598);
        objectPropertyGrid.TabIndex = 0;
        // 
        // materialTabPage
        // 
        materialTabPage.Controls.Add(materialPageLayoutPanel);
        materialTabPage.Location = new Point(4, 26);
        materialTabPage.Margin = new Padding(1);
        materialTabPage.Name = "materialTabPage";
        materialTabPage.Size = new Size(435, 598);
        materialTabPage.TabIndex = 1;
        materialTabPage.Text = "PBR 材質";
        // 
        // materialPageLayoutPanel
        // 
        materialPageLayoutPanel.ColumnCount = 1;
        materialPageLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        materialPageLayoutPanel.Controls.Add(materialPreviewControl, 0, 0);
        materialPageLayoutPanel.Controls.Add(materialSplitContainer, 0, 1);
        materialPageLayoutPanel.Dock = DockStyle.Fill;
        materialPageLayoutPanel.Location = new Point(0, 0);
        materialPageLayoutPanel.Margin = new Padding(0);
        materialPageLayoutPanel.Name = "materialPageLayoutPanel";
        materialPageLayoutPanel.RowCount = 2;
        materialPageLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        materialPageLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 75F));
        materialPageLayoutPanel.Size = new Size(435, 598);
        materialPageLayoutPanel.TabIndex = 0;
        // 
        // materialPreviewControl
        // 
        materialPreviewControl.Dock = DockStyle.Fill;
        materialPreviewControl.Location = new Point(2, 2);
        materialPreviewControl.Margin = new Padding(2);
        materialPreviewControl.MinimumSize = new Size(120, 150);
        materialPreviewControl.Name = "materialPreviewControl";
        materialPreviewControl.Padding = new Padding(8);
        materialPreviewControl.Size = new Size(431, 150);
        materialPreviewControl.TabIndex = 0;
        // 
        // materialSplitContainer
        // 
        materialSplitContainer.Dock = DockStyle.Fill;
        materialSplitContainer.FixedPanel = FixedPanel.Panel1;
        materialSplitContainer.Location = new Point(2, 151);
        materialSplitContainer.Margin = new Padding(2);
        materialSplitContainer.Name = "materialSplitContainer";
        materialSplitContainer.Orientation = Orientation.Horizontal;
        // 
        // materialSplitContainer.Panel1
        // 
        materialSplitContainer.Panel1.Controls.Add(materialLibraryListBox);
        materialSplitContainer.Panel1.Controls.Add(materialLibraryButtonsPanel);
        materialSplitContainer.Panel1.Controls.Add(materialLibraryLabel);
        // 
        // materialSplitContainer.Panel2
        // 
        materialSplitContainer.Panel2.Controls.Add(materialPropertyGrid);
        materialSplitContainer.Size = new Size(431, 445);
        materialSplitContainer.SplitterDistance = 300;
        materialSplitContainer.SplitterWidth = 5;
        materialSplitContainer.TabIndex = 0;
        // 
        // materialLibraryListBox
        // 
        materialLibraryListBox.Dock = DockStyle.Fill;
        materialLibraryListBox.FormattingEnabled = true;
        materialLibraryListBox.IntegralHeight = false;
        materialLibraryListBox.ItemHeight = 17;
        materialLibraryListBox.Location = new Point(0, 23);
        materialLibraryListBox.Margin = new Padding(2);
        materialLibraryListBox.Name = "materialLibraryListBox";
        materialLibraryListBox.Size = new Size(431, 196);
        materialLibraryListBox.TabIndex = 1;
        materialLibraryListBox.SelectedIndexChanged += MaterialLibraryListBox_SelectedIndexChanged;
        // 
        // materialLibraryButtonsPanel
        // 
        materialLibraryButtonsPanel.AutoSize = true;
        materialLibraryButtonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        materialLibraryButtonsPanel.Controls.Add(addLibraryMaterialButton);
        materialLibraryButtonsPanel.Controls.Add(updateLibraryMaterialButton);
        materialLibraryButtonsPanel.Controls.Add(deleteLibraryMaterialButton);
        materialLibraryButtonsPanel.Controls.Add(importProjectMaterialsButton);
        materialLibraryButtonsPanel.Controls.Add(loadMaterialLibraryButton);
        materialLibraryButtonsPanel.Controls.Add(saveMaterialLibraryButton);
        materialLibraryButtonsPanel.Controls.Add(applyLibraryMaterialButton);
        materialLibraryButtonsPanel.Dock = DockStyle.Bottom;
        materialLibraryButtonsPanel.Location = new Point(0, 219);
        materialLibraryButtonsPanel.Margin = new Padding(2);
        materialLibraryButtonsPanel.Name = "materialLibraryButtonsPanel";
        materialLibraryButtonsPanel.Padding = new Padding(5, 3, 0, 0);
        materialLibraryButtonsPanel.Size = new Size(431, 81);
        materialLibraryButtonsPanel.TabIndex = 2;
        // 
        // addLibraryMaterialButton
        // 
        addLibraryMaterialButton.AutoSize = true;
        addLibraryMaterialButton.Location = new Point(7, 5);
        addLibraryMaterialButton.Margin = new Padding(2);
        addLibraryMaterialButton.Name = "addLibraryMaterialButton";
        addLibraryMaterialButton.Size = new Size(62, 35);
        addLibraryMaterialButton.TabIndex = 0;
        addLibraryMaterialButton.Text = "新增";
        addLibraryMaterialButton.UseVisualStyleBackColor = true;
        addLibraryMaterialButton.Click += AddLibraryMaterial_Click;
        // 
        // updateLibraryMaterialButton
        // 
        updateLibraryMaterialButton.AutoSize = true;
        updateLibraryMaterialButton.Location = new Point(73, 5);
        updateLibraryMaterialButton.Margin = new Padding(2);
        updateLibraryMaterialButton.Name = "updateLibraryMaterialButton";
        updateLibraryMaterialButton.Size = new Size(62, 35);
        updateLibraryMaterialButton.TabIndex = 1;
        updateLibraryMaterialButton.Text = "更新";
        updateLibraryMaterialButton.UseVisualStyleBackColor = true;
        updateLibraryMaterialButton.Click += UpdateLibraryMaterial_Click;
        // 
        // deleteLibraryMaterialButton
        // 
        deleteLibraryMaterialButton.AutoSize = true;
        deleteLibraryMaterialButton.Location = new Point(139, 5);
        deleteLibraryMaterialButton.Margin = new Padding(2);
        deleteLibraryMaterialButton.Name = "deleteLibraryMaterialButton";
        deleteLibraryMaterialButton.Size = new Size(62, 35);
        deleteLibraryMaterialButton.TabIndex = 2;
        deleteLibraryMaterialButton.Text = "刪除";
        deleteLibraryMaterialButton.UseVisualStyleBackColor = true;
        deleteLibraryMaterialButton.Click += DeleteLibraryMaterial_Click;
        // 
        // importProjectMaterialsButton
        // 
        importProjectMaterialsButton.AutoSize = true;
        importProjectMaterialsButton.Location = new Point(205, 5);
        importProjectMaterialsButton.Margin = new Padding(2);
        importProjectMaterialsButton.Name = "importProjectMaterialsButton";
        importProjectMaterialsButton.Size = new Size(82, 35);
        importProjectMaterialsButton.TabIndex = 3;
        importProjectMaterialsButton.Text = "加入材質";
        importProjectMaterialsButton.UseVisualStyleBackColor = true;
        importProjectMaterialsButton.Click += ImportProjectMaterials_Click;
        // 
        // loadMaterialLibraryButton
        // 
        loadMaterialLibraryButton.AutoSize = true;
        loadMaterialLibraryButton.Location = new Point(205, 5);
        loadMaterialLibraryButton.Margin = new Padding(2);
        loadMaterialLibraryButton.Name = "loadMaterialLibraryButton";
        loadMaterialLibraryButton.Size = new Size(62, 35);
        loadMaterialLibraryButton.TabIndex = 5;
        loadMaterialLibraryButton.Text = "讀檔";
        loadMaterialLibraryButton.UseVisualStyleBackColor = true;
        loadMaterialLibraryButton.Click += LoadMaterialLibrary_Click;
        // 
        // saveMaterialLibraryButton
        // 
        saveMaterialLibraryButton.AutoSize = true;
        saveMaterialLibraryButton.Location = new Point(271, 5);
        saveMaterialLibraryButton.Margin = new Padding(2);
        saveMaterialLibraryButton.Name = "saveMaterialLibraryButton";
        saveMaterialLibraryButton.Size = new Size(62, 35);
        saveMaterialLibraryButton.TabIndex = 6;
        saveMaterialLibraryButton.Text = "存檔";
        saveMaterialLibraryButton.UseVisualStyleBackColor = true;
        saveMaterialLibraryButton.Click += SaveMaterialLibrary_Click;
        // 
        // applyLibraryMaterialButton
        // 
        applyLibraryMaterialButton.AutoSize = true;
        applyLibraryMaterialButton.Location = new Point(7, 44);
        applyLibraryMaterialButton.Margin = new Padding(2);
        applyLibraryMaterialButton.Name = "applyLibraryMaterialButton";
        applyLibraryMaterialButton.Size = new Size(129, 35);
        applyLibraryMaterialButton.TabIndex = 4;
        applyLibraryMaterialButton.Text = "套用到選取 Mesh";
        applyLibraryMaterialButton.UseVisualStyleBackColor = true;
        applyLibraryMaterialButton.Click += ApplyLibraryMaterial_Click;
        // 
        // materialLibraryLabel
        // 
        materialLibraryLabel.Dock = DockStyle.Top;
        materialLibraryLabel.Font = new Font("Microsoft JhengHei UI", 10F, FontStyle.Bold);
        materialLibraryLabel.Location = new Point(0, 0);
        materialLibraryLabel.Margin = new Padding(2, 0, 2, 0);
        materialLibraryLabel.Name = "materialLibraryLabel";
        materialLibraryLabel.Padding = new Padding(6, 4, 0, 0);
        materialLibraryLabel.Size = new Size(431, 23);
        materialLibraryLabel.TabIndex = 0;
        materialLibraryLabel.Text = "材質資料庫（選取後可在下方編輯）";
        // 
        // materialPropertyGrid
        // 
        materialPropertyGrid.Dock = DockStyle.Fill;
        materialPropertyGrid.Location = new Point(0, 0);
        materialPropertyGrid.Margin = new Padding(2);
        materialPropertyGrid.Name = "materialPropertyGrid";
        materialPropertyGrid.Size = new Size(431, 140);
        materialPropertyGrid.TabIndex = 0;
        // 
        // cameraTabPage
        // 
        cameraTabPage.Controls.Add(cameraPropertyGrid);
        cameraTabPage.Location = new Point(4, 26);
        cameraTabPage.Margin = new Padding(1);
        cameraTabPage.Name = "cameraTabPage";
        cameraTabPage.Size = new Size(435, 598);
        cameraTabPage.TabIndex = 2;
        cameraTabPage.Text = "相機";
        // 
        // cameraPropertyGrid
        // 
        cameraPropertyGrid.Dock = DockStyle.Fill;
        cameraPropertyGrid.Location = new Point(0, 0);
        cameraPropertyGrid.Margin = new Padding(2);
        cameraPropertyGrid.Name = "cameraPropertyGrid";
        cameraPropertyGrid.Size = new Size(435, 598);
        cameraPropertyGrid.TabIndex = 0;
        // 
        // lightTabPage
        // 
        lightTabPage.Controls.Add(lightSplitContainer);
        lightTabPage.Location = new Point(4, 26);
        lightTabPage.Margin = new Padding(1);
        lightTabPage.Name = "lightTabPage";
        lightTabPage.Size = new Size(435, 598);
        lightTabPage.TabIndex = 3;
        lightTabPage.Text = "燈光";
        // 
        // lightSplitContainer
        // 
        lightSplitContainer.Dock = DockStyle.Fill;
        lightSplitContainer.Location = new Point(0, 0);
        lightSplitContainer.Margin = new Padding(2);
        lightSplitContainer.Name = "lightSplitContainer";
        lightSplitContainer.Orientation = Orientation.Horizontal;
        // 
        // lightSplitContainer.Panel1
        // 
        lightSplitContainer.Panel1.Controls.Add(lightListBox);
        lightSplitContainer.Panel1.Controls.Add(lightButtonsPanel);
        lightSplitContainer.Panel1.Controls.Add(lightOptionsPanel);
        // 
        // lightSplitContainer.Panel2
        // 
        lightSplitContainer.Panel2.Controls.Add(lightPropertyGrid);
        lightSplitContainer.Size = new Size(435, 598);
        lightSplitContainer.SplitterDistance = 195;
        lightSplitContainer.SplitterWidth = 3;
        lightSplitContainer.TabIndex = 0;
        // 
        // lightListBox
        // 
        lightListBox.DisplayMember = "Name";
        lightListBox.Dock = DockStyle.Fill;
        lightListBox.Location = new Point(0, 39);
        lightListBox.Margin = new Padding(2);
        lightListBox.Name = "lightListBox";
        lightListBox.Size = new Size(435, 107);
        lightListBox.TabIndex = 0;
        lightListBox.ItemCheck += LightListBox_ItemCheck;
        lightListBox.SelectedIndexChanged += LightListBox_SelectedIndexChanged;
        lightListBox.MouseDown += LightListBox_MouseDown;
        lightListBox.MouseUp += LightListBox_MouseUp;
        // 
        // lightButtonsPanel
        // 
        lightButtonsPanel.Controls.Add(addLightButton);
        lightButtonsPanel.Controls.Add(removeLightButton);
        lightButtonsPanel.Controls.Add(loadLightSettingsButton);
        lightButtonsPanel.Controls.Add(saveLightSettingsButton);
        lightButtonsPanel.Dock = DockStyle.Bottom;
        lightButtonsPanel.Location = new Point(0, 146);
        lightButtonsPanel.Margin = new Padding(2);
        lightButtonsPanel.Name = "lightButtonsPanel";
        lightButtonsPanel.Padding = new Padding(5, 3, 0, 0);
        lightButtonsPanel.Size = new Size(435, 49);
        lightButtonsPanel.TabIndex = 1;
        // 
        // addLightButton
        // 
        addLightButton.AutoSize = true;
        addLightButton.Location = new Point(7, 5);
        addLightButton.Margin = new Padding(2);
        addLightButton.Name = "addLightButton";
        addLightButton.Size = new Size(62, 35);
        addLightButton.TabIndex = 0;
        addLightButton.Text = "新增";
        addLightButton.Click += AddLight_Click;
        // 
        // removeLightButton
        // 
        removeLightButton.AutoSize = true;
        removeLightButton.Location = new Point(73, 5);
        removeLightButton.Margin = new Padding(2);
        removeLightButton.Name = "removeLightButton";
        removeLightButton.Size = new Size(62, 35);
        removeLightButton.TabIndex = 1;
        removeLightButton.Text = "移除";
        removeLightButton.Click += RemoveLight_Click;
        // 
        // loadLightSettingsButton
        // 
        loadLightSettingsButton.AutoSize = true;
        loadLightSettingsButton.Location = new Point(139, 5);
        loadLightSettingsButton.Margin = new Padding(2);
        loadLightSettingsButton.Name = "loadLightSettingsButton";
        loadLightSettingsButton.Size = new Size(62, 35);
        loadLightSettingsButton.TabIndex = 2;
        loadLightSettingsButton.Text = "讀檔";
        loadLightSettingsButton.UseVisualStyleBackColor = true;
        loadLightSettingsButton.Click += LoadLightSettings_Click;
        // 
        // saveLightSettingsButton
        // 
        saveLightSettingsButton.AutoSize = true;
        saveLightSettingsButton.Location = new Point(205, 5);
        saveLightSettingsButton.Margin = new Padding(2);
        saveLightSettingsButton.Name = "saveLightSettingsButton";
        saveLightSettingsButton.Size = new Size(62, 35);
        saveLightSettingsButton.TabIndex = 3;
        saveLightSettingsButton.Text = "存檔";
        saveLightSettingsButton.UseVisualStyleBackColor = true;
        saveLightSettingsButton.Click += SaveLightSettings_Click;
        // 
        // lightOptionsPanel
        // 
        lightOptionsPanel.Controls.Add(selectAllLightsCheckBox);
        lightOptionsPanel.Controls.Add(showLightGizmosCheckBox);
        lightOptionsPanel.Controls.Add(lightTypeLabel);
        lightOptionsPanel.Controls.Add(lightTypeComboBox);
        lightOptionsPanel.Controls.Add(lightGizmoSizeLabel);
        lightOptionsPanel.Controls.Add(lightGizmoSizeNumericUpDown);
        lightOptionsPanel.Dock = DockStyle.Top;
        lightOptionsPanel.Location = new Point(0, 0);
        lightOptionsPanel.Margin = new Padding(2);
        lightOptionsPanel.Name = "lightOptionsPanel";
        lightOptionsPanel.Padding = new Padding(5, 3, 0, 3);
        lightOptionsPanel.Size = new Size(435, 39);
        lightOptionsPanel.TabIndex = 0;
        lightOptionsPanel.WrapContents = false;
        // 
        // selectAllLightsCheckBox
        // 
        selectAllLightsCheckBox.AutoSize = true;
        selectAllLightsCheckBox.Location = new Point(5, 6);
        selectAllLightsCheckBox.Margin = new Padding(0, 3, 6, 0);
        selectAllLightsCheckBox.Name = "selectAllLightsCheckBox";
        selectAllLightsCheckBox.Size = new Size(55, 22);
        selectAllLightsCheckBox.TabIndex = 0;
        selectAllLightsCheckBox.Text = "全選";
        selectAllLightsCheckBox.UseVisualStyleBackColor = true;
        selectAllLightsCheckBox.CheckedChanged += SelectAllLightsCheckBox_CheckedChanged;
        // 
        // showLightGizmosCheckBox
        // 
        showLightGizmosCheckBox.AutoSize = true;
        showLightGizmosCheckBox.Location = new Point(66, 6);
        showLightGizmosCheckBox.Margin = new Padding(0, 3, 6, 0);
        showLightGizmosCheckBox.Name = "showLightGizmosCheckBox";
        showLightGizmosCheckBox.Size = new Size(111, 22);
        showLightGizmosCheckBox.TabIndex = 1;
        showLightGizmosCheckBox.Text = "顯示燈光位置";
        showLightGizmosCheckBox.UseVisualStyleBackColor = true;
        showLightGizmosCheckBox.CheckedChanged += ShowLightGizmosCheckBox_CheckedChanged;
        // 
        // lightTypeLabel
        // 
        lightTypeLabel.AutoSize = true;
        lightTypeLabel.Location = new Point(183, 8);
        lightTypeLabel.Margin = new Padding(0, 5, 3, 0);
        lightTypeLabel.Name = "lightTypeLabel";
        lightTypeLabel.Size = new Size(36, 18);
        lightTypeLabel.TabIndex = 2;
        lightTypeLabel.Text = "類型";
        // 
        // lightTypeComboBox
        // 
        lightTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        lightTypeComboBox.FormattingEnabled = true;
        lightTypeComboBox.Items.AddRange(new object[] { "Directional", "Point", "Spot" });
        lightTypeComboBox.Location = new Point(222, 4);
        lightTypeComboBox.Margin = new Padding(0, 1, 0, 0);
        lightTypeComboBox.Name = "lightTypeComboBox";
        lightTypeComboBox.Size = new Size(90, 25);
        lightTypeComboBox.TabIndex = 3;
        lightTypeComboBox.SelectedIndexChanged += LightTypeComboBox_SelectedIndexChanged;
        // 
        // lightGizmoSizeLabel
        // 
        lightGizmoSizeLabel.AutoSize = true;
        lightGizmoSizeLabel.Location = new Point(312, 8);
        lightGizmoSizeLabel.Margin = new Padding(0, 5, 3, 0);
        lightGizmoSizeLabel.Name = "lightGizmoSizeLabel";
        lightGizmoSizeLabel.Size = new Size(60, 18);
        lightGizmoSizeLabel.TabIndex = 4;
        lightGizmoSizeLabel.Text = "大小(px)";
        // 
        // lightGizmoSizeNumericUpDown
        // 
        lightGizmoSizeNumericUpDown.Location = new Point(375, 4);
        lightGizmoSizeNumericUpDown.Margin = new Padding(0, 1, 0, 0);
        lightGizmoSizeNumericUpDown.Maximum = new decimal(new int[] { 96, 0, 0, 0 });
        lightGizmoSizeNumericUpDown.Minimum = new decimal(new int[] { 12, 0, 0, 0 });
        lightGizmoSizeNumericUpDown.Name = "lightGizmoSizeNumericUpDown";
        lightGizmoSizeNumericUpDown.Size = new Size(64, 24);
        lightGizmoSizeNumericUpDown.TabIndex = 5;
        lightGizmoSizeNumericUpDown.Value = new decimal(new int[] { 30, 0, 0, 0 });
        lightGizmoSizeNumericUpDown.ValueChanged += LightGizmoSizeNumericUpDown_ValueChanged;
        // 
        // lightPropertyGrid
        // 
        lightPropertyGrid.Dock = DockStyle.Fill;
        lightPropertyGrid.Location = new Point(0, 0);
        lightPropertyGrid.Margin = new Padding(2);
        lightPropertyGrid.Name = "lightPropertyGrid";
        lightPropertyGrid.Size = new Size(435, 400);
        lightPropertyGrid.TabIndex = 0;
        // 
        // environmentTabPage
        // 
        environmentTabPage.Controls.Add(environmentPropertyGrid);
        environmentTabPage.Controls.Add(environmentButtonsPanel);
        environmentTabPage.Location = new Point(4, 26);
        environmentTabPage.Margin = new Padding(1);
        environmentTabPage.Name = "environmentTabPage";
        environmentTabPage.Size = new Size(435, 598);
        environmentTabPage.TabIndex = 4;
        environmentTabPage.Text = "環境";
        // 
        // environmentPropertyGrid
        // 
        environmentPropertyGrid.Dock = DockStyle.Fill;
        environmentPropertyGrid.Location = new Point(0, 39);
        environmentPropertyGrid.Margin = new Padding(2);
        environmentPropertyGrid.Name = "environmentPropertyGrid";
        environmentPropertyGrid.Size = new Size(435, 559);
        environmentPropertyGrid.TabIndex = 1;
        // 
        // environmentButtonsPanel
        // 
        environmentButtonsPanel.AutoSize = true;
        environmentButtonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        environmentButtonsPanel.Controls.Add(loadEnvironmentButton);
        environmentButtonsPanel.Controls.Add(createDefaultEnvironmentButton);
        environmentButtonsPanel.Controls.Add(clearEnvironmentButton);
        environmentButtonsPanel.Dock = DockStyle.Top;
        environmentButtonsPanel.Location = new Point(0, 0);
        environmentButtonsPanel.Margin = new Padding(1);
        environmentButtonsPanel.Name = "environmentButtonsPanel";
        environmentButtonsPanel.Padding = new Padding(1);
        environmentButtonsPanel.Size = new Size(435, 39);
        environmentButtonsPanel.TabIndex = 0;
        // 
        // loadEnvironmentButton
        // 
        loadEnvironmentButton.AutoSize = true;
        loadEnvironmentButton.Location = new Point(2, 2);
        loadEnvironmentButton.Margin = new Padding(1);
        loadEnvironmentButton.Name = "loadEnvironmentButton";
        loadEnvironmentButton.Size = new Size(125, 35);
        loadEnvironmentButton.TabIndex = 0;
        loadEnvironmentButton.Text = "載入 HDR...";
        loadEnvironmentButton.Click += LoadEnvironment_Click;
        // 
        // createDefaultEnvironmentButton
        // 
        createDefaultEnvironmentButton.AutoSize = true;
        createDefaultEnvironmentButton.Location = new Point(129, 2);
        createDefaultEnvironmentButton.Margin = new Padding(1);
        createDefaultEnvironmentButton.Name = "createDefaultEnvironmentButton";
        createDefaultEnvironmentButton.Size = new Size(150, 35);
        createDefaultEnvironmentButton.TabIndex = 1;
        createDefaultEnvironmentButton.Text = "建立預設 HDR";
        createDefaultEnvironmentButton.Click += CreateDefaultEnvironment_Click;
        // 
        // clearEnvironmentButton
        // 
        clearEnvironmentButton.AutoSize = true;
        clearEnvironmentButton.Location = new Point(281, 2);
        clearEnvironmentButton.Margin = new Padding(1);
        clearEnvironmentButton.Name = "clearEnvironmentButton";
        clearEnvironmentButton.Size = new Size(62, 35);
        clearEnvironmentButton.TabIndex = 2;
        clearEnvironmentButton.Text = "清除";
        clearEnvironmentButton.Click += ClearEnvironment_Click;
        // 
        // skyboxTabPage
        // 
        skyboxTabPage.Controls.Add(skyboxLayoutPanel);
        skyboxTabPage.Location = new Point(4, 26);
        skyboxTabPage.Margin = new Padding(1);
        skyboxTabPage.Name = "skyboxTabPage";
        skyboxTabPage.Size = new Size(435, 598);
        skyboxTabPage.TabIndex = 5;
        skyboxTabPage.Text = "Skybox";
        // 
        // skyboxLayoutPanel
        // 
        skyboxLayoutPanel.ColumnCount = 1;
        skyboxLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        skyboxLayoutPanel.Controls.Add(skyboxButtonsPanel, 0, 0);
        skyboxLayoutPanel.Controls.Add(skyboxListBox, 0, 1);
        skyboxLayoutPanel.Controls.Add(skyboxOptionsPanel, 0, 2);
        skyboxLayoutPanel.Dock = DockStyle.Fill;
        skyboxLayoutPanel.Location = new Point(0, 0);
        skyboxLayoutPanel.Name = "skyboxLayoutPanel";
        skyboxLayoutPanel.RowCount = 3;
        skyboxLayoutPanel.RowStyles.Add(new RowStyle());
        skyboxLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        skyboxLayoutPanel.RowStyles.Add(new RowStyle());
        skyboxLayoutPanel.Size = new Size(435, 598);
        skyboxLayoutPanel.TabIndex = 0;
        // 
        // skyboxButtonsPanel
        // 
        skyboxButtonsPanel.AutoSize = true;
        skyboxButtonsPanel.Controls.Add(importSkyboxPanoramaButton);
        skyboxButtonsPanel.Controls.Add(importSkyboxFolderButton);
        skyboxButtonsPanel.Controls.Add(deleteSkyboxButton);
        skyboxButtonsPanel.Controls.Add(reloadSkyboxLibraryButton);
        skyboxButtonsPanel.Dock = DockStyle.Fill;
        skyboxButtonsPanel.Location = new Point(0, 0);
        skyboxButtonsPanel.Margin = new Padding(0);
        skyboxButtonsPanel.Name = "skyboxButtonsPanel";
        skyboxButtonsPanel.Padding = new Padding(3);
        skyboxButtonsPanel.Size = new Size(435, 82);
        skyboxButtonsPanel.TabIndex = 0;
        // 
        // importSkyboxPanoramaButton
        // 
        importSkyboxPanoramaButton.AutoSize = true;
        importSkyboxPanoramaButton.Location = new Point(5, 5);
        importSkyboxPanoramaButton.Margin = new Padding(2);
        importSkyboxPanoramaButton.Name = "importSkyboxPanoramaButton";
        importSkyboxPanoramaButton.Size = new Size(126, 35);
        importSkyboxPanoramaButton.TabIndex = 0;
        importSkyboxPanoramaButton.Text = "讀入全景圖...";
        importSkyboxPanoramaButton.UseVisualStyleBackColor = true;
        importSkyboxPanoramaButton.Click += ImportSkyboxPanorama_Click;
        // 
        // importSkyboxFolderButton
        // 
        importSkyboxFolderButton.AutoSize = true;
        importSkyboxFolderButton.Location = new Point(135, 5);
        importSkyboxFolderButton.Margin = new Padding(2);
        importSkyboxFolderButton.Name = "importSkyboxFolderButton";
        importSkyboxFolderButton.Size = new Size(126, 35);
        importSkyboxFolderButton.TabIndex = 1;
        importSkyboxFolderButton.Text = "讀入六面資料夾...";
        importSkyboxFolderButton.UseVisualStyleBackColor = true;
        importSkyboxFolderButton.Click += ImportSkyboxFolder_Click;
        // 
        // deleteSkyboxButton
        // 
        deleteSkyboxButton.AutoSize = true;
        deleteSkyboxButton.Location = new Point(265, 5);
        deleteSkyboxButton.Margin = new Padding(2);
        deleteSkyboxButton.Name = "deleteSkyboxButton";
        deleteSkyboxButton.Size = new Size(62, 35);
        deleteSkyboxButton.TabIndex = 2;
        deleteSkyboxButton.Text = "刪除";
        deleteSkyboxButton.UseVisualStyleBackColor = true;
        deleteSkyboxButton.Click += DeleteSkybox_Click;
        // 
        // reloadSkyboxLibraryButton
        // 
        reloadSkyboxLibraryButton.AutoSize = true;
        reloadSkyboxLibraryButton.Location = new Point(331, 5);
        reloadSkyboxLibraryButton.Margin = new Padding(2);
        reloadSkyboxLibraryButton.Name = "reloadSkyboxLibraryButton";
        reloadSkyboxLibraryButton.Size = new Size(92, 35);
        reloadSkyboxLibraryButton.TabIndex = 3;
        reloadSkyboxLibraryButton.Text = "重新掃描";
        reloadSkyboxLibraryButton.UseVisualStyleBackColor = true;
        reloadSkyboxLibraryButton.Click += ReloadSkyboxLibrary_Click;
        // 
        // skyboxListBox
        // 
        skyboxListBox.DisplayMember = "Name";
        skyboxListBox.Dock = DockStyle.Fill;
        skyboxListBox.FormattingEnabled = true;
        skyboxListBox.ItemHeight = 17;
        skyboxListBox.Location = new Point(3, 85);
        skyboxListBox.Name = "skyboxListBox";
        skyboxListBox.Size = new Size(429, 465);
        skyboxListBox.TabIndex = 1;
        skyboxListBox.SelectedIndexChanged += SkyboxListBox_SelectedIndexChanged;
        // 
        // skyboxOptionsPanel
        // 
        skyboxOptionsPanel.AutoSize = true;
        skyboxOptionsPanel.Controls.Add(showSkyboxCheckBox);
        skyboxOptionsPanel.Controls.Add(useSkyboxAsEnvironmentCheckBox);
        skyboxOptionsPanel.Controls.Add(skyboxRotationLabel);
        skyboxOptionsPanel.Controls.Add(skyboxRotationNumericUpDown);
        skyboxOptionsPanel.Dock = DockStyle.Fill;
        skyboxOptionsPanel.Location = new Point(0, 553);
        skyboxOptionsPanel.Margin = new Padding(0);
        skyboxOptionsPanel.Name = "skyboxOptionsPanel";
        skyboxOptionsPanel.Padding = new Padding(5, 4, 0, 4);
        skyboxOptionsPanel.Size = new Size(435, 45);
        skyboxOptionsPanel.TabIndex = 2;
        // 
        // showSkyboxCheckBox
        // 
        showSkyboxCheckBox.AutoSize = true;
        showSkyboxCheckBox.Location = new Point(8, 10);
        showSkyboxCheckBox.Margin = new Padding(3, 6, 12, 3);
        showSkyboxCheckBox.Name = "showSkyboxCheckBox";
        showSkyboxCheckBox.Size = new Size(55, 22);
        showSkyboxCheckBox.TabIndex = 0;
        showSkyboxCheckBox.Text = "顯示";
        showSkyboxCheckBox.UseVisualStyleBackColor = true;
        showSkyboxCheckBox.CheckedChanged += ShowSkyboxCheckBox_CheckedChanged;
        // 
        // useSkyboxAsEnvironmentCheckBox
        // 
        useSkyboxAsEnvironmentCheckBox.AutoSize = true;
        useSkyboxAsEnvironmentCheckBox.Location = new Point(78, 10);
        useSkyboxAsEnvironmentCheckBox.Margin = new Padding(3, 6, 12, 3);
        useSkyboxAsEnvironmentCheckBox.Name = "useSkyboxAsEnvironmentCheckBox";
        useSkyboxAsEnvironmentCheckBox.Size = new Size(139, 22);
        useSkyboxAsEnvironmentCheckBox.TabIndex = 1;
        useSkyboxAsEnvironmentCheckBox.Text = "作為環境照明";
        useSkyboxAsEnvironmentCheckBox.UseVisualStyleBackColor = true;
        useSkyboxAsEnvironmentCheckBox.CheckedChanged += UseSkyboxAsEnvironmentCheckBox_CheckedChanged;
        // 
        // skyboxRotationLabel
        // 
        skyboxRotationLabel.AutoSize = true;
        skyboxRotationLabel.Location = new Point(78, 12);
        skyboxRotationLabel.Margin = new Padding(3, 8, 3, 0);
        skyboxRotationLabel.Name = "skyboxRotationLabel";
        skyboxRotationLabel.Size = new Size(96, 18);
        skyboxRotationLabel.TabIndex = 2;
        skyboxRotationLabel.Text = "水平旋轉（度）";
        // 
        // skyboxRotationNumericUpDown
        // 
        skyboxRotationNumericUpDown.DecimalPlaces = 1;
        skyboxRotationNumericUpDown.Location = new Point(180, 8);
        skyboxRotationNumericUpDown.Margin = new Padding(3, 4, 3, 3);
        skyboxRotationNumericUpDown.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
        skyboxRotationNumericUpDown.Name = "skyboxRotationNumericUpDown";
        skyboxRotationNumericUpDown.Size = new Size(90, 24);
        skyboxRotationNumericUpDown.TabIndex = 3;
        skyboxRotationNumericUpDown.ValueChanged += SkyboxRotationNumericUpDown_ValueChanged;
        // 
        // viewTabPage
        // 
        viewTabPage.Controls.Add(viewOptionsPanel);
        viewTabPage.Location = new Point(4, 26);
        viewTabPage.Margin = new Padding(1);
        viewTabPage.Name = "viewTabPage";
        viewTabPage.Size = new Size(435, 598);
        viewTabPage.TabIndex = 6;
        viewTabPage.Text = "檢視";
        // 
        // viewOptionsPanel
        // 
        viewOptionsPanel.AutoScroll = true;
        viewOptionsPanel.Controls.Add(toggleAllViewGroupsCheckBox);
        viewOptionsPanel.Controls.Add(viewDisplayGroup);
        viewOptionsPanel.Controls.Add(viewColorGroup);
        viewOptionsPanel.Dock = DockStyle.Fill;
        viewOptionsPanel.FlowDirection = FlowDirection.TopDown;
        viewOptionsPanel.Location = new Point(0, 0);
        viewOptionsPanel.Margin = new Padding(2);
        viewOptionsPanel.Name = "viewOptionsPanel";
        viewOptionsPanel.Padding = new Padding(8);
        viewOptionsPanel.Size = new Size(435, 598);
        viewOptionsPanel.TabIndex = 0;
        viewOptionsPanel.WrapContents = false;
        viewOptionsPanel.ClientSizeChanged += ViewOptionsPanel_ClientSizeChanged;
        // 
        // toggleAllViewGroupsCheckBox
        // 
        toggleAllViewGroupsCheckBox.AutoSize = true;
        toggleAllViewGroupsCheckBox.Checked = true;
        toggleAllViewGroupsCheckBox.CheckState = CheckState.Indeterminate;
        toggleAllViewGroupsCheckBox.Location = new Point(8, 8);
        toggleAllViewGroupsCheckBox.Margin = new Padding(0, 0, 0, 8);
        toggleAllViewGroupsCheckBox.Name = "toggleAllViewGroupsCheckBox";
        toggleAllViewGroupsCheckBox.Size = new Size(83, 22);
        toggleAllViewGroupsCheckBox.TabIndex = 0;
        toggleAllViewGroupsCheckBox.Text = "開合全部";
        toggleAllViewGroupsCheckBox.ThreeState = true;
        toggleAllViewGroupsCheckBox.UseVisualStyleBackColor = true;
        toggleAllViewGroupsCheckBox.CheckStateChanged += ToggleAllViewGroupsCheckBox_CheckStateChanged;
        // 
        // viewDisplayGroup
        // 
        viewDisplayGroup.BorderStyle = BorderStyle.FixedSingle;
        // 
        // 
        // 
        viewDisplayGroup.ContentPanel.Controls.Add(previewModeComboBox);
        viewDisplayGroup.ContentPanel.Controls.Add(showGridCheckBox);
        viewDisplayGroup.ContentPanel.Controls.Add(showWorldAxesCheckBox);
        viewDisplayGroup.ContentPanel.Controls.Add(showCameraGizmoCheckBox);
        viewDisplayGroup.ContentPanel.Controls.Add(showSelectionHighlightCheckBox);
        viewDisplayGroup.ContentPanel.Controls.Add(wireframeCheckBox);
        viewDisplayGroup.ContentPanel.Controls.Add(showInputInfoCheckBox);
        viewDisplayGroup.ContentPanel.Controls.Add(darkModeCheckBox);
        viewDisplayGroup.ContentPanel.Location = new Point(0, 0);
        viewDisplayGroup.ContentPanel.Name = "contentFlowLayoutPanel";
        viewDisplayGroup.ContentPanel.TabIndex = 1;
        viewDisplayGroup.ExpandedHeight = 294;
        viewDisplayGroup.GroupText = "顯示設定";
        viewDisplayGroup.Location = new Point(8, 38);
        viewDisplayGroup.Margin = new Padding(0, 0, 0, 8);
        viewDisplayGroup.Name = "viewDisplayGroup";
        viewDisplayGroup.Size = new Size(410, 294);
        viewDisplayGroup.TabIndex = 1;
        viewDisplayGroup.ExpandedChanged += ViewGroup_ExpandedChanged;
        // 
        // previewModeComboBox
        // 
        previewModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        previewModeComboBox.FormattingEnabled = true;
        previewModeComboBox.Items.AddRange(new object[] { "預覽模式 1－快速", "預覽模式 2－完整", "預覽模式 3－攝影棚", "預覽模式 4－珠寶", "預覽模式 5－進階珠寶" });
        previewModeComboBox.Location = new Point(0, 0);
        previewModeComboBox.Margin = new Padding(0, 0, 0, 8);
        previewModeComboBox.Name = "previewModeComboBox";
        previewModeComboBox.Size = new Size(210, 25);
        previewModeComboBox.TabIndex = 0;
        previewModeComboBox.SelectedIndexChanged += PreviewModeComboBox_SelectedIndexChanged;
        // 
        // showGridCheckBox
        // 
        showGridCheckBox.AutoSize = true;
        showGridCheckBox.Location = new Point(0, 33);
        showGridCheckBox.Margin = new Padding(0, 0, 0, 8);
        showGridCheckBox.Name = "showGridCheckBox";
        showGridCheckBox.Size = new Size(111, 22);
        showGridCheckBox.TabIndex = 1;
        showGridCheckBox.Text = "顯示地面格線";
        showGridCheckBox.UseVisualStyleBackColor = true;
        showGridCheckBox.CheckedChanged += ShowGridCheckBox_CheckedChanged;
        // 
        // showWorldAxesCheckBox
        // 
        showWorldAxesCheckBox.AutoSize = true;
        showWorldAxesCheckBox.Location = new Point(111, 33);
        showWorldAxesCheckBox.Margin = new Padding(0, 0, 0, 8);
        showWorldAxesCheckBox.Name = "showWorldAxesCheckBox";
        showWorldAxesCheckBox.Size = new Size(67, 22);
        showWorldAxesCheckBox.TabIndex = 2;
        showWorldAxesCheckBox.Text = "XYZ軸";
        showWorldAxesCheckBox.UseVisualStyleBackColor = true;
        showWorldAxesCheckBox.CheckedChanged += ShowWorldAxesCheckBox_CheckedChanged;
        // 
        // showCameraGizmoCheckBox
        // 
        showCameraGizmoCheckBox.AutoSize = true;
        showCameraGizmoCheckBox.Location = new Point(0, 63);
        showCameraGizmoCheckBox.Margin = new Padding(0, 0, 0, 8);
        showCameraGizmoCheckBox.Name = "showCameraGizmoCheckBox";
        showCameraGizmoCheckBox.Size = new Size(83, 22);
        showCameraGizmoCheckBox.TabIndex = 3;
        showCameraGizmoCheckBox.Text = "顯示相機";
        showCameraGizmoCheckBox.UseVisualStyleBackColor = true;
        showCameraGizmoCheckBox.CheckedChanged += ShowCameraGizmoCheckBox_CheckedChanged;
        // 
        // showSelectionHighlightCheckBox
        // 
        showSelectionHighlightCheckBox.AutoSize = true;
        showSelectionHighlightCheckBox.Location = new Point(83, 63);
        showSelectionHighlightCheckBox.Margin = new Padding(0, 0, 0, 8);
        showSelectionHighlightCheckBox.Name = "showSelectionHighlightCheckBox";
        showSelectionHighlightCheckBox.Size = new Size(111, 22);
        showSelectionHighlightCheckBox.TabIndex = 3;
        showSelectionHighlightCheckBox.Text = "顯示選取高亮";
        showSelectionHighlightCheckBox.UseVisualStyleBackColor = true;
        showSelectionHighlightCheckBox.CheckedChanged += ShowSelectionHighlightCheckBox_CheckedChanged;
        // 
        // wireframeCheckBox
        // 
        wireframeCheckBox.AutoSize = true;
        wireframeCheckBox.Location = new Point(0, 93);
        wireframeCheckBox.Margin = new Padding(0, 0, 0, 8);
        wireframeCheckBox.Name = "wireframeCheckBox";
        wireframeCheckBox.Size = new Size(83, 22);
        wireframeCheckBox.TabIndex = 4;
        wireframeCheckBox.Text = "線框模式";
        wireframeCheckBox.UseVisualStyleBackColor = true;
        wireframeCheckBox.CheckedChanged += WireframeCheckBox_CheckedChanged;
        // 
        // showInputInfoCheckBox
        // 
        showInputInfoCheckBox.AutoSize = true;
        showInputInfoCheckBox.Location = new Point(0, 123);
        showInputInfoCheckBox.Margin = new Padding(0, 0, 0, 8);
        showInputInfoCheckBox.Name = "showInputInfoCheckBox";
        showInputInfoCheckBox.Size = new Size(139, 22);
        showInputInfoCheckBox.TabIndex = 5;
        showInputInfoCheckBox.Text = "顯示滑鼠鍵盤資訊";
        showInputInfoCheckBox.UseVisualStyleBackColor = true;
        showInputInfoCheckBox.CheckedChanged += ShowInputInfoCheckBox_CheckedChanged;
        // 
        // darkModeCheckBox
        // 
        darkModeCheckBox.AutoSize = true;
        darkModeCheckBox.Location = new Point(0, 153);
        darkModeCheckBox.Margin = new Padding(0);
        darkModeCheckBox.Name = "darkModeCheckBox";
        darkModeCheckBox.Size = new Size(83, 22);
        darkModeCheckBox.TabIndex = 6;
        darkModeCheckBox.Text = "黑暗模式";
        darkModeCheckBox.UseVisualStyleBackColor = true;
        darkModeCheckBox.CheckedChanged += DarkModeCheckBox_CheckedChanged;
        // 
        // viewColorGroup
        // 
        viewColorGroup.BorderStyle = BorderStyle.FixedSingle;
        // 
        // 
        // 
        viewColorGroup.ContentPanel.Controls.Add(configureViewportColorsButton);
        viewColorGroup.ContentPanel.Controls.Add(configureOverlayAppearanceButton);
        viewColorGroup.ContentPanel.Location = new Point(0, 0);
        viewColorGroup.ContentPanel.Name = "contentFlowLayoutPanel";
        viewColorGroup.ContentPanel.TabIndex = 1;
        viewColorGroup.ContentPanel.Visible = false;
        viewColorGroup.Expanded = false;
        viewColorGroup.ExpandedHeight = 112;
        viewColorGroup.GroupText = "顏色設定";
        viewColorGroup.Location = new Point(8, 340);
        viewColorGroup.Margin = new Padding(0);
        viewColorGroup.Name = "viewColorGroup";
        viewColorGroup.Size = new Size(410, 30);
        viewColorGroup.TabIndex = 2;
        viewColorGroup.ExpandedChanged += ViewGroup_ExpandedChanged;
        // 
        // configureViewportColorsButton
        // 
        configureViewportColorsButton.AutoSize = true;
        configureViewportColorsButton.Location = new Point(0, 0);
        configureViewportColorsButton.Margin = new Padding(0);
        configureViewportColorsButton.Name = "configureViewportColorsButton";
        configureViewportColorsButton.Size = new Size(148, 28);
        configureViewportColorsButton.TabIndex = 1;
        configureViewportColorsButton.Text = "ViewPort 顏色設定…";
        configureViewportColorsButton.UseVisualStyleBackColor = true;
        configureViewportColorsButton.Click += ViewportColorSettings_Click;
        // 
        // configureOverlayAppearanceButton
        // 
        configureOverlayAppearanceButton.AutoSize = true;
        configureOverlayAppearanceButton.Location = new Point(0, 28);
        configureOverlayAppearanceButton.Margin = new Padding(0);
        configureOverlayAppearanceButton.Name = "configureOverlayAppearanceButton";
        configureOverlayAppearanceButton.Size = new Size(190, 28);
        configureOverlayAppearanceButton.TabIndex = 0;
        configureOverlayAppearanceButton.Text = "Overlay 文字顏色與大小…";
        configureOverlayAppearanceButton.UseVisualStyleBackColor = true;
        configureOverlayAppearanceButton.Click += InputOverlaySettingsMenuItem_Click;
        // 
        // mainStatusStrip
        // 
        mainStatusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, projectProgressBar, statisticsLabel });
        mainStatusStrip.Location = new Point(0, 677);
        mainStatusStrip.Name = "mainStatusStrip";
        mainStatusStrip.Padding = new Padding(0, 0, 1, 0);
        mainStatusStrip.Size = new Size(1049, 22);
        mainStatusStrip.TabIndex = 3;
        mainStatusStrip.ItemClicked += mainStatusStrip_ItemClicked;
        // 
        // statusLabel
        // 
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(952, 17);
        statusLabel.Spring = true;
        statusLabel.Text = "就緒";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // projectProgressBar
        // 
        projectProgressBar.Name = "projectProgressBar";
        projectProgressBar.Size = new Size(180, 16);
        projectProgressBar.Style = ProgressBarStyle.Continuous;
        projectProgressBar.Visible = false;
        // 
        // statisticsLabel
        // 
        statisticsLabel.Name = "statisticsLabel";
        statisticsLabel.Size = new Size(96, 17);
        statisticsLabel.Text = "模型 0 | 三角形 0";
        // 
        // _materialNodeContextMenu
        // 
        _materialNodeContextMenu.ImageScalingSize = new Size(24, 24);
        _materialNodeContextMenu.Items.AddRange(new ToolStripItem[] { _addMaterialNodeToLibraryMenuItem, _addAllMaterialNodesToLibraryMenuItem, _deleteMaterialNodeMenuItem, _deleteUnusedMaterialNodesMenuItem, materialNodeSeparator, _applyMaterialNodeMenuItem });
        _materialNodeContextMenu.Name = "materialNodeContextMenu";
        _materialNodeContextMenu.Size = new Size(219, 120);
        _materialNodeContextMenu.Opening += _materialNodeContextMenu_Opening;
        // 
        // _addMaterialNodeToLibraryMenuItem
        // 
        _addMaterialNodeToLibraryMenuItem.Name = "_addMaterialNodeToLibraryMenuItem";
        _addMaterialNodeToLibraryMenuItem.Size = new Size(218, 22);
        _addMaterialNodeToLibraryMenuItem.Text = "新增到材質庫";
        _addMaterialNodeToLibraryMenuItem.Click += AddMaterialNodeToLibrary_Click;
        // 
        // _addAllMaterialNodesToLibraryMenuItem
        // 
        _addAllMaterialNodesToLibraryMenuItem.Name = "_addAllMaterialNodesToLibraryMenuItem";
        _addAllMaterialNodesToLibraryMenuItem.Size = new Size(218, 22);
        _addAllMaterialNodesToLibraryMenuItem.Text = "新增目前所有材質到資料庫";
        _addAllMaterialNodesToLibraryMenuItem.Click += AddAllMaterialNodesToLibrary_Click;
        // 
        // _deleteMaterialNodeMenuItem
        // 
        _deleteMaterialNodeMenuItem.Name = "_deleteMaterialNodeMenuItem";
        _deleteMaterialNodeMenuItem.Size = new Size(218, 22);
        _deleteMaterialNodeMenuItem.Text = "刪除";
        _deleteMaterialNodeMenuItem.Click += DeleteMaterialNode_Click;
        // 
        // _deleteUnusedMaterialNodesMenuItem
        // 
        _deleteUnusedMaterialNodesMenuItem.Name = "_deleteUnusedMaterialNodesMenuItem";
        _deleteUnusedMaterialNodesMenuItem.Size = new Size(218, 22);
        _deleteUnusedMaterialNodesMenuItem.Text = "刪除未使用的材質";
        _deleteUnusedMaterialNodesMenuItem.Click += DeleteUnusedMaterialNodes_Click;
        // 
        // materialNodeSeparator
        // 
        materialNodeSeparator.Name = "materialNodeSeparator";
        materialNodeSeparator.Size = new Size(215, 6);
        // 
        // _applyMaterialNodeMenuItem
        // 
        _applyMaterialNodeMenuItem.Name = "_applyMaterialNodeMenuItem";
        _applyMaterialNodeMenuItem.Size = new Size(218, 22);
        _applyMaterialNodeMenuItem.Text = "套用";
        _applyMaterialNodeMenuItem.Click += ApplyMaterialNode_Click;
        // 
        // toolStripSeparator1
        // 
        toolStripSeparator1.Name = "toolStripSeparator1";
        toolStripSeparator1.Size = new Size(177, 6);
        // 
        // miClearAll
        // 
        miClearAll.Name = "miClearAll";
        miClearAll.Size = new Size(180, 22);
        miClearAll.Text = "清除專案";
        miClearAll.Click += NewProject_Click;
        // 
        // editHintPanel
        // 
        editHintPanel.BackColor = SystemColors.Info;
        editHintPanel.Controls.Add(editHintLabel);
        editHintPanel.Dock = DockStyle.Top;
        editHintPanel.Location = new Point(0, 49);
        editHintPanel.Name = "editHintPanel";
        editHintPanel.Padding = new Padding(8, 2, 8, 2);
        editHintPanel.Size = new Size(1049, 28);
        editHintPanel.TabIndex = 4;
        // 
        // editHintLabel
        // 
        editHintLabel.AutoEllipsis = true;
        editHintLabel.BackColor = SystemColors.Info;
        editHintLabel.Dock = DockStyle.Fill;
        editHintLabel.ForeColor = SystemColors.InfoText;
        editHintLabel.Location = new Point(8, 2);
        editHintLabel.Name = "editHintLabel";
        editHintLabel.Size = new Size(1033, 24);
        editHintLabel.TabIndex = 0;
        editHintLabel.Text = "退出編輯｜左鍵拖曳：旋轉視角｜中鍵拖曳：移動 Camera Target｜右鍵拖曳：平移｜滾輪：縮放";
        editHintLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // MainForm
        // 
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(1049, 699);
        Controls.Add(rootSplitContainer);
        Controls.Add(editHintPanel);
        Controls.Add(mainToolStrip);
        Controls.Add(mainMenuStrip);
        Controls.Add(mainStatusStrip);
        Icon = (Icon)resources.GetObject("$this.Icon");
        KeyPreview = true;
        MainMenuStrip = mainMenuStrip;
        Margin = new Padding(0, 1, 0, 1);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Rv3d Viewer - OpenGL PBR";
        WindowState = FormWindowState.Maximized;
        KeyDown += MainForm_KeyDown;
        mainMenuStrip.ResumeLayout(false);
        mainMenuStrip.PerformLayout();
        mainToolStrip.ResumeLayout(false);
        mainToolStrip.PerformLayout();
        editHintPanel.ResumeLayout(false);
        rootSplitContainer.Panel1.ResumeLayout(false);
        rootSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)rootSplitContainer).EndInit();
        rootSplitContainer.ResumeLayout(false);
        sceneTreeLayoutPanel.ResumeLayout(false);
        sceneTreeLayoutPanel.PerformLayout();
        workSplitContainer.Panel1.ResumeLayout(false);
        workSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)workSplitContainer).EndInit();
        workSplitContainer.ResumeLayout(false);
        viewportHostPanel.ResumeLayout(false);
        inspectorTabControl.ResumeLayout(false);
        objectTabPage.ResumeLayout(false);
        materialTabPage.ResumeLayout(false);
        materialPageLayoutPanel.ResumeLayout(false);
        materialSplitContainer.Panel1.ResumeLayout(false);
        materialSplitContainer.Panel1.PerformLayout();
        materialSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)materialSplitContainer).EndInit();
        materialSplitContainer.ResumeLayout(false);
        materialLibraryButtonsPanel.ResumeLayout(false);
        materialLibraryButtonsPanel.PerformLayout();
        cameraTabPage.ResumeLayout(false);
        lightTabPage.ResumeLayout(false);
        lightSplitContainer.Panel1.ResumeLayout(false);
        lightSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)lightSplitContainer).EndInit();
        lightSplitContainer.ResumeLayout(false);
        lightButtonsPanel.ResumeLayout(false);
        lightButtonsPanel.PerformLayout();
        lightOptionsPanel.ResumeLayout(false);
        lightOptionsPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)lightGizmoSizeNumericUpDown).EndInit();
        environmentTabPage.ResumeLayout(false);
        environmentTabPage.PerformLayout();
        environmentButtonsPanel.ResumeLayout(false);
        environmentButtonsPanel.PerformLayout();
        skyboxTabPage.ResumeLayout(false);
        skyboxLayoutPanel.ResumeLayout(false);
        skyboxLayoutPanel.PerformLayout();
        skyboxButtonsPanel.ResumeLayout(false);
        skyboxButtonsPanel.PerformLayout();
        skyboxOptionsPanel.ResumeLayout(false);
        skyboxOptionsPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)skyboxRotationNumericUpDown).EndInit();
        viewTabPage.ResumeLayout(false);
        viewOptionsPanel.ResumeLayout(false);
        viewOptionsPanel.PerformLayout();
        viewDisplayGroup.ContentPanel.ResumeLayout(false);
        viewDisplayGroup.ContentPanel.PerformLayout();
        viewColorGroup.ContentPanel.ResumeLayout(false);
        viewColorGroup.ContentPanel.PerformLayout();
        mainStatusStrip.ResumeLayout(false);
        mainStatusStrip.PerformLayout();
        _materialNodeContextMenu.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private ToolStripMenuItem 說明ToolStripMenuItem = null!;
    private ToolStripMenuItem 關於ToolStripMenuItem = null!;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripMenuItem miClearAll;
}
