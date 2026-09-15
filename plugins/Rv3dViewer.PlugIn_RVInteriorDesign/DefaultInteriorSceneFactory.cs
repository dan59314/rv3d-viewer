namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using Rv3dViewer.Core;

internal static class DefaultInteriorSceneFactory
{
    internal const string SceneNodeName = "defaultInteriorSceneNode";

    internal static async Task<IReadOnlyList<ParametricDesignObject>> CreateRealAsync(
        IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("步驟 1/4：準備並驗證預設真實模型…");
        var realAssets = await DefaultInteriorAssetCatalog.LoadAsync(progress, cancellationToken);
        progress?.Report("步驟 3/4：組合參數房間與真實家具…");
        var replacedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "長方體茶几", "球體擺飾", "圓柱椅凳", "半圓柱沙發背", "多邊錐植栽盆"
        };
        var result = Create().Where(item => !replacedNames.Contains(item.Parameters.Name)).ToList();
        result.AddRange(realAssets);
        return result;
    }

    internal static IReadOnlyList<ParametricDesignObject> Create()
    {
        var result = new List<ParametricDesignObject>();

        var room = Add(result, new ParametricRoomParameters
        {
            Name = "預設展示房間",
            Width = 6f,
            Depth = 4f,
            Height = 2.8f,
            IncludeFloor = true,
            IncludeCeiling = false
        }, Vector3.Zero);
        // Open the two camera-facing sides so the furnished room remains visible in Perspective.
        room.Model.HiddenMeshIndices = [2, 4];

        Add(result, new ParametricWallParameters
        {
            Name = "門窗洞口示範牆",
            StartX = -2.7f,
            StartZ = .85f,
            EndX = 2.7f,
            EndZ = .85f,
            Height = 2.55f,
            Thickness = .1f,
            DoorOpening = new WallOpeningParameters
            {
                Enabled = true, Kind = WallOpeningKind.Door, OffsetFromStart = .35f,
                Width = .9f, Height = 2.1f
            },
            WindowOpening = new WallOpeningParameters
            {
                Enabled = true, Kind = WallOpeningKind.Window, OffsetFromStart = 2.2f,
                Width = 1.2f, Height = 1.05f, SillHeight = .9f
            }
        }, Vector3.Zero);
        Add(result, new ParametricSlabParameters
        {
            Name = "地板樣本",
            Kind = ParametricSlabKind.Floor,
            Width = 1.8f,
            Depth = 1.2f,
            Thickness = .04f,
            Elevation = .025f
        }, new Vector3(-1.45f, 0f, .65f));
        Add(result, new ParametricSlabParameters
        {
            Name = "天花板樣本",
            Kind = ParametricSlabKind.Ceiling,
            Width = 1.4f,
            Depth = 1.1f,
            Thickness = .06f,
            Elevation = 2.68f
        }, new Vector3(1.65f, 0f, -.75f));

        Add(result, new ParametricBeamParameters
        {
            Name = "後側橫樑",
            StartX = -2.45f,
            StartZ = -1.65f,
            EndX = 2.45f,
            EndZ = -1.65f,
            Width = .18f,
            Height = .25f,
            BaseElevation = 2.35f
        }, Vector3.Zero);
        Add(result, new ParametricColumnParameters
        {
            Name = "矩形柱",
            Shape = ParametricColumnShape.Rectangular,
            PositionX = -2.45f,
            PositionZ = -1.65f,
            Width = .22f,
            Depth = .22f,
            Height = 2.35f
        }, Vector3.Zero);
        Add(result, new ParametricColumnParameters
        {
            Name = "圓柱",
            Shape = ParametricColumnShape.Circular,
            PositionX = 2.45f,
            PositionZ = -1.65f,
            Diameter = .26f,
            Height = 2.35f,
            Segments = 32
        }, Vector3.Zero);

        AddPrimitive(result, "平面地毯", ParametricPrimitiveKind.Plane,
            new Vector3(-1.45f, .055f, .65f), new Vector4(.18f, .42f, .55f, 1f),
            width: 1.55f, depth: .95f, detailPreset: InteriorDetailPreset.Rug);
        AddPrimitive(result, "長方體茶几", ParametricPrimitiveKind.Box,
            new Vector3(-1.45f, .35f, .35f), new Vector4(.43f, .22f, .09f, 1f),
            width: 1.15f, depth: .62f, height: .58f, detailPreset: InteriorDetailPreset.CoffeeTable);
        AddPrimitive(result, "球體擺飾", ParametricPrimitiveKind.Sphere,
            new Vector3(-1.45f, .82f, .35f), new Vector4(.82f, .45f, .16f, 1f), radius: .22f,
            detailPreset: InteriorDetailPreset.DecorativeGlobe);
        AddPrimitive(result, "圓柱椅凳", ParametricPrimitiveKind.Cylinder,
            new Vector3(.45f, .38f, 1.05f), new Vector4(.24f, .52f, .36f, 1f),
            height: .7f, radius: .32f, detailPreset: InteriorDetailPreset.Stool);
        AddPrimitive(result, "半圓柱沙發背", ParametricPrimitiveKind.HalfCylinder,
            new Vector3(1.25f, .52f, -1.35f), new Vector4(.34f, .43f, .68f, 1f),
            width: 1.75f, depth: .78f, height: .9f, radius: .5f,
            detailPreset: InteriorDetailPreset.Sofa);
        AddPrimitive(result, "圓錐燈罩", ParametricPrimitiveKind.Cone,
            new Vector3(1.65f, 2.18f, -.75f), new Vector4(.92f, .72f, .28f, 1f),
            height: .82f, radius: .42f, detailPreset: InteriorDetailPreset.PendantLamp);
        AddPrimitive(result, "多邊錐植栽盆", ParametricPrimitiveKind.PolygonCone,
            new Vector3(2.35f, .45f, 1.35f), new Vector4(.46f, .25f, .13f, 1f),
            height: .82f, radius: .42f, polygonSides: 6, detailPreset: InteriorDetailPreset.Planter);

        return result;
    }

    private static void AddPrimitive(List<ParametricDesignObject> result, string name, ParametricPrimitiveKind kind,
        Vector3 position, Vector4 color, float width = 1f, float depth = 1f, float height = 1f,
        float radius = .5f, int polygonSides = 6, Vector3? rotation = null,
        InteriorDetailPreset detailPreset = InteriorDetailPreset.None)
    {
        var item = Add(result, new ParametricPrimitiveParameters
        {
            Name = name,
            Kind = kind,
            DetailPreset = detailPreset,
            Width = width,
            Depth = depth,
            Height = height,
            Radius = radius,
            PolygonSides = polygonSides
        }, position, rotation ?? Vector3.Zero);
        if (detailPreset == InteriorDetailPreset.None)
        {
            item.Model.Materials[0].Name = $"{name}預設材質";
            item.Model.Materials[0].BaseColor = color;
        }
    }

    private static ParametricDesignObject Add(List<ParametricDesignObject> result,
        IInteriorParametricParameters parameters, Vector3 position, Vector3? rotation = null)
    {
        var id = Guid.NewGuid();
        var model = parameters.Generate(id);
        model.Transform.Position = position;
        model.Transform.RotationDegrees = rotation ?? Vector3.Zero;
        var item = new ParametricDesignObject
        {
            Id = id,
            Parameters = parameters,
            Model = model,
            IsDefaultSceneObject = true
        };
        result.Add(item);
        return item;
    }
}
