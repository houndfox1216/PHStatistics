using System.Framework.NLog;
using System.Framework.Web;
using Newtonsoft.Json;
using NLog;
using Environment = System.Framework.Environment;

namespace EmptyProject.Services.Payment;

public class Program : Application {
    public Program(params string[] args) : base("EmptyProject.Service.Payment", args) { }

    public static void Main(string[] args) {
        // 設定輸出 JSON 的時區為本地時區
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local };
        new Program(args)
            .UseStartup<Startup, Configuration>()
            .UseLoggerContext<NLogContext>(
                configuration => {
                    GlobalDiagnosticsContext.Set("appbasepath", Environment.Directory.ContentRootPath);
                    return configuration["Cloudfun:NLog:Configuration"]?.ToString() ?? "NLog.config";
                }
            )
            .UseDataContext<DataContext>()
            .Run();
    }
}