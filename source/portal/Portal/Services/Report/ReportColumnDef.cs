using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Report;

/// <summary>
/// 報表欄位類型
/// </summary>
public enum ReportColumnKind {
    /// <summary>實際班級資料欄（對應 IsSum=false 的項目）</summary>
    Data,
    /// <summary>加總/計算欄（對應 IsSum=true 的項目，Excel 中標記 X 的欄位）</summary>
    Sum
}

/// <summary>
/// 報表欄位定義：描述 Excel 中每一欄的內容與顯示方式
/// </summary>
public class ReportColumnDef {
    public ReportColumnKind Kind { get; init; }

    // 共用
    public int    DepartmentId   { get; init; }
    public string DepartmentName { get; init; }
    public int    CourseId       { get; init; }
    public string CourseName     { get; init; }

    // Kind == Data
    public ClassType? ClassType      { get; init; }
    public int        InstanceIndex  { get; init; }  // 同課同班別的第幾個班（0-based）

    // Kind == Sum
    /// <summary>對應 StudentPopulationItem.Name（IsSum=true 時用於查找加總項目）</summary>
    public string SumItemName { get; init; }

    /// <summary>在欄位第三層顯示的文字（小1/三1/EM1/合計 等）</summary>
    public string ClassLabel => Kind == ReportColumnKind.Sum
        ? "合計"
        : ClassType switch {
            Content.ClassType.Personal  => "EM1",
            Content.ClassType.SubGroup  => $"小{InstanceIndex + 1}",
            Content.ClassType.V3        => $"三{InstanceIndex + 1}",
            Content.ClassType.V2        => $"EM2-{InstanceIndex + 1}",
            Content.ClassType.Group     => $"團{InstanceIndex + 1}",
            _                           => (InstanceIndex + 1).ToString()
        };
}
