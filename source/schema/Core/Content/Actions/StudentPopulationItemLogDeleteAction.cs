using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;
using PHStatistics.Content;

namespace PHStatistics.Actions;

/// <summary>
/// 刪除人數表項目資料之操作。
/// </summary>
[Description("刪除人數表項目資料")]
public class StudentPopulationItemLogDeleteAction : DeleteActionBase<StudentPopulationItemLog, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.StudentPopulation];

    /// <summary>
    /// 初始化 StudentPopulationItemLogCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public StudentPopulationItemLogDeleteAction(IUser user, DataContext dbContext = null) : base("刪除人數表項目資料", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }

    protected override void OnDeleting(DataContext context, StudentPopulationItemLog current) {

        }
}