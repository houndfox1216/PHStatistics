using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Runtime.Serialization;

namespace PHStatistics {
    /// <summary>
    /// 標籤資料
    /// </summary>
    [Description("標籤資料"), DataContract(IsReference = true)]
    public class Tag : ITagData, ISortable {
        #region IEntityData 成員

        object IEntityData.Id { get { return Id; } }

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
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(32)]
        public string Name { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        [Display(Name = "排序"), DataMember]
        public int? Ordinal { get; set; }
    }
}
