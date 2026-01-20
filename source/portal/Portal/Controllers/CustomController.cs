using System;
using System.Framework;
using System.Framework.Web;
using System.Linq;
using PHStatistics.Portal.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using PHStatistics.Content;

// ReSharper disable RedundantOverriddenMember

namespace PHStatistics.Portal.Controllers;

public class CustomController() : MvcController<PortalUser, Model, Culture, UrlSegment>("System") {
    private UrlSegment _urlSegment;

    protected override UrlSegment UrlSegment => _urlSegment ??= GetUrlSegmentFromUrl(Request.GetEncodedPathAndQuery());

    private UrlSegment GetUrlSegmentFromUrl(string url) {
        if (!url.HasValue()) return null;
        var path = url.IndexOf('?') > 0 ? url[..url.IndexOf('?')] : url;
        path = path.ToLower();
        var nodes = Model.DataContext.UrlSegment
            .Include("Page.Content.Texts")
            .Where(e => path.Contains(e.Name.ToLower()))
            .ToList();
        var result = (from node in nodes where node.Url == path select (_urlSegment = node)).FirstOrDefault();
        return result;
    }

    protected override string OnPageError(string content, Exception error) {
        return base.OnPageError(content, error);
    }

    protected override void OnPageInit(PageInitEventArgs eventArgs) {
        base.OnPageInit(eventArgs);
    }
}