using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq.Expressions;

namespace EmptyProject.Actions;

/// <summary>
/// 讀取圖片資料之操作。
/// </summary>
[Description("讀取圖片資料")]
public class PictureReadAction : ReadActionBase<Picture, DataContext, SystemPermission> {
    private DateTime now = DateTime.UtcNow.ToTaipeiTime();

    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 UserReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public PictureReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取圖片資料", user, dbContext, trackEnabled) { }


    protected override Expression<Func<Picture, bool>> BuildPredicate(DataContext context, IList<string> includes, string column, Operator @operator, object value) {
            if (column.Equals("IsReleaseNow", StringComparison.OrdinalIgnoreCase)) {
                this.now = DateTime.UtcNow.ToTaipeiTime();
                return @operator switch
                {
                    Operator.Equal => context.NewPredicate<Picture>(e => e.Published && (!e.StartDate.HasValue || e.StartDate <= now) && (!e.EndDate.HasValue || e.EndDate >= now)),
                    _ => throw new OperationException($"不接受 {column} 的 {@operator} 查詢方式!")
                };
            }
            return null;
        }
}