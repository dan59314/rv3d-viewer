namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.ComponentModel;
using Rv3dViewer.Core;

internal sealed class ImportedAssetParameters : IInteriorParametricParameters
{
    [Category("模型"), DisplayName("名稱")]
    public string Name { get; set; } = "匯入模型";

    [Category("模型庫"), DisplayName("分類"), ReadOnly(true)]
    public string Category { get; set; } = "使用者自訂";

    [Category("模型庫"), DisplayName("來源檔案"), ReadOnly(true)]
    public string ModelPath { get; set; } = string.Empty;

    [Category("模型庫"), DisplayName("授權"), ReadOnly(true)]
    public string License { get; set; } = "由使用者確認";

    [Category("模型庫"), DisplayName("來源網址"), ReadOnly(true)]
    public string SourceUrl { get; set; } = string.Empty;

    [Category("模型庫"), DisplayName("品質檢查"), ReadOnly(true)]
    public string QualityStatus { get; set; } = "未檢查";

    [Category("置放"), DisplayName("原始單位"), ReadOnly(true)]
    public InteriorAssetUnit Unit { get; set; } = InteriorAssetUnit.自動判斷;

    [Category("置放"), DisplayName("基準點"), ReadOnly(true)]
    public InteriorAssetPivot Pivot { get; set; } = InteriorAssetPivot.底部中心;

    [Category("置放"), DisplayName("吸附格點"), ReadOnly(true)]
    public bool SnapToGrid { get; set; } = true;

    [Category("置放"), DisplayName("置放模式"), ReadOnly(true)]
    public InteriorAssetPlacementMode PlacementMode { get; set; } = InteriorAssetPlacementMode.自動;

    [Category("置放"), DisplayName("碰撞處理"), ReadOnly(true)]
    public InteriorAssetCollisionPolicy CollisionPolicy { get; set; } = InteriorAssetCollisionPolicy.禁止重疊;

    [Category("置放"), DisplayName("碰撞容許值（公分）"), ReadOnly(true)]
    public decimal CollisionToleranceCentimeters { get; set; } = 0.5m;

    [Category("置放"), DisplayName("目標寬（公分）"), ReadOnly(true)]
    public decimal TargetWidthCentimeters { get; set; }

    [Category("置放"), DisplayName("目標深（公分）"), ReadOnly(true)]
    public decimal TargetDepthCentimeters { get; set; }

    [Category("置放"), DisplayName("目標高（公分）"), ReadOnly(true)]
    public decimal TargetHeightCentimeters { get; set; }

    public IInteriorParametricParameters CopyDefinition() => new ImportedAssetParameters
    {
        Name = Name,
        Category = Category,
        ModelPath = ModelPath,
        License = License,
        SourceUrl = SourceUrl,
        QualityStatus = QualityStatus,
        Unit = Unit,
        Pivot = Pivot,
        SnapToGrid = SnapToGrid,
        PlacementMode = PlacementMode,
        CollisionPolicy = CollisionPolicy,
        CollisionToleranceCentimeters = CollisionToleranceCentimeters,
        TargetWidthCentimeters = TargetWidthCentimeters,
        TargetDepthCentimeters = TargetDepthCentimeters,
        TargetHeightCentimeters = TargetHeightCentimeters
    };

    public SceneModel Generate(Guid? modelId = null)
    {
        var model = InteriorModelImportService.ImportAsync(ModelPath).GetAwaiter().GetResult();
        model.Id = modelId ?? Guid.NewGuid();
        model.Name = Name;
        model.IsProcedural = true;
        model.CaptureMeshMaterialIndices();
        model.CaptureProceduralGeometry();
        return model;
    }
}
