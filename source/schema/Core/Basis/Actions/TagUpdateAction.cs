using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;

namespace PHStatistics.Actions;

/// <summary>
/// 更新標籤資料之操作
/// </summary>
[Description("更新標籤資料")]
public class TagUpdateAction : UpdateActionBase<Tag, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Tag };

    /// <summary>
    /// 初始化 TagCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public TagUpdateAction(IUser user, DataContext dbContext = null) : base("更新標籤資料", user, dbContext) {             
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }
}