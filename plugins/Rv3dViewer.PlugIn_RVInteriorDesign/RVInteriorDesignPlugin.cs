using Rv3dViewer.Plugin.Abstractions;
using System.Runtime.InteropServices;

namespace Rv3dViewer.RVInteriorDesignPlugin;

public sealed class RVInteriorDesignPlugin : IRv3dPlugin
{
    private IPluginContext? _context;

    public void Initialize(IPluginContext context) => _context = context;

    public IReadOnlyCollection<PluginCommand> GetCommands() =>
    [
        new("open-rv-interior-designer", "RV室內設計…", OpenDesignerAsync, 31)
    ];

    public void Shutdown() => _context = null;

    private Task OpenDesignerAsync(CancellationToken cancellationToken)
    {
        var context = _context ?? throw new InvalidOperationException("外掛尚未初始化。");
        var form = new RVInteriorDesignForm(context);
        try
        {
            var owner = Form.ActiveForm;
            if (owner is null)
                form.ShowDialog();
            else
                form.ShowDialog(owner);
        }
        finally
        {
            try
            {
                form.Dispose();
            }
            catch (ExternalException exception)
            {
                // The modal form has already closed. A late GDI+ cleanup failure must not be
                // reported as a failed Plugin command, but retain its stack for diagnosis.
                WriteCleanupDiagnostic(exception);
            }
        }
        context.SetStatus("RV室內設計工作區已關閉");
        return Task.CompletedTask;
    }

    private static void WriteCleanupDiagnostic(Exception exception)
    {
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Rv3dViewer", "RVInteriorDesign", "Logs");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "cleanup-errors.log"),
                $"[{DateTimeOffset.Now:O}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception logException) when (logException is IOException or UnauthorizedAccessException)
        {
            // Logging is diagnostic only and must never make closing the Plugin fail.
        }
    }
}
