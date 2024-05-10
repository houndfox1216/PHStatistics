using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;

namespace EmptyProject.Actions;

/// <summary>
/// 更新包裝內容物之操作。
/// </summary>
[Description("更新包裝內容物")]
public class PackedContentUpdateAction : UpdateActionBase<PackedContent, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Product };

    /// <summary>
    /// 建構 PackedContentCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public PackedContentUpdateAction(IUser user, DataContext dbContext = null) : base("更新包裝內容物", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
        }

    protected override void OnUpdating(DataContext context, PackedContent data, PackedContent current) {
            if (!data.ProductId.HasValue() || context.Product.Find(data.ProductId) is not Product) throw new OperationException("內容物為必填");
            data.Product = null;
            if (!data.ContentId.HasValue() || context.Product.Find(data.ContentId) is not Product) throw new OperationException("未選擇內容物");
            data.Content = null;
        }
}