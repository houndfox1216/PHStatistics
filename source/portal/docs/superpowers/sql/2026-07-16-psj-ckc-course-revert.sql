-- Revert: remove the CKC CourseDepartment + 33 Course rows added by
-- 2026-07-16-psj-ckc-course-apply.sql.
-- WARNING: only safe if no StudentPopulationItem/Class rows reference Course.Id 345-377 yet
-- (i.e. before any real PSJ import has run against the new courses). If PSJ CKC data has
-- already been imported, delete those Class/StudentPopulationItem rows first or this will
-- fail on the FK constraint.
DELETE FROM Course WHERE Id BETWEEN 345 AND 377;
DELETE FROM CourseDepartment WHERE Id = 39;
