using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Security;
using System.IO;
using System.Linq;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 新增用戶資料之操作。
/// </summary>
[Description("新增用戶資料")]
public class UserCreateAction : CreateActionBase<User, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.User };

    /// <summary>
    /// 初始化 UserCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public UserCreateAction(IUser user, DataContext dbContext) : base("新增用戶資料", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }

    /// <summary>
    /// 當實體資料創建前執行
    /// </summary>
    /// <param name="context">資料脈絡</param>
    /// <param name="data">資料範本</param>
    protected override void OnCreating(DataContext context, User data) {
            if (context.User.Any(e => e.Account == data.Account)) throw new DataException("此帳號已被使用");
            data.Password = data.Password.ComputeHashStringWithSha().ToBase64();
            data.PasswordChangedTime = DateTime.Now;
            data.Person = data.Person.HasValue() ? context.Person.Find(data.Person.Id) : null;
            data.Person ??= context.Person.Single(e => e.DataMode == DataMode.System && e.Nickname == "Operator");
            foreach (var item in data.Roles) {
                if (context.Role.Find(item.Id) is Role role) context.UserRole.Add(new UserRole { User = data, Role = role });
            }
            data.Photo ??= new Picture();
        }
}