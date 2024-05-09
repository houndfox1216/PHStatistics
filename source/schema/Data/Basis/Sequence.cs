using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Framework.Data;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;

namespace EmptyProject {
    /// <summary>
    /// 序號資料
    /// </summary>
    [Description("序號資料"), DataContract(IsReference = true)]
    public class Sequence : ISequenceData {
        /// <summary>
        /// 實體類型
        /// </summary>
        [Display(Name = "實體類型"), DataMember]
        [Key, Column(Order = 0), DatabaseGenerated(DatabaseGeneratedOption.None)]
        public EntityType EntityType { get; set; }

        /// <summary>
        /// 子碼
        /// </summary>
        [Display(Name = "子碼"), DataMember]
        [Key, Column(Order = 1), Unicode(false), MaxLength(3)]
        public string SubCode { get; set; }

        /// <summary>
        /// 擴充碼
        /// </summary>
        [Display(Name = "擴充碼"), DataMember]
        [Key, Column(Order = 2), Unicode(false), MaxLength(3)]
        public string ExtendCode { get; set; }

        /// <summary>
        /// 序號
        /// </summary>
        [Display(Name = "序號"), DataMember]
        public int Number { get; set; }

        /// <summary>
        /// 最後更新時間
        /// </summary>
        [Display(Name = "最後更新時間"), DataMember]
        public DateTime? LastUpdateTime { get; set; }
    }
}
