using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Linq.Expressions;
using System.Linq;
using System.Framework.Data;

namespace EmptyProject.Actions;

/// <summary>
/// 讀取媒體檔案之操作。
/// </summary>
[Description("讀取媒體檔案")]
public class MediaFileReadAction : ReadActionBase<MediaFile, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 建構 MediaFileReadAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="dbContext">資料脈絡</param>
    /// <param name="trackEnabled">啟用追蹤</param>
    public MediaFileReadAction(IUser user, DataContext dbContext = null, bool trackEnabled = false) : base("讀取媒體檔案", user, dbContext, trackEnabled) {
            RequiredIncludesInQuery = new[] { "Title.Texts", "Content.Texts" };
            RequiredIncludesInFind = new[] { "Title.Texts", "Content.Texts" };
        }

    protected override Expression<Func<MediaFile, bool>> BuildPredicate(DataContext context, IList<string> includes, string column, Operator @operator, object value) {
            if (column.Equals("Title", StringComparison.OrdinalIgnoreCase)) {
                if (!includes.Contains("Title.Texts")) includes.Add("Title.Texts");
                return @operator switch {
                    Operator.Equal => context.PredicateTrue<MediaFile>().And(e => e.Title.Texts.Any(i => i.Content  == value.ToString())),
                    Operator.Contains => context.PredicateTrue<MediaFile>().And(e => e.Title.Texts.Any(i => i.Content.Contains(value.ToString()))),
                    _ => null,
                };
            }
            else if (column.Equals("Content", StringComparison.OrdinalIgnoreCase)) {
                if (!includes.Contains("Content.Texts")) includes.Add("Content.Texts");
                return @operator switch {
                    Operator.Equal => context.PredicateTrue<MediaFile>().And(e => e.Content.Texts.Any(i => i.Content  == value.ToString())),
                    Operator.Contains => context.PredicateTrue<MediaFile>().And(e => e.Content.Texts.Any(i => i.Content.Contains(value.ToString()))),
                    _ => null,
                };
            }
            else if (column.Equals("IsReleaseNow", StringComparison.OrdinalIgnoreCase)) {
                var now = DateTime.Now;
                return @operator switch {
                    Operator.Equal => context.NewPredicate<MediaFile>(e => e.Published && e.StartTime <= now && e.EndTime > now ),
                    _ => throw new OperationException($"不接受 {column} 的 {@operator} 查詢方式!")
                };
            }
            return null;
        }
}