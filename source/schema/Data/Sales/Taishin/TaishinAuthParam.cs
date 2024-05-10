using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace EmptyProject; 

/// <summary>
/// 信用卡授權交易
/// </summary>
[JsonObject]
public class TaishinAuthParam : ITaishinTransaction {
    /// <summary>
    /// 交易類型
    /// </summary>
    [JsonIgnore]
    public int TransactionType => 1;

    /// <summary>
    /// 客戶端版面類型
    /// 1.一般網頁
    /// 2.行動裝置網頁
    /// </summary>
    [JsonProperty(PropertyName = "layout"), JsonRequired]
    public string DeviceLayout { get; set; }

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
    /// 幣別
    /// </summary>
    [JsonProperty(PropertyName = "cur"), JsonRequired]
    public string Currency { get; set; }

    /// <summary>
    /// 訂單說明
    /// </summary>
    [JsonProperty(PropertyName = "order_desc"), JsonRequired]
    public string OrderDesc { get; set; }

    /// <summary>
    /// 授權同步請款標記
    /// 0 不同步請款
    /// 1 同步請款
    /// </summary>
    [JsonProperty(PropertyName = "capt_flag"), JsonRequired]
    public string CaptureFlag { get; set; }

    /// <summary>
    /// 回傳訊息標記
    /// 0:不查詢交易詳情
    /// 1:查詢交易詳情
    /// </summary>
    [JsonProperty(PropertyName = "result_flag"), JsonRequired]
    public string ResultFlag { get; set; }

    /// <summary>
    /// 指定接續網址
    /// </summary>
    [JsonProperty(PropertyName = "post_back_url"), JsonRequired]
    public string PostBackUrl { get; set; }

    /// <summary>
    /// 指定交易回傳網址
    /// </summary>
    [JsonProperty(PropertyName = "result_url"), JsonRequired]
    public string ResultUrl { get; set; }

    /// <summary>
    /// Install Period
    /// </summary>
    [JsonProperty(PropertyName = "install_period")]
    public string InstallPeriod { get; set; }

    /// <summary>
    /// Use Redeem
    /// </summary>
    [JsonProperty(PropertyName = "use_redeem")]
    public string UseRedeem { get; set; }

    /// <summary>
    /// 建構 TaishinAuthParam
    /// </summary>
    public TaishinAuthParam() => Currency = "NTD";
}