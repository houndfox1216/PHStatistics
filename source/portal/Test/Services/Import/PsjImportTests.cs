using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import;
using PHStatistics.Portal.Services.Import.ImportSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace PHStatistics.Portal.Test.Services.Import;

[TestFixture]
[Explicit("Writes real rows to the dev DB and requires the local PSJ fixture file; run manually to verify the PSJ CKC import rewrite (see plan Task 7)")]
public class PsjImportTests {
    private const string RealFilePath = @"C:\Leo\其他\Kuri\人數表匯入A\50\（百倍速）人數統計表更新版115.6.13).xlsx";
    private const int Year = 114;
    private const int Week = 50;

    private static ImportResult RunImport(DataContext db) {
        var importer = new PSJPopulationImporter();
        using var fs = new System.IO.FileStream(RealFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        return importer.Import(db, fs, NullLogger.Instance, Year, Week);
    }

    private static int NumberFor(DataContext db, long populationId, int courseId, ClassType classType) {
        return db.StudentPopulationItem
            .Include(i => i.Class)
            .Where(i => i.StudentPopulationId == populationId && i.Class.CourseId == courseId && i.Class.Type == classType)
            .Sum(i => (int?)i.Number) ?? 0;
    }

    [Test]
    public void Import_RealFile_ProducesExpectedKnownValues() {
        using var db = new DataContext();
        var result = RunImport(db);

        Assert.That(result.Errors, Is.Empty, () => string.Join("\n", result.Errors));

        // 南京 國一：數學班小組班第一班 = 1 (北區 row10, col10)
        // CourseMapping.GradeOrder index: 0=一年級,1=二年級,...,6=國一 — 國一 is index 6, not 1.
        var nanjing = db.StudentPopulation.First(p => p.School.Name == "南京" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        Assert.That(NumberFor(db, nanjing.Id, CourseMapping.PsjCourseIds["MS"][6], ClassType.SubGroup), Is.EqualTo(1));

        // 內湖 國二：數學班小組班第一班=2、第二班=1（Excel 2+1 拆成 2 筆各自 Class，見北區 row23 col10/col11）
        var neihu = db.StudentPopulation.First(p => p.School.Name == "內湖" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        var neihuGuo2Classes = db.Class.Where(c => c.SchoolId == neihu.SchoolId && c.CourseId == CourseMapping.PsjCourseIds["MS"][7] && c.Type == ClassType.SubGroup
            && db.StudentPopulationItem.Any(i => i.ClassId == c.Id && i.StudentPopulationId == neihu.Id)).ToList();
        Assert.That(neihuGuo2Classes.Count, Is.EqualTo(2), "內湖 國二數學班小組班應拆成 2 筆獨立 Class（第一班=2, 第二班=1）");

        // 內湖 國三：理化班一對一 = 2（北區 row24 col14）
        Assert.That(NumberFor(db, neihu.Id, CourseMapping.PsjCourseIds["SP"][8], ClassType.Personal), Is.EqualTo(2));

        // 莊敬 國二（南區左半 row38）：數學班一對一=1, 理化班一對一=1, 理化班小組班=1
        var zhuangjing = db.StudentPopulation.First(p => p.School.Name == "莊敬" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        Assert.That(NumberFor(db, zhuangjing.Id, CourseMapping.PsjCourseIds["MP"][7], ClassType.Personal), Is.EqualTo(1));
        Assert.That(NumberFor(db, zhuangjing.Id, CourseMapping.PsjCourseIds["SP"][7], ClassType.Personal), Is.EqualTo(1));
        Assert.That(NumberFor(db, zhuangjing.Id, CourseMapping.PsjCourseIds["SS"][7], ClassType.SubGroup), Is.EqualTo(1));

        // 河堤（南區右半，+20 offset）高二：數學班一對一 = 1 (row54 col29)
        // CourseMapping.GradeOrder index: 高二 is index 10.
        var hedi = db.StudentPopulation.First(p => p.School.Name == "河堤" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        Assert.That(NumberFor(db, hedi.Id, CourseMapping.PsjCourseIds["MP"][10], ClassType.Personal), Is.EqualTo(1));

        // 高美館（南區右半，Excel 頁籤寫「高美」，別名解析為資料庫的「高美館」）：國二數學班小組班第一班=3（row38 col30）
        var gaomeiguan = db.StudentPopulation.First(p => p.School.Name == "高美館" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        Assert.That(NumberFor(db, gaomeiguan.Id, CourseMapping.PsjCourseIds["MS"][7], ClassType.SubGroup), Is.EqualTo(3));

        // 中正（南區左半 row70-82，跟右半的「總計」共用同一段列號——驗證兩側各自獨立追蹤，
        // 總計側的大量非零數字不會外洩到中正）：國二理化班小組班 = 2（row77 col16）
        var zhongzheng = db.StudentPopulation.First(p => p.School.Name == "中正" && p.Year == Year && p.Week == Week && p.Type == StudentPopulationType.PSJ);
        Assert.That(NumberFor(db, zhongzheng.Id, CourseMapping.PsjCourseIds["SS"][7], ClassType.SubGroup), Is.EqualTo(2));

        // CKC 英/國/數：這份真實檔案裡三科「加上」「單上」欄位全部是 0/空白，
        // 所以這裡只驗證流程沒有例外、沒有寫入任何 CKC Class（未驗證非零情境——之後若有含 CKC 真實數字的檔案，應補測）。
        var ckcCourseIds = CourseMapping.CkcCourseIds["CKC_E"]
            .Concat(CourseMapping.CkcCourseIds["CKC_C"])
            .Concat(CourseMapping.CkcCourseIds["CKC_M"]);
        int ckcClassCount = db.Class.Count(c => ckcCourseIds.Contains(c.CourseId ?? 0));
        Assert.That(ckcClassCount, Is.EqualTo(0), "本次真實檔案 CKC 欄位全為 0，若此斷言失敗代表檔案已更新為含真實 CKC 資料，請改寫本測試改為驗證實際數字");

        TestContext.WriteLine($"Schools imported: {result.SchoolCount}, items: {result.ItemCount}");
    }
}
