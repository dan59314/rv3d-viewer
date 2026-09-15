namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Runtime.InteropServices;
using Rv3dViewer.Core;

internal sealed partial class RVInteriorDesignForm
{
    private void FileMenuItem_DropDownOpening(object? sender, EventArgs e)
    {
        exportAllGlbMenuItem.Enabled = HasExportableGeometry(_designObjects);
        exportSelectedGlbMenuItem.Enabled = HasExportableGeometry(
            _designObjects.Where(item => _selectedModelIds.Contains(item.Id)));
    }

    private async void ExportAllGlbMenuItem_Click(object? sender, EventArgs e) =>
        await ExportGlbAsync(selectedOnly: false);

    private async void ExportSelectedGlbMenuItem_Click(object? sender, EventArgs e) =>
        await ExportGlbAsync(selectedOnly: true);

    private async Task ExportGlbAsync(bool selectedOnly)
    {
        var objects = selectedOnly
            ? _designObjects.Where(item => _selectedModelIds.Contains(item.Id)).ToArray()
            : _designObjects.ToArray();
        if (objects.Length == 0)
        {
            MessageBox.Show(this,
                selectedOnly ? "目前沒有選取可匯出的模型。" : "目前場景沒有可匯出的模型。",
                "匯出 GLB", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!HasExportableGeometry(objects))
        {
            MessageBox.Show(this, "指定範圍沒有可輸出的可見 Mesh。", "匯出 GLB",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var projectName = string.IsNullOrWhiteSpace(_currentProjectPath)
            ? "RV室內設計"
            : Path.GetFileNameWithoutExtension(_currentProjectPath);
        saveGlbDialog.FileName = selectedOnly
            ? $"{projectName}-選取模型.glb"
            : $"{projectName}.glb";
        if (saveGlbDialog.ShowDialog(this) != DialogResult.OK)
            return;

        var outputPath = Path.GetFullPath(saveGlbDialog.FileName);
        try
        {
            UseWaitCursor = true;
            statusLabel.Text = selectedOnly
                ? $"正在匯出選取的 {objects.Length} 個模型至 GLB…"
                : $"正在匯出全部 {objects.Length} 個模型至 GLB…";
            var objectIds = objects.Select(item => item.Id).ToHashSet();
            foreach (var item in objects)
                InteriorMeshGeometry.Normalize(item.Model);
            var primaryId = _selectedDesignObject is not null && objectIds.Contains(_selectedDesignObject.Id)
                ? _selectedDesignObject.Id
                : objects[0].Id;
            var project = InteriorDesignProjectStore.CreateProject(
                projectName, objects, objectIds, primaryId,
                objects.Length == 1 && primaryId == _selectedDesignObject?.Id ? _selectedMeshIndex : null,
                _showModelEdges, _visibilityMode, _pbrPreviewEnabled, topViewport.Camera,
                frontViewport.Camera, rightViewport.Camera, perspectiveViewport.Camera);
            project.ProjectFilePath = _currentProjectPath;

            await InteriorGlbExportService.ExportAsync(project, outputPath);
            var fileSize = new FileInfo(outputPath).Length;
            statusLabel.Text = $"GLB 匯出完成：{Path.GetFileName(outputPath)}，{objects.Length} 個模型。";
            MessageBox.Show(this,
                $"GLB 匯出完成。\r\n\r\n模型數量：{objects.Length}\r\n" +
                $"檔案大小：{FormatFileSize(fileSize)}\r\n檔案位置：{outputPath}",
                "匯出 GLB", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                           InvalidOperationException or ArgumentException or ExternalException)
        {
            statusLabel.Text = "GLB 匯出失敗。";
            MessageBox.Show(this, $"無法匯出 GLB：\r\n\r\n{exception.Message}", "匯出 GLB",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private static bool HasExportableGeometry(IEnumerable<ParametricDesignObject> objects) =>
        objects.Any(item => item.Model.IsVisible && SceneTraversal.GetMeshInstances(item.Model).Any(instance =>
            !item.Model.HiddenMeshIndices.Contains(instance.MeshIndex) &&
            (uint)instance.MeshIndex < (uint)item.Model.Meshes.Count &&
            item.Model.Meshes[instance.MeshIndex].Positions.Length > 0 &&
            item.Model.Meshes[instance.MeshIndex].Indices.Length > 0));

    private static string FormatFileSize(long bytes) => bytes >= 1024L * 1024L
        ? $"{bytes / (1024d * 1024d):0.##} MB"
        : $"{Math.Max(1d, bytes / 1024d):0.##} KB";
}
