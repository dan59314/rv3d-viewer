using System.Numerics;
using System.Text.Json;
using Rv3dViewer.Core;

namespace Rv3dViewer.RVInteriorDesignPlugin;

internal static class RVInteriorDesignTestSceneFactory
{
    public const string ExtensionKey = "rv3dviewer.rv-interior-design";
    public const string TestSceneName = "RV室內設計－階段一測試場景";
    public const string TestLightName = "RV室內設計－暖色測試燈";

    public static (SceneModel Model, SceneLight Light) Create()
    {
        var meshes = new List<MeshData>
        {
            CreateBox("木地板", new Vector3(0f, -.04f, 0f), new Vector3(6f, .08f, 4f), 0),
            CreateBox("後牆", new Vector3(0f, 1.4f, -2f), new Vector3(6f, 2.8f, .12f), 1),
            CreateBox("左牆", new Vector3(-3f, 1.4f, 0f), new Vector3(.12f, 2.8f, 4f), 1),
            CreateBox("測試桌", new Vector3(0f, .375f, 0f), new Vector3(1.6f, .75f, .8f), 2)
        };
        var model = new SceneModel
        {
            Name = TestSceneName,
            IsProcedural = true,
            Materials = CreateMaterials(),
            Meshes = meshes,
            Nodes =
            [
                new SceneNode
                {
                    Name = "RV室內設計測試房間",
                    MeshIndices = Enumerable.Range(0, meshes.Count).ToList()
                }
            ],
            SourceMeshIndices = Enumerable.Range(0, meshes.Count).ToList(),
            SourceMeshIndicesCaptured = true
        };
        model.Extensions[ExtensionKey] = JsonSerializer.SerializeToElement(new
        {
            version = 1,
            documentType = "phase-one-test",
            room = new { width = 6f, depth = 4f, height = 2.8f, wallThickness = .12f },
            objects = new[] { new { kind = "table", x = 0f, y = 0f, z = 0f } }
        });
        model.CaptureMeshMaterialIndices();
        model.CaptureProceduralGeometry();

        var light = new SceneLight
        {
            Name = TestLightName,
            Type = SceneLightType.Point,
            Position = new Vector3(0f, 2.45f, .5f),
            Color = new Vector3(1f, .78f, .52f),
            Intensity = 5f,
            Range = 9f
        };
        return (model, light);
    }

    public static void RefreshProjectExtension(ViewerProject project)
    {
        var modelIds = project.Models
            .Where(model => model.Extensions.ContainsKey(ExtensionKey))
            .Select(model => model.Id)
            .ToArray();
        project.Extensions[ExtensionKey] = JsonSerializer.SerializeToElement(new
        {
            version = 1,
            documents = new[]
            {
                new { name = TestSceneName, modelIds, lightName = TestLightName }
            }
        });
    }

    private static List<PbrMaterial> CreateMaterials() =>
    [
        new()
        {
            Name = "RV淺色木地板",
            BaseColor = new Vector4(.52f, .31f, .15f, 1f),
            Roughness = .58f,
            TextureStacks = new Dictionary<TextureSemantic, TextureStack>
            {
                [TextureSemantic.BaseColor] = new()
                {
                    Semantic = TextureSemantic.BaseColor,
                    Layers =
                    [
                        new TextureLayer
                        {
                            Name = "程序化木地板基底",
                            Kind = TextureLayerKind.Checker,
                            Color = new Vector4(.62f, .39f, .19f, 1f),
                            Mapping = new TextureMappingSettings { Mode = TextureMappingMode.Planar },
                            Transform = new TextureTransformSettings { ScaleX = 6f, ScaleY = 4f }
                        }
                    ]
                }
            }
        },
        new() { Name = "RV暖白牆漆", BaseColor = new Vector4(.86f, .83f, .75f, 1f), Roughness = .84f },
        new() { Name = "RV胡桃木桌", BaseColor = new Vector4(.32f, .14f, .06f, 1f), Roughness = .5f }
    ];

    private static MeshData CreateBox(string name, Vector3 center, Vector3 size, int materialIndex)
    {
        var half = size / 2f;
        var min = center - half;
        var max = center + half;
        var positions = new[]
        {
            new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, max.Y, min.Z), new Vector3(min.X, max.Y, min.Z),
            new Vector3(max.X, min.Y, max.Z), new Vector3(min.X, min.Y, max.Z), new Vector3(min.X, max.Y, max.Z), new Vector3(max.X, max.Y, max.Z),
            new Vector3(min.X, min.Y, max.Z), new Vector3(min.X, min.Y, min.Z), new Vector3(min.X, max.Y, min.Z), new Vector3(min.X, max.Y, max.Z),
            new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, min.Y, max.Z), new Vector3(max.X, max.Y, max.Z), new Vector3(max.X, max.Y, min.Z),
            new Vector3(min.X, max.Y, min.Z), new Vector3(max.X, max.Y, min.Z), new Vector3(max.X, max.Y, max.Z), new Vector3(min.X, max.Y, max.Z),
            new Vector3(min.X, min.Y, max.Z), new Vector3(max.X, min.Y, max.Z), new Vector3(max.X, min.Y, min.Z), new Vector3(min.X, min.Y, min.Z)
        };
        var normals = new[]
        {
            -Vector3.UnitZ, Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY
        }.SelectMany(normal => Enumerable.Repeat(normal, 4)).ToArray();
        var textureCoordinates = Enumerable.Range(0, 6).SelectMany(_ => new[]
        {
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f)
        }).ToArray();
        var indices = Enumerable.Range(0, 6).SelectMany(face =>
        {
            var offset = (uint)(face * 4);
            return new[] { offset, offset + 1, offset + 2, offset, offset + 2, offset + 3 };
        }).ToArray();
        return new MeshData
        {
            Name = name,
            MaterialIndex = materialIndex,
            Positions = positions,
            Normals = normals,
            TextureCoordinates = textureCoordinates,
            Tangents = new Vector4[positions.Length],
            Indices = indices
        };
    }
}
