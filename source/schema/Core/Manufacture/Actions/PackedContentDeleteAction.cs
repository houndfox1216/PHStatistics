using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;

namespace EmptyProject.Actions;

/// <summary>
/// 刪除包裝內容物之操作。
/// </summary>
[Description("刪除包裝內容物")]
public class PackedContentDeleteAction : DeleteActionBase<PackedContent, DataContext, SystemPermission> {
     
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Product };

    /// <summary>
    /// 初始化 PackedContentCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public PackedContentDeleteAction(IUser user, DataContext dbContext = null) : base("刪除包裝內容物", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }
}