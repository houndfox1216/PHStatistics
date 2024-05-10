using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 商品與屬性值之關聯
    /// </summary>
    [Description("商品與屬性值之關聯"), DataContract(IsReference = true)]
    public class ProductAttributeValue {
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
        /// 屬性值識別碼
        /// </summary>
        [Display(Name = "屬性值識別碼"), DataMember]
        public int AttributeValueId { get; set; }

        /// <summary>
        /// 屬性值
        /// </summary>
        [Display(Name = "屬性值"), DataMember]
        public AttributeValue AttributeValue { get; set; }
    }
}
