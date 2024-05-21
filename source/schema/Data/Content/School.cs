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
    /// 分校資料
    /// </summary>
    [Description("分校資料"), DataContract(IsReference = true)]
    public class School : IEntityData, IOperability {
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
        /// 分校編號
        /// </summary>
        [Display(Name = "分校編號"), DataMember]
        [MaxLength(16)]
        public string Number { get; set; }

        /// <summary>
        /// 分校代碼
        /// </summary>
        [Display(Name = "分校代碼"), DataMember]
        [MaxLength(16)]
        public string Code { get; set; }

        /// <summary>
        /// 已發佈
        /// </summary>
        [Display(Name = "已發佈"), DataMember]
        public bool Published { get; set; }

        /// <summary>
        /// 發佈開始日
        /// </summary>
        [Display(Name = "發佈開始日"), DataMember]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 發佈結束日
        /// </summary>
        [Display(Name = "發佈結束日"), DataMember]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 排列順序
        /// </summary>
        [Display(Name = "排列順序"), DataMember]
        public int Ordinal { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        public string Remark { get; set; }

        /// <summary>
        /// 分校操作人員
        /// </summary>
        [Display(Name = "分校操作人員"), DataMember]
        public ICollection<SchoolAssignment> SchoolAssignment { get; set; }


    }
}
