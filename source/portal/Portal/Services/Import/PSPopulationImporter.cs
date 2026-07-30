using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Services.Import;

public class PSPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.PS;

    public ImportScanResult Scan(DataContext db, Stream fileStream, int? overrideYear = null, int? overrideWeek = null) {
        var result = new ImportScanResult();
        var sheet = new XSSFWorkbook(fileStream).GetSheetAt(0);

        string title = sheet.GetRow(0)?.GetCell(0)?.ToString()?.Trim() ?? "";
        var (weekInt, titleDate) = TitleParser.ParseWeekAndDate(title);
        if (weekInt == 0 || titleDate == null) {
            result.Errors.Add($"無法從標題解析週次或日期: {title}");
            return result;
        }
        SchoolYear scanSchoolYear = db.SchoolYear.FirstOrDefault(e =>
            e.Week == weekInt && e.WeekStartDate <= titleDate.Value && titleDate.Value <= e.WeekEndDate);
        if (scanSchoolYear == null) {
            result.Errors.Add($"找不到符合的學年週次: 第{weekInt}週 日期{titleDate:yyyy-MM-dd}");
            return result;
        }
        int yearInt = scanSchoolYear.Year.Value;

        for (int rNo = 2; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string schoolName = row.GetCell(0)?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(schoolName) || schoolName == "總計") continue;

            School school = db.School.FirstOrDefault(e => e.Name == schoolName);
            bool exists = school != null && db.StudentPopulation.Any(e =>
                e.School.Id == school.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PS);
            result.Items.Add(new ImportScanItem {
                SchoolName = schoolName,
                SchoolId = school?.Id,
                Year = yearInt,
                Week = weekInt,
                Exists = exists,
            });
        }
        return result;
    }

    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger, int? overrideYear = null, int? overrideWeek = null) {
        var result = new ImportResult { Type = "PS" };
        var sheet = new XSSFWorkbook(fileStream).GetSheetAt(0);

        // Parse year/week from row 0 title
        string title = sheet.GetRow(0)?.GetCell(0)?.ToString()?.Trim() ?? "";
        var (weekInt, titleDate) = TitleParser.ParseWeekAndDate(title);
        if (weekInt == 0 || titleDate == null) {
            result.Errors.Add($"無法從標題解析週次或日期: {title}");
            return result;
        }
        SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e =>
            e.Week == weekInt && e.WeekStartDate <= titleDate.Value && titleDate.Value <= e.WeekEndDate);
        if (schoolYear == null) {
            result.Errors.Add($"找不到符合的學年週次: 第{weekInt}週 日期{titleDate:yyyy-MM-dd}");
            return result;
        }
        int yearInt = schoolYear.Year.Value;

        // Build col→course map from row 1 headers; fallback to alias dict when Course.Name doesn't match
        IRow headerRow = sheet.GetRow(1);
        var colCourseMap = new Dictionary<int, Course>();
        if (headerRow != null) {
            for (int c = 1; c < (int)headerRow.LastCellNum; c++) {
                var hCell = headerRow.GetCell(c);
                if (hCell == null || hCell.CellType != CellType.String) continue;
                string hdr = hCell.StringCellValue?.Trim() ?? "";
                if (string.IsNullOrEmpty(hdr)) continue;
                Course course = db.Course.Include("Department").FirstOrDefault(e => e.Name == hdr);
                if (course == null && CourseMapping.PsHeaderCourseId.TryGetValue(hdr, out int fallbackId))
                    course = db.Course.Include("Department").FirstOrDefault(e => e.Id == fallbackId);
                if (course != null) colCourseMap[c] = course;
            }
        }
        if (colCourseMap.Count == 0) {
            result.Errors.Add("Row 1 未找到任何課程對應，請確認課程名稱或別名是否與資料庫一致");
            return result;
        }

        // Process data rows starting at row 2
        for (int rNo = 2; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string schoolName = row.GetCell(0)?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(schoolName) || schoolName == "總計") continue;

            School school = db.School.FirstOrDefault(e => e.Name == schoolName);
            if (school == null) continue;

            StudentPopulation pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                StudentPopulationType.PS, $"{yearInt}第{weekInt}週百世人數表", true);
            result.PopulationIds.Add(pop.Id);
            result.SchoolCount++;

            foreach (var (col, course) in colCourseMap) {
                var cell = row.GetCell(col);
                if (cell == null || cell.CellType != CellType.Numeric) continue;
                int count = (int)System.Math.Round(cell.NumericCellValue, System.MidpointRounding.AwayFromZero);
                if (count <= 0) continue;
                PopulationWriteHelper.AddClassAndItem(db, school.Id, course, ClassType.General, pop.Id, count, result, logger);
            }
        }
        return result;
    }
}
