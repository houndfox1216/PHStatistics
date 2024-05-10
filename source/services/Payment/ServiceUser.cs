using System;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Web;
using EmptyProject.Services.Payment.Models;

namespace EmptyProject.Services.Payment {
    /// <summary>
    /// 服務用戶
    /// </summary>
    public class ServiceUser : System.Framework.Web.User {
        /// <summary>
        /// 初始化系統用戶
        /// </summary>
        public ServiceUser() { Data = new User(); }

        /// <summary>
        /// 初始化系統用戶，並使用此用戶資料。
        /// </summary>
        /// <param name="data">用戶資料</param>
        public ServiceUser(IUserData data) { Data = data; }

        /// <summary>
        /// 取得或設定用戶編號。設定用戶編號時用戶資料將一併變更為該用戶之資料。
        /// </summary>
        public override string Id {
            get => Data == SystemUser.Default.Data ? "System" : Data?.Id?.ToString();
            set {
                if (!value.HasValue()) return;
                Data = value == "System" ? SystemUser.Default.Data : Http.Context.Model<Model>(SystemUser.Default).Authorize(value);
            }
        }

        /// <summary>
        /// 提交用戶資料
        /// </summary>
        public override void Commit() => throw new NotSupportedException();
    }
}