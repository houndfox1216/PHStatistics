using System.ComponentModel;
using System.Framework;
using System.Framework.Application;

namespace EmptyProject.Actions;

/// <summary>
/// 更新屬性資料之操作
/// </summary>
[Description("更新屬性資料")]
public class AttributeUpdateAction : UpdateActionBase<Attribute, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Attribute };

    /// <summary>
    /// 初始化 AttributeCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public AttributeUpdateAction(IUser user, DataContext dbContext = null) : base("更新屬性資料", user, dbContext) { /* nop */ }
}