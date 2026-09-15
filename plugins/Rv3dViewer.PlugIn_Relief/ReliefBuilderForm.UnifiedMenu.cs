using Rv3dViewer.Plugin.WinForms;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.ReliefPlugin;

partial class ReliefBuilderForm
{
    private bool _reliefDisplayMenuInitialized;
    private bool _syncingReliefDisplay;
    private bool _reliefShowLights;
    private PluginModelDisplayMode _reliefModelMode = PluginModelDisplayMode.Solid;

    private void InitializeUnifiedMenu()
    {
        unifiedMenuStrip.Configure("2.5D 浮雕模型", "從影像產生深度圖、貼圖與 2.5D 浮雕模型。", Path.GetDirectoryName(typeof(ReliefBuilderForm).Assembly.Location));
        var settings = PluginDisplaySettingsStore.Load("Relief");
        settings.ShowTextures = textureCheckBox.Checked;
        settings.ModelMode = wireframeCheckBox.Checked ? PluginModelDisplayMode.Wireframe : settings.ModelMode;
        _reliefShowLights = settings.ShowLights;
        _reliefModelMode = settings.ModelMode;
        unifiedMenuStrip.SetCapabilities(true, true, false, false, false, true, true, true);
        unifiedMenuStrip.SetDisplayState(false, settings.ShowLights, settings.ShowTextures, settings.ModelMode);
        unifiedMenuStrip.SetFileCommands(
            [
                new PluginMenuCommand("讀取影像…", OpenImageButton_Click, () => openImageButton.Enabled),
                new PluginMenuCommand("參數設定…", OpenParameterProfile_Click)
            ],
            [
                new PluginMenuCommand("儲存深度圖…", SaveDepthMapButton_Click, () => saveDepthMapButton.Enabled),
                new PluginMenuCommand("參數設定…", SaveParameterProfile_Click)
            ]);
        unifiedMenuStrip.SetAdditionalEditCommands([
            new PluginMenuCommand("重設參數", ResetButton_Click, () => resetButton.Enabled),
            new PluginMenuCommand("重新產生模型", RegenerateButton_Click, () => regenerateButton.Enabled)
        ]);
        unifiedMenuStrip.DisplayChanged += (_, e) =>
        {
            if (_syncingReliefDisplay) return;
            _syncingReliefDisplay = true;
            try
            {
                textureCheckBox.Checked = e.ShowTextures;
                wireframeCheckBox.Checked = e.ModelMode == PluginModelDisplayMode.Wireframe;
            }
            finally { _syncingReliefDisplay = false; }
            ApplyReliefDisplayOptions(e.ShowLights, e.ShowTextures, e.ModelMode, persist: true);
        };
        InitializeInputOverlayMenu();
        _reliefDisplayMenuInitialized = true;
        ApplyReliefDisplayOptions(settings.ShowLights, settings.ShowTextures, settings.ModelMode, persist: false);
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

    private void SyncReliefDisplayFromControls()
    {
        if (!_reliefDisplayMenuInitialized || _syncingReliefDisplay) return;
        var mode = wireframeCheckBox.Checked ? PluginModelDisplayMode.Wireframe
            : _reliefModelMode == PluginModelDisplayMode.Points ? PluginModelDisplayMode.Points
            : PluginModelDisplayMode.Solid;
        ApplyReliefDisplayOptions(_reliefShowLights, textureCheckBox.Checked, mode, persist: true);
    }

    private void ApplyReliefDisplayOptions(bool showLights, bool showTextures,
        PluginModelDisplayMode mode, bool persist)
    {
        _reliefShowLights = showLights;
        _reliefModelMode = mode;
        _previewRenderer.SetPluginDisplayOptions(showLights, showTextures, ToViewportMode(mode));
        unifiedMenuStrip.SetDisplayState(false, showLights, showTextures, mode);
        if (!persist) return;
        PluginDisplaySettingsStore.Save("Relief", new PluginDisplaySettings
        {
            ShowCamera = false,
            ShowLights = showLights,
            ShowTextures = showTextures,
            ModelMode = mode
        });
    }

    private static ViewportModelDisplayMode ToViewportMode(PluginModelDisplayMode mode) => mode switch
    {
        PluginModelDisplayMode.Points => ViewportModelDisplayMode.Points,
        PluginModelDisplayMode.Wireframe => ViewportModelDisplayMode.Wireframe,
        _ => ViewportModelDisplayMode.Solid
    };
}
