using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Linq;

namespace EmptyProject.Services.Payment.Actions {
    /// <summary>
    /// 讀取用戶資料之操作。
    /// </summary>
    [Description("讀取用戶資料")]
    internal class AuthorizationAction : ActionBase<DataContext, SystemPermission> {
        /// <summary>
        /// 建構 AuthorizationAction。
        /// </summary>
        public AuthorizationAction(DataContext dbContext) : base("讀取用戶資料", SystemUser.Default, dbContext) { }

        protected override void OnExecuted(DataContext context) {
            var id = Parameters.GetValue<Guid>("id");
            var token = Parameters.GetValue<string>("token");
            if (token != null) {
                Result = context.User.SingleOrDefault(e => e.Id == id && e.Token == token) as IUserData;
            } else {
                Result = context.User.SingleOrDefault(e => e.Id == id) as IUserData;
            }
        }
    }
}