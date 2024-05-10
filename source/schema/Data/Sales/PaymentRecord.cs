using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Framework.Data;
using System.Framework.Sales;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace EmptyProject {
    /// <summary>
    /// 支付紀錄
    /// </summary>
    [Description("支付紀錄"), DataContract(IsReference = true)]
    public class PaymentRecord : IPaymentRecordData {
        #region IPaymentRecordData members

        IPaymentData IPaymentRecordData.Payment => Payment;

        #region IEntityData members

        object IEntityData.Id => Id;

        string IEntityData.Name => this.GetDefaultName<PaymentRecord>();

        #endregion

        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼")]
        public long Id { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "建立時間")]
        public DateTime? CreatedTime { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        [Display(Name = "更新時間")]
        public DateTime? UpdatedTime { get; set; }

        /// <summary>
        /// 資料模式
        /// </summary>
        [Display(Name = "資料模式")]
        public DataMode DataMode { get; set; }

        /// <summary>
        /// 編號
        /// </summary>
        [Display(Name = "編號")]
        [MaxLength(30)]
        public string Number { get; set; }

        /// <summary>
        /// 交易類型
        /// </summary>
        [Display(Name = "交易類型")]
        public PaymentTransactionType Type { get; set; }

        /// <summary>
        /// 時間
        /// </summary>
        [Display(Name = "時間")]
        public DateTime Time { get; set; }

        /// <summary>
        /// 請求之型別(用於反序列化)
        /// </summary>
        [Display(Name = "請求型別")]
        [MaxLength(128)]
        public string RequestType { get; set; }

        /// <summary>
        /// 請求之序列化資料(XML)
        /// </summary>
        [Display(Name = "請求資料")]
        [Column(TypeName = "xml")]
        public string Request { get; set; }

        /// <summary>
        /// 回應之型別(用於反序列化)
        /// </summary>
        [Display(Name = "回應型別")]
        [MaxLength(128)]
        public string ResponseType { get; set; }

        /// <summary>
        /// 回應之序列化資料(XML)
        /// </summary>
        [Display(Name = "回應資料")]
        [Column(TypeName = "xml")]
        public string Response { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        [Display(Name = "是否成功")]
        public bool? Success { get; set; }

        /// <summary>
        /// 支付資料識別碼
        /// </summary>
        [Display(Name = "支付資料")]
        public int PaymentId { get; set; }

        /// <summary>
        /// 支付資料
        /// </summary>
        [Display(Name = "支付資料")]
        public Payment Payment { get; set; }
    }
}