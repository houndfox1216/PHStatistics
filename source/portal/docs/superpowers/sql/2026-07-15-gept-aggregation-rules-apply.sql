-- GEPT (StudentPopulationType.GEPT) aggregation rule configuration.
-- See docs/superpowers/plans/2026-07-15-aggregation-engine-and-gept-migration.md, Task 5, for the full mapping table and rationale.

UPDATE Course SET StatisticsType = 1 WHERE Id IN (71, 74, 78, 82, 87, 107); -- SumByDepartment

UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[14,15,16,17,18]' WHERE Id = 88; -- 本週英檢總人數 (excludes dept 21)

UPDATE Course SET StatisticsType = 40, SourceDepartmentIds = '[14,15,16,17,18,21]' WHERE Id = 89;  -- 上週英檢總人數
UPDATE Course SET StatisticsType = 10, SourceDepartmentIds = '[14,15,16,17,18,21]' WHERE Id = 90;  -- 與上週相比
UPDATE Course SET StatisticsType = 41, SourceDepartmentIds = '[14,15,16,17,18,21]' WHERE Id = 91;  -- 去年同期人數
UPDATE Course SET StatisticsType = 11, SourceDepartmentIds = '[14,15,16,17,18,21]' WHERE Id = 92;  -- 去年同期/比

UPDATE Course SET StatisticsType = 50 WHERE Id IN (93, 94); -- ManualInput (新生/流失)