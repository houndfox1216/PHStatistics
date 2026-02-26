using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services;

/// <summary>
/// 統計計算服務
/// </summary>
public class StatisticsCalculationService
{
    private readonly DataContext _context;

    /// <summary>
    /// 建構統計計算服務
    /// </summary>
    /// <param name="context">資料脈絡</param>
    public StatisticsCalculationService(DataContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 計算人數表所有統計項目
    /// </summary>
    /// <param name="population">人數表</param>
    public void CalculateAll(StudentPopulation population)
    {
        if (population?.Items == null) return;

        // 取得所有加總項目，依照課程順序計算
        var sumItems = population.Items
            .Where(i => i.IsSum)
            .OrderBy(i => i.Class?.Course?.Ordinal ?? 0)
            .ToList();

        foreach (var item in sumItems)
        {
            Calculate(item, population);
        }
    }

    /// <summary>
    /// 計算單一統計項目
    /// </summary>
    /// <param name="item">人數表項目</param>
    /// <param name="population">人數表</param>
    public void Calculate(StudentPopulationItem item, StudentPopulation population)
    {
        if (item == null || population?.Items == null) return;
        if (!item.IsSum) return;
        if (item.IsManual) return; // 手動調整的項目不自動計算

        var course = item.Class?.Course;
        if (course == null) return;

        var statisticsType = course.StatisticsType;

        // 如果 StatisticsType 為 null 但 IsSum=true，用課程名稱判斷（相容舊邏輯）
        if (statisticsType == null && course.IsSum)
        {
            statisticsType = DetermineStatisticsTypeByName(course.Name);
        }

        if (statisticsType == null || statisticsType == StatisticsType.None || statisticsType == StatisticsType.ManualInput)
        {
            return; // 不需要計算
        }

        var result = statisticsType switch
        {
            StatisticsType.SumByDepartment => CalculateSumByDepartment(item, population),
            StatisticsType.SumByDepartmentAndClassType => CalculateSumByDepartmentAndClassType(item, population),
            StatisticsType.SumBySourceDepartments => CalculateSumBySourceDepartments(item, population, course),
            StatisticsType.SumBySourceCourses => CalculateSumBySourceCourses(item, population, course),
            StatisticsType.SumAll => CalculateSumAll(item, population),
            StatisticsType.SumAllByClassType => CalculateSumAllByClassType(item, population),
            StatisticsType.DiffWithLastWeek => CalculateDiffWithLastWeek(item, population),
            StatisticsType.DiffWithLastYear => CalculateDiffWithLastYear(item, population),
            StatisticsType.NewStudents => CalculateNewStudents(item, population),
            StatisticsType.LostStudents => CalculateLostStudents(item, population),
            StatisticsType.CountClasses => CalculateCountClasses(item, population),
            StatisticsType.CountClassesByClassType => CalculateCountClassesByClassType(item, population),
            StatisticsType.LastWeekValue => CalculateLastWeekValue(item, population),
            StatisticsType.LastYearValue => CalculateLastYearValue(item, population),
            StatisticsType.Average => CalculateAverage(item, population),
            _ => (int?)null
        };

        if (result.HasValue)
        {
            item.Number = result.Value;
        }
    }

    /// <summary>
    /// 根據課程名稱判斷統計類型（相容舊邏輯）
    /// </summary>
    private StatisticsType? DetermineStatisticsTypeByName(string courseName)
    {
        if (string.IsNullOrEmpty(courseName)) return null;

        if (courseName.Contains("新增") || courseName.Contains("新生"))
            return StatisticsType.NewStudents;
        if (courseName.Contains("流失"))
            return StatisticsType.LostStudents;
        if (courseName.Contains("班級數") || courseName.Contains("班數"))
            return StatisticsType.CountClasses;
        if (courseName.Contains("上週"))
            return StatisticsType.LastWeekValue;
        if (courseName.Contains("去年") || courseName.Contains("同期"))
            return StatisticsType.LastYearValue;
        if (courseName.Contains("小計") || courseName.Contains("合計") || courseName.Contains("總計"))
            return StatisticsType.SumByDepartment;

        return StatisticsType.SumByDepartment;
    }

    /// <summary>
    /// 同班系內所有非 IsSum 課程的人數加總
    /// </summary>
    private int CalculateSumByDepartment(StudentPopulationItem item, StudentPopulation population)
    {
        var departmentId = item.Class?.Course?.DepartmentId;
        if (departmentId == null) return 0;

        return population.Items
            .Where(i => !i.IsSum &&
                        i.Class?.Course?.DepartmentId == departmentId)
            .Sum(i => i.Number);
    }

    /// <summary>
    /// 同班系 + 同班別的加總
    /// </summary>
    private int CalculateSumByDepartmentAndClassType(StudentPopulationItem item, StudentPopulation population)
    {
        var departmentId = item.Class?.Course?.DepartmentId;
        var classType = item.Class?.Type;
        if (departmentId == null) return 0;

        return population.Items
            .Where(i => !i.IsSum &&
                        i.Class?.Course?.DepartmentId == departmentId &&
                        i.Class?.Type == classType)
            .Sum(i => i.Number);
    }

    /// <summary>
    /// 根據 SourceDepartmentIds JSON 陣列指定的班系加總
    /// </summary>
    private int CalculateSumBySourceDepartments(StudentPopulationItem item, StudentPopulation population, Course course)
    {
        var departmentIds = ParseJsonIntArray(course.SourceDepartmentIds);
        if (departmentIds == null || departmentIds.Count == 0) return 0;

        var query = population.Items
            .Where(i => !i.IsSum &&
                        i.Class?.Course?.DepartmentId != null &&
                        departmentIds.Contains(i.Class.Course.DepartmentId.Value));

        // 如果有指定適用班別，則過濾
        if (course.ApplicableClassType.HasValue)
        {
            query = query.Where(i => i.Class?.Type == course.ApplicableClassType.Value);
        }

        // 如果有指定來源學科，則過濾
        if (course.SourceSubject.HasValue)
        {
            query = query.Where(i => i.Class?.Course?.Department?.Subject == course.SourceSubject.Value);
        }

        return query.Sum(i => i.Number);
    }

    /// <summary>
    /// 根據 SourceCourseIds JSON 陣列指定的課程加總
    /// </summary>
    private int CalculateSumBySourceCourses(StudentPopulationItem item, StudentPopulation population, Course course)
    {
        var courseIds = ParseJsonIntArray(course.SourceCourseIds);
        if (courseIds == null || courseIds.Count == 0) return 0;

        var query = population.Items
            .Where(i => !i.IsSum &&
                        i.Class?.CourseId != null &&
                        courseIds.Contains(i.Class.CourseId.Value));

        // 如果有指定適用班別，則過濾
        if (course.ApplicableClassType.HasValue)
        {
            query = query.Where(i => i.Class?.Type == course.ApplicableClassType.Value);
        }

        return query.Sum(i => i.Number);
    }

    /// <summary>
    /// 全部人數加總
    /// </summary>
    private int CalculateSumAll(StudentPopulationItem item, StudentPopulation population)
    {
        return population.Items
            .Where(i => !i.IsSum)
            .Sum(i => i.Number);
    }

    /// <summary>
    /// 全部人數加總依班別
    /// </summary>
    private int CalculateSumAllByClassType(StudentPopulationItem item, StudentPopulation population)
    {
        var classType = item.Class?.Type;

        return population.Items
            .Where(i => !i.IsSum && i.Class?.Type == classType)
            .Sum(i => i.Number);
    }

    /// <summary>
    /// 計算相關項目的 Number 總和 - LastWeekNumber 總和
    /// </summary>
    private int CalculateDiffWithLastWeek(StudentPopulationItem item, StudentPopulation population)
    {
        var course = item.Class?.Course;
        var relatedItems = GetRelatedItems(item, population, course);

        var thisWeekSum = relatedItems.Sum(i => i.Number);
        var lastWeekSum = relatedItems.Sum(i => i.LastWeekNumber);

        return thisWeekSum - lastWeekSum;
    }

    /// <summary>
    /// 與去年同期比較
    /// </summary>
    private int CalculateDiffWithLastYear(StudentPopulationItem item, StudentPopulation population)
    {
        var thisYearSum = GetRelatedItems(item, population, item.Class?.Course).Sum(i => i.Number);
        var lastYearSum = CalculateLastYearValue(item, population);

        return thisYearSum - lastYearSum;
    }

    /// <summary>
    /// 新生人數: max(本週-上週, 0)
    /// </summary>
    private int CalculateNewStudents(StudentPopulationItem item, StudentPopulation population)
    {
        var course = item.Class?.Course;
        var relatedItems = GetRelatedItems(item, population, course);

        var thisWeekSum = relatedItems.Sum(i => i.Number);
        var lastWeekSum = relatedItems.Sum(i => i.LastWeekNumber);

        return Math.Max(thisWeekSum - lastWeekSum, 0);
    }

    /// <summary>
    /// 流失人數: max(上週-本週, 0)
    /// </summary>
    private int CalculateLostStudents(StudentPopulationItem item, StudentPopulation population)
    {
        var course = item.Class?.Course;
        var relatedItems = GetRelatedItems(item, population, course);

        var thisWeekSum = relatedItems.Sum(i => i.Number);
        var lastWeekSum = relatedItems.Sum(i => i.LastWeekNumber);

        return Math.Max(lastWeekSum - thisWeekSum, 0);
    }

    /// <summary>
    /// 計算有人數(Number > 0)的班級數量
    /// </summary>
    private int CalculateCountClasses(StudentPopulationItem item, StudentPopulation population)
    {
        var course = item.Class?.Course;
        var relatedItems = GetRelatedItems(item, population, course);

        return relatedItems.Count(i => i.Number > 0);
    }

    /// <summary>
    /// 計算有人數(Number > 0)的班級數量依班別
    /// </summary>
    private int CalculateCountClassesByClassType(StudentPopulationItem item, StudentPopulation population)
    {
        var classType = item.Class?.Type;
        var course = item.Class?.Course;
        var relatedItems = GetRelatedItems(item, population, course);

        return relatedItems
            .Where(i => i.Class?.Type == classType)
            .Count(i => i.Number > 0);
    }

    /// <summary>
    /// 計算相關項目的 LastWeekNumber 總和
    /// </summary>
    private int CalculateLastWeekValue(StudentPopulationItem item, StudentPopulation population)
    {
        var course = item.Class?.Course;
        var relatedItems = GetRelatedItems(item, population, course);

        return relatedItems.Sum(i => i.LastWeekNumber);
    }

    /// <summary>
    /// 查詢去年同週次的人數
    /// </summary>
    private int CalculateLastYearValue(StudentPopulationItem item, StudentPopulation population)
    {
        var course = item.Class?.Course;
        if (course == null) return 0;

        var lastYear = population.Year - 1;
        var week = population.Week;
        var schoolId = population.SchoolId;
        var type = population.Type;

        // 查詢去年同週次的人數表
        var lastYearPopulation = _context.StudentPopulation
            .Include(p => p.Items)
                .ThenInclude(i => i.Class)
                    .ThenInclude(c => c.Course)
            .FirstOrDefault(p => p.Year == lastYear &&
                                  p.Week == week &&
                                  p.SchoolId == schoolId &&
                                  p.Type == type);

        if (lastYearPopulation?.Items == null) return 0;

        // 取得相關項目的人數
        var relatedItems = GetRelatedItems(item, lastYearPopulation, course);
        return relatedItems.Sum(i => i.Number);
    }

    /// <summary>
    /// 計算平均值
    /// </summary>
    private int CalculateAverage(StudentPopulationItem item, StudentPopulation population)
    {
        var course = item.Class?.Course;
        var relatedItems = GetRelatedItems(item, population, course);

        var count = relatedItems.Count(i => i.Number > 0);
        if (count == 0) return 0;

        var sum = relatedItems.Sum(i => i.Number);
        return (int)Math.Round((double)sum / count);
    }

    /// <summary>
    /// 取得相關項目（用於計算）
    /// </summary>
    private IEnumerable<StudentPopulationItem> GetRelatedItems(StudentPopulationItem item, StudentPopulation population, Course course)
    {
        if (population?.Items == null || course == null)
            return Enumerable.Empty<StudentPopulationItem>();

        var query = population.Items.Where(i => !i.IsSum);

        // 根據課程設定決定篩選條件
        if (!string.IsNullOrEmpty(course.SourceDepartmentIds))
        {
            var departmentIds = ParseJsonIntArray(course.SourceDepartmentIds);
            if (departmentIds != null && departmentIds.Count > 0)
            {
                query = query.Where(i => i.Class?.Course?.DepartmentId != null &&
                                          departmentIds.Contains(i.Class.Course.DepartmentId.Value));
            }
        }
        else if (!string.IsNullOrEmpty(course.SourceCourseIds))
        {
            var courseIds = ParseJsonIntArray(course.SourceCourseIds);
            if (courseIds != null && courseIds.Count > 0)
            {
                query = query.Where(i => i.Class?.CourseId != null &&
                                          courseIds.Contains(i.Class.CourseId.Value));
            }
        }
        else if (course.DepartmentId.HasValue)
        {
            // 預設使用同班系
            query = query.Where(i => i.Class?.Course?.DepartmentId == course.DepartmentId);
        }

        // 如果有指定適用班別，則過濾
        if (course.ApplicableClassType.HasValue)
        {
            query = query.Where(i => i.Class?.Type == course.ApplicableClassType.Value);
        }

        // 如果有指定來源學科，則過濾
        if (course.SourceSubject.HasValue)
        {
            query = query.Where(i => i.Class?.Course?.Department?.Subject == course.SourceSubject.Value);
        }

        // 如果需要依班別分組計算，則過濾同班別
        if (course.GroupByClassType)
        {
            var classType = item.Class?.Type;
            query = query.Where(i => i.Class?.Type == classType);
        }

        return query;
    }

    /// <summary>
    /// 解析 JSON 整數陣列
    /// </summary>
    private List<int> ParseJsonIntArray(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<List<int>>(json);
        }
        catch
        {
            return null;
        }
    }
}
