using System;
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

public class PSJPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.PSJ;

    public ImportScanResult Scan(DataContext db, Stream fileStream) {
        var result = new ImportScanResult();
        var sheet = new XSSFWorkbook(fileStream).GetSheetAt(0);
        var seen = new HashSet<(int year, int week, string school)>();

        for (int rNo = 5; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string schoolName = row.GetCell(2)?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(schoolName)) continue;
            if (!int.TryParse(row.GetCell(0)?.ToString()?.Trim(), out int yearInt)) continue;
            if (!int.TryParse(row.GetCell(1)?.ToString()?.Trim(), out int weekInt)) continue;
            string grade = row.GetCell(3)?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(grade)) continue;

            var key = (yearInt, weekInt, schoolName);
            if (seen.Contains(key)) continue;
            seen.Add(key);

            School school = db.School.FirstOrDefault(e => e.Name == schoolName);
            bool exists = school != null && db.StudentPopulation.Any(e =>
                e.School.Id == school.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PSJ);
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

    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger) {
        var result = new ImportResult { Type = "PSJ" };
        var sheet = new XSSFWorkbook(fileStream).GetSheetAt(0);
        IRow headerRow = sheet.GetRow(4);

        int prevSchoolId = 0;
        StudentPopulation pop = null;

        for (int rNo = 5; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string schoolName = row.GetCell(2)?.ToString()?.Trim() ?? "";
            School school = db.School.FirstOrDefault(e => e.Name == schoolName);
            if (school == null) continue;

            if (!int.TryParse(row.GetCell(0)?.ToString()?.Trim(), out int yearInt)) continue;
            if (!int.TryParse(row.GetCell(1)?.ToString()?.Trim(), out int weekInt)) continue;
            string grade = row.GetCell(3)?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(grade)) continue;

            SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
            if (schoolYear == null) continue;

            if (school.Id != prevSchoolId) {
                prevSchoolId = school.Id;
                pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                    StudentPopulationType.PSJ, $"{yearInt}第{weekInt}週百倍速人數表", true);
                result.PopulationIds.Add(pop.Id);
                result.SchoolCount++;
            }

            int gradeIdx = Array.IndexOf(CourseMapping.GradeOrder, grade);
            if (gradeIdx < 0) continue;

            for (int cNo = 4; cNo < headerRow.LastCellNum; cNo++) {
                try {
                    string code = headerRow.GetCell(cNo)?.ToString()?.Trim() ?? "";
                    if (code.Equals("X", StringComparison.OrdinalIgnoreCase)) continue;

                    int courseId;
                    ClassType cType;
                    if (code == "T") {
                        courseId = 158; cType = ClassType.General;
                    }
                    else if (CourseMapping.PsjCourseIds.TryGetValue(code, out int[] ids)) {
                        courseId = ids[gradeIdx]; cType = CourseMapping.PsjColumnType(code);
                    }
                    else { continue; }

                    Course course = db.Course.Include("Department").FirstOrDefault(e => e.Id == courseId);
                    int count = CourseMapping.ReadCellNumber(row, cNo);
                    if (course == null || count <= 0) continue;

                    PopulationWriteHelper.AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result, logger);
                }
                catch { continue; }
            }
        }
        return result;
    }
}
