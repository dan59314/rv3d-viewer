namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Rv3dViewer.Core;

internal static class DefaultInteriorAssetCatalog
{
    private const string ManifestFileName = "asset-manifest.json";

    internal static async Task<IReadOnlyList<ParametricDesignObject>> LoadAsync(
        IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var root = Path.Combine(Path.GetDirectoryName(typeof(DefaultInteriorAssetCatalog).Assembly.Location)!,
            "Models", "RvInterior");
        var manifestPath = Path.Combine(root, ManifestFileName);
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("找不到預設真實模型清單，請重新編譯 RV室內設計 Plugin。", manifestPath);

        await using var manifestStream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<DefaultInteriorAssetManifest>(manifestStream,
                           new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken)
                       ?? throw new InvalidDataException("預設真實模型清單內容無效。");
        if (manifest.FormatVersion != 1 || manifest.Assets.Count == 0 || manifest.Files.Count == 0)
            throw new InvalidDataException("預設真實模型清單版本或內容不完整。");

        for (var index = 0; index < manifest.Files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = manifest.Files[index];
            progress?.Report($"步驟 1/4：驗證預設真實模型 {index + 1}/{manifest.Files.Count}｜{entry.Path}");
            var path = ResolveSafePath(root, entry.Path);
            var file = new FileInfo(path);
            if (!file.Exists)
                throw new FileNotFoundException($"缺少預設真實模型檔案：{entry.Path}", path);
            if (file.Length != entry.Size)
                throw new InvalidDataException($"預設真實模型檔案大小不符：{entry.Path}");
            await using var stream = file.OpenRead();
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
            if (!hash.Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"預設真實模型檔案驗證失敗：{entry.Path}");
        }

        var result = new List<ParametricDesignObject>(manifest.Assets.Count);
        for (var index = 0; index < manifest.Assets.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var asset = manifest.Assets[index];
            progress?.Report($"步驟 2/4：載入真實模型 {index + 1}/{manifest.Assets.Count}｜{asset.Name}");
            result.Add(await LoadAssetAsync(root, asset, cancellationToken));
        }
        return result;
    }

    private static async Task<ParametricDesignObject> LoadAssetAsync(string root,
        DefaultInteriorAssetEntry asset, CancellationToken cancellationToken)
    {
        if (asset.TargetWidthMeters <= 0f || asset.Position.Length != 3 || asset.RotationDegrees.Length != 3)
            throw new InvalidDataException($"預設真實模型參數無效：{asset.Name}");
        var modelPath = ResolveSafePath(root, asset.ModelPath);
        var model = await InteriorModelImportService.ImportAsync(modelPath, cancellationToken);
        if (!SceneTraversal.TryCalculateBounds(model, out var rawBounds) || rawBounds.Size.X <= .000001f)
            throw new InvalidDataException($"預設真實模型沒有有效尺寸：{asset.Name}");

        var id = Guid.NewGuid();
        model.Id = id;
        model.Name = asset.Name;
        model.IsProcedural = true;
        var scale = asset.TargetWidthMeters / rawBounds.Size.X;
        model.Transform.Scale = new Vector3(scale);
        model.Transform.RotationDegrees = new Vector3(
            asset.RotationDegrees[0], asset.RotationDegrees[1], asset.RotationDegrees[2]);
        model.Transform.Position = Vector3.Zero;
        if (!SceneTraversal.TryCalculateBounds(model, out var transformedBounds))
            throw new InvalidDataException($"無法計算預設真實模型邊界：{asset.Name}");
        model.Transform.Position = new Vector3(
            asset.Position[0] - transformedBounds.Center.X,
            asset.Position[1] - transformedBounds.Minimum.Y,
            asset.Position[2] - transformedBounds.Center.Z);
        model.CaptureMeshMaterialIndices();
        model.CaptureProceduralGeometry();
        SceneTraversal.TryCalculateBounds(model, out var finalBounds);
        var parameters = new ImportedAssetParameters
        {
            Name = asset.Name,
            Category = asset.Category,
            ModelPath = modelPath,
            License = $"CC0｜Poly Haven｜{asset.Author}",
            SourceUrl = asset.SourceUrl,
            QualityStatus = "內建真實模型｜SHA-256 已驗證",
            Unit = InteriorAssetUnit.公尺,
            Pivot = InteriorAssetPivot.底部中心,
            SnapToGrid = false,
            PlacementMode = InteriorAssetPlacementMode.落地,
            CollisionPolicy = InteriorAssetCollisionPolicy.禁止重疊,
            TargetWidthCentimeters = (decimal)finalBounds.Size.X * 100m,
            TargetDepthCentimeters = (decimal)finalBounds.Size.Z * 100m,
            TargetHeightCentimeters = (decimal)finalBounds.Size.Y * 100m
        };
        return new ParametricDesignObject
        {
            Id = id,
            Parameters = parameters,
            Model = model,
            IsDefaultSceneObject = true
        };
    }

    private static string ResolveSafePath(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException($"預設資產包含無效路徑：{relativePath}");
        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"預設資產路徑超出允許目錄：{relativePath}");
        return fullPath;
    }
}

internal sealed class DefaultInteriorAssetManifest
{
    public int FormatVersion { get; set; }
    public List<DefaultInteriorAssetEntry> Assets { get; set; } = [];
    public List<DefaultInteriorAssetFile> Files { get; set; } = [];
}

internal sealed class DefaultInteriorAssetEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public float TargetWidthMeters { get; set; }
    public float[] Position { get; set; } = [];
    public float[] RotationDegrees { get; set; } = [];
}

internal sealed class DefaultInteriorAssetFile
{
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}
