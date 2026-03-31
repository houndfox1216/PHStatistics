using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;

namespace PHStatistics;

/// <summary>
/// 系統權限
/// </summary>
[Description("系統權限")]
public enum SystemPermission : short {
    /// <summary>
    /// 系統管理員
    /// </summary>
    [Display(Name = "系統管理員"), NotEnumerated]
    Administrator,

    /// <summary>
    /// 系統配置
    /// </summary>
    [Display(Name = "系統配置")]
    Configuration,

    /// <summary>
    /// 個資管理
    /// </summary>
    [Display(Name = "個資管理")]
    Person,

    /// <summary>
    /// 角色管理
    /// </summary>
    [Display(Name = "角色管理")]
    Role,

    /// <summary>
    /// 用戶管理
    /// </summary>
    [Display(Name = "用戶管理")]
    User,

    /// <summary>
    /// 操作紀錄
    /// </summary>
    [Display(Name = "操作紀錄")]
    ActionLog,

    /// <summary>
    /// 屬性管理
    /// </summary>
    [Display(Name = "屬性管理")]
    Attribute,

    /// <summary>
    /// 相簿管理
    /// </summary>
    [Display(Name = "相簿管理")]
    Album,

    /// <summary>
    /// 類別管理
    /// </summary>
    [Display(Name = "類別管理")]
    Category,

    /// <summary>
    /// 標籤管理
    /// </summary>
    [Display(Name = "標籤管理")]
    Tag,

    /// <summary>
    /// 廣告管理
    /// </summary>
    [Display(Name = "廣告管理")]
    Banner,

    /// <summary>
    /// 新聞管理
    /// </summary>
    [Display(Name = "新聞管理")]
    News,

    /// <summary>
    /// 網頁管理
    /// </summary>
    [Display(Name = "網頁管理")]
    Page,

    /// <summary>
    /// 分校管理
    /// </summary>
    [Display(Name = "分校管理")]
    School,

    /// <summary>
    /// 班系管理
    /// </summary>
    [Display(Name = "班系管理")]
    CourseDepartment,


    /// <summary>
    /// 課程管理
    /// </summary>
    [Display(Name = "課程管理")]
    Course,

    /// <summary>
    /// 班級管理
    /// </summary>
    [Display(Name = "班級管理")]
    Class,

    /// <summary>
    /// 班級管理
    /// </summary>
    [Display(Name = "分校人員")]
    Member,

    /// <summary>
    /// 人數表管理
    /// </summary>
    [Display(Name = "人數表管理")]
    StudentPopulation,

    /// <summary>
    /// 網址管理
    /// </summary>
    [Display(Name = "網址管理")]
    UrlSegment,

    /// <summary>
    /// 區域管理
    /// </summary>
    [Display(Name = "區域管理")]
    Region,

    /// <summary>
    /// 查看所有分校資料（管理處人員）
    /// </summary>
    [Display(Name = "查看所有分校")]
    ViewAllSchools,

    /// <summary>
    /// 學年度管理
    /// </summary>
    [Display(Name = "學年度管理")]
    SchoolYear,

}