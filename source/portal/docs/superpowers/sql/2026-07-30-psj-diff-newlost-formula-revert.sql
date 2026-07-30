-- 還原 2026-07-30-psj-diff-newlost-formula-apply.sql：
-- 將課程526-537還原成原本的 DiffWithLastWeek(10) / [數學總人數課程,理化總人數課程] 設定。

UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[145,195]', NegativeSourceCourseIds = NULL WHERE Id = 526;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[146,196]', NegativeSourceCourseIds = NULL WHERE Id = 527;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[147,197]', NegativeSourceCourseIds = NULL WHERE Id = 528;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[148,198]', NegativeSourceCourseIds = NULL WHERE Id = 529;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[149,199]', NegativeSourceCourseIds = NULL WHERE Id = 530;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[150,200]', NegativeSourceCourseIds = NULL WHERE Id = 531;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[151,201]', NegativeSourceCourseIds = NULL WHERE Id = 532;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[152,202]', NegativeSourceCourseIds = NULL WHERE Id = 533;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[153,203]', NegativeSourceCourseIds = NULL WHERE Id = 534;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[154,204]', NegativeSourceCourseIds = NULL WHERE Id = 535;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[155,205]', NegativeSourceCourseIds = NULL WHERE Id = 536;
UPDATE Course SET StatisticsType = 10, SourceCourseIds = '[156,206]', NegativeSourceCourseIds = NULL WHERE Id = 537;
