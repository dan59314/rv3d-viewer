using System.Text;
using Rv3dViewer.App;
using Rv3dViewer.Rendering.OpenGL;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class RadianceHdrLoaderTests
{
    [Fact]
    public void Load_DecodesModernRleScanline()
    {
        using var stream = new MemoryStream();
        stream.Write(Encoding.ASCII.GetBytes("#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y 1 +X 8\n"));
        stream.Write([2, 2, 0, 8]);
        stream.Write([136, 128]);
        stream.Write([136, 64]);
        stream.Write([136, 32]);
        stream.Write([136, 129]);
        stream.Position = 0;

        var image = RadianceHdrLoader.Load(stream);

        Assert.Equal(8, image.Width);
        Assert.Equal(1, image.Height);
        Assert.Equal(1f, image.Pixels[0]);
        Assert.Equal(0.5f, image.Pixels[1]);
        Assert.Equal(0.25f, image.Pixels[2]);
        Assert.All(image.Pixels.Chunk(3), pixel => Assert.Equal(image.Pixels[..3], pixel));
    }

    [Fact]
    public void DefaultStudioHdr_RoundTripsThroughRadianceLoader()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rv3d-hdr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var path = Path.Combine(tempDirectory, "studio.hdr");

            DefaultHdrService.WriteStudioHdr(path, 64, 32);
            var image = RadianceHdrLoader.Load(path);

            Assert.Equal(64, image.Width);
            Assert.Equal(32, image.Height);
            Assert.True(image.Pixels.Max() > 5f);
            Assert.True(image.Pixels.Min() >= 0f);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }
}
