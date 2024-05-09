using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
using EmptyProject.Actions;

// ReSharper disable once CheckNamespace
namespace EmptyProject.Models;

/// <summary>
/// 新聞資料模型
/// </summary>
public class PageModel : HttpModelBase<DataContext> {
    /// <summary>
    /// 過濾條件，Model取回資料時的過濾條件(除了Find)。
    /// </summary>
    private Condition Filter { get; }

    /// <summary>
    /// 建構 PageModel
    /// </summary>
    public PageModel() : base("Page") => Filter = new Condition();

    /// <summary>
    /// 取得指定編號的會員資料
    /// </summary>
    /// <param name="id">會員編號</param>
    /// <param name="trackable">可追蹤，如為純資料請勿追蹤</param>
    /// <param name="user">執行操作的用戶</param>
    /// <returns>依編號取回的實體資料</returns>
    public Page Find(int id, bool trackable = true, IUser user = null) => new PageReadAction(user ?? CurrentUser, DataContext).Find(id, trackable);

    /// <summary>
    /// 查詢符合指定條件的實體資料
    /// </summary>
    /// <param name="keyword">關鍵字</param>
    /// <param name="condition">查詢條件</param>
    /// <param name="sortings">排序方式</param>
    /// <param name="user">執行操作的用戶</param>
    /// <param name="includes">須積極取回之相關資料</param>
    /// <returns>依指定條件取回的實體資料集合</returns>
    public IQueryable<Page> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition ??= new Condition();
        condition.And(Filter);
        if (keyword.HasValue()) condition.And("Title", Operator.Contains, keyword);
        return new PageReadAction(user ?? CurrentUser, DataContext).Query(condition, sortings, includes);
    }

    /// <summary>
    /// 依資料範本新增實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public Page Create(Page data, IUser user = null) => new PageCreateAction(user ?? CurrentUser, DataContext).Create(data);

    /// <summary>
    /// 依資料範本更新實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public Page Update(Page data, IUser user = null) => new PageUpdateAction(user ?? CurrentUser, DataContext).Update(data);

    /// <summary>
    /// 依指定識別碼刪除實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    public void Delete(int id, IUser user = null) => new PageDeleteAction(user ?? CurrentUser, DataContext).Delete(new Page { Id = id });
}