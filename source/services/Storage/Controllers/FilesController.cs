using System;
using System.Collections.Generic;
using System.Framework;
using System.Framework.Application;
using System.Framework.Logging;
using System.Framework.Storage;
using System.Framework.Web;
using EmptyProject.Services.Storage.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmptyProject.Services.Storage.Controllers {
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class FilesController : ApiController<ServiceUser, Model, Culture> {
        private IStorageContext Storage { get; }

        public FilesController() : base() => Storage = ApplicationContext.Root.Storage;

        [HttpPost]
        public JsonResponse Create(IList<IFormFile> files, string container = "temporary", string path = null) {
            try {
                if (files.Count == 0 && Request.Form.Files.Count > 0) files = new List<IFormFile>(Request.Form.Files);
                path = path.HasValue() ? $"{path.Trim().Trim('/')}/" : string.Empty;
                var uris = new HashSet<string>();
                foreach (var file in files) {
                    var filename = file.FileName;
                    var division = filename.LastIndexOf('.');
                    var extension = division > -1 ? filename[division..] : string.Empty;
                    filename = filename.Substring(0, filename.Length - extension.Length);
                    var blob = $"{path}{ShortUid.NewId}{extension}";
                    if (Storage.Create(file, container, blob) is Uri uri) uris.Add($"{uri.AbsoluteUri}?fn={filename}");
                }
                return Json(ResponseStatus.OK, uris);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e.Message, "系統忙碌中，請稍後再試");
            }
        }
    }
}