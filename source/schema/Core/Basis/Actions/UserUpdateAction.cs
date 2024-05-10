using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Security;
using System.IO;
using System.Linq;

using Environment = System.Framework.Environment;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 更新用戶資料之操作。
/// </summary>
[Description("更新用戶資料")]
public class UserUpdateAction : UpdateActionBase<User, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.User };

    /// <summary>
    /// 初始化 UserUpdateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public UserUpdateAction(IUser user, DataContext dbContext) : base("更新用戶資料", user, dbContext) {
            RequiredIncludes = new[] { "Photo", "UserRoles.Role" };
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
        }

    /// <summary>
    /// 當更新實體資料前執行
    /// </summary>
    /// <param name="context">資料脈絡</param>
    /// <param name="data">資料範本</param>
    /// <param name="current">目前資料</param>
    protected override void OnUpdating(DataContext context, User data, User current) {
            // 檢查帳號與密碼是否已被使用
            if (!data.Account.HasValue()) throw new DataException("請提供帳號");
            if (data.Account != current.Account && context.User.Any(e => e.Id != data.Id && e.Account == data.Account)) throw new DataException("此帳號已被使用");

            // 檢查是否更換密碼
            if (data.Password.HasValue() && data.Password != current.Password) {
                var hashedPassword = data.Password.ComputeHashStringWithSha().ToBase64();
                if (hashedPassword != current.Password) {
                    current.Password = hashedPassword;
                    current.PasswordChangedTime = DateTime.Now;
                }
            }
            data.Password = current.Password;

            // 檢查是否更換照片
            if (data.Photo.HasValue()) current.Photo.Uri = data.Photo.Uri;

            // 檢查角色是否有變動
            var removedReferences = current.UserRoles.Where(e => !data.RoleIds.Contains(e.RoleId));
            foreach (var reference in removedReferences) current.UserRoles.Remove(reference);
            var addedReferences = data.RoleIds.Except(current.RoleIds);
            foreach (var reference in addedReferences) current.UserRoles.Add(new UserRole { UserId = current.Id, RoleId = reference });

            data.Photo = current.Photo;
            data.PersonId ??= current.PersonId;

            data.Token = current.Token;
            data.LoginTime = current.LoginTime;
            data.LogoutTime = current.LogoutTime;
            data.LastVisitedTime = current.LastVisitedTime;
        }
}