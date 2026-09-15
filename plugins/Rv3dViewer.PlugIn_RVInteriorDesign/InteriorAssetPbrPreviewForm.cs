namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Numerics;
using System.Text;
using Rv3dViewer.Core;

internal sealed partial class InteriorAssetPbrPreviewForm : Form
{
    private readonly InteriorAssetDescriptor _asset;
    private readonly SceneModel _model;
    private readonly InteriorPbrPreviewSettings _settings;
    private string? _environmentPath;
    private bool _restoringSettings;

    internal InteriorAssetPbrPreviewForm(InteriorAssetDescriptor asset, SceneModel model)
    {
        _asset = asset ?? throw new ArgumentNullException(nameof(asset));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _settings = InteriorPbrPreviewSettings.Load();
        InitializeComponent();
        Text = $"PBR 預覽 - {asset.Name}";
        modelNameLabel.Text = asset.Name;
        diagnosticsTextBox.Text = BuildDiagnostics(model);
        RestorePreviewSettings();
        pbrViewport.PreviewError += PbrViewport_PreviewError;
    }

    private void InteriorAssetPbrPreviewForm_Shown(object? sender, EventArgs e)
    {
        try
        {
            pbrViewport.EnableRuntimeContext();
            pbrViewport.SelectionMode = InteriorViewportSelectionMode.Navigate;
            pbrViewport.SetSceneModels([_model]);
            ApplyLighting();
            ApplyBackground();
            ApplyEnvironment();
            FrameModel();
            statusLabel.Text = "PBR 預覽已就緒。左鍵旋轉、中鍵移動目標、右鍵平移、滾輪縮放。";
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            statusLabel.Text = $"PBR 預覽初始化失敗：{exception.Message}";
        }
    }

    private void InteriorAssetPbrPreviewForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        SavePreviewSettings();
        pbrViewport.PrepareForFormClose();
    }

    private void PbrViewport_PreviewError(object? sender, string message) => statusLabel.Text = message;

    private void FrameButton_Click(object? sender, EventArgs e) => FrameModel();

    private void CloseButton_Click(object? sender, EventArgs e) => Close();

    private void LightingTrackBar_ValueChanged(object? sender, EventArgs e)
    {
        ApplyLighting();
        SavePreviewSettings();
    }

    private void EnvironmentTrackBar_ValueChanged(object? sender, EventArgs e)
    {
        ApplyEnvironment();
        SavePreviewSettings();
    }

    private void BackgroundControl_Changed(object? sender, EventArgs e)
    {
        ApplyBackground();
        ApplyEnvironment();
        SavePreviewSettings();
    }

    private void LoadEnvironmentButton_Click(object? sender, EventArgs e)
    {
        if (environmentOpenFileDialog.ShowDialog(this) != DialogResult.OK)
            return;
        _environmentPath = environmentOpenFileDialog.FileName;
        environmentFileLabel.Text = Path.GetFileName(_environmentPath);
        ApplyEnvironment();
        SavePreviewSettings();
    }

    private void ClearEnvironmentButton_Click(object? sender, EventArgs e)
    {
        _environmentPath = null;
        environmentFileLabel.Text = "未載入 HDRI";
        ApplyEnvironment();
        SavePreviewSettings();
    }

    private void RestorePreviewSettings()
    {
        _restoringSettings = true;
        try
        {
            _environmentPath = string.IsNullOrWhiteSpace(_settings.EnvironmentPath)
                ? null
                : _settings.EnvironmentPath;
            lightingTrackBar.Value = Math.Clamp(_settings.LightingPercent,
                lightingTrackBar.Minimum, lightingTrackBar.Maximum);
            environmentTrackBar.Value = Math.Clamp(_settings.EnvironmentIntensityValue,
                environmentTrackBar.Minimum, environmentTrackBar.Maximum);
            backgroundComboBox.SelectedIndex = Math.Clamp(_settings.BackgroundIndex, 0,
                backgroundComboBox.Items.Count - 1);
            showGridCheckBox.Checked = _settings.ShowGrid;
            showEnvironmentCheckBox.Checked = _settings.ShowEnvironmentBackground;
            environmentFileLabel.Text = _environmentPath switch
            {
                null => "未載入 HDRI",
                { } path when File.Exists(path) => Path.GetFileName(path),
                { } path => $"找不到：{Path.GetFileName(path)}"
            };
        }
        finally
        {
            _restoringSettings = false;
        }
    }

    private void SavePreviewSettings()
    {
        if (_restoringSettings)
            return;
        _settings.EnvironmentPath = _environmentPath ?? string.Empty;
        _settings.LightingPercent = lightingTrackBar.Value;
        _settings.EnvironmentIntensityValue = environmentTrackBar.Value;
        _settings.BackgroundIndex = Math.Max(0, backgroundComboBox.SelectedIndex);
        _settings.ShowGrid = showGridCheckBox.Checked;
        _settings.ShowEnvironmentBackground = showEnvironmentCheckBox.Checked;
        _settings.Save();
    }

    private void ApplyLighting()
    {
        var scale = lightingTrackBar.Value / 100f;
        lightingValueLabel.Text = $"{scale:0.00}×";
        pbrViewport.SetLightingScale(scale);
    }

    private void ApplyEnvironment()
    {
        var intensity = environmentTrackBar.Value / 20f;
        environmentValueLabel.Text = intensity.ToString("0.00");
        pbrViewport.SetEnvironment(_environmentPath, intensity, showEnvironmentCheckBox.Checked);
    }

    private void ApplyBackground()
    {
        var color = backgroundComboBox.SelectedIndex switch
        {
            1 => Color.FromArgb(225, 228, 232),
            2 => Color.FromArgb(96, 100, 106),
            _ => Color.FromArgb(14, 17, 21)
        };
        pbrViewport.SetBackgroundColor(color);
        pbrViewport.SetGridVisible(showGridCheckBox.Checked);
    }

    private void FrameModel()
    {
        if (!SceneTraversal.TryCalculateBounds(_model, out var bounds))
            return;
        var camera = pbrViewport.Camera;
        var direction = Vector3.Normalize(new Vector3(1.35f, .9f, 1.55f));
        var radius = Math.Max(bounds.Radius, .05f);
        var distance = radius / MathF.Sin(camera.FieldOfViewDegrees * MathF.PI / 360f) * 1.25f;
        camera.To = bounds.Center;
        camera.From = bounds.Center + direction * distance;
        camera.Up = Vector3.UnitY;
        camera.RollDegrees = 0f;
        camera.NearPlane = Math.Max(.0001f, distance - radius * 2f);
        camera.FarPlane = Math.Max(camera.NearPlane + 1f, distance + radius * 3f);
        pbrViewport.SetCamera(camera);
    }

    private static string BuildDiagnostics(SceneModel model)
    {
        var triangleCount = model.Meshes.Sum(mesh => mesh.Indices.Length / 3);
        var builder = new StringBuilder()
            .AppendLine($"Mesh：{model.Meshes.Count:N0}")
            .AppendLine($"三角形：{triangleCount:N0}")
            .AppendLine($"材質：{model.Materials.Count:N0}")
            .AppendLine();
        foreach (var material in model.Materials)
        {
            builder.AppendLine($"[{material.Name}]");
            foreach (var semantic in Enum.GetValues<TextureSemantic>())
            {
                var paths = new List<string>();
                if (material.Textures.TryGetValue(semantic, out var legacy) && legacy.Enabled &&
                    !string.IsNullOrWhiteSpace(legacy.Path))
                    paths.Add(legacy.Path);
                if (material.TextureStacks.TryGetValue(semantic, out var stack) && stack.Enabled)
                    paths.AddRange(stack.Layers.Where(layer => layer.Enabled &&
                            layer.Kind == TextureLayerKind.Image && !string.IsNullOrWhiteSpace(layer.Path))
                        .Select(layer => layer.Path));
                if (paths.Count == 0) continue;
                var missing = paths.Distinct(StringComparer.OrdinalIgnoreCase).Count(path => !File.Exists(path));
                builder.AppendLine(missing == 0
                    ? $"  ✓ {semantic} ({paths.Count})"
                    : $"  ⚠ {semantic}：缺少 {missing}/{paths.Count}");
            }
            builder.AppendLine();
        }
        return builder.ToString().TrimEnd();
    }
}
