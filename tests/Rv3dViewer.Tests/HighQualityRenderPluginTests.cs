using System.Drawing;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using Rv3dViewer.Core;
using Rv3dViewer.HighQualityRenderPlugin;
using Rv3dViewer.Rendering.OpenGL;
using Xunit;

namespace Rv3dViewer.Tests;

[Collection("Render GPU")]
public sealed class HighQualityRenderPluginTests : IDisposable
{
    private readonly string _outputPath = Path.Combine(Path.GetTempPath(), $"rv3d-render-{Guid.NewGuid():N}.png");

    [Fact]
    public void ImageComparison_IdenticalImagesHaveZeroDifferenceAndWriteDiagnostics()
    {
        var referencePath = Path.Combine(Path.GetTempPath(), $"rv3d-reference-{Guid.NewGuid():N}.png");
        var candidatePath = Path.Combine(Path.GetTempPath(), $"rv3d-candidate-{Guid.NewGuid():N}.png");
        var heatmapPath = Path.Combine(Path.GetTempPath(), $"rv3d-heatmap-{Guid.NewGuid():N}.png");
        var reportPath = Path.Combine(Path.GetTempPath(), $"rv3d-report-{Guid.NewGuid():N}.json");
        try
        {
            using (var bitmap = new Bitmap(4, 3))
            {
                using var graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.FromArgb(160, 40, 80, 120));
                bitmap.Save(referencePath);
                bitmap.Save(candidatePath);
            }

            var report = RenderImageComparison.Compare(referencePath, candidatePath, heatmapPath, reportPath);

            Assert.True(report.DimensionsMatch);
            Assert.Equal(12, report.ComparedPixelCount);
            Assert.Equal(0D, report.DisplayMeanAbsoluteError);
            Assert.Equal(0D, report.LinearMeanAbsoluteError);
            Assert.Equal(0D, report.AlphaMeanAbsoluteError);
            Assert.Equal(0D, report.EdgeMismatchFraction);
            Assert.True(File.Exists(heatmapPath));
            Assert.Contains("DisplayMeanAbsoluteError", File.ReadAllText(reportPath));
        }
        finally
        {
            foreach (var path in new[] { referencePath, candidatePath, heatmapPath, reportPath })
                if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ImageComparison_DetectsColorAlphaEdgeAndDimensionDifferences()
    {
        var referencePath = Path.Combine(Path.GetTempPath(), $"rv3d-reference-{Guid.NewGuid():N}.png");
        var candidatePath = Path.Combine(Path.GetTempPath(), $"rv3d-candidate-{Guid.NewGuid():N}.png");
        try
        {
            using (var reference = new Bitmap(3, 2))
            {
                using var graphics = Graphics.FromImage(reference);
                graphics.Clear(Color.FromArgb(255, 0, 0, 0));
                reference.SetPixel(1, 0, Color.FromArgb(255, 255, 255, 255));
                reference.Save(referencePath);
            }
            using (var candidate = new Bitmap(2, 2))
            {
                using var graphics = Graphics.FromImage(candidate);
                graphics.Clear(Color.FromArgb(128, 0, 0, 0));
                candidate.Save(candidatePath);
            }

            var report = RenderImageComparison.Compare(referencePath, candidatePath);

            Assert.False(report.DimensionsMatch);
            Assert.True(report.DisplayMeanAbsoluteError > 0D);
            Assert.True(report.LinearMeanAbsoluteError > 0D);
            Assert.True(report.AlphaMeanAbsoluteError > 0D);
            Assert.True(report.ChangedPixelFraction > 0D);
            Assert.True(report.EdgeMismatchFraction > 0D);
        }
        finally
        {
            foreach (var path in new[] { referencePath, candidatePath })
                if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Form_DesignerInitializesSharedOpenGlPreviewInPanel1()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new HighQualityRenderForm(
                    CreateProject(), new HighQualityRenderUiSettings());
                var sceneField = typeof(HighQualityRenderForm).GetField("_scene",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.NotNull(sceneField);
                Assert.Null(sceneField!.GetValue(form));
                var splitContainer = Assert.IsType<SplitContainer>(form.Controls["mainSplitContainer"]);
                var preview = Assert.IsType<OpenTK.GLControl.GLControl>(
                    splitContainer.Panel1.Controls["renderPreviewGlControl"]);
                Assert.Equal(DockStyle.Fill, preview.Dock);
                var oldPreview = Assert.IsType<RenderPreviewControl>(
                    splitContainer.Panel1.Controls["oldRenderPreviewControl"]);
                Assert.Equal(DockStyle.Fill, oldPreview.Dock);
                Assert.False(oldPreview.Visible);
                var renderedImage = Assert.IsType<PictureBox>(
                    splitContainer.Panel1.Controls["renderedImagePictureBox"]);
                Assert.Equal(DockStyle.Fill, renderedImage.Dock);
                Assert.False(renderedImage.Visible);
                var settings = Assert.IsType<GroupBox>(form.Controls.Find("settingsGroupBox", true).Single());
                var gpuSelector = Assert.IsType<ComboBox>(settings.Controls.Find("gpuDeviceComboBox", true).Single());
                Assert.Equal(DockStyle.Fill, gpuSelector.Dock);
                Assert.Equal(ComboBoxStyle.DropDownList, gpuSelector.DropDownStyle);
                var samples = Assert.IsType<NumericUpDown>(
                    settings.Controls.Find("samplesNumericUpDown", true).Single());
                Assert.Equal(256M, samples.Value);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "建立 Render 視窗逾時。");
        Assert.Null(failure);
    }

    [Fact]
    public void Snapshot_CapturesVisibleTriangleAndMaterial()
    {
        var scene = RenderSceneSnapshot.Create(CreateProject());

        Assert.Single(scene.Triangles);
        Assert.Single(scene.Materials);
        Assert.Equal(new Vector3(-1F, -1F, 0F), scene.Triangles[0].P0);
        Assert.Equal(new Vector3(0.8F, 0.25F, 0.1F), new Vector3(
            scene.Materials[0].BaseColor.X,
            scene.Materials[0].BaseColor.Y,
            scene.Materials[0].BaseColor.Z));
    }

    [Fact]
    public void GpuAccelerationCacheIsReusedForCameraOnlyChanges()
    {
        var scene = RenderSceneSnapshot.Create(CreateProject());
        var movedCamera = scene.WithCamera(scene.Camera with
        {
            From = scene.Camera.From + Vector3.One
        });

        var first = GpuPathTracer.GetPreparedSceneForTest(scene);
        var second = GpuPathTracer.GetPreparedSceneForTest(movedCamera);

        Assert.Same(first, second);
    }

    [Theory]
    [InlineData("AMD Radeon(TM) 860M Graphics", 64, 1, 4)]
    [InlineData("AMD Radeon RX 7900 XTX", 256, 4, 0)]
    [InlineData("NVIDIA GeForce RTX 4080", 256, 8, 0)]
    [InlineData("Intel(R) UHD Graphics", 64, 1, 1)]
    public void GpuExecutionProfileMatchesAdapterClass(string renderer, int tileHeight,
        int samplesPerDispatch, int tilesPerSynchronization)
    {
        var profile = GpuPathTracer.SelectExecutionProfileForTest(renderer);

        Assert.Equal(tileHeight, profile.TileHeight);
        Assert.Equal(samplesPerDispatch, profile.SamplesPerDispatch);
        Assert.Equal(tilesPerSynchronization, profile.TilesPerSynchronization);
    }

    [Fact]
    public void AmdIntegratedProfileUsesOneSampleAndFourTilesPerFence()
    {
        var synchronizations = GpuPathTracer.EstimateSynchronizationCountForTest(
            "AMD Radeon(TM) 860M Graphics", height: 768, samples: 64);

        Assert.Equal(192, synchronizations);
        Assert.True(synchronizations < 384);
    }

    [Fact]
    public void QualityPresetsProduceMeaningfullyDifferentRenderSettings()
    {
        var fast = RenderQualityPresetSettings.For(RenderQualityPreset.Fast);
        var balanced = RenderQualityPresetSettings.For(RenderQualityPreset.Balanced);
        var high = RenderQualityPresetSettings.For(RenderQualityPreset.HighQuality);

        Assert.Equal((16, 3, 50),
            (fast.SamplesPerPixel, fast.MaximumBounces, fast.RenderScalePercent));
        Assert.Equal((64, 4, 100),
            (balanced.SamplesPerPixel, balanced.MaximumBounces, balanced.RenderScalePercent));
        Assert.Equal((256, 6, 100),
            (high.SamplesPerPixel, high.MaximumBounces, high.RenderScalePercent));
        Assert.Equal(RenderGlassQuality.Fast, fast.GlassQuality);
        Assert.Equal(RenderGlassQuality.Physical, balanced.GlassQuality);
        Assert.Equal(RenderGlassQuality.Physical, high.GlassQuality);
        Assert.Equal(RenderTextureQuality.Low, fast.TextureQuality);
        Assert.Equal(RenderTextureQuality.Medium, balanced.TextureQuality);
        Assert.Equal(RenderTextureQuality.Original, high.TextureQuality);
        Assert.True(fast.FireflyClamp < balanced.FireflyClamp);
        Assert.True(balanced.FireflyClamp < high.FireflyClamp);
    }

    [Theory]
    [InlineData(RenderProfile.QuickDraft, 32, 4, true, RenderDenoiserMode.Oidn, RenderGlassQuality.Fast, 8F)]
    [InlineData(RenderProfile.Basic, 128, 6, true, RenderDenoiserMode.Oidn, RenderGlassQuality.Physical, 12F)]
    [InlineData(RenderProfile.ProductStudio, 256, 8, true, RenderDenoiserMode.Oidn, RenderGlassQuality.Physical, 24F)]
    [InlineData(RenderProfile.Metal, 384, 10, true, RenderDenoiserMode.Oidn, RenderGlassQuality.Physical, 36F)]
    [InlineData(RenderProfile.Glass, 384, 12, false, RenderDenoiserMode.BuiltIn, RenderGlassQuality.Physical, 40F)]
    [InlineData(RenderProfile.Jewelry, 512, 12, false, RenderDenoiserMode.BuiltIn, RenderGlassQuality.Physical, 50F)]
    [InlineData(RenderProfile.JewelryOidn, 512, 12, false, RenderDenoiserMode.Oidn, RenderGlassQuality.Physical, 50F)]
    [InlineData(RenderProfile.Interior, 384, 10, true, RenderDenoiserMode.Oidn, RenderGlassQuality.Physical, 24F)]
    [InlineData(RenderProfile.FinalQuality, 1024, 16, false, RenderDenoiserMode.BuiltIn, RenderGlassQuality.Physical, 64F)]
    public void RenderProfilesApplyExpectedMaterialSpecificSettings(RenderProfile profile, int samples,
        int bounces, bool adaptiveSampling, RenderDenoiserMode denoiserMode,
        RenderGlassQuality glassQuality, float fireflyClamp)
    {
        var settings = RenderProfileSettings.For(profile);

        Assert.Equal(samples, settings.SamplesPerPixel);
        Assert.Equal(bounces, settings.MaximumBounces);
        Assert.Equal(adaptiveSampling, settings.AdaptiveSampling);
        Assert.Equal(denoiserMode, settings.DenoiserMode);
        Assert.Equal(glassQuality, settings.GlassQuality);
        Assert.Equal(fireflyClamp, settings.FireflyClamp);
        Assert.True(settings.UseHdriImportanceSampling);
    }

    [Fact]
    public void JewelryOidnProfileOnlyChangesDenoiser()
    {
        var jewelry = RenderProfileSettings.For(RenderProfile.Jewelry);
        var oidn = RenderProfileSettings.For(RenderProfile.JewelryOidn);

        Assert.Equal(jewelry with { DenoiserMode = RenderDenoiserMode.Oidn }, oidn);
        Assert.Equal(RenderDenoiserMode.BuiltIn, jewelry.DenoiserMode);
    }

    [Fact]
    public void CustomRenderProfileHasNoFixedSettings()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderProfileSettings.For(RenderProfile.Custom));
    }

    [Fact]
    public void GpuBvhNodeUsesCompactTwoVectorLayout()
    {
        Assert.Equal(32, GpuPathTracer.GpuNodeSizeForTest);
        var shader = GpuPathTracer.ComputeShaderSourceForTest;
        Assert.Contains("struct Node{vec4 mn;vec4 mx;}", shader, StringComparison.Ordinal);
        Assert.Contains("if(n.mn.w<0.0)", shader, StringComparison.Ordinal);
        Assert.DoesNotContain("n.data", shader, StringComparison.Ordinal);
    }

    [Fact]
    public void GpuTriangleSeparatesIntersectionAndShadingData()
    {
        Assert.Equal(48, GpuPathTracer.GpuTriangleSizeForTest);
        Assert.Equal(144, GpuPathTracer.GpuTriangleShadingSizeForTest);
        var shader = GpuPathTracer.ComputeShaderSourceForTest;
        Assert.Contains("uniform samplerBuffer uMaterials", shader, StringComparison.Ordinal);
        Assert.Contains("uniform samplerBuffer uTriangleShading", shader, StringComparison.Ordinal);
        Assert.Contains("Mat loadMaterial(int index)", shader, StringComparison.Ordinal);
        Assert.Contains("Shade loadShading(int index)", shader, StringComparison.Ordinal);
        Assert.Contains("int materialIndex=int(t.p0.w+0.5)", shader, StringComparison.Ordinal);
        Assert.Contains("struct Tri{vec4 p0;vec4 p1;vec4 p2;};",
            shader, StringComparison.Ordinal);
    }

    [Fact]
    public void GlassRelativeIor_DistinguishesEnteringAndLeavingRays()
    {
        Assert.Equal(1F / 1.5F, RenderOptics.RelativeIndexOfRefraction(frontFace: true, 1.5F), 5);
        Assert.Equal(1.5F, RenderOptics.RelativeIndexOfRefraction(frontFace: false, 1.5F), 5);
    }

    [Fact]
    public void GlassMediumStack_TracksNestedEnteringAndLeavingIors()
    {
        Span<RenderMedium> storage = stackalloc RenderMedium[4];
        var media = new RenderMediumStack(storage);
        var outer = RenderMaterial.Default with { IndexOfRefraction = 1.5F };
        var inner = RenderMaterial.Default with { IndexOfRefraction = 1.33F };

        Assert.Equal(1.5F, media.TransmittedIndexOfRefraction(0, entering: true, outer.IndexOfRefraction));
        media.CrossBoundary(0, entering: true, outer);
        Assert.Equal(1.5F, media.CurrentIndexOfRefraction);
        Assert.Equal(1.33F, media.TransmittedIndexOfRefraction(1, entering: true, inner.IndexOfRefraction));

        media.CrossBoundary(1, entering: true, inner);
        Assert.Equal(1.5F, media.TransmittedIndexOfRefraction(1, entering: false, inner.IndexOfRefraction));
        media.CrossBoundary(1, entering: false, inner);
        Assert.Equal(1.5F, media.CurrentIndexOfRefraction);

        Assert.Equal(1F, media.TransmittedIndexOfRefraction(0, entering: false, outer.IndexOfRefraction));
        media.CrossBoundary(0, entering: false, outer);
        Assert.Equal(0, media.Count);
        Assert.Equal(1F, media.CurrentIndexOfRefraction);
    }

    [Fact]
    public void GlassMediumStack_AssumesMaterialMediumWhenRayStartsInsideGlass()
    {
        Span<RenderMedium> storage = stackalloc RenderMedium[2];
        var media = new RenderMediumStack(storage);

        Assert.Equal(1.5F, media.IncidentIndexOfRefraction(0, entering: false, materialIor: 1.5F));
        Assert.Equal(1F, media.TransmittedIndexOfRefraction(0, entering: false, materialIor: 1.5F));
    }

    [Fact]
    public void GlassMediumStack_DuplicateEntryRecoversFromOverlappingJewelryShell()
    {
        Span<RenderMedium> storage = stackalloc RenderMedium[4];
        var media = new RenderMediumStack(storage);
        var gemstone = RenderMaterial.Default with { IndexOfRefraction = 2.42F };

        media.CrossBoundary(7, entering: true, gemstone);
        media.CrossBoundary(7, entering: true, gemstone);

        Assert.Equal(0, media.Count);
        Assert.Equal(1F, media.CurrentIndexOfRefraction);
    }

    [Fact]
    public void GlassAbsorption_UsesDistanceInsideMedium()
    {
        var color = new Vector3(0.25F, 0.5F, 1F);

        var oneOpticalUnit = RenderOptics.BeerLambert(color, thickness: 0.5F, distance: 0.5F);
        var noThickness = RenderOptics.BeerLambert(color, thickness: 0F, distance: 100F);

        Assert.InRange(Vector3.Distance(color, oneOpticalUnit), 0F, 0.000001F);
        Assert.Equal(Vector3.One, noThickness);
    }

    [Fact]
    public void GpuGlassShader_TracksMediaAndDistanceAbsorption()
    {
        var shader = GpuPathTracer.ComputeShaderSourceForTest;

        Assert.Contains("incidentIor/transmittedIor", shader, StringComparison.Ordinal);
        Assert.Contains("beerLambert", shader, StringComparison.Ordinal);
        Assert.Contains("mediumMaterials[8]", shader, StringComparison.Ordinal);
        Assert.Contains("max(rough*rough,0.000001)", shader, StringComparison.Ordinal);
        Assert.Contains("deltaGlass=rough<=0.01", shader, StringComparison.Ordinal);
        Assert.Contains("binding=7) buffer GlassMask", shader, StringComparison.Ordinal);
        Assert.Contains("luminance*luminance", shader, StringComparison.Ordinal);
    }

    [Fact]
    public void GpuShader_UsesTheSameBrdfNumericalFloorsAndTotalInternalReflectionRuleAsCpu()
    {
        var shader = GpuPathTracer.ComputeShaderSourceForTest;

        Assert.Contains("3.14159265*d*d,0.000001", shader, StringComparison.Ordinal);
        Assert.Contains("nv*(1.0-k)+k,0.000001", shader, StringComparison.Ordinal);
        Assert.Contains("bool cannotRefract=eta*sqrt", shader, StringComparison.Ordinal);
        Assert.Contains("reflect(rd,n)", shader, StringComparison.Ordinal);
    }

    [Fact]
    public void GpuShader_UsesVisibleNormalSamplingAndDeltaReflectionForNearPerfectNonGlassSurfaces()
    {
        var shader = GpuPathTracer.ComputeShaderSourceForTest;

        Assert.Contains("ggxVisibleHalf", shader, StringComparison.Ordinal);
        Assert.Contains("ggxVisibleReflectionPdf", shader, StringComparison.Ordinal);
        Assert.Contains("bool deltaSpecular=rough<=0.01&&metallic>=0.95", shader, StringComparison.Ordinal);
        Assert.Contains("geometryLight/specularProbability", shader, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0F, true)]
    [InlineData(0.01F, true)]
    [InlineData(0.01001F, false)]
    public void ClearGlassThreshold_SelectsDeltaReflectionAndRefraction(float roughness, bool expected)
    {
        Assert.Equal(expected, RenderOptics.IsDeltaGlass(roughness));
    }

    [Fact]
    public async Task GpuGlassShader_CompilesAndDispatchesOnAnAvailableComputeDevice()
    {
        var device = GpuPathTracer.GetAvailableDevices().FirstOrDefault(candidate => !candidate.IsAutomatic);
        if (device is null) return;
        var scene = RenderSceneSnapshot.Create(CreateProject());
        var options = new RenderOptions(8, 8, 1, 2, false, false, _outputPath);

        var result = await GpuPathTracer.TryRenderToPngAsync(
            scene, options, null, CancellationToken.None, device);

        Assert.Equal(GpuRenderStatus.Completed, result.Status);
        Assert.True(File.Exists(_outputPath), result.Message);
    }

    [Theory]
    [InlineData(1.5F, 0.04F)]
    [InlineData(1.33F, 0.020059F)]
    public void GlassReflectance_UsesMaterialIor(float ior, float expected)
    {
        Assert.Equal(expected, RenderOptics.NormalIncidenceReflectance(ior), 5);
    }

    [Fact]
    public void Snapshot_ConvertsViewportBackgroundFromSrgbToLinear()
    {
        var project = CreateProject();
        project.RenderSettings.BackgroundColor = new Vector4(0.055F, 0.065F, 0.08F, 1F);

        var scene = RenderSceneSnapshot.Create(project);

        Assert.InRange(scene.BackgroundColor.X, 0.004F, 0.005F);
        Assert.InRange(scene.BackgroundColor.Y, 0.005F, 0.006F);
        Assert.InRange(scene.BackgroundColor.Z, 0.007F, 0.008F);
    }

    [Fact]
    public async Task RenderToPng_WritesValidPng()
    {
        var scene = RenderSceneSnapshot.Create(CreateProject());
        var options = new RenderOptions(24, 16, 2, 2, false, false, _outputPath);

        var completed = await OfflinePathTracer.RenderToPngAsync(scene, options, null, CancellationToken.None);

        Assert.True(completed);
        Assert.True(File.Exists(_outputPath));
        var signature = new byte[8];
        await using var stream = File.OpenRead(_outputPath);
        Assert.Equal(8, await stream.ReadAsync(signature));
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, signature);
    }

    [Fact]
    public async Task RenderToPng_CancelDuringParallelLoop_ReturnsWithoutExceptionOrOutput()
    {
        var scene = RenderSceneSnapshot.Create(CreateProject());
        var options = new RenderOptions(512, 512, 256, 6, false, false, _outputPath);
        using var cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(20));

        var completed = await OfflinePathTracer.RenderToPngAsync(
            scene,
            options,
            null,
            cancellation.Token);

        Assert.False(completed);
        Assert.False(File.Exists(_outputPath));
    }

    [Fact]
    public async Task GpuRenderToPng_OpenGl43_WritesValidPng()
    {
        var scene = RenderSceneSnapshot.Create(CreateProject());
        var options = new RenderOptions(32, 24, 2, 2, false, false, _outputPath);

        var result = await GpuPathTracer.TryRenderToPngAsync(
            scene,
            options,
            null,
            CancellationToken.None);

        Assert.NotEqual(GpuRenderStatus.Canceled, result.Status);
        if (GpuPathTracer.GetAvailableDevices().Any(device => !device.IsAutomatic))
            Assert.Equal(GpuRenderStatus.Completed, result.Status);
        if (result.Status == GpuRenderStatus.Unavailable)
            Assert.DoesNotContain("Compute Shader", result.Message, StringComparison.OrdinalIgnoreCase);
        if (result.Status == GpuRenderStatus.Completed)
            Assert.True(File.Exists(_outputPath));
    }

    [Fact]
    public void GpuMaterialTexture_UsesSameVerticalOrientationAsRealtimeAndCpu()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rv3d-orientation-{Guid.NewGuid():N}.png");
        try
        {
            using (var bitmap = new Bitmap(1, 2))
            {
                bitmap.SetPixel(0, 0, Color.Red);
                bitmap.SetPixel(0, 1, Color.Blue);
                bitmap.Save(path);
            }

            var pixels = GpuPathTracer.LoadMaterialBitmapForTest(path, 1, 2);

            Assert.Equal(255, pixels[0]);
            Assert.Equal(0, pixels[2]);
            Assert.Equal(0, pixels[4]);
            Assert.Equal(255, pixels[6]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Theory]
    [InlineData("AMD")]
    [InlineData("Intel")]
    [InlineData("NVIDIA")]
    public void GpuCapabilities_AcceptsEveryVendorWhenRequiredOpenGlFeaturesExist(string vendor)
    {
        var capabilities = new GpuDeviceCapabilities(
            vendor,
            $"{vendor} Test GPU",
            "4.6",
            4,
            6,
            16,
            1024,
            16,
            2048,
            65535,
            65535);

        Assert.Null(capabilities.GetUnsupportedReason(3840, 2160, 128));
        Assert.Contains(vendor, capabilities.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GpuCapabilities_RejectsUnsupportedSceneBeforeDispatch()
    {
        var capabilities = new GpuDeviceCapabilities(
            "Legacy Vendor",
            "Legacy GPU",
            "4.2",
            4,
            2,
            4,
            32,
            4,
            8,
            16,
            16);

        Assert.Contains("OpenGL 4.3", capabilities.GetUnsupportedReason(1920, 1080, 16));
    }

    [Fact]
    public void GpuDeviceSelection_VerifiesTheActualOpenGlRenderer()
    {
        var capabilities = new GpuDeviceCapabilities(
            "Vendor A", "Renderer A", "4.6", 4, 6, 16, 1024, 16, 2048, 65535, 65535);
        var matching = new GpuRenderDevice("a", "GPU A", 0, 0, "Vendor A", "Renderer A");
        var different = new GpuRenderDevice("b", "GPU B", 0, 0, "Vendor B", "Renderer B");

        Assert.True(GpuRenderDevice.Automatic.Matches(capabilities));
        Assert.True(matching.Matches(capabilities));
        Assert.False(different.Matches(capabilities));
    }

    [Fact]
    public void ToneMap_ClampsHdrValues()
    {
        var mapped = OfflinePathTracer.ToneMapForTest(new Vector3(0F, 1F, 100F));

        Assert.InRange(mapped.X, 0F, 1F);
        Assert.InRange(mapped.Y, 0F, 1F);
        Assert.InRange(mapped.Z, 0F, 1F);
        Assert.True(mapped.Z >= mapped.Y);
    }

    [Fact]
    public void DisplayColorPipeline_AcesRoundTripsSceneColorForGlassRefraction()
    {
        var linear = new Vector3(0.02F, 0.5F, 4F);

        var decoded = RenderColorPipeline.DecodeDisplay(RenderColorPipeline.EncodeDisplay(linear));

        Assert.InRange(MathF.Abs(decoded.X - linear.X), 0F, 0.0001F);
        Assert.InRange(MathF.Abs(decoded.Y - linear.Y), 0F, 0.0001F);
        Assert.InRange(MathF.Abs(decoded.Z - linear.Z), 0F, 0.002F);
    }

    [Fact]
    public void Snapshot_UsesSameMaximumLightCountAsRealtimeAndGpuRenderers()
    {
        var project = CreateProject();
        project.Lights = Enumerable.Range(0, 24).Select(index => new SceneLight
        {
            Name = $"Light {index}",
            Enabled = true,
            Intensity = 1F
        }).ToList();

        var scene = RenderSceneSnapshot.Create(project);

        Assert.Equal(RenderSceneSnapshot.MaximumLights, scene.Lights.Length);
        Assert.Equal(16, scene.Lights.Length);
    }

    [Fact]
    public void GpuDenoiser_RemovesIsolatedFireflyAndPreservesHardEdge()
    {
        const int width = 9, height = 9;
        var pixels = new float[width * height * 4];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var offset = (y * width + x) * 4;
            var value = x < width / 2 ? 0.08F : 0.8F;
            pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = value;
            pixels[offset + 3] = 1F;
        }
        var firefly = (4 * width + 1) * 4;
        pixels[firefly] = pixels[firefly + 1] = pixels[firefly + 2] = 20F;

        var filtered = GpuPathTracer.DenoiseForTest(pixels, width, height);

        Assert.True(filtered[firefly] < 1F);
        Assert.True(filtered[(4 * width + 2) * 4] < 0.25F);
        Assert.True(filtered[(4 * width + 6) * 4] > 0.6F);
    }

    [Fact]
    public void GpuDenoiser_AovGuidesPreventFilteringAcrossGeometryBoundary()
    {
        const int width = 7, height = 3;
        var pixels = new float[width * height * 4];
        var normalDepth = new float[pixels.Length];
        var albedo = new float[pixels.Length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var offset = (y * width + x) * 4;
            pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = x < 3 ? 0.1F : 0.7F;
            pixels[offset + 3] = 1F;
            normalDepth[offset + (x < 3 ? 2 : 0)] = 1F;
            normalDepth[offset + 3] = x < 3 ? 1F : 5F;
            albedo[offset] = x < 3 ? 0.1F : 0.9F;
            albedo[offset + 1] = albedo[offset + 2] = albedo[offset];
            albedo[offset + 3] = 1F;
        }

        var filtered = GpuPathTracer.DenoiseForTest(pixels, normalDepth, albedo, width, height);

        Assert.InRange(filtered[(width + 2) * 4], 0.08F, 0.15F);
        Assert.InRange(filtered[(width + 3) * 4], 0.65F, 0.72F);
    }

    [Fact]
    public void GpuDenoiser_GlassMaskUsesSmallerKernelAtTheSameSamples()
    {
        const int width = 15, height = 5;
        var pixels = new float[width * height * 4];
        var normalDepth = new float[pixels.Length];
        var albedo = new float[pixels.Length];
        var glassMask = new float[pixels.Length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var offset = (y * width + x) * 4;
            var reference = x < 7 ? 0.2F : 0.8F;
            var noise = (x + y) % 2 == 0 ? 0.04F : -0.04F;
            pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = reference + noise;
            pixels[offset + 3] = 1F;
            normalDepth[offset + 2] = 1F;
            normalDepth[offset + 3] = 2F;
            albedo[offset] = albedo[offset + 1] = albedo[offset + 2] = 0.5F;
            albedo[offset + 3] = 1F;
            glassMask[offset] = 1F;
        }

        var ordinary = GpuPathTracer.DenoiseForTest(
            pixels, normalDepth, albedo, new float[pixels.Length], width, height);
        var glass = GpuPathTracer.DenoiseForTest(
            pixels, normalDepth, albedo, glassMask, width, height);
        var row = height / 2;
        var glassContrast = glass[(row * width + 7) * 4] - glass[(row * width + 6) * 4];
        var ordinaryContrast = ordinary[(row * width + 7) * 4] - ordinary[(row * width + 6) * 4];
        var sourceNoise = MathF.Abs(pixels[(row * width + 3) * 4] - 0.2F);
        var glassNoise = MathF.Abs(glass[(row * width + 3) * 4] - 0.2F);

        Assert.True(glassContrast > 0.5F,
            $"same-sample glass contrast {glassContrast} should preserve the 0.6 reference edge");
        Assert.NotEqual(ordinaryContrast, glassContrast);
        Assert.True(glassNoise < sourceNoise,
            $"same-sample glass noise {glassNoise} should be below source noise {sourceNoise}");
    }

    [Fact]
    public void GpuDenoiser_HighVarianceGlassUsesStrongerFilteringAtTheSameSamples()
    {
        const int width = 17, height = 7;
        var pixels = new float[width * height * 4];
        var normalDepth = new float[pixels.Length];
        var albedo = new float[pixels.Length];
        var lowVarianceGuides = new float[pixels.Length];
        var highVarianceGuides = new float[pixels.Length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var offset = (y * width + x) * 4;
            var noise = (x + y) % 2 == 0 ? 0.08F : -0.08F;
            pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = 0.5F + noise;
            pixels[offset + 3] = 1F;
            normalDepth[offset + 2] = 1F;
            normalDepth[offset + 3] = 2F;
            albedo[offset] = albedo[offset + 1] = albedo[offset + 2] = 0.5F;
            albedo[offset + 3] = 1F;
            lowVarianceGuides[offset] = highVarianceGuides[offset] = 1F;
            lowVarianceGuides[offset + 1] = highVarianceGuides[offset + 1] = 0.5F;
            lowVarianceGuides[offset + 2] = 0.25F;
            highVarianceGuides[offset + 2] = 0.29F;
        }

        var compact = GpuPathTracer.DenoiseForTest(
            pixels, normalDepth, albedo, lowVarianceGuides, width, height);
        var adaptive = GpuPathTracer.DenoiseForTest(
            pixels, normalDepth, albedo, highVarianceGuides, width, height);
        var compactError = 0F;
        var adaptiveError = 0F;
        for (var y = 2; y < height - 2; y++)
        for (var x = 2; x < width - 2; x++)
        {
            compactError += MathF.Abs(compact[(y * width + x) * 4] - 0.5F);
            adaptiveError += MathF.Abs(adaptive[(y * width + x) * 4] - 0.5F);
        }

        Assert.True(adaptiveError < compactError,
            $"adaptive variance error {adaptiveError} should be below compact error {compactError}");
    }

    [Fact]
    public void GpuDiagnostics_WritesDistinctPathsAndBuildsVarianceHeatmap()
    {
        var paths = GpuRenderDiagnosticPaths.FromOutputPath(_outputPath);
        var raw = new float[] { 1F, 1F, 1F, 1F, 0.5F, 0.5F, 0.5F, 1F };
        var denoised = new float[] { 0.8F, 0.8F, 0.8F, 1F, 0.5F, 0.5F, 0.5F, 1F };
        var guides = new float[] { 1F, 1F, 1.25F, 0F, 0F, 0.5F, 0.25F, 0F };

        var diagnostics = GpuRenderDiagnostics.Analyze(raw, denoised, guides, 2, 1, 256, "test denoiser");

        Assert.EndsWith("_raw.png", paths.RawPath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("_denoised.png", paths.DenoisedPath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("_variance.png", paths.VariancePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("_noise.json", paths.SummaryPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0.125D, diagnostics.Summary.MeanVariance, 6);
        Assert.Equal(0.25D, diagnostics.Summary.MaximumVariance, 6);
        Assert.True(diagnostics.Summary.RawDenoisedMeanAbsoluteDifference > 0D);
        Assert.Equal(0, diagnostics.Summary.InvalidPixelCount);
        Assert.Equal("test denoiser", diagnostics.Summary.DenoiserBackend);
        Assert.True(diagnostics.VarianceHeatmap[0] > diagnostics.VarianceHeatmap[4]);
    }

    [Fact]
    public void GpuDiagnostics_WritesSummaryWithoutReflectionSerialization()
    {
        var summaryPath = GpuRenderDiagnosticPaths.FromOutputPath(_outputPath).SummaryPath;
        var summary = new GpuRenderNoiseSummary(1920, 1080, 256, 0.125D, 0.25D, 0.5D, 0.01D, 0, 0, "test denoiser");

        GpuRenderDiagnostics.WriteSummary(summaryPath, summary);

        var json = File.ReadAllText(summaryPath);
        Assert.Contains("\"Width\": 1920", json, StringComparison.Ordinal);
        Assert.Contains("\"SamplesPerPixel\": 256", json, StringComparison.Ordinal);
        Assert.Contains("\"MaximumVariance\": 0.5", json, StringComparison.Ordinal);
        Assert.Contains("\"FireflyClampedPixelCount\": 0", json, StringComparison.Ordinal);
        Assert.Contains("\"DenoiserBackend\": \"test denoiser\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void GpuDiagnostics_NonFinitePixelsProduceValidJsonAndAreCounted()
    {
        var summaryPath = GpuRenderDiagnosticPaths.FromOutputPath(_outputPath).SummaryPath;
        var raw = new float[] { float.NaN, 1F, 1F, 1F, 0.5F, 0.5F, 0.5F, 1F };
        var denoised = new float[] { 0.8F, 0.8F, 0.8F, 1F, 0.5F, 0.5F, 0.5F, 1F };
        var guides = new float[] { 1F, float.PositiveInfinity, 1.25F, 0F, 0F, 0.5F, 0.25F, 0F };

        var diagnostics = GpuRenderDiagnostics.Analyze(raw, denoised, guides, 2, 1, 256, "test denoiser");
        GpuRenderDiagnostics.WriteSummary(summaryPath, diagnostics.Summary);

        var json = File.ReadAllText(summaryPath);
        Assert.Equal(1, diagnostics.Summary.InvalidPixelCount);
        Assert.Equal(0, diagnostics.Summary.FireflyClampedPixelCount);
        Assert.True(double.IsFinite(diagnostics.Summary.RawDenoisedMeanAbsoluteDifference));
        Assert.Contains("\"InvalidPixelCount\": 1", json, StringComparison.Ordinal);
        Assert.EndsWith("}", json.TrimEnd(), StringComparison.Ordinal);
    }

    [Fact]
    public void GpuShader_GuardsNonFinitePathsAndReportsThemThroughAov()
    {
        var shader = GpuPathTracer.ComputeShaderSourceForTest;

        Assert.Contains("safeNormalize", shader, StringComparison.Ordinal);
        Assert.Contains("!finiteVec3(throughput)", shader, StringComparison.Ordinal);
        Assert.Contains("invalidPath=1.0", shader, StringComparison.Ordinal);
        Assert.Contains("luminance*luminance,invalidPath", shader, StringComparison.Ordinal);
    }

    [Fact]
    public void GpuShader_UsesAdaptiveFireflyClampAndReportsAffectedPixels()
    {
        var shader = GpuPathTracer.ComputeShaderSourceForTest;

        Assert.Contains("previousCount>=8u", shader, StringComparison.Ordinal);
        Assert.Contains("priorMean+5.0*sqrt(priorVariance+0.0025)", shader, StringComparison.Ordinal);
        Assert.Contains("invalidPath-max(pathFireflyClamped,temporalFireflyClamped)", shader, StringComparison.Ordinal);
    }

    [Fact]
    public void GpuShader_UsesPerPixelSampleCountsForAdaptiveSampling()
    {
        var shader = GpuPathTracer.ComputeShaderSourceForTest;

        Assert.Contains("binding=8) buffer SampleCounts", shader, StringComparison.Ordinal);
        Assert.Contains("uint previousCount=sampleCounts[index]", shader, StringComparison.Ordinal);
        Assert.Contains("previousCount>=uint(uAdaptiveWarmupSamples)", shader, StringComparison.Ordinal);
        Assert.Contains("sampleCounts[index]=previousCount+1u", shader, StringComparison.Ordinal);
    }

    [Fact]
    public void GpuShader_UsesConfigurableMisWeightedHdrDirectLightSamples()
    {
        var shader = GpuPathTracer.ComputeShaderSourceForTest;

        Assert.Contains("environmentSample<uEnvironmentSampleCount", shader, StringComparison.Ordinal);
        Assert.Contains("environmentSampleWeight*ao*ndl*powerHeuristic", shader, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(RenderQualityPreset.Fast, 4)]
    [InlineData(RenderQualityPreset.Balanced, 8)]
    [InlineData(RenderQualityPreset.HighQuality, 24)]
    public void GpuShadowLayerLimitMatchesQualityPreset(RenderQualityPreset preset, int expected)
    {
        Assert.Equal(expected, GpuPathTracer.SelectMaximumShadowLayersForTest(preset));
    }

    [Theory]
    [InlineData(RenderQualityPreset.Fast, 8)]
    [InlineData(RenderQualityPreset.Balanced, 32)]
    [InlineData(RenderQualityPreset.HighQuality, 32)]
    public void GpuAdaptiveWarmupMatchesQualityPreset(RenderQualityPreset preset, int expected)
    {
        Assert.Equal(expected, GpuPathTracer.SelectAdaptiveWarmupSamplesForTest(preset));
    }

    [Fact]
    public void GpuShader_ProtectsAllTransmissiveMaterialsFromEarlyAdaptiveSampling()
    {
        var shader = GpuPathTracer.ComputeShaderSourceForTest;

        Assert.Contains("bool transmissiveLayer=mode==4||((mode==0||mode==3)&&opacity<0.999)", shader,
            StringComparison.Ordinal);
        Assert.Contains("if(!primarySurfaceResolved&&transmissiveLayer)primaryGlassMask=1.0", shader,
            StringComparison.Ordinal);
        Assert.Contains("primaryGlassMask=max(primaryGlassMask,(metallic>=0.75||rough<=0.08)?2.0:0.0)", shader,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GpuShader_RegularizesOnlyDeepGlossyPaths()
    {
        var shader = GpuPathTracer.ComputeShaderSourceForTest;

        Assert.Contains("regularizedRoughness(float rough,int bounce){return bounce>=2?max(rough,0.04):rough;}", shader,
            StringComparison.Ordinal);
        Assert.Contains("rough=regularizedRoughness(rough,bounce)", shader, StringComparison.Ordinal);
    }

    [Fact]
    public void GpuShader_ClampsExtremePathContributionsFromTheFirstSample()
    {
        var shader = GpuPathTracer.ComputeShaderSourceForTest;

        Assert.Contains("limitPathContribution", shader, StringComparison.Ordinal);
        Assert.Contains("const float maximumLuminance=12.0", shader, StringComparison.Ordinal);
        Assert.Contains("environment(rd,bounce==0)*misWeight", shader, StringComparison.Ordinal);
        Assert.Contains("pathFireflyClamped", shader, StringComparison.Ordinal);
    }

    [Fact]
    public void OidnDenoiser_SearchesApplicationAndPluginRuntimeLocations()
    {
        var candidates = OidnDenoiser.LibraryCandidatesForTest;

        Assert.Contains(candidates, path => path.EndsWith("OpenImageDenoise.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(candidates, path => path.Contains("native", StringComparison.OrdinalIgnoreCase));
        Assert.True(candidates.Count >= 4);
    }

    [Fact]
    public void EnvironmentImportanceSampler_PrefersBrightHdrTexelsAndReturnsSolidAnglePdf()
    {
        var pixels = new float[4 * 2 * 3];
        for (var index = 0; index < 8; index++)
        {
            var value = index == 3 ? 100F : 0.1F;
            pixels[index * 3] = pixels[index * 3 + 1] = pixels[index * 3 + 2] = value;
        }
        var sampler = EnvironmentImportanceSampler.Create(new HdrImage(4, 2, pixels));

        Assert.True(sampler.Mass[3] > sampler.Mass[0] * 100F);
        var sample = sampler.Sample(0.5F, 0.5F, 0.5F, 0F);
        Assert.True(sample.Pdf > 0F);
        Assert.True(float.IsFinite(sample.Pdf));
    }

    [Fact]
    public void EnvironmentImportanceSampler_UsesExactPixelSolidAngleAtPoles()
    {
        var sampler = EnvironmentImportanceSampler.Create(new HdrImage(2, 2, Enumerable.Repeat(1F, 12).ToArray()));

        var sample = sampler.Sample(0F, 0.5F, 0.5F, 0F);
        var expectedSolidAngle = MathF.PI;

        Assert.Equal(0.25F / expectedSolidAngle, sample.Pdf, 5);
    }

    [Fact]
    public void EnvironmentRotation_MatchesShaderConventionAndRoundTrips()
    {
        var world = Vector3.UnitX;
        var map = EnvironmentDirectionTransform.WorldToMap(world, MathF.PI / 2F);
        var restored = EnvironmentDirectionTransform.MapToWorld(map, MathF.PI / 2F);

        Assert.InRange(map.X, -0.00001F, 0.00001F);
        Assert.InRange(map.Z, -1.00001F, -0.99999F);
        Assert.InRange(Vector3.Distance(world, restored), 0F, 0.00001F);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 3)]
    [InlineData(32, 24)]
    public void RenderWorkerCount_ReservesCapacityForTheUi(int processors, int expected)
    {
        Assert.Equal(expected, OfflinePathTracer.GetRenderWorkerCount(processors));
    }

    [Fact]
    public void Snapshot_WithCamera_ChangesOnlyTheRenderCamera()
    {
        var scene = RenderSceneSnapshot.Create(CreateProject());
        var camera = new RenderCamera(new Vector3(4F, 3F, 2F), Vector3.One, Vector3.UnitY, 35F);

        var adjusted = scene.WithCamera(camera);

        Assert.Equal(camera, adjusted.Camera);
        Assert.Same(scene.Triangles, adjusted.Triangles);
        Assert.Same(scene.Materials, adjusted.Materials);
        Assert.Same(scene.Lights, adjusted.Lights);
    }

    [Fact]
    public void SharedRadianceHdrLoader_PreservesLinearHdrIntensity()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rv3d-plugin-hdr-{Guid.NewGuid():N}.hdr");
        try
        {
            using (var stream = File.Create(path))
            {
                stream.Write(Encoding.ASCII.GetBytes("#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y 1 +X 8\n"));
                stream.Write([2, 2, 0, 8]);
                stream.Write([136, 160]);
                stream.Write([136, 80]);
                stream.Write([136, 40]);
                stream.Write([136, 132]);
            }

            var image = RadianceHdrLoader.Load(path);

            Assert.Equal(8, image.Width);
            Assert.Equal(1, image.Height);
            Assert.Equal(10F, image.Pixels[0]);
            Assert.Equal(5F, image.Pixels[1]);
            Assert.Equal(2.5F, image.Pixels[2]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_outputPath)) File.Delete(_outputPath);
        var diagnostics = GpuRenderDiagnosticPaths.FromOutputPath(_outputPath);
        foreach (var path in new[] { diagnostics.RawPath, diagnostics.DenoisedPath,
                     diagnostics.VariancePath, diagnostics.SummaryPath })
            if (File.Exists(path)) File.Delete(path);
    }

    [Theory]
    [InlineData(0.000001F)]
    [InlineData(0.0001F)]
    [InlineData(1F)]
    [InlineData(1000F)]
    public void GeometryPrecision_PreservesHitsNormalsAndThinLayersAcrossScales(float scale)
    {
        var project = CreateProject();
        var mesh = project.Models[0].Meshes[0];
        mesh.Positions = mesh.Positions.Select(p => p * scale).ToArray();
        mesh.Normals = [];
        var triangle = RenderSceneSnapshot.Create(project).Triangles[0];
        Assert.True(Vector3.Distance(triangle.N0, Vector3.UnitZ) < 1e-5F);
        Assert.True(OfflinePathTracer.IntersectForTest([triangle], new(0, 0, scale * 3),
            -Vector3.UnitZ, scale * 4, out var distance));
        Assert.InRange(distance / scale, 2.999F, 3.001F);
        // Parallel slab axes and a ray exactly on a bounding-box edge must work.
        Assert.True(OfflinePathTracer.IntersectForTest([triangle], new(-scale, -scale, scale),
            -Vector3.UnitZ, scale * 2, out _));
        Assert.False(OfflinePathTracer.IntersectForTest([triangle], new(scale * 2, 0, scale),
            -Vector3.UnitZ, scale * 2, out _));
        var offset = RenderGeometryPrecision.RayOffset(Vector3.Zero, triangle);
        Assert.True(offset < scale * 0.001F);
        Assert.False(OfflinePathTracer.IntersectForTest([triangle], new(0, 0, offset),
            Vector3.UnitZ, scale, out _));
        Assert.True(OfflinePathTracer.IntersectForTest([triangle], new(0, 0, scale * 0.001F - offset),
            -Vector3.UnitZ, scale, out _));
    }

    [Theory]
    [InlineData(0.0001F)]
    [InlineData(1F)]
    [InlineData(1000F)]
    public async Task GpuGeometryPrecision_RendersSmallAndLargeEmissiveTriangles(float scale)
    {
        // xUnit runs on a worker thread. This Windows-only test owns a hidden
        // context and restores the library guard after the render completes.
        var checkMainThread = OpenTK.Windowing.Desktop.GLFWProvider.CheckForMainThread;
        OpenTK.Windowing.Desktop.GLFWProvider.CheckForMainThread = false;
        try
        {
            var device = GpuRenderDevice.Automatic;
            var project = CreateProject();
            var mesh = project.Models[0].Meshes[0];
            mesh.Positions = mesh.Positions.Select(p => p * scale).ToArray();
            mesh.Normals = [];
            project.Camera.From *= scale;
            project.Models[0].Materials[0].Emissive = Vector3.UnitX;
            project.Models[0].Materials[0].EmissiveStrength = 1F;
            project.Lights.Clear();
            project.RenderSettings.BackgroundColor = new Vector4(0, 0, 0, 1);
            var options = new RenderOptions(16, 16, 1, 1, false, false, _outputPath,
                DenoiserMode: RenderDenoiserMode.Disabled, AdaptiveSampling: false);
            var result = await GpuPathTracer.TryRenderToPngAsync(RenderSceneSnapshot.Create(project),
                options, null, CancellationToken.None, device);
            Assert.True(result.Status == GpuRenderStatus.Completed, result.Message);
            using var bitmap = new Bitmap(_outputPath);
            var center = bitmap.GetPixel(8, 8);
            Assert.True(center.R > 150 && center.G < 5 && center.B < 5, center.ToString());
        }
        finally
        {
            OpenTK.Windowing.Desktop.GLFWProvider.CheckForMainThread = checkMainThread;
        }
    }

    private static ViewerProject CreateProject()
    {
        var material = new PbrMaterial
        {
            BaseColor = new Vector4(0.8F, 0.25F, 0.1F, 1F),
            Metallic = 0.1F,
            Roughness = 0.45F,
            RenderMode = MaterialRenderMode.Opaque
        };
        var model = new SceneModel
        {
            Name = "Render test triangle",
            IsProcedural = true,
            Materials = [material],
            Meshes =
            [
                new MeshData
                {
                    Positions = [new(-1F, -1F, 0F), new(1F, -1F, 0F), new(0F, 1F, 0F)],
                    Normals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
                    TextureCoordinates = [Vector2.Zero, Vector2.UnitX, Vector2.UnitY],
                    Indices = [0, 1, 2],
                    MaterialIndex = 0
                }
            ]
        };
        return new ViewerProject
        {
            Camera = new CameraState { From = new Vector3(0F, 0F, 3F), To = Vector3.Zero, Up = Vector3.UnitY, FieldOfViewDegrees = 45F },
            Models = [model],
            Lights =
            [
                new SceneLight
                {
                    Type = SceneLightType.Directional,
                    Direction = Vector3.Normalize(new Vector3(0.2F, -0.4F, -1F)),
                    Intensity = 3F
                }
            ],
            RenderSettings = new RenderSettings { BackgroundColor = new Vector4(0.05F, 0.06F, 0.08F, 1F) }
        };
    }
}
