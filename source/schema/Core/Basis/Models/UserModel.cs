using System;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Security;
using System.Framework.Web;
using System.Linq;
using EmptyProject.Actions;

namespace EmptyProject.Models;

/// <summary>
/// 用戶資料模型
/// </summary>
public class UserModel : HttpModelBase<DataContext> {
    /// <summary>
    /// 過濾條件，Model取回資料時的過濾條件(除了Find)。
    /// </summary>
    public Condition Filter { get; set; }

    /// <summary>
    /// 建構 UserModel
    /// </summary>
    /// <param name="user">Model 將以此用戶進行各項操作</param>
    public UserModel() : base("User") {
        this.Filter = new Condition("DataMode", Operator.Equal, DataMode.Normal);
    }

    /// <summary>
    /// 取得指定編號的用戶資料
    /// </summary>
    /// <param name="id">用戶編號</param>
    /// <param name="user">執行操作的用戶</param>
    /// <returns>依編號取回的實體資料</returns>
    public User Find(Guid id, IUser user = null) => new UserReadAction(user ?? this.CurrentUser, this.DataContext).Find(id);

    /// <summary>
    /// 查詢符合指定條件的實體資料
    /// </summary>
    /// <param name="keyword">關鍵字</param>
    /// <param name="condition">查詢條件</param>
    /// <param name="sortings">排序方式</param>
    /// <param name="user">執行操作的用戶</param>
    /// <param name="includes">須積極取回之相關資料</param>
    /// <returns>依指定條件取回的實體資料集合</returns>
    public IQueryable<User> Query(string keyword = null, Condition condition = null, Sorting[] sortings = null, IUser user = null, params string[] includes) {
        condition ??= new Condition();
        condition.And(Filter);
        if (keyword.HasValue()) condition.And("Name", Operator.Contains, keyword);
        return new UserReadAction(user ?? this.CurrentUser, this.DataContext).Query(condition, sortings, includes);
    }

    /// <summary>
    /// 依資料範本新增實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    public User Create(User data, IUser user = null) => new UserCreateAction(user ?? this.CurrentUser, this.DataContext).Create(data);

    /// <summary>
    /// 依資料範本更新實體資料
    /// </summary>
    /// <param name="data">資料範本</param>
    /// <returns>影響的資料比數</returns>
    public User Update(User data, IUser user = null) => new UserUpdateAction(user ?? this.CurrentUser, this.DataContext).Update(data);

    /// <summary>
    /// 依指定識別碼刪除實體資料
    /// </summary>
    /// <param name="id">識別碼</param>
    public void Delete(Guid id, IUser user = null) => new UserDeleteAction(user ?? this.CurrentUser, this.DataContext).Delete(new User { Id = id });

    /// <summary>
    /// 透過帳號與密碼認證用戶
    /// </summary>
    /// <param name="account">帳號</param>
    /// <param name="password">密碼</param>
    /// <returns>經過認證的用戶資料，當認證失敗時為 Null</returns>
    public User Authorize(string account, string password) {
        var hashedPassword = password.ComputeHashStringWithSha().ToBase64();
        return new UserReadAction(SystemUser.Default, DataContext).Authorize(account, hashedPassword);
    }

    /// <summary>
    /// 變更目前用戶的密碼
    /// </summary>
    /// <param name="oldPassword">舊密碼</param>
    /// <param name="newPassword">新密碼</param>
    /// <returns></returns>
    public User ChangePassword(string oldPassword, string newPassword) {
        var action = new UserChangePasswordAction(this.CurrentUser, this.DataContext);
        var parameters = new Parameters {
            { "old-password", oldPassword },
            { "new-password", newPassword }
        };
        return action.Update(this.CurrentUser.Data as User, parameters);
    }
}