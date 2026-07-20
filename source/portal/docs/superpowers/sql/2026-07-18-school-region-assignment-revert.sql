-- 回復 2026-07-18-school-region-assignment-apply.sql

UPDATE School SET RegionId = NULL, UpdatedTime = GETDATE()
WHERE RegionId IN (SELECT Id FROM Region WHERE Name IN (N'南區', N'中北區'));

DELETE FROM Region WHERE Name IN (N'南區', N'中北區');
