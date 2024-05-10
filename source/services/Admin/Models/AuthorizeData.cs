using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PHStatistics.Services.Admin.Models {
    /// <summary>
    /// 授權資料
    /// </summary>
    [Description("授權資料")]
    public class AuthorizeData {
        /// <summary>
        /// 帳號
        /// </summary>
        [Display(Name= "帳號")]
        public string Account { get; set; }

        /// <summary>
        /// 密碼
        /// </summary>
        [Display(Name = "密碼"), DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// 驗證碼
        /// </summary>
        [Display(Name = "驗證碼")]
        public string Captcha { get; set; }

        /// <summary>
        /// 驗證碼令牌
        /// </summary>
        [Display(Name = "驗證碼令牌")]
        public string CaptchaToken { get; set; }
    }
}
