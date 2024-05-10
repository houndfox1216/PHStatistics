using System;
using System.Collections.Generic;
using System.Framework;
using System.Framework.Data;
using System.Framework.Logging;
using System.Framework.Web;
using System.Linq;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.Helpers;
using DevExtreme.AspNet.Data.ResponseModel;
using DevExtreme.AspNet.Mvc;
using PHStatistics.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace PHStatistics.Services.Admin.Controllers {
    /// <summary>
    /// Attribute API
    /// </summary>
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class AttributeController : ApiController<ServiceUser, AttributeModel, Culture> {
        /// <summary>
        /// 建構
        /// </summary>
        public AttributeController() : base("System") { }

        /// <summary>
        /// 載入屬性資料來源
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
        /// 查詢屬性
        /// </summary>
        /// <param name="keyword">關鍵字</param>
        /// <param name="page">當前頁碼</param>
        /// <param name="pageSize">每頁顯示數量</param>
        /// <param name="initialPage">起始頁碼</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, List<Attribute>>))]
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
        /// 尋找屬性
        /// </summary>
        /// <param name="id">識別碼</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet("{id}")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, Attribute>))]
        public JsonResponse Find(int id) {
            try {
                var entity = Model.Find(id);
                if (entity == null) return Json(ResponseStatus.NotFound);
                return Json(ResponseStatus.OK, entity);
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
            }
        }

        /// <summary>
        /// 建立屬性
        /// </summary>
        /// <param name="values">值</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpPost]
        [Authorize(typeof(ServiceUser))]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, Attribute>))]
        public JsonResponse Create(string values) {
            try {
                var data = new Attribute();
                JsonConvert.PopulateObject(values, data);
                if (!TryValidateModel(data)) return Json(ResponseStatus.BadRequest, null, ModelState.GetFullErrorMessage());
                var entity = Model.Create(data);
                return Json(ResponseStatus.OK, entity);
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
            }
        }

        /// <summary>
        /// 重新排序屬性
        /// </summary>
        /// <param name="source">拖曳位置的識別碼</param>
        /// <param name="target">放置位置的識別碼</param>
        /// <param name="isAfter">是否重新排序到目的之後</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpPost("Reorder")]
        [Authorize(typeof(ServiceUser))]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse Reorder(int source, int target, bool isAfter = false) {
            try {
                Model.Reorder(source, target, isAfter);
                return Json(ResponseStatus.OK);
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
            }
        }

        /// <summary>
        /// 更新屬性
        /// </summary>
        /// <param name="values">值</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpPut]
        [Authorize(typeof(ServiceUser))]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, Attribute>))]
        public JsonResponse Update(string values) {
            try {
                var entity = Model.Find(values.FromJson<Attribute>().Id);
                JsonConvert.PopulateObject(values, entity);
                if (!TryValidateModel(entity)) return Json(ResponseStatus.BadRequest, null, ModelState.GetFullErrorMessage());
                Model.Update(entity);
                return Json(ResponseStatus.OK, entity);
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
            }
        }

        /// <summary>
        /// 刪除屬性
        /// </summary>
        /// <param name="id">識別碼</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpDelete]
        [Authorize(typeof(ServiceUser))]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse Delete(int id) {
            try {
                Model.Delete(id);
                return Json(ResponseStatus.OK);
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
            }
        }
    }
}