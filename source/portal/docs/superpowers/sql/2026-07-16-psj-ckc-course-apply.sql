-- Create the CKC自立自學班 CourseDepartment and its 33 Course rows (English/Chinese/Math × 11 grades).
-- See docs/superpowers/specs/2026-07-16-psj-ckc-import-design.md ("決策摘要" + "詳細設計" §3) for the full rationale.
-- Verified live against the dev DB before writing this script: Course max Id = 344, CourseDepartment max Id = 38.
-- NOTE: Id is an IDENTITY column on both tables, so explicit Id inserts require IDENTITY_INSERT ON
-- (added here; not present in the original plan text, which omitted it).

SET IDENTITY_INSERT CourseDepartment ON;

INSERT INTO CourseDepartment (Id, Name, Ordinal, IsSum, Subject, Company, Type, Published, DataMode, CreatedTime, UpdatedTime)
VALUES (39, N'CKC自立自學班', 38, 0, NULL, NULL, 1, 1, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT CourseDepartment OFF;

SET IDENTITY_INSERT Course ON;

-- 英文 345-355, 國文 356-366, 數學 367-377 — 每科11筆，年級順序 二年級,三年級,四年級,五年級,六年級,國一,國二,國三,高一,高二,高三
-- ClassType 欄位比照既有數學班/理化班非加總課程慣例，填入 enum 值的顯示名稱說明文字（純供 Admin 後台人看）。
INSERT INTO Course (Id, Name, DepartmentId, IsSum, Ordinal, Type, ClassType, Published, StatisticsType, SourceDepartmentIds, SourceCourseIds, GroupByClassType, ApplicableClassType, SourceSubject, DataMode, CreatedTime, UpdatedTime)
VALUES
(345, N'CKC英文-二年級', 39, 0, 344, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(346, N'CKC英文-三年級', 39, 0, 345, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(347, N'CKC英文-四年級', 39, 0, 346, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(348, N'CKC英文-五年級', 39, 0, 347, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(349, N'CKC英文-六年級', 39, 0, 348, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(350, N'CKC英文-國一',   39, 0, 349, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(351, N'CKC英文-國二',   39, 0, 350, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(352, N'CKC英文-國三',   39, 0, 351, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(353, N'CKC英文-高一',   39, 0, 352, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(354, N'CKC英文-高二',   39, 0, 353, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(355, N'CKC英文-高三',   39, 0, 354, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(356, N'CKC國文-二年級', 39, 0, 355, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(357, N'CKC國文-三年級', 39, 0, 356, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(358, N'CKC國文-四年級', 39, 0, 357, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(359, N'CKC國文-五年級', 39, 0, 358, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(360, N'CKC國文-六年級', 39, 0, 359, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(361, N'CKC國文-國一',   39, 0, 360, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(362, N'CKC國文-國二',   39, 0, 361, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(363, N'CKC國文-國三',   39, 0, 362, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(364, N'CKC國文-高一',   39, 0, 363, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(365, N'CKC國文-高二',   39, 0, 364, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(366, N'CKC國文-高三',   39, 0, 365, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(367, N'CKC數學-二年級', 39, 0, 366, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(368, N'CKC數學-三年級', 39, 0, 367, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(369, N'CKC數學-四年級', 39, 0, 368, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(370, N'CKC數學-五年級', 39, 0, 369, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(371, N'CKC數學-六年級', 39, 0, 370, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(372, N'CKC數學-國一',   39, 0, 371, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(373, N'CKC數學-國二',   39, 0, 372, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(374, N'CKC數學-國三',   39, 0, 373, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(375, N'CKC數學-高一',   39, 0, 374, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(376, N'CKC數學-高二',   39, 0, 375, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE()),
(377, N'CKC數學-高三',   39, 0, 376, 1, N'EM1、團', 1, NULL, NULL, NULL, 0, NULL, NULL, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT Course OFF;
