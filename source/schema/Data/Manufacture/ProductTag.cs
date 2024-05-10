using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 商品與標籤之關聯
    /// </summary>
    [Description("商品與標籤之關聯"), DataContract(IsReference = true)]
    public class ProductTag {
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
        /// 標籤識別碼
        /// </summary>
        [Display(Name = "標籤識別碼"), DataMember]
        public int TagId { get; set; }

        /// <summary>
        /// 標籤
        /// </summary>
        [Display(Name = "標籤"), DataMember]
        public Tag Tag { get; set; }
    }
}