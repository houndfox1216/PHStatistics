using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Framework.Data;
using System.Runtime.Serialization;

namespace EmptyProject {
    /// <summary>
    /// 資源
    /// </summary>
    public class Resource : IResourceData {
        #region IResourceData 成員

        #region IEntityData 成員

        object IEntityData.Id { get { return Uri; } }

        #endregion

        bool IResourceData.Downloadable { get { return false; } }

        #endregion

        /// <summary>
        /// URI
        /// </summary>
        [Display(Name = "URI"), DataMember]
        [Key, MaxLength(2000)]
        public string Uri { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(64)]
        public string Name { get; set; }

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
        /// 內容類型
        /// </summary>
        [Display(Name = "內容類型"), DataMember]
        [MaxLength(32)]
        public string ContentType { get; set; }

        /// <summary>
        /// 內容
        /// </summary>
        [Display(Name = "內容"), DataMember]
        [Column(TypeName = "xml")]
        public string Content { get; set; }
    }
}
