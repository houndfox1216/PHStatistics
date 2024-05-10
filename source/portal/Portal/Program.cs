using System.Framework.NLog;
using System.Framework.Web;
using Newtonsoft.Json;
using Environment = System.Framework.Environment;

namespace EmptyProject.Portal;

public class Program : Application {
    private Program(params string[] args) : base("EmptyProject.Portal", args) {
        Policy = new Policy(this);
    }

    public static void Main(string[] args) {
        // 設定輸出JSON的時區為本地時區
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local };
        new Program(args)
            .UseStartup<Startup, Configuration>()
            .UseLoggerContext<NLogContext>(
                configuration => {
                    // ReSharper disable once StringLiteralTypo
                    NLog.GlobalDiagnosticsContext.Set("appbasepath", Environment.Directory.ContentRootPath);
                    return configuration["Cloudfun:NLog:Configuration"]?.ToString() ?? "NLog.config";
                }
            )
            .UseDataContext<DataContext>()
            .UseLocalization()
            .Run();
    }
}