# Rv3d Viewer

Windows WinForms/OpenGL 3D model viewer and PBR material preview editor.

## Current features

- Imports STL, OBJ/MTL and 3DS through Assimp.
- Supports multiple scene models, imported node hierarchies, shared mesh instances, transforms and visibility.
- Scene-tree meshes have independent object-local position, rotation, scale and visibility controls that are saved with the project.
- OpenGL 3.3 Metallic-Roughness PBR rendering with seven texture semantics.
- Opaque-first rendering plus camera-sorted, depth-safe alpha blending for layered glass, liquid and other transparent meshes.
- Explicit Auto/Opaque/Cutout/Transparent/Glass material modes with screen-space glass transmission, IOR, thickness, refraction strength and absorption tint controls.
- Radiance `.hdr` environments for image-based diffuse lighting, roughness-aware mipmapped reflections and an optional adjustable background.
- A DesignTime environment command can generate and immediately load a procedural dual-softbox `defaultStudio.hdr` beside the selected model asset.
- Edit modes for navigation, additive box selection and subtractive box selection, with Ctrl-add, Alt-subtract, confirmed selection deletion and an optional amber highlight.
- Selecting a scene-tree mesh shows a single white mesh outline and moves that outline exclusively to the next selected mesh.
- Up to eight editable directional lights with a bright two-light diagonal default rig.
- Left-drag orbit in navigation mode, Alt+left-drag camera roll around the From-To axis, right-drag pan, middle-drag target pan and wheel dolly, plus canonical camera views.
- Versioned `.rv3dproj` JSON projects with portable OBJ/MTL/texture/HDR environment bundles and missing-asset repair.
- Saving `Scene.rv3dproj` creates a self-contained `Scene/Scene.rv3dproj` project folder with all related assets.
- Visual Studio WinForms Designer UI.
- Automatically restores and saves `lastScene/lastScene.rv3dproj` beside the executable, including its portable asset bundle.
- Keeps the application-wide material library outside project files. It is restored from and saved to `libraries/materials.mtllib` beside the executable, and can also be loaded or saved explicitly as an `.mtllib` file.
- Loads manifest-based .NET plugins from the executable's `PlugIn` directory and adds their commands to the design-time `外掛` menu.
- Platform-neutral Core project designed for a future WebGL/WebGPU client.

## Build

```powershell
dotnet restore Rv3dViewer.sln
dotnet build Rv3dViewer.sln -c Debug -p:Platform=x64
dotnet test tests/Rv3dViewer.Tests/Rv3dViewer.Tests.csproj
```

Run `src/Rv3dViewer.App/bin/x64/Debug/net8.0-windows/win-x64/Rv3dViewer.App.exe` after building.

See [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md), [ARCHITECTURE.md](ARCHITECTURE.md), and [PLUGIN_DEVELOPMENT.md](PLUGIN_DEVELOPMENT.md).
