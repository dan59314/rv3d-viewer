using System.Numerics;
using System.Text.Json;
using Rv3dViewer.Core;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class MaterialLibraryStoreTests
{
    [Fact]
    public async Task MaterialLibrary_RoundTripsInMtllibFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"rv3d-materials-{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(directory, "materials.mtllib");
            var source = new PbrMaterial
            {
                Name = "Copper",
                BaseColor = new Vector4(0.9f, 0.4f, 0.2f, 1f),
                Metallic = 1f,
                Roughness = 0.15f,
                Textures = new() { [TextureSemantic.Normal] = new() { Path = @"D:\Textures\copper.png" } }
            };

            var store = new JsonMaterialLibraryStore();
            await store.SaveAsync([source], path);
            var loaded = await store.LoadAsync(path);

            Assert.Single(loaded);
            Assert.Equal("Copper", loaded[0].Name);
            Assert.Equal(0.15f, loaded[0].Roughness);
            Assert.Equal(@"D:\Textures\copper.png", loaded[0].Textures[TextureSemantic.Normal].Path);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task ProjectSave_DoesNotContainMaterialLibrary()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"rv3d-project-{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(directory, "scene.rv3dproj");
            var project = new ViewerProject { MaterialLibrary = [new PbrMaterial { Name = "Legacy" }] };

            await new JsonProjectStore().SaveAsync(project, path);
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path));

            Assert.False(json.RootElement.TryGetProperty("materialLibrary", out _));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task LegacyProjectLoad_ExposesLibraryForMigration()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"rv3d-legacy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "legacy.rv3dproj");
            await File.WriteAllTextAsync(path,
                """
                {
                  "formatVersion": 4,
                  "materialLibrary": [
                    { "name": "Legacy Brass", "metallic": 1, "roughness": 0.2 }
                  ]
                }
                """);

            var loaded = await new JsonProjectStore().LoadAsync(path);

            Assert.Single(loaded.MaterialLibrary);
            Assert.Equal("Legacy Brass", loaded.MaterialLibrary[0].Name);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
