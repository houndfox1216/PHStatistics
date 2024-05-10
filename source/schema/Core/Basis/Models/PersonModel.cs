using System;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
using EmptyProject.Actions;

// ReSharper disable once CheckNamespace
namespace EmptyProject.Models;

public class PersonModel : HttpModelBase<DataContext> {
    /// <summary>
    /// 過濾條件，Model取回資料時的過濾條件(除了Find)。
    /// </summary>
    public Condition Filter { get; set; }

    /// <summary>
    /// 建構 PersonModel
    /// </summary>
    /// <param name="user">Model 將以此用戶進行各項操作</param>
    public PersonModel() : base("Person") {
        this.Filter = new Condition("DataMode", Operator.Equal, DataMode.Normal);
    }

    /// <summary>
    /// 取得指定編號的實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    /// <returns>依編號取回的實體資料</returns>
    public Person Find(Guid id, IUser user = null) => new PersonReadAction(user ?? this.CurrentUser, this.DataContext).Find(id);

    /// <summary>
    /// 查詢符合指定條件的實體資料
    /// </summary>
    /// <param name="keyword">關鍵字</param>
    /// <param name="condition">查詢條件</param>
    /// <param name="sortings">排序方式</param>
    /// <param name="user">執行操作的用戶</param>
    /// <param name="includes">須積極取回之相關資料</param>
    /// <returns>依指定條件取回的實體資料集合</returns>
    public IQueryable<Person> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition ??= new Condition();
        condition.And(Filter);
        if (keyword.HasValue()) condition.And("Name", Operator.Contains, keyword);
        return new PersonReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
    }

    /// <summary>
    /// 依資料範本新增實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    public Person Create(Person data, IUser user = null) => new PersonCreateAction(user ?? this.CurrentUser, this.DataContext).Create(data);

    /// <summary>
    /// 依資料範本更新實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <returns>影響的資料比數</returns>
    public Person Update(Person data, IUser user = null) => new PersonUpdateAction(user ?? this.CurrentUser, this.DataContext).Update(data);

    /// <summary>
    /// 依指定識別碼刪除實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    public void Delete(Guid id, IUser user = null) => new PersonDeleteAction(user ?? this.CurrentUser, this.DataContext).Delete(new Person { Id = id });
}