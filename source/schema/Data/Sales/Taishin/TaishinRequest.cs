using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace EmptyProject; 

/// <summary>
/// 台新銀行交易請求電文
/// </summary>
[JsonObject]
public class TaishinRequest<T> {
    /// <summary>
    /// 傳送端程式類型
    /// 固定值:rest
    /// </summary>
    [JsonProperty(PropertyName = "sender"), JsonRequired]
    public string Sender => "rest";

    /// <summary>
    /// 格式版本號
    /// 固定值:1.0.0
    /// </summary>
    [JsonProperty(PropertyName = "ver"), JsonRequired]
    public string Ver => "1.0.0";

    /// <summary>
    /// 特店代號
    /// </summary>
    [JsonProperty(PropertyName = "mid"), JsonRequired]
    public string MerchantId { get; set; }

    /// <summary>
    /// 次特店代號
    /// </summary>
    [JsonProperty(PropertyName = "s_mid", NullValueHandling = NullValueHandling.Ignore)]
    public string SubMerchantId { get; set; }

    /// <summary>
    /// 端末機代號
    /// </summary>
    [JsonProperty(PropertyName = "tid"), JsonRequired]
    public string TerminalId { get; set; }

    /// <summary>
    /// 付款類別
    /// 1:信用卡
    /// 2:銀聯卡
    /// </summary>
    [JsonProperty(PropertyName = "pay_type"), JsonRequired]
    public int PaymentType { get; set; }

    /// <summary>
    /// 交易類型
    /// 1:授權 
    /// 3:請款
    /// 4:取消請款
    /// 5:退貨
    /// 6:取消退貨(銀聯卡 UnionPay 無此功能)
    /// 7:查詢
    /// 8:取消授權
    /// </summary>
    [JsonProperty(PropertyName = "tx_type"), JsonRequired]
    public int TransactionType { get; }

    /// <summary>
    /// 交易要求參數
    /// </summary>
    [JsonProperty(PropertyName = "params")]
    [JsonRequired]
    public T Parameters { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="param"></param>
    public TaishinRequest(T param) {
        if (param == null) return;
        TransactionType = ((ITaishinTransaction)param).TransactionType;
        Parameters = param;
    }
}