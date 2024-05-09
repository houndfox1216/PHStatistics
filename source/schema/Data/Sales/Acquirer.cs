using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Business;
using System.Framework.Data;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 收單機構
    /// </summary>
    [Description("收單機構"), DataContract(IsReference = true)]
    public class Acquirer : IVendorData, IOperability {
        #region IVendorData 成員

        #region IMerchantData 成員

        #region IPersonData 成員

        #region IEntityData

        object IEntityData.Id { get { return Id; } }

        #endregion

        string IPersonData.Number => null;
        string IContactData.ShortName => null;
        string IContactData.Phone => null;
        string IContactData.Fax => null;
        string IContactData.Email => null;
        IAddressData IContactData.Address => null;
        string IContactData.Url => null;

        #endregion

        INaturalPersonData IMerchantData.Principal => null;
        IGradeData IMerchantData.Grade => null;
        TaxType IMerchantData.TaxType => TaxType.Zero;
        string IMerchantData.InvoiceTitle => null;
        string IMerchantData.TaxId => null;
        short IMerchantData.SettleDay => 0;
        IAddressData IMerchantData.InvoiceAddress => null;
        INaturalPersonData IMerchantData.ContactPerson => null;
        INaturalPersonData IMerchantData.Accountant => null;
        ICollection<IContractData> IMerchantData.Contracts => null;

        #endregion

        ICollection<ICategoryData> IVendorData.Categories => null;
        IAddressData IVendorData.FactoryAddress => null;
        ICertificateData IPersonData.Certificate => null;

        #endregion

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
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼")]
        public int Id { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱")]
        [MaxLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// 收單行代碼
        /// </summary>
        [Display(Name = "收單行代碼")]
        [MaxLength(20)]
        public string ShortCode { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        [Display(Name = "支付方式")]
        public PaymentType PaymentType { get; set; }
    }
}