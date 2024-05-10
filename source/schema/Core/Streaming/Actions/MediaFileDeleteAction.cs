using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Logging;
using FluentFTP;
using FluentFTP.Exceptions;
using Environment = System.Framework.Environment;

namespace EmptyProject.Actions;

/// <summary>
/// 刪除媒體檔案之操作。
/// </summary>
[Description("刪除媒體檔案")]
public class MediaFileDeleteAction : DeleteActionBase<MediaFile, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.MediaFile };

    /// <summary>
    /// 建構 MediaFileDeleteAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public MediaFileDeleteAction(IUser user, DataContext dbContext = null) : base("刪除媒體檔案", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = new[] { "Title.Texts", "Content.Texts" };
        }

    protected override void OnDeleting(DataContext context, MediaFile current) {
            if (current.Title != null) context.Remove(current.Title);
            if (current.Content != null) context.Remove(current.Content);
        }

    protected override void OnDeleted(DataContext context, MediaFile current) {

            var configuration = ApplicationContext.Root.Configuration;
            var host = configuration["FTP:Host"].ToString();
            var username = configuration["FTP:Username"].ToString();
            var password = configuration["FTP:Password"].ToString();

            try {
                var ftpClient = new FtpClient(host) { Credentials = new(username, password) };
                ftpClient.Connect();
                if (ftpClient.FileExists($"/vod/{current.Entry}")) ftpClient.DeleteFile($"/vod/{current.Entry}");
                if (ftpClient.FileExists($"/vod/{current.Entry}.png")) ftpClient.DeleteFile($"/vod/{current.Entry}.png");

                var localPath = Environment.GetRealPath($"~/resources/thumbnails/{current.Entry}.png");
                if (System.IO.File.Exists(localPath)) System.IO.File.Delete(localPath);
            } catch (FtpCommandException fce) {
                Logger.LogError(fce, "刪除影片與截圖時發生錯誤");
                throw new DataException("影片正在播放中，請稍後再試");
            } catch (Exception e) {
                Logger.LogError(e, "刪除影片與截圖時發生錯誤");
                throw;
            }
        }
}