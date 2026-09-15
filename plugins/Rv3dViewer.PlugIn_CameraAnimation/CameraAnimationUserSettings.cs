using System.Text.Json;

namespace Rv3dViewer.CameraAnimationPlugin;

internal sealed class CameraAnimationUserSettings
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "CameraAnimation");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string? LastAnimationFilePath { get; set; }
    public bool ShowCamera { get; set; } = true;
    public bool ShowTrajectory { get; set; } = true;
    public bool ShowAllLights { get; set; } = true;
    public bool ShowModelTrajectory { get; set; } = true;

    public static async Task<CameraAnimationUserSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new CameraAnimationUserSettings();
            await using var stream = File.OpenRead(SettingsPath);
            return await JsonSerializer.DeserializeAsync<CameraAnimationUserSettings>(stream, JsonOptions)
                   ?? new CameraAnimationUserSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new CameraAnimationUserSettings();
        }
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(SettingsDirectory);
        await using var stream = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(stream, this, JsonOptions);
    }
}
