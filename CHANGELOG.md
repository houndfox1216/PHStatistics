# Changelog

## 2026-07-21 — AS（課輔）Excel 版網格輸入介面 Phase 2

新增一個比照 Excel 排版（列＝年級、欄＝科目×班別）的課輔（AS）人數表輸入頁面，跟現有清單式頁面並存；新版穩定後再取代舊版。

- **資料庫**：新增共用分析用 `CourseDepartment`（Id 46「課輔分析總覽」）與 48 筆 `Course`（Id 478–525，12 年級 × 上週比/新生/流失/總人數，跨安親/英文/數學/理化 4 科共用一組課程）。已套用本機 dev DB 與正式環境 DB，並各自獨立 SELECT 驗證。
- **一致性檢查**：`StudentPopulationController.CheckNewLostConsistency` 的 `AfterSchool` case 新增對新分析課程的差額檢查，讓新網格填寫的新生/流失/上週比也被系統一致性警示涵蓋。
- **新路由與頁面**：新增 `/StudentPopulation/CreateASGridPopulation`（首頁「課輔(新版)」入口），以及 `CreateASGridPopulation.cshtml` / `ASGridPopulationPartialView.cshtml` 兩個新 View，渲染 12 年級 × 7 個主要人數欄（安親／英文-一對一／英文-團體／數學-一對一／數學-團體／理化-一對一／理化-團體）+ 4 個分析欄（新生／流失／上週比／總人數）的網格。
- **互動**：新增 3 個網格專用 AJAX action（`AddNewClassGrid`／`RemoveClassItemGrid`／`UpdateClassItemGrid`），支援每格 onchange 即時存檔、「＋」彈窗新增班級（多班級時整欄新增重複子欄）、刪除班級；分析欄可直接編輯覆蓋（沿用既有 `IsManual`/`RevertToAutoCalculation` 機制）。既有的 `RevertToAutoCalculation`／`UpdateRemark`／`UpdateClassDetail`／`ConfirmPopulation` 直接沿用，無需修改。
- **舊頁面不受影響**：舊版清單式 `CreateASPopulation`／`ASPopulationPartialView` 完全未修改，仍讀寫舊的 4 科分開分析課程（259-294/309-344/392-427/442-477）——新舊版本分析數字自此不再同步更新，屬於設計上已知的過渡狀態，不是 bug。
- **驗證**：`dotnet build` 0 錯誤；`dotnet test` 34 項全過（無新增自動測試，依專案慣例延後到瀏覽器手動驗證）。**瀏覽器手動驗證尚未執行**（環境無瀏覽器工具），待後續人工測試 `/StudentPopulation/Index?type=ASGrid`。

Commits：`ff52ce5`（SQL）→ `d4d258d`（一致性檢查）→ `2ffd745`（路由＋唯讀網格）→ `ab162d2`（互動 action＋JS）→ `07c5258`（plan/spec 文件）

Plan：`source/portal/docs/superpowers/plans/2026-07-20-as-input-grid-phase2.md`
Spec：`source/portal/docs/superpowers/specs/2026-07-20-as-input-grid-phase2-design.md`
