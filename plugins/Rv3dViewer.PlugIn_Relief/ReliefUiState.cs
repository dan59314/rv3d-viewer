using System.Text.Json;

namespace Rv3dViewer.ReliefPlugin;

internal sealed class ReliefUiState
{
    public string? LastParameterProfilePath { get; set; }
    public bool SourceExpanded { get; set; } = true;
    public bool GeometryExpanded { get; set; } = true;
    public bool SmoothingExpanded { get; set; } = true;
    public bool ProcessingExpanded { get; set; } = true;
    public bool DepthExpanded { get; set; } = true;
    public bool PortraitExpanded { get; set; } = true;

    private static string StatePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "ReliefPlugin", "ui-state.json");

    public static ReliefUiState Load()
    {
        try
        {
            return File.Exists(StatePath)
                ? JsonSerializer.Deserialize<ReliefUiState>(File.ReadAllText(StatePath)) ?? new()
                : new();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(StatePath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = StatePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, StatePath, true);
    }
}
