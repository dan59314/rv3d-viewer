namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using System.Text.Json;
using Rv3dViewer.Core;

internal static class ParametricBuildingGenerator
{
    public static SceneModel GenerateRoom(ParametricRoomParameters p, Guid? modelId = null)
    {
        ValidateName(p.Name);
        ValidatePositive(p.Width, nameof(p.Width));
        ValidatePositive(p.Depth, nameof(p.Depth));
        ValidatePositive(p.Height, nameof(p.Height));
        ValidatePositive(p.WallThickness, nameof(p.WallThickness));
        if (p.IncludeFloor) ValidatePositive(p.FloorThickness, nameof(p.FloorThickness));
        if (p.IncludeCeiling) ValidatePositive(p.CeilingThickness, nameof(p.CeilingThickness));

        var halfWidth = p.Width / 2f;
        var halfDepth = p.Depth / 2f;
        var wallCenterY = p.Height / 2f;
        var meshes = new List<MeshData>();
        if (p.IncludeFloor)
            meshes.Add(InteriorGeometryBuilder.CreateBox("地板", new Vector3(0, -p.FloorThickness / 2f, 0),
                new Vector3(p.Width + p.WallThickness * 2f, p.FloorThickness, p.Depth + p.WallThickness * 2f), 0));
        meshes.Add(InteriorGeometryBuilder.CreateBox("後牆", new Vector3(0, wallCenterY, -halfDepth - p.WallThickness / 2f),
            new Vector3(p.Width + p.WallThickness * 2f, p.Height, p.WallThickness), 1));
        meshes.Add(InteriorGeometryBuilder.CreateBox("前牆", new Vector3(0, wallCenterY, halfDepth + p.WallThickness / 2f),
            new Vector3(p.Width + p.WallThickness * 2f, p.Height, p.WallThickness), 1));
        meshes.Add(InteriorGeometryBuilder.CreateBox("左牆", new Vector3(-halfWidth - p.WallThickness / 2f, wallCenterY, 0),
            new Vector3(p.WallThickness, p.Height, p.Depth), 1));
        meshes.Add(InteriorGeometryBuilder.CreateBox("右牆", new Vector3(halfWidth + p.WallThickness / 2f, wallCenterY, 0),
            new Vector3(p.WallThickness, p.Height, p.Depth), 1));
        if (p.IncludeCeiling)
            meshes.Add(InteriorGeometryBuilder.CreateBox("天花板", new Vector3(0, p.Height + p.CeilingThickness / 2f, 0),
                new Vector3(p.Width + p.WallThickness * 2f, p.CeilingThickness, p.Depth + p.WallThickness * 2f), 2));

        return CreateModel(p.Name, modelId, meshes, CreateRoomMaterials(), "parametric-room", new
        {
            p.Name, p.Width, p.Depth, p.Height, p.WallThickness, p.FloorThickness, p.CeilingThickness,
            p.IncludeFloor, p.IncludeCeiling
        });
    }

    public static SceneModel GenerateWall(ParametricWallParameters p, Guid? modelId = null)
    {
        ValidateName(p.Name);
        ValidatePositive(p.Height, nameof(p.Height));
        ValidatePositive(p.Thickness, nameof(p.Thickness));
        var deltaX = p.EndX - p.StartX;
        var deltaZ = p.EndZ - p.StartZ;
        var length = MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        ValidatePositive(length, "Length");
        var yaw = -MathF.Atan2(deltaZ, deltaX);
        var openings = new[] { p.DoorOpening, p.WindowOpening }.Where(opening => opening.Enabled).ToArray();
        ValidateOpenings(openings, length, p.Height);
        List<MeshData> meshes = openings.Length == 0
            ?
            [
                InteriorGeometryBuilder.CreateBox(p.Name,
                    new Vector3((p.StartX + p.EndX) / 2f, p.BaseElevation + p.Height / 2f,
                        (p.StartZ + p.EndZ) / 2f), new Vector3(length, p.Height, p.Thickness), 0, yaw)
            ]
            : CreateWallSegments(p, length, deltaX / length, deltaZ / length, yaw, openings);
        if (meshes.Count == 0)
            throw new ArgumentException("門窗洞口不可移除整面牆體。", nameof(p));
        return CreateModel(p.Name, modelId, meshes, [WallMaterial()], "parametric-wall", new
        {
            p.Name, start = new { x = p.StartX, z = p.StartZ }, end = new { x = p.EndX, z = p.EndZ },
            p.Height, p.Thickness, p.BaseElevation, length,
            openings = openings.Select(opening => new
            {
                kind = opening.Kind.ToString(), opening.OffsetFromStart, opening.Width,
                opening.Height, opening.SillHeight
            }).ToArray()
        });
    }

    private static List<MeshData> CreateWallSegments(ParametricWallParameters p, float length,
        float directionX, float directionZ, float yaw, IReadOnlyList<WallOpeningParameters> openings)
    {
        var xBoundaries = new List<float> { 0f, length };
        foreach (var opening in openings)
        {
            xBoundaries.Add(opening.OffsetFromStart);
            xBoundaries.Add(opening.OffsetFromStart + opening.Width);
        }
        xBoundaries.Sort();
        var distinctX = xBoundaries.Distinct().ToArray();
        var meshes = new List<MeshData>();
        for (var xIndex = 0; xIndex < distinctX.Length - 1; xIndex++)
        {
            var x0 = distinctX[xIndex];
            var x1 = distinctX[xIndex + 1];
            if (x1 - x0 <= .00001f)
                continue;
            var xMid = (x0 + x1) / 2f;
            var activeOpenings = openings.Where(opening =>
                xMid > opening.OffsetFromStart && xMid < opening.OffsetFromStart + opening.Width).ToArray();
            var yBoundaries = new List<float> { 0f, p.Height };
            foreach (var opening in activeOpenings)
            {
                yBoundaries.Add(opening.SillHeight);
                yBoundaries.Add(opening.SillHeight + opening.Height);
            }
            yBoundaries.Sort();
            var distinctY = yBoundaries.Distinct().ToArray();
            for (var yIndex = 0; yIndex < distinctY.Length - 1; yIndex++)
            {
                var y0 = distinctY[yIndex];
                var y1 = distinctY[yIndex + 1];
                if (y1 - y0 <= .00001f)
                    continue;
                var yMid = (y0 + y1) / 2f;
                if (activeOpenings.Any(opening =>
                        yMid > opening.SillHeight && yMid < opening.SillHeight + opening.Height))
                    continue;
                var distanceFromStart = (x0 + x1) / 2f;
                var center = new Vector3(
                    p.StartX + directionX * distanceFromStart,
                    p.BaseElevation + (y0 + y1) / 2f,
                    p.StartZ + directionZ * distanceFromStart);
                meshes.Add(InteriorGeometryBuilder.CreateBox($"{p.Name}－牆段 {meshes.Count + 1}", center,
                    new Vector3(x1 - x0, y1 - y0, p.Thickness), 0, yaw));
            }
        }
        return meshes;
    }

    private static void ValidateOpenings(IReadOnlyList<WallOpeningParameters> openings, float wallLength,
        float wallHeight)
    {
        foreach (var opening in openings)
        {
            ValidatePositive(opening.Width, $"{opening.Kind}.Width");
            ValidatePositive(opening.Height, $"{opening.Kind}.Height");
            if (!float.IsFinite(opening.OffsetFromStart) || opening.OffsetFromStart < 0f ||
                opening.OffsetFromStart + opening.Width > wallLength + .00001f)
                throw new ArgumentOutOfRangeException(nameof(opening.OffsetFromStart),
                    $"{OpeningName(opening.Kind)}必須位於牆體長度範圍內。");
            if (!float.IsFinite(opening.SillHeight) || opening.SillHeight < 0f ||
                opening.SillHeight + opening.Height > wallHeight + .00001f)
                throw new ArgumentOutOfRangeException(nameof(opening.SillHeight),
                    $"{OpeningName(opening.Kind)}必須位於牆體高度範圍內。");
        }
        for (var first = 0; first < openings.Count; first++)
        for (var second = first + 1; second < openings.Count; second++)
        {
            var a = openings[first];
            var b = openings[second];
            var overlapsHorizontally = a.OffsetFromStart < b.OffsetFromStart + b.Width &&
                                       b.OffsetFromStart < a.OffsetFromStart + a.Width;
            var overlapsVertically = a.SillHeight < b.SillHeight + b.Height &&
                                     b.SillHeight < a.SillHeight + a.Height;
            if (overlapsHorizontally && overlapsVertically)
                throw new ArgumentException($"{OpeningName(a.Kind)}與{OpeningName(b.Kind)}不可重疊。");
        }
    }

    private static string OpeningName(WallOpeningKind kind) => kind == WallOpeningKind.Door ? "門洞" : "窗洞";

    public static SceneModel GenerateSlab(ParametricSlabParameters p, Guid? modelId = null)
    {
        ValidateName(p.Name);
        ValidatePositive(p.Width, nameof(p.Width));
        ValidatePositive(p.Depth, nameof(p.Depth));
        ValidatePositive(p.Thickness, nameof(p.Thickness));
        var centerY = p.Kind == ParametricSlabKind.Floor
            ? p.Elevation - p.Thickness / 2f
            : p.Elevation + p.Thickness / 2f;
        var mesh = InteriorGeometryBuilder.CreateBox(p.Name, new Vector3(0, centerY, 0),
            new Vector3(p.Width, p.Thickness, p.Depth));
        var type = p.Kind == ParametricSlabKind.Floor ? "parametric-floor" : "parametric-ceiling";
        return CreateModel(p.Name, modelId, [mesh], [p.Kind == ParametricSlabKind.Floor ? FloorMaterial() : CeilingMaterial()],
            type, new { p.Name, kind = p.Kind.ToString(), p.Width, p.Depth, p.Thickness, p.Elevation });
    }

    public static SceneModel GenerateBeam(ParametricBeamParameters p, Guid? modelId = null)
    {
        ValidateName(p.Name);
        ValidatePositive(p.Width, nameof(p.Width));
        ValidatePositive(p.Height, nameof(p.Height));
        ValidateFinite(p.BaseElevation, nameof(p.BaseElevation));
        var deltaX = p.EndX - p.StartX;
        var deltaZ = p.EndZ - p.StartZ;
        var length = MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        ValidatePositive(length, "Length");
        var yaw = -MathF.Atan2(deltaZ, deltaX);
        var mesh = InteriorGeometryBuilder.CreateBox(p.Name,
            new Vector3((p.StartX + p.EndX) / 2f, p.BaseElevation + p.Height / 2f,
                (p.StartZ + p.EndZ) / 2f),
            new Vector3(length, p.Height, p.Width), 0, yaw);
        return CreateModel(p.Name, modelId, [mesh], [StructuralMaterial()], "parametric-beam", new
        {
            p.Name, start = new { x = p.StartX, z = p.StartZ }, end = new { x = p.EndX, z = p.EndZ },
            p.Width, p.Height, p.BaseElevation, length
        });
    }

    public static SceneModel GenerateColumn(ParametricColumnParameters p, Guid? modelId = null)
    {
        ValidateName(p.Name);
        ValidatePositive(p.Height, nameof(p.Height));
        ValidateFinite(p.PositionX, nameof(p.PositionX));
        ValidateFinite(p.PositionZ, nameof(p.PositionZ));
        ValidateFinite(p.BaseElevation, nameof(p.BaseElevation));
        var center = new Vector3(p.PositionX, p.BaseElevation + p.Height / 2f, p.PositionZ);
        MeshData mesh;
        if (p.Shape == ParametricColumnShape.Circular)
        {
            ValidatePositive(p.Diameter, nameof(p.Diameter));
            mesh = InteriorGeometryBuilder.CreateCylinder(p.Name, center, p.Diameter / 2f, p.Height, p.Segments);
        }
        else
        {
            ValidatePositive(p.Width, nameof(p.Width));
            ValidatePositive(p.Depth, nameof(p.Depth));
            mesh = InteriorGeometryBuilder.CreateBox(p.Name, center, new Vector3(p.Width, p.Height, p.Depth));
        }
        return CreateModel(p.Name, modelId, [mesh], [StructuralMaterial()], "parametric-column", new
        {
            p.Name, shape = p.Shape.ToString(), position = new { x = p.PositionX, z = p.PositionZ },
            p.Width, p.Depth, p.Diameter, p.Height, p.BaseElevation, p.Segments
        });
    }

    public static SceneModel GenerateDoor(ParametricDoorParameters p, Guid? modelId = null)
    {
        ValidateName(p.Name);
        ValidatePositive(p.Width, nameof(p.Width));
        ValidatePositive(p.Height, nameof(p.Height));
        ValidatePositive(p.Thickness, nameof(p.Thickness));
        ValidatePositive(p.FrameWidth, nameof(p.FrameWidth));
        ValidatePositive(p.FrameDepth, nameof(p.FrameDepth));
        ValidateFinite(p.HandleHeight, nameof(p.HandleHeight));
        if (p.IncludeFrame && (p.FrameWidth * 2f >= p.Width || p.FrameWidth >= p.Height))
            throw new ArgumentOutOfRangeException(nameof(p.FrameWidth), "門框寬度必須小於門寬的一半及門高。");
        if (p.HandleHeight <= 0f || p.HandleHeight >= p.Height)
            throw new ArgumentOutOfRangeException(nameof(p.HandleHeight), "門把高度必須位於門扇高度範圍內。");

        var frameWidth = p.IncludeFrame ? p.FrameWidth : 0f;
        var leafWidth = p.Width - frameWidth * 2f;
        var leafHeight = p.Height - frameWidth;
        var meshes = new List<MeshData>
        {
            InteriorGeometryBuilder.CreateBox("門扇",
                new Vector3(0f, leafHeight / 2f, 0f),
                new Vector3(leafWidth, leafHeight, p.Thickness), 0),
            InteriorGeometryBuilder.CreateBox("門把",
                new Vector3(leafWidth * .34f, p.HandleHeight, -p.Thickness / 2f - .018f),
                new Vector3(.12f, .035f, .035f), 2)
        };
        if (p.IncludeFrame)
        {
            meshes.Add(InteriorGeometryBuilder.CreateBox("左門框",
                new Vector3(-(p.Width - p.FrameWidth) / 2f, p.Height / 2f, 0f),
                new Vector3(p.FrameWidth, p.Height, p.FrameDepth), 1));
            meshes.Add(InteriorGeometryBuilder.CreateBox("右門框",
                new Vector3((p.Width - p.FrameWidth) / 2f, p.Height / 2f, 0f),
                new Vector3(p.FrameWidth, p.Height, p.FrameDepth), 1));
            meshes.Add(InteriorGeometryBuilder.CreateBox("上門框",
                new Vector3(0f, p.Height - p.FrameWidth / 2f, 0f),
                new Vector3(p.Width - p.FrameWidth * 2f, p.FrameWidth, p.FrameDepth), 1));
        }
        return CreateModel(p.Name, modelId, meshes, DoorMaterials(), "parametric-door", new
        {
            p.Name, p.Width, p.Height, p.Thickness, p.FrameWidth, p.FrameDepth,
            p.IncludeFrame, p.HandleHeight
        });
    }

    public static SceneModel GenerateWindow(ParametricWindowParameters p, Guid? modelId = null)
    {
        ValidateName(p.Name);
        ValidatePositive(p.Width, nameof(p.Width));
        ValidatePositive(p.Height, nameof(p.Height));
        ValidatePositive(p.FrameWidth, nameof(p.FrameWidth));
        ValidatePositive(p.FrameDepth, nameof(p.FrameDepth));
        ValidatePositive(p.GlassThickness, nameof(p.GlassThickness));
        ValidateFinite(p.SillHeight, nameof(p.SillHeight));
        if (p.SillHeight < 0f)
            throw new ArgumentOutOfRangeException(nameof(p.SillHeight), "窗台高度不可小於零。");
        if (p.FrameWidth * 2f >= p.Width || p.FrameWidth * 2f >= p.Height)
            throw new ArgumentOutOfRangeException(nameof(p.FrameWidth), "窗框寬度必須小於窗戶寬高的一半。");
        if (p.IncludeCenterMullion && p.FrameWidth * 3f >= p.Width)
            throw new ArgumentOutOfRangeException(nameof(p.FrameWidth), "包含中央直框時，窗框總寬度必須小於窗戶寬度。");
        var centerY = p.SillHeight + p.Height / 2f;
        var innerWidth = p.Width - p.FrameWidth * 2f;
        var innerHeight = p.Height - p.FrameWidth * 2f;
        var meshes = new List<MeshData>
        {
            InteriorGeometryBuilder.CreateBox("左窗框",
                new Vector3(-(p.Width - p.FrameWidth) / 2f, centerY, 0f),
                new Vector3(p.FrameWidth, p.Height, p.FrameDepth), 0),
            InteriorGeometryBuilder.CreateBox("右窗框",
                new Vector3((p.Width - p.FrameWidth) / 2f, centerY, 0f),
                new Vector3(p.FrameWidth, p.Height, p.FrameDepth), 0),
            InteriorGeometryBuilder.CreateBox("上窗框",
                new Vector3(0f, centerY + (p.Height - p.FrameWidth) / 2f, 0f),
                new Vector3(innerWidth, p.FrameWidth, p.FrameDepth), 0),
            InteriorGeometryBuilder.CreateBox("下窗框",
                new Vector3(0f, centerY - (p.Height - p.FrameWidth) / 2f, 0f),
                new Vector3(innerWidth, p.FrameWidth, p.FrameDepth), 0)
        };
        if (p.IncludeCenterMullion)
        {
            meshes.Add(InteriorGeometryBuilder.CreateBox("中央直框", new Vector3(0f, centerY, 0f),
                new Vector3(p.FrameWidth, innerHeight, p.FrameDepth), 0));
            var glassWidth = (innerWidth - p.FrameWidth) / 2f;
            meshes.Add(InteriorGeometryBuilder.CreateBox("左玻璃",
                new Vector3(-(glassWidth + p.FrameWidth) / 2f, centerY, 0f),
                new Vector3(glassWidth, innerHeight, p.GlassThickness), 1));
            meshes.Add(InteriorGeometryBuilder.CreateBox("右玻璃",
                new Vector3((glassWidth + p.FrameWidth) / 2f, centerY, 0f),
                new Vector3(glassWidth, innerHeight, p.GlassThickness), 1));
        }
        else
        {
            meshes.Add(InteriorGeometryBuilder.CreateBox("玻璃", new Vector3(0f, centerY, 0f),
                new Vector3(innerWidth, innerHeight, p.GlassThickness), 1));
        }
        return CreateModel(p.Name, modelId, meshes, WindowMaterials(), "parametric-window", new
        {
            p.Name, p.Width, p.Height, p.SillHeight, p.FrameWidth, p.FrameDepth,
            p.IncludeCenterMullion, p.GlassThickness
        });
    }

    public static SceneModel GenerateStair(ParametricStairParameters p, Guid? modelId = null)
    {
        ValidateName(p.Name);
        ValidatePositive(p.Width, nameof(p.Width));
        ValidatePositive(p.TotalRun, nameof(p.TotalRun));
        ValidatePositive(p.TotalRise, nameof(p.TotalRise));
        ValidateFinite(p.BaseElevation, nameof(p.BaseElevation));
        if (p.StepCount is < 1 or > 256)
            throw new ArgumentOutOfRangeException(nameof(p.StepCount), "樓梯階數必須介於 1 到 256 之間。");
        if (!p.SolidSteps) ValidatePositive(p.TreadThickness, nameof(p.TreadThickness));

        var treadDepth = p.TotalRun / p.StepCount;
        var riserHeight = p.TotalRise / p.StepCount;
        var meshes = new List<MeshData>(p.StepCount);
        for (var index = 0; index < p.StepCount; index++)
        {
            var top = p.BaseElevation + riserHeight * (index + 1);
            var height = p.SolidSteps ? top - p.BaseElevation : Math.Min(p.TreadThickness, top - p.BaseElevation);
            var centerY = p.SolidSteps ? p.BaseElevation + height / 2f : top - height / 2f;
            var centerZ = -p.TotalRun / 2f + treadDepth * (index + .5f);
            meshes.Add(InteriorGeometryBuilder.CreateBox($"踏階 {index + 1}",
                new Vector3(0f, centerY, centerZ), new Vector3(p.Width, height, treadDepth), 0));
        }
        return CreateModel(p.Name, modelId, meshes, [StairMaterial()], "parametric-stair", new
        {
            p.Name, p.Width, p.TotalRun, p.TotalRise, p.StepCount, p.BaseElevation,
            p.SolidSteps, p.TreadThickness, riserHeight, treadDepth
        });
    }

    private static SceneModel CreateModel(string name, Guid? modelId, List<MeshData> meshes, List<PbrMaterial> materials,
        string documentType, object parameters)
    {
        var model = new SceneModel
        {
            Id = modelId ?? Guid.NewGuid(),
            Name = name,
            IsProcedural = true,
            Materials = materials,
            Meshes = meshes,
            Nodes = [new SceneNode { Name = name, MeshIndices = Enumerable.Range(0, meshes.Count).ToList() }],
            SourceMeshIndices = Enumerable.Range(0, meshes.Count).ToList(),
            SourceMeshIndicesCaptured = true
        };
        model.Extensions[RVInteriorDesignTestSceneFactory.ExtensionKey] = JsonSerializer.SerializeToElement(new
        {
            version = 1,
            documentType,
            parameters
        });
        model.CaptureMeshMaterialIndices();
        model.CaptureProceduralGeometry();
        return model;
    }

    private static List<PbrMaterial> CreateRoomMaterials() => [FloorMaterial(), WallMaterial(), CeilingMaterial()];

    private static PbrMaterial FloorMaterial() => new()
    {
        Name = "RV室內地板",
        BaseColor = new Vector4(.55f, .36f, .2f, 1f),
        Roughness = .6f
    };

    private static PbrMaterial WallMaterial() => new()
    {
        Name = "RV室內牆面",
        BaseColor = new Vector4(.86f, .84f, .79f, 1f),
        Roughness = .82f
    };

    private static PbrMaterial CeilingMaterial() => new()
    {
        Name = "RV室內天花板",
        BaseColor = new Vector4(.94f, .94f, .92f, 1f),
        Roughness = .88f
    };

    private static PbrMaterial StructuralMaterial() => new()
    {
        Name = "RV室內結構材質",
        BaseColor = new Vector4(.68f, .67f, .63f, 1f),
        Roughness = .78f
    };

    private static List<PbrMaterial> DoorMaterials() =>
    [
        new PbrMaterial { Name = "RV室內門扇", BaseColor = new Vector4(.46f, .25f, .11f, 1f), Roughness = .58f },
        new PbrMaterial { Name = "RV室內門框", BaseColor = new Vector4(.72f, .66f, .56f, 1f), Roughness = .65f },
        new PbrMaterial { Name = "RV室內門把", BaseColor = new Vector4(.42f, .43f, .45f, 1f), Metallic = .75f, Roughness = .3f }
    ];

    private static List<PbrMaterial> WindowMaterials() =>
    [
        new PbrMaterial { Name = "RV室內窗框", BaseColor = new Vector4(.32f, .34f, .36f, 1f), Metallic = .4f, Roughness = .45f },
        new PbrMaterial
        {
            Name = "RV室內玻璃", BaseColor = new Vector4(.72f, .88f, .94f, 1f),
            Opacity = .3f, Roughness = .08f, DoubleSided = true, RenderMode = MaterialRenderMode.Glass
        }
    ];

    private static PbrMaterial StairMaterial() => new()
    {
        Name = "RV室內樓梯", BaseColor = new Vector4(.52f, .34f, .19f, 1f), Roughness = .62f
    };

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("模型名稱不可空白。", nameof(name));
    }

    private static void ValidatePositive(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(parameterName, "尺寸必須是大於零的有限數值。");
    }

    private static void ValidateFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, "位置必須是有限數值。");
    }
}
