using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NPOI.XSSF.UserModel;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services;
using PHStatistics.Portal.Services.Import;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Test.Services;

[TestFixture]
[Explicit("Writes real rows to the dev DB; run manually to verify AS export includes math/science columns (see plan Task 4)")]
public class ReportExportServiceAsMathScienceTests {
    private const int Year = 114;
    private const int Week = 52;
    private long _populationId;

    [SetUp]
    public void SeedMathScienceItem() {
        using var db = new DataContext();
        School school = db.School.First(s => s.Name == "東湖");
        SchoolYear schoolYear = db.SchoolYear.First(s => s.Year == Year && s.Week == Week);
        Course mathCourse = db.Course.Include("Department").First(c => c.Id == CourseMapping.AsCourseIds["MP"][0]); // 378, 一年級, dept 40

        var pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, Year, Week, schoolYear,
            StudentPopulationType.AfterSchool, "test-export-math-science", true);
        _populationId = pop.Id;
        PopulationWriteHelper.AddClassAndItem(db, school.Id, mathCourse, ClassType.Personal, pop.Id, 7, new ImportResult(), NullLogger.Instance);
    }

    [TearDown]
    public void CleanUp() {
        using var db = new DataContext();
        var items = db.StudentPopulationItem.Where(i => i.StudentPopulationId == _populationId).ToList();
        var classIds = items.Select(i => i.ClassId).Distinct().ToList();
        db.StudentPopulationItem.RemoveRange(items);
        db.SaveChanges();
        db.Class.RemoveRange(db.Class.Where(c => classIds.Contains(c.Id)));
        db.SaveChanges();
        var pop = db.StudentPopulation.Find(_populationId);
        if (pop != null) { db.StudentPopulation.Remove(pop); db.SaveChanges(); }
    }

    // 2026-08-05：AS 匯出改為比照「百瀚全區課輔人數總表」原始格式（每校一個 Sheet、
    // 數學班一對一/團體班為固定欄位 col 6/7），不再是舊版 code 表頭（MP/MG...）格式。
    [Test]
    public void Export_AfterSchool_IncludesMpColumnWithSeededValue() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.AfterSchool, Year, Week);

        Assert.That(bytes, Is.Not.Empty);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new XSSFWorkbook(ms);
        var sheet = wb.GetSheet($"{Year}東湖");
        Assert.That(sheet, Is.Not.Null, "應該有一個以校名命名（前綴年度）的 Sheet");

        var deptHeaderRow = sheet.GetRow(1);
        Assert.That(deptHeaderRow.GetCell(6)?.ToString(), Is.EqualTo("數學班"));
        Assert.That(sheet.GetRow(2).GetCell(6)?.ToString(), Is.EqualTo("一對一"));

        var firstGradeRow = sheet.GetRow(4);
        Assert.That(firstGradeRow.GetCell(2)?.ToString(), Is.EqualTo("一年級"));
        Assert.That((int)firstGradeRow.GetCell(6).NumericCellValue, Is.EqualTo(7));
    }
}
