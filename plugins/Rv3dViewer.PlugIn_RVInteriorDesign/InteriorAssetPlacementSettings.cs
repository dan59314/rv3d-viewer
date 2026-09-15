namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.ComponentModel;
using System.Numerics;
using Rv3dViewer.Core;

internal enum InteriorAssetUnit
{
    自動判斷,
    公尺,
    公分,
    公釐
}

internal enum InteriorAssetPivot
{
    底部中心,
    模型中心,
    原始原點
}

internal enum InteriorAssetPlacementMode
{
    自動,
    自由,
    格點,
    落地
}

internal enum InteriorAssetCollisionPolicy
{
    禁止重疊,
    警告但允許
}

internal sealed class InteriorAssetPlacementSettings
{
    private decimal _widthCentimeters;
    private decimal _depthCentimeters;
    private decimal _heightCentimeters;
    private char _lastEditedDimension = 'W';
    private bool _updating;

    [Category("尺寸"), DisplayName("原始單位")]
    public InteriorAssetUnit Unit { get; set; } = InteriorAssetUnit.自動判斷;

    [Category("尺寸"), DisplayName("等比縮放")]
    public bool KeepAspectRatio { get; set; } = true;

    [Category("尺寸"), DisplayName("寬（公分）")]
    public decimal WidthCentimeters
    {
        get => _widthCentimeters;
        set => SetDimension(ref _widthCentimeters, value, 'W');
    }

    [Category("尺寸"), DisplayName("深（公分）")]
    public decimal DepthCentimeters
    {
        get => _depthCentimeters;
        set => SetDimension(ref _depthCentimeters, value, 'D');
    }

    [Category("尺寸"), DisplayName("高（公分）")]
    public decimal HeightCentimeters
    {
        get => _heightCentimeters;
        set => SetDimension(ref _heightCentimeters, value, 'H');
    }

    [Category("置放"), DisplayName("基準點")]
    public InteriorAssetPivot Pivot { get; set; } = InteriorAssetPivot.底部中心;

    [Browsable(false)]
    public bool SnapToGrid { get; set; } = true;

    [Category("智慧置放"), DisplayName("置放模式")]
    public InteriorAssetPlacementMode PlacementMode { get; set; } = InteriorAssetPlacementMode.自動;

    [Category("智慧置放"), DisplayName("碰撞處理")]
    public InteriorAssetCollisionPolicy CollisionPolicy { get; set; } = InteriorAssetCollisionPolicy.禁止重疊;

    [Category("智慧置放"), DisplayName("碰撞容許值（公分）")]
    public decimal CollisionToleranceCentimeters { get; set; } = 0.5m;

    [Browsable(false)]
    internal bool UsesGrid => PlacementMode is InteriorAssetPlacementMode.自動 or InteriorAssetPlacementMode.格點;

    [Browsable(false)]
    internal InteriorAssetPivot EffectivePivot => PlacementMode is InteriorAssetPlacementMode.自動 or
        InteriorAssetPlacementMode.落地 ? InteriorAssetPivot.底部中心 : Pivot;

    internal static InteriorAssetPlacementSettings FromAsset(InteriorAssetDescriptor asset)
    {
        var settings = new InteriorAssetPlacementSettings();
        settings.ResetDimensions(asset);
        return settings;
    }

    internal void ResetDimensions(InteriorAssetDescriptor asset)
    {
        var rawSize = new Vector3(
            asset.RawWidth > 0f ? asset.RawWidth : (float)asset.Width / 100f,
            asset.RawHeight > 0f ? asset.RawHeight : (float)asset.Height / 100f,
            asset.RawDepth > 0f ? asset.RawDepth : (float)asset.Depth / 100f);
        var factor = UnitToMeters(Unit, rawSize);
        _widthCentimeters = Math.Max(.01m, (decimal)rawSize.X * (decimal)factor * 100m);
        _depthCentimeters = Math.Max(.01m, (decimal)rawSize.Z * (decimal)factor * 100m);
        _heightCentimeters = Math.Max(.01m, (decimal)rawSize.Y * (decimal)factor * 100m);
        _lastEditedDimension = 'W';
    }

    internal InteriorAssetPlacementSettings Copy() => new()
    {
        Unit = Unit,
        KeepAspectRatio = KeepAspectRatio,
        _widthCentimeters = _widthCentimeters,
        _depthCentimeters = _depthCentimeters,
        _heightCentimeters = _heightCentimeters,
        _lastEditedDimension = _lastEditedDimension,
        Pivot = Pivot,
        SnapToGrid = UsesGrid,
        PlacementMode = PlacementMode,
        CollisionPolicy = CollisionPolicy,
        CollisionToleranceCentimeters = CollisionToleranceCentimeters
    };

    internal Vector3 CalculateScale(Vector3 rawSize)
    {
        var unitScale = UnitToMeters(Unit, rawSize);
        var natural = rawSize * unitScale;
        var desired = new Vector3(
            (float)_widthCentimeters / 100f,
            (float)_heightCentimeters / 100f,
            (float)_depthCentimeters / 100f);
        if (!KeepAspectRatio)
            return new Vector3(
                RatioOrUnit(desired.X, natural.X, unitScale),
                RatioOrUnit(desired.Y, natural.Y, unitScale),
                RatioOrUnit(desired.Z, natural.Z, unitScale));

        var (requested, actual) = _lastEditedDimension switch
        {
            'H' => (desired.Y, natural.Y),
            'D' => (desired.Z, natural.Z),
            _ => (desired.X, natural.X)
        };
        var scale = actual > .000001f && requested > 0f ? requested / actual * unitScale : unitScale;
        return new Vector3(scale);
    }

    internal static Vector3 CalculatePosition(SceneBounds bounds, Vector3 scale, Vector3 groundPoint,
        InteriorAssetPivot pivot)
    {
        var scaledMinimum = bounds.Minimum * scale;
        var scaledCenter = bounds.Center * scale;
        return pivot switch
        {
            InteriorAssetPivot.原始原點 => groundPoint,
            InteriorAssetPivot.模型中心 => groundPoint - scaledCenter,
            _ => new Vector3(groundPoint.X - scaledCenter.X, groundPoint.Y - scaledMinimum.Y,
                groundPoint.Z - scaledCenter.Z)
        };
    }

    private void SetDimension(ref decimal field, decimal value, char dimension)
    {
        value = Math.Clamp(value, 0.01m, 1000000m);
        if (_updating || !KeepAspectRatio || field <= 0m)
        {
            field = value;
            _lastEditedDimension = dimension;
            return;
        }
        var ratio = value / field;
        _updating = true;
        field = value;
        if (dimension != 'W') _widthCentimeters = Math.Max(.01m, _widthCentimeters * ratio);
        if (dimension != 'D') _depthCentimeters = Math.Max(.01m, _depthCentimeters * ratio);
        if (dimension != 'H') _heightCentimeters = Math.Max(.01m, _heightCentimeters * ratio);
        _updating = false;
        _lastEditedDimension = dimension;
    }

    private static float RatioOrUnit(float desired, float natural, float unitScale) =>
        desired > 0f && natural > .000001f ? desired / natural * unitScale : unitScale;

    private static float UnitToMeters(InteriorAssetUnit unit, Vector3 rawSize)
    {
        if (unit == InteriorAssetUnit.公尺) return 1f;
        if (unit == InteriorAssetUnit.公分) return .01f;
        if (unit == InteriorAssetUnit.公釐) return .001f;
        var largest = Math.Max(rawSize.X, Math.Max(rawSize.Y, rawSize.Z));
        return largest > 100f ? .001f : largest > 10f ? .01f : 1f;
    }
}

internal sealed class InteriorAssetPlacementRequestEventArgs(
    InteriorAssetDescriptor asset, InteriorAssetPlacementSettings settings) : EventArgs
{
    internal InteriorAssetDescriptor Asset { get; } = asset;
    internal InteriorAssetPlacementSettings Settings { get; } = settings;
}

internal sealed record InteriorAssetDragData(
    InteriorAssetDescriptor Asset, InteriorAssetPlacementSettings Settings)
{
    internal const string DataFormat = "Rv3dViewer.RVInteriorDesign.InteriorAsset";
}
