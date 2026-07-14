using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Aggregation;
using System.Framework.Data;

namespace PHStatistics.Portal.Test.Services.Aggregation;

[TestFixture]
[Explicit("Requires a live connection to the dev database; run manually to verify the GEPT AggregationEngine migration before cutover (see plan Task 6)")]
public class GeptAggregationComparisonTests {
    [Test]
    public void Engine_ReproducesStoredNumbers_ForAllRealGeptPopulations() {
        using var context = new DataContext();

        var populations = context.StudentPopulation
            .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
            .Where(p => p.Type == StudentPopulationType.GEPT && p.DataMode == DataMode.Normal)
            .ToList();

        var engine = new AggregationEngine((year, week, schoolId, type) =>
            context.StudentPopulation
                .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type));

        var mismatches = new List<string>();
        foreach (var population in populations) {
            foreach (var item in population.Items.Where(i => i.IsSum).ToList()) {
                int oldNumber = item.Number;
                engine.Calculate(item, population);
                if (item.Number != oldNumber) {
                    mismatches.Add(
                        $"Population {population.Id} (School {population.SchoolId}, {population.Year}/{population.Week}): " +
                        $"course {item.Class.Course.Id} \"{item.Class.Course.Name}\" stored={oldNumber} engine={item.Number}");
                }
                item.Number = oldNumber; // 唯讀比對，還原避免誤動資料
            }
        }

        TestContext.WriteLine($"Checked {populations.Count} GEPT populations, {mismatches.Count} mismatches:");
        foreach (var m in mismatches) TestContext.WriteLine(m);

        Assert.That(mismatches, Is.Empty, () => string.Join("\n", mismatches));
    }
}
