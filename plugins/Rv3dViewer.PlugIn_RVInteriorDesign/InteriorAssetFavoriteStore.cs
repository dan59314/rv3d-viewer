namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Text.Json;

internal static class InteriorAssetFavoriteStore
{
    private sealed class FavoriteDocument
    {
        public int Version { get; set; } = 1;
        public List<string> AssetKeys { get; set; } = [];
    }

    private static readonly object SyncRoot = new();
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static HashSet<string>? _favoriteKeys;
    private static string StoreDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "RVInteriorDesign", "ModelLibrary");
    private static string StorePath => Path.Combine(StoreDirectory, "favorites.json");

    internal static IReadOnlyList<InteriorAssetDescriptor> Apply(IEnumerable<InteriorAssetDescriptor> assets)
    {
        lock (SyncRoot)
        {
            var keys = GetKeys();
            return assets.Select(asset => asset.IsFavorite || keys.Contains(CreateKey(asset))
                    ? asset.WithFavorite(true)
                    : asset)
                .ToArray();
        }
    }

    internal static bool Add(InteriorAssetDescriptor asset)
    {
        lock (SyncRoot)
        {
            var keys = GetKeys();
            if (!keys.Add(CreateKey(asset)))
                return false;
            Directory.CreateDirectory(StoreDirectory);
            var document = new FavoriteDocument { AssetKeys = keys.Order(StringComparer.OrdinalIgnoreCase).ToList() };
            File.WriteAllText(StorePath, JsonSerializer.Serialize(document, Options));
            return true;
        }
    }

    private static HashSet<string> GetKeys()
    {
        if (_favoriteKeys is not null)
            return _favoriteKeys;
        try
        {
            var document = File.Exists(StorePath)
                ? JsonSerializer.Deserialize<FavoriteDocument>(File.ReadAllText(StorePath), Options)
                : null;
            _favoriteKeys = new HashSet<string>(document?.AssetKeys ?? [], StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _favoriteKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        return _favoriteKeys;
    }

    private static string CreateKey(InteriorAssetDescriptor asset)
    {
        if (!string.IsNullOrWhiteSpace(asset.ModelPath))
        {
            try
            {
                return $"file|{Path.GetFullPath(asset.ModelPath)}";
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
            {
            }
        }
        return $"catalog|{asset.Source}|{asset.Category}|{asset.Subcategory}|{asset.Name}";
    }
}
