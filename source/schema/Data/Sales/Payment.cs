using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework;
using System.Framework.Business;
using System.Framework.Data;
using System.Framework.Sales;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EmptyProject {
    /// <summary>
    /// 繳費資料
    /// </summary>
    [Description("繳費資料"), DataContract(IsReference = true)]
    public class Payment : IPaymentData {
        #region IPaymentData members

        #region IEntityData members

        object IEntityData.Id => Id;
        string IEntityData.Name => this.GetDefaultName<Payment>();

        #endregion

        IOrderData IPaymentData.Order => Order;
        IUserData IPaymentData.Confirmor => Confirmor;
        IVendorData IPaymentData.Acquier => Acquier;
        string IPaymentData.Token => Pan;
        ICurrencyData IPaymentData.Currency => null;
        DateTime? IPaymentData.SettlementDate => null;
        DateTime? IPaymentData.RemittanceDate => null;
        DateTime? IPaymentData.ConfirmTime => null;
        ICollection<IPaymentRecordData> IPaymentData.Records => new List<IPaymentRecordData>(Records).AsReadOnly();

        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼")]
        public int Id { get; set; }

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
        /// 繳費方式
        /// </summary>
        [Display(Name = "繳費方式")]
        public PaymentType Type { get; set; }

        /// <summary>
        /// 時間
        /// </summary>
        [Display(Name = "時間")]
        public DateTime Time { get; set; }

        /// <summary>
        /// 卡/帳號
        /// </summary>
        [Display(Name = "卡/帳號")]
        [MaxLength(128), Unicode(false)]
        public string Pan { get; set; }

        /// <summary>
        /// 繳費期限
        /// </summary>
        [Display(Name = "繳費期限")]
        public DateTime? Deadline { get; set; }

        /// <summary>
        /// 金額
        /// </summary>
        [Display(Name = "金額")]
        [Decimal(16, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        [Display(Name = "狀態")]
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註")]
        [MaxLength(512)]
        public string Remark { get; set; }

        /// <summary>
        /// 收單行識別碼
        /// </summary>
        [Display(Name = "收單行")]
        public int AcquierId { get; set; }

        /// <summary>
        /// 收單行
        /// </summary>
        [Display(Name = "收單行")]
        public Acquirer Acquier { get; set; }

        /// <summary>
        /// 訂單識別碼
        /// </summary>
        [Display(Name = "訂單識別碼")]
        public int OrderId { get; set; }

        /// <summary>
        /// 訂單
        /// </summary>
        [Display(Name = "申請案件")]
        public Order Order { get; set; }

        /// <summary>
        /// 確認者識別碼
        /// </summary>
        [Display(Name = "確認者")]
        public Guid ConfirmorId { get; set; }

        /// <summary>
        /// 確認者
        /// </summary>
        [Display(Name = "確認者")]
        public User Confirmor { get; set; }

        /// <summary>
        /// 交易紀錄
        /// </summary>
        [Display(Name = "交易紀錄")]
        public ICollection<PaymentRecord> Records { get; set; }

        /// <summary>
        /// 取得收單行訂單編號
        /// </summary>
        public string GetAcquierOrderNo() {
            var record = Records.OrderByDescending(e => e.Time).FirstOrDefault(e => e.Type == PaymentTransactionType.Auth);
            if (record == null) return null;
            switch (record.ResponseType) {
                default: return JsonConvert.DeserializeObject<TaishinRequest<TaishinAuthParam>>(record.Request).Parameters.OrderNo;
            }
        }

        /// <summary>
        /// 取得調閱編號
        /// </summary>
        public string GetRetrievalReferenceNo() {
            var record = Records.OrderByDescending(e => e.Time).FirstOrDefault(e => e.Type == PaymentTransactionType.Auth && e.Success == true);
            if (record == null) return null;
            switch (record.ResponseType) {
                default: return JsonConvert.DeserializeObject<TaishinResponse<TaishinTransResultParam>>(record.Response).Parameters.ReferenceNo;
            }
        }

        /// <summary>
        /// 取得授權碼
        /// </summary>
        public string GetAuthCode() {
            var record = Records.OrderByDescending(e => e.Time).FirstOrDefault(e => e.Type == PaymentTransactionType.Auth && e.Success == true);
            if (record == null || !record.Response.HasValue()) return null;
            return record.Response.FromJson<TaishinResponse<TaishinTransResultParam>>().Parameters.AuthCode;
        }

        /// <summary>
        /// 取得查詢號
        /// </summary>
        public string GetTraceNo() => throw new NotSupportedException();

        /// <summary>
        /// 取得批次編號
        /// </summary>
        /// <param name="type">交易類型</param>
        public string GetBatchNo(PaymentTransactionType type) => throw new NotSupportedException();
    }
}