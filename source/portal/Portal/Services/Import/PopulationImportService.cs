using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import;

public class PopulationImportService {
    private readonly ILogger<PopulationImportService> _logger;
    private readonly Dictionary<StudentPopulationType, IPopulationImporter> _importers;

    public PopulationImportService(ILogger<PopulationImportService> logger) {
        _logger = logger;
        _importers = new Dictionary<StudentPopulationType, IPopulationImporter> {
            [StudentPopulationType.PH] = new PHPopulationImporter(),
            [StudentPopulationType.GEPT] = new GeptPopulationImporter(),
            [StudentPopulationType.PS] = new PSPopulationImporter(),
            [StudentPopulationType.PSJ] = new PSJPopulationImporter(),
            [StudentPopulationType.AfterSchool] = new ASPopulationImporter(),
        };
    }

    public ImportScanResult Scan(DataContext db, StudentPopulationType type, Stream fileStream) {
        if (!_importers.TryGetValue(type, out var importer)) {
            var r = new ImportScanResult();
            r.Errors.Add($"不支援的匯入類型: {type}");
            return r;
        }
        return importer.Scan(db, fileStream);
    }

    public ImportResult Import(DataContext db, StudentPopulationType type, Stream fileStream, string fileName) {
        if (!_importers.TryGetValue(type, out var importer))
            return new ImportResult { Type = type.ToString(), File = fileName, Errors = { $"不支援的匯入類型: {type}" } };

        var result = importer.Import(db, fileStream, _logger);
        result.File = fileName;

        foreach (long popId in result.PopulationIds) {
            CorrectLastWeekNumbers(db, popId);
        }
        return result;
    }

    // 校正上週資料：比照 HomeController.FillLastWeekNumbers 的 (CourseId, ClassType) 分組比對邏輯。
    // 不可用 ClassId 比對——匯入資料每次都建立新 Class 記錄，ClassId 跨週不會相同。
    private void CorrectLastWeekNumbers(DataContext db, long populationId) {
        var pop = db.StudentPopulation.Find(populationId);
        if (pop == null) return;

        var schoolYear = db.SchoolYear.FirstOrDefault(sy => sy.Year == pop.Year && sy.Week == pop.Week);
        if (schoolYear == null) return;

        SchoolYear prevSY = schoolYear.Week > 1
            ? db.SchoolYear.Where(sy => sy.Year == schoolYear.Year && sy.Week == schoolYear.Week - 1)
                           .OrderBy(sy => sy.Id).FirstOrDefault()
            : db.SchoolYear.Where(sy => sy.Year == schoolYear.Year - 1)
                           .OrderByDescending(sy => sy.Week).ThenByDescending(sy => sy.Id).FirstOrDefault();
        if (prevSY == null) return;

        var prevPop = db.StudentPopulation.FirstOrDefault(p =>
            p.SchoolId == pop.SchoolId && p.Year == prevSY.Year && p.Week == prevSY.Week && p.Type == pop.Type);
        if (prevPop == null) return;

        var prevItems = db.StudentPopulationItem
            .Include("Class")
            .Where(i => i.StudentPopulationId == prevPop.Id && i.ClassId != null)
            .ToList();
        var prevLookup = prevItems
            .GroupBy(i => (i.Class.CourseId, i.Class.Type))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Number));

        var currentItems = db.StudentPopulationItem
            .Include("Class")
            .Where(i => i.StudentPopulationId == pop.Id && i.ClassId != null)
            .ToList();

        int updated = 0;
        foreach (var grp in currentItems.GroupBy(i => (i.Class.CourseId, i.Class.Type))) {
            prevLookup.TryGetValue(grp.Key, out int prevTotal);
            bool isFirst = true;
            foreach (var item in grp) {
                item.LastWeekNumber = isFirst ? prevTotal : 0;
                isFirst = false;
                updated++;
            }
        }
        if (updated > 0) db.SaveChanges();
    }
}
