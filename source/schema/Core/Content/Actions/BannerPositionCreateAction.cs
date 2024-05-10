using System.ComponentModel;
using System.Framework;
using System.Framework.Application;

namespace PHStatistics.Actions;

/// <summary>
/// 新增廣告位置之操作。
/// </summary>
[Description("新增廣告位置")]
public class BannerPositionCreateAction : CreateActionBase<BannerPosition, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Banner];

    /// <summary>
    /// 初始化 BannerPositionCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public BannerPositionCreateAction(IUser user, DataContext dbContext = null) : base("新增廣告位置", user, dbContext) { }
}