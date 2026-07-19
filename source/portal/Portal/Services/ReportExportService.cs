using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services;

/// <summary>
/// 人數表匯出服務，產生與匯入格式完全相容的 Excel 檔案。
/// </summary>
public class ReportExportService {
    private readonly DataContext _context;

    public ReportExportService() => _context = new DataContext();

    // ── PSJ / AS 對照表（與 HomeController 保持一致）─────────────────────────

    private static readonly string[] _gradeOrder = {
        "一年級","二年級","三年級","四年級","五年級","六年級",
        "國一","國二","國三","高一","高二","高三"
    };

    private static readonly Dictionary<string, int[]> _psjCourseIds = new() {
        ["MP"] = new[] {145,146,147,148,149,150,151,152,153,154,155,156},
        ["MS"] = new[] {145,146,147,148,149,150,151,152,153,154,155,156},
        ["SP"] = new[] {195,196,197,198,199,200,201,202,203,204,205,206},
        ["SS"] = new[] {195,196,197,198,199,200,201,202,203,204,205,206},
        ["N"]  = new[] {171,172,173,174,175,176,177,178,179,180,181,182},
        ["L"]  = new[] {183,184,185,186,187,188,189,190,191,192,193,194},
        ["W"]  = new[] {159,160,161,162,163,164,165,166,167,168,169,170},
    };

    private static readonly Dictionary<string, int[]> _asCourseIds = new() {
        ["AS"] = new[] {245,246,247,248,249,250,251,252,253,254,255,256},
        ["EP"] = new[] {295,296,297,298,299,300,301,302,303,305,306,307},
        ["EG"] = new[] {295,296,297,298,299,300,301,302,303,305,306,307},
        ["N"]  = new[] {271,272,273,274,275,276,277,278,279,280,281,282},
        ["L"]  = new[] {283,284,285,286,287,288,289,290,291,292,293,294},
        ["W"]  = new[] {259,260,261,262,263,264,265,266,267,268,269,270},
    };

    private static ClassType PsjColType(string code) => code switch {
        "MP" or "SP" => ClassType.Personal,
        "MS" or "SS" => ClassType.SubGroup,
        _ => ClassType.General,
    };

    private static ClassType AsColType(string code) => code switch {
        "EP" or "MP" or "SP" => ClassType.Personal,
        "ES" or "MS" or "SS" => ClassType.SubGroup,
        _ => ClassType.General,
    };

    // ── 主入口 ───────────────────────────────────────────────────────────────

    /// <param name="schoolIds">限定分校 Id（null = 所有分校）</param>
    public byte[] Export(StudentPopulationType type, int year, int week,
                          IList<int> schoolIds = null) {
        var populations = LoadPopulations(type, year, week, schoolIds);
        if (populations.Count == 0) return Array.Empty<byte>();

        var wb = new XSSFWorkbook();

        var courses = (type == StudentPopulationType.PSJ || type == StudentPopulationType.AfterSchool)
            ? null
            : LoadCourses(type);

        switch (type) {
            case StudentPopulationType.PH:
                foreach (var (regionName, regionPopulations) in GroupByRegion(populations)) {
                    var sheet = wb.CreateSheet(regionName);
                    BuildSheetPH(sheet, regionPopulations, courses, year, week,
                        $"{year}年第{week}週百瀚英語{regionName}分校人數統計表");
                }
                break;
            case StudentPopulationType.GEPT: {
                var sheet = wb.CreateSheet("英檢");
                BuildSheetGEPT(sheet, populations, courses, year, week);
                break;
            }
            case StudentPopulationType.PS: {
                var sheet = wb.CreateSheet("Sheet1");
                BuildSheetPS(sheet, populations, courses, year, week);
                break;
            }
            case StudentPopulationType.PSJ: {
                var psjCourses = LoadCourses(StudentPopulationType.PSJ);
                var totalSheet = wb.CreateSheet("總表");
                BuildSheetPSJ(totalSheet, populations, psjCourses, year, week);
                foreach (var (regionName, regionPopulations) in GroupByRegion(populations)) {
                    var sheet = wb.CreateSheet(regionName);
                    BuildSheetPSJ(sheet, regionPopulations, psjCourses, year, week);
                }
                break;
            }
            case StudentPopulationType.AfterSchool: {
                var sheet = wb.CreateSheet("Sheet1");
                BuildSheetAS(sheet, populations, year, week);
                break;
            }
        }

        using var ms = new MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }

    // ── PH ───────────────────────────────────────────────────────────────────
    // Row 0: 標題
    // Rows 1-3: col 0=分校(合併3列)、col 1=類型(合併3列)、col 2+=科別→課程，每科末尾加「合計」欄
    // Row 4+: 每校兩列（小=SubGroup、三=V3），各科末自動加總

    private static void BuildSheetPH(ISheet sheet,
        List<StudentPopulation> populations, List<Course> courses,
        int year, int week, string title) {

        sheet.CreateRow(0).CreateCell(0).SetCellValue(title);

        var r1 = sheet.CreateRow(1);
        sheet.CreateRow(2);
        var r3 = sheet.CreateRow(3);
        r1.CreateCell(0).SetCellValue("分校");
        r1.CreateCell(1).SetCellValue("類型");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 1, 1)); } catch { }

        var deptGroups = courses
            .Where(c => !c.IsSum)
            .GroupBy(c => c.Department.Id)
            .Select(g => (dept: g.First().Department, list: g.ToList()))
            .ToList();

        // 表頭：Row 1 = 班系名稱（含合計欄合併），Row 3 = 課程名稱 + "合計"
        int col = 2;
        foreach (var (dept, list) in deptGroups) {
            int deptStart = col;
            foreach (var c in list) {
                r3.CreateCell(col).SetCellValue(c.Name);
                sheet.SetColumnWidth(col, 4 * 256);
                col++;
            }
            r3.CreateCell(col).SetCellValue("合計");
            sheet.SetColumnWidth(col, 4 * 256);
            col++;
            r1.CreateCell(deptStart).SetCellValue(dept.Name);
            if (col - 1 > deptStart)
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, col - 1)); } catch { }
        }

        // 資料列：每校兩列（小/三），每班系末附加小計
        int rowIdx = 4;
        foreach (var pop in populations) {
            var sgRow = sheet.CreateRow(rowIdx);
            var v3Row = sheet.CreateRow(rowIdx + 1);
            sgRow.CreateCell(0).SetCellValue(pop.School?.Name ?? "");
            sgRow.CreateCell(1).SetCellValue("小");
            v3Row.CreateCell(0).SetCellValue("");
            v3Row.CreateCell(1).SetCellValue("三");
            try { sheet.AddMergedRegion(new CellRangeAddress(rowIdx, rowIdx + 1, 0, 0)); } catch { }

            col = 2;
            foreach (var (dept, list) in deptGroups) {
                int sgTotal = 0, v3Total = 0;
                foreach (var c in list) {
                    int sg = pop.Items.Where(i => i.Class?.CourseId == c.Id && i.Class?.Type == ClassType.SubGroup).Sum(i => i.Number);
                    int v3 = pop.Items.Where(i => i.Class?.CourseId == c.Id && i.Class?.Type == ClassType.V3).Sum(i => i.Number);
                    if (sg > 0) { sgRow.CreateCell(col).SetCellValue(sg); sgTotal += sg; }
                    if (v3 > 0) { v3Row.CreateCell(col).SetCellValue(v3); v3Total += v3; }
                    col++;
                }
                if (sgTotal > 0) sgRow.CreateCell(col).SetCellValue(sgTotal);
                if (v3Total > 0) v3Row.CreateCell(col).SetCellValue(v3Total);
                col++;
            }
            rowIdx += 2;
        }
    }

    // ── GEPT ──────────────────────────────────────────────────────────────────
    // Row 0: 標題；Rows 1-3: 同 PH 但無「類型」欄；每校一列，每班系末加合計

    private static void BuildSheetGEPT(ISheet sheet,
        List<StudentPopulation> populations, List<Course> courses,
        int year, int week) {

        sheet.CreateRow(0).CreateCell(0).SetCellValue($"{year}年第{week}週英檢人數表");

        var r1 = sheet.CreateRow(1);
        sheet.CreateRow(2);
        var r3 = sheet.CreateRow(3);
        r1.CreateCell(0).SetCellValue("分校");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }

        var deptGroups = courses
            .Where(c => !c.IsSum)
            .GroupBy(c => c.Department.Id)
            .Select(g => (dept: g.First().Department, list: g.ToList()))
            .ToList();

        int col = 2;
        foreach (var (dept, list) in deptGroups) {
            int deptStart = col;
            foreach (var c in list) {
                r3.CreateCell(col).SetCellValue(c.Name);
                sheet.SetColumnWidth(col, 4 * 256);
                col++;
            }
            r3.CreateCell(col).SetCellValue("合計");
            sheet.SetColumnWidth(col, 4 * 256);
            col++;
            r1.CreateCell(deptStart).SetCellValue(dept.Name);
            if (col - 1 > deptStart)
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, col - 1)); } catch { }
        }

        int rowIdx = 4;
        foreach (var pop in populations) {
            var row = sheet.CreateRow(rowIdx++);
            row.CreateCell(0).SetCellValue(pop.School?.Name ?? "");
            col = 2;
            foreach (var (dept, list) in deptGroups) {
                int total = 0;
                foreach (var c in list) {
                    int sum = pop.Items.Where(i => i.Class?.CourseId == c.Id).Sum(i => i.Number);
                    if (sum > 0) { row.CreateCell(col).SetCellValue(sum); total += sum; }
                    col++;
                }
                if (total > 0) row.CreateCell(col).SetCellValue(total);
                col++;
            }
        }
    }

    // ── PS ───────────────────────────────────────────────────────────────────
    // Row 0: 標題；Row 1: 課程名稱（每班系末加「合計」）；Row 2+: 資料

    private static void BuildSheetPS(ISheet sheet,
        List<StudentPopulation> populations, List<Course> courses,
        int year, int week) {

        sheet.CreateRow(0).CreateCell(0).SetCellValue($"{year}年第{week}週百世人數表");

        var deptGroups = courses
            .Where(c => !c.IsSum)
            .GroupBy(c => c.Department.Id)
            .Select(g => (dept: g.First().Department, list: g.ToList()))
            .ToList();

        var hdr = sheet.CreateRow(1);
        hdr.CreateCell(0).SetCellValue("");
        int col = 1;
        foreach (var (dept, list) in deptGroups) {
            foreach (var c in list) {
                hdr.CreateCell(col).SetCellValue(c.Name);
                sheet.SetColumnWidth(col, 4 * 256);
                col++;
            }
            hdr.CreateCell(col).SetCellValue("合計");
            sheet.SetColumnWidth(col, 4 * 256);
            col++;
        }

        int rowIdx = 2;
        foreach (var pop in populations) {
            var row = sheet.CreateRow(rowIdx++);
            row.CreateCell(0).SetCellValue(pop.School?.Name ?? "");
            col = 1;
            foreach (var (dept, list) in deptGroups) {
                int total = 0;
                foreach (var c in list) {
                    int sum = pop.Items.Where(i => i.Class?.CourseId == c.Id).Sum(i => i.Number);
                    if (sum > 0) { row.CreateCell(col).SetCellValue(sum); total += sum; }
                    col++;
                }
                if (total > 0) row.CreateCell(col).SetCellValue(total);
                col++;
            }
        }
    }

    // ── PSJ ──────────────────────────────────────────────────────────────────
    // Row 0: 標題；Row 1: 班系名稱（含合計欄合併）；Row 2: 課程名稱（依 GroupByClassType 展開 EM1/小組班兩欄或單欄）
    // Row 3+: 每分校一列

    private static void BuildSheetPSJ(ISheet sheet,
        List<StudentPopulation> populations, List<Course> courses, int year, int week) {

        sheet.CreateRow(0).CreateCell(0).SetCellValue($"{year}年第{week}週百倍速人數表");

        var r1 = sheet.CreateRow(1);
        var r2 = sheet.CreateRow(2);
        r1.CreateCell(0).SetCellValue("分校");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 2, 0, 0)); } catch { }

        var deptGroups = courses
            .Where(c => !c.IsSum)
            .GroupBy(c => c.Department.Id)
            .Select(g => (dept: g.First().Department, list: g.ToList()))
            .ToList();

        // 每個非合計課程展開成 1 欄（GroupByClassType=false）或 2 欄 EM1/小組班（GroupByClassType=true）
        // 欄位規格：(course, classType) —— classType 為 null 表示不分班別
        var columns = new List<(Course course, ClassType? classType)>();
        int col = 1;
        foreach (var (dept, list) in deptGroups) {
            int deptStart = col;
            foreach (var c in list) {
                if (c.GroupByClassType) {
                    r2.CreateCell(col).SetCellValue($"{c.Name}(EM1)");
                    columns.Add((c, ClassType.Personal));
                    col++;
                    r2.CreateCell(col).SetCellValue($"{c.Name}(小組班)");
                    columns.Add((c, ClassType.SubGroup));
                    col++;
                } else {
                    r2.CreateCell(col).SetCellValue(c.Name);
                    columns.Add((c, null));
                    col++;
                }
                sheet.SetColumnWidth(col - 1, 4 * 256);
            }
            r2.CreateCell(col).SetCellValue("合計");
            sheet.SetColumnWidth(col, 4 * 256);
            columns.Add((null, null)); // 合計欄佔位，資料列時特別處理
            col++;
            r1.CreateCell(deptStart).SetCellValue(dept.Name);
            if (col - 1 > deptStart)
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, col - 1)); } catch { }
        }

        // 資料列：每分校一列
        int rowIdx = 3;
        foreach (var pop in populations) {
            var row = sheet.CreateRow(rowIdx++);
            row.CreateCell(0).SetCellValue(pop.School?.Name ?? "");

            int ci = 1;
            foreach (var (dept, list) in deptGroups) {
                int deptTotal = 0;
                foreach (var c in list) {
                    if (c.GroupByClassType) {
                        int em1 = pop.Items.Where(i => i.Class?.CourseId == c.Id && i.Class?.Type == ClassType.Personal).Sum(i => i.Number);
                        int sub = pop.Items.Where(i => i.Class?.CourseId == c.Id && i.Class?.Type == ClassType.SubGroup).Sum(i => i.Number);
                        if (em1 > 0) row.CreateCell(ci).SetCellValue(em1);
                        ci++;
                        if (sub > 0) row.CreateCell(ci).SetCellValue(sub);
                        ci++;
                        deptTotal += em1 + sub;
                    } else {
                        int sum = pop.Items.Where(i => i.Class?.CourseId == c.Id).Sum(i => i.Number);
                        if (sum > 0) row.CreateCell(ci).SetCellValue(sum);
                        ci++;
                        deptTotal += sum;
                    }
                }
                if (deptTotal > 0) row.CreateCell(ci).SetCellValue(deptTotal);
                ci++;
            }
        }
    }

    // ── AS ───────────────────────────────────────────────────────────────────

    private static void BuildSheetAS(ISheet sheet,
        List<StudentPopulation> populations, int year, int week) {

        sheet.CreateRow(0).CreateCell(0).SetCellValue($"{year}年第{week}週課輔人數表");

        string[] codes = { "T", "AS", "EP", "EG", "N", "L", "W" };
        WriteGradeHeader(sheet, 4, codes);

        int rowIdx = 5;
        foreach (var pop in populations) {
            int tTotal = pop.Items.Where(i => i.Class?.CourseId == 258).Sum(i => i.Number);
            bool tWritten = false;

            for (int gi = 0; gi < _gradeOrder.Length; gi++) {
                var vals = new int[codes.Length];
                vals[0] = (!tWritten && tTotal > 0) ? tTotal : 0;

                for (int ci = 1; ci < codes.Length; ci++) {
                    if (!_asCourseIds.TryGetValue(codes[ci], out var ids)) continue;
                    var ct = AsColType(codes[ci]);
                    vals[ci] = pop.Items.Where(i => i.Class?.CourseId == ids[gi] && i.Class?.Type == ct).Sum(i => i.Number);
                }

                if (vals.All(v => v == 0)) continue;
                if (vals[0] > 0) tWritten = true;

                WriteGradeRow(sheet.CreateRow(rowIdx++), year, week, pop.School?.Name ?? "", _gradeOrder[gi], vals);
            }
        }
    }

    // ── 共用 helper ───────────────────────────────────────────────────────────

    // 計算 IsSum 課程的合計值（對應 StatisticsCalculationService 邏輯）
    // rowClassType：本列的班別（PH 的 SubGroup/V3；GEPT/PS 傳 null）
    private static int ComputeIsumValue(Course course,
        IEnumerable<StudentPopulationItem> allItems, ClassType? rowClassType) {

        // 排除 IsSum 課程本身的項目，只用真實班級資料計算
        var src = allItems.Where(i => i.Class?.Course?.IsSum != true);

        // 來源篩選：SourceDepartmentIds > SourceCourseIds > 同班系
        if (!string.IsNullOrEmpty(course.SourceDepartmentIds)) {
            var ids = TryParseIntArray(course.SourceDepartmentIds);
            if (ids.Length > 0)
                src = src.Where(i => i.Class?.Course?.DepartmentId != null &&
                                     ids.Contains(i.Class.Course.DepartmentId.Value));
        } else if (!string.IsNullOrEmpty(course.SourceCourseIds)) {
            var ids = TryParseIntArray(course.SourceCourseIds);
            if (ids.Length > 0)
                src = src.Where(i => i.Class?.CourseId != null &&
                                     ids.Contains(i.Class.CourseId.Value));
        } else {
            src = src.Where(i => i.Class?.Course?.DepartmentId == course.DepartmentId);
        }

        // 班別篩選：ApplicableClassType 優先，其次 GroupByClassType 使用列班別
        var ct = course.ApplicableClassType
                 ?? (course.GroupByClassType ? rowClassType : null);
        if (ct.HasValue)
            src = src.Where(i => i.Class?.Type == ct.Value);

        // 決定統計類型（db 設定 > 名稱推斷）
        var statsType = course.StatisticsType ?? InferStatisticsType(course.Name);

        return statsType switch {
            StatisticsType.CountClasses or StatisticsType.CountClassesByClassType
                => src.Count(i => i.Number > 0),
            StatisticsType.LastWeekValue
                => src.Sum(i => i.LastWeekNumber),
            StatisticsType.DiffWithLastWeek
                => src.Sum(i => i.Number) - src.Sum(i => i.LastWeekNumber),
            StatisticsType.NewStudents
                => Math.Max(src.Sum(i => i.Number) - src.Sum(i => i.LastWeekNumber), 0),
            StatisticsType.LostStudents
                => Math.Max(src.Sum(i => i.LastWeekNumber) - src.Sum(i => i.Number), 0),
            _ => src.Sum(i => i.Number),   // SumByDepartment / SumBySourceX / SumAll / 預設
        };
    }

    private static StatisticsType? InferStatisticsType(string name) {
        if (string.IsNullOrEmpty(name)) return null;
        if (name.Contains("班級數") || name.Contains("班數")) return StatisticsType.CountClasses;
        if (name.Contains("上週"))                               return StatisticsType.LastWeekValue;
        if (name.Contains("新增") || name.Contains("新生"))      return StatisticsType.NewStudents;
        if (name.Contains("流失"))                               return StatisticsType.LostStudents;
        if (name.Contains("與上週相比"))                         return StatisticsType.DiffWithLastWeek;
        return StatisticsType.SumByDepartment;
    }

    private static int[] TryParseIntArray(string json) {
        try { return JsonSerializer.Deserialize<int[]>(json) ?? Array.Empty<int>(); }
        catch { return Array.Empty<int>(); }
    }

    private static void WriteGradeHeader(ISheet sheet, int rowIdx, string[] codes) {
        var hdr = sheet.CreateRow(rowIdx);
        hdr.CreateCell(0).SetCellValue("年");
        hdr.CreateCell(1).SetCellValue("週");
        hdr.CreateCell(2).SetCellValue("分校");
        hdr.CreateCell(3).SetCellValue("年級");
        for (int i = 0; i < codes.Length; i++) hdr.CreateCell(4 + i).SetCellValue(codes[i]);
    }

    private static void WriteGradeRow(IRow row, int year, int week,
        string school, string grade, int[] vals) {

        row.CreateCell(0).SetCellValue(year);
        row.CreateCell(1).SetCellValue(week);
        row.CreateCell(2).SetCellValue(school);
        row.CreateCell(3).SetCellValue(grade);
        for (int ci = 0; ci < vals.Length; ci++) {
            if (vals[ci] > 0) row.CreateCell(4 + ci).SetCellValue(vals[ci]);
        }
    }

    // 依 School.Region.Name 分組，維持組內原有排序（LoadPopulations 已依 Region.Ordinal, School.Ordinal 排序）
    // 查無 Region 的分校統一歸入「未分區」，永遠排在最後
    private static List<(string RegionName, List<StudentPopulation> Populations)> GroupByRegion(
        List<StudentPopulation> populations) {

        var withRegion = populations.Where(p => p.School?.Region != null).ToList();
        var withoutRegion = populations.Where(p => p.School?.Region == null).ToList();

        var groups = withRegion
            .GroupBy(p => (Name: p.School.Region.Name, Ordinal: p.School.Region.Ordinal))
            .OrderBy(g => g.Key.Ordinal)
            .Select(g => (RegionName: g.Key.Name, Populations: g.ToList()))
            .ToList();

        if (withoutRegion.Count > 0)
            groups.Add(("未分區", withoutRegion));

        return groups;
    }

    // ── 資料查詢 ──────────────────────────────────────────────────────────────

    private List<StudentPopulation> LoadPopulations(StudentPopulationType type,
        int year, int week, IList<int> schoolIds) {

        var query = _context.StudentPopulation
            .Include(sp => sp.School).ThenInclude(s => s.Region)
            .Include(sp => sp.Items)
                .ThenInclude(i => i.Class)
                    .ThenInclude(c => c.Course)
                        .ThenInclude(c => c.Department)
            .Where(sp => sp.Year == year && sp.Week == week && sp.Type == type);

        if (schoolIds?.Count > 0)
            query = query.Where(sp => schoolIds.Contains(sp.SchoolId ?? 0));

        return query
            .OrderBy(sp => sp.School.Region.Ordinal)
            .ThenBy(sp => sp.School.Ordinal)
            .ToList();
    }

    private List<Course> LoadCourses(StudentPopulationType type) =>
        _context.Course
            .Include(c => c.Department)
            .Where(c => c.Type == type && c.Published)
            .OrderBy(c => c.Department.Ordinal)
            .ThenBy(c => c.Ordinal)
            .ToList();

    // ── PH 班級明細 ───────────────────────────────────────────────────────────
    // 格式：每校 2 列（小班 / 三人班），每課程展開為 N 個子欄位（甲班、乙班…）

    private static readonly string[] _chineseOrdinals =
        { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };

    private static string GetOrdinalLabel(int zeroIdx) =>
        zeroIdx < _chineseOrdinals.Length ? $"{_chineseOrdinals[zeroIdx]}班" : $"第{zeroIdx + 1}班";

    public byte[] ExportPHDetail(int year, int week, IList<int> schoolIds = null) {
        var populations = LoadPopulations(StudentPopulationType.PH, year, week, schoolIds);
        if (populations.Count == 0) return Array.Empty<byte>();

        var courses = LoadCourses(StudentPopulationType.PH);
        var nonSumCourses = courses.Where(c => !c.IsSum).ToList();

        var targetTypes = new[] { ClassType.SubGroup, ClassType.V3 };

        // 計算各 courseId 的全區最大班數（SubGroup 與 V3 取其中較大值）
        var maxSlots = new Dictionary<int, int>();
        foreach (var pop in populations) {
            foreach (var courseId in nonSumCourses.Select(c => c.Id)) {
                int sg = pop.Items.Count(i => i.Class?.CourseId == courseId && i.Class?.Type == ClassType.SubGroup);
                int v3 = pop.Items.Count(i => i.Class?.CourseId == courseId && i.Class?.Type == ClassType.V3);
                int mx = Math.Max(sg, v3);
                if (!maxSlots.TryGetValue(courseId, out int ex) || mx > ex)
                    maxSlots[courseId] = mx;
            }
        }

        // 只保留有資料的課程欄
        var activeCourses = nonSumCourses.Where(c => maxSlots.TryGetValue(c.Id, out int s) && s > 0).ToList();
        var deptGroups = activeCourses
            .GroupBy(c => c.Department.Id)
            .Select(g => (dept: g.First().Department, list: g.ToList()))
            .ToList();

        int fixedCols = 2;
        int totalCols = fixedCols + activeCourses.Sum(c => maxSlots[c.Id]) + deptGroups.Count;

        var wb    = new XSSFWorkbook();
        var sheet = wb.CreateSheet("Sheet1");

        // Row 0: 標題
        sheet.CreateRow(0).CreateCell(0).SetCellValue($"{year}年第{week}週百瀚人數表（班級明細）");
        try { sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, totalCols - 1)); } catch { }

        var r1 = sheet.CreateRow(1);
        sheet.CreateRow(2);
        var r3 = sheet.CreateRow(3);
        r1.CreateCell(0).SetCellValue("分校");
        r1.CreateCell(1).SetCellValue("班型");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 1, 1)); } catch { }

        // Rows 1-3: 班系 → 課程 → 子欄位
        int col = fixedCols;
        foreach (var (dept, list) in deptGroups) {
            int deptStart = col;
            foreach (var c in list) {
                int slots = maxSlots[c.Id];
                var row2 = sheet.GetRow(2) ?? sheet.CreateRow(2);
                row2.CreateCell(col).SetCellValue(c.Name);
                if (slots > 1)
                    try { sheet.AddMergedRegion(new CellRangeAddress(2, 2, col, col + slots - 1)); } catch { }
                for (int si = 0; si < slots; si++)
                    r3.CreateCell(col + si).SetCellValue(GetOrdinalLabel(si));
                col += slots;
            }
            // 部門合計欄
            r1.CreateCell(deptStart).SetCellValue(dept.Name);
            int deptEnd = col;
            if (deptEnd > deptStart)
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, deptEnd)); } catch { }
            r3.CreateCell(col).SetCellValue("合計");
            col++;
        }

        // Row 4+: 每校 2 列（小班 / 三人班）
        int rowIdx = 4;
        foreach (var pop in populations) {
            var sgRow = sheet.CreateRow(rowIdx);
            var v3Row = sheet.CreateRow(rowIdx + 1);
            sgRow.CreateCell(0).SetCellValue(pop.School?.Name ?? "");
            sgRow.CreateCell(1).SetCellValue("小班");
            v3Row.CreateCell(1).SetCellValue("三人班");
            try { sheet.AddMergedRegion(new CellRangeAddress(rowIdx, rowIdx + 1, 0, 0)); } catch { }

            col = fixedCols;
            foreach (var (_, list) in deptGroups) {
                int sgDeptTotal = 0, v3DeptTotal = 0;
                foreach (var c in list) {
                    int slots = maxSlots[c.Id];
                    var sgItems = pop.Items
                        .Where(i => i.Class?.CourseId == c.Id && i.Class?.Type == ClassType.SubGroup)
                        .OrderBy(i => i.Class.Ordinal).ThenBy(i => i.Class.Id).ToList();
                    var v3Items = pop.Items
                        .Where(i => i.Class?.CourseId == c.Id && i.Class?.Type == ClassType.V3)
                        .OrderBy(i => i.Class.Ordinal).ThenBy(i => i.Class.Id).ToList();
                    for (int si = 0; si < slots; si++) {
                        if (si < sgItems.Count && sgItems[si].Number > 0) {
                            sgRow.CreateCell(col + si).SetCellValue(sgItems[si].Number);
                            sgDeptTotal += sgItems[si].Number;
                        }
                        if (si < v3Items.Count && v3Items[si].Number > 0) {
                            v3Row.CreateCell(col + si).SetCellValue(v3Items[si].Number);
                            v3DeptTotal += v3Items[si].Number;
                        }
                    }
                    col += slots;
                }
                if (sgDeptTotal > 0) sgRow.CreateCell(col).SetCellValue(sgDeptTotal);
                if (v3DeptTotal > 0) v3Row.CreateCell(col).SetCellValue(v3DeptTotal);
                col++;
            }
            rowIdx += 2;
        }

        sheet.SetColumnWidth(0, 20 * 256);
        sheet.SetColumnWidth(1, 9 * 256);
        for (int c = fixedCols; c < totalCols; c++)
            sheet.SetColumnWidth(c, 7 * 256);

        using var ms = new MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }
}
