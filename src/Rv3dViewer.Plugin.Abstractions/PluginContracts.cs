using System.Numerics;
using Rv3dViewer.Core;

namespace Rv3dViewer.Plugin.Abstractions;

public static class PluginApi
{
    public const string Version = "1.0";
}

public interface IRv3dPlugin
{
    void Initialize(IPluginContext context);
    IReadOnlyCollection<PluginCommand> GetCommands();
    void Shutdown();
}

public interface IPluginContext
{
    ViewerProject Project { get; }
    SceneModel? SelectedModel { get; }
    int? SelectedMeshIndex { get; }
    PbrMaterial? SelectedMaterial { get; }
    SceneLight? SelectedLight { get; }
    bool QuickPreviewEnabled { get; }
    ICameraService Camera { get; }
    IViewportCaptureService Viewport { get; }
    IPluginProjectAssetService ProjectAssets { get; }

    event EventHandler? ProjectChanged;
    event EventHandler? SelectionChanged;
    event EventHandler? PreviewModeChanged;

    void NotifySceneChanged();
    void InvalidateScene();
    void MarkProjectModified();
    IPluginProjectEditTransaction BeginProjectEdit(string description);
    void SetStatus(string message);
    void ShowMessage(string message, string title = "外掛訊息", PluginMessageKind kind = PluginMessageKind.Information);
    bool Confirm(string message, string title = "外掛確認");
}

/// <summary>
/// Optional extension for hosts that expose the complete viewport preview mode.
/// It remains separate from IPluginContext for compatibility with existing plug-ins.
/// </summary>
public interface IPluginPreviewModeContext
{
    int PreviewMode { get; }
}

public interface IPluginProjectEditTransaction : IDisposable
{
    bool IsCommitted { get; }
    void Commit(Guid? selectedModelId = null);
}

public interface IPluginProjectAssetService
{
    string StageAsset(string sourceFilePath, PluginProjectAssetKind kind);
}

/// <summary>Optional host service for plug-ins that edit an isolated scene.</summary>
public interface IPluginIsolatedSceneService
{
    ViewerProject CloneCurrentProject();
    Task<ViewerProject> LoadProjectAsync(string filePath, CancellationToken cancellationToken = default);
    Task<SceneModel> ImportGlbAsync(string filePath, CancellationToken cancellationToken = default);
    Task<byte[]> RenderPreviewPngAsync(ViewerProject project, CameraState camera, int width, int height,
        bool useQuickPreview = false, CancellationToken cancellationToken = default);
    Task<CameraEditorRenderResult> RenderCameraEditorViewPngAsync(ViewerProject project, CameraState camera,
        IReadOnlyList<CameraState> trajectory, IReadOnlyList<CameraState> draftTrajectory,
        IReadOnlyList<Vector3> editControlPoints, CameraEditorView view, int width, int height,
        float viewScale, Vector2 viewOffset, bool showCamera, bool showTrajectory,
        bool useQuickPreview = false, CancellationToken cancellationToken = default);
}

public enum PluginProjectAssetKind
{
    Model,
    Texture,
    License
}

public interface ICameraService
{
    CameraState Current { get; }
    event EventHandler? Changed;

    void Apply(CameraState state, CameraApplyMode mode = CameraApplyMode.TransientPreview);
    IDisposable AcquirePlaybackControl();
}

public enum CameraApplyMode
{
    TransientPreview,
    Commit
}

public interface IViewportCaptureService
{
    Task<byte[]> RenderPreviewPngAsync(
        CameraState camera,
        int width,
        int height,
        bool useQuickPreview = false,
        CancellationToken cancellationToken = default);

    Task<CameraEditorRenderResult> RenderCameraEditorViewPngAsync(
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
        bool useQuickPreview = false,
        CancellationToken cancellationToken = default);

    Task CapturePngAsync(
        string filePath,
        int width,
        int height,
        bool includeEditorOverlays = false,
        CancellationToken cancellationToken = default);
}

public sealed record CameraEditorPoint(float X, float Y);
public sealed record CameraEditorRenderResult(
    byte[] Png,
    IReadOnlyList<CameraEditorPoint> EditControlPoints,
    CameraEditorPoint CameraPosition,
    CameraEditorPoint CameraTarget,
    float HorizontalUnitsPerPixel,
    float VerticalUnitsPerPixel);

public enum CameraEditorView
{
    Front,
    Left,
    Top
}

public sealed record PluginCommand(
    string Id,
    string Text,
    Func<CancellationToken, Task> ExecuteAsync,
    int Order = 100,
    Func<bool>? CanExecute = null);

public enum PluginMessageKind
{
    Information,
    Warning,
    Error
}
