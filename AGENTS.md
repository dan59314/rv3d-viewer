# Project UI Rules

- Define every fixed WinForms UI control and fixed component in the owning form or user control's `.Designer.cs` and `.resx` files.
- Do not create fixed UI controls at runtime and do not add them to a control tree from non-Designer code.
- Keep UI behavior and application logic in the non-Designer partial class; keep control construction, layout, fixed properties, and Designer event wiring in the Designer partial class.
- Do not change existing UI layout, sizing, styling, control properties, or event wiring unless the user explicitly requests that UI change.
- Do not change UI DPI behavior, `AutoScaleMode`, `AutoScaleDimensions`, scale factors, or font family/style/size unless the user explicitly requests that exact change.
- Custom controls may initialize behavior intrinsic to the control in their constructors, but the owning form must instantiate and place those controls through its Designer files.
- Runtime-generated data items that cannot exist at design time, such as plugin commands or filesystem-derived menu entries, may remain dynamic. Do not use this exception for fixed controls or fixed menu entries.

# Build and Test Rules

- When compiling or testing, use the build configuration, platform, and output path currently displayed in the Visual Studio UI as the source of truth. Do not infer or substitute another output path such as `bin\Release`.

# Git 與重大改版自動推送

本規則適用於此專案及子目錄，並保留上方 UI 與建置規則的優先約束。回覆使用繁體中文。

使用者已授權：完成重大功能改版或重要修正並通過適當驗證後，建立 Git 提交、
附註版本標籤，並推送至既有 `origin`，無需重複詢問。這是協作代理在完成任務時
執行的發布流程，不是常駐背景監控、排程或每次檔案儲存就推送。

## 重大改版的判定

- 新增或大幅改變使用者可見功能、專案格式、外掛 API、渲染流程或相機動畫格式。
- 跨多個元件的重要修正，或影響專案相容性、資料安全、效能與輸出正確性的修正。
- 純說明、調查、未完成實驗、驗證失敗及單純文件更新不建立發布標籤。

## 發布流程

1. 先檢查 `git status`、目前分支、遠端與標籤，辨識使用者原有或無關修改。
2. 依 Visual Studio 目前顯示的組態、平台與輸出路徑建置／測試；若無法確認該設定，
   不自行猜測 Release 路徑，應回報尚未完成發布驗證。
3. 檢查差異，只暫存本次任務相關檔案；不得混入無關的使用者修改。
4. 建立清楚描述最終行為的提交。優先使用既有 Git 作者；若未設定，可僅針對該次
   提交使用 `Codex <codex@localhost>`，不得修改全域 Git 作者設定。
5. 遵循語意版本：不相容的專案／外掛 API 變更提升 major；新增相容功能提升 minor；
   相容修正提升 patch。使用尚未存在的 `vMAJOR.MINOR.PATCH` 附註標籤，不移動舊標籤。
6. 執行 `git push --follow-tags`；若分支尚未追蹤，僅設定並推送目前分支。
   不得使用 `--force`、`--force-with-lease`、`--mirror` 或推送所有本機分支。
7. 比對本機與遠端提交／標籤後，回報版本、提交編號、驗證結果與遠端網址。

## 遠端與失敗處理

- 建議使用 GitHub 私人儲存庫 `https://github.com/dan59314/rv3d-viewer` 作為 `origin`。
- 不得擅自改為公開、改變遠端目的地或擴大帳號／協作者權限。
- 遠端已有新提交、登入失敗或發生衝突時，保留本機提交與標籤並回報。
  不得覆蓋遠端，也不得在未成功驗證前宣稱已備份。

## 追蹤與回復

- 遵循 `.gitignore`。追蹤原始碼、Designer／resx、方案與專案檔、測試、文件及必要設定。
- 不提交 `.vs`、`bin`、`obj`、`artifacts`、暫存下載、模型權重、第三方大型相依檔、
  教學影片、使用者專案資料、密碼、權杖或其他機密。
- 回復舊版優先使用獨立 `git worktree`，或以 `git revert` 保留歷史。
  未經使用者明確要求，不得執行 `git reset --hard` 或刪除使用者工作。
