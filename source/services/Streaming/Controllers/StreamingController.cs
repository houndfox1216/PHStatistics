using System;
using System.Framework;
using System.Framework.Data;
using System.Framework.Logging;
using System.Framework.Web;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyProject.Services.Streaming.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EmptyProject.Services.Streaming.Controllers {
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class StreamingController : ApiController<ServiceUser, Model, Culture> {
        public StreamingController() : base("System") { }

        /// <summary>
        /// 授權資料
        /// </summary>
        public class AuthorizationValues {
            [JsonProperty("streamName")]
            public string StreamName { get; set; }
            [JsonProperty("userName")]
            public string UserName { get; set; }
            [JsonProperty("token")]
            public string Token { get; set; }
            [JsonProperty("applicationName")]
            public string ApplicationName { get; set; }
            [JsonProperty("applicationInstance")]
            public string ApplicationInstance { get; set; }
        }

        public class PublishValues {
            [JsonProperty("event")]
            public string Event { get; set; }
            [JsonProperty("applicationName")]
            public string ApplicationName { get; set; }
            [JsonProperty("applicationInstance")]
            public string ApplicationInstance { get; set; }
            [JsonProperty("userName")]
            public string UserName { get; set; }
            [JsonProperty("ip")]
            public string Ip { get; set; }
            [JsonProperty("streamName")]
            public string StreamName { get; set; }
            [JsonProperty("sessionId")]
            public string SessionId { get; set; }
            [JsonProperty("token")]
            public string Token { get; set; }
            [JsonProperty("timestamp")]
            public DateTime? Timestamp { get; set; }
            [JsonProperty("users")]
            public PpmData[] Users { get; set; }
        }

        /// <summary>
        /// PPM資料
        /// </summary>
        public class PpmData {
            [JsonProperty("username")]
            public string Username { get; set; }
            [JsonProperty("duration")]
            public double Duration { get; set; }
            [JsonProperty("session")]
            public string Session { get; set; }
        }

        [HttpPost, Route("Authorize")]
        public IActionResult Authorize([FromForm] string account, [FromForm] string password) {
            try {
                var user = Model.User.Authorize(account, password);
                return Ok(user);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// 驗證會員
        /// </summary>
        /// <param name="values">授權資料</param>
        /// <returns>允取(allow)或拒絕(deny)</returns>
        [HttpPost("AuthorizeUser")]
        public async Task<IActionResult> AuthorizeUser() {
            string values = null;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8)) {
                values = await reader.ReadToEndAsync();
            }

            switch (values) {
                case null: return Ok(new { result = "deny" }); // bad request
                case "null": return Ok("Success"); // publish null over aithorize url
                default:
                    if (values.Contains("\"event\":\"")) { // publish event over aithorize url
                        var publicValues = new PublishValues();
                        JsonConvert.PopulateObject(values, publicValues);
                        switch (publicValues.Event) {
                            case "Publish":
                                return Model.DataContext.LiveSource.Any(e => e.Entry == publicValues.StreamName && e.Token == publicValues.Token)
                                    ? Ok(new { result = "allow" })
                                    : Ok(new { result = "deny" });
                            case "Unpublish": /* do something here */ break;
                            case "Disconnect": /* do something here */ break;
                        }
                        return Ok(new { result = "allow" });
                    } else { // authorize
                        var authorizationValues = new AuthorizationValues();
                        JsonConvert.PopulateObject(values, authorizationValues);

                        // 無token者，禁止播放
                        if (!authorizationValues.Token.HasValue()) return Ok(new { result = "deny" });

                        // 透過token取回的用戶如已被刪除或停權時，禁止播放
                        var userId = Model.DataContext.User.Where(e =>
                            e.Token == authorizationValues.Token &&
                            e.DataMode == DataMode.Normal &&
                            e.Status == UserStatus.Enabled
                        ).Select(e => e.Id).SingleOrDefault();
                        if (!userId.HasValue()) return Ok(new { result = "deny" });

                        return Ok(new { result = "allow" });
                    }
            }
        }

        /// <summary>
        /// 解答令牌的持有者
        /// </summary>
        /// <param name="token">令牌</param>
        /// <returns></returns>
        [HttpPost("ResolveToken")]
        public IActionResult ResolveToken(string token) {
            var user = token.HasValue() ? Model.DataContext.User.SingleOrDefault(e => e.Token == token) : null;
            return Ok(new { username = user?.Id.ToString() ?? "anonymous", timestamp = DateTime.Now.Ticks, encoder = false });
        }

        /// <summary>
        /// 事件發布
        /// </summary>
        [HttpPost("Publish")]
        public async Task<IActionResult> Publish() {
            string values = null;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8)) {
                values = await reader.ReadToEndAsync();
            }

            if (values.HasValue() && values != "null") {
                var publishValues = new PublishValues();
                JsonConvert.PopulateObject(values, publishValues);
                switch (publishValues.Event) {
                    case "PpmUpdate":
                        var now = DateTime.Now;
                        if (publishValues.Users != null) {
                            Guid memberId = Guid.Empty;
                            foreach (var data in publishValues.Users.Where(e => e.Username != "anonymous")) {
                                if (!Guid.TryParse(data.Username, out memberId)) continue;
                                //var initialTime = now.AddSeconds(-data.Duration);
                                //if (data.Duration < 10) { 
                                //    var usages = Model.DataContext.MediaUsages.Where(e => e.Session == null && e.Member.Id == memberId && e.CreatedTime >= initialTime).OrderBy(e => e.CreatedTime);
                                //    if (usages.Any()) usages.Take(1).Update(e => new MediaUsage { Session = data.session, Duration = data.duration });
                                //} else {
                                //    var usage = Model.DataContext.MediaUsages.Where(e => e.Session == data.Session && e.Member.Id == memberId);
                                //    if (usage.Any()) {
                                //        usage.Update(e => new MediaUsage { Duration = data.duration });
                                //    } else if (data.Duration < 20) {
                                //        var usages = Model.DataContext.MediaUsages.Where(e => e.Session == null && e.Member.Id == memberId && e.CreatedTime >= initialTime).OrderBy(e => e.CreatedTime);
                                //        if (usages.Any()) usages.Take(1).Update(e => new MediaUsage { Session = data.session, Duration = data.duration });
                                //    }
                                //}
                            }
                        }
                        break;
                }
            }
            return Ok("Success");
        }
    }
}