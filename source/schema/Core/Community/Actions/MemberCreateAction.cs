using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Community;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PHStatistics.Actions;
using Environment = System.Framework.Environment;
using PHStatistics.Community;

// ReSharper disable once CheckNamespace
namespace PHStatistics.Actions;

/// <summary>
/// 新增新聞資料之操作。
/// </summary>
[Description("新增新聞資料")]
public class MemberCreateAction : CreateActionBase<Member, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Member];

    /// <summary>
    /// 建構 MemberCreateAction
    /// </summary>
    /// <param name="user">請求操作的會員</param>
    /// <param name="context">資料脈絡ㄑ</param>
    public MemberCreateAction(IUser user, DataContext context = null) : base("新增分校人員資料", user, context) {
        SetActionLog<ActionLogCommand<ActionLog, DataContext>>(context);
    }

    protected override void OnTransacting(DataContext context, Member data) {
        if (data.Person != null && data.Person.Phone.HasValue()) {
            if (!data.Person.Phone.IsValidPhoneNumber())
                throw new DataException("電話格式不正確");
            data.Person.Phone = data.Person.Phone.FormatAsPhone();
        }
        if (data.Person != null && data.Person.MobilePhone.HasValue()) {
            if (!data.Person.MobilePhone.IsValidPhoneNumber())
                throw new DataException("手機格式不正確");
            data.Person.MobilePhone = data.Person.MobilePhone.FormatAsPhone();
        }
    }

    protected override void OnCreating(DataContext context, Member data) {
        if (!data.Email.HasValue() || context.Member.Any(e => e.Email == data.Email && e.DataMode == DataMode.Normal)) throw new DataException("此Email已被使用");
        if (!data.Account.HasValue() || context.Member.Any(e => e.Account == data.Account && e.DataMode == DataMode.Normal)) throw new DataException("此帳號已被使用");
        if (!data.Number.HasValue() || context.Member.Any(e => e.Number == data.Number && e.DataMode == DataMode.Normal)) throw new DataException("此編號已被使用");
        data.Password = data.Password;
        data.Person ??= new Person();
        data.Person.Address ??= new Address();

        if (data.Photo != null && !String.IsNullOrEmpty(data.Photo.Uri)) {
            var url = data.Photo.Uri;
            var env = Environment.Directory.WebRootPath;
            int filesIndex = url.IndexOf("files", StringComparison.OrdinalIgnoreCase);
            var newUrl = url.Substring(filesIndex);

            var directory = new DirectoryInfo(Environment.GetRealPath(data.Photo.Directory));
            if (!directory.Exists) directory.Create();

            var uploadFile = new FileInfo(Path.Combine(env, newUrl.Trim('/')));
            if (!uploadFile.Exists) throw new OperationException("未上傳圖檔至伺服器");

            var filename = $"{ShortUid.NewId}{uploadFile.Extension}";

            uploadFile.CopyTo(Path.Combine(directory.FullName, filename));

            data.Photo.Uri = $"{data.Photo.Directory.Trim('~')}/{filename}";
           // data.Photo.FilePath = $"{data.Photo.Directory.Trim('~')}/{filename}";

        }
        else data.Photo = new Picture();
    }
}