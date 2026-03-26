using System.Framework;
using System.Framework.Application;
using System.Framework.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PHStatistics.Portal.Models;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [Area("Admin")]
    [Authorize(typeof(PortalUser))]
    public abstract class AdminBaseController : MvcController<PortalUser, Model, Culture> {
        protected AdminBaseController() : base("System") { }

        public override void OnActionExecuting(ActionExecutingContext context) {
            base.OnActionExecuting(context);

            if (User.IsGuest()) {
                context.Result = new RedirectToActionResult("Login", "Member", new { area = "" });
                return;
            }

            // 檢查 Action/Controller 上的 RequirePermissionAttribute
            var permAttr = context.ActionDescriptor.EndpointMetadata
                .OfType<RequirePermissionAttribute>()
                .FirstOrDefault();
            if (permAttr != null && !User.HasPermission(permAttr.Permission)) {
                context.Result = new RedirectToActionResult("Index", "Dashboard", new { area = "Admin" });
            }
        }
    }
}
