using System.Numerics;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal static class RenderGeometryPrecision
{
    // Relative to a triangle, not the full scene (a ring may share a room scene).
    internal static float RayOffset(Vector3 position, RenderTriangle triangle)
    {
        var edge = MathF.Max((triangle.P1 - triangle.P0).Length(),
            MathF.Max((triangle.P2 - triangle.P0).Length(), (triangle.P2 - triangle.P1).Length()));
        var coordinate = MathF.Max(MathF.Abs(position.X), MathF.Max(MathF.Abs(position.Y), MathF.Abs(position.Z)));
        return MathF.Max(edge * 1e-5F, MathF.Max(coordinate * 4.7683716e-7F, 1e-12F));
    }
}
