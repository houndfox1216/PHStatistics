using System.Framework;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
using EmptyProject.Models;
using EmptyProject.Portal.Actions;

namespace EmptyProject.Portal.Models {
    public class Model : HttpModelBase<DataContext> {
        /// <summary>
        /// 過濾條件，Model取回資料時的過濾條件(除了Find)。
        /// </summary>
        public Condition Filter { get; set; }

        /// <summary>
        /// 建構 Model
        /// </summary>
        /// <param name="user">Model 將以此用戶進行各項操作</param>
        public Model() : base("Model") { }

        /// <summary>
        /// 廣告位置領域模型
        /// </summary>
        public BannerPositionModel BannerPosition { get { return GetSubModel<BannerPositionModel>(); } }

        /// <summary>
        /// 透過帳號與密碼認證用戶
        /// </summary>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <returns>經過認證的用戶資料，當認證失敗時為空值</returns>
        public Member Authorize(string account, string password) {
            var action = new AuthorizationAction(CurrentUser, DataContext);
            action.Parameters["account"] = account;
            action.Parameters["password"] = password;
            action.Execute();
            return action.GetResult<Member>();
        }

        /// <summary>
        /// 變更目前用戶的密碼
        /// </summary>
        /// <param name="oldPassword">舊密碼</param>
        /// <param name="newPassword">新密碼</param>
        /// <returns></returns>
        public Member ChangePassword(string oldPassword, string newPassword) {
            var action = new ChangePasswordAction(CurrentUser, DataContext);
            var parameters = new Parameters {
                { "old-password", oldPassword },
                { "new-password", newPassword }
            };
            return action.Update(this.CurrentUser.Data as Member, parameters);
        }

        /// <summary>
        /// 以指定的代碼取得文化特性
        /// </summary>
        /// <param name="code">代碼</param>
        public Culture GetCulture(string code = null) {
            if (!DataContext.Culture.Any()) return new Culture { Id = "zh-TW", Codes = "zh-TW" };
            return DataContext.Culture.Find(code) ?? DataContext.Culture.FirstOrDefault(e => e.IsDefault) ?? DataContext.Culture.First();
        }
    }
}
