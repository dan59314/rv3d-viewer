# Video2Skybox / Video2Cubemap Faces

輸入一般原地旋轉拍攝的 MP4，抽影格並用 OpenCV 球面拼接，輸出 2:1 全景畫布和 6 面 Cubemap。**不含 AI 生圖、不需 API Key、不能重建影片沒拍到的場景。** 本工具沒有修改 3D Viewer 主程式。

## 最簡單用法

1. 電腦需有 Python 3.10 以上。第一次執行 `Setup.bat`，它會建立工具專用 `.venv` 並從網路安裝 NumPy、OpenCV。
2. 將 MP4 拖到 `Video2Skybox.bat`，或雙擊 BAT 後輸入影片完整路徑。
3. 輸入影片成功拼接區域的大約水平角度。完整一圈才填 360；預設 180 只是明確的假設，不是自動量測結果。
4. 結果放在 `output\影片名稱_時間戳\`，每次建立新資料夾，不覆寫原圖。

BAT 優先使用 `.venv`；尚未安裝時嘗試 PATH 中的 Python。MP4 解碼使用 OpenCV，無須另外安裝 FFmpeg。

預設取樣 16 幀。標準拼接失敗時會嘗試 SIFT 特徵、球面投影及重投影相機校正；兩者皆失敗就停止，不輸出假成功。

## 輸出

| 檔案 | 用途 |
|---|---|
| `panorama.png` | 4096 × 2048 全景畫布，LDR PNG |
| `faces/right.png` | +X |
| `faces/left.png` | -X |
| `faces/top.png` | +Y |
| `faces/bottom.png` | -Y |
| `faces/front.png` | +Z |
| `faces/back.png` | -Z |
| `panorama_coverage.png` | 白色＝有來源畫面；黑色＝缺失 |
| `coverage/*.png` | 每一面的來源有效遮罩 |
| `stitched_strip.png` | 未強制拉伸到全景比例的拼接條帶，請先檢查是否拼錯 |
| `cubemap_preview.jpg` | 六面十字預覽；含面名文字，不應當作正式貼圖 |
| `manifest.json` | 尺寸、角度假設、方向定義、缺失比例及警告 |
| `frames/` | 抽取影格，失敗時也保留供檢查 |

六面預設各 1024 × 1024。座標為 +Y 向上，正面 +Z；全景正中為正面，左右方向由拼接結果決定，不代表東西南北。不同引擎可能使用 -Z 為正面或不同上下圖旋轉方式，請以各引擎的 Cubemap 規則載入，不保證單靠檔名即可通用。

## 重要限制

- 一段只轉了半圈、沒有拍到天空正上方和腳下的影片，不可能產生完整實拍 Skybox。預設缺失處留黑，遮罩明確標記。
- `--fill edge` 用條帶邊緣像素延伸缺失方向，不是 AI 補景；可能形成條紋。遮罩仍標記為缺失。
- 普通旋轉影片沒有已知鏡頭校正與視角，所以水平角由使用者提供，垂直視角依球面條帶比例推算；地平線預設在條帶高度 50%。不是嚴格標定的全景攝影測量。
- OpenCV 可能只拼接互相匹配的一部分影格；請先看 `stitched_strip.png`，再調整 `--coverage`。填 360 不會自動恢復未拍到的半圈。
- 360 度左右接縫未進行閉環校正；曝光變化、視差、移動車輛、樹葉及重複建築可能造成重影或錯拼。
- 低解析度影片輸出 4K 只是插值，不會增加真實細節。PNG 是 LDR，不是 HDR。
- 影片太模糊或重疊不足時會失敗，回傳非零結束碼並寫 `error.txt`，不會將任意拼貼假裝成成功全景。

## 進階執行（命令提示字元）

```bat
python video_to_skybox.py "D:\影片\rotate.mp4" --coverage 150 --frames 24 --width 4096 --face-size 1024
```

調整地平線位置、以邊緣延伸補黑：

```bat
python video_to_skybox.py "D:\影片\rotate.mp4" --coverage 150 --horizon 0.48 --fill edge
```

如果 MP4 本身已經是正確的 2:1 全景影片，可跳過拼接，取第一幀轉換：

```bat
python video_to_skybox.py "D:\影片\already_360.mp4" --mode equirect --max-frame-width 4096
```

此模式不能用於一般 16:9 手機影片。

## 測試

```bat
python -m unittest test_video_to_skybox.py -v
```

測試六面中心方向，以及缺失區域與有效遮罩。`test_output` 如有內容，是開發時用使用者提供的影片所做的示範；150 度是測試假設，不是已量測角度。

已用基地的 9.9 秒影片實測：16 幀、抽樣寬 960、全景 2048 × 1024、各面 512。第二種 SIFT 流程成功產出；拼接條帶仍有輕微重影與不規則邊緣。測試角度假設下，有效像素約占完整球面畫布 5.3%，不是完整實拍 360 度。測試資料夾也保留了早期失敗紀錄。
