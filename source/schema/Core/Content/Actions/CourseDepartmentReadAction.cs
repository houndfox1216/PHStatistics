using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq;
using System.Linq.Expressions;
using PHStatistics.Content;

namespace PHStatistics.Actions;

/// <summary>
/// 讀取班系資料之操作。
/// </summary>
[Description("讀取班系資料")]
public class CourseDepartmentReadAction : ReadActionBase<CourseDepartment, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 CourseDepartmentReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public CourseDepartmentReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取班系資料", user, dbContext, trackEnabled) {
            //RequiredIncludesInQuery = new[] { "CourseDepartmentAssignment"};
            //RequiredIncludesInFind = new[] { "CourseDepartmentAssignment" };
        }

    protected override Expression<Func<CourseDepartment, bool>> BuildPredicate(DataContext context, IList<string> includes, string column, Operator @operator, object value) {
            if (column.Equals("Name", StringComparison.OrdinalIgnoreCase)) {
                return @operator switch {
                    Operator.Equal => context.PredicateTrue<CourseDepartment>().And(e => e.Name == value.ToString()),
                    Operator.Contains => context.PredicateTrue<CourseDepartment>().And(e => e.Name.Contains(value.ToString())),
                    _ => null,
                };
            } 
            return null;
        }
}