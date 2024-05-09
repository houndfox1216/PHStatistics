using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Business;
using System.Framework.Data;
using System.Framework.Sales;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace EmptyProject {
    /// <summary>
    /// 訂單資料
    /// </summary>
    [Description("訂單資料"), DataContract(IsReference = true)]
    public class Order : IOrderData {
        #region IOrderData members

        #region IEntityData members

        object IEntityData.Id => Id;
        string IEntityData.Name => this.GetDefaultName<Order>();

        #endregion

        decimal IOrderData.ShoppingSubtotal => 0;
        decimal IOrderData.PaymentFeeSubtotal => 0;
        decimal IOrderData.ShippingSubtotal => 0;
        decimal IOrderData.DiscountSubtotal => 0;
        decimal IOrderData.TaxSubtotal => 0;
        IDeviceData IOrderData.Terminal => null;
        string IOrderData.TerminalBatchNo => null;
        IUserData IOrderData.Operator => null;
        ICustomerData IOrderData.Customer => Member;
        ICollection<IOrderItemData> IOrderData.Items => new List<IOrderItemData>(Items ?? Array.Empty<OrderItem>()).AsReadOnly();
        ICollection<IPaymentData> IOrderData.Payments => new List<IPaymentData>(Payments ?? Array.Empty<Payment>()).AsReadOnly();

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
        [MaxLength(16), Index(IsUnique = true)]
        public string Number { get; set; }

        /// <summary>
        /// 下單日期
        /// </summary>
        [Display(Name = "下單日期"), DataMember]
        public DateTime Time { get; set; }

        /// <summary>
        /// 訂單狀態
        /// </summary>
        [Display(Name = "訂單狀態"), DataMember]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// 訂單總額 = SaleAmount + ShippingAmount - CouponAmount - MemberGradeAmount - ActivityAmount - PointsAmount - AdjustAmount 
        /// </summary>
        [Display(Name = "訂單總額"), DataMember]
        [Decimal(16, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(512)]
        public string Remark { get; set; }

        /// <summary>
        /// 會員資料識別碼
        /// </summary>
        [Display(Name = "會員資料"), DataMember]
        public Guid? MemberId { get; set; }

        /// <summary>
        /// 會員資料
        /// </summary>
        [Display(Name = "會員資料"), DataMember]
        public Member Member { get; set; }

        /// <summary>
        /// 訂單項目資料
        /// </summary>
        [Display(Name = "訂單項目資料"), DataMember]
        public ICollection<OrderItem> Items { get; set; }

        /// <summary>
        /// 訂單項目資料
        /// </summary>
        [Display(Name = "訂單項目資料"), DataMember]
        public ICollection<Payment> Payments { get; set; }
    }
}