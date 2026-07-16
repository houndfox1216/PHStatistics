# 管理員編輯範圍擴大至 IsSum 合計欄位 Design Spec

## 背景

`docs/superpowers/plans/2026-07-16-population-admin-edit.md`（已完成並推送，commit `681a782`..`d97c5a5`）讓 `SystemPermission.PopulationWeekSwitch` 權限的使用者可以編輯已送出週次的本週人數/上週人數/備註/班別，並接上審計 log。

這次要再擴大：讓管理員也能編輯**自動計算的合計欄位**（`Course.IsSum = true` 且 `StatisticsType` 不是 `ManualInput` 的項目，例如「本週數學總人數合計」「本週一年級與上週相比」這類目前唯讀、由 `AggregationEngine` 自動算出的欄位）。使用情境（使用者確認）：管理員的校正動作發生在分校人員輸入「前」或人數表送出「後」，不會跟一般使用者同時編輯同一份資料，不需要處理併發衝突。

使用者的核心訴求：
1. 改了就直接存檔（跟現有本週人數/上週人數一樣，不用彈窗確認）。
2. 改過的值不能被下一次自動重算蓋掉。
3. 但如果之後系統試算出來的值跟手動值不同，要能讓管理員知道並選擇是否改用系統試算值。

## 範圍

### A. 核心機制：`IsManual` 釘住

`StudentPopulationItem.IsManual`（`schema/Data/Content/StudentPopulationItem.cs:135`）已經是真正有資料庫欄位的 `bool`（不是死碼，不需要 migration），只是目前完全沒有程式碼在讀寫它。

**`UpdateClassItem`** 判斷邏輯（`StudentPopulationController.cs`，目前簽章 `UpdateClassItem(long sId, int? number, string studentRemark = null, int? lastWeekNumber = null)`）新增：

```csharp
bool isAutoComputedSum = item.IsSum
    && item.Class?.Course?.StatisticsType != null
    && item.Class.Course.StatisticsType != StatisticsType.None
    && item.Class.Course.StatisticsType != StatisticsType.ManualInput;
bool numberApplied = number.HasValue && (!isAutoComputedSum || canEditLocked);
```

取代原本的 `if (number.HasValue) { item.Number = number.Value; }`，改成：

```csharp
if (numberApplied) {
    item.Number = number.Value;
    if (isAutoComputedSum) {
        item.IsManual = true;
    }
}
```

原本兩處用 `number.HasValue` 判斷「是否要寫 log / 是否要觸發 `SumPHPopulation`」的地方，都改用 `numberApplied`（跟現有 `lastWeekApplied` 的寫法對稱，同一套「伺服器端二次防線」原則：沒權限的人對自動計算欄位送 `number` 參數會被直接忽略，不是報錯，維持跟 `lastWeekNumber` 一致的「靜默忽略」行為）。

**影響範圍確認**：
- 一般欄位（`IsSum=false`）：`isAutoComputedSum` 恆為 `false` → `numberApplied == number.HasValue`，跟現在完全一樣，沒有任何人被多限制。
- 現有 `ManualInput` 的 IsSum 欄位（新生/流失/本週總詢問(填單)人數）：`isAutoComputedSum` 恆為 `false`（因為 `StatisticsType == ManualInput` 被排除）→ 任何人都能繼續編輯，行為不變。
- 只有「本來是唯讀、自動算出來的合計欄」才會被這次新增的權限判斷擋下來。

**`AggregationEngine.Calculate`**（`Portal/Services/Aggregation/AggregationEngine.cs:23`）開頭加一行：

```csharp
public void Calculate(StudentPopulationItem item, StudentPopulation population) {
    if (item.IsManual) return;
    ...
```

**只有被標記 `IsManual` 的這一筆項目**會跳過重算，同一張表其他合計欄照常計算（若剛好有其他合計欄的來源包含這筆手動值，會讀到手動值往下加總——這是預期行為，等同這筆資料「暫時被人工釘住的真實數字」）。

### B. 拆分 `AggregationEngine`：純計算 vs 計算並寫入

現有 `Calculate` 方法內的 `switch (type.Value) { ... item.Number = ... }` 邏輯，拆成一個不寫入、純粹回傳試算值的私有方法 `Compute`：

```csharp
public void Calculate(StudentPopulationItem item, StudentPopulation population) {
    if (item.IsManual) return;
    var course = item.Class?.Course;
    if (course == null || !course.IsSum) return;
    var type = course.StatisticsType;
    if (type == null || type == StatisticsType.None || type == StatisticsType.ManualInput) return;
    item.Number = Compute(item, population, type.Value, course);
}

public int? Preview(StudentPopulationItem item, StudentPopulation population) {
    var course = item.Class?.Course;
    if (course == null || !course.IsSum) return null;
    var type = course.StatisticsType;
    if (type == null || type == StatisticsType.None || type == StatisticsType.ManualInput) return null;
    return Compute(item, population, type.Value, course);
}

private int Compute(StudentPopulationItem item, StudentPopulation population, StatisticsType type, Course course) {
    switch (type) {
        case StatisticsType.SumByDepartment:
        case StatisticsType.SumByDepartmentAndClassType:
        case StatisticsType.SumBySourceDepartments:
        case StatisticsType.SumBySourceCourses:
            return GetSourceItems(course, item, population.Items).Sum(i => i.Number);
        case StatisticsType.CountClasses:
        case StatisticsType.CountClassesByClassType:
            return GetSourceItems(course, item, population.Items).Count(i => i.Number > 0);
        case StatisticsType.LastWeekValue:
            return GetSourceItems(course, item, population.Items).Sum(i => i.LastWeekNumber);
        case StatisticsType.DiffWithLastWeek: {
            var src = GetSourceItems(course, item, population.Items);
            return src.Sum(i => i.Number) - src.Sum(i => i.LastWeekNumber);
        }
        case StatisticsType.LastYearValue:
            return SumLastYear(course, item, population);
        case StatisticsType.DiffWithLastYear: {
            int thisWeek = GetSourceItems(course, item, population.Items).Sum(i => i.Number);
            return thisWeek - SumLastYear(course, item, population);
        }
        case StatisticsType.Average: {
            var src = GetSourceItems(course, item, population.Items).ToList();
            int count = src.Count(i => i.Number > 0);
            return count > 0 ? src.Sum(i => i.Number) / count : 0;
        }
        default:
            throw new NotSupportedException(
                $"AggregationEngine 尚未支援 StatisticsType.{type}（課程 {course.Id} {course.Name}）。");
    }
}
```

**這是純重構，不改變任何既有計算邏輯**——`Calculate` 的行為（除了新增的 `IsManual` 跳過判斷）與現在完全一致，只是把 `switch` 內容抽出來給 `Preview` 共用。

### C. 差異提示 + 回復自動計算

**`StudentPopulationItem`**（`schema/Data/Content/StudentPopulationItem.cs`）新增一個不落地的預覽欄位（不需要 migration）：

```csharp
[NotMapped]
public int? PreviewNumber { get; set; }
```

**`StudentPopulationController.cs`** 新增私有方法（比照 `SumPHPopulation` 內建立 `AggregationEngine` 的既有寫法）：

```csharp
private void AttachManualPreviews(DataContext dataContext, StudentPopulation population) {
    if (population?.Items == null) return;
    var previewEngine = new AggregationEngine((year, week, schoolId, type) =>
        dataContext.StudentPopulation.Include("Items.Class.Course")
            .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type));
    foreach (var item in population.Items.Where(i => i.IsManual)) {
        item.PreviewNumber = previewEngine.Preview(item, population);
    }
}
```

呼叫時機：在每一個會把 `StudentPopulation`（含 `Items`）渲染成 `View`/`PartialView` 之前呼叫一次，涵蓋範圍：
- 5 個 `CreateXXXPopulation` action：最終 `return View(returnData);` 之前。
- `AddNewClass`：鎖定路徑（`return PartialView("PopulationPartialView", lockedData)`）與正常路徑（最終 `return PartialView("PopulationPartialView", returnData)`）都要呼叫——鎖定路徑（已送出週次）正是管理員最常需要看到「這筆被我手動改過、現在系統試算值是多少」的情境。
- `RemoveClassItem`：同樣鎖定路徑 + 正常路徑都要呼叫。
- `UpdateClassItem`：同樣鎖定路徑 + 正常路徑都要呼叫。
- `catch` 區塊回傳 `new StudentPopulation()` 的地方不需要呼叫（`Items` 是空的，`AttachManualPreviews` 的 `null` 防護本來就會直接跳過，呼叫與否結果一樣，為求簡潔可以不呼叫）。

**新增 action** `RevertToAutoCalculation`：

```csharp
[HttpPost]
public IActionResult RevertToAutoCalculation(long sId) {
    try {
        DataContext dataContext = new DataContext();
        StudentPopulationItem item = dataContext.StudentPopulationItem.Include("StudentPopulation").Include("Class.Course").FirstOrDefault(e => e.Id == sId);
        if (item == null)
            return Json(new { success = false, message = "找不到項目" });
        if (!User.HasPermission(SystemPermission.PopulationWeekSwitch))
            return Json(new { success = false, message = "沒有權限" });

        long spId = item.StudentPopulationId;
        int? classId = item.ClassId;
        string name = item.Name;
        int oldNumber = item.Number;
        int oldLastWeekNumber = item.LastWeekNumber;
        string oldStudentRemark = item.StudentRemark;

        item.IsManual = false;
        dataContext.SaveChanges();
        SumPHPopulation(spId);

        dataContext.ChangeTracker.Clear();
        var updated = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Id == sId);
        int newNumber = updated?.Number ?? oldNumber;

        WriteItemLog(dataContext, spId, classId, name, oldNumber, newNumber, oldLastWeekNumber, updated?.LastWeekNumber ?? oldLastWeekNumber, oldStudentRemark, updated?.StudentRemark ?? oldStudentRemark, oldRemark: "手動覆蓋", newRemark: "回復自動計算");

        return Json(new { success = true, number = newNumber });
    }
    catch (Exception ex) {
        return Json(new { success = false, message = ex.Message });
    }
}
```

（`SumPHPopulation(spId)` 用的是它自己內部另開的 `DataContext`，跟這裡的 `dataContext` 不是同一個連線追蹤範圍，所以呼叫完之後要 `dataContext.ChangeTracker.Clear()` 再重新查詢，才能拿到 `SumPHPopulation` 寫入之後的最新 `Number`——這個手法是抄既有的 `CreateXXXPopulation` 路徑 B 結尾「`SumPHPopulation` + `ChangeTracker.Clear()` + 重新查詢」那段既有寫法，不是新發明的模式。）

### 畫面調整

**`PopulationPartialView.cshtml`**（兩處，約 255-266 行、382-393 行）目前：

```csharp
@if (sItem.Class.Course.IsSum) {
    if (sItem.Class.Course.Name.Contains("新生") || sItem.Class.Course.Name.Contains("流失") ||
    sItem.Class.Course.Name.Equals("本週總詢問(填單)人數")) {
        <input type="number" ... onchange="valueChange('class_@sItem.Id.ToString()_count')">
    }
    else {
        @sItem.Number
    }
}
else {
    <input type="number" ... onchange="valueChange('class_@sItem.Id.ToString()_count')">
}
```

改成（`else` 分支比照「上週人數」的可編輯/唯讀切換寫法，額外加上手動標記與差異提示）：

```csharp
@if (sItem.Class.Course.IsSum) {
    if (sItem.Class.Course.Name.Contains("新生") || sItem.Class.Course.Name.Contains("流失") ||
    sItem.Class.Course.Name.Equals("本週總詢問(填單)人數")) {
        <input type="number" ... onchange="valueChange('class_@sItem.Id.ToString()_count')">
    }
    else if (ViewBag.CanEditLastWeek != null && (bool)ViewBag.CanEditLastWeek) {
        <input type="number" class="form-control" value="@sItem.Number" data-sitem="@sItem.Id" id="class_@sItem.Id.ToString()_count" onchange="valueChange('class_@sItem.Id.ToString()_count')">
        @if (sItem.IsManual) {
            <div class="small text-muted">(手動)</div>
            @if (sItem.PreviewNumber.HasValue && sItem.PreviewNumber.Value != sItem.Number) {
                <div class="small text-warning">
                    系統試算：@sItem.PreviewNumber
                    <a href="javascript:void(0)" onclick="revertToAuto(@sItem.Id)">改用系統試算值</a>
                </div>
            }
        }
    }
    else {
        @sItem.Number
    }
}
else {
    <input type="number" ... onchange="valueChange('class_@sItem.Id.ToString()_count')">
}
```

沿用既有 `valueChange`（POST 到 `UpdateClassItem`），不需要新的存檔 JS——這就是 A 段設計刻意讓「改自動計算合計欄」跟「改一般欄位」共用同一個 `number` 參數路徑的原因。

**`ASPopulationPartialView.cshtml`**（約 20-34 行）目前：

```csharp
@if (cItem.IsSum) {
    StudentPopulationItem sumItem = Model.Items.Where(e => e.Class.Course.Id == cItem.Id).FirstOrDefault();
    string displayStr = string.Format("{0} {1} 人", cItem.Name, (sumItem != null ? sumItem.Number : 0).ToString());
    <label>
        <h2>@displayStr</h2>
    </label>
}
```

改成：

```csharp
@if (cItem.IsSum) {
    StudentPopulationItem sumItem = Model.Items.Where(e => e.Class.Course.Id == cItem.Id).FirstOrDefault();
    @if (sumItem != null && ViewBag.CanEditLastWeek != null && (bool)ViewBag.CanEditLastWeek && !cItem.Name.Contains("新生") && !cItem.Name.Contains("流失")) {
        <label>
            <h2>
                @cItem.Name
                <input type="number" class="form-control d-inline-block" style="width:100px" value="@sumItem.Number" data-sitem="@sumItem.Id" id="class_@sumItem.Id.ToString()_count" onchange="valueChange('class_@sumItem.Id.ToString()_count')">
                人
            </h2>
            @if (sumItem.IsManual) {
                <div class="small text-muted">(手動)</div>
                @if (sumItem.PreviewNumber.HasValue && sumItem.PreviewNumber.Value != sumItem.Number) {
                    <div class="small text-warning">
                        系統試算：@sumItem.PreviewNumber
                        <a href="javascript:void(0)" onclick="revertToAuto(@sumItem.Id)">改用系統試算值</a>
                    </div>
                }
            }
        </label>
    }
    else {
        string displayStr = string.Format("{0} {1} 人", cItem.Name, (sumItem != null ? sumItem.Number : 0).ToString());
        <label>
            <h2>@displayStr</h2>
        </label>
    }
}
```

（AS 的 IsSum 課程確認過 `GroupByClassType=false`，每個課程固定只有一筆 `StudentPopulationItem`，`FirstOrDefault()` 不會有多筆混淆的問題。「新生/流失」排除條件沿用第 44 行既有的 `cItem.Name.Contains("新生") || cItem.Name.Contains("流失")` 判斷邏輯，保持這兩種本來就可編輯的欄位不受影響。）

**5 個 `CreateXXXPopulation.cshtml`** 各加一個 `revertToAuto` JS 函式（比照 `lastWeekValueChange` 的寫法）：

```javascript
function revertToAuto(sId) {
    $.ajax({
        url: '@Url.Action("RevertToAutoCalculation", "StudentPopulation", new { area = "" })',
        type: 'POST',
        data: { sId: sId },
        success: function (data) {
            location.reload();
        },
        error: function () {
            alert('回復自動計算失敗，請重試');
        }
    });
}
```

（`RevertToAutoCalculation` 回傳的是 JSON，不是 partial view HTML，跟 `valueChange`/`lastWeekValueChange` 那種直接把回傳內容塞進 `#contentItem` 的模式不同，所以這裡用最簡單的「成功後整頁重新整理」，不用另外處理局部更新的 HTML 結構。）

## 影響檔案清單

- `Portal/Services/Aggregation/AggregationEngine.cs`：`Calculate` 加 `IsManual` 判斷 + 拆出 `Compute`/`Preview`
- `Portal/Controllers/StudentPopulationController.cs`：`UpdateClassItem`（`isAutoComputedSum`/`numberApplied` 判斷）、新增 `AttachManualPreviews`、新增 `RevertToAutoCalculation`、5 個 Create action + `AddNewClass` + `RemoveClassItem` + `UpdateClassItem` 呼叫 `AttachManualPreviews`
- `schema/Data/Content/StudentPopulationItem.cs`：新增 `[NotMapped] PreviewNumber`
- `Views/StudentPopulation/PopulationPartialView.cshtml`：2 處
- `Views/StudentPopulation/ASPopulationPartialView.cshtml`：1 處
- `Views/StudentPopulation/CreatePopulation.cshtml`、`CreatePSJPopulation.cshtml`、`CreateGeptPopulation.cshtml`、`CreatePSPopulation.cshtml`、`CreateASPopulation.cshtml`：各加 `revertToAuto` JS

## Out of scope

- PH 特有的「大標題式」合計顯示（`PopulationPartialView.cshtml` 裡 `本週英語文總人數`/`上週英語文總人數`/`與上週相比`/`去年同期/比` 這幾個課程，用整段自訂 `<h2>` 標題呈現、完全不經過本次改動的 col-1 儲存格結構，`foreach` 迴圈裡還會直接 `continue` 跳過這幾筆項目不進入一般列渲染）——這幾個目前沒有輸入格可以改，要讓它們可編輯需要另外設計這段自訂標題的 HTML，這次不處理。
- 沒有處理「同時兩人編輯衝突」——使用者已確認情境上不會發生，不需要樂觀鎖或版本欄位。
- `Query.cshtml`/`QueryPopulationPartialView.cshtml`——維持先前既定範圍排除（唯讀歷史查詢頁面）。
- `RevertToAutoCalculation` 沒有額外的「確認視窗」——比照現有 `valueChange`/`lastWeekValueChange` 都是直接送出、無需二次確認的慣例；點擊即生效。

## 測試方式

沿用專案既有慣例：`dotnet build` 0 錯誤 + 針對 `AggregationEngine` 重構部分，若既有的 `*AggregationComparisonTests`（`Test/` 專案，`[Explicit]`，直接接 dev DB 比對 PH/PSJ/GEPT/PS/AS 五型全部人數表）仍然 0 落差，可以驗證這次的 `Compute`/`Calculate` 拆分沒有改變任何既有計算邏輯（因為這些測試就是在比對 AggregationEngine 算出來的值跟資料庫現存值）。手動瀏覽器驗證延後到之後集中驗證那一輪（目前環境沒有瀏覽器工具）。
