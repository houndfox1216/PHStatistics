# AS（課輔）Excel 版網格輸入介面 Design Spec — Phase 2

## 背景

使用者反映課輔班（AS）人數表的網頁輸入介面（`CreateASPopulation`／清單式）跟分校原本使用的 Excel（`百瀚全區課輔人數總表`）版面差異太大。Phase 1（已完成並上線，`docs/superpowers/specs/2026-07-20-as-math-science-course-model-design.md`）補齊了系統原本完全缺失的數學班／理化班資料模型，讓匯入/匯出/合計能正確涵蓋這兩科。

Phase 2 是這次真正的原始訴求：做一個新的、比照 Excel 排版（列＝年級、欄＝科目×班別）的輸入介面，跟現有清單式頁面並存，新版穩定後再取代舊版。

## 決策摘要

| 項目 | 決定 |
|---|---|
| 範圍 | 僅 AS 課輔班；PH/GEPT/PS/PSJ 不動 |
| 新舊關係 | 新增獨立新頁面/新路由，不在現有 `CreateASPopulation` 加切換鈕；新舊並存，新版穩定後將取代舊版 |
| 進入點 | 首頁 `Home/Index.cshtml` 新增「課輔(新版)」項目（與現有「課輔」平行），連到 `/StudentPopulation/Index?type=ASGrid`；現有「課輔」項目不動，仍走舊流程 |
| 網格範圍 | 只顯示本週（單週網格），比照現有系統一個 `StudentPopulation` 對應一週的資料模型 |
| 網格結構 | 列＝12 年級；欄＝7 個主要人數欄（安親／英文-一對一／英文-團體／數學-一對一／數學-團體／理化-一對一／理化-團體）＋4 個分析欄（新生／流失／上週比／總人數） |
| 多班級支援 | 每個主要人數欄格子右下角「＋」→ 彈窗新增班級名稱＋人數 → 送出後在該欄位置整欄新增一個重複欄（如「數學-一對一(2)」），其餘年級該欄留空 |
| 分析欄互動 | 新生/流失/上週比/總人數皆可在網格直接編輯覆蓋，比照既有 `IsManual`/`RevertToAutoCalculation` 機制 |
| 分析欄資料模型 | **改為每年級一組共用**（跨安親/英文/數學/理化 4 科），取代 Phase 1 沿用的「4 科分開存」方案——因為使用者要求可直接編輯覆蓋，4 科分開儲存無法對應「編輯一個加總後的數字該寫回哪一科」的問題 |
| 儲存方式 | 每格 onchange 即時 AJAX 儲存，比照 `valueChange`/`lastWeekValueChange` 既有模式，不做整頁一次性送出 |
| 狀態流程 | 沿用既有 `StudentPopulationStatus`（建檔中/已送出/已審核/已否決/已完成）與「確認送出」機制，不設計新狀態機 |
| 上週人數編輯權限 | 沿用既有 `ViewBag.CanEditLastWeek` 判斷，行為與舊頁面一致 |
| 資料庫異動範圍 | 本機 dev DB 與正式環境 DB 都要套用（比照 Phase 1 做法） |

## 現況資料調查（延續 Phase 1 的調查結果）

Phase 1 完成後，`Course` 最大 Id = 477，`CourseDepartment` 最大 Id = 45。

AS 目前的資料形狀（Phase 1 之後）：

| 班系 | Id | 內容 |
|---|---|---|
| 安親課輔班班 | 33/34/35 | 12 年級 + 2 合計 + 36 分析（上週比/新生/流失 各12） |
| 英文班 | 36/37/38 | 同上 |
| 數學班 | 40/41/42 | 同上（Phase 1 新增） |
| 理化班 | 43/44/45 | 同上（Phase 1 新增） |

`AggregationEngine.Compute`（`Portal/Services/Aggregation/AggregationEngine.cs`）的 `GetSourceItems` 篩選邏輯確認：`SourceCourseIds` 用 `ids.Contains(courseId)` 比對，**本來就支援陣列裡有多個課程 Id**，`DiffWithLastWeek`/`SumBySourceCourses` 會直接加總所有符合的來源項目——這代表「一個課程同時加總安親+英文+數學+理化 4 科」不需要改任何計算程式碼，只要 Course 資料本身設定正確的 `SourceCourseIds` 陣列即可。

## 詳細設計

### 1. 新增 `CourseDepartment`（Id 46）

| Id | Name | IsSum | Published | Type |
|---|---|---|---|---|
| 46 | 課輔分析總覽 | 1 | 0 | 4 |

### 2. 新增 `Course`（Id 478–525，共 48 筆，`Ordinal=Id-1`，`Type=4`，`GroupByClassType=0`）

12 年級 × 4 項，`SourceCourseIds` 對照表（安親/英文/數學/理化 4 科的年級課程 Id）：

| 年級 | 安親 | 英文 | 數學 | 理化 |
|---|---|---|---|---|
| 一年級 | 245 | 295 | 378 | 428 |
| 二年級 | 246 | 296 | 379 | 429 |
| 三年級 | 247 | 297 | 380 | 430 |
| 四年級 | 248 | 298 | 381 | 431 |
| 五年級 | 249 | 299 | 382 | 432 |
| 六年級 | 250 | 300 | 383 | 433 |
| 國一 | 251 | 301 | 384 | 434 |
| 國二 | 252 | 302 | 385 | 435 |
| 國三 | 253 | 303 | 386 | 436 |
| 高一 | 254 | 304 | 387 | 437 |
| 高二 | 255 | 305 | 388 | 438 |
| 高三 | 256 | 306 | 389 | 439 |

（英文一對一/團體共用同一個課程 Id，靠 `Class.Type` 區分，所以來源陣列只需列一次英文的年級課程 Id，兩種班別都會被加總進去——跟 Phase 1 的 `SourceDepartmentIds` 用法邏輯一致。）

| Id 範圍 | 內容 | StatisticsType | SourceCourseIds（以一年級為例） |
|---|---|---|---|
| 478–489 | 本週{年級}與上週相比×12 | `DiffWithLastWeek`(10) | `[245,295,378,428]` |
| 490–501 | 本週{年級}新生人數×12 | `ManualInput`(50) | — |
| 502–513 | 本週{年級}流失人數×12 | `ManualInput`(50) | — |
| 514–525 | 本週{年級}總人數×12 | `SumBySourceCourses`(4) | `[245,295,378,428]` |

命名不加科目字樣（跟安親/英文現有的「本週{年級}與上週相比」同款，因為現在是跨科共用，不特別指哪一科）。

**Out of scope**：Phase 1 新增的數學班/理化班分析課程（392-427/442-477）與既有安親/英文分析課程（259-294/309-344）**保留不刪除**，但這次之後不再由任何寫入路徑更新——它們只在舊清單式頁面（`ASPopulationPartialView.cshtml`）繼續顯示歷史/既有資料，新網格完全改讀/寫 478-525 這組共用課程。

### 3. 路由與頁面

- `Home/Index.cshtml`：`inputItems` 陣列新增一筆 `("課輔(新版)", "/StudentPopulation/Index?type=ASGrid")`。
- `StudentPopulation/Index.cshtml`：新增 `addType.Equals("ASGrid")` 分支（比照現有 `AS` 分支），按鈕 `id="addASGridBtn"`，JS 導向 `/StudentPopulation/CreateASGridPopulation?schoolId=...&type=ASGrid...`。
- `StudentPopulationController.cs`：新增 `CreateASGridPopulation` action，邏輯比照 `CreateASPopulation`（`GetOrCreatePopulation`/`ViewBag.Courses`/`ViewBag.CanEditLastWeek`/`ViewBag.SelectedYear` 等既有查詢與 ViewBag 組裝方式不變，只是換一個 View 名稱與版面）。
- 新增 `Views/StudentPopulation/CreateASGridPopulation.cshtml`（外層：標題／狀態badge／確認送出按鈕，比照 `CreateASPopulation.cshtml` 骨架）＋ `Views/StudentPopulation/ASGridPopulationPartialView.cshtml`（內層：實際網格表格，透過 `$("#contentItem").html(data)` 局部更新，比照 `ASPopulationPartialView.cshtml` 的 AJAX 局部重繪模式）。

### 4. 網格版面（`ASGridPopulationPartialView.cshtml`）

```
        安親  英文-一對一 英文-團體 數學-一對一 數學-團體 理化-一對一 理化-團體  新生 流失 上週比 總人數
一年級  [__]    [__]      [__]      [__]      [__]      [__]      [__]     [__] [__]  12   [__]
二年級  [__]    [__]      [__]      [__]      [__]      [__]      [__]     [__] [__]  -3   [__]
...
```

- 每個主要人數欄的輸入框：`onchange` 呼叫既有 `valueChange(sId)` 模式（沿用 `StudentPopulationController.UpdateClassItem` action，不需要新 action）。
- 上週比／總人數欄：預設顯示自動算好的值（唯讀外觀＋一個可編輯 icon，比照 `ASPopulationPartialView.cshtml` 現有 `sumItem.IsManual` 分支），點擊後可輸入覆蓋值，送出走既有「手動覆蓋」寫入路徑（把該 `StudentPopulationItem.IsManual` 設為 true 並更新 `Number`，沿用現有機制，不需新 action）。
- 新生／流失欄：本來就是 `ManualInput`，直接是一般輸入框，`onchange` 走 `valueChange`。
- 每格右下「＋」按鈕：沿用現有 `addClassModal` 的 HTML/JS 架構，一樣呼叫既有 `AddNewClass` action（`schoolId`/`courseId`/`week`/`year`/`newClassName`/`newNumber`/`type='AS'` 參數不變），差別只在於觸發時機（從特定欄位的「＋」觸發，而非頁面頂部單一按鈕）與畫面呈現（新班級渲染成同一欄位的相鄰重複欄，而不是清單模式的新增一整列）。
- 刪除班級：沿用既有 `removeClassItem(sId)`（confirm 對話框 + `RemoveClassItem` action）。當某個重複欄（如「數學-一對一(2)」）底下所有年級都沒有對應班級資料時，該欄自然消失（比照現有 `if (Model.Items.Any(...))` 過濾邏輯，泛用套用到每個欄位分組，不需要額外「欄位收合」程式碼）。

### 5. 權限與狀態

- `ViewBag.CanEditLastWeek`：沿用既有判斷邏輯，決定「上週人數」是否可編輯，行為與舊頁面一致。
- `StudentPopulationStatus`：沿用既有狀態機，`Model.Status == Documented` 才顯示「新增班級」「確認送出」等編輯操作，其餘狀態唯讀顯示，比照舊頁面。

## 已知風險／需要驗證的地方

- **重複欄的欄位寬度/版面在 12 年級都很稀疏時的呈現**：由於重複欄是整欄新增（多數年級留空），視覺上可能顯得表格很寬、很多空白格。這是「同一行內新增重複欄」這個決定的必然結果，實作時需注意欄寬設定，避免表格因為稀疏的重複欄而過寬。
- **舊清單頁與新網格頁的資料是否會顯示不一致**：新生/流失/上週比/總人數改成跨科共用一組課程（478-525）後，舊清單頁（`ASPopulationPartialView.cshtml`）仍然只認舊的 4 科分開的分析課程（259-294/309-344/392-427/442-477），兩邊會呈現不同的數字（新網格顯示的是新共用課程的值，舊頁面顯示的是各科舊課程的值，兩者從新網格上線那週起不會再同步更新）。由於新舊版本本來就設計成並存、且使用者已確認接受這個過渡狀態，這裡只需要在瀏覽器驗證階段明確記錄「兩邊分析數字不同步是預期現象」，不是 bug。
- **`CheckByDiffItem` 一致性檢查**：目前 `StudentPopulationController.CheckNewLostConsistency` 的 `AfterSchool` case 呼叫 4 次 `CheckByDiffItem`（安親/英文/數學/理化，各自比對自己的分析班系）。新網格寫入的是共用課程（478-525，班系「課輔分析總覽」），不會被這 4 個既有呼叫檢查到，需要另外新增 `CheckByDiffItem("課輔", "課輔分析總覽")` 或等效邏輯，否則新網格填的新生/流失/上週比不會被一致性警示涵蓋。這點列入 Phase 2 實作範圍。

## 測試計畫

- SQL 資料驗證：比照 Phase 1，套用後獨立 SELECT 驗證 48 筆課程的 `StatisticsType`/`SourceCourseIds` 正確對應到各年級的 4 科課程 Id。
- `AggregationEngine` 計算驗證：對至少一個年級（如一年級）建立測試資料（安親/英文/數學/理化 4 科各填一個數字），驗證 478（上週比）與 514（總人數）算出的值等於 4 科加總，不需要額外測試計算邏輯本身（已由現有引擎泛用邏輯保證）。
- 網格頁面互動：需要瀏覽器手動驗證（AJAX 存檔、「＋」新增重複欄、分析欄手動覆蓋、確認送出流程），比照專案慣例延後到集中驗證那一輪。

## Out of scope（本次不處理）

- 舊清單式頁面（`CreateASPopulation`）的下架——新版穩定後另開任務處理。
- 舊的 4 科分開分析課程（259-294/309-344/392-427/442-477）的資料遷移或清理——保留不動，作為歷史資料。
- 匯入/匯出（`ASPopulationImporter`/`ReportExportService`）改為讀寫新的共用分析課程（478-525）——目前匯入/匯出的分析欄位（新生/流失/上週比/總人數）本來就没有被匯入匯出（Phase 1 已確認且維持現況），這次也不新增。
- 瀏覽器手動驗證。
