namespace Rv3dViewer.ReliefPlugin;

partial class ReliefBuilderForm
{
    private static string OperationKey(object item) => item?.ToString() switch
    {
        "邊緣偵測" => "EdgeDetection",
        "二值化" => "Binarization",
        "高斯模糊" => "GaussianBlur",
        _ => "Grayscale"
    };

    private List<string> CaptureImageProcessingOrder() =>
        imageProcessingCheckedListBox.Items.Cast<object>().Select(OperationKey).ToList();

    private List<string> CaptureEnabledImageProcessingOperations() =>
        imageProcessingCheckedListBox.CheckedItems.Cast<object>().Select(OperationKey).ToList();

    private bool IsImageProcessingEnabled(string key) =>
        imageProcessingCheckedListBox.CheckedItems.Cast<object>().Any(item => OperationKey(item) == key);

    private void RestoreImageProcessingPipeline(ReliefPluginSettings settings)
    {
        var order = settings.ImageProcessingOrder is { Count: > 0 }
            ? settings.ImageProcessingOrder
            : ["Grayscale", "EdgeDetection", "Binarization", "GaussianBlur"];
        var valid = order.Where(key => key is "Grayscale" or "EdgeDetection" or "Binarization" or "GaussianBlur")
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var missing in new[] { "Grayscale", "EdgeDetection", "Binarization", "GaussianBlur" })
            if (!valid.Contains(missing, StringComparer.OrdinalIgnoreCase)) valid.Add(missing);

        imageProcessingCheckedListBox.Items.Clear();
        foreach (var key in valid)
        {
            var label = key switch { "EdgeDetection" => "邊緣偵測", "Binarization" => "二值化", "GaussianBlur" => "高斯模糊", _ => "灰階" };
            var enabled = key switch
            {
                "EdgeDetection" => settings.EdgeDetectionEnabled ||
                    settings.BlendImageProcessingMode.Equals("EdgeDetection", StringComparison.OrdinalIgnoreCase),
                "Binarization" => settings.BinarizationEnabled,
                "GaussianBlur" => settings.GaussianBlurEnabled,
                _ => settings.Grayscale
            };
            imageProcessingCheckedListBox.Items.Add(label, enabled);
        }
        SetValue(edgeThresholdNumericUpDown, settings.EdgeThreshold);
        SetValue(edgeStrengthNumericUpDown, settings.EdgeStrength);
        SetValue(edgeSmoothingNumericUpDown, settings.EdgeSmoothing);
        SetValue(binarizationThresholdNumericUpDown, settings.BinarizationThreshold);
        binarizationInvertCheckBox.Checked = settings.BinarizationInvert;
        SetValue(gaussianBlurRadiusNumericUpDown, settings.GaussianBlurRadius);
        imageProcessingCheckedListBox.SelectedIndex = 0;
        UpdateImageProcessingParameterVisibility();
    }

    private void ImageProcessingCheckedListBox_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_suppressParameterEvents) return;
        BeginInvoke(() =>
        {
            if (IsDisposed) return;
            grayscaleCheckBox.Checked = IsImageProcessingEnabled("Grayscale");
            UpdateDependentControlStates();
            ScheduleImageProcessing(immediate: false);
        });
    }

    private void ImageProcessingCheckedListBox_SelectedIndexChanged(object? sender, EventArgs e) =>
        UpdateImageProcessingParameterVisibility();

    private void MoveImageProcessingUpButton_Click(object? sender, EventArgs e) => MoveImageProcessing(-1);
    private void MoveImageProcessingDownButton_Click(object? sender, EventArgs e) => MoveImageProcessing(1);

    private void MoveImageProcessing(int offset)
    {
        var index = imageProcessingCheckedListBox.SelectedIndex;
        var target = index + offset;
        if (index < 0 || target < 0 || target >= imageProcessingCheckedListBox.Items.Count) return;
        var item = imageProcessingCheckedListBox.Items[index];
        var state = imageProcessingCheckedListBox.GetItemCheckState(index);
        _suppressParameterEvents = true;
        try
        {
            imageProcessingCheckedListBox.Items.RemoveAt(index);
            imageProcessingCheckedListBox.Items.Insert(target, item);
            imageProcessingCheckedListBox.SetItemCheckState(target, state);
            imageProcessingCheckedListBox.SelectedIndex = target;
        }
        finally { _suppressParameterEvents = false; }
        ScheduleImageProcessing(immediate: false);
    }

    private void UpdateImageProcessingParameterVisibility()
    {
        var key = imageProcessingCheckedListBox.SelectedItem is { } item ? OperationKey(item) : "Grayscale";
        var grayscale = key == "Grayscale";
        var edge = key == "EdgeDetection";
        var binary = key == "Binarization";
        var blur = key == "GaussianBlur";
        foreach (var control in new Control[] { grayscaleCheckBox, hueLabel, hueEditorPanel, saturationLabel,
                     saturationEditorPanel, valueLabel, valueEditorPanel, invertCheckBox, reduceColorsCheckBox,
                     colorLevelsLabel, colorLevelsEditorPanel })
            control.Visible = grayscale;
        grayscaleCheckBox.Visible = false;
        edgeThresholdLabel.Visible = edge;
        edgeThresholdNumericUpDown.Visible = edge;
        edgeStrengthLabel.Visible = edge;
        edgeStrengthNumericUpDown.Visible = edge;
        edgeSmoothingLabel.Visible = edge;
        edgeSmoothingNumericUpDown.Visible = edge;
        binarizationThresholdLabel.Visible = binary;
        binarizationThresholdNumericUpDown.Visible = binary;
        binarizationInvertCheckBox.Visible = binary;
        gaussianBlurRadiusLabel.Visible = blur;
        gaussianBlurRadiusNumericUpDown.Visible = blur;
        moveImageProcessingUpButton.Enabled = imageProcessingCheckedListBox.SelectedIndex > 0;
        moveImageProcessingDownButton.Enabled = imageProcessingCheckedListBox.SelectedIndex >= 0 &&
            imageProcessingCheckedListBox.SelectedIndex < imageProcessingCheckedListBox.Items.Count - 1;
        processingTableLayoutPanel.PerformLayout();
    }
}
