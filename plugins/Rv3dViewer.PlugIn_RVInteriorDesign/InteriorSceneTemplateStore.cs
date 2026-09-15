namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rv3dViewer.Core;

internal sealed record InteriorSceneTemplateDescriptor(
    string Name, string FolderPath, string? ProjectPath, int ModelCount, string? Error)
{
    internal bool IsValid => ProjectPath is not null && Error is null;
}

internal static class InteriorSceneTemplateStore
{
    private const string ManifestFileName = "scene-template.json";
    private static readonly object SceneLoadCacheLock = new();

    private sealed record SceneTemplateManifest(string Name, int ModelCount, int Version = 1);

    private sealed class MappedProgress(IProgress<int>? target, int offset, int range) : IProgress<int>
    {
        public void Report(int value) => target?.Report(offset + Math.Clamp(value, 0, 100) * range / 100);
    }

    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    internal static string ScenesDirectory => Path.Combine(
        Path.GetDirectoryName(typeof(InteriorSceneTemplateStore).Assembly.Location)!, "Scenes");

    internal static IReadOnlyList<InteriorSceneTemplateDescriptor> Scan()
    {
        Directory.CreateDirectory(ScenesDirectory);
        var result = new List<InteriorSceneTemplateDescriptor>();
        foreach (var folder in Directory.EnumerateDirectories(ScenesDirectory)
                     .Where(path => !Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal))
                     .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase))
        {
            var name = Path.GetFileName(folder);
            var projects = Directory.EnumerateFiles(folder, "*.rv3dproj", SearchOption.TopDirectoryOnly).ToArray();
            if (projects.Length != 1)
            {
                result.Add(new InteriorSceneTemplateDescriptor(name, folder, projects.FirstOrDefault(), 0,
                    projects.Length == 0 ? "缺少 .rv3dproj" : "包含多個 .rv3dproj"));
                continue;
            }
            try
            {
                var modelCount = ReadTemplateModelCount(folder, projects[0]);
                result.Add(new InteriorSceneTemplateDescriptor(name, folder, projects[0], modelCount, null));
            }
            catch (Exception exception) when (IsTemplateException(exception))
            {
                result.Add(new InteriorSceneTemplateDescriptor(name, folder, projects[0], 0, exception.Message));
            }
        }
        return result;
    }

    internal static async Task SaveCurrentAsync(string name, ViewerProject sourceProject,
        IReadOnlyCollection<ParametricDesignObject> objects, IProgress<int>? progress = null)
    {
        ValidateTemplateName(name);
        progress?.Report(0);
        Directory.CreateDirectory(ScenesDirectory);
        var staging = Path.Combine(ScenesDirectory, $".staging-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        try
        {
            foreach (var model in sourceProject.Models)
            {
                model.CaptureMeshMaterialIndices();
                model.CaptureProceduralGeometry();
            }
            progress?.Report(8);
            var project = JsonSerializer.Deserialize<ViewerProject>(
                              JsonSerializer.SerializeToUtf8Bytes(sourceProject, CloneOptions), CloneOptions)
                          ?? throw new InvalidDataException("無法複製目前場景資料。");
            var definitionsById = objects.ToDictionary(item => item.Id, item => item.Parameters);
            foreach (var model in project.Models)
            {
                if (definitionsById.TryGetValue(model.Id, out var parameters))
                    InteriorDesignSessionStore.EnsureModelGeometry(model, parameters, model.Id);
                else
                    model.RestoreProceduralGeometry();
                if (model.Meshes.Count == 0)
                    throw new InvalidDataException($"模型「{model.Name}」沒有可儲存的 Mesh。");
                InteriorMeshGeometry.Normalize(model);
                model.CaptureMeshMaterialIndices();
                model.CaptureProceduralGeometry();
            }
            var copiedPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var objectIndex = 0;
            foreach (var item in objects)
            {
                objectIndex++;
                if (item.Parameters is not ImportedAssetParameters imported ||
                    string.IsNullOrWhiteSpace(imported.ModelPath) || !File.Exists(imported.ModelPath))
                {
                    progress?.Report(8 + objectIndex * 57 / Math.Max(1, objects.Count));
                    continue;
                }
                copiedPaths[Path.GetFullPath(imported.ModelPath)] =
                    CopyModelPackage(imported.ModelPath, staging);
                progress?.Report(8 + objectIndex * 57 / Math.Max(1, objects.Count));
            }
            progress?.Report(70);
            foreach (var model in project.Models)
            foreach (var material in model.Materials)
                RewriteMaterialAssets(material, staging, copiedPaths);
            progress?.Report(82);
            RewriteImportedParameterPaths(project, copiedPaths);
            await InteriorDesignProjectStore.SaveAsync(project, Path.Combine(staging, $"{name}.rv3dproj"));
            progress?.Report(94);
            await File.WriteAllTextAsync(Path.Combine(staging, ManifestFileName),
                JsonSerializer.Serialize(new SceneTemplateManifest(name, project.Models.Count), CloneOptions));
            CommitStaging(staging, GetTemplateFolder(name));
            progress?.Report(100);
        }
        catch
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            throw;
        }
    }

    internal static async Task ImportProjectAsync(string projectPath, string name,
        IProgress<int>? progress = null)
    {
        ValidateTemplateName(name);
        progress?.Report(0);
        var fullProjectPath = Path.GetFullPath(projectPath);
        var loaded = await InteriorDesignProjectStore.LoadAsync(fullProjectPath);
        progress?.Report(18);
        var scenesRoot = Path.GetFullPath(ScenesDirectory) + Path.DirectorySeparatorChar;
        if ((Path.GetFullPath(Path.GetDirectoryName(fullProjectPath)!) + Path.DirectorySeparatorChar)
            .StartsWith(scenesRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("選取的專案已位於場景範本目錄內。");
        loaded.Project.Extensions[InteriorDesignProjectStore.ExtensionKey] =
            JsonSerializer.SerializeToElement(loaded.Extension, CloneOptions);
        var saveProgress = new MappedProgress(progress, 18, 82);
        await SaveCurrentAsync(name, loaded.Project, loaded.Objects, saveProgress);
    }

    internal static void Delete(InteriorSceneTemplateDescriptor template)
    {
        var expected = GetTemplateFolder(template.Name);
        if (!string.Equals(Path.GetFullPath(template.FolderPath), expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("範本路徑不在允許的 Scenes 目錄內。");
        if (Directory.Exists(expected)) Directory.Delete(expected, true);
    }

    internal static string PrepareLoadProjectPath(InteriorSceneTemplateDescriptor template)
    {
        if (!template.IsValid || string.IsNullOrWhiteSpace(template.ProjectPath))
            throw new InvalidDataException("場景範本沒有可載入的專案檔案。");
        var templateFolder = Path.GetFullPath(template.FolderPath);
        var expectedFolder = GetTemplateFolder(template.Name);
        if (!string.Equals(templateFolder, expectedFolder, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("範本路徑不在允許的 Scenes 目錄內。");

        var sourceProject = Path.GetFullPath(template.ProjectPath);
        if (!File.Exists(sourceProject))
            throw new FileNotFoundException("找不到場景範本專案檔案。", sourceProject);
        var projectInfo = new FileInfo(sourceProject);
        var fingerprint = ShortHash(
            $"{sourceProject}|{projectInfo.Length}|{projectInfo.LastWriteTimeUtc.Ticks}");
        var cacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rv3dViewer", "RVInteriorDesign", "SceneLoadCache");
        var destination = Path.Combine(cacheRoot, fingerprint);
        var cachedProject = Path.Combine(destination, "scene.rv3dproj");
        lock (SceneLoadCacheLock)
        {
            if (File.Exists(cachedProject)) return cachedProject;
            Directory.CreateDirectory(cacheRoot);
            var staging = Path.Combine(cacheRoot, $".staging-{Guid.NewGuid():N}");
            try
            {
                CopyDirectory(templateFolder, staging);
                File.Copy(sourceProject, Path.Combine(staging, "scene.rv3dproj"), true);
                if (Directory.Exists(destination)) Directory.Delete(destination, true);
                Directory.Move(staging, destination);
            }
            finally
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
        }
        return cachedProject;
    }

    internal static bool IsGeometryCacheReady(string cachedProjectPath) =>
        File.Exists(GetGeometryCacheMarkerPath(cachedProjectPath));

    internal static void MarkGeometryCacheReady(string cachedProjectPath)
    {
        var fullPath = Path.GetFullPath(cachedProjectPath);
        var cacheRoot = Path.GetFullPath(Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Rv3dViewer", "RVInteriorDesign", "SceneLoadCache")) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(cacheRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("幾何快取專案不在允許的目錄內。");
        File.WriteAllText(GetGeometryCacheMarkerPath(fullPath), DateTimeOffset.UtcNow.ToString("O"));
    }

    private static string GetGeometryCacheMarkerPath(string cachedProjectPath) =>
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(cachedProjectPath))!, "geometry-cache.ready");

    private static int ReadTemplateModelCount(string folder, string projectPath)
    {
        var manifestPath = Path.Combine(folder, ManifestFileName);
        if (File.Exists(manifestPath))
        {
            var manifest = JsonSerializer.Deserialize<SceneTemplateManifest>(
                File.ReadAllText(manifestPath), CloneOptions);
            if (manifest is not null && manifest.ModelCount >= 0)
                return manifest.ModelCount;
        }

        using var stream = File.OpenRead(projectPath);
        using var document = JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty("models", out var models) ||
            models.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("場景範本專案缺少 models 清單。");
        var modelCount = models.GetArrayLength();
        File.WriteAllText(manifestPath,
            JsonSerializer.Serialize(new SceneTemplateManifest(Path.GetFileName(folder), modelCount), CloneOptions));
        return modelCount;
    }

    private static void RewriteMaterialAssets(PbrMaterial material, string root,
        IDictionary<string, string> copiedPaths)
    {
        foreach (var slot in material.Textures.Values)
            slot.Path = CopyTexture(slot.Path, root, copiedPaths);
        foreach (var layer in material.TextureStacks.Values.SelectMany(stack => stack.Layers))
        {
            layer.Path = CopyTexture(layer.Path, root, copiedPaths);
            layer.Mask.Path = CopyTexture(layer.Mask.Path, root, copiedPaths);
        }
    }

    private static string CopyTexture(string path, string root, IDictionary<string, string> copiedPaths)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return path;
        var fullPath = Path.GetFullPath(path);
        if (copiedPaths.TryGetValue(fullPath, out var existing)) return existing;
        var destination = Path.Combine(root, "Assets", "Textures",
            $"{ShortHash(fullPath)}-{Path.GetFileName(fullPath)}");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(fullPath, destination, true);
        return copiedPaths[fullPath] = ToProjectRelativePath(root, destination);
    }

    private static string CopyModelPackage(string modelPath, string root)
    {
        var fullPath = Path.GetFullPath(modelPath);
        var package = Path.Combine(root, "Assets", "Models", $"{ShortHash(fullPath)}-model");
        var targetFileName = $"model{Path.GetExtension(fullPath).ToLowerInvariant()}";
        var targetPath = Path.Combine(package, targetFileName);
        Directory.CreateDirectory(package);
        File.Copy(fullPath, targetPath, true);
        CopyGltfReferencedFiles(fullPath, package);
        var sidecar = Path.Combine(Path.GetDirectoryName(fullPath)!,
            $"{Path.GetFileNameWithoutExtension(fullPath)}.rv3d-assets");
        if (Directory.Exists(sidecar))
            CopyDirectory(sidecar, Path.Combine(package, "model.rv3d-assets"));
        return ToProjectRelativePath(root, targetPath);
    }

    private static void CopyGltfReferencedFiles(string modelPath, string package)
    {
        if (!string.Equals(Path.GetExtension(modelPath), ".gltf", StringComparison.OrdinalIgnoreCase)) return;

        using var document = JsonDocument.Parse(File.ReadAllText(modelPath));
        var sourceDirectory = Path.GetDirectoryName(modelPath)!;
        var sourceRoot = Path.GetFullPath(sourceDirectory) + Path.DirectorySeparatorChar;
        foreach (var sectionName in new[] { "buffers", "images" })
        {
            if (!document.RootElement.TryGetProperty(sectionName, out var section) ||
                section.ValueKind != JsonValueKind.Array) continue;
            foreach (var entry in section.EnumerateArray())
            {
                if (!entry.TryGetProperty("uri", out var uriElement) ||
                    uriElement.ValueKind != JsonValueKind.String) continue;
                var uri = uriElement.GetString();
                if (string.IsNullOrWhiteSpace(uri) || uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                    Uri.TryCreate(uri, UriKind.Absolute, out _)) continue;

                var relativePath = Uri.UnescapeDataString(uri).Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(relativePath)) continue;
                var source = Path.GetFullPath(Path.Combine(sourceDirectory, relativePath));
                if (!source.StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(source))
                    throw new FileNotFoundException($"GLTF 引用的資產不存在：{uri}", source);
                var destination = Path.GetFullPath(Path.Combine(package, relativePath));
                var packageRoot = Path.GetFullPath(package) + Path.DirectorySeparatorChar;
                if (!destination.StartsWith(packageRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"GLTF 引用路徑超出模型資料夾：{uri}");
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, true);
            }
        }
    }

    private static void RewriteImportedParameterPaths(ViewerProject project,
        IReadOnlyDictionary<string, string> copiedPaths)
    {
        if (!project.Extensions.TryGetValue(InteriorDesignProjectStore.ExtensionKey, out var element)) return;
        var extension = element.Deserialize<InteriorDesignProjectExtension>(CloneOptions);
        if (extension is null) return;
        foreach (var item in extension.Items.Where(item => item.ParameterType == nameof(ImportedAssetParameters)))
        {
            var parameters = item.Parameters.Deserialize<ImportedAssetParameters>(CloneOptions);
            if (parameters is null || string.IsNullOrWhiteSpace(parameters.ModelPath)) continue;
            if (copiedPaths.TryGetValue(Path.GetFullPath(parameters.ModelPath), out var relative))
            {
                parameters.ModelPath = relative;
                item.Parameters = JsonSerializer.SerializeToElement(parameters, CloneOptions);
            }
        }
        project.Extensions[InteriorDesignProjectStore.ExtensionKey] =
            JsonSerializer.SerializeToElement(extension, CloneOptions);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            var name = Path.GetFileName(directory);
            if (name is "bin" or "obj" or ".vs" || name.StartsWith(".staging-", StringComparison.Ordinal))
                continue;
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                continue;
            CopyDirectory(directory, Path.Combine(destination, name));
        }
        foreach (var file in Directory.EnumerateFiles(source))
        {
            var extension = Path.GetExtension(file);
            if (extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".bak", StringComparison.OrdinalIgnoreCase))
                continue;
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }
    }

    private static void CommitStaging(string staging, string destination)
    {
        var backup = Path.Combine(ScenesDirectory, $".backup-{Guid.NewGuid():N}");
        if (Directory.Exists(destination)) Directory.Move(destination, backup);
        try
        {
            Directory.Move(staging, destination);
        }
        catch
        {
            if (Directory.Exists(destination) && !Directory.Exists(staging))
                Directory.Move(destination, staging);
            if (Directory.Exists(backup)) Directory.Move(backup, destination);
            throw;
        }
        if (Directory.Exists(backup))
        {
            try { Directory.Delete(backup, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static string GetTemplateFolder(string name)
    {
        ValidateTemplateName(name);
        var root = Path.GetFullPath(ScenesDirectory) + Path.DirectorySeparatorChar;
        var result = Path.GetFullPath(Path.Combine(ScenesDirectory, name));
        if (!(result + Path.DirectorySeparatorChar).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("範本名稱產生了無效路徑。");
        return result;
    }

    private static void ValidateTemplateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name is "." or ".." ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("請輸入有效的場景範本名稱。", nameof(name));
    }

    private static string ShortHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..10].ToLowerInvariant();

    private static string ToProjectRelativePath(string root, string path) =>
        Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    private static bool IsTemplateException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or
            NotSupportedException or ArgumentException or InvalidOperationException;
}
