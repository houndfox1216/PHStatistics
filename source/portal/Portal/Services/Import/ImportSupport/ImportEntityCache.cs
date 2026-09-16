using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import.ImportSupport;

// Course/School 在整次匯入過程中不會變動（575筆課程/34間分校），但PHSheetReader建欄位對照表
// 逐欄查一次DB、PSJ/AS逐儲存格查一次課程DB，正式環境每次往返都疊加跨海延遲，是09-11記憶記錄的
// 主要瓶頸之一。改成依DataContext實例快取（ImportAll全程共用同一個db，只在第一次查詢時載入全表），
// 把逐欄/逐列各自查DB降為整批匯入只查兩次。
public static class ImportEntityCache {
    private class Cache {
        public List<Course> Courses;
        public Dictionary<int, Course> CoursesById;
        public Dictionary<string, School> SchoolsByName;
    }

    private static readonly ConditionalWeakTable<DataContext, Cache> _caches = new();

    private static Cache GetOrBuild(DataContext db) {
        if (_caches.TryGetValue(db, out var cache)) return cache;
        cache = new Cache {
            Courses = db.Course.Include("Department").ToList(),
            SchoolsByName = db.School.AsEnumerable().Where(s => s.Name != null).GroupBy(s => s.Name).ToDictionary(g => g.Key, g => g.First()),
        };
        cache.CoursesById = cache.Courses.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());
        _caches.Add(db, cache);
        return cache;
    }

    public static Course FindCourseById(DataContext db, int id) {
        var cache = GetOrBuild(db);
        return cache.CoursesById.TryGetValue(id, out var course) ? course : null;
    }

    public static Course FindCourseByName(DataContext db, string name, StudentPopulationType type) {
        if (string.IsNullOrEmpty(name)) return null;
        var cache = GetOrBuild(db);
        return cache.Courses.FirstOrDefault(c => c.Name == name && c.Type == type);
    }

    public static School FindSchoolByName(DataContext db, string name) {
        if (string.IsNullOrEmpty(name)) return null;
        var cache = GetOrBuild(db);
        return cache.SchoolsByName.TryGetValue(name, out var school) ? school : null;
    }
}
