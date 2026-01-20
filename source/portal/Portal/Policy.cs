using System.Framework;
using System.Framework.Application;
using System.Framework.Content;
using System.Framework.Globalization;
using System.Framework.Web;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PHStatistics;

namespace PHStatistics.Portal;

/// <summary>
/// 安全策略
/// </summary>
/// <param name="application">應用程式</param>
public class Policy(IWebApplication application) : System.Framework.Web.Policy(application) {
    public override SitemapNode ModifySitemap<TDataContext>(HttpContext context) {
        var sitemap = Sitemap.Clone();
        if (context.DataContext<TDataContext>() is not DataContext dataContext) return sitemap;
        var segments = dataContext.UrlSegment.AsNoTracking().Include("Title.Texts").Include("Children.Title.Texts");
        var segment = segments.SingleOrDefault(e => e.ParentId == null && e.Name == "custom");
        if (segment == null) return sitemap;
        segment.Children = segment.Children.OrderBy(e => e.Ordinal).ToList();
        var node = segment.ToSitemapNode(context.GetCulture().GetCode());
        if (sitemap.SubNodes.Length == 2) sitemap.SubNodes = [sitemap.SubNodes[0], node, sitemap.SubNodes[1]];
        else sitemap.SubNodes[1] = node;
        return sitemap;
    }
}