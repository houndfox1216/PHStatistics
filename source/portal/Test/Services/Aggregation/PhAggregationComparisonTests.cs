using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Aggregation;
using System.Framework.Data;

namespace PHStatistics.Portal.Test.Services.Aggregation;

[TestFixture]
[Explicit("Requires a live connection to the dev database; run manually to verify the PH AggregationEngine migration before cutover (see plan Task 2)")]
public class PhAggregationComparisonTests {
    // Id 22/49 (英文/國文總班數)：CountClasses 語意故意從「全部筆數」改成「Number>0 筆數」，允許不同。
    // Id 34/61 (上週英語文/國語文總人數)：故意從「永遠是死值0」改成 LastWeekValue，允許不同。
    // 兩者皆為 docs/superpowers/specs/2026-07-15-ph-aggregation-migration-design.md 記錄的刻意行為變更。
    private static readonly HashSet<int> ExpectedChangeCourseIds = new() { 22, 34, 49, 61 };

    [Test]
    public void Engine_ReproducesStoredNumbers_ForAllRealPhPopulations() {
        using var context = new DataContext();

        var populations = context.StudentPopulation
            .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
            .Where(p => p.Type == StudentPopulationType.PH && p.DataMode == DataMode.Normal)
            .ToList();

        var engine = new AggregationEngine((year, week, schoolId, type) =>
            context.StudentPopulation
                .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type));

        var unexpectedMismatches = new List<string>();
        var expectedChanges = new List<string>();
        foreach (var population in populations) {
            foreach (var item in population.Items.Where(i => i.IsSum).ToList()) {
                int oldNumber = item.Number;
                engine.Calculate(item, population);
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
                item.Number = oldNumber; // 唯讀比對，還原避免誤動資料
            }
        }

        TestContext.WriteLine($"Checked {populations.Count} PH populations.");
        TestContext.WriteLine($"{expectedChanges.Count} expected changes (course 22/34/49/61 — intentional behavior fixes):");
        foreach (var m in expectedChanges) TestContext.WriteLine(m);
        TestContext.WriteLine($"{unexpectedMismatches.Count} UNEXPECTED mismatches:");
        foreach (var m in unexpectedMismatches) TestContext.WriteLine(m);

        Assert.That(unexpectedMismatches, Is.Empty, () => string.Join("\n", unexpectedMismatches));
    }
}
