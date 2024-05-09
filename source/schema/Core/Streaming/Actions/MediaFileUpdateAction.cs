using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Linq;

namespace EmptyProject.Actions;

/// <summary>
/// 更新媒體檔案之操作。
/// </summary>
[Description("更新媒體檔案")]
public class MediaFileUpdateAction : UpdateActionBase<MediaFile, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.MediaFile };

    /// <summary>
    /// 建構 MediaFileUpdateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public MediaFileUpdateAction(IUser user, DataContext dbContext = null) : base("更新媒體檔案", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = new[] { "Title.Texts", "Content.Texts" };
        }

    protected override void OnUpdating(DataContext context, MediaFile data, MediaFile current) {
        if (User.Data is User user) data.PublisherId = user.Id;

        var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.First();

        #region 檢查標題是否有變動

        foreach (var text in data.Title.Texts) {
            if (text.Id == 0) { // create text
                current.Title.Texts.Add(text);
            } else if (current.Title.Texts.SingleOrDefault(e => e.Id == text.Id) is StringResource existedText) { // update text
                existedText.Culture = text.Culture;
                existedText.Content = text.Content;
            }
            if (defaultCulture.Id == text.Culture) data.Title.DefaultText = text.Content;
            // remove the same culture images
            foreach (var duplicateText in current.Title.Texts.Where(e => e.Culture == text.Culture && e.Id != text.Id)) {
                context.Remove(duplicateText);
            }
        }

        #endregion

        #region 檢查內容是否有變動

        foreach (var text in data.Content.Texts) {
            if (text.Id == 0) { // create text
                current.Content.Texts.Add(text);
            } else if (current.Content.Texts.SingleOrDefault(e => e.Id == text.Id) is StringResource existedText) { // update text
                existedText.Culture = text.Culture;
                existedText.Content = text.Content;
            }
            if (defaultCulture.Id == text.Culture) data.Content.DefaultText = text.Content;
            // remove the same culture images
            foreach (var duplicateText in current.Content.Texts.Where(e => e.Culture == text.Culture && e.Id != text.Id)) {
                context.Remove(duplicateText);
            }
        }

        #endregion
    }
}