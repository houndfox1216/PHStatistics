# 加總計算抽離 設計文件

**日期**：2026-07-14
**狀態**：設計確認，待撰寫實作計畫
**範圍**：把 `StudentPopulationController.SumPHPopulation` 與 `ReportExportService.ComputeIsumValue` 兩套各自維護的加總 if/else，抽離成一份共用、資料驅動的加總引擎

---

## 背景

目前人數表的加總邏輯有兩套平行實作，各自維護、容易失準：

1. `StudentPopulationController.cs` 的 `SumPHPopulation` 方法（約 1350-1705 行）：每次存檔後重算，依 `StudentPopulationType`（PH/PSJ/GEPT/PS/AfterSchool）分 5 大分支，用課程名稱字串比對＋`Course.Id` 位移運算（如 `Course.Id - 38`、`Department.Id != 21`）寫死判斷邏輯。
2. `ReportExportService.ComputeIsumValue`/`InferStatisticsType`：匯出 Excel 時即時計算，邏輯與上面平行但獨立維護。

另外還有一套**先前規劃但從未被呼叫的死碼**：`Portal/Services/StatisticsCalculationService.cs`（`CalculateAll`/`Calculate`/`DetermineStatisticsTypeByName`）。全域搜尋確認沒有任何地方真正呼叫它，只在自己內部、`CLAUDE.md`、舊設計文件被提及。

當初 `SumPHPopulation` 採用 `Course.Id` 位移/範圍寫法，是因為正式環境與開發環境資料庫的課程 `Id` 對不起來，只能用位移湊出對應範圍；**現在正式與開發環境已同步，所有課程 `Id` 一致**，這個歷史限制已經不存在，可以改用明確的資料欄位（`DepartmentId`/`SourceDepartmentIds` 等）取代位移運算。

使用者要求：以下列 4 條實際業務規則重新檢視現有 schema 是否足夠，並抽離成獨立、可共用、資料驅動（不寫死在程式碼裡）的加總邏輯，讓未來新增班系或課程不需要大規模調整程式碼：

1. 依班系（`CourseDepartment`）為單位加總人數
2. 班數＝該週人數不為 0 的班級數量
3. 某些課程人數不計入加總（例如合作開班類型的班級，是否計入其他分校目前規則未定，預設不計入）
4. 同期相比＝本週人數－去年同期人數；上週相比＝本週人數－上週人數

---

## Schema 現況盤點

逐條對照現有欄位：

| 規則 | 對應機制 | 結論 |
|---|---|---|
| 1. 依班系加總 | `Course.DepartmentId` | 已足夠 |
| 2. 班數（人數不為 0 的班級數） | 以 `Number > 0` 計數 | 已足夠，不需新欄位 |
| 3. 某些課程不計入 | 不把該課程/班系放進任何來源清單，即自然不計入 | 已足夠（見下方「範圍外」說明） |
| 4. 同期/上週相比 | 本週 `Number` 減去對應週次的 `Number`/`LastWeekNumber` | 已足夠 |

`Course` 表已有的關鍵欄位（目前只有死碼在用，Admin 後台完全沒有介面可以設定）：

- `StatisticsType`（smallint，可為 null）：計算類型列舉
- `SourceDepartmentIds`（nvarchar，JSON int 陣列）：明確指定要加總的班系清單，可直接取代 `Course.Id <= 38` 這類位移/範圍寫法
- `SourceCourseIds`（nvarchar，JSON int 陣列）：明確指定要加總的課程清單
- `ApplicableClassType`（smallint，可為 null）：限定只加總特定班別
- `GroupByClassType`（bit）：是否依列本身的班別分組計算
- `DepartmentId`（int，可為 null）：課程所屬班系
- `Ordinal`（int）：排序（已存在，Admin `Course/Index.cshtml` 已可編輯「排列順序」欄位，**不需要新增欄位**）
- `IsSum`（bit）：是否為加總列（**Admin 後台目前已經是可編輯欄位**，`Areas/Admin/Views/Course/Index.cshtml` 的 DevExtreme 表格已包含且未鎖定編輯）

結論：**schema 大致上已經足夠**，缺的不是欄位，而是「後台管理介面」——`StatisticsType`/`SourceDepartmentIds`/`SourceCourseIds`/`ApplicableClassType`/`GroupByClassType` 這些欄位資料庫有，但 Admin 課程管理畫面完全沒有暴露，只能改程式碼或直接改資料庫。

新生/流失（`StatisticsType.NewStudents`/`LostStudents`）：07-14 已確定全面改人工輸入（見 `project-newlost-manual-and-deadcode` 記憶），新引擎不需要真的計算這兩種類型，比照人工輸入課程處理（略過自動計算）。

---

## 架構設計

### 新的共用加總引擎

新增 `Portal/Services/Aggregation/AggregationEngine.cs`（暫名，不沿用 `StatisticsCalculationService.cs`——該檔案未經任何真實資料驗證過，且使用者傾向重寫一份針對目前 5 種人數表實際規則設計過的乾淨版本，而非沿用未驗證的舊檔案）。

引擎職責：輸入一個 `IsSum=true` 的 `Course` 與該週人數表的 `Items` 集合，依 `Course.StatisticsType` 分派到對應計算：

- `SumByDepartment`：同 `DepartmentId` 底下非 `IsSum` 項目加總
- `SumByDepartmentAndClassType`：同 `DepartmentId` 且同班別加總
- `SumBySourceDepartments`：依 `SourceDepartmentIds` 指定的班系清單加總（可選擇性套用 `ApplicableClassType`）
- `SumBySourceCourses`：依 `SourceCourseIds` 指定的課程清單加總
- `CountClasses`/`CountClassesByClassType`：計算 `Number > 0` 的班級數
- `DiffWithLastWeek`：本週 `Number` 加總 － 上週 `LastWeekNumber` 加總
- `DiffWithLastYear`：本週加總 － 去年同期加總（需跨 `StudentPopulation` 查詢去年同週次資料）
- `LastWeekValue`：單純加總範圍內項目的 `LastWeekNumber`（不做差額，例如「上週OO總人數」這類欄位）
- `LastYearValue`：單純加總去年同週次範圍內項目的 `Number`（不做差額，例如「去年同期人數」這類欄位；需跨 `StudentPopulation` 查詢去年同週次資料）
- `ManualInput`/`None`：不計算，維持使用者輸入值原樣

（`LastWeekValue`/`LastYearValue` 是盤點 GEPT 實際規則時發現需要的：「上週英檢總人數」「去年同期人數」這兩個課程只需要範圍內的單純加總，不是差額。這兩個列舉值在 `StatisticsType.cs` 裡本來就已經定義好、只是死碼從未真正實作，這裡補上實作，不需要新增列舉值或資料庫欄位。）

「來源班系」（`SourceDepartmentIds`）與「來源課程」（`SourceCourseIds`）互斥、擇一設定：課程設定了 `SourceDepartmentIds` 就用 `SumBySourceDepartments`，否則若設定了 `SourceCourseIds` 就用 `SumBySourceCourses`；兩者皆未設定、但 `StatisticsType` 選了 `SumByDepartment` 類，則預設用課程自身的 `DepartmentId`。Admin 介面應在使用者同時填兩者時提示只會採用其中一個，避免誤解成兩者疊加。

### 呼叫端統一

- `StudentPopulationController.SumPHPopulation`：移除 5 大分支 if/else，改成遍歷該人數表所有 `IsSum=true` 項目，逐一呼叫 `AggregationEngine.Calculate(...)`
- `ReportExportService.ComputeIsumValue`：同樣改呼叫 `AggregationEngine.Calculate(...)`，移除自己的平行實作與 `InferStatisticsType`（匯出時無法依課程名稱字串猜測類型，改成強制要求 `Course.StatisticsType` 已設定；若未設定則視為 0 並記錄警告，逼迫資料确實配置好規則，而不是悄悄猜錯）
- 舊的 `StatisticsCalculationService.cs` 與 `ComputeIsumValue`/`InferStatisticsType` 死碼，於全部 5 種類型都完成遷移驗證後一併刪除

### Admin 後台規則設定介面

`Areas/Admin/Views/Course/Index.cshtml` 的 DevExtreme 表格新增欄位（只在 `IsSum=true` 時顯示/有意義）：

- 「加總類型」下拉（對應 `StatisticsType` 列舉：依班系加總／依班系+班別加總／依指定班系清單加總／依指定課程清單加總／班數／班數(依班別)／上週相比／去年同期／人工輸入不計算）
- 「來源班系」多選（對應 `SourceDepartmentIds`，取代目前寫死的 Id 位移/範圍）
- 「來源課程」多選（對應 `SourceCourseIds`）
- 「適用班別」下拉（對應 `ApplicableClassType`）
- 「依班別分組」勾選（對應 `GroupByClassType`）

這樣未來新增班系或課程，只需要在後台設定對應課程的加總規則，不需要改程式碼、不需要重新部署。

---

## 遷移順序（分型別上線，降低風險）

這是正式在用、影響學校真實人數/報表的系統，換算法不能算錯。採用**分型別、由簡到繁**的上線順序，每個型別都經過「比對驗證」才切換，不一次全部換過去：

**順序：GEPT → PS → PSJ → AfterSchool → PH**

每個型別的步驟：

1. 盤點該型別所有 `IsSum=true` 課程，對照現行 if/else 的實際邏輯，在 Admin 後台設定對應的加總規則（`StatisticsType`/`SourceDepartmentIds`/`SourceCourseIds`/`ApplicableClassType`/`GroupByClassType`）
2. 用現有真實人數資料（多筆不同分校/週次），同時跑「舊 if/else」與「新引擎」，逐課程比對計算結果數字是否完全一致
3. 一致才把該型別的存檔（`SumPHPopulation`）與匯出（`ReportExportService`）改呼叫新引擎；不一致則檢討並修正後台規則設定，除非發現目前規則設計本身無法表達某個特殊情況，才回頭修引擎程式碼
4. 全部 5 型別都切換完成、驗證通過後，才刪除舊 if/else 分支與死碼檔案（`StatisticsCalculationService.cs`、`ComputeIsumValue`/`InferStatisticsType`）

---

## 資料異動摘要

- 無新增資料庫欄位／無 schema migration（`Course` 表所需欄位皆已存在）
- 資料面異動：對現有全部 5 種類型、所有 `IsSum=true` 的課程列，逐一設定 `StatisticsType`/`SourceDepartmentIds`/`SourceCourseIds`/`ApplicableClassType`/`GroupByClassType`（一次性資料配置工作，非程式碼變更）

---

## Out of scope（本次不處理）

- **合作開班計入其他分校**：目前規則未定（哪些班級、計入哪個分校、以什麼條件判斷皆未確認），維持預設不計入（不把合作開班課程放進任何其他加總的來源清單即可），待業務規則明確後另案設計，不需要現在改 schema
- **課程排序（`Ordinal`）**：欄位已存在、Admin 後台已可編輯，不需要任何異動
- 死碼檔案（`StatisticsCalculationService.cs`、`ComputeIsumValue`/`InferStatisticsType`）的刪除時機在全部 5 型別遷移驗證通過後，不在本次設計階段執行

---

## 測試考量

- 每個型別遷移時：用該型別至少 2-3 筆不同分校、不同週次的真實資料，比對舊 if/else 與新引擎逐課程計算結果
- 新引擎的每種 `StatisticsType` 至少需要單元測試覆蓋（`SumByDepartment`/`SumBySourceDepartments`/`CountClasses`/`DiffWithLastWeek`/`DiffWithLastYear`/`ManualInput`）
- Admin 規則設定介面：涵蓋「`IsSum=false` 時規則欄位不顯示」「規則欄位缺漏或衝突時的提示」
- `ReportExportService` 遇到 `IsSum=true` 但 `StatisticsType` 未設定的課程：需驗證會記錄警告而非悄悄輸出 0 或錯誤數字
