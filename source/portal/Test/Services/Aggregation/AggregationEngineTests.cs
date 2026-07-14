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
}
