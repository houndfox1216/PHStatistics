using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace EmptyProject.Actions;

/// <summary>
/// 更新新聞資料之操作。
/// </summary>
[Description("更新新聞資料")]
public class UrlSegmentUpdateAction : UpdateActionBase<UrlSegment, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.UrlSegment];

    /// <summary>
    /// 建構 UrlSegmentCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="context">資料脈絡</param>
    public UrlSegmentUpdateAction(IUser user, DataContext context = null) : base("更新新聞資料", user, context) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(context);
            RequiredIncludes = ["Title.Texts"];
        }

    protected override void OnUpdating(DataContext context, UrlSegment data, UrlSegment current) {
        current.PageId = data.PageId;
        var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.First();

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
    }
}