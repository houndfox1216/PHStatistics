using System;
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

    // 解析週次跟標題裡的實際西元日期（民國年+1911），用來反查 SchoolYear 表的真實日期範圍決定學年，
    // 不用「月份<8就學年-1」這種猜測規則——校歷實際的切分日期以 SchoolYear 表為準，猜測規則對這個
    // 業務（校歷約從6月底/7月開始算新學年）並不成立，會把7月的資料誤判成上一個學年。
    public static (int week, DateTime? date) ParseWeekAndDate(string title) {
        var weekMatch = Regex.Match(title, @"第(\d+)週");
        int week = weekMatch.Success ? int.Parse(weekMatch.Groups[1].Value) : 0;
        var dateMatch = Regex.Match(title, @"(\d+)年(\d+)月(\d+)日");
        DateTime? date = null;
        if (dateMatch.Success) {
            int rocYear = int.Parse(dateMatch.Groups[1].Value);
            int month = int.Parse(dateMatch.Groups[2].Value);
            int day = int.Parse(dateMatch.Groups[3].Value);
            date = new DateTime(rocYear + 1911, month, day);
        }
        return (week, date);
    }
}
