-- Revert: remove the 數學班/理化班 CourseDepartment triads + 100 Course rows added by
-- 2026-07-20-as-math-science-course-apply.sql.
-- WARNING: only safe if no StudentPopulationItem/Class rows reference Course.Id 378-477 yet
-- (i.e. before any real import/manual entry has used the new courses). If data has already
-- been written against these courses, delete those Class/StudentPopulationItem rows first or
-- this will fail on the FK constraint.
DELETE FROM Course WHERE Id BETWEEN 378 AND 477;
DELETE FROM CourseDepartment WHERE Id BETWEEN 40 AND 45;
