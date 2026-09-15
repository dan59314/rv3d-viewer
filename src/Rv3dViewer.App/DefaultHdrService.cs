using System.Numerics;
using System.Text;
using Rv3dViewer.Core;

namespace Rv3dViewer.App;

public static class DefaultHdrService
{
    public const string FileName = "defaultStudio.hdr";

    public static string CreateForProject(ViewerProject project, SceneModel? preferredModel = null)
    {
        var hdrPath = GetTargetPath(project, preferredModel);
        WriteStudioHdr(hdrPath, 512, 256);

        var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);
        project.Environment.Path = string.IsNullOrWhiteSpace(projectDirectory)
            ? hdrPath
            : Path.GetRelativePath(projectDirectory, hdrPath);
        project.Environment.Enabled = true;
        project.Environment.Intensity = 1.5f;
        project.Environment.RotationDegrees = 0f;
        project.Environment.ShowBackground = true;
        project.Environment.BackgroundBlur = 0.05f;
        project.Environment.Validate();
        return hdrPath;
    }

    public static string GetTargetPath(ViewerProject project, SceneModel? preferredModel = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        var model = preferredModel is not null && project.Models.Contains(preferredModel)
            ? preferredModel
            : project.Models.FirstOrDefault()
              ?? throw new InvalidOperationException("請先載入模型，才能在模型路徑建立預設 HDR。");
        var modelPath = ProjectAssetService.ResolveModelPath(project, model);
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new InvalidOperationException("找不到模型路徑。");
        return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(modelPath))!, FileName);
    }

    public static void WriteStudioHdr(string path, int width, int height)
    {
        if (width is < 8 or > 32767) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var stream = File.Create(path);
        var header = Encoding.ASCII.GetBytes($"#?RADIANCE\nFORMAT=32-bit_rle_rgbe\nEXPOSURE=1.000000\n\n-Y {height} +X {width}\n");
        stream.Write(header);

        var scanline = new byte[width * 4];
        var channel = new byte[width];
        for (var y = 0; y < height; y++)
        {
            var v = (y + 0.5f) / height;
            for (var x = 0; x < width; x++)
            {
                var u = (x + 0.5f) / width;
                var color = StudioColor(u, v);
                EncodeRgbe(color, scanline, x * 4);
            }

            stream.WriteByte(2);
            stream.WriteByte(2);
            stream.WriteByte((byte)(width >> 8));
            stream.WriteByte((byte)width);
            for (var component = 0; component < 4; component++)
            {
                for (var x = 0; x < width; x++) channel[x] = scanline[x * 4 + component];
                WriteLiteralChannel(stream, channel);
            }
        }
    }

    private static Vector3 StudioColor(float u, float v)
    {
        var vertical = Math.Clamp(1f - v, 0f, 1f);
        var color = Vector3.Lerp(new Vector3(0.025f, 0.03f, 0.045f), new Vector3(0.18f, 0.22f, 0.3f), vertical);
        color += new Vector3(0.12f, 0.1f, 0.08f) * MathF.Exp(-MathF.Pow((v - 0.56f) / 0.16f, 2f));
        color += SoftBox(u, v, 0.18f, 0.33f, 0.075f, 0.16f, new Vector3(15f, 12.5f, 9.5f));
        color += SoftBox(u, v, 0.68f, 0.38f, 0.1f, 0.2f, new Vector3(6.5f, 8.5f, 12f));
        color += SoftBox(u, v, 0.46f, 0.22f, 0.035f, 0.07f, new Vector3(4f, 3.5f, 3f));
        if (v > 0.72f)
            color *= MathF.Max(0.18f, 1f - (v - 0.72f) * 2.4f);
        return Vector3.Max(color, Vector3.Zero);
    }

    private static Vector3 SoftBox(float u, float v, float centerU, float centerV, float radiusU, float radiusV, Vector3 intensity)
    {
        var du = MathF.Abs(u - centerU);
        du = MathF.Min(du, 1f - du);
        var distance = du * du / (radiusU * radiusU) + (v - centerV) * (v - centerV) / (radiusV * radiusV);
        return intensity * MathF.Exp(-distance * 2.2f);
    }

    private static void EncodeRgbe(Vector3 color, byte[] destination, int offset)
    {
        var maximum = MathF.Max(color.X, MathF.Max(color.Y, color.Z));
        if (maximum < 1e-32f)
        {
            destination[offset] = destination[offset + 1] = destination[offset + 2] = destination[offset + 3] = 0;
            return;
        }
        var exponent = Math.ILogB(maximum) + 1;
        var scale = 256f / MathF.Pow(2f, exponent);
        destination[offset] = (byte)Math.Clamp((int)(color.X * scale), 0, 255);
        destination[offset + 1] = (byte)Math.Clamp((int)(color.Y * scale), 0, 255);
        destination[offset + 2] = (byte)Math.Clamp((int)(color.Z * scale), 0, 255);
        destination[offset + 3] = (byte)Math.Clamp(exponent + 128, 0, 255);
    }

    private static void WriteLiteralChannel(Stream stream, byte[] channel)
    {
        for (var offset = 0; offset < channel.Length;)
        {
            var count = Math.Min(128, channel.Length - offset);
            stream.WriteByte((byte)count);
            stream.Write(channel, offset, count);
            offset += count;
        }
    }
}
