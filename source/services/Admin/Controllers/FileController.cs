using System;
using System.Collections.Generic;
using System.Framework;
using System.Framework.Data;
using System.Framework.Logging;
using System.Framework.Web;
using System.IO;
using System.Linq;
using System.Web;
using DevExtreme.AspNet.Mvc.FileManagement;
using EmptyProject.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using Environment = System.Framework.Environment;

namespace EmptyProject.Services.Admin.Controllers;

/// <summary>
/// File API
/// </summary>
[Route("api/[controller]")]
[EnableCors("AllPassOrigins")]
public class FileController : ApiController<ServiceUser, RoleModel, Culture> {
    private static DateTime? _clearTempTime;

    /// <summary>
    /// 建構
    /// </summary>
    public FileController() : base("System") {
        if (Environment.Directory.TempPath == Path.GetTempPath()) return; // Not need to clear
        if (_clearTempTime.HasValue && _clearTempTime.Value.Date == DateTime.Today) return;
        _clearTempTime = DateTime.Now;
        var tempDirectory = Environment.Directory.Temp;
        if (!tempDirectory.Exists) return;
        try {
            foreach (var file in tempDirectory.GetFiles().Where(e => !e.Name.StartsWith('.') && _clearTempTime - e.LastWriteTime > TimeSpan.FromDays(1)))
                file.Delete();
            foreach (var directory in tempDirectory.GetDirectories()) directory.Delete(true);
        } catch (Exception e) {
            Logger.LogError(e, $"Failed to clear temp directory: {tempDirectory.FullName}");
        }
    }

    /// <summary>
    /// 執行命令
    /// </summary>
    /// <param name="command">命令</param>
    /// <param name="arguments">參數</param>
    /// <param name="limited">限制</param>
    /// <param name="dpi">DPI</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpGet("Execute")]
    [HttpPost("Execute")]
    [HttpDelete("Execute")]
    [Produces("application/json")]
    public object Execute(string command, string arguments, string limited, int? dpi = null) {
        try {
            var fsCommand = Enum.Parse<FileSystemCommand>(command);
            var fileRootPath = Path.Combine(Environment.Directory.WebRootPath, "files");
            var configuration = new FileSystemConfiguration {
                Request = Request,
                FileSystemProvider = new PhysicalFileSystemProvider(
                    fileRootPath, (fileSystemItem, clientItem) => {
                        if (!clientItem.IsDirectory) clientItem.CustomFields["url"] = GetFileItemUrl(fileSystemItem);
                    }
                ),
                AllowCopy = true,
                AllowCreate = true,
                AllowMove = true,
                AllowDelete = true,
                AllowRename = true,
                AllowUpload = true,
                AllowDownload = true
            };
            var processor = new FileSystemCommandProcessor(configuration);
            var result = processor.Execute(fsCommand, arguments);
            var clientResult = result.GetClientCommandResult();

            #region File Chunk Upload 時判斷是否需要 resize

            if (fsCommand == FileSystemCommand.UploadChunk && result.Success && limited.IndexOf('x') is var splitIndex and >= 0) {
                var args = arguments.FromJson<JObject>();
                if (args["destinationPathInfo"] == null) return clientResult;
                var path = Path.Combine(args["destinationPathInfo"].Select(e => e["name"].ToString()).ToArray());
                path = Path.Combine(fileRootPath, path);
                if (args["chunkMetadata"] == null) return clientResult;
                var chunkMetadata = args["chunkMetadata"].ToString().FromJson<JObject>();
                if (chunkMetadata["FileName"] == null) return clientResult;
                path = Path.Combine(path, chunkMetadata["FileName"].ToString());
                if (!System.IO.File.Exists(path)) return clientResult;
                if (chunkMetadata["TotalCount"]?.ToString() != "1" && chunkMetadata["Index"]?.ToString() == "0") {
                    System.IO.File.Delete(path); // remove old file when chunk upload starting
                } else { // resize image
                    int? limitedWidth = null;
                    try {
                        limitedWidth = int.Parse(limited[0..splitIndex]);
                    } catch { /* nop */
                    }

                    int? limitedHeight = null;
                    try {
                        limitedHeight = int.Parse(limited[(splitIndex + 1)..]);
                    } catch { /* nop */
                    }

                    try {
                        var image = Image.Load(path).Resize(limitedWidth, limitedHeight, dpi);
                        image.Save(path);
                    } catch { /* npp */
                    }
                }
            }

            #endregion

            return clientResult;
        } catch (FrameworkException fe) {
            return Json(ResponseStatus.InternalServerError, fe, fe.Message);
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
        }
    }

    /// <summary>
    /// 尋找檔案
    /// </summary>
    /// <param name="path">路徑。根目錄為 WebRoot</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpGet("{path}")]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, IFileSystemData>))]
    public JsonResponse Find(string path) {
        try {
            var correctPath = HttpUtility.UrlDecode(path).AvoidPathManipulation();
            var data = Application.FileSystem.Get(correctPath);
            if (data == null) return Json(ResponseStatus.InternalServerError, "找不到檔案！");
            return Json(ResponseStatus.OK, data is WebFile file ? file : data as WebDirectory);
        } catch (FrameworkException fe) {
            return Json(ResponseStatus.InternalServerError, fe, fe.Message);
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
        }
    }

    /// <summary>
    /// 搜尋檔案
    /// </summary>
    /// <param name="path">搜尋路徑</param>
    /// <param name="pattern">搜尋格式</param>
    /// <param name="includeSubdirectories">包含子目錄</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpGet]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, IFileData[]>))]
    public JsonResponse Search(string path, string pattern, bool includeSubdirectories) {
        try {
            var sortings = GetParameters<Sorting>("sortings");
            var correctPath = HttpUtility.UrlDecode(path).AvoidPathManipulation();
            var list = Application.FileSystem.Search(correctPath, pattern, includeSubdirectories, sortings);
            return Json(ResponseStatus.OK, list);
        } catch (FrameworkException fe) {
            return Json(ResponseStatus.InternalServerError, fe, fe.Message);
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
        }
    }

    /// <summary>
    /// 建立檔案
    /// </summary>
    /// <param name="files">檔案</param>
    /// <param name="mode">模式。當模式為 wysiwyg 時，不需指定目錄，固定存放於 /files/images 目錄下供網路讀取</param>
    /// <param name="directory">存放目錄</param>
    /// <param name="filename">檔名</param>
    /// <param name="contentType">內容類型</param>
    /// <response code="200">請求已被處理，回應各檔案網址</response>
    [HttpPost]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
    public JsonResponse Create(IList<IFormFile> files, string mode, string directory, string filename, string contentType) {
        try {
            var correctDirectory = HttpUtility.UrlDecode(directory).AvoidPathManipulation();
            var correctFilename = HttpUtility.UrlDecode(filename).AvoidPathManipulation();
            if (correctDirectory != null && correctFilename != null) {
                var formFile = files is { Count: > 1 } ? files[0] : null;
                if (formFile == null && Request.Form.Files.Count > 0) formFile = Request.Form.Files[0];
                if (formFile == null) return Json(ResponseStatus.BadRequest);
                contentType ??= formFile.ContentType;
                var file = Application.FileSystem.Create(formFile, correctDirectory, correctFilename, contentType);
                return Json(ResponseStatus.OK, file);
            } else {
                if (files.Count == 0 && Request.Form.Files.Count > 0) files = new List<IFormFile>(Request.Form.Files);

                var directoryInfo = mode == "wysiwyg"
                    ? new DirectoryInfo(Path.Combine(Environment.Directory.WebRootPath, "files", "images"))
                    : Environment.Directory.Temp;
                if (!directoryInfo.Exists) directoryInfo.Create();

                var filenames = new HashSet<string>();
                foreach (var file in files) {
                    correctFilename = $"{ShortUid.NewId}{file.FileName[file.FileName.LastIndexOf('.')..]}".AvoidPathManipulation();
                    using Stream fileStream = new FileStream(Path.Combine(directoryInfo.FullName, correctFilename.Trim('/')), FileMode.Create);
                    file.CopyTo(fileStream);
                    filenames.Add(mode == "wysiwyg" ? $"/files/images/{correctFilename}" : correctFilename);
                }

                return Json(ResponseStatus.OK, filenames);
            }
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e.Message, "系統忙碌中，請稍後再試");
        }
    }

    /// <summary>
    /// 刪除檔案或目錄
    /// </summary>
    /// <param name="path">檔案或目錄的路徑</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpDelete]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
    public JsonResponse Delete(string path) {
        try {
            var correctPath = HttpUtility.UrlDecode(path).AvoidPathManipulation();
            Application.FileSystem.Delete(correctPath);
            return Json(ResponseStatus.OK);
        } catch (FrameworkException fe) {
            return Json(ResponseStatus.InternalServerError, fe, fe.Message);
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
        }
    }

    /// <summary>
    /// 移動檔案或目錄
    /// </summary>
    /// <param name="source">移動的來源目錄或檔案路徑</param>
    /// <param name="destination">移動的目的目錄或檔案路徑</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpPost("Move")]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
    public JsonResponse Move(string source, string destination) {
        try {
            var correctSource = HttpUtility.UrlDecode(source).AvoidPathManipulation();
            var correctDestination = HttpUtility.UrlDecode(destination).AvoidPathManipulation();
            Application.FileSystem.Move(correctSource, correctDestination);
            return Json(ResponseStatus.OK);
        } catch (FrameworkException fe) {
            return Json(ResponseStatus.InternalServerError, fe, fe.Message);
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
        }
    }

    /// <summary>
    /// 複製檔案或目錄
    /// </summary>
    /// <param name="source">複製的來源目錄或檔案路徑</param>
    /// <param name="destination">複製的目的目錄或檔案路徑</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpPost("Copy")]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus>))]
    public JsonResponse Copy(string source, string destination) {
        try {
            var correctSource = HttpUtility.UrlDecode(source).AvoidPathManipulation();
            var correctDestination = HttpUtility.UrlDecode(destination).AvoidPathManipulation();
            Application.FileSystem.Copy(correctSource, correctDestination);
            return Json(ResponseStatus.OK);
        } catch (FrameworkException fe) {
            return Json(ResponseStatus.InternalServerError, fe, fe.Message);
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
        }
    }

    /// <summary>
    /// 建立目錄
    /// </summary>
    /// <param name="path">路徑</param>
    /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
    [HttpPost("CreateDirectory")]
    [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, IDirectoryData>))]
    public JsonResponse CreateDirectory(string path) {
        try {
            var correctPath = HttpUtility.UrlDecode(path).AvoidPathManipulation();
            var directory = Application.FileSystem.Create(correctPath);
            return Json(ResponseStatus.OK, directory);
        } catch (FrameworkException fe) {
            return Json(ResponseStatus.InternalServerError, fe, fe.Message);
        } catch (Exception e) {
            Logger.LogError(e);
            return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
        }
    }

    private static string GetFileItemUrl(FileSystemInfo fileSystemItem) {
        var relativeUrl = fileSystemItem.FullName
            .Replace(Environment.Directory.WebRootPath, "")
            .Replace(Path.DirectorySeparatorChar, '/');
        return relativeUrl;
    }
}