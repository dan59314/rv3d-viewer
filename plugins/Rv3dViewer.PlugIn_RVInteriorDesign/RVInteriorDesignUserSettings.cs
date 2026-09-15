namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Text.Json;

internal sealed class RVInteriorDesignUserSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public int WorkspaceSplitterDistance { get; set; } = 270;
    public int DesignSplitterDistance { get; set; } = 1174;
    public int ModelLibrarySplitterDistance { get; set; } = 300;
    public int RightWorkspaceSplitterDistance { get; set; } = 410;
    public int SceneTemplateSplitterDistance { get; set; } = 520;
    public int AssetPlacementPanelHeight { get; set; } = 430;
    public int AssetPreviewPanelHeight { get; set; } = 220;
    public int OnlineAssetPreviewPanelHeight { get; set; } = 320;
    public int RightPanelSelectedIndex { get; set; }
    public int LibrarySelectedIndex { get; set; }
    public int AssetCategorySelectedIndex { get; set; }
    public int OnlineAssetProviderIndex { get; set; }
    public int WindowX { get; set; }
    public int WindowY { get; set; }
    public int WindowWidth { get; set; } = 1440;
    public int WindowHeight { get; set; } = 900;
    public bool IsMaximized { get; set; } = true;
    public bool ShowModelEdges { get; set; } = true;
    public bool ShowModelDimensions { get; set; }
    public bool UsePbrPreview { get; set; }
    public bool TransformSnapEnabled { get; set; } = true;
    public bool GridSnapEnabled { get; set; } = true;
    public bool SurfaceSnapEnabled { get; set; } = true;
    public int MoveSnapCentimeters { get; set; } = 10;
    public int RotationSnapDegrees { get; set; } = 5;
    public InteriorVisibilityMode VisibilityMode { get; set; } = InteriorVisibilityMode.AutoHideForegroundWalls;
    public int LayoutVersion { get; set; }

    internal static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "RVInteriorDesign", "ui-settings.json");

    internal static RVInteriorDesignUserSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new RVInteriorDesignUserSettings();
            return JsonSerializer.Deserialize<RVInteriorDesignUserSettings>(File.ReadAllText(SettingsPath))
                   ?? new RVInteriorDesignUserSettings();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new RVInteriorDesignUserSettings();
        }
    }

    internal void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, SerializerOptions));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // UI state persistence is best-effort and must never prevent the designer from closing.
        }
    }
}
