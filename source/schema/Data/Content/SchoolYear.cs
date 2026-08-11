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
    /// 學年度資料
    /// </summary>
    [Description("學年度資料"), DataContract(IsReference = true)]
    public class SchoolYear : IEntityData, IOperability {
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
        /// 學年度
        /// </summary>
        [Display(Name = "學年度"), DataMember]
        public int? Year { get; set; }

        /// <summary>
        /// 西元年
        /// </summary>
        [Display(Name = "西元年"), DataMember]
        public int? ADYear { get; set; }

        /// <summary>
        /// 週次
        /// </summary>
        [Display(Name = "週次"), DataMember]
        public int? Week { get; set; }

        /// <summary>
        /// 週次起始日
        /// </summary>
        [Display(Name = "週次起始日"), DataMember]
        public DateTime WeekStartDate { get; set; }

        /// <summary>
        /// 週次起始日
        /// </summary>
        [Display(Name = "週次結束日"), DataMember]
        public DateTime WeekEndDate { get; set; }

        /// <summary>
        /// 週次起始日
        /// </summary>
        [Display(Name = "輸入結束日"), DataMember]
        public DateTime ImportEndDate { get; set; }

        /// <summary>
        /// 開始輸入時間，留空時沿用週次起始日
        /// </summary>
        [Display(Name = "開始輸入時間"), DataMember]
        public DateTime? InputStartDate { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(128)]
        public string Remark { get; set; }
    }
}

