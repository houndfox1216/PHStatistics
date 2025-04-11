using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHStatistics.Content {
    /// <summary>
    /// 班級類型
    /// </summary>
    [Description("班級類型")]
    public enum ClassType : short {
        /// <summary>
        /// 團體
        /// </summary>
        [Display(Name = "團")]
        Group,

        /// <summary>
        /// 三
        /// </summary>
        [Display(Name = "1V1")]
        Personal,

        /// <summary>
        /// 三
        /// </summary>
        [Display(Name = "1V2")]
        V2,

        /// <summary>
        /// 三
        /// </summary>
        [Display(Name = "1V3")]
        V3,

        /// <summary>
        /// 小
        /// </summary>
        [Display(Name = "小")]
        SubGroup
    }
}
