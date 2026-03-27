using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Report;

namespace PHStatistics.Portal.Services;

/// <summary>
/// 人數統計報表匯出服務。
/// 依照 StudentPopulationType 產生對應的 Excel 報表（原手填格式）。
///
/// 欄位規則：
///   - IsSum=false 的課程 → 資料欄（Data），欄數依各分校實際班級數動態決定
///   - IsSum=true  的課程 → 計算欄（Sum），值來自 StudentPopulationItem.IsSum 項目
///
/// 班別（ClassType）規則：
///   - Personal (EM1)：個別指導，一對一，值=人數=班級數，一個課程固定 1 欄
///   - SubGroup  (小)：小組班，一欄一班，值=該班人數
///   - V3        (三)：三人班，一欄一班，值=該班人數
///   當分校增班（人數達上限）時，相同課程下新增一欄
/// </summary>
public class ReportExportService {
    private readonly DataContext _context;

    public ReportExportService() => _context = new DataContext();

    // ─────────────────────────── 主入口 ───────────────────────────

    /// <summary>
    /// 匯出報表
    /// </summary>
    /// <param name="type">報表類型（PH/GEPT/PS/PSJ/AfterSchool）</param>
    /// <param name="year">學年度</param>
    /// <param name="week">週次</param>
    /// <param name="schoolIds">限定分校 Id（null = 所有分校）</param>
    public byte[] Export(StudentPopulationType type, int year, int week,
                          IList<int> schoolIds = null) {
        var populations = LoadPopulations(type, year, week, schoolIds);
        if (populations.Count == 0) return Array.Empty<byte>();

        var courses = LoadCourses(type);
        var columns = BuildColumns(courses, populations);

        var wb     = new XSSFWorkbook();
        var styles = CreateStyles(wb);

        // 多分校：依區域分 Sheet；單分校：直接一個 Sheet
        bool isMultiSchool = schoolIds == null || schoolIds.Count > 1;
        if (isMultiSchool) {
            var byRegion = populations
                .GroupBy(p => p.School?.Region?.Name ?? "未分區")
                .OrderBy(g => g.Min(p => p.School?.Region?.Ordinal ?? 999));

            int page = 1;
            foreach (var grp in byRegion) {
                string title = BuildTitle(type, grp.Key, year, week, page++);
                BuildSheet(wb, grp.Key, title, columns, grp.ToList(), styles, isMultiSchool);
            }
        } else {
            var school = populations.First().School;
            string title = BuildTitle(type, school?.Name ?? "", year, week, 1);
            BuildSheet(wb, school?.Name ?? "報表", title, columns, populations, styles, false);
        }

        using var ms = new MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }

    // ─────────────────────────── 資料查詢 ───────────────────────────

    private List<StudentPopulation> LoadPopulations(StudentPopulationType type,
                                                     int year, int week,
                                                     IList<int> schoolIds) {
        var query = _context.StudentPopulation
            .Include(sp => sp.School).ThenInclude(s => s.Region)
            .Include(sp => sp.Items)
                .ThenInclude(i => i.Class)
                    .ThenInclude(c => c.Course)
                        .ThenInclude(c => c.Department)
            .Where(sp => sp.Year == year && sp.Week == week && sp.Type == type);

        if (schoolIds?.Count > 0)
            query = query.Where(sp => schoolIds.Contains(sp.SchoolId ?? 0));

        return query.OrderBy(sp => sp.School.Region.Ordinal)
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

    // ─────────────────────────── 欄位建構 ───────────────────────────

    private static List<ReportColumnDef> BuildColumns(List<Course> courses,
                                                       List<StudentPopulation> populations) {
        var cols = new List<ReportColumnDef>();

        foreach (var course in courses) {
            if (course.IsSum) {
                // 計算欄（標記 X 的欄位）
                cols.Add(new ReportColumnDef {
                    Kind           = ReportColumnKind.Sum,
                    DepartmentId   = course.DepartmentId ?? 0,
                    DepartmentName = course.Department?.Name ?? "",
                    CourseId       = course.Id,
                    CourseName     = course.Name,
                    SumItemName    = course.Name
                });
            } else {
                // 資料欄：依實際出現的 ClassType 建立動態欄數
                var classTypes = ResolveClassTypes(course, populations);

                foreach (var ct in classTypes) {
                    int maxInstances = ct == ClassType.Personal
                        // EM1：固定 1 欄（值=人數=班級數）
                        ? (populations.Any(sp => sp.Items.Any(i =>
                              !i.IsSum && i.Class?.CourseId == course.Id
                                       && i.Class?.Type == ct)) ? 1 : 0)
                        // 其他：依各分校最多班級數決定欄數
                        : populations
                            .Select(sp => sp.Items.Count(i =>
                                !i.IsSum && i.Class?.CourseId == course.Id
                                         && i.Class?.Type == ct))
                            .DefaultIfEmpty(0).Max();

                    for (int idx = 0; idx < maxInstances; idx++) {
                        cols.Add(new ReportColumnDef {
                            Kind           = ReportColumnKind.Data,
                            DepartmentId   = course.DepartmentId ?? 0,
                            DepartmentName = course.Department?.Name ?? "",
                            CourseId       = course.Id,
                            CourseName     = course.Name,
                            ClassType      = ct,
                            InstanceIndex  = idx
                        });
                    }
                }
            }
        }

        return cols;
    }

    /// <summary>
    /// 決定此課程適用的 ClassType 清單（依實際資料 + Course 設定）
    /// </summary>
    private static IEnumerable<ClassType> ResolveClassTypes(Course course,
                                                              List<StudentPopulation> populations) {
        // 優先使用 Course 上明確設定的班別
        if (course.ApplicableClassType.HasValue)
            return new[] { course.ApplicableClassType.Value };

        // 否則從實際資料中推斷（確保只顯示真實存在的班別，並依 ClassType 值排序）
        var found = populations
            .SelectMany(sp => sp.Items.Where(i =>
                !i.IsSum && i.Class?.CourseId == course.Id))
            .Select(i => i.Class!.Type)
            .Distinct()
            .OrderBy(ct => (int)ct)
            .ToList();

        // 若資料中完全沒有此課程（新課程），預設 SubGroup + V3
        return found.Count > 0 ? found : new[] { ClassType.SubGroup, ClassType.V3 };
    }

    // ─────────────────────────── Excel Sheet 建構 ───────────────────────────

    private static void BuildSheet(XSSFWorkbook wb, string sheetName, string title,
                                    List<ReportColumnDef> columns,
                                    List<StudentPopulation> populations,
                                    Dictionary<string, ICellStyle> styles,
                                    bool showRegionSummary) {
        var sheet = wb.CreateSheet(sheetName);
        int totalCols = columns.Count + 1;  // +1 for school name column

        // ── Row 0：標題列 ──
        var r0 = sheet.CreateRow(0);
        SetCell(r0, 0, title, styles["Title"]);
        sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, totalCols - 1));
        r0.HeightInPoints = 24;

        // ── Row 1-3：三層表頭 ──
        var r1 = sheet.CreateRow(1);
        var r2 = sheet.CreateRow(2);
        var r3 = sheet.CreateRow(3);

        // 校名欄（跨 3 行）
        SetCell(r1, 0, "校名＆班別＆人數", styles["Header"]);
        sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0));

        // 課程欄
        WriteColumnHeaders(sheet, r1, r2, r3, columns, styles);

        // ── Row 4+：資料列 ──
        int rowIdx = 4;
        foreach (var sp in populations) {
            var row = sheet.CreateRow(rowIdx++);
            SetCell(row, 0, sp.School?.Name ?? "", styles["DataSchool"]);

            for (int c = 0; c < columns.Count; c++) {
                var colDef = columns[c];
                int val    = GetCellValue(sp, colDef);
                var style  = colDef.Kind == ReportColumnKind.Sum ? styles["DataSum"] : styles["Data"];
                var cell   = row.CreateCell(c + 1);
                if (val != 0) cell.SetCellValue(val);
                cell.CellStyle = style;
            }
        }

        // ── 加總列（多分校時）──
        if (showRegionSummary && populations.Count > 1) {
            var sumRow = sheet.CreateRow(rowIdx);
            SetCell(sumRow, 0, "合　計", styles["SumRow"]);

            for (int c = 0; c < columns.Count; c++) {
                int total = populations.Sum(sp => GetCellValue(sp, columns[c]));
                var cell  = sumRow.CreateCell(c + 1);
                if (total != 0) cell.SetCellValue(total);
                cell.CellStyle = styles["SumRow"];
            }
        }

        // ── 欄寬 ──
        sheet.SetColumnWidth(0, 20 * 256);
        for (int c = 1; c < totalCols; c++)
            sheet.SetColumnWidth(c, 7 * 256);
        sheet.CreateFreezePane(1, 4);  // 凍結校名欄與表頭
    }

    private static void WriteColumnHeaders(ISheet sheet,
                                            IRow r1, IRow r2, IRow r3,
                                            List<ReportColumnDef> columns,
                                            Dictionary<string, ICellStyle> styles) {
        int deptStart  = 1, lastDeptId  = -1;
        int courseStart = 1, lastCourseId = -1;

        for (int i = 0; i < columns.Count; i++) {
            var col     = columns[i];
            int excelCol = i + 1;

            // 班系（Level 1）
            if (col.DepartmentId != lastDeptId) {
                if (lastDeptId >= 0 && excelCol - 1 > deptStart)
                    sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, excelCol - 1));
                SetCell(r1, excelCol, col.DepartmentName, styles["Header"]);
                deptStart   = excelCol;
                lastDeptId  = col.DepartmentId;
            } else {
                r1.CreateCell(excelCol).CellStyle = styles["Header"];
            }

            // 課程（Level 2）
            if (col.CourseId != lastCourseId) {
                if (lastCourseId >= 0 && excelCol - 1 > courseStart)
                    sheet.AddMergedRegion(new CellRangeAddress(2, 2, courseStart, excelCol - 1));
                var style = col.Kind == ReportColumnKind.Sum ? styles["HeaderSum"] : styles["Header"];
                SetCell(r2, excelCol, col.CourseName, style);
                courseStart  = excelCol;
                lastCourseId = col.CourseId;
            } else {
                r2.CreateCell(excelCol).CellStyle = styles["Header"];
            }

            // 班別 / 合計（Level 3）
            var l3Style = col.Kind == ReportColumnKind.Sum ? styles["HeaderSum"] : styles["Header"];
            SetCell(r3, excelCol, col.ClassLabel, l3Style);
        }

        // 收尾合併
        int last = columns.Count;
        if (lastDeptId   >= 0 && last > deptStart)
            sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, last));
        if (lastCourseId >= 0 && last > courseStart)
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, courseStart, last));
    }

    // ─────────────────────────── 取值邏輯 ───────────────────────────

    private static int GetCellValue(StudentPopulation sp, ReportColumnDef col) {
        if (col.Kind == ReportColumnKind.Sum) {
            // 計算欄：從 IsSum 項目依名稱取值
            return sp.Items
                .Where(i => i.IsSum && i.Name == col.SumItemName)
                .Select(i => i.Number)
                .FirstOrDefault();
        }

        if (col.ClassType == ClassType.Personal) {
            // EM1 個別指導：加總此課程所有 Personal 項目（人數 = 班級數）
            return sp.Items
                .Where(i => !i.IsSum
                         && i.Class?.CourseId == col.CourseId
                         && i.Class?.Type     == ClassType.Personal)
                .Sum(i => i.Number);
        }

        // SubGroup / V3 / 其他：取第 N 個班（依 Class.Ordinal 排序）
        return sp.Items
            .Where(i => !i.IsSum
                     && i.Class?.CourseId == col.CourseId
                     && i.Class?.Type     == col.ClassType)
            .OrderBy(i => i.Class?.Ordinal ?? 0)
            .ElementAtOrDefault(col.InstanceIndex)
            ?.Number ?? 0;
    }

    // ─────────────────────────── 樣式建立 ───────────────────────────

    private static Dictionary<string, ICellStyle> CreateStyles(XSSFWorkbook wb) {
        var normal = CreateFont(wb, bold: false, size: 10);
        var bold   = CreateFont(wb, bold: true,  size: 10);
        var boldSm = CreateFont(wb, bold: true,  size: 9);

        var styles = new Dictionary<string, ICellStyle>();

        // 標題列
        styles["Title"] = MakeStyle(wb, bold, HorizontalAlignment.Center,
            fillColor: IndexedColors.Indigo.Index, fontColor: IndexedColors.White.Index);

        // 表頭（班系/課程）
        styles["Header"] = MakeStyle(wb, boldSm, HorizontalAlignment.Center,
            fillColor: IndexedColors.LightYellow.Index);

        // 計算欄表頭（綠底）
        styles["HeaderSum"] = MakeStyle(wb, boldSm, HorizontalAlignment.Center,
            fillColor: IndexedColors.LightGreen.Index);

        // 資料儲存格 - 校名
        styles["DataSchool"] = MakeStyle(wb, normal, HorizontalAlignment.Left);

        // 資料儲存格 - 一般數值
        styles["Data"] = MakeStyle(wb, normal, HorizontalAlignment.Center);

        // 資料儲存格 - 計算欄（淡綠底）
        styles["DataSum"] = MakeStyle(wb, normal, HorizontalAlignment.Center,
            fillColor: IndexedColors.LightGreen.Index);

        // 合計列
        styles["SumRow"] = MakeStyle(wb, bold, HorizontalAlignment.Center,
            fillColor: IndexedColors.Grey25Percent.Index);

        return styles;
    }

    private static IFont CreateFont(XSSFWorkbook wb, bool bold, short size,
                                     short fontColor = 8 /* IndexedColors.Black */) {
        var font = wb.CreateFont();
        font.FontName           = "Arial";
        font.FontHeightInPoints = size;
        font.IsBold             = bold;
        font.Color              = fontColor;
        return font;
    }

    private static ICellStyle MakeStyle(XSSFWorkbook wb, IFont font,
                                         HorizontalAlignment align,
                                         short fillColor  = 9 /* IndexedColors.White */,
                                         short fontColor  = 8 /* IndexedColors.Black */) {
        var s = wb.CreateCellStyle();
        s.SetFont(font);
        s.Alignment        = align;
        s.VerticalAlignment = VerticalAlignment.Center;
        s.WrapText         = true;
        s.BorderTop = s.BorderBottom = s.BorderLeft = s.BorderRight = BorderStyle.Thin;

        if (fillColor != IndexedColors.White.Index) {
            s.FillForegroundColor = fillColor;
            s.FillPattern         = FillPattern.SolidForeground;
        }
        return s;
    }

    private static void SetCell(IRow row, int col, string value, ICellStyle style) {
        var cell = row.CreateCell(col);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    // ─────────────────────────── 輔助方法 ───────────────────────────

    private static string BuildTitle(StudentPopulationType type, string regionName,
                                      int year, int week, int page) =>
        $"{TypeDisplayName(type)} {regionName} 人數統計表  " +
        $"填表日期：{year}學年度第{week}週  Page.{page}";

    private static string TypeDisplayName(StudentPopulationType type) => type switch {
        StudentPopulationType.PH          => "百瀚英語",
        StudentPopulationType.GEPT        => "英語檢定",
        StudentPopulationType.PS          => "百世資優",
        StudentPopulationType.PSJ         => "百倍數",
        StudentPopulationType.AfterSchool => "安親課輔",
        _                                 => type.ToString()
    };
}
