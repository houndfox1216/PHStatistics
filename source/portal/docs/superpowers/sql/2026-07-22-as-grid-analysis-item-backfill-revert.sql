-- Revert: remove only the StudentPopulationItem rows (and the Class rows created to hold them)
-- added by 2026-07-22-as-grid-analysis-item-backfill-apply.sql.
-- Scoped strictly to the 42 populations targeted by the apply script — Id=2383 (which already
-- had all 48 items before the apply script ran) is intentionally excluded so this cannot delete
-- pre-existing data.

SET NOCOUNT ON;
BEGIN TRANSACTION;

DELETE spi
FROM StudentPopulationItem spi
JOIN Class cl ON cl.Id = spi.ClassId
WHERE cl.CourseId BETWEEN 478 AND 525
  AND spi.StudentPopulationId IN (88,89,87,112,5,111,115,114,113,214,215,213,250,275,247,274,308,
                                   309,307,368,369,367,364,427,428,426,486,1427,1425,1437,1442,1443,
                                   2231,2232,2241,2335,2336,2337,2375,2325,2321,2378);

-- Only the 6 schools that had zero Class rows for 478-525 before the apply script ran
-- (福山/22 already had all 48 beforehand and is excluded here).
DELETE FROM Class
WHERE CourseId BETWEEN 478 AND 525
  AND SchoolId IN (23, 29, 11, 3, 4, 25);

COMMIT;
