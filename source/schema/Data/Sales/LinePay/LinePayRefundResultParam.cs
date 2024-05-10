using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace EmptyProject; 

/// <summary>
/// 退款交易回應
/// </summary>
[JsonObject]
public class LinePayRefundResultParam : LinePayResponseBase {
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
        /// 退款序號（該次退款產生的新序號, 19 digits）
        /// </summary>
        [JsonProperty("refundTransactionId")] 
        public string TransactionId { get; set; }

        /// <summary>
        /// 退款日期 ISO 8601
        /// </summary>
        [JsonProperty("refundTransactionDate")] 
        public string TransactionDate { get; set; }
    }
}