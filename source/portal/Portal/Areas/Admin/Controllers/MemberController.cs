using System;
using System.Linq;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PHStatistics.Actions;
using PHStatistics.Community;
using PHStatistics.Content;
using System.Framework.Data;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [RequirePermission(SystemPermission.Member)]
    public class MemberController : AdminBaseController {
        public IActionResult Index() {
            ViewBag.Title = "使用者管理";
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

        #region MemberRole CRUD

        [HttpGet]
        public object GetMemberRoles(Guid memberId, DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.MemberRole
                .Where(e => e.MemberId == memberId)
                .Select(e => new { e.RoleId, e.MemberId, e.CreatedTime });
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpPost]
        public IActionResult CreateMemberRole(string values) {
            try {
                var data = new MemberRole();
                JsonConvert.PopulateObject(values, data);
                data.CreatedTime = DateTime.Now;
                Model.DataContext.MemberRole.Add(data);
                Model.DataContext.SaveChanges();
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        public IActionResult DeleteMemberRole(Guid memberId, Guid roleId) {
            try {
                var data = Model.DataContext.MemberRole.Find(memberId, roleId);
                if (data == null) return NotFound();
                Model.DataContext.MemberRole.Remove(data);
                Model.DataContext.SaveChanges();
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public object GetRoles(DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.Role.Where(e => e.DataMode == DataMode.Normal);
            return DataSourceLoader.Load(query, loadOptions);
        }

        #endregion

        #region SchoolAssignment CRUD

        [HttpGet]
        public object GetSchoolAssignments(Guid memberId, DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.SchoolAssignment
                .Where(e => e.MemberId == memberId)
                .Select(e => new { e.Id, e.SchoolId, e.MemberId, e.Name, e.CreatedTime, e.UpdatedTime });
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpPost]
        public IActionResult CreateSchoolAssignment(string values) {
            try {
                var data = new SchoolAssignment();
                JsonConvert.PopulateObject(values, data);
                data.CreatedTime = DateTime.Now;
                Model.DataContext.SchoolAssignment.Add(data);
                Model.DataContext.SaveChanges();
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public IActionResult UpdateSchoolAssignment(int key, string values) {
            try {
                var data = Model.DataContext.SchoolAssignment.Find(key);
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
        public IActionResult DeleteSchoolAssignment(int key) {
            try {
                var data = Model.DataContext.SchoolAssignment.Find(key);
                if (data == null) return NotFound();
                Model.DataContext.SchoolAssignment.Remove(data);
                Model.DataContext.SaveChanges();
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public object GetSchools(DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.School.OrderBy(e => e.Ordinal);
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpGet]
        public object GetAssignedSchools(Guid memberId, DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.SchoolAssignment
                .Where(e => e.MemberId == memberId)
                .Select(e => new { Id = e.SchoolId, Name = e.School.Name });
            return DataSourceLoader.Load(query, loadOptions);
        }

        #endregion
    }
}
