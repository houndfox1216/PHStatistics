using System;
using System.Framework;
using System.Framework.Data;
using System.Framework.Web;
using System.Linq;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using PHStatistics.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PHStatistics.Services.Admin.Controllers.Community {
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class MemberController : ApiController<ServiceUser, ManagerModel, Culture> {

        public ManagerController() : base("System") { }

        public IActionResult Index() {
            return View();
        }
    }
}
