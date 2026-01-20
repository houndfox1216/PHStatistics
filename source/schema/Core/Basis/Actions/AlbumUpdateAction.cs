using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;

namespace PHStatistics.Actions;

/// <summary>
/// 更新相簿資料之操作。
/// </summary>
[Description("更新相簿資料")]
public class AlbumUpdateAction : UpdateActionBase<Album, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Album };

    /// <summary>
    /// 建構 PictureAlbumUpdateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public AlbumUpdateAction(IUser user, DataContext dbContext = null) : base("更新相簿資料", user, dbContext) {
        SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
    }

    /// <summary>
    /// 當更新實體資料前執行
    /// </summary>
    /// <param name="context">資料脈絡</param>
    /// <param name="data">資料範本</param>
    /// <param name="current">目前資料</param>
    /// <param name="parameters">參數</param>
    protected override void OnUpdating(DataContext context, Album data, Album current) {
        if (data.Cover.HasValue()) {
            current.Cover = context.Picture.Find(data.Cover.Id);
            current.Cover.Uri = data.Cover.Uri;
            current.Cover.Type = data.Cover.Type;
        }
    }
}