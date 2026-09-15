using System.Drawing;
using Rv3dViewer.App;
using Rv3dViewer.Core;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class SkyboxFolderServiceTests
{
    [Fact]
    public void LoadFolder_ReadsSixNamedSquareFaces()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            foreach (var face in new[] { "px", "nx", "py", "ny", "pz", "nz" })
            {
                using var bitmap = new Bitmap(16, 16);
                bitmap.Save(Path.Combine(root, $"{face}.png"));
            }

            var result = SkyboxFolderService.LoadFolder(root);

            Assert.Equal(new DirectoryInfo(root).Name, result.Name);
            Assert.Equal(6, result.FacePaths.Count());
            Assert.All(result.FacePaths, path => Assert.True(File.Exists(path)));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void LoadPanorama_ConvertsTwoToOneImageAndKeepsSource()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var panoramaPath = Path.Combine(root, "studio.png");
            using (var panorama = new Bitmap(256, 128))
            using (var graphics = Graphics.FromImage(panorama))
            {
                graphics.Clear(Color.CornflowerBlue);
                panorama.Save(panoramaPath);
            }

            var result = SkyboxFolderService.LoadPanorama(panoramaPath, Path.Combine(root, "library"), 128);

            Assert.True(File.Exists(result.SourcePanorama));
            Assert.All(result.FacePaths, path =>
            {
                using var face = Image.FromFile(path);
                Assert.Equal(128, face.Width);
                Assert.Equal(128, face.Height);
            });
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void LoadPanorama_RejectsNonTwoToOneImage()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var panoramaPath = Path.Combine(root, "invalid.png");
            using (var panorama = new Bitmap(100, 100)) panorama.Save(panoramaPath);

            var exception = Assert.Throws<InvalidDataException>(() =>
                SkyboxFolderService.LoadPanorama(panoramaPath, Path.Combine(root, "library"), 128));

            Assert.Contains("2:1", exception.Message);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void LoadLibrary_GeneratesFacesWhenFolderOnlyContainsSourcePanorama()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var folder = Path.Combine(root, "Temple_DayTime");
            Directory.CreateDirectory(folder);
            using (var panorama = new Bitmap(256, 128))
            using (var graphics = Graphics.FromImage(panorama))
            {
                graphics.Clear(Color.SandyBrown);
                panorama.Save(Path.Combine(folder, "source_panorama.png"));
            }

            var result = Assert.Single(SkyboxFolderService.LoadLibrary(root));

            Assert.Equal("Temple_DayTime", result.Name);
            Assert.All(result.FacePaths, path => Assert.True(File.Exists(path)));
            Assert.EndsWith("source_panorama.png", result.SourcePanorama, StringComparison.OrdinalIgnoreCase);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void PrepareForSave_BundlesSkyboxFacesAndPanorama()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var source = Path.Combine(root, "source");
            Directory.CreateDirectory(source);
            foreach (var face in new[] { "px", "nx", "py", "ny", "pz", "nz" })
            {
                using var bitmap = new Bitmap(8, 8);
                bitmap.Save(Path.Combine(source, $"{face}.png"));
            }
            using (var panorama = new Bitmap(16, 8)) panorama.Save(Path.Combine(source, "source_panorama.png"));
            var project = new ViewerProject { Skyboxes = [SkyboxFolderService.LoadFolder(source)] };
            project.Skyboxes[0].SourcePanorama = Path.Combine(source, "source_panorama.png");
            var projectPath = Path.Combine(root, "saved", "scene.rv3dproj");

            ProjectAssetService.PrepareForSave(project, projectPath);

            Assert.All(project.Skyboxes[0].FacePaths, path =>
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(projectPath)!, path))));
            Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(projectPath)!, project.Skyboxes[0].SourcePanorama)));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"Rv3dViewerSkybox_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
