namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.ComponentModel;

internal sealed class InteriorAssetDescriptor
{
    [Category("識別"), DisplayName("名稱")]
    public required string Name { get; init; }

    [Category("識別"), DisplayName("分類")]
    public required string Category { get; init; }

    [Category("識別"), DisplayName("子分類")]
    public required string Subcategory { get; init; }

    [Category("資產"), DisplayName("來源")]
    public required string Source { get; init; }

    [Category("資產"), DisplayName("授權")]
    public required string License { get; init; }

    [Category("資產"), DisplayName("來源網址")]
    public string SourceUrl { get; init; } = string.Empty;

    [Category("資產"), DisplayName("本機模型")]
    public string ModelPath { get; init; } = string.Empty;

    [Category("建議尺寸（公分）"), DisplayName("寬")]
    public decimal Width { get; init; }

    [Category("建議尺寸（公分）"), DisplayName("深")]
    public decimal Depth { get; init; }

    [Category("建議尺寸（公分）"), DisplayName("高")]
    public decimal Height { get; init; }

    [Browsable(false)]
    public float RawWidth { get; init; }

    [Browsable(false)]
    public float RawDepth { get; init; }

    [Browsable(false)]
    public float RawHeight { get; init; }

    [Category("品質檢查"), DisplayName("品質")]
    public string QualityStatus { get; init; } = "未檢查";

    [Category("品質檢查"), DisplayName("說明")]
    public string QualityMessage { get; init; } = "尚未執行模型品質檢查。";

    [Category("品質檢查"), DisplayName("Mesh 數")]
    public int MeshCount { get; init; }

    [Category("品質檢查"), DisplayName("三角形數")]
    public long TriangleCount { get; init; }

    [Category("品質檢查"), DisplayName("材質數")]
    public int MaterialCount { get; init; }

    [Category("品質檢查"), DisplayName("貼圖數")]
    public int TextureCount { get; init; }

    [Category("品質檢查"), DisplayName("遺失貼圖")]
    public int MissingTextureCount { get; init; }

    [Category("品質檢查"), DisplayName("檔案大小")]
    public string FileSizeText => FileSizeBytes <= 0 ? "未知" : $"{FileSizeBytes / 1024d / 1024d:0.##} MB";

    [Browsable(false)]
    public long FileSizeBytes { get; init; }

    [Category("PBR"), DisplayName("基礎色貼圖")]
    public bool HasBaseColorTexture { get; init; }

    [Category("PBR"), DisplayName("法線貼圖")]
    public bool HasNormalTexture { get; init; }

    [Category("PBR"), DisplayName("金屬度／粗糙度")]
    public bool HasMetallicRoughnessTexture { get; init; }

    [Browsable(false)]
    public bool IsFavorite { get; init; }

    [Browsable(false)]
    public bool CanPlace => !string.IsNullOrWhiteSpace(ModelPath) && File.Exists(ModelPath);

    [Browsable(false)]
    public bool IsManagedLocalAsset => Source.StartsWith("本機", StringComparison.Ordinal) &&
                                       !string.IsNullOrWhiteSpace(ModelPath);

    [Browsable(false)]
    public string DimensionsText => Width > 0m && Depth > 0m && Height > 0m
        ? $"{Width:0.#}×{Depth:0.#}×{Height:0.#} cm"
        : "未知";

    [Browsable(false)]
    public string QualitySummary => $"{QualityStatus}｜{MeshCount} Mesh｜{TriangleCount:N0} 三角形｜{TextureCount} 貼圖";

    internal InteriorAssetDescriptor WithFavorite(bool isFavorite) => new()
    {
        Name = Name,
        Category = Category,
        Subcategory = Subcategory,
        Source = Source,
        License = License,
        SourceUrl = SourceUrl,
        ModelPath = ModelPath,
        Width = Width,
        Depth = Depth,
        Height = Height,
        RawWidth = RawWidth,
        RawDepth = RawDepth,
        RawHeight = RawHeight,
        QualityStatus = QualityStatus,
        QualityMessage = QualityMessage,
        MeshCount = MeshCount,
        TriangleCount = TriangleCount,
        MaterialCount = MaterialCount,
        TextureCount = TextureCount,
        MissingTextureCount = MissingTextureCount,
        FileSizeBytes = FileSizeBytes,
        HasBaseColorTexture = HasBaseColorTexture,
        HasNormalTexture = HasNormalTexture,
        HasMetallicRoughnessTexture = HasMetallicRoughnessTexture,
        IsFavorite = isFavorite
    };

    public override string ToString() => Name;
}
