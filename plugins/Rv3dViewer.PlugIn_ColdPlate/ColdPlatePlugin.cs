using Rv3dViewer.Plugin.Abstractions;

namespace Rv3dViewer.ColdPlatePlugin;

public sealed class ColdPlatePlugin : IRv3dPlugin
{
    private IPluginContext? _context;

    public void Initialize(IPluginContext context) => _context = context;

    public IReadOnlyCollection<PluginCommand> GetCommands() =>
    [
        new("create-cold-plate", "建立冷板模型", CreateColdPlateAsync, 10)
    ];

    public void Shutdown() => _context = null;

    private Task CreateColdPlateAsync(CancellationToken cancellationToken)
    {
        var context = _context ?? throw new InvalidOperationException("外掛尚未初始化。");
        using var form = new ColdPlateBuilderForm();
        if (form.ShowDialog() != DialogResult.OK || form.GeneratedModel is null)
            return Task.CompletedTask;

        context.Project.Models.Add(form.GeneratedModel);
        context.NotifySceneChanged();
        context.SetStatus("已建立冷板模型並加入場景");
        return Task.CompletedTask;
    }
}
