# RunImportAS 改寫設計

**日期：** 2026-06-23  
**範圍：** `HomeController.cs` → `RunImportAS` 方法  
**目標：** 支援百瀚全區課輔新格式（多頁籤，每頁籤 = 一分校）

---

## 背景

舊格式為單一頁籤，所有分校資料集中一張 Sheet，分校名在 col 2、年份在 col 0、週次在 col 1。新格式改為每個分校獨立一個頁籤，頁籤名含分校名（帶前導學年數字，如「114東湖」），年份從 Row 0 標題取得，週次從資料列 col 0 取得。

---

## 格式差異

| 欄位 | 舊格式 | 新格式 |
|------|--------|--------|
| 頁籤 | 單一 GetSheetAt(0) | 多個，每頁 = 一分校 |
| 分校名 | col 2 (string) | 頁籤名，剝除前導數字（`^\d+`） |
| 年份 | col 0 (int) | Row 0 標題，正則 `(\d+)學年度` |
| 週次 | col 1 (int) | 第一筆資料列 col 0 (int) |
| 年級 | col 3 | col 2 |
| 欄位對應 | 動態（讀 Row 4 header code） | 固定三欄（見下表） |
| T 欄（合計） | courseId=258 | 新格式無此欄，移除 |

### 新格式固定欄位（Row 4+）

| col | 內容 | code | CourseIds | ClassType |
|-----|------|------|-----------|-----------|
| 0 | 週次 (int) | — | — | — |
| 1 | 日期 (date) | — | 略過 | — |
| 2 | 年級 | — | 查 `_gradeOrder` 取 gradeIdx | — |
| 3 | 安親課輔班 | AS | `_asCourseIds["AS"][gradeIdx]` | General |
| 4 | 英文班一對一 | EP | `_asCourseIds["EP"][gradeIdx]` | Personal |
| 5 | 英文班團體班 | EG | `_asCourseIds["EG"][gradeIdx]` | General |
| 6–9 | 數學班/理化班 | — | 無 DB 課程，略過 | — |

---

## 改寫邏輯

```
RunImportAS(db, filePath):
    result = new ImportAllResult { Type = "AS" }
    workbook = new XSSFWorkbook(fileStream)

    foreach sheetIndex in 0..workbook.NumberOfSheets-1:
        sheet = workbook.GetSheetAt(sheetIndex)
        schoolName = Regex.Replace(sheet.SheetName, @"^\d+", "").Trim()
        school = db.School.FirstOrDefault(e => e.Name == schoolName)
        if school == null → result.Errors.Add(...); continue

        title = sheet.GetRow(0)?.GetCell(0)?.ToString() ?? ""
        yearMatch = Regex.Match(title, @"(\d+)學年度")
        if !yearMatch.Success → result.Errors.Add(...); continue
        yearInt = int.Parse(yearMatch.Groups[1].Value)

        // 週次從第一筆有效資料列取得
        weekInt = 0
        for r = 4..sheet.LastRowNum:
            if int.TryParse(row.GetCell(0), out int w) && w > 0:
                weekInt = w; break
        if weekInt == 0 → result.Errors.Add(...); continue

        schoolYear = db.SchoolYear.FirstOrDefault(e => e.Year == yearInt && e.Week == weekInt)
        if schoolYear == null → result.Errors.Add(...); continue

        pop = GetOrCreatePopulation(db, school.Id, yearInt, weekInt, schoolYear,
                  StudentPopulationType.AfterSchool, $"{yearInt}第{weekInt}週課輔人數表", deleteExisting=true)
        result.SchoolCount++

        foreach row in rows 4..LastRowNum:
            grade = row.GetCell(2)?.ToString()?.Trim()
            gradeIdx = Array.IndexOf(_gradeOrder, grade)
            if gradeIdx < 0 → continue

            // col 3: AS · General
            AddIfPositive(col=3, "AS", ClassType.General)
            // col 4: EP · Personal
            AddIfPositive(col=4, "EP", ClassType.Personal)
            // col 5: EG · General
            AddIfPositive(col=5, "EG", ClassType.General)
```

`AddIfPositive`：`count = ReadCellNumber(row, col)`；count > 0 時呼叫 `AddClassAndItem(db, school.Id, course, cType, pop.Id, count, result)`

---

## 不異動項目

- `_asCourseIds` 字典（AS/EP/EG 沿用現有 ID，N/L/W 保留不刪）
- `AsColumnType()`、`_gradeOrder`、`GetOrCreatePopulation()`、`AddClassAndItem()`、`ReadCellNumber()`
- 方法 signature：`private ImportAllResult RunImportAS(DataContext db, string filePath)`
- `ImportAll` 呼叫端不需修改

---

## 錯誤處理

| 條件 | 行為 |
|------|------|
| 頁籤名剝除數字後找不到 School | `result.Errors.Add(...)`, 跳過此頁籤 |
| Row 0 標題無 `學年度` | `result.Errors.Add(...)`, 跳過此頁籤 |
| 找不到有效週次（col 0 非數字）| `result.Errors.Add(...)`, 跳過此頁籤 |
| SchoolYear 不存在 | `result.Errors.Add(...)`, 跳過此頁籤 |
| 年級不在 `_gradeOrder` | 跳過此列（同舊邏輯）|
| count <= 0 | 跳過（同舊邏輯）|
