using System.Collections.Generic;

namespace PHStatistics.Portal.Services.Import;

public class ImportResult {
    public string Week { get; set; }
    public string File { get; set; }
    public string Type { get; set; }
    public int SchoolCount { get; set; }
    public int ItemCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public HashSet<long> PopulationIds { get; set; } = new();
}

public class ImportScanItem {
    public string SchoolName { get; set; }
    public int? SchoolId { get; set; }
    public int Year { get; set; }
    public int Week { get; set; }
    public bool Exists { get; set; }
}

public class ImportScanResult {
    public List<ImportScanItem> Items { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
