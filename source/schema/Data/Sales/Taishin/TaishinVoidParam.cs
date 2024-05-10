using Newtonsoft.Json;

namespace EmptyProject {
    /// <summary>
    /// 信用卡取消授權交易
    /// </summary>
    [JsonObject]
    public class TaishinVoidParam : ITaishinTransaction {
        /// <summary>
        /// 交易類型
        /// </summary>
        [JsonIgnore]
        public int TransactionType => 8;

        /// <summary>
        /// 訂單號碼
        /// </summary>
        [JsonProperty(PropertyName = "order_no"), JsonRequired]
        public string OrderNo { get; set; }

        /// <summary>
        /// 回傳詳細訊息標記
        /// </summary>
        [JsonProperty(PropertyName = "result_flag")]
        public string ResultFlag => "1";
    }
}
