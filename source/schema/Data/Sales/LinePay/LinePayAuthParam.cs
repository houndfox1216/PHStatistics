using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// ReSharper disable CheckNamespace

namespace EmptyProject;

/// <summary>
/// 請求交易(LinePay Request API)
/// </summary>
[JsonObject]
public class LinePayAuthParam {
    /// <summary>
    /// 付款金額
    /// </summary>
    [JsonProperty("amount"), JsonRequired]
    public string Amount { get; set; }

    /// <summary>
    /// 交易貨幣
    /// </summary>
    [MaxLength(3), JsonProperty("currency"), JsonRequired]
    public string Currency { get; set; } = LinePayDefaults.Currency.Twd;

    /// <summary>
    /// 商家訂單編號(唯一)
    /// </summary>
    [MaxLength(100), JsonProperty("orderId"), JsonRequired]
    public string OrderId { get; set; }

    /// <summary>
    /// 交易資訊
    /// </summary>
    [JsonProperty("packages"), JsonRequired]
    public List<Package> Packages { get; set; }

    /// <summary>
    /// 交易完成導向網址
    /// </summary>
    [JsonProperty("redirectUrls"), JsonRequired]
    public RedirectUrl RedirectUrls { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public class Package {
        /// <summary>
        /// Package 編號(唯一)
        /// </summary>
        [MaxLength(50), JsonProperty("id"), JsonRequired]
        public string Id { get; set; }

        /// <summary>
        /// Package 商品總額
        /// </summary>
        [JsonProperty("amount"), JsonRequired]
        public string Amount { get; set; }

        /// <summary>
        /// Package or Shop 名稱
        /// </summary>
        [MaxLength(100), JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Package 商品
        /// </summary>
        [JsonProperty("products"), JsonRequired]
        public List<Product> Products { get; set; }

        /// <summary>
        /// 商品資訊
        /// </summary>
        public class Product {
            /// <summary>
            /// 識別碼
            /// </summary>
            [MaxLength(50), JsonProperty("id")] public string Id { get; set; }

            /// <summary>
            /// 數量
            /// </summary>
            [JsonProperty("quantity"), JsonRequired]
            public string Quantity { get; set; }

            /// <summary>
            /// 名稱
            /// </summary>
            [Required, MaxLength(4000), JsonProperty("name")]
            public string Name { get; set; }

            /// <summary>
            /// 單價
            /// </summary>
            [JsonProperty("price"), JsonRequired] public string Price { get; set; }
        }
    }

    /// <summary>
    /// 轉跳網址
    /// </summary>
    public class RedirectUrl {
        /// <summary>
        /// 取消付款後轉跳網址
        /// </summary>
        [MaxLength(500), JsonProperty("cancelUrl"), JsonRequired]
        public string Cancel { get; set; }

        /// <summary>
        /// 授權後轉跳網址
        /// </summary>
        [MaxLength(500), JsonProperty("confirmUrl"), JsonRequired]
        public string Confirm { get; set; }
    }
}