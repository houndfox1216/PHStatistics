using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;

namespace EmptyProject.Actions;

/// <summary>
/// 新增直播來源之操作。
/// </summary>
[Description("新增直播來源")]
public partial class LiveSourceCreateAction : CreateActionBase<LiveSource, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.LiveSource };

    /// <summary>
    /// 建構 LiveSourceCreateAction
    /// </summary>
    /// <param name="user">請求操作的會員</param>
    public LiveSourceCreateAction(IUser user, DataContext dbContext = null) : base("新增直播來源", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
        }

    protected override void OnTransacting(DataContext context, LiveSource data) {
            if (context.LiveSource.Any(e => e.Entry == data.Entry)) throw new OperationException("串流名稱已存在");
        }

    protected override void OnCreating(DataContext context, LiveSource data) {
            if (User.Data is User user) data.PublisherId = user.Id;

            if (!data.Ordinal.HasValue) {
                var lastRow = context.LiveSource.OrderByDescending(e => e.Ordinal).FirstOrDefault();
                data.Ordinal = (lastRow != null ? lastRow.Ordinal : 0) + 100;
            }

            if (!data.Entry.HasValue() || !IsSafeUrl().IsMatch(data.Entry)) data.Entry = ShortUid.NewId;
            if (!data.Token.HasValue() || !IsSafeUrl().IsMatch(data.Entry)) data.Token = ShortUid.NewId;

            data.Title ??= new MultilingualText { Texts = new HashSet<StringResource>() };
            data.Content ??= new MultilingualText { Texts = new HashSet<StringResource>() };

            var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.OrderBy(e => e.Ordinal).FirstOrDefault();

            var defaultText = data.Title.Texts.FirstOrDefault();
            if (defaultCulture != null) defaultText = data.Title.Texts.FirstOrDefault(e => e.Culture == defaultCulture.Id);
            data.Title.DefaultText = defaultText?.Content;

            defaultText = data.Content.Texts.FirstOrDefault();
            if (defaultCulture != null) defaultText = data.Content.Texts.FirstOrDefault(e => e.Culture == defaultCulture.Id);
            data.Content.DefaultText = defaultText?.Content;
        }

    [GeneratedRegex("^[a-zA-Z0-9_-]{8,}$")]
    private static partial Regex IsSafeUrl();
}