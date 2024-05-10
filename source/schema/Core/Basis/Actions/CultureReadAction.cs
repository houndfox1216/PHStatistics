using System.ComponentModel;
using System.Framework;
using System.Framework.Application;

namespace EmptyProject.Actions;

/// <summary>
/// 讀取會員級距之操作。
/// </summary>
[Description("讀取語言資料")]
public class CultureReadAction : ReadActionBase<Culture, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions {
        get { return System.Array.Empty<SystemPermission>(); }
    }

    /// <summary>
    /// 初始化 UserReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public CultureReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取語言資料", user, dbContext, trackEnabled) {
            RequiredIncludesInQuery = RequiredIncludesInFind = new[] { "Picture" };
        }
}