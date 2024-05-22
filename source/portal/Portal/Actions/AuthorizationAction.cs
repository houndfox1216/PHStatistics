using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Security;
using System.Linq;

namespace PHStatistics.Portal.Actions {
    /// <summary>
    /// 認證用戶之操作。
    /// </summary>
    [Description("認證用戶")]
    internal class AuthorizationAction : ActionBase<DataContext, SystemPermission> {
        /// <summary>
        /// 建構 AuthorizationAction。
        /// </summary>
        public AuthorizationAction(IUser user, DataContext dbContext = null) : base("認證用戶", user, dbContext) { }

        protected override void OnExecuted(DataContext context) {
            var account = Parameters.GetValue<string>("account");
            var password = Parameters.GetValue<string>("password");
            if (!account.HasValue() || !password.HasValue()) throw new OperationException("帳號或密碼不可空白");
            var hashedPassword = password.ComputeHashStringWithSha().ToBase64();
            var member = context.Member.SingleOrDefault(e => e.Account == account && e.Password == hashedPassword) ?? throw new SecurityException("帳號或密碼不正確");
            Result = member;
        }
    }
}