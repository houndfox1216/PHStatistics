using System.ComponentModel;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Payment;
using System.Framework.Sales;
using System.Linq;

namespace EmptyProject.Services.Payment.Actions {
    /// <summary>
    /// 授權繳費之操作。
    /// </summary>
    [Description("授權繳費")]
    public class PaymentAuthAction : ActionBase<DataContext, SystemPermission> {
        /// <summary>
        /// 建構 NewsCategoryCreateAction
        /// </summary>
        /// <param name="user">請求操作的會員</param>
        public PaymentAuthAction(IUser user, DataContext dbContext = null) : base("授權繳費", user, dbContext) {
            SetActionLog<ActionLogCommand<ActionLog, DataContext>>(dbContext);
        }

        protected override void OnExecuting(DataContext context) {
            var authData = Parameters.GetValue<AuthData>("authData");
            var paymentId = Parameters.GetValue<int>("paymentId");
            var type = Parameters.GetValue<PaymentType>("type");
            var provider = Parameters.GetValue<string>("provider");

            var service = PaymentService.GetService(type, provider);
            if (service.TransactionTypes.Contains(PaymentTransactionType.Auth)) throw new OperationException("此金流服務無法授權");
            var payment = context.Payment.Find(paymentId);
            Result = service.Auth(payment, authData);
        }

        public PaymentRecord Execute(AuthData authData, int paymentId, PaymentType type, string provider = null) {
            Parameters.Add(nameof(authData), authData);
            Parameters.Add(nameof(paymentId), paymentId);
            Parameters.Add(nameof(type), type);
            Parameters.Add(nameof(provider), provider);
            return Result as PaymentRecord;
        }
    }
}
