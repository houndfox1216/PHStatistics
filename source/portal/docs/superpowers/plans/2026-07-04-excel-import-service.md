# 後台 Excel 匯入服務 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓後台使用者可自行上傳 Excel 檔匯入人數表資料（不再需要技術人員手動跑 `ImportAll`），匯入完成後自動校正本次匯入資料的 `LastWeekNumber`，並把現有 700 行、三處各自維護課程對照表的匯入邏輯，重構成可長期擴充的插件式服務。

**Architecture:** 新建 `Portal/Services/Import/` 命名空間 `PHStatistics.Portal.Services.Import`：共用支援類別（課程對照表、寫入輔助、標題解析、PH/GEPT 共用 Sheet 解析器）放在 `ImportSupport/` 子命名空間；每種報表格式（PH/GEPT/PS/PSJ/AS）各一個實作 `IPopulationImporter` 介面的類別，提供 `Scan`（輕量掃描，不寫入 DB）與 `Import`（正式寫入）兩個方法；`PopulationImportService` 是唯一對外入口，依 `StudentPopulationType` 選擇對應 Importer，並在 `Import` 完成後執行「校正上週資料」後置流程。`HomeController.ImportAll`（技術人員舊路徑）與新建的 `Areas/Admin/Controllers/StudentPopulationImportController.cs`（後台使用者新路徑）都改為呼叫這個共用服務。

**Tech Stack:** ASP.NET Core 8.0 MVC、Entity Framework Core 8、NPOI（Excel 讀取，`XSSFWorkbook`/`ISheet`/`IRow`）、DevExtreme（既有後台 UI 元件，本次不使用）、jQuery（前端 AJAX）。

## Global Constraints

- 對應 spec：`source/portal/docs/superpowers/specs/2026-07-04-week-switch-and-excel-import-design.md` 的「需求二」章節（含 2026-07-04 三次修正：校正演算法改用 `(CourseId, ClassType)` 分組、匯入 action 移到獨立 Controller）
- 新權限 `SystemPermission.StudentPopulationImport` 只能加在 `SystemPermission.cs` enum 尾端，不可插在中間
- **匯入 Controller 必須是獨立的 `StudentPopulationImportController`**，不可加在既有 `Areas/Admin/Controllers/StudentPopulationController.cs`（該類別已有類別層級 `[RequirePermission(SystemPermission.StudentPopulation)]`，`AdminBaseController.OnActionExecuting` 用 `.FirstOrDefault()` 只取一個權限屬性檢查，疊加方法層級的不同權限有被蓋過風險）
- 校正上週資料**不可**用 `ClassId` 比對（匯入資料每次都建立新 `Class` 記錄，`ClassId` 跨週不會相同），必須用 `(Class.CourseId, Class.Type)` 分組加總比對，同組第一筆填入上週總數、其餘填 0（比照 `HomeController.cs:3881` 既有 `FillLastWeekNumbers` 邏輯）
- 校正範圍僅限本次匯入產生/更新的 `StudentPopulation`（不做全庫掃描、不做向下一週連鎖修正）
- 匯入資料不建立任何 `IsSum` 項目，不需要額外呼叫合計重算
- 重複匯入（分校/週次已有資料）需要使用者二次確認才覆寫重建；確認後沿用 `deleteExisting=true` 冪等重建
- 此專案沒有 Controller/Service 單元測試基礎設施；驗證方式一律為編譯成功 + 啟動本機服務手動操作驗證，不生產虛假的單元測試
- 移動既有程式碼時，方法本體邏輯必須逐行保持不變（只允許：命名空間/類別包裝、私有欄位改為公開存取、`ImportAllResult`→`ImportResult` 型別改名、`Logger` 從隱式的 controller 屬性改為明確傳入參數），任何本體邏輯改動都必須在該任務的 Step 說明中明確標註

---

### Task 1: 新增 `StudentPopulationImport` 權限

**Files:**
- Modify: `source/schema/Data/SystemPermission.cs:151-155`（緊接在前次 plan 新增的 `PopulationWeekSwitch` 之後）

**Interfaces:**
- Produces: `SystemPermission.StudentPopulationImport`，供 Task 10 的 `[RequirePermission(...)]` 使用

- [ ] **Step 1: 在 enum 尾端新增列舉值**

目前檔案尾端：
```csharp
    /// <summary>
    /// 人數表週次切換（可任選週次輸入/編輯，不受當週自動判定限制）
    /// </summary>
    [Display(Name = "人數表週次切換")]
    PopulationWeekSwitch,
}
```

改為：
```csharp
    /// <summary>
    /// 人數表週次切換（可任選週次輸入/編輯，不受當週自動判定限制）
    /// </summary>
    [Display(Name = "人數表週次切換")]
    PopulationWeekSwitch,

    /// <summary>
    /// 人數表 Excel 匯入（後台自助匯入，與人數表管理權限分開）
    /// </summary>
    [Display(Name = "人數表匯入")]
    StudentPopulationImport,
}
```

- [ ] **Step 2: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded，無錯誤

- [ ] **Step 3: Commit**

```bash
git add source/schema/Data/SystemPermission.cs
git commit -m "feat: add StudentPopulationImport permission"
```

---

### Task 2: 抽出匯入共用支援類別（ImportResult、CourseMapping、PopulationWriteHelper、TitleParser）

**Files:**
- Create: `source/portal/Portal/Services/Import/ImportResult.cs`
- Create: `source/portal/Portal/Services/Import/ImportSupport/CourseMapping.cs`
- Create: `source/portal/Portal/Services/Import/ImportSupport/PopulationWriteHelper.cs`
- Create: `source/portal/Portal/Services/Import/ImportSupport/TitleParser.cs`

**Interfaces:**
- Produces:
  - `PHStatistics.Portal.Services.Import.ImportResult`：`Week`/`File`/`Type` (string)、`SchoolCount`/`ItemCount` (int)、`Errors` (`List<string>`)、`PopulationIds` (`HashSet<long>`，本次匯入新增/更新過的 `StudentPopulation.Id`，Task 3/4/5/6/7 每次呼叫 `GetOrCreatePopulation` 後都要 `Add` 進去)
  - `PHStatistics.Portal.Services.Import.ImportScanItem`：`SchoolName` (string)、`SchoolId` (int?)、`Year`/`Week` (int)、`Exists` (bool)
  - `PHStatistics.Portal.Services.Import.ImportScanResult`：`Items` (`List<ImportScanItem>`)、`Errors` (`List<string>`)
  - `PHStatistics.Portal.Services.Import.ImportSupport.CourseMapping`：靜態欄位 `GradeOrder` (`string[]`)、`PsjCourseIds`/`AsCourseIds` (`Dictionary<string, int[]>`)、`DefaultSubmitterId` (`Guid`)、`PhExcludeSheets` (`HashSet<string>`)、`PhColCourseId` (`Dictionary<int, int>`)、`PsHeaderCourseId` (`Dictionary<string, int>`)、`Em1CourseIds` (`HashSet<int>`)；靜態方法 `PsjColumnType(string code)`、`AsColumnType(string code)`（皆回傳 `ClassType`）、`ReadCellNumber(IRow row, int col)`（回傳 `int`）、`AsChineseNumerals(string s)`（回傳 `string`）
  - `PHStatistics.Portal.Services.Import.ImportSupport.PopulationWriteHelper`：靜態方法 `GetOrCreatePopulation(DataContext db, int schoolId, int yearInt, int weekInt, SchoolYear schoolYear, StudentPopulationType type, string name, bool deleteExisting)` 回傳 `StudentPopulation`；`AddClassAndItem(DataContext db, int schoolId, Course course, ClassType cType, long populationId, int number, ImportResult result, ILogger logger)` 無回傳值
  - `PHStatistics.Portal.Services.Import.ImportSupport.TitleParser`：靜態方法 `ParseYearWeek(string title)` 回傳 `(int academicYear, int week)`

- [ ] **Step 1: 建立 `ImportResult.cs`**

```csharp
using System.Collections.Generic;

namespace PHStatistics.Portal.Services.Import;

public class ImportResult {
    public string Week { get; set; }
    public string File { get; set; }
    public string Type { get; set; }
    public int SchoolCount { get; set; }
    public int ItemCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public HashSet<long> PopulationIds { get; set; } = new();
}

public class ImportScanItem {
    public string SchoolName { get; set; }
    public int? SchoolId { get; set; }
    public int Year { get; set; }
    public int Week { get; set; }
    public bool Exists { get; set; }
}

public class ImportScanResult {
    public List<ImportScanItem> Items { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
```

- [ ] **Step 2: 建立 `ImportSupport/CourseMapping.cs`**

這是從 `source/portal/Portal/Controllers/HomeController.cs:3269-3357` 原樣搬移，只把 `private static readonly` 改為 `public static readonly`、`private static` 方法改為 `public static`、底線開頭欄位改成 PascalCase：

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import.ImportSupport;

public static class CourseMapping {
    public static readonly string[] GradeOrder = {
        "一年級","二年級","三年級","四年級","五年級","六年級",
        "國一","國二","國三","高一","高二","高三"
    };

    public static readonly Dictionary<string, int[]> PsjCourseIds = new() {
        ["MP"] = new[]{145,146,147,148,149,150,151,152,153,154,155,156},
        ["MS"] = new[]{145,146,147,148,149,150,151,152,153,154,155,156},
        ["SP"] = new[]{195,196,197,198,199,200,201,202,203,204,205,206},
        ["SS"] = new[]{195,196,197,198,199,200,201,202,203,204,205,206},
        ["N"]  = new[]{171,172,173,174,175,176,177,178,179,180,181,182},
        ["L"]  = new[]{183,184,185,186,187,188,189,190,191,192,193,194},
        ["W"]  = new[]{159,160,161,162,163,164,165,166,167,168,169,170},
    };

    public static readonly Dictionary<string, int[]> AsCourseIds = new() {
        ["AS"] = new[]{245,246,247,248,249,250,251,252,253,254,255,256},
        ["EP"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
        ["EG"] = new[]{295,296,297,298,299,300,301,302,303,305,306,307},
        ["N"]  = new[]{271,272,273,274,275,276,277,278,279,280,281,282},
        ["L"]  = new[]{283,284,285,286,287,288,289,290,291,292,293,294},
        ["W"]  = new[]{259,260,261,262,263,264,265,266,267,268,269,270},
    };

    public static readonly Guid DefaultSubmitterId = Guid.Parse("23858D7E-F622-4D15-4A74-08DC7A5137DD");

    // 排除在 PH 匯入之外的頁籤名稱（英檢、舊版本、非人數表頁籤）
    public static readonly HashSet<string> PhExcludeSheets = new(StringComparer.OrdinalIgnoreCase) {
        "英檢", "舊版本", "南區 (舊版本)", "中北區（舊版本）", "Rocky班", "各校開班數"
    };

    // PH: Excel column index → DB Course ID (hardcoded from 百瀚英語南區 multi-level header layout)
    public static readonly Dictionary<int, int> PhColCourseId = new() {
        [9]=1,  [10]=1,                                                    // P1-初階
        [11]=2, [12]=2, [13]=2,                                            // P2-先階
        [14]=3, [15]=3, [16]=3, [17]=3, [18]=3,                            // P3-中階
        [19]=4, [20]=4, [21]=4, [22]=4,                                    // P4-進階
        [23]=5, [24]=5, [25]=5,                                            // P5-高階
        [26]=6, [27]=6, [28]=6,                                            // P6-優階
        [29]=7, [30]=8,                                                    // SAT Junior A/B
        [32]=10,[33]=10,[34]=10,[35]=10,[36]=10,                           // 國一準特/特訓
        [37]=11,[38]=11,[39]=11,[40]=11,[41]=11,[42]=11,                   // 國二準特/特訓
        [43]=12,[44]=12,[45]=12,[46]=12,[47]=12,                           // 國三準特/特訓
        [48]=13,[49]=14,[50]=15,                                           // 海外特訓班-TOEFL/SSAT/PSAT
        [52]=17,[53]=17,                                                   // Elite/英檢/sat班系
        [54]=18,[55]=18,[56]=18,[57]=18,[58]=18,                           // 高中小組班-高一
        [59]=19,[60]=19,[61]=19,[62]=19,[63]=19,[64]=19,[65]=19,           // 高中小組班-高二
        [66]=20,[67]=20,[68]=20,[69]=20,                                   // 高中小組班-高三
        [72]=23,[73]=24,[74]=25,[75]=26,                                   // 英文個別指導
        [77]=28,[78]=29,[79]=30,[80]=31,                                   // 英文合作開班1-4
        [89]=39,[90]=39,[91]=39,[92]=39,[93]=39,                           // 國語文國小三力
        [94]=40,[95]=40,                                                   // 國語文國小中階
        [96]=41,[97]=41,[98]=41,                                           // 國語文國小攻略
        [99]=42,[100]=42,                                                  // 國語文國一班
        [101]=43,[102]=43,                                                 // 國語文國二班
        [103]=44,[104]=44,                                                 // 國語文國三班
        [105]=45,[106]=46,[107]=47,                                        // 國語文高一/二/三班
        [110]=50,[111]=51,[112]=52,[113]=53,                               // 國語文個別指導
        [115]=55,[116]=56,[117]=57,[118]=58,                               // 國語文合作開班
    };

    // PS: Excel column header → DB Course ID (aliases for multi-variant class names)
    public static readonly Dictionary<string, int> PsHeaderCourseId = new(StringComparer.OrdinalIgnoreCase) {
        ["一資"]=108, ["一特"]=109,
        ["二資"]=110, ["二特"]=111, ["二PS特"]=111, ["二特2"]=111,
        ["三資"]=112, ["三特"]=113, ["三特Ps"]=113, ["三特1"]=113, ["三特2"]=113,
        ["四資"]=114, ["四特"]=115, ["四ps特"]=115, ["四P特"]=115, ["四S特"]=115, ["四特1"]=115, ["四特2"]=115,
        ["五資"]=116, ["五特"]=117, ["五P特"]=117, ["五S特"]=117, ["五資1"]=116, ["五特2"]=117,
        ["六資"]=118, ["六特"]=119, ["六P特"]=119, ["六資1"]=118, ["六資2"]=118, ["六特1"]=119, ["六特2"]=119,
        ["七資"]=121, ["七特"]=122, ["七PS特"]=122,
        ["八資"]=123, ["八特"]=124, ["八特1"]=124, ["八特2"]=124, ["八特3"]=124, ["八課內"]=124,
        ["九資"]=125, ["九特"]=126, ["九資1"]=125, ["九資2"]=125,
        ["高一特"]=128, ["高二特"]=129, ["高三特"]=130,
    };

    // CourseIds that use EM1 logic: count = # individual students, each gets its own class record of 1
    public static readonly HashSet<int> Em1CourseIds = new() { 23, 24, 25, 26, 50, 51, 52, 53 };

    public static ClassType PsjColumnType(string code) => code switch {
        "MP" or "SP" => ClassType.Personal,
        "MS" or "SS" => ClassType.SubGroup,
        _ => ClassType.General,
    };

    public static ClassType AsColumnType(string code) => code switch {
        "EP" or "MP" or "SP" => ClassType.Personal,
        "ES" or "MS" or "SS" => ClassType.SubGroup,
        _ => ClassType.General,
    };

    public static int ReadCellNumber(IRow row, int col) {
        var cell = row.GetCell(col);
        if (cell == null) return 0;
        try {
            if (cell.CellType == CellType.Formula) {
                cell.SetCellType(CellType.Numeric);
                return (int)Math.Round(cell.NumericCellValue, MidpointRounding.AwayFromZero);
            }
            return int.TryParse(cell.ToString().Trim(), out int v) ? v : 0;
        }
        catch {
            return 0;
        }
    }

    public static string AsChineseNumerals(string s) {
        static string Convert(int n) {
            string[] d = { "", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            if (n < 10) return d[n];
            if (n < 20) return "十" + (n % 10 == 0 ? "" : d[n % 10]);
            return d[n / 10] + "十" + (n % 10 == 0 ? "" : d[n % 10]);
        }
        return Regex.Replace(s, @"\d+", m =>
            int.TryParse(m.Value, out int n) && n >= 1 && n <= 99 ? Convert(n) : m.Value);
    }
}
```

- [ ] **Step 3: 建立 `ImportSupport/PopulationWriteHelper.cs`**

從 `HomeController.cs:3374-3439` 搬移，`_defaultSubmitterId` 改參照 `CourseMapping.DefaultSubmitterId`，`Logger?.LogError` 改為傳入的 `logger?.LogError`，`ImportAllResult` 改為 `ImportResult`：

```csharp
using System;
using Microsoft.Extensions.Logging;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import.ImportSupport;

public static class PopulationWriteHelper {
    public static StudentPopulation GetOrCreatePopulation(DataContext db, int schoolId, int yearInt, int weekInt,
        SchoolYear schoolYear, StudentPopulationType type, string name, bool deleteExisting) {
        StudentPopulation pop;
        if (deleteExisting && db.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type)) {
            pop = db.StudentPopulation.First(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type);
            var delItems = db.StudentPopulationItem.Where(e => e.StudentPopulation.Id == pop.Id).ToList();
            var classIds = delItems.Select(e => e.ClassId).Distinct().ToList();
            db.StudentPopulationItem.RemoveRange(delItems);
            db.SaveChanges();
            var orphanClasses = db.Class
                .Where(e => classIds.Contains(e.Id) && !db.StudentPopulationItem.Any(i => i.ClassId == e.Id))
                .ToList();
            if (orphanClasses.Count > 0) {
                db.Class.RemoveRange(orphanClasses);
                db.SaveChanges();
            }
        }
        else if (db.StudentPopulation.Any(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type)) {
            pop = db.StudentPopulation.First(e => e.School.Id == schoolId && e.Year == yearInt && e.Week == weekInt && e.Type == type);
        }
        else {
            pop = new StudentPopulation {
                SchoolId = schoolId,
                Year = yearInt,
                Week = schoolYear.Week.Value,
                WeekDate = schoolYear.WeekStartDate,
                Items = new System.Collections.Generic.List<StudentPopulationItem>(),
                Submitter = db.Member.Find(CourseMapping.DefaultSubmitterId),
                Type = type,
                Name = name,
            };
            db.StudentPopulation.Add(pop);
            db.SaveChanges();
        }
        return pop;
    }

    public static void AddClassAndItem(DataContext db, int schoolId, Course course, ClassType cType, long populationId, int number, ImportResult result, ILogger logger) {
        var newClass = new Class();
        try {
            int classCount = db.StudentPopulationItem.Count(e => e.Class.Course.Id == course.Id);
            newClass.Course = null;
            newClass.CourseId = course.Id;
            newClass.SchoolId = schoolId;
            newClass.Type = cType;
            newClass.Name = $"{course.Name}_{(classCount + 1):00}";
            db.Class.Add(newClass);
            db.SaveChanges();
        }
        catch (Exception ex) {
            logger?.LogError("ImportAll 新增班級錯誤: {msg}", ex.Message);
            return;
        }
        db.StudentPopulationItem.Add(new StudentPopulationItem {
            Class = null,
            ClassId = newClass.Id,
            Name = newClass.Name,
            Number = number,
            SchoolName = newClass.Name,
            LastWeekNumber = 0,
            StudentPopulation = null,
            StudentPopulationId = (int)populationId,
        });
        db.SaveChanges();
        result.ItemCount++;
    }
}
```

Note: `db.StudentPopulation.Any(...)`/`.First(...)`/`.Where(...)` 等需要 `System.Linq`，加上 `using System.Linq;`。完整 using 區塊：

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PHStatistics.Content;
```

- [ ] **Step 4: 建立 `ImportSupport/TitleParser.cs`**

從 `HomeController.cs:3441-3452` 搬移，方法名稱由 `ParseYearWeekFromTitle` 改為 `ParseYearWeek`（放進專門的 `TitleParser` 類別後不需要重複「FromTitle」字尾）：

```csharp
using System.Text.RegularExpressions;

namespace PHStatistics.Portal.Services.Import.ImportSupport;

public static class TitleParser {
    public static (int academicYear, int week) ParseYearWeek(string title) {
        var weekMatch = Regex.Match(title, @"第(\d+)週");
        int week = weekMatch.Success ? int.Parse(weekMatch.Groups[1].Value) : 0;
        var dateMatch = Regex.Match(title, @"(\d+)年(\d+)月");
        int academicYear = 0;
        if (dateMatch.Success) {
            int rocYear = int.Parse(dateMatch.Groups[1].Value);
            int month = int.Parse(dateMatch.Groups[2].Value);
            academicYear = month < 8 ? rocYear - 1 : rocYear;
        }
        return (academicYear, week);
    }
}
```

- [ ] **Step 5: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded（這 4 個新檔案目前沒有任何呼叫者，純新增不影響既有程式碼，應該 0 錯誤）

- [ ] **Step 6: Commit**

```bash
git add source/portal/Portal/Services/Import/
git commit -m "feat: extract shared import support types (CourseMapping, PopulationWriteHelper, TitleParser)"
```

---

### Task 3: 抽出 PH/GEPT 共用 Sheet 解析器（PHSheetReader）

**Files:**
- Create: `source/portal/Portal/Services/Import/ImportSupport/PHSheetReader.cs`

**Interfaces:**
- Consumes: `CourseMapping.PhColCourseId`、`CourseMapping.Em1CourseIds`（Task 2）、`PopulationWriteHelper.GetOrCreatePopulation`/`AddClassAndItem`（Task 2）、`TitleParser.ParseYearWeek`（Task 2）
- Produces: `PHStatistics.Portal.Services.Import.ImportSupport.PHSheetReader` 靜態方法：
  - `Run(DataContext db, ISheet sheet, StudentPopulationType popType, string typeLabel, string nameTemplate, bool requireTypeIndicator, ILogger logger)` 回傳 `ImportResult`（供 Task 4 的 PH/GEPT Importer 呼叫）
  - `Scan(DataContext db, ISheet sheet, StudentPopulationType popType, bool requireTypeIndicator)` 回傳 `ImportScanResult`（供 Task 4 的 PH/GEPT Importer 呼叫，新邏輯，非搬移）

- [ ] **Step 1: 建立 `Run` 方法**

從 `HomeController.cs:3457-3559`（`RunImportSheetPH`）搬移，方法名稱改為 `Run`，`ImportAllResult`→`ImportResult`，`GetOrCreatePopulation`/`AddClassAndItem`/`ParseYearWeekFromTitle`/`_phColCourseId`/`_em1CourseIds` 改為對應的 `PopulationWriteHelper.`/`TitleParser.`/`CourseMapping.` 呼叫，`AddClassAndItem` 呼叫加上 `logger` 參數，並在每次呼叫 `GetOrCreatePopulation` 後把回傳的 `pop.Id` 加進 `result.PopulationIds`：

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import.ImportSupport;

public static class PHSheetReader {
    // Shared sheet reader for PH-style sheets (title row 0, headers rows 1-3, data from row 4).
    // requireTypeIndicator=true  → only process rows where col1 is "小" or "三" (PH behaviour)
    // requireTypeIndicator=false → rows without "小"/"三" are treated as ClassType.General (GEPT behaviour)
    public static ImportResult Run(DataContext db, ISheet sheet,
        StudentPopulationType popType, string typeLabel, string nameTemplate,
        bool requireTypeIndicator, ILogger logger) {

        var result = new ImportResult { File = sheet.SheetName, Type = typeLabel };

        // 標題可能不在 Cell 0（合併儲存格），掃 Row 0 找第一個非空儲存格
        string title = "";
        IRow titleRow = sheet.GetRow(0);
        if (titleRow != null) {
            for (int ci = 0; ci < Math.Min(10, (int)titleRow.LastCellNum + 1); ci++) {
                string v = titleRow.GetCell(ci)?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(v)) { title = v; break; }
            }
        }
        var (yearInt, weekInt) = TitleParser.ParseYearWeek(title);
        if (yearInt == 0 || weekInt == 0) {
            result.Errors.Add($"無法從標題解析年份週次: {title}");
            return result;
        }
        SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
        if (schoolYear == null) {
            result.Errors.Add($"找不到學年週次: {yearInt}第{weekInt}週");
            return result;
        }

        int maxHdrCol = 0;
        for (int r = 1; r <= 3; r++) {
            var rw = sheet.GetRow(r);
            if (rw != null && (int)rw.LastCellNum > maxHdrCol) maxHdrCol = (int)rw.LastCellNum;
        }

        var row2Prop = new string[maxHdrCol + 1];
        string lastR2 = "";
        for (int c = 0; c <= maxHdrCol; c++) {
            var cell = sheet.GetRow(2)?.GetCell(c);
            string v = (cell?.CellType == CellType.String) ? (cell.StringCellValue?.Trim() ?? "") : "";
            if (!string.IsNullOrEmpty(v)) lastR2 = v;
            row2Prop[c] = lastR2;
        }

        var colCourseMap = new Dictionary<int, Course>();
        for (int c = 2; c < maxHdrCol; c++) {
            var r3Cell = sheet.GetRow(3)?.GetCell(c);
            string r3Val = (r3Cell?.CellType == CellType.String) ? (r3Cell.StringCellValue?.Trim() ?? "") : "";
            string label = !string.IsNullOrEmpty(r3Val) ? r3Val : row2Prop[c];
            Course course = null;
            if (!string.IsNullOrEmpty(label))
                course = db.Course.Include("Department").FirstOrDefault(e => e.Name == label);
            if (course == null && CourseMapping.PhColCourseId.TryGetValue(c, out int fallbackId))
                course = db.Course.Include("Department").FirstOrDefault(e => e.Id == fallbackId);
            if (course != null) colCourseMap[c] = course;
        }

        string currentSchoolName = "";
        string lastCountedSchool = "";
        for (int rNo = 4; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string col0 = row.GetCell(0)?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(col0)) currentSchoolName = col0;
            if (string.IsNullOrEmpty(currentSchoolName)) continue;

            string col1 = row.GetCell(1)?.ToString()?.Trim() ?? "";
            ClassType cType;
            bool deleteExisting;
            bool isFirstRow;

            if (col1 == "小") {
                cType = ClassType.SubGroup; deleteExisting = true; isFirstRow = true;
            } else if (col1 == "三") {
                cType = ClassType.V3; deleteExisting = false; isFirstRow = false;
            } else if (!requireTypeIndicator) {
                cType = ClassType.General;
                isFirstRow = currentSchoolName != lastCountedSchool;
                deleteExisting = isFirstRow;
            } else {
                continue;
            }

            School school = db.School.FirstOrDefault(e => e.Name == currentSchoolName);
            if (school == null) continue;

            StudentPopulation pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                popType, string.Format(nameTemplate, yearInt, weekInt), deleteExisting);
            result.PopulationIds.Add(pop.Id);
            if (isFirstRow) { result.SchoolCount++; lastCountedSchool = currentSchoolName; }

            foreach (var (col, course) in colCourseMap) {
                var cell = row.GetCell(col);
                if (cell == null || cell.CellType != CellType.Numeric) continue;
                int count = (int)Math.Round(cell.NumericCellValue, MidpointRounding.AwayFromZero);
                if (count <= 0) continue;
                if (CourseMapping.Em1CourseIds.Contains(course.Id)) {
                    for (int i = 0; i < count; i++)
                        PopulationWriteHelper.AddClassAndItem(db, school.Id, course, cType, pop.Id, 1, result, logger);
                } else {
                    PopulationWriteHelper.AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result, logger);
                }
            }
        }
        return result;
    }
}
```

- [ ] **Step 2: 新增 `Scan` 方法（輕量掃描，新邏輯）**

在同一個 `PHSheetReader` 類別中加入 `Scan`。這不是搬移，是新寫的邏輯，但刻意鏡射 `Run` 的「標題解析」與「逐列判斷 isFirstRow」流程，只是不建立班級/項目、只收集本 Sheet 涉及的分校清單：

```csharp
    public static ImportScanResult Scan(DataContext db, ISheet sheet, StudentPopulationType popType, bool requireTypeIndicator) {
        var result = new ImportScanResult();

        string title = "";
        IRow titleRow = sheet.GetRow(0);
        if (titleRow != null) {
            for (int ci = 0; ci < Math.Min(10, (int)titleRow.LastCellNum + 1); ci++) {
                string v = titleRow.GetCell(ci)?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(v)) { title = v; break; }
            }
        }
        var (yearInt, weekInt) = TitleParser.ParseYearWeek(title);
        if (yearInt == 0 || weekInt == 0) {
            result.Errors.Add($"無法從標題解析年份週次: {title}");
            return result;
        }

        string currentSchoolName = "";
        string lastCountedSchool = "";
        var seen = new HashSet<string>();
        for (int rNo = 4; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string col0 = row.GetCell(0)?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(col0)) currentSchoolName = col0;
            if (string.IsNullOrEmpty(currentSchoolName)) continue;

            string col1 = row.GetCell(1)?.ToString()?.Trim() ?? "";
            bool isFirstRow;
            if (col1 == "小") {
                isFirstRow = true;
            } else if (col1 == "三") {
                isFirstRow = false;
            } else if (!requireTypeIndicator) {
                isFirstRow = currentSchoolName != lastCountedSchool;
            } else {
                continue;
            }

            if (!isFirstRow || seen.Contains(currentSchoolName)) continue;
            seen.Add(currentSchoolName);
            lastCountedSchool = currentSchoolName;

            School school = db.School.FirstOrDefault(e => e.Name == currentSchoolName);
            bool exists = school != null && db.StudentPopulation.Any(e =>
                e.School.Id == school.Id && e.Year == yearInt && e.Week == weekInt && e.Type == popType);
            result.Items.Add(new ImportScanItem {
                SchoolName = currentSchoolName,
                SchoolId = school?.Id,
                Year = yearInt,
                Week = weekInt,
                Exists = exists,
            });
        }
        return result;
    }
```

- [ ] **Step 3: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Services/Import/ImportSupport/PHSheetReader.cs
git commit -m "feat: extract PHSheetReader with Run and Scan methods"
```

---

### Task 4: `IPopulationImporter` 介面 + PH/GEPT Importer

**Files:**
- Create: `source/portal/Portal/Services/Import/IPopulationImporter.cs`
- Create: `source/portal/Portal/Services/Import/PHPopulationImporter.cs`
- Create: `source/portal/Portal/Services/Import/GeptPopulationImporter.cs`

**Interfaces:**
- Consumes: `PHSheetReader.Run`/`Scan`（Task 3）、`CourseMapping.PhExcludeSheets`（Task 2）
- Produces: `PHStatistics.Portal.Services.Import.IPopulationImporter` 介面（`StudentPopulationType Type { get; }`、`ImportScanResult Scan(DataContext db, Stream fileStream)`、`ImportResult Import(DataContext db, Stream fileStream, ILogger logger)`），供 Task 5/6/7/8 實作與呼叫

- [ ] **Step 1: 建立介面**

```csharp
using System.IO;
using Microsoft.Extensions.Logging;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import;

public interface IPopulationImporter {
    StudentPopulationType Type { get; }
    ImportScanResult Scan(DataContext db, Stream fileStream);
    ImportResult Import(DataContext db, Stream fileStream, ILogger logger);
}
```

- [ ] **Step 2: 建立 `PHPopulationImporter`**

`Import` 從 `HomeController.cs:3561-3575`（`RunImportPH`）改寫而成，改為呼叫 `PHSheetReader.Run`；因為輸入來源從檔案路徑改為 `Stream`，不再自行設定 `.File`（由 `PopulationImportService` 統一在呼叫端設定檔名）：

```csharp
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Services.Import;

public class PHPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.PH;

    public ImportScanResult Scan(DataContext db, Stream fileStream) {
        var wb = new XSSFWorkbook(fileStream);
        var combined = new ImportScanResult();
        for (int s = 0; s < wb.NumberOfSheets; s++) {
            var sheet = wb.GetSheetAt(s);
            if (CourseMapping.PhExcludeSheets.Contains(sheet.SheetName)) continue;
            var r = PHSheetReader.Scan(db, sheet, StudentPopulationType.PH, requireTypeIndicator: true);
            combined.Items.AddRange(r.Items);
            combined.Errors.AddRange(r.Errors);
        }
        return combined;
    }

    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger) {
        var wb = new XSSFWorkbook(fileStream);
        var combined = new ImportResult { Type = "PH" };
        for (int s = 0; s < wb.NumberOfSheets; s++) {
            var sheet = wb.GetSheetAt(s);
            if (CourseMapping.PhExcludeSheets.Contains(sheet.SheetName)) continue;
            var r = PHSheetReader.Run(db, sheet, StudentPopulationType.PH,
                "PH", "{0}第{1}週百瀚人數表", requireTypeIndicator: true, logger);
            combined.SchoolCount += r.SchoolCount;
            combined.ItemCount += r.ItemCount;
            combined.Errors.AddRange(r.Errors);
            foreach (long id in r.PopulationIds) combined.PopulationIds.Add(id);
        }
        return combined;
    }
}
```

- [ ] **Step 3: 建立 `GeptPopulationImporter`**

`Import` 從 `HomeController.cs:3577-3585`（`RunImportGEPT`）改寫：

```csharp
using System.IO;
using Microsoft.Extensions.Logging;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Services.Import;

public class GeptPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.GEPT;

    public ImportScanResult Scan(DataContext db, Stream fileStream) {
        var wb = new XSSFWorkbook(fileStream);
        if (wb.NumberOfSheets < 3) {
            var r = new ImportScanResult();
            r.Errors.Add("找不到第3個頁籤（英檢）");
            return r;
        }
        return PHSheetReader.Scan(db, wb.GetSheetAt(2), StudentPopulationType.GEPT, requireTypeIndicator: false);
    }

    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger) {
        var wb = new XSSFWorkbook(fileStream);
        if (wb.NumberOfSheets < 3)
            return new ImportResult { Type = "GEPT", Errors = { "找不到第3個頁籤（英檢）" } };
        return PHSheetReader.Run(db, wb.GetSheetAt(2), StudentPopulationType.GEPT,
            "GEPT", "{0}第{1}週英檢人數表", requireTypeIndicator: false, logger);
    }
}
```

- [ ] **Step 4: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Services/Import/IPopulationImporter.cs source/portal/Portal/Services/Import/PHPopulationImporter.cs source/portal/Portal/Services/Import/GeptPopulationImporter.cs
git commit -m "feat: add IPopulationImporter interface with PH and GEPT implementations"
```

---

### Task 5: `PSPopulationImporter`

**Files:**
- Create: `source/portal/Portal/Services/Import/PSPopulationImporter.cs`

**Interfaces:**
- Consumes: `IPopulationImporter`（Task 4）、`CourseMapping.PsHeaderCourseId`（Task 2）、`PopulationWriteHelper`（Task 2）、`TitleParser`（Task 2）
- Produces: `PHStatistics.Portal.Services.Import.PSPopulationImporter`，供 Task 8 的 `PopulationImportService` 註冊使用

- [ ] **Step 1: 建立 `Scan`（新邏輯，鏡射 `Import` 的標題/分校解析）**

```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Services.Import;

public class PSPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.PS;

    public ImportScanResult Scan(DataContext db, Stream fileStream) {
        var result = new ImportScanResult();
        var sheet = new XSSFWorkbook(fileStream).GetSheetAt(0);

        string title = sheet.GetRow(0)?.GetCell(0)?.ToString()?.Trim() ?? "";
        var (yearInt, weekInt) = TitleParser.ParseYearWeek(title);
        if (yearInt == 0 || weekInt == 0) {
            result.Errors.Add($"無法從標題解析年份週次: {title}");
            return result;
        }

        for (int rNo = 2; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string schoolName = row.GetCell(0)?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(schoolName) || schoolName == "總計") continue;

            School school = db.School.FirstOrDefault(e => e.Name == schoolName);
            bool exists = school != null && db.StudentPopulation.Any(e =>
                e.School.Id == school.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PS);
            result.Items.Add(new ImportScanItem {
                SchoolName = schoolName,
                SchoolId = school?.Id,
                Year = yearInt,
                Week = weekInt,
                Exists = exists,
            });
        }
        return result;
    }
```

- [ ] **Step 2: 建立 `Import`**

從 `HomeController.cs:3587-3649`（`RunImportPS`）改寫：

```csharp
    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger) {
        var result = new ImportResult { Type = "PS" };
        var sheet = new XSSFWorkbook(fileStream).GetSheetAt(0);

        // Parse year/week from row 0 title
        string title = sheet.GetRow(0)?.GetCell(0)?.ToString()?.Trim() ?? "";
        var (yearInt, weekInt) = TitleParser.ParseYearWeek(title);
        if (yearInt == 0 || weekInt == 0) {
            result.Errors.Add($"無法從標題解析年份週次: {title}");
            return result;
        }
        SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
        if (schoolYear == null) {
            result.Errors.Add($"找不到學年週次: {yearInt}第{weekInt}週");
            return result;
        }

        // Build col→course map from row 1 headers; fallback to alias dict when Course.Name doesn't match
        IRow headerRow = sheet.GetRow(1);
        var colCourseMap = new Dictionary<int, Course>();
        if (headerRow != null) {
            for (int c = 1; c < (int)headerRow.LastCellNum; c++) {
                var hCell = headerRow.GetCell(c);
                if (hCell == null || hCell.CellType != CellType.String) continue;
                string hdr = hCell.StringCellValue?.Trim() ?? "";
                if (string.IsNullOrEmpty(hdr)) continue;
                Course course = db.Course.Include("Department").FirstOrDefault(e => e.Name == hdr);
                if (course == null && CourseMapping.PsHeaderCourseId.TryGetValue(hdr, out int fallbackId))
                    course = db.Course.Include("Department").FirstOrDefault(e => e.Id == fallbackId);
                if (course != null) colCourseMap[c] = course;
            }
        }
        if (colCourseMap.Count == 0) {
            result.Errors.Add("Row 1 未找到任何課程對應，請確認課程名稱或別名是否與資料庫一致");
            return result;
        }

        // Process data rows starting at row 2
        for (int rNo = 2; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string schoolName = row.GetCell(0)?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(schoolName) || schoolName == "總計") continue;

            School school = db.School.FirstOrDefault(e => e.Name == schoolName);
            if (school == null) continue;

            StudentPopulation pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                StudentPopulationType.PS, $"{yearInt}第{weekInt}週百世人數表", true);
            result.PopulationIds.Add(pop.Id);
            result.SchoolCount++;

            foreach (var (col, course) in colCourseMap) {
                var cell = row.GetCell(col);
                if (cell == null || cell.CellType != CellType.Numeric) continue;
                int count = (int)System.Math.Round(cell.NumericCellValue, System.MidpointRounding.AwayFromZero);
                if (count <= 0) continue;
                PopulationWriteHelper.AddClassAndItem(db, school.Id, course, ClassType.General, pop.Id, count, result, logger);
            }
        }
        return result;
    }
}
```

- [ ] **Step 3: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Services/Import/PSPopulationImporter.cs
git commit -m "feat: add PSPopulationImporter"
```

---

### Task 6: `PSJPopulationImporter`

**Files:**
- Create: `source/portal/Portal/Services/Import/PSJPopulationImporter.cs`

**Interfaces:**
- Consumes: `IPopulationImporter`（Task 4）、`CourseMapping.GradeOrder`/`PsjCourseIds`/`PsjColumnType`/`ReadCellNumber`（Task 2）、`PopulationWriteHelper`（Task 2）
- Produces: `PHStatistics.Portal.Services.Import.PSJPopulationImporter`，供 Task 8 註冊使用

- [ ] **Step 1: 建立 `Scan`（新邏輯，鏡射 `Import` 逐列讀取 年/週/分校）**

```csharp
using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Services.Import;

public class PSJPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.PSJ;

    public ImportScanResult Scan(DataContext db, Stream fileStream) {
        var result = new ImportScanResult();
        var sheet = new XSSFWorkbook(fileStream).GetSheetAt(0);
        var seen = new System.Collections.Generic.HashSet<(int year, int week, string school)>();

        for (int rNo = 5; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string schoolName = row.GetCell(2)?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(schoolName)) continue;
            if (!int.TryParse(row.GetCell(0)?.ToString()?.Trim(), out int yearInt)) continue;
            if (!int.TryParse(row.GetCell(1)?.ToString()?.Trim(), out int weekInt)) continue;
            string grade = row.GetCell(3)?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(grade)) continue;

            var key = (yearInt, weekInt, schoolName);
            if (seen.Contains(key)) continue;
            seen.Add(key);

            School school = db.School.FirstOrDefault(e => e.Name == schoolName);
            bool exists = school != null && db.StudentPopulation.Any(e =>
                e.School.Id == school.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.PSJ);
            result.Items.Add(new ImportScanItem {
                SchoolName = schoolName,
                SchoolId = school?.Id,
                Year = yearInt,
                Week = weekInt,
                Exists = exists,
            });
        }
        return result;
    }
```

- [ ] **Step 2: 建立 `Import`**

從 `HomeController.cs:3651-3711`（`RunImportPSJ`）改寫：

```csharp
    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger) {
        var result = new ImportResult { Type = "PSJ" };
        var sheet = new XSSFWorkbook(fileStream).GetSheetAt(0);
        IRow headerRow = sheet.GetRow(4);

        int prevSchoolId = 0;
        StudentPopulation pop = null;

        for (int rNo = 5; rNo <= sheet.LastRowNum; rNo++) {
            IRow row = sheet.GetRow(rNo);
            if (row == null) continue;

            string schoolName = row.GetCell(2)?.ToString()?.Trim() ?? "";
            School school = db.School.FirstOrDefault(e => e.Name == schoolName);
            if (school == null) continue;

            if (!int.TryParse(row.GetCell(0)?.ToString()?.Trim(), out int yearInt)) continue;
            if (!int.TryParse(row.GetCell(1)?.ToString()?.Trim(), out int weekInt)) continue;
            string grade = row.GetCell(3)?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(grade)) continue;

            SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
            if (schoolYear == null) continue;

            if (school.Id != prevSchoolId) {
                prevSchoolId = school.Id;
                pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                    StudentPopulationType.PSJ, $"{yearInt}第{weekInt}週百倍速人數表", true);
                result.PopulationIds.Add(pop.Id);
                result.SchoolCount++;
            }

            int gradeIdx = Array.IndexOf(CourseMapping.GradeOrder, grade);
            if (gradeIdx < 0) continue;

            for (int cNo = 4; cNo < headerRow.LastCellNum; cNo++) {
                try {
                    string code = headerRow.GetCell(cNo)?.ToString()?.Trim() ?? "";
                    if (code.Equals("X", StringComparison.OrdinalIgnoreCase)) continue;

                    int courseId;
                    ClassType cType;
                    if (code == "T") {
                        courseId = 158; cType = ClassType.General;
                    }
                    else if (CourseMapping.PsjCourseIds.TryGetValue(code, out int[] ids)) {
                        courseId = ids[gradeIdx]; cType = CourseMapping.PsjColumnType(code);
                    }
                    else { continue; }

                    Course course = db.Course.Include("Department").FirstOrDefault(e => e.Id == courseId);
                    int count = CourseMapping.ReadCellNumber(row, cNo);
                    if (course == null || count <= 0) continue;

                    PopulationWriteHelper.AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result, logger);
                }
                catch { continue; }
            }
        }
        return result;
    }
}
```

- [ ] **Step 3: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Services/Import/PSJPopulationImporter.cs
git commit -m "feat: add PSJPopulationImporter"
```

---

### Task 7: `ASPopulationImporter`

**Files:**
- Create: `source/portal/Portal/Services/Import/ASPopulationImporter.cs`

**Interfaces:**
- Consumes: `IPopulationImporter`（Task 4）、`CourseMapping.GradeOrder`/`AsCourseIds`/`ReadCellNumber`/`AsChineseNumerals`（Task 2）、`PopulationWriteHelper`（Task 2）
- Produces: `PHStatistics.Portal.Services.Import.ASPopulationImporter`，供 Task 8 註冊使用

- [ ] **Step 1: 建立 `Scan`（新邏輯，鏡射 `Import` 逐頁籤解析標題/週次）**

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import.ImportSupport;

namespace PHStatistics.Portal.Services.Import;

public class ASPopulationImporter : IPopulationImporter {
    public StudentPopulationType Type => StudentPopulationType.AfterSchool;

    public ImportScanResult Scan(DataContext db, Stream fileStream) {
        var result = new ImportScanResult();
        var workbook = new XSSFWorkbook(fileStream);

        for (int sheetIdx = 0; sheetIdx < workbook.NumberOfSheets; sheetIdx++) {
            var sheet = workbook.GetSheetAt(sheetIdx);
            string schoolName = Regex.Replace(sheet.SheetName, @"^\d+", "").Trim();
            School school = db.School.FirstOrDefault(e => e.Name == schoolName)
                ?? db.School.FirstOrDefault(e => e.Name == CourseMapping.AsChineseNumerals(schoolName));
            if (school == null) {
                result.Errors.Add($"找不到分校: {sheet.SheetName} (解析為 {schoolName})");
                continue;
            }

            string title = sheet.GetRow(0)?.GetCell(0)?.ToString()?.Trim() ?? "";
            var yearMatch = Regex.Match(title, @"(\d+)學年度");
            if (!yearMatch.Success) {
                result.Errors.Add($"頁籤 {sheet.SheetName}: 無法從標題解析學年度: {title}");
                continue;
            }
            int yearInt = int.Parse(yearMatch.Groups[1].Value);

            int weekInt = 0;
            for (int r = 4; r <= sheet.LastRowNum; r++) {
                IRow wr = sheet.GetRow(r);
                if (wr == null) continue;
                if (int.TryParse(wr.GetCell(0)?.ToString()?.Trim(), out int w) && w > weekInt)
                    weekInt = w;
            }
            if (weekInt == 0) {
                result.Errors.Add($"頁籤 {sheet.SheetName}: 找不到有效週次");
                continue;
            }

            bool exists = db.StudentPopulation.Any(e =>
                e.School.Id == school.Id && e.Year == yearInt && e.Week == weekInt && e.Type == StudentPopulationType.AfterSchool);
            result.Items.Add(new ImportScanItem {
                SchoolName = schoolName,
                SchoolId = school.Id,
                Year = yearInt,
                Week = weekInt,
                Exists = exists,
            });
        }
        return result;
    }
```

- [ ] **Step 2: 建立 `Import`**

從 `HomeController.cs:3713-3796`（`RunImportAS`）改寫：

```csharp
    public ImportResult Import(DataContext db, Stream fileStream, ILogger logger) {
        var result = new ImportResult { Type = "AS" };
        var workbook = new XSSFWorkbook(fileStream);

        for (int sheetIdx = 0; sheetIdx < workbook.NumberOfSheets; sheetIdx++) {
            var sheet = workbook.GetSheetAt(sheetIdx);
            string schoolName = Regex.Replace(sheet.SheetName, @"^\d+", "").Trim();
            // 嘗試完全比對，失敗時轉換阿拉伯數字為中文（農16 → 農十六）
            School school = db.School.FirstOrDefault(e => e.Name == schoolName)
                ?? db.School.FirstOrDefault(e => e.Name == CourseMapping.AsChineseNumerals(schoolName));
            if (school == null) {
                result.Errors.Add($"找不到分校: {sheet.SheetName} (解析為 {schoolName})");
                continue;
            }

            string title = sheet.GetRow(0)?.GetCell(0)?.ToString()?.Trim() ?? "";
            var yearMatch = Regex.Match(title, @"(\d+)學年度");
            if (!yearMatch.Success) {
                result.Errors.Add($"頁籤 {sheet.SheetName}: 無法從標題解析學年度: {title}");
                continue;
            }
            int yearInt = int.Parse(yearMatch.Groups[1].Value);

            // 取最大週次（檔案可能含多週歷史資料，只匯入最新週）
            int weekInt = 0;
            for (int r = 4; r <= sheet.LastRowNum; r++) {
                IRow wr = sheet.GetRow(r);
                if (wr == null) continue;
                if (int.TryParse(wr.GetCell(0)?.ToString()?.Trim(), out int w) && w > weekInt)
                    weekInt = w;
            }
            if (weekInt == 0) {
                result.Errors.Add($"頁籤 {sheet.SheetName}: 找不到有效週次");
                continue;
            }

            SchoolYear schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt);
            if (schoolYear == null) {
                result.Errors.Add($"頁籤 {sheet.SheetName}: SchoolYear 不存在 (年{yearInt} 週{weekInt})");
                continue;
            }

            StudentPopulation pop = PopulationWriteHelper.GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                StudentPopulationType.AfterSchool, $"{yearInt}第{weekInt}週課輔人數表", true);
            result.PopulationIds.Add(pop.Id);
            result.SchoolCount++;

            var colDefs = new[] {
                (col: 3, code: "AS", cType: ClassType.General),
                (col: 4, code: "EP", cType: ClassType.Personal),
                (col: 5, code: "EG", cType: ClassType.General),
            };

            // col 0 的週次只出現在每週第一列，後續同週的列 col 0 為空
            // 用 currentWeek 追蹤目前所屬週次，確保每週所有年級列都被處理
            int currentWeek = 0;
            for (int rNo = 4; rNo <= sheet.LastRowNum; rNo++) {
                IRow row = sheet.GetRow(rNo);
                if (row == null) continue;

                string col0 = row.GetCell(0)?.ToString()?.Trim() ?? "";
                if (int.TryParse(col0, out int rowWeek) && rowWeek > 0)
                    currentWeek = rowWeek;

                if (currentWeek != weekInt) continue;

                string grade = row.GetCell(2)?.ToString()?.Trim() ?? "";
                int gradeIdx = Array.IndexOf(CourseMapping.GradeOrder, grade);
                if (gradeIdx < 0) continue;

                foreach (var (col, code, cType) in colDefs) {
                    try {
                        int courseId = CourseMapping.AsCourseIds[code][gradeIdx];
                        Course course = db.Course.Include("Department").FirstOrDefault(e => e.Id == courseId);
                        int count = CourseMapping.ReadCellNumber(row, col);
                        if (course == null || count <= 0) continue;
                        PopulationWriteHelper.AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result, logger);
                    }
                    catch { continue; }
                }
            }
        }
        return result;
    }
}
```

- [ ] **Step 3: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Services/Import/ASPopulationImporter.cs
git commit -m "feat: add ASPopulationImporter"
```

---

### Task 8: `PopulationImportService` 服務外觀 + 校正上週資料 + DI 註冊

**Files:**
- Create: `source/portal/Portal/Services/Import/PopulationImportService.cs`
- Modify: `source/portal/Portal/Startup.cs:38`

**Interfaces:**
- Consumes: `IPopulationImporter` 五個實作（Task 4/5/6/7）
- Produces: `PHStatistics.Portal.Services.Import.PopulationImportService`：
  - 建構子 `PopulationImportService(ILogger<PopulationImportService> logger)`（DI 注入，供 Task 9/10 的 Controller 建構子注入使用）
  - `ImportScanResult Scan(DataContext db, StudentPopulationType type, Stream fileStream)`
  - `ImportResult Import(DataContext db, StudentPopulationType type, Stream fileStream, string fileName)`（內部完成後自動呼叫校正上週資料，呼叫端不需要另外處理）

- [ ] **Step 1: 建立 `PopulationImportService.cs`**

```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using PHStatistics.Content;

namespace PHStatistics.Portal.Services.Import;

public class PopulationImportService {
    private readonly ILogger<PopulationImportService> _logger;
    private readonly Dictionary<StudentPopulationType, IPopulationImporter> _importers;

    public PopulationImportService(ILogger<PopulationImportService> logger) {
        _logger = logger;
        _importers = new Dictionary<StudentPopulationType, IPopulationImporter> {
            [StudentPopulationType.PH] = new PHPopulationImporter(),
            [StudentPopulationType.GEPT] = new GeptPopulationImporter(),
            [StudentPopulationType.PS] = new PSPopulationImporter(),
            [StudentPopulationType.PSJ] = new PSJPopulationImporter(),
            [StudentPopulationType.AfterSchool] = new ASPopulationImporter(),
        };
    }

    public ImportScanResult Scan(DataContext db, StudentPopulationType type, Stream fileStream) {
        if (!_importers.TryGetValue(type, out var importer)) {
            var r = new ImportScanResult();
            r.Errors.Add($"不支援的匯入類型: {type}");
            return r;
        }
        return importer.Scan(db, fileStream);
    }

    public ImportResult Import(DataContext db, StudentPopulationType type, Stream fileStream, string fileName) {
        if (!_importers.TryGetValue(type, out var importer))
            return new ImportResult { Type = type.ToString(), File = fileName, Errors = { $"不支援的匯入類型: {type}" } };

        var result = importer.Import(db, fileStream, _logger);
        result.File = fileName;

        foreach (long popId in result.PopulationIds) {
            CorrectLastWeekNumbers(db, popId);
        }
        return result;
    }

    // 校正上週資料：比照 HomeController.FillLastWeekNumbers 的 (CourseId, ClassType) 分組比對邏輯。
    // 不可用 ClassId 比對——匯入資料每次都建立新 Class 記錄，ClassId 跨週不會相同。
    private void CorrectLastWeekNumbers(DataContext db, long populationId) {
        var pop = db.StudentPopulation.Find(populationId);
        if (pop == null) return;

        var schoolYear = db.SchoolYear.FirstOrDefault(sy => sy.Year == pop.Year && sy.Week == pop.Week);
        if (schoolYear == null) return;

        SchoolYear prevSY = schoolYear.Week > 1
            ? db.SchoolYear.Where(sy => sy.Year == schoolYear.Year && sy.Week == schoolYear.Week - 1)
                           .OrderBy(sy => sy.Id).FirstOrDefault()
            : db.SchoolYear.Where(sy => sy.Year == schoolYear.Year - 1)
                           .OrderByDescending(sy => sy.Week).ThenByDescending(sy => sy.Id).FirstOrDefault();
        if (prevSY == null) return;

        var prevPop = db.StudentPopulation.FirstOrDefault(p =>
            p.SchoolId == pop.SchoolId && p.Year == prevSY.Year && p.Week == prevSY.Week && p.Type == pop.Type);
        if (prevPop == null) return;

        var prevItems = db.StudentPopulationItem
            .Include("Class")
            .Where(i => i.StudentPopulationId == prevPop.Id && i.ClassId != null)
            .ToList();
        var prevLookup = prevItems
            .GroupBy(i => (i.Class.CourseId, i.Class.Type))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Number));

        var currentItems = db.StudentPopulationItem
            .Include("Class")
            .Where(i => i.StudentPopulationId == pop.Id && i.ClassId != null)
            .ToList();

        int updated = 0;
        foreach (var grp in currentItems.GroupBy(i => (i.Class.CourseId, i.Class.Type))) {
            prevLookup.TryGetValue(grp.Key, out int prevTotal);
            bool isFirst = true;
            foreach (var item in grp) {
                item.LastWeekNumber = isFirst ? prevTotal : 0;
                isFirst = false;
                updated++;
            }
        }
        if (updated > 0) db.SaveChanges();
    }
}
```

- [ ] **Step 2: 在 `Startup.cs` 註冊 DI**

第 38 行：
```csharp
            services.AddScoped<ReportExportService>();
```
改為：
```csharp
            services.AddScoped<ReportExportService>();
            services.AddScoped<PHStatistics.Portal.Services.Import.PopulationImportService>();
```

- [ ] **Step 3: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Services/Import/PopulationImportService.cs source/portal/Portal/Startup.cs
git commit -m "feat: add PopulationImportService facade with LastWeekNumber correction"
```

---

### Task 9: 重構 `HomeController.ImportAll` 改用 `PopulationImportService`

**Files:**
- Modify: `source/portal/Portal/Controllers/HomeController.cs`

**Interfaces:**
- Consumes: `PopulationImportService.Import`（Task 8）
- Produces: 無新介面（`ImportAll` 端點路徑/參數/回傳 JSON 結構不變，行為對現有呼叫者透明）

- [ ] **Step 1: 修改類別宣告，注入 `PopulationImportService`**

第 45 行：
```csharp
    public class HomeController() : MvcController<PortalUser, Model, Culture>("System") {
```
改為（C# primary constructor 直接接收注入的服務，類別內以參數名稱 `importService` 存取）：
```csharp
    public class HomeController(PHStatistics.Portal.Services.Import.PopulationImportService importService) : MvcController<PortalUser, Model, Culture>("System") {
```

- [ ] **Step 2: 刪除已搬移到 `Portal/Services/Import/` 的私有成員**

刪除 `HomeController.cs:3269-3807` 範圍內的下列成員（已在 Task 2/3/4/5/6/7 搬移到對應新檔案，內容完全一致，此處只刪除不修改）：
- `_gradeOrder`、`_psjCourseIds`、`_asCourseIds`、`_defaultSubmitterId`、`_phExcludeSheets`、`_phColCourseId`、`_psHeaderCourseId`、`_em1CourseIds`（第 3269-3345 行）
- `PsjColumnType`、`AsColumnType`、`ReadCellNumber`（第 3347-3372 行）
- `GetOrCreatePopulation`、`AddClassAndItem`、`ParseYearWeekFromTitle`（第 3374-3452 行）
- `RunImportSheetPH`（第 3454-3559 行）
- `RunImportPH`、`RunImportGEPT`、`RunImportPS`、`RunImportPSJ`、`RunImportAS`（第 3561-3796 行）
- `AsChineseNumerals`（第 3798-3807 行）

**保留不動**：`DiagnoseImport`（第 3809-3837 行，獨立的診斷端點，不屬於匯入邏輯本體）、`FillLastWeekNumbers`（第 3881-3963 行，保留作為全庫補救工具，其邏輯已被 `PopulationImportService.CorrectLastWeekNumbers` 以更小範圍複用，但既有端點不刪除以免影響現有維運習慣）、`FixLastWeekData`（第 3965-4044 行，同樣保留）、`ImportAllResult` 類別定義（第 4046-4053 行，因為 `FillLastWeekNumbers`/`FixLastWeekData` 內部沒有使用它，但 `LoadHeaderMapFromCsv` 等後續程式碼在同一 region 內，保留類別定義避免影響；若編譯後發現此類別已無任何參照，可以之後再單獨清理，非本任務範圍）。

- [ ] **Step 3: 修改 `ImportAll` action**

原本第 3839-3879 行：
```csharp
        [HttpGet("ImportAll")]
        public IActionResult ImportAll(string rootPath = @"C:\Leo\其他\Kuri\人數表匯入A") {
            if (!Directory.Exists(rootPath))
                return Json(new { success = false, message = $"路徑不存在: {rootPath}" });

            var results = new List<ImportAllResult>();
            using var db = new DataContext();

            var weekDirs = Directory.GetDirectories(rootPath)
                .OrderBy(d => int.TryParse(Path.GetFileName(d), out var n) ? n : int.MaxValue);

            foreach (var weekDir in weekDirs) {
                var weekName = Path.GetFileName(weekDir);
                foreach (var filePath in Directory.GetFiles(weekDir, "*.xlsx").OrderBy(f => f)) {
                    var fn = Path.GetFileName(filePath);
                    var fileResults = new List<ImportAllResult>();
                    try {
                        if (fn.Contains("全國人數表")) {
                            fileResults.Add(RunImportPH(db, filePath));
                            fileResults.Add(RunImportGEPT(db, filePath));
                        } else if (fn.Contains("PS南區"))
                            fileResults.Add(RunImportPS(db, filePath));
                        else if (fn.Contains("百倍速"))
                            fileResults.Add(RunImportPSJ(db, filePath));
                        else if (fn.Contains("百瀚全區課輔"))
                            fileResults.Add(RunImportAS(db, filePath));
                    }
                    catch (Exception ex) {
                        fileResults.Add(new ImportAllResult { File = fn, Type = "Unknown", Errors = { ex.Message } });
                        Logger?.LogError("ImportAll 例外 {file}: {msg}", fn, ex.Message);
                    }
                    foreach (var r in fileResults) {
                        r.Week = weekName;
                        results.Add(r);
                        Logger?.LogInformation("ImportAll {week}/{file} [{type}] → {schools}校 {items}筆 錯誤:{errs}",
                            weekName, fn, r.Type, r.SchoolCount, r.ItemCount, r.Errors.Count);
                    }
                }
            }
            return Json(new { success = true, results = results });
        }
```

改為：
```csharp
        [HttpGet("ImportAll")]
        public IActionResult ImportAll(string rootPath = @"C:\Leo\其他\Kuri\人數表匯入A") {
            if (!Directory.Exists(rootPath))
                return Json(new { success = false, message = $"路徑不存在: {rootPath}" });

            var results = new List<PHStatistics.Portal.Services.Import.ImportResult>();
            using var db = new DataContext();

            var weekDirs = Directory.GetDirectories(rootPath)
                .OrderBy(d => int.TryParse(Path.GetFileName(d), out var n) ? n : int.MaxValue);

            foreach (var weekDir in weekDirs) {
                var weekName = Path.GetFileName(weekDir);
                foreach (var filePath in Directory.GetFiles(weekDir, "*.xlsx").OrderBy(f => f)) {
                    var fn = Path.GetFileName(filePath);
                    var fileResults = new List<PHStatistics.Portal.Services.Import.ImportResult>();
                    try {
                        if (fn.Contains("全國人數表")) {
                            using (var fs1 = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                                fileResults.Add(importService.Import(db, StudentPopulationType.PH, fs1, fn));
                            using (var fs2 = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                                fileResults.Add(importService.Import(db, StudentPopulationType.GEPT, fs2, fn));
                        } else if (fn.Contains("PS南區")) {
                            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                            fileResults.Add(importService.Import(db, StudentPopulationType.PS, fs, fn));
                        } else if (fn.Contains("百倍速")) {
                            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                            fileResults.Add(importService.Import(db, StudentPopulationType.PSJ, fs, fn));
                        } else if (fn.Contains("百瀚全區課輔")) {
                            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                            fileResults.Add(importService.Import(db, StudentPopulationType.AfterSchool, fs, fn));
                        }
                    }
                    catch (Exception ex) {
                        fileResults.Add(new PHStatistics.Portal.Services.Import.ImportResult { File = fn, Type = "Unknown", Errors = { ex.Message } });
                        Logger?.LogError("ImportAll 例外 {file}: {msg}", fn, ex.Message);
                    }
                    foreach (var r in fileResults) {
                        r.Week = weekName;
                        results.Add(r);
                        Logger?.LogInformation("ImportAll {week}/{file} [{type}] → {schools}校 {items}筆 錯誤:{errs}",
                            weekName, fn, r.Type, r.SchoolCount, r.ItemCount, r.Errors.Count);
                    }
                }
            }
            return Json(new { success = true, results = results });
        }
```

注意：PH+GEPT 各自開一個新的 `FileStream` 讀同一個 `filePath`——因為 `XSSFWorkbook(stream)` 建構時會把整個 stream 讀完，同一個 stream 不能重複讀取兩次，所以本機路徑批次匯入這邊維持用 `filePath` 各自開新的 `FileStream`（跟原本各自呼叫 `RunImportPH(db, filePath)`/`RunImportGEPT(db, filePath)` 各自用 `using var fs = new FileStream(filePath, ...)` 的行為一致，沒有改變語意）。

- [ ] **Step 4: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded，且不應該再出現「找不到 RunImportPH/RunImportGEPT/RunImportPS/RunImportPSJ/RunImportAS/GetOrCreatePopulation/AddClassAndItem/ParseYearWeekFromTitle」等編譯錯誤（代表 Step 2 刪除的成員在 `ImportAll` 之外沒有其他呼叫者殘留）

- [ ] **Step 5: 手動驗證**

啟動 `dotnet run --project source/portal/Portal/Portal.csproj`（短時間，確認無啟動例外），並用 `curl` 或瀏覽器打 `GET /ImportAll?rootPath=<一個不存在的路徑>`，確認回傳 `{"success":false,"message":"路徑不存在: ..."}`（驗證 DI 注入的 `PopulationImportService` 沒有造成建構失敗）。若手邊有現成的測試資料夾，可額外用真實 `rootPath` 跑一次，比對回傳的 `results` 結構與搬移前一致。

- [ ] **Step 6: Commit**

```bash
git add source/portal/Portal/Controllers/HomeController.cs
git commit -m "refactor: delegate HomeController.ImportAll to PopulationImportService"
```

---

### Task 10: 後台匯入 Controller（`StudentPopulationImportController`）

**Files:**
- Create: `source/portal/Portal/Areas/Admin/Controllers/StudentPopulationImportController.cs`

**Interfaces:**
- Consumes: `PopulationImportService.Scan`/`Import`（Task 8）、`SystemPermission.StudentPopulationImport`（Task 1）
- Produces: `POST /Admin/StudentPopulationImport/Import`（`IFormFile file, StudentPopulationType type, bool confirmed = false` 參數，回傳 JSON），供 Task 11 前端呼叫；`GET /Admin/StudentPopulationImport/Index`（回傳匯入頁面）

- [ ] **Step 1: 建立 Controller**

```csharp
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PHStatistics.Content;
using PHStatistics.Portal.Services.Import;

namespace PHStatistics.Portal.Areas.Admin.Controllers {
    [RequirePermission(SystemPermission.StudentPopulationImport)]
    public class StudentPopulationImportController : AdminBaseController {
        private readonly PopulationImportService _importService;

        public StudentPopulationImportController(PopulationImportService importService) {
            _importService = importService;
        }

        public IActionResult Index() {
            ViewBag.Title = "人數表匯入";
            return View();
        }

        [HttpPost]
        public IActionResult Import(IFormFile file, StudentPopulationType type, bool confirmed = false) {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "請選擇檔案" });

            using var db = new DataContext();

            ImportScanResult scan;
            using (var scanStream = file.OpenReadStream()) {
                scan = _importService.Scan(db, type, scanStream);
            }

            if (scan.Errors.Count > 0)
                return Json(new { success = false, message = string.Join("; ", scan.Errors) });

            var conflicts = scan.Items.Where(i => i.Exists).ToList();
            if (conflicts.Count > 0 && !confirmed) {
                return Json(new {
                    success = true,
                    requiresConfirmation = true,
                    conflicts = conflicts.Select(c => new { c.SchoolName, c.Year, c.Week }),
                });
            }

            ImportResult result;
            using (var importStream = file.OpenReadStream()) {
                result = _importService.Import(db, type, importStream, file.FileName);
            }

            return Json(new {
                success = true,
                requiresConfirmation = false,
                schoolCount = result.SchoolCount,
                itemCount = result.ItemCount,
                errors = result.Errors,
            });
        }
    }
}
```

`IFormFile.OpenReadStream()` 可以呼叫多次（ASP.NET Core 預設會把上傳檔案緩衝到記憶體或暫存檔，不是一次性 stream），所以這裡先呼叫一次做 `Scan`、確認/無衝突後再呼叫一次做 `Import`，兩次都會拿到從頭開始的完整串流，不需要手動 seek。

- [ ] **Step 2: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: 因為 `Index()` 回傳 `View()` 但對應的 `Areas/Admin/Views/StudentPopulationImport/Index.cshtml` 還沒建立（Task 11），**執行期**才會找不到 View，但**編譯期**不會報錯（Razor View 是執行期尋找，不是編譯期強制檢查）。確認 `dotnet build` 回報 0 錯誤即可。

- [ ] **Step 3: Commit**

```bash
git add source/portal/Portal/Areas/Admin/Controllers/StudentPopulationImportController.cs
git commit -m "feat: add StudentPopulationImportController with scan-confirm-import flow"
```

---

### Task 11: 後台匯入頁面 + 側邊選單連結

**Files:**
- Create: `source/portal/Portal/Areas/Admin/Views/StudentPopulationImport/Index.cshtml`
- Modify: `source/portal/Portal/Areas/Admin/Views/Shared/_AdminSidebar.cshtml:46-51`

**Interfaces:**
- Consumes: `POST /Admin/StudentPopulationImport/Import`（Task 10，JSON 回應含 `success`/`requiresConfirmation`/`conflicts`/`schoolCount`/`itemCount`/`errors`/`message` 欄位）

- [ ] **Step 1: 建立匯入頁面**

```html
@{
    ViewBag.Title = "人數表匯入";
}
<div class="container py-4">
    <h3>人數表匯入</h3>
    <p class="text-muted">選擇報表類型並上傳對應的 Excel 檔案。若該分校/週次已有資料，系統會先詢問確認才覆寫重建。</p>

    <div class="mb-3">
        <label class="form-label">報表類型</label>
        <select class="form-select" id="importType" style="max-width:300px">
            <option value="PH">百瀚（PH）</option>
            <option value="GEPT">英檢（GEPT）</option>
            <option value="PS">百世（PS）</option>
            <option value="PSJ">百倍速（PSJ）</option>
            <option value="AfterSchool">課輔（AS）</option>
        </select>
    </div>
    <div class="mb-3">
        <label class="form-label">Excel 檔案</label>
        <input type="file" class="form-control" id="importFile" accept=".xlsx" style="max-width:400px">
    </div>
    <button type="button" class="btn btn-primary" id="importBtn">上傳並匯入</button>

    <div id="importResult" class="mt-3"></div>
</div>

<script>
    function doImport(confirmed) {
        var fileInput = document.getElementById('importFile');
        if (!fileInput.files || fileInput.files.length === 0) {
            alert('請選擇檔案');
            return;
        }
        var formData = new FormData();
        formData.append('file', fileInput.files[0]);
        formData.append('type', $('#importType').val());
        formData.append('confirmed', confirmed ? 'true' : 'false');

        $('#importResult').html('<div class="text-muted">匯入中...</div>');

        $.ajax({
            url: '@Url.Action("Import")',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                if (!res.success) {
                    $('#importResult').html('<div class="alert alert-danger">' + res.message + '</div>');
                    return;
                }
                if (res.requiresConfirmation) {
                    var lines = res.conflicts.map(function (c) {
                        return c.schoolName + ' ' + c.year + '年第' + c.week + '週';
                    }).join('、');
                    if (confirm('以下資料已存在，確定要覆寫重建嗎？\n' + lines)) {
                        doImport(true);
                    } else {
                        $('#importResult').html('<div class="text-muted">已取消匯入</div>');
                    }
                    return;
                }
                var errHtml = (res.errors && res.errors.length > 0)
                    ? '<div class="alert alert-warning mt-2">錯誤：' + res.errors.join('<br>') + '</div>'
                    : '';
                $('#importResult').html(
                    '<div class="alert alert-success">匯入完成：' + res.schoolCount + ' 校，' + res.itemCount + ' 筆項目</div>' + errHtml
                );
            },
            error: function () {
                $('#importResult').html('<div class="alert alert-danger">匯入失敗，請稍後再試</div>');
            }
        });
    }

    $('#importBtn').on('click', function () { doImport(false); });
</script>
```

- [ ] **Step 2: 新增側邊選單連結**

`_AdminSidebar.cshtml` 第 46-51 行（「人數表管理」項目）後面新增：
```html
        <li class="sidebar-item">
            <a href="@Url.Action("Index", "StudentPopulation", new { area = "Admin" })" class="sidebar-link @(ViewContext.RouteData.Values["controller"]?.ToString() == "StudentPopulation" ? "active" : "")">
                <i class="dx-icon-chart"></i>
                <span>人數表管理</span>
            </a>
        </li>

        <li class="sidebar-item">
            <a href="@Url.Action("Index", "StudentPopulationImport", new { area = "Admin" })" class="sidebar-link @(ViewContext.RouteData.Values["controller"]?.ToString() == "StudentPopulationImport" ? "active" : "")">
                <i class="dx-icon-upload"></i>
                <span>人數表匯入</span>
            </a>
        </li>
```

- [ ] **Step 3: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded

- [ ] **Step 4: 手動驗證**

啟動網站，用具備 `StudentPopulationImport` 權限的帳號登入後台，確認：
1. 側邊選單出現「人數表匯入」，點擊進入 `/Admin/StudentPopulationImport`，頁面正常顯示（類型下拉＋檔案選擇＋上傳按鈕）
2. 選一個目前資料庫已有資料的分校/週次對應的 Excel 檔上傳，確認跳出覆寫確認對話框，取消後顯示「已取消匯入」
3. 確認後執行，畫面顯示「匯入完成：X 校，Y 筆項目」
4. 换一個不具備此權限的帳號，確認打 `/Admin/StudentPopulationImport` 會被導回 `/Admin/Dashboard`（`AdminBaseController.OnActionExecuting` 既有行為）
5. 匯入完成後，用 SQL 或既有匯出功能檢查該筆 `StudentPopulation` 的 `Items` 中，非新生班級的 `LastWeekNumber` 是否已正確回填為上週對應課程總人數（而非 0）

- [ ] **Step 5: Commit**

```bash
git add "source/portal/Portal/Areas/Admin/Views/StudentPopulationImport/Index.cshtml" source/portal/Portal/Areas/Admin/Views/Shared/_AdminSidebar.cshtml
git commit -m "feat: add admin import page and sidebar link"
```

---

## Self-Review Notes

- **Spec coverage**：spec「需求二」的權限設計（Task 1）、插件式匯入架構＋課程對照表單一來源（Task 2-8）、掃描確認流程（Task 10）、校正上週資料（Task 8，改用驗證過的 `(CourseId, ClassType)` 演算法而非原 spec 誤寫的 `ClassId`）、獨立 Controller（Task 10，修正原 spec「放在既有 Controller」的錯誤設計）、重複匯入自動覆寫（Task 10）皆已對應到具體任務。`HomeController.ImportAll` 相容性（Task 9）、後台入口與導覽（Task 11）也都涵蓋。Out-of-scope 項目（跨週連鎖修正、課程對照表三處合併、`ImportAll`/`FixLastWeekData` 補權限保護）未安排任務，符合 spec。
- **Placeholder scan**：所有 Step 都含完整程式碼與確切檔案/行號，搬移類任務都標明原始行號範圍與唯一允許的改動（型別改名、私有轉公開、`Logger` 改為顯式參數），無 TBD。
- **Type consistency**：`IPopulationImporter.Scan(DataContext, Stream)`/`Import(DataContext, Stream, ILogger)` 簽章在 Task 4 定義後，Task 5/6/7 的三個實作與 Task 8 `PopulationImportService` 的呼叫端完全一致；`ImportResult.PopulationIds`（`HashSet<long>`）在 Task 2 定義後，Task 3/4/5/6/7 每個 Importer 都正確地在呼叫 `GetOrCreatePopulation` 後 `Add` 對應的 `pop.Id`，Task 8 的 `CorrectLastWeekNumbers` 也以 `long populationId` 一致接收。
