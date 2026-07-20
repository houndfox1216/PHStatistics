# 匯出Excel格式改版（PH／GEPT／PSJ 第一階段）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 `ReportExportService` 的 PH／GEPT／PSJ 匯出改成依 `School.Region`（南區／中北區）分 Sheet、並把 PSJ 從寫死的課程Id陣列（`_psjCourseIds`）改成跟 PH／GEPT 一致的「動態走訪 `Course`/`CourseDepartment`」方式。

**Architecture:** `LoadPopulations` 已經會 `Include(School.Region)` 並依 `Region.Ordinal, School.Ordinal` 排序（`ReportExportService.cs:428-446`），不需要改資料查詢層。這次的改動集中在「同一份查詢結果依 `Region.Name` 分組後，各自呼叫既有的 sheet-building 方法產生獨立 Sheet」，以及把 `BuildSheetPSJ` 從讀 `_psjCourseIds`/`_gradeOrder` 改成跟 `BuildSheetPH`/`BuildSheetGEPT` 同款的 `courses.GroupBy(Department)` 走訪。

**Tech Stack:** ASP.NET Core 8 / EF Core / NPOI 2.7.1 / NUnit（`[Explicit]` 測試直接接 dev DB，比照本專案既有的 Aggregation 遷移驗證模式）

## Global Constraints

- 依 2026-07-18 spec（`docs/superpowers/specs/2026-07-18-export-format-rework-design.md`）：這次只做 PH／GEPT／PSJ，AS／PS 不動。
- 分區只有「南區」「中北區」兩個值，資料庫已在本機 dev DB 套用（`docs/superpowers/sql/2026-07-18-school-region-assignment-apply.sql`），尚未套用正式環境——本計畫的程式碼變更不依賴「正式環境已套用分區」這件事，但**瀏覽器/真實資料驗證前必須先確認目標環境已套用該 SQL script**。
- 公式一律用系統算好的靜態數字寫入儲存格，**不寫 Excel 公式字串**（i.e. 不呼叫 `cell.SetCellFormula`），這是 spec 建議且本計畫預設採用的方向。
- PSJ「總計」（北區+南區合計）這次直接在記憶體裡把兩個分區的 `List<StudentPopulation>` 攤平運算後寫入靜態數字，不使用跨 Sheet 儲存格參照公式。
- **這次不把 PH 和 GEPT 合併成同一個活頁簿**（參考檔案是 3 個 Sheet 在同一個檔案，這裡先維持 `Export(PH,...)`／`Export(GEPT,...)` 各自呼叫各自回傳一個 `byte[]`），PH 的回傳活頁簿內含「南區」「中北區」兩個 Sheet，GEPT 維持單一 Sheet 不變。若之後要合併成單一活頁簿，是另一個獨立任務（改 `StudentPopulationController.ExportReport` 呼叫端）。
- 參考檔案裡「兒美系列／美語」（p1/p2/p3 子欄，PH南區 Sheet 的 col2-8）目前資料庫查無對應的 `CourseDepartment`，且參考檔案裡這幾欄在所有分校都是空白——確認為已停用的舊班系，**這次不處理，匯出時直接省略**。

---

## 檔案異動總覽

- **修改**：`source/portal/Portal/Services/ReportExportService.cs`
  - `Export(...)` 入口：PH／PSJ 分支改成依區域分組呼叫
  - `BuildSheetPH`：內部邏輯不變，但呼叫端改成「每個區域一個 Sheet」
  - `BuildSheetPSJ`：整個改寫，改用 `courses.GroupBy(Department)` 動態走訪，移除對 `_psjCourseIds`/`_gradeOrder`/`PsjColType` 的依賴（這三個仍保留給 AS 用，AS 這次不動）
  - 新增私有方法 `GroupByRegion(List<StudentPopulation>)`（PH／PSJ 共用）
- **新建**：`source/portal/Test/Services/ReportExportServiceStructureTests.cs`（結構驗證測試，`[Explicit]`，直接接 dev DB）

---

### Task 1: 新增「依區域分組」共用 helper

**Files:**
- Modify: `source/portal/Portal/Services/ReportExportService.cs`
- Test: `source/portal/Test/Services/ReportExportServiceStructureTests.cs`

**Interfaces:**
- Produces: `private static List<(string RegionName, List<StudentPopulation> Populations)> GroupByRegion(List<StudentPopulation> populations)` —— 依 `pop.School.Region.Name` 分組，組內維持原本排序；查無 `Region`（`RegionId == null`）的分校歸入一組叫 `"未分區"`，並排在最後。回傳的分組本身依區域第一筆資料的 `School.Region.Ordinal` 排序（`"未分區"` 永遠排最後）。

- [ ] **Step 1: 建立測試專案骨架（若 `Test/Services` 目錄不存在則新增）並寫失敗測試**

```csharp
// source/portal/Test/Services/ReportExportServiceStructureTests.cs
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PHStatistics.Content;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

[TestFixture]
public class ReportExportServiceStructureTests {

    private static List<(string RegionName, List<StudentPopulation> Populations)> InvokeGroupByRegion(
        List<StudentPopulation> populations) {
        var method = typeof(ReportExportService).GetMethod("GroupByRegion",
            BindingFlags.NonPublic | BindingFlags.Static);
        return (List<(string, List<StudentPopulation>)>)method.Invoke(null, new object[] { populations });
    }

    private static StudentPopulation MakePopulation(string schoolName, string regionName, int regionOrdinal) {
        var region = regionName == null ? null : new Region { Name = regionName, Ordinal = regionOrdinal };
        return new StudentPopulation {
            School = new School { Name = schoolName, Region = region, RegionId = region == null ? (int?)null : 1 },
            Items = new List<StudentPopulationItem>()
        };
    }

    [Test]
    public void GroupByRegion_GroupsBySchoolRegionName_PreservingRegionOrdinalOrder() {
        var populations = new List<StudentPopulation> {
            MakePopulation("東安", "南區", 1),
            MakePopulation("南京", "中北區", 0),
            MakePopulation("莊敬", "南區", 1),
        };

        var groups = InvokeGroupByRegion(populations);

        Assert.That(groups.Select(g => g.RegionName).ToList(), Is.EqualTo(new[] { "中北區", "南區" }));
        Assert.That(groups.First(g => g.RegionName == "南區").Populations.Select(p => p.School.Name),
            Is.EqualTo(new[] { "東安", "莊敬" }));
    }

    [Test]
    public void GroupByRegion_SchoolWithoutRegion_GoesIntoUnassignedGroupLast() {
        var populations = new List<StudentPopulation> {
            MakePopulation("南京", "中北區", 0),
            MakePopulation("查無分區學校", null, 0),
        };

        var groups = InvokeGroupByRegion(populations);

        Assert.That(groups.Last().RegionName, Is.EqualTo("未分區"));
        Assert.That(groups.Last().Populations.Single().School.Name, Is.EqualTo("查無分區學校"));
    }
}
```

- [ ] **Step 2: 執行測試確認失敗（`GroupByRegion` 方法還不存在）**

Run: `cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~ReportExportServiceStructureTests"`
Expected: 編譯失敗或找不到方法（`method` 為 null 導致 `Invoke` NullReferenceException），確認測試目前是失敗的。

- [ ] **Step 3: 在 `ReportExportService.cs` 新增 `GroupByRegion` 方法**

在 `ReportExportService.cs` 的「共用 helper」區塊（`WriteGradeRow` 方法後面）新增：

```csharp
    // 依 School.Region.Name 分組，維持組內原有排序（LoadPopulations 已依 Region.Ordinal, School.Ordinal 排序）
    // 查無 Region 的分校統一歸入「未分區」，永遠排在最後
    private static List<(string RegionName, List<StudentPopulation> Populations)> GroupByRegion(
        List<StudentPopulation> populations) {

        var withRegion = populations.Where(p => p.School?.Region != null).ToList();
        var withoutRegion = populations.Where(p => p.School?.Region == null).ToList();

        var groups = withRegion
            .GroupBy(p => p.School.Region)
            .OrderBy(g => g.Key.Ordinal)
            .Select(g => (RegionName: g.Key.Name, Populations: g.ToList()))
            .ToList();

        if (withoutRegion.Count > 0)
            groups.Add(("未分區", withoutRegion));

        return groups;
    }
```

- [ ] **Step 4: 執行測試確認通過**

Run: `cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~ReportExportServiceStructureTests"`
Expected: 2 個測試皆 PASS

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Services/ReportExportService.cs source/portal/Test/Services/ReportExportServiceStructureTests.cs
git commit -m "feat: add region grouping helper to ReportExportService"
```

---

### Task 2: PH 匯出改成依區域分 Sheet

**Files:**
- Modify: `source/portal/Portal/Services/ReportExportService.cs:63-92`（`Export` 方法的 PH 分支）
- Test: `source/portal/Test/Services/ReportExportServiceStructureTests.cs`

**Interfaces:**
- Consumes: Task 1 的 `GroupByRegion(List<StudentPopulation>)`；既有的 `BuildSheetPH(ISheet, List<StudentPopulation>, List<Course>, int, int, string)`（簽章不變）
- Produces: `Export(StudentPopulationType.PH, year, week, schoolIds)` 回傳的活頁簿含 N 個 Sheet（依實際資料涵蓋的區域數，通常是「南區」「中北區」兩個），每個 Sheet 只含該區域分校資料，Sheet 名稱等於區域名稱。

- [ ] **Step 1: 寫失敗測試——驗證 PH 匯出產生依區域命名的多個 Sheet**

在 `ReportExportServiceStructureTests.cs` 新增（這支測試需要 dev DB 實際資料，標記 `[Explicit]`；先找一個本機 dev DB 已知存在 PH 資料的 year/week，例如 115年第1週）：

```csharp
    [Explicit("需要本機 dev DB 連線，且假設 115年第1週已有 PH 資料與南區/中北區分區設定")]
    [Test]
    public void Export_PH_ProducesOneSheetPerRegion() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.PH, 115, 1);

        Assert.That(bytes, Is.Not.Empty);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook(ms);

        var sheetNames = Enumerable.Range(0, wb.NumberOfSheets)
            .Select(i => wb.GetSheetAt(i).SheetName)
            .ToList();

        Assert.That(sheetNames, Does.Contain("南區"));
        Assert.That(sheetNames, Does.Contain("中北區"));
        Assert.That(sheetNames, Has.No.Member("Sheet1"));
    }
```

- [ ] **Step 2: 執行測試確認失敗**

Run: `cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~Export_PH_ProducesOneSheetPerRegion" -- NUnit.Explicit=true`

（若專案的 Explicit 測試執行方式跟既有 `PhAggregationComparisonTests` 一致，直接照抄該測試專案既有的執行慣例）

Expected: FAIL，因為目前 `Export` 的 PH 分支還是建立單一 `"Sheet1"`。

- [ ] **Step 3: 修改 `Export` 方法的 PH 分支**

把 `ReportExportService.cs:63-97` 的 `Export` 方法改成：

```csharp
    public byte[] Export(StudentPopulationType type, int year, int week,
                          IList<int> schoolIds = null) {
        var populations = LoadPopulations(type, year, week, schoolIds);
        if (populations.Count == 0) return Array.Empty<byte>();

        var wb = new XSSFWorkbook();

        var courses = (type == StudentPopulationType.PSJ || type == StudentPopulationType.AfterSchool)
            ? null
            : LoadCourses(type);

        switch (type) {
            case StudentPopulationType.PH:
                foreach (var (regionName, regionPopulations) in GroupByRegion(populations)) {
                    var sheet = wb.CreateSheet(regionName);
                    BuildSheetPH(sheet, regionPopulations, courses, year, week,
                        $"{year}年第{week}週百瀚英語{regionName}分校人數統計表");
                }
                break;
            case StudentPopulationType.GEPT: {
                var sheet = wb.CreateSheet("英檢");
                BuildSheetGEPT(sheet, populations, courses, year, week);
                break;
            }
            case StudentPopulationType.PS: {
                var sheet = wb.CreateSheet("Sheet1");
                BuildSheetPS(sheet, populations, courses, year, week);
                break;
            }
            case StudentPopulationType.PSJ:
                foreach (var (regionName, regionPopulations) in GroupByRegion(populations)) {
                    var sheet = wb.CreateSheet(regionName);
                    BuildSheetPSJ(sheet, regionPopulations, year, week);
                }
                break;
            case StudentPopulationType.AfterSchool: {
                var sheet = wb.CreateSheet("Sheet1");
                BuildSheetAS(sheet, populations, year, week);
                break;
            }
        }

        using var ms = new MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }
```

（`BuildSheetPSJ` 這裡先維持目前生產程式碼既有的 4 參數簽章 `(sheet, populations, year, week)` 不變——只是從「呼叫一次涵蓋全部分校」改成「依區域分組後每組各呼叫一次」。PSJ 內部邏輯改寫、簽章加 `courses` 參數是 Task 3 的範圍，這裡先不動，確保 Task 2 自己可以獨立編譯、測試。）

- [ ] **Step 4: 執行測試確認通過**

Run: `cd source/portal && dotnet build PHStatistics.portal.sln`
Expected: 0 error

Run: `cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~Export_PH_ProducesOneSheetPerRegion"`（比照專案 Explicit 測試執行慣例手動指定執行）
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Services/ReportExportService.cs source/portal/Test/Services/ReportExportServiceStructureTests.cs
git commit -m "feat: split PH export by region"
```

---

### Task 3: PSJ 改用動態 Course 走訪，移除 `_psjCourseIds`

**Files:**
- Modify: `source/portal/Portal/Services/ReportExportService.cs`（`BuildSheetPSJ` 方法，`ReportExportService.cs:277-306`）
- Test: `source/portal/Test/Services/ReportExportServiceStructureTests.cs`

**Interfaces:**
- Consumes: `LoadCourses(StudentPopulationType.PSJ)` 既有方法；`Course.GroupByClassType`／`Course.DepartmentId`／`Course.Ordinal`（既有欄位，[[project-psj-ph-groupbyclasstype-fix]] 已確認 PSJ 的 `GroupByClassType` 設定正確反映「該課程要不要依班別（Personal/SubGroup）分兩欄」）
- Produces: `private static void BuildSheetPSJ(ISheet sheet, List<StudentPopulation> populations, List<Course> courses, int year, int week)` —— **簽章新增 `List<Course> courses` 參數**（Task 2 留下的呼叫端是舊的 4 參數簽章，本 Task Step 4 會一併更新該呼叫端）

**設計**：比照 `BuildSheetPH`/`BuildSheetGEPT` 的「`courses.GroupBy(Department)` → 每個 Department 一組欄位、組末加合計」寫法，取代目前讀 `_psjCourseIds`/`_gradeOrder`/`PsjColType` 的年級展開邏輯。**資料列的維度也要跟著改**：目前 PSJ 是「每分校每年級一列」（列=分校×年級），改成跟 PH/GEPT 一致的「每分校一列，每課程一欄」（因為 PSJ 的原始課程本來就是「一年級」「二年級」...「高三」這 12 筆 `Course` 記錄，年級本身已經是欄位維度而不需要另外用列來表示）。

- [ ] **Step 1: 寫失敗測試——驗證 PSJ 匯出欄位是依 `Course.Ordinal` 動態產生、不再依賴寫死陣列**

```csharp
    [Explicit("需要本機 dev DB 連線，且假設 115年第1週已有 PSJ 資料")]
    [Test]
    public void Export_PSJ_HeaderMatchesCourseOrdinalOrder() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.PSJ, 115, 1);

        Assert.That(bytes, Is.Not.Empty);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook(ms);
        var sheet = wb.GetSheetAt(0);

        // Row 1 應該出現「數學班」部門名稱（來源：CourseDepartment.Name，非寫死字串）
        var headerRow1Text = string.Join("", Enumerable.Range(0, sheet.GetRow(1).LastCellNum)
            .Select(c => sheet.GetRow(1).GetCell(c)?.StringCellValue ?? ""));
        Assert.That(headerRow1Text, Does.Contain("數學班"));

        // Row 2（課程名稱列）應該出現「一年級」（Course.Name，來自資料庫 Course.Id=145 等）
        var headerRow2Text = string.Join("|", Enumerable.Range(0, sheet.GetRow(2).LastCellNum)
            .Select(c => sheet.GetRow(2).GetCell(c)?.StringCellValue ?? ""));
        Assert.That(headerRow2Text, Does.Contain("一年級"));
    }
```

- [ ] **Step 2: 執行測試確認失敗**

Run: `cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~Export_PSJ_HeaderMatchesCourseOrdinalOrder"`
Expected: FAIL（目前 `BuildSheetPSJ` 產生的表頭是 `年/週/分校/年級 + T/MP/MS/...` 這種 code 欄位，不會出現「數學班」「一年級」這些文字）

- [ ] **Step 3: 改寫 `BuildSheetPSJ`**

把 `ReportExportService.cs:272-306` 的整段 `BuildSheetPSJ` 方法換成：

```csharp
    // ── PSJ ──────────────────────────────────────────────────────────────────
    // Row 0: 標題；Row 1: 班系名稱（含合計欄合併）；Row 2: 課程名稱（依 GroupByClassType 展開 EM1/小組班兩欄或單欄）
    // Row 3+: 每分校一列

    private static void BuildSheetPSJ(ISheet sheet,
        List<StudentPopulation> populations, List<Course> courses, int year, int week) {

        sheet.CreateRow(0).CreateCell(0).SetCellValue($"{year}年第{week}週百倍速人數表");

        var r1 = sheet.CreateRow(1);
        var r2 = sheet.CreateRow(2);
        r1.CreateCell(0).SetCellValue("分校");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 2, 0, 0)); } catch { }

        var deptGroups = courses
            .Where(c => !c.IsSum)
            .GroupBy(c => c.Department.Id)
            .Select(g => (dept: g.First().Department, list: g.ToList()))
            .ToList();

        // 每個非合計課程展開成 1 欄（GroupByClassType=false）或 2 欄 EM1/小組班（GroupByClassType=true）
        // 欄位規格：(course, classType) —— classType 為 null 表示不分班別
        var columns = new List<(Course course, ClassType? classType)>();
        int col = 1;
        var deptColStart = new Dictionary<int, int>();
        foreach (var (dept, list) in deptGroups) {
            deptColStart[dept.Id] = col;
            foreach (var c in list) {
                if (c.GroupByClassType) {
                    r2.CreateCell(col).SetCellValue($"{c.Name}(EM1)");
                    columns.Add((c, ClassType.Personal));
                    col++;
                    r2.CreateCell(col).SetCellValue($"{c.Name}(小組班)");
                    columns.Add((c, ClassType.SubGroup));
                    col++;
                } else {
                    r2.CreateCell(col).SetCellValue(c.Name);
                    columns.Add((c, null));
                    col++;
                }
                sheet.SetColumnWidth(col - 1, 4 * 256);
            }
            r2.CreateCell(col).SetCellValue("合計");
            sheet.SetColumnWidth(col, 4 * 256);
            columns.Add((null, null)); // 合計欄佔位，資料列時特別處理
            col++;
        }
        foreach (var (deptId, startCol) in deptColStart) {
            var dept = deptGroups.First(g => g.dept.Id == deptId).dept;
            r1.CreateCell(startCol).SetCellValue(dept.Name);
            if (col - 1 > startCol)
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, startCol, col - 1)); } catch { }
        }

        // 資料列：每分校一列
        int rowIdx = 3;
        foreach (var pop in populations) {
            var row = sheet.CreateRow(rowIdx++);
            row.CreateCell(0).SetCellValue(pop.School?.Name ?? "");

            int ci = 1;
            foreach (var (dept, list) in deptGroups) {
                int deptTotal = 0;
                foreach (var c in list) {
                    if (c.GroupByClassType) {
                        int em1 = pop.Items.Where(i => i.Class?.CourseId == c.Id && i.Class?.Type == ClassType.Personal).Sum(i => i.Number);
                        int sub = pop.Items.Where(i => i.Class?.CourseId == c.Id && i.Class?.Type == ClassType.SubGroup).Sum(i => i.Number);
                        if (em1 > 0) row.CreateCell(ci).SetCellValue(em1);
                        ci++;
                        if (sub > 0) row.CreateCell(ci).SetCellValue(sub);
                        ci++;
                        deptTotal += em1 + sub;
                    } else {
                        int sum = pop.Items.Where(i => i.Class?.CourseId == c.Id).Sum(i => i.Number);
                        if (sum > 0) row.CreateCell(ci).SetCellValue(sum);
                        ci++;
                        deptTotal += sum;
                    }
                }
                if (deptTotal > 0) row.CreateCell(ci).SetCellValue(deptTotal);
                ci++;
            }
        }
    }
```

- [ ] **Step 4: 更新 `Export` 方法裡 PSJ 分支的呼叫端，改用新簽章**

把 Task 2 Step 3 留下的：

```csharp
            case StudentPopulationType.PSJ:
                foreach (var (regionName, regionPopulations) in GroupByRegion(populations)) {
                    var sheet = wb.CreateSheet(regionName);
                    BuildSheetPSJ(sheet, regionPopulations, year, week);
                }
                break;
```

改成：

```csharp
            case StudentPopulationType.PSJ: {
                var psjCourses = LoadCourses(StudentPopulationType.PSJ);
                foreach (var (regionName, regionPopulations) in GroupByRegion(populations)) {
                    var sheet = wb.CreateSheet(regionName);
                    BuildSheetPSJ(sheet, regionPopulations, psjCourses, year, week);
                }
                break;
            }
```

（這裡先各自呼叫 `LoadCourses(StudentPopulationType.PSJ)`，而不是改動 `Export` 方法開頭「`courses` 只給 PH/GEPT/PS」的既有判斷式——那個判斷式的調整留給 Task 4 一起處理，因為 Task 4 也需要同一個 `courses`／`psjCourses` 給「總表」Sheet 使用，屆時一次改乾淨，避免這裡先改一半。）

- [ ] **Step 5: 執行測試確認通過**

Run: `cd source/portal && dotnet build PHStatistics.portal.sln`
Expected: 0 error

Run: `cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~ReportExportServiceStructureTests"`
Expected: 所有非 `[Explicit]` 測試 PASS（`[Explicit]` 測試需另外指定執行，比照專案既有 `PhAggregationComparisonTests` 的執行方式）

- [ ] **Step 6: Commit**

```bash
git add source/portal/Portal/Services/ReportExportService.cs source/portal/Test/Services/ReportExportServiceStructureTests.cs
git commit -m "feat: rewrite PSJ export to use dynamic course iteration and split by region"
```

---

### Task 4: PSJ 新增「總表」（全分校不分區）Sheet

**Files:**
- Modify: `source/portal/Portal/Services/ReportExportService.cs`（`Export` 方法 PSJ 分支）

**Interfaces:**
- Consumes: Task 3 的 `BuildSheetPSJ(ISheet, List<StudentPopulation>, List<Course>, int, int)`
- Produces: `Export(StudentPopulationType.PSJ, ...)` 回傳的活頁簿在「南區」「中北區」之外，額外多一個名為「總表」的 Sheet，內容是**不分區、全部分校**的資料（沿用 `populations` 整包，不用 `GroupByRegion` 拆開的任何一組）。

- [ ] **Step 1: 寫失敗測試**

```csharp
    [Explicit("需要本機 dev DB 連線")]
    [Test]
    public void Export_PSJ_IncludesTotalSheetWithAllSchools() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.PSJ, 115, 1);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook(ms);

        var sheetNames = Enumerable.Range(0, wb.NumberOfSheets)
            .Select(i => wb.GetSheetAt(i).SheetName).ToList();
        Assert.That(sheetNames, Does.Contain("總表"));

        var totalSheet = wb.GetSheet("總表");
        var southSheet = wb.GetSheet("南區");
        var northSheet = wb.GetSheet("中北區");

        int totalDataRows = totalSheet.LastRowNum - 2; // 扣除標題+表頭2列
        int regionDataRows = (southSheet.LastRowNum - 2) + (northSheet.LastRowNum - 2);
        Assert.That(totalDataRows, Is.EqualTo(regionDataRows));
    }
```

- [ ] **Step 2: 執行測試確認失敗**

Run: `cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~Export_PSJ_IncludesTotalSheetWithAllSchools"`
Expected: FAIL（目前沒有「總表」Sheet）

- [ ] **Step 3: 修改 `Export` 方法 PSJ 分支**

把 Task 3 Step 4 留下的 PSJ 分支：

```csharp
            case StudentPopulationType.PSJ: {
                var psjCourses = LoadCourses(StudentPopulationType.PSJ);
                foreach (var (regionName, regionPopulations) in GroupByRegion(populations)) {
                    var sheet = wb.CreateSheet(regionName);
                    BuildSheetPSJ(sheet, regionPopulations, psjCourses, year, week);
                }
                break;
            }
```

改成（新增「總表」Sheet，內容用完整 `populations`，在區域迴圈之前寫入）：

```csharp
            case StudentPopulationType.PSJ: {
                var psjCourses = LoadCourses(StudentPopulationType.PSJ);
                var totalSheet = wb.CreateSheet("總表");
                BuildSheetPSJ(totalSheet, populations, psjCourses, year, week);
                foreach (var (regionName, regionPopulations) in GroupByRegion(populations)) {
                    var sheet = wb.CreateSheet(regionName);
                    BuildSheetPSJ(sheet, regionPopulations, psjCourses, year, week);
                }
                break;
            }
```

- [ ] **Step 4: 執行測試確認通過**

Run: `cd source/portal && dotnet build PHStatistics.portal.sln` → 0 error
Run: `cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~ReportExportServiceStructureTests"` → PASS

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Services/ReportExportService.cs
git commit -m "feat: add unsplit total sheet to PSJ export"
```

---

### Task 5: PH Sheet 表頭調整——課程名稱移到 Row 2（貼近參考檔案的視覺結構）

**Files:**
- Modify: `source/portal/Portal/Services/ReportExportService.cs`（`BuildSheetPH` 方法，`ReportExportService.cs:104-168`）

**背景**：目前 `BuildSheetPH` 的 Row 2 完全空白（`sheet.CreateRow(2);` 沒有寫入任何內容），課程名稱直接寫在 Row 3。參考檔案的結構是 Row1=班系分組、Row2=課程名稱、Row3 只在極少數需要三層細分的欄位才使用（這次確認「兒美系列/美語」這個唯一需要 Row3 子分類的區塊已停用不處理，見 Global Constraints）。這個 Task 把課程名稱從 Row3 移到 Row2，Row3 保留但這次留空（未來若要支援子欄位細分可以用）。

**Interfaces:**
- Consumes: 無新依賴
- Produces: 無新對外介面（純內部欄位配置調整），但 Row2/Row3 的內容語意改變，任何依賴目前 Row3 = 課程名稱的呼叫端（目前查無其他呼叫端讀取匯出結果，僅供人閱讀/下載）需注意。

- [ ] **Step 1: 寫失敗測試**

```csharp
    [Explicit("需要本機 dev DB 連線")]
    [Test]
    public void Export_PH_CourseNamesAppearInRow2NotRow3() {
        var service = new ReportExportService();
        byte[] bytes = service.Export(StudentPopulationType.PH, 115, 1);

        using var ms = new System.IO.MemoryStream(bytes);
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook(ms);
        var sheet = wb.GetSheetAt(0);

        var row2Text = string.Join("|", Enumerable.Range(0, sheet.GetRow(2).LastCellNum)
            .Select(c => sheet.GetRow(2).GetCell(c)?.StringCellValue ?? ""));
        Assert.That(row2Text, Does.Contain("P1-初階"));
    }
```

- [ ] **Step 2: 執行測試確認失敗**

Run: `cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~Export_PH_CourseNamesAppearInRow2NotRow3"`
Expected: FAIL（目前課程名稱在 Row3，Row2 是空的）

- [ ] **Step 3: 修改 `BuildSheetPH`**

把 `ReportExportService.cs:110-139` 這段：

```csharp
        var r1 = sheet.CreateRow(1);
        sheet.CreateRow(2);
        var r3 = sheet.CreateRow(3);
        r1.CreateCell(0).SetCellValue("分校");
        r1.CreateCell(1).SetCellValue("類型");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 1, 1)); } catch { }

        var deptGroups = courses
            .Where(c => !c.IsSum)
            .GroupBy(c => c.Department.Id)
            .Select(g => (dept: g.First().Department, list: g.ToList()))
            .ToList();

        // 表頭：Row 1 = 班系名稱（含合計欄合併），Row 3 = 課程名稱 + "合計"
        int col = 2;
        foreach (var (dept, list) in deptGroups) {
            int deptStart = col;
            foreach (var c in list) {
                r3.CreateCell(col).SetCellValue(c.Name);
                sheet.SetColumnWidth(col, 4 * 256);
                col++;
            }
            r3.CreateCell(col).SetCellValue("合計");
            sheet.SetColumnWidth(col, 4 * 256);
            col++;
            r1.CreateCell(deptStart).SetCellValue(dept.Name);
            if (col - 1 > deptStart)
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, col - 1)); } catch { }
        }
```

改成：

```csharp
        var r1 = sheet.CreateRow(1);
        var r2 = sheet.CreateRow(2);
        sheet.CreateRow(3);
        r1.CreateCell(0).SetCellValue("分校");
        r1.CreateCell(1).SetCellValue("類型");
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 0, 0)); } catch { }
        try { sheet.AddMergedRegion(new CellRangeAddress(1, 3, 1, 1)); } catch { }

        var deptGroups = courses
            .Where(c => !c.IsSum)
            .GroupBy(c => c.Department.Id)
            .Select(g => (dept: g.First().Department, list: g.ToList()))
            .ToList();

        // 表頭：Row 1 = 班系名稱（含合計欄合併），Row 2 = 課程名稱 + "合計"，Row 3 保留給未來子欄位細分用
        int col = 2;
        foreach (var (dept, list) in deptGroups) {
            int deptStart = col;
            foreach (var c in list) {
                r2.CreateCell(col).SetCellValue(c.Name);
                sheet.SetColumnWidth(col, 4 * 256);
                col++;
            }
            r2.CreateCell(col).SetCellValue("合計");
            sheet.SetColumnWidth(col, 4 * 256);
            col++;
            r1.CreateCell(deptStart).SetCellValue(dept.Name);
            if (col - 1 > deptStart)
                try { sheet.AddMergedRegion(new CellRangeAddress(1, 1, deptStart, col - 1)); } catch { }
        }
```

同樣的調整也套用到 `BuildSheetGEPT`（`ReportExportService.cs:179-205`，把 `r3.CreateCell` 全部改成 `r2.CreateCell`，`sheet.CreateRow(2)` 改成建立並保留 `r2` 變數，原本 `r3` 宣告移除或保留給未來使用）。

- [ ] **Step 4: 執行測試確認通過**

Run: `cd source/portal && dotnet build PHStatistics.portal.sln` → 0 error
Run: `cd source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~ReportExportServiceStructureTests"` → PASS

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Services/ReportExportService.cs
git commit -m "refactor: move PH/GEPT export course names from row3 to row2 to match reference layout"
```

---

## Out of scope（本計畫不處理，沿用 spec 的既有決議）

- AS／PS 匯出格式改版
- 歷史週次垂直累積呈現
- PH/GEPT 合併成單一活頁簿（這次維持 `Export(PH,...)` 和 `Export(GEPT,...)` 各自回傳獨立檔案）
- 「兒美系列/美語」舊欄位（確認已停用、資料庫查無對應、參考檔案裡全部分校皆空白）
- **PSJ 參考檔案裡的「總計」Sheet**（北區+南區跨Sheet公式彙整成「每年級一列」的加總彙整表，跟本計畫 Task 4 做的「總表」是兩個不同東西——「總表」是全分校攤平列出，「總計」是進一步依年級彙整成單一列）——這次只做「總表」，「總計」這種彙整彙報表待使用者確認是否還需要後，另開一個小任務用「北區+南區資料在記憶體裡依年級加總後寫入靜態數字」的方式實作（不使用跨Sheet公式）
- 正式環境套用 `2026-07-18-school-region-assignment-apply.sql`（需另外跟使用者確認套用時機）
- 瀏覽器/真實下載檔案的人工比對驗證（本專案慣例延後到集中驗證那一輪，但由於這次的驗證方式高度仰賴「跟參考檔案視覺比對」，建議完成本計畫後至少開一次 Excel 實際比對一份匯出結果跟參考檔案的欄位順序是否吻合，不能只靠自動化測試斷言幾個關鍵字就結案）
