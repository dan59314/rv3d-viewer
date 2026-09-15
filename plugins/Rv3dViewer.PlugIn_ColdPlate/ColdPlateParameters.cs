using System.ComponentModel;
using System.Numerics;

namespace Rv3dViewer.ColdPlatePlugin;

public enum ColdPlateShape
{
    [Description("方形")] Rectangle,
    [Description("圓形")] Circle,
    [Description("自訂 Polygon（凸多邊形）")] Polygon
}

public enum CavitySizingMode
{
    [Description("由內縮尺寸計算")] Inset,
    [Description("指定內部尺寸")] Explicit
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class PortParameters
{
    [DisplayName("所在邊索引")]
    [Description("方形／圓形：0=前、1=右、2=後、3=左；Polygon 使用輪廓邊索引。")]
    public int EdgeIndex { get; set; }

    [DisplayName("沿邊位置 mm")]
    [Description("相對於該邊中心的位置；負值向邊起點，正值向邊終點。")]
    public float HorizontalOffset { get; set; }

    [DisplayName("中心高度 mm")]
    public float Height { get; set; } = 7f;

    [DisplayName("伸出長度 mm")]
    public float Extension { get; set; } = 20f;

    [DisplayName("外徑 mm")]
    public float OuterDiameter { get; set; } = 10f;

    [DisplayName("內徑／開口直徑 mm")]
    public float InnerDiameter { get; set; } = 6f;

    public override string ToString() => $"邊 {EdgeIndex}, Ø{OuterDiameter:0.##}/{InnerDiameter:0.##}";
}

public sealed class PolygonVertex
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class ColdPlateParameters
{
    [Category("1. 底座")]
    [DisplayName("外型")]
    public ColdPlateShape Shape { get; set; } = ColdPlateShape.Rectangle;

    [Category("1. 底座")]
    [DisplayName("外部長度 mm")]
    public float OuterWidth { get; set; } = 120f;

    [Category("1. 底座")]
    [DisplayName("外部寬度 mm")]
    public float OuterDepth { get; set; } = 80f;

    [Category("1. 底座")]
    [DisplayName("外徑 mm")]
    public float OuterDiameter { get; set; } = 100f;

    [Category("1. 底座")]
    [DisplayName("外部總厚度 mm")]
    public float BaseHeight { get; set; } = 14f;

    [Category("2. 底座凹槽")]
    [DisplayName("內部尺寸模式")]
    public CavitySizingMode CavitySizing { get; set; } = CavitySizingMode.Inset;

    [Category("2. 底座凹槽")]
    [DisplayName("內縮尺寸 mm")]
    public float BaseInset { get; set; } = 8f;

    [Category("2. 底座凹槽")]
    [DisplayName("內部長度 mm")]
    public float CavityWidth { get; set; } = 104f;

    [Category("2. 底座凹槽")]
    [DisplayName("內部寬度 mm")]
    public float CavityDepthSize { get; set; } = 64f;

    [Category("2. 底座凹槽")]
    [DisplayName("內徑 mm")]
    public float CavityDiameter { get; set; } = 84f;

    [Category("2. 底座凹槽")]
    [DisplayName("內凹深度 mm")]
    public float BaseRecessDepth { get; set; } = 10f;

    [Category("3. 進出水口")]
    [DisplayName("進水口")]
    public PortParameters Inlet { get; set; } = new() { EdgeIndex = 3, HorizontalOffset = 0f };

    [Category("3. 進出水口")]
    [DisplayName("出水口")]
    public PortParameters Outlet { get; set; } = new() { EdgeIndex = 1, HorizontalOffset = 0f };

    [Category("4. 上蓋")]
    [DisplayName("自動符合底座凹槽")]
    public bool AutoFitCover { get; set; } = true;

    [Category("4. 上蓋")]
    [DisplayName("外部長度 mm")]
    public float CoverWidth { get; set; } = 104f;

    [Category("4. 上蓋")]
    [DisplayName("外部寬度 mm")]
    public float CoverDepth { get; set; } = 64f;

    [Category("4. 上蓋")]
    [DisplayName("外徑 mm")]
    public float CoverDiameter { get; set; } = 84f;

    [Category("4. 上蓋")]
    [DisplayName("高度 mm")]
    public float CoverHeight { get; set; } = 8f;

    [Category("5. 上蓋內凹")]
    [DisplayName("內部尺寸模式")]
    public CavitySizingMode CoverCavitySizing { get; set; } = CavitySizingMode.Inset;

    [Category("5. 上蓋內凹")]
    [DisplayName("內縮尺寸 mm")]
    public float CoverInset { get; set; } = 4f;

    [Category("5. 上蓋內凹")]
    [DisplayName("內部長度 mm")]
    public float CoverCavityWidth { get; set; } = 96f;

    [Category("5. 上蓋內凹")]
    [DisplayName("內部寬度 mm")]
    public float CoverCavityDepth { get; set; } = 56f;

    [Category("5. 上蓋內凹")]
    [DisplayName("內徑 mm")]
    public float CoverCavityDiameter { get; set; } = 76f;

    [Category("5. 上蓋內凹")]
    [DisplayName("內凹深度 mm")]
    public float CoverRecessDepth { get; set; } = 5f;

    [Browsable(false)]
    public BindingList<PolygonVertex> PolygonVertices { get; } =
    [
        new() { X = -60f, Y = -40f },
        new() { X = 60f, Y = -40f },
        new() { X = 60f, Y = 40f },
        new() { X = -60f, Y = 40f }
    ];

}
