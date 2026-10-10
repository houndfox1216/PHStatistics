using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PHStatistics.Content;
using PHStatistics.Portal.Controllers;

namespace PHStatistics.Portal.Test.Controllers;

// 建立新週人數表前的「上週資料缺失」判斷。分校已確認送出的上週人數表即使只有合計列（例如河堤沒有英檢班）
// 也不算缺失，避免每週誤跳警告頁。
[TestFixture]
public class IsLastWeekDataMissingTests {

    private static bool Invoke(StudentPopulation lastWeek) {
        var method = typeof(StudentPopulationController).GetMethod("IsLastWeekDataMissing",
            BindingFlags.NonPublic | BindingFlags.Static);
        return (bool)method.Invoke(null, new object[] { lastWeek });
    }

    private static StudentPopulationItem Item(bool isSum) => new StudentPopulationItem {
        Class = new Class { Course = new Course { IsSum = isSum } }
    };

    private static StudentPopulation Population(StudentPopulationStatus status, params StudentPopulationItem[] items) =>
        new StudentPopulation { Status = status, Items = new List<StudentPopulationItem>(items) };

    [Test]
    public void NullPopulation_IsMissing() {
        Assert.That(Invoke(null), Is.True);
    }

    [Test]
    public void PopulationWithoutItems_IsMissing() {
        Assert.That(Invoke(new StudentPopulation { Status = StudentPopulationStatus.Pending, Items = null }), Is.True);
    }

    [Test]
    public void HasClassItems_IsNotMissing() {
        Assert.That(Invoke(Population(StudentPopulationStatus.Documented, Item(false), Item(true))), Is.False);
    }

    [Test]
    public void OnlySumItemsAndNotSubmitted_IsMissing() {
        Assert.That(Invoke(Population(StudentPopulationStatus.Documented, Item(true), Item(true))), Is.True);
    }

    [Test]
    public void OnlySumItemsButSubmitted_IsNotMissing() {
        // 河堤英檢：沒有班級、只有合計列，但分校每週都有確認送出
        Assert.That(Invoke(Population(StudentPopulationStatus.Pending, Item(true), Item(true))), Is.False);
        Assert.That(Invoke(Population(StudentPopulationStatus.Approved, Item(true))), Is.False);
    }
}
