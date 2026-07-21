# Changelog

## 2026-07-22 — AS 網格版分析欄位批次補檔（正式環境資料修復）

使用者反映正式環境福山 115 年第 1 週課輔網格版頁面，新生/流失等「分析」欄位沒有出現輸入格。查證後發現不是單一分校/週次的問題：除了福山 115 年第 2 週（population 2383，先前手動驗證 Phase 2 時剛好開過頁觸發過補檔）之外，其餘全部 42 張既有 AS 人數表（福山／農十六／東湖／新東湖／內湖／大安／岡山共 7 所分校，橫跨 114 年第 17 週～115 年第 3 週）都完全缺少這 48 筆分析項目（Course 478–525）。

- **根本原因**：Phase 2 的補檔邏輯（`CreateASGridPopulation` action 內，commit `ca50000`）只在使用者實際打開該分校該週的網格頁面時才觸發（GET on-demand），不會一次套用到所有既有資料——這是 Phase 2 設計時的既定行為（不自動建立空白預設列），但代表幾乎所有既有人數表都要等被人點開才會補齊分析欄位。
- **修復方式**：撰寫一次性 SQL 批次腳本直接補齊正式環境資料，不修改任何程式碼。計算邏輯完全比照 `AggregationEngine.Calculate`：
  - 上週比（Course 478–489，`StatisticsType=10` 與上週相比）：`SUM(Number) − SUM(LastWeekNumber)`，來源課程走 `SourceCourseIds`（同年級跨安親/英文/數學/理化 4 科，不分班別）
  - 新生／流失（Course 490–513，`StatisticsType=50` 手動輸入）：維持 0，等分校人工填寫（本來就是人工輸入項目，非 bug）
  - 總人數（Course 514–525，`StatisticsType=4` 指定來源課程加總）：`SUM(Number)`
  - 福山以外的 6 所分校連對應的 `Class` 記錄都不存在，一併新增 288 筆
- **驗證**：套用前先用 SELECT-only 方式 dry-run 驗證計算邏輯（手算比對福山第 1 週實際資料），套用後獨立 SELECT 確認全部 43 張 AS 人數表分析項目皆為 48 筆且數值正確。
- **待辦**：福山 115 年第 1 週頁面待使用者實際以瀏覽器確認畫面顯示正常（環境無瀏覽器工具）。

Commit：`80ba9c4`

Script：`source/portal/docs/superpowers/sql/2026-07-22-as-grid-analysis-item-backfill-apply.sql`（+ revert）

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
