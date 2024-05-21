using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq;
using System.Linq.Expressions;
using PHStatistics.Community;

namespace PHStatistics.Actions;

/// <summary>
/// 讀取新聞資料之操作。
/// </summary>
[Description("讀取新聞資料")]
public class MemberReadAction : ReadActionBase<Member, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 MemberReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public MemberReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取新聞資料", user, dbContext, trackEnabled) {
            RequiredIncludesInQuery = new[] { "Person", "Photo" };
            RequiredIncludesInFind = new[] { "Person", "Photo" };
        }

    protected override Expression<Func<Member, bool>> BuildPredicate(DataContext context, IList<string> includes, string column, Operator @operator, object value) {
            if (column.Equals("Name", StringComparison.OrdinalIgnoreCase)) {
                if (!includes.Contains("Person")) includes.Add("Person");
                return @operator switch {
                    Operator.Equal => context.PredicateTrue<Member>().And(e => e.Person.Name == value.ToString()),
                    Operator.Contains => context.PredicateTrue<Member>().And(e => e.Person.Name.Contains(value.ToString())),
                    _ => null,
                };
            } 
            return null;
        }
}