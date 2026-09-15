using System.Globalization;
using System.Text;

namespace Rv3dViewer.Rendering.OpenGL;

public sealed record HdrImage(int Width, int Height, float[] Pixels);

public static class RadianceHdrLoader
{
    public static HdrImage Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    public static HdrImage Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var signature = ReadLine(stream);
        if (signature is null || (!signature.StartsWith("#?RADIANCE", StringComparison.Ordinal) &&
                                  !signature.StartsWith("#?RGBE", StringComparison.Ordinal)))
            throw new InvalidDataException("不是有效的 Radiance HDR 檔案。");

        string? line;
        do
        {
            line = ReadLine(stream);
            if (line is null) throw new InvalidDataException("HDR 標頭不完整。");
        } while (line.Length != 0);

        var resolution = ReadLine(stream)?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (resolution is null || resolution.Length != 4)
            throw new InvalidDataException("HDR 解析度格式不正確。");

        var yTokenFirst = resolution[0].EndsWith('Y');
        var yToken = yTokenFirst ? resolution[0] : resolution[2];
        var xToken = yTokenFirst ? resolution[2] : resolution[0];
        var heightText = yTokenFirst ? resolution[1] : resolution[3];
        var widthText = yTokenFirst ? resolution[3] : resolution[1];
        if (!int.TryParse(widthText, NumberStyles.None, CultureInfo.InvariantCulture, out var width) ||
            !int.TryParse(heightText, NumberStyles.None, CultureInfo.InvariantCulture, out var height) ||
            width <= 0 || height <= 0)
            throw new InvalidDataException("HDR 解析度無效。");

        var rgbe = new byte[checked(width * height * 4)];
        for (var row = 0; row < height; row++)
            ReadScanline(stream, rgbe, row * width * 4, width);

        var pixels = new float[checked(width * height * 3)];
        var flipY = yToken[0] == '+';
        var flipX = xToken[0] == '-';
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var sourceX = flipX ? width - 1 - x : x;
            var sourceY = flipY ? height - 1 - y : y;
            var source = (sourceY * width + sourceX) * 4;
            var target = (y * width + x) * 3;
            var exponent = rgbe[source + 3];
            if (exponent == 0) continue;
            var scale = MathF.Pow(2f, exponent - 136);
            pixels[target] = rgbe[source] * scale;
            pixels[target + 1] = rgbe[source + 1] * scale;
            pixels[target + 2] = rgbe[source + 2] * scale;
        }
        return new HdrImage(width, height, pixels);
    }

    private static void ReadScanline(Stream stream, byte[] destination, int offset, int width)
    {
        var header = ReadExact(stream, 4);
        if (width < 8 || width > 32767 || header[0] != 2 || header[1] != 2 || (header[2] & 0x80) != 0)
        {
            Buffer.BlockCopy(header, 0, destination, offset, 4);
            ReadInto(stream, destination, offset + 4, (width - 1) * 4);
            return;
        }
        if ((header[2] << 8 | header[3]) != width)
            throw new InvalidDataException("HDR 掃描線寬度不一致。");

        var channel = new byte[width];
        for (var component = 0; component < 4; component++)
        {
            var position = 0;
            while (position < width)
            {
                var count = ReadByte(stream);
                if (count > 128)
                {
                    var run = count - 128;
                    if (run == 0 || position + run > width) throw new InvalidDataException("HDR RLE 資料損毀。");
                    Array.Fill(channel, (byte)ReadByte(stream), position, run);
                    position += run;
                }
                else
                {
                    if (count == 0 || position + count > width) throw new InvalidDataException("HDR RLE 資料損毀。");
                    ReadInto(stream, channel, position, count);
                    position += count;
                }
            }
            for (var x = 0; x < width; x++) destination[offset + x * 4 + component] = channel[x];
        }
    }

    private static string? ReadLine(Stream stream)
    {
        var bytes = new List<byte>();
        while (true)
        {
            var value = stream.ReadByte();
            if (value < 0) return bytes.Count == 0 ? null : Encoding.ASCII.GetString(bytes.ToArray());
            if (value == '\n') return Encoding.ASCII.GetString(bytes.ToArray()).TrimEnd('\r');
            bytes.Add((byte)value);
        }
    }

    private static byte[] ReadExact(Stream stream, int count)
    {
        var result = new byte[count];
        ReadInto(stream, result, 0, count);
        return result;
    }

    private static void ReadInto(Stream stream, byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            var read = stream.Read(buffer, offset, count);
            if (read == 0) throw new EndOfStreamException("HDR 像素資料不完整。");
            offset += read;
            count -= read;
        }
    }

    private static int ReadByte(Stream stream)
    {
        var value = stream.ReadByte();
        return value < 0 ? throw new EndOfStreamException("HDR 像素資料不完整。") : value;
    }
}
