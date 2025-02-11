using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHStatistics.Content {
    /// <summary>
    /// 人數表類型
    /// </summary>
    [Description("人數表類型")]
    public enum StudentPopulationType : short {
        /// <summary>
        /// 百瀚
        /// </summary>
        [Display(Name = "百瀚")]
        PH,

        /// <summary>
        /// 百倍速
        /// </summary>
        [Display(Name = "百倍速")]
        PHM,

        /// <summary>
        /// PSJ/PS
        /// </summary>
        [Display(Name = "百世")]
        PS,

        /// <summary>
        /// PSJ
        /// </summary>
        [Display(Name = "PSJ")]
        PSJ,

        /// <summary>
        /// 英檢班
        /// </summary>
        [Display(Name = "英檢班")]
        GEPT,

        /// <summary>
        /// 安親課輔
        /// </summary>
        [Display(Name = "安親課輔")]
        AfterSchool
    }
}
