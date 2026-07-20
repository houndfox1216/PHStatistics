using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Services.Import;

public class ASPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.AfterSchool;

    public ImportScanResult Scan(DataContext db, Stream fileStream, int? overrideYear = null, int? overrideWeek = null) {
        var result = new ImportScanResult();
        var workbook = new XSSFWorkbook(fileStream);

        for (int sheetIdx = 0; sheetIdx < workbook.NumberOfSheets; sheetIdx++) {
            var sheet = workbook.GetSheetAt(sheetIdx);
            string schoolName = Regex.Replace(sheet.SheetName, @"^\d+", "").Trim();
            School school = db.School.FirstOrDefault(e => e.Name == schoolName)
                ?? db.School.FirstOrDefault(e => e.Name == CourseMapping.AsChineseNumerals(schoolName));
            if (school == null) {
                result.Errors.Add($"找不到分校: {sheet.SheetName} (解析為 {schoolName})");
                continue;
            }

            string title = sheet.GetRow(0)?.GetCell(0)?.ToString()?.Trim() ?? "";
            var yearMatch = Regex.Match(title, @"(\d+)學年度");
            if (!yearMatch.Success) {
                result.Errors.Add($"頁籤 {sheet.SheetName}: 無法從標題解析學年度: {title}");
                continue;
            }
            int yearInt = int.Parse(yearMatch.Groups[1].Value);

            int weekInt = 0;
            for (int r = 4; r <= sheet.LastRowNum; r++) {
                IRow wr = sheet.GetRow(r);
                if (wr == null) continue;
                if (int.TryParse(wr.GetCell(0)?.ToString()?.Trim(), out int w) && w > weekInt)
                    weekInt = w;
            }
            if (weekInt == 0) {
                result.Errors.Add($"頁籤 {sheet.SheetName}: 找不到有效週次");
                continue;
            }

            bool exists = db.StudentPopulation.Any(e =>
                e.School.Id == school.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.AfterSchool);
            result.Items.Add(new ImportScanItem {
                SchoolName = schoolName,
                SchoolId = school.Id,
                Year = yearInt,
                Week = weekInt,
                Exists = exists,
            });
        }
        return result;
    }

    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger, int? overrideYear = null, int? overrideWeek = null) {
        var result = new ImportResult { Type = "AS" };
        var workbook = new XSSFWorkbook(fileStream);

        for (int sheetIdx = 0; sheetIdx < workbook.NumberOfSheets; sheetIdx++) {
            var sheet = workbook.GetSheetAt(sheetIdx);
            string schoolName = Regex.Replace(sheet.SheetName, @"^\d+", "").Trim();
            // 嘗試完全比對，失敗時轉換阿拉伯數字為中文（農16 → 農十六）
            School school = db.School.FirstOrDefault(e => e.Name == schoolName)
                ?? db.School.FirstOrDefault(e => e.Name == CourseMapping.AsChineseNumerals(schoolName));
            if (school == null) {
                result.Errors.Add($"找不到分校: {sheet.SheetName} (解析為 {schoolName})");
                continue;
            }

            string title = sheet.GetRow(0)?.GetCell(0)?.ToString()?.Trim() ?? "";
            var yearMatch = Regex.Match(title, @"(\d+)學年度");
            if (!yearMatch.Success) {
                result.Errors.Add($"頁籤 {sheet.SheetName}: 無法從標題解析學年度: {title}");
                continue;
            }
            int yearInt = int.Parse(yearMatch.Groups[1].Value);

            // 取最大週次（檔案可能含多週歷史資料，只匯入最新週）
            int weekInt = 0;
            for (int r = 4; r <= sheet.LastRowNum; r++) {
                IRow wr = sheet.GetRow(r);
                if (wr == null) continue;
                if (int.TryParse(wr.GetCell(0)?.ToString()?.Trim(), out int w) && w > weekInt)
                    weekInt = w;
            }
            if (weekInt == 0) {
                result.Errors.Add($"頁籤 {sheet.SheetName}: 找不到有效週次");
                continue;
            }

            SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
            if (schoolYear == null) {
                result.Errors.Add($"頁籤 {sheet.SheetName}: SchoolYear 不存在 (年{yearInt} 週{weekInt})");
                continue;
            }

            StudentPopulation pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                StudentPopulationType.AfterSchool, $"{yearInt}第{weekInt}週課輔人數表", true);
            result.PopulationIds.Add(pop.Id);
            result.SchoolCount++;

            var colDefs = new[] {
                (col: 3, code: "AS", cType: ClassType.General),
                (col: 4, code: "EP", cType: ClassType.Personal),
                (col: 5, code: "EG", cType: ClassType.General),
                (col: 6, code: "MP", cType: ClassType.Personal),
                (col: 7, code: "MG", cType: ClassType.General),
                (col: 8, code: "SP", cType: ClassType.Personal),
                (col: 9, code: "SG", cType: ClassType.General),
            };

            // col 0 的週次只出現在每週第一列，後續同週的列 col 0 為空
            // 用 currentWeek 追蹤目前所屬週次，確保每週所有年級列都被處理
            int currentWeek = 0;
            for (int rNo = 4; rNo <= sheet.LastRowNum; rNo++) {
                IRow row = sheet.GetRow(rNo);
                if (row == null) continue;

                string col0 = row.GetCell(0)?.ToString()?.Trim() ?? "";
                if (int.TryParse(col0, out int rowWeek) && rowWeek > 0)
                    currentWeek = rowWeek;

                if (currentWeek != weekInt) continue;

                string grade = row.GetCell(2)?.ToString()?.Trim() ?? "";
                int gradeIdx = Array.IndexOf(CourseMapping.GradeOrder, grade);
                if (gradeIdx < 0) continue;

                foreach (var (col, code, cType) in colDefs) {
                    try {
                        int courseId = CourseMapping.AsCourseIds[code][gradeIdx];
                        Course course = db.Course.Include("Department").FirstOrDefault(e => e.Id == courseId);
                        int count = CourseMapping.ReadCellNumber(row, col);
                        if (course == null || count <= 0) continue;
                        PopulationWriteHelper.AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result, logger);
                    }
                    catch { continue; }
                }
            }
        }
        return result;
    }
}
