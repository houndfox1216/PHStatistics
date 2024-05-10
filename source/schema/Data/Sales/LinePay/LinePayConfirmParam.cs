using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

// ReSharper disable once CheckNamespace

namespace EmptyProject; 

/// <summary>
/// 授權交易(LinePay Confirm API)
/// </summary>
[JsonObject]
public class LinePayConfirmParam : ILinePayTransaction {
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
    /// 建構 LinePayConfirmParam
    /// </summary>
    public LinePayConfirmParam() => Currency = LinePayDefaults.Currency.Twd;
}