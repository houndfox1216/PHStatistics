using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq.Expressions;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 讀取類別資訊之操作。
/// </summary>
[Description("讀取類別資訊")]
public class CategoryReadAction : ReadActionBase<Category, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 NewsCategoryReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public CategoryReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取類別資料", user, dbContext, trackEnabled) { }

    protected override Expression<Func<Category, bool>> BuildPredicate(DataContext context, IList<string> includes, string column, Operator @operator, object value) {
            if (column.Equals("ParentId", StringComparison.OrdinalIgnoreCase)) {
                // 產生 ParentId 條件之 predicate
                return @operator switch {
                    Operator.Equal => context.PredicateTrue<Category>().And(e => e.ParentId == int.Parse(value.ToString())),
                    _ => null,
                };
            } 
            return null;
        }
}