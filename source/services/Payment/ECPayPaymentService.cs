using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Logging;
using System.Framework.Payment;
using System.Framework.Sales;
using System.Framework.Security;
using System.Linq;
using System.Web;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable once CheckNamespace
namespace EmptyProject;

// ReSharper disable once InconsistentNaming
public class ECPayPaymentService : PaymentService {
    private static string HashKey { get; set; }
    private static string HashIv { get; set; }
    private static string ReturnUrl { get; set; }
    private static string OrderResultUrl { get; set; }

    public ECPayPaymentService() : this(null) { }

    public ECPayPaymentService(DataContext dataContext) : base(dataContext, new[] { PaymentType.CreditCard, PaymentType.Atm }, PaymentTransactionType.Auth) {
        var configuration = ApplicationContext.Root.Configuration;
        HashKey ??= configuration["ECPay:HashKey"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 HashKey 參數");
        HashIv ??= configuration["ECPay:HashIV"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 HashIV 參數");
        ReturnUrl ??= configuration["ECPay:ReturnURL"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 ReturnURL 參數");
        OrderResultUrl ??= configuration["ECPay:OrderResultURL"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 OrderResultURL 參數");
    }

    /// <summary>
    /// 授權
    /// </summary>
    /// <param name="payment">繳費資料</param>
    /// <param name="authData">授權資料</param>
    /// <returns>支付記錄</returns>
    public PaymentRecord Auth([NotNull] Payment payment, [NotNull] AuthData authData) {
        if (payment.Status > PaymentStatus.Voided) throw new InvalidOperationException($"目前狀態無法授權: {payment.Status}");

        var context = DataContext as DataContext ?? new DataContext();
        try {
            payment.Acquier = context.Acquirer.SingleOrDefault(e => e.ShortCode == "ECPay" && e.PaymentType == PaymentType.CreditCard);
            payment.Status = PaymentStatus.Confirming;
            payment.Amount = authData.Amount;

            var sequence = context.PaymentRecord.Count(e => e.Payment.Id == payment.Id && e.Type == PaymentTransactionType.Auth) + 1;
            var record = new PaymentRecord {
                Payment = payment,
                Type = PaymentTransactionType.Auth,
                Number = $"{payment.Order.Number}-{sequence:000}",
                Time = DateTime.Now.ToTaipeiTime()
            };

            var order = context.Order.Find(payment.OrderId);

            var request = new Dictionary<string, string> {
                { "MerchantTradeNo", record.Number },
                { "MerchantTradeDate", record.Time.ToString("yyyy/MM/dd HH:mm:ss") },
                { "TotalAmount", payment.Amount.ToString("0") },
                { "TradeDesc", authData.Description },
                { "Remark", order?.Remark },
                { "PaymentType", "aio" }, // 固定值
                { "NeedExtraPaidInfo", "1" },
                { "EncryptType", "1" }, // 固定值
                { "DeviceSource", "0" }, // PC Only
                { "ChoosePayment", authData.PaymentType == PaymentType.Atm ? "WebATM" : "Credit" },
                { "ItemName", authData.ItemName },
                { "CustomField1", payment.OrderId.ToString() },
                { "CustomField2", order?.MemberId.ToString() },
                { "ReturnURL", ReturnUrl },
                // { "ClientBackURL" , $"{host}/Order/Checkout" },
                { "OrderResultURL", OrderResultUrl },
            };

            record.RequestType = request.GetType().FullName;
            record.Request = JsonConvert.SerializeObject(request);
            return record;
        } catch (Exception e) {
            LogManager.GetLogger<TaishinPaymentService>().LogError(e, $"授權失敗: {e.Message}");
            throw;
        } finally {
            if (this.DataContext == null) context.Dispose();
        }
    }

    public IPaymentRecordData Continue([NotNull] PaymentRecord record, [NotNull] Parameters parameters) {
        if (record.Type != PaymentTransactionType.Auth && record.Success.HasValue) throw new FrameworkException("目前狀態無法繼續");

        var context = DataContext as DataContext ?? new DataContext();
        try {
            var orderId = parameters.GetValue<int>("CustomField1");
            var memberId = parameters.GetValue<Guid>("CustomField2");
            var payment = context.Payment.Include("Order").SingleOrDefault(e => e.Id == record.PaymentId);
            if (payment?.OrderId != orderId || payment.Order.MemberId != memberId) throw new FrameworkException("回應資料有誤");

            var checkCode = parameters.GetValue<string>("CheckMacValue");
            parameters["CheckMacValue"] = GetCheckMacValue(parameters);
            if (checkCode != parameters.GetValue<string>("CheckMacValue")) throw new FrameworkException("檢查碼驗證錯誤");

            record.ResponseType = parameters.GetType().FullName;
            record.Response = JsonConvert.SerializeObject(parameters);
            record.Success = parameters.GetValue<string>("RtnCode") == "1";

            if (record.Success == true) {
                payment.Status = record.Success == true ? PaymentStatus.Confirmed : PaymentStatus.Confirming;
                payment.Time = DateTime.Parse(parameters.GetValue<string>("PaymentDate"));
            }

            return record;
        } catch (Exception e) {
            LogManager.GetLogger<TaishinPaymentService>().LogError(e, $"繼續交易失敗: {e.Message}");
            throw;
        } finally {
            if (this.DataContext == null) context.Dispose();
        }
    }

    // 用來比對傳遞參數是否一致
    private static string GetCheckMacValue(IDictionary<string, object> parameters, bool useSha = true) {
        parameters.Remove("HashKey");
        parameters.Remove("HashIV");

        var queryParam = parameters.OrderBy(e => e.Key).Select(e => $"{e.Key}={e.Value}");

        // 產生檢查碼。
        var checkValue = string.Join("&", queryParam);
        checkValue = $"HashKey={HashKey}&{checkValue}&HashIV={HashIv}";
        checkValue = HttpUtility.UrlEncode(checkValue).ToLower();
        checkValue = useSha ? checkValue.ComputeHashStringWithSha().ToHexString() : checkValue.ComputeHashStringWithMd5().ToHexString();
        return checkValue.ToUpper();
    }

    public override IPaymentRecordData Auth<TPayment, TAuthData>(TPayment payment, TAuthData authData) => Auth(payment as Payment, authData as AuthData);

    public override IPaymentRecordData Continue<TPaymentRecord>(TPaymentRecord record, Parameters parameters = null) =>
        Continue(record as PaymentRecord, parameters);
}