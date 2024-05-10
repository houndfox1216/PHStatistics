using Newtonsoft.Json;

namespace EmptyProject {
    /// <summary>
    /// 信用卡退貨交易
    /// </summary>
    [JsonObject]
    public class TaishinRefundParam : ITaishinTransaction {
        /// <summary>
        /// 交易類型
        /// </summary>
        [JsonIgnore]
        public int TransactionType => 5;

        /// <summary>
        /// 訂單號碼
        /// </summary>
        [JsonProperty(PropertyName = "order_no"), JsonRequired]
        public string OrderNo { get; set; }

        /// <summary>
        /// 交易金額
        /// </summary>
        [JsonProperty(PropertyName = "amt"), JsonRequired]
        public string Amount { get; set; }

        /// <summary>
        /// 回傳詳細訊息標記
        /// </summary>
        [JsonProperty(PropertyName = "result_flag")]
        public string ResultFlag => "1";
    }
}
