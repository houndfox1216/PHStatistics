using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;

namespace EmptyProject.Actions;

/// <summary>
/// 刪除類別資訊之操作。
/// </summary>
[Description("刪除類別資訊")]
public class CategoryDeleteAction : DeleteActionBase<Category, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Category };

    /// <summary>
    /// 初始化 NewsCategoryCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public CategoryDeleteAction(IUser user, DataContext dbContext = null) : base("刪除類別資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }
}