using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Framework.Security;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 變更用戶密碼之操作。
/// </summary>
[Description("變更用戶密碼")]
public class UserChangePasswordAction : UpdateActionBase<User, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 UserChangePasswordAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public UserChangePasswordAction(IUser user, DataContext dbContext) : base("變更用戶密碼", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
        }

    /// <summary>
    /// 當更新實體資料前執行
    /// </summary>
    /// <param name="context">資料脈絡</param>
    /// <param name="data">資料範本</param>
    /// <param name="current">目前資料</param>
    protected override void OnUpdating(DataContext context, User data, User current) {
            var oldPassword = Parameters.GetValue<string>("old-password");
            var newPassword = Parameters.GetValue<string>("new-password");

            if (User.Data is not User user || user.Id != current.Id) throw new SecurityException("須用戶本人變更密碼");
            if (current.Password != oldPassword.ComputeHashStringWithSha().ToBase64()) throw new SecurityException("提供的舊密碼不符合");

            current.Password = newPassword.ComputeHashStringWithSha().ToBase64();
            current.PasswordChangedTime = DateTime.Now;
            current.Apply(data);
        }
}