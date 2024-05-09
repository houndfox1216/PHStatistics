using System.ComponentModel;
using System.Framework;
using System.Framework.Application;

namespace EmptyProject.Actions;

/// <summary>
/// 新增屬性值之操作。
/// </summary>
[Description("新增屬性值")]
public class AttributeValueCreateAction : CreateActionBase<AttributeValue, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Attribute };

    /// <summary>
    /// 初始化 AttributeValueCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public AttributeValueCreateAction(IUser user, DataContext dbContext = null) : base("新增屬性值", user, dbContext) { }
}