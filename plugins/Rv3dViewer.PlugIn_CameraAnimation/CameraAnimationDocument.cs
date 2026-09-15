using System.Numerics;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Rv3dViewer.Core;

namespace Rv3dViewer.CameraAnimationPlugin;

public sealed class CameraAnimationDocument
{
    public const int CurrentVersion = 4;
    public int Version { get; set; } = CurrentVersion;
    public string Name { get; set; } = "Camera Animation";
    public double DurationSeconds { get; set; } = 5d;
    public int FramesPerSecond { get; set; } = 30;
    public bool Loop { get; set; }
    public List<CameraKeyframe> Keyframes { get; set; } = [];

    public void Validate()
    {
        if (Version == 1) UpgradeVersionOne();
        if (Version is 2 or 3) Version = CurrentVersion;
        if (Version != CurrentVersion)
            throw new InvalidDataException($"不支援 Camera 動畫格式版本 {Version}。");
        if (!double.IsFinite(DurationSeconds))
            throw new InvalidDataException("動畫長度必須是有限數值。");
        Name = string.IsNullOrWhiteSpace(Name) ? "Camera Animation" : Name.Trim();
        DurationSeconds = Math.Clamp(DurationSeconds, 0.1d, 3600d);
        FramesPerSecond = Math.Clamp(FramesPerSecond, 1, 240);
        Keyframes ??= [];
        Keyframes = Keyframes.OrderBy(frame => frame.TimeSeconds).ThenBy(frame => frame.Id).ToList();
        for (var index = 0; index < Keyframes.Count; index++)
        {
            var keyframe = Keyframes[index];
            if (index > 0) keyframe.UpgradeBezierControls(Keyframes[index - 1]);
            keyframe.Validate(DurationSeconds);
        }
        RepairLightTrackIds();
    }

    private void RepairLightTrackIds()
    {
        var canonical = Keyframes.FirstOrDefault(frame => frame.Lights is { Count: > 0 })?.Lights;
        if (canonical is null) return;
        for (var frameIndex = 0; frameIndex < Keyframes.Count; frameIndex++)
        {
            var lights = Keyframes[frameIndex].Lights;
            if (lights is null) continue;
            for (var lightIndex = 0; lightIndex < lights.Count; lightIndex++)
            {
                var matching = canonical.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, lights[lightIndex].Name, StringComparison.Ordinal));
                if (matching is not null) lights[lightIndex].Id = matching.Id;
                else if (lightIndex < canonical.Count) lights[lightIndex].Id = canonical[lightIndex].Id;
            }
        }
    }

    private void UpgradeVersionOne()
    {
        Keyframes ??= [];
        var ordered = Keyframes.OrderBy(frame => frame.TimeSeconds).ThenBy(frame => frame.Id).ToArray();
        for (var index = ordered.Length - 1; index >= 1; index--)
            ordered[index].Easing = ordered[index - 1].Easing;
        if (ordered.Length > 0) ordered[0].Easing = CameraEasing.EaseInOut;
        Version = CurrentVersion;
    }
}

public sealed class CameraKeyframe
{
    [Browsable(false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Category("關鍵影格")]
    [DisplayName("名稱")]
    public string Name { get; set; } = "關鍵影格";

    [Category("關鍵影格")]
    [DisplayName("時間（秒）")]
    public double TimeSeconds { get; set; }

    [Browsable(false)]
    public Vector3 From { get; set; }

    [Browsable(false)]
    public Vector3 To { get; set; }

    [Browsable(false)]
    public Vector3? PositionControl { get; set; }

    [Browsable(false)]
    public Vector3? TargetControl { get; set; }

    [Browsable(false), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Obsolete("Legacy two-control-point animation data.")]
    public Vector3? PositionControl1 { get; set; }

    [Browsable(false), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Obsolete("Legacy two-control-point animation data.")]
    public Vector3? PositionControl2 { get; set; }

    [Browsable(false), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Obsolete("Legacy two-control-point animation data.")]
    public Vector3? TargetControl1 { get; set; }

    [Browsable(false), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Obsolete("Legacy two-control-point animation data.")]
    public Vector3? TargetControl2 { get; set; }

    [Browsable(false)]
    public Vector3 Up { get; set; } = Vector3.UnitY;

    [Category("相機")]
    [DisplayName("鏡頭翻滾（度）")]
    public float RollDegrees { get; set; }

    [Category("相機")]
    [DisplayName("視野角度")]
    public float FieldOfViewDegrees { get; set; } = 60f;

    [Browsable(false)]
    public float NearPlane { get; set; } = 0.01f;

    [Browsable(false)]
    public float FarPlane { get; set; } = 10000f;

    [Browsable(false), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AnimationLightState>? Lights { get; set; }

    [Browsable(false), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AnimationModelState>? Models { get; set; }

    [Category("關鍵影格")]
    [DisplayName("與前一影格的轉場")]
    public CameraEasing Easing { get; set; } = CameraEasing.EaseInOut;

    public static CameraKeyframe Capture(CameraState camera, double timeSeconds, string name)
    {
        ArgumentNullException.ThrowIfNull(camera);
        return new CameraKeyframe
        {
            Name = name,
            TimeSeconds = timeSeconds,
            From = camera.From,
            To = camera.To,
            Up = camera.Up,
            RollDegrees = camera.RollDegrees,
            FieldOfViewDegrees = camera.FieldOfViewDegrees,
            NearPlane = camera.NearPlane,
            FarPlane = camera.FarPlane
        };
    }

    public void Validate(double durationSeconds)
    {
        if (Id == Guid.Empty) Id = Guid.NewGuid();
        Name = string.IsNullOrWhiteSpace(Name) ? "關鍵影格" : Name.Trim();
        if (!double.IsFinite(TimeSeconds) || !IsFinite(From) || !IsFinite(To) || !IsFinite(Up) ||
            PositionControl is Vector3 positionControl && !IsFinite(positionControl) ||
            TargetControl is Vector3 targetControl && !IsFinite(targetControl) ||
            !float.IsFinite(RollDegrees) || !float.IsFinite(FieldOfViewDegrees) ||
            !float.IsFinite(NearPlane) || !float.IsFinite(FarPlane))
            throw new InvalidDataException($"關鍵影格「{Name}」含有無效數值。");
        TimeSeconds = Math.Clamp(TimeSeconds, 0d, durationSeconds);
        var camera = ToCameraState();
        camera.Validate();
        From = camera.From;
        To = camera.To;
        Up = camera.Up;
        RollDegrees = camera.RollDegrees;
        FieldOfViewDegrees = camera.FieldOfViewDegrees;
        NearPlane = camera.NearPlane;
        FarPlane = camera.FarPlane;
        Lights?.ForEach(light => light.Validate());
        Models?.ForEach(model => model.Validate());
    }

    public CameraState ToCameraState() => new()
    {
        From = From,
        To = To,
        Up = Up,
        RollDegrees = RollDegrees,
        FieldOfViewDegrees = FieldOfViewDegrees,
        NearPlane = NearPlane,
        FarPlane = FarPlane
    };

    internal void UpgradeBezierControls(CameraKeyframe previous)
    {
#pragma warning disable CS0618
        PositionControl ??= ReduceLegacyControl(previous.From, From, PositionControl1, PositionControl2);
        TargetControl ??= ReduceLegacyControl(previous.To, To, TargetControl1, TargetControl2);
        PositionControl1 = PositionControl2 = TargetControl1 = TargetControl2 = null;
#pragma warning restore CS0618
    }

    private static Vector3? ReduceLegacyControl(Vector3 start, Vector3 end, Vector3? first, Vector3? second)
    {
        if (first is null && second is null) return null;
        if (first is not null && second is not null)
            return ((3f * first.Value - start) * 0.5f + (3f * second.Value - end) * 0.5f) * 0.5f;
        return first ?? second;
    }

    public override string ToString() => $"{TimeSeconds,7:0.000}s  {Name}";

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}

public sealed class AnimationModelState
{
    public Guid ModelId { get; set; }
    public string Name { get; set; } = "Model";
    public bool Visible { get; set; } = true;
    public Vector3 Position { get; set; }
    public Vector3 RotationDegrees { get; set; }
    public Vector3 Scale { get; set; } = Vector3.One;
    public Vector3? PositionControl { get; set; }

    public void Validate()
    {
        Name = string.IsNullOrWhiteSpace(Name) ? "Model" : Name.Trim();
        if (!IsFinite(Position) || !IsFinite(RotationDegrees) || !IsFinite(Scale) ||
            PositionControl is Vector3 control && !IsFinite(control))
            throw new InvalidDataException($"模型動畫「{Name}」含有無效數值。");
        Scale = new Vector3(
            MathF.Abs(Scale.X) < 0.000001f ? 0.000001f : Scale.X,
            MathF.Abs(Scale.Y) < 0.000001f ? 0.000001f : Scale.Y,
            MathF.Abs(Scale.Z) < 0.000001f ? 0.000001f : Scale.Z);
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}

public sealed class AnimationLightState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Light";
    public bool Enabled { get; set; } = true;
    public SceneLightType Type { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; } = -Vector3.UnitY;
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; }
    public float Range { get; set; } = 20f;
    public float FallInDegrees { get; set; } = 20f;
    public float FallOffDegrees { get; set; } = 35f;

    public void Validate()
    {
        if (Id == Guid.Empty) Id = Guid.NewGuid();
        Name = string.IsNullOrWhiteSpace(Name) ? "Light" : Name.Trim();
        var light = ToSceneLight();
        light.Validate();
        Direction = light.Direction;
        Color = light.Color;
        Intensity = light.Intensity;
        Range = light.Range;
        FallInDegrees = light.FallInDegrees;
        FallOffDegrees = light.FallOffDegrees;
    }

    public SceneLight ToSceneLight() => new()
    {
        Name = Name, Enabled = Enabled, Type = Type, Position = Position, Direction = Direction,
        Color = Color, Intensity = Intensity, Range = Range,
        FallInDegrees = FallInDegrees, FallOffDegrees = FallOffDegrees
    };
}

public enum CameraEasing
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    Hold
}
