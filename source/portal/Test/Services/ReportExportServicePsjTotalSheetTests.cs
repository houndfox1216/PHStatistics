using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PHStatistics.Content;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

// PSJ（百倍速）匯出目前只有「總表」+南區+中北區三個分頁，115年第4週參考檔案多一個獨立「總計」分頁
// （依區列出南區/北區合計）。這裡用我們系統自己的彙整方式（依區加總全部分校），不逐儲存格複製參考檔案
// 裡那種跨分頁公式結構。
[TestFixture]
public class ReportExportServicePsjTotalSheetTests {

    private static readonly CourseDepartment Dept = new CourseDepartment { Id = 1, Name = "數學班" };

    private static readonly List<Course> Courses = new() {
        new Course { Id = 20, Name = "一年級", Department = Dept },
    };

    private static void InvokeBuildSheetPSJTotal(NPOI.SS.UserModel.ISheet sheet,
        List<StudentPopulation> populations, List<Course> courses, int year, int week) {
        var method = typeof(ReportExportService).GetMethod("BuildSheetPSJTotal",
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

    private static string CellText(NPOI.SS.UserModel.IRow row) => row?.GetCell(0)?.StringCellValue ?? "";

    [Test]
    public void ProducesOneRowPerRegionPlusGrandTotalRow() {
        var populations = new List<StudentPopulation> {
            MakePopulation("東安", "南區", 1, 20, 3),
            MakePopulation("瑞祥", "南區", 1, 20, 5),
            MakePopulation("南京", "中北區", 2, 20, 7),
        };

        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook();
        var sheet = wb.CreateSheet("總計");
        InvokeBuildSheetPSJTotal(sheet, populations, Courses, 115, 4);

        var southRow = sheet.GetRow(3);
        var northRow = sheet.GetRow(4);
        var grandRow = sheet.GetRow(5);

        Assert.That(CellText(southRow), Is.EqualTo("南區合計"));
        Assert.That(southRow.GetCell(1).NumericCellValue, Is.EqualTo(8)); // 3+5

        Assert.That(CellText(northRow), Is.EqualTo("中北區合計"));
        Assert.That(northRow.GetCell(1).NumericCellValue, Is.EqualTo(7));

        Assert.That(CellText(grandRow), Is.EqualTo("全國合計"));
        Assert.That(grandRow.GetCell(1).NumericCellValue, Is.EqualTo(15));
    }
}
