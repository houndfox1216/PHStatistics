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
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using PHStatistics.Actions;
using PHStatistics.Content;
using PHStatistics.Portal.Services;
using System.Framework.Data;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [RequirePermission(SystemPermission.StudentPopulation)]
    public class StudentPopulationController : AdminBaseController {
        private readonly ReportExportService _reportExport;

        public StudentPopulationController(ReportExportService reportExport) {
            _reportExport = reportExport;
        }

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

            // 依班系類型排序，同類型再依分校順序/學年度/週次
            var data = query
                .OrderBy(e => e.Type)
                .ThenBy(e => e.School.Ordinal)
                .ThenByDescending(e => e.Year)
                .ThenByDescending(e => e.Week)
                .ToList();

            var wb    = new XSSFWorkbook();
            var sheet = wb.CreateSheet("人數表管理");

            var boldFont = wb.CreateFont();
            boldFont.FontName = "Arial"; boldFont.FontHeightInPoints = 10; boldFont.IsBold = true;

            var normalFont = wb.CreateFont();
            normalFont.FontName = "Arial"; normalFont.FontHeightInPoints = 10;

            NPOI.SS.UserModel.ICellStyle MakeStyle(IFont font,
                NPOI.SS.UserModel.HorizontalAlignment align,
                short fillColor = -1) {
                var s = wb.CreateCellStyle();
                s.SetFont(font);
                s.Alignment = align;
                if (fillColor >= 0) {
                    s.FillForegroundColor = fillColor;
                    s.FillPattern = FillPattern.SolidForeground;
                }
                s.BorderTop = s.BorderBottom = s.BorderLeft = s.BorderRight = BorderStyle.Thin;
                return s;
            }

            var hStyle     = MakeStyle(boldFont,   NPOI.SS.UserModel.HorizontalAlignment.Center, IndexedColors.LightYellow.Index);
            var dLeft      = MakeStyle(normalFont, NPOI.SS.UserModel.HorizontalAlignment.Left);
            var dCenter    = MakeStyle(normalFont, NPOI.SS.UserModel.HorizontalAlignment.Center);
            var subtLeft   = MakeStyle(boldFont,   NPOI.SS.UserModel.HorizontalAlignment.Left,   IndexedColors.LightCornflowerBlue.Index);
            var subtCenter = MakeStyle(boldFont,   NPOI.SS.UserModel.HorizontalAlignment.Center, IndexedColors.LightCornflowerBlue.Index);
            var totLeft    = MakeStyle(boldFont,   NPOI.SS.UserModel.HorizontalAlignment.Left,   IndexedColors.LightOrange.Index);
            var totCenter  = MakeStyle(boldFont,   NPOI.SS.UserModel.HorizontalAlignment.Center, IndexedColors.LightOrange.Index);

            // ── 表頭 ──
            var headers = new[] { "識別碼", "分校", "學年度", "週次", "類型", "狀態", "名稱", "詢問人數", "送出時間", "建立時間" };
            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < headers.Length; i++) {
                var c = headerRow.CreateCell(i);
                c.SetCellValue(headers[i]);
                c.CellStyle = hStyle;
            }

            static string TypeName(StudentPopulationType t) => t switch {
                StudentPopulationType.PH          => "百瀚 (PH)",
                StudentPopulationType.PSJ         => "百倍速 (PSJ)",
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

            // 將欄位索引轉為 Excel 欄位字母（0-indexed → A, B, ... Z, AA, ...）
            static string ColLetter(int idx) => idx < 26
                ? ((char)('A' + idx)).ToString()
                : ((char)('A' + idx / 26 - 1)).ToString() + ((char)('A' + idx % 26)).ToString();

            const int NumericCol = 7; // 詢問人數 (H 欄)
            var subtotalExcelRows = new List<int>();
            int rowIdx = 1;

            // ── 資料列（依班系類型分組，每組末尾插入合計列）──
            foreach (var group in data.GroupBy(e => e.Type)) {
                int groupStartExcelRow = rowIdx + 1; // Excel 列號從 1 起，+1 跳過表頭

                foreach (var sp in group) {
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

                int groupEndExcelRow = rowIdx; // 最後一筆資料列的 Excel 列號

                // 班系類型合計列
                var subtRow = sheet.CreateRow(rowIdx);
                int subtExcelRow = rowIdx + 1;
                subtotalExcelRows.Add(subtExcelRow);
                rowIdx++;

                for (int ci = 0; ci < headers.Length; ci++) {
                    var cell = subtRow.CreateCell(ci);
                    if (ci == 4) {
                        cell.SetCellValue($"{TypeName(group.Key)} 合計");
                        cell.CellStyle = subtLeft;
                    } else if (ci == NumericCol) {
                        cell.SetCellFormula($"SUM({ColLetter(ci)}{groupStartExcelRow}:{ColLetter(ci)}{groupEndExcelRow})");
                        cell.CellStyle = subtCenter;
                    } else {
                        cell.SetCellValue("");
                        cell.CellStyle = subtCenter;
                    }
                }
            }

            // ── 總計列 ──
            if (subtotalExcelRows.Count > 0) {
                var totalRow = sheet.CreateRow(rowIdx);
                for (int ci = 0; ci < headers.Length; ci++) {
                    var cell = totalRow.CreateCell(ci);
                    if (ci == 4) {
                        cell.SetCellValue("總計");
                        cell.CellStyle = totLeft;
                    } else if (ci == NumericCol) {
                        string sumArgs = string.Join(",", subtotalExcelRows.Select(r => $"{ColLetter(ci)}{r}"));
                        cell.SetCellFormula($"SUM({sumArgs})");
                        cell.CellStyle = totCenter;
                    } else {
                        cell.SetCellValue("");
                        cell.CellStyle = totCenter;
                    }
                }
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

        // ─────────────────────────────────────────────────────────────────────
        //  PH 專屬報表匯出（每區一個工作表，每分校兩列：小班 / 三人班）
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult ExportPH(int? year, int? week) {
            if (year == null || week == null) {
                var today = DateTime.Today;
                var current = Model.DataContext.SchoolYear
                    .Where(sy => sy.WeekStartDate <= today && sy.WeekEndDate >= today)
                    .FirstOrDefault();
                if (current != null) { year ??= current.Year; week ??= current.Week; }
            }

            // ── 課程欄位結構（依 CourseDepartment → Course 排序）──
            var departments = Model.DataContext.CourseDepartment
                .Where(d => d.Type == StudentPopulationType.PH && !d.IsSum
                         && d.Published && d.DataMode == DataMode.Normal)
                .OrderBy(d => d.Ordinal)
                .ToList();

            var courses = Model.DataContext.Course
                .Where(c => c.Type == StudentPopulationType.PH && !c.IsSum
                         && c.Published && c.DataMode == DataMode.Normal)
                .OrderBy(c => c.Ordinal)
                .ToList();

            // colDefs: 依顯示順序排列的 (班系, 課程) 組合
            var colDefs = departments
                .SelectMany(d => courses
                    .Where(c => c.DepartmentId == d.Id)
                    .Select(c => (dept: d, course: c)))
                .ToList();

            // ── 各分校各課程人數 key=(schoolId, courseId, classType) ──
            var itemLookup = Model.DataContext.StudentPopulationItem
                .Include(i => i.StudentPopulation)
                .Include(i => i.Class).ThenInclude(cls => cls.Course)
                .Where(i => i.StudentPopulation.Type == StudentPopulationType.PH
                         && i.StudentPopulation.DataMode == DataMode.Normal
                         && i.DataMode == DataMode.Normal
                         && !i.Class.Course.IsSum
                         && (!year.HasValue || i.StudentPopulation.Year == year)
                         && (!week.HasValue || i.StudentPopulation.Week == week))
                .AsNoTracking()
                .AsEnumerable()
                .GroupBy(i => (schoolId: i.StudentPopulation.SchoolId ?? 0,
                               courseId: i.Class.Course.Id,
                               classType: i.Class.Type))
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Number));

            // ── 地區與分校 ──
            var regions = Model.DataContext.Region
                .Where(r => r.DataMode == DataMode.Normal)
                .OrderBy(r => r.Ordinal)
                .ToList();

            var schools = Model.DataContext.School
                .Where(s => s.Published && s.DataMode == DataMode.Normal && s.RegionId != null)
                .OrderBy(s => s.Ordinal)
                .ToList();

            // ── NPOI 樣式工廠 ──
            var wb = new XSSFWorkbook();

            var boldFont   = wb.CreateFont();
            boldFont.FontName = "Arial"; boldFont.FontHeightInPoints = 10; boldFont.IsBold = true;
            var normalFont = wb.CreateFont();
            normalFont.FontName = "Arial"; normalFont.FontHeightInPoints = 10;

            NPOI.SS.UserModel.ICellStyle MkStyle(IFont font,
                NPOI.SS.UserModel.HorizontalAlignment hAlign,
                short fillColor = -1, bool wrap = false) {
                var s = wb.CreateCellStyle();
                s.SetFont(font);
                s.Alignment = hAlign;
                s.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                if (fillColor >= 0) { s.FillForegroundColor = fillColor; s.FillPattern = FillPattern.SolidForeground; }
                s.BorderTop = s.BorderBottom = s.BorderLeft = s.BorderRight = BorderStyle.Thin;
                s.WrapText = wrap;
                return s;
            }

            var sTitle   = MkStyle(boldFont,   NPOI.SS.UserModel.HorizontalAlignment.Center, IndexedColors.LightYellow.Index);
            var sHeader  = MkStyle(boldFont,   NPOI.SS.UserModel.HorizontalAlignment.Center, IndexedColors.LightYellow.Index, wrap: true);
            var sLeft    = MkStyle(normalFont, NPOI.SS.UserModel.HorizontalAlignment.Left);
            var sCenter  = MkStyle(normalFont, NPOI.SS.UserModel.HorizontalAlignment.Center);
            var sSubtL   = MkStyle(boldFont,   NPOI.SS.UserModel.HorizontalAlignment.Left,   IndexedColors.LightCornflowerBlue.Index);
            var sSubtC   = MkStyle(boldFont,   NPOI.SS.UserModel.HorizontalAlignment.Center, IndexedColors.LightCornflowerBlue.Index);

            static string ColLetter(int idx) => idx < 26
                ? ((char)('A' + idx)).ToString()
                : ((char)('A' + idx / 26 - 1)).ToString() + ((char)('A' + idx % 26)).ToString();

            const int COL_SCHOOL = 0;
            const int COL_MODE   = 1;
            const int COL_DATA   = 2;

            // 每分校兩列：小班(SubGroup) 和 三人班(V3)
            var ctRows = new[] {
                (ct: ClassType.SubGroup, label: "小"),
                (ct: ClassType.V3,       label: "三"),
            };

            // ── 每個地區建立一個工作表 ──
            foreach (var region in regions) {
                var regionSchools = schools.Where(s => s.RegionId == region.Id).ToList();
                if (!regionSchools.Any()) continue;

                var sheet     = wb.CreateSheet(region.Name);
                int totalCols = COL_DATA + colDefs.Count;

                // 第 0 列：標題
                {
                    var row = sheet.CreateRow(0);
                    var c   = row.CreateCell(0);
                    c.SetCellValue($"百瀚英語{region.Name}分校人數統計表");
                    c.CellStyle = sTitle;
                    sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, totalCols - 1));
                    for (int ci = 1; ci < totalCols; ci++) row.CreateCell(ci).CellStyle = sTitle;
                }

                // 第 1 列：班系標頭（CourseDepartment 名稱橫跨其課程欄）
                {
                    var row = sheet.CreateRow(1);
                    // 校名 & 開班模式：跨第 1-2 列合併
                    var c0 = row.CreateCell(COL_SCHOOL); c0.SetCellValue("校名/班別/人數"); c0.CellStyle = sHeader;
                    sheet.AddMergedRegion(new CellRangeAddress(1, 2, COL_SCHOOL, COL_SCHOOL));
                    var c1 = row.CreateCell(COL_MODE);   c1.SetCellValue("開班模式");       c1.CellStyle = sHeader;
                    sheet.AddMergedRegion(new CellRangeAddress(1, 2, COL_MODE, COL_MODE));

                    int offset = COL_DATA;
                    foreach (var dept in departments) {
                        var dc = colDefs.Where(x => x.dept.Id == dept.Id).ToList();
                        if (!dc.Any()) continue;
                        int s = offset, e = offset + dc.Count - 1;
                        var cell = row.CreateCell(s); cell.SetCellValue(dept.Name); cell.CellStyle = sHeader;
                        if (e > s) {
                            sheet.AddMergedRegion(new CellRangeAddress(1, 1, s, e));
                            for (int ci = s + 1; ci <= e; ci++) row.CreateCell(ci).CellStyle = sHeader;
                        }
                        offset += dc.Count;
                    }
                }

                // 第 2 列：課程名稱
                {
                    var row = sheet.CreateRow(2);
                    row.CreateCell(COL_SCHOOL).CellStyle = sHeader;
                    row.CreateCell(COL_MODE).CellStyle   = sHeader;
                    for (int ci = 0; ci < colDefs.Count; ci++) {
                        var cell = row.CreateCell(COL_DATA + ci);
                        cell.SetCellValue(colDefs[ci].course.Name);
                        cell.CellStyle = sHeader;
                    }
                }

                // 第 3 列起：每分校兩筆資料列
                int rowIdx          = 3;
                int dataStartExcel  = rowIdx + 1;   // Excel 列號從 1 起

                foreach (var school in regionSchools) {
                    int schoolSheetRow = rowIdx;

                    for (int ri = 0; ri < ctRows.Length; ri++) {
                        var (ct, label) = ctRows[ri];
                        var row = sheet.CreateRow(rowIdx++);

                        // 校名（第一列才設值，兩列合併）
                        var nameCell = row.CreateCell(COL_SCHOOL);
                        nameCell.CellStyle = sLeft;
                        if (ri == 0) {
                            nameCell.SetCellValue(school.Name);
                            sheet.AddMergedRegion(new CellRangeAddress(
                                schoolSheetRow, schoolSheetRow + ctRows.Length - 1,
                                COL_SCHOOL, COL_SCHOOL));
                        }

                        // 開班模式
                        var modeCell = row.CreateCell(COL_MODE);
                        modeCell.SetCellValue(label);
                        modeCell.CellStyle = sCenter;

                        // 各課程人數
                        for (int ci = 0; ci < colDefs.Count; ci++) {
                            var cell  = row.CreateCell(COL_DATA + ci);
                            int count = itemLookup.GetValueOrDefault(
                                (schoolId: school.Id, courseId: colDefs[ci].course.Id, classType: ct), 0);
                            cell.SetCellValue(count);
                            cell.CellStyle = sCenter;
                        }
                    }
                }

                int dataEndExcel = rowIdx; // 最後一筆資料的 Excel 列號

                // 小計列
                {
                    var row = sheet.CreateRow(rowIdx);
                    var c0  = row.CreateCell(COL_SCHOOL); c0.SetCellValue("小計"); c0.CellStyle = sSubtL;
                    sheet.AddMergedRegion(new CellRangeAddress(rowIdx, rowIdx, COL_SCHOOL, COL_MODE));
                    row.CreateCell(COL_MODE).CellStyle = sSubtC;
                    for (int ci = 0; ci < colDefs.Count; ci++) {
                        int col  = COL_DATA + ci;
                        var cell = row.CreateCell(col);
                        cell.SetCellFormula($"SUM({ColLetter(col)}{dataStartExcel}:{ColLetter(col)}{dataEndExcel})");
                        cell.CellStyle = sSubtC;
                    }
                }

                // 欄寬
                sheet.SetColumnWidth(COL_SCHOOL, 14 * 256);
                sheet.SetColumnWidth(COL_MODE,    5 * 256);
                for (int ci = 0; ci < colDefs.Count; ci++)
                    sheet.SetColumnWidth(COL_DATA + ci, 8 * 256);

                sheet.CreateFreezePane(COL_DATA, 3);
            }

            using var ms2 = new MemoryStream();
            wb.Write(ms2);
            var bytes2 = ms2.ToArray();

            string yLabel = year.HasValue ? $"{year}學年度" : "全學年度";
            string wLabel = week.HasValue ? $"第{week}週"   : "全週次";
            string fname  = Uri.EscapeDataString($"百瀚人數表_{yLabel}{wLabel}.xlsx");
            return File(bytes2,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fname);
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

        [HttpGet]
        public IActionResult ExportReport(int year, int week, string reportType, int? schoolId = null) {
            var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PH", "PS", "GEPT", "PSJ", "AfterSchool" };
            if (!validTypes.Contains(reportType))
                return BadRequest($"不支援的報表類型：{reportType}");

            var type = reportType switch {
                "PH"   => StudentPopulationType.PH,
                "PS"   => StudentPopulationType.PS,
                "GEPT" => StudentPopulationType.GEPT,
                "PSJ"  => StudentPopulationType.PSJ,
                "AS"   => StudentPopulationType.AfterSchool,
                _      => StudentPopulationType.PH
            };

            // 管理後台：指定單一分校時限定，否則匯出全部分校
            IList<int> schoolIds = schoolId.HasValue
                ? new List<int> { schoolId.Value }
                : null; // null = 所有分校

            var bytes = _reportExport.Export(type, year, week, schoolIds);
            if (bytes.Length == 0)
                return NotFound("查無符合條件的資料");

            string title = reportType switch {
                "PH"   => $"{year}年第{week}週百瀚英語全國人數表",
                "GEPT" => $"{year}年第{week}週英檢人數表",
                "PS"   => $"{year}年第{week}週百世人數表",
                "PSJ"  => $"{year}年第{week}週百倍速人數表",
                "AS"   => $"{year}年第{week}週課輔人數表",
                _      => $"{year}年第{week}週人數表"
            };
            string fileName = Uri.EscapeDataString($"{title}.xlsx");
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
