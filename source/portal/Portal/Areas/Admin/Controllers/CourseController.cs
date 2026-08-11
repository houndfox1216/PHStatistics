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
    [RequirePermission(SystemPermission.Course)]
    public class CourseController : AdminBaseController {
        public IActionResult Index() {
            ViewBag.Title = "課程管理";
            return View();
        }

        [HttpGet]
        public object GetCourses(DataSourceLoadOptions loadOptions) {
            var readAction = new CourseReadAction(User, Model.DataContext);
            // 先載入實體再轉為記憶體查詢，確保 NotMapped 的 SourceDepartmentIdValues/SourceCourseIdValues 能正確計算
            var query = readAction.Query(new Condition(), (Sorting[])null, "Department").ToList().AsQueryable();
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpGet]
        public IActionResult GetCourseOptions() {
            var readAction = new CourseReadAction(User, Model.DataContext);
            // 先具現化再投影，DisplayName 是 NotMapped 計算屬性（依賴 Department 導覽屬性），無法轉譯成 SQL
            var options = readAction.Query(new Condition(), (Sorting[])null, "Department")
                .OrderBy(e => e.Ordinal).ThenBy(e => e.Id)
                .ToList()
                .Select(e => new { e.Id, e.Name, e.DisplayName })
                .ToList();
            return Json(options);
        }

        [HttpPost]
        public IActionResult Create(string values) {
            try {
                var data = new Course();
                JsonConvert.PopulateObject(values, data);
                var createAction = new CourseCreateAction(User, Model.DataContext);
                createAction.Create(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public IActionResult Update(int key, string values) {
            try {
                var readAction = new CourseReadAction(User, Model.DataContext, true);
                var data = readAction.Find(key);
                if (data == null) return NotFound();
                JsonConvert.PopulateObject(values, data);
                var updateAction = new CourseUpdateAction(User, Model.DataContext);
                updateAction.Update(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        public IActionResult Delete(int key) {
            try {
                var deleteAction = new CourseDeleteAction(User, Model.DataContext);
                deleteAction.Delete(new Course { Id = key });
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public object GetDepartments(DataSourceLoadOptions loadOptions) {
            var readAction = new CourseDepartmentReadAction(User, Model.DataContext);
            // 先具現化，DisplayName 是 NotMapped 計算屬性，無法轉譯成 SQL
            var query = readAction.Query(new Condition(), (Sorting[])null).ToList();
            return DataSourceLoader.Load(query, loadOptions);
        }
    }
}
