using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHStatistics.Content {
    /// <summary>
    /// 學科
    /// </summary>
    [Description("學科")]
    public enum CourseSubject : short {
        /// <summary>
        /// 英語文
        /// </summary>
        [Display(Name = "英語文")]
        English,

        /// <summary>
        /// 國語文
        /// </summary>
        [Display(Name = "國語文")]
        Chinese,

        /// <summary>
        /// 數學
        /// </summary>
        [Display(Name = "數學")]
        Mathematic,

        /// <summary>
        /// 國中理化
        /// </summary>
        [Display(Name = "國中理化")]
        PhysicsAndChemistry,

        /// <summary>
        /// 高中物理
        /// </summary>
        [Display(Name = "高中物理")]
        Physics,

        /// <summary>
        /// 高中化學
        /// </summary>
        [Display(Name = "高中化學")]
        Chemistry,

        /// <summary>
        /// 程式語言
        /// </summary>
        [Display(Name = "程式語言")]
        Coding,

        /// <summary>
        /// 安親課輔
        /// </summary>
        [Display(Name = "安親課輔")]
        AfterSchool,

        /// <summary>
        /// 其他
        /// </summary>
        [Display(Name = "其他")]
        Other = 9,
    }
}
