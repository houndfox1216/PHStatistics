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
    /// 課程資料
    /// </summary>
    [Description("課程資料"), DataContract(IsReference = true)]
    public class Course : IEntityData, IOperability {
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
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// 班系資料識別碼
        /// </summary>
        [Display(Name = "班系資料識別碼"), DataMember]
        public int? DepartmentId { get; set; }

        /// <summary>
        /// 班系
        /// </summary>
        [Display(Name = "班系"), DataMember]
        public CourseDepartment Department { get; set; }

        /// <summary>
        /// 加總
        /// </summary>
        [Display(Name = "加總"), DataMember]
        public bool IsSum { get; set; }

        /// <summary>
        /// 排列順序
        /// </summary>
        [Display(Name = "排列順序"), DataMember]
        public int Ordinal { get; set; }

        /// <summary>
        /// 所屬單位
        /// </summary>
        [Display(Name = "所屬單位"), DefaultValue(StudentPopulationType.PH), DataMember]
        public StudentPopulationType Type { get; set; }

    }
}
