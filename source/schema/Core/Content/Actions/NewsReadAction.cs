using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq;
using System.Linq.Expressions;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 讀取新聞資料之操作。
/// </summary>
[Description("讀取新聞資料")]
public class NewsReadAction : ReadActionBase<News, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 NewsReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public NewsReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取新聞資料", user, dbContext, trackEnabled) {
            RequiredIncludesInQuery = new[] { "Picture.Images", "Title.Texts", "Introduction.Texts", "Content.Texts", "NewsTags" };
            RequiredIncludesInFind = new[] { "Picture.Images", "Title.Texts", "Introduction.Texts", "Content.Texts", "NewsTags" };
        }

    protected override Expression<Func<News, bool>> BuildPredicate(DataContext context, IList<string> includes, string column, Operator @operator, object value) {
            if (column.Equals("Title", StringComparison.OrdinalIgnoreCase)) {
                if (!includes.Contains("Title.Texts")) includes.Add("Title.Texts");
                return @operator switch {
                    Operator.Equal => context.PredicateTrue<News>().And(e => e.Title.Texts.Any(i => i.Content  == value.ToString())),
                    Operator.Contains => context.PredicateTrue<News>().And(e => e.Title.Texts.Any(i => i.Content.Contains(value.ToString()))),
                    _ => null,
                };
            } else if (column.Equals("Introduction", StringComparison.OrdinalIgnoreCase)) {
                if (!includes.Contains("Introduction.Texts")) includes.Add("Introduction.Texts");
                return @operator switch {
                    Operator.Equal => context.PredicateTrue<News>().And(e => e.Introduction.Texts.Any(i => i.Content == value.ToString())),
                    Operator.Contains => context.PredicateTrue<News>().And(e => e.Introduction.Texts.Any(i => i.Content.Contains(value.ToString()))),
                    _ => null,
                };
            } else if (column.Equals("Content", StringComparison.OrdinalIgnoreCase)) {
                if (!includes.Contains("Content.Texts")) includes.Add("Content.Texts");
                return @operator switch {
                    Operator.Equal => context.PredicateTrue<News>().And(e => e.Content.Texts.Any(i => i.Content == value.ToString())),
                    Operator.Contains => context.PredicateTrue<News>().And(e => e.Content.Texts.Any(i => i.Content.Contains(value.ToString()))),
                    _ => null,
                };
            }
            return null;
        }
}