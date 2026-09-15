using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class DesignerIntegrityTests
{
    private static readonly Regex FieldRegex = new(
        @"(?m)^\s*private\s+(?!void\b)(?:[\w.<>?,]+\s+)(?<name>[_A-Za-z]\w*)\s*(?:=\s*null!|=\s*null|;)",
        RegexOptions.CultureInvariant);

    private static readonly Regex NameRegex = new(
        @"(?m)^\s*(?<field>[_A-Za-z]\w*)\.Name\s*=\s*""(?<name>[^""]+)""",
        RegexOptions.CultureInvariant);

    private static readonly Regex ControlAddRegex = new(
        @"(?m)^\s*(?<parent>[\w.]+)\.Controls\.Add\((?<child>[_A-Za-z]\w*)",
        RegexOptions.CultureInvariant);

    [Fact]
    public void WinFormsDesignerFiles_HaveCompleteAndUniqueControlDefinitions()
    {
        var root = FindSolutionRoot();
        var designerFiles = Directory.EnumerateFiles(root, "*.Designer.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Where(path => !path.EndsWith("Resources.Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(designerFiles);
        foreach (var path in designerFiles)
        {
            var source = File.ReadAllText(path);
            var fields = FieldRegex.Matches(source)
                .Select(match => match.Groups["name"].Value)
                .Where(name => name != "components")
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var field in fields)
                Assert.Matches($@"(?m)^\s*{Regex.Escape(field)}\s*=\s*new(?:\s|\()", source);

            var duplicateNames = NameRegex.Matches(source)
                .GroupBy(match => match.Groups["name"].Value, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            Assert.True(duplicateNames.Length == 0,
                $"{Path.GetFileName(path)} 有重複的 Control.Name：{string.Join(", ", duplicateNames)}");

            var duplicateAdds = ControlAddRegex.Matches(source)
                .Select(match => $"{match.Groups["parent"].Value}|{match.Groups["child"].Value}")
                .GroupBy(value => value, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            Assert.True(duplicateAdds.Length == 0,
                $"{Path.GetFileName(path)} 有重複的 Controls.Add：{string.Join(", ", duplicateAdds)}");
        }
    }

    [Fact]
    public void ResxFiles_HaveUniqueResourceKeys()
    {
        var root = FindSolutionRoot();
        foreach (var path in Directory.EnumerateFiles(root, "*.resx", SearchOption.AllDirectories)
                     .Where(path => !IsBuildOutput(path)))
        {
            var document = XDocument.Load(path);
            var duplicateKeys = document.Root?.Elements("data")
                .Select(element => (string?)element.Attribute("name"))
                .Where(name => !string.IsNullOrEmpty(name))
                .GroupBy(name => name!, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray() ?? [];

            Assert.True(duplicateKeys.Length == 0,
                $"{Path.GetFileName(path)} 有重複的 resx Key：{string.Join(", ", duplicateKeys)}");
        }
    }

    [Fact]
    public void MainAndRenderForms_KeepRequiredDesignTimeControlsInTheirControlTrees()
    {
        var root = FindSolutionRoot();
        var mainDesigner = File.ReadAllText(Path.Combine(root, "src", "Rv3dViewer.App", "MainForm.Designer.cs"));
        Assert.Contains("workSplitContainer.Panel2.Controls.Add(inspectorTabControl);", mainDesigner);
        Assert.Contains("materialPageLayoutPanel.Controls.Add(materialPreviewControl, 0, 0);", mainDesigner);
        Assert.Contains("materialPageLayoutPanel.Controls.Add(materialSplitContainer, 0, 1);", mainDesigner);

        var renderDesigner = File.ReadAllText(Path.Combine(root, "plugins",
            "Rv3dViewer.PlugIn_HighQualityRender", "HighQualityRenderForm.Designer.cs"));
        Assert.Contains("oldRenderPreviewControl = new RenderPreviewControl();", renderDesigner);
        Assert.Contains("mainSplitContainer.Panel1.Controls.Add(oldRenderPreviewControl);", renderDesigner);
        Assert.Contains("renderPreviewGlControl = new OpenTK.GLControl.GLControl();", renderDesigner);
        Assert.Contains("mainSplitContainer.Panel1.Controls.Add(renderPreviewGlControl);", renderDesigner);
        Assert.Contains("gpuDeviceComboBox = new ComboBox();", renderDesigner);
    }

    private static string FindSolutionRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Rv3dViewer.sln")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("找不到 Rv3dViewer.sln。 ");
    }

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
}
