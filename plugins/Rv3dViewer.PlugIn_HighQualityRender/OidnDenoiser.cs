using System.Runtime.InteropServices;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal static class OidnDenoiser
{
    private const int DeviceTypeDefault = 0;
    private const int FormatFloat3 = 3;

    internal static IReadOnlyList<string> LibraryCandidatesForTest => GetLibraryCandidates().ToArray();

    internal static bool TryDenoise(float[] color, float[] normalDepth, float[] albedo, int width, int height,
        out float[] denoised, out string backend)
    {
        denoised = [];
        backend = "內建 AOV denoiser";
        if (!OperatingSystem.IsWindows() || color.Length != checked(width * height * 4) ||
            normalDepth.Length != color.Length || albedo.Length != color.Length)
            return false;

        IntPtr library = IntPtr.Zero;
        NativeApi? api = null;
        IntPtr device = IntPtr.Zero;
        IntPtr colorBuffer = IntPtr.Zero;
        IntPtr albedoBuffer = IntPtr.Zero;
        IntPtr normalBuffer = IntPtr.Zero;
        IntPtr outputBuffer = IntPtr.Zero;
        IntPtr filter = IntPtr.Zero;
        try
        {
            if (!TryLoadLibrary(out library)) return false;
            api = NativeApi.Load(library);
            device = api.NewDevice(DeviceTypeDefault);
            if (device == IntPtr.Zero) return false;
            api.CommitDevice(device);
            ThrowIfError(api, device);

            var pixelCount = checked(width * height);
            var input = ExtractRgb(color, pixelCount, preserveNormal: false);
            var featureAlbedo = ExtractRgb(albedo, pixelCount, preserveNormal: false);
            var featureNormal = ExtractRgb(normalDepth, pixelCount, preserveNormal: true);
            var output = new float[input.Length];
            var byteSize = checked((nuint)(input.Length * sizeof(float)));
            colorBuffer = api.NewBuffer(device, byteSize);
            albedoBuffer = api.NewBuffer(device, byteSize);
            normalBuffer = api.NewBuffer(device, byteSize);
            outputBuffer = api.NewBuffer(device, byteSize);
            if (colorBuffer == IntPtr.Zero || albedoBuffer == IntPtr.Zero || normalBuffer == IntPtr.Zero || outputBuffer == IntPtr.Zero)
                return false;
            Write(api, colorBuffer, input);
            Write(api, albedoBuffer, featureAlbedo);
            Write(api, normalBuffer, featureNormal);

            filter = api.NewFilter(device, "RT");
            if (filter == IntPtr.Zero) return false;
            SetImage(api, filter, "color", colorBuffer, width, height);
            SetImage(api, filter, "albedo", albedoBuffer, width, height);
            SetImage(api, filter, "normal", normalBuffer, width, height);
            SetImage(api, filter, "output", outputBuffer, width, height);
            api.SetFilterBool(filter, "hdr", true);
            api.SetFilterBool(filter, "cleanAux", false);
            api.SetFilterInt(filter, "quality", 6); // OIDN_QUALITY_HIGH
            api.CommitFilter(filter);
            api.ExecuteFilter(filter);
            ThrowIfError(api, device);
            Read(api, outputBuffer, output);
            if (output.Any(value => !float.IsFinite(value))) return false;

            denoised = ExpandRgb(output, color);
            backend = "OIDN RT (自動 CPU/GPU)";
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            if (api is not null)
            {
                if (filter != IntPtr.Zero) api.ReleaseFilter(filter);
                if (outputBuffer != IntPtr.Zero) api.ReleaseBuffer(outputBuffer);
                if (normalBuffer != IntPtr.Zero) api.ReleaseBuffer(normalBuffer);
                if (albedoBuffer != IntPtr.Zero) api.ReleaseBuffer(albedoBuffer);
                if (colorBuffer != IntPtr.Zero) api.ReleaseBuffer(colorBuffer);
                if (device != IntPtr.Zero) api.ReleaseDevice(device);
            }
            if (library != IntPtr.Zero) NativeLibrary.Free(library);
        }
    }

    private static IEnumerable<string> GetLibraryCandidates()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "OpenImageDenoise.dll");
        var pluginDirectory = Path.GetDirectoryName(typeof(OidnDenoiser).Assembly.Location) ?? string.Empty;
        yield return Path.Combine(pluginDirectory, "OpenImageDenoise.dll");
        yield return Path.Combine(pluginDirectory, "native", "oidn", "OpenImageDenoise.dll");
        yield return "OpenImageDenoise.dll";
    }

    private static bool TryLoadLibrary(out IntPtr library)
    {
        foreach (var candidate in GetLibraryCandidates())
            if (NativeLibrary.TryLoad(candidate, out library)) return true;
        library = IntPtr.Zero;
        return false;
    }

    private static float[] ExtractRgb(float[] source, int pixelCount, bool preserveNormal)
    {
        var result = new float[checked(pixelCount * 3)];
        for (var pixel = 0; pixel < pixelCount; pixel++)
        {
            var sourceOffset = pixel * 4;
            var destinationOffset = pixel * 3;
            var x = source[sourceOffset];
            var y = source[sourceOffset + 1];
            var z = source[sourceOffset + 2];
            if (preserveNormal)
            {
                var length = MathF.Sqrt(x * x + y * y + z * z);
                if (!float.IsFinite(length) || length <= 0.0001F) x = y = z = 0F;
                else { x /= length; y /= length; z /= length; }
            }
            result[destinationOffset] = float.IsFinite(x) ? x : 0F;
            result[destinationOffset + 1] = float.IsFinite(y) ? y : 0F;
            result[destinationOffset + 2] = float.IsFinite(z) ? z : 0F;
        }
        return result;
    }

    private static float[] ExpandRgb(float[] rgb, float[] source)
    {
        var result = new float[source.Length];
        for (var pixel = 0; pixel < rgb.Length / 3; pixel++)
        {
            var sourceOffset = pixel * 4;
            var rgbOffset = pixel * 3;
            result[sourceOffset] = rgb[rgbOffset];
            result[sourceOffset + 1] = rgb[rgbOffset + 1];
            result[sourceOffset + 2] = rgb[rgbOffset + 2];
            result[sourceOffset + 3] = source[sourceOffset + 3];
        }
        return result;
    }

    private static void SetImage(NativeApi api, IntPtr filter, string name, IntPtr buffer, int width, int height) =>
        api.SetFilterImage(filter, name, buffer, FormatFloat3, (nuint)width, (nuint)height, 0, 0, 0);

    private static void Write(NativeApi api, IntPtr buffer, float[] values)
    {
        var handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try { api.WriteBuffer(buffer, 0, checked((nuint)(values.Length * sizeof(float))), handle.AddrOfPinnedObject()); }
        finally { handle.Free(); }
    }

    private static void Read(NativeApi api, IntPtr buffer, float[] values)
    {
        var handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try { api.ReadBuffer(buffer, 0, checked((nuint)(values.Length * sizeof(float))), handle.AddrOfPinnedObject()); }
        finally { handle.Free(); }
    }

    private static void ThrowIfError(NativeApi api, IntPtr device)
    {
        var error = api.GetDeviceError(device, out var message);
        if (error != 0) throw new InvalidOperationException(Marshal.PtrToStringUTF8(message) ?? $"OIDN error {error}");
    }

    private sealed class NativeApi
    {
        private NativeApi(IntPtr library)
        {
            NewDevice = Load<NewDeviceDelegate>(library, "oidnNewDevice");
            CommitDevice = Load<CommitDeviceDelegate>(library, "oidnCommitDevice");
            GetDeviceError = Load<GetDeviceErrorDelegate>(library, "oidnGetDeviceError");
            ReleaseDevice = Load<ReleaseDelegate>(library, "oidnReleaseDevice");
            NewBuffer = Load<NewBufferDelegate>(library, "oidnNewBuffer");
            WriteBuffer = Load<TransferBufferDelegate>(library, "oidnWriteBuffer");
            ReadBuffer = Load<TransferBufferDelegate>(library, "oidnReadBuffer");
            ReleaseBuffer = Load<ReleaseDelegate>(library, "oidnReleaseBuffer");
            NewFilter = Load<NewFilterDelegate>(library, "oidnNewFilter");
            SetFilterImage = Load<SetFilterImageDelegate>(library, "oidnSetFilterImage");
            SetFilterBool = Load<SetFilterBoolDelegate>(library, "oidnSetFilterBool");
            SetFilterInt = Load<SetFilterIntDelegate>(library, "oidnSetFilterInt");
            CommitFilter = Load<CommitFilterDelegate>(library, "oidnCommitFilter");
            ExecuteFilter = Load<ExecuteFilterDelegate>(library, "oidnExecuteFilter");
            ReleaseFilter = Load<ReleaseDelegate>(library, "oidnReleaseFilter");
        }

        internal NewDeviceDelegate NewDevice { get; }
        internal CommitDeviceDelegate CommitDevice { get; }
        internal GetDeviceErrorDelegate GetDeviceError { get; }
        internal ReleaseDelegate ReleaseDevice { get; }
        internal NewBufferDelegate NewBuffer { get; }
        internal TransferBufferDelegate WriteBuffer { get; }
        internal TransferBufferDelegate ReadBuffer { get; }
        internal ReleaseDelegate ReleaseBuffer { get; }
        internal NewFilterDelegate NewFilter { get; }
        internal SetFilterImageDelegate SetFilterImage { get; }
        internal SetFilterBoolDelegate SetFilterBool { get; }
        internal SetFilterIntDelegate SetFilterInt { get; }
        internal CommitFilterDelegate CommitFilter { get; }
        internal ExecuteFilterDelegate ExecuteFilter { get; }
        internal ReleaseDelegate ReleaseFilter { get; }

        internal static NativeApi Load(IntPtr library) => new(library);
        private static T Load<T>(IntPtr library, string name) where T : Delegate =>
            Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate IntPtr NewDeviceDelegate(int type);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void CommitDeviceDelegate(IntPtr device);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int GetDeviceErrorDelegate(IntPtr device, out IntPtr message);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void ReleaseDelegate(IntPtr handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate IntPtr NewBufferDelegate(IntPtr device, nuint byteSize);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void TransferBufferDelegate(IntPtr buffer, nuint offset, nuint byteSize, IntPtr memory);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate IntPtr NewFilterDelegate(IntPtr device, [MarshalAs(UnmanagedType.LPUTF8Str)] string type);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void SetFilterImageDelegate(IntPtr filter,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name, IntPtr buffer, int format, nuint width, nuint height,
            nuint offset, nuint pixelStride, nuint rowStride);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void SetFilterBoolDelegate(IntPtr filter,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.I1)] bool value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void SetFilterIntDelegate(IntPtr filter,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void CommitFilterDelegate(IntPtr filter);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void ExecuteFilterDelegate(IntPtr filter);
    }
}
