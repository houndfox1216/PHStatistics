using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;
using PHStatistics.Content;

namespace PHStatistics.Actions;

/// <summary>
/// 更新課程資料之操作。
/// </summary>
[Description("更新課程資料")]
public class CourseUpdateAction : UpdateActionBase<Course, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Course];

    /// <summary>
    /// 建構 CourseCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public CourseUpdateAction(IUser user, DataContext dbContext = null) : base("更新課程資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            //RequiredIncludes = ["CourseAssignment"];
        }

    //protected override void OnUpdating(DataContext context, Course data, Course current) {
    //    var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.First();
    //}
}