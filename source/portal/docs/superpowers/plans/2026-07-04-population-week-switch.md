# 人數表週次切換輸入 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓具備 `PopulationWeekSwitch` 權限的使用者，在前台任選本學年度中的任一週進行人數表輸入或編輯（已有資料→編輯，無資料→依現行規則新建並繼承上週）。

**Architecture:** 新增一個獨立的 `SystemPermission` 列舉值，並在 `Index` 選分校頁面新增週次下拉選單。5 個既有的 `CreateXXXPopulation` action（PH/PSJ/GEPT/PS/AS）目前各自重複一段「取得維護年度週次」的自動判定邏輯，抽出共用私有方法 `ResolveSchoolYear`，當帶有 `schoolYearId` 參數且使用者具備新權限時改用指定週次，否則維持現行自動判定行為。既有的「編輯或新建＋繼承上週」邏輯完全不動。

**Tech Stack:** ASP.NET Core 8.0 MVC、Entity Framework Core 8、Razor Views、jQuery（既有前端）。

## Global Constraints

- 對應 spec：`source/portal/docs/superpowers/specs/2026-07-04-week-switch-and-excel-import-design.md` 的「需求一」章節
- 新權限 enum 值只能加在 `SystemPermission.cs` 尾端（`short` 底層型別、位元運算儲存，插在中間會打亂既有授權資料）
- 分校範圍不變：僅限使用者被指派（`SchoolAssignment`）的分校
- 編輯既有資料時不限制 `Status`，編輯後不改變原 `Status`
- 週次下拉選單清單來源為「`SchoolYear` 表中最大 `Year`」的全部週次，不受 `ImportEndDate` 視窗限制
- **此專案沒有 Controller 單元測試基礎設施**（`StudentPopulationController` 直接 `new DataContext()` 連接真實 SQL Server，非依賴注入，無法在不連 DB 的情況下做隔離單元測試；`Test` 專案目前只有一個連到本機執行中網站的 Selenium E2E 測試 `Test/Page.cs`）。本計畫的驗證方式一律採用：**編譯成功 + 啟動本機服務手動操作驗證**，不生產虛假的單元測試

---

### Task 1: 新增 `PopulationWeekSwitch` 權限

**Files:**
- Modify: `source/schema/Data/SystemPermission.cs:145-151`

**Interfaces:**
- Produces: `SystemPermission.PopulationWeekSwitch`（供 Task 2、Task 3 的 `User.HasPermission(SystemPermission.PopulationWeekSwitch)` 呼叫使用）

- [ ] **Step 1: 在 `SystemPermission.cs` 尾端新增列舉值**

目前檔案尾端（第 139-151 行）：
```csharp
    /// <summary>
    /// 查看所有分校資料（管理處人員）
    /// </summary>
    [Display(Name = "查看所有分校")]
    ViewAllSchools,

    /// <summary>
    /// 學年度管理
    /// </summary>
    [Display(Name = "學年度管理")]
    SchoolYear,

}
```

改為：
```csharp
    /// <summary>
    /// 查看所有分校資料（管理處人員）
    /// </summary>
    [Display(Name = "查看所有分校")]
    ViewAllSchools,

    /// <summary>
    /// 學年度管理
    /// </summary>
    [Display(Name = "學年度管理")]
    SchoolYear,

    /// <summary>
    /// 人數表週次切換（可任選週次輸入/編輯，不受當週自動判定限制）
    /// </summary>
    [Display(Name = "人數表週次切換")]
    PopulationWeekSwitch,

}
```

- [ ] **Step 2: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded，無錯誤

- [ ] **Step 3: 到角色管理頁面確認新權限可被指派**

啟動網站（`dotnet run --project source/portal/Portal/Portal.csproj`），登入具備 `Role` 管理權限的帳號，進入 `/Admin/Role`（角色管理），編輯任一角色，確認清單中出現「人數表週次切換」選項，勾選後儲存成功。

- [ ] **Step 4: Commit**

```bash
git add source/schema/Data/SystemPermission.cs
git commit -m "feat: add PopulationWeekSwitch permission for manual week selection"
```

---

### Task 2: 抽出 `ResolveSchoolYear` 共用方法並套用到 5 個 CreateXXXPopulation action

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`
  - 新增私有方法（插入於第 73 行 `AsColType` 方法後、第 75 行 `Index` action 前）
  - `CreatePopulation`（第 160、181-183 行）
  - `CreatePSJPopulation`（第 324、340-341 行）
  - `CreateGeptPopulation`（第 487、503-504 行）
  - `CreatePSPopulation`（第 595、611-612 行）
  - `CreateASPopulation`（第 704、723-724 行）

**Interfaces:**
- Consumes: `SystemPermission.PopulationWeekSwitch`（Task 1 產出）
- Produces: `private SchoolYear ResolveSchoolYear(DataContext dataContext, int? schoolYearId)`，供 Task 3 之外不需使用（僅本檔案內部 5 處呼叫點使用）；5 個 action 簽章新增 `int? schoolYearId = null` 參數，供 Task 4 的前端導頁 URL 帶入

- [ ] **Step 1: 新增 `ResolveSchoolYear` 私有方法**

在 `source/portal/Portal/Controllers/StudentPopulationController.cs` 第 73 行（`AsColType` 方法結尾 `};`）之後插入：

```csharp

        private SchoolYear ResolveSchoolYear(DataContext dataContext, int? schoolYearId) {
            if (schoolYearId.HasValue && User.HasPermission(SystemPermission.PopulationWeekSwitch)) {
                SchoolYear overrideYear = dataContext.SchoolYear.Find(schoolYearId.Value);
                if (overrideYear != null) {
                    return overrideYear;
                }
            }
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            return dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).OrderBy(e => e.Id).FirstOrDefault();
        }
```

- [ ] **Step 2: 修改 `CreatePopulation`（第 160、181-183 行）**

簽章（第 160 行）由：
```csharp
        public IActionResult CreatePopulation(StudentPopulation data, int schoolId, string type) {
```
改為：
```csharp
        public IActionResult CreatePopulation(StudentPopulation data, int schoolId, string type, int? schoolYearId = null) {
```

第 181-183 行由：
```csharp
                DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
                School school = dataContext.School.Find(schoolId);
                SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).OrderBy(e => e.Id).FirstOrDefault();
```
改為：
```csharp
                School school = dataContext.School.Find(schoolId);
                SchoolYear schoolYear = ResolveSchoolYear(dataContext, schoolYearId);
```

- [ ] **Step 3: 修改 `CreatePSJPopulation`（第 324、340-341 行）**

簽章由：
```csharp
        public IActionResult CreatePSJPopulation(StudentPopulation data, int schoolId, string type) {
```
改為：
```csharp
        public IActionResult CreatePSJPopulation(StudentPopulation data, int schoolId, string type, int? schoolYearId = null) {
```

第 340-341 行由：
```csharp
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).OrderBy(e => e.Id).FirstOrDefault();
```
改為：
```csharp
            SchoolYear schoolYear = ResolveSchoolYear(dataContext, schoolYearId);
```

- [ ] **Step 4: 修改 `CreateGeptPopulation`（第 487、503-504 行）**

簽章由：
```csharp
        public IActionResult CreateGeptPopulation(StudentPopulation data, int schoolId, string type) {
```
改為：
```csharp
        public IActionResult CreateGeptPopulation(StudentPopulation data, int schoolId, string type, int? schoolYearId = null) {
```

第 503-504 行由：
```csharp
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).OrderBy(e => e.Id).FirstOrDefault();
```
改為：
```csharp
            SchoolYear schoolYear = ResolveSchoolYear(dataContext, schoolYearId);
```

- [ ] **Step 5: 修改 `CreatePSPopulation`（第 595、611-612 行）**

簽章由：
```csharp
        public IActionResult CreatePSPopulation(StudentPopulation data, int schoolId, string type) {
```
改為：
```csharp
        public IActionResult CreatePSPopulation(StudentPopulation data, int schoolId, string type, int? schoolYearId = null) {
```

第 611-612 行由：
```csharp
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).OrderBy(e => e.Id).FirstOrDefault();
```
改為：
```csharp
            SchoolYear schoolYear = ResolveSchoolYear(dataContext, schoolYearId);
```

- [ ] **Step 6: 修改 `CreateASPopulation`（第 704、723-724 行）**

簽章由：
```csharp
        public IActionResult CreateASPopulation(StudentPopulation data, int schoolId, string type) {
```
改為：
```csharp
        public IActionResult CreateASPopulation(StudentPopulation data, int schoolId, string type, int? schoolYearId = null) {
```

第 723-724 行由：
```csharp
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).OrderBy(e => e.Id).FirstOrDefault();
```
改為：
```csharp
            SchoolYear schoolYear = ResolveSchoolYear(dataContext, schoolYearId);
```

- [ ] **Step 7: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded，無錯誤（若有殘留未使用的 `dateTime` 區域變數警告屬正常，非錯誤）

- [ ] **Step 8: 手動驗證 — 未帶 `schoolYearId` 時行為不變**

啟動網站，用一般帳號（無 `PopulationWeekSwitch` 權限）登入，走「首頁→輸入資料-百瀚→選分校→開始輸入」，確認行為與修改前一致（自動判定當週，能正常輸入/編輯）。

- [ ] **Step 9: 手動驗證 — 帶 `schoolYearId` 且具備權限時可切換週次**

用具備 `PopulationWeekSwitch` 權限的帳號登入，直接在網址列輸入 `/StudentPopulation/CreatePopulation?schoolId=<你的分校ID>&type=PH&schoolYearId=<某個歷史週次的SchoolYear.Id>`，確認：
- 頁面標題顯示的年/週為指定週次，而非當週
- 若該週已有資料，顯示既有資料可編輯
- 若該週無資料，顯示新建畫面且已繼承上一週人數

- [ ] **Step 10: 手動驗證 — 無權限時 `schoolYearId` 參數被忽略**

用不具備 `PopulationWeekSwitch` 權限的帳號，同樣在網址列帶入 `schoolYearId` 參數，確認頁面仍顯示「當週」資料，`schoolYearId` 未被採用（因為 `ResolveSchoolYear` 內的權限檢查會擋下）。

- [ ] **Step 11: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "refactor: extract ResolveSchoolYear helper to support permission-gated week override"
```

---

### Task 3: `Index` action 提供週次清單給前端

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs:75-107`

**Interfaces:**
- Consumes: `SystemPermission.PopulationWeekSwitch`（Task 1）
- Produces: `ViewBag.CanSwitchWeek`（`bool`）、`ViewBag.Weeks`（`List<SchoolYear>`，僅當 `CanSwitchWeek == true` 時有值）— 供 Task 4 的 `Index.cshtml` 使用

- [ ] **Step 1: 修改 `Index` action**

第 96-106 行由：
```csharp
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).OrderBy(e => e.Id).FirstOrDefault();
            List<SchoolAssignment> schools = Model.GetMemberSchool(User.Id);
            ViewBag.Schools = schools;
            ViewBag.CanEdit = schoolYear != null;
            ViewBag.Type = type;
            if (schools == null || schools.Count <= 0) {
                Redirect("StudentPopulation/CreatePopulation");
            }
            return View();
        }
```
改為：
```csharp
            //取得維護年度週次
            DateTime dateTime = DateTime.UtcNow.ToTaipeiTime();
            SchoolYear schoolYear = dataContext.SchoolYear.Where(e => e.WeekStartDate <= dateTime && e.ImportEndDate >= dateTime).OrderBy(e => e.Id).FirstOrDefault();
            List<SchoolAssignment> schools = Model.GetMemberSchool(User.Id);
            ViewBag.Schools = schools;
            ViewBag.CanEdit = schoolYear != null;
            ViewBag.Type = type;
            ViewBag.CanSwitchWeek = User.HasPermission(SystemPermission.PopulationWeekSwitch);
            if ((bool)ViewBag.CanSwitchWeek) {
                int currentMaxYear = dataContext.SchoolYear.Max(e => e.Year);
                ViewBag.Weeks = dataContext.SchoolYear.Where(e => e.Year == currentMaxYear).OrderBy(e => e.Week).ToList();
            }
            if (schools == null || schools.Count <= 0) {
                Redirect("StudentPopulation/CreatePopulation");
            }
            return View();
        }
```

- [ ] **Step 2: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: expose CanSwitchWeek and Weeks to Index view"
```

---

### Task 4: `Index.cshtml` 新增週次下拉選單

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/Index.cshtml`

**Interfaces:**
- Consumes: `ViewBag.CanSwitchWeek`（`bool`）、`ViewBag.Weeks`（`List<SchoolYear>`）— Task 3 產出
- Produces: 導頁 URL 於使用者選擇週次時帶上 `&schoolYearId=<SchoolYear.Id>` 查詢參數，供 Task 2 的 5 個 `CreateXXXPopulation` action 使用

- [ ] **Step 1: 在分校下拉選單區塊後新增週次下拉選單**

第 13-25 行由：
```html
            <div class="mt-3 mb-4">
                @if (schools != null) {
                    <label for="">
                        分校 <small class="text-primary">＊必填</small>
                    </label>
                    <select class="form-select validate[required]" id="schoolSelect">
                        <option selected disabled>選擇分校</option>
                        @foreach (SchoolAssignment assignment in schools) {
                            <option value=@assignment.School.Id>@assignment.School.Name</option>
                        }
                    </select>
                }
            </div>
```
改為：
```html
            <div class="mt-3 mb-4">
                @if (schools != null) {
                    <label for="">
                        分校 <small class="text-primary">＊必填</small>
                    </label>
                    <select class="form-select validate[required]" id="schoolSelect">
                        <option selected disabled>選擇分校</option>
                        @foreach (SchoolAssignment assignment in schools) {
                            <option value=@assignment.School.Id>@assignment.School.Name</option>
                        }
                    </select>
                }
            </div>
            <div class="mt-3 mb-4">
                @if (ViewBag.CanSwitchWeek != null && (bool)ViewBag.CanSwitchWeek) {
                    List<SchoolYear> weeks = (List<SchoolYear>)ViewBag.Weeks;
                    <label for="">
                        週次 <small class="text-muted">（不選則預設當週）</small>
                    </label>
                    <select class="form-select" id="weekSelect">
                        <option value="" selected>預設當週</option>
                        @foreach (SchoolYear w in weeks) {
                            <option value="@w.Id">@w.Year 年第 @w.Week 週</option>
                        }
                    </select>
                }
            </div>
```

- [ ] **Step 2: 修改 JS，導頁時帶上 `schoolYearId`**

第 62-91 行由：
```html
<script>
    $(function () {
        $('#addPHBtn').on('click', function () {
            //alert($('#schoolSelect').val());
            var location = "/StudentPopulation/CreatePopulation?schoolId=" + $('#schoolSelect').val() + "&type=PH";
            window.location.href = location;
        });
        $('#addPSJBtn').on('click', function () {
            //alert($('#schoolSelect').val());
            var location = "/StudentPopulation/CreatePSJPopulation?schoolId=" + $('#schoolSelect').val() + "&type=PSJ";
            window.location.href = location;
        });
        $('#addGeptBtn').on('click', function () {
            //alert($('#schoolSelect').val());
            var location = "/StudentPopulation/CreateGeptPopulation?schoolId=" + $('#schoolSelect').val() + "&type=Gept";
            window.location.href = location;
        });
        $('#addPSBtn').on('click', function () {
            //alert($('#schoolSelect').val());
            var location = "/StudentPopulation/CreatePSPopulation?schoolId=" + $('#schoolSelect').val() + "&type=PS";
            window.location.href = location;
        });
        $('#addASBtn').on('click', function () {
            //alert($('#schoolSelect').val());
            var location = "/StudentPopulation/CreateASPopulation?schoolId=" + $('#schoolSelect').val() + "&type=AS";
            window.location.href = location;
        });
    })

</script>
```
改為：
```html
<script>
    function weekParam() {
        var sel = $('#weekSelect');
        if (sel.length && sel.val()) {
            return '&schoolYearId=' + sel.val();
        }
        return '';
    }
    $(function () {
        $('#addPHBtn').on('click', function () {
            var location = "/StudentPopulation/CreatePopulation?schoolId=" + $('#schoolSelect').val() + "&type=PH" + weekParam();
            window.location.href = location;
        });
        $('#addPSJBtn').on('click', function () {
            var location = "/StudentPopulation/CreatePSJPopulation?schoolId=" + $('#schoolSelect').val() + "&type=PSJ" + weekParam();
            window.location.href = location;
        });
        $('#addGeptBtn').on('click', function () {
            var location = "/StudentPopulation/CreateGeptPopulation?schoolId=" + $('#schoolSelect').val() + "&type=Gept" + weekParam();
            window.location.href = location;
        });
        $('#addPSBtn').on('click', function () {
            var location = "/StudentPopulation/CreatePSPopulation?schoolId=" + $('#schoolSelect').val() + "&type=PS" + weekParam();
            window.location.href = location;
        });
        $('#addASBtn').on('click', function () {
            var location = "/StudentPopulation/CreateASPopulation?schoolId=" + $('#schoolSelect').val() + "&type=AS" + weekParam();
            window.location.href = location;
        });
    })

</script>
```

- [ ] **Step 3: 編譯確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded

- [ ] **Step 4: 手動驗證 — 完整流程**

啟動網站，用具備 `PopulationWeekSwitch` 權限的帳號登入：
1. 首頁 → 「輸入資料-百瀚」→ 進入 `/StudentPopulation/Index?type=PH`
2. 確認「週次」下拉選單出現，且列出目前學年度所有週次（依週次排序）
3. 選擇分校＋選擇一個「非當週」的歷史週次 → 點「開始輸入」
4. 確認網址列出現 `schoolYearId` 參數，且頁面顯示的是所選週次的資料（既有資料可編輯，或無資料時新建並繼承上一週）
5. 換一般帳號（無此權限）重複步驟 1-2，確認「週次」下拉選單不出現

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Views/StudentPopulation/Index.cshtml
git commit -m "feat: add week selector dropdown for PopulationWeekSwitch permission holders"
```

---

## Self-Review Notes

- **Spec coverage**：spec「需求一」的權限設計（Task 1）、`ResolveSchoolYear` 重構＋5 個 action 套用（Task 2）、`Index` action 週次清單來源（Task 3）、UI 下拉選單與導頁參數（Task 4）皆已對應到具體任務。「不修改單一項目層級 AJAX 端點」「不修改 POST 儲存流程」「不做跨學年度切換」等 out-of-scope 項目未安排任務，符合 spec。
- **Placeholder scan**：所有 Step 均含完整程式碼與確切檔案/行號，無 TBD。
- **Type consistency**：`ResolveSchoolYear(DataContext dataContext, int? schoolYearId)` 簽章與回傳型別 `SchoolYear` 在 Task 2 定義後，Task 2 的 5 處呼叫點與 Task 4 的 URL 參數命名（`schoolYearId`）全部一致。
