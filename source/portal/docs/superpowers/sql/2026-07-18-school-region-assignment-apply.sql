-- 建立南區/中北區兩個Region，並依PH/PSJ參考匯出檔案實際分校名單回填 School.RegionId
-- 無法從參考檔案判定分區的分校，依使用者指示一律歸入南區
-- 來源：C:\Leo\其他\Kuri\人數表匯入A\50\2025 07(全國人數表第50週).xlsx（PH，南區/中北區兩個Sheet）
--       C:\Leo\其他\Kuri\人數表匯入A\50\（百倍速）人數統計表更新版115.6.13).xlsx（PSJ，北區/南區兩個Sheet，交叉驗證一致）

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = N'南區')
    INSERT INTO Region (Name, Ordinal, Remark, CreatedTime, UpdatedTime, DataMode)
    VALUES (N'南區', 1, N'2026-07-18 依匯出格式改版新增', GETDATE(), GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = N'中北區')
    INSERT INTO Region (Name, Ordinal, Remark, CreatedTime, UpdatedTime, DataMode)
    VALUES (N'中北區', 2, N'2026-07-18 依匯出格式改版新增', GETDATE(), GETDATE(), 0);

DECLARE @SouthId INT = (SELECT Id FROM Region WHERE Name = N'南區');
DECLARE @NorthId INT = (SELECT Id FROM Region WHERE Name = N'中北區');

-- 中北區（10校，來源：PH「中北區」Sheet）
UPDATE School SET RegionId = @NorthId, UpdatedTime = GETDATE()
WHERE Name IN (N'向上', N'大安', N'南京', N'板橋忠孝', N'東湖', N'木柵', N'板橋陽明', N'敦南', N'內湖', N'古亭');

-- 南區（16校，來源：PH「南區」Sheet + 其餘查無分區資料的分校，依使用者指示一律歸入南區）
UPDATE School SET RegionId = @SouthId, UpdatedTime = GETDATE()
WHERE Name IN (
    N'復興', N'陽明', N'莊敬', N'瑞祥', N'華夏', N'龍華', N'中正', N'正興', N'河堤', N'右昌',
    N'東安', N'福山', N'農十六', N'高美館', N'岡山', N'仁武',
    N'新東湖', N'崇明', N'秀朗', N'分校', N'鳳山', N'鳳甲', N'天母忠誠', N'許博智'
);

SELECT s.Id, s.Name, s.RegionId, r.Name AS RegionName FROM School s LEFT JOIN Region r ON r.Id = s.RegionId ORDER BY r.Ordinal, s.Ordinal;
