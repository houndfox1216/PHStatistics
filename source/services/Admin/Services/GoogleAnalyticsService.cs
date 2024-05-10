using System;
using System.Collections.Generic;
using System.Framework.Application;
using System.Framework.Web;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Azure;
using Google.Analytics.Data.V1Beta;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Org.BouncyCastle.Asn1.X509;


namespace EmptyProject.Services.Admin.Services {
    /// <summary>
    /// Google Analytics Service
    /// </summary>
    public class GoogleAnalyticsService {
        private readonly string property;
        private readonly BetaAnalyticsDataClient client = null;

        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="property">屬性</param>
        /// <param name="credential">憑證</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FileNotFoundException">找不到憑證時拋出</exception>
        public GoogleAnalyticsService(string property = null, FileInfo credential = null) {
            this.property = property ?? Application.Current.Configuration["Cloudfun:GoogleAnalytics:Property"].ToString()
                ?? throw new ArgumentException("請指定Property");

            if (credential == null) {
                var credentialPath = Application.Current.Configuration["Cloudfun:GoogleAnalytics:Credential"].ToString()
                    ?? throw new ArgumentException("請指定憑證路徑");
                credential = new FileInfo(Application.Current.GetRealPath(credentialPath));
            }
            if (!credential.Exists) throw new FileNotFoundException("請指定憑證路徑");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credential.FullName);

            this.client = BetaAnalyticsDataClient.Create();
        }

        /// <summary>
        /// 執行請求
        /// </summary>
        /// <param name="startDate">起始時間</param>
        /// <param name="endDate">結束時間</param>
        /// <param name="metric">指標</param>
        /// <param name="dimension">維度</param>
        /// <returns>回應</returns>
        public RunReportResponse ExecuteRequest(DateTime startDate, DateTime endDate, string metric, string dimension = null) {
            startDate = startDate.Date;
            endDate = endDate.Date;
            if (endDate == startDate) endDate = startDate.AddDays(1);
            var request = new RunReportRequest {
                Property = $"properties/{property}",
                Metrics = { new Metric { Name = metric } },
                DateRanges = { new DateRange { StartDate = startDate.ToString("yyyy-MM-dd"), EndDate = endDate.ToString("yyyy-MM-dd") } }
            };
            if (dimension != null) request.Dimensions.Add(new Dimension { Name = dimension });
            return client.RunReport(request);
        }

        /// <summary>
        /// 跳出率
        /// </summary>
        /// <returns></returns>
        public double GetBounceRate(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "bounceRate");
            return double.Parse(response.Rows[0].MetricValues[0].Value);
        }

        /// <summary>
        /// 平均工作階段停留時間
        /// </summary>
        /// <returns></returns>
        public double GetAvgSessionDuration(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "averageSessionDuration");
            return double.Parse(response.Rows[0].MetricValues[0].Value);
        }

        /// <summary>
        /// 工作階段
        /// </summary>
        /// <returns></returns>
        public int GetSessions(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "sessions");
            return int.Parse(response.Rows[0].MetricValues[0].Value);
        }

        /// <summary>
        /// 訪客數量
        /// </summary>
        /// <returns></returns>
        public int GetUsers(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "activeUsers");
            return int.Parse(response.Rows[0].MetricValues[0].Value);
        }

        /// <summary>
        /// 新訪客數量
        /// </summary>
        /// <returns></returns>
        public int GetNewUsers(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "newUsers");
            return int.Parse(response.Rows[0].MetricValues[0].Value);
        }

        /// <summary>
        /// 瀏覽量
        /// </summary>
        /// <returns></returns>
        public int GetPageviews(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "screenPageViews");
            return int.Parse(response.Rows[0].MetricValues[0].Value);
        }

        /// <summary>
        /// 平均每連接階段瀏覽頁數
        /// </summary>
        /// <returns></returns>
        public float GetPageviewsPerSession(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "screenPageViewsPerSession");
            return float.Parse(response.Rows[0].MetricValues[0].Value);
        }

        /// <summary>
        /// 取得裝置類型
        /// </summary>
        /// <returns>訪客裝置類型數據(裝置類型、人數)</returns>
        public IEnumerable<Tuple<string, decimal>> GetVisitorDeviceCategeory(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "activeUsers", "deviceCategory");
            var list = new List<Tuple<string, decimal>>();
            foreach (var row in response.Rows) list.Add(new Tuple<string, decimal>(row.DimensionValues[0].Value, decimal.Parse(row.MetricValues[0].Value)));
            return list;
        }

        /// <summary>
        /// 取得瀏覽器
        /// </summary>
        /// <returns>訪客瀏覽器數據(瀏覽器、人數)</returns>
        public IEnumerable<Tuple<string, decimal>> GetVisitorBrowser(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "activeUsers", "browser");
            var list = new List<Tuple<string, decimal>>();
            foreach (var row in response.Rows) list.Add(new Tuple<string, decimal>(row.DimensionValues[0].Value, decimal.Parse(row.MetricValues[0].Value)));
            return list;
        }


        /// <summary>
        /// 取得訪客國別
        /// </summary>
        /// <returns>訪客國別數據(國別、人數)</returns>
        public IEnumerable<Tuple<string, decimal>> GetVisitorCountry(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "activeUsers", "country");
            var list = new List<Tuple<string, decimal>>();
            foreach (var row in response.Rows) list.Add(new Tuple<string, decimal>(row.DimensionValues[0].Value, decimal.Parse(row.MetricValues[0].Value)));
            return list;
        }

        /// <summary>
        /// 取得訪客使用語系
        /// </summary>
        /// <returns>訪客國別數據(語系、人數)</returns>
        public IEnumerable<Tuple<string, decimal>> GetVisitorLanguage(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "activeUsers", "language");
            var list = new List<Tuple<string, decimal>>();
            foreach (var row in response.Rows) list.Add(new Tuple<string, decimal>(row.DimensionValues[0].Value, decimal.Parse(row.MetricValues[0].Value)));
            return list;
        }

        /// <summary>
        /// 取得訪問數
        /// </summary>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        public IEnumerable<Tuple<string, decimal>> GetVisitsCount(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "activeUsers", "date");
            var list = new List<Tuple<string, decimal>>();
            foreach (var row in response.Rows) {
                var date = row.DimensionValues[0].Value;
                var value = decimal.Parse(row.MetricValues[0].Value);
                list.Add(new Tuple<string, decimal>($"{date[0..4]}-{date[4..6]}-{date[6..]}", value));
            }
            return list;
        }

        /// <summary>
        /// 取得新客訪問數
        /// </summary>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        public IEnumerable<Tuple<string, decimal>> GetNewVisitsCount(DateTime startDate, DateTime endDate) {
            var response = ExecuteRequest(startDate, endDate, "newUsers", "date");
            var list = new List<Tuple<string, decimal>>();
            foreach (var row in response.Rows) {
                var date = row.DimensionValues[0].Value;
                var value = decimal.Parse(row.MetricValues[0].Value);
                list.Add(new Tuple<string, decimal>($"{date[0..4]}-{date[4..6]}-{date[6..]}", value));
            }
            return list;
        }

        /// <summary>
        /// 報告
        /// </summary>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        public GoogleAnalyticsReport Report(DateTime startDate, DateTime endDate) {
            return new GoogleAnalyticsReport {
                Users = GetUsers(startDate, endDate),
                NewUsers = GetNewUsers(startDate, endDate),
                Sessions = GetSessions(startDate, endDate),
                Pageviews = GetPageviews(startDate, endDate),
                PageviewsPerSession = GetPageviewsPerSession(startDate, endDate),
                AvgSessionDuration = GetAvgSessionDuration(startDate, endDate),
                BounceRate = GetBounceRate(startDate, endDate),
                UsersByDate = GetVisitsCount(startDate, endDate).Select(e => new ChartDataItem(e.Item1, e.Item2)).OrderBy(e => e.Name).ToArray(),
                NewUsersByDate = GetNewVisitsCount(startDate, endDate).Select(e => new ChartDataItem(e.Item1, e.Item2)).OrderBy(e => e.Name).ToArray(),
                UsersByDeviceCategory = GetVisitorDeviceCategeory(startDate, endDate).Select(e => new ChartDataItem(e.Item1, e.Item2)).OrderByDescending(e => e.Value).ToArray(),
                UsersByBrowser = GetVisitorBrowser(startDate, endDate).Select(e => new ChartDataItem(e.Item1, e.Item2)).OrderByDescending(e => e.Value).ToArray(),
                UsersByCountry = GetVisitorCountry(startDate, endDate).Select(e => new ChartDataItem(e.Item1, e.Item2)).OrderByDescending(e => e.Value).ToArray(),
                UsersByLanguage = GetVisitorLanguage(startDate, endDate).Select(e => new ChartDataItem(e.Item1, e.Item2)).OrderByDescending(e => e.Value).ToArray(),
            };
        }
    }
}
