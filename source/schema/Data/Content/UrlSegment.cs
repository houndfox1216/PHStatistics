using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Content;
using System.Framework.Data;
using System.Framework.Globalization;
using System.Runtime.Serialization;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

// ReSharper disable once CheckNamespace

<<<<<<< HEAD
namespace EmptyProject;
=======
namespace PHStatistics;
>>>>>>> origin/develop/schema

/// <summary>
/// 網址區段
/// </summary>
[Description("網址區段")]
public class UrlSegment : IMultilingualUrlSegmentData {
    #region IMultilingualUrlSegmentData members

    #region IUrlSegmentData members

    IUrlSegmentData IUrlSegmentData.Parent => Parent;
    ICollection<IUrlSegmentData> IUrlSegmentData.Children => new List<IUrlSegmentData>(Children ?? Array.Empty<UrlSegment>());
    object IUrlSegmentData.PageId => PageId;
    IPageData IUrlSegmentData.Page => Page;

    #region IEntityData members

    object IEntityData.Id => Id;

    #endregion

    #region ISortable memebers

    int ISortable.Ordinal => Ordinal ?? 0;

    #endregion
    
    #endregion

    IMultilingualTextData IMultilingualUrlSegmentData.Title => Title;
    
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
    /// 資料模式
    /// </summary>
    [Display(Name = "資料模式"), DataMember]
    public DataMode DataMode { get; set; }

    /// <summary>
    /// 排列順序
    /// </summary>
    [Display(Name = "排列順序"), DataMember]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 父區段識別碼
    /// </summary>
    [Display(Name = "父區段識別碼"), DataMember]
    public int? ParentId { get; set; }

    /// <summary>
    /// 父區段
    /// </summary>
    [Display(Name = "父區段"), DataMember]
    public UrlSegment Parent { get; set; }

    /// <summary>
    /// 擁有子區段
    /// </summary>
    [Display(Name = "擁有子區段"), DataMember]
    public bool HasChild { get; set; }

    /// <summary>
    /// 子區段
    /// </summary>
    [Display(Name = "子區段"), DataMember]
    public ICollection<UrlSegment> Children { get; set; }

    /// <summary>
    /// 標題
    /// </summary>
    [Display(Name = "標題"), DataMember]
    public MultilingualText Title { get; set; }

    /// <summary>
    /// 鏈結網址
    /// </summary>
    [Display(Name = "鏈結網址"), DataMember]
    [MaxLength(2000)]
    public string LinkUrl { get; set; }

    /// <summary>
    /// 網頁識別碼
    /// </summary>
    [Display(Name = "網頁識別碼"), DataMember]
    public int? PageId { get; set; }

    /// <summary>
    /// 網頁
    /// </summary>
    [Display(Name = "網頁"), DataMember]
    public Page Page { get; set; }

    /// <summary>
    /// 網址
    /// </summary>
    [Display(Name = "網址"), DataMember]
    public string Url {
        get {
            var segments = new List<string>();
            var segment = this;
            do segments.Add(segment.Name);
            while ((segment = segment.Parent) != null);
            segments.Reverse();
            return $"/{string.Join('/', segments)}";
        }
    }
}