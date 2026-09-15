# Rv3d Viewer Architecture

## Portability boundary

`Rv3dViewer.Core` contains the canonical scene, mesh, PBR material, camera, project DTOs and interaction math. It has no WinForms, OpenTK, Assimp or Windows dependency. This is the contract shared by the desktop application and a future Web application.

The desktop project supplies the platform adapters:

- `AssimpModelImporter` implements `IModelImporter` with the native Assimp library.
- `OpenGlRenderer` implements `ISceneRenderer` with OpenTK and a WinForms GLControl.
- `ProjectAssetService` maps portable relative asset identifiers to the Windows file system.

## Future Web application

A Web client should reference `Rv3dViewer.Core` from Blazor WebAssembly or consume an equivalent generated TypeScript schema. Replace only the adapters:

- Implement `IModelImporter` with a WebAssembly Assimp build, a JavaScript loader, or a server-side conversion service.
- Implement `ISceneRenderer` with WebGL2/WebGPU, Three.js, Babylon.js or a native WebGL renderer.
- Store `.rv3dproj` JSON and assets in browser File System Access API, IndexedDB, object storage, or an HTTP API.

The project JSON uses relative asset paths and does not serialize GPU handles or imported vertex buffers. A Web client reimports the referenced model and then applies the saved transform, PBR material overrides and camera state.

## Coordinate and material contract

- Right-handed coordinates, Y-up.
- Transforms are position, XYZ Euler rotation in degrees, and scale.
- Camera is explicit `From`, `To`, `Up`, vertical FOV, near and far planes.
- PBR uses Metallic-Roughness with BaseColor, Metallic, Roughness, Normal, AO, Emissive and Opacity texture semantics.
- BaseColor and Emissive textures are sRGB; all remaining data textures are linear.
