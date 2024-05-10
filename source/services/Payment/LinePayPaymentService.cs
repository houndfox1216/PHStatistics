using System;
using System.Collections.Generic;
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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EmptyProject {
    public class LinePayPaymentService : PaymentService {
        private static HttpClient Client { get; set; }
        private static string ChannelId { get; set; }
        private static string ChannelSecret { get; set; }
        private static string AuthConfirmedUrl { get; set; }
        private static string AuthCanceledUrl { get; set; }

        public LinePayPaymentService() : this(null) { }

        public LinePayPaymentService(DataContext dataContext) : base(dataContext, new[] { PaymentType.ThirdParty }, PaymentTransactionType.Auth, PaymentTransactionType.Void, PaymentTransactionType.Refund) {
            var configuration = ApplicationContext.Root.Configuration;
            if (Client == null) { // Initial HTTP Client
                var apiUri = configuration["LinePay:ApiUrl"]?.ToString() ?? throw new ArgumentException("請於設定檔中提供 ApiUrl 參數");
                //specify to use TLS 1.2 as default connection
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Client = new HttpClient { BaseAddress = new Uri(apiUri) };
            }
            ChannelId ??= configuration["LinePay:ChannelId"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 ChannelId 參數");
            ChannelSecret ??= configuration["LinePay:ChannelSecret"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 ChannelSecret 參數");
            AuthConfirmedUrl ??= configuration["LinePay:AuthConfirmedUrl"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 AuthConfirmedUrl 參數");
            AuthCanceledUrl ??= configuration["LinePay:AuthCanceledUrl"]?.ToString() ?? throw new InvalidOperationException("請於設定檔中提供 AuthCanceledUrl 參數");
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
                payment.Acquier = context.Acquirer.SingleOrDefault(e => e.ShortCode == "LinePay" && e.PaymentType == PaymentType.CreditCard);
                payment.Status = PaymentStatus.Confirming;
                payment.Amount = authData.Amount;

                var sequence = context.PaymentRecord.Count(e => e.Payment.Id == payment.Id && e.Type == PaymentTransactionType.Auth) + 1;
                var record = new PaymentRecord {
                    Payment = payment,
                    Type = PaymentTransactionType.Auth,
                    Number = $"{payment.Order.Number}-{sequence:000}",
                    Time = DateTime.Now.ToTaipeiTime()
                };

                var order = context.Order.Include("Items").SingleOrDefault(e => e.Id == payment.OrderId);
                var products = new List<LinePayAuthParam.Package.Product>();
                foreach (var item in order.Items) {
                    products.Add(new LinePayAuthParam.Package.Product {
                        Id = item.Id.ToString(),
                        Name = item.Name,
                        Price = $"{item.Price:F0}",
                        Quantity = $"{item.Quantity:F0}"
                    });
                }

                var request = new LinePayAuthParam {
                    OrderId = record.Number,
                    Amount = $"{payment.Amount:F0}",
                    RedirectUrls = new LinePayAuthParam.RedirectUrl {
                        Confirm = AuthConfirmedUrl,
                        Cancel = AuthCanceledUrl,
                    },
                    Packages = new List<LinePayAuthParam.Package> {
                        new LinePayAuthParam.Package {
                            Id = "1",
                            // Name = "車輛租借",
                            Amount = $"{payment.Amount:F0}",
                            Products = products,
                        }
                    }
                };

                record.RequestType = request.GetType().FullName;
                record.Request = JsonConvert.SerializeObject(request);

                var requestUrl = "/v3/payments/request";
                var content = new StringContent(record.Request, Encoding.UTF8, "application/json");
                NormalizeHeader(requestUrl, record.Request);
                var responseMessage = Client.PostAsync(requestUrl, content).ConfigureAwait(false).GetAwaiter().GetResult();
                if (responseMessage.IsSuccessStatusCode) {
                    var responseContent = responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var response = JsonConvert.DeserializeObject<LinePayAuthResultParam>(responseContent);

                    record.ResponseType = response.GetType().FullName;
                    record.Response = responseContent;
                    if (response.ReturnCode == "0000") {
                        payment.Status = PaymentStatus.Authorizing;
                    }
                }

                return record;
            } catch (Exception e) {
                LogManager.GetLogger<LinePayPaymentService>().LogError(e, $"授權失敗: {e.Message}");
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

                var request = new LinePayCaptureParam {
                    Amount = $"{payment.Amount:F0}",
                    TransactionId = record.Number,
                    // Currency = 
                };

                record.RequestType = request.GetType().FullName;
                record.Request = JsonConvert.SerializeObject(request);

                var requestUrl = $"/v3/payments/authorizations/{request.TransactionId}/capture";
                var content = new StringContent(record.Request, Encoding.UTF8, "application/json");
                NormalizeHeader(requestUrl, record.Request);
                var responseMessage = Client.PostAsync(requestUrl, content).ConfigureAwait(false).GetAwaiter().GetResult();
                if (responseMessage.IsSuccessStatusCode) {
                    var responseContent = responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var response = JsonConvert.DeserializeObject<LinePayCaptureResultParam>(responseContent);

                    record.ResponseType = response.GetType().FullName;
                    record.Response = responseContent;
                    record.Success = response.ReturnCode == "0000";
                    if (record.Success == true) payment.Status = PaymentStatus.Captured;
                }

                return record;
            } catch (Exception e) {
                LogManager.GetLogger<LinePayPaymentService>().LogError(e, "Failure to capture");
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

                var request = new LinePayVoidParam {
                    TransactionId = record.Number
                };

                record.RequestType = request.GetType().FullName;
                record.Request = JsonConvert.SerializeObject(request);

                var requestUrl = $"/v3/payments/authorizations/{request.TransactionId}/void";
                var content = new StringContent(record.Request, Encoding.UTF8, "application/json");
                NormalizeHeader(requestUrl, record.Request);
                var responseMessage = Client.PostAsync(requestUrl, content).ConfigureAwait(false).GetAwaiter().GetResult();
                if (responseMessage.IsSuccessStatusCode) {
                    var responseContent = responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var response = JsonConvert.DeserializeObject<LinePayResponseBase>(responseContent);

                    record.ResponseType = response.GetType().FullName;
                    record.Response = responseContent;
                    record.Success = response.ReturnCode == "0000";
                    if (record.Success == true) payment.Status = PaymentStatus.Refunded;
                }

                return record;
            } catch (Exception e) {
                LogManager.GetLogger<LinePayPaymentService>().LogError(e, $"請款失敗: {e.Message}");
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

                var request = new LinePayRefundParam {
                    Amount = $"{payment.Amount:F0}",
                    TransactionId = record.Number
                };

                record.RequestType = request.GetType().FullName;
                record.Request = JsonConvert.SerializeObject(request);

                var requestUrl = $"/v3/payments/{request.TransactionId}/refund";
                var content = new StringContent(record.Request, Encoding.UTF8, "application/json");
                NormalizeHeader(requestUrl, record.Request);
                var responseMessage = Client.PostAsync(requestUrl, content).ConfigureAwait(false).GetAwaiter().GetResult();
                if (responseMessage.IsSuccessStatusCode) {
                    var responseContent = responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var response = JsonConvert.DeserializeObject<LinePayRefundResultParam>(responseContent);

                    record.ResponseType = response.GetType().FullName;
                    record.Response = responseContent;
                    record.Success = response.ReturnCode == "0000";
                    if (record.Success == true) payment.Status = PaymentStatus.Voided;
                }

                return record;
            } catch (Exception e) {
                LogManager.GetLogger<LinePayPaymentService>().LogError(e, $"請款失敗: {e.Message}");
                throw;
            } finally {
                if (this.DataContext == null) context.Dispose();
            }
        }

        public IPaymentRecordData Continue([NotNull] PaymentRecord record, [NotNull] Parameters parameters) {
            var data = parameters.GetValue<LinePayAuthResultParam>("data");
            if (!data.Payload.TransactionId.HasValue()) throw new FrameworkException("資料格式有誤!");

            var context = this.DataContext as DataContext ?? new DataContext();
            try {
                if (record.Type != PaymentTransactionType.Auth && record.Success.HasValue) throw new FrameworkException("資料格式有誤!");
        
                record = Confirm(record);
                return record;
            } catch (Exception e) {
                LogManager.GetLogger<LinePayPaymentService>().LogError(e, $"繼續交易失敗: {e.Message}");
                throw;
            } finally {
                if (this.DataContext == null) context.Dispose();
            }
        }

        /// <summary>
        /// 完成付款
        /// </summary>
        /// <param name="payment">繳費資料</param>
        /// <returns>支付記錄</returns>
        public PaymentRecord Confirm([NotNull] PaymentRecord record) {
            var context = this.DataContext as DataContext ?? new DataContext();
            try {
                var request = new LinePayConfirmParam {
                    Amount = $"{record.Payment.Amount:F0}",
                    TransactionId = record.Number,
                    // Currency = 
                };

                record.Payment = context.Payment.Find(record.PaymentId);
                record.RequestType = request.GetType().FullName;
                record.Request = JsonConvert.SerializeObject(request);

                var requestUrl = $"/v3/payments/{request.TransactionId}/confirm";
                var content = new StringContent(record.Request, Encoding.UTF8, "application/json");
                NormalizeHeader(requestUrl, record.Request);
                var responseMessage = Client.PostAsync(requestUrl, content).ConfigureAwait(false).GetAwaiter().GetResult();
                if (responseMessage.IsSuccessStatusCode) {
                    var responseContent = responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var response = JsonConvert.DeserializeObject<LinePayConfirmResultParam>(responseContent);

                    record.ResponseType = response.GetType().FullName;
                    record.Response = responseContent;
                    record.Success = response.ReturnCode == "0000";
                    if (record.Success == true) record.Payment.Status = PaymentStatus.Confirmed;
                }

                return record;
            } catch (Exception e) {
                LogManager.GetLogger<LinePayPaymentService>().LogError(e, "Failure to capture");
                throw;
            } finally {
                if (this.DataContext == null) context.Dispose();
            }
        }

        private string ComputeHash(string body, string secret) {
            byte[] keyByte = Encoding.UTF8.GetBytes(secret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(body);

            using (var hmacSHA256 = new HMACSHA256(keyByte)) {
                byte[] hashMessage = hmacSHA256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashMessage);
            }
        }

        private void NormalizeHeader(string requestUrl, string body) {
            var nonce = Guid.NewGuid().ToString();
            Client.DefaultRequestHeaders.Add("X-LINE-ChannelId", ChannelId);
            Client.DefaultRequestHeaders.Add("X-LINE-Authorization-Nonce", nonce);

            var auth = ComputeHash($"{ChannelSecret}{requestUrl}{body}{nonce}", ChannelSecret);
            Client.DefaultRequestHeaders.Add("X-Line-Authorization", auth);
        }


        public override IPaymentRecordData Auth<TPayment, TAuthData>(TPayment payment, TAuthData authData) => Auth(payment as Payment, authData as AuthData);
        // 預設會自動請款，暫不使用
        // public override IPaymentRecordData Capture<TPayment>(TPayment payment) => Capture(payment as Payment);
        public override IPaymentRecordData Void<TPayment>(TPayment payment) => Void(payment as Payment);
        public override IPaymentRecordData Refund<TPayment, TAuthData>(TPayment payment, TAuthData authData) => Refund(payment as Payment, authData as AuthData);
        public override IPaymentRecordData Continue<TPaymentRecord>(TPaymentRecord record, Parameters parameters = null) => Continue(record as PaymentRecord, parameters);
    }
}
