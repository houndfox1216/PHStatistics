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
}
