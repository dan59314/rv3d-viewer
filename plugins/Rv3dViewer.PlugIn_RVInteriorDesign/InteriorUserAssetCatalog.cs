namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Text.Json;
using System.Text.Json.Nodes;

internal static class InteriorUserAssetCatalog
{
    private sealed class CatalogDocument
    {
        public int Version { get; set; } = 1;
        public List<CatalogItem> Items { get; set; } = [];
    }

    private sealed class CatalogItem
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "使用者自訂";
        public string Subcategory { get; set; } = "GLB 模型";
        public string ModelPath { get; set; } = string.Empty;
        public string License { get; set; } = "由使用者確認";
        public string SourceUrl { get; set; } = string.Empty;
        public decimal Width { get; set; }
        public decimal Depth { get; set; }
        public decimal Height { get; set; }
        public float RawWidth { get; set; }
        public float RawDepth { get; set; }
        public float RawHeight { get; set; }
        public long FileSizeBytes { get; set; }
        public int MeshCount { get; set; }
        public long TriangleCount { get; set; }
        public int MaterialCount { get; set; }
        public int TextureCount { get; set; }
        public int MissingTextureCount { get; set; }
        public bool HasBaseColorTexture { get; set; }
        public bool HasNormalTexture { get; set; }
        public bool HasMetallicRoughnessTexture { get; set; }
        public string QualityStatus { get; set; } = "未檢查";
        public string QualityMessage { get; set; } = "尚未執行模型品質檢查。";
    }

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static string CatalogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "RVInteriorDesign", "ModelLibrary");
    private static string ModelsDirectory => Path.Combine(CatalogDirectory, "Models");
    private static string CatalogPath => Path.Combine(CatalogDirectory, "model-library.json");

    internal static IReadOnlyList<InteriorAssetDescriptor> Load()
    {
        if (!File.Exists(CatalogPath))
            return [];
        try
        {
            var document = JsonSerializer.Deserialize<CatalogDocument>(File.ReadAllText(CatalogPath), Options)
                           ?? new CatalogDocument();
            document.Items ??= [];
            return document.Items
                .Where(item => !string.IsNullOrWhiteSpace(item.ModelPath))
                .Select(ToDescriptor)
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    internal static InteriorAssetDescriptor? FindBySourceUrl(string sourceUrl)
    {
        var normalizedSourceUrl = NormalizeSourceUrl(sourceUrl);
        if (normalizedSourceUrl.Length == 0)
            return null;
        return Load()
            .Where(asset => string.Equals(NormalizeSourceUrl(asset.SourceUrl), normalizedSourceUrl,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(asset => asset.CanPlace)
            .FirstOrDefault();
    }

    internal static InteriorAssetDescriptor StoreModel(string sourcePath, string category)
    {
        var fullSourcePath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSourcePath))
            throw new FileNotFoundException("找不到要加入的模型。", fullSourcePath);
        var extension = Path.GetExtension(fullSourcePath).ToLowerInvariant();
        if (extension is not ".glb" and not ".gltf")
            throw new NotSupportedException("本機模型庫目前支援 GLB 與 GLTF。");

        Directory.CreateDirectory(ModelsDirectory);
        var safeName = MakeSafeFileName(Path.GetFileNameWithoutExtension(fullSourcePath));
        var destination = extension == ".glb"
            ? StoreGlb(fullSourcePath, safeName)
            : StoreGltfPackage(fullSourcePath, safeName);
        return new InteriorAssetDescriptor
        {
            Name = Path.GetFileNameWithoutExtension(fullSourcePath),
            Category = InteriorAssetCatalog.Categories.Contains(category) ? category : "使用者自訂",
            Subcategory = extension == ".glb" ? "GLB 模型" : "GLTF 資產組",
            Source = "本機模型庫",
            License = "由使用者確認",
            ModelPath = destination
        };
    }

    internal static void DeleteStoredAsset(string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath)) return;
        var fullPath = Path.GetFullPath(modelPath);
        var fullModelsDirectory = Path.GetFullPath(ModelsDirectory) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(fullModelsDirectory, StringComparison.OrdinalIgnoreCase))
            return;
        if (File.Exists(fullPath)) File.Delete(fullPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory) &&
            !string.Equals(directory, ModelsDirectory, StringComparison.OrdinalIgnoreCase) &&
            Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    internal static InteriorAssetDescriptor Save(InteriorAssetDescriptor descriptor,
        bool replaceMatchingSourceUrl = false)
    {
        var document = ReadDocument();
        var normalizedSourceUrl = replaceMatchingSourceUrl
            ? NormalizeSourceUrl(descriptor.SourceUrl)
            : string.Empty;
        document.Items.RemoveAll(item =>
            string.Equals(item.ModelPath, descriptor.ModelPath, StringComparison.OrdinalIgnoreCase) ||
            (normalizedSourceUrl.Length > 0 &&
             string.Equals(NormalizeSourceUrl(item.SourceUrl), normalizedSourceUrl,
                 StringComparison.OrdinalIgnoreCase)));
        document.Items.Add(new CatalogItem
        {
            Name = descriptor.Name,
            Category = descriptor.Category,
            Subcategory = descriptor.Subcategory,
            ModelPath = descriptor.ModelPath,
            License = descriptor.License,
            SourceUrl = descriptor.SourceUrl,
            Width = descriptor.Width,
            Depth = descriptor.Depth,
            Height = descriptor.Height,
            RawWidth = descriptor.RawWidth,
            RawDepth = descriptor.RawDepth,
            RawHeight = descriptor.RawHeight,
            FileSizeBytes = descriptor.FileSizeBytes,
            MeshCount = descriptor.MeshCount,
            TriangleCount = descriptor.TriangleCount,
            MaterialCount = descriptor.MaterialCount,
            TextureCount = descriptor.TextureCount,
            MissingTextureCount = descriptor.MissingTextureCount,
            HasBaseColorTexture = descriptor.HasBaseColorTexture,
            HasNormalTexture = descriptor.HasNormalTexture,
            HasMetallicRoughnessTexture = descriptor.HasMetallicRoughnessTexture,
            QualityStatus = descriptor.QualityStatus,
            QualityMessage = descriptor.QualityMessage
        });
        Directory.CreateDirectory(CatalogDirectory);
        WriteDocument(document);
        return descriptor;
    }

    private static string NormalizeSourceUrl(string? sourceUrl) =>
        sourceUrl?.Trim().TrimEnd('/') ?? string.Empty;

    internal static InteriorAssetDescriptor WithMetadata(InteriorAssetDescriptor descriptor,
        string name, string category, string license, string? sourceUrl = null) => new()
    {
        Name = name.Trim(),
        Category = InteriorAssetCatalog.Categories.Contains(category) ? category : "使用者自訂",
        Subcategory = descriptor.Subcategory,
        Source = descriptor.Source,
        License = license.Trim(),
        SourceUrl = sourceUrl?.Trim() ?? descriptor.SourceUrl,
        ModelPath = descriptor.ModelPath,
        Width = descriptor.Width,
        Depth = descriptor.Depth,
        Height = descriptor.Height,
        RawWidth = descriptor.RawWidth,
        RawDepth = descriptor.RawDepth,
        RawHeight = descriptor.RawHeight,
        FileSizeBytes = descriptor.FileSizeBytes,
        MeshCount = descriptor.MeshCount,
        TriangleCount = descriptor.TriangleCount,
        MaterialCount = descriptor.MaterialCount,
        TextureCount = descriptor.TextureCount,
        MissingTextureCount = descriptor.MissingTextureCount,
        HasBaseColorTexture = descriptor.HasBaseColorTexture,
        HasNormalTexture = descriptor.HasNormalTexture,
        HasMetallicRoughnessTexture = descriptor.HasMetallicRoughnessTexture,
        QualityStatus = descriptor.QualityStatus,
        QualityMessage = descriptor.QualityMessage,
        IsFavorite = descriptor.IsFavorite
    };

    internal static void Remove(InteriorAssetDescriptor descriptor)
    {
        var document = ReadDocument();
        document.Items.RemoveAll(item =>
            string.Equals(item.ModelPath, descriptor.ModelPath, StringComparison.OrdinalIgnoreCase));
        WriteDocument(document);
        InteriorAssetThumbnailService.Remove(descriptor);
        DeleteStoredAsset(descriptor.ModelPath);
    }

    internal static InteriorAssetDescriptor WithDimensions(InteriorAssetDescriptor descriptor,
        float rawWidth, float rawDepth, float rawHeight)
    {
        var factor = InferUnitScale(new System.Numerics.Vector3(rawWidth, rawHeight, rawDepth));
        return new InteriorAssetDescriptor
        {
        Name = descriptor.Name,
        Category = descriptor.Category,
        Subcategory = descriptor.Subcategory,
        Source = descriptor.Source,
        License = descriptor.License,
        SourceUrl = descriptor.SourceUrl,
        ModelPath = descriptor.ModelPath,
        Width = ToCentimeters(rawWidth, factor),
        Depth = ToCentimeters(rawDepth, factor),
        Height = ToCentimeters(rawHeight, factor),
        RawWidth = rawWidth,
        RawDepth = rawDepth,
        RawHeight = rawHeight,
        FileSizeBytes = descriptor.FileSizeBytes,
        MeshCount = descriptor.MeshCount,
        TriangleCount = descriptor.TriangleCount,
        MaterialCount = descriptor.MaterialCount,
        TextureCount = descriptor.TextureCount,
        MissingTextureCount = descriptor.MissingTextureCount,
        HasBaseColorTexture = descriptor.HasBaseColorTexture,
        HasNormalTexture = descriptor.HasNormalTexture,
        HasMetallicRoughnessTexture = descriptor.HasMetallicRoughnessTexture,
        QualityStatus = descriptor.QualityStatus,
        QualityMessage = descriptor.QualityMessage,
        IsFavorite = descriptor.IsFavorite
        };
    }

    internal static InteriorAssetDescriptor WithAnalysis(InteriorAssetDescriptor descriptor,
        float rawWidth, float rawDepth, float rawHeight, InteriorAssetQualityReport report)
    {
        var sized = WithDimensions(descriptor, rawWidth, rawDepth, rawHeight);
        return new InteriorAssetDescriptor
        {
            Name = sized.Name,
            Category = sized.Category,
            Subcategory = sized.Subcategory,
            Source = sized.Source,
            License = sized.License,
            SourceUrl = sized.SourceUrl,
            ModelPath = sized.ModelPath,
            Width = sized.Width,
            Depth = sized.Depth,
            Height = sized.Height,
            RawWidth = sized.RawWidth,
            RawDepth = sized.RawDepth,
            RawHeight = sized.RawHeight,
            FileSizeBytes = report.FileSizeBytes,
            MeshCount = report.MeshCount,
            TriangleCount = report.TriangleCount,
            MaterialCount = report.MaterialCount,
            TextureCount = report.TextureCount,
            MissingTextureCount = report.MissingTextureCount,
            HasBaseColorTexture = report.HasBaseColorTexture,
            HasNormalTexture = report.HasNormalTexture,
            HasMetallicRoughnessTexture = report.HasMetallicRoughnessTexture,
            QualityStatus = report.QualityStatus,
            QualityMessage = report.QualityMessage,
            IsFavorite = sized.IsFavorite
        };
    }

    private static CatalogDocument ReadDocument()
    {
        if (!File.Exists(CatalogPath))
            return new CatalogDocument();
        var result = JsonSerializer.Deserialize<CatalogDocument>(File.ReadAllText(CatalogPath), Options)
                     ?? new CatalogDocument();
        result.Items ??= [];
        return result;
    }

    private static void WriteDocument(CatalogDocument document)
    {
        Directory.CreateDirectory(CatalogDirectory);
        var temporaryPath = CatalogPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, Options));
        File.Move(temporaryPath, CatalogPath, overwrite: true);
    }

    private static string StoreGlb(string sourcePath, string safeName)
    {
        var destination = Path.Combine(ModelsDirectory, $"{safeName}-{Guid.NewGuid():N}.glb");
        File.Copy(sourcePath, destination, overwrite: false);
        return destination;
    }

    private static string StoreGltfPackage(string sourcePath, string safeName)
    {
        var packageDirectory = Path.Combine(ModelsDirectory, $"{safeName}-{Guid.NewGuid():N}");
        var assetDirectory = Path.Combine(packageDirectory, "Assets");
        Directory.CreateDirectory(assetDirectory);
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(sourcePath))?.AsObject()
                       ?? throw new InvalidDataException("GLTF JSON 內容為空白。");
            var sourceDirectory = Path.GetDirectoryName(sourcePath)!;
            var copiedUris = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var assetIndex = 0;
            RewriteUris(root["buffers"] as JsonArray);
            RewriteUris(root["images"] as JsonArray);
            var destination = Path.Combine(packageDirectory, "model.gltf");
            File.WriteAllText(destination, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return destination;

            void RewriteUris(JsonArray? items)
            {
                if (items is null) return;
                foreach (var item in items.OfType<JsonObject>())
                {
                    var uri = item["uri"]?.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(uri) || uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (Uri.TryCreate(uri, UriKind.Absolute, out _))
                        throw new InvalidDataException($"GLTF 使用不支援的遠端或絕對資產 URI：{uri}");
                    var decoded = Uri.UnescapeDataString(uri).Replace('/', Path.DirectorySeparatorChar);
                    var sourceAssetPath = Path.GetFullPath(Path.Combine(sourceDirectory, decoded));
                    if (!File.Exists(sourceAssetPath))
                        throw new FileNotFoundException($"GLTF 缺少相依資產：{uri}", sourceAssetPath);
                    if (!copiedUris.TryGetValue(sourceAssetPath, out var packagedUri))
                    {
                        var assetName = MakeSafeFileName(Path.GetFileName(sourceAssetPath));
                        packagedUri = $"Assets/{assetIndex++:D3}-{assetName}";
                        File.Copy(sourceAssetPath,
                            Path.Combine(packageDirectory, packagedUri.Replace('/', Path.DirectorySeparatorChar)));
                        copiedUris[sourceAssetPath] = packagedUri;
                    }
                    item["uri"] = packagedUri;
                }
            }
        }
        catch
        {
            if (Directory.Exists(packageDirectory))
                Directory.Delete(packageDirectory, recursive: true);
            throw;
        }
    }

    private static InteriorAssetDescriptor ToDescriptor(CatalogItem item)
    {
        var rawWidth = item.RawWidth > 0f ? item.RawWidth : (float)item.Width / 100f;
        var rawDepth = item.RawDepth > 0f ? item.RawDepth : (float)item.Depth / 100f;
        var rawHeight = item.RawHeight > 0f ? item.RawHeight : (float)item.Height / 100f;
        var factor = InferUnitScale(new System.Numerics.Vector3(rawWidth, rawHeight, rawDepth));
        return new InteriorAssetDescriptor
        {
            Name = item.Name,
            Category = InteriorAssetCatalog.Categories.Contains(item.Category) ? item.Category : "使用者自訂",
            Subcategory = item.Subcategory,
            Source = "本機模型庫",
            License = item.License,
            SourceUrl = item.SourceUrl,
            ModelPath = item.ModelPath,
            Width = ToCentimeters(rawWidth, factor),
            Depth = ToCentimeters(rawDepth, factor),
            Height = ToCentimeters(rawHeight, factor),
            RawWidth = rawWidth,
            RawDepth = rawDepth,
            RawHeight = rawHeight,
            FileSizeBytes = item.FileSizeBytes,
            MeshCount = item.MeshCount,
            TriangleCount = item.TriangleCount,
            MaterialCount = item.MaterialCount,
            TextureCount = item.TextureCount,
            MissingTextureCount = item.MissingTextureCount,
            HasBaseColorTexture = item.HasBaseColorTexture,
            HasNormalTexture = item.HasNormalTexture,
            HasMetallicRoughnessTexture = item.HasMetallicRoughnessTexture,
            QualityStatus = item.QualityStatus,
            QualityMessage = item.QualityMessage
        };
    }

    private static float InferUnitScale(System.Numerics.Vector3 rawSize)
    {
        var largest = Math.Max(rawSize.X, Math.Max(rawSize.Y, rawSize.Z));
        return largest > 100f ? .001f : largest > 10f ? .01f : 1f;
    }

    private static decimal ToCentimeters(float rawValue, float unitScale) =>
        Math.Round((decimal)Math.Max(0f, rawValue) * (decimal)unitScale * 100m, 2);

    private static string MakeSafeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(value) ? "model" : value.Trim();
    }
}
