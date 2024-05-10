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

// ReSharper disable once CheckNamespace
namespace PHStatistics.Services.Admin.Controllers;

/// <summary>
/// News API
/// </summary>
[Route("api/[controller]")]
[EnableCors("AllPassOrigins")]
public class NewsController : ApiController<ServiceUser, NewsModel, Culture> {
    /// <summary>
    /// 建構
    /// </summary>
    public NewsController() : base("System") { }

    /// <summary>
    /// 載入新聞資料來源
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
    /// 查詢新聞
    /// </summary>
    /// <param name="keyword">關鍵字</param>
    /// <param name="page">當前頁碼</param>
    /// <param name="pageSize">每頁顯示數量</param>
    /// <param name="initialPage">起始頁碼</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpGet]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, List<News>>))]
    public JsonResponse Query(string keyword, int? page, int? pageSize, int initialPage = 1) {
        try {
            var query = Model.Query(
                keyword,
                GetParameter<Condition>("condition"),
                GetParameters<Sorting>("sortings")
            );
            if (page.HasValue() && pageSize.HasValue()) return Json(ResponseStatus.OK, query.Page(page!.Value, pageSize!.Value, initialPage));
            return Json(ResponseStatus.OK, query.ToList());
        } catch (FrameworkException fe) {
            return Json(ResponseStatus.InternalServerError, fe, fe.Message);
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
        }
    }

    /// <summary>
    /// 尋找新聞
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpGet("{id}")]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, News>))]
    public JsonResponse Find(int id) {
        try {
            var entity = Model.Find(id);
            return entity == null ? Json(ResponseStatus.InternalServerError, "找不到實體資料！") : Json(ResponseStatus.OK, entity);
        } catch (FrameworkException fe) {
            return Json(ResponseStatus.InternalServerError, fe, fe.Message);
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
        }
    }

    /// <summary>
    /// 建立新聞
    /// </summary>
    /// <param name="values">值</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpPost]
    [Authorize(typeof(ServiceUser))]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, News>))]
    public JsonResponse Create(string values) {
        try {
            var data = new News();
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
    /// 重新排序新聞
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
    /// 更新新聞
    /// </summary>
    /// <param name="values">值</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpPut]
    [Authorize(typeof(ServiceUser))]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, News>))]
    public JsonResponse Update(string values) {
        try {
            var data = values.FromJson<News>();
            data = Model.Find(data.Id, false);
            data.Picture.Images.Clear();
            data.Title.Texts.Clear();
            data.Introduction.Texts.Clear();
            data.Content.Texts.Clear();
            data.NewsTags.Clear();
            JsonConvert.PopulateObject(values, data);
            if (!TryValidateModel(data)) return Json(ResponseStatus.BadRequest, null, ModelState.GetFullErrorMessage());
            var entity = Model.Update(data);
            return Json(ResponseStatus.OK, entity);
        } catch (FrameworkException fe) {
            return Json(ResponseStatus.InternalServerError, fe, fe.Message);
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
        }
    }

    /// <summary>
    /// 刪除新聞
    /// </summary>
    /// <param name="id">識別碼</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpDelete]
    [Authorize(typeof(ServiceUser))]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
    public JsonResponse Delete(int id) {
        try {
            var entity = Model.Find(id);
            Model.Delete(entity.Id);
            return Json(ResponseStatus.OK, entity);
        } catch (FrameworkException fe) {
            return Json(ResponseStatus.InternalServerError, fe, fe.Message);
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
        }
    }
}