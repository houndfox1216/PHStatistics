# PS Aggregation Engine Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate PS (百世／南區, `StudentPopulationType.PS`)'s aggregation logic in `StudentPopulationController.SumPHPopulation` onto the shared `AggregationEngine`, extending the engine with the 3 new capabilities PS requires (an `Average` statistic, a `SourceCourseIds` source that can reference other summary courses, and a deterministic calculation order), then configuring PS's 16 summary courses' rule fields via SQL, verifying the engine reproduces real stored data, then cutting the live code path over.

**Architecture:** `AggregationEngine` (`source/portal/Portal/Services/Aggregation/AggregationEngine.cs`) already exists and is live for GEPT and PH. Unlike those two migrations, this one requires real (small, backward-compatible) changes to the engine itself: implementing the already-declared-but-unused `StatisticsType.Average`, letting `SourceCourseIds`-based lookups include other summary (`IsSum=true`) courses (needed because PS course 144 "總人數" must sum two OTHER summary courses, 132 and 135), and making `StudentPopulationController.cs`'s `classGroup` loop iterate in `Course.Ordinal` order (needed so course 132 is recalculated before course 144 reads it in the same pass). After those engine changes land, this plan configures PS's 16 `IsSum=true` courses' rule fields (9 engine-computed, 7 remain `ManualInput`) and swaps the `SumPHPopulation` PS branch to call the engine, exactly like the GEPT and PH cutovers.

**Tech Stack:** ASP.NET Core 8 MVC, Entity Framework Core 8, NUnit 4, SQL Server (`sqlcmd`).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-15-ps-aggregation-migration-design.md` — this plan implements it exactly; do not deviate from its 16-course rule table or its 3 engine changes without stopping to re-check with the user.
- No database schema changes — every column this plan sets already exists and is already used by GEPT's and PH's configuration.
- This plan covers ONLY the PS type. PSJ/AfterSchool follow the same pattern in separate future plans — do not touch their branches in `SumPHPopulation`.
- Per the approved spec: **comment out the old PS code, do not delete it** (same conservative choice as PH; the post-loop `else if (Type == PS) { }` block is already empty and needs no change).
- Dev DB connection used for verification steps: `sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -d NewPAS07 -U sa -P 'cloudfun@12' -C` (matches `source/portal/Portal/appsettings.json`)
- Follow existing code style: no XML doc comments unless the WHY is non-obvious, `Include("string.path")` style EF includes (matches the rest of `StudentPopulationController.cs`), 4-space indentation matching surrounding files.
- `StudentPopulationType.PS` = `Type = 3` in the DB. PS has no `ClassType` split in any of its rules — every rule in this plan uses `GroupByClassType = false` (the default) and no `ApplicableClassType`.
- The engine changes in Task 1 must not change behavior for any already-configured GEPT or PH rule. GEPT and PH currently only ever use `SourceDepartmentIds` (never `SourceCourseIds`), so the `SourceCourseIds`-includes-`IsSum` change is additive and zero-risk for them — but Task 1's tests must still prove the `SourceDepartmentIds` path keeps excluding summary items, since that's the behavior GEPT/PH depend on.

---

### Task 1: Extend `AggregationEngine` — `Average`, `SourceCourseIds` including summary courses

**Files:**
- Modify: `source/portal/Portal/Services/Aggregation/AggregationEngine.cs`
- Modify: `source/portal/Test/Services/Aggregation/AggregationEngineTests.cs`

**Interfaces:**
- Consumes: nothing new — pure extension of the existing `AggregationEngine.Calculate(StudentPopulationItem, StudentPopulation)` method.
- Produces: `AggregationEngine` now supports `StatisticsType.Average`, and any course configured with `SourceCourseIds` can reference other `IsSum=true` courses (previously always excluded). Task 3 and Task 4 depend on both of these.

This task is pure engine logic + unit tests — no database, no controller changes.

- [ ] **Step 1: Write the new/changed unit tests**

Open `source/portal/Test/Services/Aggregation/AggregationEngineTests.cs`. First, **fix the now-incorrect existing test** that uses `StatisticsType.Average` as its "this is unsupported" example — once this task implements `Average`, that test would start failing for the wrong reason (it would stop throwing). Replace its use of `Average` with `SumAll`, which remains genuinely unimplemented in the engine's switch statement:

Find:
```csharp
    [Test]
    public void Calculate_UnsupportedStatisticsType_ThrowsNotSupportedException() {
        var sumCourse = MakeCourse(199, isSum: true, statisticsType: StatisticsType.Average);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> { sumItem },
        };

        var engine = MakeEngine();

        Assert.Throws<NotSupportedException>(() => engine.Calculate(sumItem, population));
    }
```

Replace with:
```csharp
    [Test]
    public void Calculate_UnsupportedStatisticsType_ThrowsNotSupportedException() {
        var sumCourse = MakeCourse(199, isSum: true, statisticsType: StatisticsType.SumAll);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> { sumItem },
        };

        var engine = MakeEngine();

        Assert.Throws<NotSupportedException>(() => engine.Calculate(sumItem, population));
    }
```

Then add these 4 new tests at the end of the class, just before the final closing `}`:

```csharp
    [Test]
    public void Calculate_Average_ReturnsSumDividedByCountOfItemsWithNumberGreaterThanZero() {
        var course1 = MakeCourse(101, departmentId: 1);
        var course2 = MakeCourse(102, departmentId: 1);
        var course3 = MakeCourse(103, departmentId: 1); // Number == 0, must not count toward the denominator
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.Average);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PS,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 10),
                MakeItem(course2, ClassType.General, 4),
                MakeItem(course3, ClassType.General, 0),
                sumItem,
            },
        };

        MakeEngine().Calculate(sumItem, population);

        // (10 + 4) / 2 = 7 — course3 contributes 0 to the sum and is excluded from the count
        Assert.That(sumItem.Number, Is.EqualTo(7));
    }

    [Test]
    public void Calculate_Average_ReturnsZeroWhenNoItemsHaveNumberGreaterThanZero() {
        var course1 = MakeCourse(101, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.Average);
        var sumItem = MakeItem(sumCourse, ClassType.General, 5);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PS,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 0),
                sumItem,
            },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(0));
    }

    [Test]
    public void Calculate_SourceCourseIds_CanIncludeOtherSummaryCourses() {
        var rawCourse = MakeCourse(101, departmentId: 1);
        var otherSumCourse = MakeCourse(132, departmentId: 25, isSum: true, statisticsType: StatisticsType.SumByDepartment);
        var otherSumItem = MakeItem(otherSumCourse, ClassType.General, 20); // pre-computed value from an earlier item in the same pass
        var targetSumCourse = MakeCourse(144, departmentId: 13, isSum: true,
            statisticsType: StatisticsType.SumBySourceCourses, sourceCourseIds: "[101,132]");
        var targetSumItem = MakeItem(targetSumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PS,
            Items = new List<StudentPopulationItem> {
                MakeItem(rawCourse, ClassType.General, 5),
                otherSumItem,
                targetSumItem,
            },
        };

        MakeEngine().Calculate(targetSumItem, population);

        // 5 (raw course 101) + 20 (summary course 132) = 25 — proves SourceCourseIds no longer
        // excludes IsSum=true items, unlike SourceDepartmentIds/own-department lookups.
        Assert.That(targetSumItem.Number, Is.EqualTo(25));
    }

    [Test]
    public void Calculate_SumByDepartment_StillExcludesOtherSumItemsInSameDepartment() {
        var course1 = MakeCourse(101, departmentId: 1);
        var otherSumCourse = MakeCourse(198, departmentId: 1, isSum: true, statisticsType: StatisticsType.ManualInput);
        var otherSumItem = MakeItem(otherSumCourse, ClassType.General, 999); // must NOT be included
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.SumByDepartment);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 5),
                otherSumItem,
                sumItem,
            },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(5));
    }
```

- [ ] **Step 2: Run the tests to verify the new ones fail correctly**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`

Expected:
- `Calculate_UnsupportedStatisticsType_ThrowsNotSupportedException` — PASS (SumAll is genuinely unimplemented, same as Average was)
- `Calculate_Average_ReturnsSumDividedByCountOfItemsWithNumberGreaterThanZero` — FAIL with `NotSupportedException` (Average not implemented yet)
- `Calculate_Average_ReturnsZeroWhenNoItemsHaveNumberGreaterThanZero` — FAIL with `NotSupportedException`
- `Calculate_SourceCourseIds_CanIncludeOtherSummaryCourses` — FAIL, `targetSumItem.Number` will be `5` not `25` (the summary item is currently excluded)
- `Calculate_SumByDepartment_StillExcludesOtherSumItemsInSameDepartment` — PASS already (this is a regression-lock test for behavior that must NOT change)
- All other pre-existing tests — PASS (unchanged)

- [ ] **Step 3: Implement the engine changes**

In `source/portal/Portal/Services/Aggregation/AggregationEngine.cs`, add the `Average` case to the `switch` in `Calculate`. Insert it right after the `DiffWithLastYear` case and before `default`:

```csharp
            case StatisticsType.Average: {
                var src = GetSourceItems(course, item, population.Items).ToList();
                int count = src.Count(i => i.Number > 0);
                item.Number = count > 0 ? src.Sum(i => i.Number) / count : 0;
                break;
            }
```

Then replace the entire `GetSourceItems` method with:

```csharp
    // 篩選優先序：SourceDepartmentIds > SourceCourseIds > 課程自身 DepartmentId；
    // 班別篩選：ApplicableClassType（固定班別）優先於 GroupByClassType（用 contextItem 自己的班別）
    // SourceCourseIds 刻意不排除 IsSum 項目：部分課程（如 PS 的「總人數」）需要直接加總
    // 其他「加總課程」目前算出的值，SourceDepartmentIds／課程自身 DepartmentId 這兩條路徑
    // 則維持排除加總課程，這是 GEPT/PH 既有規則正確運作所依賴的行為，不能改。
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

(The only semantic change from the original: the `items.Where(i => i.Class?.Course?.IsSum != true)` filter that used to apply unconditionally up front now only applies inside the `SourceDepartmentIds` branch and the fallback `DepartmentId` branch — the `SourceCourseIds` branch has no such filter.)

- [ ] **Step 4: Run the tests to verify they all pass**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`
Expected: all tests PASS, output pristine (no warnings).

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Services/Aggregation/AggregationEngine.cs source/portal/Test/Services/Aggregation/AggregationEngineTests.cs
git commit -m "feat: add Average statistic and let SourceCourseIds reference summary courses"
```

---

### Task 2: Configure PS's 16 summary courses

**Files:**
- Create: `source/portal/docs/superpowers/sql/2026-07-15-ps-aggregation-rules-apply.sql`
- Create: `source/portal/docs/superpowers/sql/2026-07-15-ps-aggregation-rules-revert.sql`

**Interfaces:**
- Consumes: nothing (pure SQL data configuration).
- Produces: correctly configured `Course.StatisticsType`/`SourceDepartmentIds`/`SourceCourseIds` for PS's 16 `IsSum=true` courses — this is what Task 3's comparison test and Task 4's engine cutover depend on.

The rule mapping below is copied from the approved design spec (`docs/superpowers/specs/2026-07-15-ps-aggregation-migration-design.md`), grounded in real dev-DB department/course IDs (`NewPAS07`, `CourseDepartment`/`Course` where `Type=3`):

| Course Id | Name | Department | StatisticsType | SourceDepartmentIds / SourceCourseIds |
|---|---|---|---|---|
| 120 | 國小班人數合計 | 國小班 (22) | `SumByDepartment` (1) | — |
| 127 | 國中班人數合計 | 國中班 (23) | `SumByDepartment` (1) | — |
| 131 | 高中班人數合計 | 高中班 (24) | `SumByDepartment` (1) | — |
| 132 | PS數學總人數 | PS統計 (25) | `SumBySourceDepartments` (3) | `[22,23,24]` |
| 133 | PS數學開班數 | PS統計 (25) | `CountClasses` (30) | `[22,23,24]` |
| 134 | PS數學班平均人數 | PS統計 (25) | `Average` (60) | `[22,23,24]` |
| 138 | PS上週人數 | 分析 (26) | `LastWeekValue` (40) | `[22,23,24]` |
| 142 | 本週變更(PS+百倍速) | 分析 (26) | `DiffWithLastWeek` (10) | `[132,135]` (SourceCourseIds) |
| 144 | 總人數(PS+百倍速) | 統計 (13) | `SumBySourceCourses` (4) | `[132,135]` (SourceCourseIds) |
| 135 | 百倍速總人數 | 分析 (26) | `ManualInput` (50) | — |
| 136 | 百世數學去年同期 | 分析 (26) | `ManualInput` (50) | — |
| 137 | 百倍速去年同期 | 分析 (26) | `ManualInput` (50) | — |
| 139 | 百倍速上週人數 | 分析 (26) | `ManualInput` (50) | — |
| 140 | 本週PS新生人數 | 分析 (26) | `ManualInput` (50) | — |
| 141 | 本週PS流失人數 | 分析 (26) | `ManualInput` (50) | — |
| 143 | 本週總詢問人數 | 統計 (13) | `ManualInput` (50) | — |

`GroupByClassType` is left at its default (`false`/0) for every row — PS has no `ClassType` split (see spec's data-investigation finding 5).

- [ ] **Step 1: Write the apply script**

Create `source/portal/docs/superpowers/sql/2026-07-15-ps-aggregation-rules-apply.sql`:

```sql
-- PS (StudentPopulationType.PS, Type=3) aggregation rule configuration.
-- See docs/superpowers/plans/2026-07-15-ps-aggregation-migration.md, Task 2, and
-- docs/superpowers/specs/2026-07-15-ps-aggregation-migration-design.md for the full mapping table and rationale.
--
-- IMPORTANT: if this script is ever reverted (see the paired revert script), Task 4's code
-- cutover (StudentPopulationController.cs, SumPHPopulation PS branch calling AggregationEngine)
-- MUST be reverted at the same time. Reverting only the SQL leaves StatisticsType NULL while the
-- live code still calls the engine, which will leave every PS summary course frozen at whatever
-- value it last held (the engine no-ops on StatisticsType=null) — worse than the original bug.

-- 同班系加總 (own department, no ClassType split)
UPDATE Course SET StatisticsType = 1 WHERE Id IN (120, 127, 131);

-- PS數學總人數：加總 22/23/24 三個原始年級班系
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[22,23,24]' WHERE Id = 132;

-- PS數學開班數：班數統計 (Number > 0 count)，不分班別
UPDATE Course SET StatisticsType = 30, SourceDepartmentIds = '[22,23,24]' WHERE Id = 133;

-- PS數學班平均人數：平均值 = 總人數 / 開班數
UPDATE Course SET StatisticsType = 60, SourceDepartmentIds = '[22,23,24]' WHERE Id = 134;

-- PS上週人數：LastWeekValue，跟132同源 (修正原本永遠只能靠人工填寫維持正確的欄位)
UPDATE Course SET StatisticsType = 40, SourceDepartmentIds = '[22,23,24]' WHERE Id = 138;

-- 本週變更(PS+百倍速) / 總人數(PS+百倍速)：來源直接指向另外兩個加總課程 132(可計算) + 135(人工填寫)
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[132,135]' WHERE Id = 142;
UPDATE Course SET StatisticsType = 4, SourceCourseIds = '[132,135]' WHERE Id = 144;

-- 手動輸入 (百倍速系列 + 新生/流失 + 本週總詢問人數 — 分校自填，系統不計算)
UPDATE Course SET StatisticsType = 50 WHERE Id IN (135, 136, 137, 139, 140, 141, 143);
```

Create `source/portal/docs/superpowers/sql/2026-07-15-ps-aggregation-rules-revert.sql`:

```sql
-- Revert PS aggregation rule configuration.
-- WARNING: only run this together with reverting the Task 4 code change
-- (SumPHPopulation's PS branch calling AggregationEngine) — see the apply
-- script's header comment for why running one without the other is unsafe.
UPDATE Course
SET StatisticsType = NULL, SourceDepartmentIds = NULL, SourceCourseIds = NULL
WHERE Id IN (120, 127, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144);
```

- [ ] **Step 2: Run the apply script against the dev DB**

Run:
```bash
sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -d NewPAS07 -U sa -P 'cloudfun@12' -C -i "source/portal/docs/superpowers/sql/2026-07-15-ps-aggregation-rules-apply.sql"
```
Expected: a series of `(N 個資料列受到影響)` messages, no errors.

**Known risk (hit twice already during the GEPT/PH migrations, once on dev and once on production):** `sqlcmd -i` on this machine has intermittently failed to execute every statement in a multi-statement file without `GO` separators, silently reporting fewer row-count messages than there are `UPDATE` statements, with no error. Do NOT trust the message count — always do Step 3's verification query regardless of how many messages printed. If Step 3 shows any of the 16 rows still `NULL`, re-run the specific missing `UPDATE` statement(s) individually via `sqlcmd -Q "<single statement>"` (proven reliable both times this happened before), then re-verify.

- [ ] **Step 3: Verify the configuration landed correctly**

Run:
```bash
sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -d NewPAS07 -U sa -P 'cloudfun@12' -C -Q "SELECT Id, Name, StatisticsType, SourceDepartmentIds, SourceCourseIds FROM Course WHERE Id IN (120,127,131,132,133,134,135,136,137,138,139,140,141,142,143,144) ORDER BY Id" -W -s"|"
```
Expected: 16 rows, `StatisticsType`/`SourceDepartmentIds`/`SourceCourseIds` matching the mapping table above exactly (see the Known Risk note above — verify every row, don't just count output messages).

- [ ] **Step 4: Commit the scripts**

```bash
git add source/portal/docs/superpowers/sql/2026-07-15-ps-aggregation-rules-apply.sql source/portal/docs/superpowers/sql/2026-07-15-ps-aggregation-rules-revert.sql
git commit -m "data: configure PS's 16 summary courses with AggregationEngine rules"
```

---

### Task 3: Verify the engine against real PS data

**Files:**
- Create: `source/portal/Test/Services/Aggregation/PsAggregationComparisonTests.cs`

**Interfaces:**
- Consumes: `AggregationEngine` (extended by Task 1), PS course configuration (Task 2), `DataContext`.
- Produces: confidence that the engine reproduces every currently-stored PS summary number before Task 4 flips the live code path over.

This follows the exact pattern of `source/portal/Test/Services/Aggregation/PhAggregationComparisonTests.cs`, with two differences: (1) unlike PH, the design spec's data investigation found no expected/intentional value changes for PS — the comparison test's `ExpectedChangeCourseIds` set starts **empty**, meaning every engine-computed course is expected to match stored data exactly; (2) the loop over `IsSum` items must process them in `Course.Ordinal` order, because course 144's calculation (`SourceCourseIds=[132,135]`) depends on course 132 already having its freshly-recalculated value from earlier in the same pass — this mirrors the `classGroup` ordering fix Task 4 makes to the live controller path, so the test genuinely simulates production behavior rather than getting lucky on already-consistent stored data.

- [ ] **Step 1: Write the comparison test**

Create `source/portal/Test/Services/Aggregation/PsAggregationComparisonTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Aggregation;

namespace PHStatistics.Portal.Test.Services.Aggregation;

[TestFixture]
[Explicit("Requires a live connection to the dev database; run manually to verify the PS AggregationEngine migration before cutover (see plan Task 3)")]
public class PsAggregationComparisonTests {
    // 資料調查（見 docs/superpowers/specs/2026-07-15-ps-aggregation-migration-design.md）沒有發現任何
    // PS 課程需要刻意接受行為變更——9 個引擎計算課程的既有真實資料本身就已經符合新公式，理論上應該 0 落差。
    private static readonly HashSet<int> ExpectedChangeCourseIds = new();

    [Test]
    public void Engine_ReproducesStoredNumbers_ForAllRealPsPopulations() {
        using var context = new DataContext();

        var populations = context.StudentPopulation
            .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
            .Where(p => p.Type == StudentPopulationType.PS && p.DataMode == DataMode.Normal)
            .ToList();

        var engine = new AggregationEngine((year, week, schoolId, type) =>
            context.StudentPopulation
                .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type));

        var unexpectedMismatches = new List<string>();
        var expectedChanges = new List<string>();
        foreach (var population in populations) {
            // Course.Ordinal 排序：144 的 SourceCourseIds=[132,135] 需要 132 已經在同一輪算過，
            // 跟 Task 4 替 classGroup 迴圈補上的 OrderBy(Ordinal) 是同一個順序保證。
            var sumItems = population.Items.Where(i => i.IsSum).OrderBy(i => i.Class.Course.Ordinal).ToList();
            foreach (var item in sumItems) {
                int oldNumber = item.Number;
                engine.Calculate(item, population);
                if (item.Number != oldNumber) {
                    string line =
                        $"Population {population.Id} (School {population.SchoolId}, {population.Year}/{population.Week}): " +
                        $"course {item.Class.Course.Id} \"{item.Class.Course.Name}\" stored={oldNumber} engine={item.Number}";
                    if (ExpectedChangeCourseIds.Contains(item.Class.Course.Id)) {
                        expectedChanges.Add(line);
                    }
                    else {
                        unexpectedMismatches.Add(line);
                    }
                }
                item.Number = oldNumber; // 唯讀比對，還原避免誤動資料
            }
        }

        TestContext.WriteLine($"Checked {populations.Count} PS populations.");
        TestContext.WriteLine($"{expectedChanges.Count} expected changes:");
        foreach (var m in expectedChanges) TestContext.WriteLine(m);
        TestContext.WriteLine($"{unexpectedMismatches.Count} UNEXPECTED mismatches:");
        foreach (var m in unexpectedMismatches) TestContext.WriteLine(m);

        Assert.That(unexpectedMismatches, Is.Empty, () => string.Join("\n", unexpectedMismatches));
    }
}
```

- [ ] **Step 2: Run it against the dev DB**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~PsAggregationComparisonTests"`

If `new DataContext()` throws a configuration-resolution error, replace the `using var context = new DataContext();` line with:

```csharp
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer("Server=CLOUDFUN-MSI-LE\\SQLEXPRESS;Database=NewPAS07;User=sa;Pwd=cloudfun@12;Encrypt=false;MultipleActiveResultSets=true")
            .Options;
        using var context = new DataContext(options);
```

- [ ] **Step 3: Interpret the result**

- Test passes (0 unexpected mismatches) → proceed to Task 4.
- Test fails → read each mismatch line. Per the design spec's investigation, no mismatches are expected at all, so any failure here needs real investigation before proceeding:
  - If the mismatch is on course 144 or 142: check whether it's a `Course.Ordinal` ordering problem (confirm `SumPHPopulation`'s `classGroup` — not yet touched by this task — isn't relevant here since this test does its own ordering; instead check whether the stored `Number` for course 132 or 135 in that specific population is itself something unusual) or a genuine cross-type data-pollution issue like the one found during the PH migration (course 67 — stray `Class`/`Course` rows from a different `StudentPopulationType` incorrectly attached to a PS population's `Items`). If you find such stray data, do NOT silently add the course ID to `ExpectedChangeCourseIds` — report it in your task report so the controller can decide with the user, exactly as happened for PH's course 67/35/62 findings.
  - If the mismatch is on any other course: check the SQL configuration from Task 2 actually matches the design spec's table for that course.
- If the test cannot be run at all in this environment (no reachable dev DB), do NOT proceed to Task 4. Escalate — Task 4 depends on this test passing.

- [ ] **Step 4: Commit**

```bash
git add source/portal/Test/Services/Aggregation/PsAggregationComparisonTests.cs
git commit -m "test: add explicit PS AggregationEngine comparison test against real dev data"
```

---

### Task 4: Cut PS over to the AggregationEngine (old code commented out, not deleted)

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes: `AggregationEngine` (extended by Task 1), verified PS configuration (Task 2, confirmed by Task 3).
- Produces: `SumPHPopulation`'s PS branch now delegates to the shared engine for all 16 summary courses; PSJ/AfterSchool branches are untouched (out of scope — future plans). The `classGroup` loop now iterates in `Course.Ordinal` order, which is required for PS's course 144/142 to see course 132's freshly-computed value, and is a no-op for GEPT/PH's already-migrated rules (neither depends on iteration order).

- [ ] **Step 1: Add `Course.Ordinal` ordering to the `classGroup` loop**

In `source/portal/Portal/Controllers/StudentPopulationController.cs`, find (currently line 1404):

```csharp
            var classGroup = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.IsSum).ToList();
```

Replace with:

```csharp
            var classGroup = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.IsSum).OrderBy(e => e.Class.Course.Ordinal).ToList();
```

- [ ] **Step 2: Wrap the PS branch in `#if false` and add the engine call**

Find this block (currently lines 1477-1495):

```csharp
                    else if (studentPopulationData.Type == StudentPopulationType.PS) {
                        int psDeptId = group.Class.Course.Department.Id;
                        int psCourseId = group.Class.Course.Id;
                        if (psDeptId == 22 || psDeptId == 23 || psDeptId == 24) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == psDeptId && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (psCourseId == 132 || psCourseId == 144) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && !e.Class.Course.IsSum && (e.Class.Course.Department.Id == 22 || e.Class.Course.Department.Id == 23 || e.Class.Course.Department.Id == 24)).Sum(e => e.Number);
                        }
                        else if (psCourseId == 133) {
                            group.Number = studentPopulationData.Items.Count(e => e.Class.Course.Department != null && !e.Class.Course.IsSum && (e.Class.Course.Department.Id == 22 || e.Class.Course.Department.Id == 23 || e.Class.Course.Department.Id == 24) && e.Number > 0);
                        }
                        else if (psCourseId == 134) {
                            int total = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && !e.Class.Course.IsSum && (e.Class.Course.Department.Id == 22 || e.Class.Course.Department.Id == 23 || e.Class.Course.Department.Id == 24)).Sum(e => e.Number);
                            int count = studentPopulationData.Items.Count(e => e.Class.Course.Department != null && !e.Class.Course.IsSum && (e.Class.Course.Department.Id == 22 || e.Class.Course.Department.Id == 23 || e.Class.Course.Department.Id == 24) && e.Number > 0);
                            group.Number = count > 0 ? total / count : 0;
                        }
                        // IDs 135-143: Manual items, skip auto-calculation
                    }
```

Replace it with (old body preserved verbatim inside `#if false`, new engine call added after `#endif`):

```csharp
                    else if (studentPopulationData.Type == StudentPopulationType.PS) {
#if false // 舊 PS 加總邏輯，2026-07-15 遷移到 AggregationEngine 時停用保留（不刪除），
          // 見 docs/superpowers/plans/2026-07-15-ps-aggregation-migration.md Task 4
                        int psDeptId = group.Class.Course.Department.Id;
                        int psCourseId = group.Class.Course.Id;
                        if (psDeptId == 22 || psDeptId == 23 || psDeptId == 24) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == psDeptId && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (psCourseId == 132 || psCourseId == 144) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && !e.Class.Course.IsSum && (e.Class.Course.Department.Id == 22 || e.Class.Course.Department.Id == 23 || e.Class.Course.Department.Id == 24)).Sum(e => e.Number);
                        }
                        else if (psCourseId == 133) {
                            group.Number = studentPopulationData.Items.Count(e => e.Class.Course.Department != null && !e.Class.Course.IsSum && (e.Class.Course.Department.Id == 22 || e.Class.Course.Department.Id == 23 || e.Class.Course.Department.Id == 24) && e.Number > 0);
                        }
                        else if (psCourseId == 134) {
                            int total = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && !e.Class.Course.IsSum && (e.Class.Course.Department.Id == 22 || e.Class.Course.Department.Id == 23 || e.Class.Course.Department.Id == 24)).Sum(e => e.Number);
                            int count = studentPopulationData.Items.Count(e => e.Class.Course.Department != null && !e.Class.Course.IsSum && (e.Class.Course.Department.Id == 22 || e.Class.Course.Department.Id == 23 || e.Class.Course.Department.Id == 24) && e.Number > 0);
                            group.Number = count > 0 ? total / count : 0;
                        }
                        // IDs 135-143: Manual items, skip auto-calculation
#endif
                        aggregationEngine.Calculate(group, studentPopulationData);
                    }
```

(The post-loop `else if (studentPopulationData.Type == StudentPopulationType.PS) { }` block, currently around line 1693, is already empty — per the spec, no change needed there.)

- [ ] **Step 3: Build**

Run: `dotnet build source/portal/Portal/Portal.csproj -c Debug`
Expected: `0 Error(s)`. (If the build fails with an unexpected token error inside the `#if false` block, the pasted old code doesn't exactly match what's currently in the file — re-copy the exact current content of that region instead of this plan's copy before wrapping it.)

- [ ] **Step 4: Re-run the Task 3 comparison test to confirm nothing regressed**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~PsAggregationComparisonTests"` and confirm it still shows 0 unexpected mismatches. Also re-run Task 1's engine unit tests as a full regression check since this task's `classGroup` ordering change is controller-side, not engine-side: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`.

- [ ] **Step 5: Manual browser verification**

This changes a live save-path for PS populations. Note this alongside the other pending manual-verification items already tracked in project memory (this project's established pattern when no browser tool is available in the agentic environment):
- Open a PS population for a real school, edit a regular (non-summary) grade class's number (e.g. one of the 一資/一特/二資 classes), save
- Confirm 國小班/國中班/高中班人數合計、PS數學總人數、PS數學開班數、PS數學班平均人數 all recompute to the expected values
- Confirm PS上週人數 shows the correct prior week's PS數學總人數 (not a stale/manually-typed value)
- Confirm 總人數(PS+百倍速) equals PS數學總人數 + 百倍速總人數 (i.e., changing the raw grade classes updates 144 correctly, and it still reflects whatever is currently in 百倍速總人數)
- Confirm 本週變更(PS+百倍速) reflects the difference between this week's and last week's 總人數(PS+百倍速)
- Confirm 百倍速總人數、百世/百倍速去年同期、百倍速上週人數、本週PS新生/流失人數、本週總詢問人數 are NOT reset when other fields are saved (same class of bug already fixed once for PH/GEPT earlier — verify it holds for PS too)

- [ ] **Step 6: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "refactor: migrate PS aggregation to the shared AggregationEngine (old code disabled, not deleted)"
```

---

## Self-Review Notes

**Spec coverage:** Task 1 implements both engine extensions the spec requires (`Average`, `SourceCourseIds` including summary courses), with unit tests proving both the new capability and that GEPT/PH's existing `SourceDepartmentIds`/own-department behavior is unchanged. Task 2 configures all 16 courses from the design spec's rule table, including the two courses that use `SourceCourseIds` instead of `SourceDepartmentIds` (142, 144). Task 3's comparison test starts with an empty `ExpectedChangeCourseIds` set, matching the spec's finding that no intentional behavior changes are expected for PS (unlike PH). Task 4 implements the "comment out, don't delete" decision and adds the `Course.Ordinal` ordering fix the spec calls out as required for course 144/142's intra-pass dependency on course 132. The spec's "Out of scope" items (PSJ/AfterSchool, prior GEPT/PH review findings, dead-code deletion, `CourseDepartment.Id` cross-type cleanup) are correctly not addressed here.

**Type consistency:** `AggregationEngine`'s constructor and `Calculate(item, population)` signature are used identically to the existing GEPT/PH wiring — Task 4 reuses the same `aggregationEngine` instance already instantiated once per `SumPHPopulation` call, it does not re-instantiate it. `StatisticsType.Average = 60` and `StatisticsType.SumBySourceCourses = 4` are both pre-existing enum values (confirmed in `source/schema/Content/StatisticsType.cs`) — this plan does not add new enum members, only new `switch` handling and a `GetSourceItems` behavior change.

**Placeholder scan:** No TBD/TODO. Task 4's `#if false` code block reproduces the current file's exact text (verified against a fresh read of `StudentPopulationController.cs` lines 1477-1495 at plan-writing time) rather than paraphrasing, so Task 4 has no ambiguity about what "the old code" is.
