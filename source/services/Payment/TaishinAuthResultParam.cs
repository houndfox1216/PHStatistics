using Newtonsoft.Json;

namespace EmptyProject {
    /// <summary>
    /// 信用卡授權交易回應
    /// </summary>
    [JsonObject]
    public class TaishinAuthResultParam : TaishinResponseBase {
        /// <summary>
        /// 付款網頁資訊
        /// </summary>
        [JsonProperty(PropertyName = "hpp_url"), JsonRequired]
        public string HppUrl { get; set; }
    }
}