using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.IO;
using PHStatistics.Community;

namespace PHStatistics.Actions;

/// <summary>
/// 刪除分校人員資料之操作。
/// </summary>
[Description("刪除分校人員資料")]
public class MemberDeleteAction : DeleteActionBase<Member, DataContext, SystemPermission> {
    /// <summary>
    /// 必須具備的系統權限
    /// </summary>
    public override SystemPermission[] Permissions => [SystemPermission.Member];

    /// <summary>
    /// 初始化 MemberCreateAction。
    /// </summary>
    /// <param name="user">請求操作的用戶</param>
    public MemberDeleteAction(IUser user, DataContext dbContext = null) : base("刪除分校人員資料", user, dbContext) { 
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext); 
        }

    protected override void OnDeleting(DataContext context, Member current) {

        }
}