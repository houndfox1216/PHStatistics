using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;

namespace EmptyProject.Actions;

/// <summary>
/// 用以表示讀取角色資料之行為。
/// </summary>
[Description("讀取角色資料")]
public class RoleReadAction : ReadActionBase<Role, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 RoleReadAction
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public RoleReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取角色資料", user, dbContext, trackEnabled) { }
}