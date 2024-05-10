using System.Framework.Data;
using System.Framework.Web;
using EmptyProject.Services.Payment.Actions;

namespace EmptyProject.Services.Payment.Models {
    public class Model : HttpModelBase<DataContext> {
        /// <summary>
        /// 建構領域模型
        /// </summary>
        public Model() : base("EmptyProject Payment Service Model") { }

        /// <summary>
        /// 透過帳號與密碼認證用戶
        /// </summary>
        /// <param name="id">識別碼</param>
        /// <param name="token">令牌，當為空值時代表不檢查</param>
        /// <returns>經過認證的用戶資料，當認證失敗時為空值</returns>
        public IUserData Authorize(string id, string token = null) {
            var action = new AuthorizationAction(DataContext);
            action.Parameters["id"] = id;
            action.Parameters["token"] = token;
            action.Execute();
            return action.GetResult<IUserData>();
        }
    }
}
