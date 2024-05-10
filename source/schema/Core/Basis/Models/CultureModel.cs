using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
<<<<<<< HEAD
using EmptyProject.Actions;

// ReSharper disable once CheckNamespace
namespace EmptyProject.Models;
=======
using PHStatistics.Actions;

// ReSharper disable once CheckNamespace
namespace PHStatistics.Models;
>>>>>>> origin/develop/schema

public class CultureModel : HttpModelBase<DataContext> {
    /// <summary>
    /// 建構 CultureModel
    /// </summary>
    public CultureModel() : base("Culture") { }

    /// <summary>
    /// 取得指定識別碼的文化特性
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    public Culture Find(string id, IUser user = null) {
        return new CultureReadAction(user ?? this.CurrentUser, this.DataContext).Find(id);
    }

    /// <summary>
    /// 查詢符合指定條件的實體資料
    /// </summary>
    /// <param name="keyword">關鍵字</param>
    /// <param name="condition">查詢條件</param>
    /// <param name="sortings">排序方式</param>
    /// <param name="user">執行操作的用戶</param>
    /// <param name="includes">須積極取回之相關資料</param>
    public IQueryable<Culture> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition ??= new Condition();
        if (keyword.HasValue()) condition.And("Name", Operator.Contains, keyword);
        return new CultureReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
    }
}