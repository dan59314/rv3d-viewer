using System.Text.Json;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal sealed class HighQualityRenderUiSettings
{
    public string LastOutputPath { get; set; } = string.Empty;
    public string PreferredGpuDeviceId { get; set; } = string.Empty;
    public bool PreferredGpuDeviceExplicitlySelected { get; set; }
    public string QualityPreset { get; set; } = "HighQuality";
    public string RenderProfile { get; set; } = "Custom";
    public int SamplesPerPixel { get; set; } = 256;
    public int MaximumBounces { get; set; } = 6;
    public int RenderScalePercent { get; set; } = 100;
    public RenderExecutionMode ExecutionMode { get; set; } = RenderExecutionMode.Automatic;
    public RenderDenoiserMode DenoiserMode { get; set; } = RenderDenoiserMode.Oidn;
    public bool AdaptiveSampling { get; set; } = true;
    public bool UseHdriImportanceSampling { get; set; } = true;
    public RenderGlassQuality GlassQuality { get; set; } = RenderGlassQuality.Physical;
    public RenderTextureQuality TextureQuality { get; set; } = RenderTextureQuality.Original;
    public float FireflyClamp { get; set; } = 12F;
    public bool ExportAov { get; set; }

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "HighQualityRender", "ui-settings.json");

    public static HighQualityRenderUiSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new HighQualityRenderUiSettings();
            return JsonSerializer.Deserialize<HighQualityRenderUiSettings>(File.ReadAllText(SettingsPath))
                ?? new HighQualityRenderUiSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new HighQualityRenderUiSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Persisting UI convenience data must never prevent a render from completing.
        }
    }
}
