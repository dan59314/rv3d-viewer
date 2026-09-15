using System.Drawing;
using System.Numerics;
using Rv3dViewer.Core;
using Rv3dViewer.HighQualityRenderPlugin;
using Xunit;

namespace Rv3dViewer.Tests;

[Collection("Render GPU")]
public sealed class GlassColorTests
{
    [Theory]
    [InlineData(0.0000008F)]
    [InlineData(0.0008F)]
    [InlineData(0.8F)]
    public void AbsorptionUsesRelativeDistance(float thickness)
    {
        var pink = new Vector3(1, 0.43F, 0.58F);
        Assert.True(Vector3.Distance(pink, RenderOptics.BeerLambert(pink, thickness, thickness)) < 1e-6F);
        Assert.True(Vector3.Distance(pink * pink, RenderOptics.BeerLambert(pink, thickness, thickness * 2)) < 1e-6F);
        Assert.Equal(Vector3.One, RenderOptics.BeerLambert(Vector3.One, thickness, thickness * 10));
        Assert.Equal(Vector3.One, RenderOptics.BeerLambert(pink, 0, thickness));
        var boundary = RenderOptics.SurfaceTransmission(pink, 1);
        Assert.True(Vector3.Distance(pink, boundary * boundary) < 1e-6F);
        Assert.Equal(Vector3.One, RenderOptics.SurfaceTransmission(Vector3.One, 1));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    public async Task TransmittedLightHasAuthoredColor(bool gpu, bool fast, bool volumeColor)
    {
        var output = Path.Combine(Path.GetTempPath(), $"rv3d-glass-color-{Guid.NewGuid():N}.png");
        try
        {
            var white = await Render(Vector3.One);
            var pink = await Render(new Vector3(1, 0.43F, 0.58F));
            Assert.True(white.R > 180 && Math.Abs(white.R - white.G) <= 2 && Math.Abs(white.G - white.B) <= 2, white.ToString());
            Assert.True(pink.R > pink.B && pink.B > pink.G + 8, pink.ToString());
            Assert.True(pink.G < white.G - 20, $"White={white}; Pink={pink}");
        }
        finally
        {
            if (File.Exists(output)) File.Delete(output);
        }

        async Task<Color> Render(Vector3 tint)
        {
            var project = CreateGlassProject(tint, volumeColor);
            var options = new RenderOptions(16, 16, 16, 8, false, false, output,
                DenoiserMode: RenderDenoiserMode.Disabled, AdaptiveSampling: false,
                GlassQuality: fast ? RenderGlassQuality.Fast : RenderGlassQuality.Physical);
            var scene = RenderSceneSnapshot.Create(project);
            if (gpu)
            {
                var result = await GpuRenderTestThread.RenderAsync(scene, options);
                Assert.True(result.Status == GpuRenderStatus.Completed, result.Message);
            }
            else Assert.True(await OfflinePathTracer.RenderToPngAsync(scene, options, null, CancellationToken.None));
            using var bitmap = new Bitmap(output);
            return bitmap.GetPixel(8, 8);
        }
    }

    private static ViewerProject CreateGlassProject(Vector3 tint, bool volumeColor)
    {
        var glass = new PbrMaterial
        {
            RenderMode = MaterialRenderMode.Glass, Transmission = 1, Roughness = 0.001F,
            IndexOfRefraction = 1.5F, Thickness = 0.0008F,
            BaseColor = new Vector4(volumeColor ? Vector3.One : tint, 1),
            AbsorptionColor = volumeColor ? tint : Vector3.One
        };
        return new ViewerProject
        {
            Camera = new CameraState { From = new(0, 0, 0.003F), To = Vector3.Zero, Up = Vector3.UnitY, FieldOfViewDegrees = 30 },
            Models = [new SceneModel
            {
                IsProcedural = true,
                Materials = [glass, new PbrMaterial { RenderMode = MaterialRenderMode.Opaque, Emissive = Vector3.One }],
                Meshes = [Quad(0, 0, false), Quad(-0.0008F, 0, true), Quad(-0.0016F, 1, false)]
            }],
            Lights = []
        };
    }

    private static MeshData Quad(float z, int materialIndex, bool back) => new()
    {
        Positions = [new(-0.003F, -0.003F, z), new(0.003F, -0.003F, z), new(0.003F, 0.003F, z), new(-0.003F, 0.003F, z)],
        Indices = back ? [0, 2, 1, 0, 3, 2] : [0, 1, 2, 0, 2, 3],
        MaterialIndex = materialIndex
    };
}
