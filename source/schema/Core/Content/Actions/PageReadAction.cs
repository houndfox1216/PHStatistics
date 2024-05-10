using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq;
using System.Linq.Expressions;

// ReSharper disable once CheckNamespace
namespace EmptyProject.Actions;

/// <summary>
/// 讀取新聞資料之操作。
/// </summary>
[Description("讀取新聞資料")]
public class PageReadAction : ReadActionBase<Page, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 PageReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="context">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public PageReadAction(IUser user, DataContext context = null, bool trackEnabled = false) : base("讀取新聞資料", user, context, trackEnabled) {
        RequiredIncludesInQuery = ["Content.Texts"];
        RequiredIncludesInFind = ["Content.Texts"];
    }

    protected override Expression<Func<Page, bool>> BuildPredicate(
        DataContext context, IList<string> includes, string column, Operator @operator, object value
    ) {
        if (column.Equals("Content", StringComparison.OrdinalIgnoreCase)) {
            if (!includes.Contains("Content.Texts")) includes.Add("Content.Texts");
            return @operator switch {
                Operator.Equal => context.PredicateTrue<Page>().And(e => e.Content.Texts.Any(i => i.Content == value.ToString())),
                Operator.Contains => context.PredicateTrue<Page>().And(e => e.Content.Texts.Any(i => i.Content.Contains(value.ToString()))),
                _ => null,
            };
        }

        return null;
    }
}