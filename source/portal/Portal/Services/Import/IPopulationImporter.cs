using System.IO;
using Microsoft.Extensions.Logging;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import;

public interface IPopulationImporter {
    StudentPopulationType Type { get; }
    ImportScanResult Scan(DataContext db, Stream fileStream);
    ImportResult Import(DataContext db, Stream fileStream, ILogger logger);
}
