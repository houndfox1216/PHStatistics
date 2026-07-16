-- Revert AS aggregation rule configuration.
-- WARNING: only run this together with reverting the Task 6 code change
-- (SumPHPopulation's AS branch calling AggregationEngine) — see the apply
-- script's header comment for why running one without the other is unsafe.
UPDATE Course
SET StatisticsType = NULL, SourceDepartmentIds = NULL, SourceCourseIds = NULL, GroupByClassType = 0
WHERE Id BETWEEN 257 AND 344 AND Id NOT BETWEEN 295 AND 306;
