using System;
using System.Linq;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PHStatistics.Actions;
using PHStatistics.Content;
using System.Framework.Data;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [RequirePermission(SystemPermission.School)]
    public class SchoolController : AdminBaseController {
        public IActionResult Index() {
            ViewBag.Title = "分校管理";
            return View();
        }

        [HttpGet]
        public object GetSchools(DataSourceLoadOptions loadOptions) {
            var readAction = new SchoolReadAction(User, Model.DataContext);
            var query = readAction.Query(new Condition(), (Sorting[])null);
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpGet]
        public object GetRegions(DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.Region.OrderBy(e => e.Ordinal);
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpPost]
        public IActionResult Create(string values) {
            try {
                var data = new School();
                JsonConvert.PopulateObject(values, data);
                var createAction = new SchoolCreateAction(User, Model.DataContext);
                createAction.Create(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public IActionResult Update(int key, string values) {
            try {
                var readAction = new SchoolReadAction(User, Model.DataContext, true);
                var data = readAction.Find(key);
                if (data == null) return NotFound();
                JsonConvert.PopulateObject(values, data);
                var updateAction = new SchoolUpdateAction(User, Model.DataContext);
                updateAction.Update(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        public IActionResult Delete(int key) {
            try {
                var deleteAction = new SchoolDeleteAction(User, Model.DataContext);
                deleteAction.Delete(new School { Id = key });
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }
}
