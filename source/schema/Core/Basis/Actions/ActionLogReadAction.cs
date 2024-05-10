using System.ComponentModel;
using System.Framework;
using System.Framework.Application;

namespace PHStatistics.Actions;

/// <summary>
/// 讀取操作記錄
/// </summary>
[Description("讀取操作記錄")]
public class ActionLogReadAction : ReadActionBase<ActionLog, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.ActionLog };

    /// <summary>
    /// 初始化 ActionLogReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public ActionLogReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取操作記錄", user, dbContext, trackEnabled) { ; }
}