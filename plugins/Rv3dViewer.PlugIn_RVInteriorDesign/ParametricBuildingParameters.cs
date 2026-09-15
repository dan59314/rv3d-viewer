namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.ComponentModel;
using Rv3dViewer.Core;

public enum WallOpeningKind
{
    Door,
    Window
}

[TypeConverter(typeof(ExpandableObjectConverter))]
internal sealed class WallOpeningParameters
{
    [Category("洞口"), DisplayName("啟用")]
    public bool Enabled { get; set; }

    [Category("洞口"), DisplayName("類型"), ReadOnly(true)]
    public WallOpeningKind Kind { get; init; }

    [Category("位置（公尺）"), DisplayName("距牆起點")]
    public float OffsetFromStart { get; set; }

    [Category("尺寸（公尺）"), DisplayName("寬度")]
    public float Width { get; set; } = .9f;

    [Category("尺寸（公尺）"), DisplayName("高度")]
    public float Height { get; set; } = 2.1f;

    [Category("位置（公尺）"), DisplayName("底部離地")]
    public float SillHeight { get; set; }

    public WallOpeningParameters Copy() => new()
    {
        Enabled = Enabled,
        Kind = Kind,
        OffsetFromStart = OffsetFromStart,
        Width = Width,
        Height = Height,
        SillHeight = SillHeight
    };

    public override string ToString() => Enabled
        ? $"距起點 {OffsetFromStart:0.##} m，{Width:0.##} × {Height:0.##} m"
        : "未啟用";
}

internal sealed class ParametricRoomParameters : IInteriorParametricParameters
{
    [Category("房間"), DisplayName("名稱")]
    public string Name { get; set; } = "房間";

    [Category("室內淨尺寸（公尺）"), DisplayName("寬度")]
    public float Width { get; set; } = 6f;

    [Category("室內淨尺寸（公尺）"), DisplayName("深度")]
    public float Depth { get; set; } = 4f;

    [Category("室內淨尺寸（公尺）"), DisplayName("高度")]
    public float Height { get; set; } = 2.8f;

    [Category("構造（公尺）"), DisplayName("牆厚")]
    public float WallThickness { get; set; } = .12f;

    [Category("構造（公尺）"), DisplayName("地板厚度")]
    public float FloorThickness { get; set; } = .1f;

    [Category("構造（公尺）"), DisplayName("天花板厚度")]
    public float CeilingThickness { get; set; } = .08f;

    [Category("構件"), DisplayName("建立地板")]
    public bool IncludeFloor { get; set; } = true;

    [Category("構件"), DisplayName("建立天花板")]
    public bool IncludeCeiling { get; set; } = true;

    public IInteriorParametricParameters CopyDefinition() => new ParametricRoomParameters
    {
        Name = Name,
        Width = Width,
        Depth = Depth,
        Height = Height,
        WallThickness = WallThickness,
        FloorThickness = FloorThickness,
        CeilingThickness = CeilingThickness,
        IncludeFloor = IncludeFloor,
        IncludeCeiling = IncludeCeiling
    };

    public SceneModel Generate(Guid? modelId = null) => ParametricBuildingGenerator.GenerateRoom(this, modelId);
}

internal sealed class ParametricWallParameters : IInteriorParametricParameters
{
    [Category("牆體"), DisplayName("名稱")]
    public string Name { get; set; } = "牆體";

    [Category("起點（公尺）"), DisplayName("X")]
    public float StartX { get; set; } = -2f;

    [Category("起點（公尺）"), DisplayName("Z")]
    public float StartZ { get; set; }

    [Category("終點（公尺）"), DisplayName("X")]
    public float EndX { get; set; } = 2f;

    [Category("終點（公尺）"), DisplayName("Z")]
    public float EndZ { get; set; }

    [Category("牆體（公尺）"), DisplayName("高度")]
    public float Height { get; set; } = 2.8f;

    [Category("牆體（公尺）"), DisplayName("厚度")]
    public float Thickness { get; set; } = .12f;

    [Category("牆體（公尺）"), DisplayName("底部標高")]
    public float BaseElevation { get; set; }

    [Category("牆體"), DisplayName("長度"), ReadOnly(true)]
    public float Length => MathF.Sqrt(MathF.Pow(EndX - StartX, 2f) + MathF.Pow(EndZ - StartZ, 2f));

    [Category("門窗洞口"), DisplayName("門洞")]
    public WallOpeningParameters DoorOpening { get; set; } = new()
    {
        Kind = WallOpeningKind.Door,
        OffsetFromStart = .5f,
        Width = .9f,
        Height = 2.1f,
        SillHeight = 0f
    };

    [Category("門窗洞口"), DisplayName("窗洞")]
    public WallOpeningParameters WindowOpening { get; set; } = new()
    {
        Kind = WallOpeningKind.Window,
        OffsetFromStart = 2.2f,
        Width = 1.2f,
        Height = 1.1f,
        SillHeight = .9f
    };

    public IInteriorParametricParameters CopyDefinition() => new ParametricWallParameters
    {
        Name = Name,
        StartX = StartX,
        StartZ = StartZ,
        EndX = EndX,
        EndZ = EndZ,
        Height = Height,
        Thickness = Thickness,
        BaseElevation = BaseElevation,
        DoorOpening = DoorOpening.Copy(),
        WindowOpening = WindowOpening.Copy()
    };

    public SceneModel Generate(Guid? modelId = null) => ParametricBuildingGenerator.GenerateWall(this, modelId);
}

public enum ParametricSlabKind
{
    Floor,
    Ceiling
}

internal sealed class ParametricSlabParameters : IInteriorParametricParameters
{
    [Category("板件"), DisplayName("名稱")]
    public string Name { get; set; } = "地板";

    [Category("板件"), DisplayName("類型"), ReadOnly(true)]
    public ParametricSlabKind Kind { get; init; }

    [Category("尺寸（公尺）"), DisplayName("寬度")]
    public float Width { get; set; } = 6f;

    [Category("尺寸（公尺）"), DisplayName("深度")]
    public float Depth { get; set; } = 4f;

    [Category("尺寸（公尺）"), DisplayName("厚度")]
    public float Thickness { get; set; } = .1f;

    [Category("位置（公尺）"), DisplayName("完成面標高")]
    public float Elevation { get; set; }

    public IInteriorParametricParameters CopyDefinition() => new ParametricSlabParameters
    {
        Name = Name,
        Kind = Kind,
        Width = Width,
        Depth = Depth,
        Thickness = Thickness,
        Elevation = Elevation
    };

    public SceneModel Generate(Guid? modelId = null) => ParametricBuildingGenerator.GenerateSlab(this, modelId);
}

internal sealed class ParametricBeamParameters : IInteriorParametricParameters
{
    [Category("樑"), DisplayName("名稱")]
    public string Name { get; set; } = "樑";

    [Category("起點（公尺）"), DisplayName("X")]
    public float StartX { get; set; } = -2f;

    [Category("起點（公尺）"), DisplayName("Z")]
    public float StartZ { get; set; }

    [Category("終點（公尺）"), DisplayName("X")]
    public float EndX { get; set; } = 2f;

    [Category("終點（公尺）"), DisplayName("Z")]
    public float EndZ { get; set; }

    [Category("斷面（公尺）"), DisplayName("寬度")]
    public float Width { get; set; } = .2f;

    [Category("斷面（公尺）"), DisplayName("高度")]
    public float Height { get; set; } = .3f;

    [Category("位置（公尺）"), DisplayName("底部標高")]
    public float BaseElevation { get; set; } = 2.4f;

    [Category("樑"), DisplayName("長度"), ReadOnly(true)]
    public float Length => MathF.Sqrt(MathF.Pow(EndX - StartX, 2f) + MathF.Pow(EndZ - StartZ, 2f));

    public IInteriorParametricParameters CopyDefinition() => new ParametricBeamParameters
    {
        Name = Name,
        StartX = StartX,
        StartZ = StartZ,
        EndX = EndX,
        EndZ = EndZ,
        Width = Width,
        Height = Height,
        BaseElevation = BaseElevation
    };

    public SceneModel Generate(Guid? modelId = null) => ParametricBuildingGenerator.GenerateBeam(this, modelId);
}

public enum ParametricColumnShape
{
    Rectangular,
    Circular
}

internal sealed class ParametricColumnParameters : IInteriorParametricParameters
{
    [Category("柱"), DisplayName("名稱")]
    public string Name { get; set; } = "柱";

    [Category("柱"), DisplayName("斷面形狀")]
    public ParametricColumnShape Shape { get; set; }

    [Category("位置（公尺）"), DisplayName("中心 X")]
    public float PositionX { get; set; }

    [Category("位置（公尺）"), DisplayName("中心 Z")]
    public float PositionZ { get; set; }

    [Category("矩形斷面（公尺）"), DisplayName("寬度")]
    public float Width { get; set; } = .35f;

    [Category("矩形斷面（公尺）"), DisplayName("深度")]
    public float Depth { get; set; } = .35f;

    [Category("圓形斷面（公尺）"), DisplayName("直徑")]
    public float Diameter { get; set; } = .35f;

    [Category("尺寸（公尺）"), DisplayName("高度")]
    public float Height { get; set; } = 2.6f;

    [Category("位置（公尺）"), DisplayName("底部標高")]
    public float BaseElevation { get; set; }

    [Category("圓形斷面"), DisplayName("圓周分段數")]
    public int Segments { get; set; } = 32;

    public IInteriorParametricParameters CopyDefinition() => new ParametricColumnParameters
    {
        Name = Name,
        Shape = Shape,
        PositionX = PositionX,
        PositionZ = PositionZ,
        Width = Width,
        Depth = Depth,
        Diameter = Diameter,
        Height = Height,
        BaseElevation = BaseElevation,
        Segments = Segments
    };

    public SceneModel Generate(Guid? modelId = null) => ParametricBuildingGenerator.GenerateColumn(this, modelId);
}

internal sealed class ParametricDoorParameters : IInteriorParametricParameters
{
    [Category("門"), DisplayName("名稱")]
    public string Name { get; set; } = "門";

    [Category("總尺寸（公尺）"), DisplayName("寬度")]
    public float Width { get; set; } = .9f;

    [Category("總尺寸（公尺）"), DisplayName("高度")]
    public float Height { get; set; } = 2.1f;

    [Category("門扇（公尺）"), DisplayName("厚度")]
    public float Thickness { get; set; } = .045f;

    [Category("門框（公尺）"), DisplayName("框寬")]
    public float FrameWidth { get; set; } = .07f;

    [Category("門框（公尺）"), DisplayName("框深")]
    public float FrameDepth { get; set; } = .12f;

    [Category("門框"), DisplayName("建立門框")]
    public bool IncludeFrame { get; set; } = true;

    [Category("門把（公尺）"), DisplayName("離地高度")]
    public float HandleHeight { get; set; } = 1f;

    public IInteriorParametricParameters CopyDefinition() => new ParametricDoorParameters
    {
        Name = Name, Width = Width, Height = Height, Thickness = Thickness,
        FrameWidth = FrameWidth, FrameDepth = FrameDepth, IncludeFrame = IncludeFrame,
        HandleHeight = HandleHeight
    };

    public SceneModel Generate(Guid? modelId = null) => ParametricBuildingGenerator.GenerateDoor(this, modelId);
}

internal sealed class ParametricWindowParameters : IInteriorParametricParameters
{
    [Category("窗戶"), DisplayName("名稱")]
    public string Name { get; set; } = "窗戶";

    [Category("總尺寸（公尺）"), DisplayName("寬度")]
    public float Width { get; set; } = 1.2f;

    [Category("總尺寸（公尺）"), DisplayName("高度")]
    public float Height { get; set; } = 1.1f;

    [Category("位置（公尺）"), DisplayName("窗台高度")]
    public float SillHeight { get; set; } = .9f;

    [Category("窗框（公尺）"), DisplayName("框寬")]
    public float FrameWidth { get; set; } = .06f;

    [Category("窗框（公尺）"), DisplayName("框深")]
    public float FrameDepth { get; set; } = .1f;

    [Category("窗框"), DisplayName("中央直框")]
    public bool IncludeCenterMullion { get; set; } = true;

    [Category("玻璃（公尺）"), DisplayName("厚度")]
    public float GlassThickness { get; set; } = .008f;

    public IInteriorParametricParameters CopyDefinition() => new ParametricWindowParameters
    {
        Name = Name, Width = Width, Height = Height, SillHeight = SillHeight,
        FrameWidth = FrameWidth, FrameDepth = FrameDepth,
        IncludeCenterMullion = IncludeCenterMullion, GlassThickness = GlassThickness
    };

    public SceneModel Generate(Guid? modelId = null) => ParametricBuildingGenerator.GenerateWindow(this, modelId);
}

internal sealed class ParametricStairParameters : IInteriorParametricParameters
{
    [Category("樓梯"), DisplayName("名稱")]
    public string Name { get; set; } = "直跑樓梯";

    [Category("尺寸（公尺）"), DisplayName("梯寬")]
    public float Width { get; set; } = 1f;

    [Category("尺寸（公尺）"), DisplayName("總水平長度")]
    public float TotalRun { get; set; } = 3.6f;

    [Category("尺寸（公尺）"), DisplayName("總高度")]
    public float TotalRise { get; set; } = 2.8f;

    [Category("樓梯"), DisplayName("階數")]
    public int StepCount { get; set; } = 16;

    [Category("位置（公尺）"), DisplayName("起始標高")]
    public float BaseElevation { get; set; }

    [Category("形式"), DisplayName("實心梯段")]
    public bool SolidSteps { get; set; } = true;

    [Category("形式（公尺）"), DisplayName("踏板厚度")]
    public float TreadThickness { get; set; } = .06f;

    [Category("樓梯"), DisplayName("每階高度"), ReadOnly(true)]
    public float RiserHeight => StepCount > 0 ? TotalRise / StepCount : 0f;

    [Category("樓梯"), DisplayName("每階深度"), ReadOnly(true)]
    public float TreadDepth => StepCount > 0 ? TotalRun / StepCount : 0f;

    public IInteriorParametricParameters CopyDefinition() => new ParametricStairParameters
    {
        Name = Name, Width = Width, TotalRun = TotalRun, TotalRise = TotalRise,
        StepCount = StepCount, BaseElevation = BaseElevation, SolidSteps = SolidSteps,
        TreadThickness = TreadThickness
    };

    public SceneModel Generate(Guid? modelId = null) => ParametricBuildingGenerator.GenerateStair(this, modelId);
}

internal static class ParametricBuildingParametersFactory
{
    public static IInteriorParametricParameters? FromTreeText(string text) => text switch
    {
        "房間" => new ParametricRoomParameters(),
        "牆體" => new ParametricWallParameters(),
        "門窗牆體" => new ParametricWallParameters
        {
            Name = "門窗牆體",
            DoorOpening = new WallOpeningParameters
            {
                Enabled = true, Kind = WallOpeningKind.Door, OffsetFromStart = .35f,
                Width = .9f, Height = 2.1f
            },
            WindowOpening = new WallOpeningParameters
            {
                Enabled = true, Kind = WallOpeningKind.Window, OffsetFromStart = 2.15f,
                Width = 1.2f, Height = 1.1f, SillHeight = .9f
            }
        },
        "地板" => new ParametricSlabParameters { Name = "地板", Kind = ParametricSlabKind.Floor, Elevation = 0f },
        "天花板" => new ParametricSlabParameters { Name = "天花板", Kind = ParametricSlabKind.Ceiling, Elevation = 2.8f, Thickness = .08f },
        "樑" => new ParametricBeamParameters(),
        "柱" => new ParametricColumnParameters(),
        "門" => new ParametricDoorParameters(),
        "窗戶" => new ParametricWindowParameters(),
        "樓梯" => new ParametricStairParameters(),
        _ => null
    };
}
