using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Security;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace PHStatistics.Actions;

/// <summary>
/// 讀取用戶資料之操作。
/// </summary>
[Description("讀取用戶資料")]
public class UserReadAction : ReadActionBase<User, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.User };

    /// <summary>
    /// 初始化 UserReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public UserReadAction(IUser user, DataContext dbContext, bool trackEnabled = false) : base("讀取用戶資料", user, dbContext, trackEnabled) {
            RequiredIncludesInFind = new[] { "Photo", "UserRoles.Role" };
            RequiredIncludesInQuery = new[] { "Photo", "UserRoles.Role" };
        }

    /// <summary>
    /// 讀取指定用戶資料
    /// </summary>
    /// <param name="account">帳號</param>
    /// <param name="password">密碼</param>
    public User Authorize(string account, string password) {
            try {
                var query = DataContext.User.AsNoTracking();
                foreach (var item in RequiredIncludesInFind) query = query.Include(item);
                return query.Single(e => e.Account == account && e.Password == password); 
            } catch (Exception e) { throw new SecurityException("用戶帳號或密碼錯誤!",e); }
        }
}