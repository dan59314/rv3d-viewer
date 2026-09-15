using System.Windows.Forms;
using Rv3dViewer.HighQualityRenderPlugin;
using Xunit;

namespace Rv3dViewer.Tests;

[CollectionDefinition("Render GPU", DisableParallelization = true)]
public sealed class GpuRenderTestCollection;

internal static class GpuRenderTestThread
{
    // Match the plugin's message-pumped UI thread so creation and disposal of
    // WGL windows stay on the same thread, including after asynchronous renders.
    internal static Task<GpuRenderResult> RenderAsync(RenderSceneSnapshot scene, RenderOptions options)
    {
        var completion = new TaskCompletionSource<GpuRenderResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            GpuRenderResult? result = null;
            Exception? failure = null;
            using var context = new ApplicationContext();
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            EventHandler? start = null;
            start = async (_, _) =>
            {
                Application.Idle -= start;
                var guard = OpenTK.Windowing.Desktop.GLFWProvider.CheckForMainThread;
                OpenTK.Windowing.Desktop.GLFWProvider.CheckForMainThread = false;
                try
                {
                    result = await GpuPathTracer.TryRenderToPngAsync(scene, options, null, CancellationToken.None);
                }
                catch (Exception ex) { failure = ex; }
                finally
                {
                    OpenTK.Windowing.Desktop.GLFWProvider.CheckForMainThread = guard;
                    context.ExitThread();
                }
            };
            Application.Idle += start;
            Application.Run(context);
            if (failure is not null) completion.TrySetException(failure);
            else completion.TrySetResult(result!);
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
