namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using Assimp;

internal sealed record OnlineModelAsset(
    string ProviderId,
    string ProviderName,
    string Id,
    string Name,
    string Description,
    string Category,
    string ThumbnailUrl,
    string Authors,
    string License,
    string SourceUrl,
    string Tags,
    decimal WidthCentimeters,
    decimal DepthCentimeters,
    decimal HeightCentimeters,
    long Polycount,
    long DownloadCount,
    object NativeAsset);

internal sealed record OnlineModelDownloadOption(
    string ProviderId, string Label, long TotalSize, object NativeOption)
{
    public override string ToString() => $"{Label}（{TotalSize / 1024d / 1024d:0.##} MB）";
}

internal interface IOnlineModelDownloadedPackage : IDisposable
{
    string ModelPath { get; }
}

internal interface IOnlineModelProvider
{
    string Id { get; }
    string DisplayName { get; }
    string AttributionText { get; }
    string AttributionUrl { get; }
    string LicenseSummary { get; }

    Task<IReadOnlyList<OnlineModelAsset>> GetModelsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OnlineModelDownloadOption>> GetDownloadOptionsAsync(
        OnlineModelAsset asset, CancellationToken cancellationToken = default);
    Task<Bitmap?> GetThumbnailAsync(string url, CancellationToken cancellationToken = default);
    Task<IOnlineModelDownloadedPackage> DownloadAsync(OnlineModelAsset asset,
        OnlineModelDownloadOption option, IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}

internal static class OnlineModelProviderRegistry
{
    internal static IReadOnlyList<IOnlineModelProvider> All { get; } =
    [
        new PolyHavenOnlineModelProvider(),
        new AmbientCgOnlineModelProvider()
    ];

    internal static IOnlineModelProvider Get(string providerId) =>
        All.First(provider => string.Equals(provider.Id, providerId, StringComparison.Ordinal));
}

internal sealed class PolyHavenOnlineModelProvider : IOnlineModelProvider
{
    public string Id => "polyhaven";
    public string DisplayName => "Poly Haven";
    public string AttributionText => "CC0 資產來源：Poly Haven";
    public string AttributionUrl => "https://polyhaven.com";
    public string LicenseSummary => "CC0，可商用";

    public async Task<IReadOnlyList<OnlineModelAsset>> GetModelsAsync(
        CancellationToken cancellationToken = default) =>
        (await PolyHavenClient.GetModelsAsync(cancellationToken)).Select(asset => new OnlineModelAsset(
            Id, DisplayName, asset.Id, asset.Name, asset.Description, asset.Category, asset.ThumbnailUrl,
            asset.Authors, "CC0", PolyHavenClient.GetAssetPageUrl(asset.Id), asset.Tags,
            asset.WidthCentimeters, asset.DepthCentimeters, asset.HeightCentimeters, asset.Polycount,
            asset.DownloadCount, asset)).ToArray();

    public async Task<IReadOnlyList<OnlineModelDownloadOption>> GetDownloadOptionsAsync(
        OnlineModelAsset asset, CancellationToken cancellationToken = default) =>
        (await PolyHavenClient.GetDownloadOptionsAsync(asset.Id, cancellationToken)).Select(option =>
            new OnlineModelDownloadOption(Id, $"{option.Resolution} GLTF", option.TotalSize, option)).ToArray();

    public Task<Bitmap?> GetThumbnailAsync(string url, CancellationToken cancellationToken = default) =>
        PolyHavenClient.GetThumbnailAsync(url, cancellationToken);

    public async Task<IOnlineModelDownloadedPackage> DownloadAsync(OnlineModelAsset asset,
        OnlineModelDownloadOption option, IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (asset.NativeAsset is not PolyHavenAsset nativeAsset ||
            option.NativeOption is not PolyHavenDownloadOption nativeOption)
            throw new InvalidDataException("Poly Haven 下載資料格式無效。");
        return await PolyHavenClient.DownloadAsync(nativeAsset, nativeOption, progress, cancellationToken);
    }
}

internal sealed record AmbientCgDownloadData(string Attributes, string Url, long Size);

internal sealed class AmbientCgOnlineModelProvider : IOnlineModelProvider
{
    private const string ApiUrl = "https://ambientcg.com/api/v3/assets";
    private static readonly HttpClient HttpClient = CreateClient();

    public string Id => "ambientcg";
    public string DisplayName => "ambientCG";
    public string AttributionText => "CC0 資產來源：ambientCG";
    public string AttributionUrl => "https://ambientcg.com";
    public string LicenseSummary => "CC0，可商用";

    public async Task<IReadOnlyList<OnlineModelAsset>> GetModelsAsync(
        CancellationToken cancellationToken = default)
    {
        var uri = $"{ApiUrl}?type=3d-model&sort=popular&limit=500&include=" +
                  "type,title,url,tags,dimensions,downloadStatistics,downloads,thumbnails";
        using var response = await HttpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("assets", out var assets) ||
            assets.ValueKind != JsonValueKind.Array)
            return [];
        return assets.EnumerateArray().Select(ParseAsset)
            .OrderByDescending(asset => asset.DownloadCount).ToArray();
    }

    public async Task<IReadOnlyList<OnlineModelDownloadOption>> GetDownloadOptionsAsync(
        OnlineModelAsset asset, CancellationToken cancellationToken = default)
    {
        var uri = $"{ApiUrl}?id={Uri.EscapeDataString(asset.Id)}&limit=1&include=downloads";
        using var response = await HttpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("assets", out var assets) ||
            assets.ValueKind != JsonValueKind.Array || assets.GetArrayLength() == 0 ||
            !assets[0].TryGetProperty("downloads", out var downloads) ||
            downloads.ValueKind != JsonValueKind.Array)
            return [];

        return downloads.EnumerateArray()
            .Select(ParseDownload)
            .Where(option => option is not null)
            .Cast<OnlineModelDownloadOption>()
            .OrderBy(option => DownloadOrder(option.Label))
            .ToArray();
    }

    public Task<Bitmap?> GetThumbnailAsync(string url, CancellationToken cancellationToken = default) =>
        PolyHavenClient.GetThumbnailAsync(url, cancellationToken);

    public async Task<IOnlineModelDownloadedPackage> DownloadAsync(OnlineModelAsset asset,
        OnlineModelDownloadOption option, IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (option.NativeOption is not AmbientCgDownloadData download)
            throw new InvalidDataException("ambientCG 下載資料格式無效。");
        var root = Path.Combine(Path.GetTempPath(), "RvInteriorDesign", "AmbientCG");
        Directory.CreateDirectory(root);
        var directory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var archivePath = Path.Combine(directory, "asset.zip");
            using var response = await HttpClient.GetAsync(download.Url, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength ?? download.Size;
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var target = File.Create(archivePath))
                await CopyToAsync(source, target, totalBytes, progress, cancellationToken);
            progress?.Report(86);
            ExtractArchive(archivePath, directory, cancellationToken);
            var modelPath = PrepareSupportedModel(directory, cancellationToken);
            progress?.Report(100);
            return new AmbientCgDownloadedPackage(directory, modelPath);
        }
        catch
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            throw;
        }
    }

    private OnlineModelAsset ParseAsset(JsonElement asset)
    {
        var id = GetString(asset, "id", string.Empty);
        var tags = asset.TryGetProperty("tags", out var tagElement) && tagElement.ValueKind == JsonValueKind.Array
            ? tagElement.EnumerateArray().Select(tag => tag.GetString() ?? string.Empty)
                .Where(tag => tag.Length > 0).ToArray()
            : [];
        var category = tags.FirstOrDefault(tag => tag != "3d" && !int.TryParse(tag, out _)) ?? "3D Model";
        var thumbnail = asset.TryGetProperty("thumbnails", out var thumbnails)
            ? GetString(thumbnails, "256-JPG-FFFFFF", GetString(thumbnails, "256-PNG", string.Empty))
            : string.Empty;
        decimal width = 0, depth = 0, height = 0;
        if (asset.TryGetProperty("dimensions", out var dimensions))
        {
            width = GetDecimal(dimensions, "width") * 100m;
            depth = GetDecimal(dimensions, "depth") * 100m;
            height = GetDecimal(dimensions, "height") * 100m;
        }
        var downloads = asset.TryGetProperty("downloadStatistics", out var statistics)
            ? GetInt64(statistics, "total")
            : 0;
        return new OnlineModelAsset(Id, DisplayName, id, GetString(asset, "title", id), string.Empty,
            category, thumbnail, "ambientCG", "CC0", GetString(asset, "url", $"https://ambientcg.com/a/{id}"),
            string.Join(' ', tags), width, depth, height, 0, downloads, id);
    }

    private OnlineModelDownloadOption? ParseDownload(JsonElement element)
    {
        var attributes = GetString(element, "attributes", string.Empty);
        var extension = GetString(element, "extension", string.Empty);
        var url = GetString(element, "url", string.Empty);
        var size = GetInt64(element, "size");
        if (!extension.Equals("zip", StringComparison.OrdinalIgnoreCase) ||
            !attributes.EndsWith("-JPG", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(url))
            return null;
        var data = new AmbientCgDownloadData(attributes, url, size);
        return new OnlineModelDownloadOption(Id, $"{attributes} ZIP", size, data);
    }

    private static int DownloadOrder(string label)
    {
        var quality = label.StartsWith("LQ", StringComparison.OrdinalIgnoreCase) ? 0 :
            label.StartsWith("SQ", StringComparison.OrdinalIgnoreCase) ? 10 : 20;
        var resolution = label.Contains("1K", StringComparison.OrdinalIgnoreCase) ? 0 :
            label.Contains("2K", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
        return quality + resolution;
    }

    private static async Task CopyToAsync(Stream source, Stream target, long totalBytes,
        IProgress<int>? progress, CancellationToken cancellationToken)
    {
        var buffer = new byte[128 * 1024];
        long copied = 0;
        progress?.Report(0);
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            copied += read;
            if (totalBytes > 0)
                progress?.Report((int)Math.Clamp(copied * 85L / totalBytes, 0L, 85L));
        }
    }

    private static void ExtractArchive(string archivePath, string directory, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        if (archive.Entries.Count > 10_000 || archive.Entries.Sum(entry => entry.Length) > 2L * 1024 * 1024 * 1024)
            throw new InvalidDataException("ambientCG 壓縮檔內容超過安全限制。");
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Name)) continue;
            var destination = ResolveSafePath(directory, entry.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, overwrite: true);
        }
    }

    private static string FindModelPath(string directory)
    {
        var extensions = new[] { ".glb", ".gltf", ".obj", ".fbx", ".dae" };
        foreach (var extension in extensions)
        {
            var path = Directory.EnumerateFiles(directory, $"*{extension}", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (path is not null) return path;
        }
        throw new InvalidDataException("ambientCG 壓縮檔中沒有支援的模型檔案。");
    }

    private static string PrepareSupportedModel(string directory, CancellationToken cancellationToken)
    {
        var sourcePath = FindModelPath(directory);
        var extension = Path.GetExtension(sourcePath);
        if (extension.Equals(".glb", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".gltf", StringComparison.OrdinalIgnoreCase))
            return sourcePath;

        cancellationToken.ThrowIfCancellationRequested();
        using var context = new AssimpContext();
        var scene = context.ImportFile(sourcePath,
                        PostProcessSteps.Triangulate |
                        PostProcessSteps.JoinIdenticalVertices |
                        PostProcessSteps.GenerateSmoothNormals |
                        PostProcessSteps.CalculateTangentSpace)
                    ?? throw new InvalidDataException("ambientCG 模型無法轉換為 GLTF。");
        cancellationToken.ThrowIfCancellationRequested();
        var destination = Path.Combine(directory, "model.gltf");
        if (!context.ExportFile(scene, destination, "gltf2"))
            throw new InvalidDataException("ambientCG 模型轉換為 GLTF 失敗。");
        return destination;
    }

    private static string ResolveSafePath(string root, string relativePath)
    {
        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("ambientCG 壓縮檔包含不安全的路徑。");
        return fullPath;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RVInteriorDesignPlugin", "1.0"));
        return client;
    }

    private static string GetString(JsonElement element, string name, string fallback) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static long GetInt64(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt64(out var result) ? result : 0L;

    private static decimal GetDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetDecimal(out var result) ? result : 0m;
}

internal sealed class AmbientCgDownloadedPackage(string directoryPath, string modelPath) :
    IOnlineModelDownloadedPackage
{
    public string ModelPath { get; } = modelPath;

    public void Dispose()
    {
        var fullDirectory = Path.GetFullPath(directoryPath);
        var safeRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "RvInteriorDesign", "AmbientCG")) +
                       Path.DirectorySeparatorChar;
        if (!fullDirectory.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase) ||
            !Directory.Exists(fullDirectory)) return;
        try
        {
            Directory.Delete(fullDirectory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The operating system can clean a temporary package whose decoder still owns a short-lived handle.
        }
    }
}
