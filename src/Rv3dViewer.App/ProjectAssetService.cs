using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Rv3dViewer.Core;
using Rv3dViewer.Plugin.Abstractions;

namespace Rv3dViewer.App;

public static partial class ProjectAssetService
{
    private static readonly IReadOnlyDictionary<PluginProjectAssetKind, HashSet<string>> PluginAssetExtensions =
        new Dictionary<PluginProjectAssetKind, HashSet<string>>
        {
            [PluginProjectAssetKind.Model] = new(StringComparer.OrdinalIgnoreCase)
                { ".glb", ".gltf", ".obj", ".fbx", ".stl", ".dae", ".ply", ".3ds" },
            [PluginProjectAssetKind.Texture] = new(StringComparer.OrdinalIgnoreCase)
                { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".tif", ".tiff", ".webp", ".hdr", ".exr" },
            [PluginProjectAssetKind.License] = new(StringComparer.OrdinalIgnoreCase)
                { ".txt", ".md", ".json", ".xml", ".pdf" }
        };

    private static readonly HashSet<string> TextureCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "map_Ka", "map_Kd", "map_Ks", "map_Ke", "map_Ns", "map_d",
        "map_bump", "bump", "disp", "decal", "norm", "refl"
    };

    public static string GetBundledProjectPath(string requestedProjectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedProjectPath);

        var fullPath = Path.GetFullPath(requestedProjectPath);
        var fileName = Path.GetFileName(fullPath);
        var projectName = Path.GetFileNameWithoutExtension(fullPath);
        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project file name must not be empty.", nameof(requestedProjectPath));

        var parentDirectory = Path.GetDirectoryName(fullPath)!;
        var currentDirectoryName = new DirectoryInfo(parentDirectory).Name;
        if (currentDirectoryName.Equals(projectName, StringComparison.OrdinalIgnoreCase))
            return fullPath;

        return Path.Combine(parentDirectory, projectName, fileName);
    }

    public static void PrepareForSave(
        ViewerProject project,
        string projectFilePath,
        IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFilePath);

        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectFilePath))!;
        var modelsDirectory = Path.Combine(projectDirectory, "Assets", "Models");
        var texturesDirectory = Path.Combine(projectDirectory, "Assets", "Textures");
        var environmentDirectory = Path.Combine(projectDirectory, "Assets", "Environment");
        var skyboxesDirectory = Path.Combine(projectDirectory, "Assets", "Skybox");
        Directory.CreateDirectory(modelsDirectory);
        Directory.CreateDirectory(texturesDirectory);
        Directory.CreateDirectory(environmentDirectory);
        Directory.CreateDirectory(skyboxesDirectory);
        progress?.Report(5);

        var environmentSource = ResolveExistingPath(project, project.Environment.Path);
        if (File.Exists(environmentSource))
        {
            var destination = CopyUnique(environmentSource, environmentDirectory);
            project.Environment.Path = Path.GetRelativePath(projectDirectory, destination);
        }
        progress?.Report(10);

        foreach (var skybox in project.Skyboxes)
        {
            var skyboxDirectory = Path.Combine(skyboxesDirectory, SanitizeFileName(skybox.Name));
            Directory.CreateDirectory(skyboxDirectory);
            skybox.PositiveX = BundleSkyboxFile(project, skybox.PositiveX, skyboxDirectory, projectDirectory, "px");
            skybox.NegativeX = BundleSkyboxFile(project, skybox.NegativeX, skyboxDirectory, projectDirectory, "nx");
            skybox.PositiveY = BundleSkyboxFile(project, skybox.PositiveY, skyboxDirectory, projectDirectory, "py");
            skybox.NegativeY = BundleSkyboxFile(project, skybox.NegativeY, skyboxDirectory, projectDirectory, "ny");
            skybox.PositiveZ = BundleSkyboxFile(project, skybox.PositiveZ, skyboxDirectory, projectDirectory, "pz");
            skybox.NegativeZ = BundleSkyboxFile(project, skybox.NegativeZ, skyboxDirectory, projectDirectory, "nz");
            if (!string.IsNullOrWhiteSpace(skybox.SourcePanorama))
                skybox.SourcePanorama = BundleSkyboxFile(project, skybox.SourcePanorama, skyboxDirectory, projectDirectory, "source_panorama");
        }

        for (var modelIndex = 0; modelIndex < project.Models.Count; modelIndex++)
        {
            var model = project.Models[modelIndex];
            var source = ResolveExistingPath(project, model.SourceFilePath ?? model.AssetPath);
            if (!model.IsProcedural && File.Exists(source))
            {
                var isObj = Path.GetExtension(source).Equals(".obj", StringComparison.OrdinalIgnoreCase);
                var destination = isObj && !IsWithinDirectory(source, modelsDirectory)
                    ? BundleObj(model, source, modelsDirectory, texturesDirectory)
                    : isObj
                        ? source
                        : CopyUnique(source, modelsDirectory);
                model.AssetPath = Path.GetRelativePath(projectDirectory, destination);
                model.SourceFilePath = destination;
            }

            foreach (var slot in model.Materials.SelectMany(m => m.Textures.Values))
            {
                var textureSource = ResolveExistingPath(project, slot.Path);
                if (!File.Exists(textureSource)) continue;
                var destination = CopyUnique(textureSource, texturesDirectory);
                slot.Path = Path.GetRelativePath(projectDirectory, destination);
            }

            foreach (var layer in model.Materials
                         .SelectMany(material => material.TextureStacks.Values)
                         .SelectMany(stack => stack.Layers))
            {
                if (!string.IsNullOrWhiteSpace(layer.Path))
                {
                    var textureSource = ResolveExistingPath(project, layer.Path);
                    if (File.Exists(textureSource))
                    {
                        var destination = CopyUnique(textureSource, texturesDirectory);
                        layer.Path = Path.GetRelativePath(projectDirectory, destination);
                    }
                }

                if (!string.IsNullOrWhiteSpace(layer.Mask.Path))
                {
                    var maskSource = ResolveExistingPath(project, layer.Mask.Path);
                    if (File.Exists(maskSource))
                    {
                        var destination = CopyUnique(maskSource, texturesDirectory);
                        layer.Mask.Path = Path.GetRelativePath(projectDirectory, destination);
                    }
                }
            }
            progress?.Report(10 + (int)Math.Round((modelIndex + 1) * 70d / Math.Max(1, project.Models.Count)));
        }
        project.ProjectFilePath = Path.GetFullPath(projectFilePath);
        progress?.Report(80);
    }

    private static string BundleSkyboxFile(
        ViewerProject project, string path, string destinationDirectory, string projectDirectory, string baseName)
    {
        var source = ResolveExistingPath(project, path);
        if (!File.Exists(source)) return path;
        var destination = Path.Combine(destinationDirectory, $"{baseName}{Path.GetExtension(source).ToLowerInvariant()}");
        if (!Path.GetFullPath(source).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            File.Copy(source, destination, overwrite: true);
        return Path.GetRelativePath(projectDirectory, destination);
    }

    public static string StagePluginAsset(string sourceFilePath, PluginProjectAssetKind kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);
        var source = Path.GetFullPath(sourceFilePath);
        if (!File.Exists(source))
            throw new FileNotFoundException("找不到要匯入的 Plugin 資產。", source);
        if (!PluginAssetExtensions[kind].Contains(Path.GetExtension(source)))
            throw new NotSupportedException($"不支援的 {kind} 資產格式：{Path.GetExtension(source)}");

        var cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rv3dViewer",
            "PluginAssetCache",
            kind.ToString());
        Directory.CreateDirectory(cacheDirectory);
        return CopyUnique(source, cacheDirectory);
    }

    public static string ResolveModelPath(ViewerProject project, SceneModel model) =>
        ResolveExistingPath(project, model.AssetPath);

    public static string CanonicalizeBundledAssetPath(string path)
    {
        if (!File.Exists(path)) return path;
        if (Path.GetExtension(path).Equals(".obj", StringComparison.OrdinalIgnoreCase))
            return Path.GetFullPath(path);
        return CopyUnique(path, Path.GetDirectoryName(Path.GetFullPath(path))!);
    }

    public static IReadOnlyList<MissingProjectAsset> FindMissingAssets(ViewerProject project)
    {
        var missing = new List<MissingProjectAsset>();
        foreach (var model in project.Models)
        {
            var modelPath = ResolveExistingPath(project, model.SourceFilePath ?? model.AssetPath);
            if (!model.IsProcedural && !File.Exists(modelPath))
                missing.Add(new MissingProjectAsset(ProjectAssetKind.Model, model, null, null, modelPath));

            foreach (var material in model.Materials)
            foreach (var (semantic, slot) in material.Textures)
            {
                if (!slot.Enabled || string.IsNullOrWhiteSpace(slot.Path)) continue;
                var texturePath = ResolveExistingPath(project, slot.Path);
                if (!File.Exists(texturePath))
                    missing.Add(new MissingProjectAsset(ProjectAssetKind.Texture, model, material, semantic, texturePath));
            }


            foreach (var material in model.Materials)
            foreach (var layer in material.TextureStacks.Values.SelectMany(stack => stack.Layers))
            {
                if (layer.Enabled && !string.IsNullOrWhiteSpace(layer.Path))
                {
                    var texturePath = ResolveExistingPath(project, layer.Path);
                    if (!File.Exists(texturePath))
                        missing.Add(new MissingProjectAsset(ProjectAssetKind.Texture, model, material, null, texturePath));
                }

                if (layer.Mask.Enabled && !string.IsNullOrWhiteSpace(layer.Mask.Path))
                {
                    var maskPath = ResolveExistingPath(project, layer.Mask.Path);
                    if (!File.Exists(maskPath))
                        missing.Add(new MissingProjectAsset(ProjectAssetKind.Texture, model, material, null, maskPath));
                }
            }
        }
        return missing;
    }

    private static string BundleObj(SceneModel model, string objPath, string modelsDirectory, string texturesDirectory)
    {
        var bundleName = $"{SanitizeFileName(Path.GetFileNameWithoutExtension(objPath))}_{model.Id:N}"[..Math.Min(
            SanitizeFileName(Path.GetFileNameWithoutExtension(objPath)).Length + 9, 72)];
        var bundleDirectory = Path.Combine(modelsDirectory, bundleName);
        var materialsDirectory = Path.Combine(bundleDirectory, "Materials");
        Directory.CreateDirectory(bundleDirectory);
        Directory.CreateDirectory(materialsDirectory);

        var objDirectory = Path.GetDirectoryName(objPath)!;
        var output = new List<string>();
        foreach (var line in File.ReadLines(objPath))
        {
            if (!TrySplitCommand(line, out var command, out var arguments) ||
                !command.Equals("mtllib", StringComparison.OrdinalIgnoreCase))
            {
                output.Add(line);
                continue;
            }

            var bundledLibraries = new List<string>();
            foreach (var libraryPath in ResolveMaterialLibraries(arguments, objDirectory))
            {
                var hash = ComputeFileHash(libraryPath);
                var fileName = $"{SanitizeFileName(Path.GetFileNameWithoutExtension(libraryPath))}_{hash}{Path.GetExtension(libraryPath)}";
                var destination = Path.Combine(materialsDirectory, fileName);
                RewriteMaterialLibrary(libraryPath, destination, texturesDirectory);
                bundledLibraries.Add(QuoteIfNeeded(ToAssetPath(Path.GetRelativePath(bundleDirectory, destination))));
            }

            output.Add(bundledLibraries.Count == 0 ? line : $"mtllib {string.Join(' ', bundledLibraries)}");
        }

        var bundledObj = Path.Combine(bundleDirectory, Path.GetFileName(objPath));
        File.WriteAllLines(bundledObj, output, new UTF8Encoding(false));
        return bundledObj;
    }

    private static IEnumerable<string> ResolveMaterialLibraries(string arguments, string objDirectory)
    {
        var wholePath = ResolveDependencyPath(objDirectory, Unquote(arguments.Trim()));
        if (File.Exists(wholePath))
        {
            yield return wholePath;
            yield break;
        }

        foreach (Match match in QuotedOrBareTokenRegex().Matches(arguments))
        {
            var candidate = ResolveDependencyPath(objDirectory, Unquote(match.Value));
            if (File.Exists(candidate)) yield return candidate;
        }
    }

    private static void RewriteMaterialLibrary(string sourcePath, string destinationPath, string texturesDirectory)
    {
        var sourceDirectory = Path.GetDirectoryName(sourcePath)!;
        var destinationDirectory = Path.GetDirectoryName(destinationPath)!;
        var output = new List<string>();

        foreach (var line in File.ReadLines(sourcePath))
        {
            if (!TrySplitCommand(line, out var command, out var arguments) ||
                !TextureCommands.Contains(command) ||
                !TryFindTextureArgument(arguments, sourceDirectory, out var prefix, out var texturePath))
            {
                output.Add(line);
                continue;
            }

            var bundledTexture = CopyUnique(texturePath, texturesDirectory);
            var relativeTexture = QuoteIfNeeded(ToAssetPath(Path.GetRelativePath(destinationDirectory, bundledTexture)));
            output.Add(string.IsNullOrWhiteSpace(prefix)
                ? $"{command} {relativeTexture}"
                : $"{command} {prefix} {relativeTexture}");
        }

        File.WriteAllLines(destinationPath, output, new UTF8Encoding(false));
    }

    private static bool TryFindTextureArgument(
        string arguments,
        string sourceDirectory,
        out string prefix,
        out string texturePath)
    {
        for (var index = 0; index < arguments.Length; index++)
        {
            if (index > 0 && !char.IsWhiteSpace(arguments[index - 1])) continue;
            var candidateText = Unquote(arguments[index..].Trim());
            var candidate = ResolveDependencyPath(sourceDirectory, candidateText);
            if (!File.Exists(candidate)) continue;
            prefix = arguments[..index].Trim();
            texturePath = candidate;
            return true;
        }

        prefix = string.Empty;
        texturePath = string.Empty;
        return false;
    }

    private static bool TrySplitCommand(string line, out string command, out string arguments)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed[0] == '#')
        {
            command = string.Empty;
            arguments = string.Empty;
            return false;
        }

        var separator = trimmed.IndexOfAny([' ', '\t']);
        if (separator < 0)
        {
            command = trimmed;
            arguments = string.Empty;
            return true;
        }

        command = trimmed[..separator];
        arguments = trimmed[(separator + 1)..].Trim();
        return true;
    }

    private static string ResolveExistingPath(ViewerProject project, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
        var baseDirectory = Path.GetDirectoryName(project.ProjectFilePath);
        return string.IsNullOrWhiteSpace(baseDirectory)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static string ResolveDependencyPath(string baseDirectory, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path.Replace('/', Path.DirectorySeparatorChar)));

    private static string CopyUnique(string source, string destinationDirectory)
    {
        var hash = ComputeFileHash(source);
        var baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(source));
        var hashSuffix = $"_{hash}";
        while (baseName.EndsWith(hashSuffix, StringComparison.OrdinalIgnoreCase))
            baseName = baseName[..^hashSuffix.Length];
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "asset";
        var destination = Path.Combine(
            destinationDirectory,
            $"{baseName}_{hash}{Path.GetExtension(source)}");
        if (Path.GetFullPath(source).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            return destination;
        if (!File.Exists(destination)) File.Copy(source, destination);
        return destination;
    }

    private static bool IsWithinDirectory(string path, string directory)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(directory), Path.GetFullPath(path));
        return !Path.IsPathRooted(relative) &&
               !relative.Equals("..", StringComparison.Ordinal) &&
               !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static string ComputeFileHash(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))[..8].ToLowerInvariant();

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "asset" : sanitized;
    }

    private static string Unquote(string value) =>
        value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;

    private static string ToAssetPath(string path) => path.Replace(Path.DirectorySeparatorChar, '/');

    private static string QuoteIfNeeded(string path) =>
        path.Any(char.IsWhiteSpace) ? $"\"{path}\"" : path;

    [GeneratedRegex("\\\"[^\\\"]+\\\"|\\S+")]
    private static partial Regex QuotedOrBareTokenRegex();
}

public enum ProjectAssetKind
{
    Model,
    Texture
}

public sealed record MissingProjectAsset(
    ProjectAssetKind Kind,
    SceneModel Model,
    PbrMaterial? Material,
    TextureSemantic? Semantic,
    string ExpectedPath);
