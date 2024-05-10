using System;
using System.Framework;
using System.Framework.Web;
using EmptyProject.Services.Admin.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace EmptyProject.Services.Admin.Controllers {
    /// <summary>
    /// Configuration API
    /// </summary>
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class ConfigurationController : ApiController<ServiceUser, Model, Culture> {
        /// <summary>
        /// 讀取系統組態
        /// </summary>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, Configuration>))]
        public JsonResponse Read() {
            return Json(ResponseStatus.OK, Configuration);
        }

        /// <summary>
        /// 更新系統組態
        /// </summary>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpPut]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse Update() {
            try {
                var value = GetParameter<Configuration>();
                Configuration.Apply(value);
                Configuration.Persist();
                return Json(ResponseStatus.OK);
            } catch (Exception e) {
                return Json(ResponseStatus.InternalServerError, e.Message);
            }
        }
    }
}