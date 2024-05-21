using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;
using PHStatistics.Content;

namespace PHStatistics.Actions;

/// <summary>
/// 刪除班系資料之操作。
/// </summary>
[Description("刪除班系資料")]
public class CourseDepartmentDeleteAction : DeleteActionBase<CourseDepartment, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.CourseDepartment];

    /// <summary>
    /// 初始化 CourseDepartmentCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public CourseDepartmentDeleteAction(IUser user, DataContext dbContext = null) : base("刪除班系資料", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }

    protected override void OnDeleting(DataContext context, CourseDepartment current) {

        }
}