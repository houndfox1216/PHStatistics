# PSJ（百倍速）匯入改寫 + CKC 自立自學班支援 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rewrite `PSJPopulationImporter` to actually read real data from the 北區/南區 tabs (the current implementation reads the wrong sheet and has never written a single real row to the database), and add 33 new CKC自立自學班 courses so their historical Excel data can be imported.

**Architecture:** `IPopulationImporter.Scan`/`Import` gain two new optional parameters (`overrideYear`, `overrideWeek`) that PSJ requires and every other importer ignores. `PSJPopulationImporter` finds school blocks via NPOI merged-cell regions (not text carry-forward, because some blocks are unlabeled placeholder rows with no visible school name), then reads a fixed, hardcoded column layout per block (north vs. south, left vs. right +20 offset). New `Course`/`CourseDepartment` rows for CKC are added via a plain SQL script (matching the existing GEPT/PH/PS convention — this project doesn't use EF migrations for course data).

**Tech Stack:** ASP.NET Core 8 / EF Core 8, NPOI (XSSFWorkbook, `ISheet.MergedRegions`), NUnit 4 (`[Explicit]` integration tests against the live dev DB — this codebase has no DI seam for mocking `DataContext`, so import-logic tests are integration tests, not unit tests, by established convention).

## Global Constraints

- Existing `Course` max `Id` = 344, existing `CourseDepartment` max `Id` = 38 (verified live against the dev DB during spec review — do not re-derive, use directly).
- New CKC `Course` rows use `Id` 345–377 (English 345–355, Chinese 356–366, Math 367–377, each 11 rows for grades 二年級–高三 in that order). New `CourseDepartment` row uses `Id` 39, `Name` = `CKC自立自學班`.
- `ClassType` mapping: CKC 加上 → `ClassType.Group`, CKC 單上 → `ClassType.Personal`. This is intentionally a different pair of enum values than the existing 數學班/理化班 convention (`Personal`/`SubGroup`) — do not unify them, they are different semantic axes (see spec §"ClassType enum 實際定義").
- CKC 分析/合計 columns (新生/流失/上週比/總人數) are **out of scope** — do not create courses or read data for them.
- PSJ 加總抽離 (courses 157–244) is **out of scope** — do not touch `SumPHPopulation`'s PSJ branch.
- AS (課輔) importer rewrite is **out of scope**.
- Browser testing of the admin import page is **out of scope** for this plan (deferred to the project's usual end-of-cycle browser verification pass), except where a step below explicitly calls for a build check.
- Spec: `source/portal/docs/superpowers/specs/2026-07-16-psj-ckc-import-design.md`.

---

### Task 1: `IPopulationImporter` gains `overrideYear`/`overrideWeek`; wire through service + 4 unaffected importers

**Files:**
- Modify: `source/portal/Portal/Services/Import/IPopulationImporter.cs`
- Modify: `source/portal/Portal/Services/Import/PopulationImportService.cs`
- Modify: `source/portal/Portal/Services/Import/PHPopulationImporter.cs`
- Modify: `source/portal/Portal/Services/Import/GeptPopulationImporter.cs`
- Modify: `source/portal/Portal/Services/Import/PSPopulationImporter.cs`
- Modify: `source/portal/Portal/Services/Import/ASPopulationImporter.cs`

**Interfaces:**
- Produces: `IPopulationImporter.Scan(DataContext db, Stream fileStream, int? overrideYear = null, int? overrideWeek = null)` and `IPopulationImporter.Import(DataContext db, Stream fileStream, ILogger logger, int? overrideYear = null, int? overrideWeek = null)` — every later task's `PSJPopulationImporter` implementation and `PopulationImportService` call site depends on this exact signature.

This is a pure signature change — no behavior changes in PH/GEPT/PS/AS (they simply ignore the two new parameters). There is nothing here to unit-test in isolation; the verification is that the solution still builds and the existing (unrelated) test suite still passes.

- [ ] **Step 1: Update the interface**

Edit `source/portal/Portal/Services/Import/IPopulationImporter.cs` to:

```csharp
using System.IO;
using Microsoft.Extensions.Logging;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import;

public interface IPopulationImporter {
    StudentPopulationType Type { get; }
    ImportScanResult Scan(DataContext db, Stream fileStream, int? overrideYear = null, int? overrideWeek = null);
    ImportResult Import(DataContext db, Stream fileStream, ILogger logger, int? overrideYear = null, int? overrideWeek = null);
}
```

- [ ] **Step 2: Update `PopulationImportService` to pass the parameters through**

In `source/portal/Portal/Services/Import/PopulationImportService.cs`, replace the `Scan` and `Import` methods (lines 25–45) with:

```csharp
    public ImportScanResult Scan(DataContext db, StudentPopulationType type, Stream fileStream, int? overrideYear = null, int? overrideWeek = null) {
        if (!_importers.TryGetValue(type, out var importer)) {
            var r = new ImportScanResult();
            r.Errors.Add($"不支援的匯入類型: {type}");
            return r;
        }
        return importer.Scan(db, fileStream, overrideYear, overrideWeek);
    }

    public ImportResult Import(DataContext db, StudentPopulationType type, Stream fileStream, string fileName, int? overrideYear = null, int? overrideWeek = null) {
        if (!_importers.TryGetValue(type, out var importer))
            return new ImportResult { Type = type.ToString(), File = fileName, Errors = { $"不支援的匯入類型: {type}" } };

        var result = importer.Import(db, fileStream, _logger, overrideYear, overrideWeek);
        result.File = fileName;

        foreach (long popId in result.PopulationIds) {
            CorrectLastWeekNumbers(db, popId);
        }
        return result;
    }
```

(Leave `CorrectLastWeekNumbers` and the constructor untouched.)

- [ ] **Step 3: Update the 4 unaffected importers' signatures**

In each of `PHPopulationImporter.cs`, `GeptPopulationImporter.cs`, `PSPopulationImporter.cs`, `ASPopulationImporter.cs`, change only the method signatures (body unchanged):

```csharp
    public ImportScanResult Scan(DataContext db, Stream fileStream, int? overrideYear = null, int? overrideWeek = null) {
```

```csharp
    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger, int? overrideYear = null, int? overrideWeek = null) {
```

(Apply to all 8 method declarations across the 4 files — 2 per file. Do not touch anything inside the method bodies.)

- [ ] **Step 4: Build and confirm 0 errors**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: `Build succeeded. 0 Error(s)` (the old `PSJPopulationImporter.cs` will also need its signature updated to compile — do that now too, as a mechanical copy of Step 3's pattern, since Task 4 will rewrite its body anyway):

```csharp
    public ImportScanResult Scan(DataContext db, Stream fileStream, int? overrideYear = null, int? overrideWeek = null) {
```
```csharp
    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger, int? overrideYear = null, int? overrideWeek = null) {
```

- [ ] **Step 5: Run the existing test suite to confirm no regression**

Run: `dotnet test source/portal/Test/Test.csproj`
Expected: all tests pass, with the existing `[Explicit(...)]`-tagged fixtures reported as skipped (NUnit's default behavior — `[Explicit]` tests only run when selected by name, unlike `[Category]`-based filtering). This change touches no logic, only signatures, so nothing should regress.

- [ ] **Step 6: Commit**

```bash
git add source/portal/Portal/Services/Import/IPopulationImporter.cs \
        source/portal/Portal/Services/Import/PopulationImportService.cs \
        source/portal/Portal/Services/Import/PHPopulationImporter.cs \
        source/portal/Portal/Services/Import/GeptPopulationImporter.cs \
        source/portal/Portal/Services/Import/PSPopulationImporter.cs \
        source/portal/Portal/Services/Import/ASPopulationImporter.cs \
        source/portal/Portal/Services/Import/PSJPopulationImporter.cs
git commit -m "feat: add overrideYear/overrideWeek to IPopulationImporter for PSJ"
```

---

### Task 2: `CourseMapping` additions — CKC grade order, CKC course IDs, school-name alias

**Files:**
- Modify: `source/portal/Portal/Services/Import/ImportSupport/CourseMapping.cs`
- Create: `source/portal/Test/Services/Import/CourseMappingPsjCkcTests.cs`

**Interfaces:**
- Produces: `CourseMapping.PsjGradeOrder` (`string[]`, 11 entries, 二年級–高三, no 一年級), `CourseMapping.CkcCourseIds` (`Dictionary<string, int[]>` keyed `"CKC_E"`/`"CKC_C"`/`"CKC_M"`, each `int[11]`), `CourseMapping.PsjSchoolNameAliases` (`Dictionary<string,string>`) — Task 5 (`PSJPopulationImporter.Import`) consumes all three.

While investigating the real Excel file (`（百倍速）人數統計表更新版115.6.13).xlsx`) against the live dev DB `School` table, we found the 南區 tab's right-half school name `高美` does not exact-match the DB's `School.Name` = `高美館` — under the existing exact-match lookup convention used by every importer, that school's entire week would silently import as zero rows. `PsjSchoolNameAliases` fixes this with a fallback lookup, the same pattern `ASPopulationImporter` already uses via `CourseMapping.AsChineseNumerals` for its own school-name mismatches.

- [ ] **Step 1: Write the failing test**

Create `source/portal/Test/Services/Import/CourseMappingPsjCkcTests.cs`:

```csharp
using System.Linq;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
public class CourseMappingPsjCkcTests {
    [Test]
    public void PsjGradeOrder_Has11GradesStartingAtGrade2NoGrade1() {
        Assert.That(CourseMapping.PsjGradeOrder.Length, Is.EqualTo(11));
        Assert.That(CourseMapping.PsjGradeOrder[0], Is.EqualTo("二年級"));
        Assert.That(CourseMapping.PsjGradeOrder[10], Is.EqualTo("高三"));
        Assert.That(CourseMapping.PsjGradeOrder, Does.Not.Contain("一年級"));
    }

    [Test]
    public void CkcCourseIds_HasThreeSubjectsWith11ContiguousIdsEach() {
        Assert.That(CourseMapping.CkcCourseIds["CKC_E"], Is.EqualTo(Enumerable.Range(345, 11).ToArray()));
        Assert.That(CourseMapping.CkcCourseIds["CKC_C"], Is.EqualTo(Enumerable.Range(356, 11).ToArray()));
        Assert.That(CourseMapping.CkcCourseIds["CKC_M"], Is.EqualTo(Enumerable.Range(367, 11).ToArray()));
    }

    [Test]
    public void PsjSchoolNameAliases_MapsGaomeiToGaomeiguan() {
        Assert.That(CourseMapping.PsjSchoolNameAliases["高美"], Is.EqualTo("高美館"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~CourseMappingPsjCkcTests"`
Expected: compile error (`CourseMapping.PsjGradeOrder`/`CkcCourseIds`/`PsjSchoolNameAliases` do not exist yet).

- [ ] **Step 3: Add the three members to `CourseMapping.cs`**

In `source/portal/Portal/Services/Import/ImportSupport/CourseMapping.cs`, add immediately after the existing `PsjCourseIds` dictionary (after line 23):

```csharp
    // PSJ/CKC 專用年級順序：11 個年級，不含「一年級」（Excel 北區資料列從未出現這個年級；
    // 南區資料列雖然有「一年級」，但 CKC 三科本身就未涵蓋這個年級，查不到時呼叫端會直接跳過該欄）
    public static readonly string[] PsjGradeOrder = {
        "二年級","三年級","四年級","五年級","六年級",
        "國一","國二","國三","高一","高二","高三"
    };

    public static readonly Dictionary<string, int[]> CkcCourseIds = new() {
        ["CKC_E"] = new[]{345,346,347,348,349,350,351,352,353,354,355},
        ["CKC_C"] = new[]{356,357,358,359,360,361,362,363,364,365,366},
        ["CKC_M"] = new[]{367,368,369,370,371,372,373,374,375,376,377},
    };

    // 南區右半頁籤分校名寫「高美」，但資料庫 School.Name 是「高美館」，精確比對會找不到分校
    // 而靜默漏掉整週資料——比照 AsChineseNumerals 的 fallback 慣例修正。
    public static readonly Dictionary<string, string> PsjSchoolNameAliases = new() {
        ["高美"] = "高美館",
    };
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~CourseMappingPsjCkcTests"`
Expected: 3 passed, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Services/Import/ImportSupport/CourseMapping.cs \
        source/portal/Test/Services/Import/CourseMappingPsjCkcTests.cs
git commit -m "feat: add PSJ/CKC grade order, course ID map, and school name alias to CourseMapping"
```

---

### Task 3: SQL scripts — create CKC `CourseDepartment` + 33 `Course` rows

**Files:**
- Create: `source/portal/docs/superpowers/sql/2026-07-16-psj-ckc-course-apply.sql`
- Create: `source/portal/docs/superpowers/sql/2026-07-16-psj-ckc-course-revert.sql`

These scripts are run manually against the dev/prod DB (this project doesn't use EF migrations for course/department seed data — see the existing `docs/superpowers/sql/2026-07-15-*.sql` scripts for the established convention). Verified live against the dev DB during spec investigation: `Course` max `Id` = 344, `CourseDepartment` max `Id` = 38, `Course.DataMode`/`CourseDepartment.DataMode` = `0` (Normal) on all existing rows, and existing 數學班/理化班 non-aggregate courses use `ClassType = 'EM1、團'` as the free-text "適用班別" description (e.g. `Course.Id=145` "一年級": `DepartmentId=27, IsSum=0, Ordinal=144, Type=1, ClassType='EM1、團', Published=1, DataMode=0`) and `Ordinal = Id - 1` (also true of `CourseDepartment`: `Id=27` → `Ordinal=26`).

- [ ] **Step 1: Write `2026-07-16-psj-ckc-course-apply.sql`**

```sql
-- Create the CKC自立自學班 CourseDepartment and its 33 Course rows (English/Chinese/Math × 11 grades).
-- See docs/superpowers/specs/2026-07-16-psj-ckc-import-design.md ("決策摘要" + "詳細設計" §3) for the full rationale.
-- Verified live against the dev DB before writing this script: Course max Id = 344, CourseDepartment max Id = 38.

INSERT INTO CourseDepartment (Id, Name, Ordinal, IsSum, Subject, Company, Type, Published, DataMode, CreatedTime, UpdatedTime)
VALUES (39, N'CKC自立自學班', 38, 0, NULL, NULL, 1, 1, 0, GETDATE(), GETDATE());

-- 英文 345-355, 國文 356-366, 數學 367-377 — 每科11筆，年級順序 二年級,三年級,四年級,五年級,六年級,國一,國二,國三,高一,高二,高三
-- ClassType 欄位比照既有數學班/理化班非加總課程慣例，填入 enum 值的顯示名稱說明文字（純供 Admin 後台人看）。
INSERT INTO Course (Id, Name, DepartmentId, IsSum, Ordinal, Type, ClassType, Published, StatisticsType, SourceDepartmentIds, SourceCourseIds, GroupByClassType, ApplicableClassType, SourceSubject, DataMode, CreatedTime, UpdatedTime)
VALUES
(345, N'CKC英文-二年級', 39, 0, 344, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(346, N'CKC英文-三年級', 39, 0, 345, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(347, N'CKC英文-四年級', 39, 0, 346, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(348, N'CKC英文-五年級', 39, 0, 347, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(349, N'CKC英文-六年級', 39, 0, 348, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(350, N'CKC英文-國一',   39, 0, 349, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(351, N'CKC英文-國二',   39, 0, 350, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(352, N'CKC英文-國三',   39, 0, 351, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(353, N'CKC英文-高一',   39, 0, 352, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(354, N'CKC英文-高二',   39, 0, 353, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(355, N'CKC英文-高三',   39, 0, 354, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(356, N'CKC國文-二年級', 39, 0, 355, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(357, N'CKC國文-三年級', 39, 0, 356, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(358, N'CKC國文-四年級', 39, 0, 357, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(359, N'CKC國文-五年級', 39, 0, 358, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(360, N'CKC國文-六年級', 39, 0, 359, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(361, N'CKC國文-國一',   39, 0, 360, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(362, N'CKC國文-國二',   39, 0, 361, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(363, N'CKC國文-國三',   39, 0, 362, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(364, N'CKC國文-高一',   39, 0, 363, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(365, N'CKC國文-高二',   39, 0, 364, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(366, N'CKC國文-高三',   39, 0, 365, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(367, N'CKC數學-二年級', 39, 0, 366, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(368, N'CKC數學-三年級', 39, 0, 367, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(369, N'CKC數學-四年級', 39, 0, 368, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(370, N'CKC數學-五年級', 39, 0, 369, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(371, N'CKC數學-六年級', 39, 0, 370, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(372, N'CKC數學-國一',   39, 0, 371, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(373, N'CKC數學-國二',   39, 0, 372, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(374, N'CKC數學-國三',   39, 0, 373, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(375, N'CKC數學-高一',   39, 0, 374, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(376, N'CKC數學-高二',   39, 0, 375, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(377, N'CKC數學-高三',   39, 0, 376, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE());
```

- [ ] **Step 2: Write `2026-07-16-psj-ckc-course-revert.sql`**

```sql
-- Revert: remove the CKC CourseDepartment + 33 Course rows added by
-- 2026-07-16-psj-ckc-course-apply.sql.
-- WARNING: only safe if no StudentPopulationItem/Class rows reference Course.Id 345-377 yet
-- (i.e. before any real PSJ import has run against the new courses). If PSJ CKC data has
-- already been imported, delete those Class/StudentPopulationItem rows first or this will
-- fail on the FK constraint.
DELETE FROM Course WHERE Id BETWEEN 345 AND 377;
DELETE FROM CourseDepartment WHERE Id = 39;
```

- [ ] **Step 3: Apply the script to the dev DB and verify**

Run (dev DB connection string is in `source/portal/Portal/appsettings.json`):

```bash
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P 'Phpcm539@6@52' -C -i source/portal/docs/superpowers/sql/2026-07-16-psj-ckc-course-apply.sql
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P 'Phpcm539@6@52' -C -Q "SET NOCOUNT ON; SELECT COUNT(*) AS CourseCount FROM Course WHERE Id BETWEEN 345 AND 377; SELECT COUNT(*) AS DeptCount FROM CourseDepartment WHERE Id = 39;"
```

Expected: `CourseCount` = 33, `DeptCount` = 1.

- [ ] **Step 4: Commit**

```bash
git add source/portal/docs/superpowers/sql/2026-07-16-psj-ckc-course-apply.sql \
        source/portal/docs/superpowers/sql/2026-07-16-psj-ckc-course-revert.sql
git commit -m "feat: add SQL script creating CKC自立自學班 CourseDepartment and 33 Course rows"
```

---

### Task 4: `PSJPopulationImporter` — merged-region block detection + rewritten `Scan`

**Files:**
- Modify: `source/portal/Portal/Services/Import/PSJPopulationImporter.cs` (full rewrite of the class body; interface members already match Task 1)

**Interfaces:**
- Consumes: `IPopulationImporter` (Task 1), `CourseMapping.PsjGradeOrder`/`CkcCourseIds`/`PsjSchoolNameAliases` (Task 2), `CourseMapping.GradeOrder`/`PsjCourseIds`/`ReadCellNumber` (pre-existing), `PopulationWriteHelper.GetOrCreatePopulation`/`AddClassAndItem` (pre-existing).
- Produces: `internal` `ColumnLayout` class with static `North`/`SouthLeft` instances and a `Shift(int offset)` method; `internal static List<(string SchoolName, int FirstRow, int LastRow)> FindSchoolBlocks(ISheet sheet, int nameColOneBased)`; `internal static School ResolveSchool(DataContext db, string schoolName)` — Task 5's `Import` rewrite and Task 7's test both depend on these exact names/shapes.

Why merged regions instead of the text-carry-forward technique `PHSheetReader` uses: the real file has placeholder blocks in both 北區 (rows 41–52) and 南區 (several blocks) where the school-name cell is merged but genuinely blank (not just "same as the row above"). Reading `ISheet.MergedRegions` and taking each region's own top-left cell value handles this correctly — a blank/no-region-value block resolves to `School == null` and is skipped, without accidentally attributing its rows to whichever school happened to appear earlier in the sheet.

This class needs `PHStatistics.Portal.Test` to be able to see `internal` members. Add that first.

- [ ] **Step 1: Make `Portal` internals visible to the test project**

Create `source/portal/Portal/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PHStatistics.Portal.Test")]
```

- [ ] **Step 2: Build to confirm the new file doesn't break anything**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 3: Rewrite `PSJPopulationImporter.cs` — layout, block-finding, school resolution, and `Scan`**

Replace the entire contents of `source/portal/Portal/Services/Import/PSJPopulationImporter.cs` with:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Services.Import;

public class PSJPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.PSJ;

    // 欄位配置：1-based（跟設計文件的人類可讀欄號一致），實際讀 Cell 時各處自行 -1。
    // 北區只有 4 個數學班小組班子欄（第一~第四班），南區有 5 個（第一~第五班），
    // 導致南區理化班/分析欄位往後多推 1 格——這是兩份頁籤唯一的欄位配置差異。
    internal sealed class ColumnLayout {
        public int NameCol, GradeCol;
        public int CkcEnglishGroupCol, CkcEnglishPersonalCol;
        public int CkcChineseGroupCol, CkcChinesePersonalCol;
        public int CkcMathGroupCol, CkcMathPersonalCol;
        public int MathPersonalCol;
        public int[] MathGroupCols;
        public int SciencePersonalCol, ScienceGroupCol;

        public ColumnLayout Shift(int offset) => new ColumnLayout {
            NameCol = NameCol + offset, GradeCol = GradeCol + offset,
            CkcEnglishGroupCol = CkcEnglishGroupCol + offset, CkcEnglishPersonalCol = CkcEnglishPersonalCol + offset,
            CkcChineseGroupCol = CkcChineseGroupCol + offset, CkcChinesePersonalCol = CkcChinesePersonalCol + offset,
            CkcMathGroupCol = CkcMathGroupCol + offset, CkcMathPersonalCol = CkcMathPersonalCol + offset,
            MathPersonalCol = MathPersonalCol + offset,
            MathGroupCols = MathGroupCols.Select(c => c + offset).ToArray(),
            SciencePersonalCol = SciencePersonalCol + offset, ScienceGroupCol = ScienceGroupCol + offset,
        };

        public static readonly ColumnLayout North = new ColumnLayout {
            NameCol = 1, GradeCol = 2,
            CkcEnglishGroupCol = 3, CkcEnglishPersonalCol = 4,
            CkcChineseGroupCol = 5, CkcChinesePersonalCol = 6,
            CkcMathGroupCol = 7, CkcMathPersonalCol = 8,
            MathPersonalCol = 9, MathGroupCols = new[] { 10, 11, 12, 13 },
            SciencePersonalCol = 14, ScienceGroupCol = 15,
        };

        public static readonly ColumnLayout SouthLeft = new ColumnLayout {
            NameCol = 1, GradeCol = 2,
            CkcEnglishGroupCol = 3, CkcEnglishPersonalCol = 4,
            CkcChineseGroupCol = 5, CkcChinesePersonalCol = 6,
            CkcMathGroupCol = 7, CkcMathPersonalCol = 8,
            MathPersonalCol = 9, MathGroupCols = new[] { 10, 11, 12, 13, 14 },
            SciencePersonalCol = 15, ScienceGroupCol = 16,
        };
    }

    // 用合併儲存格範圍找分校名區塊，而不是文字向下延伸（carry-forward）：
    // 真實檔案裡有些區塊（例如北區 rows 41-52）合併儲存格本身就是空白（沒有分校名，是預留格），
    // carry-forward 會誤把它當成上一個分校的延伸列；讀合併區域自己的值則會正確得到空字串並被跳過。
    internal static List<(string SchoolName, int FirstRow, int LastRow)> FindSchoolBlocks(ISheet sheet, int nameColOneBased) {
        int zeroBasedCol = nameColOneBased - 1;
        var blocks = new List<(string, int, int)>();
        for (int i = 0; i < sheet.NumMergedRegions; i++) {
            CellRangeAddress region = sheet.GetMergedRegion(i);
            if (region.FirstColumn != zeroBasedCol || region.FirstRow < 4) continue;
            string name = sheet.GetRow(region.FirstRow)?.GetCell(zeroBasedCol)?.ToString()?.Trim() ?? "";
            blocks.Add((name, region.FirstRow, region.LastRow));
        }
        return blocks.OrderBy(b => b.Item2).ToList();
    }

    internal static School ResolveSchool(DataContext db, string schoolName) {
        if (string.IsNullOrEmpty(schoolName) || schoolName == "總計") return null;
        School school = db.School.FirstOrDefault(e => e.Name == schoolName);
        if (school == null && CourseMapping.PsjSchoolNameAliases.TryGetValue(schoolName, out string alias))
            school = db.School.FirstOrDefault(e => e.Name == alias);
        return school;
    }

    public ImportScanResult Scan(DataContext db, Stream fileStream, int? overrideYear = null, int? overrideWeek = null) {
        var result = new ImportScanResult();
        if (!overrideYear.HasValue || overrideYear.Value <= 0 || !overrideWeek.HasValue || overrideWeek.Value <= 0) {
            result.Errors.Add("PSJ 匯入必須指定學年度與週次");
            return result;
        }
        int yearInt = overrideYear.Value, weekInt = overrideWeek.Value;

        var wb = new XSSFWorkbook(fileStream);
        ISheet north = wb.GetSheet("北區");
        ISheet south = wb.GetSheet("南區");
        if (north == null || south == null) {
            result.Errors.Add("找不到「北區」或「南區」頁籤");
            return result;
        }

        void ScanSide(ISheet sheet, int nameCol) {
            foreach (var block in FindSchoolBlocks(sheet, nameCol)) {
                School school = ResolveSchool(db, block.SchoolName);
                if (school == null) continue;
                bool exists = db.StudentPopulation.Any(e =>
                    e.School.Id == school.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PSJ);
                result.Items.Add(new ImportScanItem {
                    SchoolName = block.SchoolName,
                    SchoolId = school.Id,
                    Year = yearInt,
                    Week = weekInt,
                    Exists = exists,
                });
            }
        }
        ScanSide(north, ColumnLayout.North.NameCol);
        ScanSide(south, ColumnLayout.SouthLeft.NameCol);
        ScanSide(south, ColumnLayout.SouthLeft.NameCol + 20);
        return result;
    }

    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger, int? overrideYear = null, int? overrideWeek = null) {
        var result = new ImportResult { Type = "PSJ" };
        result.Errors.Add("NOT_IMPLEMENTED_YET_TASK_5");
        return result;
    }
}
```

(The `Import` placeholder is deliberate — Task 5 replaces it. Leaving it as a hard-failing stub rather than a half-working body keeps this task's diff reviewable on its own, and nothing calls `Import` from production code paths until Task 5 lands in the same PR/branch.)

- [ ] **Step 4: Build**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 5: Write and run a block-detection test against the real file (no DB needed)**

Create `source/portal/Test/Services/Import/PsjBlockDetectionTests.cs`. This test only needs the real spreadsheet, not a database connection — it verifies `FindSchoolBlocks` and `ColumnLayout` against ground truth read directly from the file during spec investigation:

```csharp
using System.Linq;
using NPOI.XSSF.UserModel;
using PHStatistics.Portal.Services.Import;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
public class PsjBlockDetectionTests {
    // Local copy of the same real file referenced by the design spec
    // (docs/superpowers/specs/2026-07-16-psj-ckc-import-design.md). Adjust this path if your
    // machine keeps the import fixtures elsewhere.
    private const string RealFilePath = @"C:\Leo\其他\Kuri\人數表匯入A\50\（百倍速）人數統計表更新版115.6.13).xlsx";

    private static XSSFWorkbook OpenWorkbook() {
        using var fs = new System.IO.FileStream(RealFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        return new XSSFWorkbook(fs);
    }

    [Test]
    public void FindSchoolBlocks_North_FindsFourNamedSchoolsAndSkipsBlankAndTotalBlocks() {
        var wb = OpenWorkbook();
        var blocks = PSJPopulationImporter.FindSchoolBlocks(wb.GetSheet("北區"), 1);
        var namedSchools = blocks.Where(b => b.SchoolName != "" && b.SchoolName != "總計").Select(b => b.SchoolName).ToList();

        Assert.That(namedSchools, Is.EqualTo(new[] { "南京", "內湖", "敦南", "東湖" }));
        // rows are 0-based NPOI indices; 南京's merged region is Excel A5:A16 → NPOI rows 4-15
        var nanjing = blocks.First(b => b.SchoolName == "南京");
        Assert.That(nanjing.FirstRow, Is.EqualTo(4));
        Assert.That(nanjing.LastRow, Is.EqualTo(15));
        Assert.That(blocks.Any(b => b.SchoolName == ""), Is.True, "北區 rows 41-52 (0-based 40-51) is a genuinely blank merged block and must still be found (and later skipped by ResolveSchool)");
    }

    [Test]
    public void FindSchoolBlocks_SouthLeftAndRight_FindAllSevenNamedSchools() {
        var wb = OpenWorkbook();
        ISheet south = wb.GetSheet("南區");
        var left = PSJPopulationImporter.FindSchoolBlocks(south, 1).Where(b => b.SchoolName != "" && b.SchoolName != "總計").Select(b => b.SchoolName).ToList();
        var right = PSJPopulationImporter.FindSchoolBlocks(south, 21).Where(b => b.SchoolName != "" && b.SchoolName != "總計").Select(b => b.SchoolName).ToList();

        Assert.That(left, Is.EqualTo(new[] { "東安", "莊敬", "瑞祥", "中正" }));
        Assert.That(right, Is.EqualTo(new[] { "岡山", "高美", "河堤" }));
    }

    [Test]
    public void ColumnLayout_SouthLeftShiftedBy20_MatchesSouthRightRawColumns() {
        var shifted = PSJPopulationImporter.ColumnLayout.SouthLeft.Shift(20);
        Assert.That(shifted.NameCol, Is.EqualTo(21));
        Assert.That(shifted.MathGroupCols, Is.EqualTo(new[] { 30, 31, 32, 33, 34 }));
        Assert.That(shifted.ScienceGroupCol, Is.EqualTo(36));
    }
}
```

- [ ] **Step 6: Run and confirm pass**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~PsjBlockDetectionTests"`
Expected: 3 passed, 0 failed. (If `FindSchoolBlocks_SouthLeftAndRight` fails on ordering, sort by `FirstRow` — school order in the assertions follows each merged region's row position in the real file, not alphabetical.)

- [ ] **Step 7: Commit**

```bash
git add source/portal/Portal/AssemblyInfo.cs \
        source/portal/Portal/Services/Import/PSJPopulationImporter.cs \
        source/portal/Test/Services/Import/PsjBlockDetectionTests.cs
git commit -m "feat: rewrite PSJPopulationImporter.Scan using merged-region block detection"
```

---

### Task 5: `PSJPopulationImporter.Import` — read CKC + 數學班/理化班 data per block

**Files:**
- Modify: `source/portal/Portal/Services/Import/PSJPopulationImporter.cs`

**Interfaces:**
- Consumes: `ColumnLayout`, `FindSchoolBlocks`, `ResolveSchool` (Task 4); `CourseMapping.GradeOrder`, `CourseMapping.PsjGradeOrder`, `CourseMapping.PsjCourseIds`, `CourseMapping.CkcCourseIds`, `CourseMapping.ReadCellNumber` (Task 2 + pre-existing); `PopulationWriteHelper.GetOrCreatePopulation`, `PopulationWriteHelper.AddClassAndItem` (pre-existing).

- [ ] **Step 1: Replace the `Import` stub**

In `source/portal/Portal/Services/Import/PSJPopulationImporter.cs`, replace the placeholder `Import` method from Task 4 with:

```csharp
    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger, int? overrideYear = null, int? overrideWeek = null) {
        var result = new ImportResult { Type = "PSJ" };
        if (!overrideYear.HasValue || overrideYear.Value <= 0 || !overrideWeek.HasValue || overrideWeek.Value <= 0) {
            result.Errors.Add("PSJ 匯入必須指定學年度與週次");
            return result;
        }
        int yearInt = overrideYear.Value, weekInt = overrideWeek.Value;

        SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
        if (schoolYear == null) {
            result.Errors.Add($"找不到學年週次: {yearInt}第{weekInt}週");
            return result;
        }

        var wb = new XSSFWorkbook(fileStream);
        ISheet north = wb.GetSheet("北區");
        ISheet south = wb.GetSheet("南區");
        if (north == null || south == null) {
            result.Errors.Add("找不到「北區」或「南區」頁籤");
            return result;
        }

        ProcessSide(db, north, ColumnLayout.North, yearInt, weekInt, schoolYear, result, logger);
        ProcessSide(db, south, ColumnLayout.SouthLeft, yearInt, weekInt, schoolYear, result, logger);
        ProcessSide(db, south, ColumnLayout.SouthLeft.Shift(20), yearInt, weekInt, schoolYear, result, logger);

        return result;
    }

    private static void ProcessSide(DataContext db, ISheet sheet, ColumnLayout layout, int yearInt, int weekInt,
        SchoolYear schoolYear, ImportResult result, ILogger logger) {

        foreach (var block in FindSchoolBlocks(sheet, layout.NameCol)) {
            School school = ResolveSchool(db, block.SchoolName);
            if (school == null) continue;

            StudentPopulation pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                StudentPopulationType.PSJ, $"{yearInt}第{weekInt}週百倍速人數表", true);
            result.PopulationIds.Add(pop.Id);
            result.SchoolCount++;

            for (int rNo = block.FirstRow; rNo <= block.LastRow; rNo++) {
                IRow row = sheet.GetRow(rNo);
                if (row == null) continue;

                string grade = row.GetCell(layout.GradeCol - 1)?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(grade) || grade == "小計") continue;

                int mathGradeIdx = Array.IndexOf(CourseMapping.GradeOrder, grade);
                int ckcGradeIdx = Array.IndexOf(CourseMapping.PsjGradeOrder, grade);

                if (ckcGradeIdx >= 0) {
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_E"][ckcGradeIdx], ClassType.Group, row, layout.CkcEnglishGroupCol, pop.Id, result, logger);
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_E"][ckcGradeIdx], ClassType.Personal, row, layout.CkcEnglishPersonalCol, pop.Id, result, logger);
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_C"][ckcGradeIdx], ClassType.Group, row, layout.CkcChineseGroupCol, pop.Id, result, logger);
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_C"][ckcGradeIdx], ClassType.Personal, row, layout.CkcChinesePersonalCol, pop.Id, result, logger);
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_M"][ckcGradeIdx], ClassType.Group, row, layout.CkcMathGroupCol, pop.Id, result, logger);
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_M"][ckcGradeIdx], ClassType.Personal, row, layout.CkcMathPersonalCol, pop.Id, result, logger);
                }

                if (mathGradeIdx >= 0) {
                    WriteIfPositive(db, school.Id, CourseMapping.PsjCourseIds["MP"][mathGradeIdx], ClassType.Personal, row, layout.MathPersonalCol, pop.Id, result, logger);
                    foreach (int col in layout.MathGroupCols)
                        WriteIfPositive(db, school.Id, CourseMapping.PsjCourseIds["MS"][mathGradeIdx], ClassType.SubGroup, row, col, pop.Id, result, logger);

                    WriteIfPositive(db, school.Id, CourseMapping.PsjCourseIds["SP"][mathGradeIdx], ClassType.Personal, row, layout.SciencePersonalCol, pop.Id, result, logger);
                    WriteIfPositive(db, school.Id, CourseMapping.PsjCourseIds["SS"][mathGradeIdx], ClassType.SubGroup, row, layout.ScienceGroupCol, pop.Id, result, logger);
                }
            }
        }
    }

    private static void WriteIfPositive(DataContext db, int schoolId, int courseId, ClassType cType, IRow row, int oneBasedCol, long populationId, ImportResult result, ILogger logger) {
        int count = CourseMapping.ReadCellNumber(row, oneBasedCol - 1);
        if (count <= 0) return;
        Course course = db.Course.Include("Department").FirstOrDefault(e => e.Id == courseId);
        if (course == null) return;
        PopulationWriteHelper.AddClassAndItem(db, schoolId, course, cType, populationId, count, result, logger);
    }
```

- [ ] **Step 2: Build**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: `Build succeeded. 0 Error(s)`.

There is no isolated unit test for `Import`/`ProcessSide` in this task — it calls `DataContext` (live EF Core context) and needs the CKC courses from Task 3 to exist, so meaningful verification requires the live dev DB. That full verification is Task 7's `[Explicit]` integration test, run after Task 6 wires up the admin UI. This mirrors the existing convention in this codebase (see `Test/Services/Aggregation/*ComparisonTests.cs`): import/aggregation logic is integration-tested against a real DB, not mocked.

- [ ] **Step 3: Commit**

```bash
git add source/portal/Portal/Services/Import/PSJPopulationImporter.cs
git commit -m "feat: implement PSJPopulationImporter.Import for CKC + existing 數學班/理化班 columns"
```

---

### Task 6: Admin UI — 學年度/週次 selector for PSJ imports

**Files:**
- Modify: `source/portal/Portal/Areas/Admin/Controllers/StudentPopulationImportController.cs`
- Modify: `source/portal/Portal/Areas/Admin/Views/StudentPopulationImport/Index.cshtml`

**Interfaces:**
- Consumes: `PopulationImportService.Scan(db, type, stream, int? overrideYear, int? overrideWeek)` / `.Import(db, type, stream, fileName, int? overrideYear, int? overrideWeek)` (Task 1).

- [ ] **Step 1: Update the controller**

Replace `source/portal/Portal/Areas/Admin/Controllers/StudentPopulationImportController.cs` in full with:

```csharp
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [RequirePermission(SystemPermission.StudentPopulationImport)]
    public class StudentPopulationImportController : AdminBaseController {
        private readonly PopulationImportService _importService;

        public StudentPopulationImportController(PopulationImportService importService) {
            _importService = importService;
        }

        public IActionResult Index() {
            ViewBag.Title = "人數表匯入";
            using var db = new DataContext();
            int currentMaxYear = db.SchoolYear.Max(e => e.Year) ?? 0;
            ViewBag.SchoolYears = db.SchoolYear.Where(e => e.Year == currentMaxYear).OrderBy(e => e.Week).ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Import(IFormFile file, StudentPopulationType type, bool confirmed = false, int? year = null, int? week = null) {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "請選擇檔案" });

            using var db = new DataContext();

            ImportScanResult scan;
            using (var scanStream = file.OpenReadStream()) {
                scan = _importService.Scan(db, type, scanStream, year, week);
            }

            if (scan.Errors.Count > 0)
                return Json(new { success = false, message = string.Join("; ", scan.Errors) });

            var conflicts = scan.Items.Where(i => i.Exists).ToList();
            if (conflicts.Count > 0 && !confirmed) {
                return Json(new {
                    success = true,
                    requiresConfirmation = true,
                    conflicts = conflicts.Select(c => new { schoolName = c.SchoolName, year = c.Year, week = c.Week }),
                });
            }

            ImportResult result;
            using (var importStream = file.OpenReadStream()) {
                result = _importService.Import(db, type, importStream, file.FileName, year, week);
            }

            return Json(new {
                success = true,
                requiresConfirmation = false,
                schoolCount = result.SchoolCount,
                itemCount = result.ItemCount,
                errors = result.Errors,
            });
        }
    }
}
```

- [ ] **Step 2: Update the view**

Replace `source/portal/Portal/Areas/Admin/Views/StudentPopulationImport/Index.cshtml` in full with:

```html
@using PHStatistics.Content
@{
    ViewBag.Title = "人數表匯入";
}
<div class="container py-4">
    <h3>人數表匯入</h3>
    <p class="text-muted">選擇報表類型並上傳對應的 Excel 檔案。若該分校/週次已有資料，系統會先詢問確認才覆寫重建。</p>

    <div class="mb-3">
        <label class="form-label">報表類型</label>
        <select class="form-select" id="importType" style="max-width:300px">
            <option value="PH">百瀚（PH）</option>
            <option value="GEPT">英檢（GEPT）</option>
            <option value="PS">百世（PS）</option>
            <option value="PSJ">百倍速（PSJ）</option>
            <option value="AfterSchool">課輔（AS）</option>
        </select>
    </div>
    <div class="mb-3">
        <label class="form-label">學年度／週次（百倍速 PSJ 必填，其餘格式沿用檔案內建年份週次即可不選）</label>
        <select class="form-select" id="importYearWeek" style="max-width:300px">
            <option value="">（不指定）</option>
            @foreach (var sy in (System.Collections.Generic.List<SchoolYear>)ViewBag.SchoolYears) {
                <option value="@sy.Id" data-year="@sy.Year" data-week="@sy.Week">@sy.Year 年第 @sy.Week 週</option>
            }
        </select>
    </div>
    <div class="mb-3">
        <label class="form-label">Excel 檔案</label>
        <input type="file" class="form-control" id="importFile" accept=".xlsx" style="max-width:400px">
    </div>
    <button type="button" class="btn btn-primary" id="importBtn">上傳並匯入</button>

    <div id="importResult" class="mt-3"></div>
</div>

<script>
    function doImport(confirmed) {
        var fileInput = document.getElementById('importFile');
        if (!fileInput.files || fileInput.files.length === 0) {
            alert('請選擇檔案');
            return;
        }
        var type = $('#importType').val();
        var ywSelect = document.getElementById('importYearWeek');
        var selectedOption = ywSelect.options[ywSelect.selectedIndex];
        var year = selectedOption.getAttribute('data-year');
        var week = selectedOption.getAttribute('data-week');

        if (type === 'PSJ' && !year) {
            alert('百倍速（PSJ）匯入必須選擇學年度與週次');
            return;
        }

        var formData = new FormData();
        formData.append('file', fileInput.files[0]);
        formData.append('type', type);
        formData.append('confirmed', confirmed ? 'true' : 'false');
        if (year) {
            formData.append('year', year);
            formData.append('week', week);
        }

        $('#importResult').html('<div class="text-muted">匯入中...</div>');

        $.ajax({
            url: '@Url.Action("Import")',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                if (!res.success) {
                    $('#importResult').html('<div class="alert alert-danger">' + res.message + '</div>');
                    return;
                }
                if (res.requiresConfirmation) {
                    var lines = res.conflicts.map(function (c) {
                        return c.schoolName + ' ' + c.year + '年第' + c.week + '週';
                    }).join('、');
                    if (confirm('以下資料已存在，確定要覆寫重建嗎？\n' + lines)) {
                        doImport(true);
                    } else {
                        $('#importResult').html('<div class="text-muted">已取消匯入</div>');
                    }
                    return;
                }
                var errHtml = (res.errors && res.errors.length > 0)
                    ? '<div class="alert alert-warning mt-2">錯誤：' + res.errors.join('<br>') + '</div>'
                    : '';
                $('#importResult').html(
                    '<div class="alert alert-success">匯入完成：' + res.schoolCount + ' 校，' + res.itemCount + ' 筆項目</div>' + errHtml
                );
            },
            error: function () {
                $('#importResult').html('<div class="alert alert-danger">匯入失敗，請稍後再試</div>');
            }
        });
    }

    $('#importBtn').on('click', function () { doImport(false); });
</script>
```

- [ ] **Step 3: Build**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Areas/Admin/Controllers/StudentPopulationImportController.cs \
        source/portal/Portal/Areas/Admin/Views/StudentPopulationImport/Index.cshtml
git commit -m "feat: add 學年度/週次 selector to admin import page for PSJ"
```

---

### Task 7: `[Explicit]` integration test against the real file + live dev DB

**Files:**
- Create: `source/portal/Test/Services/Import/PsjImportTests.cs`

**Interfaces:**
- Consumes: `PSJPopulationImporter` (Tasks 4–5), live `DataContext` (`School`, `SchoolYear`, `Course`, `StudentPopulation`, `StudentPopulationItem`), the real file at `C:\Leo\其他\Kuri\人數表匯入A\50\（百倍速）人數統計表更新版115.6.13).xlsx`.

This test actually calls `Import`, which writes real rows to the dev DB (same as production behavior) — it is not a read-only comparison like the aggregation tests, because there is no prior "old importer's real output" to diff against (the design spec's investigation established the old importer never wrote anything real — see spec "背景"). It's safe to re-run: `PopulationWriteHelper.GetOrCreatePopulation(..., deleteExisting: true)` clears and rebuilds the target `StudentPopulation` each time.

All expected numbers below were read directly from the real file with `openpyxl` during spec/plan investigation — cross-check against the file yourself if anything looks off. School year: this file is dated ROC 115.6.13 (within the week starting 2026-06-07), which is `SchoolYear.Year=114, Week=50` in the dev DB (confirmed live) — the same week as the sibling PH/PS files in the same import folder.

- [ ] **Step 1: Write the test**

```csharp
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import;
using Microsoft.Extensions.Logging.Abstractions;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
[Explicit("Writes real rows to the dev DB and requires the local PSJ fixture file; run manually to verify the PSJ CKC import rewrite (see plan Task 7)")]
public class PsjImportTests {
    private const string RealFilePath = @"C:\Leo\其他\Kuri\人數表匯入A\50\（百倍速）人數統計表更新版115.6.13).xlsx";
    private const int Year = 114;
    private const int Week = 50;

    private static ImportResult RunImport(DataContext db) {
        var importer = new PSJPopulationImporter();
        using var fs = new System.IO.FileStream(RealFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        return importer.Import(db, fs, NullLogger.Instance, Year, Week);
    }

    private static int NumberFor(DataContext db, long populationId, int courseId, ClassType classType) {
        return db.StudentPopulationItem
            .Include(i => i.Class)
            .Where(i => i.StudentPopulationId == populationId && i.Class.CourseId == courseId && i.Class.Type == classType)
            .Sum(i => (int?)i.Number) ?? 0;
    }

    [Test]
    public void Import_RealFile_ProducesExpectedKnownValues() {
        using var db = new DataContext();
        var result = RunImport(db);

        Assert.That(result.Errors, Is.Empty, () => string.Join("\n", result.Errors));

        // 南京 國一：數學班小組班第一班 = 1 (北區 row10, col10)
        // CourseMapping.GradeOrder index: 0=一年級,1=二年級,...,6=國一 — 國一 is index 6, not 1.
        var nanjing = db.StudentPopulation.First(p => p.School.Name == "南京" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        Assert.That(NumberFor(db, nanjing.Id, CourseMapping.PsjCourseIds["MS"][6], ClassType.SubGroup), Is.EqualTo(1));

        // 內湖 國二：數學班小組班第一班=2、第二班=1（Excel 2+1 拆成 2 筆各自 Class，見北區 row23 col10/col11）
        var neihu = db.StudentPopulation.First(p => p.School.Name == "內湖" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        var neihuGuo2Classes = db.Class.Where(c => c.SchoolId == neihu.SchoolId && c.CourseId == CourseMapping.PsjCourseIds["MS"][7] && c.Type == ClassType.SubGroup
            && db.StudentPopulationItem.Any(i => i.ClassId == c.Id && i.StudentPopulationId == neihu.Id)).ToList();
        Assert.That(neihuGuo2Classes.Count, Is.EqualTo(2), "內湖 國二數學班小組班應拆成 2 筆獨立 Class（第一班=2, 第二班=1）");

        // 內湖 國三：理化班一對一 = 2（北區 row24 col14）
        Assert.That(NumberFor(db, neihu.Id, CourseMapping.PsjCourseIds["SP"][8], ClassType.Personal), Is.EqualTo(2));

        // 莊敬 國二（南區左半 row38）：數學班一對一=1, 理化班一對一=1, 理化班小組班=1
        var zhuangjing = db.StudentPopulation.First(p => p.School.Name == "莊敬" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        Assert.That(NumberFor(db, zhuangjing.Id, CourseMapping.PsjCourseIds["MP"][7], ClassType.Personal), Is.EqualTo(1));
        Assert.That(NumberFor(db, zhuangjing.Id, CourseMapping.PsjCourseIds["SP"][7], ClassType.Personal), Is.EqualTo(1));
        Assert.That(NumberFor(db, zhuangjing.Id, CourseMapping.PsjCourseIds["SS"][7], ClassType.SubGroup), Is.EqualTo(1));

        // 河堤（南區右半，+20 offset）高二：數學班一對一 = 1 (row54 col29)
        // CourseMapping.GradeOrder index: 高二 is index 10.
        var hedi = db.StudentPopulation.First(p => p.School.Name == "河堤" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        Assert.That(NumberFor(db, hedi.Id, CourseMapping.PsjCourseIds["MP"][10], ClassType.Personal), Is.EqualTo(1));

        // 高美館（南區右半，Excel 頁籤寫「高美」，別名解析為資料庫的「高美館」）：國二數學班小組班第一班=3（row38 col30）
        var gaomeiguan = db.StudentPopulation.First(p => p.School.Name == "高美館" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        Assert.That(NumberFor(db, gaomeiguan.Id, CourseMapping.PsjCourseIds["MS"][7], ClassType.SubGroup), Is.EqualTo(3));

        // 中正（南區左半 row70-82，跟右半的「總計」共用同一段列號——驗證兩側各自獨立追蹤，
        // 總計側的大量非零數字不會外洩到中正）：國二理化班小組班 = 2（row77 col16）
        var zhongzheng = db.StudentPopulation.First(p => p.School.Name == "中正" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        Assert.That(NumberFor(db, zhongzheng.Id, CourseMapping.PsjCourseIds["SS"][7], ClassType.SubGroup), Is.EqualTo(2));

        // CKC 英/國/數：這份真實檔案裡三科「加上」「單上」欄位全部是 0/空白，
        // 所以這裡只驗證流程沒有例外、沒有寫入任何 CKC Class（未驗證非零情境——之後若有含 CKC 真實數字的檔案，應補測）。
        var ckcCourseIds = CourseMapping.CkcCourseIds["CKC_E"]
            .Concat(CourseMapping.CkcCourseIds["CKC_C"])
            .Concat(CourseMapping.CkcCourseIds["CKC_M"]);
        int ckcClassCount = db.Class.Count(c => ckcCourseIds.Contains(c.CourseId ?? 0));
        Assert.That(ckcClassCount, Is.EqualTo(0), "本次真實檔案 CKC 欄位全為 0，若此斷言失敗代表檔案已更新為含真實 CKC 資料，請改寫本測試改為驗證實際數字");

        TestContext.WriteLine($"Schools imported: {result.SchoolCount}, items: {result.ItemCount}");
    }
}
```

- [ ] **Step 2: Run it**

`[Explicit]` tests are skipped by a normal `dotnet test` run — filtering by fully-qualified name overrides that and runs it directly:

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~PsjImportTests"
```

Expected: 1 passed. If any assertion fails, open the real file with the column layout from Task 4/5 side-by-side and check whether the failing cell's row/column matches what's documented above — the numbers in this test were read directly from the file, so a mismatch means either the file changed or the importer has a real bug, not a stale expectation.

- [ ] **Step 3: Commit**

```bash
git add source/portal/Test/Services/Import/PsjImportTests.cs
git commit -m "test: add PSJ CKC import explicit integration test against real file and dev DB"
```

---

## Post-plan cleanup note (not a task — just don't forget)

`docs/superpowers/specs/2026-07-16-psj-ckc-import-design.md`'s test plan also calls for confirming the +20 offset reads the right-half schools correctly, which Task 4's `PsjBlockDetectionTests` and Task 7's 河堤/高美館 assertions already cover — no separate task needed.
