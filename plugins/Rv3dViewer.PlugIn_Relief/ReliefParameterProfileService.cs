using System.Text.Json;

namespace Rv3dViewer.ReliefPlugin;

internal static class ReliefParameterProfileService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string ParamsDirectory => Path.Combine(
        Path.GetDirectoryName(typeof(ReliefBuilderForm).Assembly.Location)!, "Params");

    public static IReadOnlyList<string> FindProfiles()
    {
        if (!Directory.Exists(ParamsDirectory)) return [];
        return Directory.GetFiles(ParamsDirectory, "*.rlfPar", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static ReliefPluginSettings Load(string path)
    {
        var settings = JsonSerializer.Deserialize<ReliefPluginSettings>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("參數設定檔沒有有效內容。");
        if (settings.Version != ReliefPluginSettings.CurrentVersion)
            throw new InvalidDataException($"不支援參數設定版本 {settings.Version}。");
        return settings;
    }

    public static void Save(string path, ReliefPluginSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, Options));
        File.Move(temporaryPath, path, true);
    }
}
