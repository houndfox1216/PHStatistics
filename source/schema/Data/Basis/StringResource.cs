using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;

namespace PHStatistics {
    /// <summary>
    /// 文字資源
    /// </summary>
    [Description("文字資源"), DataContract(IsReference = true)]
    public class StringResource : IStringResourceData {
        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name => this.GetDefaultName<StringResource>();

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
        /// 文化特性(代碼)
        /// </summary>
        [Display(Name = "文化特性"), DataMember]
        [MaxLength(64)]
        public string Culture { get; set; }

        /// <summary>
        /// 內容類型
        /// </summary>
        [Display(Name = "內容類型"), DataMember]
        [Unicode(false), MaxLength(32)]
        public string ContentType { get; set; }

        /// <summary>
        /// 內容
        /// </summary>
        [Display(Name = "內容"), DataMember]
        public string Content { get; set; }

        /// <summary>
        /// 可代表此資源之URI
        /// </summary>
        [Display(Name = "URI"), DataMember]
        [MaxLength(2000)]
        public string Uri { get; set; }

        /// <summary>
        /// 是否為預設值
        /// </summary>
        [Display(Name = "預設"), DataMember]
        public bool IsDefault { get; set; }

        /// <summary>
        /// 可下載
        /// </summary>
        [Display(Name = "可下載"), DataMember]
        public bool Downloadable { get; set; }

        /// <summary>
        /// 新資源
        /// </summary>
        [Display(Name = "新資源"), DataMember]
        public bool IsNew { get; set; }
    }
}
