using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

// ReSharper disable once CheckNamespace

namespace EmptyProject; 

/// <summary>
/// 請款交易
/// </summary>
[JsonObject]
public class LinePayCaptureParam : ILinePayTransaction {
    /// <summary>
    /// 交易編號
    /// </summary>
    [JsonIgnore]
    public string TransactionId { get; set; }

    /// <summary>
    /// 交易金額
    /// </summary>
    [JsonProperty(PropertyName = "amount"), JsonRequired]
    public string Amount { get; set; }

    /// <summary>
    /// 交易貨幣
    /// </summary>
    [MaxLength(3), JsonProperty("currency"), JsonRequired]
    public string Currency { get; set; }
        
    /// <summary>
    /// 建構 LinePayCaptureParam
    /// </summary>
    public LinePayCaptureParam() => Currency = LinePayDefaults.Currency.Twd;
}