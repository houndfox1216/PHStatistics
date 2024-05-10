using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;

namespace PHStatistics.Actions;

/// <summary>
/// 刪除標籤資料之操作。
/// </summary>
[Description("刪除標籤資料")]
public class TagDeleteAction : DeleteActionBase<Tag, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Tag };

    /// <summary>
    /// 初始化 TagDeleteAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public TagDeleteAction(IUser user, DataContext dbContext = null) : base("刪除標籤資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
        }
}