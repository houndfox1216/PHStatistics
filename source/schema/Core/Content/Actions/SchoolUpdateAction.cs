using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;
using PHStatistics.Content;

namespace PHStatistics.Actions;

/// <summary>
/// 更新分校資料之操作。
/// </summary>
[Description("更新分校資料")]
public class SchoolUpdateAction : UpdateActionBase<School, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.School];

    /// <summary>
    /// 建構 SchoolCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public SchoolUpdateAction(IUser user, DataContext dbContext = null) : base("更新分校資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = ["SchoolAssignment"];
        }

    //protected override void OnUpdating(DataContext context, School data, School current) {
    //    var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.First();
    //}
}