-- Revert PSJ aggregation rule configuration.
-- WARNING: only run this together with reverting the Task 3 code change
-- (SumPHPopulation's PSJ branch calling AggregationEngine) — see the apply
-- script's header comment for why running one without the other is unsafe.
UPDATE Course
SET StatisticsType = NULL, SourceDepartmentIds = NULL, SourceCourseIds = NULL, GroupByClassType = 0
WHERE Id BETWEEN 157 AND 244 AND Id NOT BETWEEN 195 AND 206;
