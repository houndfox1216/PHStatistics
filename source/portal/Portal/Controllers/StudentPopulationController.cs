using System;
using System.Framework;
using System.Framework.Logging;
using System.Framework.Web;
using PHStatistics.Portal.Models;
using Microsoft.AspNetCore.Mvc;
using PHStatistics.Content;
using System.Collections.Generic;
using System.Linq;
using System.Framework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nest;
using FluentFTP.Helpers;
using NuGet.Configuration;
using System.Framework.Globalization;
using System.Framework.Application;
using DevExpress.Data.Browsing;
using static NPOI.HSSF.Util.HSSFColor;
using Humanizer;

namespace PHStatistics.Portal.Controllers {
    public class StudentPopulationController : MvcController<PortalUser, Model, Culture> {
        public StudentPopulationController() : base("System") { }

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
            int[] weeks = dataContext.StudentPopulation.GroupBy(e => e.Week).Select(e => e.Key).ToArray();
            ViewBag.Schools = schools;
            ViewBag.Years = years;
            ViewBag.Weeks = weeks;
            ViewBag.SelectedYear = schoolYear;
            ViewBag.CanEdit = schoolYear != null;
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
                    if (type.Equals("PH")) {
                        returnData.Type = StudentPopulationType.PH;
                        returnData.Name = string.Format("{0}第{1}週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    }
                    else if (type.Equals("PSJ")) {
                        returnData.Type = StudentPopulationType.PSJ;
                        returnData.Name = string.Format("{0}第{1}週百倍速人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    }
                    else if (type.Equals("PHM")) {
                        returnData.Type = StudentPopulationType.PS;
                        returnData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    }
                    else if (type.Equals("GEPT")) {
                        returnData.Type = StudentPopulationType.GEPT;
                        returnData.Name = string.Format("{0}第{1}英檢週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    }
                    else {
                        returnData.Type = StudentPopulationType.PH;
                    }
                    dataContext.SaveChanges();
                }
                else {
                    returnData = new StudentPopulation();
                    returnData.School = dataContext.School.Find(schoolId);
                    returnData.Year = schoolYear.Year.Value;
                    returnData.Week = schoolYear.Week.Value;
                    returnData.Name = string.Format("{0}第{1}週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    returnData.WeekDate = schoolYear.WeekStartDate;
                    returnData.Items = new List<StudentPopulationItem>();
                    returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
                    if (type.Equals("PH")) {
                        returnData.Type = StudentPopulationType.PH;
                        returnData.Name = string.Format("{0}第{1}週百瀚人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    }
                    else if (type.Equals("PSJ")) {
                        returnData.Type = StudentPopulationType.PSJ;
                        returnData.Name = string.Format("{0}第{1}週百倍速人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    }
                    else if (type.Equals("PS")) {
                        returnData.Type = StudentPopulationType.PS;
                        returnData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    }
                    else if (type.Equals("GEPT")) {
                        returnData.Type = StudentPopulationType.GEPT;
                        returnData.Name = string.Format("{0}第{1}週英檢人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    }
                    else {
                        returnData.Type = StudentPopulationType.PH;
                    }
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
                                item.Number = 0;
                                item.LastWeekNumber = lItem.Number;
                                item.StudentRemark = lItem.StudentRemark;
                                returnData.Items.Add(item);
                            }
                        }
                    }
                    dataContext.SaveChanges();
                    //增加固定總計項目
                    if (type.Equals("PH")) {
                        //英文個別指導 162 國文個別指導 172
                        foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PH).OrderBy(e => e.Ordinal).ToList()) {
                            if (course.Department.Name.Equals("英文個別指導") || course.Department.Name.Equals("國語文個別指導") || course.Name.Equals("英文總班數統計") ||
                                course.Name.Equals("英文總班數統計") || course.Name.Equals("英文合作開班人數合計") || course.Name.Equals("本週英語文新生") ||
                                course.Name.Equals("上週英語文總人數") || course.Name.Equals("與上週相比") || course.Name.Equals("去年同期/比") ||
                                course.Name.Equals("本週英語文新生") || course.Name.Equals("本週英語文流失") || course.Name.Equals("國語文個別指導人數合計") ||
                                course.Name.Equals("本週國語文總人數") || course.Name.Equals("本週英語文流失") || course.Name.Equals("上週國語文總人數") ||
                                course.Name.Equals("本週國語文新生人數") || course.Name.Equals("本週國語文流失人數") || course.Name.Equals("本週總詢問(填單)人數") ||
                                course.Name.Equals("總人數")) {
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
                                //小
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
                        if (lastWeekData != null && (lastWeekData.Items != null && lastWeekData.Items.Count > 0)) {
                            foreach (StudentPopulationItem sItem in lastWeekData.Items) {
                                if (sItem.Class == null || sItem.Class.Id <= 0) continue; //如果班級不存在則跳過
                                if (!returnData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                                    StudentPopulationItem item = new StudentPopulationItem();
                                    item.Name = sItem.Name;
                                    item.SchoolName = sItem.SchoolName;
                                    item.Class = dataContext.Class.Find(sItem.Class.Id);
                                    item.Number = sItem.Number;
                                    item.LastWeekNumber = sItem.Number;
                                    item.StudentRemark = sItem.StudentRemark;
                                    item.Remark = sItem.Remark;
                                    returnData.Items.Add(item);
                                }
                                else {
                                    StudentPopulationItem item = returnData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id);
                                    if (item != null) {
                                        item.LastWeekNumber = sItem.Number;
                                    }
                                }
                            }
                            dataContext.SaveChanges();
                        }
                    }
                    else if (type.Equals("PSJ")) {
                        foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PH).OrderBy(e => e.Ordinal).ToList()) {
                            int[] sumIds = { 162, 163, 164, 167, 168, 170, 171 };
                            if (sumIds.Contains(course.Department.Id)) {
                                //1v1
                                if (course.Id == 686) {
                                    //團
                                    //if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Group && e.StudentPopulation.Id == returnData.Id)) {
                                    //    StudentPopulationItem item = new StudentPopulationItem();
                                    //    Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Group);
                                    //    if (classItem == null) {
                                    //        classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Group };
                                    //        dataContext.Class.Add(classItem);
                                    //        dataContext.SaveChanges();
                                    //    }
                                    //    item.Name = course.Name;
                                    //    item.SchoolName = course.Name;
                                    //    item.Class = classItem;
                                    //    item.Number = 0;
                                    //    item.LastWeekNumber = 0;
                                    //    returnData.Items.Add(item);
                                    //}
                                    //小
                                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.SubGroup && e.StudentPopulation.Id == returnData.Id)) {
                                        StudentPopulationItem item = new StudentPopulationItem();
                                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.SubGroup);
                                        if (classItem == null) {
                                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.SubGroup };
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
                                        item.SchoolName = course.Name;
                                        item.Class = classItem;
                                        item.Number = 0;
                                        item.LastWeekNumber = 0;
                                        returnData.Items.Add(item);
                                    }
                                }
                                else {
                                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Personal && e.StudentPopulation.Id == returnData.Id)) {
                                        StudentPopulationItem item = new StudentPopulationItem();
                                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Personal);
                                        if (classItem == null) {
                                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Personal };
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

                            }
                            else {
                                //團
                                //if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Group && e.StudentPopulation.Id == returnData.Id)) {
                                //    StudentPopulationItem item = new StudentPopulationItem();
                                //    Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Group);
                                //    if (classItem == null) {
                                //        classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Group };
                                //        dataContext.Class.Add(classItem);
                                //        dataContext.SaveChanges();
                                //    }
                                //    item.Name = course.Name;
                                //    item.SchoolName = course.Name;
                                //    item.Class = classItem;
                                //    item.Number = 0;
                                //    item.LastWeekNumber = 0;
                                //    returnData.Items.Add(item);
                                //}
                                //小
                                if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.SubGroup && e.StudentPopulation.Id == returnData.Id)) {
                                    StudentPopulationItem item = new StudentPopulationItem();
                                    Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.SubGroup);
                                    if (classItem == null) {
                                        classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.SubGroup };
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
                        if (lastWeekData != null && (lastWeekData.Items != null && lastWeekData.Items.Count > 0)) {
                            foreach (StudentPopulationItem sItem in lastWeekData.Items) {
                                if (sItem.Class == null || sItem.Class.Id <= 0) continue; //如果班級不存在則跳過
                                if (!returnData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                                    StudentPopulationItem item = new StudentPopulationItem();
                                    item.Name = sItem.Name;
                                    item.SchoolName = sItem.SchoolName;
                                    item.Class = dataContext.Class.Find(sItem.Class.Id);
                                    item.Number = sItem.Number;
                                    item.LastWeekNumber = sItem.Number;
                                    returnData.Items.Add(item);
                                }
                                else {
                                    StudentPopulationItem item = returnData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id);
                                    if (item != null) {
                                        item.LastWeekNumber = sItem.Number;
                                    }
                                }
                            }
                            dataContext.SaveChanges();
                        }
                    }
                    else if (type.Equals("PS")) {
                        foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PH).OrderBy(e => e.Ordinal).ToList()) {
                            int[] sumIds = { 162, 163, 164, 167, 168, 170, 171 };
                            if (sumIds.Contains(course.Department.Id)) {
                                //1v1
                                if (course.Id == 686) {
                                    //小
                                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.SubGroup && e.StudentPopulation.Id == returnData.Id)) {
                                        StudentPopulationItem item = new StudentPopulationItem();
                                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.SubGroup);
                                        if (classItem == null) {
                                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.SubGroup };
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
                                        item.SchoolName = course.Name;
                                        item.Class = classItem;
                                        item.Number = 0;
                                        item.LastWeekNumber = 0;
                                        returnData.Items.Add(item);
                                    }
                                }
                                else {
                                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Personal && e.StudentPopulation.Id == returnData.Id)) {
                                        StudentPopulationItem item = new StudentPopulationItem();
                                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Personal);
                                        if (classItem == null) {
                                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Personal };
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

                            }
                            else {
                                //小
                                if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.SubGroup && e.StudentPopulation.Id == returnData.Id)) {
                                    StudentPopulationItem item = new StudentPopulationItem();
                                    Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.SubGroup);
                                    if (classItem == null) {
                                        classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.SubGroup };
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
                        if (lastWeekData != null && (lastWeekData.Items != null && lastWeekData.Items.Count > 0)) {
                            foreach (StudentPopulationItem sItem in lastWeekData.Items) {
                                if (sItem.Class == null || sItem.Class.Id <= 0) continue; //如果班級不存在則跳過
                                if (!returnData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                                    StudentPopulationItem item = new StudentPopulationItem();
                                    item.Name = sItem.Name;
                                    item.SchoolName = sItem.SchoolName;
                                    item.Class = dataContext.Class.Find(sItem.Class.Id);
                                    item.Number = sItem.Number;
                                    item.LastWeekNumber = sItem.Number;
                                    returnData.Items.Add(item);
                                }
                                else {
                                    StudentPopulationItem item = returnData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id);
                                    if (item != null) {
                                        item.LastWeekNumber = sItem.Number;
                                    }
                                }
                            }
                            dataContext.SaveChanges();
                        }
                    }
                    else if (type.Equals("GEPT")) {
                        foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PH).OrderBy(e => e.Ordinal).ToList()) {
                            int[] sumIds = { 162, 163, 164, 167, 168, 170, 171 };
                            if (sumIds.Contains(course.Department.Id)) {
                                //1v1
                                if (course.Id == 686) {
                                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.SubGroup && e.StudentPopulation.Id == returnData.Id)) {
                                        StudentPopulationItem item = new StudentPopulationItem();
                                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.SubGroup);
                                        if (classItem == null) {
                                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.SubGroup };
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
                                        item.SchoolName = course.Name;
                                        item.Class = classItem;
                                        item.Number = 0;
                                        item.LastWeekNumber = 0;
                                        returnData.Items.Add(item);
                                    }
                                }
                                else {
                                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Personal && e.StudentPopulation.Id == returnData.Id)) {
                                        StudentPopulationItem item = new StudentPopulationItem();
                                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Personal);
                                        if (classItem == null) {
                                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Personal };
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

                            }
                            else {
                                //團
                                //if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Group && e.StudentPopulation.Id == returnData.Id)) {
                                //    StudentPopulationItem item = new StudentPopulationItem();
                                //    Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Group);
                                //    if (classItem == null) {
                                //        classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Group };
                                //        dataContext.Class.Add(classItem);
                                //        dataContext.SaveChanges();
                                //    }
                                //    item.Name = course.Name;
                                //    item.SchoolName = course.Name;
                                //    item.Class = classItem;
                                //    item.Number = 0;
                                //    item.LastWeekNumber = 0;
                                //    returnData.Items.Add(item);
                                //}
                                //小
                                if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.SubGroup && e.StudentPopulation.Id == returnData.Id)) {
                                    StudentPopulationItem item = new StudentPopulationItem();
                                    Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.SubGroup);
                                    if (classItem == null) {
                                        classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.SubGroup };
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
                        if (lastWeekData != null && (lastWeekData.Items != null && lastWeekData.Items.Count > 0)) {
                            foreach (StudentPopulationItem sItem in lastWeekData.Items) {
                                if (sItem.Class == null || sItem.Class.Id <= 0) continue; //如果班級不存在則跳過
                                if (!returnData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                                    StudentPopulationItem item = new StudentPopulationItem();
                                    item.Name = sItem.Name;
                                    item.SchoolName = sItem.SchoolName;
                                    item.Class = dataContext.Class.Find(sItem.Class.Id);
                                    item.Number = sItem.Number;
                                    item.LastWeekNumber = sItem.Number;
                                    returnData.Items.Add(item);
                                }
                                else {
                                    StudentPopulationItem item = returnData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id);
                                    if (item != null) {
                                        item.LastWeekNumber = sItem.Number;
                                    }
                                }
                            }
                            dataContext.SaveChanges();
                        }
                    }
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

            StudentPopulation returnData = new StudentPopulation();
            List<Course> courses = Model.DataContext.Course.Where(e => e.Type == populationType ).OrderBy(e => e.Ordinal).ToList();
            List<CourseDepartment> department = Model.DataContext.CourseDepartment.Where(e => e.Type == populationType ).OrderBy(e => e.Ordinal).ToList();
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
                if (type.Equals("PH")) {
                    returnData.Type = StudentPopulationType.PH;
                    returnData.Name = string.Format("{0}第{1}百瀚週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PSJ")) {
                    returnData.Type = StudentPopulationType.PSJ;
                    returnData.Name = string.Format("{0}第{1}百倍速週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PS")) {
                    returnData.Type = StudentPopulationType.PS;
                    returnData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("GEPT")) {
                    returnData.Type = StudentPopulationType.GEPT;
                    returnData.Name = string.Format("{0}第{1}英檢週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else {
                    returnData.Type = StudentPopulationType.PH;
                }
                dataContext.SaveChanges();
            }
            else {
                returnData = new StudentPopulation();
                returnData.School = dataContext.School.Find(schoolId);
                returnData.Year = schoolYear.Year.Value;
                returnData.Week = schoolYear.Week.Value;
                returnData.Name = string.Format("{0}第{1}週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                returnData.WeekDate = schoolYear.WeekStartDate;
                returnData.Items = new List<StudentPopulationItem>();
                returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
                if (type.Equals("PH")) {
                    returnData.Type = StudentPopulationType.PH;
                    returnData.Name = string.Format("{0}第{1}百瀚週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PSJ")) {
                    returnData.Type = StudentPopulationType.PSJ;
                    returnData.Name = string.Format("{0}第{1}百倍速週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PS")) {
                    returnData.Type = StudentPopulationType.PS;
                    returnData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("GEPT")) {
                    returnData.Type = StudentPopulationType.GEPT;
                    returnData.Name = string.Format("{0}第{1}英檢週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else {
                    returnData.Type = StudentPopulationType.PH;
                }
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
                            item.Number = 0;
                            item.LastWeekNumber = lItem.Number;
                            returnData.Items.Add(item);
                        }
                    }
                }
                dataContext.SaveChanges();
                //增加固定總計項目
                foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PSJ).OrderBy(e => e.Ordinal).ToList()) {
                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Personal && e.StudentPopulation.Id == returnData.Id)) {
                        StudentPopulationItem item = new StudentPopulationItem();
                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Personal);
                        if (classItem == null) {
                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Personal };
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
                if (type.Equals("PH")) {
                    returnData.Type = StudentPopulationType.PH;
                    returnData.Name = string.Format("{0}第{1}百瀚週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PSJ")) {
                    returnData.Type = StudentPopulationType.PSJ;
                    returnData.Name = string.Format("{0}第{1}百倍速週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PS")) {
                    returnData.Type = StudentPopulationType.PS;
                    returnData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("GEPT")) {
                    returnData.Type = StudentPopulationType.GEPT;
                    returnData.Name = string.Format("{0}第{1}英檢週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else {
                    returnData.Type = StudentPopulationType.PH;
                }
                dataContext.SaveChanges();
            }
            else {
                returnData = new StudentPopulation();
                returnData.School = dataContext.School.Find(schoolId);
                returnData.Year = schoolYear.Year.Value;
                returnData.Week = schoolYear.Week.Value;
                returnData.Name = string.Format("{0}第{1}週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                returnData.WeekDate = schoolYear.WeekStartDate;
                returnData.Items = new List<StudentPopulationItem>();
                returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
                if (type.Equals("PH")) {
                    returnData.Type = StudentPopulationType.PH;
                    returnData.Name = string.Format("{0}第{1}百瀚週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PSJ")) {
                    returnData.Type = StudentPopulationType.PSJ;
                    returnData.Name = string.Format("{0}第{1}百倍速週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PS")) {
                    returnData.Type = StudentPopulationType.PS;
                    returnData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("Gept")) {
                    returnData.Type = StudentPopulationType.GEPT;
                    returnData.Name = string.Format("{0}第{1}英檢週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else {
                    returnData.Type = StudentPopulationType.PH;
                }
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
                            item.Number = 0;
                            item.LastWeekNumber = lItem.Number;
                            returnData.Items.Add(item);
                        }
                    }
                }
                dataContext.SaveChanges();
                //增加固定總計項目
                foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.GEPT).OrderBy(e => e.Ordinal).ToList()) {
                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Personal && e.StudentPopulation.Id == returnData.Id)) {
                        StudentPopulationItem item = new StudentPopulationItem();
                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Personal);
                        if (classItem == null) {
                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Personal };
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
                if (type.Equals("PH")) {
                    returnData.Type = StudentPopulationType.PH;
                    returnData.Name = string.Format("{0}第{1}百瀚週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PSJ")) {
                    returnData.Type = StudentPopulationType.PSJ;
                    returnData.Name = string.Format("{0}第{1}百倍速週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PS")) {
                    returnData.Type = StudentPopulationType.PS;
                    returnData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("GEPT")) {
                    returnData.Type = StudentPopulationType.GEPT;
                    returnData.Name = string.Format("{0}第{1}英檢週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else {
                    returnData.Type = StudentPopulationType.PH;
                }
                dataContext.SaveChanges();
            }
            else {
                returnData = new StudentPopulation();
                returnData.School = dataContext.School.Find(schoolId);
                returnData.Year = schoolYear.Year.Value;
                returnData.Week = schoolYear.Week.Value;
                returnData.Name = string.Format("{0}第{1}週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                returnData.WeekDate = schoolYear.WeekStartDate;
                returnData.Items = new List<StudentPopulationItem>();
                returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
                if (type.Equals("PH")) {
                    returnData.Type = StudentPopulationType.PH;
                    returnData.Name = string.Format("{0}第{1}百瀚週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PSJ")) {
                    returnData.Type = StudentPopulationType.PSJ;
                    returnData.Name = string.Format("{0}第{1}百倍速週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("PS")) {
                    returnData.Type = StudentPopulationType.PS;
                    returnData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else if (type.Equals("GEPT")) {
                    returnData.Type = StudentPopulationType.GEPT;
                    returnData.Name = string.Format("{0}第{1}英檢週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                }
                else {
                    returnData.Type = StudentPopulationType.PH;
                }
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
                            item.Number = 0;
                            item.LastWeekNumber = lItem.Number;
                            returnData.Items.Add(item);
                        }
                    }
                }
                dataContext.SaveChanges();
                //增加固定總計項目
                foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PS).OrderBy(e => e.Ordinal).ToList()) {
                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Personal && e.StudentPopulation.Id == returnData.Id)) {
                        StudentPopulationItem item = new StudentPopulationItem();
                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Personal);
                        if (classItem == null) {
                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Personal };
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
                _ => StudentPopulationType.PH
            };
            StudentPopulation studentPopulationData = Model.GetStudentPopulation(schoolId, year, week, seleceedType);
            List<Course> courses = Model.DataContext.Course.Where(e => e.Type == studentPopulationData.Type).OrderBy(e => e.Ordinal).ToList();
            ViewBag.Courses = courses;
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
                        if (newClassType == 0) {
                            newClass.Type = ClassType.Group;
                        }
                        else if (newClassType == 1) {
                            newClass.Type = ClassType.Personal;
                        }
                        else if (newClassType == 2) {
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
                        if (newClassType == 0) {
                            newClass.Type = ClassType.Group;
                        }
                        else if (newClassType == 1) {
                            newClass.Type = ClassType.Personal;
                        }
                        else if (newClassType == 2) {
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

        public IActionResult UpdateClassItem(long sId, int number) {
            DataContext dataContext = new DataContext();
            try {
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("Class.Course.Department").Include("StudentPopulation").Where(e => e.Id == sId).FirstOrDefault();
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type && !e.IsSum).OrderBy(e => e.Ordinal).ToList();
                ViewBag.Courses = courses;
                if (item != null) {
                    if (item.Class.Course.Name.Equals("本週英語文新生") || item.Class.Course.Name.Equals("本週英語文流失") || 
                        item.Class.Course.Name.Equals("本週國語文新生人數") || item.Class.Course.Name.Equals("本週國語文流失人數")) {
                        item.IsManual = true;
                    }
                    item.Number = number;
                    dataContext.StudentPopulationItem.Update(item);
                    dataContext.SaveChanges();
                    //進行加總
                    SumPHPopulation(item.StudentPopulation.Id);
                }
                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                return PartialView("PopulationPartialView", returnData);
            }
            catch (Exception ex) {
                return PartialView("PopulationPartialView", new StudentPopulation());
            }
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
        public IActionResult QueryPopulationPartial(int schoolId, int year, int week) {

            List<Course> courses = Model.DataContext.Course.OrderBy(e => e.Ordinal).ToList();
            ViewBag.Courses = courses;
            DataContext dataContext = new DataContext();
            var seleceedType = StudentPopulationType.PH;
            //var seleceedType = type switch {
            //    "PH" => StudentPopulationType.PH,
            //    "PS" => StudentPopulationType.PS,
            //    "PHM" => StudentPopulationType.PHM,
            //    _ => StudentPopulationType.AfterSchool
            //};


            StudentPopulation studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week).FirstOrDefault();
            return PartialView("QueryPopulationPartialView", studentPopulationData);
        }

        public StudentPopulation SumPHPopulation(long spId) {

            DataContext dataContext = new DataContext();
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
                    //取得相同班系及班型的班級
                    var classItems = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).ToList();
                    group.Number = classItems.Sum(e => e.Number);
                    int lastWeekNumber = classItems.Sum(e => e.LastWeekNumber);
                    if (!group.Class.Course.Department.Name.Equals("英文個別指導")) {
                        if (group.Class.Course.Department.Name.Equals("英文國小班") || group.Class.Course.Department.Name.Equals("英文國中班") || group.Class.Course.Department.Name.Equals("英文高中班"))
                            eNCount = eNCount + studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Count();
                    }else if (!group.Class.Course.Department.Name.Equals("國語文個別指導")) {
                        if (group.Class.Course.Department.Name.Equals("國語文"))
                            chCount = chCount + studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == group.Class.Course.Department.Id && e.Class.Type == group.Class.Type && !e.Class.Course.IsSum).Count();
                    }
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

                    dataContext.StudentPopulationItem.Update(group);
                    dataContext.SaveChanges();
                }
                catch (Exception ex) {
                    string e = ex.Message;
                }

            }

            if (studentPopulationData.Type == StudentPopulationType.PH) {
                //總班數
                StudentPopulationItem sumClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("英文總班數統計"));
                sumClassCount.Number = eNCount;
                dataContext.StudentPopulationItem.Update(sumClassCount);
                dataContext.SaveChanges();

                //國文總班數
                StudentPopulationItem sumChClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("國文總班數"));
                sumChClassCount.Number = chCount;
                dataContext.StudentPopulationItem.Update(sumChClassCount);
                dataContext.SaveChanges();

                ////總班數
                //StudentPopulationItem sumClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("英文總班數統計"));
                //sumClassCount.Number = eNCount;
                //dataContext.StudentPopulationItem.Update(sumClassCount);
                //dataContext.SaveChanges();

                ////國文總班數
                //StudentPopulationItem sumChClassCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("國文總班數"));
                //sumChClassCount.Number = chCount;
                //dataContext.StudentPopulationItem.Update(sumChClassCount);
                //dataContext.SaveChanges();


                //本週英語文總人數 三
                StudentPopulationItem sumWeekEn3Count = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週英語文總人數") && e.Class.Type == ClassType.V3);
                if(sumWeekEn3Count != null) {
                    sumWeekEn3Count.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == sumWeekEn3Count.Class.Course.Department.Id && e.Class.Type == sumWeekEn3Count.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                    dataContext.StudentPopulationItem.Update(sumWeekEn3Count);
                    dataContext.SaveChanges();
                }

                //本週英語文總人數 小
                StudentPopulationItem sumWeekEnGCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週英語文總人數") && e.Class.Type == ClassType.SubGroup);
                if (sumWeekEnGCount != null) {
                    sumWeekEnGCount.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == sumWeekEnGCount.Class.Course.Department.Id && e.Class.Type == sumWeekEnGCount.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number); ;
                    dataContext.StudentPopulationItem.Update(sumWeekEnGCount);
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

                //本週國語文總人數 三
                StudentPopulationItem sumWeekCh3Count = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週國語文總人數") && e.Class.Type == ClassType.V3);
                if(sumWeekCh3Count != null) {
                    sumWeekCh3Count.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == sumWeekCh3Count.Class.Course.Department.Id && e.Class.Type == sumWeekCh3Count.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number);
                    dataContext.StudentPopulationItem.Update(sumWeekCh3Count);
                    dataContext.SaveChanges();
                }

                //本週國語文總人數 小
                StudentPopulationItem sumWeekChGCount = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Name.Equals("本週國語文總人數") && e.Class.Type == ClassType.SubGroup);
                if(sumWeekChGCount != null) {
                    sumWeekChGCount.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department != null && e.Class.Course.Department.Id == sumWeekChGCount.Class.Course.Department.Id && e.Class.Type == sumWeekChGCount.Class.Type && !e.Class.Course.IsSum).Sum(e => e.Number); ;
                    dataContext.StudentPopulationItem.Update(sumWeekChGCount);
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

                //總班數
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

    }
}
