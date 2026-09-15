using System.Numerics;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rv3dViewer.Core;

public sealed class ViewerProject
{
    public const int CurrentFormatVersion = 11;
    public int FormatVersion { get; set; } = CurrentFormatVersion;
    public string Name { get; set; } = "Untitled";
    public List<SceneModel> Models { get; set; } = [];
    public CameraState Camera { get; set; } = CameraState.Default;
    public RenderSettings RenderSettings { get; set; } = new();
    public List<SceneLight> Lights { get; set; } = SceneLight.CreateDefaultRig();
    public EnvironmentSettings Environment { get; set; } = new();
    public List<SkyboxSettings> Skyboxes { get; set; } = [];
    public Dictionary<string, JsonElement> Extensions { get; set; } = [];
    // Kept only as an in-memory migration target for projects created before the
    // material library became application-wide. New project files never contain it.
    [JsonIgnore]
    public List<PbrMaterial> MaterialLibrary { get; set; } = [];

    [JsonIgnore]
    public string? ProjectFilePath { get; set; }
}

public sealed class SceneModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Model";
    public string AssetPath { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public List<int> HiddenMeshIndices { get; set; } = [];
    public Dictionary<int, TransformState> MeshTransforms { get; set; } = [];
    public Dictionary<int, int> MeshMaterialIndices { get; set; } = [];
    public List<int> SourceMeshIndices { get; set; } = [];
    public bool SourceMeshIndicesCaptured { get; set; }
    public TransformState Transform { get; set; } = new();
    public bool IsProcedural { get; set; }
    public List<SceneNode> EmbeddedNodes { get; set; } = [];
    public List<MeshData> EmbeddedMeshes { get; set; } = [];
    [JsonIgnore]
    public List<SceneNode> Nodes { get; set; } = [];
    [JsonIgnore]
    public List<MeshData> Meshes { get; set; } = [];
    public List<PbrMaterial> Materials { get; set; } = [];
    public Dictionary<string, JsonElement> Extensions { get; set; } = [];

    [JsonIgnore]
    public string? SourceFilePath { get; set; }

    public TransformState GetOrCreateMeshTransform(int meshIndex)
    {
        if ((uint)meshIndex >= (uint)Meshes.Count)
            throw new ArgumentOutOfRangeException(nameof(meshIndex));
        if (!MeshTransforms.TryGetValue(meshIndex, out var transform))
        {
            transform = new TransformState();
            MeshTransforms[meshIndex] = transform;
        }
        return transform;
    }

    public void CaptureMeshMaterialIndices()
    {
        EnsureSourceMeshIndices();
        MeshMaterialIndices = Meshes
            .Select((mesh, meshIndex) => (meshIndex, mesh.MaterialIndex))
            .ToDictionary(item => item.meshIndex, item => item.MaterialIndex);
    }

    public void CaptureProceduralGeometry()
    {
        if (!IsProcedural) return;
        EmbeddedNodes = Nodes;
        EmbeddedMeshes = Meshes;
    }

    public void RestoreProceduralGeometry()
    {
        if (!IsProcedural) return;
        EmbeddedNodes ??= [];
        EmbeddedMeshes ??= [];
        Nodes = EmbeddedNodes;
        Meshes = EmbeddedMeshes;
        EnsureSourceMeshIndices();
        RestoreMeshMaterialIndices();
    }

    public void RestoreMeshMaterialIndices()
    {
        foreach (var (meshIndex, materialIndex) in MeshMaterialIndices)
            if ((uint)meshIndex < (uint)Meshes.Count)
                Meshes[meshIndex].MaterialIndex = materialIndex;
    }

    public void EnsureSourceMeshIndices()
    {
        if (!SourceMeshIndicesCaptured || SourceMeshIndices.Count != Meshes.Count)
            SourceMeshIndices = Enumerable.Range(0, Meshes.Count).ToList();
        SourceMeshIndicesCaptured = true;
    }

    public void RestoreImportedGeometry(SceneModel imported)
    {
        var sourceIndices = SourceMeshIndicesCaptured
            ? SourceMeshIndices
                .Where(index => (uint)index < (uint)imported.Meshes.Count)
                .Distinct()
                .ToList()
            : Enumerable.Range(0, imported.Meshes.Count).ToList();

        var newIndexBySourceIndex = sourceIndices
            .Select((sourceIndex, meshIndex) => (sourceIndex, meshIndex))
            .ToDictionary(item => item.sourceIndex, item => item.meshIndex);
        Meshes = sourceIndices.Select(index => imported.Meshes[index]).ToList();
        Nodes = imported.Nodes;
        foreach (var node in Nodes)
            RemapNodeMeshIndices(node, newIndexBySourceIndex);
        SourceMeshIndices = sourceIndices;
        SourceMeshIndicesCaptured = true;
        RestoreMeshMaterialIndices();
    }

    public void RemoveMeshes(IEnumerable<int> meshIndices)
    {
        EnsureSourceMeshIndices();
        var removed = meshIndices
            .Where(index => (uint)index < (uint)Meshes.Count)
            .Distinct()
            .OrderBy(index => index)
            .ToArray();
        if (removed.Length == 0) return;

        var removedSet = removed.ToHashSet();
        var newIndexByOldIndex = new Dictionary<int, int>();
        var newMeshes = new List<MeshData>(Meshes.Count - removed.Length);
        var newSourceMeshIndices = new List<int>(Meshes.Count - removed.Length);
        for (var oldIndex = 0; oldIndex < Meshes.Count; oldIndex++)
        {
            if (removedSet.Contains(oldIndex)) continue;
            newIndexByOldIndex[oldIndex] = newMeshes.Count;
            newMeshes.Add(Meshes[oldIndex]);
            newSourceMeshIndices.Add(SourceMeshIndices[oldIndex]);
        }

        Meshes = newMeshes;
        SourceMeshIndices = newSourceMeshIndices;
        HiddenMeshIndices = HiddenMeshIndices
            .Where(newIndexByOldIndex.ContainsKey)
            .Select(index => newIndexByOldIndex[index])
            .Distinct()
            .ToList();
        MeshTransforms = MeshTransforms
            .Where(pair => newIndexByOldIndex.ContainsKey(pair.Key))
            .ToDictionary(pair => newIndexByOldIndex[pair.Key], pair => pair.Value);
        foreach (var node in Nodes)
            RemapNodeMeshIndices(node, newIndexByOldIndex);
        CaptureMeshMaterialIndices();
    }

    private static void RemapNodeMeshIndices(SceneNode node, IReadOnlyDictionary<int, int> newIndexByOldIndex)
    {
        node.MeshIndices = node.MeshIndices
            .Where(newIndexByOldIndex.ContainsKey)
            .Select(index => newIndexByOldIndex[index])
            .ToList();
        foreach (var child in node.Children)
            RemapNodeMeshIndices(child, newIndexByOldIndex);
    }
}

public sealed class SceneNode
{
    public string Name { get; set; } = "Node";
    public Matrix4x4 LocalTransform { get; set; } = Matrix4x4.Identity;
    public List<int> MeshIndices { get; set; } = [];
    public List<SceneNode> Children { get; set; } = [];
}

public sealed class MeshData
{
    public string Name { get; set; } = "Mesh";
    public Vector3[] Positions { get; set; } = [];
    public Vector3[] Normals { get; set; } = [];
    public Vector2[] TextureCoordinates { get; set; } = [];
    public Vector4[] Tangents { get; set; } = [];
    public uint[] Indices { get; set; } = [];
    public int MaterialIndex { get; set; }

    [JsonIgnore]
    public int TriangleCount => Indices.Length / 3;
}

public sealed class TransformState
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 RotationDegrees { get; set; } = Vector3.Zero;
    public Vector3 Scale { get; set; } = Vector3.One;

    [JsonIgnore]
    public Matrix4x4 Matrix =>
        Matrix4x4.CreateScale(Scale) *
        Matrix4x4.CreateFromYawPitchRoll(
            DegreesToRadians(RotationDegrees.Y),
            DegreesToRadians(RotationDegrees.X),
            DegreesToRadians(RotationDegrees.Z)) *
        Matrix4x4.CreateTranslation(Position);

    private static float DegreesToRadians(float value) => value * MathF.PI / 180f;
}

public sealed class PbrMaterial
{
    public string Name { get; set; } = "Material";
    public Vector4 BaseColor { get; set; } = Vector4.One;
    public float Metallic { get; set; }
    public float Roughness { get; set; } = 0.7f;
    public float NormalScale { get; set; } = 1f;
    public float AmbientOcclusion { get; set; } = 1f;
    public Vector3 Emissive { get; set; } = Vector3.Zero;
    public float EmissiveStrength { get; set; } = 1f;
    public float Opacity { get; set; } = 1f;
    public bool DoubleSided { get; set; }
    public MaterialRenderMode RenderMode { get; set; } = MaterialRenderMode.Auto;
    public float AlphaCutoff { get; set; } = 0.5f;
    public float Transmission { get; set; } = 0.95f;
    public float IndexOfRefraction { get; set; } = 1.5f;
    public float Thickness { get; set; } = 0.05f;
    public float RefractionStrength { get; set; } = 0.04f;
    public float Dispersion { get; set; }
    public Vector3 AbsorptionColor { get; set; } = Vector3.One;
    public Dictionary<TextureSemantic, TextureSlot> Textures { get; set; } = [];
    public Dictionary<TextureSemantic, TextureStack> TextureStacks { get; set; } = [];

    [JsonIgnore]
    public bool RequiresAlphaBlending => RenderMode switch
    {
        MaterialRenderMode.Opaque or MaterialRenderMode.Cutout => false,
        MaterialRenderMode.Transparent or MaterialRenderMode.Glass => true,
        _ => Opacity < 0.999f ||
             BaseColor.W < 0.999f ||
             Textures.TryGetValue(TextureSemantic.Opacity, out var opacityTexture) && opacityTexture.Enabled
    };

    [JsonIgnore]
    public bool IsGlass => RenderMode == MaterialRenderMode.Glass;

    public void Validate()
    {
        Metallic = Math.Clamp(Metallic, 0f, 1f);
        Roughness = Math.Clamp(Roughness, 0f, 1f);
        Opacity = Math.Clamp(Opacity, 0f, 1f);
        AlphaCutoff = Math.Clamp(AlphaCutoff, 0f, 1f);
        Transmission = Math.Clamp(Transmission, 0f, 1f);
        IndexOfRefraction = Math.Clamp(IndexOfRefraction, 1f, 2.5f);
        Thickness = Math.Clamp(Thickness, 0f, 10f);
        RefractionStrength = Math.Clamp(RefractionStrength, 0f, 0.25f);
        Dispersion = Math.Clamp(Dispersion, 0f, 1f);
        AbsorptionColor = Vector3.Clamp(AbsorptionColor, Vector3.Zero, Vector3.One);
        Textures ??= [];
        TextureStacks ??= [];
        MigrateLegacyTextures();
        foreach (var stack in TextureStacks.Values) stack.Validate();
    }

    public TextureStack GetOrCreateTextureStack(TextureSemantic semantic)
    {
        TextureStacks ??= [];
        if (TextureStacks.TryGetValue(semantic, out var stack)) return stack;
        stack = new TextureStack { Semantic = semantic };
        if (Textures.TryGetValue(semantic, out var legacy) && !string.IsNullOrWhiteSpace(legacy.Path))
            stack.Layers.Add(TextureLayer.FromLegacy(legacy, semantic));
        TextureStacks[semantic] = stack;
        return stack;
    }

    private void MigrateLegacyTextures()
    {
        foreach (var (semantic, slot) in Textures)
        {
            if (TextureStacks.ContainsKey(semantic) || string.IsNullOrWhiteSpace(slot.Path)) continue;
            TextureStacks[semantic] = new TextureStack
            {
                Semantic = semantic,
                Enabled = slot.Enabled,
                Layers = [TextureLayer.FromLegacy(slot, semantic)]
            };
        }
    }

    public PbrMaterial Clone(string? name = null) => new()
    {
        Name = name ?? Name,
        BaseColor = BaseColor,
        Metallic = Metallic,
        Roughness = Roughness,
        NormalScale = NormalScale,
        AmbientOcclusion = AmbientOcclusion,
        Emissive = Emissive,
        EmissiveStrength = EmissiveStrength,
        Opacity = Opacity,
        DoubleSided = DoubleSided,
        RenderMode = RenderMode,
        AlphaCutoff = AlphaCutoff,
        Transmission = Transmission,
        IndexOfRefraction = IndexOfRefraction,
        Thickness = Thickness,
        RefractionStrength = RefractionStrength,
        Dispersion = Dispersion,
        AbsorptionColor = AbsorptionColor,
        Textures = Textures.ToDictionary(
            pair => pair.Key,
            pair => new TextureSlot
            {
                Path = pair.Value.Path,
                Enabled = pair.Value.Enabled,
                UvChannel = pair.Value.UvChannel,
                Wrap = pair.Value.Wrap,
                Filter = pair.Value.Filter
            }),
        TextureStacks = TextureStacks.ToDictionary(pair => pair.Key, pair => pair.Value.Clone())
    };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MaterialRenderMode
{
    Auto,
    Opaque,
    Cutout,
    Transparent,
    Glass
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextureSemantic
{
    BaseColor,
    Metallic,
    Roughness,
    Normal,
    Bump,
    AmbientOcclusion,
    Emissive,
    Opacity
}

public sealed class TextureSlot
{
    public string Path { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int UvChannel { get; set; }
    public TextureWrap Wrap { get; set; } = TextureWrap.Repeat;
    public TextureFilter Filter { get; set; } = TextureFilter.LinearMipmapLinear;
}

public sealed class TextureStack
{
    public TextureSemantic Semantic { get; set; }
    public bool Enabled { get; set; } = true;
    public List<TextureLayer> Layers { get; set; } = [];

    public void Validate()
    {
        Layers ??= [];
        if (Layers.Count > 8) Layers.RemoveRange(8, Layers.Count - 8);
        foreach (var layer in Layers) layer.Validate();
    }

    public TextureStack Clone() => new()
    {
        Semantic = Semantic,
        Enabled = Enabled,
        Layers = Layers.Select(layer => layer.Clone()).ToList()
    };
}

public sealed class TextureLayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "圖層";
    public TextureLayerKind Kind { get; set; } = TextureLayerKind.Image;
    public string Path { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public float Opacity { get; set; } = 1f;
    public TextureBlendMode BlendMode { get; set; } = TextureBlendMode.Normal;
    public Vector4 Color { get; set; } = Vector4.One;
    public TextureMappingSettings Mapping { get; set; } = new();
    public TextureTransformSettings Transform { get; set; } = new();
    public TextureSamplingSettings Sampling { get; set; } = new();
    public TextureChannelSettings Channels { get; set; } = new();
    public TextureMaskSettings Mask { get; set; } = new();
    public NormalTextureSettings Normal { get; set; } = new();
    public BumpTextureSettings Bump { get; set; } = new();

    public void Validate()
    {
        Name = string.IsNullOrWhiteSpace(Name) ? "圖層" : Name.Trim();
        Opacity = Math.Clamp(Opacity, 0f, 1f);
        Mapping ??= new();
        Transform ??= new();
        Sampling ??= new();
        Channels ??= new();
        Mask ??= new();
        Normal ??= new();
        Bump ??= new();
        Mapping.Validate();
        Transform.Validate();
        Sampling.Validate();
        Channels.Validate();
        Mask.Validate();
        Normal.Validate();
        Bump.Validate();
    }

    public TextureLayer Clone() => new()
    {
        Id = Guid.NewGuid(), Name = Name, Kind = Kind, Path = Path, Enabled = Enabled,
        Opacity = Opacity, BlendMode = BlendMode, Color = Color,
        Mapping = Mapping.Clone(), Transform = Transform.Clone(), Sampling = Sampling.Clone(),
        Channels = Channels.Clone(), Mask = Mask.Clone(), Normal = Normal.Clone(), Bump = Bump.Clone()
    };

    public static TextureLayer FromLegacy(TextureSlot slot, TextureSemantic semantic) => new()
    {
        Name = semantic.ToString(), Path = slot.Path, Enabled = slot.Enabled,
        Mapping = new TextureMappingSettings { UvChannel = slot.UvChannel },
        Sampling = new TextureSamplingSettings { Wrap = slot.Wrap, Filter = slot.Filter },
        Channels = new TextureChannelSettings
        {
            ColorSpace = semantic is TextureSemantic.BaseColor or TextureSemantic.Emissive
                ? TextureColorSpace.Srgb : TextureColorSpace.Linear
        }
    };
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class TextureMappingSettings
{
    public TextureMappingMode Mode { get; set; } = TextureMappingMode.Uv;
    public TextureProjectionSpace Space { get; set; } = TextureProjectionSpace.Object;
    public TextureProjectionAxis Axis { get; set; } = TextureProjectionAxis.Auto;
    public int UvChannel { get; set; }
    public float TriplanarBlend { get; set; } = 4f;
    public bool AutoFit { get; set; }
    public void Validate() { UvChannel = Math.Clamp(UvChannel, 0, 7); TriplanarBlend = Math.Clamp(TriplanarBlend, 0.1f, 32f); }
    public TextureMappingSettings Clone() => (TextureMappingSettings)MemberwiseClone();
    public override string ToString() => Mode.ToString();
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class TextureTransformSettings
{
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public float ScaleX { get; set; } = 1f;
    public float ScaleY { get; set; } = 1f;
    public bool LockAspectRatio { get; set; } = true;
    public float RotationDegrees { get; set; }
    public float PivotX { get; set; } = 0.5f;
    public float PivotY { get; set; } = 0.5f;
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }
    public bool SwapUv { get; set; }
    public void Validate()
    {
        ScaleX = Math.Clamp(ScaleX, -10000f, 10000f); ScaleY = Math.Clamp(ScaleY, -10000f, 10000f);
        if (Math.Abs(ScaleX) < 0.0001f) ScaleX = 0.0001f;
        if (Math.Abs(ScaleY) < 0.0001f) ScaleY = 0.0001f;
        RotationDegrees %= 360f;
    }
    public TextureTransformSettings Clone() => (TextureTransformSettings)MemberwiseClone();
    public override string ToString() => $"S({ScaleX:0.###}, {ScaleY:0.###}) O({OffsetX:0.###}, {OffsetY:0.###})";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class TextureSamplingSettings
{
    public TextureWrap Wrap { get; set; } = TextureWrap.Repeat;
    public TextureFilter Filter { get; set; } = TextureFilter.LinearMipmapLinear;
    public float Anisotropy { get; set; } = 4f;
    public void Validate() => Anisotropy = Math.Clamp(Anisotropy, 1f, 16f);
    public TextureSamplingSettings Clone() => (TextureSamplingSettings)MemberwiseClone();
    public override string ToString() => $"{Wrap}, {Filter}";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class TextureChannelSettings
{
    public TextureColorSpace ColorSpace { get; set; } = TextureColorSpace.Auto;
    public TextureChannel Source { get; set; } = TextureChannel.Rgba;
    public bool Invert { get; set; }
    public float Brightness { get; set; }
    public float Contrast { get; set; } = 1f;
    public float Gamma { get; set; } = 1f;
    public float HueDegrees { get; set; }
    public float Saturation { get; set; } = 1f;
    public Vector4 Tint { get; set; } = Vector4.One;
    public bool PremultipliedAlpha { get; set; }
    public void Validate()
    {
        Brightness = Math.Clamp(Brightness, -1f, 1f); Contrast = Math.Clamp(Contrast, 0f, 4f);
        Gamma = Math.Clamp(Gamma, 0.01f, 8f); HueDegrees %= 360f; Saturation = Math.Clamp(Saturation, 0f, 4f);
    }
    public TextureChannelSettings Clone() => (TextureChannelSettings)MemberwiseClone();
    public override string ToString() => $"{ColorSpace}, {Source}";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class TextureMaskSettings
{
    public bool Enabled { get; set; }
    public string Path { get; set; } = string.Empty;
    public TextureChannel Channel { get; set; } = TextureChannel.Luminance;
    public bool Invert { get; set; }
    public float Strength { get; set; } = 1f;
    public bool FollowLayerTransform { get; set; } = true;
    public TextureTransformSettings Transform { get; set; } = new();
    public void Validate() { Strength = Math.Clamp(Strength, 0f, 1f); Transform ??= new(); Transform.Validate(); }
    public TextureMaskSettings Clone() => new() { Enabled = Enabled, Path = Path, Channel = Channel, Invert = Invert, Strength = Strength, FollowLayerTransform = FollowLayerTransform, Transform = Transform.Clone() };
    public override string ToString() => Enabled ? (string.IsNullOrWhiteSpace(Path) ? "啟用" : System.IO.Path.GetFileName(Path)) : "停用";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class NormalTextureSettings
{
    public float Strength { get; set; } = 1f;
    public NormalMapConvention Convention { get; set; } = NormalMapConvention.OpenGl;
    public bool InvertX { get; set; }
    public bool InvertY { get; set; }
    public void Validate() => Strength = Math.Clamp(Strength, 0f, 8f);
    public NormalTextureSettings Clone() => (NormalTextureSettings)MemberwiseClone();
    public override string ToString() => $"{Convention}, {Strength:0.##}";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class BumpTextureSettings
{
    public float Strength { get; set; } = 1f;
    public float Bias { get; set; } = 0.5f;
    public bool Invert { get; set; }
    public TextureChannel Source { get; set; } = TextureChannel.Luminance;
    public void Validate() { Strength = Math.Clamp(Strength, 0f, 8f); Bias = Math.Clamp(Bias, 0f, 1f); }
    public BumpTextureSettings Clone() => (BumpTextureSettings)MemberwiseClone();
    public override string ToString() => $"{Source}, {Strength:0.##}";
}

[JsonConverter(typeof(JsonStringEnumConverter))] public enum TextureLayerKind { Image, SolidColor, Checker, Gradient, Noise }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum TextureBlendMode { Normal, Multiply, Add, Subtract, Screen, Overlay, Lighten, Darken }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum TextureMappingMode { Uv, Planar, Box, Triplanar }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum TextureProjectionSpace { Object, World }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum TextureProjectionAxis { Auto, X, Y, Z }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum TextureColorSpace { Auto, Srgb, Linear }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum TextureChannel { Rgba, Red, Green, Blue, Alpha, Luminance }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum NormalMapConvention { OpenGl, DirectX }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextureWrap { Repeat, ClampToEdge, MirroredRepeat }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextureFilter { Nearest, Linear, LinearMipmapLinear }

public sealed class CameraState
{
    private float _fieldOfViewDegrees = 60f;
    private float _rollDegrees;

    public Vector3 From { get; set; } = new(4f, 3f, 6f);
    public Vector3 To { get; set; } = Vector3.Zero;
    public Vector3 Up { get; set; } = Vector3.UnitY;
    public float RollDegrees
    {
        get => _rollDegrees;
        set => _rollDegrees = float.IsFinite(value) ? NormalizeDegrees(value) : 0f;
    }
    public float FieldOfViewDegrees
    {
        get => _fieldOfViewDegrees;
        set => _fieldOfViewDegrees = Math.Clamp(value, 5f, 120f);
    }
    public float NearPlane { get; set; } = 0.01f;
    public float FarPlane { get; set; } = 10000f;

    [JsonIgnore]
    public static CameraState Default => new();

    public void Validate()
    {
        if (Vector3.DistanceSquared(From, To) < 0.000001f)
            From = To + new Vector3(0f, 0f, 1f);
        NearPlane = Math.Max(0.0001f, NearPlane);
        FarPlane = Math.Max(NearPlane + 0.01f, FarPlane);
        FieldOfViewDegrees = FieldOfViewDegrees;
        RollDegrees = RollDegrees;
    }

    private static float NormalizeDegrees(float value)
    {
        value %= 360f;
        if (value >= 180f) value -= 360f;
        if (value < -180f) value += 360f;
        return value;
    }
}

public enum ModelDisplayMode
{
    Solid = 0,
    Wireframe = 1,
    Points = 2
}

public sealed class RenderSettings
{
    public Vector4 BackgroundColor { get; set; } = new(0.055f, 0.065f, 0.08f, 1f);
    public bool ShowTextures { get; set; } = true;
    public bool ShowGrid { get; set; } = true;
    public bool ShowWorldAxes { get; set; } = true;
    public bool ShowSelectionHighlight { get; set; } = true;
    public bool Wireframe { get; set; }
    public ModelDisplayMode ModelDisplayMode { get; set; } = ModelDisplayMode.Solid;
    public bool ShowCameraGizmo { get; set; }
    public bool ShowLightGizmos { get; set; }
    public int LightGizmoSizePixels { get; set; } = 30;
}

public sealed class EnvironmentSettings
{
    public bool Enabled { get; set; }
    public string Path { get; set; } = string.Empty;
    public float Intensity { get; set; } = 1f;
    public float RotationDegrees { get; set; }
    public bool ShowBackground { get; set; } = true;
    public float BackgroundBlur { get; set; }
    [JsonIgnore]
    public bool IsBackgroundVisible => Enabled && ShowBackground;

    public void Validate()
    {
        Intensity = Math.Clamp(Intensity, 0f, 20f);
        RotationDegrees %= 360f;
        if (RotationDegrees < 0f) RotationDegrees += 360f;
        BackgroundBlur = Math.Clamp(BackgroundBlur, 0f, 1f);
    }
}

public sealed class SkyboxSettings
{
    public string Name { get; set; } = "Skybox";
    public bool Show { get; set; }
    public bool UseAsEnvironment { get; set; }
    public float RotationDegrees { get; set; }
    public string SourcePanorama { get; set; } = string.Empty;
    public string PositiveX { get; set; } = string.Empty;
    public string NegativeX { get; set; } = string.Empty;
    public string PositiveY { get; set; } = string.Empty;
    public string NegativeY { get; set; } = string.Empty;
    public string PositiveZ { get; set; } = string.Empty;
    public string NegativeZ { get; set; } = string.Empty;

    [JsonIgnore]
    public IEnumerable<string> FacePaths =>
        [PositiveX, NegativeX, PositiveY, NegativeY, PositiveZ, NegativeZ];

    public void Validate()
    {
        Name = string.IsNullOrWhiteSpace(Name) ? "Skybox" : Name.Trim();
        RotationDegrees %= 360f;
        if (RotationDegrees < 0f) RotationDegrees += 360f;
    }
}

public sealed class SceneLight
{
    public string Name { get; set; } = "Light";
    public bool Enabled { get; set; } = true;
    public SceneLightType Type { get; set; } = SceneLightType.Directional;
    public Vector3 Position { get; set; } = new(0f, 5f, 0f);
    public Vector3 Direction { get; set; } = Vector3.Normalize(new Vector3(-0.5f, -1f, -0.35f));
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; } = 3f;
    public float Range { get; set; } = 20f;
    public float FallInDegrees { get; set; } = 20f;
    public float FallOffDegrees { get; set; } = 35f;

    public void Validate()
    {
        if (Direction.LengthSquared() < 0.000001f)
            Direction = new Vector3(0f, -1f, 0f);
        else
            Direction = Vector3.Normalize(Direction);
        Color = Vector3.Max(Color, Vector3.Zero);
        Intensity = Math.Max(0f, Intensity);
        Range = Math.Max(0.01f, Range);
        FallInDegrees = Math.Clamp(FallInDegrees, 0.1f, 89f);
        FallOffDegrees = Math.Clamp(FallOffDegrees, FallInDegrees, 89.5f);
    }

    public static List<SceneLight> CreateDefaultRig() =>
    [
        new()
        {
            Name = "Key Light",
            Position = new Vector3(-4f, 6f, 4f),
            Direction = Vector3.Normalize(new Vector3(-0.65f, -1f, -0.55f)),
            Color = new Vector3(1f, 0.96f, 0.9f),
            Intensity = 5f
        },
        new()
        {
            Name = "Fill Light",
            Position = new Vector3(4f, 4f, -3f),
            Direction = Vector3.Normalize(new Vector3(0.65f, -0.75f, 0.55f)),
            Color = new Vector3(0.78f, 0.88f, 1f),
            Intensity = 3.5f
        }
    ];
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SceneLightType
{
    Directional,
    Point,
    Spot
}
