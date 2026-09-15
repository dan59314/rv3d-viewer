namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Text.Json;

internal sealed class InteriorPbrPreviewSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public string EnvironmentPath { get; set; } = string.Empty;
    public int LightingPercent { get; set; } = 100;
    public int EnvironmentIntensityValue { get; set; } = 20;
    public int BackgroundIndex { get; set; }
    public bool ShowGrid { get; set; } = true;
    public bool ShowEnvironmentBackground { get; set; } = true;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "RVInteriorDesign", "pbr-preview-settings.json");

    internal static InteriorPbrPreviewSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new InteriorPbrPreviewSettings();
            return JsonSerializer.Deserialize<InteriorPbrPreviewSettings>(File.ReadAllText(SettingsPath))
                   ?? new InteriorPbrPreviewSettings();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new InteriorPbrPreviewSettings();
        }
    }

    internal void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, SerializerOptions));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Preview preferences are best effort and must not interrupt model inspection.
        }
    }
}
