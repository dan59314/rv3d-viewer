using System.Text.Json;

namespace Rv3dViewer.ReliefPlugin;

partial class ReliefBuilderForm
{
    private bool _refreshingParameterProfiles;
    private bool _updatingGroupExpansionState;
    private string? _lastParameterProfilePath;

    private CollapsibleGroupBox[] CollapsibleGroups =>
    [
        geometryGroupBox,
        smoothingGroupBox,
        processingGroupBox,
        depthGroupBox,
        portraitLayersGroupBox
    ];

    private void RestoreParameterProfileUiState()
    {
        var state = ReliefUiState.Load();
        sourceGroupBox.Expanded = state.SourceExpanded;
        geometryGroupBox.Expanded = state.GeometryExpanded;
        smoothingGroupBox.Expanded = state.SmoothingExpanded;
        processingGroupBox.Expanded = state.ProcessingExpanded;
        depthGroupBox.Expanded = state.DepthExpanded;
        portraitLayersGroupBox.Expanded = state.PortraitExpanded;
        SynchronizeExpandAllCheckBox();
        _lastParameterProfilePath = state.LastParameterProfilePath;
        ReloadParameterProfiles(_lastParameterProfilePath);

        if (string.IsNullOrWhiteSpace(_lastParameterProfilePath) || !File.Exists(_lastParameterProfilePath)) return;
        try
        {
            ApplySavedSettings(ReliefParameterProfileService.Load(_lastParameterProfilePath));
            UpdateDependentControlStates();
            ApplyEnvironmentLight();
            statusLabel.Text = $"已恢復上次參數設定：{Path.GetFileName(_lastParameterProfilePath)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            _lastParameterProfilePath = null;
            ReloadParameterProfiles();
            statusLabel.Text = $"無法恢復上次參數設定：{ex.Message}";
        }
    }

    private void SaveParameterProfileUiState()
    {
        new ReliefUiState
        {
            LastParameterProfilePath = _lastParameterProfilePath,
            SourceExpanded = sourceGroupBox.Expanded,
            GeometryExpanded = geometryGroupBox.Expanded,
            SmoothingExpanded = smoothingGroupBox.Expanded,
            ProcessingExpanded = processingGroupBox.Expanded,
            DepthExpanded = depthGroupBox.Expanded,
            PortraitExpanded = portraitLayersGroupBox.Expanded
        }.Save();
    }

    private void ReloadParameterProfiles(string? selectPath = null)
    {
        _refreshingParameterProfiles = true;
        try
        {
            parameterProfileComboBox.BeginUpdate();
            parameterProfileComboBox.Items.Clear();
            foreach (var path in ReliefParameterProfileService.FindProfiles())
                parameterProfileComboBox.Items.Add(new ParameterProfileItem(path));
            parameterProfileComboBox.EndUpdate();

            if (!string.IsNullOrWhiteSpace(selectPath))
            {
                var fullPath = Path.GetFullPath(selectPath);
                parameterProfileComboBox.SelectedItem = parameterProfileComboBox.Items
                    .Cast<ParameterProfileItem>()
                    .FirstOrDefault(item => string.Equals(
                        Path.GetFullPath(item.Path), fullPath, StringComparison.OrdinalIgnoreCase));
            }
            else parameterProfileComboBox.SelectedIndex = -1;
            UpdateParameterProfileButtons();
        }
        finally
        {
            _refreshingParameterProfiles = false;
        }
    }

    private async void ParameterProfileComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_refreshingParameterProfiles || parameterProfileComboBox.SelectedItem is not ParameterProfileItem item) return;
        _lastParameterProfilePath = item.Path;
        UpdateParameterProfileButtons();
        await ApplyParameterProfileAsync(item.Path);
    }

    private async void OpenParameterProfile_Click(object? sender, EventArgs e)
    {
        Directory.CreateDirectory(ReliefParameterProfileService.ParamsDirectory);
        parameterProfileOpenFileDialog.InitialDirectory = ReliefParameterProfileService.ParamsDirectory;
        if (parameterProfileOpenFileDialog.ShowDialog(this) != DialogResult.OK) return;
        _lastParameterProfilePath = parameterProfileOpenFileDialog.FileName;
        await ApplyParameterProfileAsync(parameterProfileOpenFileDialog.FileName);
        SelectProfileIfListed(parameterProfileOpenFileDialog.FileName);
    }

    private void SaveParameterProfile_Click(object? sender, EventArgs e)
    {
        Directory.CreateDirectory(ReliefParameterProfileService.ParamsDirectory);
        parameterProfileSaveFileDialog.InitialDirectory = ReliefParameterProfileService.ParamsDirectory;
        parameterProfileSaveFileDialog.FileName = parameterProfileComboBox.SelectedItem is ParameterProfileItem item
            ? Path.GetFileName(item.Path)
            : "ReliefParams.rlfPar";
        if (parameterProfileSaveFileDialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            ReliefParameterProfileService.Save(parameterProfileSaveFileDialog.FileName, CaptureSettings());
            _lastParameterProfilePath = parameterProfileSaveFileDialog.FileName;
            ReloadParameterProfiles(parameterProfileSaveFileDialog.FileName);
            statusLabel.Text = $"已儲存參數設定：{Path.GetFileName(parameterProfileSaveFileDialog.FileName)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, ex.Message, "儲存參數設定失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ApplyParameterProfileAsync(string path)
    {
        var previous = CaptureSettings();
        try
        {
            var settings = ReliefParameterProfileService.Load(path);
            ApplySavedSettings(settings);
            UpdateDependentControlStates();
            ApplyEnvironmentLight();
            if (_latestMeshBuild is not null) ApplyTextureVisibility(_latestMeshBuild.Model);
            if (_sourceBitmap is not null)
                await ProcessImageAsync();
            else
                _previewRenderer.InvalidateScene();
            statusLabel.Text = $"已載入參數設定：{Path.GetFileName(path)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            ApplySavedSettings(previous);
            UpdateDependentControlStates();
            ApplyEnvironmentLight();
            MessageBox.Show(this, ex.Message, $"讀取參數設定失敗：{Path.GetFileName(path)}",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SelectProfileIfListed(string path)
    {
        _refreshingParameterProfiles = true;
        try
        {
            var fullPath = Path.GetFullPath(path);
            parameterProfileComboBox.SelectedItem = parameterProfileComboBox.Items.Cast<ParameterProfileItem>()
                .FirstOrDefault(item => string.Equals(Path.GetFullPath(item.Path), fullPath, StringComparison.OrdinalIgnoreCase));
        }
        finally { _refreshingParameterProfiles = false; }
        UpdateParameterProfileButtons();
    }

    private void UpdateParameterProfileButtons()
    {
        var hasSelection = parameterProfileComboBox.SelectedItem is ParameterProfileItem item && File.Exists(item.Path);
        updateParameterProfileButton.Enabled = hasSelection;
        renameParameterProfileButton.Enabled = hasSelection;
    }

    private void UpdateParameterProfileButton_Click(object? sender, EventArgs e)
    {
        if (parameterProfileComboBox.SelectedItem is not ParameterProfileItem item) return;
        try
        {
            ReliefParameterProfileService.Save(item.Path, CaptureSettings());
            _lastParameterProfilePath = item.Path;
            statusLabel.Text = $"已更新參數設定：{Path.GetFileName(item.Path)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, ex.Message, "更新參數設定失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddParameterProfileButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new ParameterProfileNameDialog("新增參數設定");
        if (!TryGetProfileName(dialog, out var name)) return;
        var path = Path.Combine(ReliefParameterProfileService.ParamsDirectory, name + ".rlfPar");
        if (File.Exists(path))
        {
            MessageBox.Show(this, "已有相同名稱的參數設定。", "無法新增",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            ReliefParameterProfileService.Save(path, CaptureSettings());
            _lastParameterProfilePath = path;
            ReloadParameterProfiles(path);
            statusLabel.Text = $"已新增參數設定：{Path.GetFileName(path)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, ex.Message, "新增參數設定失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RenameParameterProfileButton_Click(object? sender, EventArgs e)
    {
        if (parameterProfileComboBox.SelectedItem is not ParameterProfileItem item) return;
        var oldName = Path.GetFileNameWithoutExtension(item.Path);
        using var dialog = new ParameterProfileNameDialog("更名參數設定", oldName);
        if (!TryGetProfileName(dialog, out var newName) ||
            string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return;

        var newPath = Path.Combine(ReliefParameterProfileService.ParamsDirectory, newName + ".rlfPar");
        if (File.Exists(newPath))
        {
            MessageBox.Show(this, "已有相同名稱的參數設定。", "無法更名",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            File.Move(item.Path, newPath);
            _lastParameterProfilePath = newPath;
            ReloadParameterProfiles(newPath);
            statusLabel.Text = $"參數設定已更名為：{Path.GetFileName(newPath)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "更名參數設定失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool TryGetProfileName(ParameterProfileNameDialog dialog, out string name)
    {
        name = string.Empty;
        if (dialog.ShowDialog(this) != DialogResult.OK) return false;
        name = dialog.ProfileName;
        var invalid = string.IsNullOrWhiteSpace(name) ||
                      name is "." or ".." ||
                      name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                      IsReservedWindowsFileName(name);
        if (!invalid) return true;

        MessageBox.Show(this, "請輸入有效的檔案名稱；名稱不可為空白、系統保留名稱或包含非法字元。",
            "名稱無效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        name = string.Empty;
        return false;
    }

    private static bool IsReservedWindowsFileName(string name)
    {
        var stem = name.Split('.')[0].TrimEnd(' ', '.').ToUpperInvariant();
        return stem is "CON" or "PRN" or "AUX" or "NUL" or
            "COM1" or "COM2" or "COM3" or "COM4" or "COM5" or "COM6" or "COM7" or "COM8" or "COM9" or
            "LPT1" or "LPT2" or "LPT3" or "LPT4" or "LPT5" or "LPT6" or "LPT7" or "LPT8" or "LPT9";
    }

    private void ExpandAllGroupsCheckBox_CheckStateChanged(object? sender, EventArgs e)
    {
        if (_updatingGroupExpansionState || expandAllGroupsCheckBox.CheckState == CheckState.Indeterminate) return;
        _updatingGroupExpansionState = true;
        parameterTableLayoutPanel.SuspendLayout();
        try
        {
            var expanded = expandAllGroupsCheckBox.Checked;
            foreach (var group in CollapsibleGroups) group.Expanded = expanded;
        }
        finally
        {
            parameterTableLayoutPanel.ResumeLayout(true);
            _updatingGroupExpansionState = false;
        }
    }

    private void CollapsibleGroupBox_ExpandedChanged(object? sender, EventArgs e)
    {
        if (!_updatingGroupExpansionState) SynchronizeExpandAllCheckBox();
    }

    private void SynchronizeExpandAllCheckBox()
    {
        _updatingGroupExpansionState = true;
        try
        {
            var groups = CollapsibleGroups;
            expandAllGroupsCheckBox.CheckState = groups.All(group => group.Expanded)
                ? CheckState.Checked
                : groups.All(group => !group.Expanded)
                    ? CheckState.Unchecked
                    : CheckState.Indeterminate;
        }
        finally { _updatingGroupExpansionState = false; }
    }

    private sealed record ParameterProfileItem(string Path)
    {
        public override string ToString() => System.IO.Path.GetFileNameWithoutExtension(Path);
    }
}
