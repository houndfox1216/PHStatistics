using System.ComponentModel;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace EmptyProject.Actions;

/// <summary>
/// 刪除新聞資料之操作。
/// </summary>
[Description("刪除新聞資料")]
public class UrlSegmentDeleteAction : DeleteActionBase<UrlSegment, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.UrlSegment];

    /// <summary>
    /// 初始化 UrlSegmentCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="context">資料脈絡</param>
    public UrlSegmentDeleteAction(IUser user, DataContext context = null) : base("刪除新聞資料", user, context) {
        SetActionLog<ActionLogCommand<ActionLog, DataContext>>(context);
        RequiredIncludes = ["Title.Texts"];
    }

    protected override void OnDeleting(DataContext context, UrlSegment current) {
        if (current.Title != null) context.Remove(current.Title);
    }
}