using System.Text.Json;

namespace Rv3dViewer.Plugin.WinForms;

public sealed record ViewportColorDefinition(
    string Key,
    string Category,
    string DisplayName,
    Color DefaultColor,
    string Scope = ViewportColorPreferences.SharedScope);

public static class ViewportColorPreferences
{
    public const string SharedScope = "Shared";
    public const string PluginCommonScope = "PluginCommon";
    public const string CameraAnimationScope = "CameraAnimation";
    public const string InteriorDesignScope = "InteriorDesign";
    public const string ColdPlateScope = "ColdPlate";
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "viewport-colors.json");

    public static IReadOnlyList<ViewportColorDefinition> Definitions { get; } =
    [
        new("StudioBackground", "ViewPort 基礎", "攝影棚背景", Color.FromArgb(255, 115, 115, 115)),
        new("GridMinor", "ViewPort 基礎", "次要格線", Color.FromArgb(56, 133, 145, 163)),
        new("GridMajor", "ViewPort 基礎", "主要格線", Color.FromArgb(107, 163, 176, 194)),
        new("AxisX", "座標軸", "X 軸", Color.FromArgb(255, 242, 46, 46)),
        new("AxisY", "座標軸", "Y 軸", Color.FromArgb(255, 46, 230, 56)),
        new("AxisZ", "座標軸", "Z 軸", Color.FromArgb(255, 51, 115, 255)),
        new("Selection", "選取與編輯", "選取與框選", Color.FromArgb(255, 255, 158, 20)),
        new("CameraGizmo", "相機", "相機 Gizmo", Color.FromArgb(255, 64, 230, 255)),
        new("DisabledLight", "燈光", "停用燈光", Color.FromArgb(255, 107, 107, 107)),
        new("LightHighlight", "燈光", "燈光高亮混合色", Color.White),
        new("OverlayText", "操作 Overlay", "Overlay 文字", Color.White),
        new("OverlayBackground", "操作 Overlay", "Overlay 背景", Color.FromArgb(190, 0, 0, 0)),
        new("CameraPath", "Camera Animation", "動畫路徑", Color.FromArgb(220, 135, 206, 235), CameraAnimationScope),
        new("CameraDraftPath", "Camera Animation", "草稿路徑", Color.LightGray, CameraAnimationScope),
        new("CameraControlPoint", "Camera Animation", "控制點", Color.MediumPurple, CameraAnimationScope),
        new("CameraPosition", "Camera Animation", "Camera Position", Color.OrangeRed, CameraAnimationScope),
        new("CameraTarget", "Camera Animation", "Camera Target", Color.DeepSkyBlue, CameraAnimationScope),
        new("CameraFrustum", "Camera Animation", "Camera Frustum", Color.FromArgb(210, Color.Gold), CameraAnimationScope),
        new("Interior2dBackground", "Interior Design", "2D 背景", Color.FromArgb(255, 32, 35, 40), InteriorDesignScope),
        new("Interior3dBackground", "Interior Design", "3D 背景", Color.FromArgb(255, 20, 22, 26), InteriorDesignScope),
        new("Interior2dGrid", "Interior Design", "2D 格線", Color.FromArgb(48, 180, 190, 205), InteriorDesignScope),
        new("Interior3dGrid", "Interior Design", "3D 格線", Color.FromArgb(65, 130, 160, 180), InteriorDesignScope),
        new("InteriorWallFill", "Interior Design", "牆體填色", Color.FromArgb(190, 210, 190, 150), InteriorDesignScope),
        new("InteriorWallEdge", "Interior Design", "牆體邊線", Color.FromArgb(245, 255, 235, 180), InteriorDesignScope),
        new("InteriorPendingPoint", "Interior Design", "待建立牆體起點", Color.Gold, InteriorDesignScope),
        new("ColdPlateBackground", "ColdPlate", "背景", Color.FromArgb(255, 28, 31, 36), ColdPlateScope),
        new("ColdPlateGrid", "ColdPlate", "格線", Color.FromArgb(24, 210, 220, 230), ColdPlateScope),
        new("ColdPlateEdge", "ColdPlate", "模型邊線", Color.FromArgb(60, 240, 240, 240), ColdPlateScope),
        new("PreviewHelpText", "PlugIn 共用", "預覽提示文字", Color.Gainsboro, PluginCommonScope)
    ];

    public static IReadOnlyList<ViewportColorDefinition> MainFormDefinitions =>
        Definitions.Where(definition => definition.Scope == SharedScope).ToArray();

    public static IReadOnlyList<ViewportColorDefinition> ForPlugin(string pluginName)
    {
        var pluginScope = pluginName switch
        {
            "動畫製作" => CameraAnimationScope,
            "室內配置設計" => InteriorDesignScope,
            "冷板模型" => ColdPlateScope,
            _ => string.Empty
        };
        return Definitions.Where(definition =>
            definition.Scope == SharedScope ||
            definition.Scope == PluginCommonScope ||
            definition.Scope == pluginScope).ToArray();
    }

    private static Dictionary<string, int> _colors = Load();
    public static event EventHandler? Changed;

    public static Color Get(string key)
    {
        var definition = Definitions.FirstOrDefault(item => item.Key == key);
        var fallback = definition?.DefaultColor ?? Color.White;
        return _colors.TryGetValue(key, out var argb) ? Color.FromArgb(argb) : fallback;
    }

    public static void Set(string key, Color color)
    {
        _colors[key] = color.ToArgb();
        Save();
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static void Preview(string key, Color color)
    {
        _colors[key] = color.ToArgb();
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static void Reset(string key)
    {
        _colors.Remove(key);
        Save();
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static void ResetAll()
    {
        _colors.Clear();
        Save();
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static void Reset(IEnumerable<string> keys)
    {
        foreach (var key in keys) _colors.Remove(key);
        Save();
        Changed?.Invoke(null, EventArgs.Empty);
    }

    private static Dictionary<string, int> Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new(StringComparer.Ordinal);
            var document = JsonSerializer.Deserialize<SettingsDocument>(File.ReadAllText(SettingsPath));
            return document?.Colors is null
                ? new(StringComparer.Ordinal)
                : new Dictionary<string, int>(document.Colors, StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new(StringComparer.Ordinal);
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(
                new SettingsDocument { Version = 1, Colors = _colors },
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private sealed class SettingsDocument
    {
        public int Version { get; set; } = 1;
        public Dictionary<string, int> Colors { get; set; } = new(StringComparer.Ordinal);
    }
}
