using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Framework;
using System.Framework.Globalization;
using System.Framework.Web;
using System.IO;
using System.Reflection;
using EmptyProject.Portal.Models;
using Microsoft.AspNetCore.Mvc;

using Environment = System.Framework.Environment;

namespace EmptyProject.Portal.Controllers {
    [Route("/")]
    public class HomeController() : MvcController<PortalUser, Model, Culture>("System") {
        [Route("")]
        public IActionResult Index() {
            ViewBag.Title = "Home Page".ToI18n(Culture.GetCode());
            ViewBag.BannerPositions = new List<BannerPosition>();
            ViewBag.BannerPositions.Add(Model.BannerPosition.FindByCode("Home.Slider"));

            return View();
        }

        [Route("Culture/{code}")]
        public IActionResult ChangeCulture(string code) {
            Culture = Model.GetCulture(code);
            var referer = Request.Headers.Referer.ToString();
            return Redirect(referer.HasValue() ? referer : "/");
        }

        [Route("error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            ViewBag.Title = "Error".ToI18n(Culture.GetCode());
            return View("Error", Activity.Current?.Id ?? HttpContext.TraceIdentifier);
        }

        [Route("Sitemap")]
        [Route("sitemap.xml")]
        public IActionResult Sitemap() {
            const string virtualPath = "~/sitemap.xml";
            var file = new FileInfo(Environment.GetRealPath(virtualPath));
            if (file.Exists && file.LastWriteTime.Date >= DateTime.Today) return File("~/sitemap.xml", "text/xml");
            var sitemap = new Sitemap("~/", Application.Policy.ModifySitemap<DataContext>(HttpContext));
            sitemap.Save();
            return Content(sitemap.ToXml(), "text/xml");
        }

        [Route("Information")]
        public IActionResult Information() {
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var appVersion = Application.Configuration["Version"].ToString();
            return Content(new { assemblyVersion, appVersion }.ToJson(), "text/json");
        }
    }
}