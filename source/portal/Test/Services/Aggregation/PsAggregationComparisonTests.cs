using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Aggregation;
using System.Framework.Data;

namespace PHStatistics.Portal.Test.Services.Aggregation;

[TestFixture]
[Explicit("Requires a live connection to the dev database; run manually to verify the PS AggregationEngine migration before cutover (see plan Task 3)")]
public class PsAggregationComparisonTests {
    // 資料調查（見 docs/superpowers/specs/2026-07-15-ps-aggregation-migration-design.md）沒有發現任何
    // PS 課程需要刻意接受行為變更——9 個引擎計算課程的既有真實資料本身就已經符合新公式，理論上應該 0 落差。
    private static readonly HashSet<int> ExpectedChangeCourseIds = new();

    [Test]
    public void Engine_ReproducesStoredNumbers_ForAllRealPsPopulations() {
        using var context = new DataContext();

        var populations = context.StudentPopulation
            .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
            .Where(p => p.Type == StudentPopulationType.PS && p.DataMode == DataMode.Normal)
            .ToList();

        StudentPopulation LookupLastWeek(StudentPopulation p, StudentPopulationType type) {
            if (p?.SchoolId == null) return null;
            var lastSchoolYear = p.Week > 1
                ? context.SchoolYear.Where(e => e.Year == p.Year && e.Week == p.Week - 1).OrderBy(e => e.Id).FirstOrDefault()
                : context.SchoolYear.Where(e => e.Year == p.Year - 1).OrderByDescending(e => e.Week).ThenByDescending(e => e.Id).FirstOrDefault();
            if (lastSchoolYear == null) return null;
            return context.StudentPopulation
                .Include(x => x.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(x => x.Year == lastSchoolYear.Year && x.Week == lastSchoolYear.Week && x.SchoolId == p.SchoolId && x.Type == type);
        }

        var engine = new AggregationEngine(
            (year, week, schoolId, type) =>
                context.StudentPopulation
                    .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                    .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type),
            LookupLastWeek);

        var unexpectedMismatches = new List<string>();
        var expectedChanges = new List<string>();
        foreach (var population in populations) {
            // Course.Ordinal 排序：144 的 SourceCourseIds=[132,135] 需要 132 已經在同一輪算過，
            // 跟 Task 4 替 classGroup 迴圈補上的 OrderBy(Ordinal) 是同一個順序保證。
            // 三段式處理（先全部算完，再全部比對，最後全部還原）：如果邊算邊比對邊還原，132 會在
            // 144 被算之前就被還原回舊值，144 讀到的就不是「132 剛算完的新值」，順序保證形同虛設。
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

        TestContext.WriteLine($"Checked {populations.Count} PS populations.");
        TestContext.WriteLine($"{expectedChanges.Count} expected changes:");
        foreach (var m in expectedChanges) TestContext.WriteLine(m);
        TestContext.WriteLine($"{unexpectedMismatches.Count} UNEXPECTED mismatches:");
        foreach (var m in unexpectedMismatches) TestContext.WriteLine(m);

        Assert.That(unexpectedMismatches, Is.Empty, () => string.Join("\n", unexpectedMismatches));
    }
}
