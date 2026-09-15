namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Reflection;
using System.Runtime.ExceptionServices;
using Rv3dViewer.Core;

internal static class InteriorGlbExportService
{
    internal static async Task ExportAsync(ViewerProject project, string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var appAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly =>
            string.Equals(assembly.GetName().Name, "Rv3dViewer.App", StringComparison.OrdinalIgnoreCase));
        var exporterType = appAssembly?.GetType("Rv3dViewer.App.GlbExporter", throwOnError: false);
        var exportMethod = exporterType?.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Static,
            binder: null, [typeof(ViewerProject), typeof(string), typeof(CancellationToken)], modifiers: null);
        if (exportMethod is null)
            throw new NotSupportedException("目前的 MainForm 版本沒有提供 GLB 匯出服務。");
        try
        {
            if (exportMethod.Invoke(null, [project, filePath, cancellationToken]) is not Task task)
                throw new InvalidOperationException("MainForm GLB 匯出服務沒有回傳有效工作。");
            await task.ConfigureAwait(true);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
