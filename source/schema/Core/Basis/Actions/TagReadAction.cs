using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;

namespace EmptyProject.Actions;

/// <summary>
/// 讀取標籤資料之操作。
/// </summary>
[Description("讀取標籤資料")]
public class TagReadAction : ReadActionBase<Tag, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 TagReadAction
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public TagReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取標籤資料", user, dbContext, trackEnabled) { }
}