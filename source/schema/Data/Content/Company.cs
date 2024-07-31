using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHStatistics.Content {
    /// <summary>
    /// 班系所屬公司
    /// </summary>
    [Description("班系所屬公司")]
    public enum Company : short {
        /// <summary>
        /// 百瀚
        /// </summary>
        [Display(Name = "百瀚")]
        PH,

        /// <summary>
        /// 百世
        /// </summary>
        [Display(Name = "百世")]
        PSJ,

        /// <summary>
        /// 其他
        /// </summary>
        [Display(Name = "其他")]
        Other = 9,
    }
}
