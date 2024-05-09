using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Business;
using System.Framework.Data;
using System.Framework.Manufacture;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 包裝內容物
    /// </summary>
    [Description("包裝內容物")]
    public class PackedContent : IPackedContentData {
        #region IPackedContentData members

        IProductData IPackedContentData.Product => Product;

        #region IItemData members

        IEntityData IItemData.Master => Product;

        #region IEntityData members

        object IEntityData.Id => Id;

        string IEntityData.Name => this.GetDefaultName<PackedContent>();

        #endregion

        #endregion

        IMaterialData IPackedContentData.Content => Content;

        #endregion

        #region ISortable memebers

        int ISortable.Ordinal => Ordinal ?? 0;

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
        /// 商品識別碼
        /// </summary>
        [Display(Name = "商品識別碼"), DataMember]
        public int ProductId { get; set; }

        /// <summary>
        /// 商品
        /// </summary>
        [Display(Name = "商品"), DataMember]
        public Product Product { get; set; }

        /// <summary>
        /// 內容物識別碼
        /// </summary>
        [Display(Name = "內容物識別碼"), DataMember]
        public int ContentId { get; set; }

        /// <summary>
        /// 內容物
        /// </summary>
        [Display(Name = "內容物"), DataMember]
        public Product Content { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        [Display(Name = "數量"), DataMember]
        [Decimal(16, 2)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 排序值
        /// </summary>
        [Display(Name = "排序值"), DataMember]
        public int? Ordinal { get; set; }
    }
}