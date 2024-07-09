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

namespace PHStatistics.Portal.Controllers {
    public class StudentPopulationController : MvcController<PortalUser, Model, Culture> {
        public StudentPopulationController() : base("System") { }

        [Authorize(typeof(PortalUser))]
        public IActionResult Index() {
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
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.WeekEndDate >= dateTime).FirstOrDefault();
            List<SchoolAssignment> schools = Model.GetMemberSchool(User.Id);
            ViewBag.Schools = schools;
            ViewBag.CanEdit = schoolYear != null;
            if (schools == null || schools.Count <= 0) {
                Redirect("StudentPopulation/CreatePopulation");
            }
            return View();
        }

        [Authorize(typeof(PortalUser))]
        public IActionResult CreatePopulation(StudentPopulation data, int schoolId) {
            DataContext dataContext = new DataContext();
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.WeekEndDate >= dateTime).FirstOrDefault();
            SchoolYear lastschoolYear = dataContext.SchoolYear.Where(e => e.Id < schoolYear.Id).OrderByDescending(e => e.Id).FirstOrDefault();
            StudentPopulation lastWeekData = new StudentPopulation();
            lastWeekData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == lastschoolYear.Year && e.Week == lastschoolYear.Week).FirstOrDefault();

            StudentPopulation returnData = new StudentPopulation();
            List<Course> courses = Model.DataContext.Course.ToList();
            ViewBag.Year = schoolYear.Year;
            ViewBag.Week = schoolYear.Week;

            ViewBag.Courses = courses;
            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value)) {
                returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value);
                foreach (StudentPopulationItem sItem in returnData.Items) {
                    if (lastWeekData != null && lastWeekData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                        sItem.LastWeekNumber = lastWeekData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id).Number;
                    }
                }
                dataContext.SaveChanges();
            }
            else {
                returnData = new StudentPopulation();
                returnData.School = dataContext.School.Find(schoolId);
                returnData.Year = schoolYear.Year.Value;
                returnData.Week = schoolYear.Week.Value;
                returnData.Name = string.Format("{0}第{1}週人數表",schoolYear.Year.ToString(), schoolYear.Week.ToString());
                returnData.WeekDate = schoolYear.WeekStartDate;
                returnData.Items = new List<StudentPopulationItem>();
                returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
                if(lastWeekData != null && lastWeekData.Items.Any()) {
                    foreach (StudentPopulationItem sItem in lastWeekData.Items) {
                        StudentPopulationItem newSItem = new StudentPopulationItem();
                        newSItem.Class = sItem.Class;
                        newSItem.Name = sItem.Name;
                        newSItem.Number = sItem.Number;
                        newSItem.LastWeekNumber = sItem.Number;
                        newSItem.StudentRemark = sItem.StudentRemark;
                        newSItem.Remark = sItem.Remark;
                        newSItem.IsNew = false;
                        returnData.Items.Add(newSItem);
                    }
                }
                else {
                    foreach(Course courseItem in dataContext.Course.Where(e => e.IsSum == true)) {
                        StudentPopulationItem newSItem = new StudentPopulationItem();
                        newSItem.Class = new Class() { Course = courseItem, Name = courseItem.Name };
                        newSItem.Name = courseItem.Name;
                        newSItem.Number = 0;
                        newSItem.LastWeekNumber = 0;
                        newSItem.IsNew = false;
                        returnData.Items.Add(newSItem);
                    }
                }
                dataContext.StudentPopulation.Add(returnData);
                dataContext.SaveChanges();
            }
            if (Request.Method == "POST") {
                //進行人數表新增或更新
            }
            return View(returnData);
        }

        [Authorize(typeof(PortalUser))]
        [HttpPost("AddClass")]
        public IActionResult AddClass(int courseId, int schoolId, int year, int week, string[][] itemArr) {
            List<Course> courses = Model.DataContext.Course.ToList();
            ViewBag.Courses = courses;
            DataContext dataContext = new DataContext();
            StudentPopulation studentPopulationData = Model.GetStudentPopulation(schoolId, year, week);
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
            studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week).FirstOrDefault();// Model.GetStudentPopulation(schoolId, year, week);
            return PartialView("PopulationPartialView", studentPopulationData);
        }

        [Authorize(typeof(PortalUser))]
        [HttpPost("AddNewClass")]
        // data: { 'schoolId': schoolId, 'courseId': newCourses.value, 'week': week, 'year': year, 'newClassType': newClassType, 'newClassName': newClassName, 'newNumber': newNumber, 'newStudentremark':newStudentremark },
        public IActionResult AddNewClass(int courseId, int schoolId, int year, int week, string[][] itemArr, int newClassType, string newClassName, int newNumber, string newStudentremark) {
            List<Course> courses = Model.DataContext.Course.ToList();
            ViewBag.Courses = courses;
            DataContext dataContext = new DataContext();
            StudentPopulation studentPopulationData = Model.GetStudentPopulation(schoolId, year, week);
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
                    if(newClassType == 0) {
                        newClass.Type = ClassType.Group;
                    }
                    else if(newClassType == 1) {
                        newClass.Type = ClassType.Personal;
                    }
                    else if (newClassType == 2) {
                        newClass.Type = ClassType.SubGroup;
                    }
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
                addItem.Number = newNumber;
                addItem.SchoolName = newClassName;
                addItem.LastWeekNumber = 0;
                addItem.StudentPopulation = null;
                addItem.StudentPopulationId = studentPopulationData.Id;
                dataContext.StudentPopulationItem.Add(addItem);
                dataContext.SaveChanges();
            }
            catch (Exception ex) {
                string e = ex.Message;
            }
            studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week).FirstOrDefault();// Model.GetStudentPopulation(schoolId, year, week);
            return PartialView("PopulationPartialView", studentPopulationData);
        }

    }
}
