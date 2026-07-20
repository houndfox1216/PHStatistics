-- Revert: remove the shared "課輔分析總覽" CourseDepartment + 48 Course rows added by
-- 2026-07-20-as-grid-analysis-course-apply.sql.
-- WARNING: only safe if no StudentPopulationItem/Class rows reference Course.Id 478-525 yet
-- (i.e. before any real usage of the new grid). If data has already been written against
-- these courses, delete those Class/StudentPopulationItem rows first or this will fail on
-- the FK constraint.
DELETE FROM Course WHERE Id BETWEEN 478 AND 525;
DELETE FROM CourseDepartment WHERE Id = 46;
