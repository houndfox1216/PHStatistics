-- PSJ (StudentPopulationType.PSJ, Type=1) aggregation rule configuration.
-- See docs/superpowers/specs/2026-07-16-psj-as-aggregation-migration-design.md and
-- docs/superpowers/plans/2026-07-16-psj-as-aggregation-migration.md Task 3 for the full
-- mapping table and rationale.
--
-- IMPORTANT: if this script is ever reverted (see the paired revert script), Task 3's code
-- cutover (StudentPopulationController.cs, SumPHPopulation PSJ branch calling AggregationEngine)
-- MUST be reverted at the same time. Reverting only the SQL leaves StatisticsType NULL while the
-- live code still calls the engine, which will leave every PSJ summary course frozen at whatever
-- value it last held (the engine no-ops on StatisticsType=null) — worse than the original bug.

-- 數學班：157 依班別分開算，158 不分班別（兩者都加總「數學班」部門(27)內的原始課程）
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[27]', GroupByClassType = 1 WHERE Id = 157;
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[27]' WHERE Id = 158;

-- 數學班：159-170 本週{年級}與上週相比 ×12，SourceCourseIds = [Id-14]（對應145-156），依班別分開算
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[145]', GroupByClassType = 1 WHERE Id = 159;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[146]', GroupByClassType = 1 WHERE Id = 160;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[147]', GroupByClassType = 1 WHERE Id = 161;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[148]', GroupByClassType = 1 WHERE Id = 162;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[149]', GroupByClassType = 1 WHERE Id = 163;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[150]', GroupByClassType = 1 WHERE Id = 164;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[151]', GroupByClassType = 1 WHERE Id = 165;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[152]', GroupByClassType = 1 WHERE Id = 166;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[153]', GroupByClassType = 1 WHERE Id = 167;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[154]', GroupByClassType = 1 WHERE Id = 168;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[155]', GroupByClassType = 1 WHERE Id = 169;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[156]', GroupByClassType = 1 WHERE Id = 170;

-- 數學班：171-194 新生×12 + 流失×12，分校自填
UPDATE Course SET StatisticsType = 50 WHERE Id BETWEEN 171 AND 194;

-- 理化班：207 依班別分開算，208 不分班別（兩者都加總「理化班」部門(30)內的原始課程）
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[30]', GroupByClassType = 1 WHERE Id = 207;
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[30]' WHERE Id = 208;

-- 理化班：209-220 本週{年級}與上週相比 ×12，SourceCourseIds = [Id-14]（對應195-206），依班別分開算
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[195]', GroupByClassType = 1 WHERE Id = 209;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[196]', GroupByClassType = 1 WHERE Id = 210;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[197]', GroupByClassType = 1 WHERE Id = 211;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[198]', GroupByClassType = 1 WHERE Id = 212;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[199]', GroupByClassType = 1 WHERE Id = 213;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[200]', GroupByClassType = 1 WHERE Id = 214;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[201]', GroupByClassType = 1 WHERE Id = 215;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[202]', GroupByClassType = 1 WHERE Id = 216;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[203]', GroupByClassType = 1 WHERE Id = 217;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[204]', GroupByClassType = 1 WHERE Id = 218;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[205]', GroupByClassType = 1 WHERE Id = 219;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[206]', GroupByClassType = 1 WHERE Id = 220;

-- 理化班：221-244 新生×12 + 流失×12，分校自填
UPDATE Course SET StatisticsType = 50 WHERE Id BETWEEN 221 AND 244;
