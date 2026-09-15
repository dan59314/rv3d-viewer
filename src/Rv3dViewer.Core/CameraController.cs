using System.Numerics;

namespace Rv3dViewer.Core;

public static class CameraController
{
    public static void SetView(CameraState camera, CameraView view)
    {
        camera.Validate();
        var distance = Math.Max(Vector3.Distance(camera.From, camera.To), 0.001f);
        var direction = view switch
        {
            CameraView.Front => Vector3.UnitZ,
            CameraView.Back => -Vector3.UnitZ,
            CameraView.Left => -Vector3.UnitX,
            CameraView.Right => Vector3.UnitX,
            CameraView.Top => Vector3.UnitY,
            CameraView.Bottom => -Vector3.UnitY,
            _ => Vector3.Normalize(new Vector3(1f, 0.75f, 1.5f))
        };

        camera.From = camera.To + direction * distance;
        UpdateUpFromRoll(camera);
    }

    public static void Orbit(CameraState camera, float yawRadians, float pitchRadians)
    {
        camera.Validate();
        var offset = camera.From - camera.To;
        var radius = Math.Max(offset.Length(), 0.001f);
        var yaw = MathF.Atan2(offset.X, offset.Z) + yawRadians;
        var pitch = MathF.Asin(Math.Clamp(offset.Y / radius, -1f, 1f)) + pitchRadians;
        pitch = Math.Clamp(pitch, -MathF.PI * 0.495f, MathF.PI * 0.495f);
        camera.From = camera.To + new Vector3(
            radius * MathF.Sin(yaw) * MathF.Cos(pitch),
            radius * MathF.Sin(pitch),
            radius * MathF.Cos(yaw) * MathF.Cos(pitch));
        UpdateUpFromRoll(camera);
    }

    public static void UpdateUpFromRoll(CameraState camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        camera.Validate();
        var forward = Vector3.Normalize(camera.To - camera.From);
        var referenceUp = MathF.Abs(Vector3.Dot(forward, Vector3.UnitY)) > 0.999f
            ? forward.Y < 0f ? -Vector3.UnitZ : Vector3.UnitZ
            : Vector3.UnitY;
        var right = Vector3.Normalize(Vector3.Cross(forward, referenceUp));
        var baseUp = Vector3.Normalize(Vector3.Cross(right, forward));
        var roll = Quaternion.CreateFromAxisAngle(forward, DegreesToRadians(camera.RollDegrees));
        camera.Up = Vector3.Normalize(Vector3.Transform(baseUp, roll));
    }

    public static void AdjustRoll(CameraState camera, float deltaDegrees)
    {
        ArgumentNullException.ThrowIfNull(camera);
        camera.RollDegrees += deltaDegrees;
        UpdateUpFromRoll(camera);
    }

    public static void Pan(CameraState camera, float horizontal, float vertical)
    {
        camera.Validate();
        var forward = Vector3.Normalize(camera.To - camera.From);
        var right = Vector3.Normalize(Vector3.Cross(forward, camera.Up));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var delta = right * horizontal + up * vertical;
        camera.From += delta;
        camera.To += delta;
    }

    public static void PanTarget(CameraState camera, float horizontal, float vertical)
    {
        camera.Validate();
        var forward = Vector3.Normalize(camera.To - camera.From);
        var right = Vector3.Normalize(Vector3.Cross(forward, camera.Up));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        camera.To += right * horizontal + up * vertical;
        camera.Validate();
        UpdateUpFromRoll(camera);
    }

    public static void AdjustFieldOfView(CameraState camera, float deltaDegrees)
    {
        ArgumentNullException.ThrowIfNull(camera);
        camera.FieldOfViewDegrees = Math.Clamp(camera.FieldOfViewDegrees + deltaDegrees, 5f, 120f);
    }

    public static void Dolly(CameraState camera, float amount)
    {
        camera.Validate();
        var offset = camera.From - camera.To;
        var distance = offset.Length();
        var newDistance = Math.Clamp(distance * MathF.Exp(amount), 0.001f, camera.FarPlane * 0.9f);
        camera.From = camera.To + Vector3.Normalize(offset) * newDistance;
    }

    private static float DegreesToRadians(float value) => value * MathF.PI / 180f;
}

public enum CameraView
{
    Perspective,
    Front,
    Back,
    Left,
    Right,
    Top,
    Bottom
}
