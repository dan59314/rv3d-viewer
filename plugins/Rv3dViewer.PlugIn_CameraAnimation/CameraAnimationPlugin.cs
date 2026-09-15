using Rv3dViewer.Plugin.Abstractions;

namespace Rv3dViewer.CameraAnimationPlugin;

public sealed class CameraAnimationPlugin : IRv3dPlugin
{
    private IPluginContext? _context;
    private CameraAnimationForm? _form;

    public void Initialize(IPluginContext context) => _context = context;

    public IReadOnlyCollection<PluginCommand> GetCommands() =>
    [
        new("camera-animation", "Camera 運鏡動畫…", ShowEditorAsync, 10)
    ];

    public void Shutdown()
    {
        if (_form is { IsDisposed: false }) _form.ShutdownEditor();
        _form = null;
        _context = null;
    }

    private Task ShowEditorAsync(CancellationToken cancellationToken)
    {
        var context = _context ?? throw new InvalidOperationException("外掛尚未初始化。");
        if (_form is { IsDisposed: false })
        {
            _form.Show();
            _form.Activate();
            return Task.CompletedTask;
        }

        var form = new CameraAnimationForm(context);
        _form = form;
        form.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_form, form)) _form = null;
        };
        form.Show();
        return Task.CompletedTask;
    }
}
