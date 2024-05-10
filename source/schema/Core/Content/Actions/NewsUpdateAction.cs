using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;

namespace PHStatistics.Actions;

/// <summary>
/// 更新新聞資料之操作。
/// </summary>
[Description("更新新聞資料")]
public class NewsUpdateAction : UpdateActionBase<News, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.News];

    /// <summary>
    /// 建構 NewsCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public NewsUpdateAction(IUser user, DataContext dbContext = null) : base("更新新聞資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = ["Title.Texts", "Introduction.Texts", "Content.Texts", "Picture.Images", "NewsTags"];
        }

    protected override void OnUpdating(DataContext context, News data, News current) {
        var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.First();

        #region 檢查圖片是否有變動

        foreach (var image in data.Picture.Images) {
            if (image.Id == 0) { // create image
                current.Picture.Images.Add(image);
            } else if (current.Picture.Images.SingleOrDefault(e => e.Id == image.Id) is { } existedImage) { // update image
                existedImage.Culture = image.Culture;
                existedImage.Uri = image.Uri;
            }
            if (defaultCulture.Id == image.Culture) current.Picture.DefaultImageUri = image.Uri;
            // remove the same culture images
            foreach (var duplicateImage in current.Picture.Images.Where(e => e.Culture == image.Culture && e.Id != image.Id)) {
                context.Remove(duplicateImage);
            }
        }

        #endregion
        #region 檢查標題是否有變動

        foreach (var text in data.Title.Texts) {
            if (text.Id == 0) { // create text
                current.Title.Texts.Add(text);
            } else if (current.Title.Texts.SingleOrDefault(e => e.Id == text.Id) is { } existedText) { // update text
                existedText.Culture = text.Culture;
                existedText.Content = text.Content;
            }
            if (defaultCulture.Id == text.Culture) current.Title.DefaultText = text.Content;
            // remove the same culture images
            foreach (var duplicateText in current.Title.Texts.Where(e => e.Culture == text.Culture && e.Id != text.Id)) {
                context.Remove(duplicateText);
            }
        }

        #endregion
        #region 檢查簡介是否有變動

        foreach (var text in data.Introduction.Texts) {
            if (text.Id == 0) { // create text
                current.Introduction.Texts.Add(text);
            } else if (current.Introduction.Texts.SingleOrDefault(e => e.Id == text.Id) is { } existedText) { // update text
                existedText.Culture = text.Culture;
                existedText.Content = text.Content;
            }
            if (defaultCulture.Id == text.Culture) current.Introduction.DefaultText = text.Content;
            // remove the same culture images
            foreach (var duplicateText in current.Introduction.Texts.Where(e => e.Culture == text.Culture && e.Id != text.Id)) {
                context.Remove(duplicateText);
            }
        }

        #endregion
        #region 檢查內容是否有變動

        foreach (var text in data.Content.Texts) {
            if (text.Id == 0) { // create text
                current.Content.Texts.Add(text);
            } else if (current.Content.Texts.SingleOrDefault(e => e.Id == text.Id) is { } existedText) { // update text
                existedText.Culture = text.Culture;
                existedText.Content = text.Content;
            }
            if (defaultCulture.Id == text.Culture) current.Content.DefaultText = text.Content;
            // remove the same culture images
            foreach (var duplicateText in current.Content.Texts.Where(e => e.Culture == text.Culture && e.Id != text.Id)) {
                context.Remove(duplicateText);
            }
        }

        #endregion
        #region 檢查標籤是否有變動

        var removedReferences = current.NewsTags.Where(e => !data.TagIds.Contains(e.TagId));
        foreach (var reference in removedReferences) current.NewsTags.Remove(reference);
        var addedReferences = data.TagIds.Except(current.TagIds);
        foreach (var reference in addedReferences) current.NewsTags.Add(new NewsTag { NewsId = current.Id, TagId = reference });

        #endregion
    }
}