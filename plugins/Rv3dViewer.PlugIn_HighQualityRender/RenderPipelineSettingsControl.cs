namespace Rv3dViewer.HighQualityRenderPlugin;

internal sealed partial class RenderPipelineSettingsControl : UserControl
{
    private bool _suppressEvents;

    public RenderPipelineSettingsControl()
    {
        InitializeComponent();
    }

    internal event EventHandler? SettingsChanged;
    internal event EventHandler<RenderQualityPreset>? PresetSelected;

    internal int RenderScalePercent
    {
        get => decimal.ToInt32(renderScaleNumericUpDown.Value);
        set => renderScaleNumericUpDown.Value = Math.Clamp(value,
            decimal.ToInt32(renderScaleNumericUpDown.Minimum),
            decimal.ToInt32(renderScaleNumericUpDown.Maximum));
    }

    internal RenderExecutionMode ExecutionMode
    {
        get => (RenderExecutionMode)Math.Max(0, executionModeComboBox.SelectedIndex);
        set => executionModeComboBox.SelectedIndex = (int)value;
    }

    internal RenderDenoiserMode DenoiserMode
    {
        get => (RenderDenoiserMode)Math.Max(0, denoiserComboBox.SelectedIndex);
        set => denoiserComboBox.SelectedIndex = (int)value;
    }

    internal bool AdaptiveSampling { get => adaptiveSamplingCheckBox.Checked; set => adaptiveSamplingCheckBox.Checked = value; }
    internal bool UseHdriImportanceSampling { get => hdriImportanceCheckBox.Checked; set => hdriImportanceCheckBox.Checked = value; }

    internal RenderGlassQuality GlassQuality
    {
        get => (RenderGlassQuality)Math.Max(0, glassQualityComboBox.SelectedIndex);
        set => glassQualityComboBox.SelectedIndex = (int)value;
    }

    internal RenderTextureQuality TextureQuality
    {
        get => (RenderTextureQuality)Math.Max(0, textureQualityComboBox.SelectedIndex);
        set => textureQualityComboBox.SelectedIndex = (int)value;
    }

    internal float FireflyClamp
    {
        get => (float)fireflyClampNumericUpDown.Value;
        set => fireflyClampNumericUpDown.Value = Math.Clamp((decimal)value,
            fireflyClampNumericUpDown.Minimum, fireflyClampNumericUpDown.Maximum);
    }

    internal bool ExportAov { get => exportAovCheckBox.Checked; set => exportAovCheckBox.Checked = value; }

    internal void SetPresetSelection(RenderQualityPreset? preset)
    {
        _suppressEvents = true;
        try
        {
            fastRadioButton.Checked = preset == RenderQualityPreset.Fast;
            balancedRadioButton.Checked = preset == RenderQualityPreset.Balanced;
            highQualityRadioButton.Checked = preset == RenderQualityPreset.HighQuality;
        }
        finally { _suppressEvents = false; }
    }

    private void PresetRadioButton_CheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressEvents || sender is not RadioButton { Checked: true } radioButton) return;
        var preset = radioButton == fastRadioButton
            ? RenderQualityPreset.Fast
            : radioButton == balancedRadioButton
                ? RenderQualityPreset.Balanced
                : RenderQualityPreset.HighQuality;
        PresetSelected?.Invoke(this, preset);
    }

    private void PipelineSetting_Changed(object? sender, EventArgs e)
    {
        if (!_suppressEvents) SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
