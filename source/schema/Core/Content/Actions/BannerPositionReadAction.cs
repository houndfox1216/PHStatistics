using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace EmptyProject.Actions;

/// <summary>
/// 用以表示讀取廣告位置之行為。
/// </summary>
[Description("讀取廣告位置")]
public class BannerPositionReadAction : ReadActionBase<BannerPosition, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 BannerPositionReadAction
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public BannerPositionReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取廣告位置", user, dbContext, trackEnabled) { ; }

    public BannerPosition FindByCode(string code) {
            return DataContext.BannerPosition.Include("Banners").SingleOrDefault(e => e.Code == code);
        }
}