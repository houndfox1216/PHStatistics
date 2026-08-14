using System;
using System.Collections.Generic;
using System.Linq;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services {
    public static class SchoolYearResolver {
        /// <summary>
        /// 找出「目前」的學年度週次。bypassImportWindow=true 時（例如管理員）不受 ImportEndDate 上限限制，
        /// 只要週次已開始輸入（InputStartDate ?? WeekStartDate 已到）就視為目前週次，取最近開始的一筆。
        /// </summary>
        public static SchoolYear ResolveCurrent(IEnumerable<SchoolYear> schoolYears, DateTime now, bool bypassImportWindow) {
            IEnumerable<SchoolYear> started = schoolYears.Where(e => (e.InputStartDate ?? e.WeekStartDate) <= now);
            if (bypassImportWindow) {
                return started.OrderByDescending(e => e.Id).FirstOrDefault();
            }
            return started.Where(e => e.ImportEndDate >= now).OrderBy(e => e.Id).FirstOrDefault();
        }
    }
}
