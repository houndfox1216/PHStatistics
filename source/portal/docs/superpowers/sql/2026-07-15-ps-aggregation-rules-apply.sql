-- PS (StudentPopulationType.PS, Type=3) aggregation rule configuration.
-- See docs/superpowers/plans/2026-07-15-ps-aggregation-migration.md, Task 2, and
-- docs/superpowers/specs/2026-07-15-ps-aggregation-migration-design.md for the full mapping table and rationale.
--
-- IMPORTANT: if this script is ever reverted (see the paired revert script), Task 4's code
-- cutover (StudentPopulationController.cs, SumPHPopulation PS branch calling AggregationEngine)
-- MUST be reverted at the same time. Reverting only the SQL leaves StatisticsType NULL while the
-- live code still calls the engine, which will leave every PS summary course frozen at whatever
-- value it last held (the engine no-ops on StatisticsType=null) — worse than the original bug.

-- 同班系加總 (own department, no ClassType split)
UPDATE Course SET StatisticsType = 1 WHERE Id IN (120, 127, 131);

-- PS數學總人數：加總 22/23/24 三個原始年級班系
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[22,23,24]' WHERE Id = 132;

-- PS數學開班數：班數統計 (Number > 0 count)，不分班別
UPDATE Course SET StatisticsType = 30, SourceDepartmentIds = '[22,23,24]' WHERE Id = 133;

-- PS數學班平均人數：平均值 = 總人數 / 開班數
UPDATE Course SET StatisticsType = 60, SourceDepartmentIds = '[22,23,24]' WHERE Id = 134;

-- PS上週人數：LastWeekValue，跟132同源 (修正原本永遠只能靠人工填寫維持正確的欄位)
UPDATE Course SET StatisticsType = 40, SourceDepartmentIds = '[22,23,24]' WHERE Id = 138;

-- 本週變更(PS+百倍數) / 總人數(PS+百倍數)：來源直接指向另外兩個加總課程 132(可計算) + 135(人工填寫)
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[132,135]' WHERE Id = 142;
UPDATE Course SET StatisticsType = 4, SourceCourseIds = '[132,135]' WHERE Id = 144;

-- 手動輸入 (百倍速系列 + 新生/流失 + 本週總詢問人數 — 分校自填，系統不計算)
UPDATE Course SET StatisticsType = 50 WHERE Id IN (135, 136, 137, 139, 140, 141, 143);
