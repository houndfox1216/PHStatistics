using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Runtime.Serialization;

namespace PHStatistics.Content {
    /// <summary>
    /// 區域資料
    /// </summary>
    [Description("區域資料"), DataContract(IsReference = true)]
    public class Region : IEntityData, IOperability {
        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name => Name;

        #endregion

        #region IOperability 成員

        [Display(Name = "建立時間")]
        public DateTime? CreatedTime { get; set; }

        [Display(Name = "更新時間")]
        public DateTime? UpdatedTime { get; set; }

        [Display(Name = "資料模式")]
        public DataMode DataMode { get; set; }

        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public int Id { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(64)]
        public string Name { get; set; }

        /// <summary>
        /// 排列順序
        /// </summary>
        [Display(Name = "排列順序"), DataMember]
        public int Ordinal { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(512)]
        public string Remark { get; set; }

        /// <summary>
        /// 分校清單
        /// </summary>
        [Display(Name = "分校清單"), DataMember]
        public ICollection<School> Schools { get; set; }
    }
}
