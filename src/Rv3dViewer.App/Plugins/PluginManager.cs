using Rv3dViewer.Plugin.Abstractions;
using System.Reflection;

namespace Rv3dViewer.App.Plugins;

public sealed class PluginManager : IDisposable
{
    private readonly IPluginContext _context;
    private readonly string _rootDirectory;
    private readonly List<LoadedPlugin> _loaded = [];
    private bool _disposed;

    public PluginManager(string rootDirectory, IPluginContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _rootDirectory = rootDirectory;
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public PluginLoadResult LoadAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var discovery = PluginCatalog.Discover(_rootDirectory);
        var issues = discovery.Issues.ToList();

        foreach (var descriptor in discovery.Plugins)
        {
            PluginLoadContext? loadContext = null;
            try
            {
                loadContext = new PluginLoadContext(descriptor.AssemblyPath);
                var assembly = loadContext.LoadFromAssemblyPath(descriptor.AssemblyPath);
                var type = assembly.GetType(descriptor.Manifest.EntryType, throwOnError: true, ignoreCase: false)
                    ?? throw new TypeLoadException($"找不到進入類別：{descriptor.Manifest.EntryType}");
                if (!typeof(IRv3dPlugin).IsAssignableFrom(type))
                    throw new InvalidCastException($"{descriptor.Manifest.EntryType} 未實作 IRv3dPlugin。");
                if (Activator.CreateInstance(type) is not IRv3dPlugin plugin)
                    throw new InvalidOperationException("無法建立外掛執行個體，請確認有公開的無參數建構函式。");

                plugin.Initialize(_context);
                var commands = ValidateCommands(plugin.GetCommands());
                _loaded.Add(new LoadedPlugin(descriptor.Manifest, plugin, commands, loadContext));
                loadContext = null;
            }
            catch (Exception ex) when (ex is not StackOverflowException and not OutOfMemoryException)
            {
                issues.Add(new PluginLoadIssue(descriptor.Manifest.Name, Unwrap(ex).Message));
                loadContext?.Unload();
            }
        }

        WriteIssues(issues);
        return new PluginLoadResult(_loaded.ToList(), issues);
    }

    public async Task ExecuteAsync(LoadedPlugin plugin, PluginCommand command, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            await command.ExecuteAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not StackOverflowException and not OutOfMemoryException)
        {
            var actual = Unwrap(ex);
            WriteIssues([new PluginLoadIssue(plugin.Manifest.Name, $"命令「{command.Text}」執行失敗：{actual.Message}")]);
            throw new PluginExecutionException(plugin.Manifest.Name, command.Text, actual);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        for (var index = _loaded.Count - 1; index >= 0; index--)
        {
            var loaded = _loaded[index];
            try { loaded.Instance.Shutdown(); }
            catch (Exception ex) { WriteIssues([new PluginLoadIssue(loaded.Manifest.Name, $"關閉失敗：{Unwrap(ex).Message}")]); }
            loaded.LoadContext.Unload();
        }
        _loaded.Clear();
    }

    private static IReadOnlyList<PluginCommand> ValidateCommands(IReadOnlyCollection<PluginCommand>? commands)
    {
        if (commands is null) throw new InvalidDataException("GetCommands() 不可回傳 null。");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var command in commands)
        {
            if (string.IsNullOrWhiteSpace(command.Id) || string.IsNullOrWhiteSpace(command.Text))
                throw new InvalidDataException("外掛命令必須提供 Id 與 Text。");
            if (!ids.Add(command.Id))
                throw new InvalidDataException($"外掛命令 ID '{command.Id}' 重複。");
        }
        return commands.OrderBy(command => command.Order).ThenBy(command => command.Text, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void WriteIssues(IEnumerable<PluginLoadIssue> issues)
    {
        var entries = issues.Select(issue => $"{DateTimeOffset.Now:O}\t{issue.Source}\t{issue.Message}").ToList();
        if (entries.Count == 0) return;
        try
        {
            var logDirectory = Path.Combine(_rootDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);
            File.AppendAllLines(Path.Combine(logDirectory, $"plugin-{DateTime.Now:yyyyMMdd}.log"), entries);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Logging must never stop the viewer from starting or closing.
        }
    }

    private static Exception Unwrap(Exception exception) =>
        exception is TargetInvocationException { InnerException: not null } invocation ? invocation.InnerException! : exception;
}

public sealed record LoadedPlugin(
    PluginManifest Manifest,
    IRv3dPlugin Instance,
    IReadOnlyList<PluginCommand> Commands,
    System.Runtime.Loader.AssemblyLoadContext LoadContext);

public sealed record PluginLoadResult(
    IReadOnlyList<LoadedPlugin> Plugins,
    IReadOnlyList<PluginLoadIssue> Issues);

public sealed class PluginExecutionException(string pluginName, string commandName, Exception innerException)
    : Exception($"外掛「{pluginName}」的命令「{commandName}」執行失敗。", innerException);
