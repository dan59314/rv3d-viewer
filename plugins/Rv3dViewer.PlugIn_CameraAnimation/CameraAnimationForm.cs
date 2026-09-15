using System.Diagnostics;
using System.ComponentModel;
using System.Numerics;
using Rv3dViewer.Core;
using Rv3dViewer.Plugin.Abstractions;
using Rv3dViewer.Plugin.WinForms;

namespace Rv3dViewer.CameraAnimationPlugin;

public partial class CameraAnimationForm : Form
{
    private const double DefaultKeyframeStepSeconds = 1d;
    private readonly IPluginContext _context;
    private readonly CameraState _mainCameraAtOpen;
    private readonly List<SceneLight> _mainLightsAtOpen;
    private readonly List<AnimationModelState> _mainModelsAtOpen;
    private readonly Stopwatch _playbackClock = new();
    private CameraAnimationDocument _document = new();
    private ViewerProject? _isolatedProject;
    private IPluginIsolatedSceneService? IsolatedSceneService => _context as IPluginIsolatedSceneService;
    private ViewerProject SceneProject => _isolatedProject ?? _context.Project;
    private string? _filePath;
    private string? _lastAnimationFilePath;
    private CameraAnimationUserSettings _userSettings = new();
    private bool _initialFileLoadAttempted;
    private CameraState _previewCamera;
    private readonly PreviewCameraProperties _previewProperties;
    private Point _previewLastMouse;
    private bool _previewMouseCaptured;
    private bool _previewDragChanged;
    private PointF _orthographicLastMouse;
    private MouseButtons _orthographicDragButton;
    private bool _orthographicDragChanged;
    private CameraEditorView? _activeOrthographicView;
    private bool _previewRenderDirty;
    private bool _previewRenderRunning;
    private int _previewRenderRevision;
    private CancellationTokenSource? _previewRenderCancellation;
    private double _playbackStartSeconds;
    private double _currentSeconds;
    private bool _updatingUi;
    private CancellationTokenSource? _exportCancellation;
    private bool _shuttingDown;
    private bool _isDirty;
    private bool _allowClose;
    private bool _closeSaveInProgress;
    private float _frontViewScale = 1f;
    private float _leftViewScale = 1f;
    private float _topViewScale = 1f;
    private Vector2 _frontViewOffset;
    private Vector2 _leftViewOffset;
    private Vector2 _topViewOffset;
    private readonly Dictionary<CameraEditorView, IReadOnlyList<CameraEditorPoint>> _projectedEditControls = [];
    private readonly Dictionary<CameraEditorView, CameraEditorPoint> _projectedPositionControls = [];
    private readonly Dictionary<CameraEditorView, CameraEditorPoint> _projectedTargetControls = [];
    private readonly Dictionary<CameraEditorView, (CameraEditorPoint Position, CameraEditorPoint Target)> _projectedCameraPoints = [];
    private readonly Dictionary<CameraEditorView, Vector2> _editorWorldUnitsPerPixel = [];
    private readonly Dictionary<CameraEditorView, Vector3> _editorProjectionCameraFrom = [];
    private readonly Dictionary<CameraEditorView, float> _editorProjectionCameraDistance = [];
    private OrthographicDragTarget _orthographicDragTarget;
    private Guid? _draftKeyframeId;
    private Vector3? _draftPositionControl;
    private Vector3? _draftTargetControl;
    private bool _hasDraftEdits;
    private readonly List<EditorHistoryState> _undoHistory = [];
    private readonly List<EditorHistoryState> _redoHistory = [];
    private bool _restoringHistory;
    private SceneLight? _selectedLight;
    private readonly Dictionary<CameraEditorView, List<ProjectedLightControl>> _projectedLightControls = [];
    private EditorHistoryState? _pendingDragHistory;
    private ProjectedLightControl? _activeLightControl;
    private Vector3 _activeLightControlWorld;
    private readonly Dictionary<CameraEditorView, Bitmap> _editorBaseImages = [];
    private long _lastDragRenderTicks;
    private const int DragRenderIntervalMilliseconds = 100;
    private bool _perspectiveDragPreviewOnly;
    private bool _perspectiveWheelEditPending;
    private long _lastPerspectiveWheelTicks;
    private const int WheelEditCompletionMilliseconds = 200;
    private bool _closing;
    private bool _applyingAnimatedLights;
    private long _lastPlaybackOverlayTicks;
    private SceneModel? _selectedModel;
    private bool _rebuildingSceneTree;
    private Guid? _draftModelKeyframeId;
    private Guid? _draftModelId;
    private Vector3? _draftModelPosition;
    private Vector3? _draftModelControl;
    private readonly Dictionary<CameraEditorView, ProjectedModelControl> _projectedModelControls = [];
    private readonly Dictionary<CameraEditorView, List<ProjectedModelBounds>> _projectedModelBounds = [];
    private ProjectedModelControl? _projectedPerspectiveModelControl;
    private bool _previewModelDrag;
    private bool _previewModelControlDrag;

    public CameraAnimationForm(IPluginContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mainCameraAtOpen = CloneCamera(_context.Camera.Current);
        _mainLightsAtOpen = SceneProject.Lights.Select(CloneLight).ToList();
        _mainModelsAtOpen = CaptureModels();
        _previewCamera = CloneCamera(_context.Camera.Current);
        _previewProperties = new PreviewCameraProperties(_previewCamera);
        InitializeComponent();
        InitializeUnifiedMenu();
        cameraPropertyGrid.SelectedObject = _previewProperties;
        RebuildLightList();
        RebuildSceneTree();
        UpdateHistoryButtons();
        previewRenderTimer.Start();
        _context.ProjectChanged += Context_ProjectChanged;
        _context.PreviewModeChanged += Context_PreviewModeChanged;
        ViewportColorPreferences.Changed += ViewportColorPreferences_Changed;
        SyncDocumentToUi();
        RequestPreviewRender();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Z)) { UndoButton_Click(this, EventArgs.Empty); return true; }
        if (keyData == (Keys.Control | Keys.Y)) { RedoButton_Click(this, EventArgs.Empty); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void PushEditorHistory(string description)
    {
        if (_restoringHistory) return;
        _undoHistory.Add(CaptureEditorState(description));
        if (_undoHistory.Count > 100) _undoHistory.RemoveAt(0);
        _redoHistory.Clear();
        UpdateHistoryButtons();
    }

    private void PushKeyframePropertyHistory(CameraKeyframe selected, PropertyValueChangedEventArgs e)
    {
        if (_restoringHistory) return;
        var state = CaptureEditorState("編輯關鍵影格");
        var oldFrame = state.Document.Keyframes.FirstOrDefault(frame => frame.Id == selected.Id);
        var descriptor = e.ChangedItem?.PropertyDescriptor;
        if (oldFrame is not null && descriptor is not null && !descriptor.IsReadOnly)
        {
            try { descriptor.SetValue(oldFrame, e.OldValue); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { }
        }
        _undoHistory.Add(state);
        if (_undoHistory.Count > 100) _undoHistory.RemoveAt(0);
        _redoHistory.Clear();
        UpdateHistoryButtons();
    }

    private EditorHistoryState CaptureEditorState(string description) => new(
        description,
        CloneDocument(_document),
        CloneCamera(_previewCamera),
        SceneProject.Lights.Select(CloneLight).ToList(),
        CaptureModels(),
        (keyframeListBox.SelectedItem as CameraKeyframe)?.Id,
        SceneProject.Lights.IndexOf(_selectedLight!),
        _selectedModel?.Id);

    private void UndoButton_Click(object? sender, EventArgs e)
    {
        if (_undoHistory.Count == 0) return;
        var state = _undoHistory[^1];
        _undoHistory.RemoveAt(_undoHistory.Count - 1);
        _redoHistory.Add(CaptureEditorState(state.Description));
        RestoreEditorState(state);
    }

    private void RedoButton_Click(object? sender, EventArgs e)
    {
        if (_redoHistory.Count == 0) return;
        var state = _redoHistory[^1];
        _redoHistory.RemoveAt(_redoHistory.Count - 1);
        _undoHistory.Add(CaptureEditorState(state.Description));
        RestoreEditorState(state);
    }

    private void RestoreEditorState(EditorHistoryState state)
    {
        _restoringHistory = true;
        try
        {
            _document = CloneDocument(state.Document);
            SceneProject.Lights = state.Lights.Select(CloneLight).ToList();
            RestoreProjectModels(state.Models);
            SyncDocumentToUi();
            RebuildKeyframeList();
            if (state.SelectedKeyframeId is Guid id)
                keyframeListBox.SelectedItem = _document.Keyframes.FirstOrDefault(frame => frame.Id == id);
            RebuildLightList(state.SelectedLightIndex);
            RebuildSceneTree(state.SelectedModelId);
            SetPreviewCamera(CloneCamera(state.Camera));
            _context.InvalidateScene();
            _isDirty = true;
            RequestPreviewRender();
        }
        finally
        {
            _restoringHistory = false;
            UpdateHistoryButtons();
        }
    }

    private void UpdateHistoryButtons()
    {
        undoButton.Enabled = _undoHistory.Count > 0;
        redoButton.Enabled = _redoHistory.Count > 0;
        unifiedMenuStrip.SetEditAvailability(undoButton.Enabled, redoButton.Enabled);
        RefreshUnifiedMenuCommandStates();
        undoButton.Text = _undoHistory.Count == 0 ? "Undo" : $"Undo ({_undoHistory.Count})";
        redoButton.Text = _redoHistory.Count == 0 ? "Redo" : $"Redo ({_redoHistory.Count})";
    }

    private static CameraAnimationDocument CloneDocument(CameraAnimationDocument source) => new()
    {
        Version = source.Version,
        Name = source.Name,
        DurationSeconds = source.DurationSeconds,
        FramesPerSecond = source.FramesPerSecond,
        Loop = source.Loop,
        Keyframes = source.Keyframes.Select(CloneKeyframe).ToList()
    };

    private static SceneLight CloneLight(SceneLight source) => new()
    {
        Name = source.Name,
        Enabled = source.Enabled,
        Type = source.Type,
        Position = source.Position,
        Direction = source.Direction,
        Color = source.Color,
        Intensity = source.Intensity,
        Range = source.Range,
        FallInDegrees = source.FallInDegrees,
        FallOffDegrees = source.FallOffDegrees
    };

    private static void CopyLight(SceneLight source, SceneLight target)
    {
        target.Name = source.Name; target.Enabled = source.Enabled; target.Type = source.Type;
        target.Position = source.Position; target.Direction = source.Direction; target.Color = source.Color;
        target.Intensity = source.Intensity; target.Range = source.Range;
        target.FallInDegrees = source.FallInDegrees; target.FallOffDegrees = source.FallOffDegrees;
        target.Validate();
    }

    private List<AnimationModelState> CaptureModels() => SceneProject.Models.Select(model => new AnimationModelState
    {
        ModelId = model.Id,
        Name = model.Name,
        Visible = model.IsVisible,
        Position = model.Transform.Position,
        RotationDegrees = model.Transform.RotationDegrees,
        Scale = model.Transform.Scale
    }).ToList();

    private static AnimationModelState CloneAnimationModel(AnimationModelState source) => new()
    {
        ModelId = source.ModelId, Name = source.Name, Visible = source.Visible,
        Position = source.Position, RotationDegrees = source.RotationDegrees, Scale = source.Scale,
        PositionControl = source.PositionControl
    };

    private void RestoreProjectModels(IEnumerable<AnimationModelState> states)
    {
        foreach (var state in states)
        {
            var model = SceneProject.Models.FirstOrDefault(candidate => candidate.Id == state.ModelId);
            if (model is null) continue;
            model.Name = state.Name;
            model.IsVisible = state.Visible;
            model.Transform.Position = state.Position;
            model.Transform.RotationDegrees = state.RotationDegrees;
            model.Transform.Scale = state.Scale;
        }
        _context.InvalidateScene();
    }

    private void ApplyAnimatedModels(double timeSeconds)
    {
        var states = CameraAnimationEvaluator.EvaluateModels(_document, timeSeconds);
        if (states is null) return;
        RestoreProjectModels(states);
        objectPropertyGrid.Refresh();
    }

    private void RestoreMainSceneState()
    {
        _context.Camera.Apply(CloneCamera(_mainCameraAtOpen), CameraApplyMode.TransientPreview);
        if (_context.Project.Lights.Count == _mainLightsAtOpen.Count)
            for (var index = 0; index < _mainLightsAtOpen.Count; index++)
                CopyLight(_mainLightsAtOpen[index], _context.Project.Lights[index]);
        else
            _context.Project.Lights = _mainLightsAtOpen.Select(CloneLight).ToList();
        foreach (var state in _mainModelsAtOpen)
        {
            var model = _context.Project.Models.FirstOrDefault(candidate => candidate.Id == state.ModelId);
            if (model is null) continue;
            model.IsVisible = state.Visible; model.Transform.Position = state.Position;
            model.Transform.RotationDegrees = state.RotationDegrees; model.Transform.Scale = state.Scale;
        }
        _context.InvalidateScene();
    }

    private void CommitCurrentStateToMainForm()
    {
        ApplyAnimatedLights(_currentSeconds);
        ApplyAnimatedModels(_currentSeconds);
        if (_isolatedProject is not null)
        {
            _context.Project.Lights = _isolatedProject.Lights.Select(CloneLight).ToList();
            foreach (var source in _isolatedProject.Models)
            {
                var target = _context.Project.Models.FirstOrDefault(model => model.Id == source.Id);
                if (target is null) continue;
                target.IsVisible = source.IsVisible;
                target.Transform.Position = source.Transform.Position;
                target.Transform.RotationDegrees = source.Transform.RotationDegrees;
                target.Transform.Scale = source.Transform.Scale;
            }
        }
        _context.Camera.Apply(CloneCamera(_previewCamera), CameraApplyMode.Commit);
        _context.NotifySceneChanged();
    }

    private List<AnimationLightState> CaptureLights(IReadOnlyList<AnimationLightState>? previous = null) =>
        SceneProject.Lights.Select((light, index) => new AnimationLightState
        {
            Id = previous?.FirstOrDefault(state => string.Equals(state.Name, light.Name, StringComparison.Ordinal))?.Id
                 ?? previous?.ElementAtOrDefault(index)?.Id
                 ?? _document.Keyframes.Select(frame => frame.Lights?.ElementAtOrDefault(index)?.Id)
                     .FirstOrDefault(id => id.HasValue)
                 ?? Guid.NewGuid(),
            Name = light.Name, Enabled = light.Enabled, Type = light.Type,
            Position = light.Position, Direction = light.Direction, Color = light.Color,
            Intensity = light.Intensity, Range = light.Range,
            FallInDegrees = light.FallInDegrees, FallOffDegrees = light.FallOffDegrees
        }).ToList();

    private void ApplyAnimatedLights(double timeSeconds)
    {
        var states = CameraAnimationEvaluator.EvaluateLights(_document, timeSeconds);
        if (states is null) return;
        for (var index = 0; index < states.Count; index++)
        {
            var state = states[index];
            var light = SceneProject.Lights.ElementAtOrDefault(index)
                        ?? SceneProject.Lights.FirstOrDefault(candidate =>
                            string.Equals(candidate.Name, state.Name, StringComparison.Ordinal));
            if (light is null) continue;
            light.Enabled = state.Enabled; light.Type = state.Type;
            light.Position = state.Position; light.Direction = state.Direction;
            light.Color = state.Color; light.Intensity = state.Intensity; light.Range = state.Range;
            light.FallInDegrees = state.FallInDegrees; light.FallOffDegrees = state.FallOffDegrees;
            light.Validate();
        }
        _context.InvalidateScene();
        lightPropertyGrid.Refresh();
    }

    private void RestoreProjectLights(IReadOnlyList<SceneLight> lights)
    {
        SceneProject.Lights = lights.Select(CloneLight).ToList();
        _applyingAnimatedLights = true;
        try { _context.InvalidateScene(); }
        finally { _applyingAnimatedLights = false; }
        RebuildLightList();
    }

    private void RebuildSceneTree(Guid? selectedModelId = null)
    {
        selectedModelId ??= _selectedModel?.Id;
        _rebuildingSceneTree = true;
        sceneTreeView.BeginUpdate();
        try
        {
            sceneTreeView.Nodes.Clear();
            foreach (var model in SceneProject.Models)
            {
                var modelNode = new TreeNode(model.Name) { Tag = model, Checked = model.IsVisible };
                var meshGroup = new TreeNode("Meshes");
                for (var index = 0; index < model.Meshes.Count; index++)
                    meshGroup.Nodes.Add(new TreeNode(model.Meshes[index].Name)
                    {
                        Tag = new AnimationMeshTreeItem(model, index)
                    });
                var materialGroup = new TreeNode("Materials");
                foreach (var material in model.Materials)
                    materialGroup.Nodes.Add(new TreeNode(material.Name) { Tag = material });
                modelNode.Nodes.Add(meshGroup);
                modelNode.Nodes.Add(materialGroup);
                sceneTreeView.Nodes.Add(modelNode);
                if (model.Id == selectedModelId) sceneTreeView.SelectedNode = modelNode;
            }
        }
        finally
        {
            sceneTreeView.EndUpdate();
            _rebuildingSceneTree = false;
        }
    }

    private void SceneTreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        _selectedModel = e.Node?.Tag switch
        {
            SceneModel model => model,
            AnimationMeshTreeItem mesh => mesh.Model,
            _ => _selectedModel
        };
        BindSelectedModel();
        RequestPreviewRender();
    }

    private void SceneTreeView_AfterCheck(object? sender, TreeViewEventArgs e)
    {
        if (_rebuildingSceneTree || e.Action == TreeViewAction.Unknown || e.Node?.Tag is not SceneModel model) return;
        PushEditorHistory("變更模型顯示");
        model.IsVisible = e.Node.Checked;
        CommitModelsToSelectedKeyframe();
        _context.InvalidateScene();
        RequestPreviewRender();
    }

    private void BindSelectedModel()
    {
        objectPropertyGrid.SelectedObject = _selectedModel is null
            ? null
            : new AnimationModelProperties(_selectedModel, () => PushEditorHistory("編輯模型"));
        objectPropertyGrid.Enabled = _selectedModel is not null && keyframeListBox.SelectedItem is not null;
    }

    private void ObjectPropertyGrid_PropertyValueChanged(object? sender, PropertyValueChangedEventArgs e)
    {
        if (_selectedModel is null) return;
        CommitModelsToSelectedKeyframe();
        RebuildSceneTree(_selectedModel.Id);
        BindSelectedModel();
        _context.InvalidateScene();
        RequestPreviewRender();
    }

    private void CommitModelsToSelectedKeyframe()
    {
        if (editModeComboBox.SelectedIndex != 3 ||
            keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        var captured = CaptureModels();
        foreach (var model in captured)
            model.PositionControl = selected.Models?.FirstOrDefault(old => old.ModelId == model.ModelId)?.PositionControl;
        var sceneModelIds = SceneProject.Models.Select(model => model.Id).ToHashSet();
        var unresolved = selected.Models?.Where(model => !sceneModelIds.Contains(model.ModelId))
            .Select(CloneAnimationModel) ?? [];
        selected.Models = captured.Concat(unresolved).ToList();
        MarkDocumentDirty();
    }

    private void RebuildLightList(int? selectedIndex = null)
    {
        var lights = SceneProject.Lights;
        var index = selectedIndex ?? (_selectedLight is null ? 0 : lights.IndexOf(_selectedLight));
        lightComboBox.BeginUpdate();
        lightComboBox.Items.Clear();
        lightComboBox.Items.AddRange(lights.Cast<object>().ToArray());
        lightComboBox.DisplayMember = nameof(SceneLight.Name);
        lightComboBox.EndUpdate();
        if (lights.Count > 0) lightComboBox.SelectedIndex = Math.Clamp(index, 0, lights.Count - 1);
        else { _selectedLight = null; lightPropertyGrid.SelectedObject = null; }
    }

    private void LightComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectedLight = lightComboBox.SelectedItem as SceneLight;
        var lightInspector = _selectedLight is null
            ? null
            : new AnimationLightProperties(_selectedLight, () => PushEditorHistory("編輯燈光"));
        if (lightInspector is not null) TypeDescriptor.Refresh(lightInspector);
        lightPropertyGrid.SelectedObject = lightInspector;
        RequestPreviewRender();
    }

    private void LightPropertyGrid_PropertyValueChanged(object? sender, PropertyValueChangedEventArgs e)
    {
        _selectedLight?.Validate();
        CommitLightsToSelectedKeyframe();
        lightPropertyGrid.Refresh();
        _context.InvalidateScene();
        _isDirty = true;
        RequestPreviewRender();
    }

    private void CommitLightsToSelectedKeyframe()
    {
        if (editModeComboBox.SelectedIndex != 2 ||
            keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        foreach (var light in SceneProject.Lights) light.Validate();
        selected.Lights = CaptureLights(selected.Lights);
        MarkDocumentDirty();
    }

    private void NewButton_Click(object? sender, EventArgs e)
    {
        StopPlayback(restoreCamera: true);
        ClearDraftEdit();
        _document = new CameraAnimationDocument();
        _filePath = null;
        _currentSeconds = 0d;
        SyncDocumentToUi();
        SetDirty(false);
        _undoHistory.Clear(); _redoHistory.Clear(); UpdateHistoryButtons();
    }

    private async void OpenButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Camera 動畫 (*.camera-animation.json)|*.camera-animation.json|JSON (*.json)|*.json|所有檔案 (*.*)|*.*",
            Title = "開啟 Camera 動畫",
            InitialDirectory = GetLastAnimationDirectory(),
            FileName = GetLastAnimationFileName()
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await LoadAnimationAsync(dialog.FileName, showErrorMessage: true);
    }

    private async void ImportProjectScene_Click(object? sender, EventArgs e)
    {
        if (IsolatedSceneService is null) return;
        using var dialog = new OpenFileDialog
        {
            Filter = "Rv3d Viewer 專案 (*.rv3dproj)|*.rv3dproj",
            Title = "輸入 Rv3dPrj 場景檔案"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            UseWaitCursor = true;
            var loaded = await IsolatedSceneService.LoadProjectAsync(dialog.FileName);
            var previous = _isolatedProject;
            _isolatedProject = loaded;
            var choice = ConfirmModelTracksForCurrentScene(_document);
            if (choice == DialogResult.Cancel) { _isolatedProject = previous; return; }
            if (choice == DialogResult.Yes)
                foreach (var frame in _document.Keyframes) frame.Models = null;
            SetPreviewCamera(CloneCamera(loaded.Camera));
            RefreshImportedScene();
            if (choice == DialogResult.Yes) MarkDocumentDirty();
            _context.SetStatus($"Camera Animation 已輸入場景：{Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException)
        {
            _context.ShowMessage($"無法輸入場景：{ex.Message}", "Camera 運鏡動畫", PluginMessageKind.Error);
        }
        finally { UseWaitCursor = false; }
    }

    private async void ImportGlbScene_Click(object? sender, EventArgs e)
    {
        if (IsolatedSceneService is null) return;
        using var dialog = new OpenFileDialog { Filter = "GLB 模型 (*.glb)|*.glb", Title = "輸入 GLB 模型" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            UseWaitCursor = true;
            var imported = await IsolatedSceneService.ImportGlbAsync(dialog.FileName);
            var choice = MessageBox.Show(this, "是否清除目前場景中的模型？\n\n是：清除後加入 GLB\n否：保留並加入 GLB\n取消：不輸入",
                "輸入 GLB 模型", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (choice == DialogResult.Cancel) return;
            _isolatedProject ??= IsolatedSceneService.CloneCurrentProject();
            if (choice == DialogResult.Yes) _isolatedProject.Models.Clear();
            _isolatedProject.Models.Add(imported);
            RefreshImportedScene(imported.Id);
            _context.SetStatus($"Camera Animation 已輸入 GLB：{Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException)
        {
            _context.ShowMessage($"無法輸入 GLB：{ex.Message}", "Camera 運鏡動畫", PluginMessageKind.Error);
        }
        finally { UseWaitCursor = false; }
    }

    private void RefreshImportedScene(Guid? selectedModelId = null)
    {
        _selectedModel = selectedModelId is Guid id ? SceneProject.Models.FirstOrDefault(model => model.Id == id) :
            SceneProject.Models.FirstOrDefault();
        RebuildSceneTree(selectedModelId);
        RebuildLightList();
        BindSelectedModel();
        ClearModelDraftEdit();
        RequestPreviewRender();
    }

    private async Task<bool> LoadAnimationAsync(string path, bool showErrorMessage)
    {
        try
        {
            var loadedDocument = await CameraAnimationSerializer.LoadAsync(path);
            var modelLoadChoice = ConfirmModelTracksForCurrentScene(loadedDocument);
            if (modelLoadChoice == DialogResult.Cancel) return false;
            var clearedModelTracks = modelLoadChoice == DialogResult.Yes;
            if (clearedModelTracks)
                foreach (var keyframe in loadedDocument.Keyframes)
                    keyframe.Models = null;

            StopPlayback(restoreCamera: true);
            ClearDraftEdit();
            ClearModelDraftEdit();
            _document = loadedDocument;
            _filePath = path;
            await RememberAnimationPathAsync(path);
            _currentSeconds = 0d;
            SyncDocumentToUi();
            SetDirty(clearedModelTracks);
            _undoHistory.Clear(); _redoHistory.Clear(); UpdateHistoryButtons();
            _context.SetStatus(clearedModelTracks
                ? $"已開啟 Camera 動畫並清除不相符的模型資料：{Path.GetFileName(path)}"
                : $"已開啟 Camera 動畫：{Path.GetFileName(path)}");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
        {
            if (showErrorMessage)
                _context.ShowMessage($"無法開啟 Camera 動畫：{ex.Message}", "Camera 運鏡動畫", PluginMessageKind.Error);
            else
                _context.SetStatus($"無法自動載入最後 Camera 動畫：{ex.Message}");
            return false;
        }
    }

    private DialogResult ConfirmModelTracksForCurrentScene(CameraAnimationDocument document)
    {
        var animationModels = document.Keyframes
            .SelectMany(frame => frame.Models ?? [])
            .GroupBy(model => model.ModelId)
            .Select(group => group.First())
            .ToArray();
        if (animationModels.Length == 0) return DialogResult.No;

        var sceneModels = SceneProject.Models;
        var exactMatch = animationModels.Length == sceneModels.Count &&
            animationModels.All(animationModel => sceneModels.Any(sceneModel =>
                sceneModel.Id == animationModel.ModelId &&
                string.Equals(sceneModel.Name, animationModel.Name, StringComparison.Ordinal)));
        if (exactMatch) return DialogResult.No;

        return MessageBox.Show(this,
            "動畫檔案中的模型與目前場景不一致。\n\n" +
            "選擇「是」：清除動畫內的模型資料，但保留 Camera、燈光及時間軸。\n" +
            "選擇「否」：保留模型資料為未連結軌跡，不套用到目前模型。\n" +
            "選擇「取消」：取消載入並維持目前狀態。\n\n" +
            "清除只會修改目前載入的內容；儲存前不會覆寫原檔案。",
            "模型資料不一致",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);
    }

    private async void SaveButton_Click(object? sender, EventArgs e) => await SaveAsync(forceChoosePath: false);

    private async void SaveAsButton_Click(object? sender, EventArgs e) => await SaveAsync(forceChoosePath: true);

    private async Task<bool> SaveAsync(bool forceChoosePath)
    {
        var path = forceChoosePath ? null : _filePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Camera 動畫 (*.camera-animation.json)|*.camera-animation.json",
                DefaultExt = "camera-animation.json",
                AddExtension = true,
                FileName = SuggestFileName(),
                InitialDirectory = GetLastAnimationDirectory(),
                Title = "儲存 Camera 動畫"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return false;
            path = dialog.FileName;
        }

        try
        {
            ReadDocumentSettings();
            await CameraAnimationSerializer.SaveAsync(_document, path);
            _filePath = path;
            await RememberAnimationPathAsync(path);
            SetDirty(false);
            _context.SetStatus($"已儲存 Camera 動畫：{Path.GetFileName(path)}");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _context.ShowMessage($"無法儲存 Camera 動畫：{ex.Message}", "Camera 運鏡動畫", PluginMessageKind.Error);
            return false;
        }
    }

    private void CaptureButton_Click(object? sender, EventArgs e)
    {
        PushEditorHistory("加入關鍵影格");
        var keyframe = CameraKeyframe.Capture(_previewCamera, _currentSeconds,
            $"關鍵影格 {_document.Keyframes.Count + 1}");
        keyframe.Lights = CaptureLights(_document.Keyframes.LastOrDefault()?.Lights);
        keyframe.Models = CaptureModels();
        _document.Keyframes.Add(keyframe);
        MarkDocumentDirty();
        SortAndSelect(keyframe);
        AdvanceTimeMarkerAfterKeyframe();
    }

    private void AdvanceTimeMarkerAfterKeyframe()
    {
        var nextSeconds = Math.Min(3600d, _currentSeconds + DefaultKeyframeStepSeconds);
        if (nextSeconds > _document.DurationSeconds)
        {
            _document.DurationSeconds = nextSeconds;
            _updatingUi = true;
            durationNumericUpDown.Value = (decimal)nextSeconds;
            _updatingUi = false;
        }
        SetCurrentTime(nextSeconds, preview: true);
    }

    private void InsertKeyframeButton_Click(object? sender, EventArgs e)
    {
        if (keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        if (!CanInsertBefore(selected)) return;
        PushEditorHistory("插入關鍵影格");
        var plan = CameraKeyframeInsertionPlan.Create(_document, selected);
        var camera = CameraAnimationEvaluator.Evaluate(_document, plan.TimeSeconds);
        var inserted = CameraKeyframe.Capture(
            camera, plan.TimeSeconds, $"插入影格 {_document.Keyframes.Count + 1}");
        inserted.Lights = CameraAnimationEvaluator.EvaluateLights(_document, plan.TimeSeconds)?
            .Select(CloneAnimationLight).ToList() ?? CaptureLights();
        inserted.Models = CameraAnimationEvaluator.EvaluateModels(_document, plan.TimeSeconds)?
            .Select(CloneAnimationModel).ToList() ?? CaptureModels();
        SplitModelBezierControls(selected, inserted, plan.TimeSeconds);
        inserted.Easing = plan.Easing;
        _document.Keyframes.Add(inserted);
        ResetBezierControls(selected);
        MarkDocumentDirty();
        SortAndSelect(inserted);
        SetCurrentTime(plan.TimeSeconds, preview: true);
    }

    private void UpdateKeyframeButton_Click(object? sender, EventArgs e)
        => CommitSelectedKeyframe();

    private void CommitSelectedKeyframe()
    {
        if (keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        if (_pendingDragHistory is null) PushEditorHistory("更新關鍵影格");
        var captured = CameraKeyframe.Capture(_previewCamera, selected.TimeSeconds, selected.Name);
        captured.Id = selected.Id;
        captured.Easing = selected.Easing;
        captured.PositionControl = _draftKeyframeId == selected.Id ? _draftPositionControl : selected.PositionControl;
        captured.TargetControl = _draftKeyframeId == selected.Id ? _draftTargetControl : selected.TargetControl;
        captured.Lights = CaptureLights(selected.Lights);
        captured.Models = CaptureModels().Select(model =>
        {
            var old = selected.Models?.FirstOrDefault(candidate => candidate.ModelId == model.ModelId);
            model.PositionControl = old?.PositionControl;
            if (_draftModelKeyframeId == selected.Id && _draftModelId == model.ModelId)
            {
                if (_draftModelPosition is Vector3 position) model.Position = position;
                model.PositionControl = _draftModelControl;
            }
            return model;
        }).ToList();
        var index = _document.Keyframes.IndexOf(selected);
        _document.Keyframes[index] = captured;
        ClearDraftEdit();
        ClearModelDraftEdit();
        MarkDocumentDirty();
        SortAndSelect(captured);
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        PushEditorHistory("刪除關鍵影格");
        var index = _document.Keyframes.IndexOf(selected);
        var next = _document.Keyframes.ElementAtOrDefault(index + 1);
        _document.Keyframes.Remove(selected);
        if (next is not null) ResetBezierControls(next);
        MarkDocumentDirty();
        RebuildKeyframeList();
        if (_document.Keyframes.Count > 0)
            keyframeListBox.SelectedIndex = Math.Clamp(index, 0, _document.Keyframes.Count - 1);
    }

    private void KeyframeListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_updatingUi) return;
        ClearDraftEdit();
        ClearModelDraftEdit();
        var selected = keyframeListBox.SelectedItem as CameraKeyframe;
        keyframePropertyGrid.SelectedObject = selected;
        updateKeyframeButton.Enabled = selected is not null;
        deleteButton.Enabled = selected is not null;
        insertKeyframeButton.Enabled = CanInsertBefore(selected);
        _updatingUi = true;
        keyframeTimeNumericUpDown.Enabled = selected is not null;
        if (selected is not null) keyframeTimeNumericUpDown.Value = (decimal)selected.TimeSeconds;
        _updatingUi = false;
        if (selected is null) return;
        SetPreviewCamera(selected.ToCameraState());
        SetCurrentTime(selected.TimeSeconds, preview: true);
        BindSelectedModel();
    }

    private void KeyframePropertyGrid_PropertyValueChanged(object? sender, PropertyValueChangedEventArgs e)
    {
        if (keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        PushKeyframePropertyHistory(selected, e);
        if (e.ChangedItem?.PropertyDescriptor?.Name == nameof(CameraKeyframe.TimeSeconds) &&
            e.OldValue is double oldTime)
        {
            ShiftKeyframeAndFollowing(selected, oldTime, selected.TimeSeconds);
        }
        else
        {
            selected.Validate(_document.DurationSeconds);
        }
        MarkDocumentDirty();
        SortAndSelect(selected);
        SetPreviewCamera(selected.ToCameraState());
        SetCurrentTime(selected.TimeSeconds, preview: true);
    }

    private void KeyframeTimeNumericUpDown_ValueChanged(object? sender, EventArgs e)
    {
        if (_updatingUi || keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        PushEditorHistory("修改關鍵影格時間");
        ShiftKeyframeAndFollowing(selected, selected.TimeSeconds, (double)keyframeTimeNumericUpDown.Value);
        MarkDocumentDirty();
        SortAndSelect(selected);
        SetCurrentTime(selected.TimeSeconds, preview: true);
    }

    private void ShiftKeyframeAndFollowing(CameraKeyframe selected, double oldTime, double requestedTime)
    {
        var selectedIndex = _document.Keyframes.IndexOf(selected);
        if (selectedIndex < 0) return;
        var previousTime = selectedIndex == 0 ? 0d : _document.Keyframes[selectedIndex - 1].TimeSeconds;
        var lastTime = selectedIndex == _document.Keyframes.Count - 1
            ? oldTime
            : _document.Keyframes[^1].TimeSeconds;
        if (!double.IsFinite(requestedTime)) requestedTime = oldTime;
        var newTime = Math.Clamp(requestedTime, previousTime, 3600d - (lastTime - oldTime));
        var delta = newTime - oldTime;
        selected.TimeSeconds = oldTime;
        for (var index = selectedIndex; index < _document.Keyframes.Count; index++)
            _document.Keyframes[index].TimeSeconds += delta;

        var updatedLastTime = _document.Keyframes[^1].TimeSeconds;
        if (updatedLastTime > _document.DurationSeconds)
        {
            _document.DurationSeconds = updatedLastTime;
            _updatingUi = true;
            durationNumericUpDown.Value = (decimal)updatedLastTime;
            _updatingUi = false;
        }
        foreach (var keyframe in _document.Keyframes) keyframe.Validate(_document.DurationSeconds);
    }

    private void TimelineTrackBar_Scroll(object? sender, EventArgs e)
    {
        if (_updatingUi) return;
        StopPlayback(restoreCamera: false);
        var duration = (double)durationNumericUpDown.Value;
        SetCurrentTime(duration * timelineTrackBar.Value / timelineTrackBar.Maximum, preview: true);
    }

    private void DurationNumericUpDown_ValueChanged(object? sender, EventArgs e)
    {
        if (_updatingUi) return;
        PushEditorHistory("修改動畫長度");
        _document.DurationSeconds = (double)durationNumericUpDown.Value;
        foreach (var keyframe in _document.Keyframes) keyframe.Validate(_document.DurationSeconds);
        SetCurrentTime(Math.Min(_currentSeconds, _document.DurationSeconds), preview: false);
        RebuildKeyframeList();
        MarkDocumentDirty();
    }

    private void FpsNumericUpDown_ValueChanged(object? sender, EventArgs e)
    {
        if (!_updatingUi)
        {
            PushEditorHistory("修改 FPS");
            _document.FramesPerSecond = (int)fpsNumericUpDown.Value;
            MarkDocumentDirty();
        }
    }

    private void LoopCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (!_updatingUi)
        {
            PushEditorHistory("修改循環設定");
            _document.Loop = loopCheckBox.Checked;
            MarkDocumentDirty();
        }
    }

    private void PlayButton_Click(object? sender, EventArgs e)
    {
        if (_document.Keyframes.Count == 0)
        {
            _context.ShowMessage("請先擷取至少一個關鍵影格。", "Camera 運鏡動畫", PluginMessageKind.Warning);
            return;
        }
        if (_currentSeconds >= _document.DurationSeconds) SetCurrentTime(0d, preview: false);
        _playbackStartSeconds = _currentSeconds;
        _playbackClock.Restart();
        playbackTimer.Start();
        UpdatePlaybackButtons();
    }

    private void PauseButton_Click(object? sender, EventArgs e)
    {
        if (!playbackTimer.Enabled) return;
        PlaybackTimer_Tick(sender, e);
        playbackTimer.Stop();
        _playbackClock.Stop();
        UpdatePlaybackButtons();
    }

    private void StopButton_Click(object? sender, EventArgs e)
    {
        StopPlayback(restoreCameraCheckBox.Checked);
        SetCurrentTime(0d, preview: true);
    }

    private async void ExportFramesButton_Click(object? sender, EventArgs e)
    {
        if (_document.Keyframes.Count == 0)
        {
            _context.ShowMessage("請先擷取至少一個關鍵影格。", "Camera 運鏡動畫", PluginMessageKind.Warning);
            return;
        }

        ReadDocumentSettings();
        _document.Validate();
        var plan = CameraFrameSequencePlan.Create(_document.DurationSeconds, _document.FramesPerSecond);
        if (plan.FrameCount > 10_000 && !_context.Confirm(
                $"將輸出 {plan.FrameCount:N0} 張 PNG，可能需要較長時間與大量磁碟空間。是否繼續？",
                "大量影格輸出"))
            return;

        using var folderDialog = new FolderBrowserDialog
        {
            Description = "選擇 PNG 影格序列輸出資料夾",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            SelectedPath = SuggestOutputDirectory()
        };
        if (folderDialog.ShowDialog(this) != DialogResult.OK) return;

        var outputDirectory = folderDialog.SelectedPath;
        var existingCount = Enumerable.Range(0, plan.FrameCount)
            .Select(index => Path.Combine(outputDirectory, plan.GetFileName(index)))
            .Count(File.Exists);
        if (existingCount > 0 && !_context.Confirm(
                $"輸出資料夾已有 {existingCount:N0} 個同名影格，繼續將覆寫這些檔案。是否繼續？",
                "覆寫 PNG 影格"))
            return;

        StopPlayback(restoreCamera: true);
        using var cancellation = new CancellationTokenSource();
        _exportCancellation = cancellation;
        SetExporting(true, plan.FrameCount);
        var completed = false;
        var finalStatus = "就緒";
        var lightsBeforeExport = SceneProject.Lights.Select(CloneLight).ToList();
        var modelsBeforeExport = CaptureModels();

        try
        {
            Directory.CreateDirectory(outputDirectory);
            var width = (int)exportWidthNumericUpDown.Value;
            var height = (int)exportHeightNumericUpDown.Value;
            for (var frameIndex = 0; frameIndex < plan.FrameCount; frameIndex++)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                var time = plan.GetTimeSeconds(frameIndex);
                var camera = CameraAnimationEvaluator.Evaluate(_document, time);
                ApplyAnimatedLights(time);
                ApplyAnimatedModels(time);
                var outputPath = Path.Combine(outputDirectory, plan.GetFileName(frameIndex));
                var png = await RenderScenePreviewAsync(
                    camera, width, height, useQuickPreview: false, cancellationToken: cancellation.Token);
                await File.WriteAllBytesAsync(outputPath, png, cancellation.Token);

                exportProgressBar.Value = frameIndex + 1;
                exportStatusLabel.Text = $"{frameIndex + 1:N0} / {plan.FrameCount:N0}";
                _context.SetStatus($"正在輸出 Camera 動畫影格 {frameIndex + 1:N0}/{plan.FrameCount:N0}");
                await Task.Yield();
            }

            _context.SetStatus($"Camera 動畫影格輸出完成：{outputDirectory}");
            completed = true;
            finalStatus = "輸出完成";
        }
        catch (OperationCanceledException)
        {
            _context.SetStatus("Camera 動畫影格輸出已取消；已完成的 PNG 會保留。");
            finalStatus = "已取消，已完成影格保留";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or
                                      ArgumentException or System.Runtime.InteropServices.ExternalException)
        {
            _context.ShowMessage(
                $"PNG 影格輸出失敗：{ex.Message}\n已完成的影格會保留。",
                "Camera 動畫輸出",
                PluginMessageKind.Error);
            finalStatus = "輸出失敗";
        }
        finally
        {
            RestoreProjectLights(lightsBeforeExport);
            RestoreProjectModels(modelsBeforeExport);
            if (ReferenceEquals(_exportCancellation, cancellation)) _exportCancellation = null;
            if (!IsDisposed)
            {
                SetExporting(false, plan.FrameCount);
                exportStatusLabel.Text = finalStatus;
            }
        }

        if (completed && !IsDisposed && _context.Confirm(
                $"已輸出 {plan.FrameCount:N0} 張 PNG。\n{outputDirectory}\n\n是否開啟輸出資料夾？",
                "Camera 動畫輸出完成"))
            OpenOutputDirectory(outputDirectory);
    }

    private async void ExportVideoButton_Click(object? sender, EventArgs e)
    {
        if (_document.Keyframes.Count == 0)
        {
            _context.ShowMessage("請先加入至少一個關鍵影格。", "Camera 運鏡動畫", PluginMessageKind.Warning);
            return;
        }

        var ffmpeg = FfmpegVideoEncoder.FindExecutable();
        if (ffmpeg is null)
        {
            _context.ShowMessage("找不到 FFmpeg。請安裝 FFmpeg 並將其 bin 資料夾加入 PATH。",
                "無法輸出影片", PluginMessageKind.Error);
            return;
        }

        var width = (int)exportWidthNumericUpDown.Value;
        var height = (int)exportHeightNumericUpDown.Value;
        if (width % 2 != 0 || height % 2 != 0)
        {
            _context.ShowMessage("H.264 MP4 的寬度與高度必須是偶數。", "Camera 動畫輸出", PluginMessageKind.Warning);
            return;
        }

        ReadDocumentSettings();
        _document.Validate();
        var plan = CameraFrameSequencePlan.Create(_document.DurationSeconds, _document.FramesPerSecond);
        using var dialog = new SaveFileDialog
        {
            Filter = "MP4 影片 (*.mp4)|*.mp4",
            DefaultExt = "mp4",
            AddExtension = true,
            FileName = SuggestVideoFileName(),
            InitialDirectory = SuggestOutputDirectory(),
            Title = "輸出 Camera 動畫影片"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        using var cancellation = new CancellationTokenSource();
        _exportCancellation = cancellation;
        SetExporting(true, plan.FrameCount);
        var completed = false;
        var finalStatus = "就緒";
        var lightsBeforeExport = SceneProject.Lights.Select(CloneLight).ToList();
        var modelsBeforeExport = CaptureModels();
        try
        {
            await using var encoder = FfmpegVideoEncoder.Start(ffmpeg, dialog.FileName, _document.FramesPerSecond);
            for (var frameIndex = 0; frameIndex < plan.FrameCount; frameIndex++)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                var time = plan.GetTimeSeconds(frameIndex);
                var camera = CameraAnimationEvaluator.Evaluate(_document, time);
                ApplyAnimatedLights(time);
                ApplyAnimatedModels(time);
                var png = await RenderScenePreviewAsync(
                    camera, width, height, useQuickPreview: false, cancellationToken: cancellation.Token);
                await encoder.WriteFrameAsync(png, cancellation.Token);
                exportProgressBar.Value = frameIndex + 1;
                exportStatusLabel.Text = $"編碼 {frameIndex + 1:N0} / {plan.FrameCount:N0}";
                _context.SetStatus($"正在輸出 Camera 動畫影片 {frameIndex + 1:N0}/{plan.FrameCount:N0}");
                await Task.Yield();
            }
            await encoder.CompleteAsync(cancellation.Token);
            completed = true;
            finalStatus = "影片輸出完成";
            _context.SetStatus($"Camera 動畫影片輸出完成：{dialog.FileName}");
        }
        catch (OperationCanceledException)
        {
            finalStatus = "影片輸出已取消";
            _context.SetStatus("Camera 動畫影片輸出已取消；不完整的影片檔可能會保留。");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or
                                      ArgumentException or System.Runtime.InteropServices.ExternalException)
        {
            finalStatus = "影片輸出失敗";
            _context.ShowMessage($"影片輸出失敗：{ex.Message}", "Camera 動畫輸出", PluginMessageKind.Error);
        }
        finally
        {
            RestoreProjectLights(lightsBeforeExport);
            RestoreProjectModels(modelsBeforeExport);
            if (ReferenceEquals(_exportCancellation, cancellation)) _exportCancellation = null;
            if (!IsDisposed)
            {
                SetExporting(false, plan.FrameCount);
                exportStatusLabel.Text = finalStatus;
            }
        }

        if (completed && !IsDisposed && _context.Confirm(
                $"影片輸出完成：\n{dialog.FileName}\n\n是否開啟輸出資料夾？", "Camera 動畫輸出完成"))
            OpenOutputDirectory(Path.GetDirectoryName(dialog.FileName)!);
    }

    private void CancelExportButton_Click(object? sender, EventArgs e)
    {
        cancelExportButton.Enabled = false;
        exportStatusLabel.Text = "正在取消…";
        _exportCancellation?.Cancel();
    }

    private void PlaybackTimer_Tick(object? sender, EventArgs e)
    {
        var time = _playbackStartSeconds + _playbackClock.Elapsed.TotalSeconds;
        if (time >= _document.DurationSeconds)
        {
            if (_document.Loop)
            {
                time %= _document.DurationSeconds;
                _playbackStartSeconds = time;
                _playbackClock.Restart();
            }
            else
            {
                SetCurrentTime(_document.DurationSeconds, preview: true);
                StopPlayback(restoreCameraCheckBox.Checked);
                return;
            }
        }
        SetCurrentTime(time, preview: true);
    }

    private void StopPlayback(bool restoreCamera)
    {
        playbackTimer.Stop();
        _playbackClock.Reset();
        UpdatePlaybackButtons();
        foreach (var (view, pictureBox) in GetOrthographicPictureBoxes())
            RedrawEditorOverlay(pictureBox, view);
        RequestPreviewRender();
    }

    private void SetCurrentTime(double seconds, bool preview)
    {
        _currentSeconds = Math.Clamp(seconds, 0d, _document.DurationSeconds);
        _updatingUi = true;
        timelineTrackBar.Value = (int)Math.Round(timelineTrackBar.Maximum * _currentSeconds / _document.DurationSeconds);
        currentTimeLabel.Text = $"{_currentSeconds:0.000} / {_document.DurationSeconds:0.000} 秒";
        _updatingUi = false;
        if (preview) PreviewCurrentTime();
    }

    private void PreviewCurrentTime()
    {
        if (_document.Keyframes.Count == 0) return;
        SetPreviewCamera(CameraAnimationEvaluator.Evaluate(_document, _currentSeconds));
        ApplyAnimatedLights(_currentSeconds);
        ApplyAnimatedModels(_currentSeconds);
        var now = Environment.TickCount64;
        if (!playbackTimer.Enabled || now - _lastPlaybackOverlayTicks >= 33)
        {
            _lastPlaybackOverlayTicks = now;
            foreach (var (view, pictureBox) in GetOrthographicPictureBoxes())
                RedrawEditorOverlay(pictureBox, view);
        }
    }

    private void SortAndSelect(CameraKeyframe selected)
    {
        _document.Keyframes = _document.Keyframes.OrderBy(frame => frame.TimeSeconds).ThenBy(frame => frame.Id).ToList();
        RebuildKeyframeList();
        keyframeListBox.SelectedItem = selected;
    }

    private void RebuildKeyframeList()
    {
        var selectedId = (keyframeListBox.SelectedItem as CameraKeyframe)?.Id;
        _updatingUi = true;
        keyframeListBox.BeginUpdate();
        keyframeListBox.Items.Clear();
        keyframeListBox.Items.AddRange(_document.Keyframes.Cast<object>().ToArray());
        keyframeListBox.EndUpdate();
        if (selectedId is Guid id)
            keyframeListBox.SelectedItem = _document.Keyframes.FirstOrDefault(frame => frame.Id == id);
        _updatingUi = false;
        keyframePropertyGrid.SelectedObject = keyframeListBox.SelectedItem;
        updateKeyframeButton.Enabled = keyframeListBox.SelectedItem is not null;
        deleteButton.Enabled = keyframeListBox.SelectedItem is not null;
        insertKeyframeButton.Enabled = CanInsertBefore(keyframeListBox.SelectedItem as CameraKeyframe);
        var selected = keyframeListBox.SelectedItem as CameraKeyframe;
        _updatingUi = true;
        keyframeTimeNumericUpDown.Enabled = selected is not null;
        if (selected is not null) keyframeTimeNumericUpDown.Value = (decimal)selected.TimeSeconds;
        _updatingUi = false;
    }

    private bool CanInsertBefore(CameraKeyframe? selected)
    {
        if (selected is null) return false;
        var ordered = _document.Keyframes.OrderBy(frame => frame.TimeSeconds).ThenBy(frame => frame.Id).ToArray();
        var index = Array.IndexOf(ordered, selected);
        return index > 0 && selected.TimeSeconds - ordered[index - 1].TimeSeconds > double.Epsilon;
    }

    private void SyncDocumentToUi()
    {
        _updatingUi = true;
        durationNumericUpDown.Value = (decimal)_document.DurationSeconds;
        keyframeTimeNumericUpDown.Maximum = 3600M;
        fpsNumericUpDown.Value = _document.FramesPerSecond;
        loopCheckBox.Checked = _document.Loop;
        _updatingUi = false;
        RebuildKeyframeList();
        SetCurrentTime(_currentSeconds, preview: false);
        UpdatePlaybackButtons();
        UpdateTitle();
        if (_document.Keyframes.Count > 0) PreviewCurrentTime();
    }

    private void ReadDocumentSettings()
    {
        _document.DurationSeconds = (double)durationNumericUpDown.Value;
        _document.FramesPerSecond = (int)fpsNumericUpDown.Value;
        _document.Loop = loopCheckBox.Checked;
    }

    private void UpdatePlaybackButtons()
    {
        playButton.Enabled = !playbackTimer.Enabled;
        pauseButton.Enabled = playbackTimer.Enabled;
        stopButton.Enabled = playbackTimer.Enabled || _playbackClock.Elapsed > TimeSpan.Zero;
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_initialFileLoadAttempted) return;
        _initialFileLoadAttempted = true;
        try
        {
            _userSettings = await CameraAnimationUserSettings.LoadAsync();
            _lastAnimationFilePath = _userSettings.LastAnimationFilePath;
            _updatingUi = true;
            showCameraCheckBox.Checked = _userSettings.ShowCamera;
            showTrajectoryCheckBox.Checked = _userSettings.ShowTrajectory;
            showAllLightsCheckBox.Checked = _userSettings.ShowAllLights;
            showModelTrajectoryCheckBox.Checked = _userSettings.ShowModelTrajectory;
            _updatingUi = false;
            SyncUnifiedMenuDisplayState();
            if (!string.IsNullOrWhiteSpace(_lastAnimationFilePath) && File.Exists(_lastAnimationFilePath))
                await LoadAnimationAsync(_lastAnimationFilePath, showErrorMessage: false);
            RequestPreviewRender();
        }
        catch (Exception ex) when (ex is not StackOverflowException and not OutOfMemoryException)
        {
            _updatingUi = false;
            _context.ShowMessage($"Camera Animation 初始化失敗：{ex.GetBaseException().Message}",
                "Camera 運鏡動畫", PluginMessageKind.Error);
        }
    }

    private async Task RememberAnimationPathAsync(string path)
    {
        _lastAnimationFilePath = Path.GetFullPath(path);
        _userSettings.LastAnimationFilePath = _lastAnimationFilePath;
        try
        {
            await _userSettings.SaveAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _context.SetStatus($"Camera 動畫已存取，但無法記住最後檔案：{ex.Message}");
        }
    }

    private string GetLastAnimationDirectory()
    {
        var directory = string.IsNullOrWhiteSpace(_lastAnimationFilePath)
            ? null
            : Path.GetDirectoryName(_lastAnimationFilePath);
        return !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)
            ? directory
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private string GetLastAnimationFileName() =>
        string.IsNullOrWhiteSpace(_lastAnimationFilePath) ? string.Empty : Path.GetFileName(_lastAnimationFilePath);

    private void MarkDocumentDirty()
    {
        SetDirty(true);
        RequestPreviewRender();
    }

    private void SetDirty(bool dirty)
    {
        _isDirty = dirty;
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        var dirtyMarker = _isDirty ? "*" : string.Empty;
        Text = string.IsNullOrWhiteSpace(_filePath)
            ? $"{dirtyMarker}Camera 運鏡動畫"
            : $"{dirtyMarker}Camera 運鏡動畫 — {Path.GetFileName(_filePath)}";
    }

    private string SuggestFileName()
    {
        var projectPath = SceneProject.ProjectFilePath;
        var name = string.IsNullOrWhiteSpace(projectPath)
            ? SceneProject.Name
            : Path.GetFileNameWithoutExtension(projectPath);
        foreach (var invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
        return $"{name}.camera-animation.json";
    }

    private string SuggestOutputDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_filePath))
            return Path.Combine(Path.GetDirectoryName(_filePath)!, Path.GetFileNameWithoutExtension(_filePath) + "-frames");
        if (!string.IsNullOrWhiteSpace(SceneProject.ProjectFilePath))
            return Path.Combine(Path.GetDirectoryName(SceneProject.ProjectFilePath)!,
                Path.GetFileNameWithoutExtension(SceneProject.ProjectFilePath) + "-camera-frames");
        return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    }

    private string SuggestVideoFileName() => Path.GetFileNameWithoutExtension(SuggestFileName()) + ".mp4";

    private void OpenOutputDirectory(string directory)
    {
        try
        {
            Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            _context.ShowMessage($"無法開啟輸出資料夾：{ex.Message}",
                "Camera 動畫輸出", PluginMessageKind.Warning);
        }
    }

    private void SetExporting(bool exporting, int frameCount)
    {
        commandFlowLayoutPanel.Enabled = !exporting;
        editorSplitContainer.Enabled = !exporting;
        playbackFlowLayoutPanel.Enabled = !exporting;
        timelineTrackBar.Enabled = !exporting;
        exportFramesButton.Enabled = !exporting;
        exportVideoButton.Enabled = !exporting;
        exportWidthNumericUpDown.Enabled = !exporting;
        exportHeightNumericUpDown.Enabled = !exporting;
        cancelExportButton.Enabled = exporting;
        exportProgressBar.Minimum = 0;
        exportProgressBar.Maximum = Math.Max(1, frameCount);
        if (!exporting)
        {
            exportProgressBar.Value = 0;
            exportStatusLabel.Text = "就緒";
        }
    }

    private void CameraPropertyGrid_PropertyValueChanged(object? sender, PropertyValueChangedEventArgs e)
    {
        PushEditorHistory("編輯 Camera");
        try
        {
            _previewCamera = _previewProperties.ToCameraState();
            _previewCamera.Validate();
            _previewProperties.SetFrom(_previewCamera);
            cameraPropertyGrid.Refresh();
            MarkDraftEdit();
            RequestPreviewRender();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _context.ShowMessage($"Camera 參數無效：{ex.Message}", "Camera 運鏡動畫", PluginMessageKind.Warning);
            _previewProperties.SetFrom(_previewCamera);
            cameraPropertyGrid.Refresh();
        }
    }

    private void UseMainCameraButton_Click(object? sender, EventArgs e)
    {
        PushEditorHistory("載入主視角");
        SetPreviewCamera(CloneCamera(_context.Camera.Current));
        MarkDraftEdit();
    }

    private void ApplyMainCameraButton_Click(object? sender, EventArgs e)
    {
        _context.Camera.Apply(CloneCamera(_previewCamera), CameraApplyMode.Commit);
        _context.SetStatus("已將 Camera 動畫 Preview 視角套用至主視角。");
    }

    private void PreviewPictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button is not (MouseButtons.Left or MouseButtons.Middle or MouseButtons.Right)) return;
        _previewLastMouse = e.Location;
        _previewMouseCaptured = true;
        _previewDragChanged = false;
        previewPictureBox.Capture = true;
        previewPictureBox.Focus();
        _previewModelControlDrag = false;
        _previewModelDrag = editModeComboBox.SelectedIndex == 3 && e.Button == MouseButtons.Left &&
                            TryBeginPerspectiveModelEdit(e.Location);
        if (_previewModelDrag) EnsureModelDraft();
        _pendingDragHistory = CaptureEditorState(_previewModelDrag ? "編輯模型" : "編輯 Camera");
    }

    private void PreviewPictureBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_previewMouseCaptured) return;
        var dx = e.X - _previewLastMouse.X;
        var dy = e.Y - _previewLastMouse.Y;
        _previewLastMouse = e.Location;
        if (dx == 0 && dy == 0) return;

        if (_previewModelDrag && _selectedModel is not null)
        {
            EnsureModelDraft();
            var forward = Vector3.Normalize(_previewCamera.To - _previewCamera.From);
            var right = Vector3.Normalize(Vector3.Cross(forward, _previewCamera.Up));
            var up = Vector3.Normalize(Vector3.Cross(right, forward));
            var dragWorld = _previewModelControlDrag
                ? _draftModelControl ?? _selectedModel.Transform.Position
                : _draftModelPosition ?? _selectedModel.Transform.Position;
            var distance = Math.Max(Vector3.Distance(_previewCamera.From, dragWorld), 0.1f);
            var scale = 2f * distance * MathF.Tan(_previewCamera.FieldOfViewDegrees * MathF.PI / 360f) /
                        Math.Max(1, previewPictureBox.ClientSize.Height);
            var delta = right * (dx * scale) - up * (dy * scale);
            if (_previewModelControlDrag)
                _draftModelControl += delta;
            else
            {
                _draftModelPosition += delta;
                if (_draftModelControl is not null) _draftModelControl += delta * 0.5f;
                _selectedModel.Transform.Position = _draftModelPosition!.Value;
            }
            objectPropertyGrid.Refresh();
            _context.InvalidateScene();
        }
        else if (e.Button == MouseButtons.Left)
            CameraController.Orbit(_previewCamera, -dx * 0.01f, dy * 0.01f);
        else if (e.Button == MouseButtons.Middle)
        {
            var distance = Math.Max(Vector3.Distance(_previewCamera.From, _previewCamera.To), 0.001f);
            CameraController.PanTarget(_previewCamera, -dx * distance * 0.0015f, dy * distance * 0.0015f);
        }
        else if (e.Button == MouseButtons.Right)
        {
            var distance = Math.Max(Vector3.Distance(_previewCamera.From, _previewCamera.To), 0.001f);
            CameraController.Pan(_previewCamera, -dx * distance * 0.0015f, dy * distance * 0.0015f);
        }
        else return;

        _previewDragChanged = true;
        _perspectiveDragPreviewOnly = true;
        if (_previewModelDrag) RequestDragPreviewRender();
        else PreviewCameraChangedByNavigation();
    }

    private void PreviewPictureBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button is not (MouseButtons.Left or MouseButtons.Middle or MouseButtons.Right)) return;
        _previewMouseCaptured = false;
        previewPictureBox.Capture = false;
        if (_previewDragChanged && _previewModelDrag) CommitModelDraft();
        if (_previewDragChanged && editModeComboBox.SelectedIndex == 1) CommitSelectedKeyframe();
        if (_previewDragChanged) CommitPendingDragHistory();
        else _pendingDragHistory = null;
        _perspectiveDragPreviewOnly = false;
        _previewModelDrag = false;
        _previewModelControlDrag = false;
        if (_previewDragChanged) RequestPreviewRender();
        _previewDragChanged = false;
    }

    private bool TryBeginPerspectiveModelEdit(Point location)
    {
        var imagePoint = ToImagePoint(previewPictureBox, location);
        const float controlRadiusSquared = 18f * 18f;
        if (_projectedPerspectiveModelControl?.BezierControl is CameraEditorPoint control &&
            DistanceSquared(control, imagePoint) <= controlRadiusSquared)
        {
            _selectedModel = _projectedPerspectiveModelControl.Model;
            _previewModelControlDrag = true;
            EnsureModelDraft();
            return true;
        }

        var candidates = SceneProject.Models
            .Where(model => model.IsVisible)
            .Select(model => TryGetPerspectiveModelBounds(model, out var bounds)
                ? new ProjectedModelBounds(model, bounds)
                : null)
            .Where(item => item is not null && item.Bounds.Contains(imagePoint))
            .OrderBy(item => item!.Bounds.Width * item.Bounds.Height)
            .ToArray();
        if (candidates.Length == 0) return false;
        _selectedModel = candidates[0]!.Model;
        SelectModelTreeNode(_selectedModel.Id);
        BindSelectedModel();
        inspectorTabControl.SelectedTab = objectTabPage;
        EnsureModelDraft();
        RequestPreviewRender();
        return true;
    }

    private CameraEditorPoint? ProjectPerspectivePoint(Vector3 world)
    {
        var forward = Vector3.Normalize(_previewCamera.To - _previewCamera.From);
        var right = Vector3.Normalize(Vector3.Cross(forward, _previewCamera.Up));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var relative = world - _previewCamera.From;
        var depth = Vector3.Dot(relative, forward);
        if (depth <= 0.001f) return null;
        var height = Math.Max(1, previewPictureBox.Image?.Height ?? previewPictureBox.ClientSize.Height);
        var width = Math.Max(1, previewPictureBox.Image?.Width ?? previewPictureBox.ClientSize.Width);
        var halfHeight = depth * MathF.Tan(_previewCamera.FieldOfViewDegrees * MathF.PI / 360f);
        var halfWidth = halfHeight * width / height;
        var x = width * 0.5f * (1f + Vector3.Dot(relative, right) / Math.Max(halfWidth, 0.0001f));
        var y = height * 0.5f * (1f - Vector3.Dot(relative, up) / Math.Max(halfHeight, 0.0001f));
        return new CameraEditorPoint(x, y);
    }

    private bool TryGetPerspectiveModelBounds(SceneModel model, out RectangleF rectangle)
    {
        rectangle = RectangleF.Empty;
        if (!SceneTraversal.TryCalculateBounds(model, out var bounds)) return false;
        var points = GetBoundsCorners(bounds).Select(ProjectPerspectivePoint)
            .Where(point => point is not null).Cast<CameraEditorPoint>().ToArray();
        return TryCreateScreenBounds(points, out rectangle);
    }

    private bool TryGetOrthographicModelBounds(SceneModel model, CameraEditorView view, out RectangleF rectangle)
    {
        rectangle = RectangleF.Empty;
        if (!SceneTraversal.TryCalculateBounds(model, out var bounds)) return false;
        return TryCreateScreenBounds(GetBoundsCorners(bounds).Select(point => ProjectWorldPoint(view, point)),
            out rectangle);
    }

    private static IEnumerable<Vector3> GetBoundsCorners(SceneBounds bounds)
    {
        for (var x = 0; x <= 1; x++)
        for (var y = 0; y <= 1; y++)
        for (var z = 0; z <= 1; z++)
            yield return new Vector3(
                x == 0 ? bounds.Minimum.X : bounds.Maximum.X,
                y == 0 ? bounds.Minimum.Y : bounds.Maximum.Y,
                z == 0 ? bounds.Minimum.Z : bounds.Maximum.Z);
    }

    private static bool TryCreateScreenBounds(IEnumerable<CameraEditorPoint> points, out RectangleF rectangle)
    {
        var values = points.ToArray();
        if (values.Length == 0)
        {
            rectangle = RectangleF.Empty;
            return false;
        }
        var minimumX = values.Min(point => point.X);
        var minimumY = values.Min(point => point.Y);
        var maximumX = values.Max(point => point.X);
        var maximumY = values.Max(point => point.Y);
        rectangle = RectangleF.FromLTRB(minimumX, minimumY, maximumX, maximumY);
        if (rectangle.Width < 8f) rectangle.Inflate((8f - rectangle.Width) * 0.5f, 0f);
        if (rectangle.Height < 8f) rectangle.Inflate(0f, (8f - rectangle.Height) * 0.5f);
        return true;
    }

    private void DrawPerspectiveModelOverlay()
    {
        _projectedPerspectiveModelControl = null;
        if (editModeComboBox.SelectedIndex != 3 || _selectedModel is null ||
            previewPictureBox.Image is not Bitmap bitmap) return;
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        if (TryGetPerspectiveModelBounds(_selectedModel, out var bounds))
        {
            using var selectionPen = new Pen(ViewportColorPreferences.Get("Selection"), 2f);
            graphics.DrawRectangle(selectionPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
        if (!showModelTrajectoryCheckBox.Checked) return;

        var states = _document.Keyframes
            .Select(frame => (Frame: frame, State: frame.Models?.FirstOrDefault(state => state.ModelId == _selectedModel.Id)))
            .Where(item => item.State is not null).ToArray();
        using var pathPen = new Pen(ViewportColorPreferences.Get("CameraPath"), 1.5f)
        { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        using var draftPen = new Pen(ViewportColorPreferences.Get("CameraDraftPath"), 2f);
        for (var index = 1; index < states.Length; index++)
        {
            var first = states[index - 1].State!;
            var second = states[index].State!;
            var isDraft = _draftModelKeyframeId == states[index].Frame.Id && _draftModelId == _selectedModel.Id;
            var end = isDraft && _draftModelPosition is Vector3 draftPosition ? draftPosition : second.Position;
            var control = isDraft && _draftModelControl is Vector3 draftControl
                ? draftControl : second.PositionControl ?? Vector3.Lerp(first.Position, end, 0.5f);
            CameraEditorPoint? previous = ProjectPerspectivePoint(first.Position);
            for (var sample = 1; sample <= 24; sample++)
            {
                var amount = sample / 24f;
                var inverse = 1f - amount;
                var point = ProjectPerspectivePoint(inverse * inverse * first.Position +
                    2f * inverse * amount * control + amount * amount * end);
                if (previous is not null && point is not null)
                    graphics.DrawLine(isDraft ? draftPen : pathPen, previous.X, previous.Y, point.X, point.Y);
                previous = point;
            }
        }

        if (keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        var state = selected.Models?.FirstOrDefault(model => model.ModelId == _selectedModel.Id);
        if (state is null) return;
        EnsureModelDraft();
        var position = ProjectPerspectivePoint(_draftModelPosition ?? state.Position);
        CameraEditorPoint? controlPoint = null;
        if (_document.Keyframes.IndexOf(selected) > 0 && _draftModelControl is Vector3 draftControlPoint)
        {
            controlPoint = ProjectPerspectivePoint(draftControlPoint);
            if (position is not null && controlPoint is not null)
                graphics.DrawLine(Pens.Gray, position.X, position.Y, controlPoint.X, controlPoint.Y);
            if (controlPoint is not null) DrawHandle(graphics, controlPoint, ViewportColorPreferences.Get("CameraDraftPath"), 7f);
        }
        if (position is not null) DrawHandle(graphics, position, ViewportColorPreferences.Get("CameraControlPoint"), 8f);
        if (position is not null)
            _projectedPerspectiveModelControl = new ProjectedModelControl(_selectedModel, position, controlPoint);
        previewPictureBox.Invalidate();
    }

    private void PreviewPictureBox_MouseLeave(object? sender, EventArgs e)
    {
        if (!_perspectiveDragPreviewOnly) return;
        _perspectiveDragPreviewOnly = false;
        RequestPreviewRender();
    }

    private void PreviewPictureBox_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (playbackTimer.Enabled || _previewMouseCaptured || e.Delta == 0) return;
        if (!_perspectiveWheelEditPending)
        {
            _pendingDragHistory = CaptureEditorState("編輯 Camera");
            _perspectiveWheelEditPending = true;
        }

        var wheelSteps = e.Delta / 120f;
        var modifiers = ModifierKeys;
        if (modifiers.HasFlag(Keys.Control))
            CameraController.AdjustFieldOfView(_previewCamera, -wheelSteps * 2f);
        else if (modifiers.HasFlag(Keys.Shift))
            CameraController.AdjustRoll(_previewCamera, wheelSteps * 2f);
        else
            CameraController.Dolly(_previewCamera, -wheelSteps * 0.12f);

        _lastPerspectiveWheelTicks = Environment.TickCount64;
        PreviewCameraChangedByNavigation();
    }

    private void CompletePerspectiveWheelEdit()
    {
        if (!_perspectiveWheelEditPending) return;
        _perspectiveWheelEditPending = false;
        if (editModeComboBox.SelectedIndex == 1) CommitSelectedKeyframe();
        CommitPendingDragHistory();
        RequestPreviewRender();
    }

    private void PreviewPictureBox_Resize(object? sender, EventArgs e) => RequestPreviewRender();

    private void OrthographicPictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (playbackTimer.Enabled || e.Button is not (MouseButtons.Left or MouseButtons.Right) ||
            sender is not PictureBox pictureBox) return;
        var imagePoint = ToImagePoint(pictureBox, e.Location);
        _activeOrthographicView = GetOrthographicView(sender);
        if (_activeOrthographicView is null) return;
        _orthographicDragTarget = OrthographicDragTarget.None;
        if (e.Button == MouseButtons.Right)
        {
            _orthographicDragTarget = OrthographicDragTarget.Viewport;
        }
        else if (editModeComboBox.SelectedIndex == 1)
        {
            const float hitRadiusSquared = 18f * 18f;
            var candidates = new List<(OrthographicDragTarget Target, float Distance)>();
            if (_projectedPositionControls.TryGetValue(_activeOrthographicView.Value, out var positionControl))
                candidates.Add((OrthographicDragTarget.PositionBezierControl, DistanceSquared(positionControl, imagePoint)));
            if (_projectedTargetControls.TryGetValue(_activeOrthographicView.Value, out var targetControl))
                candidates.Add((OrthographicDragTarget.TargetBezierControl, DistanceSquared(targetControl, imagePoint)));
            if (showCameraCheckBox.Checked &&
                _projectedCameraPoints.TryGetValue(_activeOrthographicView.Value, out var cameraPoints))
            {
                candidates.Add((OrthographicDragTarget.CameraPosition,
                    DistanceSquared(cameraPoints.Position, imagePoint)));
                candidates.Add((OrthographicDragTarget.CameraTarget,
                    DistanceSquared(cameraPoints.Target, imagePoint)));
            }
            _orthographicDragTarget = candidates
                .Where(candidate => candidate.Distance <= hitRadiusSquared)
                .OrderBy(candidate => candidate.Distance)
                .Select(candidate => candidate.Target)
                .DefaultIfEmpty(OrthographicDragTarget.None)
                .First();
        }
        else if (editModeComboBox.SelectedIndex == 2 &&
                 _projectedLightControls.TryGetValue(_activeOrthographicView.Value, out var lightControls))
        {
            const float hitRadiusSquared = 18f * 18f;
            _activeLightControl = lightControls
                .Where(control => control.Light.Enabled &&
                                  DistanceSquared(control.Point, imagePoint) <= hitRadiusSquared)
                .OrderBy(control => LightControlPriority(control.Target))
                .ThenBy(control => DistanceSquared(control.Point, imagePoint))
                .FirstOrDefault();
            if (_activeLightControl is not null)
            {
                _orthographicDragTarget = _activeLightControl.Target;
                _activeLightControlWorld = _activeLightControl.WorldPoint;
                _selectedLight = _activeLightControl.Light;
                lightComboBox.SelectedItem = _selectedLight;
                inspectorTabControl.SelectedTab = lightTabPage;
            }
        }
        else if (editModeComboBox.SelectedIndex == 3)
        {
            const float hitRadiusSquared = 18f * 18f;
            _projectedModelControls.TryGetValue(_activeOrthographicView.Value, out var modelControl);
            var positionDistance = modelControl is null
                ? float.MaxValue : DistanceSquared(modelControl.Position, imagePoint);
            var controlDistance = modelControl?.BezierControl is null
                ? float.MaxValue : DistanceSquared(modelControl.BezierControl, imagePoint);
            if (modelControl is not null && Math.Min(positionDistance, controlDistance) <= hitRadiusSquared)
            {
                _selectedModel = modelControl.Model;
                SelectModelTreeNode(_selectedModel.Id);
                BindSelectedModel();
                inspectorTabControl.SelectedTab = objectTabPage;
                _orthographicDragTarget = controlDistance < positionDistance
                    ? OrthographicDragTarget.ModelBezierControl
                    : OrthographicDragTarget.ModelPosition;
                EnsureModelDraft();
            }
            else if (_projectedModelBounds.TryGetValue(_activeOrthographicView.Value, out var bounds))
            {
                var hit = bounds.Where(item => item.Bounds.Contains(imagePoint))
                    .OrderBy(item => item.Bounds.Width * item.Bounds.Height)
                    .FirstOrDefault();
                if (hit is not null)
                {
                    _selectedModel = hit.Model;
                    SelectModelTreeNode(_selectedModel.Id);
                    BindSelectedModel();
                    inspectorTabControl.SelectedTab = objectTabPage;
                    _orthographicDragTarget = OrthographicDragTarget.ModelPosition;
                    EnsureModelDraft();
                    RedrawEditorOverlay(pictureBox, _activeOrthographicView.Value);
                }
            }
        }
        else if (showCameraCheckBox.Checked &&
                 _projectedCameraPoints.TryGetValue(_activeOrthographicView.Value, out var cameraPoints))
        {
            var positionDistance = DistanceSquared(cameraPoints.Position, imagePoint);
            var targetDistance = DistanceSquared(cameraPoints.Target, imagePoint);
            const float hitRadiusSquared = 18f * 18f;
            if (Math.Min(positionDistance, targetDistance) <= hitRadiusSquared)
                _orthographicDragTarget = positionDistance <= targetDistance
                    ? OrthographicDragTarget.CameraPosition
                    : OrthographicDragTarget.CameraTarget;
        }
        if (_orthographicDragTarget == OrthographicDragTarget.None)
        {
            _activeOrthographicView = null;
            return;
        }
        _orthographicLastMouse = imagePoint;
        _orthographicDragButton = e.Button;
        _orthographicDragChanged = false;
        pictureBox.Capture = true;
        pictureBox.Focus();
        if (_orthographicDragTarget != OrthographicDragTarget.Viewport)
            _pendingDragHistory = CaptureEditorState(
                editModeComboBox.SelectedIndex == 2 ? "編輯燈光" :
                editModeComboBox.SelectedIndex == 3 ? "編輯模型" : "編輯 Camera Keyframe");
    }

    private void OrthographicPictureBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_activeOrthographicView is not CameraEditorView view || _orthographicDragButton == MouseButtons.None) return;
        if (sender is not PictureBox pictureBox) return;
        var imagePoint = ToImagePoint(pictureBox, e.Location);
        var dx = imagePoint.X - _orthographicLastMouse.X;
        var dy = imagePoint.Y - _orthographicLastMouse.Y;
        _orthographicLastMouse = imagePoint;
        if (dx == 0 && dy == 0) return;
        var fallbackScale = Math.Max(Vector3.Distance(_previewCamera.From, _previewCamera.To), 1f) * 0.0035f;
        var units = _editorWorldUnitsPerPixel.GetValueOrDefault(view, new Vector2(fallbackScale));
        var delta = view switch
        {
            CameraEditorView.Left => new Vector3(0f, -dy * units.Y, dx * units.X),
            CameraEditorView.Top => new Vector3(dx * units.X, 0f, dy * units.Y),
            _ => new Vector3(dx * units.X, -dy * units.Y, 0f)
        };
        if (_orthographicDragTarget == OrthographicDragTarget.Viewport)
        {
            var offset = GetViewOffset(view);
            offset.X -= dx * units.X;
            offset.Y += dy * units.Y;
            SetViewOffset(view, offset);
            RequestPreviewRender();
            return;
        }
        if (_activeLightControl is not null && _activeLightControl.Light.Enabled &&
            editModeComboBox.SelectedIndex == 2)
        {
            ApplyLightDrag(_activeLightControl, delta);
            _orthographicDragChanged = true;
            lightPropertyGrid.Refresh();
            RedrawEditorOverlay(pictureBox, view);
            RequestLightDragPreviewRender();
            return;
        }
        if (editModeComboBox.SelectedIndex == 3 && _selectedModel is not null &&
            _orthographicDragTarget is OrthographicDragTarget.ModelPosition or OrthographicDragTarget.ModelBezierControl)
        {
            EnsureModelDraft();
            if (_orthographicDragTarget == OrthographicDragTarget.ModelPosition)
            {
                _draftModelPosition += delta;
                if (_draftModelControl is not null) _draftModelControl += delta * 0.5f;
                _selectedModel.Transform.Position = _draftModelPosition!.Value;
            }
            else
                _draftModelControl += delta;
            _orthographicDragChanged = true;
            objectPropertyGrid.Refresh();
            _context.InvalidateScene();
            RedrawEditorOverlay(pictureBox, view);
            RequestDragPreviewRender();
            return;
        }
        if (keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        EnsureDraftControls(selected);
        if (_orthographicDragTarget == OrthographicDragTarget.PositionBezierControl)
            _draftPositionControl += delta;
        else if (_orthographicDragTarget == OrthographicDragTarget.TargetBezierControl)
            _draftTargetControl += delta;
        else if (_orthographicDragTarget == OrthographicDragTarget.CameraPosition)
            _previewCamera.From += delta;
        else if (_orthographicDragTarget == OrthographicDragTarget.CameraTarget)
            _previewCamera.To += delta;
        try { _previewCamera.Validate(); }
        catch (ArgumentException) { return; }
        _hasDraftEdits = true;
        _orthographicDragChanged = true;
        _previewProperties.SetFrom(_previewCamera);
        cameraPropertyGrid.Refresh();
        RedrawEditorOverlay(pictureBox, view);
        RequestDragPreviewRender();
    }

    private void OrthographicPictureBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != _orthographicDragButton) return;
        if (sender is PictureBox pictureBox) pictureBox.Capture = false;
        _orthographicDragButton = MouseButtons.None;
        _activeOrthographicView = null;
        var shouldCommit = _orthographicDragChanged &&
                           _orthographicDragTarget != OrthographicDragTarget.Viewport &&
                           editModeComboBox.SelectedIndex == 1;
        var shouldCommitModel = _orthographicDragChanged &&
                                editModeComboBox.SelectedIndex == 3 &&
                                _orthographicDragTarget is OrthographicDragTarget.ModelPosition or OrthographicDragTarget.ModelBezierControl;
        _orthographicDragTarget = OrthographicDragTarget.None;
        if (shouldCommit) CommitSelectedKeyframe();
        if (shouldCommitModel) CommitModelDraft();
        if (_orthographicDragChanged && _activeLightControl is not null)
        {
            CommitLightsToSelectedKeyframe();
            CommitPendingDragHistory();
            _context.InvalidateScene();
            _isDirty = true;
        }
        else if (_orthographicDragChanged && (shouldCommit || shouldCommitModel))
            CommitPendingDragHistory();
        else
            _pendingDragHistory = null;
        _activeLightControl = null;
        _orthographicDragChanged = false;
        RequestPreviewRender();
    }

    private void CommitPendingDragHistory()
    {
        if (_pendingDragHistory is null) return;
        _undoHistory.Add(_pendingDragHistory);
        if (_undoHistory.Count > 100) _undoHistory.RemoveAt(0);
        _redoHistory.Clear();
        _pendingDragHistory = null;
        UpdateHistoryButtons();
    }

    private static int LightControlPriority(OrthographicDragTarget target) => target switch
    {
        OrthographicDragTarget.LightFallIn or OrthographicDragTarget.LightFallOff => 0,
        OrthographicDragTarget.LightTarget => 1,
        _ => 2
    };

    private void ApplyLightDrag(ProjectedLightControl control, Vector3 delta)
    {
        var light = control.Light;
        _activeLightControlWorld += delta;
        switch (control.Target)
        {
            case OrthographicDragTarget.LightPosition:
                light.Position += delta;
                break;
            case OrthographicDragTarget.LightTarget:
            {
                var direction = _activeLightControlWorld - light.Position;
                if (direction.LengthSquared() > 0.000001f) light.Direction = Vector3.Normalize(direction);
                break;
            }
            case OrthographicDragTarget.LightFallIn:
            case OrthographicDragTarget.LightFallOff:
            {
                var fromLight = _activeLightControlWorld - light.Position;
                var axial = Math.Max(0.01f, Vector3.Dot(fromLight, light.Direction));
                var radial = (fromLight - light.Direction * axial).Length();
                var angle = MathF.Atan2(radial, axial) * 180f / MathF.PI;
                if (control.Target == OrthographicDragTarget.LightFallIn)
                    light.FallInDegrees = Math.Clamp(angle, 0.1f, Math.Max(0.1f, light.FallOffDegrees - 0.1f));
                else
                    light.FallOffDegrees = Math.Clamp(angle, Math.Min(89.5f, light.FallInDegrees + 0.1f), 89.5f);
                break;
            }
        }
        light.Validate();
    }

    private static float DistanceSquared(CameraEditorPoint point, PointF mouse)
    {
        var dx = point.X - mouse.X;
        var dy = point.Y - mouse.Y;
        return dx * dx + dy * dy;
    }

    private void DrawPluginEditorOverlays(PictureBox pictureBox, CameraEditorView view)
    {
        if (pictureBox.Image is not Bitmap bitmap) return;
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        _projectedPositionControls.Remove(view);
        _projectedTargetControls.Remove(view);
        _projectedModelControls.Remove(view);
        var modelBounds = SceneProject.Models.Where(model => model.IsVisible)
            .Select(model => TryGetOrthographicModelBounds(model, view, out var bounds)
                ? new ProjectedModelBounds(model, bounds)
                : null)
            .Where(item => item is not null).Cast<ProjectedModelBounds>().ToList();
        _projectedModelBounds[view] = modelBounds;
        if (editModeComboBox.SelectedIndex == 3 && _selectedModel is not null)
        {
            var selectedBounds = modelBounds.FirstOrDefault(item => ReferenceEquals(item.Model, _selectedModel));
            if (selectedBounds is not null)
            {
                using var selectionPen = new Pen(ViewportColorPreferences.Get("Selection"), 2f);
                graphics.DrawRectangle(selectionPen, selectedBounds.Bounds.X, selectedBounds.Bounds.Y,
                    selectedBounds.Bounds.Width, selectedBounds.Bounds.Height);
            }
        }
        if (editModeComboBox.SelectedIndex == 1 && keyframeListBox.SelectedItem is CameraKeyframe selected)
        {
            var index = _document.Keyframes.IndexOf(selected);
            if (index > 0)
            {
                EnsureDraftControls(selected);
                var positionPoint = ProjectWorldPoint(view, _draftPositionControl!.Value);
                var targetPoint = ProjectWorldPoint(view, _draftTargetControl!.Value);
                _projectedPositionControls[view] = positionPoint;
                _projectedTargetControls[view] = targetPoint;
                using var frustumPen = new Pen(ViewportColorPreferences.Get("CameraFrustum"), 1.5f);
                graphics.DrawLine(frustumPen, positionPoint.X, positionPoint.Y, targetPoint.X, targetPoint.Y);
                DrawHandle(graphics, positionPoint, ViewportColorPreferences.Get("CameraPosition"), 7f);
                DrawHandle(graphics, targetPoint, ViewportColorPreferences.Get("CameraTarget"), 7f);
            }
        }

        if (showModelTrajectoryCheckBox.Checked && _selectedModel is not null)
            DrawModelTrajectory(graphics, view, _selectedModel);

        var projectedLights = new List<ProjectedLightControl>();
        _projectedLightControls[view] = projectedLights;
        var lightsToDraw = showAllLightsCheckBox.Checked
            ? SceneProject.Lights
            : editModeComboBox.SelectedIndex == 2 && _selectedLight is not null
                ? SceneProject.Lights.Where(light => ReferenceEquals(light, _selectedLight))
                : Enumerable.Empty<SceneLight>();
        foreach (var light in lightsToDraw)
            DrawLightControls(graphics, view, light, projectedLights);
    }

    private void DrawModelTrajectory(Graphics graphics, CameraEditorView view, SceneModel model)
    {
        var states = _document.Keyframes
            .Select(frame => (Frame: frame, State: frame.Models?.FirstOrDefault(state => state.ModelId == model.Id)))
            .Where(item => item.State is not null).ToArray();
        if (states.Length == 0) return;
        using var pathPen = new Pen(ViewportColorPreferences.Get("CameraPath"), 1.5f)
        { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        using var draftPen = new Pen(ViewportColorPreferences.Get("CameraDraftPath"), 2f);
        for (var index = 1; index < states.Length; index++)
        {
            var first = states[index - 1].State!;
            var second = states[index].State!;
            var isDraft = _draftModelKeyframeId == states[index].Frame.Id && _draftModelId == model.Id;
            var end = isDraft && _draftModelPosition is Vector3 draftPosition ? draftPosition : second.Position;
            var curveControl = isDraft && _draftModelControl is Vector3 draftControl
                ? draftControl : second.PositionControl ?? Vector3.Lerp(first.Position, end, 0.5f);
            var previousPoint = ProjectWorldPoint(view, first.Position);
            for (var sample = 1; sample <= 24; sample++)
            {
                var amount = sample / 24f;
                var inverse = 1f - amount;
                var world = inverse * inverse * first.Position + 2f * inverse * amount * curveControl + amount * amount * end;
                var point = ProjectWorldPoint(view, world);
                graphics.DrawLine(isDraft ? draftPen : pathPen, previousPoint.X, previousPoint.Y, point.X, point.Y);
                previousPoint = point;
            }
        }

        if (keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        var selectedState = selected.Models?.FirstOrDefault(state => state.ModelId == model.Id);
        if (selectedState is null) return;
        EnsureModelDraft();
        var position = ProjectWorldPoint(view, _draftModelPosition ?? selectedState.Position);
        CameraEditorPoint? controlPoint = null;
        if (_document.Keyframes.IndexOf(selected) > 0 && _draftModelControl is Vector3 control)
        {
            controlPoint = ProjectWorldPoint(view, control);
            graphics.DrawLine(Pens.Gray, position.X, position.Y, controlPoint.X, controlPoint.Y);
            DrawHandle(graphics, controlPoint, ViewportColorPreferences.Get("CameraDraftPath"), 7f);
        }
        DrawHandle(graphics, position, ViewportColorPreferences.Get("CameraControlPoint"), 8f);
        _projectedModelControls[view] = new ProjectedModelControl(model, position, controlPoint);
    }

    private void EnsureModelDraft()
    {
        if (_selectedModel is null || keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        if (_draftModelKeyframeId == selected.Id && _draftModelId == _selectedModel.Id) return;
        var state = selected.Models?.FirstOrDefault(model => model.ModelId == _selectedModel.Id);
        var index = _document.Keyframes.IndexOf(selected);
        var previous = index > 0
            ? _document.Keyframes[index - 1].Models?.FirstOrDefault(model => model.ModelId == _selectedModel.Id)
            : null;
        var position = state?.Position ?? _selectedModel.Transform.Position;
        _draftModelKeyframeId = selected.Id;
        _draftModelId = _selectedModel.Id;
        _draftModelPosition = position;
        _draftModelControl = index > 0
            ? state?.PositionControl ?? Vector3.Lerp(previous?.Position ?? position, position, 0.5f)
            : null;
    }

    private void CommitModelDraft()
    {
        if (_selectedModel is null || keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        selected.Models ??= [];
        var state = selected.Models.FirstOrDefault(model => model.ModelId == _selectedModel.Id);
        if (state is null)
        {
            state = new AnimationModelState
            {
                ModelId = _selectedModel.Id,
                Name = _selectedModel.Name,
                Visible = _selectedModel.IsVisible,
                Position = _selectedModel.Transform.Position,
                RotationDegrees = _selectedModel.Transform.RotationDegrees,
                Scale = _selectedModel.Transform.Scale
            };
            selected.Models.Add(state);
        }
        if (_draftModelPosition is Vector3 position) state.Position = position;
        state.PositionControl = _draftModelControl;
        _selectedModel.Transform.Position = state.Position;
        MarkDocumentDirty();
        ClearModelDraftEdit();
    }

    private void SelectModelTreeNode(Guid modelId)
    {
        foreach (TreeNode node in sceneTreeView.Nodes)
            if (node.Tag is SceneModel model && model.Id == modelId)
            {
                sceneTreeView.SelectedNode = node;
                node.EnsureVisible();
                return;
            }
    }

    private void DrawLightControls(Graphics graphics, CameraEditorView view, SceneLight light,
        List<ProjectedLightControl> controls)
    {
        var selected = ReferenceEquals(light, _selectedLight);
        var position = ProjectWorldPoint(view, light.Position);
        var lightColor = ToDrawingColor(light.Color);
        var selectedColor = BlendColor(lightColor, Color.White, 0.35f);
        var positionColor = !light.Enabled ? Color.Gray : selected ? selectedColor : lightColor;
        DrawHandle(graphics, position, positionColor, selected ? 8f : 6f);
        if (light.Enabled)
            controls.Add(new ProjectedLightControl(light, OrthographicDragTarget.LightPosition, position, light.Position));

        if (light.Type == SceneLightType.Point) return;
        var displayLength = light.Type == SceneLightType.Spot
            ? Math.Max(light.Range, 0.01f)
            : Math.Max((_editorProjectionCameraDistance.GetValueOrDefault(view,
                Vector3.Distance(_previewCamera.From, _previewCamera.To))) * 0.3f, 1f);
        var targetWorld = light.Position + light.Direction * displayLength;
        var target = ProjectWorldPoint(view, targetWorld);
        using var axisPen = new Pen(!light.Enabled ? Color.Gray : selected ? selectedColor : lightColor,
            selected ? 2f : 1f);
        graphics.DrawLine(axisPen, position.X, position.Y, target.X, target.Y);
        DrawHandle(graphics, target, light.Enabled ? selected ? selectedColor : lightColor : Color.Gray, 7f);
        if (light.Enabled)
            controls.Add(new ProjectedLightControl(light, OrthographicDragTarget.LightTarget, target, targetWorld));

        if (light.Type != SceneLightType.Spot) return;
        var reference = MathF.Abs(light.Direction.Y) < 0.95f ? Vector3.UnitY : Vector3.UnitX;
        var right = Vector3.Normalize(Vector3.Cross(light.Direction, reference));
        var up = Vector3.Normalize(Vector3.Cross(right, light.Direction));
        DrawSpotRing(graphics, view, light, targetWorld, right, up, light.FallInDegrees,
            light.Enabled ? Color.FromArgb(230, selected ? selectedColor : lightColor) : Color.Gray,
            OrthographicDragTarget.LightFallIn, controls, System.Drawing.Drawing2D.DashStyle.Solid);
        DrawSpotRing(graphics, view, light, targetWorld, right, up, light.FallOffDegrees,
            light.Enabled ? Color.FromArgb(165, selected ? selectedColor : lightColor) : Color.Gray,
            OrthographicDragTarget.LightFallOff, controls, System.Drawing.Drawing2D.DashStyle.Dash);
    }

    private void DrawSpotRing(Graphics graphics, CameraEditorView view, SceneLight light,
        Vector3 center, Vector3 right, Vector3 up, float angleDegrees, Color color,
        OrthographicDragTarget target, List<ProjectedLightControl> controls,
        System.Drawing.Drawing2D.DashStyle dashStyle)
    {
        const int segments = 32;
        var radius = MathF.Tan(angleDegrees * MathF.PI / 180f) * light.Range;
        var points = new PointF[segments + 1];
        for (var index = 0; index <= segments; index++)
        {
            var angle = 2f * MathF.PI * index / segments;
            var world = center + right * (radius * MathF.Cos(angle)) + up * (radius * MathF.Sin(angle));
            var point = ProjectWorldPoint(view, world);
            points[index] = new PointF(point.X, point.Y);
        }
        using var pen = new Pen(color, 1.5f);
        pen.DashStyle = dashStyle;
        graphics.DrawLines(pen, points);
        foreach (var handleWorld in new[]
                 {
                     center + right * radius, center - right * radius,
                     center + up * radius, center - up * radius
                 })
        {
            var handle = ProjectWorldPoint(view, handleWorld);
            DrawHandle(graphics, handle, color, 6f);
            if (light.Enabled)
                controls.Add(new ProjectedLightControl(light, target, handle, handleWorld));
        }
    }

    private CameraEditorPoint ProjectWorldPoint(CameraEditorView view, Vector3 world)
    {
        if (!_projectedCameraPoints.TryGetValue(view, out var cameraPoints) ||
            !_editorWorldUnitsPerPixel.TryGetValue(view, out var units) ||
            !_editorProjectionCameraFrom.TryGetValue(view, out var projectionCameraFrom))
            return new CameraEditorPoint(0f, 0f);
        units.X = Math.Max(units.X, 0.000001f);
        units.Y = Math.Max(units.Y, 0.000001f);
        return view switch
        {
            CameraEditorView.Left => new CameraEditorPoint(
                cameraPoints.Position.X + (world.Z - projectionCameraFrom.Z) / units.X,
                cameraPoints.Position.Y - (world.Y - projectionCameraFrom.Y) / units.Y),
            CameraEditorView.Top => new CameraEditorPoint(
                cameraPoints.Position.X + (world.X - projectionCameraFrom.X) / units.X,
                cameraPoints.Position.Y + (world.Z - projectionCameraFrom.Z) / units.Y),
            _ => new CameraEditorPoint(
                cameraPoints.Position.X + (world.X - projectionCameraFrom.X) / units.X,
                cameraPoints.Position.Y - (world.Y - projectionCameraFrom.Y) / units.Y)
        };
    }

    private static void DrawHandle(Graphics graphics, CameraEditorPoint point, Color color, float radius)
    {
        using var brush = new SolidBrush(color);
        using var outline = new Pen(Color.Black, 1.5f);
        graphics.FillEllipse(brush, point.X - radius, point.Y - radius, radius * 2f, radius * 2f);
        graphics.DrawEllipse(outline, point.X - radius, point.Y - radius, radius * 2f, radius * 2f);
    }

    private static Color ToDrawingColor(Vector3 color) => Color.FromArgb(
        255,
        (int)MathF.Round(Math.Clamp(color.X, 0f, 1f) * 255f),
        (int)MathF.Round(Math.Clamp(color.Y, 0f, 1f) * 255f),
        (int)MathF.Round(Math.Clamp(color.Z, 0f, 1f) * 255f));

    private static Color BlendColor(Color first, Color second, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            first.A,
            (int)MathF.Round(first.R + (second.R - first.R) * amount),
            (int)MathF.Round(first.G + (second.G - first.G) * amount),
            (int)MathF.Round(first.B + (second.B - first.B) * amount));
    }

    private static PointF ToImagePoint(PictureBox pictureBox, Point clientPoint)
    {
        if (pictureBox.Image is null || pictureBox.ClientSize.Width <= 0 || pictureBox.ClientSize.Height <= 0)
            return clientPoint;
        var scale = Math.Min(pictureBox.ClientSize.Width / (float)pictureBox.Image.Width,
            pictureBox.ClientSize.Height / (float)pictureBox.Image.Height);
        if (scale <= 0f) return clientPoint;
        var displayedWidth = pictureBox.Image.Width * scale;
        var displayedHeight = pictureBox.Image.Height * scale;
        var left = (pictureBox.ClientSize.Width - displayedWidth) * 0.5f;
        var top = (pictureBox.ClientSize.Height - displayedHeight) * 0.5f;
        return new PointF((clientPoint.X - left) / scale, (clientPoint.Y - top) / scale);
    }

    private void OrthographicPictureBox_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (playbackTimer.Enabled) return;
        if ((ModifierKeys & Keys.Control) == Keys.Control || wheelChangesFovCheckBox.Checked)
        {
            _previewCamera.FieldOfViewDegrees = Math.Clamp(
                _previewCamera.FieldOfViewDegrees - e.Delta / 120f * 2f, 1f, 179f);
            PreviewCameraChangedByNavigation();
            return;
        }

        var view = GetOrthographicView(sender);
        if (view is null || sender is not PictureBox pictureBox) return;
        var factor = e.Delta > 0 ? 1f / 1.15f : 1.15f;
        var oldScale = GetViewScale(view.Value);
        var newScale = Math.Clamp(oldScale * factor, 0.1f, 20f);
        if (zoomAtMouseCheckBox.Checked && _editorWorldUnitsPerPixel.TryGetValue(view.Value, out var oldUnits))
        {
            var newUnits = oldUnits * (newScale / oldScale);
            var offset = GetViewOffset(view.Value);
            offset.X += (e.X - pictureBox.ClientSize.Width * 0.5f) * (oldUnits.X - newUnits.X);
            offset.Y += (pictureBox.ClientSize.Height * 0.5f - e.Y) * (oldUnits.Y - newUnits.Y);
            SetViewOffset(view.Value, offset);
        }
        switch (view.Value)
        {
            case CameraEditorView.Front: _frontViewScale = newScale; break;
            case CameraEditorView.Left: _leftViewScale = newScale; break;
            case CameraEditorView.Top: _topViewScale = newScale; break;
        }
        RequestPreviewRender();
    }

    private CameraEditorView? GetOrthographicView(object? sender) => sender switch
    {
        PictureBox picture when ReferenceEquals(picture, frontPictureBox) => CameraEditorView.Front,
        PictureBox picture when ReferenceEquals(picture, leftPictureBox) => CameraEditorView.Left,
        PictureBox picture when ReferenceEquals(picture, topPictureBox) => CameraEditorView.Top,
        _ => null
    };

    private void PreviewCameraChangedByNavigation()
    {
        _previewProperties.SetFrom(_previewCamera);
        cameraPropertyGrid.Refresh();
        MarkDraftEdit(requestRender: false);
        RequestDragPreviewRender();
    }

    private void SetPreviewCamera(CameraState camera)
    {
        _previewCamera = CloneCamera(camera);
        _previewCamera.Validate();
        _previewProperties.SetFrom(_previewCamera);
        cameraPropertyGrid.Refresh();
        RequestPreviewRender();
    }

    private void RequestPreviewRender()
    {
        if (_closing) return;
        _previewRenderDirty = true;
        unchecked { _previewRenderRevision++; }
    }

    private Task<byte[]> RenderScenePreviewAsync(CameraState camera, int width, int height,
        bool useQuickPreview, CancellationToken cancellationToken) =>
        _isolatedProject is not null && IsolatedSceneService is not null
            ? IsolatedSceneService.RenderPreviewPngAsync(_isolatedProject, camera, width, height,
                useQuickPreview, cancellationToken)
            : _context.Viewport.RenderPreviewPngAsync(camera, width, height, useQuickPreview, cancellationToken);

    private Task<CameraEditorRenderResult> RenderSceneEditorViewAsync(CameraState camera,
        IReadOnlyList<CameraState> trajectory, IReadOnlyList<CameraState> draftTrajectory,
        IReadOnlyList<Vector3> editControlPoints, CameraEditorView view, int width, int height,
        float viewScale, Vector2 viewOffset, bool showCamera, bool showTrajectory, bool useQuickPreview,
        CancellationToken cancellationToken) =>
        _isolatedProject is not null && IsolatedSceneService is not null
            ? IsolatedSceneService.RenderCameraEditorViewPngAsync(_isolatedProject, camera, trajectory,
                draftTrajectory, editControlPoints, view, width, height, viewScale, viewOffset,
                showCamera, showTrajectory, useQuickPreview, cancellationToken)
            : _context.Viewport.RenderCameraEditorViewPngAsync(camera, trajectory, draftTrajectory,
                editControlPoints, view, width, height, viewScale, viewOffset, showCamera, showTrajectory,
                useQuickPreview, cancellationToken);

    private void RequestDragPreviewRender()
    {
        var now = Environment.TickCount64;
        if (now - _lastDragRenderTicks < DragRenderIntervalMilliseconds) return;
        _lastDragRenderTicks = now;
        RequestPreviewRender();
    }

    private void RequestLightDragPreviewRender()
    {
        var now = Environment.TickCount64;
        if (now - _lastDragRenderTicks < DragRenderIntervalMilliseconds) return;
        _lastDragRenderTicks = now;
        _context.InvalidateScene();
        RequestPreviewRender();
    }

    private void CacheEditorBaseImage(PictureBox pictureBox, CameraEditorView view)
    {
        if (pictureBox.Image is null) return;
        if (_editorBaseImages.Remove(view, out var previous)) previous.Dispose();
        _editorBaseImages[view] = new Bitmap(pictureBox.Image);
    }

    private void RedrawEditorOverlay(PictureBox pictureBox, CameraEditorView view)
    {
        if (!_editorBaseImages.TryGetValue(view, out var baseImage)) return;
        var image = new Bitmap(baseImage);
        var previous = pictureBox.Image;
        pictureBox.Image = image;
        previous?.Dispose();
        DrawPluginEditorOverlays(pictureBox, view);
        pictureBox.Invalidate();
    }

    private IEnumerable<(CameraEditorView View, PictureBox PictureBox)> GetOrthographicPictureBoxes()
    {
        yield return (CameraEditorView.Front, frontPictureBox);
        yield return (CameraEditorView.Left, leftPictureBox);
        yield return (CameraEditorView.Top, topPictureBox);
    }

    private async void PreviewRenderTimer_Tick(object? sender, EventArgs e)
    {
        if (_perspectiveWheelEditPending &&
            Environment.TickCount64 - _lastPerspectiveWheelTicks >= WheelEditCompletionMilliseconds)
            CompletePerspectiveWheelEdit();
        if (!_previewRenderDirty || _previewRenderRunning || IsDisposed || !previewPictureBox.IsHandleCreated) return;
        var width = Math.Clamp(previewPictureBox.ClientSize.Width, 1, 1280);
        var height = Math.Clamp(previewPictureBox.ClientSize.Height, 1, 720);
        var revision = _previewRenderRevision;
        var camera = CloneCamera(_previewCamera);
        var trajectory = SampleTrajectory(_document);
        var draftTrajectory = SampleDraftTrajectory();
        var editControlPoints = GetEditControlPoints();
        var quickPreview = _context.QuickPreviewEnabled;
        var perspectiveQuickPreview = editModeComboBox.SelectedIndex == 2
            ? false
            : quickPreview;
        var showCamera = showCameraCheckBox.Checked;
        var showTrajectory = showTrajectoryCheckBox.Checked;
        _previewRenderDirty = false;
        _previewRenderRunning = true;
        _previewRenderCancellation?.Cancel();
        _previewRenderCancellation?.Dispose();
        _previewRenderCancellation = new CancellationTokenSource();
        var cancellation = _previewRenderCancellation;
        try
        {
            var png = await RenderScenePreviewAsync(
                camera, width, height,
                useQuickPreview: perspectiveQuickPreview,
                cancellationToken: cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!playbackTimer.Enabled && revision != _previewRenderRevision) throw new OperationCanceledException();
            SetPictureBoxPng(previewPictureBox, png);
            DrawPerspectiveModelOverlay();
            if (_perspectiveDragPreviewOnly) return;
            foreach (var (view, pictureBox) in new[]
                     {
                         (CameraEditorView.Front, frontPictureBox),
                         (CameraEditorView.Left, leftPictureBox),
                         (CameraEditorView.Top, topPictureBox)
                     })
            {
                var editorWidth = Math.Clamp(pictureBox.ClientSize.Width, 1, 640);
                var editorHeight = Math.Clamp(pictureBox.ClientSize.Height, 1, 480);
                var editorResult = await RenderSceneEditorViewAsync(
                    camera, trajectory, draftTrajectory, editControlPoints, view, editorWidth, editorHeight,
                    GetViewScale(view), GetViewOffset(view), showCamera, showTrajectory,
                    useQuickPreview: quickPreview,
                    cancellationToken: cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (!playbackTimer.Enabled && revision != _previewRenderRevision) throw new OperationCanceledException();
                _projectedEditControls[view] = editorResult.EditControlPoints;
                _projectedCameraPoints[view] = (editorResult.CameraPosition, editorResult.CameraTarget);
                _editorWorldUnitsPerPixel[view] = new Vector2(
                    editorResult.HorizontalUnitsPerPixel, editorResult.VerticalUnitsPerPixel);
                _editorProjectionCameraFrom[view] = camera.From;
                _editorProjectionCameraDistance[view] = Vector3.Distance(camera.From, camera.To);
                SetPictureBoxPng(pictureBox, editorResult.Png);
                CacheEditorBaseImage(pictureBox, view);
                DrawPluginEditorOverlays(pictureBox, view);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or
                                      System.Runtime.InteropServices.ExternalException)
        {
            previewRenderTimer.Stop();
            _context.SetStatus($"Camera Preview 無法渲染：{ex.Message}");
        }
        finally
        {
            _previewRenderRunning = false;
        }
    }

    private float GetViewScale(CameraEditorView view) => view switch
    {
        CameraEditorView.Front => _frontViewScale,
        CameraEditorView.Left => _leftViewScale,
        CameraEditorView.Top => _topViewScale,
        _ => 1f
    };

    private Vector2 GetViewOffset(CameraEditorView view) => view switch
    {
        CameraEditorView.Front => _frontViewOffset,
        CameraEditorView.Left => _leftViewOffset,
        CameraEditorView.Top => _topViewOffset,
        _ => Vector2.Zero
    };

    private void SetViewOffset(CameraEditorView view, Vector2 offset)
    {
        switch (view)
        {
            case CameraEditorView.Front: _frontViewOffset = offset; break;
            case CameraEditorView.Left: _leftViewOffset = offset; break;
            case CameraEditorView.Top: _topViewOffset = offset; break;
        }
    }

    private static IReadOnlyList<CameraState> SampleTrajectory(CameraAnimationDocument document)
    {
        if (document.Keyframes.Count == 0) return Array.Empty<CameraState>();
        if (document.Keyframes.Count == 1) return new[] { document.Keyframes[0].ToCameraState() };
        var ordered = document.Keyframes.OrderBy(frame => frame.TimeSeconds).ThenBy(frame => frame.Id).ToArray();
        var result = new List<CameraState>();
        const int samplesPerSegment = 24;
        for (var segment = 1; segment < ordered.Length; segment++)
        {
            var start = ordered[segment - 1].TimeSeconds;
            var end = ordered[segment].TimeSeconds;
            for (var sample = segment == 1 ? 0 : 1; sample <= samplesPerSegment; sample++)
            {
                var time = start + (end - start) * sample / samplesPerSegment;
                result.Add(CameraAnimationEvaluator.Evaluate(document, time));
            }
        }
        return result;
    }

    private IReadOnlyList<CameraState> SampleDraftTrajectory()
    {
        if (!_hasDraftEdits || _draftKeyframeId is not Guid selectedId) return Array.Empty<CameraState>();
        var draft = new CameraAnimationDocument
        {
            Version = _document.Version,
            Name = _document.Name,
            DurationSeconds = _document.DurationSeconds,
            FramesPerSecond = _document.FramesPerSecond,
            Loop = _document.Loop,
            Keyframes = _document.Keyframes.Select(CloneKeyframe).ToList()
        };
        var selected = draft.Keyframes.FirstOrDefault(frame => frame.Id == selectedId);
        if (selected is null) return Array.Empty<CameraState>();
        selected.From = _previewCamera.From;
        selected.To = _previewCamera.To;
        selected.Up = _previewCamera.Up;
        selected.RollDegrees = _previewCamera.RollDegrees;
        selected.FieldOfViewDegrees = _previewCamera.FieldOfViewDegrees;
        selected.NearPlane = _previewCamera.NearPlane;
        selected.FarPlane = _previewCamera.FarPlane;
        selected.PositionControl = _draftPositionControl;
        selected.TargetControl = _draftTargetControl;
        return SampleTrajectory(draft);
    }

    private void DisplayOverlayCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_updatingUi) return;
        SyncUnifiedMenuDisplayState();
        RequestPreviewRender();
    }

    private void ShowAllLightsCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_updatingUi) return;
        SyncUnifiedMenuDisplayState();
        foreach (var (view, pictureBox) in GetOrthographicPictureBoxes())
            RedrawEditorOverlay(pictureBox, view);
        RequestPreviewRender();
    }

    private void EditModeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (editModeComboBox.SelectedIndex == 1)
        {
            showTrajectoryCheckBox.Checked = true;
            lightComboBox.SelectedIndex = -1;
            _selectedLight = null;
            lightPropertyGrid.SelectedObject = null;

            if (keyframeListBox.SelectedIndex < 0 && keyframeListBox.Items.Count > 0)
                keyframeListBox.SelectedIndex = keyframeListBox.Items.Count - 1;
            else if (keyframeListBox.Items.Count == 0)
                _context.SetStatus("Camera 編輯模式需要至少一個 KeyFrame，請先加入關鍵影格。");
        }
        else if (editModeComboBox.SelectedIndex == 2)
        {
            showAllLightsCheckBox.Checked = true;
            if (keyframeListBox.SelectedIndex < 0 && keyframeListBox.Items.Count > 0)
                keyframeListBox.SelectedIndex = keyframeListBox.Items.Count - 1;
            else if (keyframeListBox.Items.Count == 0)
                _context.SetStatus("燈光編輯模式需要至少一個 KeyFrame，請先加入關鍵影格。");
        }
        else if (editModeComboBox.SelectedIndex == 3)
        {
            showModelTrajectoryCheckBox.Checked = true;
            sceneTabControl.SelectedTab = objectTreeTabPage;
            inspectorTabControl.SelectedTab = objectTabPage;
            lightComboBox.SelectedIndex = -1;
            _selectedLight = null;
            lightPropertyGrid.SelectedObject = null;
            if (keyframeListBox.SelectedIndex < 0 && keyframeListBox.Items.Count > 0)
                keyframeListBox.SelectedIndex = keyframeListBox.Items.Count - 1;
            else if (keyframeListBox.Items.Count == 0)
                _context.SetStatus("模型編輯模式需要至少一個 KeyFrame，請先加入關鍵影格。");
            BindSelectedModel();
        }
        RequestPreviewRender();
    }

    private IReadOnlyList<Vector3> GetEditControlPoints()
    {
        if (editModeComboBox.SelectedIndex != 1 || keyframeListBox.SelectedItem is not CameraKeyframe selected)
            return Array.Empty<Vector3>();
        var index = _document.Keyframes.IndexOf(selected);
        if (index <= 0) return Array.Empty<Vector3>();
        EnsureDraftControls(selected);
        return Array.Empty<Vector3>();
    }

    private void EnsureDraftControls(CameraKeyframe selected)
    {
        var index = _document.Keyframes.IndexOf(selected);
        if (index <= 0) return;
        if (_draftKeyframeId == selected.Id) return;
        var previous = _document.Keyframes[index - 1];
        _draftKeyframeId = selected.Id;
        _draftPositionControl = selected.PositionControl ?? Vector3.Lerp(previous.From, selected.From, 0.5f);
        _draftTargetControl = selected.TargetControl ?? Vector3.Lerp(previous.To, selected.To, 0.5f);
    }

    private void MarkDraftEdit(bool requestRender = true)
    {
        if (keyframeListBox.SelectedItem is not CameraKeyframe selected) return;
        EnsureDraftControls(selected);
        _draftKeyframeId = selected.Id;
        _draftPositionControl ??= selected.PositionControl;
        _draftTargetControl ??= selected.TargetControl;
        _hasDraftEdits = true;
        if (requestRender) RequestPreviewRender();
    }

    private void ClearDraftEdit()
    {
        _draftKeyframeId = null;
        _draftPositionControl = null;
        _draftTargetControl = null;
        _hasDraftEdits = false;
    }

    private static CameraKeyframe CloneKeyframe(CameraKeyframe source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        TimeSeconds = source.TimeSeconds,
        From = source.From,
        To = source.To,
        Up = source.Up,
        RollDegrees = source.RollDegrees,
        PositionControl = source.PositionControl,
        TargetControl = source.TargetControl,
        FieldOfViewDegrees = source.FieldOfViewDegrees,
        NearPlane = source.NearPlane,
        FarPlane = source.FarPlane,
        Easing = source.Easing,
        Lights = source.Lights?.Select(CloneAnimationLight).ToList(),
        Models = source.Models?.Select(CloneAnimationModel).ToList()
    };

    private static AnimationLightState CloneAnimationLight(AnimationLightState source) => new()
    {
        Id = source.Id, Name = source.Name, Enabled = source.Enabled, Type = source.Type,
        Position = source.Position, Direction = source.Direction, Color = source.Color,
        Intensity = source.Intensity, Range = source.Range,
        FallInDegrees = source.FallInDegrees, FallOffDegrees = source.FallOffDegrees
    };

    private static void ResetBezierControls(CameraKeyframe keyframe)
    {
        keyframe.PositionControl = null;
        keyframe.TargetControl = null;
    }

    private void SplitModelBezierControls(CameraKeyframe selected, CameraKeyframe inserted, double timeSeconds)
    {
        var index = _document.Keyframes.IndexOf(selected);
        if (index <= 0 || selected.Models is null || inserted.Models is null) return;
        var previous = _document.Keyframes[index - 1];
        if (previous.Models is null) return;
        var span = selected.TimeSeconds - previous.TimeSeconds;
        var amount = span <= double.Epsilon ? 0.5f :
            Math.Clamp((float)((timeSeconds - previous.TimeSeconds) / span), 0f, 1f);
        foreach (var insertedModel in inserted.Models)
        {
            var first = previous.Models.FirstOrDefault(model => model.ModelId == insertedModel.ModelId);
            var second = selected.Models.FirstOrDefault(model => model.ModelId == insertedModel.ModelId);
            if (first is null || second is null) continue;
            var control = second.PositionControl ?? Vector3.Lerp(first.Position, second.Position, 0.5f);
            var firstSplitControl = Vector3.Lerp(first.Position, control, amount);
            var secondSplitControl = Vector3.Lerp(control, second.Position, amount);
            insertedModel.PositionControl = firstSplitControl;
            second.PositionControl = secondSplitControl;
        }
    }

    private void ClearModelDraftEdit()
    {
        _draftModelKeyframeId = null;
        _draftModelId = null;
        _draftModelPosition = null;
        _draftModelControl = null;
    }

    private static void SetPictureBoxPng(PictureBox pictureBox, byte[] png)
    {
        using var stream = new MemoryStream(png, writable: false);
        using var decoded = Image.FromStream(stream);
        var image = new Bitmap(decoded);
        var previous = pictureBox.Image;
        pictureBox.Image = image;
        previous?.Dispose();
    }

    private void Context_ProjectChanged(object? sender, EventArgs e)
    {
        if (_applyingAnimatedLights || _closing) return;
        ClearDraftEdit();
        _exportCancellation?.Cancel();
        playbackTimer.Stop();
        _playbackClock.Stop();
        SetPreviewCamera(CloneCamera(_context.Camera.Current));
        RebuildSceneTree();
        UpdatePlaybackButtons();
    }

    private void Context_PreviewModeChanged(object? sender, EventArgs e) => RequestPreviewRender();

    private async void CameraAnimationForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        if (_closeSaveInProgress) return;
        _closeSaveInProgress = true;
        _closing = true;
        previewRenderTimer.Stop();
        _previewRenderCancellation?.Cancel();
        CompletePerspectiveWheelEdit();
        if (_exportCancellation is not null)
        {
            _exportCancellation.Cancel();
            if (!_shuttingDown)
            {
                exportStatusLabel.Text = "正在取消輸出，完成後可關閉視窗。";
                _closeSaveInProgress = false;
                _closing = false;
                previewRenderTimer.Start();
                return;
            }
            while (_exportCancellation is not null && !IsDisposed)
                await Task.Delay(15);
        }

        if (_isDirty && !_shuttingDown)
        {
            var choice = MessageBox.Show(this, "Camera 動畫已變更，關閉前要儲存嗎？", "Camera 運鏡動畫",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (choice == DialogResult.Cancel)
            {
                _closeSaveInProgress = false;
                _closing = false;
                previewRenderTimer.Start();
                return;
            }
            if (choice == DialogResult.Yes && !await SaveAsync(forceChoosePath: false))
            {
                _closeSaveInProgress = false;
                _closing = false;
                previewRenderTimer.Start();
                return;
            }
        }

        if (!_shuttingDown)
        {
            var sceneChoice = MessageBox.Show(this,
                "是否將目前時間位置的 Camera、燈光與模型狀態更新到 MainForm？\n\n" +
                "選擇「否」將恢復開啟 Camera Animation 前的 MainForm 狀態。",
                "更新 MainForm 場景",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (sceneChoice == DialogResult.Cancel)
            {
                _closeSaveInProgress = false;
                _closing = false;
                previewRenderTimer.Start();
                RequestPreviewRender();
                return;
            }
            if (sceneChoice == DialogResult.Yes) CommitCurrentStateToMainForm();
            else RestoreMainSceneState();
        }

        await SaveDisplaySettingsAsync();
        _previewRenderDirty = false;
        _previewRenderCancellation?.Cancel();
        while (_previewRenderRunning && !IsDisposed)
            await Task.Delay(15);
        _allowClose = true;
        Close();
    }

    private async Task SaveDisplaySettingsAsync()
    {
        _userSettings.LastAnimationFilePath = _lastAnimationFilePath;
        _userSettings.ShowCamera = showCameraCheckBox.Checked;
        _userSettings.ShowTrajectory = showTrajectoryCheckBox.Checked;
        _userSettings.ShowAllLights = showAllLightsCheckBox.Checked;
        _userSettings.ShowModelTrajectory = showModelTrajectoryCheckBox.Checked;
        try
        {
            await _userSettings.SaveAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _context.SetStatus($"無法儲存 Camera 動畫顯示設定：{ex.Message}");
        }
    }

    internal void ShutdownEditor()
    {
        _shuttingDown = true;
        _exportCancellation?.Cancel();
        Close();
    }

    private void ViewportColorPreferences_Changed(object? sender, EventArgs e)
    {
        frontPictureBox.Invalidate();
        leftPictureBox.Invalidate();
        topPictureBox.Invalidate();
        previewPictureBox.Invalidate();
        RequestPreviewRender();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        StopPlayback(restoreCamera: true);
        previewRenderTimer.Stop();
        _previewRenderCancellation?.Cancel();
        _previewRenderCancellation?.Dispose();
        previewPictureBox.Image?.Dispose();
        frontPictureBox.Image?.Dispose();
        leftPictureBox.Image?.Dispose();
        topPictureBox.Image?.Dispose();
        foreach (var image in _editorBaseImages.Values) image.Dispose();
        _editorBaseImages.Clear();
        _exportCancellation?.Cancel();
        _exportCancellation = null;
        _context.ProjectChanged -= Context_ProjectChanged;
        _context.PreviewModeChanged -= Context_PreviewModeChanged;
        ViewportColorPreferences.Changed -= ViewportColorPreferences_Changed;
        base.OnFormClosed(e);
    }

    private static CameraState CloneCamera(CameraState camera) => new()
    {
        From = camera.From,
        To = camera.To,
        Up = camera.Up,
        RollDegrees = camera.RollDegrees,
        FieldOfViewDegrees = camera.FieldOfViewDegrees,
        NearPlane = camera.NearPlane,
        FarPlane = camera.FarPlane
    };

    private enum OrthographicDragTarget
    {
        None,
        Viewport,
        CameraPosition,
        CameraTarget,
        PositionBezierControl,
        TargetBezierControl,
        LightPosition,
        LightTarget,
        LightFallIn,
        LightFallOff,
        ModelPosition,
        ModelBezierControl
    }

    private sealed record ProjectedLightControl(
        SceneLight Light,
        OrthographicDragTarget Target,
        CameraEditorPoint Point,
        Vector3 WorldPoint);

    private sealed record ProjectedModelControl(
        SceneModel Model,
        CameraEditorPoint Position,
        CameraEditorPoint? BezierControl);

    private sealed record EditorHistoryState(
        string Description,
        CameraAnimationDocument Document,
        CameraState Camera,
        List<SceneLight> Lights,
        List<AnimationModelState> Models,
        Guid? SelectedKeyframeId,
        int SelectedLightIndex,
        Guid? SelectedModelId);

    private sealed record AnimationMeshTreeItem(SceneModel Model, int MeshIndex);
    private sealed record ProjectedModelBounds(SceneModel Model, RectangleF Bounds);

    private sealed class AnimationModelProperties(SceneModel model, Action changing)
    {
        private void Set(Action setter) { changing(); setter(); }

        [Category("物件"), DisplayName("Name")]
        public string Name { get => model.Name; set => Set(() => model.Name = string.IsNullOrWhiteSpace(value) ? "Model" : value.Trim()); }
        [Category("物件"), DisplayName("Visible")]
        public bool Visible { get => model.IsVisible; set => Set(() => model.IsVisible = value); }

        [Category("位置"), DisplayName("X")]
        public float PositionX { get => model.Transform.Position.X; set => Set(() => model.Transform.Position = model.Transform.Position with { X = value }); }
        [Category("位置"), DisplayName("Y")]
        public float PositionY { get => model.Transform.Position.Y; set => Set(() => model.Transform.Position = model.Transform.Position with { Y = value }); }
        [Category("位置"), DisplayName("Z")]
        public float PositionZ { get => model.Transform.Position.Z; set => Set(() => model.Transform.Position = model.Transform.Position with { Z = value }); }

        [Category("旋轉（度）"), DisplayName("X")]
        public float RotationX { get => model.Transform.RotationDegrees.X; set => Set(() => model.Transform.RotationDegrees = model.Transform.RotationDegrees with { X = value }); }
        [Category("旋轉（度）"), DisplayName("Y")]
        public float RotationY { get => model.Transform.RotationDegrees.Y; set => Set(() => model.Transform.RotationDegrees = model.Transform.RotationDegrees with { Y = value }); }
        [Category("旋轉（度）"), DisplayName("Z")]
        public float RotationZ { get => model.Transform.RotationDegrees.Z; set => Set(() => model.Transform.RotationDegrees = model.Transform.RotationDegrees with { Z = value }); }

        [Category("縮放"), DisplayName("X")]
        public float ScaleX { get => model.Transform.Scale.X; set => Set(() => model.Transform.Scale = model.Transform.Scale with { X = value }); }
        [Category("縮放"), DisplayName("Y")]
        public float ScaleY { get => model.Transform.Scale.Y; set => Set(() => model.Transform.Scale = model.Transform.Scale with { Y = value }); }
        [Category("縮放"), DisplayName("Z")]
        public float ScaleZ { get => model.Transform.Scale.Z; set => Set(() => model.Transform.Scale = model.Transform.Scale with { Z = value }); }
    }

    private sealed class AnimationLightProperties(SceneLight light, Action changing)
    {
        private void Set(Action setter) { changing(); setter(); light.Validate(); }

        [Category("燈光"), DisplayName("Name")]
        public string Name { get => light.Name; set => Set(() => light.Name = string.IsNullOrWhiteSpace(value) ? "Light" : value.Trim()); }
        [Category("燈光"), DisplayName("Enabled")]
        public bool Enabled { get => light.Enabled; set => Set(() => light.Enabled = value); }
        [Category("燈光"), DisplayName("Type")]
        public SceneLightType Type { get => light.Type; set => Set(() => light.Type = value); }

        [Category("位置"), DisplayName("Position X")]
        public float PositionX { get => light.Position.X; set => Set(() => light.Position = light.Position with { X = value }); }
        [Category("位置"), DisplayName("Position Y")]
        public float PositionY { get => light.Position.Y; set => Set(() => light.Position = light.Position with { Y = value }); }
        [Category("位置"), DisplayName("Position Z")]
        public float PositionZ { get => light.Position.Z; set => Set(() => light.Position = light.Position with { Z = value }); }

        [Category("方向"), DisplayName("Direction X")]
        public float DirectionX { get => light.Direction.X; set => Set(() => light.Direction = light.Direction with { X = value }); }
        [Category("方向"), DisplayName("Direction Y")]
        public float DirectionY { get => light.Direction.Y; set => Set(() => light.Direction = light.Direction with { Y = value }); }
        [Category("方向"), DisplayName("Direction Z")]
        public float DirectionZ { get => light.Direction.Z; set => Set(() => light.Direction = light.Direction with { Z = value }); }

        [Category("顏色與強度"), DisplayName("Color")]
        [Editor(typeof(CameraAnimationRichColorEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [TypeConverter(typeof(RichColorConverter))]
        public Color Color
        {
            get => Color.FromArgb(255,
                ToByte(light.Color.X), ToByte(light.Color.Y), ToByte(light.Color.Z));
            set => Set(() => light.Color = new Vector3(value.R / 255f, value.G / 255f, value.B / 255f));
        }
        [Category("顏色與強度"), DisplayName("Intensity")]
        public float Intensity { get => light.Intensity; set => Set(() => light.Intensity = Math.Max(0f, value)); }

        [Category("範圍"), DisplayName("Range")]
        public float Range { get => light.Range; set => Set(() => light.Range = Math.Max(0.01f, value)); }
        [Category("範圍"), DisplayName("Fall In（度）")]
        public float FallInDegrees
        {
            get => light.FallInDegrees;
            set => Set(() => light.FallInDegrees = Math.Clamp(value, 0.1f, Math.Max(0.1f, light.FallOffDegrees - 0.1f)));
        }
        [Category("範圍"), DisplayName("Fall Off（度）")]
        public float FallOffDegrees
        {
            get => light.FallOffDegrees;
            set => Set(() => light.FallOffDegrees = Math.Clamp(value, Math.Min(89.5f, light.FallInDegrees + 0.1f), 89.5f));
        }

        private static int ToByte(float value) => (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);
    }

    public sealed class CameraAnimationRichColorEditor : System.Drawing.Design.UITypeEditor
    {
        public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(
            ITypeDescriptorContext? context) => System.Drawing.Design.UITypeEditorEditStyle.Modal;

        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            var originalColor = value is Color color ? color : Color.White;
            var committedColor = originalColor;
            using var dialog = new RichColorPickerForm(originalColor);

            void Preview(Color previewColor)
            {
                if (context?.Instance is null || context.PropertyDescriptor is null) return;
                context.PropertyDescriptor.SetValue(context.Instance, previewColor);
                context.OnComponentChanged();
            }

            dialog.PreviewColorChanged += (_, _) => Preview(dialog.SelectedColor);
            dialog.ApplyRequested += (_, _) =>
            {
                Preview(dialog.SelectedColor);
                committedColor = dialog.SelectedColor;
            };
            if (dialog.ShowDialog(Form.ActiveForm) == DialogResult.OK)
                return dialog.SelectedColor;

            Preview(committedColor);
            return committedColor;
        }
    }

    private sealed class PreviewCameraProperties
    {
        public PreviewCameraProperties(CameraState camera) => SetFrom(camera);

        [Category("位置"), DisplayName("Position X")]
        public float FromX { get; set; }
        [Category("位置"), DisplayName("Position Y")]
        public float FromY { get; set; }
        [Category("位置"), DisplayName("Position Z")]
        public float FromZ { get; set; }
        [Category("目標"), DisplayName("Target X")]
        public float ToX { get; set; }
        [Category("目標"), DisplayName("Target Y")]
        public float ToY { get; set; }
        [Category("目標"), DisplayName("Target Z")]
        public float ToZ { get; set; }
        [Category("方向"), DisplayName("Up X")]
        public float UpX { get; set; }
        [Category("方向"), DisplayName("Up Y")]
        public float UpY { get; set; }
        [Category("方向"), DisplayName("Up Z")]
        public float UpZ { get; set; }
        [Browsable(false)]
        public float RollDegrees { get; set; }
        [Category("鏡頭"), DisplayName("Field of View")]
        public float FieldOfViewDegrees { get; set; }
        [Category("鏡頭"), DisplayName("Near Plane")]
        public float NearPlane { get; set; }
        [Category("鏡頭"), DisplayName("Far Plane")]
        public float FarPlane { get; set; }

        public void SetFrom(CameraState camera)
        {
            FromX = camera.From.X; FromY = camera.From.Y; FromZ = camera.From.Z;
            ToX = camera.To.X; ToY = camera.To.Y; ToZ = camera.To.Z;
            UpX = camera.Up.X; UpY = camera.Up.Y; UpZ = camera.Up.Z;
            RollDegrees = camera.RollDegrees;
            FieldOfViewDegrees = camera.FieldOfViewDegrees;
            NearPlane = camera.NearPlane;
            FarPlane = camera.FarPlane;
        }

        public CameraState ToCameraState() => new()
        {
            From = new Vector3(FromX, FromY, FromZ),
            To = new Vector3(ToX, ToY, ToZ),
            Up = new Vector3(UpX, UpY, UpZ),
            RollDegrees = RollDegrees,
            FieldOfViewDegrees = FieldOfViewDegrees,
            NearPlane = NearPlane,
            FarPlane = FarPlane
        };
    }
}
