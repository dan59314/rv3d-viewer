namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using Rv3dViewer.Core;

internal static class InteriorModelImportService
{
    private static readonly object AsciiStagingLock = new();

    internal static async Task<SceneModel> ImportAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var originalPath = Path.GetFullPath(filePath);
        var importPath = PrepareAsciiImportPath(originalPath);
        if (string.Equals(Path.GetExtension(importPath), ".gltf", StringComparison.OrdinalIgnoreCase))
        {
            var gltfModel = await InteriorAssimpGltfImporter.ImportAsync(importPath, cancellationToken)
                .ConfigureAwait(true);
            gltfModel.SourceFilePath = originalPath;
            gltfModel.AssetPath = Path.GetFileName(originalPath);
            InteriorMeshGeometry.Normalize(gltfModel, preserveWinding: true);
            return gltfModel;
        }
        var appAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly =>
            string.Equals(assembly.GetName().Name, "Rv3dViewer.App", StringComparison.OrdinalIgnoreCase));
        var importerType = appAssembly?.GetType("Rv3dViewer.App.AssimpModelImporter", throwOnError: false);
        if (importerType is null || Activator.CreateInstance(importerType, nonPublic: true) is not IModelImporter importer)
            throw new NotSupportedException("目前的 MainForm 版本沒有提供相容的模型匯入服務。");
        try
        {
            var model = await importer.ImportAsync(importPath, cancellationToken)
                .ConfigureAwait(true);
            model.SourceFilePath = originalPath;
            model.AssetPath = Path.GetFileName(originalPath);
            InteriorMeshGeometry.Normalize(model, preserveWinding: true);
            return model;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static string PrepareAsciiImportPath(string sourcePath)
    {
        if (sourcePath.All(character => character <= 0x7f))
            return sourcePath;
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("找不到模型檔案。", sourcePath);

        var info = new FileInfo(sourcePath);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{sourcePath}|{info.Length}|{info.LastWriteTimeUtc.Ticks}")))[..20].ToLowerInvariant();
        var packageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rv3dViewer", "RVInteriorDesign", "ImportStaging", fingerprint);
        var stagedPath = Path.Combine(packageDirectory, $"model{info.Extension.ToLowerInvariant()}");
        lock (AsciiStagingLock)
        {
            if (File.Exists(stagedPath))
                return stagedPath;
            Directory.CreateDirectory(packageDirectory);
            CopyPackageDirectory(info.DirectoryName!, packageDirectory);
            File.Copy(sourcePath, stagedPath, true);
            var sourceSidecar = Path.Combine(info.DirectoryName!, $"{Path.GetFileNameWithoutExtension(sourcePath)}.rv3d-assets");
            if (Directory.Exists(sourceSidecar))
                CopyPackageDirectory(sourceSidecar, Path.Combine(packageDirectory, "model.rv3d-assets"));
        }
        return stagedPath;
    }

    private static void CopyPackageDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        foreach (var directory in Directory.EnumerateDirectories(source))
            CopyPackageDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }
}
