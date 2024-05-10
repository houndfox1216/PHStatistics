using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;
using System.Linq;

<<<<<<< HEAD
namespace EmptyProject.Actions;
=======
namespace PHStatistics.Actions;
>>>>>>> origin/develop/schema

/// <summary>
/// 刪除用戶資料之操作。
/// </summary>
[Description("刪除用戶資料")]
public class UserDeleteAction : DeleteActionBase<User, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.User };

    /// <summary>
    /// 初始化 UserDeleteAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public UserDeleteAction(IUser user, DataContext dbContext = null) : base("刪除用戶資料", user, dbContext) {             
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = new[] { "Photo" };
        }

    protected override void OnDeleting(DataContext context, User current) {
            if (current.Photo.HasValue()) {
                if (current.Photo.Uri != null) {
                    var versionStartPosition = current.Photo.Uri.IndexOf("?v=");
                    var filename = versionStartPosition > 0 ? current.Photo.Uri.Substring(0, versionStartPosition) : current.Photo.Uri;
                    var file = new FileInfo(Path.Combine(Environment.Directory.WebRootPath, filename.Trim('/')));
                    if (file.Exists) file.Delete();
                }
                context.Picture.Remove(current.Photo);
            }
            if (!context.User.Any(e => e.Id != current.Id && e.PersonId == current.PersonId)) {
                if (current.Person.Photo != null) context.Picture.Remove(current.Person.Photo);
                if (current.Person.Address != null) context.Address.Remove(current.Person.Address);
                context.Person.Remove(current.Person);
            }
        }
}