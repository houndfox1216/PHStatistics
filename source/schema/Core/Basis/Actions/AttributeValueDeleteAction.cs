using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.IO;

namespace EmptyProject.Actions;

/// <summary>
/// 刪除屬性值之操作。
/// </summary>
[Description("刪除屬性值")]
public class AttributeValueDeleteAction : DeleteActionBase<AttributeValue, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Attribute };

    /// <summary>
    /// 初始化 AttributeValueDeleteAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public AttributeValueDeleteAction(IUser user, DataContext dbContext = null) : base("刪除屬性值", user, dbContext) { /* nop */ }
}