using System.Drawing;
using System.Numerics;
using Rv3dViewer.Core;
using Rv3dViewer.MapPlugin;
using Rv3dViewer.Rendering.OpenGL;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class TextureStackTests
{
    [Fact]
    public async Task ProjectStore_RoundTripsTextureStacksAndMappingSettings()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"rv3d-map-{Guid.NewGuid():N}.rv3d");
        try
        {
            var material = new PbrMaterial { Name = "Layered" };
            material.GetOrCreateTextureStack(TextureSemantic.BaseColor).Layers.Add(new TextureLayer
            {
                Name = "Box layer", Kind = TextureLayerKind.Checker, BlendMode = TextureBlendMode.Overlay,
                Mapping = new TextureMappingSettings { Mode = TextureMappingMode.Triplanar, TriplanarBlend = 7f },
                Transform = new TextureTransformSettings { ScaleX = 2f, OffsetY = 0.25f }
            });
            var project = new ViewerProject
            {
                Models = [new SceneModel { Name = "Model", Materials = [material] }]
            };

            var store = new JsonProjectStore();
            await store.SaveAsync(project, filePath);
            var loaded = await store.LoadAsync(filePath);

            var layer = Assert.Single(loaded.Models[0].Materials[0].TextureStacks[TextureSemantic.BaseColor].Layers);
            Assert.Equal(TextureMappingMode.Triplanar, layer.Mapping.Mode);
            Assert.Equal(TextureBlendMode.Overlay, layer.BlendMode);
            Assert.Equal(2f, layer.Transform.ScaleX);
        }
        finally { if (File.Exists(filePath)) File.Delete(filePath); }
    }

    [Fact]
    public void Validate_MigratesLegacyTextureWithoutRemovingLegacySlot()
    {
        var material = new PbrMaterial
        {
            Textures = new Dictionary<TextureSemantic, TextureSlot>
            {
                [TextureSemantic.BaseColor] = new() { Path = "wood.png", Wrap = TextureWrap.ClampToEdge }
            }
        };

        material.Validate();

        var layer = Assert.Single(material.TextureStacks[TextureSemantic.BaseColor].Layers);
        Assert.Equal("wood.png", layer.Path);
        Assert.Equal(TextureWrap.ClampToEdge, layer.Sampling.Wrap);
        Assert.Equal("wood.png", material.Textures[TextureSemantic.BaseColor].Path);
    }

    [Fact]
    public void Clone_CreatesIndependentTextureLayersAndNestedSettings()
    {
        var material = new PbrMaterial();
        material.GetOrCreateTextureStack(TextureSemantic.Bump).Layers.Add(new TextureLayer
        {
            Name = "Height", Kind = TextureLayerKind.Noise,
            Transform = new TextureTransformSettings { ScaleX = 4f, ScaleY = 3f },
            Mask = new TextureMaskSettings { Enabled = true, Strength = 0.6f }
        });

        var clone = material.Clone();
        clone.TextureStacks[TextureSemantic.Bump].Layers[0].Transform.ScaleX = 9f;
        clone.TextureStacks[TextureSemantic.Bump].Layers[0].Mask.Strength = 0.2f;

        Assert.Equal(4f, material.TextureStacks[TextureSemantic.Bump].Layers[0].Transform.ScaleX);
        Assert.Equal(0.6f, material.TextureStacks[TextureSemantic.Bump].Layers[0].Mask.Strength);
    }

    [Fact]
    public void Composer_BlendsSolidAndProceduralLayers()
    {
        var stack = new TextureStack
        {
            Semantic = TextureSemantic.BaseColor,
            Layers =
            [
                new TextureLayer { Kind = TextureLayerKind.SolidColor, Color = new Vector4(1f, 0f, 0f, 1f) },
                new TextureLayer { Kind = TextureLayerKind.Checker, Color = new Vector4(0f, 1f, 0f, 1f), Opacity = 0.5f }
            ]
        };

        using var bitmap = TextureStackComposer.Compose(stack, path => path, 32);

        Assert.Equal(32, bitmap.Width);
        Assert.Equal(32, bitmap.Height);
        var sample = bitmap.GetPixel(2, 2);
        Assert.True(sample.R > 0 || sample.G > 0 || sample.B > 0);
        Assert.Equal(255, sample.A);
    }

    [Fact]
    public void PbrDiscovery_LoadsOnlySelectedTextureFamilyAndPrefersDedicatedMaps()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"rv3d-pbr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var selected = CreateImage(directory, "oak_basecolor_2k.png", Color.SaddleBrown);
            CreateImage(directory, "oak_orm_2k.png", Color.White);
            var roughness = CreateImage(directory, "oak_roughness_2k.png", Color.Gray);
            CreateImage(directory, "oak_normalgl_2k.png", Color.Blue);
            CreateImage(directory, "oak_height_2k.png", Color.Black);
            CreateImage(directory, "brick_basecolor_2k.png", Color.Red);

            var matches = PbrTextureSetDiscovery.Discover(selected);

            Assert.Equal(6, matches.Count);
            Assert.DoesNotContain(matches, match => Path.GetFileName(match.Path).StartsWith("brick", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(roughness, matches.Single(match => match.Semantic == TextureSemantic.Roughness).Path);
            Assert.Equal(TextureChannel.Blue, matches.Single(match => match.Semantic == TextureSemantic.Metallic).Channel);
            Assert.Equal(TextureColorSpace.Srgb, matches.Single(match => match.Semantic == TextureSemantic.BaseColor).ColorSpace);
            Assert.All(matches.Where(match => match.Semantic != TextureSemantic.BaseColor),
                match => Assert.Equal(TextureColorSpace.Linear, match.ColorSpace));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void TextureAssetStore_ImportsDeduplicatesAndRollsBackNewAssets()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rv3d-assets-{Guid.NewGuid():N}");
        var sourceDirectory = Path.Combine(root, "Source");
        var projectDirectory = Path.Combine(root, "Project");
        Directory.CreateDirectory(sourceDirectory);
        Directory.CreateDirectory(projectDirectory);
        try
        {
            var source = CreateImage(sourceDirectory, "wood.png", Color.BurlyWood);
            var store = new TextureAssetStore(Path.Combine(projectDirectory, "scene.rv3dproj"));

            var first = store.Import(source);
            var second = store.Import(source);
            var importedPath = store.Resolve(first);

            Assert.Equal(first, second);
            Assert.True(File.Exists(importedPath));
            Assert.Single(store.CreatedFiles);
            store.Rollback();
            Assert.False(File.Exists(importedPath));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void TextureAssetStore_RejectsUnreadableImageBeforeCopying()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rv3d-invalid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var source = Path.Combine(root, "broken.png");
            File.WriteAllText(source, "not an image");
            var store = new TextureAssetStore(Path.Combine(root, "Project", "scene.rv3dproj"));

            Assert.Throws<InvalidDataException>(() => store.Import(source));
            Assert.Empty(store.CreatedFiles);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void TextureAssetStore_FindsMissingImageAndMaskBeforeBake()
    {
        var stack = new TextureStack
        {
            Layers =
            [
                new TextureLayer { Kind = TextureLayerKind.Image, Path = "missing-color.png" },
                new TextureLayer
                {
                    Kind = TextureLayerKind.SolidColor,
                    Mask = new TextureMaskSettings { Enabled = true, Path = "missing-mask.png" }
                },
                new TextureLayer { Kind = TextureLayerKind.Image, Path = "ignored.png", Enabled = false }
            ]
        };

        var unavailable = TextureAssetStore.FindUnavailableAssets(stack, path => Path.Combine(Path.GetTempPath(), path));

        Assert.Equal(["missing-color.png", "missing-mask.png"], unavailable);
    }

    private static string CreateImage(string directory, string fileName, Color color)
    {
        var path = Path.Combine(directory, fileName);
        using var bitmap = new Bitmap(4, 4);
        using (var graphics = Graphics.FromImage(bitmap)) graphics.Clear(color);
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }
}
