-- PH (StudentPopulationType.PH, Type=0) aggregation rule configuration.
-- See docs/superpowers/plans/2026-07-15-ph-aggregation-migration.md, Task 1, and
-- docs/superpowers/specs/2026-07-15-ph-aggregation-migration-design.md for the full mapping table and rationale.
--
-- IMPORTANT: if this script is ever reverted (see the paired revert script), Task 3's code
-- cutover (StudentPopulationController.cs, SumPHPopulation PH branch calling AggregationEngine)
-- MUST be reverted at the same time. Reverting only the SQL leaves StatisticsType NULL while the
-- live code still calls the engine, which will leave every PH summary course frozen at whatever
-- value it last held (the engine no-ops on StatisticsType=null) — worse than the original bug.

-- 同班系+同班別加總 (own department, split by 小/三)
UPDATE Course SET StatisticsType = 2, GroupByClassType = 1 WHERE Id IN (9, 16, 21, 48);

-- 同班系加總，不分班別 (own department, EM1/合作開班 combine 小+三 into one number)
UPDATE Course SET StatisticsType = 1, GroupByClassType = 0 WHERE Id IN (27, 32, 54, 59);

-- 班數統計 (Number > 0 count), split by 小/三
UPDATE Course SET StatisticsType = 31, SourceDepartmentIds = '[1,2,3]', GroupByClassType = 1 WHERE Id = 22;  -- 英文總班數統計
UPDATE Course SET StatisticsType = 31, SourceDepartmentIds = '[8]', GroupByClassType = 1 WHERE Id = 49;      -- 國文總班數

-- 本週英語文/國語文總人數 (sum across all raw English/Chinese departments, not split by 小/三)
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[1,2,3,5,6]', GroupByClassType = 0 WHERE Id = 33;   -- 本週英語文總人數
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[8,10,11]', GroupByClassType = 0 WHERE Id = 60;     -- 本週國語文總人數

-- 上週英語文/國語文總人數 (LastWeekValue — fixes the previously-dead 0-only column per the design spec)
UPDATE Course SET StatisticsType = 40, SourceDepartmentIds = '[1,2,3,5,6]', GroupByClassType = 0 WHERE Id = 34;  -- 上週英語文總人數
UPDATE Course SET StatisticsType = 40, SourceDepartmentIds = '[8,10,11]', GroupByClassType = 0 WHERE Id = 61;    -- 上週國語文總人數

-- 與上週相比 (DiffWithLastWeek)
UPDATE Course SET StatisticsType = 10, SourceDepartmentIds = '[1,2,3,5,6]', GroupByClassType = 0 WHERE Id = 35;  -- 英文 與上週相比
UPDATE Course SET StatisticsType = 10, SourceDepartmentIds = '[8,10,11]', GroupByClassType = 0 WHERE Id = 62;    -- 國文 與上週相比

-- 去年同期/比 (DiffWithLastYear)
UPDATE Course SET StatisticsType = 11, SourceDepartmentIds = '[1,2,3,5,6]', GroupByClassType = 0 WHERE Id = 36;  -- 英文 去年同期/比
UPDATE Course SET StatisticsType = 11, SourceDepartmentIds = '[8,10,11]', GroupByClassType = 0 WHERE Id = 63;    -- 國文 去年同期/比

-- 手動輸入 (新生/流失 + 本週總詢問(填單)人數 — never auto-computed)
UPDATE Course SET StatisticsType = 50 WHERE Id IN (37, 38, 64, 65, 66);

-- 總人數 (grand total across every raw PH department)
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[1,2,3,5,6,8,10,11]', GroupByClassType = 0 WHERE Id = 67;
