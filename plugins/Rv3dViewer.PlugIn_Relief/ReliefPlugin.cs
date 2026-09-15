using Rv3dViewer.Plugin.Abstractions;
using Rv3dViewer.Core;

namespace Rv3dViewer.ReliefPlugin;

public sealed class ReliefPlugin : IRv3dPlugin
{
    private IPluginContext? _context;

    public void Initialize(IPluginContext context) => _context = context;

    public IReadOnlyCollection<PluginCommand> GetCommands() =>
    [
        new("create-relief-model", "建立 2.5D 浮雕模型…", ShowBuilderAsync, 40)
    ];

    public void Shutdown() => _context = null;

    private Task ShowBuilderAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = _context ?? throw new InvalidOperationException("外掛尚未初始化。");
        var targetProject = context.Project;

        using var form = new ReliefBuilderForm(targetProject);
        if (form.ShowDialog() != DialogResult.OK || form.AcceptedModel is not { } model)
            return Task.CompletedTask;

        if (!ReferenceEquals(targetProject, context.Project))
        {
            ReliefSceneAssetService.RollBack(form.AcceptedAsset);
            context.ShowMessage("專案已在浮雕視窗開啟期間變更，因此未加入模型。", "建立 2.5D 浮雕模型", PluginMessageKind.Warning);
            return Task.CompletedTask;
        }

        if (targetProject.Models.Any(existing => existing.Id == model.Id))
        {
            ReliefSceneAssetService.RollBack(form.AcceptedAsset);
            context.SetStatus("浮雕模型已存在於場景，未重複加入。");
            return Task.CompletedTask;
        }

        try
        {
            targetProject.Models.Add(model);
            context.NotifySceneChanged();
            var triangleCount = model.Meshes.Sum(mesh => mesh.TriangleCount);
            context.SetStatus($"已加入「{model.Name}」至場景，共 {triangleCount:N0} 個三角形。");
        }
        catch (Exception ex)
        {
            targetProject.Models.Remove(model);
            ReliefSceneAssetService.RollBack(form.AcceptedAsset);
            try { context.NotifySceneChanged(); }
            catch { }
            context.ShowMessage($"浮雕模型未能加入場景：{ex.Message}", "建立 2.5D 浮雕模型", PluginMessageKind.Error);
        }
        return Task.CompletedTask;
    }
}
