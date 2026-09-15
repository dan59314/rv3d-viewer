# Rv3d Viewer Plugin Development

Plugins run inside the Viewer process and are loaded from `PlugIn` beside `Rv3dViewer.App.exe`. Install each plugin in its own directory:

```text
PlugIn/
  MyPlugin/
    plugin.json
    MyPlugin.dll
    MyPlugin.deps.json
    dependency.dll
```

## Manifest

```json
{
  "id": "vendor.my-plugin",
  "name": "My Plugin",
  "description": "Plugin description",
  "version": "1.0.0",
  "assembly": "MyPlugin.dll",
  "entryType": "Vendor.MyPlugin.Plugin",
  "hostApiVersion": "1.0",
  "enabled": true,
  "menuOrder": 100
}
```

IDs must be unique. The host currently accepts plugins with the same major API version. Invalid, disabled, duplicate, or incompatible plugins are skipped; errors are written to `PlugIn/Logs`.

## Entry point

Reference `Rv3dViewer.Plugin.Abstractions` and implement `IRv3dPlugin` with a public parameterless constructor. `Initialize` receives an `IPluginContext`; `GetCommands` returns menu commands; `Shutdown` releases event handlers and resources.

```csharp
public sealed class Plugin : IRv3dPlugin
{
    private IPluginContext context = null!;

    public void Initialize(IPluginContext value) => context = value;

    public IReadOnlyCollection<PluginCommand> GetCommands() =>
    [
        new("show-selection", "Show selection", cancellationToken =>
        {
            context.ShowMessage(context.SelectedModel?.Name ?? "Nothing selected");
            return Task.CompletedTask;
        })
    ];

    public void Shutdown() { }
}
```

After changing project data, call `NotifySceneChanged()` to rebuild the Viewer UI, invalidate rendering, and mark the project as modified. Use `InvalidateScene()` for a render-only refresh or `MarkProjectModified()` when no UI rebuild is required.

Camera plugins should use `context.Camera` instead of modifying `Project.Camera` directly. `Apply(..., CameraApplyMode.TransientPreview)` updates the viewport and Camera inspector without creating an undo entry or marking the project modified; use `Commit` only when the new view should become project state. During animation playback, hold the `IDisposable` returned by `AcquirePlaybackControl()` and dispose it when playback stops so mouse navigation is restored. Subscribe to `context.Camera.Changed` when the plugin needs to follow camera edits made by the Viewer.

Use `context.Viewport.RenderPreviewPngAsync(camera, ..., useQuickPreview)` to render a scene preview from an independent camera without changing the project's camera. Pass `context.QuickPreviewEnabled` for an interactive plugin preview that follows the MainForm mode, and `false` for final exports. Use `CapturePngAsync(...)` for deterministic capture of the current project viewport. Calls must remain on the WinForms UI thread, and callers should throttle interactive preview requests and yield between sequential exports so cancellation and UI progress remain responsive.

Camera editors can call `RenderCameraEditorViewPngAsync(...)` for host-rendered Front, Left, and Top orthographic views. These frames include the scene plus Camera position, target, direction, and FOV overlays, while the plugin remains responsible for mapping mouse gestures back to Camera parameters.

Commands start on the WinForms UI thread. Do not modify the project or call the context concurrently from worker threads.

Use **外掛 > 重新掃描外掛** after installing a new plugin. Updating or removing an already loaded DLL requires restarting the Viewer.

## Security

Plugins have the same operating-system permissions as the Viewer. Install only trusted plugins. `AssemblyLoadContext` separates dependency loading but is not a security sandbox.

The `samples/Rv3dViewer.PlugIn_Sample` project is a working example and deploys its build output to the Debug or Release Viewer `PlugIn/SamplePlugin` directory.
