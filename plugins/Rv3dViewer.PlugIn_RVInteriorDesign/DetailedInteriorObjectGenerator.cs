namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using System.Text.Json;
using Rv3dViewer.Core;

internal static class DetailedInteriorObjectGenerator
{
    public static SceneModel Generate(ParametricPrimitiveParameters p, Guid? modelId = null)
    {
        Validate(p);
        var meshes = p.DetailPreset switch
        {
            InteriorDetailPreset.Rug => CreateRug(p),
            InteriorDetailPreset.CoffeeTable => CreateCoffeeTable(p),
            InteriorDetailPreset.DecorativeGlobe => CreateDecorativeGlobe(p),
            InteriorDetailPreset.Stool => CreateStool(p),
            InteriorDetailPreset.Sofa => CreateSofa(p),
            InteriorDetailPreset.PendantLamp => CreatePendantLamp(p),
            InteriorDetailPreset.Planter => CreatePlanter(p),
            _ => throw new ArgumentOutOfRangeException(nameof(p.DetailPreset))
        };
        foreach (var mesh in meshes)
            InteriorMeshGeometry.Normalize(mesh);
        var materials = CreateMaterials();
        var model = new SceneModel
        {
            Id = modelId ?? Guid.NewGuid(),
            Name = p.Name,
            IsProcedural = true,
            Materials = materials,
            Meshes = meshes,
            Nodes = [new SceneNode { Name = p.Name, MeshIndices = Enumerable.Range(0, meshes.Count).ToList() }],
            SourceMeshIndices = Enumerable.Range(0, meshes.Count).ToList(),
            SourceMeshIndicesCaptured = true
        };
        model.Extensions[RVInteriorDesignTestSceneFactory.ExtensionKey] = JsonSerializer.SerializeToElement(new
        {
            version = 1,
            documentType = "detailed-interior-object",
            preset = p.DetailPreset.ToString(),
            primitiveKind = p.Kind.ToString(),
            parameters = new { p.Name, p.Width, p.Depth, p.Height, p.Radius, p.Segments, p.PolygonSides }
        });
        model.CaptureMeshMaterialIndices();
        model.CaptureProceduralGeometry();
        return model;
    }

    private static List<MeshData> CreateRug(ParametricPrimitiveParameters p)
    {
        var width = p.Width;
        var depth = p.Depth;
        var edge = MathF.Min(width, depth) * .055f;
        var meshes = new List<MeshData>
        {
            Box("地毯絨面", new(0f, -.012f, 0f), new(width, .024f, depth), 8),
            Box("地毯前飾邊", new(0f, .004f, depth / 2f - edge / 2f), new(width, .012f, edge), 3),
            Box("地毯後飾邊", new(0f, .004f, -depth / 2f + edge / 2f), new(width, .012f, edge), 3),
            Box("地毯左飾邊", new(-width / 2f + edge / 2f, .004f, 0f), new(edge, .012f, depth - edge * 2f), 3),
            Box("地毯右飾邊", new(width / 2f - edge / 2f, .004f, 0f), new(edge, .012f, depth - edge * 2f), 3)
        };
        for (var index = 0; index < 12; index++)
        {
            var x = -width * .43f + width * .86f * index / 11f;
            meshes.Add(Box($"地毯流蘇 {index + 1}", new(x, -.008f, depth / 2f + .025f),
                new(width * .018f, .012f, .07f), 4));
            meshes.Add(Box($"地毯流蘇 {index + 13}", new(x, -.008f, -depth / 2f - .025f),
                new(width * .018f, .012f, .07f), 4));
        }
        return meshes;
    }

    private static List<MeshData> CreateCoffeeTable(ParametricPrimitiveParameters p)
    {
        var topThickness = MathF.Max(.045f, p.Height * .1f);
        var legSize = MathF.Min(p.Width, p.Depth) * .085f;
        var legHeight = p.Height - topThickness;
        var legY = -p.Height / 2f + legHeight / 2f;
        var insetX = p.Width / 2f - legSize * 1.2f;
        var insetZ = p.Depth / 2f - legSize * 1.2f;
        return
        [
            Box("實木桌板", new(0f, p.Height / 2f - topThickness / 2f, 0f),
                new(p.Width, topThickness, p.Depth), 0),
            Box("桌腳左前", new(-insetX, legY, insetZ), new(legSize, legHeight, legSize), 1),
            Box("桌腳右前", new(insetX, legY, insetZ), new(legSize, legHeight, legSize), 1),
            Box("桌腳左後", new(-insetX, legY, -insetZ), new(legSize, legHeight, legSize), 1),
            Box("桌腳右後", new(insetX, legY, -insetZ), new(legSize, legHeight, legSize), 1),
            Box("下層置物板", new(0f, -p.Height / 2f + legHeight * .25f, 0f),
                new(p.Width * .76f, topThickness * .55f, p.Depth * .72f), 0)
        ];
    }

    private static List<MeshData> CreateDecorativeGlobe(ParametricPrimitiveParameters p)
    {
        var radius = p.Radius;
        return
        [
            Sphere("裝飾球本體", new(0f, radius * .08f, 0f), new(radius, radius, radius), 4, 48),
            Cylinder("裝飾球頸圈", new(0f, -radius * .72f, 0f), radius * .28f, radius * .18f, 32, 1),
            Cylinder("裝飾球底座", new(0f, -radius * .94f, 0f), radius * .48f, radius * .12f, 32, 1),
            Cylinder("裝飾球頂飾", new(0f, radius * 1.08f, 0f), radius * .08f, radius * .18f, 24, 1)
        ];
    }

    private static List<MeshData> CreateStool(ParametricPrimitiveParameters p)
    {
        var seatHeight = MathF.Max(.08f, p.Height * .16f);
        var legHeight = p.Height - seatHeight;
        var legRadius = p.Radius * .065f;
        var legY = -p.Height / 2f + legHeight / 2f;
        var offset = p.Radius * .62f;
        var meshes = new List<MeshData>
        {
            Cylinder("軟墊座面", new(0f, p.Height / 2f - seatHeight / 2f, 0f), p.Radius, seatHeight, 48, 2),
            Cylinder("座面下框", new(0f, p.Height / 2f - seatHeight * 1.05f, 0f), p.Radius * .84f,
                seatHeight * .18f, 40, 1)
        };
        foreach (var (x, z, name) in new[]
                 {
                     (-offset, -offset, "左後"), (offset, -offset, "右後"),
                     (-offset, offset, "左前"), (offset, offset, "右前")
                 })
            meshes.Add(Cylinder($"{name}椅腳", new(x, legY, z), legRadius, legHeight, 20, 1));
        return meshes;
    }

    private static List<MeshData> CreateSofa(ParametricPrimitiveParameters p)
    {
        var width = p.Width;
        var depth = p.Depth;
        var height = p.Height;
        var baseHeight = height * .25f;
        var seatHeight = height * .18f;
        var armWidth = width * .105f;
        var cushionGap = width * .018f;
        var cushionWidth = (width - armWidth * 2f - cushionGap) / 2f;
        return
        [
            Box("沙發底座", new(0f, -height / 2f + baseHeight / 2f, 0f),
                new(width * .94f, baseHeight, depth * .86f), 1),
            Box("左座墊", new(-cushionWidth / 2f - cushionGap / 2f, -height * .08f, depth * .04f),
                new(cushionWidth, seatHeight, depth * .72f), 2),
            Box("右座墊", new(cushionWidth / 2f + cushionGap / 2f, -height * .08f, depth * .04f),
                new(cushionWidth, seatHeight, depth * .72f), 2),
            Box("沙發靠背", new(0f, height * .2f, -depth * .35f),
                new(width * .92f, height * .58f, depth * .18f), 2),
            Box("左靠枕", new(-width * .235f, height * .2f, -depth * .225f),
                new(width * .4f, height * .42f, depth * .12f), 3, -.045f),
            Box("右靠枕", new(width * .235f, height * .2f, -depth * .225f),
                new(width * .4f, height * .42f, depth * .12f), 3, .045f),
            Box("左扶手", new(-width / 2f + armWidth / 2f, -height * .01f, 0f),
                new(armWidth, height * .58f, depth), 2),
            Box("右扶手", new(width / 2f - armWidth / 2f, -height * .01f, 0f),
                new(armWidth, height * .58f, depth), 2),
            Cylinder("左前腳", new(-width * .38f, -height * .46f, depth * .31f), width * .018f, height * .08f, 16, 1),
            Cylinder("右前腳", new(width * .38f, -height * .46f, depth * .31f), width * .018f, height * .08f, 16, 1)
        ];
    }

    private static List<MeshData> CreatePendantLamp(ParametricPrimitiveParameters p)
    {
        var height = p.Height;
        var radius = p.Radius;
        return
        [
            Cylinder("吸頂座", new(0f, height * .43f, 0f), radius * .25f, height * .08f, 32, 1),
            Cylinder("吊線", new(0f, height * .2f, 0f), radius * .025f, height * .42f, 12, 1),
            Cylinder("燈頭", new(0f, height * .01f, 0f), radius * .12f, height * .12f, 24, 1),
            Frustum("金屬燈罩", new(0f, -height * .18f, 0f), radius, radius * .23f,
                height * .38f, 48, 0, false, false),
            Sphere("燈泡", new(0f, -height * .25f, 0f),
                new(radius * .21f, radius * .27f, radius * .21f), 7, 32)
        ];
    }

    private static List<MeshData> CreatePlanter(ParametricPrimitiveParameters p)
    {
        var height = p.Height;
        var radius = p.Radius;
        var potHeight = height * .48f;
        var potY = -height / 2f + potHeight / 2f;
        var soilY = -height / 2f + potHeight * .86f;
        var meshes = new List<MeshData>
        {
            Frustum("陶盆", new(0f, potY, 0f), radius * .72f, radius, potHeight,
                Math.Max(12, p.PolygonSides * 4), 4, true, true),
            Cylinder("盆口飾圈", new(0f, -height / 2f + potHeight * .94f, 0f), radius * 1.04f,
                potHeight * .12f, 36, 4),
            Cylinder("培養土", new(0f, soilY, 0f), radius * .88f, potHeight * .04f, 36, 6)
        };
        var stems = new[]
        {
            new Vector3(-.22f, .76f, -.08f), new Vector3(.16f, .92f, -.14f),
            new Vector3(-.05f, 1f, .12f), new Vector3(.3f, .7f, .14f), new Vector3(-.32f, .64f, .2f)
        };
        for (var index = 0; index < stems.Length; index++)
        {
            var stem = stems[index];
            var stemHeight = height * stem.Y * .48f;
            var x = radius * stem.X;
            var z = radius * stem.Z;
            meshes.Add(Cylinder($"植栽莖 {index + 1}", new(x, soilY + stemHeight / 2f, z),
                radius * .035f, stemHeight, 12, 5));
            meshes.Add(Sphere($"植栽葉 {index + 1}", new(x, soilY + stemHeight, z),
                new(radius * .34f, height * .075f, radius * .18f), 5, 24));
        }
        return meshes;
    }

    private static MeshData Box(string name, Vector3 center, Vector3 size, int materialIndex,
        float yawRadians = 0f) =>
        InteriorGeometryBuilder.CreateBox(name, center, size, materialIndex, yawRadians);

    private static MeshData Cylinder(string name, Vector3 center, float radius, float height, int segments,
        int materialIndex) =>
        InteriorGeometryBuilder.CreateCylinder(name, center, radius, height, segments, materialIndex);

    private static MeshData Sphere(string name, Vector3 center, Vector3 scale, int materialIndex, int segments)
    {
        var source = new ParametricPrimitiveParameters
        {
            Name = name,
            Kind = ParametricPrimitiveKind.Sphere,
            Radius = 1f,
            Segments = segments
        }.Generate().Meshes[0];
        source.Positions = source.Positions.Select(position => position * scale + center).ToArray();
        source.Normals = source.Normals.Select(normal => Vector3.Normalize(new Vector3(
            normal.X / scale.X, normal.Y / scale.Y, normal.Z / scale.Z))).ToArray();
        source.MaterialIndex = materialIndex;
        return source;
    }

    private static MeshData Frustum(string name, Vector3 center, float bottomRadius, float topRadius, float height,
        int segments, int materialIndex, bool capBottom, bool capTop)
    {
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tangents = new List<Vector4>();
        var indices = new List<uint>();
        var halfHeight = height / 2f;
        var slope = (bottomRadius - topRadius) / height;
        for (var index = 0; index <= segments; index++)
        {
            var u = (float)index / segments;
            var angle = MathF.Tau * u;
            var radial = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            var normal = Vector3.Normalize(new Vector3(radial.X, slope, radial.Z));
            var tangent = new Vector4(-MathF.Sin(angle), 0f, MathF.Cos(angle), 1f);
            positions.Add(center + new Vector3(radial.X * bottomRadius, -halfHeight, radial.Z * bottomRadius));
            positions.Add(center + new Vector3(radial.X * topRadius, halfHeight, radial.Z * topRadius));
            normals.Add(normal);
            normals.Add(normal);
            uvs.Add(new(u, 1f));
            uvs.Add(new(u, 0f));
            tangents.Add(tangent);
            tangents.Add(tangent);
        }
        for (var index = 0; index < segments; index++)
        {
            var offset = (uint)(index * 2);
            indices.AddRange([offset, offset + 1, offset + 3, offset, offset + 3, offset + 2]);
        }
        if (capBottom) AddCap(-halfHeight, bottomRadius, -Vector3.UnitY, false);
        if (capTop) AddCap(halfHeight, topRadius, Vector3.UnitY, true);
        var mesh = new MeshData
        {
            Name = name,
            Positions = positions.ToArray(),
            Normals = normals.ToArray(),
            TextureCoordinates = uvs.ToArray(),
            Tangents = tangents.ToArray(),
            Indices = indices.ToArray(),
            MaterialIndex = materialIndex
        };
        InteriorMeshGeometry.Normalize(mesh);
        return mesh;

        void AddCap(float y, float radius, Vector3 normal, bool top)
        {
            var capCenter = (uint)positions.Count;
            positions.Add(center + new Vector3(0f, y, 0f));
            normals.Add(normal);
            uvs.Add(new(.5f, .5f));
            tangents.Add(new(1f, 0f, 0f, 1f));
            for (var index = 0; index < segments; index++)
            {
                var angle = MathF.Tau * index / segments;
                positions.Add(center + new Vector3(MathF.Cos(angle) * radius, y, MathF.Sin(angle) * radius));
                normals.Add(normal);
                uvs.Add(new(.5f + MathF.Cos(angle) * .5f, .5f + MathF.Sin(angle) * .5f));
                tangents.Add(new(1f, 0f, 0f, 1f));
            }
            for (var index = 0; index < segments; index++)
            {
                var current = capCenter + 1u + (uint)index;
                var next = capCenter + 1u + (uint)((index + 1) % segments);
                if (top) indices.AddRange([capCenter, next, current]);
                else indices.AddRange([capCenter, current, next]);
            }
        }
    }

    private static List<PbrMaterial> CreateMaterials() =>
    [
        new() { Name = "胡桃實木", BaseColor = new(.34f, .16f, .065f, 1f), Roughness = .48f },
        new() { Name = "霧黑金屬", BaseColor = new(.075f, .085f, .09f, 1f), Metallic = .78f, Roughness = .3f },
        new() { Name = "主布料", BaseColor = new(.24f, .38f, .58f, 1f), Roughness = .88f },
        new() { Name = "飾面布料", BaseColor = new(.78f, .65f, .38f, 1f), Roughness = .9f },
        new() { Name = "霧面陶瓷", BaseColor = new(.72f, .48f, .27f, 1f), Roughness = .72f },
        new() { Name = "植栽葉片", BaseColor = new(.12f, .42f, .2f, 1f), Roughness = .82f, DoubleSided = true },
        new() { Name = "培養土", BaseColor = new(.13f, .075f, .035f, 1f), Roughness = .96f },
        new() { Name = "暖光燈泡", BaseColor = new(1f, .77f, .38f, 1f), Roughness = .2f,
            Emissive = new(.95f, .52f, .16f), EmissiveStrength = 2.2f },
        new() { Name = "短絨地毯", BaseColor = new(.12f, .35f, .48f, 1f), Roughness = .94f }
    ];

    private static void Validate(ParametricPrimitiveParameters p)
    {
        if (string.IsNullOrWhiteSpace(p.Name))
            throw new ArgumentException("模型名稱不可空白。", nameof(p));
        if (!float.IsFinite(p.Width) || !float.IsFinite(p.Depth) || !float.IsFinite(p.Height) ||
            !float.IsFinite(p.Radius) || p.Width <= 0f || p.Depth <= 0f || p.Height <= 0f || p.Radius <= 0f)
            throw new ArgumentOutOfRangeException(nameof(p), "詳細室內物件尺寸必須是大於零的有限數值。");
    }
}
