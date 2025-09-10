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
            ViewBag.CanEdit = schoolYear != null;
            ViewBag.Courses = Model.DataContext.Course.OrderBy(e => e.Ordinal).ToList();
            int memberSchool = schools.FirstOrDefault().School.Id;
            int year = years[years.Length - 1];
            int week = weeks[weeks.Length - 1];
            StudentPopulation studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.School.Id == memberSchool && e.Year == year && e.Week == week).FirstOrDefault();

            return View(studentPopulationData);
        }

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
                ViewBag.Year = schoolYear.Year;
                ViewBag.Week = schoolYear.Week;
                ViewBag.Courses = courses;

                if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType)) {
                    returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == populationType);
                    //foreach (StudentPopulationItem sItem in returnData.Items) {
                    //    if (lastWeekData != null && lastWeekData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                    //        sItem.LastWeekNumber = lastWeekData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id).Number;
                    //    }
                    //}
                    //if (type.Equals("PH")) {
                    //    returnData.Type = StudentPopulationType.PH;
                    //    returnData.Name = string.Format("{0}第{1}週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    //}
                    //else if (type.Equals("PSJ")) {
                    //    returnData.Type = StudentPopulationType.PSJ;
                    //    returnData.Name = string.Format("{0}第{1}週百倍速人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    //}
                    //else if (type.Equals("PHM")) {
                    //    returnData.Type = StudentPopulationType.PHM;
                    //    returnData.Name = string.Format("{0}第{1}週百世人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    //}
                    //else if (type.Equals("GEPT")) {
                    //    returnData.Type = StudentPopulationType.GEPT;
                    //    returnData.Name = string.Format("{0}第{1}英檢週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
                    //}
                    //else {
                    //    returnData.Type = StudentPopulationType.PH;
                    //}
                    //dataContext.SaveChanges();
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
                        foreach(StudentPopulationItem lItem in lastWeekData.Items) {
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
                    //增加固定總計項目
                    if (type.Equals("PH")) {
                        //英文個別指導 162 國文個別指導 172
                        foreach (Course course in dataContext.Course.Include("Department").Where(e => e.IsSum == true && e.Type == StudentPopulationType.PH).OrderBy(e => e.Ordinal).ToList()) {
                            int[] sumIds = { 162, 163, 164, 167, 168, 170, 171 };
                            if (sumIds.Contains(course.Department.Id)) {
                                //1v1
                                if (course.Id == 686) {
                                    //團
                                    if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Group && e.StudentPopulation.Id == returnData.Id)) {
                                        StudentPopulationItem item = new StudentPopulationItem();
                                        Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Group);
                                        if (classItem == null) {
                                            classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Group };
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
                                if (!dataContext.StudentPopulationItem.Any(e => e.Class.Course.Id == course.Id && e.Class.Type == ClassType.Group && e.StudentPopulation.Id == returnData.Id)) {
                                    StudentPopulationItem item = new StudentPopulationItem();
                                    Class classItem = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id && e.Type == ClassType.Group);
                                    if (classItem == null) {
                                        classItem = new Class() { SchoolId = schoolId, CourseId = course.Id, Name = course.Name, Type = ClassType.Group };
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
                    else if (type.Equals("PSJ")) {

                    }
                    else if (type.Equals("PS")) {

                    }
                    else if (type.Equals("GEPT")) {

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
            List<Course> courses = Model.DataContext.Course.Where(e => e.Type == populationType).OrderBy(e => e.Ordinal).ToList();
            ViewBag.Year = schoolYear.Year;
            ViewBag.Week = schoolYear.Week;
            ViewBag.Courses = courses;

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
            ViewBag.Year = schoolYear.Year;
            ViewBag.Week = schoolYear.Week;
            ViewBag.Courses = courses;

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
            ViewBag.Year = schoolYear.Year;
            ViewBag.Week = schoolYear.Week;
            ViewBag.Courses = courses;

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
        [HttpPost("AddClass")]
        public IActionResult AddClass(int courseId, int schoolId, int year, int week, string[][] itemArr, int newClassType, string newClassName, int newNumber, string newStudentremark, string type) {
            List<Course> courses = Model.DataContext.Course.Include("Department").Where(e => e.Department.Company == Company.PH).OrderBy(e => e.Ordinal).ToList();
            ViewBag.Courses = courses;
            DataContext dataContext = new DataContext();
            var seleceedType = type switch {
                "PH" => StudentPopulationType.PH,
                "PS" => StudentPopulationType.PS,
                "PSJ" => StudentPopulationType.PSJ,
                "Gept" => StudentPopulationType.GEPT,
                _ => StudentPopulationType.PH
            };
            StudentPopulation studentPopulationData = Model.GetStudentPopulation(schoolId, year, week, seleceedType);
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
                    newClass.Name = string.IsNullOrEmpty(newClassName) ? string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00")) : newClassName;
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
                addItem.SchoolName = newClass.Name;
                addItem.Number = 0;
                addItem.LastWeekNumber = 0;
                addItem.StudentPopulation = null;
                addItem.StudentPopulationId = studentPopulationData.Id;
                dataContext.StudentPopulationItem.Add(addItem);
                dataContext.SaveChanges();
            }
            catch (Exception ex) {
                string e = ex.Message;
            }

            //檢查及加總

            studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week).FirstOrDefault();// Model.GetStudentPopulation(schoolId, year, week);
            return PartialView("PopulationPartialView", studentPopulationData);
        }

        [Authorize(typeof(PortalUser))]
        [HttpPost("AddNewClass")]
        // data: { 'schoolId': schoolId, 'courseId': newCourses.value, 'week': week, 'year': year, 'newClassType': newClassType, 'newClassName': newClassName, 'newNumber': newNumber, 'newStudentremark':newStudentremark },
        public IActionResult AddNewClass(int courseId, int schoolId, int year, int week, string[][] itemArr, int newClassType, string newClassName, int newNumber, string newStudentremark, string type) {

            List<Course> courses = Model.DataContext.Course.OrderBy(e => e.Ordinal).ToList();
            ViewBag.Courses = courses;
            DataContext dataContext = new DataContext();
            var seleceedType = type switch {
                "PH" => StudentPopulationType.PH,
                "PS" => StudentPopulationType.PS,
                "PSJ" => StudentPopulationType.PSJ,
                "Gept" => StudentPopulationType.GEPT,
                _ => StudentPopulationType.PH
            };
            StudentPopulation studentPopulationData = Model.GetStudentPopulation(schoolId, year, week, seleceedType);
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
                        newClass.Name = string.IsNullOrEmpty(newClassName) ? string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00")) : newClassName;
                        newClass.
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
                            newClass.Type = ClassType.SubGroup;
                        }
                        newClass.Name = string.IsNullOrEmpty(newClassName) ? string.Format("{0}_{1}", course.Name, (classCount + 1).ToString("00")) : newClassName;
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
                    dataContext.StudentPopulationItem.Add(addItem);
                    dataContext.SaveChanges();



                    //加總
                    //取得上周資料
                    StudentPopulation lastStudentPopulationData = dataContext.StudentPopulation.Include("Items").Where(e => e.School.Id == schoolId && week < studentPopulationData.Week && e.Type == StudentPopulationType.PSJ).OrderByDescending(e => e.Id).FirstOrDefault();
                    List<StudentPopulationItem> lastsumItem = new List<StudentPopulationItem>();
                    if (lastStudentPopulationData != null && lastStudentPopulationData.HasValue()) {
                        lastsumItem = dataContext.StudentPopulationItem.Include("Class.Course.Department").Where(e => e.StudentPopulationId == lastStudentPopulationData.Id).ToList();
                    }
                    List<StudentPopulationItem> sumItem = dataContext.StudentPopulationItem.Include("Class.Course.Department").Where(e => e.StudentPopulationId == studentPopulationData.Id).ToList();
                    var departmentGroup = sumItem.GroupBy(e => new { e.Class.Course.Department.Id }).Select(group => new { depId = group.Key.Id });
                    int lastWeekCount = 0;
                    foreach (var department in departmentGroup) {
                        Course sumCourse = dataContext.Course.Where(e => e.Department.Id == department.depId && e.IsSum == true).FirstOrDefault();
                        if (sumCourse != null) {
                            Class sumClass = new Class();
                            if (dataContext.Class.Any(e => e.School.Id == schoolId && e.Course.Id == sumCourse.Id)) {
                                sumClass = dataContext.Class.Include("Course.Department").Where(e => e.School.Id == schoolId && e.Course.Id == sumCourse.Id).FirstOrDefault();
                            }
                            else {
                                sumClass = new Class() { SchoolId = schoolId, CourseId = sumCourse.Id, Name = sumCourse.Name };
                                dataContext.Class.Add(sumClass);
                                dataContext.SaveChanges();
                            }
                            //新增班系合計
                            bool itemIsNew = true;
                            StudentPopulationItem newSumItem = new StudentPopulationItem();
                            if (dataContext.StudentPopulationItem.Any(e => e.Class.Id == sumClass.Id && e.StudentPopulation.Id == studentPopulationData.Id)) {
                                itemIsNew = false;
                                newSumItem = dataContext.StudentPopulationItem.Where(e => e.Class.Id == sumClass.Id && e.StudentPopulation.Id == studentPopulationData.Id).FirstOrDefault();
                            }
                            else {
                                itemIsNew = true;
                                newSumItem = new StudentPopulationItem();
                                newSumItem.IsSum = true;
                            }
                            newSumItem.ClassId = sumClass.Id;
                            newSumItem.Name = sumClass.Name;
                            var ff = sumItem.Where(e => e.Class.Course.Department.Id == department.depId && e.IsSum == false).ToList();
                            newSumItem.Number = sumItem.Where(e => e.Class.Course.Department.Id == department.depId && e.IsSum == false).Sum(e => e.Number);
                            newSumItem.LastWeekNumber = lastsumItem.Count > 0 ? lastsumItem.Where(e => e.Class.Course.Department.Id == department.depId).Sum(e => e.Number) : 0;
                            if (itemIsNew) {
                                newSumItem.StudentPopulationId = studentPopulationData.Id;
                                dataContext.StudentPopulationItem.Add(newSumItem);
                            }
                            dataContext.SaveChanges();
                        }


                    }
                    //新增英文合計
                    Course sumenCourse = dataContext.Course.Where(e => e.Department.Id == 163 && e.Department.Subject == CourseSubject.English && e.IsSum == true).FirstOrDefault();
                    Class sumenClass = new Class();
                    if (dataContext.Class.Any(e => e.School.Id == schoolId && e.Course.Id == sumenCourse.Id)) {
                        sumenClass = dataContext.Class.Include("Course.Department").Where(e => e.School.Id == schoolId && e.Course.Id == sumenCourse.Id).FirstOrDefault();
                    }
                    else {
                        sumenClass = new Class() { SchoolId = schoolId, CourseId = sumenCourse.Id, Name = sumenCourse.Name };
                        dataContext.Class.Add(sumenClass);
                        dataContext.SaveChanges();
                    }
                    bool sumItemIsNew = true;
                    StudentPopulationItem newEnSumItem = new StudentPopulationItem();
                    if (dataContext.StudentPopulationItem.Any(e => e.Class.Id == sumenClass.Id && e.StudentPopulation.Id == studentPopulationData.Id)) {
                        sumItemIsNew = false;
                        newEnSumItem = dataContext.StudentPopulationItem.Where(e => e.Class.Id == sumenClass.Id && e.StudentPopulation.Id == studentPopulationData.Id).FirstOrDefault();
                    }
                    else {
                        sumItemIsNew = true;
                        newEnSumItem = new StudentPopulationItem();
                        newEnSumItem.IsSum = true;
                    }
                    newEnSumItem.ClassId = sumenClass.Id;
                    newEnSumItem.Name = sumenClass.Name;
                    int[] departmentIds = dataContext.CourseDepartment.Where(p => p.Subject == CourseSubject.English && p.IsSum == false).Select(p => p.Id).ToArray();
                    newEnSumItem.Number = sumItem.Where(e => departmentIds.Contains(e.Class.Course.Department.Id) && e.IsSum == false).Sum(e => e.Number);
                    newEnSumItem.LastWeekNumber = lastsumItem.Count > 0 ? lastsumItem.Where(e => e.Class.Id == sumenClass.Id).FirstOrDefault().Number : 0;
                    if (sumItemIsNew) {
                        newEnSumItem.StudentPopulationId = studentPopulationData.Id;
                        dataContext.StudentPopulationItem.Add(newEnSumItem);
                    }
                    dataContext.SaveChanges();
                    //計算英文總班數
                    Course countenCourse = dataContext.Course.Where(e => e.Name == "總班數" && e.Department.Subject == CourseSubject.English && e.IsSum == true).FirstOrDefault();
                    Class countenClass = new Class();
                    if (dataContext.Class.Any(e => e.School.Id == schoolId && e.Course.Id == countenCourse.Id)) {
                        countenClass = dataContext.Class.Include("Course.Department").Where(e => e.School.Id == schoolId && e.Course.Id == countenCourse.Id).FirstOrDefault();
                    }
                    else {
                        countenClass = new Class() { SchoolId = schoolId, CourseId = countenCourse.Id, Name = countenCourse.Name };
                        dataContext.Class.Add(countenClass);
                        dataContext.SaveChanges();
                    }
                    bool countItemIsNew = true;
                    StudentPopulationItem newEnCountItem = new StudentPopulationItem();
                    if (dataContext.StudentPopulationItem.Any(e => e.Class.Id == countenClass.Id && e.StudentPopulation.Id == studentPopulationData.Id)) {
                        countItemIsNew = false;
                        newEnCountItem = dataContext.StudentPopulationItem.Where(e => e.Class.Id == countenClass.Id && e.StudentPopulation.Id == studentPopulationData.Id).FirstOrDefault();
                    }
                    else {
                        countItemIsNew = true;
                        newEnCountItem = new StudentPopulationItem();
                        newEnCountItem.IsSum = true;
                    }
                    newEnCountItem.ClassId = countenClass.Id;
                    newEnCountItem.Name = countenClass.Name;
                    newEnCountItem.Number = sumItem.Where(e => e.Class.Course.Department.Subject == CourseSubject.English && e.IsSum == false).Count();
                    newEnCountItem.LastWeekNumber = lastsumItem.Count > 0 ? lastsumItem.Where(e => e.Class.Id == countenClass.Id).FirstOrDefault().Number : 0;
                    if (countItemIsNew) {
                        newEnCountItem.StudentPopulationId = studentPopulationData.Id;
                        dataContext.StudentPopulationItem.Add(newEnCountItem);
                    }
                    dataContext.SaveChanges();
                    //新增國文合計
                    Course sumchCourse = dataContext.Course.Where(e => e.Department.Id == 169 && e.Department.Subject == CourseSubject.Chinese && e.IsSum == true).FirstOrDefault();
                    if (sumchCourse == null) {
                        sumchCourse = dataContext.Course.Where(e => e.Department.Id == 165 && e.Department.Subject == CourseSubject.Chinese && e.IsSum == false).FirstOrDefault();
                    }
                    Class sumchClass = new Class();
                    if (dataContext.Class.Any(e => e.School.Id == schoolId && e.Course.Id == sumchCourse.Id)) {
                        sumchClass = dataContext.Class.Include("Course.Department").Where(e => e.School.Id == schoolId && e.Course.Id == sumchCourse.Id).FirstOrDefault();
                    }
                    else {
                        sumchClass = new Class() { SchoolId = schoolId, CourseId = sumchCourse.Id, Name = sumchCourse.Name };
                        dataContext.Class.Add(sumchClass);
                        dataContext.SaveChanges();
                    }
                    sumItemIsNew = true;
                    StudentPopulationItem newChSumItem = new StudentPopulationItem();
                    if (dataContext.StudentPopulationItem.Any(e => e.Class.Id == sumchClass.Id && e.StudentPopulation.Id == studentPopulationData.Id)) {
                        sumItemIsNew = false;
                        newChSumItem = dataContext.StudentPopulationItem.Where(e => e.Class.Id == sumchClass.Id && e.StudentPopulation.Id == studentPopulationData.Id).FirstOrDefault();
                    }
                    else {
                        sumItemIsNew = true;
                        newChSumItem = new StudentPopulationItem();
                        newChSumItem.IsSum = true;
                    }
                    newChSumItem.ClassId = sumchClass.Id;
                    newChSumItem.Name = sumchClass.Name;
                    int[] chDepartmentIds = dataContext.CourseDepartment.Where(p => p.Subject == CourseSubject.Chinese && p.IsSum == false).Select(p => p.Id).ToArray();
                    newChSumItem.Number = sumItem.Where(e => chDepartmentIds.Contains(e.Class.Course.Department.Id) && e.IsSum == false).Sum(e => e.Number);
                    newChSumItem.LastWeekNumber = lastsumItem.Count > 0 ? lastsumItem.Where(e => e.Class.Id == sumenClass.Id).FirstOrDefault().Number : 0;
                    if (sumItemIsNew) {
                        newChSumItem.StudentPopulationId = studentPopulationData.Id;
                        dataContext.StudentPopulationItem.Add(newChSumItem);
                    }
                    dataContext.SaveChanges();
                }
                catch (Exception ex) {
                    string e = ex.Message;
                }
            }
            else if (seleceedType == StudentPopulationType.GEPT) {

            }
            else if (seleceedType == StudentPopulationType.PS) {

            }

            studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week && e.Type == seleceedType).FirstOrDefault();// Model.GetStudentPopulation(schoolId, year, week);
            return PartialView("PopulationPartialView", studentPopulationData);
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
            StudentPopulation lastStudentPopulationData = dataContext.StudentPopulation.Include("Items").Where(e => e.School.Id == studentPopulationData.School.Id && e.Week < studentPopulationData.Week && e.Type == StudentPopulationType.PSJ).OrderByDescending(e => e.Id).FirstOrDefault();
            List<StudentPopulationItem> lastsumItem = new List<StudentPopulationItem>();
            if (lastStudentPopulationData != null && lastStudentPopulationData.HasValue()) {
                lastsumItem = dataContext.StudentPopulationItem.Include("Class.Course.Department").Where(e => e.StudentPopulationId == lastStudentPopulationData.Id).ToList();
            }
            //List<StudentPopulationItem> sumItem = dataContext.StudentPopulationItem.Include("Class.Course.Department").Where(e => e.StudentPopulationId == studentPopulationData.Id).ToList();

            //國小班加總 班系 國小班 158 課程 674 國小人數合計  681
            //團                    
            StudentPopulationItem sumEnElementaryGItem = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 674 && e.Class.Type == ClassType.Group && e.StudentPopulation.Id == studentPopulationData.Id);
            sumEnElementaryGItem.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 158 && e.Class.Course.Id != 162 && e.Class.Type == ClassType.Group && e.Class.Course.IsSum == false).Sum(e => e.Number);
            eNCount = eNCount + sumEnElementaryGItem.Number;
            dataContext.StudentPopulationItem.Update(sumEnElementaryGItem);
            //小組班 EV3
            StudentPopulationItem sumEnElementarySGItem = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 674 && e.Class.Type == ClassType.SubGroup && e.StudentPopulation.Id == studentPopulationData.Id);
            sumEnElementarySGItem.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 158 && e.Class.Course.Id != 162 && e.Class.Type == ClassType.SubGroup && e.Class.Course.IsSum == false).Sum(e => e.Number);
            eNCount = eNCount + sumEnElementarySGItem.Number;
            dataContext.StudentPopulationItem.Update(sumEnElementarySGItem);
            //三
            StudentPopulationItem sumEnElementaryV3Item = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 674 && e.Class.Type == ClassType.V3 && e.StudentPopulation.Id == studentPopulationData.Id);
            sumEnElementaryV3Item.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 158 && e.Class.Course.Id != 162 && e.Class.Type == ClassType.V3 && e.Class.Course.IsSum == false).Sum(e => e.Number);
            eNCount = eNCount + sumEnElementaryV3Item.Number;
            dataContext.StudentPopulationItem.Update(sumEnElementaryV3Item);
            dataContext.SaveChanges();

            //國中班加總 班系 國中班 159 課程 國中人數合計 681
            //團                    
            StudentPopulationItem sumEnJuniorHighGItem = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 681 && e.Class.Type == ClassType.Group);
            sumEnJuniorHighGItem.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 159 && e.Class.Course.Id != 162 && e.Class.Type == ClassType.Group && e.Class.Course.IsSum == false).Sum(e => e.Number);
            eNCount = eNCount + sumEnJuniorHighGItem.Number;

            //小組班 EV3
            StudentPopulationItem sumEnJuniorHighSGItem = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 681 && e.Class.Type == ClassType.SubGroup);
            sumEnJuniorHighSGItem.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 159 && e.Class.Course.Id != 162 && e.Class.Type == ClassType.SubGroup && e.Class.Course.IsSum == false).Sum(e => e.Number);
            eNCount = eNCount + sumEnJuniorHighSGItem.Number;
            //三
            StudentPopulationItem sumEnJuniorHighV3Item = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 681 && e.Class.Type == ClassType.V3);
            sumEnJuniorHighV3Item.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 159 && e.Class.Course.Id != 162 && e.Class.Type == ClassType.V3 && e.Class.Course.IsSum == false).Sum(e => e.Number);
            eNCount = eNCount + sumEnJuniorHighV3Item.Number;

            dataContext.SaveChanges();
            //Elite/英檢/sat班系
            ////團                    
            //StudentPopulationItem sumEnHighGItem = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Id == 685 && e.Class.Type == ClassType.Group);
            //sumEnHighGItem.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 158 && e.Class.Type == ClassType.Group && e.IsSum == false).Sum(e => e.Number);


            ////小組班 EV3
            //StudentPopulationItem sumEnHighSGItem = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Id == 685 && e.Class.Type == ClassType.SubGroup);
            //sumEnHighSGItem.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 158 && e.Class.Type == ClassType.SubGroup && e.IsSum == false).Sum(e => e.Number);

            ////三
            //StudentPopulationItem sumEnHighV3Item = studentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Id == 685 && e.Class.Type == ClassType.V3);
            //sumEnHighV3Item.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 158 && e.Class.Type == ClassType.V3 && e.IsSum == false).Sum(e => e.Number);


            //高中班加總 班系 高中課程 161 課程 高中人數合計 685
            //團                    
            StudentPopulationItem sumEnHighGItem = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 685 && e.Class.Type == ClassType.Group);
            sumEnHighGItem.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 173 && e.Class.Course.Id != 162 && e.Class.Type == ClassType.Group && e.Class.Course.IsSum == false).Sum(e => e.Number);
            eNCount = eNCount + sumEnHighGItem.Number;

            //小組班 EV3
            StudentPopulationItem sumEnHighSGItem = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 685 && e.Class.Type == ClassType.SubGroup);
            sumEnHighSGItem.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 173 && e.Class.Course.Id != 162 && e.Class.Type == ClassType.SubGroup && e.Class.Course.IsSum == false).Sum(e => e.Number);
            eNCount = eNCount + sumEnHighSGItem.Number;

            //三
            StudentPopulationItem sumEnHighV3Item = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 685 && e.Class.Type == ClassType.V3);
            sumEnHighV3Item.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 173 && e.Class.Course.Id != 162 && e.Class.Type == ClassType.V3 && e.Class.Course.IsSum == false).Sum(e => e.Number);
            eNCount = eNCount + sumEnHighV3Item.Number;

            dataContext.SaveChanges();

            //總班數 班系 英文合計 163 課程 英文總班數 686
            //團                    
            int[] docIds = new int[] { 158, 159, 173 };
            StudentPopulationItem allEnHighGClass = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 686 && e.Class.Type == ClassType.Group);
            if (studentPopulationData.Items.Any(e => docIds.Contains(e.Class.Course.Department.Id) && e.Class.Course.Id != 162 && e.Class.Type == ClassType.Group && e.IsSum == false)) {
                allEnHighGClass.Number = studentPopulationData.Items.Where(e => docIds.Contains(e.Class.Course.Department.Id) && e.Class.Course.Id != 162 && e.Class.Type == ClassType.Group && e.Class.Course.IsSum == false).Count();
            }
            else {
                allEnHighGClass.Number = 0;
            }

            //小組班 EV3
            StudentPopulationItem allEnHighSGClass = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 686 && e.Class.Type == ClassType.SubGroup);
            if (studentPopulationData.Items.Any(e => docIds.Contains(e.Class.Course.Department.Id) && e.Class.Course.Id != 162 && e.Class.Type == ClassType.SubGroup && e.IsSum == false)) {
                allEnHighSGClass.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 161 && e.Class.Course.Id != 162 && e.Class.Type == ClassType.SubGroup && e.Class.Course.IsSum == false).Count();
            }
            else {
                allEnHighSGClass.Number = 0;
            }

            //三
            StudentPopulationItem allEnHighV3Class = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 686 && e.Class.Type == ClassType.V3);
            if (studentPopulationData.Items.Any(e => docIds.Contains(e.Class.Course.Department.Id) && e.Class.Course.Id != 162 && e.Class.Type == ClassType.V3 && e.IsSum == false)) {
                allEnHighV3Class.Number = studentPopulationData.Items.Where(e => docIds.Contains(e.Class.Course.Department.Id) && e.Class.Course.Id != 162 && e.Class.Type == ClassType.V3 && e.Class.Course.IsSum == false).Count();
            }
            else {
                allEnHighV3Class.Number = 0;
            }

            //個別指導合計 英文個別指導 162 個別指導人數合計 840
            StudentPopulationItem personalItem = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 840 && e.Class.Type == ClassType.Personal);
            personalItem.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 162 && e.Class.Course.IsSum == false).Sum(e => e.Number);
            eNCount = eNCount + personalItem.Number;

            //本週英語文總人數 691
            StudentPopulationItem thisWeekEnCount = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 691);
            thisWeekEnCount.Number = eNCount;

            //上週英語文總人數 692
            StudentPopulationItem lastWeekEnCount = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 692);
            lastWeekEnCount.Number = lastStudentPopulationData != null && lastStudentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Id == 692) != null ? lastStudentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Id == 692).Number : 0;

            //與上週相比 693
            StudentPopulationItem comparedLastWeek = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 693);
            comparedLastWeek.Number = thisWeekEnCount.Number - lastWeekEnCount.Number;

            //去年同期/比 694 schoolId, year, week
            int lastYear = studentPopulationData.Year - 1;
            StudentPopulationItem lastYearData = dataContext.StudentPopulationItem.FirstOrDefault(e => e.StudentPopulation.Year == lastYear && e.StudentPopulation.Week == studentPopulationData.Week && e.Class.Course.Id == 691);
            StudentPopulationItem comparedLastYear = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 694);
            comparedLastYear.Number = eNCount - (lastYearData != null ? lastYearData.Number : 0);

            //國語文人數合計 班系 國語文 165 課程 國語文人數合計 719
            //團                    
            int cHCount = 0;
            StudentPopulationItem allChHighGClass = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 719 && e.Class.Type == ClassType.Group);
            allChHighGClass.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 165 && e.Class.Type == ClassType.Group && e.Class.Course.IsSum == false).Sum(e => e.Number);
            cHCount = cHCount + allChHighGClass.Number;

            //小組班 EV3
            StudentPopulationItem allChHighSGClass = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 719 && e.Class.Type == ClassType.SubGroup);
            allChHighSGClass.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 165 && e.Class.Type == ClassType.SubGroup && e.Class.Course.IsSum == false).Sum(e => e.Number);
            cHCount = cHCount + allChHighSGClass.Number;

            //三
            StudentPopulationItem allChHighV3Class = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 719 && e.Class.Type == ClassType.V3);
            allChHighV3Class.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 165 && e.Class.Type == ClassType.V3 && e.Class.Course.IsSum == false).Sum(e => e.Number);
            cHCount = cHCount + allChHighV3Class.Number;

            //國語文班數合計 班系 國語文 165 課程 總班數 718
            //團                                        
            StudentPopulationItem allChHighGCount = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 718 && e.Class.Type == ClassType.Group);
            allChHighGCount.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 165 && e.Class.Type == ClassType.Group && e.Class.Course.IsSum == false).Count();


            //小組班 EV3
            StudentPopulationItem allChHighSGCount = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 718 && e.Class.Type == ClassType.SubGroup);
            allChHighSGCount.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 165 && e.Class.Type == ClassType.SubGroup && e.Class.Course.IsSum == false).Count();


            //三
            StudentPopulationItem allChHighV3Count = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 718 && e.Class.Type == ClassType.V3);
            allChHighV3Count.Number = studentPopulationData.Items.Where(e => e.Class.Course.Department.Id == 165 && e.Class.Type == ClassType.V3 && e.Class.Course.IsSum == false).Count();

            //本週國語文總人數 708
            StudentPopulationItem thisWeekChCount = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 708);
            thisWeekChCount.Number = cHCount;

            //上週國語文總人數 709
            StudentPopulationItem lastWeekChCount = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 709);
            lastWeekChCount.Number = lastStudentPopulationData != null && lastStudentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Id == 708) != null ? lastStudentPopulationData.Items.FirstOrDefault(e => e.Class.Course.Id == 708).Number : 0;

            //與上週相比 716
            StudentPopulationItem comparedLastWeekCh = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 716);
            comparedLastWeekCh.Number = thisWeekChCount.Number - lastWeekChCount.Number;

            //去年同期/比 841 schoolId, year, week
            StudentPopulationItem lastYearChData = dataContext.StudentPopulationItem.FirstOrDefault(e => e.StudentPopulation.Year == lastYear && e.StudentPopulation.Week == studentPopulationData.Week && e.Class.Course.Id == 708);
            StudentPopulationItem comparedLastYearCh = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 841);
            comparedLastYearCh.Number = cHCount - (lastYearChData != null ? lastYearChData.Number : 0);

            //總人數 716
            StudentPopulationItem totle = dataContext.StudentPopulationItem.FirstOrDefault(e => e.Class.Course.Id == 715);
            totle.Number = eNCount + cHCount;
            dataContext.SaveChanges();
            #region 原加總程式

            ////加總
            ////取得上周資料
            //StudentPopulation lastStudentPopulationData = dataContext.StudentPopulation.Include("Items").Where(e => e.School.Id == schoolId && week < studentPopulationData.Week && e.Type == StudentPopulationType.PSJ).OrderByDescending(e => e.Id).FirstOrDefault();
            //List<StudentPopulationItem> lastsumItem = new List<StudentPopulationItem>();
            //if (lastStudentPopulationData != null && lastStudentPopulationData.HasValue()) {
            //    lastsumItem = dataContext.StudentPopulationItem.Include("Class.Course.Department").Where(e => e.StudentPopulationId == lastStudentPopulationData.Id).ToList();
            //}
            //List<StudentPopulationItem> sumItem = dataContext.StudentPopulationItem.Include("Class.Course.Department").Where(e => e.StudentPopulationId == studentPopulationData.Id).ToList();
            //var departmentGroup = sumItem.GroupBy(e => new { e.Class.Course.Department.Id }).Select(group => new { depId = group.Key.Id });
            //int lastWeekCount = 0;
            ////foreach (var department in departmentGroup) {
            ////    Course sumCourse = dataContext.Course.Where(e => e.Department.Id == department.depId && e.IsSum == true).FirstOrDefault();
            ////    if (sumCourse != null) {
            ////        Class sumClass = new Class();
            ////        if (dataContext.Class.Any(e => e.School.Id == schoolId && e.Course.Id == sumCourse.Id)) {
            ////            sumClass = dataContext.Class.Include("Course.Department").Where(e => e.School.Id == schoolId && e.Course.Id == sumCourse.Id).FirstOrDefault();
            ////        }
            ////        else {
            ////            sumClass = new Class() { SchoolId = schoolId, CourseId = sumCourse.Id, Name = sumCourse.Name };
            ////            dataContext.Class.Add(sumClass);
            ////            dataContext.SaveChanges();
            ////        }
            ////        //新增班系合計
            ////        bool itemIsNew = true;
            ////        StudentPopulationItem newSumItem = new StudentPopulationItem();
            ////        if (dataContext.StudentPopulationItem.Any(e => e.Class.Id == sumClass.Id && e.StudentPopulation.Id == studentPopulationData.Id)) {
            ////            itemIsNew = false;
            ////            newSumItem = dataContext.StudentPopulationItem.Where(e => e.Class.Id == sumClass.Id && e.StudentPopulation.Id == studentPopulationData.Id).FirstOrDefault();
            ////        }
            ////        else {
            ////            itemIsNew = true;
            ////            newSumItem = new StudentPopulationItem();
            ////            newSumItem.IsSum = true;
            ////        }
            ////        newSumItem.ClassId = sumClass.Id;
            ////        newSumItem.Name = sumClass.Name;
            ////        var ff = sumItem.Where(e => e.Class.Course.Department.Id == department.depId && e.IsSum == false).ToList();
            ////        newSumItem.Number = sumItem.Where(e => e.Class.Course.Department.Id == department.depId && e.IsSum == false).Sum(e => e.Number);
            ////        newSumItem.LastWeekNumber = lastsumItem.Count > 0 ? lastsumItem.Where(e => e.Class.Course.Department.Id == department.depId).Sum(e => e.Number) : 0;
            ////        if (itemIsNew) {
            ////            newSumItem.StudentPopulationId = studentPopulationData.Id;
            ////            dataContext.StudentPopulationItem.Add(newSumItem);
            ////        }
            ////        dataContext.SaveChanges();
            ////    }
            ////}
            ////新增英文合計
            ////國小班 P1~P6 SAT Juior AB  
            ////國中班 國一準特/特訓 國二準特/特訓 國三準特/特訓 海外特訓班 TOEFL	SSAT	PSAT
            ////Elite/sat班系 直接進總人數
            ////高中合計 高中小組班 高一 高二 高三
            ////總班數 計算到高中的各別班數(不含EM1)
            ////EM1合計 (EM1)國小	(EM1)國中	(EM1)高一&高二	(EM1)高三
            ////本週英語文總人數 所有英文班級人數
            ////上週英語文總人數 上週英文
            ////與上週相比  本週英語文總人數 - 上週英語文總人數
            ////去年同期 / 比 本週總人數 - 去年同週次總人數
            ////本週英語文新生 分校自填
            ////本週英語文流失 分校自填
            //Course sumenCourse = dataContext.Course.Where(e => e.Department.Id == 163 && e.Department.Subject == CourseSubject.English && e.IsSum == true).FirstOrDefault();
            //Class sumenClass = new Class();
            //if (dataContext.Class.Any(e => e.School.Id == schoolId && e.Course.Id == sumenCourse.Id)) {
            //    sumenClass = dataContext.Class.Include("Course.Department").Where(e => e.School.Id == schoolId && e.Course.Id == sumenCourse.Id).FirstOrDefault();
            //}
            //else {
            //    sumenClass = new Class() { SchoolId = schoolId, CourseId = sumenCourse.Id, Name = sumenCourse.Name };
            //    dataContext.Class.Add(sumenClass);
            //    dataContext.SaveChanges();
            //}
            //bool sumItemIsNew = true;
            //StudentPopulationItem newEnSumItem = new StudentPopulationItem();
            //if (dataContext.StudentPopulationItem.Any(e => e.Class.Id == sumenClass.Id && e.StudentPopulation.Id == studentPopulationData.Id)) {
            //    sumItemIsNew = false;
            //    newEnSumItem = dataContext.StudentPopulationItem.Where(e => e.Class.Id == sumenClass.Id && e.StudentPopulation.Id == studentPopulationData.Id).FirstOrDefault();
            //}
            //else {
            //    sumItemIsNew = true;
            //    newEnSumItem = new StudentPopulationItem();
            //    newEnSumItem.IsSum = true;
            //}
            //newEnSumItem.ClassId = sumenClass.Id;
            //newEnSumItem.Name = sumenClass.Name;
            //int[] departmentIds = dataContext.CourseDepartment.Where(p => p.Subject == CourseSubject.English && p.IsSum == false).Select(p => p.Id).ToArray();
            //newEnSumItem.Number = sumItem.Where(e => departmentIds.Contains(e.Class.Course.Department.Id) && e.IsSum == false).Sum(e => e.Number);
            //newEnSumItem.LastWeekNumber = lastsumItem.Count > 0 ? lastsumItem.Where(e => e.Class.Id == sumenClass.Id).FirstOrDefault().Number : 0;
            //if (sumItemIsNew) {
            //    newEnSumItem.StudentPopulationId = studentPopulationData.Id;
            //    dataContext.StudentPopulationItem.Add(newEnSumItem);
            //}
            //dataContext.SaveChanges();
            ////計算英文總班數
            //Course countenCourse = dataContext.Course.Where(e => e.Name == "總班數" && e.Department.Subject == CourseSubject.English && e.IsSum == true).FirstOrDefault();
            //Class countenClass = new Class();
            //if (dataContext.Class.Any(e => e.School.Id == schoolId && e.Course.Id == countenCourse.Id)) {
            //    countenClass = dataContext.Class.Include("Course.Department").Where(e => e.School.Id == schoolId && e.Course.Id == countenCourse.Id).FirstOrDefault();
            //}
            //else {
            //    countenClass = new Class() { SchoolId = schoolId, CourseId = countenCourse.Id, Name = countenCourse.Name };
            //    dataContext.Class.Add(countenClass);
            //    dataContext.SaveChanges();
            //}
            //bool countItemIsNew = true;
            //StudentPopulationItem newEnCountItem = new StudentPopulationItem();
            //if (dataContext.StudentPopulationItem.Any(e => e.Class.Id == countenClass.Id && e.StudentPopulation.Id == studentPopulationData.Id)) {
            //    countItemIsNew = false;
            //    newEnCountItem = dataContext.StudentPopulationItem.Where(e => e.Class.Id == countenClass.Id && e.StudentPopulation.Id == studentPopulationData.Id).FirstOrDefault();
            //}
            //else {
            //    countItemIsNew = true;
            //    newEnCountItem = new StudentPopulationItem();
            //    newEnCountItem.IsSum = true;
            //}
            //newEnCountItem.ClassId = countenClass.Id;
            //newEnCountItem.Name = countenClass.Name;
            //newEnCountItem.Number = sumItem.Where(e => e.Class.Course.Department.Subject == CourseSubject.English && e.IsSum == false).Count();
            //newEnCountItem.LastWeekNumber = lastsumItem.Count > 0 ? lastsumItem.Where(e => e.Class.Id == countenClass.Id).FirstOrDefault().Number : 0;
            //if (countItemIsNew) {
            //    newEnCountItem.StudentPopulationId = studentPopulationData.Id;
            //    dataContext.StudentPopulationItem.Add(newEnCountItem);
            //}
            //dataContext.SaveChanges();
            ////新增國文合計
            //Course sumchCourse = dataContext.Course.Where(e => e.Department.Id == 169 && e.Department.Subject == CourseSubject.Chinese && e.IsSum == true).FirstOrDefault();
            //if (sumchCourse == null) {
            //    sumchCourse = dataContext.Course.Where(e => e.Department.Id == 165 && e.Department.Subject == CourseSubject.Chinese && e.IsSum == false).FirstOrDefault();
            //}
            //Class sumchClass = new Class();
            //if (dataContext.Class.Any(e => e.School.Id == schoolId && e.Course.Id == sumchCourse.Id)) {
            //    sumchClass = dataContext.Class.Include("Course.Department").Where(e => e.School.Id == schoolId && e.Course.Id == sumchCourse.Id).FirstOrDefault();
            //}
            //else {
            //    sumchClass = new Class() { SchoolId = schoolId, CourseId = sumchCourse.Id, Name = sumchCourse.Name };
            //    dataContext.Class.Add(sumchClass);
            //    dataContext.SaveChanges();
            //}
            //sumItemIsNew = true;
            //StudentPopulationItem newChSumItem = new StudentPopulationItem();
            //if (dataContext.StudentPopulationItem.Any(e => e.Class.Id == sumchClass.Id && e.StudentPopulation.Id == studentPopulationData.Id)) {
            //    sumItemIsNew = false;
            //    newChSumItem = dataContext.StudentPopulationItem.Where(e => e.Class.Id == sumchClass.Id && e.StudentPopulation.Id == studentPopulationData.Id).FirstOrDefault();
            //}
            //else {
            //    sumItemIsNew = true;
            //    newChSumItem = new StudentPopulationItem();
            //    newChSumItem.IsSum = true;
            //}
            //newChSumItem.ClassId = sumchClass.Id;
            //newChSumItem.Name = sumchClass.Name;
            //int[] chDepartmentIds = dataContext.CourseDepartment.Where(p => p.Subject == CourseSubject.Chinese && p.IsSum == false).Select(p => p.Id).ToArray();
            //newChSumItem.Number = sumItem.Where(e => chDepartmentIds.Contains(e.Class.Course.Department.Id) && e.IsSum == false).Sum(e => e.Number);
            //newChSumItem.LastWeekNumber = lastsumItem.Count > 0 ? lastsumItem.Where(e => e.Class.Id == sumenClass.Id).FirstOrDefault().Number : 0;
            //if (sumItemIsNew) {
            //    newChSumItem.StudentPopulationId = studentPopulationData.Id;
            //    dataContext.StudentPopulationItem.Add(newChSumItem);
            //}
            //dataContext.SaveChanges();

            #endregion

            return studentPopulationData;
        }

    }
}
