using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PHStatistics.Content;
using PHStatistics.Portal.Controllers;
using PHStatistics.Portal.Services.Import.ImportSupport;

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

    public ImportScanResult Scan(DataContext db, StudentPopulationType type, Stream fileStream, int? overrideYear = null, int? overrideWeek = null) {
        if (!_importers.TryGetValue(type, out var importer)) {
            var r = new ImportScanResult();
            r.Errors.Add($"不支援的匯入類型: {type}");
            return r;
        }
        return importer.Scan(db, fileStream, overrideYear, overrideWeek);
    }

    public ImportResult Import(DataContext db, StudentPopulationType type, Stream fileStream, string fileName, int? overrideYear = null, int? overrideWeek = null) {
        if (!_importers.TryGetValue(type, out var importer))
            return new ImportResult { Type = type.ToString(), File = fileName, Errors = { $"不支援的匯入類型: {type}" } };

        var result = importer.Import(db, fileStream, _logger, overrideYear, overrideWeek);
        result.File = fileName;

        foreach (long popId in result.PopulationIds) {
            CorrectLastWeekNumbers(db, popId);
        }
        // 匯入完成後立即計算IsSum合計欄位，不必等分校人員打開頁面存檔才觸發重算
        // （跟StudentPopulationController各Action呼叫的是同一套邏輯，SumPHPopulation依Type自行分支）。
        foreach (long popId in result.PopulationIds) {
            try {
                StudentPopulationController.SumPHPopulation(popId);
            } catch (System.Exception ex) {
                _logger.LogWarning(ex, "匯入後計算合計欄位失敗: populationId={PopId}", popId);
            }
        }
        return result;
    }

    // 校正上週資料：HomeController.FillLastWeekNumbers 共用同一份邏輯（不可各自維護一份，
    // 否則兩處遲早會分岔）。不可用 ClassId 比對——匯入資料每次都建立新 Class 記錄，ClassId
    // 跨週不會相同，改用 LastWeekNumberMatcher 在同一 (CourseId, ClassType) 分組內依學生備註／
    // 建立順序配對。
    // dryRun=true 時只計算變動、不呼叫 SaveChanges，供一次性回填前先行預覽；呼叫端應在讀取
    // 回傳結果後對這個 db 執行 ChangeTracker.Clear()，避免未儲存的異動殘留在追蹤中影響下一筆。
    public static List<LastWeekNumberChange> CorrectLastWeekNumbers(DataContext db, long populationId, bool dryRun = false) {
        var changes = new List<LastWeekNumberChange>();
        var pop = db.StudentPopulation.Find(populationId);
        if (pop == null) return changes;

        var schoolYear = db.SchoolYear.FirstOrDefault(sy => sy.Year == pop.Year && sy.Week == pop.Week);
        if (schoolYear == null) return changes;

        SchoolYear prevSY = schoolYear.Week > 1
            ? db.SchoolYear.Where(sy => sy.Year == schoolYear.Year && sy.Week == schoolYear.Week - 1)
                           .OrderBy(sy => sy.Id).FirstOrDefault()
            : db.SchoolYear.Where(sy => sy.Year == schoolYear.Year - 1)
                           .OrderByDescending(sy => sy.Week).ThenByDescending(sy => sy.Id).FirstOrDefault();
        if (prevSY == null) return changes;

        var prevPop = db.StudentPopulation.FirstOrDefault(p =>
            p.SchoolId == pop.SchoolId && p.Year == prevSY.Year && p.Week == prevSY.Week && p.Type == pop.Type);
        if (prevPop == null) return changes;

        var prevItems = db.StudentPopulationItem
            .Include("Class")
            .Where(i => i.StudentPopulationId == prevPop.Id && i.ClassId != null)
            .ToList();

        var currentItems = db.StudentPopulationItem
            .Include("Class")
            .Where(i => i.StudentPopulationId == pop.Id && i.ClassId != null)
            .ToList();

        var before = currentItems.ToDictionary(i => i.Id, i => i.LastWeekNumber);
        LastWeekNumberMatcher.Apply(currentItems, prevItems);
        foreach (var item in currentItems) {
            if (before[item.Id] != item.LastWeekNumber)
                changes.Add(new LastWeekNumberChange(item.Id, before[item.Id], item.LastWeekNumber));
        }

        if (!dryRun && changes.Count > 0) db.SaveChanges();
        return changes;
    }
}

public record LastWeekNumberChange(long ItemId, int OldValue, int NewValue);
