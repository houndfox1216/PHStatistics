using Newtonsoft.Json;

namespace EmptyProject {
    /// <summary>
    /// 信用卡取消授權交易
    /// </summary>
    [JsonObject]
    public class LinePayVoidParam : ILinePayTransaction {
        /// <summary>
        /// 交易編號
        /// </summary>
        [JsonIgnore]
        public string TransactionId { get; set; }
    }
}
