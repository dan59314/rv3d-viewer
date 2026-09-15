namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Text.Json;

internal sealed partial class RVInteriorDesignForm
{
    private const int HistoryLimit = 30;
    private readonly List<InteriorHistorySnapshot> _undoHistory = [];
    private readonly List<InteriorHistorySnapshot> _redoHistory = [];
    private bool _restoringHistory;

    private sealed record InteriorHistorySnapshot(
        string Description,
        string SerializedSession,
        Guid[] SelectedModelIds);

    private InteriorHistorySnapshot CreateHistorySnapshot(string description)
    {
        var session = InteriorDesignSessionStore.CreateHistorySession(
            _designObjects, _selectedDesignObject?.Id, _selectedMeshIndex, _hasUnexportedChanges);
        return new InteriorHistorySnapshot(
            description,
            InteriorDesignSessionStore.SerializeForHistory(session),
            _selectedModelIds.ToArray());
    }

    private void RecordUndoSnapshot(string description)
    {
        if (_restoringHistory)
            return;
        _undoHistory.Add(CreateHistorySnapshot(description));
        if (_undoHistory.Count > HistoryLimit)
            _undoHistory.RemoveAt(0);
        _redoHistory.Clear();
        MarkProjectDirty();
        UpdateHistoryMenuState();
    }

    private void UnifiedMenuStrip_UndoRequested(object? sender, EventArgs e) => UndoInteriorEdit();

    private void UnifiedMenuStrip_RedoRequested(object? sender, EventArgs e) => RedoInteriorEdit();

    private void UndoInteriorEdit()
    {
        if (_undoHistory.Count == 0)
            return;
        var target = _undoHistory[^1];
        _undoHistory.RemoveAt(_undoHistory.Count - 1);
        _redoHistory.Add(CreateHistorySnapshot(target.Description));
        RestoreHistorySnapshot(target, $"已復原：{target.Description}");
        MarkProjectDirty();
    }

    private void RedoInteriorEdit()
    {
        if (_redoHistory.Count == 0)
            return;
        var target = _redoHistory[^1];
        _redoHistory.RemoveAt(_redoHistory.Count - 1);
        _undoHistory.Add(CreateHistorySnapshot(target.Description));
        RestoreHistorySnapshot(target, $"已重做：{target.Description}");
        MarkProjectDirty();
    }

    private void RestoreHistorySnapshot(InteriorHistorySnapshot snapshot, string status)
    {
        _restoringHistory = true;
        try
        {
            var session = InteriorDesignSessionStore.DeserializeForHistory(snapshot.SerializedSession);
            var restored = InteriorDesignSessionStore.RestoreObjects(session);
            _designObjects.Clear();
            _designObjects.AddRange(restored);
            _hasUnexportedChanges = session.HasUnexportedChanges;
            _draftParameters = null;
            _draftPreviewModel = null;
            RebuildSceneTreeFromDesignObjects();
            RefreshViewportScene();
            SetSelectedModels(snapshot.SelectedModelIds, session.SelectedModelId, session.SelectedMeshIndex, true);
            parameterPropertyGrid.Refresh();
            objectPropertyGrid.Refresh();
            statusLabel.Text = status;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or NotSupportedException)
        {
            MessageBox.Show(this, $"無法還原編輯歷程：\r\n\r\n{exception.Message}",
                "RV室內設計 Undo／Redo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _restoringHistory = false;
            UpdateHistoryMenuState();
        }
    }

    private void UpdateHistoryMenuState() =>
        unifiedMenuStrip.SetEditAvailability(_undoHistory.Count > 0, _redoHistory.Count > 0);
}
