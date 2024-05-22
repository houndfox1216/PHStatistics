using PHStatistics.Community;
using System;
using System.Framework;
using System.Framework.Application;
using System.Framework.Community;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Web;
using System.Linq;
using Environment = System.Framework.Environment;

namespace PHStatistics.Portal;

/// <summary>
/// 服務用戶
/// </summary>
public class PortalUser : System.Framework.Web.User {
    /// <summary>
    /// 初始化系統用戶
    /// </summary>
    public PortalUser() => Data = new Member();

    /// <summary>
    /// 初始化系統用戶，並使用此用戶資料。
    /// </summary>
    /// <param name="data">用戶資料</param>
    public PortalUser(IUserData data) => Data = data;

    /// <summary>
    /// 取得或設定用戶編號。設定用戶編號時用戶資料將一併變更為該用戶之資料。
    /// </summary>
    public override string Id {
        get => Data == SystemUser.Default.Data ? "System" : Data?.Id?.ToString();
        set {
            if (!value.HasValue()) return;
            if (value == "System") Data = SystemUser.Default.Data;
            else if (Guid.TryParse(value, out Guid id)) {
                Data = null;
                if (Http.Context.DataContext<DataContext>().Member.Include("Photo").SingleOrDefault(e => e.Id == id) is not { } member) return;
                if (member.Status == MemberStatus.Enabled) {
                    Data = member.DataMode switch {
                        DataMode.Debug when Environment.Debug => member,
                        DataMode.Normal or DataMode.System => member,
                        _ => Data
                    };
                }
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