using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;
using PHStatistics.Content;

namespace PHStatistics.Actions;

/// <summary>
/// 更新人數表資料之操作。
/// </summary>
[Description("更新人數表資料")]
public class StudentPopulationUpdateAction : UpdateActionBase<StudentPopulation, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.StudentPopulation];

    /// <summary>
    /// 建構 StudentPopulationCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public StudentPopulationUpdateAction(IUser user, DataContext dbContext = null) : base("更新人數表資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = ["Items"];
        }

    //protected override void OnUpdating(DataContext context, StudentPopulation data, StudentPopulation current) {
    //    var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.First();
    //}
}