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
/// 新增廣告資料
/// </summary>
[Description("新增廣告資料")]
public class BannerCreateAction : CreateActionBase<Banner, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Banner];

    /// <summary>
    /// 初始化 BannerCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public BannerCreateAction(IUser user, DataContext dbContext = null) : base("新增廣告資料", user, dbContext) { ; }

    protected override void OnCreating(DataContext context, Banner data) {
            if (data.PositionId.HasValue()) data.Position = context.BannerPosition.Find(data.PositionId);
            else if (data.Position.HasValue()) data.Position = context.BannerPosition.Find(data.Position.Id);
            else throw new OperationException("未提供位置相關資訊");
        }
}