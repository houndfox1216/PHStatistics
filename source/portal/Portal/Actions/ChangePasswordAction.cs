using PHStatistics.Community;
using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Security;

namespace PHStatistics.Portal.Actions {
    /// <summary>
    /// 變更密碼之操作。
    /// </summary>
    [Description("變更密碼")]
    public class ChangePasswordAction : UpdateActionBase<Member, DataContext, SystemPermission> {
        /// <summary>
        /// 必須具備的系統權限
        /// </summary>
        public override SystemPermission[] Permissions => Array.Empty<SystemPermission>();

        /// <summary>
        /// 初始化 ChangePasswordAction。
        /// </summary>
        /// <param name="user">請求操作的用戶</param>
        public ChangePasswordAction(IUser user, DataContext dbContext) : base("變更密碼", user, dbContext) { }

        /// <summary>
        /// 當更新實體資料前執行
        /// </summary>
        /// <param name="context">資料脈絡</param>
        /// <param name="data">資料範本</param>
        /// <param name="current">目前資料</param>
        protected override void OnUpdating(DataContext context, Member data, Member current) {
            var oldPassword = Parameters.GetValue<string>("old-password");
            var newPassword = Parameters.GetValue<string>("new-password");

            if (User.Data is not Member member || member.Id != current.Id) throw new SecurityException("須會員本人變更密碼");
            if (current.Password != oldPassword.ComputeHashStringWithSha().ToBase64()) throw new SecurityException("提供的舊密碼不符合");

            current.Password = newPassword.ComputeHashStringWithSha().ToBase64();
            current.PasswordChangedTime = DateTime.Now;
            current.Apply(data);
        }
    }
}
