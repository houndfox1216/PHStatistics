using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;

namespace EmptyProject.Actions;

/// <summary>
/// 刪除商品資料之操作。
/// </summary>
[Description("刪除商品資料")]
public class ProductDeleteAction : DeleteActionBase<Product, DataContext, SystemPermission> {
    private string directory = null;
     
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Product };

    /// <summary>
    /// 初始化 ProductCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public ProductDeleteAction(IUser user, DataContext dbContext = null) : base("刪除商品資料", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
            RequiredIncludes = new[] { "Picture", "Album", "Description" };
        }

    protected override void OnDeleting(DataContext context, Product current) {
            if (current.Description.HasValue()) context.StringResource.Remove(current.Description);
            if (current.Picture.HasValue()) {
                if (current.Picture.Uri != null) {
                    var versionStartPosition = current.Picture.Uri.IndexOf("?v=");
                    var filename = versionStartPosition > 0 ? current.Picture.Uri.Substring(0, versionStartPosition) : current.Picture.Uri;
                    var file = new FileInfo(Path.Combine(Environment.Directory.WebRootPath, filename.Trim('/')));
                    if (file.Exists) file.Delete();
                }
                context.Picture.Remove(current.Picture);
            }
            if (current.Album.HasValue()) {
                if (current.Album.Number.HasValue()) directory = Path.Combine(Environment.Directory.WebRootPath, "files", "albums", current.Album.Number);
                context.Album.Remove(current.Album);
            }
        }

    protected override void OnDeleted(DataContext context, Product current) {
            if (directory.HasValue() && Directory.Exists(directory)) Directory.Delete(directory, true);
        }

}