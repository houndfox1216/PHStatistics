namespace EmptyProject.Services.Admin.Services {
    /// <summary>
    /// Google Analytics Report
    /// </summary>
    public class GoogleAnalyticsReport {
        /// <summary>
        /// Bounce Rate
        /// </summary>
        public double BounceRate { get; set; }

        /// <summary>
        /// Avg Session Duration
        /// </summary>
        public double AvgSessionDuration { get; set; }

        /// <summary>
        /// Sessions
        /// </summary>
        public int Sessions { get; set; }

        /// <summary>
        /// Users
        /// </summary>
        public int Users { get; set; }

        /// <summary>
        /// New Users
        /// </summary>
        public int NewUsers { get; set; }

        /// <summary>
        /// Pageviews
        /// </summary>
        public int Pageviews { get; set; }

        /// <summary>
        /// Pageviews per Session
        /// </summary>
        public float PageviewsPerSession { get; set; }

        /// <summary>
        /// Users by Country
        /// </summary>
        public ChartDataItem[] UsersByCountry { get; set; }

        /// <summary>
        /// Users by Device Category
        /// </summary>
        public ChartDataItem[] UsersByDeviceCategory { get; set; }

        /// <summary>
        /// Users by Browser
        /// </summary>
        public ChartDataItem[] UsersByBrowser { get; set; }

        /// <summary>
        /// Users by Language
        /// </summary>
        public ChartDataItem[] UsersByLanguage { get; set; }

        /// <summary>
        /// Users by Date
        /// </summary>
        public ChartDataItem[] UsersByDate { get; set; }

        /// <summary>
        /// New Users by Date
        /// </summary>
        public ChartDataItem[] NewUsersByDate { get; set; }
    }

    /// <summary>
    /// 圖表數據項目
    /// </summary>
    public class ChartDataItem {
        /// <summary>
        /// 建構
        /// </summary>
        public ChartDataItem() { }

        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="name">名稱</param>
        /// <param name="value">數值</param>
        public ChartDataItem(string name, decimal value) {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// 名稱
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 值
        /// </summary>
        public decimal Value { get; set; }
    }
}
