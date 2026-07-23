# PSJ（百倍速）Excel 版網格輸入介面 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a new, independent "Excel 版" grid input page for PSJ（百倍速）population data — 12 grade rows × 4 subject/classtype columns + 4 analysis columns — that operates on the exact same underlying `StudentPopulation`/`StudentPopulationItem` data as the existing list-style `CreatePSJPopulation` page, reachable from a new, separate menu entry. The old page is untouched and keeps working exactly as before.

**Architecture:** New `CreatePSJGridPopulation` controller action + `CreatePSJGridPopulation.cshtml`/`PSJGridPopulationPartialView.cshtml` views, reusing the existing `StudentPopulation` lookup/creation logic, `SumPHPopulation` aggregation, and `StudentPopulationStatus` workflow verbatim (no changes to shared calculation code). A new shared analysis `CourseDepartment`("百倍速分析總覽", 48 courses) replaces the per-subject 新生/流失/上週比/總人數 for the new page only — old page keeps reading its existing per-subject analysis courses (171-194 數學班分析, 221-244 理化班分析), unaffected. This is a direct port of the AS grid phase 2 pattern (`docs/superpowers/plans/2026-07-20-as-input-grid-phase2.md`) — 3 new grid-specific actions (`AddNewClassGridPsj`/`RemoveClassItemGridPsj`/`UpdateClassItemGridPsj`) render `PSJGridPopulationPartialView`; everything else (`RevertToAutoCalculation`, `UpdateRemark`, `UpdateClassDetail`, `ConfirmPopulation`) already returns plain JSON with no partial dispatch, so those are reused as-is with zero changes.

**Tech Stack:** ASP.NET Core 8 MVC (Razor views), jQuery AJAX, Bootstrap 5 (existing project conventions), EF Core 8.

## Global Constraints

- Design spec: `source/portal/docs/superpowers/specs/2026-07-23-psj-input-grid-design.md`.
- Existing `Course` max `Id` = 525, `CourseDepartment` max `Id` = 46 before this plan (verified live against dev DB 2026-07-23). New shared analysis `CourseDepartment` uses `Id` 47; new `Course` rows use `Id` 526–573 (12 grades × 4 metrics: 上週比/新生/流失/總人數).
- The two SQL scripts for this plan already exist (written during spec/plan authoring): `source/portal/docs/superpowers/sql/2026-07-23-psj-grid-analysis-course-apply.sql` and the matching `-revert.sql`. Task 1 applies and verifies them — it does not need to write them from scratch.
- Database changes in this plan (the Task 1 SQL) must be applied to **both** the local dev DB (`CLOUDFUN-MSI-LE\SQLEXPRESS/NewPAS0716`) **and** the production DB (`20.188.19.77,52056/NewPAS`) — same requirement as AS phase 2. Verify pre-state matches between the two before applying to production, and verify post-state independently on both. **Per project convention, confirm the exact row counts/scope with the user before running anything against the production DB** — do not assume prior approval of the overall PSJ grid feature extends to each individual write step.
- `sqlcmd -i` is unreliable on this machine (can silently no-op with 0 rows affected and no error) — always pass `-f 65001` when using `-i`/`-Q` against a `.sql` file with Chinese text, and always re-verify with an independent `SELECT` afterward regardless of what the console output claimed.
- The new grid does **not** auto-create default "empty" class rows for the 48 (grade × subject × classtype) cells that have no data yet — a cell with zero existing classes shows only a "＋" button, exactly matching the old page's existing behavior and the AS grid's behavior.
- Multi-class support: when a (grade, subject, classtype) cell already has N ≥ 1 classes, the grid renders N input columns for that specific grade in that subject's column-group (other grades in the same column-group render blank cells if they have fewer classes) — the column-group's width is `max(class count across all 12 grades for that subject/classtype)`. This is a per-column-group computation done once per partial-view render (identical logic to the AS grid).
- The 4 analysis columns (新生/流失/上週比/總人數) are single-value editable cells (not multi-class) — they map 1:1 to the new shared `Course` rows 526–573, always exactly one `StudentPopulationItem` per grade (auto-created by the existing generic "增加固定總計項目" loop, which already iterates all `IsSum==true && Type==PSJ` courses — no code change needed there since it's already generic, and the new courses have `GroupByClassType=0` so the loop creates exactly 1 row per grade per course, same as AS).
- Old page (`CreatePSJPopulation`/`PopulationPartialView`) is **not modified in any way** by this plan. It keeps reading the old per-subject analysis courses (171-194/221-244) — this is expected divergence, not a bug (documented in the design spec's Known Risks, identical situation to AS phase 2).
- `AddNewClassGridPsj`/`RemoveClassItemGridPsj`/`UpdateClassItemGridPsj` are **new, separate actions** — do not modify the existing `AddNewClass`/`RemoveClassItem`/`UpdateClassItem` actions (used by PH/PS/PSJ/GEPT/old-AS) or the AS grid's `AddNewClassGrid`/`RemoveClassItemGrid`/`UpdateClassItemGrid` actions, to keep blast radius at zero for every other population type/page.
- `RevertToAutoCalculation`, `UpdateRemark`, `UpdateClassDetail`, `ConfirmPopulation` are reused **unchanged** — they already return plain JSON (no partial-view dispatch), so they work identically regardless of which page called them.
- This controller (`StudentPopulationController`) has zero existing automated test coverage anywhere in the codebase — established project convention defers controller/view verification to manual browser testing, not unit tests. Verification for controller/view tasks in this plan is `dotnet build` (0 errors) + full `dotnet test` suite (no regressions) + a code-reading self-review against the brief, not a new test harness.
- `CourseMapping.GradeOrder` (`Portal/Services/Import/ImportSupport/CourseMapping.cs`) is the canonical 12-grade order to reuse for iterating rows: `一年級,二年級,三年級,四年級,五年級,六年級,國一,國二,國三,高一,高二,高三`.
- 數學班 grade course Ids (145–156) and 理化班 grade course Ids (195–206) are the existing raw per-grade courses (one-to-one/group differentiated via `Class.Type`), verified live against the dev DB 2026-07-23. Do not confuse these with the analysis courses being replaced (171-194/221-244).

---

### Task 1: SQL — apply the shared "百倍速分析總覽" `CourseDepartment` + 48 `Course` rows

**Files:**
- Already created: `source/portal/docs/superpowers/sql/2026-07-23-psj-grid-analysis-course-apply.sql`
- Already created: `source/portal/docs/superpowers/sql/2026-07-23-psj-grid-analysis-course-revert.sql`

**Interfaces:**
- Produces: `Course` rows 526–573, `CourseDepartment` row 47 — Tasks 2–4 all depend on these Ids existing in both dev and prod DBs before their own steps run.

- [ ] **Step 1: Confirm the apply script's contents**

Open `source/portal/docs/superpowers/sql/2026-07-23-psj-grid-analysis-course-apply.sql` and confirm: one `CourseDepartment` insert (Id 47, `Type=1` matching `StudentPopulationType.PSJ`, `IsSum=1`, `Published=0`), one `Course` insert with 48 rows (Ids 526–573), `SourceCourseIds` on the 526–537 (上週比) and 562–573 (總人數) ranges each referencing 2 course Ids per grade in the pattern `[數學,理化]` (e.g. course 526 → `[145,195]`, course 537 → `[156,206]`). If it doesn't match, re-derive from the design spec §"詳細設計" §1-2 before proceeding.

- [ ] **Step 2: Apply to the dev DB**

```bash
sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -U sa -P "cloudfun@12" -d NewPAS0716 -f 65001 -i "source/portal/docs/superpowers/sql/2026-07-23-psj-grid-analysis-course-apply.sql"
```

- [ ] **Step 3: Verify dev with an independent SELECT**

```bash
sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -U sa -P "cloudfun@12" -d NewPAS0716 -f 65001 -Q "SET NOCOUNT ON; SELECT COUNT(*) AS DeptCount FROM CourseDepartment WHERE Id = 47; SELECT COUNT(*) AS CourseCount FROM Course WHERE Id BETWEEN 526 AND 573; SELECT Id, StatisticsType, SourceCourseIds FROM Course WHERE Id IN (526,537,538,550,562,573) ORDER BY Id;" -W -s"|"
```

Expected: `DeptCount`=1, `CourseCount`=48, and the 6 spot-checked rows show `StatisticsType`=10/10/50/50/4/4 respectively with `SourceCourseIds` = `[145,195]` (526), `[156,206]` (537), `NULL` (538, 550), `[145,195]` (562), `[156,206]` (573).

- [ ] **Step 4: Confirm scope with the user, then verify production's pre-state matches dev, then apply to production**

Before running anything against the production DB, report the exact change to the user (1 `CourseDepartment` row + 48 `Course` rows, Ids 47 and 526–573, no existing data touched) and get explicit go-ahead — per project convention, prior approval of the overall PSJ grid feature does not itself authorize this write.

```bash
sqlcmd -S 20.188.19.77,52056 -U pcmdba -P "Phpcm539@6@52" -d NewPAS -C -f 65001 -Q "SET NOCOUNT ON; SELECT MAX(Id) AS MaxCourseId FROM Course; SELECT MAX(Id) AS MaxDeptId FROM CourseDepartment; SELECT COUNT(*) AS ExistingInRange FROM Course WHERE Id BETWEEN 526 AND 573;"
```

Expected: `MaxCourseId`=525, `MaxDeptId`=46, `ExistingInRange`=0 (matches dev's pre-Task-1 state exactly). If it doesn't match, STOP and report to the user before applying anything to production.

```bash
sqlcmd -S 20.188.19.77,52056 -U pcmdba -P "Phpcm539@6@52" -d NewPAS -C -f 65001 -i "source/portal/docs/superpowers/sql/2026-07-23-psj-grid-analysis-course-apply.sql"
```

- [ ] **Step 5: Verify production with the same independent SELECT as Step 3**

```bash
sqlcmd -S 20.188.19.77,52056 -U pcmdba -P "Phpcm539@6@52" -d NewPAS -C -f 65001 -Q "SET NOCOUNT ON; SELECT COUNT(*) AS DeptCount FROM CourseDepartment WHERE Id = 47; SELECT COUNT(*) AS CourseCount FROM Course WHERE Id BETWEEN 526 AND 573; SELECT Id, StatisticsType, SourceCourseIds FROM Course WHERE Id IN (526,537,538,550,562,573) ORDER BY Id;"
```

Expected: identical to Step 3's dev output.

- [ ] **Step 6: Commit**

```bash
git add source/portal/docs/superpowers/sql/2026-07-23-psj-grid-analysis-course-apply.sql \
        source/portal/docs/superpowers/sql/2026-07-23-psj-grid-analysis-course-revert.sql
git commit -m "feat: add SQL script creating shared PSJ grid analysis CourseDepartment and 48 Course rows"
```

---

### Task 2: `CheckNewLostConsistency` — add the new shared analysis department to the consistency check

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs` (around line 2065, the `PSJ` case of `CheckNewLostConsistency`)

**Interfaces:**
- Consumes: `Course`/`CourseDepartment` rows from Task 1 (department name `百倍速分析總覽`, course names `本週{年級}與上週相比`/`本週{年級}新生人數`/`本週{年級}流失人數`).
- Produces: nothing consumed by later tasks — independent of Tasks 3-4.

`CheckByDiffItem(label, departmentName)` matches on exact `Class.Course.Department.Name` and, within that department, on `Course.Name.Contains(grade)` combined with `Contains("與上週相比")`/`Contains("新生")`/`Contains("流失")`. The new courses' names (`本週一年級與上週相比`, `本週一年級新生人數`, `本週一年級流失人數`, etc.) already satisfy this pattern with no changes needed to `CheckByDiffItem` itself.

- [ ] **Step 1: Add the new call**

In the `PSJ` case of `CheckNewLostConsistency`'s switch statement, currently:

```csharp
                case StudentPopulationType.PSJ:
                    CheckByDiffItem("數學", "數學班分析");
                    CheckByDiffItem("理化", "理化班分析");
                    break;
```

change to:

```csharp
                case StudentPopulationType.PSJ:
                    CheckByDiffItem("數學", "數學班分析");
                    CheckByDiffItem("理化", "理化班分析");
                    CheckByDiffItem("百倍速(新網格)", "百倍速分析總覽");
                    break;
```

- [ ] **Step 2: Build and confirm 0 errors**

```bash
dotnet build source/portal/PHStatistics.portal.sln
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 3: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: check new/lost consistency for the shared PSJ grid analysis department"
```

---

### Task 3: Routing scaffold + read-only grid render

**Files:**
- Modify: `source/portal/Portal/Views/Home/Index.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/Index.cshtml`
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`
- Create: `source/portal/Portal/Views/StudentPopulation/CreatePSJGridPopulation.cshtml`
- Create: `source/portal/Portal/Views/StudentPopulation/PSJGridPopulationPartialView.cshtml`

**Interfaces:**
- Consumes: `ResolveSchoolYear`, `AttachManualPreviews`, `SumPHPopulation`, `CheckNewLostConsistency` (all pre-existing private/internal methods on `StudentPopulationController`, reused verbatim).
- Produces: `CreatePSJGridPopulation` action route (`/StudentPopulation/CreatePSJGridPopulation?schoolId=&type=PSJGrid&schoolYearId=`) and the `PSJGridPopulationPartialView` partial (renders into `#gridContentItem` via AJAX in Task 4) — Task 4's 3 new actions render this same partial.

This task delivers a fully working, real-data grid render (read-only: inputs exist and show correct values, but no `onchange`/`+`/remove wiring yet — that's Task 4). Verification is manual: load the page in a browser against real dev data and visually confirm the grid shows the right numbers in the right cells. No automated test (per Global Constraints).

- [ ] **Step 1: Add the new home page menu entry**

In `source/portal/Portal/Views/Home/Index.cshtml`, change:

```csharp
    var inputItems = new (string Label, string Url)[] {
        ("百瀚",        "/StudentPopulation/Index?type=PH"),
        ("百倍速",      "/StudentPopulation/Index?type=PSJ"),
        ("英檢班/其他", "/StudentPopulation/Index?type=Gept"),
        ("百世",        "/StudentPopulation/Index?type=PS"),
        ("課輔",        "/StudentPopulation/Index?type=AS"),
        ("課輔(新版)",  "/StudentPopulation/Index?type=ASGrid"),
    };
```

to:

```csharp
    var inputItems = new (string Label, string Url)[] {
        ("百瀚",        "/StudentPopulation/Index?type=PH"),
        ("百倍速",      "/StudentPopulation/Index?type=PSJ"),
        ("百倍速(新版)", "/StudentPopulation/Index?type=PSJGrid"),
        ("英檢班/其他", "/StudentPopulation/Index?type=Gept"),
        ("百世",        "/StudentPopulation/Index?type=PS"),
        ("課輔",        "/StudentPopulation/Index?type=AS"),
        ("課輔(新版)",  "/StudentPopulation/Index?type=ASGrid"),
    };
```

- [ ] **Step 2: Add the `PSJGrid` branch to the school/week selector page**

In `source/portal/Portal/Views/StudentPopulation/Index.cshtml`, in the button-rendering block, change:

```csharp
                else if (addType.Equals("PSJ")) {
                    <button type="button" class="btn btn-primary btn-lg btn-round" id="addPSJBtn">
                        開始輸入 <i class="fas fa-paper-plane"></i>
                    </button>
                }
```

to:

```csharp
                else if (addType.Equals("PSJ")) {
                    <button type="button" class="btn btn-primary btn-lg btn-round" id="addPSJBtn">
                        開始輸入 <i class="fas fa-paper-plane"></i>
                    </button>
                }
                else if (addType.Equals("PSJGrid")) {
                    <button type="button" class="btn btn-primary btn-lg btn-round" id="addPSJGridBtn">
                        開始輸入 <i class="fas fa-paper-plane"></i>
                    </button>
                }
```

And in the `<script>` block, add alongside the existing `$('#addPSJBtn')` handler:

```javascript
        $('#addPSJGridBtn').on('click', function () {
            var location = "/StudentPopulation/CreatePSJGridPopulation?schoolId=" + $('#schoolSelect').val() + "&type=PSJGrid" + weekParam();
            window.location.href = location;
        });
```

- [ ] **Step 3: Add the `CreatePSJGridPopulation` action**

In `source/portal/Portal/Controllers/StudentPopulationController.cs`, add this new action immediately after the existing `CreateASGridPopulation` action, which currently ends at line 909 (`return View(returnData); }`), right before the `AddNewClass` action begins at line 911:

```csharp
        //百倍速(新網格)
        [Authorize(typeof(PortalUser))]
        public IActionResult CreatePSJGridPopulation(int schoolId, string type, int? schoolYearId = null) {
            DataContext dataContext = new DataContext();
            SchoolYear schoolYear = ResolveSchoolYear(dataContext, schoolYearId);
            SchoolYear lastschoolYear = schoolYear.Week > 1
                    ? dataContext.SchoolYear.Where(e => e.Year == schoolYear.Year && e.Week == schoolYear.Week - 1).OrderBy(e => e.Id).FirstOrDefault()
                    : dataContext.SchoolYear.Where(e => e.Year == schoolYear.Year - 1).OrderByDescending(e => e.Week).ThenByDescending(e => e.Id).FirstOrDefault();
            StudentPopulation lastWeekData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == lastschoolYear.Year && e.Week == lastschoolYear.Week && e.Type == StudentPopulationType.PSJ).FirstOrDefault();

            StudentPopulation returnData;
            List<Course> courses = Model.DataContext.Course.Where(e => e.Type == StudentPopulationType.PSJ).OrderBy(e => e.Ordinal).ToList();
            ViewBag.Year = schoolYear.Year;
            ViewBag.Week = schoolYear.Week;
            ViewBag.Courses = courses;
            ViewBag.SelectedYear = schoolYear;
            ViewBag.CanEditLastWeek = User.HasPermission(SystemPermission.PopulationWeekSwitch);
            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == StudentPopulationType.PSJ)) {
                returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == StudentPopulationType.PSJ);
                foreach (StudentPopulationItem sItem in returnData.Items) {
                    if (lastWeekData != null && lastWeekData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                        sItem.LastWeekNumber = lastWeekData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id).Number;
                    }
                }
                dataContext.SaveChanges();
                //補上此人數表建立之後才新增的固定總計項目（例如這次新增的共用分析課程 526-573），避免舊人數表開啟網格版時分析欄空白
                bool backfilledAny = false;
                foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PSJ).OrderBy(e => e.Ordinal).ToList()) {
                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.General && e.StudentPopulation.Id == returnData.Id)) {
                        StudentPopulationItem item = new StudentPopulationItem();
                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.General);
                        if (classItem == null) {
                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.General };
                            dataContext.Class.Add(classItem);
                            dataContext.SaveChanges();
                        }
                        item.Name = course.Name;
                        item.SchoolName = course.Name;
                        item.Class = classItem;
                        item.Number = 0;
                        item.LastWeekNumber = lastWeekData?.Items?.FirstOrDefault(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.General)?.Number ?? 0;
                        item.IsSum = true;
                        item.StudentPopulationId = returnData.Id;
                        dataContext.StudentPopulationItem.Add(item);
                        backfilledAny = true;
                    }
                }
                if (backfilledAny) {
                    dataContext.SaveChanges();
                    SumPHPopulation(returnData.Id);
                    dataContext.ChangeTracker.Clear();
                    returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.Id == returnData.Id);
                }
            }
            else {
                returnData = new StudentPopulation();
                returnData.School = dataContext.School.Find(schoolId);
                returnData.Year = schoolYear.Year.Value;
                returnData.Week = schoolYear.Week.Value;
                returnData.WeekDate = schoolYear.WeekStartDate;
                returnData.Items = new List<StudentPopulationItem>();
                returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
                returnData.Type = StudentPopulationType.PSJ;
                returnData.Name = string.Format("{0}第{1}週百倍速人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                dataContext.StudentPopulation.Add(returnData);
                dataContext.SaveChanges();
                if (lastWeekData != null && lastWeekData.Items != null && lastWeekData.Items.Count > 0) {
                    foreach (StudentPopulationItem lItem in lastWeekData.Items) {
                        if (!lItem.Class.Course.IsSum && !dataContext.StudentPopulationItem.Any(e => e.Class.Id == lItem.Class.Id && e.StudentPopulation.Id == returnData.Id)) {
                            StudentPopulationItem item = new StudentPopulationItem();
                            Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == lItem.Class.Course.Id && e.Type == lItem.Class.Type && e.Name == lItem.Class.Name);
                            if (classItem == null) {
                                classItem = new Class() { SchoolId = schoolId, CourseId = lItem.Class.Course.Id, Name = lItem.Class.Name, Type = lItem.Class.Type };
                                dataContext.Class.Add(classItem);
                                dataContext.SaveChanges();
                            }
                            item.Name = lItem.Name;
                            item.SchoolName = lItem.Class.Course.Name;
                            item.Class = classItem;
                            item.Number = lItem.Class.Course.IsSum ? 0 : lItem.Number;
                            item.LastWeekNumber = lItem.Number;
                            item.IsSum = lItem.Class.Course.IsSum;
                            returnData.Items.Add(item);
                        }
                    }
                }
                dataContext.SaveChanges();
                //增加固定總計項目（涵蓋所有 IsSum 課程，含既有數學班/理化班合計與分析、以及這次新增的共用分析課程 526-573）
                foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PSJ).OrderBy(e => e.Ordinal).ToList()) {
                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.General && e.StudentPopulation.Id == returnData.Id)) {
                        StudentPopulationItem item = new StudentPopulationItem();
                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.General);
                        if (classItem == null) {
                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.General };
                            dataContext.Class.Add(classItem);
                            dataContext.SaveChanges();
                        }
                        item.Name = course.Name;
                        item.SchoolName = course.Name;
                        item.Class = classItem;
                        item.Number = 0;
                        item.LastWeekNumber = lastWeekData?.Items?.FirstOrDefault(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.General)?.Number ?? 0;
                        item.IsSum = true;
                        returnData.Items.Add(item);
                    }
                }
                dataContext.SaveChanges();
                SumPHPopulation(returnData.Id);
                dataContext.ChangeTracker.Clear();
                returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.Id == returnData.Id);
            }
            AttachManualPreviews(dataContext, returnData);
            return View(returnData);
        }
```

This mirrors `CreateASGridPopulation` exactly (same population lookup/creation, same last-week carry-over, same fixed-IsSum-item backfill loop — which already covers the new 526-573 courses generically since it filters only on `IsSum==true && Type==PSJ`, and those courses have `GroupByClassType=0` so exactly 1 `ClassType.General` item is created per grade, same as `CreatePSJPopulation`'s existing `targetTypes` branch for non-`GroupByClassType` courses) but does **not** filter/pass `CourseDepartment` in `ViewBag` (the grid partial doesn't need a "新增班級" department dropdown) and does not set `ViewBag.CourseDepartment`.

- [ ] **Step 4: Build and confirm 0 errors**

```bash
dotnet build source/portal/PHStatistics.portal.sln
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 5: Create `CreatePSJGridPopulation.cshtml`**

Create `source/portal/Portal/Views/StudentPopulation/CreatePSJGridPopulation.cshtml`:

```html
@using PHStatistics.Content
@model StudentPopulation
@{
    int yearStr = (int)ViewBag.Year;
    int weekStr = (int)ViewBag.Week;
}

<section>
    <div class="py-5 bg-lightbg">
        <div class="container mb-3">
            <fieldset class="center"><h1>@yearStr 年第 @weekStr 週百倍速輸入（網格版）</h1></fieldset>
            @{
                string statusBadgeClass = Model.Status == StudentPopulationStatus.Documented ? "bg-secondary" : "bg-success";
                string statusText = Model.Status switch {
                    StudentPopulationStatus.Documented => "建檔中",
                    StudentPopulationStatus.Pending => "已送出",
                    StudentPopulationStatus.Approved => "已審核",
                    StudentPopulationStatus.Rejected => "已否決",
                    StudentPopulationStatus.Finished => "已完成",
                    _ => Model.Status.ToString()
                };
            }
            <fieldset class="center"><span class="badge @statusBadgeClass fs-6">@statusText</span></fieldset>
            <input type="hidden" id="PopulationId" value="@Model.Id" />
            <input type="hidden" id="SchoolId" value="@Model.School.Id" />
            <div id="gridContentItem" class="table-responsive">
                @await Html.PartialAsync("PSJGridPopulationPartialView", Model)
            </div>
            @if (Model.Status == StudentPopulationStatus.Documented) {
                <div class="mt-3 text-center">
                    <button type="button" class="btn btn-success btn-lg btn-round" onclick="confirmGridPopulation(@Model.Id)">
                        確認送出 <i class="fas fa-paper-plane"></i>
                    </button>
                </div>
            }
        </div>
    </div>
</section>

<div class="modal fade" id="gridAddClassModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">新增班級</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <input type="hidden" id="grid_new_course_id" value="">
                <input type="hidden" id="grid_new_class_type" value="">
                <div class="mb-3">
                    <label class="form-label">班級名稱</label>
                    <input type="text" class="form-control" id="grid_new_class_name" value="">
                </div>
                <div class="mb-3">
                    <label class="form-label">本週人數</label>
                    <input type="number" class="form-control" id="grid_new_number" value="0" min="0">
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">取消</button>
                <button type="button" class="btn btn-primary" id="gridAddClassSubmitBtn" onclick="submitGridAddClass()">新增</button>
            </div>
        </div>
    </div>
</div>

<script src="~/js/studentPopulationLoading.js"></script>
<script>
    var gridAddClassModal = new bootstrap.Modal(document.getElementById('gridAddClassModal'));

    function openGridAddClassModal(courseId, classType) {
        $('#grid_new_course_id').val(courseId);
        $('#grid_new_class_type').val(classType);
        $('#grid_new_class_name').val('');
        $('#grid_new_number').val(0);
        gridAddClassModal.show();
    }

    function submitGridAddClass() {
        var submitBtn = document.getElementById('gridAddClassSubmitBtn');
        submitBtn.disabled = true;
        $.ajax({
            url: '@Url.Action("AddNewClassGridPsj", "StudentPopulation", new { area = "" })',
            type: 'POST',
            data: {
                populationId: $('#PopulationId').val(),
                courseId: $('#grid_new_course_id').val(),
                classType: $('#grid_new_class_type').val(),
                newClassName: $('#grid_new_class_name').val(),
                newNumber: $('#grid_new_number').val()
            },
            dataType: 'html',
            success: function (data) {
                $('#gridContentItem').html(data);
                gridAddClassModal.hide();
                submitBtn.disabled = false;
            },
            error: function () {
                alert('新增班級失敗!!');
                submitBtn.disabled = false;
            }
        });
    }

    function gridRemoveClassItem(sId) {
        if (!confirm('確認要刪除班級資料!?')) return;
        $.ajax({
            url: '@Url.Action("RemoveClassItemGridPsj", "StudentPopulation", new { area = "" })',
            type: 'POST',
            data: { sId: sId },
            dataType: 'html',
            success: function (data) {
                $('#gridContentItem').html(data);
            },
            error: function () {
                alert('刪除失敗，請稍後再試');
            }
        });
    }

    function gridValueChange(elementId) {
        var input = document.getElementById(elementId);
        $.ajax({
            url: '@Url.Action("UpdateClassItemGridPsj", "StudentPopulation", new { area = "" })',
            type: 'POST',
            data: { sId: input.dataset.sitem, number: input.value },
            dataType: 'html',
            success: function (data) {
                $('#gridContentItem').html(data);
            },
            error: function () {
                alert('儲存失敗，請稍後再試');
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

    function confirmGridPopulation(populationId) {
        if (!confirm('確認要送出人數表？送出後本週資料將無法修改，如需調整須由管理人員解鎖並說明原因。')) return;
        $.ajax({
            url: '@Url.Action("ConfirmPopulation", "StudentPopulation", new { area = "" })',
            type: 'POST',
            data: { populationId: populationId },
            success: function (res) {
                if (res.success) {
                    alert('人數表已成功送出！');
                    location.reload();
                } else {
                    alert(res.message || '送出失敗');
                }
            },
            error: function () {
                alert('送出失敗，請稍後再試');
            }
        });
    }
</script>
```

(This includes the full modal + script block up front, unlike AS phase 2's plan which added it in a later task — since we already know the final shape from the AS grid's finished implementation, there is no need for an intermediate read-only-only script block. `AddNewClassGridPsj`/`RemoveClassItemGridPsj`/`UpdateClassItemGridPsj` are added in Task 4 before this page is loaded in a browser, so nothing here is dead code at verification time.)

- [ ] **Step 6: Create `PSJGridPopulationPartialView.cshtml` (read-only render)**

Create `source/portal/Portal/Views/StudentPopulation/PSJGridPopulationPartialView.cshtml`:

```html
@using PHStatistics.Content
@using PHStatistics.Portal.Services.Import.ImportSupport
@model StudentPopulation
@{
    string[] grades = CourseMapping.GradeOrder;

    var mainColumns = new (string Label, int[] CourseIds, ClassType Type)[] {
        ("數學-一對一", new[]{145,146,147,148,149,150,151,152,153,154,155,156}, ClassType.Personal),
        ("數學-團體",   new[]{145,146,147,148,149,150,151,152,153,154,155,156}, ClassType.General),
        ("理化-一對一", new[]{195,196,197,198,199,200,201,202,203,204,205,206}, ClassType.Personal),
        ("理化-團體",   new[]{195,196,197,198,199,200,201,202,203,204,205,206}, ClassType.General),
    };
    var analysisColumns = new (string Label, int[] CourseIds)[] {
        ("上週比", new[]{526,527,528,529,530,531,532,533,534,535,536,537}),
        ("新生",   new[]{538,539,540,541,542,543,544,545,546,547,548,549}),
        ("流失",   new[]{550,551,552,553,554,555,556,557,558,559,560,561}),
        ("總人數", new[]{562,563,564,565,566,567,568,569,570,571,572,573}),
    };

    // 每個主要欄位群組跨 12 年級的最大重複班級數，決定要渲染幾個子欄
    int[] maxRepeat = new int[mainColumns.Length];
    for (int c = 0; c < mainColumns.Length; c++) {
        int max = 1;
        for (int g = 0; g < grades.Length; g++) {
            int count = Model.Items.Count(i => i.Class.CourseId == mainColumns[c].CourseIds[g] && i.Class.Type == mainColumns[c].Type);
            if (count > max) max = count;
        }
        maxRepeat[c] = max;
    }
}
<table class="table table-bordered table-sm align-middle" id="psjGridTable">
    <thead>
        <tr>
            <th rowspan="2">年級</th>
            @for (int c = 0; c < mainColumns.Length; c++) {
                <th colspan="@maxRepeat[c]">@mainColumns[c].Label</th>
            }
            <th colspan="4">分析</th>
        </tr>
        <tr>
            @for (int c = 0; c < mainColumns.Length; c++) {
                for (int k = 0; k < maxRepeat[c]; k++) {
                    <th>@(k == 0 ? mainColumns[c].Label : $"{mainColumns[c].Label}({k + 1})")</th>
                }
            }
            @foreach (var a in analysisColumns) {
                <th>@a.Label</th>
            }
        </tr>
    </thead>
    <tbody>
        @for (int g = 0; g < grades.Length; g++) {
            <tr>
                <td>@grades[g]</td>
                @for (int c = 0; c < mainColumns.Length; c++) {
                    var cellItems = Model.Items.Where(i => i.Class.CourseId == mainColumns[c].CourseIds[g] && i.Class.Type == mainColumns[c].Type).OrderBy(i => i.Id).ToList();
                    for (int k = 0; k < maxRepeat[c]; k++) {
                        <td>
                            @if (k < cellItems.Count) {
                                StudentPopulationItem item = cellItems[k];
                                <input type="number" class="form-control form-control-sm" style="width:70px;display:inline-block"
                                       value="@item.Number" data-sitem="@item.Id" id="grid_@(item.Id)_count"
                                       onchange="gridValueChange('grid_@(item.Id)_count')">
                                @if (k == cellItems.Count - 1) {
                                    <button type="button" class="btn btn-sm btn-outline-primary" title="新增班級"
                                            onclick="openGridAddClassModal(@mainColumns[c].CourseIds[g], @((int)mainColumns[c].Type))">＋</button>
                                }
                                <button type="button" class="btn btn-sm btn-outline-danger" title="刪除班級"
                                        onclick="gridRemoveClassItem(@item.Id)">×</button>
                            }
                            else if (k == 0) {
                                <button type="button" class="btn btn-sm btn-outline-primary" title="新增班級"
                                        onclick="openGridAddClassModal(@mainColumns[c].CourseIds[g], @((int)mainColumns[c].Type))">＋</button>
                            }
                        </td>
                    }
                }
                @foreach (var a in analysisColumns) {
                    StudentPopulationItem aItem = Model.Items.FirstOrDefault(i => i.Class.CourseId == a.CourseIds[g]);
                    <td>
                        @if (aItem != null) {
                            <input type="number" class="form-control form-control-sm" style="width:70px;display:inline-block"
                                   value="@aItem.Number" data-sitem="@aItem.Id" id="grid_@(aItem.Id)_count"
                                   onchange="gridValueChange('grid_@(aItem.Id)_count')">
                            @if (aItem.IsManual) {
                                <div class="small text-muted">(手動)</div>
                                @if (aItem.PreviewNumber.HasValue && aItem.PreviewNumber.Value != aItem.Number) {
                                    <div class="small text-warning">
                                        試算：@aItem.PreviewNumber
                                        <a href="javascript:void(0)" onclick="revertToAuto(@aItem.Id)">改用系統試算值</a>
                                    </div>
                                }
                            }
                        }
                    </td>
                }
            </tr>
        }
    </tbody>
</table>
```

This is a direct port of `ASGridPopulationPartialView.cshtml` with `mainColumns` swapped to the 4 PSJ subject/classtype columns and `analysisColumns` swapped to course Ids 526–573. `revertToAuto`/`gridValueChange`/`gridRemoveClassItem`/`openGridAddClassModal` are defined in `CreatePSJGridPopulation.cshtml`'s `<script>` block (Step 5).

- [ ] **Step 7: Manual smoke check**

Start the app (`dotnet run --project source/portal/Portal/Portal.csproj`), log in, go to `/StudentPopulation/Index?type=PSJGrid`, pick a school with existing PSJ data for the current week, click 開始輸入. Confirm: the grid renders 12 rows, the 4 main columns + 4 analysis columns show the right header labels, and any existing class data appears with the correct numbers in the right cells (cross-check 2-3 values against the same school/week's old `CreatePSJPopulation` page — note the analysis columns are expected to differ per the design spec's Known Risks, since they read a different, newly-backfilled data source; only the 4 main數學/理化 columns should match exactly). No interactivity is expected yet (`onchange`/`+`/`×` will not do anything until Task 4 — that's fine for this step, though because Step 5/6 already wired the JS/actions ahead of time, this may already work; if so, that's a bonus, not a regression).

- [ ] **Step 8: Commit**

```bash
git add source/portal/Portal/Views/Home/Index.cshtml \
        source/portal/Portal/Views/StudentPopulation/Index.cshtml \
        source/portal/Portal/Controllers/StudentPopulationController.cs \
        source/portal/Portal/Views/StudentPopulation/CreatePSJGridPopulation.cshtml \
        source/portal/Portal/Views/StudentPopulation/PSJGridPopulationPartialView.cshtml
git commit -m "feat: add PSJ grid input page routing and grid render"
```

---

### Task 4: New AJAX actions + full grid interactivity

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes: nothing new — mirrors `AddNewClassGrid`/`RemoveClassItemGrid`/`UpdateClassItemGrid` (AS grid) exactly except for the returned partial, population type filter, and action route names.
- Produces: nothing consumed by later tasks — this is the last task in this plan. `CreatePSJGridPopulation.cshtml`'s script block (Task 3 Step 5) already calls these action names, so this task makes that page's interactivity actually work end-to-end.

- [ ] **Step 1: Add `AddNewClassGridPsj`**

In `source/portal/Portal/Controllers/StudentPopulationController.cs`, add this new action immediately after the existing `UpdateClassItemGrid` action (the AS grid's, ends around line 1430):

```csharp
        [Authorize(typeof(PortalUser))]
        [HttpPost("AddNewClassGridPsj")]
        public IActionResult AddNewClassGridPsj(long populationId, int courseId, int classType, string newClassName, int newNumber) {
            DataContext dataContext = new DataContext();
            StudentPopulation studentPopulationData = dataContext.StudentPopulation.Include("Items.Class.Course").Include("School").FirstOrDefault(e => e.Id == populationId);
            if (studentPopulationData == null)
                return Json(new { success = false, message = "找不到人數表" });

            bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
            ViewBag.CanEditLastWeek = canEditLocked;
            ViewBag.Courses = dataContext.Course.Where(e => e.Type == StudentPopulationType.PSJ).OrderBy(e => e.Ordinal).ToList();
            if (studentPopulationData.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                AttachManualPreviews(dataContext, studentPopulationData);
                return PartialView("PSJGridPopulationPartialView", studentPopulationData);
            }

            try {
                Course course = dataContext.Course.Find(courseId);
                int classCount = studentPopulationData.Items.Count(e => e.Class.Course.Id == courseId && e.Class.Type == (ClassType)classType);
                Class newClass = new Class {
                    SchoolId = studentPopulationData.School.Id,
                    CourseId = courseId,
                    Type = (ClassType)classType,
                    Name = string.IsNullOrEmpty(newClassName) ? string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00")) : newClassName,
                };
                dataContext.Class.Add(newClass);
                dataContext.SaveChanges();

                StudentPopulationItem addItem = new StudentPopulationItem {
                    ClassId = newClass.Id,
                    Name = newClass.Name,
                    Number = newNumber,
                    SchoolName = newClass.Name,
                    LastWeekNumber = 0,
                    StudentPopulationId = studentPopulationData.Id,
                };
                dataContext.StudentPopulationItem.Add(addItem);
                dataContext.SaveChanges();
                WriteItemLog(dataContext, studentPopulationData.Id, newClass.Id, addItem.Name, 0, addItem.Number, 0, addItem.LastWeekNumber, null, addItem.StudentRemark, isNew: true);
                SumPHPopulation(studentPopulationData.Id);
            }
            catch (Exception ex) {
                Logger.LogError(ex, "AddNewClassGridPsj populationId={populationId} courseId={courseId}", populationId, courseId);
            }

            dataContext.ChangeTracker.Clear();
            var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").FirstOrDefault(e => e.Id == populationId);
            AttachManualPreviews(dataContext, returnData);
            return PartialView("PSJGridPopulationPartialView", returnData);
        }
```

- [ ] **Step 2: Add `RemoveClassItemGridPsj`**

```csharp
        [Authorize(typeof(PortalUser))]
        [HttpPost("RemoveClassItemGridPsj")]
        public IActionResult RemoveClassItemGridPsj(long sId) {
            DataContext dataContext = new DataContext();
            try {
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("StudentPopulation").FirstOrDefault(e => e.Id == sId);
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });

                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
                ViewBag.CanEditLastWeek = canEditLocked;
                ViewBag.Courses = dataContext.Course.Where(e => e.Type == StudentPopulationType.PSJ).OrderBy(e => e.Ordinal).ToList();
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").FirstOrDefault(e => e.Id == item.StudentPopulationId);
                    AttachManualPreviews(dataContext, lockedData);
                    return PartialView("PSJGridPopulationPartialView", lockedData);
                }

                long spId = item.StudentPopulationId;
                dataContext.StudentPopulationItem.Remove(item);
                dataContext.SaveChanges();
                WriteItemLog(dataContext, spId, item.ClassId, item.Name, item.Number, 0, item.LastWeekNumber, 0, item.StudentRemark, null, isDeleted: true);
                SumPHPopulation(spId);

                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").FirstOrDefault(e => e.Id == spId);
                AttachManualPreviews(dataContext, returnData);
                return PartialView("PSJGridPopulationPartialView", returnData);
            }
            catch (Exception ex) {
                ViewBag.Courses = new List<Course>();
                Logger.LogError(ex, "RemoveClassItemGridPsj sId={sId}", sId);
                return PartialView("PSJGridPopulationPartialView", new StudentPopulation());
            }
        }
```

- [ ] **Step 3: Add `UpdateClassItemGridPsj`**

```csharp
        [Authorize(typeof(PortalUser))]
        [HttpPost("UpdateClassItemGridPsj")]
        public IActionResult UpdateClassItemGridPsj(long sId, int? number, int? lastWeekNumber = null) {
            DataContext dataContext = new DataContext();
            try {
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("Class.Course.Department").Include("StudentPopulation").FirstOrDefault(e => e.Id == sId);
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });

                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
                ViewBag.CanEditLastWeek = canEditLocked;
                ViewBag.Courses = dataContext.Course.Where(e => e.Type == StudentPopulationType.PSJ).OrderBy(e => e.Ordinal).ToList();
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").FirstOrDefault(e => e.Id == item.StudentPopulation.Id);
                    AttachManualPreviews(dataContext, lockedData);
                    return PartialView("PSJGridPopulationPartialView", lockedData);
                }

                bool lastWeekApplied = lastWeekNumber.HasValue && canEditLocked;
                bool isAutoComputedSum = item.Class?.Course?.IsSum == true
                    && item.Class?.Course?.StatisticsType != null
                    && item.Class.Course.StatisticsType != StatisticsType.None
                    && item.Class.Course.StatisticsType != StatisticsType.ManualInput;
                bool numberApplied = number.HasValue && (!isAutoComputedSum || canEditLocked);
                int oldNumber = item.Number;
                int oldLastWeekNumber = item.LastWeekNumber;
                if (numberApplied) {
                    item.Number = number.Value;
                    if (isAutoComputedSum) {
                        item.IsManual = true;
                    }
                }
                if (lastWeekApplied) {
                    item.LastWeekNumber = lastWeekNumber.Value;
                }
                dataContext.StudentPopulationItem.Update(item);
                dataContext.SaveChanges();
                if (numberApplied || lastWeekApplied) {
                    WriteItemLog(dataContext, item.StudentPopulationId, item.ClassId, item.Name, oldNumber, item.Number, oldLastWeekNumber, item.LastWeekNumber, item.StudentRemark, item.StudentRemark);
                    SumPHPopulation(item.StudentPopulation.Id);
                }

                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").FirstOrDefault(e => e.Id == item.StudentPopulation.Id);
                AttachManualPreviews(dataContext, returnData);
                return PartialView("PSJGridPopulationPartialView", returnData);
            }
            catch (Exception ex) {
                ViewBag.Courses = new List<Course>();
                Logger.LogError(ex, "UpdateClassItemGridPsj sId={sId}", sId);
                return PartialView("PSJGridPopulationPartialView", new StudentPopulation());
            }
        }
```

(This intentionally omits the `studentRemark` parameter the old `UpdateClassItem` supports — the grid has no remark column per the design spec's layout, same as the AS grid.)

- [ ] **Step 4: Build and confirm 0 errors**

```bash
dotnet build source/portal/PHStatistics.portal.sln
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 5: Manual browser verification**

Load `/StudentPopulation/CreatePSJGridPopulation?schoolId=...&type=PSJGrid` for a `Documented`-status population and confirm:
1. Editing a number in an existing cell and tabbing away saves it (grid re-renders, value persists on a hard refresh).
2. Clicking "＋" on an empty cell opens the modal; submitting creates a new class and the cell now shows an input with that value.
3. Clicking "＋" on a cell that already has 1 class adds a 2nd column for that specific grade/subject, other grades' same column stay blank.
4. Clicking "×" removes a class; if it was the last one in an extra column, that column collapses back out for all grades on the next render.
5. An analysis column (e.g. 上週比) shows the auto-computed value; editing it directly marks it `(手動)` and shows the 改用系統試算值 link; clicking that link reloads the page and reverts to the computed value.
6. 確認送出 button locks the population (status changes, further edits blocked) — matches existing behavior.
7. The `CheckByDiffItem("百倍速(新網格)", "百倍速分析總覽")` warning (Task 2) fires when 上週比 ≠ 新生 − 流失 for a grade, and does not false-positive when they're consistent.

- [ ] **Step 6: Run the full test suite to confirm no regression in the rest of the app**

```bash
dotnet test source/portal/Test/Test.csproj
```

Expected: no new failures (this plan added no automated tests, per Global Constraints — existing suite should show the same pass/skip counts as before this plan started).

- [ ] **Step 7: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: add PSJ grid-specific AJAX actions and complete grid interactivity"
```

---

## After this plan

- Manual browser verification (Task 3 Step 7, Task 4 Step 5) is the primary quality gate for this plan, per established project convention for controller/view work — per the design spec, this is deferred to a later consolidated verification round rather than done immediately after each task.
- Old `CreatePSJPopulation` page removal is a future task once the new grid is confirmed stable in production use — not part of this plan (see design spec's Out of Scope).
- Import/export (`PSJPopulationImporter`/`ReportExportService`) reading/writing the new shared analysis courses (526–573) is out of scope for this plan (see design spec's Out of Scope) — a future task if ever needed.
