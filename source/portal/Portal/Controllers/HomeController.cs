using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Framework;
using System.Framework.Globalization;
using System.Framework.Web;
using System.IO;
using System.Reflection;
using PHStatistics.Portal.Models;
using Microsoft.AspNetCore.Mvc;
using System.Framework.Logging;
using PHStatistics.Content;
using System.Linq;
using System.Framework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nest;
using FluentFTP.Helpers;
using Environment = System.Framework.Environment;
using System.Framework.Data;
using System.Text.Json;
using Castle.Core.Resource;
using NPOI;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using PHStatistics.Community;
using System.Framework.Security;



namespace PHStatistics.Portal.Controllers {
    [Route("/")]
    public class HomeController() : MvcController<PortalUser, Model, Culture>("System") {
        [Route("/")]
        [Route("Index")]
        [Authorize(typeof(PortalUser))]
        public IActionResult Index() {
            Logger.LogInformation("進入首頁");
            DataContext dataContext = new DataContext();
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.WeekEndDate >= dateTime).FirstOrDefault();
            ViewBag.CanEdit = schoolYear != null;
            ViewBag.Title = "Home Page".ToI18n(Culture.GetCode());
            ViewBag.BannerPositions = new List<BannerPosition>();
            ViewBag.BannerPositions.Add(Model.BannerPosition.FindByCode("Home.Slider"));
            return View();
        }

        [Route("Culture/{code}")]
        public IActionResult ChangeCulture(string code) {
            Culture = Model.GetCulture(code);
            var referer = Request.Headers.Referer.ToString();
            return Redirect(referer.HasValue() ? referer : "/");
        }

        [Route("error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            ViewBag.Title = "Error".ToI18n(Culture.GetCode());
            return View("Error", Activity.Current?.Id ?? HttpContext.TraceIdentifier);
        }

        [Route("Sitemap")]
        [Route("sitemap.xml")]
        public IActionResult Sitemap() {
            const string virtualPath = "~/sitemap.xml";
            var file = new FileInfo(Environment.GetRealPath(virtualPath));
            if (file.Exists && file.LastWriteTime.Date >= DateTime.Today) return File("~/sitemap.xml", "text/xml");
            var sitemap = new Sitemap("~/", Application.Policy.ModifySitemap<DataContext>(HttpContext));
            sitemap.Save();
            return Content(sitemap.ToXml(), "text/xml");
        }

        [Route("Information")]
        public IActionResult Information() {
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var appVersion = Application.Configuration["Version"].ToString();
            return Content(new { assemblyVersion, appVersion }.ToJson(), "text/json");
        }

        #region 資料匯入處理
        [HttpGet("ImportPIData")]
        public IActionResult ImportPIData(string type) {
            try {
                using (FileStream file = new FileStream(@"C:\Leo\其他\Kuri\人數表\班系課程整理20240729-修改20240902-匯入.xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new DataException("取得資料發生錯誤");
                    try {
                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet1 = workbook.GetSheetAt(0);
                        ISheet sheet2 = workbook.GetSheetAt(1);
                        ISheet sheet3 = workbook.GetSheetAt(2);
                        ISheet sheet4 = workbook.GetSheetAt(3);
                        List<ImportData> ph = new List<ImportData>();
                        List<ImportData> gept = new List<ImportData>();
                        List<ImportData> ps = new List<ImportData>();
                        List<ImportData> psj = new List<ImportData>();

                        var list = new List<ISheet>() { sheet1, sheet2, sheet3, sheet4 };
                        var count = 0;
                        for (int k = 0; k < workbook.NumberOfSheets; k++) {
                            string[] input = new string[2];
                            var sheet = workbook.GetSheetAt(k); ;
                            string comName = sheet.SheetName;
                            for (int row = 0; row <= sheet.LastRowNum; row++) {
                                XSSFRow xlRow = sheet.GetRow(row) as XSSFRow; //取得每一列
                                ImportData newItem = new ImportData();
                                if (xlRow != null) {
                                    for (int col = 0; col < xlRow.LastCellNum; col++) {
                                        XSSFCell xlCell = xlRow.GetCell(col) as XSSFCell; //取得目前列的每個儲存格
                                        string value = string.Empty;
                                        //取得儲存格的值
                                        if (xlCell.CellType == CellType.Numeric) {
                                            if (DateUtil.IsCellDateFormatted(xlCell))
                                                value = xlCell.DateCellValue.ToString(); //日期格式
                                            else
                                                value = xlCell.NumericCellValue.ToString(); //數值格式
                                        }
                                        else if (xlCell.CellType == CellType.String) {
                                            value = xlCell.StringCellValue; //字串格式
                                        }
                                        if (col == 0) {
                                            newItem.Department = value;
                                        }
                                        else {
                                            newItem.Course = value;
                                        }
                                    }
                                }
                                else {
                                    continue;
                                }

                                if (k == 0) {
                                    if(newItem.Department != null && !string.IsNullOrEmpty(newItem.Department) && newItem.Course != null && !string.IsNullOrEmpty(newItem.Course)) {
                                        ph.Add(newItem);
                                    }
                                }
                                else if (k == 1) {
                                    if (newItem.Department != null && !string.IsNullOrEmpty(newItem.Department) && newItem.Course != null && !string.IsNullOrEmpty(newItem.Course)) {
                                        gept.Add(newItem);
                                    }
                                }
                                else if (k == 2) {
                                    if (newItem.Department != null && !string.IsNullOrEmpty(newItem.Department) && newItem.Course != null && !string.IsNullOrEmpty(newItem.Course)) {
                                        ps.Add(newItem);
                                    }
                                }
                                else if (k == 3) {
                                    if (newItem.Department != null && !string.IsNullOrEmpty(newItem.Department) && newItem.Course != null && !string.IsNullOrEmpty(newItem.Course)) {
                                        psj.Add(newItem);
                                    }
                                }
                            }
                        }
                        using (DataContext dataContext = new DataContext()) {
                            int departmentOrdinal = 0;
                            int courseOrdinal = 0;
                            foreach (ImportData phItem in ph) {
                                if (phItem != null) {
                                    CourseDepartment courseDepartment = new CourseDepartment();
                                    if (dataContext.CourseDepartment.Any(e => e.Name == phItem.Department)) {
                                        courseDepartment = dataContext.CourseDepartment.FirstOrDefault(e => e.Name == phItem.Department);
                                    }
                                    else {
                                        courseDepartment.Name = phItem.Department;
                                        courseDepartment.Ordinal = departmentOrdinal;
                                        courseDepartment.Company = Company.PH;
                                        dataContext.CourseDepartment.Add(courseDepartment);
                                        dataContext.SaveChanges();
                                        departmentOrdinal++;
                                    }
                                    Course course = new Course();
                                    if (dataContext.Course.Any(e => e.Name == phItem.Course)) {
                                        continue;
                                    }
                                    else {
                                        course.Name = phItem.Course;
                                        course.Ordinal = courseOrdinal;
                                        course.Department = courseDepartment;
                                        course.Type = StudentPopulationType.PH;
                                        dataContext.Course.Add(course);
                                        dataContext.SaveChanges();
                                        courseOrdinal++;
                                    }
                                }
                            }

                            
                            
                            
                            foreach (ImportData phItem in gept) {
                                if (phItem != null) {
                                    CourseDepartment courseDepartment = new CourseDepartment();
                                    if (dataContext.CourseDepartment.Any(e => e.Name == phItem.Department)) {
                                        courseDepartment = dataContext.CourseDepartment.FirstOrDefault(e => e.Name == phItem.Department);
                                    }
                                    else {
                                        courseDepartment.Name = phItem.Department;
                                        courseDepartment.Ordinal = departmentOrdinal;
                                        courseDepartment.Company = Company.PH;
                                        dataContext.CourseDepartment.Add(courseDepartment);
                                        dataContext.SaveChanges();
                                        departmentOrdinal++;
                                    }
                                    Course course = new Course();
                                    if (dataContext.Course.Any(e => e.Name == phItem.Course)) {
                                        continue;
                                    }
                                    else {
                                        course.Name = phItem.Course;
                                        course.Ordinal = courseOrdinal;
                                        course.Department = courseDepartment;
                                        course.Type = StudentPopulationType.PHM;
                                        dataContext.Course.Add(course);
                                        dataContext.SaveChanges();
                                        courseOrdinal++;
                                    }
                                }
                            }

                            foreach (ImportData phItem in ps) {
                                if (phItem != null) {
                                    CourseDepartment courseDepartment = new CourseDepartment();
                                    if (dataContext.CourseDepartment.Any(e => e.Name == phItem.Department)) {
                                        courseDepartment = dataContext.CourseDepartment.FirstOrDefault(e => e.Name == phItem.Department);
                                    }
                                    else {
                                        courseDepartment.Name = phItem.Department;
                                        courseDepartment.Ordinal = departmentOrdinal;
                                        courseDepartment.Company = Company.PH;
                                        dataContext.CourseDepartment.Add(courseDepartment);
                                        dataContext.SaveChanges();
                                        departmentOrdinal++;
                                    }
                                    Course course = new Course();
                                    if (dataContext.Course.Any(e => e.Name == phItem.Course)) {
                                        continue;
                                    }
                                    else {
                                        course.Name = phItem.Course;
                                        course.Ordinal = courseOrdinal;
                                        course.Department = courseDepartment;
                                        course.Type = StudentPopulationType.PS;
                                        dataContext.Course.Add(course);
                                        dataContext.SaveChanges();
                                        courseOrdinal++;
                                    }
                                }
                            }

                            foreach (ImportData phItem in psj) {
                                if (phItem != null) {
                                    CourseDepartment courseDepartment = new CourseDepartment();
                                    if (dataContext.CourseDepartment.Any(e => e.Name == phItem.Department)) {
                                        courseDepartment = dataContext.CourseDepartment.FirstOrDefault(e => e.Name == phItem.Department);
                                    }
                                    else {
                                        courseDepartment.Name = phItem.Department;
                                        courseDepartment.Ordinal = departmentOrdinal;
                                        courseDepartment.Company = Company.PH;
                                        dataContext.CourseDepartment.Add(courseDepartment);
                                        dataContext.SaveChanges();
                                        departmentOrdinal++;
                                    }
                                    Course course = new Course();
                                    if (dataContext.Course.Any(e => e.Name == phItem.Course)) {
                                        continue;
                                    }
                                    else {
                                        course.Name = phItem.Course;
                                        course.Ordinal = courseOrdinal;
                                        course.Department = courseDepartment;
                                        course.Type = StudentPopulationType.PS;
                                        dataContext.Course.Add(course);
                                        dataContext.SaveChanges();
                                        courseOrdinal++;
                                    }
                                }
                            }
                        }
                    }
                    catch (FrameworkException ex) {
                        Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                        throw new FrameworkException(ex.Message);
                    }
                    catch (Exception ex) {
                        Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                        throw new Exception("系統忙碌中，請稍後再試");
                    }
                    return Json(ResponseStatus.OK, 1);
                }
            }
            catch (FrameworkException fe) {
                return Json(ResponseStatus.OK, 1);
            }
            catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.OK, 1);
            }
        }

        [HttpGet("ImportMemberData")]
        public IActionResult ImportMemberData(string type) {
            try {
                using (FileStream file = new FileStream(@"C:\Leo\其他\Kuri\人數表\人數統計表網站帳密資料_20241218.xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new DataException("取得資料發生錯誤");
                    try {
                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet1 = workbook.GetSheetAt(0);
                        List<ImportData> member = new List<ImportData>();

                        var list = new List<ISheet>() { sheet1 };
                        var count = 0;
                        for (int k = 0; k < workbook.NumberOfSheets; k++) {
                            string[] input = new string[3];
                            var sheet = workbook.GetSheetAt(k); ;
                            for (int row = 0; row <= sheet.LastRowNum; row++) {
                                XSSFRow xlRow = sheet.GetRow(row) as XSSFRow; //取得每一列
                                ImportData newItem = new ImportData();
                                if (xlRow != null) {
                                    for (int col = 0; col < xlRow.LastCellNum; col++) {
                                        XSSFCell xlCell = xlRow.GetCell(col) as XSSFCell; //取得目前列的每個儲存格
                                        string value = string.Empty;
                                        //取得儲存格的值
                                        if (xlCell.CellType == CellType.Numeric) {
                                            if (DateUtil.IsCellDateFormatted(xlCell))
                                                value = xlCell.DateCellValue.ToString(); //日期格式
                                            else
                                                value = xlCell.NumericCellValue.ToString(); //數值格式
                                        }
                                        else if (xlCell.CellType == CellType.String) {
                                            value = xlCell.StringCellValue; //字串格式
                                        }
                                        if (col == 0) {
                                            newItem.SchoolName = value;
                                        }
                                        else if (col == 1) {
                                            newItem.Account = value;
                                        }
                                        else if (col == 2) {
                                            newItem.PassWord = value;
                                        }
                                    }
                                }
                                else {
                                    continue;
                                }

                                if (k == 0) {
                                    member.Add(newItem);
                                }
                            }
                        }
                        using (DataContext dataContext = new DataContext()) {
                            int departmentOrdinal = 0;
                            int courseOrdinal = 0;
                            foreach (ImportData memberItem in member) {
                                if (memberItem != null) {
                                    //確認分校資料
                                    School school = new School();
                                    if (dataContext.School.Any(e => e.Name == memberItem.SchoolName)) {
                                        school = dataContext.School.FirstOrDefault(e => e.Name == memberItem.SchoolName);
                                    }
                                    else {
                                        school.Name = memberItem.SchoolName;
                                        dataContext.School.Add(school);
                                        dataContext.SaveChanges();
                                    }
                                    //確認會員資料
                                    Member newMember = new Member();
                                    if (!dataContext.Member.Any(e => e.Account == memberItem.Account)) {
                                        newMember.Account = memberItem.Account;
                                        newMember.Password = memberItem.PassWord.ComputeHashStringWithSha().ToBase64();
                                        newMember.Status = System.Framework.Community.MemberStatus.Enabled;
                                        dataContext.Member.Add(newMember);
                                        dataContext.SaveChanges();
                                    }
                                    //增加分校所屬成員
                                    if(!dataContext.SchoolAssignment.Any(e => e.School.Id == school.Id && e.Member.Id == newMember.Id)) {
                                        SchoolAssignment newAss = new SchoolAssignment();
                                        newAss.School = school;
                                        newAss.Member = newMember;
                                        dataContext.SchoolAssignment.Add(newAss);
                                        dataContext.SaveChanges();
                                    }
                                    else {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    catch (FrameworkException ex) {
                        Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                        throw new FrameworkException(ex.Message);
                    }
                    catch (Exception ex) {
                        Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                        throw new Exception("系統忙碌中，請稍後再試");
                    }
                    return Json(ResponseStatus.OK, 1);
                }
            }
            catch (FrameworkException fe) {
                return Json(ResponseStatus.OK, 1);
            }
            catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.OK, 1);
            }
        }

        public class ImportData {
            public string Course { get; set; }
            public string Department { get; set; }

            public string SchoolName { get; set; }
            public string Account { get; set; }

            public string PassWord { get; set; }

        }

        [HttpGet("AddSchoolYear")]
        public IActionResult AddSchoolYear(string type) {
            try {
                DataContext dataContext = new DataContext();
                DateTime begintime = new DateTime(2025, 7, 5);
                DateTime endDateTime = new DateTime(2025, 12, 31);
                int week = 1;
                while (begintime < endDateTime) {
                    SchoolYear newSchoolYear = new SchoolYear();
                    newSchoolYear.Year = 114;
                    newSchoolYear.ADYear = begintime.Year;
                    newSchoolYear.Week = week;
                    newSchoolYear.WeekStartDate = new DateTime(begintime.GetFirstDayOfTheWeek().Year, begintime.GetFirstDayOfTheWeek().Month, begintime.GetFirstDayOfTheWeek().Day, 16, 0, 0);
                    newSchoolYear.WeekEndDate = new DateTime(begintime.Year, begintime.Month, begintime.Day, 18, 0, 0);
                    begintime = begintime.AddDays(7);
                    week++;
                    dataContext.SchoolYear.Add(newSchoolYear);
                    dataContext.SaveChanges();
                }
                return Json(ResponseStatus.OK, 1);
            }
            catch (FrameworkException fe) {
                return Json(ResponseStatus.OK, 1);
            }
            catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.OK, 1);
            }
        }

        #endregion

        #region

        [HttpGet("ExportDateTest")]
        public IActionResult ExportDateTest() {
            IWorkbook wb = new XSSFWorkbook();
            ISheet ws = wb.CreateSheet("Course");
            using (DataContext dataContext = new DataContext()) {
                List<StudentPopulation> populationList = new List<StudentPopulation>();
                List<Course> course = new List<Course>();// dataContext.Course.Include("Department").OrderBy(e => e.Ordinal).ToList();
                course = dataContext.Course.Include("Department").OrderBy(e => e.Ordinal).ToList();
                ws.CreateRow(0);//第一行為欄位名稱
                int cellNo = 0;
                //設定欄位樣式
                XSSFCellStyle courseCellStyle = (XSSFCellStyle)wb.CreateCellStyle();
                courseCellStyle.WrapText = true; //自動換行設定
                IRow departmentRow = ws.CreateRow(0);
                IRow courseRow = ws.CreateRow(1);
                int currentDep = 0;
                int mergedIndex = 0;
                foreach (Course cItem in course) {
                    if (currentDep != cItem.Department.Id) {
                        currentDep = cItem.Department.Id;
                        ICell departmentCell = departmentRow.CreateCell(cellNo);
                        departmentCell.SetCellValue(cItem.Department.Name);
                        try {
                            if (mergedIndex != cellNo) {
                                ws.AddMergedRegion(new CellRangeAddress(0, 0, mergedIndex, cellNo - 1));
                                mergedIndex = cellNo;
                            }
                        }
                        catch (Exception ex) {

                        }
                    }
                    ICell courseCell = courseRow.CreateCell(cellNo);
                    courseCell.SetCellValue(cItem.Name);
                    ws.SetColumnWidth(cellNo, 4 * 256);//設定欄寬
                    courseCell.CellStyle = courseCellStyle;
                    cellNo++;
                }
            }
            var path = Path.Combine($"{System.Framework.Environment.Directory.WebRootPath}", "files", "reports", "NewReport.xlsx");
            //var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            wb.Write(fs);
            return Json(ResponseStatus.OK, 1);
        }

        #endregion
    }
}