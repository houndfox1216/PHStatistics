using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace EmptyProject; 

/// <summary>
/// 台新銀行交易回應電文基礎類別
/// </summary>
[JsonObject]
public class TaishinResponseBase {
    /// <summary>
    /// 回傳碼
    /// </summary>
    [JsonProperty(PropertyName = "ret_code"), JsonRequired]
    public string ReturnCode { get; set; }

    /// <summary>
    /// 回傳訊息
    /// </summary>
    [JsonProperty(PropertyName = "ret_msg")]
    public string ReturnMessage { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public TaishinResponseBase() { }
}