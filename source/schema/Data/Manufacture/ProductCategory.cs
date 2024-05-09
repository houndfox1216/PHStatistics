using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 商品與類別之關聯
    /// </summary>
    [Description("商品與屬性值之關聯"), DataContract(IsReference = true)]
    public class ProductCategory {
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
        /// 類別識別碼
        /// </summary>
        [Display(Name = "類別識別碼"), DataMember]
        public int CategoryId { get; set; }

        /// <summary>
        /// 類別
        /// </summary>
        [Display(Name = "類別"), DataMember]
        public Category Category { get; set; }
    }
}