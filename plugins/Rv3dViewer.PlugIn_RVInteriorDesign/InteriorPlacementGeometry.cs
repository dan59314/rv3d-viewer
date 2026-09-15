namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using System.Runtime.CompilerServices;
using Rv3dViewer.Core;

/// <summary>
/// Builds lightweight collision proxies from disconnected mesh parts.  Furniture
/// such as a table is therefore represented by its top and legs instead of one
/// large box that also occupies all of the empty space below the top.
/// </summary>
internal static class InteriorPlacementGeometry
{
    private const int MaximumComponentCount = 96;
    private static readonly ConditionalWeakTable<MeshData, LocalCollisionProxySet> ProxyCache = new();
    private static readonly ConditionalWeakTable<SceneModel, WorldGeometryCache> WorldCache = new();

    internal static IReadOnlyList<SceneBounds> GetWorldCollisionBounds(SceneModel model)
    {
        var fingerprint = CreateFingerprint(model);
        var cache = WorldCache.GetOrCreateValue(model);
        if (cache.CollisionFingerprint == fingerprint && cache.CollisionBounds is not null)
            return cache.CollisionBounds;

        var result = new List<SceneBounds>();
        foreach (var instance in SceneTraversal.GetMeshInstances(model))
        {
            if ((uint)instance.MeshIndex >= (uint)model.Meshes.Count ||
                model.HiddenMeshIndices.Contains(instance.MeshIndex))
                continue;

            var mesh = model.Meshes[instance.MeshIndex];
            var proxies = ProxyCache.GetValue(mesh, BuildLocalCollisionProxies).Bounds;
            var transform = SceneTraversal.GetMeshWorldTransform(model, instance);
            foreach (var proxy in proxies)
                if (TryTransformBounds(proxy, transform, out var worldBounds))
                    result.Add(worldBounds);
        }

        cache.CollisionFingerprint = fingerprint;
        cache.CollisionBounds = result;
        return result;
    }

    internal static bool LooksLikeBroadHorizontalSlab(SceneModel model, SceneBounds bounds)
    {
        var size = bounds.Size;
        if (size.X < .75f || size.Z < .75f || size.Y > MathF.Max(.25f, MathF.Min(size.X, size.Z) * .12f))
            return false;
        return GetSurfaceAnalysis(model, bounds).BroadHorizontalSlab;
    }

    internal static bool HasUsableTopSurface(SceneModel model, SceneBounds bounds)
    {
        var size = bounds.Size;
        if (size.X < .08f || size.Z < .08f)
            return false;
        return GetSurfaceAnalysis(model, bounds).UsableTopSurface;
    }

    private static SurfaceAnalysis GetSurfaceAnalysis(SceneModel model, SceneBounds bounds)
    {
        var fingerprint = CreateFingerprint(model);
        var cache = WorldCache.GetOrCreateValue(model);
        if (cache.SurfaceFingerprint == fingerprint && cache.SurfaceBounds == bounds &&
            cache.SurfaceAnalysis is { } cached)
            return cached;

        var size = bounds.Size;
        var analysis = new SurfaceAnalysis(
            HasUpwardSurface(model, bounds.Maximum.Y, MathF.Max(.03f, size.Y * .35f), .2f),
            HasUpwardSurface(model, bounds.Maximum.Y, MathF.Max(.05f, size.Y * .2f), .01f));
        cache.SurfaceFingerprint = fingerprint;
        cache.SurfaceBounds = bounds;
        cache.SurfaceAnalysis = analysis;
        return analysis;
    }

    private static bool HasUpwardSurface(SceneModel model, float top, float topBand, float minimumArea)
    {
        var area = 0f;
        foreach (var instance in SceneTraversal.GetMeshInstances(model))
        {
            if ((uint)instance.MeshIndex >= (uint)model.Meshes.Count ||
                model.HiddenMeshIndices.Contains(instance.MeshIndex))
                continue;
            var mesh = model.Meshes[instance.MeshIndex];
            var transform = SceneTraversal.GetMeshWorldTransform(model, instance);
            for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
            {
                var i0 = mesh.Indices[index];
                var i1 = mesh.Indices[index + 1];
                var i2 = mesh.Indices[index + 2];
                if (i0 >= mesh.Positions.Length || i1 >= mesh.Positions.Length || i2 >= mesh.Positions.Length)
                    continue;
                var a = Vector3.Transform(mesh.Positions[i0], transform);
                var b = Vector3.Transform(mesh.Positions[i1], transform);
                var c = Vector3.Transform(mesh.Positions[i2], transform);
                var cross = Vector3.Cross(b - a, c - a);
                if (cross.LengthSquared() < .00000001f)
                    continue;
                // Imported assets are not guaranteed to use the same triangle winding.
                // Either winding still represents a usable horizontal physical surface.
                var normalY = MathF.Abs(cross.Y) / cross.Length();
                var triangleTop = MathF.Max(a.Y, MathF.Max(b.Y, c.Y));
                if (normalY < .8f || triangleTop < top - topBand)
                    continue;
                area += MathF.Abs(Vector3.Cross(b - a, c - a).Y) * .5f;
                if (area >= minimumArea)
                    return true;
            }
        }
        return false;
    }

    private static LocalCollisionProxySet BuildLocalCollisionProxies(MeshData mesh)
    {
        if (mesh.Positions.Length == 0)
            return new LocalCollisionProxySet([]);

        var parent = Enumerable.Range(0, mesh.Positions.Length).ToArray();
        for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
        {
            var i0 = mesh.Indices[index];
            var i1 = mesh.Indices[index + 1];
            var i2 = mesh.Indices[index + 2];
            if (i0 >= parent.Length || i1 >= parent.Length || i2 >= parent.Length)
                continue;
            Union(parent, (int)i0, (int)i1);
            Union(parent, (int)i1, (int)i2);
        }

        var groups = new Dictionary<int, BoundsBuilder>();
        for (var index = 0; index < mesh.Positions.Length; index++)
        {
            var root = Find(parent, index);
            if (!groups.TryGetValue(root, out var builder))
                builder = new BoundsBuilder();
            builder.Include(mesh.Positions[index]);
            groups[root] = builder;
            if (groups.Count > MaximumComponentCount)
                return new LocalCollisionProxySet([CalculateBounds(mesh.Positions)]);
        }

        var bounds = groups.Values.Where(item => item.HasPoint).Select(item => item.Bounds).ToArray();
        return new LocalCollisionProxySet(bounds.Length == 0 ? [CalculateBounds(mesh.Positions)] : bounds);
    }

    private static int Find(int[] parent, int value)
    {
        while (parent[value] != value)
        {
            parent[value] = parent[parent[value]];
            value = parent[value];
        }
        return value;
    }

    private static void Union(int[] parent, int first, int second)
    {
        var firstRoot = Find(parent, first);
        var secondRoot = Find(parent, second);
        if (firstRoot != secondRoot)
            parent[secondRoot] = firstRoot;
    }

    private static SceneBounds CalculateBounds(IEnumerable<Vector3> positions)
    {
        var builder = new BoundsBuilder();
        foreach (var position in positions)
            builder.Include(position);
        return builder.Bounds;
    }

    private static bool TryTransformBounds(SceneBounds local, Matrix4x4 transform, out SceneBounds world)
    {
        var builder = new BoundsBuilder();
        for (var x = 0; x < 2; x++)
        for (var y = 0; y < 2; y++)
        for (var z = 0; z < 2; z++)
        {
            var corner = new Vector3(
                x == 0 ? local.Minimum.X : local.Maximum.X,
                y == 0 ? local.Minimum.Y : local.Maximum.Y,
                z == 0 ? local.Minimum.Z : local.Maximum.Z);
            var transformed = Vector3.Transform(corner, transform);
            if (float.IsFinite(transformed.X) && float.IsFinite(transformed.Y) && float.IsFinite(transformed.Z))
                builder.Include(transformed);
        }
        world = builder.Bounds;
        return builder.HasPoint;
    }

    private static GeometryFingerprint CreateFingerprint(SceneModel model)
    {
        var detailHash = new HashCode();
        detailHash.Add(model.Meshes.Count);
        foreach (var mesh in model.Meshes)
        {
            detailHash.Add(RuntimeHelpers.GetHashCode(mesh));
            detailHash.Add(mesh.Positions.Length);
            detailHash.Add(mesh.Indices.Length);
        }
        foreach (var pair in model.MeshTransforms.OrderBy(pair => pair.Key))
        {
            detailHash.Add(pair.Key);
            detailHash.Add(pair.Value.Matrix);
        }
        foreach (var index in model.HiddenMeshIndices.Order())
            detailHash.Add(index);
        return new GeometryFingerprint(model.Transform.Matrix, detailHash.ToHashCode());
    }

    private sealed record LocalCollisionProxySet(IReadOnlyList<SceneBounds> Bounds);
    private readonly record struct GeometryFingerprint(Matrix4x4 ModelTransform, int DetailHash);
    private readonly record struct SurfaceAnalysis(bool BroadHorizontalSlab, bool UsableTopSurface);

    private sealed class WorldGeometryCache
    {
        internal GeometryFingerprint CollisionFingerprint { get; set; }
        internal IReadOnlyList<SceneBounds>? CollisionBounds { get; set; }
        internal GeometryFingerprint SurfaceFingerprint { get; set; }
        internal SceneBounds SurfaceBounds { get; set; }
        internal SurfaceAnalysis? SurfaceAnalysis { get; set; }
    }

    private struct BoundsBuilder
    {
        private Vector3 _minimum;
        private Vector3 _maximum;
        internal bool HasPoint { get; private set; }
        internal SceneBounds Bounds => new(_minimum, _maximum);

        internal void Include(Vector3 point)
        {
            if (!HasPoint)
            {
                _minimum = point;
                _maximum = point;
                HasPoint = true;
                return;
            }
            _minimum = Vector3.Min(_minimum, point);
            _maximum = Vector3.Max(_maximum, point);
        }
    }
}
