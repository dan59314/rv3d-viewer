using System.Numerics;
using Rv3dViewer.Core;
using Rv3dViewer.Plugin.Abstractions;

namespace Rv3dViewer.App.Plugins;

internal sealed class MainFormPluginContext(MainForm form) : IPluginContext, IPluginPreviewModeContext, ICameraService, IViewportCaptureService, IPluginProjectAssetService, IPluginIsolatedSceneService
{
    private ViewerProject _knownProject = form.PluginProject;
    private SceneModel? _knownModel = form.PluginSelectedModel;
    private int? _knownMeshIndex = form.PluginSelectedMeshIndex;
    private PbrMaterial? _knownMaterial = form.PluginSelectedMaterial;
    private SceneLight? _knownLight = form.PluginSelectedLight;

    public ViewerProject Project => form.PluginProject;
    public SceneModel? SelectedModel => form.PluginSelectedModel;
    public int? SelectedMeshIndex => form.PluginSelectedMeshIndex;
    public PbrMaterial? SelectedMaterial => form.PluginSelectedMaterial;
    public SceneLight? SelectedLight => form.PluginSelectedLight;
    public bool QuickPreviewEnabled => form.PluginQuickPreviewEnabled;
    public int PreviewMode => form.PluginPreviewMode;
    public ICameraService Camera => this;
    public IViewportCaptureService Viewport => this;
    public IPluginProjectAssetService ProjectAssets => this;
    CameraState ICameraService.Current => form.PluginProject.Camera;

    public event EventHandler? ProjectChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler? PreviewModeChanged;
    public event EventHandler? Changed;

    public void NotifySceneChanged() => form.ApplyPluginSceneChanges();
    public void InvalidateScene() => form.InvalidatePluginScene();
    public void MarkProjectModified() => form.MarkPluginProjectModified();
    public IPluginProjectEditTransaction BeginProjectEdit(string description) => form.BeginPluginProjectEdit(description);
    public void SetStatus(string message) => form.SetPluginStatus(message);

    public void ShowMessage(string message, string title = "外掛訊息", PluginMessageKind kind = PluginMessageKind.Information)
    {
        var icon = kind switch
        {
            PluginMessageKind.Warning => MessageBoxIcon.Warning,
            PluginMessageKind.Error => MessageBoxIcon.Error,
            _ => MessageBoxIcon.Information
        };
        MessageBox.Show(form, message, title, MessageBoxButtons.OK, icon);
    }

    public bool Confirm(string message, string title = "外掛確認") =>
        MessageBox.Show(form, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    string IPluginProjectAssetService.StageAsset(string sourceFilePath, PluginProjectAssetKind kind) =>
        ProjectAssetService.StagePluginAsset(sourceFilePath, kind);

    Task<ViewerProject> IPluginIsolatedSceneService.LoadProjectAsync(string filePath, CancellationToken cancellationToken) =>
        form.LoadPluginIsolatedProjectAsync(filePath, cancellationToken);

    ViewerProject IPluginIsolatedSceneService.CloneCurrentProject() => form.ClonePluginProject();

    Task<SceneModel> IPluginIsolatedSceneService.ImportGlbAsync(string filePath, CancellationToken cancellationToken) =>
        form.ImportPluginGlbAsync(filePath, cancellationToken);

    Task<byte[]> IPluginIsolatedSceneService.RenderPreviewPngAsync(ViewerProject project, CameraState camera,
        int width, int height, bool useQuickPreview, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(form.RenderPluginViewportPreview(project, camera, width, height, useQuickPreview));
    }

    Task<CameraEditorRenderResult> IPluginIsolatedSceneService.RenderCameraEditorViewPngAsync(ViewerProject project,
        CameraState camera, IReadOnlyList<CameraState> trajectory, IReadOnlyList<CameraState> draftTrajectory,
        IReadOnlyList<Vector3> editControlPoints, CameraEditorView view, int width, int height, float viewScale,
        Vector2 viewOffset, bool showCamera, bool showTrajectory, bool useQuickPreview,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(form.RenderPluginCameraEditorView(project, camera, trajectory, draftTrajectory,
            editControlPoints, view, width, height, viewScale, viewOffset, showCamera, showTrajectory, useQuickPreview));
    }

    internal void NotifyStateChanged()
    {
        if (!ReferenceEquals(_knownProject, form.PluginProject))
        {
            _knownProject = form.PluginProject;
            ProjectChanged?.Invoke(this, EventArgs.Empty);
        }

        if (!ReferenceEquals(_knownModel, form.PluginSelectedModel) ||
            _knownMeshIndex != form.PluginSelectedMeshIndex ||
            !ReferenceEquals(_knownMaterial, form.PluginSelectedMaterial) ||
            !ReferenceEquals(_knownLight, form.PluginSelectedLight))
        {
            _knownModel = form.PluginSelectedModel;
            _knownMeshIndex = form.PluginSelectedMeshIndex;
            _knownMaterial = form.PluginSelectedMaterial;
            _knownLight = form.PluginSelectedLight;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal void NotifyPreviewModeChanged() => PreviewModeChanged?.Invoke(this, EventArgs.Empty);

    internal void NotifyCameraChanged() => Changed?.Invoke(this, EventArgs.Empty);

    void ICameraService.Apply(CameraState state, CameraApplyMode mode)
    {
        ArgumentNullException.ThrowIfNull(state);
        form.ApplyPluginCamera(state, mode == CameraApplyMode.Commit);
        NotifyCameraChanged();
    }

    IDisposable ICameraService.AcquirePlaybackControl() => form.AcquirePluginCameraPlaybackControl();

    Task<byte[]> IViewportCaptureService.RenderPreviewPngAsync(
        CameraState camera,
        int width,
        int height,
        bool useQuickPreview,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(form.RenderPluginViewportPreview(camera, width, height, useQuickPreview));
    }

    Task<CameraEditorRenderResult> IViewportCaptureService.RenderCameraEditorViewPngAsync(
        CameraState camera,
        IReadOnlyList<CameraState> trajectory,
        IReadOnlyList<CameraState> draftTrajectory,
        IReadOnlyList<Vector3> editControlPoints,
        CameraEditorView view,
        int width,
        int height,
        float viewScale,
        Vector2 viewOffset,
        bool showCamera,
        bool showTrajectory,
        bool useQuickPreview,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(form.RenderPluginCameraEditorView(
            camera, trajectory, draftTrajectory, editControlPoints, view, width, height, viewScale, viewOffset,
            showCamera, showTrajectory, useQuickPreview));
    }

    Task IViewportCaptureService.CapturePngAsync(
        string filePath,
        int width,
        int height,
        bool includeEditorOverlays,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        form.CapturePluginViewportPng(filePath, width, height, includeEditorOverlays);
        return Task.CompletedTask;
    }
}
