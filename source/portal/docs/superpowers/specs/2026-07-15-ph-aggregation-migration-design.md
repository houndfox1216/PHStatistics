# PH（全國人數表）加總引擎遷移 設計文件

**日期**：2026-07-15
**狀態**：設計確認，待撰寫實作計畫
**範圍**：把 `StudentPopulationController.SumPHPopulation` 裡 `StudentPopulationType.PH` 分支的 if/else 加總邏輯，改用共用的 `AggregationEngine` 計算，比照已完成並瀏覽器驗證通過的 GEPT 遷移模式

---

## 背景

`AggregationEngine`（`Portal/Services/Aggregation/AggregationEngine.cs`）與 Admin 後台規則設定介面已於 GEPT 遷移時完成並上線（commit 0e8b4dc），GEPT 的 13 個加總課程已用引擎計算並經 902 筆真實資料驗證 0 落差、瀏覽器實測通過。本次接續遷移 PH（全國人數表），是繼 GEPT 之後第二個型別；PS/PSJ/AfterSchool 仍維持舊邏輯，留待後續個別遷移。

PH 目前的加總邏輯分兩塊，都在 `SumPHPopulation` 方法內：

1. **`classGroup` 迴圈內的 PH 分支**（`StudentPopulationController.cs:1408-1449`）：預設「同班系+同班別加總」，另外對「英文個別指導」「國語文個別指導」「英文合作開班」「國語文合作開班」四個課程用課程名稱字串比對做覆寫（改成不分班別、依部門名稱重新查詢）。
2. **迴圈外的 PH 專屬後處理**（`StudentPopulationController.cs:1519-1679`，約 160 行）：計算英文/國文總班數（用 `Course.Id` 邊界值 `e.Id <= 38`／`38 < e.Id <= 58` 篩選課程，註解寫著「如果有重新匯入課程要調整對應Id」）、本週英語文/國語文總人數、英文/國文「與上週相比」（**即時跨週查詢**上週那張人數表的對應課程值，不是讀 `LastWeekNumber` 快取欄位）、英文/國文「去年同期/比」（**即時跨年度查詢**）、總人數（全體不分部門加總）。

這兩塊加起來約 260 行，是這次要用引擎規則取代的範圍。

### 額外發現：「上週英語文總人數」「上週國語文總人數」是從未被計算的死欄位

實際查詢 dev 資料庫（`NewPAS07`）確認：Course.Id 34（上週英語文總人數）與 61（上週國語文總人數）的 `Number`／`LastWeekNumber` 在所有已存資料裡都是 0，程式碼裡也確實找不到任何地方會寫入這兩個課程的值。使用者確認：這兩個課程的原始設計意圖應該是「繼承上週那張人數表的『本週英語文/國語文總人數』」，屬於過去實作時的疏漏，這次遷移一併修正（詳見下方規則表 Id 34/61）。

---

## 引擎能力盤點結論：不需要新增任何 StatisticsType

逐一比對 24 個 PH 加總課程的現有邏輯後確認：**現有 6 種已實作的 StatisticsType（`SumByDepartment`/`SumByDepartmentAndClassType`/`SumBySourceDepartments`/`CountClassesByClassType`/`DiffWithLastWeek`/`DiffWithLastYear`/`LastWeekValue`/`ManualInput`）已完全足夠**，不需要修改 `AggregationEngine.cs`。關鍵原因：

- 「英文/國文個別指導」「英文/國文合作開班」四個課程的名稱覆寫，本質上就是「來源部門＝課程自己的部門，但不分班別」，用 `SourceDepartmentIds`（或留空用自身 `DepartmentId`）+ `GroupByClassType=false` 即可表達，不需要新機制
- 「與上週相比」「去年同期/比」原本的即時跨週/跨年查詢，可以重新表達成「來源部門＝該科目的原始課程部門清單」+ `DiffWithLastWeek`/`DiffWithLastYear`（讀取來源項目的 `LastWeekNumber` 快取欄位），跟 GEPT 課程 90/92 的做法完全一致。這個快取欄位在每次開啟人數表頁面時都會依 `Class.Id` 重新同步（`StudentPopulationController.cs:216-220`），理論上與即時查詢等價，但**必須靠比對測試驗證，不能只憑推理**
- 「總人數」（全體不分部門加總）不需要新增「忽略部門」模式：直接把 `SourceDepartmentIds` 明確列出全部 8 個原始部門即可，繞開「自身 DepartmentId」的預設 fallback
- 「上週英語文/國語文總人數」補上規則後，用 `LastWeekValue` 對相同的來源部門清單計算即可，同樣不需要新機制

---

## 24 個 PH 加總課程規則對照表

英文原始（非加總）部門：`[1,2,3,5,6]`（英文國小班/國中班/高中班/個別指導/合作開班）
國文原始（非加總）部門：`[8,10,11]`（國語文/國語文個別指導/國語文合作開班）
全體原始部門（供「總人數」use）：`[1,2,3,5,6,8,10,11]`

| Id | 課程名稱 | 自身部門 | StatisticsType | SourceDepartmentIds | GroupByClassType |
|---|---|---|---|---|---|
| 9 | 英文國小人數合計 | 1 英文國小班 | SumByDepartmentAndClassType(2) | （用自身部門） | true |
| 16 | 英文國中人數合計 | 2 英文國中班 | SumByDepartmentAndClassType(2) | （用自身部門） | true |
| 21 | 英文高中人數合計 | 3 英文高中班 | SumByDepartmentAndClassType(2) | （用自身部門） | true |
| 22 | 英文總班數統計 | 4 英文統計 | CountClassesByClassType(31) | [1,2,3] | true |
| 27 | 英文個別指導人數合計 | 5 英文個別指導 | SumByDepartment(1) | （用自身部門） | false |
| 32 | 英文合作開班人數合計 | 6 英文合作開班 | SumByDepartment(1) | （用自身部門） | false |
| 33 | 本週英語文總人數 | 4 英文統計 | SumBySourceDepartments(3) | [1,2,3,5,6] | false |
| 34 | 上週英語文總人數 | 4 英文統計 | **LastWeekValue(40)**（補疏漏，原本永遠是死值 0） | [1,2,3,5,6] | false |
| 35 | 與上週相比 | 7 英文分析 | DiffWithLastWeek(10) | [1,2,3,5,6] | false |
| 36 | 去年同期/比 | 7 英文分析 | DiffWithLastYear(11) | [1,2,3,5,6] | false |
| 37 | 本週英語文新生 | 7 英文分析 | ManualInput(50) | — | — |
| 38 | 本週英語文流失 | 7 英文分析 | ManualInput(50) | — | — |
| 48 | 國語文人數合計 | 8 國語文 | SumByDepartmentAndClassType(2) | （用自身部門） | true |
| 49 | 國文總班數 | 9 國語文統計 | CountClassesByClassType(31) | [8] | true |
| 54 | 國語文個別指導人數合計 | 10 國語文個別指導 | SumByDepartment(1) | （用自身部門） | false |
| 59 | 國語文合作開班人數合計 | 11 國語文合作開班 | SumByDepartment(1) | （用自身部門） | false |
| 60 | 本週國語文總人數 | 9 國語文統計 | SumBySourceDepartments(3) | [8,10,11] | false |
| 61 | 上週國語文總人數 | 9 國語文統計 | **LastWeekValue(40)**（補疏漏） | [8,10,11] | false |
| 62 | 與上週相比 | 12 國語文分析 | DiffWithLastWeek(10) | [8,10,11] | false |
| 63 | 去年同期/比 | 12 國語文分析 | DiffWithLastYear(11) | [8,10,11] | false |
| 64 | 本週國語文新生人數 | 12 國語文分析 | ManualInput(50) | — | — |
| 65 | 本週國語文流失人數 | 12 國語文分析 | ManualInput(50) | — | — |
| 66 | 本週總詢問(填單)人數 | 13 統計 | ManualInput(50)（現況本就是每次存檔時直接 `continue` 跳過，等同人工） | — | — |
| 67 | 總人數 | 13 統計 | SumBySourceDepartments(3) | [1,2,3,5,6,8,10,11]（全部原始部門） | false |

「個別指導」「合作開班」六個課程一律 `GroupByClassType=false`（不分小/三班別，兩種班別加總成同一數字），這是沿用現有覆寫邏輯的實際行為，非新假設。

---

## 已確認的行為變更（相對於目前 if/else 的落差，皆為刻意接受）

1. **英文/國文總班數（Id 22/49）**：從「符合條件的項目筆數（含人數=0的班）」改為跟 GEPT 一致的「`Number > 0` 的筆數」。若實際資料存在人數=0 但班級仍存在的情況，比對測試會出現落差，這是預期、正確的落差，不是 bug。
2. **上週英語文/國語文總人數（Id 34/61）**：從「永遠是 0（死欄位）」改為「真正等於上週的本週英文/國文總人數」（`LastWeekValue`）。比對測試會看到舊值 0、新值非 0 的落差，這是本次要修正的疏漏，同樣是預期落差。

其餘 22 個課程理論上應該 0 落差；若出現非上述兩類的落差，代表規則設定或引擎理解有誤，需要回頭檢討。

---

## 程式碼改動方式

比照使用者本次決定：**先把 `SumPHPopulation` 的 PH 分支（`classGroup` 迴圈內 1408-1449 行 + 迴圈外 PH 專屬後處理 1519-1679 行）整段註解保留，不刪除**，改成呼叫引擎：

```csharp
else if (studentPopulationData.Type == StudentPopulationType.PH) {
    aggregationEngine.Calculate(group, studentPopulationData);
}
```

因為新規則全部直接指向原始（非加總）課程的部門，不依賴其他聚合課程算完的結果，24 個課程之間沒有計算順序依賴，`classGroup` 迴圈原本的遍歷順序（不特別排序）足以正確計算全部規則，不需要額外處理順序問題。

待比對測試 0 落差（扣除上述兩項預期落差）驗證通過、且瀏覽器實測沒問題後，才在**另一次**任務中把註解掉的舊碼真正刪除（比照 GEPT：本次不刪除舊碼、不刪除死碼服務）。

---

## 測試方式

沿用 GEPT 驗證模式：寫一支 `[Explicit]` NUnit 測試（比照 `GeptAggregationComparisonTests.cs`），直接接 dev 的 `DataContext`，抓 PH 所有已存在的人數表資料，逐筆比對「舊 if/else 算出來的 Number」vs「新引擎算出來的 Number」：

- 22 個課程（扣除 Id 22/49/34/61）：目標 0 落差
- Id 22/49：允許因 `Number>0` 篩選條件變更產生的落差，測試需要分開統計、標註為「預期變更」而非失敗
- Id 34/61：預期看到舊值 0、新值非 0，同樣標註為「預期變更（修正疏漏）」而非失敗

---

## Out of scope（本次不處理）

- PS / PSJ / AfterSchool 的遷移，留待各自獨立的後續計畫
- GEPT 最終審查留下的 5 項待處理問題（91/92 去年同期行為驗證、Admin 欄位命名、JSON 格式錯誤靜默失敗、revert 腳本註記、文件數字校正）維持原狀，不在本次範圍
- 死碼服務（`StatisticsCalculationService.cs`、`ReportExportService.ComputeIsumValue`/`InferStatisticsType`）的刪除時機仍是全部 5 型別遷移驗證通過後，不在本次處理
- 舊 PH if/else 程式碼的正式刪除（本次只註解保留，刪除留待下一次任務）
