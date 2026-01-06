using System;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PHStatistics.Actions;
using PHStatistics.Community;
using System.Framework.Data;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    public class MemberController : AdminBaseController {
        public IActionResult Index() {
            ViewBag.Title = "會員管理";
            return View();
        }

        [HttpGet]
        public object GetMembers(DataSourceLoadOptions loadOptions) {
            var readAction = new MemberReadAction(User, Model.DataContext);
            var query = readAction.Query(new Condition(), (Sorting[])null, "Person");
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpPost]
        public IActionResult Create(string values) {
            try {
                var data = new Member();
                JsonConvert.PopulateObject(values, data);
                var createAction = new MemberCreateAction(User, Model.DataContext);
                createAction.Create(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public IActionResult Update(Guid key, string values) {
            try {
                var readAction = new MemberReadAction(User, Model.DataContext, true);
                var data = readAction.Find(key);
                if (data == null) return NotFound();
                JsonConvert.PopulateObject(values, data);
                var updateAction = new MemberUpdateAction(User, Model.DataContext);
                updateAction.Update(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        public IActionResult Delete(Guid key) {
            try {
                var deleteAction = new MemberDeleteAction(User, Model.DataContext);
                deleteAction.Delete(new Member { Id = key });
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }
}
