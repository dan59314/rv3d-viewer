# 圖片轉 3D 模型 PlugIn 開發計畫

## 1. 目標與產品定位

新增 `Rv3dViewer.PlugIn_ImageTo3D` WinForms 外掛，讓使用者選擇一張主體清楚的圖片，在完全地端的環境中產生帶材質的完整 3D 模型，預覽後加入目前 Viewer 場景。

第一版定位為「單張商品／物件概念圖轉可編輯 3D 資產」，不是量測、攝影測量或 CAD 逆向工程。背面與遮蔽區域是模型推測結果，不保證尺寸或幾何真實性。

### 第一版必須做到

- 支援 PNG、JPG、JPEG、BMP、TIFF、WebP。
- 圖片與模型權重均不離開本機。
- 自動／手動去背，允許透明 PNG 直接輸入。
- 產生 GLB，至少包含封閉或可正常顯示的三角網格、UV 與 Base Color。
- 顯示處理進度、階段、耗時、取消與可理解的錯誤訊息。
- 產生後可在外掛內預覽，再以一個可復原的專案交易加入 MainForm 場景。
- 保存生成參數與後端資訊，方便重現與問題診斷。

### 已確認的產品限制

- 推論必須完全地端，不使用任何線上 API。
- 可在台灣及亞洲免費散布。
- 優先使用 C# + ONNX Runtime，不要求使用者安裝 Python。
- 以低 VRAM 裝置為目標，CPU 必須能作為保底執行路徑。

### 第一版不做

- 人物骨架、動畫、Rigging、Blend Shape。
- 文字轉 3D、場景生成或多物件自動拆分。
- 毫米級尺寸、工程曲面或可直接加工的 CAD 實體。
- 模型微調或訓練。
- 同時排程多張圖片；先確保單工作業穩定。

## 2. 模型選型

採用可替換的 `IImageTo3DBackend`，不把 UI 與特定模型綁死。依已確認限制，第一版以 **TripoSR 的 ONNX 可行性 Spike** 為唯一主線；若 Spike 未達門檻，應降低第一版材質／解析度目標或回報 No-Go，不改接線上 API。

| 後端 | 定位 | 優點 | 主要限制 | 建議 |
|---|---|---|---|---|
| TripoSR → 自行驗證的 ONNX | 第一版主線 | 官方模型卡與程式碼標示 MIT；單圖 feed-forward；神經網路推論可放進 C# ONNX Runtime | 官方交付是 PyTorch，不是官方 ONNX；Marching Cubes、分批密度查詢與色彩烘焙需自行實作 | 最符合免費散布、C# 優先及低 VRAM 限制 |
| Stable Fast 3D | 研究備選 | 單圖直接輸出 GLB、UV 與材質，官方流程約需 6 GB VRAM | Windows experimental、PyTorch/CUDA 原生相依多，Community License 有商用條件 | 不列入第一版正式散布 |
| Hunyuan3D 2.1 | 不採用 | 高解析幾何與 PBR 材質 | Community License 有地域限制，且顯存與部署成本高 | 與本版限制不符 |
| TRELLIS.2 | 不採用 | 完整 PBR、複雜拓撲品質高、MIT | 官方只測 Linux，至少 24 GB NVIDIA VRAM | 與低 VRAM 及 Windows C# 優先不符 |

### 建議決策

1. 將 TripoSR 拆成可匯出的神經網路區段，建立固定輸入 ONNX 原型；Python 只在開發／轉檔時使用，不成為產品執行需求。
2. 在 C# 先以 ONNX Runtime CPU 跑出與 PyTorch reference 接近的結果，再評估 DirectML。
3. Marching Cubes、網格清理、法線與 GLB 輸出使用 C#；density／color 查詢按區塊執行，限制峰值 VRAM/RAM。
4. 若 DirectML 不支援部分節點，優先調整 ONNX graph；不可默默產生大量 CPU/GPU 資料搬移。
5. 正式散布前仍需盤點模型、轉檔工具及執行期相依授權，隨外掛附 MIT 授權文字及第三方聲明。

官方參考：

- Stable Fast 3D: https://github.com/Stability-AI/stable-fast-3d
- TripoSR: https://github.com/VAST-AI-Research/TripoSR
- Hunyuan3D 2.1: https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1
- TRELLIS.2: https://github.com/microsoft/TRELLIS.2

## 3. 系統架構

```text
ImageTo3DForm (WinForms / Viewer 行程)
  ├─ ImagePreprocessor
  ├─ GenerationController
  │    └─ TripoSrOnnxBackend
  │         ├─ ONNX Runtime CPU（保底）
  │         ├─ DirectML EP（通過測試後啟用）
  │         ├─ BatchedFieldSampler
  │         ├─ MarchingCubesMesher
  │         └─ MeshColorBaker
  ├─ GLB 驗證與匯入
  ├─ OpenGL 暫存預覽
  └─ IPluginContext 專案交易／資產保存
```

### 執行方式

- 第一版正式路徑為 C# `Microsoft.ML.OnnxRuntime`，不啟動 Python，也不要求 CUDA。
- CPU 是相容性基線；DirectML 是加速選項，可涵蓋支援 DirectX 12 的 NVIDIA、AMD 與 Intel GPU。
- 推論在背景工作執行，`InferenceSession`、Tensor 與大型 buffer 都有明確生命週期，完成、取消或失敗後釋放。
- 若 ONNX Runtime 在 Viewer 行程內造成不可接受的原生 DLL 或記憶體風險，再將同一套 C# backend 搬到獨立 .NET Worker。

### 可選的 .NET Worker 通訊方式

只有需要行程隔離時才使用標準輸入／輸出的 JSON Lines，不開 HTTP 埠：

- 請求：`jobId`、輸入圖片絕對路徑、輸出資料夾、後端、seed、品質、貼圖解析度、去背選項。
- 事件：`started`、`progress`、`preview`、`completed`、`cancelled`、`failed`。
- `stdout` 只允許協定 JSON；第三方套件輸出全部導向 `stderr` 與每次工作的 log。
- 每列包含 `protocolVersion`，未知版本必須拒絕而不是猜測。
- 路徑由 C# 建立並限制在該次工作資料夾；Worker 不接受任意輸出路徑。

## 4. 專案與檔案配置

```text
plugins/Rv3dViewer.PlugIn_ImageTo3D/
  plugin.json
  Rv3dViewer.PlugIn_ImageTo3D.csproj
  ImageTo3DPlugin.cs
  ImageTo3DForm.cs
  ImageTo3DForm.Designer.cs
  ImageTo3DForm.resx
  ImageTo3DPreviewControl.cs
  ImageTo3DPreviewControl.Designer.cs
  ImageTo3DPreviewControl.resx
  GenerationController.cs
  TripoSrOnnxBackend.cs
  BatchedFieldSampler.cs
  MarchingCubesMesher.cs
  MeshColorBaker.cs
  ModelPackageService.cs
  GeneratedAssetImporter.cs
  ImageTo3DSettings.cs
  THIRD-PARTY-NOTICES.md
  Models/
    model-manifest.json
  tools/
    export_triposr_onnx.py   # 僅供開發建置，不隨產品執行
```

原始 PyTorch 權重不要提交 Git。正式散布物只包含通過測試的 ONNX 模型、manifest 與授權檔；大型模型可做成額外的免費離線安裝包。預設放在：

```text
%LOCALAPPDATA%/Rv3dViewer/ImageTo3D/
  models/<backend>/<model-version>/
  cache/
  logs/
```

`model-manifest.json` 必須記錄檔名、版本、來源、SHA-256、大小、授權檔、ONNX opset、輸入輸出 contract 與所需 PlugIn 版本。安裝後先驗證雜湊，再用原子改名啟用，避免半套模型被誤用。

## 5. UI 規劃

新增 Designer 管理的 `ImageTo3DForm`，所有固定控制項、元件、版面、固定屬性與事件綁定放在 `.Designer.cs`／`.resx`。不修改 MainForm 現有版面、DPI、字型或樣式。

### 視窗結構

- 左側「來源與參數」：
  - 載入圖片、拖放區、來源縮圖。
  - 背景處理：使用 Alpha／自動去背／不去背；背景色與邊緣收縮選項。
  - 後端與品質：快速／標準／精細；Seed；貼圖解析度；目標面數。
  - 生成、取消、重新生成。
  - 模型狀態：未安裝／可用／版本不符；安裝或開啟模型資料夾。
- 右側 Tab：
  - 「輸入預覽」：實際送入模型的 RGBA 圖。
  - 「3D 預覽」：Designer 內放置預覽控制項，操作沿用 Viewer 的 Orbit、Pan、Zoom。
  - 「工作紀錄」：目前階段、進度、耗時、Execution Provider、顯存不足或 ONNX 訊息。
- 下方：加入場景、另存 GLB、關閉。

### 操作狀態

`NoImage → Ready → Preparing → GeneratingShape → GeneratingTexture → PostProcessing → PreviewReady`

另有 `Cancelling`、`Cancelled`、`Failed`。生成中鎖定會改變輸入的控制項；取消後恢復到 `Ready`，已成功的上一版預覽仍保留。

## 6. 生成流程

1. 載入圖片並套用 EXIF 方向；驗證格式、尺寸、像素數與 Alpha。
2. 在 C# 端建立顯示用縮圖；實際 AI 輸入也由 C# 做固定且可版本化的預處理。
3. 執行去背：透明圖保留 Alpha；非透明圖可沿用 Relief PlugIn 的 MODNet 概念，但模型與程式封裝成 ImageTo3D 自己的服務，避免跨 PlugIn DLL 相依。
4. 將物件依遮罩裁切、保留安全邊界、置中成正方形 RGBA PNG。
5. C# backend 以 CPU 或 DirectML 載入 ONNX；先算影像條件特徵，再分批查詢 3D 場的 density／color，避免一次配置完整高解析 Tensor。
6. C# Marching Cubes 從 density field 建立表面，接著移除退化面、合併近似頂點、計算法線並做基本貼色／紋理烘焙。
7. 輸出 `model.glb`、`result.json`、縮圖與 log。`result.json` 記錄 ONNX SHA-256、Execution Provider、取樣解析度、批次大小、耗時與警告。
8. C# 驗證檔案存在、大小上限、GLB header、頂點／索引有限值、材質與紋理可解析。
9. 將 GLB 匯入成 `SceneModel`，統一成右手座標、Y-up，置中地面並依合理包圍盒縮放。
10. 在外掛的暫存 `ViewerProject` 顯示模型。
11. 使用者按「加入場景」時，先 `StageAsset(..., Model)`，再於 `BeginProjectEdit(...)` 交易內加入模型並提交；失敗則回復，不能留下半成品。

## 7. 與現有 Viewer 的整合決策

目前 `IPluginContext` 沒有公開模型匯入服務，而 `AssimpModelImporter` 位於 App 且是 `internal`。新外掛不應照現有室內設計 PlugIn 以反射取得 internal 類型。

建議在實作前先做一個小型 Host API 改良：

- 新增 `IPluginModelImportService`，公開 `SupportedExtensions` 與 `ImportAsync`。
- `IPluginContext` 提供該服務；MainForm 仍由現有 `AssimpModelImporter` 實作。
- 將 `PluginApi.Version` 提升為 `1.1`，保留同 major 的相容規則並補相容性測試。
- 若二進位相容性測試證明直接增加介面成員會影響舊 PlugIn，改以新衍生能力介面 `IPluginContextV11`，外掛用型別檢查取得，沒有時顯示明確的 Viewer 版本需求。

此改良可避免複製匯入器、反射與 Assimp 原生相依衝突，往後其他 PlugIn 也能安全匯入模型。

## 8. 安裝與部署

第一版採「外掛 + ONNX 模型」離線散布，可拆成兩個免費安裝包：

- PlugIn 本體帶 C# DLL、ONNX Runtime 原生檔、模型管理器與授權清單。
- 安裝器顯示下載大小、目標磁碟空間、模型授權與硬體檢查結果。
- 正式執行完全不依賴 Python、PyTorch、CUDA Toolkit 或使用者全域環境。
- 安裝與生成均可取消；中斷後能清理暫存檔並重新開始。
- 正式離線部署可另提供經過雜湊驗證的完整模型包，透過「從本機套件安裝」匯入。
- 不在 UI 執行緒解壓縮、雜湊大型檔案或等待 Worker。

硬體檢查至少包含 Windows x64、RAM、可用磁碟、DirectX 12 adapter、Dedicated/Shared memory 與 ONNX Runtime 實際可建立的 Execution Provider。不要只依顯卡名稱推測能力。

## 9. 安全性與可靠性

- 若啟用獨立 .NET Worker，只接受結構化參數，不拼接 Shell 命令；所有路徑用 `ArgumentList` 傳入。
- 輸入圖先檢查副檔名、實際解碼、像素上限與檔案大小，防止解壓炸彈。
- GLB 匯入前設檔案大小、節點數、材質數、頂點與三角形上限。
- 每次生成用獨立 GUID 工作目錄；啟動時清理超過保留期且不在使用中的暫存工作。
- 不把使用者路徑、圖片或模型內容寫進一般遙測；本地 log 可由使用者主動開啟資料夾。
- ONNX Session 建立、模型載入與雜湊失敗都要有可復原錯誤；Viewer 不因 AI 失敗而退出。
- 避免自動重試顯存不足；改為建議降低品質／貼圖解析度，防止反覆耗時。

## 10. 測試計畫

### C# 單元測試

- ONNX 輸入輸出 contract、模型版本與不支援 opset。
- 狀態機、取消、Session 建立失敗、逾時與錯誤映射。
- 模型 manifest、SHA-256、版本不符、磁碟空間不足。
- 圖片方向、Alpha、裁切、尺寸與像素限制。
- GLB header、檔案大小及幾何資料防護。
- 加入場景的交易成功、取消、失敗回復與重複加入防護。
- PlugIn manifest 掃描與 Host API 1.0/1.1 相容行為。
- Designer 完整性：固定控制項全部位於 Designer，名稱與 Controls.Add 不重複。

### ONNX backend 測試

- 使用固定小圖的 smoke test；檢查 GLB 可開啟且包含網格。
- 同 seed 的可重現程度與不同品質參數是否生效。
- CPU／DirectML 選擇、GPU 不存在、顯存不足、模型缺失與取消。
- ONNX 每個關鍵輸出與 PyTorch reference Tensor 的誤差門檻。
- 64／96／128／192 等場解析度及不同查詢 batch size 的峰值 RAM、VRAM 與耗時。
- 輸入帶 Alpha、白底、複雜背景、細腳／孔洞與反光物件。
- 結果限制：NaN／Infinity、空網格、退化面、極端包圍盒、紋理缺失。

### 固定驗收資料集

準備至少 10 張可合法重散布的圖片：椅子、鞋、杯子、玩具、植物、金屬物、透明／半透明物、細長結構、正面不對稱物及失敗案例。保留同版本輸出縮圖、三角形數、耗時與峰值顯存作回歸比較，但不要以像素完全相同作唯一判定。

### 手動驗收

- 依 Visual Studio 畫面當下選定的 Configuration、Platform 與 Output Path 執行 Build／Test，不推測 `bin\Release`。
- 驗證生成期間 Viewer 仍能重繪，視窗可取消且不假死。
- 驗證成功加入、取消加入、專案切換、關閉視窗及 Viewer 結束時 Session 都能正確收尾。
- 儲存並重新載入 `.rv3dproj`，確認 GLB 與內嵌材質仍完整。
- 在另一台乾淨 Windows 測試首次安裝、離線模型包與移除流程。

## 11. 分階段里程碑

### M0：ONNX 技術 Spike（約 5～10 個工作天）

- 以官方 TripoSR PyTorch 為 reference，找出可穩定匯出的模型邊界。
- 匯出 ONNX、通過 checker，並以固定 Tensor 比對 PyTorch／ONNX Runtime CPU 數值。
- C# 跑通「圖片 → latent → 分批 density → Marching Cubes → GLB」最小流程。
- 在無獨顯、2 GB 與 4 GB VRAM 級別機器量測記憶體與時間；未實機驗證的硬體不得宣稱支援。
- 產出 Go／No-Go：最低 RAM/VRAM、場解析度、是否啟用 DirectML，以及第一版是否只能提供基本貼色。

### M1：垂直切片（約 5～7 個工作天）

- 建立 PlugIn、Designer 表單、C# ONNX backend 與單一 TripoSR 模型。
- 完成「選圖 → 生成 GLB → 匯入 → 暫存預覽 → 加入場景」。
- 先使用固定 ONNX 開發模型，不做完整安裝器。

### M2：可靠性與產品化（約 7～10 個工作天）

- 模型管理、離線模型包、雜湊與版本檢查。
- 進度、取消、log、錯誤分類、GPU／磁碟檢查與暫存清理。
- 去背、品質預設、另存 GLB 與設定保存。

### M3：品質與回歸（約 5～7 個工作天）

- 完成單元／整合／Designer 測試與固定驗收資料集。
- GLB 防護、專案儲存重載、乾淨機器安裝測試。
- 補齊第三方聲明、使用限制與使用者文件。

### M4：品質提升（另估）

- 改善 UV 展開、投影貼圖、背面補色、網格簡化與孔洞修補。
- 只有新後端同時符合免費散布、亞洲授權、C#/ONNX 與低 VRAM 門檻時才納入。

單人第一版估計約 5～8 週，最大變數是 TripoSR ONNX 匯出後的算子相容性、低記憶體場取樣與貼色品質，而不是 WinForms UI。

## 12. 完成定義

- 在已公布的最低規格 Windows x64 機器上，10 張驗收圖至少 9 張能產生可載入 GLB；失敗案例會清楚說明且 Viewer 不崩潰。
- 取消在合理時間內生效，Session／Tensor 釋放後顯存被回收。
- 整個生成流程可離線執行，工作期間沒有外部網路請求。
- 輸出模型加入場景後可正常選取、移動、顯示材質、儲存及重新載入。
- 所有固定 UI 符合 Designer／resx 規則，沒有改動既有 MainForm 版面或 DPI 行為。
- 模型、程式碼與相依套件的版本、雜湊及授權資料齊全。
- 完整測試通過，並以 Visual Studio 畫面中的實際組態、平台及輸出路徑完成最終驗收。

## 13. 實作前需要鎖定的三個產品決策

1. 最低支援的 Windows 版本、系統 RAM 與可接受的 CPU 最長等待時間。
2. 「低配」驗收門檻定義為無獨顯、2 GB VRAM 或 4 GB VRAM；建議至少選定其中兩級實機。
3. 第一版是否接受「幾何完整、基本貼色，但不是完整 PBR」；若不能接受，這組限制下的技術風險與工期會顯著增加。
