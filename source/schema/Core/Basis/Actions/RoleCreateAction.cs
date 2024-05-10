using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;

namespace PHStatistics.Actions;

/// <summary>
/// 新增角色資料之操作。
/// </summary>
[Description("新增角色資料")]
public class RoleCreateAction : CreateActionBase<Role, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Role };

    /// <summary>
    /// 初始化 RoleCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public RoleCreateAction(IUser user, DataContext dbContext = null) : base("新增角色資料", user, dbContext) {             
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }
}