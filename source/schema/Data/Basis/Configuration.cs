using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework;
using System.Framework.Application;
using System.Linq;

// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable once CheckNamespace
namespace PHStatistics;

/// <summary>
/// 系統配置
/// </summary>
[Description("系統配置")]
public sealed class Configuration : ConfigurationBase {
    #region 前台網站

    /// <summary>
    /// 入口網站標題
    /// </summary>
    [Display(Name = "入口網站標題")]
    public string PortalTitle { get; set; }

    /// <summary>
    /// 入口網站關鍵字
    /// </summary>
    [Display(Name = "入口網站關鍵字")]
    public string PortalMetaKeywords { get; set; }

    /// <summary>
    /// 入口網站描述
    /// </summary>
    [Display(Name = "入口網站描述")]
    public string PortalMetaDescription { get; set; }

    /// <summary>
    /// 服務電話
    /// </summary>
    [Display(Name = "服務電話")]
    public string ServicePhone { get; set; }

    /// <summary>
    /// 公司傳真
    /// </summary>
    [Display(Name = "公司傳真")]
    public string CompanyFax { get; set; }

    /// <summary>
    /// 公司地址
    /// </summary>
    [Display(Name = "公司地址")]
    public string CompanyAddress { get; set; }

    /// <summary>
    /// 服務信箱
    /// </summary>
    [Display(Name = "服務信箱")]
    public string ServiceEmail { get; set; }

    /// <summary>
    /// Facebook
    /// </summary>
    [Display(Name = "Facebook")]
    public string Facebook { get; set; }

    /// <summary>
    /// Instagram
    /// </summary>
    [Display(Name = "Instagram")]
    public string Instagram { get; set; }

    /// <summary>
    /// Twitter
    /// </summary>
    [Display(Name = "Twitter")]
    public string Twitter { get; set; }

    /// <summary>
    /// LINE
    /// </summary>
    [Display(Name = "LINE")]
    public string Line { get; set; }

    /// <summary>
    /// 追蹤代碼
    /// </summary>
    [Display(Name = "TrackingCode")]
    public string TrackingCode { get; set; }

    #endregion

    #region 後台管理

    /// <summary>
    /// 後台網站標題
    /// </summary>
    [Display(Name = "後台網站標題")]
    public string AdminTitle { get; set; }

    #endregion

    /// <summary>
    /// 建構 Configuration
    /// </summary>
    public Configuration() : base(null, "cloudfun://PHStatistics/configuration") {
        AdminTitle = "PHStatistics後台";
        PortalTitle = "PHStatistics";
        PortalMetaKeywords = "empty,project,sample,prototype";
        PortalMetaDescription = "PHStatistics入口網站";
        ServicePhone = "+886-7-226-9166";
        CompanyFax = "+886-7-226-9266";
        CompanyAddress = "800 高雄市新興區民生一路56號23F-1";
        ServiceEmail = "service@cloudfun.com.tw";
        Facebook = "https://www.facebook.com/cloudfun.tw/";
        Instagram = "";
        Twitter = "";
        Line = "";
        TrackingCode = "";
    }

    /// <summary>
    /// 重新載入
    /// </summary>
    public override void Reload<TConfiguration>() {
        if (Uri == null) return;
        if (!Uri.StartsWith("cloudfun://")) {
            base.Reload<Configuration>();
        } else {
            using var context = new DataContext();
            if (!context.Resource.Any(e => e.Uri == Uri && (!LastReloadTime.HasValue || e.UpdatedTime > LastReloadTime.Value))) {
                LastReloadTime = DateTime.Now;
                return;
            }

            var resource = context.Resource.Find(Uri);
            if (resource is { Content: not null }) {
                using var configuration = resource.Content.FromXml<Configuration>();
                Apply(configuration);
                LastReloadTime = DateTime.Now;
            } else {
                Persist();
            }
        }
    }

    /// <summary>
    /// 將此配置進行留存
    /// </summary>
    public override void Persist() {
        if (Uri == null) return;
        if (!Uri.StartsWith("cloudfun://")) {
            base.Persist();
        } else {
            using var context = new DataContext();
            var resource = context.Resource.Find(Uri) ?? context.Add(new Resource { Uri = Uri, Name = "系統配置" });
            resource.Content = this.ToXml();
            context.SaveChanges();
        }
    }
}