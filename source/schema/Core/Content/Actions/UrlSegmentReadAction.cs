using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq;
using System.Linq.Expressions;

// ReSharper disable once CheckNamespace
<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 讀取新聞資料之操作。
/// </summary>
[Description("讀取新聞資料")]
public class UrlSegmentReadAction : ReadActionBase<UrlSegment, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 UrlSegmentReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>ㄑ
    public UrlSegmentReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取新聞資料", user, dbContext, trackEnabled) {
        RequiredIncludesInQuery = new[] { "Title.Texts" };
        RequiredIncludesInFind = new[] { "Title.Texts" };
    }

    protected override Expression<Func<UrlSegment, bool>> BuildPredicate(
        DataContext context, IList<string> includes, string column, Operator @operator, object value
    ) {
        if (column.Equals("Title", StringComparison.OrdinalIgnoreCase)) {
            if (!includes.Contains("Title.Texts")) includes.Add("Title.Texts");
            return @operator switch {
                Operator.Equal => context.PredicateTrue<UrlSegment>().And(e => e.Title.Texts.Any(i => i.Content == value.ToString())),
                Operator.Contains => context.PredicateTrue<UrlSegment>().And(e => e.Title.Texts.Any(i => i.Content.Contains(value.ToString()))),
                _ => null,
            };
        }

        return null;
    }
}