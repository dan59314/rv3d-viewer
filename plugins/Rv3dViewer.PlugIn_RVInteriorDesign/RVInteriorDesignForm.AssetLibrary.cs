namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using System.Runtime.InteropServices;
using Rv3dViewer.Core;

internal sealed partial class RVInteriorDesignForm
{
    private enum PlacementSurfaceRequirement { Ground, Floor, HorizontalSurface, Wall, Ceiling }

    private readonly record struct PlacementSurfaceHit(
        Vector3 Point, Vector3 Normal, SceneBounds? HostBounds, Guid? HostObjectId, string SurfaceName,
        PlacementSurfaceRequirement Kind);

    private sealed record PendingAssetPlacement(InteriorAssetDescriptor Asset,
        InteriorAssetPlacementSettings Settings, SceneModel Model, SceneBounds RawBounds)
    {
        internal bool HasValidSurface { get; set; } = true;
        internal string InvalidSurfaceReason { get; set; } = string.Empty;
        internal PlacementSurfaceHit? SurfaceHit { get; set; }
    }

    private PendingAssetPlacement? _pendingAssetPlacement;
    private bool _onlineAssetDownloadInProgress;

    private async void ImportAssetButton_Click(object? sender, EventArgs e)
    {
        if (importAssetDialog.ShowDialog(this) != DialogResult.OK)
            return;
        var initialCategory = assetTabControl.SelectedTab?.Text ?? "使用者自訂";
        using var optionsDialog = new InteriorAssetBatchImportForm(importAssetDialog.FileNames, initialCategory);
        if (optionsDialog.ShowDialog(this) != DialogResult.OK)
            return;
        var importedCount = 0;
        var errors = new List<string>();
        try
        {
            UseWaitCursor = true;
            var entries = optionsDialog.Entries;
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                InteriorAssetDescriptor? storedAsset = null;
                try
                {
                    statusLabel.Text = $"正在檢查模型 {index + 1}/{entries.Count}：{entry.Name}…";
                    storedAsset = InteriorUserAssetCatalog.StoreModel(entry.SourcePath, optionsDialog.AssetCategory);
                    storedAsset = InteriorUserAssetCatalog.WithMetadata(storedAsset, entry.Name,
                        optionsDialog.AssetCategory, optionsDialog.AssetLicense, optionsDialog.AssetSourceUrl);
                    var model = await InteriorModelImportService.ImportAsync(storedAsset.ModelPath);
                    if (!SceneTraversal.TryCalculateBounds(model, out var bounds))
                        throw new InvalidDataException("模型沒有可置放的有效 Mesh。");
                    var report = InteriorAssetQualityService.Analyze(model, storedAsset.ModelPath);
                    storedAsset = InteriorUserAssetCatalog.WithAnalysis(storedAsset,
                        bounds.Size.X, bounds.Size.Z, bounds.Size.Y, report);
                    InteriorUserAssetCatalog.Save(storedAsset);
                    importedCount++;
                }
                catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
                {
                    if (storedAsset is not null)
                        InteriorUserAssetCatalog.DeleteStoredAsset(storedAsset.ModelPath);
                    errors.Add($"{entry.Name}：{exception.Message}");
                }
            }
            InitializeAssetCatalog();
            statusLabel.Text = $"已加入 {importedCount} 個模型，品質檢查失敗 {errors.Count} 個。";
            if (errors.Count > 0)
                MessageBox.Show(this, $"部分模型無法加入：\r\n\r\n{string.Join("\r\n", errors.Take(12))}",
                    "批次加入模型", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            MessageBox.Show(this, $"無法執行批次加入：\r\n\r\n{exception.Message}", "批次加入模型",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "批次加入模型失敗。";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void RefreshAssetLibraryButton_Click(object? sender, EventArgs e)
    {
        InitializeAssetCatalog();
        statusLabel.Text = "已重新整理本機模型庫。";
    }

    private void AssetTabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateActiveAssetCategoryThumbnailLoading();
        UpdateLibraryAssetCommandButtonStates();
    }

    private void LibraryTabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateActiveAssetCategoryThumbnailLoading();
        if (_uiSettingsRestored && !_uiSettingsRestoring)
            CaptureUiSettings().Save();
    }

    private void UpdateActiveAssetCategoryThumbnailLoading()
    {
        var assetLibraryVisible = rightPanelTabControl.SelectedTab == modelLibraryTabPage &&
                                  libraryTabControl.SelectedTab == assetLibraryTabPage;
        var activeControl = assetLibraryVisible ? GetActiveAssetCategoryControl() : null;
        foreach (var control in GetAssetCategoryControls())
            control.SetThumbnailLoadingEnabled(ReferenceEquals(control, activeControl));
    }

    private void AddFavoriteLibraryAssetButton_Click(object? sender, EventArgs e)
    {
        var asset = GetSelectedLibraryAsset();
        if (asset is null)
        {
            ShowExplanation("無法加入我的最愛：請先在下方清單選取模型。", ExplanationKind.Error);
            UpdateLibraryAssetCommandButtonStates();
            return;
        }
        if (asset.IsFavorite)
        {
            ShowExplanation($"「{asset.Name}」已在我的最愛中。");
            UpdateLibraryAssetCommandButtonStates();
            return;
        }

        try
        {
            InteriorAssetFavoriteStore.Add(asset);
            InitializeAssetCatalog();
            statusLabel.Text = $"已將 {asset.Name} 加到我的最愛。";
            ShowExplanation($"已將「{asset.Name}」加到我的最愛；可勾選「只顯示我的最愛」查看。");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            statusLabel.Text = "加入我的最愛失敗。";
            ShowExplanation($"無法加入我的最愛：{exception.Message}", ExplanationKind.Error);
            MessageBox.Show(this, $"無法儲存我的最愛：{exception.Message}", "加到我的最愛",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteLibraryAssetButton_Click(object? sender, EventArgs e)
    {
        var asset = GetSelectedLibraryAsset();
        if (asset is not { IsManagedLocalAsset: true })
        {
            ShowExplanation("無法刪除模型：請先在下方清單選取本機模型庫的模型。", ExplanationKind.Error);
            UpdateLibraryAssetCommandButtonStates();
            return;
        }
        AssetCategoryControl_DeleteRequested(sender, asset);
    }

    private InteriorAssetDescriptor? GetSelectedLibraryAsset() => GetActiveAssetCategoryControl()?.SelectedAsset;

    private AssetCategoryControl? GetActiveAssetCategoryControl() =>
        assetTabControl.SelectedTab?.Controls.OfType<AssetCategoryControl>().FirstOrDefault();

    private void UpdateDeleteLibraryAssetButtonState()
    {
        if (deleteLibraryAssetButton is not null)
            deleteLibraryAssetButton.Enabled = GetSelectedLibraryAsset() is { IsManagedLocalAsset: true };
    }

    private void UpdateAddFavoriteLibraryAssetButtonState()
    {
        if (addFavoriteLibraryAssetButton is not null)
            addFavoriteLibraryAssetButton.Enabled = GetSelectedLibraryAsset() is { IsFavorite: false };
    }

    private void UpdateLibraryAssetCommandButtonStates()
    {
        UpdateDeleteLibraryAssetButtonState();
        UpdateAddFavoriteLibraryAssetButtonState();
    }

    private async void OnlineAssetsTabPage_Enter(object? sender, EventArgs e) =>
        await polyHavenAssetBrowserControl.EnsureLoadedAsync();

    private async void PolyHavenAssetBrowserControl_DownloadRequested(
        object? sender, OnlineModelDownloadRequestedEventArgs e)
    {
        if (_onlineAssetDownloadInProgress)
        {
            ShowExplanation("已有線上模型正在下載，請等待目前下載完成。", ExplanationKind.Warning);
            return;
        }

        var provider = OnlineModelProviderRegistry.Get(e.Asset.ProviderId);
        var sourceUrl = e.Asset.SourceUrl;
        var existingAsset = InteriorUserAssetCatalog.FindBySourceUrl(sourceUrl);
        if (existingAsset is { CanPlace: true })
        {
            var duplicateMessage = $"「{e.Asset.Name}」已下載至「模型庫／{existingAsset.Category}」，不再重複下載。";
            statusLabel.Text = "已略過重複的線上模型。";
            ShowExplanation(duplicateMessage, ExplanationKind.Warning);
            polyHavenAssetBrowserControl.SetDownloadBusy(false, duplicateMessage);
            return;
        }

        _onlineAssetDownloadInProgress = true;
        InteriorAssetDescriptor? storedAsset = null;
        var finalMessage = $"準備下載 {e.Asset.Name}。";
        try
        {
            BeginOperationProgress($"準備下載 {provider.DisplayName} 模型：{e.Asset.Name}…");
            if (existingAsset is not null)
                ShowExplanation($"偵測到「{e.Asset.Name}」的舊資料，但模型檔案已遺失，將重新下載。",
                    ExplanationKind.Warning);
            polyHavenAssetBrowserControl.SetDownloadBusy(true, $"正在下載 {e.Asset.Name}…");
            statusLabel.Text = $"正在下載 {provider.DisplayName} 模型：{e.Asset.Name}…";
            var progress = new Progress<int>(percent =>
            {
                var message = $"正在下載 {e.Asset.Name}：{percent}%";
                polyHavenAssetBrowserControl.SetDownloadBusy(true, message);
                ReportOperationProgress(percent * 70 / 100, message);
            });
            using var package = await provider.DownloadAsync(e.Asset, e.Option, progress);
            ReportOperationProgress(74, $"正在整理 {e.Asset.Name} 的模型資產…");
            var category = MapOnlineAssetCategory($"{e.Asset.Category} {e.Asset.Tags}");
            storedAsset = InteriorUserAssetCatalog.StoreModel(package.ModelPath, category);
            var attribution = string.IsNullOrWhiteSpace(e.Asset.Authors)
                ? e.Asset.ProviderName
                : $"{e.Asset.ProviderName}／{e.Asset.Authors}";
            storedAsset = InteriorUserAssetCatalog.WithMetadata(storedAsset, e.Asset.Name, category,
                $"{e.Asset.License}｜{attribution}｜{e.Option.Label}",
                sourceUrl);
            ReportOperationProgress(84, $"正在匯入並驗證 {e.Asset.Name}…");
            var model = await InteriorModelImportService.ImportAsync(storedAsset.ModelPath);
            if (!SceneTraversal.TryCalculateBounds(model, out var bounds))
                throw new InvalidDataException("下載的模型沒有可置放的有效 Mesh。");
            var report = InteriorAssetQualityService.Analyze(model, storedAsset.ModelPath);
            ReportOperationProgress(94, $"正在更新本機模型庫：{e.Asset.Name}…");
            storedAsset = InteriorUserAssetCatalog.WithAnalysis(storedAsset,
                bounds.Size.X, bounds.Size.Z, bounds.Size.Y, report);
            InteriorUserAssetCatalog.Save(storedAsset, replaceMatchingSourceUrl: true);
            InitializeAssetCatalog();
            ReportOperationProgress(100, $"已下載並加入模型庫：{e.Asset.Name}");
            finalMessage = $"已將 {e.Asset.Name} 加入「{category}」本機模型庫。";
            statusLabel.Text = finalMessage;
            ShowExplanation($"下載完成：{e.Asset.Name} 已加入「模型庫／{category}」TabPage；目前仍保留在線上模型頁面。");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            if (storedAsset is not null)
                InteriorUserAssetCatalog.DeleteStoredAsset(storedAsset.ModelPath);
            finalMessage = $"無法下載或加入模型：{exception.Message}";
            statusLabel.Text = "線上模型加入失敗。";
            MessageBox.Show(this, finalMessage, $"{provider.DisplayName} 線上模型",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _onlineAssetDownloadInProgress = false;
            polyHavenAssetBrowserControl.SetDownloadBusy(false, finalMessage);
            EndOperationProgress();
        }
    }

    private static string MapOnlineAssetCategory(string category)
    {
        var value = category.ToLowerInvariant();
        if (value.Contains("light")) return "燈具";
        if (value.Contains("door") || value.Contains("window")) return "門窗";
        if (value.Contains("kitchen")) return "廚房";
        if (value.Contains("bath")) return "衛浴";
        if (value.Contains("storage") || value.Contains("container") || value.Contains("shelf")) return "收納";
        if (value.Contains("plant") || value.Contains("nature")) return "植栽";
        if (value.Contains("appliance") || value.Contains("electronic")) return "電器";
        if (value.Contains("decor") || value.Contains("art") || value.Contains("fabric") ||
            value.Contains("rug")) return "軟裝";
        if (value.Contains("furniture") || value.Contains("seating") || value.Contains("table") ||
            value.Contains("chair") || value.Contains("bed")) return "家具";
        return "其他";
    }

    private async void AnalyzeAssetLibraryButton_Click(object? sender, EventArgs e)
    {
        var assets = InteriorUserAssetCatalog.Load().Where(asset => asset.CanPlace).ToArray();
        if (assets.Length == 0)
        {
            statusLabel.Text = "本機模型庫沒有可檢查的模型。";
            return;
        }
        var passed = 0;
        var errors = new List<string>();
        try
        {
            UseWaitCursor = true;
            analyzeAssetLibraryButton.Enabled = false;
            for (var index = 0; index < assets.Length; index++)
            {
                var asset = assets[index];
                try
                {
                    statusLabel.Text = $"正在檢查 {index + 1}/{assets.Length}：{asset.Name}…";
                    var model = await InteriorModelImportService.ImportAsync(asset.ModelPath);
                    if (!SceneTraversal.TryCalculateBounds(model, out var bounds))
                        throw new InvalidDataException("模型沒有可用 Mesh。");
                    var report = InteriorAssetQualityService.Analyze(model, asset.ModelPath);
                    InteriorUserAssetCatalog.Save(InteriorUserAssetCatalog.WithAnalysis(asset,
                        bounds.Size.X, bounds.Size.Z, bounds.Size.Y, report));
                    passed++;
                }
                catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
                {
                    errors.Add($"{asset.Name}：{exception.Message}");
                }
            }
            InitializeAssetCatalog();
            statusLabel.Text = $"品質檢查完成：{passed} 個成功，{errors.Count} 個失敗。";
            if (errors.Count > 0)
                MessageBox.Show(this, string.Join("\r\n", errors.Take(12)), "模型品質檢查",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            analyzeAssetLibraryButton.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void AssetCategoryControl_EditRequested(object? sender, InteriorAssetDescriptor asset)
    {
        using var dialog = new InteriorAssetEditForm(asset);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            var updated = InteriorUserAssetCatalog.WithMetadata(asset, dialog.AssetName,
                dialog.AssetCategory, dialog.AssetLicense, dialog.AssetSourceUrl);
            InteriorUserAssetCatalog.Save(updated);
            InitializeAssetCatalog();
            assetTabControl.SelectedIndex = Math.Max(0,
                Array.FindIndex(InteriorAssetCatalog.Categories.ToArray(), category => category == updated.Category));
            statusLabel.Text = $"已更新模型庫資產：{updated.Name}。";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show(this, $"無法更新模型庫資產：\r\n\r\n{exception.Message}",
                "編輯模型庫資產", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AssetCategoryControl_DeleteRequested(object? sender, InteriorAssetDescriptor asset)
    {
        var usedCount = _designObjects.Count(item =>
            item.Parameters is ImportedAssetParameters imported &&
            string.Equals(imported.ModelPath, asset.ModelPath, StringComparison.OrdinalIgnoreCase));
        var usageText = usedCount > 0
            ? $"\r\n\r\n場景中已置放 {usedCount} 個此模型；已置放的內嵌模型會保留。"
            : string.Empty;
        var result = MessageBox.Show(this,
            $"確定要從本機模型庫刪除「{asset.Name}」及其資產檔案嗎？{usageText}",
            "刪除模型庫資產", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (result != DialogResult.Yes)
            return;
        try
        {
            InteriorUserAssetCatalog.Remove(asset);
            InitializeAssetCatalog();
            statusLabel.Text = $"已從本機模型庫刪除：{asset.Name}。";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"無法刪除模型庫資產：\r\n\r\n{exception.Message}",
                "刪除模型庫資產", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AssetCategoryControl_SearchOnlineRequested(object? sender, InteriorAssetDescriptor asset)
    {
        var query = GetOnlineAssetSearchQuery(asset);
        polyHavenAssetBrowserControl.SetSearchQuery(query);
        assetTabControl.SelectedTab = onlineAssetsTabPage;
        ShowExplanation($"正在『線上模型』搜尋「{query}」；選取模型與解析度後，按「下載並加入本機模型庫」。");
    }

    private static string GetOnlineAssetSearchQuery(InteriorAssetDescriptor asset) => asset.Name switch
    {
        "單人沙發" or "三人沙發" => "sofa",
        "餐桌" => "table",
        "餐椅" => "chair",
        "雙人床" => "bed",
        "床頭櫃" => "nightstand",
        "壁掛電視" => "television",
        "直立式冰箱" => "refrigerator",
        "分離式冷氣" => "air conditioner",
        "洗衣機" => "washing machine",
        "吸頂燈" or "吊燈" or "嵌燈" or "軌道燈" => "ceiling light",
        "檯燈" => "table lamp",
        "立燈" => "floor lamp",
        "單開門" or "雙開門" => "door",
        "橫拉窗" or "落地窗" => "window",
        "下櫃模組" or "吊櫃模組" => "kitchen cabinet",
        "廚房中島" => "kitchen island",
        "水槽" => "sink",
        "洗手台" => "bathroom sink",
        "馬桶" => "toilet",
        "浴缸" => "bathtub",
        "淋浴拉門" => "shower",
        "開放層架" => "shelf",
        "衣櫃" => "wardrobe",
        "電視櫃" => "cabinet",
        "窗簾" => "curtain",
        "地毯" => "rug",
        "抱枕" => "pillow",
        "掛畫" => "wall art",
        "大型盆栽" or "桌上盆栽" => "potted plant",
        "植生牆模組" => "plant",
        "樓梯" => "stairs",
        "欄杆" => "railing",
        _ => asset.Subcategory
    };

    private async Task PlaceAssetAsync(InteriorAssetDescriptor asset, InteriorAssetPlacementSettings settings)
    {
        try
        {
            CancelAssetPlacement(showStatus: false);
            UseWaitCursor = true;
            ShowExplanation($"步驟 1/3：正在載入「{asset.Name}」並準備置放預覽…");
            var id = Guid.NewGuid();
            var model = await InteriorModelImportService.ImportAsync(asset.ModelPath);
            model.Id = id;
            model.Name = asset.Name;
            model.IsProcedural = true;
            if (!SceneTraversal.TryCalculateBounds(model, out var bounds))
                throw new InvalidDataException("模型沒有可置放的有效 Mesh。");
            model.Transform.Scale = settings.CalculateScale(bounds.Size);
            var cameraTarget = perspectiveViewport.Camera.To;
            var target = new Vector3(cameraTarget.X, 0f, cameraTarget.Z);
            if (settings.UsesGrid)
            {
                target.X = MathF.Round(target.X / InteriorViewportControl.GridSpacingMeters) *
                           InteriorViewportControl.GridSpacingMeters;
                target.Z = MathF.Round(target.Z / InteriorViewportControl.GridSpacingMeters) *
                           InteriorViewportControl.GridSpacingMeters;
            }
            model.Transform.Position = InteriorAssetPlacementSettings.CalculatePosition(
                bounds, model.Transform.Scale, target, settings.EffectivePivot);
            model.CaptureMeshMaterialIndices();
            model.CaptureProceduralGeometry();
            _pendingAssetPlacement = new PendingAssetPlacement(asset, settings, model, bounds);
            if (_pbrPreviewEnabled)
                pbrPreviewToolButton.Checked = false;
            RefreshViewportScene();
            perspectiveViewport.BeginPlacement(model.Id, settings.UsesGrid);
            var rayDirection = perspectiveViewport.Camera.To - perspectiveViewport.Camera.From;
            if (rayDirection.LengthSquared() < .000001f)
                rayDirection = -Vector3.UnitZ;
            var initialPlacement = new InteriorPlacementPointEventArgs(target, perspectiveViewport.Camera.From,
                Vector3.Normalize(rayDirection), hasGroundPoint: true);
            UpdatePendingPlacement(_pendingAssetPlacement, initialPlacement);
            UpdatePlacementCollisionStatus(_pendingAssetPlacement);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            MessageBox.Show(this, $"無法置放模型：\r\n\r\n{exception.Message}", "置放本機模型",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            ShowExplanation($"無法置放「{asset.Name}」：{exception.Message}", ExplanationKind.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void PerspectiveViewport_PlacementPointChanged(object? sender, InteriorPlacementPointEventArgs e)
    {
        if (_pendingAssetPlacement is not { } pending)
            return;
        UpdatePendingPlacement(pending, e);
        // The pending model is shared by every viewport. During pointer movement only the
        // perspective view needs immediate feedback; repaint all views after confirmation.
        perspectiveViewport.Invalidate();
    }

    private async void PerspectiveViewport_PlacementConfirmed(object? sender, InteriorPlacementPointEventArgs e)
    {
        if (_pendingAssetPlacement is not { } pending)
            return;
        UpdatePendingPlacement(pending, e);
        if (!pending.HasValidSurface)
        {
            ShowExplanation($"步驟 2/3 無法確認位置：「{pending.Asset.Name}」{pending.InvalidSurfaceReason}",
                ExplanationKind.Error);
            return;
        }
        var collisions = placementBoundaryDetectionMenuItem.Checked
            ? FindPlacementCollisions(pending)
            : [];
        if (collisions.Count > 0 &&
            pending.Settings.CollisionPolicy == InteriorAssetCollisionPolicy.禁止重疊)
        {
            ShowExplanation(
                $"步驟 2/3 無法確認位置：「{pending.Asset.Name}」與 {string.Join("、", collisions)} 重疊。請移至綠框位置，或將碰撞處理改為「警告但允許」。",
                ExplanationKind.Error);
            return;
        }
        var parameters = new ImportedAssetParameters
        {
            Name = pending.Asset.Name,
            Category = pending.Asset.Category,
            ModelPath = pending.Asset.ModelPath,
            License = pending.Asset.License,
            SourceUrl = pending.Asset.SourceUrl,
            QualityStatus = pending.Asset.QualityStatus,
            Unit = pending.Settings.Unit,
            Pivot = pending.Settings.Pivot,
            SnapToGrid = pending.Settings.SnapToGrid,
            PlacementMode = pending.Settings.PlacementMode,
            CollisionPolicy = pending.Settings.CollisionPolicy,
            CollisionToleranceCentimeters = pending.Settings.CollisionToleranceCentimeters,
            TargetWidthCentimeters = pending.Settings.WidthCentimeters,
            TargetDepthCentimeters = pending.Settings.DepthCentimeters,
            TargetHeightCentimeters = pending.Settings.HeightCentimeters
        };
        var designObject = new ParametricDesignObject
        {
            Id = pending.Model.Id,
            Parameters = parameters,
            Model = pending.Model
        };
        RecordUndoSnapshot($"置放本機模型「{pending.Asset.Name}」");
        _pendingAssetPlacement = null;
        perspectiveViewport.EndPlacement();
        _designObjects.Add(designObject);
        RefreshViewportScene();
        var node = CreateDesignObjectNode(designObject);
        sceneTreeView.Nodes[0].Nodes.Add(node);
        sceneTreeView.Nodes[0].Expand();
        sceneTreeView.SelectedNode = node;
        _hasUnexportedChanges = true;
        MarkProjectDirty();
        var continuePlacement = ModifierKeys.HasFlag(Keys.Shift);
        if (!continuePlacement)
            TransformToolButton_Click(moveToolButton, EventArgs.Empty);
        if (collisions.Count == 0)
        {
            var detectionNote = placementBoundaryDetectionMenuItem.Checked
                ? string.Empty
                : "（快速置放，未偵測邊界）";
            ShowExplanation(continuePlacement
                ? $"步驟 3/3：已置放 {pending.Asset.Name}{detectionNote}；Shift 連續置放正在準備下一個相同模型…"
                : $"步驟 3/3：已置放 {pending.Asset.Name}{detectionNote}；已選取新模型並進入移動模式，可拖曳 Gizmo 微調位置。");
        }
        else
        {
            var nextStep = continuePlacement
                ? "Shift 連續置放正在準備下一個相同模型…"
                : "已選取新模型並進入移動模式，可拖曳 Gizmo 調整重疊位置。";
            ShowExplanation($"步驟 3/3：已強制置放 {pending.Asset.Name}；與 {string.Join("、", collisions)} 重疊。{nextStep}",
                ExplanationKind.Warning);
        }
        if (continuePlacement)
            await PlaceAssetAsync(pending.Asset, pending.Settings.Copy());
    }

    private void PerspectiveViewport_DragEnter(object? sender, DragEventArgs e)
    {
        if (TryGetDraggedAsset(e.Data, out var dragData))
        {
            e.Effect = DragDropEffects.Copy;
            ShowExplanation($"步驟 1/3：放開滑鼠，將「{dragData.Asset.Name}」拖放到目前透視圖位置。");
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private void PerspectiveViewport_DragOver(object? sender, DragEventArgs e) =>
        e.Effect = TryGetDraggedAsset(e.Data, out _) ? DragDropEffects.Copy : DragDropEffects.None;

    private async void PerspectiveViewport_DragDrop(object? sender, DragEventArgs e)
    {
        var restorePbrPreview = ReferenceEquals(sender, previewViewport) && _pbrPreviewEnabled;
        try
        {
            if (!TryGetDraggedAsset(e.Data, out var dragData))
                return;
            var screenPoint = new Point(e.X, e.Y);
            var hasPlacementPoint = ReferenceEquals(sender, previewViewport)
                ? previewViewport.TryGetPlacementPointerFromScreen(screenPoint, out var placement)
                : perspectiveViewport.TryGetPlacementPointer(perspectiveViewport.PointToClient(screenPoint),
                    out placement);
            if (!hasPlacementPoint)
            {
                ShowExplanation($"無法置放「{dragData.Asset.Name}」：請拖放到透視圖的模型顯示區域內。",
                    ExplanationKind.Error);
                return;
            }
            await PlaceAssetAsync(dragData.Asset, dragData.Settings);
            if (_pendingAssetPlacement is null)
                return;
            PerspectiveViewport_PlacementConfirmed(perspectiveViewport, placement);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            var message = exception.GetBaseException().Message;
            CancelAssetPlacement(showStatus: false);
            ShowExplanation($"拖放模型失敗：{message}", ExplanationKind.Error);
        }
        finally
        {
            if (restorePbrPreview && _pendingAssetPlacement is null && !pbrPreviewToolButton.Checked)
                pbrPreviewToolButton.Checked = true;
        }
    }

    private static bool TryGetDraggedAsset(IDataObject? data, out InteriorAssetDragData dragData)
    {
        dragData = null!;
        if (data?.GetDataPresent(InteriorAssetDragData.DataFormat, autoConvert: false) != true ||
            data.GetData(InteriorAssetDragData.DataFormat, autoConvert: false) is not InteriorAssetDragData value)
            return false;
        dragData = value;
        return value.Asset.CanPlace;
    }

    private void PerspectiveViewport_PlacementCanceled(object? sender, EventArgs e) => CancelAssetPlacement();

    private void UpdatePendingPlacement(PendingAssetPlacement pending, InteriorPlacementPointEventArgs placement)
    {
        if (!TryResolvePlacementSurface(pending, placement, out var surface, out var reason))
        {
            pending.HasValidSurface = false;
            pending.InvalidSurfaceReason = reason;
            pending.SurfaceHit = null;
            UpdatePlacementCollisionStatus(pending);
            return;
        }
        ApplySurfacePlacement(pending, surface);
        pending.HasValidSurface = !placementBoundaryDetectionMenuItem.Checked ||
                                  ValidateSurfaceContainment(pending, surface, out reason);
        pending.InvalidSurfaceReason = pending.HasValidSurface ? string.Empty : reason;
        pending.SurfaceHit = surface;
        UpdatePlacementCollisionStatus(pending);
    }

    private bool TryResolvePlacementSurface(PendingAssetPlacement pending,
        InteriorPlacementPointEventArgs placement, out PlacementSurfaceHit surface, out string reason)
    {
        var requirement = GetSurfaceRequirement(pending);
        if (requirement == PlacementSurfaceRequirement.Ground)
        {
            if (!placement.HasGroundPoint)
            {
                surface = default;
                reason = "目前視線沒有與地面相交，請將游標移到格點地面上。";
                return false;
            }
            surface = new PlacementSurfaceHit(placement.Point, Vector3.UnitY, null, null, "格點地面", requirement);
            reason = string.Empty;
            return true;
        }

        if (TryFindPlacementSurface(requirement, placement.RayOrigin, placement.RayDirection, out surface))
        {
            surface = surface with { Point = SnapSurfacePoint(surface.Point, surface.Normal, pending.Settings.UsesGrid) };
            reason = string.Empty;
            return true;
        }

        if (requirement is (PlacementSurfaceRequirement.Floor or PlacementSurfaceRequirement.HorizontalSurface) &&
            placement.HasGroundPoint && !placementBoundaryDetectionMenuItem.Checked)
        {
            if (TryFindFastFloorSurface(placement.Point, out surface))
            {
                surface = surface with
                {
                    Point = SnapSurfacePoint(surface.Point, surface.Normal, pending.Settings.UsesGrid)
                };
                reason = string.Empty;
                return true;
            }

            surface = new PlacementSurfaceHit(
                SnapSurfacePoint(placement.Point, Vector3.UnitY, pending.Settings.UsesGrid),
                Vector3.UnitY, null, null, "格點地面（快速退回）", PlacementSurfaceRequirement.Ground);
            reason = string.Empty;
            return true;
        }

        var hasDesignedFloor = _designObjects.Any(item => item.Model.IsVisible && IsFloorSurface(item));
        if (requirement is (PlacementSurfaceRequirement.Floor or PlacementSurfaceRequirement.HorizontalSurface) &&
            !hasDesignedFloor && placement.HasGroundPoint)
        {
            surface = new PlacementSurfaceHit(
                SnapSurfacePoint(placement.Point, Vector3.UnitY, pending.Settings.UsesGrid),
                Vector3.UnitY, null, null, "格點地面", PlacementSurfaceRequirement.Ground);
            reason = string.Empty;
            return true;
        }

        surface = default;
        reason = requirement switch
        {
            PlacementSurfaceRequirement.Wall => "游標沒有指向有效牆面，請將游標移到牆體範圍內。",
            PlacementSurfaceRequirement.Ceiling => "游標沒有指向有效天花板，請將游標移到天花板範圍內。",
            PlacementSurfaceRequirement.HorizontalSurface => "游標沒有指向地板、桌面、檯面或櫃體頂面。",
            _ => "游標沒有指向有效地板範圍。"
        };
        return false;
    }

    private bool TryFindFastFloorSurface(Vector3 groundPoint, out PlacementSurfaceHit surface)
    {
        const float edgeTolerance = .02f;
        var candidates = _designObjects
            .Where(item => item.Model.IsVisible && IsFloorSurface(item))
            .Select(item => new
            {
                Item = item,
                HasBounds = SceneTraversal.TryCalculateBounds(item.Model, out var bounds),
                Bounds = bounds
            })
            .Where(candidate => candidate.HasBounds &&
                                groundPoint.X >= candidate.Bounds.Minimum.X - edgeTolerance &&
                                groundPoint.X <= candidate.Bounds.Maximum.X + edgeTolerance &&
                                groundPoint.Z >= candidate.Bounds.Minimum.Z - edgeTolerance &&
                                groundPoint.Z <= candidate.Bounds.Maximum.Z + edgeTolerance)
            .OrderByDescending(candidate => candidate.Bounds.Maximum.Y)
            .ThenBy(candidate => candidate.Bounds.Size.X * candidate.Bounds.Size.Z)
            .FirstOrDefault();

        if (candidates is null)
        {
            surface = default;
            return false;
        }

        surface = new PlacementSurfaceHit(
            new Vector3(groundPoint.X, candidates.Bounds.Maximum.Y, groundPoint.Z),
            Vector3.UnitY, candidates.Bounds, candidates.Item.Id,
            $"{candidates.Item.Parameters.Name}完成面（快速吸附）", PlacementSurfaceRequirement.Floor);
        return true;
    }

    private bool TryFindPlacementSurface(PlacementSurfaceRequirement requirement, Vector3 rayOrigin,
        Vector3 rayDirection, out PlacementSurfaceHit surface)
    {
        surface = default;
        var nearestDistance = float.PositiveInfinity;
        var found = false;
        foreach (var item in _designObjects)
        {
            if (!item.Model.IsVisible || !SceneTraversal.TryCalculateBounds(item.Model, out var bounds) ||
                !TryIntersectBounds(rayOrigin, rayDirection, bounds, out var distance, out var normal) ||
                distance >= nearestDistance || !IsValidSurface(item, normal, requirement))
                continue;
            nearestDistance = distance;
            surface = new PlacementSurfaceHit(rayOrigin + rayDirection * distance, normal, bounds, item.Id,
                item.Parameters.Name, requirement);
            found = true;
        }
        return found;
    }

    private static PlacementSurfaceRequirement GetSurfaceRequirement(PendingAssetPlacement pending)
    {
        if (pending.Settings.PlacementMode is InteriorAssetPlacementMode.自由 or InteriorAssetPlacementMode.格點)
            return PlacementSurfaceRequirement.Ground;
        if (pending.Settings.PlacementMode == InteriorAssetPlacementMode.落地)
            return PlacementSurfaceRequirement.Floor;
        var text = $"{pending.Asset.Category} {pending.Asset.Subcategory} {pending.Asset.Name}";
        if (ContainsAny(text, "吊燈", "吸頂燈", "嵌燈", "軌道燈", "pendant", "ceiling light", "downlight"))
            return PlacementSurfaceRequirement.Ceiling;
        if (pending.Asset.Category == "門窗" ||
            ContainsAny(text, "壁燈", "壁掛", "掛畫", "窗簾", "冷氣", "植生牆", "吊櫃",
                "wall", "door", "window", "curtain", "wall-mounted"))
            return PlacementSurfaceRequirement.Wall;
        var compactSurfaceAsset = pending.Asset.Category is "植栽" or "軟裝" or "燈具" &&
                                  pending.Settings.WidthCentimeters <= 60m &&
                                  pending.Settings.DepthCentimeters <= 60m &&
                                  pending.Settings.HeightCentimeters <= 80m;
        if (compactSurfaceAsset ||
            ContainsAny(text, "檯燈", "桌上", "抱枕", "水槽", "table lamp", "desk lamp", "cushion", "tabletop"))
            return PlacementSurfaceRequirement.HorizontalSurface;
        return PlacementSurfaceRequirement.Floor;
    }

    private static bool IsValidSurface(ParametricDesignObject item, Vector3 normal,
        PlacementSurfaceRequirement requirement) => requirement switch
    {
        PlacementSurfaceRequirement.Floor => normal.Y > .5f && IsFloorSurface(item),
        PlacementSurfaceRequirement.HorizontalSurface => normal.Y > .5f && IsHorizontalSupportSurface(item),
        PlacementSurfaceRequirement.Wall => MathF.Abs(normal.Y) < .2f && IsWallSurface(item),
        PlacementSurfaceRequirement.Ceiling => MathF.Abs(normal.Y) > .5f && IsCeilingSurface(item),
        _ => false
    };

    private static bool IsFloorSurface(ParametricDesignObject item)
    {
        if (item.Parameters is ParametricSlabParameters { Kind: ParametricSlabKind.Floor })
            return true;
        if (ContainsAny(item.Parameters.Name, "地板", "地面", "地毯", "floor", "ground", "rug"))
            return true;
        return SceneTraversal.TryCalculateBounds(item.Model, out var bounds) &&
               InteriorPlacementGeometry.LooksLikeBroadHorizontalSlab(item.Model, bounds);
    }

    private static bool IsWallSurface(ParametricDesignObject item) =>
        ContainsAny(item.Parameters.Name, "牆", "wall");

    private static bool IsCeilingSurface(ParametricDesignObject item) =>
        item.Parameters is ParametricSlabParameters { Kind: ParametricSlabKind.Ceiling } ||
        ContainsAny(item.Parameters.Name, "天花", "ceiling");

    private static bool IsHorizontalSupportSurface(ParametricDesignObject item)
    {
        if (IsFloorSurface(item) || ContainsAny(item.Parameters.Name,
                "桌", "檯", "櫃", "床", "層架", "中島", "工作台", "table", "desk", "counter", "cabinet", "shelf", "bed"))
            return true;
        if (IsWallSurface(item) || IsCeilingSurface(item) ||
            !SceneTraversal.TryCalculateBounds(item.Model, out var bounds))
            return false;
        return InteriorPlacementGeometry.HasUsableTopSurface(item.Model, bounds);
    }

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static Vector3 SnapSurfacePoint(Vector3 point, Vector3 normal, bool enabled)
    {
        if (!enabled)
            return point;
        if (MathF.Abs(normal.X) > .5f)
            return new Vector3(point.X, SnapCoordinate(point.Y), SnapCoordinate(point.Z));
        if (MathF.Abs(normal.Z) > .5f)
            return new Vector3(SnapCoordinate(point.X), SnapCoordinate(point.Y), point.Z);
        return new Vector3(SnapCoordinate(point.X), point.Y, SnapCoordinate(point.Z));
    }

    private static float SnapCoordinate(float value) =>
        MathF.Round(value / InteriorViewportControl.GridSpacingMeters) * InteriorViewportControl.GridSpacingMeters;

    private static void ApplySurfacePlacement(PendingAssetPlacement pending, PlacementSurfaceHit surface)
    {
        const float surfaceGap = .005f;
        var model = pending.Model;
        var scale = model.Transform.Scale;
        var scaledCenter = pending.RawBounds.Center * scale;
        if (surface.Kind is PlacementSurfaceRequirement.Ground or PlacementSurfaceRequirement.Floor or
            PlacementSurfaceRequirement.HorizontalSurface)
        {
            model.Transform.Position = InteriorAssetPlacementSettings.CalculatePosition(
                pending.RawBounds, scale, surface.Point + Vector3.UnitY * surfaceGap,
                pending.Settings.EffectivePivot);
            return;
        }

        if (surface.Kind == PlacementSurfaceRequirement.Ceiling)
        {
            var scaledMaximum = pending.RawBounds.Maximum * scale;
            model.Transform.Position = new Vector3(
                surface.Point.X - scaledCenter.X,
                surface.Point.Y - surfaceGap - scaledMaximum.Y,
                surface.Point.Z - scaledCenter.Z);
            return;
        }

        var yaw = MathF.Atan2(surface.Normal.X, surface.Normal.Z) * 180f / MathF.PI;
        model.Transform.RotationDegrees = new Vector3(0f, yaw, 0f);
        var rotation = Matrix4x4.CreateRotationY(yaw * MathF.PI / 180f);
        var rotatedCenter = Vector3.Transform(scaledCenter, rotation);
        var depth = MathF.Abs(pending.RawBounds.Size.Z * scale.Z) * .5f;
        var desiredCenter = surface.Point + surface.Normal * (depth + surfaceGap);
        model.Transform.Position = desiredCenter - rotatedCenter;
    }

    private static bool ValidateSurfaceContainment(PendingAssetPlacement pending, PlacementSurfaceHit surface,
        out string reason)
    {
        reason = string.Empty;
        if (surface.HostBounds is not SceneBounds host ||
            !SceneTraversal.TryCalculateBounds(pending.Model, out var candidate))
            return true;
        const float tolerance = .01f;
        var contained = surface.Kind switch
        {
            PlacementSurfaceRequirement.Wall when MathF.Abs(surface.Normal.X) > .5f =>
                candidate.Minimum.Y >= host.Minimum.Y - tolerance && candidate.Maximum.Y <= host.Maximum.Y + tolerance &&
                candidate.Minimum.Z >= host.Minimum.Z - tolerance && candidate.Maximum.Z <= host.Maximum.Z + tolerance,
            PlacementSurfaceRequirement.Wall =>
                candidate.Minimum.X >= host.Minimum.X - tolerance && candidate.Maximum.X <= host.Maximum.X + tolerance &&
                candidate.Minimum.Y >= host.Minimum.Y - tolerance && candidate.Maximum.Y <= host.Maximum.Y + tolerance,
            _ => candidate.Minimum.X >= host.Minimum.X - tolerance && candidate.Maximum.X <= host.Maximum.X + tolerance &&
                 candidate.Minimum.Z >= host.Minimum.Z - tolerance && candidate.Maximum.Z <= host.Maximum.Z + tolerance
        };
        if (contained)
            return true;
        reason = $"模型超出「{surface.SurfaceName}」的可用邊界，請往表面中央移動。";
        return false;
    }

    private static bool TryIntersectBounds(Vector3 origin, Vector3 direction, SceneBounds bounds,
        out float distance, out Vector3 normal)
    {
        distance = 0f;
        normal = Vector3.Zero;
        var farDistance = float.PositiveInfinity;
        for (var axis = 0; axis < 3; axis++)
        {
            var componentOrigin = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
            var componentDirection = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
            var minimum = axis == 0 ? bounds.Minimum.X : axis == 1 ? bounds.Minimum.Y : bounds.Minimum.Z;
            var maximum = axis == 0 ? bounds.Maximum.X : axis == 1 ? bounds.Maximum.Y : bounds.Maximum.Z;
            if (MathF.Abs(componentDirection) < .000001f)
            {
                if (componentOrigin < minimum || componentOrigin > maximum)
                    return false;
                continue;
            }
            var near = (minimum - componentOrigin) / componentDirection;
            var far = (maximum - componentOrigin) / componentDirection;
            var axisVector = axis == 0 ? Vector3.UnitX : axis == 1 ? Vector3.UnitY : Vector3.UnitZ;
            var nearNormal = componentDirection > 0f ? -axisVector : axisVector;
            if (near > far)
                (near, far) = (far, near);
            if (near > distance)
            {
                distance = near;
                normal = nearNormal;
            }
            farDistance = Math.Min(farDistance, far);
            if (distance > farDistance)
                return false;
        }
        return farDistance >= Math.Max(0f, distance) && distance >= 0f && normal != Vector3.Zero;
    }

    private void UpdatePlacementCollisionStatus(PendingAssetPlacement? pending)
    {
        if (pending is null)
            return;
        if (!pending.HasValidSurface)
        {
            perspectiveViewport.SetPlacementCollision(true);
            ShowExplanation($"步驟 2/3 無法置放「{pending.Asset.Name}」：{pending.InvalidSurfaceReason}",
                ExplanationKind.Error);
            return;
        }
        if (!placementBoundaryDetectionMenuItem.Checked)
        {
            perspectiveViewport.SetPlacementCollision(false);
            var surfaceName = pending.SurfaceHit?.SurfaceName ?? "有效表面";
            ShowExplanation(
                $"步驟 2/3：{pending.Asset.Name} 已吸附至「{surfaceName}」；快速置放未偵測模型重疊及表面邊界，按左鍵確認。",
                ExplanationKind.Warning);
            return;
        }
        var collisions = FindPlacementCollisions(pending);
        perspectiveViewport.SetPlacementCollision(collisions.Count > 0);
        if (collisions.Count == 0)
        {
            var surfaceName = pending.SurfaceHit?.SurfaceName ?? "有效表面";
            ShowExplanation($"步驟 2/3：{pending.Asset.Name} 已吸附至「{surfaceName}」；綠框按左鍵確認，Shift+左鍵連續置放，右鍵或 Esc 取消。");
        }
        else if (pending.Settings.CollisionPolicy == InteriorAssetCollisionPolicy.禁止重疊)
        {
            ShowExplanation(
                $"步驟 2/3 無法置放「{pending.Asset.Name}」：與 {string.Join("、", collisions)} 重疊；紅框位置禁止置放。",
                ExplanationKind.Error);
        }
        else
        {
            ShowExplanation($"步驟 2/3 碰撞警告：與 {string.Join("、", collisions)} 重疊；目前允許強制置放。",
                ExplanationKind.Warning);
        }
    }

    private IReadOnlyList<string> FindPlacementCollisions(PendingAssetPlacement pending)
    {
        if (!SceneTraversal.TryCalculateBounds(pending.Model, out var candidate))
            return [];
        var tolerance = Math.Max(0f, (float)pending.Settings.CollisionToleranceCentimeters / 100f);
        var candidateParts = InteriorPlacementGeometry.GetWorldCollisionBounds(pending.Model);
        var hostObjectId = pending.SurfaceHit?.HostObjectId;
        return _designObjects
            .Where(item => item.Id != hostObjectId && !IsNonBlockingPlacementSurface(item))
            .Where(item => SceneTraversal.TryCalculateBounds(item.Model, out var bounds) &&
                           BoundsOverlap(candidate, bounds, tolerance) &&
                           CollisionPartsOverlap(candidateParts,
                               InteriorPlacementGeometry.GetWorldCollisionBounds(item.Model), tolerance))
            .Select(item => item.Parameters.Name)
            .Distinct(StringComparer.CurrentCulture)
            .Take(4)
            .ToArray();
    }

    private void PlacementBoundaryDetectionMenuItem_CheckedChanged(object? sender, EventArgs e)
    {
        if (_pendingAssetPlacement is { SurfaceHit: { } surface } pending)
        {
            var reason = string.Empty;
            pending.HasValidSurface = !placementBoundaryDetectionMenuItem.Checked ||
                                      ValidateSurfaceContainment(pending, surface, out reason);
            pending.InvalidSurfaceReason = pending.HasValidSurface ? string.Empty : reason;
            UpdatePlacementCollisionStatus(pending);
            foreach (var viewport in GetViewports())
                viewport.Invalidate();
            return;
        }

        ShowExplanation(placementBoundaryDetectionMenuItem.Checked
                ? "已啟用偵測置放邊界：置放時會檢查支撐面範圍及模型零件碰撞。"
                : "已關閉偵測置放邊界：使用快速置放，不檢查模型重疊及支撐面邊界。",
            placementBoundaryDetectionMenuItem.Checked ? ExplanationKind.Information : ExplanationKind.Warning);
    }

    private static bool BoundsOverlap(SceneBounds first, SceneBounds second, float tolerance) =>
        Math.Min(first.Maximum.X, second.Maximum.X) - Math.Max(first.Minimum.X, second.Minimum.X) > tolerance &&
        Math.Min(first.Maximum.Y, second.Maximum.Y) - Math.Max(first.Minimum.Y, second.Minimum.Y) > tolerance &&
        Math.Min(first.Maximum.Z, second.Maximum.Z) - Math.Max(first.Minimum.Z, second.Minimum.Z) > tolerance;

    private static bool CollisionPartsOverlap(IReadOnlyList<SceneBounds> first,
        IReadOnlyList<SceneBounds> second, float tolerance)
    {
        if (first.Count == 0 || second.Count == 0)
            return false;
        foreach (var firstPart in first)
        foreach (var secondPart in second)
            if (BoundsOverlap(firstPart, secondPart, tolerance))
                return true;
        return false;
    }

    private static bool IsPlacementGroundSurface(ParametricDesignObject item)
    {
        var name = item.Parameters.Name;
        return name.Contains("地板", StringComparison.Ordinal) ||
               name.Contains("地毯", StringComparison.Ordinal);
    }

    private static bool IsNonBlockingPlacementSurface(ParametricDesignObject item) =>
        IsPlacementGroundSurface(item) ||
        item.Parameters.Name.Contains("天花板", StringComparison.Ordinal);

    private string? TrySnapMovedSelectionToSurface()
    {
        if (!snapEnabledMenuItem.Checked || !surfaceSnapMenuItem.Checked ||
            _selectedDesignObject is null || _selectedMeshIndex is not null ||
            !SceneTraversal.TryCalculateBounds(_selectedDesignObject.Model, out var candidate))
        {
            SetTransformCollisionWarnings([]);
            return null;
        }

        const float maximumSnapDistance = .25f;
        const float surfaceGap = .005f;
        var selectedIds = _selectedModelIds.Count > 0
            ? _selectedModelIds
            : new HashSet<Guid> { _selectedDesignObject.Id };
        var selectedName = _selectedDesignObject.Parameters.Name;
        var wallMounted = ContainsAny(selectedName,
            "壁燈", "壁掛", "掛畫", "窗簾", "冷氣", "門", "窗", "wall", "door", "window", "curtain");
        string? snappedSurface = null;

        if (wallMounted)
        {
            var bestDelta = float.PositiveInfinity;
            var bestWallUsesXNormal = true;
            foreach (var host in _designObjects.Where(item => !selectedIds.Contains(item.Id) &&
                                                               item.Model.IsVisible && IsWallSurface(item)))
            {
                if (!SceneTraversal.TryCalculateBounds(host.Model, out var wall))
                    continue;
                var wallUsesXNormal = wall.Maximum.X - wall.Minimum.X <= wall.Maximum.Z - wall.Minimum.Z;
                if (wallUsesXNormal)
                {
                    if (!RangesOverlap(candidate.Minimum.Y, candidate.Maximum.Y, wall.Minimum.Y, wall.Maximum.Y) ||
                        !RangesOverlap(candidate.Minimum.Z, candidate.Maximum.Z, wall.Minimum.Z, wall.Maximum.Z))
                        continue;
                    var delta = NearestFaceDelta(candidate.Minimum.X, candidate.Maximum.X,
                        wall.Minimum.X, wall.Maximum.X, surfaceGap);
                    if (MathF.Abs(delta) >= MathF.Abs(bestDelta) || MathF.Abs(delta) > maximumSnapDistance)
                        continue;
                    bestDelta = delta;
                    bestWallUsesXNormal = true;
                    snappedSurface = host.Parameters.Name;
                }
                else
                {
                    if (!RangesOverlap(candidate.Minimum.Y, candidate.Maximum.Y, wall.Minimum.Y, wall.Maximum.Y) ||
                        !RangesOverlap(candidate.Minimum.X, candidate.Maximum.X, wall.Minimum.X, wall.Maximum.X))
                        continue;
                    var delta = NearestFaceDelta(candidate.Minimum.Z, candidate.Maximum.Z,
                        wall.Minimum.Z, wall.Maximum.Z, surfaceGap);
                    if (MathF.Abs(delta) >= MathF.Abs(bestDelta) || MathF.Abs(delta) > maximumSnapDistance)
                        continue;
                    bestDelta = delta;
                    bestWallUsesXNormal = false;
                    snappedSurface = host.Parameters.Name;
                }
            }
            if (snappedSurface is not null)
                _selectedDesignObject.Model.Transform.Position += bestWallUsesXNormal
                    ? new Vector3(bestDelta, 0f, 0f)
                    : new Vector3(0f, 0f, bestDelta);
        }
        else
        {
            var targetHeight = 0f;
            var bestDelta = targetHeight + surfaceGap - candidate.Minimum.Y;
            snappedSurface = MathF.Abs(bestDelta) <= maximumSnapDistance ? "格點地面" : null;
            if (snappedSurface is null)
                bestDelta = float.PositiveInfinity;

            foreach (var host in _designObjects.Where(item => !selectedIds.Contains(item.Id) &&
                                                               item.Model.IsVisible && IsHorizontalSupportSurface(item)))
            {
                if (!SceneTraversal.TryCalculateBounds(host.Model, out var support) ||
                    !RangesOverlap(candidate.Minimum.X, candidate.Maximum.X, support.Minimum.X, support.Maximum.X) ||
                    !RangesOverlap(candidate.Minimum.Z, candidate.Maximum.Z, support.Minimum.Z, support.Maximum.Z))
                    continue;
                var delta = support.Maximum.Y + surfaceGap - candidate.Minimum.Y;
                if (MathF.Abs(delta) >= MathF.Abs(bestDelta) || MathF.Abs(delta) > maximumSnapDistance)
                    continue;
                bestDelta = delta;
                snappedSurface = host.Parameters.Name;
            }
            if (snappedSurface is not null)
                _selectedDesignObject.Model.Transform.Position += new Vector3(0f, bestDelta, 0f);
        }

        if (placementBoundaryDetectionMenuItem.Checked)
        {
            var collisions = FindMovedModelCollisions(_selectedDesignObject, selectedIds);
            if (collisions.Count > 0)
            {
                SetTransformCollisionWarnings(selectedIds);
                return $"碰撞警告：{GetSelectedTargetName()}與 {string.Join("、", collisions)} 重疊；目前保留移動結果。";
            }
        }
        SetTransformCollisionWarnings([]);
        return snappedSurface is null
            ? null
            : $"已將{GetSelectedTargetName()}吸附至「{snappedSurface}」。";
    }

    private IReadOnlyList<string> FindMovedModelCollisions(ParametricDesignObject moved,
        IReadOnlySet<Guid> selectedIds)
    {
        if (!SceneTraversal.TryCalculateBounds(moved.Model, out var candidate))
            return [];
        var candidateParts = InteriorPlacementGeometry.GetWorldCollisionBounds(moved.Model);
        return _designObjects
            .Where(item => !selectedIds.Contains(item.Id) && !IsNonBlockingPlacementSurface(item))
            .Where(item => SceneTraversal.TryCalculateBounds(item.Model, out var bounds) &&
                           BoundsOverlap(candidate, bounds, .005f) &&
                           CollisionPartsOverlap(candidateParts,
                               InteriorPlacementGeometry.GetWorldCollisionBounds(item.Model), .005f))
            .Select(item => item.Parameters.Name)
            .Distinct(StringComparer.CurrentCulture)
            .Take(4)
            .ToArray();
    }

    private void SetTransformCollisionWarnings(IEnumerable<Guid> modelIds)
    {
        foreach (var viewport in GetViewports())
            viewport.SetCollisionWarningModels(modelIds);
    }

    private static bool RangesOverlap(float firstMinimum, float firstMaximum,
        float secondMinimum, float secondMaximum) =>
        Math.Min(firstMaximum, secondMaximum) - Math.Max(firstMinimum, secondMinimum) > .01f;

    private static float NearestFaceDelta(float candidateMinimum, float candidateMaximum,
        float surfaceMinimum, float surfaceMaximum, float gap)
    {
        var towardMinimum = surfaceMinimum - gap - candidateMaximum;
        var towardMaximum = surfaceMaximum + gap - candidateMinimum;
        return MathF.Abs(towardMinimum) <= MathF.Abs(towardMaximum) ? towardMinimum : towardMaximum;
    }

    private void CancelAssetPlacement(bool showStatus = true)
    {
        if (_pendingAssetPlacement is null)
            return;
        var name = _pendingAssetPlacement.Asset.Name;
        _pendingAssetPlacement = null;
        perspectiveViewport.EndPlacement();
        RefreshViewportScene();
        if (showStatus)
            ShowExplanation($"置放已取消：{name}；未新增模型。");
    }

}
