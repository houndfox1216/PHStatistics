using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;

namespace EmptyProject.Actions;

/// <summary>
/// 新增類別資訊之操作。
/// </summary>
[Description("新增類別資訊")]
public class CategoryCreateAction : CreateActionBase<Category, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Category };

    /// <summary>
    /// 建構 NewsCategoryCreateAction
    /// </summary>
    /// <param name="user">請求操作的會員</param>
    public CategoryCreateAction(IUser user, DataContext dbContext = null) : base("新增類別資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }

    protected override void OnCreating(DataContext context, Category data) {
            if (!data.Ordinal.HasValue) {
                var lastRow = context.Category.OrderByDescending(e => e.Ordinal).FirstOrDefault();
                data.Ordinal = (lastRow != null ? lastRow.Ordinal : 0) + 100;
            }
        }
}