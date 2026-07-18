using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PHStatistics.Content;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

[TestFixture]
public class ReportExportServiceStructureTests {

    private static List<(string RegionName, List<StudentPopulation> Populations)> InvokeGroupByRegion(
        List<StudentPopulation> populations) {
        var method = typeof(ReportExportService).GetMethod("GroupByRegion",
            BindingFlags.NonPublic | BindingFlags.Static);
        return (List<(string, List<StudentPopulation>)>)method.Invoke(null, new object[] { populations });
    }

    private static StudentPopulation MakePopulation(string schoolName, string regionName, int regionOrdinal) {
        var region = regionName == null ? null : new Region { Name = regionName, Ordinal = regionOrdinal };
        return new StudentPopulation {
            School = new School { Name = schoolName, Region = region, RegionId = region == null ? (int?)null : 1 },
            Items = new List<StudentPopulationItem>()
        };
    }

    [Test]
    public void GroupByRegion_GroupsBySchoolRegionName_PreservingRegionOrdinalOrder() {
        var populations = new List<StudentPopulation> {
            MakePopulation("東安", "南區", 1),
            MakePopulation("南京", "中北區", 0),
            MakePopulation("莊敬", "南區", 1),
        };

        var groups = InvokeGroupByRegion(populations);

        Assert.That(groups.Select(g => g.RegionName).ToList(), Is.EqualTo(new[] { "中北區", "南區" }));
        Assert.That(groups.First(g => g.RegionName == "南區").Populations.Select(p => p.School.Name),
            Is.EqualTo(new[] { "東安", "莊敬" }));
    }

    [Test]
    public void GroupByRegion_SchoolWithoutRegion_GoesIntoUnassignedGroupLast() {
        var populations = new List<StudentPopulation> {
            MakePopulation("南京", "中北區", 0),
            MakePopulation("查無分區學校", null, 0),
        };

        var groups = InvokeGroupByRegion(populations);

        Assert.That(groups.Last().RegionName, Is.EqualTo("未分區"));
        Assert.That(groups.Last().Populations.Single().School.Name, Is.EqualTo("查無分區學校"));
    }
}
