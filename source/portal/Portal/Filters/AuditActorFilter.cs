using Microsoft.AspNetCore.Mvc.Filters;
using PHStatistics.Audit;
using PHStatistics.Community;
using System.Framework.Web;

namespace PHStatistics.Portal.Filters {
    /// <summary>
    /// 全域 action filter，在每個 request 開始時把目前登入的使用者寫入 <see cref="AuditActorContext"/>，
    /// 讓 Data 層的 AuditSaveChangesInterceptor 不用相依 Web 層也能知道「是誰做的」。
    /// 對所有 controller（前台、Admin、包含未登入的 HomeController.ImportAll）都會套用，
    /// 未登入情境下 PortalUser.Id 會自動回退成 "System"，不需要另外特判。
    /// </summary>
    public class AuditActorFilter : IActionFilter {
        public void OnActionExecuting(ActionExecutingContext context) {
            var user = context.HttpContext.User<PortalUser>();
            AuditActorContext.CurrentActorId = user?.Id;
            AuditActorContext.CurrentActorName = (user?.Data as Member)?.Nickname ?? user?.Data?.Account;
        }

        public void OnActionExecuted(ActionExecutedContext context) {
        }
    }
}
