using System.Text.Json;

namespace Rv3dViewer.CameraAnimationPlugin;

public static class CameraAnimationSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true
    };

    public static async Task SaveAsync(CameraAnimationDocument document, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.Validate();
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, document, Options, cancellationToken);
    }

    public static async Task<CameraAnimationDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<CameraAnimationDocument>(stream, Options, cancellationToken)
                       ?? throw new InvalidDataException("Camera 動畫檔沒有有效內容。");
        document.Validate();
        return document;
    }
}
