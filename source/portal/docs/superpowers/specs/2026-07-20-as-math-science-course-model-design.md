# AS（課輔）新增數學班／理化班資料模型 Design Spec — Phase 1

## 背景

使用者反映課輔班（AS）人數表的網頁輸入介面，跟分校原本使用的 Excel（`百瀚全區課輔人數總表`）版面差異太大，希望做一個新的、比照 Excel 排版的輸入介面（列＝年級、欄＝科目×班別的網格）。這份新網格介面規劃為 Phase 2，會用新路由/新頁面呈現，不影響現有 `CreateASPopulation` 清單式頁面（新舊並存，新版穩定後再取代舊版）。

在規劃 Phase 2 網格之前，比對參考 Excel 與現有資料庫結構，發現一個更根本的落差：**Excel 實際有 7 個人數欄位**（安親課輔班、英文班×2、數學班×2、理化班×2），但資料庫裡 AS（`StudentPopulationType.AfterSchool`，`Type=4`）**只有「安親課輔班」和「英文班」兩個班系**，完全沒有「數學班」「理化班」對應的 `Course`/`CourseDepartment`。對照 `CourseMapping.AsCourseIds` 裡的 `N`/`L`/`W` 代碼，實際對應到的是安親的「新生/流失/上週比」分析欄，不是數學/理化——也就是說匯入程式和資料庫從一開始就沒有承接數學班、理化班的人數，這兩欄在系統裡形同不存在（`ASPopulationImporter.Import` 的 `colDefs` 只讀到 col5，col6-9 完全被忽略）。

這份 Phase 1 spec 專門處理「補齊數學班/理化班資料模型」，讓 Phase 2 的網格有完整資料可以呈現。跟使用者確認過的範圍：

- 這次新增數學班、理化班的完整資料結構（比照安親/英文的慣例）。
- 新生/流失/上週比/總人數這類跨科目的彙總顯示邏輯（Excel 只有一欄，不分科目），留到 Phase 2 網格設計時處理；Phase 1 只需要讓數學班/理化班各自擁有自己的一套「合計/上週比/新生/流失」分析課程，跟安親/英文目前的做法保持一致（不做跨科目彙總，這是既有分析欄位 import 也一直略過的範圍，比照 PSJ 遷移 spec 的先例）。

## 現況資料調查

### 參考 Excel 結構（`百瀚全區課輔人數總表(20260613).xlsx`，3 個分校頁籤，欄位完全相同）

| Row | 內容 |
|---|---|
| 1 | 標題 |
| 2 | 大分類表頭：col3 安親課輔班、col4-5 英文班、col6-7 數學班、col8-9 理化班、col10-13 分析 |
| 3 | 子項：col4/6/8=一對一、col5/7/9=團體班、col10=新生、col11=流失、col12=上週比、col13=總人數 |
| 5+ | 資料列，每列一個年級（12 年級一次），週次/日期只在每週第一列出現 |

### 資料庫既有 AS 課程結構（Type=4，Course Id 245–344，共 100 筆；已於 2026-07-16 PSJ+AS 加總遷移 spec 查證過，這次沿用其調查結果）

| Id 範圍 | 名稱模式 | 班系 | IsSum | 說明 |
|---|---|---|---|---|
| 245–256 | 安親課輔班原始課程（一年級–高三） | 安親課輔班班(33) | 0 | 網格「安親」欄的來源 |
| 257 | 本周安親課輔班人數合計 | 安親課輔班班統計(34) | 1，`SumBySourceDepartments`(3)，`SourceDepartmentIds=[33]` | 舊清單式 UI 的合計 header |
| 258 | 本週安親課輔班總人數合計 | 同上 | 1，同上 | 跟 257 算出來是同一個數字，既有現象，非本次修正範圍 |
| 259–270 | 本週{年級}與上週相比×12 | 安親課輔班班分析(35) | 1，`DiffWithLastWeek`(10)，`SourceCourseIds=[245]`...`[256]` | |
| 271–282 | 本週{年級}安親課輔班文新生人數×12 | 同上 | 1，`ManualInput`(50) | 分校自填，系統不計算 |
| 283–294 | 本週{年級}安親課輔班文流失人數×12 | 同上 | 1，`ManualInput`(50) | 同上 |
| 295–306 | 英文班原始課程（EP/EG 共用同一批 Course，用 `Class.Type` 區分一對一/團體） | 英文班(36) | 0 | |
| 307–308 | 本周/本週英文班(總)人數合計 | 英文班統計(37) | 1，同 257/258 模式 | |
| 309–320 | 上週比×12 | 英文班分析(38) | 1，`DiffWithLastWeek`(10) | |
| 321–332 | 新生×12 | 同上 | 1，`ManualInput`(50) | |
| 333–344 | 流失×12 | 同上 | 1，`ManualInput`(50) | |

目前 `Course` 最大 Id = 377（PSJ CKC 用到 377），`CourseDepartment` 最大 Id = 39（CKC）。

**加總計算完全是資料驅動**：`AggregationEngine`（2026-07-16 遷移後）已通用支援 `SumBySourceDepartments`/`DiffWithLastWeek`/`ManualInput`，不需要为 AS 写任何 if/else 特判。只要新的 Course 設定好 `StatisticsType`/`SourceDepartmentIds`/`SourceCourseIds`/`GroupByClassType`，加總會自動生效，Phase 1 完全不用動 `AggregationEngine.cs` 或 `StudentPopulationController.SumPHPopulation`。

### PSJ 已有對稱先例可直接參考

PSJ（`Type=1`）本來就有數學班（Course Id 145–156，`GroupByClassType=true` 分班別算合計）、理化班（195–206）這兩科，且已在 2026-07-16 完成加總規則遷移（`docs/superpowers/specs/2026-07-16-psj-as-aggregation-migration-design.md`）。這次 AS 新增數學班/理化班的形狀完全比照 PSJ 的既有結構，只是：

- **`GroupByClassType` 這次沿用 AS 自己的既有慣例，設為 `false`**（安親/英文的 257/258/307/308/259-270/309-320 全部是 `GroupByClassType=false`），不採用 PSJ 「157 用 true、158 用 false」那種依班別分開算的做法。這是刻意保持 AS 現有行為一致，不是這次要修的東西。

## 決策摘要

| 項目 | 決定 |
|---|---|
| 新增範圍 | 數學班、理化班各自一套「年級原始課程(12) + 統計合計(2) + 分析：上週比/新生/流失(各12)」，完全比照安親/英文現有形狀 |
| `GroupByClassType` | 全部 `false`，比照 AS 現有慣例（不比照 PSJ 依班別分開算） |
| ClassType 對應 | 一對一 → `Personal`，團體班 → `General`（比照英文 EP/EG，不用 PSJ 的 `SubGroup`） |
| 新生/流失/上週比/總人數 跨科目彙總 | **Out of scope，留給 Phase 2**（Excel 只有一欄，但資料庫這次比照安親/英文繼續各科目分開存，Phase 2 網格畫面上再加總顯示） |
| 命名 | 新課程/班系名稱不沿用安親「安親課輔班班」（疊字）、「…文新生人數」（多一個「文」字）這兩個既有 typo，新資料採乾淨命名（「數學班」「數學班統計」「數學班分析」「本週{年級}數學班新生人數」），避免把舊 typo 複製到新資料 |
| 匯入/匯出 | 同步修正 3 處重複維護的 `_asCourseIds`/`AsCourseIds` 對照表（`CourseMapping.cs`、`ReportExportService.cs`、`StudentPopulationController.cs`）與對應的 `colDefs`/`codes` 陣列，讓數學班/理化班人數真正被匯入/匯出 |
| 一致性檢查 | `StudentPopulationController.CheckNewLostConsistency` 的 `AfterSchool` case 補上 `CheckByDiffItem("數學班", "數學班分析")`／`CheckByDiffItem("理化班", "理化班分析")` 兩行，這是寫死清單、不會自動涵蓋新科目 |

## 詳細設計

### 1. 新增 `CourseDepartment`（Id 40–45，`Ordinal = Id - 1`，比照既有 `Type=4`/`Company=0` 慣例）

| Id | Name | IsSum | Published | Subject |
|---|---|---|---|---|
| 40 | 數學班 | 0 | 1 | NULL |
| 41 | 數學班統計 | 1 | 0 | NULL |
| 42 | 數學班分析 | 1 | 0 | NULL |
| 43 | 理化班 | 0 | 1 | NULL |
| 44 | 理化班統計 | 1 | 0 | NULL |
| 45 | 理化班分析 | 1 | 0 | NULL |

`Subject` 欄位查證後發現目前 AS 完全沒有任何課程靠 `SourceSubject` 讀取這個值（只有 `StatisticsCalculationService` 支援這個篩選路徑，但 AS/PSJ 現有課程全部用 `SourceDepartmentIds`/`SourceCourseIds`，不用 `SourceSubject`），所以新班系的 `Subject` 直接留 `NULL`，不刻意編號。

`Published`（班系層級）沿用「原始課程班系=1（會出現在『新增班級』下拉選單）、統計/分析班系=0（不會出現）」的既有規則。

### 2. 新增 `Course`（Id 378–477，共 100 筆，`Ordinal = Id - 1`，`Type=4`，`ClassType='5'` 比照 245/295 等既有 AS 原始課程的做法，`DataMode=0`）

**數學班（378–427）**

| Id 範圍 | 內容 | DepartmentId | IsSum | StatisticsType | Source |
|---|---|---|---|---|---|
| 378–389 | 一年級–高三（原始課程） | 40 | 0 | NULL | — |
| 390 | 本周數學班人數合計 | 41 | 1 | `SumBySourceDepartments`(3) | `SourceDepartmentIds=[40]` |
| 391 | 本週數學班總人數合計 | 41 | 1 | `SumBySourceDepartments`(3) | `SourceDepartmentIds=[40]` |
| 392–403 | 本週{年級}與上週相比×12 | 42 | 1 | `DiffWithLastWeek`(10) | `SourceCourseIds=[378]`…`[389]`（逐年級對應） |
| 404–415 | 本週{年級}數學班新生人數×12 | 42 | 1 | `ManualInput`(50) | — |
| 416–427 | 本週{年級}數學班流失人數×12 | 42 | 1 | `ManualInput`(50) | — |

**理化班（428–477），結構跟數學班完全對稱**

| Id 範圍 | 內容 | DepartmentId | IsSum | StatisticsType | Source |
|---|---|---|---|---|---|
| 428–439 | 一年級–高三（原始課程） | 43 | 0 | NULL | — |
| 440 | 本周理化班人數合計 | 44 | 1 | `SumBySourceDepartments`(3) | `SourceDepartmentIds=[43]` |
| 441 | 本週理化班總人數合計 | 44 | 1 | `SumBySourceDepartments`(3) | `SourceDepartmentIds=[43]` |
| 442–453 | 上週比×12 | 45 | 1 | `DiffWithLastWeek`(10) | `SourceCourseIds=[428]`…`[439]` |
| 454–465 | 新生×12 | 45 | 1 | `ManualInput`(50) | — |
| 466–477 | 流失×12 | 45 | 1 | `ManualInput`(50) | — |

年級順序統一使用既有 `CourseMapping.GradeOrder`：`一年級,二年級,三年級,四年級,五年級,六年級,國一,國二,國三,高一,高二,高三`。

### 3. `CourseMapping.cs` 新增（`Portal/Services/Import/ImportSupport/CourseMapping.cs`）

```csharp
public static readonly Dictionary<string, int[]> AsCourseIds = new() {
    ["AS"] = new[]{245,246,247,248,249,250,251,252,253,254,255,256},
    ["EP"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
    ["EG"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
    ["N"]  = new[]{271,272,273,274,275,276,277,278,279,280,281,282},
    ["L"]  = new[]{283,284,285,286,287,288,289,290,291,292,293,294},
    ["W"]  = new[]{259,260,261,262,263,264,265,266,267,268,269,270},
    // 新增：
    ["MP"] = new[]{378,379,380,381,382,383,384,385,386,387,388,389}, // 數學班-一對一
    ["MG"] = new[]{378,379,380,381,382,383,384,385,386,387,388,389}, // 數學班-團體（跟 EP/EG 一樣，一對一/團體共用同一批 Course.Id，靠 Class.Type 區分）
    ["SP"] = new[]{428,429,430,431,432,433,434,435,436,437,438,439}, // 理化班-一對一
    ["SG"] = new[]{428,429,430,431,432,433,434,435,436,437,438,439}, // 理化班-團體
};
```

`CourseMapping.AsColumnType(string code)` 目前已經是：

```csharp
public static ClassType AsColumnType(string code) => code switch {
    "EP" or "MP" or "SP" => ClassType.Personal,
    "ES" or "MS" or "SS" => ClassType.SubGroup,
    _ => ClassType.General,
};
```

`"MP"`/`"SP"` 已經預先對應到 `Personal`，`"MG"`/`"SG"` 會落到預設的 `General` 分支——**這個函式不需要修改**（推測是先前比照 PSJ 複製過來時就順手预留的，這次剛好用上）。`ReportExportService.AsColType` 和 `StudentPopulationController.AsColType` 這兩份重複的副本內容跟這個函式完全一致，同樣不需要修改。

### 4. `ASPopulationImporter.cs` 修正（讀取 Excel col6–9）

```csharp
var colDefs = new[] {
    (col: 3, code: "AS", cType: ClassType.General),
    (col: 4, code: "EP", cType: ClassType.Personal),
    (col: 5, code: "EG", cType: ClassType.General),
    // 新增：
    (col: 6, code: "MP", cType: ClassType.Personal),
    (col: 7, code: "MG", cType: ClassType.General),
    (col: 8, code: "SP", cType: ClassType.Personal),
    (col: 9, code: "SG", cType: ClassType.General),
};
```

其餘匯入邏輯（`AsCourseIds[code][gradeIdx]` 查課程、`PopulationWriteHelper.AddClassAndItem` 寫入）完全不用改，col10–13（分析欄）比照現況繼續略過不匯入。

### 5. 匯出修正（`ReportExportService.cs` + `StudentPopulationController.cs`）

兩處各自維護的 `_asCourseIds` 字典比照第 3 節同步加上 `MP`/`MG`/`SP`/`SG` 四筆。

`ReportExportService.BuildSheetAS`（批次多校匯出）與 `StudentPopulationController` 內單校匯出區塊，`codes` 陣列都從：

```csharp
string[] codes = { "T", "AS", "EP", "EG", "N", "L", "W" };
```

改成：

```csharp
string[] codes = { "T", "AS", "EP", "EG", "MP", "MG", "SP", "SG", "N", "L", "W" };
```

其餘邏輯（`WriteGradeHeader`/`WriteGradeRow`/逐格查 `codeMap[code][gradeIdx]` 加總）沿用泛用的 code-driven 迴圈，不用額外改動。

### 6. 新生/流失一致性檢查（`StudentPopulationController.CheckNewLostConsistency`）修正

`CheckNewLostConsistency` 內的 `switch (population.Type)` 對每個人數表類型都是**寫死呼叫哪些班系**（不是泛用迴圈）。目前 `AfterSchool` 這個 case（約 line 1812-1815）：

```csharp
case StudentPopulationType.AfterSchool:
    CheckByDiffItem("安親課輔", "安親課輔班班分析");
    CheckByDiffItem("英文班", "英文班分析");
    break;
```

需要新增兩行涵蓋數學班/理化班：

```csharp
case StudentPopulationType.AfterSchool:
    CheckByDiffItem("安親課輔", "安親課輔班班分析");
    CheckByDiffItem("英文班", "英文班分析");
    CheckByDiffItem("數學班", "數學班分析");
    CheckByDiffItem("理化班", "理化班分析");
    break;
```

`CheckByDiffItem(label, departmentName)` 用 `Class.Course.Department.Name == departmentName` 精確比對班系名稱，並在該班系內用 `Course.Name.Contains(grade) && Course.Name.Contains("與上週相比"/"新生"/"流失")` 找對應課程——這正是第 2 節規劃的「數學班分析」「理化班分析」班系名稱與「本週{年級}與上週相比」「本週{年級}數學班新生人數」「本週{年級}數學班流失人數」課程命名，天然相容，不需要額外調整命名。

（先前這裡誤判為「泛用邏輯，不需要新增數學/理化專屬分支」，查證程式碼後修正：這其實跟 codes 陣列一樣是寫死的清單，這次一併補上。）

### 7. SQL Script

新增 `docs/superpowers/sql/2026-07-20-as-math-science-course-apply.sql` / `...-revert.sql`，比照 `2026-07-16-psj-ckc-course-apply.sql` 的格式（`SET IDENTITY_INSERT ... ON/OFF` 包住兩段 INSERT）。Revert script 為：

```sql
DELETE FROM Course WHERE Id BETWEEN 378 AND 477;
DELETE FROM CourseDepartment WHERE Id BETWEEN 40 AND 45;
```

（套用前需確認當下沒有已匯入資料引用這些 Id——這批是全新 Id，剛建立時不會有引用，但如果套用後又實際跑過匯入/手動輸入才要 revert，需要先處理 `Class`/`StudentPopulationItem`。）

## 已知風險／需要驗證的地方

- **這次新增的 100 筆 Course 對「舊清單式 UI」（`CreateASPopulation`/`ASPopulationPartialView`）是立即生效的**：因為該頁面是資料驅動（`foreach (Course cItem in courses)` 撈 `Type=AfterSchool` 的所有課程），新增數學班/理化班後，舊頁面會自動多出這兩個課程群組（含「新增班級」下拉選單新增選項）。這是預期中的副作用，不是 bug，但瀏覽器實測時要涵蓋到（確認舊頁面顯示正常、不會因為多了課程而排版跑掉）。
- **390/391（以及 440/441）這兩個統計課程算出來會是同一個數字**：比照安親 257/258 的既有現象（`GroupByClassType` 都是 `false`），刻意保持一致，不是這次引入的新 bug。
- **兩份 `codes` 陣列（`ReportExportService.BuildSheetAS` 與 `StudentPopulationController` 單校匯出）目前完全沒有涵蓋數學班/理化班**，這是本次要修的既有 bug，不是新功能——換句話說，補上這兩科不只是「支援新資料」，也是修正一個匯出遺漏長達一段時間的既有缺陷。
- **`SourceCourseIds` JSON 格式**：沿用專案既有慣例，單元素也要包成陣列字面值（`[378]`），對應 `AggregationEngine.ParseIntArray`/`ComputeIsumValue.TryParseIntArray` 一律用 JSON 陣列解析的既有規則。

## 測試計畫

- 寫一支 `[Explicit]` NUnit 測試，比照 `PsjImportTests.cs`/既有 AS 相關測試的模式：用參考 Excel（`百瀚全區課輔人數總表(20260613).xlsx`）任一分校頁籤的數學班/理化班欄位非零值，實際跑 `ASPopulationImporter.Import`，核對 `StudentPopulationItem.Number` 與 Excel 原始儲存格一致。
- 核對 390/391/440/441/392-403/442-453 這些新增的 `IsSum` 課程，套用資料後透過既有 `StudentPopulationController.SumPHPopulation` → `AggregationEngine.Calculate` 跑一次，確認算出來的值符合預期公式（同班系加總、與上週相比），不需要額外寫計算邏輯測試（引擎既有邏輯已經由 PSJ/AS 遷移測試驗證過）。
- 匯出：用一筆已有數學班/理化班測試資料的 population，分別呼叫 `ReportExportService.Export("AS", ...)` 與 `StudentPopulationController` 單校匯出 action，核對輸出 Excel 的 `codes` header 與對應數值正確。
- 瀏覽器手動驗證（舊頁面顯示是否正常、新增班級下拉選單是否正確列出數學班/理化班）留到之後集中驗證那一輪，比照專案慣例。

## Out of scope（本次不處理，留給 Phase 2 或後續）

- 新生/流失/上週比/總人數的**跨科目彙總顯示**（Excel 只有一欄，這次資料庫仍比照安親/英文各科目分開存）——Phase 2 網格畫面設計時處理，包含「總人數」這個目前系統完全沒有的、跨 4 科目加總的新概念（`StatisticsType.SumBySourceCourses`，可手動覆蓋，比照現有 `IsManual`/`RevertToAutoCalculation` 機制）。
- Phase 2 的新網格輸入頁面本身（新 Controller action、新 View、新 JS、每格「＋」按鈕支援多班、新進入入口）。
- 既有安親/英文 257/258（合計/總合計算出同數字）、299-306(EP/EG 共用同課程) 這類既有現象的「修正」——這次刻意保持一致，不藉機修正。
