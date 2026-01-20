using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;

namespace PHStatistics.Actions;

/// <summary>
/// 更新圖片資料之操作。
/// </summary>
[Description("更新圖片資料")]
public class PictureUpdateAction : UpdateActionBase<Picture, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Album };

    /// <summary>
    /// 初始化 PictureCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public PictureUpdateAction(IUser user, DataContext dbContext = null) : base("更新圖片資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = new[] { "Album" };
        }

    protected override void OnUpdating(DataContext context, Picture data, Picture current) {
            data.Album = current.Album;
        }
}