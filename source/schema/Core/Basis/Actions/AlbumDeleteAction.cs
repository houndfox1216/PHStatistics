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
/// 刪除相簿資料之操作。
/// </summary>
[Description("刪除相簿資料")]
public class AlbumDeleteAction : DeleteActionBase<Album, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Album };

    /// <summary>
    /// 建構 PictureAlbumDeleteAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public AlbumDeleteAction(IUser user, DataContext dbContext = null) : base("刪除相簿資料", user, dbContext) {             
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }

    protected override void OnDeleted(DataContext context, Album current) {
            var directory = Path.Combine(Environment.Directory.WebRootPath, "files", "albums", current.Number);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
}