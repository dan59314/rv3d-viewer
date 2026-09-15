using System.Text.Json;

namespace Rv3dViewer.ReliefPlugin;

internal sealed class ReliefPluginSettings
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public decimal Width { get; set; } = 100;
    public decimal Thickness { get; set; } = 10;
    public decimal BorderWidth { get; set; } = 3;
    public decimal ReliefHeight { get; set; } = 6;
    public int QualityIndex { get; set; } = 1;
    public bool ShowTexture { get; set; } = true;
    public bool Grayscale { get; set; } = true;
    public decimal Hue { get; set; }
    public decimal Saturation { get; set; } = 100;
    public decimal Value { get; set; } = 100;
    public bool Invert { get; set; }
    public bool ReduceColors { get; set; }
    public decimal ColorLevels { get; set; } = 16;
    public bool AiDepth { get; set; }
    public bool UseDepthImageFile { get; set; }
    public string DepthImageFilePath { get; set; } = string.Empty;
    public bool BlendDepth { get; set; }
    public string BlendImageProcessingMode { get; set; } = "Grayscale";
    public List<string> ImageProcessingOrder { get; set; } = ["Grayscale", "EdgeDetection", "Binarization", "GaussianBlur"];
    public bool EdgeDetectionEnabled { get; set; }
    public decimal EdgeThreshold { get; set; } = 15;
    public decimal EdgeStrength { get; set; } = 100;
    public decimal EdgeSmoothing { get; set; } = 1;
    public bool BinarizationEnabled { get; set; }
    public decimal BinarizationThreshold { get; set; } = 50;
    public bool BinarizationInvert { get; set; }
    public bool GaussianBlurEnabled { get; set; }
    public decimal GaussianBlurRadius { get; set; } = 2;
    public decimal AiDepthWeight { get; set; } = 50;
    public decimal DepthCurve { get; set; } = 100;
    public decimal LocalDetail { get; set; } = 25;
    public decimal PortraitGeometry { get; set; } = 60;
    public string PortraitLevel { get; set; } = "標準";
    public decimal GlassesRelief { get; set; } = 35;
    public decimal HairDetail { get; set; } = 25;
    public decimal SurfaceNormalDetail { get; set; } = 20;
    public decimal FacialFeatureContour { get; set; } = 25;
    public decimal FacialDepthContrast { get; set; } = 35;
    public decimal FacialMicroDetail { get; set; } = 20;
    public bool AutoPortraitCrop { get; set; } = true;
    public decimal BackgroundSuppression { get; set; } = 80;
    public bool BustSilhouette { get; set; } = true;
    public bool FullBodySegmentation { get; set; }
    public string BodyLevel { get; set; } = "標準";
    public decimal FullBodyDepth { get; set; } = 45;
    public decimal BodyMaskCleanup { get; set; } = 35;
    public bool Symmetry { get; set; }
    public decimal SymmetryAxis { get; set; } = 50;
    public bool Smoothing { get; set; }
    public decimal SmoothingThreshold { get; set; } = 0.5m;
    public decimal SmoothingStrength { get; set; } = 50;
    public decimal SmoothingIterations { get; set; } = 2;
    public decimal EnvironmentLight { get; set; } = 40;
    public bool SimplifyModel { get; set; }
    public decimal SimplificationTarget { get; set; } = 50;
    public decimal SimplificationNormalAngle { get; set; } = 5;
    public bool Wireframe { get; set; }
    public string ModelPlane { get; set; } = "XZ";
    public decimal ModelRotationAngle { get; set; }
    public string ModelAlignment { get; set; } = "Center";

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "ReliefPlugin", "settings.json");

    public static ReliefPluginSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new();
            var settings = JsonSerializer.Deserialize<ReliefPluginSettings>(File.ReadAllText(SettingsPath));
            return settings is { Version: CurrentVersion } ? settings : new();
        }
        catch
        {
            return new();
        }
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = SettingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, SettingsPath, true);
    }
}
