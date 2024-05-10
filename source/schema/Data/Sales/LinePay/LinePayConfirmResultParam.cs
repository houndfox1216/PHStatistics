using Newtonsoft.Json;

// ReSharper disable once CheckNamespace

namespace EmptyProject; 

/// <summary>
/// 授權交易回應
/// </summary>
[JsonObject]
public class LinePayConfirmResultParam : LinePayResponseBase {
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
        /// 交易序號 (19 digits)
        /// </summary>
        [JsonProperty("transactionId")]
        public long TransactionId { get; set; }

        /// <summary>
        /// 授權代碼，在LINE Pay可以代替掃描器使用
        /// </summary>
        [JsonProperty("paymentAccessToken")]
        public string AccessToken { get; set; }

        /// <summary>
        /// 付款頁專用URL
        /// </summary>
        [JsonProperty("paymentUrl")]
        public PaymentUrl Url { get; set; }

        /// <summary>
        /// 付款網址
        /// </summary>
        public class PaymentUrl {
            /// <summary>
            /// App 網址
            /// 在應用程式發起付款請求時使用
            /// 在從商家應用跳轉到LINE Pay時使用
            /// </summary>
            [JsonProperty("app")]
            public string App { get; set; }

            /// <summary>
            /// Web 網址
            /// 在網頁請求付款時使用
            /// 在跳轉到LINE Pay等待付款頁時使用
            /// 不經參數，直接跳轉到傳來的URL
            /// 在Desktop版，彈窗大小為Width：700px，Height：546px
            /// </summary>
            [JsonProperty("web")]
            public string Web { get; set; }
        }
    }
}