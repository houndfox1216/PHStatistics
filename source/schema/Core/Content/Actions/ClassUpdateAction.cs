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
public class ClassUpdateAction : UpdateActionBase<Class, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Class];

    /// <summary>
    /// 建構 ClassCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public ClassUpdateAction(IUser user, DataContext dbContext = null) : base("更新班系資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            //RequiredIncludes = ["ClassAssignment"];
        }

    //protected override void OnUpdating(DataContext context, Class data, Class current) {
    //    var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.First();
    //}
}