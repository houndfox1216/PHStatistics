using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PHStatistics.Content;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

// GEPT（英檢）匯出比照115年第4週參考檔案的「英檢」頁籤，補上南區小計／中北區小計／全國總計三列，
// 這三列在參考檔案裡都是純SUM公式（不牽涉人工資料），可以直接自動算。
[TestFixture]
public class ReportExportServiceGeptSubtotalTests {

    private static readonly CourseDepartment Dept = new CourseDepartment { Id = 1, Name = "英檢班" };

    private static readonly List<Course> Courses = new() {
        new Course { Id = 10, Name = "初級", Department = Dept },
        new Course { Id = 11, Name = "中級", Department = Dept },
    };

    private static void InvokeBuildSheetGEPT(NPOI.SS.UserModel.ISheet sheet,
        List<StudentPopulation> populations, List<Course> courses, int year, int week) {
        var method = typeof(ReportExportService).GetMethod("BuildSheetGEPT",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, new object[] { sheet, populations, courses, year, week });
    }

    private static StudentPopulation MakePopulation(string schoolName, string regionName, int regionOrdinal, int courseId, int number) {
        return new StudentPopulation {
            School = new School { Name = schoolName, Region = new Region { Name = regionName, Ordinal = regionOrdinal } },
            Items = new List<StudentPopulationItem> {
                new StudentPopulationItem {
                    Number = number,
                    Class = new Class { CourseId = courseId, Type = ClassType.General }
                }
            }
        };
    }

    private static NPOI.SS.UserModel.ISheet NewSheet() {
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook();
        return wb.CreateSheet("英檢");
    }

    private static string CellText(NPOI.SS.UserModel.IRow row) =>
        row?.GetCell(0)?.StringCellValue ?? "";

    [Test]
    public void AddsPerRegionSubtotalRowsAndGrandTotalRow() {
        var populations = new List<StudentPopulation> {
            MakePopulation("復興", "南區", 1, 10, 3),
            MakePopulation("陽明", "南區", 1, 10, 5),
            MakePopulation("向上", "中北區", 2, 10, 7),
        };

        var sheet = NewSheet();
        InvokeBuildSheetGEPT(sheet, populations, Courses, 115, 4);

        // Row 4 起是南區資料列(2校)+南區小計，接著中北區資料列(1校)+中北區小計，最後全國總計
        var southSubtotal = sheet.GetRow(6);
        var northSubtotal = sheet.GetRow(8);
        var grandTotal = sheet.GetRow(9);

        Assert.That(CellText(southSubtotal), Is.EqualTo("南區小計"));
        Assert.That(southSubtotal.GetCell(2).NumericCellValue, Is.EqualTo(8)); // 3+5

        Assert.That(CellText(northSubtotal), Is.EqualTo("中北區小計"));
        Assert.That(northSubtotal.GetCell(2).NumericCellValue, Is.EqualTo(7));

        Assert.That(CellText(grandTotal), Is.EqualTo("全國總計"));
        Assert.That(grandTotal.GetCell(2).NumericCellValue, Is.EqualTo(15));
    }
}
