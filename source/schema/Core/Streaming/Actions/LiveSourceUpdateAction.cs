using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;

namespace EmptyProject.Actions;

/// <summary>
/// 更新直播來源之操作。
/// </summary>
[Description("更新直播來源")]
public partial class LiveSourceUpdateAction : UpdateActionBase<LiveSource, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.LiveSource };

    /// <summary>
    /// 建構 LiveSourceUpdateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public LiveSourceUpdateAction(IUser user, DataContext dbContext = null) : base("更新直播來源", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = new[] { "Title.Texts", "Content.Texts" };
        }

    protected override void OnTransacting(DataContext context, LiveSource data) {
            if (context.LiveSource.Any(e => e.Id != data.Id && e.Entry == data.Entry)) throw new OperationException("串流名稱已存在");
        }

    protected override void OnUpdating(DataContext context, LiveSource data, LiveSource current) {
        if (User.Data is User user) data.PublisherId = user.Id;

        if (!data.Entry.HasValue() || !IsSafeUrl().IsMatch(data.Entry)) data.Entry = ShortUid.NewId;
        if (!data.Token.HasValue() || !IsSafeUrl().IsMatch(data.Entry)) data.Token = ShortUid.NewId;

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

    [GeneratedRegex("^[a-zA-Z0-9_-]{8,}$")]
    private static partial Regex IsSafeUrl();
}