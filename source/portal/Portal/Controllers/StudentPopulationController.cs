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

namespace PHStatistics.Portal.Controllers {
    public class StudentPopulationController : MvcController<PortalUser, Model, Culture> {
        public StudentPopulationController() : base("System") { }

        [Authorize(typeof(PortalUser))]
        public IActionResult Index() {
            List<SchoolAssignment> schools = Model.GetMemberSchool(User.Id);
            ViewBag.Schools = schools;
            if (schools == null || schools.Count <= 0) {
                Redirect("StudentPopulation/CreatePopulation");
            }
            return View();
        }

        [Authorize(typeof(PortalUser))]
        public IActionResult CreatePopulation(StudentPopulation data, int schoolId) {
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            DataContext dataContext = new DataContext();
            StudentPopulation lastWeekData = Model.GetLastStudentPopulation(schoolId, dateTime.Year);
            StudentPopulation returnData = new StudentPopulation();
            List<Course> courses = Model.DataContext.Course.ToList();
            ViewBag.Year = lastWeekData.Year;
            ViewBag.Week = lastWeekData.Week + 1;
            ViewBag.Courses = courses;
            if (dataContext.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == 2023 && e.Week == 2)) {
                returnData = dataContext.StudentPopulation.Include("Submitter").Include("School").Include("Items.Class.Course").FirstOrDefault(e => e.School.Id == schoolId && e.Year == 2023 && e.Week == 2);
                foreach (StudentPopulationItem sItem in returnData.Items) {
                    if (lastWeekData.Items.Any(e => e.Class.Id == sItem.Class.Id)) {
                        sItem.LastWeekNumber = lastWeekData.Items.FirstOrDefault(e => e.Class.Id == sItem.Class.Id).Number;
                    }
                }
                dataContext.SaveChanges();
            }
            else {
                returnData = new StudentPopulation();
                returnData.School = dataContext.School.Find(schoolId);
                returnData.Year = 2023;
                returnData.Week = 2;
                returnData.Items = new List<StudentPopulationItem>();
                returnData.Submitter = dataContext.Member.Find(Guid.Parse(User.Id));
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
    }
}
