using System.Drawing.Imaging;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rv3dViewer.Core;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.App;

public static class GlbExporter
{
    public static Task ExportAsync(ViewerProject project, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return Task.Run(() => new Writer(project, cancellationToken).Write(filePath), cancellationToken);
    }

    private sealed class Writer(ViewerProject project, CancellationToken cancellationToken)
    {
        private const int ArrayBuffer = 34962;
        private const int ElementArrayBuffer = 34963;
        private readonly MemoryStream _binary = new();
        private readonly List<Dictionary<string, object?>> _bufferViews = [];
        private readonly List<Dictionary<string, object?>> _accessors = [];
        private readonly List<Dictionary<string, object?>> _meshes = [];
        private readonly List<Dictionary<string, object?>> _nodes = [];
        private readonly List<Dictionary<string, object?>> _materials = [];
        private readonly List<Dictionary<string, object?>> _images = [];
        private readonly List<Dictionary<string, object?>> _textures = [];
        private readonly List<Dictionary<string, object?>> _samplers = [];
        private readonly Dictionary<PbrMaterial, int> _materialIndices = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, int> _imageIndices = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(string Path, TextureWrap Wrap, TextureFilter Filter), int> _textureIndices = new();
        private readonly Dictionary<(TextureWrap Wrap, TextureFilter Filter), int> _samplerIndices = [];
        private readonly HashSet<string> _extensionsUsed = [];

        public void Write(string filePath)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sceneNodes = BuildScene();
            if (sceneNodes.Count == 0)
                throw new InvalidOperationException("場景中沒有可輸出的可見 Mesh。");

            var root = new Dictionary<string, object?>
            {
                ["asset"] = new Dictionary<string, object?>
                {
                    ["version"] = "2.0",
                    ["generator"] = "Rv3d Viewer"
                },
                ["scene"] = 0,
                ["scenes"] = new[] { new Dictionary<string, object?> { ["nodes"] = sceneNodes } },
                ["nodes"] = _nodes,
                ["meshes"] = _meshes,
                ["materials"] = _materials,
                ["accessors"] = _accessors,
                ["bufferViews"] = _bufferViews,
                ["buffers"] = new[] { new Dictionary<string, object?> { ["byteLength"] = checked((int)_binary.Length) } }
            };
            if (_images.Count > 0) root["images"] = _images;
            if (_textures.Count > 0) root["textures"] = _textures;
            if (_samplers.Count > 0) root["samplers"] = _samplers;
            if (_extensionsUsed.Count > 0) root["extensionsUsed"] = _extensionsUsed.OrderBy(value => value).ToArray();

            var json = JsonSerializer.SerializeToUtf8Bytes(root, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            });
            var jsonLength = Align4(json.Length);
            var binaryLength = Align4(checked((int)_binary.Length));
            var totalLength = checked(12 + 8 + jsonLength + 8 + binaryLength);
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
            Directory.CreateDirectory(outputDirectory);
            var temporaryPath = Path.Combine(outputDirectory, $".{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");
            try
            {
                using (var output = File.Create(temporaryPath))
                using (var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: false))
                {
                    writer.Write(0x46546C67u);
                    writer.Write(2u);
                    writer.Write((uint)totalLength);
                    writer.Write((uint)jsonLength);
                    writer.Write(0x4E4F534Au);
                    writer.Write(json);
                    WritePadding(writer, jsonLength - json.Length, 0x20);
                    writer.Write((uint)binaryLength);
                    writer.Write(0x004E4942u);
                    _binary.Position = 0;
                    _binary.CopyTo(output);
                    WritePadding(writer, binaryLength - checked((int)_binary.Length), 0x00);
                }
                File.Move(temporaryPath, Path.GetFullPath(filePath), overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        private List<int> BuildScene()
        {
            var sceneNodes = new List<int>();
            foreach (var model in project.Models.Where(model => model.IsVisible))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var gltfMeshIndices = new Dictionary<int, int>();
                foreach (var instance in SceneTraversal.GetMeshInstances(model))
                {
                    if (model.HiddenMeshIndices.Contains(instance.MeshIndex)) continue;
                    var mesh = model.Meshes[instance.MeshIndex];
                    if (mesh.Positions.Length == 0 || mesh.Indices.Length == 0) continue;
                    if (!gltfMeshIndices.TryGetValue(instance.MeshIndex, out var gltfMeshIndex))
                    {
                        gltfMeshIndex = AddMesh(model, mesh);
                        gltfMeshIndices[instance.MeshIndex] = gltfMeshIndex;
                    }

                    var world = SceneTraversal.GetMeshWorldTransform(model, instance);
                    var node = new Dictionary<string, object?>
                    {
                        ["name"] = string.IsNullOrWhiteSpace(mesh.Name) ? model.Name : $"{model.Name} / {mesh.Name}",
                        ["mesh"] = gltfMeshIndex,
                        ["matrix"] = MatrixToGltf(world)
                    };
                    sceneNodes.Add(_nodes.Count);
                    _nodes.Add(node);
                }
            }
            return sceneNodes;
        }

        private int AddMesh(SceneModel model, MeshData mesh)
        {
            var attributes = new Dictionary<string, object?>
            {
                ["POSITION"] = AddVector3Accessor(mesh.Positions, includeBounds: true)
            };
            if (mesh.Normals.Length == mesh.Positions.Length)
                attributes["NORMAL"] = AddVector3Accessor(mesh.Normals, includeBounds: false);
            if (mesh.TextureCoordinates.Length == mesh.Positions.Length)
                attributes["TEXCOORD_0"] = AddVector2Accessor(mesh.TextureCoordinates.Select(uv => new Vector2(uv.X, 1f - uv.Y)).ToArray());
            if (mesh.Tangents.Length == mesh.Positions.Length)
                attributes["TANGENT"] = AddVector4Accessor(mesh.Tangents
                    .Select(tangent => new Vector4(tangent.X, tangent.Y, tangent.Z, -tangent.W))
                    .ToArray());

            var material = model.Materials.ElementAtOrDefault(mesh.MaterialIndex) ?? new PbrMaterial { Name = "Default" };
            var primitive = new Dictionary<string, object?>
            {
                ["attributes"] = attributes,
                ["indices"] = AddIndexAccessor(mesh.Indices),
                ["material"] = AddMaterial(material),
                ["mode"] = 4
            };
            var gltfMesh = new Dictionary<string, object?>
            {
                ["name"] = mesh.Name,
                ["primitives"] = new[] { primitive }
            };
            var index = _meshes.Count;
            _meshes.Add(gltfMesh);
            return index;
        }

        private int AddMaterial(PbrMaterial material)
        {
            if (_materialIndices.TryGetValue(material, out var existing)) return existing;
            material.Validate();
            var alpha = Math.Clamp(material.BaseColor.W * material.Opacity, 0f, 1f);
            var pbr = new Dictionary<string, object?>
            {
                ["baseColorFactor"] = new[] { material.BaseColor.X, material.BaseColor.Y, material.BaseColor.Z, alpha },
                ["metallicFactor"] = material.Metallic,
                ["roughnessFactor"] = material.Roughness
            };
            AddTextureInfo(material, TextureSemantic.BaseColor, pbr, "baseColorTexture");
            if (TryGetMatchingTexture(material, TextureSemantic.Metallic, TextureSemantic.Roughness, out var metallicRoughnessTexture))
                pbr["metallicRoughnessTexture"] = TextureInfo(metallicRoughnessTexture);

            var result = new Dictionary<string, object?>
            {
                ["name"] = material.Name,
                ["pbrMetallicRoughness"] = pbr,
                ["doubleSided"] = material.DoubleSided,
                ["emissiveFactor"] = new[] { material.Emissive.X, material.Emissive.Y, material.Emissive.Z }
            };
            AddTextureInfo(material, TextureSemantic.Normal, result, "normalTexture", (info) => info["scale"] = material.NormalScale);
            AddTextureInfo(material, TextureSemantic.AmbientOcclusion, result, "occlusionTexture", (info) => info["strength"] = material.AmbientOcclusion);
            AddTextureInfo(material, TextureSemantic.Emissive, result, "emissiveTexture");

            switch (material.RenderMode)
            {
                case MaterialRenderMode.Cutout:
                    result["alphaMode"] = "MASK";
                    result["alphaCutoff"] = material.AlphaCutoff;
                    break;
                case MaterialRenderMode.Transparent:
                case MaterialRenderMode.Glass:
                    result["alphaMode"] = "BLEND";
                    break;
                default:
                    result["alphaMode"] = material.RequiresAlphaBlending ? "BLEND" : "OPAQUE";
                    break;
            }

            var extensions = new Dictionary<string, object?>();
            if (material.IsGlass || material.Transmission > 0f && material.RenderMode == MaterialRenderMode.Transparent)
            {
                extensions["KHR_materials_transmission"] = new Dictionary<string, object?>
                {
                    ["transmissionFactor"] = material.Transmission
                };
                extensions["KHR_materials_ior"] = new Dictionary<string, object?>
                {
                    ["ior"] = material.IndexOfRefraction
                };
                extensions["KHR_materials_volume"] = new Dictionary<string, object?>
                {
                    ["thicknessFactor"] = material.Thickness,
                    ["attenuationColor"] = new[] { material.AbsorptionColor.X, material.AbsorptionColor.Y, material.AbsorptionColor.Z }
                };
                _extensionsUsed.UnionWith(["KHR_materials_transmission", "KHR_materials_ior", "KHR_materials_volume"]);
            }
            if (material.Dispersion > 0f)
            {
                extensions["KHR_materials_dispersion"] = new Dictionary<string, object?>
                {
                    ["dispersion"] = material.Dispersion
                };
                _extensionsUsed.Add("KHR_materials_dispersion");
            }
            if (material.EmissiveStrength != 1f && material.Emissive.LengthSquared() > 0f)
            {
                extensions["KHR_materials_emissive_strength"] = new Dictionary<string, object?>
                {
                    ["emissiveStrength"] = material.EmissiveStrength
                };
                _extensionsUsed.Add("KHR_materials_emissive_strength");
            }
            if (extensions.Count > 0) result["extensions"] = extensions;

            var index = _materials.Count;
            _materials.Add(result);
            _materialIndices[material] = index;
            return index;
        }

        private void AddTextureInfo(
            PbrMaterial material,
            TextureSemantic semantic,
            Dictionary<string, object?> target,
            string propertyName,
            Action<Dictionary<string, object?>>? configure = null)
        {
            if (!TryGetTexture(material, semantic, out var textureIndex)) return;
            var info = TextureInfo(textureIndex);
            configure?.Invoke(info);
            target[propertyName] = info;
        }

        private bool TryGetMatchingTexture(
            PbrMaterial material,
            TextureSemantic first,
            TextureSemantic second,
            out int textureIndex)
        {
            textureIndex = -1;
            if (!TryGetTextureSource(material, first, out var firstPath, out var firstSampling) ||
                !TryGetTextureSource(material, second, out var secondPath, out _)) return false;
            var path = firstPath.Equals(secondPath, StringComparison.OrdinalIgnoreCase)
                ? firstPath : PackMetallicRoughness(firstPath, secondPath);
            textureIndex = AddTexture(path, firstSampling.Wrap, firstSampling.Filter);
            return true;
        }

        private bool TryGetTexture(PbrMaterial material, TextureSemantic semantic, out int textureIndex)
        {
            textureIndex = -1;
            if (!TryGetTextureSource(material, semantic, out var path, out var sampling)) return false;
            textureIndex = AddTexture(path, sampling.Wrap, sampling.Filter);
            return true;
        }

        private bool TryGetTextureSource(PbrMaterial material, TextureSemantic semantic, out string path,
            out TextureSamplingSettings sampling)
        {
            material.Validate();
            if (material.TextureStacks.TryGetValue(semantic, out var stack) && stack.Enabled &&
                stack.Layers.Any(layer => layer.Enabled))
            {
                sampling = stack.Layers.Last(layer => layer.Enabled).Sampling;
                path = BakeStack(stack);
                return true;
            }
            if (material.Textures.TryGetValue(semantic, out var slot) && slot.Enabled)
            {
                path = ResolveAssetPath(slot.Path);
                sampling = new TextureSamplingSettings { Wrap = slot.Wrap, Filter = slot.Filter };
                return File.Exists(path);
            }
            path = string.Empty;
            sampling = new TextureSamplingSettings();
            return false;
        }

        private string BakeStack(TextureStack stack)
        {
            var fingerprint = JsonSerializer.Serialize(stack);
            foreach (var layer in stack.Layers)
                foreach (var source in new[] { layer.Path, layer.Mask.Path }.Where(value => !string.IsNullOrWhiteSpace(value)))
                {
                    var resolved = ResolveAssetPath(source);
                    fingerprint += File.Exists(resolved) ? $"|{resolved}:{File.GetLastWriteTimeUtc(resolved).Ticks}" : $"|{resolved}";
                }
            var directory = Path.Combine(Path.GetTempPath(), "Rv3dViewer", "GlbTextureBake");
            Directory.CreateDirectory(directory);
            // TextureStackComposer produces a bitmap laid out for direct OpenGL upload
            // (lower-left texture origin). glTF images use an upper-left origin, so the
            // baked pixels must be normalized before they are embedded in the GLB.
            var path = Path.Combine(directory, $"{stack.Semantic}_GltfTopLeftV1_{Hash(fingerprint)}.png");
            if (File.Exists(path)) return path;
            using var bitmap = TextureStackComposer.Compose(stack, ResolveAssetPath, 1024);
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            bitmap.Save(path, ImageFormat.Png);
            return path;
        }

        private static string PackMetallicRoughness(string metallicPath, string roughnessPath)
        {
            var fingerprint = $"{metallicPath}:{File.GetLastWriteTimeUtc(metallicPath).Ticks}|{roughnessPath}:{File.GetLastWriteTimeUtc(roughnessPath).Ticks}";
            var directory = Path.Combine(Path.GetTempPath(), "Rv3dViewer", "GlbTextureBake");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"MetallicRoughness_{Hash(fingerprint)}.png");
            if (File.Exists(path)) return path;
            using var metallicSource = new Bitmap(metallicPath);
            using var roughnessSource = new Bitmap(roughnessPath);
            using var metallic = new Bitmap(metallicSource, 512, 512);
            using var roughness = new Bitmap(roughnessSource, 512, 512);
            using var packed = new Bitmap(512, 512, PixelFormat.Format32bppArgb);
            for (var y = 0; y < 512; y++)
            for (var x = 0; x < 512; x++)
                packed.SetPixel(x, y, Color.FromArgb(255, 0, roughness.GetPixel(x, y).R, metallic.GetPixel(x, y).R));
            packed.Save(path, ImageFormat.Png);
            return path;
        }

        private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        private int AddTexture(string path, TextureWrap wrap, TextureFilter filter)
        {
            path = Path.GetFullPath(path);
            var key = (path, wrap, filter);
            if (_textureIndices.TryGetValue(key, out var existing)) return existing;
            var texture = new Dictionary<string, object?>
            {
                ["source"] = AddImage(path),
                ["sampler"] = AddSampler(wrap, filter)
            };
            var index = _textures.Count;
            _textures.Add(texture);
            _textureIndices[key] = index;
            return index;
        }

        private int AddImage(string path)
        {
            if (_imageIndices.TryGetValue(path, out var existing)) return existing;
            cancellationToken.ThrowIfCancellationRequested();
            byte[] bytes;
            string mimeType;
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension is ".png" or ".jpg" or ".jpeg")
            {
                bytes = File.ReadAllBytes(path);
                mimeType = extension == ".png" ? "image/png" : "image/jpeg";
            }
            else
            {
                using var bitmap = new Bitmap(path);
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                bytes = stream.ToArray();
                mimeType = "image/png";
            }
            var image = new Dictionary<string, object?>
            {
                ["name"] = Path.GetFileNameWithoutExtension(path),
                ["bufferView"] = AddBufferView(bytes, target: null),
                ["mimeType"] = mimeType
            };
            var index = _images.Count;
            _images.Add(image);
            _imageIndices[path] = index;
            return index;
        }

        private int AddSampler(TextureWrap wrap, TextureFilter filter)
        {
            var key = (wrap, filter);
            if (_samplerIndices.TryGetValue(key, out var existing)) return existing;
            var wrapValue = wrap switch
            {
                TextureWrap.ClampToEdge => 33071,
                TextureWrap.MirroredRepeat => 33648,
                _ => 10497
            };
            var (magFilter, minFilter) = filter switch
            {
                TextureFilter.Nearest => (9728, 9728),
                TextureFilter.Linear => (9729, 9729),
                _ => (9729, 9987)
            };
            var index = _samplers.Count;
            _samplers.Add(new Dictionary<string, object?>
            {
                ["magFilter"] = magFilter,
                ["minFilter"] = minFilter,
                ["wrapS"] = wrapValue,
                ["wrapT"] = wrapValue
            });
            _samplerIndices[key] = index;
            return index;
        }

        private int AddVector3Accessor(Vector3[] values, bool includeBounds)
        {
            var view = AddBufferView(writer =>
            {
                foreach (var value in values)
                {
                    writer.Write(value.X); writer.Write(value.Y); writer.Write(value.Z);
                }
            }, values.Length * 12, ArrayBuffer);
            var accessor = Accessor(view, 5126, values.Length, "VEC3");
            if (includeBounds)
            {
                var minimum = new Vector3(float.PositiveInfinity);
                var maximum = new Vector3(float.NegativeInfinity);
                foreach (var value in values) { minimum = Vector3.Min(minimum, value); maximum = Vector3.Max(maximum, value); }
                accessor["min"] = new[] { minimum.X, minimum.Y, minimum.Z };
                accessor["max"] = new[] { maximum.X, maximum.Y, maximum.Z };
            }
            return AddAccessor(accessor);
        }

        private int AddVector2Accessor(Vector2[] values)
        {
            var view = AddBufferView(writer =>
            {
                foreach (var value in values) { writer.Write(value.X); writer.Write(value.Y); }
            }, values.Length * 8, ArrayBuffer);
            return AddAccessor(Accessor(view, 5126, values.Length, "VEC2"));
        }

        private int AddVector4Accessor(Vector4[] values)
        {
            var view = AddBufferView(writer =>
            {
                foreach (var value in values)
                {
                    writer.Write(value.X); writer.Write(value.Y); writer.Write(value.Z); writer.Write(value.W);
                }
            }, values.Length * 16, ArrayBuffer);
            return AddAccessor(Accessor(view, 5126, values.Length, "VEC4"));
        }

        private int AddIndexAccessor(uint[] values)
        {
            var view = AddBufferView(writer => { foreach (var value in values) writer.Write(value); }, values.Length * 4, ElementArrayBuffer);
            return AddAccessor(Accessor(view, 5125, values.Length, "SCALAR"));
        }

        private int AddBufferView(byte[] bytes, int? target)
        {
            return AddBufferView(writer => writer.Write(bytes), bytes.Length, target);
        }

        private int AddBufferView(Action<BinaryWriter> write, int byteLength, int? target)
        {
            AlignBinary();
            var offset = checked((int)_binary.Position);
            using (var writer = new BinaryWriter(_binary, Encoding.UTF8, leaveOpen: true)) write(writer);
            var view = new Dictionary<string, object?>
            {
                ["buffer"] = 0,
                ["byteOffset"] = offset,
                ["byteLength"] = byteLength
            };
            if (target is not null) view["target"] = target.Value;
            var index = _bufferViews.Count;
            _bufferViews.Add(view);
            return index;
        }

        private void AlignBinary()
        {
            while ((_binary.Position & 3) != 0) _binary.WriteByte(0);
        }

        private static Dictionary<string, object?> Accessor(int bufferView, int componentType, int count, string type) => new()
        {
            ["bufferView"] = bufferView,
            ["byteOffset"] = 0,
            ["componentType"] = componentType,
            ["count"] = count,
            ["type"] = type
        };

        private int AddAccessor(Dictionary<string, object?> accessor)
        {
            var index = _accessors.Count;
            _accessors.Add(accessor);
            return index;
        }

        private static Dictionary<string, object?> TextureInfo(int index) => new() { ["index"] = index, ["texCoord"] = 0 };

        private string ResolveAssetPath(string path)
        {
            if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
            var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);
            return string.IsNullOrWhiteSpace(projectDirectory)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(projectDirectory, path));
        }

        private static float[] MatrixToGltf(Matrix4x4 value) =>
        [
            value.M11, value.M12, value.M13, value.M14,
            value.M21, value.M22, value.M23, value.M24,
            value.M31, value.M32, value.M33, value.M34,
            value.M41, value.M42, value.M43, value.M44
        ];

        private static int Align4(int value) => checked((value + 3) & ~3);

        private static void WritePadding(BinaryWriter writer, int count, byte value)
        {
            for (var index = 0; index < count; index++) writer.Write(value);
        }
    }
}
