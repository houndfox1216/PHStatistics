using System;
using System.Linq;
using System.Framework.Globalization;
using Microsoft.AspNetCore.Mvc;
using PHStatistics.Content;
using System.Framework;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    public class DashboardController : AdminBaseController {
        public IActionResult Index() {
            ViewBag.Title = "管理後台";

            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            var schoolYear = Model.DataContext.SchoolYear
                .Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime)
                .FirstOrDefault();

            if (schoolYear != null) {
                var populations = Model.DataContext.StudentPopulation
                    .Where(e => e.Year == schoolYear.Year && e.Week == schoolYear.Week)
                    .ToList();

                ViewBag.DocumentedCount = populations
                    .Where(e => e.Status == StudentPopulationStatus.Documented && e.SchoolId.HasValue)
                    .Select(e => e.SchoolId.Value)
                    .Distinct()
                    .Count();

                ViewBag.PendingCount = populations
                    .Where(e => e.Status == StudentPopulationStatus.Pending && e.SchoolId.HasValue)
                    .Select(e => e.SchoolId.Value)
                    .Distinct()
                    .Count();

                ViewBag.TotalInquiry = populations.Sum(e => e.TotalInquiryCount);
                ViewBag.SchoolYearLabel = $"{schoolYear.Year} 學年第 {schoolYear.Week} 週";
            } else {
                ViewBag.DocumentedCount = 0;
                ViewBag.PendingCount = 0;
                ViewBag.TotalInquiry = 0;
                ViewBag.SchoolYearLabel = "（非填報期間）";
            }

            return View();
        }
    }
}
