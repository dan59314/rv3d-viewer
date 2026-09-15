namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.ComponentModel;
using System.Numerics;
using System.Text.Json;
using Rv3dViewer.Plugin.Abstractions;

internal sealed partial class RVInteriorDesignForm : Form
{
    private enum ModelSelectionMode { Navigate, Select, Subtract }
    private enum ExplanationKind { Information, Warning, Error }

    private Control? _maximizedViewport;
    private IPluginContext? _context;
    private IInteriorParametricParameters? _draftParameters;
    private Rv3dViewer.Core.SceneModel? _draftPreviewModel;
    private ParametricDesignObject? _selectedDesignObject;
    private int? _selectedMeshIndex;
    private readonly HashSet<Guid> _selectedModelIds = [];
    private ModelSelectionMode _modelSelectionMode;
    private bool _syncingSceneSelection;
    private Guid? _transformSnapshotModelId;
    private Vector3 _transformSnapshotPosition;
    private Vector3 _transformSnapshotRotation;
    private Vector3 _transformSnapshotScale = Vector3.One;
    private InteriorMaterialPreset? _selectedMaterialPreset;
    private InteriorTextureApplicationSettings? _selectedTextureSettings;
    private readonly List<ParametricDesignObject> _designObjects = [];
    private bool _uiSettingsRestored;
    private bool _uiSettingsRestoring;
    private bool _syncingAssetPlacementSplitters;
    private bool _syncingAssetPreviewSplitters;
    private bool _hasUnexportedChanges;
    private bool _showModelEdges = true;
    private bool _showModelDimensions;
    private bool _updatingVisibilityMenu;
    private bool _updatingDisplayMenu;
    private bool _updatingSnapSettings;
    private bool _pbrPreviewEnabled;
    private InteriorVisibilityMode _visibilityMode = InteriorVisibilityMode.AutoHideForegroundWalls;

    public RVInteriorDesignForm()
    {
        InitializeComponent();
        InitializeUnifiedMenu();
        InitializeAssetCatalog();
        InitializeMaterialCatalog();
        InitializeTextureCatalog();
        UpdateHistoryMenuState();
        UpdateProjectTitle();
    }

    public RVInteriorDesignForm(IPluginContext context) : this()
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private void ShowExplanation(string message, ExplanationKind kind = ExplanationKind.Information)
    {
        explanationLabel.Text = message;
        explanationLabel.ForeColor = kind switch
        {
            ExplanationKind.Error => Color.Red,
            ExplanationKind.Warning => Color.DarkOrange,
            _ => SystemColors.ControlText
        };
        statusLabel.Text = message;
    }

    private async void RVInteriorDesignForm_Shown(object? sender, EventArgs e)
    {
        // Shown is raised by the running application, never by the WinForms Designer.
        // This keeps GLFW disabled while Visual Studio renders the form at design time.
        previewViewport.EnableRuntimeContext();
        if (_uiSettingsRestored)
            return;
        _uiSettingsRestored = true;
        _uiSettingsRestoring = true;
        var settings = RVInteriorDesignUserSettings.Load();
        ShowModelEdges(settings.ShowModelEdges);
        ShowModelDimensions(settings.ShowModelDimensions);
        ApplyVisibilityMode(settings.VisibilityMode);
        pbrPreviewToolButton.Checked = settings.UsePbrPreview;
        RestoreTransformSnapSettings(settings);
        _ = RefreshSceneTemplatesAsync();
        var savedBounds = new Rectangle(settings.WindowX, settings.WindowY, settings.WindowWidth, settings.WindowHeight);
        if (settings.WindowWidth >= MinimumSize.Width && settings.WindowHeight >= MinimumSize.Height &&
            Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(savedBounds)))
        {
            //WindowState = FormWindowState.Normal;
            StartPosition = FormStartPosition.Manual;
            Bounds = savedBounds;
        }

        if (settings.IsMaximized)
        {
            // WindowState restoration is currently disabled; preserve the saved flag for future use.
        }
        BeginInvoke(new Action(() =>
        {
            var designSplitterDistance = settings.LayoutVersion >= 2
                ? settings.DesignSplitterDistance
                : settings.DesignSplitterDistance + settings.WorkspaceSplitterDistance;
            ApplySplitterDistance(designSplitContainer, designSplitterDistance);
            ApplySplitterDistance(rightWorkspaceSplitContainer, settings.RightWorkspaceSplitterDistance);
            ApplySplitterDistance(sceneTemplateSplitContainer, settings.LayoutVersion >= 5
                ? settings.SceneTemplateSplitterDistance
                : 520);
            ApplySplitterDistance(modelLibrarySplitContainer, settings.ModelLibrarySplitterDistance);
            rightPanelTabControl.SelectedIndex = Math.Clamp(settings.RightPanelSelectedIndex, 0,
                rightPanelTabControl.TabCount - 1);
            libraryTabControl.SelectedIndex = Math.Clamp(settings.LibrarySelectedIndex, 0,
                libraryTabControl.TabCount - 1);
            polyHavenAssetBrowserControl.SelectedProviderIndex = settings.OnlineAssetProviderIndex;
            assetTabControl.SelectedIndex = Math.Clamp(settings.AssetCategorySelectedIndex, 0,
                assetTabControl.TabCount - 1);
            UpdateActiveAssetCategoryThumbnailLoading();
            foreach (var control in GetAssetCategoryControls())
            {
                control.SetPlacementPanelHeight(settings.AssetPlacementPanelHeight);
                control.SetPreviewPanelHeight(settings.AssetPreviewPanelHeight);
            }
            polyHavenAssetBrowserControl.SetPreviewPanelHeight(settings.OnlineAssetPreviewPanelHeight);
            _uiSettingsRestoring = false;
        }));
        await Task.Yield();
        if (_context is not null)
            await RestoreSavedSessionAsync();
    }

    private void RVInteriorDesignForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && !ConfirmSaveProjectChanges())
        {
            e.Cancel = true;
            return;
        }
        if (e.CloseReason == CloseReason.UserClosing && _designObjects.Count > 0)
        {
            var result = MessageBox.Show(this,
                $"目前 RV室內設計中有 {_designObjects.Count} 個模型。\r\n\r\n" +
                "是否將目前的模型、Mesh、材質與貼圖資訊套用到 MainForm？\r\n\r\n" +
                "選擇「否」將只保存 Plugin 工作階段，不套用到 MainForm。",
                "關閉 RV室內設計", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            if (result == DialogResult.Yes && !TryExportAllDesignObjects())
            {
                e.Cancel = true;
                return;
            }
        }
        if (!TrySaveSession(out var saveError))
        {
            var closeWithoutSaving = MessageBox.Show(this,
                $"無法保存目前的室內設計工作階段：\r\n\r\n{saveError}\r\n\r\n仍要關閉嗎？",
                "RV室內設計保存失敗", MessageBoxButtons.YesNo, MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button2);
            if (closeWithoutSaving != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }
        CaptureUiSettings().Save();
        PrepareAssetImageControlsForClose();
        // Release the PBR renderer while the Form and GLControl handles are still valid.
        // Waiting for child-control disposal can make OpenTK attempt to recreate GLControl.
        previewViewport.PrepareForFormClose();
    }

    private void PrepareAssetImageControlsForClose()
    {
        foreach (var control in new[]
                 {
                     furnitureAssetControl, applianceAssetControl, lightingAssetControl,
                     doorsWindowsAssetControl, kitchenAssetControl, bathroomAssetControl,
                     storageAssetControl, decorAssetControl, plantsAssetControl,
                     otherAssetsControl, customAssetsControl
                 })
            control.PrepareForDispose();
        polyHavenAssetBrowserControl.PrepareForDispose();
    }

    private async Task RestoreSavedSessionAsync()
    {
        BeginOperationProgress("正在讀取上次的室內設計工作階段…");
        UseWaitCursor = true;
        var readProgress = new Progress<int>(percent => ReportOperationProgress(percent * 65 / 100,
            $"正在讀取上次的室內設計工作階段：{percent}%"));
        var loadResult = await Task.Run(() => InteriorDesignSessionStore.Load(readProgress));
        if (IsDisposed || Disposing) return;
        if (loadResult.Session is null)
        {
            if (!string.IsNullOrWhiteSpace(loadResult.ErrorMessage))
                MessageBox.Show(this, loadResult.ErrorMessage, "RV室內設計工作階段",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            EndOperationProgress();
            UseWaitCursor = false;
            return;
        }
        try
        {
            var progress = new Progress<int>(percent => ReportOperationProgress(65 + percent * 35 / 100,
                $"正在還原上次的室內設計模型：{percent}%"));
            var restored = await Task.Run(() =>
                InteriorDesignSessionStore.RestoreObjects(loadResult.Session, progress));
            if (IsDisposed || Disposing) return;
            _designObjects.Clear();
            _designObjects.AddRange(restored);
            _hasUnexportedChanges = loadResult.Session.HasUnexportedChanges;
            _currentProjectPath = string.IsNullOrWhiteSpace(loadResult.Session.ProjectFilePath)
                ? null
                : loadResult.Session.ProjectFilePath;
            _projectDirty = loadResult.Session.ProjectDirty;
            RebuildSceneTreeFromDesignObjects();
            RefreshViewportScene();

            var selectedId = loadResult.Session.SelectedModelId;
            var selectedNode = selectedId is Guid id
                ? FindDesignObjectNode(id)
                : null;
            if (selectedNode is not null && loadResult.Session.SelectedMeshIndex is int meshIndex)
                selectedNode = FindMeshNode(selectedNode, meshIndex) ?? selectedNode;
            if (selectedNode is not null)
                sceneTreeView.SelectedNode = selectedNode;
            UpdateProjectTitle();
            var recoveryText = loadResult.RecoveredFromBackup ? "（由備份還原）" : string.Empty;
            statusLabel.Text = $"已載入上次室內設計，共 {_designObjects.Count} 個模型{recoveryText}。";
            if (!string.IsNullOrWhiteSpace(loadResult.ErrorMessage))
                MessageBox.Show(this, loadResult.ErrorMessage, "RV室內設計工作階段",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException or
                                           NotSupportedException or ArgumentException or OutOfMemoryException)
        {
            MessageBox.Show(this, $"無法還原室內設計工作階段：\r\n\r\n{exception.Message}",
                "RV室內設計工作階段", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            if (!IsDisposed && !Disposing)
            {
                UseWaitCursor = false;
                EndOperationProgress();
            }
        }
    }

    private bool TrySaveSession(out string? errorMessage)
    {
        try
        {
            var session = InteriorDesignSessionStore.CreateHistorySession(
                _designObjects, _selectedDesignObject?.Id, _selectedMeshIndex, _hasUnexportedChanges);
            session.ProjectFilePath = _currentProjectPath;
            session.ProjectDirty = _projectDirty;
            InteriorDesignSessionStore.Save(session);
            errorMessage = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or
                                           NotSupportedException or OutOfMemoryException)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private void RebuildSceneTreeFromDesignObjects()
    {
        var root = sceneTreeView.Nodes[0];
        root.Nodes.Clear();
        var defaultObjects = _designObjects.Where(item => item.IsDefaultSceneObject).ToArray();
        if (defaultObjects.Length > 0)
        {
            var group = new TreeNode("預設室內裝潢模型") { Name = DefaultInteriorSceneFactory.SceneNodeName };
            foreach (var item in defaultObjects)
                group.Nodes.Add(CreateDesignObjectNode(item));
            root.Nodes.Add(group);
            group.Expand();
        }
        foreach (var item in _designObjects.Where(item => !item.IsDefaultSceneObject))
            root.Nodes.Add(CreateDesignObjectNode(item));
        root.Expand();
    }

    private static TreeNode CreateDesignObjectNode(ParametricDesignObject item)
    {
        var node = new TreeNode(item.Parameters.Name) { Tag = item };
        for (var meshIndex = 0; meshIndex < item.Model.Meshes.Count; meshIndex++)
        {
            var mesh = item.Model.Meshes[meshIndex];
            var meshName = string.IsNullOrWhiteSpace(mesh.Name) ? $"Mesh {meshIndex + 1}" : mesh.Name;
            node.Nodes.Add(new TreeNode(meshName)
            {
                Tag = new InteriorMeshTreeItem(item, meshIndex)
            });
        }
        return node;
    }

    private static TreeNode? FindMeshNode(TreeNode modelNode, int meshIndex) =>
        modelNode.Nodes.Cast<TreeNode>().FirstOrDefault(node =>
            node.Tag is InteriorMeshTreeItem item && item.MeshIndex == meshIndex);

    private static void RefreshDesignObjectNode(TreeNode modelNode, ParametricDesignObject item)
    {
        modelNode.Text = item.Parameters.Name;
        modelNode.Nodes.Clear();
        var refreshedNode = CreateDesignObjectNode(item);
        while (refreshedNode.Nodes.Count > 0)
            modelNode.Nodes.Add(refreshedNode.Nodes[0]);
    }

    private TreeNode? FindDesignObjectNode(Guid id)
    {
        static TreeNode? Find(TreeNode node, Guid id)
        {
            if (node.Tag is ParametricDesignObject item && item.Id == id)
                return node;
            foreach (TreeNode child in node.Nodes)
            {
                var found = Find(child, id);
                if (found is not null)
                    return found;
            }
            return null;
        }
        return Find(sceneTreeView.Nodes[0], id);
    }

    private void UiSplitterMoved(object? sender, SplitterEventArgs e)
    {
        if (_uiSettingsRestored && !_uiSettingsRestoring)
            CaptureUiSettings().Save();
    }

    private void AssetCategoryControl_PlacementSplitterMoved(object? sender, EventArgs e)
    {
        if (_syncingAssetPlacementSplitters || sender is not AssetCategoryControl source)
            return;
        _syncingAssetPlacementSplitters = true;
        try
        {
            foreach (var control in GetAssetCategoryControls().Where(control => !ReferenceEquals(control, source)))
                control.SetPlacementPanelHeight(source.PlacementPanelHeight);
        }
        finally
        {
            _syncingAssetPlacementSplitters = false;
        }
        if (_uiSettingsRestored && !_uiSettingsRestoring)
            CaptureUiSettings().Save();
    }

    private void PolyHavenAssetBrowserControl_PreviewSplitterMoved(object? sender, EventArgs e)
    {
        if (_uiSettingsRestored && !_uiSettingsRestoring)
            CaptureUiSettings().Save();
    }

    private void AssetCategoryControl_PreviewSplitterMoved(object? sender, EventArgs e)
    {
        if (_syncingAssetPreviewSplitters || sender is not AssetCategoryControl source)
            return;
        _syncingAssetPreviewSplitters = true;
        try
        {
            foreach (var control in GetAssetCategoryControls().Where(control => !ReferenceEquals(control, source)))
                control.SetPreviewPanelHeight(source.PreviewPanelHeight);
        }
        finally
        {
            _syncingAssetPreviewSplitters = false;
        }
        if (_uiSettingsRestored && !_uiSettingsRestoring)
            CaptureUiSettings().Save();
    }

    private void RightPanelTabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateActiveAssetCategoryThumbnailLoading();
        if (_uiSettingsRestored && !_uiSettingsRestoring)
            CaptureUiSettings().Save();
    }

    private RVInteriorDesignUserSettings CaptureUiSettings()
    {
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        if (bounds.Width < MinimumSize.Width || bounds.Height < MinimumSize.Height)
            bounds = Bounds;
        return new RVInteriorDesignUserSettings
        {
            DesignSplitterDistance = designSplitContainer.SplitterDistance,
            ModelLibrarySplitterDistance = modelLibrarySplitContainer.SplitterDistance,
            RightWorkspaceSplitterDistance = rightWorkspaceSplitContainer.SplitterDistance,
            SceneTemplateSplitterDistance = sceneTemplateSplitContainer.SplitterDistance,
            AssetPlacementPanelHeight = furnitureAssetControl.PlacementPanelHeight,
            AssetPreviewPanelHeight = furnitureAssetControl.PreviewPanelHeight,
            OnlineAssetPreviewPanelHeight = polyHavenAssetBrowserControl.PreviewPanelHeight,
            RightPanelSelectedIndex = rightPanelTabControl.SelectedIndex,
            LibrarySelectedIndex = libraryTabControl.SelectedIndex,
            AssetCategorySelectedIndex = assetTabControl.SelectedIndex,
            OnlineAssetProviderIndex = polyHavenAssetBrowserControl.SelectedProviderIndex,
            WindowX = bounds.X,
            WindowY = bounds.Y,
            WindowWidth = bounds.Width,
            WindowHeight = bounds.Height,
            IsMaximized = WindowState == FormWindowState.Maximized,
            ShowModelEdges = _showModelEdges,
            ShowModelDimensions = _showModelDimensions,
            VisibilityMode = _visibilityMode,
            UsePbrPreview = _pbrPreviewEnabled,
            TransformSnapEnabled = snapEnabledMenuItem.Checked,
            GridSnapEnabled = gridSnapMenuItem.Checked,
            SurfaceSnapEnabled = surfaceSnapMenuItem.Checked,
            MoveSnapCentimeters = GetCheckedSnapValue(GetMoveSnapMenuItems(), 10),
            RotationSnapDegrees = GetCheckedSnapValue(GetRotationSnapMenuItems(), 5),
            LayoutVersion = 5
        };
    }

    private static void ApplySplitterDistance(SplitContainer splitContainer, int distance)
    {
        var available = splitContainer.Orientation == Orientation.Vertical
            ? splitContainer.ClientSize.Width
            : splitContainer.ClientSize.Height;
        var maximum = Math.Max(splitContainer.Panel1MinSize,
            available - splitContainer.SplitterWidth - splitContainer.Panel2MinSize);
        splitContainer.SplitterDistance = Math.Clamp(distance, splitContainer.Panel1MinSize, maximum);
    }

    private void InitializeUnifiedMenu()
    {
        unifiedMenuStrip.Configure(
            "RV室內設計",
            "參數化室內模型、家具資產、貼圖與材質設計工作區。",
            Path.GetDirectoryName(typeof(RVInteriorDesignForm).Assembly.Location));
        unifiedMenuStrip.SetCapabilities(true, true, true, true, false, false, false, false);
    }

    private void DisplayOptionMenuItem_CheckedChanged(object? sender, EventArgs e)
    {
        if (_updatingDisplayMenu || _updatingVisibilityMenu) return;
        if (ReferenceEquals(sender, showModelEdgesMenuItem))
            ShowModelEdges(showModelEdgesMenuItem.Checked);
        else if (ReferenceEquals(sender, showModelDimensionsMenuItem))
            ShowModelDimensions(showModelDimensionsMenuItem.Checked);
        else if (ReferenceEquals(sender, transparentOccludersMenuItem))
            ToggleTransparentMode(transparentOccludersMenuItem.Checked);
        else if (ReferenceEquals(sender, autoHideForegroundWallsMenuItem))
            ToggleAutoHideMode(autoHideForegroundWallsMenuItem.Checked);
    }

    private void ShowModelDimensions(bool enabled)
    {
        _showModelDimensions = enabled;
        _updatingDisplayMenu = true;
        showModelDimensionsMenuItem.Checked = enabled;
        _updatingDisplayMenu = false;
        foreach (var viewport in GetViewports())
            viewport.ShowModelDimensions = enabled;
        if (_uiSettingsRestored && !_uiSettingsRestoring)
        {
            CaptureUiSettings().Save();
            ShowExplanation(enabled
                ? "已啟用模型尺寸；選取場景模型後，四視圖會顯示公尺尺寸標記。"
                : "已關閉模型尺寸標記。");
        }
    }

    private void EditModelMenuItem_DropDownOpening(object? sender, EventArgs e)
    {
        selectModelsMenuItem.Checked = _modelSelectionMode == ModelSelectionMode.Select;
        subtractModelsMenuItem.Checked = _modelSelectionMode == ModelSelectionMode.Subtract;
        selectAllModelsMenuItem.Enabled = _designObjects.Count > 0;
        clearModelSelectionMenuItem.Enabled = _selectedModelIds.Count > 0;
    }

    private void EditMenuItem_DropDownOpening(object? sender, EventArgs e)
    {
        deleteSelectedModelsMenuItem.Enabled = _selectedModelIds.Any(id =>
            _designObjects.Any(item => item.Id == id));
        clearAllModelsMenuItem.Enabled = _designObjects.Count > 0;
    }

    private void ModelSelectionModeMenuItem_Click(object? sender, EventArgs e)
    {
        var mode = ReferenceEquals(sender, subtractModelsMenuItem)
            ? ModelSelectionMode.Subtract
            : ModelSelectionMode.Select;
        SetModelSelectionMode(_modelSelectionMode == mode ? ModelSelectionMode.Navigate : mode);
    }

    private void ExitEditModeMenuItem_Click(object? sender, EventArgs e) =>
        ExitEditModeAndClearSelection();

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Shift | Keys.S))
        {
            SaveProject(true);
            return true;
        }
        if (keyData == (Keys.Control | Keys.S))
        {
            SaveProject(false);
            return true;
        }
        if (keyData == (Keys.Control | Keys.O))
        {
            UnifiedMenuStrip_ReadRequested(this, EventArgs.Empty);
            return true;
        }
        if (keyData == (Keys.Control | Keys.Z))
        {
            UndoInteriorEdit();
            return true;
        }
        if (keyData == (Keys.Control | Keys.Y))
        {
            RedoInteriorEdit();
            return true;
        }
        if ((keyData & Keys.KeyCode) == Keys.Delete &&
            (keyData & Keys.Modifiers) == Keys.None &&
            _modelSelectionMode != ModelSelectionMode.Navigate &&
            _selectedModelIds.Count > 0 && !IsTextEditingControlFocused())
        {
            DeleteSelectedModels();
            return true;
        }
        if ((keyData & Keys.KeyCode) == Keys.Escape && _pendingAssetPlacement is not null)
        {
            CancelAssetPlacement();
            return true;
        }
        if ((keyData & Keys.KeyCode) == Keys.Escape)
        {
            var canceledDrag = false;
            var transformChanged = false;
            foreach (var viewport in GetViewports())
            {
                if (!viewport.TryCancelGizmoDrag(out var viewportChanged))
                    continue;
                canceledDrag = true;
                transformChanged |= viewportChanged;
            }
            if (canceledDrag)
            {
                if (transformChanged)
                    UndoInteriorEdit();
                ShowExplanation("已取消目前的變換，並回復拖曳前的位置。");
                return true;
            }
        }
        if ((keyData & Keys.KeyCode) == Keys.Escape &&
            (_modelSelectionMode != ModelSelectionMode.Navigate || _selectedModelIds.Count > 0))
        {
            ExitEditModeAndClearSelection();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void SelectAllModelsMenuItem_Click(object? sender, EventArgs e) =>
        SetSelectedModels(_designObjects.Select(item => item.Id), _selectedDesignObject?.Id, null, true);

    private void ClearModelSelectionMenuItem_Click(object? sender, EventArgs e) =>
        SetSelectedModels([], null, null, true);

    private void ExitEditModeAndClearSelection()
    {
        SetTransformCollisionWarnings([]);
        SetModelSelectionMode(ModelSelectionMode.Navigate);
        SetSelectedModels([], null, null, true);
        statusLabel.Text = "已退出編輯並取消所有模型與 Mesh 選取。";
    }

    private void DeleteSelectedModelsMenuItem_Click(object? sender, EventArgs e) => DeleteSelectedModels();

    private void DeleteSelectedModels()
    {
        var selectedObjects = _designObjects
            .Where(item => _selectedModelIds.Contains(item.Id))
            .ToArray();
        if (selectedObjects.Length == 0)
            return;

        var targetText = selectedObjects.Length == 1
            ? $"模型「{selectedObjects[0].Parameters.Name}」"
            : $"選取的 {selectedObjects.Length} 個模型";
        var result = MessageBox.Show(this,
            $"確定要刪除{targetText}嗎？\r\n\r\n" +
            "此操作可使用 Undo 復原，且不會刪除先前已匯入 MainForm 的模型。",
            "刪除選取模型", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (result != DialogResult.Yes)
            return;

        RecordUndoSnapshot(selectedObjects.Length == 1
            ? $"刪除模型「{selectedObjects[0].Parameters.Name}」"
            : $"刪除 {selectedObjects.Length} 個模型");
        var deletedIds = selectedObjects.Select(item => item.Id).ToHashSet();
        _designObjects.RemoveAll(item => deletedIds.Contains(item.Id));
        RebuildSceneTreeFromDesignObjects();
        RefreshViewportScene();
        SetSelectedModels([], null, null, true);
        _hasUnexportedChanges = true;

        if (TrySaveSession(out var saveError))
            statusLabel.Text = $"已刪除{targetText}；可使用 Undo 復原。";
        else
            MessageBox.Show(this, $"模型已刪除，但無法更新工作階段檔：\r\n\r\n{saveError}",
                "RV室內設計保存失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        unifiedMenuStrip.RefreshCommandStates();
    }

    private bool IsTextEditingControlFocused()
    {
        Control? focused = this;
        while (focused is ContainerControl container && container.ActiveControl is { } active)
            focused = active;
        return focused is TextBoxBase or ComboBox or NumericUpDown or PropertyGrid or DataGridView;
    }

    private void SetModelSelectionMode(ModelSelectionMode mode)
    {
        _modelSelectionMode = mode;
        foreach (var viewport in GetViewports())
            viewport.CancelSelectionDrag();
        previewViewport.CancelSelectionDrag();
        if (mode != ModelSelectionMode.Navigate)
        {
            selectToolButton.Checked = true;
            moveToolButton.Checked = false;
            rotateToolButton.Checked = false;
            scaleToolButton.Checked = false;
            foreach (var viewport in GetViewports())
                viewport.TransformTool = InteriorTransformTool.Select;
        }
        selectModelsMenuItem.Checked = mode == ModelSelectionMode.Select;
        subtractModelsMenuItem.Checked = mode == ModelSelectionMode.Subtract;
        var viewportMode = mode == ModelSelectionMode.Subtract
            ? InteriorViewportSelectionMode.Subtract
            : mode == ModelSelectionMode.Select
                ? InteriorViewportSelectionMode.Select
                : InteriorViewportSelectionMode.Navigate;
        foreach (var viewport in GetViewports())
            viewport.SelectionMode = viewportMode;
        previewViewport.SelectionMode = viewportMode;
        statusLabel.Text = mode switch
        {
            ModelSelectionMode.Select => "模型選取：在 ViewPort 點擊模型；Ctrl 可保留既有選取。",
            ModelSelectionMode.Subtract => "模型減選：在 ViewPort 點擊已選模型以取消選取。",
            _ => "已退出模型選取模式，ViewPort 恢復相機操作。"
        };
    }

    private void ClearAllModelsMenuItem_Click(object? sender, EventArgs e)
    {
        if (_designObjects.Count == 0)
            return;
        var result = MessageBox.Show(this,
            $"確定要清除 RV室內設計中的全部 {_designObjects.Count} 個模型嗎？\r\n\r\n" +
            "此操作不會刪除先前已匯入 MainForm 的模型。",
            "清除所有模型", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (result != DialogResult.Yes)
            return;

        RecordUndoSnapshot("清除所有模型");
        _designObjects.Clear();
        _selectedDesignObject = null;
        _selectedMeshIndex = null;
        _selectedModelIds.Clear();
        UpdateApplyMaterialButtonState();
        _draftPreviewModel = null;
        parameterPropertyGrid.SelectedObject = null;
        objectPropertyGrid.SelectedObject = null;
        sceneTreeView.Nodes[0].Nodes.Clear();
        SelectViewportModels([], null, null);
        RefreshViewportScene();
        _hasUnexportedChanges = true;
        if (TrySaveSession(out var saveError))
            statusLabel.Text = "已清除 RV室內設計中的所有模型；先前匯入 MainForm 的模型不受影響。";
        else
            MessageBox.Show(this, $"模型已清除，但無法更新工作階段檔：\r\n\r\n{saveError}",
                "RV室內設計保存失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        unifiedMenuStrip.RefreshCommandStates();
    }

    private void ShowModelEdges(bool enabled)
    {
        _showModelEdges = enabled;
        foreach (var viewport in GetViewports())
            viewport.ShowModelEdges = enabled;
        _updatingDisplayMenu = true;
        showModelEdgesMenuItem.Checked = enabled;
        _updatingDisplayMenu = false;
        statusLabel.Text = enabled ? "已顯示模型框線。" : "已隱藏模型框線。";
        if (_uiSettingsRestored && !_uiSettingsRestoring)
            MarkProjectDirty();
    }

    private void ToggleTransparentMode(bool enabled)
    {
        if (!_updatingVisibilityMenu)
            ApplyVisibilityMode(enabled ? InteriorVisibilityMode.TransparentOccluders : InteriorVisibilityMode.Solid);
    }

    private void ToggleAutoHideMode(bool enabled)
    {
        if (!_updatingVisibilityMenu)
            ApplyVisibilityMode(enabled ? InteriorVisibilityMode.AutoHideForegroundWalls : InteriorVisibilityMode.Solid);
    }

    private void ApplyVisibilityMode(InteriorVisibilityMode mode)
    {
        if (!Enum.IsDefined(mode))
            mode = InteriorVisibilityMode.AutoHideForegroundWalls;
        _visibilityMode = mode;
        foreach (var viewport in GetViewports())
            viewport.VisibilityMode = mode;
        _updatingVisibilityMenu = true;
        transparentOccludersMenuItem.Checked = mode == InteriorVisibilityMode.TransparentOccluders;
        autoHideForegroundWallsMenuItem.Checked = mode == InteriorVisibilityMode.AutoHideForegroundWalls;
        _updatingVisibilityMenu = false;
        statusLabel.Text = mode switch
        {
            InteriorVisibilityMode.TransparentOccluders => "顯示模式：牆體與天花板半透明。",
            InteriorVisibilityMode.AutoHideForegroundWalls => "顯示模式：自動隱藏遮擋室內的前景牆。",
            _ => "顯示模式：實體。"
        };
        if (_uiSettingsRestored && !_uiSettingsRestoring)
            MarkProjectDirty();
    }

    private void InitializeAssetCatalog()
    {
        var assets = InteriorAssetFavoriteStore.Apply(
            InteriorAssetCatalog.CreateDefault().Concat(InteriorUserAssetCatalog.Load()));
        var controls = new Dictionary<string, AssetCategoryControl>
        {
            ["家具"] = furnitureAssetControl,
            ["電器"] = applianceAssetControl,
            ["燈具"] = lightingAssetControl,
            ["門窗"] = doorsWindowsAssetControl,
            ["廚房"] = kitchenAssetControl,
            ["衛浴"] = bathroomAssetControl,
            ["收納"] = storageAssetControl,
            ["軟裝"] = decorAssetControl,
            ["植栽"] = plantsAssetControl,
            ["其他"] = otherAssetsControl,
            ["使用者自訂"] = customAssetsControl
        };

        foreach (var category in InteriorAssetCatalog.Categories)
            controls[category].SetAssets(assets.Where(asset => asset.Category == category));
        UpdateActiveAssetCategoryThumbnailLoading();
        UpdateLibraryAssetCommandButtonStates();
    }

    private AssetCategoryControl[] GetAssetCategoryControls() =>
    [
        furnitureAssetControl, applianceAssetControl, lightingAssetControl, doorsWindowsAssetControl,
        kitchenAssetControl, bathroomAssetControl, storageAssetControl, decorAssetControl,
        plantsAssetControl, otherAssetsControl, customAssetsControl
    ];

    private void InitializeMaterialCatalog()
    {
        materialsListBox.BeginUpdate();
        try
        {
            materialsListBox.Items.Clear();
            materialsListBox.Items.AddRange(InteriorMaterialCatalog.CreateDefault().Cast<object>().ToArray());
        }
        finally
        {
            materialsListBox.EndUpdate();
        }
        UpdateApplyMaterialButtonState();
    }

    private void InitializeTextureCatalog()
    {
        texturesListBox.BeginUpdate();
        try
        {
            texturesListBox.Items.Clear();
            texturesListBox.Items.AddRange(InteriorTextureCatalog.CreateDefault().Cast<object>().ToArray());
        }
        finally
        {
            texturesListBox.EndUpdate();
        }
        UpdateApplyMaterialButtonState();
    }

    private void TexturesListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectedTextureSettings = texturesListBox.SelectedItem is InteriorTexturePreset preset
            ? new InteriorTextureApplicationSettings(preset)
            : null;
        textureSettingsPropertyGrid.SelectedObject = _selectedTextureSettings;
        UpdateApplyMaterialButtonState();
        statusLabel.Text = _selectedTextureSettings is null
            ? "請選取預設貼圖。"
            : $"已選取貼圖：{_selectedTextureSettings.Name}；可調整映射後套用至選取模型或 Mesh。";
    }

    private void TextureSettingsPropertyGrid_PropertyValueChanged(object? sender, PropertyValueChangedEventArgs e)
    {
        textureSettingsPropertyGrid.Refresh();
        UpdateApplyMaterialButtonState();
    }

    private void ApplyTextureButton_Click(object? sender, EventArgs e)
    {
        if (_selectedTextureSettings is null || _selectedDesignObject is null || _selectedModelIds.Count == 0)
            return;

        var targets = _selectedMeshIndex is int selectedMeshIndex
            ? new[] { (_selectedDesignObject, MeshIndex: selectedMeshIndex) }
            : _designObjects.Where(item => _selectedModelIds.Contains(item.Id))
                .SelectMany(item => Enumerable.Range(0, item.Model.Meshes.Count)
                    .Select(meshIndex => (item, MeshIndex: meshIndex)))
                .ToArray();
        var applicableTargets = targets.Where(target =>
                (uint)target.MeshIndex < (uint)target.Item1.Model.Meshes.Count &&
                CanApplyTextureMapping(target.Item1.Model.Meshes[target.MeshIndex],
                    _selectedTextureSettings.MappingMode))
            .ToArray();
        var skippedCount = targets.Length - applicableTargets.Length;
        if (applicableTargets.Length == 0)
        {
            ShowExplanation(_selectedTextureSettings.MappingMode == InteriorTextureMappingMode.現有UV
                    ? "無法套用貼圖：選取的 Mesh 沒有完整 UV，請改用平面、箱型、三平面、圓柱或球狀映射。"
                    : "無法套用貼圖：目前選取範圍沒有有效 Mesh。",
                ExplanationKind.Error);
            return;
        }

        RecordUndoSnapshot($"套用貼圖「{_selectedTextureSettings.Name}」");
        foreach (var (item, meshIndex) in applicableTargets)
            ApplyTextureToMesh(item.Model, meshIndex, _selectedTextureSettings);
        foreach (var item in applicableTargets.Select(target => target.Item1).Distinct())
            item.Model.CaptureMeshMaterialIndices();
        _hasUnexportedChanges = true;
        UpdateSelectionInspector();
        objectPropertyGrid.Refresh();
        RefreshViewportScene();
        var resultMessage = $"貼圖套用完成：已將「{_selectedTextureSettings.Name}」以" +
                            $"{_selectedTextureSettings.MappingMode}映射套用至 {applicableTargets.Length} 個 Mesh。";
        if (skippedCount > 0)
            resultMessage += $"另有 {skippedCount} 個 Mesh 因缺少完整 UV 而略過。";
        ShowExplanation(resultMessage,
            skippedCount > 0 ? ExplanationKind.Warning : ExplanationKind.Information);
    }

    private static bool CanApplyTextureMapping(Rv3dViewer.Core.MeshData mesh,
        InteriorTextureMappingMode mappingMode) =>
        mesh.Positions.Length > 0 &&
        (mappingMode != InteriorTextureMappingMode.現有UV ||
         mesh.TextureCoordinates.Length == mesh.Positions.Length);

    private static void ApplyTextureToMesh(Rv3dViewer.Core.SceneModel model, int meshIndex,
        InteriorTextureApplicationSettings settings)
    {
        if ((uint)meshIndex >= (uint)model.Meshes.Count)
            return;
        var mesh = model.Meshes[meshIndex];
        ApplyGeneratedTextureCoordinates(mesh, settings.MappingMode);
        var sourceMaterial = (uint)mesh.MaterialIndex < (uint)model.Materials.Count
            ? model.Materials[mesh.MaterialIndex]
            : new Rv3dViewer.Core.PbrMaterial { Name = "RV室內預設材質" };
        var material = sourceMaterial.Clone($"RV Mesh {meshIndex + 1}｜{settings.Name}");
        material.Textures.Remove(Rv3dViewer.Core.TextureSemantic.BaseColor);
        material.TextureStacks[Rv3dViewer.Core.TextureSemantic.BaseColor] =
            settings.CreateTextureStack();
        mesh.MaterialIndex = SetMeshSpecificMaterial(model, meshIndex, material);
    }

    private static int SetMeshSpecificMaterial(Rv3dViewer.Core.SceneModel model, int meshIndex,
        Rv3dViewer.Core.PbrMaterial material)
    {
        var marker = $"RV Mesh {meshIndex + 1}｜";
        var currentIndex = model.Meshes[meshIndex].MaterialIndex;
        if ((uint)currentIndex < (uint)model.Materials.Count &&
            model.Materials[currentIndex].Name.StartsWith(marker, StringComparison.Ordinal))
        {
            model.Materials[currentIndex] = material;
            return currentIndex;
        }
        model.Materials.Add(material);
        return model.Materials.Count - 1;
    }

    private static void ApplyGeneratedTextureCoordinates(Rv3dViewer.Core.MeshData mesh,
        InteriorTextureMappingMode mappingMode)
    {
        if (mappingMode is not (InteriorTextureMappingMode.圓柱 or InteriorTextureMappingMode.球狀) ||
            mesh.Positions.Length == 0)
            return;
        var minimum = mesh.Positions[0];
        var maximum = mesh.Positions[0];
        foreach (var position in mesh.Positions)
        {
            minimum = Vector3.Min(minimum, position);
            maximum = Vector3.Max(maximum, position);
        }
        var center = (minimum + maximum) * .5f;
        var height = Math.Max(maximum.Y - minimum.Y, .0001f);
        mesh.TextureCoordinates = mesh.Positions.Select(position =>
        {
            var relative = position - center;
            var u = .5f + MathF.Atan2(relative.Z, relative.X) / (2f * MathF.PI);
            if (mappingMode == InteriorTextureMappingMode.圓柱)
                return new Vector2(u, (position.Y - minimum.Y) / height);
            var direction = relative.LengthSquared() > .0000001f
                ? Vector3.Normalize(relative)
                : Vector3.UnitY;
            return new Vector2(u, .5f - MathF.Asin(Math.Clamp(direction.Y, -1f, 1f)) / MathF.PI);
        }).ToArray();
    }

    private void MaterialsListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectedMaterialPreset = materialsListBox.SelectedItem as InteriorMaterialPreset;
        materialPresetPropertyGrid.SelectedObject = _selectedMaterialPreset;
        UpdateApplyMaterialButtonState();
        statusLabel.Text = _selectedMaterialPreset is null
            ? "請選取預設材質。"
            : $"已選取材質：{_selectedMaterialPreset.Name}；請選取模型或 Mesh 後按「套用材質」。";
    }

    private void ApplyMaterialButton_Click(object? sender, EventArgs e)
    {
        if (_selectedMaterialPreset is null || _selectedDesignObject is null || _selectedModelIds.Count == 0)
            return;
        RecordUndoSnapshot($"套用材質「{_selectedMaterialPreset.Name}」");
        var targets = _selectedMeshIndex is int selectedMeshIndex
            ? new[] { (_selectedDesignObject, MeshIndex: selectedMeshIndex) }
            : _designObjects.Where(item => _selectedModelIds.Contains(item.Id))
                .SelectMany(item => Enumerable.Range(0, item.Model.Meshes.Count)
                    .Select(meshIndex => (item, MeshIndex: meshIndex)))
                .ToArray();
        foreach (var (item, meshIndex) in targets)
            ApplyMaterialToMesh(item.Model, meshIndex, _selectedMaterialPreset);
        foreach (var item in targets.Select(target => target.Item1).Distinct())
            item.Model.CaptureMeshMaterialIndices();
        _hasUnexportedChanges = true;
        UpdateSelectionInspector();
        objectPropertyGrid.Refresh();
        RefreshViewportScene();
        statusLabel.Text = $"已將材質「{_selectedMaterialPreset.Name}」套用至 {targets.Length} 個 Mesh。";
    }

    private static void ApplyMaterialToMesh(Rv3dViewer.Core.SceneModel model, int meshIndex,
        InteriorMaterialPreset preset)
    {
        if ((uint)meshIndex >= (uint)model.Meshes.Count)
            return;
        var materialName = preset.Material.Name;
        var mesh = model.Meshes[meshIndex];
        var currentMaterial = (uint)mesh.MaterialIndex < (uint)model.Materials.Count
            ? model.Materials[mesh.MaterialIndex]
            : null;
        var hasBaseColorTexture = currentMaterial is not null &&
                                  (currentMaterial.TextureStacks.ContainsKey(
                                       Rv3dViewer.Core.TextureSemantic.BaseColor) ||
                                   currentMaterial.Textures.ContainsKey(
                                       Rv3dViewer.Core.TextureSemantic.BaseColor));
        int materialIndex;
        if (hasBaseColorTexture)
        {
            var material = preset.CreateMaterial();
            material.Name = $"RV Mesh {meshIndex + 1}｜{preset.Name}";
            material.Textures = currentMaterial!.Textures.ToDictionary(pair => pair.Key, pair => new Rv3dViewer.Core.TextureSlot
            {
                Path = pair.Value.Path,
                Enabled = pair.Value.Enabled,
                UvChannel = pair.Value.UvChannel,
                Wrap = pair.Value.Wrap,
                Filter = pair.Value.Filter
            });
            material.TextureStacks = currentMaterial.TextureStacks.ToDictionary(pair => pair.Key,
                pair => pair.Value.Clone());
            materialIndex = SetMeshSpecificMaterial(model, meshIndex, material);
        }
        else
        {
            materialIndex = model.Materials.FindIndex(material =>
                string.Equals(material.Name, materialName, StringComparison.Ordinal));
            if (materialIndex < 0)
            {
                materialIndex = model.Materials.Count;
                model.Materials.Add(preset.CreateMaterial());
            }
        }
        mesh.MaterialIndex = materialIndex;
    }

    private void UpdateApplyMaterialButtonState()
    {
        var hasModelSelection = _selectedDesignObject is not null && _selectedModelIds.Count > 0;
        applyMaterialButton.Enabled = _selectedMaterialPreset is not null && hasModelSelection;
        applyTextureButton.Enabled = _selectedTextureSettings is not null && hasModelSelection;
    }

    private void Viewport_MaximizeRequested(object? sender, EventArgs e)
    {
        if (sender is not InteriorViewportControl viewport)
            return;

        if (_maximizedViewport is null)
            MaximizeViewport(viewport);
        else
            RestoreFourViewports();
    }

    private void PreviewViewport_MaximizeRequested(object? sender, EventArgs e)
    {
        if (_maximizedViewport is null)
            MaximizeViewport(previewViewport);
        else
            RestoreFourViewports();
    }

    private void Viewport_CameraChanged(object? sender, EventArgs e)
    {
        if (sender is not InteriorViewportControl viewport)
            return;
        var camera = viewport.Camera;
        if (ReferenceEquals(viewport, perspectiveViewport))
            previewViewport.SetCamera(camera);
        MarkProjectDirty();
        statusLabel.Text = $"{viewport.ViewName}｜{viewport.LastNavigationOperation}｜" +
                           $"Position ({camera.From.X:0.00}, {camera.From.Y:0.00}, {camera.From.Z:0.00})｜" +
                           $"Target ({camera.To.X:0.00}, {camera.To.Y:0.00}, {camera.To.Z:0.00})｜" +
                           $"FOV {camera.FieldOfViewDegrees:0.0}°｜Roll {camera.RollDegrees:0.0}°";
    }

    private void PreviewViewport_CameraChanged(object? sender, EventArgs e)
    {
        var camera = previewViewport.Camera;
        AdaptPerspectiveNearPlane(camera);
        InteriorPreviewViewportControl.CopyCamera(camera, perspectiveViewport.Camera);
        perspectiveViewport.Invalidate();
        MarkProjectDirty();
        statusLabel.Text = $"PBR 預覽｜Position ({camera.From.X:0.00}, {camera.From.Y:0.00}, {camera.From.Z:0.00})｜" +
                           $"Target ({camera.To.X:0.00}, {camera.To.Y:0.00}, {camera.To.Z:0.00})｜" +
                           $"FOV {camera.FieldOfViewDegrees:0.0}°｜Roll {camera.RollDegrees:0.0}°";
    }

    private void PreviewViewport_PreviewError(object? sender, string message)
    {
        statusLabel.Text = $"PBR 預覽：{message}";
        ShowExplanation($"PBR 預覽：{message}", ExplanationKind.Error);
    }

    private void Viewport_RenderError(object? sender, string message) =>
        ShowExplanation($"ViewPort 繪製失敗：{message}", ExplanationKind.Error);

    private static void AdaptPerspectiveNearPlane(Rv3dViewer.Core.CameraState camera)
    {
        var distance = Math.Max(Vector3.Distance(camera.From, camera.To), .0001f);
        camera.NearPlane = Math.Clamp(distance * .0025f, .0001f, .01f);
        camera.FarPlane = Math.Max(camera.FarPlane, camera.NearPlane + .01f);
    }

    private void PbrPreviewToolButton_CheckedChanged(object? sender, EventArgs e)
    {
        ApplyPerspectivePreviewMode(pbrPreviewToolButton.Checked);
        if (_uiSettingsRestored && !_uiSettingsRestoring)
            MarkProjectDirty();
    }

    private void ApplyPerspectivePreviewMode(bool enabled)
    {
        var perspectiveWasMaximized = ReferenceEquals(_maximizedViewport, perspectiveViewport) ||
                                      ReferenceEquals(_maximizedViewport, previewViewport);
        if (enabled)
        {
            var models = _designObjects.Select(item => item.Model)
                .Concat(_pendingAssetPlacement is null ? [] : [_pendingAssetPlacement.Model])
                .ToArray();
            previewViewport.SetSceneModels(models);
            previewViewport.SetCamera(perspectiveViewport.Camera);
        }
        else
            InteriorPreviewViewportControl.CopyCamera(previewViewport.Camera, perspectiveViewport.Camera);

        _pbrPreviewEnabled = enabled;
        if (perspectiveWasMaximized)
        {
            RestoreFourViewports();
            MaximizeViewport(enabled ? previewViewport : perspectiveViewport);
        }
        else if (_maximizedViewport is null)
        {
            perspectiveViewport.Visible = !enabled;
            previewViewport.Visible = enabled;
        }
        statusLabel.Text = enabled
            ? "右下角已切換為 MainForm PBR 預覽模式。"
            : "右下角已切換為快速設計透視模式。";
    }

    private void MaximizeViewport(Control viewport)
    {
        _maximizedViewport = viewport;
        foreach (var item in GetDisplayViewportControls())
            item.Visible = ReferenceEquals(item, viewport);

        viewportTableLayoutPanel.SetCellPosition(viewport, new TableLayoutPanelCellPosition(0, 0));
        viewportTableLayoutPanel.SetColumnSpan(viewport, 2);
        viewportTableLayoutPanel.SetRowSpan(viewport, 2);
        SetViewportMaximizedState(viewport, true);
        var viewName = ReferenceEquals(viewport, previewViewport)
            ? "PBR 預覽"
            : ((InteriorViewportControl)viewport).ViewName;
        statusLabel.Text = $"{viewName}已切換為單一視圖。";
    }

    private void RestoreFourViewports()
    {
        viewportTableLayoutPanel.SetCellPosition(topViewport, new TableLayoutPanelCellPosition(0, 0));
        viewportTableLayoutPanel.SetCellPosition(frontViewport, new TableLayoutPanelCellPosition(1, 0));
        viewportTableLayoutPanel.SetCellPosition(rightViewport, new TableLayoutPanelCellPosition(0, 1));
        viewportTableLayoutPanel.SetCellPosition(perspectiveViewport, new TableLayoutPanelCellPosition(1, 1));
        viewportTableLayoutPanel.SetCellPosition(previewViewport, new TableLayoutPanelCellPosition(1, 1));

        foreach (var item in GetDisplayViewportControls())
        {
            viewportTableLayoutPanel.SetColumnSpan(item, 1);
            viewportTableLayoutPanel.SetRowSpan(item, 1);
            SetViewportMaximizedState(item, false);
            item.Visible = !ReferenceEquals(item, perspectiveViewport) && !ReferenceEquals(item, previewViewport);
        }
        perspectiveViewport.Visible = !_pbrPreviewEnabled;
        previewViewport.Visible = _pbrPreviewEnabled;

        _maximizedViewport = null;
        statusLabel.Text = "已恢復四視圖。";
    }

    private InteriorViewportControl[] GetViewports() =>
        [topViewport, frontViewport, rightViewport, perspectiveViewport];

    private Control[] GetDisplayViewportControls() =>
        [topViewport, frontViewport, rightViewport, perspectiveViewport, previewViewport];

    private static void SetViewportMaximizedState(Control viewport, bool maximized)
    {
        if (viewport is InteriorViewportControl designViewport)
            designViewport.IsMaximized = maximized;
        else if (viewport is InteriorPreviewViewportControl preview)
            preview.IsMaximized = maximized;
    }

    private void TransformToolButton_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripButton button)
            return;

        selectToolButton.Checked = ReferenceEquals(button, selectToolButton);
        moveToolButton.Checked = ReferenceEquals(button, moveToolButton);
        rotateToolButton.Checked = ReferenceEquals(button, rotateToolButton);
        scaleToolButton.Checked = ReferenceEquals(button, scaleToolButton);
        var tool = ReferenceEquals(button, moveToolButton)
            ? InteriorTransformTool.Move
            : ReferenceEquals(button, rotateToolButton)
                ? InteriorTransformTool.Rotate
                : ReferenceEquals(button, scaleToolButton)
                    ? InteriorTransformTool.Scale
                    : InteriorTransformTool.Select;
        foreach (var viewport in GetViewports())
            viewport.TransformTool = tool;
        SetModelSelectionMode(tool == InteriorTransformTool.Select
            ? ModelSelectionMode.Select
            : ModelSelectionMode.Navigate);
        statusLabel.Text = _selectedDesignObject is null
            ? $"目前工具：{button.Text}；請先在場景清單選取模型或 Mesh。"
            : tool == InteriorTransformTool.Select
                ? "目前工具：選取。"
                : tool == InteriorTransformTool.Move
                    ? "目前工具：移動；拖曳 X／Y／Z 軸可單軸移動，拖曳中央控制點可沿視圖平面自由移動。"
                    : $"目前工具：{button.Text}；拖曳 ViewPort 的 X／Y／Z Gizmo，或在物件屬性輸入精確數值。";
    }

    private void RestoreTransformSnapSettings(RVInteriorDesignUserSettings settings)
    {
        _updatingSnapSettings = true;
        try
        {
            snapEnabledMenuItem.Checked = settings.TransformSnapEnabled;
            gridSnapMenuItem.Checked = settings.GridSnapEnabled;
            surfaceSnapMenuItem.Checked = settings.SurfaceSnapEnabled;
            CheckSnapValue(GetMoveSnapMenuItems(), settings.MoveSnapCentimeters, 10);
            CheckSnapValue(GetRotationSnapMenuItems(), settings.RotationSnapDegrees, 5);
        }
        finally
        {
            _updatingSnapSettings = false;
        }
        ApplyTransformSnapSettings(false);
    }

    private void SnapSettingsMenuItem_Changed(object? sender, EventArgs e) =>
        ApplyTransformSnapSettings(true);

    private void MoveSnapMenuItem_Click(object? sender, EventArgs e)
    {
        SelectSnapMenuItem(GetMoveSnapMenuItems(), sender as ToolStripMenuItem);
        ApplyTransformSnapSettings(true);
    }

    private void RotationSnapMenuItem_Click(object? sender, EventArgs e)
    {
        SelectSnapMenuItem(GetRotationSnapMenuItems(), sender as ToolStripMenuItem);
        ApplyTransformSnapSettings(true);
    }

    private void ApplyTransformSnapSettings(bool showStatus)
    {
        if (_updatingSnapSettings)
            return;
        var moveCentimeters = GetCheckedSnapValue(GetMoveSnapMenuItems(), 10);
        var rotationDegrees = GetCheckedSnapValue(GetRotationSnapMenuItems(), 5);
        foreach (var viewport in GetViewports())
        {
            viewport.TransformSnapEnabled = snapEnabledMenuItem.Checked;
            viewport.TransformMoveSnapEnabled = gridSnapMenuItem.Checked;
            viewport.TransformMoveSnapMeters = moveCentimeters / 100f;
            viewport.TransformRotationSnapDegrees = rotationDegrees;
        }
        snapToolDropDownButton.Text = snapEnabledMenuItem.Checked ? "吸附 ✓" : "吸附";
        if (showStatus)
        {
            var message = snapEnabledMenuItem.Checked
                ? $"吸附已啟用：移動 {moveCentimeters} cm、旋轉 {rotationDegrees}°；格點{(gridSnapMenuItem.Checked ? "開" : "關")}、表面{(surfaceSnapMenuItem.Checked ? "開" : "關")}。"
                : "吸附已停用；模型可自由移動與旋轉。";
            ShowExplanation(message);
        }
        if (_uiSettingsRestored && !_uiSettingsRestoring)
            CaptureUiSettings().Save();
    }

    private static void SelectSnapMenuItem(IEnumerable<ToolStripMenuItem> items, ToolStripMenuItem? selected)
    {
        if (selected is null)
            return;
        foreach (var item in items)
            item.Checked = ReferenceEquals(item, selected);
    }

    private static void CheckSnapValue(IEnumerable<ToolStripMenuItem> items, int value, int fallback)
    {
        var array = items.ToArray();
        var selected = array.FirstOrDefault(item => item.Tag is int candidate && candidate == value) ??
                       array.First(item => item.Tag is int candidate && candidate == fallback);
        SelectSnapMenuItem(array, selected);
    }

    private static int GetCheckedSnapValue(IEnumerable<ToolStripMenuItem> items, int fallback) =>
        items.FirstOrDefault(item => item.Checked)?.Tag is int value ? value : fallback;

    private IEnumerable<ToolStripMenuItem> GetMoveSnapMenuItems()
    {
        yield return moveSnap1CmMenuItem;
        yield return moveSnap5CmMenuItem;
        yield return moveSnap10CmMenuItem;
        yield return moveSnap20CmMenuItem;
        yield return moveSnap50CmMenuItem;
    }

    private IEnumerable<ToolStripMenuItem> GetRotationSnapMenuItems()
    {
        yield return rotationSnap1DegreeMenuItem;
        yield return rotationSnap5DegreeMenuItem;
        yield return rotationSnap15DegreeMenuItem;
        yield return rotationSnap45DegreeMenuItem;
    }

    private void Viewport_TransformChanged(object? sender, InteriorTransformChangedEventArgs e)
    {
        try
        {
            var message = $"正在用 ViewPort Gizmo {e.Operation}{GetSelectedTargetName()}…";
            SelectionTransformChanged(message, liveViewport: sender as InteriorViewportControl);
            ShowExplanation(message);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            ShowExplanation($"移動模型失敗：{exception.GetBaseException().Message}", ExplanationKind.Error);
        }
    }

    private void Viewport_TransformStarting(object? sender, InteriorTransformChangedEventArgs e)
    {
        try
        {
            RecordUndoSnapshot($"{e.Operation}{GetSelectedTargetName()}");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            ShowExplanation($"無法建立移動前的 Undo 快照：{exception.GetBaseException().Message}",
                ExplanationKind.Error);
        }
    }

    private void Viewport_TransformCompleted(object? sender, InteriorTransformChangedEventArgs e)
    {
        try
        {
            var surfaceMessage = e.Operation.Contains("移動", StringComparison.Ordinal)
                ? TrySnapMovedSelectionToSurface()
                : null;
            RefreshViewportScene();
            objectPropertyGrid.Refresh();
            var message = surfaceMessage ?? $"已用 ViewPort Gizmo {e.Operation}{GetSelectedTargetName()}。";
            ShowExplanation(message, message.StartsWith("碰撞警告", StringComparison.Ordinal)
                ? ExplanationKind.Warning
                : ExplanationKind.Information);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            ShowExplanation($"完成{e.Operation}時更新視圖失敗：{exception.GetBaseException().Message}",
                ExplanationKind.Error);
        }
    }

    private void MirrorAxisMenuItem_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: string axis } || !TryGetSelectedTransform(out var transform))
        {
            statusLabel.Text = "請先在場景清單選取要鏡射的模型或 Mesh。";
            return;
        }

        RecordUndoSnapshot($"沿 {axis} 軸鏡射{GetSelectedTargetName()}");
        foreach (var target in GetSelectedTransforms())
            target.Scale = axis switch
            {
                "X" => target.Scale with { X = ToggleMirrorScale(target.Scale.X) },
                "Y" => target.Scale with { Y = ToggleMirrorScale(target.Scale.Y) },
                "Z" => target.Scale with { Z = ToggleMirrorScale(target.Scale.Z) },
                _ => target.Scale
            };
        CaptureTransformSnapshot();
        SelectionTransformChanged($"已沿 {axis} 軸鏡射{GetSelectedTargetName()}。 ", false);
    }

    private void ResetTransformToolButton_Click(object? sender, EventArgs e)
    {
        if (!TryGetSelectedTransform(out var transform))
        {
            statusLabel.Text = "請先在場景清單選取要重設的模型或 Mesh。";
            return;
        }

        RecordUndoSnapshot($"重設{GetSelectedTargetName()}變換");
        foreach (var target in GetSelectedTransforms())
        {
            target.Position = Vector3.Zero;
            target.RotationDegrees = Vector3.Zero;
            target.Scale = Vector3.One;
        }
        CaptureTransformSnapshot();
        SelectionTransformChanged($"已重設{GetSelectedTargetName()}的位置、旋轉與縮放。 ", false);
    }

    private bool TryGetSelectedTransform(out Rv3dViewer.Core.TransformState transform)
    {
        if (_selectedDesignObject is null)
        {
            transform = null!;
            return false;
        }
        if (_selectedMeshIndex is int meshIndex &&
            (uint)meshIndex < (uint)_selectedDesignObject.Model.Meshes.Count)
        {
            transform = _selectedDesignObject.Model.GetOrCreateMeshTransform(meshIndex);
            return true;
        }
        transform = _selectedDesignObject.Model.Transform;
        return true;
    }

    private static float ToggleMirrorScale(float value) => value < 0f
        ? MathF.Max(MathF.Abs(value), .0001f)
        : -MathF.Max(MathF.Abs(value), .0001f);

    private string GetSelectedTargetName()
    {
        if (_selectedDesignObject is null)
            return "選取項目";
        return _selectedMeshIndex is int meshIndex &&
               (uint)meshIndex < (uint)_selectedDesignObject.Model.Meshes.Count
            ? $" Mesh「{_selectedDesignObject.Model.Meshes[meshIndex].Name}」"
            : _selectedModelIds.Count > 1
                ? $" {_selectedModelIds.Count} 個模型"
                : $"模型「{_selectedDesignObject.Parameters.Name}」";
    }

    private IEnumerable<Rv3dViewer.Core.TransformState> GetSelectedTransforms()
    {
        if (_selectedDesignObject is null)
            return [];
        if (_selectedMeshIndex is int meshIndex)
            return [_selectedDesignObject.Model.GetOrCreateMeshTransform(meshIndex)];
        return _designObjects.Where(item => _selectedModelIds.Contains(item.Id))
            .Select(item => item.Model.Transform)
            .ToArray();
    }

    private void CaptureTransformSnapshot()
    {
        if (_selectedDesignObject is null || _selectedMeshIndex is not null)
        {
            _transformSnapshotModelId = null;
            return;
        }
        var transform = _selectedDesignObject.Model.Transform;
        _transformSnapshotModelId = _selectedDesignObject.Id;
        _transformSnapshotPosition = transform.Position;
        _transformSnapshotRotation = transform.RotationDegrees;
        _transformSnapshotScale = transform.Scale;
    }

    private void PropagatePrimaryTransformToSelection()
    {
        if (_selectedDesignObject is null || _selectedMeshIndex is not null || _selectedModelIds.Count <= 1 ||
            _transformSnapshotModelId != _selectedDesignObject.Id)
        {
            CaptureTransformSnapshot();
            return;
        }
        var primary = _selectedDesignObject.Model.Transform;
        var positionDelta = primary.Position - _transformSnapshotPosition;
        var rotationDelta = primary.RotationDegrees - _transformSnapshotRotation;
        var scaleRatio = new Vector3(
            SafeScaleRatio(primary.Scale.X, _transformSnapshotScale.X),
            SafeScaleRatio(primary.Scale.Y, _transformSnapshotScale.Y),
            SafeScaleRatio(primary.Scale.Z, _transformSnapshotScale.Z));
        foreach (var item in _designObjects.Where(item => item.Id != _selectedDesignObject.Id &&
                                                           _selectedModelIds.Contains(item.Id)))
        {
            item.Model.Transform.Position += positionDelta;
            item.Model.Transform.RotationDegrees += rotationDelta;
            item.Model.Transform.Scale *= scaleRatio;
        }
        CaptureTransformSnapshot();
    }

    private static float SafeScaleRatio(float current, float previous) =>
        MathF.Abs(previous) < .0001f ? 1f : current / previous;

    private void SelectionTransformChanged(string? message = null, bool propagateToSelection = true,
        InteriorViewportControl? liveViewport = null)
    {
        if (propagateToSelection)
            PropagatePrimaryTransformToSelection();
        _hasUnexportedChanges = true;
        if (liveViewport is null)
        {
            RefreshViewportScene();
            objectPropertyGrid.Refresh();
        }
        else
        {
            liveViewport.Invalidate();
        }
        statusLabel.Text = message ?? $"已更新{GetSelectedTargetName()}的變換。";
    }

    private void ShowSelectionInspector(ParametricDesignObject item, int? meshIndex)
    {
        objectPropertyGrid.SelectedObject = meshIndex is int selectedMeshIndex &&
                                            (uint)selectedMeshIndex < (uint)item.Model.Meshes.Count
            ? new InteriorMeshInspector(item, selectedMeshIndex,
                description => RecordUndoSnapshot(description), message => SelectionTransformChanged(message))
            : new InteriorModelInspector(item,
                description => RecordUndoSnapshot(description), message => SelectionTransformChanged(message));
    }

    private void RulerMenuItem_CheckedChanged(object? sender, EventArgs e)
    {
        topViewport.ShowScaleBar = topRulerMenuItem.Checked;
        frontViewport.ShowScaleBar = frontRulerMenuItem.Checked;
        rightViewport.ShowScaleBar = sideRulerMenuItem.Checked;
        var enabledCount = new[] { topViewport, frontViewport, rightViewport }.Count(viewport => viewport.ShowScaleBar);
        statusLabel.Text = enabledCount == 0
            ? "所有正投影視圖比例尺均已隱藏。"
            : $"已在 {enabledCount} 個正投影視圖顯示動態比例尺；刻度會隨相機縮放同步更新。";
    }

    private void AssetFilterTextBox_TextChanged(object? sender, EventArgs e)
    {
        statusLabel.Text = string.IsNullOrWhiteSpace(assetFilterTextBox.Text)
            ? "資產篩選已清除。"
            : $"篩選資產：{assetFilterTextBox.Text}";
    }

    private void AssetCategoryControl_AssetSelected(object? sender, InteriorAssetDescriptor asset)
    {
        if (!ReferenceEquals(sender, GetActiveAssetCategoryControl()) ||
            rightPanelTabControl.SelectedTab != modelLibraryTabPage ||
            libraryTabControl.SelectedTab != assetLibraryTabPage)
            return;
        _selectedDesignObject = null;
        _selectedMeshIndex = null;
        _selectedModelIds.Clear();
        UpdateApplyMaterialButtonState();
        SelectViewportModels([], null, null);
        objectPropertyGrid.SelectedObject = asset;
        UpdateLibraryAssetCommandButtonStates();
        if (asset.CanPlace)
        {
            ShowExplanation($"步驟 1/3：已選取 {asset.Name}；可按「置放模型」、雙擊模型，或直接拖曳到透視圖。");
        }
        else
        {
            var message = asset.Source == "內建目錄"
                ? $"「{asset.Name}」是尚未綁定模型檔案的參考項目；可按「搜尋線上模型」尋找可下載模型。"
                : $"無法置放「{asset.Name}」：找不到可用的模型檔案。請重新加入或下載模型。";
            ShowExplanation(message, asset.Source == "內建目錄" ? ExplanationKind.Warning : ExplanationKind.Error);
        }
    }

    private async void AssetCategoryControl_PlaceRequested(object? sender,
        InteriorAssetPlacementRequestEventArgs e) => await PlaceAssetAsync(e.Asset, e.Settings);

    private void ModelsTreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        var node = e.Node;
        if (node is null)
            return;
        _draftParameters = ParametricPrimitiveParameters.FromTreeText(node.Text) ??
                           ParametricBuildingParametersFactory.FromTreeText(node.Text);
        _selectedDesignObject = null;
        _selectedMeshIndex = null;
        UpdateApplyMaterialButtonState();
        createParametricModelButton.Enabled = _draftParameters is not null;
        if (_draftParameters is null)
        {
            statusLabel.Text = "此模型類型將於階段三的後續步驟提供。";
            return;
        }

        parameterPropertyGrid.SelectedObject = _draftParameters;
        statusLabel.Text = $"請在右下方調整{node.Text}參數，再按「建立參數模型」。";
    }

    private void CreateParametricModelButton_Click(object? sender, EventArgs e)
    {
        if (_draftParameters is null)
            return;

        try
        {
            var parameters = _draftParameters.CopyDefinition();
            var id = Guid.NewGuid();
            var designObject = new ParametricDesignObject
            {
                Id = id,
                Parameters = parameters,
                Model = parameters.Generate(id)
            };
            RecordUndoSnapshot($"建立參數模型「{parameters.Name}」");
            _designObjects.Add(designObject);
            RefreshViewportScene();

            var node = CreateDesignObjectNode(designObject);
            sceneTreeView.Nodes[0].Nodes.Add(node);
            sceneTreeView.Nodes[0].Expand();
            sceneTreeView.SelectedNode = node;
            _hasUnexportedChanges = true;
            statusLabel.Text = $"已建立參數模型：{parameters.Name}；關閉 Plugin 時可匯入 MainForm。";
            ShowExplanation($"參數模型建立完成：{parameters.Name} 已加入場景，可在四視圖選取、移動或旋轉。");
        }
        catch (ArgumentException ex)
        {
            ShowExplanation($"無法建立參數模型：{ex.Message}", ExplanationKind.Error);
            MessageBox.Show(this, ex.Message, "參數無效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void CreateDefaultInteriorButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new DefaultInteriorVersionDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Selection == DefaultInteriorVersion.None)
        {
            ShowExplanation("已取消產生預設室內裝潢；目前場景未變更。");
            return;
        }

        IReadOnlyList<ParametricDesignObject> objects;
        createDefaultInteriorButton.Enabled = false;
        UseWaitCursor = true;
        try
        {
            if (dialog.Selection == DefaultInteriorVersion.Real)
            {
                var progress = new Progress<string>(message => ShowExplanation(message));
                objects = await DefaultInteriorSceneFactory.CreateRealAsync(progress);
            }
            else
            {
                ShowExplanation("步驟 1/2：建立簡單版參數化房間與家具…");
                objects = DefaultInteriorSceneFactory.Create();
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            ShowExplanation($"無法產生真實版室內裝潢：{exception.Message}", ExplanationKind.Error);
            MessageBox.Show(this,
                $"無法產生真實版室內裝潢，原有場景未變更。\r\n\r\n{exception.Message}",
                "產生預設室內裝潢", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        finally
        {
            UseWaitCursor = false;
            createDefaultInteriorButton.Enabled = true;
        }

        var versionName = dialog.Selection == DefaultInteriorVersion.Real ? "真實版" : "簡單版";
        ShowExplanation(dialog.Selection == DefaultInteriorVersion.Real
            ? "步驟 4/4：更新場景清單與四視圖…"
            : "步驟 2/2：更新場景清單與四視圖…");
        RecordUndoSnapshot($"產生預設室內模型（{versionName}）");
        var removedIds = _designObjects
            .Where(item => item.IsDefaultSceneObject)
            .Select(item => item.Id)
            .ToHashSet();
        _designObjects.RemoveAll(item => item.IsDefaultSceneObject);
        if (_selectedDesignObject is not null && removedIds.Contains(_selectedDesignObject.Id))
        {
            _selectedDesignObject = null;
            _selectedMeshIndex = null;
        }

        var root = sceneTreeView.Nodes[0];
        foreach (var existingGroup in root.Nodes.Cast<TreeNode>()
                     .Where(node => node.Name == DefaultInteriorSceneFactory.SceneNodeName).ToArray())
            existingGroup.Remove();

        _designObjects.AddRange(objects);
        var group = new TreeNode($"預設室內裝潢模型（{versionName}）")
            { Name = DefaultInteriorSceneFactory.SceneNodeName };
        foreach (var item in objects)
            group.Nodes.Add(CreateDesignObjectNode(item));
        root.Nodes.Add(group);
        root.Expand();
        group.Expand();
        RefreshViewportScene();
        sceneTreeView.SelectedNode = group.Nodes[0];
        _hasUnexportedChanges = true;
        MarkProjectDirty();
        ShowExplanation($"完成：已產生{versionName}預設室內裝潢，共 {objects.Count} 個模型；再次產生會更新此預設場景。");
    }

    private void SceneTreeView_SelectedNodesChanged(object? sender, EventArgs e)
    {
        if (_syncingSceneSelection)
            return;
        var selectedNodes = sceneTreeView.SelectedNodes
            .Where(node => node.Tag is ParametricDesignObject or InteriorMeshTreeItem)
            .ToArray();
        var primaryNode = sceneTreeView.SelectedNode is { Tag: ParametricDesignObject or InteriorMeshTreeItem }
            ? sceneTreeView.SelectedNode
            : selectedNodes.LastOrDefault();
        var primaryObject = primaryNode?.Tag switch
        {
            ParametricDesignObject modelItem => modelItem,
            InteriorMeshTreeItem meshItem => meshItem.DesignObject,
            _ => null
        };
        var ids = selectedNodes.Select(node => node.Tag switch
        {
            ParametricDesignObject modelItem => modelItem.Id,
            InteriorMeshTreeItem meshItem => meshItem.DesignObject.Id,
            _ => Guid.Empty
        }).Where(id => id != Guid.Empty).Distinct().ToArray();
        int? meshIndex = selectedNodes.Length == 1 && primaryNode?.Tag is InteriorMeshTreeItem selectedMesh
            ? selectedMesh.MeshIndex
            : null;
        SetSelectedModels(ids, primaryObject?.Id, meshIndex, false);
    }

    private void Viewport_ModelPicked(object? sender, InteriorModelPickedEventArgs e)
    {
        var selected = _selectedModelIds.ToHashSet();
        switch (e.Operation)
        {
            case InteriorSelectionOperation.Replace:
                selected.Clear();
                selected.UnionWith(e.ModelIds);
                break;
            case InteriorSelectionOperation.Add:
                selected.UnionWith(e.ModelIds);
                break;
            case InteriorSelectionOperation.Subtract:
                selected.ExceptWith(e.ModelIds);
                break;
            case InteriorSelectionOperation.Toggle:
                foreach (var id in e.ModelIds)
                    if (!selected.Add(id))
                        selected.Remove(id);
                break;
        }
        var requestedPrimary = e.ModelIds.LastOrDefault();
        Guid? primaryId = requestedPrimary != Guid.Empty && selected.Contains(requestedPrimary)
            ? requestedPrimary
            : selected.LastOrDefault() is var remaining && remaining != Guid.Empty ? remaining : null;
        SetSelectedModels(selected, primaryId, null, true);
        statusLabel.Text = e.Operation switch
        {
            InteriorSelectionOperation.Add => $"已加選；目前共 {_selectedModelIds.Count} 個模型。",
            InteriorSelectionOperation.Subtract => $"已減選；目前共 {_selectedModelIds.Count} 個模型。",
            InteriorSelectionOperation.Toggle => $"已切換選取；目前共 {_selectedModelIds.Count} 個模型。",
            _ => _selectedModelIds.Count == 0 ? "目前未選取模型。" : $"已選取 {_selectedModelIds.Count} 個模型。"
        };
    }

    private void SetSelectedModels(IEnumerable<Guid> modelIds, Guid? primaryModelId, int? meshIndex,
        bool synchronizeTree)
    {
        var availableIds = _designObjects.Select(item => item.Id).ToHashSet();
        _selectedModelIds.Clear();
        _selectedModelIds.UnionWith(modelIds.Where(availableIds.Contains));
        if (primaryModelId is not Guid primaryId || !_selectedModelIds.Contains(primaryId))
            primaryModelId = _selectedModelIds.LastOrDefault() is var fallback && fallback != Guid.Empty
                ? fallback
                : null;
        _selectedDesignObject = primaryModelId is Guid id
            ? _designObjects.FirstOrDefault(item => item.Id == id)
            : null;
        _selectedMeshIndex = _selectedModelIds.Count == 1 && _selectedDesignObject is not null &&
                             meshIndex is int index && (uint)index < (uint)_selectedDesignObject.Model.Meshes.Count
            ? index
            : null;

        if (synchronizeTree)
            SynchronizeSceneTreeSelection();
        SelectViewportModels(_selectedModelIds, _selectedDesignObject?.Id, _selectedMeshIndex);
        UpdateSelectionInspector();
        CaptureTransformSnapshot();
        UpdateApplyMaterialButtonState();
    }

    private void SynchronizeSceneTreeSelection()
    {
        _syncingSceneSelection = true;
        try
        {
            var nodes = _selectedModelIds.Select(FindDesignObjectNode).OfType<TreeNode>().ToList();
            var primaryNode = _selectedDesignObject is null ? null : FindDesignObjectNode(_selectedDesignObject.Id);
            if (primaryNode is not null && _selectedMeshIndex is int meshIndex)
            {
                nodes.Remove(primaryNode);
                primaryNode = FindMeshNode(primaryNode, meshIndex) ?? primaryNode;
                nodes.Add(primaryNode);
            }
            sceneTreeView.SetSelectedNodes(nodes, primaryNode);
        }
        finally
        {
            _syncingSceneSelection = false;
        }
    }

    private void UpdateSelectionInspector()
    {
        var designObject = _selectedDesignObject;
        if (designObject is null)
        {
            parameterPropertyGrid.SelectedObject = null;
            objectPropertyGrid.SelectedObject = null;
            statusLabel.Text = "目前未選取模型。";
            return;
        }
        _draftParameters = null;
        parameterPropertyGrid.SelectedObject = designObject.Parameters;
        if (_selectedMeshIndex is int meshIndex && (uint)meshIndex < (uint)designObject.Model.Meshes.Count)
        {
            var mesh = designObject.Model.Meshes[meshIndex];
            ShowSelectionInspector(designObject, meshIndex);
            statusLabel.Text = $"已選取 Mesh：{mesh.Name}（{designObject.Parameters.Name}）";
        }
        else
        {
            ShowSelectionInspector(designObject, null);
            statusLabel.Text = _selectedModelIds.Count > 1
                ? $"已選取 {_selectedModelIds.Count} 個模型；主要模型：{designObject.Parameters.Name}"
                : $"已選取參數模型：{designObject.Parameters.Name}";
        }
    }

    private void ParameterPropertyGrid_PropertyValueChanged(object? sender, PropertyValueChangedEventArgs e)
    {
        if (_selectedDesignObject is null)
        {
            if (_draftParameters is null || !ReferenceEquals(parameterPropertyGrid.SelectedObject, _draftParameters))
                return;
            try
            {
                _draftPreviewModel = _draftParameters.Generate();
                parameterPropertyGrid.Refresh();
                statusLabel.Text = $"已依新參數重新產生 {_draftParameters.Name} 預覽 Mesh 與 UV。";
            }
            catch (ArgumentException ex)
            {
                statusLabel.Text = $"參數無效：{ex.Message}";
            }
            return;
        }
        var selectedDesignObject = _selectedDesignObject;
        if (!ReferenceEquals(parameterPropertyGrid.SelectedObject, selectedDesignObject.Parameters))
            return;

        var descriptor = e.ChangedItem?.PropertyDescriptor;
        var editedObject = parameterPropertyGrid.SelectedObject;
        var editedValue = descriptor?.GetValue(editedObject);
        if (descriptor is not null && editedObject is not null)
        {
            descriptor.SetValue(editedObject, e.OldValue);
            RecordUndoSnapshot($"修改「{selectedDesignObject.Parameters.Name}」參數");
            descriptor.SetValue(editedObject, editedValue);
        }
        else
        {
            RecordUndoSnapshot($"修改「{selectedDesignObject.Parameters.Name}」參數");
        }
        try
        {
            selectedDesignObject.Model = RegenerateDesignModel(selectedDesignObject);
            var modelNode = FindDesignObjectNode(selectedDesignObject.Id);
            if (modelNode is not null)
            {
                var selectedMeshIndex = _selectedMeshIndex;
                RefreshDesignObjectNode(modelNode, selectedDesignObject);
                if (selectedMeshIndex is int meshIndex)
                {
                    var meshNode = FindMeshNode(modelNode, meshIndex);
                    _selectedMeshIndex = meshNode is null ? null : meshIndex;
                    sceneTreeView.SelectedNode = meshNode ?? modelNode;
                }
            }
            parameterPropertyGrid.Refresh();
            RefreshViewportScene();
            _hasUnexportedChanges = true;
            statusLabel.Text = $"已重新產生 {selectedDesignObject.Parameters.Name} 的 Mesh 與 UV。";
        }
        catch (ArgumentException ex)
        {
            statusLabel.Text = $"參數無效：{ex.Message}";
        }
    }

    private bool TryExportAllDesignObjects()
    {
        var context = _context;
        if (context is null)
        {
            MessageBox.Show(this, "目前沒有可用的 MainForm 專案內容，無法匯入。", "RV室內設計",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        try
        {
            using var transaction = context.BeginProjectEdit("關閉 RV室內設計並匯入全部設計");
            var ids = _designObjects.Select(item => item.Id).ToHashSet();
            context.Project.Models.RemoveAll(model => ids.Contains(model.Id));
            foreach (var item in _designObjects)
                context.Project.Models.Add(RegenerateDesignModel(item));
            RefreshParametricProjectExtension(context.Project);
            transaction.Commit(_selectedDesignObject?.Id);
            _hasUnexportedChanges = false;
            context.SetStatus($"已從 RV室內設計匯入 {_designObjects.Count} 個參數模型");
            return true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"匯入 MainForm 失敗，Plugin 將保持開啟。\r\n\r\n{exception.Message}",
                "RV室內設計", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private static Rv3dViewer.Core.SceneModel RegenerateDesignModel(ParametricDesignObject item)
    {
        var previous = item.Model;
        if (item.Parameters is ImportedAssetParameters)
        {
            InteriorMeshGeometry.Normalize(previous);
            previous.CaptureMeshMaterialIndices();
            previous.CaptureProceduralGeometry();
            return previous;
        }
        var model = item.Parameters.Generate(item.Id);
        model.Transform = new Rv3dViewer.Core.TransformState
        {
            Position = previous.Transform.Position,
            RotationDegrees = previous.Transform.RotationDegrees,
            Scale = previous.Transform.Scale
        };
        model.HiddenMeshIndices = previous.HiddenMeshIndices
            .Where(index => (uint)index < (uint)model.Meshes.Count)
            .ToList();
        model.MeshTransforms = previous.MeshTransforms
            .Where(pair => (uint)pair.Key < (uint)model.Meshes.Count)
            .ToDictionary(pair => pair.Key, pair => CloneTransform(pair.Value));
        if (previous.Materials.Count >= model.Materials.Count)
            model.Materials = previous.Materials.Select(material => material.Clone()).ToList();
        if (previous.Meshes.Count == model.Meshes.Count)
        {
            for (var meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
            {
                var materialIndex = previous.Meshes[meshIndex].MaterialIndex;
                if ((uint)materialIndex < (uint)model.Materials.Count)
                    model.Meshes[meshIndex].MaterialIndex = materialIndex;
                if (previous.Meshes[meshIndex].TextureCoordinates.Length ==
                    model.Meshes[meshIndex].Positions.Length)
                {
                    model.Meshes[meshIndex].TextureCoordinates =
                        previous.Meshes[meshIndex].TextureCoordinates.ToArray();
                }
            }
        }
        model.CaptureMeshMaterialIndices();
        return model;
    }

    private static Rv3dViewer.Core.TransformState CloneTransform(Rv3dViewer.Core.TransformState transform) => new()
    {
        Position = transform.Position,
        RotationDegrees = transform.RotationDegrees,
        Scale = transform.Scale
    };

    private void RefreshParametricProjectExtension(Rv3dViewer.Core.ViewerProject project)
    {
        project.Extensions[InteriorDesignProjectStore.ExtensionKey] =
            InteriorDesignProjectStore.CreateExtensionElement(
                _designObjects, _selectedModelIds, _selectedDesignObject?.Id, _selectedMeshIndex,
                _showModelEdges, _visibilityMode, _pbrPreviewEnabled, topViewport.Camera,
                frontViewport.Camera, rightViewport.Camera, perspectiveViewport.Camera);
    }

    private void RefreshViewportScene()
    {
        var models = _designObjects.Select(item => item.Model)
            .Concat(_pendingAssetPlacement is null ? [] : [_pendingAssetPlacement.Model])
            .ToArray();
        foreach (var viewport in GetViewports())
            viewport.SetSceneModels(models);
        // A hidden GLControl does not need scene invalidation and can fail while its native
        // context is transitioning. It is synchronized immediately when PBR mode is enabled.
        if (_pbrPreviewEnabled)
            previewViewport.SetSceneModels(models);
    }

    private void SelectViewportModel(Guid? modelId, int? meshIndex = null) =>
        SelectViewportModels(modelId is Guid id ? [id] : [], modelId, meshIndex);

    private void SelectViewportModels(IEnumerable<Guid> modelIds, Guid? primaryModelId, int? meshIndex = null)
    {
        var ids = modelIds.ToArray();
        foreach (var viewport in GetViewports())
            viewport.SetSelection(ids, primaryModelId, meshIndex);
        previewViewport.SetSelection(ids, primaryModelId, meshIndex);
    }

    private abstract class InteriorTransformInspector
    {
        private readonly Action<string> _changing;
        private readonly Action<string?> _changed;

        protected InteriorTransformInspector(Action<string> changing, Action<string?> changed)
        {
            _changing = changing;
            _changed = changed;
        }

        protected abstract Rv3dViewer.Core.TransformState Transform { get; }

        [Category("變換｜位置"), DisplayName("X（公尺）")]
        public float PositionX { get => Transform.Position.X; set => SetPosition(value, 0); }

        [Category("變換｜位置"), DisplayName("Y（公尺）")]
        public float PositionY { get => Transform.Position.Y; set => SetPosition(value, 1); }

        [Category("變換｜位置"), DisplayName("Z（公尺）")]
        public float PositionZ { get => Transform.Position.Z; set => SetPosition(value, 2); }

        [Category("變換｜旋轉"), DisplayName("X（度）")]
        public float RotationX { get => Transform.RotationDegrees.X; set => SetRotation(value, 0); }

        [Category("變換｜旋轉"), DisplayName("Y（度）")]
        public float RotationY { get => Transform.RotationDegrees.Y; set => SetRotation(value, 1); }

        [Category("變換｜旋轉"), DisplayName("Z（度）")]
        public float RotationZ { get => Transform.RotationDegrees.Z; set => SetRotation(value, 2); }

        [Category("變換｜縮放"), DisplayName("X（倍率）")]
        public float ScaleX { get => Transform.Scale.X; set => SetScale(value, 0); }

        [Category("變換｜縮放"), DisplayName("Y（倍率）")]
        public float ScaleY { get => Transform.Scale.Y; set => SetScale(value, 1); }

        [Category("變換｜縮放"), DisplayName("Z（倍率）")]
        public float ScaleZ { get => Transform.Scale.Z; set => SetScale(value, 2); }

        private void SetPosition(float value, int axis)
        {
            ValidateFinite(value);
            var vector = Transform.Position;
            var updated = SetComponent(vector, value, axis);
            if (updated == vector) return;
            _changing("修改位置");
            Transform.Position = updated;
            _changed(null);
        }

        private void SetRotation(float value, int axis)
        {
            ValidateFinite(value);
            var vector = Transform.RotationDegrees;
            var updated = SetComponent(vector, value, axis);
            if (updated == vector) return;
            _changing("修改旋轉");
            Transform.RotationDegrees = updated;
            _changed(null);
        }

        private void SetScale(float value, int axis)
        {
            ValidateFinite(value);
            if (MathF.Abs(value) < .0001f)
                throw new ArgumentOutOfRangeException(nameof(value), "縮放倍率不可為 0。 ");
            var vector = Transform.Scale;
            var updated = SetComponent(vector, value, axis);
            if (updated == vector) return;
            _changing("修改縮放");
            Transform.Scale = updated;
            _changed(null);
        }

        private static Vector3 SetComponent(Vector3 vector, float value, int axis) => axis switch
        {
            0 => vector with { X = value },
            1 => vector with { Y = value },
            _ => vector with { Z = value }
        };

        private static void ValidateFinite(float value)
        {
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value), "數值必須是有限值。 ");
        }
    }

    private sealed class InteriorModelInspector : InteriorTransformInspector
    {
        private readonly ParametricDesignObject _item;

        internal InteriorModelInspector(ParametricDesignObject item, Action<string> changing, Action<string?> changed)
            : base(changing, changed) =>
            _item = item;

        protected override Rv3dViewer.Core.TransformState Transform => _item.Model.Transform;

        [Category("模型"), DisplayName("模型名稱"), ReadOnly(true)]
        public string ModelName => _item.Parameters.Name;

        [Category("模型"), DisplayName("Mesh 數量"), ReadOnly(true)]
        public int MeshCount => _item.Model.Meshes.Count;

        public override string ToString() => ModelName;
    }

    private sealed class InteriorMeshInspector : InteriorTransformInspector
    {
        private readonly Rv3dViewer.Core.SceneModel _model;
        private readonly Rv3dViewer.Core.MeshData _mesh;

        internal InteriorMeshInspector(ParametricDesignObject item, int meshIndex,
            Action<string> changing, Action<string?> changed)
            : base(changing, changed)
        {
            _model = item.Model;
            _mesh = item.Model.Meshes[meshIndex];
            ModelName = item.Parameters.Name;
            MeshIndex = meshIndex;
        }

        protected override Rv3dViewer.Core.TransformState Transform => _model.GetOrCreateMeshTransform(MeshIndex);

        [Category("模型"), DisplayName("模型名稱"), ReadOnly(true)]
        public string ModelName { get; }

        [Category("Mesh"), DisplayName("Mesh 名稱"), ReadOnly(true)]
        public string MeshName => _mesh.Name;

        [Category("Mesh"), DisplayName("Mesh 索引"), ReadOnly(true)]
        public int MeshIndex { get; }

        [Category("幾何"), DisplayName("頂點數"), ReadOnly(true)]
        public int VertexCount => _mesh.Positions.Length;

        [Category("幾何"), DisplayName("三角形數"), ReadOnly(true)]
        public int TriangleCount => _mesh.TriangleCount;

        [Category("材質"), DisplayName("材質索引"), ReadOnly(true)]
        public int MaterialIndex => _mesh.MaterialIndex;

        [Category("材質"), DisplayName("材質名稱"), ReadOnly(true)]
        public string MaterialName => (uint)_mesh.MaterialIndex < (uint)_model.Materials.Count
            ? _model.Materials[_mesh.MaterialIndex].Name
            : "（未指定）";

        [Category("貼圖"), DisplayName("基礎色貼圖"), ReadOnly(true)]
        public string BaseColorTexture
        {
            get
            {
                if ((uint)_mesh.MaterialIndex >= (uint)_model.Materials.Count)
                    return "（未指定）";
                var material = _model.Materials[_mesh.MaterialIndex];
                if (material.TextureStacks.TryGetValue(Rv3dViewer.Core.TextureSemantic.BaseColor, out var stack) &&
                    stack is { Enabled: true })
                    return stack.Layers.LastOrDefault(layer => layer.Enabled)?.Name ?? "（未指定）";
                return material.Textures.TryGetValue(Rv3dViewer.Core.TextureSemantic.BaseColor, out var slot) &&
                       slot.Enabled
                    ? Path.GetFileName(slot.Path)
                    : "（未指定）";
            }
        }

        [Category("貼圖"), DisplayName("映射模式"), ReadOnly(true)]
        public string TextureMapping
        {
            get
            {
                if ((uint)_mesh.MaterialIndex >= (uint)_model.Materials.Count)
                    return "（未指定）";
                var material = _model.Materials[_mesh.MaterialIndex];
                return material.TextureStacks.TryGetValue(Rv3dViewer.Core.TextureSemantic.BaseColor, out var stack)
                    ? stack.Layers.LastOrDefault(layer => layer.Enabled)?.Mapping.Mode.ToString() ?? "（未指定）"
                    : "（未指定）";
            }
        }

        public override string ToString() => MeshName;
    }

}
