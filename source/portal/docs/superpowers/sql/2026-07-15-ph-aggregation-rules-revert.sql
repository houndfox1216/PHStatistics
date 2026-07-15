-- Revert PH aggregation rule configuration.
-- WARNING: only run this together with reverting the Task 3 code change
-- (SumPHPopulation's PH branch calling AggregationEngine) — see the apply
-- script's header comment for why running one without the other is unsafe.
UPDATE Course
SET StatisticsType = NULL, SourceDepartmentIds = NULL, GroupByClassType = 0
WHERE Id IN (9, 16, 21, 22, 27, 32, 33, 34, 35, 36, 37, 38,
             48, 49, 54, 59, 60, 61, 62, 63, 64, 65, 66, 67);
