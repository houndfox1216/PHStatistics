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

    [Test]
    public void Export_AfterSchool_IncludesMpColumnWithSeededValue() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.AfterSchool, Year, Week);

        Assert.That(bytes, Is.Not.Empty);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new XSSFWorkbook(ms);
        var sheet = wb.GetSheetAt(0);

        var hdrRow = sheet.GetRow(4);
        var headers = Enumerable.Range(0, hdrRow.LastCellNum).Select(c => hdrRow.GetCell(c)?.ToString() ?? "").ToList();
        int mpCol = headers.IndexOf("MP");
        Assert.That(mpCol, Is.GreaterThan(-1), "MP header column should exist in the exported code-header row");

        bool found = false;
        for (int r = 5; r <= sheet.LastRowNum; r++) {
            var row = sheet.GetRow(r);
            if (row?.GetCell(2)?.ToString() == "東湖" && row.GetCell(3)?.ToString() == "一年級") {
                Assert.That((int)row.GetCell(mpCol).NumericCellValue, Is.EqualTo(7));
                found = true;
                break;
            }
        }
        Assert.That(found, Is.True, "Expected a 東湖/一年級 row in the exported sheet");
    }
}
