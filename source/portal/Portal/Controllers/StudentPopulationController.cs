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
            lastWeekData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == lastschoolYear.Year && e.Week == lastschoolYear.Week && e.Type == StudentPopulationType.PH).FirstOrDefault();

            StudentPopulation returnData = new StudentPopulation();
            List<Course> courses = Model.DataContext.Course.ToList();
            ViewBag.Year = schoolYear.Year;
            ViewBag.Week = schoolYear.Week;

            ViewBag.Courses = courses;
            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value)) {
                returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == StudentPopulationType.PH);
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
                returnData.Type = StudentPopulationType.PH;
                //if(lastWeekData != null && lastWeekData.Items.Any()) {
                //    foreach (StudentPopulationItem sItem in lastWeekData.Items) {
                //        StudentPopulationItem newSItem = new StudentPopulationItem();
                //        newSItem.Class = sItem.Class;
                //        newSItem.Name = sItem.Name;
                //        newSItem.Number = sItem.Number;
                //        newSItem.LastWeekNumber = sItem.Number;
                //        newSItem.StudentRemark = sItem.StudentRemark;
                //        newSItem.Remark = sItem.Remark;
                //        newSItem.IsNew = false;
                //        returnData.Items.Add(newSItem);
                //    }
                //}
                //else {
                //    //新增總計欄位只新增班系及流失
                //    Course course = dataContext.Course.FirstOrDefault(e => e.Id == 1);
                //    StudentPopulationItem newSItem = new StudentPopulationItem();
                //    Class sumClass = new Class();
                //    if (dataContext.Class.Any(e => e.School.Id == schoolId && e.Course.Id == course.Id)) {
                //        sumClass = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id);
                //    }
                //    else {
                //        sumClass = new Class() { CourseId = 1, SchoolId = schoolId, Name = course.Name };
                //    }
                    
                //    newSItem.Class = sumClass;
                //    newSItem.Name = course.Name;
                //    newSItem.Number = 0;
                //    newSItem.LastWeekNumber = 0;
                //    newSItem.IsNew = false;
                //    returnData.Items.Add(newSItem);
                //    //foreach(Course courseItem in dataContext.Course.Where(e => e.IsSum == true)) {
                //    //    StudentPopulationItem newSItem = new StudentPopulationItem();
                //    //    newSItem.Class = new Class() { Course = courseItem, Name = courseItem.Name };
                //    //    newSItem.Name = courseItem.Name;
                //    //    newSItem.Number = 0;
                //    //    newSItem.LastWeekNumber = 0;
                //    //    newSItem.IsNew = false;
                //    //    returnData.Items.Add(newSItem);
                //    //}
                //}
                dataContext.StudentPopulation.Add(returnData);
                dataContext.SaveChanges();
            }
            if (Request.Method == "POST") {
                //進行人數表新增或更新
            }
            return View(returnData);
        }

        [Authorize(typeof(PortalUser))]
        public IActionResult CreatePSJPopulation(StudentPopulation data, int schoolId) {
            DataContext dataContext = new DataContext();
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.WeekEndDate >= dateTime).FirstOrDefault();
            SchoolYear lastschoolYear = dataContext.SchoolYear.Where(e => e.Id < schoolYear.Id).OrderByDescending(e => e.Id).FirstOrDefault();
            StudentPopulation lastWeekData = new StudentPopulation();
            lastWeekData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == lastschoolYear.Year && e.Week == lastschoolYear.Week && e.Type == StudentPopulationType.PH).FirstOrDefault();

            StudentPopulation returnData = new StudentPopulation();
            List<Course> courses = Model.DataContext.Course.ToList();
            ViewBag.Year = schoolYear.Year;
            ViewBag.Week = schoolYear.Week;

            ViewBag.Courses = courses;
            //if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value)) {
            //    returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.School.Id == schoolId && e.Year == schoolYear.Year.Value && e.Week == schoolYear.Week.Value && e.Type == StudentPopulationType.PH);
            //    foreach (StudentPopulationItem sItem in returnData.Items) {
            //        if (lastWeekData != null && lastWeekData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
            //            sItem.LastWeekNumber = lastWeekData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id).Number;
            //        }
            //    }
            //    dataContext.SaveChanges();
            //}
            //else {
            //    returnData = new StudentPopulation();
            //    returnData.School = dataContext.School.Find(schoolId);
            //    returnData.Year = schoolYear.Year.Value;
            //    returnData.Week = schoolYear.Week.Value;
            //    returnData.Name = string.Format("{0}第{1}週人數表", schoolYear.Year.ToString(), schoolYear.Week.ToString());
            //    returnData.WeekDate = schoolYear.WeekStartDate;
            //    returnData.Items = new List<StudentPopulationItem>();
            //    returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
            //    returnData.Type = StudentPopulationType.PH;
            //    if (lastWeekData != null && lastWeekData.Items.Any()) {
            //        foreach (StudentPopulationItem sItem in lastWeekData.Items) {
            //            if(sItem.Number > 0 && sItem.IsSum == false) {
            //                StudentPopulationItem newSItem = new StudentPopulationItem();
            //                newSItem.Class = sItem.Class;
            //                newSItem.Name = sItem.Name;
            //                newSItem.Number = sItem.Number;
            //                newSItem.LastWeekNumber = sItem.Number;
            //                newSItem.StudentRemark = sItem.StudentRemark;
            //                newSItem.Remark = sItem.Remark;
            //                newSItem.IsNew = false;
            //                returnData.Items.Add(newSItem);
            //            }
            //        }
            //    }
            //    else {
            //        //新增總計欄位只新增班系及流失
            //        Course course = dataContext.Course.FirstOrDefault(e => e.Name == "上週英語文總人數");
            //        StudentPopulationItem newSItem = new StudentPopulationItem();
            //        Class sumClass = new Class();
            //        if (dataContext.Class.Any(e => e.School.Id == schoolId && e.Course.Id == course.Id)) {
            //            sumClass = dataContext.Class.FirstOrDefault(e => e.School.Id == schoolId && e.Course.Id == course.Id);
            //        }
            //        else {
            //            sumClass = new Class() { CourseId = 1, SchoolId = schoolId, Name = course.Name };
            //        }

            //        newSItem.Class = sumClass;
            //        newSItem.Name = course.Name;
            //        newSItem.Number = 0;
            //        newSItem.LastWeekNumber = 0;
            //        newSItem.IsNew = false;
            //        returnData.Items.Add(newSItem);
            //        //foreach(Course courseItem in dataContext.Course.Where(e => e.IsSum == true)) {
            //        //    StudentPopulationItem newSItem = new StudentPopulationItem();
            //        //    newSItem.Class = new Class() { Course = courseItem, Name = courseItem.Name };
            //        //    newSItem.Name = courseItem.Name;
            //        //    newSItem.Number = 0;
            //        //    newSItem.LastWeekNumber = 0;
            //        //    newSItem.IsNew = false;
            //        //    returnData.Items.Add(newSItem);
            //        //}
            //    }
            //    dataContext.StudentPopulation.Add(returnData);
            //    dataContext.SaveChanges();
            //}
            if (Request.Method == "POST") {
                //進行人數表新增或更新
            }
            return View(returnData);
        }


        [Authorize(typeof(PortalUser))]
        [HttpPost("AddClass")]
        public IActionResult AddClass(int courseId, int schoolId, int year, int week, string[][] itemArr, string type) {
            List<Course> courses = Model.DataContext.Course.Include("Department").Where(e => e.Department.Company == Company.PH ).ToList();
            ViewBag.Courses = courses;
            DataContext dataContext = new DataContext();
            var seleceedType = type switch {
                "PH" => StudentPopulationType.PH,
                "PS" => StudentPopulationType.PS,
                "PHM" => StudentPopulationType.PHM,
                _ => StudentPopulationType.AfterSchool
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

            //檢查及加總
            
            studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week).FirstOrDefault();// Model.GetStudentPopulation(schoolId, year, week);
            return PartialView("PopulationPartialView", studentPopulationData);
        }

        [Authorize(typeof(PortalUser))]
        [HttpPost("AddNewClass")]
        // data: { 'schoolId': schoolId, 'courseId': newCourses.value, 'week': week, 'year': year, 'newClassType': newClassType, 'newClassName': newClassName, 'newNumber': newNumber, 'newStudentremark':newStudentremark },
        public IActionResult AddNewClass(int courseId, int schoolId, int year, int week, string[][] itemArr, int newClassType, string newClassName, int newNumber, string newStudentremark, string type) {
            
            List<Course> courses = Model.DataContext.Course.ToList();
            ViewBag.Courses = courses;
            DataContext dataContext = new DataContext();
            var seleceedType = type switch {
                "PH" => StudentPopulationType.PH,
                "PS" => StudentPopulationType.PS,
                "PHM" => StudentPopulationType.PHM,
                _ => StudentPopulationType.AfterSchool
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

                //加總
                //取得上周資料
                StudentPopulation lastStudentPopulationData = dataContext.StudentPopulation.Include("Items").Where(e => e.School.Id == schoolId && week < studentPopulationData.Week).OrderByDescending(e => e.Id).FirstOrDefault();
                List<StudentPopulationItem> lastsumItem = new List<StudentPopulationItem>();
                if(lastStudentPopulationData != null && lastStudentPopulationData.HasValue()) {
                    lastsumItem = dataContext.StudentPopulationItem.Include("Class.Course.Department").Where(e => e.StudentPopulationId == lastStudentPopulationData.Id).ToList();
                }                
                List<StudentPopulationItem> sumItem = dataContext.StudentPopulationItem.Include("Class.Course.Department").Where(e => e.StudentPopulationId == studentPopulationData.Id).ToList(); 
                var departmentGroup = sumItem.GroupBy(e => new { e.Class.Course.Department.Id }).Select(group => new { depId = group.Key.Id });
                int lastWeekCount = 0;
                foreach ( var department in departmentGroup) {
                    Course sumCourse = dataContext.Course.Where(e => e.Department.Id == department.depId && e.IsSum == true).FirstOrDefault();
                    if(sumCourse != null) {
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
            studentPopulationData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week).FirstOrDefault();// Model.GetStudentPopulation(schoolId, year, week);
            return PartialView("PopulationPartialView", studentPopulationData);
        }

    }
}
