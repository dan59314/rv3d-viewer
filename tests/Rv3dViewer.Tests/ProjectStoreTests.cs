using System.Numerics;
using System.Text.Json;
using Rv3dViewer.Core;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class ProjectStoreTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void EnvironmentBackgroundRequiresBothFlags(bool enabled, bool showBackground, bool expected)
    {
        var environment = new EnvironmentSettings { Enabled = enabled, ShowBackground = showBackground };

        Assert.Equal(expected, environment.IsBackgroundVisible);
    }

    [Fact]
    public async Task Project_RoundTripsOpaquePluginExtensions()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rv3d-extension-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var path = Path.Combine(tempDirectory, "extension.rv3dproj");
            var model = new SceneModel { Name = "Plugin model", IsProcedural = true };
            model.Extensions["rv3dviewer.rv-interior-design"] = JsonSerializer.SerializeToElement(new
            {
                version = 1,
                primitive = new { kind = "box", width = 2.5, height = 1.2 }
            });
            var project = new ViewerProject { Models = [model] };
            project.Extensions["third-party.unknown"] = JsonSerializer.SerializeToElement(new
            {
                enabled = true,
                nested = new[] { "keep", "unchanged" }
            });

            var store = new JsonProjectStore();
            await store.SaveAsync(project, path);
            var loaded = await store.LoadAsync(path);

            Assert.True(loaded.Extensions["third-party.unknown"].GetProperty("enabled").GetBoolean());
            Assert.Equal("unchanged", loaded.Extensions["third-party.unknown"].GetProperty("nested")[1].GetString());
            Assert.Equal("box", loaded.Models[0].Extensions["rv3dviewer.rv-interior-design"]
                .GetProperty("primitive").GetProperty("kind").GetString());
            Assert.Equal(2.5, loaded.Models[0].Extensions["rv3dviewer.rv-interior-design"]
                .GetProperty("primitive").GetProperty("width").GetDouble());
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public async Task Project_RoundTripsPortableStateWithoutGpuGeometry()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rv3d-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var path = Path.Combine(tempDirectory, "sample.rv3dproj");
            var project = new ViewerProject
            {
                Name = "Sample",
                Camera = new CameraState { From = new Vector3(1, 2, 3), To = new Vector3(4, 5, 6), RollDegrees = 25f, FieldOfViewDegrees = 45 },
                RenderSettings = new RenderSettings
                {
                    ShowTextures = false,
                    ShowGrid = false,
                    ShowSelectionHighlight = false,
                    ModelDisplayMode = ModelDisplayMode.Points
                },
                Environment = new EnvironmentSettings
                {
                    Enabled = true,
                    Path = "Assets/Environment/studio.hdr",
                    Intensity = 2.5f,
                    RotationDegrees = 45f,
                    ShowBackground = false,
                    BackgroundBlur = 0.3f
                },
                Skyboxes =
                [
                    new SkyboxSettings
                    {
                        Name = "Studio Cube",
                        Show = true,
                        UseAsEnvironment = true,
                        RotationDegrees = 30f,
                        PositiveX = "Assets/Skybox/Studio Cube/px.png",
                        NegativeX = "Assets/Skybox/Studio Cube/nx.png",
                        PositiveY = "Assets/Skybox/Studio Cube/py.png",
                        NegativeY = "Assets/Skybox/Studio Cube/ny.png",
                        PositiveZ = "Assets/Skybox/Studio Cube/pz.png",
                        NegativeZ = "Assets/Skybox/Studio Cube/nz.png"
                    }
                ],
                Lights =
                [
                    new SceneLight
                    {
                        Name = "Test Light",
                        Type = SceneLightType.Spot,
                        Position = new Vector3(3, 4, 5),
                        Direction = new Vector3(1, -2, 3),
                        Color = new Vector3(0.5f, 0.75f, 1f),
                        Intensity = 6f,
                        Range = 12f,
                        FallInDegrees = 18f,
                        FallOffDegrees = 32f
                    }
                ],
                MaterialLibrary =
                [
                    new PbrMaterial
                    {
                        Name = "Polished Copper",
                        BaseColor = new Vector4(0.95f, 0.45f, 0.2f, 1f),
                        Metallic = 1f,
                        Roughness = 0.12f,
                        Textures = new Dictionary<TextureSemantic, TextureSlot>
                        {
                            [TextureSemantic.Normal] = new() { Path = "Assets/Textures/copper-normal.png" }
                        }
                    }
                ]
            };
            project.Models.Add(new SceneModel
            {
                Name = "Cube",
                AssetPath = "Assets/Models/cube.obj",
                HiddenMeshIndices = [1, 3],
                SourceMeshIndices = [0],
                MeshTransforms = new Dictionary<int, TransformState>
                {
                    [0] = new() { Position = new Vector3(2, 3, 4), RotationDegrees = new Vector3(10, 20, 30) }
                },
                Transform = new TransformState { Position = new Vector3(7, 8, 9) },
                Meshes = [new MeshData { Positions = [Vector3.One], Indices = [0, 0, 0], MaterialIndex = 1 }],
                Materials = [new PbrMaterial { Name = "Unused" }, new PbrMaterial
                {
                    Name = "Gold",
                    Metallic = 1f,
                    Roughness = 0.2f,
                    RenderMode = MaterialRenderMode.Glass,
                    Transmission = 0.8f,
                    IndexOfRefraction = 1.45f,
                    Thickness = 0.2f,
                    RefractionStrength = 0.06f,
                    AbsorptionColor = new Vector3(0.8f, 0.9f, 1f)
                }]
            });

            var store = new JsonProjectStore();
            await store.SaveAsync(project, path);
            var loaded = await store.LoadAsync(path);

            Assert.Equal(new Vector3(1, 2, 3), loaded.Camera.From);
            Assert.Equal(25f, loaded.Camera.RollDegrees);
            Assert.True(Math.Abs(Vector3.Dot(Vector3.Normalize(loaded.Camera.To - loaded.Camera.From), loaded.Camera.Up)) < 0.0001f);
            Assert.False(loaded.RenderSettings.ShowTextures);
            Assert.False(loaded.RenderSettings.ShowGrid);
            Assert.False(loaded.RenderSettings.ShowSelectionHighlight);
            Assert.Equal(ModelDisplayMode.Points, loaded.RenderSettings.ModelDisplayMode);
            Assert.Equal(new Vector3(7, 8, 9), loaded.Models[0].Transform.Position);
            Assert.Equal([1, 3], loaded.Models[0].HiddenMeshIndices);
            Assert.Equal([0], loaded.Models[0].SourceMeshIndices);
            Assert.Equal(new Vector3(2, 3, 4), loaded.Models[0].MeshTransforms[0].Position);
            Assert.Equal(new Vector3(10, 20, 30), loaded.Models[0].MeshTransforms[0].RotationDegrees);
            Assert.Equal("Gold", loaded.Models[0].Materials[1].Name);
            Assert.Equal(MaterialRenderMode.Glass, loaded.Models[0].Materials[1].RenderMode);
            Assert.Equal(0.8f, loaded.Models[0].Materials[1].Transmission);
            Assert.Equal(1.45f, loaded.Models[0].Materials[1].IndexOfRefraction);
            Assert.Equal(0.2f, loaded.Models[0].Materials[1].Thickness);
            Assert.Equal(0.06f, loaded.Models[0].Materials[1].RefractionStrength);
            Assert.Equal(new Vector3(0.8f, 0.9f, 1f), loaded.Models[0].Materials[1].AbsorptionColor);
            Assert.Equal(1, loaded.Models[0].MeshMaterialIndices[0]);
            Assert.Empty(loaded.Models[0].Meshes);
            Assert.Single(loaded.Lights);
            Assert.Equal("Test Light", loaded.Lights[0].Name);
            Assert.Equal(SceneLightType.Spot, loaded.Lights[0].Type);
            Assert.Equal(new Vector3(3, 4, 5), loaded.Lights[0].Position);
            Assert.Equal(6f, loaded.Lights[0].Intensity);
            Assert.Equal(12f, loaded.Lights[0].Range);
            Assert.Equal(18f, loaded.Lights[0].FallInDegrees);
            Assert.Equal(32f, loaded.Lights[0].FallOffDegrees);
            Assert.InRange(loaded.Lights[0].Direction.Length(), 0.999f, 1.001f);
            Assert.True(loaded.Environment.Enabled);
            Assert.Equal("Assets/Environment/studio.hdr", loaded.Environment.Path);
            Assert.Equal(2.5f, loaded.Environment.Intensity);
            Assert.Equal(45f, loaded.Environment.RotationDegrees);
            Assert.False(loaded.Environment.ShowBackground);
            Assert.Equal(0.3f, loaded.Environment.BackgroundBlur);
            Assert.Single(loaded.Skyboxes);
            Assert.True(loaded.Skyboxes[0].Show);
            Assert.True(loaded.Skyboxes[0].UseAsEnvironment);
            Assert.Equal(30f, loaded.Skyboxes[0].RotationDegrees);
            Assert.Empty(loaded.MaterialLibrary);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public async Task VersionTwoProject_UpgradesGlassAndTransparentMaterials()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rv3d-v2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var path = Path.Combine(tempDirectory, "legacy.rv3dproj");
            await File.WriteAllTextAsync(path,
                """
                {
                  "formatVersion": 2,
                  "models": [
                    {
                      "name": "Assembly",
                      "materials": [
                        { "name": "Tempered_Glass", "opacity": 0.4 },
                        { "name": "Cooling_Water", "opacity": 0.7 }
                      ]
                    }
                  ]
                }
                """);

            var loaded = await new JsonProjectStore().LoadAsync(path);

            Assert.Equal(MaterialRenderMode.Glass, loaded.Models[0].Materials[0].RenderMode);
            Assert.Equal(MaterialRenderMode.Transparent, loaded.Models[0].Materials[1].RenderMode);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public async Task Project_RestoresSavedMeshMaterialAssignmentsAfterGeometryImport()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"rv3d-material-map-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "mapping.rv3dproj");
            var model = new SceneModel
            {
                Materials =
                [
                    new PbrMaterial { Name = "Original" },
                    new PbrMaterial { Name = "User Applied" }
                ],
                Meshes =
                [
                    new MeshData { Name = "Body", MaterialIndex = 1 },
                    new MeshData { Name = "Cap", MaterialIndex = -1 }
                ]
            };
            await new JsonProjectStore().SaveAsync(new ViewerProject { Models = [model] }, path);

            var loaded = await new JsonProjectStore().LoadAsync(path);
            loaded.Models[0].Meshes =
            [
                new MeshData { Name = "Body", MaterialIndex = 0 },
                new MeshData { Name = "Cap", MaterialIndex = 0 }
            ];
            loaded.Models[0].RestoreMeshMaterialIndices();

            Assert.Equal(1, loaded.Models[0].Meshes[0].MaterialIndex);
            Assert.Equal(-1, loaded.Models[0].Meshes[1].MaterialIndex);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void DefaultProject_HasTwoBrightDiagonalLights()
    {
        var lights = new ViewerProject().Lights;

        Assert.Equal(2, lights.Count);
        Assert.All(lights, light => Assert.True(light.Enabled));
        Assert.True(lights.Sum(light => light.Intensity) >= 8f);
        Assert.True(Math.Sign(lights[0].Direction.X) != Math.Sign(lights[1].Direction.X));
        Assert.True(Math.Sign(lights[0].Direction.Z) != Math.Sign(lights[1].Direction.Z));
    }
}
