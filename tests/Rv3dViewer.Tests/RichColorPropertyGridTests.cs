using System.ComponentModel;
using System.Drawing.Design;
using Rv3dViewer.App;
using Rv3dViewer.Plugin.WinForms;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class RichColorPropertyGridTests
{
    [Theory]
    [InlineData("BaseColor")]
    [InlineData("Emissive")]
    [InlineData("AbsorptionColor")]
    public void MaterialColorsUseRichEditorWithoutKnownColorDropDown(string propertyName)
    {
        var inspectorType = typeof(MainForm).GetNestedType(
            "MaterialInspector",
            System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(inspectorType);
        AssertRichColorProperty(inspectorType!, propertyName);
    }

    [Fact]
    public void LightColorUsesRichEditorWithoutKnownColorDropDown()
    {
        var inspectorType = typeof(MainForm).GetNestedType(
            "LightInspector",
            System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(inspectorType);
        AssertRichColorProperty(inspectorType!, "Color");
    }

    [Fact]
    public void EveryMaterialParameterProvidesPropertyGridHelpText()
    {
        var inspectorType = typeof(MainForm).GetNestedType(
            "MaterialInspector",
            System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(inspectorType);
        var properties = TypeDescriptor.GetProperties(inspectorType!);
        Assert.NotEmpty(properties.Cast<PropertyDescriptor>());
        Assert.All(properties.Cast<PropertyDescriptor>(), property =>
            Assert.False(string.IsNullOrWhiteSpace(property.Description), property.Name));
        Assert.Contains("反射越清晰", properties["Roughness"]!.Description, StringComparison.Ordinal);
        Assert.Contains("一般玻璃約 1.5", properties["IndexOfRefraction"]!.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void MainFormViewportColorsExcludePluginSpecificSettings()
    {
        var definitions = ViewportColorPreferences.MainFormDefinitions;

        Assert.Contains(definitions, definition => definition.Key == "StudioBackground");
        Assert.Contains(definitions, definition => definition.Key == "Selection");
        Assert.DoesNotContain(definitions, definition => definition.Key == "CameraPath");
        Assert.DoesNotContain(definitions, definition => definition.Key == "InteriorWallFill");
        Assert.DoesNotContain(definitions, definition => definition.Key == "ColdPlateGrid");
        Assert.DoesNotContain(definitions, definition => definition.Key == "PreviewHelpText");
    }

    [Theory]
    [InlineData("動畫製作", "CameraPath")]
    [InlineData("室內配置設計", "InteriorWallFill")]
    [InlineData("冷板模型", "ColdPlateGrid")]
    public void PluginViewportColorsIncludeOnlyTheirSpecificSettings(
        string pluginName,
        string expectedSpecificKey)
    {
        var definitions = ViewportColorPreferences.ForPlugin(pluginName);

        Assert.Contains(definitions, definition => definition.Key == "StudioBackground");
        Assert.Contains(definitions, definition => definition.Key == "PreviewHelpText");
        Assert.Contains(definitions, definition => definition.Key == expectedSpecificKey);
        Assert.DoesNotContain(definitions, definition =>
            definition.Scope is not ViewportColorPreferences.SharedScope and
                not ViewportColorPreferences.PluginCommonScope &&
            definition.Key != expectedSpecificKey &&
            (definition.Key == "CameraPath" || definition.Key == "InteriorWallFill" ||
             definition.Key == "ColdPlateGrid"));
    }

    private static void AssertRichColorProperty(Type inspectorType, string propertyName)
    {
        var property = TypeDescriptor.GetProperties(inspectorType)[propertyName];

        Assert.NotNull(property);
        Assert.IsType<RichColorConverter>(property!.Converter);
        Assert.False(property.Converter.GetStandardValuesSupported());
        var editor = Assert.IsAssignableFrom<UITypeEditor>(
            property.GetEditor(typeof(UITypeEditor)));
        Assert.Equal(UITypeEditorEditStyle.Modal, editor.GetEditStyle());
    }
}
