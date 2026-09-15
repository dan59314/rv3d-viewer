using System.Drawing;
using System.Numerics;
using Assimp;
using Rv3dViewer.App;
using Rv3dViewer.Core;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class ModelExporterTests
{
    [Fact]
    public async Task ObjExporter_WritesModelMaterialAndTexturesInsideNamedFolder()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"rv3d-obj-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var texturePath = Path.Combine(directory, "source.png");
            using (var texture = new Bitmap(16, 16))
            {
                for (var y = 0; y < texture.Height; y++)
                for (var x = 0; x < texture.Width; x++)
                    texture.SetPixel(x, y, y < texture.Height / 2 ? Color.Red : Color.Blue);
                texture.Save(texturePath);
            }

            var material = new PbrMaterial
            {
                Name = "Copper Finish",
                BaseColor = new Vector4(0.9f, 0.4f, 0.15f, 1f),
                Metallic = 0.8f,
                Roughness = 0.2f
            };
            material.GetOrCreateTextureStack(TextureSemantic.BaseColor).Layers.Add(new TextureLayer
            {
                Name = "Color",
                Path = texturePath
            });
            var project = CreateProject(material);
            project.Models[0].HiddenMeshIndices = [1];
            var requestedPath = Path.Combine(directory, "Export Scene.obj");

            await ObjExporter.ExportAsync(project, requestedPath);

            var outputDirectory = Path.Combine(directory, "Export Scene");
            var objPath = Path.Combine(outputDirectory, "Export Scene.obj");
            var mtlPath = Path.Combine(outputDirectory, "Export Scene.mtl");
            var exportedTexturePath = Path.Combine(outputDirectory, "Textures", "Copper_Finish_BaseColor.png");
            Assert.False(File.Exists(requestedPath));
            Assert.True(File.Exists(objPath));
            Assert.True(File.Exists(mtlPath));
            Assert.True(File.Exists(exportedTexturePath));

            var obj = await File.ReadAllTextAsync(objPath);
            var mtl = await File.ReadAllTextAsync(mtlPath);
            Assert.Contains("mtllib \"Export Scene.mtl\"", obj);
            Assert.Single(obj.Split('\n'), line => line.StartsWith("f ", StringComparison.Ordinal));
            Assert.Contains("newmtl Copper_Finish", mtl);
            Assert.Contains("Pm 0.8", mtl);
            Assert.Contains("Pr 0.2", mtl);
            Assert.Contains("map_Kd Textures/Copper_Finish_BaseColor.png", mtl);
            using (var exportedTexture = new Bitmap(exportedTexturePath))
            {
                Assert.True(exportedTexture.GetPixel(0, 0).R > exportedTexture.GetPixel(0, 0).B);
                Assert.True(exportedTexture.GetPixel(0, exportedTexture.Height - 1).B >
                            exportedTexture.GetPixel(0, exportedTexture.Height - 1).R);
            }

            using var assimp = new AssimpContext();
            var imported = assimp.ImportFile(objPath, PostProcessSteps.Triangulate);
            Assert.NotNull(imported);
            Assert.Single(imported.Meshes);
            Assert.Single(imported.Meshes[0].Faces);
            Assert.Contains(imported.Meshes[0].Vertices, vertex => Math.Abs(vertex.X - 2f) < 0.0001f &&
                                                                 Math.Abs(vertex.Y - 3f) < 0.0001f &&
                                                                 Math.Abs(vertex.Z - 4f) < 0.0001f);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task StlExporter_WritesOnlyVisibleTrianglesWithWorldTransform()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"rv3d-stl-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "scene.stl");
            var project = CreateProject(new PbrMaterial());
            project.Models[0].HiddenMeshIndices = [1];

            await StlExporter.ExportAsync(project, path);

            var bytes = await File.ReadAllBytesAsync(path);
            Assert.Equal(134, bytes.Length);
            Assert.Equal(1u, BitConverter.ToUInt32(bytes, 80));
            Assert.Equal(2f, BitConverter.ToSingle(bytes, 96));
            Assert.Equal(3f, BitConverter.ToSingle(bytes, 100));
            Assert.Equal(4f, BitConverter.ToSingle(bytes, 104));

            using var assimp = new AssimpContext();
            var imported = assimp.ImportFile(path, PostProcessSteps.Triangulate);
            Assert.NotNull(imported);
            Assert.Single(imported.Meshes);
            Assert.Single(imported.Meshes[0].Faces);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("obj")]
    [InlineData("stl")]
    public async Task Exporters_RejectSceneWithoutVisibleMesh(string format)
    {
        var path = Path.Combine(Path.GetTempPath(), $"rv3d-empty-{Guid.NewGuid():N}.{format}");
        var project = new ViewerProject { Models = [new SceneModel { IsVisible = false }] };

        var exception = format == "obj"
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => ObjExporter.ExportAsync(project, path))
            : await Assert.ThrowsAsync<InvalidOperationException>(() => StlExporter.ExportAsync(project, path));

        Assert.Contains("可見 Mesh", exception.Message);
        Assert.False(File.Exists(path));
        if (format == "obj") Assert.False(Directory.Exists(ObjExporter.GetOutputDirectory(path)));
    }

    private static ViewerProject CreateProject(PbrMaterial material)
    {
        var visible = new MeshData
        {
            Name = "Visible",
            Positions = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
            Normals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            TextureCoordinates = [Vector2.Zero, Vector2.UnitX, Vector2.UnitY],
            Indices = [0, 1, 2]
        };
        var hidden = new MeshData
        {
            Name = "Hidden",
            Positions = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
            Indices = [0, 1, 2]
        };
        return new ViewerProject
        {
            Models =
            [
                new SceneModel
                {
                    Name = "Model",
                    Materials = [material],
                    Meshes = [visible, hidden],
                    Transform = new TransformState { Position = new Vector3(2f, 3f, 4f) }
                }
            ]
        };
    }
}
