using System;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
using EmptyProject.Actions;

// ReSharper disable once CheckNamespace
namespace EmptyProject.Models;

/// <summary>
/// 操作記錄Model
/// </summary>
public class ActionLogModel : HttpModelBase<DataContext> {
    /// <summary>
    /// 建構 ActionLogModel
    /// </summary>
    public ActionLogModel() : base("ActionLog") { }

    /// <summary>
    /// 取得指定識別碼的操作記錄
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    public ActionLog Find(long id, IUser user = null) => new ActionLogReadAction(user ?? this.CurrentUser, this.DataContext).Find(id);

    /// <summary>
    /// 查詢符合指定條件的實體資料
    /// </summary>
    /// <param name="keyword">關鍵字</param>
    /// <param name="condition">查詢條件</param>
    /// <param name="sortings">排序方式</param>
    /// <param name="user">執行操作的用戶</param>
    /// <param name="includes">須積極取回之相關資料</param>
    public IQueryable<ActionLog> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition ??= new Condition();
        if (keyword != null) condition.And("Name", Operator.Contains, keyword);
        return new ActionLogReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
    }

    /// <summary>
    /// 查詢符合指定條件的操作記錄
    /// </summary>
    /// <param name="startTime">開始時間</param>
    /// <param name="endTime">結束時間</param>
    /// <param name="keyword">關鍵字</param>
    /// <param name="condition">查詢條件</param>
    /// <param name="sortings">排序方式</param>
    /// <param name="user">執行操作的用戶</param>
    /// <param name="includes">須積極取回之相關資料</param>
    public IQueryable<ActionLog> Query(DateTime? startTime, DateTime? endTime, string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition ??= new Condition();
        if (startTime != null) condition.And("CreatedTime", Operator.GreaterThanOrEqual, startTime.Value);
        if (endTime != null) condition.And("CreatedTime", Operator.LessThan, endTime.Value);
        if (keyword.HasValue()) condition.And(new Condition(
            new Condition("UserName", Operator.Like, keyword),
            new Condition("ActionName", Operator.Like, keyword, LogicalConnective.Or),
            new Condition("EntityName", Operator.Like, keyword, LogicalConnective.Or),
            new Condition("EntityTypeName", Operator.Like, keyword, LogicalConnective.Or)
        ));
        return new ActionLogReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
    }
}