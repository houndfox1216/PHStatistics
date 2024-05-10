using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;

namespace PHStatistics.Actions;

/// <summary>
/// 用以表示讀取屬性值之行為。
/// </summary>
[Description("讀取屬性值")]
public class AttributeValueReadAction : ReadActionBase<AttributeValue, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 AttributeValueReadAction
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public AttributeValueReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取屬性值", user, dbContext, trackEnabled) { ; }
}