using System.Numerics;
using System.Text;
using System.Text.Json;
using Rv3dViewer.Core;

namespace Rv3dViewer.App;

internal static class GltfMaterialMetadataReader
{
    private const uint JsonChunkType = 0x4E4F534A;

    public static void Apply(string glbPath, IList<PbrMaterial> materials)
    {
        using var stream = File.OpenRead(glbPath);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        if (reader.ReadUInt32() != 0x46546C67 || reader.ReadUInt32() != 2) return;
        _ = reader.ReadUInt32();
        var chunkLength = reader.ReadUInt32();
        if (reader.ReadUInt32() != JsonChunkType || chunkLength > int.MaxValue) return;
        using var document = JsonDocument.Parse(reader.ReadBytes((int)chunkLength));
        if (!document.RootElement.TryGetProperty("materials", out var sourceMaterials)) return;

        var count = Math.Min(materials.Count, sourceMaterials.GetArrayLength());
        for (var index = 0; index < count; index++) Apply(sourceMaterials[index], materials[index]);
    }

    private static void Apply(JsonElement source, PbrMaterial target)
    {
        if (source.TryGetProperty("pbrMetallicRoughness", out var pbr))
        {
            target.BaseColor = TryVector4(pbr, "baseColorFactor", out var baseColor) ? baseColor : Vector4.One;
            target.Metallic = TryFloat(pbr, "metallicFactor", out var metallic) ? metallic : 1f;
            target.Roughness = TryFloat(pbr, "roughnessFactor", out var roughness) ? roughness : 1f;
        }
        if (TryVector3(source, "emissiveFactor", out var emissive)) target.Emissive = emissive;
        if (source.TryGetProperty("doubleSided", out var doubleSided)) target.DoubleSided = doubleSided.GetBoolean();
        if (TryFloat(source, "alphaCutoff", out var alphaCutoff)) target.AlphaCutoff = alphaCutoff;
        if (source.TryGetProperty("alphaMode", out var alphaMode))
            target.RenderMode = alphaMode.GetString() switch
            {
                "MASK" => MaterialRenderMode.Cutout,
                "BLEND" => MaterialRenderMode.Transparent,
                _ => MaterialRenderMode.Opaque
            };

        if (!source.TryGetProperty("extensions", out var extensions))
        {
            target.Validate();
            return;
        }
        if (extensions.TryGetProperty("KHR_materials_transmission", out var transmission) &&
            TryFloat(transmission, "transmissionFactor", out var transmissionFactor))
        {
            target.Transmission = transmissionFactor;
            if (transmissionFactor > 0f) target.RenderMode = MaterialRenderMode.Glass;
        }
        if (extensions.TryGetProperty("KHR_materials_ior", out var ior) && TryFloat(ior, "ior", out var iorValue))
            target.IndexOfRefraction = iorValue;
        if (extensions.TryGetProperty("KHR_materials_volume", out var volume))
        {
            if (TryFloat(volume, "thicknessFactor", out var thickness)) target.Thickness = thickness;
            if (TryVector3(volume, "attenuationColor", out var absorption)) target.AbsorptionColor = absorption;
        }
        if (extensions.TryGetProperty("KHR_materials_dispersion", out var dispersion) &&
            TryFloat(dispersion, "dispersion", out var dispersionValue))
            target.Dispersion = dispersionValue;
        if (extensions.TryGetProperty("KHR_materials_emissive_strength", out var emissiveStrength) &&
            TryFloat(emissiveStrength, "emissiveStrength", out var strength))
            target.EmissiveStrength = strength;
        target.Validate();
    }

    private static bool TryFloat(JsonElement element, string name, out float value)
    {
        value = 0f;
        return element.TryGetProperty(name, out var property) && property.TryGetSingle(out value);
    }

    private static bool TryVector3(JsonElement element, string name, out Vector3 value)
    {
        value = default;
        if (!element.TryGetProperty(name, out var array) || array.GetArrayLength() < 3) return false;
        value = new Vector3(array[0].GetSingle(), array[1].GetSingle(), array[2].GetSingle());
        return true;
    }

    private static bool TryVector4(JsonElement element, string name, out Vector4 value)
    {
        value = default;
        if (!element.TryGetProperty(name, out var array) || array.GetArrayLength() < 4) return false;
        value = new Vector4(array[0].GetSingle(), array[1].GetSingle(), array[2].GetSingle(), array[3].GetSingle());
        return true;
    }
}
