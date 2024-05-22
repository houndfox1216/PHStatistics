using System;
using System.Framework;
using System.Framework.Logging;
using System.Framework.Web;
using PHStatistics.Portal.Models;
using Microsoft.AspNetCore.Mvc;

namespace PHStatistics.Portal.Controllers {
    public class MemberController : MvcController<PortalUser, Model, Culture> {
        public MemberController() : base("System") { }

        public IActionResult Login(string account, string password, string captcha, string token, string returnUrl = "/") {
            if (captcha != null && token != null) {
                if (CaptchaProvider.Check(token, captcha)) {
                    try {
                        var entity = Model.Authorize(account, password);
                        var user = new PortalUser(entity);
                        user.Login();
                        return Redirect(returnUrl);
                    } catch (FrameworkException fe) {
                        ViewBag.ErrorMessage = fe.Message;
                    } catch (Exception e) {
                        Logger.LogError(e);
                        ViewBag.ErrorMessage = "系統維護中，請稍後再試";
                    }
                } else ViewBag.ErrorMessage = "驗證碼錯誤";
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [Authorize(typeof(PortalUser))]
        public IActionResult ChangePassword(string oldPassword, string newPassword, string captcha, string token, string returnUrl = null) {
            if (captcha != null && token != null) {
                if (CaptchaProvider.Check(token, captcha)) {
                    try {
                        var entity = Model.ChangePassword(oldPassword, newPassword);
                        ViewBag.SuccessMessage = "密碼已變更";
                        if (returnUrl != null) return Redirect(returnUrl);
                    } catch (FrameworkException fe) {
                        ViewBag.ErrorMessage = fe.Message;
                    } catch (Exception e) {
                        Logger.LogError(e);
                        ViewBag.ErrorMessage = "系統維護中，請稍後再試";
                    }
                } else ViewBag.ErrorMessage = "驗證碼錯誤";
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public IActionResult Contact() {
            return View();
        }
    }
}
