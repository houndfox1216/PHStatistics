using System;
using System.Collections.Generic;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PHStatistics.Actions;
using PHStatistics.Content;
using System.Framework.Data;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [RequirePermission(SystemPermission.SchoolYear)]
    public class SchoolYearController : AdminBaseController {
        public IActionResult Index() {
            ViewBag.Title = "學年度管理";
            return View();
        }

        [HttpGet]
        public object GetSchoolYears(DataSourceLoadOptions loadOptions) {
            var readAction = new SchoolYearReadAction(User, Model.DataContext);
            var query = readAction.Query(new Condition(), (Sorting[])null);
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpPost]
        public IActionResult Create(string values) {
            try {
                var data = new SchoolYear();
                JsonConvert.PopulateObject(values, data);
                var createAction = new SchoolYearCreateAction(User, Model.DataContext);
                createAction.Create(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public IActionResult Update(int key, string values) {
            try {
                var readAction = new SchoolYearReadAction(User, Model.DataContext, true);
                var data = readAction.Find(key);
                if (data == null) return NotFound();
                JsonConvert.PopulateObject(values, data);
                var updateAction = new SchoolYearUpdateAction(User, Model.DataContext);
                updateAction.Update(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        public IActionResult Delete(int key) {
            try {
                var deleteAction = new SchoolYearDeleteAction(User, Model.DataContext);
                deleteAction.Delete(new SchoolYear { Id = key });
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 批次產生整學年度的週次資料。
        /// 以第一週起始日（星期一）為基準，逐週產生至隔年6月30日止。
        /// ImportEndDate 為隔週星期一中午12:00。
        /// </summary>
        [HttpPost]
        public IActionResult BatchGenerate(int year, int adYear, DateTime firstWeekStartDate) {
            try {
                // 學年度結束：隔年7月1日（不含）
                var schoolYearEnd = new DateTime(adYear + 1, 7, 1);
                var currentMonday = firstWeekStartDate.Date;
                var week = 1;
                var count = 0;

                while (currentMonday < schoolYearEnd) {
                    var schoolYear = new SchoolYear {
                        Year = year,
                        ADYear = currentMonday.Year,
                        Week = week,
                        Name = $"{year}學年度 第{week}週",
                        WeekStartDate = currentMonday,
                        WeekEndDate = currentMonday.AddDays(6),           // 星期日
                        ImportEndDate = currentMonday.AddDays(7).AddHours(12), // 隔週星期一 12:00
                    };

                    new SchoolYearCreateAction(User, Model.DataContext).Create(schoolYear);
                    currentMonday = currentMonday.AddDays(7);
                    week++;
                    count++;
                }

                return Ok(new { count });
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }
}
