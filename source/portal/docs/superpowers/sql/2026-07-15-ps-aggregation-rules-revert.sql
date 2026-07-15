-- Revert PS aggregation rule configuration.
-- WARNING: only run this together with reverting the Task 4 code change
-- (SumPHPopulation's PS branch calling AggregationEngine) — see the apply
-- script's header comment for why running one without the other is unsafe.
UPDATE Course
SET StatisticsType = NULL, SourceDepartmentIds = NULL, SourceCourseIds = NULL
WHERE Id IN (120, 127, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144);
