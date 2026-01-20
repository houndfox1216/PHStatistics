using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;

namespace PHStatistics.Actions;

/// <summary>
/// 刪除廣告資料之操作。
/// </summary>
[Description("刪除廣告資料")]
public class BannerDeleteAction : DeleteActionBase<Banner, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Banner];

    /// <summary>
    /// 初始化 BannerCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public BannerDeleteAction(IUser user, DataContext dbContext = null) : base("刪除廣告資料", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);            
        }
}