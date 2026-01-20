using System.Framework.Web;
using System.IO;
using PHStatistics.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;

namespace PHStatistics.Portal.Controllers {
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class CaptchaController : ApiController<PortalUser, UserModel, Culture> {
        public CaptchaController() : base("System") { }

        [HttpGet]
        public IActionResult Get(int length = 4, string token = null) {
            var captcha = CaptchaProvider.GetCaptcha(length, 32, 16, token);
            var format = "image/png";
            using var stream = new MemoryStream();
            captcha.Image.SaveAsPng(stream);
            var bytes = stream.ToArray();
            return File(bytes, format, captcha.Token);
        }
    }
}

