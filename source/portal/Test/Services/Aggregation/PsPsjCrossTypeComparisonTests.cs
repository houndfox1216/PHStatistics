using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Aggregation;
using System.Framework.Data;

namespace PHStatistics.Portal.Test.Services.Aggregation;

[TestFixture]
[Explicit("Requires a live connection to the dev database with Task 5's Course 135/139/142 configuration already applied; run manually to verify the PS-PSJ cross-type formulas (see plan Task 6)")]
public class PsPsjCrossTypeComparisonTests {
    // 135/139/142 只套用到「之後新填/重新開啟儲存」的週次（spec 明確決定不回填），
    // 所以拿現有已存資料比對時，多數舊週次的 stored 值仍是舊公式或人工輸入的結果，預期會有落差——
    // 這份測試的目的不是「0落差」，而是驗證「PS報表當週已存在對應PSJ報表」時，引擎算出的值
    // 確實等於 PSJ 12個年級課程562-573的加總/上週加總，公式本身正確。
    [Test]
    public void Engine_ComputesPsjCrossTypeFields_MatchingRealPsjData_WhenBothReportsExistForSameWeek() {
        using var context = new DataContext();

        var psPopulations = context.StudentPopulation
            .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
            .Where(p => p.Type == StudentPopulationType.PS && p.DataMode == DataMode.Normal)
            .ToList();

        StudentPopulation LookupSameWeek(int year, int week, int schoolId, StudentPopulationType type) {
            return context.StudentPopulation
                .Include(p => p.Items).ThenInclude(i => i.Class).ThenInclude(c => c.Course)
                .FirstOrDefault(p => p.Year == year && p.Week == week && p.SchoolId == schoolId && p.Type == type);
        }

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

        var engine = new AggregationEngine(LookupSameWeek, LookupLastWeek);

        int psjCoursesChecked = 0;
        int noPsjDataSkipped = 0;
        var mismatches = new List<string>();

        foreach (var population in psPopulations) {
            var psjSameWeek = LookupSameWeek(population.Year, population.Week, population.SchoolId ?? 0, StudentPopulationType.PSJ);
            if (psjSameWeek == null) {
                noPsjDataSkipped++;
                continue; // 該校當週沒有PSJ報表，135/139理論上會算成0，不是這份測試要驗證的情境
            }

            var course135Item = population.Items.FirstOrDefault(i => i.Class?.CourseId == 135);
            var course139Item = population.Items.FirstOrDefault(i => i.Class?.CourseId == 139);
            if (course135Item == null || course139Item == null) continue;

            int expected135 = psjSameWeek.Items
                .Where(i => i.Class?.CourseId != null && new[] { 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 572, 573 }.Contains(i.Class.CourseId.Value))
                .Sum(i => i.Number);

            int engine135 = engine.Preview(course135Item, population) ?? -1;
            if (engine135 != expected135) {
                mismatches.Add($"Population {population.Id} (School {population.SchoolId}, {population.Year}/{population.Week}): " +
                    $"course 135 expected={expected135} (直接加總PSJ課程562-573) engine={engine135}");
            }

            var psjLastWeek = LookupLastWeek(population, StudentPopulationType.PSJ);
            int expected139 = psjLastWeek?.Items
                .Where(i => i.Class?.CourseId != null && new[] { 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 572, 573 }.Contains(i.Class.CourseId.Value))
                .Sum(i => i.Number) ?? 0;

            int engine139 = engine.Preview(course139Item, population) ?? -1;
            if (engine139 != expected139) {
                mismatches.Add($"Population {population.Id} (School {population.SchoolId}, {population.Year}/{population.Week}): " +
                    $"course 139 expected={expected139} (PSJ上週資料，課程562-573加總) engine={engine139}");
            }
            psjCoursesChecked++;
        }

        TestContext.WriteLine($"Checked {psjCoursesChecked} PS populations with a matching same-week PSJ report.");
        TestContext.WriteLine($"Skipped {noPsjDataSkipped} PS populations with no matching PSJ report (expected — no backfill).");
        TestContext.WriteLine($"{mismatches.Count} mismatches:");
        foreach (var m in mismatches) TestContext.WriteLine(m);

        Assert.That(mismatches, Is.Empty, () => string.Join("\n", mismatches));
    }
}
