using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Framework.Globalization;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 文化特性
    /// </summary>
    [Description("文化特性"), DataContract(IsReference = true)]
    [ProxyEntity]
    public class Culture : ICulture, IEntityData {
        #region IEntityData members

        object IEntityData.Id => Id;

        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(64)]
        public string Name { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "建立時間"), DataMember]
        public DateTime? CreatedTime { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        [Display(Name = "更新時間"), DataMember]
        public DateTime? UpdatedTime { get; set; }

        /// <summary>
        /// 相關代碼，包含所有符合此此文化特性的已登記代碼(IANA registry)，並以逗號隔開
        /// </summary>
        [MaxLength(128)]
        public string Codes { get; set; }

        /// <summary>
        /// 語言代碼，參照ISO 639，可利用LanguageCode代入
        /// </summary>
        [Display(Name = "語言代碼"), DataMember]
        [MaxLength(16)]
        public string Language { get; set; }

        /// <summary>
        /// 書寫代碼，參照ISO 15924，可利用ScriptCode代入
        /// </summary>
        [Display(Name = "書寫方式"), DataMember]
        [MaxLength(16)]
        public string Script { get; set; }

        /// <summary>
        /// 書寫方向
        /// </summary>
        [Display(Name = "書寫方向"), DataMember]
        public ScriptDirection? ScriptDirection { get; set; }

        /// <summary>
        /// 地域代碼，參照ISO 1366，可利用CountrtCode(ISO 1366-1)代入
        /// </summary>
        [Display(Name = "地域代碼"), DataMember]
        [MaxLength(16)]
        public string Region { get; set; }

        /// <summary>
        /// 貨幣代碼，參照ISO ISO 4217，可利用CurrencyCode代入
        /// </summary>
        [Display(Name = "貨幣"), DataMember]
        [MaxLength(16)]
        public string Currency { get; set; }

        /// <summary>
        /// 日期格式
        /// </summary>
        [Display(Name = "日期格式"), DataMember]
        [MaxLength(64)]
        public string DateFormat { get; set; }

        /// <summary>
        /// 時間格式
        /// </summary>
        [Display(Name = "時間格式"), DataMember]
        [MaxLength(64)]
        public string TimeFormat { get; set; }

        /// <summary>
        /// 數值格式
        /// </summary>
        [Display(Name = "數值格式"), DataMember]
        [MaxLength(64)]
        public string NumberFormat { get; set; }

        /// <summary>
        /// 貨幣格式
        /// </summary>
        [Display(Name = "貨幣格式"), DataMember]
        [MaxLength(64)]
        public string CurrencyFormat { get; set; }

        /// <summary>
        /// 排列順序
        /// </summary>
        [Display(Name = "排列順序"), DataMember]
        public int Ordinal { get; set; }

        /// <summary>
        /// 圖片
        /// </summary>
        [Display(Name = "圖片"), DataMember]
        public Picture Picture { get; set; }

        /// <summary>
        /// 是否為預設值
        /// </summary>
        [Display(Name = "預設"), DataMember]
        public bool IsDefault { get; set; }
    }
}
