-- 將 PSJ（StudentPopulationType.PSJ, Type=1）網格版「上週比」課程（526-537，
-- CourseDepartment=47「百倍速分析總覽」）從 DiffWithLastWeek(10)：Σ(本週總人數)-Σ(上週總人數)
-- 改成 DiffBetweenCourses(12)：Σ(新生人數課程) − Σ(流失人數課程)。
--
-- 不動：舊清單頁課程 159-170/209-220（依 docs/superpowers/specs/2026-07-23-psj-input-grid-design.md
-- 已停用不再寫入）、PH 的英文/國文「與上週相比」hardcode邏輯（StudentPopulationController.cs 約2317-2360行）、
-- GEPT 課程90（DiffWithLastWeek=10，維持不變）。
--
-- 前置條件：程式碼（StatisticsType.DiffBetweenCourses、AggregationEngine新分支、
-- Course.NegativeSourceCourseIds欄位與migration）必須已經部署完成，
-- 否則任何觸碰這12個課程項目的存檔動作，AggregationEngine.Compute() 會對未知
-- StatisticsType 直接 throw NotSupportedException。
--
-- 年級對應（grade i, i=1..12）：上週比課程=525+i，新生課程=537+i，流失課程=549+i。

UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[538]', NegativeSourceCourseIds = '[550]' WHERE Id = 526; -- 一年級
UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[539]', NegativeSourceCourseIds = '[551]' WHERE Id = 527; -- 二年級
UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[540]', NegativeSourceCourseIds = '[552]' WHERE Id = 528; -- 三年級
UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[541]', NegativeSourceCourseIds = '[553]' WHERE Id = 529; -- 四年級
UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[542]', NegativeSourceCourseIds = '[554]' WHERE Id = 530; -- 五年級
UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[543]', NegativeSourceCourseIds = '[555]' WHERE Id = 531; -- 六年級
UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[544]', NegativeSourceCourseIds = '[556]' WHERE Id = 532; -- 國一
UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[545]', NegativeSourceCourseIds = '[557]' WHERE Id = 533; -- 國二
UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[546]', NegativeSourceCourseIds = '[558]' WHERE Id = 534; -- 國三
UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[547]', NegativeSourceCourseIds = '[559]' WHERE Id = 535; -- 高一
UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[548]', NegativeSourceCourseIds = '[560]' WHERE Id = 536; -- 高二
UPDATE Course SET StatisticsType = 12, SourceCourseIds = '[549]', NegativeSourceCourseIds = '[561]' WHERE Id = 537; -- 高三
