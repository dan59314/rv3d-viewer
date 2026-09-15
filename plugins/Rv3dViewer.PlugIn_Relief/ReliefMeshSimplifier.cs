using System.Numerics;
using Rv3dViewer.Core;

namespace Rv3dViewer.ReliefPlugin;

internal static class ReliefMeshSimplifier
{
    internal static MeshData Simplify(MeshData source, float targetRatio, float angleDegrees, CancellationToken token)
    {
        var target = Math.Max(4, (int)MathF.Round(source.TriangleCount * Math.Clamp(targetRatio, .1f, 1f)));
        if (target >= source.TriangleCount || source.Positions.Length == 0) return source;
        var positions = (Vector3[])source.Positions.Clone();
        var uv = source.TextureCoordinates.Length == positions.Length ? (Vector2[])source.TextureCoordinates.Clone() : new Vector2[positions.Length];
        var normals = source.Normals.Length == positions.Length ? source.Normals : CalculateNormals(source);
        var parents = Enumerable.Range(0, positions.Length).ToArray();
        var protectedVertices = FindProtectedVertices(source);
        var triangles = CreateTriangles(source);
        var vertexTriangles = CreateVertexTriangleMap(positions.Length, triangles);
        var faceCosine = MathF.Cos(MathF.Max(15f, angleDegrees * 2f) * MathF.PI / 180f);
        var candidates = BuildCandidates(source, normals, MathF.Cos(angleDegrees * MathF.PI / 180f), token);
        var activeCount = source.TriangleCount;

        foreach (var candidate in candidates)
        {
            if ((activeCount & 0x1FFF) == 0) token.ThrowIfCancellationRequested();
            if (activeCount <= target) break;
            var keep = Find(parents, candidate.First);
            var remove = Find(parents, candidate.Second);
            if (keep == remove || protectedVertices[keep] || protectedVertices[remove]) continue;
            if (!CanCollapse(keep, remove, positions, triangles, vertexTriangles, faceCosine, out var affected)) continue;
            positions[keep] = (positions[keep] + positions[remove]) * .5f;
            uv[keep] = (uv[keep] + uv[remove]) * .5f;
            parents[remove] = keep;
            foreach (var triangleIndex in affected)
            {
                var triangle = triangles[triangleIndex];
                if (!triangle.Active) continue;
                RemoveReferences(triangleIndex, triangle, vertexTriangles);
                triangle.Replace(remove, keep);
                if (triangle.IsDegenerate) { triangle.Active = false; activeCount--; }
                else AddReferences(triangleIndex, triangle, vertexTriangles);
            }
            vertexTriangles[remove].Clear();
        }
        return activeCount < source.TriangleCount ? Rebuild(source, positions, uv, parents, triangles, token) : source;
    }

    private static bool CanCollapse(int keep, int remove, Vector3[] positions, MutableTriangle[] triangles,
        HashSet<int>[] vertexTriangles, float faceCosine, out int[] affected)
    {
        affected = vertexTriangles[keep].Concat(vertexTriangles[remove]).Distinct().ToArray();
        var edgeTriangles = vertexTriangles[keep].Intersect(vertexTriangles[remove]).Where(i => triangles[i].Active).ToArray();
        if (edgeTriangles.Length != 2) return false;
        var firstNeighbors = GetNeighbors(keep, triangles, vertexTriangles);
        var secondNeighbors = GetNeighbors(remove, triangles, vertexTriangles);
        firstNeighbors.Remove(remove); secondNeighbors.Remove(keep);
        if (firstNeighbors.Intersect(secondNeighbors).Count() != 2) return false;
        var replacement = (positions[keep] + positions[remove]) * .5f;
        foreach (var index in affected)
        {
            var triangle = triangles[index];
            if (!triangle.Active || triangle.Contains(keep) && triangle.Contains(remove)) continue;
            var oldNormal = FaceNormal(triangle, positions);
            var a = triangle.A == keep || triangle.A == remove ? replacement : positions[triangle.A];
            var b = triangle.B == keep || triangle.B == remove ? replacement : positions[triangle.B];
            var c = triangle.C == keep || triangle.C == remove ? replacement : positions[triangle.C];
            var cross = Vector3.Cross(b - a, c - a);
            if (cross.LengthSquared() <= 1e-14f || Vector3.Dot(oldNormal, Vector3.Normalize(cross)) < faceCosine) return false;
        }
        return true;
    }

    private static HashSet<int> GetNeighbors(int vertex, MutableTriangle[] triangles, HashSet<int>[] map)
    {
        var result = new HashSet<int>();
        foreach (var index in map[vertex])
        {
            var triangle = triangles[index];
            if (!triangle.Active) continue;
            result.Add(triangle.A); result.Add(triangle.B); result.Add(triangle.C);
        }
        result.Remove(vertex);
        return result;
    }

    private static List<EdgeCandidate> BuildCandidates(MeshData mesh, Vector3[] normals, float cosineLimit, CancellationToken token)
    {
        var edges = new Dictionary<(int, int), float>();
        for (var index = 0; index < mesh.Indices.Length; index += 3)
        {
            if ((index & 0x3FFFF) == 0) token.ThrowIfCancellationRequested();
            Add((int)mesh.Indices[index], (int)mesh.Indices[index + 1]);
            Add((int)mesh.Indices[index + 1], (int)mesh.Indices[index + 2]);
            Add((int)mesh.Indices[index + 2], (int)mesh.Indices[index]);
        }
        return edges.Select(pair => new EdgeCandidate(pair.Key.Item1, pair.Key.Item2, pair.Value)).OrderBy(edge => edge.Cost).ToList();
        void Add(int first, int second)
        {
            if (first == second) return;
            if (first > second) (first, second) = (second, first);
            var dot = Vector3.Dot(Vector3.Normalize(normals[first]), Vector3.Normalize(normals[second]));
            if (!float.IsFinite(dot) || dot < cosineLimit) return;
            edges.TryAdd((first, second), Vector3.DistanceSquared(mesh.Positions[first], mesh.Positions[second]) * (2f - dot));
        }
    }

    private static bool[] FindProtectedVertices(MeshData mesh)
    {
        var counts = new Dictionary<(int, int), int>();
        for (var index = 0; index < mesh.Indices.Length; index += 3)
        { Add((int)mesh.Indices[index], (int)mesh.Indices[index + 1]); Add((int)mesh.Indices[index + 1], (int)mesh.Indices[index + 2]); Add((int)mesh.Indices[index + 2], (int)mesh.Indices[index]); }
        var result = new bool[mesh.Positions.Length];
        foreach (var (edge, count) in counts) if (count != 2) result[edge.Item1] = result[edge.Item2] = true;
        var minimum = 0; var maximum = 0;
        for (var index = 1; index < mesh.Positions.Length; index++)
        { if (mesh.Positions[index].Y < mesh.Positions[minimum].Y) minimum = index; if (mesh.Positions[index].Y > mesh.Positions[maximum].Y) maximum = index; }
        result[minimum] = result[maximum] = true;
        return result;
        void Add(int a, int b) { if (a > b) (a, b) = (b, a); counts[(a, b)] = counts.GetValueOrDefault((a, b)) + 1; }
    }

    private static MutableTriangle[] CreateTriangles(MeshData mesh)
    {
        var result = new MutableTriangle[mesh.TriangleCount];
        for (var i = 0; i < result.Length; i++) result[i] = new((int)mesh.Indices[i * 3], (int)mesh.Indices[i * 3 + 1], (int)mesh.Indices[i * 3 + 2]);
        return result;
    }

    private static HashSet<int>[] CreateVertexTriangleMap(int count, MutableTriangle[] triangles)
    {
        var result = Enumerable.Range(0, count).Select(_ => new HashSet<int>()).ToArray();
        for (var i = 0; i < triangles.Length; i++) AddReferences(i, triangles[i], result);
        return result;
    }
    private static void AddReferences(int i, MutableTriangle t, HashSet<int>[] map) { map[t.A].Add(i); map[t.B].Add(i); map[t.C].Add(i); }
    private static void RemoveReferences(int i, MutableTriangle t, HashSet<int>[] map) { map[t.A].Remove(i); map[t.B].Remove(i); map[t.C].Remove(i); }

    private static MeshData Rebuild(MeshData source, Vector3[] positions, Vector2[] uv, int[] parents, MutableTriangle[] triangles, CancellationToken token)
    {
        var roots = triangles.Where(t => t.Active).SelectMany(t => new[] { Find(parents, t.A), Find(parents, t.B), Find(parents, t.C) }).Distinct().ToArray();
        var newIndex = roots.Select((root, index) => (root, index: (uint)index)).ToDictionary(item => item.root, item => item.index);
        var indices = new List<uint>();
        foreach (var triangle in triangles.Where(t => t.Active))
        {
            if ((indices.Count & 0x3FFFF) == 0) token.ThrowIfCancellationRequested();
            indices.Add(newIndex[Find(parents, triangle.A)]); indices.Add(newIndex[Find(parents, triangle.B)]); indices.Add(newIndex[Find(parents, triangle.C)]);
        }
        var result = new MeshData { Name = source.Name, Positions = roots.Select(r => positions[r]).ToArray(), TextureCoordinates = roots.Select(r => uv[r]).ToArray(), Indices = indices.ToArray(), MaterialIndex = source.MaterialIndex };
        result.Normals = CalculateNormals(result);
        return result;
    }

    private static Vector3 FaceNormal(MutableTriangle t, Vector3[] p) => Vector3.Normalize(Vector3.Cross(p[t.B] - p[t.A], p[t.C] - p[t.A]));
    private static Vector3[] CalculateNormals(MeshData mesh)
    {
        var result = new Vector3[mesh.Positions.Length];
        for (var i = 0; i < mesh.Indices.Length; i += 3)
        { var a = (int)mesh.Indices[i]; var b = (int)mesh.Indices[i + 1]; var c = (int)mesh.Indices[i + 2]; var n = Vector3.Cross(mesh.Positions[b] - mesh.Positions[a], mesh.Positions[c] - mesh.Positions[a]); result[a] += n; result[b] += n; result[c] += n; }
        for (var i = 0; i < result.Length; i++) result[i] = result[i].LengthSquared() > 1e-12f ? Vector3.Normalize(result[i]) : Vector3.UnitY;
        return result;
    }
    private static int Find(int[] parents, int value) { while (parents[value] != value) { parents[value] = parents[parents[value]]; value = parents[value]; } return value; }

    private sealed class MutableTriangle(int a, int b, int c)
    {
        public int A = a, B = b, C = c; public bool Active = true;
        public bool IsDegenerate => A == B || B == C || C == A;
        public bool Contains(int vertex) => A == vertex || B == vertex || C == vertex;
        public void Replace(int oldValue, int newValue) { if (A == oldValue) A = newValue; if (B == oldValue) B = newValue; if (C == oldValue) C = newValue; }
    }
    private readonly record struct EdgeCandidate(int First, int Second, float Cost);
}
