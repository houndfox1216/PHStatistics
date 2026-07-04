using System.IO;
using Microsoft.Extensions.Logging;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Services.Import;

public class GeptPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.GEPT;

    public ImportScanResult Scan(DataContext db, Stream fileStream) {
        var wb = new XSSFWorkbook(fileStream);
        if (wb.NumberOfSheets < 3) {
            var r = new ImportScanResult();
            r.Errors.Add("找不到第3個頁籤（英檢）");
            return r;
        }
        return PHSheetReader.Scan(db, wb.GetSheetAt(2), StudentPopulationType.GEPT, requireTypeIndicator: false);
    }

    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger) {
        var wb = new XSSFWorkbook(fileStream);
        if (wb.NumberOfSheets < 3)
            return new ImportResult { Type = "GEPT", Errors = { "找不到第3個頁籤（英檢）" } };
        return PHSheetReader.Run(db, wb.GetSheetAt(2), StudentPopulationType.GEPT,
            "GEPT", "{0}第{1}週英檢人數表", requireTypeIndicator: false, logger);
    }
}
