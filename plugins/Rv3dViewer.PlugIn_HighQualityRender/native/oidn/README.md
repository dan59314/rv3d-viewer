# Intel Open Image Denoise runtime

Download the official Windows x64 OIDN archive from https://www.openimagedenoise.org/downloads.html.

Copy `OpenImageDenoise.dll` and every DLL shipped beside it from the archive's `bin` folder into this directory.
They are copied to the plug-in deployment folder during a normal Debug/x64 build.

At render time the plug-in automatically uses OIDN's `RT` filter with HDR color, albedo, and normal AOVs. OIDN selects its default supported CPU or GPU device. If the runtime is absent or cannot load, the existing built-in AOV denoiser is used instead.
