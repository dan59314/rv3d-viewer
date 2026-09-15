using Rv3dViewer.Core;

namespace Rv3dViewer.App;

public static class SkyboxFolderService
{
    private static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".tga"];
    private static readonly IReadOnlyDictionary<string, string[]> FaceAliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["px"] = ["px", "right"],
            ["nx"] = ["nx", "left"],
            ["py"] = ["py", "top", "up"],
            ["ny"] = ["ny", "bottom", "down"],
            ["pz"] = ["pz", "front"],
            ["nz"] = ["nz", "back"]
        };

    public static SkyboxSettings LoadFolder(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        var folder = Path.GetFullPath(folderPath);
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($"找不到 Skybox 資料夾：{folder}");

        var files = Directory.EnumerateFiles(folder)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .ToArray();
        var faces = FaceAliases.ToDictionary(
            pair => pair.Key,
            pair => FindSingleFace(files, pair.Key, pair.Value),
            StringComparer.OrdinalIgnoreCase);

        Size? expectedSize = null;
        foreach (var (face, path) in faces)
        {
            using var image = Image.FromFile(path);
            if (image.Width != image.Height)
                throw new InvalidDataException($"Skybox 的 {face} 圖片必須是正方形：{Path.GetFileName(path)}");
            var size = new Size(image.Width, image.Height);
            if (expectedSize is not null && expectedSize != size)
                throw new InvalidDataException("Skybox 六張圖片的尺寸必須完全相同。");
            expectedSize = size;
        }

        return new SkyboxSettings
        {
            Name = new DirectoryInfo(folder).Name,
            PositiveX = faces["px"],
            NegativeX = faces["nx"],
            PositiveY = faces["py"],
            NegativeY = faces["ny"],
            PositiveZ = faces["pz"],
            NegativeZ = faces["nz"]
        };
    }

    public static SkyboxSettings LoadPanorama(string panoramaPath, string libraryDirectory, int faceSize = 1024)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(panoramaPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(libraryDirectory);
        var sourcePath = Path.GetFullPath(panoramaPath);
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("找不到 Skybox 全景圖。", sourcePath);
        using var panorama = new Bitmap(sourcePath);
        if (panorama.Width != panorama.Height * 2)
            throw new InvalidDataException($"Skybox 全景圖必須是 2:1，現在為 {panorama.Width}×{panorama.Height}。");

        faceSize = Math.Clamp(faceSize, 128, 4096);
        var folder = CreateUniqueFolder(libraryDirectory, Path.GetFileNameWithoutExtension(sourcePath));
        Directory.CreateDirectory(folder);
        var copiedSource = Path.Combine(folder, $"source_panorama{Path.GetExtension(sourcePath).ToLowerInvariant()}");
        File.Copy(sourcePath, copiedSource, overwrite: false);
        GenerateFaces(panorama, folder, faceSize);

        var result = LoadFolder(folder);
        result.SourcePanorama = copiedSource;
        return result;
    }

    public static IReadOnlyList<SkyboxSettings> LoadLibrary(string libraryDirectory)
    {
        if (!Directory.Exists(libraryDirectory)) return [];
        var result = new List<SkyboxSettings>();
        foreach (var folder in Directory.EnumerateDirectories(libraryDirectory).OrderBy(path => path))
        {
            try
            {
                var panoramaPath = FindSourcePanorama(folder);
                if (!string.IsNullOrWhiteSpace(panoramaPath) && FacesNeedRebuild(folder, panoramaPath))
                {
                    using var panorama = new Bitmap(panoramaPath);
                    if (panorama.Width != panorama.Height * 2)
                        throw new InvalidDataException(
                            $"Skybox「{new DirectoryInfo(folder).Name}」的 source_panorama 必須是 2:1，現在為 {panorama.Width}×{panorama.Height}。");
                    GenerateFaces(panorama, folder, Math.Clamp(panorama.Height, 128, 2048));
                }
                var item = LoadFolder(folder);
                item.SourcePanorama = panoramaPath;
                result.Add(item);
            }
            catch (InvalidDataException)
            {
                // Ignore unrelated or incomplete folders in the library.
            }
        }
        return result;
    }

    private static string FindSourcePanorama(string folder) =>
        Directory.EnumerateFiles(folder)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path)
                .Equals("source_panorama", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

    private static bool FacesNeedRebuild(string folder, string panoramaPath)
    {
        var sourceTime = File.GetLastWriteTimeUtc(panoramaPath);
        foreach (var face in FaceAliases.Keys)
        {
            var path = SupportedExtensions
                .Select(extension => Path.Combine(folder, $"{face}{extension}"))
                .FirstOrDefault(File.Exists);
            if (path is null || File.GetLastWriteTimeUtc(path) < sourceTime) return true;
        }
        return false;
    }

    private static void GenerateFaces(Bitmap panorama, string folder, int faceSize)
    {
        foreach (var face in FaceAliases.Keys)
        {
            using var output = ConvertFace(panorama, face, faceSize);
            var existingPath = SupportedExtensions
                .Select(extension => Path.Combine(folder, $"{face}{extension}"))
                .FirstOrDefault(File.Exists);
            var outputPath = existingPath ?? Path.Combine(folder, $"{face}.png");
            var format = Path.GetExtension(outputPath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => System.Drawing.Imaging.ImageFormat.Jpeg,
                ".bmp" => System.Drawing.Imaging.ImageFormat.Bmp,
                _ => System.Drawing.Imaging.ImageFormat.Png
            };
            output.Save(outputPath, format);
        }
    }

    private static Bitmap ConvertFace(Bitmap panorama, string face, int size)
    {
        using var source = new Bitmap(panorama.Width, panorama.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(source)) graphics.DrawImageUnscaled(panorama, 0, 0);
        var sourceRect = new Rectangle(0, 0, source.Width, source.Height);
        var sourceData = source.LockBits(sourceRect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var sourceBytes = new byte[Math.Abs(sourceData.Stride) * source.Height];
        System.Runtime.InteropServices.Marshal.Copy(sourceData.Scan0, sourceBytes, 0, sourceBytes.Length);
        source.UnlockBits(sourceData);

        var output = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var outputRect = new Rectangle(0, 0, size, size);
        var outputData = output.LockBits(outputRect, System.Drawing.Imaging.ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var outputBytes = new byte[Math.Abs(outputData.Stride) * size];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var a = (x + 0.5f) * 2f / size - 1f;
            var b = (y + 0.5f) * 2f / size - 1f;
            var direction = face switch
            {
                "px" => new System.Numerics.Vector3(1f, -b, -a),
                "nx" => new System.Numerics.Vector3(-1f, -b, a),
                "py" => new System.Numerics.Vector3(a, 1f, b),
                "ny" => new System.Numerics.Vector3(a, -1f, -b),
                "pz" => new System.Numerics.Vector3(a, -b, 1f),
                _ => new System.Numerics.Vector3(-a, -b, -1f)
            };
            direction = System.Numerics.Vector3.Normalize(direction);
            var sourceX = (MathF.Atan2(direction.Z, direction.X) / (2f * MathF.PI) + 0.5f) * source.Width;
            var sourceY = (0.5f - MathF.Asin(Math.Clamp(direction.Y, -1f, 1f)) / MathF.PI) * source.Height;
            var sx = ((int)MathF.Floor(sourceX) % source.Width + source.Width) % source.Width;
            var sy = Math.Clamp((int)MathF.Floor(sourceY), 0, source.Height - 1);
            var sourceOffset = sy * Math.Abs(sourceData.Stride) + sx * 4;
            var outputOffset = y * Math.Abs(outputData.Stride) + x * 4;
            Buffer.BlockCopy(sourceBytes, sourceOffset, outputBytes, outputOffset, 4);
        }
        System.Runtime.InteropServices.Marshal.Copy(outputBytes, 0, outputData.Scan0, outputBytes.Length);
        output.UnlockBits(outputData);
        return output;
    }

    private static string CreateUniqueFolder(string parent, string requestedName)
    {
        Directory.CreateDirectory(parent);
        var invalid = Path.GetInvalidFileNameChars();
        var name = new string(requestedName.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(name)) name = "Skybox";
        var candidate = Path.Combine(parent, name);
        for (var suffix = 2; Directory.Exists(candidate); suffix++) candidate = Path.Combine(parent, $"{name}_{suffix}");
        return candidate;
    }

    private static string FindSingleFace(IEnumerable<string> files, string face, IEnumerable<string> aliases)
    {
        var aliasSet = aliases.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matches = files.Where(path => aliasSet.Contains(Path.GetFileNameWithoutExtension(path))).ToArray();
        return matches.Length switch
        {
            1 => Path.GetFullPath(matches[0]),
            0 => throw new InvalidDataException($"Skybox 缺少 {face} 圖片。"),
            _ => throw new InvalidDataException($"Skybox 的 {face} 圖片重複：{string.Join("、", matches.Select(Path.GetFileName))}")
        };
    }
}
