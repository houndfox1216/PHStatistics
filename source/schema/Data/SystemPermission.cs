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
    /// 商品管理
    /// </summary>
    [Display(Name = "商品管理")]
    Product,

    /// <summary>
    /// 媒體管理
    /// </summary>
    [Display(Name = "媒體管理")]
    MediaFile,

    /// <summary>
    /// 直播管理
    /// </summary>
    [Display(Name = "直播管理")]
    LiveSource,

    /// <summary>
    /// 網址管理
    /// </summary>
    [Display(Name = "網址管理")]
    UrlSegment,

    /// <summary>
    /// 網頁管理
    /// </summary>
    [Display(Name = "網頁管理")]
    Page
}