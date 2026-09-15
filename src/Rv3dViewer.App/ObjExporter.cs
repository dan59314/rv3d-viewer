using System.Drawing.Imaging;
using System.Globalization;
using System.Numerics;
using System.Text;
using Rv3dViewer.Core;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.App;

public static class ObjExporter
{
    public static string GetOutputDirectory(string requestedFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedFilePath);
        var fullPath = Path.GetFullPath(requestedFilePath);
        return Path.Combine(Path.GetDirectoryName(fullPath)!, Path.GetFileNameWithoutExtension(fullPath));
    }

    public static string GetOutputFilePath(string requestedFilePath)
    {
        var directory = GetOutputDirectory(requestedFilePath);
        var baseName = Path.GetFileNameWithoutExtension(requestedFilePath);
        return Path.Combine(directory, $"{baseName}.obj");
    }

    public static Task ExportAsync(ViewerProject project, string requestedFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedFilePath);
        return Task.Run(() => new Writer(project, cancellationToken).Write(requestedFilePath), cancellationToken);
    }

    private sealed class Writer(ViewerProject project, CancellationToken cancellationToken)
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private readonly Dictionary<PbrMaterial, ExportMaterial> _materials =
            new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<string> _materialNames = new(StringComparer.OrdinalIgnoreCase);

        public void Write(string requestedFilePath)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outputDirectory = GetOutputDirectory(requestedFilePath);
            var baseName = Path.GetFileNameWithoutExtension(requestedFilePath);
            var objPath = Path.Combine(outputDirectory, $"{baseName}.obj");
            var mtlPath = Path.Combine(outputDirectory, $"{baseName}.mtl");
            var texturesDirectory = Path.Combine(outputDirectory, "Textures");
            var parentDirectory = Path.GetDirectoryName(outputDirectory)!;
            Directory.CreateDirectory(parentDirectory);
            var temporaryDirectory = Path.Combine(parentDirectory, $".{baseName}.obj-export-{Guid.NewGuid():N}");

            try
            {
                Directory.CreateDirectory(temporaryDirectory);
                var temporaryObjPath = Path.Combine(temporaryDirectory, Path.GetFileName(objPath));
                var temporaryMtlPath = Path.Combine(temporaryDirectory, Path.GetFileName(mtlPath));
                var temporaryTexturesDirectory = Path.Combine(temporaryDirectory, "Textures");
                Directory.CreateDirectory(temporaryTexturesDirectory);

                var triangleCount = WriteObj(temporaryObjPath, Path.GetFileName(mtlPath));
                if (triangleCount == 0)
                    throw new InvalidOperationException("場景中沒有可輸出的可見 Mesh。");
                WriteMaterials(temporaryMtlPath, temporaryTexturesDirectory);

                Directory.CreateDirectory(outputDirectory);
                Directory.CreateDirectory(texturesDirectory);
                File.Move(temporaryObjPath, objPath, overwrite: true);
                File.Move(temporaryMtlPath, mtlPath, overwrite: true);
                foreach (var texturePath in Directory.EnumerateFiles(temporaryTexturesDirectory))
                    File.Move(texturePath, Path.Combine(texturesDirectory, Path.GetFileName(texturePath)), overwrite: true);
            }
            finally
            {
                if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, recursive: true);
            }
        }

        private long WriteObj(string path, string materialLibraryFileName)
        {
            using var writer = new StreamWriter(path, append: false, new UTF8Encoding(false));
            writer.WriteLine("# Exported by Rv3d Viewer");
            writer.WriteLine($"mtllib {QuotePath(materialLibraryFileName)}");

            long triangleCount = 0;
            var positionOffset = 1;
            var textureOffset = 1;
            var normalOffset = 1;
            foreach (var model in project.Models.Where(model => model.IsVisible))
            foreach (var instance in SceneTraversal.GetMeshInstances(model))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (model.HiddenMeshIndices.Contains(instance.MeshIndex)) continue;
                var mesh = model.Meshes[instance.MeshIndex];
                if (mesh.Positions.Length == 0 || mesh.Indices.Length < 3) continue;

                var world = SceneTraversal.GetMeshWorldTransform(model, instance);
                var hasTextureCoordinates = mesh.TextureCoordinates.Length == mesh.Positions.Length;
                var hasNormals = mesh.Normals.Length == mesh.Positions.Length;
                var normalTransform = Matrix4x4.Identity;
                if (hasNormals && Matrix4x4.Invert(world, out var inverse))
                    normalTransform = Matrix4x4.Transpose(inverse);
                var reverseWinding = world.GetDeterminant() < 0f;
                var objectName = SanitizeName($"{model.Name}_{mesh.Name}", $"Mesh_{instance.MeshIndex + 1}");
                var sourceMaterial = model.Materials.ElementAtOrDefault(mesh.MaterialIndex) ?? new PbrMaterial();
                var material = GetOrAddMaterial(sourceMaterial);

                writer.WriteLine();
                writer.WriteLine($"o {objectName}");
                writer.WriteLine($"g {objectName}");
                writer.WriteLine($"usemtl {material.Name}");
                foreach (var position in mesh.Positions)
                {
                    var transformed = Vector3.Transform(position, world);
                    writer.WriteLine($"v {F(transformed.X)} {F(transformed.Y)} {F(transformed.Z)}");
                }
                if (hasTextureCoordinates)
                    foreach (var uv in mesh.TextureCoordinates)
                        writer.WriteLine($"vt {F(uv.X)} {F(uv.Y)}");
                if (hasNormals)
                    foreach (var normal in mesh.Normals)
                    {
                        var transformed = Vector3.TransformNormal(normal, normalTransform);
                        if (transformed.LengthSquared() > 0.0000001f) transformed = Vector3.Normalize(transformed);
                        writer.WriteLine($"vn {F(transformed.X)} {F(transformed.Y)} {F(transformed.Z)}");
                    }

                for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
                {
                    var a = mesh.Indices[index];
                    var b = mesh.Indices[index + (reverseWinding ? 2 : 1)];
                    var c = mesh.Indices[index + (reverseWinding ? 1 : 2)];
                    if (a >= mesh.Positions.Length || b >= mesh.Positions.Length || c >= mesh.Positions.Length) continue;
                    writer.WriteLine($"f {Face(a)} {Face(b)} {Face(c)}");
                    triangleCount++;
                }

                positionOffset += mesh.Positions.Length;
                if (hasTextureCoordinates) textureOffset += mesh.TextureCoordinates.Length;
                if (hasNormals) normalOffset += mesh.Normals.Length;

                string Face(uint index)
                {
                    var vertex = positionOffset + checked((int)index);
                    if (hasTextureCoordinates && hasNormals)
                        return $"{vertex}/{textureOffset + checked((int)index)}/{normalOffset + checked((int)index)}";
                    if (hasTextureCoordinates) return $"{vertex}/{textureOffset + checked((int)index)}";
                    if (hasNormals) return $"{vertex}//{normalOffset + checked((int)index)}";
                    return vertex.ToString(Invariant);
                }
            }
            return triangleCount;
        }

        private ExportMaterial GetOrAddMaterial(PbrMaterial source)
        {
            if (_materials.TryGetValue(source, out var existing)) return existing;
            source.Validate();
            var requestedName = SanitizeName(source.Name, "Material");
            var name = requestedName;
            for (var suffix = 2; !_materialNames.Add(name); suffix++) name = $"{requestedName}_{suffix}";
            var material = new ExportMaterial(name, source);
            _materials[source] = material;
            return material;
        }

        private void WriteMaterials(string path, string texturesDirectory)
        {
            using var writer = new StreamWriter(path, append: false, new UTF8Encoding(false));
            writer.WriteLine("# Exported by Rv3d Viewer");
            foreach (var export in _materials.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var material = export.Source;
                var color = new Vector3(material.BaseColor.X, material.BaseColor.Y, material.BaseColor.Z);
                var specular = Vector3.Lerp(new Vector3(0.04f), color, material.Metallic);
                var alpha = Math.Clamp(material.BaseColor.W * material.Opacity, 0f, 1f);
                var shininess = Math.Clamp(2f / MathF.Max(material.Roughness * material.Roughness, 0.0001f) - 2f, 0f, 1000f);

                writer.WriteLine();
                writer.WriteLine($"newmtl {export.Name}");
                writer.WriteLine($"Ka {F(color.X * material.AmbientOcclusion)} {F(color.Y * material.AmbientOcclusion)} {F(color.Z * material.AmbientOcclusion)}");
                writer.WriteLine($"Kd {F(color.X)} {F(color.Y)} {F(color.Z)}");
                writer.WriteLine($"Ks {F(specular.X)} {F(specular.Y)} {F(specular.Z)}");
                writer.WriteLine($"Ke {F(material.Emissive.X * material.EmissiveStrength)} {F(material.Emissive.Y * material.EmissiveStrength)} {F(material.Emissive.Z * material.EmissiveStrength)}");
                writer.WriteLine($"Ns {F(shininess)}");
                writer.WriteLine($"Ni {F(material.IndexOfRefraction)}");
                writer.WriteLine($"d {F(alpha)}");
                writer.WriteLine($"Tr {F(1f - alpha)}");
                writer.WriteLine($"illum {(material.RequiresAlphaBlending ? 4 : 2)}");
                writer.WriteLine($"Pm {F(material.Metallic)}");
                writer.WriteLine($"Pr {F(material.Roughness)}");
                writer.WriteLine($"Tf {F(material.AbsorptionColor.X)} {F(material.AbsorptionColor.Y)} {F(material.AbsorptionColor.Z)}");
                writer.WriteLine($"# double_sided {(material.DoubleSided ? 1 : 0)}");

                WriteTexture(writer, export, TextureSemantic.BaseColor, "map_Kd", texturesDirectory);
                WriteTexture(writer, export, TextureSemantic.Metallic, "map_Pm", texturesDirectory);
                WriteTexture(writer, export, TextureSemantic.Roughness, "map_Pr", texturesDirectory);
                WriteTexture(writer, export, TextureSemantic.Normal, "norm", texturesDirectory);
                WriteTexture(writer, export, TextureSemantic.Bump, "bump", texturesDirectory);
                WriteTexture(writer, export, TextureSemantic.AmbientOcclusion, "map_Ka", texturesDirectory);
                WriteTexture(writer, export, TextureSemantic.Emissive, "map_Ke", texturesDirectory);
                WriteTexture(writer, export, TextureSemantic.Opacity, "map_d", texturesDirectory);
            }
        }

        private void WriteTexture(StreamWriter writer, ExportMaterial export, TextureSemantic semantic,
            string command, string texturesDirectory)
        {
            if (!TryWriteTexture(export, semantic, texturesDirectory, out var fileName)) return;
            writer.WriteLine($"{command} {QuotePath($"Textures/{fileName}")}");
        }

        private bool TryWriteTexture(ExportMaterial export, TextureSemantic semantic, string directory,
            out string fileName)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var material = export.Source;
            fileName = $"{export.Name}_{semantic}.png";
            var destination = Path.Combine(directory, fileName);
            if (material.TextureStacks.TryGetValue(semantic, out var stack) && stack.Enabled &&
                stack.Layers.Any(layer => layer.Enabled))
            {
                using var bitmap = TextureStackComposer.Compose(stack, ResolveAssetPath, 1024);
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                bitmap.Save(destination, ImageFormat.Png);
                return true;
            }
            if (!material.Textures.TryGetValue(semantic, out var slot) || !slot.Enabled) return false;
            var sourcePath = ResolveAssetPath(slot.Path);
            if (!File.Exists(sourcePath)) return false;
            using (var bitmap = new Bitmap(sourcePath)) bitmap.Save(destination, ImageFormat.Png);
            return true;
        }

        private string ResolveAssetPath(string path)
        {
            if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
            var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);
            return string.IsNullOrWhiteSpace(projectDirectory)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(projectDirectory, path));
        }

        private static string F(float value) => value.ToString("0.######", Invariant);

        private static string QuotePath(string path) => path.Any(char.IsWhiteSpace) ? $"\"{path}\"" : path;

        private sealed record ExportMaterial(string Name, PbrMaterial Source);
    }

    private static string SanitizeName(string? value, string fallback)
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var result = new string(source.Select(character =>
            invalid.Contains(character) || char.IsWhiteSpace(character) ? '_' : character).ToArray()).Trim('_');
        return string.IsNullOrWhiteSpace(result) ? fallback : result;
    }
}
