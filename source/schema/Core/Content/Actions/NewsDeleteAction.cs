using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;

namespace PHStatistics.Actions;

/// <summary>
/// 刪除新聞資料之操作。
/// </summary>
[Description("刪除新聞資料")]
public class NewsDeleteAction : DeleteActionBase<News, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.News];

    /// <summary>
    /// 初始化 NewsCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public NewsDeleteAction(IUser user, DataContext dbContext = null) : base("刪除新聞資料", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
            RequiredIncludes = ["Title.Texts", "Introduction.Texts", "Content.Texts", "Picture.Images", "NewsTags"];
        }

    protected override void OnDeleting(DataContext context, News current) {
            if (current.Title != null) context.Remove(current.Title);
            if (current.Introduction != null) context.Remove(current.Introduction);
            if (current.Content != null) context.Remove(current.Content);
            if (current.Picture.HasValue()) foreach (var image in current.Picture.Images) context.Picture.Remove(image);
            if (current.Picture != null) context.Remove(current.Picture);
        }
}