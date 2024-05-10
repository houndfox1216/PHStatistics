using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Security;
using System.Framework.Web;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace PHStatistics.Services.Admin;

/// <summary>
/// 後台服務用戶
/// </summary>
[Description("後台服務用戶")]
public class ServiceUser : System.Framework.Web.User {
    /// <summary>
    /// 初始化系統用戶
    /// </summary>
    public ServiceUser() { Data = new User(); }

    /// <summary>
    /// 初始化系統用戶，並使用此用戶資料。
    /// </summary>
    /// <param name="userData">用戶資料</param>
    public ServiceUser(IUserData userData) { Data = userData; }

    /// <summary>
    /// 初始化系統用戶，並以此用戶編號搜尋用戶資料。
    /// </summary>
    /// <param name="id">用戶編號</param>
    public ServiceUser(string id) {
        try { Id = id; } catch (Exception e) { throw new SecurityException("此用戶不存在!", e); }
    }

    /// <summary>
    /// 取得或設定用戶編號。設定用戶編號時用戶資料將一併變更為該用戶之資料。
    /// </summary>
    public sealed override string Id {
        get => Data == SystemUser.Default.Data ? "System" : Data?.Id?.ToString();
        set {
            if (!value.HasValue()) return;
            if (value == "System") Data = SystemUser.Default.Data;
            else if (Guid.TryParse(value, out var id)) {
                Data = null;
                var context = Http.Context.DataContext<DataContext>();
                if (context.User.Include("Photo").Include("UserRoles.Role").SingleOrDefault(e => e.Id == id) is not { } user) return;
                if (user.Status == UserStatus.Enabled && user.DataMode != DataMode.Deleted) Data = user;
            }
        }
    }

    /// <summary>
    /// 提交用戶資料
    /// </summary>
    public override void Commit() {
        if (Data == SystemUser.Default.Data || this.IsGuest()) return;
        Http.Context.DataContext<DataContext>().SaveChanges();
    }
}