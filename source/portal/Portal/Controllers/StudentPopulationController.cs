using DevExpress.Data.Browsing;
using DevExpress.XtraReports.Native;
using FluentFTP.Helpers;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using NuGet.Configuration;
using PHStatistics.Content;
using PHStatistics.Migrations;
using PHStatistics.Portal.Models;
using PHStatistics.Portal.Services;
using System;
using System.Collections.Generic;
using System.Framework;
using System.Framework.Application;
using System.Framework.EntityFrameworkCore;
using System.Framework.Globalization;
using System.Framework.Logging;
using System.Framework.Web;
using System.IO;
using System.Linq;
using static NPOI.HSSF.Util.HSSFColor;

namespace PHStatistics.Portal.Controllers {
    public class StudentPopulationController : MvcController<PortalUser, Model, Culture> {
        private readonly ReportExportService _reportExport;

        public StudentPopulationController(ReportExportService reportExport) : base("System") {
            _reportExport = reportExport;
        }

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

        private static readonly HashSet<int> _em1CourseIds = new() { 23, 24, 25, 26, 50, 51, 52, 53 };

        private static ClassType PsjColType(string code) => code switch {
            "MP" or "SP" => ClassType.Personal,
            "MS" or "SS" => ClassType.SubGroup,
            _ => ClassType.General,
        };

        private static ClassType AsColType(string code) => code switch {
            "EP" or "MP" or "SP" => ClassType.Personal,
            "ES" or "MS" or "SS" => ClassType.SubGroup,
            _ => ClassType.General,
        };

        [Authorize(typeof(PortalUser))]
        public IActionResult Index(string type) {
            DataContext dataContext = new DataContext();
            #region 新增學年度
            //DataContext dataContext = new DataContext();
            //DateTime begintime = new DateTime(2024, 7, 1);
            //DateTime endDateTime = begintime.AddYears(1);
            //int week = 1;
            //while (begintime < endDateTime) {
            //    SchoolYear newSchoolYear = new SchoolYear();
            //    newSchoolYear.Year = 113;
            //    newSchoolYear.ADYear = begintime.Year;
            //    newSchoolYear.Week = week;
            //    newSchoolYear.WeekStartDate = new DateTime(begintime.AddDays(-2).Year, begintime.AddDays(-2).Month, begintime.AddDays(-2).Day, 16, 0, 0);
            //    newSchoolYear.WeekEndDate = new DateTime(begintime.Year, begintime.Month, begintime.Day, 18, 0, 0);
            //    begintime = begintime.AddDays(7);
            //    week++;
            //    dataContext.SchoolYear.Add(newSchoolYear);
            //    dataContext.SaveChanges();
            //}
            #endregion
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).FirstOrDefault();
            List<SchoolAssignment> schools = Model.GetMemberSchool(User.Id);
            ViewBag.Schools = schools;
            ViewBag.CanEdit = schoolYear != null;
            ViewBag.Type = type;
            if (schools == null || schools.Count <= 0) {
                Redirect("StudentPopulation/CreatePopulation");
            }
            return View();
        }

        public IActionResult MainMenu(string type) {
            Logger.LogInformation("進入首頁(MainMenu)");
            try {
                Logger.LogInformation($"進入首頁確認使用者 User.IsGuest {User.IsGuest()} User.Id {User.Id}");
            }
            catch (Exception ex) {

            }
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

        public IActionResult Query() {
            Logger.LogInformation("進入首頁(MainMenu)");
            try {
                Logger.LogInformation($"進入首頁確認使用者 User.IsGuest {User.IsGuest()} User.Id {User.Id}");
            }
            catch (Exception ex) {

            }
            DataContext dataContext = new DataContext();
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).FirstOrDefault();
            List<SchoolAssignment> schools = Model.GetMemberSchool(User.Id);
            int[] years = dataContext.StudentPopulation.GroupBy(e => e.Year).Select(e => e.Key).ToArray();
            int[] weeks = dataContext.StudentPopulation.GroupBy(e => e.Week).Select(e => e.Key).OrderBy(e => e).ToArray();
            ViewBag.Schools = schools;
            ViewBag.Years = years;
            ViewBag.Weeks = weeks;
            ViewBag.SelectedYear = schoolYear;
            ViewBag.CanEdit = schoolYear != null;
            ViewBag.CanExportAll = User.HasPermission(SystemPermission.ViewAllSchools);
            ViewBag.Courses = Model.DataContext.Course.OrderBy(e => e.Ordinal).ToList();
            int memberSchool = schools.FirstOrDefault().School.Id;
            int year = years[years.Length - 1];
            int week = weeks[weeks.Length - 1];
            StudentPopulation studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.School.Id == memberSchool && e.Year == year && e.Week == week).FirstOrDefault();

            return View(studentPopulationData);
        }

        //百瀚
        [Authorize(typeof(PortalUser))]
        public IActionResult CreatePopulation(StudentPopulation data, int schoolId, string type) {
            if (Request.Method == "POST") {
                //進行人數表新增或更新
                SumPHPopulation(data.Id);
            }
            else {
                DataContext dataContext = new DataContext();
                StudentPopulationType populationType = new StudentPopulationType();
                if (type.Equals("PH")) {
                    populationType = StudentPopulationType.PH;
                }
                else if (type.Equals("PSJ")) {
                    populationType = StudentPopulationType.PSJ;
                }
                else if (type.Equals("PS")) {
                    populationType = StudentPopulationType.PS;
                }
                else if (type.Equals("GEPT")) {
                    populationType = StudentPopulationType.GEPT;
                }
                //取得維護年度週次
                DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
                School school = dataContext.School.Find(schoolId);
                SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).FirstOrDefault();
                SchoolYear lastschoolYear = dataContext.SchoolYear.Where(e => e.Id < schoolYear.Id).OrderByDescending(e => e.Id).FirstOrDefault();
                StudentPopulation lastWeekData = new StudentPopulation();
                lastWeekData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.School.Id == schoolId && e.Year == lastschoolYear.Year && e.Week == lastschoolYear.Week && e.Type == populationType).FirstOrDefault();

                StudentPopulation returnData = new StudentPopulation();
                List<Course> courses = Model.DataContext.Course.Include("Department").Where(e => e.Type == populationType).OrderBy(e => e.Ordinal).ToList();
                List<CourseDepartment> department = Model.DataContext.CourseDepartment.Where(e => e.Type == populationType).OrderBy(e => e.Ordinal).ToList();
                ViewBag.Year = schoolYear.Year;
                ViewBag.Week = schoolYear.Week;
                ViewBag.Courses = courses;
                ViewBag.CourseDepartment = department;
                ViewBag.SelectedYear = schoolYear;
                if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType)) {
                    returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType);
                    foreach (StudentPopulationItem sItem in returnData.Items) {
                        if (lastWeekData != null && lastWeekData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                            sItem.LastWeekNumber = lastWeekData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id).Number;
                        }
                    }
                    returnData.Type = StudentPopulationType.PH;
                    returnData.Name = string.Format("{0}第{1}週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    dataContext.SaveChanges();
                }
                else {
                    returnData = new StudentPopulation();
                    returnData.School = dataContext.School.Find(schoolId);
                    returnData.Year = schoolYear.Year.Value;
                    returnData.Week = schoolYear.Week.Value;
                    returnData.WeekDate = schoolYear.WeekStartDate;
                    returnData.Items = new List<StudentPopulationItem>();
                    returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
                    returnData.Type = StudentPopulationType.PH;
                    returnData.Name = string.Format("{0}第{1}週百瀚人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    dataContext.StudentPopulation.Add(returnData);
                    dataContext.SaveChanges();
                    //增加上週資料
                    if (lastWeekData != null && lastWeekData.Items != null && lastWeekData.Items.Count > 0) {
                        foreach (StudentPopulationItem lItem in lastWeekData.Items) {
                            if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == lItem.Class.Course.Id && e.Class.Type == lItem.Class.Type && e.StudentPopulation.Id == returnData.Id)) {
                                StudentPopulationItem item = new StudentPopulationItem();
                                Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == lItem.Class.Course.Id && e.Type == lItem.Class.Type);
                                if (classItem == null) {
                                    classItem = new Class() { SchoolId = schoolId, CourseId = lItem.Class.Course.Id, Name = lItem.Class.Course.Name, Type = lItem.Class.Type };
                                    dataContext.Class.Add(classItem);
                                    dataContext.SaveChanges();
                                }
                                item.Name = lItem.Class.Course.Name;
                                item.SchoolName = lItem.SchoolName;
                                item.Class = classItem;
                                item.Number = lItem.Number;
                                item.LastWeekNumber = lItem.Number;
                                item.StudentRemark = lItem.StudentRemark;
                                returnData.Items.Add(item);
                            }
                        }
                    }
                    dataContext.SaveChanges();
                    //增加固定總計項目                    
                    //英文個別指導 162 國文個別指導 172 course.Name.Equals("英文總班數統計")
                    foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PH).OrderBy(e => e.Ordinal).ToList()) {
                        if (course.Department.Name.Equals("英文個別指導") || course.Department.Name.Equals("國語文個別指導") || course.Name.Equals("英文合作開班人數合計") || course.Name.Equals("本週英語文新生") ||
                            course.Name.Equals("本週英語文總人數") || course.Name.Equals("上週英語文總人數") || course.Name.Equals("與上週相比") || course.Name.Equals("去年同期/比") ||
                            course.Name.Equals("本週英語文新生") || course.Name.Equals("本週英語文流失") || course.Name.Equals("國語文個別指導人數合計") ||
                            course.Name.Equals("本週國語文總人數") || course.Name.Equals("本週英語文流失") || course.Name.Equals("上週國語文總人數") ||
                            course.Name.Equals("本週國語文新生人數") || course.Name.Equals("本週國語文流失人數") || course.Name.Equals("本週總詢問(填單)人數") ||
                            course.Name.Equals("總人數")) {
                            if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.StudentPopulation.Id == returnData.Id)) {
                                StudentPopulationItem item = new StudentPopulationItem();
                                Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.General);
                                if (classItem == null) {
                                    classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.General };
                                    dataContext.Class.Add(classItem);
                                    dataContext.SaveChanges();
                                }
                                if (course.Department.Name.Equals("英文個別指導") || course.Department.Name.Equals("國語文個別指導") || course.Name.Equals("國語文個別指導人數合計")) {
                                    classItem.Type = ClassType.Personal;
                                }
                                else {
                                    classItem.Type = ClassType.General;
                                }
                                item.Name = course.Name;
                                item.SchoolName = school.Name;
                                item.Class = classItem;
                                item.Number = 0;
                                item.LastWeekNumber = 0;
                                returnData.Items.Add(item);
                            }
                        }
                        else {
                            //小
                            if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.StudentPopulation.Id == returnData.Id)) {
                                StudentPopulationItem item = new StudentPopulationItem();
                                Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.SubGroup);
                                if (classItem == null) {
                                    classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.SubGroup };
                                    dataContext.Class.Add(classItem);
                                    dataContext.SaveChanges();
                                }
                                item.Name = course.Name;
                                item.SchoolName = school.Name;
                                item.Class = classItem;
                                item.Number = 0;
                                item.LastWeekNumber = 0;
                                returnData.Items.Add(item);
                            }
                            //三
                            if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.V3 && e.StudentPopulation.Id == returnData.Id)) {
                                StudentPopulationItem item = new StudentPopulationItem();
                                Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.V3);
                                if (classItem == null) {
                                    classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.V3 };
                                    dataContext.Class.Add(classItem);
                                    dataContext.SaveChanges();
                                }
                                item.Name = course.Name;
                                item.SchoolName = school.Name;
                                item.Class = classItem;
                                item.Number = 0;
                                item.LastWeekNumber = 0;
                                returnData.Items.Add(item);
                            }
                        }
                    }
                    dataContext.SaveChanges();
                }
                return View(returnData);
            }
            return View();
        }
        //百倍速
        [Authorize(typeof(PortalUser))]
        public IActionResult CreatePSJPopulation(StudentPopulation data, int schoolId, string type) {
            DataContext dataContext = new DataContext();
            StudentPopulationType populationType = new StudentPopulationType();
            if (type.Equals("PH")) {
                populationType = StudentPopulationType.PH;
            }
            else if (type.Equals("PSJ")) {
                populationType = StudentPopulationType.PSJ;
            }
            else if (type.Equals("PS")) {
                populationType = StudentPopulationType.PS;
            }
            else if (type.Equals("GEPT")) {
                populationType = StudentPopulationType.GEPT;
            }
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).FirstOrDefault();
            SchoolYear lastschoolYear = dataContext.SchoolYear.Where(e => e.Id < schoolYear.Id).OrderByDescending(e => e.Id).FirstOrDefault();
            StudentPopulation lastWeekData = new StudentPopulation();
            lastWeekData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == lastschoolYear.Year && e.Week == lastschoolYear.Week && e.Type == populationType).FirstOrDefault();
            School school = dataContext.School.Find(schoolId);
            StudentPopulation returnData = new StudentPopulation();
            List<Course> courses = Model.DataContext.Course.Where(e => e.Type == populationType).OrderBy(e => e.Ordinal).ToList();
            List<CourseDepartment> department = Model.DataContext.CourseDepartment.Where(e => e.Type == populationType).OrderBy(e => e.Ordinal).ToList();
            ViewBag.Year = schoolYear.Year;
            ViewBag.Week = schoolYear.Week;
            ViewBag.Courses = courses;
            ViewBag.CourseDepartment = department;
            ViewBag.SelectedYear = schoolYear;
            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType)) {
                returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType);
                foreach (StudentPopulationItem sItem in returnData.Items) {
                    if (lastWeekData != null && lastWeekData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                        sItem.LastWeekNumber = lastWeekData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id).Number;
                    }
                }
                returnData.Type = StudentPopulationType.PSJ;
                returnData.Name = string.Format("{0}第{1}週百倍速人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                dataContext.SaveChanges();
            }
            else {
                returnData = new StudentPopulation();
                returnData.School = dataContext.School.Find(schoolId);
                returnData.Year = schoolYear.Year.Value;
                returnData.Week = schoolYear.Week.Value;
                returnData.WeekDate = schoolYear.WeekStartDate;
                returnData.Items = new List<StudentPopulationItem>();
                returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
                returnData.Type = StudentPopulationType.PSJ;
                returnData.Name = string.Format("{0}第{1}週百倍速人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                dataContext.StudentPopulation.Add(returnData);
                dataContext.SaveChanges();
                //增加上週資料
                if (lastWeekData != null && lastWeekData.Items != null && lastWeekData.Items.Count > 0) {
                    foreach (StudentPopulationItem lItem in lastWeekData.Items) {
                        if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == lItem.Class.Course.Id && e.Class.Type == lItem.Class.Type && e.StudentPopulation.Id == returnData.Id)) {
                            StudentPopulationItem item = new StudentPopulationItem();
                            Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == lItem.Class.Course.Id && e.Type == lItem.Class.Type);
                            if (classItem == null) {
                                classItem = new Class() { SchoolId = schoolId, CourseId = lItem.Class.Course.Id, Name = lItem.Class.Course.Name, Type = lItem.Class.Type };
                                dataContext.Class.Add(classItem);
                                dataContext.SaveChanges();
                            }
                            item.Name = lItem.Class.Course.Name;
                            item.SchoolName = lItem.Class.Course.Name;
                            item.Class = classItem;
                            item.Number = lItem.Number;
                            item.LastWeekNumber = lItem.Number;
                            returnData.Items.Add(item);
                        }
                    }
                }
                dataContext.SaveChanges();
                //增加固定總計項目
                foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PSJ).OrderBy(e => e.Ordinal).ToList()) {
                    if (course.Name.Equals("本週數學總人數合計") || course.Name.Equals("本週理化總人數合計")) {
                        if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.General && e.StudentPopulation.Id == returnData.Id)) {
                            StudentPopulationItem item = new StudentPopulationItem();
                            Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.General);
                            if (classItem == null) {
                                classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.General };
                                dataContext.Class.Add(classItem);
                                dataContext.SaveChanges();
                            }
                            item.Name = course.Name;
                            item.SchoolName = school.Name;
                            item.Class = classItem;
                            item.Number = 0;
                            item.LastWeekNumber = 0;
                            returnData.Items.Add(item);
                        }
                    }
                    else {
                        //EM1
                        if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Personal && e.StudentPopulation.Id == returnData.Id)) {
                            StudentPopulationItem item = new StudentPopulationItem();
                            Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Personal);
                            if (classItem == null) {
                                classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Personal };
                                dataContext.Class.Add(classItem);
                                dataContext.SaveChanges();
                            }
                            item.Name = course.Name;
                            item.SchoolName = school.Name;
                            item.Class = classItem;
                            item.Number = 0;
                            item.LastWeekNumber = 0;
                            returnData.Items.Add(item);
                        }
                        //小組班
                        if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.SubGroup && e.StudentPopulation.Id == returnData.Id)) {
                            StudentPopulationItem item = new StudentPopulationItem();
                            Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.SubGroup);
                            if (classItem == null) {
                                classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.SubGroup };
                                dataContext.Class.Add(classItem);
                                dataContext.SaveChanges();
                            }
                            item.Name = course.Name;
                            item.SchoolName = school.Name;
                            item.Class = classItem;
                            item.Number = 0;
                            item.LastWeekNumber = 0;
                            returnData.Items.Add(item);
                        }
                    }
                }
                dataContext.SaveChanges();               
            }
            if (Request.Method == "POST") {
                //進行人數表新增或更新
                if (data.Items != null && data.Items.Count > 0) {
                    foreach (StudentPopulationItem item in data.Items) {
                        if (item.Class != null && item.Class.Id > 0) {
                            StudentPopulationItem existItem = returnData.Items.FirstOrDefault(e => e.Class.Id == item.Class.Id);
                            if (existItem != null) {
                                existItem.Number = item.Number;
                                existItem.LastWeekNumber = item.LastWeekNumber;
                            }
                            else {
                                item.StudentPopulation = returnData;
                                returnData.Items.Add(item);
                            }
                        }
                    }
                    dataContext.SaveChanges();
                }
            }
            return View(returnData);
        }

        //英檢
        [Authorize(typeof(PortalUser))]
        public IActionResult CreateGeptPopulation(StudentPopulation data, int schoolId, string type) {
            DataContext dataContext = new DataContext();
            StudentPopulationType populationType = new StudentPopulationType();
            if (type.Equals("PH")) {
                populationType = StudentPopulationType.PH;
            }
            else if (type.Equals("PSJ")) {
                populationType = StudentPopulationType.PSJ;
            }
            else if (type.Equals("PS")) {
                populationType = StudentPopulationType.PS;
            }
            else if (type.Equals("Gept")) {
                populationType = StudentPopulationType.GEPT;
            }
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).FirstOrDefault();
            SchoolYear lastschoolYear = dataContext.SchoolYear.Where(e => e.Id < schoolYear.Id).OrderByDescending(e => e.Id).FirstOrDefault();
            StudentPopulation lastWeekData = new StudentPopulation();
            lastWeekData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == lastschoolYear.Year && e.Week == lastschoolYear.Week && e.Type == populationType).FirstOrDefault();

            StudentPopulation returnData = new StudentPopulation();
            List<Course> courses = Model.DataContext.Course.Where(e => e.Type == populationType).OrderBy(e => e.Ordinal).ToList();
            List<CourseDepartment> department = Model.DataContext.CourseDepartment.Where(e => e.Type == populationType).OrderBy(e => e.Ordinal).ToList();
            ViewBag.Year = schoolYear.Year;
            ViewBag.Week = schoolYear.Week;
            ViewBag.Courses = courses;
            ViewBag.CourseDepartment = department;
            ViewBag.SelectedYear = schoolYear;
            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType)) {
                returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType);
                foreach (StudentPopulationItem sItem in returnData.Items) {
                    if (lastWeekData != null && lastWeekData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                        sItem.LastWeekNumber = lastWeekData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id).Number;
                    }
                }
                returnData.Type = StudentPopulationType.GEPT;
                returnData.Name = string.Format("{0}第{1}週英檢人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                dataContext.SaveChanges();
            }
            else {
                returnData = new StudentPopulation();
                returnData.School = dataContext.School.Find(schoolId);
                returnData.Year = schoolYear.Year.Value;
                returnData.Week = schoolYear.Week.Value;
                returnData.WeekDate = schoolYear.WeekStartDate;
                returnData.Items = new List<StudentPopulationItem>();
                returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
                returnData.Type = StudentPopulationType.GEPT;
                returnData.Name = string.Format("{0}第{1}週英檢人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                dataContext.StudentPopulation.Add(returnData);
                dataContext.SaveChanges();
                //增加上週資料
                if (lastWeekData != null && lastWeekData.Items != null && lastWeekData.Items.Count > 0) {
                    foreach (StudentPopulationItem lItem in lastWeekData.Items) {
                        if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == lItem.Class.Course.Id && e.Class.Type == lItem.Class.Type && e.StudentPopulation.Id == returnData.Id)) {
                            StudentPopulationItem item = new StudentPopulationItem();
                            Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == lItem.Class.Course.Id && e.Type == lItem.Class.Type);
                            if (classItem == null) {
                                classItem = new Class() { SchoolId = schoolId, CourseId = lItem.Class.Course.Id, Name = lItem.Class.Course.Name, Type = lItem.Class.Type };
                                dataContext.Class.Add(classItem);
                                dataContext.SaveChanges();
                            }
                            item.Name = lItem.Class.Course.Name;
                            item.SchoolName = lItem.Class.Course.Name;
                            item.Class = classItem;
                            item.Number = lItem.Number;
                            item.LastWeekNumber = lItem.Number;
                            returnData.Items.Add(item);
                        }
                    }
                }
                dataContext.SaveChanges();
                //增加固定總計項目
                foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.GEPT).OrderBy(e => e.Ordinal).ToList()) {
                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.General && e.StudentPopulation.Id == returnData.Id)) {
                        StudentPopulationItem item = new StudentPopulationItem();
                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.General);
                        if (classItem == null) {
                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.General };
                            dataContext.Class.Add(classItem);
                            dataContext.SaveChanges();
                        }
                        item.Name = course.Name;
                        item.SchoolName = course.Name;
                        item.Class = classItem;
                        item.Number = 0;
                        item.LastWeekNumber = 0;
                        returnData.Items.Add(item);
                    }
                }
                dataContext.SaveChanges();
            }
            if (Request.Method == "POST") {
                //進行人數表新增或更新
            }
            return View(returnData);
        }
        //百世
        [Authorize(typeof(PortalUser))]
        public IActionResult CreatePSPopulation(StudentPopulation data, int schoolId, string type) {
            DataContext dataContext = new DataContext();
            StudentPopulationType populationType = new StudentPopulationType();
            if (type.Equals("PH")) {
                populationType = StudentPopulationType.PH;
            }
            else if (type.Equals("PSJ")) {
                populationType = StudentPopulationType.PSJ;
            }
            else if (type.Equals("PS")) {
                populationType = StudentPopulationType.PS;
            }
            else if (type.Equals("GEPT")) {
                populationType = StudentPopulationType.GEPT;
            }
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).FirstOrDefault();
            SchoolYear lastschoolYear = dataContext.SchoolYear.Where(e => e.Id < schoolYear.Id).OrderByDescending(e => e.Id).FirstOrDefault();
            StudentPopulation lastWeekData = new StudentPopulation();
            lastWeekData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == lastschoolYear.Year && e.Week == lastschoolYear.Week && e.Type == populationType).FirstOrDefault();

            StudentPopulation returnData = new StudentPopulation();
            List<Course> courses = Model.DataContext.Course.Where(e => e.Type == populationType).OrderBy(e => e.Ordinal).ToList();
            List<CourseDepartment> department = Model.DataContext.CourseDepartment.Where(e => e.Type == populationType).OrderBy(e => e.Ordinal).ToList();
            ViewBag.Year = schoolYear.Year;
            ViewBag.Week = schoolYear.Week;
            ViewBag.Courses = courses;
            ViewBag.CourseDepartment = department;
            ViewBag.SelectedYear = schoolYear;
            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType)) {
                returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType);
                foreach (StudentPopulationItem sItem in returnData.Items) {
                    if (lastWeekData != null && lastWeekData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                        sItem.LastWeekNumber = lastWeekData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id).Number;
                    }
                }
                returnData.Type = StudentPopulationType.PS;
                returnData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                dataContext.SaveChanges();
            }
            else {
                returnData = new StudentPopulation();
                returnData.School = dataContext.School.Find(schoolId);
                returnData.Year = schoolYear.Year.Value;
                returnData.Week = schoolYear.Week.Value;
                returnData.WeekDate = schoolYear.WeekStartDate;
                returnData.Items = new List<StudentPopulationItem>();
                returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
                returnData.Type = StudentPopulationType.PS;
                returnData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                dataContext.StudentPopulation.Add(returnData);
                dataContext.SaveChanges();
                //增加上週資料
                if (lastWeekData != null && lastWeekData.Items != null && lastWeekData.Items.Count > 0) {
                    foreach (StudentPopulationItem lItem in lastWeekData.Items) {
                        if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == lItem.Class.Course.Id && e.Class.Type == lItem.Class.Type && e.StudentPopulation.Id == returnData.Id)) {
                            StudentPopulationItem item = new StudentPopulationItem();
                            Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == lItem.Class.Course.Id && e.Type == lItem.Class.Type);
                            if (classItem == null) {
                                classItem = new Class() { SchoolId = schoolId, CourseId = lItem.Class.Course.Id, Name = lItem.Class.Course.Name, Type = lItem.Class.Type };
                                dataContext.Class.Add(classItem);
                                dataContext.SaveChanges();
                            }
                            item.Name = lItem.Class.Course.Name;
                            item.SchoolName = lItem.Class.Course.Name;
                            item.Class = classItem;
                            item.Number = lItem.Number;
                            item.LastWeekNumber = lItem.Number;
                            returnData.Items.Add(item);
                        }
                    }
                }
                dataContext.SaveChanges();
                //增加固定總計項目
                foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PS).OrderBy(e => e.Ordinal).ToList()) {
                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.General && e.StudentPopulation.Id == returnData.Id)) {
                        StudentPopulationItem item = new StudentPopulationItem();
                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.General);
                        if (classItem == null) {
                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.General };
                            dataContext.Class.Add(classItem);
                            dataContext.SaveChanges();
                        }
                        item.Name = course.Name;
                        item.SchoolName = course.Name;
                        item.Class = classItem;
                        item.Number = 0;
                        item.LastWeekNumber = 0;
                        returnData.Items.Add(item);
                    }
                }
                dataContext.SaveChanges();
            }
            if (Request.Method == "POST") {
                //進行人數表新增或更新
            }
            return View(returnData);
        }

        //課輔
        [Authorize(typeof(PortalUser))]
        public IActionResult CreateASPopulation(StudentPopulation data, int schoolId, string type) {
            DataContext dataContext = new DataContext();
            StudentPopulationType populationType = new StudentPopulationType();
            if (type.Equals("PH")) {
                populationType = StudentPopulationType.PH;
            }
            else if (type.Equals("PSJ")) {
                populationType = StudentPopulationType.PSJ;
            }
            else if (type.Equals("PS")) {
                populationType = StudentPopulationType.PS;
            }
            else if (type.Equals("GEPT")) {
                populationType = StudentPopulationType.GEPT;
            }
            else if (type.Equals("AS")) {
                populationType = StudentPopulationType.AfterSchool;
            }
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).FirstOrDefault();
            SchoolYear lastschoolYear = dataContext.SchoolYear.Where(e => e.Id < schoolYear.Id).OrderByDescending(e => e.Id).FirstOrDefault();
            StudentPopulation lastWeekData = new StudentPopulation();
            lastWeekData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == lastschoolYear.Year && e.Week == lastschoolYear.Week && e.Type == populationType).FirstOrDefault();

            StudentPopulation returnData = new StudentPopulation();
            List<Course> courses = Model.DataContext.Course.Where(e => e.Type == populationType).OrderBy(e => e.Ordinal).ToList();
            List<CourseDepartment> department = Model.DataContext.CourseDepartment.Where(e => e.Type == populationType).OrderBy(e => e.Ordinal).ToList();
            ViewBag.Year = schoolYear.Year;
            ViewBag.Week = schoolYear.Week;
            ViewBag.Courses = courses;
            ViewBag.CourseDepartment = department;
            ViewBag.SelectedYear = schoolYear;
            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType)) {
                returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType);
                foreach (StudentPopulationItem sItem in returnData.Items) {
                    if (lastWeekData != null && lastWeekData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                        sItem.LastWeekNumber = lastWeekData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id).Number;
                    }
                }
                returnData.Type = StudentPopulationType.AfterSchool;
                returnData.Name = string.Format("{0}第{1}週課輔人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                dataContext.SaveChanges();
            }
            else {
                returnData = new StudentPopulation();
                returnData.School = dataContext.School.Find(schoolId);
                returnData.Year = schoolYear.Year.Value;
                returnData.Week = schoolYear.Week.Value;
                returnData.WeekDate = schoolYear.WeekStartDate;
                returnData.Items = new List<StudentPopulationItem>();
                returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
                returnData.Type = StudentPopulationType.AfterSchool;
                returnData.Name = string.Format("{0}第{1}週課輔人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                dataContext.StudentPopulation.Add(returnData);
                dataContext.SaveChanges();
                //增加上週資料
                if (lastWeekData != null && lastWeekData.Items != null && lastWeekData.Items.Count > 0) {
                    foreach (StudentPopulationItem lItem in lastWeekData.Items) {
                        if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == lItem.Class.Course.Id && e.Class.Type == lItem.Class.Type && e.StudentPopulation.Id == returnData.Id)) {
                            StudentPopulationItem item = new StudentPopulationItem();
                            Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == lItem.Class.Course.Id && e.Type == lItem.Class.Type);
                            if (classItem == null) {
                                classItem = new Class() { SchoolId = schoolId, CourseId = lItem.Class.Course.Id, Name = lItem.Class.Course.Name, Type = lItem.Class.Type };
                                dataContext.Class.Add(classItem);
                                dataContext.SaveChanges();
                            }
                            item.Name = lItem.Class.Course.Name;
                            item.SchoolName = lItem.Class.Course.Name;
                            item.Class = classItem;
                            item.Number = lItem.Number;
                            item.LastWeekNumber = lItem.Number;
                            returnData.Items.Add(item);
                        }
                    }
                }
                dataContext.SaveChanges();
                //增加固定總計項目
                foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PS).OrderBy(e => e.Ordinal).ToList()) {
                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.General && e.StudentPopulation.Id == returnData.Id)) {
                        StudentPopulationItem item = new StudentPopulationItem();
                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.General);
                        if (classItem == null) {
                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.General };
                            dataContext.Class.Add(classItem);
                            dataContext.SaveChanges();
                        }
                        item.Name = course.Name;
                        item.SchoolName = course.Name;
                        item.Class = classItem;
                        item.Number = 0;
                        item.LastWeekNumber = 0;
                        returnData.Items.Add(item);
                    }
                }
                dataContext.SaveChanges();
            }
            if (Request.Method == "POST") {
                //進行人數表新增或更新
            }
            return View(returnData);
        }


        [Authorize(typeof(PortalUser))]
        [HttpPost("AddNewClass")]
        // data: { 'schoolId': schoolId, 'courseId': newCourses.value, 'week': week, 'year': year, 'newClassType': newClassType, 'newClassName': newClassName, 'newNumber': newNumber, 'newStudentremark':newStudentremark },
        public IActionResult AddNewClass(int courseId, int schoolId, int year, int week, string[][] itemArr, int newClassType, string newClassName, int newNumber, string newStudentremark, string type) {
            DataContext dataContext = new DataContext();
            var seleceedType = type switch {
                "PH" => StudentPopulationType.PH,
                "PS" => StudentPopulationType.PS,
                "PSJ" => StudentPopulationType.PSJ,
                "Gept" => StudentPopulationType.GEPT,
                "AS" => StudentPopulationType.AfterSchool,
                _ => StudentPopulationType.PH
            };
            StudentPopulation studentPopulationData = Model.GetStudentPopulation(schoolId, year, week, seleceedType);
            List<Course> courses = Model.DataContext.Course.Where(e => e.Type == studentPopulationData.Type).OrderBy(e => e.Ordinal).ToList();
            ViewBag.Courses = courses;
            if (studentPopulationData.Status != StudentPopulationStatus.Documented) {
                var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == studentPopulationData.Id).FirstOrDefault();
                return PartialView("PopulationPartialView", lockedData);
            }
            //更新人數表資料
            try {
                foreach (string[] updateItem in itemArr) {
                    int classId = int.Parse(updateItem[1]);
                    if (studentPopulationData.Items.Any(e => e.Class.Id == classId)) {
                        long itemId = studentPopulationData.Items.FirstOrDefault(e => e.Class.Id == classId).Id;
                        StudentPopulationItem sItem = dataContext.StudentPopulationItem.Find(itemId);
                        sItem.Number = int.Parse((string)updateItem[2]);
                        dataContext.SaveChanges();
                    }
                    else {
                        continue;
                    }
                }
            }
            catch (Exception ex) {
                string e = ex.Message;
            }

            if (seleceedType == StudentPopulationType.PH) {
                //進行百瀚人數表異動
                try {
                    Course course = dataContext.Course.Find(courseId);
                    //新增班級
                    //取得目前班級數
                    Class newClass = new Class();
                    try {
                        int classCount = studentPopulationData.Items.Count(e => e.Class.Course.Id == course.Id);
                        newClass.Course = null;
                        newClass.CourseId = course.Id;
                        newClass.SchoolId = schoolId;
                        if (newClassType == 0) {
                            newClass.Type = ClassType.Personal;
                        }
                        else if (newClassType == 1) {
                            newClass.Type = ClassType.Personal;
                        }
                        else if (newClassType == 2) {
                            newClass.Type = ClassType.V2;
                        }
                        else if (newClassType == 3) {
                            newClass.Type = ClassType.V3;
                        }
                        else if (newClassType == 4) {
                            newClass.Type = ClassType.SubGroup;
                        }
                        newClass.Name = string.IsNullOrEmpty(newClassName) ? string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00")) : newClassName;
                        newClass.Remark = newStudentremark;
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
                    addItem.Number = newNumber;
                    addItem.SchoolName = newClassName;
                    addItem.LastWeekNumber = 0;
                    addItem.StudentPopulation = null;
                    addItem.StudentPopulationId = studentPopulationData.Id;
                    addItem.StudentRemark = newStudentremark;
                    dataContext.StudentPopulationItem.Add(addItem);
                    dataContext.SaveChanges();
                    SumPHPopulation(studentPopulationData.Id);
                }
                catch (Exception ex) {
                    string e = ex.Message;
                }
            }
            else if (seleceedType == StudentPopulationType.PSJ) {
                try {
                    Course course = dataContext.Course.Find(courseId);
                    //新增班級
                    //取得目前班級數
                    Class newClass = new Class();
                    try {
                        int classCount = studentPopulationData.Items.Count(e => e.Class.Course.Id == course.Id);
                        newClass.Course = null;
                        newClass.CourseId = course.Id;
                        newClass.SchoolId = schoolId;
                        if (newClassType == 0) {
                            newClass.Type = ClassType.Group;
                        }
                        else if (newClassType == 1) {
                            newClass.Type = ClassType.Personal;
                        }
                        else if (newClassType == 2) {
                            newClass.Type = ClassType.V2;
                        }
                        else if (newClassType == 3) {
                            newClass.Type = ClassType.V3;
                        }
                        else if (newClassType == 4) {
                            newClass.Type = ClassType.SubGroup;
                        }
                        else if (newClassType == 5) {
                            newClass.Type = ClassType.General;
                        }
                        else {
                            newClass.Type = ClassType.Group;
                        }
                        newClass.Name = string.IsNullOrEmpty(newClassName) ? string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00")) : newClassName;
                        newClass.Remark = newStudentremark;
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
                    addItem.Number = newNumber;
                    addItem.SchoolName = newClassName;
                    addItem.LastWeekNumber = 0;
                    addItem.StudentPopulation = null;
                    addItem.StudentPopulationId = studentPopulationData.Id;
                    addItem.StudentRemark = newStudentremark;
                    dataContext.StudentPopulationItem.Add(addItem);
                    dataContext.SaveChanges();
                    //進行加總
                    SumPHPopulation(studentPopulationData.Id);
                }
                catch (Exception ex) {
                    string e = ex.Message;
                }
            }
            else if (seleceedType == StudentPopulationType.GEPT) {
                try {
                    Course course = dataContext.Course.Find(courseId);
                    //新增班級
                    //取得目前班級數
                    Class newClass = new Class();
                    try {
                        int classCount = studentPopulationData.Items.Count(e => e.Class.Course.Id == course.Id);
                        newClass.Course = null;
                        newClass.CourseId = course.Id;
                        newClass.SchoolId = schoolId;
                        newClass.Type = ClassType.General;
                        newClass.Name = string.IsNullOrEmpty(newClassName) ? string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00")) : newClassName;
                        newClass.Remark = newStudentremark;
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
                    addItem.Number = newNumber;
                    addItem.SchoolName = newClassName;
                    addItem.LastWeekNumber = 0;
                    addItem.StudentPopulation = null;
                    addItem.StudentPopulationId = studentPopulationData.Id;
                    addItem.StudentRemark = newStudentremark;
                    dataContext.StudentPopulationItem.Add(addItem);
                    dataContext.SaveChanges();
                    //進行加總
                    SumPHPopulation(studentPopulationData.Id);
                }
                catch (Exception ex) {
                    string e = ex.Message;
                }
            }
            else if (seleceedType == StudentPopulationType.PS) {
                try {
                    Course course = dataContext.Course.Find(courseId);
                    //新增班級
                    //取得目前班級數
                    Class newClass = new Class();
                    try {
                        int classCount = studentPopulationData.Items.Count(e => e.Class.Course.Id == course.Id);
                        newClass.Course = null;
                        newClass.CourseId = course.Id;
                        newClass.SchoolId = schoolId;
                        newClass.Type = ClassType.General;
                        newClass.Name = string.IsNullOrEmpty(newClassName) ? string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00")) : newClassName;
                        newClass.Remark = newStudentremark;
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
                    addItem.Number = newNumber;
                    addItem.SchoolName = newClassName;
                    addItem.LastWeekNumber = 0;
                    addItem.StudentPopulation = null;
                    addItem.StudentPopulationId = studentPopulationData.Id;
                    addItem.StudentRemark = newStudentremark;
                    dataContext.StudentPopulationItem.Add(addItem);
                    dataContext.SaveChanges();
                    //進行加總
                    SumPHPopulation(studentPopulationData.Id);
                }
                catch (Exception ex) {
                    string e = ex.Message;
                }
            }
            else if (seleceedType == StudentPopulationType.AfterSchool) {
                try {
                    Course course = dataContext.Course.Find(courseId);
                    //新增班級
                    //取得目前班級數
                    Class newClass = new Class();
                    try {
                        int classCount = studentPopulationData.Items.Count(e => e.Class.Course.Id == course.Id);
                        newClass.Course = null;
                        newClass.CourseId = course.Id;
                        newClass.SchoolId = schoolId;
                        newClass.Type = ClassType.General;
                        newClass.Name = string.IsNullOrEmpty(newClassName) ? string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00")) : newClassName;
                        newClass.Remark = newStudentremark;
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
                    addItem.Number = newNumber;
                    addItem.SchoolName = newClassName;
                    addItem.LastWeekNumber = 0;
                    addItem.StudentPopulation = null;
                    addItem.StudentPopulationId = studentPopulationData.Id;
                    addItem.StudentRemark = newStudentremark;
                    dataContext.StudentPopulationItem.Add(addItem);
                    dataContext.SaveChanges();
                    //進行加總
                    SumPHPopulation(studentPopulationData.Id);
                }
                catch (Exception ex) {
                    string e = ex.Message;
                }
            }
            dataContext.ChangeTracker.Clear();
            //var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week && e.Type == seleceedType).FirstOrDefault();
            var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == studentPopulationData.Id).FirstOrDefault();
            return PartialView("PopulationPartialView", returnData);
        }

        public IActionResult RemoveClassItem(long sId) {
            DataContext dataContext = new DataContext();
            long spId = 0;
            try {
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("StudentPopulation").Where(e => e.Id == sId).FirstOrDefault();
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
                ViewBag.Courses = courses;
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulationId).FirstOrDefault();
                    return PartialView("PopulationPartialView", lockedData);
                }
                if (item != null) {
                    spId = item.StudentPopulationId;
                    dataContext.StudentPopulationItem.Remove(item);
                    dataContext.SaveChanges();
                    //進行加總
                    SumPHPopulation(spId);
                }
                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == spId).FirstOrDefault();
                return PartialView("PopulationPartialView", returnData);
            }
            catch (Exception ex) {
                return PartialView("PopulationPartialView", new StudentPopulation());
            }
        }

        public IActionResult UpdateClassItem(long sId, int? number, string studentRemark = null) {
            DataContext dataContext = new DataContext();
            try {
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("Class.Course.Department").Include("StudentPopulation").Where(e => e.Id == sId).FirstOrDefault();
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
                ViewBag.Courses = courses;
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                    return PartialView("PopulationPartialView", lockedData);
                }
                if (item != null) {
                    if (number.HasValue) {
                        if (item.Class.Course.Name.Equals("本週英語文新生") || item.Class.Course.Name.Equals("本週英語文流失") ||
                            item.Class.Course.Name.Equals("本週國語文新生人數") || item.Class.Course.Name.Equals("本週國語文流失人數")) {
                            item.IsManual = true;
                        }
                        item.Number = number.Value;
                    }
                    if (studentRemark != null) {
                        item.StudentRemark = studentRemark;
                    }
                    dataContext.StudentPopulationItem.Update(item);
                    dataContext.SaveChanges();
                    //進行加總
                    if (number.HasValue) {
                        SumPHPopulation(item.StudentPopulation.Id);
                    }
                }
                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                return PartialView("PopulationPartialView", returnData);
            }
            catch (Exception ex) {
                return PartialView("PopulationPartialView", new StudentPopulation());
            }
        }

        [HttpPost]
        public IActionResult UpdateClassDetail(long populationId, int classId, string name, int classType) {
            DataContext dataContext = new DataContext();
            var population = dataContext.StudentPopulation
                .Include("Items.Class")
                .FirstOrDefault(p => p.Id == populationId);

            if (population == null)
                return Json(new { success = false, message = "找不到人數表" });

            if (population.Status != StudentPopulationStatus.Documented)
                return Json(new { success = false, message = "人數表狀態不允許修改" });

            var cls = dataContext.Class.FirstOrDefault(c => c.Id == classId);
            if (cls == null)
                return Json(new { success = false, message = "找不到班級" });

            bool classTypeChanged = (int)cls.Type != classType;

            if (!string.IsNullOrWhiteSpace(name))
                cls.Name = name;

            cls.Type = (ClassType)classType;
            dataContext.SaveChanges();

            if (classTypeChanged)
                SumPHPopulation(populationId);

            return Json(new { success = true });
        }

        [Authorize(typeof(PortalUser))]
        [HttpPost("ConfirmPopulation")]
        public IActionResult ConfirmPopulation(long populationId) {
            DataContext dataContext = new DataContext();
            StudentPopulation sp = dataContext.StudentPopulation.Find(populationId);
            if (sp == null)
                return Json(new { success = false, message = "找不到人數表資料" });
            if (sp.Status != StudentPopulationStatus.Documented)
                return Json(new { success = false, message = "人數表已送出，無法重複確認" });
            sp.Status = StudentPopulationStatus.Pending;
            sp.SubmitterTime = DateTime.UtcNow.ToTaipeiTime();
            sp.SubmitterId = Guid.Parse(User.Id);
            dataContext.SaveChanges();
            return Json(new { success = true });
        }

        //

        /// <summary>
        /// 區域取得考場
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        [HttpGet("GetCourses")]
        public IActionResult GetCourses(int depId) {
            try {
                var courses = Model.DataContext.Course.Include("Department").Where(e => e.Department.Id == depId && e.Published && !e.IsSum).OrderBy(e => e.Ordinal).ToList();
                return Json(new { success = true, data = courses });
            }
            catch (FrameworkException fe) {
                return Json(new { success = false, message = fe.Message.ToString() });
            }
            catch (Exception e) {
                Logger.LogError(e.Message);
                return Json(new { success = false, message = "系統忙碌中，請稍後再試" });
            }
        }

        /// <summary>
        /// 取得班別
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        [HttpGet("GetClassType")]
        public IActionResult GetClassType(int coursesId) {
            try {
                var courses = Model.DataContext.Course.Include("Department").Where(e => e.Id == coursesId).FirstOrDefault();
                return Json(new { success = true, data = courses });
            }
            catch (FrameworkException fe) {
                return Json(new { success = false, message = fe.Message.ToString() });
            }
            catch (Exception e) {
                Logger.LogError(e.Message);
                return Json(new { success = false, message = "系統忙碌中，請稍後再試" });
            }
        }

        [Authorize(typeof(PortalUser))]
        [HttpPost("QueryPopulationPartial")]
        // data: { 'schoolId': schoolId, 'courseId': newCourses.value, 'week': week, 'year': year, 'newClassType': newClassType, 'newClassName': newClassName, 'newNumber': newNumber, 'newStudentremark':newStudentremark },
        public IActionResult QueryPopulationPartial(int schoolId, int year, int week, string reportType) {

            List<Course> courses = Model.DataContext.Course.OrderBy(e => e.Ordinal).ToList();
            ViewBag.Courses = courses;
            DataContext dataContext = new DataContext();
            var seleceedType = reportType switch {
                "PH" => StudentPopulationType.PH,
                "PS" => StudentPopulationType.PS,
                "GEPT" => StudentPopulationType.GEPT,
                "PSJ" => StudentPopulationType.PSJ,
                "AS" => StudentPopulationType.AfterSchool,
                _ => StudentPopulationType.PH
            };


            StudentPopulation studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week && e.Type == seleceedType).FirstOrDefault();
            return PartialView("QueryPopulationPartialView", studentPopulationData);
        }
        public StudentPopulation SumPHPopulation(long spId) {
            DataContext dataContext = new DataContext();
            dataContext.ChangeTracker.Clear();
            //取得本週資料
            StudentPopulation studentPopulationData = Model.GetStudentPopulationById(spId);
            //加總說明
            //新增英文合計
            //國小班 P1~P6 SAT Juior AB  
            //國中班 國一準特/特訓 國二準特/特訓 國三準特/特訓 海外特訓班 TOEFL	SSAT	PSAT
            //Elite/sat班系 直接進總人數
            //高中合計 高中小組班 高一 高二 高三
            //總班數 計算到高中的各別班數(不含EM1)
            //EM1合計 (EM1)國小	(EM1)國中	(EM1)高一&高二	(EM1)高三
            //本週英語文總人數 所有英文班級人數
            //上週英語文總人數 上週英文
            //與上週相比  本週英語文總人數 - 上週英語文總人數
            //去年同期 / 比 本週總人數 - 去年同週次總人數
            //本週英語文新生 分校自填
            //本週英語文流失 分校自填

            //取得上周及目前資料
            int eNCount = 0;
            int eNLastCount = 0;
            int chCount = 0;
            int chLastCount = 0;
            //StudentPopulation lastStudentPopulationData = dataContext.StudentPopulation.Where(e => e.School.Id == studentPopulationData.School.Id && e.Week < studentPopulationData.Week && e.Type == studentPopulationData.Type).OrderByDescending(e => e.Id).FirstOrDefault();
            //if (lastStudentPopulationData != null) {
            //    lastStudentPopulationData.Items = dataContext.StudentPopulationItem.Include("Class.Course.Department").Where(e => e.StudentPopulationId == lastStudentPopulationData.Id).ToList();
            //}

            //班系加總
            var classGroup = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.IsSum).ToList();
            int addEnStudent = 0;
            int lostEnStudent = 0;
            int addChStudent = 0;
            int lostChStudent = 0;
            foreach (var group in classGroup) {
                try {

                    if (studentPopulationData.Type == StudentPopulationType.PH) {
                        if (group.Class.Course.Name.Equals("本週總詢問人數") || group.Class.Course.Name.Equals("本週總詢問(填單)人數")) {
                            continue;
                        }
                        //取得相同班系及班型的班級
                        List<StudentPopulationItem> classItems = new List<StudentPopulationItem>();
                        if (group.Name.Contains("個別指導"))
                        {
                            classItems = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id  && !e.Class.Course.IsSum).ToList();
                        }
                        else {
                            classItems = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).ToList();
                        }
                        group.Number = classItems.Sum(e => e.Number);
                        int lastWeekNumber = classItems.Sum(e => e.LastWeekNumber);


                        if (group.Class.Course.Department.Subject == CourseSubject.English) {
                            if (group.Number - lastWeekNumber > 0) {
                                addEnStudent = addEnStudent + (group.Number - lastWeekNumber);
                            }
                            else if (group.Number - lastWeekNumber < 0) {
                                lostEnStudent = lostEnStudent + (lastWeekNumber - group.Number);
                            }
                        }
                        else if (group.Class.Course.Department.Subject == CourseSubject.Chinese) {
                            if (group.Number - lastWeekNumber > 0) {
                                addChStudent = addChStudent + (group.Number - lastWeekNumber);
                            }
                            else if (group.Number - lastWeekNumber < 0) {
                                lostChStudent = lostChStudent + (lastWeekNumber - group.Number);
                            }
                        }
                        if (group.Class.Course.Name.Equals("英文個別指導人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("英文個別指導")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("國語文個別指導人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("國語文個別指導")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("英文合作開班人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("英文合作開班")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("國語文合作開班人數合計")) {
                            CourseDepartment courseDepartment = dataContext.CourseDepartment.Where(e => e.Name.Equals("國語文合作開班")).FirstOrDefault();
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == courseDepartment.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        //else if (group.Class.Course.Name.IndexOf("與上週相比") >= 0) {
                        //    string subjectName = group.Class.Course.Name.Replace("與上週相比", "").Replace("本週", "");
                        //    int lastWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.LastWeekNumber);
                        //    int thisWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                        //    group.Number = thisWeekNum - lastWeekNum;
                        //}
                    }
                    else if (studentPopulationData.Type == StudentPopulationType.PSJ) {
                        if (group.Class.Course.Name.Equals("本周數學人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("數學班") && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("本週數學總人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("數學班") && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("本周理化人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("理化班") && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("本週理化總人數合計")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Name.Equals("理化班") && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.IndexOf("與上週相比") >= 0) {
                            string subjectName = group.Class.Course.Name.Replace("與上週相比", "").Replace("本週", "");
                            int lastWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.LastWeekNumber);
                            int thisWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                            group.Number = thisWeekNum - lastWeekNum;

                        }
                        else if (group.Class.Course.Name.IndexOf("流失人數") >= 0) {
                            if (!group.IsManual) {
                                string subjectName = group.Class.Course.Name.Replace("數學文流失人數", "").Replace("數學流失人數", "").Replace("理化文流失人數", "").Replace("理化流失人數", "").Replace("本週", "");
                                int lastWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.LastWeekNumber);
                                int thisWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                                if ((thisWeekNum - lastWeekNum) >= 0) {
                                    group.Number = 0;
                                }
                                else {
                                    group.Number = (thisWeekNum - lastWeekNum) * -1;
                                }
                            }
                        }
                        else if (group.Class.Course.Name.IndexOf("新生人數") >= 0) {
                            if (!group.IsManual) {
                                string subjectName = group.Class.Course.Name.Replace("數學文新生人數", "").Replace("數學新生人數", "").Replace("理化文新生人數", "").Replace("理化新生人數", "").Replace("本週", "");
                                int lastWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.LastWeekNumber);
                                int thisWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Name.Equals(subjectName) && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                                if ((thisWeekNum - lastWeekNum) >= 0) {
                                    group.Number = thisWeekNum - lastWeekNum;
                                }
                                else {
                                    group.Number = 0;
                                }
                            }
                        }
                    }
                    else if (studentPopulationData.Type == StudentPopulationType.GEPT) {
                        //取得相同班系及班型的班級
                        var classItems = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).ToList();
                        group.Number = classItems.Sum(e => e.Number);
                        if (group.Class.Course.Name.Equals("本週英檢總人數")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id != 21 && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("上週英檢總人數")) {
                            group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.LastWeekNumber); ;
                        }
                        else if (group.Class.Course.Name.Equals("與上週相比")) {
                            int lastWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.LastWeekNumber);
                            int thisWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                            group.Number = thisWeekNum - lastWeekNum;                            
                        }
                        else if (group.Class.Course.Name.Equals("去年同期人數")) {
                            int lastYear = studentPopulationData.Year - 1;
                            group.Number = dataContext.StudentPopulationItem.Include("StudentPopulation").Where(e => e.StudentPopulation.Year == lastYear && e.StudentPopulation.Week == studentPopulationData.Week && e.StudentPopulation.SchoolId == studentPopulationData.School.Id && e.Class.Course.Department != null && !e.Class.Course.IsSum).Sum(e => e.Number);
                        }
                        else if (group.Class.Course.Name.Equals("去年同期/比")) {
                            int lastYear = studentPopulationData.Year - 1;
                            int lastYearNum = dataContext.StudentPopulationItem.Include("StudentPopulation").Where(e => e.StudentPopulation.Year == lastYear && e.StudentPopulation.Week == studentPopulationData.Week && e.StudentPopulation.SchoolId == studentPopulationData.School.Id && e.Class.Course.Department != null && !e.Class.Course.IsSum).Sum(e => e.Number);
                            int thisWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                            group.Number = thisWeekNum - lastYearNum;
                        }
                        else if (group.Class.Course.Name.Equals("本週英檢新生人數")) {
                            int lastWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && !e.Class.Course.IsSum).Sum(e => e.LastWeekNumber);
                            int thisWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && !e.Class.Course.IsSum).Sum(e => e.Number);
                            if ((thisWeekNum - lastWeekNum) >= 0) {
                                group.Number = 0;
                            }
                            else {
                                group.Number = (thisWeekNum - lastWeekNum) * -1;
                            }
                        }
                        else if (group.Class.Course.Name.Equals("本週英檢流失人數")) {
                            int lastWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.LastWeekNumber);
                            int thisWeekNum = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                            if ((thisWeekNum - lastWeekNum) >= 0) {
                                group.Number = 0;
                            }
                            else {
                                group.Number = (thisWeekNum - lastWeekNum) * -1;
                            }
                        }

                    }
                    else if (studentPopulationData.Type == StudentPopulationType.PS) {
                        //取得相同班系及班型的班級
                        group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                    }
                    else if (studentPopulationData.Type == StudentPopulationType.AfterSchool) {
                        //取得相同班系及班型的班級
                        group.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id && !e.Class.Course.IsSum).Sum(e => e.Number);
                    }
                        dataContext.StudentPopulationItem.Update(group);
                        dataContext.SaveChanges();
                    }
                catch (Exception ex) {
                    string e = ex.Message;
                }

            }

            if (studentPopulationData.Type == StudentPopulationType.PH) {
                //總班數 小
                StudentPopulationItem subgroupClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("英文總班數統計") && e.Class.Type == ClassType.SubGroup);
                int[] countIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && (e.Department.Name.Equals("英文國小班") || e.Department.Name.Equals("英文國中班") || e.Department.Name.Equals("英文高中班"))).Select(e => e.Id).ToArray();
                subgroupClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && countIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.SubGroup && !e.Class.Course.IsSum).Count(); ;
                dataContext.StudentPopulationItem.Update(subgroupClassCount);
                dataContext.SaveChanges();
                //總班數 三
                StudentPopulationItem em3ClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("英文總班數統計") && e.Class.Type == ClassType.V3);
                //int[] countIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && (e.Department.Name.Equals("英文國小班") || e.Department.Name.Equals("英文國中班") || e.Department.Name.Equals("英文高中班"))).Select(e => e.Id).ToArray();
                em3ClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && countIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.V3 && !e.Class.Course.IsSum).Count(); ;
                dataContext.StudentPopulationItem.Update(em3ClassCount);
                dataContext.SaveChanges();

                //國文總班數 小
                StudentPopulationItem subgroupChClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("國文總班數") && e.Class.Type == ClassType.SubGroup);
                int[] chIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && (e.Department.Name.Equals("國語文"))).Select(e => e.Id).ToArray();
                subgroupChClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && chIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.SubGroup && !e.Class.Course.IsSum).Count();
                dataContext.StudentPopulationItem.Update(subgroupChClassCount);
                dataContext.SaveChanges();

                //國文總班數 三
                StudentPopulationItem em3ChClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("國文總班數") && e.Class.Type == ClassType.V3);
                em3ChClassCount.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && chIds.Contains(e.Class.Course.Id) && e.Class.Type == ClassType.V3 && !e.Class.Course.IsSum).Count();
                dataContext.StudentPopulationItem.Update(em3ChClassCount);
                dataContext.SaveChanges();

                //本週英語文總人數 全部
                //如果有重新匯入課程要調整對應Id
                int[] enCountIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && e.Id <= 38).Select(e => e.Id).ToArray();
                StudentPopulationItem sumWeekEn3Count = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週英語文總人數"));
                if (sumWeekEn3Count != null) {
                    sumWeekEn3Count.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && enCountIds.Contains(e.Class.Course.Id) && !e.Class.Course.IsSum).Sum(e => e.Number);
                    dataContext.StudentPopulationItem.Update(sumWeekEn3Count);
                    dataContext.SaveChanges();
                }

                //本周流失/新增 英文
                StudentPopulationItem addCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週英語文新生"));
                if (!addCount.IsManual) {
                    addCount.Number = addCount.Number + addEnStudent;
                    dataContext.StudentPopulationItem.Update(addCount);
                    dataContext.SaveChanges();
                }
                StudentPopulationItem lostCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週英語文流失"));
                if (!lostCount.IsManual) {
                    lostCount.Number = lostCount.Number + lostEnStudent;
                    dataContext.StudentPopulationItem.Update(lostCount);
                    dataContext.SaveChanges();
                }

                //本週國語文總人數 全部
                int[] chCountIds = dataContext.Course.Where(e => e.Type == StudentPopulationType.PH && !e.IsSum && e.Id > 38 && e.Id <= 58).Select(e => e.Id).ToArray();
                StudentPopulationItem sumWeekCh3Count = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週國語文總人數") && e.Class.Type == ClassType.V3);
                if (sumWeekCh3Count != null) {
                    sumWeekCh3Count.Number = studentPopulationData.Items.Where(e => e.Class.Course != null && chCountIds.Contains(e.Class.Course.Id) && !e.Class.Course.IsSum).Sum(e => e.Number);
                    dataContext.StudentPopulationItem.Update(sumWeekCh3Count);
                    dataContext.SaveChanges();
                }

                //本周流失/新增 國語
                StudentPopulationItem addChCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週國語文新生人數"));
                if (!addChCount.IsManual) {
                    addChCount.Number = addChCount.Number + addChStudent;
                    dataContext.StudentPopulationItem.Update(addChCount);
                    dataContext.SaveChanges();
                }
                StudentPopulationItem lostChCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週國語文流失人數"));
                if (!lostChCount.IsManual) {
                    lostChCount.Number = lostChCount.Number + lostChStudent;
                    dataContext.StudentPopulationItem.Update(lostChCount);
                    dataContext.SaveChanges();
                }

                /*與上週相比(英文) 
                33 本週英語文總人數
                34 上週英語文總人數        
                35 與上週相比
                36 去年同期/比
                 */
                try {

                    StudentPopulationItem lastSumAllCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("與上週相比"));
                    int lastWeek = studentPopulationData.Week - 1;
                    int lastWeekNum = dataContext.StudentPopulationItem.FirstOrDefault(e => e.StudentPopulation.Year == studentPopulationData.Year && e.StudentPopulation.Week == lastWeek && e.StudentPopulation.School.Id == studentPopulationData.School.Id && e.Class.Course.Department != null && e.Class.Course.Name.Equals("本週英語文總人數"))?.Number ?? 0;
                    lastSumAllCount.Number = studentPopulationData.Items.Where(e => !e.Class.Course.IsSum).Sum(e => e.Number);
                    lastSumAllCount.LastWeekNumber = lastWeekNum;
                    lastSumAllCount.Number = lastSumAllCount.Number - lastSumAllCount.LastWeekNumber;
                    dataContext.StudentPopulationItem.Update(lastSumAllCount);
                    dataContext.SaveChanges();

                    /*與上週相比(國文)
                    33 本週國文文總人數
                    34 上週國文文總人數        
                    35 與上週相比
                    36 去年同期/比            
                     */
                    StudentPopulationItem chLastSumAllCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("與上週相比"));
                    int chLastWeekNum = dataContext.StudentPopulationItem.FirstOrDefault(e => e.StudentPopulation.Year == studentPopulationData.Year && e.StudentPopulation.Week == lastWeek && e.StudentPopulation.School.Id == studentPopulationData.School.Id && e.Class.Course.Department != null && e.Class.Course.Name.Equals("本週英語文總人數"))?.Number ?? 0;
                    chLastSumAllCount.Number = studentPopulationData.Items.Where(e => !e.Class.Course.IsSum).Sum(e => e.Number);
                    chLastSumAllCount.LastWeekNumber = chLastWeekNum;
                    chLastSumAllCount.Number = lastSumAllCount.Number - chLastSumAllCount.LastWeekNumber;
                    dataContext.StudentPopulationItem.Update(chLastSumAllCount);
                    dataContext.SaveChanges();
                }
                catch (Exception ex) {
                    
                }


                //總人數
                StudentPopulationItem sumAllCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("總人數"));
                sumAllCount.Number = studentPopulationData.Items.Where(e => !e.Class.Course.IsSum).Sum(e => e.Number); ;
                dataContext.StudentPopulationItem.Update(sumAllCount);
                dataContext.SaveChanges();
            }
            else if (studentPopulationData.Type == StudentPopulationType.PSJ) {

            }
            else if (studentPopulationData.Type == StudentPopulationType.GEPT) {

            }
            else if (studentPopulationData.Type == StudentPopulationType.PS) {

            }
            dataContext.SaveChanges();
            return studentPopulationData;
        }
        #region 資料匯出

        [HttpGet("ExportPopulationPartial")]
        public IActionResult ExportPopulationPartial(int schoolId, int year, int week, string reportType) {
            var seleceedType = reportType switch {
                "PH"   => StudentPopulationType.PH,
                "PS"   => StudentPopulationType.PS,
                "GEPT" => StudentPopulationType.GEPT,
                "PSJ"  => StudentPopulationType.PSJ,
                "AS"   => StudentPopulationType.AfterSchool,
                _      => StudentPopulationType.PH
            };
            School school = Model.DataContext.School.Find(schoolId);

            IWorkbook wb = new XSSFWorkbook();
            ISheet ws = wb.CreateSheet("Sheet1");
            string fileName = "report.xlsx";

            using (DataContext dataContext = new DataContext()) {
                var pop = dataContext.StudentPopulation
                    .Include("Items.Class.Course")
                    .FirstOrDefault(e => e.School.Id == schoolId && e.Year == year && e.Week == week && e.Type == seleceedType);
                var courses = dataContext.Course.Include("Department")
                    .Where(e => e.Type == seleceedType)
                    .OrderBy(e => e.Department.Ordinal).ThenBy(e => e.Ordinal)
                    .ToList();
                // ── PH ──────────────────────────────────────────────────────────────
                if (seleceedType == StudentPopulationType.PH) {
                    fileName = $"{year}年第{week}週百瀚全國人數表.xlsx";

                    ws.CreateRow(0).CreateCell(0).SetCellValue($"{year}年第{week}週百瀚英語全國人數表");

                    IRow r1 = ws.CreateRow(1), r2 = ws.CreateRow(2), r3 = ws.CreateRow(3);
                    r1.CreateCell(0).SetCellValue("分校");
                    r1.CreateCell(1).SetCellValue("類型");
                    try { ws.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }
                    try { ws.AddMergedRegion(new CellRangeAddress(1, 3, 1, 1)); } catch { }

                    int col = 2, deptStart = 2, lastDeptId = 0;
                    foreach (var c in courses) {
                        if (c.Department.Id != lastDeptId) {
                            if (lastDeptId != 0 && col > deptStart)
                                try { ws.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, col - 1)); } catch { }
                            r1.CreateCell(col).SetCellValue(c.Department.Name);
                            lastDeptId = c.Department.Id;
                            deptStart = col;
                        }
                        r3.CreateCell(col).SetCellValue(c.Name);
                        ws.SetColumnWidth(col, 4 * 256);
                        col++;
                    }
                    if (lastDeptId != 0 && col > deptStart + 1)
                        try { ws.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, col - 1)); } catch { }

                    IRow sgRow = ws.CreateRow(4), v3Row = ws.CreateRow(5);
                    sgRow.CreateCell(0).SetCellValue(school?.Name ?? "");
                    sgRow.CreateCell(1).SetCellValue("小");
                    v3Row.CreateCell(0).SetCellValue("");
                    v3Row.CreateCell(1).SetCellValue("三");
                    try { ws.AddMergedRegion(new CellRangeAddress(4, 5, 0, 0)); } catch { }

                    col = 2;
                    foreach (var c in courses) {
                        int sg = pop?.Items.Where(e => e.Class.Course.Id == c.Id && e.Class.Type == ClassType.SubGroup).Sum(e => e.Number) ?? 0;
                        int v3 = pop?.Items.Where(e => e.Class.Course.Id == c.Id && e.Class.Type == ClassType.V3).Sum(e => e.Number) ?? 0;
                        if (sg > 0) sgRow.CreateCell(col).SetCellValue(sg);
                        if (v3 > 0) v3Row.CreateCell(col).SetCellValue(v3);
                        col++;
                    }
                }
                // ── PS ──────────────────────────────────────────────────────────────
                else if (seleceedType == StudentPopulationType.PS) {
                    fileName = $"{year}年第{week}週百世人數表.xlsx";

                    ws.CreateRow(0).CreateCell(0).SetCellValue($"{year}年第{week}週百世人數表");

                    IRow hdrRow = ws.CreateRow(1);
                    hdrRow.CreateCell(0).SetCellValue("");
                    int col = 1;
                    foreach (var c in courses) {
                        hdrRow.CreateCell(col).SetCellValue(c.Name);
                        ws.SetColumnWidth(col, 4 * 256);
                        col++;
                    }

                    IRow dataRow = ws.CreateRow(2);
                    dataRow.CreateCell(0).SetCellValue(school?.Name ?? "");
                    col = 1;
                    foreach (var c in courses) {
                        int sum = pop?.Items.Where(e => e.Class.Course.Id == c.Id).Sum(e => e.Number) ?? 0;
                        if (sum > 0) dataRow.CreateCell(col).SetCellValue(sum);
                        col++;
                    }
                }
                // ── PSJ / AS ────────────────────────────────────────────────────────
                else if (seleceedType == StudentPopulationType.PSJ || seleceedType == StudentPopulationType.AfterSchool) {
                    bool isPsj = seleceedType == StudentPopulationType.PSJ;
                    fileName = isPsj ? $"{year}年第{week}週百倍速人數表.xlsx" : $"{year}年第{week}週課輔人數表.xlsx";
                    int tId = isPsj ? 158 : 258;
                    var codeMap = isPsj ? _psjCourseIds : _asCourseIds;
                    Func<string, ClassType> colTypeFn = isPsj ? PsjColType : AsColType;
                    string[] codes = isPsj
                        ? new[] { "T", "MP", "MS", "SP", "SS", "N", "L", "W" }
                        : new[] { "T", "AS", "EP", "EG", "N", "L", "W" };

                    ws.CreateRow(0).CreateCell(0).SetCellValue(isPsj ? $"{year}年第{week}週百倍速人數表" : $"{year}年第{week}週課輔人數表");

                    IRow hdrRow = ws.CreateRow(4);
                    hdrRow.CreateCell(0).SetCellValue("年");
                    hdrRow.CreateCell(1).SetCellValue("週");
                    hdrRow.CreateCell(2).SetCellValue("分校");
                    hdrRow.CreateCell(3).SetCellValue("年級");
                    for (int i = 0; i < codes.Length; i++) hdrRow.CreateCell(4 + i).SetCellValue(codes[i]);

                    int tTotal = pop?.Items.Where(e => e.Class.Course.Id == tId).Sum(e => e.Number) ?? 0;
                    bool tWritten = false;
                    int dataRowNo = 5;

                    for (int gi = 0; gi < _gradeOrder.Length; gi++) {
                        var vals = new int[codes.Length];
                        vals[0] = (!tWritten && tTotal > 0) ? tTotal : 0;

                        for (int ci = 1; ci < codes.Length; ci++) {
                            if (!codeMap.TryGetValue(codes[ci], out int[] ids)) continue;
                            ClassType cType = colTypeFn(codes[ci]);
                            vals[ci] = pop?.Items.Where(e => e.Class.Course.Id == ids[gi] && e.Class.Type == cType).Sum(e => e.Number) ?? 0;
                        }

                        if (vals.All(v => v == 0)) continue;
                        if (vals[0] > 0) tWritten = true;

                        IRow row = ws.CreateRow(dataRowNo++);
                        row.CreateCell(0).SetCellValue(year);
                        row.CreateCell(1).SetCellValue(week);
                        row.CreateCell(2).SetCellValue(school?.Name ?? "");
                        row.CreateCell(3).SetCellValue(_gradeOrder[gi]);
                        for (int ci = 0; ci < vals.Length; ci++) {
                            if (vals[ci] > 0) row.CreateCell(4 + ci).SetCellValue(vals[ci]);
                        }
                    }
                }
                // ── GEPT (no matching import; keep dept/course header format) ────────
                else if (seleceedType == StudentPopulationType.GEPT) {
                    fileName = $"英檢班{week}週人數表.xlsx";
                    var courseCellStyle = (XSSFCellStyle)wb.CreateCellStyle();
                    courseCellStyle.WrapText = true;
                    IRow departmentRow = ws.CreateRow(0), courseRow = ws.CreateRow(1);
                    int cellNo = 0, currentDep = 0, mergedIndex = 0;
                    foreach (var cItem in courses) {
                        if (currentDep != cItem.Department.Id) {
                            currentDep = cItem.Department.Id;
                            departmentRow.CreateCell(cellNo).SetCellValue(cItem.Department.Name);
                            try { if (mergedIndex != cellNo) ws.AddMergedRegion(new CellRangeAddress(0, 0, mergedIndex, cellNo - 1)); } catch { }
                            mergedIndex = cellNo;
                        }
                        var cell = courseRow.CreateCell(cellNo);
                        cell.SetCellValue(cItem.Name);
                        cell.CellStyle = courseCellStyle;
                        ws.SetColumnWidth(cellNo, 4 * 256);
                        cellNo++;
                    }
                }
            }
            var memoryStream = new MemoryStream();
            wb.Write(memoryStream);
            return File(memoryStream.ToArray(), "application/octet-stream", fileName);
        }

        /// <summary>
        /// 匯出標準格式報表（原手填報表格式）。
        /// 支援單一分校或全區所有分校匯出，依區域分 Sheet。
        /// </summary>
        /// <param name="year">學年度</param>
        /// <param name="week">週次</param>
        /// <param name="reportType">PH / GEPT / PS / PSJ / AS</param>
        /// <param name="allSchools">true=所有分校（需 ViewAllSchools 權限），false=依登入者分校</param>
        /// <param name="schoolId">指定單一分校 Id；有值時優先，忽略 allSchools</param>
        [HttpGet]
        [Authorize(typeof(PortalUser))]
        public IActionResult ExportReport(int year, int week, string reportType, bool allSchools = false, int? schoolId = null) {
            var type = reportType switch {
                "PH"   => StudentPopulationType.PH,
                "PS"   => StudentPopulationType.PS,
                "GEPT" => StudentPopulationType.GEPT,
                "PSJ"  => StudentPopulationType.PSJ,
                "AS"   => StudentPopulationType.AfterSchool,
                _      => StudentPopulationType.PH
            };

            // 決定分校範圍
            IList<int> schoolIds = null;
            if (schoolId.HasValue) {
                // 單一分校：確認使用者有權限
                var accessible = Model.GetAccessibleSchools(User).Select(s => s.Id).ToList();
                if (!User.HasPermission(SystemPermission.ViewAllSchools) && !accessible.Contains(schoolId.Value))
                    return Forbid();
                schoolIds = new List<int> { schoolId.Value };
            } else if (!allSchools || !User.HasPermission(SystemPermission.ViewAllSchools)) {
                // 僅限登入者有權限的分校
                schoolIds = Model.GetAccessibleSchools(User)
                                 .Select(s => s.Id)
                                 .ToList();
            }

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
            string fileName = $"{title}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion

    }
}
