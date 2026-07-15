# PH Aggregation Engine Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate PH (全國人數表)'s aggregation logic in `StudentPopulationController.SumPHPopulation` onto the shared `AggregationEngine` (already built and live for GEPT), by configuring 24 PH courses' rule fields via SQL, verifying the engine reproduces real stored data (with two intentional, documented exceptions), then cutting the live code path over.

**Architecture:** No new engine code. `AggregationEngine` (`source/portal/Portal/Services/Aggregation/AggregationEngine.cs`) and the Admin UI rule fields (`Areas/Admin/Views/Course/Index.cshtml`) already exist and are live for GEPT — this plan only configures data (`Course.StatisticsType`/`SourceDepartmentIds`/`GroupByClassType` for PH's 24 `IsSum=true` courses) and swaps the `SumPHPopulation` PH branch to call the engine, exactly like the GEPT cutover.

**Tech Stack:** ASP.NET Core 8 MVC, Entity Framework Core 8, NUnit 4, SQL Server (`sqlcmd`).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-15-ph-aggregation-migration-design.md` — this plan implements it exactly; do not deviate from its 24-course rule table without stopping to re-check with the user.
- No database schema changes — every column this plan sets already exists and is already used by GEPT's configuration.
- This plan covers ONLY the PH type. PS/PSJ/AfterSchool follow the same pattern in separate future plans — do not touch their branches in `SumPHPopulation`.
- Per the approved spec: **comment out the old PH code, do not delete it.** Deletion is a separate future task, done only after browser verification passes (same discipline as the GEPT migration, which also still has its old GEPT branch preserved... actually GEPT's old branch WAS deleted in commit 0e8b4dc — for PH, the user explicitly chose the more conservative "comment out first" option this time; follow that, not the GEPT precedent, for the code-removal step).
- Dev DB connection used for verification steps: `sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -d NewPAS07 -U sa -P 'cloudfun@12' -C` (matches `source/portal/Portal/appsettings.json`)
- Follow existing code style: no XML doc comments unless the WHY is non-obvious, `Include("string.path")` style EF includes (matches the rest of `StudentPopulationController.cs`), 4-space indentation matching surrounding files.
- `StudentPopulationType.PH` = `Type = 0` in the DB. `ClassType.SubGroup` = 小, `ClassType.V3` = 三 (matches existing PH course rows' usage).

---

### Task 1: Configure PH's 24 summary courses

**Files:**
- Create: `source/portal/docs/superpowers/sql/2026-07-15-ph-aggregation-rules-apply.sql`
- Create: `source/portal/docs/superpowers/sql/2026-07-15-ph-aggregation-rules-revert.sql`

**Interfaces:**
- Produces: correctly configured `Course.StatisticsType`/`SourceDepartmentIds`/`GroupByClassType` for PH's 24 `IsSum=true` courses — this is what Task 2's comparison test and Task 3's engine cutover depend on.

The rule mapping below is copied from the approved design spec (`docs/superpowers/specs/2026-07-15-ph-aggregation-migration-design.md`), grounded in real dev-DB department/course IDs (`NewPAS07`, `CourseDepartment`/`Course` where `Type=0`):

| Course Id | Name | Department | StatisticsType | SourceDepartmentIds | GroupByClassType |
|---|---|---|---|---|---|
| 9 | 英文國小人數合計 | 英文國小班 (1) | `SumByDepartmentAndClassType` (2) | — | 1 |
| 16 | 英文國中人數合計 | 英文國中班 (2) | `SumByDepartmentAndClassType` (2) | — | 1 |
| 21 | 英文高中人數合計 | 英文高中班 (3) | `SumByDepartmentAndClassType` (2) | — | 1 |
| 22 | 英文總班數統計 | 英文統計 (4) | `CountClassesByClassType` (31) | `[1,2,3]` | 1 |
| 27 | 英文個別指導人數合計 | 英文個別指導 (5) | `SumByDepartment` (1) | — | 0 |
| 32 | 英文合作開班人數合計 | 英文合作開班 (6) | `SumByDepartment` (1) | — | 0 |
| 33 | 本週英語文總人數 | 英文統計 (4) | `SumBySourceDepartments` (3) | `[1,2,3,5,6]` | 0 |
| 34 | 上週英語文總人數 | 英文統計 (4) | `LastWeekValue` (40) | `[1,2,3,5,6]` | 0 |
| 35 | 與上週相比 | 英文分析 (7) | `DiffWithLastWeek` (10) | `[1,2,3,5,6]` | 0 |
| 36 | 去年同期/比 | 英文分析 (7) | `DiffWithLastYear` (11) | `[1,2,3,5,6]` | 0 |
| 37 | 本週英語文新生 | 英文分析 (7) | `ManualInput` (50) | — | — |
| 38 | 本週英語文流失 | 英文分析 (7) | `ManualInput` (50) | — | — |
| 48 | 國語文人數合計 | 國語文 (8) | `SumByDepartmentAndClassType` (2) | — | 1 |
| 49 | 國文總班數 | 國語文統計 (9) | `CountClassesByClassType` (31) | `[8]` | 1 |
| 54 | 國語文個別指導人數合計 | 國語文個別指導 (10) | `SumByDepartment` (1) | — | 0 |
| 59 | 國語文合作開班人數合計 | 國語文合作開班 (11) | `SumByDepartment` (1) | — | 0 |
| 60 | 本週國語文總人數 | 國語文統計 (9) | `SumBySourceDepartments` (3) | `[8,10,11]` | 0 |
| 61 | 上週國語文總人數 | 國語文統計 (9) | `LastWeekValue` (40) | `[8,10,11]` | 0 |
| 62 | 與上週相比 | 國語文分析 (12) | `DiffWithLastWeek` (10) | `[8,10,11]` | 0 |
| 63 | 去年同期/比 | 國語文分析 (12) | `DiffWithLastYear` (11) | `[8,10,11]` | 0 |
| 64 | 本週國語文新生人數 | 國語文分析 (12) | `ManualInput` (50) | — | — |
| 65 | 本週國語文流失人數 | 國語文分析 (12) | `ManualInput` (50) | — | — |
| 66 | 本週總詢問(填單)人數 | 統計 (13) | `ManualInput` (50) | — | — |
| 67 | 總人數 | 統計 (13) | `SumBySourceDepartments` (3) | `[1,2,3,5,6,8,10,11]` | 0 |

- [ ] **Step 1: Write the apply script**

Create `source/portal/docs/superpowers/sql/2026-07-15-ph-aggregation-rules-apply.sql`:

```sql
-- PH (StudentPopulationType.PH, Type=0) aggregation rule configuration.
-- See docs/superpowers/plans/2026-07-15-ph-aggregation-migration.md, Task 1, and
-- docs/superpowers/specs/2026-07-15-ph-aggregation-migration-design.md for the full mapping table and rationale.
--
-- IMPORTANT: if this script is ever reverted (see the paired revert script), Task 3's code
-- cutover (StudentPopulationController.cs, SumPHPopulation PH branch calling AggregationEngine)
-- MUST be reverted at the same time. Reverting only the SQL leaves StatisticsType NULL while the
-- live code still calls the engine, which will leave every PH summary course frozen at whatever
-- value it last held (the engine no-ops on StatisticsType=null) — worse than the original bug.

-- 同班系+同班別加總 (own department, split by 小/三)
UPDATE Course SET StatisticsType = 2, GroupByClassType = 1 WHERE Id IN (9, 16, 21, 48);

-- 同班系加總，不分班別 (own department, EM1/合作開班 combine 小+三 into one number)
UPDATE Course SET StatisticsType = 1, GroupByClassType = 0 WHERE Id IN (27, 32, 54, 59);

-- 班數統計 (Number > 0 count), split by 小/三
UPDATE Course SET StatisticsType = 31, SourceDepartmentIds = '[1,2,3]', GroupByClassType = 1 WHERE Id = 22;  -- 英文總班數統計
UPDATE Course SET StatisticsType = 31, SourceDepartmentIds = '[8]', GroupByClassType = 1 WHERE Id = 49;      -- 國文總班數

-- 本週英語文/國語文總人數 (sum across all raw English/Chinese departments, not split by 小/三)
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[1,2,3,5,6]', GroupByClassType = 0 WHERE Id = 33;   -- 本週英語文總人數
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[8,10,11]', GroupByClassType = 0 WHERE Id = 60;     -- 本週國語文總人數

-- 上週英語文/國語文總人數 (LastWeekValue — fixes the previously-dead 0-only column per the design spec)
UPDATE Course SET StatisticsType = 40, SourceDepartmentIds = '[1,2,3,5,6]', GroupByClassType = 0 WHERE Id = 34;  -- 上週英語文總人數
UPDATE Course SET StatisticsType = 40, SourceDepartmentIds = '[8,10,11]', GroupByClassType = 0 WHERE Id = 61;    -- 上週國語文總人數

-- 與上週相比 (DiffWithLastWeek)
UPDATE Course SET StatisticsType = 10, SourceDepartmentIds = '[1,2,3,5,6]', GroupByClassType = 0 WHERE Id = 35;  -- 英文 與上週相比
UPDATE Course SET StatisticsType = 10, SourceDepartmentIds = '[8,10,11]', GroupByClassType = 0 WHERE Id = 62;    -- 國文 與上週相比

-- 去年同期/比 (DiffWithLastYear)
UPDATE Course SET StatisticsType = 11, SourceDepartmentIds = '[1,2,3,5,6]', GroupByClassType = 0 WHERE Id = 36;  -- 英文 去年同期/比
UPDATE Course SET StatisticsType = 11, SourceDepartmentIds = '[8,10,11]', GroupByClassType = 0 WHERE Id = 63;    -- 國文 去年同期/比

-- 手動輸入 (新生/流失 + 本週總詢問(填單)人數 — never auto-computed)
UPDATE Course SET StatisticsType = 50 WHERE Id IN (37, 38, 64, 65, 66);

-- 總人數 (grand total across every raw PH department)
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[1,2,3,5,6,8,10,11]', GroupByClassType = 0 WHERE Id = 67;
```

Create `source/portal/docs/superpowers/sql/2026-07-15-ph-aggregation-rules-revert.sql`:

```sql
-- Revert PH aggregation rule configuration.
-- WARNING: only run this together with reverting the Task 3 code change
-- (SumPHPopulation's PH branch calling AggregationEngine) — see the apply
-- script's header comment for why running one without the other is unsafe.
UPDATE Course
SET StatisticsType = NULL, SourceDepartmentIds = NULL, GroupByClassType = 0
WHERE Id IN (9, 16, 21, 22, 27, 32, 33, 34, 35, 36, 37, 38,
             48, 49, 54, 59, 60, 61, 62, 63, 64, 65, 66, 67);
```

- [ ] **Step 2: Run the apply script against the dev DB**

Run:
```bash
sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -d NewPAS07 -U sa -P 'cloudfun@12' -C -i "source/portal/docs/superpowers/sql/2026-07-15-ph-aggregation-rules-apply.sql"
```
Expected: a series of `(N 個資料列受到影響)` messages, no errors.

- [ ] **Step 3: Verify the configuration landed correctly**

Run:
```bash
sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -d NewPAS07 -U sa -P 'cloudfun@12' -C -Q "SELECT Id, Name, StatisticsType, SourceDepartmentIds, GroupByClassType FROM Course WHERE Id IN (9,16,21,22,27,32,33,34,35,36,37,38,48,49,54,59,60,61,62,63,64,65,66,67) ORDER BY Id" -W -s"|"
```
Expected: 24 rows, `StatisticsType`/`SourceDepartmentIds`/`GroupByClassType` matching the mapping table above exactly.

- [ ] **Step 4: Commit the scripts**

```bash
git add source/portal/docs/superpowers/sql/2026-07-15-ph-aggregation-rules-apply.sql source/portal/docs/superpowers/sql/2026-07-15-ph-aggregation-rules-revert.sql
git commit -m "data: configure PH's 24 summary courses with AggregationEngine rules"
```

---

### Task 2: Verify the engine against real PH data

**Files:**
- Create: `source/portal/Test/Services/Aggregation/PhAggregationComparisonTests.cs`

**Interfaces:**
- Consumes: `AggregationEngine` (already exists, `source/portal/Portal/Services/Aggregation/AggregationEngine.cs`), PH course configuration (Task 1), `DataContext` (Data.csproj, already referenced transitively via `Test.csproj`'s `ProjectReference` to `Portal.csproj`, added during the GEPT migration).
- Produces: confidence that the engine reproduces every currently-stored PH summary number (with two documented, intentional exceptions) before Task 3 flips the live code path over.

This follows the exact pattern of `source/portal/Test/Services/Aggregation/GeptAggregationComparisonTests.cs`, with one addition: PH has two courses per the design spec where a mismatch is *expected* (Id 22/49 — `CountClasses` semantics changed from "all rows" to "`Number > 0` rows"; Id 34/61 — `LastWeekValue` now actually computes a value instead of the previously-dead always-0 column). The test must bucket mismatches into "expected" vs "unexpected" so a real regression can't hide among expected changes.

- [ ] **Step 1: Write the comparison test**

Create `source/portal/Test/Services/Aggregation/PhAggregationComparisonTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Aggregation;

namespace PHStatistics.Portal.Test.Services.Aggregation;

[TestFixture]
[Explicit("Requires a live connection to the dev database; run manually to verify the PH AggregationEngine migration before cutover (see plan Task 2)")]
public class PhAggregationComparisonTests {
    // Id 22/49 (英文/國文總班數)：CountClasses 語意故意從「全部筆數」改成「Number>0 筆數」，允許不同。
    // Id 34/61 (上週英語文/國語文總人數)：故意從「永遠是死值0」改成 LastWeekValue，允許不同。
    // 兩者皆為 docs/superpowers/specs/2026-07-15-ph-aggregation-migration-design.md 記錄的刻意行為變更。
    private static readonly HashSet<int> ExpectedChangeCourseIds = new() { 22, 34, 49, 61 };

    [Test]
    public void Engine_ReproducesStoredNumbers_ForAllRealPhPopulations() {
        using var context = new DataContext();

        var populations = context.StudentPopulation
            .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
            .Where(p => p.Type == StudentPopulationType.PH && p.DataMode == DataMode.Normal)
            .ToList();

        var engine = new AggregationEngine((year, week, schoolId, type) =>
            context.StudentPopulation
                .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type));

        var unexpectedMismatches = new List<string>();
        var expectedChanges = new List<string>();
        foreach (var population in populations) {
            foreach (var item in population.Items.Where(i => i.IsSum).ToList()) {
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

        TestContext.WriteLine($"Checked {populations.Count} PH populations.");
        TestContext.WriteLine($"{expectedChanges.Count} expected changes (course 22/34/49/61 — intentional behavior fixes):");
        foreach (var m in expectedChanges) TestContext.WriteLine(m);
        TestContext.WriteLine($"{unexpectedMismatches.Count} UNEXPECTED mismatches:");
        foreach (var m in unexpectedMismatches) TestContext.WriteLine(m);

        Assert.That(unexpectedMismatches, Is.Empty, () => string.Join("\n", unexpectedMismatches));
    }
}
```

- [ ] **Step 2: Run it against the dev DB**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~PhAggregationComparisonTests"`

(This is the same invocation style the GEPT migration's Task 6 confirmed works for `[Explicit]` tests filtered by name — no special NUnit category syntax needed.)

If `new DataContext()` throws a configuration-resolution error (same failure mode documented in the GEPT migration plan), replace the `using var context = new DataContext();` line with:

```csharp
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer("Server=CLOUDFUN-MSI-LE\\SQLEXPRESS;Database=NewPAS07;User=sa;Pwd=cloudfun@12;Encrypt=false;MultipleActiveResultSets=true")
            .Options;
        using var context = new DataContext(options);
```

- [ ] **Step 3: Interpret the result**

- Test passes (0 *unexpected* mismatches) → read the "expected changes" log lines to sanity-check they're all course 22/34/49/61 (nothing else snuck into that bucket by coincidence), then proceed to Task 3.
- Test fails (unexpected mismatches exist) → read each mismatch line and check whether it's actually explained by something the design spec already flagged but that isn't in `ExpectedChangeCourseIds` (re-read the spec's "已確認的行為變更" section before concluding it's a new bug). If it's a genuine rule-configuration mistake, fix the SQL (Task 1) or a follow-up `UPDATE Course` statement, re-run, and re-check.
- If the test cannot be run at all in this environment (no reachable dev DB), do NOT proceed to Task 3. Escalate to a human who can run it against the dev DB and report the result — Task 3 depends on this test passing.

- [ ] **Step 4: Commit**

```bash
git add source/portal/Test/Services/Aggregation/PhAggregationComparisonTests.cs
git commit -m "test: add explicit PH AggregationEngine comparison test against real dev data"
```

---

### Task 3: Cut PH over to the AggregationEngine (old code commented out, not deleted)

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes: `AggregationEngine` (existing), verified PH configuration (Task 1, confirmed by Task 2).
- Produces: `SumPHPopulation`'s PH branch now delegates to the shared engine for all 24 summary courses; PS/PSJ/AfterSchool branches are untouched (out of scope — future plans). The ~260 lines of old PH-specific logic remain in the file, wrapped in `#if false`/`#endif` so they don't compile, per the user's decision to defer deletion to a follow-up task.

- [ ] **Step 1: Wrap the `classGroup`-loop PH branch in `#if false` and add the engine call**

In `source/portal/Portal/Controllers/StudentPopulationController.cs`, find this block (currently lines 1408-1449):

```csharp
                    if (studentPopulationData.Type == StudentPopulationType.PH) {
                        if (group.Class.Course.Name.Equals("本週總詢問人數") || group.Class.Course.Name.Equals("本週總詢問(填單)人數") ||
                            group.Class.Course.Name.Equals("本週英語文新生") || group.Class.Course.Name.Equals("本週英語文流失") ||
                            group.Class.Course.Name.Equals("本週國語文新生人數") || group.Class.Course.Name.Equals("本週國語文流失人數")) {
                            // 新生/流失：分校自填，系統不計算。這裡必須提早 continue 跳過，
                            // 否則會先被下面「同班系非加總課程加總」預設成 0（英文分析/國語文分析班系底下沒有非加總課程）。
                            continue;
                        }
                        //取得相同班系及班型的班級
                        List<StudentPopulationItem> classItems = new List<StudentPopulationItem>();
                        if (group.Name.Contains("個別指導"))
                        {
                            classItems = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id  && !e.Class.Course.IsSum).ToList();
                        }
                        else {
                            classItems = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).ToList();
                        }
                        group.Number = classItems.Sum(e => e.Number);

                        if (group.Class.Course.Name.Equals("英文個別指導人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("英文個別指導")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("國語文個別指導人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("國語文個別指導")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("英文合作開班人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("英文合作開班")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("國語文合作開班人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("國語文合作開班")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        //else if (group.Class.Course.Name.IndexOf("與上週相比") >= 0) {
                        //    string subjectName = group.Class.Course.Name.Replace("與上週相比", "").Replace("本週", "");
                        //    int lastWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.LastWeekNumber);
                        //    int thisWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                        //    group.Number = thisWeekNum - lastWeekNum;
                        //}
                    }
```

Replace it with (old body preserved verbatim inside `#if false`, new engine call added after `#endif`):

```csharp
                    if (studentPopulationData.Type == StudentPopulationType.PH) {
#if false // 舊 PH 加總邏輯，2026-07-15 遷移到 AggregationEngine 時停用保留（不刪除），
          // 見 docs/superpowers/plans/2026-07-15-ph-aggregation-migration.md Task 3
                        if (group.Class.Course.Name.Equals("本週總詢問人數") || group.Class.Course.Name.Equals("本週總詢問(填單)人數") ||
                            group.Class.Course.Name.Equals("本週英語文新生") || group.Class.Course.Name.Equals("本週英語文流失") ||
                            group.Class.Course.Name.Equals("本週國語文新生人數") || group.Class.Course.Name.Equals("本週國語文流失人數")) {
                            // 新生/流失：分校自填，系統不計算。這裡必須提早 continue 跳過，
                            // 否則會先被下面「同班系非加總課程加總」預設成 0（英文分析/國語文分析班系底下沒有非加總課程）。
                            continue;
                        }
                        //取得相同班系及班型的班級
                        List<StudentPopulationItem> classItems = new List<StudentPopulationItem>();
                        if (group.Name.Contains("個別指導"))
                        {
                            classItems = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id  && !e.Class.Course.IsSum).ToList();
                        }
                        else {
                            classItems = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).ToList();
                        }
                        group.Number = classItems.Sum(e => e.Number);

                        if (group.Class.Course.Name.Equals("英文個別指導人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("英文個別指導")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("國語文個別指導人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("國語文個別指導")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("英文合作開班人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("英文合作開班")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("國語文合作開班人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("國語文合作開班")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        //else if (group.Class.Course.Name.IndexOf("與上週相比") >= 0) {
                        //    string subjectName = group.Class.Course.Name.Replace("與上週相比", "").Replace("本週", "");
                        //    int lastWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.LastWeekNumber);
                        //    int thisWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                        //    group.Number = thisWeekNum - lastWeekNum;
                        //}
#endif
                        aggregationEngine.Calculate(group, studentPopulationData);
                    }
```

- [ ] **Step 2: Wrap the post-loop PH-specific block in `#if false`**

Immediately after the `classGroup` `foreach` loop closes, find this block (currently lines 1519-1679):

```csharp
            if (studentPopulationData.Type == StudentPopulationType.PH) {
                //總班數 小
                StudentPopulationItem subgroupClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("英文總班數統計") && e.Class.Type == ClassType.SubGroup);
                int[] countIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && (e.Department.Name.Equals("英文國小班") || e.Department.Name.Equals("英文國中班") || e.Department.Name.Equals("英文高中班"))).Select(e => e.Id).ToArray();
                if (subgroupClassCount != null) {
                    subgroupClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && countIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.SubGroup && !e.Class.Course.IsSum).Count();
                    dataContext.StudentPopulationItem.Update(subgroupClassCount);
                    dataContext.SaveChanges();
                }
                //總班數 三
                StudentPopulationItem em3ClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("英文總班數統計") && e.Class.Type == ClassType.V3);
                if (em3ClassCount != null) {
                    em3ClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && countIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.V3 && !e.Class.Course.IsSum).Count();
                    dataContext.StudentPopulationItem.Update(em3ClassCount);
                    dataContext.SaveChanges();
                }

                //國文總班數 小
                StudentPopulationItem subgroupChClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("國文總班數") && e.Class.Type == ClassType.SubGroup);
                int[] chIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && (e.Department.Name.Equals("國語文"))).Select(e => e.Id).ToArray();
                if (subgroupChClassCount != null) {
                    subgroupChClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && chIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.SubGroup && !e.Class.Course.IsSum).Count();
                    dataContext.StudentPopulationItem.Update(subgroupChClassCount);
                    dataContext.SaveChanges();
                }

                //國文總班數 三
                StudentPopulationItem em3ChClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("國文總班數") && e.Class.Type == ClassType.V3);
                if (em3ChClassCount != null) {
                    em3ChClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && chIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.V3 && !e.Class.Course.IsSum).Count();
                    dataContext.StudentPopulationItem.Update(em3ChClassCount);
                    dataContext.SaveChanges();
                }

                //本週英語文總人數 全部
                //如果有重新匯入課程要調整對應Id
                int[] enCountIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && e.Id <= 38).Select(e => e.Id).ToArray();
                StudentPopulationItem sumWeekEn3Count = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週英語文總人數"));
                if (sumWeekEn3Count != null) {
                    sumWeekEn3Count.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && enCountIds.Contains(e.Class.Course.Id) && !e.Class.Course.IsSum).Sum(e => e.Number);
                    dataContext.StudentPopulationItem.Update(sumWeekEn3Count);
                    dataContext.SaveChanges();
                }

                //本週英語文新生／本週英語文流失：分校自填，系統不計算

                //本週國語文總人數 全部
                int[] chCountIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && e.Id > 38 && e.Id <= 58).Select(e => e.Id).ToArray();
                StudentPopulationItem sumWeekCh3Count = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週國語文總人數"));
                if (sumWeekCh3Count != null) {
                    sumWeekCh3Count.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && chCountIds.Contains(e.Class.Course.Id) && !e.Class.Course.IsSum).Sum(e => e.Number);
                    dataContext.StudentPopulationItem.Update(sumWeekCh3Count);
                    dataContext.SaveChanges();
                }

                //本週國語文新生人數／本週國語文流失人數：分校自填，系統不計算

                /*與上週相比(英文)
                33 本週英語文總人數
                34 上週英語文總人數        
                35 與上週相比
                36 去年同期/比
                 */
                try {
                    int lastWeek = studentPopulationData.Week - 1;

                    // 英文「與上週相比」：用英語文總人數（非全部人數）
                    StudentPopulationItem enDiffItem = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("與上週相比") &&
                                             e.Class.Course.Department?.Name == "英文分析");
                    if (enDiffItem != null) {
                        int enLastWeekNum = dataContext.StudentPopulationItem
                            .FirstOrDefault(e => e.StudentPopulation.Year == studentPopulationData.Year &&
                                                 e.StudentPopulation.Week == lastWeek &&
                                                 e.StudentPopulation.School.Id == studentPopulationData.School.Id &&
                                                 e.Class.Course.Name.Equals("本週英語文總人數"))?.Number ?? 0;
                        int enThisWeekNum = studentPopulationData.Items
                            .FirstOrDefault(e => e.Class.Course.Name.Equals("本週英語文總人數"))?.Number ?? 0;
                        enDiffItem.LastWeekNumber = enLastWeekNum;
                        enDiffItem.Number = enThisWeekNum - enLastWeekNum;
                        dataContext.StudentPopulationItem.Update(enDiffItem);
                        dataContext.SaveChanges();
                    }

                    // 國文「與上週相比」：找國語文分析的 item，用國語文總人數
                    StudentPopulationItem chDiffItem = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("與上週相比") &&
                                             e.Class.Course.Department?.Name == "國語文分析");
                    if (chDiffItem != null) {
                        int chLastWeekNum = dataContext.StudentPopulationItem
                            .FirstOrDefault(e => e.StudentPopulation.Year == studentPopulationData.Year &&
                                                 e.StudentPopulation.Week == lastWeek &&
                                                 e.StudentPopulation.School.Id == studentPopulationData.School.Id &&
                                                 e.Class.Course.Name.Equals("本週國語文總人數"))?.Number ?? 0;
                        int chThisWeekNum = studentPopulationData.Items
                            .FirstOrDefault(e => e.Class.Course.Name.Equals("本週國語文總人數"))?.Number ?? 0;
                        chDiffItem.LastWeekNumber = chLastWeekNum;
                        chDiffItem.Number = chThisWeekNum - chLastWeekNum;
                        dataContext.StudentPopulationItem.Update(chDiffItem);
                        dataContext.SaveChanges();
                    }
                }
                catch (Exception ex) {
                }

                // PH 去年同期/比（英文 & 國文）
                try {
                    int lastYear = studentPopulationData.Year - 1;
                    List<int> enCountIdList = enCountIds.ToList();
                    List<int> chCountIdList = chCountIds.ToList();
                    int enLastYearNum = dataContext.StudentPopulationItem
                        .Include("StudentPopulation")
                        .Where(e => e.StudentPopulation.Year == lastYear &&
                                    e.StudentPopulation.Week == studentPopulationData.Week &&
                                    e.StudentPopulation.SchoolId == studentPopulationData.School.Id &&
                                    e.StudentPopulation.Type == StudentPopulationType.PH &&
                                    e.Class.CourseId.HasValue && enCountIdList.Contains(e.Class.CourseId.Value))
                        .Sum(e => e.Number);
                    int chLastYearNum = dataContext.StudentPopulationItem
                        .Include("StudentPopulation")
                        .Where(e => e.StudentPopulation.Year == lastYear &&
                                    e.StudentPopulation.Week == studentPopulationData.Week &&
                                    e.StudentPopulation.SchoolId == studentPopulationData.School.Id &&
                                    e.StudentPopulation.Type == StudentPopulationType.PH &&
                                    e.Class.CourseId.HasValue && chCountIdList.Contains(e.Class.CourseId.Value))
                        .Sum(e => e.Number);

                    int enTotalThisWeek = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("本週英語文總人數"))?.Number ?? 0;
                    int chTotalThisWeek = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("本週國語文總人數"))?.Number ?? 0;

                    StudentPopulationItem enLastYearItem = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("去年同期/比") &&
                                             e.Class.Course.Department?.Name == "英文分析");
                    if (enLastYearItem != null) {
                        enLastYearItem.Number = enTotalThisWeek - enLastYearNum;
                        dataContext.StudentPopulationItem.Update(enLastYearItem);
                        dataContext.SaveChanges();
                    }

                    StudentPopulationItem chLastYearItem = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("去年同期/比") &&
                                             e.Class.Course.Department?.Name == "國語文分析");
                    if (chLastYearItem != null) {
                        chLastYearItem.Number = chTotalThisWeek - chLastYearNum;
                        dataContext.StudentPopulationItem.Update(chLastYearItem);
                        dataContext.SaveChanges();
                    }
                }
                catch (Exception ex) {
                }

                //總人數
                StudentPopulationItem sumAllCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("總人數"));
                if (sumAllCount != null) {
                    sumAllCount.Number = studentPopulationData.Items.Where(e => !e.Class.Course.IsSum).Sum(e => e.Number);
                    dataContext.StudentPopulationItem.Update(sumAllCount);
                    dataContext.SaveChanges();
                }
            }
```

Replace it with (entire body preserved verbatim inside `#if false`; the `if` becomes an empty block, matching the existing empty `else if (Type==PSJ) {} else if (Type==GEPT) {} else if (Type==PS) {}` blocks that already follow it):

```csharp
            if (studentPopulationData.Type == StudentPopulationType.PH) {
#if false // 舊 PH 後處理邏輯，已併入 AggregationEngine（classGroup 迴圈內的單一呼叫已涵蓋這 24 個課程），
          // 2026-07-15 停用保留（不刪除），見 docs/superpowers/plans/2026-07-15-ph-aggregation-migration.md Task 3
                //總班數 小
                StudentPopulationItem subgroupClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("英文總班數統計") && e.Class.Type == ClassType.SubGroup);
                int[] countIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && (e.Department.Name.Equals("英文國小班") || e.Department.Name.Equals("英文國中班") || e.Department.Name.Equals("英文高中班"))).Select(e => e.Id).ToArray();
                if (subgroupClassCount != null) {
                    subgroupClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && countIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.SubGroup && !e.Class.Course.IsSum).Count();
                    dataContext.StudentPopulationItem.Update(subgroupClassCount);
                    dataContext.SaveChanges();
                }
                //總班數 三
                StudentPopulationItem em3ClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("英文總班數統計") && e.Class.Type == ClassType.V3);
                if (em3ClassCount != null) {
                    em3ClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && countIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.V3 && !e.Class.Course.IsSum).Count();
                    dataContext.StudentPopulationItem.Update(em3ClassCount);
                    dataContext.SaveChanges();
                }

                //國文總班數 小
                StudentPopulationItem subgroupChClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("國文總班數") && e.Class.Type == ClassType.SubGroup);
                int[] chIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && (e.Department.Name.Equals("國語文"))).Select(e => e.Id).ToArray();
                if (subgroupChClassCount != null) {
                    subgroupChClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && chIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.SubGroup && !e.Class.Course.IsSum).Count();
                    dataContext.StudentPopulationItem.Update(subgroupChClassCount);
                    dataContext.SaveChanges();
                }

                //國文總班數 三
                StudentPopulationItem em3ChClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("國文總班數") && e.Class.Type == ClassType.V3);
                if (em3ChClassCount != null) {
                    em3ChClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && chIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.V3 && !e.Class.Course.IsSum).Count();
                    dataContext.StudentPopulationItem.Update(em3ChClassCount);
                    dataContext.SaveChanges();
                }

                //本週英語文總人數 全部
                //如果有重新匯入課程要調整對應Id
                int[] enCountIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && e.Id <= 38).Select(e => e.Id).ToArray();
                StudentPopulationItem sumWeekEn3Count = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週英語文總人數"));
                if (sumWeekEn3Count != null) {
                    sumWeekEn3Count.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && enCountIds.Contains(e.Class.Course.Id) && !e.Class.Course.IsSum).Sum(e => e.Number);
                    dataContext.StudentPopulationItem.Update(sumWeekEn3Count);
                    dataContext.SaveChanges();
                }

                //本週英語文新生／本週英語文流失：分校自填，系統不計算

                //本週國語文總人數 全部
                int[] chCountIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && e.Id > 38 && e.Id <= 58).Select(e => e.Id).ToArray();
                StudentPopulationItem sumWeekCh3Count = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週國語文總人數"));
                if (sumWeekCh3Count != null) {
                    sumWeekCh3Count.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && chCountIds.Contains(e.Class.Course.Id) && !e.Class.Course.IsSum).Sum(e => e.Number);
                    dataContext.StudentPopulationItem.Update(sumWeekCh3Count);
                    dataContext.SaveChanges();
                }

                //本週國語文新生人數／本週國語文流失人數：分校自填，系統不計算

                /*與上週相比(英文)
                33 本週英語文總人數
                34 上週英語文總人數        
                35 與上週相比
                36 去年同期/比
                 */
                try {
                    int lastWeek = studentPopulationData.Week - 1;

                    // 英文「與上週相比」：用英語文總人數（非全部人數）
                    StudentPopulationItem enDiffItem = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("與上週相比") &&
                                             e.Class.Course.Department?.Name == "英文分析");
                    if (enDiffItem != null) {
                        int enLastWeekNum = dataContext.StudentPopulationItem
                            .FirstOrDefault(e => e.StudentPopulation.Year == studentPopulationData.Year &&
                                                 e.StudentPopulation.Week == lastWeek &&
                                                 e.StudentPopulation.School.Id == studentPopulationData.School.Id &&
                                                 e.Class.Course.Name.Equals("本週英語文總人數"))?.Number ?? 0;
                        int enThisWeekNum = studentPopulationData.Items
                            .FirstOrDefault(e => e.Class.Course.Name.Equals("本週英語文總人數"))?.Number ?? 0;
                        enDiffItem.LastWeekNumber = enLastWeekNum;
                        enDiffItem.Number = enThisWeekNum - enLastWeekNum;
                        dataContext.StudentPopulationItem.Update(enDiffItem);
                        dataContext.SaveChanges();
                    }

                    // 國文「與上週相比」：找國語文分析的 item，用國語文總人數
                    StudentPopulationItem chDiffItem = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("與上週相比") &&
                                             e.Class.Course.Department?.Name == "國語文分析");
                    if (chDiffItem != null) {
                        int chLastWeekNum = dataContext.StudentPopulationItem
                            .FirstOrDefault(e => e.StudentPopulation.Year == studentPopulationData.Year &&
                                                 e.StudentPopulation.Week == lastWeek &&
                                                 e.StudentPopulation.School.Id == studentPopulationData.School.Id &&
                                                 e.Class.Course.Name.Equals("本週國語文總人數"))?.Number ?? 0;
                        int chThisWeekNum = studentPopulationData.Items
                            .FirstOrDefault(e => e.Class.Course.Name.Equals("本週國語文總人數"))?.Number ?? 0;
                        chDiffItem.LastWeekNumber = chLastWeekNum;
                        chDiffItem.Number = chThisWeekNum - chLastWeekNum;
                        dataContext.StudentPopulationItem.Update(chDiffItem);
                        dataContext.SaveChanges();
                    }
                }
                catch (Exception ex) {
                }

                // PH 去年同期/比（英文 & 國文）
                try {
                    int lastYear = studentPopulationData.Year - 1;
                    List<int> enCountIdList = enCountIds.ToList();
                    List<int> chCountIdList = chCountIds.ToList();
                    int enLastYearNum = dataContext.StudentPopulationItem
                        .Include("StudentPopulation")
                        .Where(e => e.StudentPopulation.Year == lastYear &&
                                    e.StudentPopulation.Week == studentPopulationData.Week &&
                                    e.StudentPopulation.SchoolId == studentPopulationData.School.Id &&
                                    e.StudentPopulation.Type == StudentPopulationType.PH &&
                                    e.Class.CourseId.HasValue && enCountIdList.Contains(e.Class.CourseId.Value))
                        .Sum(e => e.Number);
                    int chLastYearNum = dataContext.StudentPopulationItem
                        .Include("StudentPopulation")
                        .Where(e => e.StudentPopulation.Year == lastYear &&
                                    e.StudentPopulation.Week == studentPopulationData.Week &&
                                    e.StudentPopulation.SchoolId == studentPopulationData.School.Id &&
                                    e.StudentPopulation.Type == StudentPopulationType.PH &&
                                    e.Class.CourseId.HasValue && chCountIdList.Contains(e.Class.CourseId.Value))
                        .Sum(e => e.Number);

                    int enTotalThisWeek = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("本週英語文總人數"))?.Number ?? 0;
                    int chTotalThisWeek = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("本週國語文總人數"))?.Number ?? 0;

                    StudentPopulationItem enLastYearItem = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("去年同期/比") &&
                                             e.Class.Course.Department?.Name == "英文分析");
                    if (enLastYearItem != null) {
                        enLastYearItem.Number = enTotalThisWeek - enLastYearNum;
                        dataContext.StudentPopulationItem.Update(enLastYearItem);
                        dataContext.SaveChanges();
                    }

                    StudentPopulationItem chLastYearItem = studentPopulationData.Items
                        .FirstOrDefault(e => e.Class.Course.Name.Equals("去年同期/比") &&
                                             e.Class.Course.Department?.Name == "國語文分析");
                    if (chLastYearItem != null) {
                        chLastYearItem.Number = chTotalThisWeek - chLastYearNum;
                        dataContext.StudentPopulationItem.Update(chLastYearItem);
                        dataContext.SaveChanges();
                    }
                }
                catch (Exception ex) {
                }

                //總人數
                StudentPopulationItem sumAllCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("總人數"));
                if (sumAllCount != null) {
                    sumAllCount.Number = studentPopulationData.Items.Where(e => !e.Class.Course.IsSum).Sum(e => e.Number);
                    dataContext.StudentPopulationItem.Update(sumAllCount);
                    dataContext.SaveChanges();
                }
#endif
            }
```

- [ ] **Step 3: Build**

Run: `dotnet build source/portal/Portal/Portal.csproj -c Debug`
Expected: `0 Error(s)`. (`#if false` blocks are still parsed enough to require balanced braces inside them, but they compile out entirely — if the build fails with an unexpected token error inside one of these blocks, it means the pasted old code doesn't exactly match what's currently in the file; re-copy the exact current content of that region instead of the plan's copy before wrapping it.)

- [ ] **Step 4: Re-run the Task 2 comparison test to confirm nothing regressed**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~PhAggregationComparisonTests"` and confirm it still shows 0 unexpected mismatches. The test independently constructs its own `AggregationEngine` and reads live data — it doesn't call `SumPHPopulation` directly — so this step is a sanity check that swapping the actual call site didn't somehow introduce a discrepancy versus Task 2's isolated check, not a re-test of new logic.

- [ ] **Step 5: Manual browser verification**

This changes a live save-path for every PH population — the highest-usage type in the system. Per this project's established verification pattern, note this alongside (but do not skip separately from) the other pending manual-verification items already tracked in project memory:
- Open a PH population for a real school, edit a regular (non-summary) course's number (e.g. a P1-初階 class), save
- Confirm 英文/國文國小/國中/高中人數合計、英文/國文總班數、英文/國文個別指導/合作開班人數合計、本週英語文/國語文總人數 all recompute to the expected values
- Confirm 上週英語文/國語文總人數 now show a real non-zero number (not the old dead 0) — this is the fix the user asked for
- Confirm 與上週相比、去年同期/比 (both 英文/國文) show reasonable values
- Confirm 本週英語文/國語文新生/流失人數 and 本週總詢問(填單)人數 are NOT reset when other fields are saved (same class of bug already fixed once for PH/GEPT earlier this session — re-verify it stays fixed through this engine swap)
- Confirm 總人數 equals the sum of every non-summary course's number for that school/week

- [ ] **Step 6: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "refactor: migrate PH aggregation to the shared AggregationEngine (old code disabled, not deleted)"
```

---

## Self-Review Notes

**Spec coverage:** Task 1 configures all 24 courses from the design spec's rule table, including the two intentional behavior changes (Id 22/49, Id 34/61). Task 2's comparison test explicitly separates those two expected-change course IDs from the must-match set, matching the spec's testing section. Task 3 implements the "comment out, don't delete" decision using `#if false`/`#endif` (chosen over block comments specifically because the original code already contains a `/* ... */` block at the "與上週相比(英文)" comment, which would break nesting if the whole region were wrapped in `/* */`). The spec's "Out of scope" items (PS/PSJ/AfterSchool, GEPT's 5 deferred review items, dead-code deletion) are correctly not addressed here.

**Type consistency:** `AggregationEngine`'s constructor signature and `Calculate(item, population)` method are used identically to the existing GEPT wiring (`StudentPopulationController.cs`'s existing `aggregationEngine` variable, already instantiated once per `SumPHPopulation` call — Task 3 does not re-instantiate it, it reuses the same instance the GEPT branch already uses).

**Placeholder scan:** No TBD/TODO. Both `#if false` code blocks reproduce the current file's exact text (verified against a fresh read of `StudentPopulationController.cs` lines 1408-1449 and 1519-1679 at plan-writing time) rather than paraphrasing, so Task 3 has no ambiguity about what "the old code" is.
