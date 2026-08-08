using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PHStatistics.Actions;
using PHStatistics.Content;
using System.Framework.Data;
using System.Linq;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [RequirePermission(SystemPermission.ActionLog)]
    public class OperationLogController : AdminBaseController {
        public IActionResult Index() {
            ViewBag.Title = "操作紀錄";
            return View();
        }

        [HttpGet]
        public object GetActionLogs(DataSourceLoadOptions loadOptions) {
            var readAction = new ActionLogReadAction(User, Model.DataContext);
            var query = readAction.Query(new Condition(), (Sorting[])null)
                .OrderByDescending(e => e.CreatedTime);
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpGet]
        public object GetItemLogs(DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.StudentPopulationItemLog
                .Include(e => e.StudentPopulation).ThenInclude(sp => sp.School)
                .Include(e => e.Member)
                .OrderByDescending(e => e.Id)
                .Select(e => new ItemLogRow {
                    Id = e.Id,
                    StudentPopulationId = e.StudentPopulationId,
                    SchoolName = e.StudentPopulation.School.Name,
                    Year = e.StudentPopulation.Year,
                    Week = e.StudentPopulation.Week,
                    Type = e.StudentPopulation.Type,
                    ClassName = e.Name,
                    Number = e.Number,
                    ChangeNumber = e.ChangeNumber,
                    LastWeekNumber = e.LastWeekNumber,
                    ChangeLastWeekNumber = e.ChangeLastWeekNumber,
                    Remark = e.Remark,
                    ChangeRemark = e.ChangeRemark,
                    StudentRemark = e.StudentRemark,
                    ChangeStudentRemark = e.ChangeStudentRemark,
                    IsNew = e.IsNew,
                    IsDeleted = e.IsDeleted,
                    MemberName = e.Member != null ? e.Member.Nickname : null
                });
            return DataSourceLoader.Load(query, loadOptions);
        }

        public class ItemLogRow {
            public long Id { get; set; }
            public long StudentPopulationId { get; set; }
            public string SchoolName { get; set; }
            public int Year { get; set; }
            public int Week { get; set; }
            public StudentPopulationType Type { get; set; }
            public string ClassName { get; set; }
            public int Number { get; set; }
            public int ChangeNumber { get; set; }
            public int? LastWeekNumber { get; set; }
            public int? ChangeLastWeekNumber { get; set; }
            public string Remark { get; set; }
            public string ChangeRemark { get; set; }
            public string StudentRemark { get; set; }
            public string ChangeStudentRemark { get; set; }
            public bool IsNew { get; set; }
            public bool IsDeleted { get; set; }
            public string MemberName { get; set; }
        }
    }
}
