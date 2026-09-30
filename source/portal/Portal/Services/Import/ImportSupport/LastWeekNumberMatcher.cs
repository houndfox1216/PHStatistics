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
    // 人數表開頁時同步上週人數用：本週與上週同一個 Class.Id 底下可能有多筆 Item（小組班共用同一筆 Class），
    // 不能一律取上週第一筆，改成同 Class.Id 分組內先比備註、再依 Id 順序配對。
    // 上週完全沒有該 Class 的項目維持原值不動；同組內上週人數不足以配對的多出項目設為 0。
    public static void ApplyByClassId(IEnumerable<StudentPopulationItem> currentItems, IEnumerable<StudentPopulationItem> previousItems) {
        PairByClassId(currentItems, previousItems);
    }

    public sealed record MatchedPair(StudentPopulationItem Current, StudentPopulationItem Previous, bool Certain);

    // 以 Item.PreviousItemId 為主的同步：有連結且上週項目還在者直接取值；其餘退回 Class 配對，
    // 並且只把「確定」的配對寫回 PreviousItemId（備註唯一相符，或該組上下週各只有一筆）。
    public static void SyncFromLastWeek(IEnumerable<StudentPopulationItem> currentItems, IEnumerable<StudentPopulationItem> previousItems) {
        var prevList = previousItems.Where(i => i.Class != null).ToList();
        var prevById = prevList.ToDictionary(i => i.Id);
        var claimed = new HashSet<long>();
        var unlinked = new List<StudentPopulationItem>();

        foreach (var cur in currentItems.Where(i => i.Class != null).OrderBy(i => i.Id)) {
            if (cur.PreviousItemId.HasValue
                && prevById.TryGetValue(cur.PreviousItemId.Value, out var linked)
                && claimed.Add(linked.Id)) {
                cur.LastWeekNumber = linked.Number;
            }
            else {
                cur.PreviousItemId = null; // 連結失效或重複認領，先清掉，配對確定再寫回
                unlinked.Add(cur);
            }
        }

        var remainingPrev = prevList.Where(p => !claimed.Contains(p.Id)).ToList();
        foreach (var pair in PairByClassId(unlinked, remainingPrev)) {
            if (pair.Certain) pair.Current.PreviousItemId = pair.Previous.Id;
        }
    }

    private static List<MatchedPair> PairByClassId(IEnumerable<StudentPopulationItem> currentItems, IEnumerable<StudentPopulationItem> previousItems) {
        var pairs = new List<MatchedPair>();
        var prevGroups = previousItems
            .Where(i => i.Class != null)
            .GroupBy(i => i.Class.Id)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Id).ToList());

        foreach (var grp in currentItems.Where(i => i.Class != null).GroupBy(i => i.Class.Id)) {
            if (!prevGroups.TryGetValue(grp.Key, out var prevList)) continue;
            pairs.AddRange(PairGroup(grp.OrderBy(i => i.Id).ToList(), prevList));
        }
        return pairs;
    }

    private static List<MatchedPair> PairGroup(List<StudentPopulationItem> current, List<StudentPopulationItem> previous) {
        var pairs = new List<MatchedPair>();
        var available = new List<StudentPopulationItem>(previous);
        var unmatched = new List<StudentPopulationItem>();

        foreach (var cur in current) {
            StudentPopulationItem match = null;
            if (!string.IsNullOrWhiteSpace(cur.StudentRemark)) {
                match = available.FirstOrDefault(p => p.StudentRemark == cur.StudentRemark);
            }
            if (match != null) {
                bool unique = current.Count(c => c.StudentRemark == cur.StudentRemark) == 1
                    && previous.Count(p => p.StudentRemark == cur.StudentRemark) == 1;
                cur.LastWeekNumber = match.Number;
                available.Remove(match);
                pairs.Add(new MatchedPair(cur, match, unique));
            }
            else {
                unmatched.Add(cur);
            }
        }

        bool oneToOne = current.Count == 1 && previous.Count == 1;
        for (int i = 0; i < unmatched.Count; i++) {
            if (i < available.Count) {
                unmatched[i].LastWeekNumber = available[i].Number;
                pairs.Add(new MatchedPair(unmatched[i], available[i], oneToOne));
            }
            else {
                unmatched[i].LastWeekNumber = 0;
            }
        }
        return pairs;
    }

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

            PairGroup(grp.OrderBy(i => i.Id).ToList(), prevList);
        }
    }
}
