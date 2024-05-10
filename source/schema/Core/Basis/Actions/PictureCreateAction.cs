using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 新增圖片資料
/// </summary>
[Description("新增圖片資料")]
public class PictureCreateAction : CreateActionBase<Picture, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Album };

    /// <summary>
    /// 初始化 PictureCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public PictureCreateAction(IUser user, DataContext dbContext = null) : base("新增圖片資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
        }

    protected override void OnCreating(DataContext context, Picture data) {
            if (data.AlbumId.HasValue()) data.Album = context.Album.Find(data.AlbumId);
            else if (data.Album.HasValue()) data.Album = context.Album.Find(data.Album.Id);
        }
}