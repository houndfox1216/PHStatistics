using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Framework.Globalization;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace EmptyProject.Actions;

/// <summary>
/// 新增新聞資料之操作。
/// </summary>
[Description("新增新聞資料")]
public class NewsCreateAction : CreateActionBase<News, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.News];

    /// <summary>
    /// 建構 NewsCreateAction
    /// </summary>
    /// <param name="user">請求操作的會員</param>
    /// <param name="context">資料脈絡ㄑ</param>
    public NewsCreateAction(IUser user, DataContext context = null) : base("新增新聞資料", user, context) {
        SetActionLog<ActionLogCommand<ActionLog, DataContext>>(context);
    }

    protected override void OnCreating(DataContext context, News data) {
        if (!data.Ordinal.HasValue) {
            var lastRow = context.News.OrderByDescending(e => e.Ordinal).FirstOrDefault();
            data.Ordinal = (lastRow != null ? lastRow.Ordinal : 0) + 100;
        }

        data.Picture ??= new MultilingualImage { Images = new HashSet<Picture>() };
        data.Title ??= new MultilingualText { Texts = new HashSet<StringResource>() };
        data.Introduction ??= new MultilingualText { Texts = new HashSet<StringResource>() };
        data.Content ??= new MultilingualText { Texts = new HashSet<StringResource>() };

        var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault)?.GetCode() ?? 
                             context.Culture.OrderBy(e => e.Ordinal).FirstOrDefault()?.GetCode();

        data.Picture.DecideDefaultImage(defaultCulture);
        data.Title.DefaultText = data.Title.DecideDefaultText(defaultCulture);
        data.Introduction.DefaultText = data.Introduction.DecideDefaultText(defaultCulture);
        data.Content.DefaultText = data.Content.DecideDefaultText(defaultCulture);

        if (!data.Title.DefaultText.HasValue()) throw new OperationException("未提供預設標題");
        if (!data.Content.DefaultText.HasValue()) throw new OperationException("未提供預設內容");

        foreach (var id in data.TagIds) {
            if (context.Tag.Find(id) is { } tag) context.NewsTag.Add(new NewsTag { News = data, Tag = tag });
        }
    }
}