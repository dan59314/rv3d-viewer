using Rv3dViewer.Core;

namespace Rv3dViewer.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args is ["--create-default-hdr", var projectPath])
        {
            var store = new JsonProjectStore();
            var project = store.LoadAsync(projectPath).GetAwaiter().GetResult();
            DefaultHdrService.CreateForProject(project);
            store.SaveAsync(project, projectPath).GetAwaiter().GetResult();
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
