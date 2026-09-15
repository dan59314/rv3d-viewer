namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.ComponentModel;
using System.Numerics;
using Rv3dViewer.Core;

internal enum InteriorTextureMappingMode
{
    現有UV,
    平面,
    箱型,
    三平面,
    圓柱,
    球狀
}

internal sealed class InteriorTexturePreset
{
    internal InteriorTexturePreset(string name, string category, string description,
        Vector4 baseColor, TextureLayerKind pattern, Vector2 patternScale, float patternOpacity,
        TextureBlendMode blendMode = TextureBlendMode.Overlay)
    {
        Name = name;
        Category = category;
        Description = description;
        BaseColor = baseColor;
        Pattern = pattern;
        PatternScale = patternScale;
        PatternOpacity = patternOpacity;
        BlendMode = blendMode;
    }

    public string Name { get; }
    public string Category { get; }
    public string Description { get; }
    internal Vector4 BaseColor { get; }
    internal TextureLayerKind Pattern { get; }
    internal Vector2 PatternScale { get; }
    internal float PatternOpacity { get; }
    internal TextureBlendMode BlendMode { get; }
    public override string ToString() => $"{Category}｜{Name}";
}

internal sealed class InteriorTextureApplicationSettings
{
    internal InteriorTextureApplicationSettings(InteriorTexturePreset preset)
    {
        Preset = preset;
    }

    [Browsable(false)]
    internal InteriorTexturePreset Preset { get; }

    [Category("貼圖"), DisplayName("名稱"), ReadOnly(true)]
    public string Name => Preset.Name;

    [Category("貼圖"), DisplayName("分類"), ReadOnly(true)]
    public string Category => Preset.Category;

    [Category("貼圖"), DisplayName("說明"), ReadOnly(true)]
    public string Description => Preset.Description;

    [Category("映射"), DisplayName("映射模式")]
    public InteriorTextureMappingMode MappingMode { get; set; } = InteriorTextureMappingMode.箱型;

    [Category("映射"), DisplayName("水平重複")]
    public float RepeatU { get; set; } = 1f;

    [Category("映射"), DisplayName("垂直重複")]
    public float RepeatV { get; set; } = 1f;

    [Category("映射"), DisplayName("旋轉（度）")]
    public float RotationDegrees { get; set; }

    [Category("映射"), DisplayName("水平位移")]
    public float OffsetU { get; set; }

    [Category("映射"), DisplayName("垂直位移")]
    public float OffsetV { get; set; }

    internal TextureStack CreateTextureStack()
    {
        var mapping = new TextureMappingSettings
        {
            Mode = MappingMode switch
            {
                InteriorTextureMappingMode.平面 => TextureMappingMode.Planar,
                InteriorTextureMappingMode.箱型 => TextureMappingMode.Box,
                InteriorTextureMappingMode.三平面 => TextureMappingMode.Triplanar,
                _ => TextureMappingMode.Uv
            },
            Space = TextureProjectionSpace.Object,
            Axis = TextureProjectionAxis.Auto
        };
        var transform = new TextureTransformSettings
        {
            ScaleX = Math.Clamp(RepeatU, .05f, 100f),
            ScaleY = Math.Clamp(RepeatV, .05f, 100f),
            RotationDegrees = RotationDegrees,
            OffsetX = Math.Clamp(OffsetU, -100f, 100f),
            OffsetY = Math.Clamp(OffsetV, -100f, 100f),
            LockAspectRatio = false
        };
        var baseLayer = new TextureLayer
        {
            Name = $"{Preset.Name}底色",
            Kind = TextureLayerKind.SolidColor,
            Color = Preset.BaseColor,
            Mapping = mapping.Clone(),
            Transform = transform.Clone(),
            Channels = new TextureChannelSettings { ColorSpace = TextureColorSpace.Srgb }
        };
        var patternTransform = transform.Clone();
        patternTransform.ScaleX *= Preset.PatternScale.X;
        patternTransform.ScaleY *= Preset.PatternScale.Y;
        var patternLayer = new TextureLayer
        {
            Name = $"{Preset.Name}紋理",
            Kind = Preset.Pattern,
            Color = Preset.BaseColor,
            Opacity = Preset.PatternOpacity,
            BlendMode = Preset.BlendMode,
            Mapping = mapping.Clone(),
            Transform = patternTransform,
            Channels = new TextureChannelSettings { ColorSpace = TextureColorSpace.Srgb }
        };
        return new TextureStack
        {
            Semantic = TextureSemantic.BaseColor,
            Layers = [baseLayer, patternLayer]
        };
    }
}

internal static class InteriorTextureCatalog
{
    internal static IReadOnlyList<InteriorTexturePreset> CreateDefault() =>
    [
        Preset("細緻壁紙", "壁紙", "低對比室內壁紙", .82f, .80f, .73f, TextureLayerKind.Gradient, 2f, 6f, .24f),
        Preset("橡木木紋", "木紋", "暖色橡木條紋", .55f, .30f, .12f, TextureLayerKind.Noise, 2f, 14f, .34f, TextureBlendMode.Multiply),
        Preset("拉絲金屬", "金屬", "細密水平拉絲", .66f, .69f, .72f, TextureLayerKind.Gradient, 1f, 18f, .30f),
        Preset("方形磁磚", "磁磚", "規則方形磁磚接縫", .78f, .82f, .84f, TextureLayerKind.Checker, 1.5f, 1.5f, .28f),
        Preset("天然石紋", "石材", "不規則天然石材表面", .48f, .46f, .42f, TextureLayerKind.Noise, 5f, 5f, .40f, TextureBlendMode.Overlay),
        Preset("清水混凝土", "混凝土", "細顆粒混凝土", .52f, .53f, .52f, TextureLayerKind.Noise, 12f, 12f, .22f, TextureBlendMode.Multiply),
        Preset("霧面玻璃", "玻璃", "柔和漸層玻璃表面", .70f, .84f, .88f, TextureLayerKind.Gradient, 2f, 2f, .18f),
        Preset("家具織物", "織物", "細密交織布紋", .30f, .44f, .61f, TextureLayerKind.Checker, 8f, 8f, .20f),
        Preset("皮革紋理", "皮革", "低對比皮革顆粒", .31f, .15f, .08f, TextureLayerKind.Noise, 16f, 16f, .25f, TextureBlendMode.Overlay),
        Preset("霧面塑膠", "塑膠", "均勻霧面塑膠", .24f, .36f, .44f, TextureLayerKind.Gradient, 1f, 2f, .16f),
        Preset("短毛地毯", "地毯", "高密度短毛地毯", .24f, .38f, .52f, TextureLayerKind.Noise, 24f, 24f, .32f, TextureBlendMode.Multiply),
        Preset("自然表面", "自然", "土壤與自然顆粒", .35f, .25f, .12f, TextureLayerKind.Noise, 10f, 10f, .42f, TextureBlendMode.Overlay)
    ];

    private static InteriorTexturePreset Preset(string name, string category, string description,
        float red, float green, float blue, TextureLayerKind pattern, float scaleX, float scaleY,
        float opacity, TextureBlendMode blend = TextureBlendMode.Overlay) =>
        new(name, category, description, new Vector4(red, green, blue, 1f), pattern,
            new Vector2(scaleX, scaleY), opacity, blend);
}
