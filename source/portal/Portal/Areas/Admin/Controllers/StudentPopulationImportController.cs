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
            return View();
        }

        [HttpPost]
        public IActionResult Import(IFormFile file, StudentPopulationType type, bool confirmed = false) {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "請選擇檔案" });

            using var db = new DataContext();

            ImportScanResult scan;
            using (var scanStream = file.OpenReadStream()) {
                scan = _importService.Scan(db, type, scanStream);
            }

            if (scan.Errors.Count > 0)
                return Json(new { success = false, message = string.Join("; ", scan.Errors) });

            var conflicts = scan.Items.Where(i => i.Exists).ToList();
            if (conflicts.Count > 0 && !confirmed) {
                return Json(new {
                    success = true,
                    requiresConfirmation = true,
                    conflicts = conflicts.Select(c => new { c.SchoolName, c.Year, c.Week }),
                });
            }

            ImportResult result;
            using (var importStream = file.OpenReadStream()) {
                result = _importService.Import(db, type, importStream, file.FileName);
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
