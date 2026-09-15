
/*
統一 Viewport 的滑鼠操作行為：

1. 左鍵拖曳：維持目前 Orbit 行為。
2. 中鍵拖曳：新增只移動 Camera Target。
3. 右鍵拖曳：改成目前 Pan 行為。
4. 一般滾輪：改為 FOV。
*/



using System.Diagnostics;
using Rv3dViewer.Core;
using Rv3dViewer.Plugin.Abstractions;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal sealed partial class HighQualityRenderForm : Form
{

    private readonly ViewerProject _project;
    private readonly IPluginContext? _pluginContext;
    private readonly OpenGlRenderer? _previewRenderer = null;
    private RenderSceneSnapshot? _scene;
    private bool _sceneSnapshotDirty = true;
    private CancellationTokenSource? _renderCancellation;
    private bool _rendering;
    private bool _gpuDevicesLoaded;
    private bool _applyingPipelineSettings;
    private string _qualityPresetName = "HighQuality";
    private RenderProfile _renderProfile = RenderProfile.Custom;
    private readonly HighQualityRenderUiSettings _uiSettings;

    // Diagnostic sidecar files are deliberately opt-in. They are useful while
    // tuning the tracer, but ordinary renders should only write the requested PNG.
    internal bool ExportRenderDebugImage { get; set; } = false;

    public HighQualityRenderForm(ViewerProject project)
        : this(project, HighQualityRenderUiSettings.Load())
    {
    }

    internal HighQualityRenderForm(IPluginContext context)
        : this(context?.Project ?? throw new ArgumentNullException(nameof(context)),
            HighQualityRenderUiSettings.Load())
    {
        _pluginContext = context;
        _pluginContext.ProjectChanged += PluginContext_ProjectChanged;
    }

    internal HighQualityRenderForm(ViewerProject project, HighQualityRenderUiSettings uiSettings)
    {
        InitializeComponent();
        _project = project;
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        // Upgrade the old jewelry preset; manual Denoiser edits are saved as Custom.
        if (_uiSettings.RenderProfile == nameof(RenderProfile.Jewelry) &&
            _uiSettings.DenoiserMode == RenderDenoiserMode.Disabled)
            _uiSettings.DenoiserMode = RenderDenoiserMode.BuiltIn;
        renderPipelineSettingsControl.PresetSelected += RenderPipelineSettingsControl_PresetSelected;
        renderPipelineSettingsControl.SettingsChanged += RenderPipelineSettingsControl_SettingsChanged;
        RestorePipelineSettings();

        _previewRenderer = new OpenGlRenderer(renderPreviewGlControl);
        _previewRenderer.SetProject(_project);
        _previewRenderer.SetPreviewOverlaysEnabled(false);
        InitializeUnifiedMenu();
        _previewRenderer.CameraChanged += RenderPreviewControl_CameraChanged;
        _previewRenderer.RendererError += (_, message) => statusLabel.Text = message;

        outputPathTextBox.Text = string.IsNullOrWhiteSpace(_uiSettings.LastOutputPath)
            ? CreateDefaultOutputPath(project)
            : RenderOutputFile.NextPath(Path.GetDirectoryName(Path.GetFullPath(_uiSettings.LastOutputPath))!, DateTime.Now);
    }

    public string? CompletedOutputPath { get; private set; }
    public event EventHandler<string>? RenderCompleted;

    internal void SetQuickPreviewEnabled(bool enabled)
    {
        _previewRenderer?.SetQuickPreviewEnabled(enabled);
    }

    internal void SetPreviewMode(ViewportPreviewMode mode)
    {
        _previewRenderer?.SetPreviewMode(mode);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (_rendering) return;
        _previewRenderer!.InvalidateScene();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_gpuDevicesLoaded) return;
        PopulateGpuDevices();
        _gpuDevicesLoaded = true;
    }

    private void PopulateGpuDevices()
    {
        gpuDeviceComboBox.BeginUpdate();
        try
        {
            gpuDeviceComboBox.Items.Clear();
            foreach (var device in GpuPathTracer.GetAvailableDevices())
                gpuDeviceComboBox.Items.Add(device);
            if (gpuDeviceComboBox.Items.Count == 0)
                gpuDeviceComboBox.Items.Add(GpuRenderDevice.Automatic);
            var devices = gpuDeviceComboBox.Items.Cast<GpuRenderDevice>().ToArray();
            var selectedDevice = _uiSettings.PreferredGpuDeviceExplicitlySelected
                ? devices.FirstOrDefault(device =>
                    string.Equals(device.Id, _uiSettings.PreferredGpuDeviceId, StringComparison.OrdinalIgnoreCase))
                : null;
            selectedDevice ??= devices.FirstOrDefault(device =>
                device.ExpectedRenderer.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase));
            selectedDevice ??= devices.FirstOrDefault(device => !device.IsAutomatic);
            selectedDevice ??= GpuRenderDevice.Automatic;
            gpuDeviceComboBox.SelectedItem = selectedDevice;
            var detectedCount = Math.Max(0, gpuDeviceComboBox.Items.Count - 1);
            statusLabel.Text = detectedCount == 0
                ? "未偵測到可指定的 OpenGL GPU，將自動選擇或回退 CPU"
                : $"已選擇 GPU：{selectedDevice.DisplayName}";
        }
        finally
        {
            gpuDeviceComboBox.EndUpdate();
        }
    }

    private void GpuDeviceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_gpuDevicesLoaded || gpuDeviceComboBox.SelectedItem is not GpuRenderDevice selectedDevice) return;
        _uiSettings.PreferredGpuDeviceId = selectedDevice.Id;
        _uiSettings.PreferredGpuDeviceExplicitlySelected = true;
        _uiSettings.Save();
        statusLabel.Text = $"已選擇 GPU：{selectedDevice.DisplayName}";
    }

    private RenderSceneSnapshot GetOrCreateSceneSnapshot()
    {
        if (_scene is null || _sceneSnapshotDirty)
        {
            _scene = RenderSceneSnapshot.Create(_project);
            _sceneSnapshotDirty = false;
        }
        return _scene;
    }

    private void PluginContext_ProjectChanged(object? sender, EventArgs e)
    {
        _sceneSnapshotDirty = true;
        if (!_rendering) _previewRenderer?.InvalidateScene();
    }

    private void RestorePipelineSettings()
    {
        _applyingPipelineSettings = true;
        try
        {
            samplesNumericUpDown.Value = Math.Clamp(_uiSettings.SamplesPerPixel,
                decimal.ToInt32(samplesNumericUpDown.Minimum), decimal.ToInt32(samplesNumericUpDown.Maximum));
            bouncesNumericUpDown.Value = Math.Clamp(_uiSettings.MaximumBounces,
                decimal.ToInt32(bouncesNumericUpDown.Minimum), decimal.ToInt32(bouncesNumericUpDown.Maximum));
            renderPipelineSettingsControl.RenderScalePercent = _uiSettings.RenderScalePercent;
            renderPipelineSettingsControl.ExecutionMode = _uiSettings.ExecutionMode;
            renderPipelineSettingsControl.DenoiserMode = _uiSettings.DenoiserMode;
            renderPipelineSettingsControl.AdaptiveSampling = _uiSettings.AdaptiveSampling;
            renderPipelineSettingsControl.UseHdriImportanceSampling = _uiSettings.UseHdriImportanceSampling;
            renderPipelineSettingsControl.GlassQuality = _uiSettings.GlassQuality;
            renderPipelineSettingsControl.TextureQuality = _uiSettings.TextureQuality;
            renderPipelineSettingsControl.FireflyClamp = _uiSettings.FireflyClamp;
            renderPipelineSettingsControl.ExportAov = _uiSettings.ExportAov;
            _qualityPresetName = _uiSettings.QualityPreset;
            renderPipelineSettingsControl.SetPresetSelection(
                Enum.TryParse<RenderQualityPreset>(_qualityPresetName, out var preset) ? preset : null);
            _renderProfile = Enum.TryParse<RenderProfile>(_uiSettings.RenderProfile, out var profile)
                ? profile
                : RenderProfile.Custom;
            SetRenderProfileSelection(_renderProfile);
        }
        finally { _applyingPipelineSettings = false; }
    }

    private void RenderProfileComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_applyingPipelineSettings) return;
        var profile = renderProfileComboBox.SelectedIndex switch
        {
            0 => RenderProfile.QuickDraft,
            1 => RenderProfile.Basic,
            2 => RenderProfile.ProductStudio,
            3 => RenderProfile.Metal,
            4 => RenderProfile.Glass,
            5 => RenderProfile.Jewelry,
            6 => RenderProfile.JewelryOidn,
            7 => RenderProfile.Interior,
            8 => RenderProfile.FinalQuality,
            _ => RenderProfile.Custom
        };
        if (profile == RenderProfile.Custom)
        {
            _applyingPipelineSettings = true;
            try
            {
                _renderProfile = RenderProfile.Custom;
                _qualityPresetName = "Custom";
                renderPipelineSettingsControl.SetPresetSelection(null);
            }
            finally { _applyingPipelineSettings = false; }
            SavePipelineSettings();
            return;
        }

        _applyingPipelineSettings = true;
        try
        {
            _renderProfile = profile;
            _qualityPresetName = "Custom";
            var settings = RenderProfileSettings.For(profile);
            samplesNumericUpDown.Value = settings.SamplesPerPixel;
            bouncesNumericUpDown.Value = settings.MaximumBounces;
            renderPipelineSettingsControl.RenderScalePercent = settings.RenderScalePercent;
            renderPipelineSettingsControl.DenoiserMode = settings.DenoiserMode;
            renderPipelineSettingsControl.AdaptiveSampling = settings.AdaptiveSampling;
            renderPipelineSettingsControl.UseHdriImportanceSampling = settings.UseHdriImportanceSampling;
            renderPipelineSettingsControl.GlassQuality = settings.GlassQuality;
            renderPipelineSettingsControl.TextureQuality = settings.TextureQuality;
            renderPipelineSettingsControl.FireflyClamp = settings.FireflyClamp;
            renderPipelineSettingsControl.ExportAov = settings.ExportAov;
            renderPipelineSettingsControl.SetPresetSelection(null);
            SetRenderProfileSelection(profile);
        }
        finally { _applyingPipelineSettings = false; }
        SavePipelineSettings();
        statusLabel.Text = $"已套用 Render 設定：{renderProfileComboBox.Text}";
    }

    private void SetRenderProfileSelection(RenderProfile profile)
    {
        renderProfileComboBox.SelectedIndex = profile switch
        {
            RenderProfile.QuickDraft => 0,
            RenderProfile.Basic => 1,
            RenderProfile.ProductStudio => 2,
            RenderProfile.Metal => 3,
            RenderProfile.Glass => 4,
            RenderProfile.Jewelry => 5,
            RenderProfile.JewelryOidn => 6,
            RenderProfile.Interior => 7,
            RenderProfile.FinalQuality => 8,
            _ => 9
        };
    }

    private void RenderPipelineSettingsControl_PresetSelected(object? sender, RenderQualityPreset preset)
    {
        _applyingPipelineSettings = true;
        try
        {
            _renderProfile = RenderProfile.Custom;
            SetRenderProfileSelection(_renderProfile);
            _qualityPresetName = preset.ToString();
            var settings = RenderQualityPresetSettings.For(preset);
            samplesNumericUpDown.Value = settings.SamplesPerPixel;
            bouncesNumericUpDown.Value = settings.MaximumBounces;
            renderPipelineSettingsControl.RenderScalePercent = settings.RenderScalePercent;
            renderPipelineSettingsControl.DenoiserMode = settings.DenoiserMode;
            renderPipelineSettingsControl.AdaptiveSampling = settings.AdaptiveSampling;
            renderPipelineSettingsControl.UseHdriImportanceSampling = settings.UseHdriImportanceSampling;
            renderPipelineSettingsControl.GlassQuality = settings.GlassQuality;
            renderPipelineSettingsControl.TextureQuality = settings.TextureQuality;
            renderPipelineSettingsControl.FireflyClamp = settings.FireflyClamp;
            renderPipelineSettingsControl.ExportAov = settings.ExportAov;
            renderPipelineSettingsControl.SetPresetSelection(preset);
        }
        finally { _applyingPipelineSettings = false; }
        SavePipelineSettings();
    }

    private void RenderPipelineSettingsControl_SettingsChanged(object? sender, EventArgs e) =>
        MarkPipelineSettingsCustom();

    private void BasicPipelineSetting_Changed(object? sender, EventArgs e) =>
        MarkPipelineSettingsCustom();

    private void MarkPipelineSettingsCustom()
    {
        if (_applyingPipelineSettings) return;
        _applyingPipelineSettings = true;
        try
        {
            _renderProfile = RenderProfile.Custom;
            SetRenderProfileSelection(_renderProfile);
            _qualityPresetName = "Custom";
            renderPipelineSettingsControl.SetPresetSelection(null);
        }
        finally { _applyingPipelineSettings = false; }
        SavePipelineSettings();
    }

    private void SavePipelineSettings()
    {
        _uiSettings.QualityPreset = _qualityPresetName;
        _uiSettings.RenderProfile = _renderProfile.ToString();
        _uiSettings.SamplesPerPixel = decimal.ToInt32(samplesNumericUpDown.Value);
        _uiSettings.MaximumBounces = decimal.ToInt32(bouncesNumericUpDown.Value);
        _uiSettings.RenderScalePercent = renderPipelineSettingsControl.RenderScalePercent;
        _uiSettings.ExecutionMode = renderPipelineSettingsControl.ExecutionMode;
        _uiSettings.DenoiserMode = renderPipelineSettingsControl.DenoiserMode;
        _uiSettings.AdaptiveSampling = renderPipelineSettingsControl.AdaptiveSampling;
        _uiSettings.UseHdriImportanceSampling = renderPipelineSettingsControl.UseHdriImportanceSampling;
        _uiSettings.GlassQuality = renderPipelineSettingsControl.GlassQuality;
        _uiSettings.TextureQuality = renderPipelineSettingsControl.TextureQuality;
        _uiSettings.FireflyClamp = renderPipelineSettingsControl.FireflyClamp;
        _uiSettings.ExportAov = renderPipelineSettingsControl.ExportAov;
        _uiSettings.Save();
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        var directory = Path.GetDirectoryName(outputPathTextBox.Text);
        saveFileDialog.FileName = Path.GetFileName(RenderOutputFile.NextPath(directory ?? string.Empty, DateTime.Now));
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            saveFileDialog.InitialDirectory = directory;
        if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            outputPathTextBox.Text = saveFileDialog.FileName;
    }

    private async void RenderButton_Click(object? sender, EventArgs e)
    {
        if (_rendering) return;
        var outputPath = outputPathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            MessageBox.Show(this, "請選擇輸出 PNG 路徑。", "高品質 Render", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var fullPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var outputWidth = decimal.ToInt32(widthNumericUpDown.Value);
            var outputHeight = decimal.ToInt32(heightNumericUpDown.Value);
            var renderScale = renderPipelineSettingsControl.RenderScalePercent / 100F;
            var renderWidth = Math.Max(1, (int)MathF.Round(outputWidth * renderScale));
            var renderHeight = Math.Max(1, (int)MathF.Round(outputHeight * renderScale));
            var options = new RenderOptions(
                renderWidth,
                renderHeight,
                decimal.ToInt32(samplesNumericUpDown.Value),
                decimal.ToInt32(bouncesNumericUpDown.Value),
                transparentBackgroundCheckBox.Checked,
                environmentCheckBox.Checked,
                fullPath,
                renderPipelineSettingsControl.ExecutionMode,
                renderPipelineSettingsControl.DenoiserMode,
                renderPipelineSettingsControl.AdaptiveSampling,
                renderPipelineSettingsControl.UseHdriImportanceSampling,
                renderPipelineSettingsControl.GlassQuality,
                renderPipelineSettingsControl.TextureQuality,
                renderPipelineSettingsControl.FireflyClamp,
                renderPipelineSettingsControl.ExportAov,
                Enum.TryParse<RenderQualityPreset>(_qualityPresetName, out var selectedPreset)
                    ? selectedPreset
                    : null);
            if (!ConfirmRenderWorkload(options)) return;

            // Keep the selected directory, but generate a fresh name for every render.
            using var outputFile = RenderOutputFile.Reserve(Path.GetDirectoryName(fullPath)!, DateTime.Now);
            fullPath = outputFile.Path;
            options = options with { OutputPath = fullPath };
            outputPathTextBox.Text = fullPath;

            statusLabel.Text = "正在準備 Render…";
            HideRenderedImage();
            var snapshotStopwatch = Stopwatch.StartNew();
            var scene = GetOrCreateSceneSnapshot();
            snapshotStopwatch.Stop();
            _renderCancellation = new CancellationTokenSource();
            SetRenderingState(true);
            var completed = false;
            if (options.ExecutionMode != RenderExecutionMode.Cpu)
            {
                var gpuProgress = new Progress<RenderProgress>(UpdateGpuProgress);
                var gpuResult = await GpuPathTracer.TryRenderToPngAsync(
                    scene,
                    options,
                    gpuProgress,
                    _renderCancellation.Token,
                    gpuDeviceComboBox.SelectedItem as GpuRenderDevice,
                    ExportRenderDebugImage || options.ExportAov);
                if (gpuResult.Status == GpuRenderStatus.Completed)
                {
                    completed = true;
                    statusLabel.Text = $"Render 完成（GPU：{gpuResult.DeviceName}；" +
                        $"快照 {snapshotStopwatch.Elapsed.TotalSeconds:F2}s／" +
                        $"總計 {gpuResult.TotalElapsed.TotalSeconds:F1}s）{Environment.NewLine}" +
                        (gpuResult.Timings?.ToDisplayText() ?? string.Empty);
                }
                else if (gpuResult.Status == GpuRenderStatus.Canceled)
                {
                    statusLabel.Text = "已取消 Render";
                    return;
                }
                else if (options.ExecutionMode == RenderExecutionMode.Gpu ||
                         gpuResult.Message.StartsWith("指定 GPU 為", StringComparison.Ordinal))
                {
                    statusLabel.Text = $"GPU Render 失敗：{gpuResult.Message}";
                    MessageBox.Show(this, gpuResult.Message, "GPU Render 失敗",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    statusLabel.Text = $"GPU 不可用：{gpuResult.Message}";
                    if (MessageBox.Show(this,
                            $"GPU Render 無法使用：{gpuResult.Message}{Environment.NewLine}{Environment.NewLine}要改用 CPU Render 嗎？CPU 在目前設定下可能需要較長時間。",
                            "高品質 Render", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    {
                        statusLabel.Text = "已取消 CPU Render";
                        return;
                    }
                }
            }
            if (!completed)
            {
                statusLabel.Text = options.DenoiserMode == RenderDenoiserMode.Disabled
                    ? "正在 CPU Render（Denoiser 關閉）"
                    : "正在 CPU Render（CPU 路徑不含 AOV Denoiser）";
                var progress = new Progress<RenderProgress>(UpdateProgress);
                completed = await OfflinePathTracer.RenderToPngAsync(
                    scene,
                    options,
                    progress,
                    _renderCancellation.Token);
            }
            if (!completed)
            {
                statusLabel.Text = "已取消 Render";
                return;
            }
            if (renderWidth != outputWidth || renderHeight != outputHeight)
                ResizeOutputImage(fullPath, outputWidth, outputHeight);
            CompletedOutputPath = fullPath;
            _uiSettings.LastOutputPath = fullPath;
            SavePipelineSettings();
            ShowRenderedImage(fullPath);
            viewButton.Enabled = true;
            progressBar.Value = 100;
            if (!statusLabel.Text.StartsWith("Render 完成（GPU", StringComparison.Ordinal))
                statusLabel.Text = "Render 完成（CPU）";
            RenderCompleted?.Invoke(this, fullPath);
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "已取消 Render";
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Render 失敗";
            MessageBox.Show(this, ex.Message, "高品質 Render 失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _renderCancellation?.Dispose();
            _renderCancellation = null;
            SetRenderingState(false);
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        if (_rendering)
        {
            cancelButton.Enabled = false;
            statusLabel.Text = "正在取消…";
            _renderCancellation?.Cancel();
            return;
        }
        Close();
    }

    private void ViewButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CompletedOutputPath) || !File.Exists(CompletedOutputPath))
        {
            viewButton.Enabled = false;
            statusLabel.Text = "找不到已完成的輸出圖片";
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(CompletedOutputPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "無法開啟圖片", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RenderPreviewControl_CameraChanged(object? sender, EventArgs e)
    {
        if (_rendering) return;
        if (_scene is not null && !_sceneSnapshotDirty)
        {
            var camera = _project.Camera;
            _scene = _scene.WithCamera(new RenderCamera(
                camera.From, camera.To, camera.Up, camera.FieldOfViewDegrees));
        }
        HideRenderedImage();
        statusLabel.Text = "已同步調整預覽視角";
    }

    private void RenderedImagePictureBox_MouseDown(object? sender, MouseEventArgs e) => HideRenderedImage();

    private void ShowRenderedImage(string path)
    {
        if (!File.Exists(path)) return;
        using var image = Image.FromFile(path);
        var rendered = new Bitmap(image);
        var previous = renderedImagePictureBox.Image;
        renderedImagePictureBox.Image = rendered;
        previous?.Dispose();
        renderedImagePictureBox.Visible = true;
        renderedImagePictureBox.BringToFront();

    }

    private void HideRenderedImage()
    {
        renderedImagePictureBox.Visible = false;
        renderPreviewGlControl.Focus();
    }

    private void HighQualityRenderForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_rendering) return;
        e.Cancel = true;
        _renderCancellation?.Cancel();
        statusLabel.Text = "正在取消…";
    }

    private void SetRenderingState(bool rendering)
    {
        _rendering = rendering;
        settingsTabControl.Enabled = !rendering;
        outputGroupBox.Enabled = !rendering;
        renderPreviewGlControl.Enabled = !rendering;

        renderButton.Enabled = !rendering;
        cancelButton.Enabled = true;
        cancelButton.Text = rendering ? "取消 Render" : "關閉";
        if (rendering)
        {
            progressBar.Value = 0;
            viewButton.Enabled = false;
        }
    }

    private void UpdateProgress(RenderProgress progress)
    {
        if (!_rendering || _renderCancellation?.IsCancellationRequested != false) return;
        progressBar.Value = progress.Percentage;
        statusLabel.Text = $"正在 Render：{progress.CompletedRows}/{progress.TotalRows} 列（{progress.Percentage}%）";
    }

    private void UpdateGpuProgress(RenderProgress progress)
    {
        if (!_rendering || _renderCancellation?.IsCancellationRequested != false) return;
        progressBar.Value = progress.Percentage;
        statusLabel.Text = $"正在 GPU Render：{progress.CompletedRows}/{progress.TotalRows} Samples（{progress.Percentage}%）";
    }

    private bool ConfirmRenderWorkload(RenderOptions options)
    {
        var pathEvaluations = (long)options.Width * options.Height * options.SamplesPerPixel * options.MaximumBounces;
        const long warningThreshold = 400_000_000L;
        if (pathEvaluations < warningThreshold) return true;
        return MessageBox.Show(this,
            $"目前設定約需計算 {pathEvaluations / 1_000_000D:0} 百萬次路徑彈跳。{Environment.NewLine}" +
            "透明、玻璃與 HDRI 場景可能需要很長時間。是否繼續？",
            "高品質 Render 工作量提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    private static string CreateDefaultOutputPath(ViewerProject project)
    {
        var directory = string.IsNullOrWhiteSpace(project.ProjectFilePath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            : Path.GetDirectoryName(project.ProjectFilePath)!;
        return RenderOutputFile.NextPath(directory, DateTime.Now);
    }

    private static void ResizeOutputImage(string path, int width, int height)
    {
        var temporaryPath = Path.Combine(Path.GetDirectoryName(path)!,
            $".{Path.GetFileNameWithoutExtension(path)}-{Guid.NewGuid():N}.png");
        try
        {
            using (var source = Image.FromFile(path))
            using (var resized = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using var graphics = Graphics.FromImage(resized);
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, new Rectangle(0, 0, width, height));
                resized.Save(temporaryPath, System.Drawing.Imaging.ImageFormat.Png);
            }
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_pluginContext is not null)
            _pluginContext.ProjectChanged -= PluginContext_ProjectChanged;
        if (!_rendering && !string.IsNullOrWhiteSpace(outputPathTextBox.Text))
        {
            _uiSettings.LastOutputPath = outputPathTextBox.Text.Trim();
            SavePipelineSettings();
        }
        _previewRenderer?.Dispose();
        renderedImagePictureBox.Image?.Dispose();
        renderedImagePictureBox.Image = null;
        base.OnFormClosed(e);
    }
}
