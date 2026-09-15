namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Text.Json;

internal sealed partial class RVInteriorDesignForm
{
    private string? _currentProjectPath;
    private bool _projectDirty;

    private void NewProjectMenuItem_Click(object? sender, EventArgs e)
    {
        if (!ConfirmSaveProjectChanges())
            return;

        _designObjects.Clear();
        _draftParameters = null;
        _draftPreviewModel = null;
        _hasUnexportedChanges = false;
        _undoHistory.Clear();
        _redoHistory.Clear();
        RebuildSceneTreeFromDesignObjects();
        RefreshViewportScene();
        SetSelectedModels([], null, null, true);
        parameterPropertyGrid.SelectedObject = null;
        objectPropertyGrid.SelectedObject = null;
        _currentProjectPath = null;
        _projectDirty = false;
        UpdateHistoryMenuState();
        UpdateProjectTitle();
        TrySaveSession(out _);
        statusLabel.Text = "已建立新的空白室內設計專案。";
    }

    private void UnifiedMenuStrip_ReadRequested(object? sender, EventArgs e)
    {
        if (!ConfirmSaveProjectChanges())
            return;
        if (openProjectDialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            UseWaitCursor = true;
            var fullPath = Path.GetFullPath(openProjectDialog.FileName);
            var result = Task.Run(() => InteriorDesignProjectStore.LoadAsync(fullPath)).GetAwaiter().GetResult();
            ApplyLoadedProject(result, fullPath);
            TrySaveSession(out _);
            statusLabel.Text = $"已開啟室內設計專案：{Path.GetFileName(fullPath)}；共 {_designObjects.Count} 個模型。";
        }
        catch (Exception exception) when (IsProjectFileException(exception))
        {
            MessageBox.Show(this, $"無法開啟室內設計專案：\r\n\r\n{exception.Message}",
                "開啟 .Rv3dPrj 失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void UnifiedMenuStrip_SaveRequested(object? sender, EventArgs e) => SaveProject(false);

    private void SaveProjectAsMenuItem_Click(object? sender, EventArgs e) => SaveProject(true);

    private bool SaveProject(bool saveAs)
    {
        var path = _currentProjectPath;
        if (saveAs || string.IsNullOrWhiteSpace(path))
        {
            saveProjectDialog.FileName = string.IsNullOrWhiteSpace(path)
                ? "RV室內設計.rv3dproj"
                : Path.GetFileName(path);
            if (saveProjectDialog.ShowDialog(this) != DialogResult.OK)
                return false;
            path = Path.GetFullPath(saveProjectDialog.FileName);
        }

        try
        {
            UseWaitCursor = true;
            var project = InteriorDesignProjectStore.CreateProject(
                Path.GetFileNameWithoutExtension(path), _designObjects, _selectedModelIds,
                _selectedDesignObject?.Id, _selectedMeshIndex, _showModelEdges, _visibilityMode,
                _pbrPreviewEnabled, topViewport.Camera, frontViewport.Camera, rightViewport.Camera,
                perspectiveViewport.Camera);
            Task.Run(() => InteriorDesignProjectStore.SaveAsync(project, path)).GetAwaiter().GetResult();
            _currentProjectPath = path;
            _projectDirty = false;
            UpdateProjectTitle();
            TrySaveSession(out _);
            statusLabel.Text = $"已儲存室內設計專案：{Path.GetFileName(path)}。";
            return true;
        }
        catch (Exception exception) when (IsProjectFileException(exception))
        {
            MessageBox.Show(this, $"無法儲存室內設計專案：\r\n\r\n{exception.Message}",
                "儲存 .Rv3dPrj 失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private bool ConfirmSaveProjectChanges()
    {
        if (!_projectDirty)
            return true;
        var name = string.IsNullOrWhiteSpace(_currentProjectPath)
            ? "未命名專案"
            : Path.GetFileName(_currentProjectPath);
        var result = MessageBox.Show(this,
            $"室內設計專案「{name}」有尚未儲存的變更。\r\n\r\n是否先儲存？",
            "RV室內設計", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1);
        return result switch
        {
            DialogResult.Yes => SaveProject(false),
            DialogResult.No => true,
            _ => false
        };
    }

    private void ApplyLoadedProject(InteriorDesignProjectLoadResult result, string fullPath)
    {
        _designObjects.Clear();
        _designObjects.AddRange(result.Objects);
        _draftParameters = null;
        _draftPreviewModel = null;
        _hasUnexportedChanges = _designObjects.Count > 0;
        _undoHistory.Clear();
        _redoHistory.Clear();

        var extension = result.Extension;
        InteriorPreviewViewportControl.CopyCamera(extension.TopCamera, topViewport.Camera);
        InteriorPreviewViewportControl.CopyCamera(extension.FrontCamera, frontViewport.Camera);
        InteriorPreviewViewportControl.CopyCamera(extension.RightCamera, rightViewport.Camera);
        InteriorPreviewViewportControl.CopyCamera(extension.PerspectiveCamera, perspectiveViewport.Camera);
        previewViewport.SetCamera(extension.PerspectiveCamera);
        foreach (var viewport in GetViewports())
            viewport.Invalidate();

        ShowModelEdges(extension.ShowModelEdges);
        ApplyVisibilityMode(extension.VisibilityMode);
        pbrPreviewToolButton.Checked = extension.PbrPreviewEnabled;
        RebuildSceneTreeFromDesignObjects();
        RefreshViewportScene();
        var selectedIds = extension.SelectedModelIds.Count > 0
            ? extension.SelectedModelIds
            : extension.SelectedModelId is Guid selectedId ? [selectedId] : [];
        SetSelectedModels(selectedIds, extension.SelectedModelId, extension.SelectedMeshIndex, true);

        _currentProjectPath = fullPath;
        _projectDirty = false;
        UpdateHistoryMenuState();
        UpdateProjectTitle();
    }

    private void MarkProjectDirty()
    {
        if (_restoringHistory)
            return;
        if (_projectDirty)
            return;
        _projectDirty = true;
        UpdateProjectTitle();
    }

    private void UpdateProjectTitle()
    {
        var name = string.IsNullOrWhiteSpace(_currentProjectPath)
            ? "未命名"
            : Path.GetFileNameWithoutExtension(_currentProjectPath);
        Text = $"{(_projectDirty ? "*" : string.Empty)}{name} - RV室內設計";
    }

    private static bool IsProjectFileException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or
            NotSupportedException or ArgumentException;
}
