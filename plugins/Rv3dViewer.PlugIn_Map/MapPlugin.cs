using Rv3dViewer.Plugin.Abstractions;

namespace Rv3dViewer.MapPlugin;

public sealed class MapPlugin : IRv3dPlugin
{
    private IPluginContext? _context;
    public void Initialize(IPluginContext context) => _context = context;

    public IReadOnlyCollection<PluginCommand> GetCommands() =>
    [
        new("edit-map", "貼圖編輯器…", OpenEditorAsync, 10)
    ];

    public void Shutdown() => _context = null;

    private Task OpenEditorAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = _context ?? throw new InvalidOperationException("外掛尚未初始化。");
        if (context.SelectedModel is null || context.SelectedMeshIndex is not int meshIndex ||
            context.SelectedMaterial is null || (uint)meshIndex >= (uint)context.SelectedModel.Meshes.Count)
        {
            context.ShowMessage("沒有選取 Mesh。", "貼圖編輯器", PluginMessageKind.Warning);
            return Task.CompletedTask;
        }

        using var form = new MapEditorForm(context, context.SelectedModel, meshIndex, context.SelectedMaterial);
        form.ShowDialog();
        return Task.CompletedTask;
    }
}
