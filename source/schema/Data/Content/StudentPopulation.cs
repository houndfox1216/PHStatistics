using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Framework.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using PHStatistics.Community;

namespace PHStatistics.Content {
    /// <summary>
    /// 人數表
    /// </summary>
    [Description("人數表"), DataContract(IsReference = true)]
    public class StudentPopulation : IEntityData, IOperability {
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
        /// 分校識別碼
        /// </summary>
        [Display(Name = "分校識別碼"), DataMember]
        public int? SchoolId { get; set; }

        /// <summary>
        /// 分校識別碼
        /// </summary>
        [Display(Name = "分校識別碼"), DataMember]
        public School School { get; set; }

        /// <summary>
        /// 年度
        /// </summary>
        [Display(Name = "年度"), DataMember]
        public int Year { get; set; }

        /// <summary>
        /// 週次
        /// </summary>
        [Display(Name = "週次"), DataMember]
        public int Week { get; set; }

        /// <summary>
        /// 週次起日
        /// </summary>
        [Display(Name = "週次起日"), DataMember]
        public DateTime WeekDate { get; set; }

        /// <summary>
        /// 類型
        /// </summary>
        [Display(Name = "類型"), DefaultValue(StudentPopulationType.PH), DataMember]
        public StudentPopulationType Type { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// 人數表項目
        /// </summary>
        [Display(Name = "人數表項目"), DataMember]
        public ICollection<StudentPopulationItem> Items { get; set; }

        /// <summary>
        /// 人數表項目Log
        /// </summary>
        [Display(Name = "人數表項目Log"), DataMember]
        public ICollection<StudentPopulationItemLog> ItemsLog { get; set; }

        /// <summary>
        /// 人數表狀態
        /// </summary>
        [Display(Name = "人數表狀態"), DataMember]
        public StudentPopulationStatus Status { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        public string Remark { get; set; }

        /// <summary>
        /// 送出時間
        /// </summary>
        [Display(Name = "送出時間")]
        public DateTime? SubmitterTime { get; set; }

        /// <summary>
        /// 使用者(填寫人)識別碼
        /// </summary>
        [Display(Name = " 使用者(申請人)識別碼"), DataMember]
        public Guid? SubmitterId { get; set; }

        /// <summary>
        ///  使用者(填寫人)
        /// </summary>
        [Display(Name = "使用者(申請人)"), DataMember]
        public Member Submitter { get; set; }


        /// <summary>
        /// 確認時間
        /// </summary>
        [Display(Name = "確認時間")]
        public DateTime? ConfirmTime { get; set; }


        /// <summary>
        /// 使用者(確認者)識別碼
        /// </summary>
        [Display(Name = " 使用者(確認者)識別碼"), DataMember]
        public Guid? ConfirmerId { get; set; }

        /// <summary>
        ///  使用者(確認者)
        /// </summary>
        [Display(Name = "使用者(確認者)"), DataMember]
        public User Confirmer { get; set; }

        #region 統計
       

        /// <summary>
        ///  本周英文人數
        /// </summary>
        [Display(Name = "本周英文人數"), DataMember]
        public int ThisWeekEnglishCount { get; set; }
        /// <summary>
        ///  上週英文人數
        /// </summary>
        [Display(Name = "上週英文人數"), DataMember]
        public int LastWeekEnglishCount { get; set; }
        /// <summary>
        ///  本周國文人數
        /// </summary>
        [Display(Name = "本周國文人數"), DataMember]
        public int ThisWeekChineseCount { get; set; }
        /// <summary>
        ///  上週國文人數
        /// </summary>
        [Display(Name = "上週國文人數"), DataMember]
        public int LastWeekChineseCount { get; set; }
        /// <summary>
        ///  本周新增人數
        /// </summary>
        [Display(Name = "本周新增人數"), DataMember]
        public int ThisWeekNewCount { get; set; }
        /// <summary>
        ///  上週流失人數
        /// </summary>
        [Display(Name = "上週流失人數"), DataMember]
        public int LastWeekLostCount { get; set; }

        /// <summary>
        ///  本周總人數
        /// </summary>
        [Display(Name = "本周總人數"), DataMember]
        public int ThisWeekCount { get; set; }

        /// <summary>
        ///  本周總班數
        /// </summary>
        [Display(Name = "本周總班數"), DataMember]
        public int ThisWeekClassCount { get; set; }

        /// <summary>
        ///  本周總班數
        /// </summary>
        [Display(Name = "本周總班數"), DataMember]
        public int TotalInquiryCount { get; set; }
        
        #endregion
    }
}
