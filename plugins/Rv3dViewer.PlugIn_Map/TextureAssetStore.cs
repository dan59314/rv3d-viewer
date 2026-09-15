using System.Drawing;
using System.Security.Cryptography;
using Rv3dViewer.Core;

namespace Rv3dViewer.MapPlugin;

public sealed class TextureAssetStore(string? projectFilePath)
{
    private readonly string? _projectDirectory = string.IsNullOrWhiteSpace(projectFilePath)
        ? null : Path.GetDirectoryName(Path.GetFullPath(projectFilePath));
    private readonly HashSet<string> _createdFiles = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> CreatedFiles => _createdFiles;

    public string Import(string sourcePath)
    {
        sourcePath = Path.GetFullPath(sourcePath);
        ValidateImage(sourcePath);
        if (string.IsNullOrWhiteSpace(_projectDirectory)) return sourcePath;

        var assetDirectory = Path.Combine(_projectDirectory, "Assets", "Textures");
        Directory.CreateDirectory(assetDirectory);
        var target = Path.Combine(assetDirectory, Path.GetFileName(sourcePath));
        if (sourcePath.Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            return Path.GetRelativePath(_projectDirectory, target);

        var stem = Path.GetFileNameWithoutExtension(target);
        var extension = Path.GetExtension(target);
        var suffix = 2;
        while (File.Exists(target) && !FilesEqual(sourcePath, target))
            target = Path.Combine(assetDirectory, $"{stem}_{suffix++}{extension}");
        if (!File.Exists(target))
        {
            File.Copy(sourcePath, target);
            _createdFiles.Add(target);
        }
        return Path.GetRelativePath(_projectDirectory, target);
    }

    public string Resolve(string path) => Path.IsPathRooted(path) || string.IsNullOrWhiteSpace(_projectDirectory)
        ? Path.GetFullPath(path)
        : Path.GetFullPath(Path.Combine(_projectDirectory, path));

    public void Commit() => _createdFiles.Clear();

    public void Rollback()
    {
        foreach (var path in _createdFiles)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        _createdFiles.Clear();
    }

    public static void ValidateImage(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("找不到圖片檔案。", path);
        if (!PbrTextureSetDiscovery.IsSupported(path)) throw new InvalidDataException($"不支援的圖片格式：{Path.GetExtension(path)}");
        try
        {
            using var image = Image.FromFile(path);
            if (image.Width <= 0 || image.Height <= 0) throw new InvalidDataException("圖片尺寸無效。");
        }
        catch (Exception exception) when (exception is ArgumentException or OutOfMemoryException)
        {
            throw new InvalidDataException("圖片內容無法讀取或格式不受支援。", exception);
        }
    }

    public static IReadOnlyList<string> FindUnavailableAssets(TextureStack stack, Func<string, string> resolvePath)
    {
        ArgumentNullException.ThrowIfNull(stack);
        ArgumentNullException.ThrowIfNull(resolvePath);
        var paths = stack.Layers.Where(layer => layer.Enabled).SelectMany(layer =>
        {
            var result = new List<string>();
            if (layer.Kind == TextureLayerKind.Image && !string.IsNullOrWhiteSpace(layer.Path)) result.Add(layer.Path);
            if (layer.Mask is { Enabled: true } && !string.IsNullOrWhiteSpace(layer.Mask.Path)) result.Add(layer.Mask.Path);
            return result;
        }).Distinct(StringComparer.OrdinalIgnoreCase);
        var unavailable = new List<string>();
        foreach (var path in paths)
        {
            try { ValidateImage(resolvePath(path)); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
            {
                unavailable.Add(path);
            }
        }
        return unavailable;
    }

    private static bool FilesEqual(string first, string second)
    {
        var firstInfo = new FileInfo(first);
        var secondInfo = new FileInfo(second);
        if (firstInfo.Length != secondInfo.Length) return false;
        using var firstStream = File.OpenRead(first);
        using var secondStream = File.OpenRead(second);
        return SHA256.HashData(firstStream).AsSpan().SequenceEqual(SHA256.HashData(secondStream));
    }
}
