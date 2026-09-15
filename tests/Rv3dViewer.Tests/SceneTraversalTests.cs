using System.Numerics;
using Rv3dViewer.Core;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class SceneTraversalTests
{
    [Fact]
    public void GetMeshInstances_AccumulatesNestedNodeTransforms()
    {
        var child = new SceneNode
        {
            LocalTransform = Matrix4x4.CreateTranslation(2f, 0f, 0f),
            MeshIndices = [0]
        };
        var model = CreateModel();
        model.Nodes.Add(new SceneNode
        {
            LocalTransform = Matrix4x4.CreateRotationZ(MathF.PI / 2f),
            Children = [child]
        });

        var instance = Assert.Single(SceneTraversal.GetMeshInstances(model));
        var transformed = Vector3.Transform(Vector3.UnitX, instance.NodeTransform);

        AssertVectorNearlyEqual(new Vector3(0f, 3f, 0f), transformed);
    }

    [Fact]
    public void GetMeshInstances_CreatesMultipleInstancesForSharedMesh()
    {
        var model = CreateModel();
        model.Nodes.Add(new SceneNode
        {
            Children =
            [
                new SceneNode { LocalTransform = Matrix4x4.CreateTranslation(-2f, 0f, 0f), MeshIndices = [0] },
                new SceneNode { LocalTransform = Matrix4x4.CreateTranslation(4f, 0f, 0f), MeshIndices = [0] }
            ]
        });

        var instances = SceneTraversal.GetMeshInstances(model);

        Assert.Equal(2, instances.Count);
        Assert.Equal(-2f, instances[0].NodeTransform.Translation.X);
        Assert.Equal(4f, instances[1].NodeTransform.Translation.X);
    }

    [Fact]
    public void TryCalculateBounds_AppliesNodeAndModelTransforms()
    {
        var model = CreateModel();
        model.Transform.Position = new Vector3(10f, 0f, 0f);
        model.Nodes.Add(new SceneNode
        {
            LocalTransform = Matrix4x4.CreateTranslation(0f, 5f, 0f),
            MeshIndices = [0]
        });

        var result = SceneTraversal.TryCalculateBounds(model, out var bounds);

        Assert.True(result);
        AssertVectorNearlyEqual(new Vector3(11f, 5f, 0f), bounds.Minimum);
        AssertVectorNearlyEqual(new Vector3(11f, 5f, 0f), bounds.Maximum);
    }

    [Fact]
    public void TryCalculateBounds_AppliesMeshLocalTransformBeforeNodeAndModel()
    {
        var model = CreateModel();
        model.MeshTransforms[0] = new TransformState { Position = new Vector3(2f, 0f, 0f) };
        model.Nodes.Add(new SceneNode
        {
            LocalTransform = Matrix4x4.CreateTranslation(0f, 3f, 0f),
            MeshIndices = [0]
        });
        model.Transform.Position = new Vector3(0f, 0f, 4f);

        var result = SceneTraversal.TryCalculateBounds(model, out var bounds);

        Assert.True(result);
        AssertVectorNearlyEqual(new Vector3(3f, 3f, 4f), bounds.Minimum);
        AssertVectorNearlyEqual(new Vector3(3f, 3f, 4f), bounds.Maximum);
    }

    [Fact]
    public void TryCalculateMeshBounds_ReturnsOnlyRequestedVisibleMesh()
    {
        var model = CreateModel();
        model.Meshes.Add(new MeshData { Positions = [new Vector3(10f, 0f, 0f)] });
        model.MeshTransforms[1] = new TransformState { Position = new Vector3(5f, 0f, 0f) };

        var result = SceneTraversal.TryCalculateMeshBounds(model, 1, out var bounds);

        Assert.True(result);
        AssertVectorNearlyEqual(new Vector3(15f, 0f, 0f), bounds.Minimum);
        AssertVectorNearlyEqual(new Vector3(15f, 0f, 0f), bounds.Maximum);
    }

    [Fact]
    public void GetMeshInstances_KeepsUnreferencedMeshesVisibleAndIgnoresInvalidIndices()
    {
        var model = CreateModel();
        model.Meshes.Add(new MeshData { Positions = [Vector3.Zero] });
        model.Nodes.Add(new SceneNode { MeshIndices = [-1, 5] });

        var instances = SceneTraversal.GetMeshInstances(model);

        Assert.Equal([0, 1], instances.Select(instance => instance.MeshIndex));
        Assert.All(instances, instance => Assert.Equal(Matrix4x4.Identity, instance.NodeTransform));
    }

    [Fact]
    public void TryCalculateBounds_IgnoresHiddenMeshes()
    {
        var model = CreateModel();
        model.Meshes.Add(new MeshData { Positions = [new Vector3(100f, 0f, 0f)] });
        model.HiddenMeshIndices.Add(1);

        var result = SceneTraversal.TryCalculateBounds(model, out var bounds);

        Assert.True(result);
        AssertVectorNearlyEqual(Vector3.UnitX, bounds.Minimum);
        AssertVectorNearlyEqual(Vector3.UnitX, bounds.Maximum);
    }

    [Fact]
    public void TryCalculateBounds_CombinesMultipleModels()
    {
        var left = CreateModel();
        left.Transform.Position = new Vector3(-5f, 2f, 0f);
        var right = CreateModel();
        right.Transform.Position = new Vector3(8f, -3f, 4f);

        var result = SceneTraversal.TryCalculateBounds([left, right], out var bounds);

        Assert.True(result);
        AssertVectorNearlyEqual(new Vector3(-4f, -3f, 0f), bounds.Minimum);
        AssertVectorNearlyEqual(new Vector3(9f, 2f, 4f), bounds.Maximum);
        Assert.Equal(13f, bounds.MaximumExtent);
    }

    private static SceneModel CreateModel()
    {
        var model = new SceneModel();
        model.Meshes.Add(new MeshData { Positions = [Vector3.UnitX] });
        return model;
    }

    private static void AssertVectorNearlyEqual(Vector3 expected, Vector3 actual)
    {
        Assert.InRange(actual.X, expected.X - 0.0001f, expected.X + 0.0001f);
        Assert.InRange(actual.Y, expected.Y - 0.0001f, expected.Y + 0.0001f);
        Assert.InRange(actual.Z, expected.Z - 0.0001f, expected.Z + 0.0001f);
    }
}
