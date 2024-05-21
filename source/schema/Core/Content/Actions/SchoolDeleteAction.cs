using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;
using PHStatistics.Content;

namespace PHStatistics.Actions;

/// <summary>
/// 刪除新聞資料之操作。
/// </summary>
[Description("刪除新聞資料")]
public class SchoolDeleteAction : DeleteActionBase<School, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.School];

    /// <summary>
    /// 初始化 SchoolCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public SchoolDeleteAction(IUser user, DataContext dbContext = null) : base("刪除新聞資料", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }

    protected override void OnDeleting(DataContext context, School current) {

        }
}