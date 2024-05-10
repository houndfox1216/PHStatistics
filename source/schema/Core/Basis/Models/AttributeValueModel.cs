using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
using PHStatistics.Actions;

// ReSharper disable once CheckNamespace
namespace PHStatistics.Models;

public class AttributeValueModel : HttpModelBase<DataContext> {
    /// <summary>
    /// 過濾條件，Model取回資料時的過濾條件(除了Find)。
    /// </summary>
    public Condition Filter { get; set; }

    /// <summary>
    /// 建構 BannerModel
    /// </summary>
    /// <param name="user">Model 將以此用戶進行各項操作</param>
    public AttributeValueModel() : base("AttributeValue") => this.Filter = new Condition();

    /// <summary>
    /// 取得指定編號的實體資料
    /// </summary>
    /// <param name="id">廣告資料編號</param>
    /// <returns>依編號取回的廣告資料</returns>
    public AttributeValue Find(int id, IUser user = null) => new AttributeValueReadAction(user ?? this.CurrentUser, this.DataContext).Find(id);

    /// <summary>
    /// 查詢符合指定條件的實體資料
    /// </summary>
    /// <param name="keyword">關鍵字</param>
    /// <param name="condition">查詢條件</param>
    /// <param name="sortings">排序方式</param>
    /// <param name="user">執行操作的用戶</param>
    /// <param name="includes">須積極取回之相關資料</param>
    /// <returns>依指定條件取回的實體資料集合</returns>
    public IQueryable<AttributeValue> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition = Filter.And(condition ?? new Condition());
        if (keyword.HasValue()) condition.And(new Condition(new Condition("Code", Operator.Contains, keyword), new Condition("Name", Operator.Contains, keyword, LogicalConnective.Or)));
        return new AttributeValueReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
    }

    /// <summary>
    /// 依資料範本新增實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public AttributeValue Create(AttributeValue data, IUser user = null) => new AttributeValueCreateAction(user ?? this.CurrentUser, this.DataContext).Create(data);

    /// <summary>
    /// 依資料範本更新實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public AttributeValue Update(AttributeValue data, IUser user = null) => new AttributeValueUpdateAction(user ?? this.CurrentUser, this.DataContext).Update(data);

    /// <summary>
    /// 依指定識別碼刪除實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    public void Delete(int id, IUser user = null) => new AttributeValueDeleteAction(user ?? this.CurrentUser, this.DataContext).Delete(new AttributeValue { Id = id });
}