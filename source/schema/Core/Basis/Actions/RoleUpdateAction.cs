using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 更新角色資料之操作
/// </summary>
[Description("更新角色資料")]
public class RoleUpdateAction : UpdateActionBase<Role, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Role };

    /// <summary>
    /// 初始化 RoleCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public RoleUpdateAction(IUser user, DataContext dbContext = null) : base("修改角色資料", user, dbContext) {             
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }

    /// <summary>
    /// 當實體資料更新前執行
    /// </summary>
    /// <param name="context">資料脈絡</param>
    /// <param name="data">資料範本</param>
    /// <param name="current">目前資料</param>
    protected override void OnUpdating(DataContext context, Role data, Role current) {
            data.PermissionValue ??= current.PermissionValue;
        }
}