using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
using PHStatistics.Actions;

namespace PHStatistics.Models;

public class PictureModel : HttpModelBase<DataContext> {
    /// <summary>
    /// 建構 PictureModel
    /// </summary>
    public PictureModel() : base("Picture") { }

    /// <summary>
    /// 取得指定識別碼的實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    public Picture Find(int id, IUser user = null) => new PictureReadAction(user ?? this.CurrentUser, this.DataContext).Find(id);

    /// <summary>
    /// 查詢符合指定條件的實體資料
    /// </summary>
    /// <param name="keyword">關鍵字</param>
    /// <param name="condition">查詢條件</param>
    /// <param name="sortings">排序方式</param>
    /// <param name="user">執行操作的用戶</param>
    /// <param name="includes">須積極取回之相關資料</param>
    /// <returns>依指定條件取回的實體資料集合</returns>
    public IQueryable<Picture> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition ??= new Condition();
        if (keyword.HasValue()) condition.And("Name", Operator.Contains, keyword);
        return new PictureReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
    }

    /// <summary>
    /// 依資料範本新增實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public Picture Create(Picture data, IUser user = null) => new PictureCreateAction(user ?? this.CurrentUser, this.DataContext).Create(data);

    /// <summary>
    /// 依資料範本更新實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <param name="user">執行操作的用戶</param>
    public Picture Update(Picture data, IUser user = null) => new PictureUpdateAction(user ?? this.CurrentUser, this.DataContext).Update(data);

    /// <summary>
    /// 依指定識別碼刪除實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <param name="user">執行操作的用戶</param>
    public void Delete(int id, IUser user = null) => new PictureDeleteAction(user ?? this.CurrentUser, this.DataContext).Delete(new Picture { Id = id });
}