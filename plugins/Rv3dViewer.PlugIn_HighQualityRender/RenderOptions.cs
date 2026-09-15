namespace Rv3dViewer.HighQualityRenderPlugin;

public enum RenderExecutionMode { Automatic, Gpu, Cpu }
public enum RenderDenoiserMode { Disabled, BuiltIn, Oidn }
public enum RenderGlassQuality { Fast, Physical }
public enum RenderTextureQuality { Low, Medium, Original }
public enum RenderQualityPreset { Fast, Balanced, HighQuality }
public enum RenderProfile { Custom, QuickDraft, Basic, ProductStudio, Metal, Glass, Jewelry, Interior, FinalQuality, JewelryOidn }

internal sealed record RenderProfileSettings(
    int SamplesPerPixel,
    int MaximumBounces,
    int RenderScalePercent,
    RenderDenoiserMode DenoiserMode,
    bool AdaptiveSampling,
    bool UseHdriImportanceSampling,
    RenderGlassQuality GlassQuality,
    RenderTextureQuality TextureQuality,
    float FireflyClamp,
    bool ExportAov)
{
    internal static RenderProfileSettings For(RenderProfile profile) => profile switch
    {
        RenderProfile.QuickDraft => new(32, 4, 50, RenderDenoiserMode.Oidn, true, true,
            RenderGlassQuality.Fast, RenderTextureQuality.Medium, 8F, false),
        RenderProfile.Basic => new(128, 6, 100, RenderDenoiserMode.Oidn, true, true,
            RenderGlassQuality.Physical, RenderTextureQuality.Original, 12F, false),
        RenderProfile.ProductStudio => new(256, 8, 100, RenderDenoiserMode.Oidn, true, true,
            RenderGlassQuality.Physical, RenderTextureQuality.Original, 24F, false),
        RenderProfile.Metal => new(384, 10, 100, RenderDenoiserMode.Oidn, true, true,
            RenderGlassQuality.Physical, RenderTextureQuality.Original, 36F, false),
        RenderProfile.Glass => new(384, 12, 100, RenderDenoiserMode.BuiltIn, false, true,
            RenderGlassQuality.Physical, RenderTextureQuality.Original, 40F, false),
        RenderProfile.Jewelry => new(512, 12, 100, RenderDenoiserMode.BuiltIn, false, true,
            RenderGlassQuality.Physical, RenderTextureQuality.Original, 50F, false),
        RenderProfile.JewelryOidn => For(RenderProfile.Jewelry) with { DenoiserMode = RenderDenoiserMode.Oidn },
        RenderProfile.Interior => new(384, 10, 100, RenderDenoiserMode.Oidn, true, true,
            RenderGlassQuality.Physical, RenderTextureQuality.Original, 24F, false),
        RenderProfile.FinalQuality => new(1024, 16, 100, RenderDenoiserMode.BuiltIn, false, true,
            RenderGlassQuality.Physical, RenderTextureQuality.Original, 64F, true),
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "自訂模式沒有固定參數。")
    };
}

internal sealed record RenderQualityPresetSettings(
    int SamplesPerPixel,
    int MaximumBounces,
    int RenderScalePercent,
    RenderDenoiserMode DenoiserMode,
    bool AdaptiveSampling,
    bool UseHdriImportanceSampling,
    RenderGlassQuality GlassQuality,
    RenderTextureQuality TextureQuality,
    float FireflyClamp,
    bool ExportAov)
{
    internal static RenderQualityPresetSettings For(RenderQualityPreset preset) => preset switch
    {
        RenderQualityPreset.Fast => new(16, 3, 50, RenderDenoiserMode.Oidn, true, true,
            RenderGlassQuality.Fast, RenderTextureQuality.Low, 6F, false),
        RenderQualityPreset.Balanced => new(64, 4, 100, RenderDenoiserMode.Oidn, true, true,
            RenderGlassQuality.Physical, RenderTextureQuality.Medium, 12F, false),
        _ => new(256, 6, 100, RenderDenoiserMode.Oidn, true, true,
            RenderGlassQuality.Physical, RenderTextureQuality.Original, 20F, false)
    };
}

public sealed record RenderOptions(
    int Width,
    int Height,
    int SamplesPerPixel,
    int MaximumBounces,
    bool TransparentBackground,
    bool UseEnvironment,
    string OutputPath,
    RenderExecutionMode ExecutionMode = RenderExecutionMode.Automatic,
    RenderDenoiserMode DenoiserMode = RenderDenoiserMode.Oidn,
    bool AdaptiveSampling = true,
    bool UseHdriImportanceSampling = true,
    RenderGlassQuality GlassQuality = RenderGlassQuality.Physical,
    RenderTextureQuality TextureQuality = RenderTextureQuality.Original,
    float FireflyClamp = 12F,
    bool ExportAov = false,
    RenderQualityPreset? QualityPreset = null);

public readonly record struct RenderProgress(int CompletedRows, int TotalRows)
{
    public int Percentage => TotalRows <= 0 ? 0 : Math.Clamp(CompletedRows * 100 / TotalRows, 0, 100);
}
