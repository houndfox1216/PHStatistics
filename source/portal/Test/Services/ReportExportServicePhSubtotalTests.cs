using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PHStatistics.Content;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

// PH 分區匯出的區內小計／跨區合計列，比照 115年第4週參考檔案（南區/中北區頁籤）補齊：
// 各分區頁籤最後一列標「小計」（原本誤標「總計」），最後一個分區頁籤再多加「合計」（南區+中北區加總）
// 及「去年同期／總計／分析」列與 114年/115年 比較區塊（算法取自客戶原檔，去年同期讀系統內114年同週資料）。
[TestFixture]
public class ReportExportServicePhSubtotalTests {

    private static readonly CourseDepartment Dept = new CourseDepartment { Id = 1, Name = "英文國小班" };

    private static readonly List<Course> Courses = new() {
        new Course { Id = 1, Name = "P1-初階", Department = Dept },
        new Course { Id = 2, Name = "P2-先階", Department = Dept },
    };

    private static void InvokeBuildSheetPH(NPOI.SS.UserModel.ISheet sheet,
        List<StudentPopulation> populations, List<Course> courses,
        int year, int week, string title,
        List<StudentPopulation> allPopulationsForGrandTotal,
        ReportExportService.PhSummaryData summaryData = null) {
        var method = typeof(ReportExportService).GetMethod("BuildSheetPH",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, new object[] { sheet, populations, courses, year, week, title, allPopulationsForGrandTotal, summaryData });
    }

    private static StudentPopulation MakePopulation(string schoolName, int courseId, int number) {
        return new StudentPopulation {
            School = new School { Name = schoolName },
            Items = new List<StudentPopulationItem> {
                new StudentPopulationItem {
                    Number = number,
                    Class = new Class { CourseId = courseId, Type = ClassType.SubGroup }
                }
            }
        };
    }

    private static NPOI.SS.UserModel.ISheet NewSheet() {
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook();
        return wb.CreateSheet("Sheet1");
    }

    [Test]
    public void RegionSheet_LastRowIsLabeledSubtotalNotTotal() {
        var populations = new List<StudentPopulation> {
            MakePopulation("復興", 1, 3),
            MakePopulation("陽明", 1, 5),
        };

        var sheet = NewSheet();
        InvokeBuildSheetPH(sheet, populations, Courses, 115, 4, "title", null);

        var lastRow = sheet.GetRow(sheet.LastRowNum);
        Assert.That(lastRow.GetCell(0).StringCellValue, Is.EqualTo("小計"));
        Assert.That(lastRow.GetCell(2).NumericCellValue, Is.EqualTo(8));
    }

    [Test]
    public void LastRegionSheet_WithGrandTotalPopulations_AddsCombinedRowAfterSubtotal() {
        var southPopulations = new List<StudentPopulation> {
            MakePopulation("復興", 1, 3),
            MakePopulation("陽明", 1, 5),
        };
        var northPopulations = new List<StudentPopulation> {
            MakePopulation("向上", 1, 7),
        };
        var allPopulations = southPopulations.Concat(northPopulations).ToList();

        var sheet = NewSheet();
        InvokeBuildSheetPH(sheet, northPopulations, Courses, 115, 4, "title", allPopulations);

        int subtotalRowIdx = 4 + northPopulations.Count * 2;
        var subtotalRow = sheet.GetRow(subtotalRowIdx);
        Assert.That(subtotalRow.GetCell(0).StringCellValue, Is.EqualTo("小計"));
        Assert.That(subtotalRow.GetCell(2).NumericCellValue, Is.EqualTo(7));

        var combinedRow = sheet.GetRow(subtotalRowIdx + 1);
        Assert.That(combinedRow.GetCell(0).StringCellValue, Is.EqualTo("合計"));
        Assert.That(combinedRow.GetCell(2).NumericCellValue, Is.EqualTo(15)); // 3+5+7
    }

    [Test]
    public void LastRegionSheet_AddsLastYearTotalAndAnalysisRowsFromSummaryData() {
        var populations = new List<StudentPopulation> { MakePopulation("向上", 1, 7) };
        // 國小合計(9)：本週467、114年同週551 → 分析 = (467-551)/551
        var courses = new List<Course> {
            new Course { Id = 1, Name = "P1-初階", Department = Dept },
            new Course { Id = 9, Name = "英文國小人數合計", Department = Dept },
        };
        var data = new ReportExportService.PhSummaryData();
        data.PhThis[9] = 467; data.PhLast[9] = 551;

        var sheet = NewSheet();
        InvokeBuildSheetPH(sheet, populations, courses, 115, 4, "title", populations, data);

        int subtotalRowIdx = 4 + populations.Count * 2;
        var lastYearRow = sheet.GetRow(subtotalRowIdx + 2);
        var totalRow = sheet.GetRow(subtotalRowIdx + 3);
        var analysisRow = sheet.GetRow(subtotalRowIdx + 4);

        Assert.That(lastYearRow.GetCell(0).StringCellValue, Is.EqualTo("去年同期"));
        Assert.That(totalRow.GetCell(0).StringCellValue, Is.EqualTo("總計"));
        Assert.That(analysisRow.GetCell(0).StringCellValue, Is.EqualTo("分析"));

        // 區塊橫跨國小班系各欄（col 2~3），數值寫在區塊第一欄
        Assert.That(lastYearRow.GetCell(2).NumericCellValue, Is.EqualTo(551));
        Assert.That(totalRow.GetCell(2).NumericCellValue, Is.EqualTo(467));
        Assert.That(analysisRow.GetCell(2).NumericCellValue, Is.EqualTo((467.0 - 551) / 551).Within(1e-9));
    }

    [Test]
    public void LastRegionSheet_LastYearZero_LeavesAnalysisBlank() {
        var populations = new List<StudentPopulation> { MakePopulation("向上", 1, 7) };
        var courses = new List<Course> { new Course { Id = 32, Name = "英文合作開班人數合計", Department = Dept } };
        var data = new ReportExportService.PhSummaryData();
        data.PhThis[32] = 92; data.PhLast[32] = 0;

        var sheet = NewSheet();
        InvokeBuildSheetPH(sheet, populations, courses, 115, 4, "title", populations, data);

        int subtotalRowIdx = 4 + populations.Count * 2;
        Assert.That(sheet.GetRow(subtotalRowIdx + 3).GetCell(2).NumericCellValue, Is.EqualTo(92));
        Assert.That(sheet.GetRow(subtotalRowIdx + 4).GetCell(2), Is.Null);   // 去年同期為0不顯示 #DIV/0!
    }

    [Test]
    public void LastRegionSheet_EnglishTotalExcludesEliteAndYearBlocksSumSixCells() {
        var populations = new List<StudentPopulation> { MakePopulation("向上", 1, 7) };
        var courses = new List<Course> { new Course { Id = 9, Name = "英文國小人數合計", Department = Dept } };
        var data = new ReportExportService.PhSummaryData();
        // 本週：國小100、國中200、高中合計(含Elite)300、其中Elite 30、個別指導10、合作開班5、國語文50
        data.PhThis[9] = 100; data.PhThis[16] = 200; data.PhThis[21] = 300; data.PhThis[17] = 30;
        data.PhThis[27] = 10; data.PhThis[32] = 5; data.PhThis[60] = 50;
        data.GeptThis = 7; data.PsjThis = 3; data.PsThis = 4; data.AsThis = 2; data.GeptOtherThis = 1;

        var sheet = NewSheet();
        InvokeBuildSheetPH(sheet, populations, courses, 115, 4, "title", populations, data);

        // 英文總人數 = 100+200+(300-30)+10+5 = 585；英+國 = 585+50 = 635；115年總計 = 635+7+3+4+2+0+1 = 652
        int engChi = 585 + 50;
        int yearRowStart = 4 + populations.Count * 2 + 5 + 1;   // 小計、合計、去年同期/總計/分析 後空一列
        var year115Values = sheet.GetRow(yearRowStart + 3);        // 114年 header+value 兩列後，115年 header、value
        Assert.That(year115Values.GetCell(5).NumericCellValue, Is.EqualTo(engChi));
        Assert.That(year115Values.GetCell(5 + 6 * 5).NumericCellValue, Is.EqualTo(engChi + 7 + 3 + 4 + 2 + 0 + 1));
    }

    [Test]
    public void RegionSheet_WithoutGrandTotalPopulations_DoesNotAddCombinedBlock() {
        var populations = new List<StudentPopulation> { MakePopulation("復興", 1, 3) };

        var sheet = NewSheet();
        InvokeBuildSheetPH(sheet, populations, Courses, 115, 4, "title", null);

        int subtotalRowIdx = 4 + populations.Count * 2;
        Assert.That(sheet.LastRowNum, Is.EqualTo(subtotalRowIdx));
    }
}
