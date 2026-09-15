using Rv3dViewer.Plugin.WinForms;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.MapPlugin;

partial class MapEditorForm
{
    private void InitializeUnifiedMenu()
    {
        unifiedMenuStrip.Configure("貼圖編輯器", "編輯 PBR 貼圖圖層、材質通道並輸出合成貼圖。", Path.GetDirectoryName(typeof(MapEditorForm).Assembly.Location));
        var settings = PluginDisplaySettingsStore.Load("Map");
        unifiedMenuStrip.SetCapabilities(true, true, true, true, false, true, true, true);
        unifiedMenuStrip.SetDisplayState(false, settings.ShowLights, settings.ShowTextures, settings.ModelMode);
        unifiedMenuStrip.SetFileCommands(
            [
                new PluginMenuCommand("載入 PBR 貼圖組…", LoadPbrSetButton_Click),
                new PluginMenuCommand("新增影像圖層…", AddImageButton_Click)
            ],
            [new PluginMenuCommand("輸出合成貼圖…", BakeButton_Click, () => bakeButton.Enabled)]);
        unifiedMenuStrip.SetAdditionalEditCommands([
            new PluginMenuCommand("重設圖層", ResetStackButton_Click)
        ]);
        unifiedMenuStrip.UndoRequested += UndoButton_Click;
        unifiedMenuStrip.RedoRequested += RedoButton_Click;
        InitializeInputOverlayMenu();
        ApplyDisplaySettings(settings);
        unifiedMenuStrip.DisplayChanged += (_, e) =>
        {
            var changed = new PluginDisplaySettings
            {
                ShowCamera = false,
                ShowLights = e.ShowLights,
                ShowTextures = e.ShowTextures,
                ModelMode = e.ModelMode
            };
            ApplyDisplaySettings(changed);
            PluginDisplaySettingsStore.Save("Map", changed);
        };
    }

    private void InitializeInputOverlayMenu()
    {
        ApplyInputOverlayStyle();
        unifiedMenuStrip.InputOverlayEnabledChanged += (_, _) =>
            _previewRenderer.SetInputOverlayEnabled(unifiedMenuStrip.InputOverlayEnabled);
        unifiedMenuStrip.InputActivity += (_, text) => _previewRenderer.ShowInputActivity(text);
        InputOverlayPreferences.Changed += InputOverlayPreferences_Changed;
        ViewportColorPreferences.Changed += ViewportColorPreferences_Changed;
        ApplyViewportColors();
        FormClosed += (_, _) =>
        {
            InputOverlayPreferences.Changed -= InputOverlayPreferences_Changed;
            ViewportColorPreferences.Changed -= ViewportColorPreferences_Changed;
        };
    }

    private void InputOverlayPreferences_Changed(object? sender, EventArgs e) => ApplyInputOverlayStyle();

    private void ApplyInputOverlayStyle()
    {
        var style = InputOverlayPreferences.Current;
        _previewRenderer.SetInputOverlayStyle(ViewportColorPreferences.Get("OverlayText"), style.FontSize);
    }

    private void ViewportColorPreferences_Changed(object? sender, EventArgs e)
    {
        ApplyViewportColors();
        ApplyInputOverlayStyle();
    }

    private void ApplyViewportColors() => _previewRenderer.SetEditorColors(CreateViewportEditorColors());

    private static ViewportEditorColors CreateViewportEditorColors() => new(
        ViewportColorPreferences.Get("StudioBackground"), ViewportColorPreferences.Get("GridMinor"),
        ViewportColorPreferences.Get("GridMajor"), ViewportColorPreferences.Get("AxisX"),
        ViewportColorPreferences.Get("AxisY"), ViewportColorPreferences.Get("AxisZ"),
        ViewportColorPreferences.Get("Selection"), ViewportColorPreferences.Get("CameraGizmo"),
        ViewportColorPreferences.Get("DisabledLight"), ViewportColorPreferences.Get("LightHighlight"),
        ViewportColorPreferences.Get("OverlayText"), ViewportColorPreferences.Get("OverlayBackground"));

    private void ApplyDisplaySettings(PluginDisplaySettings settings) =>
        _previewRenderer.SetPluginDisplayOptions(settings.ShowLights, settings.ShowTextures,
            ToViewportMode(settings.ModelMode));

    private static ViewportModelDisplayMode ToViewportMode(PluginModelDisplayMode mode) => mode switch
    {
        PluginModelDisplayMode.Points => ViewportModelDisplayMode.Points,
        PluginModelDisplayMode.Wireframe => ViewportModelDisplayMode.Wireframe,
        _ => ViewportModelDisplayMode.Solid
    };
}
