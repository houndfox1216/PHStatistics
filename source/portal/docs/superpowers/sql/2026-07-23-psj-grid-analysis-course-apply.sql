-- Create the shared "百倍速分析總覽" CourseDepartment + 48 Course rows (12 grades x
-- 上週比/新生/流失/總人數), superseding the per-subject analysis courses (171-194 數學班分析,
-- 221-244 理化班分析) for PSJ going forward, for the new Excel-style grid input page only.
-- See docs/superpowers/specs/2026-07-23-psj-input-grid-design.md ("詳細設計" §1-2) for the
-- full rationale. Mirrors 2026-07-20-as-grid-analysis-course-apply.sql for AS.
-- Verified live against the dev DB before writing this script: Course max Id = 525, CourseDepartment max Id = 46.
-- NOTE: this file is UTF-8 without BOM. `sqlcmd -i` on this machine silently no-ops on it
-- (0 rows affected, no error) unless you pass -f 65001. Always verify with an independent SELECT
-- afterward regardless.

SET IDENTITY_INSERT CourseDepartment ON;

INSERT INTO CourseDepartment (Id, Name, Ordinal, IsSum, Subject, Company, Type, Published, DataMode, CreatedTime, UpdatedTime)
VALUES
(47, N'百倍速分析總覽', 46, 1, NULL, 0, 1, 0, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT CourseDepartment OFF;

SET IDENTITY_INSERT Course ON;

-- 526-537: 本週{年級}與上週相比 (DiffWithLastWeek=10, SourceCourseIds=[數學,理化] 該年級課程Id)
-- 538-549: 本週{年級}新生人數 (ManualInput=50)
-- 550-561: 本週{年級}流失人數 (ManualInput=50)
-- 562-573: 本週{年級}總人數 (SumBySourceCourses=4, SourceCourseIds 同上週比)
-- ClassType 欄位比照既有 AS 網格慣例，填入 enum General(=5) 的字串值 '5'。
INSERT INTO Course (Id, Name, DepartmentId, IsSum, Ordinal, Type, ClassType, Published, StatisticsType, SourceDepartmentIds, SourceCourseIds, GroupByClassType, ApplicableClassType, SourceSubject, DataMode, CreatedTime, UpdatedTime)
VALUES
(526, N'本週一年級與上週相比', 47, 1, 525, 1, N'5', 1, 10, NULL, N'[145,195]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(527, N'本週二年級與上週相比', 47, 1, 526, 1, N'5', 1, 10, NULL, N'[146,196]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(528, N'本週三年級與上週相比', 47, 1, 527, 1, N'5', 1, 10, NULL, N'[147,197]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(529, N'本週四年級與上週相比', 47, 1, 528, 1, N'5', 1, 10, NULL, N'[148,198]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(530, N'本週五年級與上週相比', 47, 1, 529, 1, N'5', 1, 10, NULL, N'[149,199]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(531, N'本週六年級與上週相比', 47, 1, 530, 1, N'5', 1, 10, NULL, N'[150,200]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(532, N'本週國一與上週相比', 47, 1, 531, 1, N'5', 1, 10, NULL, N'[151,201]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(533, N'本週國二與上週相比', 47, 1, 532, 1, N'5', 1, 10, NULL, N'[152,202]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(534, N'本週國三與上週相比', 47, 1, 533, 1, N'5', 1, 10, NULL, N'[153,203]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(535, N'本週高一與上週相比', 47, 1, 534, 1, N'5', 1, 10, NULL, N'[154,204]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(536, N'本週高二與上週相比', 47, 1, 535, 1, N'5', 1, 10, NULL, N'[155,205]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(537, N'本週高三與上週相比', 47, 1, 536, 1, N'5', 1, 10, NULL, N'[156,206]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(538, N'本週一年級新生人數', 47, 1, 537, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(539, N'本週二年級新生人數', 47, 1, 538, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(540, N'本週三年級新生人數', 47, 1, 539, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(541, N'本週四年級新生人數', 47, 1, 540, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(542, N'本週五年級新生人數', 47, 1, 541, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(543, N'本週六年級新生人數', 47, 1, 542, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(544, N'本週國一新生人數', 47, 1, 543, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(545, N'本週國二新生人數', 47, 1, 544, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(546, N'本週國三新生人數', 47, 1, 545, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(547, N'本週高一新生人數', 47, 1, 546, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(548, N'本週高二新生人數', 47, 1, 547, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(549, N'本週高三新生人數', 47, 1, 548, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(550, N'本週一年級流失人數', 47, 1, 549, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(551, N'本週二年級流失人數', 47, 1, 550, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(552, N'本週三年級流失人數', 47, 1, 551, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(553, N'本週四年級流失人數', 47, 1, 552, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(554, N'本週五年級流失人數', 47, 1, 553, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(555, N'本週六年級流失人數', 47, 1, 554, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(556, N'本週國一流失人數', 47, 1, 555, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(557, N'本週國二流失人數', 47, 1, 556, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(558, N'本週國三流失人數', 47, 1, 557, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(559, N'本週高一流失人數', 47, 1, 558, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(560, N'本週高二流失人數', 47, 1, 559, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(561, N'本週高三流失人數', 47, 1, 560, 1, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(562, N'本週一年級總人數', 47, 1, 561, 1, N'5', 1, 4, NULL, N'[145,195]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(563, N'本週二年級總人數', 47, 1, 562, 1, N'5', 1, 4, NULL, N'[146,196]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(564, N'本週三年級總人數', 47, 1, 563, 1, N'5', 1, 4, NULL, N'[147,197]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(565, N'本週四年級總人數', 47, 1, 564, 1, N'5', 1, 4, NULL, N'[148,198]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(566, N'本週五年級總人數', 47, 1, 565, 1, N'5', 1, 4, NULL, N'[149,199]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(567, N'本週六年級總人數', 47, 1, 566, 1, N'5', 1, 4, NULL, N'[150,200]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(568, N'本週國一總人數', 47, 1, 567, 1, N'5', 1, 4, NULL, N'[151,201]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(569, N'本週國二總人數', 47, 1, 568, 1, N'5', 1, 4, NULL, N'[152,202]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(570, N'本週國三總人數', 47, 1, 569, 1, N'5', 1, 4, NULL, N'[153,203]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(571, N'本週高一總人數', 47, 1, 570, 1, N'5', 1, 4, NULL, N'[154,204]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(572, N'本週高二總人數', 47, 1, 571, 1, N'5', 1, 4, NULL, N'[155,205]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(573, N'本週高三總人數', 47, 1, 572, 1, N'5', 1, 4, NULL, N'[156,206]', 0, NULL, NULL, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT Course OFF;
