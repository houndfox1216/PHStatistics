using System.Collections.Generic;
using System.ComponentModel;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;

// ReSharper disable once CheckNamespace
<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 新增新聞資料之操作。
/// </summary>
[Description("新增新聞資料")]
public class PageCreateAction : CreateActionBase<Page, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Page];

    /// <summary>
    /// 建構 PageCreateAction
    /// </summary>
    /// <param name="user">請求操作的會員</param>
    /// <param name="context">資料脈絡</param>
    public PageCreateAction(IUser user, DataContext context = null) : base("新增新聞資料", user, context) {
        SetActionLog<ActionLogCommand<ActionLog, DataContext>>(context);
    }

    protected override void OnCreating(DataContext context, Page data) {
        data.Content ??= new MultilingualText { Texts = new HashSet<StringResource>() };

        var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.OrderBy(e => e.Ordinal).FirstOrDefault();

        var defaultText = data.Content.Texts.FirstOrDefault();
        if (defaultCulture != null) defaultText = data.Content.Texts.FirstOrDefault(e => e.Culture == defaultCulture.Id);
        data.Content.DefaultText = defaultText?.Content;
    }
}