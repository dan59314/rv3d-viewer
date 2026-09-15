using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Rv3dViewer.Core;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal sealed class RenderSceneSnapshot
{
    internal const int MaximumLights = 16;
    private RenderSceneSnapshot(
        RenderCamera camera,
        Vector4 backgroundColor,
        RenderEnvironment environment,
        RenderTriangle[] triangles,
        RenderMaterial[] materials,
        RenderLight[] lights,
        object accelerationCacheKey)
    {
        Camera = camera;
        BackgroundColor = backgroundColor;
        Environment = environment;
        Triangles = triangles;
        Materials = materials;
        Lights = lights;
        AccelerationCacheKey = accelerationCacheKey;
    }

    public RenderCamera Camera { get; }
    public Vector4 BackgroundColor { get; }
    public RenderEnvironment Environment { get; }
    public RenderTriangle[] Triangles { get; }
    public RenderMaterial[] Materials { get; }
    public RenderLight[] Lights { get; }
    internal object AccelerationCacheKey { get; }

    public RenderSceneSnapshot WithCamera(RenderCamera camera) =>
        new(camera, BackgroundColor, Environment, Triangles, Materials, Lights, AccelerationCacheKey);

    public static RenderSceneSnapshot Create(ViewerProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var triangles = new List<RenderTriangle>();
        var materials = new List<RenderMaterial>();
        var projectDirectory = string.IsNullOrWhiteSpace(project.ProjectFilePath)
            ? null
            : Path.GetDirectoryName(project.ProjectFilePath);

        foreach (var model in project.Models.Where(model => model.IsVisible))
        {
            var materialBase = materials.Count;
            var assetDirectory = !string.IsNullOrWhiteSpace(model.SourceFilePath)
                ? Path.GetDirectoryName(model.SourceFilePath)
                : projectDirectory;
            if (model.Materials.Count == 0)
                materials.Add(RenderMaterial.Default);
            else
                materials.AddRange(model.Materials.Select(material => RenderMaterial.Create(material, assetDirectory, projectDirectory)));

            foreach (var instance in SceneTraversal.GetMeshInstances(model))
            {
                if (model.HiddenMeshIndices.Contains(instance.MeshIndex)) continue;
                var mesh = model.Meshes[instance.MeshIndex];
                var world = SceneTraversal.GetMeshWorldTransform(model, instance);
                Matrix4x4.Invert(world, out var inverseWorld);
                var normalMatrix = Matrix4x4.Transpose(inverseWorld);
                var localMaterialIndex = model.Materials.Count == 0
                    ? 0
                    : Math.Clamp(mesh.MaterialIndex, 0, model.Materials.Count - 1);
                var materialIndex = materialBase + localMaterialIndex;

                for (var i = 0; i + 2 < mesh.Indices.Length; i += 3)
                {
                    var i0 = checked((int)mesh.Indices[i]);
                    var i1 = checked((int)mesh.Indices[i + 1]);
                    var i2 = checked((int)mesh.Indices[i + 2]);
                    if ((uint)i0 >= (uint)mesh.Positions.Length ||
                        (uint)i1 >= (uint)mesh.Positions.Length ||
                        (uint)i2 >= (uint)mesh.Positions.Length) continue;

                    var p0 = Vector3.Transform(mesh.Positions[i0], world);
                    var p1 = Vector3.Transform(mesh.Positions[i1], world);
                    var p2 = Vector3.Transform(mesh.Positions[i2], world);
                    var faceNormal = SafeNormalize(Vector3.Cross(p1 - p0, p2 - p0), Vector3.UnitY);
                    var n0 = TransformNormal(mesh.Normals, i0, normalMatrix, faceNormal);
                    var n1 = TransformNormal(mesh.Normals, i1, normalMatrix, faceNormal);
                    var n2 = TransformNormal(mesh.Normals, i2, normalMatrix, faceNormal);
                    var uv0 = Get(mesh.TextureCoordinates, i0, Vector2.Zero);
                    var uv1 = Get(mesh.TextureCoordinates, i1, Vector2.Zero);
                    var uv2 = Get(mesh.TextureCoordinates, i2, Vector2.Zero);
                    var calculatedTangent = CalculateTangent(p0, p1, p2, uv0, uv1, uv2, n0);
                    var t0 = TransformTangent(mesh.Tangents, i0, normalMatrix, n0, calculatedTangent);
                    var t1 = TransformTangent(mesh.Tangents, i1, normalMatrix, n1, calculatedTangent);
                    var t2 = TransformTangent(mesh.Tangents, i2, normalMatrix, n2, calculatedTangent);
                    triangles.Add(new RenderTriangle(p0, p1, p2, n0, n1, n2, uv0, uv1, uv2,
                        t0, t1, t2, materialIndex));
                }
            }
        }

        var camera = new RenderCamera(project.Camera.From, project.Camera.To, project.Camera.Up, project.Camera.FieldOfViewDegrees);
        var environmentPath = ResolvePath(project.Environment.Path, projectDirectory, projectDirectory);
        var environment = new RenderEnvironment(
            project.Environment.Enabled,
            environmentPath,
            project.Environment.Intensity,
            project.Environment.RotationDegrees * MathF.PI / 180F,
            project.Environment.ShowBackground,
            project.Environment.BackgroundBlur);
        var lights = project.Lights.Where(light => light.Enabled && light.Intensity > 0F)
            .Take(MaximumLights).Select(RenderLight.Create).ToArray();
        var background = project.RenderSettings.BackgroundColor;
        var linearBackgroundRgb = RenderColorPipeline.SrgbToLinear(new Vector3(background.X, background.Y, background.Z));
        var linearBackground = new Vector4(linearBackgroundRgb, background.W);
        return new RenderSceneSnapshot(camera, linearBackground, environment, triangles.ToArray(), materials.ToArray(),
            lights, new object());
    }

    private static Vector3 TransformNormal(Vector3[] normals, int index, Matrix4x4 matrix, Vector3 fallback) =>
        (uint)index < (uint)normals.Length
            ? SafeNormalize(Vector3.TransformNormal(normals[index], matrix), fallback)
            : fallback;

    private static Vector3 TransformTangent(Vector4[] tangents, int index, Matrix4x4 matrix,
        Vector3 normal, Vector3 fallback)
    {
        var tangent = (uint)index < (uint)tangents.Length
            ? SafeNormalize(Vector3.TransformNormal(new Vector3(tangents[index].X, tangents[index].Y, tangents[index].Z), matrix), fallback)
            : fallback;
        return SafeNormalize(tangent - normal * Vector3.Dot(tangent, normal), OrthonormalTangent(normal));
    }

    private static T Get<T>(T[] values, int index, T fallback) => (uint)index < (uint)values.Length ? values[index] : fallback;

    private static Vector3 CalculateTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector3 normal)
    {
        var edge1 = p1 - p0;
        var edge2 = p2 - p0;
        var duv1 = uv1 - uv0;
        var duv2 = uv2 - uv0;
        var determinant = duv1.X * duv2.Y - duv1.Y * duv2.X;
        if (MathF.Abs(determinant) < 0.000001F) return OrthonormalTangent(normal);
        return SafeNormalize((edge1 * duv2.Y - edge2 * duv1.Y) / determinant, OrthonormalTangent(normal));
    }

    internal static Vector3 OrthonormalTangent(Vector3 normal) =>
        SafeNormalize(Vector3.Cross(MathF.Abs(normal.Y) > 0.98F ? Vector3.UnitX : Vector3.UnitY, normal), Vector3.UnitX);

    internal static Vector3 SafeNormalize(Vector3 value, Vector3 fallback)
    {
        var scale = MathF.Max(MathF.Abs(value.X), MathF.Max(MathF.Abs(value.Y), MathF.Abs(value.Z)));
        if (!float.IsFinite(scale) || scale <= 0F) return fallback;
        return Vector3.Normalize(value / scale);
    }

    internal static string? ResolvePath(string? path, string? primaryDirectory, string? fallbackDirectory)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (Path.IsPathRooted(path)) return File.Exists(path) ? Path.GetFullPath(path) : null;
        foreach (var directory in new[] { primaryDirectory, fallbackDirectory }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var candidate = Path.GetFullPath(Path.Combine(directory!, path));
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }
}

internal readonly record struct RenderCamera(Vector3 From, Vector3 To, Vector3 Up, float FieldOfViewDegrees);

internal readonly record struct RenderEnvironment(bool Enabled, string? Path, float Intensity,
    float RotationRadians, bool ShowBackground, float BackgroundBlur);

internal readonly record struct RenderTriangle(
    Vector3 P0, Vector3 P1, Vector3 P2,
    Vector3 N0, Vector3 N1, Vector3 N2,
    Vector2 Uv0, Vector2 Uv1, Vector2 Uv2,
    Vector3 T0, Vector3 T1, Vector3 T2,
    int MaterialIndex);

internal sealed record RenderMaterial(
    Vector4 BaseColor,
    float Metallic,
    float Roughness,
    float NormalScale,
    float BumpScale,
    float AmbientOcclusion,
    Vector3 Emissive,
    float Opacity,
    bool DoubleSided,
    MaterialRenderMode RenderMode,
    float AlphaCutoff,
    float Transmission,
    float IndexOfRefraction,
    float Thickness,
    Vector3 AbsorptionColor,
    IReadOnlyDictionary<TextureSemantic, string> TexturePaths)
{
    public static RenderMaterial Default { get; } = new(Vector4.One, 0F, 0.7F, 1F, 1F, 1F, Vector3.Zero, 1F, false,
        MaterialRenderMode.Opaque, 0.5F, 0F, 1.5F, 0F, Vector3.One, new Dictionary<TextureSemantic, string>());

    public static RenderMaterial Create(PbrMaterial material, string? assetDirectory, string? projectDirectory)
    {
        material.Validate();
        var paths = new Dictionary<TextureSemantic, string>();
        foreach (var (semantic, slot) in material.Textures)
        {
            if (!slot.Enabled) continue;
            var resolved = RenderSceneSnapshot.ResolvePath(slot.Path, assetDirectory, projectDirectory);
            if (resolved is not null) paths[semantic] = resolved;
        }
        foreach (var (semantic, stack) in material.TextureStacks)
        {
            if (!stack.Enabled || !stack.Layers.Any(layer => layer.Enabled)) continue;
            var normalContainsHeight = semantic == TextureSemantic.Normal &&
                NormalStackContainsHeightMap(stack, assetDirectory, projectDirectory);
            paths[semantic] = BakeStack(stack, assetDirectory, projectDirectory, normalContainsHeight);
        }
        if (paths.TryGetValue(TextureSemantic.Normal, out var normalPath) && LooksLikeHeightMap(normalPath))
            paths[TextureSemantic.Normal] = BakeHeightMapAsNormal(normalPath);
        var bumpScale = material.TextureStacks.TryGetValue(TextureSemantic.Bump, out var bumpStack)
            ? bumpStack.Layers.LastOrDefault(layer => layer.Enabled)?.Bump.Strength ?? 1f : 1f;
        return new RenderMaterial(
            material.BaseColor, material.Metallic, material.Roughness, material.NormalScale, bumpScale, material.AmbientOcclusion,
            material.Emissive * material.EmissiveStrength, material.Opacity, material.DoubleSided, material.RenderMode,
            material.AlphaCutoff, material.Transmission, material.IndexOfRefraction, material.Thickness,
            material.AbsorptionColor, paths);
    }

    private static bool NormalStackContainsHeightMap(TextureStack stack,
        string? assetDirectory, string? projectDirectory)
    {
        foreach (var layer in stack.Layers.Where(layer => layer.Enabled &&
                     layer.Kind == TextureLayerKind.Image && !string.IsNullOrWhiteSpace(layer.Path)))
        {
            var resolved = RenderSceneSnapshot.ResolvePath(layer.Path, assetDirectory, projectDirectory);
            if (resolved is not null && LooksLikeHeightMap(resolved)) return true;
        }
        return false;
    }

    private static string BakeStack(TextureStack stack, string? assetDirectory, string? projectDirectory,
        bool treatNormalAsHeight = false)
    {
        string Resolve(string path) => RenderSceneSnapshot.ResolvePath(path, assetDirectory, projectDirectory) ?? path;
        // TextureStackComposer produces pixels in the orientation expected by
        // a direct OpenGL upload. The offline renderers subsequently apply
        // their normal top-left image to bottom-left UV conversion, so store
        // the baked image in conventional bitmap orientation first.
        const string cacheVersion = "TextureStackBakeV3-HeightAware";
        var fingerprint = cacheVersion + treatNormalAsHeight + JsonSerializer.Serialize(stack);
        foreach (var layer in stack.Layers)
        {
            foreach (var path in new[] { layer.Path, layer.Mask.Path }.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                var resolved = Resolve(path);
                fingerprint += File.Exists(resolved) ? $"|{resolved}:{File.GetLastWriteTimeUtc(resolved).Ticks}" : $"|{resolved}";
            }
        }
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rv3dViewer", "TextureStackCache");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"{stack.Semantic}_{hash}.png");
        if (File.Exists(filePath)) return filePath;
        var composeStack = stack;
        if (treatNormalAsHeight)
        {
            composeStack = stack.Clone();
            composeStack.Semantic = TextureSemantic.Bump;
        }
        using var bitmap = TextureStackComposer.Compose(composeStack, Resolve, 1024);
        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
        bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        return filePath;
    }

    private static bool LooksLikeHeightMap(string path)
    {
        try
        {
            using var source = new Bitmap(path);
            using var sample = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(sample))
                graphics.DrawImage(source, 0, 0, sample.Width, sample.Height);
            var difference = 0D;
            for (var y = 0; y < sample.Height; y++)
            for (var x = 0; x < sample.Width; x++)
            {
                var color = sample.GetPixel(x, y);
                difference += Math.Max(Math.Abs(color.R - color.G),
                    Math.Max(Math.Abs(color.R - color.B), Math.Abs(color.G - color.B)));
            }
            return difference / (sample.Width * sample.Height) <= 8D;
        }
        catch
        {
            return false;
        }
    }

    private static string BakeHeightMapAsNormal(string path)
    {
        const string cacheVersion = "AutoHeightNormalV1";
        var fullPath = Path.GetFullPath(path);
        var fingerprint = $"{cacheVersion}|{fullPath}|{File.GetLastWriteTimeUtc(fullPath).Ticks}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rv3dViewer", "TextureStackCache");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"AutoNormal_{hash}.png");
        if (File.Exists(filePath)) return filePath;

        using var sourceImage = Image.FromFile(fullPath);
        using var source = new Bitmap(sourceImage.Width, sourceImage.Height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(source)) graphics.DrawImageUnscaled(sourceImage, 0, 0);
        var rectangle = new Rectangle(0, 0, source.Width, source.Height);
        var sourceData = source.LockBits(rectangle, System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        byte[] sourcePixels;
        try
        {
            sourcePixels = new byte[Math.Abs(sourceData.Stride) * source.Height];
            System.Runtime.InteropServices.Marshal.Copy(sourceData.Scan0, sourcePixels, 0, sourcePixels.Length);
        }
        finally
        {
            source.UnlockBits(sourceData);
        }

        using var normal = new Bitmap(source.Width, source.Height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var normalData = normal.LockBits(rectangle, System.Drawing.Imaging.ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            var output = new byte[Math.Abs(normalData.Stride) * normal.Height];
            float HeightAt(int x, int y)
            {
                x = (x + source.Width) % source.Width;
                y = (y + source.Height) % source.Height;
                var offset = y * Math.Abs(sourceData.Stride) + x * 4;
                return (sourcePixels[offset + 2] * 0.2126F + sourcePixels[offset + 1] * 0.7152F +
                    sourcePixels[offset] * 0.0722F) / 255F;
            }

            const float strength = 2F;
            for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++)
            {
                var left = HeightAt(x - 1, y);
                var right = HeightAt(x + 1, y);
                var up = HeightAt(x, y - 1);
                var down = HeightAt(x, y + 1);
                var mapped = Vector3.Normalize(new Vector3(
                    -(right - left) * strength,
                    (up - down) * strength,
                    1F));
                var offset = y * Math.Abs(normalData.Stride) + x * 4;
                output[offset] = ToNormalByte(mapped.Z);
                output[offset + 1] = ToNormalByte(mapped.Y);
                output[offset + 2] = ToNormalByte(mapped.X);
                output[offset + 3] = 255;
            }
            System.Runtime.InteropServices.Marshal.Copy(output, 0, normalData.Scan0, output.Length);
        }
        finally
        {
            normal.UnlockBits(normalData);
        }
        normal.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        return filePath;
    }

    private static byte ToNormalByte(float value) =>
        (byte)Math.Clamp((int)MathF.Round((value * 0.5F + 0.5F) * 255F), 0, 255);
}

internal readonly record struct RenderLight(
    SceneLightType Type, Vector3 Position, Vector3 Direction, Vector3 Color,
    float Intensity, float Range, float InnerCos, float OuterCos)
{
    public static RenderLight Create(SceneLight light) => new(
        light.Type, light.Position, RenderSceneSnapshot.SafeNormalize(light.Direction, -Vector3.UnitY), light.Color,
        light.Intensity, light.Range,
        MathF.Cos(light.FallInDegrees * MathF.PI / 180F),
        MathF.Cos(light.FallOffDegrees * MathF.PI / 180F));
}
