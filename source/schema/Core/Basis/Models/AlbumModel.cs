using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
using PHStatistics.Actions;

// ReSharper disable once CheckNamespace
namespace PHStatistics.Models;

public class AlbumModel : HttpModelBase<DataContext> {
    /// <summary>
    /// 過濾條件，Model取回資料時的過濾條件(除了Find)。
    /// </summary>
    public Condition Filter { get; set; }

    /// <summary>
    /// 建構 BannerModel
    /// </summary>
    public AlbumModel() : base("PictureAlbum") {
        this.Filter = new Condition("DataMode", Operator.NotEqual, DataMode.Deleted);
    }

    /// <summary>
    /// 取得指定識別碼的實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    public Album Find(int id, IUser user = null) => new AlbumReadAction(user ?? this.CurrentUser, this.DataContext).Find(id, "Pictures");

    /// <summary>
    /// 查詢符合指定條件的實體資料
    /// </summary>
    /// <param name="keyword">關鍵字</param>
    /// <param name="condition">查詢條件</param>
    /// <param name="sortings">排序方式</param>
    /// <param name="user">執行操作的用戶</param>
    /// <param name="includes">須積極取回之相關資料</param>
    public IQueryable<Album> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition ??= new Condition();
        condition.And(Filter);
        if (keyword.HasValue()) condition.And("Title", Operator.Contains, keyword);
        return new AlbumReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
    }

    /// <summary>
    /// 依資料範本新增實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public Album Create(Album data, IUser user = null) => new AlbumCreateAction(user ?? this.CurrentUser, this.DataContext).Create(data);

    /// <summary>
    /// 依資料範本更新實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public Album Update(Album data, IUser user = null) => new AlbumUpdateAction(user ?? this.CurrentUser, this.DataContext).Update(data);

    /// <summary>
    /// 依指定識別碼刪除實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    public void Delete(int id, IUser user = null) => new AlbumDeleteAction(user ?? this.CurrentUser, this.DataContext).Delete(new Album { Id = id });

    /// <summary>
    /// 將指定兩識別碼之間的資料進行重新排序，如兩者的父類別不同時將皆設為 target 的父類別
    /// </summary>
    /// <param name="source">來源(要求排序)的實體資料是別碼</param>
    /// <param name="target">目標(插入位置)的實體資料識別碼</param>
    /// <param name="isAfter">是否添加於目標之後</param>
    /// <param name="user">執行操作的用戶</param>
    public void Reorder(int source, int target, bool isAfter, IUser user = null) {
        new AlbumReorderAction(user ?? this.CurrentUser, this.DataContext).Execute(source, target, isAfter);
    }
}