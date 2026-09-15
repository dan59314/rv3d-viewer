using Rv3dViewer.ColdPlatePlugin;
using Rv3dViewer.Core;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class ColdPlateGeneratorTests
{
    [Theory]
    [InlineData(ColdPlateShape.Rectangle)]
    [InlineData(ColdPlateShape.Circle)]
    [InlineData(ColdPlateShape.Polygon)]
    public void Generate_CreatesCompleteProceduralModel(ColdPlateShape shape)
    {
        var parameters = new ColdPlateParameters { Shape = shape };

        var model = ColdPlateGenerator.Generate(parameters);

        Assert.True(model.IsProcedural);
        Assert.Equal(4, model.Meshes.Count);
        Assert.Equal(4, model.EmbeddedMeshes.Count);
        Assert.Equal(2, model.Materials.Count);
        Assert.Equal([0, 0, 0, 1], model.Meshes.Select(mesh => mesh.MaterialIndex));
        Assert.All(model.Meshes, mesh =>
        {
            Assert.NotEmpty(mesh.Positions);
            Assert.NotEmpty(mesh.Indices);
            Assert.Equal(0, mesh.Indices.Length % 3);
            Assert.All(mesh.Positions, point => Assert.True(float.IsFinite(point.X + point.Y + point.Z)));
        });
    }

    [Fact]
    public void Generate_RejectsPortOutsideCavityHeight()
    {
        var parameters = new ColdPlateParameters();
        parameters.Inlet.Height = 2f;

        var error = Assert.Throws<InvalidOperationException>(() => ColdPlateGenerator.Generate(parameters));

        Assert.Contains("進水口", error.Message);
    }

    [Fact]
    public void Generate_DefaultModelKeepsInteractiveTriangleBudget()
    {
        var model = ColdPlateGenerator.Generate(new ColdPlateParameters());

        Assert.InRange(model.Meshes.Sum(mesh => mesh.TriangleCount), 1, 6_500);
    }

    [Fact]
    public void Generate_RejectsConcaveCustomPolygon()
    {
        var parameters = new ColdPlateParameters { Shape = ColdPlateShape.Polygon };
        parameters.PolygonVertices.Clear();
        parameters.PolygonVertices.Add(new PolygonVertex { X = -60, Y = -40 });
        parameters.PolygonVertices.Add(new PolygonVertex { X = 60, Y = -40 });
        parameters.PolygonVertices.Add(new PolygonVertex { X = 0, Y = 0 });
        parameters.PolygonVertices.Add(new PolygonVertex { X = 60, Y = 40 });
        parameters.PolygonVertices.Add(new PolygonVertex { X = -60, Y = 40 });

        Assert.Throws<InvalidOperationException>(() => ColdPlateGenerator.Generate(parameters));
    }

    [Fact]
    public async Task ProceduralModel_RoundTripsEmbeddedGeometry()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"rv3d-coldplate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "coldplate.rv3dproj");
            var original = ColdPlateGenerator.Generate(new ColdPlateParameters());
            var store = new JsonProjectStore();

            await store.SaveAsync(new ViewerProject { Models = [original] }, path);
            var loaded = await store.LoadAsync(path);

            Assert.True(loaded.Models[0].IsProcedural);
            Assert.Equal(original.Meshes.Count, loaded.Models[0].Meshes.Count);
            Assert.Equal(original.Meshes.Sum(mesh => mesh.Indices.Length),
                loaded.Models[0].Meshes.Sum(mesh => mesh.Indices.Length));
            Assert.Equal(1, loaded.Models[0].Meshes[3].MaterialIndex);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
