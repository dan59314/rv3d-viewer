namespace Rv3dViewer.RVInteriorDesignPlugin;

using System.Drawing.Imaging;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using Rv3dViewer.Core;

internal static class InteriorAssetThumbnailService
{
    private static string CacheDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "RVInteriorDesign", "ModelLibrary", "Thumbnails");

    internal static async Task<Bitmap?> GetAsync(InteriorAssetDescriptor asset,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!asset.CanPlace)
            return null;
        var cachePath = GetCachePath(asset);
        if (File.Exists(cachePath))
            return LoadUnlocked(cachePath);
        var model = await InteriorModelImportService.ImportAsync(asset.ModelPath, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!SceneTraversal.TryCalculateBounds(model, out var bounds))
            return null;
        using var viewport = new InteriorViewportControl
        {
            Size = new Size(320, 240),
            ViewKind = InteriorViewportKind.Perspective,
            ShowModelEdges = true,
            VisibilityMode = InteriorVisibilityMode.Solid
        };
        viewport.SetSceneModels([model]);
        FrameCamera(viewport.Camera, bounds);
        using var rendered = viewport.RenderSceneBitmapForTest(new Size(320, 240));
        var thumbnail = new Bitmap(240, 180, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(thumbnail))
        {
            graphics.Clear(Color.FromArgb(238, 240, 242));
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            var contentBounds = FindContentBounds(rendered);
            if (contentBounds.IsEmpty)
                contentBounds = new Rectangle(0, 0, rendered.Width, rendered.Height);
            contentBounds.Inflate(3, 3);
            contentBounds.Intersect(new Rectangle(0, 0, rendered.Width, rendered.Height));
            const int padding = 12;
            var availableWidth = thumbnail.Width - padding * 2;
            var availableHeight = thumbnail.Height - padding * 2;
            var scale = Math.Min(availableWidth / (float)contentBounds.Width,
                availableHeight / (float)contentBounds.Height);
            var targetWidth = Math.Max(1, (int)MathF.Round(contentBounds.Width * scale));
            var targetHeight = Math.Max(1, (int)MathF.Round(contentBounds.Height * scale));
            var target = new Rectangle((thumbnail.Width - targetWidth) / 2,
                (thumbnail.Height - targetHeight) / 2, targetWidth, targetHeight);
            graphics.DrawImage(rendered, target, contentBounds, GraphicsUnit.Pixel);
            using var border = new Pen(Color.FromArgb(150, 155, 160));
            graphics.DrawRectangle(border, 0, 0, thumbnail.Width - 1, thumbnail.Height - 1);
        }
        string? temporaryPath = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(CacheDirectory);
            temporaryPath = Path.Combine(CacheDirectory, $".{Path.GetFileName(cachePath)}.{Guid.NewGuid():N}.tmp");
            thumbnail.Save(temporaryPath, ImageFormat.Png);
            File.Move(temporaryPath, cachePath, true);
            temporaryPath = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ExternalException)
        {
            // Thumbnail caching is best effort; the in-memory image is still usable.
        }
        finally
        {
            if (temporaryPath is not null && File.Exists(temporaryPath))
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // Another thumbnail operation may still be releasing the temporary image file.
                }
            }
        }
        return thumbnail;
    }

    internal static void Remove(InteriorAssetDescriptor asset)
    {
        var path = GetCachePath(asset);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void FrameCamera(CameraState camera, SceneBounds bounds)
    {
        var direction = Vector3.Normalize(new Vector3(1.35f, .9f, 1.55f));
        var radius = Math.Max(bounds.Radius, .05f);
        var distance = radius / MathF.Sin(camera.FieldOfViewDegrees * MathF.PI / 360f) * 1.35f;
        camera.To = bounds.Center;
        camera.From = bounds.Center + direction * distance;
        camera.Up = Vector3.UnitY;
        camera.NearPlane = Math.Max(.0001f, distance - radius * 2f);
        camera.FarPlane = Math.Max(camera.NearPlane + 1f, distance + radius * 3f);
    }

    private static Rectangle FindContentBounds(Bitmap bitmap)
    {
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var minX = bitmap.Width;
            var minY = bitmap.Height;
            var maxX = -1;
            var maxY = -1;
            var row = new byte[Math.Abs(data.Stride)];
            for (var y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                for (var x = 0; x < bitmap.Width; x++)
                {
                    if (row[x * 4 + 3] <= 8) continue;
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
            return maxX < minX || maxY < minY
                ? Rectangle.Empty
                : Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static string GetCachePath(InteriorAssetDescriptor asset)
    {
        var modified = File.Exists(asset.ModelPath) ? File.GetLastWriteTimeUtc(asset.ModelPath).Ticks : 0L;
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{Path.GetFullPath(asset.ModelPath)}|{modified}|v7-scale-aware-viewport")));
        return Path.Combine(CacheDirectory, $"{key}.png");
    }

    private static Bitmap LoadUnlocked(string path)
    {
        var bytes = File.ReadAllBytes(path);
        using var stream = new MemoryStream(bytes, writable: false);
        using var image = Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: true);
        return new Bitmap(image);
    }
}
