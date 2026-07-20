# AS（課輔）新增數學班／理化班資料模型 Implementation Plan — Phase 1

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the missing 數學班（Math）／理化班（Science）course data model for AS（課輔，`StudentPopulationType.AfterSchool`）so it mirrors the existing 安親／英文 shape, then fix the 3 duplicated course-id lookup tables + import/export column lists that currently silently drop these two subjects' headcounts.

**Architecture:** Pure data addition (100 new `Course` rows across 2 new subject "triads" — 年級原始課程 + 統計合計 + 分析(上週比/新生/流失), each mirroring the existing 安親/英文 `CourseDepartment` shape and the already-migrated `AggregationEngine`-driven calculation model) plus 4 mechanical code changes to read/write the new columns. No changes to `AggregationEngine` or `StudentPopulationController.SumPHPopulation` — `StatisticsType`-driven calculation is already generic and picks up the new courses automatically once the data exists.

**Tech Stack:** ASP.NET Core 8 / EF Core 8, NPOI (XSSFWorkbook), NUnit 4 (`[Explicit]` integration tests against the live dev DB — this codebase has no DI seam for mocking `DataContext`, so import/export logic tests are integration tests, not unit tests, by established convention; see `Test/Services/Import/PsjImportTests.cs`, `Test/Services/ReportExportServiceStructureTests.cs`).

## Global Constraints

- Existing `Course` max `Id` = 377, existing `CourseDepartment` max `Id` = 39 (verified live against the dev DB during spec review — do not re-derive, use directly).
- New `CourseDepartment` rows use `Id` 40–45 (數學班/數學班統計/數學班分析/理化班/理化班統計/理化班分析). New `Course` rows use `Id` 378–477 (數學班 378–427, 理化班 428–477), see spec §"詳細設計" for the exact per-Id breakdown.
- `GroupByClassType = false` for all new `IsSum` courses — mirrors AS's existing 安親/英文 convention, **not** PSJ's (PSJ uses `true` on its per-classtype total). Do not "fix" this into matching PSJ; it's a deliberate, confirmed-with-user inconsistency being preserved, not introduced.
- ClassType mapping: 一對一 → `ClassType.Personal`, 團體班 → `ClassType.General` — mirrors AS's existing 英文(EP/EG) convention, not PSJ's `Personal`/`SubGroup`.
- 新生/流失/上週比/總人數 **cross-subject aggregation** (a single combined value per grade spanning all 4 subjects, matching what the reference Excel actually shows) is **out of scope** — deferred to Phase 2 (the new grid UI). This plan keeps 新生/流失/上週比 as 4 independent per-subject course sets, exactly like 安親/英文 today.
- Phase 2 (new grid input page) is **out of scope** for this plan entirely.
- Browser/manual verification of the existing list-style `CreateASPopulation` page (which will automatically show the 2 new course groups once this data exists) is **out of scope** — deferred to the project's usual end-of-cycle browser verification pass.
- Spec: `source/portal/docs/superpowers/specs/2026-07-20-as-math-science-course-model-design.md`.
- Dev DB connection string: `source/portal/Portal/appsettings.json` → `Server=CLOUDFUN-MSI-LE\SQLEXPRESS;Database=NewPAS0716;User=sa;Pwd=cloudfun@12`. **Do not use the commented-out production connection string in that file for anything in this plan.**
- Reference school/week used by every test in this plan: School `東湖` (`Id=29`), `SchoolYear` `Year=114, Week=52` (`Id=1162`) — both already exist in the dev DB; verified no `StudentPopulation` row exists yet for `東湖/114/52/AfterSchool`, so tests that create one and clean it up in a `finally` block will not collide with any pre-existing data.

---

### Task 1: SQL — create the 6 `CourseDepartment` + 100 `Course` rows

**Files:**
- Create: `source/portal/docs/superpowers/sql/2026-07-20-as-math-science-course-apply.sql` (already written during spec review — verify its contents match this task, do not regenerate from scratch)
- Create: `source/portal/docs/superpowers/sql/2026-07-20-as-math-science-course-revert.sql` (already written during spec review)

**Interfaces:**
- Produces: `Course` rows 378–477, `CourseDepartment` rows 40–45 — every later task depends on these exact Ids existing in the dev DB before its own step runs.

These two files already exist (written and reviewed during spec authoring). This task is: confirm their contents, apply them to the dev DB, and verify.

- [ ] **Step 1: Read and confirm the apply script**

Open `source/portal/docs/superpowers/sql/2026-07-20-as-math-science-course-apply.sql` and confirm it contains exactly:
- One `INSERT INTO CourseDepartment` statement with 6 rows, Ids 40–45 (wrapped in `SET IDENTITY_INSERT CourseDepartment ON/OFF`)
- One `INSERT INTO Course` statement with 100 rows, Ids 378–477 (wrapped in `SET IDENTITY_INSERT Course ON/OFF`)

If the file is missing or doesn't match, stop and re-derive it from `docs/superpowers/specs/2026-07-20-as-math-science-course-model-design.md` §"詳細設計" §1–2 before proceeding — do not hand-edit individual rows without re-checking the spec's Id/StatisticsType/SourceCourseIds table.

- [ ] **Step 2: Apply the script to the dev DB**

```bash
sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -U sa -P "cloudfun@12" -d NewPAS0716 -i "source/portal/docs/superpowers/sql/2026-07-20-as-math-science-course-apply.sql"
```

- [ ] **Step 3: Verify with an independent SELECT (do not trust console output alone — this project's `sqlcmd -i` has a known history of silently skipping statements)**

```bash
sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -U sa -P "cloudfun@12" -d NewPAS0716 -Q "SET NOCOUNT ON; SELECT COUNT(*) AS DeptCount FROM CourseDepartment WHERE Id BETWEEN 40 AND 45; SELECT COUNT(*) AS CourseCount FROM Course WHERE Id BETWEEN 378 AND 477; SELECT DepartmentId, COUNT(*) AS Cnt FROM Course WHERE Id BETWEEN 378 AND 477 GROUP BY DepartmentId ORDER BY DepartmentId;" -W -s"|"
```

Expected:
- `DeptCount` = 6
- `CourseCount` = 100
- Per-department breakdown: `40→12, 41→2, 42→36, 43→12, 44→2, 45→36`

- [ ] **Step 4: Spot-check one `StatisticsType`-driven row's `SourceCourseIds` resolves to the right course**

```bash
sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -U sa -P "cloudfun@12" -d NewPAS0716 -Q "SET NOCOUNT ON; SELECT c.Id, c.Name, c.SourceCourseIds, src.Name AS SourceName FROM Course c JOIN Course src ON src.Id = 378 WHERE c.Id = 392;" -W -s"|"
```

Expected: row `Id=392, Name='本週一年級與上週相比', SourceCourseIds='[378]', SourceName='一年級'` — confirms course 392 (數學班 一年級 上週比) points at course 378 (數學班 一年級 grade course), not some other grade.

- [ ] **Step 5: Commit**

```bash
git add source/portal/docs/superpowers/sql/2026-07-20-as-math-science-course-apply.sql \
        source/portal/docs/superpowers/sql/2026-07-20-as-math-science-course-revert.sql
git commit -m "feat: add SQL script creating AS 數學班/理化班 CourseDepartment triads and 100 Course rows"
```

---

### Task 2: `CourseMapping.cs` — add `MP`/`MG`/`SP`/`SG` to `AsCourseIds`

**Files:**
- Modify: `source/portal/Portal/Services/Import/ImportSupport/CourseMapping.cs`
- Create: `source/portal/Test/Services/Import/CourseMappingAsMathScienceTests.cs`

**Interfaces:**
- Consumes: nothing new (pure static data addition to an existing dictionary).
- Produces: `CourseMapping.AsCourseIds["MP"]`, `["MG"]`, `["SP"]`, `["SG"]` (each `int[12]`) — Task 3 (`ASPopulationImporter`), Task 4 (`ReportExportService`), and Task 5 (`StudentPopulationController`) all read these by key.

This is a pure in-memory data test — no DB dependency, unlike every other task in this plan.

- [ ] **Step 1: Write the failing test**

Create `source/portal/Test/Services/Import/CourseMappingAsMathScienceTests.cs`:

```csharp
using System.Linq;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
public class CourseMappingAsMathScienceTests {
    [Test]
    public void AsCourseIds_MathKeys_Are12ContiguousIdsStartingAt378() {
        Assert.That(CourseMapping.AsCourseIds["MP"], Is.EqualTo(Enumerable.Range(378, 12).ToArray()));
        Assert.That(CourseMapping.AsCourseIds["MG"], Is.EqualTo(Enumerable.Range(378, 12).ToArray()));
    }

    [Test]
    public void AsCourseIds_ScienceKeys_Are12ContiguousIdsStartingAt428() {
        Assert.That(CourseMapping.AsCourseIds["SP"], Is.EqualTo(Enumerable.Range(428, 12).ToArray()));
        Assert.That(CourseMapping.AsCourseIds["SG"], Is.EqualTo(Enumerable.Range(428, 12).ToArray()));
    }

    [Test]
    public void AsColumnType_MapsPersonalCodesToPersonal_AndGeneralCodesToGeneral() {
        Assert.That(CourseMapping.AsColumnType("MP"), Is.EqualTo(PHStatistics.Content.ClassType.Personal));
        Assert.That(CourseMapping.AsColumnType("SP"), Is.EqualTo(PHStatistics.Content.ClassType.Personal));
        Assert.That(CourseMapping.AsColumnType("MG"), Is.EqualTo(PHStatistics.Content.ClassType.General));
        Assert.That(CourseMapping.AsColumnType("SG"), Is.EqualTo(PHStatistics.Content.ClassType.General));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~CourseMappingAsMathScienceTests"
```

Expected: `AsCourseIds_MathKeys_...` and `AsCourseIds_ScienceKeys_...` FAIL with `KeyNotFoundException` (`"MP"`/`"SP"` not present yet in `AsCourseIds`). `AsColumnType_...` PASSES already (the switch expression already handles `MP`/`SP`→`Personal` and defaults everything else, including `MG`/`SG`, to `General` — this test exists to lock in that this pre-existing behavior is correct and doesn't need changing, not to drive new code).

- [ ] **Step 3: Add the 4 entries to `AsCourseIds`**

In `source/portal/Portal/Services/Import/ImportSupport/CourseMapping.cs`, replace the existing `AsCourseIds` dictionary (currently ending `["W"] = new[]{259,260,261,262,263,264,265,266,267,268,269,270},`) with:

```csharp
    public static readonly Dictionary<string, int[]> AsCourseIds = new() {
        ["AS"] = new[]{245,246,247,248,249,250,251,252,253,254,255,256},
        ["EP"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
        ["EG"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
        ["N"]  = new[]{271,272,273,274,275,276,277,278,279,280,281,282},
        ["L"]  = new[]{283,284,285,286,287,288,289,290,291,292,293,294},
        ["W"]  = new[]{259,260,261,262,263,264,265,266,267,268,269,270},
        ["MP"] = new[]{378,379,380,381,382,383,384,385,386,387,388,389},
        ["MG"] = new[]{378,379,380,381,382,383,384,385,386,387,388,389},
        ["SP"] = new[]{428,429,430,431,432,433,434,435,436,437,438,439},
        ["SG"] = new[]{428,429,430,431,432,433,434,435,436,437,438,439},
    };
```

(`AsColumnType`, a few lines below, is unchanged — it already maps `"MP"`/`"SP"` to `Personal` and defaults everything else to `General`.)

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~CourseMappingAsMathScienceTests"
```

Expected: 3 passed, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Services/Import/ImportSupport/CourseMapping.cs \
        source/portal/Test/Services/Import/CourseMappingAsMathScienceTests.cs
git commit -m "feat: add AS 數學班/理化班 course IDs to CourseMapping.AsCourseIds"
```

---

### Task 3: `ASPopulationImporter.cs` — read Excel col6–9 (數學班/理化班)

**Files:**
- Modify: `source/portal/Portal/Services/Import/ASPopulationImporter.cs`
- Create: `source/portal/Test/Services/Import/AsMathScienceImportTests.cs`

**Interfaces:**
- Consumes: `CourseMapping.AsCourseIds["MP"/"MG"/"SP"/"SG"]` (Task 2), `Course` rows 378–439 (Task 1), `PopulationWriteHelper.GetOrCreatePopulation`/`AddClassAndItem` (pre-existing, unchanged).
- Produces: nothing new consumed by later tasks — this is a leaf change.

The reference Excel fixture (`百瀚全區課輔人數總表(20260613).xlsx`) has **zero non-null values** in columns 6–9 (數學班/理化班) across all 3 school tabs and all weeks (verified during spec investigation) — so a real-file test can't demonstrate a real non-zero extraction. Instead this task builds a small synthetic in-memory workbook with known non-zero values in col6–9, giving a genuine red→green cycle. (This still requires the live dev DB — `ASPopulationImporter.Import` takes a real `DataContext` and looks up `Course`/`School` rows — it is not a pure unit test.)

- [ ] **Step 1: Write the failing test**

Create `source/portal/Test/Services/Import/AsMathScienceImportTests.cs`:

```csharp
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NPOI.XSSF.UserModel;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
[Explicit("Writes real rows to the dev DB; run manually to verify AS math/science column reading (see plan Task 3)")]
public class AsMathScienceImportTests {
    private const int Year = 114;
    private const int Week = 52; // reuses the real SchoolYear/School fixture already in the dev DB; test cleans up fully in [TearDown]

    private static MemoryStream BuildWorkbook(int week, string grade, int asVal, int epVal, int egVal, int mpVal, int mgVal, int spVal, int sgVal) {
        var wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet($"{week}東湖");
        sheet.CreateRow(0).CreateCell(0).SetCellValue($"東湖 教室{Year}學年度7-6月課輔班人數統計表(請於每週六下班回傳)");
        var row = sheet.CreateRow(4);
        row.CreateCell(0).SetCellValue(week);
        row.CreateCell(2).SetCellValue(grade);
        row.CreateCell(3).SetCellValue(asVal);
        row.CreateCell(4).SetCellValue(epVal);
        row.CreateCell(5).SetCellValue(egVal);
        row.CreateCell(6).SetCellValue(mpVal);
        row.CreateCell(7).SetCellValue(mgVal);
        row.CreateCell(8).SetCellValue(spVal);
        row.CreateCell(9).SetCellValue(sgVal);
        var ms = new MemoryStream();
        wb.Write(ms);
        ms.Position = 0;
        return ms;
    }

    private static int NumberFor(DataContext db, long populationId, int courseId, ClassType classType) {
        return db.StudentPopulationItem
            .Include(i => i.Class)
            .Where(i => i.StudentPopulationId == populationId && i.Class.CourseId == courseId && i.Class.Type == classType)
            .Sum(i => (int?)i.Number) ?? 0;
    }

    [TearDown]
    public void CleanUp() {
        using var db = new DataContext();
        var pop = db.StudentPopulation.FirstOrDefault(p => p.School.Name == "東湖" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.AfterSchool);
        if (pop == null) return;
        var items = db.StudentPopulationItem.Where(i => i.StudentPopulationId == pop.Id).ToList();
        var classIds = items.Select(i => i.ClassId).Distinct().ToList();
        db.StudentPopulationItem.RemoveRange(items);
        db.SaveChanges();
        db.Class.RemoveRange(db.Class.Where(c => classIds.Contains(c.Id)));
        db.SaveChanges();
        db.StudentPopulation.Remove(pop);
        db.SaveChanges();
    }

    [Test]
    public void Import_ReadsMathAndScienceColumns_ForOneGradeRow() {
        using var db = new DataContext();
        using var ms = BuildWorkbook(Week, grade: "一年級", asVal: 0, epVal: 0, egVal: 0, mpVal: 5, mgVal: 3, spVal: 2, sgVal: 1);

        var importer = new ASPopulationImporter();
        var result = importer.Import(db, ms, NullLogger.Instance);

        Assert.That(result.Errors, Is.Empty, () => string.Join("\n", result.Errors));

        var pop = db.StudentPopulation.First(p => p.School.Name == "東湖" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.AfterSchool);

        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["MP"][0], ClassType.Personal), Is.EqualTo(5), "數學班一對一(一年級)");
        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["MG"][0], ClassType.General), Is.EqualTo(3), "數學班團體(一年級)");
        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["SP"][0], ClassType.Personal), Is.EqualTo(2), "理化班一對一(一年級)");
        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["SG"][0], ClassType.General), Is.EqualTo(1), "理化班團體(一年級)");
    }

    [Test]
    [Explicit("Requires the real reference file at C:\\Leo\\其他\\Kuri\\人數表匯入A\\50\\; documents that this file has zero math/science data, not a regression guard")]
    public void Import_RealFile_MathAndScienceColumnsAreAllZero_ThisSpecificFileHasNoNonZeroData() {
        const string realFilePath = @"C:\Leo\其他\Kuri\人數表匯入A\50\百瀚全區課輔人數總表(20260613).xlsx";
        using var db = new DataContext();
        using var fs = new FileStream(realFilePath, FileMode.Open, FileAccess.Read);
        var importer = new ASPopulationImporter();
        var result = importer.Import(db, fs, NullLogger.Instance);

        Assert.That(result.Errors, Is.Empty, () => string.Join("\n", result.Errors));

        var pop = db.StudentPopulation.First(p => p.School.Name == "東湖" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.AfterSchool);
        // 東湖 一年級 安親 = 2 (existing regression check — must still work after extending colDefs)
        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["AS"][0], ClassType.General), Is.EqualTo(2), "安親一年級 regression check");
        // This real file has zero non-null values in col6-9 for all 3 schools/weeks (verified during spec investigation) —
        // this assertion documents that fact, it is NOT proof the new column-reading code is correct (see the synthetic-workbook test above for that).
        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["MP"][0], ClassType.Personal), Is.EqualTo(0));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AsMathScienceImportTests.Import_ReadsMathAndScienceColumns_ForOneGradeRow"
```

Expected: FAIL — `mpVal`/`mgVal`/`spVal`/`sgVal` are all asserted non-zero but `ASPopulationImporter`'s `colDefs` doesn't read columns 6–9 yet, so no `StudentPopulationItem` gets created for courses 378/428 and `NumberFor(...)` returns 0 for all four assertions.

- [ ] **Step 3: Extend `colDefs` in `ASPopulationImporter.Import`**

In `source/portal/Portal/Services/Import/ASPopulationImporter.cs`, replace:

```csharp
            var colDefs = new[] {
                (col: 3, code: "AS", cType: ClassType.General),
                (col: 4, code: "EP", cType: ClassType.Personal),
                (col: 5, code: "EG", cType: ClassType.General),
            };
```

with:

```csharp
            var colDefs = new[] {
                (col: 3, code: "AS", cType: ClassType.General),
                (col: 4, code: "EP", cType: ClassType.Personal),
                (col: 5, code: "EG", cType: ClassType.General),
                (col: 6, code: "MP", cType: ClassType.Personal),
                (col: 7, code: "MG", cType: ClassType.General),
                (col: 8, code: "SP", cType: ClassType.Personal),
                (col: 9, code: "SG", cType: ClassType.General),
            };
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AsMathScienceImportTests.Import_ReadsMathAndScienceColumns_ForOneGradeRow"
```

Expected: 1 passed, 0 failed.

- [ ] **Step 5: Run the real-file regression test**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AsMathScienceImportTests.Import_RealFile_MathAndScienceColumnsAreAllZero_ThisSpecificFileHasNoNonZeroData"
```

Expected: 1 passed, 0 failed (confirms extending `colDefs` didn't break the existing 安親 column, and documents that this specific reference file has no non-zero 數學/理化 data to verify against).

- [ ] **Step 6: Commit**

```bash
git add source/portal/Portal/Services/Import/ASPopulationImporter.cs \
        source/portal/Test/Services/Import/AsMathScienceImportTests.cs
git commit -m "feat: read AS 數學班/理化班 columns (Excel col6-9) in ASPopulationImporter"
```

---

### Task 4: `ReportExportService.cs` — export 數學班/理化班 columns

**Files:**
- Modify: `source/portal/Portal/Services/ReportExportService.cs`
- Create: `source/portal/Test/Services/ReportExportServiceAsMathScienceTests.cs`

**Interfaces:**
- Consumes: `CourseMapping.AsCourseIds["MP"/"MG"/"SP"/"SG"]` values (Task 2's values, duplicated locally per this file's existing `_asCourseIds` convention), `Course` rows 378–439 (Task 1), `PopulationWriteHelper.GetOrCreatePopulation`/`AddClassAndItem` (pre-existing, used only by this task's test setup, not by production code in this file).
- Produces: nothing new consumed by later tasks — this is a leaf change (independent of Task 3; only shares Task 1/2 as a common dependency).

- [ ] **Step 1: Write the failing test**

Create `source/portal/Test/Services/ReportExportServiceAsMathScienceTests.cs`:

```csharp
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using NPOI.XSSF.UserModel;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services;
using PHStatistics.Portal.Services.Import;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Test.Services;

[TestFixture]
[Explicit("Writes real rows to the dev DB; run manually to verify AS export includes math/science columns (see plan Task 4)")]
public class ReportExportServiceAsMathScienceTests {
    private const int Year = 114;
    private const int Week = 52;
    private long _populationId;

    [SetUp]
    public void SeedMathScienceItem() {
        using var db = new DataContext();
        School school = db.School.First(s => s.Name == "東湖");
        SchoolYear schoolYear = db.SchoolYear.First(s => s.Year == Year && s.Week == Week);
        Course mathCourse = db.Course.Include("Department").First(c => c.Id == CourseMapping.AsCourseIds["MP"][0]); // 378, 一年級, dept 40

        var pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, Year, Week, schoolYear,
            StudentPopulationType.AfterSchool, "test-export-math-science", true);
        _populationId = pop.Id;
        PopulationWriteHelper.AddClassAndItem(db, school.Id, mathCourse, ClassType.Personal, pop.Id, 7, new ImportResult(), NullLogger.Instance);
    }

    [TearDown]
    public void CleanUp() {
        using var db = new DataContext();
        var items = db.StudentPopulationItem.Where(i => i.StudentPopulationId == _populationId).ToList();
        var classIds = items.Select(i => i.ClassId).Distinct().ToList();
        db.StudentPopulationItem.RemoveRange(items);
        db.SaveChanges();
        db.Class.RemoveRange(db.Class.Where(c => classIds.Contains(c.Id)));
        db.SaveChanges();
        var pop = db.StudentPopulation.Find(_populationId);
        if (pop != null) { db.StudentPopulation.Remove(pop); db.SaveChanges(); }
    }

    [Test]
    public void Export_AfterSchool_IncludesMpColumnWithSeededValue() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.AfterSchool, Year, Week);

        Assert.That(bytes, Is.Not.Empty);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new XSSFWorkbook(ms);
        var sheet = wb.GetSheetAt(0);

        var hdrRow = sheet.GetRow(4);
        var headers = Enumerable.Range(0, hdrRow.LastCellNum).Select(c => hdrRow.GetCell(c)?.ToString() ?? "").ToList();
        int mpCol = headers.IndexOf("MP");
        Assert.That(mpCol, Is.GreaterThan(-1), "MP header column should exist in the exported code-header row");

        bool found = false;
        for (int r = 5; r <= sheet.LastRowNum; r++) {
            var row = sheet.GetRow(r);
            if (row?.GetCell(2)?.ToString() == "東湖" && row.GetCell(3)?.ToString() == "一年級") {
                Assert.That((int)row.GetCell(mpCol).NumericCellValue, Is.EqualTo(7));
                found = true;
                break;
            }
        }
        Assert.That(found, Is.True, "Expected a 東湖/一年級 row in the exported sheet");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~ReportExportServiceAsMathScienceTests"
```

Expected: FAIL — `mpCol` is `-1` (`codes` array in `BuildSheetAS` doesn't include `"MP"` yet).

- [ ] **Step 3: Update `_asCourseIds` and the `codes` array in `ReportExportService.cs`**

Replace the existing `_asCourseIds` dictionary (around line 39):

```csharp
    private static readonly Dictionary<string, int[]> _asCourseIds = new() {
        ["AS"] = new[] {245,246,247,248,249,250,251,252,253,254,255,256},
        ["EP"] = new[] {295,296,297,298,299,300,301,302,303,305,306,307},
        ["EG"] = new[] {295,296,297,298,299,300,301,302,303,305,306,307},
        ["N"]  = new[] {271,272,273,274,275,276,277,278,279,280,281,282},
        ["L"]  = new[] {283,284,285,286,287,288,289,290,291,292,293,294},
        ["W"]  = new[] {259,260,261,262,263,264,265,266,267,268,269,270},
        ["MP"] = new[] {378,379,380,381,382,383,384,385,386,387,388,389},
        ["MG"] = new[] {378,379,380,381,382,383,384,385,386,387,388,389},
        ["SP"] = new[] {428,429,430,431,432,433,434,435,436,437,438,439},
        ["SG"] = new[] {428,429,430,431,432,433,434,435,436,437,438,439},
    };
```

Then, in `BuildSheetAS` (around line 375), replace:

```csharp
        string[] codes = { "T", "AS", "EP", "EG", "N", "L", "W" };
```

with:

```csharp
        string[] codes = { "T", "AS", "EP", "EG", "MP", "MG", "SP", "SG", "N", "L", "W" };
```

(`AsColType`, defined a few lines above `_asCourseIds`, is unchanged — same reasoning as Task 2/3.)

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~ReportExportServiceAsMathScienceTests"
```

Expected: 1 passed, 0 failed.

- [ ] **Step 5: Run the existing PH/PSJ export tests to confirm no regression**

```bash
dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~ReportExportServiceStructureTests"
```

Expected: the 2 non-`[Explicit]` tests (`GroupByRegion_*`) pass; the `[Explicit]`-tagged ones report skipped (not run) unless you have the assumed 115年第1週 PH/PSJ dev data — this is expected NUnit behavior, not a failure.

- [ ] **Step 6: Commit**

```bash
git add source/portal/Portal/Services/ReportExportService.cs \
        source/portal/Test/Services/ReportExportServiceAsMathScienceTests.cs
git commit -m "feat: export AS 數學班/理化班 columns in ReportExportService.BuildSheetAS"
```

---

### Task 5: `StudentPopulationController.cs` — single-school export + new-lost consistency check

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes: `Course` rows 378–477 (Task 1). Does not consume Task 2/3/4 code directly (this file maintains its own duplicate `_asCourseIds` copy, per the project's established "3 places, each maintains its own static copy" convention documented in `source/CLAUDE.md`).
- Produces: nothing consumed by later tasks — this is the last task in this plan.

This controller's export action and `CheckNewLostConsistency` are both embedded inside a large MVC controller (`MvcController<PortalUser, Model, Culture>` subclass) with no existing test coverage anywhere in this codebase (`grep`-verified: zero tests instantiate any `Controller` subtype in this project — every precedent tests plain/static classes and defers controller-level behavior to the manual browser-verification pass). This task follows that same established convention: mechanical changes, verified by build success and manual code review, with browser verification deferred per the Global Constraints.

- [ ] **Step 1: Update `_asCourseIds`**

In `source/portal/Portal/Controllers/StudentPopulationController.cs`, replace the existing `_asCourseIds` dictionary (around line 53):

```csharp
        private static readonly Dictionary<string, int[]> _asCourseIds = new() {
            ["AS"] = new[]{245,246,247,248,249,250,251,252,253,254,255,256},
            ["EP"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
            ["EG"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
            ["N"]  = new[]{271,272,273,274,275,276,277,278,279,280,281,282},
            ["L"]  = new[]{283,284,285,286,287,288,289,290,291,292,293,294},
            ["W"]  = new[]{259,260,261,262,263,264,265,266,267,268,269,270},
            ["MP"] = new[]{378,379,380,381,382,383,384,385,386,387,388,389},
            ["MG"] = new[]{378,379,380,381,382,383,384,385,386,387,388,389},
            ["SP"] = new[]{428,429,430,431,432,433,434,435,436,437,438,439},
            ["SG"] = new[]{428,429,430,431,432,433,434,435,436,437,438,439},
        };
```

- [ ] **Step 2: Update the `codes` array in the single-school export action**

Around line 1933, replace:

```csharp
                        : new[] { "T", "AS", "EP", "EG", "N", "L", "W" };
```

with:

```csharp
                        : new[] { "T", "AS", "EP", "EG", "MP", "MG", "SP", "SG", "N", "L", "W" };
```

(Leave the PSJ branch of this ternary — `new[] { "T", "MP", "MS", "SP", "SS", "N", "L", "W" }` — completely untouched; PSJ's `MP`/`SP` mean something different there and are out of scope.)

- [ ] **Step 3: Add the two `CheckByDiffItem` calls**

Around line 1812, replace:

```csharp
                case StudentPopulationType.AfterSchool:
                    CheckByDiffItem("安親課輔", "安親課輔班班分析");
                    CheckByDiffItem("英文班", "英文班分析");
                    break;
```

with:

```csharp
                case StudentPopulationType.AfterSchool:
                    CheckByDiffItem("安親課輔", "安親課輔班班分析");
                    CheckByDiffItem("英文班", "英文班分析");
                    CheckByDiffItem("數學班", "數學班分析");
                    CheckByDiffItem("理化班", "理化班分析");
                    break;
```

- [ ] **Step 4: Build and confirm 0 errors**

```bash
dotnet build source/portal/PHStatistics.portal.sln
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 5: Run the full test suite to confirm no regression**

```bash
dotnet test source/portal/Test/Test.csproj
```

Expected: all non-`[Explicit]` tests pass; `[Explicit]`-tagged fixtures (including all 3 new test classes from Tasks 2–4) report as skipped by default.

- [ ] **Step 6: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: export AS 數學班/理化班 columns and check new/lost consistency in StudentPopulationController"
```

---

## After this plan

Per the Global Constraints, this plan does **not** include:
- Manual browser verification of `CreateASPopulation` (will now show 2 new course groups automatically)
- Phase 2 (the new grid input page) — start that with a fresh `superpowers:brainstorming` pass off `docs/superpowers/specs/2026-07-20-as-math-science-course-model-design.md`'s "Out of scope" section
