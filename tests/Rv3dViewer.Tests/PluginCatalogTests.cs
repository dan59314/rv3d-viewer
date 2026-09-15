using Rv3dViewer.App.Plugins;
using System.Text.Json;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class PluginCatalogTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"rv3d-plugin-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Discover_CreatesMissingRootDirectory()
    {
        var result = PluginCatalog.Discover(_root);

        Assert.True(Directory.Exists(_root));
        Assert.Empty(result.Plugins);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Discover_ReportsMalformedManifestWithoutStoppingOtherPlugins()
    {
        var brokenDirectory = Path.Combine(_root, "Broken");
        Directory.CreateDirectory(brokenDirectory);
        File.WriteAllText(Path.Combine(brokenDirectory, "plugin.json"), "{not-json");
        CreatePlugin("Valid", "valid", "Valid plugin", menuOrder: 20);

        var result = PluginCatalog.Discover(_root);

        Assert.Single(result.Plugins);
        Assert.Equal("valid", result.Plugins[0].Manifest.Id);
        Assert.Single(result.Issues);
    }

    [Fact]
    public void Discover_SortsPluginsAndRejectsDuplicateIds()
    {
        CreatePlugin("Later", "same-id", "Later", menuOrder: 200);
        CreatePlugin("Earlier", "earlier-id", "Earlier", menuOrder: 10);
        CreatePlugin("Duplicate", "same-id", "Duplicate", menuOrder: 300);

        var result = PluginCatalog.Discover(_root);

        Assert.Equal(["earlier-id", "same-id"], result.Plugins.Select(plugin => plugin.Manifest.Id));
        Assert.Single(result.Issues);
        Assert.Contains("重複", result.Issues[0].Message);
    }

    [Fact]
    public void Discover_RejectsIncompatibleApiVersion()
    {
        CreatePlugin("Future", "future", "Future", hostApiVersion: "2.0");

        var result = PluginCatalog.Discover(_root);

        Assert.Empty(result.Plugins);
        Assert.Single(result.Issues);
        Assert.Contains("不相容", result.Issues[0].Message);
    }

    [Fact]
    public void Discover_SkipsDisabledPlugin()
    {
        CreatePlugin("Disabled", "disabled", "Disabled", enabled: false);

        var result = PluginCatalog.Discover(_root);

        Assert.Empty(result.Plugins);
        Assert.Empty(result.Issues);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private void CreatePlugin(
        string folder,
        string id,
        string name,
        int menuOrder = 100,
        string hostApiVersion = "1.0",
        bool enabled = true)
    {
        var directory = Path.Combine(_root, folder);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "Plugin.dll"), []);
        var manifest = new PluginManifest
        {
            Id = id,
            Name = name,
            Description = "Test plugin",
            Version = "1.0.0",
            Assembly = "Plugin.dll",
            EntryType = "Test.Plugin",
            HostApiVersion = hostApiVersion,
            Enabled = enabled,
            MenuOrder = menuOrder
        };
        File.WriteAllText(Path.Combine(directory, "plugin.json"), JsonSerializer.Serialize(manifest));
    }
}
