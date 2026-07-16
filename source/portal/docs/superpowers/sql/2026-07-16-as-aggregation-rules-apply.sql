-- AfterSchool/AS (StudentPopulationType.AfterSchool, Type=4) aggregation rule configuration.
-- See docs/superpowers/specs/2026-07-16-psj-as-aggregation-migration-design.md and
-- docs/superpowers/plans/2026-07-16-psj-as-aggregation-migration.md Task 6 for the full
-- mapping table and rationale.
--
-- IMPORTANT: if this script is ever reverted (see the paired revert script), Task 6's code
-- cutover (StudentPopulationController.cs, SumPHPopulation AS branch calling AggregationEngine)
-- MUST be reverted at the same time — see the PSJ apply script's header comment for why.
--
-- NOTE: GroupByClassType is deliberately 0 (false) for every rule below, including the two
-- "合計"/"總合計" pairs (257/258 and 307/308) — this is a faithful 1:1 preservation of the
-- existing code's behavior (which never split AS courses by ClassType, even for 英文班's real
-- EP/EG distinction), confirmed with the user during spec review. Do not change this to 1.

-- 安親課輔班：257「合計」與258「總合計」在舊碼裡完全同義（皆不分班別），照實搬
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[33]' WHERE Id = 257;
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[33]' WHERE Id = 258;

-- 安親課輔班：259-270 本週{年級}與上週相比 ×12，SourceCourseIds = [Id-14]（對應245-256）
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[245]' WHERE Id = 259;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[246]' WHERE Id = 260;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[247]' WHERE Id = 261;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[248]' WHERE Id = 262;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[249]' WHERE Id = 263;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[250]' WHERE Id = 264;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[251]' WHERE Id = 265;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[252]' WHERE Id = 266;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[253]' WHERE Id = 267;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[254]' WHERE Id = 268;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[255]' WHERE Id = 269;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[256]' WHERE Id = 270;

-- 安親課輔班：271-294 新生×12 + 流失×12，分校自填
UPDATE Course SET StatisticsType = 50 WHERE Id BETWEEN 271 AND 294;

-- 英文班：307「合計」與308「總合計」在舊碼裡完全同義（皆不分班別，EP+EG合併），照實搬
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[36]' WHERE Id = 307;
UPDATE Course SET StatisticsType = 3, SourceDepartmentIds = '[36]' WHERE Id = 308;

-- 英文班：309-320 本週{年級}與上週相比 ×12，SourceCourseIds = [Id-14]（對應295-306）
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[295]' WHERE Id = 309;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[296]' WHERE Id = 310;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[297]' WHERE Id = 311;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[298]' WHERE Id = 312;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[299]' WHERE Id = 313;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[300]' WHERE Id = 314;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[301]' WHERE Id = 315;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[302]' WHERE Id = 316;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[303]' WHERE Id = 317;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[304]' WHERE Id = 318;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[305]' WHERE Id = 319;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[306]' WHERE Id = 320;

-- 英文班：321-344 新生×12 + 流失×12，分校自填
UPDATE Course SET StatisticsType = 50 WHERE Id BETWEEN 321 AND 344;
