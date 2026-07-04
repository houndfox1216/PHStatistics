# 週次切換輸入 與 後台 Excel 匯入 設計文件

**日期**：2026-07-04
**狀態**：設計確認，待撰寫實作計畫
**範圍**：兩項獨立但可並行實作的功能，各自對應獨立權限

---

## 背景

目前系統有兩個實務痛點：

1. 前台人數表輸入的「週次」完全由伺服器自動判定（落在 `SchoolYear.WeekStartDate`~`ImportEndDate` 區間內的那一筆），沒有人可以回頭補登或修正其他週次的資料，只能等技術人員用資料庫或維運端點處理。
2. 後台匯入資料完全仰賴技術人員手動執行 `HomeController.ImportAll`（無權限保護、吃寫死本機路徑的 GET 端點），一般後台使用者無法自行匯入。匯入完成的資料 `LastWeekNumber` 全部是 0（匯入程式不像前台輸入流程會去抓上週資料回填），需要靠人工另外執行 `FixLastWeekData`。

另外，考量到未來需要陸續匯入約 5 年份的歷史人數表資料，過程中會不斷出現新的舊格式、需要新增/調整匯入程式，匯入相關程式碼這次一併整理成可長期擴充的模組。

---

## 需求一：管理員切換週數輸入/編輯

### 目標

具備特定權限的使用者，可以在前台任選「本學年度」中的某一週進行輸入：
- 若該週該分校該類型已有資料 → 進入編輯模式
- 若尚未有資料 → 依現行規則新建，並繼承上一週資料

### 權限設計

新增獨立權限 `SystemPermission.PopulationWeekSwitch`（顯示名稱：「人數表週次切換」），定義於 `source/schema/Data/SystemPermission.cs`（enum 尾端新增一個值，`short` 底層型別、以 `PermissionValue` 位元運算儲存，新增不需要 schema migration）。

此權限獨立於 `Administrator` 與任何後台存取權限之外，可單獨授予不需要後台權限、但需要協助校正數據的人員。

### 分校範圍

不改變現行規則：使用者仍只能操作自己被指派（`SchoolAssignment`）的分校，透過既有 `Model.GetMemberSchool(User.Id)`。

### 適用範圍

PH、PSJ、GEPT、PS、AS 共 5 種人數表類型皆適用。

### UI 設計

在 `Views/StudentPopulation/Index.cshtml`（選分校頁面）：
- 若 `ViewBag.CanSwitchWeek == true`，在「選分校」下拉選單旁新增「週次」下拉選單
- 週次清單來源：目前學年度（取 `SchoolYear` 表中最大 `Year`）的全部 `SchoolYear` 記錄，依 `Week` 排序，不受 `ImportEndDate` 視窗限制
- 使用者選擇分校＋週次後，導向對應 `CreateXXXPopulation` action 時，額外帶上 `schoolYearId` 查詢參數；不選則沿用現行行為（不帶參數，自動判定當週）

### 後端設計

`Controllers/StudentPopulationController.cs` 的 `Index` action 新增：
```csharp
ViewBag.CanSwitchWeek = User.HasPermission(SystemPermission.PopulationWeekSwitch);
if (ViewBag.CanSwitchWeek) {
    int currentMaxYear = dataContext.SchoolYear.Max(e => e.Year);
    ViewBag.Weeks = dataContext.SchoolYear.Where(e => e.Year == currentMaxYear).OrderBy(e => e.Week).ToList();
}
```

5 個 `CreateXXXPopulation` action 目前各自重複下列邏輯（`CreatePopulation` 第 181-183 行等，共 5 處）：
```csharp
DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).OrderBy(e => e.Id).FirstOrDefault();
```

抽出共用私有方法，5 處呼叫點統一改用：
```csharp
private SchoolYear ResolveSchoolYear(DataContext dataContext, int? schoolYearId) {
    if (schoolYearId.HasValue && User.HasPermission(SystemPermission.PopulationWeekSwitch)) {
        SchoolYear overrideYear = dataContext.SchoolYear.Find(schoolYearId.Value);
        if (overrideYear != null) return overrideYear;
    }
    DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
    return dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).OrderBy(e => e.Id).FirstOrDefault();
}
```

各 action 簽章新增 `int? schoolYearId = null` 參數。「上一週」判定、既有資料載入編輯、不存在則新建並繼承上週資料等既有邏輯**完全不動**——因為這些邏輯本來就是以「解析出的 `schoolYear`」為準，不管這個 `schoolYear` 是自動判定還是手動指定。

### 不需要修改的部分

- 單一項目層級 AJAX 端點（`UpdateClassItem`、`UpdateSumClassItem`、`UpdateRemark`、`AddNewClass`、`RemoveClassItem`、`UpdateClassDetail`）已經綁定在具體 `StudentPopulationItem`/`StudentPopulation` Id 上操作，與週次判定無關，不需修改。
- 編輯已核准（`Status = Approved`）資料時不做狀態限制，編輯後也不改變原狀態。
- POST 儲存流程（`CreatePopulation(StudentPopulation data, ...)` 的 POST 分支）已經是用 `data.Id` 操作，不需要重新解析週次。

### Out of scope

- 不處理「跨學年度」切換（下拉選單只列當前學年度）
- 不修改 `MainMenu.cshtml` 的 `CanEdit` 全域行為（無自動判定週次時，一般使用者仍看不到輸入按鈕；本功能只影響有 `PopulationWeekSwitch` 權限的使用者在 Index 頁面多一個週次選項）

---

## 需求二：後台 Excel 匯入

### 目標

後台使用者可自行上傳 Excel 檔匯入人數表資料，不再需要技術人員用 `ImportAll` 手動匯入；匯入完成後自動校正本次匯入資料的「上週人數」（`LastWeekNumber`）與相關合計欄位。

### 權限設計

新增獨立權限 `SystemPermission.StudentPopulationImport`（顯示名稱：「人數表匯入」），與既有 `SystemPermission.StudentPopulation`（人數表管理）分開，可單獨授權。

### 匯入方式

後台使用者先選擇「報表類型」（PH / PSJ / GEPT / PS / AS），再上傳單一 Excel 檔（該檔案本身可能包含多分校資料，如 PH/PS/PSJ/GEPT 為單一 Sheet 多分校列，AS 為多 Sheet 每頁籤一分校，皆沿用現行檔案格式與判斷邏輯，不改變 Excel 本身格式）。

### 架構：抽出可長期擴充的匯入模組

因為匯入程式會長期持續擴充（陸續補入約 5 年歷史資料、不斷出現新的舊格式），採用**插件式架構**，新建 `Portal/Services/Import/` 目錄：

```
Portal/Services/Import/
  IPopulationImporter.cs        // 介面：Scan() + Import()
  PopulationImportService.cs    // 對外唯一入口，依類型（未來含格式版本）選擇 Importer
  PHPopulationImporter.cs
  GeptPopulationImporter.cs
  PSPopulationImporter.cs
  PSJPopulationImporter.cs
  ASPopulationImporter.cs
  ImportSupport/
    CourseMapping.cs            // 匯入專用課程對照表（單一來源）
    PopulationWriteHelper.cs    // GetOrCreatePopulation / AddClassAndItem 等共用寫入邏輯
    TitleParser.cs              // ParseYearWeekFromTitle
```

- 現有 `HomeController.cs` 中的 `RunImportPH`/`RunImportGEPT`/`RunImportPS`/`RunImportPSJ`/`RunImportAS`/`RunImportSheetPH`、`GetOrCreatePopulation`、`AddClassAndItem`、`ParseYearWeekFromTitle`，以及 `_phColCourseId`/`_psHeaderCourseId`/`_psjCourseIds`/`_asCourseIds`/`_em1CourseIds` 對照表，搬移到對應 Importer 類別與 `ImportSupport`，並改吃 `Stream` 而非檔案路徑
- `HomeController.ImportAll`（保留給技術人員的舊路徑，維持現行檔名關鍵字判斷＋資料夾掃描行為）與新的後台匯入功能，皆改為呼叫 `PopulationImportService`，不再各自維護一份解析邏輯
- 新增格式（如未來的舊版 PH 格式）時，只需新增一個實作 `IPopulationImporter` 的類別並在 `PopulationImportService` 註冊，不需修改既有已上線的 Importer
- **不在本次範圍**：前台 `StudentPopulationController.cs` 與 `ReportExportService.cs` 中另外各自維護的課程對照表副本，暫不合併（用途不同、非本次匯入功能直接觸及的程式碼，避免範圍擴大）

### 匯入流程：掃描確認 → 執行

單一後端 action（`Areas/Admin/Controllers/StudentPopulationController.cs` 新增 `Import(IFormFile file, StudentPopulationType type, bool confirmed = false)`）：

1. 呼叫對應 `IPopulationImporter.Scan(stream)`：僅解析標題列與分校欄位，取得本檔案涉及的 `(School, Year, Week)` 清單，**不寫入資料庫**
2. 逐一比對資料庫是否已有對應資料：
   - 若有衝突（任一週次已存在資料）且 `confirmed == false` → 回傳 JSON `{ requiresConfirmation: true, conflicts: [...] }`，前端彈出確認訊息（例如：「XX分校 115年第20週 已有資料，確定要覆寫嗎？」）
   - 使用者確認後，前端用同一份已選檔案重新送出，帶上 `confirmed = true`
3. `confirmed == true`（或本來就無衝突）→ 呼叫 `IPopulationImporter.Import(stream)` 正式寫入，沿用現行 `GetOrCreatePopulation(deleteExisting: true)` 冪等重建方式（先刪除該校該週該類型舊資料再重建）
4. 匯入完成後，對本次匯入涉及的每一筆 `StudentPopulation`，執行「校正上週資料」（見下節）
5. 回傳匯入結果摘要（成功筆數、影響分校/週次清單）給前端顯示

### 校正上週資料（匯入後自動執行）

範圍：**僅限本次匯入涉及的週次/分校**，不做全庫掃描（不使用現行 `FixLastWeekData` 的全庫版本），也不向下一週連鎖修正（若下週資料已存在，可能已被人工調整過，連鎖回填有覆蓋風險；此邊界情況維持用現有 `FixLastWeekData` 手動處理）。

步驟：
1. 對匯入產生/更新的每個 `StudentPopulation`，逐一檢查其 `Items`
2. 對 `LastWeekNumber == 0` 的項目，比照現行 `FixLastWeekData` 的邏輯，查詢上一週（`Year`/`Week - 1`，跨學年度時取上一學年度最後一週）同分校同 `Class` 的 `Number`，回填至 `LastWeekNumber`
3. 回填完成後，對該 `StudentPopulation` 重新執行合計計算（`SumPHPopulation` 或 `StatisticsCalculationService.CalculateAll`，依現行實際呼叫路徑為準，實作階段確認），確保「與上週相比」「本週新生」「本週流失」等依賴 `LastWeekNumber` 的 `IsSum` 欄位正確

此步驟實作為 `PopulationImportService` 的共用後置流程，所有 Importer 匯入完成後自動套用，不需要每個格式各自實作一次。

### 重複匯入行為

若匯入的分校/週次資料已存在，確認後**自動覆寫重建**（沿用 `deleteExisting=true`），不要求使用者手動先刪除。

---

## 資料異動摘要

- `source/schema/Data/SystemPermission.cs`：新增 `PopulationWeekSwitch`、`StudentPopulationImport` 兩個列舉值（尾端新增，不需要 migration）
- 無資料庫 schema 異動

---

## Out of scope（本次不處理）

- 匯入資料向下一週的連鎖修正（邊界情況用現有 `FixLastWeekData` 手動處理）
- 前台 `StudentPopulationController.cs`、`ReportExportService.cs` 課程對照表與匯入模組的整併
- `HomeController.ImportAll`/`FixLastWeekData` 補上權限保護（現況已知問題，但不在本次需求範圍，若要處理需另外確認）
- 舊格式歷史資料（5 年份）匯入程式本身的撰寫——本次只搭好可擴充的架構，未來各年份/格式的 Importer 實作另案處理

---

## 測試考量

- 需求一：涵蓋「切換到已有資料的週次編輯」「切換到空白週次繼承上週」「無權限使用者看不到週次選單」「已核准資料仍可編輯且狀態不變」
- 需求二：涵蓋「掃描到衝突需二次確認」「無衝突直接匯入」「匯入後 LastWeekNumber 正確回填」「合計欄位正確重算」「無 `StudentPopulationImport` 權限者看不到匯入功能」
