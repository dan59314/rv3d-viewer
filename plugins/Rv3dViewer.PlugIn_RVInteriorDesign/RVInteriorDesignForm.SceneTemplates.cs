namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Text.Json;

internal sealed partial class RVInteriorDesignForm
{
    private bool _sceneTemplateOperationRunning;

    private async Task RefreshSceneTemplatesAsync()
    {
        var templates = await Task.Run(InteriorSceneTemplateStore.Scan);
        if (IsDisposed || Disposing) return;
        sceneTemplatesListView.BeginUpdate();
        try
        {
            sceneTemplatesListView.Items.Clear();
            foreach (var template in templates)
            {
                var item = new ListViewItem(template.Name) { Tag = template };
                item.SubItems.Add(template.IsValid ? template.ModelCount.ToString() : "無效");
                if (!template.IsValid)
                {
                    item.ForeColor = Color.Firebrick;
                    item.ToolTipText = template.Error ?? "無效的場景範本";
                }
                sceneTemplatesListView.Items.Add(item);
            }
        }
        finally
        {
            sceneTemplatesListView.EndUpdate();
        }
        UpdateSceneTemplateButtons();
    }

    private async void SaveSceneTemplateButton_Click(object? sender, EventArgs e)
    {
        var name = sceneTemplateNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowExplanation("尚未輸入場景範本名稱。", ExplanationKind.Error);
            sceneTemplateNameTextBox.Focus();
            sceneTemplateNameTextBox.SelectAll();
            return;
        }
        if (name is "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            ShowExplanation("場景範本名稱包含無效字元，請重新輸入。", ExplanationKind.Error);
            sceneTemplateNameTextBox.Focus();
            sceneTemplateNameTextBox.SelectAll();
            return;
        }
        if (_designObjects.Count == 0)
        {
            ShowExplanation("無法加入場景範本：目前場景沒有模型。", ExplanationKind.Error);
            return;
        }
        if (MessageBox.Show(this,
                $"是否將目前場景加入範本「{name}」？",
                "加入場景範本", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1) != DialogResult.Yes)
        {
            ShowExplanation("已取消將目前場景加入範本。");
            return;
        }
        if (!ConfirmReplaceSceneTemplate(name)) return;
        try
        {
            SetSceneTemplateOperationRunning(true);
            UseWaitCursor = true;
            BeginOperationProgress($"正在建立場景範本：{name}…");
            ShowExplanation($"步驟 1/3：正在整理場景「{name}」…");
            var project = InteriorDesignProjectStore.CreateProject(
                name, _designObjects, _selectedModelIds, _selectedDesignObject?.Id, _selectedMeshIndex,
                _showModelEdges, _visibilityMode, _pbrPreviewEnabled, topViewport.Camera,
                frontViewport.Camera, rightViewport.Camera, perspectiveViewport.Camera);
            var objects = _designObjects.ToArray();
            ShowExplanation($"步驟 2/3：正在背景封裝模型與貼圖資產…");
            var progress = new Progress<int>(percent => ReportOperationProgress(percent,
                $"正在建立場景範本「{name}」：{percent}%"));
            await Task.Run(() => InteriorSceneTemplateStore.SaveCurrentAsync(name, project, objects, progress));
            sceneTemplateNameTextBox.Clear();
            ShowExplanation("步驟 3/3：正在更新場景範本清單…");
            await RefreshSceneTemplatesAsync();
            SelectSceneTemplate(name);
            ReportOperationProgress(100, $"場景範本「{name}」建立完成。");
            ShowExplanation($"已建立場景範本「{name}」；完整資料儲存於 Scenes\\{name}。 ");
        }
        catch (Exception exception) when (IsSceneTemplateException(exception))
        {
            ShowExplanation($"無法建立場景範本：{exception.Message}", ExplanationKind.Error);
        }
        finally
        {
            UseWaitCursor = false;
            SetSceneTemplateOperationRunning(false);
            EndOperationProgress();
        }
    }

    private async void ImportSceneTemplateButton_Click(object? sender, EventArgs e)
    {
        if (importSceneTemplateDialog.ShowDialog(this) != DialogResult.OK) return;
        var projectPath = Path.GetFullPath(importSceneTemplateDialog.FileName);
        var name = Path.GetFileNameWithoutExtension(projectPath).Trim();
        if (!ConfirmReplaceSceneTemplate(name)) return;
        try
        {
            SetSceneTemplateOperationRunning(true);
            UseWaitCursor = true;
            BeginOperationProgress($"正在將專案加入場景範本：{name}…");
            ShowExplanation($"步驟 1/3：正在驗證「{Path.GetFileName(projectPath)}」…");
            ShowExplanation("步驟 2/3：正在背景複製專案、模型與貼圖資產…");
            var progress = new Progress<int>(percent => ReportOperationProgress(percent,
                $"正在將專案加入場景範本「{name}」：{percent}%"));
            await Task.Run(() => InteriorSceneTemplateStore.ImportProjectAsync(projectPath, name, progress));
            ShowExplanation("步驟 3/3：正在更新場景範本清單…");
            await RefreshSceneTemplatesAsync();
            SelectSceneTemplate(name);
            ReportOperationProgress(100, $"專案已加入場景範本「{name}」。");
            ShowExplanation($"已加入場景範本「{name}」；專案與引用資產已儲存到 Scenes\\{name}。 ");
        }
        catch (Exception exception) when (IsSceneTemplateException(exception))
        {
            ShowExplanation($"無法加入場景範本：{exception.Message}", ExplanationKind.Error);
        }
        finally
        {
            UseWaitCursor = false;
            SetSceneTemplateOperationRunning(false);
            EndOperationProgress();
        }
    }

    private async void DeleteSceneTemplateButton_Click(object? sender, EventArgs e)
    {
        if (GetSelectedSceneTemplate() is not { } template) return;
        if (MessageBox.Show(this,
                $"確定刪除場景範本「{template.Name}」？\r\n\r\n將刪除其完整 Scenes 子資料夾，無法復原。",
                "刪除場景範本", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;
        try
        {
            SetSceneTemplateOperationRunning(true);
            UseWaitCursor = true;
            ShowExplanation($"正在刪除場景範本「{template.Name}」…");
            await Task.Run(() => InteriorSceneTemplateStore.Delete(template));
            if (IsDisposed || Disposing) return;
            await RefreshSceneTemplatesAsync();
            ShowExplanation($"已刪除場景範本「{template.Name}」及其完整資料夾。", ExplanationKind.Warning);
        }
        catch (Exception exception)
        {
            ShowExplanation($"無法刪除場景範本：{exception.Message}", ExplanationKind.Error);
        }
        finally
        {
            if (!IsDisposed && !Disposing)
            {
                UseWaitCursor = false;
                SetSceneTemplateOperationRunning(false);
            }
        }
    }

    private async void LoadSceneTemplateButton_Click(object? sender, EventArgs e)
    {
        if (GetSelectedSceneTemplate() is not { IsValid: true, ProjectPath: { } projectPath } template) return;
        var action = _designObjects.Count == 0
            ? DialogResult.Yes
            : MessageBox.Show(this,
                $"如何載入場景範本「{template.Name}」？\r\n\r\n是：取代目前場景\r\n否：合併加入目前場景",
                "載入場景範本", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
        if (action == DialogResult.Cancel) return;
        if (action == DialogResult.Yes && !ConfirmSaveProjectChanges()) return;
        try
        {
            SetSceneTemplateOperationRunning(true);
            UseWaitCursor = true;
            BeginOperationProgress($"正在載入場景範本：{template.Name}…");
            ShowExplanation($"步驟 1/3：正在背景讀取場景範本「{template.Name}」與模型資產…");
            await Task.Yield();
            var loadProgress = new Progress<int>(percent => ReportOperationProgress(10 + percent * 65 / 100,
                $"正在載入場景範本「{template.Name}」的模型：{percent}%"));
            var loadResult = await Task.Run(async () =>
            {
                var cachedProjectPath = InteriorSceneTemplateStore.PrepareLoadProjectPath(template);
                var cacheReady = InteriorSceneTemplateStore.IsGeometryCacheReady(cachedProjectPath);
                var loadedProject = await InteriorDesignProjectStore.LoadAsync(cachedProjectPath,
                    progress: loadProgress);
                return (LoadedProject: loadedProject, CachedProjectPath: cachedProjectPath,
                    GeometryCacheReady: cacheReady);
            });
            if (IsDisposed || Disposing) return;
            if (!loadResult.GeometryCacheReady)
            {
                ReportOperationProgress(78, $"正在建立場景範本「{template.Name}」的快速載入快取…");
                await Task.Run(async () =>
                {
                    await InteriorDesignProjectStore.SaveAsync(loadResult.LoadedProject.Project,
                        loadResult.CachedProjectPath);
                    InteriorSceneTemplateStore.MarkGeometryCacheReady(loadResult.CachedProjectPath);
                });
            }
            var loaded = loadResult.LoadedProject;
            ReportOperationProgress(82, $"正在套用場景範本「{template.Name}」…");
            ShowExplanation($"步驟 2/3：正在建立 {loaded.Objects.Count} 個場景模型並更新視圖…");
            SuspendLayout();
            if (action == DialogResult.Yes)
            {
                ApplyLoadedProject(loaded, projectPath);
                _currentProjectPath = null;
                _projectDirty = true;
                UpdateProjectTitle();
            }
            else
            {
                RecordUndoSnapshot($"合併場景範本「{template.Name}」");
                var addedIds = new List<Guid>();
                foreach (var source in loaded.Objects)
                {
                    var id = Guid.NewGuid();
                    source.Model.Id = id;
                    _designObjects.Add(new ParametricDesignObject
                    {
                        Id = id,
                        Parameters = source.Parameters.CopyDefinition(),
                        Model = source.Model,
                        IsDefaultSceneObject = false
                    });
                    addedIds.Add(id);
                }
                RebuildSceneTreeFromDesignObjects();
                RefreshViewportScene();
                SetSelectedModels(addedIds, addedIds.Count > 0 ? addedIds[0] : null, null, true);
                _hasUnexportedChanges = true;
                MarkProjectDirty();
            }
            ResumeLayout(true);
            ReportOperationProgress(92, $"場景已套用，正在保存工作階段…");
            ShowExplanation("步驟 3/3：正在背景保存工作階段…");
            var session = InteriorDesignSessionStore.CreateSession(
                _designObjects, _selectedDesignObject?.Id, _selectedMeshIndex, _hasUnexportedChanges);
            session.ProjectFilePath = _currentProjectPath;
            session.ProjectDirty = _projectDirty;
            await Task.Run(() => InteriorDesignSessionStore.Save(session));
            ReportOperationProgress(100, $"場景範本「{template.Name}」載入完成。");
            ShowExplanation(action == DialogResult.Yes
                ? $"已由範本「{template.Name}」建立新的未命名場景，共 {_designObjects.Count} 個模型。"
                : $"已將範本「{template.Name}」的 {loaded.Objects.Count} 個模型合併至目前場景。");
        }
        catch (Exception exception) when (IsSceneTemplateException(exception))
        {
            ShowExplanation($"無法載入場景範本：{exception.Message}", ExplanationKind.Error);
        }
        finally
        {
            if (IsHandleCreated)
                ResumeLayout(true);
            UseWaitCursor = false;
            SetSceneTemplateOperationRunning(false);
            EndOperationProgress();
        }
    }

    private void SceneTemplatesListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateSceneTemplateButtons();
        if (GetSelectedSceneTemplate() is { IsValid: false } invalid)
            ShowExplanation($"場景範本「{invalid.Name}」無效：{invalid.Error}", ExplanationKind.Error);
    }

    private void UpdateSceneTemplateButtons()
    {
        var selected = GetSelectedSceneTemplate();
        deleteSceneTemplateButton.Enabled = !_sceneTemplateOperationRunning && selected is not null;
        loadSceneTemplateButton.Enabled = !_sceneTemplateOperationRunning && selected?.IsValid == true;
    }

    private void SetSceneTemplateOperationRunning(bool running)
    {
        _sceneTemplateOperationRunning = running;
        sceneTemplateNameTextBox.Enabled = !running;
        saveSceneTemplateButton.Enabled = !running;
        importSceneTemplateButton.Enabled = !running;
        sceneTemplatesListView.Enabled = !running;
        UpdateSceneTemplateButtons();
    }

    private InteriorSceneTemplateDescriptor? GetSelectedSceneTemplate() =>
        sceneTemplatesListView.SelectedItems.Count == 1
            ? sceneTemplatesListView.SelectedItems[0].Tag as InteriorSceneTemplateDescriptor
            : null;

    private void SelectSceneTemplate(string name)
    {
        foreach (ListViewItem item in sceneTemplatesListView.Items)
        {
            if (item.Tag is not InteriorSceneTemplateDescriptor template ||
                !string.Equals(template.Name, name, StringComparison.CurrentCultureIgnoreCase)) continue;
            item.Selected = true;
            item.EnsureVisible();
            break;
        }
    }

    private bool ConfirmReplaceSceneTemplate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowExplanation("請先輸入有效的場景範本名稱。", ExplanationKind.Error);
            return false;
        }
        var exists = sceneTemplatesListView.Items.Cast<ListViewItem>()
            .Any(item => item.Tag is InteriorSceneTemplateDescriptor template &&
                         string.Equals(template.Name, name, StringComparison.CurrentCultureIgnoreCase));
        return !exists || MessageBox.Show(this,
            $"場景範本「{name}」已存在，是否以新資料夾內容覆寫？",
            "覆寫場景範本", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    private static bool IsSceneTemplateException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or
            NotSupportedException or ArgumentException or InvalidOperationException;
}
