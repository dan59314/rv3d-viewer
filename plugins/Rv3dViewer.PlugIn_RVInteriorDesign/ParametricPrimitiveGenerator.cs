namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using System.Text.Json;
using Rv3dViewer.Core;

internal static class ParametricPrimitiveGenerator
{
    public static SceneModel Generate(ParametricPrimitiveParameters parameters, Guid? modelId = null)
    {
        if (parameters.DetailPreset != InteriorDetailPreset.None)
            return DetailedInteriorObjectGenerator.Generate(parameters, modelId);
        Validate(parameters);
        var mesh = parameters.Kind switch
        {
            ParametricPrimitiveKind.Plane => CreatePlane(parameters),
            ParametricPrimitiveKind.Box => InteriorGeometryBuilder.CreateBox(parameters.Name, Vector3.Zero,
                new Vector3(parameters.Width, parameters.Height, parameters.Depth)),
            ParametricPrimitiveKind.Sphere => CreateSphere(parameters),
            ParametricPrimitiveKind.Cylinder => CreateCylinder(parameters, false, false),
            ParametricPrimitiveKind.HalfCylinder => CreateCylinder(parameters, true, false),
            ParametricPrimitiveKind.Cone => CreateCylinder(parameters, false, true),
            ParametricPrimitiveKind.PolygonCone => CreateCylinder(parameters, false, true, parameters.PolygonSides),
            _ => throw new ArgumentOutOfRangeException(nameof(parameters.Kind))
        };

        var model = new SceneModel
        {
            Id = modelId ?? Guid.NewGuid(),
            Name = parameters.Name,
            IsProcedural = true,
            Materials =
            [
                new PbrMaterial
                {
                    Name = "RV參數模型預設材質",
                    BaseColor = new Vector4(.72f, .76f, .82f, 1f),
                    Roughness = .65f
                }
            ],
            Meshes = [mesh],
            Nodes = [new SceneNode { Name = parameters.Name, MeshIndices = [0] }],
            SourceMeshIndices = [0],
            SourceMeshIndicesCaptured = true
        };
        model.Extensions[RVInteriorDesignTestSceneFactory.ExtensionKey] = JsonSerializer.SerializeToElement(new
        {
            version = 1,
            documentType = "parametric-primitive",
            primitive = new
            {
                kind = parameters.Kind.ToString(),
                parameters.Name,
                parameters.Width,
                parameters.Depth,
                parameters.Height,
                parameters.Radius,
                parameters.Segments,
                parameters.HeightSegments,
                parameters.PolygonSides
            }
        });
        model.CaptureMeshMaterialIndices();
        model.CaptureProceduralGeometry();
        return model;
    }

    private static void Validate(ParametricPrimitiveParameters p)
    {
        if (string.IsNullOrWhiteSpace(p.Name)) throw new ArgumentException("模型名稱不可空白。");
        if (p.Kind == ParametricPrimitiveKind.Plane && (p.Width <= 0 || p.Depth <= 0))
            throw new ArgumentOutOfRangeException(nameof(p), "平面寬度與深度必須大於零。");
        if (p.Kind == ParametricPrimitiveKind.Box && (p.Width <= 0 || p.Depth <= 0 || p.Height <= 0))
            throw new ArgumentOutOfRangeException(nameof(p), "長方體尺寸必須大於零。");
        if (p.Kind is not ParametricPrimitiveKind.Plane and not ParametricPrimitiveKind.Box && p.Radius <= 0)
            throw new ArgumentOutOfRangeException(nameof(p.Radius), "半徑必須大於零。");
        if (p.Kind is ParametricPrimitiveKind.Cylinder or ParametricPrimitiveKind.HalfCylinder or ParametricPrimitiveKind.Cone or ParametricPrimitiveKind.PolygonCone && p.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(p.Height), "高度必須大於零。");
        if (p.Kind is ParametricPrimitiveKind.Sphere or ParametricPrimitiveKind.Cylinder or ParametricPrimitiveKind.HalfCylinder or ParametricPrimitiveKind.Cone && p.Segments is < 3 or > 256)
            throw new ArgumentOutOfRangeException(nameof(p.Segments), "圓周分段必須介於 3 到 256。");
        if (p.Kind is ParametricPrimitiveKind.Cylinder or ParametricPrimitiveKind.HalfCylinder or ParametricPrimitiveKind.Cone or ParametricPrimitiveKind.PolygonCone && p.HeightSegments is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(p.HeightSegments), "高度分段必須介於 1 到 64。");
        if (p.Kind == ParametricPrimitiveKind.PolygonCone && p.PolygonSides is < 3 or > 64)
            throw new ArgumentOutOfRangeException(nameof(p.PolygonSides), "多邊形邊數必須介於 3 到 64。");
    }

    private static MeshData CreatePlane(ParametricPrimitiveParameters p)
    {
        var x = p.Width / 2f;
        var z = p.Depth / 2f;
        return Mesh(p.Name,
            [new(-x, 0, -z), new(x, 0, -z), new(x, 0, z), new(-x, 0, z)],
            [Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY],
            [new(0, 1), new(1, 1), new(1, 0), new(0, 0)],
            [0, 2, 1, 0, 3, 2]);
    }

    private static MeshData CreateSphere(ParametricPrimitiveParameters p)
    {
        var longitude = p.Segments;
        var latitude = Math.Max(2, p.Segments / 2);
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var indices = new List<uint>();
        for (var y = 0; y <= latitude; y++)
        {
            var v = y / (float)latitude;
            var phi = v * MathF.PI;
            for (var x = 0; x <= longitude; x++)
            {
                var u = x / (float)longitude;
                var theta = u * MathF.Tau;
                var normal = new Vector3(MathF.Sin(phi) * MathF.Cos(theta), MathF.Cos(phi), MathF.Sin(phi) * MathF.Sin(theta));
                positions.Add(normal * p.Radius);
                normals.Add(normal);
                uvs.Add(new Vector2(u, v));
            }
        }
        for (var y = 0; y < latitude; y++)
        for (var x = 0; x < longitude; x++)
        {
            var a = (uint)(y * (longitude + 1) + x);
            var b = a + (uint)longitude + 1;
            indices.AddRange([a, b, a + 1, a + 1, b, b + 1]);
        }
        return Mesh(p.Name, positions, normals, uvs, indices);
    }

    private static MeshData CreateCylinder(ParametricPrimitiveParameters p, bool half, bool cone, int? forcedSegments = null)
    {
        var segments = forcedSegments ?? p.Segments;
        var arc = half ? MathF.PI : MathF.Tau;
        var bottomY = -p.Height / 2f;
        var topY = p.Height / 2f;
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var indices = new List<uint>();

        for (var yStep = 0; yStep <= p.HeightSegments; yStep++)
        {
            var v = yStep / (float)p.HeightSegments;
            var y = bottomY + p.Height * v;
            var radius = cone ? p.Radius * (1f - v) : p.Radius;
            for (var i = 0; i <= segments; i++)
            {
                var u = i / (float)segments;
                var angle = -arc / 2f + arc * u;
                var radial = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle));
                var normal = cone ? Vector3.Normalize(new Vector3(radial.X, p.Radius / p.Height, radial.Z)) : radial;
                positions.Add(new Vector3(radial.X * radius, y, radial.Z * radius));
                normals.Add(normal);
                uvs.Add(new Vector2(u, 1f - v));
            }
        }
        for (var y = 0; y < p.HeightSegments; y++)
        for (var i = 0; i < segments; i++)
        {
            var a = (uint)(y * (segments + 1) + i);
            var b = a + (uint)segments + 1;
            indices.AddRange([a, b, a + 1, a + 1, b, b + 1]);
        }

        AddCap(positions, normals, uvs, indices, p.Radius, bottomY, segments, arc, false);
        if (!cone) AddCap(positions, normals, uvs, indices, p.Radius, topY, segments, arc, true);
        if (half) AddHalfCylinderCutFaces(positions, normals, uvs, indices, p.Radius, bottomY, topY);
        return Mesh(p.Name, positions, normals, uvs, indices);
    }

    private static void AddCap(List<Vector3> positions, List<Vector3> normals, List<Vector2> uvs, List<uint> indices,
        float radius, float y, int segments, float arc, bool top)
    {
        var center = (uint)positions.Count;
        positions.Add(new Vector3(0, y, 0));
        normals.Add(top ? Vector3.UnitY : -Vector3.UnitY);
        uvs.Add(new Vector2(.5f, .5f));
        for (var i = 0; i <= segments; i++)
        {
            var angle = -arc / 2f + arc * i / segments;
            var x = MathF.Cos(angle);
            var z = MathF.Sin(angle);
            positions.Add(new Vector3(x * radius, y, z * radius));
            normals.Add(top ? Vector3.UnitY : -Vector3.UnitY);
            uvs.Add(new Vector2(x * .5f + .5f, z * .5f + .5f));
        }
        for (var i = 0; i < segments; i++)
        {
            var a = center + 1u + (uint)i;
            if (top) indices.AddRange([center, a, a + 1]);
            else indices.AddRange([center, a + 1, a]);
        }
    }

    private static void AddHalfCylinderCutFaces(List<Vector3> positions, List<Vector3> normals, List<Vector2> uvs,
        List<uint> indices, float radius, float bottomY, float topY)
    {
        AddQuad(positions, normals, uvs, indices,
            new(0, bottomY, -radius), new(0, bottomY, radius), new(0, topY, radius), new(0, topY, -radius), -Vector3.UnitX);
    }

    private static void AddQuad(List<Vector3> positions, List<Vector3> normals, List<Vector2> uvs, List<uint> indices,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        var start = (uint)positions.Count;
        positions.AddRange([a, b, c, d]);
        normals.AddRange([normal, normal, normal, normal]);
        uvs.AddRange([new(0, 1), new(1, 1), new(1, 0), new(0, 0)]);
        indices.AddRange([start, start + 1, start + 2, start, start + 2, start + 3]);
    }

    private static MeshData Mesh(string name, IEnumerable<Vector3> positions, IEnumerable<Vector3> normals,
        IEnumerable<Vector2> uvs, IEnumerable<uint> indices)
    {
        var positionArray = positions.ToArray();
        var mesh = new MeshData
        {
            Name = name,
            Positions = positionArray,
            Normals = normals.ToArray(),
            TextureCoordinates = uvs.ToArray(),
            Tangents = Enumerable.Repeat(new Vector4(1, 0, 0, 1), positionArray.Length).ToArray(),
            Indices = indices.ToArray(),
            MaterialIndex = 0
        };
        InteriorMeshGeometry.Normalize(mesh);
        return mesh;
    }
}
