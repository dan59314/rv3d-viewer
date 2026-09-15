
/*
統一 Viewport 的滑鼠操作行為：

1. 左鍵拖曳：Orbit 。
2. 中鍵拖曳：移動 Camera Target。
3. 右鍵拖曳：Pan。
4. 滾輪：將 camera position 沿 position-target 的向量 前進或後退。
5. 滾輪 + Ctrl : FOV
6. 滾輪 + Shift : Roll
*/



using Rv3dViewer.Core;
using Rv3dViewer.App.Plugins;
using Rv3dViewer.Plugin.Abstractions;
using Rv3dViewer.Plugin.WinForms;
using Rv3dViewer.Rendering.OpenGL;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using WiseCooling_TwoPhase;

namespace Rv3dViewer.App;

public partial class MainForm : Form
{
    private static string LastSceneDirectory => Path.Combine(AppContext.BaseDirectory, "lastScene");
    private static string LastScenePath => Path.Combine(LastSceneDirectory, "lastScene.rv3dproj");
    private static string LegacyLastScenePath => Path.Combine(AppContext.BaseDirectory, "lastScene.rv3dproj");
    private static string LibrariesDirectory => Path.Combine(AppContext.BaseDirectory, "libraries");
    private static string SkyboxLibraryDirectory => Path.Combine(LibrariesDirectory, "Skybox");
    private static string DefaultMaterialLibraryPath => Path.Combine(LibrariesDirectory, "materials.mtllib");
    private static string UiSettingsPath => Path.Combine(LibrariesDirectory, "ui-settings.json");
    private static string PluginsDirectory => Path.Combine(AppContext.BaseDirectory, "PlugIn");
    private static string TutorialsDirectory => Path.Combine(AppContext.BaseDirectory, "Tutorials");
    private const int UndoHistoryLimit = 30;
    private const string NavigateHint = "退出編輯｜左鍵拖曳：旋轉視角｜中鍵拖曳：移動 Camera Target｜右鍵拖曳：平移｜滾輪：縮放";
    private const string SelectModelHint = "選取模型｜Ctrl + 點擊：選取最前方模型｜Ctrl + 拖曳：框選加入模型｜Ctrl + Alt + 點擊／框選：減少選取";
    private const string SelectMeshHint = "選取 Mesh｜Ctrl + 點擊：選取最前方 Mesh｜Ctrl + 拖曳：框選加入 Mesh｜Ctrl + Alt + 點擊／框選：減少選取";
    private const string SubtractHint = "減選模式｜Ctrl + 點擊或拖曳框選：從目前選取中移除模型／Mesh｜需要加選時請切回選取模式";
    private static readonly JsonSerializerOptions UndoJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true
    };
    private static readonly JsonSerializerOptions LightSettingsJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true
    };
    private readonly IModelImporter _importer = new AssimpModelImporter();
    private readonly IProjectStore _projectStore = new JsonProjectStore();
    private readonly IMaterialLibraryStore _materialLibraryStore = new JsonMaterialLibraryStore();
    private readonly OpenGlRenderer _renderer = null!;
    private readonly MainFormPluginContext _pluginContext = null!;
    private PluginManager? _pluginManager;
    private ViewerProject _project = new();
    private List<PbrMaterial> _materialLibrary = [];
    private SceneModel? _selectedModel;
    private PbrMaterial? _selectedMaterial;
    private PbrMaterial? _selectedLibraryMaterial;
    private int? _selectedMeshIndex;
    private readonly HashSet<MeshSelection> _treeSelectedMeshes = [];
    private bool _treeSelectionRepresentsModels;
    private MeshSelection? _treeSelectionAnchor;
    private TreeNode? _appliedMaterialNode;
    private SceneLight? _selectedLight;
    private SkyboxSettings? _selectedSkybox;
    private bool _dirty;
    private bool _closingConfirmed;
    private bool _rebuildingLights;
    private bool _rebuildingSkyboxes;
    private bool _rebuildingMaterialLibrary;
    private bool _syncingBoxSelection;
    private bool _rebuildingSceneTree;
    private bool _syncingAllModelsCheckBox;
    private bool _allowLightItemCheck;
    private int _lightCheckMouseDownIndex = -1;
    private SceneModel? _contextMaterialModel;
    private PbrMaterial? _contextMaterial;
    private ViewportEditMode _editMode = ViewportEditMode.Navigate;
    private bool _isFullScreen;
    private Rectangle _restoreBounds;
    private FormWindowState _restoreWindowState;
    private FormBorderStyle _restoreBorderStyle;
    private AppUiSettings _savedUiSettings = new();
    private ViewportPreviewMode _previewMode = ViewportPreviewMode.Preview1;
    private readonly List<UndoEntry> _undoHistory = [];
    private UndoEntry? _undoBaseline;
    private bool _skipNextAutomaticUndo;
    private DateTime _lastAutomaticUndoUtc;
    private bool FIsDarkMode = false;
    private bool _syncingViewOptions;
    private bool _syncingViewGroupExpansion;
    private int _pluginCameraPlaybackLocks;
    private bool _pluginProjectEditActive;
    private bool _cameraInteractionActive;
    private bool _cameraInteractionChanged;
    private readonly Stopwatch _cameraUiRefreshClock = Stopwatch.StartNew();


    public MainForm()
    {
        InitializeComponent();

        // Visual Studio instantiates the form inside its out-of-process designer.
        // Runtime DPI/font normalization must not execute there, otherwise the
        // designer serializes the converted values back as progressively smaller
        // fonts every time MainForm is opened or saved.
        if (IsDesignTimeHost())
            return;

        RichColorEditor.Register();

        RefreshTutorialMenu(showErrors: false);

        Initial_Interfacees();


        _savedUiSettings = LoadUiSettings();
        FIsDarkMode = _savedUiSettings.IsDarkMode;
        darkModeMenuItem.Checked = FIsDarkMode;
        _renderer = new OpenGlRenderer(viewportGlControl);
        ApplyViewportColors();
        ApplyInputOverlayStyle();
        InputOverlayPreferences.Changed += InputOverlayPreferences_Changed;
        ViewportColorPreferences.Changed += ViewportColorPreferences_Changed;
        _renderer.SetProject(_project);
        RestoreSavedViewOptions();
        _pluginContext = new MainFormPluginContext(this);
        _renderer.CameraInteractionStarted += (_, _) => BeginCameraInteraction();
        _renderer.CameraChanged += (_, _) => RendererCameraChanged();
        _renderer.CameraInteractionCompleted += (_, _) => CompleteCameraInteraction();
        _renderer.RendererError += (_, message) => statusLabel.Text = message;
        _renderer.BoxSelectionChanged += (_, e) => BoxSelectionChanged(e.Models, e.Meshes);
        _selectedLight = _project.Lights.FirstOrDefault();
        cameraPropertyGrid.SelectedObject = new CameraInspector(_project.Camera, CameraEdited);
        FormClosing += MainForm_FormClosing;
        Shown += MainForm_Shown;
        RebuildLightList();
        RebuildSkyboxList();
        RebuildMaterialLibrary();
        ApplyInterfaceTheme();
        UpdateUi();
        _undoBaseline = CaptureUndoEntry("編輯場景");
    }

    private static bool IsDesignTimeHost() =>
        LicenseManager.UsageMode == LicenseUsageMode.Designtime ||
        string.Equals(Process.GetCurrentProcess().ProcessName, "devenv", StringComparison.OrdinalIgnoreCase) ||
        Process.GetCurrentProcess().ProcessName.Contains("DesignToolsServer", StringComparison.OrdinalIgnoreCase);

    private void Initial_Interfacees()
    {
        UpdateUndoMenuItem();

        // Set the initial state of the interface elements
        var modelMode = GetProjectModelDisplayMode();
        modelPointsMenuItem.Checked = modelMode == ModelDisplayMode.Points;
        modelWireframeMenuItem.Checked = modelMode == ModelDisplayMode.Wireframe;
        modelSolidMenuItem.Checked = modelMode == ModelDisplayMode.Solid;
        texturesMenuItem.Checked = _project.RenderSettings.ShowTextures;
        gridMenuItem.Checked = _project.RenderSettings.ShowGrid;
        worldAxesMenuItem.Checked = _project.RenderSettings.ShowWorldAxes;
        selectionHighlightMenuItem.Checked = _project.RenderSettings.ShowSelectionHighlight;
        SyncViewOptions();
        UpdateEditModeChecks();
    }

    private void Exit_Click(object? sender, EventArgs e) => Close();

    private async void MainForm_Shown(object? sender, EventArgs e)
    {

        RefreshTutorialMenu(showErrors: false);
        RestorePanelWidths(_savedUiSettings);
        await LoadAutomaticMaterialLibraryAsync();
        ReloadSkyboxLibrary(showStatus: false);
        var startupProjectPath = File.Exists(LastScenePath)
            ? LastScenePath
            : File.Exists(LegacyLastScenePath)
                ? LegacyLastScenePath
                : null;
        if (startupProjectPath is not null)
            await LoadProjectAsync(startupProjectPath, promptForMissingAssets: false);
        ReloadPlugins();

        ShowAboutDialog();
    }

    private void ReloadPlugins()
    {
        _pluginManager?.Dispose();
        pluginMenuItem.DropDownItems.Clear();
        _pluginManager = new PluginManager(PluginsDirectory, _pluginContext);
        var result = _pluginManager.LoadAll();

        foreach (var plugin in result.Plugins)
        {
            var pluginItem = new ToolStripMenuItem(plugin.Manifest.Name)
            {
                ToolTipText = string.IsNullOrWhiteSpace(plugin.Manifest.Description)
                    ? $"版本 {plugin.Manifest.Version}"
                    : $"{plugin.Manifest.Description}\n版本 {plugin.Manifest.Version}"
            };
            foreach (var command in plugin.Commands)
            {
                var commandItem = new ToolStripMenuItem(command.Text) { Tag = new PluginMenuCommand(plugin, command) };
                commandItem.Click += PluginCommand_Click;
                pluginItem.DropDownItems.Add(commandItem);
            }
            if (plugin.Commands.Count == 0)
                pluginItem.DropDownItems.Add(new ToolStripMenuItem("（沒有可用命令）") { Enabled = false });
            pluginMenuItem.DropDownItems.Add(pluginItem);
        }

        if (result.Plugins.Count == 0)
            pluginMenuItem.DropDownItems.Add(noPluginsMenuItem);

        pluginMenuItem.DropDownItems.Add(pluginSeparator);
        pluginMenuItem.DropDownItems.Add(reloadPluginsMenuItem);
        pluginMenuItem.DropDownItems.Add(openPluginFolderMenuItem);
        pluginMenuItem.DropDownOpening -= PluginMenuItem_DropDownOpening;
        pluginMenuItem.DropDownOpening += PluginMenuItem_DropDownOpening;

        var palette = FIsDarkMode ? ThemePalette.Dark : ThemePalette.Light;
        ApplyToolStripItemsTheme(mainMenuStrip.Items, palette, mainMenuStrip.Renderer);

        RefreshTutorialMenu(showErrors: false);

        statusLabel.Text = result.Issues.Count == 0
            ? $"已載入 {result.Plugins.Count:N0} 個外掛"
            : $"已載入 {result.Plugins.Count:N0} 個外掛，{result.Issues.Count:N0} 個載入錯誤（請查看 PlugIn\\Logs）";
    }

    private void PluginMenuItem_DropDownOpening(object? sender, EventArgs e)
    {
        foreach (var item in EnumerateMenuItems(pluginMenuItem.DropDownItems))
            if (item.Tag is PluginMenuCommand entry)
            {
                try { item.Enabled = entry.Command.CanExecute?.Invoke() ?? true; }
                catch { item.Enabled = false; }
            }
    }

    private static IEnumerable<ToolStripMenuItem> EnumerateMenuItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
            if (item is ToolStripMenuItem menuItem)
            {
                yield return menuItem;
                foreach (var child in EnumerateMenuItems(menuItem.DropDownItems)) yield return child;
            }
    }

    private async void PluginCommand_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: PluginMenuCommand entry } || _pluginManager is null) return;
        try
        {
            SetBusy(true, $"正在執行外掛：{entry.Command.Text}...");
            await _pluginManager.ExecuteAsync(entry.Plugin, entry.Command);
            statusLabel.Text = $"外掛命令完成：{entry.Command.Text}";
        }
        catch (PluginExecutionException ex)
        {
            MessageBox.Show(this, ex.InnerException?.Message ?? ex.Message, "外掛執行失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OpenPluginFolder_Click(object? sender, EventArgs e)
    {
        Directory.CreateDirectory(PluginsDirectory);
        Process.Start(new ProcessStartInfo(PluginsDirectory) { UseShellExecute = true });
    }

    private void ReloadPlugins_Click(object? sender, EventArgs e) => ReloadPlugins();

    private void RefreshTutorialMenu_Click(object? sender, EventArgs e) => RefreshTutorialMenu(showErrors: true);

    private void RefreshTutorialMenu(bool showErrors)
    {
        try
        {
            Directory.CreateDirectory(TutorialsDirectory);
            tutorialMenuItem.DropDownItems.Clear();
            tutorialMenuItem.DropDownItems.Add(updateTutorialMenuItem);
            tutorialMenuItem.DropDownItems.Add(tutorialSeparator);
            var contentCount = AddTutorialEntries(tutorialMenuItem.DropDownItems, TutorialsDirectory);
            var pluginTutorialItems = new List<ToolStripItem>();
            var pluginContentCount = AddPluginTutorialEntries(pluginTutorialItems);
            if (pluginContentCount > 0)
            {
                if (contentCount > 0)
                    tutorialMenuItem.DropDownItems.Add(new ToolStripSeparator());
                tutorialMenuItem.DropDownItems.AddRange(pluginTutorialItems.ToArray());
                contentCount += pluginContentCount;
            }
            if (contentCount == 0)
                tutorialMenuItem.DropDownItems.Add(noTutorialContentMenuItem);

            var palette = FIsDarkMode ? ThemePalette.Dark : ThemePalette.Light;
            ApplyToolStripItemsTheme(tutorialMenuItem.DropDownItems, palette, mainMenuStrip.Renderer);
            if (showErrors)
                statusLabel.Text = $"已更新教學選單：{contentCount:N0} 個項目";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (showErrors)
                MessageBox.Show(this, ex.Message, "更新教學選單失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                statusLabel.Text = $"無法更新教學選單：{ex.Message}";
        }
    }

    private int AddPluginTutorialEntries(ICollection<ToolStripItem> target)
    {
        Directory.CreateDirectory(PluginsDirectory);
        var entries = new List<(string DisplayName, string DirectoryPath)>();
        foreach (var pluginDirectory in Directory.EnumerateDirectories(PluginsDirectory))
        {
            try
            {
                var info = new DirectoryInfo(pluginDirectory);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                var tutorialsDirectory = Path.Combine(pluginDirectory, "Tutorials");
                if (!Directory.Exists(tutorialsDirectory)) continue;
                entries.Add((ReadPluginDisplayName(pluginDirectory), tutorialsDirectory));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // A damaged or inaccessible plug-in must not hide tutorials
                // belonging to the remaining installed plug-ins.
            }
        }

        var count = 0;
        foreach (var entry in entries.OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                     .ThenBy(item => item.DirectoryPath, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var pluginItem = new ToolStripMenuItem(entry.DisplayName);
                var childCount = AddTutorialEntries(pluginItem.DropDownItems, entry.DirectoryPath);
                if (childCount == 0)
                {
                    pluginItem.Dispose();
                    continue;
                }
                target.Add(pluginItem);
                count += childCount + 1;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Continue scanning when one Tutorials tree cannot be read.
            }
        }
        return count;
    }

    private static string ReadPluginDisplayName(string pluginDirectory)
    {
        var fallback = Path.GetFileName(pluginDirectory);
        var manifestPath = Path.Combine(pluginDirectory, "plugin.json");
        if (!File.Exists(manifestPath)) return fallback;
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath), new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });
        if (!document.RootElement.TryGetProperty("name", out var nameElement) ||
            nameElement.ValueKind != JsonValueKind.String) return fallback;
        var name = nameElement.GetString();
        return string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
    }

    private int AddTutorialEntries(ToolStripItemCollection target, string directoryPath)
    {
        var count = 0;
        foreach (var directory in Directory.EnumerateDirectories(directoryPath)
                     .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase))
        {
            var info = new DirectoryInfo(directory);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;
            var directoryItem = new ToolStripMenuItem(info.Name);
            var childCount = AddTutorialEntries(directoryItem.DropDownItems, directory);
            if (childCount == 0)
                directoryItem.DropDownItems.Add(new ToolStripMenuItem("（空白）") { Enabled = false });
            target.Add(directoryItem);
            count++;
        }

        foreach (var filePath in Directory.EnumerateFiles(directoryPath)
                     .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase))
        {
            var fileItem = new ToolStripMenuItem(Path.GetFileName(filePath)) { Tag = filePath };
            fileItem.Click += OpenTutorialFile_Click;
            target.Add(fileItem);
            count++;
        }
        return count;
    }

    private void OpenTutorialFile_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: string filePath } || !File.Exists(filePath))
        {
            statusLabel.Text = "教學檔案已不存在，請更新教學選單。";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            MessageBox.Show(this, ex.Message, "開啟教學檔案失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static AppUiSettings LoadUiSettings()
    {
        try
        {
            if (!File.Exists(UiSettingsPath)) return new AppUiSettings();
            return JsonSerializer.Deserialize<AppUiSettings>(File.ReadAllText(UiSettingsPath)) ?? new AppUiSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppUiSettings();
        }
    }

    private void RestorePanelWidths(AppUiSettings settings)
    {
        if (settings.LeftPanelWidth > 0)
        {
            var maximum = rootSplitContainer.ClientSize.Width - rootSplitContainer.SplitterWidth - rootSplitContainer.Panel2MinSize;
            rootSplitContainer.SplitterDistance = Math.Clamp(
                settings.LeftPanelWidth,
                rootSplitContainer.Panel1MinSize,
                Math.Max(rootSplitContainer.Panel1MinSize, maximum));
        }

        if (settings.CenterPanelWidth <= 0 || settings.RightPanelWidth <= 0) return;
        var available = workSplitContainer.ClientSize.Width - workSplitContainer.SplitterWidth;
        if (available <= 0) return;
        var savedTotal = settings.CenterPanelWidth + settings.RightPanelWidth;
        var desiredCenter = savedTotal == available
            ? settings.CenterPanelWidth
            : (int)Math.Round(settings.CenterPanelWidth * (double)available / savedTotal);
        var minimum = workSplitContainer.Panel1MinSize;
        var maximumCenter = available - workSplitContainer.Panel2MinSize;
        workSplitContainer.SplitterDistance = Math.Clamp(
            desiredCenter,
            minimum,
            Math.Max(minimum, maximumCenter));
    }

    private async Task SaveUiSettingsAsync()
    {
        Directory.CreateDirectory(LibrariesDirectory);
        var settings = new AppUiSettings
        {
            IsDarkMode = FIsDarkMode,
            LeftPanelWidth = rootSplitContainer.Panel1.Width,
            CenterPanelWidth = workSplitContainer.Panel1.Width,
            RightPanelWidth = workSplitContainer.Panel2.Width,
            LastPreviewImageOutputPath = _savedUiSettings.LastPreviewImageOutputPath,
            PreviewMode = (int)_previewMode,
            QuickPreviewEnabled = _previewMode == ViewportPreviewMode.Preview1,
            ShowTextures = texturesMenuItem.Checked,
            ShowGrid = gridMenuItem.Checked,
            ShowWorldAxes = worldAxesMenuItem.Checked,
            ShowCameraGizmo = cameraGizmoMenuItem.Checked,
            ShowLightGizmos = lightGizmosMenuItem.Checked,
            ShowInputInfo = inputInfoMenuItem.Checked,
            ViewDisplayGroupExpanded = viewDisplayGroup.Expanded,
            ViewColorGroupExpanded = viewColorGroup.Expanded,
            ShowSelectionHighlight = selectionHighlightMenuItem.Checked,
            Wireframe = GetProjectModelDisplayMode() == ModelDisplayMode.Wireframe,
            ModelDisplayMode = (int)GetProjectModelDisplayMode()
        };
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(UiSettingsPath, json);
        _savedUiSettings = settings;
    }

    private void RestoreSavedViewOptions()
    {
        _previewMode = Enum.IsDefined(typeof(ViewportPreviewMode), _savedUiSettings.PreviewMode)
            ? (ViewportPreviewMode)_savedUiSettings.PreviewMode
            : _savedUiSettings.QuickPreviewEnabled is bool quickPreview
                ? quickPreview ? ViewportPreviewMode.Preview1 : ViewportPreviewMode.Preview2
                : ViewportPreviewMode.Preview1;
        if (_savedUiSettings.ShowGrid is bool showGrid)
        {
            gridMenuItem.Checked = showGrid;
            _project.RenderSettings.ShowGrid = showGrid;
        }
        if (_savedUiSettings.ShowTextures is bool showTextures)
        {
            texturesMenuItem.Checked = showTextures;
            _project.RenderSettings.ShowTextures = showTextures;
        }
        if (_savedUiSettings.ShowWorldAxes is bool showWorldAxes)
        {
            worldAxesMenuItem.Checked = showWorldAxes;
            _project.RenderSettings.ShowWorldAxes = showWorldAxes;
        }
        if (_savedUiSettings.ShowSelectionHighlight is bool showSelectionHighlight)
        {
            selectionHighlightMenuItem.Checked = showSelectionHighlight;
            _project.RenderSettings.ShowSelectionHighlight = showSelectionHighlight;
        }
        if (_savedUiSettings.ModelDisplayMode is int savedModelMode &&
            Enum.IsDefined(typeof(ModelDisplayMode), savedModelMode))
        {
            SetModelDisplayMode((ModelDisplayMode)savedModelMode, markDirty: false);
        }
        if (_savedUiSettings.ShowCameraGizmo is bool showCameraGizmo)
        {
            cameraGizmoMenuItem.Checked = showCameraGizmo;
            _project.RenderSettings.ShowCameraGizmo = showCameraGizmo;
        }
        if (_savedUiSettings.ShowLightGizmos is bool showLightGizmos)
        {
            lightGizmosMenuItem.Checked = showLightGizmos;
            _project.RenderSettings.ShowLightGizmos = showLightGizmos;
        }
        else if (_savedUiSettings.Wireframe is bool wireframe)
        {
            SetModelDisplayMode(wireframe ? ModelDisplayMode.Wireframe : ModelDisplayMode.Solid, markDirty: false);
        }
        if (_savedUiSettings.ShowInputInfo is bool showInputInfo)
            inputInfoMenuItem.Checked = showInputInfo;
        _renderer.SetInputOverlayEnabled(inputInfoMenuItem.Checked);
        if (_savedUiSettings.ViewDisplayGroupExpanded is bool displayExpanded)
            viewDisplayGroup.Expanded = displayExpanded;
        if (_savedUiSettings.ViewColorGroupExpanded is bool colorExpanded)
            viewColorGroup.Expanded = colorExpanded;

        // Always set the renderer explicitly. Older settings only contain the
        // QuickPreviewEnabled boolean and are migrated above to Preview 1 or 2.
        ApplyPreviewMode(_previewMode);
        SyncViewOptions();
    }

    private async Task LoadAutomaticMaterialLibraryAsync()
    {
        if (!File.Exists(DefaultMaterialLibraryPath)) return;
        try
        {
            _materialLibrary = await _materialLibraryStore.LoadAsync(DefaultMaterialLibraryPath);
            RebuildMaterialLibrary();
            statusLabel.Text = $"已載入材質資料庫：{_materialLibrary.Count:N0} 筆";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"無法載入預設材質資料庫：\n{ex.Message}", "材質資料庫", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void NewProject_Click(object? sender, EventArgs e)
    {
        if (!ConfirmDiscardChanges()) return;
        _project = new ViewerProject();
        _selectedModel = null;
        _selectedMaterial = null;
        _selectedLibraryMaterial = null;
        _selectedMeshIndex = null;
        _selectedLight = _project.Lights.FirstOrDefault();
        _selectedSkybox = null;
        _dirty = false;
        _renderer.SetProject(_project);
        cameraPropertyGrid.SelectedObject = new CameraInspector(_project.Camera, CameraEdited);
        RebuildLightList();
        ReloadSkyboxLibrary(showStatus: false);
        RebuildMaterialLibrary();
        RebuildTree();
        UpdateUi();
    }

    private async void AddModel_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "加入 3D 模型",
            Filter = "支援的模型 (*.stl;*.obj;*.3ds;*.glb)|*.stl;*.obj;*.3ds;*.glb|STL (*.stl)|*.stl|Wavefront OBJ (*.obj)|*.obj|3D Studio (*.3ds)|*.3ds|Binary glTF (*.glb)|*.glb"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await ImportModelAsync(dialog.FileName);
    }

    private async Task ImportModelAsync(string filePath)
    {
        SetBusy(true, $"正在載入 {Path.GetFileName(filePath)}...");
        try
        {
            var model = await _importer.ImportAsync(filePath);
            _project.Models.Add(model);
            _selectedModel = model;
            _selectedMaterial = model.Materials.FirstOrDefault();
            _selectedMeshIndex = null;
            _renderer.InvalidateScene();
            _renderer.FrameModel(model);
            MarkDirty();
            RebuildTree();
            SelectTreeObject(model);
            statusLabel.Text = $"已載入 {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "模型載入失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "模型載入失敗";
        }
        finally { SetBusy(false); }
    }

    private async void OpenProject_Click(object? sender, EventArgs e)
    {
        if (!ConfirmDiscardChanges()) return;
        using var dialog = new OpenFileDialog { Filter = "Rv3d Viewer 專案 (*.rv3dproj)|*.rv3dproj" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await LoadProjectAsync(dialog.FileName, promptForMissingAssets: true);
    }

    private async Task<bool> LoadProjectAsync(string projectPath, bool promptForMissingAssets)
    {
        SetBusy(true, "正在開啟專案...");
        BeginProjectProgress("正在讀取專案...", 0);
        try
        {
            var loaded = await _projectStore.LoadAsync(projectPath);
            ReportProjectProgress(5, "正在解析專案資料...");
            ImportLegacyMaterialLibrary(loaded, projectPath);
            var importJobs = new List<(SceneModel SavedModel, string ModelPath)>();
            foreach (var savedModel in loaded.Models)
            {
                if (savedModel.IsProcedural)
                {
                    savedModel.RestoreProceduralGeometry();
                    continue;
                }
                var modelPath = ProjectAssetService.ResolveModelPath(loaded, savedModel);
                if (!File.Exists(modelPath)) continue;
                modelPath = ProjectAssetService.CanonicalizeBundledAssetPath(modelPath);
                savedModel.AssetPath = Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, modelPath);
                importJobs.Add((savedModel, modelPath));
            }

            ReportProjectProgress(10, importJobs.Count == 0 ? "正在還原專案..." : $"正在載入模型 0/{importJobs.Count}...");
            var completedImports = 0;
            IProgress<int> importProgress = new Progress<int>(completed =>
                ReportProjectProgress(10 + completed * 70 / Math.Max(1, importJobs.Count),
                    $"正在載入模型 {completed}/{importJobs.Count}..."));
            using var importGate = new SemaphoreSlim(2);
            var importedModels = await Task.WhenAll(importJobs.Select(async job =>
            {
                await importGate.WaitAsync();
                try
                {
                    var imported = await _importer.ImportAsync(job.ModelPath);
                    importProgress.Report(Interlocked.Increment(ref completedImports));
                    return (job.SavedModel, job.ModelPath, Imported: imported);
                }
                finally
                {
                    importGate.Release();
                }
            }));

            foreach (var result in importedModels)
            {
                var savedModel = result.SavedModel;
                savedModel.SourceFilePath = result.ModelPath;
                if (savedModel.Materials.Count == 0) savedModel.Materials = result.Imported.Materials;
                savedModel.RestoreImportedGeometry(result.Imported);
                foreach (var slot in savedModel.Materials.SelectMany(material => material.Textures.Values))
                {
                    var texturePath = Path.IsPathRooted(slot.Path)
                        ? slot.Path
                        : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, slot.Path));
                    if (!File.Exists(texturePath)) continue;
                    var canonicalTexturePath = ProjectAssetService.CanonicalizeBundledAssetPath(texturePath);
                    slot.Path = Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, canonicalTexturePath);
                }
            }
            ReportProjectProgress(82, "正在處理 HDR 與貼圖路徑...");
            if (!string.IsNullOrWhiteSpace(loaded.Environment.Path))
            {
                var environmentPath = Path.IsPathRooted(loaded.Environment.Path)
                    ? loaded.Environment.Path
                    : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, loaded.Environment.Path));
                if (File.Exists(environmentPath))
                {
                    environmentPath = ProjectAssetService.CanonicalizeBundledAssetPath(environmentPath);
                    loaded.Environment.Path = Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, environmentPath);
                }
            }
            foreach (var skybox in loaded.Skyboxes)
            {
                skybox.PositiveX = ResolveProjectAssetPath(projectPath, skybox.PositiveX);
                skybox.NegativeX = ResolveProjectAssetPath(projectPath, skybox.NegativeX);
                skybox.PositiveY = ResolveProjectAssetPath(projectPath, skybox.PositiveY);
                skybox.NegativeY = ResolveProjectAssetPath(projectPath, skybox.NegativeY);
                skybox.PositiveZ = ResolveProjectAssetPath(projectPath, skybox.PositiveZ);
                skybox.NegativeZ = ResolveProjectAssetPath(projectPath, skybox.NegativeZ);
                skybox.SourcePanorama = ResolveProjectAssetPath(projectPath, skybox.SourcePanorama);
            }
            _project = loaded;
            _selectedModel = _project.Models.FirstOrDefault();
            _selectedMaterial = _selectedModel?.Materials.FirstOrDefault();
            _selectedLibraryMaterial = null;
            _selectedMeshIndex = null;
            _selectedLight = _project.Lights.FirstOrDefault();
            _selectedSkybox = _project.Skyboxes.FirstOrDefault(skybox => skybox.Show) ?? _project.Skyboxes.FirstOrDefault();
            _dirty = false;
            _undoHistory.Clear();
            _undoBaseline = CaptureUndoEntry("編輯場景");
            _renderer.SetProject(_project);
            ReportProjectProgress(90, "正在建立 ViewPort 場景...");
            cameraPropertyGrid.SelectedObject = new CameraInspector(_project.Camera, CameraEdited);
            RebuildLightList();
            RebuildSkyboxList();
            RebuildMaterialLibrary();
            ReportProjectProgress(95, "正在建立模型與材質清單...");
            RebuildTree();
            var missingCount = ProjectAssetService.FindMissingAssets(_project).Count;
            statusLabel.Text = missingCount == 0
                ? $"已開啟 {Path.GetFileName(projectPath)}"
                : $"已開啟 {Path.GetFileName(projectPath)}，缺少 {missingCount} 個資產";
            UpdateUi();
            if (promptForMissingAssets && missingCount > 0 && MessageBox.Show(
                    this,
                    $"專案缺少 {missingCount} 個模型或貼圖資產，是否立即重新指定？",
                    "缺失資產",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) == DialogResult.Yes)
                await RepairMissingAssetsAsync();
            ReportProjectProgress(100, "專案載入完成");
            return true;
        }
        catch (Exception ex)
        {
            if (promptForMissingAssets)
                MessageBox.Show(this, ex.Message, "專案開啟失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                statusLabel.Text = $"無法載入 lastScene：{ex.Message}";
            return false;
        }
        finally
        {
            EndProjectProgress();
            SetBusy(false);
        }
    }

    private void ImportLegacyMaterialLibrary(ViewerProject loaded, string projectPath)
    {
        if (loaded.MaterialLibrary.Count == 0) return;
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath))!;
        foreach (var legacy in loaded.MaterialLibrary)
        {
            var material = legacy.Clone(CreateUniqueMaterialName(legacy.Name, _materialLibrary.Select(item => item.Name)));
            foreach (var slot in material.Textures.Values)
                if (!string.IsNullOrWhiteSpace(slot.Path) && !Path.IsPathRooted(slot.Path))
                    slot.Path = Path.GetFullPath(Path.Combine(projectDirectory, slot.Path));
            _materialLibrary.Add(material);
        }
        loaded.MaterialLibrary.Clear();
    }

    private async void SaveProject_Click(object? sender, EventArgs e) => await SaveProjectAsync(false);
    private async void SaveAsProject_Click(object? sender, EventArgs e) => await SaveProjectAsync(true);

    private async void ExportModel_Click(object? sender, EventArgs e)
    {
        var hasVisibleMesh = _project.Models.Any(model =>
            model.IsVisible && Enumerable.Range(0, model.Meshes.Count).Any(meshIndex =>
                !model.HiddenMeshIndices.Contains(meshIndex) &&
                model.Meshes[meshIndex].Positions.Length > 0 &&
                model.Meshes[meshIndex].Indices.Length > 0));
        if (!hasVisibleMesh)
        {
            MessageBox.Show(this, "場景中沒有可輸出的可見 Mesh。", "輸出模型", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var suggestedName = string.IsNullOrWhiteSpace(_project.ProjectFilePath)
            ? _project.Name
            : Path.GetFileNameWithoutExtension(_project.ProjectFilePath);
        foreach (var invalid in Path.GetInvalidFileNameChars()) suggestedName = suggestedName.Replace(invalid, '_');
        using var dialog = new SaveFileDialog
        {
            Title = "輸出模型",
            Filter = "Wavefront OBJ (*.obj)|*.obj|STL (*.stl)|*.stl|Binary glTF (*.glb)|*.glb",
            FilterIndex = 3,
            DefaultExt = "glb",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"{suggestedName}.glb"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
        if (extension == ".stl" && MessageBox.Show(
                this,
                "STL 格式只能保存幾何與法線，不支援材質、UV 或貼圖。\n若要保留完整外觀，請選擇 OBJ 或 GLB。\n\n仍要輸出 STL 嗎？",
                "STL 格式限制",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;
        if (extension == ".obj")
        {
            var outputDirectory = ObjExporter.GetOutputDirectory(dialog.FileName);
            if (Directory.Exists(outputDirectory) && Directory.EnumerateFileSystemEntries(outputDirectory).Any() &&
                MessageBox.Show(
                    this,
                    $"輸出資料夾已存在：{outputDirectory}\n同名的 OBJ、MTL 與貼圖檔將被覆寫。\n\n確定要繼續嗎？",
                    "覆寫 OBJ 模型",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
        }

        SetBusy(true, $"正在輸出 {extension.TrimStart('.').ToUpperInvariant()} 模型...");
        try
        {
            switch (extension)
            {
                case ".obj":
                    await ObjExporter.ExportAsync(_project, dialog.FileName);
                    break;
                case ".stl":
                    await StlExporter.ExportAsync(_project, dialog.FileName);
                    break;
                case ".glb":
                    await GlbExporter.ExportAsync(_project, dialog.FileName);
                    break;
                default:
                    throw new NotSupportedException($"不支援的輸出格式：{extension}");
            }
            var exportedFilePath = extension == ".obj" ? ObjExporter.GetOutputFilePath(dialog.FileName) : dialog.FileName;
            exportedFilePath = Path.GetFullPath(exportedFilePath);
            var size = new FileInfo(exportedFilePath).Length / 1024d / 1024d;
            statusLabel.Text = $"輸出成功：{exportedFilePath}（{size:N2} MB）";
            MessageBox.Show(
                this,
                $"輸出成功\n\n{exportedFilePath}",
                "輸出模型",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "模型輸出失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "模型輸出失敗";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ExportPreviewImage_Click(object? sender, EventArgs e)
    {
        using var dialog = new PreviewImageExportForm(_renderer, _savedUiSettings.LastPreviewImageOutputPath);
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SavedOutputPath)) return;
        var exportedFilePath = Path.GetFullPath(dialog.SavedOutputPath);
        _savedUiSettings.LastPreviewImageOutputPath = exportedFilePath;
        _ = SaveUiSettingsAsync();
        statusLabel.Text = $"輸出成功：{exportedFilePath}";
        MessageBox.Show(
            this,
            $"輸出成功\n\n{exportedFilePath}",
            "輸出影像",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private async void RepairMissingAssets_Click(object? sender, EventArgs e) => await RepairMissingAssetsAsync();

    private async Task RepairMissingAssetsAsync()
    {
        var missingAssets = ProjectAssetService.FindMissingAssets(_project);
        if (missingAssets.Count == 0)
        {
            statusLabel.Text = "沒有缺失資產";
            return;
        }

        var repaired = 0;
        foreach (var missing in missingAssets)
        {
            using var dialog = new OpenFileDialog
            {
                Title = missing.Kind == ProjectAssetKind.Model
                    ? $"重新指定模型：{missing.Model.Name}"
                    : $"重新指定貼圖：{missing.Material?.Name} / {missing.Semantic}",
                Filter = missing.Kind == ProjectAssetKind.Model
                    ? "支援的模型 (*.stl;*.obj;*.3ds;*.glb)|*.stl;*.obj;*.3ds;*.glb|所有檔案 (*.*)|*.*"
                    : "圖片 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|所有檔案 (*.*)|*.*",
                FileName = Path.GetFileName(missing.ExpectedPath)
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) break;

            if (missing.Kind == ProjectAssetKind.Model)
            {
                SetBusy(true, $"正在重新載入 {missing.Model.Name}...");
                try
                {
                    var imported = await _importer.ImportAsync(dialog.FileName);
                    missing.Model.SourceFilePath = Path.GetFullPath(dialog.FileName);
                    if (missing.Model.Materials.Count == 0) missing.Model.Materials = imported.Materials;
                    missing.Model.RestoreImportedGeometry(imported);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "模型重新載入失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }
                finally
                {
                    SetBusy(false);
                }
            }
            else if (missing.Material is not null && missing.Semantic is { } semantic)
            {
                missing.Material.Textures[semantic].Path = Path.GetFullPath(dialog.FileName);
            }
            repaired++;
        }

        if (repaired == 0) return;
        _renderer.InvalidateScene();
        RebuildTree();
        MarkDirty();
        var remaining = ProjectAssetService.FindMissingAssets(_project).Count;
        statusLabel.Text = remaining == 0
            ? $"已修復 {repaired} 個資產"
            : $"已修復 {repaired} 個資產，仍缺少 {remaining} 個";
    }

    private async Task<bool> SaveProjectAsync(bool saveAs)
    {
        var path = _project.ProjectFilePath;
        if (saveAs || string.IsNullOrWhiteSpace(path))
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Rv3d Viewer 專案 (*.rv3dproj)|*.rv3dproj",
                DefaultExt = "rv3dproj",
                AddExtension = true,
                FileName = _project.Name == "Untitled" ? "Scene.rv3dproj" : $"{_project.Name}.rv3dproj"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return false;
            path = dialog.FileName;
        }
        path = ProjectAssetService.GetBundledProjectPath(path!);
        path = Path.GetFullPath(path);
        SetBusy(true, "正在儲存專案...");
        BeginProjectProgress("正在準備專案資料夾...", 0);
        _renderer.SetRenderingPaused(true);
        try
        {
            var saveProgress = new Progress<int>(value =>
                ReportProjectProgress(value, value < 10
                    ? "正在準備專案資料夾..."
                    : "正在複製模型、貼圖與 HDR..."));
            await Task.Run(() => ProjectAssetService.PrepareForSave(_project, path!, saveProgress));
            ReportProjectProgress(90, "正在寫入專案檔案...");
            await _projectStore.SaveAsync(_project, path!);
            ReportProjectProgress(100, "專案儲存完成");
            _dirty = false;
            statusLabel.Text = saveAs
                ? $"儲存成功：{path}"
                : $"已儲存 {Path.GetFileName(path)}";
            UpdateTitle();
            if (saveAs)
            {
                MessageBox.Show(
                    this,
                    $"儲存成功\n\n{path}",
                    "另存新檔",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "專案儲存失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            _renderer.SetRenderingPaused(false);
            EndProjectProgress();
            SetBusy(false);
        }
    }

    private void RemoveModel_Click(object? sender, EventArgs e)
    {
        if (_selectedModel is null) return;
        if (MessageBox.Show(
                this,
                $"確定要從場景刪除模型「{_selectedModel.Name}」嗎？",
                "確認刪除模型",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        PushUndoState($"刪除模型「{_selectedModel.Name}」");
        _project.Models.Remove(_selectedModel);
        _selectedModel = _project.Models.FirstOrDefault();
        _selectedMaterial = _selectedModel?.Materials.FirstOrDefault();
        _selectedMeshIndex = null;
        _renderer.InvalidateScene();
        MarkDirty();
        RebuildTree();
    }

    private void FrameSelected_Click(object? sender, EventArgs e)
    {
        if (_selectedModel is null) return;
        if (_selectedMeshIndex is int meshIndex)
            _renderer.FrameMesh(_selectedModel, meshIndex);
        else
            _renderer.FrameModel(_selectedModel);
    }

    private void CameraView_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripItem { Tag: CameraView view }) return;
        CameraController.SetView(_project.Camera, view);
        cameraPropertyGrid.Refresh();
        CameraEdited();
    }

    private void ModelDisplayMode_Click(object? sender, EventArgs e)
    {
        var mode = ReferenceEquals(sender, modelPointsMenuItem) ? ModelDisplayMode.Points
            : ReferenceEquals(sender, modelWireframeMenuItem) ? ModelDisplayMode.Wireframe
            : ModelDisplayMode.Solid;
        SetModelDisplayMode(mode, markDirty: true);
    }

    private ModelDisplayMode GetProjectModelDisplayMode()
    {
        var mode = _project.RenderSettings.ModelDisplayMode;
        return mode == ModelDisplayMode.Solid && _project.RenderSettings.Wireframe
            ? ModelDisplayMode.Wireframe
            : mode;
    }

    private void SetModelDisplayMode(ModelDisplayMode mode, bool markDirty)
    {
        if (!Enum.IsDefined(mode)) mode = ModelDisplayMode.Solid;
        _project.RenderSettings.ModelDisplayMode = mode;
        _project.RenderSettings.Wireframe = mode == ModelDisplayMode.Wireframe;
        _renderer.InvalidateScene();
        if (markDirty) MarkDirty();
        SyncViewOptions();
    }

    private void Grid_Click(object? sender, EventArgs e)
    {
        _project.RenderSettings.ShowGrid = gridMenuItem.Checked;
        MarkDirty();
        SyncViewOptions();
    }

    private void TexturesMenuItem_Click(object? sender, EventArgs e)
    {
        _project.RenderSettings.ShowTextures = texturesMenuItem.Checked;
        _renderer.InvalidateScene();
        MarkDirty();
    }

    private void PreviewModeMenuItem_Click(object? sender, EventArgs e)
    {
        var mode = ReferenceEquals(sender, previewMode1MenuItem) ? ViewportPreviewMode.Preview1
            : ReferenceEquals(sender, previewMode2MenuItem) ? ViewportPreviewMode.Preview2
            : ReferenceEquals(sender, previewMode3MenuItem) ? ViewportPreviewMode.Preview3
            : ReferenceEquals(sender, previewMode4MenuItem) ? ViewportPreviewMode.Preview4
            : ViewportPreviewMode.Preview5;
        ApplyPreviewMode(mode);
    }

    private void WorldAxesMenuItem_Click(object? sender, EventArgs e)
    {
        _project.RenderSettings.ShowWorldAxes = worldAxesMenuItem.Checked;
        MarkDirty();
        SyncViewOptions();
    }

    private void CameraGizmoMenuItem_Click(object? sender, EventArgs e)
    {
        _project.RenderSettings.ShowCameraGizmo = cameraGizmoMenuItem.Checked;
        _renderer.InvalidateScene();
        MarkDirty();
        SyncViewOptions();
    }

    private void LightGizmosMenuItem_Click(object? sender, EventArgs e)
    {
        _project.RenderSettings.ShowLightGizmos = lightGizmosMenuItem.Checked;
        _renderer.InvalidateScene();
        MarkDirty();
        SyncViewOptions();
    }

    private void InputInfoMenuItem_Click(object? sender, EventArgs e)
    {
        _renderer.SetInputOverlayEnabled(inputInfoMenuItem.Checked);
        SyncViewOptions();
    }

    private void InputOverlaySettingsMenuItem_Click(object? sender, EventArgs e) =>
        InputOverlayPreferences.Edit(this);

    private void ViewportColorSettings_Click(object? sender, EventArgs e)
    {
        using var dialog = new ViewportColorSettingsForm(
            ViewportColorPreferences.MainFormDefinitions,
            "MainForm－ViewPort 顏色設定");
        dialog.ShowDialog(this);
    }

    private void InputOverlayPreferences_Changed(object? sender, EventArgs e) => ApplyInputOverlayStyle();

    private void ViewportColorPreferences_Changed(object? sender, EventArgs e)
    {
        ApplyViewportColors();
        ApplyInputOverlayStyle();
    }

    private void ApplyInputOverlayStyle()
    {
        var style = InputOverlayPreferences.Current;
        _renderer.SetInputOverlayStyle(ViewportColorPreferences.Get("OverlayText"), style.FontSize);
    }

    private void ApplyViewportColors() => _renderer.SetEditorColors(new ViewportEditorColors(
        ViewportColorPreferences.Get("StudioBackground"),
        ViewportColorPreferences.Get("GridMinor"),
        ViewportColorPreferences.Get("GridMajor"),
        ViewportColorPreferences.Get("AxisX"),
        ViewportColorPreferences.Get("AxisY"),
        ViewportColorPreferences.Get("AxisZ"),
        ViewportColorPreferences.Get("Selection"),
        ViewportColorPreferences.Get("CameraGizmo"),
        ViewportColorPreferences.Get("DisabledLight"),
        ViewportColorPreferences.Get("LightHighlight"),
        ViewportColorPreferences.Get("OverlayText"),
        ViewportColorPreferences.Get("OverlayBackground")));

    private void SelectionHighlight_Click(object? sender, EventArgs e)
    {
        _project.RenderSettings.ShowSelectionHighlight = selectionHighlightMenuItem.Checked;
        MarkDirty();
        SyncViewOptions();
    }

    private void DarkModeMenuItem_CheckedChanged(object? sender, EventArgs e)
    {
        FIsDarkMode = darkModeMenuItem.Checked;
        ApplyInterfaceTheme();
        SyncViewOptions();
    }

    private void PreviewModeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_syncingViewOptions) return;
        if (previewModeComboBox.SelectedIndex >= 0)
            ApplyPreviewMode((ViewportPreviewMode)(previewModeComboBox.SelectedIndex + 1));
    }

    private void ApplyPreviewMode(ViewportPreviewMode mode)
    {
        _previewMode = mode;
        _renderer.SetPreviewMode(mode);
        statusLabel.Text = mode switch
        {
            ViewportPreviewMode.Preview1 => "預覽模式 1：快速近似繪製",
            ViewportPreviewMode.Preview2 => "預覽模式 2：原完整預覽",
            ViewportPreviewMode.Preview3 => "預覽模式 3：中性攝影棚照明與柔和色調映射",
            ViewportPreviewMode.Preview4 => "預覽模式 4：珠寶高反射、折射與色散預覽",
            _ => "預覽模式 5：進階珠寶反射、切面折射與柔和火彩"
        };
        SyncViewOptions();
        _pluginContext?.NotifyPreviewModeChanged();
    }

    private void ShowGridCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_syncingViewOptions) return;
        gridMenuItem.Checked = showGridCheckBox.Checked;
        Grid_Click(sender, e);
    }

    private void ShowWorldAxesCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_syncingViewOptions) return;
        worldAxesMenuItem.Checked = showWorldAxesCheckBox.Checked;
        WorldAxesMenuItem_Click(sender, e);
    }

    private void ShowCameraGizmoCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_syncingViewOptions) return;
        cameraGizmoMenuItem.Checked = showCameraGizmoCheckBox.Checked;
        CameraGizmoMenuItem_Click(sender, e);
    }

    private void ShowSelectionHighlightCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_syncingViewOptions) return;
        selectionHighlightMenuItem.Checked = showSelectionHighlightCheckBox.Checked;
        SelectionHighlight_Click(sender, e);
    }

    private void WireframeCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_syncingViewOptions) return;
        SetModelDisplayMode(wireframeCheckBox.Checked ? ModelDisplayMode.Wireframe : ModelDisplayMode.Solid,
            markDirty: true);
    }

    private void ShowInputInfoCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_syncingViewOptions) return;
        inputInfoMenuItem.Checked = showInputInfoCheckBox.Checked;
        InputInfoMenuItem_Click(sender, e);
    }

    private void DarkModeCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_syncingViewOptions) return;
        darkModeMenuItem.Checked = darkModeCheckBox.Checked;
    }

    private void ToggleAllViewGroupsCheckBox_CheckStateChanged(object? sender, EventArgs e)
    {
        if (_syncingViewGroupExpansion || toggleAllViewGroupsCheckBox.CheckState == CheckState.Indeterminate)
            return;
        _syncingViewGroupExpansion = true;
        try
        {
            var expanded = toggleAllViewGroupsCheckBox.Checked;
            viewDisplayGroup.Expanded = expanded;
            viewColorGroup.Expanded = expanded;
        }
        finally
        {
            _syncingViewGroupExpansion = false;
        }
    }

    private void ViewGroup_ExpandedChanged(object? sender, EventArgs e)
    {
        ResizeViewOptionGroups();
        if (_syncingViewGroupExpansion) return;
        _syncingViewGroupExpansion = true;
        try
        {
            toggleAllViewGroupsCheckBox.CheckState = viewDisplayGroup.Expanded && viewColorGroup.Expanded
                ? CheckState.Checked
                : !viewDisplayGroup.Expanded && !viewColorGroup.Expanded
                    ? CheckState.Unchecked
                    : CheckState.Indeterminate;
        }
        finally
        {
            _syncingViewGroupExpansion = false;
        }
    }

    private void ViewOptionsPanel_ClientSizeChanged(object? sender, EventArgs e) =>
        ResizeViewOptionGroups();

    private void ResizeViewOptionGroups()
    {
        var availableWidth = viewOptionsPanel.ClientSize.Width
            - viewOptionsPanel.Padding.Left
            - viewOptionsPanel.Padding.Right
            - (viewOptionsPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0);
        availableWidth = Math.Max(1, availableWidth);
        if (viewDisplayGroup.Width != availableWidth)
            viewDisplayGroup.Width = availableWidth;
        if (viewColorGroup.Width != availableWidth)
            viewColorGroup.Width = availableWidth;
    }

    private void SyncViewOptions()
    {
        _syncingViewOptions = true;
        try
        {
            previewMode1MenuItem.Checked = _previewMode == ViewportPreviewMode.Preview1;
            previewMode2MenuItem.Checked = _previewMode == ViewportPreviewMode.Preview2;
            previewMode3MenuItem.Checked = _previewMode == ViewportPreviewMode.Preview3;
            previewMode4MenuItem.Checked = _previewMode == ViewportPreviewMode.Preview4;
            previewMode5MenuItem.Checked = _previewMode == ViewportPreviewMode.Preview5;
            previewModeComboBox.SelectedIndex = (int)_previewMode - 1;
            showGridCheckBox.Checked = gridMenuItem.Checked;
            showWorldAxesCheckBox.Checked = worldAxesMenuItem.Checked;
            showCameraGizmoCheckBox.Checked = cameraGizmoMenuItem.Checked;
            showLightGizmosCheckBox.Checked = lightGizmosMenuItem.Checked;
            showInputInfoCheckBox.Checked = inputInfoMenuItem.Checked;
            showSelectionHighlightCheckBox.Checked = selectionHighlightMenuItem.Checked;
            var modelMode = GetProjectModelDisplayMode();
            modelPointsMenuItem.Checked = modelMode == ModelDisplayMode.Points;
            modelWireframeMenuItem.Checked = modelMode == ModelDisplayMode.Wireframe;
            modelSolidMenuItem.Checked = modelMode == ModelDisplayMode.Solid;
            wireframeCheckBox.Checked = modelMode == ModelDisplayMode.Wireframe;
            darkModeCheckBox.Checked = darkModeMenuItem.Checked;
        }
        finally
        {
            _syncingViewOptions = false;
        }
    }

    private void ApplyInterfaceTheme()
    {
        var palette = FIsDarkMode ? ThemePalette.Dark : ThemePalette.Light;
        SuspendLayout();
        try
        {
            BackColor = palette.Window;
            ForeColor = palette.Text;
            ApplyControlTheme(this, palette);

            // ToolStripProfessionalRenderer can ignore an inherited ToolStrip.ForeColor
            // when Windows changes the application theme.  Force the palette at render
            // time so toolbar button text never falls back to black in dark mode.
            var renderer = new ViewerToolStripRenderer(palette);
            foreach (var strip in new ToolStrip[] { mainMenuStrip, mainToolStrip, mainStatusStrip, _materialNodeContextMenu })
            {
                strip.Renderer = renderer;
                strip.BackColor = palette.Control;
                strip.ForeColor = palette.Text;
                ApplyToolStripItemsTheme(strip.Items, palette, renderer);
            }

            inspectorTabControl.Invalidate();
            sceneTreeView.Refresh();
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    private static void ApplyToolStripItemsTheme(
        ToolStripItemCollection items,
        ThemePalette palette,
        ToolStripRenderer renderer)
    {
        foreach (ToolStripItem item in items)
        {
            item.ForeColor = palette.Text;
            item.BackColor = item.Owner is ToolStripDropDown ? palette.Window : palette.Control;
            if (item is not ToolStripMenuItem menuItem) continue;

            menuItem.DropDown.Renderer = renderer;
            menuItem.DropDown.BackColor = palette.Window;
            menuItem.DropDown.ForeColor = palette.Text;
            ApplyToolStripItemsTheme(menuItem.DropDownItems, palette, renderer);
        }
    }

    private static void ApplyControlTheme(Control parent, ThemePalette palette)
    {
        foreach (Control control in parent.Controls)
        {
            switch (control)
            {
                case PropertyGrid grid:
                    grid.BackColor = palette.Window;
                    grid.ViewBackColor = palette.Window;
                    grid.ViewForeColor = palette.Text;
                    grid.LineColor = palette.Border;
                    grid.CategoryForeColor = palette.Text;
                    grid.CommandsBackColor = palette.Control;
                    grid.CommandsForeColor = palette.Text;
                    grid.HelpBackColor = palette.Control;
                    grid.HelpForeColor = palette.Text;
                    break;
                case TreeView tree:
                    tree.BackColor = palette.Window;
                    tree.ForeColor = palette.Text;
                    tree.LineColor = palette.Border;
                    break;
                case CheckedListBox checkedList:
                    checkedList.BackColor = palette.Window;
                    checkedList.ForeColor = palette.Text;
                    break;
                case ListBox list:
                    list.BackColor = palette.Window;
                    list.ForeColor = palette.Text;
                    break;
                case Button button:
                    button.UseVisualStyleBackColor = false;
                    button.BackColor = palette.Button;
                    button.ForeColor = palette.Text;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = palette.Border;
                    break;
                case ComboBox combo:
                    combo.BackColor = palette.Window;
                    combo.ForeColor = palette.Text;
                    break;
                case TabPage page:
                    page.BackColor = palette.Window;
                    page.ForeColor = palette.Text;
                    break;
                case SplitContainer split:
                    split.BackColor = palette.Border;
                    break;
                case Panel or FlowLayoutPanel:
                    control.BackColor = palette.Control;
                    control.ForeColor = palette.Text;
                    break;
                default:
                    if (control is not OpenTK.GLControl.GLControl)
                    {
                        control.BackColor = palette.Control;
                        control.ForeColor = palette.Text;
                    }
                    break;
            }
            ApplyControlTheme(control, palette);
        }
    }

    private void EditMode_Click(object? sender, EventArgs e)
    {
        var mode = sender switch
        {
            _ when ReferenceEquals(sender, exitEditModeMenuItem) => ViewportEditMode.Navigate,
            _ when ReferenceEquals(sender, selectModeMenuItem) => ViewportEditMode.Select,
            _ when ReferenceEquals(sender, selectMeshModeMenuItem) => ViewportEditMode.SelectMesh,
            _ when ReferenceEquals(sender, subtractModeMenuItem) => ViewportEditMode.Subtract,
            _ => (ViewportEditMode?)null
        };
        if (mode is not null)
            SetEditMode(mode.Value);
    }

    private void SelectionFunctionsMenuItem_DropDownOpening(object? sender, EventArgs e) =>
        SetEditHint("選取功能｜選取模式下必須按住 Ctrl，再點擊物件或拖曳框選");

    private void ModelEditMenuItem_DropDownOpening(object? sender, EventArgs e) =>
        SetEditHint("模型工具｜管理模型材質及未使用資料");

    private void SelectAll_Click(object? sender, EventArgs e)
    {
        _renderer.SelectAll();
        SetEditHint("全部選取｜已選取目前目標中的所有可見模型／Mesh｜隱藏項目不會被選取");
    }

    private void ClearSelection_Click(object? sender, EventArgs e)
    {
        _renderer.ClearBoxSelection();
        SetEditHint("全部取消選取｜已清除左側反白與 ViewPort 選取線框");
    }

    private void SetEditMode(ViewportEditMode mode)
    {
        _editMode = mode;
        _renderer.SetEditMode(mode);
        UpdateEditModeChecks();
        statusLabel.Text = mode switch
        {
            ViewportEditMode.Navigate => "已退出編輯：左鍵拖曳旋轉視角",
            ViewportEditMode.Select => "選取模型：Ctrl + 左鍵框選；左鍵拖曳旋轉視角",
            ViewportEditMode.SelectMesh => "選取 Mesh：Ctrl + 左鍵框選；左鍵拖曳旋轉視角",
            ViewportEditMode.Subtract => "減選模式：Ctrl + 左鍵框選減選；左鍵拖曳旋轉視角",
            _ => statusLabel.Text
        };
        SetEditHint(mode switch
        {
            ViewportEditMode.Navigate => NavigateHint,
            ViewportEditMode.Select => SelectModelHint,
            ViewportEditMode.SelectMesh => SelectMeshHint,
            ViewportEditMode.Subtract => SubtractHint,
            _ => NavigateHint
        });
    }

    private void SetEditHint(string text) => editHintLabel.Text = text;

    private void FullScreen_Click(object? sender, EventArgs e) => ToggleFullScreen();

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        _renderer.ShowInputActivity(FormatKeyInput(e.KeyData));
        if (e.KeyCode == Keys.F11)
        {
            ToggleFullScreen();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            HandleEscape();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if ((keyData & Keys.KeyCode) != Keys.Escape)
            return base.ProcessCmdKey(ref msg, keyData);

        HandleEscape();
        return true;
    }

    private void HandleEscape()
    {
        SetEditMode(ViewportEditMode.Navigate);
        _renderer.ClearBoxSelection();
        ClearAppliedMaterialHighlight();
        _selectedModel = null;
        _selectedMaterial = null;
        _selectedMeshIndex = null;
        sceneTreeView.SelectedNode = null;
        BindInspectors();
        UpdateUi();
        if (_isFullScreen) ExitFullScreen();
        SetEditHint("已退出編輯並取消選取｜左鍵拖曳可旋轉視角");
    }

    private void ToggleFullScreen()
    {
        if (_isFullScreen) ExitFullScreen(); else EnterFullScreen();
    }

    private void EnterFullScreen()
    {
        if (_isFullScreen) return;
        _restoreWindowState = WindowState;
        _restoreBorderStyle = FormBorderStyle;
        _restoreBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _isFullScreen = true;

        SuspendLayout();
        mainMenuStrip.Visible = false;
        mainToolStrip.Visible = false;
        editHintPanel.Visible = false;
        mainStatusStrip.Visible = false;
        rootSplitContainer.Panel1Collapsed = true;
        workSplitContainer.Panel2Collapsed = true;
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        Bounds = Screen.FromControl(this).Bounds;
        ResumeLayout(true);
        fullScreenMenuItem.Checked = true;
    }

    private void ExitFullScreen()
    {
        if (!_isFullScreen) return;
        _isFullScreen = false;

        SuspendLayout();
        FormBorderStyle = _restoreBorderStyle;
        Bounds = _restoreBounds;
        WindowState = _restoreWindowState;
        rootSplitContainer.Panel1Collapsed = false;
        workSplitContainer.Panel2Collapsed = false;
        mainMenuStrip.Visible = true;
        mainToolStrip.Visible = true;
        editHintPanel.Visible = true;
        mainStatusStrip.Visible = true;
        ResumeLayout(true);
        fullScreenMenuItem.Checked = false;
    }

    private void DeleteSelection_Click(object? sender, EventArgs e)
    {
        SetEditHint("刪除選取｜刪除目前反白選取的模型或 Mesh｜執行前會再次詢問");
        var selectedModels = _renderer.HighlightedModels.ToArray();
        var selectedMeshes = _renderer.HighlightedMeshes.ToArray();
        if (selectedModels.Length == 0 && selectedMeshes.Length == 0)
        {
            statusLabel.Text = "目前沒有高亮選取的模型或 Mesh";
            return;
        }

        var deletingMeshes = selectedMeshes.Length > 0;
        var names = deletingMeshes
            ? string.Join("、", selectedMeshes.Take(8).Select(selection =>
                $"{selection.Model.Name} / {selection.Model.Meshes[selection.MeshIndex].Name}"))
            : string.Join("、", selectedModels.Take(8).Select(model => model.Name));
        var selectedCount = deletingMeshes ? selectedMeshes.Length : selectedModels.Length;
        if (selectedCount > 8) names += $" 等 {selectedCount} 個項目";
        var answer = MessageBox.Show(
            this,
            $"確定要從場景刪除以下選取{(deletingMeshes ? " Mesh" : "模型")}嗎？\n\n{names}\n\n原始模型檔案不會被刪除。",
            "確認刪除選取",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        PushUndoState(deletingMeshes
            ? $"刪除 {selectedCount:N0} 個 Mesh"
            : $"刪除 {selectedCount:N0} 個模型");
        if (deletingMeshes)
        {
            foreach (var group in selectedMeshes.GroupBy(selection => selection.Model))
                group.Key.RemoveMeshes(group.Select(selection => selection.MeshIndex));
        }
        else
        {
            foreach (var model in selectedModels)
                _project.Models.Remove(model);
        }
        _renderer.ClearBoxSelection();
        _selectedModel = _project.Models.FirstOrDefault();
        _selectedMaterial = _selectedModel?.Materials.FirstOrDefault();
        _selectedMeshIndex = null;
        _renderer.InvalidateScene();
        MarkDirty();
        RebuildTree();
        if (_selectedModel is not null) SelectTreeObject(_selectedModel);
        statusLabel.Text = $"已從場景刪除 {selectedCount:N0} 個{(deletingMeshes ? " Mesh" : "模型")}";
    }

    private void TextureSemantic_Click(object? sender, EventArgs e)
    {
        var material = _selectedLibraryMaterial ?? _selectedMaterial;
        if (material is null || sender is not ToolStripItem { Tag: TextureSemantic semantic }) return;
        using var dialog = new OpenFileDialog
        {
            Title = $"選擇 {semantic} 貼圖",
            Filter = "圖片 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|所有檔案 (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        material.Textures[semantic] = new TextureSlot { Path = dialog.FileName };
        materialPropertyGrid.Refresh();
        if (_selectedLibraryMaterial is null) SceneChanged(); else MaterialLibraryChanged();
    }

    private void MaterialLibraryListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_rebuildingMaterialLibrary) return;
        _selectedLibraryMaterial = (materialLibraryListBox.SelectedItem as MaterialLibraryItem)?.Material;
        BindInspectors();
        UpdateUi();
    }

    private void AddLibraryMaterial_Click(object? sender, EventArgs e)
    {
        var source = _selectedLibraryMaterial ?? _selectedMaterial ?? new PbrMaterial();
        var name = CreateUniqueMaterialName($"{source.Name} 副本", _materialLibrary.Select(material => material.Name));
        var material = source.Clone(name);
        _materialLibrary.Add(material);
        _selectedLibraryMaterial = material;
        RebuildMaterialLibrary(material);
        materialPropertyGrid.Focus();
        statusLabel.Text = $"已新增資料庫材質：{material.Name}";
    }

    private void UpdateLibraryMaterial_Click(object? sender, EventArgs e)
    {
        if (_selectedLibraryMaterial is null || _selectedMaterial is null) return;
        var index = _materialLibrary.IndexOf(_selectedLibraryMaterial);
        if (index < 0) return;
        var updated = _selectedMaterial.Clone(_selectedLibraryMaterial.Name);
        _materialLibrary[index] = updated;
        _selectedLibraryMaterial = updated;
        RebuildMaterialLibrary(updated);
        BindInspectors();
        statusLabel.Text = $"已更新資料庫材質：{updated.Name}";
    }

    private void DeleteLibraryMaterial_Click(object? sender, EventArgs e)
    {
        if (_selectedLibraryMaterial is null) return;
        if (MessageBox.Show(
                this,
                $"確定要從材質資料庫刪除「{_selectedLibraryMaterial.Name}」嗎？\n已套用到 Mesh 的材質不會受影響。",
                "刪除材質",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        var deletedName = _selectedLibraryMaterial.Name;
        _materialLibrary.Remove(_selectedLibraryMaterial);
        _selectedLibraryMaterial = null;
        RebuildMaterialLibrary();
        BindInspectors();
        statusLabel.Text = $"已刪除資料庫材質：{deletedName}";
    }

    private void DeleteUnusedMaterials_Click(object? sender, EventArgs e)
    {
        SetEditHint("刪除未使用材質｜只刪除目前模型中沒有任何 Mesh 使用的材質｜使用中的材質不受影響");
        var removed = 0;
        foreach (var model in _project.Models)
        {
            var used = model.Meshes.Select(mesh => mesh.MaterialIndex).Where(index => index >= 0).ToHashSet();
            var oldMaterials = model.Materials;
            var newMaterials = new List<PbrMaterial>();
            var newIndexByOldIndex = new Dictionary<int, int>();
            for (var oldIndex = 0; oldIndex < oldMaterials.Count; oldIndex++)
            {
                if (!used.Contains(oldIndex)) continue;
                newIndexByOldIndex[oldIndex] = newMaterials.Count;
                newMaterials.Add(oldMaterials[oldIndex]);
            }
            foreach (var mesh in model.Meshes)
                mesh.MaterialIndex = newIndexByOldIndex.GetValueOrDefault(mesh.MaterialIndex, 0);
            removed += oldMaterials.Count - newMaterials.Count;
            model.Materials = newMaterials;
        }
        if (removed == 0)
        {
            statusLabel.Text = "沒有未使用的材質。";
            return;
        }
        _selectedMaterial = _selectedModel?.Materials.FirstOrDefault();
        _renderer.InvalidateScene();
        RebuildTree();
        BindInspectors();
        MarkDirty();
        statusLabel.Text = $"已刪除 {removed:N0} 個未使用的場景材質。";
    }

    private async void LoadMaterialLibrary_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "材質資料庫 (*.mtllib)|*.mtllib", DefaultExt = "mtllib" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _materialLibrary = await _materialLibraryStore.LoadAsync(dialog.FileName);
            _selectedLibraryMaterial = null;
            RebuildMaterialLibrary();
            BindInspectors();
            statusLabel.Text = $"已讀取材質資料庫：{Path.GetFileName(dialog.FileName)}（{_materialLibrary.Count:N0} 筆）";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "讀取材質資料庫失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void SaveMaterialLibrary_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "材質資料庫 (*.mtllib)|*.mtllib",
            DefaultExt = "mtllib",
            AddExtension = true,
            FileName = "materials.mtllib"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _materialLibraryStore.SaveAsync(_materialLibrary, dialog.FileName);
            statusLabel.Text = $"已儲存材質資料庫：{Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "儲存材質資料庫失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ImportProjectMaterials_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "從專案加入材質",
            Filter = "Rv3d Viewer 專案 (*.rv3dproj;*.rv3dprj)|*.rv3dproj;*.rv3dprj|所有檔案 (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        SetBusy(true, $"正在從 {Path.GetFileName(dialog.FileName)} 讀取材質...");
        try
        {
            var sourceProject = await _projectStore.LoadAsync(dialog.FileName);
            var sourceMaterials = sourceProject.Models.SelectMany(model => model.Materials)
                .Concat(sourceProject.MaterialLibrary ?? [])
                .ToList();
            if (sourceMaterials.Count == 0)
            {
                MessageBox.Show(this, "選取的專案沒有可加入的材質。", "加入材質",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                statusLabel.Text = "選取的專案沒有材質";
                return;
            }

            var clearChoice = MessageBox.Show(this,
                $"已找到 {sourceMaterials.Count:N0} 個材質。\n\n是否先清除目前的材質清單？\n\n「是」：清除後加入\n「否」：保留並附加\n「取消」：停止加入",
                "加入專案材質", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (clearChoice == DialogResult.Cancel)
            {
                statusLabel.Text = "已取消加入材質";
                return;
            }

            var clearExisting = clearChoice == DialogResult.Yes;
            var names = (clearExisting ? Enumerable.Empty<string>() : _materialLibrary.Select(material => material.Name))
                .ToList();
            var imported = new List<PbrMaterial>(sourceMaterials.Count);
            var renamedCount = 0;
            var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(dialog.FileName))!;
            foreach (var source in sourceMaterials)
            {
                var uniqueName = CreateUniqueMaterialName(source.Name, names);
                if (!string.Equals(uniqueName, source.Name, StringComparison.Ordinal)) renamedCount++;
                var clone = source.Clone(uniqueName);
                ResolveImportedMaterialTexturePaths(clone, sourceDirectory);
                imported.Add(clone);
                names.Add(uniqueName);
            }

            if (clearExisting) _materialLibrary.Clear();
            _materialLibrary.AddRange(imported);
            _selectedLibraryMaterial = imported[0];
            RebuildMaterialLibrary(_selectedLibraryMaterial);
            BindInspectors();
            statusLabel.Text = $"已從 {Path.GetFileName(dialog.FileName)} 加入 {imported.Count:N0} 個材質，其中 {renamedCount:N0} 個已重新命名";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"無法從專案加入材質：\n{ex.Message}", "加入材質",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "加入專案材質失敗";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static void ResolveImportedMaterialTexturePaths(PbrMaterial material, string sourceDirectory)
    {
        foreach (var slot in material.Textures.Values)
            slot.Path = ResolveImportedTexturePath(slot.Path, sourceDirectory);
        foreach (var layer in material.TextureStacks.Values.SelectMany(stack => stack.Layers))
            layer.Path = ResolveImportedTexturePath(layer.Path, sourceDirectory);
    }

    private static string ResolveImportedTexturePath(string path, string sourceDirectory)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)) return path;
        return Path.GetFullPath(Path.Combine(sourceDirectory, path));
    }

    private void ApplyLibraryMaterial_Click(object? sender, EventArgs e)
    {
        if (_selectedLibraryMaterial is null) return;
        var selections = GetSelectedMeshes();
        if (selections.Count == 0)
        {
            MessageBox.Show(this, "請先選取至少一個 Mesh。", "套用材質", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ConfirmApplyMaterial(_selectedLibraryMaterial.Name, selections.Count)) return;

        PbrMaterial? lastAppliedMaterial = null;
        foreach (var group in selections.GroupBy(selection => selection.Model))
        {
            var model = group.Key;
            var name = CreateUniqueMaterialName(_selectedLibraryMaterial.Name, model.Materials.Select(material => material.Name));
            var appliedMaterial = _selectedLibraryMaterial.Clone(name);
            model.Materials.Add(appliedMaterial);
            var materialIndex = model.Materials.Count - 1;
            foreach (var selection in group)
                if ((uint)selection.MeshIndex < (uint)model.Meshes.Count)
                    model.Meshes[selection.MeshIndex].MaterialIndex = materialIndex;
            lastAppliedMaterial = appliedMaterial;
        }

        _selectedMaterial = lastAppliedMaterial;
        _renderer.InvalidateScene();
        RebuildTree();
        RestoreMeshSelectionAfterTreeRebuild(selections);
        MarkDirty();
        statusLabel.Text = $"已將「{_selectedLibraryMaterial.Name}」套用到 {selections.Count:N0} 個 Mesh";
    }

    private bool ConfirmApplyMaterial(string materialName, int meshCount) =>
        MessageBox.Show(this,
            $"是否要將材質「{materialName}」套用到選取的 {meshCount:N0} 個 Mesh？",
            "確認套用材質", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    private void RebuildMaterialLibrary(PbrMaterial? select = null)
    {
        _rebuildingMaterialLibrary = true;
        try
        {
            materialLibraryListBox.Items.Clear();
            foreach (var material in _materialLibrary)
                materialLibraryListBox.Items.Add(new MaterialLibraryItem(material));
            var target = select ?? _selectedLibraryMaterial;
            if (target is not null)
                materialLibraryListBox.SelectedIndex = _materialLibrary.FindIndex(material => ReferenceEquals(material, target));
        }
        finally
        {
            _rebuildingMaterialLibrary = false;
        }
        UpdateUi();
    }

    private void ClearMaterialLibrarySelection()
    {
        if (_selectedLibraryMaterial is null && materialLibraryListBox.SelectedIndex < 0) return;
        _rebuildingMaterialLibrary = true;
        try
        {
            materialLibraryListBox.ClearSelected();
            _selectedLibraryMaterial = null;
        }
        finally
        {
            _rebuildingMaterialLibrary = false;
        }
    }

    private void MaterialLibraryChanged()
    {
        if (_selectedLibraryMaterial is null) return;
        _selectedLibraryMaterial.Validate();
        var selected = _selectedLibraryMaterial;
        RebuildMaterialLibrary(selected);
        materialPropertyGrid.Refresh();
        RefreshMaterialPreview();
    }

    private static string CreateUniqueMaterialName(string requestedName, IEnumerable<string> existingNames)
    {
        var names = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(requestedName)) return requestedName;
        for (var number = 2; ; number++)
        {
            var candidate = $"{requestedName} ({number})";
            if (!names.Contains(candidate)) return candidate;
        }
    }

    private void LightListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_rebuildingLights) return;
        _selectedLight = lightListBox.SelectedItem as SceneLight;
        _renderer.SetHighlightedLight(_selectedLight);
        var lightInspector = _selectedLight is null ? null : new LightInspector(_selectedLight, LightChanged);
        if (lightInspector is not null) TypeDescriptor.Refresh(lightInspector);
        lightPropertyGrid.SelectedObject = lightInspector;
        SyncSelectedLightType();
        UpdateUi();
    }

    private void LightTypeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_rebuildingLights || _selectedLight is null || lightTypeComboBox.SelectedIndex < 0) return;
        var type = (SceneLightType)lightTypeComboBox.SelectedIndex;
        if (_selectedLight.Type == type) return;

        _selectedLight.Type = type;
        _selectedLight.Validate();
        lightPropertyGrid.Refresh();
        _renderer.InvalidateScene();
        MarkDirty();
        statusLabel.Text = $"燈光「{_selectedLight.Name}」已切換為 {type}。";
    }

    private void SyncSelectedLightType()
    {
        var selectedIndex = _selectedLight is null ? -1 : (int)_selectedLight.Type;
        if (lightTypeComboBox.SelectedIndex != selectedIndex)
            lightTypeComboBox.SelectedIndex = selectedIndex;
        lightTypeComboBox.Enabled = _selectedLight is not null;
    }

    private void ShowLightGizmosCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_rebuildingLights || _syncingViewOptions) return;
        _project.RenderSettings.ShowLightGizmos = showLightGizmosCheckBox.Checked;
        lightGizmosMenuItem.Checked = showLightGizmosCheckBox.Checked;
        _renderer.InvalidateScene();
        MarkDirty();
    }

    private void LightGizmoSizeNumericUpDown_ValueChanged(object? sender, EventArgs e)
    {
        if (_rebuildingLights) return;
        _project.RenderSettings.LightGizmoSizePixels = (int)lightGizmoSizeNumericUpDown.Value;
        _renderer.InvalidateScene();
        MarkDirty();
    }

    private void SelectAllLightsCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_rebuildingLights) return;

        var enabled = selectAllLightsCheckBox.Checked;
        _rebuildingLights = true;
        _allowLightItemCheck = true;
        try
        {
            for (var index = 0; index < _project.Lights.Count; index++)
            {
                _project.Lights[index].Enabled = enabled;
                if ((uint)index < (uint)lightListBox.Items.Count)
                    lightListBox.SetItemChecked(index, enabled);
            }
        }
        finally
        {
            _allowLightItemCheck = false;
            _rebuildingLights = false;
        }

        _renderer.InvalidateScene();
        lightPropertyGrid.Refresh();
        MarkDirty();
        statusLabel.Text = enabled ? "已啟用所有燈光。" : "已停用所有燈光。";
        UpdateUi();
    }

    private void LightListBox_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (!_rebuildingLights && !_allowLightItemCheck)
        {
            e.NewValue = e.CurrentValue;
            return;
        }
        if (_rebuildingLights ||
            (uint)e.Index >= (uint)lightListBox.Items.Count ||
            lightListBox.Items[e.Index] is not SceneLight light)
            return;

        light.Enabled = e.NewValue == CheckState.Checked;
        BeginInvoke(() => SyncSelectAllLightsCheckBox());
        _renderer.InvalidateScene();
        MarkDirty();
        if (ReferenceEquals(light, _selectedLight))
            lightPropertyGrid.Refresh();
        statusLabel.Text = light.Enabled
            ? $"已啟用燈光：{light.Name}"
            : $"已停用燈光：{light.Name}";
        UpdateUi();
    }

    private void LightListBox_MouseDown(object? sender, MouseEventArgs e)
    {
        _lightCheckMouseDownIndex = IsLightCheckBoxHit(e.Location)
            ? lightListBox.IndexFromPoint(e.Location)
            : -1;
    }

    private void LightListBox_MouseUp(object? sender, MouseEventArgs e)
    {
        var index = lightListBox.IndexFromPoint(e.Location);
        if (index < 0 || index != _lightCheckMouseDownIndex || !IsLightCheckBoxHit(e.Location))
        {
            _lightCheckMouseDownIndex = -1;
            return;
        }

        _allowLightItemCheck = true;
        try
        {
            lightListBox.SetItemChecked(index, !lightListBox.GetItemChecked(index));
        }
        finally
        {
            _allowLightItemCheck = false;
            _lightCheckMouseDownIndex = -1;
        }
    }

    private bool IsLightCheckBoxHit(Point location)
    {
        var index = lightListBox.IndexFromPoint(location);
        if (index < 0) return false;
        var itemBounds = lightListBox.GetItemRectangle(index);
        var checkWidth = SystemInformation.MenuCheckSize.Width + 8;
        return location.X >= itemBounds.Left && location.X < itemBounds.Left + checkWidth;
    }

    private void AddLight_Click(object? sender, EventArgs e)
    {
        if (_project.Lights.Count >= OpenGlRenderer.MaximumLights)
        {
            const string message = "OpenGL 預覽最多支援 16 盞燈光，無法新增第 17 盞燈光。";
            statusLabel.Text = message;
            MessageBox.Show(this, message, "燈光數量已達上限", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var hasModelBounds = SceneTraversal.TryCalculateBounds(
            _project.Models.Where(model => model.IsVisible),
            out var modelBounds);
        var maximumExtent = hasModelBounds ? Math.Max(modelBounds.MaximumExtent, 0.02f) : 10f;
        var outsideOffset = Math.Max(maximumExtent * 0.5f, 0.5f);
        var position = hasModelBounds
            ? new Vector3(
                modelBounds.Minimum.X - outsideOffset,
                modelBounds.Maximum.Y + outsideOffset,
                modelBounds.Center.Z + outsideOffset)
            : _project.Camera.To + new Vector3(-5f, 5f, 5f);
        var target = hasModelBounds ? modelBounds.Center : _project.Camera.To;
        var direction = target - position;
        if (direction.LengthSquared() < 0.000001f)
            direction = new Vector3(0.5f, -1f, -0.5f);

        var light = new SceneLight
        {
            Name = $"Light {_project.Lights.Count + 1}",
            Position = position,
            Direction = Vector3.Normalize(direction),
            Color = Vector3.One,
            Intensity = 3f,
            Range = Math.Max(maximumExtent * 0.5f, 0.01f)
        };
        _project.Lights.Add(light);
        _selectedLight = light;
        RebuildLightList();
        LightChanged();
    }

    private void RemoveLight_Click(object? sender, EventArgs e)
    {
        if (_selectedLight is null) return;
        if (MessageBox.Show(
                this,
                $"確定要刪除燈光「{_selectedLight.Name}」嗎？",
                "確認刪除燈光",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        var index = _project.Lights.IndexOf(_selectedLight);
        _project.Lights.Remove(_selectedLight);
        _selectedLight = _project.Lights.Count == 0
            ? null
            : _project.Lights[Math.Clamp(index, 0, _project.Lights.Count - 1)];
        RebuildLightList();
        LightChanged();
    }

    private async void LoadLightSettings_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "讀取燈光設定",
            Filter = "Rv3d 燈光設定 (*.rv3dlights)|*.rv3dlights|JSON 檔案 (*.json)|*.json|所有檔案 (*.*)|*.*",
            DefaultExt = "rv3dlights"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var json = await File.ReadAllTextAsync(dialog.FileName);
            var document = JsonSerializer.Deserialize<LightSettingsDocument>(json, LightSettingsJsonOptions)
                ?? throw new InvalidDataException("燈光設定檔內容無效。");
            if (document.FormatVersion > LightSettingsDocument.CurrentFormatVersion)
                throw new NotSupportedException($"燈光設定格式 {document.FormatVersion} 尚未支援。");
            if (document.Lights is null)
                throw new InvalidDataException("燈光設定檔缺少 Lights 資料。");
            if (document.Lights.Count > 8)
                throw new InvalidDataException("燈光設定最多只能包含 8 盞燈光。");
            foreach (var light in document.Lights)
                light.Validate();

            PushUndoState("讀取燈光設定");
            _project.Lights = document.Lights;
            _project.RenderSettings.ShowLightGizmos = document.ShowLightGizmos;
            _selectedLight = _project.Lights.FirstOrDefault();
            RebuildLightList();
            _renderer.InvalidateScene();
            MarkDirty();
            UpdateUi();
            statusLabel.Text = $"已讀取燈光設定：{Path.GetFileName(dialog.FileName)}（{_project.Lights.Count:N0} 盞）";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or NotSupportedException)
        {
            MessageBox.Show(this, ex.Message, "讀取燈光設定失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void SaveLightSettings_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "儲存燈光設定",
            Filter = "Rv3d 燈光設定 (*.rv3dlights)|*.rv3dlights|JSON 檔案 (*.json)|*.json",
            DefaultExt = "rv3dlights",
            AddExtension = true,
            FileName = "lights.rv3dlights"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var document = new LightSettingsDocument
            {
                Lights = _project.Lights,
                ShowLightGizmos = _project.RenderSettings.ShowLightGizmos
            };
            var json = JsonSerializer.Serialize(document, LightSettingsJsonOptions);
            await File.WriteAllTextAsync(dialog.FileName, json);
            statusLabel.Text = $"已儲存燈光設定：{Path.GetFileName(dialog.FileName)}（{_project.Lights.Count:N0} 盞）";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            MessageBox.Show(this, ex.Message, "儲存燈光設定失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RebuildLightList()
    {
        _rebuildingLights = true;
        try
        {
            lightListBox.BeginUpdate();
            lightListBox.Items.Clear();
            foreach (var light in _project.Lights) lightListBox.Items.Add(light, light.Enabled);
            lightListBox.EndUpdate();
            if (_selectedLight is not null) lightListBox.SelectedItem = _selectedLight;
            SyncSelectAllLightsCheckBox();
            showLightGizmosCheckBox.Checked = _project.RenderSettings.ShowLightGizmos;
            lightGizmosMenuItem.Checked = _project.RenderSettings.ShowLightGizmos;
            lightGizmoSizeNumericUpDown.Value = Math.Clamp(_project.RenderSettings.LightGizmoSizePixels,
                (int)lightGizmoSizeNumericUpDown.Minimum, (int)lightGizmoSizeNumericUpDown.Maximum);
            SyncSelectedLightType();
            var lightInspector = _selectedLight is null ? null : new LightInspector(_selectedLight, LightChanged);
            if (lightInspector is not null) TypeDescriptor.Refresh(lightInspector);
            lightPropertyGrid.SelectedObject = lightInspector;
            _renderer.SetHighlightedLight(_selectedLight);
        }
        finally
        {
            _rebuildingLights = false;
        }
    }

    private void SyncSelectAllLightsCheckBox()
    {
        var allEnabled = _project.Lights.Count > 0 && _project.Lights.All(light => light.Enabled);
        if (selectAllLightsCheckBox.Checked == allEnabled) return;

        var wasRebuilding = _rebuildingLights;
        _rebuildingLights = true;
        try
        {
            selectAllLightsCheckBox.Checked = allEnabled;
        }
        finally
        {
            _rebuildingLights = wasRebuilding;
        }
    }

    private void LightChanged()
    {
        _renderer.InvalidateScene();
        MarkDirty();
        var selected = _selectedLight;
        RebuildLightList();
        _selectedLight = selected;
        SyncSelectedLightType();
        lightPropertyGrid.Refresh();
        UpdateUi();
    }

    private void LoadEnvironment_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "載入 HDRI 環境",
            Filter = "Radiance HDR (*.hdr)|*.hdr|所有檔案 (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _project.Environment.Path = dialog.FileName;
        _project.Environment.Enabled = true;
        environmentPropertyGrid.Refresh();
        EnvironmentChanged();
        statusLabel.Text = $"已載入環境：{Path.GetFileName(dialog.FileName)}";
    }

    private void CreateDefaultEnvironment_Click(object? sender, EventArgs e)
    {
        try
        {
            var targetPath = DefaultHdrService.GetTargetPath(_project, _selectedModel);
            if (File.Exists(targetPath) && MessageBox.Show(
                    this,
                    $"預設 HDR 已存在：\n{targetPath}\n\n是否覆寫？",
                    "覆寫預設 HDR",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            var hdrPath = DefaultHdrService.CreateForProject(_project, _selectedModel);
            environmentPropertyGrid.SelectedObject = new EnvironmentInspector(_project.Environment, EnvironmentChanged);
            EnvironmentChanged();
            statusLabel.Text = $"已建立並載入預設 HDR：{hdrPath}";
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "建立預設 HDR 失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearEnvironment_Click(object? sender, EventArgs e)
    {
        _project.Environment.Path = string.Empty;
        _project.Environment.Enabled = false;
        environmentPropertyGrid.Refresh();
        EnvironmentChanged();
        statusLabel.Text = "已清除 HDRI 環境";
    }

    private void EnvironmentChanged()
    {
        _project.Environment.Validate();
        _renderer.InvalidateEnvironmentAppearance();
        environmentPropertyGrid.Refresh();
        MarkDirty();
        UpdateUi();
    }

    private static string ResolveProjectAssetPath(string projectPath, string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath)) return string.Empty;
        return Path.IsPathRooted(assetPath)
            ? Path.GetFullPath(assetPath)
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, assetPath));
    }

    private async void ImportSkyboxPanorama_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "讀入 2:1 Skybox 全景圖",
            Filter = "全景圖 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|所有檔案 (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        SetBusy(true, "正在將全景圖轉換為 Cubemap...");
        try
        {
            var skybox = await Task.Run(() => SkyboxFolderService.LoadPanorama(dialog.FileName, SkyboxLibraryDirectory));
            AddSkybox(skybox);
            statusLabel.Text = $"已讀入並轉換 Skybox：{skybox.Name}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            MessageBox.Show(this, ex.Message, "讀入 Skybox 失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { SetBusy(false); }
    }

    private void ImportSkyboxFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "選取包含 px、nx、py、ny、pz、nz 六張正方形圖片的資料夾",
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var skybox = SkyboxFolderService.LoadFolder(dialog.SelectedPath);
            AddSkybox(skybox);
            statusLabel.Text = $"已讀入六面 Skybox：{skybox.Name}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            MessageBox.Show(this, ex.Message, "讀入 Skybox 失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddSkybox(SkyboxSettings skybox)
    {
        var baseName = skybox.Name;
        for (var suffix = 2; _project.Skyboxes.Any(item => item.Name.Equals(skybox.Name, StringComparison.OrdinalIgnoreCase)); suffix++)
            skybox.Name = $"{baseName}_{suffix}";
        _project.Skyboxes.Add(skybox);
        _selectedSkybox = skybox;
        RebuildSkyboxList();
        MarkDirty();
        UpdateUi();
    }

    private void ReloadSkyboxLibrary_Click(object? sender, EventArgs e) => ReloadSkyboxLibrary(showStatus: true);

    private void InspectorTabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(inspectorTabControl.SelectedTab, skyboxTabPage) || _project.Skyboxes.Count > 0)
            return;
        ReloadSkyboxLibrary(showStatus: false);
        if (_project.Skyboxes.Count == 0)
        {
            _selectedSkybox = null;
            RebuildSkyboxList();
        }
    }

    private void ReloadSkyboxLibrary(bool showStatus)
    {
        var libraryItems = SkyboxFolderService.LoadLibrary(SkyboxLibraryDirectory);
        var added = 0;
        foreach (var item in libraryItems)
        {
            if (_project.Skyboxes.Any(existing => existing.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase))) continue;
            _project.Skyboxes.Add(item);
            added++;
        }
        _selectedSkybox ??= _project.Skyboxes.FirstOrDefault(skybox => skybox.Show) ?? _project.Skyboxes.FirstOrDefault();
        RebuildSkyboxList();
        if (added > 0 && showStatus) MarkDirty();
        UpdateUi();
        if (showStatus) statusLabel.Text = $"Skybox 資料庫共 {_project.Skyboxes.Count:N0} 筆，本次加入 {added:N0} 筆";
    }

    private void DeleteSkybox_Click(object? sender, EventArgs e)
    {
        if (_selectedSkybox is null) return;
        if (MessageBox.Show(this, $"是否從目前專案移除 Skybox「{_selectedSkybox.Name}」？\n原始圖片不會刪除。",
                "移除 Skybox", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        var index = _project.Skyboxes.IndexOf(_selectedSkybox);
        _project.Skyboxes.Remove(_selectedSkybox);
        _selectedSkybox = _project.Skyboxes.Count == 0 ? null : _project.Skyboxes[Math.Clamp(index, 0, _project.Skyboxes.Count - 1)];
        RebuildSkyboxList();
        _renderer.InvalidateEnvironmentAppearance();
        MarkDirty();
        UpdateUi();
    }

    private void SkyboxListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_rebuildingSkyboxes) return;
        var keepVisible = showSkyboxCheckBox.Checked;
        _selectedSkybox = skyboxListBox.SelectedItem as SkyboxSettings;
        foreach (var item in _project.Skyboxes)
            item.Show = keepVisible && ReferenceEquals(item, _selectedSkybox);
        SyncSkyboxOptions();
        _renderer.InvalidateEnvironmentAppearance();
        if (keepVisible) MarkDirty();
        UpdateUi();
    }

    private void ShowSkyboxCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_rebuildingSkyboxes || _selectedSkybox is null) return;
        if (showSkyboxCheckBox.Checked)
            foreach (var item in _project.Skyboxes) item.Show = ReferenceEquals(item, _selectedSkybox);
        else
            _selectedSkybox.Show = false;
        _renderer.InvalidateEnvironmentAppearance();
        MarkDirty();
        RebuildSkyboxList();
        UpdateUi();
    }

    private void UseSkyboxAsEnvironmentCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_rebuildingSkyboxes || _selectedSkybox is null) return;
        _selectedSkybox.UseAsEnvironment = useSkyboxAsEnvironmentCheckBox.Checked;
        _renderer.InvalidateEnvironmentAppearance();
        MarkDirty();
    }

    private void SkyboxRotationNumericUpDown_ValueChanged(object? sender, EventArgs e)
    {
        if (_rebuildingSkyboxes || _selectedSkybox is null) return;
        _selectedSkybox.RotationDegrees = (float)skyboxRotationNumericUpDown.Value;
        _renderer.InvalidateEnvironmentAppearance();
        MarkDirty();
    }

    private void RebuildSkyboxList()
    {
        _rebuildingSkyboxes = true;
        try
        {
            skyboxListBox.BeginUpdate();
            skyboxListBox.Items.Clear();
            foreach (var item in _project.Skyboxes) skyboxListBox.Items.Add(item);
            skyboxListBox.EndUpdate();
            if (_selectedSkybox is not null) skyboxListBox.SelectedItem = _selectedSkybox;
            SyncSkyboxOptions();
        }
        finally { _rebuildingSkyboxes = false; }
    }

    private void SyncSkyboxOptions()
    {
        var hasSelection = _selectedSkybox is not null;
        var anyVisible = _project.Skyboxes.Any(item => item.Show);
        if (!hasSelection) anyVisible = false;
        showSkyboxCheckBox.Enabled = hasSelection;
        useSkyboxAsEnvironmentCheckBox.Enabled = hasSelection;
        skyboxRotationNumericUpDown.Enabled = hasSelection;
        showSkyboxCheckBox.Checked = anyVisible;
        useSkyboxAsEnvironmentCheckBox.Checked = hasSelection && _selectedSkybox!.UseAsEnvironment;
        skyboxRotationNumericUpDown.Value = hasSelection
            ? Math.Clamp((decimal)_selectedSkybox!.RotationDegrees, skyboxRotationNumericUpDown.Minimum, skyboxRotationNumericUpDown.Maximum)
            : 0m;
    }

    private void SceneTreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (_syncingBoxSelection || _rebuildingSceneTree) return;
        ClearMaterialLibrarySelection();
        var node = e.Node;
        if (node is null)
        {
            ClearAppliedMaterialHighlight();
            _selectedModel = null;
            _selectedMaterial = null;
            _selectedMeshIndex = null;
            SetTreeSelectedMeshes([]);
            _renderer.SetSelectedMeshHighlight(null, null);
            BindInspectors();
            UpdateUi();
            return;
        }
        switch (node.Tag)
        {
            case SceneModel model:
                ClearAppliedMaterialHighlight();
                _renderer.SetSelectedMeshHighlight(null, null);
                _selectedModel = model;
                _selectedMeshIndex = null;
                SetTreeSelectedModels([model], expandGroups: true);
                //_selectedMaterial = model.Materials.FirstOrDefault();
                break;
            case MeshTreeItem meshItem:
                SelectMeshFromTree(meshItem);
                //inspectorTabControl.SelectedTab = objectTabPage;
                break;
            case PbrMaterial material:
                SelectMeshesUsingMaterial(node, material);
                _selectedMaterial = material;
                inspectorTabControl.SelectedTab = materialTabPage;
                break;
            default:
                // Selecting a grouping node (such as "Meshes" or "Materials") is
                // navigation only. Keep the current Mesh node selected.
                RestoreSelectedMeshTreeNode();
                break;
        }
        BindInspectors();
        UpdateUi();
    }

    private void SceneTreeView_DrawNode(object? sender, DrawTreeNodeEventArgs e)
    {
        if (e.Node?.Tag is not MeshTreeItem meshItem ||
            !_treeSelectedMeshes.Contains(new MeshSelection(meshItem.Model, meshItem.MeshIndex)))
        {
            e.DrawDefault = true;
            return;
        }

        var primary = ReferenceEquals(_selectedModel, meshItem.Model) &&
                      _selectedMeshIndex == meshItem.MeshIndex;
        var rowBounds = new Rectangle(
            e.Bounds.Left,
            e.Bounds.Top,
            Math.Max(1, sceneTreeView.ClientSize.Width - e.Bounds.Left),
            e.Bounds.Height);
        var accent = Color.FromArgb(255, 158, 20);
        var background = FIsDarkMode
            ? Color.FromArgb(primary ? 118 : 82, 78, 48, 12)
            : Color.FromArgb(primary ? 255 : 245, 224, 174);
        var foreground = FIsDarkMode ? Color.FromArgb(255, 226, 170) : Color.FromArgb(78, 42, 0);

        using var backgroundBrush = new SolidBrush(background);
        using var accentBrush = new SolidBrush(accent);
        e.Graphics.FillRectangle(backgroundBrush, rowBounds);
        e.Graphics.FillRectangle(accentBrush, rowBounds.Left, rowBounds.Top, primary ? 4 : 3, rowBounds.Height);
        if (primary)
        {
            using var borderPen = new Pen(accent);
            e.Graphics.DrawRectangle(borderPen, rowBounds.Left, rowBounds.Top,
                Math.Max(0, rowBounds.Width - 1), Math.Max(0, rowBounds.Height - 1));
        }

        var textBounds = new Rectangle(
            e.Bounds.Left + 6,
            e.Bounds.Top,
            Math.Max(1, rowBounds.Right - e.Bounds.Left - 8),
            e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, e.Node.Text, e.Node.NodeFont ?? sceneTreeView.Font,
            textBounds, foreground,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if ((e.State & TreeNodeStates.Focused) != 0)
            ControlPaint.DrawFocusRectangle(e.Graphics, rowBounds, foreground, background);
    }

    private void SceneTreeView_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Button != MouseButtons.Right ||
            e.Node.Tag is not PbrMaterial material ||
            e.Node.Parent?.Parent?.Tag is not SceneModel model)
            return;

        _contextMaterialModel = model;
        _contextMaterial = material;
        sceneTreeView.SelectedNode = e.Node;

        var alreadyInLibrary = _materialLibrary.Any(libraryMaterial =>
            libraryMaterial.Name.Equals(material.Name, StringComparison.OrdinalIgnoreCase));
        _addMaterialNodeToLibraryMenuItem.Enabled = !alreadyInLibrary;
        _addMaterialNodeToLibraryMenuItem.ToolTipText = alreadyInLibrary
            ? "材質庫中已有同名材質。"
            : string.Empty;
        var libraryNames = _materialLibrary.Select(item => item.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingMaterialCount = model.Materials.Select(item => item.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count(name => !libraryNames.Contains(name));
        _addAllMaterialNodesToLibraryMenuItem.Enabled = missingMaterialCount > 0;
        _addAllMaterialNodesToLibraryMenuItem.ToolTipText = missingMaterialCount == 0
            ? "目前模型的材質都已存在於材質庫。"
            : $"將新增 {missingMaterialCount:N0} 個尚未存在的材質。";
        _deleteUnusedMaterialNodesMenuItem.Enabled = HasUnusedMaterials(model);
        _applyMaterialNodeMenuItem.Enabled = GetSelectedMeshes().Count > 0;
        _materialNodeContextMenu.Show(sceneTreeView, e.Location);
    }

    private void AddMaterialNodeToLibrary_Click(object? sender, EventArgs e)
    {
        if (_contextMaterial is null || _materialLibrary.Any(material =>
                material.Name.Equals(_contextMaterial.Name, StringComparison.OrdinalIgnoreCase)))
            return;

        var added = _contextMaterial.Clone();
        _materialLibrary.Add(added);
        RebuildMaterialLibrary();
        statusLabel.Text = $"已將「{added.Name}」新增到材質庫。";
    }

    private void AddAllMaterialNodesToLibrary_Click(object? sender, EventArgs e)
    {
        if (_contextMaterialModel is null) return;
        var names = _materialLibrary.Select(material => material.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;
        foreach (var material in _contextMaterialModel.Materials)
        {
            if (!names.Add(material.Name)) continue;
            _materialLibrary.Add(material.Clone());
            added++;
        }

        if (added == 0) return;
        RebuildMaterialLibrary();
        statusLabel.Text = $"已將目前模型的 {added:N0} 個新材質新增到材質庫。";
    }

    private void DeleteMaterialNode_Click(object? sender, EventArgs e)
    {
        if (_contextMaterialModel is null || _contextMaterial is null) return;
        var model = _contextMaterialModel;
        var material = _contextMaterial;
        var materialIndex = model.Materials.FindIndex(candidate => ReferenceEquals(candidate, material));
        if (materialIndex < 0) return;

        var usingMeshes = model.Meshes
            .Select((mesh, meshIndex) => (Mesh: mesh, MeshIndex: meshIndex))
            .Where(item => item.Mesh.MaterialIndex == materialIndex)
            .ToList();
        if (usingMeshes.Count > 0)
        {
            var meshList = string.Join(Environment.NewLine, usingMeshes.Take(10)
                .Select(item => $"• {item.Mesh.Name}"));
            var remaining = usingMeshes.Count - Math.Min(10, usingMeshes.Count);
            var remainingText = remaining > 0 ? $"{Environment.NewLine}…另有 {remaining:N0} 個 Mesh" : string.Empty;
            var answer = MessageBox.Show(
                this,
                $"材質「{material.Name}」目前被 {usingMeshes.Count:N0} 個 Mesh 使用：{Environment.NewLine}{Environment.NewLine}" +
                $"{meshList}{remainingText}{Environment.NewLine}{Environment.NewLine}" +
                "刪除後，這些 Mesh 的材質連結將被清除。確定要刪除嗎？",
                "刪除使用中的材質",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return;
        }

        foreach (var mesh in model.Meshes)
        {
            if (mesh.MaterialIndex == materialIndex)
                mesh.MaterialIndex = -1;
            else if (mesh.MaterialIndex > materialIndex)
                mesh.MaterialIndex--;
        }
        model.Materials.RemoveAt(materialIndex);
        RefreshSelectedMaterialFromMesh();
        _contextMaterialModel = null;
        _contextMaterial = null;
        _renderer.InvalidateScene();
        RebuildTree();
        MarkDirty();
        statusLabel.Text = usingMeshes.Count == 0
            ? $"已刪除材質「{material.Name}」。"
            : $"已刪除材質「{material.Name}」，並清除 {usingMeshes.Count:N0} 個 Mesh 的材質連結。";
    }

    private void DeleteUnusedMaterialNodes_Click(object? sender, EventArgs e)
    {
        if (_contextMaterialModel is null) return;
        var model = _contextMaterialModel;
        var removed = RemoveUnusedMaterials(model);
        if (removed == 0) return;

        RefreshSelectedMaterialFromMesh();
        _contextMaterial = null;
        _renderer.InvalidateScene();
        RebuildTree();
        MarkDirty();
        statusLabel.Text = $"已從模型「{model.Name}」刪除 {removed:N0} 個未使用的材質。";
    }

    private void ApplyMaterialNode_Click(object? sender, EventArgs e)
    {
        if (_contextMaterialModel is null || _contextMaterial is null) return;
        var selections = GetSelectedMeshes();
        if (selections.Count == 0) return;
        if (!ConfirmApplyMaterial(_contextMaterial.Name, selections.Count)) return;

        PbrMaterial? lastAppliedMaterial = null;
        foreach (var group in selections.GroupBy(selection => selection.Model))
        {
            var targetModel = group.Key;
            int targetMaterialIndex;
            PbrMaterial targetMaterial;
            if (ReferenceEquals(targetModel, _contextMaterialModel))
            {
                targetMaterial = _contextMaterial;
                targetMaterialIndex = targetModel.Materials.FindIndex(material => ReferenceEquals(material, targetMaterial));
                if (targetMaterialIndex < 0) continue;
            }
            else
            {
                var name = CreateUniqueMaterialName(
                    _contextMaterial.Name,
                    targetModel.Materials.Select(material => material.Name));
                targetMaterial = _contextMaterial.Clone(name);
                targetModel.Materials.Add(targetMaterial);
                targetMaterialIndex = targetModel.Materials.Count - 1;
            }

            foreach (var selection in group)
                if ((uint)selection.MeshIndex < (uint)targetModel.Meshes.Count)
                    targetModel.Meshes[selection.MeshIndex].MaterialIndex = targetMaterialIndex;
            lastAppliedMaterial = targetMaterial;
        }

        _selectedMaterial = lastAppliedMaterial;
        _renderer.InvalidateScene();
        RebuildTree();
        RestoreMeshSelectionAfterTreeRebuild(selections);
        MarkDirty();
        statusLabel.Text = $"已將材質「{_contextMaterial.Name}」套用到 {selections.Count:N0} 個 Mesh。";
    }

    private List<MeshSelection> GetSelectedMeshes()
    {
        if (_treeSelectionRepresentsModels) return [];
        return _treeSelectedMeshes
            .Where(selection => _project.Models.Contains(selection.Model) &&
                                (uint)selection.MeshIndex < (uint)selection.Model.Meshes.Count)
            .ToList();
    }

    private void RestoreMeshSelectionAfterTreeRebuild(IEnumerable<MeshSelection> selections)
    {
        var validSelections = selections
            .Where(selection => _project.Models.Contains(selection.Model) &&
                                (uint)selection.MeshIndex < (uint)selection.Model.Meshes.Count)
            .ToList();
        SetTreeSelectedMeshes(validSelections);
        ExpandTreeSelectedMeshGroups(ensurePrimaryVisible: true);
        _renderer.SetSelectedMeshHighlights(validSelections);
        RestoreSelectedMeshTreeNode();
    }

    private void RefreshSelectedMaterialFromMesh()
    {
        _selectedMaterial = _selectedModel is not null &&
                            _selectedMeshIndex is int meshIndex &&
                            (uint)meshIndex < (uint)_selectedModel.Meshes.Count
            ? _selectedModel.Materials.ElementAtOrDefault(_selectedModel.Meshes[meshIndex].MaterialIndex)
            : _selectedModel?.Materials.FirstOrDefault();
    }

    private static bool HasUnusedMaterials(SceneModel model)
    {
        var usedIndices = model.Meshes.Select(mesh => mesh.MaterialIndex).ToHashSet();
        return Enumerable.Range(0, model.Materials.Count).Any(index => !usedIndices.Contains(index));
    }

    private static int RemoveUnusedMaterials(SceneModel model)
    {
        var usedIndices = model.Meshes
            .Select(mesh => mesh.MaterialIndex)
            .Where(index => (uint)index < (uint)model.Materials.Count)
            .ToHashSet();
        if (usedIndices.Count == model.Materials.Count) return 0;

        var newIndexByOldIndex = new Dictionary<int, int>();
        var retainedMaterials = new List<PbrMaterial>(usedIndices.Count);
        for (var oldIndex = 0; oldIndex < model.Materials.Count; oldIndex++)
        {
            if (!usedIndices.Contains(oldIndex)) continue;
            newIndexByOldIndex[oldIndex] = retainedMaterials.Count;
            retainedMaterials.Add(model.Materials[oldIndex]);
        }

        foreach (var mesh in model.Meshes)
            mesh.MaterialIndex = newIndexByOldIndex.TryGetValue(mesh.MaterialIndex, out var newIndex)
                ? newIndex
                : -1;

        var removed = model.Materials.Count - retainedMaterials.Count;
        model.Materials = retainedMaterials;
        return removed;
    }

    private void SceneTreeView_BeforeCheck(object? sender, TreeViewCancelEventArgs e)
    {
        if (e.Action != TreeViewAction.Unknown && e.Node?.Tag is not (SceneModel or MeshTreeItem))
            e.Cancel = true;
    }

    private void SceneTreeView_AfterCheck(object? sender, TreeViewEventArgs e)
    {
        if (e.Action == TreeViewAction.Unknown) return;

        switch (e.Node?.Tag)
        {
            case SceneModel model:
                SetModelVisibility(model, e.Node.Checked);
                break;
            case MeshTreeItem meshItem:
                if (e.Node.Checked)
                    meshItem.Model.HiddenMeshIndices.Remove(meshItem.MeshIndex);
                else if (!meshItem.Model.HiddenMeshIndices.Contains(meshItem.MeshIndex))
                    meshItem.Model.HiddenMeshIndices.Add(meshItem.MeshIndex);
                break;
            default:
                return;
        }

        SyncModelChecks();
        SyncAllModelsCheckBox();
        _renderer.InvalidateScene();
        objectPropertyGrid.Refresh();
        MarkDirty();
        UpdateUi();
    }

    private void RebuildTree()
    {
        _treeSelectedMeshes.RemoveWhere(selection =>
            !_project.Models.Contains(selection.Model) ||
            (uint)selection.MeshIndex >= (uint)selection.Model.Meshes.Count);
        _rebuildingSceneTree = true;
        sceneTreeView.BeginUpdate();
        try
        {
            ClearAppliedMaterialHighlight();
            sceneTreeView.Nodes.Clear();
            foreach (var model in _project.Models)
            {
                var modelNode = new TreeNode(model.Name) { Tag = model, Checked = model.IsVisible };
                var meshes = new TreeNode($"Meshes ({model.Meshes.Count})");
                for (var meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
                {
                    var mesh = model.Meshes[meshIndex];
                    meshes.Nodes.Add(new TreeNode($"{mesh.Name} ({mesh.TriangleCount:N0} tris)")
                    {
                        Tag = new MeshTreeItem(model, meshIndex),
                        Checked = !model.HiddenMeshIndices.Contains(meshIndex)
                    });
                }
                var materials = new TreeNode($"Materials ({model.Materials.Count})");
                foreach (var material in model.Materials) materials.Nodes.Add(new TreeNode(material.Name) { Tag = material });
                modelNode.Nodes.Add(meshes);
                modelNode.Nodes.Add(materials);
                sceneTreeView.Nodes.Add(modelNode);
            }
        }
        finally
        {
            sceneTreeView.EndUpdate();
            _rebuildingSceneTree = false;
        }
        SyncAllModelsCheckBox();
        ExpandTreeSelectedMeshGroups(ensurePrimaryVisible: false);
        sceneTreeView.Invalidate();
        if (_selectedModel is not null && _selectedMeshIndex is int selectedMeshIndex)
        {
            HighlightAppliedMaterial(new MeshTreeItem(_selectedModel, selectedMeshIndex));
            RestoreSelectedMeshTreeNode();
        }
        BindInspectors();
        UpdateUi();
    }

    private void RestoreSelectedMeshTreeNode()
    {
        if (_selectedModel is null || _selectedMeshIndex is not int meshIndex) return;

        _syncingBoxSelection = true;
        try
        {
            SelectTreeMesh(_selectedModel, meshIndex);
        }
        finally
        {
            _syncingBoxSelection = false;
        }
    }

    private void HighlightAppliedMaterial(MeshTreeItem meshItem)
    {
        ClearAppliedMaterialHighlight();
        if ((uint)meshItem.MeshIndex >= (uint)meshItem.Model.Meshes.Count) return;
        var materialIndex = meshItem.Model.Meshes[meshItem.MeshIndex].MaterialIndex;
        if ((uint)materialIndex >= (uint)meshItem.Model.Materials.Count) return;
        var material = meshItem.Model.Materials[materialIndex];

        var modelNode = sceneTreeView.Nodes.Cast<TreeNode>()
            .FirstOrDefault(node => ReferenceEquals(node.Tag, meshItem.Model));
        if (modelNode is null) return;
        var materialNode = modelNode.Nodes.Cast<TreeNode>()
            .SelectMany(group => group.Nodes.Cast<TreeNode>())
            .FirstOrDefault(node => ReferenceEquals(node.Tag, material));
        if (materialNode is null) return;

        _appliedMaterialNode = materialNode;
        materialNode.BackColor = Color.FromArgb(255, 226, 138);
        materialNode.ForeColor = Color.FromArgb(72, 45, 0);
        materialNode.ToolTipText = $"目前選取的 Mesh 使用此材質：{material.Name}";
        materialNode.Parent?.Expand();
    }

    private void ClearAppliedMaterialHighlight()
    {
        if (_appliedMaterialNode is null) return;
        _appliedMaterialNode.BackColor = Color.Empty;
        _appliedMaterialNode.ForeColor = Color.Empty;
        _appliedMaterialNode.ToolTipText = string.Empty;
        _appliedMaterialNode = null;
    }

    private void SelectTreeObject(object target)
    {
        foreach (TreeNode node in sceneTreeView.Nodes)
            if (ReferenceEquals(node.Tag, target)) { sceneTreeView.SelectedNode = node; return; }
    }

    private void SelectTreeMesh(SceneModel model, int meshIndex)
    {
        foreach (TreeNode modelNode in sceneTreeView.Nodes)
        {
            if (!ReferenceEquals(modelNode.Tag, model)) continue;
            foreach (TreeNode groupNode in modelNode.Nodes)
                foreach (TreeNode childNode in groupNode.Nodes)
                    if (childNode.Tag is MeshTreeItem item &&
                        ReferenceEquals(item.Model, model) &&
                        item.MeshIndex == meshIndex)
                    {
                        modelNode.Expand();
                        groupNode.Expand();
                        sceneTreeView.SelectedNode = childNode;
                        childNode.EnsureVisible();
                        return;
                    }
        }
    }

    private void SetTreeSelectedModels(IEnumerable<SceneModel> models, bool expandGroups)
    {
        var selectedModels = models
            .Where(_project.Models.Contains)
            .ToHashSet();
        SetTreeSelectedMeshes(selectedModels.SelectMany(model =>
            Enumerable.Range(0, model.Meshes.Count)
                .Select(meshIndex => new MeshSelection(model, meshIndex))));
        _treeSelectionRepresentsModels = true;
        if (expandGroups)
            ExpandTreeSelectedMeshGroups(ensurePrimaryVisible: true);
    }

    private void SetTreeSelectedMeshes(IEnumerable<MeshSelection> meshes)
    {
        _treeSelectionRepresentsModels = false;
        _treeSelectedMeshes.Clear();
        foreach (var selection in meshes)
        {
            if (_project.Models.Contains(selection.Model) &&
                (uint)selection.MeshIndex < (uint)selection.Model.Meshes.Count)
                _treeSelectedMeshes.Add(selection);
        }
        sceneTreeView.Invalidate();
    }

    private void ExpandTreeSelectedMeshGroups(bool ensurePrimaryVisible)
    {
        TreeNode? primaryNode = null;
        TreeNode? firstSelectedNode = null;
        foreach (TreeNode modelNode in sceneTreeView.Nodes)
        {
            if (modelNode.Tag is not SceneModel model ||
                !_treeSelectedMeshes.Any(selection => ReferenceEquals(selection.Model, model)))
                continue;

            var meshGroup = modelNode.Nodes.Cast<TreeNode>()
                .FirstOrDefault(group => group.Nodes.Cast<TreeNode>()
                    .Any(child => child.Tag is MeshTreeItem));
            if (meshGroup is null) continue;
            modelNode.Expand();
            meshGroup.Expand();

            if (!ensurePrimaryVisible) continue;
            firstSelectedNode ??= meshGroup.Nodes.Cast<TreeNode>().FirstOrDefault(child =>
                child.Tag is MeshTreeItem item &&
                _treeSelectedMeshes.Contains(new MeshSelection(item.Model, item.MeshIndex)));
            primaryNode ??= meshGroup.Nodes.Cast<TreeNode>().FirstOrDefault(child =>
                child.Tag is MeshTreeItem item &&
                ReferenceEquals(item.Model, _selectedModel) &&
                item.MeshIndex == _selectedMeshIndex);
        }
        (primaryNode ?? firstSelectedNode)?.EnsureVisible();
    }

    private void BindInspectors()
    {
        objectPropertyGrid.SelectedObject = _selectedModel is null
            ? null
            : _selectedMeshIndex is int meshIndex && (uint)meshIndex < (uint)_selectedModel.Meshes.Count
                ? new MeshInspector(_selectedModel, meshIndex, SceneChanged)
                : new ModelInspector(_selectedModel, SceneChanged);
        var materialInspector = _selectedLibraryMaterial is not null
            ? new MaterialInspector(_selectedLibraryMaterial, MaterialLibraryChanged)
            : _selectedMaterial is null
                ? null
                : new MaterialInspector(_selectedMaterial, SceneChanged);
        if (materialInspector is not null) TypeDescriptor.Refresh(materialInspector);
        materialPropertyGrid.SelectedObject = materialInspector;
        cameraPropertyGrid.SelectedObject = new CameraInspector(_project.Camera, CameraEdited);
        var lightInspector = _selectedLight is null ? null : new LightInspector(_selectedLight, LightChanged);
        if (lightInspector is not null) TypeDescriptor.Refresh(lightInspector);
        lightPropertyGrid.SelectedObject = lightInspector;
        environmentPropertyGrid.SelectedObject = new EnvironmentInspector(_project.Environment, EnvironmentChanged);
        RefreshMaterialPreview();
    }

    private void RefreshMaterialPreview()
    {
        if (materialPreviewControl is null) return;
        materialPreviewControl.ProjectDirectory = string.IsNullOrWhiteSpace(_project.ProjectFilePath)
            ? null
            : Path.GetDirectoryName(_project.ProjectFilePath);
        materialPreviewControl.Material = _selectedLibraryMaterial ?? _selectedMaterial;
    }

    private void BoxSelectionChanged(IReadOnlyList<SceneModel> models, IReadOnlyList<MeshSelection> meshes)
    {
        ClearMaterialLibrarySelection();
        ClearAppliedMaterialHighlight();
        var selectedMesh = meshes.LastOrDefault();
        if (meshes.Count > 0)
        {
            _selectedModel = selectedMesh.Model;
            _selectedMeshIndex = selectedMesh.MeshIndex;
            _selectedMaterial = selectedMesh.Model.Materials.ElementAtOrDefault(
                selectedMesh.Model.Meshes[selectedMesh.MeshIndex].MaterialIndex);
            HighlightAppliedMaterial(new MeshTreeItem(selectedMesh.Model, selectedMesh.MeshIndex));
        }
        else
        {
            _selectedModel = models.LastOrDefault();
            _selectedMaterial = _selectedModel?.Materials.FirstOrDefault();
            _selectedMeshIndex = null;
        }

        _syncingBoxSelection = true;
        try
        {
            if (meshes.Count > 0)
            {
                SetTreeSelectedMeshes(meshes);
                ExpandTreeSelectedMeshGroups(ensurePrimaryVisible: true);
                SelectTreeMesh(selectedMesh.Model, selectedMesh.MeshIndex);
            }
            else if (_selectedModel is not null)
            {
                SetTreeSelectedModels(models, expandGroups: true);
                SelectTreeObject(_selectedModel);
            }
            else
            {
                SetTreeSelectedMeshes([]);
                sceneTreeView.SelectedNode = null;
            }
        }
        finally
        {
            _syncingBoxSelection = false;
        }
        BindInspectors();
        UpdateUi();
        statusLabel.Text = meshes.Count > 0
            ? $"已選取 {meshes.Count:N0} 個 Mesh"
            : $"已選取 {models.Count:N0} 個模型";
        var highlightSuffix = _project.RenderSettings.ShowSelectionHighlight
            ? string.Empty
            : "｜顯示選取高亮已關閉";
        if (meshes.Count > 0)
        {
            var modelCount = meshes.Select(selection => selection.Model).Distinct().Count();
            SetEditHint($"選取 Mesh｜目前已選取 {meshes.Count:N0} 個 Mesh（{modelCount:N0} 個模型）｜Ctrl + Alt 可減少選取{highlightSuffix}");
        }
        else if (models.Count > 0)
        {
            SetEditHint($"選取模型｜目前已選取 {models.Count:N0} 個模型｜Ctrl + Alt 可減少選取{highlightSuffix}");
        }
        else
        {
            SetEditHint(_editMode == ViewportEditMode.Navigate
                ? NavigateHint
                : "沒有選取到項目｜請按住 Ctrl，在物件上點擊或拖曳框選");
        }
    }

    private void SelectMeshFromTree(MeshTreeItem meshItem)
    {
        var clicked = new MeshSelection(meshItem.Model, meshItem.MeshIndex);
        var control = (ModifierKeys & Keys.Control) != 0;
        var shift = (ModifierKeys & Keys.Shift) != 0;
        HashSet<MeshSelection> selections = control ? _treeSelectedMeshes.ToHashSet() : [];

        if (shift && _treeSelectionAnchor is MeshSelection anchor && ReferenceEquals(anchor.Model, meshItem.Model))
        {
            var first = Math.Min(anchor.MeshIndex, meshItem.MeshIndex);
            var last = Math.Max(anchor.MeshIndex, meshItem.MeshIndex);
            if (!control) selections.Clear();
            for (var index = first; index <= last; index++)
                selections.Add(new MeshSelection(meshItem.Model, index));
        }
        else if (control)
        {
            if (!selections.Add(clicked)) selections.Remove(clicked);
            _treeSelectionAnchor = clicked;
        }
        else
        {
            selections.Add(clicked);
            _treeSelectionAnchor = clicked;
        }

        SetTreeSelectedMeshes(selections);
        _renderer.SetSelectedMeshHighlights(selections);
        var primary = selections.Contains(clicked) ? clicked : selections.LastOrDefault();
        if (selections.Count == 0)
        {
            ClearAppliedMaterialHighlight();
            _selectedModel = meshItem.Model;
            _selectedMeshIndex = null;
            _selectedMaterial = null;
            return;
        }

        _selectedModel = primary.Model;
        _selectedMeshIndex = primary.MeshIndex;
        _selectedMaterial = primary.Model.Materials.ElementAtOrDefault(
            primary.Model.Meshes[primary.MeshIndex].MaterialIndex);
        HighlightAppliedMaterial(new MeshTreeItem(primary.Model, primary.MeshIndex));
        statusLabel.Text = $"已選取 {selections.Count:N0} 個 Mesh";
    }

    private void SelectMeshesUsingMaterial(TreeNode materialNode, PbrMaterial material)
    {
        var model = materialNode.Parent?.Parent?.Tag as SceneModel;
        if (model is null) return;
        var materialIndex = model.Materials.FindIndex(candidate => ReferenceEquals(candidate, material));
        var selections = materialIndex < 0
            ? []
            : model.Meshes.Select((mesh, index) => (mesh, index))
                .Where(item => item.mesh.MaterialIndex == materialIndex)
                .Select(item => new MeshSelection(model, item.index))
                .ToList();

        _selectedModel = model;
        _selectedMeshIndex = null;
        _treeSelectionAnchor = selections.Count > 0 ? selections[0] : null;
        SetTreeSelectedMeshes(selections);
        ExpandTreeSelectedMeshGroups(ensurePrimaryVisible: true);
        _renderer.SetSelectedMeshHighlights(selections);
        ClearAppliedMaterialHighlight();
        statusLabel.Text = selections.Count == 0
            ? $"材質「{material.Name}」目前未被任何 Mesh 使用"
            : $"材質「{material.Name}」使用於 {selections.Count:N0} 個 Mesh";
    }

    private void UpdateEditModeChecks()
    {
        exitEditModeMenuItem.Checked = _editMode == ViewportEditMode.Navigate;
        selectModeMenuItem.Checked = _editMode == ViewportEditMode.Select;
        selectMeshModeMenuItem.Checked = _editMode == ViewportEditMode.SelectMesh;
        subtractModeMenuItem.Checked = _editMode == ViewportEditMode.Subtract;
    }

    private void SceneChanged()
    {
        _renderer.InvalidateScene();
        materialPreviewControl?.RefreshMaterial();
        SyncModelChecks();
        SyncTreeNodeNames();
        MarkDirty();
        sceneTreeView.Refresh();
        UpdateUi();
    }

    private void CameraEdited()
    {
        MarkCameraDirty();
        cameraPropertyGrid.Refresh();
        _pluginContext.NotifyCameraChanged();
    }

    private void SyncTreeNodeNames()
    {
        foreach (TreeNode modelNode in sceneTreeView.Nodes)
        {
            if (modelNode.Tag is SceneModel model)
                modelNode.Text = model.Name;

            foreach (TreeNode groupNode in modelNode.Nodes)
                foreach (TreeNode childNode in groupNode.Nodes)
                    if (childNode.Tag is PbrMaterial material)
                        childNode.Text = material.Name;
        }
    }

    private void SyncModelChecks()
    {
        foreach (TreeNode modelNode in sceneTreeView.Nodes)
        {
            if (modelNode.Tag is not SceneModel model) continue;
            if (modelNode.Checked != model.IsVisible)
                modelNode.Checked = model.IsVisible;

            foreach (TreeNode groupNode in modelNode.Nodes)
                foreach (TreeNode childNode in groupNode.Nodes)
                {
                    if (childNode.Tag is not MeshTreeItem meshItem) continue;
                    var visible = !model.HiddenMeshIndices.Contains(meshItem.MeshIndex);
                    if (childNode.Checked != visible)
                        childNode.Checked = visible;
                }
        }
        SyncAllModelsCheckBox();
    }

    private void AllModelsCheckBox_CheckStateChanged(object? sender, EventArgs e)
    {
        if (_syncingAllModelsCheckBox || allModelsCheckBox.CheckState == CheckState.Indeterminate)
            return;

        var visible = allModelsCheckBox.Checked;
        foreach (var model in _project.Models)
            SetModelVisibility(model, visible);

        SyncModelChecks();
        _renderer.InvalidateScene();
        objectPropertyGrid.Refresh();
        MarkDirty();
        statusLabel.Text = visible ? "已顯示所有模型。" : "已隱藏所有模型。";
        UpdateUi();
    }

    private void SyncAllModelsCheckBox()
    {
        var models = _project.Models;
        var state = models.Count == 0
            ? CheckState.Unchecked
            : models.All(model => model.IsVisible)
                ? CheckState.Checked
                : models.All(model => !model.IsVisible)
                    ? CheckState.Unchecked
                    : CheckState.Indeterminate;

        _syncingAllModelsCheckBox = true;
        try
        {
            allModelsCheckBox.Enabled = models.Count > 0;
            allModelsCheckBox.CheckState = state;
        }
        finally
        {
            _syncingAllModelsCheckBox = false;
        }
    }

    private static string FormatKeyInput(Keys keyData)
    {
        var parts = new List<string>(4);
        if (keyData.HasFlag(Keys.Control)) parts.Add("Ctrl");
        if (keyData.HasFlag(Keys.Shift)) parts.Add("Shift");
        if (keyData.HasFlag(Keys.Alt)) parts.Add("Alt");

        var keyCode = keyData & Keys.KeyCode;
        if (keyCode is not (Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.None))
        {
            parts.Add(keyCode switch
            {
                Keys.Escape => "Esc",
                Keys.Space => "Space",
                Keys.Return => "Enter",
                Keys.Back => "Backspace",
                Keys.Delete => "Delete",
                _ => keyCode.ToString()
            });
        }

        return string.Join(" + ", parts);
    }

    private static void SetModelVisibility(SceneModel model, bool visible)
    {
        model.IsVisible = visible;
    }

    private void MarkDirty()
    {
        if (_skipNextAutomaticUndo)
        {
            _skipNextAutomaticUndo = false;
        }
        else if (_undoBaseline is not null)
        {
            var now = DateTime.UtcNow;
            var coalesce = _undoHistory.Count > 0 &&
                           _undoHistory[^1].Description == "編輯場景" &&
                           now - _lastAutomaticUndoUtc < TimeSpan.FromMilliseconds(500);
            if (!coalesce)
            {
                _undoHistory.Add(_undoBaseline);
                if (_undoHistory.Count > UndoHistoryLimit)
                    _undoHistory.RemoveAt(0);
            }
            _lastAutomaticUndoUtc = now;
        }
        _undoBaseline = CaptureUndoEntry("編輯場景");
        _dirty = true;
        UpdateTitle();
        UpdateUndoMenuItem();
    }

    private void BeginCameraInteraction()
    {
        _cameraInteractionActive = true;
        _cameraInteractionChanged = false;
        _cameraUiRefreshClock.Restart();
    }

    private void RendererCameraChanged()
    {
        if (!_cameraInteractionActive)
        {
            cameraPropertyGrid.Refresh();
            MarkCameraDirty();
            _pluginContext.NotifyCameraChanged();
            return;
        }

        var firstChange = !_cameraInteractionChanged;
        _cameraInteractionChanged = true;
        if (firstChange)
        {
            _dirty = true;
            UpdateTitle();
        }
        if (_cameraUiRefreshClock.Elapsed < TimeSpan.FromMilliseconds(50)) return;
        cameraPropertyGrid.Refresh();
        _pluginContext.NotifyCameraChanged();
        _cameraUiRefreshClock.Restart();
    }

    private void CompleteCameraInteraction()
    {
        if (!_cameraInteractionActive) return;
        _cameraInteractionActive = false;
        if (!_cameraInteractionChanged)
            return;

        cameraPropertyGrid.Refresh();
        _pluginContext.NotifyCameraChanged();
        _cameraInteractionChanged = false;
    }

    private void MarkCameraDirty()
    {
        // Camera navigation is persisted with the project but deliberately does not
        // create, consume, or replace an Undo entry.
        _dirty = true;
        UpdateTitle();
    }

    private void PushUndoState(string description)
    {
        _undoHistory.Add(CaptureUndoEntry(description));
        if (_undoHistory.Count > UndoHistoryLimit)
            _undoHistory.RemoveAt(0);
        _skipNextAutomaticUndo = true;
        UpdateUndoMenuItem();
    }

    private UndoEntry CaptureUndoEntry(string description) => new(
        description,
        CloneProject(_project),
        _selectedModel?.Id,
        _selectedMeshIndex,
        _selectedModel is null || _selectedMaterial is null
            ? null
            : _selectedModel.Materials.FindIndex(material => ReferenceEquals(material, _selectedMaterial)),
        _selectedLibraryMaterial is null
            ? null
            : _materialLibrary.FindIndex(material => ReferenceEquals(material, _selectedLibraryMaterial)));

    private void Undo_Click(object? sender, EventArgs e)
    {
        SetEditHint("復原｜回復上一個場景編輯動作｜目前 Camera 視角不變");
        if (_undoHistory.Count == 0) return;
        var entry = _undoHistory[^1];
        _undoHistory.RemoveAt(_undoHistory.Count - 1);
        // Undo restores scene content only. The current viewport camera is navigation
        // state and must survive object/material/light/environment restoration.
        entry.Project.Camera = CloneCamera(_project.Camera);
        _project = entry.Project;
        _selectedModel = entry.SelectedModelId is Guid modelId
            ? _project.Models.FirstOrDefault(model => model.Id == modelId)
            : _project.Models.FirstOrDefault();
        _selectedMeshIndex = _selectedModel is not null &&
                             entry.SelectedMeshIndex is int meshIndex &&
                             (uint)meshIndex < (uint)_selectedModel.Meshes.Count
            ? meshIndex
            : null;
        _selectedMaterial = _selectedModel is not null &&
                            entry.SelectedMaterialIndex is int materialIndex
            ? _selectedModel.Materials.ElementAtOrDefault(materialIndex)
            : _selectedModel?.Materials.FirstOrDefault();
        _selectedLibraryMaterial = entry.SelectedLibraryMaterialIndex is int libraryIndex
            ? _materialLibrary.ElementAtOrDefault(libraryIndex)
            : null;
        _selectedLight = _project.Lights.FirstOrDefault();

        _renderer.SetProject(_project);
        if (_selectedModel is not null && _selectedMeshIndex is int selectedMeshIndex)
            _renderer.SetSelectedMeshHighlight(_selectedModel, selectedMeshIndex);
        cameraPropertyGrid.SelectedObject = new CameraInspector(_project.Camera, CameraEdited);
        RebuildLightList();
        RebuildMaterialLibrary(_selectedLibraryMaterial);
        RebuildTree();
        if (_selectedModel is not null)
        {
            if (_selectedMeshIndex is int selectedIndex)
                SelectTreeMesh(_selectedModel, selectedIndex);
            else
                SelectTreeObject(_selectedModel);
        }
        BindInspectors();
        _undoBaseline = CaptureUndoEntry("編輯場景");
        _skipNextAutomaticUndo = false;
        _dirty = true;
        UpdateTitle();
        UpdateUndoMenuItem();
        UpdateUi();
        statusLabel.Text = $"已復原：{entry.Description}";
    }

    private void UpdateUndoMenuItem()
    {
        undoMenuItem.Enabled = _undoHistory.Count > 0;
        undoMenuItem.Text = _undoHistory.Count == 0
            ? "復原"
            : $"復原 {_undoHistory[^1].Description}";
    }

    private static ViewerProject CloneProject(ViewerProject source)
    {
        var json = JsonSerializer.Serialize(source, UndoJsonOptions);
        var clone = JsonSerializer.Deserialize<ViewerProject>(json, UndoJsonOptions)
            ?? throw new InvalidOperationException("無法建立復原狀態。");
        clone.ProjectFilePath = source.ProjectFilePath;
        for (var modelIndex = 0; modelIndex < source.Models.Count; modelIndex++)
        {
            var sourceModel = source.Models[modelIndex];
            var clonedModel = clone.Models[modelIndex];
            clonedModel.SourceFilePath = sourceModel.SourceFilePath;
            clonedModel.Meshes = sourceModel.Meshes.Select(CloneMesh).ToList();
            clonedModel.Nodes = sourceModel.Nodes.Select(CloneNode).ToList();
        }
        return clone;
    }

    private static CameraState CloneCamera(CameraState source) => new()
    {
        From = source.From,
        To = source.To,
        Up = source.Up,
        RollDegrees = source.RollDegrees,
        FieldOfViewDegrees = source.FieldOfViewDegrees,
        NearPlane = source.NearPlane,
        FarPlane = source.FarPlane
    };

    private static MeshData CloneMesh(MeshData source) => new()
    {
        Name = source.Name,
        Positions = source.Positions,
        Normals = source.Normals,
        TextureCoordinates = source.TextureCoordinates,
        Tangents = source.Tangents,
        Indices = source.Indices,
        MaterialIndex = source.MaterialIndex
    };

    private static SceneNode CloneNode(SceneNode source) => new()
    {
        Name = source.Name,
        LocalTransform = source.LocalTransform,
        MeshIndices = [.. source.MeshIndices],
        Children = source.Children.Select(CloneNode).ToList()
    };

    private void SetBusy(bool busy, string? message = null)
    {
        UseWaitCursor = busy;
        mainMenuStrip.Enabled = !busy;
        mainToolStrip.Enabled = !busy;
        if (message is not null) statusLabel.Text = message;
    }

    private void BeginProjectProgress(string message, int value)
    {
        projectProgressBar.Style = ProgressBarStyle.Continuous;
        projectProgressBar.Value = Math.Clamp(value, projectProgressBar.Minimum, projectProgressBar.Maximum);
        projectProgressBar.Visible = true;
        statusLabel.Text = message;
    }

    private void ReportProjectProgress(int value, string message)
    {
        projectProgressBar.Value = Math.Clamp(value, projectProgressBar.Minimum, projectProgressBar.Maximum);
        projectProgressBar.Visible = true;
        statusLabel.Text = message;
    }

    private void EndProjectProgress()
    {
        projectProgressBar.Value = projectProgressBar.Minimum;
        projectProgressBar.Visible = false;
    }

    private void UpdateUi()
    {
        removeModelButton.Enabled = _selectedModel is not null;
        frameSelectedButton.Enabled = _selectedModel is not null;
        frameSelectedMenuItem.Enabled = _selectedModel is not null;
        textureButton.Enabled = _selectedLibraryMaterial is not null || _selectedMaterial is not null;
        deleteLibraryMaterialButton.Enabled = _selectedLibraryMaterial is not null;
        updateLibraryMaterialButton.Enabled = _selectedLibraryMaterial is not null && _selectedMaterial is not null;
        saveMaterialLibraryButton.Enabled = _materialLibrary.Count > 0;
        deleteUnusedMaterialsMenuItem.Enabled = _project.Models.Any(model =>
            Enumerable.Range(0, model.Materials.Count).Except(model.Meshes.Select(mesh => mesh.MaterialIndex)).Any());
        applyLibraryMaterialButton.Enabled = _selectedLibraryMaterial is not null &&
            GetSelectedMeshes().Count > 0;
        UpdateUndoMenuItem();
        repairAssetsMenuItem.Enabled = ProjectAssetService.FindMissingAssets(_project).Count > 0;
        texturesMenuItem.Checked = _project.RenderSettings.ShowTextures;
        gridMenuItem.Checked = _project.RenderSettings.ShowGrid;
        worldAxesMenuItem.Checked = _project.RenderSettings.ShowWorldAxes;
        cameraGizmoMenuItem.Checked = _project.RenderSettings.ShowCameraGizmo;
        lightGizmosMenuItem.Checked = _project.RenderSettings.ShowLightGizmos;
        selectionHighlightMenuItem.Checked = _project.RenderSettings.ShowSelectionHighlight;
        var modelMode = GetProjectModelDisplayMode();
        modelPointsMenuItem.Checked = modelMode == ModelDisplayMode.Points;
        modelWireframeMenuItem.Checked = modelMode == ModelDisplayMode.Wireframe;
        modelSolidMenuItem.Checked = modelMode == ModelDisplayMode.Solid;
        SyncViewOptions();
        deleteSelectionMenuItem.Enabled = _renderer.HighlightedModels.Count > 0 || _renderer.HighlightedMeshes.Count > 0;
        selectAllMenuItem.Enabled = _project.Models.Any(model => model.IsVisible);
        clearSelectionMenuItem.Enabled = deleteSelectionMenuItem.Enabled;
        UpdateEditModeChecks();
        removeLightButton.Enabled = _selectedLight is not null;
        addLightButton.Enabled = true;
        deleteSkyboxButton.Enabled = _selectedSkybox is not null;
        var triangles = _project.Models.SelectMany(m => m.Meshes).Sum(m => m.TriangleCount);
        statisticsLabel.Text = $"模型 {_project.Models.Count:N0} | 三角形 {triangles:N0}";
        UpdateTitle();
        _pluginContext?.NotifyStateChanged();
    }

    internal ViewerProject PluginProject => _project;
    // QuickPreviewEnabled remains for older plug-ins; PreviewMode is exposed through
    // the optional preview-mode context so newer plug-ins can match modes 1 through 5.
    internal bool PluginQuickPreviewEnabled => _previewMode == ViewportPreviewMode.Preview1;
    internal int PluginPreviewMode => (int)_previewMode;
    internal SceneModel? PluginSelectedModel => _selectedModel;
    internal int? PluginSelectedMeshIndex => _selectedMeshIndex;
    internal PbrMaterial? PluginSelectedMaterial => _selectedMaterial;
    internal SceneLight? PluginSelectedLight => _selectedLight;

    internal void ApplyPluginSceneChanges()
    {
        if (_selectedModel is not null && !_project.Models.Contains(_selectedModel))
        {
            _selectedModel = null;
            _selectedMeshIndex = null;
            _selectedMaterial = null;
        }
        if (_selectedLight is not null && !_project.Lights.Contains(_selectedLight))
            _selectedLight = null;

        _renderer.SetProject(_project);
        RebuildTree();
        RebuildLightList();
        BindInspectors();
        MarkDirty();
        UpdateUi();
    }

    internal void InvalidatePluginScene() => _renderer.InvalidateScene();
    internal void MarkPluginProjectModified() => MarkDirty();
    internal void SetPluginStatus(string message) => statusLabel.Text = message;

    internal IPluginProjectEditTransaction BeginPluginProjectEdit(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (InvokeRequired)
            throw new InvalidOperationException("Plugin 專案交易必須在 UI 執行緒開始。");
        if (_pluginProjectEditActive)
            throw new InvalidOperationException("目前已有進行中的 Plugin 專案交易。");

        _pluginProjectEditActive = true;
        return new PluginProjectEditTransaction(this, CaptureUndoEntry(description.Trim()), _dirty);
    }

    private void CommitPluginProjectEdit(UndoEntry snapshot, Guid? selectedModelId)
    {
        _undoHistory.Add(snapshot);
        if (_undoHistory.Count > UndoHistoryLimit)
            _undoHistory.RemoveAt(0);
        _skipNextAutomaticUndo = true;
        _pluginProjectEditActive = false;

        if (selectedModelId is Guid modelId)
        {
            _selectedModel = _project.Models.FirstOrDefault(model => model.Id == modelId);
            _selectedMeshIndex = null;
            _selectedMaterial = _selectedModel?.Materials.FirstOrDefault();
        }

        ApplyPluginSceneChanges();
        _undoBaseline = CaptureUndoEntry("編輯場景");
        UpdateUndoMenuItem();
        statusLabel.Text = $"已完成：{snapshot.Description}";
    }

    private void RollbackPluginProjectEdit(UndoEntry snapshot, bool dirtyBefore)
    {
        _project = snapshot.Project;
        _selectedModel = snapshot.SelectedModelId is Guid modelId
            ? _project.Models.FirstOrDefault(model => model.Id == modelId)
            : _project.Models.FirstOrDefault();
        _selectedMeshIndex = _selectedModel is not null &&
                             snapshot.SelectedMeshIndex is int meshIndex &&
                             (uint)meshIndex < (uint)_selectedModel.Meshes.Count
            ? meshIndex
            : null;
        _selectedMaterial = _selectedModel is not null && snapshot.SelectedMaterialIndex is int materialIndex
            ? _selectedModel.Materials.ElementAtOrDefault(materialIndex)
            : _selectedModel?.Materials.FirstOrDefault();
        _selectedLight = _project.Lights.FirstOrDefault();
        _pluginProjectEditActive = false;
        _skipNextAutomaticUndo = false;
        _dirty = dirtyBefore;

        _renderer.SetProject(_project);
        RebuildTree();
        RebuildLightList();
        BindInspectors();
        _undoBaseline = CaptureUndoEntry("編輯場景");
        UpdateTitle();
        UpdateUndoMenuItem();
        UpdateUi();
        statusLabel.Text = $"已取消：{snapshot.Description}";
    }

    internal void ApplyPluginCamera(CameraState state, bool commit)
    {
        var camera = _project.Camera;
        camera.From = state.From;
        camera.To = state.To;
        camera.Up = state.Up;
        camera.RollDegrees = state.RollDegrees;
        camera.FieldOfViewDegrees = state.FieldOfViewDegrees;
        camera.NearPlane = state.NearPlane;
        camera.FarPlane = state.FarPlane;
        camera.Validate();
        _renderer.InvalidateScene();
        cameraPropertyGrid.Refresh();
        if (commit) MarkDirty();
    }

    internal IDisposable AcquirePluginCameraPlaybackControl()
    {
        _pluginCameraPlaybackLocks++;
        _renderer.NavigationEnabled = false;
        cameraPropertyGrid.Enabled = false;
        return new DelegateDisposable(() =>
        {
            if (_pluginCameraPlaybackLocks > 0) _pluginCameraPlaybackLocks--;
            if (_pluginCameraPlaybackLocks != 0 || IsDisposed) return;
            _renderer.NavigationEnabled = true;
            cameraPropertyGrid.Enabled = true;
        });
    }

    internal void CapturePluginViewportPng(string filePath, int width, int height, bool includeEditorOverlays)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        width = Math.Clamp(width, 1, 16384);
        height = Math.Clamp(height, 1, 16384);
        using var image = _renderer.CaptureRenderedImage(width, height, includeEditorOverlays);
        image.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
    }

    internal byte[] RenderPluginViewportPreview(CameraState camera, int width, int height, bool useQuickPreview)
    {
        ArgumentNullException.ThrowIfNull(camera);
        width = Math.Clamp(width, 1, 16384);
        height = Math.Clamp(height, 1, 16384);
        using var image = _renderer.CaptureRenderedImage(
            width, height, includeEditorOverlays: false, camera, useQuickPreview);
        using var stream = new MemoryStream();
        image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    internal byte[] RenderPluginViewportPreview(ViewerProject project, CameraState camera, int width, int height,
        bool useQuickPreview) => WithPluginRenderProject(project,
        () => RenderPluginViewportPreview(camera, width, height, useQuickPreview));

    internal CameraEditorRenderResult RenderPluginCameraEditorView(ViewerProject project, CameraState camera,
        IReadOnlyList<CameraState> trajectory, IReadOnlyList<CameraState> draftTrajectory,
        IReadOnlyList<Vector3> editControlPoints, CameraEditorView view, int width, int height, float viewScale,
        Vector2 viewOffset, bool showCamera, bool showTrajectory, bool useQuickPreview) =>
        WithPluginRenderProject(project, () => RenderPluginCameraEditorView(camera, trajectory, draftTrajectory,
            editControlPoints, view, width, height, viewScale, viewOffset, showCamera, showTrajectory, useQuickPreview));

    private T WithPluginRenderProject<T>(ViewerProject project, Func<T> render)
    {
        var mainProject = _project;
        try
        {
            _project = project;
            _renderer.SetProject(project);
            return render();
        }
        finally
        {
            _project = mainProject;
            _renderer.SetProject(mainProject);
            _renderer.InvalidateScene();
        }
    }

    internal async Task<ViewerProject> LoadPluginIsolatedProjectAsync(string filePath,
        CancellationToken cancellationToken)
    {
        var project = await _projectStore.LoadAsync(filePath, cancellationToken);
        foreach (var savedModel in project.Models.Where(model => !model.IsProcedural))
        {
            var modelPath = ProjectAssetService.ResolveModelPath(project, savedModel);
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"找不到模型資源：{modelPath}", modelPath);
            var imported = await _importer.ImportAsync(modelPath, cancellationToken);
            savedModel.SourceFilePath = modelPath;
            if (savedModel.Materials.Count == 0) savedModel.Materials = imported.Materials;
            savedModel.RestoreImportedGeometry(imported);
        }
        return project;
    }

    internal Task<SceneModel> ImportPluginGlbAsync(string filePath, CancellationToken cancellationToken) =>
        _importer.ImportAsync(filePath, cancellationToken);

    internal ViewerProject ClonePluginProject() => CloneProject(_project);

    internal CameraEditorRenderResult RenderPluginCameraEditorView(
        CameraState subjectCamera,
        IReadOnlyList<CameraState> trajectory,
        IReadOnlyList<CameraState> draftTrajectory,
        IReadOnlyList<Vector3> editControlPoints,
        CameraEditorView view,
        int width,
        int height,
        float viewScale,
        Vector2 viewOffset,
        bool showCamera,
        bool showTrajectory,
        bool useQuickPreview)
    {
        ArgumentNullException.ThrowIfNull(subjectCamera);
        width = Math.Clamp(width, 1, 2048);
        height = Math.Clamp(height, 1, 2048);
        var hasBounds = SceneTraversal.TryCalculateBounds(
            _project.Models.Where(model => model.IsVisible), out var sceneBounds);
        var minimum = hasBounds ? sceneBounds.Minimum : new Vector3(-1f);
        var maximum = hasBounds ? sceneBounds.Maximum : new Vector3(1f);
        minimum = Vector3.Min(minimum, Vector3.Min(subjectCamera.From, subjectCamera.To));
        maximum = Vector3.Max(maximum, Vector3.Max(subjectCamera.From, subjectCamera.To));
        foreach (var state in trajectory)
        {
            minimum = Vector3.Min(minimum, Vector3.Min(state.From, state.To));
            maximum = Vector3.Max(maximum, Vector3.Max(state.From, state.To));
        }
        foreach (var state in draftTrajectory)
        {
            minimum = Vector3.Min(minimum, Vector3.Min(state.From, state.To));
            maximum = Vector3.Max(maximum, Vector3.Max(state.From, state.To));
        }
        foreach (var point in editControlPoints)
        {
            minimum = Vector3.Min(minimum, point);
            maximum = Vector3.Max(maximum, point);
        }
        var center = (minimum + maximum) * 0.5f;
        var viewCenter = center + (view switch
        {
            CameraEditorView.Left => new Vector3(0f, viewOffset.Y, viewOffset.X),
            CameraEditorView.Top => new Vector3(viewOffset.X, 0f, -viewOffset.Y),
            _ => new Vector3(viewOffset.X, viewOffset.Y, 0f)
        });
        var extent = Math.Max((maximum - minimum).Length(), 1f);
        var distance = extent * 2f + 1f;
        var editorCamera = new CameraState
        {
            From = view switch
            {
                CameraEditorView.Left => viewCenter - Vector3.UnitX * distance,
                CameraEditorView.Top => viewCenter + Vector3.UnitY * distance,
                _ => viewCenter + Vector3.UnitZ * distance
            },
            To = viewCenter,
            Up = view == CameraEditorView.Top ? -Vector3.UnitZ : Vector3.UnitY,
            NearPlane = 0.01f,
            FarPlane = distance * 4f,
            FieldOfViewDegrees = 60f
        };

        var horizontalCenter = view == CameraEditorView.Left ? viewCenter.Z : viewCenter.X;
        var verticalCenter = view == CameraEditorView.Top ? -viewCenter.Z : viewCenter.Y;
        var horizontalSpan = view == CameraEditorView.Left ? maximum.Z - minimum.Z : maximum.X - minimum.X;
        var verticalSpan = view == CameraEditorView.Top ? maximum.Z - minimum.Z : maximum.Y - minimum.Y;
        var orthographicHeight = Math.Max(Math.Max(verticalSpan, horizontalSpan * height / (float)width), 1f) *
                                 1.2f * Math.Clamp(viewScale, 0.1f, 20f);
        var orthographicWidth = orthographicHeight * width / height;
        using var image = _renderer.CaptureRenderedImage(width, height, includeEditorOverlays: false,
            editorCamera, useQuickPreview, orthographicHeight);
        using (var graphics = Graphics.FromImage(image))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            PointF Project(Vector3 point)
            {
                var horizontal = view == CameraEditorView.Left ? point.Z : point.X;
                var vertical = view == CameraEditorView.Top ? -point.Z : point.Y;
                return new PointF(
                    (horizontal - (horizontalCenter - orthographicWidth * 0.5f)) / orthographicWidth * width,
                    height - (vertical - (verticalCenter - orthographicHeight * 0.5f)) / orthographicHeight * height);
            }

            if (showTrajectory && trajectory.Count > 1)
            {
                using var positionPathPen = new Pen(Color.FromArgb(230, Color.Orange), 2f)
                    { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                using var targetPathPen = new Pen(Color.FromArgb(190, Color.DeepSkyBlue), 1.5f)
                    { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
                graphics.DrawLines(positionPathPen, trajectory.Select(state => Project(state.From)).ToArray());
                graphics.DrawLines(targetPathPen, trajectory.Select(state => Project(state.To)).ToArray());
            }
            if (showTrajectory && draftTrajectory.Count > 1)
            {
                using var draftPen = new Pen(Color.FromArgb(235, Color.LightGray), 2.5f)
                    { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                graphics.DrawLines(draftPen, draftTrajectory.Select(state => Project(state.From)).ToArray());
                graphics.DrawLines(draftPen, draftTrajectory.Select(state => Project(state.To)).ToArray());
            }

            if (editControlPoints.Count == 3)
            {
                var projected = editControlPoints.Select(Project).ToArray();
                using var controlPen = new Pen(Color.FromArgb(220, Color.LimeGreen), 1.5f)
                    { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                using var controlBrush = new SolidBrush(Color.LimeGreen);
                graphics.DrawLines(controlPen, projected);
                foreach (var point in projected.Skip(1).Take(1))
                    graphics.FillEllipse(controlBrush, point.X - 6f, point.Y - 6f, 12f, 12f);
            }

            var from = Project(subjectCamera.From);
            var target = Project(subjectCamera.To);
            using var directionPen = new Pen(Color.OrangeRed, 2f);
            using var frustumPen = new Pen(Color.FromArgb(210, Color.Gold), 1.5f);
            using var cameraBrush = new SolidBrush(Color.OrangeRed);
            using var targetBrush = new SolidBrush(Color.DeepSkyBlue);
            if (showCamera)
            {
                graphics.DrawLine(directionPen, from, target);
                graphics.FillEllipse(cameraBrush, from.X - 6f, from.Y - 6f, 12f, 12f);
                graphics.FillEllipse(targetBrush, target.X - 5f, target.Y - 5f, 10f, 10f);
            }
            var direction = new Vector2(target.X - from.X, target.Y - from.Y);
            if (showCamera && direction.LengthSquared() > 0.01f)
            {
                direction = Vector2.Normalize(direction) * Math.Min(Vector2.Distance(new Vector2(from.X, from.Y),
                    new Vector2(target.X, target.Y)), Math.Min(width, height) * 0.35f);
                var halfFov = subjectCamera.FieldOfViewDegrees * MathF.PI / 360f;
                Vector2 Rotate(Vector2 value, float angle) => new(
                    value.X * MathF.Cos(angle) - value.Y * MathF.Sin(angle),
                    value.X * MathF.Sin(angle) + value.Y * MathF.Cos(angle));
                var left = Rotate(direction, -halfFov);
                var right = Rotate(direction, halfFov);
                graphics.DrawLine(frustumPen, from, new PointF(from.X + left.X, from.Y + left.Y));
                graphics.DrawLine(frustumPen, from, new PointF(from.X + right.X, from.Y + right.Y));
            }
            using var textBrush = new SolidBrush(Color.White);
            using var textBackground = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
            var label = $"{view}   Pos {subjectCamera.From.X:0.##}, {subjectCamera.From.Y:0.##}, {subjectCamera.From.Z:0.##}   FOV {subjectCamera.FieldOfViewDegrees:0.#}°";
            graphics.FillRectangle(textBackground, 4, 4, Math.Min(width - 8, 420), 24);
            graphics.DrawString(label, Font, textBrush, 8f, 7f);
        }
        using var stream = new MemoryStream();
        image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        IReadOnlyList<CameraEditorPoint> projectedControls = editControlPoints
            .Select(point =>
            {
                var horizontal = view == CameraEditorView.Left ? point.Z : point.X;
                var vertical = view == CameraEditorView.Top ? -point.Z : point.Y;
                return new CameraEditorPoint(
                    (horizontal - (horizontalCenter - orthographicWidth * 0.5f)) / orthographicWidth * width,
                    height - (vertical - (verticalCenter - orthographicHeight * 0.5f)) / orthographicHeight * height);
            }).ToArray();
        CameraEditorPoint ProjectResult(Vector3 point)
        {
            var projected = new PointF(
                ((view == CameraEditorView.Left ? point.Z : point.X) -
                 (horizontalCenter - orthographicWidth * 0.5f)) / orthographicWidth * width,
                height - ((view == CameraEditorView.Top ? -point.Z : point.Y) -
                          (verticalCenter - orthographicHeight * 0.5f)) / orthographicHeight * height);
            return new CameraEditorPoint(projected.X, projected.Y);
        }
        return new CameraEditorRenderResult(stream.ToArray(), projectedControls,
            ProjectResult(subjectCamera.From), ProjectResult(subjectCamera.To),
            orthographicWidth / width, orthographicHeight / height);
    }

    private void UpdateTitle()
    {
        var name = string.IsNullOrWhiteSpace(_project.ProjectFilePath) ? _project.Name : Path.GetFileName(_project.ProjectFilePath);
        Text = $"{(_dirty ? "*" : string.Empty)}{name} - Rv3d Viewer";
    }

    private bool ConfirmDiscardChanges()
    {
        if (!_dirty) return true;
        var answer = MessageBox.Show(this, "目前專案尚未儲存，是否捨棄變更？", "未儲存的變更", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        return answer == DialogResult.Yes;
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closingConfirmed)
        {
            _pluginManager?.Dispose();
            InputOverlayPreferences.Changed -= InputOverlayPreferences_Changed;
            ViewportColorPreferences.Changed -= ViewportColorPreferences_Changed;
            _renderer.Dispose();
            return;
        }

        var closeAnswer = MessageBox.Show(
            this,
            "確定要離開程式嗎？",
            "確認離開",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (closeAnswer != DialogResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        e.Cancel = true;
        SetBusy(true, "正在儲存 lastScene.rv3dproj...");
        BeginProjectProgress("正在準備 lastScene 專案資料...", 0);
        _renderer.SetRenderingPaused(true);
        var saved = false;
        try
        {
            var saveProgress = new Progress<int>(value =>
                ReportProjectProgress(value, "正在儲存 lastScene 專案與資產..."));
            await Task.Run(() => ProjectAssetService.PrepareForSave(_project, LastScenePath, saveProgress));
            ReportProjectProgress(90, "正在寫入 lastScene.rv3dproj...");
            await _projectStore.SaveAsync(_project, LastScenePath);
            ReportProjectProgress(96, "正在儲存材質資料庫...");
            await _materialLibraryStore.SaveAsync(_materialLibrary, DefaultMaterialLibraryPath);
            ReportProjectProgress(100, "自動儲存完成");
            _dirty = false;
            saved = true;
        }
        catch (Exception ex)
        {
            var answer = MessageBox.Show(
                this,
                $"無法儲存 lastScene.rv3dproj：\n{ex.Message}\n\n仍要離開程式嗎？",
                "自動儲存失敗",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error);
            if (answer != DialogResult.Yes) return;
        }
        finally
        {
            _renderer.SetRenderingPaused(false);
            EndProjectProgress();
            if (!saved) SetBusy(false);
        }

        try
        {
            await SaveUiSettingsAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                $"無法儲存介面設定：\n{ex.Message}",
                "介面設定儲存失敗",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        _closingConfirmed = true;
        Close();
    }

    private sealed class ModelInspector(SceneModel model, Action changed)
    {
        [Category("物件")]
        public string Name { get => model.Name; set { model.Name = value; changed(); } }
        [Category("物件")]
        public bool Visible { get => model.IsVisible; set { SetModelVisibility(model, value); changed(); } }
        [Category("位置")]
        public float X { get => model.Transform.Position.X; set { model.Transform.Position = model.Transform.Position with { X = value }; changed(); } }
        [Category("位置")]
        public float Y { get => model.Transform.Position.Y; set { model.Transform.Position = model.Transform.Position with { Y = value }; changed(); } }
        [Category("位置")]
        public float Z { get => model.Transform.Position.Z; set { model.Transform.Position = model.Transform.Position with { Z = value }; changed(); } }
        [Category("旋轉（度）")]
        public float RotationX { get => model.Transform.RotationDegrees.X; set { model.Transform.RotationDegrees = model.Transform.RotationDegrees with { X = value }; changed(); } }
        [Category("旋轉（度）")]
        public float RotationY { get => model.Transform.RotationDegrees.Y; set { model.Transform.RotationDegrees = model.Transform.RotationDegrees with { Y = value }; changed(); } }
        [Category("旋轉（度）")]
        public float RotationZ { get => model.Transform.RotationDegrees.Z; set { model.Transform.RotationDegrees = model.Transform.RotationDegrees with { Z = value }; changed(); } }
        [Category("縮放")]
        public float ScaleX { get => model.Transform.Scale.X; set { model.Transform.Scale = model.Transform.Scale with { X = Math.Max(value, 0.0001f) }; changed(); } }
        [Category("縮放")]
        public float ScaleY { get => model.Transform.Scale.Y; set { model.Transform.Scale = model.Transform.Scale with { Y = Math.Max(value, 0.0001f) }; changed(); } }
        [Category("縮放")]
        public float ScaleZ { get => model.Transform.Scale.Z; set { model.Transform.Scale = model.Transform.Scale with { Z = Math.Max(value, 0.0001f) }; changed(); } }
    }

    private sealed record MeshTreeItem(SceneModel Model, int MeshIndex);

    private sealed record UndoEntry(
        string Description,
        ViewerProject Project,
        Guid? SelectedModelId,
        int? SelectedMeshIndex,
        int? SelectedMaterialIndex,
        int? SelectedLibraryMaterialIndex);

    private sealed record PluginMenuCommand(LoadedPlugin Plugin, PluginCommand Command);

    private sealed class PluginProjectEditTransaction(
        MainForm form,
        UndoEntry snapshot,
        bool dirtyBefore) : IPluginProjectEditTransaction
    {
        private MainForm? _form = form;
        public bool IsCommitted { get; private set; }

        public void Commit(Guid? selectedModelId = null)
        {
            var owner = _form ?? throw new ObjectDisposedException(nameof(PluginProjectEditTransaction));
            if (IsCommitted) throw new InvalidOperationException("Plugin 專案交易已提交。");
            owner.CommitPluginProjectEdit(snapshot, selectedModelId);
            IsCommitted = true;
        }

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _form, null);
            if (owner is null || IsCommitted || owner.IsDisposed) return;
            owner.RollbackPluginProjectEdit(snapshot, dirtyBefore);
        }
    }

    private sealed class DelegateDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }

    private sealed class AppUiSettings
    {
        public bool IsDarkMode { get; set; }
        public int LeftPanelWidth { get; set; }
        public int CenterPanelWidth { get; set; }
        public int RightPanelWidth { get; set; }
        public string LastPreviewImageOutputPath { get; set; } = string.Empty;
        public int PreviewMode { get; set; }
        public bool? QuickPreviewEnabled { get; set; }
        public bool? ShowTextures { get; set; }
        public bool? ShowGrid { get; set; }
        public bool? ShowWorldAxes { get; set; }
        public bool? ShowCameraGizmo { get; set; }
        public bool? ShowLightGizmos { get; set; }
        public bool? ShowInputInfo { get; set; }
        public bool? ViewDisplayGroupExpanded { get; set; }
        public bool? ViewColorGroupExpanded { get; set; }
        public bool? ShowSelectionHighlight { get; set; }
        public bool? Wireframe { get; set; }
        public int? ModelDisplayMode { get; set; }
    }

    private sealed class LightSettingsDocument
    {
        public const int CurrentFormatVersion = 1;
        public int FormatVersion { get; set; } = CurrentFormatVersion;
        public List<SceneLight>? Lights { get; set; } = [];
        public bool ShowLightGizmos { get; set; }
    }

    private sealed record MaterialLibraryItem(PbrMaterial Material)
    {
        public override string ToString() => Material.Name;
    }

    private sealed class MeshInspector
    {
        private readonly SceneModel _model;
        private readonly int _meshIndex;
        private readonly TransformState _transform;
        private readonly Action _changed;

        public MeshInspector(SceneModel model, int meshIndex, Action changed)
        {
            _model = model;
            _meshIndex = meshIndex;
            _transform = model.GetOrCreateMeshTransform(meshIndex);
            _changed = changed;
        }

        [Category("Mesh")]
        [DisplayName("名稱")]
        [ReadOnly(true)]
        public string Name => _model.Meshes[_meshIndex].Name;

        [Category("Mesh")]
        [DisplayName("顯示")]
        public bool Visible
        {
            get => !_model.HiddenMeshIndices.Contains(_meshIndex);
            set
            {
                if (value)
                    _model.HiddenMeshIndices.Remove(_meshIndex);
                else if (!_model.HiddenMeshIndices.Contains(_meshIndex))
                    _model.HiddenMeshIndices.Add(_meshIndex);
                _changed();
            }
        }

        [Category("局部位置")]
        public float X { get => _transform.Position.X; set { _transform.Position = _transform.Position with { X = value }; _changed(); } }
        [Category("局部位置")]
        public float Y { get => _transform.Position.Y; set { _transform.Position = _transform.Position with { Y = value }; _changed(); } }
        [Category("局部位置")]
        public float Z { get => _transform.Position.Z; set { _transform.Position = _transform.Position with { Z = value }; _changed(); } }
        [Category("局部旋轉（度）")]
        public float RotationX { get => _transform.RotationDegrees.X; set { _transform.RotationDegrees = _transform.RotationDegrees with { X = value }; _changed(); } }
        [Category("局部旋轉（度）")]
        public float RotationY { get => _transform.RotationDegrees.Y; set { _transform.RotationDegrees = _transform.RotationDegrees with { Y = value }; _changed(); } }
        [Category("局部旋轉（度）")]
        public float RotationZ { get => _transform.RotationDegrees.Z; set { _transform.RotationDegrees = _transform.RotationDegrees with { Z = value }; _changed(); } }
        [Category("局部縮放")]
        public float ScaleX { get => _transform.Scale.X; set { _transform.Scale = _transform.Scale with { X = Math.Max(value, 0.0001f) }; _changed(); } }
        [Category("局部縮放")]
        public float ScaleY { get => _transform.Scale.Y; set { _transform.Scale = _transform.Scale with { Y = Math.Max(value, 0.0001f) }; _changed(); } }
        [Category("局部縮放")]
        public float ScaleZ { get => _transform.Scale.Z; set { _transform.Scale = _transform.Scale with { Z = Math.Max(value, 0.0001f) }; _changed(); } }
    }

    private sealed class MaterialInspector(PbrMaterial material, Action changed)
    {
        [Category("材質")]
        [Description("材質名稱，用來辨識、儲存及套用材質。")]
        public string Name { get => material.Name; set { material.Name = value; changed(); } }
        [Category("PBR")]
        [Description("材質的基本顏色；RGB 控制表面顏色，Alpha 參與透明度計算。若有 BaseColor 貼圖，顏色會與貼圖相乘。")]
        [Editor(typeof(MainFormRichColorEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [TypeConverter(typeof(RichColorConverter))]
        public Color BaseColor { get => ToColor(material.BaseColor); set { material.BaseColor = ToVector(value); changed(); } }
        [Category("PBR")]
        [Description("金屬度，範圍 0～1。0 代表非金屬，1 代表金屬；會影響反射顏色與漫反射比例。")]
        public float Metallic { get => material.Metallic; set { material.Metallic = Math.Clamp(value, 0f, 1f); changed(); } }
        [Category("PBR")]
        [Description("粗糙度，範圍 0～1。數值越小反射越清晰，數值越大反射越模糊。")]
        public float Roughness { get => material.Roughness; set { material.Roughness = Math.Clamp(value, 0f, 1f); changed(); } }
        [Category("PBR")]
        [Description("法線貼圖強度。0 代表忽略法線貼圖，1 代表原始強度，較大的值會加強表面凹凸效果。")]
        public float NormalScale { get => material.NormalScale; set { material.NormalScale = Math.Clamp(value, 0f, 4f); changed(); } }
        [Category("PBR")]
        [Description("環境遮蔽強度，範圍 0～1。用來降低縫隙、角落及遮蔽區域接收到的間接光。")]
        public float AmbientOcclusion { get => material.AmbientOcclusion; set { material.AmbientOcclusion = Math.Clamp(value, 0f, 1f); changed(); } }
        [Category("PBR")]
        [Description("自發光顏色。決定材質自行發出的光色，不需要外部燈光即可顯示。")]
        [Editor(typeof(MainFormRichColorEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [TypeConverter(typeof(RichColorConverter))]
        public Color Emissive { get => Color.FromArgb(255, ToByte(material.Emissive.X), ToByte(material.Emissive.Y), ToByte(material.Emissive.Z)); set { material.Emissive = new Vector3(value.R / 255f, value.G / 255f, value.B / 255f); changed(); } }
        [Category("PBR")]
        [Description("自發光強度。0 表示不發光；數值越大，材質顯示越明亮。")]
        public float EmissiveStrength { get => material.EmissiveStrength; set { material.EmissiveStrength = Math.Max(0f, value); changed(); } }
        [Category("PBR")]
        [Description("整體不透明度，範圍 0～1。0 完全透明，1 完全不透明；會與 BaseColor Alpha 及透明貼圖相乘。")]
        public float Opacity { get => material.Opacity; set { material.Opacity = Math.Clamp(value, 0f, 1f); changed(); } }
        [Category("PBR")]
        [Description("是否顯示模型正反兩面。適合薄片、布料或只有單面幾何的模型。")]
        public bool DoubleSided { get => material.DoubleSided; set { material.DoubleSided = value; changed(); } }
        [Category("渲染")]
        [DisplayName("材質模式")]
        [Description("決定材質使用自動、不透明、Alpha Test、透明或玻璃等渲染方式。")]
        public MaterialRenderMode RenderMode { get => material.RenderMode; set { material.RenderMode = value; changed(); } }
        [Category("渲染")]
        [DisplayName("Alpha 截斷值")]
        [Description("Alpha Test 的裁切門檻。透明度低於此值的像素會被完全捨棄，適合樹葉或柵欄貼圖。")]
        public float AlphaCutoff { get => material.AlphaCutoff; set { material.AlphaCutoff = Math.Clamp(value, 0f, 1f); changed(); } }
        [Category("玻璃")]
        [DisplayName("透射率")]
        [Description("光線穿透材質的比例，範圍 0～1。數值越高，材質越接近透明介質。")]
        public float Transmission { get => material.Transmission; set { material.Transmission = Math.Clamp(value, 0f, 1f); changed(); } }
        [Category("玻璃")]
        [DisplayName("折射率 IOR")]
        [Description("控制光線進出透明介質時的折射程度。空氣約 1.0、水約 1.33、一般玻璃約 1.5。")]
        public float IndexOfRefraction { get => material.IndexOfRefraction; set { material.IndexOfRefraction = Math.Clamp(value, 1f, 2.5f); changed(); } }
        [Category("玻璃")]
        [DisplayName("厚度")]
        [Description("透明介質的光學厚度，用於吸收與透射效果；數值越大，吸收顏色通常越明顯。")]
        public float Thickness { get => material.Thickness; set { material.Thickness = Math.Clamp(value, 0f, 10f); changed(); } }
        [Category("玻璃")]
        [DisplayName("折射強度")]
        [Description("即時預覽中的折射偏移強度。數值越高，透過材質看到的背景扭曲越明顯。")]
        public float RefractionStrength { get => material.RefractionStrength; set { material.RefractionStrength = Math.Clamp(value, 0f, 0.25f); changed(); } }
        [Category("玻璃")]
        [DisplayName("色散")]
        [Description("不同波長光線的折射差異，範圍 0～1。鑽石可使用約 0.044，會產生彩虹般的火彩。")]
        public float Dispersion { get => material.Dispersion; set { material.Dispersion = Math.Clamp(value, 0f, 1f); changed(); } }
        [Category("玻璃")]
        [DisplayName("吸收顏色")]
        [Description("光線穿過透明介質後保留下來的顏色，用於模擬有色玻璃或液體。")]
        [Editor(typeof(MainFormRichColorEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [TypeConverter(typeof(RichColorConverter))]
        public Color AbsorptionColor
        {
            get => Color.FromArgb(255, ToByte(material.AbsorptionColor.X), ToByte(material.AbsorptionColor.Y), ToByte(material.AbsorptionColor.Z));
            set { material.AbsorptionColor = new Vector3(value.R / 255f, value.G / 255f, value.B / 255f); changed(); }
        }
        [Category("貼圖"), ReadOnly(true)]
        [Description("顯示目前已啟用的 PBR 貼圖種類；此欄位僅供檢視。")]
        public string TextureSummary => string.Join(", ", material.Textures.Where(x => x.Value.Enabled).Select(x => x.Key));
        private static byte ToByte(float value) => (byte)(Math.Clamp(value, 0f, 1f) * 255f);
        private static Color ToColor(Vector4 value) => Color.FromArgb(ToByte(value.W), ToByte(value.X), ToByte(value.Y), ToByte(value.Z));
        private static Vector4 ToVector(Color value) => new(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f);
    }

    private sealed class CameraInspector(CameraState camera, Action changed)
    {
        [Category("Camera From")]
        public float FromX { get => camera.From.X; set { camera.From = camera.From with { X = value }; CameraController.UpdateUpFromRoll(camera); changed(); } }
        [Category("Camera From")]
        public float FromY { get => camera.From.Y; set { camera.From = camera.From with { Y = value }; CameraController.UpdateUpFromRoll(camera); changed(); } }
        [Category("Camera From")]
        public float FromZ { get => camera.From.Z; set { camera.From = camera.From with { Z = value }; CameraController.UpdateUpFromRoll(camera); changed(); } }
        [Category("Camera To")]
        public float ToX { get => camera.To.X; set { camera.To = camera.To with { X = value }; CameraController.UpdateUpFromRoll(camera); changed(); } }
        [Category("Camera To")]
        public float ToY { get => camera.To.Y; set { camera.To = camera.To with { Y = value }; CameraController.UpdateUpFromRoll(camera); changed(); } }
        [Category("Camera To")]
        public float ToZ { get => camera.To.Z; set { camera.To = camera.To with { Z = value }; CameraController.UpdateUpFromRoll(camera); changed(); } }
        [Category("Camera Rotation")]
        [DisplayName("Roll（度）")]
        public float Roll { get => camera.RollDegrees; set { camera.RollDegrees = value; CameraController.UpdateUpFromRoll(camera); changed(); } }
        [Category("Projection")]
        public float Fov { get => camera.FieldOfViewDegrees; set { camera.FieldOfViewDegrees = value; changed(); } }
        [Category("Projection")]
        public float Near { get => camera.NearPlane; set { camera.NearPlane = value; camera.Validate(); changed(); } }
        [Category("Projection")]
        public float Far { get => camera.FarPlane; set { camera.FarPlane = value; camera.Validate(); changed(); } }
    }

    private sealed class LightInspector(SceneLight light, Action changed)
    {
        [Category("燈光")]
        public string Name { get => light.Name; set { light.Name = value; changed(); } }
        [Category("燈光")]
        public bool Enabled { get => light.Enabled; set { light.Enabled = value; changed(); } }
        [Category("燈光")]
        [DisplayName("類型")]
        public SceneLightType Type { get => light.Type; set { light.Type = value; changed(); } }
        [Category("位置")]
        public float PositionX { get => light.Position.X; set { light.Position = light.Position with { X = value }; changed(); } }
        [Category("位置")]
        public float PositionY { get => light.Position.Y; set { light.Position = light.Position with { Y = value }; changed(); } }
        [Category("位置")]
        public float PositionZ { get => light.Position.Z; set { light.Position = light.Position with { Z = value }; changed(); } }
        [Category("顏色與強度")]
        [Editor(typeof(MainFormRichColorEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [TypeConverter(typeof(RichColorConverter))]
        public Color Color
        {
            get => Color.FromArgb(255, ToByte(light.Color.X), ToByte(light.Color.Y), ToByte(light.Color.Z));
            set { light.Color = new Vector3(value.R / 255f, value.G / 255f, value.B / 255f); changed(); }
        }
        [Category("顏色與強度")]
        public float Intensity { get => light.Intensity; set { light.Intensity = Math.Max(0f, value); changed(); } }
        [Category("點光源 / 聚光燈")]
        public float Range { get => light.Range; set { light.Range = Math.Max(0.01f, value); changed(); } }
        [Category("聚光燈")]
        [DisplayName("Fall In（度）")]
        public float FallIn { get => light.FallInDegrees; set { light.FallInDegrees = Math.Clamp(value, 0.1f, light.FallOffDegrees); changed(); } }
        [Category("聚光燈")]
        [DisplayName("Fall Off（度）")]
        public float FallOff { get => light.FallOffDegrees; set { light.FallOffDegrees = Math.Clamp(value, light.FallInDegrees, 89.5f); changed(); } }
        [Category("方向")]
        public float DirectionX { get => light.Direction.X; set { SetDirection(light.Direction with { X = value }); } }
        [Category("方向")]
        public float DirectionY { get => light.Direction.Y; set { SetDirection(light.Direction with { Y = value }); } }
        [Category("方向")]
        public float DirectionZ { get => light.Direction.Z; set { SetDirection(light.Direction with { Z = value }); } }

        private void SetDirection(Vector3 direction)
        {
            light.Direction = direction.LengthSquared() < 0.000001f ? new Vector3(0f, -1f, 0f) : Vector3.Normalize(direction);
            changed();
        }

        private static byte ToByte(float value) => (byte)(Math.Clamp(value, 0f, 1f) * 255f);
    }

    public sealed class MainFormRichColorEditor : System.Drawing.Design.UITypeEditor
    {
        public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(
            ITypeDescriptorContext? context) => System.Drawing.Design.UITypeEditorEditStyle.Modal;

        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            var originalColor = value is Color color ? color : Color.White;
            var committedColor = originalColor;
            using var dialog = new RichColorPickerForm(originalColor);

            void Preview(Color previewColor)
            {
                if (context?.Instance is null || context.PropertyDescriptor is null) return;
                context.PropertyDescriptor.SetValue(context.Instance, previewColor);
                context.OnComponentChanged();
            }

            dialog.PreviewColorChanged += (_, _) => Preview(dialog.SelectedColor);
            dialog.ApplyRequested += (_, _) =>
            {
                Preview(dialog.SelectedColor);
                committedColor = dialog.SelectedColor;
            };
            if (dialog.ShowDialog(Form.ActiveForm) == DialogResult.OK)
                return dialog.SelectedColor;

            Preview(committedColor);
            return committedColor;
        }
    }

    private sealed class EnvironmentInspector(EnvironmentSettings environment, Action changed)
    {
        [Category("環境")]
        [DisplayName("啟用 HDRI")]
        public bool Enabled { get => environment.Enabled; set { environment.Enabled = value; changed(); } }

        [Category("環境")]
        [DisplayName("HDR 檔案")]
        [ReadOnly(true)]
        public string Path => environment.Path;

        [Category("光照")]
        [DisplayName("強度")]
        public float Intensity { get => environment.Intensity; set { environment.Intensity = Math.Clamp(value, 0f, 20f); changed(); } }

        [Category("光照")]
        [DisplayName("水平旋轉（度）")]
        public float Rotation { get => environment.RotationDegrees; set { environment.RotationDegrees = value; environment.Validate(); changed(); } }

        [Category("背景")]
        [DisplayName("顯示 HDRI 背景")]
        public bool ShowBackground { get => environment.ShowBackground; set { environment.ShowBackground = value; changed(); } }

        [Category("背景")]
        [DisplayName("背景模糊")]
        public float BackgroundBlur { get => environment.BackgroundBlur; set { environment.BackgroundBlur = Math.Clamp(value, 0f, 1f); changed(); } }
    }

    private sealed record ThemePalette(
        Color Window,
        Color Control,
        Color Button,
        Color Text,
        Color Border,
        Color Selection)
    {
        public static ThemePalette Light { get; } = new(
            SystemColors.Window,
            SystemColors.Control,
            SystemColors.ControlLight,
            SystemColors.ControlText,
            SystemColors.ControlDark,
            SystemColors.Highlight);

        public static ThemePalette Dark { get; } = new(
            Color.FromArgb(30, 30, 30),
            Color.FromArgb(42, 42, 42),
            Color.FromArgb(58, 58, 58),
            Color.FromArgb(235, 235, 235),
            Color.FromArgb(82, 82, 82),
            Color.FromArgb(0, 120, 215));
    }

    private sealed class ViewerColorTable(ThemePalette palette) : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => palette.Control;
        public override Color ToolStripGradientMiddle => palette.Control;
        public override Color ToolStripGradientEnd => palette.Control;
        public override Color MenuStripGradientBegin => palette.Control;
        public override Color MenuStripGradientEnd => palette.Control;
        public override Color StatusStripGradientBegin => palette.Control;
        public override Color StatusStripGradientEnd => palette.Control;
        public override Color ToolStripDropDownBackground => palette.Window;
        public override Color ImageMarginGradientBegin => palette.Control;
        public override Color ImageMarginGradientMiddle => palette.Control;
        public override Color ImageMarginGradientEnd => palette.Control;
        public override Color MenuItemSelected => palette.Selection;
        public override Color MenuItemSelectedGradientBegin => palette.Selection;
        public override Color MenuItemSelectedGradientEnd => palette.Selection;
        public override Color MenuItemPressedGradientBegin => palette.Selection;
        public override Color MenuItemPressedGradientMiddle => palette.Selection;
        public override Color MenuItemPressedGradientEnd => palette.Selection;
        public override Color ButtonSelectedGradientBegin => palette.Selection;
        public override Color ButtonSelectedGradientEnd => palette.Selection;
        public override Color ButtonPressedGradientBegin => palette.Selection;
        public override Color ButtonPressedGradientEnd => palette.Selection;
        public override Color SeparatorDark => palette.Border;
        public override Color SeparatorLight => palette.Border;
        public override Color ToolStripBorder => palette.Border;
        public override Color MenuBorder => palette.Border;
        public override Color MenuItemBorder => palette.Border;
    }

    private sealed class ViewerToolStripRenderer(ThemePalette palette)
        : ToolStripProfessionalRenderer(new ViewerColorTable(palette))
    {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled
                ? palette.Text
                : Color.FromArgb(
                    (palette.Text.R + palette.Control.R) / 2,
                    (palette.Text.G + palette.Control.G) / 2,
                    (palette.Text.B + palette.Control.B) / 2);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = e.Item?.Enabled == true ? palette.Text : palette.Border;
            base.OnRenderArrow(e);
        }
    }

    private void mainStatusStrip_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
    {

    }

    private void mainMenuStrip_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
    {

    }

    private void ShowAboutDialog()
    {
        using var aboutForm = new AboutForm(FIsDarkMode);
        aboutForm.ShowDialog(this);
    }

    private void 關於ToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        ShowAboutDialog();
    }

    private void _materialNodeContextMenu_Opening(object? sender, CancelEventArgs e)
    {

    }
}
