-- Backfill missing Course 478-525 (課輔分析總覽: 上週比/新生/流失/總人數) Class + StudentPopulationItem
-- rows for AfterSchool (Type=4) StudentPopulation records created before those courses existed
-- (see 2026-07-20-as-grid-analysis-course-apply.sql). This is the batch equivalent of what
-- StudentPopulationController.CreateASGridPopulation already does on-demand for a single
-- school/week when someone opens that page — here applied once to every existing AS population
-- so nobody has to visit each page individually.
--
-- Target populations (42 rows; excludes Id=2383 which already has all 48 items from an earlier
-- manual page visit that exercised the on-demand backfill):
-- 88,89,87,112,5,111,115,114,113,214,215,213,250,275,247,274,308,309,307,368,369,367,364,427,
-- 428,426,486,1427,1425,1437,1442,1443,2231,2232,2241,2335,2336,2337,2375,2325,2321,2378
--
-- Computation mirrors AggregationEngine.Calculate exactly for these 3 StatisticsType values:
--   StatisticsType=10 (DiffWithLastWeek, 上週比 478-489): SUM(Number) - SUM(LastWeekNumber) over SourceCourseIds
--   StatisticsType=50 (ManualInput, 新生/流失 490-513): left at 0 (分校自填, matches app default)
--   StatisticsType=4  (SumBySourceCourses, 總人數 514-525): SUM(Number) over SourceCourseIds
-- SourceCourseIds filtering intentionally does NOT restrict by ClassType (GroupByClassType=0,
-- ApplicableClassType=NULL for all 48 courses), matching Course table config confirmed 2026-07-22.

SET NOCOUNT ON;
BEGIN TRANSACTION;

-- Step 1: create missing Class rows (SchoolId, CourseId, Type=General) for schools that have
-- never had one of these 48 courses assigned a Class yet (only 福山/22 already has all 48;
-- the other 6 schools with AS populations have none).
INSERT INTO Class (SchoolId, CourseId, Name, Type, DataMode)
SELECT DISTINCT sp.SchoolId, c.Id, c.Name, 5, 0
FROM StudentPopulation sp
CROSS JOIN Course c
WHERE sp.Type = 4
  AND c.Id BETWEEN 478 AND 525
  AND NOT EXISTS (
      SELECT 1 FROM Class cl WHERE cl.SchoolId = sp.SchoolId AND cl.CourseId = c.Id AND cl.Type = 5
  );

-- Step 2: create missing StudentPopulationItem rows for the 42 target populations only.
INSERT INTO StudentPopulationItem (StudentPopulationId, ClassId, Name, SchoolName, Number, LastWeekNumber, IsSum, DataMode)
SELECT
    sp.Id,
    cl.Id,
    c.Name,
    c.Name,
    CASE
        WHEN c.StatisticsType = 50 THEN 0
        WHEN c.StatisticsType = 10 THEN ISNULL(src.SumNumber, 0) - ISNULL(src.SumLastWeek, 0)
        WHEN c.StatisticsType = 4  THEN ISNULL(src.SumNumber, 0)
        ELSE 0
    END,
    0,
    1,
    0
FROM StudentPopulation sp
CROSS JOIN Course c
JOIN Class cl ON cl.SchoolId = sp.SchoolId AND cl.CourseId = c.Id AND cl.Type = 5
OUTER APPLY (
    SELECT SUM(spi2.Number) AS SumNumber, SUM(spi2.LastWeekNumber) AS SumLastWeek
    FROM StudentPopulationItem spi2
    JOIN Class cl2 ON cl2.Id = spi2.ClassId
    WHERE spi2.StudentPopulationId = sp.Id
      AND c.SourceCourseIds IS NOT NULL
      AND cl2.CourseId IN (SELECT value FROM OPENJSON(c.SourceCourseIds))
) src
WHERE sp.Type = 4
  AND c.Id BETWEEN 478 AND 525
  AND sp.Id IN (88,89,87,112,5,111,115,114,113,214,215,213,250,275,247,274,308,309,307,368,369,367,
                364,427,428,426,486,1427,1425,1437,1442,1443,2231,2232,2241,2335,2336,2337,2375,
                2325,2321,2378)
  AND NOT EXISTS (
      SELECT 1 FROM StudentPopulationItem spi WHERE spi.StudentPopulationId = sp.Id AND spi.ClassId = cl.Id
  );

COMMIT;
