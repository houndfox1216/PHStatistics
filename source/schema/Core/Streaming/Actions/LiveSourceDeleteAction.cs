using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;

namespace EmptyProject.Actions;

/// <summary>
/// 刪除直播來源之操作。
/// </summary>
[Description("刪除直播來源")]
public class LiveSourceDeleteAction : DeleteActionBase<LiveSource, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.LiveSource };

    /// <summary>
    /// 建構 LiveSourceDeleteAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public LiveSourceDeleteAction(IUser user, DataContext dbContext = null) : base("刪除直播來源", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = new[] { "Title.Texts", "Content.Texts" };
        }

    protected override void OnDeleting(DataContext context, LiveSource current) {
            if (current.Title != null) context.Remove(current.Title);
            if (current.Content != null) context.Remove(current.Content);
        }
}