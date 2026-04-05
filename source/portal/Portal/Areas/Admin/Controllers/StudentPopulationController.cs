using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
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

        [HttpPost]
        public IActionResult Approve(long key) {
            try {
                var data = Model.DataContext.StudentPopulation.Find(key);
                if (data == null) return NotFound();
                data.Status = StudentPopulationStatus.Approved;
                data.ConfirmTime = DateTime.Now;
                data.UpdatedTime = DateTime.Now;
                if (Guid.TryParse(User.Id, out var confirmerId))
                    data.ConfirmerId = confirmerId;
                Model.DataContext.SaveChanges();
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
        public IActionResult ExportExcel(int? schoolId, int? year, int? week) {
            // 未提供查詢條件時，使用當周學年度
            if (year == null || week == null) {
                var today = DateTime.Today;
                var current = Model.DataContext.SchoolYear
                    .Where(sy => sy.WeekStartDate <= today && sy.WeekEndDate >= today)
                    .FirstOrDefault();
                if (current != null) {
                    year ??= current.Year;
                    week ??= current.Week;
                }
            }

            var query = Model.DataContext.StudentPopulation
                .Include(e => e.School)
                .Where(e => e.DataMode == DataMode.Normal);

            if (schoolId.HasValue) query = query.Where(e => e.SchoolId == schoolId);
            if (year.HasValue)     query = query.Where(e => e.Year == year);
            if (week.HasValue)     query = query.Where(e => e.Week == week);

            var data = query
                .OrderByDescending(e => e.Year).ThenByDescending(e => e.Week)
                .ThenBy(e => e.School.Ordinal)
                .ToList();

            var wb    = new XSSFWorkbook();
            var sheet = wb.CreateSheet("人數表管理");

            var boldFont = wb.CreateFont();
            boldFont.FontName = "Arial"; boldFont.FontHeightInPoints = 10; boldFont.IsBold = true;

            var normalFont = wb.CreateFont();
            normalFont.FontName = "Arial"; normalFont.FontHeightInPoints = 10;

            ICellStyle HeaderStyle() {
                var s = wb.CreateCellStyle();
                s.SetFont(boldFont);
                s.Alignment = HorizontalAlignment.Center;
                s.FillForegroundColor = IndexedColors.LightYellow.Index;
                s.FillPattern = FillPattern.SolidForeground;
                s.BorderTop = s.BorderBottom = s.BorderLeft = s.BorderRight = BorderStyle.Thin;
                return s;
            }

            ICellStyle DataStyle(HorizontalAlignment align = HorizontalAlignment.Left) {
                var s = wb.CreateCellStyle();
                s.SetFont(normalFont);
                s.Alignment = align;
                s.BorderTop = s.BorderBottom = s.BorderLeft = s.BorderRight = BorderStyle.Thin;
                return s;
            }

            var hStyle  = HeaderStyle();
            var dLeft   = DataStyle();
            var dCenter = DataStyle(HorizontalAlignment.Center);

            // ── 表頭 ──
            var headers = new[] { "識別碼", "分校", "學年度", "週次", "類型", "狀態", "名稱", "詢問人數", "送出時間", "建立時間" };
            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < headers.Length; i++) {
                var c = headerRow.CreateCell(i);
                c.SetCellValue(headers[i]);
                c.CellStyle = hStyle;
            }

            // ── 資料列 ──
            static string TypeName(StudentPopulationType t) => t switch {
                StudentPopulationType.PH          => "百瀚 (PH)",
                StudentPopulationType.PSJ         => "百倍數 (PSJ)",
                StudentPopulationType.GEPT        => "英檢班 (GEPT)",
                StudentPopulationType.PS          => "百世 (PS)",
                StudentPopulationType.AfterSchool => "安親課輔 (AfterSchool)",
                _                                 => t.ToString()
            };

            static string StatusName(StudentPopulationStatus s) => s switch {
                StudentPopulationStatus.Documented => "已建檔",
                StudentPopulationStatus.Pending    => "已送出",
                StudentPopulationStatus.Approved   => "已審核",
                StudentPopulationStatus.Rejected   => "已否決",
                StudentPopulationStatus.Finished   => "已完成",
                _                                  => s.ToString()
            };

            int rowIdx = 1;
            foreach (var sp in data) {
                var row = sheet.CreateRow(rowIdx++);
                void SetNum(int col, long val)    { var c = row.CreateCell(col); c.SetCellValue(val); c.CellStyle = dCenter; }
                void SetStr(int col, string val)  { var c = row.CreateCell(col); c.SetCellValue(val); c.CellStyle = dLeft;   }
                void SetDate(int col, DateTime? val) {
                    var c = row.CreateCell(col);
                    c.SetCellValue(val.HasValue ? val.Value.ToString("yyyy/MM/dd HH:mm") : "");
                    c.CellStyle = dCenter;
                }

                SetNum(0,  sp.Id);
                SetStr(1,  sp.School?.Name ?? "");
                SetNum(2,  sp.Year);
                SetNum(3,  sp.Week);
                SetStr(4,  TypeName(sp.Type));
                SetStr(5,  StatusName(sp.Status));
                SetStr(6,  sp.Name ?? "");
                SetNum(7,  sp.TotalInquiryCount);
                SetDate(8, sp.SubmitterTime);
                SetDate(9, sp.CreatedTime);
            }

            // ── 欄寬 ──
            int[] colWidths = { 10, 16, 10, 8, 20, 10, 20, 10, 18, 18 };
            for (int i = 0; i < colWidths.Length; i++)
                sheet.SetColumnWidth(i, colWidths[i] * 256);

            sheet.CreateFreezePane(0, 1);

            using var ms = new MemoryStream();
            wb.Write(ms);
            var bytes = ms.ToArray();

            string yearLabel = year.HasValue ? $"{year}學年度" : "全學年度";
            string weekLabel = week.HasValue ? $"第{week}週"   : "全週次";
            string fileName  = Uri.EscapeDataString($"人數表管理_{yearLabel}{weekLabel}.xlsx");

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

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
