namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Text.Json;
using System.Text.Json.Serialization;
using Rv3dViewer.Core;

internal sealed class InteriorDesignSession
{
    public int Version { get; set; } = InteriorDesignSessionStore.CurrentVersion;
    public bool HasUnexportedChanges { get; set; }
    public Guid? SelectedModelId { get; set; }
    public int? SelectedMeshIndex { get; set; }
    public string? ProjectFilePath { get; set; }
    public bool ProjectDirty { get; set; }
    public List<InteriorDesignSessionItem> Items { get; set; } = [];
}

internal sealed class InteriorDesignSessionItem
{
    public Guid Id { get; set; }
    public bool IsDefaultSceneObject { get; set; }
    public string ParameterType { get; set; } = string.Empty;
    public JsonElement Parameters { get; set; }
    public SceneModel Model { get; set; } = new();
}

internal sealed record InteriorDesignSessionLoadResult(
    InteriorDesignSession? Session,
    bool RecoveredFromBackup,
    string? ErrorMessage);

internal static class InteriorDesignSessionStore
{
    internal const int CurrentVersion = 2;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    internal static string SessionPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "RVInteriorDesign", "interior-session.json");

    private static string BackupPath => SessionPath + ".bak";

    internal static InteriorDesignSession CreateSession(
        IEnumerable<ParametricDesignObject> objects,
        Guid? selectedModelId,
        int? selectedMeshIndex,
        bool hasUnexportedChanges)
    {
        var session = new InteriorDesignSession
        {
            HasUnexportedChanges = hasUnexportedChanges,
            SelectedModelId = selectedModelId,
            SelectedMeshIndex = selectedModelId is null ? null : selectedMeshIndex
        };
        foreach (var item in objects)
        {
            item.Model.CaptureMeshMaterialIndices();
            item.Model.CaptureProceduralGeometry();
            session.Items.Add(new InteriorDesignSessionItem
            {
                Id = item.Id,
                IsDefaultSceneObject = item.IsDefaultSceneObject,
                ParameterType = item.Parameters.GetType().Name,
                Parameters = JsonSerializer.SerializeToElement(
                    item.Parameters, item.Parameters.GetType(), SerializerOptions),
                Model = item.Model
            });
        }
        return session;
    }

    internal static InteriorDesignSession CreateHistorySession(
        IEnumerable<ParametricDesignObject> objects,
        Guid? selectedModelId,
        int? selectedMeshIndex,
        bool hasUnexportedChanges)
    {
        var session = new InteriorDesignSession
        {
            HasUnexportedChanges = hasUnexportedChanges,
            SelectedModelId = selectedModelId,
            SelectedMeshIndex = selectedModelId is null ? null : selectedMeshIndex
        };
        foreach (var item in objects)
        {
            item.Model.CaptureMeshMaterialIndices();
            session.Items.Add(new InteriorDesignSessionItem
            {
                Id = item.Id,
                IsDefaultSceneObject = item.IsDefaultSceneObject,
                ParameterType = item.Parameters.GetType().Name,
                Parameters = JsonSerializer.SerializeToElement(
                    item.Parameters, item.Parameters.GetType(), SerializerOptions),
                Model = CreateHistoryModel(item.Model)
            });
        }
        return session;
    }

    private static SceneModel CreateHistoryModel(SceneModel source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        AssetPath = source.AssetPath,
        IsVisible = source.IsVisible,
        HiddenMeshIndices = [.. source.HiddenMeshIndices],
        MeshTransforms = source.MeshTransforms.ToDictionary(pair => pair.Key, pair => CopyTransform(pair.Value)),
        MeshMaterialIndices = new Dictionary<int, int>(source.MeshMaterialIndices),
        SourceMeshIndices = [.. source.SourceMeshIndices],
        SourceMeshIndicesCaptured = source.SourceMeshIndicesCaptured,
        Transform = CopyTransform(source.Transform),
        // Undo/Redo can regenerate geometry from the saved parameter definition. Keeping tens of
        // thousands of vertices in every transform snapshot made the first Gizmo movement fail.
        IsProcedural = true,
        EmbeddedNodes = [],
        EmbeddedMeshes = [],
        Materials = source.Materials,
        Extensions = new Dictionary<string, JsonElement>(source.Extensions)
    };

    private static TransformState CopyTransform(TransformState source) => new()
    {
        Position = source.Position,
        RotationDegrees = source.RotationDegrees,
        Scale = source.Scale
    };

    internal static void Save(InteriorDesignSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        session.Version = CurrentVersion;
        var directory = Path.GetDirectoryName(SessionPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = SessionPath + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None,
                       bufferSize: 64 * 1024, FileOptions.SequentialScan))
                JsonSerializer.Serialize(stream, session, SerializerOptions);
            if (File.Exists(SessionPath))
                File.Replace(temporaryPath, SessionPath, BackupPath, true);
            else
                File.Move(temporaryPath, SessionPath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    internal static string SerializeForHistory(InteriorDesignSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        session.Version = CurrentVersion;
        return JsonSerializer.Serialize(session, SerializerOptions);
    }

    internal static InteriorDesignSession DeserializeForHistory(string json) =>
        JsonSerializer.Deserialize<InteriorDesignSession>(json, SerializerOptions)
        ?? throw new InvalidDataException("Undo／Redo 工作階段內容為空白。");

    internal static InteriorDesignSessionLoadResult Load(IProgress<int>? progress = null)
    {
        if (!File.Exists(SessionPath))
            return new InteriorDesignSessionLoadResult(null, false, null);
        try
        {
            return new InteriorDesignSessionLoadResult(LoadFile(SessionPath, progress), false, null);
        }
        catch (Exception primaryException) when (IsSessionReadException(primaryException))
        {
            if (File.Exists(BackupPath))
            {
                try
                {
                    progress?.Report(0);
                    return new InteriorDesignSessionLoadResult(LoadFile(BackupPath, progress), true,
                        $"主要工作階段檔無法讀取，已從備份還原：{primaryException.Message}");
                }
                catch (Exception backupException) when (IsSessionReadException(backupException))
                {
                    return new InteriorDesignSessionLoadResult(null, false,
                        $"工作階段檔與備份均無法讀取。\r\n主要檔案：{primaryException.Message}\r\n備份：{backupException.Message}");
                }
            }
            return new InteriorDesignSessionLoadResult(null, false,
                $"工作階段檔無法讀取：{primaryException.Message}");
        }
    }

    internal static IReadOnlyList<ParametricDesignObject> RestoreObjects(InteriorDesignSession session,
        IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.Version is < 1 or > CurrentVersion)
            throw new NotSupportedException($"不支援工作階段格式版本 {session.Version}。");
        var ids = new HashSet<Guid>();
        var result = new List<ParametricDesignObject>(session.Items.Count);
        for (var itemIndex = 0; itemIndex < session.Items.Count; itemIndex++)
        {
            var saved = session.Items[itemIndex];
            if (saved.Id == Guid.Empty || !ids.Add(saved.Id))
                throw new InvalidDataException("工作階段包含空白或重複的模型 ID。");
            var parameters = DeserializeParameters(saved.ParameterType, saved.Parameters);
            var model = saved.Model ?? throw new InvalidDataException("工作階段缺少模型資料。");
            var hadEmbeddedGeometry = (model.EmbeddedMeshes?.Count ?? 0) > 0;
            EnsureModelGeometry(model, parameters, saved.Id);
            // Current and legacy session files already contain normalized geometry. Rebuilding
            // normals and tangents here needlessly scans every vertex and captures the same large
            // arrays again. Only regenerated lightweight models need the compatibility pass.
            if (!hadEmbeddedGeometry)
                InteriorMeshGeometry.Normalize(model);
            result.Add(new ParametricDesignObject
            {
                Id = saved.Id,
                Parameters = parameters,
                Model = model,
                IsDefaultSceneObject = saved.IsDefaultSceneObject
            });
            progress?.Report((itemIndex + 1) * 100 / Math.Max(1, session.Items.Count));
        }
        return result;
    }

    internal static void EnsureModelGeometry(SceneModel model, IInteriorParametricParameters parameters, Guid id)
    {
        model.Id = id;
        model.RestoreProceduralGeometry();
        if (model.Meshes.Count > 0) return;

        var generated = parameters.Generate(id);
        // Some early scene templates wrote an empty source-index list while still marking it as captured.
        // Treat that combination as missing metadata, otherwise all freshly imported meshes are filtered out.
        if (model.SourceMeshIndicesCaptured && model.SourceMeshIndices.Count == 0 && generated.Meshes.Count > 0)
            model.SourceMeshIndicesCaptured = false;
        model.RestoreImportedGeometry(generated);
    }

    private static InteriorDesignSession LoadFile(string path, IProgress<int>? progress)
    {
        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 64 * 1024, FileOptions.SequentialScan);
        using var stream = new ProgressReadStream(fileStream, progress);
        progress?.Report(0);
        var session = JsonSerializer.Deserialize<InteriorDesignSession>(stream, SerializerOptions)
                      ?? throw new InvalidDataException("工作階段檔內容為空白。");
        progress?.Report(100);
        session.Items ??= [];
        return session;
    }

    private sealed class ProgressReadStream(Stream inner, IProgress<int>? progress) : Stream
    {
        private int _lastReportedPercent = -1;

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = inner.Read(buffer, offset, count);
            ReportProgress();
            return bytesRead;
        }

        public override int Read(Span<byte> buffer)
        {
            var bytesRead = inner.Read(buffer);
            ReportProgress();
            return bytesRead;
        }

        public override int ReadByte()
        {
            var value = inner.ReadByte();
            ReportProgress();
            return value;
        }

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void Flush() => inner.Flush();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }

        private void ReportProgress()
        {
            if (progress is null) return;
            var percent = Length <= 0 ? 100 : (int)Math.Min(100, Position * 100L / Length);
            if (percent == _lastReportedPercent) return;
            _lastReportedPercent = percent;
            progress.Report(percent);
        }
    }

    private static IInteriorParametricParameters DeserializeParameters(string parameterType, JsonElement parameters) =>
        parameterType switch
        {
            nameof(ParametricPrimitiveParameters) => Deserialize<ParametricPrimitiveParameters>(parameters),
            nameof(ParametricRoomParameters) => Deserialize<ParametricRoomParameters>(parameters),
            nameof(ParametricWallParameters) => Deserialize<ParametricWallParameters>(parameters),
            nameof(ParametricSlabParameters) => Deserialize<ParametricSlabParameters>(parameters),
            nameof(ParametricBeamParameters) => Deserialize<ParametricBeamParameters>(parameters),
            nameof(ParametricColumnParameters) => Deserialize<ParametricColumnParameters>(parameters),
            nameof(ParametricDoorParameters) => Deserialize<ParametricDoorParameters>(parameters),
            nameof(ParametricWindowParameters) => Deserialize<ParametricWindowParameters>(parameters),
            nameof(ParametricStairParameters) => Deserialize<ParametricStairParameters>(parameters),
            nameof(ImportedAssetParameters) => Deserialize<ImportedAssetParameters>(parameters),
            _ => throw new NotSupportedException($"不支援參數模型類型：{parameterType}")
        };

    private static T Deserialize<T>(JsonElement element) where T : IInteriorParametricParameters =>
        element.Deserialize<T>(SerializerOptions)
        ?? throw new InvalidDataException($"無法還原參數模型：{typeof(T).Name}");

    private static bool IsSessionReadException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or
            NotSupportedException or OutOfMemoryException;
}
