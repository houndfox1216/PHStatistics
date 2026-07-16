using System.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PHStatistics.Portal.Services.Import;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
public class PsjBlockDetectionTests {
    // Local copy of the same real file referenced by the design spec
    // (docs/superpowers/specs/2026-07-16-psj-ckc-import-design.md). Adjust this path if your
    // machine keeps the import fixtures elsewhere.
    private const string RealFilePath = @"C:\Leo\其他\Kuri\人數表匯入A\50\（百倍速）人數統計表更新版115.6.13).xlsx";

    private static XSSFWorkbook OpenWorkbook() {
        using var fs = new System.IO.FileStream(RealFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        return new XSSFWorkbook(fs);
    }

    [Test]
    public void FindSchoolBlocks_North_FindsFourNamedSchoolsAndSkipsBlankAndTotalBlocks() {
        var wb = OpenWorkbook();
        var blocks = PSJPopulationImporter.FindSchoolBlocks(wb.GetSheet("北區"), 1);
        var namedSchools = blocks.Where(b => b.SchoolName != "" && b.SchoolName != "總計").Select(b => b.SchoolName).ToList();

        Assert.That(namedSchools, Is.EqualTo(new[] { "南京", "內湖", "敦南", "東湖" }));
        // rows are 0-based NPOI indices; 南京's merged region is Excel A5:A16 → NPOI rows 4-15
        var nanjing = blocks.First(b => b.SchoolName == "南京");
        Assert.That(nanjing.FirstRow, Is.EqualTo(4));
        Assert.That(nanjing.LastRow, Is.EqualTo(15));
        Assert.That(blocks.Any(b => b.SchoolName == ""), Is.True, "北區 rows 41-52 (0-based 40-51) is a genuinely blank merged block and must still be found (and later skipped by ResolveSchool)");
    }

    [Test]
    public void FindSchoolBlocks_SouthLeftAndRight_FindAllSevenNamedSchools() {
        var wb = OpenWorkbook();
        ISheet south = wb.GetSheet("南區");
        var left = PSJPopulationImporter.FindSchoolBlocks(south, 1).Where(b => b.SchoolName != "" && b.SchoolName != "總計").Select(b => b.SchoolName).ToList();
        var right = PSJPopulationImporter.FindSchoolBlocks(south, 21).Where(b => b.SchoolName != "" && b.SchoolName != "總計").Select(b => b.SchoolName).ToList();

        Assert.That(left, Is.EqualTo(new[] { "東安", "莊敬", "瑞祥", "中正" }));
        Assert.That(right, Is.EqualTo(new[] { "岡山", "高美", "河堤" }));
    }

    [Test]
    public void ColumnLayout_SouthLeftShiftedBy20_MatchesSouthRightRawColumns() {
        var shifted = PSJPopulationImporter.ColumnLayout.SouthLeft.Shift(20);
        Assert.That(shifted.NameCol, Is.EqualTo(21));
        Assert.That(shifted.MathGroupCols, Is.EqualTo(new[] { 30, 31, 32, 33, 34 }));
        Assert.That(shifted.ScienceGroupCol, Is.EqualTo(36));
    }
}
