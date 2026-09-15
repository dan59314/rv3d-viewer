# Third-party notices

## MODNet

- Purpose: offline full-body subject segmentation and alpha matting
- Model: MODNet photographic portrait matting
- Model SHA-256: `07C308CF0FC7E6E8B2065A12ED7FC07E1DE8FEBB7DC7839D7B7F15DD66584DF9`
- License: Apache License 2.0
- Source: https://github.com/ZHKKKe/MODNet
- ONNX artifact: https://github.com/Zeyi-Lin/HivisionIDPhotos/releases/download/pretrained-model/modnet_photographic_portrait_matting.onnx

## Depth Anything V2 Small

- Project: Depth Anything V2
- Upstream model: `depth-anything/Depth-Anything-V2-Small`
- ONNX conversion: `onnx-community/depth-anything-v2-small`
- Model file: `model_uint8.onnx`
- Model SHA-256: `fcf51f1b230362b28690bb9d1809bf0431f29cad20534e3f589bd7285547f20d`
- License: Apache License 2.0
- Sources:
  - https://github.com/DepthAnything/Depth-Anything-V2
  - https://huggingface.co/depth-anything/Depth-Anything-V2-Small
  - https://huggingface.co/onnx-community/depth-anything-v2-small

The model is used locally to estimate relative monocular depth. Its output is not a metric measurement and does not represent millimetres.

## BiSeNet Face Parsing ResNet18

- Project: face-parsing
- Model file: `resnet18.onnx`
- Model SHA-256: `0d9bd318e46987c3bdbfacae9e2c0f461cae1c6ac6ea6d43bbe541a91727e33f`
- License: MIT
- Source: https://github.com/yakhyo/face-parsing
- Model release: https://github.com/yakhyo/face-parsing/releases/tag/weights

The model runs locally and segments 19 portrait regions, including skin, eyebrows, eyes, glasses, ears, nose, lips, hair, and neck.

## ONNX Runtime

- Project: Microsoft ONNX Runtime
- Package: `Microsoft.ML.OnnxRuntime`
- License: MIT
- Source: https://github.com/microsoft/onnxruntime

## SkiaSharp

- Project: SkiaSharp
- Packages: `SkiaSharp`, `SkiaSharp.NativeAssets.Win32`
- License: MIT
- Source: https://github.com/mono/SkiaSharp
