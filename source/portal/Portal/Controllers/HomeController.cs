using Castle.Core.Resource;
using FluentFTP.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Nest;
using NPOI;
using NPOI.HSSF.Record;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;
using PHStatistics.Community;
using PHStatistics.Content;
using PHStatistics.Portal.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Framework;
using System.Framework.Application;
using System.Framework.Data;
using System.Framework.EntityFrameworkCore;
using System.Framework.Globalization;
using System.Framework.Logging;
using System.Framework.Security;
using System.Framework.Web;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Environment = System.Framework.Environment;



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
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).OrderBy(e => e.Id).FirstOrDefault();
            ViewBag.CanEdit = schoolYear != null;
            ViewBag.IsAdmin = User.HasPermission(SystemPermission.Administrator);
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
                using (FileStream file = new FileStream(@"C:\Leo\其他\Kuri\人數表\班系課程整理20250926_調整.xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new System.Data.DataException("取得資料發生錯誤");
                    try {
                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet1 = workbook.GetSheetAt(0);
                        ISheet sheet2 = workbook.GetSheetAt(1);
                        ISheet sheet3 = workbook.GetSheetAt(2);
                        ISheet sheet4 = workbook.GetSheetAt(3);
                        ISheet sheet5 = workbook.GetSheetAt(4);
                        List<ImportData> ph = new List<ImportData>();
                        List<ImportData> gept = new List<ImportData>();
                        List<ImportData> ps = new List<ImportData>();
                        List<ImportData> psj = new List<ImportData>();
                        List<ImportData> aft = new List<ImportData>();

                        var list = new List<ISheet>() { sheet1, sheet2, sheet3, sheet4 };
                        var count = 0;
                        for (int k = 0; k < workbook.NumberOfSheets; k++) {
                            string[] input = new string[2];
                            var sheet = workbook.GetSheetAt(k); ;
                            string comName = sheet.SheetName;
                            for (int row = 1; row <= sheet.LastRowNum; row++) {
                                XSSFRow xlRow = sheet.GetRow(row) as XSSFRow; //取得每一列
                                ImportData newItem = new ImportData();
                                if (xlRow != null) {
                                    for (int col = 0; col < xlRow.LastCellNum; col++) {
                                        XSSFCell xlCell = xlRow.GetCell(col) as XSSFCell; //取得目前列的每個儲存格
                                        string value = string.Empty;
                                        if (xlCell == null) {
                                            continue;
                                        }
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
                                        else if (col == 1) {
                                            newItem.Course = value;
                                        }
                                        else if (col == 2) {
                                            newItem.SchoolName = value;
                                        }
                                        else if (col == 3) {
                                            newItem.Account = value;
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
                                else if (k == 4) {
                                    if (newItem.Department != null && !string.IsNullOrEmpty(newItem.Department) && newItem.Course != null && !string.IsNullOrEmpty(newItem.Course)) {
                                        aft.Add(newItem);
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
                                        courseDepartment.Type = StudentPopulationType.PH;
                                        dataContext.CourseDepartment.Add(courseDepartment);
                                        dataContext.SaveChanges();
                                        departmentOrdinal++;
                                    }
                                    Course course = new Course();
                                    if (dataContext.Course.Any(e => e.Name == phItem.Course && e.Department.Name == courseDepartment.Name)) {
                                        continue;
                                    }
                                    else {
                                        course.Name = phItem.Course;
                                        course.Ordinal = courseOrdinal;
                                        course.Department = courseDepartment;
                                        course.Type = StudentPopulationType.PH;
                                        course.ClassType = string.IsNullOrEmpty(phItem.SchoolName) ? null : phItem.SchoolName;
                                        course.IsSum = string.IsNullOrEmpty(phItem.Account) ? false : (phItem.Account == "X" ? true : false);
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
                                        courseDepartment.Type = StudentPopulationType.GEPT;
                                        dataContext.CourseDepartment.Add(courseDepartment);
                                        dataContext.SaveChanges();
                                        departmentOrdinal++;
                                    }
                                    Course course = new Course();
                                    if (dataContext.Course.Any(e => e.Name == phItem.Course && e.Department.Name == courseDepartment.Name)) {
                                        continue;
                                    }
                                    else {
                                        course.Name = phItem.Course;
                                        course.Ordinal = courseOrdinal;
                                        course.Department = courseDepartment;
                                        course.Type = StudentPopulationType.GEPT;
                                        course.ClassType = string.IsNullOrEmpty(phItem.SchoolName) ? null : phItem.SchoolName;
                                        course.IsSum = string.IsNullOrEmpty(phItem.Account) ? false : (phItem.Account == "X" ? true : false);
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
                                        courseDepartment.Type = StudentPopulationType.PS;
                                        dataContext.CourseDepartment.Add(courseDepartment);
                                        dataContext.SaveChanges();
                                        departmentOrdinal++;
                                    }
                                    Course course = new Course();
                                    if (dataContext.Course.Any(e => e.Name == phItem.Course && e.Department.Name == courseDepartment.Name)) {
                                        continue;
                                    }
                                    else {
                                        course.Name = phItem.Course;
                                        course.Ordinal = courseOrdinal;
                                        course.Department = courseDepartment;
                                        course.Type = StudentPopulationType.PS;
                                        course.ClassType = string.IsNullOrEmpty(phItem.SchoolName) ? null : phItem.SchoolName;
                                        course.IsSum = string.IsNullOrEmpty(phItem.Account) ? false : (phItem.Account == "X" ? true : false);
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
                                        courseDepartment.Type = StudentPopulationType.PSJ;
                                        dataContext.CourseDepartment.Add(courseDepartment);
                                        dataContext.SaveChanges();
                                        departmentOrdinal++;
                                    }
                                    Course course = new Course();
                                    if (dataContext.Course.Any(e => e.Name == phItem.Course && e.Department.Name == courseDepartment.Name)) {
                                        continue;
                                    }
                                    else {
                                        course.Name = phItem.Course;
                                        course.Ordinal = courseOrdinal;
                                        course.Department = courseDepartment;
                                        course.Type = StudentPopulationType.PSJ;
                                        course.ClassType = string.IsNullOrEmpty(phItem.SchoolName) ? null : phItem.SchoolName;
                                        course.IsSum = string.IsNullOrEmpty(phItem.Account) ? false : (phItem.Account == "X" ? true : false);
                                        dataContext.Course.Add(course);
                                        dataContext.SaveChanges();
                                        courseOrdinal++;
                                    }
                                }
                            }
                            foreach (ImportData phItem in aft) {
                                if (phItem != null) {
                                    CourseDepartment courseDepartment = new CourseDepartment();
                                    if (dataContext.CourseDepartment.Any(e => e.Name == phItem.Department)) {
                                        courseDepartment = dataContext.CourseDepartment.FirstOrDefault(e => e.Name == phItem.Department);
                                    }
                                    else {
                                        courseDepartment.Name = phItem.Department;
                                        courseDepartment.Ordinal = departmentOrdinal;
                                        courseDepartment.Company = Company.PH;
                                        courseDepartment.Type = StudentPopulationType.AfterSchool;
                                        dataContext.CourseDepartment.Add(courseDepartment);
                                        dataContext.SaveChanges();
                                        departmentOrdinal++;
                                    }
                                    Course course = new Course();
                                    if (dataContext.Course.Any(e => e.Name == phItem.Course && e.Department.Name == courseDepartment.Name)) {
                                        continue;
                                    }
                                    else {
                                        course.Name = phItem.Course;
                                        course.Ordinal = courseOrdinal;
                                        course.Department = courseDepartment;
                                        course.Type = StudentPopulationType.AfterSchool;
                                        course.ClassType = string.IsNullOrEmpty(phItem.SchoolName) ? null : phItem.SchoolName;
                                        course.IsSum = string.IsNullOrEmpty(phItem.Account) ? false : (phItem.Account == "X" ? true : false);
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
                                    if (dataContext.Course.Any(e => e.Name == phItem.Course && e.Department.Name == courseDepartment.Name)) {
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
                using (FileStream file = new FileStream(@"C:\Users\hound\Downloads\分校帳密.xlsx", FileMode.Open, FileAccess.Read)) {
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
                                        if (newMember.Password != memberItem.PassWord.ComputeHashStringWithSha().ToBase64()) {
                                            newMember.Password = memberItem.PassWord.ComputeHashStringWithSha().ToBase64();
                                            dataContext.SaveChanges();
                                        }
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
                                    && xlRow.Cells[4] != null) {
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


        [HttpGet("ImportExcelData2")]
        public IActionResult ImportExcelData2() {
            try {
                DataContext dataContext = new DataContext();
                //載入Mapping
                //List<ImportMapping> mappings = LoadHeaderMapFromCsv(@"C:\Users\hound\Downloads\人數表系統\課程對照20250929.csv");

                using (
                    FileStream file = new FileStream(@"C:\Users\hound\Downloads\人數表系統\人數表系統\\2025 07(全國人數表第12週)_匯入.xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new System.Data.DataException("取得資料發生錯誤");
                    try {
                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet1 = workbook.GetSheetAt(0);
                        var list = new List<ISheet>() { sheet1 };
                        var count = 0;
                        string[] input = new string[3];
                        var sheet = workbook.GetSheetAt(0);
                        IRow headerRow = sheet.GetRow(1);
                        string year = "2025";
                        int yearInt = 114;
                        int colCount = headerRow.Cells.Count();
                        int rowCount = sheet.LastRowNum;
                        string countryName = string.Empty;
                        string brandName = string.Empty;
                        decimal pics = 0;
                        string sizeStr = string.Empty;
                        string exNo = string.Empty;
                        List<ImportCourse> courseData = dataContext.ImportCourse.ToList();
                        List<Mapping> mappings = new List<Mapping>();
                        for (int k = 0; k < workbook.NumberOfSheets; k++) {
                            try {
                                //第三個Sheet為英檢
                                var readSheet = workbook.GetSheetAt(k);
                                if (k < 2) {
                                    #region 解析表頭
                                    IRow row0 = readSheet.GetRow(0);
                                    IRow row1 = readSheet.GetRow(1);
                                    IRow row2 = readSheet.GetRow(2);
                                    IRow row3 = readSheet.GetRow(3);
                                    //IRow row4 = readSheet.GetRow(4);
                                    //IRow row5 = readSheet.GetRow(5);
                                    //IRow row6 = readSheet.GetRow(6);
                                    //IRow row7 = readSheet.GetRow(7);
                                    string schoolName = string.Empty;
                                    string classType = string.Empty;
                                    string depName = string.Empty;
                                    string courseName = string.Empty;
                                    string courseName2 = string.Empty;
                                    string itemCourseName = string.Empty;
                                    for (int rNo = 4; rNo <= readSheet.LastRowNum; rNo++) {
                                        IRow rowR = readSheet.GetRow(rNo);
                                        if (rowR == null) {
                                            continue;
                                        }
                                        else {
                                            for (int cNo = 0; cNo <= rowR.Cells.Count; cNo++) {
                                                try {
                                                    if (!string.IsNullOrEmpty(row1.Cells[cNo].ToString()) && !depName.Equals(row1.Cells[cNo].ToString())) {
                                                        depName = row1.Cells[cNo].ToString().Trim();
                                                        courseName = string.Empty;
                                                        courseName2 = string.Empty;
                                                    }
                                                    if (!string.IsNullOrEmpty(row2.Cells[cNo].ToString()) && !courseName.Equals(row2.Cells[cNo].ToString())) {
                                                        courseName = row2.Cells[cNo].ToString().Trim();
                                                        courseName2 = string.Empty;
                                                    }
                                                    if (!string.IsNullOrEmpty(row3.Cells[cNo].ToString()) && !courseName2.Equals(row3.Cells[cNo].ToString())) {
                                                        courseName2 = row3.Cells[cNo].ToString().Trim();
                                                    }
                                                    //確認班系課程資料
                                                    if (!string.IsNullOrEmpty(courseName2)) {
                                                        if (courseName2.Equals("A") || courseName2.Equals("B")) {
                                                            itemCourseName = courseName.Trim() + courseName2.Trim();
                                                        }
                                                        else {
                                                            itemCourseName = courseName.Trim() + "-" + courseName2.Trim();
                                                        }
                                                    }
                                                    else {
                                                        itemCourseName = courseName.Trim();
                                                    }
                                                    string cellStr = depName + "|" + courseName + "|" + courseName2;
                                                    if (!courseData.Any(e => e.CourseDepartmentName == depName && e.CourseName == itemCourseName && e.Name == cellStr)) {
                                                        courseData.Add(new ImportCourse() {
                                                            CourseDepartmentName = depName,
                                                            CourseName = itemCourseName,
                                                            Name = cellStr,
                                                        });
                                                    }

                                                    if (cNo == 0) {
                                                        if (!string.IsNullOrEmpty(rowR.Cells[cNo].ToString()) && !schoolName.Equals(rowR.Cells[cNo].ToString())) {
                                                            schoolName = rowR.Cells[cNo].ToString().Trim();
                                                        }
                                                    }
                                                    else if (cNo == 1) {
                                                        if (!string.IsNullOrEmpty(rowR.Cells[cNo].ToString()) && !classType.Equals(rowR.Cells[cNo].ToString())) {
                                                            classType = rowR.Cells[cNo].ToString().Trim();
                                                        }
                                                    }
                                                    else {
                                                        int number = 0;
                                                        try {
                                                            if (rowR.Cells[cNo].CellType == CellType.Formula) {
                                                                number = int.Parse(rowR.Cells[cNo].NumericCellValue.ToString("N0"));
                                                            }
                                                            else {
                                                                number = string.IsNullOrEmpty(rowR.Cells[cNo].ToString()) ? 0 : int.Parse(rowR.Cells[cNo].ToString());
                                                            }
                                                        }
                                                        catch (Exception ex) {
                                                            string fake = ex.Message;
                                                            number = 0;
                                                        }
                                                        mappings.Add(new Mapping() {
                                                            Year = 2025,
                                                            YearStr = 114,
                                                            Week = 12,
                                                            Name = schoolName,
                                                            ClassType = classType,
                                                            CourseDepartmentName = depName,
                                                            CourseName = itemCourseName,
                                                            //CourseId = dataContext.Course.Any(e => e.Name == itemCourseName) ? dataContext.Course.FirstOrDefault(e => e.Name == itemCourseName).Id : 0,
                                                            Number = number
                                                        });

                                                    }
                                                }
                                                catch (Exception ex) {
                                                    Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                                    continue;
                                                }
                                            }
                                        }
                                    }
                                    string debug = string.Empty;

                                    dataContext.ImportCourse.AddRange(courseData);
                                    dataContext.SaveChanges();

                                    dataContext.Mapping.AddRange(mappings);
                                    dataContext.SaveChanges();
                                    #region
                                    //for (int cNo = 0; cNo < row0.Cells.Count; cNo++) {
                                    //    try {
                                    //        if (!string.IsNullOrEmpty(row1.Cells[cNo].ToString()) && !depName.Equals(row1.Cells[cNo].ToString())) {
                                    //            depName = row1.Cells[cNo].ToString().Trim();
                                    //            courseName = string.Empty;
                                    //            courseName2 = string.Empty;
                                    //        }
                                    //        if (!string.IsNullOrEmpty(row2.Cells[cNo].ToString()) && !courseName.Equals(row2.Cells[cNo].ToString())) {
                                    //            courseName = row2.Cells[cNo].ToString().Trim();
                                    //            courseName2 = string.Empty;
                                    //        }
                                    //        if (!string.IsNullOrEmpty(row3.Cells[cNo].ToString()) && !courseName2.Equals(row3.Cells[cNo].ToString())) {
                                    //            courseName2 = row3.Cells[cNo].ToString().Trim();
                                    //        }
                                    //        //確認班系課程資料
                                    //        if (!string.IsNullOrEmpty(courseName2)) {
                                    //            if (courseName2.Equals("A") || courseName2.Equals("B")) {
                                    //                itemCourseName = courseName.Trim() + courseName2.Trim();
                                    //            }
                                    //            else {
                                    //                itemCourseName = courseName.Trim() + "-" + courseName2.Trim();
                                    //            }
                                    //        }
                                    //        else {
                                    //            itemCourseName = courseName.Trim();
                                    //        }

                                    //        //if (mappings.Any(e => e.CourseDepartmentName == depName && e.CourseName == itemCourseName)) {
                                    //        //    continue;
                                    //        //}
                                    //        //else {
                                    //        //    ImportMapping mappingItem = new ImportMapping();
                                    //        //    mappingItem.CourseDepartmentName = depName;
                                    //        //    mappingItem.CourseName = itemCourseName;
                                    //        //    if (mappingItem.CourseDepartmentName != null && mappingItem.CourseName != null) {
                                    //        //        mappings.Add(mappingItem);
                                    //        //    }
                                    //        //}

                                    //        if (courseData.Any(e => e.CourseDepartmentName == depName && e.CourseName == itemCourseName)) {
                                    //            continue;
                                    //        }
                                    //        else {
                                    //            if (dataContext.Course.Any(e => e.Name == itemCourseName)) {
                                    //                ImportCourse newCourseItem = new ImportCourse();
                                    //                newCourseItem.CourseDepartmentName = depName;
                                    //                newCourseItem.Department = dataContext.CourseDepartment.FirstOrDefault(e => e.Name == newCourseItem.CourseDepartmentName);
                                    //                newCourseItem.CourseName = itemCourseName;
                                    //                newCourseItem.Course = dataContext.Course.FirstOrDefault(e => e.Name == newCourseItem.CourseName); ;
                                    //                if (newCourseItem.Department != null && newCourseItem.Course != null) {
                                    //                    courseData.Add(newCourseItem);
                                    //                }
                                    //            }
                                    //        }
                                    //    }
                                    //    catch (Exception ex) {
                                    //        Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                    //        throw new FrameworkException(ex.Message);
                                    //    }
                                    //}
                                    #endregion

                                    #endregion
                                }
                                else {

                                }
                            }
                            catch (Exception ex) {
                                Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                throw new FrameworkException(ex.Message);
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

        #region 匯入資料 
        //使用公版匯入總人數表
        [HttpGet("ImportPH")]
        public IActionResult ImportPH() {
            try {
                DataContext dataContext = new DataContext();
                using (
                    FileStream file = new FileStream(@"C:\\Users\\hound\\Downloads\\人數表系統\\20251218\\2025 07(全國人數表第24週)_北.xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new System.Data.DataException("取得資料發生錯誤");
                    try {

                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet1 = workbook.GetSheetAt(0);
                        var list = new List<ISheet>() { sheet1 };
                        var count = 0;
                        string[] input = new string[3];
                        var sheet = workbook.GetSheetAt(0);
                        IRow headerRow = sheet.GetRow(1);
                        string year = "2025";
                        int yearInt = 0;
                        int weekInt = 0;
                        int colCount = headerRow.Cells.Count();
                        int rowCount = sheet.LastRowNum;
                        try {
                            var readSheet = workbook.GetSheetAt(0);
                            #region 解析資料
                            IRow countryRow = readSheet.GetRow(4);
                            string schoolName = string.Empty;
                            string classType = string.Empty;
                            for (int rNo = 5; rNo <= readSheet.LastRowNum; rNo++) {
                                StudentPopulation populationData = new StudentPopulation();
                                ClassType cType = new ClassType();
                                IRow rowR = readSheet.GetRow(rNo);
                                if (rowR == null) {
                                    continue;
                                }
                                else {
                                    School schoolData = dataContext.School.FirstOrDefault(e => e.Name == rowR.Cells[2].ToString().Trim());
                                    if (schoolData == null) {
                                        continue;
                                    }
                                    try {
                                        #region 取得基礎資料
                                        //取得年度
                                        if (!string.IsNullOrEmpty(rowR.Cells[0].ToString())) {
                                            yearInt = int.Parse(rowR.Cells[0].ToString().Trim());
                                            year = (yearInt + 1911).ToString();
                                        }
                                        else {
                                            continue;
                                        }
                                        //取得周次
                                        if (!string.IsNullOrEmpty(rowR.Cells[1].ToString())) {
                                            weekInt = int.Parse(rowR.Cells[1].ToString().Trim());
                                        }
                                        else {
                                            continue;
                                        }
                                        SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.Year == yearInt && e.Week == weekInt).FirstOrDefault();
                                        //取得班別                                                                              
                                        if (rowR.Cells[3].ToString().Trim().Equals("團")) {
                                            cType = ClassType.Group;
                                        }
                                        else if (rowR.Cells[3].ToString().Trim().Equals("小")) {
                                            cType = ClassType.SubGroup;
                                        }
                                        else if (rowR.Cells[3].ToString().Trim().Equals("三")) {
                                            cType = ClassType.V3;
                                        }
                                        else {
                                            cType = ClassType.General;
                                        }
                                        #endregion
                                        //刪除既有資料
                                        if (schoolData != null && cType == ClassType.SubGroup) {
                                            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PH)) {
                                                populationData = dataContext.StudentPopulation.First(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PH);
                                                //刪除既有明細資料
                                                var delDetails = dataContext.StudentPopulationItem.Where(e => e.StudentPopulation.Id == populationData.Id).ToList();
                                                dataContext.StudentPopulationItem.RemoveRange(delDetails);
                                                dataContext.SaveChanges();
                                            }
                                            else {
                                                populationData = new StudentPopulation();
                                                populationData.SchoolId = schoolData.Id;
                                                populationData.Year = yearInt;
                                                populationData.Week = schoolYear.Week.Value;
                                                populationData.WeekDate = schoolYear.WeekStartDate;
                                                populationData.Items = new List<StudentPopulationItem>();
                                                populationData.Submitter = dataContext.Member.Find(Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD"));
                                                populationData.Type = StudentPopulationType.PH;
                                                populationData.Name = string.Format("{0}第{1}週百瀚人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                                                dataContext.StudentPopulation.Add(populationData);
                                                dataContext.SaveChanges();
                                            }
                                        }
                                        else {
                                            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PH)) {
                                                populationData = dataContext.StudentPopulation.First(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PH);
                                            }
                                            else {
                                                populationData = new StudentPopulation();
                                                populationData.SchoolId = schoolData.Id;
                                                populationData.Year = yearInt;
                                                populationData.Week = schoolYear.Week.Value;
                                                populationData.WeekDate = schoolYear.WeekStartDate;
                                                populationData.Items = new List<StudentPopulationItem>();
                                                populationData.Submitter = dataContext.Member.Find(Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD"));
                                                populationData.Type = StudentPopulationType.PH;
                                                populationData.Name = string.Format("{0}第{1}週百瀚人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                                                dataContext.StudentPopulation.Add(populationData);
                                                dataContext.SaveChanges();
                                            }
                                        }

                                    }
                                    catch (Exception ex) {
                                        Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                        continue;
                                    }
                                    //開始匯入
                                    for (int cNo = 4; cNo <= rowR.Cells.Count; cNo++) {
                                        try {
                                            if (countryRow.Cells[cNo] != null && !countryRow.Cells[cNo].ToString().ToUpper().Equals("P")) {
                                                int cId = 0;
                                                try {
                                                    cId = int.Parse(countryRow.Cells[cNo].ToString().Trim());
                                                }
                                                catch {
                                                    continue;
                                                }
                                                Course course = dataContext.Course.Include("Department").FirstOrDefault(e => e.Id == cId);
                                                int cellNo = 0;
                                                try {


                                                    if (rowR.Cells[cNo].CellType != CellType.Formula) {
                                                        if (rowR.Cells[cNo].HasValue() && !string.IsNullOrEmpty(rowR.Cells[cNo].ToString())) {
                                                            try {
                                                                cellNo = int.Parse(rowR.Cells[cNo].ToString().Trim());
                                                            }
                                                            catch {
                                                                cellNo = 0;
                                                            }
                                                        }
                                                        else {
                                                            continue;
                                                        }
                                                    }
                                                    else {
                                                        try {
                                                            rowR.Cells[cNo].SetCellType(CellType.Numeric);
                                                            cellNo = int.Parse(rowR.Cells[cNo].NumericCellValue.ToString());
                                                        }
                                                        catch {
                                                            continue;
                                                        }
                                                    }

                                                }
                                                catch (Exception ex) {
                                                    cellNo = 0;
                                                }

                                                if (course != null && cellNo != 0) {
                                                    //判斷是否為個別指導 個別指導需要依照人數開班
                                                    if (course.Name.IndexOf("EM1") > 0) {
                                                        for (int i = 0; i < cellNo; i++) {
                                                            //新增班級
                                                            Class newClass = new Class();
                                                            try {
                                                                //取得目前班級數
                                                                int classCount = dataContext.StudentPopulationItem.Count(e => e.Class.Course.Id == course.Id);
                                                                newClass.Course = null;
                                                                newClass.CourseId = course.Id;
                                                                newClass.SchoolId = schoolData.Id;
                                                                newClass.Type = cType;
                                                                newClass.Name = string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00"));
                                                                dataContext.Class.Add(newClass);
                                                                dataContext.SaveChanges();
                                                            }
                                                            catch (Exception ex) {
                                                                string e = ex.Message;
                                                            }
                                                            StudentPopulationItem addItem = new StudentPopulationItem();
                                                            addItem.Class = null;
                                                            addItem.ClassId = newClass.Id;
                                                            addItem.Name = newClass.Name;
                                                            addItem.Number = 1;
                                                            addItem.SchoolName = newClass.Name;
                                                            addItem.LastWeekNumber = 0;
                                                            addItem.StudentPopulation = null;
                                                            addItem.StudentPopulationId = populationData.Id;
                                                            dataContext.StudentPopulationItem.Add(addItem);
                                                            dataContext.SaveChanges();
                                                        }
                                                    }
                                                    else {
                                                        //新增班級
                                                        //取得目前班級數
                                                        Class newClass = new Class();
                                                        try {
                                                            //確認開班
                                                            int classCount = dataContext.StudentPopulationItem.Count(e => e.Class.Course.Id == course.Id);
                                                            newClass.Course = null;
                                                            newClass.CourseId = course.Id;
                                                            newClass.SchoolId = schoolData.Id;
                                                            newClass.Type = cType;
                                                            newClass.Name = string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00"));
                                                            dataContext.Class.Add(newClass);
                                                            dataContext.SaveChanges();
                                                        }
                                                        catch (Exception ex) {
                                                            string e = ex.Message;
                                                        }
                                                        StudentPopulationItem addItem = new StudentPopulationItem();
                                                        addItem.Class = null;
                                                        addItem.ClassId = newClass.Id;
                                                        addItem.Name = newClass.Name;
                                                        addItem.Number = cellNo;
                                                        addItem.SchoolName = newClass.Name;
                                                        addItem.LastWeekNumber = 0;
                                                        addItem.StudentPopulation = null;
                                                        addItem.StudentPopulationId = populationData.Id;
                                                        dataContext.StudentPopulationItem.Add(addItem);
                                                        dataContext.SaveChanges();
                                                    }
                                                    try {

                                                    }
                                                    catch (Exception ex) {

                                                    }
                                                }
                                            }
                                            else {
                                                continue;
                                            }
                                        }
                                        catch {
                                            continue;
                                        }
                                    }
                                }
                            }
                            string debug = string.Empty;
                            #endregion
                        }
                        catch (Exception ex) {
                            Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                            throw new FrameworkException(ex.Message);
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

        [HttpGet("ImportGEPT")]
        public IActionResult ImportGEPT() {
            try {
                DataContext dataContext = new DataContext();
                using (
                    FileStream file = new FileStream(@"C:\\Users\\hound\\Downloads\\人數表系統\\20251218\\2025 07(全國人數表第24週)_英檢.xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new System.Data.DataException("取得資料發生錯誤");
                    try {
                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet1 = workbook.GetSheetAt(0);
                        var list = new List<ISheet>() { sheet1 };
                        var count = 0;
                        string[] input = new string[3];
                        var sheet = workbook.GetSheetAt(0);
                        IRow headerRow = sheet.GetRow(1);
                        string year = "2025";
                        int yearInt = 0;
                        int weekInt = 0;
                        int colCount = headerRow.Cells.Count();
                        int rowCount = sheet.LastRowNum;
                        try {
                            var readSheet = workbook.GetSheetAt(0);
                            #region 解析資料
                            IRow countryRow = readSheet.GetRow(4);
                            string schoolName = string.Empty;
                            string classType = string.Empty;
                            for (int rNo = 5; rNo <= readSheet.LastRowNum; rNo++) {
                                StudentPopulation populationData = new StudentPopulation();
                                ClassType cType = new ClassType();
                                IRow rowR = readSheet.GetRow(rNo);
                                if (rowR == null) {
                                    continue;
                                }
                                else {
                                    School schoolData = dataContext.School.FirstOrDefault(e => e.Name == rowR.Cells[2].ToString().Trim());
                                    if (schoolData == null) {
                                        continue;
                                    }
                                    try {
                                        #region 取得基礎資料
                                        //取得年度
                                        if (!string.IsNullOrEmpty(rowR.Cells[0].ToString())) {
                                            yearInt = int.Parse(rowR.Cells[0].ToString().Trim());
                                            year = (yearInt + 1911).ToString();
                                        }
                                        else {
                                            continue;
                                        }
                                        //取得周次
                                        if (!string.IsNullOrEmpty(rowR.Cells[1].ToString())) {
                                            weekInt = int.Parse(rowR.Cells[1].ToString().Trim());
                                        }
                                        else {
                                            continue;
                                        }
                                        SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.Year == yearInt && e.Week == weekInt).FirstOrDefault();
                                        //取得班別                                                                              
                                        if (rowR.Cells[3].ToString().Trim().Equals("團")) {
                                            cType = ClassType.Group;
                                        }
                                        else if (rowR.Cells[3].ToString().Trim().Equals("小")) {
                                            cType = ClassType.SubGroup;
                                        }
                                        else if (rowR.Cells[3].ToString().Trim().Equals("三")) {
                                            cType = ClassType.V3;
                                        }
                                        else {
                                            cType = ClassType.General;
                                        }
                                        #endregion
                                        //刪除既有資料
                                        if (schoolData != null) {
                                            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.GEPT)) {
                                                populationData = dataContext.StudentPopulation.First(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.GEPT);
                                                //刪除既有明細資料
                                                var delDetails = dataContext.StudentPopulationItem.Where(e => e.StudentPopulation.Id == populationData.Id).ToList();
                                                dataContext.StudentPopulationItem.RemoveRange(delDetails);
                                                dataContext.SaveChanges();
                                            }
                                            else {
                                                populationData = new StudentPopulation();
                                                populationData.SchoolId = schoolData.Id;
                                                populationData.Year = yearInt;
                                                populationData.Week = schoolYear.Week.Value;
                                                populationData.WeekDate = schoolYear.WeekStartDate;
                                                populationData.Items = new List<StudentPopulationItem>();
                                                populationData.Submitter = dataContext.Member.Find(Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD"));
                                                populationData.Type = StudentPopulationType.GEPT;
                                                populationData.Name = string.Format("{0}第{1}週英檢人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                                                dataContext.StudentPopulation.Add(populationData);
                                                dataContext.SaveChanges();
                                            }
                                        }
                                        else {
                                            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.GEPT)) {
                                                populationData = dataContext.StudentPopulation.First(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.GEPT);
                                            }
                                            else {
                                                populationData = new StudentPopulation();
                                                populationData.SchoolId = schoolData.Id;
                                                populationData.Year = yearInt;
                                                populationData.Week = schoolYear.Week.Value;
                                                populationData.WeekDate = schoolYear.WeekStartDate;
                                                populationData.Items = new List<StudentPopulationItem>();
                                                populationData.Submitter = dataContext.Member.Find(Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD"));
                                                populationData.Type = StudentPopulationType.GEPT;
                                                populationData.Name = string.Format("{0}第{1}週英檢人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                                                dataContext.StudentPopulation.Add(populationData);
                                                dataContext.SaveChanges();
                                            }
                                        }

                                    }
                                    catch (Exception ex) {
                                        Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                        continue;
                                    }
                                    //開始匯入
                                    for (int cNo = 4; cNo <= rowR.Cells.Count; cNo++) {
                                        try {
                                            if (countryRow.Cells[cNo] != null && !countryRow.Cells[cNo].ToString().ToUpper().Equals("P")) {
                                                int cId = 0;
                                                try {
                                                    cId = int.Parse(countryRow.Cells[cNo].ToString().Trim());
                                                }
                                                catch {
                                                    continue;
                                                }
                                                Course course = dataContext.Course.Include("Department").FirstOrDefault(e => e.Id == cId);
                                                int cellNo = 0;
                                                try {
                                                    if (rowR.Cells[cNo].CellType != CellType.Formula) {
                                                        if (rowR.Cells[cNo].HasValue() && !string.IsNullOrEmpty(rowR.Cells[cNo].ToString())) {
                                                            try {
                                                                cellNo = int.Parse(rowR.Cells[cNo].ToString().Trim());
                                                            }
                                                            catch {
                                                                cellNo = 0;
                                                            }
                                                        }
                                                        else {
                                                            continue;
                                                        }
                                                    }
                                                    else {
                                                        try {
                                                            rowR.Cells[cNo].SetCellType(CellType.Numeric);
                                                            cellNo = int.Parse(rowR.Cells[cNo].NumericCellValue.ToString());
                                                        }
                                                        catch (Exception ex) {
                                                            Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                                            continue;
                                                        }
                                                    }

                                                }
                                                catch (Exception ex) {
                                                    Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                                    continue;
                                                }

                                                if (course != null && cellNo > 0) {
                                                    //判斷是否為個別指導 個別指導需要依照人數開班
                                                    if (course.Name.IndexOf("EM1") > 0) {
                                                        for (int i = 0; i < cellNo; i++) {
                                                            //新增班級
                                                            Class newClass = new Class();
                                                            try {
                                                                //取得目前班級數
                                                                int classCount = dataContext.StudentPopulationItem.Count(e => e.Class.Course.Id == course.Id);
                                                                newClass.Course = null;
                                                                newClass.CourseId = course.Id;
                                                                newClass.SchoolId = schoolData.Id;
                                                                newClass.Type = cType;
                                                                newClass.Name = string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00"));
                                                                dataContext.Class.Add(newClass);
                                                                dataContext.SaveChanges();
                                                            }
                                                            catch (Exception ex) {
                                                                Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                                                continue;
                                                            }
                                                            StudentPopulationItem addItem = new StudentPopulationItem();
                                                            addItem.Class = null;
                                                            addItem.ClassId = newClass.Id;
                                                            addItem.Name = newClass.Name;
                                                            addItem.Number = 1;
                                                            addItem.SchoolName = newClass.Name;
                                                            addItem.LastWeekNumber = 0;
                                                            addItem.StudentPopulation = null;
                                                            addItem.StudentPopulationId = populationData.Id;
                                                            dataContext.StudentPopulationItem.Add(addItem);
                                                            dataContext.SaveChanges();
                                                        }
                                                    }
                                                    else {
                                                        //新增班級
                                                        //取得目前班級數
                                                        Class newClass = new Class();
                                                        try {
                                                            //確認開班
                                                            int classCount = dataContext.StudentPopulationItem.Count(e => e.Class.Course.Id == course.Id);
                                                            newClass.Course = null;
                                                            newClass.CourseId = course.Id;
                                                            newClass.SchoolId = schoolData.Id;
                                                            newClass.Type = cType;
                                                            newClass.Name = string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00"));
                                                            dataContext.Class.Add(newClass);
                                                            dataContext.SaveChanges();
                                                        }
                                                        catch (Exception ex) {
                                                            Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                                            continue;
                                                        }
                                                        StudentPopulationItem addItem = new StudentPopulationItem();
                                                        addItem.Class = null;
                                                        addItem.ClassId = newClass.Id;
                                                        addItem.Name = newClass.Name;
                                                        addItem.Number = cellNo;
                                                        addItem.SchoolName = newClass.Name;
                                                        addItem.LastWeekNumber = 0;
                                                        addItem.StudentPopulation = null;
                                                        addItem.StudentPopulationId = populationData.Id;
                                                        dataContext.StudentPopulationItem.Add(addItem);
                                                        dataContext.SaveChanges();
                                                    }
                                                    try {

                                                    }
                                                    catch (Exception ex) {
                                                        Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                                        continue;
                                                    }
                                                }
                                            }
                                            else {
                                                continue;
                                            }
                                        }
                                        catch (Exception ex) {
                                            Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                            continue;
                                        }
                                    }
                                }
                            }
                            string debug = string.Empty;
                            #endregion
                        }
                        catch (Exception ex) {
                            Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                            throw new FrameworkException(ex.Message);
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

        [HttpGet("ImportPS")]
        public IActionResult ImportPS() {
            try {
                DataContext dataContext = new DataContext();
                using (
                    FileStream file = new FileStream(@"C:\\Users\\hound\\Downloads\\人數表系統\\20251218\\PS南區人數統計表 114學年度(24週).xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new System.Data.DataException("取得資料發生錯誤");
                    try {

                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet1 = workbook.GetSheetAt(0);
                        var list = new List<ISheet>() { sheet1 };
                        var count = 0;
                        string[] input = new string[3];
                        var sheet = workbook.GetSheetAt(0);
                        IRow headerRow = sheet.GetRow(1);
                        string year = "2025";
                        int yearInt = 0;
                        int weekInt = 0;
                        int colCount = headerRow.Cells.Count();
                        int rowCount = sheet.LastRowNum;
                        try {
                            var readSheet = workbook.GetSheetAt(0);
                            #region 解析資料
                            IRow countryRow = readSheet.GetRow(2);
                            string schoolName = string.Empty;
                            string classType = string.Empty;
                            for (int rNo = 3; rNo <= readSheet.LastRowNum; rNo++) {
                                StudentPopulation populationData = new StudentPopulation();
                                ClassType cType = new ClassType();
                                IRow rowR = readSheet.GetRow(rNo);
                                if (rowR == null) {
                                    continue;
                                }
                                else {
                                    School schoolData = dataContext.School.FirstOrDefault(e => e.Name == rowR.Cells[2].ToString().Trim());
                                    if (schoolData == null) {
                                        continue;
                                    }
                                    try {
                                        #region 取得基礎資料
                                        //取得年度
                                        if (!string.IsNullOrEmpty(rowR.Cells[0].ToString())) {
                                            yearInt = int.Parse(rowR.Cells[0].ToString().Trim());
                                            year = (yearInt + 1911).ToString();
                                        }
                                        else {
                                            continue;
                                        }
                                        //取得周次
                                        if (!string.IsNullOrEmpty(rowR.Cells[1].ToString())) {
                                            weekInt = int.Parse(rowR.Cells[1].ToString().Trim());
                                        }
                                        else {
                                            continue;
                                        }
                                        SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.Year == yearInt && e.Week == weekInt).FirstOrDefault();
                                        //取得班別                                                                              
                                        cType = ClassType.General;
                                        #endregion
                                        //刪除既有資料
                                        if (schoolData != null) {
                                            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PS)) {
                                                populationData = dataContext.StudentPopulation.First(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PS);
                                                //刪除既有明細資料
                                                var delDetails = dataContext.StudentPopulationItem.Where(e => e.StudentPopulation.Id == populationData.Id).ToList();
                                                dataContext.StudentPopulationItem.RemoveRange(delDetails);
                                                dataContext.SaveChanges();
                                            }
                                            else {
                                                populationData = new StudentPopulation();
                                                populationData.SchoolId = schoolData.Id;
                                                populationData.Year = yearInt;
                                                populationData.Week = schoolYear.Week.Value;
                                                populationData.WeekDate = schoolYear.WeekStartDate;
                                                populationData.Items = new List<StudentPopulationItem>();
                                                populationData.Submitter = dataContext.Member.Find(Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD"));
                                                populationData.Type = StudentPopulationType.PS;
                                                populationData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                                                dataContext.StudentPopulation.Add(populationData);
                                                dataContext.SaveChanges();
                                            }
                                        }
                                        else {
                                            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PS)) {
                                                populationData = dataContext.StudentPopulation.First(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PS);
                                            }
                                            else {
                                                populationData = new StudentPopulation();
                                                populationData.SchoolId = schoolData.Id;
                                                populationData.Year = yearInt;
                                                populationData.Week = schoolYear.Week.Value;
                                                populationData.WeekDate = schoolYear.WeekStartDate;
                                                populationData.Items = new List<StudentPopulationItem>();
                                                populationData.Submitter = dataContext.Member.Find(Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD"));
                                                populationData.Type = StudentPopulationType.PS;
                                                populationData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                                                dataContext.StudentPopulation.Add(populationData);
                                                dataContext.SaveChanges();
                                            }
                                        }

                                    }
                                    catch (Exception ex) {
                                        Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                        continue;
                                    }
                                    //開始匯入
                                    for (int cNo = 3; cNo <= rowR.Cells.Count; cNo++) {
                                        try {
                                            if (countryRow.Cells[cNo] != null && !countryRow.Cells[cNo].ToString().ToUpper().Equals("P")) {
                                                int cId = 0;
                                                try {
                                                    cId = int.Parse(countryRow.Cells[cNo].ToString().Trim());
                                                }
                                                catch {
                                                    continue;
                                                }
                                                Course course = dataContext.Course.Include("Department").FirstOrDefault(e => e.Id == cId);
                                                int cellNo = 0;
                                                try {
                                                    if (rowR.Cells[cNo].CellType != CellType.Formula) {
                                                        if (rowR.Cells[cNo].HasValue() && !string.IsNullOrEmpty(rowR.Cells[cNo].ToString())) {
                                                            try {
                                                                cellNo = int.Parse(rowR.Cells[cNo].ToString().Trim());
                                                            }
                                                            catch {
                                                                cellNo = 0;
                                                            }
                                                        }
                                                        else {
                                                            continue;
                                                        }
                                                    }
                                                    else {
                                                        try {
                                                            rowR.Cells[cNo].SetCellType(CellType.Numeric);
                                                            cellNo = int.Parse(Math.Round(decimal.Parse(rowR.Cells[cNo].NumericCellValue.ToString()), MidpointRounding.AwayFromZero).ToString());
                                                            ;
                                                        }
                                                        catch {
                                                            continue;
                                                        }
                                                    }

                                                }
                                                catch (Exception ex) {
                                                    cellNo = 0;
                                                }

                                                if (course != null && cellNo > 0) {
                                                    //取得目前班級數
                                                    Class newClass = new Class();
                                                    try {
                                                        //確認開班
                                                        int classCount = dataContext.StudentPopulationItem.Count(e => e.Class.Course.Id == course.Id);
                                                        newClass.Course = null;
                                                        newClass.CourseId = course.Id;
                                                        newClass.SchoolId = schoolData.Id;
                                                        newClass.Type = cType;
                                                        newClass.Name = string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00"));
                                                        dataContext.Class.Add(newClass);
                                                        dataContext.SaveChanges();
                                                    }
                                                    catch (Exception ex) {
                                                        string e = ex.Message;
                                                    }
                                                    StudentPopulationItem addItem = new StudentPopulationItem();
                                                    addItem.Class = null;
                                                    addItem.ClassId = newClass.Id;
                                                    addItem.Name = newClass.Name;
                                                    addItem.Number = cellNo;
                                                    addItem.SchoolName = newClass.Name;
                                                    addItem.LastWeekNumber = 0;
                                                    addItem.StudentPopulation = null;
                                                    addItem.StudentPopulationId = populationData.Id;
                                                    dataContext.StudentPopulationItem.Add(addItem);
                                                    dataContext.SaveChanges();
                                                }
                                            }
                                            else {
                                                continue;
                                            }
                                        }
                                        catch {
                                            continue;
                                        }
                                    }
                                }
                            }
                            string debug = string.Empty;
                            #endregion
                        }
                        catch (Exception ex) {
                            Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                            throw new FrameworkException(ex.Message);
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

        [HttpGet("ImportPSJ")]
        public IActionResult ImportPSJ() {
            try {
                DataContext dataContext = new DataContext();
                using (
                    FileStream file = new FileStream(@"C:\\Users\\hound\\Downloads\\人數表系統\\20251218\\（百倍速）人數統計表更新版114.12.13_北.xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new System.Data.DataException("取得資料發生錯誤");
                    try {

                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet1 = workbook.GetSheetAt(0);
                        var list = new List<ISheet>() { sheet1 };
                        var count = 0;
                        string[] input = new string[3];
                        var sheet = workbook.GetSheetAt(0);
                        IRow headerRow = sheet.GetRow(1);
                        string year = "2025";
                        int yearInt = 0;
                        int weekInt = 0;
                        int colCount = headerRow.Cells.Count();
                        int rowCount = sheet.LastRowNum;
                        try {
                            var readSheet = workbook.GetSheetAt(0);
                            #region 解析資料
                            IRow countryRow = readSheet.GetRow(4);
                            string schoolName = string.Empty;
                            string classType = string.Empty;
                            bool first = false;
                            int doSchoolId = 0;
                            string classLv = string.Empty;
                            for (int rNo = 5; rNo <= readSheet.LastRowNum; rNo++) {
                                StudentPopulation populationData = new StudentPopulation();
                                ClassType cType = new ClassType();
                                IRow rowR = readSheet.GetRow(rNo);
                                if (rowR == null) {
                                    continue;
                                }
                                else {
                                    School schoolData = dataContext.School.FirstOrDefault(e => e.Name == rowR.Cells[2].ToString().Trim());
                                    if (schoolData == null) {
                                        continue;
                                    }
                                    if (doSchoolId != schoolData.Id) {
                                        doSchoolId = schoolData.Id;
                                        first = true;
                                    }
                                    else {
                                        first = false;
                                    }
                                    try {
                                        #region 取得基礎資料
                                        //取得年度
                                        if (!string.IsNullOrEmpty(rowR.Cells[0].ToString())) {
                                            yearInt = int.Parse(rowR.Cells[0].ToString().Trim());
                                            year = (yearInt + 1911).ToString();
                                        }
                                        else {
                                            continue;
                                        }
                                        //取得周次
                                        if (!string.IsNullOrEmpty(rowR.Cells[1].ToString())) {
                                            weekInt = int.Parse(rowR.Cells[1].ToString().Trim());
                                        }
                                        else {
                                            continue;
                                        }
                                        //取得年級
                                        if (!string.IsNullOrEmpty(rowR.Cells[3].ToString())) {
                                            classLv = string.IsNullOrEmpty(rowR.Cells[3].ToString().Trim()) ? "" : rowR.Cells[3].ToString().Trim();
                                        }
                                        else {
                                            continue;
                                        }

                                        SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.Year == yearInt && e.Week == weekInt).FirstOrDefault();
                                        #endregion
                                        //刪除既有資料
                                        if (schoolData != null && first) {
                                            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PSJ)) {
                                                populationData = dataContext.StudentPopulation.First(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PSJ);
                                                //刪除既有明細資料
                                                var delDetails = dataContext.StudentPopulationItem.Where(e => e.StudentPopulation.Id == populationData.Id).ToList();
                                                dataContext.StudentPopulationItem.RemoveRange(delDetails);
                                                dataContext.SaveChanges();
                                            }
                                            else {
                                                populationData = new StudentPopulation();
                                                populationData.SchoolId = schoolData.Id;
                                                populationData.Year = yearInt;
                                                populationData.Week = schoolYear.Week.Value;
                                                populationData.WeekDate = schoolYear.WeekStartDate;
                                                populationData.Items = new List<StudentPopulationItem>();
                                                populationData.Submitter = dataContext.Member.Find(Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD"));
                                                populationData.Type = StudentPopulationType.PSJ;
                                                populationData.Name = string.Format("{0}第{1}週百倍速人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                                                dataContext.StudentPopulation.Add(populationData);
                                                dataContext.SaveChanges();
                                            }
                                        }
                                        else {
                                            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PSJ)) {
                                                populationData = dataContext.StudentPopulation.First(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PSJ);
                                            }
                                            else {
                                                populationData = new StudentPopulation();
                                                populationData.SchoolId = schoolData.Id;
                                                populationData.Year = yearInt;
                                                populationData.Week = schoolYear.Week.Value;
                                                populationData.WeekDate = schoolYear.WeekStartDate;
                                                populationData.Items = new List<StudentPopulationItem>();
                                                populationData.Submitter = dataContext.Member.Find(Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD"));
                                                populationData.Type = StudentPopulationType.PSJ;
                                                populationData.Name = string.Format("{0}第{1}週百倍速人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                                                dataContext.StudentPopulation.Add(populationData);
                                                dataContext.SaveChanges();
                                            }
                                        }

                                    }
                                    catch (Exception ex) {
                                        Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                        continue;
                                    }
                                    //開始匯入
                                    for (int cNo = 4; cNo <= rowR.Cells.Count; cNo++) {
                                        try {
                                            if (countryRow.Cells[cNo] != null && !countryRow.Cells[cNo].ToString().ToUpper().Equals("X")) {
                                                string courseName = string.Empty;
                                                int cId = 0;
                                                try {
                                                    if (countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("SP")) {
                                                        cType = ClassType.Personal;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("MS") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) {
                                                        cType = ClassType.SubGroup;
                                                    }
                                                    else {
                                                        cType = ClassType.General;
                                                    }
                                                    //數學1V1
                                                    if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("一年級")) {
                                                        cId = 145;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("二年級")) {
                                                        cId = 146;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("三年級")) {
                                                        cId = 147;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("四年級")) {
                                                        cId = 148;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("五年級")) {
                                                        cId = 149;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("六年級")) {
                                                        cId = 150;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("國一")) {
                                                        cId = 151;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("國二")) {
                                                        cId = 152;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("國三")) {
                                                        cId = 153;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("高一")) {
                                                        cId = 154;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("高二")) {
                                                        cId = 155;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("高三")) {
                                                        cId = 156;
                                                    }//理化
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("一年級")) {
                                                        cId = 195;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("二年級")) {
                                                        cId = 196;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("三年級")) {
                                                        cId = 197;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("四年級")) {
                                                        cId = 198;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("五年級")) {
                                                        cId = 199;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("六年級")) {
                                                        cId = 200;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("國一")) {
                                                        cId = 201;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("國二")) {
                                                        cId = 202;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("國三")) {
                                                        cId = 203;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("高一")) {
                                                        cId = 204;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("高二")) {
                                                        cId = 205;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("高三")) {
                                                        cId = 206;
                                                    }//新生
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("一年級")) {
                                                        cId = 171;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("二年級")) {
                                                        cId = 172;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("三年級")) {
                                                        cId = 173;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("四年級")) {
                                                        cId = 174;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("五年級")) {
                                                        cId = 175;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("六年級")) {
                                                        cId = 176;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("國一")) {
                                                        cId = 177;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("國二")) {
                                                        cId = 178;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("國三")) {
                                                        cId = 179;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("高一")) {
                                                        cId = 180;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("高二")) {
                                                        cId = 181;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("高三")) {
                                                        cId = 182;
                                                    }//流失
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("一年級")) {
                                                        cId = 183;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("二年級")) {
                                                        cId = 184;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("三年級")) {
                                                        cId = 185;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("四年級")) {
                                                        cId = 186;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("五年級")) {
                                                        cId = 187;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("六年級")) {
                                                        cId = 188;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("國一")) {
                                                        cId = 189;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("國二")) {
                                                        cId = 190;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("國三")) {
                                                        cId = 191;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("高一")) {
                                                        cId = 192;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("高二")) {
                                                        cId = 193;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("高三")) {
                                                        cId = 194;
                                                    }//上週比
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("一年級")) {
                                                        cId = 159;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("二年級")) {
                                                        cId = 160;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("三年級")) {
                                                        cId = 161;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("四年級")) {
                                                        cId = 162;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("五年級")) {
                                                        cId = 163;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("六年級")) {
                                                        cId = 164;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("國一")) {
                                                        cId = 165;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("國二")) {
                                                        cId = 166;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("國三")) {
                                                        cId = 167;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("高一")) {
                                                        cId = 168;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("高二")) {
                                                        cId = 169;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("高三")) {
                                                        cId = 170;
                                                    }//總計
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("T")) {
                                                        cId = 158;
                                                    }
                                                    else {
                                                        continue;
                                                    }
                                                }
                                                catch {
                                                    continue;
                                                }
                                                Course course = dataContext.Course.Include("Department").FirstOrDefault(e => e.Id == cId);
                                                int cellNo = 0;
                                                try {


                                                    if (rowR.Cells[cNo].CellType != CellType.Formula) {
                                                        if (rowR.Cells[cNo].HasValue() && !string.IsNullOrEmpty(rowR.Cells[cNo].ToString())) {
                                                            try {
                                                                cellNo = int.Parse(rowR.Cells[cNo].ToString().Trim());
                                                            }
                                                            catch {
                                                                cellNo = 0;
                                                            }
                                                        }
                                                        else {
                                                            continue;
                                                        }
                                                    }
                                                    else {
                                                        try {
                                                            rowR.Cells[cNo].SetCellType(CellType.Numeric);
                                                            cellNo = int.Parse(rowR.Cells[cNo].NumericCellValue.ToString());
                                                        }
                                                        catch {
                                                            continue;
                                                        }
                                                    }

                                                }
                                                catch (Exception ex) {
                                                    cellNo = 0;
                                                }

                                                if (course != null && cellNo > 0) {
                                                    //新增班級
                                                    //取得目前班級數
                                                    Class newClass = new Class();
                                                    try {
                                                        //確認開班
                                                        int classCount = dataContext.StudentPopulationItem.Count(e => e.Class.Course.Id == course.Id);
                                                        newClass.Course = null;
                                                        newClass.CourseId = course.Id;
                                                        newClass.SchoolId = schoolData.Id;
                                                        newClass.Type = cType;
                                                        newClass.Name = string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00"));
                                                        dataContext.Class.Add(newClass);
                                                        dataContext.SaveChanges();
                                                    }
                                                    catch (Exception ex) {
                                                        string e = ex.Message;
                                                    }
                                                    StudentPopulationItem addItem = new StudentPopulationItem();
                                                    addItem.Class = null;
                                                    addItem.ClassId = newClass.Id;
                                                    addItem.Name = newClass.Name;
                                                    addItem.Number = cellNo;
                                                    addItem.SchoolName = newClass.Name;
                                                    addItem.LastWeekNumber = 0;
                                                    addItem.StudentPopulation = null;
                                                    addItem.StudentPopulationId = populationData.Id;
                                                    dataContext.StudentPopulationItem.Add(addItem);
                                                    dataContext.SaveChanges();
                                                }
                                            }
                                            else {
                                                continue;
                                            }
                                        }
                                        catch {
                                            continue;
                                        }
                                    }
                                }
                            }
                            string debug = string.Empty;
                            #endregion
                        }
                        catch (Exception ex) {
                            Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                            throw new FrameworkException(ex.Message);
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

        [HttpGet("ImportAS")]
        public IActionResult ImportAS() {
            try {
                DataContext dataContext = new DataContext();
                using (
                    FileStream file = new FileStream(@"C:\\Users\\hound\\Downloads\\人數表系統\\20251218\\百瀚全區課輔人數總表(20251213).xlsx", FileMode.Open, FileAccess.Read)) {
                    if (!file.HasValue())
                        throw new System.Data.DataException("取得資料發生錯誤");
                    try {

                        var workbook = new XSSFWorkbook(file);
                        ISheet sheet1 = workbook.GetSheetAt(0);
                        var list = new List<ISheet>() { sheet1 };
                        var count = 0;
                        string[] input = new string[3];
                        var sheet = workbook.GetSheetAt(0);
                        IRow headerRow = sheet.GetRow(1);
                        string year = "2025";
                        int yearInt = 0;
                        int weekInt = 0;
                        int colCount = headerRow.Cells.Count();
                        int rowCount = sheet.LastRowNum;
                        try {
                            var readSheet = workbook.GetSheetAt(0);
                            #region 解析資料
                            IRow countryRow = readSheet.GetRow(4);
                            string schoolName = string.Empty;
                            string classType = string.Empty;
                            bool first = false;
                            int doSchoolId = 0;
                            string classLv = string.Empty;
                            for (int rNo = 5; rNo <= readSheet.LastRowNum; rNo++) {
                                StudentPopulation populationData = new StudentPopulation();
                                ClassType cType = new ClassType();
                                IRow rowR = readSheet.GetRow(rNo);
                                if (rowR == null) {
                                    continue;
                                }
                                else {
                                    School schoolData = dataContext.School.FirstOrDefault(e => e.Name == rowR.Cells[2].ToString().Trim());
                                    if (schoolData == null) {
                                        continue;
                                    }
                                    if (doSchoolId != schoolData.Id) {
                                        doSchoolId = schoolData.Id;
                                        first = true;
                                    }
                                    else {
                                        first = false;
                                    }
                                    try {
                                        #region 取得基礎資料
                                        //取得年度
                                        if (!string.IsNullOrEmpty(rowR.Cells[0].ToString())) {
                                            yearInt = int.Parse(rowR.Cells[0].ToString().Trim());
                                            year = (yearInt + 1911).ToString();
                                        }
                                        else {
                                            continue;
                                        }
                                        //取得周次
                                        if (!string.IsNullOrEmpty(rowR.Cells[1].ToString())) {
                                            weekInt = int.Parse(rowR.Cells[1].ToString().Trim());
                                        }
                                        else {
                                            continue;
                                        }
                                        //取得年級
                                        if (!string.IsNullOrEmpty(rowR.Cells[3].ToString())) {
                                            classLv = string.IsNullOrEmpty(rowR.Cells[3].ToString().Trim()) ? "" : rowR.Cells[3].ToString().Trim();
                                        }
                                        else {
                                            continue;
                                        }

                                        SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.Year == yearInt && e.Week == weekInt).FirstOrDefault();
                                        #endregion
                                        //刪除既有資料
                                        if (schoolData != null && first) {
                                            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.AfterSchool)) {
                                                populationData = dataContext.StudentPopulation.First(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.AfterSchool);
                                                //刪除既有明細資料
                                                var delDetails = dataContext.StudentPopulationItem.Where(e => e.StudentPopulation.Id == populationData.Id).ToList();
                                                dataContext.StudentPopulationItem.RemoveRange(delDetails);
                                                dataContext.SaveChanges();
                                            }
                                            else {
                                                populationData = new StudentPopulation();
                                                populationData.SchoolId = schoolData.Id;
                                                populationData.Year = yearInt;
                                                populationData.Week = schoolYear.Week.Value;
                                                populationData.WeekDate = schoolYear.WeekStartDate;
                                                populationData.Items = new List<StudentPopulationItem>();
                                                populationData.Submitter = dataContext.Member.Find(Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD"));
                                                populationData.Type = StudentPopulationType.AfterSchool;
                                                populationData.Name = string.Format("{0}第{1}週課輔人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                                                dataContext.StudentPopulation.Add(populationData);
                                                dataContext.SaveChanges();
                                            }
                                        }
                                        else {
                                            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.AfterSchool)) {
                                                populationData = dataContext.StudentPopulation.First(e => e.School.Id == schoolData.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.AfterSchool);
                                            }
                                            else {
                                                populationData = new StudentPopulation();
                                                populationData.SchoolId = schoolData.Id;
                                                populationData.Year = yearInt;
                                                populationData.Week = schoolYear.Week.Value;
                                                populationData.WeekDate = schoolYear.WeekStartDate;
                                                populationData.Items = new List<StudentPopulationItem>();
                                                populationData.Submitter = dataContext.Member.Find(Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD"));
                                                populationData.Type = StudentPopulationType.AfterSchool;
                                                populationData.Name = string.Format("{0}第{1}週課輔人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                                                dataContext.StudentPopulation.Add(populationData);
                                                dataContext.SaveChanges();
                                            }
                                        }

                                    }
                                    catch (Exception ex) {
                                        Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                                        continue;
                                    }
                                    //開始匯入
                                    for (int cNo = 4; cNo <= rowR.Cells.Count; cNo++) {
                                        try {
                                            if (countryRow.Cells[cNo] != null && !countryRow.Cells[cNo].ToString().ToUpper().Equals("X")) {
                                                string courseName = string.Empty;
                                                int cId = 0;
                                                try {
                                                    if(countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("SP")) {
                                                        cType = ClassType.Personal;
                                                    }else if (countryRow.Cells[cNo].ToString().Trim().Equals("ES") || countryRow.Cells[cNo].ToString().Trim().Equals("MS") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) {
                                                        cType = ClassType.SubGroup;
                                                    }
                                                    else {
                                                        cType = ClassType.General;
                                                    }
                                                    //安親
                                                    if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("一年級")) {
                                                        cId = 245;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("二年級")) {
                                                        cId = 246;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("三年級")) {
                                                        cId = 247;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("四年級")) {
                                                        cId = 248;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("五年級")) {
                                                        cId = 249;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("六年級")) {
                                                        cId = 250;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("國一")) {
                                                        cId = 251;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("國二")) {
                                                        cId = 252;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("國三")) {
                                                        cId = 253;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("高一")) {
                                                        cId = 254;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("高二")) {
                                                        cId = 255;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("AS") && classLv.Equals("高三")) {
                                                        cId = 256;
                                                    }//英文
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("一年級")) {
                                                        cId = 295;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("二年級")) {
                                                        cId = 296;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("三年級")) {
                                                        cId = 297;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("四年級")) {
                                                        cId = 298;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("五年級")) {
                                                        cId = 299;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("六年級")) {
                                                        cId = 300;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("國一")) {
                                                        cId = 301;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("國二")) {
                                                        cId = 302;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("國三")) {
                                                        cId = 303;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("高一")) {
                                                        cId = 305;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("高二")) {
                                                        cId = 306;
                                                    }
                                                    else if ((countryRow.Cells[cNo].ToString().Trim().Equals("EP") || countryRow.Cells[cNo].ToString().Trim().Equals("EG")) && classLv.Equals("高三")) {
                                                        cId = 307;
                                                    }//數學
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("一年級")) {
                                                    //    cId = 195;                                            
                                                    //}                                                         
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("二年級")) {
                                                    //    cId = 196;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("三年級")) {
                                                    //    cId = 197;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("四年級")) {
                                                    //    cId = 198;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("五年級")) {
                                                    //    cId = 199;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("六年級")) {
                                                    //    cId = 200;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("國一")) {
                                                    //    cId = 201;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("國二")) {
                                                    //    cId = 202;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("國三")) {
                                                    //    cId = 203;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("高一")) {
                                                    //    cId = 204;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("高二")) {
                                                    //    cId = 205;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("MP") || countryRow.Cells[cNo].ToString().Trim().Equals("MS")) && classLv.Equals("高三")) {
                                                    //    cId = 206;
                                                    //}//理化
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("一年級")) {
                                                    //    cId = 195;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("二年級")) {
                                                    //    cId = 196;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("三年級")) {
                                                    //    cId = 197;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("四年級")) {
                                                    //    cId = 198;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("五年級")) {
                                                    //    cId = 199;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("六年級")) {
                                                    //    cId = 200;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("國一")) {
                                                    //    cId = 201;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("國二")) {
                                                    //    cId = 202;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("國三")) {
                                                    //    cId = 203;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("高一")) {
                                                    //    cId = 204;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("高二")) {
                                                    //    cId = 205;
                                                    //}
                                                    //else if ((countryRow.Cells[cNo].ToString().Trim().Equals("SP") || countryRow.Cells[cNo].ToString().Trim().Equals("SS")) && classLv.Equals("高三")) {
                                                    //    cId = 206;
                                                    //}//新生
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("一年級")) {
                                                        cId = 271;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("二年級")) {
                                                        cId = 272;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("三年級")) {
                                                        cId = 273;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("四年級")) {
                                                        cId = 274;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("五年級")) {
                                                        cId = 275;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("六年級")) {
                                                        cId = 276;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("國一")) {
                                                        cId = 277;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("國二")) {
                                                        cId = 278;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("國三")) {
                                                        cId = 279;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("高一")) {
                                                        cId = 280;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("高二")) {
                                                        cId = 281;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("N") && classLv.Equals("高三")) {
                                                        cId = 282;
                                                    }//流失
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("一年級")) {
                                                        cId = 283;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("二年級")) {
                                                        cId = 284;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("三年級")) {
                                                        cId = 285;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("四年級")) {
                                                        cId = 286;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("五年級")) {
                                                        cId = 287;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("六年級")) {
                                                        cId = 288;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("國一")) {
                                                        cId = 289;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("國二")) {
                                                        cId = 290;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("國三")) {
                                                        cId = 291;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("高一")) {
                                                        cId = 292;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("高二")) {
                                                        cId = 293;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("L") && classLv.Equals("高三")) {
                                                        cId = 294;
                                                    }//上週比
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("一年級")) {
                                                        cId = 259;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("二年級")) {
                                                        cId = 260;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("三年級")) {
                                                        cId = 261;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("四年級")) {
                                                        cId = 262;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("五年級")) {
                                                        cId = 263;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("六年級")) {
                                                        cId = 264;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("國一")) {
                                                        cId = 265;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("國二")) {
                                                        cId = 266;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("國三")) {
                                                        cId = 267;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("高一")) {
                                                        cId = 268;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("高二")) {
                                                        cId = 269;
                                                    }
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("W") && classLv.Equals("高三")) {
                                                        cId = 270;
                                                    }//總計
                                                    else if (countryRow.Cells[cNo].ToString().Trim().Equals("T")) {
                                                        cId = 258;
                                                    }
                                                    else {
                                                        continue;
                                                    }
                                                }
                                                catch {
                                                    continue;
                                                }
                                                Course course = dataContext.Course.Include("Department").FirstOrDefault(e => e.Id == cId);
                                                int cellNo = 0;
                                                try {


                                                    if (rowR.Cells[cNo].CellType != CellType.Formula) {
                                                        if (rowR.Cells[cNo].HasValue() && !string.IsNullOrEmpty(rowR.Cells[cNo].ToString())) {
                                                            try {
                                                                cellNo = int.Parse(rowR.Cells[cNo].ToString().Trim());
                                                            }
                                                            catch {
                                                                cellNo = 0;
                                                            }
                                                        }
                                                        else {
                                                            continue;
                                                        }
                                                    }
                                                    else {
                                                        try {
                                                            rowR.Cells[cNo].SetCellType(CellType.Numeric);
                                                            cellNo = int.Parse(rowR.Cells[cNo].NumericCellValue.ToString());
                                                        }
                                                        catch {
                                                            continue;
                                                        }
                                                    }

                                                }
                                                catch (Exception ex) {
                                                    cellNo = 0;
                                                }

                                                if (course != null && cellNo > 0) {
                                                    //新增班級
                                                    //取得目前班級數
                                                    Class newClass = new Class();
                                                    try {
                                                        //確認開班
                                                        int classCount = dataContext.StudentPopulationItem.Count(e => e.Class.Course.Id == course.Id);
                                                        newClass.Course = null;
                                                        newClass.CourseId = course.Id;
                                                        newClass.SchoolId = schoolData.Id;
                                                        newClass.Type = cType;
                                                        newClass.Name = string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00"));
                                                        dataContext.Class.Add(newClass);
                                                        dataContext.SaveChanges();
                                                    }
                                                    catch (Exception ex) {
                                                        string e = ex.Message;
                                                    }
                                                    StudentPopulationItem addItem = new StudentPopulationItem();
                                                    addItem.Class = null;
                                                    addItem.ClassId = newClass.Id;
                                                    addItem.Name = newClass.Name;
                                                    addItem.Number = cellNo;
                                                    addItem.SchoolName = newClass.Name;
                                                    addItem.LastWeekNumber = 0;
                                                    addItem.StudentPopulation = null;
                                                    addItem.StudentPopulationId = populationData.Id;
                                                    dataContext.StudentPopulationItem.Add(addItem);
                                                    dataContext.SaveChanges();
                                                }
                                            }
                                            else {
                                                continue;
                                            }
                                        }
                                        catch {
                                            continue;
                                        }
                                    }
                                }
                            }
                            string debug = string.Empty;
                            #endregion
                        }
                        catch (Exception ex) {
                            Logger?.LogError("匯入EXCEL錯誤{0}", ex.Message);
                            throw new FrameworkException(ex.Message);
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

        #region ImportAll 批次匯入

        private static readonly string[] _gradeOrder = {
            "一年級","二年級","三年級","四年級","五年級","六年級",
            "國一","國二","國三","高一","高二","高三"
        };

        private static readonly Dictionary<string, int[]> _psjCourseIds = new() {
            ["MP"] = new[]{145,146,147,148,149,150,151,152,153,154,155,156},
            ["MS"] = new[]{145,146,147,148,149,150,151,152,153,154,155,156},
            ["SP"] = new[]{195,196,197,198,199,200,201,202,203,204,205,206},
            ["SS"] = new[]{195,196,197,198,199,200,201,202,203,204,205,206},
            ["N"]  = new[]{171,172,173,174,175,176,177,178,179,180,181,182},
            ["L"]  = new[]{183,184,185,186,187,188,189,190,191,192,193,194},
            ["W"]  = new[]{159,160,161,162,163,164,165,166,167,168,169,170},
        };

        private static readonly Dictionary<string, int[]> _asCourseIds = new() {
            ["AS"] = new[]{245,246,247,248,249,250,251,252,253,254,255,256},
            ["EP"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
            ["EG"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
            ["N"]  = new[]{271,272,273,274,275,276,277,278,279,280,281,282},
            ["L"]  = new[]{283,284,285,286,287,288,289,290,291,292,293,294},
            ["W"]  = new[]{259,260,261,262,263,264,265,266,267,268,269,270},
        };

        private static readonly Guid _defaultSubmitterId = Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD");

        // 排除在 PH 匯入之外的頁籤名稱（英檢、舊版本、非人數表頁籤）
        private static readonly HashSet<string> _phExcludeSheets = new(StringComparer.OrdinalIgnoreCase) {
            "英檢", "舊版本", "南區 (舊版本)", "中北區（舊版本）", "Rocky班", "各校開班數"
        };

        // PH: Excel column index → DB Course ID (hardcoded from 百瀚英語南區 multi-level header layout)
        private static readonly Dictionary<int, int> _phColCourseId = new() {
            [9]=1,  [10]=1,                                                    // P1-初階
            [11]=2, [12]=2, [13]=2,                                            // P2-先階
            [14]=3, [15]=3, [16]=3, [17]=3, [18]=3,                            // P3-中階
            [19]=4, [20]=4, [21]=4, [22]=4,                                    // P4-進階
            [23]=5, [24]=5, [25]=5,                                            // P5-高階
            [26]=6, [27]=6, [28]=6,                                            // P6-優階
            [29]=7, [30]=8,                                                    // SAT Junior A/B
            [32]=10,[33]=10,[34]=10,[35]=10,[36]=10,                           // 國一準特/特訓
            [37]=11,[38]=11,[39]=11,[40]=11,[41]=11,[42]=11,                   // 國二準特/特訓
            [43]=12,[44]=12,[45]=12,[46]=12,[47]=12,                           // 國三準特/特訓
            [48]=13,[49]=14,[50]=15,                                           // 海外特訓班-TOEFL/SSAT/PSAT
            [52]=17,[53]=17,                                                   // Elite/英檢/sat班系
            [54]=18,[55]=18,[56]=18,[57]=18,[58]=18,                           // 高中小組班-高一
            [59]=19,[60]=19,[61]=19,[62]=19,[63]=19,[64]=19,[65]=19,           // 高中小組班-高二
            [66]=20,[67]=20,[68]=20,[69]=20,                                   // 高中小組班-高三
            [72]=23,[73]=24,[74]=25,[75]=26,                                   // 英文個別指導
            [77]=28,[78]=29,[79]=30,[80]=31,                                   // 英文合作開班1-4
            [89]=39,[90]=39,[91]=39,[92]=39,[93]=39,                           // 國語文國小三力
            [94]=40,[95]=40,                                                   // 國語文國小中階
            [96]=41,[97]=41,[98]=41,                                           // 國語文國小攻略
            [99]=42,[100]=42,                                                  // 國語文國一班
            [101]=43,[102]=43,                                                 // 國語文國二班
            [103]=44,[104]=44,                                                 // 國語文國三班
            [105]=45,[106]=46,[107]=47,                                        // 國語文高一/二/三班
            [110]=50,[111]=51,[112]=52,[113]=53,                               // 國語文個別指導
            [115]=55,[116]=56,[117]=57,[118]=58,                               // 國語文合作開班
        };

        // PS: Excel column header → DB Course ID (aliases for multi-variant class names)
        private static readonly Dictionary<string, int> _psHeaderCourseId = new(StringComparer.OrdinalIgnoreCase) {
            ["一資"]=108, ["一特"]=109,
            ["二資"]=110, ["二特"]=111, ["二PS特"]=111, ["二特2"]=111,
            ["三資"]=112, ["三特"]=113, ["三特Ps"]=113, ["三特1"]=113, ["三特2"]=113,
            ["四資"]=114, ["四特"]=115, ["四ps特"]=115, ["四P特"]=115, ["四S特"]=115, ["四特1"]=115, ["四特2"]=115,
            ["五資"]=116, ["五特"]=117, ["五P特"]=117, ["五S特"]=117, ["五資1"]=116, ["五特2"]=117,
            ["六資"]=118, ["六特"]=119, ["六P特"]=119, ["六資1"]=118, ["六資2"]=118, ["六特1"]=119, ["六特2"]=119,
            ["七資"]=121, ["七特"]=122, ["七PS特"]=122,
            ["八資"]=123, ["八特"]=124, ["八特1"]=124, ["八特2"]=124, ["八特3"]=124, ["八課內"]=124,
            ["九資"]=125, ["九特"]=126, ["九資1"]=125, ["九資2"]=125,
            ["高一特"]=128, ["高二特"]=129, ["高三特"]=130,
        };

        // CourseIds that use EM1 logic: count = # individual students, each gets its own class record of 1
        private static readonly HashSet<int> _em1CourseIds = new() { 23, 24, 25, 26, 50, 51, 52, 53 };

        private static ClassType PsjColumnType(string code) => code switch {
            "MP" or "SP" => ClassType.Personal,
            "MS" or "SS" => ClassType.SubGroup,
            _ => ClassType.General,
        };

        private static ClassType AsColumnType(string code) => code switch {
            "EP" or "MP" or "SP" => ClassType.Personal,
            "ES" or "MS" or "SS" => ClassType.SubGroup,
            _ => ClassType.General,
        };

        private static int ReadCellNumber(IRow row, int col) {
            var cell = row.GetCell(col);
            if (cell == null) return 0;
            try {
                if (cell.CellType == CellType.Formula) {
                    cell.SetCellType(CellType.Numeric);
                    return (int)Math.Round(cell.NumericCellValue, MidpointRounding.AwayFromZero);
                }
                return int.TryParse(cell.ToString().Trim(), out int v) ? v : 0;
            }
            catch {
                return 0;
            }
        }

        private StudentPopulation GetOrCreatePopulation(DataContext db, int schoolId, int yearInt, int weekInt,
            SchoolYear schoolYear, StudentPopulationType type, string name, bool deleteExisting) {
            StudentPopulation pop;
            if (deleteExisting && db.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type)) {
                pop = db.StudentPopulation.First(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type);
                var delItems = db.StudentPopulationItem.Where(e => e.StudentPopulation.Id == pop.Id).ToList();
                var classIds = delItems.Select(e => e.ClassId).Distinct().ToList();
                db.StudentPopulationItem.RemoveRange(delItems);
                db.SaveChanges();
                var orphanClasses = db.Class
                    .Where(e => classIds.Contains(e.Id) && !db.StudentPopulationItem.Any(i => i.ClassId == e.Id))
                    .ToList();
                if (orphanClasses.Count > 0) {
                    db.Class.RemoveRange(orphanClasses);
                    db.SaveChanges();
                }
            }
            else if (db.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type)) {
                pop = db.StudentPopulation.First(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type);
            }
            else {
                pop = new StudentPopulation {
                    SchoolId = schoolId,
                    Year = yearInt,
                    Week = schoolYear.Week.Value,
                    WeekDate = schoolYear.WeekStartDate,
                    Items = new List<StudentPopulationItem>(),
                    Submitter = db.Member.Find(_defaultSubmitterId),
                    Type = type,
                    Name = name,
                };
                db.StudentPopulation.Add(pop);
                db.SaveChanges();
            }
            return pop;
        }

        private void AddClassAndItem(DataContext db, int schoolId, Course course, ClassType cType, long populationId, int number, ImportAllResult result) {
            var newClass = new Class();
            try {
                int classCount = db.StudentPopulationItem.Count(e => e.Class.Course.Id == course.Id);
                newClass.Course = null;
                newClass.CourseId = course.Id;
                newClass.SchoolId = schoolId;
                newClass.Type = cType;
                newClass.Name = $"{course.Name}_{(classCount + 1):00}";
                db.Class.Add(newClass);
                db.SaveChanges();
            }
            catch (Exception ex) {
                Logger?.LogError("ImportAll 新增班級錯誤: {msg}", ex.Message);
                return;
            }
            db.StudentPopulationItem.Add(new StudentPopulationItem {
                Class = null,
                ClassId = newClass.Id,
                Name = newClass.Name,
                Number = number,
                SchoolName = newClass.Name,
                LastWeekNumber = 0,
                StudentPopulation = null,
                StudentPopulationId = (int)populationId,
            });
            db.SaveChanges();
            result.ItemCount++;
        }

        private static (int academicYear, int week) ParseYearWeekFromTitle(string title) {
            var weekMatch = Regex.Match(title, @"第(\d+)週");
            int week = weekMatch.Success ? int.Parse(weekMatch.Groups[1].Value) : 0;
            var dateMatch = Regex.Match(title, @"(\d+)年(\d+)月");
            int academicYear = 0;
            if (dateMatch.Success) {
                int rocYear = int.Parse(dateMatch.Groups[1].Value);
                int month = int.Parse(dateMatch.Groups[2].Value);
                academicYear = month < 8 ? rocYear - 1 : rocYear;
            }
            return (academicYear, week);
        }

        // Shared sheet reader for PH-style sheets (title row 0, headers rows 1-3, data from row 4).
        // requireTypeIndicator=true  → only process rows where col1 is "小" or "三" (PH behaviour)
        // requireTypeIndicator=false → rows without "小"/"三" are treated as ClassType.General (GEPT behaviour)
        private ImportAllResult RunImportSheetPH(DataContext db, ISheet sheet,
            StudentPopulationType popType, string typeLabel, string nameTemplate,
            bool requireTypeIndicator) {

            var result = new ImportAllResult { File = sheet.SheetName, Type = typeLabel };

            string title = sheet.GetRow(0)?.GetCell(0)?.ToString()?.Trim() ?? "";
            var (yearInt, weekInt) = ParseYearWeekFromTitle(title);
            if (yearInt == 0 || weekInt == 0) {
                result.Errors.Add($"無法從標題解析年份週次: {title}");
                return result;
            }
            SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
            if (schoolYear == null) {
                result.Errors.Add($"找不到學年週次: {yearInt}第{weekInt}週");
                return result;
            }

            int maxHdrCol = 0;
            for (int r = 1; r <= 3; r++) {
                var rw = sheet.GetRow(r);
                if (rw != null && (int)rw.LastCellNum > maxHdrCol) maxHdrCol = (int)rw.LastCellNum;
            }

            var row2Prop = new string[maxHdrCol + 1];
            string lastR2 = "";
            for (int c = 0; c <= maxHdrCol; c++) {
                var cell = sheet.GetRow(2)?.GetCell(c);
                string v = (cell?.CellType == CellType.String) ? (cell.StringCellValue?.Trim() ?? "") : "";
                if (!string.IsNullOrEmpty(v)) lastR2 = v;
                row2Prop[c] = lastR2;
            }

            var colCourseMap = new Dictionary<int, Course>();
            for (int c = 2; c < maxHdrCol; c++) {
                var r3Cell = sheet.GetRow(3)?.GetCell(c);
                string r3Val = (r3Cell?.CellType == CellType.String) ? (r3Cell.StringCellValue?.Trim() ?? "") : "";
                string label = !string.IsNullOrEmpty(r3Val) ? r3Val : row2Prop[c];
                Course course = null;
                if (!string.IsNullOrEmpty(label))
                    course = db.Course.Include("Department").FirstOrDefault(e => e.Name == label);
                if (course == null && _phColCourseId.TryGetValue(c, out int fallbackId))
                    course = db.Course.Include("Department").FirstOrDefault(e => e.Id == fallbackId);
                if (course != null) colCourseMap[c] = course;
            }

            string currentSchoolName = "";
            string lastCountedSchool = "";
            for (int rNo = 4; rNo <= sheet.LastRowNum; rNo++) {
                IRow row = sheet.GetRow(rNo);
                if (row == null) continue;

                string col0 = row.GetCell(0)?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(col0)) currentSchoolName = col0;
                if (string.IsNullOrEmpty(currentSchoolName)) continue;

                string col1 = row.GetCell(1)?.ToString()?.Trim() ?? "";
                ClassType cType;
                bool deleteExisting;
                bool isFirstRow;

                if (col1 == "小") {
                    cType = ClassType.SubGroup; deleteExisting = true; isFirstRow = true;
                } else if (col1 == "三") {
                    cType = ClassType.V3; deleteExisting = false; isFirstRow = false;
                } else if (!requireTypeIndicator) {
                    cType = ClassType.General;
                    isFirstRow = currentSchoolName != lastCountedSchool;
                    deleteExisting = isFirstRow;
                } else {
                    continue;
                }

                School school = db.School.FirstOrDefault(e => e.Name == currentSchoolName);
                if (school == null) continue;

                StudentPopulation pop = GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                    popType, string.Format(nameTemplate, yearInt, weekInt), deleteExisting);
                if (isFirstRow) { result.SchoolCount++; lastCountedSchool = currentSchoolName; }

                foreach (var (col, course) in colCourseMap) {
                    var cell = row.GetCell(col);
                    if (cell == null || cell.CellType != CellType.Numeric) continue;
                    int count = (int)Math.Round(cell.NumericCellValue, MidpointRounding.AwayFromZero);
                    if (count <= 0) continue;
                    if (_em1CourseIds.Contains(course.Id)) {
                        for (int i = 0; i < count; i++)
                            AddClassAndItem(db, school.Id, course, cType, pop.Id, 1, result);
                    } else {
                        AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result);
                    }
                }
            }
            return result;
        }

        private ImportAllResult RunImportPH(DataContext db, string filePath) {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var wb = new XSSFWorkbook(fs);
            var combined = new ImportAllResult { File = Path.GetFileName(filePath), Type = "PH" };
            for (int s = 0; s < wb.NumberOfSheets; s++) {
                var sheet = wb.GetSheetAt(s);
                if (_phExcludeSheets.Contains(sheet.SheetName)) continue;
                var r = RunImportSheetPH(db, sheet, StudentPopulationType.PH,
                    "PH", "{0}第{1}週百瀚人數表", requireTypeIndicator: true);
                combined.SchoolCount += r.SchoolCount;
                combined.ItemCount += r.ItemCount;
                combined.Errors.AddRange(r.Errors);
            }
            return combined;
        }

        private ImportAllResult RunImportGEPT(DataContext db, string filePath) {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var wb = new XSSFWorkbook(fs);
            if (wb.NumberOfSheets < 3)
                return new ImportAllResult { File = Path.GetFileName(filePath), Type = "GEPT",
                    Errors = { "找不到第3個頁籤（英檢）" } };
            return RunImportSheetPH(db, wb.GetSheetAt(2), StudentPopulationType.GEPT,
                "GEPT", "{0}第{1}週英檢人數表", requireTypeIndicator: false);
        }

        private ImportAllResult RunImportPS(DataContext db, string filePath) {
            var result = new ImportAllResult { File = Path.GetFileName(filePath), Type = "PS" };
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var sheet = new XSSFWorkbook(fs).GetSheetAt(0);

            // Parse year/week from row 0 title
            string title = sheet.GetRow(0)?.GetCell(0)?.ToString()?.Trim() ?? "";
            var (yearInt, weekInt) = ParseYearWeekFromTitle(title);
            if (yearInt == 0 || weekInt == 0) {
                result.Errors.Add($"無法從標題解析年份週次: {title}");
                return result;
            }
            SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
            if (schoolYear == null) {
                result.Errors.Add($"找不到學年週次: {yearInt}第{weekInt}週");
                return result;
            }

            // Build col→course map from row 1 headers; fallback to alias dict when Course.Name doesn't match
            IRow headerRow = sheet.GetRow(1);
            var colCourseMap = new Dictionary<int, Course>();
            if (headerRow != null) {
                for (int c = 1; c < (int)headerRow.LastCellNum; c++) {
                    var hCell = headerRow.GetCell(c);
                    if (hCell == null || hCell.CellType != CellType.String) continue;
                    string hdr = hCell.StringCellValue?.Trim() ?? "";
                    if (string.IsNullOrEmpty(hdr)) continue;
                    Course course = db.Course.Include("Department").FirstOrDefault(e => e.Name == hdr);
                    if (course == null && _psHeaderCourseId.TryGetValue(hdr, out int fallbackId))
                        course = db.Course.Include("Department").FirstOrDefault(e => e.Id == fallbackId);
                    if (course != null) colCourseMap[c] = course;
                }
            }
            if (colCourseMap.Count == 0) {
                result.Errors.Add("Row 1 未找到任何課程對應，請確認課程名稱或別名是否與資料庫一致");
                return result;
            }

            // Process data rows starting at row 2
            for (int rNo = 2; rNo <= sheet.LastRowNum; rNo++) {
                IRow row = sheet.GetRow(rNo);
                if (row == null) continue;

                string schoolName = row.GetCell(0)?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(schoolName) || schoolName == "總計") continue;

                School school = db.School.FirstOrDefault(e => e.Name == schoolName);
                if (school == null) continue;

                StudentPopulation pop = GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                    StudentPopulationType.PS, $"{yearInt}第{weekInt}週百世人數表", true);
                result.SchoolCount++;

                foreach (var (col, course) in colCourseMap) {
                    var cell = row.GetCell(col);
                    if (cell == null || cell.CellType != CellType.Numeric) continue;
                    int count = (int)Math.Round(cell.NumericCellValue, MidpointRounding.AwayFromZero);
                    if (count <= 0) continue;
                    AddClassAndItem(db, school.Id, course, ClassType.General, pop.Id, count, result);
                }
            }
            return result;
        }

        private ImportAllResult RunImportPSJ(DataContext db, string filePath) {
            var result = new ImportAllResult { File = Path.GetFileName(filePath), Type = "PSJ" };
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var sheet = new XSSFWorkbook(fs).GetSheetAt(0);
            IRow headerRow = sheet.GetRow(4);

            int prevSchoolId = 0;
            StudentPopulation pop = null;

            for (int rNo = 5; rNo <= sheet.LastRowNum; rNo++) {
                IRow row = sheet.GetRow(rNo);
                if (row == null) continue;

                string schoolName = row.GetCell(2)?.ToString()?.Trim() ?? "";
                School school = db.School.FirstOrDefault(e => e.Name == schoolName);
                if (school == null) continue;

                if (!int.TryParse(row.GetCell(0)?.ToString()?.Trim(), out int yearInt)) continue;
                if (!int.TryParse(row.GetCell(1)?.ToString()?.Trim(), out int weekInt)) continue;
                string grade = row.GetCell(3)?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(grade)) continue;

                SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
                if (schoolYear == null) continue;

                if (school.Id != prevSchoolId) {
                    prevSchoolId = school.Id;
                    pop = GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                        StudentPopulationType.PSJ, $"{yearInt}第{weekInt}週百倍速人數表", true);
                    result.SchoolCount++;
                }

                int gradeIdx = Array.IndexOf(_gradeOrder, grade);
                if (gradeIdx < 0) continue;

                for (int cNo = 4; cNo < headerRow.LastCellNum; cNo++) {
                    try {
                        string code = headerRow.GetCell(cNo)?.ToString()?.Trim() ?? "";
                        if (code.Equals("X", StringComparison.OrdinalIgnoreCase)) continue;

                        int courseId;
                        ClassType cType;
                        if (code == "T") {
                            courseId = 158; cType = ClassType.General;
                        }
                        else if (_psjCourseIds.TryGetValue(code, out int[] ids)) {
                            courseId = ids[gradeIdx]; cType = PsjColumnType(code);
                        }
                        else { continue; }

                        Course course = db.Course.Include("Department").FirstOrDefault(e => e.Id == courseId);
                        int count = ReadCellNumber(row, cNo);
                        if (course == null || count <= 0) continue;

                        AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result);
                    }
                    catch { continue; }
                }
            }
            return result;
        }

        private ImportAllResult RunImportAS(DataContext db, string filePath) {
            var result = new ImportAllResult { File = Path.GetFileName(filePath), Type = "AS" };
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var sheet = new XSSFWorkbook(fs).GetSheetAt(0);
            IRow headerRow = sheet.GetRow(4);

            int prevSchoolId = 0;
            StudentPopulation pop = null;

            for (int rNo = 5; rNo <= sheet.LastRowNum; rNo++) {
                IRow row = sheet.GetRow(rNo);
                if (row == null) continue;

                string schoolName = row.GetCell(2)?.ToString()?.Trim() ?? "";
                School school = db.School.FirstOrDefault(e => e.Name == schoolName);
                if (school == null) continue;

                if (!int.TryParse(row.GetCell(0)?.ToString()?.Trim(), out int yearInt)) continue;
                if (!int.TryParse(row.GetCell(1)?.ToString()?.Trim(), out int weekInt)) continue;
                string grade = row.GetCell(3)?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(grade)) continue;

                SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
                if (schoolYear == null) continue;

                if (school.Id != prevSchoolId) {
                    prevSchoolId = school.Id;
                    pop = GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                        StudentPopulationType.AfterSchool, $"{yearInt}第{weekInt}週課輔人數表", true);
                    result.SchoolCount++;
                }

                int gradeIdx = Array.IndexOf(_gradeOrder, grade);
                if (gradeIdx < 0) continue;

                for (int cNo = 4; cNo < headerRow.LastCellNum; cNo++) {
                    try {
                        string code = headerRow.GetCell(cNo)?.ToString()?.Trim() ?? "";
                        if (code.Equals("X", StringComparison.OrdinalIgnoreCase)) continue;

                        int courseId;
                        ClassType cType;
                        if (code == "T") {
                            courseId = 258; cType = ClassType.General;
                        }
                        else if (_asCourseIds.TryGetValue(code, out int[] ids)) {
                            courseId = ids[gradeIdx]; cType = AsColumnType(code);
                        }
                        else { continue; }

                        Course course = db.Course.Include("Department").FirstOrDefault(e => e.Id == courseId);
                        int count = ReadCellNumber(row, cNo);
                        if (course == null || count <= 0) continue;

                        AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result);
                    }
                    catch { continue; }
                }
            }
            return result;
        }

        [HttpGet("DiagnoseImport")]
        public IActionResult DiagnoseImport(string filePath, int startRow = 0, int endRow = 8) {
            if (!System.IO.File.Exists(filePath))
                return Json(new { success = false, message = $"檔案不存在: {filePath}" });
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var sheet = new XSSFWorkbook(fs).GetSheetAt(0);
            int maxCol = 0;
            for (int r = startRow; r <= Math.Min(endRow, sheet.LastRowNum); r++) {
                var rw = sheet.GetRow(r);
                if (rw != null && rw.LastCellNum > maxCol) maxCol = rw.LastCellNum;
            }
            var rows = new List<object>();
            for (int r = startRow; r <= Math.Min(endRow, sheet.LastRowNum); r++) {
                var row = sheet.GetRow(r);
                var cells = Enumerable.Range(0, maxCol)
                    .Select(i => {
                        var c = row?.GetCell(i);
                        return (object)new { col = i, val = c?.ToString() ?? "", type = c?.CellType.ToString() ?? "Null" };
                    }).Where(x => ((dynamic)x).val != "" || ((dynamic)x).col < 4)
                    .ToList();
                rows.Add(new { rowNum = r, cells });
            }
            using var db = new DataContext();
            var allSchools = db.School.OrderBy(s => s.Id).Select(s => new { s.Id, s.Name }).ToList();
            var allCourses = db.Course.Include("Department")
                .OrderBy(c => c.Department.Id).ThenBy(c => c.Ordinal)
                .Select(c => new { c.Id, c.Name, Dept = c.Department.Name }).ToList();
            return Json(new { totalRows = sheet.LastRowNum, maxCol, rows, allSchools, allCourses });
        }

        [HttpGet("ImportAll")]
        public IActionResult ImportAll(string rootPath = @"C:\Leo\其他\Kuri\人數表匯入A") {
            if (!Directory.Exists(rootPath))
                return Json(new { success = false, message = $"路徑不存在: {rootPath}" });

            var results = new List<ImportAllResult>();
            using var db = new DataContext();

            var weekDirs = Directory.GetDirectories(rootPath)
                .OrderBy(d => int.TryParse(Path.GetFileName(d), out var n) ? n : int.MaxValue);

            foreach (var weekDir in weekDirs) {
                var weekName = Path.GetFileName(weekDir);
                foreach (var filePath in Directory.GetFiles(weekDir, "*.xlsx").OrderBy(f => f)) {
                    var fn = Path.GetFileName(filePath);
                    var fileResults = new List<ImportAllResult>();
                    try {
                        if (fn.Contains("全國人數表")) {
                            fileResults.Add(RunImportPH(db, filePath));
                            fileResults.Add(RunImportGEPT(db, filePath));
                        } else if (fn.Contains("PS南區"))
                            fileResults.Add(RunImportPS(db, filePath));
                        else if (fn.Contains("百倍速"))
                            fileResults.Add(RunImportPSJ(db, filePath));
                        else if (fn.Contains("百瀚全區課輔"))
                            fileResults.Add(RunImportAS(db, filePath));
                    }
                    catch (Exception ex) {
                        fileResults.Add(new ImportAllResult { File = fn, Type = "Unknown", Errors = { ex.Message } });
                        Logger?.LogError("ImportAll 例外 {file}: {msg}", fn, ex.Message);
                    }
                    foreach (var r in fileResults) {
                        r.Week = weekName;
                        results.Add(r);
                        Logger?.LogInformation("ImportAll {week}/{file} [{type}] → {schools}校 {items}筆 錯誤:{errs}",
                            weekName, fn, r.Type, r.SchoolCount, r.ItemCount, r.Errors.Count);
                    }
                }
            }
            return Json(new { success = true, results = results });
        }

        [HttpGet("FillLastWeekNumbers")]
        public IActionResult FillLastWeekNumbers(string rootPath = @"C:\Leo\其他\Kuri\人數表匯入A") {
            if (!Directory.Exists(rootPath))
                return Json(new { success = false, message = $"路徑不存在: {rootPath}" });

            var weekNos = Directory.GetDirectories(rootPath)
                .Select(d => Path.GetFileName(d))
                .Where(n => int.TryParse(n, out _))
                .Select(n => int.Parse(n))
                .ToHashSet();

            using var db = new DataContext();
            var allSchoolYears = db.SchoolYear.OrderBy(sy => sy.Id).ToList();

            int totalUpdated = 0;
            var log = new List<object>();

            var populations = db.StudentPopulation
                .Where(p => weekNos.Contains(p.Week))
                .OrderBy(p => p.Year).ThenBy(p => p.Week)
                .ToList();

            foreach (var pop in populations) {
                var schoolYear = allSchoolYears
                    .FirstOrDefault(sy => sy.Year == pop.Year && sy.Week == pop.Week);
                if (schoolYear == null) continue;

                var prevSY = allSchoolYears
                    .Where(sy => sy.Id < schoolYear.Id)
                    .OrderByDescending(sy => sy.Id)
                    .FirstOrDefault();
                if (prevSY == null) continue;

                var prevPop = db.StudentPopulation
                    .FirstOrDefault(p => p.SchoolId == pop.SchoolId
                                      && p.Year == prevSY.Year
                                      && p.Week == prevSY.Week
                                      && p.Type == pop.Type);
                if (prevPop == null) continue;

                var prevItems = db.StudentPopulationItem
                    .Include("Class")
                    .Where(i => i.StudentPopulationId == prevPop.Id && i.ClassId != null)
                    .ToList();

                // (CourseId, ClassType) → 上週人數總和
                var prevLookup = prevItems
                    .GroupBy(i => (i.Class.CourseId, i.Class.Type))
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Number));

                var currentItems = db.StudentPopulationItem
                    .Include("Class")
                    .Where(i => i.StudentPopulationId == pop.Id && i.ClassId != null)
                    .ToList();

                // 同一 (CourseId, ClassType) 群組：第一筆填入上週總數，其餘填 0
                // 對 EM1 等多筆課程，Sum(LastWeekNumber) 仍等於上週總數，統計正確
                int updated = 0;
                foreach (var grp in currentItems.GroupBy(i => (i.Class.CourseId, i.Class.Type))) {
                    prevLookup.TryGetValue(grp.Key, out int prevTotal);
                    bool isFirst = true;
                    foreach (var item in grp) {
                        item.LastWeekNumber = isFirst ? prevTotal : 0;
                        isFirst = false;
                        updated++;
                    }
                }

                if (updated > 0)
                    db.SaveChanges();

                totalUpdated += updated;
                log.Add(new {
                    week = pop.Week,
                    year = pop.Year,
                    schoolId = pop.SchoolId,
                    type = pop.Type.ToString(),
                    updated
                });
            }

            return Json(new { success = true, totalUpdated, log });
        }

        [HttpGet("FixLastWeekData")]
        public IActionResult FixLastWeekData() {
            using var db = new DataContext();
            var allSchoolYears = db.SchoolYear.OrderBy(sy => sy.Year).ThenBy(sy => sy.Week).ToList();
            var allPopulations = db.StudentPopulation.ToList();

            int totalUpdated = 0;
            var log = new List<object>();

            foreach (var pop in allPopulations.OrderBy(p => p.Year).ThenBy(p => p.Week)) {
                var schoolYear = allSchoolYears.FirstOrDefault(sy => sy.Year == pop.Year && sy.Week == pop.Week);
                if (schoolYear == null) continue;

                SchoolYear prevSY = schoolYear.Week > 1
                    ? allSchoolYears.Where(sy => sy.Year == schoolYear.Year && sy.Week == schoolYear.Week - 1)
                                    .OrderBy(sy => sy.Id).FirstOrDefault()
                    : allSchoolYears.Where(sy => sy.Year == schoolYear.Year - 1)
                                    .OrderByDescending(sy => sy.Week).ThenByDescending(sy => sy.Id).FirstOrDefault();

                if (prevSY == null) continue;

                var prevPop = allPopulations.FirstOrDefault(p =>
                    p.SchoolId == pop.SchoolId && p.Year == prevSY.Year && p.Week == prevSY.Week && p.Type == pop.Type);
                if (prevPop == null) continue;

                var currentItems = db.StudentPopulationItem
                    .Include("Class.Course")
                    .Where(i => i.StudentPopulationId == pop.Id && i.ClassId != null && i.LastWeekNumber == 0)
                    .ToList()
                    .Where(i => i.Class?.Course?.IsSum != true)
                    .ToList();

                if (!currentItems.Any()) continue;

                var prevByClassId = db.StudentPopulationItem
                    .Where(i => i.StudentPopulationId == prevPop.Id && i.ClassId != null)
                    .ToDictionary(i => i.ClassId!.Value, i => i.Number);

                int updated = 0;
                foreach (var item in currentItems) {
                    if (item.ClassId.HasValue && prevByClassId.TryGetValue(item.ClassId.Value, out int prevNum)) {
                        item.LastWeekNumber = prevNum;
                        updated++;
                    }
                }

                if (updated > 0) {
                    db.SaveChanges();
                    totalUpdated += updated;
                    log.Add(new {
                        year = pop.Year,
                        week = pop.Week,
                        schoolId = pop.SchoolId,
                        type = pop.Type.ToString(),
                        updated
                    });
                }
            }

            return Json(new { success = true, totalUpdated, log });
        }

        public class ImportAllResult {
            public string Week { get; set; }
            public string File { get; set; }
            public string Type { get; set; }
            public int SchoolCount { get; set; }
            public int ItemCount { get; set; }
            public List<string> Errors { get; set; } = new();
        }

        #endregion
        #endregion


        public static List<ImportMapping> LoadHeaderMapFromCsv(string path) {

            var map = new List<ImportMapping>();
            using var sr = new StreamReader(path, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            string? header = sr.ReadLine();
            if (header is null) return new List<ImportMapping>();

            var cols = header.Split(',');
            //int idxHeader = Array.FindIndex(cols, c => c.Trim().Equals(headerColName, StringComparison.OrdinalIgnoreCase));
            //int idxTarget = Array.FindIndex(cols, c => c.Trim().Equals(targetColName, StringComparison.OrdinalIgnoreCase));
            //if (idxHeader < 0 || idxTarget < 0) return map;

            string? line;
            while ((line = sr.ReadLine()) != null) {
                var parts = line.Split(',');
                map.Add(new ImportMapping() {
                    CourseDepartmentName = parts[0],
                    CourseName = string.IsNullOrEmpty(parts[1]) ? parts[0] : parts[1],
                    CourseId = int.Parse(parts[2])
                });
                //if (parts.Length <= Math.Max(idxHeader, idxTarget)) continue;
                //var key = parts[idxHeader].Trim();
                //var val = parts[idxTarget].Trim();

            }
            return map;
        }
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
            var memoryStream = new MemoryStream();
            //  FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            wb.Write(memoryStream);
            //return Json(ResponseStatus.OK, 1);
            return File(memoryStream.ToArray(), "application/octet-stream", "團體報名範例.xlsx");
        }

        #endregion

        #region 資料物件
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

        public class ImportMapping {

            /// <summary>
            /// 分校
            /// </summary>
            [Display(Name = "分校")]
            public string School { get; set; }

            /// <summary>
            /// 班別
            /// </summary>
            [Display(Name = "班別")]
            public string ClassType { get; set; }
            /// <summary>
            /// 班系
            /// </summary>
            [Display(Name = "班系")]
            public string CourseDepartmentName { get; set; }

            /// <summary>
            /// 課程
            /// </summary>
            [Display(Name = "課程")]
            public string CourseName { get; set; }

            /// <summary>
            /// 課程編號
            /// </summary>
            [Display(Name = "課程編號")]
            public int CourseId { get; set; }

            /// <summary>
            /// 人數
            /// </summary>
            [Display(Name = "人數")]
            public int Number { get; set; }
        }
        #endregion        
    }
}