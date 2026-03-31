using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Framework.Security;
using System.Linq;
using PHStatistics.Community;

namespace PHStatistics.Actions;

/// <summary>
/// 更新新聞資料之操作。
/// </summary>
[Description("更新新聞資料")]
public class MemberUpdateAction : UpdateActionBase<Member, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Member];

    /// <summary>
    /// 建構 MemberCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public MemberUpdateAction(IUser user, DataContext dbContext = null) : base("更新新聞資料", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
            RequiredIncludes = ["Title.Texts", "Introduction.Texts", "Content.Texts", "Picture.Images", "MemberTags"];
        }

    protected override void OnUpdating(DataContext context, Member data, Member current) {
        if (data.Password.HasValue())
            data.Password = data.Password.ComputeHashStringWithSha().ToBase64();
        else
            data.Password = current.Password;
    }
}