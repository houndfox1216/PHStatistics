# Aggregation Engine + GEPT Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a shared, data-driven `AggregationEngine` that replaces the hardcoded per-type if/else in `StudentPopulationController.SumPHPopulation`, add an Admin UI to configure its rules per course, and migrate the first (simplest) population type — GEPT — onto it end to end.

**Architecture:** A new `AggregationEngine` class reads `Course.StatisticsType` plus `SourceDepartmentIds`/`SourceCourseIds`/`ApplicableClassType`/`GroupByClassType` (all existing DB columns, added by a prior migration but never wired to any live code) to compute the value of an `IsSum=true` course's item. Cross-year lookups are injected via a delegate so the engine has zero dependency on `DataContext` and can be unit tested with plain in-memory objects. `SumPHPopulation`'s GEPT branch is replaced with a single call into the engine once GEPT's 12 summary courses are configured and verified against real data.

**Tech Stack:** ASP.NET Core 8 MVC, Entity Framework Core 8, NUnit 4, DevExtreme.AspNet.Core (Admin grids), SQL Server (`sqlcmd`).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-14-aggregation-extraction-design.md` (as amended 2026-07-15 with `LastWeekValue`/`LastYearValue`)
- No database schema changes — every column the engine reads already exists (`schema/Data/Migrations/20260120174156_AddCourseStatisticsFields.cs`)
- This plan covers ONLY: the engine, its unit tests, the Admin rule-configuration UI, and the GEPT migration. PS/PSJ/AfterSchool/PH follow the same pattern in separate future plans — do not touch their branches in `SumPHPopulation`.
- Dev DB connection used for verification steps: `sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -d NewPAS07 -U sa -P 'cloudfun@12' -C` (matches `source/portal/Portal/appsettings.json`)
- Follow existing code style: no XML doc comments unless the WHY is non-obvious, `Include("string.path")` style EF includes (matches the rest of `StudentPopulationController.cs`), 4-space indentation matching surrounding files.

---

### Task 1: Wire the Test project to the Portal assembly

**Files:**
- Modify: `source/portal/Test/Test.csproj`

**Interfaces:**
- Produces: `Test.csproj` can reference any public type in `PHStatistics.Portal` (Portal.csproj) and transitively `PHStatistics.Content`/`PHStatistics.Migrations` (Data.csproj) — needed by every later task in this plan.

Test.csproj currently has zero project references (its only existing file, `Page.cs`, is a Selenium page-object helper, not a unit test). This is the first task in the repo to add real unit tests, so we need to add the reference.

- [ ] **Step 1: Add the project reference**

Open `source/portal/Test/Test.csproj` and add a new `ItemGroup` right before the closing `</Project>` tag:

```xml
  <ItemGroup>
    <ProjectReference Include="..\Portal\Portal.csproj" />
  </ItemGroup>
```

- [ ] **Step 2: Verify the test project still builds**

Run: `dotnet build source/portal/Test/Test.csproj -c Debug`
Expected: `0 Error(s)` in the output (warnings are fine).

- [ ] **Step 3: Commit**

```bash
git add source/portal/Test/Test.csproj
git commit -m "test: reference Portal project from Test.csproj for upcoming AggregationEngine unit tests"
```

---

### Task 2: AggregationEngine — sum/count operations

**Files:**
- Create: `source/portal/Portal/Services/Aggregation/AggregationEngine.cs`
- Test: `source/portal/Test/Services/Aggregation/AggregationEngineTests.cs`

**Interfaces:**
- Produces:
  - `namespace PHStatistics.Portal.Services.Aggregation`
  - `class AggregationEngine`
  - `AggregationEngine(Func<int year, int week, int schoolId, StudentPopulationType type, StudentPopulation> lookupLastYearPopulation)` — constructor. The delegate looks up "the population for this year/week/school/type" (used later, in Task 3, for `DiffWithLastYear`/`LastYearValue`); Task 2's tests can just pass `(y, w, s, t) => null` since none of this task's `StatisticsType` values need it.
  - `void Calculate(StudentPopulationItem item, StudentPopulation population)` — computes `item.Number` in place for one `IsSum=true` item. No-op if `item.Class?.Course` is null, not `IsSum`, or `StatisticsType` is `null`/`None`/`ManualInput`.
  - `void CalculateAll(StudentPopulation population)` — runs `Calculate` for every `IsSum=true` item in `population.Items`, ordered by `Course.Ordinal`.
- Consumes: `PHStatistics.Content.Course`, `StudentPopulationItem`, `StudentPopulation`, `ClassType`, `StatisticsType` (all in Data.csproj, already referenced transitively via Portal.csproj).

This task covers the `Sum*`/`Count*` family. Diff/LastWeek/LastYear values and the unsupported-type error path are Task 3.

- [ ] **Step 1: Write the failing tests for `SumByDepartment` and `SumBySourceDepartments`**

Create `source/portal/Test/Services/Aggregation/AggregationEngineTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Aggregation;

namespace PHStatistics.Portal.Test.Services.Aggregation;

[TestFixture]
public class AggregationEngineTests {
    private static Course MakeCourse(int id, int? departmentId = null, bool isSum = false,
        StatisticsType? statisticsType = null, string sourceDepartmentIds = null, string sourceCourseIds = null,
        bool groupByClassType = false, ClassType? applicableClassType = null, int ordinal = 0) {
        return new Course {
            Id = id,
            DepartmentId = departmentId,
            IsSum = isSum,
            StatisticsType = statisticsType,
            SourceDepartmentIds = sourceDepartmentIds,
            SourceCourseIds = sourceCourseIds,
            GroupByClassType = groupByClassType,
            ApplicableClassType = applicableClassType,
            Ordinal = ordinal,
        };
    }

    private static StudentPopulationItem MakeItem(Course course, ClassType classType, int number, int lastWeekNumber = 0) {
        return new StudentPopulationItem {
            Class = new Class { CourseId = course.Id, Course = course, Type = classType },
            Number = number,
            LastWeekNumber = lastWeekNumber,
            IsSum = course.IsSum,
        };
    }

    private static AggregationEngine MakeEngine(
        System.Func<int, int, int, StudentPopulationType, StudentPopulation> lookupLastYearPopulation = null) {
        return new AggregationEngine(lookupLastYearPopulation ?? ((y, w, s, t) => null));
    }

    [Test]
    public void Calculate_SumByDepartment_SumsNonSumItemsInSameDepartment() {
        var courseA1 = MakeCourse(101, departmentId: 1);
        var courseA2 = MakeCourse(102, departmentId: 1);
        var courseB1 = MakeCourse(103, departmentId: 2);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.SumByDepartment);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> {
                MakeItem(courseA1, ClassType.General, 5),
                MakeItem(courseA2, ClassType.General, 7),
                MakeItem(courseB1, ClassType.General, 100), // different department, must be ignored
                sumItem,
            },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(12));
    }

    [Test]
    public void Calculate_SumBySourceDepartments_UsesExplicitDepartmentListInsteadOfOwnDepartment() {
        var course1 = MakeCourse(101, departmentId: 1);
        var course2 = MakeCourse(102, departmentId: 2);
        var course3 = MakeCourse(103, departmentId: 3); // not in the source list, must be ignored
        var sumCourse = MakeCourse(199, departmentId: 9, isSum: true,
            statisticsType: StatisticsType.SumBySourceDepartments, sourceDepartmentIds: "[1,2]");
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 10),
                MakeItem(course2, ClassType.General, 20),
                MakeItem(course3, ClassType.General, 999),
                sumItem,
            },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(30));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail to compile (AggregationEngine doesn't exist yet)**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`
Expected: build FAILS with `The type or namespace name 'AggregationEngine' could not be found` (or similar) — this confirms the test file is wired up and we're about to make it pass.

- [ ] **Step 3: Write the minimal engine implementation for these two cases**

Create `source/portal/Portal/Services/Aggregation/AggregationEngine.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Aggregation;

public class AggregationEngine {
    private readonly Func<int, int, int, StudentPopulationType, StudentPopulation> _lookupPopulation;

    public AggregationEngine(Func<int, int, int, StudentPopulationType, StudentPopulation> lookupPopulation) {
        _lookupPopulation = lookupPopulation;
    }

    public void CalculateAll(StudentPopulation population) {
        if (population?.Items == null) return;
        foreach (var item in population.Items.Where(i => i.IsSum).OrderBy(i => i.Class?.Course?.Ordinal ?? 0).ToList()) {
            Calculate(item, population);
        }
    }

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
            default:
                throw new NotSupportedException(
                    $"AggregationEngine 尚未支援 StatisticsType.{type.Value}（課程 {course.Id} {course.Name}）。");
        }
    }

    // 篩選優先序：SourceDepartmentIds > SourceCourseIds > 課程自身 DepartmentId；
    // 班別篩選：ApplicableClassType（固定班別）優先於 GroupByClassType（用 contextItem 自己的班別）
    private static IEnumerable<StudentPopulationItem> GetSourceItems(
        Course course, StudentPopulationItem contextItem, IEnumerable<StudentPopulationItem> items) {
        var query = items.Where(i => i.Class?.Course?.IsSum != true);

        if (!string.IsNullOrEmpty(course.SourceDepartmentIds)) {
            var ids = ParseIntArray(course.SourceDepartmentIds);
            query = query.Where(i => i.Class?.Course?.DepartmentId != null && ids.Contains(i.Class.Course.DepartmentId.Value));
        }
        else if (!string.IsNullOrEmpty(course.SourceCourseIds)) {
            var ids = ParseIntArray(course.SourceCourseIds);
            query = query.Where(i => i.Class?.CourseId != null && ids.Contains(i.Class.CourseId.Value));
        }
        else if (course.DepartmentId.HasValue) {
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

    private static List<int> ParseIntArray(string json) {
        try { return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>(); }
        catch { return new List<int>(); }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`
Expected: `Passed! - Failed: 0, Passed: 2, ...`

- [ ] **Step 5: Add the `SumBySourceCourses` and `CountClasses` tests, then extend the switch**

Append to `AggregationEngineTests.cs`:

```csharp
    [Test]
    public void Calculate_SumBySourceCourses_UsesExplicitCourseListInsteadOfOwnDepartment() {
        var course1 = MakeCourse(101, departmentId: 1);
        var course2 = MakeCourse(102, departmentId: 2);
        var course3 = MakeCourse(103, departmentId: 2); // same department as course2 but not in the source list
        var sumCourse = MakeCourse(199, departmentId: 9, isSum: true,
            statisticsType: StatisticsType.SumBySourceCourses, sourceCourseIds: "[101,102]");
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PS,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 3),
                MakeItem(course2, ClassType.General, 4),
                MakeItem(course3, ClassType.General, 999),
                sumItem,
            },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(7));
    }

    [Test]
    public void Calculate_CountClasses_CountsItemsWithNumberGreaterThanZero() {
        var course1 = MakeCourse(101, departmentId: 1);
        var course2 = MakeCourse(102, departmentId: 1);
        var course3 = MakeCourse(103, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.CountClasses);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 5),
                MakeItem(course2, ClassType.General, 0),
                MakeItem(course3, ClassType.General, 3),
                sumItem,
            },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(2));
    }
```

- [ ] **Step 6: Run the tests to verify exactly one fails**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`
Expected: `Failed: 1, Passed: 3`. `Calculate_SumBySourceCourses_...` passes already — `SumBySourceCourses` was included in the Step 3 switch's `Sum` case group from the start (it shares the exact same `GetSourceItems(...).Sum(...)` code path as `SumByDepartment`/`SumBySourceDepartments`, so no new implementation is needed for it; this test exists to pin that shared behavior down explicitly). `Calculate_CountClasses_...` fails with `NotSupportedException`, since `CountClasses` isn't in the switch yet — that's the one Step 7 implements.

- [ ] **Step 7: Add the `CountClasses`/`CountClassesByClassType` case to the switch**

In `AggregationEngine.cs`, add this case right after the four `Sum*` cases (before `default`):

```csharp
            case StatisticsType.CountClasses:
            case StatisticsType.CountClassesByClassType:
                item.Number = GetSourceItems(course, item, population.Items).Count(i => i.Number > 0);
                break;
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`
Expected: `Passed! - Failed: 0, Passed: 4, ...`

- [ ] **Step 9: Commit**

```bash
git add source/portal/Portal/Services/Aggregation/AggregationEngine.cs source/portal/Test/Services/Aggregation/AggregationEngineTests.cs
git commit -m "feat: add AggregationEngine sum/count operations with unit tests"
```

---

### Task 3: AggregationEngine — diff/last-week/last-year, filters, and CalculateAll

**Files:**
- Modify: `source/portal/Portal/Services/Aggregation/AggregationEngine.cs`
- Modify: `source/portal/Test/Services/Aggregation/AggregationEngineTests.cs`

**Interfaces:**
- Consumes: `AggregationEngine`, `GetSourceItems` (private, from Task 2) — this task extends the same class/file.
- Produces: full `Calculate`/`CalculateAll` behavior the GEPT migration (Task 7) depends on: `DiffWithLastWeek`, `LastWeekValue`, `DiffWithLastYear`, `LastYearValue`, `ManualInput`/`None` no-ops, `GroupByClassType`/`ApplicableClassType` filtering, and a `NotSupportedException` for any `StatisticsType` not implemented.

- [ ] **Step 1: Write the failing tests for `DiffWithLastWeek` and `LastWeekValue`**

Append to `AggregationEngineTests.cs`:

```csharp
    [Test]
    public void Calculate_DiffWithLastWeek_SubtractsLastWeekSumFromThisWeekSum() {
        var course1 = MakeCourse(101, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.DiffWithLastWeek);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 15, lastWeekNumber: 10),
                sumItem,
            },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(5));
    }

    [Test]
    public void Calculate_LastWeekValue_SumsLastWeekNumberOnly() {
        var course1 = MakeCourse(101, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.LastWeekValue);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 999, lastWeekNumber: 42),
                sumItem,
            },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(42));
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`
Expected: `Failed: 2` with `NotSupportedException`.

- [ ] **Step 3: Add the `DiffWithLastWeek` and `LastWeekValue` cases**

In `AggregationEngine.cs`, add after the `CountClasses`/`CountClassesByClassType` case:

```csharp
            case StatisticsType.LastWeekValue:
                item.Number = GetSourceItems(course, item, population.Items).Sum(i => i.LastWeekNumber);
                break;
            case StatisticsType.DiffWithLastWeek: {
                var src = GetSourceItems(course, item, population.Items);
                item.Number = src.Sum(i => i.Number) - src.Sum(i => i.LastWeekNumber);
                break;
            }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`
Expected: `Passed! - Failed: 0, Passed: 6, ...`

- [ ] **Step 5: Write the failing tests for `LastYearValue` and `DiffWithLastYear` (the delegate-based cross-year lookup)**

Append to `AggregationEngineTests.cs`:

```csharp
    [Test]
    public void Calculate_LastYearValue_ReturnsRawLastYearSumViaLookupDelegate() {
        var course1 = MakeCourse(101, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.LastYearValue);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> { sumItem },
        };

        var lastYearPopulation = new StudentPopulation {
            Year = 2025, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> { MakeItem(course1, ClassType.General, 77) },
        };

        var engine = MakeEngine((year, week, schoolId, type) => {
            Assert.That(year, Is.EqualTo(2025));
            Assert.That(week, Is.EqualTo(10));
            Assert.That(schoolId, Is.EqualTo(1));
            Assert.That(type, Is.EqualTo(StudentPopulationType.GEPT));
            return lastYearPopulation;
        });

        engine.Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(77));
    }

    [Test]
    public void Calculate_LastYearValue_ReturnsZeroWhenNoLastYearPopulationExists() {
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.LastYearValue);
        var sumItem = MakeItem(sumCourse, ClassType.General, 5);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> { sumItem },
        };

        MakeEngine((y, w, s, t) => null).Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(0));
    }

    [Test]
    public void Calculate_DiffWithLastYear_SubtractsLastYearSumFromThisWeekSum() {
        var course1 = MakeCourse(101, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.DiffWithLastYear);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 50),
                sumItem,
            },
        };

        var lastYearPopulation = new StudentPopulation {
            Year = 2025, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> { MakeItem(course1, ClassType.General, 30) },
        };

        var engine = MakeEngine((y, w, s, t) => lastYearPopulation);
        engine.Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(20));
    }
```

- [ ] **Step 6: Run the tests to verify they fail**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`
Expected: `Failed: 3` with `NotSupportedException`.

- [ ] **Step 7: Add a `SumLastYear` helper and the `LastYearValue`/`DiffWithLastYear` cases**

In `AggregationEngine.cs`, add this private method after `GetSourceItems`:

```csharp
    private int SumLastYear(Course course, StudentPopulationItem item, StudentPopulation population) {
        if (population.SchoolId == null) return 0;
        var lastYearPopulation = _lookupPopulation(population.Year - 1, population.Week, population.SchoolId.Value, population.Type);
        if (lastYearPopulation?.Items == null) return 0;
        return GetSourceItems(course, item, lastYearPopulation.Items).Sum(i => i.Number);
    }
```

And add these cases to the switch, after `DiffWithLastWeek`:

```csharp
            case StatisticsType.LastYearValue:
                item.Number = SumLastYear(course, item, population);
                break;
            case StatisticsType.DiffWithLastYear: {
                int thisWeek = GetSourceItems(course, item, population.Items).Sum(i => i.Number);
                item.Number = thisWeek - SumLastYear(course, item, population);
                break;
            }
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`
Expected: `Passed! - Failed: 0, Passed: 9, ...`

- [ ] **Step 9: Write the failing tests for `ManualInput`, non-sum no-op, `GroupByClassType`, `ApplicableClassType`, unsupported type, and `CalculateAll`**

Append to `AggregationEngineTests.cs`:

```csharp
    [Test]
    public void Calculate_ManualInput_LeavesNumberUnchanged() {
        var sumCourse = MakeCourse(199, isSum: true, statisticsType: StatisticsType.ManualInput);
        var sumItem = MakeItem(sumCourse, ClassType.General, 42);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> { sumItem },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(42));
    }

    [Test]
    public void Calculate_NonSumCourse_DoesNothing() {
        var course = MakeCourse(101, isSum: false);
        var item = MakeItem(course, ClassType.General, 8);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> { item },
        };

        MakeEngine().Calculate(item, population);

        Assert.That(item.Number, Is.EqualTo(8));
    }

    [Test]
    public void Calculate_GroupByClassType_OnlySumsSameClassTypeAsContextItem() {
        var course1 = MakeCourse(101, departmentId: 1);
        var course2 = MakeCourse(102, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true,
            statisticsType: StatisticsType.SumByDepartmentAndClassType, groupByClassType: true);
        var subGroupSumItem = MakeItem(sumCourse, ClassType.SubGroup, 0);
        var v3SumItem = MakeItem(sumCourse, ClassType.V3, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.SubGroup, 4),
                MakeItem(course2, ClassType.V3, 9),
                subGroupSumItem,
                v3SumItem,
            },
        };

        var engine = MakeEngine();
        engine.Calculate(subGroupSumItem, population);
        engine.Calculate(v3SumItem, population);

        Assert.That(subGroupSumItem.Number, Is.EqualTo(4));
        Assert.That(v3SumItem.Number, Is.EqualTo(9));
    }

    [Test]
    public void Calculate_ApplicableClassType_FiltersRegardlessOfContextItemOwnClassType() {
        var course1 = MakeCourse(101, departmentId: 1);
        var course2 = MakeCourse(102, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true,
            statisticsType: StatisticsType.SumByDepartment, applicableClassType: ClassType.SubGroup);
        // context item's own type is V3, but ApplicableClassType=SubGroup must win
        var sumItem = MakeItem(sumCourse, ClassType.V3, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.SubGroup, 6),
                MakeItem(course2, ClassType.V3, 50),
                sumItem,
            },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(6));
    }

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

    [Test]
    public void CalculateAll_ProcessesEverySumItemAndSkipsManualInput() {
        var course1 = MakeCourse(101, departmentId: 1);
        var sumCourseA = MakeCourse(198, departmentId: 1, isSum: true, statisticsType: StatisticsType.SumByDepartment, ordinal: 2);
        var sumCourseB = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.ManualInput, ordinal: 1);
        var baseItem = MakeItem(course1, ClassType.General, 9);
        var sumItemA = MakeItem(sumCourseA, ClassType.General, 0);
        var sumItemB = MakeItem(sumCourseB, ClassType.General, 3);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> { baseItem, sumItemA, sumItemB },
        };

        MakeEngine().CalculateAll(population);

        Assert.That(sumItemA.Number, Is.EqualTo(9));
        Assert.That(sumItemB.Number, Is.EqualTo(3)); // ManualInput must stay untouched
    }
```

- [ ] **Step 10: Run the tests to verify they pass without further implementation changes**

Run: `dotnet test source/portal/Test/Test.csproj --filter "FullyQualifiedName~AggregationEngineTests"`
Expected: `Passed! - Failed: 0, Passed: 15, ...` — every case in this batch is already covered by the Task 2/3 implementation, so this step should go green immediately. If anything fails, fix `AggregationEngine.cs` (not the tests) until it does.

- [ ] **Step 11: Commit**

```bash
git add source/portal/Portal/Services/Aggregation/AggregationEngine.cs source/portal/Test/Services/Aggregation/AggregationEngineTests.cs
git commit -m "feat: complete AggregationEngine with diff/last-week/last-year support and full test coverage"
```

---

### Task 4: Admin UI — expose aggregation rule fields on Course management

**Files:**
- Modify: `source/portal/Portal/Areas/Admin/Views/Course/Index.cshtml`

**Interfaces:**
- Consumes: existing `Course` entity fields (`StatisticsType`, `SourceDepartmentIds`, `SourceCourseIds`, `ApplicableClassType`, `GroupByClassType`) and existing `Areas/Admin/Controllers/CourseController.cs` `Create`/`Update` actions — both already do generic `JsonConvert.PopulateObject(values, data)` binding, so no controller changes are needed; adding grid columns is sufficient for the new fields to be editable and persisted.
- Produces: an editable Admin screen for the fields Task 5's data configuration will set — going forward, staff can configure the same fields here instead of running SQL.

Scope note: `SourceDepartmentIds`/`SourceCourseIds` are plain `nvarchar` JSON-array columns (e.g. `[14,15,16,17,18]`), not normalized relations, so this task uses a simple text input for them (the admin types a JSON array) rather than building a custom multi-select tag-box editor. That keeps this task small while still fully removing the "must edit C# and redeploy" requirement. A nicer multi-select UI is a reasonable fast-follow, not required here.

- [ ] **Step 1: Add the rule columns to the grid**

In `source/portal/Portal/Areas/Admin/Views/Course/Index.cshtml`, inside the existing `.Columns(columns => { ... })` block (currently ending after `columns.AddFor(m => m.Type).Caption("所屬單位").Width(100);`), add:

```csharp
                columns.AddFor(m => m.StatisticsType)
                    .Caption("加總類型")
                    .Lookup(lookup => lookup
                        .DataSource(new[] {
                            new { Id = 0,  Name = "使用者輸入" },
                            new { Id = 1,  Name = "同班系人數加總" },
                            new { Id = 2,  Name = "同班系+同班別人數加總" },
                            new { Id = 3,  Name = "指定來源班系人數加總" },
                            new { Id = 4,  Name = "指定來源課程人數加總" },
                            new { Id = 10, Name = "與上週相比" },
                            new { Id = 11, Name = "去年同期比較" },
                            new { Id = 30, Name = "班級數量統計" },
                            new { Id = 31, Name = "班級數量統計依班別" },
                            new { Id = 40, Name = "上週數值" },
                            new { Id = 41, Name = "去年同期數值" },
                            new { Id = 50, Name = "手動輸入" },
                        })
                        .ValueExpr("Id")
                        .DisplayExpr("Name")
                    );
                columns.AddFor(m => m.SourceDepartmentIds).Caption("來源班系Id (JSON陣列，如 [14,15,16])").Width(220);
                columns.AddFor(m => m.SourceCourseIds).Caption("來源課程Id (JSON陣列)").Width(180);
                columns.AddFor(m => m.ApplicableClassType)
                    .Caption("適用班別")
                    .Lookup(lookup => lookup
                        .DataSource(new[] {
                            new { Id = 0, Name = "團" },
                            new { Id = 1, Name = "EM1" },
                            new { Id = 2, Name = "EM2" },
                            new { Id = 3, Name = "三" },
                            new { Id = 4, Name = "小" },
                            new { Id = 5, Name = "一般" },
                        })
                        .ValueExpr("Id")
                        .DisplayExpr("Name")
                    );
                columns.AddFor(m => m.GroupByClassType).Caption("依班別分組").Width(90);
```

- [ ] **Step 2: Build the Portal project**

Run: `dotnet build source/portal/Portal/Portal.csproj -c Debug`
Expected: `0 Error(s)`.

- [ ] **Step 3: Manually verify the grid renders and edits persist**

This is a Razor/DevExtreme view — there's no automated test for it. Start the app (`dotnet run --project source/portal/Portal/Portal.csproj`), log in as a user with `SystemPermission.Course`, open `/Admin/Course`, and confirm:
- The new columns appear and the popup edit form includes them
- Editing a GEPT course's "加總類型" to e.g. "同班系人數加總" and saving does not error, and reloading the grid shows the saved value
- Typing `[14,15,16]` into "來源班系Id" and saving round-trips correctly

Record the result in the task notes before moving on; if the harness running this plan has no browser tool available, flag this step for manual verification alongside the other pending manual-verification items from earlier work (per this project's established pattern of batching browser verification).

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Areas/Admin/Views/Course/Index.cshtml
git commit -m "feat: expose aggregation rule fields (StatisticsType/SourceDepartmentIds/etc) on Admin course management"
```

---

### Task 5: Configure GEPT's 12 summary courses

**Files:**
- Create: `source/portal/docs/superpowers/sql/2026-07-15-gept-aggregation-rules-apply.sql`
- Create: `source/portal/docs/superpowers/sql/2026-07-15-gept-aggregation-rules-revert.sql`

**Interfaces:**
- Produces: correctly configured `Course.StatisticsType`/`SourceDepartmentIds` for GEPT's 12 `IsSum=true` courses — this is what Task 6's comparison and Task 7's engine cutover depend on.

GEPT (`StudentPopulationType.GEPT` = `Type=2`) has 12 `IsSum=true` courses across 6 departments. The rule mapping below was derived by reading the current live `SumPHPopulation` GEPT branch (`StudentPopulationController.cs:1466-1495` as of this plan) against real department membership queried from the dev DB:

| Course Id | Name | Department | StatisticsType | SourceDepartmentIds | Why |
|---|---|---|---|---|---|
| 71 | 複試班人數合計 | 複試達陣班 (14) | `SumByDepartment` (1) | — | own department, no other course in dept 14 is a summary row |
| 74 | 三十六週人數合計 | 三十六週攻略 (15) | `SumByDepartment` (1) | — | same pattern |
| 78 | 二十四週人數合計 | 二十四週攻略班 (16) | `SumByDepartment` (1) | — | same pattern |
| 82 | 十六回人數合計 | 先修養成班 (17) | `SumByDepartment` (1) | — | same pattern |
| 87 | 劍橋人數合計 | Cambrige (18) | `SumByDepartment` (1) | — | same pattern |
| 107 | 其他班系合計 | 其他班系 (21) | `SumByDepartment` (1) | — | same pattern |
| 88 | 本週英檢總人數 | 英檢統計 (19) | `SumBySourceDepartments` (3) | `[14,15,16,17,18]` | live code excludes department 21 ("其他班系") from this total |
| 89 | 上週英檢總人數 | 英檢統計 (19) | `LastWeekValue` (40) | `[14,15,16,17,18,21]` | live code has **no** department-21 exclusion here (unlike course 88) — preserved as-is; flagged for the school to confirm this asymmetry is intentional |
| 90 | 與上週相比 | 英檢統計 (19) | `DiffWithLastWeek` (10) | `[14,15,16,17,18,21]` | same scope as 89 |
| 91 | 去年同期人數 | 英檢分析 (20) | `LastYearValue` (41) | `[14,15,16,17,18,21]` | same scope as 89 |
| 92 | 去年同期/比 | 英檢分析 (20) | `DiffWithLastYear` (11) | `[14,15,16,17,18,21]` | same scope as 89 |
| 93 | 本週英檢新生人數 | 英檢分析 (20) | `ManualInput` (50) | — | 2026-07-14 decision: new/lost is always manual, never computed |
| 94 | 本週英檢流失人數 | 英檢分析 (20) | `ManualInput` (50) | — | same as 93 |

- [ ] **Step 1: Write the apply script**

Create `source/portal/docs/superpowers/sql/2026-07-15-gept-aggregation-rules-apply.sql`:

```sql
-- GEPT (StudentPopulationType.GEPT) aggregation rule configuration.
-- See docs/superpowers/plans/2026-07-15-aggregation-engine-and-gept-migration.md, Task 5, for the full mapping table and rationale.

UPDATE Course SET StatisticsType = 1 WHERE Id IN (71, 74, 78, 82, 87, 107); -- SumByDepartment

UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[14,15,16,17,18]' WHERE Id = 88; -- 本週英檢總人數 (excludes dept 21)

UPDATE Course SET StatisticsType = 40, SourceDepartmentIds = '[14,15,16,17,18,21]' WHERE Id = 89;  -- 上週英檢總人數
UPDATE Course SET StatisticsType = 10, SourceDepartmentIds = '[14,15,16,17,18,21]' WHERE Id = 90;  -- 與上週相比
UPDATE Course SET StatisticsType = 41, SourceDepartmentIds = '[14,15,16,17,18,21]' WHERE Id = 91;  -- 去年同期人數
UPDATE Course SET StatisticsType = 11, SourceDepartmentIds = '[14,15,16,17,18,21]' WHERE Id = 92;  -- 去年同期/比

UPDATE Course SET StatisticsType = 50 WHERE Id IN (93, 94); -- ManualInput (新生/流失)
```

Create `source/portal/docs/superpowers/sql/2026-07-15-gept-aggregation-rules-revert.sql`:

```sql
UPDATE Course
SET StatisticsType = NULL, SourceDepartmentIds = NULL
WHERE Id IN (71, 74, 78, 82, 87, 88, 89, 90, 91, 92, 93, 94, 107);
```

- [ ] **Step 2: Run the apply script against the dev DB**

Run:
```bash
sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -d NewPAS07 -U sa -P 'cloudfun@12' -C -i "source/portal/docs/superpowers/sql/2026-07-15-gept-aggregation-rules-apply.sql"
```
Expected: 8 `(N 個資料列受到影響)` messages, no errors.

- [ ] **Step 3: Verify the configuration landed correctly**

Run:
```bash
sqlcmd -S "CLOUDFUN-MSI-LE\SQLEXPRESS" -d NewPAS07 -U sa -P 'cloudfun@12' -C -Q "SELECT Id, Name, StatisticsType, SourceDepartmentIds FROM Course WHERE Id IN (71,74,78,82,87,88,89,90,91,92,93,94,107) ORDER BY Id" -W -s"|"
```
Expected: output matches the mapping table above exactly (12 rows, `StatisticsType` and `SourceDepartmentIds` per the table).

- [ ] **Step 4: Commit the scripts**

```bash
git add source/portal/docs/superpowers/sql/2026-07-15-gept-aggregation-rules-apply.sql source/portal/docs/superpowers/sql/2026-07-15-gept-aggregation-rules-revert.sql
git commit -m "data: configure GEPT's 12 summary courses with AggregationEngine rules"
```

---

### Task 6: Verify the engine against real GEPT data, then remove the temporary tool

**Files:**
- Modify: `source/portal/Portal/Areas/Admin/Controllers/StudentPopulationController.cs` (temporary action, added then removed within this task)

**Interfaces:**
- Consumes: `AggregationEngine` (Task 2/3), GEPT course configuration (Task 5)
- Produces: confidence that the engine reproduces every currently-stored GEPT summary number before Task 7 flips the live code path over. This task adds no permanent code — the action is removed in Step 4 below.

The most reliable ground truth for "does the new engine match the old logic" is the `Number` already stored on every existing GEPT `IsSum` item — it was last written by the current live `SumPHPopulation` code. Rather than re-implementing the old if/else a second time just to compare, this task runs the new engine (read-only, never saving) against every real GEPT population and diffs its output against what's already stored.

- [ ] **Step 1: Add the temporary comparison action**

In `source/portal/Portal/Areas/Admin/Controllers/StudentPopulationController.cs`, add `using PHStatistics.Portal.Services.Aggregation;` to the top of the file, and add this action inside the `StudentPopulationController` class (anywhere after `Index()` is fine, e.g. right before the `#region StudentPopulation CRUD` block):

```csharp
        // 暫時性端點，僅供 GEPT 加總引擎遷移驗證使用，驗證完成後即刪除（見 2026-07-15-aggregation-engine-and-gept-migration.md Task 6）
        [HttpGet]
        public IActionResult CompareGeptAggregation() {
            var populations = Model.DataContext.StudentPopulation
                .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .Where(p => p.Type == StudentPopulationType.GEPT && p.DataMode == DataMode.Normal)
                .ToList();

            var engine = new AggregationEngine((year, week, schoolId, type) =>
                Model.DataContext.StudentPopulation
                    .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                    .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type));

            var mismatches = new List<object>();
            foreach (var population in populations) {
                foreach (var item in population.Items.Where(i => i.IsSum).ToList()) {
                    int oldNumber = item.Number;
                    engine.Calculate(item, population);
                    if (item.Number != oldNumber) {
                        mismatches.Add(new {
                            PopulationId = population.Id,
                            population.SchoolId,
                            population.Year,
                            population.Week,
                            CourseId = item.Class.Course.Id,
                            CourseName = item.Class.Course.Name,
                            OldNumber = oldNumber,
                            NewNumber = item.Number,
                        });
                    }
                    item.Number = oldNumber; // 唯讀比對，還原避免誤動資料
                }
            }

            return Json(new { totalPopulations = populations.Count, mismatchCount = mismatches.Count, mismatches });
        }
```

- [ ] **Step 2: Build and run the comparison**

Run: `dotnet build source/portal/Portal/Portal.csproj -c Debug` — expect `0 Error(s)`.

Start the app and hit `GET /Admin/StudentPopulation/CompareGeptAggregation` (as a user with `SystemPermission.StudentPopulation`), or if browser access isn't available in this environment, note this as a manual step for whoever runs the app next.

- [ ] **Step 3: Interpret the result**

- `mismatchCount: 0` → the engine reproduces every stored GEPT summary number exactly. Proceed to Task 7.
- `mismatchCount > 0` → for each mismatch, check whether:
  - It's course 89/90/91/92 and the mismatch is explained by the department-21 asymmetry flagged in Task 5's table (i.e. old code's actual behavior differs from what's currently configured) — if so, adjust the `SourceDepartmentIds` for that course via the Admin UI (Task 4) or a follow-up SQL statement, re-run, and re-check.
  - It's a population whose `Number` predates any change made today and simply hasn't been recalculated since — cross-check against `UpdatedTime` before concluding it's a real discrepancy.
  - Anything else — stop and investigate before proceeding to Task 7; do not cut over GEPT while mismatches are unexplained.

- [ ] **Step 4: Remove the temporary action**

Delete the `CompareGeptAggregation` method (and the `using PHStatistics.Portal.Services.Aggregation;` line added in Step 1, if nothing else in this file needs it after Task 7 — check Task 7 first, since it modifies `StudentPopulationController.cs` in the `Controllers` namespace, a different file, so this `using` will indeed become unused here and should be removed).

Run: `dotnet build source/portal/Portal/Portal.csproj -c Debug` — expect `0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Areas/Admin/Controllers/StudentPopulationController.cs
git commit -m "chore: remove temporary GEPT aggregation comparison endpoint after verification"
```

---

### Task 7: Cut GEPT over to the AggregationEngine

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes: `AggregationEngine` (Task 2/3), verified GEPT configuration (Task 5, confirmed by Task 6)
- Produces: `SumPHPopulation`'s GEPT branch now delegates to the shared engine; PS/PSJ/AfterSchool/PH branches are untouched (out of scope — future plans).

- [ ] **Step 1: Add the using statement**

In `source/portal/Portal/Controllers/StudentPopulationController.cs`, add right after the existing `using PHStatistics.Portal.Services;` line:

```csharp
using PHStatistics.Portal.Services.Aggregation;
```

- [ ] **Step 2: Instantiate the engine once per `SumPHPopulation` call**

Right after `dataContext.ChangeTracker.Clear();` (inside `public StudentPopulation SumPHPopulation(long spId) {`), add:

```csharp
            var aggregationEngine = new AggregationEngine((year, week, schoolId, type) =>
                dataContext.StudentPopulation.Include("Items.Class.Course")
                    .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type));
```

- [ ] **Step 3: Replace the GEPT branch body**

Find the GEPT branch (currently reads, in full, from `else if (studentPopulationData.Type == StudentPopulationType.GEPT) {` through its matching closing `}` — includes the `continue`-guard added in the 2026-07-15 zero-out bugfix, the `classItems` default sum, and the five named `if`/`else if` overrides for 本週英檢總人數/上週英檢總人數/與上週相比/去年同期人數/去年同期/比). Replace the entire block with:

```csharp
                    else if (studentPopulationData.Type == StudentPopulationType.GEPT) {
                        aggregationEngine.Calculate(group, studentPopulationData);
                    }
```

- [ ] **Step 4: Build**

Run: `dotnet build source/portal/Portal/Portal.csproj -c Debug`
Expected: `0 Error(s)`.

- [ ] **Step 5: Re-run the Task 6 comparison one more time to confirm nothing regressed**

If the temporary action from Task 6 was already removed, temporarily re-add it (same code as Task 6 Step 1), hit `GET /Admin/StudentPopulation/CompareGeptAggregation` again, confirm `mismatchCount: 0`, then remove it again (mirroring Task 6 Steps 4-5). This double-checks that swapping the actual call site didn't introduce a discrepancy versus the isolated Task 6 check.

- [ ] **Step 6: Manual browser verification**

This changes a live save-path for every GEPT population. Per this project's established verification pattern, batch this with the other pending manual-verification items already noted in project memory (login session continuation, the GEPT/PS/PSJ/AS new/lost input fields) rather than verifying in isolation:
- Open a GEPT population, edit a regular (non-summary) course's number, save
- Confirm 本週英檢總人數/上週英檢總人數/與上週相比/去年同期人數/去年同期/比 all recompute to the expected values
- Confirm 本週英檢新生人數/本週英檢流失人數 are NOT reset (this is the exact bug fixed earlier — re-verify it stays fixed through the engine swap)

- [ ] **Step 7: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "refactor: migrate GEPT aggregation to the shared AggregationEngine"
```

---

## Self-Review Notes

**Spec coverage:** Task 2/3 implement every `StatisticsType` the amended spec lists (`SumByDepartment`, `SumByDepartmentAndClassType`, `SumBySourceDepartments`, `SumBySourceCourses`, `CountClasses`, `CountClassesByClassType`, `DiffWithLastWeek`, `DiffWithLastYear`, `LastWeekValue`, `LastYearValue`, `ManualInput`/`None`). Task 4 covers the Admin UI section of the spec. Task 5-7 cover the "GEPT first" rollout step. The spec's "Out of scope" items (cooperative cross-school counting, course ordering) are correctly not addressed here. `ReportExportService.ComputeIsumValue`/`InferStatisticsType` and `StatisticsCalculationService.cs` deletion is explicitly deferred to after all 5 types migrate (per spec) — not part of this plan.

**Type consistency:** `AggregationEngine(Func<int,int,int,StudentPopulationType,StudentPopulation>)` constructor signature is used identically in Task 2/3 tests, Task 6's temporary action, and Task 7's real wiring.
