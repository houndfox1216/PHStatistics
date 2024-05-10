using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Linq;

namespace EmptyProject.Actions;

/// <summary>
/// 刪除角色資料之操作。
/// </summary>
[Description("刪除角色資料")]
public class RoleDeleteAction : DeleteActionBase<Role, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Role };

    /// <summary>
    /// 初始化 RoleDeleteAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public RoleDeleteAction(IUser user, DataContext dbContext = null) : base("刪除角色資料", user, dbContext) {             
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }

    /// <summary>
    /// 當刪除實體資料前執行
    /// </summary>
    /// <param name="context">資料脈絡</param>
    /// <param name="current">目前的實體資料</param>
    /// <param name="parameters">參數</param>
    protected override void OnDeleting(DataContext context, Role current) {
            if (context.User.Any(e => e.UserRoles.Any(e => e.RoleId == current.Id)))
                throw new DataException("多個用戶與此角色關聯，請先將成員清除");
        }
}