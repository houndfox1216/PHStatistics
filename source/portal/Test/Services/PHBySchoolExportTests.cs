using System.IO;
using System.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

[TestFixture]
[Explicit("Requires a live connection to the database with real PH data for school 板橋陽明 (Id 6), year 115")]
public class PHBySchoolExportTests {
    private const int SchoolId = 6; // 板橋陽明
    private const int Year = 115;
    private const int Week = 5;

    [Test]
    public void Export_SingleSchool_ProducesOneRowPerWeekWithNoTeamRow() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.PH, Year, Week, new[] { SchoolId });

        Assert.That(bytes, Is.Not.Empty);

        using var ms = new MemoryStream(bytes);
        var wb = new XSSFWorkbook(ms);
        var sheet = wb.GetSheetAt(0);

        var r1 = sheet.GetRow(1);
        Assert.That(r1.GetCell(0).StringCellValue, Is.EqualTo("週次"));
        Assert.That(r1.GetCell(1).StringCellValue, Is.EqualTo("日期"));
        Assert.That(r1.GetCell(2).StringCellValue, Is.EqualTo("開班模式"));

        // Row 4+：每週2列（小/三），無團。至少涵蓋第1~5週 => 至少10列資料。
        int dataRows = 0;
        int lastNonEmptyRow = sheet.LastRowNum;
        for (int r = 4; r <= lastNonEmptyRow; r++) {
            if (sheet.GetRow(r) != null) dataRows++;
        }
        Assert.That(dataRows, Is.GreaterThanOrEqualTo(10), "應至少有5週 x 2列(小/三)資料");

        // 開班模式欄只會出現 小/三，不會出現團
        var modes = Enumerable.Range(4, dataRows)
            .Select(r => sheet.GetRow(r)?.GetCell(2)?.ToString())
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct()
            .ToList();
        Assert.That(modes, Is.EquivalentTo(new[] { "小", "三" }));

        TestContext.WriteLine($"Sheet name: {sheet.SheetName}, data rows: {dataRows}");
    }

    [Test]
    public void ExportPHDetail_SingleSchool_ProducesOneRowPerWeek() {
        var service = new ReportExportService();
        byte[] bytes = service.ExportPHDetail(Year, Week, new[] { SchoolId });

        Assert.That(bytes, Is.Not.Empty);

        using var ms = new MemoryStream(bytes);
        var wb = new XSSFWorkbook(ms);
        var sheet = wb.GetSheetAt(0);

        var r1 = sheet.GetRow(1);
        Assert.That(r1.GetCell(0).StringCellValue, Is.EqualTo("週次"));
        Assert.That(r1.GetCell(1).StringCellValue, Is.EqualTo("日期"));
        Assert.That(r1.GetCell(2).StringCellValue, Is.EqualTo("開班模式"));

        int dataRows = 0;
        for (int r = 4; r <= sheet.LastRowNum; r++) {
            if (sheet.GetRow(r) != null) dataRows++;
        }
        Assert.That(dataRows, Is.GreaterThanOrEqualTo(10));

        var modes = Enumerable.Range(4, dataRows)
            .Select(r => sheet.GetRow(r)?.GetCell(2)?.ToString())
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct()
            .ToList();
        Assert.That(modes, Is.EquivalentTo(new[] { "小班", "三人班" }));
    }
}
