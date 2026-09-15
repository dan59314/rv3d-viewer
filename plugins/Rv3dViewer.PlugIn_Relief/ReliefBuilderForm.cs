using System.Numerics;
using Rv3dViewer.Core;
using Rv3dViewer.Rendering.OpenGL;
using SkiaSharp;

namespace Rv3dViewer.ReliefPlugin;

internal sealed partial class ReliefBuilderForm : Form
{
    private readonly ViewerProject _targetProject;
    private readonly ViewerProject _previewProject;
    private readonly OpenGlRenderer _previewRenderer;
    private readonly ReliefDepthEstimator _depthEstimator;
    private readonly ReliefPortraitAnalyzer _portraitAnalyzer;
    private readonly ReliefBodyAnalyzer _bodyAnalyzer;
    private readonly SemaphoreSlim _processingGate = new(1, 1);
    private readonly SemaphoreSlim _modelGenerationGate = new(1, 1);
    private readonly string _sessionDirectory = Path.Combine(
        Path.GetTempPath(),
        "Rv3dViewer",
        "ReliefPlugin",
        Guid.NewGuid().ToString("N"));
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _processingCancellation;
    private CancellationTokenSource? _modelGenerationCancellation;
    private SKBitmap? _sourceBitmap;
    private SKBitmap? _processedBitmap;
    private ReliefDepthMap? _processedDepthMap;
    private ReliefDepthMap? _imagePipelineDepthMap;
    private ReliefDepthMap? _aiPreviewDepthMap;
    private ReliefDepthMap? _blendedPreviewDepthMap;
    private int _depthRevision;
    private ReliefDepthMap? _cachedAiDepthMap;
    private ReliefPortraitAnalysis? _portraitAnalysis;
    private Bitmap? _portraitAnalysisDisplayBitmap;
    private ReliefBodyAnalysis? _bodyAnalysis;
    private ReliefBodyAnalysis? _rawBodyAnalysis;
    private Bitmap? _bodyAnalysisDisplayBitmap;
    private string? _sourceTexturePath;
    private string? _workingTexturePath;
    private int _processingGeneration;
    private int _modelGeneration;
    private bool _suppressParameterEvents;
    private bool _selectProcessedTabAfterProcessing;
    private bool _selectModelTabAfterGeneration;
    private bool _hasFramedGeneratedModel;
    private bool _applyingPortraitLevel;
    private bool _applyingBodyLevel;
    private ReliefMeshBuildResult? _latestMeshBuild;
    private bool _closeDecisionMade;
    private string? _lastInvalidDepthImageWarningPath;

    private static string LastReliefImagePath => Path.Combine(
        Path.GetDirectoryName(typeof(ReliefBuilderForm).Assembly.Location)!,
        "lastReliefImage.png");

    internal string? WorkingTexturePath => _workingTexturePath;
    internal SceneModel? LatestModel => _latestMeshBuild?.Model;
    internal SceneModel? AcceptedModel { get; private set; }
    internal ReliefSceneAsset? AcceptedAsset { get; private set; }

    public ReliefBuilderForm(ViewerProject targetProject)
    {
        _targetProject = targetProject ?? throw new ArgumentNullException(nameof(targetProject));
        InitializeComponent();

        _previewProject = CreatePreviewProject();
        _previewRenderer = new OpenGlRenderer(previewGlControl);
        _depthEstimator = new ReliefDepthEstimator(Path.Combine(
            Path.GetDirectoryName(typeof(ReliefBuilderForm).Assembly.Location)!,
            ReliefDepthEstimator.ModelFileName));
        _portraitAnalyzer = new ReliefPortraitAnalyzer(Path.Combine(
            Path.GetDirectoryName(typeof(ReliefBuilderForm).Assembly.Location)!,
            ReliefPortraitAnalyzer.ModelFileName));
        _bodyAnalyzer = new ReliefBodyAnalyzer(Path.Combine(
            Path.GetDirectoryName(typeof(ReliefBuilderForm).Assembly.Location)!,
            ReliefBodyAnalyzer.ModelFileName));
        _previewRenderer.SetProject(_previewProject);
        _previewRenderer.SetQuickPreviewEnabled(true);
        _previewRenderer.RendererError += PreviewRenderer_RendererError;
        _previewRenderer.CameraChanged += PreviewRenderer_CameraChanged;
        ApplySavedSettings(ReliefPluginSettings.Load());
        InitializeUnifiedMenu();
        RestoreParameterProfileUiState();
        ApplyEnvironmentLight();
        UpdateDependentControlStates();
    }

    private void CloseButton_Click(object? sender, EventArgs e) => Close();

    private void ReliefBuilderForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closeDecisionMade) return;
        if (_latestMeshBuild is null || string.IsNullOrWhiteSpace(_workingTexturePath))
        {
            _closeDecisionMade = true;
            DialogResult = DialogResult.No;
            return;
        }

        var decision = MessageBox.Show(
            this,
            "是否將最新產生的 2.5D 浮雕模型加入 MainForm 場景？",
            "加入浮雕模型",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1);
        if (decision == DialogResult.Cancel)
        {
            e.Cancel = true;
            DialogResult = DialogResult.None;
            statusLabel.Text = "已取消關閉，可繼續調整浮雕模型。";
            return;
        }

        if (decision == DialogResult.No)
        {
            _closeDecisionMade = true;
            DialogResult = DialogResult.No;
            return;
        }

        try
        {
            var result = _latestMeshBuild;
            var model = result.Model;
            model.Name = CreateSceneModelName(result);
            AcceptedAsset = ReliefSceneAssetService.StageTexture(model, _workingTexturePath, _targetProject);
            AcceptedModel = model;
            _closeDecisionMade = true;
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            ReliefSceneAssetService.RollBack(AcceptedAsset);
            AcceptedAsset = null;
            AcceptedModel = null;
            ApplyTextureVisibility(_latestMeshBuild.Model);
            e.Cancel = true;
            DialogResult = DialogResult.None;
            MessageBox.Show(this, $"無法準備場景模型：{ex.Message}", "加入浮雕模型", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string CreateSceneModelName(ReliefMeshBuildResult result)
    {
        var sourceName = Path.GetFileNameWithoutExtension(sourcePathTextBox.Text);
        if (string.IsNullOrWhiteSpace(sourceName)) sourceName = "影像";
        var invalidCharacters = Path.GetInvalidFileNameChars();
        sourceName = new string(sourceName.Where(character => !invalidCharacters.Contains(character)).ToArray()).Trim();
        if (sourceName.Length > 48) sourceName = sourceName[..48];
        return $"浮雕_{sourceName}_{result.FinishedWidth:0.#}x{result.FinishedHeight:0.#}mm";
    }

    private async void ReliefBuilderForm_Shown(object? sender, EventArgs e)
    {
        // 僅在首次顯示時修復可能被 DPI／視窗配置折疊的參數區；
        // 顯示後仍允許使用者自行拖曳分隔線。
        workspaceSplitContainer.Panel1Collapsed = false;
        parameterScrollPanel.Visible = true;
        if (workspaceSplitContainer.SplitterDistance < workspaceSplitContainer.Panel1MinSize)
            workspaceSplitContainer.SplitterDistance = workspaceSplitContainer.Panel1MinSize;
        workspaceSplitContainer.Panel1.PerformLayout();

        if (File.Exists(LastReliefImagePath))
            await LoadImageAsync(LastReliefImagePath, saveAsLastImage: false, displayPath: "lastReliefImage.png");
    }

    private async void OpenImageButton_Click(object? sender, EventArgs e)
    {
        if (imageOpenFileDialog.ShowDialog(this) != DialogResult.OK) return;

        await LoadImageAsync(imageOpenFileDialog.FileName, saveAsLastImage: true);
        if (_sourceBitmap is not null &&
            string.Equals(sourcePathTextBox.Text, imageOpenFileDialog.FileName, StringComparison.OrdinalIgnoreCase))
            PromptForDepthImageFile();
    }

    private void BrowseDepthImageFileButton_Click(object? sender, EventArgs e)
        => SelectDepthImageFile();

    private void PromptForDepthImageFile()
    {
        var decision = MessageBox.Show(
            this,
            "是否開啟對應的 Depth 影像檔？",
            "Depth 影像檔",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (decision == DialogResult.Yes)
        {
            SelectDepthImageFile();
            return;
        }

        useDepthImageFileCheckBox.Checked = false;
        statusLabel.Text = "未使用外部 Depth 影像，維持原來的深度流程。";
    }

    private bool SelectDepthImageFile()
    {
        if (!string.IsNullOrWhiteSpace(depthImageFilePathTextBox.Text))
        {
            var directory = Path.GetDirectoryName(depthImageFilePathTextBox.Text);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                depthImageOpenFileDialog.InitialDirectory = directory;
        }
        if (depthImageOpenFileDialog.ShowDialog(this) != DialogResult.OK) return false;

        depthImageFilePathTextBox.Text = depthImageOpenFileDialog.FileName;
        _lastInvalidDepthImageWarningPath = null;
        _suppressParameterEvents = true;
        try
        {
            useDepthImageFileCheckBox.Checked = File.Exists(depthImageOpenFileDialog.FileName);
        }
        finally
        {
            _suppressParameterEvents = false;
        }
        UpdateDependentControlStates();
        if (_sourceBitmap is not null) ScheduleImageProcessing(immediate: true);
        return useDepthImageFileCheckBox.Checked;
    }

    private void UseDepthImageFileCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateDependentControlStates();
        if (_suppressParameterEvents) return;
        if (useDepthImageFileCheckBox.Checked && !IsExternalDepthImageAvailable(showWarning: true))
            statusLabel.Text = "Depth 影像檔無效，已套用原來的深度流程。";
        if (_sourceBitmap is not null) ScheduleImageProcessing(immediate: true);
    }

    private bool IsExternalDepthImageAvailable(bool showWarning)
    {
        if (!useDepthImageFileCheckBox.Checked) return false;
        var path = depthImageFilePathTextBox.Text.Trim();
        var valid = !string.IsNullOrWhiteSpace(path) && File.Exists(path) &&
                    !string.Equals(_lastInvalidDepthImageWarningPath, path, StringComparison.OrdinalIgnoreCase);
        if (valid)
        {
            _lastInvalidDepthImageWarningPath = null;
            return true;
        }
        if (showWarning && !string.Equals(_lastInvalidDepthImageWarningPath, path, StringComparison.OrdinalIgnoreCase))
        {
            _lastInvalidDepthImageWarningPath = path;
            MessageBox.Show(
                this,
                string.IsNullOrWhiteSpace(path)
                    ? "尚未指定 Depth 影像檔，將套用原來的深度流程。"
                    : $"找不到 Depth 影像檔：\n{path}\n\n將套用原來的深度流程。",
                "Depth 影像檔",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        return false;
    }

    private async Task LoadImageAsync(string path, bool saveAsLastImage, string? displayPath = null)
    {

        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var cancellationToken = _loadCancellation.Token;
        statusLabel.Text = $"正在載入：{Path.GetFileName(path)}";
        openImageButton.Enabled = false;

        try
        {
            var loadResult = await Task.Run(() =>
            {
                var bitmap = ReliefImageProcessor.LoadAndOrient(path, cancellationToken);
                try
                {
                    var displayBitmap = ReliefImageProcessor.CreateDisplayBitmap(bitmap);
                    var workingTexturePath = Path.Combine(
                        _sessionDirectory,
                        $"{Path.GetFileNameWithoutExtension(path)}_{Guid.NewGuid():N}.png");
                    ReliefImageProcessor.SaveLosslessPng(bitmap, workingTexturePath);
                    string? lastImageSaveError = null;
                    if (saveAsLastImage)
                    {
                        try { SaveLastReliefImage(bitmap); }
                        catch (Exception exception) { lastImageSaveError = exception.Message; }
                    }
                    return (Bitmap: bitmap, DisplayBitmap: displayBitmap, WorkingTexturePath: workingTexturePath, LastImageSaveError: lastImageSaveError);
                }
                catch
                {
                    bitmap.Dispose();
                    throw;
                }
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested || IsDisposed)
            {
                loadResult.Bitmap.Dispose();
                loadResult.DisplayBitmap.Dispose();
                return;
            }

            CancelProcessing();
            _sourceBitmap?.Dispose();
            _sourceBitmap = loadResult.Bitmap;
            _sourceTexturePath = loadResult.WorkingTexturePath;
            _workingTexturePath = loadResult.WorkingTexturePath;
            _latestMeshBuild = null;
            _processedDepthMap = null;
            saveDepthMapButton.Enabled = false;
            _cachedAiDepthMap = null;
            _portraitAnalysis = null;
            _bodyAnalysis = null;
            _rawBodyAnalysis = null;
            _portraitAnalysisDisplayBitmap?.Dispose();
            _portraitAnalysisDisplayBitmap = null;
            _bodyAnalysisDisplayBitmap?.Dispose();
            _bodyAnalysisDisplayBitmap = null;
            portraitAnalysisCheckBox.Checked = false;
            bodyAnalysisCheckBox.Checked = false;
            bodyAnalysisCheckBox.Enabled = false;
            _hasFramedGeneratedModel = false;
            textureCheckBox.Enabled = false;
            _previewProject.Models.Clear();
            _previewRenderer.SetProject(_previewProject);
            ReplacePictureBoxImage(originalImagePictureBox, loadResult.DisplayBitmap);
            sourcePathTextBox.Text = displayPath ?? path;
            originalImagePlaceholderLabel.Visible = false;
            processedImagePlaceholderLabel.Visible = true;
            processingTableLayoutPanel.Enabled = true;
            smoothingGroupBox.Enabled = true;
            symmetryCheckBox.Enabled = true;
            aiDepthCheckBox.Enabled = _depthEstimator.IsModelAvailable;
            UpdateDependentControlStates();
            resetButton.Enabled = true;
            regenerateButton.Enabled = false;
            previewTabControl.SelectedTab = originalImageTabPage;
            _selectProcessedTabAfterProcessing = true;
            _selectModelTabAfterGeneration = true;
            statusLabel.Text = $"已載入 {Path.GetFileName(path)}（{_sourceBitmap.Width} × {_sourceBitmap.Height}）";
            if (!string.IsNullOrWhiteSpace(loadResult.LastImageSaveError))
                statusLabel.Text += $"；無法保存 lastReliefImage.png：{loadResult.LastImageSaveError}";
            ScheduleImageProcessing(immediate: true);
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "已取消載入圖檔。";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "無法載入圖檔", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = $"載入失敗：{exception.Message}";
        }
        finally
        {
            if (!IsDisposed) openImageButton.Enabled = true;
        }
    }

    private static void SaveLastReliefImage(SKBitmap bitmap)
    {
        var destination = LastReliefImagePath;
        var temporaryPath = destination + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            ReliefImageProcessor.SaveLosslessPng(bitmap, temporaryPath);
            File.Move(temporaryPath, destination, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch { }
        }
    }

    private void ImageProcessingParameter_Changed(object? sender, EventArgs e)
    {
        //previewTabControl.SelectedTab = processedImageTabPage;

        if (!_suppressParameterEvents && !_applyingPortraitLevel && IsPortraitLevelParameter(sender))
            SelectPortraitLevel("自訂");
        if (!_suppressParameterEvents && !_applyingBodyLevel && IsBodyLevelParameter(sender))
            SelectBodyLevel("自訂");
        UpdateDependentControlStates();
        if (_suppressParameterEvents || _sourceBitmap is null) return;
        ScheduleImageProcessing(immediate: false);
    }

    private void ProcessingDebounceTimer_Tick(object? sender, EventArgs e)
    {
        processingDebounceTimer.Stop();
        _ = ProcessImageAsync();
    }

    private void ModelGenerationDebounceTimer_Tick(object? sender, EventArgs e)
    {
        modelGenerationDebounceTimer.Stop();
        _ = GenerateModelAsync();
    }

    private void GeometryParameter_Changed(object? sender, EventArgs e)
    {
        if (_suppressParameterEvents) return;
        var adjustment = NormalizeGeometryParameters(includeBorder: _processedBitmap is not null);
        if (!string.IsNullOrEmpty(adjustment)) statusLabel.Text = adjustment;
        if (_processedBitmap is null) return;
        if (!TryCaptureMeshSettings(out _, out var error))
        {
            CancelModelGeneration();
            regenerateButton.Enabled = false;
            statusLabel.Text = error;
            return;
        }
        regenerateButton.Enabled = true;
        ScheduleModelGeneration(immediate: false);
    }

    private void SmoothingParameter_Changed(object? sender, EventArgs e)
    {
        UpdateDependentControlStates();
        GeometryParameter_Changed(sender, e);
    }

    private void SimplificationParameter_Changed(object? sender, EventArgs e)
    {
        UpdateDependentControlStates();
        GeometryParameter_Changed(sender, e);
    }

    private void RegenerateButton_Click(object? sender, EventArgs e)
    {
        _selectModelTabAfterGeneration = true;
        ScheduleModelGeneration(immediate: true);
    }

    private void TextureCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        SyncReliefDisplayFromControls();
        if (_suppressParameterEvents || _latestMeshBuild is null) return;

        ApplyTextureVisibility(_latestMeshBuild.Model);
        _previewRenderer.SetProject(_previewProject);
        _previewRenderer.InvalidateScene();
        statusLabel.Text = textureCheckBox.Checked
            ? "模型貼圖已開啟。"
            : "模型貼圖已關閉，目前僅顯示模型材質與表面光影。";
    }

    private void WireframeCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        SyncReliefDisplayFromControls();
        if (_previewProject is null || _previewRenderer is null) return;
        _previewProject.RenderSettings.Wireframe = wireframeCheckBox.Checked;
        _previewRenderer.InvalidateScene();
        statusLabel.Text = wireframeCheckBox.Checked ? "模型線框顯示已開啟。" : "模型線框顯示已關閉。";
    }

    private void SaveDepthMapButton_Click(object? sender, EventArgs e)
    {
        if (_latestMeshBuild is null && _processedDepthMap is null)
        {
            MessageBox.Show(this, "尚未產生可保存的深度圖。", "儲存深度圖", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var sourceName = Path.GetFileNameWithoutExtension(sourcePathTextBox.Text);
        if (string.IsNullOrWhiteSpace(sourceName) || sourceName.StartsWith("lastReliefImage", StringComparison.OrdinalIgnoreCase))
            sourceName = "relief";
        depthMapSaveFileDialog.FileName = $"{sourceName}_depth.png";
        if (depthMapSaveFileDialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            using var bitmap = _latestMeshBuild is not null
                ? ReliefMeshGenerator.CreateHeightPreview(_latestMeshBuild)
                : _processedDepthMap!.ToBitmap(CancellationToken.None);
            ReliefImageProcessor.SaveLosslessPng(bitmap, depthMapSaveFileDialog.FileName);
            statusLabel.Text = $"深度圖已儲存：{depthMapSaveFileDialog.FileName}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "無法儲存深度圖", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PortraitAnalysisCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressParameterEvents) return;
        if (portraitAnalysisCheckBox.Checked)
        {
            _suppressParameterEvents = true;
            bodyAnalysisCheckBox.Checked = false;
            _suppressParameterEvents = false;
            ShowPortraitAnalysisPreview();
            statusLabel.Text = _portraitAnalysis is null
                ? "尚未產生人像分析結果。"
                : $"人像分析：偵測到 {_portraitAnalysis.Features.Count} 個五官區域；黃色點為區域中心。";
            return;
        }

        if (_latestMeshBuild is not null)
            ReplacePictureBoxImage(processedImagePictureBox, CreateProcessedPreview(_latestMeshBuild));
        else if (_processedBitmap is not null)
            ReplacePictureBoxImage(
                processedImagePictureBox,
                ReliefImageProcessor.CreateDisplayBitmap(_processedBitmap));
    }

    private void BodyAnalysisCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressParameterEvents) return;
        if (bodyAnalysisCheckBox.Checked && _bodyAnalysisDisplayBitmap is not null)
        {
            _suppressParameterEvents = true;
            portraitAnalysisCheckBox.Checked = false;
            _suppressParameterEvents = false;
            ReplacePictureBoxImage(processedImagePictureBox, new Bitmap(_bodyAnalysisDisplayBitmap));
            processedImagePlaceholderLabel.Visible = false;
            statusLabel.Text = _bodyAnalysis?.Bounds is SKRectI bounds
                ? $"全身人物分析：範圍 {bounds.Width} × {bounds.Height} 像素；綠框為主要人物。"
                : "未偵測到完整人物範圍。";
            return;
        }
        if (_latestMeshBuild is not null)
            ReplacePictureBoxImage(processedImagePictureBox, CreateProcessedPreview(_latestMeshBuild));
        else if (_processedBitmap is not null)
            ReplacePictureBoxImage(processedImagePictureBox, ReliefImageProcessor.CreateDisplayBitmap(_processedBitmap));
    }

    private void ResetButton_Click(object? sender, EventArgs e)
    {
        _suppressParameterEvents = true;
        try
        {
            grayscaleCheckBox.Checked = true;
            RestoreImageProcessingPipeline(new ReliefPluginSettings());
            aiDepthCheckBox.Checked = false;
            blendDepthCheckBox.Checked = false;
            aiDepthWeightNumericUpDown.Value = 50;
            depthCurveNumericUpDown.Value = 100;
            localDetailNumericUpDown.Value = 25;
            portraitGeometryNumericUpDown.Value = 60;
            portraitLevelComboBox.SelectedItem = "標準";
            portraitAnalysisCheckBox.Checked = false;
            bodyAnalysisCheckBox.Checked = false;
            glassesReliefNumericUpDown.Value = 35;
            hairDetailNumericUpDown.Value = 25;
            surfaceNormalDetailNumericUpDown.Value = 20;
            facialFeatureContourNumericUpDown.Value = 25;
            facialDepthContrastNumericUpDown.Value = 35;
            facialMicroDetailNumericUpDown.Value = 20;
            autoPortraitCropCheckBox.Checked = true;
            fullBodySegmentationCheckBox.Checked = false;
            bodyLevelComboBox.SelectedItem = "標準";
            fullBodyDepthNumericUpDown.Value = 45;
            bodyMaskCleanupNumericUpDown.Value = 35;
            backgroundSuppressionNumericUpDown.Value = 80;
            bustSilhouetteCheckBox.Checked = true;
            hueNumericUpDown.Value = 0;
            saturationNumericUpDown.Value = 100;
            valueNumericUpDown.Value = 100;
            invertCheckBox.Checked = false;
            reduceColorsCheckBox.Checked = false;
            colorLevelsNumericUpDown.Value = 16;
            symmetryCheckBox.Checked = false;
            symmetryAxisNumericUpDown.Value = 50;
            textureCheckBox.Checked = true;
            smoothingCheckBox.Checked = false;
            smoothingThresholdNumericUpDown.Value = 0.5M;
            smoothingStrengthNumericUpDown.Value = 50;
            smoothingIterationsNumericUpDown.Value = 2;
            simplifyModelCheckBox.Checked = false;
            simplificationTargetNumericUpDown.Value = 50;
            simplificationNormalAngleNumericUpDown.Value = 5;
            xzPlaneRadioButton.Checked = true;
            modelRotationAngleNumericUpDown.Value = 0;
            centerAlignmentRadioButton.Checked = true;
            environmentLightNumericUpDown.Value = 40;
            environmentLightTrackBar.Value = 40;
            wireframeCheckBox.Checked = false;
        }
        finally
        {
            _suppressParameterEvents = false;
        }

        UpdateDependentControlStates();
        if (_sourceBitmap is not null) ScheduleImageProcessing(immediate: true);
    }

    private void ReliefBuilderForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        try { CaptureSettings().Save(); }
        catch { /* 設定保存失敗不應阻止視窗關閉。 */ }
        try { SaveParameterProfileUiState(); }
        catch { /* UI 狀態保存失敗不應阻止視窗關閉。 */ }
        processingDebounceTimer.Stop();
        modelGenerationDebounceTimer.Stop();
        _loadCancellation?.Cancel();
        _processingCancellation?.Cancel();
        _modelGenerationCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _sourceBitmap?.Dispose();
        _processedBitmap?.Dispose();
        _processedDepthMap = null;
        _cachedAiDepthMap = null;
        _portraitAnalysis = null;
        _bodyAnalysis = null;
        _rawBodyAnalysis = null;
        _portraitAnalysisDisplayBitmap?.Dispose();
        _portraitAnalysisDisplayBitmap = null;
        _bodyAnalysisDisplayBitmap?.Dispose();
        _bodyAnalysisDisplayBitmap = null;
        ReplacePictureBoxImage(originalImagePictureBox, null);
        ReplacePictureBoxImage(processedImagePictureBox, null);
        _previewRenderer.RendererError -= PreviewRenderer_RendererError;
        _previewRenderer.CameraChanged -= PreviewRenderer_CameraChanged;
        _previewRenderer.Dispose();
        _depthEstimator.Dispose();
        _portraitAnalyzer.Dispose();
        _bodyAnalyzer.Dispose();

        try
        {
            if (Directory.Exists(_sessionDirectory)) Directory.Delete(_sessionDirectory, recursive: true);
        }
        catch
        {
            // 暫存檔清除失敗不應阻止視窗關閉。
        }
    }

    private void PreviewRenderer_RendererError(object? sender, string message) => statusLabel.Text = message;

    private void PreviewRenderer_CameraChanged(object? sender, EventArgs e) =>
        statusLabel.Text = "預覽操作：左鍵旋轉、中鍵移動目標、右鍵平移、滾輪調整 FOV";

    private void EnvironmentLightTrackBar_ValueChanged(object? sender, EventArgs e)
    {
        if (environmentLightNumericUpDown.Value != environmentLightTrackBar.Value)
            environmentLightNumericUpDown.Value = environmentLightTrackBar.Value;
        ApplyEnvironmentLight();
    }

    private void EnvironmentLightNumericUpDown_ValueChanged(object? sender, EventArgs e)
    {
        var value = decimal.ToInt32(environmentLightNumericUpDown.Value);
        if (environmentLightTrackBar.Value != value) environmentLightTrackBar.Value = value;
        ApplyEnvironmentLight();
    }

    private void ApplyEnvironmentLight()
    {
        if (_previewProject is null || _previewRenderer is null) return;
        var scale = (float)environmentLightNumericUpDown.Value / 100f;
        if (_previewProject.Lights.Count > 0) _previewProject.Lights[0].Intensity = 5f * scale;
        if (_previewProject.Lights.Count > 1) _previewProject.Lights[1].Intensity = 3.5f * scale;
        _previewRenderer.InvalidateScene();
    }

    private void PortraitLevelComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressParameterEvents || portraitLevelComboBox.SelectedItem is not string level || level == "自訂") return;
        ApplyPortraitLevel(level);
    }

    private void ApplyPortraitLevel(string level)
    {
        var values = level switch
        {
            "柔和" => (Local: 15m, Geometry: 45m, Glasses: 20m, Hair: 15m, Normal: 10m, Contour: 15m, Contrast: 20m, Micro: 10m, Background: 75m),
            "清晰" => (Local: 45m, Geometry: 78m, Glasses: 38m, Hair: 42m, Normal: 42m, Contour: 58m, Contrast: 72m, Micro: 48m, Background: 90m),
            _ => (Local: 25m, Geometry: 60m, Glasses: 35m, Hair: 25m, Normal: 20m, Contour: 25m, Contrast: 35m, Micro: 20m, Background: 80m)
        };

        _applyingPortraitLevel = true;
        _suppressParameterEvents = true;
        try
        {
            localDetailNumericUpDown.Value = values.Local;
            portraitGeometryNumericUpDown.Value = values.Geometry;
            glassesReliefNumericUpDown.Value = values.Glasses;
            hairDetailNumericUpDown.Value = values.Hair;
            surfaceNormalDetailNumericUpDown.Value = values.Normal;
            facialFeatureContourNumericUpDown.Value = values.Contour;
            facialDepthContrastNumericUpDown.Value = values.Contrast;
            facialMicroDetailNumericUpDown.Value = values.Micro;
            backgroundSuppressionNumericUpDown.Value = values.Background;
        }
        finally
        {
            _suppressParameterEvents = false;
            _applyingPortraitLevel = false;
        }
        UpdateDependentControlStates();
        if (_sourceBitmap is not null) ScheduleImageProcessing(immediate: false);
    }

    private bool IsPortraitLevelParameter(object? sender) => sender == localDetailNumericUpDown ||
        sender == portraitGeometryNumericUpDown || sender == glassesReliefNumericUpDown ||
        sender == hairDetailNumericUpDown || sender == surfaceNormalDetailNumericUpDown ||
        sender == facialFeatureContourNumericUpDown || sender == facialDepthContrastNumericUpDown ||
        sender == facialMicroDetailNumericUpDown || sender == backgroundSuppressionNumericUpDown;

    private void SelectPortraitLevel(string level)
    {
        if (portraitLevelComboBox.SelectedItem as string == level) return;
        _suppressParameterEvents = true;
        portraitLevelComboBox.SelectedItem = level;
        _suppressParameterEvents = false;
    }

    private void BodyLevelComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressParameterEvents || bodyLevelComboBox.SelectedItem is not string level || level == "自訂") return;
        var values = level switch
        {
            "柔和" => (Depth: 25m, Cleanup: 20m),
            "清晰" => (Depth: 65m, Cleanup: 55m),
            _ => (Depth: 45m, Cleanup: 35m)
        };
        _applyingBodyLevel = true;
        _suppressParameterEvents = true;
        try
        {
            fullBodyDepthNumericUpDown.Value = values.Depth;
            bodyMaskCleanupNumericUpDown.Value = values.Cleanup;
        }
        finally
        {
            _suppressParameterEvents = false;
            _applyingBodyLevel = false;
        }
        UpdateDependentControlStates();
        if (_sourceBitmap is not null) ScheduleImageProcessing(immediate: false);
    }

    private bool IsBodyLevelParameter(object? sender) =>
        sender == fullBodyDepthNumericUpDown || sender == bodyMaskCleanupNumericUpDown;

    private void SelectBodyLevel(string level)
    {
        if (bodyLevelComboBox.SelectedItem as string == level) return;
        _suppressParameterEvents = true;
        bodyLevelComboBox.SelectedItem = level;
        _suppressParameterEvents = false;
    }

    private void ScheduleImageProcessing(bool immediate)
    {
        processingDebounceTimer.Stop();
        CancelProcessing();
        CancelModelGeneration();
        if (immediate)
            _ = ProcessImageAsync();
        else
            processingDebounceTimer.Start();
    }

    private async Task ProcessImageAsync()
    {
        if (_sourceBitmap is null || IsDisposed) return;

        var cancellation = new CancellationTokenSource();
        _processingCancellation = cancellation;
        var cancellationToken = cancellation.Token;
        var generation = ++_processingGeneration;
        var sourceCopy = _sourceBitmap.Copy();
        var gateEntered = false;
        var settings = CaptureProcessingSettings();
        var externalDepthPath = depthImageFilePathTextBox.Text.Trim();
        var useExternalDepth = IsExternalDepthImageAvailable(showWarning: true);
        var useAiDepth = !useExternalDepth && aiDepthCheckBox.Checked;
        var useDepthSource = useExternalDepth || useAiDepth;
        var enabledImageOperations = CaptureEnabledImageProcessingOperations();
        var blendDepth = useDepthSource && blendDepthCheckBox.Checked && enabledImageOperations.Count > 0;
        var useEdgeDetectionBlend = blendDepth && enabledImageOperations.Contains("EdgeDetection");
        var edgeThreshold = (float)edgeThresholdNumericUpDown.Value;
        var edgeStrength = (float)edgeStrengthNumericUpDown.Value;
        var edgeSmoothing = (int)edgeSmoothingNumericUpDown.Value;
        var binaryThreshold = (float)binarizationThresholdNumericUpDown.Value;
        var binaryInvert = binarizationInvertCheckBox.Checked;
        var gaussianBlurRadius = (float)gaussianBlurRadiusNumericUpDown.Value;
        var aiDepthWeight = (float)aiDepthWeightNumericUpDown.Value / 100f;
        var aiSemanticScale = useAiDepth ? (blendDepth ? aiDepthWeight : 1f) : 0f;
        var depthCurvePercent = (float)depthCurveNumericUpDown.Value;
        var localDetailPercent = (float)localDetailNumericUpDown.Value;
        var portraitGeometryPercent = (float)portraitGeometryNumericUpDown.Value;
        var glassesReliefPercent = (float)glassesReliefNumericUpDown.Value;
        var hairDetailPercent = (float)hairDetailNumericUpDown.Value;
        var surfaceNormalDetailPercent = (float)surfaceNormalDetailNumericUpDown.Value;
        var facialFeatureContourPercent = (float)facialFeatureContourNumericUpDown.Value;
        var facialDepthContrastPercent = (float)facialDepthContrastNumericUpDown.Value;
        var facialMicroDetailPercent = (float)facialMicroDetailNumericUpDown.Value;
        var autoPortraitCrop = useAiDepth && aiSemanticScale > 0.0001f && autoPortraitCropCheckBox.Checked;
        var useFullBodySegmentation = useAiDepth && aiSemanticScale > 0.0001f && fullBodySegmentationCheckBox.Checked;
        var fullBodyDepthPercent = (float)fullBodyDepthNumericUpDown.Value;
        var bodyMaskCleanupPercent = (float)bodyMaskCleanupNumericUpDown.Value;
        var backgroundSuppressionPercent = (float)backgroundSuppressionNumericUpDown.Value;
        var createBustSilhouette = bustSilhouetteCheckBox.Checked;
        var cachedAiDepth = useAiDepth ? _cachedAiDepthMap?.Copy() : null;
        var cachedPortraitAnalysis = useAiDepth ? _portraitAnalysis?.Copy() : null;
        var cachedBodyAnalysis = useFullBodySegmentation ? _rawBodyAnalysis?.Copy() : null;
        statusLabel.Text = useExternalDepth
            ? blendDepth
                ? $"正在載入並融合 Depth 影像 {aiDepthWeight * 100f:0}%／影像處理 {(1f - aiDepthWeight) * 100f:0}%…"
                : "正在載入 Depth 影像檔…"
            : useAiDepth
            ? blendDepth
                ? $"正在本機推論並融合 AI {aiDepthWeight * 100f:0}%／{(useEdgeDetectionBlend ? "邊緣偵測" : "灰階")} {(1f - aiDepthWeight) * 100f:0}%…"
                : "正在本機推論 AI 相對深度…"
            : "正在處理影像…";

        try
        {
            await _processingGate.WaitAsync(cancellationToken);
            gateEntered = true;
            var result = await Task.Run(() =>
            {
                SKBitmap bitmap;
                ReliefDepthMap depthMap;
                ReliefDepthMap? imagePipelinePreview = null;
                ReliefDepthMap? aiPreview = null;
                ReliefDepthMap? blendedPreview = null;
                ReliefDepthMap? newAiDepthCache = null;
                ReliefPortraitAnalysis? newPortraitAnalysis = null;
                ReliefBodyAnalysis? newBodyAnalysis = null;
                ReliefBodyAnalysis? effectiveBodyAnalysis = null;
                Bitmap? portraitAnalysisDisplayBitmap = null;
                Bitmap? bodyAnalysisDisplayBitmap = null;
                string? portraitAnalysisError = null;
                string? externalDepthLoadError = null;
                string workingTexturePath = _sourceTexturePath!;
                SKRectI? appliedCropBounds = null;
                if (!useDepthSource)
                {
                    bitmap = ReliefImageProcessor.ProcessPipeline(
                        sourceCopy, settings, enabledImageOperations, edgeThreshold, edgeStrength,
                        edgeSmoothing, binaryThreshold, binaryInvert, gaussianBlurRadius, cancellationToken);
                    depthMap = ReliefDepthMap.FromBitmap(bitmap, cancellationToken);
                    imagePipelinePreview = depthMap.Copy();
                }
                else
                {
                    if (useAiDepth && cachedPortraitAnalysis is null && _portraitAnalyzer.IsModelAvailable)
                    {
                        try
                        {
                            newPortraitAnalysis = _portraitAnalyzer.Analyze(sourceCopy, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            portraitAnalysisError = exception.Message;
                        }
                    }
                    if (useAiDepth && useFullBodySegmentation && cachedBodyAnalysis is null && _bodyAnalyzer.IsModelAvailable)
                    {
                        try
                        {
                            newBodyAnalysis = _bodyAnalyzer.Analyze(
                                sourceCopy,
                                cachedPortraitAnalysis ?? newPortraitAnalysis,
                                cancellationToken);
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception exception) { portraitAnalysisError = $"全身分析：{exception.Message}"; }
                    }
                    var rawBodyAnalysis = cachedBodyAnalysis ?? newBodyAnalysis;
                    if (useFullBodySegmentation && rawBodyAnalysis is not null)
                        effectiveBodyAnalysis = ReliefBodyMaskRefiner.Refine(
                            sourceCopy,
                            rawBodyAnalysis,
                            cachedPortraitAnalysis ?? newPortraitAnalysis,
                            bodyMaskCleanupPercent,
                            cancellationToken);
                    ReliefDepthMap aiBase;
                    if (useExternalDepth)
                    {
                        try
                        {
                            using var externalBitmap = ReliefImageProcessor.LoadAndOrient(externalDepthPath, cancellationToken);
                            var externalMap = ReliefDepthMap.FromBitmap(externalBitmap, cancellationToken);
                            aiBase = ReliefDepthMap.FromRawDepth(
                                externalMap.Values,
                                externalMap.Width,
                                externalMap.Height,
                                cancellationToken);
                            if (aiBase.Width != sourceCopy.Width || aiBase.Height != sourceCopy.Height)
                                aiBase = aiBase.ResizeBicubic(sourceCopy.Width, sourceCopy.Height, cancellationToken);
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception exception)
                        {
                            externalDepthLoadError = exception.Message;
                            if (_depthEstimator.IsModelAvailable)
                            {
                                aiBase = _depthEstimator
                                    .Estimate(sourceCopy, cancellationToken)
                                    .RefineWithGuidance(sourceCopy, cancellationToken);
                                newAiDepthCache = aiBase.Copy();
                            }
                            else
                            {
                                using var fallbackBitmap = ReliefImageProcessor.ProcessPipeline(
                                    sourceCopy, settings, enabledImageOperations, edgeThreshold, edgeStrength,
                                    edgeSmoothing, binaryThreshold, binaryInvert, gaussianBlurRadius, cancellationToken);
                                aiBase = ReliefDepthMap.FromBitmap(fallbackBitmap, cancellationToken);
                            }
                        }
                    }
                    else
                    {
                        aiBase = cachedAiDepth!;
                        if (aiBase is null)
                        {
                            aiBase = _depthEstimator
                                .Estimate(sourceCopy, cancellationToken)
                                .RefineWithGuidance(sourceCopy, cancellationToken);
                            newAiDepthCache = aiBase.Copy();
                        }
                    }
                    aiPreview = aiBase.Copy();
                    ReliefDepthMap combined;
                    if (blendDepth)
                    {
                        var grayscaleSettings = settings with
                        {
                            Grayscale = true,
                            Invert = false,
                            Symmetry = false,
                            ReduceColors = false
                        };
                        using var processedBase = ReliefImageProcessor.ProcessPipeline(
                            sourceCopy, grayscaleSettings, enabledImageOperations, edgeThreshold, edgeStrength,
                            edgeSmoothing, binaryThreshold, binaryInvert, gaussianBlurRadius, cancellationToken);
                        var imageProcessingDepth = ReliefDepthMap.FromBitmap(processedBase, cancellationToken);
                        imagePipelinePreview = imageProcessingDepth.Copy();
                        combined = ReliefDepthMap.Blend(
                            imageProcessingDepth,
                            aiBase,
                            aiDepthWeight,
                            cancellationToken);
                    }
                    else
                    {
                        combined = aiBase;
                    }
                    blendedPreview = combined.Copy();
                    var activePortraitAnalysis = cachedPortraitAnalysis ?? newPortraitAnalysis;
                    var activeBodyGeometry = effectiveBodyAnalysis;
                    var bodyModeReady = useFullBodySegmentation && activeBodyGeometry is not null;
                    depthMap = combined
                        .ApplyPortraitGeometry(
                            sourceCopy,
                            activePortraitAnalysis,
                            portraitGeometryPercent * aiSemanticScale,
                            cancellationToken)
                        .ApplySurfaceNormalDetail(
                            sourceCopy,
                            activePortraitAnalysis,
                            surfaceNormalDetailPercent * aiSemanticScale,
                            cancellationToken)
                        .ApplyFacialFeatureContours(
                            activePortraitAnalysis,
                            facialFeatureContourPercent * aiSemanticScale,
                            cancellationToken)
                        .ApplyFacialDepthContrast(
                            activePortraitAnalysis,
                            facialDepthContrastPercent * aiSemanticScale,
                            cancellationToken)
                        .ApplyFacialMicroDetails(
                            activePortraitAnalysis,
                            facialMicroDetailPercent * aiSemanticScale,
                            cancellationToken)
                        .ApplyPortraitLayers(
                            sourceCopy,
                            cachedPortraitAnalysis ?? newPortraitAnalysis,
                            glassesReliefPercent * aiSemanticScale,
                            hairDetailPercent * aiSemanticScale,
                            bodyModeReady ? 0f : backgroundSuppressionPercent * aiSemanticScale,
                            createBustSilhouette && !bodyModeReady && aiSemanticScale > 0.0001f,
                            cancellationToken)
                        .ApplyBodyGeometry(
                            sourceCopy,
                            activeBodyGeometry,
                            activePortraitAnalysis,
                            bodyModeReady ? fullBodyDepthPercent * aiSemanticScale : 0f,
                            bodyModeReady ? fullBodyDepthPercent * aiSemanticScale : 0f,
                            bodyModeReady ? backgroundSuppressionPercent * aiSemanticScale : 0f,
                            cancellationToken)
                        .ApplyDepthShaping(depthCurvePercent, localDetailPercent, cancellationToken)
                        .ApplyPostProcessing(settings, cancellationToken);
                    var activeAnalysis = cachedPortraitAnalysis ?? newPortraitAnalysis;
                    var activeBodyAnalysis = effectiveBodyAnalysis;
                    var bodyCropBounds = default(SKRectI);
                    var faceCropBounds = default(SKRectI);
                    var hasBodyCrop = autoPortraitCrop && useFullBodySegmentation &&
                                      ReliefPortraitCropper.TryGetCropBounds(activeBodyAnalysis, out bodyCropBounds);
                    var hasFaceCrop = autoPortraitCrop && ReliefPortraitCropper.TryGetCropBounds(activeAnalysis, out faceCropBounds);
                    var cropBounds = hasBodyCrop ? bodyCropBounds : faceCropBounds;
                    if (hasBodyCrop || hasFaceCrop)
                    {
                        depthMap = depthMap.Crop(cropBounds, cancellationToken);
                        using var croppedTexture = ReliefPortraitCropper.CropBitmap(
                            sourceCopy,
                            cropBounds,
                            cancellationToken);
                        workingTexturePath = Path.Combine(
                            _sessionDirectory,
                            $"portrait_crop_{generation}_{Guid.NewGuid():N}.png");
                        ReliefImageProcessor.SaveLosslessPng(croppedTexture, workingTexturePath);
                        appliedCropBounds = cropBounds;
                    }
                    bitmap = depthMap.ToBitmap(cancellationToken);
                }
                var displayAnalysis = newPortraitAnalysis ?? cachedPortraitAnalysis;
                if (displayAnalysis is not null)
                {
                    try
                    {
                        using var overlay = displayAnalysis.CreateOverlay(sourceCopy, cancellationToken);
                        if (appliedCropBounds is SKRectI overlayCrop)
                        {
                            using var croppedOverlay = ReliefPortraitCropper.CropBitmap(
                                overlay,
                                overlayCrop,
                                cancellationToken);
                            portraitAnalysisDisplayBitmap = ReliefImageProcessor.CreateDisplayBitmap(croppedOverlay);
                        }
                        else
                        {
                            portraitAnalysisDisplayBitmap = ReliefImageProcessor.CreateDisplayBitmap(overlay);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        displayAnalysis = null;
                        portraitAnalysisError = exception.Message;
                    }
                }
                var displayBodyAnalysis = effectiveBodyAnalysis;
                if (displayBodyAnalysis is not null)
                {
                    try
                    {
                        using var overlay = displayBodyAnalysis.CreateOverlay(sourceCopy, cancellationToken);
                        if (appliedCropBounds is SKRectI bodyOverlayCrop)
                        {
                            using var croppedOverlay = ReliefPortraitCropper.CropBitmap(overlay, bodyOverlayCrop, cancellationToken);
                            bodyAnalysisDisplayBitmap = ReliefImageProcessor.CreateDisplayBitmap(croppedOverlay);
                        }
                        else bodyAnalysisDisplayBitmap = ReliefImageProcessor.CreateDisplayBitmap(overlay);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception exception) { portraitAnalysisError = $"全身分析預覽：{exception.Message}"; }
                }
                try
                {
                    var displayBitmap = ReliefImageProcessor.CreateDisplayBitmap(bitmap);
                    return (
                        Bitmap: bitmap,
                        DepthMap: depthMap,
                        DisplayBitmap: displayBitmap,
                        NewAiDepthCache: newAiDepthCache,
                        NewPortraitAnalysis: displayAnalysis,
                        NewBodyAnalysis: displayBodyAnalysis,
                        NewRawBodyAnalysis: newBodyAnalysis,
                        PortraitAnalysisDisplayBitmap: portraitAnalysisDisplayBitmap,
                        BodyAnalysisDisplayBitmap: bodyAnalysisDisplayBitmap,
                        ImagePipelinePreview: imagePipelinePreview,
                        AiPreview: aiPreview,
                        BlendedPreview: blendedPreview,
                        PortraitAnalysisError: portraitAnalysisError,
                        ExternalDepthLoadError: externalDepthLoadError,
                        WorkingTexturePath: workingTexturePath,
                        AppliedCropBounds: appliedCropBounds);
                }
                catch
                {
                    bitmap.Dispose();
                    portraitAnalysisDisplayBitmap?.Dispose();
                    throw;
                }
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested || generation != _processingGeneration || IsDisposed)
            {
                result.Bitmap.Dispose();
                result.DisplayBitmap.Dispose();
                result.PortraitAnalysisDisplayBitmap?.Dispose();
                result.BodyAnalysisDisplayBitmap?.Dispose();
                return;
            }

            _processedBitmap?.Dispose();
            _processedBitmap = result.Bitmap;
            var geometryAdjustment = NormalizeGeometryParameters(includeBorder: true);
            _processedDepthMap = result.DepthMap;
            _imagePipelineDepthMap = result.ImagePipelinePreview;
            _aiPreviewDepthMap = result.AiPreview;
            _blendedPreviewDepthMap = result.BlendedPreview;
            _depthRevision++;
            saveDepthMapButton.Enabled = true;
            _workingTexturePath = result.WorkingTexturePath;
            if (result.NewAiDepthCache is not null)
                _cachedAiDepthMap = result.NewAiDepthCache;
            if (result.NewPortraitAnalysis is not null)
            {
                _portraitAnalysis = result.NewPortraitAnalysis;
                _portraitAnalysisDisplayBitmap?.Dispose();
                _portraitAnalysisDisplayBitmap = result.PortraitAnalysisDisplayBitmap;
            }
            else
            {
                result.PortraitAnalysisDisplayBitmap?.Dispose();
            }
            if (result.NewBodyAnalysis is not null)
            {
                _bodyAnalysis = result.NewBodyAnalysis;
                _bodyAnalysisDisplayBitmap?.Dispose();
                _bodyAnalysisDisplayBitmap = result.BodyAnalysisDisplayBitmap;
            }
            else result.BodyAnalysisDisplayBitmap?.Dispose();
            if (result.NewRawBodyAnalysis is not null)
                _rawBodyAnalysis = result.NewRawBodyAnalysis;
            ReplacePictureBoxImage(processedImagePictureBox, result.DisplayBitmap);
            ShowSelectedDepthPreview();
            processedImagePlaceholderLabel.Visible = false;
            statusLabel.Text = BuildProcessingSummary(
                settings,
                useDepthSource,
                blendDepth,
                useEdgeDetectionBlend,
                aiDepthWeight,
                depthCurvePercent,
                localDetailPercent,
                portraitGeometryPercent,
                glassesReliefPercent,
                hairDetailPercent,
                surfaceNormalDetailPercent,
                facialFeatureContourPercent,
                facialDepthContrastPercent,
                facialMicroDetailPercent,
                result.AppliedCropBounds is not null,
                useFullBodySegmentation && result.NewBodyAnalysis is not null,
                fullBodyDepthPercent,
                bodyMaskCleanupPercent,
                backgroundSuppressionPercent,
                createBustSilhouette);
            if (!string.IsNullOrEmpty(geometryAdjustment)) statusLabel.Text += $"；{geometryAdjustment}";
            if (!string.IsNullOrWhiteSpace(result.PortraitAnalysisError))
                statusLabel.Text += $"；人像分析無法使用：{result.PortraitAnalysisError}";
            if (!string.IsNullOrWhiteSpace(result.ExternalDepthLoadError))
            {
                _lastInvalidDepthImageWarningPath = externalDepthPath;
                UpdateDependentControlStates();
                statusLabel.Text = $"Depth 影像讀取失敗，已套用原來的深度流程：{result.ExternalDepthLoadError}";
                MessageBox.Show(
                    this,
                    $"無法讀取 Depth 影像檔：\n{externalDepthPath}\n\n{result.ExternalDepthLoadError}\n\n已套用原來的深度流程。",
                    "Depth 影像檔",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            UpdateDependentControlStates();
            if (bodyAnalysisCheckBox.Checked && _bodyAnalysisDisplayBitmap is not null)
                ReplacePictureBoxImage(processedImagePictureBox, new Bitmap(_bodyAnalysisDisplayBitmap));
            else if (portraitAnalysisCheckBox.Checked) ShowPortraitAnalysisPreview();
            regenerateButton.Enabled = TryCaptureMeshSettings(out _, out _);
            ScheduleModelGeneration(immediate: true);
            if (_selectProcessedTabAfterProcessing)
            {
                previewTabControl.SelectedTab = processedImageTabPage;
                _selectProcessedTabAfterProcessing = false;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (cancellationToken.IsCancellationRequested || IsDisposed || generation != _processingGeneration)
                return;

            if (useAiDepth)
            {
                _suppressParameterEvents = true;
                try
                {
                    aiDepthCheckBox.Checked = false;
                    blendDepthCheckBox.Checked = false;
                }
                finally
                {
                    _suppressParameterEvents = false;
                }
                UpdateDependentControlStates();
                MessageBox.Show(
                    this,
                    $"AI 相對深度推論失敗，已切回一般灰階模式。\r\n\r\n{exception.Message}",
                    "AI 深度無法使用",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ScheduleImageProcessing(immediate: true);
                return;
            }

            statusLabel.Text = $"影像處理失敗：{exception.Message}";
        }
        finally
        {
            sourceCopy.Dispose();
            if (gateEntered) _processingGate.Release();
            if (ReferenceEquals(_processingCancellation, cancellation))
                _processingCancellation = null;
            cancellation.Dispose();
        }
    }

    private ReliefImageProcessingSettings CaptureProcessingSettings() => new(
        (float)hueNumericUpDown.Value,
        (float)saturationNumericUpDown.Value,
        (float)valueNumericUpDown.Value,
        grayscaleCheckBox.Checked,
        invertCheckBox.Checked,
        symmetryCheckBox.Checked,
        (float)symmetryAxisNumericUpDown.Value,
        reduceColorsCheckBox.Checked,
        (int)colorLevelsNumericUpDown.Value);

    private void CancelProcessing()
    {
        _processingGeneration++;
        _processingCancellation?.Cancel();
        _processingCancellation = null;
    }

    private void ScheduleModelGeneration(bool immediate)
    {
        modelGenerationDebounceTimer.Stop();
        CancelModelGeneration();
        if (_processedBitmap is null) return;
        if (immediate)
            _ = GenerateModelAsync();
        else
            modelGenerationDebounceTimer.Start();
    }

    private async Task GenerateModelAsync()
    {
        if (_processedBitmap is null || _processedDepthMap is null || IsDisposed) return;
        if (!TryCaptureMeshSettings(out var settings, out var error))
        {
            regenerateButton.Enabled = false;
            statusLabel.Text = error;
            return;
        }

        var cancellation = new CancellationTokenSource();
        _modelGenerationCancellation = cancellation;
        var cancellationToken = cancellation.Token;
        var generation = ++_modelGeneration;
        var depthCopy = _processedDepthMap.Copy();
        var depthRevision = _depthRevision;
        var gateEntered = false;
        regenerateButton.Enabled = false;
        statusLabel.Text = $"正在產生 {settings.LongestSidePoints} 點封閉浮雕網格…";

        try
        {
            await _modelGenerationGate.WaitAsync(cancellationToken);
            gateEntered = true;
            var result = await Task.Run(() =>
                ReliefMeshGenerator.Generate(depthCopy, settings, cancellationToken), cancellationToken);

            if (cancellationToken.IsCancellationRequested || generation != _modelGeneration || IsDisposed)
                return;

            _latestMeshBuild = result;
            ApplyTextureVisibility(result.Model);
            textureCheckBox.Enabled = true;
            if (!portraitAnalysisCheckBox.Checked)
                ShowSelectedDepthPreview();
            processedImagePlaceholderLabel.Visible = false;
            _previewProject.Models = [result.Model];
            _previewRenderer.SetProject(_previewProject);
            if (!_hasFramedGeneratedModel)
            {
                _previewRenderer.FrameModel(result.Model);
                _hasFramedGeneratedModel = true;
            }
            _previewRenderer.InvalidateScene();
            regenerateButton.Enabled = true;
            statusLabel.Text =
                $"浮雕完成（深度版本 #{depthRevision}）：{result.FinishedWidth:0.0} × {result.FinishedHeight:0.0} mm，" +
                $"高度 {result.MinimumTopHeight:0.###}–{result.MaximumTopHeight:0.###} mm，" +
                $"平面 {settings.ModelPlane}，{result.Columns} × {result.Rows} 點／" +
                $"{result.TriangleCount:N0} 三角形，封閉邊 {result.OpenGeometricEdgeCount}" +
                (result.SimplificationApplied
                    ? $"；已由 {result.OriginalTriangleCount:N0} 面簡化至 {result.TriangleCount:N0} 面"
                    : settings.SimplificationEnabled ? "；簡化未通過封閉性／品質檢查，已保留原網格" : string.Empty) +
                (result.SmoothingApplied
                    ? $"；平滑高度場預覽：門檻 {settings.SmoothingThreshold:0.##} mm／" +
                      $"強度 {settings.SmoothingStrength * 100f:0}%／{settings.SmoothingIterations} 次"
                    : string.Empty);
            if (_selectModelTabAfterGeneration)
            {
                previewTabControl.SelectedTab = modelPreviewTabPage;
                _selectModelTabAfterGeneration = false;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (!cancellationToken.IsCancellationRequested && !IsDisposed && generation == _modelGeneration)
            {
                regenerateButton.Enabled = true;
                statusLabel.Text = $"浮雕模型產生失敗：{exception.Message}";
            }
        }
        finally
        {
            if (gateEntered) _modelGenerationGate.Release();
            if (ReferenceEquals(_modelGenerationCancellation, cancellation))
                _modelGenerationCancellation = null;
            cancellation.Dispose();
        }
    }

    private bool TryCaptureMeshSettings(out ReliefMeshBuildSettings settings, out string error)
    {
        settings = default;
        if (_processedBitmap is null || _processedDepthMap is null || string.IsNullOrWhiteSpace(_workingTexturePath))
        {
            error = "請先載入並處理影像。";
            return false;
        }

        var width = (float)widthNumericUpDown.Value;
        var thickness = (float)thicknessNumericUpDown.Value;
        var border = (float)borderWidthNumericUpDown.Value;
        var reliefHeight = (float)reliefHeightNumericUpDown.Value;
        var finishedHeight = width * _processedBitmap.Height / _processedBitmap.Width;
        if (reliefHeight > thickness)
        {
            error = "浮雕高度不得超過成品厚度。";
            return false;
        }
        if (border * 2f >= Math.Min(width, finishedHeight))
        {
            error = "邊框寬度必須小於成品寬、高較短邊的一半。";
            return false;
        }

        var longestSidePoints = qualityComboBox.SelectedIndex switch
        {
            0 => 128,
            2 => 512,
            _ => 256
        };
        settings = new ReliefMeshBuildSettings(
            width,
            thickness,
            border,
            reliefHeight,
            longestSidePoints,
            _workingTexturePath,
            smoothingCheckBox.Checked,
            (float)smoothingThresholdNumericUpDown.Value,
            (float)smoothingStrengthNumericUpDown.Value / 100f,
            (int)smoothingIterationsNumericUpDown.Value,
            simplifyModelCheckBox.Checked,
            (float)simplificationTargetNumericUpDown.Value / 100f,
            (float)simplificationNormalAngleNumericUpDown.Value,
            xyPlaneRadioButton.Checked ? ReliefModelPlane.XY :
                yzPlaneRadioButton.Checked ? ReliefModelPlane.YZ : ReliefModelPlane.XZ,
            (float)modelRotationAngleNumericUpDown.Value,
            leftBottomAlignmentRadioButton.Checked
                ? ReliefModelAlignment.LeftBottom
                : ReliefModelAlignment.Center);
        error = string.Empty;
        return true;
    }

    private string? NormalizeGeometryParameters(bool includeBorder)
    {
        var messages = new List<string>();
        _suppressParameterEvents = true;
        try
        {
            var legalReliefMaximum = Math.Max(reliefHeightNumericUpDown.Minimum, thicknessNumericUpDown.Value);
            var previousReliefHeight = reliefHeightNumericUpDown.Value;
            reliefHeightNumericUpDown.Maximum = legalReliefMaximum;
            if (previousReliefHeight > legalReliefMaximum)
            {
                messages.Add($"浮雕高度已依模型厚度調整為 {legalReliefMaximum:0.##} mm");
            }

            if (includeBorder && _processedBitmap is not null)
            {
                var finishedHeight = widthNumericUpDown.Value * _processedBitmap.Height / _processedBitmap.Width;
                var shorterSide = Math.Min(widthNumericUpDown.Value, finishedHeight);
                var legalBorderMaximum = Math.Max(
                    borderWidthNumericUpDown.Minimum,
                    Math.Floor((shorterSide / 2m - 0.1m) * 10m) / 10m);
                var previousBorderWidth = borderWidthNumericUpDown.Value;
                borderWidthNumericUpDown.Maximum = legalBorderMaximum;
                if (previousBorderWidth > legalBorderMaximum)
                {
                    messages.Add($"邊框寬度已依成品比例調整為 {legalBorderMaximum:0.##} mm");
                }
            }
            else
            {
                borderWidthNumericUpDown.Maximum = 1000m;
            }
        }
        finally
        {
            _suppressParameterEvents = false;
        }
        return messages.Count == 0 ? null : string.Join("；", messages);
    }

    private void CancelModelGeneration()
    {
        _modelGeneration++;
        _modelGenerationCancellation?.Cancel();
        _modelGenerationCancellation = null;
    }

    private void UpdateDependentControlStates()
    {
        var useExternalDepth = IsExternalDepthImageAvailable(showWarning: false);
        var useAiDepth = !useExternalDepth && aiDepthCheckBox.Checked;
        var useDepthSource = useExternalDepth || useAiDepth;
        var blendDepth = useDepthSource && blendDepthCheckBox.Checked;
        depthImageFileFlowLayoutPanel.Enabled = useDepthImageFileCheckBox.Checked;
        aiDepthCheckBox.Enabled = !useExternalDepth && _sourceBitmap is not null && _depthEstimator.IsModelAvailable;
        blendDepthCheckBox.Enabled = useDepthSource;
        blendDepthCheckBox.Enabled &= CaptureEnabledImageProcessingOperations().Count > 0;
        aiDepthWeightLabel.Enabled = blendDepth;
        aiDepthWeightLabel.Text = useExternalDepth ? "Depth 影像比例 (%)" : "AI 深度比例 (%)";
        aiDepthWeightNumericUpDown.Enabled = blendDepth;
        depthCurveLabel.Enabled = useDepthSource;
        depthCurveNumericUpDown.Enabled = useDepthSource;
        localDetailLabel.Enabled = useDepthSource;
        localDetailNumericUpDown.Enabled = useDepthSource;
        portraitGeometryLabel.Enabled = useAiDepth;
        portraitGeometryNumericUpDown.Enabled = useAiDepth;
        portraitAnalysisCheckBox.Enabled = useAiDepth && _portraitAnalysis is not null;
        portraitLayersGroupBox.Enabled = useAiDepth && _portraitAnalysis is not null;
        fullBodySegmentationCheckBox.Enabled = useAiDepth && _bodyAnalyzer.IsModelAvailable;
        bodyLevelLabel.Enabled = useAiDepth && fullBodySegmentationCheckBox.Checked;
        bodyLevelComboBox.Enabled = bodyLevelLabel.Enabled;
        fullBodyDepthLabel.Enabled = useAiDepth && fullBodySegmentationCheckBox.Checked;
        fullBodyDepthNumericUpDown.Enabled = fullBodyDepthLabel.Enabled;
        bodyAnalysisCheckBox.Enabled = useAiDepth && fullBodySegmentationCheckBox.Checked && _bodyAnalysis is not null;
        bodyMaskCleanupLabel.Enabled = useAiDepth && fullBodySegmentationCheckBox.Checked;
        bodyMaskCleanupNumericUpDown.Enabled = bodyMaskCleanupLabel.Enabled;
        if (!bodyAnalysisCheckBox.Enabled && bodyAnalysisCheckBox.Checked)
        {
            _suppressParameterEvents = true;
            bodyAnalysisCheckBox.Checked = false;
            _suppressParameterEvents = false;
        }
        var generalImageControlsEnabled = (!useDepthSource || blendDepth) && processingTableLayoutPanel.Enabled;
        grayscaleCheckBox.Enabled = !useDepthSource && processingTableLayoutPanel.Enabled;
        hueLabel.Enabled = generalImageControlsEnabled;
        hueNumericUpDown.Enabled = hueLabel.Enabled;
        saturationLabel.Enabled = generalImageControlsEnabled;
        saturationNumericUpDown.Enabled = saturationLabel.Enabled;
        valueLabel.Enabled = generalImageControlsEnabled;
        valueNumericUpDown.Enabled = valueLabel.Enabled;
        colorLevelsLabel.Enabled = reduceColorsCheckBox.Checked && processingTableLayoutPanel.Enabled;
        colorLevelsNumericUpDown.Enabled = colorLevelsLabel.Enabled;
        symmetryAxisLabel.Enabled = symmetryCheckBox.Checked;
        symmetryAxisNumericUpDown.Enabled = symmetryCheckBox.Checked;
        var smoothingParametersEnabled = smoothingGroupBox.Enabled && smoothingCheckBox.Checked;
        smoothingThresholdLabel.Enabled = smoothingParametersEnabled;
        smoothingThresholdNumericUpDown.Enabled = smoothingParametersEnabled;
        smoothingStrengthLabel.Enabled = smoothingParametersEnabled;
        smoothingStrengthNumericUpDown.Enabled = smoothingParametersEnabled;
        simplificationTargetLabel.Enabled = simplifyModelCheckBox.Checked;
        simplificationTargetNumericUpDown.Enabled = simplifyModelCheckBox.Checked;
        simplificationNormalAngleLabel.Enabled = simplifyModelCheckBox.Checked;
        simplificationNormalAngleNumericUpDown.Enabled = simplifyModelCheckBox.Checked;
        smoothingIterationsLabel.Enabled = smoothingParametersEnabled;
        smoothingIterationsNumericUpDown.Enabled = smoothingParametersEnabled;
        depthNoticeLabel.Text = useExternalDepth
            ? blendDepth
                ? $"融合結果：Depth 影像 {aiDepthWeightNumericUpDown.Value:0}%＋影像處理 {100 - aiDepthWeightNumericUpDown.Value:0}%；白近黑遠。"
                : "使用外部 Depth 影像；白色較近、黑色較遠。"
            : useDepthImageFileCheckBox.Checked
                ? "Depth 影像檔無效，已套用原來的深度流程。"
                : _depthEstimator.IsModelAvailable
            ? useAiDepth
                ? blendDepth
                    ? $"融合結果：AI {aiDepthWeightNumericUpDown.Value:0}%＋影像處理 {100 - aiDepthWeightNumericUpDown.Value:0}%；白近黑遠，不上傳圖檔。"
                    : "白色較近、黑色較遠；相對深度不是毫米級量測，推論不上傳圖檔。"
                : "AI 輸出為相對深度，不是毫米級量測；推論不上傳圖檔。"
            : "找不到離線 AI 模型；一般灰階模式仍可使用。";
    }

    private void ApplyTextureVisibility(SceneModel model)
    {
        if (model.Materials.Count == 0) return;

        var frontMaterial = model.Materials[0];
        frontMaterial.Validate();
        var showTexture = textureCheckBox.Checked;
        frontMaterial.BaseColor = showTexture
            ? Vector4.One
            : new Vector4(0.72f, 0.74f, 0.78f, 1f);
        if (frontMaterial.Textures.TryGetValue(TextureSemantic.BaseColor, out var texture))
            texture.Enabled = showTexture;
        if (frontMaterial.TextureStacks.TryGetValue(TextureSemantic.BaseColor, out var textureStack))
            textureStack.Enabled = showTexture;
    }

    private Bitmap CreateProcessedPreview(ReliefMeshBuildResult result)
    {
        if (!result.SmoothingApplied)
            return ReliefImageProcessor.CreateDisplayBitmap(_processedBitmap!);

        using var heightPreview = ReliefMeshGenerator.CreateHeightPreview(result);
        return ReliefImageProcessor.CreateDisplayBitmap(heightPreview);
    }

    private void DepthPreviewModeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_suppressParameterEvents) ShowSelectedDepthPreview();
    }

    private void ShowSelectedDepthPreview()
    {
        ReliefDepthMap? selected = depthPreviewModeComboBox.SelectedIndex switch
        {
            0 => _imagePipelineDepthMap,
            1 => _aiPreviewDepthMap,
            2 => _blendedPreviewDepthMap,
            _ => _processedDepthMap
        };
        if (selected is null)
        {
            processedImagePlaceholderLabel.Text = "目前模式沒有可用的深度資料";
            processedImagePlaceholderLabel.Visible = true;
            return;
        }
        using var bitmap = selected.ToBitmap(CancellationToken.None);
        ReplacePictureBoxImage(processedImagePictureBox, ReliefImageProcessor.CreateDisplayBitmap(bitmap));
        processedImagePlaceholderLabel.Visible = false;
    }

    private void ShowPortraitAnalysisPreview()
    {
        if (_portraitAnalysisDisplayBitmap is null) return;
        ReplacePictureBoxImage(processedImagePictureBox, new Bitmap(_portraitAnalysisDisplayBitmap));
        processedImagePlaceholderLabel.Visible = false;
    }

    private static void ReplacePictureBoxImage(PictureBox pictureBox, Image? replacement)
    {
        var previous = pictureBox.Image;
        pictureBox.Image = replacement;
        previous?.Dispose();
    }

    private static string BuildProcessingSummary(
        ReliefImageProcessingSettings settings,
        bool useAiDepth,
        bool blendDepth,
        bool useEdgeDetectionBlend,
        float aiDepthWeight,
        float depthCurvePercent,
        float localDetailPercent,
        float portraitGeometryPercent,
        float glassesReliefPercent,
        float hairDetailPercent,
        float surfaceNormalDetailPercent,
        float facialFeatureContourPercent,
        float facialDepthContrastPercent,
        float facialMicroDetailPercent,
        bool portraitCropApplied,
        bool fullBodySegmentationApplied,
        float fullBodyDepthPercent,
        float bodyMaskCleanupPercent,
        float backgroundSuppressionPercent,
        bool createBustSilhouette)
    {
        var modes = new List<string>();
        if (blendDepth)
            modes.Add($"AI／影像處理融合（AI {aiDepthWeight * 100f:0}%＋{(useEdgeDetectionBlend ? "邊緣偵測" : "灰階")} {(1f - aiDepthWeight) * 100f:0}%）");
        else if (useAiDepth)
            modes.Add("AI 相對深度（白近黑遠）");
        if (useAiDepth && depthCurvePercent != 100f) modes.Add($"深度曲線 {depthCurvePercent:0}%");
        if (useAiDepth && localDetailPercent > 0f) modes.Add($"局部細節 {localDetailPercent:0}%");
        if (useAiDepth && portraitGeometryPercent > 0f) modes.Add($"3D 臉型融合 {portraitGeometryPercent:0}%");
        if (useAiDepth && glassesReliefPercent > 0f) modes.Add($"眼鏡凸起 {glassesReliefPercent:0}%");
        if (useAiDepth && hairDetailPercent > 0f) modes.Add($"頭髮細節 {hairDetailPercent:0}%");
        if (useAiDepth && surfaceNormalDetailPercent > 0f) modes.Add($"曲面法線細節 {surfaceNormalDetailPercent:0}%");
        if (useAiDepth && facialFeatureContourPercent > 0f) modes.Add($"五官輪廓 {facialFeatureContourPercent:0}%");
        if (useAiDepth && facialDepthContrastPercent > 0f) modes.Add($"五官深度對比 {facialDepthContrastPercent:0}%");
        if (useAiDepth && facialMicroDetailPercent > 0f) modes.Add($"五官微細節 {facialMicroDetailPercent:0}%");
        if (useAiDepth && portraitCropApplied)
            modes.Add(fullBodySegmentationApplied ? "全身人物裁切" : "臉部胸像裁切");
        if (useAiDepth && fullBodySegmentationApplied && fullBodyDepthPercent > 0f)
            modes.Add($"全身輪廓深度 {fullBodyDepthPercent:0}%");
        if (useAiDepth && fullBodySegmentationApplied && bodyMaskCleanupPercent > 0f)
            modes.Add($"遮罩／附屬物清理 {bodyMaskCleanupPercent:0}%");
        if (useAiDepth && backgroundSuppressionPercent > 0f) modes.Add($"背景壓低 {backgroundSuppressionPercent:0}%");
        if (useAiDepth && createBustSilhouette) modes.Add("胸像輪廓");
        if ((!useAiDepth || blendDepth) &&
            (settings.HueDegrees != 0f || settings.SaturationPercent != 100f || settings.ValuePercent != 100f))
            modes.Add($"HSV({settings.HueDegrees:+0;-0;0}°, {settings.SaturationPercent:0}%, {settings.ValuePercent:0}%)");
        if (!useAiDepth && settings.Grayscale) modes.Add("灰階");
        if (settings.Invert) modes.Add("反相");
        if (settings.Symmetry) modes.Add($"對稱 {settings.SymmetryAxisPercent:0}%");
        if (settings.ReduceColors) modes.Add($"{settings.ColorLevels} 階降色");
        if (modes.Count == 0) modes.Add("HSV／正規化");
        return $"影像處理完成：{string.Join("、", modes)}";
    }

    private static ViewerProject CreatePreviewProject()
    {
        var model = new SceneModel
        {
            Name = "Relief preview placeholder",
            IsProcedural = true,
            Materials =
            [
                new PbrMaterial
                {
                    Name = "Preview material",
                    BaseColor = new Vector4(0.72f, 0.76f, 0.82f, 1f),
                    Metallic = 0.05f,
                    Roughness = 0.55f,
                    DoubleSided = true
                }
            ],
            Meshes = [CreatePreviewMesh()],
            Nodes =
            [
                new SceneNode
                {
                    Name = "Preview plate",
                    MeshIndices = [0]
                }
            ],
            SourceMeshIndices = [0],
            SourceMeshIndicesCaptured = true
        };
        model.CaptureMeshMaterialIndices();
        model.CaptureProceduralGeometry();

        return new ViewerProject
        {
            Name = "Relief preview",
            Models = [model],
            Camera = new CameraState
            {
                From = new Vector3(115f, 85f, 145f),
                To = new Vector3(0f, 4f, 0f),
                Up = Vector3.UnitY,
                FieldOfViewDegrees = 48f,
                FarPlane = 2000f
            },
            RenderSettings = new RenderSettings
            {
                BackgroundColor = new Vector4(0.055f, 0.065f, 0.08f, 1f),
                ShowGrid = true,
                ShowWorldAxes = true
            }
        };
    }

    private void ApplySavedSettings(ReliefPluginSettings settings)
    {
        _suppressParameterEvents = true;
        try
        {
            SetValue(widthNumericUpDown, settings.Width);
            SetValue(thicknessNumericUpDown, settings.Thickness);
            reliefHeightNumericUpDown.Maximum = Math.Max(
                reliefHeightNumericUpDown.Minimum,
                thicknessNumericUpDown.Value);
            SetValue(borderWidthNumericUpDown, settings.BorderWidth);
            SetValue(reliefHeightNumericUpDown, settings.ReliefHeight);
            qualityComboBox.SelectedIndex = Math.Clamp(settings.QualityIndex, 0, qualityComboBox.Items.Count - 1);
            textureCheckBox.Checked = settings.ShowTexture;
            grayscaleCheckBox.Checked = settings.Grayscale;
            SetValue(hueNumericUpDown, settings.Hue);
            SetValue(saturationNumericUpDown, settings.Saturation);
            SetValue(valueNumericUpDown, settings.Value);
            invertCheckBox.Checked = settings.Invert;
            reduceColorsCheckBox.Checked = settings.ReduceColors;
            SetValue(colorLevelsNumericUpDown, settings.ColorLevels);
            aiDepthCheckBox.Checked = settings.AiDepth;
            useDepthImageFileCheckBox.Checked = settings.UseDepthImageFile;
            depthImageFilePathTextBox.Text = settings.DepthImageFilePath ?? string.Empty;
            blendDepthCheckBox.Checked = settings.BlendDepth;
            RestoreImageProcessingPipeline(settings);
            SetValue(aiDepthWeightNumericUpDown, settings.AiDepthWeight);
            SetValue(depthCurveNumericUpDown, settings.DepthCurve);
            SetValue(localDetailNumericUpDown, settings.LocalDetail);
            SetValue(portraitGeometryNumericUpDown, settings.PortraitGeometry);
            SelectPortraitLevel(settings.PortraitLevel);
            SetValue(glassesReliefNumericUpDown, settings.GlassesRelief);
            SetValue(hairDetailNumericUpDown, settings.HairDetail);
            SetValue(surfaceNormalDetailNumericUpDown, settings.SurfaceNormalDetail);
            SetValue(facialFeatureContourNumericUpDown, settings.FacialFeatureContour);
            SetValue(facialDepthContrastNumericUpDown, settings.FacialDepthContrast);
            SetValue(facialMicroDetailNumericUpDown, settings.FacialMicroDetail);
            autoPortraitCropCheckBox.Checked = settings.AutoPortraitCrop;
            SetValue(backgroundSuppressionNumericUpDown, settings.BackgroundSuppression);
            bustSilhouetteCheckBox.Checked = settings.BustSilhouette;
            fullBodySegmentationCheckBox.Checked = settings.FullBodySegmentation;
            SelectBodyLevel(settings.BodyLevel);
            SetValue(fullBodyDepthNumericUpDown, settings.FullBodyDepth);
            SetValue(bodyMaskCleanupNumericUpDown, settings.BodyMaskCleanup);
            symmetryCheckBox.Checked = settings.Symmetry;
            SetValue(symmetryAxisNumericUpDown, settings.SymmetryAxis);
            smoothingCheckBox.Checked = settings.Smoothing;
            SetValue(smoothingThresholdNumericUpDown, settings.SmoothingThreshold);
            SetValue(smoothingStrengthNumericUpDown, settings.SmoothingStrength);
            SetValue(smoothingIterationsNumericUpDown, settings.SmoothingIterations);
            SetValue(environmentLightNumericUpDown, settings.EnvironmentLight);
            simplifyModelCheckBox.Checked = settings.SimplifyModel;
            SetValue(simplificationTargetNumericUpDown, settings.SimplificationTarget);
            SetValue(simplificationNormalAngleNumericUpDown, settings.SimplificationNormalAngle);
            xyPlaneRadioButton.Checked = settings.ModelPlane.Equals("XY", StringComparison.OrdinalIgnoreCase);
            yzPlaneRadioButton.Checked = settings.ModelPlane.Equals("YZ", StringComparison.OrdinalIgnoreCase);
            xzPlaneRadioButton.Checked = !xyPlaneRadioButton.Checked && !yzPlaneRadioButton.Checked;
            SetValue(modelRotationAngleNumericUpDown, settings.ModelRotationAngle);
            leftBottomAlignmentRadioButton.Checked = settings.ModelAlignment.Equals(
                "LeftBottom", StringComparison.OrdinalIgnoreCase);
            centerAlignmentRadioButton.Checked = !leftBottomAlignmentRadioButton.Checked;
            environmentLightTrackBar.Value = decimal.ToInt32(environmentLightNumericUpDown.Value);
            wireframeCheckBox.Checked = settings.Wireframe;
            _previewProject.RenderSettings.Wireframe = settings.Wireframe;
        }
        finally { _suppressParameterEvents = false; }
        SyncReliefDisplayFromControls();
    }

    private ReliefPluginSettings CaptureSettings()
    {
        NormalizeGeometryParameters(includeBorder: _processedBitmap is not null);
        return new ReliefPluginSettings
        {
            Width = widthNumericUpDown.Value,
            Thickness = thicknessNumericUpDown.Value,
            BorderWidth = borderWidthNumericUpDown.Value,
            ReliefHeight = reliefHeightNumericUpDown.Value,
            QualityIndex = qualityComboBox.SelectedIndex,
            ShowTexture = textureCheckBox.Checked,
            Grayscale = grayscaleCheckBox.Checked,
            Hue = hueNumericUpDown.Value,
            Saturation = saturationNumericUpDown.Value,
            Value = valueNumericUpDown.Value,
            Invert = invertCheckBox.Checked,
            ReduceColors = reduceColorsCheckBox.Checked,
            ColorLevels = colorLevelsNumericUpDown.Value,
            AiDepth = aiDepthCheckBox.Checked,
            UseDepthImageFile = useDepthImageFileCheckBox.Checked,
            DepthImageFilePath = depthImageFilePathTextBox.Text.Trim(),
            BlendDepth = blendDepthCheckBox.Checked,
            BlendImageProcessingMode = CaptureEnabledImageProcessingOperations().Contains("EdgeDetection") ? "EdgeDetection" : "Grayscale",
            ImageProcessingOrder = CaptureImageProcessingOrder(),
            EdgeDetectionEnabled = IsImageProcessingEnabled("EdgeDetection"),
            EdgeThreshold = edgeThresholdNumericUpDown.Value,
            EdgeStrength = edgeStrengthNumericUpDown.Value,
            EdgeSmoothing = edgeSmoothingNumericUpDown.Value,
            BinarizationEnabled = IsImageProcessingEnabled("Binarization"),
            BinarizationThreshold = binarizationThresholdNumericUpDown.Value,
            BinarizationInvert = binarizationInvertCheckBox.Checked,
            GaussianBlurEnabled = IsImageProcessingEnabled("GaussianBlur"),
            GaussianBlurRadius = gaussianBlurRadiusNumericUpDown.Value,
            AiDepthWeight = aiDepthWeightNumericUpDown.Value,
            DepthCurve = depthCurveNumericUpDown.Value,
            LocalDetail = localDetailNumericUpDown.Value,
            PortraitGeometry = portraitGeometryNumericUpDown.Value,
            PortraitLevel = portraitLevelComboBox.SelectedItem as string ?? "自訂",
            GlassesRelief = glassesReliefNumericUpDown.Value,
            HairDetail = hairDetailNumericUpDown.Value,
            SurfaceNormalDetail = surfaceNormalDetailNumericUpDown.Value,
            FacialFeatureContour = facialFeatureContourNumericUpDown.Value,
            FacialDepthContrast = facialDepthContrastNumericUpDown.Value,
            FacialMicroDetail = facialMicroDetailNumericUpDown.Value,
            AutoPortraitCrop = autoPortraitCropCheckBox.Checked,
            BackgroundSuppression = backgroundSuppressionNumericUpDown.Value,
            BustSilhouette = bustSilhouetteCheckBox.Checked,
            FullBodySegmentation = fullBodySegmentationCheckBox.Checked,
            BodyLevel = bodyLevelComboBox.SelectedItem as string ?? "自訂",
            FullBodyDepth = fullBodyDepthNumericUpDown.Value,
            BodyMaskCleanup = bodyMaskCleanupNumericUpDown.Value,
            Symmetry = symmetryCheckBox.Checked,
            SymmetryAxis = symmetryAxisNumericUpDown.Value,
            Smoothing = smoothingCheckBox.Checked,
            SmoothingThreshold = smoothingThresholdNumericUpDown.Value,
            SmoothingStrength = smoothingStrengthNumericUpDown.Value,
            SmoothingIterations = smoothingIterationsNumericUpDown.Value,
            EnvironmentLight = environmentLightNumericUpDown.Value,
            SimplifyModel = simplifyModelCheckBox.Checked,
            SimplificationTarget = simplificationTargetNumericUpDown.Value,
            SimplificationNormalAngle = simplificationNormalAngleNumericUpDown.Value,
            Wireframe = wireframeCheckBox.Checked,
            ModelPlane = xyPlaneRadioButton.Checked ? "XY" : yzPlaneRadioButton.Checked ? "YZ" : "XZ",
            ModelRotationAngle = modelRotationAngleNumericUpDown.Value,
            ModelAlignment = leftBottomAlignmentRadioButton.Checked ? "LeftBottom" : "Center"
        };
    }

    private static void SetValue(NumericUpDown control, decimal value) =>
        control.Value = Math.Clamp(value, control.Minimum, control.Maximum);

    private static MeshData CreatePreviewMesh()
    {
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var textureCoordinates = new List<Vector2>();
        var indices = new List<uint>();

        AddFace(new(-50f, 0f, -35f), new(50f, 0f, -35f), new(50f, 0f, 35f), new(-50f, 0f, 35f), -Vector3.UnitY);
        AddFace(new(-50f, 8f, 35f), new(50f, 8f, 35f), new(50f, 8f, -35f), new(-50f, 8f, -35f), Vector3.UnitY);
        AddFace(new(-50f, 0f, 35f), new(50f, 0f, 35f), new(50f, 8f, 35f), new(-50f, 8f, 35f), Vector3.UnitZ);
        AddFace(new(50f, 0f, -35f), new(-50f, 0f, -35f), new(-50f, 8f, -35f), new(50f, 8f, -35f), -Vector3.UnitZ);
        AddFace(new(-50f, 0f, -35f), new(-50f, 0f, 35f), new(-50f, 8f, 35f), new(-50f, 8f, -35f), -Vector3.UnitX);
        AddFace(new(50f, 0f, 35f), new(50f, 0f, -35f), new(50f, 8f, -35f), new(50f, 8f, 35f), Vector3.UnitX);

        return new MeshData
        {
            Name = "Preview plate",
            MaterialIndex = 0,
            Positions = [.. positions],
            Normals = [.. normals],
            TextureCoordinates = [.. textureCoordinates],
            Tangents = Enumerable.Repeat(new Vector4(1f, 0f, 0f, 1f), positions.Count).ToArray(),
            Indices = [.. indices]
        };

        void AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            var start = (uint)positions.Count;
            positions.AddRange([a, b, c, d]);
            normals.AddRange([normal, normal, normal, normal]);
            textureCoordinates.AddRange([new(0f, 1f), new(1f, 1f), new(1f, 0f), new(0f, 0f)]);
            indices.AddRange([start, start + 1, start + 2, start, start + 2, start + 3]);
        }
    }

    private void ReliefBuilderForm_Load(object? sender, EventArgs e)
    {

    }
}
