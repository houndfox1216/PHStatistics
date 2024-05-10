using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace EmptyProject; 

/// <summary>
/// 信用卡授權交易回應
/// </summary>
[JsonObject]
public class LinePayTransResultParam : LinePayResponseBase {
    /// <summary>
    /// 訂單編號
    /// </summary>
    [JsonProperty(PropertyName = "order_no")]
    public string OrderNo { get; set; }

    /// <summary>
    /// 授權碼
    /// </summary>
    [JsonProperty(PropertyName = "auth_id_resp")]
    public string AuthCode { get; set; }

    /// <summary>
    /// 調單號碼
    /// </summary>
    [JsonProperty(PropertyName = "rrn")]
    public string ReferenceNo { get; set; }

    /// <summary>
    /// 訂單狀態碼
    /// </summary>
    [JsonProperty(PropertyName = "order_status")]
    public string OrderStatus { get; set; }

    /// <summary>
    /// 授權方式
    ///SSL:SSL 授權
    ///3D:3D 驗證
    /// </summary>
    [JsonProperty(PropertyName = "auth_type")]
    public string AuthType { get; set; }

    /// <summary>
    /// 幣別 NTD:新台幣
    /// </summary>
    [JsonProperty(PropertyName = "cur")]
    public string Currency { get; set; }

    /// <summary>
    /// 採購日期   (yyyy-MM-dd HH:mm:ss)
    /// </summary>
    [JsonProperty(PropertyName = "purchase_date")]
    public string PurchaseDate { get; set; }

    /// <summary>
    /// 交易金額
    /// </summary>
    [JsonProperty(PropertyName = "tx_amt")]
    public string TransactionAmount { get; set; }
    /// <summary>
    /// 請款金額
    /// </summary>
    [JsonProperty(PropertyName = "settle_amt")]
    public string CaptureAmount { get; set; }

    /// <summary>
    /// 請款批號
    /// </summary>
    [JsonProperty(PropertyName = "settle_seq")]
    public string CaptureBatchNo { get; set; }

    /// <summary>
    /// 請款日期   (yyyy-MM-dd)
    /// </summary>
    [JsonProperty(PropertyName = "settle_date")]
    public string CaptureDate { get; set; }

    /// <summary>
    /// 退貨金額
    /// </summary>
    [JsonProperty(PropertyName = "refund_trans_amt")]
    public string RefundAmount { get; set; }

    /// <summary>
    /// 退貨調單編號
    /// </summary>
    [JsonProperty(PropertyName = "refund_rrn")]
    public string RefundReferenceNo { get; set; }

    /// <summary>
    /// 退貨授權碼
    /// </summary>
    [JsonProperty(PropertyName = "refund_auth_id_resp")]
    public string RefundAuthCode { get; set; }

    /// <summary>
    /// 退貨日期 (yyyy-MM-dd)
    /// </summary>
    [JsonProperty(PropertyName = "refund_date")]
    public string RefundDate { get; set; }

    /// <summary>
    /// 紅利訂單編號
    /// </summary>
    [JsonProperty(PropertyName = "redeem_order_no")]
    public string RedeemOrderNo { get; set; }

    /// <summary>
    /// 折抵點數
    /// </summary>
    [JsonProperty(PropertyName = "redeem_pt")]
    public string RedeemPoint { get; set; }

    /// <summary>
    /// 折抵金額
    /// </summary>
    [JsonProperty(PropertyName = "redeem_amt")]
    public string RedeemAmount { get; set; }

    /// <summary>
    /// 實付金額
    /// </summary>
    [JsonProperty(PropertyName = "post_redeem_amt")]
    public string PostRedeemAmount { get; set; }

    /// <summary>
    /// 剩餘點數
    /// </summary>
    [JsonProperty(PropertyName = "post_redeem_pt")]
    public string PostRedeemPoint { get; set; }

    /// <summary>
    /// 分期訂單號碼
    /// </summary>
    [JsonProperty(PropertyName = "install_order_no")]
    public string InstallOrderNo { get; set; }

    /// <summary>
    /// 首期金額
    /// </summary>
    [JsonProperty(PropertyName = "install_down_pay")]
    public string InstallDownPayAmount { get; set; }

    /// <summary>
    /// 分期期數
    /// </summary>
    [JsonProperty(PropertyName = "install_period")]
    public string InstallPeriod { get; set; }

    /// <summary>
    /// 每期金額
    /// </summary>
    [JsonProperty(PropertyName = "install_pay")]
    public string InstallPay { get; set; }

    /// <summary>
    /// 首期手續費
    /// </summary>
    [JsonProperty(PropertyName = "install_down_pay_fee")]
    public string InstallDownPayFee { get; set; }

    /// <summary>
    /// 每期手續費
    /// </summary>
    [JsonProperty(PropertyName = "install_pay_fee")]
    public string InstallPayFee { get; set; }

    /// <summary>
    /// 信用卡卡號前 6 碼
    /// </summary>
    [JsonProperty(PropertyName = "first_6_digit_of_pan")]
    public string FirstPan { get; set; }

    /// <summary>
    /// 信用卡卡號後 4 碼
    /// </summary>
    [JsonProperty(PropertyName = "last_4_digit_of_pan")]
    public string LastPan { get; set; }

    /// <summary>
    /// 建構 LinePayTransResultParam
    /// </summary>
    public LinePayTransResultParam() { }
}