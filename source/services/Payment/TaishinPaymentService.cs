using System;
using System.Diagnostics.CodeAnalysis;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Logging;
using System.Framework.Payment;
using System.Framework.Sales;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace EmptyProject {
    public class TaishinPaymentService : PaymentService {
        private static HttpClient Client { get; set; }
        private static string MerchantId { get; set; }
        private static string TerminalId { get; set; }
        private static string PostBackUrl { get; set; }
        private static string ResultUrl { get; set; }

        public TaishinPaymentService() : this(null) { }

        public TaishinPaymentService(DataContext dataContext) : base(dataContext, new[] { PaymentType.CreditCard }, PaymentTransactionType.Auth, PaymentTransactionType.Void, PaymentTransactionType.Capture, PaymentTransactionType.Refund) {
            var configuration = ApplicationContext.Root.Configuration;
            if (Client == null) { // Initial HTTP Client
                var apiUri = configuration["Taishin:ApiUrl"]?.ToString() ?? throw new ArgumentException("請於設定檔中提供 ApiUrl 參數");
                //specify to use TLS 1.2 as default connection
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Client = new HttpClient { BaseAddress = new Uri(apiUri.ToString()) };
            }
            MerchantId ??= configuration["Taishin:MerchantId"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 MerchantId 參數");
            TerminalId ??= configuration["Taishin:TerminalId"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 TerminalId 參數");
            PostBackUrl ??= configuration["Taishin:PostBackUrl"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 PostBackUrl 參數");
            ResultUrl ??= configuration["Taishin:ResultUrl"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 ResultUrl 參數");
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
                payment.Acquier = context.Acquirer.SingleOrDefault(e => e.ShortCode == "Taishin" && e.PaymentType == PaymentType.CreditCard);
                payment.Status = PaymentStatus.Confirming;
                payment.Amount = authData.Amount;

                var sequence = context.PaymentRecord.Count(e => e.Payment.Id == payment.Id && e.Type == PaymentTransactionType.Auth) + 1;
                var record = new PaymentRecord {
                    Payment = payment,
                    Type = PaymentTransactionType.Auth,
                    Number = $"{payment.Order.Number}-{sequence:000}",
                    Time = DateTime.Now.ToTaipeiTime()
                };

                var request = new TaishinRequest<TaishinAuthParam>(new() {
                    Amount = $"{payment.Amount * 100:F0}",
                    CaptureFlag = "0",
                    DeviceLayout = authData.IsMobileDevice ? "2" : "1",
                    OrderNo = record.Number,
                    PostBackUrl = PostBackUrl,
                    ResultFlag = "1",
                    ResultUrl = $"{ResultUrl.TrimEnd('/')}/{record.Number}"
                }) {
                    MerchantId = MerchantId,
                    TerminalId = TerminalId,
                    PaymentType = 1
                };

                record.RequestType = request.GetType().FullName;
                record.Request = JsonConvert.SerializeObject(request);

                var content = new StringContent(record.Request, Encoding.UTF8, "application/json");
                var responseMessage = Client.PostAsync("/auth.ashx", content).ConfigureAwait(false).GetAwaiter().GetResult();
                if (responseMessage.IsSuccessStatusCode) {
                    var responseContent = responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var response = JsonConvert.DeserializeObject<TaishinResponse<TaishinAuthResultParam>>(responseContent);

                    record.ResponseType = response.GetType().FullName;
                    record.Response = responseContent;
                    if (response.Parameters.ReturnCode == "00" && response.Parameters.HppUrl.HasValue()) {
                        payment.Status = PaymentStatus.Authorizing;
                    }
                }

                return record;
            } catch (Exception e) {
                LogManager.GetLogger<TaishinPaymentService>().LogError(e, $"授權失敗: {e.Message}");
                throw;
            } finally {
                if (this.DataContext == null) context.Dispose();
            }
        }

        /// <summary>
        /// 請款
        /// </summary>
        /// <param name="payment">繳費資料</param>
        /// <returns>支付記錄</returns>
        public PaymentRecord Capture([NotNull] Payment payment) {
            if (payment.Status != PaymentStatus.Confirmed) throw new InvalidOperationException("尚未授權");

            var context = this.DataContext as DataContext ?? new DataContext();
            try {
                var authRecord = context.PaymentRecord
                    .Where(e => e.PaymentId == payment.Id && e.Type == PaymentTransactionType.Auth && e.Success == true)
                    .OrderByDescending(e => e.Time)
                    .FirstOrDefault();
                if (authRecord == null) throw new InvalidOperationException("尚未授權");

                var record = new PaymentRecord {
                    Payment = payment,
                    Number = authRecord.Number,
                    Type = PaymentTransactionType.Capture,
                    Time = DateTime.Now.ToTaipeiTime()
                };

                var request = new TaishinRequest<TaishinCaptureParam>(new() {
                    Amount = $"{payment.Amount * 100:F0}",
                    OrderNo = record.Number
                }) {
                    MerchantId = MerchantId,
                    TerminalId = TerminalId,
                    PaymentType = 1
                };

                record.RequestType = request.GetType().FullName;
                record.Request = JsonConvert.SerializeObject(request);

                var content = new StringContent(record.Request, Encoding.UTF8, "application/json");
                var responseMessage = Client.PostAsync("/other.ashx", content).ConfigureAwait(false).GetAwaiter().GetResult();
                if (responseMessage.IsSuccessStatusCode) {
                    var responseContent = responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var response = JsonConvert.DeserializeObject<TaishinResponse<TaishinTransResultParam>>(responseContent);

                    record.ResponseType = response.GetType().FullName;
                    record.Response = responseContent;
                    record.Success = response.Parameters.ReturnCode == "00";
                    if (record.Success == true) payment.Status = PaymentStatus.Captured;
                }

                return record;
            } catch (Exception e) {
                LogManager.GetLogger<TaishinPaymentService>().LogError(e, "Failure to capture");
                throw;
            } finally {
                if (this.DataContext == null) context.Dispose();
            }
        }

        /// <summary>
        /// 廢止
        /// </summary>
        /// <param name="payment">繳費資料</param>
        /// <returns>支付記錄</returns>
        public PaymentRecord Void(Payment payment) {
            if (payment.Status != PaymentStatus.Authorizing) throw new InvalidOperationException("尚未授權");

            var context = this.DataContext as DataContext ?? new DataContext();
            try {
                var authRecord = context.PaymentRecord
                   .Where(e => e.PaymentId == payment.Id && e.Type == PaymentTransactionType.Auth && e.Success == true)
                   .OrderByDescending(e => e.Time)
                   .FirstOrDefault();
                if (authRecord == null) throw new InvalidOperationException("尚未授權");

                var record = new PaymentRecord {
                    Payment = payment,
                    Number = authRecord.Number,
                    Type = PaymentTransactionType.Void,
                    Time = DateTime.Now.ToTaipeiTime()
                };

                var request = new TaishinRequest<TaishinVoidParam>(new() {
                    OrderNo = record.Number
                }) {
                    MerchantId = MerchantId,
                    TerminalId = TerminalId,
                    PaymentType = 1
                };

                record.RequestType = request.GetType().FullName;
                record.Request = JsonConvert.SerializeObject(request);

                var content = new StringContent(record.Request, Encoding.UTF8, "application/json");
                var responseMessage = Client.PostAsync("/other.ashx", content).ConfigureAwait(false).GetAwaiter().GetResult();
                if (responseMessage.IsSuccessStatusCode) {
                    var responseContent = responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var response = JsonConvert.DeserializeObject<TaishinResponse<TaishinTransResultParam>>(responseContent);

                    record.ResponseType = response.GetType().FullName;
                    record.Response = responseContent;
                    record.Success = response.Parameters.ReturnCode == "00";
                    if (record.Success == true) payment.Status = PaymentStatus.Refunded;
                }

                return record;
            } catch (Exception e) {
                LogManager.GetLogger<TaishinPaymentService>().LogError(e, $"請款失敗: {e.Message}");
                throw;
            } finally {
                if (this.DataContext == null) context.Dispose();
            }
        }

        /// <summary>
        /// 退款
        /// </summary>
        /// <param name="payment">繳費資料</param>
        /// <param name="authData">授權資料</param>
        /// <returns>支付記錄</returns>
        public PaymentRecord Refund(Payment payment, AuthData authData) {
            if (payment.Status != PaymentStatus.Captured) throw new InvalidOperationException("尚未請款");

            var context = this.DataContext as DataContext ?? new DataContext();
            try {
                var captureRecord = context.PaymentRecord
                    .Where(e => e.PaymentId == payment.Id && e.Type == PaymentTransactionType.Capture && e.Success == true)
                    .OrderByDescending(e => e.Time)
                    .FirstOrDefault();
                if (captureRecord == null) throw new InvalidOperationException("尚未請款");
                var record = new PaymentRecord {
                    Payment = payment,
                    Number = captureRecord.Number,
                    Type = PaymentTransactionType.Refund,
                    Time = DateTime.Now.ToTaipeiTime()
                };

                var request = new TaishinRequest<TaishinRefundParam>(new() {
                    Amount = $"{payment.Amount * 100:F0}",
                    OrderNo = record.Number
                }) {
                    MerchantId = MerchantId,
                    TerminalId = TerminalId,
                    PaymentType = 1
                };

                record.RequestType = request.GetType().FullName;
                record.Request = JsonConvert.SerializeObject(request);

                var content = new StringContent(record.Request, Encoding.UTF8, "application/json");
                var responseMessage = Client.PostAsync("/other.ashx", content).ConfigureAwait(false).GetAwaiter().GetResult();
                if (responseMessage.IsSuccessStatusCode) {
                    var responseContent = responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var response = JsonConvert.DeserializeObject<TaishinResponse<TaishinTransResultParam>>(responseContent);

                    record.ResponseType = response.GetType().FullName;
                    record.Response = responseContent;
                    record.Success = response.Parameters.ReturnCode == "00";
                    if (record.Success == true) payment.Status = PaymentStatus.Voided;
                }

                return record;
            } catch (Exception e) {
                LogManager.GetLogger<TaishinPaymentService>().LogError(e, $"請款失敗: {e.Message}");
                throw;
            } finally {
                if (this.DataContext == null) context.Dispose();
            }
        }

        public IPaymentRecordData Continue([NotNull] PaymentRecord record, [NotNull] Parameters parameters) {
            var data = parameters.GetValue<TaishinResponse<TaishinTransResultParam>>("data");
            if (!data.Parameters.OrderNo.HasValue()) throw new FrameworkException("資料格式有誤!");

            var context = this.DataContext as DataContext ?? new DataContext();
            try {
                if (record.Type != PaymentTransactionType.Auth && record.Success.HasValue) throw new FrameworkException("資料格式有誤!");

                record.ResponseType = data.GetType().FullName;
                record.Response = JsonConvert.SerializeObject(data);
                record.Success = data.Parameters.ReturnCode == "00" && data.Parameters.AuthCode.HasValue();

                if (record.Success == true) {
                    var payment = context.Payment.Find(record.PaymentId);
                    payment.Status = PaymentStatus.Confirmed;
                    payment.Time = data.Parameters.PurchaseDate.HasValue() ? DateTime.Parse(data.Parameters.PurchaseDate) : DateTime.Now.ToTaipeiTime();
                    payment.Pan = data.Parameters.LastPan;
                }

                return record;
            } catch (Exception e) {
                LogManager.GetLogger<TaishinPaymentService>().LogError(e, $"繼續交易失敗: {e.Message}");
                throw;
            } finally {
                if (this.DataContext == null) context.Dispose();
            }
        }

        public override IPaymentRecordData Auth<TPayment, TAuthData>(TPayment payment, TAuthData authData) => Auth(payment as Payment, authData as AuthData);
        public override IPaymentRecordData Capture<TPayment>(TPayment payment) => Capture(payment as Payment);
        public override IPaymentRecordData Void<TPayment>(TPayment payment) => Void(payment as Payment);
        public override IPaymentRecordData Refund<TPayment, TAuthData>(TPayment payment, TAuthData authData) => Refund(payment as Payment, authData as AuthData);
        public override IPaymentRecordData Continue<TPaymentRecord>(TPaymentRecord record, Parameters parameters = null) => Continue(record as PaymentRecord, parameters);
    }
}