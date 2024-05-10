using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;

namespace EmptyProject.Actions;

/// <summary>
/// 讀取包裝內容物之操作。
/// </summary>
[Description("讀取包裝內容物")]
public class PackedContentReadAction : ReadActionBase<PackedContent, DataContext, SystemPermission> {
    private DateTime now = DateTime.UtcNow.ToTaipeiTime();

    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 PackedContentReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public PackedContentReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取包裝內容物", user, dbContext, trackEnabled) {
            RequiredIncludesInFind = new[] { "Product", "Content" };
            RequiredIncludesInQuery = new[] { "Product", "Content" };
        }
}