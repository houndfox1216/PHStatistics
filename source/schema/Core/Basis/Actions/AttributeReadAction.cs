using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Linq;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 用以表示讀取屬性資料之行為。
/// </summary>
[Description("讀取屬性資料")]
public class AttributeReadAction : ReadActionBase<Attribute, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 AttributeReadAction
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public AttributeReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取屬性資料", user, dbContext, trackEnabled) {
            RequiredIncludesInFind = new[] { "Values" };
        }

    public Attribute FindByCode(string code) {
            return DataContext.Attribute.SingleOrDefault(e => e.Code == code);
        }
}