using Rv3dViewer.Core;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class SceneModelMeshPersistenceTests
{
    [Fact]
    public void RemoveMeshes_PreservesSourceIndicesAndMaterialAssignments()
    {
        var model = CreateImportedModel();
        model.Meshes[0].MaterialIndex = 2;
        model.Meshes[1].MaterialIndex = 1;
        model.Meshes[2].MaterialIndex = 0;
        model.CaptureMeshMaterialIndices();

        model.RemoveMeshes([1]);

        Assert.Equal([0, 2], model.SourceMeshIndices);
        Assert.Equal([2, 0], model.Meshes.Select(mesh => mesh.MaterialIndex));
        Assert.Equal(new Dictionary<int, int> { [0] = 2, [1] = 0 }, model.MeshMaterialIndices);
        Assert.Equal([0, 1], model.Nodes[0].MeshIndices);
    }

    [Fact]
    public void RestoreImportedGeometry_UsesPersistedSourceIndicesBeforeRestoringMaterials()
    {
        var imported = CreateImportedModel();
        var saved = new SceneModel
        {
            SourceMeshIndices = [0, 2],
            SourceMeshIndicesCaptured = true,
            MeshMaterialIndices = new Dictionary<int, int> { [0] = 2, [1] = 0 }
        };

        saved.RestoreImportedGeometry(imported);

        Assert.Equal(["Mesh 0", "Mesh 2"], saved.Meshes.Select(mesh => mesh.Name));
        Assert.Equal([2, 0], saved.Meshes.Select(mesh => mesh.MaterialIndex));
        Assert.Equal([0, 1], saved.Nodes[0].MeshIndices);
    }

    [Fact]
    public void RestoreImportedGeometry_KeepsAllMeshesDeletedWhenCapturedSelectionIsEmpty()
    {
        var imported = CreateImportedModel();
        var saved = new SceneModel { SourceMeshIndicesCaptured = true };

        saved.RestoreImportedGeometry(imported);

        Assert.Empty(saved.Meshes);
        Assert.Empty(saved.Nodes[0].MeshIndices);
    }

    private static SceneModel CreateImportedModel() => new()
    {
        Meshes =
        [
            new MeshData { Name = "Mesh 0", MaterialIndex = 0 },
            new MeshData { Name = "Mesh 1", MaterialIndex = 1 },
            new MeshData { Name = "Mesh 2", MaterialIndex = 2 }
        ],
        SourceMeshIndices = [0, 1, 2],
        SourceMeshIndicesCaptured = true,
        Nodes = [new SceneNode { MeshIndices = [0, 1, 2] }]
    };
}
