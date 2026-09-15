namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.ComponentModel;
using Rv3dViewer.Core;

internal interface IInteriorParametricParameters
{
    string Name { get; set; }
    IInteriorParametricParameters CopyDefinition();
    SceneModel Generate(Guid? modelId = null);
}

public enum ParametricPrimitiveKind
{
    Plane,
    Box,
    Sphere,
    Cylinder,
    HalfCylinder,
    Cone,
    PolygonCone
}

internal enum InteriorDetailPreset
{
    None,
    Rug,
    CoffeeTable,
    DecorativeGlobe,
    Stool,
    Sofa,
    PendantLamp,
    Planter
}

internal sealed class ParametricPrimitiveParameters : IInteriorParametricParameters
{
    [Category("模型"), DisplayName("名稱")]
    public string Name { get; set; } = "參數模型";

    [Category("模型"), DisplayName("類型"), ReadOnly(true)]
    public ParametricPrimitiveKind Kind { get; init; }

    [Browsable(false)]
    public InteriorDetailPreset DetailPreset { get; init; }

    [Category("尺寸（公尺）"), DisplayName("寬度")]
    public float Width { get; set; } = 1f;

    [Category("尺寸（公尺）"), DisplayName("深度")]
    public float Depth { get; set; } = 1f;

    [Category("尺寸（公尺）"), DisplayName("高度")]
    public float Height { get; set; } = 1f;

    [Category("尺寸（公尺）"), DisplayName("半徑")]
    public float Radius { get; set; } = .5f;

    [Category("網格"), DisplayName("圓周分段")]
    public int Segments { get; set; } = 32;

    [Category("網格"), DisplayName("高度分段")]
    public int HeightSegments { get; set; } = 1;

    [Category("網格"), DisplayName("多邊形邊數")]
    public int PolygonSides { get; set; } = 6;

    public ParametricPrimitiveParameters Copy() => new()
    {
        Name = Name,
        Kind = Kind,
        DetailPreset = DetailPreset,
        Width = Width,
        Depth = Depth,
        Height = Height,
        Radius = Radius,
        Segments = Segments,
        HeightSegments = HeightSegments,
        PolygonSides = PolygonSides
    };

    public IInteriorParametricParameters CopyDefinition() => Copy();

    public SceneModel Generate(Guid? modelId = null) => ParametricPrimitiveGenerator.Generate(this, modelId);

    public static ParametricPrimitiveParameters? FromTreeText(string text) => text switch
    {
        "平面" => Create("平面", ParametricPrimitiveKind.Plane),
        "長方體" => Create("長方體", ParametricPrimitiveKind.Box),
        "球體" => Create("球體", ParametricPrimitiveKind.Sphere),
        "圓柱體" => Create("圓柱體", ParametricPrimitiveKind.Cylinder),
        "半圓柱體" => Create("半圓柱體", ParametricPrimitiveKind.HalfCylinder),
        "圓錐" => Create("圓錐", ParametricPrimitiveKind.Cone),
        "多邊錐體" => Create("多邊錐體", ParametricPrimitiveKind.PolygonCone),
        _ => null
    };

    private static ParametricPrimitiveParameters Create(string name, ParametricPrimitiveKind kind) =>
        new() { Name = name, Kind = kind };
}

internal sealed class ParametricDesignObject
{
    public required Guid Id { get; init; }
    public required IInteriorParametricParameters Parameters { get; init; }
    public required Rv3dViewer.Core.SceneModel Model { get; set; }
    public bool IsDefaultSceneObject { get; init; }
}

internal sealed record InteriorMeshTreeItem(ParametricDesignObject DesignObject, int MeshIndex);
