using Rv3dViewer.Core;
using Rv3dViewer.Plugin.Abstractions;
using System.Numerics;

namespace Rv3dViewer.SamplePlugin;

public sealed class SamplePlugin : IRv3dPlugin
{
    private IPluginContext? _context;

    public void Initialize(IPluginContext context) => _context = context;

    public IReadOnlyCollection<PluginCommand> GetCommands() =>
    [
        new("selection-info", "顯示目前選取資訊", ShowSelectionInfoAsync, 10),
        new("make-material-red", "將選取材質改為紅色", MakeMaterialRedAsync, 20,
            () => _context?.SelectedMaterial is not null),
        new("add-point-light", "新增點光源", AddPointLightAsync, 30,
            () => _context is { Project.Lights.Count: < 8 })
    ];

    public void Shutdown() => _context = null;

    private Task ShowSelectionInfoAsync(CancellationToken cancellationToken)
    {
        var context = RequireContext();
        var model = context.SelectedModel?.Name ?? "（未選取）";
        var mesh = context.SelectedMeshIndex is int meshIndex ? meshIndex.ToString() : "（未選取）";
        var material = context.SelectedMaterial?.Name ?? "（未選取）";
        var light = context.SelectedLight?.Name ?? "（未選取）";
        context.ShowMessage($"模型：{model}\nMesh Index：{mesh}\n材質：{material}\n燈光：{light}", "範例外掛");
        return Task.CompletedTask;
    }

    private Task MakeMaterialRedAsync(CancellationToken cancellationToken)
    {
        var context = RequireContext();
        var material = context.SelectedMaterial;
        if (material is null) return Task.CompletedTask;
        material.BaseColor = new Vector4(1f, 0.12f, 0.08f, material.BaseColor.W);
        context.NotifySceneChanged();
        context.SetStatus($"範例外掛已修改材質：{material.Name}");
        return Task.CompletedTask;
    }

    private Task AddPointLightAsync(CancellationToken cancellationToken)
    {
        var context = RequireContext();
        if (context.Project.Lights.Count >= 8) return Task.CompletedTask;
        context.Project.Lights.Add(new SceneLight
        {
            Name = "Plugin Point Light",
            Type = SceneLightType.Point,
            Position = new Vector3(0f, 3f, 2f),
            Color = new Vector3(1f, 0.8f, 0.55f),
            Intensity = 4f,
            Range = 15f
        });
        context.NotifySceneChanged();
        context.SetStatus("範例外掛已新增點光源");
        return Task.CompletedTask;
    }

    private IPluginContext RequireContext() =>
        _context ?? throw new InvalidOperationException("外掛尚未初始化。");
}
