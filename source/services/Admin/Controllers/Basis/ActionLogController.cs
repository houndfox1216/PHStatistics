using System;
using System.Collections.Generic;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.Logging;
using System.Framework.Web;
using System.Linq;
using System.Reflection;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.Helpers;
using DevExtreme.AspNet.Data.ResponseModel;
using DevExtreme.AspNet.Mvc;
using PHStatistics.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace PHStatistics.Services.Admin.Controllers {
    /// <summary>
    /// ActionLog API
    /// </summary>
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    [Authorize(typeof(ServiceUser))]
    public class ActionLogController : ApiController<ServiceUser, ActionLogModel, Culture> {
        /// <summary>
        /// 建構
        /// </summary>
        public ActionLogController() : base("System") { }

        /// <summary>
        /// 載入操作記錄資料來源
        /// </summary>
        /// <param name="keyword">關鍵字</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet("Load")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, LoadResult>))]
        public JsonResponse Load(string keyword) {
            try {
                var loadOptions = new DataSourceLoadOptions();
                DataSourceLoadOptionsParser.Parse(loadOptions, (name) => Request.Query.FirstOrDefault(i => i.Key == name).Value);
                var query = Model.Query(
                    keyword,
                    GetParameter<Condition>("condition"),
                    GetParameters<Sorting>("sortings")
                );
                var result = DataSourceLoader.Load(query, loadOptions);
                return Json(ResponseStatus.OK, result);
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
            }
        }

        /// <summary>
        /// 查詢操作記錄
        /// </summary>
        /// <param name="keyword">關鍵字</param>
        /// <param name="page">當前頁碼</param>
        /// <param name="pageSize">每頁顯示數量</param>
        /// <param name="initialPage">起始頁碼</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, List<ActionLog>>))]
        public JsonResponse Query(string keyword, int? page, int? pageSize, int initialPage = 1) {
            try {
                var query = Model.Query(
                    keyword,
                    GetParameter<Condition>("condition"),
                    GetParameters<Sorting>("sortings")
                );
                if (page.HasValue() && pageSize.HasValue()) return Json(ResponseStatus.OK, query.Page(page.Value, pageSize.Value, initialPage));
                return Json(ResponseStatus.OK, query.ToList());
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
            }
        }

        /// <summary>
        /// 尋找操作記錄
        /// </summary>
        /// <param name="id">識別碼</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet("{id}")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, ActionLog>))]
        public JsonResponse Find(int id) {
            try {
                var entity = Model.Find(id);
                if (entity == null) return Json(ResponseStatus.InternalServerError, "找不到資料！");
                if (entity.Xml.HasValue()) {
                    var json = JObject.FromObject(entity);
                    var delta = entity.Xml.FromXml<ActionDelta>();
                    var assembly = Assembly.GetAssembly(typeof(ActionLog));
                    var entityType = assembly.GetType(entity.EntityType);
                    foreach (var column in delta.DeltaColumns) {
                        var displayName = entityType.GetDisplayName(column.Name);
                        if (displayName != column.Name) column.Name = $"{column.Name} ({displayName})";
                    }
                    json.Property("Xml").Value = JObject.FromObject(delta);
                    return Json(ResponseStatus.OK, json);
                }
                return Json(ResponseStatus.OK, entity);
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
            }
        }
    }
}