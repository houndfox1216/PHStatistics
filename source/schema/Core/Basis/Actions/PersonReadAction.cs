using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Security;

namespace EmptyProject.Actions;

/// <summary>
/// 讀取個人資料之操作。
/// </summary>
[Description("讀取個人資料")]
public class PersonReadAction : ReadActionBase<Person, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 PersonReadAction。
    /// </summary>
    /// <param name="user">請求操作的個人</param>
    public PersonReadAction(IUser user, DataContext dbContext, bool trackEnabled = false) : base("讀取個人資料", user, dbContext, trackEnabled) {
            RequiredIncludesInFind = new[] { "Photo", "Address" };
            RequiredIncludesInQuery = new[] { "Photo", "Address" };
        }

    protected override void OnFinding(DataContext context, object key, IList<string> includes) {
            if (!User.IsApproved(SystemPermission.Person) && (User.Data is not User user || user.PersonId != (Guid)key)) throw new SecurityException("僅本人或具權限者可讀取個資");
            base.OnFinding(context, key, includes);
        }

    protected override void OnQuerying(DataContext context, IList<string> includes, Condition condition) {
            if (!(User.IsApproved(SystemPermission.Person) || User.IsApproved(SystemPermission.User))) throw new SecurityException("權限不足");
            base.OnQuerying(context, includes, condition);
        }
}