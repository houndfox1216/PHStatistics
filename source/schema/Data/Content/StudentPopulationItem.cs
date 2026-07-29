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
    public class StudentPopulationItem : IEntityData, IOperability {
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
        /// 新增項目
        /// </summary>
        [Display(Name = "新增項目"), DataMember, NotMapped]
        public bool IsNew { get; set; }

        /// <summary>
        /// 上週人數
        /// </summary>
        [Display(Name = "上週人數"), DataMember]
        public int LastWeekNumber { get; set; }

        /// <summary>
        /// 加總項目
        /// </summary>
        [Display(Name = "加總項目"), DataMember]
        public bool IsSum { get; set; }

        /// <summary>
        /// 本週人數變化
        /// </summary>
        [Display(Name = "人數"), DataMember, NotMapped]
        public int WeekNumber { get; set; }

        /// <summary>
        /// 手動調整
        /// </summary>
        [Display(Name = "手動調整"), DataMember]
        public bool IsManual { get; set; }

        /// <summary>
        /// 系統試算值（僅在 IsManual 時由 AttachManualPreviews 填入，不落地）
        /// </summary>
        [Display(Name = "系統試算值"), DataMember, NotMapped]
        public int? PreviewNumber { get; set; }

        /// <summary>
        /// 本年度累計值（僅在渲染「本週總詢問(填單)人數」列時由 AttachManualPreviews 填入，不落地）
        /// </summary>
        [Display(Name = "本年度累計"), DataMember, NotMapped]
        public int? YearToDateNumber { get; set; }
    }
}
