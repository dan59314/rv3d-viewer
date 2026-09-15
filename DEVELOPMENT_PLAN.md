# C# 3D Viewer 開發計畫

## 摘要

建立 Windows x64、.NET 8 WinForms 應用程式，使用 Visual Studio DesignTime Designer 設計 UI，並以 OpenGL 單一後端提供：

- 匯入 STL、OBJ/MTL、3DS。
- 多模型場景組合、選取、刪除、顯示控制及位置／旋轉／縮放調整。
- Orbit 相機操作及 Camera From、To、FOV 精確編輯。
- Metallic-Roughness PBR 材質與貼圖即時編輯預覽。
- 專案資料夾保存模型、貼圖、材質覆寫、場景及相機設定。

預估一位熟悉 C#/OpenGL 的工程師約需 20–25 個工作天。

## 架構與技術

- 建立 `Rv3dViewer.sln`，分成：
  - `Rv3dViewer.App`：WinForms UI、Designer、命令與資料繫結。
  - `Rv3dViewer.Core`：場景、相機、材質、匯入、專案序列化及渲染介面。
  - `Rv3dViewer.Tests`：匯入、資料模型與專案檔測試。
- 使用 .NET 8、OpenTK 4.9.4、OpenTK.GLControl 4.0.2；GLControl 在執行期掛入 Designer 建立的 Viewport Panel，避免 Designer 建立 OpenGL Context 的問題。
- 使用 AssimpNetter 6.0.5 統一讀取 STL、OBJ、3DS，取代範例中功能有限的自製 OBJ Parser。
- OpenGL 3.3 Core Profile；採 VAO/VBO/EBO、Depth Test、Back-face Culling、透明材質混合及 GLSL PBR Shader。
- 參考專案僅重用其 OpenTK GLControl 生命週期、座標換算、相機環繞概念及 Shader 結構；不沿用 SharpDX、雙後端、全域靜態狀態、動態建立 UI 和固定六張貼圖的設計。

套件參考：

- [OpenTK](https://www.nuget.org/packages/OpenTK/)
- [OpenTK.GLControl](https://www.nuget.org/packages/OpenTK.GLControl/)
- [AssimpNetter](https://www.nuget.org/packages/AssimpNetter/)

## 核心介面與資料結構

- `IModelImporter.ImportAsync(path, cancellationToken)`：將來源檔轉為包含節點、Mesh、材質與貼圖參考的 `ImportedModel`。
- `IRenderer`：提供初始化、調整尺寸、同步場景、上傳/釋放 GPU 資源及繪製功能。
- `ViewerProject`：
  - 格式版本、場景模型集合、Camera、RenderSettings。
  - 每個 `SceneModel` 保存 ID、名稱、資產路徑、可見性、節點/Mesh、TRS Transform 及材質集合。
- `PbrMaterial`：
  - BaseColor、Metallic、Roughness、NormalScale、AO、Emissive、EmissiveStrength、Opacity。
  - BaseColor、Metallic、Roughness、Normal、AO、Emissive、Opacity 貼圖槽。
  - 每個貼圖槽保存相對路徑、啟用狀態、UV Channel、Wrap 與 Filter 設定。
- `CameraState`：From、To、Up、FOV、Near、Far；驗證 From 不可等於 To，FOV 限制為 5–120 度。
- OBJ/3DS 傳統材質轉換為 PBR；Diffuse 映射 BaseColor、Emission 與 Opacity 直接映射，Specular/Shininess 轉為合理的 Metallic/Roughness 初值。STL 沒有材質或 UV 時套用預設灰色材質並停用貼圖。

## UI 與操作

- Designer 主畫面包含：
  - 上方 MenuStrip/ToolStrip：新增模型、移除、開啟專案、儲存、另存新檔、重設相機。
  - 左側 Scene Tree：顯示多個來源模型及內部節點/Mesh。
  - 中央 OpenGL Viewport：模型、網格地面、背景與選取提示。
  - 右側分頁：Transform、Material、Camera。
  - 下方 StatusStrip：模型格式、三角形數、載入進度、FPS 與錯誤訊息。
- 左鍵拖曳繞 Camera To 旋轉；中鍵拖曳同時平移 From/To；滾輪沿視線方向 Dolly。
- Camera 分頁可直接輸入 From、To、FOV；提供 Front、Back、Left、Right、Top、Bottom、Perspective 與 Frame Selected。
- 場景樹選取模型後，可調整 Position、Rotation、Scale、Visible；材質分頁依 Mesh 材質槽切換。
- 材質數值、顏色或貼圖變更後立即更新 GPU 並預覽；貼圖不存在或載入失敗時顯示警告並使用預設貼圖，不使程式終止。
- 大型模型在背景執行解析；GPU 上傳回到 GL 執行緒，並提供進度與取消功能。

## 專案保存與資產管理

- 專案副檔名採 `.rv3dproj`，內容為具版本欄位的 JSON。
- 專案旁建立 `Assets/Models`、`Assets/Textures`，新增模型時複製來源檔、MTL 及引用貼圖。
- JSON 僅保存相對路徑；同名資產以雜湊後綴避免覆蓋。
- 開啟專案時重建場景、材質覆寫與相機；缺少資產時保留場景節點、列出缺檔清單並允許重新指定。
- 第一版不回寫來源模型、不匯出新模型格式、不提供 Undo/Redo 或節點父子關係重組。

## 里程碑

### 1. 基礎架構與技術驗證（3 天）

- 建立 solution、Designer 主窗體、GLControl Host。
- 驗證 OpenGL、Assimp native library 及 x64 發佈部署。

### 2. 匯入與場景資料層（4–5 天）

- 實作 STL、OBJ/MTL、3DS 匯入、座標/單位正規化及多模型管理。
- 建立 Mesh、Node、Transform、Material、Texture 資料模型。

### 3. 渲染與相機（5–6 天）

- 完成 GPU 資源管理、場景繪製、Orbit/Pan/Dolly、From/To/FOV 及 Frame Selected。
- 加入網格、燈光、透明混合與錯誤回復。

### 4. PBR 材質編輯器（5–6 天）

- 完成 Metallic-Roughness Shader、所有材質欄位、貼圖選擇與即時預覽。
- 處理 sRGB 色彩貼圖與 Linear 資料貼圖。

### 5. 專案檔、測試與交付（3–5 天）

- 完成資產複製、JSON 保存/載入、缺檔修復、範例模型及 Release 發佈。

## 測試與驗收

- ASCII/Binary STL、含 MTL/多材質/外部貼圖的 OBJ、含材質與貼圖的 3DS 均可載入。
- 同一場景可加入多個模型，TRS、可見性、刪除及選取互不影響。
- 滑鼠 Orbit/Pan/Dolly 與 From/To/FOV 欄位雙向同步；Frame Selected 可完整框住選取模型。
- 每項 PBR 參數與貼圖切換都能立即改變預覽，Normal/AO/Metallic/Roughness 貼圖以 Linear 色彩處理。
- 儲存後搬移整個專案資料夾，仍可完整重開；缺少貼圖時不崩潰並顯示明確訊息。
- 重複加入、移除及重載模型不殘留 GPU 資源；視窗縮放、最小化與 OpenGL Context 銷毀安全。
- Visual Studio Designer 可正常開啟 MainForm，所有一般 UI 元件均能在 DesignTime 編輯。
- Debug/Release x64 建置及乾淨機器發佈驗證通過。

## 假設與預設

- 目標環境為 Windows 10/11 x64、Visual Studio 2022、.NET 8。
- 第一版正式支援 STL、OBJ/MTL、3DS；FBX、glTF/GLB、DAE、PLY 留作後續擴充。
- 採右手座標系、Y 軸向上，匯入時統一三角化並保留來源節點階層。
- 預設採三點式燈光與深灰背景，不包含 HDR Environment、動畫、骨架、陰影、模型輸出及碰撞功能。
