using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [RequirePermission(SystemPermission.StudentPopulationImport)]
    public class StudentPopulationImportController : AdminBaseController {
        private readonly PopulationImportService _importService;

        public StudentPopulationImportController(PopulationImportService importService) {
            _importService = importService;
        }

        public IActionResult Index() {
            ViewBag.Title = "人數表匯入";
            using var db = new DataContext();
            int currentMaxYear = db.SchoolYear.Max(e => e.Year) ?? 0;
            ViewBag.SchoolYears = db.SchoolYear.Where(e => e.Year == currentMaxYear).OrderBy(e => e.Week).ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Import(IFormFile file, StudentPopulationType type, bool confirmed = false, int? year = null, int? week = null) {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "請選擇檔案" });

            using var db = new DataContext();

            ImportScanResult scan;
            using (var scanStream = file.OpenReadStream()) {
                scan = _importService.Scan(db, type, scanStream, year, week);
            }

            if (scan.Errors.Count > 0)
                return Json(new { success = false, message = string.Join("; ", scan.Errors) });

            var conflicts = scan.Items.Where(i => i.Exists).ToList();
            if (conflicts.Count > 0 && !confirmed) {
                return Json(new {
                    success = true,
                    requiresConfirmation = true,
                    conflicts = conflicts.Select(c => new { schoolName = c.SchoolName, year = c.Year, week = c.Week }),
                });
            }

            ImportResult result;
            using (var importStream = file.OpenReadStream()) {
                result = _importService.Import(db, type, importStream, file.FileName, year, week);
            }

            return Json(new {
                success = true,
                requiresConfirmation = false,
                schoolCount = result.SchoolCount,
                itemCount = result.ItemCount,
                errors = result.Errors,
            });
        }
    }
}
