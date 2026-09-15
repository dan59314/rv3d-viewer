using System.Globalization;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal sealed class RenderOutputFile : IDisposable
{
    private RenderOutputFile(string path) => Path = path;
    internal string Path { get; }

    internal static string NextPath(string directory, DateTime timestamp)
    {
        var stem = "HiRender_" + timestamp.ToString("yyyy_HHmmss", CultureInfo.InvariantCulture);
        for (var index = 0; ; index++)
        {
            var name = index == 0 ? stem : $"{stem}_{index}";
            var path = System.IO.Path.Combine(directory, name + ".png");
            var diagnostics = GpuRenderDiagnosticPaths.FromOutputPath(path);
            if (!Exists(path) && !Exists(diagnostics.RawPath) && !Exists(diagnostics.DenoisedPath) &&
                !Exists(diagnostics.VariancePath) && !Exists(diagnostics.SummaryPath)) return path;
        }
    }

    internal static RenderOutputFile Reserve(string directory, DateTime timestamp)
    {
        Directory.CreateDirectory(directory);
        while (true)
        {
            var path = NextPath(directory, timestamp);
            try
            {
                // Reserve atomically so simultaneous renders also select different names.
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                return new RenderOutputFile(path);
            }
            catch (IOException) when (Exists(path)) { }
        }
    }

    private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    public void Dispose()
    {
        // Cancellation before PNG output must not leave an empty reservation.
        try { if (new FileInfo(Path) is { Exists: true, Length: 0 }) File.Delete(Path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
