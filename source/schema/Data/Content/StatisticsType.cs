using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PHStatistics.Content;

/// <summary>
/// 統計計算類型列舉
/// </summary>
public enum StatisticsType : short
{
    /// <summary>
    /// 使用者輸入
    /// </summary>
    [Display(Name = "使用者輸入")]
    [Description("使用者輸入")]
    None = 0,

    /// <summary>
    /// 同班系人數加總
    /// </summary>
    [Display(Name = "同班系人數加總")]
    [Description("同班系人數加總")]
    SumByDepartment = 1,

    /// <summary>
    /// 同班系+同班別人數加總
    /// </summary>
    [Display(Name = "同班系+同班別人數加總")]
    [Description("同班系+同班別人數加總")]
    SumByDepartmentAndClassType = 2,

    /// <summary>
    /// 指定來源班系人數加總
    /// </summary>
    [Display(Name = "指定來源班系人數加總")]
    [Description("指定來源班系人數加總")]
    SumBySourceDepartments = 3,

    /// <summary>
    /// 指定來源課程人數加總
    /// </summary>
    [Display(Name = "指定來源課程人數加總")]
    [Description("指定來源課程人數加總")]
    SumBySourceCourses = 4,

    /// <summary>
    /// 全部人數加總
    /// </summary>
    [Display(Name = "全部人數加總")]
    [Description("全部人數加總")]
    SumAll = 5,

    /// <summary>
    /// 全部人數加總依班別
    /// </summary>
    [Display(Name = "全部人數加總依班別")]
    [Description("全部人數加總依班別")]
    SumAllByClassType = 6,

    /// <summary>
    /// 與上週相比
    /// </summary>
    [Display(Name = "與上週相比")]
    [Description("與上週相比")]
    DiffWithLastWeek = 10,

    /// <summary>
    /// 去年同期比較
    /// </summary>
    [Display(Name = "去年同期比較")]
    [Description("去年同期比較")]
    DiffWithLastYear = 11,

    /// <summary>
    /// 指定來源課程相減（正－負）
    /// </summary>
    [Display(Name = "指定來源課程相減")]
    [Description("指定來源課程相減")]
    DiffBetweenCourses = 12,

    /// <summary>
    /// 指定來源課程相除（分子÷分母），來源課程為分子、扣除來源課程為分母
    /// </summary>
    [Display(Name = "指定來源課程相除")]
    [Description("指定來源課程相除")]
    DivideBySourceCourses = 13,

    /// <summary>
    /// 本年度累計加總：來源課程從本學年度第1週加總到目前週次
    /// </summary>
    [Display(Name = "本年度累計加總")]
    [Description("本年度累計加總")]
    YearToDateSum = 14,

    /// <summary>
    /// 新生人數
    /// </summary>
    [Display(Name = "新生人數")]
    [Description("新生人數")]
    NewStudents = 20,

    /// <summary>
    /// 流失人數
    /// </summary>
    [Display(Name = "流失人數")]
    [Description("流失人數")]
    LostStudents = 21,

    /// <summary>
    /// 班級數量統計
    /// </summary>
    [Display(Name = "班級數量統計")]
    [Description("班級數量統計")]
    CountClasses = 30,

    /// <summary>
    /// 班級數量統計依班別
    /// </summary>
    [Display(Name = "班級數量統計依班別")]
    [Description("班級數量統計依班別")]
    CountClassesByClassType = 31,

    /// <summary>
    /// 上週數值
    /// </summary>
    [Display(Name = "上週數值")]
    [Description("上週數值")]
    LastWeekValue = 40,

    /// <summary>
    /// 去年同期數值
    /// </summary>
    [Display(Name = "去年同期數值")]
    [Description("去年同期數值")]
    LastYearValue = 41,

    /// <summary>
    /// 手動輸入
    /// </summary>
    [Display(Name = "手動輸入")]
    [Description("手動輸入")]
    ManualInput = 50,

    /// <summary>
    /// 平均值
    /// </summary>
    [Display(Name = "平均值")]
    [Description("平均值")]
    Average = 60
}
