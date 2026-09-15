using System.Numerics;

namespace Rv3dViewer.Core;

public static class RenderColorPipeline
{
    public static Vector3 SrgbToLinear(Vector3 value) => new(
        SrgbChannelToLinear(value.X),
        SrgbChannelToLinear(value.Y),
        SrgbChannelToLinear(value.Z));

    public static Vector3 LinearToSrgb(Vector3 value) => new(
        LinearChannelToSrgb(value.X),
        LinearChannelToSrgb(value.Y),
        LinearChannelToSrgb(value.Z));

    public static Vector3 AcesToneMap(Vector3 color)
    {
        const float a = 2.51F;
        const float b = 0.03F;
        const float c = 2.43F;
        const float d = 0.59F;
        const float e = 0.14F;
        color = Vector3.Max(color, Vector3.Zero);
        return Vector3.Clamp(
            color * (a * color + new Vector3(b)) /
            (color * (c * color + new Vector3(d)) + new Vector3(e)),
            Vector3.Zero,
            Vector3.One);
    }

    public static Vector3 EncodeDisplay(Vector3 linearColor) =>
        LinearToSrgb(AcesToneMap(linearColor));

    public static Vector3 DecodeDisplay(Vector3 displayColor) =>
        InverseAcesToneMap(SrgbToLinear(displayColor));

    public static Vector3 InverseAcesToneMap(Vector3 color) => new(
        InverseAcesChannel(color.X),
        InverseAcesChannel(color.Y),
        InverseAcesChannel(color.Z));

    private static float InverseAcesChannel(float value)
    {
        const float a = 2.51F;
        const float b = 0.03F;
        const float c = 2.43F;
        const float d = 0.59F;
        const float e = 0.14F;
        value = Math.Clamp(value, 0F, 0.9999F);
        var quadratic = value * c - a;
        var linear = value * d - b;
        var constant = value * e;
        if (MathF.Abs(quadratic) < 0.000001F)
            return MathF.Max(0F, -constant / MathF.CopySign(MathF.Max(MathF.Abs(linear), 0.000001F), linear));
        var discriminant = MathF.Max(0F, linear * linear - 4F * quadratic * constant);
        var root = MathF.Sqrt(discriminant);
        var first = (-linear + root) / (2F * quadratic);
        var second = (-linear - root) / (2F * quadratic);
        return MathF.Max(0F, MathF.Max(first, second));
    }

    private static float SrgbChannelToLinear(float value) =>
        value <= 0.04045F ? value / 12.92F : MathF.Pow((value + 0.055F) / 1.055F, 2.4F);

    private static float LinearChannelToSrgb(float value)
    {
        value = Math.Max(value, 0F);
        return value <= 0.0031308F
            ? value * 12.92F
            : 1.055F * MathF.Pow(value, 1F / 2.4F) - 0.055F;
    }
}
