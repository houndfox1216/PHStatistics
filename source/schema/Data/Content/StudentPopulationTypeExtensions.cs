using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PHStatistics.Content {
    public static class StudentPopulationTypeExtensions {
        /// <summary>
        /// 取得 StudentPopulationType 的中文顯示名稱（取自 [Display(Name=...)]）
        /// </summary>
        public static string GetDisplayName(this StudentPopulationType type) {
            var member = typeof(StudentPopulationType).GetMember(type.ToString()).FirstOrDefault();
            var display = member?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            return display?.Name ?? type.ToString();
        }
    }
}
