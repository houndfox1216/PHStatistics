using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Services.Import;

public class PSJPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.PSJ;

    // 欄位配置：1-based（跟設計文件的人類可讀欄號一致），實際讀 Cell 時各處自行 -1。
    // 北區只有 4 個數學班小組班子欄（第一~第四班），南區有 5 個（第一~第五班），
    // 導致南區理化班/分析欄位往後多推 1 格——這是兩份頁籤唯一的欄位配置差異。
    internal sealed class ColumnLayout {
        public int NameCol, GradeCol;
        public int CkcEnglishGroupCol, CkcEnglishPersonalCol;
        public int CkcChineseGroupCol, CkcChinesePersonalCol;
        public int CkcMathGroupCol, CkcMathPersonalCol;
        public int MathPersonalCol;
        public int[] MathGroupCols;
        public int SciencePersonalCol, ScienceGroupCol;

        public ColumnLayout Shift(int offset) => new ColumnLayout {
            NameCol = NameCol + offset, GradeCol = GradeCol + offset,
            CkcEnglishGroupCol = CkcEnglishGroupCol + offset, CkcEnglishPersonalCol = CkcEnglishPersonalCol + offset,
            CkcChineseGroupCol = CkcChineseGroupCol + offset, CkcChinesePersonalCol = CkcChinesePersonalCol + offset,
            CkcMathGroupCol = CkcMathGroupCol + offset, CkcMathPersonalCol = CkcMathPersonalCol + offset,
            MathPersonalCol = MathPersonalCol + offset,
            MathGroupCols = MathGroupCols.Select(c => c + offset).ToArray(),
            SciencePersonalCol = SciencePersonalCol + offset, ScienceGroupCol = ScienceGroupCol + offset,
        };

        public static readonly ColumnLayout North = new ColumnLayout {
            NameCol = 1, GradeCol = 2,
            CkcEnglishGroupCol = 3, CkcEnglishPersonalCol = 4,
            CkcChineseGroupCol = 5, CkcChinesePersonalCol = 6,
            CkcMathGroupCol = 7, CkcMathPersonalCol = 8,
            MathPersonalCol = 9, MathGroupCols = new[] { 10, 11, 12, 13 },
            SciencePersonalCol = 14, ScienceGroupCol = 15,
        };

        public static readonly ColumnLayout SouthLeft = new ColumnLayout {
            NameCol = 1, GradeCol = 2,
            CkcEnglishGroupCol = 3, CkcEnglishPersonalCol = 4,
            CkcChineseGroupCol = 5, CkcChinesePersonalCol = 6,
            CkcMathGroupCol = 7, CkcMathPersonalCol = 8,
            MathPersonalCol = 9, MathGroupCols = new[] { 10, 11, 12, 13, 14 },
            SciencePersonalCol = 15, ScienceGroupCol = 16,
        };
    }

    // 用合併儲存格範圍找分校名區塊，而不是文字向下延伸（carry-forward）：
    // 真實檔案裡有些區塊（例如北區 rows 41-52）合併儲存格本身就是空白（沒有分校名，是預留格），
    // carry-forward 會誤把它當成上一個分校的延伸列；讀合併區域自己的值則會正確得到空字串並被跳過。
    internal static List<(string SchoolName, int FirstRow, int LastRow)> FindSchoolBlocks(ISheet sheet, int nameColOneBased) {
        int zeroBasedCol = nameColOneBased - 1;
        var blocks = new List<(string, int, int)>();
        for (int i = 0; i < sheet.NumMergedRegions; i++) {
            CellRangeAddress region = sheet.GetMergedRegion(i);
            if (region.FirstColumn != zeroBasedCol || region.FirstRow < 4) continue;
            string name = sheet.GetRow(region.FirstRow)?.GetCell(zeroBasedCol)?.ToString()?.Trim() ?? "";
            blocks.Add((name, region.FirstRow, region.LastRow));
        }
        return blocks.OrderBy(b => b.Item2).ToList();
    }

    internal static School ResolveSchool(DataContext db, string schoolName) {
        if (string.IsNullOrEmpty(schoolName) || schoolName == "總計") return null;
        School school = db.School.FirstOrDefault(e => e.Name == schoolName);
        if (school == null && CourseMapping.PsjSchoolNameAliases.TryGetValue(schoolName, out string alias))
            school = db.School.FirstOrDefault(e => e.Name == alias);
        return school;
    }

    public ImportScanResult Scan(DataContext db, Stream fileStream, int? overrideYear = null, int? overrideWeek = null) {
        var result = new ImportScanResult();
        if (!overrideYear.HasValue || overrideYear.Value <= 0 || !overrideWeek.HasValue || overrideWeek.Value <= 0) {
            result.Errors.Add("PSJ 匯入必須指定學年度與週次");
            return result;
        }
        int yearInt = overrideYear.Value, weekInt = overrideWeek.Value;

        var wb = new XSSFWorkbook(fileStream);
        ISheet north = wb.GetSheet("北區");
        ISheet south = wb.GetSheet("南區");
        if (north == null || south == null) {
            result.Errors.Add("找不到「北區」或「南區」頁籤");
            return result;
        }

        void ScanSide(ISheet sheet, int nameCol) {
            foreach (var block in FindSchoolBlocks(sheet, nameCol)) {
                School school = ResolveSchool(db, block.SchoolName);
                if (school == null) continue;
                bool exists = db.StudentPopulation.Any(e =>
                    e.School.Id == school.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PSJ);
                result.Items.Add(new ImportScanItem {
                    SchoolName = block.SchoolName,
                    SchoolId = school.Id,
                    Year = yearInt,
                    Week = weekInt,
                    Exists = exists,
                });
            }
        }
        ScanSide(north, ColumnLayout.North.NameCol);
        ScanSide(south, ColumnLayout.SouthLeft.NameCol);
        ScanSide(south, ColumnLayout.SouthLeft.NameCol + 20);
        return result;
    }

    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger, int? overrideYear = null, int? overrideWeek = null) {
        var result = new ImportResult { Type = "PSJ" };
        if (!overrideYear.HasValue || overrideYear.Value <= 0 || !overrideWeek.HasValue || overrideWeek.Value <= 0) {
            result.Errors.Add("PSJ 匯入必須指定學年度與週次");
            return result;
        }
        int yearInt = overrideYear.Value, weekInt = overrideWeek.Value;

        SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
        if (schoolYear == null) {
            result.Errors.Add($"找不到學年週次: {yearInt}第{weekInt}週");
            return result;
        }

        var wb = new XSSFWorkbook(fileStream);
        ISheet north = wb.GetSheet("北區");
        ISheet south = wb.GetSheet("南區");
        if (north == null || south == null) {
            result.Errors.Add("找不到「北區」或「南區」頁籤");
            return result;
        }

        var classCountCache = new Dictionary<int, int>();
        ProcessSide(db, north, ColumnLayout.North, yearInt, weekInt, schoolYear, result, logger, classCountCache);
        ProcessSide(db, south, ColumnLayout.SouthLeft, yearInt, weekInt, schoolYear, result, logger, classCountCache);
        ProcessSide(db, south, ColumnLayout.SouthLeft.Shift(20), yearInt, weekInt, schoolYear, result, logger, classCountCache);

        return result;
    }

    private static void ProcessSide(DataContext db, ISheet sheet, ColumnLayout layout, int yearInt, int weekInt,
        SchoolYear schoolYear, ImportResult result, ILogger logger, Dictionary<int, int> classCountCache) {

        foreach (var block in FindSchoolBlocks(sheet, layout.NameCol)) {
            School school = ResolveSchool(db, block.SchoolName);
            if (school == null) continue;

            StudentPopulation pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                StudentPopulationType.PSJ, $"{yearInt}第{weekInt}週百倍速人數表", true);
            result.PopulationIds.Add(pop.Id);
            result.SchoolCount++;

            for (int rNo = block.FirstRow; rNo <= block.LastRow; rNo++) {
                IRow row = sheet.GetRow(rNo);
                if (row == null) continue;

                string grade = row.GetCell(layout.GradeCol - 1)?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(grade) || grade == "小計") continue;

                int mathGradeIdx = Array.IndexOf(CourseMapping.GradeOrder, grade);
                int ckcGradeIdx = Array.IndexOf(CourseMapping.PsjGradeOrder, grade);

                if (ckcGradeIdx >= 0) {
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_E"][ckcGradeIdx], ClassType.Group, row, layout.CkcEnglishGroupCol, pop.Id, result, logger, classCountCache);
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_E"][ckcGradeIdx], ClassType.Personal, row, layout.CkcEnglishPersonalCol, pop.Id, result, logger, classCountCache);
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_C"][ckcGradeIdx], ClassType.Group, row, layout.CkcChineseGroupCol, pop.Id, result, logger, classCountCache);
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_C"][ckcGradeIdx], ClassType.Personal, row, layout.CkcChinesePersonalCol, pop.Id, result, logger, classCountCache);
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_M"][ckcGradeIdx], ClassType.Group, row, layout.CkcMathGroupCol, pop.Id, result, logger, classCountCache);
                    WriteIfPositive(db, school.Id, CourseMapping.CkcCourseIds["CKC_M"][ckcGradeIdx], ClassType.Personal, row, layout.CkcMathPersonalCol, pop.Id, result, logger, classCountCache);
                }

                if (mathGradeIdx >= 0) {
                    WriteIfPositive(db, school.Id, CourseMapping.PsjCourseIds["MP"][mathGradeIdx], ClassType.Personal, row, layout.MathPersonalCol, pop.Id, result, logger, classCountCache);
                    foreach (int col in layout.MathGroupCols)
                        WriteIfPositive(db, school.Id, CourseMapping.PsjCourseIds["MS"][mathGradeIdx], ClassType.SubGroup, row, col, pop.Id, result, logger, classCountCache);

                    WriteIfPositive(db, school.Id, CourseMapping.PsjCourseIds["SP"][mathGradeIdx], ClassType.Personal, row, layout.SciencePersonalCol, pop.Id, result, logger, classCountCache);
                    WriteIfPositive(db, school.Id, CourseMapping.PsjCourseIds["SS"][mathGradeIdx], ClassType.SubGroup, row, layout.ScienceGroupCol, pop.Id, result, logger, classCountCache);
                }
            }

            try {
                db.SaveChanges();
            }
            catch (Exception ex) {
                result.Errors.Add($"存檔失敗（{block.SchoolName}）: {ex.Message}");
                logger?.LogError(ex, "PSJ 匯入存檔失敗: {school}", block.SchoolName);
            }
        }
    }

    private static void WriteIfPositive(DataContext db, int schoolId, int courseId, ClassType cType, IRow row, int oneBasedCol, long populationId, ImportResult result, ILogger logger, Dictionary<int, int> classCountCache) {
        int count = CourseMapping.ReadCellNumber(row, oneBasedCol - 1);
        if (count <= 0) return;
        Course course = db.Course.Include("Department").FirstOrDefault(e => e.Id == courseId);
        if (course == null) return;
        PopulationWriteHelper.AddClassAndItem(db, schoolId, course, cType, populationId, count, result, logger, classCountCache: classCountCache);
    }
}
