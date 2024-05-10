using System.ComponentModel;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace PHStatistics.Actions;

/// <summary>
/// 更新新聞資料之操作。
/// </summary>
[Description("更新新聞資料")]
public class PageUpdateAction : UpdateActionBase<Page, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Page];

    /// <summary>
    /// 建構 PageCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    /// <param name="context">資料脈絡</param>
    public PageUpdateAction(IUser user, DataContext context = null) : base("更新新聞資料", user, context) {
        SetActionLog<ActionLogCommand<ActionLog, DataContext>>(context);
        RequiredIncludes = ["Content.Texts"];
    }

    protected override void OnUpdating(DataContext context, Page data, Page current) {
        var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.First();

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
    }
}