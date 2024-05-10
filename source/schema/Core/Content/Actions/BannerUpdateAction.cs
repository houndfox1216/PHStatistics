using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.IO;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 更新廣告資料之操作。
/// </summary>
[Description("更新廣告資料")]
public class BannerUpdateAction : UpdateActionBase<Banner, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Banner];

    /// <summary>
    /// 初始化 BannerCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public BannerUpdateAction(IUser user, DataContext dbContext = null) : base("更新廣告資料", user, dbContext) { /* nop */ }
}