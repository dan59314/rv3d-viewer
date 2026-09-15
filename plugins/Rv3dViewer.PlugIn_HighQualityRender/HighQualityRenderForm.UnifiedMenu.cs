using Rv3dViewer.Plugin.WinForms;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.HighQualityRenderPlugin;

partial class HighQualityRenderForm
{
    private void InitializeUnifiedMenu()
    {
        unifiedMenuStrip.Configure("高品質 Render", "以高品質渲染管線輸出場景影像。", Path.GetDirectoryName(typeof(HighQualityRenderForm).Assembly.Location));
        var settings = PluginDisplaySettingsStore.Load("HighQualityRender");
        unifiedMenuStrip.SetCapabilities(false, true, false, false, false, true, true, true);
        unifiedMenuStrip.SetDisplayState(false, settings.ShowLights, settings.ShowTextures, settings.ModelMode);
        unifiedMenuStrip.SetFileCommands([], [
            new PluginMenuCommand("選擇輸出位置…", BrowseButton_Click),
            new PluginMenuCommand("輸出 Render", RenderButton_Click, () => renderButton.Enabled)
        ]);
        ApplyDisplaySettings(settings);
        InitializeInputOverlayMenu();
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
            PluginDisplaySettingsStore.Save("HighQualityRender", changed);
        };
    }

    private void InitializeInputOverlayMenu()
    {
        ApplyInputOverlayStyle();
        unifiedMenuStrip.InputOverlayEnabledChanged += (_, _) =>
        {
            _previewRenderer?.SetInputOverlayEnabled(unifiedMenuStrip.InputOverlayEnabled);
            oldRenderPreviewControl.InputOverlayEnabled = unifiedMenuStrip.InputOverlayEnabled;
        };
        unifiedMenuStrip.InputActivity += (_, text) =>
        {
            _previewRenderer?.ShowInputActivity(text);
            oldRenderPreviewControl.ShowInputActivity(text);
        };
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
        _previewRenderer?.SetInputOverlayStyle(ViewportColorPreferences.Get("OverlayText"), style.FontSize);
    }

    private void ViewportColorPreferences_Changed(object? sender, EventArgs e)
    {
        ApplyViewportColors();
        ApplyInputOverlayStyle();
    }

    private void ApplyViewportColors() => _previewRenderer?.SetEditorColors(CreateViewportEditorColors());

    private static ViewportEditorColors CreateViewportEditorColors() => new(
        ViewportColorPreferences.Get("StudioBackground"), ViewportColorPreferences.Get("GridMinor"),
        ViewportColorPreferences.Get("GridMajor"), ViewportColorPreferences.Get("AxisX"),
        ViewportColorPreferences.Get("AxisY"), ViewportColorPreferences.Get("AxisZ"),
        ViewportColorPreferences.Get("Selection"), ViewportColorPreferences.Get("CameraGizmo"),
        ViewportColorPreferences.Get("DisabledLight"), ViewportColorPreferences.Get("LightHighlight"),
        ViewportColorPreferences.Get("OverlayText"), ViewportColorPreferences.Get("OverlayBackground"));

    private void ApplyDisplaySettings(PluginDisplaySettings settings) =>
        _previewRenderer?.SetPluginDisplayOptions(settings.ShowLights, settings.ShowTextures,
            ToViewportMode(settings.ModelMode));

    private static ViewportModelDisplayMode ToViewportMode(PluginModelDisplayMode mode) => mode switch
    {
        PluginModelDisplayMode.Points => ViewportModelDisplayMode.Points,
        PluginModelDisplayMode.Wireframe => ViewportModelDisplayMode.Wireframe,
        _ => ViewportModelDisplayMode.Solid
    };
}
