using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace EmptyProject; 

/// <summary>
/// LinePay交易回應電文基礎類別
/// </summary>
[JsonObject]
public class LinePayResponseBase {
    /// <summary>
    /// 回傳碼
    /// </summary>
    [JsonProperty(PropertyName = "returnCode"), JsonRequired]
    public string ReturnCode { get; set; }

    /// <summary>
    /// 回傳訊息
    /// </summary>
    [JsonProperty(PropertyName = "returnMessage")]
    public string ReturnMessage { get; set; }

    /// <summary>
    /// 建構 LinePayResponseBase
    /// </summary>
    public LinePayResponseBase() { }
}