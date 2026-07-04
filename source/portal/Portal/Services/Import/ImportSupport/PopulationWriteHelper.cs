using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import.ImportSupport;

public static class PopulationWriteHelper {
    public static StudentPopulation GetOrCreatePopulation(DataContext db, int schoolId, int yearInt, int weekInt,
        SchoolYear schoolYear, StudentPopulationType type, string name, bool deleteExisting) {
        StudentPopulation pop;
        if (deleteExisting && db.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type)) {
            pop = db.StudentPopulation.First(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type);
            var delItems = db.StudentPopulationItem.Where(e => e.StudentPopulation.Id == pop.Id).ToList();
            var classIds = delItems.Select(e => e.ClassId).Distinct().ToList();
            db.StudentPopulationItem.RemoveRange(delItems);
            db.SaveChanges();
            var orphanClasses = db.Class
                .Where(e => classIds.Contains(e.Id) && !db.StudentPopulationItem.Any(i => i.ClassId == e.Id))
                .ToList();
            if (orphanClasses.Count > 0) {
                db.Class.RemoveRange(orphanClasses);
                db.SaveChanges();
            }
        }
        else if (db.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type)) {
            pop = db.StudentPopulation.First(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type);
        }
        else {
            pop = new StudentPopulation {
                SchoolId = schoolId,
                Year = yearInt,
                Week = schoolYear.Week.Value,
                WeekDate = schoolYear.WeekStartDate,
                Items = new System.Collections.Generic.List<StudentPopulationItem>(),
                Submitter = db.Member.Find(CourseMapping.DefaultSubmitterId),
                Type = type,
                Name = name,
            };
            db.StudentPopulation.Add(pop);
            db.SaveChanges();
        }
        return pop;
    }

    public static void AddClassAndItem(DataContext db, int schoolId, Course course, ClassType cType, long populationId, int number, ImportResult result, ILogger logger) {
        var newClass = new Class();
        try {
            int classCount = db.StudentPopulationItem.Count(e => e.Class.Course.Id == course.Id);
            newClass.Course = null;
            newClass.CourseId = course.Id;
            newClass.SchoolId = schoolId;
            newClass.Type = cType;
            newClass.Name = $"{course.Name}_{(classCount + 1):00}";
            db.Class.Add(newClass);
            db.SaveChanges();
        }
        catch (Exception ex) {
            logger?.LogError("ImportAll 新增班級錯誤: {msg}", ex.Message);
            return;
        }
        db.StudentPopulationItem.Add(new StudentPopulationItem {
            Class = null,
            ClassId = newClass.Id,
            Name = newClass.Name,
            Number = number,
            SchoolName = newClass.Name,
            LastWeekNumber = 0,
            StudentPopulation = null,
            StudentPopulationId = (int)populationId,
        });
        db.SaveChanges();
        result.ItemCount++;
    }
}
