using System;
using System.ComponentModel.DataAnnotations;
using System.Framework.Content;
using System.Framework.Data;
using System.Framework.Globalization;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace

namespace EmptyProject;

/// <summary>
/// 網頁
/// </summary>
public class Page : IMultilingualPageData {
    #region IMultilingualPageData members

    #region IEntityData members

    object IEntityData.Id => Id;

    #endregion

    #region IPublishable members

    DaysOfWeek? IPublishable.AllowedDays => null;
    DateTime? IPublishable.StartTime => StartDate;
    DateTime? IPublishable.EndTime => EndDate;
    TimeSpan? IPublishable.DailyStartTime => null;
    TimeSpan? IPublishable.DailyEndTime => null;

    #endregion

    bool IPageData.IsPartial => false;
    IMultilingualTextData IMultilingualPageData.Content => Content;

    #endregion

    /// <summary>
    /// 識別碼
    /// </summary>
    [Display(Name = "識別碼"), DataMember]
    public int Id { get; set; }

    /// <summary>
    /// 名稱
    /// </summary>
    [Display(Name = "名稱"), DataMember]
    [MaxLength(32)]
    public string Name { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    [Display(Name = "建立時間"), DataMember]
    public DateTime? CreatedTime { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    [Display(Name = "更新時間"), DataMember]
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 發佈開始日期
    /// </summary>
    [Display(Name = "發佈開始日期"), DataMember]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 發佈結束日期
    /// </summary>
    [Display(Name = "發佈結束日期"), DataMember]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 已發布
    /// </summary>
    [Display(Name = "已發布"), DataMember]
    public bool Published { get; set; }

    /// <summary>
    /// 正規URL
    /// </summary>
    [Display(Name = "正規URL"), DataMember]
    [MaxLength(2000)]
    public string CanonicalUrl { get; set; }

    /// <summary>
    /// SEO標題
    /// </summary>
    [Display(Name = "SEO標題"), DataMember]
    [MaxLength(128)]
    public string MetaTitle { get; set; }

    /// <summary>
    /// SEO關鍵字
    /// </summary>
    [Display(Name = "SEO關鍵字"), DataMember]
    [MaxLength(128)]
    public string MetaKeywords { get; set; }

    /// <summary>
    /// SEO描述
    /// </summary>
    [Display(Name = "SEO描述"), DataMember]
    [MaxLength(256)]
    public string MetaDescription { get; set; }

    /// <summary>
    /// 內容
    /// </summary>
    [Display(Name = "內容"), DataMember]
    public MultilingualText Content { get; set; }
}