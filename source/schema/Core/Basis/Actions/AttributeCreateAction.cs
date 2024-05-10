using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Linq;

namespace PHStatistics.Actions;

/// <summary>
/// 新增屬性資料之操作。
/// </summary>
[Description("新增屬性資料")]
public class AttributeCreateAction : CreateActionBase<Attribute, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Attribute };

    /// <summary>
    /// 初始化 AttributeCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public AttributeCreateAction(IUser user, DataContext dbContext = null) : base("新增屬性資料", user, dbContext) { }

    protected override void OnCreating(DataContext context, Attribute data) {
            if (!data.Ordinal.HasValue) {
                var lastRow = context.Attribute.OrderByDescending(e => e.Ordinal).FirstOrDefault();
                data.Ordinal = (lastRow != null ? lastRow.Ordinal : 0) + 100;
            }
        }
}