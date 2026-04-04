using System;
using System.Linq;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PHStatistics.Actions;
using PHStatistics.Content;
using System.Framework.Data;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [RequirePermission(SystemPermission.StudentPopulation)]
    public class StudentPopulationController : AdminBaseController {
        public IActionResult Index() {
            ViewBag.Title = "人數表管理";
            return View();
        }

        #region StudentPopulation CRUD

        [HttpGet]
        public object GetStudentPopulations(DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.StudentPopulation
                .Include(e => e.School)
                .Where(e => e.DataMode == DataMode.Normal)
                .OrderByDescending(e => e.Year).ThenByDescending(e => e.Week);
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpPost]
        public IActionResult Create(string values) {
            try {
                var data = new StudentPopulation();
                JsonConvert.PopulateObject(values, data);
                new StudentPopulationCreateAction(User, Model.DataContext).Create(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public IActionResult Update(long key, string values) {
            try {
                var data = Model.DataContext.StudentPopulation.Find(key);
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
        public IActionResult Delete(long key) {
            try {
                new StudentPopulationDeleteAction(User, Model.DataContext)
                    .Delete(new StudentPopulation { Id = key });
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region StudentPopulationItem CRUD

        [HttpGet]
        public object GetItems(long populationId, DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.StudentPopulationItem
                .Where(e => e.StudentPopulationId == populationId && e.DataMode == DataMode.Normal);
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpPost]
        public IActionResult CreateItem(string values) {
            try {
                var data = new StudentPopulationItem();
                JsonConvert.PopulateObject(values, data);
                new StudentPopulationItemCreateAction(User, Model.DataContext).Create(data);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public IActionResult UpdateItem(long key, string values) {
            try {
                var data = Model.DataContext.StudentPopulationItem.Find(key);
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
        public IActionResult DeleteItem(long key) {
            try {
                new StudentPopulationItemDeleteAction(User, Model.DataContext)
                    .Delete(new StudentPopulationItem { Id = key });
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        [HttpGet]
        public object GetSchools(DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.School.OrderBy(e => e.Ordinal);
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpGet]
        public object GetYears(DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.SchoolYear
                .GroupBy(e => e.Year)
                .Select(g => new { Year = g.Key })
                .OrderByDescending(e => e.Year);
            return DataSourceLoader.Load(query, loadOptions);
        }

        [HttpGet]
        public object GetWeeks(int year, DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.SchoolYear
                .Where(e => e.Year == year)
                .Select(e => e.Week)
                .Distinct()
                .OrderBy(e => e)
                .Select(w => new { Week = w });
            return DataSourceLoader.Load(query, loadOptions);
        }
    }
}
