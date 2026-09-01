using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PHStatistics.Content;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

// PH「班級明細」(BuildSheetPHDetail) 補上跟「總表」(BuildSheetPH) 一樣的分區小計/跨區合計列
// （使用者反饋：明細沒有跟著總表一起補）。非IsSum的班級展開欄位（第1班/第2班...）不同分區代表不同
// 實體班級，跨區加總沒有意義，所以「合計」列只加總IsSum欄位（各班系合計/統計欄），跟總表的語意一致。
[TestFixture]
public class ReportExportServicePhDetailSubtotalTests {

    private static readonly CourseDepartment Dept = new CourseDepartment { Id = 1, Name = "英文國小班" };
    private static readonly Course ClassCourse = new Course { Id = 1, Name = "P1-初階", Department = Dept, IsSum = false };
    private static readonly Course SumCourse = new Course { Id = 2, Name = "英文國小人數合計", Department = Dept, IsSum = true };

    private static readonly List<(CourseDepartment dept, List<Course> list)> DeptGroups = new() {
        (Dept, new List<Course> { ClassCourse, SumCourse }),
    };

    private static readonly Dictionary<int, int> MaxSlots = new() { [1] = 1 };
    private const int FixedCols = 2;
    private const int TotalCols = FixedCols + 1 /* ClassCourse 1 slot */ + 1 /* SumCourse */;

    private static void InvokeBuildSheetPHDetail(NPOI.SS.UserModel.ISheet sheet,
        List<StudentPopulation> populations, List<StudentPopulation> allPopulationsForGrandTotal, string title) {
        var method = typeof(ReportExportService).GetMethod("BuildSheetPHDetail",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, new object[] {
            sheet, populations, DeptGroups, MaxSlots, FixedCols, TotalCols, title, allPopulationsForGrandTotal
        });
    }

    private static StudentPopulation MakePopulation(string schoolName, int classCourseNumber, int sumCourseNumber) {
        return new StudentPopulation {
            School = new School { Name = schoolName },
            Items = new List<StudentPopulationItem> {
                new StudentPopulationItem {
                    Number = classCourseNumber,
                    Class = new Class { CourseId = ClassCourse.Id, Type = ClassType.SubGroup }
                },
                new StudentPopulationItem {
                    Number = sumCourseNumber,
                    Class = new Class { CourseId = SumCourse.Id, Type = ClassType.SubGroup }
                },
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
            MakePopulation("復興", 3, 10),
            MakePopulation("陽明", 5, 20),
        };

        var sheet = NewSheet();
        InvokeBuildSheetPHDetail(sheet, populations, null, "title");

        var lastRow = sheet.GetRow(sheet.LastRowNum);
        Assert.That(lastRow.GetCell(0).StringCellValue, Is.EqualTo("小計"));
        Assert.That(lastRow.GetCell(3).NumericCellValue, Is.EqualTo(30)); // IsSum 欄：10+20
    }

    [Test]
    public void LastRegionSheet_WithGrandTotalPopulations_AddsCombinedRowSummingOnlyIsSumColumns() {
        var southPopulations = new List<StudentPopulation> {
            MakePopulation("復興", 3, 10),
            MakePopulation("陽明", 5, 20),
        };
        var northPopulations = new List<StudentPopulation> { MakePopulation("向上", 7, 40) };
        var allPopulations = southPopulations.Concat(northPopulations).ToList();

        var sheet = NewSheet();
        InvokeBuildSheetPHDetail(sheet, northPopulations, allPopulations, "title");

        int subtotalRowIdx = 4 + northPopulations.Count * 2;
        var subtotalRow = sheet.GetRow(subtotalRowIdx);
        Assert.That(subtotalRow.GetCell(0).StringCellValue, Is.EqualTo("小計"));

        var combinedRow = sheet.GetRow(subtotalRowIdx + 1);
        Assert.That(combinedRow.GetCell(0).StringCellValue, Is.EqualTo("合計"));
        // 非IsSum欄（第1班展開欄，col=2）不同分區代表不同實體班級，不跨區加總
        Assert.That(combinedRow.GetCell(2), Is.Null);
        // IsSum欄（col=3）可以跨區加總：10+20+40
        Assert.That(combinedRow.GetCell(3).NumericCellValue, Is.EqualTo(70));

        var lastYearRow = sheet.GetRow(subtotalRowIdx + 2);
        var totalRow = sheet.GetRow(subtotalRowIdx + 3);
        var analysisRow = sheet.GetRow(subtotalRowIdx + 4);
        Assert.That(lastYearRow.GetCell(0).StringCellValue, Is.EqualTo("去年同期"));
        Assert.That(totalRow.GetCell(0).StringCellValue, Is.EqualTo("總計"));
        Assert.That(analysisRow.GetCell(0).StringCellValue, Is.EqualTo("分析"));
    }
}
