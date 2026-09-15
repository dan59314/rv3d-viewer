namespace Rv3dViewer.Core;

public interface IModelImporter
{
    IReadOnlyCollection<string> SupportedExtensions { get; }
    Task<SceneModel> ImportAsync(string filePath, CancellationToken cancellationToken = default);
}

public interface IProjectStore
{
    Task SaveAsync(ViewerProject project, string projectFilePath, CancellationToken cancellationToken = default);
    Task<ViewerProject> LoadAsync(string projectFilePath, CancellationToken cancellationToken = default);
}

public interface IMaterialLibraryStore
{
    Task SaveAsync(IReadOnlyCollection<PbrMaterial> materials, string filePath, CancellationToken cancellationToken = default);
    Task<List<PbrMaterial>> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}

public interface ISceneRenderer : IDisposable
{
    void SetProject(ViewerProject project);
    void InvalidateScene();
    void FrameModel(SceneModel model);
}
