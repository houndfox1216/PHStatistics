using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import.ImportSupport;

public static class PHSheetReader {
    // Shared sheet reader for PH-style sheets (title row 0, headers rows 1-3, data from row 4).
    // requireTypeIndicator=true  → only process rows where col1 is "小" or "三" (PH behaviour)
    // requireTypeIndicator=false → rows without "小"/"三" are treated as ClassType.General (GEPT behaviour)
    public static ImportResult Run(DataContext db, ISheet sheet,
        StudentPopulationType popType, string typeLabel, string nameTemplate,
        bool requireTypeIndicator, ILogger logger) {

        var result = new ImportResult { File = sheet.SheetName, Type = typeLabel };

        // 標題可能不在 Cell 0（合併儲存格），掃 Row 0 找第一個非空儲存格
        string title = "";
        IRow titleRow = sheet.GetRow(0);
        if (titleRow != null) {
            for (int ci = 0; ci < Math.Min(10, (int)titleRow.LastCellNum + 1); ci++) {
                string v = titleRow.GetCell(ci)?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(v)) { title = v; break; }
            }
        }
        var (yearInt, weekInt) = TitleParser.ParseYearWeek(title);
        if (yearInt == 0 || weekInt == 0) {
            result.Errors.Add($"無法從標題解析年份週次: {title}");
            return result;
        }
        SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
        if (schoolYear == null) {
            result.Errors.Add($"找不到學年週次: {yearInt}第{weekInt}週");
            return result;
        }

        int maxHdrCol = 0;
        for (int r = 1; r <= 3; r++) {
            var rw = sheet.GetRow(r);
            if (rw != null && (int)rw.LastCellNum > maxHdrCol) maxHdrCol = (int)rw.LastCellNum;
        }

        var row2Prop = new string[maxHdrCol + 1];
        string lastR2 = "";
        for (int c = 0; c <= maxHdrCol; c++) {
            var cell = sheet.GetRow(2)?.GetCell(c);
            string v = (cell?.CellType == CellType.String) ? (cell.StringCellValue?.Trim() ?? "") : "";
            if (!string.IsNullOrEmpty(v)) lastR2 = v;
            row2Prop[c] = lastR2;
        }

        var colCourseMap = new Dictionary<int, Course>();
        for (int c = 2; c < maxHdrCol; c++) {
            var r3Cell = sheet.GetRow(3)?.GetCell(c);
            string r3Val = (r3Cell?.CellType == CellType.String) ? (r3Cell.StringCellValue?.Trim() ?? "") : "";
            string label = !string.IsNullOrEmpty(r3Val) ? r3Val : row2Prop[c];
            Course course = null;
            if (!string.IsNullOrEmpty(label))
                course = db.Course.Include("Department").FirstOrDefault(e => e.Name == label);
            if (course == null && CourseMapping.PhColCourseId.TryGetValue(c, out int fallbackId))
                course = db.Course.Include("Department").FirstOrDefault(e => e.Id == fallbackId);
            if (course != null) colCourseMap[c] = course;
        }

        string currentSchoolName = "";
        string lastCountedSchool = "";
        for (int rNo = 4; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string col0 = row.GetCell(0)?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(col0)) currentSchoolName = col0;
            if (string.IsNullOrEmpty(currentSchoolName)) continue;

            string col1 = row.GetCell(1)?.ToString()?.Trim() ?? "";
            ClassType cType;
            bool deleteExisting;
            bool isFirstRow;

            if (col1 == "小") {
                cType = ClassType.SubGroup; deleteExisting = true; isFirstRow = true;
            } else if (col1 == "三") {
                cType = ClassType.V3; deleteExisting = false; isFirstRow = false;
            } else if (!requireTypeIndicator) {
                cType = ClassType.General;
                isFirstRow = currentSchoolName != lastCountedSchool;
                deleteExisting = isFirstRow;
            } else {
                continue;
            }

            School school = db.School.FirstOrDefault(e => e.Name == currentSchoolName);
            if (school == null) continue;

            StudentPopulation pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                popType, string.Format(nameTemplate, yearInt, weekInt), deleteExisting);
            result.PopulationIds.Add(pop.Id);
            if (isFirstRow) { result.SchoolCount++; lastCountedSchool = currentSchoolName; }

            foreach (var (col, course) in colCourseMap) {
                var cell = row.GetCell(col);
                if (cell == null || cell.CellType != CellType.Numeric) continue;
                int count = (int)Math.Round(cell.NumericCellValue, MidpointRounding.AwayFromZero);
                if (count <= 0) continue;
                if (CourseMapping.Em1CourseIds.Contains(course.Id)) {
                    for (int i = 0; i < count; i++)
                        PopulationWriteHelper.AddClassAndItem(db, school.Id, course, cType, pop.Id, 1, result, logger);
                } else {
                    PopulationWriteHelper.AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result, logger);
                }
            }
        }
        return result;
    }

    public static ImportScanResult Scan(DataContext db, ISheet sheet, StudentPopulationType popType, bool requireTypeIndicator) {
        var result = new ImportScanResult();

        string title = "";
        IRow titleRow = sheet.GetRow(0);
        if (titleRow != null) {
            for (int ci = 0; ci < Math.Min(10, (int)titleRow.LastCellNum + 1); ci++) {
                string v = titleRow.GetCell(ci)?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(v)) { title = v; break; }
            }
        }
        var (yearInt, weekInt) = TitleParser.ParseYearWeek(title);
        if (yearInt == 0 || weekInt == 0) {
            result.Errors.Add($"無法從標題解析年份週次: {title}");
            return result;
        }

        string currentSchoolName = "";
        string lastCountedSchool = "";
        var seen = new HashSet<string>();
        for (int rNo = 4; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string col0 = row.GetCell(0)?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(col0)) currentSchoolName = col0;
            if (string.IsNullOrEmpty(currentSchoolName)) continue;

            string col1 = row.GetCell(1)?.ToString()?.Trim() ?? "";
            bool isFirstRow;
            if (col1 == "小") {
                isFirstRow = true;
            } else if (col1 == "三") {
                isFirstRow = false;
            } else if (!requireTypeIndicator) {
                isFirstRow = currentSchoolName != lastCountedSchool;
            } else {
                continue;
            }

            if (!isFirstRow || seen.Contains(currentSchoolName)) continue;
            seen.Add(currentSchoolName);
            lastCountedSchool = currentSchoolName;

            School school = db.School.FirstOrDefault(e => e.Name == currentSchoolName);
            bool exists = school != null && db.StudentPopulation.Any(e =>
                e.School.Id == school.Id && e.Year == yearInt && e.Week == weekInt && e.Type == popType);
            result.Items.Add(new ImportScanItem {
                SchoolName = currentSchoolName,
                SchoolId = school?.Id,
                Year = yearInt,
                Week = weekInt,
                Exists = exists,
            });
        }
        return result;
    }
}
