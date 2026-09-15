namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using Rv3dViewer.Core;

internal static class InteriorMeshGeometry
{
    private const float Epsilon = 1e-10f;

    internal static void Normalize(SceneModel model, bool preserveWinding = false)
    {
        ArgumentNullException.ThrowIfNull(model);
        foreach (var mesh in model.Meshes)
            Normalize(mesh, preserveWinding);
        model.CaptureProceduralGeometry();
    }

    internal static void Normalize(MeshData mesh, bool preserveWinding = false)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (mesh.Positions.Length == 0 || mesh.Indices.Length < 3)
            return;

        var hasNormals = mesh.Normals.Length == mesh.Positions.Length;
        var repairedIndices = new List<uint>(mesh.Indices.Length);
        for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
        {
            var i0 = mesh.Indices[index];
            var i1 = mesh.Indices[index + 1];
            var i2 = mesh.Indices[index + 2];
            if (i0 >= mesh.Positions.Length || i1 >= mesh.Positions.Length || i2 >= mesh.Positions.Length)
                continue;

            var edge1 = mesh.Positions[i1] - mesh.Positions[i0];
            var edge2 = mesh.Positions[i2] - mesh.Positions[i0];
            var faceNormal = Vector3.Cross(edge1, edge2);
            var edgeScaleSquared = MathF.Max(edge1.LengthSquared(), edge2.LengthSquared());
            var relativeAreaTolerance = edgeScaleSquared * edgeScaleSquared * 1e-12f;
            var faceAreaSquared = faceNormal.LengthSquared();
            if (!float.IsFinite(faceAreaSquared) || faceAreaSquared <= MathF.Max(float.Epsilon,
                    relativeAreaTolerance))
                continue;

            if (hasNormals && !preserveWinding)
            {
                var vertexNormal = mesh.Normals[i0] + mesh.Normals[i1] + mesh.Normals[i2];
                if (vertexNormal.LengthSquared() > Epsilon && Vector3.Dot(faceNormal, vertexNormal) < 0f)
                    (i1, i2) = (i2, i1);
            }
            repairedIndices.AddRange([i0, i1, i2]);
        }
        mesh.Indices = repairedIndices.ToArray();

        if (!hasNormals)
            mesh.Normals = BuildNormals(mesh);
        else
            NormalizeNormals(mesh.Normals);
        mesh.Tangents = BuildTangents(mesh);
    }

    private static Vector3[] BuildNormals(MeshData mesh)
    {
        var normals = new Vector3[mesh.Positions.Length];
        for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
        {
            var i0 = mesh.Indices[index];
            var i1 = mesh.Indices[index + 1];
            var i2 = mesh.Indices[index + 2];
            var normal = Vector3.Cross(mesh.Positions[i1] - mesh.Positions[i0],
                mesh.Positions[i2] - mesh.Positions[i0]);
            normals[i0] += normal;
            normals[i1] += normal;
            normals[i2] += normal;
        }
        NormalizeNormals(normals);
        return normals;
    }

    private static void NormalizeNormals(Vector3[] normals)
    {
        for (var index = 0; index < normals.Length; index++)
            normals[index] = normals[index].LengthSquared() > Epsilon
                ? Vector3.Normalize(normals[index])
                : Vector3.UnitY;
    }

    private static Vector4[] BuildTangents(MeshData mesh)
    {
        var vertexCount = mesh.Positions.Length;
        var tangents = new Vector3[vertexCount];
        var bitangents = new Vector3[vertexCount];
        var hasUvs = mesh.TextureCoordinates.Length == vertexCount;
        if (hasUvs)
        {
            for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
            {
                var i0 = mesh.Indices[index];
                var i1 = mesh.Indices[index + 1];
                var i2 = mesh.Indices[index + 2];
                var edge1 = mesh.Positions[i1] - mesh.Positions[i0];
                var edge2 = mesh.Positions[i2] - mesh.Positions[i0];
                var uv1 = mesh.TextureCoordinates[i1] - mesh.TextureCoordinates[i0];
                var uv2 = mesh.TextureCoordinates[i2] - mesh.TextureCoordinates[i0];
                var denominator = uv1.X * uv2.Y - uv1.Y * uv2.X;
                if (MathF.Abs(denominator) <= Epsilon)
                    continue;
                var inverse = 1f / denominator;
                var tangent = (edge1 * uv2.Y - edge2 * uv1.Y) * inverse;
                var bitangent = (edge2 * uv1.X - edge1 * uv2.X) * inverse;
                tangents[i0] += tangent;
                tangents[i1] += tangent;
                tangents[i2] += tangent;
                bitangents[i0] += bitangent;
                bitangents[i1] += bitangent;
                bitangents[i2] += bitangent;
            }
        }

        var result = new Vector4[vertexCount];
        for (var index = 0; index < vertexCount; index++)
        {
            var normal = mesh.Normals[index];
            var tangent = tangents[index] - normal * Vector3.Dot(normal, tangents[index]);
            if (tangent.LengthSquared() <= Epsilon)
            {
                var reference = MathF.Abs(normal.Y) < .9f ? Vector3.UnitY : Vector3.UnitX;
                tangent = Vector3.Cross(reference, normal);
            }
            tangent = Vector3.Normalize(tangent);
            var handedness = bitangents[index].LengthSquared() > Epsilon &&
                             Vector3.Dot(Vector3.Cross(normal, tangent), bitangents[index]) < 0f
                ? -1f
                : 1f;
            result[index] = new Vector4(tangent, handedness);
        }
        return result;
    }
}
