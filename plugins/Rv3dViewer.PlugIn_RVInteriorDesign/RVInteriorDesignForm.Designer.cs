#nullable enable

namespace Rv3dViewer.RVInteriorDesignPlugin;

partial class RVInteriorDesignForm
{
    private System.ComponentModel.IContainer? components;
    private Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip unifiedMenuStrip = null!;
    private ToolStripMenuItem newProjectMenuItem = null!;
    private ToolStripSeparator projectFileSeparator = null!;
    private ToolStripMenuItem saveProjectAsMenuItem = null!;
    private ToolStripSeparator exportFileSeparator = null!;
    private ToolStripMenuItem exportGlbMenuItem = null!;
    private ToolStripMenuItem exportAllGlbMenuItem = null!;
    private ToolStripMenuItem exportSelectedGlbMenuItem = null!;
    private OpenFileDialog openProjectDialog = null!;
    private SaveFileDialog saveProjectDialog = null!;
    private SaveFileDialog saveGlbDialog = null!;
    private OpenFileDialog importAssetDialog = null!;
    private ToolStripMenuItem exitEditModeMenuItem = null!;
    private ToolStripSeparator exitEditModeSeparator = null!;
    private ToolStripMenuItem editModelMenuItem = null!;
    private ToolStripMenuItem selectModelsMenuItem = null!;
    private ToolStripMenuItem subtractModelsMenuItem = null!;
    private ToolStripSeparator modelSelectionSeparator = null!;
    private ToolStripMenuItem selectAllModelsMenuItem = null!;
    private ToolStripMenuItem clearModelSelectionMenuItem = null!;
    private ToolStripSeparator clearModelsSeparator = null!;
    private ToolStripMenuItem deleteSelectedModelsMenuItem = null!;
    private ToolStripMenuItem clearAllModelsMenuItem = null!;
    private ToolStripSeparator placementDetectionSeparator = null!;
    private ToolStripMenuItem placementBoundaryDetectionMenuItem = null!;
    private ToolStripSeparator displayOptionsSeparator = null!;
    private ToolStripMenuItem showModelEdgesMenuItem = null!;
    private ToolStripMenuItem showModelDimensionsMenuItem = null!;
    private ToolStripMenuItem transparentOccludersMenuItem = null!;
    private ToolStripMenuItem autoHideForegroundWallsMenuItem = null!;
    private ToolStrip mainToolStrip = null!;
    private ToolStripButton selectToolButton = null!;
    private ToolStripButton exitEditModeToolButton = null!;
    private ToolStripButton moveToolButton = null!;
    private ToolStripButton rotateToolButton = null!;
    private ToolStripButton scaleToolButton = null!;
    private ToolStripDropDownButton mirrorToolDropDownButton = null!;
    private ToolStripMenuItem mirrorXToolStripMenuItem = null!;
    private ToolStripMenuItem mirrorYToolStripMenuItem = null!;
    private ToolStripMenuItem mirrorZToolStripMenuItem = null!;
    private ToolStripButton resetTransformToolButton = null!;
    private ToolStripSeparator transformToolSeparator = null!;
    private ToolStripDropDownButton snapToolDropDownButton = null!;
    private ToolStripMenuItem snapEnabledMenuItem = null!;
    private ToolStripSeparator snapStepToolSeparator = null!;
    private ToolStripMenuItem moveSnapDropDownMenuItem = null!;
    private ToolStripMenuItem moveSnap1CmMenuItem = null!;
    private ToolStripMenuItem moveSnap5CmMenuItem = null!;
    private ToolStripMenuItem moveSnap10CmMenuItem = null!;
    private ToolStripMenuItem moveSnap20CmMenuItem = null!;
    private ToolStripMenuItem moveSnap50CmMenuItem = null!;
    private ToolStripMenuItem rotationSnapDropDownMenuItem = null!;
    private ToolStripMenuItem rotationSnap1DegreeMenuItem = null!;
    private ToolStripMenuItem rotationSnap5DegreeMenuItem = null!;
    private ToolStripMenuItem rotationSnap15DegreeMenuItem = null!;
    private ToolStripMenuItem rotationSnap45DegreeMenuItem = null!;
    private ToolStripSeparator snapModeToolSeparator = null!;
    private ToolStripMenuItem surfaceSnapMenuItem = null!;
    private ToolStripMenuItem gridSnapMenuItem = null!;
    private ToolStripSeparator rulerToolSeparator = null!;
    private ToolStripDropDownButton rulerToolDropDownButton = null!;
    private ToolStripMenuItem topRulerMenuItem = null!;
    private ToolStripMenuItem frontRulerMenuItem = null!;
    private ToolStripMenuItem sideRulerMenuItem = null!;
    private ToolStripSeparator previewModeToolSeparator = null!;
    private ToolStripButton pbrPreviewToolButton = null!;
    private Panel explanationPanel = null!;
    private Label explanationLabel = null!;
    private Panel libraryPanel = null!;
    private TextBox assetFilterTextBox = null!;
    private TabControl libraryTabControl = null!;
    private TabPage modelsTabPage = null!;
    private SplitContainer modelLibrarySplitContainer = null!;
    private TreeView modelsTreeView = null!;
    private TabPage texturesTabPage = null!;
    private TableLayoutPanel textureLibraryTableLayoutPanel = null!;
    private ListBox texturesListBox = null!;
    private PropertyGrid textureSettingsPropertyGrid = null!;
    private Button applyTextureButton = null!;
    private TabPage materialsTabPage = null!;
    private TabPage assetLibraryTabPage = null!;
    private FlowLayoutPanel assetLibraryCommandPanel = null!;
    private Button importAssetButton = null!;
    private Button refreshAssetLibraryButton = null!;
    private Button analyzeAssetLibraryButton = null!;
    private Button deleteLibraryAssetButton = null!;
    private Button addFavoriteLibraryAssetButton = null!;
    private TableLayoutPanel materialLibraryTableLayoutPanel = null!;
    private ListBox materialsListBox = null!;
    private PropertyGrid materialPresetPropertyGrid = null!;
    private Button applyMaterialButton = null!;
    private Panel modelCommandPanel = null!;
    private TableLayoutPanel parameterCreationTableLayoutPanel = null!;
    private Label parameterCreationLabel = null!;
    private PropertyGrid parameterPropertyGrid = null!;
    private Button createParametricModelButton = null!;
    private Button createDefaultInteriorButton = null!;
    private SplitContainer designSplitContainer = null!;
    private TableLayoutPanel viewportTableLayoutPanel = null!;
    private InteriorViewportControl topViewport = null!;
    private InteriorViewportControl frontViewport = null!;
    private InteriorViewportControl rightViewport = null!;
    private InteriorViewportControl perspectiveViewport = null!;
    private InteriorPreviewViewportControl previewViewport = null!;
    private TabControl rightPanelTabControl = null!;
    private TabPage scenePropertiesTabPage = null!;
    private TabPage modelLibraryTabPage = null!;
    private SplitContainer rightWorkspaceSplitContainer = null!;
    private SplitContainer sceneTemplateSplitContainer = null!;
    private TableLayoutPanel sceneModelTableLayoutPanel = null!;
    private FlowLayoutPanel saveSceneTemplatePanel = null!;
    private TextBox sceneTemplateNameTextBox = null!;
    private Button saveSceneTemplateButton = null!;
    private GroupBox sceneTemplatesGroupBox = null!;
    private TableLayoutPanel sceneTemplatesTableLayoutPanel = null!;
    private ListView sceneTemplatesListView = null!;
    private ColumnHeader sceneTemplateNameColumnHeader = null!;
    private ColumnHeader sceneTemplateModelCountColumnHeader = null!;
    private Panel sceneTemplateCommandPanel = null!;
    private Button importSceneTemplateButton = null!;
    private Button deleteSceneTemplateButton = null!;
    private Button loadSceneTemplateButton = null!;
    private OpenFileDialog importSceneTemplateDialog = null!;
    private TabControl assetTabControl = null!;
    private TabPage furnitureTabPage = null!;
    private AssetCategoryControl furnitureAssetControl = null!;
    private TabPage applianceTabPage = null!;
    private AssetCategoryControl applianceAssetControl = null!;
    private TabPage lightingAssetTabPage = null!;
    private AssetCategoryControl lightingAssetControl = null!;
    private TabPage doorsWindowsTabPage = null!;
    private AssetCategoryControl doorsWindowsAssetControl = null!;
    private TabPage kitchenTabPage = null!;
    private AssetCategoryControl kitchenAssetControl = null!;
    private TabPage bathroomTabPage = null!;
    private AssetCategoryControl bathroomAssetControl = null!;
    private TabPage storageTabPage = null!;
    private AssetCategoryControl storageAssetControl = null!;
    private TabPage decorTabPage = null!;
    private AssetCategoryControl decorAssetControl = null!;
    private TabPage plantsTabPage = null!;
    private AssetCategoryControl plantsAssetControl = null!;
    private TabPage otherAssetsTabPage = null!;
    private AssetCategoryControl otherAssetsControl = null!;
    private TabPage customAssetsTabPage = null!;
    private AssetCategoryControl customAssetsControl = null!;
    private TabPage onlineAssetsTabPage = null!;
    private PolyHavenAssetBrowserControl polyHavenAssetBrowserControl = null!;
    private PropertyGrid objectPropertyGrid = null!;
    private InteriorSceneTreeView sceneTreeView = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private ToolStripProgressBar statusProgressBar = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        TreeNode treeNode1 = new TreeNode("平面");
        TreeNode treeNode2 = new TreeNode("球體");
        TreeNode treeNode3 = new TreeNode("長方體");
        TreeNode treeNode4 = new TreeNode("圓柱體");
        TreeNode treeNode5 = new TreeNode("半圓柱體");
        TreeNode treeNode6 = new TreeNode("圓錐");
        TreeNode treeNode7 = new TreeNode("多邊錐體");
        TreeNode treeNode8 = new TreeNode("基本模型", new TreeNode[] { treeNode1, treeNode2, treeNode3, treeNode4, treeNode5, treeNode6, treeNode7 });
        TreeNode treeNode9 = new TreeNode("房間");
        TreeNode treeNode10 = new TreeNode("牆體");
        TreeNode treeNode11 = new TreeNode("門窗牆體");
        TreeNode treeNode12 = new TreeNode("地板");
        TreeNode treeNode13 = new TreeNode("天花板");
        TreeNode treeNode14 = new TreeNode("門");
        TreeNode treeNode15 = new TreeNode("窗戶");
        TreeNode treeNode16 = new TreeNode("樑");
        TreeNode treeNode17 = new TreeNode("柱");
        TreeNode treeNode18 = new TreeNode("樓梯");
        TreeNode treeNode19 = new TreeNode("建築構件", new TreeNode[] { treeNode9, treeNode10, treeNode11, treeNode12, treeNode13, treeNode14, treeNode15, treeNode16, treeNode17, treeNode18 });
        TreeNode treeNode20 = new TreeNode("桌");
        TreeNode treeNode21 = new TreeNode("椅");
        TreeNode treeNode22 = new TreeNode("沙發");
        TreeNode treeNode23 = new TreeNode("床");
        TreeNode treeNode24 = new TreeNode("櫃體");
        TreeNode treeNode25 = new TreeNode("廚房設備");
        TreeNode treeNode26 = new TreeNode("衛浴設備");
        TreeNode treeNode27 = new TreeNode("家電");
        TreeNode treeNode28 = new TreeNode("家具與設備", new TreeNode[] { treeNode20, treeNode21, treeNode22, treeNode23, treeNode24, treeNode25, treeNode26, treeNode27 });
        TreeNode treeNode29 = new TreeNode("檯燈");
        TreeNode treeNode30 = new TreeNode("立燈");
        TreeNode treeNode31 = new TreeNode("吊燈");
        TreeNode treeNode32 = new TreeNode("吸頂燈");
        TreeNode treeNode33 = new TreeNode("壁燈");
        TreeNode treeNode34 = new TreeNode("窗簾");
        TreeNode treeNode35 = new TreeNode("地毯");
        TreeNode treeNode36 = new TreeNode("植栽");
        TreeNode treeNode37 = new TreeNode("裝飾品");
        TreeNode treeNode38 = new TreeNode("燈具與軟裝", new TreeNode[] { treeNode29, treeNode30, treeNode31, treeNode32, treeNode33, treeNode34, treeNode35, treeNode36, treeNode37 });
        TreeNode treeNode39 = new TreeNode("RV室內設計場景");
        unifiedMenuStrip = new Rv3dViewer.Plugin.WinForms.UnifiedPluginMenuStrip();
        newProjectMenuItem = new ToolStripMenuItem();
        projectFileSeparator = new ToolStripSeparator();
        saveProjectAsMenuItem = new ToolStripMenuItem();
        exportFileSeparator = new ToolStripSeparator();
        exportGlbMenuItem = new ToolStripMenuItem();
        exportAllGlbMenuItem = new ToolStripMenuItem();
        exportSelectedGlbMenuItem = new ToolStripMenuItem();
        exitEditModeSeparator = new ToolStripSeparator();
        exitEditModeMenuItem = new ToolStripMenuItem();
        editModelMenuItem = new ToolStripMenuItem();
        selectModelsMenuItem = new ToolStripMenuItem();
        subtractModelsMenuItem = new ToolStripMenuItem();
        modelSelectionSeparator = new ToolStripSeparator();
        selectAllModelsMenuItem = new ToolStripMenuItem();
        clearModelSelectionMenuItem = new ToolStripMenuItem();
        clearModelsSeparator = new ToolStripSeparator();
        deleteSelectedModelsMenuItem = new ToolStripMenuItem();
        clearAllModelsMenuItem = new ToolStripMenuItem();
        placementDetectionSeparator = new ToolStripSeparator();
        placementBoundaryDetectionMenuItem = new ToolStripMenuItem();
        displayOptionsSeparator = new ToolStripSeparator();
        showModelEdgesMenuItem = new ToolStripMenuItem();
        showModelDimensionsMenuItem = new ToolStripMenuItem();
        transparentOccludersMenuItem = new ToolStripMenuItem();
        autoHideForegroundWallsMenuItem = new ToolStripMenuItem();
        openProjectDialog = new OpenFileDialog();
        saveProjectDialog = new SaveFileDialog();
        saveGlbDialog = new SaveFileDialog();
        importAssetDialog = new OpenFileDialog();
        mainToolStrip = new ToolStrip();
        exitEditModeToolButton = new ToolStripButton();
        selectToolButton = new ToolStripButton();
        moveToolButton = new ToolStripButton();
        rotateToolButton = new ToolStripButton();
        scaleToolButton = new ToolStripButton();
        mirrorToolDropDownButton = new ToolStripDropDownButton();
        mirrorXToolStripMenuItem = new ToolStripMenuItem();
        mirrorYToolStripMenuItem = new ToolStripMenuItem();
        mirrorZToolStripMenuItem = new ToolStripMenuItem();
        resetTransformToolButton = new ToolStripButton();
        transformToolSeparator = new ToolStripSeparator();
        snapToolDropDownButton = new ToolStripDropDownButton();
        snapEnabledMenuItem = new ToolStripMenuItem();
        snapStepToolSeparator = new ToolStripSeparator();
        moveSnapDropDownMenuItem = new ToolStripMenuItem();
        moveSnap1CmMenuItem = new ToolStripMenuItem();
        moveSnap5CmMenuItem = new ToolStripMenuItem();
        moveSnap10CmMenuItem = new ToolStripMenuItem();
        moveSnap20CmMenuItem = new ToolStripMenuItem();
        moveSnap50CmMenuItem = new ToolStripMenuItem();
        rotationSnapDropDownMenuItem = new ToolStripMenuItem();
        rotationSnap1DegreeMenuItem = new ToolStripMenuItem();
        rotationSnap5DegreeMenuItem = new ToolStripMenuItem();
        rotationSnap15DegreeMenuItem = new ToolStripMenuItem();
        rotationSnap45DegreeMenuItem = new ToolStripMenuItem();
        snapModeToolSeparator = new ToolStripSeparator();
        surfaceSnapMenuItem = new ToolStripMenuItem();
        gridSnapMenuItem = new ToolStripMenuItem();
        rulerToolSeparator = new ToolStripSeparator();
        rulerToolDropDownButton = new ToolStripDropDownButton();
        topRulerMenuItem = new ToolStripMenuItem();
        frontRulerMenuItem = new ToolStripMenuItem();
        sideRulerMenuItem = new ToolStripMenuItem();
        previewModeToolSeparator = new ToolStripSeparator();
        pbrPreviewToolButton = new ToolStripButton();
        explanationPanel = new Panel();
        explanationLabel = new Label();
        libraryPanel = new Panel();
        libraryTabControl = new TabControl();
        modelsTabPage = new TabPage();
        modelLibrarySplitContainer = new SplitContainer();
        modelsTreeView = new TreeView();
        modelCommandPanel = new Panel();
        parameterCreationTableLayoutPanel = new TableLayoutPanel();
        parameterCreationLabel = new Label();
        parameterPropertyGrid = new PropertyGrid();
        createParametricModelButton = new Button();
        createDefaultInteriorButton = new Button();
        texturesTabPage = new TabPage();
        textureLibraryTableLayoutPanel = new TableLayoutPanel();
        texturesListBox = new ListBox();
        textureSettingsPropertyGrid = new PropertyGrid();
        applyTextureButton = new Button();
        materialsTabPage = new TabPage();
        materialLibraryTableLayoutPanel = new TableLayoutPanel();
        materialsListBox = new ListBox();
        materialPresetPropertyGrid = new PropertyGrid();
        applyMaterialButton = new Button();
        assetLibraryTabPage = new TabPage();
        assetTabControl = new TabControl();
        furnitureTabPage = new TabPage();
        furnitureAssetControl = new AssetCategoryControl();
        applianceTabPage = new TabPage();
        applianceAssetControl = new AssetCategoryControl();
        lightingAssetTabPage = new TabPage();
        lightingAssetControl = new AssetCategoryControl();
        doorsWindowsTabPage = new TabPage();
        doorsWindowsAssetControl = new AssetCategoryControl();
        kitchenTabPage = new TabPage();
        kitchenAssetControl = new AssetCategoryControl();
        bathroomTabPage = new TabPage();
        bathroomAssetControl = new AssetCategoryControl();
        storageTabPage = new TabPage();
        storageAssetControl = new AssetCategoryControl();
        decorTabPage = new TabPage();
        decorAssetControl = new AssetCategoryControl();
        plantsTabPage = new TabPage();
        plantsAssetControl = new AssetCategoryControl();
        otherAssetsTabPage = new TabPage();
        otherAssetsControl = new AssetCategoryControl();
        customAssetsTabPage = new TabPage();
        customAssetsControl = new AssetCategoryControl();
        onlineAssetsTabPage = new TabPage();
        polyHavenAssetBrowserControl = new PolyHavenAssetBrowserControl();
        assetLibraryCommandPanel = new FlowLayoutPanel();
        importAssetButton = new Button();
        refreshAssetLibraryButton = new Button();
        analyzeAssetLibraryButton = new Button();
        deleteLibraryAssetButton = new Button();
        addFavoriteLibraryAssetButton = new Button();
        assetFilterTextBox = new TextBox();
        designSplitContainer = new SplitContainer();
        viewportTableLayoutPanel = new TableLayoutPanel();
        topViewport = new InteriorViewportControl();
        frontViewport = new InteriorViewportControl();
        rightViewport = new InteriorViewportControl();
        perspectiveViewport = new InteriorViewportControl();
        previewViewport = new InteriorPreviewViewportControl();
        rightPanelTabControl = new TabControl();
        scenePropertiesTabPage = new TabPage();
        sceneTemplateSplitContainer = new SplitContainer();
        rightWorkspaceSplitContainer = new SplitContainer();
        sceneModelTableLayoutPanel = new TableLayoutPanel();
        sceneTreeView = new InteriorSceneTreeView();
        saveSceneTemplatePanel = new FlowLayoutPanel();
        sceneTemplateNameTextBox = new TextBox();
        saveSceneTemplateButton = new Button();
        objectPropertyGrid = new PropertyGrid();
        sceneTemplatesGroupBox = new GroupBox();
        sceneTemplatesTableLayoutPanel = new TableLayoutPanel();
        sceneTemplatesListView = new ListView();
        sceneTemplateNameColumnHeader = new ColumnHeader();
        sceneTemplateModelCountColumnHeader = new ColumnHeader();
        sceneTemplateCommandPanel = new Panel();
        loadSceneTemplateButton = new Button();
        deleteSceneTemplateButton = new Button();
        importSceneTemplateButton = new Button();
        modelLibraryTabPage = new TabPage();
        importSceneTemplateDialog = new OpenFileDialog();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        statusProgressBar = new ToolStripProgressBar();
        mainToolStrip.SuspendLayout();
        explanationPanel.SuspendLayout();
        libraryPanel.SuspendLayout();
        libraryTabControl.SuspendLayout();
        modelsTabPage.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)modelLibrarySplitContainer).BeginInit();
        modelLibrarySplitContainer.Panel1.SuspendLayout();
        modelLibrarySplitContainer.Panel2.SuspendLayout();
        modelLibrarySplitContainer.SuspendLayout();
        modelCommandPanel.SuspendLayout();
        parameterCreationTableLayoutPanel.SuspendLayout();
        texturesTabPage.SuspendLayout();
        textureLibraryTableLayoutPanel.SuspendLayout();
        materialsTabPage.SuspendLayout();
        materialLibraryTableLayoutPanel.SuspendLayout();
        assetLibraryTabPage.SuspendLayout();
        assetTabControl.SuspendLayout();
        furnitureTabPage.SuspendLayout();
        applianceTabPage.SuspendLayout();
        lightingAssetTabPage.SuspendLayout();
        doorsWindowsTabPage.SuspendLayout();
        kitchenTabPage.SuspendLayout();
        bathroomTabPage.SuspendLayout();
        storageTabPage.SuspendLayout();
        decorTabPage.SuspendLayout();
        plantsTabPage.SuspendLayout();
        otherAssetsTabPage.SuspendLayout();
        customAssetsTabPage.SuspendLayout();
        onlineAssetsTabPage.SuspendLayout();
        assetLibraryCommandPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)designSplitContainer).BeginInit();
        designSplitContainer.Panel1.SuspendLayout();
        designSplitContainer.Panel2.SuspendLayout();
        designSplitContainer.SuspendLayout();
        viewportTableLayoutPanel.SuspendLayout();
        rightPanelTabControl.SuspendLayout();
        scenePropertiesTabPage.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)sceneTemplateSplitContainer).BeginInit();
        sceneTemplateSplitContainer.Panel1.SuspendLayout();
        sceneTemplateSplitContainer.Panel2.SuspendLayout();
        sceneTemplateSplitContainer.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)rightWorkspaceSplitContainer).BeginInit();
        rightWorkspaceSplitContainer.Panel1.SuspendLayout();
        rightWorkspaceSplitContainer.Panel2.SuspendLayout();
        rightWorkspaceSplitContainer.SuspendLayout();
        sceneModelTableLayoutPanel.SuspendLayout();
        saveSceneTemplatePanel.SuspendLayout();
        sceneTemplatesGroupBox.SuspendLayout();
        sceneTemplatesTableLayoutPanel.SuspendLayout();
        sceneTemplateCommandPanel.SuspendLayout();
        modelLibraryTabPage.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // unifiedMenuStrip
        // 
        unifiedMenuStrip.ImageScalingSize = new Size(24, 24);
        unifiedMenuStrip.Location = new Point(0, 0);
        unifiedMenuStrip.Name = "unifiedMenuStrip";
        unifiedMenuStrip.Size = new Size(1440, 31);
        unifiedMenuStrip.TabIndex = 0;
        ((ToolStripMenuItem)unifiedMenuStrip.Items[0]).DropDownItems.AddRange(new ToolStripItem[] { projectFileSeparator, newProjectMenuItem, saveProjectAsMenuItem, exportFileSeparator, exportGlbMenuItem });
        ((ToolStripMenuItem)unifiedMenuStrip.Items[1]).DropDownItems.AddRange(new ToolStripItem[] { exitEditModeSeparator, exitEditModeMenuItem, editModelMenuItem, clearModelsSeparator, deleteSelectedModelsMenuItem, clearAllModelsMenuItem, placementDetectionSeparator, placementBoundaryDetectionMenuItem });
        ((ToolStripMenuItem)unifiedMenuStrip.Items[1]).DropDownOpening += EditMenuItem_DropDownOpening;
        ((ToolStripMenuItem)unifiedMenuStrip.Items[2]).DropDownItems.AddRange(new ToolStripItem[] { displayOptionsSeparator, showModelEdgesMenuItem, showModelDimensionsMenuItem, transparentOccludersMenuItem, autoHideForegroundWallsMenuItem });
        unifiedMenuStrip.ReadRequested += UnifiedMenuStrip_ReadRequested;
        unifiedMenuStrip.SaveRequested += UnifiedMenuStrip_SaveRequested;
        unifiedMenuStrip.UndoRequested += UnifiedMenuStrip_UndoRequested;
        unifiedMenuStrip.RedoRequested += UnifiedMenuStrip_RedoRequested;
        // 
        // newProjectMenuItem
        // 
        newProjectMenuItem.Name = "newProjectMenuItem";
        newProjectMenuItem.ShortcutKeys = Keys.Control | Keys.N;
        newProjectMenuItem.Size = new Size(32, 19);
        newProjectMenuItem.Text = "新增專案";
        newProjectMenuItem.Click += NewProjectMenuItem_Click;
        // 
        // projectFileSeparator
        // 
        projectFileSeparator.Name = "projectFileSeparator";
        projectFileSeparator.Size = new Size(6, 6);
        // 
        // saveProjectAsMenuItem
        // 
        saveProjectAsMenuItem.Name = "saveProjectAsMenuItem";
        saveProjectAsMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
        saveProjectAsMenuItem.Size = new Size(32, 19);
        saveProjectAsMenuItem.Text = "另存新檔";
        saveProjectAsMenuItem.Click += SaveProjectAsMenuItem_Click;
        // 
        // exportFileSeparator
        // 
        exportFileSeparator.Name = "exportFileSeparator";
        exportFileSeparator.Size = new Size(6, 6);
        // 
        // exportGlbMenuItem
        // 
        exportGlbMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exportAllGlbMenuItem, exportSelectedGlbMenuItem });
        exportGlbMenuItem.Name = "exportGlbMenuItem";
        exportGlbMenuItem.Size = new Size(32, 19);
        exportGlbMenuItem.Text = "匯出 GLB";
        // 
        // exportAllGlbMenuItem
        // 
        exportAllGlbMenuItem.Name = "exportAllGlbMenuItem";
        exportAllGlbMenuItem.Size = new Size(182, 34);
        exportAllGlbMenuItem.Text = "全部場景";
        exportAllGlbMenuItem.Click += ExportAllGlbMenuItem_Click;
        // 
        // exportSelectedGlbMenuItem
        // 
        exportSelectedGlbMenuItem.Name = "exportSelectedGlbMenuItem";
        exportSelectedGlbMenuItem.Size = new Size(182, 34);
        exportSelectedGlbMenuItem.Text = "選取模型";
        exportSelectedGlbMenuItem.Click += ExportSelectedGlbMenuItem_Click;
        // 
        // exitEditModeSeparator
        // 
        exitEditModeSeparator.Name = "exitEditModeSeparator";
        exitEditModeSeparator.Size = new Size(6, 6);
        // 
        // exitEditModeMenuItem
        // 
        exitEditModeMenuItem.Name = "exitEditModeMenuItem";
        exitEditModeMenuItem.ShortcutKeyDisplayString = "Esc";
        exitEditModeMenuItem.Size = new Size(32, 19);
        exitEditModeMenuItem.Text = "退出編輯";
        exitEditModeMenuItem.Click += ExitEditModeMenuItem_Click;
        // 
        // editModelMenuItem
        // 
        editModelMenuItem.DropDownItems.AddRange(new ToolStripItem[] { selectModelsMenuItem, subtractModelsMenuItem, modelSelectionSeparator, selectAllModelsMenuItem, clearModelSelectionMenuItem });
        editModelMenuItem.Name = "editModelMenuItem";
        editModelMenuItem.Size = new Size(32, 19);
        editModelMenuItem.Text = "模型";
        editModelMenuItem.DropDownOpening += EditModelMenuItem_DropDownOpening;
        // 
        // selectModelsMenuItem
        // 
        selectModelsMenuItem.CheckOnClick = true;
        selectModelsMenuItem.Name = "selectModelsMenuItem";
        selectModelsMenuItem.Size = new Size(285, 34);
        selectModelsMenuItem.Text = "選取";
        selectModelsMenuItem.Click += ModelSelectionModeMenuItem_Click;
        // 
        // subtractModelsMenuItem
        // 
        subtractModelsMenuItem.CheckOnClick = true;
        subtractModelsMenuItem.Name = "subtractModelsMenuItem";
        subtractModelsMenuItem.Size = new Size(285, 34);
        subtractModelsMenuItem.Text = "減選";
        subtractModelsMenuItem.Click += ModelSelectionModeMenuItem_Click;
        // 
        // modelSelectionSeparator
        // 
        modelSelectionSeparator.Name = "modelSelectionSeparator";
        modelSelectionSeparator.Size = new Size(282, 6);
        // 
        // selectAllModelsMenuItem
        // 
        selectAllModelsMenuItem.Name = "selectAllModelsMenuItem";
        selectAllModelsMenuItem.ShortcutKeys = Keys.Control | Keys.A;
        selectAllModelsMenuItem.Size = new Size(285, 34);
        selectAllModelsMenuItem.Text = "全部選取";
        selectAllModelsMenuItem.Click += SelectAllModelsMenuItem_Click;
        // 
        // clearModelSelectionMenuItem
        // 
        clearModelSelectionMenuItem.Name = "clearModelSelectionMenuItem";
        clearModelSelectionMenuItem.ShortcutKeys = Keys.Control | Keys.U;
        clearModelSelectionMenuItem.Size = new Size(285, 34);
        clearModelSelectionMenuItem.Text = "全部取消選取";
        clearModelSelectionMenuItem.Click += ClearModelSelectionMenuItem_Click;
        // 
        // clearModelsSeparator
        // 
        clearModelsSeparator.Name = "clearModelsSeparator";
        clearModelsSeparator.Size = new Size(6, 6);
        // 
        // deleteSelectedModelsMenuItem
        // 
        deleteSelectedModelsMenuItem.Name = "deleteSelectedModelsMenuItem";
        deleteSelectedModelsMenuItem.Size = new Size(32, 19);
        deleteSelectedModelsMenuItem.Text = "刪除選取模型";
        deleteSelectedModelsMenuItem.Click += DeleteSelectedModelsMenuItem_Click;
        // 
        // clearAllModelsMenuItem
        // 
        clearAllModelsMenuItem.Name = "clearAllModelsMenuItem";
        clearAllModelsMenuItem.Size = new Size(32, 19);
        clearAllModelsMenuItem.Text = "清除所有模型";
        clearAllModelsMenuItem.Click += ClearAllModelsMenuItem_Click;
        // 
        // placementDetectionSeparator
        // 
        placementDetectionSeparator.Name = "placementDetectionSeparator";
        placementDetectionSeparator.Size = new Size(6, 6);
        // 
        // placementBoundaryDetectionMenuItem
        // 
        placementBoundaryDetectionMenuItem.CheckOnClick = true;
        placementBoundaryDetectionMenuItem.Name = "placementBoundaryDetectionMenuItem";
        placementBoundaryDetectionMenuItem.Size = new Size(32, 19);
        placementBoundaryDetectionMenuItem.Text = "偵測置放邊界";
        placementBoundaryDetectionMenuItem.ToolTipText = "檢查模型重疊與支撐面邊界；關閉時使用快速置放";
        placementBoundaryDetectionMenuItem.CheckedChanged += PlacementBoundaryDetectionMenuItem_CheckedChanged;
        // 
        // displayOptionsSeparator
        // 
        displayOptionsSeparator.Name = "displayOptionsSeparator";
        // 
        // showModelEdgesMenuItem
        // 
        showModelEdgesMenuItem.CheckOnClick = true;
        showModelEdgesMenuItem.Checked = true;
        showModelEdgesMenuItem.CheckState = CheckState.Checked;
        showModelEdgesMenuItem.Name = "showModelEdgesMenuItem";
        showModelEdgesMenuItem.Text = "框線";
        showModelEdgesMenuItem.CheckedChanged += DisplayOptionMenuItem_CheckedChanged;
        // 
        // showModelDimensionsMenuItem
        // 
        showModelDimensionsMenuItem.CheckOnClick = true;
        showModelDimensionsMenuItem.Name = "showModelDimensionsMenuItem";
        showModelDimensionsMenuItem.Text = "模型尺寸";
        showModelDimensionsMenuItem.CheckedChanged += DisplayOptionMenuItem_CheckedChanged;
        // 
        // transparentOccludersMenuItem
        // 
        transparentOccludersMenuItem.CheckOnClick = true;
        transparentOccludersMenuItem.Name = "transparentOccludersMenuItem";
        transparentOccludersMenuItem.Text = "半透明";
        transparentOccludersMenuItem.CheckedChanged += DisplayOptionMenuItem_CheckedChanged;
        // 
        // autoHideForegroundWallsMenuItem
        // 
        autoHideForegroundWallsMenuItem.CheckOnClick = true;
        autoHideForegroundWallsMenuItem.Checked = true;
        autoHideForegroundWallsMenuItem.CheckState = CheckState.Checked;
        autoHideForegroundWallsMenuItem.Name = "autoHideForegroundWallsMenuItem";
        autoHideForegroundWallsMenuItem.Text = "自動隱藏前景牆";
        autoHideForegroundWallsMenuItem.CheckedChanged += DisplayOptionMenuItem_CheckedChanged;
        // 
        // openProjectDialog
        // 
        openProjectDialog.DefaultExt = "rv3dproj";
        openProjectDialog.Filter = "Rv3d Viewer 專案 (*.rv3dproj)|*.rv3dproj";
        openProjectDialog.RestoreDirectory = true;
        openProjectDialog.Title = "開啟 RV室內設計專案";
        // 
        // saveProjectDialog
        // 
        saveProjectDialog.DefaultExt = "rv3dproj";
        saveProjectDialog.Filter = "Rv3d Viewer 專案 (*.rv3dproj)|*.rv3dproj";
        saveProjectDialog.RestoreDirectory = true;
        saveProjectDialog.Title = "儲存 RV室內設計專案";
        // 
        // saveGlbDialog
        // 
        saveGlbDialog.DefaultExt = "glb";
        saveGlbDialog.Filter = "Binary glTF (*.glb)|*.glb";
        saveGlbDialog.RestoreDirectory = true;
        saveGlbDialog.Title = "匯出 RV室內設計 GLB";
        // 
        // importAssetDialog
        // 
        importAssetDialog.Filter = "glTF 模型 (*.glb;*.gltf)|*.glb;*.gltf|Binary glTF (*.glb)|*.glb|JSON glTF 資產組 (*.gltf)|*.gltf";
        importAssetDialog.Multiselect = true;
        importAssetDialog.RestoreDirectory = true;
        importAssetDialog.Title = "批次加入本機 GLB／GLTF 模型";
        // 
        // mainToolStrip
        // 
        mainToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        mainToolStrip.ImageScalingSize = new Size(24, 24);
        mainToolStrip.Items.AddRange(new ToolStripItem[] { exitEditModeToolButton, selectToolButton, moveToolButton, rotateToolButton, scaleToolButton, mirrorToolDropDownButton, resetTransformToolButton, transformToolSeparator, snapToolDropDownButton, rulerToolSeparator, rulerToolDropDownButton, previewModeToolSeparator, pbrPreviewToolButton });
        mainToolStrip.Location = new Point(0, 31);
        mainToolStrip.Name = "mainToolStrip";
        mainToolStrip.Size = new Size(1440, 32);
        mainToolStrip.TabIndex = 1;
        // 
        // exitEditModeToolButton
        // 
        exitEditModeToolButton.Name = "exitEditModeToolButton";
        exitEditModeToolButton.Size = new Size(86, 27);
        exitEditModeToolButton.Text = "退出編輯";
        exitEditModeToolButton.ToolTipText = "退出編輯模式並取消所有模型與 Mesh 選取（Esc）";
        exitEditModeToolButton.Click += ExitEditModeMenuItem_Click;
        // 
        // selectToolButton
        // 
        selectToolButton.Checked = true;
        selectToolButton.CheckOnClick = true;
        selectToolButton.CheckState = CheckState.Checked;
        selectToolButton.Name = "selectToolButton";
        selectToolButton.Size = new Size(50, 27);
        selectToolButton.Text = "選取";
        selectToolButton.Click += TransformToolButton_Click;
        // 
        // moveToolButton
        // 
        moveToolButton.CheckOnClick = true;
        moveToolButton.Name = "moveToolButton";
        moveToolButton.Size = new Size(50, 27);
        moveToolButton.Text = "移動";
        moveToolButton.Click += TransformToolButton_Click;
        // 
        // rotateToolButton
        // 
        rotateToolButton.CheckOnClick = true;
        rotateToolButton.Name = "rotateToolButton";
        rotateToolButton.Size = new Size(50, 27);
        rotateToolButton.Text = "旋轉";
        rotateToolButton.Click += TransformToolButton_Click;
        // 
        // scaleToolButton
        // 
        scaleToolButton.CheckOnClick = true;
        scaleToolButton.Name = "scaleToolButton";
        scaleToolButton.Size = new Size(50, 27);
        scaleToolButton.Text = "縮放";
        scaleToolButton.Click += TransformToolButton_Click;
        // 
        // mirrorToolDropDownButton
        // 
        mirrorToolDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { mirrorXToolStripMenuItem, mirrorYToolStripMenuItem, mirrorZToolStripMenuItem });
        mirrorToolDropDownButton.Name = "mirrorToolDropDownButton";
        mirrorToolDropDownButton.Size = new Size(64, 27);
        mirrorToolDropDownButton.Text = "鏡射";
        mirrorToolDropDownButton.ToolTipText = "沿指定軸鏡射選取的模型或 Mesh";
        // 
        // mirrorXToolStripMenuItem
        // 
        mirrorXToolStripMenuItem.Name = "mirrorXToolStripMenuItem";
        mirrorXToolStripMenuItem.Size = new Size(185, 34);
        mirrorXToolStripMenuItem.Tag = "X";
        mirrorXToolStripMenuItem.Text = "鏡射 X 軸";
        mirrorXToolStripMenuItem.Click += MirrorAxisMenuItem_Click;
        // 
        // mirrorYToolStripMenuItem
        // 
        mirrorYToolStripMenuItem.Name = "mirrorYToolStripMenuItem";
        mirrorYToolStripMenuItem.Size = new Size(185, 34);
        mirrorYToolStripMenuItem.Tag = "Y";
        mirrorYToolStripMenuItem.Text = "鏡射 Y 軸";
        mirrorYToolStripMenuItem.Click += MirrorAxisMenuItem_Click;
        // 
        // mirrorZToolStripMenuItem
        // 
        mirrorZToolStripMenuItem.Name = "mirrorZToolStripMenuItem";
        mirrorZToolStripMenuItem.Size = new Size(185, 34);
        mirrorZToolStripMenuItem.Tag = "Z";
        mirrorZToolStripMenuItem.Text = "鏡射 Z 軸";
        mirrorZToolStripMenuItem.Click += MirrorAxisMenuItem_Click;
        // 
        // resetTransformToolButton
        // 
        resetTransformToolButton.Name = "resetTransformToolButton";
        resetTransformToolButton.Size = new Size(86, 27);
        resetTransformToolButton.Text = "重設變換";
        resetTransformToolButton.ToolTipText = "將選取的模型或 Mesh 回復到原始位置、旋轉與縮放";
        resetTransformToolButton.Click += ResetTransformToolButton_Click;
        // 
        // transformToolSeparator
        // 
        transformToolSeparator.Name = "transformToolSeparator";
        transformToolSeparator.Size = new Size(6, 32);
        // 
        // snapToolDropDownButton
        // 
        snapToolDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { snapEnabledMenuItem, snapStepToolSeparator, moveSnapDropDownMenuItem, rotationSnapDropDownMenuItem, snapModeToolSeparator, surfaceSnapMenuItem, gridSnapMenuItem });
        snapToolDropDownButton.Name = "snapToolDropDownButton";
        snapToolDropDownButton.Size = new Size(79, 27);
        snapToolDropDownButton.Text = "吸附 ✓";
        snapToolDropDownButton.ToolTipText = "設定移動、旋轉、格點與表面吸附";
        // 
        // snapEnabledMenuItem
        // 
        snapEnabledMenuItem.Checked = true;
        snapEnabledMenuItem.CheckOnClick = true;
        snapEnabledMenuItem.CheckState = CheckState.Checked;
        snapEnabledMenuItem.Name = "snapEnabledMenuItem";
        snapEnabledMenuItem.Size = new Size(182, 34);
        snapEnabledMenuItem.Text = "啟用吸附";
        snapEnabledMenuItem.CheckedChanged += SnapSettingsMenuItem_Changed;
        // 
        // snapStepToolSeparator
        // 
        snapStepToolSeparator.Name = "snapStepToolSeparator";
        snapStepToolSeparator.Size = new Size(179, 6);
        // 
        // moveSnapDropDownMenuItem
        // 
        moveSnapDropDownMenuItem.DropDownItems.AddRange(new ToolStripItem[] { moveSnap1CmMenuItem, moveSnap5CmMenuItem, moveSnap10CmMenuItem, moveSnap20CmMenuItem, moveSnap50CmMenuItem });
        moveSnapDropDownMenuItem.Name = "moveSnapDropDownMenuItem";
        moveSnapDropDownMenuItem.Size = new Size(182, 34);
        moveSnapDropDownMenuItem.Text = "移動步距";
        // 
        // moveSnap1CmMenuItem
        // 
        moveSnap1CmMenuItem.Name = "moveSnap1CmMenuItem";
        moveSnap1CmMenuItem.Size = new Size(161, 34);
        moveSnap1CmMenuItem.Tag = 1;
        moveSnap1CmMenuItem.Text = "1 cm";
        moveSnap1CmMenuItem.Click += MoveSnapMenuItem_Click;
        // 
        // moveSnap5CmMenuItem
        // 
        moveSnap5CmMenuItem.Name = "moveSnap5CmMenuItem";
        moveSnap5CmMenuItem.Size = new Size(161, 34);
        moveSnap5CmMenuItem.Tag = 5;
        moveSnap5CmMenuItem.Text = "5 cm";
        moveSnap5CmMenuItem.Click += MoveSnapMenuItem_Click;
        // 
        // moveSnap10CmMenuItem
        // 
        moveSnap10CmMenuItem.Checked = true;
        moveSnap10CmMenuItem.CheckState = CheckState.Checked;
        moveSnap10CmMenuItem.Name = "moveSnap10CmMenuItem";
        moveSnap10CmMenuItem.Size = new Size(161, 34);
        moveSnap10CmMenuItem.Tag = 10;
        moveSnap10CmMenuItem.Text = "10 cm";
        moveSnap10CmMenuItem.Click += MoveSnapMenuItem_Click;
        // 
        // moveSnap20CmMenuItem
        // 
        moveSnap20CmMenuItem.Name = "moveSnap20CmMenuItem";
        moveSnap20CmMenuItem.Size = new Size(161, 34);
        moveSnap20CmMenuItem.Tag = 20;
        moveSnap20CmMenuItem.Text = "20 cm";
        moveSnap20CmMenuItem.Click += MoveSnapMenuItem_Click;
        // 
        // moveSnap50CmMenuItem
        // 
        moveSnap50CmMenuItem.Name = "moveSnap50CmMenuItem";
        moveSnap50CmMenuItem.Size = new Size(161, 34);
        moveSnap50CmMenuItem.Tag = 50;
        moveSnap50CmMenuItem.Text = "50 cm";
        moveSnap50CmMenuItem.Click += MoveSnapMenuItem_Click;
        // 
        // rotationSnapDropDownMenuItem
        // 
        rotationSnapDropDownMenuItem.DropDownItems.AddRange(new ToolStripItem[] { rotationSnap1DegreeMenuItem, rotationSnap5DegreeMenuItem, rotationSnap15DegreeMenuItem, rotationSnap45DegreeMenuItem });
        rotationSnapDropDownMenuItem.Name = "rotationSnapDropDownMenuItem";
        rotationSnapDropDownMenuItem.Size = new Size(182, 34);
        rotationSnapDropDownMenuItem.Text = "旋轉步距";
        // 
        // rotationSnap1DegreeMenuItem
        // 
        rotationSnap1DegreeMenuItem.Name = "rotationSnap1DegreeMenuItem";
        rotationSnap1DegreeMenuItem.Size = new Size(137, 34);
        rotationSnap1DegreeMenuItem.Tag = 1;
        rotationSnap1DegreeMenuItem.Text = "1°";
        rotationSnap1DegreeMenuItem.Click += RotationSnapMenuItem_Click;
        // 
        // rotationSnap5DegreeMenuItem
        // 
        rotationSnap5DegreeMenuItem.Checked = true;
        rotationSnap5DegreeMenuItem.CheckState = CheckState.Checked;
        rotationSnap5DegreeMenuItem.Name = "rotationSnap5DegreeMenuItem";
        rotationSnap5DegreeMenuItem.Size = new Size(137, 34);
        rotationSnap5DegreeMenuItem.Tag = 5;
        rotationSnap5DegreeMenuItem.Text = "5°";
        rotationSnap5DegreeMenuItem.Click += RotationSnapMenuItem_Click;
        // 
        // rotationSnap15DegreeMenuItem
        // 
        rotationSnap15DegreeMenuItem.Name = "rotationSnap15DegreeMenuItem";
        rotationSnap15DegreeMenuItem.Size = new Size(137, 34);
        rotationSnap15DegreeMenuItem.Tag = 15;
        rotationSnap15DegreeMenuItem.Text = "15°";
        rotationSnap15DegreeMenuItem.Click += RotationSnapMenuItem_Click;
        // 
        // rotationSnap45DegreeMenuItem
        // 
        rotationSnap45DegreeMenuItem.Name = "rotationSnap45DegreeMenuItem";
        rotationSnap45DegreeMenuItem.Size = new Size(137, 34);
        rotationSnap45DegreeMenuItem.Tag = 45;
        rotationSnap45DegreeMenuItem.Text = "45°";
        rotationSnap45DegreeMenuItem.Click += RotationSnapMenuItem_Click;
        // 
        // snapModeToolSeparator
        // 
        snapModeToolSeparator.Name = "snapModeToolSeparator";
        snapModeToolSeparator.Size = new Size(179, 6);
        // 
        // surfaceSnapMenuItem
        // 
        surfaceSnapMenuItem.Checked = true;
        surfaceSnapMenuItem.CheckOnClick = true;
        surfaceSnapMenuItem.CheckState = CheckState.Checked;
        surfaceSnapMenuItem.Name = "surfaceSnapMenuItem";
        surfaceSnapMenuItem.Size = new Size(182, 34);
        surfaceSnapMenuItem.Text = "表面吸附";
        surfaceSnapMenuItem.ToolTipText = "移動時靠近地板、桌面、層板或牆面即自動貼合";
        surfaceSnapMenuItem.CheckedChanged += SnapSettingsMenuItem_Changed;
        // 
        // gridSnapMenuItem
        // 
        gridSnapMenuItem.Checked = true;
        gridSnapMenuItem.CheckOnClick = true;
        gridSnapMenuItem.CheckState = CheckState.Checked;
        gridSnapMenuItem.Name = "gridSnapMenuItem";
        gridSnapMenuItem.Size = new Size(182, 34);
        gridSnapMenuItem.Text = "格點吸附";
        gridSnapMenuItem.CheckedChanged += SnapSettingsMenuItem_Changed;
        // 
        // rulerToolSeparator
        // 
        rulerToolSeparator.Name = "rulerToolSeparator";
        rulerToolSeparator.Size = new Size(6, 32);
        // 
        // rulerToolDropDownButton
        // 
        rulerToolDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { topRulerMenuItem, frontRulerMenuItem, sideRulerMenuItem });
        rulerToolDropDownButton.Name = "rulerToolDropDownButton";
        rulerToolDropDownButton.Size = new Size(82, 27);
        rulerToolDropDownButton.Text = "比例尺";
        rulerToolDropDownButton.ToolTipText = "設定正投影視圖的比例尺";
        // 
        // topRulerMenuItem
        // 
        topRulerMenuItem.Checked = true;
        topRulerMenuItem.CheckOnClick = true;
        topRulerMenuItem.CheckState = CheckState.Checked;
        topRulerMenuItem.Name = "topRulerMenuItem";
        topRulerMenuItem.Size = new Size(218, 34);
        topRulerMenuItem.Text = "上視圖比例尺";
        topRulerMenuItem.CheckedChanged += RulerMenuItem_CheckedChanged;
        // 
        // frontRulerMenuItem
        // 
        frontRulerMenuItem.Checked = true;
        frontRulerMenuItem.CheckOnClick = true;
        frontRulerMenuItem.CheckState = CheckState.Checked;
        frontRulerMenuItem.Name = "frontRulerMenuItem";
        frontRulerMenuItem.Size = new Size(218, 34);
        frontRulerMenuItem.Text = "前視圖比例尺";
        frontRulerMenuItem.CheckedChanged += RulerMenuItem_CheckedChanged;
        // 
        // sideRulerMenuItem
        // 
        sideRulerMenuItem.Checked = true;
        sideRulerMenuItem.CheckOnClick = true;
        sideRulerMenuItem.CheckState = CheckState.Checked;
        sideRulerMenuItem.Name = "sideRulerMenuItem";
        sideRulerMenuItem.Size = new Size(218, 34);
        sideRulerMenuItem.Text = "側視圖比例尺";
        sideRulerMenuItem.CheckedChanged += RulerMenuItem_CheckedChanged;
        // 
        // previewModeToolSeparator
        // 
        previewModeToolSeparator.Name = "previewModeToolSeparator";
        previewModeToolSeparator.Size = new Size(6, 32);
        // 
        // pbrPreviewToolButton
        // 
        pbrPreviewToolButton.CheckOnClick = true;
        pbrPreviewToolButton.Name = "pbrPreviewToolButton";
        pbrPreviewToolButton.Size = new Size(89, 27);
        pbrPreviewToolButton.Text = "PBR 預覽";
        pbrPreviewToolButton.ToolTipText = "切換右下角透視圖的設計模式與 MainForm PBR 預覽模式";
        pbrPreviewToolButton.CheckedChanged += PbrPreviewToolButton_CheckedChanged;
        // 
        // explanationPanel
        // 
        explanationPanel.BackColor = SystemColors.ControlLightLight;
        explanationPanel.BorderStyle = BorderStyle.FixedSingle;
        explanationPanel.Controls.Add(explanationLabel);
        explanationPanel.Dock = DockStyle.Top;
        explanationPanel.Location = new Point(0, 63);
        explanationPanel.Name = "explanationPanel";
        explanationPanel.Padding = new Padding(8, 0, 8, 0);
        explanationPanel.Size = new Size(1440, 32);
        explanationPanel.TabIndex = 2;
        // 
        // explanationLabel
        // 
        explanationLabel.AccessibleName = "說明與警示";
        explanationLabel.AutoEllipsis = true;
        explanationLabel.Dock = DockStyle.Fill;
        explanationLabel.ForeColor = SystemColors.ControlText;
        explanationLabel.Location = new Point(8, 0);
        explanationLabel.Name = "explanationLabel";
        explanationLabel.Size = new Size(1422, 30);
        explanationLabel.TabIndex = 0;
        explanationLabel.Text = "說明：請從右側模型庫選取模型；按「置放模型」後，在透視圖移動並按左鍵確認。";
        explanationLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // libraryPanel
        // 
        libraryPanel.Controls.Add(libraryTabControl);
        libraryPanel.Controls.Add(assetFilterTextBox);
        libraryPanel.Dock = DockStyle.Fill;
        libraryPanel.Location = new Point(3, 3);
        libraryPanel.Name = "libraryPanel";
        libraryPanel.Padding = new Padding(6);
        libraryPanel.Size = new Size(248, 755);
        libraryPanel.TabIndex = 0;
        // 
        // libraryTabControl
        // 
        libraryTabControl.Controls.Add(modelsTabPage);
        libraryTabControl.Controls.Add(texturesTabPage);
        libraryTabControl.Controls.Add(materialsTabPage);
        libraryTabControl.Controls.Add(assetLibraryTabPage);
        libraryTabControl.Dock = DockStyle.Fill;
        libraryTabControl.Location = new Point(6, 36);
        libraryTabControl.Name = "libraryTabControl";
        libraryTabControl.SelectedIndex = 0;
        libraryTabControl.Size = new Size(236, 713);
        libraryTabControl.TabIndex = 1;
        libraryTabControl.SelectedIndexChanged += LibraryTabControl_SelectedIndexChanged;
        // 
        // modelsTabPage
        // 
        modelsTabPage.Controls.Add(modelLibrarySplitContainer);
        modelsTabPage.Location = new Point(4, 32);
        modelsTabPage.Name = "modelsTabPage";
        modelsTabPage.Padding = new Padding(3);
        modelsTabPage.Size = new Size(228, 677);
        modelsTabPage.TabIndex = 0;
        modelsTabPage.Text = "模型";
        modelsTabPage.UseVisualStyleBackColor = true;
        // 
        // modelLibrarySplitContainer
        // 
        modelLibrarySplitContainer.Dock = DockStyle.Fill;
        modelLibrarySplitContainer.FixedPanel = FixedPanel.Panel1;
        modelLibrarySplitContainer.Location = new Point(3, 3);
        modelLibrarySplitContainer.Name = "modelLibrarySplitContainer";
        modelLibrarySplitContainer.Orientation = Orientation.Horizontal;
        // 
        // modelLibrarySplitContainer.Panel1
        // 
        modelLibrarySplitContainer.Panel1.Controls.Add(modelsTreeView);
        modelLibrarySplitContainer.Panel1MinSize = 120;
        // 
        // modelLibrarySplitContainer.Panel2
        // 
        modelLibrarySplitContainer.Panel2.Controls.Add(modelCommandPanel);
        modelLibrarySplitContainer.Panel2MinSize = 220;
        modelLibrarySplitContainer.Size = new Size(222, 671);
        modelLibrarySplitContainer.SplitterDistance = 300;
        modelLibrarySplitContainer.TabIndex = 0;
        modelLibrarySplitContainer.SplitterMoved += UiSplitterMoved;
        // 
        // modelsTreeView
        // 
        modelsTreeView.Dock = DockStyle.Fill;
        modelsTreeView.HideSelection = false;
        modelsTreeView.Location = new Point(0, 0);
        modelsTreeView.Name = "modelsTreeView";
        treeNode1.Name = "";
        treeNode1.Text = "平面";
        treeNode2.Name = "";
        treeNode2.Text = "球體";
        treeNode3.Name = "";
        treeNode3.Text = "長方體";
        treeNode4.Name = "";
        treeNode4.Text = "圓柱體";
        treeNode5.Name = "";
        treeNode5.Text = "半圓柱體";
        treeNode6.Name = "";
        treeNode6.Text = "圓錐";
        treeNode7.Name = "";
        treeNode7.Text = "多邊錐體";
        treeNode8.Name = "";
        treeNode8.Text = "基本模型";
        treeNode9.Name = "";
        treeNode9.Text = "房間";
        treeNode10.Name = "";
        treeNode10.Text = "牆體";
        treeNode11.Name = "";
        treeNode11.Text = "門窗牆體";
        treeNode12.Name = "";
        treeNode12.Text = "地板";
        treeNode13.Name = "";
        treeNode13.Text = "天花板";
        treeNode14.Name = "";
        treeNode14.Text = "門";
        treeNode15.Name = "";
        treeNode15.Text = "窗戶";
        treeNode16.Name = "";
        treeNode16.Text = "樑";
        treeNode17.Name = "";
        treeNode17.Text = "柱";
        treeNode18.Name = "";
        treeNode18.Text = "樓梯";
        treeNode19.Name = "";
        treeNode19.Text = "建築構件";
        treeNode20.Name = "";
        treeNode20.Text = "桌";
        treeNode21.Name = "";
        treeNode21.Text = "椅";
        treeNode22.Name = "";
        treeNode22.Text = "沙發";
        treeNode23.Name = "";
        treeNode23.Text = "床";
        treeNode24.Name = "";
        treeNode24.Text = "櫃體";
        treeNode25.Name = "";
        treeNode25.Text = "廚房設備";
        treeNode26.Name = "";
        treeNode26.Text = "衛浴設備";
        treeNode27.Name = "";
        treeNode27.Text = "家電";
        treeNode28.Name = "";
        treeNode28.Text = "家具與設備";
        treeNode29.Name = "";
        treeNode29.Text = "檯燈";
        treeNode30.Name = "";
        treeNode30.Text = "立燈";
        treeNode31.Name = "";
        treeNode31.Text = "吊燈";
        treeNode32.Name = "";
        treeNode32.Text = "吸頂燈";
        treeNode33.Name = "";
        treeNode33.Text = "壁燈";
        treeNode34.Name = "";
        treeNode34.Text = "窗簾";
        treeNode35.Name = "";
        treeNode35.Text = "地毯";
        treeNode36.Name = "";
        treeNode36.Text = "植栽";
        treeNode37.Name = "";
        treeNode37.Text = "裝飾品";
        treeNode38.Name = "";
        treeNode38.Text = "燈具與軟裝";
        modelsTreeView.Nodes.AddRange(new TreeNode[] { treeNode8, treeNode19, treeNode28, treeNode38 });
        modelsTreeView.Size = new Size(222, 300);
        modelsTreeView.TabIndex = 0;
        modelsTreeView.AfterSelect += ModelsTreeView_AfterSelect;
        // 
        // modelCommandPanel
        // 
        modelCommandPanel.Controls.Add(parameterCreationTableLayoutPanel);
        modelCommandPanel.Dock = DockStyle.Fill;
        modelCommandPanel.Location = new Point(0, 0);
        modelCommandPanel.Name = "modelCommandPanel";
        modelCommandPanel.Padding = new Padding(4);
        modelCommandPanel.Size = new Size(222, 367);
        modelCommandPanel.TabIndex = 2;
        // 
        // parameterCreationTableLayoutPanel
        // 
        parameterCreationTableLayoutPanel.ColumnCount = 1;
        parameterCreationTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        parameterCreationTableLayoutPanel.Controls.Add(parameterCreationLabel, 0, 0);
        parameterCreationTableLayoutPanel.Controls.Add(parameterPropertyGrid, 0, 1);
        parameterCreationTableLayoutPanel.Controls.Add(createParametricModelButton, 0, 2);
        parameterCreationTableLayoutPanel.Controls.Add(createDefaultInteriorButton, 0, 3);
        parameterCreationTableLayoutPanel.Dock = DockStyle.Fill;
        parameterCreationTableLayoutPanel.Location = new Point(4, 4);
        parameterCreationTableLayoutPanel.Name = "parameterCreationTableLayoutPanel";
        parameterCreationTableLayoutPanel.RowCount = 4;
        parameterCreationTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        parameterCreationTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        parameterCreationTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        parameterCreationTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        parameterCreationTableLayoutPanel.Size = new Size(214, 359);
        parameterCreationTableLayoutPanel.TabIndex = 0;
        // 
        // parameterCreationLabel
        // 
        parameterCreationLabel.Dock = DockStyle.Fill;
        parameterCreationLabel.Location = new Point(3, 0);
        parameterCreationLabel.Name = "parameterCreationLabel";
        parameterCreationLabel.Size = new Size(208, 28);
        parameterCreationLabel.TabIndex = 0;
        parameterCreationLabel.Text = "參數建立";
        parameterCreationLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // parameterPropertyGrid
        // 
        parameterPropertyGrid.Dock = DockStyle.Fill;
        parameterPropertyGrid.HelpVisible = false;
        parameterPropertyGrid.Location = new Point(3, 31);
        parameterPropertyGrid.Name = "parameterPropertyGrid";
        parameterPropertyGrid.PropertySort = PropertySort.Categorized;
        parameterPropertyGrid.Size = new Size(208, 249);
        parameterPropertyGrid.TabIndex = 0;
        parameterPropertyGrid.ToolbarVisible = false;
        parameterPropertyGrid.PropertyValueChanged += ParameterPropertyGrid_PropertyValueChanged;
        // 
        // createParametricModelButton
        // 
        createParametricModelButton.Dock = DockStyle.Fill;
        createParametricModelButton.Enabled = false;
        createParametricModelButton.Location = new Point(3, 286);
        createParametricModelButton.Name = "createParametricModelButton";
        createParametricModelButton.Size = new Size(208, 32);
        createParametricModelButton.TabIndex = 0;
        createParametricModelButton.Text = "建立參數模型";
        createParametricModelButton.UseVisualStyleBackColor = true;
        createParametricModelButton.Click += CreateParametricModelButton_Click;
        // 
        // createDefaultInteriorButton
        // 
        createDefaultInteriorButton.Dock = DockStyle.Fill;
        createDefaultInteriorButton.Location = new Point(3, 324);
        createDefaultInteriorButton.Name = "createDefaultInteriorButton";
        createDefaultInteriorButton.Size = new Size(208, 32);
        createDefaultInteriorButton.TabIndex = 1;
        createDefaultInteriorButton.Text = "產生預設室內模型";
        createDefaultInteriorButton.UseVisualStyleBackColor = true;
        createDefaultInteriorButton.Click += CreateDefaultInteriorButton_Click;
        // 
        // texturesTabPage
        // 
        texturesTabPage.Controls.Add(textureLibraryTableLayoutPanel);
        texturesTabPage.Location = new Point(4, 32);
        texturesTabPage.Name = "texturesTabPage";
        texturesTabPage.Padding = new Padding(3);
        texturesTabPage.Size = new Size(180, 22);
        texturesTabPage.TabIndex = 1;
        texturesTabPage.Text = "貼圖";
        texturesTabPage.UseVisualStyleBackColor = true;
        // 
        // textureLibraryTableLayoutPanel
        // 
        textureLibraryTableLayoutPanel.ColumnCount = 1;
        textureLibraryTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        textureLibraryTableLayoutPanel.Controls.Add(texturesListBox, 0, 0);
        textureLibraryTableLayoutPanel.Controls.Add(textureSettingsPropertyGrid, 0, 1);
        textureLibraryTableLayoutPanel.Controls.Add(applyTextureButton, 0, 2);
        textureLibraryTableLayoutPanel.Dock = DockStyle.Fill;
        textureLibraryTableLayoutPanel.Location = new Point(3, 3);
        textureLibraryTableLayoutPanel.Name = "textureLibraryTableLayoutPanel";
        textureLibraryTableLayoutPanel.RowCount = 3;
        textureLibraryTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
        textureLibraryTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
        textureLibraryTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        textureLibraryTableLayoutPanel.Size = new Size(174, 16);
        textureLibraryTableLayoutPanel.TabIndex = 0;
        // 
        // texturesListBox
        // 
        texturesListBox.Dock = DockStyle.Fill;
        texturesListBox.FormattingEnabled = true;
        texturesListBox.Location = new Point(3, 3);
        texturesListBox.Name = "texturesListBox";
        texturesListBox.Size = new Size(168, 1);
        texturesListBox.TabIndex = 0;
        texturesListBox.SelectedIndexChanged += TexturesListBox_SelectedIndexChanged;
        // 
        // textureSettingsPropertyGrid
        // 
        textureSettingsPropertyGrid.Dock = DockStyle.Fill;
        textureSettingsPropertyGrid.HelpVisible = false;
        textureSettingsPropertyGrid.Location = new Point(3, -6);
        textureSettingsPropertyGrid.Name = "textureSettingsPropertyGrid";
        textureSettingsPropertyGrid.PropertySort = PropertySort.Categorized;
        textureSettingsPropertyGrid.Size = new Size(168, 1);
        textureSettingsPropertyGrid.TabIndex = 1;
        textureSettingsPropertyGrid.ToolbarVisible = false;
        textureSettingsPropertyGrid.PropertyValueChanged += TextureSettingsPropertyGrid_PropertyValueChanged;
        // 
        // applyTextureButton
        // 
        applyTextureButton.Dock = DockStyle.Fill;
        applyTextureButton.Enabled = false;
        applyTextureButton.Location = new Point(3, -18);
        applyTextureButton.Name = "applyTextureButton";
        applyTextureButton.Size = new Size(168, 32);
        applyTextureButton.TabIndex = 2;
        applyTextureButton.Text = "套用貼圖至選取模型／Mesh";
        applyTextureButton.UseVisualStyleBackColor = true;
        applyTextureButton.Click += ApplyTextureButton_Click;
        // 
        // materialsTabPage
        // 
        materialsTabPage.Controls.Add(materialLibraryTableLayoutPanel);
        materialsTabPage.Location = new Point(4, 32);
        materialsTabPage.Name = "materialsTabPage";
        materialsTabPage.Padding = new Padding(3);
        materialsTabPage.Size = new Size(180, 22);
        materialsTabPage.TabIndex = 2;
        materialsTabPage.Text = "材質";
        materialsTabPage.UseVisualStyleBackColor = true;
        // 
        // materialLibraryTableLayoutPanel
        // 
        materialLibraryTableLayoutPanel.ColumnCount = 1;
        materialLibraryTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        materialLibraryTableLayoutPanel.Controls.Add(materialsListBox, 0, 0);
        materialLibraryTableLayoutPanel.Controls.Add(materialPresetPropertyGrid, 0, 1);
        materialLibraryTableLayoutPanel.Controls.Add(applyMaterialButton, 0, 2);
        materialLibraryTableLayoutPanel.Dock = DockStyle.Fill;
        materialLibraryTableLayoutPanel.Location = new Point(3, 3);
        materialLibraryTableLayoutPanel.Name = "materialLibraryTableLayoutPanel";
        materialLibraryTableLayoutPanel.RowCount = 3;
        materialLibraryTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
        materialLibraryTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        materialLibraryTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        materialLibraryTableLayoutPanel.Size = new Size(174, 16);
        materialLibraryTableLayoutPanel.TabIndex = 0;
        // 
        // materialsListBox
        // 
        materialsListBox.Dock = DockStyle.Fill;
        materialsListBox.FormattingEnabled = true;
        materialsListBox.Location = new Point(3, 3);
        materialsListBox.Name = "materialsListBox";
        materialsListBox.Size = new Size(168, 1);
        materialsListBox.TabIndex = 0;
        materialsListBox.SelectedIndexChanged += MaterialsListBox_SelectedIndexChanged;
        // 
        // materialPresetPropertyGrid
        // 
        materialPresetPropertyGrid.Dock = DockStyle.Fill;
        materialPresetPropertyGrid.HelpVisible = false;
        materialPresetPropertyGrid.Location = new Point(3, -7);
        materialPresetPropertyGrid.Name = "materialPresetPropertyGrid";
        materialPresetPropertyGrid.PropertySort = PropertySort.Categorized;
        materialPresetPropertyGrid.Size = new Size(168, 1);
        materialPresetPropertyGrid.TabIndex = 1;
        materialPresetPropertyGrid.ToolbarVisible = false;
        // 
        // applyMaterialButton
        // 
        applyMaterialButton.Dock = DockStyle.Fill;
        applyMaterialButton.Enabled = false;
        applyMaterialButton.Location = new Point(3, -18);
        applyMaterialButton.Name = "applyMaterialButton";
        applyMaterialButton.Size = new Size(168, 32);
        applyMaterialButton.TabIndex = 2;
        applyMaterialButton.Text = "套用至選取 Mesh";
        applyMaterialButton.UseVisualStyleBackColor = true;
        applyMaterialButton.Click += ApplyMaterialButton_Click;
        // 
        // assetLibraryTabPage
        // 
        assetLibraryTabPage.Controls.Add(assetTabControl);
        assetLibraryTabPage.Controls.Add(assetLibraryCommandPanel);
        assetLibraryTabPage.Location = new Point(4, 32);
        assetLibraryTabPage.Name = "assetLibraryTabPage";
        assetLibraryTabPage.Padding = new Padding(3);
        assetLibraryTabPage.Size = new Size(180, 22);
        assetLibraryTabPage.TabIndex = 3;
        assetLibraryTabPage.Text = "家具設備";
        assetLibraryTabPage.UseVisualStyleBackColor = true;
        // 
        // assetTabControl
        // 
        assetTabControl.Controls.Add(furnitureTabPage);
        assetTabControl.Controls.Add(applianceTabPage);
        assetTabControl.Controls.Add(lightingAssetTabPage);
        assetTabControl.Controls.Add(doorsWindowsTabPage);
        assetTabControl.Controls.Add(kitchenTabPage);
        assetTabControl.Controls.Add(bathroomTabPage);
        assetTabControl.Controls.Add(storageTabPage);
        assetTabControl.Controls.Add(decorTabPage);
        assetTabControl.Controls.Add(plantsTabPage);
        assetTabControl.Controls.Add(otherAssetsTabPage);
        assetTabControl.Controls.Add(customAssetsTabPage);
        assetTabControl.Controls.Add(onlineAssetsTabPage);
        assetTabControl.Dock = DockStyle.Fill;
        assetTabControl.Location = new Point(3, 204);
        assetTabControl.Multiline = true;
        assetTabControl.Name = "assetTabControl";
        assetTabControl.SelectedIndex = 0;
        assetTabControl.Size = new Size(174, 0);
        assetTabControl.TabIndex = 0;
        assetTabControl.SelectedIndexChanged += AssetTabControl_SelectedIndexChanged;
        // 
        // furnitureTabPage
        // 
        furnitureTabPage.Controls.Add(furnitureAssetControl);
        furnitureTabPage.Location = new Point(4, 144);
        furnitureTabPage.Name = "furnitureTabPage";
        furnitureTabPage.Padding = new Padding(3);
        furnitureTabPage.Size = new Size(166, 0);
        furnitureTabPage.TabIndex = 0;
        furnitureTabPage.Text = "家具";
        furnitureTabPage.UseVisualStyleBackColor = true;
        // 
        // furnitureAssetControl
        // 
        furnitureAssetControl.Dock = DockStyle.Fill;
        furnitureAssetControl.Location = new Point(3, 3);
        furnitureAssetControl.Name = "furnitureAssetControl";
        furnitureAssetControl.Size = new Size(160, 0);
        furnitureAssetControl.TabIndex = 0;
        furnitureAssetControl.AssetSelected += AssetCategoryControl_AssetSelected;
        furnitureAssetControl.PlaceRequested += AssetCategoryControl_PlaceRequested;
        furnitureAssetControl.EditRequested += AssetCategoryControl_EditRequested;
        furnitureAssetControl.DeleteRequested += AssetCategoryControl_DeleteRequested;
        furnitureAssetControl.SearchOnlineRequested += AssetCategoryControl_SearchOnlineRequested;
        furnitureAssetControl.PlacementSplitterMoved += AssetCategoryControl_PlacementSplitterMoved;
        furnitureAssetControl.PreviewSplitterMoved += AssetCategoryControl_PreviewSplitterMoved;
        // 
        // applianceTabPage
        // 
        applianceTabPage.Controls.Add(applianceAssetControl);
        applianceTabPage.Location = new Point(4, 32);
        applianceTabPage.Name = "applianceTabPage";
        applianceTabPage.Padding = new Padding(3);
        applianceTabPage.Size = new Size(192, 64);
        applianceTabPage.TabIndex = 1;
        applianceTabPage.Text = "電器";
        applianceTabPage.UseVisualStyleBackColor = true;
        // 
        // applianceAssetControl
        // 
        applianceAssetControl.Dock = DockStyle.Fill;
        applianceAssetControl.Location = new Point(3, 3);
        applianceAssetControl.Name = "applianceAssetControl";
        applianceAssetControl.Size = new Size(186, 58);
        applianceAssetControl.TabIndex = 0;
        applianceAssetControl.AssetSelected += AssetCategoryControl_AssetSelected;
        applianceAssetControl.PlaceRequested += AssetCategoryControl_PlaceRequested;
        applianceAssetControl.EditRequested += AssetCategoryControl_EditRequested;
        applianceAssetControl.DeleteRequested += AssetCategoryControl_DeleteRequested;
        applianceAssetControl.SearchOnlineRequested += AssetCategoryControl_SearchOnlineRequested;
        applianceAssetControl.PlacementSplitterMoved += AssetCategoryControl_PlacementSplitterMoved;
        applianceAssetControl.PreviewSplitterMoved += AssetCategoryControl_PreviewSplitterMoved;
        // 
        // lightingAssetTabPage
        // 
        lightingAssetTabPage.Controls.Add(lightingAssetControl);
        lightingAssetTabPage.Location = new Point(4, 32);
        lightingAssetTabPage.Name = "lightingAssetTabPage";
        lightingAssetTabPage.Padding = new Padding(3);
        lightingAssetTabPage.Size = new Size(192, 64);
        lightingAssetTabPage.TabIndex = 2;
        lightingAssetTabPage.Text = "燈具";
        lightingAssetTabPage.UseVisualStyleBackColor = true;
        // 
        // lightingAssetControl
        // 
        lightingAssetControl.Dock = DockStyle.Fill;
        lightingAssetControl.Location = new Point(3, 3);
        lightingAssetControl.Name = "lightingAssetControl";
        lightingAssetControl.Size = new Size(186, 58);
        lightingAssetControl.TabIndex = 0;
        lightingAssetControl.AssetSelected += AssetCategoryControl_AssetSelected;
        lightingAssetControl.PlaceRequested += AssetCategoryControl_PlaceRequested;
        lightingAssetControl.EditRequested += AssetCategoryControl_EditRequested;
        lightingAssetControl.DeleteRequested += AssetCategoryControl_DeleteRequested;
        lightingAssetControl.SearchOnlineRequested += AssetCategoryControl_SearchOnlineRequested;
        lightingAssetControl.PlacementSplitterMoved += AssetCategoryControl_PlacementSplitterMoved;
        lightingAssetControl.PreviewSplitterMoved += AssetCategoryControl_PreviewSplitterMoved;
        // 
        // doorsWindowsTabPage
        // 
        doorsWindowsTabPage.Controls.Add(doorsWindowsAssetControl);
        doorsWindowsTabPage.Location = new Point(4, 60);
        doorsWindowsTabPage.Name = "doorsWindowsTabPage";
        doorsWindowsTabPage.Padding = new Padding(3);
        doorsWindowsTabPage.Size = new Size(192, 36);
        doorsWindowsTabPage.TabIndex = 3;
        doorsWindowsTabPage.Text = "門窗";
        doorsWindowsTabPage.UseVisualStyleBackColor = true;
        // 
        // doorsWindowsAssetControl
        // 
        doorsWindowsAssetControl.Dock = DockStyle.Fill;
        doorsWindowsAssetControl.Location = new Point(3, 3);
        doorsWindowsAssetControl.Name = "doorsWindowsAssetControl";
        doorsWindowsAssetControl.Size = new Size(186, 30);
        doorsWindowsAssetControl.TabIndex = 0;
        doorsWindowsAssetControl.AssetSelected += AssetCategoryControl_AssetSelected;
        doorsWindowsAssetControl.PlaceRequested += AssetCategoryControl_PlaceRequested;
        doorsWindowsAssetControl.EditRequested += AssetCategoryControl_EditRequested;
        doorsWindowsAssetControl.DeleteRequested += AssetCategoryControl_DeleteRequested;
        doorsWindowsAssetControl.SearchOnlineRequested += AssetCategoryControl_SearchOnlineRequested;
        doorsWindowsAssetControl.PlacementSplitterMoved += AssetCategoryControl_PlacementSplitterMoved;
        doorsWindowsAssetControl.PreviewSplitterMoved += AssetCategoryControl_PreviewSplitterMoved;
        // 
        // kitchenTabPage
        // 
        kitchenTabPage.Controls.Add(kitchenAssetControl);
        kitchenTabPage.Location = new Point(4, 60);
        kitchenTabPage.Name = "kitchenTabPage";
        kitchenTabPage.Padding = new Padding(3);
        kitchenTabPage.Size = new Size(192, 36);
        kitchenTabPage.TabIndex = 4;
        kitchenTabPage.Text = "廚房";
        kitchenTabPage.UseVisualStyleBackColor = true;
        // 
        // kitchenAssetControl
        // 
        kitchenAssetControl.Dock = DockStyle.Fill;
        kitchenAssetControl.Location = new Point(3, 3);
        kitchenAssetControl.Name = "kitchenAssetControl";
        kitchenAssetControl.Size = new Size(186, 30);
        kitchenAssetControl.TabIndex = 0;
        kitchenAssetControl.AssetSelected += AssetCategoryControl_AssetSelected;
        kitchenAssetControl.PlaceRequested += AssetCategoryControl_PlaceRequested;
        kitchenAssetControl.EditRequested += AssetCategoryControl_EditRequested;
        kitchenAssetControl.DeleteRequested += AssetCategoryControl_DeleteRequested;
        kitchenAssetControl.SearchOnlineRequested += AssetCategoryControl_SearchOnlineRequested;
        kitchenAssetControl.PlacementSplitterMoved += AssetCategoryControl_PlacementSplitterMoved;
        kitchenAssetControl.PreviewSplitterMoved += AssetCategoryControl_PreviewSplitterMoved;
        // 
        // bathroomTabPage
        // 
        bathroomTabPage.Controls.Add(bathroomAssetControl);
        bathroomTabPage.Location = new Point(4, 60);
        bathroomTabPage.Name = "bathroomTabPage";
        bathroomTabPage.Padding = new Padding(3);
        bathroomTabPage.Size = new Size(192, 36);
        bathroomTabPage.TabIndex = 5;
        bathroomTabPage.Text = "衛浴";
        bathroomTabPage.UseVisualStyleBackColor = true;
        // 
        // bathroomAssetControl
        // 
        bathroomAssetControl.Dock = DockStyle.Fill;
        bathroomAssetControl.Location = new Point(3, 3);
        bathroomAssetControl.Name = "bathroomAssetControl";
        bathroomAssetControl.Size = new Size(186, 30);
        bathroomAssetControl.TabIndex = 0;
        bathroomAssetControl.AssetSelected += AssetCategoryControl_AssetSelected;
        bathroomAssetControl.PlaceRequested += AssetCategoryControl_PlaceRequested;
        bathroomAssetControl.EditRequested += AssetCategoryControl_EditRequested;
        bathroomAssetControl.DeleteRequested += AssetCategoryControl_DeleteRequested;
        bathroomAssetControl.SearchOnlineRequested += AssetCategoryControl_SearchOnlineRequested;
        bathroomAssetControl.PlacementSplitterMoved += AssetCategoryControl_PlacementSplitterMoved;
        bathroomAssetControl.PreviewSplitterMoved += AssetCategoryControl_PreviewSplitterMoved;
        // 
        // storageTabPage
        // 
        storageTabPage.Controls.Add(storageAssetControl);
        storageTabPage.Location = new Point(4, 88);
        storageTabPage.Name = "storageTabPage";
        storageTabPage.Padding = new Padding(3);
        storageTabPage.Size = new Size(192, 8);
        storageTabPage.TabIndex = 6;
        storageTabPage.Text = "收納";
        storageTabPage.UseVisualStyleBackColor = true;
        // 
        // storageAssetControl
        // 
        storageAssetControl.Dock = DockStyle.Fill;
        storageAssetControl.Location = new Point(3, 3);
        storageAssetControl.Name = "storageAssetControl";
        storageAssetControl.Size = new Size(186, 2);
        storageAssetControl.TabIndex = 0;
        storageAssetControl.AssetSelected += AssetCategoryControl_AssetSelected;
        storageAssetControl.PlaceRequested += AssetCategoryControl_PlaceRequested;
        storageAssetControl.EditRequested += AssetCategoryControl_EditRequested;
        storageAssetControl.DeleteRequested += AssetCategoryControl_DeleteRequested;
        storageAssetControl.SearchOnlineRequested += AssetCategoryControl_SearchOnlineRequested;
        storageAssetControl.PlacementSplitterMoved += AssetCategoryControl_PlacementSplitterMoved;
        storageAssetControl.PreviewSplitterMoved += AssetCategoryControl_PreviewSplitterMoved;
        // 
        // decorTabPage
        // 
        decorTabPage.Controls.Add(decorAssetControl);
        decorTabPage.Location = new Point(4, 88);
        decorTabPage.Name = "decorTabPage";
        decorTabPage.Padding = new Padding(3);
        decorTabPage.Size = new Size(192, 8);
        decorTabPage.TabIndex = 7;
        decorTabPage.Text = "軟裝";
        decorTabPage.UseVisualStyleBackColor = true;
        // 
        // decorAssetControl
        // 
        decorAssetControl.Dock = DockStyle.Fill;
        decorAssetControl.Location = new Point(3, 3);
        decorAssetControl.Name = "decorAssetControl";
        decorAssetControl.Size = new Size(186, 2);
        decorAssetControl.TabIndex = 0;
        decorAssetControl.AssetSelected += AssetCategoryControl_AssetSelected;
        decorAssetControl.PlaceRequested += AssetCategoryControl_PlaceRequested;
        decorAssetControl.EditRequested += AssetCategoryControl_EditRequested;
        decorAssetControl.DeleteRequested += AssetCategoryControl_DeleteRequested;
        decorAssetControl.SearchOnlineRequested += AssetCategoryControl_SearchOnlineRequested;
        decorAssetControl.PlacementSplitterMoved += AssetCategoryControl_PlacementSplitterMoved;
        decorAssetControl.PreviewSplitterMoved += AssetCategoryControl_PreviewSplitterMoved;
        // 
        // plantsTabPage
        // 
        plantsTabPage.Controls.Add(plantsAssetControl);
        plantsTabPage.Location = new Point(4, 88);
        plantsTabPage.Name = "plantsTabPage";
        plantsTabPage.Padding = new Padding(3);
        plantsTabPage.Size = new Size(192, 8);
        plantsTabPage.TabIndex = 8;
        plantsTabPage.Text = "植栽";
        plantsTabPage.UseVisualStyleBackColor = true;
        // 
        // plantsAssetControl
        // 
        plantsAssetControl.Dock = DockStyle.Fill;
        plantsAssetControl.Location = new Point(3, 3);
        plantsAssetControl.Name = "plantsAssetControl";
        plantsAssetControl.Size = new Size(186, 2);
        plantsAssetControl.TabIndex = 0;
        plantsAssetControl.AssetSelected += AssetCategoryControl_AssetSelected;
        plantsAssetControl.PlaceRequested += AssetCategoryControl_PlaceRequested;
        plantsAssetControl.EditRequested += AssetCategoryControl_EditRequested;
        plantsAssetControl.DeleteRequested += AssetCategoryControl_DeleteRequested;
        plantsAssetControl.SearchOnlineRequested += AssetCategoryControl_SearchOnlineRequested;
        plantsAssetControl.PlacementSplitterMoved += AssetCategoryControl_PlacementSplitterMoved;
        plantsAssetControl.PreviewSplitterMoved += AssetCategoryControl_PreviewSplitterMoved;
        // 
        // otherAssetsTabPage
        // 
        otherAssetsTabPage.Controls.Add(otherAssetsControl);
        otherAssetsTabPage.Location = new Point(4, 116);
        otherAssetsTabPage.Name = "otherAssetsTabPage";
        otherAssetsTabPage.Padding = new Padding(3);
        otherAssetsTabPage.Size = new Size(192, 0);
        otherAssetsTabPage.TabIndex = 9;
        otherAssetsTabPage.Text = "其他";
        otherAssetsTabPage.UseVisualStyleBackColor = true;
        // 
        // otherAssetsControl
        // 
        otherAssetsControl.Dock = DockStyle.Fill;
        otherAssetsControl.Location = new Point(3, 3);
        otherAssetsControl.Name = "otherAssetsControl";
        otherAssetsControl.Size = new Size(186, 0);
        otherAssetsControl.TabIndex = 0;
        otherAssetsControl.AssetSelected += AssetCategoryControl_AssetSelected;
        otherAssetsControl.PlaceRequested += AssetCategoryControl_PlaceRequested;
        otherAssetsControl.EditRequested += AssetCategoryControl_EditRequested;
        otherAssetsControl.DeleteRequested += AssetCategoryControl_DeleteRequested;
        otherAssetsControl.SearchOnlineRequested += AssetCategoryControl_SearchOnlineRequested;
        otherAssetsControl.PlacementSplitterMoved += AssetCategoryControl_PlacementSplitterMoved;
        otherAssetsControl.PreviewSplitterMoved += AssetCategoryControl_PreviewSplitterMoved;
        // 
        // customAssetsTabPage
        // 
        customAssetsTabPage.Controls.Add(customAssetsControl);
        customAssetsTabPage.Location = new Point(4, 116);
        customAssetsTabPage.Name = "customAssetsTabPage";
        customAssetsTabPage.Padding = new Padding(3);
        customAssetsTabPage.Size = new Size(192, 0);
        customAssetsTabPage.TabIndex = 10;
        customAssetsTabPage.Text = "使用者自訂";
        customAssetsTabPage.UseVisualStyleBackColor = true;
        // 
        // customAssetsControl
        // 
        customAssetsControl.Dock = DockStyle.Fill;
        customAssetsControl.Location = new Point(3, 3);
        customAssetsControl.Name = "customAssetsControl";
        customAssetsControl.Size = new Size(186, 0);
        customAssetsControl.TabIndex = 0;
        customAssetsControl.AssetSelected += AssetCategoryControl_AssetSelected;
        customAssetsControl.PlaceRequested += AssetCategoryControl_PlaceRequested;
        customAssetsControl.EditRequested += AssetCategoryControl_EditRequested;
        customAssetsControl.DeleteRequested += AssetCategoryControl_DeleteRequested;
        customAssetsControl.SearchOnlineRequested += AssetCategoryControl_SearchOnlineRequested;
        customAssetsControl.PlacementSplitterMoved += AssetCategoryControl_PlacementSplitterMoved;
        customAssetsControl.PreviewSplitterMoved += AssetCategoryControl_PreviewSplitterMoved;
        // 
        // onlineAssetsTabPage
        // 
        onlineAssetsTabPage.Controls.Add(polyHavenAssetBrowserControl);
        onlineAssetsTabPage.Location = new Point(4, 144);
        onlineAssetsTabPage.Name = "onlineAssetsTabPage";
        onlineAssetsTabPage.Padding = new Padding(3);
        onlineAssetsTabPage.Size = new Size(192, 0);
        onlineAssetsTabPage.TabIndex = 11;
        onlineAssetsTabPage.Text = "線上模型";
        onlineAssetsTabPage.UseVisualStyleBackColor = true;
        onlineAssetsTabPage.Enter += OnlineAssetsTabPage_Enter;
        // 
        // polyHavenAssetBrowserControl
        // 
        polyHavenAssetBrowserControl.Dock = DockStyle.Fill;
        polyHavenAssetBrowserControl.Location = new Point(3, 3);
        polyHavenAssetBrowserControl.Name = "polyHavenAssetBrowserControl";
        polyHavenAssetBrowserControl.Size = new Size(186, 0);
        polyHavenAssetBrowserControl.TabIndex = 0;
        polyHavenAssetBrowserControl.DownloadRequested += PolyHavenAssetBrowserControl_DownloadRequested;
        polyHavenAssetBrowserControl.PreviewSplitterMoved += PolyHavenAssetBrowserControl_PreviewSplitterMoved;
        // 
        // assetLibraryCommandPanel
        // 
        assetLibraryCommandPanel.AutoSize = true;
        assetLibraryCommandPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        assetLibraryCommandPanel.Controls.Add(importAssetButton);
        assetLibraryCommandPanel.Controls.Add(refreshAssetLibraryButton);
        assetLibraryCommandPanel.Controls.Add(analyzeAssetLibraryButton);
        assetLibraryCommandPanel.Controls.Add(deleteLibraryAssetButton);
        assetLibraryCommandPanel.Controls.Add(addFavoriteLibraryAssetButton);
        assetLibraryCommandPanel.Dock = DockStyle.Top;
        assetLibraryCommandPanel.Location = new Point(3, 3);
        assetLibraryCommandPanel.Name = "assetLibraryCommandPanel";
        assetLibraryCommandPanel.Padding = new Padding(3);
        assetLibraryCommandPanel.Size = new Size(174, 201);
        assetLibraryCommandPanel.TabIndex = 1;
        // 
        // importAssetButton
        // 
        importAssetButton.AutoSize = true;
        importAssetButton.Location = new Point(6, 6);
        importAssetButton.Name = "importAssetButton";
        importAssetButton.Size = new Size(92, 33);
        importAssetButton.TabIndex = 0;
        importAssetButton.Text = "加入模型";
        importAssetButton.UseVisualStyleBackColor = true;
        importAssetButton.Click += ImportAssetButton_Click;
        // 
        // refreshAssetLibraryButton
        // 
        refreshAssetLibraryButton.AutoSize = true;
        refreshAssetLibraryButton.Location = new Point(6, 45);
        refreshAssetLibraryButton.Name = "refreshAssetLibraryButton";
        refreshAssetLibraryButton.Size = new Size(92, 33);
        refreshAssetLibraryButton.TabIndex = 1;
        refreshAssetLibraryButton.Text = "重新整理";
        refreshAssetLibraryButton.UseVisualStyleBackColor = true;
        refreshAssetLibraryButton.Click += RefreshAssetLibraryButton_Click;
        // 
        // analyzeAssetLibraryButton
        // 
        analyzeAssetLibraryButton.AutoSize = true;
        analyzeAssetLibraryButton.Location = new Point(6, 84);
        analyzeAssetLibraryButton.Name = "analyzeAssetLibraryButton";
        analyzeAssetLibraryButton.Size = new Size(92, 33);
        analyzeAssetLibraryButton.TabIndex = 2;
        analyzeAssetLibraryButton.Text = "品質檢查";
        analyzeAssetLibraryButton.UseVisualStyleBackColor = true;
        analyzeAssetLibraryButton.Click += AnalyzeAssetLibraryButton_Click;
        // 
        // deleteLibraryAssetButton
        // 
        deleteLibraryAssetButton.AutoSize = true;
        deleteLibraryAssetButton.Enabled = false;
        deleteLibraryAssetButton.Location = new Point(6, 123);
        deleteLibraryAssetButton.Name = "deleteLibraryAssetButton";
        deleteLibraryAssetButton.Size = new Size(92, 33);
        deleteLibraryAssetButton.TabIndex = 3;
        deleteLibraryAssetButton.Text = "刪除模型";
        deleteLibraryAssetButton.UseVisualStyleBackColor = true;
        deleteLibraryAssetButton.Click += DeleteLibraryAssetButton_Click;
        // 
        // addFavoriteLibraryAssetButton
        // 
        addFavoriteLibraryAssetButton.AutoSize = true;
        addFavoriteLibraryAssetButton.Enabled = false;
        addFavoriteLibraryAssetButton.Location = new Point(6, 162);
        addFavoriteLibraryAssetButton.Name = "addFavoriteLibraryAssetButton";
        addFavoriteLibraryAssetButton.Size = new Size(128, 33);
        addFavoriteLibraryAssetButton.TabIndex = 4;
        addFavoriteLibraryAssetButton.Text = "加到我的最愛";
        addFavoriteLibraryAssetButton.UseVisualStyleBackColor = true;
        addFavoriteLibraryAssetButton.Click += AddFavoriteLibraryAssetButton_Click;
        // 
        // assetFilterTextBox
        // 
        assetFilterTextBox.Dock = DockStyle.Top;
        assetFilterTextBox.Location = new Point(6, 6);
        assetFilterTextBox.Name = "assetFilterTextBox";
        assetFilterTextBox.PlaceholderText = "搜尋模型、貼圖或材質";
        assetFilterTextBox.Size = new Size(236, 30);
        assetFilterTextBox.TabIndex = 0;
        assetFilterTextBox.TextChanged += AssetFilterTextBox_TextChanged;
        // 
        // designSplitContainer
        // 
        designSplitContainer.Dock = DockStyle.Fill;
        designSplitContainer.FixedPanel = FixedPanel.Panel2;
        designSplitContainer.Location = new Point(0, 95);
        designSplitContainer.Name = "designSplitContainer";
        // 
        // designSplitContainer.Panel1
        // 
        designSplitContainer.Panel1.Controls.Add(viewportTableLayoutPanel);
        // 
        // designSplitContainer.Panel2
        // 
        designSplitContainer.Panel2.Controls.Add(rightPanelTabControl);
        designSplitContainer.Size = new Size(1440, 775);
        designSplitContainer.SplitterDistance = 1174;
        designSplitContainer.TabIndex = 2;
        // 
        // viewportTableLayoutPanel
        // 
        viewportTableLayoutPanel.ColumnCount = 2;
        viewportTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        viewportTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        viewportTableLayoutPanel.Controls.Add(topViewport, 0, 0);
        viewportTableLayoutPanel.Controls.Add(frontViewport, 1, 0);
        viewportTableLayoutPanel.Controls.Add(rightViewport, 0, 1);
        viewportTableLayoutPanel.Controls.Add(perspectiveViewport, 1, 1);
        viewportTableLayoutPanel.Controls.Add(previewViewport, 1, 1);
        viewportTableLayoutPanel.Dock = DockStyle.Fill;
        viewportTableLayoutPanel.Location = new Point(0, 0);
        viewportTableLayoutPanel.Name = "viewportTableLayoutPanel";
        viewportTableLayoutPanel.RowCount = 2;
        viewportTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        viewportTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        viewportTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        viewportTableLayoutPanel.Size = new Size(1174, 775);
        viewportTableLayoutPanel.TabIndex = 0;
        // 
        // topViewport
        // 
        topViewport.BackColor = Color.FromArgb(25, 29, 34);
        topViewport.BorderStyle = BorderStyle.FixedSingle;
        topViewport.Dock = DockStyle.Fill;
        topViewport.IsMaximized = false;
        topViewport.Location = new Point(3, 3);
        topViewport.Name = "topViewport";
        topViewport.ShowMaximizeButton = true;
        topViewport.ShowScaleBar = true;
        topViewport.Size = new Size(581, 371);
        topViewport.TabIndex = 0;
        topViewport.ViewKind = InteriorViewportKind.Top;
        topViewport.MaximizeRequested += Viewport_MaximizeRequested;
        topViewport.CameraChanged += Viewport_CameraChanged;
        topViewport.ModelPicked += Viewport_ModelPicked;
        topViewport.TransformStarting += Viewport_TransformStarting;
        topViewport.TransformChanged += Viewport_TransformChanged;
        topViewport.TransformCompleted += Viewport_TransformCompleted;
        topViewport.RenderError += Viewport_RenderError;
        // 
        // frontViewport
        // 
        frontViewport.BackColor = Color.FromArgb(25, 29, 34);
        frontViewport.BorderStyle = BorderStyle.FixedSingle;
        frontViewport.Dock = DockStyle.Fill;
        frontViewport.IsMaximized = false;
        frontViewport.Location = new Point(590, 3);
        frontViewport.Name = "frontViewport";
        frontViewport.ShowMaximizeButton = true;
        frontViewport.ShowScaleBar = true;
        frontViewport.Size = new Size(581, 371);
        frontViewport.TabIndex = 1;
        frontViewport.ViewKind = InteriorViewportKind.Front;
        frontViewport.MaximizeRequested += Viewport_MaximizeRequested;
        frontViewport.CameraChanged += Viewport_CameraChanged;
        frontViewport.ModelPicked += Viewport_ModelPicked;
        frontViewport.TransformStarting += Viewport_TransformStarting;
        frontViewport.TransformChanged += Viewport_TransformChanged;
        frontViewport.TransformCompleted += Viewport_TransformCompleted;
        frontViewport.RenderError += Viewport_RenderError;
        // 
        // rightViewport
        // 
        rightViewport.BackColor = Color.FromArgb(25, 29, 34);
        rightViewport.BorderStyle = BorderStyle.FixedSingle;
        rightViewport.Dock = DockStyle.Fill;
        rightViewport.IsMaximized = false;
        rightViewport.Location = new Point(3, 380);
        rightViewport.Name = "rightViewport";
        rightViewport.ShowMaximizeButton = true;
        rightViewport.ShowScaleBar = true;
        rightViewport.Size = new Size(581, 371);
        rightViewport.TabIndex = 2;
        rightViewport.ViewKind = InteriorViewportKind.Right;
        rightViewport.MaximizeRequested += Viewport_MaximizeRequested;
        rightViewport.CameraChanged += Viewport_CameraChanged;
        rightViewport.ModelPicked += Viewport_ModelPicked;
        rightViewport.TransformStarting += Viewport_TransformStarting;
        rightViewport.TransformChanged += Viewport_TransformChanged;
        rightViewport.TransformCompleted += Viewport_TransformCompleted;
        rightViewport.RenderError += Viewport_RenderError;
        // 
        // perspectiveViewport
        // 
        perspectiveViewport.AllowDrop = true;
        perspectiveViewport.BackColor = Color.FromArgb(25, 29, 34);
        perspectiveViewport.BorderStyle = BorderStyle.FixedSingle;
        perspectiveViewport.Dock = DockStyle.Fill;
        perspectiveViewport.IsMaximized = false;
        perspectiveViewport.Location = new Point(3, 757);
        perspectiveViewport.Name = "perspectiveViewport";
        perspectiveViewport.ShowMaximizeButton = true;
        perspectiveViewport.ShowScaleBar = false;
        perspectiveViewport.Size = new Size(581, 15);
        perspectiveViewport.TabIndex = 3;
        perspectiveViewport.ViewKind = InteriorViewportKind.Perspective;
        perspectiveViewport.MaximizeRequested += Viewport_MaximizeRequested;
        perspectiveViewport.CameraChanged += Viewport_CameraChanged;
        perspectiveViewport.ModelPicked += Viewport_ModelPicked;
        perspectiveViewport.TransformStarting += Viewport_TransformStarting;
        perspectiveViewport.TransformChanged += Viewport_TransformChanged;
        perspectiveViewport.TransformCompleted += Viewport_TransformCompleted;
        perspectiveViewport.RenderError += Viewport_RenderError;
        perspectiveViewport.PlacementPointChanged += PerspectiveViewport_PlacementPointChanged;
        perspectiveViewport.PlacementConfirmed += PerspectiveViewport_PlacementConfirmed;
        perspectiveViewport.PlacementCanceled += PerspectiveViewport_PlacementCanceled;
        perspectiveViewport.DragDrop += PerspectiveViewport_DragDrop;
        perspectiveViewport.DragEnter += PerspectiveViewport_DragEnter;
        perspectiveViewport.DragOver += PerspectiveViewport_DragOver;
        // 
        // previewViewport
        // 
        previewViewport.BackColor = Color.FromArgb(25, 29, 34);
        previewViewport.BorderStyle = BorderStyle.FixedSingle;
        previewViewport.Dock = DockStyle.Fill;
        previewViewport.Location = new Point(590, 380);
        previewViewport.Name = "previewViewport";
        previewViewport.Size = new Size(581, 371);
        previewViewport.TabIndex = 4;
        previewViewport.Visible = false;
        previewViewport.MaximizeRequested += PreviewViewport_MaximizeRequested;
        previewViewport.CameraChanged += PreviewViewport_CameraChanged;
        previewViewport.PreviewError += PreviewViewport_PreviewError;
        previewViewport.ModelPicked += Viewport_ModelPicked;
        previewViewport.AssetDragEnter += PerspectiveViewport_DragEnter;
        previewViewport.AssetDragOver += PerspectiveViewport_DragOver;
        previewViewport.AssetDragDrop += PerspectiveViewport_DragDrop;
        // 
        // rightPanelTabControl
        // 
        rightPanelTabControl.Controls.Add(scenePropertiesTabPage);
        rightPanelTabControl.Controls.Add(modelLibraryTabPage);
        rightPanelTabControl.Dock = DockStyle.Fill;
        rightPanelTabControl.Location = new Point(0, 0);
        rightPanelTabControl.Name = "rightPanelTabControl";
        rightPanelTabControl.SelectedIndex = 0;
        rightPanelTabControl.Size = new Size(262, 775);
        rightPanelTabControl.TabIndex = 0;
        rightPanelTabControl.SelectedIndexChanged += RightPanelTabControl_SelectedIndexChanged;
        // 
        // scenePropertiesTabPage
        // 
        scenePropertiesTabPage.Controls.Add(sceneTemplateSplitContainer);
        scenePropertiesTabPage.Location = new Point(4, 32);
        scenePropertiesTabPage.Name = "scenePropertiesTabPage";
        scenePropertiesTabPage.Padding = new Padding(3);
        scenePropertiesTabPage.Size = new Size(254, 739);
        scenePropertiesTabPage.TabIndex = 0;
        scenePropertiesTabPage.Text = "場景屬性";
        scenePropertiesTabPage.UseVisualStyleBackColor = true;
        // 
        // sceneTemplateSplitContainer
        // 
        sceneTemplateSplitContainer.Dock = DockStyle.Fill;
        sceneTemplateSplitContainer.Location = new Point(3, 3);
        sceneTemplateSplitContainer.Name = "sceneTemplateSplitContainer";
        sceneTemplateSplitContainer.Orientation = Orientation.Horizontal;
        // 
        // sceneTemplateSplitContainer.Panel1
        // 
        sceneTemplateSplitContainer.Panel1.Controls.Add(rightWorkspaceSplitContainer);
        sceneTemplateSplitContainer.Panel1MinSize = 230;
        // 
        // sceneTemplateSplitContainer.Panel2
        // 
        sceneTemplateSplitContainer.Panel2.Controls.Add(sceneTemplatesGroupBox);
        sceneTemplateSplitContainer.Panel2MinSize = 110;
        sceneTemplateSplitContainer.Size = new Size(248, 733);
        sceneTemplateSplitContainer.SplitterDistance = 520;
        sceneTemplateSplitContainer.TabIndex = 0;
        sceneTemplateSplitContainer.SplitterMoved += UiSplitterMoved;
        // 
        // rightWorkspaceSplitContainer
        // 
        rightWorkspaceSplitContainer.Dock = DockStyle.Fill;
        rightWorkspaceSplitContainer.FixedPanel = FixedPanel.Panel2;
        rightWorkspaceSplitContainer.Location = new Point(0, 0);
        rightWorkspaceSplitContainer.Name = "rightWorkspaceSplitContainer";
        rightWorkspaceSplitContainer.Orientation = Orientation.Horizontal;
        // 
        // rightWorkspaceSplitContainer.Panel1
        // 
        rightWorkspaceSplitContainer.Panel1.Controls.Add(sceneModelTableLayoutPanel);
        rightWorkspaceSplitContainer.Panel1MinSize = 100;
        // 
        // rightWorkspaceSplitContainer.Panel2
        // 
        rightWorkspaceSplitContainer.Panel2.Controls.Add(objectPropertyGrid);
        rightWorkspaceSplitContainer.Panel2MinSize = 120;
        rightWorkspaceSplitContainer.Size = new Size(248, 520);
        rightWorkspaceSplitContainer.SplitterDistance = 250;
        rightWorkspaceSplitContainer.TabIndex = 0;
        rightWorkspaceSplitContainer.SplitterMoved += UiSplitterMoved;
        // 
        // sceneModelTableLayoutPanel
        // 
        sceneModelTableLayoutPanel.ColumnCount = 1;
        sceneModelTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        sceneModelTableLayoutPanel.Controls.Add(sceneTreeView, 0, 0);
        sceneModelTableLayoutPanel.Controls.Add(saveSceneTemplatePanel, 0, 1);
        sceneModelTableLayoutPanel.Dock = DockStyle.Fill;
        sceneModelTableLayoutPanel.Location = new Point(0, 0);
        sceneModelTableLayoutPanel.Name = "sceneModelTableLayoutPanel";
        sceneModelTableLayoutPanel.RowCount = 2;
        sceneModelTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        sceneModelTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        sceneModelTableLayoutPanel.Size = new Size(248, 250);
        sceneModelTableLayoutPanel.TabIndex = 0;
        // 
        // sceneTreeView
        // 
        sceneTreeView.Dock = DockStyle.Fill;
        sceneTreeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
        sceneTreeView.HideSelection = false;
        sceneTreeView.Location = new Point(3, 3);
        sceneTreeView.Name = "sceneTreeView";
        treeNode39.Name = "";
        treeNode39.Text = "RV室內設計場景";
        sceneTreeView.Nodes.AddRange(new TreeNode[] { treeNode39 });
        sceneTreeView.Size = new Size(242, 198);
        sceneTreeView.TabIndex = 0;
        sceneTreeView.SelectedNodesChanged += SceneTreeView_SelectedNodesChanged;
        // 
        // saveSceneTemplatePanel
        // 
        saveSceneTemplatePanel.Controls.Add(sceneTemplateNameTextBox);
        saveSceneTemplatePanel.Controls.Add(saveSceneTemplateButton);
        saveSceneTemplatePanel.Dock = DockStyle.Fill;
        saveSceneTemplatePanel.Location = new Point(3, 207);
        saveSceneTemplatePanel.Name = "saveSceneTemplatePanel";
        saveSceneTemplatePanel.Padding = new Padding(2);
        saveSceneTemplatePanel.Size = new Size(242, 40);
        saveSceneTemplatePanel.TabIndex = 1;
        saveSceneTemplatePanel.WrapContents = false;
        // 
        // sceneTemplateNameTextBox
        // 
        sceneTemplateNameTextBox.Location = new Point(5, 5);
        sceneTemplateNameTextBox.Name = "sceneTemplateNameTextBox";
        sceneTemplateNameTextBox.PlaceholderText = "輸入範本名稱";
        sceneTemplateNameTextBox.Size = new Size(126, 30);
        sceneTemplateNameTextBox.TabIndex = 0;
        // 
        // saveSceneTemplateButton
        // 
        saveSceneTemplateButton.Location = new Point(137, 5);
        saveSceneTemplateButton.MinimumSize = new Size(88, 34);
        saveSceneTemplateButton.Name = "saveSceneTemplateButton";
        saveSceneTemplateButton.Size = new Size(102, 34);
        saveSceneTemplateButton.TabIndex = 1;
        saveSceneTemplateButton.Text = "加入範本";
        saveSceneTemplateButton.UseVisualStyleBackColor = true;
        saveSceneTemplateButton.Click += SaveSceneTemplateButton_Click;
        // 
        // objectPropertyGrid
        // 
        objectPropertyGrid.Dock = DockStyle.Fill;
        objectPropertyGrid.Location = new Point(0, 0);
        objectPropertyGrid.Name = "objectPropertyGrid";
        objectPropertyGrid.PropertySort = PropertySort.Categorized;
        objectPropertyGrid.Size = new Size(248, 266);
        objectPropertyGrid.TabIndex = 0;
        objectPropertyGrid.ToolbarVisible = false;
        // 
        // sceneTemplatesGroupBox
        // 
        sceneTemplatesGroupBox.Controls.Add(sceneTemplatesTableLayoutPanel);
        sceneTemplatesGroupBox.Dock = DockStyle.Fill;
        sceneTemplatesGroupBox.Location = new Point(0, 0);
        sceneTemplatesGroupBox.Name = "sceneTemplatesGroupBox";
        sceneTemplatesGroupBox.Size = new Size(248, 209);
        sceneTemplatesGroupBox.TabIndex = 0;
        sceneTemplatesGroupBox.TabStop = false;
        sceneTemplatesGroupBox.Text = "場景範本";
        // 
        // sceneTemplatesTableLayoutPanel
        // 
        sceneTemplatesTableLayoutPanel.ColumnCount = 1;
        sceneTemplatesTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        sceneTemplatesTableLayoutPanel.Controls.Add(sceneTemplatesListView, 0, 0);
        sceneTemplatesTableLayoutPanel.Controls.Add(sceneTemplateCommandPanel, 0, 1);
        sceneTemplatesTableLayoutPanel.Dock = DockStyle.Fill;
        sceneTemplatesTableLayoutPanel.Location = new Point(3, 26);
        sceneTemplatesTableLayoutPanel.Name = "sceneTemplatesTableLayoutPanel";
        sceneTemplatesTableLayoutPanel.RowCount = 2;
        sceneTemplatesTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        sceneTemplatesTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        sceneTemplatesTableLayoutPanel.Size = new Size(242, 180);
        sceneTemplatesTableLayoutPanel.TabIndex = 0;
        // 
        // sceneTemplatesListView
        // 
        sceneTemplatesListView.Columns.AddRange(new ColumnHeader[] { sceneTemplateNameColumnHeader, sceneTemplateModelCountColumnHeader });
        sceneTemplatesListView.Dock = DockStyle.Fill;
        sceneTemplatesListView.FullRowSelect = true;
        sceneTemplatesListView.Location = new Point(3, 3);
        sceneTemplatesListView.MultiSelect = false;
        sceneTemplatesListView.Name = "sceneTemplatesListView";
        sceneTemplatesListView.ShowItemToolTips = true;
        sceneTemplatesListView.Size = new Size(236, 132);
        sceneTemplatesListView.TabIndex = 0;
        sceneTemplatesListView.UseCompatibleStateImageBehavior = false;
        sceneTemplatesListView.View = View.Details;
        sceneTemplatesListView.SelectedIndexChanged += SceneTemplatesListView_SelectedIndexChanged;
        sceneTemplatesListView.DoubleClick += LoadSceneTemplateButton_Click;
        // 
        // sceneTemplateNameColumnHeader
        // 
        sceneTemplateNameColumnHeader.Text = "名稱";
        sceneTemplateNameColumnHeader.Width = 150;
        // 
        // sceneTemplateModelCountColumnHeader
        // 
        sceneTemplateModelCountColumnHeader.Text = "模型";
        sceneTemplateModelCountColumnHeader.Width = 55;
        // 
        // sceneTemplateCommandPanel
        // 
        sceneTemplateCommandPanel.Controls.Add(loadSceneTemplateButton);
        sceneTemplateCommandPanel.Controls.Add(deleteSceneTemplateButton);
        sceneTemplateCommandPanel.Controls.Add(importSceneTemplateButton);
        sceneTemplateCommandPanel.Dock = DockStyle.Fill;
        sceneTemplateCommandPanel.Location = new Point(3, 141);
        sceneTemplateCommandPanel.Name = "sceneTemplateCommandPanel";
        sceneTemplateCommandPanel.Size = new Size(236, 36);
        sceneTemplateCommandPanel.TabIndex = 1;
        // 
        // loadSceneTemplateButton
        // 
        loadSceneTemplateButton.Dock = DockStyle.Fill;
        loadSceneTemplateButton.Enabled = false;
        loadSceneTemplateButton.Location = new Point(128, 0);
        loadSceneTemplateButton.Name = "loadSceneTemplateButton";
        loadSceneTemplateButton.Size = new Size(108, 36);
        loadSceneTemplateButton.TabIndex = 2;
        loadSceneTemplateButton.Text = "載入至場景";
        loadSceneTemplateButton.UseVisualStyleBackColor = true;
        loadSceneTemplateButton.Click += LoadSceneTemplateButton_Click;
        // 
        // deleteSceneTemplateButton
        // 
        deleteSceneTemplateButton.Dock = DockStyle.Left;
        deleteSceneTemplateButton.Enabled = false;
        deleteSceneTemplateButton.Location = new Point(64, 0);
        deleteSceneTemplateButton.Name = "deleteSceneTemplateButton";
        deleteSceneTemplateButton.Size = new Size(64, 36);
        deleteSceneTemplateButton.TabIndex = 1;
        deleteSceneTemplateButton.Text = "刪除";
        deleteSceneTemplateButton.UseVisualStyleBackColor = true;
        deleteSceneTemplateButton.Click += DeleteSceneTemplateButton_Click;
        // 
        // importSceneTemplateButton
        // 
        importSceneTemplateButton.Dock = DockStyle.Left;
        importSceneTemplateButton.Location = new Point(0, 0);
        importSceneTemplateButton.Name = "importSceneTemplateButton";
        importSceneTemplateButton.Size = new Size(64, 36);
        importSceneTemplateButton.TabIndex = 0;
        importSceneTemplateButton.Text = "加入";
        importSceneTemplateButton.UseVisualStyleBackColor = true;
        importSceneTemplateButton.Click += ImportSceneTemplateButton_Click;
        // 
        // modelLibraryTabPage
        // 
        modelLibraryTabPage.Controls.Add(libraryPanel);
        modelLibraryTabPage.Location = new Point(4, 32);
        modelLibraryTabPage.Name = "modelLibraryTabPage";
        modelLibraryTabPage.Padding = new Padding(3);
        modelLibraryTabPage.Size = new Size(254, 761);
        modelLibraryTabPage.TabIndex = 1;
        modelLibraryTabPage.Text = "模型庫";
        modelLibraryTabPage.UseVisualStyleBackColor = true;
        // 
        // importSceneTemplateDialog
        // 
        importSceneTemplateDialog.Filter = "RV室內設計專案 (*.rv3dproj)|*.rv3dproj";
        importSceneTemplateDialog.RestoreDirectory = true;
        importSceneTemplateDialog.Title = "加入場景範本專案";
        // 
        // statusStrip
        // 
        statusStrip.ImageScalingSize = new Size(24, 24);
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, statusProgressBar });
        statusStrip.Location = new Point(0, 870);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1440, 30);
        statusStrip.TabIndex = 3;
        // 
        // statusLabel
        // 
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(280, 23);
        statusLabel.Spring = true;
        statusLabel.Text = "就緒；請從右側模型庫選取項目。";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // statusProgressBar
        // 
        statusProgressBar.MarqueeAnimationSpeed = 24;
        statusProgressBar.Name = "statusProgressBar";
        statusProgressBar.Size = new Size(260, 22);
        statusProgressBar.Visible = false;
        // 
        // RVInteriorDesignForm
        // 
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(1440, 900);
        Controls.Add(designSplitContainer);
        Controls.Add(explanationPanel);
        Controls.Add(mainToolStrip);
        Controls.Add(unifiedMenuStrip);
        Controls.Add(statusStrip);
        KeyPreview = true;
        MainMenuStrip = unifiedMenuStrip;
        MinimumSize = new Size(1100, 700);
        Name = "RVInteriorDesignForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "RV室內設計";
        WindowState = FormWindowState.Maximized;
        FormClosing += RVInteriorDesignForm_FormClosing;
        Shown += RVInteriorDesignForm_Shown;
        mainToolStrip.ResumeLayout(false);
        mainToolStrip.PerformLayout();
        explanationPanel.ResumeLayout(false);
        libraryPanel.ResumeLayout(false);
        libraryPanel.PerformLayout();
        libraryTabControl.ResumeLayout(false);
        modelsTabPage.ResumeLayout(false);
        modelLibrarySplitContainer.Panel1.ResumeLayout(false);
        modelLibrarySplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)modelLibrarySplitContainer).EndInit();
        modelLibrarySplitContainer.ResumeLayout(false);
        modelCommandPanel.ResumeLayout(false);
        parameterCreationTableLayoutPanel.ResumeLayout(false);
        texturesTabPage.ResumeLayout(false);
        textureLibraryTableLayoutPanel.ResumeLayout(false);
        materialsTabPage.ResumeLayout(false);
        materialLibraryTableLayoutPanel.ResumeLayout(false);
        assetLibraryTabPage.ResumeLayout(false);
        assetLibraryTabPage.PerformLayout();
        assetTabControl.ResumeLayout(false);
        furnitureTabPage.ResumeLayout(false);
        applianceTabPage.ResumeLayout(false);
        lightingAssetTabPage.ResumeLayout(false);
        doorsWindowsTabPage.ResumeLayout(false);
        kitchenTabPage.ResumeLayout(false);
        bathroomTabPage.ResumeLayout(false);
        storageTabPage.ResumeLayout(false);
        decorTabPage.ResumeLayout(false);
        plantsTabPage.ResumeLayout(false);
        otherAssetsTabPage.ResumeLayout(false);
        customAssetsTabPage.ResumeLayout(false);
        onlineAssetsTabPage.ResumeLayout(false);
        assetLibraryCommandPanel.ResumeLayout(false);
        assetLibraryCommandPanel.PerformLayout();
        designSplitContainer.Panel1.ResumeLayout(false);
        designSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)designSplitContainer).EndInit();
        designSplitContainer.ResumeLayout(false);
        viewportTableLayoutPanel.ResumeLayout(false);
        rightPanelTabControl.ResumeLayout(false);
        scenePropertiesTabPage.ResumeLayout(false);
        sceneTemplateSplitContainer.Panel1.ResumeLayout(false);
        sceneTemplateSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)sceneTemplateSplitContainer).EndInit();
        sceneTemplateSplitContainer.ResumeLayout(false);
        rightWorkspaceSplitContainer.Panel1.ResumeLayout(false);
        rightWorkspaceSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)rightWorkspaceSplitContainer).EndInit();
        rightWorkspaceSplitContainer.ResumeLayout(false);
        sceneModelTableLayoutPanel.ResumeLayout(false);
        saveSceneTemplatePanel.ResumeLayout(false);
        saveSceneTemplatePanel.PerformLayout();
        sceneTemplatesGroupBox.ResumeLayout(false);
        sceneTemplatesTableLayoutPanel.ResumeLayout(false);
        sceneTemplateCommandPanel.ResumeLayout(false);
        modelLibraryTabPage.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
