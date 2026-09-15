namespace Rv3dViewer.ColdPlatePlugin;

partial class ColdPlateBuilderForm
{
    private void InitializeUnifiedMenu()
    {
        unifiedMenuStrip.Configure("冷板模型", "設定冷板尺寸、流道與外形參數，產生冷板 3D 模型。", Path.GetDirectoryName(typeof(ColdPlateBuilderForm).Assembly.Location));
        unifiedMenuStrip.SetCapabilities(false, false, false, false, false, false, false, false);
        unifiedMenuStrip.SetFileCommands([], [
            new Rv3dViewer.Plugin.WinForms.PluginMenuCommand("建立冷板模型", CreateButton_Click, () => _createButton.Enabled)
        ]);
        unifiedMenuStrip.SetAdditionalEditCommands([
            new Rv3dViewer.Plugin.WinForms.PluginMenuCommand("重設預設值", ResetButton_Click)
        ]);
        unifiedMenuStrip.InputOverlayEnabledChanged += (_, _) =>
            _preview.InputOverlayEnabled = unifiedMenuStrip.InputOverlayEnabled;
        unifiedMenuStrip.InputActivity += (_, text) => _preview.ShowInputActivity(text);
    }
}
