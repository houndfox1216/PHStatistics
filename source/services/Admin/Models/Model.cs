using System.Framework.Application;
using System.Framework.Web;
using EmptyProject.Models;

namespace EmptyProject.Services.Admin.Models {
    /// <summary>
    /// 領域模型
    /// </summary>
    public class Model : HttpModelBase<DataContext> {
        /// <summary>
        /// 建構領域模型
        /// </summary>
        public Model() : base("EmptyNext Admin Service Model") { }

        /// <summary>
        /// 用戶領域模型
        /// </summary>
        public UserModel User => GetSubModel<UserModel>();
    }
}
