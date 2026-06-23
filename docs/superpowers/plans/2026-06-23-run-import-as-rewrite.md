# RunImportAS 改寫實作計畫

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 將 `RunImportAS` 改寫為支援新多頁籤格式（每頁籤 = 一分校）。

**Architecture:** 只修改 `HomeController.cs` 中的 `RunImportAS` 方法，從讀取單一 Sheet 改為遍歷所有 Sheet。分校名從頁籤名取得，年份從 Row 0 標題正則解析，週次從第一筆資料列 col 0 取得，欄位改為固定三欄（AS/EP/EG）。

**Tech Stack:** ASP.NET Core 8.0、NPOI（XSSFWorkbook）、Entity Framework Core 8.0、NLog

## Global Constraints

- 只修改 `HomeController.cs` 中的 `RunImportAS` 方法，其他方法不動
- 方法 signature 維持不變：`private ImportAllResult RunImportAS(DataContext db, string filePath)`
- `_asCourseIds`、`AsColumnType`、`_gradeOrder`、`GetOrCreatePopulation`、`AddClassAndItem`、`ReadCellNumber` 均不異動
- 樣式與鄰近程式碼一致（縮排 4 空白、`var`、`FirstOrDefault` 等慣用語）

---

### Task 1: 改寫 RunImportAS 方法

**Files:**
- Modify: `source/portal/Portal/Controllers/HomeController.cs:3705-3765`

**Interfaces:**
- Consumes: `_asCourseIds["AS"|"EP"|"EG"]`、`_gradeOrder`、`GetOrCreatePopulation()`、`AddClassAndItem()`、`ReadCellNumber()`（全部已存在，不異動）
- Produces: 方法回傳 `ImportAllResult`，同舊版（`ImportAll` 呼叫端不變）

- [ ] **Step 1: 確認目前程式碼範圍**

  開啟 `source/portal/Portal/Controllers/HomeController.cs`，確認 `RunImportAS` 方法在 **第 3705–3765 行**，舊內容如下（此為要被取代的程式碼）：

  ```csharp
  private ImportAllResult RunImportAS(DataContext db, string filePath) {
      var result = new ImportAllResult { File = Path.GetFileName(filePath), Type = "AS" };
      using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
      var sheet = new XSSFWorkbook(fs).GetSheetAt(0);
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
              pop = GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                  StudentPopulationType.AfterSchool, $"{yearInt}第{weekInt}週課輔人數表", true);
              result.SchoolCount++;
          }

          int gradeIdx = Array.IndexOf(_gradeOrder, grade);
          if (gradeIdx < 0) continue;

          for (int cNo = 4; cNo < headerRow.LastCellNum; cNo++) {
              try {
                  string code = headerRow.GetCell(cNo)?.ToString()?.Trim() ?? "";
                  if (code.Equals("X", StringComparison.OrdinalIgnoreCase)) continue;

                  int courseId;
                  ClassType cType;
                  if (code == "T") {
                      courseId = 258; cType = ClassType.General;
                  }
                  else if (_asCourseIds.TryGetValue(code, out int[] ids)) {
                      courseId = ids[gradeIdx]; cType = AsColumnType(code);
                  }
                  else { continue; }

                  Course course = db.Course.Include("Department").FirstOrDefault(e => e.Id == courseId);
                  int count = ReadCellNumber(row, cNo);
                  if (course == null || count <= 0) continue;

                  AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result);
              }
              catch { continue; }
          }
      }
      return result;
  }
  ```

- [ ] **Step 2: 取代為新實作**

  將上述整個方法（第 3705–3765 行）取代為以下新版本：

  ```csharp
  private ImportAllResult RunImportAS(DataContext db, string filePath) {
      var result = new ImportAllResult { File = Path.GetFileName(filePath), Type = "AS" };
      using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
      var workbook = new XSSFWorkbook(fs);

      for (int sheetIdx = 0; sheetIdx < workbook.NumberOfSheets; sheetIdx++) {
          var sheet = workbook.GetSheetAt(sheetIdx);
          string schoolName = Regex.Replace(sheet.SheetName, @"^\d+", "").Trim();
          School school = db.School.FirstOrDefault(e => e.Name == schoolName);
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
              if (int.TryParse(wr.GetCell(0)?.ToString()?.Trim(), out int w) && w > 0) {
                  weekInt = w;
                  break;
              }
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

          StudentPopulation pop = GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
              StudentPopulationType.AfterSchool, $"{yearInt}第{weekInt}週課輔人數表", true);
          result.SchoolCount++;

          var colDefs = new[] {
              (col: 3, code: "AS", cType: ClassType.General),
              (col: 4, code: "EP", cType: ClassType.Personal),
              (col: 5, code: "EG", cType: ClassType.General),
          };

          for (int rNo = 4; rNo <= sheet.LastRowNum; rNo++) {
              IRow row = sheet.GetRow(rNo);
              if (row == null) continue;

              string grade = row.GetCell(2)?.ToString()?.Trim() ?? "";
              int gradeIdx = Array.IndexOf(_gradeOrder, grade);
              if (gradeIdx < 0) continue;

              foreach (var (col, code, cType) in colDefs) {
                  try {
                      int courseId = _asCourseIds[code][gradeIdx];
                      Course course = db.Course.Include("Department").FirstOrDefault(e => e.Id == courseId);
                      int count = ReadCellNumber(row, col);
                      if (course == null || count <= 0) continue;
                      AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result);
                  }
                  catch { continue; }
              }
          }
      }
      return result;
  }
  ```

- [ ] **Step 3: Build 驗證**

  ```powershell
  cd source
  dotnet build portal/PHStatistics.portal.sln
  ```

  預期：`Build succeeded.` 無錯誤。

- [ ] **Step 4: 手動驗證匯入**

  啟動應用程式後，瀏覽器開啟：
  ```
  http://localhost:5000/DiagnoseImport?filePath=C:\Leo\其他\Kuri\人數表匯入A\50\百瀚全區課輔人數總表(20260613).xlsx&startRow=0&endRow=6
  ```
  確認回傳 JSON 中 `rows[0]` 標題含「學年度」字串，且各頁籤可被識別。

  接著執行完整匯入（可用 ImportAll endpoint 或後台 UI）：
  ```
  http://localhost:5000/ImportAll?rootPath=C:\Leo\其他\Kuri\人數表匯入A\50
  ```
  預期結果中 AS 項目：
  - `SchoolCount` > 0（至少 1 間分校）
  - `Errors` 為空或僅含「找不到分校」之非關鍵錯誤
  - `ItemCount` > 0

- [ ] **Step 5: Commit**

  ```bash
  git add source/portal/Portal/Controllers/HomeController.cs
  git commit -m "fix: 改寫 RunImportAS 支援新多頁籤格式（每頁籤一分校）"
  ```
