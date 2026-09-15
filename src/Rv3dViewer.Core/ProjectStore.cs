using System.Text.Json;

namespace Rv3dViewer.Core;

public sealed class JsonProjectStore : IProjectStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true
    };

    public async Task SaveAsync(ViewerProject project, string projectFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFilePath);
        var fullPath = Path.GetFullPath(projectFilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        project.FormatVersion = ViewerProject.CurrentFormatVersion;
        project.Name = Path.GetFileNameWithoutExtension(fullPath);
        foreach (var model in project.Models)
        {
            model.CaptureMeshMaterialIndices();
            model.CaptureProceduralGeometry();
        }
        await using var stream = File.Create(fullPath);
        await JsonSerializer.SerializeAsync(stream, project, Options, cancellationToken);
        project.ProjectFilePath = fullPath;
    }

    public async Task<ViewerProject> LoadAsync(string projectFilePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(projectFilePath);
        ViewerProject project;
        await using (var stream = new FileStream(
                         fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                         bufferSize: 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            project = await JsonSerializer.DeserializeAsync<ViewerProject>(stream, Options, cancellationToken)
                ?? throw new InvalidDataException("Project file is empty or invalid.");
        }

        // MaterialLibrary is deliberately ignored by the current project model.
        // Only legacy projects can contain it. Avoid retaining another complete
        // JSON document for current projects, which can exceed 1 GB when they
        // contain procedural geometry.
        if (project.FormatVersion <= 4)
        {
            await using var legacyStream = new FileStream(
                fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var document = await JsonDocument.ParseAsync(legacyStream, cancellationToken: cancellationToken);
            if (document.RootElement.TryGetProperty("materialLibrary", out var legacyLibrary))
                project.MaterialLibrary = legacyLibrary.Deserialize<List<PbrMaterial>>(Options) ?? [];
        }
        if (project.FormatVersion > ViewerProject.CurrentFormatVersion)
            throw new NotSupportedException($"Project format {project.FormatVersion} is newer than supported version {ViewerProject.CurrentFormatVersion}.");
        project.ProjectFilePath = fullPath;
        project.Camera.Validate();
        CameraController.UpdateUpFromRoll(project.Camera);
        project.Lights ??= SceneLight.CreateDefaultRig();
        foreach (var light in project.Lights) light.Validate();
        project.Environment ??= new EnvironmentSettings();
        project.Environment.Validate();
        project.Skyboxes ??= [];
        foreach (var skybox in project.Skyboxes) skybox.Validate();
        var visibleSkyboxFound = false;
        foreach (var skybox in project.Skyboxes)
        {
            if (!skybox.Show) continue;
            if (!visibleSkyboxFound)
                visibleSkyboxFound = true;
            else
                skybox.Show = false;
        }
        project.Extensions ??= [];
        project.MaterialLibrary ??= [];
        foreach (var material in project.MaterialLibrary) material.Validate();
        foreach (var model in project.Models)
        {
            model.Extensions ??= [];
            model.MeshTransforms ??= [];
            model.MeshMaterialIndices ??= [];
            model.SourceMeshIndices ??= [];
            model.EmbeddedNodes ??= [];
            model.EmbeddedMeshes ??= [];
            model.RestoreProceduralGeometry();
            foreach (var material in model.Materials)
            {
                if (project.FormatVersion < 3 && material.RenderMode == MaterialRenderMode.Auto)
                    material.RenderMode = material.Name.Contains("glass", StringComparison.OrdinalIgnoreCase) ||
                                          material.Name.Contains("玻璃", StringComparison.OrdinalIgnoreCase)
                        ? MaterialRenderMode.Glass
                        : material.RequiresAlphaBlending
                            ? MaterialRenderMode.Transparent
                            : MaterialRenderMode.Auto;
                material.Validate();
            }
        }
        return project;
    }
}


public sealed class JsonMaterialLibraryStore : IMaterialLibraryStore
{
    private const int CurrentFormatVersion = 1;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true
    };

    public async Task SaveAsync(
        IReadOnlyCollection<PbrMaterial> materials,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var fullPath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var document = new MaterialLibraryDocument
        {
            FormatVersion = CurrentFormatVersion,
            Materials = materials.Select(material => material.Clone()).ToList()
        };
        await using var stream = File.Create(fullPath);
        await JsonSerializer.SerializeAsync(stream, document, Options, cancellationToken);
    }

    public async Task<List<PbrMaterial>> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        await using var stream = File.OpenRead(Path.GetFullPath(filePath));
        var document = await JsonSerializer.DeserializeAsync<MaterialLibraryDocument>(stream, Options, cancellationToken)
            ?? throw new InvalidDataException("Material library file is empty or invalid.");
        if (document.FormatVersion > CurrentFormatVersion)
            throw new NotSupportedException($"Material library format {document.FormatVersion} is newer than supported version {CurrentFormatVersion}.");
        document.Materials ??= [];
        foreach (var material in document.Materials) material.Validate();
        return document.Materials;
    }

    private sealed class MaterialLibraryDocument
    {
        public MaterialLibraryDocument() { }
        public int FormatVersion { get; set; } = CurrentFormatVersion;
        public List<PbrMaterial> Materials { get; set; } = [];
    }
}
