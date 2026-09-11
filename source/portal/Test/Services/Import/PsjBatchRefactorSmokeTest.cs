using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import;

namespace PHStatistics.Portal.Test.Services.Import;

// 一次性驗證：PSJPopulationImporter 批次存檔改動（PopulationWriteHelper 效能優化）不影響匯入結果。
// 期望值（11校/51筆）是用同一份檔案分別跑過改動前/改動後的程式碼比對得出，兩者一致；
// 直接讀該檔案原始儲存格內容（openpyxl）也確認114年第1週（開學第一週）本來就大多是空白，51筆才是真實資料量，
// 不是batching改動造成的資料遺失。
[TestFixture]
[Explicit("Writes real rows to the local dev DB using a 114-year week-1 PSJ fixture from a prior session's staging folder")]
public class PsjBatchRefactorSmokeTest {
    private const string FilePath = @"C:\Users\hound\AppData\Local\Temp\claude\C--Company-PHStatistics-portal\1d718402-4337-4915-bafe-7f909320352f\scratchpad\import114_staging\1\（百倍速）人數統計表更新版114.07.05.xlsx";

    [Test]
    public void Import_Week1_MatchesKnownGoodCounts() {
        using var db = new DataContext();
        var importer = new PSJPopulationImporter();
        using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
        var result = importer.Import(db, fs, NullLogger.Instance, overrideYear: 114, overrideWeek: 1);

        Assert.That(result.Errors, Is.Empty, () => string.Join("\n", result.Errors));
        Assert.That(result.SchoolCount, Is.EqualTo(11));
        Assert.That(result.ItemCount, Is.EqualTo(51));

        int dbItemCount = db.StudentPopulationItem
            .Count(i => i.StudentPopulation.Year == 114 && i.StudentPopulation.Week == 1 && i.StudentPopulation.Type == StudentPopulationType.PSJ);
        Assert.That(dbItemCount, Is.EqualTo(51));
    }
}
