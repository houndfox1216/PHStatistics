using System.Framework.Web;
using System.IO;
using PHStatistics.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;

namespace PHStatistics.Services.Admin.Controllers {
    /// <summary>
    /// Captcha API
    /// </summary>
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class CaptchaController : ApiController<ServiceUser, UserModel, Culture> {
        /// <summary>
        /// 建構
        /// </summary>
        public CaptchaController() : base("System") { }

        /// <summary>
        /// 取得驗證碼
        /// </summary>
        /// <param name="length">驗證碼長度。預設長度為4</param>
        /// <param name="token">令牌。未指定表示由系統產生，可於檔名或Header取得</param>
        /// <response code="200">回應驗證碼PNG圖檔，檔名為指定或自動產生的token值</response>
        [HttpGet]
        [Produces("image/png")]
        public IActionResult Get(int length = 4, string token = null) {
            var captcha = CaptchaProvider.GetCaptcha(length, 22, 16, token);
            var format = "image/png";
            using var stream = new MemoryStream();
            captcha.Image.SaveAsPng(stream);
            var bytes = stream.ToArray();
            return File(bytes, format, captcha.Token);
        }
    }
}
