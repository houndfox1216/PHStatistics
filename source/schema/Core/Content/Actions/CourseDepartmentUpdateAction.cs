using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;
using PHStatistics.Content;

namespace PHStatistics.Actions;

/// <summary>
/// 更新班系資料之操作。
/// </summary>
[Description("更新班系資料")]
public class CourseDepartmentUpdateAction : UpdateActionBase<CourseDepartment, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.CourseDepartment];

    /// <summary>
    /// 建構 CourseDepartmentCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public CourseDepartmentUpdateAction(IUser user, DataContext dbContext = null) : base("更新班系資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            //RequiredIncludes = ["CourseDepartmentAssignment"];
        }

    //protected override void OnUpdating(DataContext context, CourseDepartment data, CourseDepartment current) {
    //    var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.First();
    //}
}