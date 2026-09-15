namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using Rv3dViewer.Core;

internal static class InteriorGeometryBuilder
{
    public static MeshData CreateBox(string name, Vector3 center, Vector3 size, int materialIndex = 0, float yawRadians = 0f)
    {
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "方體尺寸必須大於零。");
        var half = size / 2f;
        var min = -half;
        var max = half;
        var positions = new[]
        {
            new Vector3(min.X,min.Y,min.Z),new Vector3(max.X,min.Y,min.Z),new Vector3(max.X,max.Y,min.Z),new Vector3(min.X,max.Y,min.Z),
            new Vector3(max.X,min.Y,max.Z),new Vector3(min.X,min.Y,max.Z),new Vector3(min.X,max.Y,max.Z),new Vector3(max.X,max.Y,max.Z),
            new Vector3(min.X,min.Y,max.Z),new Vector3(min.X,min.Y,min.Z),new Vector3(min.X,max.Y,min.Z),new Vector3(min.X,max.Y,max.Z),
            new Vector3(max.X,min.Y,min.Z),new Vector3(max.X,min.Y,max.Z),new Vector3(max.X,max.Y,max.Z),new Vector3(max.X,max.Y,min.Z),
            new Vector3(min.X,max.Y,min.Z),new Vector3(max.X,max.Y,min.Z),new Vector3(max.X,max.Y,max.Z),new Vector3(min.X,max.Y,max.Z),
            new Vector3(min.X,min.Y,max.Z),new Vector3(max.X,min.Y,max.Z),new Vector3(max.X,min.Y,min.Z),new Vector3(min.X,min.Y,min.Z)
        };
        var normals = new[] { -Vector3.UnitZ, Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY }
            .SelectMany(normal => Enumerable.Repeat(normal, 4)).ToArray();
        if (MathF.Abs(yawRadians) > .000001f)
        {
            var rotation = Matrix4x4.CreateRotationY(yawRadians);
            for (var index = 0; index < positions.Length; index++)
            {
                positions[index] = Vector3.Transform(positions[index], rotation);
                normals[index] = Vector3.Normalize(Vector3.TransformNormal(normals[index], rotation));
            }
        }
        for (var index = 0; index < positions.Length; index++)
            positions[index] += center;

        var uvs = Enumerable.Range(0, 6).SelectMany(_ => new[]
        {
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0)
        }).ToArray();
        var indices = Enumerable.Range(0, 6).SelectMany(face =>
        {
            var offset = (uint)(face * 4);
            return new[] { offset, offset + 1, offset + 2, offset, offset + 2, offset + 3 };
        }).ToArray();
        var mesh = new MeshData
        {
            Name = name,
            Positions = positions,
            Normals = normals,
            TextureCoordinates = uvs,
            Tangents = Enumerable.Repeat(new Vector4(1, 0, 0, 1), positions.Length).ToArray(),
            Indices = indices,
            MaterialIndex = materialIndex
        };
        InteriorMeshGeometry.Normalize(mesh);
        return mesh;
    }

    public static MeshData CreateCylinder(string name, Vector3 center, float radius, float height,
        int segments, int materialIndex = 0)
    {
        if (!float.IsFinite(radius) || radius <= 0f)
            throw new ArgumentOutOfRangeException(nameof(radius), "圓柱半徑必須大於零。");
        if (!float.IsFinite(height) || height <= 0f)
            throw new ArgumentOutOfRangeException(nameof(height), "圓柱高度必須大於零。");
        if (segments is < 3 or > 256)
            throw new ArgumentOutOfRangeException(nameof(segments), "圓周分段數必須介於 3 到 256。 ");

        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tangents = new List<Vector4>();
        var indices = new List<uint>();
        var halfHeight = height / 2f;

        for (var index = 0; index <= segments; index++)
        {
            var angle = MathF.Tau * index / segments;
            var normal = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            var tangent = new Vector4(-MathF.Sin(angle), 0f, MathF.Cos(angle), 1f);
            positions.Add(center + new Vector3(normal.X * radius, -halfHeight, normal.Z * radius));
            positions.Add(center + new Vector3(normal.X * radius, halfHeight, normal.Z * radius));
            normals.Add(normal);
            normals.Add(normal);
            uvs.Add(new Vector2((float)index / segments, 1f));
            uvs.Add(new Vector2((float)index / segments, 0f));
            tangents.Add(tangent);
            tangents.Add(tangent);
        }
        for (var index = 0; index < segments; index++)
        {
            var offset = (uint)(index * 2);
            indices.AddRange([offset, offset + 1, offset + 3, offset, offset + 3, offset + 2]);
        }

        AddCap(-halfHeight, -Vector3.UnitY, false);
        AddCap(halfHeight, Vector3.UnitY, true);
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

        void AddCap(float y, Vector3 normal, bool top)
        {
            var centerIndex = (uint)positions.Count;
            positions.Add(center + new Vector3(0f, y, 0f));
            normals.Add(normal);
            uvs.Add(new Vector2(.5f, .5f));
            tangents.Add(new Vector4(1f, 0f, 0f, 1f));
            for (var index = 0; index < segments; index++)
            {
                var angle = MathF.Tau * index / segments;
                positions.Add(center + new Vector3(MathF.Cos(angle) * radius, y, MathF.Sin(angle) * radius));
                normals.Add(normal);
                uvs.Add(new Vector2(.5f + MathF.Cos(angle) * .5f, .5f + MathF.Sin(angle) * .5f));
                tangents.Add(new Vector4(1f, 0f, 0f, 1f));
            }
            for (var index = 0; index < segments; index++)
            {
                var current = centerIndex + 1u + (uint)index;
                var next = centerIndex + 1u + (uint)((index + 1) % segments);
                if (top)
                    indices.AddRange([centerIndex, next, current]);
                else
                    indices.AddRange([centerIndex, current, next]);
            }
        }
    }
}
