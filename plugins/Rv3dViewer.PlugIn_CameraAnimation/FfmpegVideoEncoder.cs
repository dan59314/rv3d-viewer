using System.ComponentModel;
using System.Diagnostics;

namespace Rv3dViewer.CameraAnimationPlugin;

internal sealed class FfmpegVideoEncoder : IAsyncDisposable
{
    private readonly Process _process;
    private readonly Task<string> _errorTask;
    private bool _completed;

    private FfmpegVideoEncoder(Process process)
    {
        _process = process;
        _errorTask = process.StandardError.ReadToEndAsync();
    }

    public static string? FindExecutable()
    {
        var fileName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(directory => Path.Combine(directory.Trim('"'), fileName))
            .FirstOrDefault(File.Exists);
    }

    public static FfmpegVideoEncoder Start(string executable, string outputPath, int framesPerSecond)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[]
                 {
                     "-hide_banner", "-loglevel", "error", "-y",
                     "-f", "image2pipe", "-framerate", framesPerSecond.ToString(),
                     "-vcodec", "png", "-i", "pipe:0", "-an",
                     "-c:v", "libx264", "-pix_fmt", "yuv420p", "-movflags", "+faststart",
                     outputPath
                 })
            startInfo.ArgumentList.Add(argument);

        try
        {
            return new FfmpegVideoEncoder(Process.Start(startInfo) ??
                throw new InvalidOperationException("無法啟動 FFmpeg。"));
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"無法啟動 FFmpeg：{ex.Message}", ex);
        }
    }

    public Task WriteFrameAsync(byte[] png, CancellationToken cancellationToken) =>
        _process.StandardInput.BaseStream.WriteAsync(png, cancellationToken).AsTask();

    public async Task CompleteAsync(CancellationToken cancellationToken)
    {
        await _process.StandardInput.BaseStream.FlushAsync(cancellationToken);
        _process.StandardInput.Close();
        await _process.WaitForExitAsync(cancellationToken);
        var error = await _errorTask;
        _completed = true;
        if (_process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? $"FFmpeg 編碼失敗（Exit Code {_process.ExitCode}）。"
                : $"FFmpeg 編碼失敗：{error.Trim()}");
    }

    public async ValueTask DisposeAsync()
    {
        if (!_completed && !_process.HasExited)
        {
            try { _process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        }
        try { await _process.WaitForExitAsync(); }
        catch (InvalidOperationException) { }
        _process.Dispose();
    }
}
