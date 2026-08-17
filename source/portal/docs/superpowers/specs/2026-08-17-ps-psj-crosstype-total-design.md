# PS報表PSJ相關欄位改接真實PSJ資料 — 設計規格

## 背景

08-17稽核（見對話記錄）確認PS報表（`StudentPopulationType.PS`，分析班系Id26）三個PSJ相關欄位目前的計算方式：

| Id | 名稱 | IsSum | StatisticsType | 現況 |
|---|---|---|---|---|
| 135 | PSJ總人數 | `0` | `4`(SumBySourceCourses，`SourceCourseIds=[157]`) | 因IsSum=0，公式從未生效（`AggregationEngine.Calculate`一開頭就因`!course.IsSum`直接return）；來源課程157本身也是PSJ舊班系死碼「數學班統計」(Type=PSJ非PS)。實務上是分校人工鍵入 |
| 139 | PSJ上週人數 | `1` | `NULL` | 人工輸入，系統不自動算 |
| 142 | 本週變更(PS+PSJ) | `1` | `NULL` | 人工輸入，系統不自動算 |

分校目前是自己另外開PSJ報表查數字，再手動抄一份到PS報表這三欄，容易抄錯或忘記更新。這次要讓這三欄改成自動抓PSJ報表的真實資料，減少人工謄寫。

使用者已確認：
- 資料來源＝PSJ報表「百倍速分析總覽」班系（`CourseDepartmentId=47`）底下12個年級的「本週XX總人數」課程（Id 562–573，各自＝該年級數學班+理化班人數），加總即為「PSJ當週總人數」。
- 三個欄位（135/139/142）全部自動化。
- 不回填歷史週次的舊資料，只套用到之後新填寫/重新開啟儲存的週次。

## 現有架構限制

`AggregationEngine`（`Portal/Services/Aggregation/AggregationEngine.cs`）目前所有計算類型（`SumBySourceCourses`／`DiffBetweenCourses`／`LastWeekValue`…）都只在**同一個`StudentPopulation.Items`集合**（同校、同週、同`Type`）裡查找來源項目。PS報表要抓PSJ報表（不同`Type`）的資料，目前完全沒有對應機制，純DB設定改不出來，需要異動`AggregationEngine`本身。

`_lookupPopulation`委派本身其實已經支援任意`StudentPopulationType`參數（`Func<int,int,int,StudentPopulationType,StudentPopulation>`，見`SumLastYear`/`SumYearToDate`呼叫方式），只是目前沒有任何`StatisticsType`用它去查「別的Type」。真正卡住的是「上週」的查詢：`_lookupLastWeekPopulation`委派簽章是`Func<StudentPopulation, StudentPopulation>`，其實作`StudentPopulationController.LookupLastWeekPopulation`內部寫死`p.Type == population.Type`，沒有辦法指定查別的Type。

## 設計方案

### 1. `StatisticsType`新增2個列舉值（`schema/Data/Content/StatisticsType.cs`）

```csharp
/// <summary>
/// 指定來源課程加總（來自另一種報表類型的「本週」資料）
/// </summary>
SumFromOtherType = 15,

/// <summary>
/// 指定來源課程加總（來自另一種報表類型的「上週」資料）
/// </summary>
LastWeekValueFromOtherType = 16,
```

### 2. `Course`新增欄位（`schema/Data/Content/Course.cs`）

```csharp
/// <summary>
/// 統計來源報表類型（跨Type抓值專用，例如PS抓PSJ當週/上週資料）
/// </summary>
[Display(Name = "統計來源報表類型"), DataMember]
public StudentPopulationType? SourceStudentPopulationType { get; set; }
```
需要一支EF Core migration（比照`20260730010513_AddCourseNegativeSourceCourseIds.cs`），在`Course`表新增`SourceStudentPopulationType`（`smallint`, nullable）欄位。

### 3. `AggregationEngine.cs`

- 建構子第二個委派簽章從`Func<StudentPopulation, StudentPopulation>`改成`Func<StudentPopulation, StudentPopulationType, StudentPopulation>`，並改名`_lookupLastWeekPopulationOfType`以求清楚。
- `SumLastWeek`內部呼叫改成`_lookupLastWeekPopulationOfType(population, population.Type)`（行為不變，只是明確帶入自己的Type）。
- `Compute()`新增2個case：

```csharp
case StatisticsType.SumFromOtherType: {
    if (population.SchoolId == null || course.SourceStudentPopulationType == null) return 0;
    var sourcePopulation = _lookupPopulation(population.Year, population.Week, population.SchoolId.Value, course.SourceStudentPopulationType.Value);
    if (sourcePopulation?.Items == null) return 0;
    var ids = ParseIntArray(course.SourceCourseIds);
    return GetItemsByCourseIds(course, item, sourcePopulation.Items, ids).Sum(i => i.Number);
}
case StatisticsType.LastWeekValueFromOtherType: {
    if (population.SchoolId == null || course.SourceStudentPopulationType == null) return 0;
    var sourceLastWeekPopulation = _lookupLastWeekPopulationOfType(population, course.SourceStudentPopulationType.Value);
    if (sourceLastWeekPopulation?.Items == null) return 0;
    var ids = ParseIntArray(course.SourceCourseIds);
    return GetItemsByCourseIds(course, item, sourceLastWeekPopulation.Items, ids).Sum(i => i.Number);
}
```

兩者都重用既有的`GetItemsByCourseIds`（不排除IsSum項目，符合PSJ 562-573本身就是加總課程的情況）。

「本週變更(PS+PSJ)」（142）**不需要新機制**：135/139算好之後就是PS自己`population.Items`裡的普通值，142可以直接沿用既有的`DiffBetweenCourses`(12)：`SourceCourseIds=[132,135]` − `NegativeSourceCourseIds=[138,139]`。

### 4. `StudentPopulationController.cs`（僅此一個檔案有2個呼叫點）

`LookupLastWeekPopulation`加一個可選參數：

```csharp
private static StudentPopulation LookupLastWeekPopulation(DataContext dataContext, StudentPopulation population, StudentPopulationType? type = null) {
    if (population?.SchoolId == null) return null;
    var targetType = type ?? population.Type;
    SchoolYear lastSchoolYear = population.Week > 1
        ? dataContext.SchoolYear.Where(e => e.Year == population.Year && e.Week == population.Week - 1).OrderBy(e => e.Id).FirstOrDefault()
        : dataContext.SchoolYear.Where(e => e.Year == population.Year - 1).OrderByDescending(e => e.Week).ThenByDescending(e => e.Id).FirstOrDefault();
    if (lastSchoolYear == null) return null;
    return dataContext.StudentPopulation.Include("Items.Class.Course")
        .FirstOrDefault(p => p.Year == lastSchoolYear.Year && p.Week == lastSchoolYear.Week && p.SchoolId == population.SchoolId && p.Type == targetType);
}
```

兩個`new AggregationEngine(...)`呼叫點（`SumPHPopulation`、預覽用的`previewEngine`）的第二個委派從`p => LookupLastWeekPopulation(dataContext, p)`改成`(p, type) => LookupLastWeekPopulation(dataContext, p, type)`。

### 5. Course資料設定（正式環境DB，SQL apply script）

| Id | 名稱 | IsSum | StatisticsType | SourceCourseIds | NegativeSourceCourseIds | SourceStudentPopulationType |
|---|---|---|---|---|---|---|
| 135 | PSJ總人數 | `1`（**由0改1**） | `15`(SumFromOtherType) | `[562,563,564,565,566,567,568,569,570,571,572,573]` | — | `1`(PSJ) |
| 139 | PSJ上週人數 | `1`（不變） | `16`(LastWeekValueFromOtherType) | `[562,563,564,565,566,567,568,569,570,571,572,573]` | — | `1`(PSJ) |
| 142 | 本週變更(PS+PSJ) | `1`（不變） | `12`(DiffBetweenCourses) | `[132,135]` | `[138,139]` | — |

查過現有資料：135/139/142目前存的`StudentPopulationItem`全部167筆`IsManual=0`，代表沒有任何一筆是「凍結」的手動覆蓋值，之後只要該週PS報表被重新開啟/存檔，就會照新公式全部重算蓋過去（符合「不特別回填但之後存檔就套用新值」的決定）。

## Course 135「IsSum: 0→1」的UX影響（使用者已確認接受）

`PopulationPartialView.cshtml`依`Class.Course.IsSum`決定該欄位是「可直接輸入」還是「唯讀顯示計算值」。135目前`IsSum=0`，畫面上是分校可以手動打字的輸入框；改成`IsSum=1`之後對一般分校使用者會變成跟138/142一樣的唯讀計算欄，**分校之後不能再手動修改「PSJ總人數」這格，數字要改要去改PSJ報表**。

擁有`PopulationWeekSwitch`權限的使用者（例如管理員）不受影響：07-17已上線的「管理員合計欄位編輯」機制（見`project-population-sum-edit-pending`記憶）本來就允許直接編輯任何`IsSum=true`欄位，編輯時自動標記`IsManual=true`凍結成手動值，之後可用既有的`RevertToAutoCalculation`一鍵解凍回自動計算。135變成`IsSum=1`後直接套用同一套既有機制，不需要額外開發。

## 已知風險與限制

1. **跨報表存檔順序依賴**：135/139是查詢「當下已存檔」的PSJ population算出來的。若該校當週PSJ報表還沒建立/存檔，135/139會查不到資料而算成`0`（142連帶也會用到錯誤的135/139值）。
2. **非即時連動**：PSJ報表事後修改不會自動反映到已存檔的PS報表；PS報表要重新開啟並存檔一次，135/139/142才會重新抓最新的PSJ數字。這跟現有`LastYearValue`/`YearToDateSum`等「讀取其他已存population」的欄位限制一致，非本次新增的問題。
3. 未涵蓋「PSJ報表還沒填時要不要跳警示提醒分校」這種主動提示——本次不做，之後如有需要可以參考07-30「上週資料缺失提示」的模式另外設計。

## 測試方式

- 新增/擴充NUnit `[Explicit]`比對測試，比照既有`PhAggregationComparisonTests`模式：找PS報表+對應PSJ報表都存在的學校/週次，驗證135/139/142自動算出的值符合預期公式。
- 手動情境測試（需瀏覽器）：
  1. 開一個新週次的PSJ報表，填好12年級的數學/理化班人數並存檔。
  2. 開同校同週的PS報表，確認135/139/142自動帶出正確值，且135格已變成唯讀。
  3. 修改PSJ報表任一年級人數並重新存檔，回到PS報表重新開啟，確認135/139/142有跟著更新。
  4. 情境：PSJ報表該週還沒建立時開PS報表，確認135/139顯示0且不會噴例外。

## Out of scope（本次不處理）

- 歷史週次135/139/142舊資料的回填/重算（使用者已確認不用）。
- PSJ報表未填寫時的主動警示/擋存檔機制。
- AS（課輔）報表是否有類似「抄別的報表數字」的欄位需要一併處理（未盤點，之後如有需要另開任務）。
