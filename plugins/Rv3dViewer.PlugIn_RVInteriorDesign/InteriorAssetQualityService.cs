namespace Rv3dViewer.RVInteriorDesignPlugin;

using Rv3dViewer.Core;

internal sealed record InteriorAssetQualityReport(
    long FileSizeBytes,
    int MeshCount,
    long TriangleCount,
    int MaterialCount,
    int TextureCount,
    int MissingTextureCount,
    bool HasBaseColorTexture,
    bool HasNormalTexture,
    bool HasMetallicRoughnessTexture,
    string QualityStatus,
    string QualityMessage);

internal static class InteriorAssetQualityService
{
    internal static InteriorAssetQualityReport Analyze(SceneModel model, string modelPath)
    {
        var textureEntries = model.Materials.SelectMany(EnumerateTextures).ToArray();
        var imagePaths = textureEntries
            .Select(entry => entry.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var missing = imagePaths.Count(path => !File.Exists(path));
        var triangleCount = model.Meshes.Sum(mesh => (long)mesh.TriangleCount);
        var hasUv = model.Meshes.Any(mesh => mesh.TextureCoordinates.Length == mesh.Positions.Length &&
                                             mesh.TextureCoordinates.Length > 0);
        var semantics = textureEntries.Select(entry => entry.Semantic).ToHashSet();
        var hasBaseColor = semantics.Contains(TextureSemantic.BaseColor);
        var hasNormal = semantics.Contains(TextureSemantic.Normal) || semantics.Contains(TextureSemantic.Bump);
        var hasMetallicRoughness = semantics.Contains(TextureSemantic.Metallic) &&
                                   semantics.Contains(TextureSemantic.Roughness);
        var warnings = new List<string>();
        if (missing > 0) warnings.Add($"遺失 {missing} 個貼圖");
        if (!hasUv && imagePaths.Length > 0) warnings.Add("Mesh 沒有完整 UV");
        if (model.Materials.Count == 0) warnings.Add("沒有材質");
        if (!hasBaseColor) warnings.Add("無基礎色貼圖");
        if (!hasNormal) warnings.Add("無法線貼圖");
        if (!hasMetallicRoughness) warnings.Add("無金屬度／粗糙度貼圖");
        if (triangleCount > 1_000_000) warnings.Add($"三角形過多（{triangleCount:N0}）");

        var status = missing > 0 || model.Meshes.Count == 0 || triangleCount == 0
            ? "錯誤"
            : hasBaseColor && hasNormal && hasMetallicRoughness
                ? "PBR 完整"
                : warnings.Count <= 2 ? "良好" : "需注意";
        return new InteriorAssetQualityReport(
            GetPackageSize(modelPath), model.Meshes.Count, triangleCount, model.Materials.Count,
            imagePaths.Length, missing, hasBaseColor, hasNormal, hasMetallicRoughness, status,
            warnings.Count == 0 ? "模型、材質與 PBR 貼圖檢查正常。" : string.Join("；", warnings));
    }

    private static IEnumerable<(TextureSemantic Semantic, string Path)> EnumerateTextures(PbrMaterial material)
    {
        foreach (var (semantic, slot) in material.Textures)
            if (slot.Enabled)
                yield return (semantic, slot.Path);
        foreach (var (semantic, stack) in material.TextureStacks)
        {
            if (!stack.Enabled) continue;
            foreach (var layer in stack.Layers.Where(layer => layer.Enabled &&
                                                               layer.Kind == TextureLayerKind.Image))
            {
                yield return (semantic, layer.Path);
                if (layer.Mask.Enabled && !string.IsNullOrWhiteSpace(layer.Mask.Path))
                    yield return (semantic, layer.Mask.Path);
            }
        }
    }

    private static long GetPackageSize(string modelPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(modelPath);
            if (string.Equals(Path.GetExtension(modelPath), ".gltf", StringComparison.OrdinalIgnoreCase) &&
                directory is not null)
                return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
            return File.Exists(modelPath) ? new FileInfo(modelPath).Length : 0L;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return 0L;
        }
    }
}
