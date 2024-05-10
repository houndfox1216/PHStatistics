using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Linq;
using FluentFTP;

using Environment = System.Framework.Environment;

namespace EmptyProject.Actions;

/// <summary>
/// 新增媒體檔案之操作。
/// </summary>
[Description("新增媒體檔案")]
public class MediaFileCreateAction : CreateActionBase<MediaFile, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.MediaFile };

    /// <summary>
    /// 建構 FaqCreateAction
    /// </summary>
    /// <param name="user">請求操作的會員</param>
    public MediaFileCreateAction(IUser user, DataContext dbContext = null) : base("新增媒體檔案", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
        }

    protected override void OnCreating(DataContext context, MediaFile data) {
            if (User.Data is User user) data.PublisherId = user.Id;

            if (!data.Ordinal.HasValue) {
                var lastRow = context.MediaFile.OrderByDescending(e => e.Ordinal).FirstOrDefault();
                data.Ordinal = (lastRow != null ? lastRow.Ordinal : 0) + 100;
            }

            data.Title ??= new MultilingualText { Texts = new HashSet<StringResource>() };
            data.Content ??= new MultilingualText { Texts = new HashSet<StringResource>() };

            var defaultCulture = context.Culture.FirstOrDefault(e => e.IsDefault) ?? context.Culture.OrderBy(e => e.Ordinal).FirstOrDefault();

            StringResource defaultText;
            if (data.Title.DefaultText.HasValue() && data.Title.Texts == null) {
                data.Title.Texts = new HashSet<StringResource> { new StringResource { Culture = defaultCulture.Id, Content = data.Title.DefaultText } };
            } else {
                defaultText = data.Title.Texts.FirstOrDefault();
                if (defaultCulture != null) defaultText = data.Title.Texts.FirstOrDefault(e => e.Culture == defaultCulture.Id);
                data.Title.DefaultText = defaultText?.Content;
            }

            defaultText = data.Content.Texts.FirstOrDefault();
            if (defaultCulture != null) defaultText = data.Content.Texts.FirstOrDefault(e => e.Culture == defaultCulture.Id);
            data.Content.DefaultText = defaultText?.Content;

            var configuration = ApplicationContext.Root.Configuration;
            var host = configuration["FTP:Host"].ToString();
            var username = configuration["FTP:Username"].ToString();
            var password = configuration["FTP:Password"].ToString();

            var ftpClient = new FtpClient(host) { Credentials = new(username, password) };
            ftpClient.Connect();
            if (ftpClient.FileExists($"/vod/{data.Entry}.png")) {
                var localPath = Environment.GetRealPath($"~/resources/thumbnails/{data.Entry}.png");
                ftpClient.DownloadFile(localPath, $"/vod/{data.Entry}.png");
                data.ThumbnailUri = $"/resources/thumbnails/{data.Entry}.png";
            }
        }
}