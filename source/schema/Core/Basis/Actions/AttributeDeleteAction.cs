using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.IO;

namespace EmptyProject.Actions;

/// <summary>
/// 刪除屬性資料之操作。
/// </summary>
[Description("刪除屬性資料")]
public class AttributeDeleteAction : DeleteActionBase<Attribute, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Attribute };

    /// <summary>
    /// 初始化 AttributeDeleteAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public AttributeDeleteAction(IUser user, DataContext dbContext = null) : base("刪除屬性資料", user, dbContext) { /* nop */ }
}