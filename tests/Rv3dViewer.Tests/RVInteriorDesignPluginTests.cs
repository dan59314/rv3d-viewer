using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using Rv3dViewer.Core;
using Rv3dViewer.Plugin.WinForms;
using Rv3dViewer.RVInteriorDesignPlugin;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class RVInteriorDesignPluginTests
{
    [Fact]
    public void DisplayMenu_ProvidesCheckableModelEdgeCommand()
    {
        using var menu = new UnifiedPluginMenuStrip();
        bool? changed = null;
        menu.SetAdditionalDisplayToggles([new PluginDisplayToggleCommand("框線", true, value => changed = value)]);

        var display = Assert.IsType<ToolStripMenuItem>(menu.Items.Cast<ToolStripItem>()
            .Single(item => item.Text == "顯示"));
        var edges = Assert.IsType<ToolStripMenuItem>(display.DropDownItems.Cast<ToolStripItem>()
            .Single(item => item.Text == "框線"));
        Assert.True(edges.Checked);

        edges.Checked = false;
        Assert.False(changed);
    }

    [Fact]
    public void DefaultInteriorScene_ContainsEverySupportedBasicObject()
    {
        var objects = DefaultInteriorSceneFactory.Create();

        Assert.Equal(14, objects.Count);
        Assert.Equal(14, objects.Select(item => item.Id).Distinct().Count());
        Assert.All(objects, item =>
        {
            Assert.True(item.IsDefaultSceneObject);
            Assert.Equal(item.Id, item.Model.Id);
            Assert.NotEmpty(item.Model.Meshes);
        });
        Assert.Equal(Enum.GetValues<ParametricPrimitiveKind>().OrderBy(kind => kind),
            objects.Select(item => item.Parameters).OfType<ParametricPrimitiveParameters>()
                .Select(parameters => parameters.Kind).OrderBy(kind => kind));
        Assert.Single(objects, item => item.Parameters is ParametricRoomParameters);
        Assert.Single(objects, item => item.Parameters is ParametricWallParameters);
        Assert.Equal(2, objects.Count(item => item.Parameters is ParametricSlabParameters));
        Assert.Single(objects, item => item.Parameters is ParametricBeamParameters);
        Assert.Equal(2, objects.Count(item => item.Parameters is ParametricColumnParameters));
        Assert.Equal(Enum.GetValues<ParametricColumnShape>().OrderBy(shape => shape),
            objects.Select(item => item.Parameters).OfType<ParametricColumnParameters>()
                .Select(parameters => parameters.Shape).OrderBy(shape => shape));

        var room = Assert.Single(objects, item => item.Parameters is ParametricRoomParameters);
        Assert.Contains(2, room.Model.HiddenMeshIndices);
        Assert.Contains(4, room.Model.HiddenMeshIndices);
        var openingWall = Assert.IsType<ParametricWallParameters>(
            Assert.Single(objects, item => item.Parameters is ParametricWallParameters).Parameters);
        Assert.True(openingWall.DoorOpening.Enabled);
        Assert.True(openingWall.WindowOpening.Enabled);
    }

    [Theory]
    [InlineData(InteriorViewportKind.Top)]
    [InlineData(InteriorViewportKind.Front)]
    [InlineData(InteriorViewportKind.Right)]
    [InlineData(InteriorViewportKind.Perspective)]
    public void ViewportPreview_ProjectsGeneratedModelsInEveryView(InteriorViewportKind viewKind)
    {
        var model = new ParametricRoomParameters().Generate();
        using var viewport = new InteriorViewportControl { ViewKind = viewKind };

        viewport.SetSceneModels([model]);
        viewport.SetSelectedModel(model.Id);

        Assert.Equal(1, viewport.RenderedModelCount);
        Assert.True(viewport.GetProjectedTriangleCount(new Size(500, 360)) > 0);
    }

    [Fact]
    public void PerspectivePreview_DepthBufferKeepsNearSurfaceInFrontRegardlessOfModelOrder()
    {
        using var viewport = new InteriorViewportControl { ViewKind = InteriorViewportKind.Perspective };
        var far = new ParametricPrimitiveParameters
        {
            Name = "遠端平面", Kind = ParametricPrimitiveKind.Plane, Width = 3f, Depth = 3f
        }.Generate();
        far.Materials[0].BaseColor = new Vector4(0f, 0f, 1f, 1f);
        var near = new ParametricPrimitiveParameters
        {
            Name = "近端平面", Kind = ParametricPrimitiveKind.Plane, Width = 3f, Depth = 3f
        }.Generate();
        near.Materials[0].BaseColor = new Vector4(1f, 0f, 0f, 1f);
        var towardCamera = Vector3.Normalize(viewport.Camera.From - viewport.Camera.To);
        near.Transform.Position = towardCamera;

        viewport.SetSceneModels([near, far]);
        using var firstOrder = viewport.RenderSceneBitmapForTest(new Size(420, 320));
        viewport.SetSceneModels([far, near]);
        using var secondOrder = viewport.RenderSceneBitmapForTest(new Size(420, 320));

        var sample = new Point(firstOrder.Width / 2, firstOrder.Height / 2);
        var firstColor = firstOrder.GetPixel(sample.X, sample.Y);
        var secondColor = secondOrder.GetPixel(sample.X, sample.Y);
        Assert.True(firstColor.R > firstColor.B, $"First order center color: {firstColor}");
        Assert.True(secondColor.R > secondColor.B, $"Second order center color: {secondColor}");
    }

    [Fact]
    public void TransparentOccluderMode_RevealsOpaqueFurnitureBehindWall()
    {
        using var viewport = new InteriorViewportControl
        {
            ViewKind = InteriorViewportKind.Front,
            VisibilityMode = InteriorVisibilityMode.TransparentOccluders,
            ShowModelEdges = false
        };
        var furniture = new ParametricPrimitiveParameters
        {
            Name = "紅色家具", Kind = ParametricPrimitiveKind.Box, Width = 2f, Height = 2f, Depth = 2f
        }.Generate();
        furniture.Materials[0].BaseColor = new Vector4(1f, 0f, 0f, 1f);
        var wall = new ParametricWallParameters
        {
            Name = "前景牆", StartX = -2f, EndX = 2f, StartZ = .7f, EndZ = .7f,
            Height = 2.8f, BaseElevation = -1.4f
        }.Generate();
        viewport.SetSceneModels([furniture, wall]);

        using var bitmap = viewport.RenderSceneBitmapForTest(new Size(420, 320));
        var color = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);

        Assert.Equal(255, color.A);
        Assert.True(color.R > color.G * 2, $"Transparent wall did not reveal red furniture: {color}");
    }

    [Fact]
    public void AutoHideMode_RemovesForegroundWallButSolidModeKeepsIt()
    {
        using var viewport = new InteriorViewportControl { ViewKind = InteriorViewportKind.Front };
        var wall = new ParametricWallParameters
        {
            Name = "前景牆", StartX = -2f, EndX = 2f, StartZ = 1f, EndZ = 1f,
            Height = 2.8f, BaseElevation = -1.4f
        }.Generate();
        viewport.SetSceneModels([wall]);

        viewport.VisibilityMode = InteriorVisibilityMode.Solid;
        var solidCount = viewport.GetProjectedTriangleCount(new Size(420, 320));
        viewport.VisibilityMode = InteriorVisibilityMode.AutoHideForegroundWalls;
        var autoHideCount = viewport.GetProjectedTriangleCount(new Size(420, 320));

        Assert.True(solidCount > 0);
        Assert.Equal(0, autoHideCount);
    }

    [Fact]
    public void InteriorDesigner_HasNoInPluginMainFormExportButtons()
    {
        using var form = new RVInteriorDesignForm();
        var buttonTexts = DescendantControls(form).OfType<Button>().Select(button => button.Text).ToArray();

        Assert.DoesNotContain(buttonTexts, text => text.Contains("匯入", StringComparison.Ordinal));
        Assert.DoesNotContain(buttonTexts, text => text.Contains("MainForm", StringComparison.Ordinal));
    }

    private static IEnumerable<Control> DescendantControls(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            yield return control;
            foreach (var descendant in DescendantControls(control))
                yield return descendant;
        }
    }

    [Fact]
    public void ParametricRoom_CreatesFourWallsFloorCeilingMaterialsAndPortableParameters()
    {
        var parameters = new ParametricRoomParameters
        {
            Width = 6f,
            Depth = 4f,
            Height = 2.8f,
            WallThickness = .12f,
            FloorThickness = .1f,
            CeilingThickness = .08f
        };

        var model = parameters.Generate();

        Assert.Equal(6, model.Meshes.Count);
        Assert.Equal(3, model.Materials.Count);
        Assert.Contains(model.Meshes, mesh => mesh.Name == "地板" && mesh.MaterialIndex == 0);
        Assert.Equal(4, model.Meshes.Count(mesh => mesh.Name.EndsWith("牆") && mesh.MaterialIndex == 1));
        Assert.Contains(model.Meshes, mesh => mesh.Name == "天花板" && mesh.MaterialIndex == 2);
        Assert.Equal(model.Meshes.Count, model.EmbeddedMeshes.Count);
        Assert.Equal("parametric-room", model.Extensions[RVInteriorDesignTestSceneFactory.ExtensionKey]
            .GetProperty("documentType").GetString());
        Assert.Equal(6f, model.Extensions[RVInteriorDesignTestSceneFactory.ExtensionKey]
            .GetProperty("parameters").GetProperty("Width").GetSingle());
    }

    [Fact]
    public void ParametricWall_UsesStartEndLengthThicknessAndElevation()
    {
        var parameters = new ParametricWallParameters
        {
            StartX = 0f,
            StartZ = 0f,
            EndX = 3f,
            EndZ = 4f,
            Height = 2.8f,
            Thickness = .12f,
            BaseElevation = .25f
        };

        var mesh = Assert.Single(parameters.Generate().Meshes);
        var center = new Vector3(1.5f, 1.65f, 2f);
        var direction = Vector3.Normalize(new Vector3(3f, 0f, 4f));
        var perpendicular = new Vector3(-direction.Z, 0f, direction.X);
        var along = mesh.Positions.Select(position => Vector3.Dot(position - center, direction)).ToArray();
        var across = mesh.Positions.Select(position => Vector3.Dot(position - center, perpendicular)).ToArray();

        Assert.Equal(5f, along.Max() - along.Min(), 4);
        Assert.Equal(.12f, across.Max() - across.Min(), 4);
        Assert.Equal(.25f, mesh.Positions.Min(position => position.Y), 4);
        Assert.Equal(3.05f, mesh.Positions.Max(position => position.Y), 4);
    }

    [Fact]
    public void ParametricBeam_UsesStartEndCrossSectionAndElevation()
    {
        var parameters = new ParametricBeamParameters
        {
            StartX = 0f,
            StartZ = 0f,
            EndX = 3f,
            EndZ = 4f,
            Width = .2f,
            Height = .35f,
            BaseElevation = 2.2f
        };

        var model = parameters.Generate();
        var mesh = Assert.Single(model.Meshes);
        var center = new Vector3(1.5f, 2.375f, 2f);
        var direction = Vector3.Normalize(new Vector3(3f, 0f, 4f));
        var perpendicular = new Vector3(-direction.Z, 0f, direction.X);
        var along = mesh.Positions.Select(position => Vector3.Dot(position - center, direction)).ToArray();
        var across = mesh.Positions.Select(position => Vector3.Dot(position - center, perpendicular)).ToArray();

        Assert.Equal(5f, along.Max() - along.Min(), 4);
        Assert.Equal(.2f, across.Max() - across.Min(), 4);
        Assert.Equal(2.2f, mesh.Positions.Min(position => position.Y), 4);
        Assert.Equal(2.55f, mesh.Positions.Max(position => position.Y), 4);
        Assert.Equal("parametric-beam", model.Extensions[RVInteriorDesignTestSceneFactory.ExtensionKey]
            .GetProperty("documentType").GetString());
    }

    [Fact]
    public void ParametricColumn_CreatesRectangularAndCircularSections()
    {
        var rectangular = new ParametricColumnParameters
        {
            Name = "矩形柱", Shape = ParametricColumnShape.Rectangular,
            PositionX = 1f, PositionZ = -2f, Width = .4f, Depth = .6f,
            Height = 2.8f, BaseElevation = .1f
        }.Generate();
        var circular = new ParametricColumnParameters
        {
            Name = "圓柱", Shape = ParametricColumnShape.Circular,
            Diameter = .5f, Height = 2.5f, Segments = 24
        }.Generate();

        var box = Assert.Single(rectangular.Meshes);
        Assert.Equal(.4f, box.Positions.Max(point => point.X) - box.Positions.Min(point => point.X), 4);
        Assert.Equal(.6f, box.Positions.Max(point => point.Z) - box.Positions.Min(point => point.Z), 4);
        Assert.Equal(.1f, box.Positions.Min(point => point.Y), 4);
        var cylinder = Assert.Single(circular.Meshes);
        Assert.Equal(cylinder.Positions.Length, cylinder.Normals.Length);
        Assert.Equal(cylinder.Positions.Length, cylinder.TextureCoordinates.Length);
        Assert.Equal(.5f, cylinder.Positions.Max(point => point.X) - cylinder.Positions.Min(point => point.X), 4);
        Assert.Equal("parametric-column", circular.Extensions[RVInteriorDesignTestSceneFactory.ExtensionKey]
            .GetProperty("documentType").GetString());
    }

    [Fact]
    public void StructuralParameters_RejectInvalidBeamAndCircularColumn()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParametricBeamParameters
        {
            StartX = 1f, StartZ = 1f, EndX = 1f, EndZ = 1f
        }.Generate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParametricColumnParameters
        {
            Shape = ParametricColumnShape.Circular, Diameter = 0f
        }.Generate());
    }

    [Fact]
    public void ParametricWall_CutsDoorAndWindowOpeningsIntoRealMeshSegments()
    {
        var parameters = new ParametricWallParameters
        {
            Name = "門窗測試牆",
            StartX = 0f,
            EndX = 6f,
            Height = 2.8f,
            DoorOpening = new WallOpeningParameters
            {
                Enabled = true, Kind = WallOpeningKind.Door, OffsetFromStart = .5f,
                Width = 1f, Height = 2.1f
            },
            WindowOpening = new WallOpeningParameters
            {
                Enabled = true, Kind = WallOpeningKind.Window, OffsetFromStart = 3f,
                Width = 1.5f, Height = 1f, SillHeight = .9f
            }
        };

        var model = parameters.Generate();

        Assert.Equal(6, model.Meshes.Count);
        Assert.All(model.Meshes, mesh => Assert.Equal(mesh.Positions.Length, mesh.TextureCoordinates.Length));
        AssertNoMeshOccupiesOpening(model, .5f, 1.5f, 0f, 2.1f);
        AssertNoMeshOccupiesOpening(model, 3f, 4.5f, .9f, 1.9f);
        var extension = model.Extensions[RVInteriorDesignTestSceneFactory.ExtensionKey];
        Assert.Equal(2, extension.GetProperty("parameters").GetProperty("openings").GetArrayLength());
    }

    [Fact]
    public void ParametricWall_RejectsOverlappingOrOutOfBoundsOpenings()
    {
        var overlapping = new ParametricWallParameters
        {
            StartX = 0f,
            EndX = 4f,
            DoorOpening = new WallOpeningParameters
            {
                Enabled = true, Kind = WallOpeningKind.Door, OffsetFromStart = .5f,
                Width = 1.2f, Height = 2.1f
            },
            WindowOpening = new WallOpeningParameters
            {
                Enabled = true, Kind = WallOpeningKind.Window, OffsetFromStart = 1f,
                Width = 1.2f, Height = 1.1f, SillHeight = .9f
            }
        };
        Assert.Throws<ArgumentException>(() => overlapping.Generate());

        overlapping.WindowOpening.OffsetFromStart = 3.5f;
        Assert.Throws<ArgumentOutOfRangeException>(() => overlapping.Generate());
    }

    private static void AssertNoMeshOccupiesOpening(SceneModel model, float left, float right,
        float bottom, float top)
    {
        const float epsilon = .0001f;
        foreach (var mesh in model.Meshes)
        {
            var minX = mesh.Positions.Min(position => position.X);
            var maxX = mesh.Positions.Max(position => position.X);
            var minY = mesh.Positions.Min(position => position.Y);
            var maxY = mesh.Positions.Max(position => position.Y);
            var overlapsX = minX < right - epsilon && maxX > left + epsilon;
            var overlapsY = minY < top - epsilon && maxY > bottom + epsilon;
            Assert.False(overlapsX && overlapsY, $"Mesh {mesh.Name} overlaps the opening.");
        }
    }

    [Theory]
    [InlineData(ParametricSlabKind.Floor, 0f)]
    [InlineData(ParametricSlabKind.Ceiling, 2.8f)]
    public void ParametricSlab_UsesElevationAsFinishedSurface(ParametricSlabKind kind, float elevation)
    {
        var parameters = new ParametricSlabParameters
        {
            Kind = kind,
            Elevation = elevation,
            Thickness = .1f
        };

        var mesh = Assert.Single(parameters.Generate().Meshes);
        var surface = kind == ParametricSlabKind.Floor
            ? mesh.Positions.Max(position => position.Y)
            : mesh.Positions.Min(position => position.Y);
        Assert.Equal(elevation, surface, 4);
    }

    [Fact]
    public void ParametricWall_RejectsCoincidentStartAndEnd()
    {
        var parameters = new ParametricWallParameters
        {
            StartX = 1f,
            StartZ = 2f,
            EndX = 1f,
            EndZ = 2f
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Generate());
    }

    [Fact]
    public void ViewportMouseDrag_UsesCameraAnimationNavigationMapping()
    {
        using var viewport = new InteriorViewportControl { ViewKind = InteriorViewportKind.Perspective };
        var initialFrom = viewport.Camera.From;
        var initialTarget = viewport.Camera.To;

        Assert.True(viewport.NavigateDrag(MouseButtons.Left, 12, -7));
        Assert.NotEqual(initialFrom, viewport.Camera.From);
        Assert.Equal(initialTarget, viewport.Camera.To);
        Assert.Equal("Orbit", viewport.LastNavigationOperation);

        var beforeTargetMoveFrom = viewport.Camera.From;
        var beforeTargetMove = viewport.Camera.To;
        Assert.True(viewport.NavigateDrag(MouseButtons.Middle, 8, 5));
        Assert.Equal(beforeTargetMoveFrom, viewport.Camera.From);
        Assert.NotEqual(beforeTargetMove, viewport.Camera.To);

        var beforePanFrom = viewport.Camera.From;
        var beforePanTarget = viewport.Camera.To;
        Assert.True(viewport.NavigateDrag(MouseButtons.Right, -9, 4));
        var fromDelta = viewport.Camera.From - beforePanFrom;
        var targetDelta = viewport.Camera.To - beforePanTarget;
        Assert.True(Vector3.Distance(fromDelta, targetDelta) < .00001f);
    }

    [Theory]
    [InlineData(InteriorViewportKind.Top)]
    [InlineData(InteriorViewportKind.Front)]
    [InlineData(InteriorViewportKind.Right)]
    public void OrthographicViewport_LeftDragDoesNotOrbit(InteriorViewportKind viewKind)
    {
        using var viewport = new InteriorViewportControl { ViewKind = viewKind };
        var initialFrom = viewport.Camera.From;
        var initialTarget = viewport.Camera.To;
        var initialUp = viewport.Camera.Up;

        Assert.False(viewport.NavigateDrag(MouseButtons.Left, 30, -20));
        Assert.Equal(initialFrom, viewport.Camera.From);
        Assert.Equal(initialTarget, viewport.Camera.To);
        Assert.Equal(initialUp, viewport.Camera.Up);
    }

    [Fact]
    public void ViewportMouseWheel_DolliesOrAdjustsFovOrRoll()
    {
        using var viewport = new InteriorViewportControl { ViewKind = InteriorViewportKind.Perspective };
        var initialDistance = Vector3.Distance(viewport.Camera.From, viewport.Camera.To);

        viewport.NavigateWheel(120, Keys.None);
        Assert.True(Vector3.Distance(viewport.Camera.From, viewport.Camera.To) < initialDistance);

        var initialFov = viewport.Camera.FieldOfViewDegrees;
        viewport.NavigateWheel(120, Keys.Control);
        Assert.Equal(initialFov - 2f, viewport.Camera.FieldOfViewDegrees);

        var initialRoll = viewport.Camera.RollDegrees;
        viewport.NavigateWheel(120, Keys.Shift);
        Assert.Equal(initialRoll + 2f, viewport.Camera.RollDegrees);
    }

    [Fact]
    public void OrthographicViewportScaleBar_UsesMeterGridAndNeverAppearsInPerspective()
    {
        Assert.Equal(.2f, InteriorViewportControl.GridSpacingMeters);

        using var viewport = new InteriorViewportControl { ShowScaleBar = true, ViewKind = InteriorViewportKind.Top };
        Assert.True(viewport.IsScaleBarVisible);
        viewport.ViewKind = InteriorViewportKind.Perspective;
        Assert.False(viewport.IsScaleBarVisible);
    }

    [Fact]
    public void PerspectiveGridProjection_TracksDollyAndFov()
    {
        using var viewport = new InteriorViewportControl { ViewKind = InteriorViewportKind.Perspective };
        var initial = viewport.GetGridMetrics(new Size(640, 480));

        viewport.NavigateWheel(120, Keys.None);
        var afterDolly = viewport.GetGridMetrics(new Size(640, 480));
        Assert.True(afterDolly.PixelsPerMeter > initial.PixelsPerMeter);

        viewport.NavigateWheel(120, Keys.Control);
        var afterFov = viewport.GetGridMetrics(new Size(640, 480));
        Assert.True(afterFov.PixelsPerMeter > afterDolly.PixelsPerMeter);
    }

    [Fact]
    public void OrthographicGridOrigin_TracksCameraPan()
    {
        using var viewport = new InteriorViewportControl { ViewKind = InteriorViewportKind.Top };
        var initial = viewport.GetGridMetrics(new Size(640, 480));

        viewport.NavigateDrag(MouseButtons.Right, 30, 18);
        var afterPan = viewport.GetGridMetrics(new Size(640, 480));

        Assert.NotEqual(initial.WorldOrigin, afterPan.WorldOrigin);
        Assert.Equal(initial.PixelsPerMeter, afterPan.PixelsPerMeter, 3);
    }

    public static TheoryData<ParametricPrimitiveKind> PrimitiveKinds => new()
    {
        ParametricPrimitiveKind.Plane,
        ParametricPrimitiveKind.Box,
        ParametricPrimitiveKind.Sphere,
        ParametricPrimitiveKind.Cylinder,
        ParametricPrimitiveKind.HalfCylinder,
        ParametricPrimitiveKind.Cone,
        ParametricPrimitiveKind.PolygonCone
    };

    [Theory]
    [MemberData(nameof(PrimitiveKinds))]
    public void ParametricPrimitiveGenerator_CreatesPortableMeshWithNormalsUvAndExtension(ParametricPrimitiveKind kind)
    {
        var parameters = new ParametricPrimitiveParameters
        {
            Name = $"測試{kind}",
            Kind = kind,
            Width = 2f,
            Depth = 3f,
            Height = 4f,
            Radius = 1.25f,
            Segments = 16,
            HeightSegments = 2,
            PolygonSides = 5
        };

        var model = ParametricPrimitiveGenerator.Generate(parameters);
        var mesh = Assert.Single(model.Meshes);

        Assert.True(model.IsProcedural);
        Assert.NotEmpty(mesh.Positions);
        Assert.NotEmpty(mesh.Indices);
        Assert.Equal(0, mesh.Indices.Length % 3);
        Assert.Equal(mesh.Positions.Length, mesh.Normals.Length);
        Assert.Equal(mesh.Positions.Length, mesh.TextureCoordinates.Length);
        Assert.Equal(mesh.Positions.Length, mesh.Tangents.Length);
        Assert.All(mesh.Indices, index => Assert.True(index < mesh.Positions.Length));
        Assert.All(mesh.Positions, position =>
            Assert.True(float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z)));
        Assert.Equal("parametric-primitive", model.Extensions[RVInteriorDesignTestSceneFactory.ExtensionKey]
            .GetProperty("documentType").GetString());
    }

    [Fact]
    public void ParametricBox_UsesRequestedDimensions()
    {
        var model = ParametricPrimitiveGenerator.Generate(new ParametricPrimitiveParameters
        {
            Name = "尺寸測試",
            Kind = ParametricPrimitiveKind.Box,
            Width = 2f,
            Depth = 3f,
            Height = 4f
        });
        var positions = model.Meshes[0].Positions;

        Assert.Equal(2f, positions.Max(p => p.X) - positions.Min(p => p.X), 4);
        Assert.Equal(4f, positions.Max(p => p.Y) - positions.Min(p => p.Y), 4);
        Assert.Equal(3f, positions.Max(p => p.Z) - positions.Min(p => p.Z), 4);
    }

    [Fact]
    public void ParametricPrimitiveGenerator_RejectsInvalidRelevantDimension()
    {
        var parameters = new ParametricPrimitiveParameters
        {
            Kind = ParametricPrimitiveKind.Sphere,
            Radius = 0f
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => ParametricPrimitiveGenerator.Generate(parameters));
    }

    [Fact]
    public void AssetCatalog_CoversEveryPlannedRightSideCategory()
    {
        var assets = InteriorAssetCatalog.CreateDefault();

        Assert.Equal(11, InteriorAssetCatalog.Categories.Count);
        Assert.All(InteriorAssetCatalog.Categories,
            category => Assert.Contains(assets, asset => asset.Category == category));
        Assert.Contains(assets, asset => asset.Name == "三人沙發");
        Assert.Contains(assets, asset => asset.Name == "直立式冰箱");
        Assert.Contains(assets, asset => asset.Name == "吸頂燈");
        Assert.All(assets, asset => Assert.False(string.IsNullOrWhiteSpace(asset.License)));
    }

    [Fact]
    public void PhaseOneTestScene_ContainsPortableGeometryMaterialsLightAndExtensionData()
    {
        var scene = RVInteriorDesignTestSceneFactory.Create();

        Assert.Equal(RVInteriorDesignTestSceneFactory.TestSceneName, scene.Model.Name);
        Assert.True(scene.Model.IsProcedural);
        Assert.Equal(4, scene.Model.Meshes.Count);
        Assert.Equal(3, scene.Model.Materials.Count);
        Assert.Equal(scene.Model.Meshes.Count, scene.Model.EmbeddedMeshes.Count);
        Assert.Equal(scene.Model.Meshes.Count, scene.Model.MeshMaterialIndices.Count);
        Assert.Equal("phase-one-test", scene.Model.Extensions[RVInteriorDesignTestSceneFactory.ExtensionKey]
            .GetProperty("documentType").GetString());
        Assert.Equal(RVInteriorDesignTestSceneFactory.TestLightName, scene.Light.Name);
        Assert.True(scene.Light.Intensity > 0f);
        Assert.Contains(scene.Model.Materials, material =>
            material.TextureStacks.Values.SelectMany(stack => stack.Layers)
                .Any(layer => layer.Kind == Rv3dViewer.Core.TextureLayerKind.Checker));
    }
}
