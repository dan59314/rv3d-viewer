namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Text.Json;
using System.Text.Json.Serialization;
using Rv3dViewer.Core;

internal sealed class InteriorDesignProjectExtension
{
    public int SchemaVersion { get; set; } = InteriorDesignProjectStore.CurrentSchemaVersion;
    public string Unit { get; set; } = "meter";
    public Guid? SelectedModelId { get; set; }
    public int? SelectedMeshIndex { get; set; }
    public List<Guid> SelectedModelIds { get; set; } = [];
    public bool ShowModelEdges { get; set; } = true;
    public InteriorVisibilityMode VisibilityMode { get; set; } = InteriorVisibilityMode.AutoHideForegroundWalls;
    public bool PbrPreviewEnabled { get; set; }
    public CameraState TopCamera { get; set; } = CameraState.Default;
    public CameraState FrontCamera { get; set; } = CameraState.Default;
    public CameraState RightCamera { get; set; } = CameraState.Default;
    public CameraState PerspectiveCamera { get; set; } = CameraState.Default;
    public List<InteriorDesignProjectItem> Items { get; set; } = [];
}

internal sealed class InteriorDesignProjectItem
{
    public Guid Id { get; set; }
    public bool IsDefaultSceneObject { get; set; }
    public string ParameterType { get; set; } = string.Empty;
    public JsonElement Parameters { get; set; }
}

internal sealed record InteriorDesignProjectLoadResult(
    ViewerProject Project,
    IReadOnlyList<ParametricDesignObject> Objects,
    InteriorDesignProjectExtension Extension);

internal static class InteriorDesignProjectStore
{
    internal const int CurrentSchemaVersion = 1;
    internal const string ExtensionKey = RVInteriorDesignTestSceneFactory.ExtensionKey;

    private static readonly JsonSerializerOptions ExtensionOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    internal static ViewerProject CreateProject(
        string name,
        IEnumerable<ParametricDesignObject> objects,
        IEnumerable<Guid> selectedModelIds,
        Guid? selectedModelId,
        int? selectedMeshIndex,
        bool showModelEdges,
        InteriorVisibilityMode visibilityMode,
        bool pbrPreviewEnabled,
        CameraState topCamera,
        CameraState frontCamera,
        CameraState rightCamera,
        CameraState perspectiveCamera)
    {
        var objectArray = objects.ToArray();
        var extension = CreateExtension(objectArray, selectedModelIds, selectedModelId, selectedMeshIndex,
            showModelEdges, visibilityMode, pbrPreviewEnabled, topCamera, frontCamera, rightCamera,
            perspectiveCamera);
        var project = new ViewerProject
        {
            Name = string.IsNullOrWhiteSpace(name) ? "RV室內設計" : name,
            Models = objectArray.Select(item => item.Model).ToList(),
            Camera = CloneCamera(perspectiveCamera)
        };
        project.Extensions[ExtensionKey] = JsonSerializer.SerializeToElement(extension, ExtensionOptions);
        return project;
    }

    internal static JsonElement CreateExtensionElement(
        IEnumerable<ParametricDesignObject> objects,
        IEnumerable<Guid> selectedModelIds,
        Guid? selectedModelId,
        int? selectedMeshIndex,
        bool showModelEdges,
        InteriorVisibilityMode visibilityMode,
        bool pbrPreviewEnabled,
        CameraState topCamera,
        CameraState frontCamera,
        CameraState rightCamera,
        CameraState perspectiveCamera) =>
        JsonSerializer.SerializeToElement(
            CreateExtension(objects.ToArray(), selectedModelIds, selectedModelId, selectedMeshIndex,
                showModelEdges, visibilityMode, pbrPreviewEnabled, topCamera, frontCamera, rightCamera,
                perspectiveCamera), ExtensionOptions);

    internal static async Task SaveAsync(ViewerProject project, string path,
        CancellationToken cancellationToken = default) =>
        await new JsonProjectStore().SaveAsync(project, path, cancellationToken);

    internal static async Task<InteriorDesignProjectLoadResult> LoadAsync(string path,
        CancellationToken cancellationToken = default, IProgress<int>? progress = null)
    {
        var project = await new JsonProjectStore().LoadAsync(path, cancellationToken);
        if (!project.Extensions.TryGetValue(ExtensionKey, out var extensionElement))
            throw new InvalidDataException("此 .Rv3dPrj 沒有 RV室內設計參數資料，請使用 MainForm 開啟一般場景專案。");
        var extension = extensionElement.Deserialize<InteriorDesignProjectExtension>(ExtensionOptions)
                        ?? throw new InvalidDataException("RV室內設計擴充資料為空白。");
        if (extension.SchemaVersion is < 1 or > CurrentSchemaVersion)
            throw new NotSupportedException($"不支援 RV室內設計擴充格式版本 {extension.SchemaVersion}。");
        if (!string.Equals(extension.Unit, "meter", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"不支援室內設計單位：{extension.Unit}。");

        extension.Items ??= [];
        extension.SelectedModelIds ??= [];
        ResolveRelativeAssetPaths(project, extension, Path.GetDirectoryName(Path.GetFullPath(path))!);
        if (extension.Items.Count == 0 && project.Models.Count > 0)
            throw new InvalidDataException("此專案只有舊版 RV室內設計識別資料，沒有可編輯的參數定義。");
        var modelsById = project.Models
            .GroupBy(model => model.Id)
            .ToDictionary(group => group.Key, group => group.First());
        var session = new InteriorDesignSession
        {
            SelectedModelId = extension.SelectedModelId,
            SelectedMeshIndex = extension.SelectedMeshIndex,
            Items = extension.Items.Select(item => new InteriorDesignSessionItem
            {
                Id = item.Id,
                IsDefaultSceneObject = item.IsDefaultSceneObject,
                ParameterType = item.ParameterType,
                Parameters = item.Parameters,
                Model = modelsById.TryGetValue(item.Id, out var model)
                    ? model
                    : throw new InvalidDataException($"專案缺少室內設計模型：{item.Id}。")
            }).ToList()
        };
        var objects = InteriorDesignSessionStore.RestoreObjects(session, progress);
        return new InteriorDesignProjectLoadResult(project, objects, extension);
    }

    private static void ResolveRelativeAssetPaths(ViewerProject project,
        InteriorDesignProjectExtension extension, string projectDirectory)
    {
        static string Resolve(string value, string directory) =>
            string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value)
                ? value
                : Path.GetFullPath(Path.Combine(directory,
                    value.Replace('/', Path.DirectorySeparatorChar)));

        foreach (var model in project.Models)
        foreach (var material in model.Materials)
        {
            foreach (var slot in material.Textures.Values)
                slot.Path = Resolve(slot.Path, projectDirectory);
            foreach (var layer in material.TextureStacks.Values.SelectMany(stack => stack.Layers))
            {
                layer.Path = Resolve(layer.Path, projectDirectory);
                layer.Mask.Path = Resolve(layer.Mask.Path, projectDirectory);
            }
        }

        foreach (var item in extension.Items.Where(item =>
                     item.ParameterType == nameof(ImportedAssetParameters)))
        {
            var parameters = item.Parameters.Deserialize<ImportedAssetParameters>(ExtensionOptions);
            if (parameters is null) continue;
            parameters.ModelPath = Resolve(parameters.ModelPath, projectDirectory);
            item.Parameters = JsonSerializer.SerializeToElement(parameters, ExtensionOptions);
        }
    }

    private static InteriorDesignProjectExtension CreateExtension(
        IReadOnlyCollection<ParametricDesignObject> objects,
        IEnumerable<Guid> selectedModelIds,
        Guid? selectedModelId,
        int? selectedMeshIndex,
        bool showModelEdges,
        InteriorVisibilityMode visibilityMode,
        bool pbrPreviewEnabled,
        CameraState topCamera,
        CameraState frontCamera,
        CameraState rightCamera,
        CameraState perspectiveCamera)
    {
        var session = InteriorDesignSessionStore.CreateSession(objects, selectedModelId, selectedMeshIndex, false);
        return new InteriorDesignProjectExtension
        {
            SelectedModelId = selectedModelId,
            SelectedMeshIndex = selectedModelId is null ? null : selectedMeshIndex,
            SelectedModelIds = selectedModelIds.Distinct().ToList(),
            ShowModelEdges = showModelEdges,
            VisibilityMode = visibilityMode,
            PbrPreviewEnabled = pbrPreviewEnabled,
            TopCamera = CloneCamera(topCamera),
            FrontCamera = CloneCamera(frontCamera),
            RightCamera = CloneCamera(rightCamera),
            PerspectiveCamera = CloneCamera(perspectiveCamera),
            Items = session.Items.Select(item => new InteriorDesignProjectItem
            {
                Id = item.Id,
                IsDefaultSceneObject = item.IsDefaultSceneObject,
                ParameterType = item.ParameterType,
                Parameters = item.Parameters
            }).ToList()
        };
    }

    internal static CameraState CloneCamera(CameraState source) => new()
    {
        From = source.From,
        To = source.To,
        Up = source.Up,
        RollDegrees = source.RollDegrees,
        FieldOfViewDegrees = source.FieldOfViewDegrees,
        NearPlane = source.NearPlane,
        FarPlane = source.FarPlane
    };
}
