using Rv3dViewer.Plugin.Abstractions;
using System.Text.Json;

namespace Rv3dViewer.App.Plugins;

public static class PluginCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static PluginDiscoveryResult Discover(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        Directory.CreateDirectory(rootDirectory);

        var descriptors = new List<PluginDescriptor>();
        var issues = new List<PluginLoadIssue>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in Directory.EnumerateDirectories(rootDirectory).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var manifestPath = Path.Combine(directory, "plugin.json");
            if (!File.Exists(manifestPath)) continue;

            try
            {
                var manifest = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath), JsonOptions)
                    ?? throw new InvalidDataException("plugin.json 沒有內容。");
                Validate(manifest);
                if (!manifest.Enabled) continue;
                if (!ids.Add(manifest.Id))
                    throw new InvalidDataException($"外掛 ID '{manifest.Id}' 重複。");

                var directoryPath = Path.GetFullPath(directory);
                var assemblyPath = Path.GetFullPath(Path.Combine(directoryPath, manifest.Assembly));
                var directoryPrefix = directoryPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!assemblyPath.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("assembly 必須位於外掛自己的資料夾內。");
                if (!File.Exists(assemblyPath))
                    throw new FileNotFoundException($"找不到外掛 DLL：{manifest.Assembly}", assemblyPath);

                descriptors.Add(new PluginDescriptor(manifest, directoryPath, assemblyPath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
            {
                issues.Add(new PluginLoadIssue(manifestPath, ex.Message));
            }
        }

        return new PluginDiscoveryResult(
            descriptors.OrderBy(item => item.Manifest.MenuOrder).ThenBy(item => item.Manifest.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            issues);
    }

    private static void Validate(PluginManifest manifest)
    {
        Require(manifest.Id, "id");
        Require(manifest.Name, "name");
        Require(manifest.Version, "version");
        Require(manifest.Assembly, "assembly");
        Require(manifest.EntryType, "entryType");
        Require(manifest.HostApiVersion, "hostApiVersion");

        if (!string.Equals(Path.GetExtension(manifest.Assembly), ".dll", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("assembly 必須是 .dll 檔案。");
        if (!Version.TryParse(manifest.Version, out _))
            throw new InvalidDataException("version 格式無效。");
        if (!Version.TryParse(manifest.HostApiVersion, out var requiredApi) ||
            !Version.TryParse(PluginApi.Version, out var hostApi) ||
            requiredApi.Major != hostApi.Major)
            throw new InvalidDataException($"外掛 API {manifest.HostApiVersion} 與主程式 API {PluginApi.Version} 不相容。");
    }

    private static void Require(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"plugin.json 缺少必要欄位：{propertyName}。");
    }
}
