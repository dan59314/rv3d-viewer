using Rv3dViewer.App;
using Rv3dViewer.Core;
using Rv3dViewer.Plugin.Abstractions;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class ProjectAssetServiceTests
{
    [Fact]
    public void PrepareForSaveReportsAssetBundlingProgress()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rv3d-progress-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var values = new List<int>();
            var progress = new InlineProgress<int>(values.Add);
            var project = new ViewerProject();

            ProjectAssetService.PrepareForSave(project, Path.Combine(root, "scene", "scene.rv3dproj"), progress);

            Assert.Contains(5, values);
            Assert.Contains(10, values);
            Assert.Equal(80, values[^1]);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task StagePluginAsset_DeduplicatesByContentAndRejectsUnsupportedFiles()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rv3d-stage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var first = Path.Combine(tempDirectory, "wood.png");
            var second = Path.Combine(tempDirectory, "same-wood.png");
            var unsupported = Path.Combine(tempDirectory, "script.exe");
            await File.WriteAllBytesAsync(first, [1, 3, 3, 7]);
            await File.WriteAllBytesAsync(second, [1, 3, 3, 7]);
            await File.WriteAllBytesAsync(unsupported, [1, 2]);

            var stagedFirst = ProjectAssetService.StagePluginAsset(first, PluginProjectAssetKind.Texture);
            var stagedSecond = ProjectAssetService.StagePluginAsset(second, PluginProjectAssetKind.Texture);

            Assert.True(File.Exists(stagedFirst));
            Assert.Equal(Path.GetFileName(stagedFirst).Split('_')[^1], Path.GetFileName(stagedSecond).Split('_')[^1]);
            Assert.Throws<NotSupportedException>(() =>
                ProjectAssetService.StagePluginAsset(unsupported, PluginProjectAssetKind.Texture));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public async Task PrepareForSave_BundlesTextureStackImagesAndMasks()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rv3d-stack-assets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var texture = Path.Combine(tempDirectory, "wall.png");
            var mask = Path.Combine(tempDirectory, "wall-mask.png");
            await File.WriteAllBytesAsync(texture, [1, 2, 3]);
            await File.WriteAllBytesAsync(mask, [4, 5, 6]);
            var layer = new TextureLayer
            {
                Path = texture,
                Mask = new TextureMaskSettings { Enabled = true, Path = mask }
            };
            var material = new PbrMaterial
            {
                TextureStacks = new Dictionary<TextureSemantic, TextureStack>
                {
                    [TextureSemantic.BaseColor] = new() { Layers = [layer] }
                }
            };
            var project = new ViewerProject
            {
                Models = [new SceneModel { IsProcedural = true, Materials = [material] }]
            };
            var projectDirectory = Path.Combine(tempDirectory, "Project");
            var projectPath = Path.Combine(projectDirectory, "stack.rv3dproj");

            ProjectAssetService.PrepareForSave(project, projectPath);

            Assert.StartsWith(Path.Combine("Assets", "Textures"), layer.Path);
            Assert.StartsWith(Path.Combine("Assets", "Textures"), layer.Mask.Path);
            Assert.True(File.Exists(Path.Combine(projectDirectory, layer.Path)));
            Assert.True(File.Exists(Path.Combine(projectDirectory, layer.Mask.Path)));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void GetBundledProjectPath_CreatesFolderFromFileName()
    {
        var requested = Path.Combine(Path.GetTempPath(), "Projects", "My Scene.rv3dproj");

        var result = ProjectAssetService.GetBundledProjectPath(requested);

        Assert.Equal(
            Path.Combine(Path.GetTempPath(), "Projects", "My Scene", "My Scene.rv3dproj"),
            result);
    }

    [Fact]
    public void GetBundledProjectPath_DoesNotDuplicateMatchingFolder()
    {
        var requested = Path.Combine(Path.GetTempPath(), "Projects", "My Scene", "My Scene.rv3dproj");

        var result = ProjectAssetService.GetBundledProjectPath(requested);

        Assert.Equal(Path.GetFullPath(requested), result);
    }

    [Fact]
    public async Task CanonicalizeBundledAssetPath_RemovesRepeatedHashSuffixes()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rv3d-canonical-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var content = new byte[] { 1, 2, 3, 4, 5 };
            var original = Path.Combine(tempDirectory, "model.bin");
            await File.WriteAllBytesAsync(original, content);
            var canonical = ProjectAssetService.CanonicalizeBundledAssetPath(original);
            var hash = Path.GetFileNameWithoutExtension(canonical)["model_".Length..];
            var repeated = Path.Combine(tempDirectory, $"model_{hash}_{hash}_{hash}.bin");
            await File.WriteAllBytesAsync(repeated, content);

            var result = ProjectAssetService.CanonicalizeBundledAssetPath(repeated);

            Assert.Equal(canonical, result);
            Assert.Equal(content, await File.ReadAllBytesAsync(result));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void FindMissingAssets_ReportsModelsAndEnabledTexturesOnly()
    {
        var project = new ViewerProject();
        var material = new PbrMaterial
        {
            Textures = new Dictionary<TextureSemantic, TextureSlot>
            {
                [TextureSemantic.BaseColor] = new() { Path = "missing.png" },
                [TextureSemantic.Normal] = new() { Path = "ignored.png", Enabled = false }
            }
        };
        project.Models.Add(new SceneModel { AssetPath = "missing.obj", Materials = [material] });

        var missing = ProjectAssetService.FindMissingAssets(project);

        Assert.Equal(2, missing.Count);
        Assert.Contains(missing, asset => asset.Kind == ProjectAssetKind.Model);
        Assert.Contains(missing, asset => asset.Kind == ProjectAssetKind.Texture && asset.Semantic == TextureSemantic.BaseColor);
    }

    [Fact]
    public async Task PrepareForSave_BundlesEnvironmentIntoProjectFolder()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rv3d-environment-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var source = Path.Combine(tempDirectory, "studio.hdr");
            await File.WriteAllBytesAsync(source, [1, 2, 3, 4]);
            var projectDirectory = Path.Combine(tempDirectory, "Project");
            var projectPath = Path.Combine(projectDirectory, "scene.rv3dproj");
            var project = new ViewerProject
            {
                Environment = new EnvironmentSettings { Enabled = true, Path = source }
            };

            ProjectAssetService.PrepareForSave(project, projectPath);

            Assert.StartsWith(Path.Combine("Assets", "Environment"), project.Environment.Path);
            Assert.True(File.Exists(Path.Combine(projectDirectory, project.Environment.Path)));
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public async Task ObjBundle_RemainsPortableAfterProjectDirectoryMoves()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rv3d-assets-{Guid.NewGuid():N}");
        var sourceDirectory = Path.Combine(tempDirectory, "Source");
        var sourceMaterials = Path.Combine(sourceDirectory, "materials");
        var sourceTextures = Path.Combine(sourceDirectory, "textures");
        var projectDirectory = Path.Combine(tempDirectory, "Project");
        var movedDirectory = Path.Combine(tempDirectory, "MovedProject");
        Directory.CreateDirectory(sourceMaterials);
        Directory.CreateDirectory(sourceTextures);

        try
        {
            var texturePath = Path.Combine(sourceTextures, "albedo color.png");
            await File.WriteAllBytesAsync(texturePath, [1, 2, 3, 4]);
            var materialPath = Path.Combine(sourceMaterials, "paint material.mtl");
            await File.WriteAllTextAsync(materialPath, "newmtl Paint\nmap_Kd -s 1 1 1 \"../textures/albedo color.png\"\n");
            var objPath = Path.Combine(sourceDirectory, "sample model.obj");
            await File.WriteAllTextAsync(objPath, "mtllib materials/paint material.mtl\nv 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");

            var projectPath = Path.Combine(projectDirectory, "portable.rv3dproj");
            var project = new ViewerProject { Name = "Portable" };
            var model = new SceneModel
            {
                Name = "Sample",
                SourceFilePath = objPath,
                Materials =
                [
                    new PbrMaterial
                    {
                        Name = "Paint",
                        Textures = new Dictionary<TextureSemantic, TextureSlot>
                        {
                            [TextureSemantic.BaseColor] = new() { Path = texturePath }
                        }
                    }
                ]
            };
            project.Models.Add(model);

            ProjectAssetService.PrepareForSave(project, projectPath);
            await new JsonProjectStore().SaveAsync(project, projectPath);

            var bundledObj = Path.Combine(projectDirectory, model.AssetPath);
            Assert.True(File.Exists(bundledObj));
            var objText = await File.ReadAllTextAsync(bundledObj);
            Assert.Contains("mtllib \"Materials/", objText);
            var bundledMtl = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(bundledObj)!, "Materials"), "*.mtl").Single();
            var mtlText = await File.ReadAllTextAsync(bundledMtl);
            Assert.Contains("map_Kd -s 1 1 1 \"../../../Textures/", mtlText);
            Assert.True(File.Exists(Path.Combine(projectDirectory, model.Materials[0].Textures[TextureSemantic.BaseColor].Path)));

            Directory.Move(projectDirectory, movedDirectory);
            var movedProjectPath = Path.Combine(movedDirectory, "portable.rv3dproj");
            var loaded = await new JsonProjectStore().LoadAsync(movedProjectPath);
            var movedObj = ProjectAssetService.ResolveModelPath(loaded, loaded.Models.Single());
            Assert.True(File.Exists(movedObj));
            var movedMtl = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(movedObj)!, "Materials"), "*.mtl").Single();
            Assert.True(File.Exists(ResolveLastQuotedPath(movedMtl, await File.ReadAllTextAsync(movedMtl))));
        }
        finally
        {
            if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
        }
    }

    private static string ResolveLastQuotedPath(string materialPath, string content)
    {
        var lastQuote = content.LastIndexOf('"');
        var firstQuote = content.LastIndexOf('"', lastQuote - 1);
        var relative = content[(firstQuote + 1)..lastQuote].Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(materialPath)!, relative));
    }
}

file sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
