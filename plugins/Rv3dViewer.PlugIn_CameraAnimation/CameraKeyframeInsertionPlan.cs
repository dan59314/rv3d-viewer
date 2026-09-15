namespace Rv3dViewer.CameraAnimationPlugin;

public sealed record CameraKeyframeInsertionPlan(double TimeSeconds, CameraEasing Easing)
{
    public static CameraKeyframeInsertionPlan Create(
        CameraAnimationDocument document,
        CameraKeyframe selected)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(selected);
        var ordered = document.Keyframes.OrderBy(frame => frame.TimeSeconds).ThenBy(frame => frame.Id).ToArray();
        var index = Array.IndexOf(ordered, selected);
        if (index <= 0)
            throw new InvalidOperationException("選取的關鍵影格沒有可插入的上一筆影格。");
        var previous = ordered[index - 1];
        if (selected.TimeSeconds - previous.TimeSeconds <= double.Epsilon)
            throw new InvalidOperationException("上一筆與選取影格的時間必須不同。");
        return new CameraKeyframeInsertionPlan(
            (previous.TimeSeconds + selected.TimeSeconds) * 0.5d,
            selected.Easing);
    }
}
