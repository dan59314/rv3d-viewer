using Rv3dViewer.Core;

namespace Rv3dViewer.ReliefPlugin;

internal sealed record ReliefSceneAsset(string Path, bool CreatedByPlugin);

internal static class ReliefSceneAssetService
{
    internal static ReliefSceneAsset StageTexture(
        SceneModel model,
        string sourceTexturePath,
        ViewerProject project)
    {
        if (!File.Exists(sourceTexturePath))
            throw new FileNotFoundException("找不到浮雕工作貼圖。", sourceTexturePath);

        var destinationDirectory = GetDestinationDirectory(project);
        Directory.CreateDirectory(destinationDirectory);
        var destination = Path.Combine(
            destinationDirectory,
            $"relief-{model.Id:N}{Path.GetExtension(sourceTexturePath).ToLowerInvariant()}");
        var sourceFullPath = Path.GetFullPath(sourceTexturePath);
        var destinationFullPath = Path.GetFullPath(destination);
        var created = !File.Exists(destinationFullPath);

        if (!string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
        {
            var temporaryPath = destinationFullPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                File.Copy(sourceFullPath, temporaryPath, overwrite: true);
                File.Move(temporaryPath, destinationFullPath, overwrite: true);
            }
            finally
            {
                try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
                catch { }
            }
        }

        foreach (var material in model.Materials)
        {
            foreach (var (semantic, slot) in material.Textures)
            {
                if (!MatchesSource(slot.Path)) continue;
                slot.Path = destinationFullPath;
                if (semantic == TextureSemantic.BaseColor)
                {
                    slot.Enabled = true;
                    material.BaseColor = System.Numerics.Vector4.One;
                }
            }

            foreach (var (semantic, stack) in material.TextureStacks)
            {
                var containsBaseColorTexture = false;
                foreach (var layer in stack.Layers)
                {
                    if (MatchesSource(layer.Path))
                    {
                        layer.Path = destinationFullPath;
                        if (semantic == TextureSemantic.BaseColor)
                        {
                            layer.Enabled = true;
                            containsBaseColorTexture = true;
                        }
                    }
                    if (MatchesSource(layer.Mask.Path)) layer.Mask.Path = destinationFullPath;
                }

                if (containsBaseColorTexture)
                {
                    stack.Enabled = true;
                    material.BaseColor = System.Numerics.Vector4.One;
                }
            }
            material.Validate();
        }

        model.SourceFilePath = null;
        model.AssetPath = string.Empty;
        model.CaptureMeshMaterialIndices();
        model.CaptureProceduralGeometry();
        return new ReliefSceneAsset(destinationFullPath, created);

        bool MatchesSource(string? path) =>
            !string.IsNullOrWhiteSpace(path) &&
            string.Equals(Path.GetFullPath(path), sourceFullPath, StringComparison.OrdinalIgnoreCase);
    }

    internal static void RollBack(ReliefSceneAsset? asset)
    {
        if (asset is not { CreatedByPlugin: true }) return;
        try { if (File.Exists(asset.Path)) File.Delete(asset.Path); }
        catch { }
    }

    private static string GetDestinationDirectory(ViewerProject project)
    {
        if (!string.IsNullOrWhiteSpace(project.ProjectFilePath))
        {
            var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(project.ProjectFilePath));
            if (!string.IsNullOrWhiteSpace(projectDirectory))
                return Path.Combine(projectDirectory, "Assets", "Textures");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rv3dViewer",
            "ReliefPlugin",
            "SceneAssets");
    }
}
