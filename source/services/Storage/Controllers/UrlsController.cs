using System;
using System.Collections.Generic;
using System.Framework;
using System.Framework.Application;
using System.Framework.Logging;
using System.Framework.Storage;
using System.Framework.Web;
using EmptyProject.Services.Storage.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace EmptyProject.Services.Storage.Controllers {
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class UrlsController : ApiController<ServiceUser, Model, Culture> {
        private IStorageContext Storage { get; }

        public UrlsController() : base() => Storage = ApplicationContext.Root.Storage;

        [HttpPost]
        [Authorize(typeof(ServiceUser))]
        public JsonResponse Create(string[] url) {
            try {
                var uris = new HashSet<string>();
                foreach (var file in url) {
                    var fileUri = new Uri(file);
                    var filename = fileUri.AbsolutePath;
                    var division = filename.LastIndexOf('.');
                    var extension = division > -1 ? filename[division..] : string.Empty;
                    filename = filename.Substring(0, filename.Length - extension.Length);
                    var blob = Storage.GetBlob("temporary", $"{ShortUid.NewId}{extension}");
                    if (Storage.Copy(fileUri, blob)) uris.Add($"{blob.AbsoluteUri}?fn={filename}");
                }
                return Json(ResponseStatus.OK, uris);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e.Message, "系統忙碌中，請稍後再試");
            }
        }
    }
}