namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

internal sealed record PolyHavenAsset(
    string Id,
    string Name,
    string Description,
    string Category,
    string ThumbnailUrl,
    string Authors,
    string Tags,
    decimal WidthCentimeters,
    decimal DepthCentimeters,
    decimal HeightCentimeters,
    long Polycount,
    long DownloadCount);

internal sealed record PolyHavenDownloadOption(
    string Resolution, string Url, string Md5, long Size, IReadOnlyList<PolyHavenFile> Includes)
{
    internal long TotalSize => Size + Includes.Sum(file => file.Size);
    public override string ToString() => $"{Resolution} GLTF（{TotalSize / 1024d / 1024d:0.##} MB）";
}

internal sealed record PolyHavenFile(string RelativePath, string Url, string Md5, long Size);

internal sealed class PolyHavenDownloadedPackage(string directoryPath, string modelPath) :
    IOnlineModelDownloadedPackage
{
    public string ModelPath { get; } = modelPath;

    public void Dispose()
    {
        var fullDirectory = Path.GetFullPath(directoryPath);
        var safeRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "RvInteriorDesign", "PolyHaven")) +
                       Path.DirectorySeparatorChar;
        if (fullDirectory.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(fullDirectory))
            try
            {
                Directory.Delete(fullDirectory, recursive: true);
            }
            catch (IOException)
            {
                // A texture decoder may release its file handle slightly later; the OS temp folder can clean it up.
            }
            catch (UnauthorizedAccessException)
            {
                // Cleanup failure must not invalidate an asset that was already copied into the managed library.
            }
    }
}

internal static class PolyHavenClient
{
    private const string BaseUrl = "https://api.polyhaven.com";
    private static readonly HttpClient HttpClient = CreateClient();

    internal static async Task<IReadOnlyList<PolyHavenAsset>> GetModelsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync($"{BaseUrl}/assets?type=models", cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var result = new List<PolyHavenAsset>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var value = property.Value;
            var dimensions = ReadNumberArray(value, "dimensions");
            result.Add(new PolyHavenAsset(
                property.Name,
                GetString(value, "name", property.Name),
                GetString(value, "description", string.Empty),
                GetString(value, "category", "Other"),
                GetString(value, "thumbnail_url", string.Empty),
                value.TryGetProperty("authors", out var authors)
                    ? string.Join("、", authors.EnumerateObject().Select(author => author.Name))
                    : "Poly Haven",
                value.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array
                    ? string.Join(" ", tags.EnumerateArray().Select(tag => tag.GetString()))
                    : string.Empty,
                dimensions.Length > 0 ? Math.Round((decimal)dimensions[0] / 10m, 1) : 0m,
                dimensions.Length > 1 ? Math.Round((decimal)dimensions[1] / 10m, 1) : 0m,
                dimensions.Length > 2 ? Math.Round((decimal)dimensions[2] / 10m, 1) : 0m,
                GetInt64(value, "polycount"),
                GetInt64(value, "download_count")));
        }
        return result.OrderByDescending(asset => asset.DownloadCount).ToArray();
    }

    internal static async Task<IReadOnlyList<PolyHavenDownloadOption>> GetDownloadOptionsAsync(
        string assetId, CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync($"{BaseUrl}/files/{Uri.EscapeDataString(assetId)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("gltf", out var gltf))
            return [];
        var options = new List<PolyHavenDownloadOption>();
        foreach (var resolution in gltf.EnumerateObject())
        {
            if (!resolution.Value.TryGetProperty("gltf", out var file))
                continue;
            var includes = new List<PolyHavenFile>();
            if (file.TryGetProperty("include", out var includeElement) &&
                includeElement.ValueKind == JsonValueKind.Object)
                foreach (var include in includeElement.EnumerateObject())
                    includes.Add(ReadFile(include.Value, include.Name));
            options.Add(new PolyHavenDownloadOption(resolution.Name,
                GetString(file, "url", string.Empty), GetString(file, "md5", string.Empty),
                GetInt64(file, "size"), includes));
        }
        var order = new[] { "1k", "2k", "4k", "8k" };
        return options.OrderBy(option => Array.IndexOf(order, option.Resolution) is var index && index >= 0
                ? index
                : int.MaxValue)
            .ToArray();
    }

    internal static async Task<Bitmap?> GetThumbnailAsync(string url,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var bytes = await HttpClient.GetByteArrayAsync(url, cancellationToken);
        using var stream = new MemoryStream(bytes);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    internal static async Task<PolyHavenDownloadedPackage> DownloadAsync(PolyHavenAsset asset,
        PolyHavenDownloadOption option, IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var root = Path.Combine(Path.GetTempPath(), "RvInteriorDesign", "PolyHaven");
        Directory.CreateDirectory(root);
        var directory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var files = new List<PolyHavenFile>
            {
                new(Path.GetFileName(new Uri(option.Url).LocalPath), option.Url, option.Md5, option.Size)
            };
            files.AddRange(option.Includes);
            var totalBytes = files.Sum(file => Math.Max(0L, file.Size));
            long completedBytes = 0;
            progress?.Report(0);
            for (var index = 0; index < files.Count; index++)
            {
                var file = files[index];
                var destination = ResolveSafePath(directory, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                using var response = await HttpClient.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                response.EnsureSuccessStatusCode();
                await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var target = File.Create(destination))
                    await CopyToAsync(source, target, completedBytes, totalBytes, index, files.Count,
                        progress, cancellationToken);
                completedBytes += Math.Max(0L, file.Size);
                VerifyMd5(destination, file.Md5);
                progress?.Report(totalBytes > 0
                    ? (int)Math.Clamp(completedBytes * 100L / totalBytes, 0L, 100L)
                    : (index + 1) * 100 / files.Count);
            }
            var modelPath = ResolveSafePath(directory, files[0].RelativePath);
            return new PolyHavenDownloadedPackage(directory, modelPath);
        }
        catch
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            throw;
        }
    }

    internal static string GetAssetPageUrl(string assetId) =>
        $"https://polyhaven.com/a/{Uri.EscapeDataString(assetId)}";

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RVInteriorDesignPlugin", "1.0"));
        return client;
    }

    private static PolyHavenFile ReadFile(JsonElement element, string relativePath) => new(relativePath,
        GetString(element, "url", string.Empty), GetString(element, "md5", string.Empty),
        GetInt64(element, "size"));

    private static string ResolveSafePath(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException("線上資產包含無效檔案路徑。");
        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("線上資產嘗試寫入下載目錄之外。");
        return fullPath;
    }

    private static void VerifyMd5(string path, string expected)
    {
        if (string.IsNullOrWhiteSpace(expected)) return;
        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(MD5.HashData(stream));
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"下載檔案驗證失敗：{Path.GetFileName(path)}");
    }

    private static async Task CopyToAsync(Stream source, Stream target, long completedBytes,
        long totalBytes, int fileIndex, int fileCount, IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[128 * 1024];
        long currentBytes = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            currentBytes += read;
            progress?.Report(totalBytes > 0
                ? (int)Math.Clamp((completedBytes + currentBytes) * 100L / totalBytes, 0L, 99L)
                : Math.Min(99, fileIndex * 100 / Math.Max(1, fileCount)));
        }
    }

    private static string GetString(JsonElement element, string name, string fallback) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static long GetInt64(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt64(out var result) ? result : 0L;

    private static double[] ReadNumberArray(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(item => item.TryGetDouble(out _)).Select(item => item.GetDouble()).ToArray()
            : [];
}
