using System;
using System.Collections.Generic;
using PHStatistics.Content;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

[TestFixture]
public class SchoolYearResolverTests {
    private static SchoolYear MakeWeek(int id, DateTime weekStart, DateTime importEnd, DateTime? inputStart = null) {
        return new SchoolYear {
            Id = id,
            WeekStartDate = weekStart,
            WeekEndDate = weekStart.AddDays(7),
            ImportEndDate = importEnd,
            InputStartDate = inputStart,
        };
    }

    [Test]
    public void ResolveCurrent_ReturnsWeek_WhenNowWithinWindow() {
        var week = MakeWeek(1, new DateTime(2026, 8, 3), new DateTime(2026, 8, 17));
        var now = new DateTime(2026, 8, 10);

        var result = SchoolYearResolver.ResolveCurrent(new[] { week }, now, bypassImportWindow: false);

        Assert.That(result, Is.SameAs(week));
    }

    [Test]
    public void ResolveCurrent_ReturnsNull_WhenNowAfterImportEndDate_AndNotBypassed() {
        var week = MakeWeek(1, new DateTime(2026, 8, 3), new DateTime(2026, 8, 10));
        var now = new DateTime(2026, 8, 15);

        var result = SchoolYearResolver.ResolveCurrent(new[] { week }, now, bypassImportWindow: false);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ResolveCurrent_ReturnsWeek_WhenNowAfterImportEndDate_AndBypassed() {
        var week = MakeWeek(1, new DateTime(2026, 8, 3), new DateTime(2026, 8, 10));
        var now = new DateTime(2026, 8, 15);

        var result = SchoolYearResolver.ResolveCurrent(new[] { week }, now, bypassImportWindow: true);

        Assert.That(result, Is.SameAs(week));
    }

    [Test]
    public void ResolveCurrent_ReturnsNull_WhenBypassed_ButWeekHasNotStartedYet() {
        var week = MakeWeek(1, new DateTime(2026, 8, 20), new DateTime(2026, 8, 27));
        var now = new DateTime(2026, 8, 10);

        var result = SchoolYearResolver.ResolveCurrent(new[] { week }, now, bypassImportWindow: true);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ResolveCurrent_WhenBypassed_PicksMostRecentlyStartedWeek() {
        var older = MakeWeek(1, new DateTime(2026, 8, 3), new DateTime(2026, 8, 10));
        var newer = MakeWeek(2, new DateTime(2026, 8, 10), new DateTime(2026, 8, 17));
        var now = new DateTime(2026, 8, 21); // both weeks' ImportEndDate has already passed

        var result = SchoolYearResolver.ResolveCurrent(new[] { older, newer }, now, bypassImportWindow: true);

        Assert.That(result, Is.SameAs(newer));
    }

    [Test]
    public void ResolveCurrent_UsesInputStartDate_OverWeekStartDate_WhenSet() {
        var week = MakeWeek(1, new DateTime(2026, 8, 3), new DateTime(2026, 8, 17), inputStart: new DateTime(2026, 8, 6));
        var now = new DateTime(2026, 8, 4); // after WeekStartDate but before InputStartDate

        var result = SchoolYearResolver.ResolveCurrent(new[] { week }, now, bypassImportWindow: false);

        Assert.That(result, Is.Null);
    }
}
