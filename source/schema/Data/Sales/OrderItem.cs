using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Framework.Business;
using System.Framework.Data;
using System.Framework.Sales;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 訂單項目資料
    /// </summary>
    [Description("訂單項目資料"), DataContract(IsReference = true)]
    public class OrderItem : IOrderItemData {
        #region IOrderItemData members

        #region IItemData members

        #region IEntityData 成員

        object IEntityData.Id => Id;

        #endregion

        IEntityData IItemData.Master => Order;

        #endregion

        IMaterialData IOrderItemData.Product => Product;
        bool IOrderItemData.IsGift => false;
        decimal IOrderItemData.TaxAmount => 0;
        ICollection<IChargeItemData> IOrderItemData.ChargItems => Array.Empty<IChargeItemData>();
        string IOrderItemData.Remark => null;

        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public int Id { get; set; }

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
        /// 資料模式
        /// </summary>
        [Display(Name = "資料模式"), DataMember]
        public DataMode DataMode { get; set; }

        /// <summary>
        /// 編號
        /// </summary>
        [Display(Name = "編號"), DataMember]
        [MaxLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// 訂單識別碼
        /// </summary>
        [Display(Name = "訂單識別碼"), DataMember]
        public int OrderId { get; set; }

        /// <summary>
        /// 訂單資料
        /// </summary>
        [Display(Name = "訂單資料"), DataMember]
        public Order Order { get; set; }

        /// <summary>
        /// 產品資料識別碼
        /// </summary>
        [Display(Name = "產品資料識別碼"), DataMember]
        public int? ProductId { get; set; }

        /// <summary>
        /// 產品資料
        /// </summary>
        [Display(Name = "產品資料"), DataMember]
        public Product Product { get; set; }

        /// <summary>
        /// 原價
        /// </summary>
        [Display(Name = "原價"), DataMember]
        [Decimal(16, 2)]
        public decimal Price { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        [Display(Name = "數量"), DataMember]
        [Decimal(16, 2)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 小計
        /// </summary>
        [Display(Name = "小計"), DataMember, NotMapped]
        [Decimal(16, 2)]
        public decimal Amount { get; set; }
    }
}