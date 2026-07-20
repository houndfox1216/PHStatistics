# 管理員編輯範圍擴大至 IsSum 合計欄位 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓具備 `SystemPermission.PopulationWeekSwitch` 權限的使用者，也能編輯目前唯讀、由 `AggregationEngine` 自動計算的 `IsSum` 合計欄位（例如「本週數學總人數合計」），改過的值不會被下一次自動重算蓋掉，並在系統試算值與手動值不同時提示管理員、讓其可以選擇改回自動計算。

**Architecture:** 重新啟用既有但從未使用的 `StudentPopulationItem.IsManual` 欄位（已有資料庫欄位，不需要 migration）當作「釘住」旗標；`AggregationEngine.Calculate` 開頭檢查這個旗標並提早返回；把 `Calculate` 內部的 `switch` 運算抽成一個不寫入的 `Compute` 私有方法，新增一個公開的 `Preview` 方法共用它，用來在畫面上算出「系統試算值」給管理員比對。`StudentPopulationController.cs` 沿用既有的「controller 直接 `new DataContext()`」慣例，新增一個 `RevertToAutoCalculation` action 讓管理員一鍵清除 `IsManual` 並觸發重新計算。全部改動不引入新的架構層。

**Tech Stack:** ASP.NET Core 8.0 MVC、Entity Framework Core 8.0（SQL Server）、Razor `.cshtml`、jQuery AJAX、NUnit（`AggregationEngineTests.cs` 既有純記憶體單元測試風格）。

## Global Constraints

- 這次調整只放寬給已經能編輯已送出週次的 `PopulationWeekSwitch` 權限使用者；一般使用者對 `IsSum` 合計欄位的行為必須完全不變（唯讀顯示，送 `number` 參數會被伺服器端靜默忽略）。
- `IsSum=false` 的一般欄位、以及既有 `StatisticsType.ManualInput` 的 `IsSum` 欄位（新生/流失/本週總詢問(填單)人數）：這次改動後行為必須完全不變（`isAutoComputedSum` 恆為 `false`，不受影響）。
- `StudentPopulationItem.IsManual`、`Course.StatisticsType` 都已經是有資料庫欄位的既有 schema，這個功能**不需要新的 EF Core migration**。`PreviewNumber` 是 `[NotMapped]`，同樣不需要 migration。
- 沒有處理「同時兩人編輯衝突」——使用者已確認情境上不會發生（管理員校正動作發生在分校人員輸入前或送出後），不需要樂觀鎖或版本欄位。
- `RevertToAutoCalculation` 沒有二次確認視窗，比照現有 `valueChange`/`lastWeekValueChange` 都是直接送出、無需確認的既有慣例。
- 本專案 controller 沒有單元測試基礎設施（直接 `new DataContext()` 接真實 SQL Server，非 DI）；`AggregationEngine` 例外——它是純 C# class，`AggregationEngineTests.cs` 已經有完整的純記憶體單元測試（不接 DB），這次的 `Compute`/`Preview`/`IsManual` 邏輯要延續這個測試風格。控制器層改動的驗證方式是 `dotnet build` 0 錯誤 + 程式碼檢查；真正的端對端瀏覽器操作驗證延後到全部任務完成後（目前環境沒有瀏覽器工具）。
- ⚠️ 若之後手動驗證階段需要用 sqlcmd 查詢，任何一次執行前都要先重新讀取 `source/portal/Portal/appsettings.json` 目前生效（未被 `//` 註解掉）的 `ConnectionStrings:DataContext` 值，確認其 `Server`/`Database` 不是已知的正式環境字串（正式環境特徵：`Server=20.188.19.77,52056` 或 `Server=52.237.119.13,52056` 且 `Database=NewPAS`，不含年份/週次後綴）。`sqlcmd -i` 在這台機器上已知會靜默跳過語句且無錯誤訊息，一律用 `sqlcmd -Q "<單一陳述式>"` 逐句執行。
- `dotnet test` 執行 `*AggregationComparisonTests`（`[Explicit]`）會用 `new DataContext()`（讀取 `source/portal/Portal/appsettings.json` 目前生效的連線字串）**唯讀**比對 DB 現存值，測試本身不呼叫 `SaveChanges()`，即使連到正式環境也不會寫入資料——但仍建議在正式驗證前確認一下目前連線字串指向哪個環境，方便解讀結果。

---

### Task 1: `AggregationEngine` — 拆出 `Compute`/`Preview`，`Calculate` 加上 `IsManual` 跳過判斷

**Files:**
- Modify: `source/portal/Portal/Services/Aggregation/AggregationEngine.cs`
- Modify: `source/portal/Test/Services/Aggregation/AggregationEngineTests.cs`

**Interfaces:**
- Consumes：既有 `StudentPopulationItem.IsManual`（`bool`，`source/schema/Data/Content/StudentPopulationItem.cs:135`，已存在的欄位）
- Produces：`public int? Preview(StudentPopulationItem item, StudentPopulation population)`，供 Task 3 的 `AttachManualPreviews` 呼叫；`Calculate` 的新行為（`IsManual=true` 時整個方法提早 return，不改 `item.Number`）供 Task 2 的 `UpdateClassItem` 依賴。

- [ ] **Step 1: 修改 `AggregationEngine.cs`**

打開 `source/portal/Portal/Services/Aggregation/AggregationEngine.cs`，把整個 `Calculate` 方法：

```csharp
    public void Calculate(StudentPopulationItem item, StudentPopulation population) {
        var course = item.Class?.Course;
        if (course == null || !course.IsSum) return;

        var type = course.StatisticsType;
        if (type == null || type == StatisticsType.None || type == StatisticsType.ManualInput) return;

        switch (type.Value) {
            case StatisticsType.SumByDepartment:
            case StatisticsType.SumByDepartmentAndClassType:
            case StatisticsType.SumBySourceDepartments:
            case StatisticsType.SumBySourceCourses:
                item.Number = GetSourceItems(course, item, population.Items).Sum(i => i.Number);
                break;
            case StatisticsType.CountClasses:
            case StatisticsType.CountClassesByClassType:
                item.Number = GetSourceItems(course, item, population.Items).Count(i => i.Number > 0);
                break;
            case StatisticsType.LastWeekValue:
                item.Number = GetSourceItems(course, item, population.Items).Sum(i => i.LastWeekNumber);
                break;
            case StatisticsType.DiffWithLastWeek: {
                var src = GetSourceItems(course, item, population.Items);
                item.Number = src.Sum(i => i.Number) - src.Sum(i => i.LastWeekNumber);
                break;
            }
            case StatisticsType.LastYearValue:
                item.Number = SumLastYear(course, item, population);
                break;
            case StatisticsType.DiffWithLastYear: {
                int thisWeek = GetSourceItems(course, item, population.Items).Sum(i => i.Number);
                item.Number = thisWeek - SumLastYear(course, item, population);
                break;
            }
            case StatisticsType.Average: {
                var src = GetSourceItems(course, item, population.Items).ToList();
                int count = src.Count(i => i.Number > 0);
                item.Number = count > 0 ? src.Sum(i => i.Number) / count : 0;
                break;
            }
            default:
                throw new NotSupportedException(
                    $"AggregationEngine 尚未支援 StatisticsType.{type.Value}（課程 {course.Id} {course.Name}）。");
        }
    }
```

改成（`Calculate`/`Preview` 呼叫共用的 `Compute`，`Calculate` 開頭多一行 `IsManual` 判斷）：

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

這是純重構——`Calculate` 的行為（除了新增的 `IsManual` 跳過判斷）與現在完全一致，只是把 `switch` 內容抽出來給 `Preview` 共用。

- [ ] **Step 2: Build 確認編譯成功**

```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`。

- [ ] **Step 3: 新增失敗的單元測試（`IsManual` 跳過 + `Preview`）**

打開 `source/portal/Test/Services/Aggregation/AggregationEngineTests.cs`，在檔案最後一個測試 `CalculateAll_ProcessesEverySumItemAndSkipsManualInput`（結尾 `}` 之後，class 結尾 `}` 之前）加入：

```csharp

    [Test]
    public void Calculate_ItemMarkedIsManual_LeavesNumberUnchangedRegardlessOfSourceData() {
        var course1 = MakeCourse(101, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.SumByDepartment);
        var sumItem = MakeItem(sumCourse, ClassType.General, 42);
        sumItem.IsManual = true;

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 999), // would sum to 999 if not pinned
                sumItem,
            },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(42));
    }

    [Test]
    public void Preview_SumByDepartment_ReturnsComputedValueWithoutMutatingItemNumber() {
        var course1 = MakeCourse(101, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.SumByDepartment);
        var sumItem = MakeItem(sumCourse, ClassType.General, 42);
        sumItem.IsManual = true;

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 999),
                sumItem,
            },
        };

        int? preview = MakeEngine().Preview(sumItem, population);

        Assert.That(preview, Is.EqualTo(999));
        Assert.That(sumItem.Number, Is.EqualTo(42)); // Preview 不寫入
    }

    [Test]
    public void Preview_NonSumCourse_ReturnsNull() {
        var course = MakeCourse(101, isSum: false);
        var item = MakeItem(course, ClassType.General, 8);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> { item },
        };

        int? preview = MakeEngine().Preview(item, population);

        Assert.That(preview, Is.Null);
    }

    [Test]
    public void Preview_ManualInputStatisticsType_ReturnsNull() {
        var sumCourse = MakeCourse(199, isSum: true, statisticsType: StatisticsType.ManualInput);
        var sumItem = MakeItem(sumCourse, ClassType.General, 42);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> { sumItem },
        };

        int? preview = MakeEngine().Preview(sumItem, population);

        Assert.That(preview, Is.Null);
    }
```

- [ ] **Step 4: 執行測試確認全部通過**

```bash
cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"
```
Expected: 全部（含新增的 4 個）測試 `Passed`，`Failed: 0`。

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Services/Aggregation/AggregationEngine.cs source/portal/Test/Services/Aggregation/AggregationEngineTests.cs
git commit -m "feat: split AggregationEngine.Calculate into Compute/Preview, skip IsManual items"
```

---

### Task 2: `UpdateClassItem` — `IsManual` 釘住機制（`isAutoComputedSum`/`numberApplied`）

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes：Task 1 的 `Calculate` 新行為（`item.IsManual=true` 時不會被下一次 `SumPHPopulation` 蓋掉）
- Produces：`UpdateClassItem` 內的 `isAutoComputedSum`/`numberApplied` 判斷邏輯（區域變數，不外露），確立「對自動計算合計欄送 `number` 參數，只有 `PopulationWeekSwitch` 權限使用者才會被套用，且套用時順便標記 `IsManual=true`」的規則。之後 Task 3 會在同一個方法裡插入 `AttachManualPreviews` 呼叫，Task 4 的 `RevertToAutoCalculation` 會清掉這裡設定的 `IsManual`。

- [ ] **Step 1: 修改 `UpdateClassItem`**

找到目前的整個方法（`source/portal/Portal/Controllers/StudentPopulationController.cs`，約 1186-1230 行）：

```csharp
        public IActionResult UpdateClassItem(long sId, int? number, string studentRemark = null, int? lastWeekNumber = null) {
            DataContext dataContext = new DataContext();
            try {
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("Class.Course.Department").Include("StudentPopulation").Where(e => e.Id == sId).FirstOrDefault();
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
                ViewBag.Courses = courses;
                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
                ViewBag.CanEditLastWeek = canEditLocked;
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                    return PartialView("PopulationPartialView", lockedData);
                }
                bool lastWeekApplied = lastWeekNumber.HasValue && canEditLocked;
                int oldNumber = item.Number;
                int oldLastWeekNumber = item.LastWeekNumber;
                string oldStudentRemark = item.StudentRemark;
                if (number.HasValue) {
                    item.Number = number.Value;
                }
                if (studentRemark != null) {
                    item.StudentRemark = studentRemark;
                }
                if (lastWeekApplied) {
                    item.LastWeekNumber = lastWeekNumber.Value;
                }
                dataContext.StudentPopulationItem.Update(item);
                dataContext.SaveChanges();
                if (number.HasValue || studentRemark != null || lastWeekApplied) {
                    WriteItemLog(dataContext, item.StudentPopulationId, item.ClassId, item.Name, oldNumber, item.Number, oldLastWeekNumber, item.LastWeekNumber, oldStudentRemark, item.StudentRemark);
                }
                if (number.HasValue || lastWeekApplied) {
                    SumPHPopulation(item.StudentPopulation.Id);
                }
                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                ViewBag.Warnings = CheckNewLostConsistency(returnData);
                return PartialView("PopulationPartialView", returnData);
            }
            catch (Exception ex) {
                ViewBag.Courses = new List<Course>();
                Logger.LogError(ex, "UpdateClassItem sId={sId}", sId);
                return PartialView("PopulationPartialView", new StudentPopulation());
            }
        }
```

改成：

```csharp
        public IActionResult UpdateClassItem(long sId, int? number, string studentRemark = null, int? lastWeekNumber = null) {
            DataContext dataContext = new DataContext();
            try {
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("Class.Course.Department").Include("StudentPopulation").Where(e => e.Id == sId).FirstOrDefault();
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
                ViewBag.Courses = courses;
                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
                ViewBag.CanEditLastWeek = canEditLocked;
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                    return PartialView("PopulationPartialView", lockedData);
                }
                bool lastWeekApplied = lastWeekNumber.HasValue && canEditLocked;
                bool isAutoComputedSum = item.IsSum
                    && item.Class?.Course?.StatisticsType != null
                    && item.Class.Course.StatisticsType != StatisticsType.None
                    && item.Class.Course.StatisticsType != StatisticsType.ManualInput;
                bool numberApplied = number.HasValue && (!isAutoComputedSum || canEditLocked);
                int oldNumber = item.Number;
                int oldLastWeekNumber = item.LastWeekNumber;
                string oldStudentRemark = item.StudentRemark;
                if (numberApplied) {
                    item.Number = number.Value;
                    if (isAutoComputedSum) {
                        item.IsManual = true;
                    }
                }
                if (studentRemark != null) {
                    item.StudentRemark = studentRemark;
                }
                if (lastWeekApplied) {
                    item.LastWeekNumber = lastWeekNumber.Value;
                }
                dataContext.StudentPopulationItem.Update(item);
                dataContext.SaveChanges();
                if (numberApplied || studentRemark != null || lastWeekApplied) {
                    WriteItemLog(dataContext, item.StudentPopulationId, item.ClassId, item.Name, oldNumber, item.Number, oldLastWeekNumber, item.LastWeekNumber, oldStudentRemark, item.StudentRemark);
                }
                if (numberApplied || lastWeekApplied) {
                    SumPHPopulation(item.StudentPopulation.Id);
                }
                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                ViewBag.Warnings = CheckNewLostConsistency(returnData);
                return PartialView("PopulationPartialView", returnData);
            }
            catch (Exception ex) {
                ViewBag.Courses = new List<Course>();
                Logger.LogError(ex, "UpdateClassItem sId={sId}", sId);
                return PartialView("PopulationPartialView", new StudentPopulation());
            }
        }
```

**重點**：
- 一般欄位（`IsSum=false`）：`isAutoComputedSum` 恆為 `false` → `numberApplied == number.HasValue`，跟現在完全一樣。
- 現有 `ManualInput` 的 `IsSum` 欄位（新生/流失/本週總詢問(填單)人數）：`isAutoComputedSum` 恆為 `false`（`StatisticsType == ManualInput` 被排除）→ 行為不變。
- 只有「本來唯讀、自動算出來的合計欄」才會被 `isAutoComputedSum` 判斷擋下來，且只有 `canEditLocked`（`PopulationWeekSwitch` 權限）為真時才會套用並標記 `IsManual=true`——沒有權限的人對這類欄位送 `number` 參數會被靜默忽略，是伺服器端的二次防線，跟 `lastWeekNumber` 現有的處理方式對稱一致。

- [ ] **Step 2: Build 確認編譯成功**

```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`。

- [ ] **Step 3: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: let PopulationWeekSwitch users edit auto-computed IsSum fields and pin them via IsManual"
```

---

### Task 3: `PreviewNumber` 欄位 + `AttachManualPreviews`，接上所有會渲染畫面的路徑

**Files:**
- Modify: `source/schema/Data/Content/StudentPopulationItem.cs`
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes：Task 1 的 `AggregationEngine.Preview(StudentPopulationItem, StudentPopulation)`
- Produces：`StudentPopulationItem.PreviewNumber`（`int?`，`[NotMapped]`）；私有方法 `AttachManualPreviews(DataContext dataContext, StudentPopulation population)`。兩者都供 Task 5 的 view 改動讀取（`sItem.IsManual`/`sItem.PreviewNumber`）。

- [ ] **Step 1: `StudentPopulationItem.cs` 新增 `PreviewNumber`**

打開 `source/schema/Data/Content/StudentPopulationItem.cs`，找到結尾：

```csharp
        /// <summary>
        /// 手動調整
        /// </summary>
        [Display(Name = "手動調整"), DataMember]
        public bool IsManual { get; set; }
    }
}
```

改成：

```csharp
        /// <summary>
        /// 手動調整
        /// </summary>
        [Display(Name = "手動調整"), DataMember]
        public bool IsManual { get; set; }

        /// <summary>
        /// 系統試算值（僅在 IsManual 時由 AttachManualPreviews 填入，不落地）
        /// </summary>
        [Display(Name = "系統試算值"), DataMember, NotMapped]
        public int? PreviewNumber { get; set; }
    }
}
```

- [ ] **Step 2: Build 確認編譯成功**

```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`。

- [ ] **Step 3: 新增 `AttachManualPreviews` 私有方法**

打開 `source/portal/Portal/Controllers/StudentPopulationController.cs`，找到（約 1411-1414 行）：

```csharp
            StudentPopulation studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week && e.Type == seleceedType).FirstOrDefault();
            return PartialView("QueryPopulationPartialView", studentPopulationData);
        }
        public StudentPopulation SumPHPopulation(long spId) {
```

改成：

```csharp
            StudentPopulation studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week && e.Type == seleceedType).FirstOrDefault();
            return PartialView("QueryPopulationPartialView", studentPopulationData);
        }

        private void AttachManualPreviews(DataContext dataContext, StudentPopulation population) {
            if (population?.Items == null) return;
            var previewEngine = new AggregationEngine((year, week, schoolId, type) =>
                dataContext.StudentPopulation.Include("Items.Class.Course")
                    .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type));
            foreach (var item in population.Items.Where(i => i.IsManual)) {
                item.PreviewNumber = previewEngine.Preview(item, population);
            }
        }

        public StudentPopulation SumPHPopulation(long spId) {
```

- [ ] **Step 4: `CreatePopulation`（PH）呼叫 `AttachManualPreviews`**

找到（約 359-367 行）：

```csharp
                    dataContext.SaveChanges();
                    SumPHPopulation(returnData.Id);
                    dataContext.ChangeTracker.Clear();
                    returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.Id == returnData.Id);
                }
                return View(returnData);
            }
            return View();
        }
```

改成：

```csharp
                    dataContext.SaveChanges();
                    SumPHPopulation(returnData.Id);
                    dataContext.ChangeTracker.Clear();
                    returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.Id == returnData.Id);
                }
                AttachManualPreviews(dataContext, returnData);
                return View(returnData);
            }
            return View();
        }
```

- [ ] **Step 5: `CreatePSJPopulation` 呼叫 `AttachManualPreviews`**

找到（約 528-532 行，這是 PSJ 特有的「合併送出資料」尾端，跟 Step 6 的 Gept/PS/AS 版本不同，不要用文字搜尋去改到別的地方，用行號定位）：

```csharp
                    dataContext.SaveChanges();
                }
            }
            return View(returnData);
        }
```

改成：

```csharp
                    dataContext.SaveChanges();
                }
            }
            AttachManualPreviews(dataContext, returnData);
            return View(returnData);
        }
```

- [ ] **Step 6: `CreateGeptPopulation`/`CreatePSPopulation`/`CreateASPopulation` 呼叫 `AttachManualPreviews`（三處文字完全相同，一次改三處）**

這三個 action 結尾的這段文字彼此完全一致（分別約在 632-641、740-749、852-861 行）：

```csharp
                dataContext.SaveChanges();
                SumPHPopulation(returnData.Id);
                dataContext.ChangeTracker.Clear();
                returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.Id == returnData.Id);
            }
            if (Request.Method == "POST") {
                //進行人數表新增或更新
            }
            return View(returnData);
        }
```

改成（**用支援「取代全部符合的地方」的編輯方式一次套用到全部 3 處**，不要只改第一個找到的）：

```csharp
                dataContext.SaveChanges();
                SumPHPopulation(returnData.Id);
                dataContext.ChangeTracker.Clear();
                returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.Id == returnData.Id);
            }
            if (Request.Method == "POST") {
                //進行人數表新增或更新
            }
            AttachManualPreviews(dataContext, returnData);
            return View(returnData);
        }
```

改完後執行：
```bash
grep -c "AttachManualPreviews(dataContext, returnData);" source/portal/Portal/Controllers/StudentPopulationController.cs
```
Expected: `5`（Step 4 的 PH + Step 5 的 PSJ + Step 6 的 Gept/PS/AS 三處）。如果不是 5，代表 Step 6 只套用到一部分，回去確認取代方式有沒有套用到全部符合的地方。

- [ ] **Step 7: `AddNewClass` 鎖定路徑呼叫 `AttachManualPreviews`**

找到（約 880-885 行）：

```csharp
            bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
            ViewBag.CanEditLastWeek = canEditLocked;
            if (studentPopulationData.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == studentPopulationData.Id).FirstOrDefault();
                return PartialView("PopulationPartialView", lockedData);
            }
```

改成：

```csharp
            bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
            ViewBag.CanEditLastWeek = canEditLocked;
            if (studentPopulationData.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == studentPopulationData.Id).FirstOrDefault();
                AttachManualPreviews(dataContext, lockedData);
                return PartialView("PopulationPartialView", lockedData);
            }
```

- [ ] **Step 8: `AddNewClass` 正常路徑呼叫 `AttachManualPreviews`**

找到（約 1141-1153 行）：

```csharp
            dataContext.ChangeTracker.Clear();
            //var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week && e.Type == seleceedType).FirstOrDefault();
            var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == studentPopulationData.Id).FirstOrDefault();
            if (newAddedClassId > 0) {
                var newItem = returnData?.Items?.FirstOrDefault(i => i.ClassId == newAddedClassId);
                if (newItem != null) {
                    newItem.IsNew = true;
                    WriteItemLog(dataContext, newItem.StudentPopulationId, newItem.ClassId, newItem.Name, 0, newItem.Number, 0, newItem.LastWeekNumber, null, newItem.StudentRemark, isNew: true);
                }
            }
            ViewBag.Warnings = CheckNewLostConsistency(returnData);
            return PartialView("PopulationPartialView", returnData);
        }
```

改成：

```csharp
            dataContext.ChangeTracker.Clear();
            //var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week && e.Type == seleceedType).FirstOrDefault();
            var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == studentPopulationData.Id).FirstOrDefault();
            if (newAddedClassId > 0) {
                var newItem = returnData?.Items?.FirstOrDefault(i => i.ClassId == newAddedClassId);
                if (newItem != null) {
                    newItem.IsNew = true;
                    WriteItemLog(dataContext, newItem.StudentPopulationId, newItem.ClassId, newItem.Name, 0, newItem.Number, 0, newItem.LastWeekNumber, null, newItem.StudentRemark, isNew: true);
                }
            }
            ViewBag.Warnings = CheckNewLostConsistency(returnData);
            AttachManualPreviews(dataContext, returnData);
            return PartialView("PopulationPartialView", returnData);
        }
```

- [ ] **Step 9: `RemoveClassItem` 鎖定路徑呼叫 `AttachManualPreviews`**

找到（約 1164-1169 行）：

```csharp
                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
                ViewBag.CanEditLastWeek = canEditLocked;
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulationId).FirstOrDefault();
                    return PartialView("PopulationPartialView", lockedData);
                }
```

改成：

```csharp
                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
                ViewBag.CanEditLastWeek = canEditLocked;
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulationId).FirstOrDefault();
                    AttachManualPreviews(dataContext, lockedData);
                    return PartialView("PopulationPartialView", lockedData);
                }
```

- [ ] **Step 10: `RemoveClassItem` 正常路徑呼叫 `AttachManualPreviews`**

找到（約 1175-1177 行）：

```csharp
                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == spId).FirstOrDefault();
                ViewBag.Warnings = CheckNewLostConsistency(returnData);
                return PartialView("PopulationPartialView", returnData);
```

改成：

```csharp
                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == spId).FirstOrDefault();
                ViewBag.Warnings = CheckNewLostConsistency(returnData);
                AttachManualPreviews(dataContext, returnData);
                return PartialView("PopulationPartialView", returnData);
```

- [ ] **Step 11: `UpdateClassItem` 鎖定路徑呼叫 `AttachManualPreviews`**

找到（Task 2 改完後的版本，約 1196-1199 行）：

```csharp
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                    return PartialView("PopulationPartialView", lockedData);
                }
```

改成：

```csharp
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                    AttachManualPreviews(dataContext, lockedData);
                    return PartialView("PopulationPartialView", lockedData);
                }
```

- [ ] **Step 12: `UpdateClassItem` 正常路徑呼叫 `AttachManualPreviews`**

找到（Task 2 改完後的版本，約 1221-1223 行）：

```csharp
                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                ViewBag.Warnings = CheckNewLostConsistency(returnData);
                return PartialView("PopulationPartialView", returnData);
```

改成：

```csharp
                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                ViewBag.Warnings = CheckNewLostConsistency(returnData);
                AttachManualPreviews(dataContext, returnData);
                return PartialView("PopulationPartialView", returnData);
```

- [ ] **Step 13: Build 確認編譯成功**

```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`。

- [ ] **Step 14: 靜態檢查 — 確認總共 8 處呼叫**

```bash
grep -c "AttachManualPreviews(dataContext, " source/portal/Portal/Controllers/StudentPopulationController.cs
```
Expected: `11`（5 個 Create action（Step 4/5/6）+ `AddNewClass` 鎖定/正常 2 處（Step 7/8）+ `RemoveClassItem` 鎖定/正常 2 處（Step 9/10）+ `UpdateClassItem` 鎖定/正常 2 處（Step 11/12）= 5+2+2+2 = 11）。如果 grep 結果不是 11，回去檢查 Step 4-12 是否每一處都改到、或是否重複改到同一處。

- [ ] **Step 15: Commit**

```bash
git add source/schema/Data/Content/StudentPopulationItem.cs source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: attach system-computed preview to manually-pinned population items before every render"
```

---

### Task 4: 新增 `RevertToAutoCalculation` action

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes：Task 2 的 `WriteItemLog`（既有簽章）、既有 `SumPHPopulation(long spId)`
- Produces：`POST /StudentPopulation/RevertToAutoCalculation`，接受 `sId`（`long`），回傳 `{ success: bool, number?: int, message?: string }`；供 Task 6 的 `revertToAuto(sId)` JS 呼叫。

- [ ] **Step 1: 新增 action**

找到 `UpdateClassItem` 方法結尾（Task 2/3 改完後，約 1231 行）：

```csharp
        [HttpPost]
        public IActionResult UpdateRemark(long sId, string studentRemark) {
```

改成（在 `UpdateRemark` 前面插入新 action）：

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

        [HttpPost]
        public IActionResult UpdateRemark(long sId, string studentRemark) {
```

**注意**：`SumPHPopulation(spId)` 內部另開自己的 `DataContext`，跟這裡的 `dataContext` 不是同一個連線追蹤範圍，所以呼叫完之後要 `dataContext.ChangeTracker.Clear()` 再重新查詢，才能拿到 `SumPHPopulation` 寫入之後的最新 `Number`——這個手法抄既有 `CreateXXXPopulation` 路徑「`SumPHPopulation` + `ChangeTracker.Clear()` + 重新查詢」那段既有寫法，不是新發明的模式（可參考 Task 3 Step 4 改完的 `CreatePopulation` 結尾）。

- [ ] **Step 2: Build 確認編譯成功**

```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`。

- [ ] **Step 3: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: add RevertToAutoCalculation action to clear IsManual pin and recompute"
```

---

### Task 5: 畫面調整 — 手動標記 + 差異提示（`PopulationPartialView.cshtml` x2、`ASPopulationPartialView.cshtml` x1）

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/PopulationPartialView.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/ASPopulationPartialView.cshtml`

**Interfaces:**
- Consumes：Task 2 的 `UpdateClassItem`（`number` 參數路徑，沿用既有 `valueChange`，不需要新的 JS 或新的 endpoint）；Task 3 的 `sItem.IsManual`/`sItem.PreviewNumber`
- Produces：畫面上呼叫 `revertToAuto(sId)`（尚未定義），由 Task 6 在 5 個 `CreateXXXPopulation.cshtml` 補上。這個任務完成後、Task 6 完成前，點擊「改用系統試算值」連結會出現 JS 錯誤（`revertToAuto is not defined`）——這是預期的中間狀態，不影響 `dotnet build`（Razor view 裡的 JS 字串不會被編譯期檢查），下一個任務會補上。

- [ ] **Step 1: `PopulationPartialView.cshtml` 第一處（特殊命名課程分支，約 254-268 行）**

找到：

```csharp
                    <div class="col-1 mb-1">
                        @if (sItem.Class.Course.IsSum) {
                            if (sItem.Class.Course.Name.Contains("新生") || sItem.Class.Course.Name.Contains("流失") ||
                            sItem.Class.Course.Name.Equals("本週總詢問(填單)人數")) {
                                <input type="number" class="form-control" placeholder="請輸入人數" name="Items[@itemRowNo].Number" id="class_@sItem.Id.ToString()_count" value="@sItem.Number" data-course="@cItem.Id" data-class="@sItem.Class.Id" data-sitem="@sItem.Id" onchange="valueChange('class_@sItem.Id.ToString()_count')">
                            }
                            else {
                                @sItem.Number
                            }
                        }
                        else {
                            <input type="number" class="form-control" placeholder="請輸入人數" name="Items[@itemRowNo].Number" id="class_@sItem.Id.ToString()_count" value="@sItem.Number" data-course="@cItem.Id" data-class="@sItem.Class.Id" data-sitem="@sItem.Id" onchange="valueChange('class_@sItem.Id.ToString()_count')">
                        }

                    </div>
```

改成：

```csharp
                    <div class="col-1 mb-1">
                        @if (sItem.Class.Course.IsSum) {
                            if (sItem.Class.Course.Name.Contains("新生") || sItem.Class.Course.Name.Contains("流失") ||
                            sItem.Class.Course.Name.Equals("本週總詢問(填單)人數")) {
                                <input type="number" class="form-control" placeholder="請輸入人數" name="Items[@itemRowNo].Number" id="class_@sItem.Id.ToString()_count" value="@sItem.Number" data-course="@cItem.Id" data-class="@sItem.Class.Id" data-sitem="@sItem.Id" onchange="valueChange('class_@sItem.Id.ToString()_count')">
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
                            <input type="number" class="form-control" placeholder="請輸入人數" name="Items[@itemRowNo].Number" id="class_@sItem.Id.ToString()_count" value="@sItem.Number" data-course="@cItem.Id" data-class="@sItem.Class.Id" data-sitem="@sItem.Id" onchange="valueChange('class_@sItem.Id.ToString()_count')">
                        }

                    </div>
```

- [ ] **Step 2: `PopulationPartialView.cshtml` 第二處（一般課程分支，約 381-395 行，縮排比 Step 1 少 4 個空白）**

找到：

```csharp
                <div class="col-1 mb-1">
                    @if (sItem.Class.Course.IsSum) {
                        if (sItem.Class.Course.Name.Contains("新生") || sItem.Class.Course.Name.Contains("流失") ||
                        sItem.Class.Course.Name.Equals("本週總詢問(填單)人數")) {
                            <input type="number" class="form-control" placeholder="請輸入人數" name="Items[@itemRowNo].Number" id="class_@sItem.Id.ToString()_count" value="@sItem.Number" data-course="@cItem.Id" data-class="@sItem.Class.Id" data-sitem="@sItem.Id" onchange="valueChange('class_@sItem.Id.ToString()_count')">
                        }
                        else {
                            @sItem.Number
                        }
                    }
                    else {
                        <input type="number" class="form-control" placeholder="請輸入人數" name="Items[@itemRowNo].Number" id="class_@sItem.Id.ToString()_count" value="@sItem.Number" data-course="@cItem.Id" data-class="@sItem.Class.Id" data-sitem="@sItem.Id" onchange="valueChange('class_@sItem.Id.ToString()_count')">
                    }

                </div>
```

改成：

```csharp
                <div class="col-1 mb-1">
                    @if (sItem.Class.Course.IsSum) {
                        if (sItem.Class.Course.Name.Contains("新生") || sItem.Class.Course.Name.Contains("流失") ||
                        sItem.Class.Course.Name.Equals("本週總詢問(填單)人數")) {
                            <input type="number" class="form-control" placeholder="請輸入人數" name="Items[@itemRowNo].Number" id="class_@sItem.Id.ToString()_count" value="@sItem.Number" data-course="@cItem.Id" data-class="@sItem.Class.Id" data-sitem="@sItem.Id" onchange="valueChange('class_@sItem.Id.ToString()_count')">
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
                        <input type="number" class="form-control" placeholder="請輸入人數" name="Items[@itemRowNo].Number" id="class_@sItem.Id.ToString()_count" value="@sItem.Number" data-course="@cItem.Id" data-class="@sItem.Class.Id" data-sitem="@sItem.Id" onchange="valueChange('class_@sItem.Id.ToString()_count')">
                    }

                </div>
```

- [ ] **Step 3: `ASPopulationPartialView.cshtml`（約 20-35 行）**

找到：

```csharp
        <div class="row mx-0" style="background-color:@color">
            <div class="col-12 mb-3">
                @if (cItem.IsSum) {
                    StudentPopulationItem sumItem = Model.Items.Where(e => e.Class.Course.Id == cItem.Id).FirstOrDefault();
                    string displayStr = string.Format("{0} {1} 人", cItem.Name, (sumItem != null ? sumItem.Number : 0).ToString());
                    <label>
                        <h2>@displayStr</h2>                        
                    </label>
                }
                else {
                    <label>
                        <h2>課程：@cItem.Name</h2>
                    </label>
                }

            </div>
```

改成：

```csharp
        <div class="row mx-0" style="background-color:@color">
            <div class="col-12 mb-3">
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
                else {
                    <label>
                        <h2>課程：@cItem.Name</h2>
                    </label>
                }

            </div>
```

（AS 的 `IsSum` 課程確認過 `GroupByClassType=false`，每個課程固定只有一筆 `StudentPopulationItem`，`FirstOrDefault()` 不會有多筆混淆的問題。「新生/流失」排除條件沿用既有 `cItem.Name.Contains("新生") || cItem.Name.Contains("流失")` 判斷邏輯，保持這兩種本來就可編輯的欄位不受影響。）

- [ ] **Step 4: Build 確認編譯成功**

```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`（Razor view 語法錯誤在 build 階段就會被抓到）。

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Views/StudentPopulation/PopulationPartialView.cshtml source/portal/Portal/Views/StudentPopulation/ASPopulationPartialView.cshtml
git commit -m "feat: render editable IsSum aggregate fields with manual badge and revert-to-auto hint"
```

---

### Task 6: 5 個 `CreateXXXPopulation.cshtml` 加 `revertToAuto` JS + 全量驗證

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePSJPopulation.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/CreateGeptPopulation.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePSPopulation.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/CreateASPopulation.cshtml`

**Interfaces:**
- Consumes：Task 4 的 `POST /StudentPopulation/RevertToAutoCalculation`
- Produces：無（畫面最末端功能，沒有後續任務依賴這裡）

- [ ] **Step 1: `CreatePopulation.cshtml` 加 `revertToAuto`（緊接在 `lastWeekValueChange` 結尾之後，約 348-364 行）**

找到：

```javascript
    function lastWeekValueChange(sId){
        var updateCourses = document.getElementById(sId);
        $.ajax({
            url: '@Url.Action("UpdateClassItem", "StudentPopulation", new { area = "" })',
            cache: false,
            data: { 'sId': updateCourses.dataset.sitem, 'lastWeekNumber': updateCourses.value },
            type: 'POST',
            dataType: "html",
            success: function (data) {
                $("#contentItem").html(data);
                if (typeof updateSubmitSummary === 'function') updateSubmitSummary();
            },
            error: function () {

            }
        });
    }

    //sumValueChange
```

改成：

```javascript
    function lastWeekValueChange(sId){
        var updateCourses = document.getElementById(sId);
        $.ajax({
            url: '@Url.Action("UpdateClassItem", "StudentPopulation", new { area = "" })',
            cache: false,
            data: { 'sId': updateCourses.dataset.sitem, 'lastWeekNumber': updateCourses.value },
            type: 'POST',
            dataType: "html",
            success: function (data) {
                $("#contentItem").html(data);
                if (typeof updateSubmitSummary === 'function') updateSubmitSummary();
            },
            error: function () {

            }
        });
    }

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

    //sumValueChange
```

- [ ] **Step 2: `CreatePSJPopulation.cshtml` 加 `revertToAuto`（緊接在 `lastWeekValueChange` 結尾之後，約 339-355 行，結尾之後是 `function confirmPopulation`）**

找到：

```javascript
    function lastWeekValueChange(sId){
        var updateCourses = document.getElementById(sId);
        $.ajax({
            url: '@Url.Action("UpdateClassItem", "StudentPopulation", new { area = "" })',
            cache: false,
            data: { 'sId': updateCourses.dataset.sitem, 'lastWeekNumber': updateCourses.value },
            type: 'POST',
            dataType: "html",
            success: function (data) {
                $("#contentItem").html(data);
                if (typeof updateSubmitSummary === 'function') updateSubmitSummary();
            },
            error: function () {

            }
        });
    }

    function confirmPopulation(populationId) {
```

改成：

```javascript
    function lastWeekValueChange(sId){
        var updateCourses = document.getElementById(sId);
        $.ajax({
            url: '@Url.Action("UpdateClassItem", "StudentPopulation", new { area = "" })',
            cache: false,
            data: { 'sId': updateCourses.dataset.sitem, 'lastWeekNumber': updateCourses.value },
            type: 'POST',
            dataType: "html",
            success: function (data) {
                $("#contentItem").html(data);
                if (typeof updateSubmitSummary === 'function') updateSubmitSummary();
            },
            error: function () {

            }
        });
    }

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

    function confirmPopulation(populationId) {
```

- [ ] **Step 3: `CreateGeptPopulation.cshtml` 加 `revertToAuto`（同樣的插入方式，約 341-357 行）**

用 Step 2 完全相同的「找到 / 改成」內容，對 `CreateGeptPopulation.cshtml` 執行同樣的插入（`lastWeekValueChange` 結尾 `}` 之後、`function confirmPopulation` 之前插入 `revertToAuto`）。

- [ ] **Step 4: `CreatePSPopulation.cshtml` 加 `revertToAuto`（同樣的插入方式，約 342-358 行）**

用 Step 2 完全相同的「找到 / 改成」內容，對 `CreatePSPopulation.cshtml` 執行同樣的插入。

- [ ] **Step 5: `CreateASPopulation.cshtml` 加 `revertToAuto`（同樣的插入方式，約 342-358 行）**

用 Step 2 完全相同的「找到 / 改成」內容，對 `CreateASPopulation.cshtml` 執行同樣的插入。

- [ ] **Step 6: Build 確認編譯成功**

```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`。

- [ ] **Step 7: 靜態檢查 — 確認 5 個檔案都有 `revertToAuto` 函式**

```bash
grep -rl "function revertToAuto" source/portal/Portal/Views/StudentPopulation/
```
Expected: 5 個檔案路徑（`CreatePopulation.cshtml`、`CreatePSJPopulation.cshtml`、`CreateGeptPopulation.cshtml`、`CreatePSPopulation.cshtml`、`CreateASPopulation.cshtml`）。

- [ ] **Step 8: 執行全部單元測試（含 Task 1 新增的 4 個）**

```bash
cd source/portal && dotnet test Test/Test.csproj
```
Expected: 既有測試全部 `Passed`（`[Explicit]` 的 5 個 `*AggregationComparisonTests` 會被跳過，不算在這次執行內），`Failed: 0`。

- [ ] **Step 9: 執行 5 個 `AggregationComparisonTests`，確認這次重構沒有改變任何既有計算邏輯**

```bash
cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~PhAggregationComparisonTests"
cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~GeptAggregationComparisonTests"
cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~PsAggregationComparisonTests"
cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~PsjAggregationComparisonTests"
cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~AsAggregationComparisonTests"
```
Expected: 全部 5 個 `Passed`（每個測試內部已經把「非預期落差」跟「已知預期落差」分開判斷並各自 assert；這次改動除了 Task 1 的 `IsManual` 跳過判斷，沒有更動任何計算邏輯，且 dev DB 現存資料裡 `IsManual` 目前全部是 `false`（從未被寫入過），理論上不會產生任何新的落差）。如果任何一個測試失敗，先看 `TestContext` 輸出的 unexpected mismatches 清單，確認是不是這次改動不小心動到了 `Compute` 裡的邏輯（Task 1 應該是純重構，不該有任何行為差異）。

- [ ] **Step 10: Commit**

```bash
git add source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml source/portal/Portal/Views/StudentPopulation/CreatePSJPopulation.cshtml source/portal/Portal/Views/StudentPopulation/CreateGeptPopulation.cshtml source/portal/Portal/Views/StudentPopulation/CreatePSPopulation.cshtml source/portal/Portal/Views/StudentPopulation/CreateASPopulation.cshtml
git commit -m "feat: add revertToAuto JS to clear manual pin on IsSum aggregate fields"
```

---

## 最終驗證（全部任務完成後，需要瀏覽器，目前環境沒有瀏覽器工具，留待使用者或之後集中驗證那一輪）

- 用具備 `PopulationWeekSwitch` 權限的帳號登入，找一個 `IsSum=true` 且 `StatisticsType` 不是 `ManualInput`/`None` 的合計欄位（例如百倍速的「本週數學總人數合計」），確認：
  - 原本唯讀的合計欄位變成可編輯輸入框，改了會立即存檔並重算同一張表其他合計欄。
  - 改過之後欄位下方出現「(手動)」標記。
  - 手動改過之後，如果之後來源資料變動導致系統試算值跟手動值不同，會出現「系統試算：N 改用系統試算值」的提示連結。
  - 點擊「改用系統試算值」後，該欄位恢復成自動計算的值，「(手動)」標記消失。
- 用一般（無 `PopulationWeekSwitch` 權限）帳號登入，確認這些合計欄位依然完全唯讀，沒有輸入框、沒有「(手動)」標記。
- 確認 `IsSum=false` 一般欄位、以及新生/流失/本週總詢問(填單)人數這幾個 `ManualInput` 欄位的既有可編輯行為完全沒變。
- 每次手動編輯合計欄位後，用（比照 `feedback_sdd_no_browser_tool`/前一輪計畫慣例的）sqlcmd 確認 `StudentPopulationItemLog` 有正確寫入一筆 log（`Number`/`ChangeNumber` 反映異動前後值）。

## Self-Review

- **Spec 涵蓋度**：spec 的 A（`IsManual` 釘住機制）→ Task 2；B（`Compute`/`Preview` 拆分）→ Task 1；C（差異提示 + 回復自動計算，含 `AttachManualPreviews`/`RevertToAutoCalculation`/畫面調整）→ Task 3/4/5/6。「影響檔案清單」列出的 6 類檔案（`AggregationEngine.cs`、`StudentPopulationController.cs`、`StudentPopulationItem.cs`、`PopulationPartialView.cshtml`、`ASPopulationPartialView.cshtml`、5 個 `CreateXXXPopulation.cshtml`）全部涵蓋。「測試方式」提到的 `dotnet build` + `AggregationComparisonTests` → Task 6 Step 8/9。「Out of scope」三項（PH 大標題式合計顯示、併發衝突、確認視窗）都沒有被任何任務誤觸。
- **Placeholder 掃描**：無 TBD/TODO，每個 Step 都有實際程式碼、實際指令與預期輸出。
- **型別一致性**：`Preview(StudentPopulationItem item, StudentPopulation population)` 回傳 `int?`，Task 3 的 `AttachManualPreviews` 賦值給 `item.PreviewNumber`（同為 `int?`）一致；Task 5 view 讀取 `sItem.PreviewNumber.HasValue`/`.Value` 用法一致。`isAutoComputedSum`/`numberApplied` 皆為 `bool`，只在 Task 2 內部使用，沒有外露給其他任務。`AttachManualPreviews(DataContext dataContext, StudentPopulation population)` 簽章在 Task 3 Step 3 定義後，Step 4-12 呼叫時參數型別（`dataContext`/`returnData`/`lockedData` 皆為對應型別）一致。`RevertToAutoCalculation` 呼叫的 `WriteItemLog` 沿用 Task 2 之前既有簽章（未改動），具名引數 `oldRemark`/`newRemark` 用法跟既有呼叫點一致。
