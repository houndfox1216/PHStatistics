using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Framework.Data;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [RequirePermission(SystemPermission.Role)]
    public class RoleController : AdminBaseController {
        public IActionResult Index() {
            ViewBag.Title = "角色管理";
            return View();
        }

        [HttpGet]
        public object GetRoles(DataSourceLoadOptions loadOptions) {
            // 先載入實體再投影，確保 NotMapped 的 PermissionValues 能正確計算
            var entities = Model.DataContext.Role
                .Where(e => e.DataMode == DataMode.Normal)
                .ToList();
            var query = entities
                .Select(e => new {
                    e.Id,
                    e.Name,
                    e.Description,
                    e.PermissionValues,
                    e.CreatedTime,
                    e.UpdatedTime
                })
                .AsQueryable();
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpGet]
        public IActionResult GetPermissions() {
            var permissions = Enum.GetValues(typeof(SystemPermission))
                .Cast<SystemPermission>()
                .Select(p => new {
                    value = (int)p,
                    text = typeof(SystemPermission)
                        .GetMember(p.ToString())
                        .First()
                        .GetCustomAttribute<DisplayAttribute>()?.Name ?? p.ToString()
                });
            return Json(permissions);
        }

        [HttpPost]
        public IActionResult Create(string values) {
            try {
                var data = new Role();
                JsonConvert.PopulateObject(values, data);
                data.CreatedTime = DateTime.Now;
                data.DataMode = DataMode.Normal;
                Model.DataContext.Role.Add(data);
                Model.DataContext.SaveChanges();
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public IActionResult Update(Guid key, string values) {
            try {
                var data = Model.DataContext.Role.Find(key);
                if (data == null) return NotFound();
                JsonConvert.PopulateObject(values, data);
                data.UpdatedTime = DateTime.Now;
                Model.DataContext.SaveChanges();
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        public IActionResult Delete(Guid key) {
            try {
                var data = Model.DataContext.Role.Find(key);
                if (data == null) return NotFound();
                data.DataMode = DataMode.Deleted;
                data.UpdatedTime = DateTime.Now;
                Model.DataContext.SaveChanges();
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }
}
