using System.ComponentModel;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 刪除新聞資料之操作。
/// </summary>
[Description("刪除新聞資料")]
public class PageDeleteAction : DeleteActionBase<Page, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Page];

    /// <summary>
    /// 初始化 PageCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="context">資料脈絡</param>
    public PageDeleteAction(IUser user, DataContext context = null) : base("刪除新聞資料", user, context) {
        SetActionLog<ActionLogCommand<ActionLog, DataContext>>(context);
        RequiredIncludes = ["Content.Texts"];
    }

    protected override void OnDeleting(DataContext context, Page current) {
        if (current.Content != null) context.Remove(current.Content);
    }
}