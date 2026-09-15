namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Diagnostics;
using System.Text.Json;

internal sealed class OnlineModelDownloadRequestedEventArgs(
    OnlineModelAsset asset, OnlineModelDownloadOption option) : EventArgs
{
    internal OnlineModelAsset Asset { get; } = asset;
    internal OnlineModelDownloadOption Option { get; } = option;
}

internal sealed partial class PolyHavenAssetBrowserControl : UserControl
{
    private readonly Dictionary<string, IReadOnlyList<OnlineModelAsset>> _providerCache = [];
    private IReadOnlyList<OnlineModelAsset> _assets = [];
    private string? _loadedProviderId;
    private int _loadGeneration;
    private int _thumbnailGeneration;
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _thumbnailCancellation;
    private bool _isDisposing;
    private CancellationTokenSource? _selectionCancellation;

    public PolyHavenAssetBrowserControl()
    {
        InitializeComponent();
        if (providerComboBox.SelectedIndex < 0 && providerComboBox.Items.Count > 0)
            providerComboBox.SelectedIndex = 0;
        UpdateProviderPresentation();
    }

    internal event EventHandler<OnlineModelDownloadRequestedEventArgs>? DownloadRequested;
    internal event EventHandler? PreviewSplitterMoved;

    internal int PreviewPanelHeight => onlinePreviewSplitContainer.Panel2.Height;

    internal int SelectedProviderIndex
    {
        get => Math.Max(0, providerComboBox.SelectedIndex);
        set
        {
            if (providerComboBox.Items.Count > 0)
                providerComboBox.SelectedIndex = Math.Clamp(value, 0, providerComboBox.Items.Count - 1);
        }
    }

    private IOnlineModelProvider SelectedProvider =>
        OnlineModelProviderRegistry.All[Math.Clamp(providerComboBox.SelectedIndex, 0,
            OnlineModelProviderRegistry.All.Count - 1)];

    internal void SetPreviewPanelHeight(int height)
    {
        var available = onlinePreviewSplitContainer.ClientSize.Height;
        if (available <= onlinePreviewSplitContainer.Panel1MinSize +
            onlinePreviewSplitContainer.Panel2MinSize + onlinePreviewSplitContainer.SplitterWidth)
            return;
        var panelHeight = Math.Clamp(height, onlinePreviewSplitContainer.Panel2MinSize,
            available - onlinePreviewSplitContainer.Panel1MinSize - onlinePreviewSplitContainer.SplitterWidth);
        onlinePreviewSplitContainer.SplitterDistance = available -
            onlinePreviewSplitContainer.SplitterWidth - panelHeight;
    }

    internal async Task EnsureLoadedAsync(bool force = false)
    {
        var provider = SelectedProvider;
        if (!force && _loadedProviderId == provider.Id) return;
        var generation = ++_loadGeneration;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var token = _loadCancellation.Token;
        CancelAssetRequests();
        try
        {
            SetBrowserBusy(true, $"正在載入 {provider.DisplayName} CC0 模型…");
            if (force || !_providerCache.TryGetValue(provider.Id, out var assets))
            {
                assets = await provider.GetModelsAsync(token);
                _providerCache[provider.Id] = assets;
            }
            if (token.IsCancellationRequested || generation != _loadGeneration) return;
            _assets = assets;
            _loadedProviderId = provider.Id;
            PopulateCategories();
            ApplyFilter();
            browserStatusLabel.Text = $"{provider.DisplayName}：{_assets.Count} 個 CC0 模型";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or
                                           JsonException or InvalidDataException)
        {
            if (generation == _loadGeneration)
                browserStatusLabel.Text = $"無法載入 {provider.DisplayName} 模型：{exception.Message}";
        }
        finally
        {
            if (generation == _loadGeneration)
                SetBrowserBusy(false, browserStatusLabel.Text);
        }
    }

    internal void SetDownloadBusy(bool busy, string message)
    {
        downloadButton.Enabled = !busy && SelectedAsset is not null &&
                                 resolutionComboBox.SelectedItem is OnlineModelDownloadOption;
        refreshButton.Enabled = !busy;
        providerComboBox.Enabled = !busy;
        browserStatusLabel.Text = message;
        UseWaitCursor = busy;
    }

    internal void SetSearchQuery(string query)
    {
        var unchanged = categoryComboBox.SelectedItem?.ToString() == "全部分類" &&
                        string.Equals(searchTextBox.Text, query, StringComparison.Ordinal);
        categoryComboBox.SelectedItem = "全部分類";
        searchTextBox.Text = query;
        if (_loadedProviderId == SelectedProvider.Id && unchanged)
            ApplyFilter();
        searchTextBox.Focus();
    }

    private void SearchControl_Changed(object? sender, EventArgs e)
    {
        if (_loadedProviderId == SelectedProvider.Id) ApplyFilter();
    }

    private async void ProviderComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateProviderPresentation();
        _loadedProviderId = null;
        _assets = [];
        CancelAssetRequests();
        onlineAssetsListView.Items.Clear();
        categoryComboBox.Items.Clear();
        categoryComboBox.Items.Add("全部分類");
        categoryComboBox.SelectedIndex = 0;
        if (Visible && IsHandleCreated && !_isDisposing)
            await EnsureLoadedAsync();
    }

    private void OnlinePreviewSplitContainer_SplitterMoved(object? sender, SplitterEventArgs e) =>
        PreviewSplitterMoved?.Invoke(this, EventArgs.Empty);

    private async void RefreshButton_Click(object? sender, EventArgs e) => await EnsureLoadedAsync(force: true);

    private void ApplyFilter()
    {
        var query = searchTextBox.Text.Trim();
        var category = categoryComboBox.SelectedItem?.ToString() ?? "全部分類";
        var filtered = _assets.Where(asset =>
                (query.Length == 0 || asset.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                 asset.Tags.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                 asset.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase)) &&
                (category == "全部分類" || asset.Category.StartsWith(category,
                    StringComparison.OrdinalIgnoreCase)))
            .Take(200)
            .ToArray();
        var generation = ++_thumbnailGeneration;
        _thumbnailCancellation?.Cancel();
        _thumbnailCancellation?.Dispose();
        _thumbnailCancellation = new CancellationTokenSource();
        SetPreviewImage(null);
        onlineThumbnailImageList.Images.Clear();
        onlineAssetsListView.BeginUpdate();
        onlineAssetsListView.Items.Clear();
        var items = new List<ListViewItem>();
        foreach (var asset in filtered)
        {
            var item = new ListViewItem(string.Empty) { Tag = asset };
            item.SubItems.Add(asset.Name);
            item.SubItems.Add(asset.Category.Split('/').LastOrDefault() ?? asset.Category);
            item.SubItems.Add(asset.Polycount > 0 ? $"{asset.Polycount:N0}" : "-");
            onlineAssetsListView.Items.Add(item);
            items.Add(item);
        }
        onlineAssetsListView.EndUpdate();
        browserStatusLabel.Text = filtered.Length == 200
            ? "顯示前 200 個結果，可輸入關鍵字縮小範圍。"
            : $"找到 {filtered.Length} 個 CC0 模型。";
        ShowSelection(null);
        _ = LoadThumbnailsAsync(items, generation, _thumbnailCancellation.Token);
    }

    private void PopulateCategories()
    {
        var selected = categoryComboBox.SelectedItem?.ToString();
        categoryComboBox.BeginUpdate();
        categoryComboBox.Items.Clear();
        categoryComboBox.Items.Add("全部分類");
        foreach (var category in _assets.Select(asset => asset.Category.Split('/')[0])
                     .Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(value => value))
            categoryComboBox.Items.Add(category);
        categoryComboBox.SelectedItem = selected is not null && categoryComboBox.Items.Contains(selected)
            ? selected
            : "全部分類";
        categoryComboBox.EndUpdate();
    }

    private async void OnlineAssetsListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var asset = SelectedAsset;
        ShowSelection(asset);
        _selectionCancellation?.Cancel();
        _selectionCancellation?.Dispose();
        _selectionCancellation = null;
        if (asset is null) return;
        var provider = OnlineModelProviderRegistry.Get(asset.ProviderId);
        _selectionCancellation = new CancellationTokenSource();
        var token = _selectionCancellation.Token;
        try
        {
            resolutionComboBox.Enabled = false;
            downloadButton.Enabled = false;
            var optionsTask = provider.GetDownloadOptionsAsync(asset, token);
            var previewTask = provider.GetThumbnailAsync(asset.ThumbnailUrl, token);
            var options = await optionsTask;
            using var preview = await previewTask;
            if (token.IsCancellationRequested || SelectedAsset is not { } selected ||
                selected.ProviderId != asset.ProviderId || selected.Id != asset.Id) return;
            if (preview is not null) SetPreviewImage(new Bitmap(preview));
            resolutionComboBox.Items.Clear();
            resolutionComboBox.Items.AddRange(options.Cast<object>().ToArray());
            resolutionComboBox.SelectedItem = options.FirstOrDefault(option =>
                                                  option.Label.Contains("2K", StringComparison.OrdinalIgnoreCase)) ??
                                              options.FirstOrDefault();
            resolutionComboBox.Enabled = options.Count > 0;
            downloadButton.Enabled = options.Count > 0;
            if (options.Count == 0) browserStatusLabel.Text = "此模型沒有可用的模型檔案。";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidDataException)
        {
            browserStatusLabel.Text = $"無法載入下載選項：{exception.Message}";
        }
    }

    private void ShowSelection(OnlineModelAsset? asset)
    {
        resolutionComboBox.Items.Clear();
        resolutionComboBox.Enabled = false;
        downloadButton.Enabled = false;
        if (asset is null)
        {
            onlineNameLabel.Text = "選取線上模型";
            onlineInfoLabel.Text = $"{SelectedProvider.DisplayName} 模型為 {SelectedProvider.LicenseSummary}。";
            SetPreviewImage(null);
            return;
        }
        onlineNameLabel.Text = asset.Name;
        var dimensions = asset.WidthCentimeters > 0
            ? $"{asset.WidthCentimeters:0.#}×{asset.DepthCentimeters:0.#}×{asset.HeightCentimeters:0.#} cm"
            : "尺寸未提供";
        var faces = asset.Polycount > 0 ? $"｜{asset.Polycount:N0} 面" : string.Empty;
        onlineInfoLabel.Text = $"{asset.ProviderName}｜{asset.Category}\r\n{dimensions}{faces}\r\n作者：{asset.Authors}";
        var key = GetImageKey(asset);
        var cachedPreview = onlineThumbnailImageList.Images.ContainsKey(key)
            ? onlineThumbnailImageList.Images[key]
            : null;
        SetPreviewImage(cachedPreview is null ? null : new Bitmap(cachedPreview));
    }

    private void ResolutionComboBox_SelectedIndexChanged(object? sender, EventArgs e) =>
        downloadButton.Enabled = SelectedAsset is not null &&
                                 resolutionComboBox.SelectedItem is OnlineModelDownloadOption;

    private void DownloadButton_Click(object? sender, EventArgs e)
    {
        if (SelectedAsset is { } asset && resolutionComboBox.SelectedItem is OnlineModelDownloadOption option)
            DownloadRequested?.Invoke(this, new OnlineModelDownloadRequestedEventArgs(asset, option));
    }

    private void AttributionLinkLabel_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) =>
        Process.Start(new ProcessStartInfo(SelectedProvider.AttributionUrl) { UseShellExecute = true });

    private OnlineModelAsset? SelectedAsset => onlineAssetsListView.SelectedItems.Count == 1
        ? onlineAssetsListView.SelectedItems[0].Tag as OnlineModelAsset
        : null;

    private async Task LoadThumbnailsAsync(IEnumerable<ListViewItem> items, int generation,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            if (cancellationToken.IsCancellationRequested || generation != _thumbnailGeneration ||
                _isDisposing || IsDisposed || item.Tag is not OnlineModelAsset asset)
                return;
            try
            {
                var provider = OnlineModelProviderRegistry.Get(asset.ProviderId);
                using var thumbnail = await provider.GetThumbnailAsync(asset.ThumbnailUrl, cancellationToken);
                if (thumbnail is null || cancellationToken.IsCancellationRequested ||
                    generation != _thumbnailGeneration || _isDisposing || IsDisposed ||
                    item.ListView != onlineAssetsListView) continue;
                using var listImage = new Bitmap(thumbnail);
                var key = GetImageKey(asset);
                onlineThumbnailImageList.Images.Add(key, listImage);
                item.ImageKey = key;
                if (SelectedAsset is { } selected && selected.ProviderId == asset.ProviderId &&
                    selected.Id == asset.Id)
                    SetPreviewImage(new Bitmap(thumbnail));
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or
                                               ArgumentException or System.Runtime.InteropServices.ExternalException)
            {
                // Keep the metadata row usable when a remote thumbnail fails.
            }
        }
    }

    private void UpdateProviderPresentation()
    {
        var provider = SelectedProvider;
        searchTextBox.PlaceholderText = $"搜尋 {provider.DisplayName} 模型";
        attributionLinkLabel.Text = provider.AttributionText;
        if (_loadedProviderId != provider.Id)
            browserStatusLabel.Text = $"切換至 {provider.DisplayName}，等待載入。";
    }

    private void CancelAssetRequests()
    {
        _thumbnailGeneration++;
        _thumbnailCancellation?.Cancel();
        _thumbnailCancellation?.Dispose();
        _thumbnailCancellation = null;
        _selectionCancellation?.Cancel();
        _selectionCancellation?.Dispose();
        _selectionCancellation = null;
        ShowSelection(null);
    }

    private static string GetImageKey(OnlineModelAsset asset) => $"{asset.ProviderId}|{asset.Id}";

    private void SetPreviewImage(Image? image)
    {
        if (onlinePreviewPictureBox is null)
        {
            image?.Dispose();
            return;
        }
        var previous = onlinePreviewPictureBox.Image;
        onlinePreviewPictureBox.Image = image;
        if (previous is not null && !ReferenceEquals(previous, image))
            previous.Dispose();
    }

    private void ReleasePreviewImage() => SetPreviewImage(null);

    internal void PrepareForDispose()
    {
        if (_isDisposing) return;
        _isDisposing = true;
        _loadGeneration++;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = null;
        CancelAssetRequests();
        ReleasePreviewImage();
    }

    private void SetBrowserBusy(bool busy, string message)
    {
        refreshButton.Enabled = !busy;
        providerComboBox.Enabled = !busy;
        searchTextBox.Enabled = !busy;
        categoryComboBox.Enabled = !busy;
        browserStatusLabel.Text = message;
        UseWaitCursor = busy;
    }
}
