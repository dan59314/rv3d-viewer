namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.ComponentModel;
using System.Numerics;
using Rv3dViewer.Core;

internal sealed class InteriorMaterialPreset
{
    internal InteriorMaterialPreset(string name, string category, string description, PbrMaterial material)
    {
        Name = name;
        Category = category;
        Description = description;
        Material = material;
    }

    [Category("材質"), DisplayName("名稱"), ReadOnly(true)]
    public string Name { get; }

    [Category("材質"), DisplayName("分類"), ReadOnly(true)]
    public string Category { get; }

    [Category("材質"), DisplayName("說明"), ReadOnly(true)]
    public string Description { get; }

    [Category("外觀"), DisplayName("基礎色"), ReadOnly(true)]
    public string BaseColor => $"R {Material.BaseColor.X:0.00}  G {Material.BaseColor.Y:0.00}  B {Material.BaseColor.Z:0.00}";

    [Category("外觀"), DisplayName("金屬度"), ReadOnly(true)]
    public float Metallic => Material.Metallic;

    [Category("外觀"), DisplayName("粗糙度"), ReadOnly(true)]
    public float Roughness => Material.Roughness;

    [Category("外觀"), DisplayName("不透明度"), ReadOnly(true)]
    public float Opacity => Material.Opacity;

    [Browsable(false)]
    internal PbrMaterial Material { get; }

    internal PbrMaterial CreateMaterial() => Material.Clone(Material.Name);

    public override string ToString() => $"{Category}｜{Name}";
}

internal static class InteriorMaterialCatalog
{
    internal static IReadOnlyList<InteriorMaterialPreset> CreateDefault() =>
    [
        Preset("乳膠漆", "牆面", "低反光室內牆面塗料", new Vector4(.86f, .84f, .78f, 1f), 0f, .9f),
        Preset("實木", "木材", "溫暖深色實木", new Vector4(.42f, .20f, .07f, 1f), 0f, .62f),
        Preset("木皮", "木材", "中性色木皮飾面", new Vector4(.58f, .32f, .13f, 1f), 0f, .55f),
        Preset("不鏽鋼", "金屬", "拋絲不鏽鋼", new Vector4(.68f, .72f, .76f, 1f), .95f, .24f),
        Preset("鋁材", "金屬", "霧面鋁合金", new Vector4(.57f, .61f, .65f, 1f), .85f, .38f),
        Glass(),
        Preset("陶瓷", "陶瓷", "釉面陶瓷", new Vector4(.92f, .90f, .82f, 1f), 0f, .24f),
        Preset("石材", "石材", "霧面天然石材", new Vector4(.47f, .46f, .43f, 1f), 0f, .76f),
        Preset("布料", "織物", "柔軟霧面家具布料", new Vector4(.29f, .43f, .62f, 1f), 0f, .94f, true),
        Preset("皮革", "皮革", "深棕色半霧面皮革", new Vector4(.28f, .12f, .06f, 1f), 0f, .52f),
        Preset("塑膠", "塑膠", "一般室內塑膠", new Vector4(.21f, .34f, .43f, 1f), 0f, .44f),
        Emissive()
    ];

    private static InteriorMaterialPreset Preset(string name, string category, string description,
        Vector4 color, float metallic, float roughness, bool doubleSided = false) =>
        new(name, category, description, new PbrMaterial
        {
            Name = $"RV材質庫｜{name}",
            BaseColor = color,
            Metallic = metallic,
            Roughness = roughness,
            DoubleSided = doubleSided
        });

    private static InteriorMaterialPreset Glass()
    {
        var preset = Preset("玻璃", "玻璃", "透明室內玻璃", new Vector4(.66f, .84f, .92f, .34f), 0f, .08f, true);
        preset.Material.Opacity = .34f;
        preset.Material.RenderMode = MaterialRenderMode.Glass;
        preset.Material.Transmission = .96f;
        preset.Material.IndexOfRefraction = 1.5f;
        return preset;
    }

    private static InteriorMaterialPreset Emissive()
    {
        var preset = Preset("發光材質", "燈具", "暖色自發光燈罩或光源表面",
            new Vector4(1f, .82f, .44f, 1f), 0f, .22f, true);
        preset.Material.Emissive = new Vector3(1f, .62f, .20f);
        preset.Material.EmissiveStrength = 3f;
        return preset;
    }
}
