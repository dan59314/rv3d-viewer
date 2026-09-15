using System.Numerics;
using Rv3dViewer.Core;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class PbrMaterialTests
{
    [Fact]
    public void Clone_CreatesIndependentMaterialAndTextureSlots()
    {
        var source = new PbrMaterial
        {
            Name = "Copper",
            Metallic = 1f,
            Textures = new Dictionary<TextureSemantic, TextureSlot>
            {
                [TextureSemantic.BaseColor] = new() { Path = "copper.png", Wrap = TextureWrap.ClampToEdge }
            }
        };

        var clone = source.Clone("Copper Copy");
        clone.Textures[TextureSemantic.BaseColor].Path = "changed.png";

        Assert.Equal("Copper Copy", clone.Name);
        Assert.Equal(1f, clone.Metallic);
        Assert.Equal("copper.png", source.Textures[TextureSemantic.BaseColor].Path);
        Assert.NotSame(source.Textures[TextureSemantic.BaseColor], clone.Textures[TextureSemantic.BaseColor]);
    }

    [Fact]
    public void RequiresAlphaBlending_DetectsMaterialAndTextureOpacity()
    {
        Assert.False(new PbrMaterial().RequiresAlphaBlending);
        Assert.True(new PbrMaterial { Opacity = 0.5f }.RequiresAlphaBlending);
        Assert.True(new PbrMaterial { BaseColor = new Vector4(1f, 1f, 1f, 0.5f) }.RequiresAlphaBlending);
        Assert.True(new PbrMaterial
        {
            Textures = new Dictionary<TextureSemantic, TextureSlot>
            {
                [TextureSemantic.Opacity] = new() { Path = "opacity.png" }
            }
        }.RequiresAlphaBlending);
        Assert.False(new PbrMaterial
        {
            Textures = new Dictionary<TextureSemantic, TextureSlot>
            {
                [TextureSemantic.Opacity] = new() { Path = "opacity.png", Enabled = false }
            }
        }.RequiresAlphaBlending);
        Assert.False(new PbrMaterial { RenderMode = MaterialRenderMode.Opaque, Opacity = 0.2f }.RequiresAlphaBlending);
        Assert.False(new PbrMaterial { RenderMode = MaterialRenderMode.Cutout, Opacity = 0.2f }.RequiresAlphaBlending);
        Assert.True(new PbrMaterial { RenderMode = MaterialRenderMode.Transparent }.RequiresAlphaBlending);
        Assert.True(new PbrMaterial { RenderMode = MaterialRenderMode.Glass }.RequiresAlphaBlending);
        Assert.True(new PbrMaterial { RenderMode = MaterialRenderMode.Glass }.IsGlass);
    }

    [Fact]
    public void Validate_ClampsGlassParameters()
    {
        var material = new PbrMaterial
        {
            Transmission = 2f,
            IndexOfRefraction = 4f,
            Thickness = -1f,
            RefractionStrength = 1f,
            Dispersion = 2f,
            AbsorptionColor = new Vector3(-1f, 0.5f, 2f)
        };

        material.Validate();

        Assert.Equal(1f, material.Transmission);
        Assert.Equal(2.5f, material.IndexOfRefraction);
        Assert.Equal(0f, material.Thickness);
        Assert.Equal(0.25f, material.RefractionStrength);
        Assert.Equal(1f, material.Dispersion);
        Assert.Equal(new Vector3(0f, 0.5f, 1f), material.AbsorptionColor);
    }

    [Theory]
    [InlineData(0F, 0F)]
    [InlineData(-0.25F, 0F)]
    [InlineData(1.25F, 1F)]
    public void Validate_AllowsZeroRoughnessAndClampsToUnitRange(float input, float expected)
    {
        var material = new PbrMaterial { Roughness = input };

        material.Validate();

        Assert.Equal(expected, material.Roughness);
    }
}
