using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Framework.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PHStatistics.Content {
    /// <summary>
    /// 分校開班狀況
    /// </summary>
    [Description("分校開班狀況"), DataContract(IsReference = true)]
    public class SchoolClass : IEntityData, IOperability {
        #region IEntityData 成員

        object IEntityData.Id { get { return Id; } }
        string IEntityData.Name { get { return string.Empty; } }

        #endregion

        #region IOperability 成員
        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "建立時間")]
        public DateTime? CreatedTime { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        [Display(Name = "更新時間")]
        public DateTime? UpdatedTime { get; set; }

        /// <summary>
        /// 資料模式
        /// </summary>
        [Display(Name = "資料模式")]
        public DataMode DataMode { get; set; }
        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public int Id { get; set; }

        /// <summary>
        /// 班系
        /// </summary>
        [Display(Name = "班系"), DataMember]
        [MaxLength(128)]
        public string CourseDepartment { get; set; }

        /// <summary>
        /// 課程
        /// </summary>
        [Display(Name = "課程"), DataMember]
        [MaxLength(128)]
        public string Course { get; set; }

        /// <summary>
        /// 班級
        /// </summary>
        [Display(Name = "班級"), DataMember]
        [MaxLength(128)]
        public string Class { get; set; }

        /// <summary>
        /// 分校
        /// </summary>
        [Display(Name = "分校"), DataMember]
        [MaxLength(128)]
        public string School { get; set; }
    }
}
