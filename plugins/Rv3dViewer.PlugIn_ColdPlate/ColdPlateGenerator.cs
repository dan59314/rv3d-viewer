using System.Numerics;
using Rv3dViewer.Core;

namespace Rv3dViewer.ColdPlatePlugin;

public static class ColdPlateGenerator
{
    // 24 sides keeps the circular silhouette smooth while leaving each side wide
    // enough for the default physical port/opening to be generated as one aligned cut.
    private const int CircleSegments = 24;

    public static SceneModel Generate(ColdPlateParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var outer = CreateOuterPolygon(parameters);
        ValidateConvexPolygon(outer, "底座外型");
        var inner = CreateBaseInnerPolygon(parameters, outer);
        ValidateNestedPolygons(outer, inner, "底座凹槽");
        ValidatePositive(parameters.BaseHeight, "底座厚度");
        ValidateDepth(parameters.BaseRecessDepth, parameters.BaseHeight, "底座內凹深度");

        var ports = ResolvePorts(parameters, outer, inner);
        var baseMesh = BuildBase(outer, inner, parameters.BaseHeight, parameters.BaseRecessDepth, ports);
        var inletMesh = BuildTube("進水口", ports[0]);
        var outletMesh = BuildTube("出水口", ports[1]);

        var coverOuter = CreateCoverOuterPolygon(parameters, inner);
        ValidateConvexPolygon(coverOuter, "上蓋外型");
        var coverInner = CreateCoverInnerPolygon(parameters, coverOuter);
        ValidateNestedPolygons(coverOuter, coverInner, "上蓋內凹");
        ValidatePositive(parameters.CoverHeight, "上蓋高度");
        ValidateDepth(parameters.CoverRecessDepth, parameters.CoverHeight, "上蓋內凹深度");
        var coverMesh = BuildCover(
            coverOuter,
            coverInner,
            parameters.BaseHeight,
            parameters.CoverHeight,
            parameters.CoverRecessDepth);

        var model = new SceneModel
        {
            Name = "Cold Plate",
            IsProcedural = true,
            Materials =
            [
                new PbrMaterial
                {
                    Name = "Copper",
                    BaseColor = new Vector4(0.72f, 0.28f, 0.10f, 1f),
                    Metallic = 0.92f,
                    Roughness = 0.24f,
                    DoubleSided = true
                },
                new PbrMaterial
                {
                    Name = "Glass",
                    BaseColor = new Vector4(0.68f, 0.88f, 0.95f, 0.32f),
                    Opacity = 0.32f,
                    Roughness = 0.08f,
                    Transmission = 0.96f,
                    IndexOfRefraction = 1.5f,
                    Thickness = Math.Max(0.01f, parameters.CoverHeight - parameters.CoverRecessDepth),
                    RenderMode = MaterialRenderMode.Glass,
                    DoubleSided = true
                }
            ],
            Meshes = [baseMesh, inletMesh, outletMesh, coverMesh],
            Nodes =
            [
                new SceneNode
                {
                    Name = "Cold Plate",
                    MeshIndices = [0, 1, 2, 3]
                }
            ],
            SourceMeshIndices = [0, 1, 2, 3],
            SourceMeshIndicesCaptured = true
        };
        model.CaptureMeshMaterialIndices();
        model.CaptureProceduralGeometry();
        return model;
    }

    private static List<Vector2> CreateOuterPolygon(ColdPlateParameters p) => p.Shape switch
    {
        ColdPlateShape.Rectangle => Rectangle(p.OuterWidth, p.OuterDepth, "底座外部尺寸"),
        ColdPlateShape.Circle => Circle(p.OuterDiameter, "底座外徑"),
        ColdPlateShape.Polygon => NormalizeCounterClockwise(p.PolygonVertices.Select(v => new Vector2(v.X, v.Y)).ToList()),
        _ => throw new ArgumentOutOfRangeException(nameof(p.Shape))
    };

    private static List<Vector2> CreateBaseInnerPolygon(ColdPlateParameters p, IReadOnlyList<Vector2> outer)
    {
        if (p.Shape == ColdPlateShape.Polygon || p.CavitySizing == CavitySizingMode.Inset)
            return OffsetConvexPolygon(outer, p.BaseInset);
        return p.Shape == ColdPlateShape.Rectangle
            ? Rectangle(p.CavityWidth, p.CavityDepthSize, "底座內部尺寸")
            : Circle(p.CavityDiameter, "底座內徑");
    }

    private static List<Vector2> CreateCoverOuterPolygon(ColdPlateParameters p, IReadOnlyList<Vector2> baseInner)
    {
        if (p.AutoFitCover || p.Shape == ColdPlateShape.Polygon)
            return baseInner.ToList();
        return p.Shape == ColdPlateShape.Rectangle
            ? Rectangle(p.CoverWidth, p.CoverDepth, "上蓋外部尺寸")
            : Circle(p.CoverDiameter, "上蓋外徑");
    }

    private static List<Vector2> CreateCoverInnerPolygon(ColdPlateParameters p, IReadOnlyList<Vector2> coverOuter)
    {
        if (p.Shape == ColdPlateShape.Polygon || p.CoverCavitySizing == CavitySizingMode.Inset)
            return OffsetConvexPolygon(coverOuter, p.CoverInset);
        return p.Shape == ColdPlateShape.Rectangle
            ? Rectangle(p.CoverCavityWidth, p.CoverCavityDepth, "上蓋內部尺寸")
            : Circle(p.CoverCavityDiameter, "上蓋內徑");
    }

    private static List<ResolvedPort> ResolvePorts(
        ColdPlateParameters p,
        IReadOnlyList<Vector2> outer,
        IReadOnlyList<Vector2> inner)
    {
        var ports = new[]
        {
            ResolvePort("進水口", p.Inlet, p.Shape, outer, inner, p.BaseHeight, p.BaseRecessDepth),
            ResolvePort("出水口", p.Outlet, p.Shape, outer, inner, p.BaseHeight, p.BaseRecessDepth)
        };
        if (ports[0].EdgeIndex == ports[1].EdgeIndex)
        {
            var distance = Math.Abs(ports[0].OuterU - ports[1].OuterU);
            if (distance < ports[0].OuterRadius + ports[1].OuterRadius)
                throw new InvalidOperationException("進水口與出水口在同一邊時不可互相重疊。");
        }
        return [.. ports];
    }

    private static ResolvedPort ResolvePort(
        string name,
        PortParameters p,
        ColdPlateShape shape,
        IReadOnlyList<Vector2> outer,
        IReadOnlyList<Vector2> inner,
        float baseHeight,
        float recessDepth)
    {
        if (shape == ColdPlateShape.Circle && (uint)p.EdgeIndex >= 4u)
            throw new InvalidOperationException($"{name}的圓形底座所在邊必須為 0=前、1=右、2=後、3=左。");
        if (shape != ColdPlateShape.Circle && (uint)p.EdgeIndex >= (uint)outer.Count)
            throw new InvalidOperationException($"{name}的所在邊索引必須介於 0 到 {outer.Count - 1}。");
        ValidatePositive(p.Extension, $"{name}伸出長度");
        ValidatePositive(p.InnerDiameter, $"{name}內徑");
        if (p.OuterDiameter <= p.InnerDiameter)
            throw new InvalidOperationException($"{name}外徑必須大於內徑。");

        var edge = shape == ColdPlateShape.Circle
            ? ((p.EdgeIndex - 1) * outer.Count / 4 + outer.Count) % outer.Count
            : p.EdgeIndex;
        var outerStart = outer[edge];
        var outerEnd = outer[(edge + 1) % outer.Count];
        var innerStart = inner[edge];
        var innerEnd = inner[(edge + 1) % inner.Count];
        var outerLength = Vector2.Distance(outerStart, outerEnd);
        var innerLength = Vector2.Distance(innerStart, innerEnd);
        var outerRadius = p.OuterDiameter * 0.5f;
        var innerRadius = p.InnerDiameter * 0.5f;
        var outerU = outerLength * 0.5f + p.HorizontalOffset;
        var innerU = innerLength * 0.5f + p.HorizontalOffset;
        if (outerU - outerRadius < 0f || outerU + outerRadius > outerLength)
            throw new InvalidOperationException($"{name}銅管外徑超出底座第 {edge} 邊範圍。");
        if (innerU - innerRadius < 0f || innerU + innerRadius > innerLength)
            throw new InvalidOperationException($"{name}開口未完整落在底座凹口的水平範圍內。");

        var cavityFloor = baseHeight - recessDepth;
        if (p.Height - innerRadius < cavityFloor || p.Height + innerRadius > baseHeight)
            throw new InvalidOperationException(
                $"{name}開口高度必須完整位於凹口範圍 {cavityFloor:0.##}～{baseHeight:0.##} mm 內。");
        if (p.Height - outerRadius < 0f || p.Height + outerRadius > baseHeight)
            throw new InvalidOperationException($"{name}銅管外徑超出底座厚度範圍。");

        var outerTangent = Vector2.Normalize(outerEnd - outerStart);
        var innerTangent = Vector2.Normalize(innerEnd - innerStart);
        var outward2 = new Vector2(outerTangent.Y, -outerTangent.X);
        var outerPoint2 = outerStart + outerTangent * outerU;
        var innerPoint2 = innerStart + innerTangent * innerU;
        var outward = Vector3.Normalize(new Vector3(outward2.X, 0f, outward2.Y));
        var innerPoint = new Vector3(innerPoint2.X, p.Height, innerPoint2.Y);
        var outerPoint = new Vector3(outerPoint2.X, p.Height, outerPoint2.Y);
        return new ResolvedPort(
            name,
            edge,
            outerU,
            innerU,
            p.Height,
            outerRadius,
            innerRadius,
            innerPoint,
            outerPoint + outward * p.Extension,
            outward);
    }

    private static MeshData BuildBase(
        IReadOnlyList<Vector2> outer,
        IReadOnlyList<Vector2> inner,
        float height,
        float recessDepth,
        IReadOnlyList<ResolvedPort> ports)
    {
        var builder = new MeshBuilder();
        var floor = height - recessDepth;
        builder.AddFace(outer, 0f, upward: false);
        builder.AddFace(inner, floor, upward: true);
        for (var edge = 0; edge < outer.Count; edge++)
        {
            var edgePorts = ports.Where(port => port.EdgeIndex == edge).ToList();
            builder.AddVerticalWall(outer[edge], outer[(edge + 1) % outer.Count], 0f, height,
                edgePorts.Select(port => new WallHole(port.OuterU, port.Height, port.InnerRadius)).ToList(),
                outward: true);
            builder.AddVerticalWall(inner[edge], inner[(edge + 1) % inner.Count], floor, height,
                edgePorts.Select(port => new WallHole(port.InnerU, port.Height, port.InnerRadius)).ToList(),
                outward: false);
            builder.AddQuad(
                To3(outer[edge], height),
                To3(inner[edge], height),
                To3(inner[(edge + 1) % inner.Count], height),
                To3(outer[(edge + 1) % outer.Count], height));
        }
        return builder.ToMesh("冷板底座", 0);
    }

    private static MeshData BuildCover(
        IReadOnlyList<Vector2> outer,
        IReadOnlyList<Vector2> inner,
        float baseHeight,
        float height,
        float recessDepth)
    {
        var builder = new MeshBuilder();
        var top = baseHeight + height;
        var ceiling = baseHeight + recessDepth;
        builder.AddFace(outer, top, upward: true);
        builder.AddFace(inner, ceiling, upward: false);
        for (var edge = 0; edge < outer.Count; edge++)
        {
            builder.AddVerticalWall(outer[edge], outer[(edge + 1) % outer.Count], baseHeight, top, [], outward: true);
            builder.AddVerticalWall(inner[edge], inner[(edge + 1) % inner.Count], baseHeight, ceiling, [], outward: false);
            builder.AddQuad(
                To3(outer[edge], baseHeight),
                To3(outer[(edge + 1) % outer.Count], baseHeight),
                To3(inner[(edge + 1) % inner.Count], baseHeight),
                To3(inner[edge], baseHeight));
        }
        return builder.ToMesh("玻璃上蓋", 1);
    }

    private static MeshData BuildTube(string name, ResolvedPort port)
    {
        var builder = new MeshBuilder();
        builder.AddHollowCylinder(port.InnerPoint, port.OuterEnd, port.OuterRadius, port.InnerRadius, 32);
        return builder.ToMesh(name, 0);
    }

    private static List<Vector2> Rectangle(float width, float depth, string name)
    {
        ValidatePositive(width, name);
        ValidatePositive(depth, name);
        var x = width * 0.5f;
        var y = depth * 0.5f;
        return [new(-x, -y), new(x, -y), new(x, y), new(-x, y)];
    }

    private static List<Vector2> Circle(float diameter, string name)
    {
        ValidatePositive(diameter, name);
        var radius = diameter * 0.5f;
        return Enumerable.Range(0, CircleSegments)
            .Select(index =>
            {
                var angle = index * MathF.Tau / CircleSegments;
                return new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
            })
            .ToList();
    }

    private static List<Vector2> OffsetConvexPolygon(IReadOnlyList<Vector2> polygon, float inset)
    {
        ValidatePositive(inset, "內縮尺寸");
        ValidateConvexPolygon(polygon, "Polygon");
        var result = new List<Vector2>(polygon.Count);
        for (var index = 0; index < polygon.Count; index++)
        {
            var previous = polygon[(index - 1 + polygon.Count) % polygon.Count];
            var current = polygon[index];
            var next = polygon[(index + 1) % polygon.Count];
            var directionA = Vector2.Normalize(current - previous);
            var directionB = Vector2.Normalize(next - current);
            var normalA = new Vector2(-directionA.Y, directionA.X);
            var normalB = new Vector2(-directionB.Y, directionB.X);
            var pointA = current + normalA * inset;
            var pointB = current + normalB * inset;
            if (!TryLineIntersection(pointA, directionA, pointB, directionB, out var intersection))
                throw new InvalidOperationException("Polygon 內縮失敗，請調整頂點或內縮尺寸。");
            result.Add(intersection);
        }
        ValidateConvexPolygon(result, "內縮後 Polygon");
        if (MathF.Abs(SignedArea(result)) >= MathF.Abs(SignedArea(polygon)))
            throw new InvalidOperationException("內縮尺寸過大或 Polygon 頂點方向無效。");
        return result;
    }

    private static bool TryLineIntersection(
        Vector2 pointA,
        Vector2 directionA,
        Vector2 pointB,
        Vector2 directionB,
        out Vector2 intersection)
    {
        var cross = Cross(directionA, directionB);
        if (MathF.Abs(cross) < 0.000001f)
        {
            intersection = default;
            return false;
        }
        var t = Cross(pointB - pointA, directionB) / cross;
        intersection = pointA + directionA * t;
        return float.IsFinite(intersection.X) && float.IsFinite(intersection.Y);
    }

    private static List<Vector2> NormalizeCounterClockwise(List<Vector2> points)
    {
        if (points.Count < 3) throw new InvalidOperationException("自訂 Polygon 至少需要 3 個頂點。");
        if (SignedArea(points) < 0f) points.Reverse();
        return points;
    }

    private static void ValidateConvexPolygon(IReadOnlyList<Vector2> polygon, string name)
    {
        if (polygon.Count < 3) throw new InvalidOperationException($"{name}至少需要 3 個頂點。");
        var sign = 0f;
        for (var index = 0; index < polygon.Count; index++)
        {
            var a = polygon[index];
            var b = polygon[(index + 1) % polygon.Count];
            var c = polygon[(index + 2) % polygon.Count];
            if (Vector2.DistanceSquared(a, b) < 0.000001f)
                throw new InvalidOperationException($"{name}包含重複頂點。");
            var cross = Cross(b - a, c - b);
            if (MathF.Abs(cross) < 0.00001f) continue;
            if (sign == 0f) sign = MathF.Sign(cross);
            else if (MathF.Sign(cross) != MathF.Sign(sign))
                throw new InvalidOperationException($"{name}必須是凸多邊形，且不可自交。");
        }
        if (MathF.Abs(SignedArea(polygon)) < 0.001f)
            throw new InvalidOperationException($"{name}面積太小。");
    }

    private static void ValidateNestedPolygons(
        IReadOnlyList<Vector2> outer,
        IReadOnlyList<Vector2> inner,
        string name)
    {
        if (outer.Count != inner.Count)
            throw new InvalidOperationException($"{name}輪廓頂點數不一致。");
        if (MathF.Abs(SignedArea(inner)) >= MathF.Abs(SignedArea(outer)))
            throw new InvalidOperationException($"{name}尺寸必須小於外部輪廓。");
    }

    private static float SignedArea(IReadOnlyList<Vector2> polygon)
    {
        var area = 0f;
        for (var index = 0; index < polygon.Count; index++)
            area += Cross(polygon[index], polygon[(index + 1) % polygon.Count]);
        return area * 0.5f;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
    private static Vector3 To3(Vector2 point, float height) => new(point.X, height, point.Y);

    private static void ValidatePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0f)
            throw new InvalidOperationException($"{name}必須大於 0。");
    }

    private static void ValidateDepth(float depth, float height, string name)
    {
        ValidatePositive(depth, name);
        if (depth >= height)
            throw new InvalidOperationException($"{name}必須小於總高度。");
    }

    private sealed record ResolvedPort(
        string Name,
        int EdgeIndex,
        float OuterU,
        float InnerU,
        float Height,
        float OuterRadius,
        float InnerRadius,
        Vector3 InnerPoint,
        Vector3 OuterEnd,
        Vector3 Outward);

    private readonly record struct WallHole(float U, float Height, float Radius);

    private sealed class MeshBuilder
    {
        private readonly List<Vector3> _positions = [];
        private readonly List<uint> _indices = [];

        public void AddFace(IReadOnlyList<Vector2> polygon, float height, bool upward)
        {
            for (var index = 1; index < polygon.Count - 1; index++)
            {
                if (upward)
                    AddTriangle(To3(polygon[0], height), To3(polygon[index + 1], height), To3(polygon[index], height));
                else
                    AddTriangle(To3(polygon[0], height), To3(polygon[index], height), To3(polygon[index + 1], height));
            }
        }

        public void AddVerticalWall(
            Vector2 start,
            Vector2 end,
            float bottom,
            float top,
            IReadOnlyList<WallHole> holes,
            bool outward)
        {
            if (holes.Count == 0)
            {
                if (outward)
                    AddQuad(To3(start, bottom), To3(start, top), To3(end, top), To3(end, bottom));
                else
                    AddQuad(To3(start, bottom), To3(end, bottom), To3(end, top), To3(start, top));
                return;
            }

            var length = Vector2.Distance(start, end);
            var minRadius = holes.Min(hole => hole.Radius);
            // Four cells across a hole radius are sufficient for a smooth opening;
            // the previous eight-cell density multiplied into thousands of tiny
            // wall quads and slowed both the WinForms preview and viewport upload.
            var cellSize = Math.Max(minRadius / 2f, 0.35f);
            var uSteps = Math.Clamp((int)MathF.Ceiling(length / cellSize), 16, 96);
            var vSteps = Math.Clamp((int)MathF.Ceiling((top - bottom) / cellSize), 10, 48);
            for (var uIndex = 0; uIndex < uSteps; uIndex++)
            for (var vIndex = 0; vIndex < vSteps; vIndex++)
            {
                var u0 = length * uIndex / uSteps;
                var u1 = length * (uIndex + 1) / uSteps;
                var v0 = bottom + (top - bottom) * vIndex / vSteps;
                var v1 = bottom + (top - bottom) * (vIndex + 1) / vSteps;
                var centerU = (u0 + u1) * 0.5f;
                var centerV = (v0 + v1) * 0.5f;
                if (holes.Any(hole =>
                        (centerU - hole.U) * (centerU - hole.U) +
                        (centerV - hole.Height) * (centerV - hole.Height) < hole.Radius * hole.Radius))
                    continue;
                var p00 = Vector2.Lerp(start, end, u0 / length);
                var p10 = Vector2.Lerp(start, end, u1 / length);
                if (outward)
                    AddQuad(To3(p00, v0), To3(p00, v1), To3(p10, v1), To3(p10, v0));
                else
                    AddQuad(To3(p00, v0), To3(p10, v0), To3(p10, v1), To3(p00, v1));
            }
        }

        public void AddHollowCylinder(Vector3 start, Vector3 end, float outerRadius, float innerRadius, int segments)
        {
            var axis = Vector3.Normalize(end - start);
            var basisA = Vector3.UnitY;
            if (MathF.Abs(Vector3.Dot(axis, basisA)) > 0.95f) basisA = Vector3.UnitX;
            basisA = Vector3.Normalize(basisA - axis * Vector3.Dot(axis, basisA));
            var basisB = Vector3.Normalize(Vector3.Cross(axis, basisA));
            for (var index = 0; index < segments; index++)
            {
                var next = (index + 1) % segments;
                var outerA = Ring(index, outerRadius);
                var outerB = Ring(next, outerRadius);
                var innerA = Ring(index, innerRadius);
                var innerB = Ring(next, innerRadius);
                AddQuad(start + outerA, end + outerA, end + outerB, start + outerB);
                AddQuad(start + innerA, start + innerB, end + innerB, end + innerA);
                AddQuad(start + innerA, start + outerA, start + outerB, start + innerB);
                AddQuad(end + innerA, end + innerB, end + outerB, end + outerA);
            }
            return;

            Vector3 Ring(int index, float radius)
            {
                var angle = index * MathF.Tau / segments;
                return (basisA * MathF.Cos(angle) + basisB * MathF.Sin(angle)) * radius;
            }
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddTriangle(a, b, c);
            AddTriangle(a, c, d);
        }

        private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var start = (uint)_positions.Count;
            _positions.Add(a);
            _positions.Add(b);
            _positions.Add(c);
            _indices.Add(start);
            _indices.Add(start + 1);
            _indices.Add(start + 2);
        }

        public MeshData ToMesh(string name, int materialIndex)
        {
            var normals = new Vector3[_positions.Count];
            for (var index = 0; index < _indices.Count; index += 3)
            {
                var a = (int)_indices[index];
                var b = (int)_indices[index + 1];
                var c = (int)_indices[index + 2];
                var normal = Vector3.Cross(_positions[b] - _positions[a], _positions[c] - _positions[a]);
                if (normal.LengthSquared() > 0.0000001f) normal = Vector3.Normalize(normal);
                normals[a] += normal;
                normals[b] += normal;
                normals[c] += normal;
            }
            for (var index = 0; index < normals.Length; index++)
                normals[index] = normals[index].LengthSquared() > 0.0000001f
                    ? Vector3.Normalize(normals[index])
                    : Vector3.UnitY;
            return new MeshData
            {
                Name = name,
                MaterialIndex = materialIndex,
                Positions = [.. _positions],
                Normals = normals,
                TextureCoordinates = new Vector2[_positions.Count],
                Tangents = new Vector4[_positions.Count],
                Indices = [.. _indices]
            };
        }
    }
}
