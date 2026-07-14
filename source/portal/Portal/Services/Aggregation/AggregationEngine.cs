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
            case StatisticsType.CountClasses:
            case StatisticsType.CountClassesByClassType:
                item.Number = GetSourceItems(course, item, population.Items).Count(i => i.Number > 0);
                break;
            case StatisticsType.LastWeekValue:
                item.Number = GetSourceItems(course, item, population.Items).Sum(i => i.LastWeekNumber);
                break;
            case StatisticsType.DiffWithLastWeek: {
                var src = GetSourceItems(course, item, population.Items);
                item.Number = src.Sum(i => i.Number) - src.Sum(i => i.LastWeekNumber);
                break;
            }
            case StatisticsType.LastYearValue:
                item.Number = SumLastYear(course, item, population);
                break;
            case StatisticsType.DiffWithLastYear: {
                int thisWeek = GetSourceItems(course, item, population.Items).Sum(i => i.Number);
                item.Number = thisWeek - SumLastYear(course, item, population);
                break;
            }
            default:
                throw new NotSupportedException(
                    $"AggregationEngine 尚未支援 StatisticsType.{type.Value}（課程 {course.Id} {course.Name}）。");
        }
    }

    private int SumLastYear(Course course, StudentPopulationItem item, StudentPopulation population) {
        if (population.SchoolId == null) return 0;
        var lastYearPopulation = _lookupPopulation(population.Year - 1, population.Week, population.SchoolId.Value, population.Type);
        if (lastYearPopulation?.Items == null) return 0;
        return GetSourceItems(course, item, lastYearPopulation.Items).Sum(i => i.Number);
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
