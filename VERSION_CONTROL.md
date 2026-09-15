# 3D Viewer 版本管理建議

專案已初始化為 Git 儲存庫，建立基準提交與 `v0.1.0` 標籤，並使用私人遠端：

`https://github.com/dan59314/rv3d-viewer`

每次重大改版由根目錄 `AGENTS.md` 的發布規則建立提交、版本標籤及推送。
日常小修改可以只提交；尚未完成或驗證失敗的修改不建立發布標籤。

## 手動發布

```powershell
git status
git diff
git add <本次相關檔案>
git commit -m "說明本次改動"
git tag -a v0.2.0 -m "說明重大版本內容"
git push --follow-tags
```

## 安全查看或取回舊版

```powershell
git log --oneline --decorate -20
git tag --list
git worktree add --detach "../3D-Viewer-v0.1.0" v0.1.0
```

這會在另一個資料夾開啟舊版，不影響目前工作。若要撤銷已提交變更並保留歷史，
使用 `git revert <提交編號>`。不要將 `reset --hard` 當作日常回復方式。

Git 只備份已追蹤且已提交的檔案。教學影片、模型、第三方 runtime、使用者專案、
編譯輸出與其他 `.gitignore` 項目須另行備份；自動推送也不會備份未提交修改。
