using System.Text.Json;

namespace Rv3dViewer.Plugin.WinForms;

public sealed class PluginDisplaySettings
{
    public bool ShowCamera { get; set; } = true;
    public bool ShowLights { get; set; } = true;
    public bool ShowTextures { get; set; } = true;
    public PluginModelDisplayMode ModelMode { get; set; } = PluginModelDisplayMode.Solid;
}

public static class PluginDisplaySettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static PluginDisplaySettings Load(string pluginKey)
    {
        try
        {
            var path = GetPath(pluginKey);
            return File.Exists(path)
                ? JsonSerializer.Deserialize<PluginDisplaySettings>(File.ReadAllText(path), Options) ?? new()
                : new();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    public static void Save(string pluginKey, PluginDisplaySettings settings)
    {
        try
        {
            var path = GetPath(pluginKey);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, Options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Display preferences must never prevent a PlugIn from operating.
        }
    }

    private static string GetPath(string pluginKey) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "PluginDisplay", pluginKey + ".json");
}
