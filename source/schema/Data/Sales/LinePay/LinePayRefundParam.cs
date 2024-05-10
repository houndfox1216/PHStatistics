using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace EmptyProject {
    /// <summary>
    /// 退款交易
    /// </summary>
    [JsonObject]
    public class LinePayRefundParam : ILinePayTransaction {
        /// <summary>
        /// 交易編號
        /// </summary>
        [JsonIgnore]
        public string TransactionId { get; set; }

        /// <summary>
        /// 退款金額
        /// </summary>
        [JsonProperty("refundAmount")]
        public string Amount { get; set; }
    }
}
