# PSJ + AS Aggregation Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate PSJ (百倍速) and AfterSchool/AS (課輔) aggregate-course calculations from the inline if/else branches in `SumPHPopulation` to the shared `AggregationEngine`, matching the GEPT/PH/PS migrations already completed.

**Architecture:** No engine code changes this round — `AggregationEngine` already supports every `StatisticsType` this migration needs (`SumBySourceDepartments`, `DiffWithLastWeek`, `ManualInput`). Each type gets its aggregate `Course` rows configured via a plain SQL apply/revert script pair (this project's established convention — no EF migrations for course rule data), verified with an `[Explicit]` NUnit comparison test against live dev DB data, then cut over in `StudentPopulationController.SumPHPopulation` by replacing the inline branch body with a single `aggregationEngine.Calculate(group, studentPopulationData)` call. PSJ and AS are structurally identical (raw courses → per-classtype 合計 → total 總合計 → per-grade 與上週相比×12 → per-grade 新生×12 → per-grade 流失×12) so this plan does PSJ first, then AS, using the same 3-task recipe (SQL → test → cutover) each type used individually in the GEPT/PH/PS migrations.

**Tech Stack:** ASP.NET Core 8 / EF Core 8, `AggregationEngine` (`Portal/Services/Aggregation/AggregationEngine.cs`, unchanged), NUnit 4 `[Explicit]` integration tests against the live dev DB (same pattern as `PhAggregationComparisonTests.cs`/`PsAggregationComparisonTests.cs`), raw SQL scripts under `docs/superpowers/sql/`.

## Global Constraints

- No `AggregationEngine.cs` code changes — every `StatisticsType` needed (`SumBySourceDepartments`=3, `DiffWithLastWeek`=10, `ManualInput`=50) already exists and is already implemented in `Calculate`'s switch.
- AS's 257/258 (安親課輔班) and 307/308 (英文班) pairs must both use `GroupByClassType=false` — this is a deliberate 1:1 preservation of the existing (undifferentiated-by-classtype) behavior, confirmed with the user; do not "fix" this to split by `ClassType` even though PSJ's equivalent pair (157/158) does split.
- PSJ's 157/159–170/207/209–220 must use `GroupByClassType=true`; PSJ's 158/208 must use `GroupByClassType=false`. This asymmetry within PSJ itself (unlike AS, which is uniformly `false`) is intentional — it mirrors the existing code's own asymmetry (157 filters by `group.Class.Type`, 158 does not).
- Every `SourceCourseIds`/`SourceDepartmentIds` SQL value is a JSON array string (e.g. `'[145]'`, `'[27]'`), matching `AggregationEngine.ParseIntArray`'s expected format — never a bare integer.
- Old if/else branches in `SumPHPopulation` must be kept as `#if false`-wrapped dead code, not deleted (matches PH/PS convention, not GEPT's delete-outright choice).
- Any unexpected comparison-test mismatch must be investigated and reported to the user for an explicit accept/reject decision — never silently added to an "expected exceptions" set.
- Spec: `source/portal/docs/superpowers/specs/2026-07-16-psj-as-aggregation-migration-design.md`.

---

### Task 1: PSJ SQL rule configuration — apply to dev DB

**Files:**
- Create: `source/portal/docs/superpowers/sql/2026-07-16-psj-aggregation-rules-apply.sql`
- Create: `source/portal/docs/superpowers/sql/2026-07-16-psj-aggregation-rules-revert.sql`

**Interfaces:**
- Produces: live dev DB state (`Course.StatisticsType`/`SourceDepartmentIds`/`SourceCourseIds`/`GroupByClassType` set on Course Ids 157–194 and 207–244) that Task 2's comparison test reads.

- [ ] **Step 1: Write `2026-07-16-psj-aggregation-rules-apply.sql`**

```sql
-- PSJ (StudentPopulationType.PSJ, Type=1) aggregation rule configuration.
-- See docs/superpowers/specs/2026-07-16-psj-as-aggregation-migration-design.md and
-- docs/superpowers/plans/2026-07-16-psj-as-aggregation-migration.md Task 3 for the full
-- mapping table and rationale.
--
-- IMPORTANT: if this script is ever reverted (see the paired revert script), Task 3's code
-- cutover (StudentPopulationController.cs, SumPHPopulation PSJ branch calling AggregationEngine)
-- MUST be reverted at the same time. Reverting only the SQL leaves StatisticsType NULL while the
-- live code still calls the engine, which will leave every PSJ summary course frozen at whatever
-- value it last held (the engine no-ops on StatisticsType=null) — worse than the original bug.

-- 數學班：157 依班別分開算，158 不分班別（兩者都加總「數學班」部門(27)內的原始課程）
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[27]', GroupByClassType = 1 WHERE Id = 157;
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[27]' WHERE Id = 158;

-- 數學班：159-170 本週{年級}與上週相比 ×12，SourceCourseIds = [Id-14]（對應145-156），依班別分開算
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[145]', GroupByClassType = 1 WHERE Id = 159;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[146]', GroupByClassType = 1 WHERE Id = 160;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[147]', GroupByClassType = 1 WHERE Id = 161;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[148]', GroupByClassType = 1 WHERE Id = 162;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[149]', GroupByClassType = 1 WHERE Id = 163;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[150]', GroupByClassType = 1 WHERE Id = 164;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[151]', GroupByClassType = 1 WHERE Id = 165;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[152]', GroupByClassType = 1 WHERE Id = 166;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[153]', GroupByClassType = 1 WHERE Id = 167;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[154]', GroupByClassType = 1 WHERE Id = 168;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[155]', GroupByClassType = 1 WHERE Id = 169;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[156]', GroupByClassType = 1 WHERE Id = 170;

-- 數學班：171-194 新生×12 + 流失×12，分校自填
UPDATE Course SET StatisticsType = 50 WHERE Id BETWEEN 171 AND 194;

-- 理化班：207 依班別分開算，208 不分班別（兩者都加總「理化班」部門(30)內的原始課程）
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[30]', GroupByClassType = 1 WHERE Id = 207;
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[30]' WHERE Id = 208;

-- 理化班：209-220 本週{年級}與上週相比 ×12，SourceCourseIds = [Id-14]（對應195-206），依班別分開算
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[195]', GroupByClassType = 1 WHERE Id = 209;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[196]', GroupByClassType = 1 WHERE Id = 210;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[197]', GroupByClassType = 1 WHERE Id = 211;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[198]', GroupByClassType = 1 WHERE Id = 212;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[199]', GroupByClassType = 1 WHERE Id = 213;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[200]', GroupByClassType = 1 WHERE Id = 214;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[201]', GroupByClassType = 1 WHERE Id = 215;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[202]', GroupByClassType = 1 WHERE Id = 216;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[203]', GroupByClassType = 1 WHERE Id = 217;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[204]', GroupByClassType = 1 WHERE Id = 218;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[205]', GroupByClassType = 1 WHERE Id = 219;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[206]', GroupByClassType = 1 WHERE Id = 220;

-- 理化班：221-244 新生×12 + 流失×12，分校自填
UPDATE Course SET StatisticsType = 50 WHERE Id BETWEEN 221 AND 244;
```

- [ ] **Step 2: Write `2026-07-16-psj-aggregation-rules-revert.sql`**

```sql
-- Revert PSJ aggregation rule configuration.
-- WARNING: only run this together with reverting the Task 3 code change
-- (SumPHPopulation's PSJ branch calling AggregationEngine) — see the apply
-- script's header comment for why running one without the other is unsafe.
UPDATE Course
SET StatisticsType = NULL, SourceDepartmentIds = NULL, SourceCourseIds = NULL, GroupByClassType = 0
WHERE Id BETWEEN 157 AND 244 AND Id NOT BETWEEN 195 AND 206;
```

(The `Id NOT BETWEEN 195 AND 206` exclusion protects the raw, non-aggregate 理化班 courses — they were never touched by the apply script and must not be reset either.)

- [ ] **Step 3: Apply to dev DB and verify**

Run:

```bash
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P 'Phpcm539@6@52' -C -i source/portal/docs/superpowers/sql/2026-07-16-psj-aggregation-rules-apply.sql
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P 'Phpcm539@6@52' -C -Q "SET NOCOUNT ON; SELECT Id, StatisticsType, SourceDepartmentIds, SourceCourseIds, GroupByClassType FROM Course WHERE Id BETWEEN 157 AND 244 AND Id NOT BETWEEN 195 AND 206 ORDER BY Id;"
```

Expected: 76 rows returned (88 total in range minus the 12 untouched 195–206 raw courses), every row has non-NULL `StatisticsType`. Independently verify a handful of specific rows against the apply script's literal values — do not just trust the row count (this project's `sqlcmd -i` has silently skipped statements before; if the returned row count or any spot-checked value doesn't match, re-run the missing `UPDATE` statements individually rather than assuming success).

- [ ] **Step 4: Commit**

```bash
git add source/portal/docs/superpowers/sql/2026-07-16-psj-aggregation-rules-apply.sql \
        source/portal/docs/superpowers/sql/2026-07-16-psj-aggregation-rules-revert.sql
git commit -m "data: configure PSJ's aggregation courses with AggregationEngine rules"
```

---

### Task 2: PSJ comparison test

**Files:**
- Create: `source/portal/Test/Services/Aggregation/PsjAggregationComparisonTests.cs`

**Interfaces:**
- Consumes: `AggregationEngine` (unchanged), live dev DB rows configured by Task 1.

- [ ] **Step 1: Write the test**

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
[Explicit("Requires a live connection to the dev database; run manually to verify the PSJ AggregationEngine migration before cutover (see plan Task 2)")]
public class PsjAggregationComparisonTests {
    // 資料調查（見 docs/superpowers/specs/2026-07-16-psj-as-aggregation-migration-design.md）沒有發現任何
    // PSJ 課程需要刻意接受行為變更——28 個引擎計算課程的既有真實資料本身就已經符合新公式，理論上應該 0 落差。
    private static readonly HashSet<int> ExpectedChangeCourseIds = new();

    [Test]
    public void Engine_ReproducesStoredNumbers_ForAllRealPsjPopulations() {
        using var context = new DataContext();

        var populations = context.StudentPopulation
            .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
            .Where(p => p.Type == StudentPopulationType.PSJ && p.DataMode == DataMode.Normal)
            .ToList();

        var engine = new AggregationEngine((year, week, schoolId, type) =>
            context.StudentPopulation
                .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type));

        var unexpectedMismatches = new List<string>();
        var expectedChanges = new List<string>();
        foreach (var population in populations) {
            // 三段式（先全部算完，再全部比對，最後全部還原）：跟 PS 遷移時的做法一致，避免逐項
            // 算完立刻比對還原導致排序保證形同虛設（這次 PSJ/AS 沒有跨課程依賴鏈，但沿用同一套安全作法）。
            var sumItems = population.Items.Where(i => i.IsSum).OrderBy(i => i.Class.Course.Ordinal).ToList();
            var oldNumbers = sumItems.ToDictionary(i => i, i => i.Number);

            foreach (var item in sumItems) {
                engine.Calculate(item, population);
            }

            foreach (var item in sumItems) {
                int oldNumber = oldNumbers[item];
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
            }

            foreach (var item in sumItems) {
                item.Number = oldNumbers[item]; // 唯讀比對，還原避免誤動資料
            }
        }

        TestContext.WriteLine($"Checked {populations.Count} PSJ populations.");
        TestContext.WriteLine($"{expectedChanges.Count} expected changes:");
        foreach (var m in expectedChanges) TestContext.WriteLine(m);
        TestContext.WriteLine($"{unexpectedMismatches.Count} UNEXPECTED mismatches:");
        foreach (var m in unexpectedMismatches) TestContext.WriteLine(m);

        Assert.That(unexpectedMismatches, Is.Empty, () => string.Join("\n", unexpectedMismatches));
    }
}
```

- [ ] **Step 2: Run it**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~PsjAggregationComparisonTests"
```

Expected: 1 passed, 0 unexpected mismatches. Read the `TestContext.WriteLine` output (visible with `--logger "console;verbosity=detailed"` if not shown by default) to confirm the actual checked-population count and mismatch counts, not just the pass/fail line.

If there are unexpected mismatches: investigate the root cause (read the specific course/population/school flagged) before doing anything else. Do not add the course Id to `ExpectedChangeCourseIds` without first understanding *why* the numbers differ and reporting that reasoning for a decision — matches the Global Constraints.

- [ ] **Step 3: Commit**

```bash
git add source/portal/Test/Services/Aggregation/PsjAggregationComparisonTests.cs
git commit -m "test: add explicit PSJ AggregationEngine comparison test against real dev data"
```

---

### Task 3: PSJ cutover

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes: `aggregationEngine` (already instantiated earlier in `SumPHPopulation`, shared across GEPT/PH/PS/PSJ branches — no new instantiation needed).

- [ ] **Step 1: Wrap the old PSJ branch in `#if false` and add the engine call**

In `source/portal/Portal/Controllers/StudentPopulationController.cs`, replace the existing PSJ branch (currently):

```csharp
                    else if (studentPopulationData.Type == StudentPopulationType.PSJ) {
                        if (group.Class.Course.Name.Equals("本周數學人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("數學班") && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("本週數學總人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("數學班") && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("本周理化人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("理化班") && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("本週理化總人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("理化班") && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.IndexOf("與上週相比") >= 0) {
                            int srcId = group.Class.Course.Id - 14;
                            var src = studentPopulationData.Items.Where(e => e.Class.Course != null && e.Class.Course.Id == srcId && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).ToList();
                            group.Number = src.Sum(e => e.Number) - src.Sum(e => e.LastWeekNumber);
                        }
                        // 流失人數／新生人數：分校自填，系統不計算
                    }
```

with:

```csharp
                    else if (studentPopulationData.Type == StudentPopulationType.PSJ) {
#if false // 舊 PSJ 加總邏輯，2026-07-16 遷移到 AggregationEngine 時停用保留（不刪除），
          // 見 docs/superpowers/plans/2026-07-16-psj-as-aggregation-migration.md Task 3
                        if (group.Class.Course.Name.Equals("本周數學人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("數學班") && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("本週數學總人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("數學班") && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("本周理化人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("理化班") && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("本週理化總人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("理化班") && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.IndexOf("與上週相比") >= 0) {
                            int srcId = group.Class.Course.Id - 14;
                            var src = studentPopulationData.Items.Where(e => e.Class.Course != null && e.Class.Course.Id == srcId && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).ToList();
                            group.Number = src.Sum(e => e.Number) - src.Sum(e => e.LastWeekNumber);
                        }
                        // 流失人數／新生人數：分校自填，系統不計算
#endif
                        aggregationEngine.Calculate(group, studentPopulationData);
                    }
```

- [ ] **Step 2: Build**

```bash
dotnet build source/portal/PHStatistics.portal.sln
```

Expected: `0 Error(s)`.

- [ ] **Step 3: Re-run Task 2's test to confirm the cutover doesn't change the result**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~PsjAggregationComparisonTests"
```

Expected: still 1 passed, 0 unexpected mismatches (this step doesn't call `SumPHPopulation` — it calls the engine directly, same as Task 2 — but re-running confirms nothing regressed from the source edit itself, e.g. a stray syntax issue inside the `#if false` block).

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "refactor: migrate PSJ aggregation to the shared AggregationEngine (old code disabled, not deleted)"
```

---

### Task 4: AS SQL rule configuration — apply to dev DB

**Files:**
- Create: `source/portal/docs/superpowers/sql/2026-07-16-as-aggregation-rules-apply.sql`
- Create: `source/portal/docs/superpowers/sql/2026-07-16-as-aggregation-rules-revert.sql`

**Interfaces:**
- Produces: live dev DB state (`Course.StatisticsType`/`SourceDepartmentIds`/`SourceCourseIds`/`GroupByClassType` set on Course Ids 257–294 and 307–344) that Task 5's comparison test reads.

- [ ] **Step 1: Write `2026-07-16-as-aggregation-rules-apply.sql`**

```sql
-- AfterSchool/AS (StudentPopulationType.AfterSchool, Type=4) aggregation rule configuration.
-- See docs/superpowers/specs/2026-07-16-psj-as-aggregation-migration-design.md and
-- docs/superpowers/plans/2026-07-16-psj-as-aggregation-migration.md Task 6 for the full
-- mapping table and rationale.
--
-- IMPORTANT: if this script is ever reverted (see the paired revert script), Task 6's code
-- cutover (StudentPopulationController.cs, SumPHPopulation AS branch calling AggregationEngine)
-- MUST be reverted at the same time — see the PSJ apply script's header comment for why.
--
-- NOTE: GroupByClassType is deliberately 0 (false) for every rule below, including the two
-- "合計"/"總合計" pairs (257/258 and 307/308) — this is a faithful 1:1 preservation of the
-- existing code's behavior (which never split AS courses by ClassType, even for 英文班's real
-- EP/EG distinction), confirmed with the user during spec review. Do not change this to 1.

-- 安親課輔班：257「合計」與258「總合計」在舊碼裡完全同義（皆不分班別），照實搬
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[33]' WHERE Id = 257;
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[33]' WHERE Id = 258;

-- 安親課輔班：259-270 本週{年級}與上週相比 ×12，SourceCourseIds = [Id-14]（對應245-256）
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[245]' WHERE Id = 259;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[246]' WHERE Id = 260;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[247]' WHERE Id = 261;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[248]' WHERE Id = 262;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[249]' WHERE Id = 263;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[250]' WHERE Id = 264;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[251]' WHERE Id = 265;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[252]' WHERE Id = 266;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[253]' WHERE Id = 267;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[254]' WHERE Id = 268;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[255]' WHERE Id = 269;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[256]' WHERE Id = 270;

-- 安親課輔班：271-294 新生×12 + 流失×12，分校自填
UPDATE Course SET StatisticsType = 50 WHERE Id BETWEEN 271 AND 294;

-- 英文班：307「合計」與308「總合計」在舊碼裡完全同義（皆不分班別，EP+EG合併），照實搬
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[36]' WHERE Id = 307;
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[36]' WHERE Id = 308;

-- 英文班：309-320 本週{年級}與上週相比 ×12，SourceCourseIds = [Id-14]（對應295-306）
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[295]' WHERE Id = 309;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[296]' WHERE Id = 310;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[297]' WHERE Id = 311;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[298]' WHERE Id = 312;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[299]' WHERE Id = 313;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[300]' WHERE Id = 314;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[301]' WHERE Id = 315;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[302]' WHERE Id = 316;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[303]' WHERE Id = 317;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[304]' WHERE Id = 318;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[305]' WHERE Id = 319;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[306]' WHERE Id = 320;

-- 英文班：321-344 新生×12 + 流失×12，分校自填
UPDATE Course SET StatisticsType = 50 WHERE Id BETWEEN 321 AND 344;
```

- [ ] **Step 2: Write `2026-07-16-as-aggregation-rules-revert.sql`**

```sql
-- Revert AS aggregation rule configuration.
-- WARNING: only run this together with reverting the Task 6 code change
-- (SumPHPopulation's AS branch calling AggregationEngine) — see the apply
-- script's header comment for why running one without the other is unsafe.
UPDATE Course
SET StatisticsType = NULL, SourceDepartmentIds = NULL, SourceCourseIds = NULL, GroupByClassType = 0
WHERE Id BETWEEN 257 AND 344 AND Id NOT BETWEEN 295 AND 306;
```

(The `Id NOT BETWEEN 295 AND 306` exclusion protects the raw, non-aggregate 英文班 courses.)

- [ ] **Step 3: Apply to dev DB and verify**

```bash
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P 'Phpcm539@6@52' -C -i source/portal/docs/superpowers/sql/2026-07-16-as-aggregation-rules-apply.sql
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P 'Phpcm539@6@52' -C -Q "SET NOCOUNT ON; SELECT Id, StatisticsType, SourceDepartmentIds, SourceCourseIds, GroupByClassType FROM Course WHERE Id BETWEEN 257 AND 344 AND Id NOT BETWEEN 295 AND 306 ORDER BY Id;"
```

Expected: 76 rows returned (88 total in range minus the 12 untouched 295–306 raw courses), every row has non-NULL `StatisticsType`. Spot-check specific rows against the apply script's literal values rather than trusting the row count alone.

- [ ] **Step 4: Commit**

```bash
git add source/portal/docs/superpowers/sql/2026-07-16-as-aggregation-rules-apply.sql \
        source/portal/docs/superpowers/sql/2026-07-16-as-aggregation-rules-revert.sql
git commit -m "data: configure AS's aggregation courses with AggregationEngine rules"
```

---

### Task 5: AS comparison test

**Files:**
- Create: `source/portal/Test/Services/Aggregation/AsAggregationComparisonTests.cs`

**Interfaces:**
- Consumes: `AggregationEngine` (unchanged), live dev DB rows configured by Task 4.

- [ ] **Step 1: Write the test**

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
[Explicit("Requires a live connection to the dev database; run manually to verify the AS AggregationEngine migration before cutover (see plan Task 5)")]
public class AsAggregationComparisonTests {
    // 資料調查（見 docs/superpowers/specs/2026-07-16-psj-as-aggregation-migration-design.md）沒有發現任何
    // AS 課程需要刻意接受行為變更——28 個引擎計算課程的既有真實資料本身就已經符合新公式，理論上應該 0 落差。
    private static readonly HashSet<int> ExpectedChangeCourseIds = new();

    [Test]
    public void Engine_ReproducesStoredNumbers_ForAllRealAsPopulations() {
        using var context = new DataContext();

        var populations = context.StudentPopulation
            .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
            .Where(p => p.Type == StudentPopulationType.AfterSchool && p.DataMode == DataMode.Normal)
            .ToList();

        var engine = new AggregationEngine((year, week, schoolId, type) =>
            context.StudentPopulation
                .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type));

        var unexpectedMismatches = new List<string>();
        var expectedChanges = new List<string>();
        foreach (var population in populations) {
            var sumItems = population.Items.Where(i => i.IsSum).OrderBy(i => i.Class.Course.Ordinal).ToList();
            var oldNumbers = sumItems.ToDictionary(i => i, i => i.Number);

            foreach (var item in sumItems) {
                engine.Calculate(item, population);
            }

            foreach (var item in sumItems) {
                int oldNumber = oldNumbers[item];
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
            }

            foreach (var item in sumItems) {
                item.Number = oldNumbers[item]; // 唯讀比對，還原避免誤動資料
            }
        }

        TestContext.WriteLine($"Checked {populations.Count} AS populations.");
        TestContext.WriteLine($"{expectedChanges.Count} expected changes:");
        foreach (var m in expectedChanges) TestContext.WriteLine(m);
        TestContext.WriteLine($"{unexpectedMismatches.Count} UNEXPECTED mismatches:");
        foreach (var m in unexpectedMismatches) TestContext.WriteLine(m);

        Assert.That(unexpectedMismatches, Is.Empty, () => string.Join("\n", unexpectedMismatches));
    }
}
```

- [ ] **Step 2: Run it**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AsAggregationComparisonTests"
```

Expected: 1 passed, 0 unexpected mismatches. Pay particular attention to courses 257/258 and 307/308 in the output — confirm they show identical `engine=` values within each pair (proving the `GroupByClassType=false` configuration reproduces the old "both compute the same total" quirk documented in the spec), not because the test asserts this directly, but as a sanity check while reading the `TestContext.WriteLine` output.

If there are unexpected mismatches: investigate before doing anything else, per the Global Constraints — do not silently expand `ExpectedChangeCourseIds`.

- [ ] **Step 3: Commit**

```bash
git add source/portal/Test/Services/Aggregation/AsAggregationComparisonTests.cs
git commit -m "test: add explicit AS AggregationEngine comparison test against real dev data"
```

---

### Task 6: AS cutover

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes: `aggregationEngine` (same shared instance used by Task 3's PSJ branch and the existing GEPT/PH/PS branches).

- [ ] **Step 1: Wrap the old AS branch in `#if false` and add the engine call**

Replace the existing AS branch (currently):

```csharp
                    else if (studentPopulationData.Type == StudentPopulationType.AfterSchool) {
                        int asDeptId = group.Class.Course.Department.Id;
                        string asCourseName = group.Class.Course.Name;
                        if (asDeptId == 34) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == 33 && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (asDeptId == 37) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == 36 && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (asDeptId == 35 || asDeptId == 38) {
                            if (asCourseName.IndexOf("與上週相比") >= 0) {
                                int srcId = group.Class.Course.Id - 14;
                                var src = studentPopulationData.Items.Where(e => e.Class.Course != null && e.Class.Course.Id == srcId && !e.Class.Course.IsSum).ToList();
                                group.Number = src.Sum(e => e.Number) - src.Sum(e => e.LastWeekNumber);
                            }
                            // 新生人數／流失人數：分校自填，系統不計算
                        }
                    }
```

with:

```csharp
                    else if (studentPopulationData.Type == StudentPopulationType.AfterSchool) {
#if false // 舊 AS 加總邏輯，2026-07-16 遷移到 AggregationEngine 時停用保留（不刪除），
          // 見 docs/superpowers/plans/2026-07-16-psj-as-aggregation-migration.md Task 6
                        int asDeptId = group.Class.Course.Department.Id;
                        string asCourseName = group.Class.Course.Name;
                        if (asDeptId == 34) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == 33 && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (asDeptId == 37) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == 36 && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (asDeptId == 35 || asDeptId == 38) {
                            if (asCourseName.IndexOf("與上週相比") >= 0) {
                                int srcId = group.Class.Course.Id - 14;
                                var src = studentPopulationData.Items.Where(e => e.Class.Course != null && e.Class.Course.Id == srcId && !e.Class.Course.IsSum).ToList();
                                group.Number = src.Sum(e => e.Number) - src.Sum(e => e.LastWeekNumber);
                            }
                            // 新生人數／流失人數：分校自填，系統不計算
                        }
#endif
                        aggregationEngine.Calculate(group, studentPopulationData);
                    }
```

- [ ] **Step 2: Build**

```bash
dotnet build source/portal/PHStatistics.portal.sln
```

Expected: `0 Error(s)`.

- [ ] **Step 3: Re-run Task 5's test to confirm the cutover doesn't change the result**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AsAggregationComparisonTests"
```

Expected: still 1 passed, 0 unexpected mismatches.

- [ ] **Step 4: Run the full test suite once to confirm no cross-type regression**

```bash
dotnet test source/portal/Test/Test.csproj
```

Expected: all non-`[Explicit]` tests pass; the pre-existing Selenium `Home` test failure (environmental, no browser available) is the only expected failure, matching every prior task's baseline in this project.

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "refactor: migrate AS aggregation to the shared AggregationEngine (old code disabled, not deleted)"
```

---

## Out of scope (per spec)

- PSJ CKC's own analysis columns — no courses exist for them, nothing to migrate.
- Raw (non-aggregate) courses 145–156, 195–206, 245–256, 295–306 — not `IsSum`, untouched by this plan.
- Changing AS's `GroupByClassType=false` behavior to split by ClassType — confirmed with the user as a deliberate non-goal this round.
- Deleting the `#if false` blocks — a future cleanup task, gated on browser verification, per established project convention.
- Browser/manual verification — deferred to the project's usual consolidated verification pass.
