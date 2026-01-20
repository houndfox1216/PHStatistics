using Microsoft.AspNetCore.Mvc;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    public class DashboardController : AdminBaseController {
        public IActionResult Index() {
            ViewBag.Title = "管理後台";
            return View();
        }
    }
}
