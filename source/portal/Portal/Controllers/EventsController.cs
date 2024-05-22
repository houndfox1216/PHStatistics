using System.Framework.Web;
using PHStatistics.Portal.Models;
using Microsoft.AspNetCore.Mvc;

namespace PHStatistics.Portal.Controllers {
    [Authorize(typeof(PortalUser))]
    public class EventsController : MvcController<PortalUser, Model, Culture> {
        public EventsController() : base("System") { }

        public IActionResult Index() {
            return View();
        }

        public IActionResult Detail() {
            return View();
        }

    }
}
