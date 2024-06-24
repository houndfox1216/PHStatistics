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
    /// 班系
    /// </summary>
    [Description("班系"), DataContract(IsReference = true)]
    public class CourseDepartment : IEntityData, IOperability {
        #region IEntityData 成員

        object IEntityData.Id { get { return Id; } }
        string IEntityData.Name { get { return this.Name; } }

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
        /// 學科
        /// </summary>
        [Display(Name = "學科"), DataMember]
        public CourseSubject? Subject { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// 排列順序
        /// </summary>
        [Display(Name = "排列順序"), DataMember]
        public int Ordinal { get; set; }

        /// <summary>
        /// 加總
        /// </summary>
        [Display(Name = "加總"), DataMember]
        public bool IsSum { get; set; }
    }
}
