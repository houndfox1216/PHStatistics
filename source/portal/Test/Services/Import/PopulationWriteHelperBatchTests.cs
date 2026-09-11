using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PHStatistics;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Test.Services.Import;

// 驗證 PopulationWriteHelper.AddClassAndItem 效能優化（延後 SaveChanges、classCount 快取）不破壞既有行為。
// 寫真實資料到本機 dev DB，測完自行清除，標成 [Explicit] 避免一般 `dotnet test` 意外連到目前設定的DB。
[TestFixture]
[Explicit("Writes real rows to whichever DB appsettings.json currently points at; run manually against the local dev DB only")]
public class PopulationWriteHelperBatchTests {
    private const string ClassNamePrefix = "TDD_BatchTest_";

    private static (School school, Course course) PickFixture(DataContext db) {
        var course = db.Course.First(c => c.Type == StudentPopulationType.PSJ);
        var school = db.School.First();
        return (school, course);
    }

    private static void Cleanup(DataContext db, long populationId) {
        var items = db.StudentPopulationItem.Where(i => i.StudentPopulationId == populationId).ToList();
        var classIds = items.Select(i => i.ClassId).ToList();
        db.StudentPopulationItem.RemoveRange(items);
        db.SaveChanges();
        db.Class.RemoveRange(db.Class.Where(c => classIds.Contains(c.Id)));
        db.StudentPopulation.RemoveRange(db.StudentPopulation.Where(p => p.Id == populationId));
        db.SaveChanges();
    }

    [Test]
    public void AddClassAndItem_DoesNotPersistUntilCallerCallsSaveChanges() {
        using var db = new DataContext();
        var (school, course) = PickFixture(db);
        var schoolYear = db.SchoolYear.First();
        var pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, schoolYear.Year.Value, schoolYear.Week.Value,
            schoolYear, StudentPopulationType.PSJ, "TDD batch test population", true);
        var result = new ImportResult();

        try {
            PopulationWriteHelper.AddClassAndItem(db, school.Id, course, ClassType.General, pop.Id, 5, result,
                NullLogger.Instance, className: ClassNamePrefix + "unsaved");

            using var freshDb = new DataContext();
            bool visibleBeforeSave = freshDb.StudentPopulationItem
                .Any(i => i.StudentPopulationId == pop.Id && i.Name == ClassNamePrefix + "unsaved");
            Assert.That(visibleBeforeSave, Is.False, "AddClassAndItem 不該在呼叫端存檔前就自己寫入DB");

            db.SaveChanges();

            using var freshDb2 = new DataContext();
            var saved = freshDb2.StudentPopulationItem
                .Include(i => i.Class)
                .First(i => i.StudentPopulationId == pop.Id && i.Name == ClassNamePrefix + "unsaved");
            Assert.That(saved.Number, Is.EqualTo(5));
            Assert.That(saved.Class.CourseId, Is.EqualTo(course.Id));
        }
        finally {
            Cleanup(db, pop.Id);
        }
    }

    [Test]
    public void AddClassAndItem_SharedCacheAcrossBatchedCalls_AssignsSequentialNamesWithoutCollision() {
        using var db = new DataContext();
        var (school, course) = PickFixture(db);
        var schoolYear = db.SchoolYear.First();
        var pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, schoolYear.Year.Value, schoolYear.Week.Value,
            schoolYear, StudentPopulationType.PSJ, "TDD batch test population", true);
        var result = new ImportResult();

        try {
            int baseline = db.StudentPopulationItem.Count(i => i.Class.Course.Id == course.Id);
            var cache = new Dictionary<int, int>();

            // 兩次呼叫之間刻意不SaveChanges，模擬批次延後存檔；若沒有共用快取，
            // 兩次都會查到同一個尚未寫入的baseline，產生重複班名。
            PopulationWriteHelper.AddClassAndItem(db, school.Id, course, ClassType.General, pop.Id, 1, result,
                NullLogger.Instance, classCountCache: cache);
            PopulationWriteHelper.AddClassAndItem(db, school.Id, course, ClassType.General, pop.Id, 1, result,
                NullLogger.Instance, classCountCache: cache);
            db.SaveChanges();

            var names = db.StudentPopulationItem
                .Include(i => i.Class)
                .Where(i => i.StudentPopulationId == pop.Id)
                .Select(i => i.Class.Name)
                .OrderBy(n => n)
                .ToList();

            Assert.That(names, Has.Count.EqualTo(2));
            Assert.That(names[0], Is.Not.EqualTo(names[1]), "共用快取應讓兩筆在同一批次內取得不同的預設班名編號");
            Assert.That(names[0], Is.EqualTo($"{course.Name}_{(baseline + 1):00}"));
            Assert.That(names[1], Is.EqualTo($"{course.Name}_{(baseline + 2):00}"));
        }
        finally {
            Cleanup(db, pop.Id);
        }
    }
}
