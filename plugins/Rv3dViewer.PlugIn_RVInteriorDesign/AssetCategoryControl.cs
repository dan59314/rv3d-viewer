namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using Rv3dViewer.Core;

internal sealed partial class AssetCategoryControl : UserControl
{
    private IReadOnlyList<InteriorAssetDescriptor> _assets = [];
    private InteriorAssetPlacementSettings? _placementSettings;
    private int _thumbnailGeneration;
    private CancellationTokenSource? _thumbnailCancellation;
    private bool _thumbnailLoadingEnabled;
    private bool _updatingItems;
    private bool _isDisposing;
    private int _previewGeneration;
    private CancellationTokenSource? _previewCancellation;
    private SceneModel? _previewModel;

    public AssetCategoryControl()
    {
        InitializeComponent();
        sourceComboBox.SelectedIndex = 0;
    }

    public event EventHandler<InteriorAssetDescriptor>? AssetSelected;

    public event EventHandler<InteriorAssetPlacementRequestEventArgs>? PlaceRequested;

    public event EventHandler<InteriorAssetDescriptor>? EditRequested;

    public event EventHandler<InteriorAssetDescriptor>? DeleteRequested;

    public event EventHandler<InteriorAssetDescriptor>? SearchOnlineRequested;

    public event EventHandler? PlacementSplitterMoved;

    public event EventHandler? PreviewSplitterMoved;

    internal int PlacementPanelHeight => Math.Max(placementSplitContainer.Panel2MinSize,
        placementSplitContainer.ClientSize.Height - placementSplitContainer.SplitterDistance -
        placementSplitContainer.SplitterWidth);

    internal void SetPlacementPanelHeight(int height)
    {
        var available = placementSplitContainer.ClientSize.Height;
        if (available <= placementSplitContainer.Panel1MinSize + placementSplitContainer.SplitterWidth)
            return;
        var maximumHeight = available - placementSplitContainer.SplitterWidth -
                            placementSplitContainer.Panel1MinSize;
        var desiredHeight = Math.Clamp(height, placementSplitContainer.Panel2MinSize, maximumHeight);
        placementSplitContainer.SplitterDistance = available - placementSplitContainer.SplitterWidth - desiredHeight;
    }

    internal int PreviewPanelHeight => Math.Max(previewSettingsSplitContainer.Panel1MinSize,
        previewSettingsSplitContainer.SplitterDistance);

    internal void SetPreviewPanelHeight(int height)
    {
        var available = previewSettingsSplitContainer.ClientSize.Height;
        if (available <= previewSettingsSplitContainer.Panel1MinSize +
            previewSettingsSplitContainer.Panel2MinSize + previewSettingsSplitContainer.SplitterWidth)
            return;
        var maximumHeight = available - previewSettingsSplitContainer.SplitterWidth -
                            previewSettingsSplitContainer.Panel2MinSize;
        previewSettingsSplitContainer.SplitterDistance = Math.Clamp(height,
            previewSettingsSplitContainer.Panel1MinSize, maximumHeight);
    }

    public void SetAssets(IEnumerable<InteriorAssetDescriptor> assets)
    {
        _assets = assets.ToArray();
        ApplyFilter();
    }

    internal void SetThumbnailLoadingEnabled(bool enabled)
    {
        if (_thumbnailLoadingEnabled == enabled)
            return;
        _thumbnailLoadingEnabled = enabled;
        if (!enabled)
        {
            _thumbnailGeneration++;
            _thumbnailCancellation?.Cancel();
            _thumbnailCancellation?.Dispose();
            _thumbnailCancellation = null;
            return;
        }

        StartThumbnailLoading(assetListView.Items.Cast<ListViewItem>());
    }

    private void FilterControl_Changed(object? sender, EventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = searchTextBox.Text.Trim();
        var source = sourceComboBox.SelectedItem?.ToString() ?? "全部來源";
        var filtered = _assets.Where(asset =>
            (query.Length == 0 || asset.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
             asset.Subcategory.Contains(query, StringComparison.CurrentCultureIgnoreCase)) &&
            (source == "全部來源" || asset.Source.StartsWith(source, StringComparison.CurrentCulture)) &&
            (!favoritesCheckBox.Checked || asset.IsFavorite));

        _thumbnailGeneration++;
        _thumbnailCancellation?.Cancel();
        _thumbnailCancellation?.Dispose();
        _thumbnailCancellation = null;
        ClearInteractivePreview();
        assetThumbnailImageList.Images.Clear();
        var visibleItems = new List<ListViewItem>();
        _updatingItems = true;
        assetListView.BeginUpdate();
        try
        {
            assetListView.Items.Clear();
            foreach (var asset in filtered)
            {
                var item = new ListViewItem(string.Empty) { Tag = asset };
                item.SubItems.Add(asset.Name);
                item.SubItems.Add(asset.Subcategory);
                item.SubItems.Add(asset.Source);
                item.SubItems.Add(asset.DimensionsText);
                item.SubItems.Add(asset.QualityStatus);
                assetListView.Items.Add(item);
                visibleItems.Add(item);
            }
        }
        finally
        {
            assetListView.EndUpdate();
            _updatingItems = false;
        }

        resultCountLabel.Text = $"{assetListView.Items.Count} 個項目";
        ShowSelection(null);
        if (_thumbnailLoadingEnabled)
            StartThumbnailLoading(visibleItems);
    }

    private void StartThumbnailLoading(IEnumerable<ListViewItem> items)
    {
        if (!_thumbnailLoadingEnabled || _isDisposing || IsDisposed)
            return;
        var generation = ++_thumbnailGeneration;
        _thumbnailCancellation?.Cancel();
        _thumbnailCancellation?.Dispose();
        _thumbnailCancellation = new CancellationTokenSource();
        _ = LoadThumbnailsAsync(items.ToArray(), generation, _thumbnailCancellation.Token);
    }

    private void AssetListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_updatingItems)
            return;
        var asset = SelectedAsset;
        ShowSelection(asset);
        if (asset is not null)
            AssetSelected?.Invoke(this, asset);
    }

    private void AssetListView_DoubleClick(object? sender, EventArgs e) => RequestPlacement();

    private void AssetListView_ItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e.Item is ListViewItem { Tag: InteriorAssetDescriptor { CanPlace: true } asset } &&
            _placementSettings is not null)
        {
            var dragData = new InteriorAssetDragData(asset, _placementSettings.Copy());
            var dataObject = new DataObject();
            dataObject.SetData(InteriorAssetDragData.DataFormat, autoConvert: false, dragData);
            assetListView.DoDragDrop(dataObject, DragDropEffects.Copy);
        }
    }

    private void PlaceButton_Click(object? sender, EventArgs e) => RequestPlacement();

    private void EditAssetButton_Click(object? sender, EventArgs e)
    {
        if (SelectedAsset is { IsManagedLocalAsset: true } asset)
            EditRequested?.Invoke(this, asset);
    }

    private void DeleteAssetButton_Click(object? sender, EventArgs e)
    {
        if (SelectedAsset is { IsManagedLocalAsset: true } asset)
            DeleteRequested?.Invoke(this, asset);
    }

    private void RequestPlacement()
    {
        var asset = SelectedAsset;
        if (asset?.CanPlace == true && _placementSettings is not null)
            PlaceRequested?.Invoke(this, new InteriorAssetPlacementRequestEventArgs(asset, _placementSettings.Copy()));
        else if (asset is { Source: "內建目錄" })
            SearchOnlineRequested?.Invoke(this, asset);
    }

    internal InteriorAssetDescriptor? SelectedAsset =>
        assetListView.SelectedItems.Count == 1
            ? assetListView.SelectedItems[0].Tag as InteriorAssetDescriptor
            : null;

    private void ShowSelection(InteriorAssetDescriptor? asset)
    {
        if (asset is null)
        {
            detailLabel.Text = "選取項目以查看來源、授權與尺寸。";
            qualityLabel.Text = "品質：未選取";
            assetToolTip.SetToolTip(detailLabel, detailLabel.Text);
            assetToolTip.SetToolTip(qualityLabel, qualityLabel.Text);
            ClearInteractivePreview();
            _placementSettings = null;
            placementSettingsPropertyGrid.SelectedObject = null;
            editAssetButton.Enabled = false;
            deleteAssetButton.Enabled = false;
            placeButton.Enabled = false;
            placeButton.Text = "置放模型";
            return;
        }

        _placementSettings = InteriorAssetPlacementSettings.FromAsset(asset);
        placementSettingsPropertyGrid.SelectedObject = _placementSettings;
        detailLabel.Text = asset.CanPlace
            ? $"{asset.Subcategory}｜{asset.DimensionsText}｜{asset.License}"
            : $"{asset.Subcategory}｜{asset.Source}｜資產檔尚未下載或建立";
        qualityLabel.Text = $"品質：{asset.QualitySummary}";
        assetToolTip.SetToolTip(detailLabel, detailLabel.Text);
        assetToolTip.SetToolTip(qualityLabel, qualityLabel.Text);
        BeginInteractivePreview(asset);
        editAssetButton.Enabled = asset.IsManagedLocalAsset;
        deleteAssetButton.Enabled = asset.IsManagedLocalAsset;
        placeButton.Text = asset.CanPlace || asset.Source != "內建目錄" ? "置放模型" : "搜尋線上模型";
        placeButton.Enabled = asset.CanPlace || asset.Source == "內建目錄";
    }

    private void PlacementSettingsPropertyGrid_PropertyValueChanged(object? sender,
        System.Windows.Forms.PropertyValueChangedEventArgs e)
    {
        if (e.ChangedItem?.PropertyDescriptor?.Name == nameof(InteriorAssetPlacementSettings.Unit) &&
            _placementSettings is not null && SelectedAsset is { } asset)
            _placementSettings.ResetDimensions(asset);
        placementSettingsPropertyGrid.Refresh();
    }

    private void PlacementSplitContainer_SplitterMoved(object? sender, SplitterEventArgs e) =>
        PlacementSplitterMoved?.Invoke(this, EventArgs.Empty);

    private void PreviewSettingsSplitContainer_SplitterMoved(object? sender, SplitterEventArgs e) =>
        PreviewSplitterMoved?.Invoke(this, EventArgs.Empty);

    private void FramePreviewButton_Click(object? sender, EventArgs e) => FrameInteractivePreview();

    private void PbrPreviewButton_Click(object? sender, EventArgs e)
    {
        if (_previewModel is null || SelectedAsset is not { } asset)
            return;
        using var dialog = new InteriorAssetPbrPreviewForm(asset, _previewModel);
        dialog.ShowDialog(FindForm());
    }

    private async Task LoadThumbnailsAsync(IEnumerable<ListViewItem> items, int generation,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            if (cancellationToken.IsCancellationRequested || generation != _thumbnailGeneration ||
                _isDisposing || IsDisposed)
                return;
            if (item.Tag is not InteriorAssetDescriptor asset || !asset.CanPlace)
                continue;
            try
            {
                using var thumbnail = await InteriorAssetThumbnailService.GetAsync(asset, cancellationToken);
                if (thumbnail is null || cancellationToken.IsCancellationRequested ||
                    generation != _thumbnailGeneration || _isDisposing || IsDisposed ||
                    item.ListView != assetListView)
                    continue;
                var key = asset.ModelPath;
                using var listImage = new Bitmap(thumbnail);
                assetThumbnailImageList.Images.Add(key, listImage);
                item.ImageKey = key;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                               InvalidDataException or NotSupportedException or ArgumentException or
                                               System.Runtime.InteropServices.ExternalException)
            {
                // A broken asset remains manageable even when its thumbnail cannot be generated.
            }
        }
    }

    private void BeginInteractivePreview(InteriorAssetDescriptor asset)
    {
        ClearInteractivePreview();
        if (!asset.CanPlace)
            return;
        var generation = ++_previewGeneration;
        _previewCancellation = new CancellationTokenSource();
        _ = LoadInteractivePreviewAsync(asset, generation, _previewCancellation.Token);
    }

    private async Task LoadInteractivePreviewAsync(InteriorAssetDescriptor asset, int generation,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = await InteriorModelImportService.ImportAsync(asset.ModelPath, cancellationToken);
            if (cancellationToken.IsCancellationRequested || generation != _previewGeneration || IsDisposed ||
                SelectedAsset is not { } selected ||
                !string.Equals(selected.ModelPath, asset.ModelPath, StringComparison.OrdinalIgnoreCase))
                return;
            _previewModel = model;
            assetPreviewViewport.SetSceneModels([model]);
            framePreviewButton.Enabled = true;
            pbrPreviewButton.Enabled = true;
            FrameInteractivePreview();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                           InvalidDataException or NotSupportedException or ArgumentException)
        {
            qualityLabel.Text = $"品質：無法載入互動預覽｜{exception.Message}";
            assetToolTip.SetToolTip(qualityLabel, qualityLabel.Text);
        }
    }

    private void FrameInteractivePreview()
    {
        if (_previewModel is null || !SceneTraversal.TryCalculateBounds(_previewModel, out var bounds))
            return;
        var camera = assetPreviewViewport.Camera;
        var direction = Vector3.Normalize(new Vector3(1.35f, .9f, 1.55f));
        var radius = Math.Max(bounds.Radius, .05f);
        var distance = radius / MathF.Sin(camera.FieldOfViewDegrees * MathF.PI / 360f) * 1.25f;
        camera.To = bounds.Center;
        camera.From = bounds.Center + direction * distance;
        camera.Up = Vector3.UnitY;
        camera.RollDegrees = 0f;
        camera.NearPlane = Math.Max(.0001f, distance - radius * 2f);
        camera.FarPlane = Math.Max(camera.NearPlane + 1f, distance + radius * 3f);
        assetPreviewViewport.Invalidate();
    }

    private void ClearInteractivePreview()
    {
        _previewCancellation?.Cancel();
        _previewCancellation?.Dispose();
        _previewCancellation = null;
        _previewModel = null;
        if (assetPreviewViewport is not null)
            assetPreviewViewport.SetSceneModels([]);
        if (framePreviewButton is not null)
            framePreviewButton.Enabled = false;
        if (pbrPreviewButton is not null)
            pbrPreviewButton.Enabled = false;
    }

    private void ReleasePreviewImage() => ClearInteractivePreview();

    internal void PrepareForDispose()
    {
        if (_isDisposing) return;
        _isDisposing = true;
        _thumbnailGeneration++;
        _thumbnailCancellation?.Cancel();
        _thumbnailCancellation?.Dispose();
        _thumbnailCancellation = null;
        ReleasePreviewImage();
    }
}
