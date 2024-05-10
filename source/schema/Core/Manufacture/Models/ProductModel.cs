using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
using EmptyProject.Actions;

namespace EmptyProject.Models;

/// <summary>
/// 商品資料模型
/// </summary>
public class ProductModel : HttpModelBase<DataContext> {
    /// <summary>
    /// 過濾條件，Model取回資料時的過濾條件(除了Find)。
    /// </summary>
    public Condition Filter { get; set; }

    /// <summary>
    /// 建構 ProductModel
    /// </summary>
    /// <param name="user">Model 將以此用戶進行各項操作</param>
    public ProductModel() : base("Product") => this.Filter = new Condition("DataMode", Operator.Equal, DataMode.Normal);

    /// <summary>
    /// 取得指定編號的會員資料
    /// </summary>
    /// <param name="id">會員編號</param>
    /// <param name="user">執行操作的用戶</param>
    /// <returns>依編號取回的實體資料</returns>
    public Product Find(int id, bool trackable = true, IUser user = null) {
        string[] include = new string[] { "Picture" };
        return new ProductReadAction(user ?? this.CurrentUser, this.DataContext).Find(id, trackable, include);
    }

    /// <summary>
    /// 查詢符合指定條件的實體資料
    /// </summary>
    /// <param name="keyword">關鍵字</param>
    /// <param name="condition">查詢條件</param>
    /// <param name="sortings">排序方式</param>
    /// <param name="user">執行操作的用戶</param>
    /// <param name="includes">須積極取回之相關資料</param>
    /// <returns>依指定條件取回的實體資料集合</returns>
    public IQueryable<Product> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition ??= new Condition();
        condition.And(Filter);
        if (keyword.HasValue()) condition.And("Name", Operator.Contains, keyword);
        return new ProductReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
    }

    /// <summary>
    /// 依資料範本新增實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public Product Create(Product data, IUser user = null) => new ProductCreateAction(user ?? this.CurrentUser, this.DataContext).Create(data);

    /// <summary>
    /// 依資料範本更新實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public Product Update(Product data, IUser user = null) => new ProductUpdateAction(user ?? this.CurrentUser, this.DataContext).Update(data);

    /// <summary>
    /// 依指定識別碼刪除實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    public void Delete(int id, IUser user = null) => new ProductDeleteAction(user ?? this.CurrentUser, this.DataContext).Delete(new Product { Id = id });
}