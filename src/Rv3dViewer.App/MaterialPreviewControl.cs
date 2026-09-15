using Rv3dViewer.Core;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Numerics;

namespace Rv3dViewer.App;

/// <summary>Design-time safe, lightweight PBR material preview.</summary>
[ToolboxItem(true)]
public sealed class MaterialPreviewControl : Control
{
    private PbrMaterial? _material;
    private string? _projectDirectory;
    private Bitmap? _sphere;

    public MaterialPreviewControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint, true);
        MinimumSize = new Size(120, 150);
        Padding = new Padding(8);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PbrMaterial? Material
    {
        get => _material;
        set
        {
            _material = value;
            ResetPreview();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? ProjectDirectory
    {
        get => _projectDirectory;
        set
        {
            if (string.Equals(_projectDirectory, value, StringComparison.OrdinalIgnoreCase)) return;
            _projectDirectory = value;
            ResetPreview();
        }
    }

    public void RefreshMaterial() => ResetPreview();

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var dark = BackColor.GetBrightness() < 0.45F;
        var inset = Rectangle.Inflate(ClientRectangle, -Padding.Left, -Padding.Top);
        if (inset.Width <= 0 || inset.Height <= 0) return;

        using (var clipPath = MaterialPreviewGraphicsExtensions.CreateRoundedRectangle(inset, 8))
        {
            var state = e.Graphics.Save();
            e.Graphics.SetClip(clipPath);
            DrawRgbTransparencyGrid(e.Graphics, inset, dark);
            e.Graphics.Restore(state);
        }

        using (var border = new Pen(dark ? Color.FromArgb(82, 86, 91) : Color.FromArgb(170, 176, 184)))
            e.Graphics.DrawRoundedRectangle(border, inset, 8);

        var titleColor = dark ? Color.Gainsboro : Color.FromArgb(38, 41, 46);
        var title = _material is null ? "未選取材質" : _material.Name;
        TextRenderer.DrawText(e.Graphics, title, Font,
            new Rectangle(inset.Left + 8, inset.Top + 5, inset.Width - 16, Font.Height + 5),
            titleColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);

        var availableHeight = inset.Height - Font.Height - 20;
        var diameter = Math.Max(1, Math.Min(inset.Width - 24, availableHeight));
        if (diameter < 12) return;
        if (_sphere is null || _sphere.Width != diameter)
            _sphere = RenderSphere(diameter, _material, dark);

        var x = inset.Left + (inset.Width - diameter) / 2;
        var y = inset.Bottom - diameter - 8;
        e.Graphics.DrawImageUnscaled(_sphere, x, y);
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        ResetPreview();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _sphere?.Dispose();
        base.Dispose(disposing);
    }

    private void ResetPreview()
    {
        _sphere?.Dispose();
        _sphere = null;
        Invalidate();
    }

    private static void DrawRgbTransparencyGrid(Graphics graphics, Rectangle bounds, bool dark)
    {
        var colors =
            //dark
            //? new[] { Color.FromArgb(56, 34, 38), Color.FromArgb(31, 54, 42), Color.FromArgb(32, 43, 63) }
            //: new[] { Color.FromArgb(245, 218, 222), Color.FromArgb(216, 241, 224), Color.FromArgb(216, 226, 245) };
            new[] { Color.FromArgb(200,255,0,0), Color.FromArgb(200,0,255,0), Color.FromArgb(200,0,0,255) };

        const int cellW = 32;
        using var red = new SolidBrush(colors[0]);
        using var green = new SolidBrush(colors[1]);
        using var blue = new SolidBrush(colors[2]);
        var brushes = new Brush[] { red, green, blue };

        for (var y = bounds.Top; y < bounds.Bottom; y += cellW)
        for (var x = bounds.Left; x < bounds.Right; x += cellW)
        {
            var column = (x - bounds.Left) / cellW;
            var row = (y - bounds.Top) / cellW;
            graphics.FillRectangle(brushes[(column + row * 2) % 3], x, y, cellW, cellW);
        }

        using var gridPen = new Pen(dark ? Color.FromArgb(42, 255, 255, 255) : Color.FromArgb(36, 0, 0, 0));
        for (var x = bounds.Left; x <= bounds.Right; x += cellW)
            graphics.DrawLine(gridPen, x, bounds.Top, x, bounds.Bottom);
        for (var y = bounds.Top; y <= bounds.Bottom; y += cellW)
            graphics.DrawLine(gridPen, bounds.Left, y, bounds.Right, y);
    }

    private Bitmap RenderSphere(int size, PbrMaterial? material, bool dark)
    {
        var result = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        if (material is null) return result;

        var textures = LoadTextures(material);
        var baseColor = Vector3.Clamp(new Vector3(material.BaseColor.X, material.BaseColor.Y, material.BaseColor.Z), Vector3.Zero, Vector3.One);
        var baseMetallic = Math.Clamp(material.Metallic, 0F, 1F);
        var baseRoughness = Math.Clamp(material.Roughness, 0F, 1F);
        var baseAo = Math.Clamp(material.AmbientOcclusion, 0F, 1F);
        var emissiveFactor = Vector3.Clamp(material.Emissive * material.EmissiveStrength, Vector3.Zero, new Vector3(3F));
        var light = Vector3.Normalize(new Vector3(-0.45F, -0.55F, 1F));
        var view = Vector3.UnitZ;
        var halfVector = Vector3.Normalize(light + view);
        var center = (size - 1) * 0.5F;
        var radius = Math.Max(1F, center - 2F);

        try
        {
            for (var py = 0; py < size; py++)
            for (var px = 0; px < size; px++)
            {
                var nx = (px - center) / radius;
                var ny = (py - center) / radius;
                var distanceSquared = nx * nx + ny * ny;
                if (distanceSquared > 1F) continue;

                var nz = MathF.Sqrt(MathF.Max(0F, 1F - distanceSquared));
                var geometricNormal = Vector3.Normalize(new Vector3(nx, ny, nz));
                var baseSample = SampleTexture(textures, TextureSemantic.BaseColor, geometricNormal, Vector4.One);
                var sampledColor = baseColor * new Vector3(baseSample.X, baseSample.Y, baseSample.Z);
                var metallic = Math.Clamp(baseMetallic * SampleScalar(textures, TextureSemantic.Metallic, geometricNormal), 0F, 1F);
                var roughness = Math.Clamp(baseRoughness * SampleScalar(textures, TextureSemantic.Roughness, geometricNormal), 0.001F, 1F);
                var ao = Math.Clamp(baseAo * SampleScalar(textures, TextureSemantic.AmbientOcclusion, geometricNormal), 0F, 1F);
                var normal = ApplyNormalMap(textures, geometricNormal, material.NormalScale);
                var opacityMap = SampleScalar(textures, TextureSemantic.Opacity, geometricNormal);
                var opacity = Math.Clamp(material.Opacity * material.BaseColor.W * baseSample.W * opacityMap, 0F, 1F);

                if (material.RenderMode == MaterialRenderMode.Cutout && opacity < material.AlphaCutoff)
                    continue;
                if (material.RenderMode is MaterialRenderMode.Opaque or MaterialRenderMode.Cutout)
                    opacity = 1F;

                var emissiveSample = SampleTexture(textures, TextureSemantic.Emissive, geometricNormal, Vector4.One);
                var emissive = emissiveFactor * new Vector3(emissiveSample.X, emissiveSample.Y, emissiveSample.Z);
                var diffuse = MathF.Max(0F, Vector3.Dot(normal, light));
                var rim = MathF.Pow(1F - MathF.Max(0F, Vector3.Dot(normal, view)), 2.2F) * 0.18F;
                var specularPower = 4F + MathF.Pow(1F - roughness, 2F) * 220F;
                var specular = MathF.Pow(MathF.Max(0F, Vector3.Dot(normal, halfVector)), specularPower);
                var dielectricF0 = 0.04F;
                var specularColor = Vector3.Lerp(new Vector3(dielectricF0), sampledColor, metallic);
                var diffuseColor = sampledColor * (1F - metallic) * (0.06F * ao + diffuse * 0.94F);
                var color = diffuseColor + specularColor * specular * (0.35F + metallic * 0.8F) + sampledColor * rim * ao + emissive;

                if (material.RenderMode == MaterialRenderMode.Glass)
                {
                    var ior = Math.Clamp(material.IndexOfRefraction, 1F, 2.5F);
                    var transmission = Math.Clamp(material.Transmission, 0F, 1F);
                    var thickness = Math.Max(0F, material.Thickness);
                    var refractionOffset = material.RefractionStrength * thickness * (0.5F + 1F / Math.Max(ior, 1.0001F));
                    var behind = RgbGridColor(
                        px + (int)MathF.Round(normal.X * refractionOffset * size),
                        py + (int)MathF.Round(normal.Y * refractionOffset * size),
                        dark);
                    var absorptionAmount = 1F - MathF.Exp(-thickness);
                    var absorption = Vector3.Lerp(Vector3.One,
                        Vector3.Clamp(material.AbsorptionColor, Vector3.Zero, Vector3.One), absorptionAmount);
                    var transmitted = behind * absorption * transmission;
                    dielectricF0 = MathF.Pow((ior - 1F) / (ior + 1F), 2F);
                    var glassFresnel = dielectricF0 + (1F - dielectricF0) * MathF.Pow(1F - MathF.Max(0F, Vector3.Dot(normal, view)), 5F);
                    color = Vector3.Lerp(transmitted, color, Math.Clamp(glassFresnel + (1F - transmission), 0F, 1F));
                    opacity = 1F;
                }

                // Soften the silhouette by fading only the outermost pixel band.
                var edgeAlpha = Math.Clamp((1F - MathF.Sqrt(distanceSquared)) * radius, 0F, 1F);
                var alpha = (int)MathF.Round(255F * opacity * edgeAlpha);
                result.SetPixel(px, py, Color.FromArgb(alpha, ToByte(color.X), ToByte(color.Y), ToByte(color.Z)));
            }
        }
        finally
        {
            foreach (var texture in textures.Values) texture.Dispose();
        }

        return result;
    }

    private Dictionary<TextureSemantic, Bitmap> LoadTextures(PbrMaterial material)
    {
        var result = new Dictionary<TextureSemantic, Bitmap>();
        foreach (var (semantic, slot) in material.Textures)
        {
            var texture = TryLoadTexture(slot);
            if (texture is not null) result[semantic] = texture;
        }
        return result;
    }

    private Bitmap? TryLoadTexture(TextureSlot slot)
    {
        if (!slot.Enabled || string.IsNullOrWhiteSpace(slot.Path)) return null;

        try
        {
            var path = slot.Path;
            if (!Path.IsPathRooted(path) && !string.IsNullOrWhiteSpace(_projectDirectory))
                path = Path.Combine(_projectDirectory, path);
            if (!File.Exists(path)) return null;
            using var source = Image.FromFile(path);
            return new Bitmap(source);
        }
        catch
        {
            return null;
        }
    }

    private static Vector4 SampleTexture(
        IReadOnlyDictionary<TextureSemantic, Bitmap> textures,
        TextureSemantic semantic,
        Vector3 normal,
        Vector4 fallback)
    {
        if (!textures.TryGetValue(semantic, out var texture)) return fallback;
        var u = 0.5F + MathF.Atan2(normal.X, normal.Z) / (2F * MathF.PI);
        var v = 0.5F - MathF.Asin(Math.Clamp(normal.Y, -1F, 1F)) / MathF.PI;
        var x = Math.Clamp((int)(u * (texture.Width - 1)), 0, texture.Width - 1);
        var y = Math.Clamp((int)(v * (texture.Height - 1)), 0, texture.Height - 1);
        var color = texture.GetPixel(x, y);
        return new Vector4(color.R, color.G, color.B, color.A) / 255F;
    }

    private static float SampleScalar(
        IReadOnlyDictionary<TextureSemantic, Bitmap> textures,
        TextureSemantic semantic,
        Vector3 normal) => SampleTexture(textures, semantic, normal, Vector4.One).X;

    private static Vector3 ApplyNormalMap(
        IReadOnlyDictionary<TextureSemantic, Bitmap> textures,
        Vector3 geometricNormal,
        float normalScale)
    {
        if (!textures.ContainsKey(TextureSemantic.Normal)) return geometricNormal;
        var sample = SampleTexture(textures, TextureSemantic.Normal, geometricNormal, new Vector4(0.5F, 0.5F, 1F, 1F));
        var tangentNormal = new Vector3(
            (sample.X * 2F - 1F) * Math.Clamp(normalScale, 0F, 4F),
            (sample.Y * 2F - 1F) * Math.Clamp(normalScale, 0F, 4F),
            sample.Z * 2F - 1F);
        if (tangentNormal.LengthSquared() < 0.000001F) return geometricNormal;
        tangentNormal = Vector3.Normalize(tangentNormal);
        var tangent = MathF.Abs(geometricNormal.Y) > 0.98F
            ? Vector3.UnitX
            : Vector3.Normalize(Vector3.Cross(Vector3.UnitY, geometricNormal));
        var bitangent = Vector3.Normalize(Vector3.Cross(geometricNormal, tangent));
        return Vector3.Normalize(
            tangent * tangentNormal.X + bitangent * tangentNormal.Y + geometricNormal * tangentNormal.Z);
    }

    private static Vector3 RgbGridColor(int x, int y, bool dark)
    {
        const int cell = 14;
        var index = (Math.Abs(x / cell) + Math.Abs(y / cell) * 2) % 3;
        return (dark, index) switch
        {
            (true, 0) => new Vector3(56, 34, 38) / 255F,
            (true, 1) => new Vector3(31, 54, 42) / 255F,
            (true, _) => new Vector3(32, 43, 63) / 255F,
            (false, 0) => new Vector3(245, 218, 222) / 255F,
            (false, 1) => new Vector3(216, 241, 224) / 255F,
            _ => new Vector3(216, 226, 245) / 255F
        };
    }

    private static int ToByte(float value) => (int)MathF.Round(Math.Clamp(value, 0F, 1F) * 255F);
}

internal static class MaterialPreviewGraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectangle(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectangle(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    internal static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
