using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 新聞與標籤之關聯
    /// </summary>
    [Description("商品與標籤之關聯"), DataContract(IsReference = true)]
    public class NewsTag {
        /// <summary>
        /// 新聞識別碼
        /// </summary>
        [Display(Name = "新聞識別碼"), DataMember]
        public int NewsId { get; set; }

        /// <summary>
        /// 新聞
        /// </summary>
        [Display(Name = "新聞"), DataMember]
        public News News { get; set; }

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