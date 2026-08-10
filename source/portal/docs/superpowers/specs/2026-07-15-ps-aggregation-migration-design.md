# PS Aggregation Migration Design Spec

## 背景

`StudentPopulationController.SumPHPopulation` 目前對 PS（`StudentPopulationType.PS`，`Type=3`，百世／南區）的加總計算，是寫在 `classGroup` 迴圈內一段依 `Department.Id`／`Course.Id` 判斷的 if/else（`StudentPopulationController.cs:1477-1494`），迴圈外沒有 PS 專屬的後處理（`else if (Type == PS) { }` 是空的）。

這次要比照 GEPT（commit `0e8b4dc`）、PH（commit `4a8db34`）的模式，把 PS 的加總邏輯遷移到共用的 `AggregationEngine`（`source/portal/Portal/Services/Aggregation/AggregationEngine.cs`）。

PS（Type=3）共有 37 個課程，其中 16 個是加總課程（`IsSum=1`）：

| Id | 名稱 | 班系 | Ordinal |
|---|---|---|---|
| 120 | 國小班人數合計 | 國小班(22) | 119 |
| 127 | 國中班人數合計 | 國中班(23) | 126 |
| 131 | 高中班人數合計 | 高中班(24) | 130 |
| 132 | PS數學總人數 | PS統計(25) | 131 |
| 133 | PS數學開班數 | PS統計(25) | 132 |
| 134 | PS數學班平均人數 | PS統計(25) | 133 |
| 135 | 百倍速總人數 | 分析(26) | 134 |
| 136 | 百世數學去年同期 | 分析(26) | 135 |
| 137 | 百倍速去年同期 | 分析(26) | 136 |
| 138 | PS上週人數 | 分析(26) | 137 |
| 139 | 百倍速上週人數 | 分析(26) | 138 |
| 140 | 本週PS新生人數 | 分析(26) | 139 |
| 141 | 本週PS流失人數 | 分析(26) | 140 |
| 142 | 本週變更(PS+百倍速) | 分析(26) | 141 |
| 143 | 本週總詢問人數 | 統計(13) | 142 |
| 144 | 總人數(PS+百倍速) | 統計(13) | 143 |

目前舊程式碼**只計算其中 7 個**（120/127/131/132/133/134/144），其餘 9 個（135-143）程式碼註解寫「Manual items, skip auto-calculation」，完全不動它們，存檔時維持載入時的舊值。

## 資料調查發現（2026-07-15，比對 dev DB 97 筆 PS 人數表真實資料）

1. **課程144「總人數(PS+百倍速)」= 課程132「PS數學總人數」+ 課程135「百倍速總人數」**，跨校跨週資料完全吻合（例如47+8=55、34+2=36、35+2=37，查了學校12、19多週皆成立）。但**現行程式碼把132跟144當成同一個公式**（都只加總部門22/23/24，完全沒把135算進去）——這是一個既有 bug：如果這段程式碼真的被存檔觸發執行，144 會被錯誤覆蓋成跟132一樣的值，把百倍速的人數洗掉。這次遷移一併修正。
2. **課程138「PS上週人數」= 上一週的課程132數值**，跨週資料完全吻合（例如週17的132=34，週18的138=34）。不同於 PH 的課程34/61（原本是永遠死值0），PS 的138**目前已經有正確的真實數值**，只是靠人工填寫維持正確，這次把它改成自動計算（`LastWeekValue`），行為上不應該造成落差。
3. **課程142「本週變更(PS+百倍速)」= 本週144 − 上週144**（唯一可查資料：37−36=1，吻合）。
4. **課程135/136/137/139（百倍速系列）確認為人工手填**（無底層原始班級可加總——PS 的37個課程裡沒有任何一個班系代表「百倍速」，132能加總是因為有22/23/24這些原始年級班級，135則是每週由分校直接輸入一個數字）。這次維持 `ManualInput`，行為不變。
5. **PS 沒有 PH 式的「小/三」班別分組**：`Class.Type` 在 PS 底下絕大多數是 `General`（一般），僅有零星幾筆歷史噪音資料是 `Group`（團）。舊程式碼對 120/127/131/132/133/134 的計算完全沒有依 `Class.Type` 篩選，這次配置全部 `GroupByClassType = false`（不分班別）。

## 規則對應表（16 個加總課程）

| Id | 名稱 | StatisticsType | SourceDepartmentIds / SourceCourseIds | GroupByClassType |
|---|---|---|---|---|
| 120 | 國小班人數合計 | `SumByDepartment`(1) | — | false |
| 127 | 國中班人數合計 | `SumByDepartment`(1) | — | false |
| 131 | 高中班人數合計 | `SumByDepartment`(1) | — | false |
| 132 | PS數學總人數 | `SumBySourceDepartments`(3) | `[22,23,24]` | false |
| 133 | PS數學開班數 | `CountClasses`(30) | `[22,23,24]` | false |
| 134 | PS數學班平均人數 | `Average`(60，**引擎新實作**) | `[22,23,24]` | false |
| 138 | PS上週人數 | `LastWeekValue`(40) | `[22,23,24]`（同132） | false |
| 142 | 本週變更(PS+百倍速) | `DiffWithLastWeek`(10) | `[132,135]`（**SourceCourseIds**） | false |
| 144 | 總人數(PS+百倍速) | `SumBySourceCourses`(4) | `[132,135]`（**SourceCourseIds**） | false |
| 135 | 百倍速總人數 | `ManualInput`(50) | — | — |
| 136 | 百世數學去年同期 | `ManualInput`(50) | — | — |
| 137 | 百倍速去年同期 | `ManualInput`(50) | — | — |
| 139 | 百倍速上週人數 | `ManualInput`(50) | — | — |
| 140 | 本週PS新生人數 | `ManualInput`(50) | — | — |
| 141 | 本週PS流失人數 | `ManualInput`(50) | — | — |
| 143 | 本週總詢問人數 | `ManualInput`(50) | — | — |

`120/127/131` 用 `SumByDepartment` 而非 `SumByDepartmentAndClassType`（PH 的 9/16/21/48 用的類型），因為 PS 不分班別（見上方發現5）。

## 引擎需要的 3 項擴充（跟 GEPT/PH「不需要新增引擎程式碼」不同）

### 1. 新增 `Average` case

`StatisticsType.Average`（值60）已經定義在 enum 裡但 `AggregationEngine.Calculate` 的 switch 從未實作過，目前會落到 `default` 丟 `NotSupportedException`。新增：

```csharp
case StatisticsType.Average: {
    var src = GetSourceItems(course, item, population.Items).ToList();
    int count = src.Count(i => i.Number > 0);
    item.Number = count > 0 ? src.Sum(i => i.Number) / count : 0;
    break;
}
```

語意跟舊程式碼（`StudentPopulationController.cs:1489-1492`）一致：`count` 只算 `Number > 0` 的班級數，`total/count` 整數除法無條件捨去，`count=0` 時回傳 0（避免除以0）。

### 2. `GetSourceItems` 調整：`SourceCourseIds` 不再排除加總課程本身

目前 `GetSourceItems` 一開始就套用 `items.Where(i => i.Class?.Course?.IsSum != true)`，不管走哪個分支都排除加總課程。這次要讓 `SourceCourseIds` 這個分支**不排除**（因為144需要直接把132、135這兩個「加總課程」的當前值加起來），`SourceDepartmentIds` 跟課程自身 `DepartmentId` 的兩條路徑維持原樣（繼續排除加總課程，這是GEPT/PH現有規則正確運作所仰賴的行為，不能動）。

```csharp
private static IEnumerable<StudentPopulationItem> GetSourceItems(
    Course course, StudentPopulationItem contextItem, IEnumerable<StudentPopulationItem> items) {
    IEnumerable<StudentPopulationItem> query = items;

    if (!string.IsNullOrEmpty(course.SourceDepartmentIds)) {
        query = query.Where(i => i.Class?.Course?.IsSum != true);
        var ids = ParseIntArray(course.SourceDepartmentIds);
        query = query.Where(i => i.Class?.Course?.DepartmentId != null && ids.Contains(i.Class.Course.DepartmentId.Value));
    }
    else if (!string.IsNullOrEmpty(course.SourceCourseIds)) {
        var ids = ParseIntArray(course.SourceCourseIds);
        query = query.Where(i => i.Class?.CourseId != null && ids.Contains(i.Class.CourseId.Value));
        // 刻意不排除 IsSum：PS 課程144/142 需要直接加總其他「加總課程」(132/135) 的當前值
    }
    else if (course.DepartmentId.HasValue) {
        query = query.Where(i => i.Class?.Course?.IsSum != true);
        query = query.Where(i => i.Class?.Course?.DepartmentId == course.DepartmentId.Value);
    }

    if (course.ApplicableClassType.HasValue) {
        query = query.Where(i => i.Class?.Type == course.ApplicableClassType.Value);
    }
    else if (course.GroupByClassType) {
        var classType = contextItem.Class?.Type;
        query = query.Where(i => i.Class?.Type == classType);
    }

    return query;
}
```

**影響範圍確認：** GEPT、PH 目前所有已設定的規則都只用 `SourceDepartmentIds`，沒有任何規則用到 `SourceCourseIds`，所以這個改動對它們零風險（分支邏輯不變）。

### 3. `classGroup` 迴圈補上依 `Ordinal` 排序

`SumPHPopulation` 裡 `classGroup`（`StudentPopulationController.cs:1403`）目前是：

```csharp
var classGroup = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.IsSum).ToList();
```

沒有任何排序保證。PH/GEPT 的規則彼此獨立（互不依賴其他加總課程算完的值），所以原本沒排序也不影響正確性。但 **PS 的課程144（`SourceCourseIds=[132,135]`）需要132已經算出最新值**，132 的 `Ordinal`(131) 小於 144 的 `Ordinal`(143)，142 的 `Ordinal`(141) 也大於132(131)跟135(134)——只要迴圈照 `Ordinal` 遞增順序跑，依賴關係就會被滿足。改成：

```csharp
var classGroup = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.IsSum).OrderBy(e => e.Class.Course.Ordinal).ToList();
```

對 GEPT/PH 是無害的（它們的規則不互相依賴，排序後結果不變），純粹是替 PS 補上正確性保證，也讓未來任何型別要用同樣「課程A依賴課程B」的模式時有一致的順序保證。

## 已知風險／需要驗證的地方

- **與 PH 課程67相同類型的風險**：PH 遷移時發現有髒資料（不同 `StudentPopulationType` 的 `Class` 錯誤關聯到某個人數表）。PS 是否也有類似髒資料需要在比對測試階段查核。
- **`CourseDepartment` Id 跨型別重複使用**：查資料庫發現 `CourseDepartment.Id=13`（"統計"）底下的 `Type` 欄位是 `0`（對應 PH），但 PS 的課程143/144 也指向這個部門 Id。`GetSourceItems` 的篩選是在單一 population 的 `Items` 集合內做，同一個 population 只會有同一個 `Type` 的課程，所以理論上不受影響；但這代表 `CourseDepartment` 的 `Id` 並非嚴格依 `Type` 分區，日後新增規則時要留意，不要單純以為 DepartmentId 就能唯一識別型別。
- **測試預期**：跟 PH 需要區分「預期例外」不同，這次資料調查顯示 132/138/142/144 這些課程**現有真實資料本身就已經符合新公式**，理論上比對測試應該 0 落差（不像 PH 有課程 22/34/35/49/61/62/67 那樣的既有落差）。仍然要實際寫比對測試驗證 97 筆 PS 人數表，不能只憑推理假設 0 落差。

## 程式碼改動方式

比照使用者這次決定：**PS 分支的舊 if/else（`classGroup` 迴圈內 1477-1494 行）整段用 `#if false` 保留、不刪除**，加上一行呼叫引擎：

```csharp
else if (studentPopulationData.Type == StudentPopulationType.PS) {
#if false // 舊 PS 加總邏輯，2026-07-15 遷移到 AggregationEngine 時停用保留（不刪除），
          // 見 docs/superpowers/plans/2026-07-15-ps-aggregation-migration.md
    int psDeptId = group.Class.Course.Department.Id;
    ...（原始程式碼原樣保留）
#endif
    aggregationEngine.Calculate(group, studentPopulationData);
}
```

迴圈外 `else if (Type == PS) { }` 本來就是空的，不需要改動。

## 測試方式

沿用 GEPT/PH 驗證模式：寫一支 `[Explicit]` NUnit 測試（比照 `PhAggregationComparisonTests.cs`），直接接 dev 的 `DataContext`，抓 PS 所有已存在的人數表資料（97筆），逐筆比對「舊 if/else 算出來的 Number」vs「新引擎算出來的 Number」：

- 9 個引擎計算課程（120/127/131/132/133/134/138/142/144）：目標 0 落差
- 7 個人工課程（135/136/137/139/140/141/143）：不比對（引擎對 `ManualInput` 是 no-op，值維持不變，本來就不會有落差）
- 若出現非預期落差，要先查是不是跟 PH 課程67類似的髒資料問題，不能直接擴大「預期例外」名單搪塞過去

## Out of scope（本次不處理）

- PSJ、AfterSchool 型別的遷移（各自獨立後續計畫）
- GEPT 最終審查記錄的5項問題、PH 最終審查記錄的3項問題（已記錄在 progress ledger，之後處理）
- 刪除 `#if false` 保留的舊碼（比照 PH，等瀏覽器驗證通過後另開任務處理）
- `CourseDepartment.Id` 跨型別共用的資料整理（本次僅記錄風險，不修資料）
