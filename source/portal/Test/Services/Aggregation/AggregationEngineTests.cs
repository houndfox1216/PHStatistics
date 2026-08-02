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
        System.Func<int, int, int, StudentPopulationType, StudentPopulation> lookupLastYearPopulation = null,
        System.Func<StudentPopulation, StudentPopulation> lookupLastWeekPopulation = null) {
        return new AggregationEngine(
            lookupLastYearPopulation ?? ((y, w, s, t) => null),
            lookupLastWeekPopulation ?? (p => null));
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

    [Test]
    public void Calculate_DiffWithLastWeek_SubtractsLastWeekPopulationSumFromThisWeekSum() {
        var course1 = MakeCourse(101, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.DiffWithLastWeek);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 15),
                sumItem,
            },
        };
        var lastWeekPopulation = new StudentPopulation {
            Year = 2026, Week = 9, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> { MakeItem(course1, ClassType.General, 10) },
        };

        var engine = MakeEngine(lookupLastWeekPopulation: p => lastWeekPopulation);
        engine.Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(5));
    }

    [Test]
    public void Calculate_LastWeekValue_ReturnsRawLastWeekSumViaLookupDelegate() {
        var course1 = MakeCourse(101, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.LastWeekValue);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> {
                MakeItem(course1, ClassType.General, 999),
                sumItem,
            },
        };
        var lastWeekPopulation = new StudentPopulation {
            Year = 2026, Week = 9, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> { MakeItem(course1, ClassType.General, 42) },
        };

        var engine = MakeEngine(lookupLastWeekPopulation: p => lastWeekPopulation);
        engine.Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(42));
    }

    [Test]
    public void Calculate_LastWeekValue_ReturnsZeroWhenNoLastWeekPopulationExists() {
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.LastWeekValue);
        var sumItem = MakeItem(sumCourse, ClassType.General, 5);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> { sumItem },
        };

        MakeEngine().Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(0));
    }

    // 迴歸測試：分校本週把上週有人數的班級整列刪除（非改成0人）後，「上週人數」不能跟著消失——
    // 這正是本次修復要根治的 production bug：改成直接讀上週實際存的population，不再依賴本週項目的LastWeekNumber快取。
    [Test]
    public void Calculate_LastWeekValue_UnaffectedByThisWeekItemBeingDeleted() {
        var course1 = MakeCourse(101, departmentId: 1);
        var sumCourse = MakeCourse(199, departmentId: 1, isSum: true, statisticsType: StatisticsType.LastWeekValue);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);

        // 本週該班級已被使用者整列刪除，population.Items 裡完全沒有 course1 的項目
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> { sumItem },
        };
        var lastWeekPopulation = new StudentPopulation {
            Year = 2026, Week = 9, SchoolId = 1, Type = StudentPopulationType.GEPT,
            Items = new List<StudentPopulationItem> { MakeItem(course1, ClassType.General, 42) },
        };

        var engine = MakeEngine(lookupLastWeekPopulation: p => lastWeekPopulation);
        engine.Calculate(sumItem, population);

        Assert.That(sumItem.Number, Is.EqualTo(42));
    }

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
        var sumCourse = MakeCourse(199, isSum: true, statisticsType: StatisticsType.SumAll);
        var sumItem = MakeItem(sumCourse, ClassType.General, 0);
        var population = new StudentPopulation {
            Year = 2026, Week = 10, SchoolId = 1, Type = StudentPopulationType.PH,
            Items = new List<StudentPopulationItem> { sumItem },
        };

        var engine = MakeEngine();

        Assert.Throws<NotSupportedException>(() => engine.Calculate(sumItem, population));
    }

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
}
