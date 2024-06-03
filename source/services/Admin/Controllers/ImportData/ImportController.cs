using System;
using System.Collections.Generic;
using System.Framework;
using System.Framework.Data;
using System.Framework.Logging;
using System.Framework.Web;
using System.Linq;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.Helpers;
using DevExtreme.AspNet.Data.ResponseModel;
using DevExtreme.AspNet.Mvc;
using PHStatistics.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using System.Data;
using System.IO;
using PHStatistics.ImportRow;

namespace PHStatistics.Services.Admin.Controllers.ImportData {
    /// <summary>
    /// Page API
    /// </summary>
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class ImportController : ApiController<ServiceUser, PageModel, Culture> {
        /// <summary>
        /// 建構
        /// </summary>
        public ImportController() : base("System") { }

        /// <summary>
        /// 載入新聞資料來源
        /// </summary>
        /// <param name="keyword">關鍵字</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet("ImpordData")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, LoadResult>))]
        public JsonResponse ImpordData(string keyword) {
            try {
                using (DataContext dataContext = new DataContext()) {

                    //取得資料夾資料
                    string sourceDirectory = "C:\\Leo\\其他\\Kuri\\人數表\\20240429\\112匯入\\";
                    string schooleName = string.Empty;
                    var xlsxFiles = Directory.EnumerateFiles(sourceDirectory, "*.xlsx");
                    foreach (string currentFile in xlsxFiles) {
                        try {
                            schooleName = currentFile.Substring(sourceDirectory.Length + 1);
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
                            string year = "2023";
                            int yearInt = int.Parse(year);
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
                            List<ImportRow.ImportRow> RowData = new List<ImportRow.ImportRow>();

                            #region 解析表頭
                            for (int rNo = 0; rNo <= 3; rNo++) {
                                try {
                                    // foreach (int cNo = 1; cNo <= colCount; cNo++ )
                                    string colSrt = string.Empty;
                                    for (int cNo = 3; cNo <= colCount; cNo++) {
                                        ImportRow.ImportRow newRow = new ImportRow.ImportRow();
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
                                    foreach (ImportRow.ImportRow rItem in RowData.Where(p => p.RowNo == i).ToList()) {
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
                                    foreach (ImportRow.ImportRow rItem in RowData.Where(p => p.RowNo == i).ToList()) {
                                        ImportRow.ImportRow cDepRow = RowData.FirstOrDefault(p => p.RowNo == 0 && p.CellsNo == rItem.CellsNo);
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
                                                foreach (ImportRow.ImportRow cItem in RowData.Where(p => p.RowNo == 2 && p.CellsNo >= rItem.CellsNo && p.CellsNo < nextCoueseCNo).ToList()) {
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
                            DateTime weekDate = new DateTime();
                            for (int drNo = 3; drNo < rowCount; drNo++) {
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
                                    //取得週日期
                                    if (sheet.GetRow(drNo).Cells[1].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[0].ToString())) {
                                        weekDate = DateTime.Parse(string.Format("{0}/{1}", year, sheet.GetRow(drNo).Cells[1].ToString()));
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
                                    int classNo = 1;
                                    //判斷班系課程
                                    //取得班系
                                    string cdStr = string.Empty;
                                    string cStr = string.Empty;
                                    string c2Str = string.Empty;
                                    Course course = new Course();
                                    CourseDepartment courseDep = new CourseDepartment();
                                    for (int c = 3; c < dataColCount; c++) {
                                        Class newClass = new Class();
                                        Course checkCourse = new Course();
                                        try {
                                            if (sheet.GetRow(0).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(0).Cells[c].ToString())) {
                                                cdStr = sheet.GetRow(0).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", "");
                                            }
                                            if (sheet.GetRow(1).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(1).Cells[c].ToString())) {
                                                cStr = sheet.GetRow(1).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", "");
                                            }
                                            if (sheet.GetRow(2).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(2).Cells[c].ToString())) {
                                                c2Str = sheet.GetRow(2).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", "");
                                            }
                                            int checkCount = 0;

                                            courseDep = dataContext.CourseDepartment.FirstOrDefault(p => p.Name == cdStr);
                                            if (courseDep == null) {
                                                continue;
                                            }
                                            if (courseDep.Name.Equals("總人數") || courseDep.Name.Equals(@"Elite/英檢/sat班系") || courseDep.Name.Equals(@"本週總詢問(填單)人數")) {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == courseDep.Name);
                                            }
                                            else {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == cStr);
                                            }
                                            if (!checkCourse.HasValue()) {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == cStr + c2Str);
                                            }
                                            if (!checkCourse.HasValue()) {
                                                continue;
                                            }
                                            if (checkCourse.Name.Equals("本週總詢問(填單)人數") || checkCourse.Name.Equals("本週英語文總人數") || checkCourse.Name.Equals("本週英語文新生") ||
                                                checkCourse.Name.Equals("本週英語文流失") || checkCourse.Name.Equals("本週國語文總人數") || checkCourse.Name.Equals("本週國語文新生人數") ||
                                                checkCourse.Name.Equals("本週國語文流失人數") || checkCourse.Name.Equals("本週新增/流失") || checkCourse.Name.Equals("總人數")) {
                                                if (course.Id != checkCourse.Id) {
                                                    course = checkCourse;
                                                    classNo = 1;
                                                }
                                                else {
                                                    classNo++;
                                                }

                                                if (courseDep.HasValue() && course.HasValue()) {
                                                    string className = string.Format("{0}_{1}", course.Name, classNo.ToString("00"));
                                                    if (dataContext.Class.Any(p => p.Course.Id == course.Id && p.Name == className && p.Type == cType && p.School.Id == newSchool.Id)) {
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
                                            else {
                                                continue;
                                            }
                                        }
                                        catch (Exception ex) {
                                            string ds = ex.Message;
                                        }
                                        StudentPopulationItem newItem = new StudentPopulationItem();

                                        newItem.StudentPopulation.Week = week;
                                        newItem.StudentPopulation.Year = yearInt;
                                        newItem.StudentPopulation.WeekDate = weekDate;
                                        newItem.Class = newClass;
                                        newItem.StudentPopulation.Name = newItem.Class.Name;
                                        int count = 0;
                                        if (sheet.GetRow(drNo).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[c].ToString())) {
                                            try {
                                                count = int.Parse(sheet.GetRow(drNo).Cells[c].NumericCellValue.ToString());
                                            }
                                            catch {
                                                count = 0;
                                            }
                                        }
                                        else {
                                            continue;
                                        }
                                        newItem.Number = count;
                                        if (count == 0) {
                                            continue;
                                        }
                                        if (dataContext.StudentPopulation.Any(e => e.Year == yearInt && e.Week == newItem.StudentPopulation.Week && e.Items.Any(ie => ie.Class.Id == newItem.Class.Id))) {
                                            try {
                                                newItem.StudentPopulation = dataContext.StudentPopulation.FirstOrDefault(e => e.Year == yearInt && e.Week == newItem.StudentPopulation.Week && e.Items.Any(ie => ie.Class.Id == newItem.Class.Id));
                                                newItem.StudentPopulation.Week = week;
                                                newItem.StudentPopulation.Year = yearInt;
                                                newItem.StudentPopulation.WeekDate = weekDate;
                                                newItem.Class = newClass;
                                                newItem.StudentPopulation.Name = newItem.Class.Name;
                                                newItem.Number = count;
                                                dataContext.SaveChanges();
                                            }
                                            catch {

                                            }
                                        }
                                        else {
                                            dataContext.StudentPopulationItem.Add(newItem);
                                            dataContext.SaveChanges();
                                        }

                                    }
                                }
                                catch (Exception ex) {
                                    continue;
                                    //string f = ex.Message;

                                }

                            }
                        }
                        catch (Exception ex) {
                            string f = ex.Message;
                            continue;
                        }
                    }
                }
                return Json(ResponseStatus.OK);
            }
            catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            }
            catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
            }
        }

        /// <summary>
        /// 匯入人數資料
        /// </summary>
        /// <param name="keyword">關鍵字</param>
        /// <response code="200">請求已被處理，處理結果以 JSON 型態回應</response>
        [HttpGet("ImpordData2")]
        [Produces("application/json", Type = typeof(JsonResponse<ResponseStatus, LoadResult>))]
        public JsonResponse ImpordData2(string keyword) {
            try {
                using (DataContext dataContext = new DataContext()) {

                    //取得資料夾資料
                    string sourceDirectory = "C:\\Leo\\其他\\Kuri\\人數表\\20240517\\匯入\\";
                    string schooleName = string.Empty;
                    var xlsxFiles = Directory.EnumerateFiles(sourceDirectory, "*.xlsx");
                    foreach (string currentFile in xlsxFiles) {
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
                            string year = "2023";
                            int yearInt = int.Parse(year);
                            using (FileStream file = new FileStream(currentFile, FileMode.Open, FileAccess.Read)) {
                                xssfworkbook = new XSSFWorkbook(file);
                            }
                            ISheet sheet = xssfworkbook.GetSheetAt(0);
                            IRow headerRow = sheet.GetRow(1);
                            int colCount = headerRow.Cells.Count();
                            int rowCount = sheet.LastRowNum;
                            string countryName = string.Empty;
                            string brandName = string.Empty;
                            decimal pics = 0;
                            string sizeStr = string.Empty;
                            string exNo = string.Empty;
                            List<PHStatistics.ImportRow.ImportRow> RowData = new List<PHStatistics.ImportRow.ImportRow>();

                            #region 解析表頭
                            for (int rNo = 1; rNo <= 4; rNo++) {
                                try {
                                    // foreach (int cNo = 1; cNo <= colCount; cNo++ )
                                    string colSrt = string.Empty;
                                    for (int cNo = 3; cNo <= colCount; cNo++) {
                                        PHStatistics.ImportRow.ImportRow newRow = new PHStatistics.ImportRow.ImportRow();
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
                            int ordinal = 1;
                            for (int i = 1; i < 4; i++) {
                                //班系
                                if (i == 1) {
                                    foreach (PHStatistics.ImportRow.ImportRow rItem in RowData.Where(p => p.RowNo == i).ToList()) {
                                        //if (rItem.CellsContent.Contains("合計") || rItem.CellsContent.Contains("總計") || rItem.CellsContent.Contains("分析") || rItem.CellsContent.Contains("總人數")) {
                                        //    continue;
                                        //}
                                        if (dataContext.CourseDepartment.Any(p => p.Name == rItem.CellsContent)) {
                                            continue;
                                        }
                                        else {
                                            dataContext.CourseDepartment.Add(new CourseDepartment { DataMode = DataMode.Normal, Name = rItem.CellsContent, Ordinal = ordinal });
                                            dataContext.SaveChanges();
                                            ordinal++;
                                            //if (rItem.CellsContent.Equals("總人數") || rItem.CellsContent.Equals("Elite/英檢/sat班系") || rItem.CellsContent.Equals("本週總詢問(填單)人數")) {
                                            //    CourseDepartment cDep = dataContext.CourseDepartment.FirstOrDefault(e => e.Name == rItem.CellsContent);
                                            //    if (dataContext.Course.Any(p => p.Name == cDep.Name)) {
                                            //        continue;
                                            //    }
                                            //    else {
                                            //        dataContext.Course.Add(new Course { DataMode = DataMode.Normal, Name = cDep.Name, Department = cDep });
                                            //        dataContext.SaveChanges();
                                            //    }
                                            //}
                                        }
                                    }
                                }
                                //課程
                                ordinal = 1;
                                if (i == 2) {
                                    foreach (PHStatistics.ImportRow.ImportRow rItem in RowData.Where(p => p.RowNo == i).ToList()) {
                                        PHStatistics.ImportRow.ImportRow cDepRow = RowData.FirstOrDefault(p => p.RowNo == 1 && p.CellsNo == rItem.CellsNo);
                                        if (!cDepRow.HasValue()) {
                                            cDepRow = RowData.Where(p => p.RowNo == 1 && p.CellsNo <= rItem.CellsNo).OrderByDescending(p => p.CellsNo).FirstOrDefault();
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
                                            if (RowData.Any(p => p.RowNo == 2 && p.CellsNo > rItem.CellsNo)) {
                                                nextCoueseCNo = RowData.FirstOrDefault(p => p.RowNo == 2 && p.CellsNo > rItem.CellsNo).CellsNo;
                                            }
                                            else {
                                                nextCoueseCNo = rItem.CellsNo + 1;
                                            }
                                            if (RowData.Any(p => p.RowNo == 3 && p.CellsNo >= rItem.CellsNo && p.CellsNo < nextCoueseCNo)) {
                                                foreach (PHStatistics.ImportRow.ImportRow cItem in RowData.Where(p => p.RowNo == 3 && p.CellsNo >= rItem.CellsNo && p.CellsNo < nextCoueseCNo).ToList()) {
                                                    if (!dataContext.Course.Any(p => p.Name.Equals(courseTitle + cItem.CellsContent) && p.Department.Id == cDep.Id)) {
                                                        dataContext.Course.Add(new Course { DataMode = DataMode.Normal, Name = courseTitle + cItem.CellsContent, Department = cDep, Ordinal = ordinal });
                                                        dataContext.SaveChanges();
                                                        ordinal++;
                                                    }
                                                }
                                            }
                                            else {
                                                dataContext.Course.Add(new Course { DataMode = DataMode.Normal, Name = courseTitle, Department = cDep, Ordinal = ordinal });
                                                dataContext.SaveChanges();
                                                ordinal++;
                                            }
                                        }
                                    }
                                }
                            }
                            ordinal = dataContext.Course.Count();
                            CourseDepartment sumDep02 = dataContext.CourseDepartment.FirstOrDefault(e => e.Name == "Elite/英檢/sat班系");
                            if (!dataContext.Course.Any(p => p.Name == sumDep02.Name)) {
                                dataContext.Course.Add(new Course { DataMode = DataMode.Normal, Name = sumDep02.Name, Department = sumDep02, Ordinal = ordinal });
                                dataContext.SaveChanges();
                                ordinal++;
                            }

                            CourseDepartment sumDep03 = dataContext.CourseDepartment.FirstOrDefault(e => e.Name == "本週總詢問(填單)人數");
                            if (!dataContext.Course.Any(p => p.Name == sumDep03.Name)) {
                                dataContext.Course.Add(new Course { DataMode = DataMode.Normal, Name = sumDep03.Name, Department = sumDep03, Ordinal = ordinal });
                                dataContext.SaveChanges();
                                ordinal++;
                            }

                            CourseDepartment sumDep01 = dataContext.CourseDepartment.FirstOrDefault(e => e.Name == "總人數");
                            if (!dataContext.Course.Any(p => p.Name == sumDep01.Name)) {
                                dataContext.Course.Add(new Course { DataMode = DataMode.Normal, Name = sumDep01.Name, Department = sumDep01, Ordinal = ordinal });
                                dataContext.SaveChanges();
                                ordinal++;
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
                            DateTime weekDate = new DateTime();

                            ////取得週別
                            //if (sheet.GetRow(4).Cells[0].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(4).Cells[0].ToString())) {
                            //    try {
                            //        week = int.Parse(sheet.GetRow(4).Cells[0].ToString());
                            //    }
                            //    catch {
                            //        continue;
                            //    }

                            //}
                            //else {

                            //}
                            ////取得週日期
                            //if (sheet.GetRow(4).Cells[1].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(4).Cells[0].ToString())) {
                            //    weekDate = DateTime.Parse(string.Format("{0}/{1}", year, sheet.GetRow(4).Cells[1].ToString()));
                            //}

                            ////增加人數表主表
                            //StudentPopulation newStudentPopulation = new StudentPopulation();
                            //if (dataContext.StudentPopulation.Any(e => e.School.Id == newSchool.Id && e.Year == yearInt && e.Week == week)) {
                            //    newStudentPopulation = dataContext.StudentPopulation.FirstOrDefault(e => e.School.Id == newSchool.Id && e.Year == yearInt && e.Week == week);
                            //}
                            //else {
                            //    newStudentPopulation = new StudentPopulation();
                            //    newStudentPopulation.Status = StudentPopulationStatus.Approved;
                            //    newStudentPopulation.School = newSchool;
                            //    newStudentPopulation.Year = yearInt;
                            //    newStudentPopulation.Week = week;
                            //    newStudentPopulation.WeekDate = weekDate;
                            //    newStudentPopulation.Remark = "匯入";
                            //    newStudentPopulation.Name = string.Format("{0}-{1}年度-{2}週-人數表", newSchool.Name, year, week.ToString("00"));
                            //    dataContext.StudentPopulation.Add(newStudentPopulation);
                            //    dataContext.SaveChanges();
                            //}
                            //if(newStudentPopulation.Items.HasValue() && newStudentPopulation.Items.Count () > 0) {
                            //    dataContext.StudentPopulationItem.RemoveRange(dataContext.StudentPopulationItem.Where(e => e.StudentPopulation.Id == newStudentPopulation.Id).ToList());
                            //    dataContext.SaveChanges();
                            //}
                            //newStudentPopulation.Items = new List<StudentPopulationItem>(); 



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
                                    //取得週日期
                                    if (sheet.GetRow(drNo).Cells[1].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[0].ToString())) {
                                        weekDate = DateTime.Parse(string.Format("{0}/{1}", year, sheet.GetRow(drNo).Cells[1].ToString()));
                                    }

                                    //增加人數表主表
                                    StudentPopulation newStudentPopulation = new StudentPopulation();
                                    if (dataContext.StudentPopulation.Any(e => e.School.Id == newSchool.Id && e.Year == yearInt && e.Week == week)) {
                                        newStudentPopulation = dataContext.StudentPopulation.FirstOrDefault(e => e.School.Id == newSchool.Id && e.Year == yearInt && e.Week == week);
                                    }
                                    else {
                                        newStudentPopulation = new StudentPopulation();
                                        newStudentPopulation.Status = StudentPopulationStatus.Approved;
                                        newStudentPopulation.School = newSchool;
                                        newStudentPopulation.Year = yearInt;
                                        newStudentPopulation.Week = week;
                                        newStudentPopulation.WeekDate = weekDate;
                                        newStudentPopulation.Remark = "匯入";
                                        newStudentPopulation.Name = string.Format("{0}-{1}年度-{2}週-人數表", newSchool.Name, year, week.ToString("00"));
                                        dataContext.StudentPopulation.Add(newStudentPopulation);
                                        dataContext.SaveChanges();
                                    }
                                    if (newStudentPopulation.Items.HasValue() && newStudentPopulation.Items.Count() > 0) {
                                        dataContext.StudentPopulationItem.RemoveRange(dataContext.StudentPopulationItem.Where(e => e.StudentPopulation.Id == newStudentPopulation.Id).ToList());
                                        dataContext.SaveChanges();
                                    }
                                    newStudentPopulation.Items = new List<StudentPopulationItem>();

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
                                    int classNo = 1;
                                    //判斷班系課程
                                    //取得班系
                                    string cdStr = string.Empty;
                                    string cStr = string.Empty;
                                    string c2Str = string.Empty;
                                    Course course = new Course();
                                    CourseDepartment courseDep = new CourseDepartment();
                                    for (int c = 3; c < dataColCount; c++) {
                                        Class newClass = new Class();
                                        Course checkCourse = new Course();
                                        int count = 0;
                                        if (sheet.GetRow(1).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(1).Cells[c].ToString())) {
                                            cdStr = sheet.GetRow(1).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", "");
                                        }
                                        if (sheet.GetRow(drNo).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(drNo).Cells[c].ToString())) {
                                            try {
                                                count = int.Parse(sheet.GetRow(drNo).Cells[c].NumericCellValue.ToString());
                                            }
                                            catch {
                                                count = 0;
                                            }
                                        }
                                        else {
                                            continue;
                                        }
                                        if (count == 0) {
                                            continue;
                                        }
                                        try {

                                            if (sheet.GetRow(2).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(2).Cells[c].ToString())) {
                                                cStr = sheet.GetRow(2).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", "");
                                            }
                                            if (sheet.GetRow(3).Cells[c].HasValue() && !string.IsNullOrEmpty(sheet.GetRow(3).Cells[c].ToString())) {
                                                c2Str = sheet.GetRow(3).Cells[c].ToString().Trim().Replace("　", "").Replace(" ", "");
                                            }
                                            int checkCount = 0;

                                            courseDep = dataContext.CourseDepartment.FirstOrDefault(p => p.Name == cdStr);
                                            if (courseDep == null) {
                                                continue;
                                            }
                                            if (courseDep != null && (courseDep.Name.Equals("總人數") || courseDep.Name.Equals(@"Elite/英檢/sat班系") || courseDep.Name.Equals(@"本週總詢問(填單)人數"))) {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == courseDep.Name);
                                            }
                                            else {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == cStr);
                                            }
                                            if (!checkCourse.HasValue()) {
                                                checkCourse = dataContext.Course.FirstOrDefault(p => p.Department.Id == courseDep.Id && p.Name == cStr + c2Str);
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
                                                string className = string.Format("{0}_{1}", course.Name, classNo.ToString("00"));
                                                if (dataContext.Class.Any(p => p.Course.Id == course.Id && p.Name == className && p.Type == cType && p.School.Id == newSchool.Id)) {
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

                                            #region 只匯入總計班級
                                            //if (checkCourse.Name.Equals("本週總詢問(填單)人數") || checkCourse.Name.Equals("本週英語文總人數") || checkCourse.Name.Equals("本週英語文新生") ||
                                            //    checkCourse.Name.Equals("本週英語文流失") || checkCourse.Name.Equals("本週國語文總人數") || checkCourse.Name.Equals("本週國語文新生人數") ||
                                            //    checkCourse.Name.Equals("本週國語文流失人數") || checkCourse.Name.Equals("本週新增/流失") || checkCourse.Name.Equals("總人數")) {
                                            //    if (course.Id != checkCourse.Id) {
                                            //        course = checkCourse;
                                            //        classNo = 1;
                                            //    }
                                            //    else {
                                            //        classNo++;
                                            //    }

                                            //    if (courseDep.HasValue() && course.HasValue()) {
                                            //        string className = string.Format("{0}_{1}", course.Name, classNo.ToString("00"));
                                            //        if (dataContext.Class.Any(p => p.Course.Id == course.Id && p.Name == className && p.Type == cType && p.School.Id == newSchool.Id)) {
                                            //            newClass = dataContext.Class.FirstOrDefault(p => p.Course.Id == course.Id && p.Name == className && p.Type == cType && p.School.Id == newSchool.Id);
                                            //        }
                                            //        else {
                                            //            newClass.Course = course;
                                            //            newClass.Name = className;
                                            //            newClass.School = newSchool;
                                            //            newClass.Type = cType;
                                            //            dataContext.Class.Add(newClass);
                                            //            dataContext.SaveChanges();
                                            //        }
                                            //    }
                                            //}
                                            //else {
                                            //    continue;
                                            //}
                                            #endregion
                                        }
                                        catch (Exception ex) {
                                            string ds = ex.Message;
                                        }
                                        StudentPopulationItem newItem = new StudentPopulationItem();
                                        newItem.StudentPopulation = newStudentPopulation;
                                        newItem.Class = newClass;
                                        newItem.Number = count;
                                        dataContext.StudentPopulationItem.Add(newItem);
                                        dataContext.SaveChanges();
                                    }
                                }
                                catch (Exception ex) {
                                    continue;
                                    //string f = ex.Message;

                                }

                            }
                        }
                        catch (Exception ex) {
                            string f = ex.Message;
                            continue;
                        }
                    }
                }
                return Json(ResponseStatus.OK);
            }
            catch (FrameworkException fe) {
                return Json(ResponseStatus.InternalServerError, fe, fe.Message);
            }
            catch (Exception e) {
                Logger.LogError(e);
                return Json(ResponseStatus.InternalServerError, e, "系統忙碌中，請稍後再試");
            }
        }
    }
}
