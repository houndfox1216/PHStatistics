using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;

namespace EmptyProject.Actions;

/// <summary>
/// 新增標籤資料之操作。
/// </summary>
[Description("新增標籤資料")]
public class TagCreateAction : CreateActionBase<Tag, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Tag };

    /// <summary>
    /// 初始化 TagCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public TagCreateAction(IUser user, DataContext dbContext = null) : base("新增標籤資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
        }

    protected override void OnCreating(DataContext context, Tag data) {
            if (!data.Ordinal.HasValue) {
                var lastRow = context.Tag.OrderByDescending(e => e.Ordinal).FirstOrDefault();
                data.Ordinal = (lastRow != null ? lastRow.Ordinal : 0) + 100;
            }
        }
}