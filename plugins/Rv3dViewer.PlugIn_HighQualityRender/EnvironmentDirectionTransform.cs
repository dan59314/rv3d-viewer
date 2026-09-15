using System.Numerics;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal static class EnvironmentDirectionTransform
{
    public static Vector3 WorldToMap(Vector3 direction, float rotation)
    {
        var c = MathF.Cos(rotation);
        var s = MathF.Sin(rotation);
        return new Vector3(direction.X * c + direction.Z * s, direction.Y,
            -direction.X * s + direction.Z * c);
    }

    public static Vector3 MapToWorld(Vector3 direction, float rotation)
    {
        var c = MathF.Cos(rotation);
        var s = MathF.Sin(rotation);
        return new Vector3(direction.X * c - direction.Z * s, direction.Y,
            direction.X * s + direction.Z * c);
    }
}
