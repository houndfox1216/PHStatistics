using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;
using PHStatistics.Content;

namespace PHStatistics.Actions;

/// <summary>
/// 刪除課程資料之操作。
/// </summary>
[Description("刪除課程資料")]
public class CourseDeleteAction : DeleteActionBase<Course, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Course];

    /// <summary>
    /// 初始化 CourseCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public CourseDeleteAction(IUser user, DataContext dbContext = null) : base("刪除課程資料", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }

    protected override void OnDeleting(DataContext context, Course current) {

        }
}