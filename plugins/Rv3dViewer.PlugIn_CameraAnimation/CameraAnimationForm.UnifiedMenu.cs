using Rv3dViewer.Plugin.WinForms;

namespace Rv3dViewer.CameraAnimationPlugin;

partial class CameraAnimationForm
{
    private void InitializeUnifiedMenu()
    {
        unifiedMenuStrip.Configure("動畫製作", "編輯 Camera 與燈光關鍵影格、預覽動畫並輸出影片。", Path.GetDirectoryName(typeof(CameraAnimationForm).Assembly.Location));
        unifiedMenuStrip.SetCapabilities(true, true, true, true, true, true, false, false);
        unifiedMenuStrip.SetDisplayState(showCameraCheckBox.Checked, showAllLightsCheckBox.Checked, true, PluginModelDisplayMode.Solid);
        unifiedMenuStrip.SetFileCommands(
            [
                new PluginMenuCommand("新建動畫", NewButton_Click),
                new PluginMenuCommand("開啟動畫…", OpenButton_Click)
            ],
            [
                new PluginMenuCommand("儲存", SaveButton_Click),
                new PluginMenuCommand("另存新檔…", SaveAsButton_Click),
                new PluginMenuCommand("輸出 PNG 影格…", ExportFramesButton_Click),
                new PluginMenuCommand("輸出影片…", ExportVideoButton_Click)
            ]);
        unifiedMenuStrip.SetInputCommands([
            new PluginMenuCommand("Rv3dPrj 場景檔案…", ImportProjectScene_Click,
                () => IsolatedSceneService is not null),
            new PluginMenuCommand("GLB 模型檔案…", ImportGlbScene_Click,
                () => IsolatedSceneService is not null)
        ]);
        unifiedMenuStrip.SetAdditionalEditCommands([
            new PluginMenuCommand("新增關鍵影格", CaptureButton_Click),
            new PluginMenuCommand("插入關鍵影格", InsertKeyframeButton_Click, () => insertKeyframeButton.Enabled),
            new PluginMenuCommand("更新關鍵影格", UpdateKeyframeButton_Click, () => updateKeyframeButton.Enabled),
            new PluginMenuCommand("刪除關鍵影格", DeleteButton_Click, () => deleteButton.Enabled)
        ]);
        unifiedMenuStrip.UndoRequested += UndoButton_Click;
        unifiedMenuStrip.RedoRequested += RedoButton_Click;
        unifiedMenuStrip.DisplayChanged += (_, e) =>
        {
            showCameraCheckBox.Checked = e.ShowCamera;
            showAllLightsCheckBox.Checked = e.ShowLights;
        };
        unifiedMenuStrip.InputOverlayEnabledChanged += (_, _) =>
        {
            var enabled = unifiedMenuStrip.InputOverlayEnabled;
            previewPictureBox.InputOverlayEnabled = enabled;
            frontPictureBox.InputOverlayEnabled = enabled;
            leftPictureBox.InputOverlayEnabled = enabled;
            topPictureBox.InputOverlayEnabled = enabled;
        };
        unifiedMenuStrip.InputActivity += (_, text) =>
        {
            previewPictureBox.ShowInputActivity(text);
            frontPictureBox.ShowInputActivity(text);
            leftPictureBox.ShowInputActivity(text);
            topPictureBox.ShowInputActivity(text);
        };
    }

    private void SyncUnifiedMenuDisplayState() =>
        unifiedMenuStrip.SetDisplayState(showCameraCheckBox.Checked, showAllLightsCheckBox.Checked,
            true, PluginModelDisplayMode.Solid);

    private void RefreshUnifiedMenuCommandStates() => unifiedMenuStrip.RefreshCommandStates();
}
