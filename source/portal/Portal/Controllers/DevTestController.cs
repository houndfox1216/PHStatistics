using Microsoft.AspNetCore.Mvc;

namespace PHStatistics.Portal.Controllers {
    public class DevTestController : Controller {
        public IActionResult Index() {
            return View();
        }
    }
}
