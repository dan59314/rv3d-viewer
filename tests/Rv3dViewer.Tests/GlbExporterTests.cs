using System.Numerics;
using System.Text.Json;
using System.Drawing;
using Assimp;
using Rv3dViewer.App;
using Rv3dViewer.Core;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class GlbExporterTests
{
    [Fact]
    public async Task ExportAsync_WritesValidGlbWithVisibleMeshesAndPbrExtensions()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"rv3d-{Guid.NewGuid():N}.glb");
        try
        {
            var material = new PbrMaterial
            {
                Name = "Glass",
                RenderMode = MaterialRenderMode.Glass,
                Transmission = 0.8f,
                IndexOfRefraction = 1.45f,
                Thickness = 0.2f,
                Dispersion = 0.044f
            };
            var visibleMesh = new MeshData
            {
                Name = "Visible",
                Positions = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
                Normals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
                TextureCoordinates = [Vector2.Zero, Vector2.UnitX, Vector2.UnitY],
                Indices = [0, 1, 2]
            };
            var hiddenMesh = new MeshData
            {
                Name = "Hidden",
                Positions = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
                Indices = [0, 1, 2]
            };
            var model = new SceneModel
            {
                Name = "Model",
                Materials = [material],
                Meshes = [visibleMesh, hiddenMesh],
                HiddenMeshIndices = [1],
                Transform = new TransformState { Position = new Vector3(2f, 3f, 4f) }
            };
            var project = new ViewerProject { Models = [model] };

            await GlbExporter.ExportAsync(project, filePath);

            var bytes = await File.ReadAllBytesAsync(filePath);
            Assert.Equal(0x46546C67u, BitConverter.ToUInt32(bytes, 0));
            Assert.Equal(2u, BitConverter.ToUInt32(bytes, 4));
            Assert.Equal((uint)bytes.Length, BitConverter.ToUInt32(bytes, 8));
            Assert.Equal(0x4E4F534Au, BitConverter.ToUInt32(bytes, 16));
            var jsonLength = checked((int)BitConverter.ToUInt32(bytes, 12));
            using var json = JsonDocument.Parse(bytes.AsMemory(20, jsonLength));
            var root = json.RootElement;

            Assert.Equal("2.0", root.GetProperty("asset").GetProperty("version").GetString());
            Assert.Single(root.GetProperty("meshes").EnumerateArray());
            Assert.Single(root.GetProperty("nodes").EnumerateArray());
            var matrix = root.GetProperty("nodes")[0].GetProperty("matrix");
            Assert.Equal(2f, matrix[12].GetSingle());
            Assert.Equal(3f, matrix[13].GetSingle());
            Assert.Equal(4f, matrix[14].GetSingle());
            Assert.Contains(root.GetProperty("extensionsUsed").EnumerateArray(),
                item => item.GetString() == "KHR_materials_transmission");
            Assert.Contains(root.GetProperty("extensionsUsed").EnumerateArray(),
                item => item.GetString() == "KHR_materials_dispersion");
            Assert.Equal(0.044f, root.GetProperty("materials")[0].GetProperty("extensions")
                .GetProperty("KHR_materials_dispersion").GetProperty("dispersion").GetSingle());
            Assert.Equal("BLEND", root.GetProperty("materials")[0].GetProperty("alphaMode").GetString());
            Assert.True(BitConverter.ToUInt32(bytes, 20 + jsonLength + 4) > 0);
            Assert.Equal(0x004E4942u, BitConverter.ToUInt32(bytes, 20 + jsonLength + 4));

            using var assimp = new AssimpContext();
            var imported = assimp.ImportFile(filePath, PostProcessSteps.Triangulate);
            Assert.NotNull(imported);
            Assert.Single(imported.Meshes);
            Assert.Equal(3, imported.Meshes[0].VertexCount);
            Assert.Single(imported.Meshes[0].Faces);
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ExportAsync_RejectsSceneWithoutVisibleMesh()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"rv3d-{Guid.NewGuid():N}.glb");
        var project = new ViewerProject
        {
            Models = [new SceneModel { IsVisible = false }]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => GlbExporter.ExportAsync(project, filePath));

        Assert.Contains("可見 Mesh", exception.Message);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task ExportAsync_NormalizesBakedTextureStackToGltfTopLeftOrigin()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"rv3d-glb-texture-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var texturePath = Path.Combine(directory, "orientation.png");
        var filePath = Path.Combine(directory, "orientation.glb");
        try
        {
            using (var texture = new Bitmap(16, 16))
            {
                for (var y = 0; y < texture.Height; y++)
                for (var x = 0; x < texture.Width; x++)
                    texture.SetPixel(x, y, y < texture.Height / 2 ? Color.Red : Color.Blue);
                texture.Save(texturePath);
            }

            var material = new PbrMaterial { Name = "Orientation" };
            material.GetOrCreateTextureStack(TextureSemantic.BaseColor).Layers.Add(new TextureLayer
            {
                Name = "Asymmetric",
                Path = texturePath
            });
            var mesh = new MeshData
            {
                Positions = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
                Normals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
                TextureCoordinates = [new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f)],
                Indices = [0, 1, 2]
            };
            var project = new ViewerProject
            {
                Models = [new SceneModel { Materials = [material], Meshes = [mesh] }]
            };

            await GlbExporter.ExportAsync(project, filePath);

            var bytes = await File.ReadAllBytesAsync(filePath);
            var jsonLength = checked((int)BitConverter.ToUInt32(bytes, 12));
            using var json = JsonDocument.Parse(bytes.AsMemory(20, jsonLength));
            var root = json.RootElement;
            var binaryOffset = 20 + jsonLength + 8;

            var imageViewIndex = root.GetProperty("images")[0].GetProperty("bufferView").GetInt32();
            var imageView = root.GetProperty("bufferViews")[imageViewIndex];
            var imageOffset = binaryOffset + GetOptionalInt(imageView, "byteOffset");
            var imageLength = imageView.GetProperty("byteLength").GetInt32();
            using (var stream = new MemoryStream(bytes, imageOffset, imageLength, writable: false))
            using (var exportedTexture = new Bitmap(stream))
            {
                Assert.True(exportedTexture.GetPixel(0, 0).R > exportedTexture.GetPixel(0, 0).B,
                    "The top of the embedded glTF image should remain the source image's top.");
                Assert.True(exportedTexture.GetPixel(0, exportedTexture.Height - 1).B >
                            exportedTexture.GetPixel(0, exportedTexture.Height - 1).R,
                    "The bottom of the embedded glTF image should remain the source image's bottom.");
            }

            var texCoordAccessorIndex = root.GetProperty("meshes")[0].GetProperty("primitives")[0]
                .GetProperty("attributes").GetProperty("TEXCOORD_0").GetInt32();
            var accessor = root.GetProperty("accessors")[texCoordAccessorIndex];
            var texCoordView = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
            var texCoordOffset = binaryOffset + GetOptionalInt(texCoordView, "byteOffset") +
                                 GetOptionalInt(accessor, "byteOffset");
            Assert.Equal(0f, BitConverter.ToSingle(bytes, texCoordOffset + sizeof(float)));
            Assert.Equal(1f, BitConverter.ToSingle(bytes, texCoordOffset + sizeof(float) * 5));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static int GetOptionalInt(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) ? property.GetInt32() : 0;
}
