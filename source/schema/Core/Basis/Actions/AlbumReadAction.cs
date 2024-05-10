using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 讀取相簿資料之操作。
/// </summary>
[Description("讀取相簿資料")]
public class AlbumReadAction : ReadActionBase<Album, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 建構 PictureAlbumReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public AlbumReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取相簿資料", user, dbContext, trackEnabled) { ; }
}