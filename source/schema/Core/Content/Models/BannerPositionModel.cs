using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
<<<<<<< HEAD
using EmptyProject.Actions;

namespace EmptyProject.Models;
=======
using PHStatistics.Actions;

namespace PHStatistics.Models;
>>>>>>> origin/develop/schema

public class BannerPositionModel : HttpModelBase<DataContext> {
    /// <summary>
    /// 過濾條件，Model取回資料時的過濾條件(除了Find)。
    /// </summary>
    public Condition Filter { get; set; }

    /// <summary>
    /// 建構 BannerModel
    /// </summary>
    /// <param name="user">Model 將以此用戶進行各項操作</param>
    public BannerPositionModel() : base("BannerPosition") {
        this.Filter = new Condition("DataMode", Operator.NotEqual, DataMode.Deleted);
    }

    /// <summary>
    /// 取得指定識別碼的實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <returns>依識別碼取回的廣實體資料</returns>
    public BannerPosition Find(int id, IUser user = null) => new BannerPositionReadAction(user ?? this.CurrentUser, this.DataContext).Find(id, "Banners");

    /// <summary>
    /// 取得指定識別碼的實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <returns>依識別碼取回的實體資料</returns>
    public BannerPosition FindByCode(string code, IUser user = null) => new BannerPositionReadAction(user ?? this.CurrentUser, this.DataContext).FindByCode(code);

    /// <summary>
    /// 查詢符合指定條件的實體資料
    /// </summary>
    /// <param name="keyword">關鍵字</param>
    /// <param name="condition">查詢條件</param>
    /// <param name="sortings">排序方式</param>
    /// <param name="user">執行操作的用戶</param>
    /// <param name="includes">須積極取回之相關資料</param>
    /// <returns>依指定條件取回的實體資料集合</returns>
    public IQueryable<BannerPosition> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition ??= new Condition();
        condition.And(Filter);
        if (keyword.HasValue()) condition.And("Name", Operator.Contains, keyword);
        return new BannerPositionReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
    }

    /// <summary>
    /// 依資料範本新增實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public BannerPosition Create(BannerPosition data, IUser user = null) => new BannerPositionCreateAction(user ?? this.CurrentUser, this.DataContext).Create(data);

    /// <summary>
    /// 依資料範本更新實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public BannerPosition Update(BannerPosition data, IUser user = null) => new BannerPositionUpdateAction(user ?? this.CurrentUser, this.DataContext).Update(data);

    /// <summary>
    /// 依指定識別碼刪除實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    public void Delete(int id, IUser user = null) => new BannerPositionDeleteAction(user ?? this.CurrentUser, this.DataContext).Delete(new BannerPosition { Id = id });
}