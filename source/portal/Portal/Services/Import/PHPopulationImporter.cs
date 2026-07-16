using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Services.Import;

public class PHPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.PH;

    public ImportScanResult Scan(DataContext db, Stream fileStream, int? overrideYear = null, int? overrideWeek = null) {
        var wb = new XSSFWorkbook(fileStream);
        var combined = new ImportScanResult();
        for (int s = 0; s < wb.NumberOfSheets; s++) {
            var sheet = wb.GetSheetAt(s);
            if (CourseMapping.PhExcludeSheets.Contains(sheet.SheetName)) continue;
            var r = PHSheetReader.Scan(db, sheet, StudentPopulationType.PH, requireTypeIndicator: true);
            combined.Items.AddRange(r.Items);
            combined.Errors.AddRange(r.Errors);
        }
        return combined;
    }

    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger, int? overrideYear = null, int? overrideWeek = null) {
        var wb = new XSSFWorkbook(fileStream);
        var combined = new ImportResult { Type = "PH" };
        for (int s = 0; s < wb.NumberOfSheets; s++) {
            var sheet = wb.GetSheetAt(s);
            if (CourseMapping.PhExcludeSheets.Contains(sheet.SheetName)) continue;
            var r = PHSheetReader.Run(db, sheet, StudentPopulationType.PH,
                "PH", "{0}第{1}週百瀚人數表", requireTypeIndicator: true, logger);
            combined.SchoolCount += r.SchoolCount;
            combined.ItemCount += r.ItemCount;
            combined.Errors.AddRange(r.Errors);
            foreach (long id in r.PopulationIds) combined.PopulationIds.Add(id);
        }
        return combined;
    }
}
