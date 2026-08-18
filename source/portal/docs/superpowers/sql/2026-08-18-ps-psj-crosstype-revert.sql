-- Reverts 2026-08-18-ps-psj-crosstype-apply.sql back to the exact pre-image values
-- (verified via SELECT against production on 2026-08-18, before any change was applied).

UPDATE Course
SET IsSum = 0,
    StatisticsType = 4, -- SumBySourceCourses
    SourceCourseIds = '[157]',
    NegativeSourceCourseIds = NULL,
    SourceStudentPopulationType = NULL
WHERE Id = 135;

UPDATE Course
SET StatisticsType = NULL,
    SourceCourseIds = NULL,
    NegativeSourceCourseIds = NULL,
    SourceStudentPopulationType = NULL
WHERE Id = 139;

UPDATE Course
SET StatisticsType = NULL,
    SourceCourseIds = NULL,
    NegativeSourceCourseIds = NULL,
    SourceStudentPopulationType = NULL
WHERE Id = 142;

SELECT Id, Name, IsSum, StatisticsType, SourceCourseIds, NegativeSourceCourseIds, SourceStudentPopulationType
FROM Course WHERE Id IN (135, 139, 142) ORDER BY Id;
