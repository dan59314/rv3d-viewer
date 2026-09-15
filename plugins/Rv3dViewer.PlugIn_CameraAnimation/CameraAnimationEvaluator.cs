using System.Numerics;
using Rv3dViewer.Core;

namespace Rv3dViewer.CameraAnimationPlugin;

public static class CameraAnimationEvaluator
{
    public static CameraState Evaluate(CameraAnimationDocument document, double timeSeconds)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Keyframes.Count == 0)
            throw new InvalidOperationException("動畫至少需要一個關鍵影格。");

        var frames = document.Keyframes.OrderBy(frame => frame.TimeSeconds).ThenBy(frame => frame.Id).ToArray();
        if (frames.Length == 1 || timeSeconds <= frames[0].TimeSeconds) return frames[0].ToCameraState();
        if (timeSeconds >= frames[^1].TimeSeconds) return frames[^1].ToCameraState();

        var right = Array.FindIndex(frames, frame => frame.TimeSeconds > timeSeconds);
        var first = frames[right - 1];
        var second = frames[right];
        var span = second.TimeSeconds - first.TimeSeconds;
        if (span <= double.Epsilon) return second.ToCameraState();
        var amount = ApplyEasing((float)((timeSeconds - first.TimeSeconds) / span), second.Easing);
        if (second.Easing == CameraEasing.Hold) return first.ToCameraState();

        var firstPose = CameraPose.From(first);
        var secondPose = CameraPose.From(second);
        var orientation = Quaternion.Normalize(Quaternion.Slerp(firstPose.Orientation, secondPose.Orientation, amount));
        var position = Bezier(first.From,
            second.PositionControl ?? Vector3.Lerp(first.From, second.From, 0.5f), second.From, amount);
        var upCandidate = Vector3.Transform(Vector3.UnitY, orientation);
        var target = Bezier(first.To,
            second.TargetControl ?? Vector3.Lerp(first.To, second.To, 0.5f), second.To, amount);
        var direction = target - position;
        var forward = direction.LengthSquared() < 0.000001f
            ? Vector3.Transform(-Vector3.UnitZ, orientation)
            : Vector3.Normalize(direction);
        if (direction.LengthSquared() < 0.000001f) target = position + forward * 0.001f;
        var up = upCandidate - forward * Vector3.Dot(upCandidate, forward);
        if (up.LengthSquared() < 0.000001f)
            up = MathF.Abs(Vector3.Dot(forward, Vector3.UnitY)) < 0.999f ? Vector3.UnitY : Vector3.UnitZ;
        up = Vector3.Normalize(up);
        var result = new CameraState
        {
            From = position,
            To = target,
            Up = up,
            RollDegrees = LerpAngle(first.RollDegrees, second.RollDegrees, amount),
            FieldOfViewDegrees = Lerp(first.FieldOfViewDegrees, second.FieldOfViewDegrees, amount),
            NearPlane = Lerp(first.NearPlane, second.NearPlane, amount),
            FarPlane = Lerp(first.FarPlane, second.FarPlane, amount)
        };
        result.Validate();
        return result;
    }

    public static IReadOnlyList<AnimationLightState>? EvaluateLights(
        CameraAnimationDocument document, double timeSeconds)
    {
        ArgumentNullException.ThrowIfNull(document);
        var frames = document.Keyframes.OrderBy(frame => frame.TimeSeconds).ThenBy(frame => frame.Id).ToArray();
        if (frames.Length == 0 || frames.All(frame => frame.Lights is null)) return null;
        if (frames.Length == 1 || timeSeconds <= frames[0].TimeSeconds)
            return CloneLights(NearestLights(frames, 0));
        if (timeSeconds >= frames[^1].TimeSeconds)
            return CloneLights(NearestLights(frames, frames.Length - 1));

        var rightIndex = Array.FindIndex(frames, frame => frame.TimeSeconds > timeSeconds);
        var first = frames[rightIndex - 1];
        var second = frames[rightIndex];
        var firstLights = first.Lights ?? NearestLights(frames, rightIndex - 1);
        var secondLights = second.Lights ?? firstLights;
        if (firstLights is null || secondLights is null) return null;
        var span = second.TimeSeconds - first.TimeSeconds;
        var amount = span <= double.Epsilon ? 1f :
            ApplyEasing((float)((timeSeconds - first.TimeSeconds) / span), second.Easing);
        if (second.Easing == CameraEasing.Hold) amount = 0f;

        return firstLights.Select((firstLight, index) =>
        {
            var secondLight = secondLights.FirstOrDefault(light => light.Id == firstLight.Id)
                              ?? secondLights.ElementAtOrDefault(index)
                              ?? firstLight;
            return InterpolateLight(firstLight, secondLight, amount);
        }).ToList();
    }

    public static IReadOnlyList<AnimationModelState>? EvaluateModels(
        CameraAnimationDocument document, double timeSeconds)
    {
        ArgumentNullException.ThrowIfNull(document);
        var frames = document.Keyframes.OrderBy(frame => frame.TimeSeconds).ThenBy(frame => frame.Id).ToArray();
        if (frames.Length == 0 || frames.All(frame => frame.Models is null)) return null;
        var modelIds = frames.SelectMany(frame => frame.Models ?? []).Select(model => model.ModelId).Distinct();
        var result = new List<AnimationModelState>();
        foreach (var modelId in modelIds)
        {
            var track = frames.Select(frame =>
                (Frame: frame, State: frame.Models?.FirstOrDefault(model => model.ModelId == modelId)))
                .Where(item => item.State is not null).ToArray();
            var previousIndex = Array.FindLastIndex(track, item => item.Frame.TimeSeconds <= timeSeconds);
            if (previousIndex < 0) continue;
            var previous = track[previousIndex];
            if (previousIndex == track.Length - 1)
            {
                result.Add(CloneModel(previous.State!));
                continue;
            }

            var next = track[previousIndex + 1];
            var span = next.Frame.TimeSeconds - previous.Frame.TimeSeconds;
            var amount = span <= double.Epsilon ? 1f :
                ApplyEasing((float)((timeSeconds - previous.Frame.TimeSeconds) / span), next.Frame.Easing);
            if (next.Frame.Easing == CameraEasing.Hold) amount = 0f;
            result.Add(InterpolateModel(previous.State!, next.State!, amount));
        }
        return result.Count == 0 ? null : result;
    }

    private static AnimationModelState CloneModel(AnimationModelState model) => new()
    {
        ModelId = model.ModelId,
        Name = model.Name,
        Visible = model.Visible,
        Position = model.Position,
        RotationDegrees = model.RotationDegrees,
        Scale = model.Scale,
        PositionControl = model.PositionControl
    };

    private static AnimationModelState InterpolateModel(
        AnimationModelState first, AnimationModelState second, float amount)
    {
        return new AnimationModelState
        {
            ModelId = first.ModelId,
            Name = amount < 0.5f ? first.Name : second.Name,
            Visible = amount < 0.5f ? first.Visible : second.Visible,
            Position = Bezier(first.Position,
                second.PositionControl ?? Vector3.Lerp(first.Position, second.Position, 0.5f),
                second.Position, amount),
            RotationDegrees = new Vector3(
                LerpAngle(first.RotationDegrees.X, second.RotationDegrees.X, amount),
                LerpAngle(first.RotationDegrees.Y, second.RotationDegrees.Y, amount),
                LerpAngle(first.RotationDegrees.Z, second.RotationDegrees.Z, amount)),
            Scale = Vector3.Lerp(first.Scale, second.Scale, amount),
            PositionControl = second.PositionControl
        };
    }

    private static List<AnimationLightState>? NearestLights(CameraKeyframe[] frames, int startIndex)
    {
        for (var distance = 0; distance < frames.Length; distance++)
        {
            var before = startIndex - distance;
            if (before >= 0 && frames[before].Lights is not null) return frames[before].Lights;
            var after = startIndex + distance;
            if (after < frames.Length && frames[after].Lights is not null) return frames[after].Lights;
        }
        return null;
    }

    private static IReadOnlyList<AnimationLightState>? CloneLights(IEnumerable<AnimationLightState>? lights) =>
        lights?.Select(light => InterpolateLight(light, light, 0f)).ToList();

    private static AnimationLightState InterpolateLight(AnimationLightState first, AnimationLightState second, float amount)
    {
        var direction = Vector3.Lerp(first.Direction, second.Direction, amount);
        if (direction.LengthSquared() < 0.000001f) direction = first.Direction;
        return new AnimationLightState
        {
            Id = first.Id,
            Name = amount < 0.5f ? first.Name : second.Name,
            Enabled = amount < 0.5f ? first.Enabled : second.Enabled,
            Type = amount < 0.5f ? first.Type : second.Type,
            Position = Vector3.Lerp(first.Position, second.Position, amount),
            Direction = Vector3.Normalize(direction),
            Color = Vector3.Lerp(first.Color, second.Color, amount),
            Intensity = Lerp(first.Intensity, second.Intensity, amount),
            Range = Lerp(first.Range, second.Range, amount),
            FallInDegrees = Lerp(first.FallInDegrees, second.FallInDegrees, amount),
            FallOffDegrees = Lerp(first.FallOffDegrees, second.FallOffDegrees, amount)
        };
    }

    internal static float ApplyEasing(float amount, CameraEasing easing)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return easing switch
        {
            CameraEasing.EaseIn => amount * amount,
            CameraEasing.EaseOut => 1f - (1f - amount) * (1f - amount),
            CameraEasing.EaseInOut => amount * amount * (3f - 2f * amount),
            CameraEasing.Hold => 0f,
            _ => amount
        };
    }

    private static float Lerp(float first, float second, float amount) => first + (second - first) * amount;

    private static float LerpAngle(float first, float second, float amount)
    {
        var delta = (second - first) % 360f;
        if (delta >= 180f) delta -= 360f;
        if (delta < -180f) delta += 360f;
        return first + delta * amount;
    }

    private static Vector3 Bezier(Vector3 p0, Vector3 control, Vector3 p2, float amount)
    {
        var inverse = 1f - amount;
        return inverse * inverse * p0 + 2f * inverse * amount * control + amount * amount * p2;
    }

    private readonly record struct CameraPose(Vector3 Position, Quaternion Orientation, float FocusDistance)
    {
        public static CameraPose From(CameraKeyframe frame)
        {
            var forward = frame.To - frame.From;
            var distance = Math.Max(forward.Length(), 0.001f);
            forward /= distance;
            var up = frame.Up.LengthSquared() > 0.000001f ? Vector3.Normalize(frame.Up) : Vector3.UnitY;
            if (MathF.Abs(Vector3.Dot(forward, up)) > 0.999f)
                up = MathF.Abs(Vector3.Dot(forward, Vector3.UnitY)) < 0.999f ? Vector3.UnitY : Vector3.UnitZ;
            var world = Matrix4x4.CreateWorld(Vector3.Zero, forward, up);
            return new CameraPose(frame.From, Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(world)), distance);
        }
    }
}
