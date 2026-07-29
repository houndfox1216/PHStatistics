using System.Collections.Generic;
using System.Linq;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import.ImportSupport;

// 匯入時每次都會建立新的 Class 記錄（連班級名稱都可能跨週不同），沒有穩定的跨週班級識別可用，
// 只能在同一 (CourseId, ClassType) 分組內盡量配對：
//   1) 學生備註（StudentRemark）逐字相同者視為同一班，直接沿用該筆上週人數
//   2) 備註配不到（含備註空白、或備註在上週分組裡找不到相同文字）的，依建立順序（Id 由小到大，
//      對應 Excel 匯入時由上而下的列順序）依序配對上週分組內剩餘未配對的項目
//   3) 上週分組人數不足以配對的（例如本週新增班級），LastWeekNumber 維持 0
public static class LastWeekNumberMatcher {
    public static void Apply(IEnumerable<StudentPopulationItem> currentItems, IEnumerable<StudentPopulationItem> previousItems) {
        var prevGroups = previousItems
            .Where(i => i.Class != null)
            .GroupBy(i => (i.Class.CourseId, i.Class.Type))
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Id).ToList());

        foreach (var grp in currentItems.Where(i => i.Class != null).GroupBy(i => (i.Class.CourseId, i.Class.Type))) {
            if (!prevGroups.TryGetValue(grp.Key, out var prevList)) {
                foreach (var cur in grp) cur.LastWeekNumber = 0;
                continue;
            }

            var available = new List<StudentPopulationItem>(prevList);
            var unmatched = new List<StudentPopulationItem>();

            foreach (var cur in grp.OrderBy(i => i.Id)) {
                StudentPopulationItem match = null;
                if (!string.IsNullOrWhiteSpace(cur.StudentRemark)) {
                    match = available.FirstOrDefault(p => p.StudentRemark == cur.StudentRemark);
                }
                if (match != null) {
                    cur.LastWeekNumber = match.Number;
                    available.Remove(match);
                }
                else {
                    unmatched.Add(cur);
                }
            }

            for (int i = 0; i < unmatched.Count; i++) {
                unmatched[i].LastWeekNumber = i < available.Count ? available[i].Number : 0;
            }
        }
    }
}
