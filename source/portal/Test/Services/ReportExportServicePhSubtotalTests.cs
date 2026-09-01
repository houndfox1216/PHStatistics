using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PHStatistics.Content;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

// PH 分區匯出的區內小計／跨區合計列，比照 115年第4週參考檔案（南區/中北區頁籤）補齊：
// 各分區頁籤最後一列標「小計」（原本誤標「總計」），最後一個分區頁籤再多加「合計」（南區+中北區加總）
// 及「去年同期／總計／分析」列標籤（數值待客戶確認算法後再補，目前刻意留空）。
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
        List<StudentPopulation> allPopulationsForGrandTotal) {
        var method = typeof(ReportExportService).GetMethod("BuildSheetPH",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, new object[] { sheet, populations, courses, year, week, title, allPopulationsForGrandTotal });
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
    public void LastRegionSheet_AddsLastYearAndAnalysisRowLabelsWithoutValues() {
        var populations = new List<StudentPopulation> { MakePopulation("向上", 1, 7) };

        var sheet = NewSheet();
        InvokeBuildSheetPH(sheet, populations, Courses, 115, 4, "title", populations);

        int subtotalRowIdx = 4 + populations.Count * 2;
        var lastYearRow = sheet.GetRow(subtotalRowIdx + 2);
        var totalRow = sheet.GetRow(subtotalRowIdx + 3);
        var analysisRow = sheet.GetRow(subtotalRowIdx + 4);

        Assert.That(lastYearRow.GetCell(0).StringCellValue, Is.EqualTo("去年同期"));
        Assert.That(totalRow.GetCell(0).StringCellValue, Is.EqualTo("總計"));
        Assert.That(analysisRow.GetCell(0).StringCellValue, Is.EqualTo("分析"));

        // 算法/資料來源待確認，目前刻意不填數值
        Assert.That(lastYearRow.GetCell(2), Is.Null);
        Assert.That(totalRow.GetCell(2), Is.Null);
        Assert.That(analysisRow.GetCell(2), Is.Null);
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
