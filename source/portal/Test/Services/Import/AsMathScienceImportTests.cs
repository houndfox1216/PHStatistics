using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NPOI.XSSF.UserModel;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
[Explicit("Writes real rows to the dev DB; run manually to verify AS math/science column reading (see plan Task 3)")]
public class AsMathScienceImportTests {
    private const int Year = 114;
    private const int Week = 52; // reuses the real SchoolYear/School fixture already in the dev DB; test cleans up fully in [TearDown]

    private static MemoryStream BuildWorkbook(int week, string grade, int asVal, int epVal, int egVal, int mpVal, int mgVal, int spVal, int sgVal) {
        var wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet($"{week}東湖");
        sheet.CreateRow(0).CreateCell(0).SetCellValue($"東湖 教室{Year}學年度7-6月課輔班人數統計表(請於每週六下班回傳)");
        var row = sheet.CreateRow(4);
        row.CreateCell(0).SetCellValue(week);
        row.CreateCell(2).SetCellValue(grade);
        row.CreateCell(3).SetCellValue(asVal);
        row.CreateCell(4).SetCellValue(epVal);
        row.CreateCell(5).SetCellValue(egVal);
        row.CreateCell(6).SetCellValue(mpVal);
        row.CreateCell(7).SetCellValue(mgVal);
        row.CreateCell(8).SetCellValue(spVal);
        row.CreateCell(9).SetCellValue(sgVal);
        var ms = new MemoryStream();
        wb.Write(ms, leaveOpen: true); // NPOI 2.7.1 IWorkbook.Write closes the stream by default; leaveOpen:true keeps it open so we can rewind and reuse it below
        ms.Position = 0;
        return ms;
    }

    private static int NumberFor(DataContext db, long populationId, int courseId, ClassType classType) {
        return db.StudentPopulationItem
            .Include(i => i.Class)
            .Where(i => i.StudentPopulationId == populationId && i.Class.CourseId == courseId && i.Class.Type == classType)
            .Sum(i => (int?)i.Number) ?? 0;
    }

    [TearDown]
    public void CleanUp() {
        using var db = new DataContext();
        var pop = db.StudentPopulation.FirstOrDefault(p => p.School.Name == "東湖" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.AfterSchool);
        if (pop == null) return;
        var items = db.StudentPopulationItem.Where(i => i.StudentPopulationId == pop.Id).ToList();
        var classIds = items.Select(i => i.ClassId).Distinct().ToList();
        db.StudentPopulationItem.RemoveRange(items);
        db.SaveChanges();
        db.Class.RemoveRange(db.Class.Where(c => classIds.Contains(c.Id)));
        db.SaveChanges();
        db.StudentPopulation.Remove(pop);
        db.SaveChanges();
    }

    [Test]
    public void Import_ReadsMathAndScienceColumns_ForOneGradeRow() {
        using var db = new DataContext();
        using var ms = BuildWorkbook(Week, grade: "一年級", asVal: 0, epVal: 0, egVal: 0, mpVal: 5, mgVal: 3, spVal: 2, sgVal: 1);

        var importer = new ASPopulationImporter();
        var result = importer.Import(db, ms, NullLogger.Instance);

        Assert.That(result.Errors, Is.Empty, () => string.Join("\n", result.Errors));

        var pop = db.StudentPopulation.First(p => p.School.Name == "東湖" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.AfterSchool);

        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["MP"][0], ClassType.Personal), Is.EqualTo(5), "數學班一對一(一年級)");
        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["MG"][0], ClassType.General), Is.EqualTo(3), "數學班團體(一年級)");
        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["SP"][0], ClassType.Personal), Is.EqualTo(2), "理化班一對一(一年級)");
        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["SG"][0], ClassType.General), Is.EqualTo(1), "理化班團體(一年級)");
    }

    [Test]
    [Explicit("Requires the real reference file at C:\\Leo\\其他\\Kuri\\人數表匯入A\\50\\; documents that this file has zero math/science data, not a regression guard")]
    public void Import_RealFile_MathAndScienceColumnsAreAllZero_ThisSpecificFileHasNoNonZeroData() {
        const string realFilePath = @"C:\Leo\其他\Kuri\人數表匯入A\50\百瀚全區課輔人數總表(20260613).xlsx";
        using var db = new DataContext();
        using var fs = new FileStream(realFilePath, FileMode.Open, FileAccess.Read);
        var importer = new ASPopulationImporter();
        var result = importer.Import(db, fs, NullLogger.Instance);

        Assert.That(result.Errors, Is.Empty, () => string.Join("\n", result.Errors));

        var pop = db.StudentPopulation.First(p => p.School.Name == "東湖" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.AfterSchool);
        // 東湖 一年級 安親 = 2 (existing regression check — must still work after extending colDefs)
        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["AS"][0], ClassType.General), Is.EqualTo(2), "安親一年級 regression check");
        // This real file has zero non-null values in col6-9 for all 3 schools/weeks (verified during spec investigation) —
        // this assertion documents that fact, it is NOT proof the new column-reading code is correct (see the synthetic-workbook test above for that).
        Assert.That(NumberFor(db, pop.Id, CourseMapping.AsCourseIds["MP"][0], ClassType.Personal), Is.EqualTo(0));
    }
}
