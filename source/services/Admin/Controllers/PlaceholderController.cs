using System.Framework;
using System.Framework.Web;
using System.IO;
using PHStatistics.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace PHStatistics.Services.Admin.Controllers {
    /// <summary>
    /// Placeholder API
    /// </summary>
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class PlaceholderController : ApiController<ServiceUser, UserModel, Culture> {
        /// <summary>
        /// 建構
        /// </summary>
        public PlaceholderController() : base("System") { }

        /// <summary>
        /// 產生佔位符
        /// </summary>
        /// <param name="width">寬度</param>
        /// <param name="height">高度</param>
        /// <param name="text">文字</param>
        /// <param name="bgColor">背景的HTML色碼</param>
        /// <param name="textColor">文字的HTML色碼</param>
        /// <param name="fontSize">字體大小(em)</param>
        /// <param name="dpi">DPI</param>
        /// <response code="200">回應驗證碼PNG圖檔，檔名為指定或自動產生的token值</response>
        [HttpGet]
        [Produces("image/png")]
        public IActionResult Get(int width, int height, string text = null, string bgColor = "LightGray", string textColor = "SlateGray", int? fontSize = null, float dpi = 96) {
            var placeholder = new Placeholder(width, height, text, bgColor, textColor, fontSize, dpi: dpi);
            using var stream = new MemoryStream();
            var image = placeholder.Generate();
            image.Save(stream, PngFormat.Instance);
            var bytes = stream.ToArray();
            return File(bytes, "image/png", $"{width}x{height}.png");
        }
    }
}
