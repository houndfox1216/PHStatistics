using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Framework.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace PHStatistics.Content {
    /// <summary>
    /// 人數表項目
    /// </summary>
    [Description("人數表項目"), DataContract(IsReference = true)]
    public class StudentPopulationItemLog : IEntityData, IOperability {
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
        public long Id { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// 分校名稱
        /// </summary>
        [Display(Name = "分校名稱"), DataMember]
        [MaxLength(128)]
        public string SchoolName { get; set; }

        /// <summary>
        /// 變更次數
        /// </summary>
        [Display(Name = "變更次數"), DataMember]
        public int? ChangeCount { get; set; }

        /// <summary>
        /// 人數表識別碼
        /// </summary>
        [Display(Name = "人數表識別碼"), DataMember]
        public long StudentPopulationId { get; set; }

        /// <summary>
        /// 人數表資料
        /// </summary>
        [Display(Name = "人數表資料"), DataMember]
        public StudentPopulation StudentPopulation { get; set; }

        /// <summary>
        /// 班級資料識別碼
        /// </summary>
        [Display(Name = "班級資料識別碼"), DataMember]
        public int? ClassId { get; set; }

        /// <summary>
        /// 班級
        /// </summary>
        [Display(Name = "班級"), DataMember]
        public Class Class { get; set; }

        /// <summary>
        /// 人數
        /// </summary>
        [Display(Name = "人數"), DataMember]
        public int Number { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        public string Remark { get; set; }

        /// <summary>
        /// 學生備註
        /// </summary>
        [Display(Name = "學生備註"), DataMember]
        public string StudentRemark { get; set; }

        /// <summary>
        /// 變更後班級資料識別碼
        /// </summary>
        [Display(Name = "變更後班級資料識別碼"), DataMember]
        public int? ChangeClassId { get; set; }

        /// <summary>
        /// 變更後班級
        /// </summary>
        [Display(Name = "變更後班級"), DataMember]
        public Class ChangeClass { get; set; }

        /// <summary>
        /// 變更後人數
        /// </summary>
        [Display(Name = "變更後人數"), DataMember]
        public int ChangeNumber { get; set; }

        /// <summary>
        /// 變更後備註
        /// </summary>
        [Display(Name = "變更後備註"), DataMember]
        public string ChangeRemark { get; set; }

        /// <summary>
        /// 變更後學生備註
        /// </summary>
        [Display(Name = "變更後學生備註"), DataMember]
        public string ChangeStudentRemark { get; set; }

        /// <summary>
        /// 新增項目
        /// </summary>
        [Display(Name = "新增項目"), DataMember, NotMapped]
        public bool IsNew { get; set; }

        /// <summary>
        /// 新增項目
        /// </summary>
        [Display(Name = "新增項目"), DataMember, NotMapped]
        public bool LastWeekNumber { get; set; }
    }
}
