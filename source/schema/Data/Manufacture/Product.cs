using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Framework.Business;
using System.Framework.Data;
using System.Framework.Manufacture;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;

namespace EmptyProject {
    /// <summary>
    /// 商品資料
    /// </summary>
    [Description("商品資料")]
    public class Product : IProductData {
        #region IProductData 成員

        #region IMaterialData members

        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name => this.GetDefaultName<Product>();

        #endregion

        IPictureData IMaterialData.Picture => Picture;
        string IMaterialData.Description => Description != null ? Description.Content : string.Empty;
        ICollection<ICategoryData> IMaterialData.Categories => new List<ICategoryData>(Categories ?? Array.Empty<Category>()).AsReadOnly();
        ICollection<IPurchaseCostData> IMaterialData.PurchaseCosts => Array.Empty<IPurchaseCostData>();

        #endregion

        IVendorData IProductData.Manufacturer => null;
        IProductData IProductData.Base => Base;
        ICollection<IPackedContentData> IProductData.PackedContents => new List<IPackedContentData>(PackedContents ?? Array.Empty<PackedContent>()).AsReadOnly();

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
        [Unicode(false), MaxLength(32)]
        public string Number { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(32)]
        public string Name { get; set; }

        /// <summary>
        /// 型號
        /// </summary>
        [Display(Name = "型號"), DataMember]
        [MaxLength(16), Unicode(false)]
        public string ModelNo { get; set; }

        /// <summary>
        /// 是否為組合商品
        /// </summary>
        [Display(Name = "組合商品"), DataMember]
        public bool Mixed { get; set; }

        /// <summary>
        /// 單位
        /// </summary>
        [Display(Name = "單位"), DataMember]
        [MaxLength(16)]
        public string Unit { get; set; }

        /// <summary>
        /// 長度單位
        /// </summary>
        [Display(Name = "長度單位"), DataMember]
        public LengthUnit LengthUnit { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        [Display(Name = "高度"), DataMember]
        public float? Height { get; set; }

        /// <summary>
        /// 寬度
        /// </summary>
        [Display(Name = "寬度"), DataMember]
        public float? Width { get; set; }

        /// <summary>
        /// 深度
        /// </summary>
        [Display(Name = "深度"), DataMember]
        public float? Depth { get; set; }

        /// <summary>
        /// 質量單位
        /// </summary>
        [Display(Name = "質量單位"), DataMember]
        public WeightUnit WeightUnit { get; set; }

        /// <summary>
        /// 淨重
        /// </summary>
        [Display(Name = "淨重"), DataMember]
        public double? NetWeight { get; set; }

        /// <summary>
        /// 毛重
        /// </summary>
        [Display(Name = "毛重"), DataMember]
        public double? GrossWeight { get; set; }

        /// <summary>
        /// 公量
        /// </summary>
        [Display(Name = "公量"), DataMember]
        public double? ConditionedWeight { get; set; }

        /// <summary>
        /// 簡介
        /// </summary>
        [Display(Name = "簡介"), DataMember]
        [MaxLength(256)]
        public string Introduction { get; set; }

        /// <summary>
        /// 說明
        /// </summary>
        [Display(Name = "說明"), DataMember]
        public StringResource Description { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(512)]
        public string Remark { get; set; }

        /// <summary>
        /// 圖片
        /// </summary>
        [Display(Name = "圖片"), DataMember]
        public Picture Picture { get; set; }

        /// <summary>
        /// 相簿
        /// </summary>
        [Display(Name = "相簿"), DataMember]
        public Album Album { get; set; }

        /// <summary>
        /// 基本款識別碼，用以陳列同款不同樣式或尺寸之商品
        /// </summary>
        [Display(Name = "基本款識別碼"), DataMember]
        public int? BaseId { get; set; }

        /// <summary>
        /// 基本款，用以陳列同款不同樣式或尺寸之商品
        /// </summary>
        [Display(Name = "基本款"), DataMember]
        public Product Base { get; set; }

        /// <summary>
        /// 包裝內容物
        /// </summary>
        [Display(Name = "包裝內容物"), DataMember]
        public ICollection<PackedContent> PackedContents { get; set; }

        /// <summary>
        /// 商品屬性值
        /// </summary>
        [Display(Name = "商品屬性值"), DataMember]
        public ICollection<ProductAttributeValue> ProductAttributeValues { get; set; }

        /// <summary>
        /// 商品與類別之關聯
        /// </summary>
        [Display(Name = "商品與類別之關聯"), DataMember]
        public ICollection<ProductCategory> ProductCategories { get; set; }

        /// <summary>
        /// 相關類別
        /// </summary>
        [Display(Name = "相關類別"), DataMember]
        [NotMapped]
        public IEnumerable<Category> Categories => ProductCategories?.Select(e => e.Category ?? new Category { Id = e.CategoryId }) ?? categories;
        private IEnumerable<Category> categories = null;

        /// <summary>
        /// 相關類別識別碼
        /// </summary>
        [Display(Name = "相關類別識別碼"), DataMember]
        [NotMapped]
        public int[] CategoryIds {
            get => Categories?.Select(e => e.Id).ToArray();
            set {
                if (ProductCategories != null) {
                    ProductCategories.Clear();
                    foreach (var categoryId in value) ProductCategories.Add(new ProductCategory { Product = this, ProductId = Id, CategoryId = categoryId });
                } else categories = value?.Select(e => new Category { Id = e });
            }
        }

        /// <summary>
        /// 商品與標籤之關聯
        /// </summary>
        [Display(Name = "商品與標籤之關聯"), DataMember]
        public ICollection<ProductTag> ProductTags { get; set; }

        /// <summary>
        /// 相關標籤
        /// </summary>
        [Display(Name = "相關標籤"), DataMember]
        [NotMapped]
        public IEnumerable<Tag> Tags => ProductTags?.Select(e => e.Tag ?? new Tag { Id = e.TagId }) ?? tags;
        private IEnumerable<Tag> tags = null;

        /// <summary>
        /// 相關標籤
        /// </summary>
        [Display(Name = "相關標籤"), DataMember]
        [NotMapped]
        public int[] TagIds {
            get => Tags?.Select(e => e.Id).ToArray();
            set {
                if (ProductTags != null) {
                    ProductTags.Clear();
                    foreach (var tagId in value) ProductTags.Add(new ProductTag { Product = this, ProductId = Id, TagId = tagId });
                } else tags = value?.Select(e => new Tag { Id = e });
            }
        }
    }
}
