using System.Text.RegularExpressions;

namespace PHStatistics.Portal.Services.Import.ImportSupport;

public static class TitleParser {
    public static (int academicYear, int week) ParseYearWeek(string title) {
        var weekMatch = Regex.Match(title, @"第(\d+)週");
        int week = weekMatch.Success ? int.Parse(weekMatch.Groups[1].Value) : 0;
        var dateMatch = Regex.Match(title, @"(\d+)年(\d+)月");
        int academicYear = 0;
        if (dateMatch.Success) {
            int rocYear = int.Parse(dateMatch.Groups[1].Value);
            int month = int.Parse(dateMatch.Groups[2].Value);
            academicYear = month < 8 ? rocYear - 1 : rocYear;
        }
        return (academicYear, week);
    }
}
