#define Enable_GpuRender
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
#if Enable_GpuRender
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
#endif
using Rv3dViewer.Core;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal enum GpuRenderStatus { Completed, Canceled, Unavailable }

internal sealed record GpuRenderTimings(
    TimeSpan Acceleration,
    TimeSpan Shader,
    TimeSpan Buffers,
    TimeSpan Textures,
    TimeSpan Trace,
    TimeSpan GpuWait,
    TimeSpan LongestGpuWait,
    int SynchronizationCount,
    TimeSpan Readback,
    TimeSpan Denoise,
    TimeSpan Save,
    double AverageSamples,
    double EarlyConvergedPixelPercentage)
{
    public string ToDisplayText() =>
        $"BVH {Acceleration.TotalSeconds:F2}s／Shader {Shader.TotalSeconds:F2}s／" +
        $"Buffer {Buffers.TotalSeconds:F2}s／貼圖 {Textures.TotalSeconds:F2}s／" +
        $"Trace {Trace.TotalSeconds:F2}s（GPU等待 {GpuWait.TotalSeconds:F2}s，" +
        $"同步 {SynchronizationCount} 次，最長 {LongestGpuWait.TotalSeconds:F2}s）／" +
        $"回讀 {Readback.TotalSeconds:F2}s／" +
        $"Denoise {Denoise.TotalSeconds:F2}s／PNG {Save.TotalSeconds:F2}s／" +
        $"平均 {AverageSamples:F1} Samples／提前收斂 {EarlyConvergedPixelPercentage:F1}%";
}

internal sealed record GpuRenderResult(
    GpuRenderStatus Status,
    string DeviceName,
    string Message,
    TimeSpan TraceElapsed,
    TimeSpan TotalElapsed,
    GpuRenderTimings? Timings)
{
    public static GpuRenderResult Completed(string device, TimeSpan traceElapsed, TimeSpan totalElapsed,
        GpuRenderTimings timings) =>
        new(GpuRenderStatus.Completed, device, string.Empty, traceElapsed, totalElapsed, timings);
    public static GpuRenderResult Canceled(string device) =>
        new(GpuRenderStatus.Canceled, device, string.Empty, TimeSpan.Zero, TimeSpan.Zero, null);
    public static GpuRenderResult Unavailable(string message) =>
        new(GpuRenderStatus.Unavailable, string.Empty, message, TimeSpan.Zero, TimeSpan.Zero, null);
}

internal sealed record GpuRenderDevice(
    string Id,
    string DisplayName,
    int WindowX,
    int WindowY,
    string ExpectedVendor,
    string ExpectedRenderer,
    bool IsAutomatic = false)
{
    public static GpuRenderDevice Automatic { get; } = new(
        "automatic", "自動選擇（作業系統）", 0, 0, string.Empty, string.Empty, true);

    public bool Matches(GpuDeviceCapabilities capabilities) => IsAutomatic ||
        string.Equals(ExpectedVendor, capabilities.Vendor, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(ExpectedRenderer, capabilities.Renderer, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => DisplayName;
}

internal sealed record GpuDeviceCapabilities(
    string Vendor,
    string Renderer,
    string Version,
    int MajorVersion,
    int MinorVersion,
    int ShaderStorageBufferBindings,
    int ComputeWorkGroupInvocations,
    int ComputeTextureImageUnits,
    int TextureArrayLayers,
    int WorkGroupCountX,
    int WorkGroupCountY)
{
    private const int RequiredShaderStorageBufferBindings = 9;
    private const int RequiredComputeWorkGroupInvocations = 64;
    private const int RequiredComputeTextureImageUnits = 5;

    public string DisplayName => $"{Renderer} · {Vendor} · OpenGL {MajorVersion}.{MinorVersion}";

    public string? GetUnsupportedReason(int width, int height, int materialCount)
    {
        if (MajorVersion < 4 || MajorVersion == 4 && MinorVersion < 3)
            return $"需要 OpenGL 4.3，目前為 {MajorVersion}.{MinorVersion}（{Renderer}）";
        if (ShaderStorageBufferBindings < RequiredShaderStorageBufferBindings)
            return $"GPU Shader Storage Buffer 數量不足：需要 {RequiredShaderStorageBufferBindings}，目前為 {ShaderStorageBufferBindings}";
        if (ComputeWorkGroupInvocations < RequiredComputeWorkGroupInvocations)
            return $"GPU Compute Work Group 容量不足：需要 {RequiredComputeWorkGroupInvocations}，目前為 {ComputeWorkGroupInvocations}";
        if (ComputeTextureImageUnits < RequiredComputeTextureImageUnits)
            return $"GPU Compute Texture Unit 數量不足：需要 {RequiredComputeTextureImageUnits}，目前為 {ComputeTextureImageUnits}";
        if (Math.Max(1, materialCount) > TextureArrayLayers)
            return $"場景材質數量 {materialCount} 超過 GPU Texture Array 上限 {TextureArrayLayers}";

        var groupCountX = (width + 7) / 8;
        var groupCountY = (height + 7) / 8;
        if (groupCountX > WorkGroupCountX || groupCountY > WorkGroupCountY)
            return $"輸出解析度需要 {groupCountX}×{groupCountY} 個 Compute Work Groups，GPU 上限為 {WorkGroupCountX}×{WorkGroupCountY}";
        return null;
    }
}

internal static class GpuPathTracer
{
    private static readonly ConditionalWeakTable<object, Lazy<PreparedGpuScene>> PreparedSceneCache = new();

    private readonly record struct GpuExecutionProfile(
        int TileHeight,
        int SamplesPerDispatch,
        int TilesPerSynchronization);

    private enum GpuWaitResult { Completed, Canceled, TimedOut }

    private static readonly TimeSpan GpuBatchTimeout = TimeSpan.FromSeconds(30);
    private static readonly GpuExecutionProfile SafeExecutionProfile = new(64, 1, 1);

    // Integrated GPUs keep conservative dispatches to avoid Windows TDR. A
    // discrete GPU receives larger tiles and several samples per dispatch so
    // the CPU does not starve it with thousands of tiny submissions.
    private static GpuExecutionProfile SelectExecutionProfile(GpuDeviceCapabilities capabilities)
        => SelectExecutionProfile(capabilities.Renderer);

    private static GpuExecutionProfile SelectExecutionProfile(string renderer)
    {
        if (renderer.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
            renderer.Contains("Integrated", StringComparison.OrdinalIgnoreCase))
            return SafeExecutionProfile;
        // AMD integrated GPUs share memory and are sensitive to long dispatches.
        // One sample per dispatch with four small tiles per fence is the fastest
        // measured stable profile for the integrated Radeon 860M.
        if (renderer.Contains("Radeon(TM)", StringComparison.OrdinalIgnoreCase) ||
            renderer.Contains("AMD Radeon Graphics", StringComparison.OrdinalIgnoreCase))
            return new GpuExecutionProfile(64, 1, 4);
        if (renderer.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
            return new GpuExecutionProfile(256, 8, 0);
        if (renderer.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
            renderer.Contains("AMD", StringComparison.OrdinalIgnoreCase))
            return new GpuExecutionProfile(256, 4, 0);
        return new GpuExecutionProfile(128, 4, 0);
    }

    internal static (int TileHeight, int SamplesPerDispatch, int TilesPerSynchronization)
        SelectExecutionProfileForTest(string renderer)
    {
        var profile = SelectExecutionProfile(renderer);
        return (profile.TileHeight, profile.SamplesPerDispatch, profile.TilesPerSynchronization);
    }

    internal static int EstimateSynchronizationCountForTest(string renderer, int height, int samples)
    {
        var profile = SelectExecutionProfile(renderer);
        var tilesPerBatch = (height + profile.TileHeight - 1) / profile.TileHeight;
        var synchronizationsPerBatch = profile.TilesPerSynchronization <= 0
            ? 1
            : (tilesPerBatch + profile.TilesPerSynchronization - 1) / profile.TilesPerSynchronization;
        var sampleBatches = (samples + profile.SamplesPerDispatch - 1) / profile.SamplesPerDispatch;
        return synchronizationsPerBatch * sampleBatches;
    }

    internal static int SelectMaximumShadowLayersForTest(RenderQualityPreset? preset) =>
        SelectMaximumShadowLayers(preset);

    private static int SelectMaximumShadowLayers(RenderQualityPreset? preset) => preset switch
    {
        RenderQualityPreset.Fast => 4,
        RenderQualityPreset.HighQuality => 24,
        _ => 8
    };

    internal static int SelectAdaptiveWarmupSamplesForTest(RenderQualityPreset? preset) =>
        SelectAdaptiveWarmupSamples(preset);

    private static int SelectAdaptiveWarmupSamples(RenderQualityPreset? preset) => preset switch
    {
        RenderQualityPreset.Fast => 8,
        _ => 32
    };
#if Enable_GpuRender
    internal static string ComputeShaderSourceForTest => ComputeShaderSource;
    internal static int GpuNodeSizeForTest => Marshal.SizeOf<GpuNode>();
    internal static int GpuTriangleSizeForTest => Marshal.SizeOf<GpuTriangleGeometry>();
    internal static int GpuTriangleShadingSizeForTest => Marshal.SizeOf<GpuTriangleShading>();
#endif

    public static async Task<GpuRenderResult> TryRenderToPngAsync(
        RenderSceneSnapshot scene,
        RenderOptions options,
        IProgress<RenderProgress>? progress,
        CancellationToken cancellationToken,
        GpuRenderDevice? selectedDevice = null,
        bool exportRenderDebugImage = false)
    {
#if Enable_GpuRender
        try
        {
            async Task<GpuRenderResult> RunAttemptAsync(GpuExecutionProfile? profile)
            {
                // Window/context creation remains on the caller (WinForms UI)
                // thread; only the expensive render work is transferred.
                OpenTK.Windowing.Desktop.NativeWindow? attemptWindow = null;
                try
                {
                    attemptWindow = CreateGpuWindow(selectedDevice, out var creationFailure);
                    if (attemptWindow is null)
                        return GpuRenderResult.Unavailable(creationFailure);
                    var renderWindow = attemptWindow;
                    renderWindow.Context.MakeNoneCurrent();
                    return await Task.Run(() =>
                        Render(renderWindow, scene, options, progress, cancellationToken,
                            selectedDevice, exportRenderDebugImage, profile));
                }
                finally
                {
                    attemptWindow?.Dispose();
                }
            }

            var result = await RunAttemptAsync(null);
            if (result.Status != GpuRenderStatus.Unavailable || cancellationToken.IsCancellationRequested ||
                !IsRetryableExecutionFailure(result.Message))
                return result;

            // A timed-out or lost context must never be reused. The retry gets a
            // fresh hidden window and OpenGL context with the most conservative profile.
            var retry = await RunAttemptAsync(SafeExecutionProfile);
            return retry.Status == GpuRenderStatus.Completed
                ? retry with { DeviceName = $"{retry.DeviceName}（安全模式重試）" }
                : retry;
        }
        catch (Exception ex)
        {
            return GpuRenderResult.Unavailable(ex.GetBaseException().Message);
        }
#else
        await Task.CompletedTask;
        return GpuRenderResult.Unavailable("GPU Render 未啟用");
#endif
    }

    public static IReadOnlyList<GpuRenderDevice> GetAvailableDevices()
    {
#if Enable_GpuRender
        return DiscoverAvailableDevices();
#else
        return new[] { GpuRenderDevice.Automatic };
#endif
    }

#if Enable_GpuRender
    private static IReadOnlyList<GpuRenderDevice> DiscoverAvailableDevices()
    {
        var devices = new List<GpuRenderDevice> { GpuRenderDevice.Automatic };
        var discovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var screen in Screen.AllScreens)
        {
            var candidate = new GpuRenderDevice(
                screen.DeviceName,
                screen.DeviceName,
                screen.Bounds.Left + Math.Max(0, screen.Bounds.Width / 2),
                screen.Bounds.Top + Math.Max(0, screen.Bounds.Height / 2),
                string.Empty,
                string.Empty);
            using var window = CreateGpuWindow(candidate, out _);
            if (window is null) continue;
            try
            {
                window.MakeCurrent();
                var capabilities = ReadCapabilities();
                if (capabilities.GetUnsupportedReason(8, 8, 1) is not null) continue;
                var key = $"{capabilities.Vendor}\n{capabilities.Renderer}";
                if (!discovered.Add(key)) continue;
                devices.Add(candidate with
                {
                    Id = key,
                    DisplayName = $"{capabilities.Renderer}（{capabilities.Vendor}）",
                    ExpectedVendor = capabilities.Vendor,
                    ExpectedRenderer = capabilities.Renderer
                });
            }
            catch
            {
                // Only verified OpenGL compute devices are presented to the user.
            }
            finally
            {
                if (window.Context.IsCurrent) window.Context.MakeNoneCurrent();
            }
        }
        return devices;
    }

    private static OpenTK.Windowing.Desktop.NativeWindow? CreateGpuWindow(
        GpuRenderDevice? selectedDevice,
        out string failure)
    {
        var errors = new List<string>();
        foreach (var profile in new[] { ContextProfile.Core, ContextProfile.Compatability, ContextProfile.Any })
        {
            try
            {
                failure = string.Empty;
                return new OpenTK.Windowing.Desktop.NativeWindow(new NativeWindowSettings
                {
                    API = ContextAPI.OpenGL,
                    APIVersion = new Version(4, 3),
                    Profile = profile,
                    Flags = ContextFlags.Offscreen,
                    StartVisible = false,
                    StartFocused = false,
                    Location = selectedDevice is { IsAutomatic: false }
                        ? new OpenTK.Mathematics.Vector2i(selectedDevice.WindowX, selectedDevice.WindowY)
                        : null,
                    ClientSize = new OpenTK.Mathematics.Vector2i(1, 1),
                    Title = $"Rv3d GPU Render ({profile})"
                });
            }
            catch (Exception ex)
            {
                errors.Add($"{profile}: {ex.GetBaseException().Message}");
            }
        }

        failure = $"無法建立 OpenGL 4.3 GPU Context：{string.Join("；", errors)}";
        return null;
    }

    private static GpuRenderResult Render(
        OpenTK.Windowing.Desktop.NativeWindow window,
        RenderSceneSnapshot scene,
        RenderOptions options,
        IProgress<RenderProgress>? progress,
        CancellationToken cancellationToken,
        GpuRenderDevice? selectedDevice,
        bool exportRenderDebugImage,
        GpuExecutionProfile? executionProfileOverride = null)
    {
        if (cancellationToken.IsCancellationRequested) return GpuRenderResult.Canceled(string.Empty);
        var totalStopwatch = Stopwatch.StartNew();
        try
        {
            window.MakeCurrent();
            var capabilities = ReadCapabilities();
            if (selectedDevice is { IsAutomatic: false } && !selectedDevice.Matches(capabilities))
                return GpuRenderResult.Unavailable(
                    $"指定 GPU 為 {selectedDevice.DisplayName}，但 OpenGL Context 實際使用 {capabilities.DisplayName}");
            var unsupportedReason = capabilities.GetUnsupportedReason(
                options.Width, options.Height, scene.Materials.Length);
            if (unsupportedReason is not null) return GpuRenderResult.Unavailable(unsupportedReason);

            var device = capabilities.DisplayName;
            var executionProfile = executionProfileOverride ?? SelectExecutionProfile(capabilities);
            var stageStopwatch = Stopwatch.StartNew();
            var preparedScene = GetPreparedScene(scene);
            var accelerationElapsed = stageStopwatch.Elapsed;
            var nodes = preparedScene.Nodes;
            var triangles = preparedScene.Geometry;
            var triangleShading = preparedScene.Shading;
            var sourceLights = scene.Lights.Take(RenderSceneSnapshot.MaximumLights).Select(GpuLight.Create).ToArray();
            var lightCount = sourceLights.Length;
            var lights = lightCount == 0 ? new[] { GpuLight.Default } : sourceLights;

            var program = 0;
            var nodeBuffer = 0;
            var triangleBuffer = 0;
            var lightBuffer = 0;
            var outputBuffer = 0;
            var normalDepthBuffer = 0;
            var albedoBuffer = 0;
            var glassMaskBuffer = 0;
            var sampleCountBuffer = 0;
            var environmentImportanceBuffer = 0;
            var environmentTexture = 0;
            var materialBuffer = 0;
            var materialBufferTexture = 0;
            var triangleShadingBuffer = 0;
            var triangleShadingTexture = 0;
            GpuMaterialTextures? materialTextures = null;
            try
            {
                stageStopwatch.Restart();
                program = CreateComputeProgram();
                var shaderElapsed = stageStopwatch.Elapsed;
                stageStopwatch.Restart();
                nodeBuffer = UploadBuffer(0, nodes);
                triangleBuffer = UploadBuffer(1, triangles);
                lightBuffer = UploadBuffer(2, lights);
                outputBuffer = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, outputBuffer);
                GL.BufferData(BufferTarget.ShaderStorageBuffer,
                    checked(options.Width * options.Height * 4 * sizeof(float)), IntPtr.Zero, BufferUsageHint.DynamicRead);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, outputBuffer);
                normalDepthBuffer = CreateOutputBuffer(4, options.Width, options.Height);
                albedoBuffer = CreateOutputBuffer(5, options.Width, options.Height);
                glassMaskBuffer = CreateOutputBuffer(7, options.Width, options.Height);
                sampleCountBuffer = CreateSampleCountBuffer(8, options.Width, options.Height);
                materialBuffer = CreateMaterialBuffer(scene.Materials, out materialBufferTexture);
                triangleShadingBuffer = CreateTextureBuffer(triangleShading, out triangleShadingTexture);
                var buffersElapsed = stageStopwatch.Elapsed;
                stageStopwatch.Restart();
                environmentTexture = CreateEnvironmentTexture(scene.Environment, out var environmentMaximumLod);
                var environmentImportance = options.UseHdriImportanceSampling
                    ? EnvironmentImportanceSampler.TryLoad(scene.Environment.Path)
                    : null;
                if (environmentImportance is not null)
                {
                    var bins = new Vector2[environmentImportance.Width * environmentImportance.Height];
                    for (var index = 0; index < bins.Length; index++)
                        bins[index] = new Vector2(environmentImportance.Cdf[index], environmentImportance.Mass[index]);
                    environmentImportanceBuffer = UploadBuffer(6, bins);
                }
                materialTextures = GpuMaterialTextures.Create(scene.Materials, options.TextureQuality);
                ThrowIfGlError("建立 GPU 場景資源");
                var texturesElapsed = stageStopwatch.Elapsed;

                GL.UseProgram(program);
                SetUniform(program, "uSize", options.Width, options.Height);
                SetUniform(program, "uTriangleCount", triangles.Length);
                SetUniform(program, "uLightCount", lightCount);
                SetUniform(program, "uMaxBounces", options.MaximumBounces);
                SetUniform(program, "uAdaptiveSampling", options.AdaptiveSampling ? 1 : 0);
                SetUniform(program, "uAdaptiveWarmupSamples", SelectAdaptiveWarmupSamples(options.QualityPreset));
                SetUniform(program, "uPhysicalGlass", options.GlassQuality == RenderGlassQuality.Physical ? 1 : 0);
                SetUniform(program, "uEnvironmentSampleCount", 1);
                SetUniform(program, "uMaxShadowLayers", SelectMaximumShadowLayers(options.QualityPreset));
                SetUniform(program, "uFireflyClamp", options.FireflyClamp);
                SetUniform(program, "uTransparent", options.TransparentBackground ? 1 : 0);
                SetUniform(program, "uUseEnvironment", options.UseEnvironment && scene.Environment.Enabled ? 1 : 0);
                SetUniform(program, "uHasEnvironmentTexture", environmentTexture != 0 ? 1 : 0);
                SetUniform(program, "uHasEnvironmentImportance", environmentImportance is not null ? 1 : 0);
                SetUniform(program, "uEnvironmentImportanceSize",
                    environmentImportance?.Width ?? 1, environmentImportance?.Height ?? 1);
                SetUniform(program, "uShowEnvironmentBackground", scene.Environment.ShowBackground ? 1 : 0);
                SetUniform(program, "uEnvironmentIntensity", scene.Environment.Intensity);
                SetUniform(program, "uEnvironmentRotation", scene.Environment.RotationRadians);
                SetUniform(program, "uEnvironmentBackgroundLod",
                    scene.Environment.BackgroundBlur * environmentMaximumLod);
                SetUniform(program, "uBackground", scene.BackgroundColor.X, scene.BackgroundColor.Y, scene.BackgroundColor.Z);
                if (environmentTexture != 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, environmentTexture);
                    SetUniform(program, "uEnvironmentMap", 0);
                }
                materialTextures.Bind(program);
                GL.ActiveTexture(TextureUnit.Texture5);
                GL.BindTexture(TextureTarget.TextureBuffer, materialBufferTexture);
                SetUniform(program, "uMaterials", 5);
                GL.ActiveTexture(TextureUnit.Texture6);
                GL.BindTexture(TextureTarget.TextureBuffer, triangleShadingTexture);
                SetUniform(program, "uTriangleShading", 6);
                SetCameraUniforms(program, scene.Camera, options.Width / (float)options.Height);

                var traceStopwatch = Stopwatch.StartNew();
                var gpuWaitElapsed = TimeSpan.Zero;
                var longestGpuWait = TimeSpan.Zero;
                var synchronizationCount = 0;
                for (var sampleStart = 0; sampleStart < options.SamplesPerPixel;
                     sampleStart += executionProfile.SamplesPerDispatch)
                {
                    if (cancellationToken.IsCancellationRequested) return GpuRenderResult.Canceled(device);
                    var samplesThisDispatch = Math.Min(
                        executionProfile.SamplesPerDispatch, options.SamplesPerPixel - sampleStart);
                    SetUniform(program, "uSampleStart", sampleStart);
                    SetUniform(program, "uSamplesThisDispatch", samplesThisDispatch);
                    var pendingTiles = 0;
                    for (var y = 0; y < options.Height; y += executionProfile.TileHeight)
                    {
                        if (cancellationToken.IsCancellationRequested) return GpuRenderResult.Canceled(device);
                        var tileHeight = Math.Min(executionProfile.TileHeight, options.Height - y);
                        SetUniform(program, "uDispatchOffset", 0, y);
                        GL.DispatchCompute((options.Width + 7) / 8, (tileHeight + 7) / 8, 1);
                        pendingTiles++;
                        if (executionProfile.TilesPerSynchronization > 0 &&
                            pendingTiles >= executionProfile.TilesPerSynchronization)
                        {
                            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
                            var waitResult = WaitForGpuCompletion(
                                cancellationToken, GpuBatchTimeout, out var waitElapsed);
                            gpuWaitElapsed += waitElapsed;
                            if (waitElapsed > longestGpuWait) longestGpuWait = waitElapsed;
                            synchronizationCount++;
                            if (waitResult == GpuWaitResult.Canceled)
                                return GpuRenderResult.Canceled(device);
                            if (waitResult == GpuWaitResult.TimedOut)
                                throw new TimeoutException($"GPU 批次超過 {GpuBatchTimeout.TotalSeconds:0} 秒仍未完成。");
                            pendingTiles = 0;
                        }
                    }
                    if (pendingTiles > 0)
                    {
                        GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
                        var waitResult = WaitForGpuCompletion(
                            cancellationToken, GpuBatchTimeout, out var waitElapsed);
                        gpuWaitElapsed += waitElapsed;
                        if (waitElapsed > longestGpuWait) longestGpuWait = waitElapsed;
                        synchronizationCount++;
                        if (waitResult == GpuWaitResult.Canceled)
                            return GpuRenderResult.Canceled(device);
                        if (waitResult == GpuWaitResult.TimedOut)
                            throw new TimeoutException($"GPU 批次超過 {GpuBatchTimeout.TotalSeconds:0} 秒仍未完成。");
                    }
                    var completedSamples = sampleStart + samplesThisDispatch;
                    ThrowIfGlError($"執行第 {completedSamples} 個 Sample");
                    progress?.Report(new RenderProgress(completedSamples, options.SamplesPerPixel));
                }
                traceStopwatch.Stop();

                if (cancellationToken.IsCancellationRequested) return GpuRenderResult.Canceled(device);
                stageStopwatch.Restart();
                var output = new float[checked(options.Width * options.Height * 4)];
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, outputBuffer);
                GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero,
                    output.Length * sizeof(float), output);
                var normalDepth = ReadOutputBuffer(normalDepthBuffer, options.Width, options.Height);
                var albedo = ReadOutputBuffer(albedoBuffer, options.Width, options.Height);
                var glassMask = ReadOutputBuffer(glassMaskBuffer, options.Width, options.Height);
                var sampleCounts = ReadSampleCounts(sampleCountBuffer, options.Width, options.Height);
                var averageSamples = sampleCounts.Length == 0 ? 0D : sampleCounts.Average(value => (double)value);
                var earlyConvergedPixelPercentage = sampleCounts.Length == 0 ? 0D :
                    sampleCounts.Count(value => value < options.SamplesPerPixel) * 100D / sampleCounts.Length;
                var rawOutput = (float[])output.Clone();
                var readbackElapsed = stageStopwatch.Elapsed;
                stageStopwatch.Restart();
                var denoiserBackend = "關閉";
                if (options.DenoiserMode == RenderDenoiserMode.BuiltIn)
                {
                    output = Denoise(output, normalDepth, albedo, glassMask,
                        options.Width, options.Height, cancellationToken);
                    denoiserBackend = "內建 AOV denoiser";
                }
                else if (options.DenoiserMode == RenderDenoiserMode.Oidn)
                {
                    if (OidnDenoiser.TryDenoise(rawOutput, normalDepth, albedo, options.Width, options.Height,
                        out var oidnDenoised, out var oidnBackend))
                    {
                        output = oidnDenoised;
                        denoiserBackend = oidnBackend;
                    }
                    else
                    {
                        output = Denoise(output, normalDepth, albedo, glassMask,
                            options.Width, options.Height, cancellationToken);
                        denoiserBackend = "內建 AOV denoiser（OIDN 不可用）";
                    }
                }
                var denoiseElapsed = stageStopwatch.Elapsed;
                if (cancellationToken.IsCancellationRequested) return GpuRenderResult.Canceled(device);
                stageStopwatch.Restart();
                OfflinePathTracer.SaveLinearGpuPixels(output, options.Width, options.Height, options.OutputPath);
                if (exportRenderDebugImage)
                {
                    try
                    {
                        var diagnosticPaths = GpuRenderDiagnosticPaths.FromOutputPath(options.OutputPath);
                        var diagnostics = GpuRenderDiagnostics.Analyze(
                            rawOutput, output, glassMask, options.Width, options.Height, options.SamplesPerPixel, denoiserBackend,
                            sampleCounts);
                        OfflinePathTracer.SaveLinearGpuPixels(
                            rawOutput, options.Width, options.Height, diagnosticPaths.RawPath);
                        OfflinePathTracer.SaveLinearGpuPixels(
                            output, options.Width, options.Height, diagnosticPaths.DenoisedPath);
                        OfflinePathTracer.SaveLinearGpuPixels(
                            diagnostics.VarianceHeatmap, options.Width, options.Height, diagnosticPaths.VariancePath);
                        GpuRenderDiagnostics.WriteSummary(diagnosticPaths.SummaryPath, diagnostics.Summary);
                    }
                    catch (Exception)
                    {
                        // Diagnostic sidecars are optional and must not turn a completed GPU render into a CPU fallback.
                    }
                }
                var saveElapsed = stageStopwatch.Elapsed;
                totalStopwatch.Stop();
                var timings = new GpuRenderTimings(
                    accelerationElapsed, shaderElapsed, buffersElapsed, texturesElapsed,
                    traceStopwatch.Elapsed, gpuWaitElapsed, longestGpuWait, synchronizationCount,
                    readbackElapsed, denoiseElapsed, saveElapsed,
                    averageSamples, earlyConvergedPixelPercentage);
                return GpuRenderResult.Completed(device, traceStopwatch.Elapsed, totalStopwatch.Elapsed, timings);
            }
            finally
            {
                if (program != 0) GL.DeleteProgram(program);
                foreach (var buffer in new[] { nodeBuffer, triangleBuffer, lightBuffer, outputBuffer, normalDepthBuffer,
                             albedoBuffer, glassMaskBuffer, sampleCountBuffer, environmentImportanceBuffer,
                             triangleShadingBuffer })
                    if (buffer != 0) GL.DeleteBuffer(buffer);
                if (environmentTexture != 0) GL.DeleteTexture(environmentTexture);
                if (materialBufferTexture != 0) GL.DeleteTexture(materialBufferTexture);
                if (materialBuffer != 0) GL.DeleteBuffer(materialBuffer);
                if (triangleShadingTexture != 0) GL.DeleteTexture(triangleShadingTexture);
                materialTextures?.Dispose();
            }
        }
        catch (Exception ex)
        {
            return GpuRenderResult.Unavailable(ex.GetBaseException().Message);
        }
        finally
        {
            if (window.Context.IsCurrent) window.Context.MakeNoneCurrent();
        }
    }

    private static bool IsRetryableExecutionFailure(string message) =>
        message.Contains("OpenGL", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("超過", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("逾時", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("context lost", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("device lost", StringComparison.OrdinalIgnoreCase);

    private static GpuDeviceCapabilities ReadCapabilities() => new(
        GL.GetString(StringName.Vendor) ?? "Unknown Vendor",
        GL.GetString(StringName.Renderer) ?? "OpenGL GPU",
        GL.GetString(StringName.Version) ?? "Unknown Version",
        GL.GetInteger(GetPName.MajorVersion),
        GL.GetInteger(GetPName.MinorVersion),
        GL.GetInteger(GetPName.MaxShaderStorageBufferBindings),
        GL.GetInteger(GetPName.MaxComputeWorkGroupInvocations),
        GL.GetInteger(GetPName.MaxComputeTextureImageUnits),
        GL.GetInteger(GetPName.MaxArrayTextureLayers),
        GetIndexedInteger((GetIndexedPName)0x91BE, 0), // GL_MAX_COMPUTE_WORK_GROUP_COUNT
        GetIndexedInteger((GetIndexedPName)0x91BE, 1));

    private static int GetIndexedInteger(GetIndexedPName name, int index)
    {
        GL.GetInteger(name, index, out var value);
        return value;
    }

    private static void ThrowIfGlError(string operation)
    {
        var error = GL.GetError();
        if (error != ErrorCode.NoError)
            throw new InvalidOperationException($"{operation}失敗：OpenGL {error}");
    }

    private static GpuWaitResult WaitForGpuCompletion(
        CancellationToken cancellationToken,
        TimeSpan timeout,
        out TimeSpan elapsed)
    {
        var fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var waitFlags = ClientWaitSyncFlags.SyncFlushCommandsBit;
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    return GpuWaitResult.Canceled;
                if (stopwatch.Elapsed >= timeout)
                    return GpuWaitResult.TimedOut;
                var status = GL.ClientWaitSync(fence, waitFlags, 20_000_000UL);
                waitFlags = ClientWaitSyncFlags.None;
                if (status == WaitSyncStatus.AlreadySignaled ||
                    status == WaitSyncStatus.ConditionSatisfied)
                    return GpuWaitResult.Completed;
                if (status == WaitSyncStatus.WaitFailed)
                    throw new InvalidOperationException("等待 GPU 完成時失敗。");
            }
        }
        finally
        {
            stopwatch.Stop();
            elapsed = stopwatch.Elapsed;
            GL.DeleteSync(fence);
        }
    }

    private static (GpuNode[] Nodes, GpuTriangleGeometry[] Geometry, GpuTriangleShading[] Shading) BuildScene(RenderSceneSnapshot scene)
    {
        var source = scene.Triangles;
        var indices = Enumerable.Range(0, source.Length).ToArray();
        var nodes = new List<GpuNode>();
        var orderedGeometry = new List<GpuTriangleGeometry>(source.Length);
        var orderedShading = new List<GpuTriangleShading>(source.Length);

        int Build(int start, int count)
        {
            var nodeIndex = nodes.Count;
            nodes.Add(default);
            var minimum = new Vector3(float.PositiveInfinity);
            var maximum = new Vector3(float.NegativeInfinity);
            var centerMin = minimum;
            var centerMax = maximum;
            for (var i = start; i < start + count; i++)
            {
                var triangle = source[indices[i]];
                minimum = Vector3.Min(minimum, Vector3.Min(triangle.P0, Vector3.Min(triangle.P1, triangle.P2)));
                maximum = Vector3.Max(maximum, Vector3.Max(triangle.P0, Vector3.Max(triangle.P1, triangle.P2)));
                var center = (triangle.P0 + triangle.P1 + triangle.P2) / 3F;
                centerMin = Vector3.Min(centerMin, center);
                centerMax = Vector3.Max(centerMax, center);
            }
            if (count <= 4)
            {
                var first = orderedGeometry.Count;
                for (var i = start; i < start + count; i++)
                {
                    var triangle = source[indices[i]];
                    var materialIndex = Math.Clamp(triangle.MaterialIndex, 0, Math.Max(0, scene.Materials.Length - 1));
                    orderedGeometry.Add(new GpuTriangleGeometry(triangle, materialIndex));
                    orderedShading.Add(new GpuTriangleShading(triangle));
                }
                nodes[nodeIndex] = GpuNode.Leaf(minimum, maximum, first, count);
                return nodeIndex;
            }
            var extent = centerMax - centerMin;
            var axis = extent.Y > extent.X ? 1 : 0;
            if ((axis == 0 ? extent.X : extent.Y) < extent.Z) axis = 2;
            Array.Sort(indices, start, count, Comparer<int>.Create((a, b) => Axis(Center(source[a]), axis).CompareTo(Axis(Center(source[b]), axis))));
            var leftCount = count / 2;
            var left = Build(start, leftCount);
            var right = Build(start + leftCount, count - leftCount);
            nodes[nodeIndex] = GpuNode.Branch(minimum, maximum, left, right);
            return nodeIndex;
        }

        Build(0, source.Length);
        return (nodes.ToArray(), orderedGeometry.ToArray(), orderedShading.ToArray());
    }

    private static PreparedGpuScene GetPreparedScene(RenderSceneSnapshot scene) =>
        PreparedSceneCache.GetValue(scene.AccelerationCacheKey,
            _ => new Lazy<PreparedGpuScene>(() =>
            {
                var (nodes, geometry, shading) = BuildScene(scene);
                return new PreparedGpuScene(nodes, geometry, shading);
            }, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    internal static object GetPreparedSceneForTest(RenderSceneSnapshot scene) => GetPreparedScene(scene);

    private sealed record PreparedGpuScene(
        GpuNode[] Nodes,
        GpuTriangleGeometry[] Geometry,
        GpuTriangleShading[] Shading);

    private static Vector3 Center(RenderTriangle triangle) => (triangle.P0 + triangle.P1 + triangle.P2) / 3F;
    private static float Axis(Vector3 value, int axis) => axis == 0 ? value.X : axis == 1 ? value.Y : value.Z;

    internal static float[] DenoiseForTest(float[] pixels, int width, int height) =>
        Denoise(pixels, CreateNeutralNormalDepth(pixels, width, height),
            CreateNeutralAlbedo(pixels), new float[pixels.Length], width, height, CancellationToken.None);

    internal static byte[] LoadMaterialBitmapForTest(string path, int width, int height) =>
        GpuMaterialTextures.TryLoadBitmap(path, width, height, out var pixels)
            ? pixels
            : throw new InvalidDataException("無法載入測試材質貼圖。");

    internal static float[] DenoiseForTest(float[] pixels, float[] normalDepth, float[] albedo, int width, int height) =>
        Denoise(pixels, normalDepth, albedo, new float[pixels.Length], width, height, CancellationToken.None);

    internal static float[] DenoiseForTest(float[] pixels, float[] normalDepth, float[] albedo,
        float[] glassMask, int width, int height) =>
        Denoise(pixels, normalDepth, albedo, glassMask, width, height, CancellationToken.None);

    private static float[] Denoise(float[] source, float[] normalDepth, float[] albedo, float[] glassMask,
        int width, int height, CancellationToken cancellationToken)
    {
        if (normalDepth.Length != source.Length || albedo.Length != source.Length || glassMask.Length != source.Length)
            throw new ArgumentException("AOV buffer dimensions must match the color buffer.");
        var fireflySuppressed = new float[source.Length];
        Parallel.For(0, height, y =>
        {
            if (cancellationToken.IsCancellationRequested) return;
            var neighborhood = new float[9];
            var deviations = new float[9];
            for (var x = 0; x < width; x++)
            {
                if ((x & 63) == 0 && cancellationToken.IsCancellationRequested) return;
                var centerOffset = (y * width + x) * 4;
                if (source[centerOffset + 3] <= 0.0001F)
                {
                    Array.Copy(source, centerOffset, fireflySuppressed, centerOffset, 4);
                    continue;
                }
                var count = 0;
                for (var oy = -1; oy <= 1; oy++)
                for (var ox = -1; ox <= 1; ox++)
                {
                    var nx = Math.Clamp(x + ox, 0, width - 1);
                    var ny = Math.Clamp(y + oy, 0, height - 1);
                    var offset = (ny * width + nx) * 4;
                    if (source[offset + 3] <= 0.0001F) continue;
                    if (MathF.Abs(glassMask[offset] - glassMask[centerOffset]) > 0.25F) continue;
                    if (NormalWeight(normalDepth, centerOffset, offset) < 0.1F ||
                        DepthWeight(normalDepth, centerOffset, offset) < 0.1F ||
                        AlbedoWeight(albedo, centerOffset, offset) < 0.1F) continue;
                    neighborhood[count++] = Luminance(source, offset);
                }
                var centerLuminance = Luminance(source, centerOffset);
                Array.Sort(neighborhood, 0, count);
                var median = count == 0 ? centerLuminance : neighborhood[count / 2];
                for (var index = 0; index < count; index++) deviations[index] = MathF.Abs(neighborhood[index] - median);
                Array.Sort(deviations, 0, count);
                var mad = count == 0 ? 0F : deviations[count / 2];
                var geometryEdge = IsGeometryEdge(normalDepth, albedo, glassMask, width, height, x, y);
                var ceiling = MathF.Max(median * (geometryEdge ? 8F : 4F),
                    median + mad * (geometryEdge ? 10F : 6F) + (geometryEdge ? 0.3F : 0.1F));
                var scale = centerLuminance > ceiling && centerLuminance > 0.0001F ? ceiling / centerLuminance : 1F;
                fireflySuppressed[centerOffset] = source[centerOffset] * scale;
                fireflySuppressed[centerOffset + 1] = source[centerOffset + 1] * scale;
                fireflySuppressed[centerOffset + 2] = source[centerOffset + 2] * scale;
                fireflySuppressed[centerOffset + 3] = source[centerOffset + 3];
            }
        });
        if (cancellationToken.IsCancellationRequested) return fireflySuppressed;

        var current = fireflySuppressed;
        foreach (var pass in new[] { (Step: 1, SigmaScale: 0.80F), (Step: 2, SigmaScale: 0.50F) })
        {
            if (cancellationToken.IsCancellationRequested) return current;
            var step = pass.Step;
            var filtered = new float[source.Length];
            Parallel.For(0, height, y =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                ReadOnlySpan<int> kernel = [1, 4, 6, 4, 1];
                for (var x = 0; x < width; x++)
                {
                    if ((x & 63) == 0 && cancellationToken.IsCancellationRequested) return;
                    var center = (y * width + x) * 4;
                    var centerAlpha = current[center + 3];
                    var centerLuminance = Luminance(current, center);
                    var isGlass = glassMask[center] >= 0.25F;
                    var noiseStrength = NoiseStrength(glassMask, center);
                    var geometryEdge = IsGeometryEdge(normalDepth, albedo, glassMask, width, height, x, y);
                    var compactGlassKernel = isGlass && (geometryEdge || noiseStrength < 0.2F);
                    var glassSigmaScale = geometryEdge ? 0.55F : 0.55F + noiseStrength * 0.7F;
                    var colorSigma = (0.025F + centerLuminance * 0.18F) * pass.SigmaScale *
                        (isGlass ? glassSigmaScale : 1F);
                    var sigmaSquared = colorSigma * colorSigma;
                    var sumR = 0F; var sumG = 0F; var sumB = 0F; var sumA = 0F; var weightSum = 0F;
                    for (var ky = -2; ky <= 2; ky++)
                    for (var kx = -2; kx <= 2; kx++)
                    {
                        if (compactGlassKernel && (step == 1 && (Math.Abs(kx) > 1 || Math.Abs(ky) > 1) ||
                                                  step > 1 && (kx != 0 || ky != 0))) continue;
                        var nx = Math.Clamp(x + kx * step, 0, width - 1);
                        var ny = Math.Clamp(y + ky * step, 0, height - 1);
                        var offset = (ny * width + nx) * 4;
                        var alphaDifference = MathF.Abs(current[offset + 3] - centerAlpha);
                        if (alphaDifference > 0.1F) continue;
                        var glassDifference = MathF.Abs(glassMask[offset] - glassMask[center]);
                        if (glassDifference > 0.25F) continue;
                        var dr = current[offset] - current[center];
                        var dg = current[offset + 1] - current[center + 1];
                        var db = current[offset + 2] - current[center + 2];
                        var colorDistance = dr * dr * 0.2126F + dg * dg * 0.7152F + db * db * 0.0722F;
                        var spatialWeight = kernel[kx + 2] * kernel[ky + 2];
                        var edgeWeight = MathF.Exp(-colorDistance / MathF.Max(2F * sigmaSquared, 0.000001F));
                        var weight = spatialWeight * edgeWeight * NormalWeight(normalDepth, center, offset) *
                            DepthWeight(normalDepth, center, offset) * AlbedoWeight(albedo, center, offset) *
                            MathF.Exp(-glassDifference * 8F);
                        sumR += current[offset] * weight;
                        sumG += current[offset + 1] * weight;
                        sumB += current[offset + 2] * weight;
                        sumA += current[offset + 3] * weight;
                        weightSum += weight;
                    }
                    var inverse = 1F / MathF.Max(weightSum, 0.0001F);
                    filtered[center] = sumR * inverse;
                    filtered[center + 1] = sumG * inverse;
                    filtered[center + 2] = sumB * inverse;
                    filtered[center + 3] = sumA * inverse;
                }
            });
            if (cancellationToken.IsCancellationRequested) return current;
            current = filtered;
        }

        // Restore only a small, clipped high-frequency residual. This keeps thin pipes and glass
        // borders crisp without bringing the Monte-Carlo noise or isolated fireflies back.
        Parallel.For(0, width * height, pixelIndex =>
        {
            if ((pixelIndex & 8191) == 0 && cancellationToken.IsCancellationRequested) return;
            var offset = pixelIndex * 4;
            var luminance = Luminance(current, offset);
            var residualLimit = 0.018F + luminance * 0.012F;
            for (var channel = 0; channel < 3; channel++)
            {
                var residual = (fireflySuppressed[offset + channel] - current[offset + channel]) * 0.12F;
                current[offset + channel] += Math.Clamp(residual, -residualLimit, residualLimit);
            }
        });
        return current;
    }

    private static float NoiseStrength(float[] denoiseGuides, int offset)
    {
        var mean = denoiseGuides[offset + 1];
        var variance = MathF.Max(0F, denoiseGuides[offset + 2] - mean * mean);
        return Math.Clamp(MathF.Sqrt(variance) / (0.015F + MathF.Abs(mean) * 0.08F), 0F, 1F);
    }

    private static bool IsGeometryEdge(float[] normalDepth, float[] albedo, float[] glassMask,
        int width, int height, int x, int y)
    {
        var center = (y * width + x) * 4;
        ReadOnlySpan<(int X, int Y)> neighbors = [(x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)];
        foreach (var neighbor in neighbors)
        {
            var nx = Math.Clamp(neighbor.X, 0, width - 1);
            var ny = Math.Clamp(neighbor.Y, 0, height - 1);
            var offset = (ny * width + nx) * 4;
            if (MathF.Abs(glassMask[offset] - glassMask[center]) > 0.25F ||
                NormalWeight(normalDepth, center, offset) < 0.6F ||
                DepthWeight(normalDepth, center, offset) < 0.35F ||
                AlbedoWeight(albedo, center, offset) < 0.35F)
                return true;
        }
        return false;
    }

    private static float NormalWeight(float[] normalDepth, int center, int offset)
    {
        var centerLength = MathF.Sqrt(normalDepth[center] * normalDepth[center] +
            normalDepth[center + 1] * normalDepth[center + 1] + normalDepth[center + 2] * normalDepth[center + 2]);
        var sampleLength = MathF.Sqrt(normalDepth[offset] * normalDepth[offset] +
            normalDepth[offset + 1] * normalDepth[offset + 1] + normalDepth[offset + 2] * normalDepth[offset + 2]);
        if (centerLength < 0.001F || sampleLength < 0.001F)
            return centerLength < 0.001F && sampleLength < 0.001F ? 1F : 0F;
        var dot = (normalDepth[center] * normalDepth[offset] + normalDepth[center + 1] * normalDepth[offset + 1] +
            normalDepth[center + 2] * normalDepth[offset + 2]) / (centerLength * sampleLength);
        return MathF.Pow(Math.Clamp(dot, 0F, 1F), 32F);
    }

    private static float DepthWeight(float[] normalDepth, int center, int offset)
    {
        var centerDepth = normalDepth[center + 3];
        var sampleDepth = normalDepth[offset + 3];
        if (centerDepth <= 0F || sampleDepth <= 0F)
            return centerDepth <= 0F && sampleDepth <= 0F ? 1F : 0F;
        return MathF.Exp(-MathF.Abs(centerDepth - sampleDepth) /
            MathF.Max(1e-12F, MathF.Min(centerDepth, sampleDepth) * 0.02F));
    }

    private static float AlbedoWeight(float[] albedo, int center, int offset)
    {
        var dr = albedo[offset] - albedo[center];
        var dg = albedo[offset + 1] - albedo[center + 1];
        var db = albedo[offset + 2] - albedo[center + 2];
        return MathF.Exp(-MathF.Sqrt(dr * dr + dg * dg + db * db) * 8F);
    }

    private static float[] CreateNeutralNormalDepth(float[] pixels, int width, int height)
    {
        var result = new float[pixels.Length];
        for (var index = 0; index < width * height; index++)
        {
            var offset = index * 4;
            if (pixels[offset + 3] <= 0.0001F) continue;
            result[offset + 2] = 1F;
            result[offset + 3] = 1F;
        }
        return result;
    }

    private static float[] CreateNeutralAlbedo(float[] pixels)
    {
        var result = new float[pixels.Length];
        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            result[offset] = result[offset + 1] = result[offset + 2] = 0.5F;
            result[offset + 3] = pixels[offset + 3];
        }
        return result;
    }

    private static float Luminance(float[] pixels, int offset) =>
        pixels[offset] * 0.2126F + pixels[offset + 1] * 0.7152F + pixels[offset + 2] * 0.0722F;

    private static int UploadBuffer<T>(int binding, T[] data) where T : struct
    {
        var buffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, data.Length * Marshal.SizeOf<T>(), data, BufferUsageHint.StaticDraw);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, binding, buffer);
        return buffer;
    }

    private static int CreateMaterialBuffer(
        IReadOnlyList<RenderMaterial> materials,
        out int texture)
    {
        var source = materials.Count == 0
            ? new[] { new GpuMaterial(RenderMaterial.Default) }
            : materials.Select(material => new GpuMaterial(material)).ToArray();
        return CreateTextureBuffer(source, out texture);
    }

    private static int CreateTextureBuffer<T>(T[] source, out int texture) where T : struct
    {
        var buffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.TextureBuffer, buffer);
        GL.BufferData(BufferTarget.TextureBuffer,
            source.Length * Marshal.SizeOf<T>(), source, BufferUsageHint.StaticDraw);
        texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureBuffer, texture);
        GL.TexBuffer(TextureBufferTarget.TextureBuffer, SizedInternalFormat.Rgba32f, buffer);
        return buffer;
    }

    private static int CreateOutputBuffer(int binding, int width, int height)
    {
        var buffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, checked(width * height * 4 * sizeof(float)),
            IntPtr.Zero, BufferUsageHint.DynamicRead);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, binding, buffer);
        return buffer;
    }

    private static float[] ReadOutputBuffer(int buffer, int width, int height)
    {
        var result = new float[checked(width * height * 4)];
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
        GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, result.Length * sizeof(float), result);
        return result;
    }

    private static int CreateSampleCountBuffer(int binding, int width, int height)
    {
        var buffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
        // BufferData with a null pointer leaves GPU memory undefined; counters
        // must start at zero before the first dispatch.
        GL.BufferData(BufferTarget.ShaderStorageBuffer, checked(width * height * sizeof(uint)),
            new uint[checked(width * height)], BufferUsageHint.DynamicRead);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, binding, buffer);
        return buffer;
    }

    private static uint[] ReadSampleCounts(int buffer, int width, int height)
    {
        var result = new uint[checked(width * height)];
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, buffer);
        GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, result.Length * sizeof(uint), result);
        return result;
    }

    private static int CreateComputeProgram()
    {
        var shader = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(shader, ComputeShaderSource);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var compiled);
        if (compiled == 0)
        {
            var message = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"GPU Compute Shader 編譯失敗：{message}");
        }
        var program = GL.CreateProgram();
        GL.AttachShader(program, shader);
        GL.LinkProgram(program);
        GL.DetachShader(program, shader);
        GL.DeleteShader(shader);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var linked);
        if (linked != 0) return program;
        var log = GL.GetProgramInfoLog(program);
        GL.DeleteProgram(program);
        throw new InvalidOperationException($"GPU Compute Shader 連結失敗：{log}");
    }

    private static void SetCameraUniforms(int program, RenderCamera camera, float aspect)
    {
        var forward = RenderSceneSnapshot.SafeNormalize(camera.To - camera.From, -Vector3.UnitZ);
        var right = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(forward, camera.Up), Vector3.UnitX);
        var up = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(right, forward), Vector3.UnitY);
        SetUniform(program, "uCameraFrom", camera.From.X, camera.From.Y, camera.From.Z);
        SetUniform(program, "uForward", forward.X, forward.Y, forward.Z);
        SetUniform(program, "uRight", right.X, right.Y, right.Z);
        SetUniform(program, "uUp", up.X, up.Y, up.Z);
        SetUniform(program, "uAspect", aspect);
        SetUniform(program, "uHalfHeight", MathF.Tan(camera.FieldOfViewDegrees * MathF.PI / 360F));
    }

    private static void SetUniform(int program, string name, int value) => GL.Uniform1(GL.GetUniformLocation(program, name), value);
    private static void SetUniform(int program, string name, float value) => GL.Uniform1(GL.GetUniformLocation(program, name), value);
    private static void SetUniform(int program, string name, int x, int y) => GL.Uniform2(GL.GetUniformLocation(program, name), x, y);
    private static void SetUniform(int program, string name, float x, float y, float z) => GL.Uniform3(GL.GetUniformLocation(program, name), x, y, z);

    private static int CreateEnvironmentTexture(RenderEnvironment environment, out float maximumLod)
    {
        maximumLod = 0F;
        if (!environment.Enabled || environment.Path is null ||
            !string.Equals(Path.GetExtension(environment.Path), ".hdr", StringComparison.OrdinalIgnoreCase)) return 0;
        try
        {
            var image = RadianceHdrLoader.Load(environment.Path);
            var texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb32f, image.Width, image.Height, 0,
                PixelFormat.Rgb, PixelType.Float, image.Pixels);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            maximumLod = MathF.Floor(MathF.Log2(Math.Max(image.Width, image.Height)));
            return texture;
        }
        catch
        {
            return 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GpuNode
    {
        public readonly Vector4 Minimum;
        public readonly Vector4 Maximum;
        private GpuNode(Vector3 min, float minimumData, Vector3 max, float maximumData)
        {
            Minimum = new(min, minimumData);
            Maximum = new(max, maximumData);
        }

        // A negative Minimum.W identifies a leaf and stores -(start + 1).
        // Branch nodes store their child indices directly in the two W fields.
        public static GpuNode Leaf(Vector3 min, Vector3 max, int start, int count) =>
            new(min, -(start + 1F), max, count);
        public static GpuNode Branch(Vector3 min, Vector3 max, int left, int right) =>
            new(min, left, max, right);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GpuTriangleGeometry
    {
        public readonly Vector4 P0, P1, P2;
        public GpuTriangleGeometry(RenderTriangle t, int materialIndex)
        {
            P0 = new(t.P0, materialIndex); P1 = new(t.P1, 0F); P2 = new(t.P2, 0F);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GpuTriangleShading
    {
        public readonly Vector4 N0, N1, N2, Uv0, Uv1, Uv2, T0, T1, T2;
        public GpuTriangleShading(RenderTriangle t)
        {
            N0 = new(t.N0, 0F); N1 = new(t.N1, 0F); N2 = new(t.N2, 0F);
            Uv0 = new(t.Uv0, 0F, 0F); Uv1 = new(t.Uv1, 0F, 0F); Uv2 = new(t.Uv2, 0F, 0F);
            T0 = new(t.T0, 0F); T1 = new(t.T1, 0F); T2 = new(t.T2, 0F);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GpuMaterial
    {
        public readonly Vector4 BaseColor, Surface, Emission, Optics, Absorption, TextureInfo;

        public GpuMaterial(RenderMaterial material)
        {
            BaseColor = material.BaseColor;
            Surface = new(material.Metallic, material.Roughness, material.Opacity, (float)material.RenderMode);
            Emission = new(material.Emissive, material.AmbientOcclusion);
            Optics = new(material.Transmission, material.IndexOfRefraction, material.Thickness, material.AlphaCutoff);
            Absorption = new(material.AbsorptionColor, material.DoubleSided ? 1F : 0F);
            TextureInfo = new(material.NormalScale, 0F, 0F, 0F);
        }
    }

    private sealed class GpuMaterialTextures : IDisposable
    {
        private readonly int _baseColor;
        private readonly int _surface;
        private readonly int _normal;
        private readonly int _emissive;

        private GpuMaterialTextures(int baseColor, int surface, int normal, int emissive)
        { _baseColor = baseColor; _surface = surface; _normal = normal; _emissive = emissive; }

        public static GpuMaterialTextures Create(IReadOnlyList<RenderMaterial> materials,
            RenderTextureQuality textureQuality = RenderTextureQuality.Original)
        {
            var layers = Math.Max(1, materials.Count);
            var (width, height) = ChooseAtlasDimensions(materials, GL.GetInteger(GetPName.MaxTextureSize));
            var qualityLimit = textureQuality switch
            {
                RenderTextureQuality.Low => 512,
                RenderTextureQuality.Medium => 1024,
                _ => int.MaxValue
            };
            width = Math.Min(width, qualityLimit);
            height = Math.Min(height, qualityLimit);
            var baseColor = CreateArray(layers, width, height, PixelInternalFormat.Srgb8Alpha8, layer =>
                LoadColor(materials, layer, TextureSemantic.BaseColor, width, height));
            var surface = CreateArray(layers, width, height, PixelInternalFormat.Rgba8, layer =>
                LoadSurface(materials, layer, width, height));
            var normal = CreateArray(layers, width, height, PixelInternalFormat.Rgba8, layer =>
                LoadNormal(materials, layer, width, height));
            var emissive = CreateArray(layers, width, height, PixelInternalFormat.Srgb8Alpha8, layer =>
                LoadColor(materials, layer, TextureSemantic.Emissive, width, height));
            return new GpuMaterialTextures(baseColor, surface, normal, emissive);
        }

        public void Bind(int program)
        {
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2DArray, _baseColor);
            SetUniform(program, "uBaseColorTextures", 1);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2DArray, _surface);
            SetUniform(program, "uSurfaceTextures", 2);
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2DArray, _normal);
            SetUniform(program, "uNormalTextures", 3);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2DArray, _emissive);
            SetUniform(program, "uEmissiveTextures", 4);
        }

        public void Dispose()
        {
            GL.DeleteTexture(_baseColor);
            GL.DeleteTexture(_surface);
            GL.DeleteTexture(_normal);
            GL.DeleteTexture(_emissive);
        }

        private static (int Width, int Height) ChooseAtlasDimensions(
            IReadOnlyList<RenderMaterial> materials, int maximumTextureSize)
        {
            var width = 1;
            var height = 1;
            foreach (var path in materials.SelectMany(material => material.TexturePaths.Values).Distinct())
            {
                try
                {
                    using var image = Image.FromFile(path);
                    width = Math.Max(width, image.Width);
                    height = Math.Max(height, image.Height);
                }
                catch { }
            }
            return (Math.Min(width, maximumTextureSize), Math.Min(height, maximumTextureSize));
        }

        private static int CreateArray(int layers, int width, int height, PixelInternalFormat format,
            Func<int, byte[]> loadLayer)
        {
            var texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, texture);
            GL.TexImage3D(TextureTarget.Texture2DArray, 0, format, width, height, layers, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            for (var layer = 0; layer < layers; layer++)
            {
                var pixels = loadLayer(layer);
                GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, layer, width, height, 1,
                    PixelFormat.Bgra, PixelType.UnsignedByte, pixels);
            }
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            return texture;
        }

        private static byte[] LoadColor(IReadOnlyList<RenderMaterial> materials, int layer,
            TextureSemantic semantic, int width, int height)
        {
            if ((uint)layer < (uint)materials.Count && materials[layer].TexturePaths.TryGetValue(semantic, out var path) &&
                TryLoadBitmap(path, width, height, out var pixels)) return pixels;
            var white = new byte[width * height * 4];
            Array.Fill(white, (byte)255);
            return white;
        }

        private static byte[] LoadSurface(IReadOnlyList<RenderMaterial> materials, int layer, int width, int height)
        {
            var result = new byte[width * height * 4];
            Array.Fill(result, (byte)255);
            if ((uint)layer >= (uint)materials.Count) return result;
            var material = materials[layer];
            ApplyScalar(material, TextureSemantic.Metallic, result, 2, width, height);
            ApplyScalar(material, TextureSemantic.Roughness, result, 1, width, height);
            ApplyScalar(material, TextureSemantic.AmbientOcclusion, result, 0, width, height);
            ApplyScalar(material, TextureSemantic.Opacity, result, 3, width, height);
            return result;
        }

        private static byte[] LoadNormal(IReadOnlyList<RenderMaterial> materials, int layer, int width, int height)
        {
            if ((uint)layer < (uint)materials.Count &&
                materials[layer].TexturePaths.TryGetValue(TextureSemantic.Normal, out var path) &&
                TryLoadBitmap(path, width, height, out var pixels)) return pixels;
            var neutral = new byte[width * height * 4];
            for (var offset = 0; offset < neutral.Length; offset += 4)
            {
                neutral[offset] = 255;
                neutral[offset + 1] = 128;
                neutral[offset + 2] = 128;
                neutral[offset + 3] = 255;
            }
            return neutral;
        }

        private static void ApplyScalar(RenderMaterial material, TextureSemantic semantic, byte[] target,
            int bgraChannel, int width, int height)
        {
            if (!material.TexturePaths.TryGetValue(semantic, out var path) ||
                !TryLoadBitmap(path, width, height, out var pixels)) return;
            for (var offset = 0; offset < target.Length; offset += 4) target[offset + bgraChannel] = pixels[offset + 2];
        }

        internal static bool TryLoadBitmap(string path, int width, int height, out byte[] pixels)
        {
            try
            {
                using var source = Image.FromFile(path);
                using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(source, 0, 0, width, height);
                }
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                var data = bitmap.LockBits(new Rectangle(0, 0, width, height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    pixels = new byte[width * height * 4];
                    for (var y = 0; y < height; y++)
                        Marshal.Copy(data.Scan0 + y * data.Stride, pixels, y * width * 4, width * 4);
                    return true;
                }
                finally { bitmap.UnlockBits(data); }
            }
            catch { pixels = null!; return false; }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GpuLight
    {
        public readonly Vector4 PositionType, DirectionRange, ColorIntensity, Cone;
        private GpuLight(Vector4 p, Vector4 d, Vector4 c, Vector4 cone) { PositionType = p; DirectionRange = d; ColorIntensity = c; Cone = cone; }
        public static GpuLight Create(RenderLight light) => new(new(light.Position, (float)light.Type), new(light.Direction, light.Range), new(light.Color, light.Intensity), new(light.InnerCos, light.OuterCos, 0F, 0F));
        public static GpuLight Default => new(new(0F, 0F, 0F, 0F), new(0.3F, -0.7F, -0.5F, 0F), new(1F, 1F, 1F, 2F), Vector4.Zero);
    }

    private const string ComputeShaderSource = """
#version 430 core
layout(local_size_x=8,local_size_y=8) in;
struct Node{vec4 mn;vec4 mx;};
struct Tri{vec4 p0;vec4 p1;vec4 p2;};
struct Shade{vec4 n0;vec4 n1;vec4 n2;vec4 uv0;vec4 uv1;vec4 uv2;vec4 t0;vec4 t1;vec4 t2;};
struct Mat{vec4 base;vec4 surface;vec4 emission;vec4 optics;vec4 absorption;vec4 textureInfo;};
struct Light{vec4 positionType;vec4 directionRange;vec4 colorIntensity;vec4 cone;};
layout(std430,binding=0) readonly buffer Nodes{Node nodes[];};
layout(std430,binding=1) readonly buffer Tris{Tri tris[];};
layout(std430,binding=2) readonly buffer Lights{Light lights[];};
layout(std430,binding=3) buffer Output{vec4 pixels[];};
layout(std430,binding=4) buffer NormalDepth{vec4 normalDepthPixels[];};
layout(std430,binding=5) buffer Albedo{vec4 albedoPixels[];};
layout(std430,binding=6) readonly buffer EnvironmentBins{vec2 environmentBins[];};
layout(std430,binding=7) buffer GlassMask{vec4 glassMaskPixels[];};
layout(std430,binding=8) buffer SampleCounts{uint sampleCounts[];};
uniform ivec2 uSize,uEnvironmentImportanceSize,uDispatchOffset; uniform int uTriangleCount,uLightCount,uMaxBounces,uTransparent,uUseEnvironment,uSampleStart,uSamplesThisDispatch,uAdaptiveSampling,uAdaptiveWarmupSamples,uPhysicalGlass,uEnvironmentSampleCount,uMaxShadowLayers;
uniform int uHasEnvironmentImportance;
uniform int uHasEnvironmentTexture,uShowEnvironmentBackground; uniform sampler2D uEnvironmentMap;
uniform sampler2DArray uBaseColorTextures,uSurfaceTextures,uNormalTextures,uEmissiveTextures;
uniform samplerBuffer uMaterials;
uniform samplerBuffer uTriangleShading;
uniform float uAspect,uHalfHeight,uEnvironmentIntensity,uEnvironmentRotation,uEnvironmentBackgroundLod,uFireflyClamp; uniform vec3 uCameraFrom,uForward,uRight,uUp,uBackground;
uint hash(uint x){x+=(x<<10u);x^=(x>>6u);x+=(x<<3u);x^=(x>>11u);x+=(x<<15u);return x;}
float rnd(inout uint s){s=hash(s);return float(s)/4294967296.0;}
bool finiteFloat(float v){return !isnan(v)&&!isinf(v);}
bool finiteVec3(vec3 v){return finiteFloat(v.x)&&finiteFloat(v.y)&&finiteFloat(v.z);}
vec3 sanitizeVec3(vec3 v){return vec3(finiteFloat(v.x)?v.x:0.0,finiteFloat(v.y)?v.y:0.0,finiteFloat(v.z)?v.z:0.0);}
Mat loadMaterial(int index){int offset=index*6;return Mat(texelFetch(uMaterials,offset),texelFetch(uMaterials,offset+1),texelFetch(uMaterials,offset+2),texelFetch(uMaterials,offset+3),texelFetch(uMaterials,offset+4),texelFetch(uMaterials,offset+5));}
Shade loadShading(int index){int offset=index*9;return Shade(texelFetch(uTriangleShading,offset),texelFetch(uTriangleShading,offset+1),texelFetch(uTriangleShading,offset+2),texelFetch(uTriangleShading,offset+3),texelFetch(uTriangleShading,offset+4),texelFetch(uTriangleShading,offset+5),texelFetch(uTriangleShading,offset+6),texelFetch(uTriangleShading,offset+7),texelFetch(uTriangleShading,offset+8));}
vec3 safeNormalize(vec3 v,vec3 fallback){float scale=max(abs(v.x),max(abs(v.y),abs(v.z)));if(!finiteFloat(scale)||scale<=0.0)return fallback;vec3 scaled=v/scale;return scaled*inversesqrt(dot(scaled,scaled));}
float rayOffset(vec3 p,Tri t){float edge=max(length(t.p1.xyz-t.p0.xyz),max(length(t.p2.xyz-t.p0.xyz),length(t.p2.xyz-t.p1.xyz)));float coordinate=max(abs(p.x),max(abs(p.y),abs(p.z)));return max(edge*1e-5,max(coordinate*4.7683716e-7,1e-12));}
vec3 limitPathContribution(vec3 contribution,inout float fireflyClamped){vec3 safeContribution=sanitizeVec3(contribution);float luminance=max(dot(safeContribution,vec3(0.2126,0.7152,0.0722)),0.0);const float maximumLuminance=12.0;float effectiveMaximumLuminance=uFireflyClamp>0.0?uFireflyClamp:maximumLuminance;if(luminance>effectiveMaximumLuminance){fireflyClamped=1.0;safeContribution*=effectiveMaximumLuminance/max(luminance,0.000001);}return safeContribution;}
bool boxHit(vec3 ro,vec3 rd,vec3 mn,vec3 mx,float maxT){float nearT=0.0,farT=maxT;for(int axis=0;axis<3;axis++){if(rd[axis]==0.0){if(ro[axis]<mn[axis]||ro[axis]>mx[axis])return false;continue;}float a=(mn[axis]-ro[axis])/rd[axis],b=(mx[axis]-ro[axis])/rd[axis];nearT=max(nearT,min(a,b));farT=min(farT,max(a,b));if(farT<nearT)return false;}return nearT<maxT;}
bool triHit(Tri t,vec3 ro,vec3 rd,float maxT,out float dist,out vec2 uv){vec3 e1=t.p1.xyz-t.p0.xyz,e2=t.p2.xyz-t.p0.xyz,p=cross(rd,e2);float d=dot(e1,p);float areaScale=length(cross(e1,e2));if(!finiteFloat(d)||areaScale<=0.0||abs(d)<=areaScale*1e-7)return false;float inv=1.0/d;vec3 q=ro-t.p0.xyz;uv.x=dot(q,p)*inv;if(uv.x<0.0||uv.x>1.0)return false;vec3 r=cross(q,e1);uv.y=dot(rd,r)*inv;if(uv.y<0.0||uv.x+uv.y>1.0)return false;dist=dot(e2,r)*inv;return finiteFloat(dist)&&dist>0.0&&dist<maxT;}
bool trace(vec3 ro,vec3 rd,float maxT,out int hitTri,out float hitT,out vec2 hitUv){int stack[64];int top=0;stack[0]=0;hitTri=-1;hitT=maxT;vec3 inv=1.0/rd;while(top>=0){int ni=stack[top--];Node n=nodes[ni];if(!boxHit(ro,rd,n.mn.xyz,n.mx.xyz,hitT))continue;if(n.mn.w<0.0){int start=int(-n.mn.w-1.0+0.5),count=int(n.mx.w+0.5);for(int i=0;i<count;i++){float t;vec2 uv;if(triHit(tris[start+i],ro,rd,hitT,t,uv)){hitT=t;hitTri=start+i;hitUv=uv;}}}else if(top<61){stack[++top]=int(n.mn.w+0.5);stack[++top]=int(n.mx.w+0.5);}}return hitTri>=0;}
vec3 tangent(vec3 n){return safeNormalize(cross(abs(n.y)>0.98?vec3(1,0,0):vec3(0,1,0),n),vec3(1,0,0));}
vec3 diffuseDir(vec3 n,inout uint s){float a=6.2831853*rnd(s),r=sqrt(rnd(s));vec3 t=tangent(n),b=cross(n,t);return safeNormalize(t*cos(a)*r+b*sin(a)*r+n*sqrt(max(0.0,1.0-r*r)),n);}
vec3 ggxHalf(vec3 n,float rough,inout uint s){float alpha=max(rough*rough,0.000001),phi=6.2831853*rnd(s),xi=min(rnd(s),0.999999);float cosTheta=sqrt((1.0-xi)/(1.0+(alpha*alpha-1.0)*xi)),sinTheta=sqrt(max(0.0,1.0-cosTheta*cosTheta));vec3 t=tangent(n),b=cross(n,t);return safeNormalize(t*cos(phi)*sinTheta+b*sin(phi)*sinTheta+n*cosTheta,n);}
// Heitz visible-normal GGX sampling: sample only microfacets visible to the
// incoming view direction instead of generating rejected/back-facing lobes.
vec3 ggxVisibleHalf(vec3 n,vec3 v,float rough,inout uint s){float alpha=max(rough*rough,0.000001);vec3 t=tangent(n),b=cross(n,t);vec3 vl=safeNormalize(vec3(dot(v,t),dot(v,b),dot(v,n)),vec3(0,0,1));vec3 vh=safeNormalize(vec3(alpha*vl.xy,vl.z),vec3(0,0,1));float lensq=dot(vh.xy,vh.xy);vec3 t1=lensq>0.000001?vec3(-vh.y,vh.x,0.0)*inversesqrt(lensq):vec3(1,0,0);vec3 t2=cross(vh,t1);float r=sqrt(rnd(s)),phi=6.2831853*rnd(s);float p1=r*cos(phi),p2=r*sin(phi);float blend=0.5*(1.0+vh.z);p2=(1.0-blend)*sqrt(max(0.0,1.0-p1*p1))+blend*p2;vec3 nh=p1*t1+p2*t2+sqrt(max(0.0,1.0-p1*p1-p2*p2))*vh;vec3 hl=safeNormalize(vec3(alpha*nh.xy,max(0.0,nh.z)),vec3(0,0,1));return safeNormalize(t*hl.x+b*hl.y+n*hl.z,n);}
// Preserve the authored primary reflection/refraction.  Only deep glossy
// chains are regularized, where sub-pixel caustics dominate variance.
float regularizedRoughness(float rough,int bounce){return bounce>=2?max(rough,0.04):rough;}
vec3 environment(vec3 d,bool primary){if(primary&&(uUseEnvironment==0||uShowEnvironmentBackground==0))return uBackground;if(uUseEnvironment!=0&&uHasEnvironmentTexture!=0){float c=cos(uEnvironmentRotation),s=sin(uEnvironmentRotation);d.xz=mat2(c,-s,s,c)*d.xz;vec2 uv=vec2(atan(d.z,d.x)/6.2831853+0.5,0.5-asin(clamp(d.y,-1.0,1.0))/3.14159265);return textureLod(uEnvironmentMap,uv,primary?uEnvironmentBackgroundLod:0.0).rgb*max(uEnvironmentIntensity,0.0);}return primary?uBackground:vec3(0.05);}
float powerHeuristic(float a,float b){float aa=a*a,bb=b*b;return aa/max(aa+bb,0.000001);}
float environmentSolidAngle(int y){float height=float(uEnvironmentImportanceSize.y),width=float(uEnvironmentImportanceSize.x);float theta0=3.14159265*float(y)/height,theta1=3.14159265*float(y+1)/height;return max(6.2831853/width*(cos(theta0)-cos(theta1)),0.00000001);}
float environmentPdf(vec3 d){if(uHasEnvironmentImportance==0)return 0.0;float c=cos(uEnvironmentRotation),s=sin(uEnvironmentRotation);d.xz=mat2(c,-s,s,c)*d.xz;float u=atan(d.z,d.x)/6.2831853+0.5,v=0.5-asin(clamp(d.y,-1.0,1.0))/3.14159265;int x=clamp(int(u*float(uEnvironmentImportanceSize.x)),0,uEnvironmentImportanceSize.x-1),y=clamp(int(v*float(uEnvironmentImportanceSize.y)),0,uEnvironmentImportanceSize.y-1),index=y*uEnvironmentImportanceSize.x+x;return environmentBins[index].y/environmentSolidAngle(y);}
vec3 sampleEnvironmentImportance(inout uint seed,out float pdf){int count=uEnvironmentImportanceSize.x*uEnvironmentImportanceSize.y;float selector=rnd(seed);int lo=0,hi=count-1;while(lo<hi){int mid=(lo+hi)/2;if(environmentBins[mid].x<selector)lo=mid+1;else hi=mid;}int x=lo%uEnvironmentImportanceSize.x,y=lo/uEnvironmentImportanceSize.x;float u=(float(x)+rnd(seed))/float(uEnvironmentImportanceSize.x),v=(float(y)+rnd(seed))/float(uEnvironmentImportanceSize.y);float phi=(u-0.5)*6.2831853,latitude=(0.5-v)*3.14159265,radial=cos(latitude);vec3 mapDirection=vec3(cos(phi)*radial,sin(latitude),sin(phi)*radial);float c=cos(uEnvironmentRotation),s=sin(uEnvironmentRotation);vec3 result=vec3(mapDirection.x*c-mapDirection.z*s,mapDirection.y,mapDirection.x*s+mapDirection.z*c);pdf=environmentBins[lo].y/environmentSolidAngle(y);return normalize(result);}
vec3 fresnelSchlick(float cosTheta,vec3 f0){return f0+(1.0-f0)*pow(1.0-cosTheta,5.0);}
float dielectricFresnel(float cosTheta,float etaI,float etaT){etaI=max(etaI,1.0);etaT=max(etaT,1.0);float f0=pow((etaT-etaI)/(etaT+etaI),2.0);return f0+(1.0-f0)*pow(1.0-clamp(cosTheta,0.0,1.0),5.0);}
vec3 beerLambert(vec3 absorptionColor,float thickness,float distance){if(thickness<=0.0||distance<=0.0)return vec3(1.0);float opticalDistance=distance/thickness;return exp(log(clamp(absorptionColor,vec3(0.000001),vec3(1.0)))*opticalDistance);}
// Keep the GPU BRDF safeguards identical to OfflinePathTracer.  The previous
// 1e-4 floor suppressed narrow GGX highlights that the CPU renderer retained.
float distributionGGX(vec3 n,vec3 h,float rough){float a=rough*rough,a2=a*a,nh=max(dot(n,h),0.0),d=nh*nh*(a2-1.0)+1.0;return a2/max(3.14159265*d*d,0.000001);}
float geometrySchlick(float nv,float rough){float r=rough+1.0,k=r*r/8.0;return nv/max(nv*(1.0-k)+k,0.000001);}
float ggxVisibleReflectionPdf(vec3 n,vec3 v,vec3 l,float rough){float ndv=max(dot(n,v),0.0001);vec3 h=safeNormalize(v+l,n);if(dot(n,h)<=0.0||dot(v,h)<=0.0)return 0.0;return distributionGGX(n,h,rough)*geometrySchlick(ndv,rough)/max(4.0*ndv,0.0001);}
vec3 surfaceTransmission(vec3 baseColor,float transmission){return sqrt(clamp(baseColor,vec3(0.0),vec3(1.0)))*clamp(transmission,0.0,1.0);}
vec3 shadowTransmittance(vec3 ro,vec3 rd,float maxT){
    vec3 transmittance=vec3(1.0);
    float remaining=maxT;
    int mediumMaterials[8];float mediumIors[8];float mediumThicknesses[8];vec3 mediumAbsorptions[8];int mediumCount=0;
    for(int layerIndex=0;layerIndex<24;layerIndex++){
        if(layerIndex>=uMaxShadowLayers)break;
        int hitIndex;float hitDistance;vec2 hitUv;
        if(!trace(ro,rd,remaining,hitIndex,hitDistance,hitUv))return transmittance;
        if(mediumCount>0)transmittance*=beerLambert(mediumAbsorptions[mediumCount-1],mediumThicknesses[mediumCount-1],hitDistance);
        Tri blocker=tris[hitIndex];Shade blockerShade=loadShading(hitIndex);
        float rayEpsilon=rayOffset(ro+rd*hitDistance,blocker);
        int materialIndex=int(blocker.p0.w+0.5);Mat blockerMaterial=loadMaterial(materialIndex);
        float barycentricW=1.0-hitUv.x-hitUv.y;
        vec2 texUv=blockerShade.uv0.xy*barycentricW+blockerShade.uv1.xy*hitUv.x+blockerShade.uv2.xy*hitUv.y;
        float textureLayer=float(materialIndex);
        vec4 baseSample=texture(uBaseColorTextures,vec3(texUv,textureLayer));
        vec4 surfaceSample=texture(uSurfaceTextures,vec3(texUv,textureLayer));
        float opacity=clamp(blockerMaterial.surface.z*blockerMaterial.base.a*baseSample.a*surfaceSample.a,0.0,1.0);
        int mode=int(blockerMaterial.surface.w+0.5);
        if(mode==4){
            transmittance*=surfaceTransmission(blockerMaterial.base.rgb*baseSample.rgb,blockerMaterial.optics.x);
            if(uPhysicalGlass!=0){
                vec3 geometricNormal=normalize(cross(blocker.p1.xyz-blocker.p0.xyz,blocker.p2.xyz-blocker.p0.xyz));
                bool entering=dot(rd,geometricNormal)<0.0;
                if(entering){
                    int duplicateIndex=-1;for(int mi=7;mi>=0;mi--){if(mi<mediumCount&&mediumMaterials[mi]==materialIndex){duplicateIndex=mi;break;}}
                    if(duplicateIndex>=0){for(int mi=0;mi<7;mi++){if(mi>=duplicateIndex&&mi<mediumCount-1){mediumMaterials[mi]=mediumMaterials[mi+1];mediumIors[mi]=mediumIors[mi+1];mediumThicknesses[mi]=mediumThicknesses[mi+1];mediumAbsorptions[mi]=mediumAbsorptions[mi+1];}}mediumCount--;}
                    else if(mediumCount<8){mediumMaterials[mediumCount]=materialIndex;mediumIors[mediumCount]=max(blockerMaterial.optics.y,1.0001);mediumThicknesses[mediumCount]=max(blockerMaterial.optics.z,0.0);mediumAbsorptions[mediumCount]=clamp(blockerMaterial.absorption.rgb,vec3(0.0),vec3(1.0));mediumCount++;}
                }else{
                    int removeIndex=-1;for(int mi=7;mi>=0;mi--){if(mi<mediumCount&&mediumMaterials[mi]==materialIndex){removeIndex=mi;break;}}
                    if(removeIndex>=0){for(int mi=0;mi<7;mi++){if(mi>=removeIndex&&mi<mediumCount-1){mediumMaterials[mi]=mediumMaterials[mi+1];mediumIors[mi]=mediumIors[mi+1];mediumThicknesses[mi]=mediumThicknesses[mi+1];mediumAbsorptions[mi]=mediumAbsorptions[mi+1];}}mediumCount--;}
                }
            }
        }else if((mode==0||mode==3)&&opacity<0.999){
            transmittance*=vec3(1.0-opacity);
        }else if(mode!=2||opacity>=blockerMaterial.optics.w){
            return vec3(0.0);
        }
        if(max(transmittance.r,max(transmittance.g,transmittance.b))<0.005)return vec3(0.0);
        float advance=hitDistance+rayEpsilon;
        if(remaining<1e29){
            remaining-=advance;
            if(remaining<=rayEpsilon)return transmittance;
        }
        ro+=rd*advance;
    }
    return transmittance;
}
vec3 directLight(vec3 p,vec3 n,vec3 v,vec3 baseColor,float metallic,float rough,float ao,bool dielectricOnly,float boundaryF0,float rayEpsilon,inout uint seed){
    vec3 sum=vec3(0);
    vec3 f0=dielectricOnly?vec3(boundaryF0):mix(vec3(0.04),baseColor,metallic);
    for(int i=0;i<uLightCount;i++){
        Light l=lights[i];
        int type=int(l.positionType.w+0.5);
        vec3 ld;
        float attenuation=1.0,maxT=1e30;
        if(type==0){
            ld=normalize(-l.directionRange.xyz);
        }else{
            vec3 off=l.positionType.xyz-p;
            maxT=length(off);if(!finiteFloat(maxT)||maxT<=rayEpsilon)continue;
            ld=off/maxT;
            float range=l.directionRange.w;if(range<=0.0)continue;float normalizedDistance=maxT/range;
            float rangeCutoff=1.0-smoothstep(0.85,1.0,normalizedDistance);
            float distanceFalloff=1.0/(1.0+4.0*normalizedDistance*normalizedDistance);
            attenuation=rangeCutoff*distanceFalloff;
            if(type==2){
                float coneCosine=dot(-ld,normalize(l.directionRange.xyz));
                attenuation*=smoothstep(l.cone.y,l.cone.x,coneCosine);
            }
        }
        float ndl=max(dot(n,ld),0.0);
        if(ndl<=0.0||attenuation<=0.0)continue;
        vec3 transmittance=shadowTransmittance(p+n*rayEpsilon,ld,maxT-rayEpsilon);
        if(max(transmittance.r,max(transmittance.g,transmittance.b))<=0.000001)continue;
        vec3 h=safeNormalize(v+ld,n),f=fresnelSchlick(max(dot(h,v),0.0),f0);
        float ndf=distributionGGX(n,h,rough);
        float g=geometrySchlick(max(dot(n,v),0.0),rough)*geometrySchlick(ndl,rough);
        vec3 spec=ndf*g*f/max(4.0*max(dot(n,v),0.0)*ndl,0.001);
        vec3 kd=dielectricOnly?vec3(0.0):(1.0-f)*(1.0-metallic);
        sum+=(kd*baseColor/3.14159265+spec)*l.colorIntensity.rgb*transmittance*(l.colorIntensity.w*attenuation*ndl);
    }
    // One MIS-weighted HDR next-event sample keeps the estimator unbiased while
    // avoiding a second expensive transparent shadow traversal at every bounce.
    if(uUseEnvironment!=0&&uHasEnvironmentImportance!=0)for(int environmentSample=0;environmentSample<uEnvironmentSampleCount;environmentSample++){
        float envPdf;vec3 ld=sampleEnvironmentImportance(seed,envPdf);float ndl=max(dot(n,ld),0.0);
        if(ndl>0.0&&envPdf>0.0){
            vec3 transmittance=shadowTransmittance(p+n*rayEpsilon,ld,1e30);
            if(max(transmittance.r,max(transmittance.g,transmittance.b))>0.000001){
                vec3 h=safeNormalize(v+ld,n),f=fresnelSchlick(max(dot(h,v),0.0),f0);
                float ndf=distributionGGX(n,h,rough),g=geometrySchlick(max(dot(n,v),0.0),rough)*geometrySchlick(ndl,rough);
                vec3 spec=ndf*g*f/max(4.0*max(dot(n,v),0.0)*ndl,0.001),kd=dielectricOnly?vec3(0.0):(1.0-f)*(1.0-metallic);
                float specularProbability=dielectricOnly?1.0:0.15+metallic*0.75;
                float bsdfPdf=specularProbability*ggxVisibleReflectionPdf(n,v,ld,rough)+(1.0-specularProbability)*ndl/3.14159265;
                float environmentSampleWeight=1.0/max(float(uEnvironmentSampleCount),1.0);
                sum+=(kd*baseColor/3.14159265+spec)*environment(ld,false)*transmittance*(environmentSampleWeight*ao*ndl*powerHeuristic(envPdf,bsdfPdf)/envPdf);
            }
        }
    }
    return sum;
}
vec3 samplePath(vec3 ro,vec3 rd,inout uint seed,out float alpha,out vec4 primaryNormalDepth,out vec4 primaryAlbedo,out float primaryGlassMask,out float invalidPath,out float fireflyClamped){
    vec3 radiance=vec3(0),throughput=vec3(1);
    float previousBsdfPdf=0.0;bool previousWasDelta=true;
    int mediumMaterials[8];float mediumIors[8];float mediumThicknesses[8];vec3 mediumAbsorptions[8];int mediumCount=0;
    alpha=1.0;
    primaryNormalDepth=vec4(0.0);
    primaryAlbedo=vec4(0.0);
    primaryGlassMask=0.0;
    invalidPath=0.0;
    fireflyClamped=0.0;
    float pathDepth=0.0;bool primarySurfaceResolved=false;
    for(int bounce=0;bounce<uMaxBounces;bounce++){
        if(!finiteVec3(rd)||dot(rd,rd)<1e-12||!finiteVec3(throughput)||!finiteVec3(radiance)){invalidPath=1.0;radiance=sanitizeVec3(radiance);break;}
        int hi;float ht;vec2 uv;
        if(!trace(ro,rd,1e30,hi,ht,uv)){
            float misWeight=(bounce>0&&!previousWasDelta)?powerHeuristic(previousBsdfPdf,environmentPdf(rd)):1.0;
            radiance+=limitPathContribution(throughput*environment(rd,bounce==0)*misWeight,fireflyClamped);
            if(bounce==0&&uTransparent!=0)alpha=0.0;
            break;
        }
        Tri t=tris[hi];Shade shade=loadShading(hi);
        int materialIndex=int(t.p0.w+0.5);Mat material=loadMaterial(materialIndex);
        pathDepth+=ht;
        if(mediumCount>0)throughput*=beerLambert(mediumAbsorptions[mediumCount-1],mediumThicknesses[mediumCount-1],ht);
        if(max(throughput.r,max(throughput.g,throughput.b))<0.000001)break;
        float w=1.0-uv.x-uv.y;
        vec3 geometricNormal=safeNormalize(cross(t.p1.xyz-t.p0.xyz,t.p2.xyz-t.p0.xyz),vec3(0,1,0));
        vec3 n=safeNormalize(shade.n0.xyz*w+shade.n1.xyz*uv.x+shade.n2.xyz*uv.y,geometricNormal);
        vec2 texUv=shade.uv0.xy*w+shade.uv1.xy*uv.x+shade.uv2.xy*uv.y;
        float layer=float(materialIndex);
        vec4 baseSample=texture(uBaseColorTextures,vec3(texUv,layer));
        vec4 surfaceSample=texture(uSurfaceTextures,vec3(texUv,layer));
        vec3 normalSample=texture(uNormalTextures,vec3(texUv,layer)).xyz*2.0-1.0;
        normalSample.xy*=material.textureInfo.x;
        vec3 interpolatedTangent=shade.t0.xyz*w+shade.t1.xyz*uv.x+shade.t2.xyz*uv.y;
        vec3 tn=safeNormalize(interpolatedTangent-n*dot(interpolatedTangent,n),tangent(n)),bn=safeNormalize(cross(n,tn),cross(n,tangent(n)));
        n=safeNormalize(mat3(tn,bn,n)*normalSample,n);
        vec3 baseColor=material.base.rgb*baseSample.rgb;
        float metallic=clamp(material.surface.x*surfaceSample.r,0.0,1.0);
        float rough=clamp(material.surface.y*surfaceSample.g,0.001,1.0);
        rough=regularizedRoughness(rough,bounce);
        float ao=clamp(material.emission.a*surfaceSample.b,0.0,1.0);
        vec3 p=ro+rd*ht;
        float rayEpsilon=rayOffset(p,t);
        bool frontFace=dot(rd,geometricNormal)<0.0;
        if(dot(n,geometricNormal)<0.0)n=-n;
        if(!frontFace)n=-n;
        int mode=int(material.surface.w+0.5);
        float opacity=clamp(material.surface.z*material.base.a*baseSample.a*surfaceSample.a,0.0,1.0);
        // This AOV protects all transmissive first-hit layers, not only the
        // explicit Glass mode. Transparent and Auto-alpha panels also need a
        // full sampling budget and the denoiser's narrow edge-aware filter.
        bool transmissiveLayer=mode==4||((mode==0||mode==3)&&opacity<0.999);
        if(!primarySurfaceResolved&&transmissiveLayer)primaryGlassMask=1.0;
        if(mode==2&&opacity<material.optics.w){ro=p+rd*rayEpsilon;bounce--;continue;}
        if((mode==0||mode==3)&&opacity<0.999&&rnd(seed)>opacity){ro=p+rd*rayEpsilon;bounce--;continue;}
        if(!primarySurfaceResolved){if(mode==4){primaryGlassMask=1.0;}else{primaryGlassMask=max(primaryGlassMask,(metallic>=0.75||rough<=0.08)?2.0:0.0);primaryNormalDepth=vec4(n,pathDepth);primaryAlbedo=vec4(baseColor,rough);primarySurfaceResolved=true;}}
        vec3 emissiveSample=texture(uEmissiveTextures,vec3(texUv,layer)).rgb;
        radiance+=limitPathContribution(throughput*material.emission.rgb*emissiveSample,fireflyClamped);
        if(mode==4){
            if(uPhysicalGlass==0){throughput*=surfaceTransmission(baseColor,material.optics.x);ro=p+rd*rayEpsilon;bounce--;continue;}
            float incidentIor=mediumCount>0?mediumIors[mediumCount-1]:1.0;
            float transmittedIor=max(material.optics.y,1.0001);
            int exitIndex=-1;
            if(!frontFace){for(int mi=7;mi>=0;mi--){if(mi<mediumCount&&mediumMaterials[mi]==materialIndex){exitIndex=mi;break;}}if(exitIndex<0)incidentIor=max(material.optics.y,1.0001);transmittedIor=exitIndex>0?mediumIors[exitIndex-1]:1.0;}
            float boundaryF0=dielectricFresnel(1.0,incidentIor,transmittedIor);
            bool deltaGlass=rough<=0.01;
            if(!deltaGlass)radiance+=limitPathContribution(throughput*directLight(p,n,-rd,baseColor,metallic,rough,ao,true,boundaryF0,rayEpsilon,seed),fireflyClamped);
            vec3 microfacetNormal=deltaGlass?n:ggxHalf(n,rough,seed);
            if(dot(-rd,microfacetNormal)<0.0)microfacetNormal=-microfacetNormal;
            float eta=incidentIor/transmittedIor;
            float cosi=clamp(dot(-rd,microfacetNormal),0.0,1.0);
            float fr=dielectricFresnel(cosi,incidentIor,transmittedIor);
            // Match OfflinePathTracer's explicit total-internal-reflection test.
            // Testing refract()'s zero vector made GPU and CPU choose different
            // paths near the critical angle on some drivers.
            bool cannotRefract=eta*sqrt(max(0.0,1.0-cosi*cosi))>1.0;
            vec3 refr=refract(rd,microfacetNormal,eta);
            bool reflected=cannotRefract||rnd(seed)<fr;
            rd=safeNormalize(reflected?reflect(rd,microfacetNormal):refr,reflect(rd,n));
            if(!reflected){
                throughput*=surfaceTransmission(baseColor,material.optics.x);
                if(frontFace){int duplicateIndex=-1;for(int mi=7;mi>=0;mi--){if(mi<mediumCount&&mediumMaterials[mi]==materialIndex){duplicateIndex=mi;break;}}if(duplicateIndex>=0){for(int mi=0;mi<7;mi++){if(mi>=duplicateIndex&&mi<mediumCount-1){mediumMaterials[mi]=mediumMaterials[mi+1];mediumIors[mi]=mediumIors[mi+1];mediumThicknesses[mi]=mediumThicknesses[mi+1];mediumAbsorptions[mi]=mediumAbsorptions[mi+1];}}mediumCount--;}else if(mediumCount<8){mediumMaterials[mediumCount]=materialIndex;mediumIors[mediumCount]=max(material.optics.y,1.0001);mediumThicknesses[mediumCount]=max(material.optics.z,0.0);mediumAbsorptions[mediumCount]=clamp(material.absorption.rgb,vec3(0.0),vec3(1.0));mediumCount++;}}
                else if(exitIndex>=0){for(int mi=0;mi<7;mi++){if(mi>=exitIndex&&mi<mediumCount-1){mediumMaterials[mi]=mediumMaterials[mi+1];mediumIors[mi]=mediumIors[mi+1];mediumThicknesses[mi]=mediumThicknesses[mi+1];mediumAbsorptions[mi]=mediumAbsorptions[mi+1];}}mediumCount--;}
            }
            ro=p+rd*rayEpsilon;
            previousWasDelta=true;previousBsdfPdf=0.0;
        }else{
            // A smooth dielectric still has a diffuse transmission/reflection
            // component.  Restrict the delta shortcut to near-perfect metals;
            // otherwise coloured low-roughness plastics turn into black mirrors.
            bool deltaSpecular=rough<=0.01&&metallic>=0.95;
            if(!deltaSpecular)radiance+=limitPathContribution(throughput*directLight(p,n,-rd,baseColor,metallic,rough,ao,false,0.04,rayEpsilon,seed),fireflyClamped);
            float specularProbability=0.15+metallic*0.75;
            float diffuseProbability=max(1.0-specularProbability,0.05);
            vec3 view=-rd;
            if(deltaSpecular){
                vec3 f0=mix(vec3(0.04),baseColor,metallic);
                throughput*=fresnelSchlick(max(dot(n,view),0.0),f0);
                rd=safeNormalize(reflect(rd,n),n);
                previousWasDelta=true;previousBsdfPdf=0.0;
                ro=p+n*rayEpsilon;
            }else if(rnd(seed)<specularProbability){
                vec3 halfVector=ggxVisibleHalf(n,view,rough,seed);
                vec3 nextDirection=reflect(rd,halfVector);
                float ndv=max(dot(n,view),0.0001);
                float ndl=max(dot(n,nextDirection),0.0);
                float ndh=max(dot(n,halfVector),0.0001);
                float vdh=max(dot(view,halfVector),0.0001);
                if(ndl<=0.0){
                    nextDirection=reflect(rd,n);
                    ndl=max(dot(n,nextDirection),0.0001);
                    ndh=1.0;
                    vdh=ndv;
                }
                vec3 f0=mix(vec3(0.04),baseColor,metallic);
                vec3 fresnel=fresnelSchlick(vdh,f0);
                float geometryLight=geometrySchlick(ndl,rough);
                throughput*=fresnel*(geometryLight/specularProbability);
                rd=safeNormalize(nextDirection,n);
            }else{
                rd=diffuseDir(n,seed);
                vec3 halfVector=safeNormalize(view+rd,n);
                vec3 diffuseFresnel=fresnelSchlick(max(dot(view,halfVector),0.0),mix(vec3(0.04),baseColor,metallic));
                throughput*=(1.0-diffuseFresnel)*baseColor*(1.0-metallic)*ao/diffuseProbability;
            }
            if(!deltaSpecular){
                previousBsdfPdf=specularProbability*ggxVisibleReflectionPdf(n,view,rd,rough)+diffuseProbability*max(dot(n,rd),0.0)/3.14159265;
                previousWasDelta=false;
                ro=p+n*rayEpsilon;
            }
        }
        if(bounce>=2){
            float survive=clamp(max(throughput.r,max(throughput.g,throughput.b)),0.08,0.95);
            if(rnd(seed)>survive)break;
            throughput/=survive;
        }
    }
    if(!finiteVec3(radiance)){invalidPath=1.0;radiance=sanitizeVec3(radiance);}
    return radiance;
}
void main(){
    ivec2 px=ivec2(gl_GlobalInvocationID.xy)+uDispatchOffset;if(any(greaterThanEqual(px,uSize)))return;
    int index=px.y*uSize.x+px.x;
    for(int sampleOffset=0;sampleOffset<uSamplesThisDispatch;sampleOffset++){
        int sampleIndex=uSampleStart+sampleOffset;
        uint previousCount=sampleCounts[index];
        vec4 previousGlassMask=previousCount==0u?vec4(0.0):glassMaskPixels[index];
        // After a 32-sample warm-up, reserve the remaining budget for noisy,
        // reflective and refractive pixels. Glass always keeps its full budget.
        if(uAdaptiveSampling!=0&&sampleIndex>=uAdaptiveWarmupSamples&&previousCount>=uint(uAdaptiveWarmupSamples)&&previousGlassMask.x<0.5){
            float priorMean=max(previousGlassMask.y,0.0);
            float priorVariance=max(previousGlassMask.z-priorMean*priorMean,0.0);
            float standardError=sqrt(priorVariance/max(float(previousCount),1.0));
            float convergenceLimit=max(0.003,0.015*max(priorMean,0.1));
            if(standardError<=convergenceLimit)return;
        }
        uint seed=uint(index*1973+sampleIndex*9277+26699)|1u;vec2 jitter=vec2(rnd(seed),rnd(seed));
        vec2 uv=(vec2(px)+jitter)/vec2(uSize);float x=(2.0*uv.x-1.0)*uAspect*uHalfHeight,y=(1.0-2.0*uv.y)*uHalfHeight;
        vec3 rd=safeNormalize(uForward+uRight*x+uUp*y,uForward);float a,glass,invalidPath,pathFireflyClamped;vec4 nd,alb;
        vec3 c=samplePath(uCameraFrom,rd,seed,a,nd,alb,glass,invalidPath,pathFireflyClamped);c=sanitizeVec3(c);
        float luminance=max(dot(c,vec3(0.2126,0.7152,0.0722)),0.0);float sampleCount=float(previousCount);
        vec4 previousColor=previousCount==0u?vec4(0.0):pixels[index]*sampleCount;
        vec4 previousNormalDepth=previousCount==0u?vec4(0.0):normalDepthPixels[index]*sampleCount;
        vec4 previousAlbedo=previousCount==0u?vec4(0.0):albedoPixels[index]*sampleCount;
        previousGlassMask=previousCount==0u?vec4(0.0):previousGlassMask*sampleCount;
        float temporalFireflyClamped=0.0;
        if(previousCount>=8u){float priorMean=max(previousGlassMask.y/max(sampleCount,1.0),0.0),priorVariance=max(previousGlassMask.z/max(sampleCount,1.0)-priorMean*priorMean,0.0);float adaptiveLimit=max(8.0,priorMean+5.0*sqrt(priorVariance+0.0025));if(luminance>adaptiveLimit){c*=adaptiveLimit/max(luminance,0.000001);luminance=adaptiveLimit;temporalFireflyClamped=1.0;}}
        previousColor=vec4(sanitizeVec3(previousColor.rgb),finiteFloat(previousColor.a)?previousColor.a:0.0);
        pixels[index]=(previousColor+vec4(c,a))/(sampleCount+1.0);normalDepthPixels[index]=(previousNormalDepth+nd)/(sampleCount+1.0);
        albedoPixels[index]=(previousAlbedo+alb)/(sampleCount+1.0);glassMaskPixels[index]=(previousGlassMask+vec4(glass,luminance,luminance*luminance,invalidPath-max(pathFireflyClamped,temporalFireflyClamped)))/(sampleCount+1.0);sampleCounts[index]=previousCount+1u;
    }
}
""";
#endif
}
