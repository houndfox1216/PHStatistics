using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHStatistics.Content {
    /// <summary>
    /// 人數表狀態
    /// </summary>
    [Description("人數表狀態")]
    public enum StudentPopulationStatus : short {
        /// <summary>
        /// 已建檔
        /// </summary>
        [Display(Name = "已建檔")]
        Documented = 0,

        /// <summary>
        /// 已送出
        /// </summary>
        [Display(Name = "已送出")]
        Pending = 1,

        /// <summary>
        /// 已審核(樣品單使用)
        /// </summary>
        [Display(Name = "已審核")]
        Approved = 2,

        /// <summary>
        /// 已否決
        /// </summary>
        [Display(Name = "已否決")]
        Rejected = 3,

        /// <summary>
        /// 已完成
        /// </summary>
        [Display(Name = "已完成")]
        Finished = 4,
    }
}
