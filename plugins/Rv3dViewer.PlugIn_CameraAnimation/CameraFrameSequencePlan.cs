namespace Rv3dViewer.CameraAnimationPlugin;

public sealed class CameraFrameSequencePlan
{
    private CameraFrameSequencePlan(double durationSeconds, int framesPerSecond, int frameCount)
    {
        DurationSeconds = durationSeconds;
        FramesPerSecond = framesPerSecond;
        FrameCount = frameCount;
        FileNameDigits = Math.Max(6, (frameCount - 1).ToString().Length);
    }

    public double DurationSeconds { get; }
    public int FramesPerSecond { get; }
    public int FrameCount { get; }
    public int FileNameDigits { get; }

    public static CameraFrameSequencePlan Create(double durationSeconds, int framesPerSecond)
    {
        if (!double.IsFinite(durationSeconds) || durationSeconds <= 0d)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        if (framesPerSecond is < 1 or > 240)
            throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
        var count = checked((int)Math.Ceiling(durationSeconds * framesPerSecond) + 1);
        return new CameraFrameSequencePlan(durationSeconds, framesPerSecond, count);
    }

    public double GetTimeSeconds(int frameIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frameIndex);
        if (frameIndex >= FrameCount) throw new ArgumentOutOfRangeException(nameof(frameIndex));
        return Math.Min(frameIndex / (double)FramesPerSecond, DurationSeconds);
    }

    public string GetFileName(int frameIndex)
    {
        _ = GetTimeSeconds(frameIndex);
        return $"frame_{frameIndex.ToString($"D{FileNameDigits}")}.png";
    }
}
