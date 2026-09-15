using System.Numerics;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal static class RenderOptics
{
    internal const float ClearGlassRoughnessThreshold = 0.01F;

    internal static bool IsDeltaGlass(float roughness) => roughness <= ClearGlassRoughnessThreshold;

    public static float NormalIncidenceReflectance(float indexOfRefraction)
    {
        var ior = Math.Max(indexOfRefraction, 1.0001F);
        return MathF.Pow((ior - 1F) / (ior + 1F), 2F);
    }

    internal static float RelativeIndexOfRefraction(bool frontFace, float indexOfRefraction)
    {
        var ior = Math.Max(indexOfRefraction, 1.0001F);
        return frontFace ? 1F / ior : ior;
    }

    internal static float RelativeIndexOfRefraction(float incidentIor, float transmittedIor) =>
        Math.Max(incidentIor, 1F) / Math.Max(transmittedIor, 1F);

    internal static float DielectricFresnel(float cosine, float indexOfRefraction)
    {
        var f0 = NormalIncidenceReflectance(indexOfRefraction);
        return f0 + (1F - f0) * MathF.Pow(1F - Math.Clamp(cosine, 0F, 1F), 5F);
    }

    internal static float DielectricFresnel(float cosine, float incidentIor, float transmittedIor)
    {
        var etaI = Math.Max(incidentIor, 1F);
        var etaT = Math.Max(transmittedIor, 1F);
        var f0 = MathF.Pow((etaT - etaI) / (etaT + etaI), 2F);
        return f0 + (1F - f0) * MathF.Pow(1F - Math.Clamp(cosine, 0F, 1F), 5F);
    }

    internal static Vector3 BeerLambert(Vector3 absorptionColor, float thickness, float distance)
    {
        // In the offline renderer, AbsorptionColor is the remaining light after
        // one material Thickness of travel. Both distances use scene units.
        // A zero thickness denotes a thin surface with no volume absorption.
        if (thickness <= 0F || distance <= 0F) return Vector3.One;
        var opticalDistance = distance / thickness;
        var color = Vector3.Clamp(absorptionColor, new Vector3(0.000001F), Vector3.One);
        return new Vector3(
            MathF.Exp(MathF.Log(color.X) * opticalDistance),
            MathF.Exp(MathF.Log(color.Y) * opticalDistance),
            MathF.Exp(MathF.Log(color.Z) * opticalDistance));
    }

    internal static Vector3 SurfaceTransmission(Vector3 baseColor, float transmission)
    {
        // Split the authored surface tint across entry and exit. A closed clear
        // shell produces BaseColor once; reflected light stays uncoloured.
        var tint = Vector3.Clamp(baseColor, Vector3.Zero, Vector3.One);
        return new Vector3(MathF.Sqrt(tint.X), MathF.Sqrt(tint.Y), MathF.Sqrt(tint.Z)) *
            Math.Clamp(transmission, 0F, 1F);
    }
}

internal readonly record struct RenderMedium(int MaterialIndex, float IndexOfRefraction, float Thickness, Vector3 AbsorptionColor);

internal ref struct RenderMediumStack(Span<RenderMedium> storage)
{
    private readonly Span<RenderMedium> _storage = storage;
    private int _count;

    internal int Count => _count;
    internal float CurrentIndexOfRefraction => _count == 0 ? 1F : _storage[_count - 1].IndexOfRefraction;

    internal float IncidentIndexOfRefraction(int materialIndex, bool entering, float materialIor) =>
        !entering && FindFromTop(materialIndex) < 0
            ? Math.Max(materialIor, 1.0001F)
            : CurrentIndexOfRefraction;

    internal float TransmittedIndexOfRefraction(int materialIndex, bool entering, float materialIor)
    {
        if (entering) return Math.Max(materialIor, 1.0001F);
        var index = FindFromTop(materialIndex);
        return index > 0 ? _storage[index - 1].IndexOfRefraction : 1F;
    }

    internal Vector3 SegmentTransmittance(float distance) => _count == 0
        ? Vector3.One
        : RenderOptics.BeerLambert(_storage[_count - 1].AbsorptionColor,
            _storage[_count - 1].Thickness, distance);

    internal void CrossBoundary(int materialIndex, bool entering, RenderMaterial material)
    {
        if (entering)
        {
            // Imported jewelry frequently contains coincident shells or inconsistent
            // winding. Do not let a duplicate entry leave the ray permanently trapped
            // in the same absorbing medium.
            var duplicateIndex = FindFromTop(materialIndex);
            if (duplicateIndex >= 0)
            {
                RemoveAt(duplicateIndex);
                return;
            }

            if (_count < _storage.Length)
                _storage[_count++] = new RenderMedium(materialIndex, Math.Max(material.IndexOfRefraction, 1.0001F),
                    Math.Max(material.Thickness, 0F), Vector3.Clamp(material.AbsorptionColor, Vector3.Zero, Vector3.One));
            return;
        }

        var index = FindFromTop(materialIndex);
        if (index < 0) return;
        RemoveAt(index);
    }

    private void RemoveAt(int index)
    {
        for (var i = index; i < _count - 1; i++) _storage[i] = _storage[i + 1];
        _count--;
    }

    private int FindFromTop(int materialIndex)
    {
        for (var i = _count - 1; i >= 0; i--)
            if (_storage[i].MaterialIndex == materialIndex) return i;
        return -1;
    }
}
