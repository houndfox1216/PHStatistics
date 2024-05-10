using Newtonsoft.Json;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace EmptyProject; 

/// <summary>
/// 請款交易回應
/// </summary>
[JsonObject]
public class LinePayCaptureResultParam : LinePayResponseBase {
    /// <summary>
    /// 籌載
    /// </summary>
    [JsonProperty("info")]
    public Info Payload { get; set; }

    /// <summary>
    /// 資訊
    /// </summary>
    public class Info {
        /// <summary>
        /// 商家回應的訂單編號
        /// </summary>
        [JsonProperty("orderId")] 
        public string OrderId { get; set; }

        /// <summary>
        /// 交易序號（19 digits）
        /// </summary>
        [JsonProperty("transactionId")] 
        public string TransactionId { get; set; }

        /// <summary>
        /// 付款資訊
        /// </summary>
        [JsonProperty("payInfo")]
        public List<PayInfo> Payments { get; set; }

        /// <summary>
        /// 付款資訊
        /// </summary>
        public class PayInfo {
            /// <summary>
            /// 付款方式
            /// 信用卡：CREDIT_CARD
            /// 餘額：BALANCE
            /// 折扣：DISCOUNT(發票金額須扣除)
            /// LINE POINTS：POINT(預設不顯示)
            /// </summary>
            [JsonProperty("method")]
            public string Method { get; set; }

            /// <summary>
            /// 付款金額
            /// </summary>
            [JsonProperty("amount")]
            public string Amount { get; set; }
        }
    }
}