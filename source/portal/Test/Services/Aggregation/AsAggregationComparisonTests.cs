using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Aggregation;
using System.Framework.Data;

namespace PHStatistics.Portal.Test.Services.Aggregation;

[TestFixture]
[Explicit("Requires a live connection to the dev database; run manually to verify the AS AggregationEngine migration before cutover (see plan Task 5)")]
public class AsAggregationComparisonTests {
    // 資料調查（見 docs/superpowers/specs/2026-07-16-psj-as-aggregation-migration-design.md）沒有發現任何
    // AS 課程需要刻意接受行為變更——28 個引擎計算課程的既有真實資料本身就已經符合新公式，理論上應該 0 落差。
    private static readonly HashSet<int> ExpectedChangeCourseIds = new();

    [Test]
    public void Engine_ReproducesStoredNumbers_ForAllRealAsPopulations() {
        using var context = new DataContext();

        var populations = context.StudentPopulation
            .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
            .Where(p => p.Type == StudentPopulationType.AfterSchool && p.DataMode == DataMode.Normal)
            .ToList();

        var engine = new AggregationEngine((year, week, schoolId, type) =>
            context.StudentPopulation
                .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type));

        var unexpectedMismatches = new List<string>();
        var expectedChanges = new List<string>();
        foreach (var population in populations) {
            var sumItems = population.Items.Where(i => i.IsSum).OrderBy(i => i.Class.Course.Ordinal).ToList();
            var oldNumbers = sumItems.ToDictionary(i => i, i => i.Number);

            foreach (var item in sumItems) {
                engine.Calculate(item, population);
            }

            foreach (var item in sumItems) {
                int oldNumber = oldNumbers[item];
                if (item.Number != oldNumber) {
                    string line =
                        $"Population {population.Id} (School {population.SchoolId}, {population.Year}/{population.Week}): " +
                        $"course {item.Class.Course.Id} \"{item.Class.Course.Name}\" stored={oldNumber} engine={item.Number}";
                    if (ExpectedChangeCourseIds.Contains(item.Class.Course.Id)) {
                        expectedChanges.Add(line);
                    }
                    else {
                        unexpectedMismatches.Add(line);
                    }
                }
            }

            foreach (var item in sumItems) {
                item.Number = oldNumbers[item]; // 唯讀比對，還原避免誤動資料
            }
        }

        TestContext.WriteLine($"Checked {populations.Count} AS populations.");
        TestContext.WriteLine($"{expectedChanges.Count} expected changes:");
        foreach (var m in expectedChanges) TestContext.WriteLine(m);
        TestContext.WriteLine($"{unexpectedMismatches.Count} UNEXPECTED mismatches:");
        foreach (var m in unexpectedMismatches) TestContext.WriteLine(m);

        Assert.That(unexpectedMismatches, Is.Empty, () => string.Join("\n", unexpectedMismatches));
    }
}
