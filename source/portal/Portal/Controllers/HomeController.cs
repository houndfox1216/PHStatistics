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
using System.Framework.Application;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;



namespace PHStatistics.Portal.Controllers {
    [Route("/")]
    public class HomeController() : MvcController<PortalUser, Model, Culture>("System") {
        [Route("/")]
        [Route("Index")]
        //[Authorize(typeof(PortalUser))]
        public IActionResult Index() {
            Logger.LogInformation($"進入首頁 IsGuest:{User.IsGuest()} IsAuthenticated:{User.Identity.IsAuthenticated}");
            if (User.IsGuest()) {
                // 讀取 Session
                string account = string.Empty;
                string login = string.Empty;
                //try {
                //    account = HttpContext.Session.GetString("Account") ?? "";
                //    login = HttpContext.Session.GetString("UserLogin") ?? "0";
                //}
                //catch (Exception ex) {
                //    try {
                //        account = HttpContext.Request.Cookies["Account"] ?? "";
                //        login = HttpContext.Request.Cookies["UserLogin"] ?? "0";
                //    }
                //    catch {
                //        account = string.Empty;
                //        login = string.Empty;
                //    }
                //}
                Logger.LogInformation($"進入首頁 讀取 Session account:{account} login:{login}");
                if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(login)) {
                    return Redirect("/Member/Login");
                }
            }
            Logger.LogInformation("進入首頁");
            DataContext dataContext = new DataContext();
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).FirstOrDefault();
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
        [HttpGet("ImportExcelData")]
        public IActionResult ImportExcelData() {
            string schooleName = string.Empty;
            try {
                Logger.LogInformation($"進入ImportExcelData");
                using (DataContext dataContext = new DataContext()) {
                    //取得資料夾資料
                    string sourceDirectory = "C:\\Leo\\其他\\Kuri\\人數表\\20250519\\0519匯入_TEST\\";
                    //C:\Leo\其他\Kuri\人數表\20250515\0519匯入_TEST
                    //"F:\\WEB\\PCM_FTP\\NewPAS\\匯入\\"                   
                    var xlsxFiles = Directory.EnumerateFiles(sourceDirectory, "*.xlsx");
                    Logger.LogInformation($"讀取路徑 {sourceDirectory}");
                    foreach (string currentFile in xlsxFiles) {
                        Logger.LogInformation($"讀取檔案 {currentFile}");
                        try {
                            schooleName = currentFile.Substring(sourceDirectory.Length);
                            schooleName = schooleName.Replace(".xlsx", "");

                            DataTable dt = new DataTable();
                            XSSFWorkbook xssfworkbook;

                            School newSchool = new School();
                            if (dataContext.School.Any(e => e.Name == schooleName)) {
                                newSchool = dataContext.School.FirstOrDefault(e => e.Name == schooleName);
                            }
                            else {
                                newSchool = new School();
                                newSchool.Name = schooleName;
                                dataContext.School.Add(newSchool);
                                dataContext.SaveChanges();
                            }
                            string year = "2025";
                            int yearInt = 114;
                            using (FileStream file = new FileStream(currentFile, FileMode.Open, FileAccess.Read)) {
                                xssfworkbook = new XSSFWorkbook(file);
                            }
                            ISheet sheet = xssfworkbook.GetSheetAt(0);
                            IRow headerRow = sheet.GetRow(0);
                            int colCount = headerRow.Cells.Count();
                            int rowCount = sheet.LastRowNum;
                            string countryName = string.Empty;
                            string brandName = string.Empty;
                            decimal pics = 0;
                            string sizeStr = string.Empty;
                            string exNo = string.Empty;
                            List<ImportRow> RowData = new List<ImportRow>();

                            #region 解析表頭
                            for (int rNo = 0; rNo <= 3; rNo++) {
                                try {
                                    // foreach (int cNo = 1; cNo <= colCount; cNo++ )
                                    string colSrt = string.Empty;
                                    for (int cNo = 3; cNo <= colCount; cNo++) {
                                        ImportRow newRow = new ImportRow();
                                        try {
                                            string cs = sheet.GetRow(rNo).Cells[cNo].ToString();
                                        }
                                        catch {
                                            continue;
                                        }
                                        newRow.RowNo = rNo;
                                        newRow.CellsNo = cNo;
                                        newRow.CellsContent = sheet.GetRow(rNo).Cells[cNo].ToString().Trim().Replace("　", "").Replace(" ", "");
                                        if (!string.IsNullOrEmpty(newRow.CellsContent)) {
                                            RowData.Add(newRow);
                                        }
                                    }
                                }
                                catch (Exception ex) {
                                    continue;
                                }
                            }
                            #endregion
                            #region 新增班系課程資料
                            //List<ImportRow> ss = RowData.Where(p => p.RowNo == 1).ToList();
                            for (int i = 0; i < 2; i++) {
                                //班系
                                if (i == 0) {
                                    foreach (ImportRow rItem in RowData.Where(p => p.RowNo == i).ToList()) {
                                        //if (rItem.CellsContent.Contains("合計") || rItem.CellsContent.Contains("總計") || rItem.CellsContent.Contains("分析") || rItem.CellsContent.Contains("總人數")) {
                                        //    continue;
                                        //}
                                        if (dataContext.CourseDepartment.Any(p => p.Name == rItem.CellsContent)) {
                                            continue;
                                        }
                                        else {
                                            dataContext.CourseDepartment.Add(new CourseDepartment { DataMode = DataMode.Normal, Name = rItem.CellsContent });
                                            dataContext.SaveChanges();
                                            if (rItem.CellsContent.Equals("總人數") || rItem.CellsContent.Equals("Elite/英檢/sat班系") || rItem.CellsContent.Equals("本週總詢問(填單)人數")) {
                                                CourseDepartment cDep = dataContext.CourseDepartment.FirstOrDefault(e => e.Name == rItem.CellsContent);
                                                if (dataContext.Course.Any(p => p.Name == cDep.Name)) {
                                                    continue;
                                                }
                                                else {
                                                    dataContext.Course.Add(new Course { DataMode = DataMode.Normal, Name = cDep.Name, Department = cDep });
                                                    dataContext.SaveChanges();
                                                }
                                            }
                                        }
                                    }
                                }
                                //課程
                                if (i == 1) {
                                    foreach (ImportRow rItem in RowData.Where(p => p.RowNo == i).ToList()) {
                                        ImportRow cDepRow = RowData.FirstOrDefault(p => p.RowNo == 0 && p.CellsNo == rItem.CellsNo);
                                        if (!cDepRow.HasValue()) {
                                            cDepRow = RowData.Where(p => p.RowNo == 0 && p.CellsNo <= rItem.CellsNo).OrderByDescending(p => p.CellsNo).FirstOrDefault();
                                        }
                                        if (cDepRow.HasValue()) {
                                            //if (cDepRow.CellsContent.Contains("合計") || cDepRow.CellsContent.Contains("總計") || cDepRow.CellsContent.Contains("分析") || cDepRow.CellsContent.Contains("總人數")) {
                                            //    continue;
                                            //}
                                        }
                                        else {
                                            continue;
                                        }
                                        string courseDep = cDepRow.CellsContent;
                                        CourseDepartment cDep = dataContext.CourseDepartment.FirstOrDefault(p => p.Name == courseDep);
                                        if (dataContext.Course.Any(p => p.Name == rItem.CellsContent)) {
                                            continue;
                                        }
                                        else {
                                            string courseTitle = rItem.CellsContent;

                                            int nextCoueseCNo = 0;
                                            if (RowData.Any(p => p.RowNo == 1 && p.CellsNo > rItem.CellsNo)) {
                                                nextCoueseCNo = RowData.FirstOrDefault(p => p.RowNo == 1 && p.CellsNo > rItem.CellsNo).CellsNo;
                                            }
                                            else {
                                                nextCoueseCNo = rItem.CellsNo + 1;
                                            }
                                            if (RowData.Any(p => p.RowNo == 2 && p.CellsNo >= rItem.CellsNo && p.CellsNo < nextCoueseCNo)) {
                                                foreach (ImportRow cItem in RowData.Where(p => p.RowNo == 2 && p.CellsNo >= rItem.CellsNo && p.CellsNo < nextCoueseCNo).ToList()) {
                                                    if (!dataContext.Course.Any(p => p.Name.Equals(courseTitle + cItem.CellsContent) && p.Department.Id == cDep.Id)) {
                                                        dataContext.Course.Add(new Course { DataMode = DataMode.Normal, Name = courseTitle + cItem.CellsContent, Department = cDep });
                                                        dataContext.SaveChanges();
                                                    }
                                                }
                                            }
                                            else {
                                                dataContext.Course.Add(new Course { DataMode = DataMode.Normal, Name = courseTitle, Department = cDep });
                                                dataContext.SaveChanges();
                                            }
                                        }
                                    }
                                }
                                if (i == 3) {

                                }
                            }

                            #endregion

                            //讀取人數資料
                            //只取
                            /*
                             * 本週英語文總人數
                             * 本週英語文新生
                             * 本週英語文流失
                             * 本週國語文總人數
                             * 本週國語文新生人數
                             * 本週國語文流失人數	
                             * 本週總詢問(填單)人數
                             * 總人數
                             */
                            IRow baseRow = sheet.GetRow(4);
                            int dataColCount = headerRow.Cells.Count();
                            int week = 0;
                            bool isNew = true;
                            for (int drNo = 4; drNo < rowCount; drNo++) {
                                try {
                                    //取得週別
                                    if (sheet.GetRow(drNo).Cells[0].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[0].ToString())) {
                                        try {
                                            week = int.Parse(sheet.GetRow(drNo).Cells[0].ToString());
                                        }
                                        catch {
                                            continue;
                                        }

                                    }
                                    else {

                                    }
                                    //取得班別
                                    ClassType cType = new ClassType();
                                    if (sheet.GetRow(drNo).Cells[2].ToString().Equals("團")) {
                                        cType = ClassType.Group;
                                    }
                                    else if (sheet.GetRow(drNo).Cells[2].ToString().Equals("小")) {
                                        cType = ClassType.SubGroup;
                                    }
                                    else if (sheet.GetRow(drNo).Cells[2].ToString().Equals("三")) {
                                        cType = ClassType.Personal;
                                    }
                                    string classType = sheet.GetRow(drNo).Cells[2].ToString();
                                    int classNo = 1;
                                    //判斷班系課程
                                    //取得班系
                                    int chId = 0;
                                    string cdStr = string.Empty;
                                    string cStr = string.Empty;
                                    string c2Str = string.Empty;
                                    Course course = new Course();
                                    CourseDepartment courseDep = new CourseDepartment();
                                    SchoolYear schoolYear = dataContext.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == week);
                                    if (schoolYear == null) {
                                        continue;
                                    }
                                    StudentPopulation studentPopulation = new StudentPopulation();
                                    if (dataContext.StudentPopulation.Any(e => e.Year == yearInt && e.Week == week && e.School.Id == newSchool.Id)) {
                                        studentPopulation = dataContext.StudentPopulation.Any(e => e.Year == yearInt && e.Week == week && e.School.Id == newSchool.Id)
                                                                         ? dataContext.StudentPopulation.FirstOrDefault(e => e.Year == yearInt && e.Week == week && e.School.Id == newSchool.Id)
                                                                         : new StudentPopulation() { Year = yearInt, Week = week, SchoolId = newSchool.Id, WeekDate = schoolYear.WeekStartDate };

                                    }
                                    else {
                                        studentPopulation = new StudentPopulation() { Year = yearInt, Week = week, SchoolId = newSchool.Id, WeekDate = schoolYear.WeekStartDate, Name = $"{schooleName}分校人數統計表{yearInt}學年度第{week}週" };
                                        studentPopulation.Items = new List<StudentPopulationItem>();
                                        dataContext.StudentPopulation.Add(studentPopulation);
                                        dataContext.SaveChanges();
                                    }
                                    cdStr = string.Empty;
                                    cStr = string.Empty;
                                    c2Str = string.Empty;
                                    for (int c = 3; c < dataColCount; c++) {

                                        Class newClass = new Class();
                                        Course checkCourse = new Course();
                                        int count = 0;
                                        //讀取班系課程
                                        if (sheet.GetRow(1).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(1).Cells[c].ToString())) {
                                            if (!string.IsNullOrEmpty(sheet.GetRow(1).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", ""))) {
                                                cdStr = sheet.GetRow(1).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", "");
                                            }

                                        }
                                        if (sheet.GetRow(2).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(2).Cells[c].ToString())) {
                                            if (!string.IsNullOrEmpty(sheet.GetRow(2).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", ""))) {
                                                cStr = sheet.GetRow(2).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", "");
                                            }

                                        }
                                        if (sheet.GetRow(3).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(3).Cells[c].ToString())) {
                                            if (!string.IsNullOrEmpty(sheet.GetRow(3).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", ""))) {
                                                c2Str = sheet.GetRow(3).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", "");
                                            }
                                        }
                                        if (cdStr.Equals("國語文")) {
                                            chId = c;
                                        }
                                        //檢查該格人數為零不進行處理
                                        if (sheet.GetRow(drNo).Cells[c].CellType != CellType.Formula) {
                                            if (sheet.GetRow(drNo).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[c].ToString())) {
                                                try {
                                                    count = int.Parse(sheet.GetRow(drNo).Cells[c].NumericCellValue.ToString());
                                                }
                                                catch {
                                                    continue;
                                                }
                                            }
                                            else {
                                                continue;
                                            }
                                        }
                                        else {
                                            try {
                                                sheet.GetRow(drNo).Cells[c].SetCellType(CellType.Numeric);
                                                count = int.Parse(sheet.GetRow(drNo).Cells[c].NumericCellValue.ToString());
                                            }
                                            catch {
                                                continue;
                                            }
                                        }
                                        if (count == 0)
                                            continue;

                                        try {
                                            int checkCount = 0;
                                            if (cdStr.Equals("兒美系列")) {
                                                cdStr = "國小班";
                                            }
                                            if (cdStr.Equals("總人數")) {
                                                cdStr = "總人數";
                                                cStr = "總人數";
                                            }
                                            if (cStr.Equals("人數合計")) {
                                                cdStr = "國小班";
                                                cStr = "國小人數合計";
                                            }
                                            else if (cStr.Equals("本週英語文總人數")) {
                                                cdStr = "英文合計";
                                                cStr = "本週英語文總人數";
                                            }
                                            else if (cStr.Equals("上週英語文總人數")) {
                                                cdStr = "英文合計";
                                                cStr = "上週英語文總人數";
                                            }
                                            else if (cStr.Equals("本週英語文新生")) {
                                                cdStr = "英文分析";
                                                cStr = "本週英語文新生";
                                            }
                                            else if (cStr.Equals("本週英語文流失")) {
                                                cdStr = "英文分析";
                                                cStr = "本週英語文流失";
                                            }
                                            else if (cStr.Equals("本週國語文總人數")) {
                                                cdStr = "國文總計";
                                                cStr = "本週國語文總人數";
                                            }
                                            else if (cStr.Equals("上週國語文總人數")) {
                                                cdStr = "國文總計";
                                                cStr = "上週國語文總人數";
                                            }
                                            else if (cStr.Equals("本週國語文新生人數")) {
                                                cdStr = "國文分析";
                                                cStr = "本週國語文新生人數";
                                            }
                                            else if (cStr.Equals("本週國語文流失人數")) {
                                                cdStr = "國文分析";
                                                cStr = "本週國語文流失人數";
                                            }
                                            else if (cStr.Equals("與上週相比")) {
                                                if (cdStr.Equals("總計")) {
                                                    cdStr = "國文總計";
                                                    cStr = "與上週相比";
                                                }
                                            }
                                            else if (cStr.Equals("總班數")) {
                                                if (chId != 0 && c >= chId) {
                                                    cdStr = "國語文";
                                                }
                                                else {
                                                    cdStr = "高中課程";
                                                }

                                            }
                                            else if (cStr.Equals("去年同期 / 比") || cStr.Equals("去年同期/比")) {
                                                cdStr = "英文分析";
                                                cStr = "去年同期/比";
                                            }

                                            if (cdStr.Contains("Elite/英檢/sat")) {
                                                cdStr = "高中課程";
                                                cStr = "Elite/英檢/sat";
                                            }

                                            if (cdStr.Equals("個別指導")) {
                                                if (chId != 0 && c > chId) {
                                                    cdStr = "國語個別指導";
                                                }
                                                else {
                                                    cdStr = "英文個別指導";
                                                }
                                            }

                                            if (cStr.Equals("(EM1)高一&高二")) {
                                                cdStr = "英文個別指導";
                                            }
                                            if (cStr.Equals("總班數")) {
                                                if (chId != 0 && c >= chId) {
                                                    cdStr = "國語文";
                                                }
                                                else {
                                                    cdStr = "高中課程";
                                                }

                                            }
                                            if (cStr.Equals("總人數")) {
                                                cdStr = "總人數";
                                                cStr = "總人數";
                                            }

                                            courseDep = dataContext.CourseDepartment.FirstOrDefault(p => p.Name == cdStr);
                                            if (courseDep.Name.Equals("總人數") || courseDep.Name.Equals(@"Elite/英檢/sat班系") || courseDep.Name.Equals(@"本週總詢問(填單)人數")) {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == courseDep.Name);
                                            }
                                            else {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == cStr);
                                            }
                                            if (!checkCourse.HasValue()) {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == cStr + c2Str);
                                            }
                                            if (cStr.Equals("p1-3")) {
                                                if (c2Str.Equals("1")) {
                                                    checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == "初階");
                                                }
                                                else if (c2Str.Equals("2")) {
                                                    checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == "先階");
                                                }
                                                else if (c2Str.Equals("3")) {
                                                    checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == "中階");
                                                }
                                            }
                                            if (cStr.Equals("P4-進階")) {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == "進階");
                                            }
                                            else if (c2Str.Equals("P5-高階")) {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == "高階");
                                            }
                                            else if (c2Str.Equals("P6-優階")) {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == "優階");
                                            }
                                            if (!checkCourse.HasValue()) {
                                                continue;
                                            }
                                            if (course.Id != checkCourse.Id) {
                                                course = checkCourse;
                                                classNo = 1;
                                            }
                                            else {
                                                classNo++;
                                            }

                                            if (courseDep.HasValue() && course.HasValue()) {
                                                string className = string.Format("{0}_{2}_{1}", course.Name, classNo.ToString("00"), classType);
                                                if (dataContext.Class.Any(p => p.Course.DepartmentId == courseDep.Id && p.Course.Id == course.Id && p.Name == className && p.Type == cType && p.School.Id == newSchool.Id)) {
                                                    newClass = dataContext.Class.FirstOrDefault(p => p.Course.Id == course.Id && p.Name == className && p.Type == cType && p.School.Id == newSchool.Id);
                                                }
                                                else {
                                                    newClass.Course = course;
                                                    newClass.Name = className;
                                                    newClass.School = newSchool;
                                                    newClass.Type = cType;
                                                    dataContext.Class.Add(newClass);
                                                    dataContext.SaveChanges();
                                                }
                                            }
                                        }
                                        catch (Exception ex) {
                                            string ds = ex.Message;
                                        }
                                        StudentPopulationItem newItem = new StudentPopulationItem();
                                        newItem.Number = count;

                                        if (dataContext.StudentPopulationItem.Any(e => e.StudentPopulation.Id == studentPopulation.Id && e.Class.Id == newClass.Id)) {
                                            StudentPopulationItem item = dataContext.StudentPopulationItem.FirstOrDefault(e => e.StudentPopulation.Id == studentPopulation.Id && e.Class.Id == newClass.Id);
                                            if (item != null) {
                                                item.Number = newItem.Number;
                                            }
                                        }
                                        else {
                                            studentPopulation.Items.Add(new StudentPopulationItem() { ClassId = newClass.Id, Number = count });
                                        }
                                        dataContext.SaveChanges();
                                    }

                                }
                                catch (Exception ex) {
                                    Logger.LogError($"匯入{schooleName}資料失敗 ex {ex.Message}  ex.InnerException {ex.InnerException?.Message}", ex.Message);
                                    continue;
                                    //string f = ex.Message;
                                }
                            }
                        }
                        catch (Exception ex) {
                            Logger.LogInformation($"解析 {schooleName} 檔案失敗 ex{ex.Message} ex.InnerException {ex.InnerException?.Message}");
                            string f = ex.Message;
                            continue;
                        }
                    }
                }
                return Json(ResponseStatus.OK);
            }
            catch (FrameworkException fe) {
                Logger.LogInformation($"讀取檔案 {schooleName} 失敗 fe{fe.Message} fe.InnerException {fe.InnerException?.Message}");
                return Json(ResponseStatus.InternalServerError);
            }
        }

        [HttpGet("ImportCourseData")]
        public IActionResult ImportCourseData() {
            try {
                using (DataContext dataContext = new DataContext()) {
                    XSSFWorkbook xssfworkbook;
                    using (FileStream file = new FileStream("C:\\Leo\\其他\\Kuri\\人數表\\分校班系課程開班資料_20250425.xlsx", FileMode.Open, FileAccess.Read)) {
                        xssfworkbook = new XSSFWorkbook(file);
                    }
                    ISheet sheet = xssfworkbook.GetSheetAt(0);
                    IRow headerRow = sheet.GetRow(0);
                    int colCount = headerRow.Cells.Count();
                    int rowCount = sheet.LastRowNum;
                    string countryName = string.Empty;
                    string brandName = string.Empty;
                    decimal pics = 0;
                    string sizeStr = string.Empty;
                    string exNo = string.Empty;
                    for (int drNo = 1; drNo < rowCount; drNo++) {
                        SchoolClass schoolClass = new SchoolClass();
                        if (sheet.GetRow(drNo).Cells[0].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[0].ToString())) {
                            if (!string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[0].ToString().Trim().Replace("　", "").Replace(" ", ""))) {
                                schoolClass.CourseDepartment = sheet.GetRow(drNo).Cells[0].ToString().Trim().Replace("　", "").Replace(" ", "");
                            }
                        }

                        if (sheet.GetRow(drNo).Cells[1].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[1].ToString())) {
                            if (!string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[1].ToString().Trim().Replace("　", "").Replace(" ", ""))) {
                                schoolClass.Course = sheet.GetRow(drNo).Cells[1].ToString().Trim().Replace("　", "").Replace(" ", "");
                            }
                        }

                        if (sheet.GetRow(drNo).Cells[2].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[2].ToString())) {
                            if (!string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[2].ToString().Trim().Replace("　", "").Replace(" ", ""))) {
                                schoolClass.Class = sheet.GetRow(drNo).Cells[2].ToString().Trim().Replace("　", "").Replace(" ", "");
                            }
                        }

                        if (sheet.GetRow(drNo).Cells[3].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[3].ToString())) {
                            if (!string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[3].ToString().Trim().Replace("　", "").Replace(" ", ""))) {
                                schoolClass.School = sheet.GetRow(drNo).Cells[3].ToString().Trim().Replace("　", "").Replace(" ", "");
                            }
                        }
                        if (!dataContext.SchoolClass.Any(e => e.CourseDepartment == schoolClass.CourseDepartment && e.Course == schoolClass.Course && e.Class == schoolClass.Class && e.School == schoolClass.School)) {
                            if (string.IsNullOrEmpty(schoolClass.CourseDepartment) ||
                                string.IsNullOrEmpty(schoolClass.Course) ||
                                string.IsNullOrEmpty(schoolClass.Class) ||
                                string.IsNullOrEmpty(schoolClass.School)) {
                                continue;
                            }
                            dataContext.SchoolClass.Add(schoolClass);
                        }
                    }
                    dataContext.SaveChanges();
                }
                return Json(ResponseStatus.OK);
            }
            catch (Exception ex) {
                return Json(ResponseStatus.InternalServerError);
            }
        }
        [HttpGet("ImportPIData")]
        public IActionResult ImportPIData(string type) {
            try {
                using (FileStream file = new FileStream(@"C:\Leo\其他\Kuri\人數表\班系課程整理20240729-修改20240902-匯入.xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new System.Data.DataException("取得資料發生錯誤");
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
                                    if (newItem.Department != null && !string.IsNullOrEmpty(newItem.Department) && newItem.Course != null && !string.IsNullOrEmpty(newItem.Course)) {
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
                                        course.Type = StudentPopulationType.GEPT;
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
                                        course.Type = StudentPopulationType.PSJ;
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

        [HttpGet("ImportPIData2")]
        public IActionResult ImportPIData2() {
            try {
                using (FileStream file = new FileStream(@"C:\Leo\其他\Kuri\人數表\班系課程整理20240729-修改20240902-GEPT.xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new System.Data.DataException("取得資料發生錯誤");
                    try {
                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet1 = workbook.GetSheetAt(0);
                        List<ImportData> psj = new List<ImportData>();

                        var list = new List<ISheet>() { sheet1 };
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
                                    if (newItem.Department != null && !string.IsNullOrEmpty(newItem.Department) && newItem.Course != null && !string.IsNullOrEmpty(newItem.Course)) {
                                        psj.Add(newItem);
                                    }
                                }   
                            }
                        }
                        using (DataContext dataContext = new DataContext()) {
                            int departmentOrdinal = 0;
                            int courseOrdinal = 0;                         
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
                                        course.Name = phItem.Course.ToUpper();
                                        course.Ordinal = courseOrdinal;
                                        course.Department = courseDepartment;
                                        course.Type = StudentPopulationType.GEPT;
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
                        throw new System.Data.DataException("取得資料發生錯誤");
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
                                    else {
                                        newMember = dataContext.Member.FirstOrDefault(e => e.Account == memberItem.Account);
                                        //if (newMember.Password != memberItem.PassWord.ComputeHashStringWithSha().ToBase64()) {
                                        //    newMember.Password = memberItem.PassWord.ComputeHashStringWithSha().ToBase64();
                                        //    dataContext.SaveChanges();
                                        //}
                                    }
                                    //增加分校所屬成員
                                    if (!dataContext.SchoolAssignment.Any(e => e.School.Id == school.Id && e.Member.Id == newMember.Id)) {
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

        [HttpGet("CreateAdmin")]
        public IActionResult CreateAdmin(string type) {
            try {
                using (DataContext dataContext = new DataContext()) {
                    //確認會員資料
                    Member newMember = new Member();
                    newMember.Account = "sysadmin";
                    newMember.Password = "53906052".ComputeHashStringWithSha().ToBase64();
                    newMember.Status = System.Framework.Community.MemberStatus.Enabled;
                    dataContext.Member.Add(newMember);
                    dataContext.SaveChanges();

                    //增加分校所屬成員
                    foreach (School school in dataContext.School.ToList()) {
                        if (!dataContext.SchoolAssignment.Any(e => e.School.Id == school.Id && e.Member.Id == newMember.Id)) {
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
                return Json(ResponseStatus.OK, 1);
            }
            catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.OK, 1);
            }
        }

        [HttpGet("SetMemberSchool")]
        public IActionResult SetMemberSchool(string memberAcc) {
            try {
                using (DataContext dataContext = new DataContext()) {
                    //確認會員資料
                    Member member = dataContext.Member.FirstOrDefault(e => e.Account == memberAcc);
                    if (member != null) {
                        //增加分校所屬成員
                        foreach (School school in dataContext.School.ToList()) {
                            if (!dataContext.SchoolAssignment.Any(e => e.School.Id == school.Id && e.Member.Id == member.Id)) {
                                SchoolAssignment newAss = new SchoolAssignment();
                                newAss.School = school;
                                newAss.Member = member;
                                dataContext.SchoolAssignment.Add(newAss);
                                dataContext.SaveChanges();
                            }
                            else {
                                continue;
                            }
                        }
                    }
                }
                return Json(ResponseStatus.OK, 1);
            }
            catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.OK, 1);
            }
        }

        [HttpGet("ImportSchoolYearData")]
        public IActionResult ImportSchoolYearData() {
            try {
                using (FileStream file = new FileStream(@"C:\Leo\其他\Kuri\人數表\2025年周次_匯入.xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new System.Data.DataException("取得資料發生錯誤");
                    try {
                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet1 = workbook.GetSheetAt(0);
                        List<ImportData> member = new List<ImportData>();

                        var list = new List<ISheet>() { sheet1 };
                        var count = 0;
                        string[] input = new string[3];
                        var sheet = workbook.GetSheetAt(0); ;
                        for (int row = 1; row <= sheet.LastRowNum; row++) {
                            XSSFRow xlRow = sheet.GetRow(row) as XSSFRow; //取得每一列
                            if (xlRow != null) {
                                /* if (xlRow.Cells[1] != null && xlRow.Cells[2] != null && xlRow.Cells[3] != null
                                    && xlRow.Cells[4] != null && xlRow.Cells[5] != null && xlRow.Cells[6] != null)
                                 * 
                                 */
                                if (xlRow.Cells[1] != null && xlRow.Cells[2] != null
                                    && xlRow.Cells[4] != null ) {
                                    try {
                                        DateTime weeksDate = xlRow.Cells[4].DateCellValue.Value;
                                        DateTime weekeDate = xlRow.Cells[4].DateCellValue.Value;
                                        if (xlRow.Cells[3].DateCellValue.Value == xlRow.Cells[4].DateCellValue.Value) {
                                            weeksDate = weeksDate.GetFirstDayOfTheWeek();
                                        }
                                        DateTime importEndDate = weekeDate.AddDays(2);
                                        if (xlRow.Cells[5].DateCellValue.Value == xlRow.Cells[4].DateCellValue.Value) {
                                            importEndDate = importEndDate.GetEndTimeOfTheDay();
                                        }
                                        using (DataContext dataContext = new DataContext()) {
                                            SchoolYear schoolYear = new SchoolYear();
                                            schoolYear.Year = int.Parse(xlRow.Cells[1].ToString());
                                            schoolYear.Week = int.Parse(xlRow.Cells[2].ToString());
                                            schoolYear.WeekStartDate = weeksDate;
                                            schoolYear.WeekEndDate = weekeDate;
                                            schoolYear.ImportEndDate = importEndDate;
                                            schoolYear.ADYear = int.Parse(xlRow.Cells[6].ToString());
                                            dataContext.SchoolYear.Add(schoolYear);
                                            dataContext.SaveChanges();
                                        }
                                    }
                                    catch {
                                        continue;
                                    }

                                }

                            }
                            else {
                                continue;
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

        public class ImportRow {

            public ImportRow() { }

            /// <summary>
            /// 行數
            /// </summary>
            [Display(Name = "行數")]
            public int RowNo { get; set; }

            /// <summary>
            /// 欄數
            /// </summary>
            [Display(Name = "欄數")]
            public int CellsNo { get; set; }

            /// <summary>
            /// 內容
            /// </summary>
            [Display(Name = "內容")]
            public string CellsContent { get; set; }
        }

        public class ImportCourse {
            /// <summary>
            /// 班系
            /// </summary>
            [Display(Name = "班系")]
            public string CourseDepartment { get; set; }

            /// <summary>
            /// 課程
            /// </summary>
            [Display(Name = "課程")]
            public string Course { get; set; }

            /// <summary>
            /// 班級
            /// </summary>
            [Display(Name = "班級")]
            public string Class { get; set; }

            /// <summary>
            /// 分校
            /// </summary>
            [Display(Name = "分校")]
            public string School { get; set; }
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

        #region 資料匯出

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