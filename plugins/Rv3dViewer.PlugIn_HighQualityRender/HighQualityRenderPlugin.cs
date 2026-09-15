using Rv3dViewer.Plugin.Abstractions;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.HighQualityRenderPlugin;

public sealed class HighQualityRenderPlugin : IRv3dPlugin
{
    private IPluginContext? _context;
    private HighQualityRenderForm? _form;

    public void Initialize(IPluginContext context) => _context = context;

    public IReadOnlyCollection<PluginCommand> GetCommands() =>
    [
        new("render-high-quality", "高品質 Render…", RenderAsync, 10)
    ];

    public void Shutdown()
    {
        if (_form is { IsDisposed: false }) _form.Close();
        _form = null;
        _context = null;
    }

    private Task RenderAsync(CancellationToken cancellationToken)
    {
        var context = _context ?? throw new InvalidOperationException("外掛尚未初始化。");
        if (_form is { IsDisposed: false })
        {
            _form.Activate();
            return Task.CompletedTask;
        }

        var form = new HighQualityRenderForm(context);
        ApplyHostPreviewMode(form, context);
        _form = form;
        EventHandler previewModeChanged = (_, _) => ApplyHostPreviewMode(form, context);
        context.PreviewModeChanged += previewModeChanged;
        form.RenderCompleted += (_, path) => context.SetStatus($"高品質 Render 已輸出：{path}");
        try
        {
            // Keep the host viewport from receiving user interaction while this
            // GPU-intensive plug-in is active. ShowDialog retains its own message
            // loop, so progress, cancellation and window repaint remain responsive.
            form.ShowDialog();
        }
        finally
        {
            context.PreviewModeChanged -= previewModeChanged;
            if (ReferenceEquals(_form, form)) _form = null;
            form.Dispose();
        }
        return Task.CompletedTask;
    }

    private static void ApplyHostPreviewMode(HighQualityRenderForm form, IPluginContext context)
    {
        if (context is IPluginPreviewModeContext previewContext &&
            Enum.IsDefined(typeof(ViewportPreviewMode), previewContext.PreviewMode))
        {
            form.SetPreviewMode((ViewportPreviewMode)previewContext.PreviewMode);
            return;
        }

        form.SetQuickPreviewEnabled(context.QuickPreviewEnabled);
    }
}
