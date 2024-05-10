using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Framework.Security;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 刪除個人資料之操作。
/// </summary>
[Description("刪除個人資料")]
public class PersonDeleteAction : DeleteActionBase<Person, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

    /// <summary>
    /// 初始化 PersonDeleteAction。
    /// </summary>
    /// <param name="user">請求操作的個人</param>
    public PersonDeleteAction(IUser user, DataContext dbContext = null) : base("刪除個人資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = new[] { "Photo", "Address" };
        }

    protected override void OnDeleting(DataContext context, Person current) {
            if (!CheckPermission(current.Id)) throw new SecurityException("僅本人或具權限者可刪除個資");
            if (current.Photo.HasValue()) {
                context.Picture.Remove(current.Photo);
            }
            if (current.Address.HasValue()) {
                context.Address.Remove(current.Address);
            }
        }

    /// <summary>
    /// 檢查權限並回傳是否允許
    /// </summary>
    /// <param name="personId">將被刪除的個資識別碼</param>
    private bool CheckPermission(Guid personId) => User.IsApproved(SystemPermission.Person) || (User.Data is User user && user.PersonId == personId);
}