using Rv3dViewer.HighQualityRenderPlugin;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class RenderOutputFileTests
{
    [Fact]
    public void ExistingRenderAndDiagnosticsArePreserved()
    {
        var directory = Path.Combine(Path.GetTempPath(), "rv3d-output-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var timestamp = new DateTime(2026, 9, 8, 13, 25, 7);
            var original = Path.Combine(directory, "HiRender_2026_132507.png");
            File.WriteAllText(original, "previous render");
            File.WriteAllText(Path.Combine(directory, "HiRender_2026_132507_1_raw.png"), "previous diagnostic");
            using (var output = RenderOutputFile.Reserve(directory, timestamp))
            {
                Assert.Equal("HiRender_2026_132507_2.png", Path.GetFileName(output.Path));
                File.WriteAllText(output.Path, "new render");
            }
            Assert.Equal("previous render", File.ReadAllText(original));
            Assert.Equal("new render", File.ReadAllText(Path.Combine(directory, "HiRender_2026_132507_2.png")));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void ConcurrentSameSecondRendersHaveUniqueNamesAndCanceledReservationsAreRemoved()
    {
        var directory = Path.Combine(Path.GetTempPath(), "rv3d-output-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var outputs = new System.Collections.Concurrent.ConcurrentBag<RenderOutputFile>();
        try
        {
            Parallel.For(0, 8, _ => outputs.Add(RenderOutputFile.Reserve(directory, new DateTime(2026, 9, 8, 13, 25, 7))));
            Assert.Equal(8, outputs.Select(o => o.Path).Distinct().Count());
            Assert.All(outputs, o => Assert.True(File.Exists(o.Path)));
            foreach (var output in outputs) output.Dispose();
            Assert.Empty(Directory.GetFiles(directory));
        }
        finally
        {
            foreach (var output in outputs) output.Dispose();
            Directory.Delete(directory, true);
        }
    }
}
