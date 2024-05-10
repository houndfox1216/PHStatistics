using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Security;
using System.Linq;
using System.Text.RegularExpressions;

namespace PHStatistics.Actions;

/// <summary>
/// 更新個人資料之操作。
/// </summary>
[Description("更新個人資料")]
public partial class PersonUpdateAction : UpdateActionBase<Person, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 PersonUpdateAction。
    /// </summary>
    /// <param name="user">請求操作的個人</param>
    public PersonUpdateAction(IUser user, DataContext dbContext) : base("更新個人資料", user, dbContext) {
            RequiredIncludes = new[] { "Photo", "Address" };
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
        }

    /// <summary>
    /// 當更新實體資料前執行
    /// </summary>
    /// <param name="context">資料脈絡</param>
    /// <param name="data">資料範本</param>
    /// <param name="current">目前資料</param>
    protected override void OnUpdating(DataContext context, Person data, Person current) {
            if (!User.IsApproved(SystemPermission.Person) && (User.Data is not User user || user.PersonId != data.Id)) throw new SecurityException("僅本人或具權限者可更新個資");
            if (data.PersonalId != null) {
                if (!PersonalIdRegex().IsMatch(data.PersonalId)) throw new DataException("請移除英數字外所有符合後再試一次");
                if (context.Person.Any(e => e.Id != current.Id && e.PersonalId == data.PersonalId)) throw new DataException("此身分證字號已被使用");
            }
            if (data.Photo.HasValue()) {
                current.Photo.Uri = data.Photo.Uri;
            }
            if (data.Address.HasValue()) {
                context.Entry(current.Address).CurrentValues.SetValues(data.Address);
            }
        }

    [GeneratedRegex("[0-9a-zA-Z]{8,}")]
    private static partial Regex PersonalIdRegex();
}