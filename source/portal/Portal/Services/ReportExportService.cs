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
        ["MP"] = new[] {378,379,380,381,382,383,384,385,386,387,388,389},
        ["MG"] = new[] {378,379,380,381,382,383,384,385,386,387,388,389},
        ["SP"] = new[] {428,429,430,431,432,433,434,435,436,437,438,439},
        ["SG"] = new[] {428,429,430,431,432,433,434,435,436,437,438,439},
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
        // 單一分校（非管理員只能看自己分校、或admin指定單一分校）：改成「每週一列」格式，
        // 涵蓋該分校整學年至今累積的週次，而不是跟全分校匯出一樣「每分校一列、單一週次」。
        if (type == StudentPopulationType.PH && schoolIds?.Count == 1) {
            return ExportPHBySchool(year, week, schoolIds[0]);
        }

        var populations = LoadPopulations(type, year, week, schoolIds);
        if (populations.Count == 0) return Array.Empty<byte>();

        var wb = new XSSFWorkbook();

        var courses = type switch {
            StudentPopulationType.PSJ or StudentPopulationType.AfterSchool => null,
            // PH/GEPT/PS的IsSum合計/分析課程在DB裡幾乎全部Published=0，總表現在要把它們一併列出，需繞過Published過濾
            StudentPopulationType.PH or StudentPopulationType.GEPT or StudentPopulationType.PS => LoadCourses(type, publishedOnly: false),
            _ => LoadCourses(type)
        };

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
            case StudentPopulationType.AfterSchool:
                BuildSheetAS(wb, populations, year, week);
                break;
        }

        using var ms = new MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }

    // ── PH ───────────────────────────────────────────────────────────────────
    // Row 0: 標題
    // Rows 1-3: col 0=分校(合併3列)、col 1=類型(合併3列)、col 2+=科別→課程，每科末尾加「合計」欄
    // Row 4+: 每校兩列（小=SubGroup、三=V3），各科末自動加總

    // 2026-08-02：欄位改成完整Course清單（含IsSum合計/上週人數/與上週相比/去年同期比/新生/流失/總班數等），
    // 比照 BuildSheetPHBySchool 的做法——這些課程本身就在正確的Ordinal位置上，不用再自行合成部門「合計」欄。
    // 呼叫端(Export())需傳入 LoadCourses(PH, publishedOnly:false)，否則這些課程幾乎全部Published=0會被濾光。
    private static void BuildSheetPH(ISheet sheet,
        List<StudentPopulation> populations, List<Course> courses,
        int year, int week, string title) {

        sheet.CreateRow(0).CreateCell(0).SetCellValue(title);

        var r1 = sheet.CreateRow(1);
        var r2 = sheet.CreateRow(2);
        sheet.CreateRow(3);
        r1.CreateCell(0).SetCellValue("分校");
        r1.CreateCell(1).SetCellValue("類型");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 1, 1)); } catch { }

        var deptGroups = GroupConsecutiveByDepartment(courses);

        // 表頭：Row 1 = 班系名稱（合併），Row 2 = 課程名稱，Row 3 保留給未來子欄位細分用
        int col = 2;
        foreach (var (dept, list) in deptGroups) {
            int deptStart = col;
            foreach (var c in list) {
                r2.CreateCell(col).SetCellValue(c.Name);
                sheet.SetColumnWidth(col, 4 * 256);
                col++;
            }
            r1.CreateCell(deptStart).SetCellValue(dept.Name);
            if (col - 1 > deptStart)
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, col - 1)); } catch { }
        }

        // 資料列：每校兩列（小=SubGroup+Personal、三=V3）。GroupByClassType/ApplicableClassType的課程依班別
        // 分別計算；其餘單一值課程（上週人數/與上週相比/新生/流失/個別指導合計/合作開班合計等）只寫在小列。
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
            foreach (var c in courses) {
                bool splitByClassType = c.ApplicableClassType.HasValue || c.GroupByClassType;
                if (splitByClassType) {
                    int sg = pop.Items.Where(i => i.Class?.CourseId == c.Id &&
                        (i.Class?.Type == ClassType.SubGroup || i.Class?.Type == ClassType.Personal)).Sum(i => i.Number);
                    int v3 = pop.Items.Where(i => i.Class?.CourseId == c.Id && i.Class?.Type == ClassType.V3).Sum(i => i.Number);
                    if (sg != 0) sgRow.CreateCell(col).SetCellValue(sg);
                    if (v3 != 0) v3Row.CreateCell(col).SetCellValue(v3);
                }
                else {
                    int total = pop.Items.Where(i => i.Class?.CourseId == c.Id).Sum(i => i.Number);
                    if (total != 0) sgRow.CreateCell(col).SetCellValue(total);
                }
                col++;
            }
            rowIdx += 2;
        }
    }

    // ── PH（單一分校，每週一列）─────────────────────────────────────────────────
    // Row 0: 標題；Rows 1-3: col0=週次、col1=日期、col2=開班模式（小/三，團體班已廢除不再輸出）
    // col3+ 沿用 Course.Ordinal 全清單（含IsSum合計/分析欄，不排除），值直接讀已由AggregationEngine算好的Number，
    // 不用另外合成部門合計欄——IsSum課程本身已經在正確的Ordinal位置上。
    // Row 4+: 每週兩列（小=SubGroup+Personal、三=V3）；GroupByClassType/ApplicableClassType 的課程依班別分別計算，
    // 其餘課程（上週人數/與上週相比/新生/流失/個別指導合計/合作開班合計等）只有單一值，只寫在小列。

    private byte[] ExportPHBySchool(int year, int week, int schoolId) {
        var weeklyPopulations = LoadWeeklyPopulationsForSchool(StudentPopulationType.PH, year, week, schoolId);
        if (weeklyPopulations.Count == 0) return Array.Empty<byte>();

        var school = weeklyPopulations[0].School ?? _context.School.Find(schoolId);
        // publishedOnly:false — 這幾乎所有IsSum合計/分析課程在DB裡都是Published=0（原本的匯入相容匯出不需要它們），
        // 但這個「每週一列」格式就是要把它們一併呈現出來，不能用預設的Published過濾。
        var courses = LoadCourses(StudentPopulationType.PH, publishedOnly: false);

        var wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet(school?.Name ?? "Sheet1");
        BuildSheetPHBySchool(sheet, weeklyPopulations, courses, school?.Name ?? "", week);

        using var ms = new MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }

    // 依Course.Ordinal順序，只合併「連續」同一班系的課程；同一班系若在Ordinal上被其他班系的課程隔開，
    // 視為兩個獨立的區塊，各自成組（不會被拉回去跟前面同班系的區塊合併）。
    private static List<(CourseDepartment dept, List<Course> list)> GroupConsecutiveByDepartment(List<Course> courses) {
        var groups = new List<(CourseDepartment dept, List<Course> list)>();
        foreach (var c in courses) {
            if (groups.Count > 0 && groups[^1].dept?.Id == c.Department?.Id) {
                groups[^1].list.Add(c);
            }
            else {
                groups.Add((c.Department, new List<Course> { c }));
            }
        }
        return groups;
    }

    private static void BuildSheetPHBySchool(ISheet sheet,
        List<StudentPopulation> weeklyPopulations, List<Course> courses,
        string schoolName, int upToWeek) {

        string title = $"{schoolName}分校人數統計表 填表日期: {DateTime.Now.Year - 1911}年{DateTime.Now.Month}月{DateTime.Now.Day}日(第{upToWeek}週)";
        sheet.CreateRow(0).CreateCell(0).SetCellValue(title);

        var r1 = sheet.CreateRow(1);
        var r2 = sheet.CreateRow(2);
        sheet.CreateRow(3);
        r1.CreateCell(0).SetCellValue("週次");
        r1.CreateCell(1).SetCellValue("日期");
        r1.CreateCell(2).SetCellValue("開班模式");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 1, 1)); } catch { }
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 2, 2)); } catch { }

        // LoadCourses 已依 Department.Ordinal → Course.Ordinal 排序，同一班系的課程本來就會排在一起；
        // 用「連續相同班系才合併」而非單純GroupBy，是為了不去依賴這個排序前提——
        // 萬一日後排序規則改變導致同班系課程不再相鄰，也不會被誤拉回同一組、打亂欄位順序。
        var deptGroups = GroupConsecutiveByDepartment(courses);

        int col = 3;
        foreach (var (dept, list) in deptGroups) {
            int deptStart = col;
            foreach (var c in list) {
                r2.CreateCell(col).SetCellValue(c.Name);
                sheet.SetColumnWidth(col, 4 * 256);
                col++;
            }
            r1.CreateCell(deptStart).SetCellValue(dept.Name);
            if (col - 1 > deptStart)
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, col - 1)); } catch { }
        }

        // 資料列：每週兩列（小/三，無團）
        var dateStyle = sheet.Workbook.CreateCellStyle();
        dateStyle.DataFormat = sheet.Workbook.CreateDataFormat().GetFormat("yyyy/m/d");

        int rowIdx = 4;
        foreach (var pop in weeklyPopulations) {
            var sgRow = sheet.CreateRow(rowIdx);
            var v3Row = sheet.CreateRow(rowIdx + 1);
            sgRow.CreateCell(0).SetCellValue(pop.Week);
            var dateCell = sgRow.CreateCell(1);
            dateCell.SetCellValue(pop.WeekDate);
            dateCell.CellStyle = dateStyle;
            sgRow.CreateCell(2).SetCellValue("小");
            v3Row.CreateCell(2).SetCellValue("三");
            try { sheet.AddMergedRegion(new CellRangeAddress(rowIdx, rowIdx + 1, 0, 0)); } catch { }
            try { sheet.AddMergedRegion(new CellRangeAddress(rowIdx, rowIdx + 1, 1, 1)); } catch { }

            col = 3;
            foreach (var c in courses) {
                bool splitByClassType = c.ApplicableClassType.HasValue || c.GroupByClassType;
                if (splitByClassType) {
                    int sg = pop.Items.Where(i => i.Class?.CourseId == c.Id &&
                        (i.Class?.Type == ClassType.SubGroup || i.Class?.Type == ClassType.Personal)).Sum(i => i.Number);
                    int v3 = pop.Items.Where(i => i.Class?.CourseId == c.Id && i.Class?.Type == ClassType.V3).Sum(i => i.Number);
                    if (sg != 0) sgRow.CreateCell(col).SetCellValue(sg);
                    if (v3 != 0) v3Row.CreateCell(col).SetCellValue(v3);
                }
                else {
                    int total = pop.Items.Where(i => i.Class?.CourseId == c.Id).Sum(i => i.Number);
                    if (total != 0) sgRow.CreateCell(col).SetCellValue(total);
                }
                col++;
            }
            rowIdx += 2;
        }

        sheet.SetColumnWidth(0, 6 * 256);
        sheet.SetColumnWidth(1, 10 * 256);
        sheet.SetColumnWidth(2, 6 * 256);
    }

    // ── GEPT ──────────────────────────────────────────────────────────────────
    // Row 0: 標題；Rows 1-3: 同 PH 但無「類型」欄；每校一列，每班系末加合計

    private static void BuildSheetGEPT(ISheet sheet,
        List<StudentPopulation> populations, List<Course> courses,
        int year, int week) {

        sheet.CreateRow(0).CreateCell(0).SetCellValue($"{year}年第{week}週英檢人數表");

        var r1 = sheet.CreateRow(1);
        var r2 = sheet.CreateRow(2);
        sheet.CreateRow(3);
        r1.CreateCell(0).SetCellValue("分校");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }

        // 不排除 IsSum：這些是官方表格要看的統計/分析欄位（上週人數、與上週相比…），
        // 本來就該一併列出，只是不計入下面自己合成的「合計」欄，避免跟課程自身數字重複相加。
        var deptGroups = courses
            .GroupBy(c => c.Department.Id)
            .Select(g => (dept: g.First().Department, list: g.ToList()))
            .ToList();

        int col = 2;
        foreach (var (dept, list) in deptGroups) {
            int deptStart = col;
            foreach (var c in list) {
                r2.CreateCell(col).SetCellValue(c.Name);
                sheet.SetColumnWidth(col, 4 * 256);
                col++;
            }
            r2.CreateCell(col).SetCellValue("合計");
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
                    if (sum > 0) { row.CreateCell(col).SetCellValue(sum); if (!c.IsSum) total += sum; }
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

        // 不排除 IsSum：這些是官方表格要看的統計/分析欄位（PS數學總人數、上週人數、新生/流失…），
        // 本來就該一併列出，只是不計入下面自己合成的「合計」欄，避免跟課程自身數字重複相加。
        var deptGroups = courses
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
                    if (sum > 0) { row.CreateCell(col).SetCellValue(sum); if (!c.IsSum) total += sum; }
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
    // 比照百瀚實際填報格式「百瀚全區課輔人數總表」：每校一個 Sheet。
    // Row 0: 標題；Row 1-3: 三層合併表頭；Row 4+: 12 個年級各一列 + 小計列
    // 週次/日期/總人數（單一合計值）合併整個資料區塊（含小計列）

    private static readonly (string OneOnOne, string Group, string Label)[] _asSubjectGroups = {
        ("EP", "EG", "英文班"),
        ("MP", "MG", "數學班"),
        ("SP", "SG", "理化班"),
    };

    private static void BuildSheetAS(XSSFWorkbook wb,
        List<StudentPopulation> populations, int year, int week) {

        foreach (var pop in populations) {
            string schoolName = pop.School?.Name ?? "";
            var sheet = wb.CreateSheet($"{year}{schoolName}");

            sheet.CreateRow(0).CreateCell(0).SetCellValue(
                $"{schoolName} 教室{year}學年度7-6月課輔班人數統計表(請於每週六下班回傳)");

            var r1 = sheet.CreateRow(1);
            var r2 = sheet.CreateRow(2);
            sheet.CreateRow(3);

            r1.CreateCell(0).SetCellValue("週次");
            r1.CreateCell(1).SetCellValue("日期");
            r1.CreateCell(2).SetCellValue("課程");
            r1.CreateCell(3).SetCellValue("安親課輔班");
            r2.CreateCell(2).SetCellValue("年級  班別");

            int col = 4;
            foreach (var (_, _, label) in _asSubjectGroups) {
                r1.CreateCell(col).SetCellValue(label);
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, col, col + 1)); } catch { }
                r2.CreateCell(col).SetCellValue("一對一");
                r2.CreateCell(col + 1).SetCellValue("團體班");
                col += 2;
            }
            int analysisCol = col;
            r1.CreateCell(analysisCol).SetCellValue("分析");
            try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, analysisCol, analysisCol + 3)); } catch { }
            r2.CreateCell(analysisCol).SetCellValue("新生");
            r2.CreateCell(analysisCol + 1).SetCellValue("流失");
            r2.CreateCell(analysisCol + 2).SetCellValue("上週比");
            r2.CreateCell(analysisCol + 3).SetCellValue("總人數");

            try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }
            try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 1, 1)); } catch { }
            try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 3, 3)); } catch { }
            try { sheet.AddMergedRegion(new CellRangeAddress(2, 3, 2, 2)); } catch { }
            for (int c = 4; c < analysisCol + 4; c++) {
                try { sheet.AddMergedRegion(new CellRangeAddress(2, 3, c, c)); } catch { }
            }

            var dateStyle = wb.CreateCellStyle();
            dateStyle.DataFormat = wb.CreateDataFormat().GetFormat("yyyy/m/d");

            const int dataStart = 4;
            int tTotal = pop.Items.Where(i => i.Class?.CourseId == 258).Sum(i => i.Number);

            int sumAS = 0, sumN = 0, sumL = 0, sumW = 0;
            var sumSubj = new int[_asSubjectGroups.Length * 2];

            for (int gi = 0; gi < _gradeOrder.Length; gi++) {
                var row = sheet.CreateRow(dataStart + gi);
                row.CreateCell(2).SetCellValue(_gradeOrder[gi]);

                int asVal = pop.Items.Where(i => i.Class?.CourseId == _asCourseIds["AS"][gi] && i.Class?.Type == AsColType("AS")).Sum(i => i.Number);
                if (asVal != 0) row.CreateCell(3).SetCellValue(asVal);
                sumAS += asVal;

                col = 4;
                for (int s = 0; s < _asSubjectGroups.Length; s++) {
                    var (oneCode, groupCode, _) = _asSubjectGroups[s];
                    int oneVal = pop.Items.Where(i => i.Class?.CourseId == _asCourseIds[oneCode][gi] && i.Class?.Type == AsColType(oneCode)).Sum(i => i.Number);
                    int groupVal = pop.Items.Where(i => i.Class?.CourseId == _asCourseIds[groupCode][gi] && i.Class?.Type == AsColType(groupCode)).Sum(i => i.Number);
                    if (oneVal != 0) row.CreateCell(col).SetCellValue(oneVal);
                    if (groupVal != 0) row.CreateCell(col + 1).SetCellValue(groupVal);
                    sumSubj[s * 2] += oneVal;
                    sumSubj[s * 2 + 1] += groupVal;
                    col += 2;
                }

                int nVal = pop.Items.Where(i => i.Class?.CourseId == _asCourseIds["N"][gi] && i.Class?.Type == AsColType("N")).Sum(i => i.Number);
                int lVal = pop.Items.Where(i => i.Class?.CourseId == _asCourseIds["L"][gi] && i.Class?.Type == AsColType("L")).Sum(i => i.Number);
                int wVal = pop.Items.Where(i => i.Class?.CourseId == _asCourseIds["W"][gi] && i.Class?.Type == AsColType("W")).Sum(i => i.Number);
                if (nVal != 0) row.CreateCell(analysisCol).SetCellValue(nVal);
                if (lVal != 0) row.CreateCell(analysisCol + 1).SetCellValue(lVal);
                if (wVal != 0) row.CreateCell(analysisCol + 2).SetCellValue(wVal);
                sumN += nVal; sumL += lVal; sumW += wVal;

                if (gi == 0) {
                    row.CreateCell(0).SetCellValue(week);
                    var dateCell = row.CreateCell(1);
                    dateCell.SetCellValue(pop.WeekDate);
                    dateCell.CellStyle = dateStyle;
                    if (tTotal != 0) row.CreateCell(analysisCol + 3).SetCellValue(tTotal);
                }
            }

            int subtotalRow = dataStart + _gradeOrder.Length;
            var subRow = sheet.CreateRow(subtotalRow);
            subRow.CreateCell(2).SetCellValue("小計");
            subRow.CreateCell(3).SetCellValue(sumAS);
            col = 4;
            for (int s = 0; s < _asSubjectGroups.Length; s++) {
                subRow.CreateCell(col).SetCellValue(sumSubj[s * 2]);
                subRow.CreateCell(col + 1).SetCellValue(sumSubj[s * 2 + 1]);
                col += 2;
            }
            subRow.CreateCell(analysisCol).SetCellValue(sumN);
            subRow.CreateCell(analysisCol + 1).SetCellValue(sumL);
            subRow.CreateCell(analysisCol + 2).SetCellValue(sumW);

            try { sheet.AddMergedRegion(new CellRangeAddress(dataStart, subtotalRow, 0, 0)); } catch { }
            try { sheet.AddMergedRegion(new CellRangeAddress(dataStart, subtotalRow, 1, 1)); } catch { }
            try { sheet.AddMergedRegion(new CellRangeAddress(dataStart, subtotalRow, analysisCol + 3, analysisCol + 3)); } catch { }

            sheet.SetColumnWidth(0, 6 * 256);
            sheet.SetColumnWidth(1, 10 * 256);
            sheet.SetColumnWidth(2, 8 * 256);
        }
    }

    // ── 共用 helper ───────────────────────────────────────────────────────────

    // 計算 IsSum 課程的合計值（對應 StatisticsCalculationService 邏輯）
    // rowClassType：本列的班別（PH 的 SubGroup/V3；GEPT/PS 傳 null）
    private static int ComputeIsumValue(Course course,
        IEnumerable<StudentPopulationItem> allItems, ClassType? rowClassType) {

        var statsTypeEarly = course.StatisticsType ?? InferStatisticsType(course.Name);
        if (statsTypeEarly == StatisticsType.DiffBetweenCourses) {
            // 來源課程本身可能是 IsSum=true（例如新生/流失手動輸入欄位），不能套用下方排除 IsSum 的 src 管線
            var ct2 = course.ApplicableClassType ?? (course.GroupByClassType ? rowClassType : null);
            var posIds = TryParseIntArray(course.SourceCourseIds);
            var negIds = TryParseIntArray(course.NegativeSourceCourseIds);
            int positive = allItems.Where(i => i.Class?.CourseId != null && posIds.Contains(i.Class.CourseId.Value))
                                    .Where(i => !ct2.HasValue || i.Class?.Type == ct2.Value).Sum(i => i.Number);
            int negative = allItems.Where(i => i.Class?.CourseId != null && negIds.Contains(i.Class.CourseId.Value))
                                    .Where(i => !ct2.HasValue || i.Class?.Type == ct2.Value).Sum(i => i.Number);
            return positive - negative;
        }

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

    // 給非管理員（或admin指定單一分校）匯出用：同一分校橫跨整學年至今累積的週次，依週次升冪排序
    private List<StudentPopulation> LoadWeeklyPopulationsForSchool(
        StudentPopulationType type, int year, int upToWeek, int schoolId) {

        return _context.StudentPopulation
            .Include(sp => sp.School)
            .Include(sp => sp.Items)
                .ThenInclude(i => i.Class)
                    .ThenInclude(c => c.Course)
                        .ThenInclude(c => c.Department)
            .Where(sp => sp.Year == year && sp.Week <= upToWeek && sp.Type == type && sp.SchoolId == schoolId)
            .OrderBy(sp => sp.Week)
            .ToList();
    }

    // publishedOnly=true（既有匯出用）：只取Published的原始可填欄位，跟匯入格式保持相容。
    // 分校週次列匯出需要完整還原畫面上所有IsSum合計/分析欄位，但這些課程在DB裡幾乎全部Published=0，
    // 所以改用publishedOnly:false取得全部課程（含IsSum、含未Published）。
    private List<Course> LoadCourses(StudentPopulationType type, bool publishedOnly = true) =>
        _context.Course
            .Include(c => c.Department)
            .Where(c => c.Type == type && (!publishedOnly || c.Published))
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
        // 單一分校：改成「每週一列」明細格式，比照 ExportPHBySchool 的作法
        if (schoolIds?.Count == 1) {
            return ExportPHDetailBySchool(year, week, schoolIds[0]);
        }

        var populations = LoadPopulations(StudentPopulationType.PH, year, week, schoolIds);
        if (populations.Count == 0) return Array.Empty<byte>();

        var courses = LoadCourses(StudentPopulationType.PH);
        var nonSumCourses = courses.Where(c => !c.IsSum).ToList();

        // 計算各 courseId 的全區最大班數（小班（含個別指導 Personal）與 V3 取其中較大值）
        var maxSlots = new Dictionary<int, int>();
        foreach (var pop in populations) {
            foreach (var courseId in nonSumCourses.Select(c => c.Id)) {
                int sg = pop.Items.Count(i => i.Class?.CourseId == courseId &&
                    (i.Class?.Type == ClassType.SubGroup || i.Class?.Type == ClassType.Personal));
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
                        .Where(i => i.Class?.CourseId == c.Id &&
                            (i.Class?.Type == ClassType.SubGroup || i.Class?.Type == ClassType.Personal))
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

    // ── PS 班級明細 ───────────────────────────────────────────────────────────
    // 格式：每校 1 列（PS 只有 ClassType.General，無小班/三人班區分）；
    // 跟 PH 明細不同的地方——PS 各校打的班名不一樣，欄位表頭沿用通用序號「第N班」，
    // 真實班名寫進儲存格內文字（"班名:人數"），代價是該欄位不再是純數字、Excel公式加總會受影響。

    public byte[] ExportPSDetail(int year, int week, IList<int> schoolIds = null) {
        var populations = LoadPopulations(StudentPopulationType.PS, year, week, schoolIds);
        if (populations.Count == 0) return Array.Empty<byte>();

        var courses = LoadCourses(StudentPopulationType.PS);
        var nonSumCourses = courses.Where(c => !c.IsSum).ToList();

        // 計算各 courseId 的全區最大班數
        var maxSlots = new Dictionary<int, int>();
        foreach (var pop in populations) {
            foreach (var courseId in nonSumCourses.Select(c => c.Id)) {
                int cnt = pop.Items.Count(i => i.Class?.CourseId == courseId);
                if (!maxSlots.TryGetValue(courseId, out int ex) || cnt > ex)
                    maxSlots[courseId] = cnt;
            }
        }

        // 只保留有資料的課程欄
        var activeCourses = nonSumCourses.Where(c => maxSlots.TryGetValue(c.Id, out int s) && s > 0).ToList();
        var deptGroups = activeCourses
            .GroupBy(c => c.Department.Id)
            .Select(g => (dept: g.First().Department, list: g.ToList()))
            .ToList();

        int fixedCols = 1;
        int totalCols = fixedCols + activeCourses.Sum(c => maxSlots[c.Id]) + deptGroups.Count;

        var wb    = new XSSFWorkbook();
        var sheet = wb.CreateSheet("Sheet1");

        sheet.CreateRow(0).CreateCell(0).SetCellValue($"{year}年第{week}週百世人數表（班級明細）");
        try { sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, totalCols - 1)); } catch { }

        var r1 = sheet.CreateRow(1);
        sheet.CreateRow(2);
        var r3 = sheet.CreateRow(3);
        r1.CreateCell(0).SetCellValue("分校");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }

        // Rows 1-3: 班系 → 課程 → 子欄位（序號表頭）
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
                    r3.CreateCell(col + si).SetCellValue($"第{si + 1}班");
                col += slots;
            }
            r1.CreateCell(deptStart).SetCellValue(dept.Name);
            int deptEnd = col;
            if (deptEnd > deptStart)
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, deptEnd)); } catch { }
            r3.CreateCell(col).SetCellValue("合計");
            col++;
        }

        // Row 4+: 每校 1 列
        int rowIdx = 4;
        foreach (var pop in populations) {
            var row = sheet.CreateRow(rowIdx);
            row.CreateCell(0).SetCellValue(pop.School?.Name ?? "");

            col = fixedCols;
            foreach (var (_, list) in deptGroups) {
                int deptTotal = 0;
                foreach (var c in list) {
                    int slots = maxSlots[c.Id];
                    var items = pop.Items
                        .Where(i => i.Class?.CourseId == c.Id)
                        .OrderBy(i => i.Class.Ordinal).ThenBy(i => i.Class.Id).ToList();
                    for (int si = 0; si < slots; si++) {
                        if (si < items.Count && items[si].Number > 0) {
                            string className = items[si].Class?.Name ?? items[si].Name ?? "";
                            row.CreateCell(col + si).SetCellValue($"{className}:{items[si].Number}");
                            deptTotal += items[si].Number;
                        }
                    }
                    col += slots;
                }
                if (deptTotal > 0) row.CreateCell(col).SetCellValue(deptTotal);
                col++;
            }
            rowIdx++;
        }

        sheet.SetColumnWidth(0, 20 * 256);
        for (int c = fixedCols; c < totalCols; c++)
            sheet.SetColumnWidth(c, 10 * 256);

        using var ms = new MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }

    // ── PH 班級明細（單一分校，每週一列）─────────────────────────────────────────
    // 格式跟 ExportPHDetail 一致（每課程依實際班級數展開N個子欄位，不含IsSum欄），
    // 只是外層迴圈從「每分校2列」改成「每週2列」，前導欄比照 BuildSheetPHBySchool 改成「週次/日期/開班模式」三欄。

    private byte[] ExportPHDetailBySchool(int year, int week, int schoolId) {
        var weeklyPopulations = LoadWeeklyPopulationsForSchool(StudentPopulationType.PH, year, week, schoolId);
        if (weeklyPopulations.Count == 0) return Array.Empty<byte>();

        var school = weeklyPopulations[0].School ?? _context.School.Find(schoolId);
        var courses = LoadCourses(StudentPopulationType.PH);
        var nonSumCourses = courses.Where(c => !c.IsSum).ToList();

        // 計算該分校各courseId橫跨這些週次的最大班數（小班（含個別指導Personal）與V3取其中較大值）
        var maxSlots = new Dictionary<int, int>();
        foreach (var pop in weeklyPopulations) {
            foreach (var courseId in nonSumCourses.Select(c => c.Id)) {
                int sg = pop.Items.Count(i => i.Class?.CourseId == courseId &&
                    (i.Class?.Type == ClassType.SubGroup || i.Class?.Type == ClassType.Personal));
                int v3 = pop.Items.Count(i => i.Class?.CourseId == courseId && i.Class?.Type == ClassType.V3);
                int mx = Math.Max(sg, v3);
                if (!maxSlots.TryGetValue(courseId, out int ex) || mx > ex)
                    maxSlots[courseId] = mx;
            }
        }

        var activeCourses = nonSumCourses.Where(c => maxSlots.TryGetValue(c.Id, out int s) && s > 0).ToList();
        var deptGroups = activeCourses
            .GroupBy(c => c.Department.Id)
            .Select(g => (dept: g.First().Department, list: g.ToList()))
            .ToList();

        int fixedCols = 3;
        int totalCols = fixedCols + activeCourses.Sum(c => maxSlots[c.Id]) + deptGroups.Count;

        var wb    = new XSSFWorkbook();
        var sheet = wb.CreateSheet(school?.Name ?? "Sheet1");

        string title = $"{school?.Name}分校人數統計表（班級明細） 填表日期: {DateTime.Now.Year - 1911}年{DateTime.Now.Month}月{DateTime.Now.Day}日(第{week}週)";
        sheet.CreateRow(0).CreateCell(0).SetCellValue(title);
        try { sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, totalCols - 1)); } catch { }

        var r1 = sheet.CreateRow(1);
        sheet.CreateRow(2);
        var r3 = sheet.CreateRow(3);
        r1.CreateCell(0).SetCellValue("週次");
        r1.CreateCell(1).SetCellValue("日期");
        r1.CreateCell(2).SetCellValue("開班模式");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 1, 1)); } catch { }
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 2, 2)); } catch { }

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
            r1.CreateCell(deptStart).SetCellValue(dept.Name);
            int deptEnd = col;
            if (deptEnd > deptStart)
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, deptEnd)); } catch { }
            r3.CreateCell(col).SetCellValue("合計");
            col++;
        }

        // Row 4+: 每週兩列（小班 / 三人班，無團）
        var dateStyle = sheet.Workbook.CreateCellStyle();
        dateStyle.DataFormat = sheet.Workbook.CreateDataFormat().GetFormat("yyyy/m/d");

        int rowIdx = 4;
        foreach (var pop in weeklyPopulations) {
            var sgRow = sheet.CreateRow(rowIdx);
            var v3Row = sheet.CreateRow(rowIdx + 1);
            sgRow.CreateCell(0).SetCellValue(pop.Week);
            var dateCell = sgRow.CreateCell(1);
            dateCell.SetCellValue(pop.WeekDate);
            dateCell.CellStyle = dateStyle;
            sgRow.CreateCell(2).SetCellValue("小班");
            v3Row.CreateCell(2).SetCellValue("三人班");
            try { sheet.AddMergedRegion(new CellRangeAddress(rowIdx, rowIdx + 1, 0, 0)); } catch { }
            try { sheet.AddMergedRegion(new CellRangeAddress(rowIdx, rowIdx + 1, 1, 1)); } catch { }

            col = fixedCols;
            foreach (var (_, list) in deptGroups) {
                int sgDeptTotal = 0, v3DeptTotal = 0;
                foreach (var c in list) {
                    int slots = maxSlots[c.Id];
                    var sgItems = pop.Items
                        .Where(i => i.Class?.CourseId == c.Id &&
                            (i.Class?.Type == ClassType.SubGroup || i.Class?.Type == ClassType.Personal))
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

        sheet.SetColumnWidth(0, 6 * 256);
        sheet.SetColumnWidth(1, 10 * 256);
        sheet.SetColumnWidth(2, 9 * 256);
        for (int c = fixedCols; c < totalCols; c++)
            sheet.SetColumnWidth(c, 7 * 256);

        using var ms = new MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }
}
