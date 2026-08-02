using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PHStatistics.Content;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

[TestFixture]
public class ReportExportServiceStructureTests {

    private static List<(string RegionName, List<StudentPopulation> Populations)> InvokeGroupByRegion(
        List<StudentPopulation> populations) {
        var method = typeof(ReportExportService).GetMethod("GroupByRegion",
            BindingFlags.NonPublic | BindingFlags.Static);
        return (List<(string, List<StudentPopulation>)>)method.Invoke(null, new object[] { populations });
    }

    private static StudentPopulation MakePopulation(string schoolName, string regionName, int regionOrdinal) {
        var region = regionName == null ? null : new Region { Name = regionName, Ordinal = regionOrdinal };
        return new StudentPopulation {
            School = new School { Name = schoolName, Region = region, RegionId = region == null ? (int?)null : 1 },
            Items = new List<StudentPopulationItem>()
        };
    }

    [Test]
    public void GroupByRegion_GroupsBySchoolRegionName_PreservingRegionOrdinalOrder() {
        var populations = new List<StudentPopulation> {
            MakePopulation("東安", "南區", 1),
            MakePopulation("南京", "中北區", 0),
            MakePopulation("莊敬", "南區", 1),
        };

        var groups = InvokeGroupByRegion(populations);

        Assert.That(groups.Select(g => g.RegionName).ToList(), Is.EqualTo(new[] { "中北區", "南區" }));
        Assert.That(groups.First(g => g.RegionName == "南區").Populations.Select(p => p.School.Name),
            Is.EqualTo(new[] { "東安", "莊敬" }));
    }

    [Test]
    public void GroupByRegion_SchoolWithoutRegion_GoesIntoUnassignedGroupLast() {
        var populations = new List<StudentPopulation> {
            MakePopulation("南京", "中北區", 0),
            MakePopulation("查無分區學校", null, 0),
        };

        var groups = InvokeGroupByRegion(populations);

        Assert.That(groups.Last().RegionName, Is.EqualTo("未分區"));
        Assert.That(groups.Last().Populations.Single().School.Name, Is.EqualTo("查無分區學校"));
    }

    [Explicit("需要本機 dev DB 連線，且假設 115年第1週已有 PH 資料與南區/中北區分區設定")]
    [Test]
    public void Export_PH_ProducesOneSheetPerRegion() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.PH, 115, 1);

        Assert.That(bytes, Is.Not.Empty);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook(ms);

        var sheetNames = Enumerable.Range(0, wb.NumberOfSheets)
            .Select(i => wb.GetSheetAt(i).SheetName)
            .ToList();

        Assert.That(sheetNames, Does.Contain("南區"));
        Assert.That(sheetNames, Does.Contain("中北區"));
        Assert.That(sheetNames, Has.No.Member("Sheet1"));
    }

    [Explicit("需要本機 dev DB 連線，且假設 115年第1週已有 PSJ 資料")]
    [Test]
    public void Export_PSJ_HeaderMatchesCourseOrdinalOrder() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.PSJ, 115, 1);

        Assert.That(bytes, Is.Not.Empty);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook(ms);
        var sheet = wb.GetSheetAt(0);

        // Row 1 應該出現「數學班」部門名稱（來源：CourseDepartment.Name，非寫死字串）
        var headerRow1Text = string.Join("", Enumerable.Range(0, sheet.GetRow(1).LastCellNum)
            .Select(c => sheet.GetRow(1).GetCell(c)?.StringCellValue ?? ""));
        Assert.That(headerRow1Text, Does.Contain("數學班"));

        // Row 2（課程名稱列）應該出現「一年級」（Course.Name，來自資料庫 Course.Id=145 等）
        var headerRow2Text = string.Join("|", Enumerable.Range(0, sheet.GetRow(2).LastCellNum)
            .Select(c => sheet.GetRow(2).GetCell(c)?.StringCellValue ?? ""));
        Assert.That(headerRow2Text, Does.Contain("一年級"));
    }

    [Explicit("需要本機 dev DB 連線")]
    [Test]
    public void Export_PSJ_IncludesTotalSheetWithAllSchools() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.PSJ, 115, 1);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook(ms);

        var sheetNames = Enumerable.Range(0, wb.NumberOfSheets)
            .Select(i => wb.GetSheetAt(i).SheetName).ToList();
        Assert.That(sheetNames, Does.Contain("總表"));

        var totalSheet = wb.GetSheet("總表");
        var southSheet = wb.GetSheet("南區");
        var northSheet = wb.GetSheet("中北區");

        int totalDataRows = totalSheet.LastRowNum - 2; // 扣除標題+表頭2列
        int regionDataRows = (southSheet.LastRowNum - 2) + (northSheet.LastRowNum - 2);
        Assert.That(totalDataRows, Is.EqualTo(regionDataRows));
    }

    [Explicit("需要本機 dev DB 連線")]
    [Test]
    public void Export_PH_CourseNamesAppearInRow2NotRow3() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.PH, 115, 1);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook(ms);
        var sheet = wb.GetSheetAt(0);

        var row2Text = string.Join("|", Enumerable.Range(0, sheet.GetRow(2).LastCellNum)
            .Select(c => sheet.GetRow(2).GetCell(c)?.StringCellValue ?? ""));
        Assert.That(row2Text, Does.Contain("P1-初階"));
    }

    // 2026-08-02：總表（全分校/多分校）改成包含IsSum合計/分析欄位，這些課程在DB裡幾乎全部Published=0，
    // 迴歸測試鎖住「不能被Published過濾掉」這件事。
    [Explicit("需要本機 dev DB 連線")]
    [Test]
    public void Export_PH_IncludesIsSumAggregateColumns() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.PH, 115, 1);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook(ms);
        var sheet = wb.GetSheetAt(0);

        var row2Text = string.Join("|", Enumerable.Range(0, sheet.GetRow(2).LastCellNum)
            .Select(c => sheet.GetRow(2).GetCell(c)?.StringCellValue ?? ""));
        Assert.That(row2Text, Does.Contain("英文國小人數合計"));
        Assert.That(row2Text, Does.Contain("上週英語文總人數"));
        Assert.That(row2Text, Does.Contain("與上週相比"));
        Assert.That(row2Text, Does.Contain("總人數"));
    }
}
