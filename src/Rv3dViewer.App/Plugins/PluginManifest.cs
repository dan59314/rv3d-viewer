namespace Rv3dViewer.App.Plugins;

public sealed class PluginManifest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Assembly { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public string HostApiVersion { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int MenuOrder { get; set; } = 100;
}

public sealed record PluginDescriptor(PluginManifest Manifest, string DirectoryPath, string AssemblyPath);

public sealed record PluginLoadIssue(string Source, string Message);

public sealed record PluginDiscoveryResult(
    IReadOnlyList<PluginDescriptor> Plugins,
    IReadOnlyList<PluginLoadIssue> Issues);
