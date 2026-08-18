-- 2026-08-18: PS報表135(PSJ總人數)/139(PSJ上週人數)/142(本週變更(PS+PSJ)) 改接真實PSJ資料
-- 對應設計：source/portal/docs/superpowers/specs/2026-08-17-ps-psj-crosstype-total-design.md
-- 套用前確認過的現況（135: IsSum=0,StatisticsType=4,SourceCourseIds='[157]'；139/142: IsSum=1,StatisticsType=NULL），
-- 見同目錄 2026-08-18-ps-psj-crosstype-revert.sql 的還原值。

UPDATE Course
SET IsSum = 1,
    StatisticsType = 15, -- SumFromOtherType
    SourceCourseIds = '[562,563,564,565,566,567,568,569,570,571,572,573]',
    SourceStudentPopulationType = 1 -- PSJ
WHERE Id = 135;

UPDATE Course
SET StatisticsType = 16, -- LastWeekValueFromOtherType
    SourceCourseIds = '[562,563,564,565,566,567,568,569,570,571,572,573]',
    SourceStudentPopulationType = 1 -- PSJ
WHERE Id = 139;

UPDATE Course
SET StatisticsType = 12, -- DiffBetweenCourses
    SourceCourseIds = '[132,135]',
    NegativeSourceCourseIds = '[138,139]'
WHERE Id = 142;

SELECT Id, Name, IsSum, StatisticsType, SourceCourseIds, NegativeSourceCourseIds, SourceStudentPopulationType
FROM Course WHERE Id IN (135, 139, 142) ORDER BY Id;
