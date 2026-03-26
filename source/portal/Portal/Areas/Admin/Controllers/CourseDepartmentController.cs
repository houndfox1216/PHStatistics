using System;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PHStatistics.Actions;
using PHStatistics.Content;
using System.Framework.Data;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [RequirePermission(SystemPermission.CourseDepartment)]
    public class CourseDepartmentController : AdminBaseController {
        public IActionResult Index() {
            ViewBag.Title = "班系管理";
            return View();
        }

        [HttpGet]
        public object GetCourseDepartments(DataSourceLoadOptions loadOptions) {
            var readAction = new CourseDepartmentReadAction(User, Model.DataContext);
            var query = readAction.Query(new Condition(), (Sorting[])null);
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpPost]
        public IActionResult Create(string values) {
            try {
                var data = new CourseDepartment();
                JsonConvert.PopulateObject(values, data);
                var createAction = new CourseDepartmentCreateAction(User, Model.DataContext);
                createAction.Create(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public IActionResult Update(int key, string values) {
            try {
                var readAction = new CourseDepartmentReadAction(User, Model.DataContext, true);
                var data = readAction.Find(key);
                if (data == null) return NotFound();
                JsonConvert.PopulateObject(values, data);
                var updateAction = new CourseDepartmentUpdateAction(User, Model.DataContext);
                updateAction.Update(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        public IActionResult Delete(int key) {
            try {
                var deleteAction = new CourseDepartmentDeleteAction(User, Model.DataContext);
                deleteAction.Delete(new CourseDepartment { Id = key });
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }
}
