using System;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Logging;
using System.Framework.Security;
using System.Framework.Web;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PHStatistics.Models;
using PHStatistics.Services.Admin.Models;
using PHStatistics.Services.Admin.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

using User = PHStatistics.User;

namespace PHStatistics.Services.Admin.Controllers
{
    /// <summary>
    /// System API
    /// </summary>
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class SystemController : ApiController<ServiceUser, UserModel, Culture> {
        /// <summary>
        /// GA4程序
        /// </summary>
        public static Task GoogleAnalyticsReportTask { get; set; }

        /// <summary>
        /// 建構
        /// </summary>
        public SystemController() : base("System") { }

        /// <summary>
        /// 登入
        /// </summary>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpPost("Login")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse Login() {
            var data = GetParameter<AuthorizeData>();
            if (!data.CaptchaToken.HasValue() || !data.Captcha.HasValue() || !CaptchaProvider.Check(data.CaptchaToken, data.Captcha))
                return Json(ResponseStatus.Unauthorized, null, "驗證碼錯誤");
            try {
                if (Model.Authorize(data.Account, data.Password) is User user) {
                    User.Id = user.Id.ToString();
                    User.Login(null);
                    var permissions = user.Roles.SelectMany(e => e.Permissions.Select(p => p.EnumName));
                    return Json(ResponseStatus.OK, new { user.Id, user.Token, user.Name, user.Account, PhotoUri = user.Photo.Uri, Permissions = permissions });
                } else return Json(ResponseStatus.Unauthorized);
            } catch (SecurityException) {
                return Json(ResponseStatus.Unauthorized, null, "帳號或密碼錯誤");
            } catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, null, e.Message);
            }
        }

        /// <summary>
        /// 登入
        /// </summary>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpPost("Logout")]
        [Authorize(typeof(ServiceUser))]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse Logout() {
            try {
                User.Logout();
                return Json(ResponseStatus.OK);
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.Unauthorized, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e, e.Message);
            }
        }

        /// <summary>
        /// 取得目前登入的用戶
        /// </summary>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpPost("CurrentUser")]
        [Authorize(typeof(ServiceUser))]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse CurrentUser() {
            try {
                var user = User.Data as User;
                return Json(ResponseStatus.OK, new {
                    user.Id,
                    user.Token,
                    user.Name,
                    user.Account,
                    PhotoUri = user.Photo.Uri,
                    user.PasswordExpirationPolicy,
                    user.PasswordChangedTime,
                    Permissions = user.Roles.SelectMany(e => e.Permissions.Select(p => p.EnumName)),
                });
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e, e.Message);
            }
        }

        /// <summary>
        /// 取得供後台使用的列舉
        /// </summary>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet("Enumerations")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse Enumerations() {
            try {
                return Json(ResponseStatus.OK, new {
                    Test = DataDictionary.BuildClassMetadata<User>().ToDynamic(),
                    SystemPermission = DataDictionary.BuildEnumMetadata<SystemPermission>().ToDynamic(),
                    Sex = DataDictionary.BuildEnumMetadata<Sex>().ToDynamic(),
                    UserStatus = DataDictionary.BuildEnumMetadata<UserStatus>().ToDynamic(),
                    LengthUnit = DataDictionary.BuildEnumMetadata<LengthUnit>().ToDynamic(),
                    WeightUnit = DataDictionary.BuildEnumMetadata<WeightUnit>().ToDynamic(),
                    createdTime = DateTime.UtcNow
                });
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e, e.Message);
            }
        }

        /// <summary>
        /// 紀錄訊息
        /// </summary>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpPost("Log")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse Log() {
            var message = GetParameter<Message>();
            try {
                var logger = LogManager.GetLogger("Vue");
                switch (message.Type) {
                    case MessageType.Trace: logger.LogTrace(message.Content); break;
                    case MessageType.Debug: logger.LogDebug(message.Content); break;
                    case MessageType.Info: logger.LogInformation(message.Content); break;
                    case MessageType.Warning: logger.LogWarning(message.Content); break;
                    case MessageType.Error: logger.LogError(message.Content); break;
                    case MessageType.Fatal: logger.LogCritical(message.Content); break;
                    default: return Json(ResponseStatus.BadRequest, null, $"The type({message.Type}) isn't supported");
                }
                return Json(ResponseStatus.OK);
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, e.Message);
            }
        }

        /// <summary>
        /// 取得 Google Analytics 報告
        /// </summary>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet("GoogleAnalytics")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse GoogleAnalytics() {
            try {
                var expiryTime = DateTime.Today.GetTaipeiToday();
                var resource = Model.DataContext.Resource.Find("cloudfun://EmptyNext/google-analytics-report")
                    ?? Model.DataContext.Resource.Add(new Resource { Uri = "cloudfun://EmptyNext/google-analytics-report", Name = "Google Analytics 報告" }).Entity;
                GoogleAnalyticsReport report;
                if (!resource.CreatedTime.HasValue) {
                    // initial report
                    var service = new GoogleAnalyticsService();
                    var startDate = DateTime.Today.AddMonths(-1).GetTaipeiToday();
                    var endDate = DateTime.Today.GetTaipeiToday();
                    report = service.Report(startDate, endDate);
                    resource.Content = report.ToXml();
                    Model.DataContext.SaveChanges();
                    return Json(ResponseStatus.OK, report);
                } else if (GoogleAnalyticsReportTask == null && resource.CreatedTime < expiryTime && (!resource.UpdatedTime.HasValue || resource.UpdatedTime < expiryTime)) {
                    // async generate report
                    GoogleAnalyticsReportTask = Task.Run(() => {
                        try {
                            var service = new GoogleAnalyticsService();
                            var startDate = DateTime.Today.AddMonths(-1).GetTaipeiToday();
                            var endDate = DateTime.Today.GetTaipeiToday();
                            var report = service.Report(startDate, endDate);
                            using var context = new DataContext();
                            var resource = context.Resource.Find("cloudfun://EmptyNext/google-analytics-report")
                                ?? context.Resource.Add(new Resource { Uri = "cloudfun://EmptyNext/google-analytics-report", Name = "Google Analytics 報告" }).Entity;
                            resource.Content = report.ToXml();
                            context.SaveChanges();
                        } catch (Exception e) {
                            Logger.LogError(e, e.Message);
                        } finally {
                            GoogleAnalyticsReportTask = null;
                        }
                    });
                }
                return Json(ResponseStatus.OK, resource.Content.FromXml<GoogleAnalyticsReport>());
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e, e.Message);
            }
        }

        /// <summary>
        /// 取得服務資訊
        /// </summary>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet("Information")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse Information() {
            try {
                var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var appVersion = Application.Configuration["Version"].ToString();
                return Json(ResponseStatus.OK, new { assemblyVersion, appVersion });
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e, e.Message);
            }
        }

        /// <summary>
        /// 加密組態值
        /// </summary>
        /// <param name="value">組態值</param>
        /// <response code="200">請求已被處理，回應加密後的組態值</response>
        [HttpGet("EncryptConfigurationValue")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse EncryptConfigurationValue(string value) {
            try {
                var encryptedValue = ConfigurationBase.EncryptConfigurationValue(value);
                return Json(ResponseStatus.OK, encryptedValue);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e, e.Message);
            }
        }

        /// <summary>
        /// 紀錄訊息
        /// </summary>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet("CreatSchoolYear")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse CreatSchoolYear() {
            try {
                DataContext dataContext = new DataContext();
                for (int i = 0;)
                return Json(ResponseStatus.OK);
            }
            catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            }
            catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, e.Message);
            }
        }
    }
}