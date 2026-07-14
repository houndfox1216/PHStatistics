using System;
using System.Framework;
using System.Framework.Logging;
using System.Framework.Web;
using PHStatistics.Portal.Models;
using Microsoft.AspNetCore.Mvc;
using System.Framework.Application;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace PHStatistics.Portal.Controllers {
    public class MemberController : MvcController<PortalUser, Model, Culture> {
        public MemberController() : base("System") { }

        public IActionResult Login(string account, string password, string captcha, string token, string returnUrl = "/Index") {
            Logger.LogInformation($"Member Login account:{account} password:{password} ");
            if (captcha != null && token != null) {
                if (CaptchaProvider.Check(token, captcha)) {
                    try {
                        var entity = Model.Authorize(account, password);
                        var user = new PortalUser(entity);
                        Logger.LogInformation($"進行登入 ");
                        user.Login();
                        // 設定 Session
                        HttpContext.Session.SetString("Account", entity.Account);
                        HttpContext.Session.SetString("UserLogin", "1");
                        //設定cookie
                        HttpContext.Response.Cookies.Append("Account", entity.Account);
                        HttpContext.Response.Cookies.Append("UserLogin", "1");
                        Logger.LogInformation($"進行登入完成 導入 {returnUrl} {user.IsGuest()} ");
                        return Redirect("/");
                    } catch (FrameworkException fe) {
                        HttpContext.Session.SetString("Account", string.Empty);
                        HttpContext.Session.SetString("UserLogin", "0");
                        HttpContext.Response.Cookies.Append("Account", string.Empty);
                        HttpContext.Response.Cookies.Append("UserLogin", "0");
                        ViewBag.ErrorMessage = fe.Message;
                    } catch (Exception e) {
                        Logger.LogError(e);
                        ViewBag.ErrorMessage = "系統維護中，請稍後再試";
                    }
                } else ViewBag.ErrorMessage = "驗證碼錯誤";
            }
            else {
                if (User.IsGuest()) {
                    // 讀取 Session
                    string sAccount = string.Empty;
                    string login = string.Empty;
                    try {
                        sAccount = HttpContext.Session.GetString("Account") ?? "";
                        login = HttpContext.Session.GetString("UserLogin") ?? "0";
                    }
                    catch (Exception ex) {
                        try {
                            sAccount = HttpContext.Request.Cookies["Account"] ?? "";
                            login = HttpContext.Request.Cookies["UserLogin"] ?? "0";
                        }
                        catch {
                            sAccount = string.Empty;
                            login = string.Empty;
                        }
                    }

                    if (!string.IsNullOrEmpty(sAccount) && login == "1") {
                        try {
                            var entity = Model.SessionAuthorizationAction(sAccount);
                            var user = new PortalUser(entity);
                            Logger.LogInformation($"進行Session登入 ");
                            user.Login();
                            // 設定 Session
                            HttpContext.Session.SetString("Account", entity.Account);
                            HttpContext.Session.SetString("UserLogin", "1");
                            Logger.LogInformation($"進行登入完成 導入 {returnUrl} {user.IsGuest()} ");
                            return Redirect("/");
                        }
                        catch (Exception ex) {
                            Logger.LogError(ex, $"Session 續登失敗 account:{sAccount}");
                        }
                    }
                }
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public IActionResult MemberTestSession() {
            HttpContext.Session.SetString("Account", "TestSession");
            HttpContext.Session.SetString("UserLogin", "TestSession");
            return View("Login");
        }

        [HttpPost]
        public IActionResult MemberTestCookie() {
            var options = new CookieOptions {
                Expires = DateTime.UtcNow.AddDays(1), // 設置Cookie過期時間
                HttpOnly = true, // 增強安全性，防止客戶端腳本訪問
                Secure = false // 確保在非HTTPS環境下也可以傳輸
            };
            HttpContext.Response.Cookies.Append("Account", DateTime.UtcNow.ToTaipeiTime().ToString("yyyy/MM/dd HH:mm.ss"));
            HttpContext.Response.Cookies.Append("UserLogin", "TestCookie", options);
            return View("Login");
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
        [Authorize(typeof(PortalUser))]
        public IActionResult LogOut() {
            User.Logout();
            return Redirect("/Member/Login");
        }

        public IActionResult Contact() {
            return View();
        }
    }
}
