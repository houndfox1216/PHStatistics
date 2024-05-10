using System.ComponentModel;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Linq;

// ReSharper disable once CheckNamespace
<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 新增相簿資料
/// </summary>
[Description("新增相簿資料")]
public class AlbumCreateAction : CreateActionBase<Album, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Album };

    /// <summary>
    /// 初始化 PictureAlbumCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    public AlbumCreateAction(IUser user, DataContext dbContext = null) : base("新增相簿資料", user, dbContext) {
        SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
    }

    /// <summary>
    /// 當 Transaction 前執行
    /// </summary>
    /// <param name="context">資料脈絡</param>
    /// <param name="data">資料範本</param>
    protected override void OnTransacting(DataContext context, Album data) {
        data.Number = new SequenceGenerateCommand<Sequence>(context).Generate(
            EntityType.PictureGallery, null, null, "{3:yyMM}{4:D4}", SequenceResetType.Monthly
        );
    }

    /// <summary>
    /// 當創建實體資料前執行
    /// </summary>
    /// <param name="context">資料脈絡</param>
    /// <param name="data">資料範本</param>
    protected override void OnCreating(DataContext context, Album data) {
        data.Cover ??= new Picture();
        if (data.Ordinal.HasValue) return;
        var lastRow = context.Album.OrderByDescending(e => e.Ordinal).FirstOrDefault();
        data.Ordinal = (lastRow != null ? lastRow.Ordinal : 0) + 100;
    }
}