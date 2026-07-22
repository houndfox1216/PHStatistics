# PSJ（百倍速）Excel 版網格輸入介面 Design Spec

## 背景

AS（課輔）人數表已完成 Excel 版網格輸入介面（`docs/superpowers/specs/2026-07-20-as-input-grid-phase2-design.md`），分校反應良好。使用者接著要求 PSJ（百倍速，數學/理化課輔）人數表也比照改成網格形式，取代目前的清單式頁面（`CreatePSJPopulation`）。

跟 AS 不同，PSJ 的數學班（145–156）／理化班（195–206）原始課程模型本來就存在（不像 AS 在 phase1 才補上數學/理化），所以這次不需要 AS phase1 那樣的資料模型補課程步驟，只需要新增網格所需的「跨科共用分析課程」，屬性上更接近 AS phase2 單一階段。

## 決策摘要

| 項目 | 決定 |
|---|---|
| 範圍 | 僅 PSJ 百倍速；PH/GEPT/PS/AS 不動 |
| 新舊關係 | 新增獨立新頁面/新路由，不在現有 `CreatePSJPopulation` 加切換鈕；新舊並存，新版穩定後將取代舊版 |
| 進入點 | 首頁 `Home/Index.cshtml` 新增「百倍速(新版)」項目（與現有「百倍速」項目平行），連到 `/StudentPopulation/Index?type=PSJGrid`；現有「百倍速」項目不動，仍走舊流程 |
| 網格範圍 | 只顯示本週（單週網格），比照現有系統一個 `StudentPopulation` 對應一週的資料模型 |
| 網格結構 | 列＝12 年級；欄＝4 個主要人數欄（數學-一對一／數學-團體／理化-一對一／理化-團體）＋4 個分析欄（新生／流失／上週比／總人數） |
| 主要人數欄資料來源 | 直接沿用既有數學班（145–156）、理化班（195–206）課程，用 `Class.Type` 區分一對一/團體，不需新增課程 |
| 多班級支援 | 每個主要人數欄格子右下角「＋」→ 彈窗新增班級名稱＋人數 → 送出後在該欄位置整欄新增一個重複欄（如「數學-一對一(2)」），其餘年級該欄留空。同 AS 網格 |
| 分析欄互動 | 新生/流失/上週比/總人數皆可在網格直接編輯覆蓋，比照既有 `IsManual`/`RevertToAutoCalculation` 機制 |
| 分析欄資料模型 | 新增每年級一組共用課程（跨數學/理化 2 科），取代既有「2 科分開存」方案（171–194 數學、221–244 理化）——因為使用者要求可直接編輯覆蓋，分開儲存無法對應「編輯一個加總後的數字該寫回哪一科」的問題。理由與作法跟 AS phase2 完全相同 |
| 儲存方式 | 每格 onchange 即時 AJAX 儲存，比照 `valueChange`/`lastWeekValueChange` 既有模式，不做整頁一次性送出 |
| 狀態流程 | 沿用既有 `StudentPopulationStatus`（建檔中/已送出/已審核/已否決/已完成）與「確認送出」機制，不設計新狀態機 |
| 上週人數編輯權限 | 沿用既有 `ViewBag.CanEditLastWeek` 判斷，行為與舊頁面一致 |
| 匯入/匯出 | `PSJPopulationImporter`／`ReportExportService` 不改讀寫新共用分析課程（526–573），維持現狀，列入 Out of scope |
| 資料庫異動範圍 | 本機 dev DB 與正式環境 DB 都要套用（比照 AS phase2）；正式環境套用前依專案慣例先確認具體筆數/SQL 內容再執行 |

## 現況資料調查

Dev DB 查證（2026-07-23）：`Course` 最大 Id = 525，`CourseDepartment` 最大 Id = 46（AS phase2 上線後的狀態）。

PSJ 目前的資料形狀（`docs/superpowers/specs/2026-07-16-psj-as-aggregation-migration-design.md` 已調查過的部分）：

| 班系 | Id | 內容 |
|---|---|---|
| 數學班 | 27 | 原始課程 145–156（12 年級，一對一/團體用 `Class.Type` 區分） |
| 數學班統計 | 28 | 157（依班別合計）／158（總合計）／159–170（本週{年級}與上週相比×12） |
| 數學班分析 | 29 | 171–182（新生×12）／183–194（流失×12），目前 `StatisticsType=NULL`（分校自填） |
| 理化班 | 30 | 原始課程 195–206（同上結構） |
| 理化班統計 | 31 | 207（依班別合計）／208（總合計）／209–220（本週{年級}與上週相比×12） |
| 理化班分析 | 32 | 221–232（新生×12）／233–244（流失×12），`StatisticsType=NULL` |

跟 AS phase1 之後的狀態一樣，PSJ 目前**沒有每年級的「總人數」欄位**（只有 158/208 不分年級的總合計），也**沒有跨科合一的分析欄位**（新生/流失分數學/理化各自獨立存）。

`AggregationEngine.Compute` 的 `GetSourceItems` 篩選邏輯（同 AS phase2 調查結論）：`SourceCourseIds` 用 `ids.Contains(courseId)` 比對，本來就支援陣列裡有多個課程 Id，`DiffWithLastWeek`/`SumBySourceCourses` 會直接加總所有符合的來源項目——「一個課程同時加總數學+理化 2 科」不需要改任何計算程式碼，只要 `Course.SourceCourseIds` 設定正確即可。

## 詳細設計

### 1. 新增 `CourseDepartment`（Id 47）

| Id | Name | IsSum | Published | Type |
|---|---|---|---|---|
| 47 | 百倍速分析總覽 | 1 | 0 | 1 |

### 2. 新增 `Course`（Id 526–573，共 48 筆，`Ordinal=Id-1`，`Type=1`，`GroupByClassType=0`）

12 年級 × 4 項，`SourceCourseIds` 對照表（數學/理化 2 科的年級原始課程 Id）：

| 年級 | 數學 | 理化 |
|---|---|---|
| 一年級 | 145 | 195 |
| 二年級 | 146 | 196 |
| 三年級 | 147 | 197 |
| 四年級 | 148 | 198 |
| 五年級 | 149 | 199 |
| 六年級 | 150 | 200 |
| 國一 | 151 | 201 |
| 國二 | 152 | 202 |
| 國三 | 153 | 203 |
| 高一 | 154 | 204 |
| 高二 | 155 | 205 |
| 高三 | 156 | 206 |

（一對一/團體共用同一個課程 Id，靠 `Class.Type` 區分，所以來源陣列只需列一次年級課程 Id，兩種班別都會被加總進去——跟 AS phase2 的英文班用法邏輯一致。）

| Id 範圍 | 內容 | StatisticsType | SourceCourseIds（以一年級為例） |
|---|---|---|---|
| 526–537 | 本週{年級}與上週相比×12 | `DiffWithLastWeek`(10) | `[145,195]` |
| 538–549 | 本週{年級}新生人數×12 | `ManualInput`(50) | — |
| 550–561 | 本週{年級}流失人數×12 | `ManualInput`(50) | — |
| 562–573 | 本週{年級}總人數×12 | `SumBySourceCourses`(4) | `[145,195]` |

命名不加科目字樣（跟數學/理化現有的「本週{年級}與上週相比」同款，因為現在是跨科共用，不特別指哪一科）。

**Out of scope**：既有 4 科分開分析課程（171–194 數學新生/流失、221–244 理化新生/流失）**保留不刪除**，但這次之後不再由任何寫入路徑更新——它們只在舊清單式頁面（`CreatePSJPopulation`）繼續顯示歷史/既有資料，新網格完全改讀/寫 526–573 這組共用課程。

### 3. 路由與頁面

- `Home/Index.cshtml`：`inputItems` 陣列新增一筆 `("百倍速(新版)", "/StudentPopulation/Index?type=PSJGrid")`，緊接在現有「百倍速」項目之後。
- `StudentPopulation/Index.cshtml`：新增 `addType.Equals("PSJGrid")` 分支（比照現有 `ASGrid` 分支），按鈕 `id="addPSJGridBtn"`，JS 導向 `/StudentPopulation/CreatePSJGridPopulation?schoolId=...&type=PSJGrid...`。
- `StudentPopulationController.cs`：新增 `CreatePSJGridPopulation` action，邏輯完整比照 `CreateASGridPopulation`（`StudentPopulationController.cs:798`，含開檔/回填固定總計項目邏輯），只把 `StudentPopulationType.AfterSchool` 換成 `StudentPopulationType.PSJ`、`ASGridPopulationPartialView` 換成 `PSJGridPopulationPartialView`。
- 新增 `Views/StudentPopulation/CreatePSJGridPopulation.cshtml`（外層：標題／狀態badge／確認送出按鈕，比照 `CreateASGridPopulation.cshtml` 骨架，含 `gridAddClassModal`）＋ `Views/StudentPopulation/PSJGridPopulationPartialView.cshtml`（內層：實際網格表格，比照 `ASGridPopulationPartialView.cshtml`，換一組 `mainColumns`/`analysisColumns` 課程 Id 定義）。

### 4. AJAX Actions（複製 AS 對應 action，非泛化共用）

三個 AS 網格 AJAX action（`AddNewClassGrid`/`RemoveClassItemGrid`/`UpdateClassItemGrid`，`StudentPopulationController.cs:1295/1346/1381`）本身就是直接寫死 `StudentPopulationType.AfterSchool`／`"ASGridPopulationPartialView"`，並非從既有泛型 action 抽出來的共用邏輯。這次沿用相同專案慣例，新增三個平行的 PSJ 版本，而非把既有 AS action 改成泛型：

| 新 Action | 對應 AS Action |
|---|---|
| `AddNewClassGridPsj` | `AddNewClassGrid`（:1295） |
| `RemoveClassItemGridPsj` | `RemoveClassItemGrid`（:1346） |
| `UpdateClassItemGridPsj` | `UpdateClassItemGrid`（:1381） |

邏輯完全比照，只換 `StudentPopulationType.PSJ`、`PartialView("PSJGridPopulationPartialView", ...)`、`ViewBag.Courses` 篩選條件。

### 5. 網格版面（`PSJGridPopulationPartialView.cshtml`）

```
        數學-一對一 數學-團體 理化-一對一 理化-團體  新生 流失 上週比 總人數
一年級  [__]       [__]     [__]       [__]    [__] [__]  12   [__]
二年級  [__]       [__]     [__]       [__]    [__] [__]  -3   [__]
...
```

- `mainColumns` 定義（比照 `ASGridPopulationPartialView.cshtml:7-15`）：
  ```
  ("數學-一對一", [145..156], ClassType.Personal)
  ("數學-團體",   [145..156], ClassType.General)
  ("理化-一對一", [195..206], ClassType.Personal)
  ("理化-團體",   [195..206], ClassType.General)
  ```
- `analysisColumns` 定義：
  ```
  ("上週比", [526..537])
  ("新生",   [538..549])
  ("流失",   [550..561])
  ("總人數", [562..573])
  ```
- 每個主要人數欄的輸入框：`onchange` 呼叫 `gridValueChange(sId)`（對應 `UpdateClassItemGridPsj`）。
- 上週比／總人數欄：預設顯示自動算好的值，點擊後可輸入覆蓋值，走既有「手動覆蓋」寫入路徑（`IsManual=true`），比照 AS 網格 `sumItem.IsManual` 分支。
- 新生／流失欄：`ManualInput`，一般輸入框，`onchange` 走 `gridValueChange`。
- 每格右下「＋」按鈕：沿用 `gridAddClassModal` 架構，呼叫 `AddNewClassGridPsj`。
- 刪除班級：沿用 `gridRemoveClassItem` 模式，呼叫 `RemoveClassItemGridPsj`。重複欄底下所有年級都無對應班級資料時自然消失，沿用既有過濾邏輯。

### 6. 一致性檢查

`CheckNewLostConsistency` 的 `PSJ` case（`StudentPopulationController.cs:2065-2068`）目前：

```csharp
case StudentPopulationType.PSJ:
    CheckByDiffItem("數學", "數學班分析");
    CheckByDiffItem("理化", "理化班分析");
    break;
```

新增第三個呼叫，比照 AS case（:2074）的做法：

```csharp
case StudentPopulationType.PSJ:
    CheckByDiffItem("數學", "數學班分析");
    CheckByDiffItem("理化", "理化班分析");
    CheckByDiffItem("百倍速(新網格)", "百倍速分析總覽");
    break;
```

### 7. 權限與狀態

- `ViewBag.CanEditLastWeek`：沿用既有判斷邏輯，決定「上週人數」是否可編輯，行為與舊頁面一致。
- `StudentPopulationStatus`：沿用既有狀態機，`Model.Status == Documented` 才顯示「新增班級」「確認送出」等編輯操作，其餘狀態唯讀顯示，比照舊頁面/AS 網格。

## 已知風險／需要驗證的地方

- **舊清單頁與新網格頁的資料是否會顯示不一致**：新生/流失/上週比/總人數改成跨科共用一組課程（526–573）後，舊清單頁（`CreatePSJPopulation`）仍然只認舊的 2 科分開分析課程（171–194/221–244），兩邊會呈現不同的數字，兩者從新網格上線那週起不會再同步更新。跟 AS phase2 完全相同的預期過渡狀態，需要在瀏覽器驗證階段明確記錄「兩邊分析數字不同步是預期現象」，不是 bug。
- **重複欄的欄位寬度/版面**：同 AS 網格的已知風險，實作時需注意欄寬設定。
- **`CheckByDiffItem` 一致性檢查**：已列入本次實作範圍（見第 6 節），非風險，僅在此提醒驗證階段需一併測試。

## 測試計畫

- SQL 資料驗證：套用後獨立 SELECT 驗證 48 筆課程的 `StatisticsType`/`SourceCourseIds` 正確對應到各年級的 2 科課程 Id。
- `AggregationEngine` 計算驗證：對至少一個年級（如一年級）建立測試資料（數學/理化各填一個數字），驗證 526（上週比）與 562（總人數）算出的值等於 2 科加總，不需要額外測試計算邏輯本身（已由現有引擎泛用邏輯保證）。
- 網格頁面互動：需要瀏覽器手動驗證（AJAX 存檔、「＋」新增重複欄、分析欄手動覆蓋、確認送出流程、一致性檢查警示），比照專案慣例延後到集中驗證那一輪。

## Out of scope（本次不處理）

- 舊清單式頁面（`CreatePSJPopulation`）的下架——新版穩定後另開任務處理。
- 舊的 2 科分開分析課程（171–194/221–244）的資料遷移或清理——保留不動，作為歷史資料。
- 匯入/匯出（`PSJPopulationImporter`/`ReportExportService`）改為讀寫新的共用分析課程（526–573）——維持現狀，不新增。
- 瀏覽器手動驗證。
