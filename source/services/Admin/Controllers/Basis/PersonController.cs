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
using EmptyProject.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EmptyProject.Services.Admin.Controllers {
    /// <summary>
    /// Person API
    /// </summary>
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    [Authorize(typeof(ServiceUser))]
    public class PersonController : ApiController<ServiceUser, PersonModel, Culture> {
        /// <summary>
        /// 建構
        /// </summary>
        public PersonController() : base("System") { }

        /// <summary>
        /// 載入個人資料來源
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
        /// 查詢個人資料
        /// </summary>
        /// <param name="keyword">關鍵字</param>
        /// <param name="page">當前頁碼</param>
        /// <param name="pageSize">每頁顯示數量</param>
        /// <param name="initialPage">起始頁碼</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, List<Person>>))]
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
        /// 尋找個人資料
        /// </summary>
        /// <param name="id">識別碼</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet("{id}")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, Person>))]
        public JsonResponse Find(string id) {
            try {
                var entity = Model.Find(Guid.Parse(id));
                if (entity == null) return Json(ResponseStatus.InternalServerError, null, "找不到實體資料！");
                return Json(ResponseStatus.OK, entity);
            } catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            } catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
            }
        }

        /// <summary>
        /// 建立個人資料
        /// </summary>
        /// <param name="values">值</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpPost]
        [Authorize(typeof(ServiceUser))]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, Person>))]
        public JsonResponse Create(string values) {
            try {
                var data = new Person();
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
        /// 更新個人資料
        /// </summary>
        /// <param name="values">值</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpPut]
        [Authorize(typeof(ServiceUser))]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, Person>))]
        public JsonResponse Update(string values) {
            try {
                var entity = Model.Find(values.FromJson<Person>().Id);
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
        /// 刪除個人資料
        /// </summary>
        /// <param name="id">識別碼</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpDelete]
        [Authorize(typeof(ServiceUser))]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
        public JsonResponse Delete(Guid id) {
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