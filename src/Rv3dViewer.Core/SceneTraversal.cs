using System.Numerics;

namespace Rv3dViewer.Core;

public readonly record struct SceneMeshInstance(int MeshIndex, Matrix4x4 NodeTransform);

public readonly record struct SceneBounds(Vector3 Minimum, Vector3 Maximum)
{
    public Vector3 Center => (Minimum + Maximum) * 0.5f;
    public Vector3 Size => Maximum - Minimum;
    public float Radius => (Maximum - Minimum).Length() * 0.5f;
    public float MaximumExtent => Math.Max(Size.X, Math.Max(Size.Y, Size.Z));
}

public static class SceneTraversal
{
    public static IReadOnlyList<SceneMeshInstance> GetMeshInstances(SceneModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var instances = new List<SceneMeshInstance>();
        var referencedMeshes = new HashSet<int>();
        var ancestors = new HashSet<SceneNode>(ReferenceEqualityComparer.Instance);

        foreach (var root in model.Nodes)
            Traverse(root, Matrix4x4.Identity, model.Meshes.Count, instances, referencedMeshes, ancestors);

        // Some formats omit a node reference for otherwise valid meshes. Keep them visible.
        for (var meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
        {
            if (!referencedMeshes.Contains(meshIndex))
                instances.Add(new SceneMeshInstance(meshIndex, Matrix4x4.Identity));
        }

        return instances;
    }

    public static bool TryCalculateBounds(SceneModel model, out SceneBounds bounds)
    {
        ArgumentNullException.ThrowIfNull(model);

        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        var hasPosition = false;
        var modelTransform = model.Transform.Matrix;

        foreach (var instance in GetMeshInstances(model))
        {
            if (model.HiddenMeshIndices.Contains(instance.MeshIndex))
                continue;

            var meshTransform = GetMeshWorldTransform(model, instance, modelTransform);
            foreach (var position in model.Meshes[instance.MeshIndex].Positions)
            {
                var world = Vector3.Transform(position, meshTransform);
                minimum = Vector3.Min(minimum, world);
                maximum = Vector3.Max(maximum, world);
                hasPosition = true;
            }
        }

        bounds = hasPosition ? new SceneBounds(minimum, maximum) : default;
        return hasPosition;
    }

    public static bool TryCalculateBounds(IEnumerable<SceneModel> models, out SceneBounds bounds)
    {
        ArgumentNullException.ThrowIfNull(models);
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        var hasBounds = false;
        foreach (var model in models)
        {
            if (!TryCalculateBounds(model, out var modelBounds)) continue;
            minimum = Vector3.Min(minimum, modelBounds.Minimum);
            maximum = Vector3.Max(maximum, modelBounds.Maximum);
            hasBounds = true;
        }
        bounds = hasBounds ? new SceneBounds(minimum, maximum) : default;
        return hasBounds;
    }

    public static bool TryCalculateMeshBounds(SceneModel model, int meshIndex, out SceneBounds bounds)
    {
        ArgumentNullException.ThrowIfNull(model);
        if ((uint)meshIndex >= (uint)model.Meshes.Count || model.HiddenMeshIndices.Contains(meshIndex))
        {
            bounds = default;
            return false;
        }

        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        var hasPosition = false;
        foreach (var instance in GetMeshInstances(model).Where(instance => instance.MeshIndex == meshIndex))
        {
            var transform = GetMeshWorldTransform(model, instance, model.Transform.Matrix);
            foreach (var position in model.Meshes[meshIndex].Positions)
            {
                var world = Vector3.Transform(position, transform);
                minimum = Vector3.Min(minimum, world);
                maximum = Vector3.Max(maximum, world);
                hasPosition = true;
            }
        }
        bounds = hasPosition ? new SceneBounds(minimum, maximum) : default;
        return hasPosition;
    }

    public static Matrix4x4 GetMeshWorldTransform(SceneModel model, SceneMeshInstance instance) =>
        GetMeshWorldTransform(model, instance, model.Transform.Matrix);

    private static Matrix4x4 GetMeshWorldTransform(SceneModel model, SceneMeshInstance instance, Matrix4x4 modelTransform)
    {
        var localTransform = model.MeshTransforms.TryGetValue(instance.MeshIndex, out var transform)
            ? transform.Matrix
            : Matrix4x4.Identity;
        return localTransform * instance.NodeTransform * modelTransform;
    }

    private static void Traverse(
        SceneNode node,
        Matrix4x4 parentTransform,
        int meshCount,
        List<SceneMeshInstance> instances,
        HashSet<int> referencedMeshes,
        HashSet<SceneNode> ancestors)
    {
        if (!ancestors.Add(node))
            return;

        // System.Numerics uses row-vector composition: local is applied before parent.
        var nodeTransform = node.LocalTransform * parentTransform;
        foreach (var meshIndex in node.MeshIndices)
        {
            if ((uint)meshIndex >= (uint)meshCount)
                continue;

            instances.Add(new SceneMeshInstance(meshIndex, nodeTransform));
            referencedMeshes.Add(meshIndex);
        }

        foreach (var child in node.Children)
            Traverse(child, nodeTransform, meshCount, instances, referencedMeshes, ancestors);

        ancestors.Remove(node);
    }
}
