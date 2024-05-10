using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;

namespace PHStatistics.Actions;

/// <summary>
/// 新增個人資料之操作。
/// </summary>
[Description("新增個人資料")]
public partial class PersonCreateAction : CreateActionBase<Person, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => new[] { SystemPermission.Person };

    /// <summary>
    /// 初始化 PersonCreateAction。
    /// </summary>
    /// <param name="user">請求操作的個人</param>
    public PersonCreateAction(IUser user, DataContext dbContext) : base("新增個人資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
        }

    /// <summary>
    /// 當實體資料創建前執行
    /// </summary>
    /// <param name="context">資料脈絡</param>
    /// <param name="data">資料範本</param>
    protected override void OnCreating(DataContext context, Person data) {
            if (data.PersonalId.HasValue()) {
                if (!PersonalIdRegex().IsMatch(data.PersonalId)) throw new DataException("請移除英數字外符號，長度至少八碼");
                if (context.Person.Any(e => e.PersonalId == data.PersonalId)) throw new DataException("此護照編號已被使用");
            } else data.PersonalId = null;
            data.Address ??= new Address();
            data.Photo ??= new Picture();
        }

    [GeneratedRegex("[0-9a-zA-Z]{8,}")]
    private static partial Regex PersonalIdRegex();
}