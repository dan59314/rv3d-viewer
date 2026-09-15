using System.Text.RegularExpressions;
using Rv3dViewer.Core;

namespace Rv3dViewer.MapPlugin;

public sealed record PbrTextureMatch(
    TextureSemantic Semantic,
    string Path,
    TextureChannel Channel,
    TextureColorSpace ColorSpace);

public static partial class PbrTextureSetDiscovery
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff"
    };

    private static readonly MapToken[] Tokens =
    [
        new([TextureSemantic.AmbientOcclusion, TextureSemantic.Roughness, TextureSemantic.Metallic],
            [TextureChannel.Red, TextureChannel.Green, TextureChannel.Blue], 50,
            ["occlusionroughnessmetallic", "ambientocclusionroughnessmetallic", "orm"]),
        new([TextureSemantic.AmbientOcclusion, TextureSemantic.Roughness, TextureSemantic.Metallic],
            [TextureChannel.Red, TextureChannel.Green, TextureChannel.Blue], 50, ["arm"]),
        One(TextureSemantic.BaseColor, TextureChannel.Rgba, 100, "basecolor", "base_color", "albedo", "diffuse"),
        One(TextureSemantic.AmbientOcclusion, TextureChannel.Red, 100, "ambientocclusion", "ambient_occlusion", "occlusion", "ao"),
        One(TextureSemantic.Roughness, TextureChannel.Red, 100, "roughness", "rough"),
        One(TextureSemantic.Metallic, TextureChannel.Red, 100, "metallic", "metalness", "metal"),
        One(TextureSemantic.Normal, TextureChannel.Rgba, 100, "normalgl", "normaldx", "normal", "nrm"),
        One(TextureSemantic.Bump, TextureChannel.Luminance, 100, "displacement", "height", "bump", "disp"),
        One(TextureSemantic.Emissive, TextureChannel.Rgba, 100, "emissive", "emission", "emit"),
        One(TextureSemantic.Opacity, TextureChannel.Red, 100, "opacity", "alpha", "transparency")
    ];

    public static IReadOnlyList<PbrTextureMatch> Discover(string selectedPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPath);
        selectedPath = Path.GetFullPath(selectedPath);
        if (!File.Exists(selectedPath) || !IsSupported(selectedPath)) return [];

        var directory = Path.GetDirectoryName(selectedPath)!;
        var selectedDescriptor = Describe(selectedPath);
        var selectedGroup = selectedDescriptor?.Group ?? NormalizeGroup(Path.GetFileNameWithoutExtension(selectedPath));
        var best = new Dictionary<TextureSemantic, Candidate>();

        foreach (var path in Directory.EnumerateFiles(directory).Where(IsSupported))
        {
            var descriptor = Describe(path);
            if (descriptor is null || !descriptor.Group.Equals(selectedGroup, StringComparison.OrdinalIgnoreCase)) continue;
            for (var index = 0; index < descriptor.Token.Semantics.Length; index++)
            {
                var semantic = descriptor.Token.Semantics[index];
                var score = descriptor.Token.Priority + (path.Equals(selectedPath, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
                if (!best.TryGetValue(semantic, out var existing) || score > existing.Score)
                    best[semantic] = new Candidate(path, descriptor.Token.Channels[index], score);
            }
        }

        return best.OrderBy(pair => pair.Key).Select(pair => new PbrTextureMatch(
            pair.Key, pair.Value.Path, pair.Value.Channel,
            pair.Key is TextureSemantic.BaseColor or TextureSemantic.Emissive
                ? TextureColorSpace.Srgb : TextureColorSpace.Linear)).ToArray();
    }

    public static bool IsSupported(string path) => SupportedExtensions.Contains(Path.GetExtension(path));

    private static Descriptor? Describe(string path)
    {
        var stem = ResolutionSuffixRegex().Replace(Path.GetFileNameWithoutExtension(path).ToLowerInvariant(), string.Empty);
        foreach (var token in Tokens)
        foreach (var alias in token.Aliases.OrderByDescending(value => value.Length))
        {
            if (!stem.EndsWith(alias, StringComparison.Ordinal)) continue;
            var prefixLength = stem.Length - alias.Length;
            if (prefixLength > 0 && char.IsLetterOrDigit(stem[prefixLength - 1]) && alias.Length <= 3) continue;
            var group = NormalizeGroup(stem[..prefixLength]);
            return new Descriptor(group, token);
        }
        return null;
    }

    private static string NormalizeGroup(string value) => value.Trim().TrimEnd('_', '-', '.', ' ').ToLowerInvariant();
    private static MapToken One(TextureSemantic semantic, TextureChannel channel, int priority, params string[] aliases) =>
        new([semantic], [channel], priority, aliases);

    [GeneratedRegex("(?:[_ .-](?:1|2|4|8|16)k)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ResolutionSuffixRegex();

    private sealed record MapToken(TextureSemantic[] Semantics, TextureChannel[] Channels, int Priority, string[] Aliases);
    private sealed record Descriptor(string Group, MapToken Token);
    private sealed record Candidate(string Path, TextureChannel Channel, int Score);
}
