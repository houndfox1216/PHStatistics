using Microsoft.EntityFrameworkCore;
using PHStatistics.Community;
using PHStatistics.Content;
using System;
using System.Collections.Generic;
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

    // 快取此次 Session 的權限集合
    private HashSet<SystemPermission> _permissions = new();

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
                var context = Http.Context.DataContext<DataContext>();
                if (context.Member
                        .Include("Photo")
                        .Include("MemberRoles.Role")
                        .SingleOrDefault(e => e.Id == id) is not { } member) return;
                if (member.Status == MemberStatus.Enabled) {
                    Data = member.DataMode switch {
                        DataMode.Debug when Environment.Debug => member,
                        DataMode.Normal or DataMode.System => member,
                        _ => Data
                    };
                    if (Data != null)
                        _permissions = BuildPermissions(member);
                }
            }
        }
    }

    /// <summary>
    /// 判斷目前用戶是否擁有指定的系統權限。
    /// Administrator 擁有所有權限。
    /// </summary>
    public bool HasPermission(SystemPermission permission) =>
        _permissions.Contains(SystemPermission.Administrator) ||
        _permissions.Contains(permission);

    private static HashSet<SystemPermission> BuildPermissions(Member member) {
        var result = new HashSet<SystemPermission>();
        if (member.MemberRoles == null) return result;
        foreach (var mr in member.MemberRoles) {
            if (mr.Role?.Permissions == null) continue;
            foreach (var p in mr.Role.Permissions) {
                if (Enum.TryParse<SystemPermission>(p.EnumName, out var sp))
                    result.Add(sp);
            }
        }
        return result;
    }

    /// <summary>
    /// 提交用戶資料
    /// </summary>
    public override void Commit() {
        if (Data == SystemUser.Default.Data || this.IsGuest()) return;
        Http.Context.DataContext<DataContext>().SaveChanges();
    }
}