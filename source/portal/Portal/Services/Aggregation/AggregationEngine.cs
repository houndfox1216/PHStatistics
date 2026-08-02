using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Aggregation;

public class AggregationEngine {
    private readonly Func<int, int, int, StudentPopulationType, StudentPopulation> _lookupPopulation;
    private readonly Func<StudentPopulation, StudentPopulation> _lookupLastWeekPopulation;

    public AggregationEngine(
        Func<int, int, int, StudentPopulationType, StudentPopulation> lookupPopulation,
        Func<StudentPopulation, StudentPopulation> lookupLastWeekPopulation) {
        _lookupPopulation = lookupPopulation;
        _lookupLastWeekPopulation = lookupLastWeekPopulation;
    }

    public void CalculateAll(StudentPopulation population) {
        if (population?.Items == null) return;
        foreach (var item in population.Items.Where(i => i.IsSum).OrderBy(i => i.Class?.Course?.Ordinal ?? 0).ToList()) {
            Calculate(item, population);
        }
    }

    public void Calculate(StudentPopulationItem item, StudentPopulation population) {
        if (item.IsManual) return;
        var course = item.Class?.Course;
        if (course == null || !course.IsSum) return;

        var type = course.StatisticsType;
        if (type == null || type == StatisticsType.None || type == StatisticsType.ManualInput) return;

        item.Number = Compute(item, population, type.Value, course);
    }

    public int? Preview(StudentPopulationItem item, StudentPopulation population) {
        var course = item.Class?.Course;
        if (course == null || !course.IsSum) return null;

        var type = course.StatisticsType;
        if (type == null || type == StatisticsType.None || type == StatisticsType.ManualInput) return null;

        return Compute(item, population, type.Value, course);
    }

    private int Compute(StudentPopulationItem item, StudentPopulation population, StatisticsType type, Course course) {
        switch (type) {
            case StatisticsType.SumByDepartment:
            case StatisticsType.SumByDepartmentAndClassType:
            case StatisticsType.SumBySourceDepartments:
            case StatisticsType.SumBySourceCourses:
                return GetSourceItems(course, item, population.Items).Sum(i => i.Number);
            case StatisticsType.CountClasses:
            case StatisticsType.CountClassesByClassType:
                return GetSourceItems(course, item, population.Items).Count(i => i.Number > 0);
            case StatisticsType.LastWeekValue:
                return SumLastWeek(course, item, population);
            case StatisticsType.DiffWithLastWeek: {
                int thisWeek = GetSourceItems(course, item, population.Items).Sum(i => i.Number);
                return thisWeek - SumLastWeek(course, item, population);
            }
            case StatisticsType.LastYearValue:
                return SumLastYear(course, item, population);
            case StatisticsType.DiffWithLastYear: {
                int thisWeek = GetSourceItems(course, item, population.Items).Sum(i => i.Number);
                return thisWeek - SumLastYear(course, item, population);
            }
            case StatisticsType.DiffBetweenCourses: {
                var positiveIds = ParseIntArray(course.SourceCourseIds);
                var negativeIds = ParseIntArray(course.NegativeSourceCourseIds);
                int positive = GetItemsByCourseIds(course, item, population.Items, positiveIds).Sum(i => i.Number);
                int negative = GetItemsByCourseIds(course, item, population.Items, negativeIds).Sum(i => i.Number);
                return positive - negative;
            }
            case StatisticsType.Average: {
                var src = GetSourceItems(course, item, population.Items).ToList();
                int count = src.Count(i => i.Number > 0);
                return count > 0 ? src.Sum(i => i.Number) / count : 0;
            }
            default:
                throw new NotSupportedException(
                    $"AggregationEngine 尚未支援 StatisticsType.{type}（課程 {course.Id} {course.Name}）。");
        }
    }

    // 直接讀上週實際存的 StudentPopulation 現場加總，不依賴本週項目上快取的 LastWeekNumber 欄位——
    // 該欄位只在建表時複製一次，分校若把本週人數0的班級整列刪除，快取值會跟著消失，導致上週總數失真。
    private int SumLastWeek(Course course, StudentPopulationItem item, StudentPopulation population) {
        var lastWeekPopulation = _lookupLastWeekPopulation(population);
        if (lastWeekPopulation?.Items == null) return 0;
        return GetSourceItems(course, item, lastWeekPopulation.Items).Sum(i => i.Number);
    }

    private int SumLastYear(Course course, StudentPopulationItem item, StudentPopulation population) {
        if (population.SchoolId == null) return 0;
        var lastYearPopulation = _lookupPopulation(population.Year - 1, population.Week, population.SchoolId.Value, population.Type);
        if (lastYearPopulation?.Items == null) return 0;
        return GetSourceItems(course, item, lastYearPopulation.Items).Sum(i => i.Number);
    }

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

    // DiffBetweenCourses 專用：直接依課程Id清單篩選（不經 SourceDepartmentIds／課程自身 DepartmentId 那條路徑），
    // 刻意不排除 IsSum 項目——來源課程本身可能就是 IsSum=true（例如 PSJ 的新生/流失手動輸入欄位）。
    // 班別篩選規則沿用 GetSourceItems：ApplicableClassType 優先，其次 GroupByClassType。
    private static IEnumerable<StudentPopulationItem> GetItemsByCourseIds(
        Course course, StudentPopulationItem contextItem, IEnumerable<StudentPopulationItem> items, List<int> courseIds) {
        var query = items.Where(i => i.Class?.CourseId != null && courseIds.Contains(i.Class.CourseId.Value));

        if (course.ApplicableClassType.HasValue) {
            query = query.Where(i => i.Class?.Type == course.ApplicableClassType.Value);
        } else if (course.GroupByClassType) {
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
