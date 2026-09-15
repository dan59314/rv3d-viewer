namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Drawing.Imaging;
using System.Numerics;
using System.Collections.Concurrent;
using Assimp;
using Rv3dViewer.Core;

internal static class InteriorAssimpGltfImporter
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ImportLocks =
        new(StringComparer.OrdinalIgnoreCase);

    internal static async Task<SceneModel> ImportAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(filePath);
        var importLock = ImportLocks.GetOrAdd(fullPath, _ => new SemaphoreSlim(1, 1));
        await importLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() => Import(fullPath, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            importLock.Release();
        }
    }

    private static SceneModel Import(string filePath, CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(filePath), ".gltf", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("GLTF 匯入器只接受 .gltf 資產組。");
        using var context = new AssimpContext();
        var flags = PostProcessSteps.Triangulate |
                    PostProcessSteps.JoinIdenticalVertices |
                    PostProcessSteps.GenerateSmoothNormals |
                    PostProcessSteps.CalculateTangentSpace |
                    PostProcessSteps.SortByPrimitiveType |
                    PostProcessSteps.ImproveCacheLocality;
        var scene = context.ImportFile(filePath, flags)
                    ?? throw new InvalidDataException($"無法讀取 GLTF 模型：{filePath}");
        cancellationToken.ThrowIfCancellationRequested();

        var result = new SceneModel
        {
            Name = Path.GetFileNameWithoutExtension(filePath),
            SourceFilePath = Path.GetFullPath(filePath),
            AssetPath = Path.GetFileName(filePath)
        };
        var textureResolver = new ImportedTextureResolver(scene, filePath);
        foreach (var material in scene.Materials)
            result.Materials.Add(ConvertMaterial(material, Path.GetDirectoryName(filePath)!, textureResolver));
        if (result.Materials.Count == 0)
            result.Materials.Add(new PbrMaterial { Name = "Default" });

        foreach (var mesh in scene.Meshes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var data = new MeshData
            {
                Name = string.IsNullOrWhiteSpace(mesh.Name) ? $"Mesh {result.Meshes.Count + 1}" : mesh.Name,
                MaterialIndex = Math.Clamp(mesh.MaterialIndex, 0, result.Materials.Count - 1),
                Positions = mesh.Vertices.Select(value => new Vector3(value.X, value.Y, value.Z)).ToArray(),
                Normals = mesh.HasNormals
                    ? mesh.Normals.Select(value => new Vector3(value.X, value.Y, value.Z)).ToArray()
                    : [],
                TextureCoordinates = mesh.HasTextureCoords(0)
                    ? mesh.TextureCoordinateChannels[0].Select(value => new Vector2(value.X, value.Y)).ToArray()
                    : [],
                Tangents = mesh.HasTangentBasis
                    ? mesh.Tangents.Select((tangent, index) =>
                    {
                        var normal = mesh.Normals[index];
                        var bitangent = mesh.BiTangents[index];
                        var handedness = Vector3.Dot(
                            Vector3.Cross(new Vector3(normal.X, normal.Y, normal.Z),
                                new Vector3(tangent.X, tangent.Y, tangent.Z)),
                            new Vector3(bitangent.X, bitangent.Y, bitangent.Z)) < 0f ? -1f : 1f;
                        return new Vector4(tangent.X, tangent.Y, tangent.Z, handedness);
                    }).ToArray()
                    : [],
                Indices = mesh.Faces.SelectMany(face => face.Indices).Select(index => (uint)index).ToArray()
            };
            result.Meshes.Add(data);
            result.SourceMeshIndices.Add(result.SourceMeshIndices.Count);
        }
        result.Nodes.Add(ConvertNode(scene.RootNode));
        result.SourceMeshIndicesCaptured = true;
        return result;
    }

    private static SceneNode ConvertNode(Node node) => new()
    {
        Name = node.Name,
        LocalTransform = Matrix4x4.Transpose(node.Transform),
        MeshIndices = node.MeshIndices.ToList(),
        Children = node.Children.Select(ConvertNode).ToList()
    };

    private static PbrMaterial ConvertMaterial(Material source, string baseDirectory,
        ImportedTextureResolver textureResolver)
    {
        var result = new PbrMaterial { Name = string.IsNullOrWhiteSpace(source.Name) ? "Material" : source.Name };
        if (source.HasColorDiffuse) result.BaseColor = source.ColorDiffuse;
        if (source.HasOpacity) result.Opacity = Math.Clamp(source.Opacity, 0f, 1f);
        if (source.HasColorEmissive)
            result.Emissive = new Vector3(source.ColorEmissive.X, source.ColorEmissive.Y, source.ColorEmissive.Z);
        if (source.HasShininess)
            result.Roughness = Math.Clamp(MathF.Sqrt(2f / (source.Shininess + 2f)), 0f, 1f);
        if (!AddTexture(source, TextureType.BaseColor, TextureSemantic.BaseColor, result, baseDirectory, textureResolver))
            AddTexture(source, TextureType.Diffuse, TextureSemantic.BaseColor, result, baseDirectory, textureResolver);
        AddTexture(source, TextureType.Normals, TextureSemantic.Normal, result, baseDirectory, textureResolver);
        AddTexture(source, TextureType.Height, TextureSemantic.Bump, result, baseDirectory, textureResolver);
        AddTexture(source, TextureType.Emissive, TextureSemantic.Emissive, result, baseDirectory, textureResolver);
        AddTexture(source, TextureType.Opacity, TextureSemantic.Opacity, result, baseDirectory, textureResolver);
        if (!AddTexture(source, TextureType.AmbientOcclusion, TextureSemantic.AmbientOcclusion, result,
                baseDirectory, textureResolver))
            AddTexture(source, TextureType.Ambient, TextureSemantic.AmbientOcclusion, result,
                baseDirectory, textureResolver);
        if (!AddTexture(source, TextureType.Metalness, TextureSemantic.Metallic, result,
                baseDirectory, textureResolver, 2))
            AddTexture(source, TextureType.GltfMetallicRoughness, TextureSemantic.Metallic, result,
                baseDirectory, textureResolver, 2);
        if (!AddTexture(source, TextureType.Roughness, TextureSemantic.Roughness, result,
                baseDirectory, textureResolver, 1))
            AddTexture(source, TextureType.GltfMetallicRoughness, TextureSemantic.Roughness, result,
                baseDirectory, textureResolver, 1);
        result.RenderMode = result.Name.Contains("glass", StringComparison.OrdinalIgnoreCase) ||
                            result.Name.Contains("玻璃", StringComparison.Ordinal)
            ? MaterialRenderMode.Glass
            : result.RequiresAlphaBlending ? MaterialRenderMode.Transparent : MaterialRenderMode.Auto;
        result.Validate();
        return result;
    }

    private static bool AddTexture(Material source, TextureType type, TextureSemantic semantic,
        PbrMaterial target, string baseDirectory, ImportedTextureResolver resolver, int? sourceChannel = null)
    {
        if (!source.GetMaterialTexture(type, 0, out var texture) || string.IsNullOrWhiteSpace(texture.FilePath))
            return false;
        var resolvedPath = resolver.Resolve(texture.FilePath, baseDirectory, sourceChannel);
        if (string.IsNullOrWhiteSpace(resolvedPath))
            return false;
        target.Textures[semantic] = new Rv3dViewer.Core.TextureSlot
            { Path = resolvedPath, UvChannel = texture.UVIndex };
        return true;
    }

    private sealed class ImportedTextureResolver(Scene scene, string modelPath)
    {
        private readonly Dictionary<string, string> _resolved = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _textureDirectory = Path.Combine(Path.GetDirectoryName(modelPath)!,
            $"{Path.GetFileNameWithoutExtension(modelPath)}.rv3d-assets", "Textures");

        internal string? Resolve(string texturePath, string baseDirectory, int? sourceChannel = null)
        {
            var cacheKey = sourceChannel is null ? texturePath : $"{texturePath}|channel:{sourceChannel}";
            if (_resolved.TryGetValue(cacheKey, out var existing)) return existing;
            if (sourceChannel is not null)
            {
                var sourcePath = Resolve(texturePath, baseDirectory);
                if (string.IsNullOrWhiteSpace(sourcePath)) return null;
                var channelPath = Path.Combine(Path.GetDirectoryName(sourcePath)!,
                    $"{Path.GetFileNameWithoutExtension(sourcePath)}-{ChannelName(sourceChannel.Value)}.png");
                ExtractChannel(sourcePath, channelPath, sourceChannel.Value);
                return _resolved[cacheKey] = Path.GetFullPath(channelPath);
            }
            var embedded = scene.GetEmbeddedTexture(texturePath);
            if (embedded is null)
            {
                var externalPath = Path.GetFullPath(Path.Combine(baseDirectory,
                    texturePath.Replace('/', Path.DirectorySeparatorChar)));
                return File.Exists(externalPath) ? _resolved[cacheKey] = externalPath : null;
            }
            Directory.CreateDirectory(_textureDirectory);
            var index = scene.Textures.IndexOf(embedded);
            var originalName = MakeSafeFileName(Path.GetFileNameWithoutExtension(embedded.Filename));
            if (string.IsNullOrWhiteSpace(originalName)) originalName = $"embedded-{Math.Max(0, index)}";
            var destination = Path.Combine(_textureDirectory,
                $"{Math.Max(0, index):D3}-{originalName}.{NormalizeImageExtension(embedded.CompressedFormatHint)}");
            if (embedded.IsCompressed && embedded.HasCompressedData)
                File.WriteAllBytes(destination, embedded.CompressedData);
            else if (embedded.HasNonCompressedData)
            {
                destination = Path.ChangeExtension(destination, ".png");
                using var bitmap = new Bitmap(embedded.Width, embedded.Height, PixelFormat.Format32bppArgb);
                for (var y = 0; y < embedded.Height; y++)
                for (var x = 0; x < embedded.Width; x++)
                {
                    var texel = embedded.NonCompressedData[y * embedded.Width + x];
                    bitmap.SetPixel(x, y, Color.FromArgb(texel.A, texel.R, texel.G, texel.B));
                }
                bitmap.Save(destination, ImageFormat.Png);
            }
            else return null;
            return _resolved[cacheKey] = Path.GetFullPath(destination);
        }

        private static void ExtractChannel(string sourcePath, string destinationPath, int channel)
        {
            if (File.Exists(destinationPath) && new FileInfo(destinationPath).Length > 0)
                return;
            using var source = new Bitmap(sourcePath);
            using var output = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++)
            {
                var color = source.GetPixel(x, y);
                var value = channel switch { 1 => color.G, 2 => color.B, 3 => color.A, _ => color.R };
                output.SetPixel(x, y, Color.FromArgb(255, value, value, value));
            }
            var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp.png";
            try
            {
                output.Save(temporaryPath, ImageFormat.Png);
                File.Move(temporaryPath, destinationPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        private static string ChannelName(int channel) => channel switch { 1 => "g", 2 => "b", 3 => "a", _ => "r" };
        private static string NormalizeImageExtension(string? hint) => hint?.Trim().TrimStart('.').ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "jpg", "bmp" => "bmp", "tga" => "tga", "webp" => "webp", _ => "png"
        };
        private static string MakeSafeFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value.Trim();
        }
    }
}
