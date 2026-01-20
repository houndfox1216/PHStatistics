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
    public class Mapping : IEntityData, IOperability {
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
        /// 年度
        /// </summary>
        [Display(Name = "年度")]
        public int Year { get; set; }

        /// <summary>
        /// 學年
        /// </summary>
        [Display(Name = "學年")]
        public int YearStr { get; set; }

        /// <summary>
        /// 周次
        /// </summary>
        [Display(Name = "周次")]
        public int Week { get; set; }

        /// <summary>
        /// 分校
        /// </summary>
        [Display(Name = "分校"), DataMember]
        [MaxLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// 分校編號
        /// </summary>
        [Display(Name = "分校編號"), DataMember]        
        public int SchoolId { get; set; }

        /// <summary>
        /// 班別
        /// </summary>
        [Display(Name = "班別")]
        public string ClassType { get; set; }
        /// <summary>
        /// 班系
        /// </summary>
        [Display(Name = "班系")]
        public string CourseDepartmentName { get; set; }

        /// <summary>
        /// 課程
        /// </summary>
        [Display(Name = "課程")]
        public string CourseName { get; set; }

        /// <summary>
        /// 課程編號
        /// </summary>
        [Display(Name = "課程編號")]
        public int CourseId { get; set; }

        /// <summary>
        /// 人數
        /// </summary>
        [Display(Name = "人數")]
        public int Number { get; set; }
    }
}
