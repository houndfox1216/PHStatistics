using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.IO;

namespace EmptyProject.Actions;

/// <summary>
/// 刪除廣告位置之操作。
/// </summary>
[Description("刪除廣告位置")]
public class BannerPositionDeleteAction : DeleteActionBase<BannerPosition, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Banner];

    /// <summary>
    /// 初始化 BannerPositionDeleteAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public BannerPositionDeleteAction(IUser user, DataContext dbContext = null) : base("刪除廣告位置", user, dbContext) { /* nop */ }

    protected override void OnDeleted(DataContext context, BannerPosition current) {
            var directory = Path.Combine(Environment.Directory.WebRootPath, "files", "banners", current.Id.ToString());
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
}