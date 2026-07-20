-- Phase 2: create the shared "課輔分析總覽" CourseDepartment + 48 Course rows
-- (12 grades x 上週比/新生/流失/總人數), superseding the per-subject analysis courses
-- for AS going forward. See docs/superpowers/specs/2026-07-20-as-input-grid-phase2-design.md
-- ("詳細設計" §1-2) for the full rationale.
-- Verified live against the dev DB before writing this script: Course max Id = 477, CourseDepartment max Id = 45.
-- NOTE: this file is UTF-8 without BOM. `sqlcmd -i` on this machine silently no-ops on it
-- (0 rows affected, no error) unless you pass -f 65001. Always verify with an independent SELECT
-- afterward regardless.

SET IDENTITY_INSERT CourseDepartment ON;

INSERT INTO CourseDepartment (Id, Name, Ordinal, IsSum, Subject, Company, Type, Published, DataMode, CreatedTime, UpdatedTime)
VALUES
(46, N'課輔分析總覽', 45, 1, NULL, 0, 4, 0, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT CourseDepartment OFF;

SET IDENTITY_INSERT Course ON;

-- 478-489: 本週{年級}與上週相比 (DiffWithLastWeek=10, SourceCourseIds=[安親,英文,數學,理化] 該年級課程Id)
-- 490-501: 本週{年級}新生人數 (ManualInput=50)
-- 502-513: 本週{年級}流失人數 (ManualInput=50)
-- 514-525: 本週{年級}總人數 (SumBySourceCourses=4, SourceCourseIds 同上週比)
INSERT INTO Course (Id, Name, DepartmentId, IsSum, Ordinal, Type, ClassType, Published, StatisticsType, SourceDepartmentIds, SourceCourseIds, GroupByClassType, ApplicableClassType, SourceSubject, DataMode, CreatedTime, UpdatedTime)
VALUES
(478, N'本週一年級與上週相比', 46, 1, 477, 4, N'5', 1, 10, NULL, N'[245,295,378,428]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(479, N'本週二年級與上週相比', 46, 1, 478, 4, N'5', 1, 10, NULL, N'[246,296,379,429]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(480, N'本週三年級與上週相比', 46, 1, 479, 4, N'5', 1, 10, NULL, N'[247,297,380,430]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(481, N'本週四年級與上週相比', 46, 1, 480, 4, N'5', 1, 10, NULL, N'[248,298,381,431]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(482, N'本週五年級與上週相比', 46, 1, 481, 4, N'5', 1, 10, NULL, N'[249,299,382,432]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(483, N'本週六年級與上週相比', 46, 1, 482, 4, N'5', 1, 10, NULL, N'[250,300,383,433]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(484, N'本週國一與上週相比', 46, 1, 483, 4, N'5', 1, 10, NULL, N'[251,301,384,434]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(485, N'本週國二與上週相比', 46, 1, 484, 4, N'5', 1, 10, NULL, N'[252,302,385,435]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(486, N'本週國三與上週相比', 46, 1, 485, 4, N'5', 1, 10, NULL, N'[253,303,386,436]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(487, N'本週高一與上週相比', 46, 1, 486, 4, N'5', 1, 10, NULL, N'[254,304,387,437]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(488, N'本週高二與上週相比', 46, 1, 487, 4, N'5', 1, 10, NULL, N'[255,305,388,438]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(489, N'本週高三與上週相比', 46, 1, 488, 4, N'5', 1, 10, NULL, N'[256,306,389,439]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(490, N'本週一年級新生人數', 46, 1, 489, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(491, N'本週二年級新生人數', 46, 1, 490, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(492, N'本週三年級新生人數', 46, 1, 491, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(493, N'本週四年級新生人數', 46, 1, 492, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(494, N'本週五年級新生人數', 46, 1, 493, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(495, N'本週六年級新生人數', 46, 1, 494, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(496, N'本週國一新生人數', 46, 1, 495, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(497, N'本週國二新生人數', 46, 1, 496, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(498, N'本週國三新生人數', 46, 1, 497, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(499, N'本週高一新生人數', 46, 1, 498, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(500, N'本週高二新生人數', 46, 1, 499, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(501, N'本週高三新生人數', 46, 1, 500, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(502, N'本週一年級流失人數', 46, 1, 501, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(503, N'本週二年級流失人數', 46, 1, 502, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(504, N'本週三年級流失人數', 46, 1, 503, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(505, N'本週四年級流失人數', 46, 1, 504, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(506, N'本週五年級流失人數', 46, 1, 505, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(507, N'本週六年級流失人數', 46, 1, 506, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(508, N'本週國一流失人數', 46, 1, 507, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(509, N'本週國二流失人數', 46, 1, 508, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(510, N'本週國三流失人數', 46, 1, 509, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(511, N'本週高一流失人數', 46, 1, 510, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(512, N'本週高二流失人數', 46, 1, 511, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(513, N'本週高三流失人數', 46, 1, 512, 4, N'5', 1, 50, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(514, N'本週一年級總人數', 46, 1, 513, 4, N'5', 1, 4, NULL, N'[245,295,378,428]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(515, N'本週二年級總人數', 46, 1, 514, 4, N'5', 1, 4, NULL, N'[246,296,379,429]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(516, N'本週三年級總人數', 46, 1, 515, 4, N'5', 1, 4, NULL, N'[247,297,380,430]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(517, N'本週四年級總人數', 46, 1, 516, 4, N'5', 1, 4, NULL, N'[248,298,381,431]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(518, N'本週五年級總人數', 46, 1, 517, 4, N'5', 1, 4, NULL, N'[249,299,382,432]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(519, N'本週六年級總人數', 46, 1, 518, 4, N'5', 1, 4, NULL, N'[250,300,383,433]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(520, N'本週國一總人數', 46, 1, 519, 4, N'5', 1, 4, NULL, N'[251,301,384,434]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(521, N'本週國二總人數', 46, 1, 520, 4, N'5', 1, 4, NULL, N'[252,302,385,435]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(522, N'本週國三總人數', 46, 1, 521, 4, N'5', 1, 4, NULL, N'[253,303,386,436]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(523, N'本週高一總人數', 46, 1, 522, 4, N'5', 1, 4, NULL, N'[254,304,387,437]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(524, N'本週高二總人數', 46, 1, 523, 4, N'5', 1, 4, NULL, N'[255,305,388,438]', 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(525, N'本週高三總人數', 46, 1, 524, 4, N'5', 1, 4, NULL, N'[256,306,389,439]', 0, NULL, NULL, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT Course OFF;
