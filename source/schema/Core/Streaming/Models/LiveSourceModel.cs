using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
using System.Net.Http;
using EmptyProject.Actions;

namespace EmptyProject.Models;

/// <summary>
/// 會員資料模型，用以存取會員相關資料
/// </summary>
public class LiveSourceModel : HttpModelBase<DataContext> {
    /// <summary>
    /// 過濾條件，Model取回資料時的過濾條件(除了Find)。
    /// </summary>
    public Condition Filter { get; set; }

    /// <summary>
    /// 建構 LiveSourceModel
    /// </summary>
    /// <param name="user">Model 將以此用戶進行各項操作</param>
    public LiveSourceModel() : base("LiveSource") => Filter = new Condition();

    /// <summary>
    /// 取得指定編號的會員資料
    /// </summary>
    /// <param name="id">會員編號</param>
    /// <param name="user">執行操作的用戶</param>
    /// <returns>依編號取回的實體資料</returns>
    public LiveSource Find(int id, bool trackable = true, IUser user = null) {
        return new LiveSourceReadAction(user ?? this.CurrentUser, this.DataContext).Find(id, trackable);
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
    public IQueryable<LiveSource> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition ??= new Condition();
        condition.And(Filter);
        if (keyword.HasValue()) condition.And("Title", Operator.Contains, keyword);
        return new LiveSourceReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
    }

    /// <summary>
    /// 依資料範本新增實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public LiveSource Create(LiveSource data, IUser user = null) => new LiveSourceCreateAction(user ?? this.CurrentUser, this.DataContext).Create(data);

    /// <summary>
    /// 依資料範本更新實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public LiveSource Update(LiveSource data, IUser user = null) => new LiveSourceUpdateAction(user ?? this.CurrentUser, this.DataContext).Update(data);

    /// <summary>
    /// 依指定識別碼刪除實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    public void Delete(int id, IUser user = null) => new LiveSourceDeleteAction(user ?? this.CurrentUser, this.DataContext).Delete(new LiveSource { Id = id });

    /// <summary>
    /// 將指定兩識別碼之間的資料進行重新排序，如兩者的父類別不同時將皆設為 target 的父類別
    /// </summary>
    /// <param name="source">來源(要求排序)的實體資料是別碼</param>
    /// <param name="target">目標(插入位置)的實體資料識別碼</param>
    /// <param name="isAfter">是否添加於目標之後</param>
    /// <param name="user">執行操作的用戶</param>
    public void Reorder(int source, int target, bool isAfter, IUser user = null) {
        new MediaFileReorderAction(user ?? this.CurrentUser, this.DataContext).Execute(source, target, isAfter);
    }
}