using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;
using PHStatistics.Content;

namespace PHStatistics.Actions;

/// <summary>
/// 更新人數表項目資料之操作。
/// </summary>
[Description("更新人數表項目資料")]
public class StudentPopulationItemLogUpdateAction : UpdateActionBase<StudentPopulationItemLog, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.StudentPopulation];

    /// <summary>
    /// 建構 StudentPopulationItemLogCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public StudentPopulationItemLogUpdateAction(IUser user, DataContext dbContext = null) : base("更新人數表項目資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = ["StudentPopulationItemLogAssignment"];
        }

    //protected override void OnUpdating(DataContext context, StudentPopulationItemLog data, StudentPopulationItemLog current) {
    //    var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.First();
    //}
}