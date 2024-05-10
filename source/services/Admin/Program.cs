using System.Framework.NLog;
using System.Framework.Web;
using Newtonsoft.Json;
using NLog;

using Environment = System.Framework.Environment;

namespace PHStatistics.Services.Admin {
    /// <summary>
    /// 主程式
    /// </summary>
    public class Program : Application {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="args">參數</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public Program(params string[] args) : base("PHStatistics.Services.Admin", args) { }

        /// <summary>
        /// 主程序
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            // 設定輸出JSON的時區為本地時區
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
                DateTimeZoneHandling = DateTimeZoneHandling.Local
            };
            new Program(args)
                .UseStartup<Startup, Configuration>()
                .UseLoggerContext<NLogContext>(configuration => {
                    GlobalDiagnosticsContext.Set("appbasepath", Environment.Directory.ContentRootPath);
                    return configuration["Cloudfun:NLog:Configuration"]?.ToString() ?? "NLog.config";
                })
                .UseFileSystemContext<WebFileSystemContext>()
                .UseDataContext<DataContext>()
                .Run();
        }
    }
}
