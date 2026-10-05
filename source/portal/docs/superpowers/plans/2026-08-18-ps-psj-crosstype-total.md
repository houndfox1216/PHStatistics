# PS報表PSJ跨類型加總 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make PS report's three PSJ-related fields (135 PSJ總人數、139 PSJ上週人數、142 本週變更(PS+PSJ)) automatically pull real data from the same school/week's PSJ report, instead of relying on schools manually copying numbers across.

**Architecture:** Extend `AggregationEngine` with two new cross-`StudentPopulationType` `StatisticsType` cases (`SumFromOtherType`, `LastWeekValueFromOtherType`) backed by a new `Course.SourceStudentPopulationType` field. Generalize the engine's "last week" lookup delegate to accept an explicit `StudentPopulationType` instead of always querying the current population's own type, then wire that through `StudentPopulationController.cs`'s two `AggregationEngine` construction sites. Field 142 needs no new mechanism — it reuses the existing `DiffBetweenCourses` case once 135/139 are computed.

**Tech Stack:** ASP.NET Core 8 MVC, EF Core 8 (SQL Server), NUnit 4.

**Spec:** `source/portal/docs/superpowers/specs/2026-08-17-ps-psj-crosstype-total-design.md`

## Global Constraints

- Do not backfill historical week data — the new formulas only apply going forward, the next time a week's PS report is opened and saved (spec's explicit user-confirmed decision).
- `GetItemsByCourseIds` (not `GetSourceItems`) is the helper both new cases must reuse — it does NOT exclude `IsSum=true` source items, which is required here since PSJ courses 562–573 are themselves aggregate columns.
- Course 135 changes from `IsSum=0` (freely editable) to `IsSum=1` (read-only, computed) for ordinary users. Users holding `SystemPermission.PopulationWeekSwitch` are unaffected — they already have the existing "覆蓋合計欄位" mechanism (manual edit → `IsManual=true` → `RevertToAutoCalculation` to unfreeze), which applies to 135 with zero new code.
- **The Course data-configuration SQL (Task 5) targets a database that is effectively production** (`reference_db_connection_is_production` — `appsettings.json`'s `DataContext` connection string points at the live server: `20.188.19.77,52056` / `NewPAS`). Task 5's apply step must never be run unattended — get the user's explicit go-ahead immediately before running the SQL, and verify with a `SELECT` afterward (this repo's `sqlcmd -i` has been unreliable before — don't trust silent success; also watch for the `QUOTED_IDENTIFIER`/`ANSI_NULLS` session-setting pitfall documented in Task 5).
- No new mechanism for field 142 — it must be configured to reuse the existing `StatisticsType.DiffBetweenCourses` (already implemented, unchanged).

---

### Task 1: Data model — `StatisticsType` new values, `Course.SourceStudentPopulationType`, EF migration (scaffold only, no DB apply)

**Files:**
- Modify: `source/schema/Data/Content/StatisticsType.cs`
- Modify: `source/schema/Data/Content/Course.cs`
- Create: `source/schema/Data/Migrations/<timestamp>_AddCourseSourceStudentPopulationType.cs` (+ matching `.Designer.cs`, generated)
- Modify: `source/schema/Data/Migrations/DataContextModelSnapshot.cs` (generated, updated by the same command)

**Interfaces:**
- Produces: `StatisticsType.SumFromOtherType` (`= 15`), `StatisticsType.LastWeekValueFromOtherType` (`= 16`), `Course.SourceStudentPopulationType` (`StudentPopulationType?`). All three consumed by Task 2.

- [ ] **Step 1: Add the two new `StatisticsType` values**

In `source/schema/Data/Content/StatisticsType.cs`, insert immediately after the existing `YearToDateSum = 14,` member (currently ends at line 93, right before `NewStudents = 20`):

```csharp
    /// <summary>
    /// 指定來源課程加總（來自另一種報表類型的「本週」資料）
    /// </summary>
    [Display(Name = "指定來源課程加總（跨報表類型）")]
    [Description("指定來源課程加總（跨報表類型）")]
    SumFromOtherType = 15,

    /// <summary>
    /// 指定來源課程加總（來自另一種報表類型的「上週」資料）
    /// </summary>
    [Display(Name = "指定來源課程加總（跨報表類型，上週）")]
    [Description("指定來源課程加總（跨報表類型，上週）")]
    LastWeekValueFromOtherType = 16,
```

Every existing member in this enum carries both `[Display(Name=...)]` and `[Description(...)]` — match that convention exactly (the design spec's own snippet only shows `[Display]`; follow the file's actual established pattern, not the abbreviated spec snippet).

- [ ] **Step 2: Add `SourceStudentPopulationType` to `Course`**

In `source/schema/Data/Content/Course.cs`, add immediately after the existing `SourceSubject` property (currently the last property, ends the class at line 196-198):

```csharp
        /// <summary>
        /// 統計來源報表類型（跨Type抓值專用，例如PS抓PSJ當週/上週資料）
        /// </summary>
        [Display(Name = "統計來源報表類型"), DataMember]
        public StudentPopulationType? SourceStudentPopulationType { get; set; }
```

`StudentPopulationType` resolves without a new `using` — `Course` and `StudentPopulationType` are both already in the `PHStatistics.Content` namespace.

- [ ] **Step 3: Build to confirm both changes compile**

Run (from `source/`): `dotnet build portal/PHStatistics.portal.sln`
Expected: `0 個錯誤` (0 errors). Pre-existing warnings unrelated to these files are fine.

- [ ] **Step 4: Scaffold the EF migration (does not touch the database)**

Run (from `source/`):
```bash
dotnet ef migrations add AddCourseSourceStudentPopulationType --project schema/Data/Data.csproj --startup-project portal/Portal/Portal.csproj --output-dir Migrations
```
This only generates C# migration files under `source/schema/Data/Migrations/` and updates `DataContextModelSnapshot.cs` — it does not connect to or modify any database. Do **not** follow this with `dotnet ef database update` against the default connection string (see Task 5 for the correct, gated way to apply schema changes to the target database — this repo's `appsettings.json` currently points at production).

- [ ] **Step 5: Verify the generated migration only does what's expected**

Open the newly generated `<timestamp>_AddCourseSourceStudentPopulationType.cs` and confirm the `Up()` method contains exactly one `AddColumn<short>` (name: `"SourceStudentPopulationType"`, table: `"Course"`, type: `"smallint"`, nullable: true) and nothing else — no index, no FK (this is a plain enum value, not a real foreign key to another table), matching this repo's existing pattern for nullable enum columns, e.g. `source/schema/Data/Migrations/20260730010513_AddCourseNegativeSourceCourseIds.cs` (same shape, but that one is a `string`/`nvarchar` column; this one is a `short`/`smallint` column — compare against `Type = table.Column<short>(type: "smallint", nullable: false)` in `source/schema/Data/Migrations/20240510091255_Initial_Schema.cs` for the smallint shape). If the generated file contains anything beyond a single `AddColumn<short>`, stop and report back rather than proceeding — do not silently accept unexpected schema drift.

- [ ] **Step 6: Commit**

```bash
git add source/schema/Data/Content/StatisticsType.cs source/schema/Data/Content/Course.cs source/schema/Data/Migrations/
git commit -m "feat: add StatisticsType.SumFromOtherType/LastWeekValueFromOtherType and Course.SourceStudentPopulationType (schema only, migration not yet applied)"
```

---

### Task 2: `AggregationEngine` — generalize last-week lookup + add the two new `StatisticsType` cases (TDD)

**Files:**
- Modify: `source/portal/Portal/Services/Aggregation/AggregationEngine.cs`
- Modify: `source/portal/Test/Services/Aggregation/AggregationEngineTests.cs`

**Interfaces:**
- Consumes: `StatisticsType.SumFromOtherType`/`LastWeekValueFromOtherType`, `Course.SourceStudentPopulationType` (Task 1).
- Produces: `AggregationEngine`'s constructor second parameter changes type from `Func<StudentPopulation, StudentPopulation>` to `Func<StudentPopulation, StudentPopulationType, StudentPopulation>`. This is a **breaking signature change** — Task 3 (production call sites) and Task 4 (5 existing comparison test files) both depend on it and must follow this task.

- [ ] **Step 1: Change the constructor's second delegate to take an explicit type, and update `SumLastWeek`'s call site**

In `source/portal/Portal/Services/Aggregation/AggregationEngine.cs`, replace:

```csharp
    private readonly Func<int, int, int, StudentPopulationType, StudentPopulation> _lookupPopulation;
    private readonly Func<StudentPopulation, StudentPopulation> _lookupLastWeekPopulation;

    public AggregationEngine(
        Func<int, int, int, StudentPopulationType, StudentPopulation> lookupPopulation,
        Func<StudentPopulation, StudentPopulation> lookupLastWeekPopulation) {
        _lookupPopulation = lookupPopulation;
        _lookupLastWeekPopulation = lookupLastWeekPopulation;
    }
```

with:

```csharp
    private readonly Func<int, int, int, StudentPopulationType, StudentPopulation> _lookupPopulation;
    private readonly Func<StudentPopulation, StudentPopulationType, StudentPopulation> _lookupLastWeekPopulationOfType;

    public AggregationEngine(
        Func<int, int, int, StudentPopulationType, StudentPopulation> lookupPopulation,
        Func<StudentPopulation, StudentPopulationType, StudentPopulation> lookupLastWeekPopulationOfType) {
        _lookupPopulation = lookupPopulation;
        _lookupLastWeekPopulationOfType = lookupLastWeekPopulationOfType;
    }
```

Then in `SumLastWeek`, replace:

```csharp
    private int SumLastWeek(Course course, StudentPopulationItem item, StudentPopulation population) {
        var lastWeekPopulation = _lookupLastWeekPopulation(population);
```

with:

```csharp
    private int SumLastWeek(Course course, StudentPopulationItem item, StudentPopulation population) {
        var lastWeekPopulation = _lookupLastWeekPopulationOfType(population, population.Type);
```

This is a pure rename + explicit-type-pass-through — behavior for every existing `StatisticsType` case is unchanged (they all still resolve to the population's own type).

- [ ] **Step 2: Fix `AggregationEngineTests.cs` to compile against the new constructor signature**

In `source/portal/Test/Services/Aggregation/AggregationEngineTests.cs`, replace the `MakeEngine` helper:

```csharp
    private static AggregationEngine MakeEngine(
        System.Func<int, int, int, StudentPopulationType, StudentPopulation> lookupLastYearPopulation = null,
        System.Func<StudentPopulation, StudentPopulation> lookupLastWeekPopulation = null) {
        return new AggregationEngine(
            lookupLastYearPopulation ?? ((y, w, s, t) => null),
            lookupLastWeekPopulation ?? (p => null));
    }
```

with:

```csharp
    private static AggregationEngine MakeEngine(
        System.Func<int, int, int, StudentPopulationType, StudentPopulation> lookupLastYearPopulation = null,
        System.Func<StudentPopulation, StudentPopulationType, StudentPopulation> lookupLastWeekPopulation = null) {
        return new AggregationEngine(
            lookupLastYearPopulation ?? ((y, w, s, t) => null),
            lookupLastWeekPopulation ?? ((p, t) => null));
    }
```

Then find the 3 existing call sites that pass `lookupLastWeekPopulation: p => lastWeekPopulation` (in `Calculate_DiffWithLastWeek_SubtractsLastWeekPopulationSumFromThisWeekSum`, `Calculate_LastWeekValue_ReturnsRawLastWeekSumViaLookupDelegate`, and `Calculate_LastWeekValue_UnaffectedByThisWeekItemBeingDeleted`) and replace all 3 occurrences of the literal text `lookupLastWeekPopulation: p => lastWeekPopulation` with `lookupLastWeekPopulation: (p, t) => lastWeekPopulation` (the text is identical in all 3 spots — a single find-and-replace-all covers them).

- [ ] **Step 3: Run the full test project to confirm this refactor introduced no behavior change**

Run (from `source/`): `dotnet test portal/Test/Test.csproj`
Expected: same pass count as before this task (this step is a pure refactor — no new tests yet, no existing test's expected outcome should change).

- [ ] **Step 4: Commit the refactor as its own checkpoint**

```bash
git add -f source/portal/Test/Services/Aggregation/AggregationEngineTests.cs
git add source/portal/Portal/Services/Aggregation/AggregationEngine.cs
git commit -m "refactor: generalize AggregationEngine's last-week lookup to accept an explicit StudentPopulationType"
```

- [ ] **Step 5: Write 2 failing tests for `SumFromOtherType`**

Add to `source/portal/Test/Services/Aggregation/AggregationEngineTests.cs`:

```csharp
    [Test]
    public void Calculate_SumFromOtherType_SumsSourceCoursesFromAnotherPopulationType() {
        var psjCourse1 = MakeCourse(562, departmentId: 47);
        var psjCourse2 = MakeCourse(563, departmentId: 47);
        var sumCourse = MakeCourse(135, departmentId: 26, isSum: true,
            statisticsType: StatisticsType.SumFromOtherType, sourceCourseIds: "[562,563]");
        sumCourse.SourceStudentPopulationType = StudentPopulationType.PSJ;
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PS,
            Items = new List<StudentPopulationItem> { sumItem },
        };
        var psjPopulation = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PSJ,
            Items = new List<StudentPopulationItem> {
                MakeItem(psjCourse1, ClassType.General, 8),
                MakeItem(psjCourse2, ClassType.General, 5),
            },
        };

        var engine = MakeEngine(lookupLastYearPopulation: (year, week, schoolId, type) => {
            Assert.That(year, Is.EqualTo(2026));
            Assert.That(week, Is.EqualTo(10));
            Assert.That(schoolId, Is.EqualTo(1));
            Assert.That(type, Is.EqualTo(StudentPopulationType.PSJ));
            return psjPopulation;
        });
        engine.Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(13));
    }

    [Test]
    public void Calculate_SumFromOtherType_ReturnsZeroWhenSourceStudentPopulationTypeNotSet() {
        var sumCourse = MakeCourse(135, departmentId: 26, isSum: true,
            statisticsType: StatisticsType.SumFromOtherType, sourceCourseIds: "[562,563]");
        // SourceStudentPopulationType intentionally left null
        var sumItem = MakeItem(sumCourse, ClassType.General, 5);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PS,
            Items = new List<StudentPopulationItem> { sumItem },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(0));
    }
```

- [ ] **Step 6: Run to verify both fail**

Run: `dotnet test portal/Test/Test.csproj --filter "FullyQualifiedName~Calculate_SumFromOtherType"`
Expected: both FAIL with `NotSupportedException` ("AggregationEngine 尚未支援 StatisticsType.SumFromOtherType...") — the case doesn't exist in `Compute()` yet.

- [ ] **Step 7: Implement the `SumFromOtherType` case**

In `source/portal/Portal/Services/Aggregation/AggregationEngine.cs`, in the `Compute()` switch, add a new case immediately before the `default:` branch:

```csharp
            case StatisticsType.SumFromOtherType: {
                if (population.SchoolId == null || course.SourceStudentPopulationType == null) return 0;
                var sourcePopulation = _lookupPopulation(population.Year, population.Week, population.SchoolId.Value, course.SourceStudentPopulationType.Value);
                if (sourcePopulation?.Items == null) return 0;
                var ids = ParseIntArray(course.SourceCourseIds);
                return GetItemsByCourseIds(course, item, sourcePopulation.Items, ids).Sum(i => i.Number);
            }
```

- [ ] **Step 8: Run to verify both pass**

Run: `dotnet test portal/Test/Test.csproj --filter "FullyQualifiedName~Calculate_SumFromOtherType"`
Expected: `已通過! - 失敗: 0，通過: 2`

- [ ] **Step 9: Write 2 failing tests for `LastWeekValueFromOtherType`**

Add to the same test file:

```csharp
    [Test]
    public void Calculate_LastWeekValueFromOtherType_SumsSourceCoursesFromAnotherPopulationTypeLastWeek() {
        var psjCourse1 = MakeCourse(562, departmentId: 47);
        var psjCourse2 = MakeCourse(563, departmentId: 47);
        var sumCourse = MakeCourse(139, departmentId: 26, isSum: true,
            statisticsType: StatisticsType.LastWeekValueFromOtherType, sourceCourseIds: "[562,563]");
        sumCourse.SourceStudentPopulationType = StudentPopulationType.PSJ;
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PS,
            Items = new List<StudentPopulationItem> { sumItem },
        };
        var lastWeekPsjPopulation = new StudentPopulation {
            Year = 2026, Week = 9, SchoolId = 1, Type = StudentPopulationType.PSJ,
            Items = new List<StudentPopulationItem> {
                MakeItem(psjCourse1, ClassType.General, 6),
                MakeItem(psjCourse2, ClassType.General, 4),
            },
        };

        var engine = MakeEngine(lookupLastWeekPopulation: (p, type) => {
            Assert.That(p, Is.SameAs(population));
            Assert.That(type, Is.EqualTo(StudentPopulationType.PSJ));
            return lastWeekPsjPopulation;
        });
        engine.Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(10));
    }

    [Test]
    public void Calculate_LastWeekValueFromOtherType_ReturnsZeroWhenSourcePopulationDoesNotExist() {
        var sumCourse = MakeCourse(139, departmentId: 26, isSum: true,
            statisticsType: StatisticsType.LastWeekValueFromOtherType, sourceCourseIds: "[562,563]");
        sumCourse.SourceStudentPopulationType = StudentPopulationType.PSJ;
        var sumItem = MakeItem(sumCourse, ClassType.General, 5);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PS,
            Items = new List<StudentPopulationItem> { sumItem },
        };

        MakeEngine(lookupLastWeekPopulation: (p, type) => null).Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(0));
    }
```

- [ ] **Step 10: Run to verify both fail**

Run: `dotnet test portal/Test/Test.csproj --filter "FullyQualifiedName~Calculate_LastWeekValueFromOtherType"`
Expected: both FAIL with `NotSupportedException`.

- [ ] **Step 11: Implement the `LastWeekValueFromOtherType` case**

Add, right after the `SumFromOtherType` case from Step 7:

```csharp
            case StatisticsType.LastWeekValueFromOtherType: {
                if (population.SchoolId == null || course.SourceStudentPopulationType == null) return 0;
                var sourceLastWeekPopulation = _lookupLastWeekPopulationOfType(population, course.SourceStudentPopulationType.Value);
                if (sourceLastWeekPopulation?.Items == null) return 0;
                var ids = ParseIntArray(course.SourceCourseIds);
                return GetItemsByCourseIds(course, item, sourceLastWeekPopulation.Items, ids).Sum(i => i.Number);
            }
```

- [ ] **Step 12: Run the full test project**

Run: `dotnet test portal/Test/Test.csproj`
Expected: `已通過!` with 4 more passing tests than Step 3's baseline (2 `SumFromOtherType` + 2 `LastWeekValueFromOtherType`), 0 failed.

- [ ] **Step 13: Commit**

```bash
git add -f source/portal/Test/Services/Aggregation/AggregationEngineTests.cs
git add source/portal/Portal/Services/Aggregation/AggregationEngine.cs
git commit -m "feat: add StatisticsType.SumFromOtherType/LastWeekValueFromOtherType to AggregationEngine"
```

---

### Task 3: `StudentPopulationController.cs` — wire the new lookup signature through production call sites

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes: `AggregationEngine`'s new constructor signature (Task 2).

- [ ] **Step 1: Add an optional `type` parameter to `LookupLastWeekPopulation`**

Find (around line 2066):

```csharp
        private static StudentPopulation LookupLastWeekPopulation(DataContext dataContext, StudentPopulation population) {
            if (population?.SchoolId == null) return null;
            SchoolYear lastSchoolYear = population.Week > 1
                ? dataContext.SchoolYear.Where(e => e.Year == population.Year && e.Week == population.Week - 1).OrderBy(e => e.Id).FirstOrDefault()
                : dataContext.SchoolYear.Where(e => e.Year == population.Year - 1).OrderByDescending(e => e.Week).ThenByDescending(e => e.Id).FirstOrDefault();
            if (lastSchoolYear == null) return null;
            return dataContext.StudentPopulation.Include("Items.Class.Course")
                .FirstOrDefault(p => p.Year == lastSchoolYear.Year && p.Week == lastSchoolYear.Week && p.SchoolId == population.SchoolId && p.Type == population.Type);
        }
```

Replace with:

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

- [ ] **Step 2: Update both `new AggregationEngine(...)` call sites**

There are exactly 2 occurrences of the literal text `p => LookupLastWeekPopulation(dataContext, p));` in this file (in `AttachManualPreviews` around line 2058, and in `SumPHPopulation` around line 2085) — replace both occurrences of that literal text with `(p, type) => LookupLastWeekPopulation(dataContext, p, type));`.

- [ ] **Step 3: Build**

Run (from `source/`): `dotnet build portal/PHStatistics.portal.sln`
Expected: `0 個錯誤`. (This will also surface any other `new AggregationEngine(...)` call site this plan's investigation missed — if the build reports a signature mismatch anywhere else in `Portal/`, fix it the same way: append a `StudentPopulationType` parameter to the lambda and pass the population's own `Type` through unchanged.)

- [ ] **Step 4: Full test suite still green**

Run (from `source/`): `dotnet test portal/Test/Test.csproj`
Expected: same pass count as Task 2 Step 12 (this task changes production wiring only, no new tests, no behavior change for existing same-type lookups — `type ?? population.Type` preserves the old default exactly).

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: pass explicit StudentPopulationType through StudentPopulationController's AggregationEngine wiring"
```

---

### Task 4: Fix the 5 existing `*AggregationComparisonTests.cs` files to compile against the new constructor signature

**Files:**
- Modify: `source/portal/Test/Services/Aggregation/PhAggregationComparisonTests.cs`
- Modify: `source/portal/Test/Services/Aggregation/GeptAggregationComparisonTests.cs`
- Modify: `source/portal/Test/Services/Aggregation/PsAggregationComparisonTests.cs`
- Modify: `source/portal/Test/Services/Aggregation/PsjAggregationComparisonTests.cs`
- Modify: `source/portal/Test/Services/Aggregation/AsAggregationComparisonTests.cs`

**Interfaces:**
- Consumes: `AggregationEngine`'s new constructor signature (Task 2). These 5 files construct `AggregationEngine` directly (not through `StudentPopulationController`), so Task 3 does not cover them — without this task the whole test project fails to compile.

All 5 files share the exact same local function, byte-for-byte:

```csharp
        StudentPopulation LookupLastWeek(StudentPopulation p) {
            if (p?.SchoolId == null) return null;
            var lastSchoolYear = p.Week > 1
                ? context.SchoolYear.Where(e => e.Year == p.Year && e.Week == p.Week - 1).OrderBy(e => e.Id).FirstOrDefault()
                : context.SchoolYear.Where(e => e.Year == p.Year - 1).OrderByDescending(e => e.Week).ThenByDescending(e => e.Id).FirstOrDefault();
            if (lastSchoolYear == null) return null;
            return context.StudentPopulation
                .Include(x => x.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(x => x.Year == lastSchoolYear.Year && x.Week == lastSchoolYear.Week && x.SchoolId == p.SchoolId && x.Type == p.Type);
        }
```

passed to the engine constructor as a bare method-group reference: `LookupLastWeek);` (last argument of the `new AggregationEngine(...)` call). Since it's passed as a method group, C# will re-resolve it against the new 2-parameter delegate type automatically **once the method itself has a matching second parameter** — the constructor call site (`LookupLastWeek);`) does not need to change at all, only the local function's own signature and its final `.Type ==` comparison.

- [ ] **Step 1: Fix `PhAggregationComparisonTests.cs`**

Replace:

```csharp
        StudentPopulation LookupLastWeek(StudentPopulation p) {
            if (p?.SchoolId == null) return null;
            var lastSchoolYear = p.Week > 1
                ? context.SchoolYear.Where(e => e.Year == p.Year && e.Week == p.Week - 1).OrderBy(e => e.Id).FirstOrDefault()
                : context.SchoolYear.Where(e => e.Year == p.Year - 1).OrderByDescending(e => e.Week).ThenByDescending(e => e.Id).FirstOrDefault();
            if (lastSchoolYear == null) return null;
            return context.StudentPopulation
                .Include(x => x.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(x => x.Year == lastSchoolYear.Year && x.Week == lastSchoolYear.Week && x.SchoolId == p.SchoolId && x.Type == p.Type);
        }
```

with:

```csharp
        StudentPopulation LookupLastWeek(StudentPopulation p, StudentPopulationType type) {
            if (p?.SchoolId == null) return null;
            var lastSchoolYear = p.Week > 1
                ? context.SchoolYear.Where(e => e.Year == p.Year && e.Week == p.Week - 1).OrderBy(e => e.Id).FirstOrDefault()
                : context.SchoolYear.Where(e => e.Year == p.Year - 1).OrderByDescending(e => e.Week).ThenByDescending(e => e.Id).FirstOrDefault();
            if (lastSchoolYear == null) return null;
            return context.StudentPopulation
                .Include(x => x.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(x => x.Year == lastSchoolYear.Year && x.Week == lastSchoolYear.Week && x.SchoolId == p.SchoolId && x.Type == type);
        }
```

(behavior is unchanged: production code always calls this with `type == p.Type` for these same-type regression tests, per Task 2/3's `_lookupLastWeekPopulationOfType(population, population.Type)` / `type ?? population.Type` defaults.)

- [ ] **Step 2: Repeat the identical edit in the other 4 files**

Apply the exact same replacement (old block → new block, both shown in Step 1) to:
- `GeptAggregationComparisonTests.cs`
- `PsAggregationComparisonTests.cs`
- `PsjAggregationComparisonTests.cs`
- `AsAggregationComparisonTests.cs`

- [ ] **Step 3: Build**

Run (from `source/`): `dotnet build portal/PHStatistics.portal.sln`
Expected: `0 個錯誤`.

- [ ] **Step 4: Run the full test project**

Run (from `source/`): `dotnet test portal/Test/Test.csproj`
Expected: same pass count as Task 3 Step 4 — these 5 comparison tests are all `[Explicit]` (they require a live DB connection) so they are excluded from a normal `dotnet test` run; this step only confirms the project still **compiles and the non-Explicit suite still passes**, not that the 5 comparison tests themselves pass (they're exercised manually in Task 6).

- [ ] **Step 5: Commit**

```bash
git add -f source/portal/Test/Services/Aggregation/PhAggregationComparisonTests.cs source/portal/Test/Services/Aggregation/GeptAggregationComparisonTests.cs source/portal/Test/Services/Aggregation/PsAggregationComparisonTests.cs source/portal/Test/Services/Aggregation/PsjAggregationComparisonTests.cs source/portal/Test/Services/Aggregation/AsAggregationComparisonTests.cs
git commit -m "fix: update the 5 AggregationEngine comparison tests for the new last-week lookup signature"
```

---

### Task 5: Apply Course 135/139/142 configuration to the database (GATED — do not run unattended)

**⚠️ This task changes data on a database that is effectively production. Do not dispatch this task to an autonomous subagent without stopping first. Whoever executes the apply step (Step 3) must get the user's explicit, real-time go-ahead immediately before running it, and must not proceed on an assumption of prior approval.**

**Files:**
- Create: `source/portal/docs/superpowers/sql/2026-08-18-ps-psj-crosstype-apply.sql`
- Create: `source/portal/docs/superpowers/sql/2026-08-18-ps-psj-crosstype-revert.sql`

**Interfaces:**
- Consumes: `Course.SourceStudentPopulationType` column (Task 1, must already exist in the target database before this task's UPDATEs can run — Task 1 only scaffolded the migration; if the database doesn't have the column yet, apply that migration first via the same gated pattern used elsewhere in this repo, e.g. `docs/superpowers/plans/2026-08-14-director-role.md` Task 7).
- Produces: courses 135/139/142 configured with the new `StatisticsType` values, ready for `AggregationEngine` (Tasks 2-3, already deployed in code) to compute them the next time a PS report is opened/saved.

- [ ] **Step 1: Confirm the `SourceStudentPopulationType` column already exists on the target database — apply Task 1's migration first if not**

Run:
```bash
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P <password from appsettings.json> -Q "SELECT TOP 1 SourceStudentPopulationType FROM Course"
```
Expected: the query runs without an "invalid column name" error.

If it DOES error with "invalid column name", Task 1's migration has not yet been applied to this database. Apply it first, using the same gated procedure as `docs/superpowers/plans/2026-08-14-director-role.md` Task 7 (generate the SQL script, review it, get explicit user go-ahead, apply, verify independently):

```bash
dotnet ef migrations script AddMemberPrimarySchoolId AddCourseSourceStudentPopulationType --project schema/Data/Data.csproj --startup-project portal/Portal/Portal.csproj --idempotent -o migration-add-source-population-type.sql
```
(Replace `AddMemberPrimarySchoolId` with whatever migration is actually immediately before `AddCourseSourceStudentPopulationType` at execution time — confirm against `source/schema/Data/Migrations/` first, since more migrations may have landed since this plan was written.) Open the generated `.sql` file and confirm it contains only a single `ALTER TABLE [Course] ADD [SourceStudentPopulationType] smallint NULL;` plus the `__EFMigrationsHistory` bookkeeping insert — nothing else. Show it to the user, get explicit confirmation, then:
```bash
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P <password> -i migration-add-source-population-type.sql
```
**Before trusting the result**, independently verify with a fresh `SELECT`:
```bash
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P <password> -Q "SELECT TOP 1 SourceStudentPopulationType FROM Course"
```
If this errors with something about `QUOTED_IDENTIFIER`/`CREATE INDEX` failing and rolling back the whole transaction (this repo's sqlcmd session hit exactly this on the director-role migration — see `docs/superpowers/plans/2026-08-14-director-role.md` Task 7's resolution): check whether `__EFMigrationsHistory` now has a row for `AddCourseSourceStudentPopulationType` despite the column not actually existing (the same failure mode can leave that table inconsistent) — if so, `DELETE` that stray history row before retrying, and prepend `SET ANSI_NULLS ON; GO SET QUOTED_IDENTIFIER ON; GO` to the top of the generated script before re-running it. Delete the generated `.sql` file once verified. Then continue to Step 2 below.

- [ ] **Step 2: Write the apply script**

Create `source/portal/docs/superpowers/sql/2026-08-18-ps-psj-crosstype-apply.sql`:

```sql
-- 2026-08-18: PS報表135(PSJ總人數)/139(PSJ上週人數)/142(本週變更(PS+PSJ)) 改接真實PSJ資料
-- 對應設計：source/portal/docs/superpowers/specs/2026-08-17-ps-psj-crosstype-total-design.md
-- 套用前確認過的現況（135: IsSum=0,StatisticsType=4,SourceCourseIds='[157]'；139/142: IsSum=1,StatisticsType=NULL），
-- 見同目錄 2026-08-18-ps-psj-crosstype-revert.sql 的還原值。

UPDATE Course
SET IsSum = 1,
    StatisticsType = 15, -- SumFromOtherType
    SourceCourseIds = '[562,563,564,565,566,567,568,569,570,571,572,573]',
    SourceStudentPopulationType = 1 -- PSJ
WHERE Id = 135;

UPDATE Course
SET StatisticsType = 16, -- LastWeekValueFromOtherType
    SourceCourseIds = '[562,563,564,565,566,567,568,569,570,571,572,573]',
    SourceStudentPopulationType = 1 -- PSJ
WHERE Id = 139;

UPDATE Course
SET StatisticsType = 12, -- DiffBetweenCourses
    SourceCourseIds = '[132,135]',
    NegativeSourceCourseIds = '[138,139]'
WHERE Id = 142;

SELECT Id, Name, IsSum, StatisticsType, SourceCourseIds, NegativeSourceCourseIds, SourceStudentPopulationType
FROM Course WHERE Id IN (135, 139, 142) ORDER BY Id;
```

- [ ] **Step 3: Write the revert script (pre-image values, verified against the live database on 2026-08-18)**

Create `source/portal/docs/superpowers/sql/2026-08-18-ps-psj-crosstype-revert.sql`:

```sql
-- Reverts 2026-08-18-ps-psj-crosstype-apply.sql back to the exact pre-image values
-- (verified via SELECT against production on 2026-08-18, before any change was applied).

UPDATE Course
SET IsSum = 0,
    StatisticsType = 4, -- SumBySourceCourses
    SourceCourseIds = '[157]',
    NegativeSourceCourseIds = NULL,
    SourceStudentPopulationType = NULL
WHERE Id = 135;

UPDATE Course
SET StatisticsType = NULL,
    SourceCourseIds = NULL,
    NegativeSourceCourseIds = NULL,
    SourceStudentPopulationType = NULL
WHERE Id = 139;

UPDATE Course
SET StatisticsType = NULL,
    SourceCourseIds = NULL,
    NegativeSourceCourseIds = NULL,
    SourceStudentPopulationType = NULL
WHERE Id = 142;

SELECT Id, Name, IsSum, StatisticsType, SourceCourseIds, NegativeSourceCourseIds, SourceStudentPopulationType
FROM Course WHERE Id IN (135, 139, 142) ORDER BY Id;
```

- [ ] **Step 4: Get explicit user confirmation, then apply**

State plainly to the user: "This will run 3 `UPDATE Course` statements (Ids 135/139/142) against `NewPAS` on `20.188.19.77,52056` (effectively production) — including flipping course 135 from a manually-editable field to a read-only computed one for every school's PS report. Proceed?" Only after an explicit yes, apply via `sqlcmd`:

```bash
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P <password from appsettings.json> -i source/portal/docs/superpowers/sql/2026-08-18-ps-psj-crosstype-apply.sql
```

If this errors with something like "CREATE INDEX 失敗...QUOTED_IDENTIFIER" (this repo's sqlcmd session has hit this before on DDL — see `docs/superpowers/plans/2026-08-14-director-role.md` Task 7's resolution), it does not apply here since this script is plain `UPDATE`/`SELECT` with no index/constraint DDL — but if any unexpected error occurs, do not retry blindly: check the actual current row state with the Step 5 SELECT before deciding whether a retry is safe (a partial multi-statement failure could leave 135 updated but 139/142 not, or vice versa).

- [ ] **Step 5: Verify with an independent SELECT (do not trust sqlcmd's exit code alone)**

```bash
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P <password> -Q "SELECT Id, Name, IsSum, StatisticsType, SourceCourseIds, NegativeSourceCourseIds, SourceStudentPopulationType FROM Course WHERE Id IN (135,139,142) ORDER BY Id"
```

Expected: 135 shows `IsSum=1, StatisticsType=15, SourceCourseIds='[562,563,564,565,566,567,568,569,570,571,572,573]', SourceStudentPopulationType=1`; 139 shows `StatisticsType=16` with the same `SourceCourseIds`/`SourceStudentPopulationType`; 142 shows `StatisticsType=12, SourceCourseIds='[132,135]', NegativeSourceCourseIds='[138,139]'`.

- [ ] **Step 6: Commit the SQL scripts**

```bash
git add source/portal/docs/superpowers/sql/2026-08-18-ps-psj-crosstype-apply.sql source/portal/docs/superpowers/sql/2026-08-18-ps-psj-crosstype-revert.sql
git commit -m "docs: add apply/revert SQL for PS report PSJ cross-type Course configuration"
```

---

### Task 6: New PS×PSJ cross-type comparison test

**Files:**
- Create: `source/portal/Test/Services/Aggregation/PsPsjCrossTypeComparisonTests.cs`

**Interfaces:**
- Consumes: `AggregationEngine` (Task 2), `StudentPopulationController.LookupLastWeekPopulation` pattern (mirrored inline, matching the other 5 comparison tests — Task 4). Requires Task 5 already applied to the database being tested against (otherwise every PS population's course 135/139/142 will still show the OLD `StatisticsType`, and the comparison will trivially "pass" by comparing old-formula-computed values against themselves, which proves nothing).

- [ ] **Step 1: Write the test**

Create `source/portal/Test/Services/Aggregation/PsPsjCrossTypeComparisonTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Aggregation;
using System.Framework.Data;

namespace PHStatistics.Portal.Test.Services.Aggregation;

[TestFixture]
[Explicit("Requires a live connection to the dev database with Task 5's Course 135/139/142 configuration already applied; run manually to verify the PS-PSJ cross-type formulas (see plan Task 6)")]
public class PsPsjCrossTypeComparisonTests {
    // 135/139/142 只套用到「之後新填/重新開啟儲存」的週次（spec 明確決定不回填），
    // 所以拿現有已存資料比對時，多數舊週次的 stored 值仍是舊公式或人工輸入的結果，預期會有落差——
    // 這份測試的目的不是「0落差」，而是驗證「PS報表當週已存在對應PSJ報表」時，引擎算出的值
    // 確實等於 PSJ 12個年級課程562-573的加總/上週加總，公式本身正確。
    [Test]
    public void Engine_ComputesPsjCrossTypeFields_MatchingRealPsjData_WhenBothReportsExistForSameWeek() {
        using var context = new DataContext();

        var psPopulations = context.StudentPopulation
            .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
            .Where(p => p.Type == StudentPopulationType.PS && p.DataMode == DataMode.Normal)
            .ToList();

        StudentPopulation LookupSameWeek(int year, int week, int schoolId, StudentPopulationType type) {
            return context.StudentPopulation
                .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type);
        }

        StudentPopulation LookupLastWeek(StudentPopulation p, StudentPopulationType type) {
            if (p?.SchoolId == null) return null;
            var lastSchoolYear = p.Week > 1
                ? context.SchoolYear.Where(e => e.Year == p.Year && e.Week == p.Week - 1).OrderBy(e => e.Id).FirstOrDefault()
                : context.SchoolYear.Where(e => e.Year == p.Year - 1).OrderByDescending(e => e.Week).ThenByDescending(e => e.Id).FirstOrDefault();
            if (lastSchoolYear == null) return null;
            return context.StudentPopulation
                .Include(x => x.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(x => x.Year == lastSchoolYear.Year && x.Week == lastSchoolYear.Week && x.SchoolId == p.SchoolId && x.Type == type);
        }

        var engine = new AggregationEngine(LookupSameWeek, LookupLastWeek);

        int psjCoursesChecked = 0;
        int noPsjDataSkipped = 0;
        var mismatches = new List<string>();

        foreach (var population in psPopulations) {
            var psjSameWeek = LookupSameWeek(population.Year, population.Week, population.SchoolId ?? 0, StudentPopulationType.PSJ);
            if (psjSameWeek == null) {
                noPsjDataSkipped++;
                continue; // 該校當週沒有PSJ報表，135/139理論上會算成0，不是這份測試要驗證的情境
            }

            var course135Item = population.Items.FirstOrDefault(i => i.Class?.CourseId == 135);
            var course139Item = population.Items.FirstOrDefault(i => i.Class?.CourseId == 139);
            if (course135Item == null || course139Item == null) continue;

            int expected135 = psjSameWeek.Items
                .Where(i => i.Class?.CourseId != null && new[] { 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 572, 573 }.Contains(i.Class.CourseId.Value))
                .Sum(i => i.Number);

            int engine135 = engine.Preview(course135Item, population) ?? -1;
            if (engine135 != expected135) {
                mismatches.Add($"Population {population.Id} (School {population.SchoolId}, {population.Year}/{population.Week}): " +
                    $"course 135 expected={expected135} (直接加總PSJ課程562-573) engine={engine135}");
            }
            psjCoursesChecked++;
        }

        TestContext.WriteLine($"Checked {psjCoursesChecked} PS populations with a matching same-week PSJ report.");
        TestContext.WriteLine($"Skipped {noPsjDataSkipped} PS populations with no matching PSJ report (expected — no backfill).");
        TestContext.WriteLine($"{mismatches.Count} mismatches:");
        foreach (var m in mismatches) TestContext.WriteLine(m);

        Assert.That(mismatches, Is.Empty, () => string.Join("\n", mismatches));
    }
}
```

Note: this uses `engine.Preview(...)` (read-only, does not mutate `item.Number`) rather than `engine.Calculate(...)`, unlike the other 5 comparison tests — those compare "engine result vs. currently-stored value" for courses whose formula is unchanged; this one compares "engine result vs. an independently-recomputed expected value" (summing the real PSJ courses directly in the test), because course 135's *stored* value for most existing weeks was produced by the *old* formula/manual input (spec: no backfill), so comparing against the stored value would fail for the wrong reason.

- [ ] **Step 2: Run it manually against a database where Task 5 has been applied**

Run (from `source/`): `dotnet test portal/Test/Test.csproj --filter "FullyQualifiedName~PsPsjCrossTypeComparisonTests"`
Expected: `已通過!`, with the console output showing how many PS populations had a matching same-week PSJ report checked, and 0 mismatches. If `noPsjDataSkipped` equals the total population count (0 checked), that means no PS report in the target database currently has a same-week PSJ report yet — this is expected until a school fills in both reports for a shared week after Task 5 ships (spec's manual test scenario 1-2 in the design doc); it is not a failure, just nothing to compare yet.

- [ ] **Step 3: Commit**

```bash
git add -f source/portal/Test/Services/Aggregation/PsPsjCrossTypeComparisonTests.cs
git commit -m "test: add PS-PSJ cross-type comparison test for courses 135/139"
```

- [ ] **Step 4: Manual browser verification (no automated UI test exists in this repo, no browser tool available in this environment)**

This repo's established convention (this environment has no browser tooling) is to defer manual verification to the user. Once Task 5 has shipped, ask the user to walk through the spec's 4 manual scenarios (design doc "測試方式" section) at their convenience:
1. Open a new week's PSJ report, fill in all 12 grades' 數學/理化 headcounts, save.
2. Open the same school/week's PS report — confirm 135/139/142 auto-populate with the correct values, and that 135 is now a read-only (computed) field instead of an input box.
3. Edit any grade's number on the PSJ report and re-save; reopen the PS report and confirm 135/139/142 updated to match.
4. Open a PS report for a school/week where no PSJ report exists yet — confirm 135/139 show `0` with no exception thrown.

Do not mark this plan complete without flagging these 4 scenarios to the user as outstanding manual verification.
