# 管理員輸入介面調整 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓具備 `SystemPermission.PopulationWeekSwitch` 權限的使用者可以在已送出的人數表週次修正資料（含新增「上週人數」可編輯輸入框），並把每一次編輯寫入審計 log。

**Architecture:** 全部改動集中在 `StudentPopulationController.cs`（後端權限判斷/資料存取）、`PopulationPartialView.cshtml`/`ASPopulationPartialView.cshtml`（畫面顯示）、5 個 `CreateXXXPopulation.cshtml`（JS）、以及 `StudentPopulationItemLog` 這個既有但欄位有 bug 的死碼 entity（需要一支 EF Core migration 修正）。不引入新的架構層，沿用專案既有的「controller 直接 `new DataContext()`」與「AJAX action 各自重設 ViewBag」慣例。

**Tech Stack:** ASP.NET Core 8.0 MVC、Entity Framework Core 8.0（SQL Server）、Razor `.cshtml`、jQuery AJAX。

## Global Constraints

- 這次調整只放寬給 `SystemPermission.PopulationWeekSwitch` 權限的使用者；一般使用者行為必須完全不變（已送出=整表鎖死、上週人數=唯讀文字）。
- `ConfirmPopulation`（送出動作本身）維持鎖定，不在這次放寬範圍。
- 本專案 controller 沒有單元測試基礎設施（直接 `new DataContext()` 接真實 SQL Server，非 DI），每個任務的驗證方式是 `dotnet build` 0 錯誤 + 針對該任務改動內容的 sqlcmd/程式碼檢查；真正的端對端瀏覽器操作驗證延後到全部任務完成後，比照本專案既有慣例（目前環境沒有瀏覽器工具）。
- ⚠️ **任何會寫入資料庫的指令（`dotnet ef database update`、`sqlcmd`）之前，必須先重新讀取 `source/portal/Portal/appsettings.json` 目前生效（未被 `//` 註解掉）的 `ConnectionStrings:DataContext` 值，確認其 `Server`/`Database` 不是已知的正式環境字串**（正式環境特徵：`Server=20.188.19.77,52056` 或 `Server=52.237.119.13,52056` 且 `Database=NewPAS`，不含年份/週次後綴）。不要沿用本計畫文件裡任何時間點記錄過的字串值，因為這個檔案的內容會被人手動切換。
- `sqlcmd -i` 在這台機器上已知會靜默跳過語句且無錯誤訊息（見專案記憶 `reference_sqlcmd_flakiness`）；所有查驗證用的 SQL 一律用 `sqlcmd -Q "<單一陳述式>"` 逐句執行，並且事後再用一次獨立的 `SELECT` 驗證結果，不能只看主控台輸出。

---

### Task 1: 修正 `StudentPopulationItemLog` schema 並建立 migration

**Files:**
- Modify: `source/schema/Data/Content/StudentPopulationItemLog.cs`
- Create: `source/schema/Data/Migrations/<timestamp>_AddStudentPopulationItemLogAuditFields.cs`（由 `dotnet ef migrations add` 產生，含對應 `.Designer.cs` 並更新 `DataContextModelSnapshot.cs`）

**Interfaces:**
- Produces：`StudentPopulationItemLog` 新增/修正後的欄位，供 Task 5 寫入使用：
  - `public int? LastWeekNumber { get; set; }`（原本是 `[NotMapped] bool`，改成有對應資料庫欄位、代表「異動前的上週人數」）
  - `public int? ChangeLastWeekNumber { get; set; }`（新增，代表「異動後的上週人數」）
  - `public bool IsNew { get; set; }`（原本是 `[NotMapped] bool`，改成有對應資料庫欄位、預設 `false`，代表這筆 log 是「新增班級」事件）
  - `public bool IsDeleted { get; set; }`（新增，預設 `false`，代表這筆 log 是「刪除班級」事件）
  - `public Guid? MemberId { get; set; }` + `public Member Member { get; set; }`（新增，記錄操作者，`Member` 型別來自 `PHStatistics.Community`）

- [ ] **Step 1: 修改 entity 檔案**

打開 `source/schema/Data/Content/StudentPopulationItemLog.cs`，把這段：

```csharp
        /// <summary>
        /// 新增項目
        /// </summary>
        [Display(Name = "新增項目"), DataMember, NotMapped]
        public bool IsNew { get; set; }

        /// <summary>
        /// 新增項目
        /// </summary>
        [Display(Name = "新增項目"), DataMember, NotMapped]
        public bool LastWeekNumber { get; set; }
    }
}
```

改成：

```csharp
        /// <summary>
        /// 新增項目
        /// </summary>
        [Display(Name = "新增項目"), DataMember]
        public bool IsNew { get; set; }

        /// <summary>
        /// 刪除項目
        /// </summary>
        [Display(Name = "刪除項目"), DataMember]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 上週人數
        /// </summary>
        [Display(Name = "上週人數"), DataMember]
        public int? LastWeekNumber { get; set; }

        /// <summary>
        /// 變更後上週人數
        /// </summary>
        [Display(Name = "變更後上週人數"), DataMember]
        public int? ChangeLastWeekNumber { get; set; }

        /// <summary>
        /// 操作人員識別碼
        /// </summary>
        [Display(Name = "操作人員識別碼"), DataMember]
        public Guid? MemberId { get; set; }

        /// <summary>
        /// 操作人員
        /// </summary>
        [Display(Name = "操作人員"), DataMember]
        public Member Member { get; set; }
    }
}
```

再把檔案最上方的 using 區塊：

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Framework.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
```

改成（加入 `PHStatistics.Community`，因為 `Member` 類別在該命名空間，跟 `StudentPopulation.cs` 已有的 `Submitter` 欄位引用方式一致）：

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Framework.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using PHStatistics.Community;
```

- [ ] **Step 2: Build 確認編譯成功**

Run（從 repo 內任一含 `source` 的目錄）：
```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`（既有的 warning 數量不變或持平即可，這步只是確認 entity 改動沒有語法/型別錯誤）。

- [ ] **Step 3: 產生 EF Core migration**

先確認 `dotnet-ef` 工具存在：
```bash
dotnet tool list -g
```
Expected: 清單裡有 `dotnet-ef`（若沒有，執行 `dotnet tool install --global dotnet-ef`）。

在 `source/schema/Data` 目錄下執行：
```bash
cd source/schema/Data && dotnet ef migrations add AddStudentPopulationItemLogAuditFields --project Data.csproj --startup-project Data.csproj
```
Expected: 主控台印出 `Done.`，且 `Migrations/` 資料夾下新增兩個檔案 `<timestamp>_AddStudentPopulationItemLogAuditFields.cs` 與對應的 `.Designer.cs`，`DataContextModelSnapshot.cs` 也被更新。

- [ ] **Step 4: 檢查產生的 migration 內容**

打開新產生的 `<timestamp>_AddStudentPopulationItemLogAuditFields.cs`，確認 `Up()` 方法裡包含以下這些欄位的新增（欄位名稱、`nullable`/`defaultValue` 要跟下面一致；型別對應與精確措辭可能因 EF Core 版本略有差異，但語意必須一致）：
- `AddColumn<int>(name: "LastWeekNumber", table: "StudentPopulationItemLog", nullable: true)`
- `AddColumn<int>(name: "ChangeLastWeekNumber", table: "StudentPopulationItemLog", nullable: true)`
- `AddColumn<bool>(name: "IsNew", table: "StudentPopulationItemLog", nullable: false, defaultValue: false)`
- `AddColumn<bool>(name: "IsDeleted", table: "StudentPopulationItemLog", nullable: false, defaultValue: false)`
- `AddColumn<Guid>(name: "MemberId", table: "StudentPopulationItemLog", nullable: true)`
- 一個 `CreateIndex(name: "IX_StudentPopulationItemLog_MemberId", ...)` 和一個 `AddForeignKey(...)` 指向 `Member` 表格的 `Id`

如果缺少任一項，代表 Step 1 的 entity 改動漏掉了；回去檢查 Step 1，刪除這次產生的 migration 檔案（`dotnet ef migrations remove --project Data.csproj --startup-project Data.csproj`），修正後重跑 Step 3。

- [ ] **Step 5: 確認目標資料庫是 dev 環境，然後套用 migration**

執行（不要用計畫文件裡任何舊字串，重新讀取當下內容）：
```bash
grep -A2 "ConnectionStrings" source/portal/Portal/appsettings.json
```
確認輸出裡「沒有被 `//` 註解掉」的那一行，`Server` 不是 `20.188.19.77,52056` 也不是 `52.237.119.13,52056`，且如果 `Database` 剛好叫 `NewPAS`（沒有任何年份/週次後綴）也要視為正式環境、立刻停下來跟使用者確認，不要繼續套用。

確認是 dev 環境後，把該行的完整連線字串複製下來（例如目前是 `Server=CLOUDFUN-MSI-LE\SQLEXPRESS;Database=NewPAS0716;User=sa;Pwd=cloudfun@12;Encrypt=false;MultipleActiveResultSets=true`，但**必須用你剛剛實際讀到的值**，不要照抄這個範例），執行：
```bash
cd source/schema/Data && dotnet ef database update --project Data.csproj --startup-project Data.csproj --connection "<剛剛確認過的 dev 連線字串>"
```
**必須明確帶 `--connection`**，因為 `source/schema/Data/appsettings.json` 裡自己的連線字串指向另一個完全不相關的資料庫（`PHStatistics`），如果不帶這個參數，migration 會套用到錯的資料庫，Portal 網站實際連線的 dev DB 不會拿到新欄位。

Expected: 主控台印出 `Applying migration '<timestamp>_AddStudentPopulationItemLogAuditFields'.` 與 `Done.`。

- [ ] **Step 6: 用 sqlcmd 獨立驗證欄位真的存在**

用 Step 5 同一組 dev 連線資訊（拆解出 Server/Database/User/Pwd），逐句執行（不要用 `-i`）：
```bash
sqlcmd -S <Server> -d <Database> -U <User> -P '<Pwd>' -Q "SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StudentPopulationItemLog' AND COLUMN_NAME IN ('LastWeekNumber','ChangeLastWeekNumber','IsNew','IsDeleted','MemberId') ORDER BY COLUMN_NAME"
```
Expected: 回傳 5 列，`LastWeekNumber`/`ChangeLastWeekNumber` 是 `int` 且 `IS_NULLABLE=YES`，`IsNew`/`IsDeleted` 是 `bit` 且 `IS_NULLABLE=NO`，`MemberId` 是 `uniqueidentifier` 且 `IS_NULLABLE=YES`。

- [ ] **Step 7: Commit**

```bash
git add source/schema/Data/Content/StudentPopulationItemLog.cs source/schema/Data/Migrations/
git commit -m "feat: add audit fields to StudentPopulationItemLog (LastWeekNumber, IsNew, IsDeleted, Member)

Previous LastWeekNumber/IsNew fields were declared [NotMapped], making
them dead weight — no DB column ever existed for them. This fixes both
and adds ChangeLastWeekNumber/IsDeleted/MemberId so the entity can
actually record who changed what."
```

---

### Task 2: 對 `PopulationWeekSwitch` 使用者放寬「已送出」鎖定（AddNewClass / RemoveClassItem / UpdateClassDetail / UpdateClassDetail2）

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes：`SystemPermission.PopulationWeekSwitch`（既有列舉值，`User.HasPermission(SystemPermission.PopulationWeekSwitch)` 既有用法，見 `ResolveSchoolYear` 76-82 行）
- Produces：`ViewBag.CanEditLastWeek`（`bool`），由 `AddNewClass`、`RemoveClassItem` 設定，供 Task 4 的 `PopulationPartialView.cshtml`/`ASPopulationPartialView.cshtml` 讀取。`UpdateClassDetail`/`UpdateClassDetail2` 回傳 JSON、不牽涉畫面渲染，不需要設定這個 ViewBag。

- [ ] **Step 1: `AddNewClass` 放寬鎖定 + 設定 ViewBag**

找到（約 848-852 行）：
```csharp
            List<Course> courses = Model.DataContext.Course.Where(e => e.Type == studentPopulationData.Type).OrderBy(e => e.Ordinal).ToList();
            ViewBag.Courses = courses;
            if (studentPopulationData.Status != StudentPopulationStatus.Documented) {
                var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == studentPopulationData.Id).FirstOrDefault();
                return PartialView("PopulationPartialView", lockedData);
            }
```
改成：
```csharp
            List<Course> courses = Model.DataContext.Course.Where(e => e.Type == studentPopulationData.Type).OrderBy(e => e.Ordinal).ToList();
            ViewBag.Courses = courses;
            bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
            ViewBag.CanEditLastWeek = canEditLocked;
            if (studentPopulationData.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == studentPopulationData.Id).FirstOrDefault();
                return PartialView("PopulationPartialView", lockedData);
            }
```

- [ ] **Step 2: `RemoveClassItem` 放寬鎖定 + 設定 ViewBag**

找到（約 1126-1131 行）：
```csharp
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
                ViewBag.Courses = courses;
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulationId).FirstOrDefault();
                    return PartialView("PopulationPartialView", lockedData);
                }
```
改成：
```csharp
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
                ViewBag.Courses = courses;
                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
                ViewBag.CanEditLastWeek = canEditLocked;
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulationId).FirstOrDefault();
                    return PartialView("PopulationPartialView", lockedData);
                }
```

- [ ] **Step 3: `UpdateClassDetail` 放寬鎖定（不需要 ViewBag，回傳的是 JSON）**

找到（約 1199-1206 行）：
```csharp
        public IActionResult UpdateClassDetail(long itemId, string name, int classType) {
            try {
                DataContext dataContext = new DataContext();
                var item = dataContext.StudentPopulationItem.Include("StudentPopulation").Include("Class").FirstOrDefault(e => e.Id == itemId);
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented)
                    return Json(new { success = false, message = "人數表狀態不允許修改" });
```
改成：
```csharp
        public IActionResult UpdateClassDetail(long itemId, string name, int classType) {
            try {
                DataContext dataContext = new DataContext();
                var item = dataContext.StudentPopulationItem.Include("StudentPopulation").Include("Class").FirstOrDefault(e => e.Id == itemId);
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked)
                    return Json(new { success = false, message = "人數表狀態不允許修改" });
```

- [ ] **Step 4: `UpdateClassDetail2` 放寬鎖定**

找到（約 1237-1246 行）：
```csharp
        public IActionResult UpdateClassDetail2(long populationId, int classId, string name, int classType) {
            DataContext dataContext = new DataContext();
            var population = dataContext.StudentPopulation
                .FirstOrDefault(p => p.Id == populationId);

            if (population == null)
                return Json(new { success = false, message = "找不到人數表" });

            if (population.Status != StudentPopulationStatus.Documented)
                return Json(new { success = false, message = "人數表狀態不允許修改" });
```
改成：
```csharp
        public IActionResult UpdateClassDetail2(long populationId, int classId, string name, int classType) {
            DataContext dataContext = new DataContext();
            var population = dataContext.StudentPopulation
                .FirstOrDefault(p => p.Id == populationId);

            if (population == null)
                return Json(new { success = false, message = "找不到人數表" });

            bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
            if (population.Status != StudentPopulationStatus.Documented && !canEditLocked)
                return Json(new { success = false, message = "人數表狀態不允許修改" });
```

- [ ] **Step 5: Build 確認編譯成功**

```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`。

- [ ] **Step 6: 用 sqlcmd 手動確認鎖定行為（不依賴瀏覽器）**

這步驟只驗證「資料庫層面 Status 判斷邏輯」，不驗證權限判斷本身（`User.HasPermission` 需要真實登入 session，留給最後的瀏覽器手動驗證）。找一筆現有 dev DB 裡 `Status != 0`（`Documented`）的 `StudentPopulation` 記錄確認資料存在（之後最終瀏覽器驗證時，會用具備 `PopulationWeekSwitch` 權限的帳號實測這筆）：
```bash
sqlcmd -S <Server> -d <Database> -U <User> -P '<Pwd>' -Q "SELECT TOP 1 Id, Status, Year, Week FROM StudentPopulation WHERE Status <> 0 ORDER BY Id DESC"
```
Expected: 回傳至少一筆，記下這個 `Id`/`Year`/`Week` 供最後階段瀏覽器驗證使用。

- [ ] **Step 7: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: unlock AddNewClass/RemoveClassItem/UpdateClassDetail(2) for PopulationWeekSwitch users on submitted weeks"
```

---

### Task 3: `UpdateClassItem` 加上「上週人數」編輯能力

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes：`SystemPermission.PopulationWeekSwitch`
- Produces：`UpdateClassItem(long sId, int? number, string studentRemark = null, int? lastWeekNumber = null)` 新簽章，供 Task 4 的 JS (`lastWeekValueChange`) 呼叫；`ViewBag.CanEditLastWeek` 供 Task 4 的 partial view 讀取。

- [ ] **Step 1: 修改 `UpdateClassItem`**

找到（約 1147-1179 行）整個方法：
```csharp
        public IActionResult UpdateClassItem(long sId, int? number, string studentRemark = null) {
            DataContext dataContext = new DataContext();
            try {
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("Class.Course.Department").Include("StudentPopulation").Where(e => e.Id == sId).FirstOrDefault();
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
                ViewBag.Courses = courses;
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                    return PartialView("PopulationPartialView", lockedData);
                }
                if (number.HasValue) {
                    item.Number = number.Value;
                }
                if (studentRemark != null) {
                    item.StudentRemark = studentRemark;
                }
                dataContext.StudentPopulationItem.Update(item);
                dataContext.SaveChanges();
                if (number.HasValue) {
                    SumPHPopulation(item.StudentPopulation.Id);
                }
                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                ViewBag.Warnings = CheckNewLostConsistency(returnData);
                return PartialView("PopulationPartialView", returnData);
            }
            catch (Exception ex) {
                ViewBag.Courses = new List<Course>();
                Logger.LogError(ex, "UpdateClassItem sId={sId}", sId);
                return PartialView("PopulationPartialView", new StudentPopulation());
            }
        }
```
改成：
```csharp
        public IActionResult UpdateClassItem(long sId, int? number, string studentRemark = null, int? lastWeekNumber = null) {
            DataContext dataContext = new DataContext();
            try {
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("Class.Course.Department").Include("StudentPopulation").Where(e => e.Id == sId).FirstOrDefault();
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
                ViewBag.Courses = courses;
                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
                ViewBag.CanEditLastWeek = canEditLocked;
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked) {
                    var lockedData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                    return PartialView("PopulationPartialView", lockedData);
                }
                bool lastWeekApplied = lastWeekNumber.HasValue && canEditLocked;
                if (number.HasValue) {
                    item.Number = number.Value;
                }
                if (studentRemark != null) {
                    item.StudentRemark = studentRemark;
                }
                if (lastWeekApplied) {
                    item.LastWeekNumber = lastWeekNumber.Value;
                }
                dataContext.StudentPopulationItem.Update(item);
                dataContext.SaveChanges();
                if (number.HasValue || lastWeekApplied) {
                    SumPHPopulation(item.StudentPopulation.Id);
                }
                var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == item.StudentPopulation.Id).FirstOrDefault();
                ViewBag.Warnings = CheckNewLostConsistency(returnData);
                return PartialView("PopulationPartialView", returnData);
            }
            catch (Exception ex) {
                ViewBag.Courses = new List<Course>();
                Logger.LogError(ex, "UpdateClassItem sId={sId}", sId);
                return PartialView("PopulationPartialView", new StudentPopulation());
            }
        }
```

**重點**：`lastWeekNumber` 只有在 `canEditLocked`（`PopulationWeekSwitch` 權限）為真時才會被套用（`lastWeekApplied` 的判斷式），這是伺服器端的二次防線——就算沒有權限的使用者直接對這個 endpoint 發送 `lastWeekNumber` 參數，也會被忽略，不能繞過前端隱藏輸入框來偷改上週人數。

- [ ] **Step 2: Build 確認編譯成功**

```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`。

- [ ] **Step 3: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: allow PopulationWeekSwitch users to edit LastWeekNumber via UpdateClassItem"
```

---

### Task 4: 畫面調整 — 上週人數輸入框 + 5 個 Create 頁面加 ViewBag + 新 JS 函式

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`（5 個 `CreateXXXPopulation` action）
- Modify: `source/portal/Portal/Views/StudentPopulation/PopulationPartialView.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/ASPopulationPartialView.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePSJPopulation.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/CreateGeptPopulation.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePSPopulation.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/CreateASPopulation.cshtml`

**Interfaces:**
- Consumes：Task 3 的 `UpdateClassItem(long sId, int? number, string studentRemark = null, int? lastWeekNumber = null)`
- Produces：無（畫面最末端，沒有後續任務依賴這裡的輸出）

- [ ] **Step 1: 5 個 `CreateXXXPopulation` action 加 `ViewBag.CanEditLastWeek`**

在下列 5 處，緊接在 `ViewBag.SelectedYear = schoolYear;` 這一行之後加入 `ViewBag.CanEditLastWeek = User.HasPermission(SystemPermission.PopulationWeekSwitch);`：

`CreatePopulation`（約 213 行，注意這行前面有額外一層縮排，屬於 `else` 區塊內）：
```csharp
                ViewBag.SelectedYear = schoolYear;
```
改成：
```csharp
                ViewBag.SelectedYear = schoolYear;
                ViewBag.CanEditLastWeek = User.HasPermission(SystemPermission.PopulationWeekSwitch);
```

`CreatePSJPopulation`（約 373 行）、`CreateGeptPopulation`（約 538 行）、`CreatePSPopulation`（約 645 行）、`CreateASPopulation`（約 756 行）這 4 處縮排一致（12 個空白）：
```csharp
            ViewBag.SelectedYear = schoolYear;
```
各自改成：
```csharp
            ViewBag.SelectedYear = schoolYear;
            ViewBag.CanEditLastWeek = User.HasPermission(SystemPermission.PopulationWeekSwitch);
```

（這行文字在檔案裡出現 6 次，第 6 次在 `Index` action 163 行——不要動那一處，那是既有的週次下拉選單邏輯，跟這次「上週人數輸入框」無關。用行號或前後文確認改對地方。）

- [ ] **Step 2: `PopulationPartialView.cshtml` 第一處 LastWeekNumber 顯示（特殊命名課程分支，約 224-276 行區塊內）**

找到（約 247-249 行）：
```csharp
                    <div class="col-1 mb-1">
                        @sItem.LastWeekNumber
                    </div>
```
（這是 `<div class="row mx-0 @rowStateClass">` 底下、`class-name-input` 那個 `<input>` 之後的第一個 `col-1 mb-1`，屬於「本週英語文總人數/上週英語文總人數/與上週相比/去年同期/比/本週總詢問(填單)人數」這個特殊命名課程分支。）改成：
```csharp
                    <div class="col-1 mb-1">
                        @if (ViewBag.CanEditLastWeek != null && (bool)ViewBag.CanEditLastWeek) {
                            <input type="number" class="form-control" value="@sItem.LastWeekNumber" data-sitem="@sItem.Id" id="class_@sItem.Id.ToString()_lastweek" onchange="lastWeekValueChange('class_@sItem.Id.ToString()_lastweek')">
                        } else {
                            @sItem.LastWeekNumber
                        }
                    </div>
```

- [ ] **Step 3: `PopulationPartialView.cshtml` 第二處 LastWeekNumber 顯示（一般課程分支，約 360-366 行區塊內）**

找到（約 364-366 行，縮排比 Step 2 那處少一層）：
```csharp
                <div class="col-1 mb-1">
                    @sItem.LastWeekNumber
                </div>
```
（這是一般課程分支裡、`class-name-input` 之後的 `col-1 mb-1`。跟 Step 2 那處文字幾乎相同，差別在縮排空白數——用行號區分，這處縮排比 Step 2 少 4 個空白。）改成：
```csharp
                <div class="col-1 mb-1">
                    @if (ViewBag.CanEditLastWeek != null && (bool)ViewBag.CanEditLastWeek) {
                        <input type="number" class="form-control" value="@sItem.LastWeekNumber" data-sitem="@sItem.Id" id="class_@sItem.Id.ToString()_lastweek" onchange="lastWeekValueChange('class_@sItem.Id.ToString()_lastweek')">
                    } else {
                        @sItem.LastWeekNumber
                    }
                </div>
```

- [ ] **Step 4: `ASPopulationPartialView.cshtml` LastWeekNumber 顯示（約 100-102 行）**

找到：
```csharp
                    <div class="col-2 mb-3">
                        @sItem.LastWeekNumber
                    </div>
```
改成：
```csharp
                    <div class="col-2 mb-3">
                        @if (ViewBag.CanEditLastWeek != null && (bool)ViewBag.CanEditLastWeek) {
                            <input type="number" class="form-control" value="@sItem.LastWeekNumber" data-sitem="@sItem.Id" id="class_@sItem.Id.ToString()_lastweek" onchange="lastWeekValueChange('class_@sItem.Id.ToString()_lastweek')">
                        } else {
                            @sItem.LastWeekNumber
                        }
                    </div>
```

- [ ] **Step 5: 5 個 `CreateXXXPopulation.cshtml` 各自加上 `lastWeekValueChange` JS 函式**

這 5 個檔案的 `valueChange(sId)` 函式內容彼此完全一致（只差檔案內行號），在每個檔案裡緊接在 `valueChange(sId)` 函式的結尾 `}` 之後插入這個新函式：

```javascript
    function lastWeekValueChange(sId){
        var updateCourses = document.getElementById(sId);
        $.ajax({
            url: '@Url.Action("UpdateClassItem", "StudentPopulation", new { area = "" })',
            cache: false,
            data: { 'sId': updateCourses.dataset.sitem, 'lastWeekNumber': updateCourses.value },
            type: 'POST',
            dataType: "html",
            success: function (data) {
                $("#contentItem").html(data);
                if (typeof updateSubmitSummary === 'function') updateSubmitSummary();
            },
            error: function () {

            }
        });
    }
```

分別插入以下 5 個檔案（用檔案裡 `function valueChange(sId){` 這段搜尋定位，緊接在該函式結尾插入）：
- `CreatePopulation.cshtml`（約 326-346 行是 `valueChange`）
- `CreatePSJPopulation.cshtml`（約 317-337 行）
- `CreateGeptPopulation.cshtml`（約 319-339 行）
- `CreatePSPopulation.cshtml`（約 320-340 行）
- `CreateASPopulation.cshtml`（約 320-340 行）

- [ ] **Step 6: Build 確認編譯成功**

```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`。

- [ ] **Step 7: 靜態檢查 — 確認每個回傳 partial 的 action 都設定了 ViewBag.CanEditLastWeek**

```bash
grep -n "ViewBag.CanEditLastWeek" source/portal/Portal/Controllers/StudentPopulationController.cs
```
Expected: 8 處（5 個 Create action + `AddNewClass` + `RemoveClassItem` + `UpdateClassItem`）——若看到 8 處以外的數字，代表某個 Task 2/3/4 的步驟漏掉了或重複了，回去檢查。

- [ ] **Step 8: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs source/portal/Portal/Views/StudentPopulation/PopulationPartialView.cshtml source/portal/Portal/Views/StudentPopulation/ASPopulationPartialView.cshtml source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml source/portal/Portal/Views/StudentPopulation/CreatePSJPopulation.cshtml source/portal/Portal/Views/StudentPopulation/CreateGeptPopulation.cshtml source/portal/Portal/Views/StudentPopulation/CreatePSPopulation.cshtml source/portal/Portal/Views/StudentPopulation/CreateASPopulation.cshtml
git commit -m "feat: render editable LastWeekNumber input for PopulationWeekSwitch users"
```

---

### Task 5: 接上 `StudentPopulationItemLog` 審計紀錄

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes：Task 1 的 `StudentPopulationItemLog` 新欄位；Task 2/3 已放寬鎖定的 5 個 action。
- Produces：私有方法 `WriteItemLog(DataContext dataContext, long studentPopulationId, int? classId, string name, int oldNumber, int newNumber, int oldLastWeekNumber, int newLastWeekNumber, string oldStudentRemark, string newStudentRemark, string oldRemark = null, string newRemark = null, int? changeClassId = null, bool isNew = false, bool isDeleted = false)`，本任務內 5 個 action 都呼叫它，之後不再有新任務依賴這裡。

- [ ] **Step 1: 新增 `WriteItemLog` 私有方法**

找到（約 76-85 行）`ResolveSchoolYear` 方法結尾：
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
在這個方法後面（`}` 之後、下一個 `[Authorize...] public IActionResult Index` 之前）插入：
```csharp

        private void WriteItemLog(DataContext dataContext, long studentPopulationId, int? classId, string name,
                int oldNumber, int newNumber, int oldLastWeekNumber, int newLastWeekNumber,
                string oldStudentRemark, string newStudentRemark,
                string oldRemark = null, string newRemark = null,
                int? changeClassId = null, bool isNew = false, bool isDeleted = false) {
            StudentPopulationItemLog log = new StudentPopulationItemLog {
                StudentPopulationId = studentPopulationId,
                ClassId = classId,
                ChangeClassId = changeClassId,
                Name = name,
                Number = oldNumber,
                ChangeNumber = newNumber,
                LastWeekNumber = oldLastWeekNumber,
                ChangeLastWeekNumber = newLastWeekNumber,
                StudentRemark = oldStudentRemark,
                ChangeStudentRemark = newStudentRemark,
                Remark = oldRemark,
                ChangeRemark = newRemark,
                IsNew = isNew,
                IsDeleted = isDeleted,
                MemberId = Guid.Parse(User.Id)
            };
            dataContext.StudentPopulationItemLog.Add(log);
            dataContext.SaveChanges();
        }
```

- [ ] **Step 2: `UpdateClassItem` 寫入 log**

找到 Task 3 改完後的 `UpdateClassItem`（約 1147 行起）這一段：
```csharp
                bool lastWeekApplied = lastWeekNumber.HasValue && canEditLocked;
                if (number.HasValue) {
                    item.Number = number.Value;
                }
                if (studentRemark != null) {
                    item.StudentRemark = studentRemark;
                }
                if (lastWeekApplied) {
                    item.LastWeekNumber = lastWeekNumber.Value;
                }
                dataContext.StudentPopulationItem.Update(item);
                dataContext.SaveChanges();
                if (number.HasValue || lastWeekApplied) {
                    SumPHPopulation(item.StudentPopulation.Id);
                }
```
改成：
```csharp
                bool lastWeekApplied = lastWeekNumber.HasValue && canEditLocked;
                int oldNumber = item.Number;
                int oldLastWeekNumber = item.LastWeekNumber;
                string oldStudentRemark = item.StudentRemark;
                if (number.HasValue) {
                    item.Number = number.Value;
                }
                if (studentRemark != null) {
                    item.StudentRemark = studentRemark;
                }
                if (lastWeekApplied) {
                    item.LastWeekNumber = lastWeekNumber.Value;
                }
                dataContext.StudentPopulationItem.Update(item);
                dataContext.SaveChanges();
                if (number.HasValue || studentRemark != null || lastWeekApplied) {
                    WriteItemLog(dataContext, item.StudentPopulationId, item.ClassId, item.Name, oldNumber, item.Number, oldLastWeekNumber, item.LastWeekNumber, oldStudentRemark, item.StudentRemark);
                }
                if (number.HasValue || lastWeekApplied) {
                    SumPHPopulation(item.StudentPopulation.Id);
                }
```

- [ ] **Step 3: `AddNewClass` 寫入 log（新增班級事件，共用的收斂點，不用改 5 個 type 分支）**

找到（約 1108-1117 行，Task 2 已加過 ViewBag 但這段沒動過）：
```csharp
            dataContext.ChangeTracker.Clear();
            //var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week && e.Type == seleceedType).FirstOrDefault();
            var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == studentPopulationData.Id).FirstOrDefault();
            if (newAddedClassId > 0) {
                var newItem = returnData?.Items?.FirstOrDefault(i => i.ClassId == newAddedClassId);
                if (newItem != null) newItem.IsNew = true;
            }
            ViewBag.Warnings = CheckNewLostConsistency(returnData);
            return PartialView("PopulationPartialView", returnData);
        }
```
改成：
```csharp
            dataContext.ChangeTracker.Clear();
            //var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.School.Id == schoolId && e.Year == year && e.Week == week && e.Type == seleceedType).FirstOrDefault();
            var returnData = dataContext.StudentPopulation.Include("Items").Include("Submitter").Include("School").Include("Items.Class.Course.Department").Where(e => e.Id == studentPopulationData.Id).FirstOrDefault();
            if (newAddedClassId > 0) {
                var newItem = returnData?.Items?.FirstOrDefault(i => i.ClassId == newAddedClassId);
                if (newItem != null) {
                    newItem.IsNew = true;
                    WriteItemLog(dataContext, newItem.StudentPopulationId, newItem.ClassId, newItem.Name, 0, newItem.Number, 0, newItem.LastWeekNumber, null, newItem.StudentRemark, isNew: true);
                }
            }
            ViewBag.Warnings = CheckNewLostConsistency(returnData);
            return PartialView("PopulationPartialView", returnData);
        }
```

**注意**：這裡的 `dataContext` 在這段程式碼之前已經 `ChangeTracker.Clear()` 過，`WriteItemLog` 內部會再呼叫一次 `dataContext.SaveChanges()`，這跟既有程式碼在同一個 `dataContext` 上多次 `SaveChanges()` 的既有寫法一致（例如 `UpdateClassDetail` 就是同一個 `dataContext` 呼叫兩次 `SaveChanges()`），不會有問題。

- [ ] **Step 4: `RemoveClassItem` 寫入 log（刪除事件）**

找到 Task 2 改完後的這段（約 1132-1135 行）：
```csharp
                spId = item.StudentPopulationId;
                dataContext.StudentPopulationItem.Remove(item);
                dataContext.SaveChanges();
                SumPHPopulation(spId);
```
改成：
```csharp
                spId = item.StudentPopulationId;
                dataContext.StudentPopulationItem.Remove(item);
                dataContext.SaveChanges();
                WriteItemLog(dataContext, spId, item.ClassId, item.Name, item.Number, 0, item.LastWeekNumber, 0, item.StudentRemark, null, isDeleted: true);
                SumPHPopulation(spId);
```

- [ ] **Step 5: `UpdateClassDetail` 寫入 log（班級名稱/班別異動）**

找到 Task 2 改完後的整個方法（約 1199-1233 行）：
```csharp
        public IActionResult UpdateClassDetail(long itemId, string name, int classType) {
            try {
                DataContext dataContext = new DataContext();
                var item = dataContext.StudentPopulationItem.Include("StudentPopulation").Include("Class").FirstOrDefault(e => e.Id == itemId);
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked)
                    return Json(new { success = false, message = "人數表狀態不允許修改" });

                var cls = item.Class;
                if (cls == null)
                    return Json(new { success = false, message = "找不到班級" });

                bool classTypeChanged = (int)cls.Type != classType;
                if (!string.IsNullOrWhiteSpace(name))
                    cls.Name = name;
                cls.Type = (ClassType)classType;
                dataContext.Class.Update(cls);
                dataContext.SaveChanges();

                if (!string.IsNullOrWhiteSpace(name)) {
                    item.Name = name;
                    dataContext.StudentPopulationItem.Update(item);
                    dataContext.SaveChanges();
                }

                if (classTypeChanged)
                    SumPHPopulation(item.StudentPopulationId);

                return Json(new { success = true });
            }
            catch (Exception ex) {
                return Json(new { success = false, message = ex.Message });
            }
        }
```
改成：
```csharp
        public IActionResult UpdateClassDetail(long itemId, string name, int classType) {
            try {
                DataContext dataContext = new DataContext();
                var item = dataContext.StudentPopulationItem.Include("StudentPopulation").Include("Class").FirstOrDefault(e => e.Id == itemId);
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
                if (item.StudentPopulation.Status != StudentPopulationStatus.Documented && !canEditLocked)
                    return Json(new { success = false, message = "人數表狀態不允許修改" });

                var cls = item.Class;
                if (cls == null)
                    return Json(new { success = false, message = "找不到班級" });

                bool classTypeChanged = (int)cls.Type != classType;
                string oldDetail = string.Format("名稱:{0} 班別:{1}", cls.Name, cls.Type);
                if (!string.IsNullOrWhiteSpace(name))
                    cls.Name = name;
                cls.Type = (ClassType)classType;
                dataContext.Class.Update(cls);
                dataContext.SaveChanges();

                if (!string.IsNullOrWhiteSpace(name)) {
                    item.Name = name;
                    dataContext.StudentPopulationItem.Update(item);
                    dataContext.SaveChanges();
                }

                string newDetail = string.Format("名稱:{0} 班別:{1}", cls.Name, cls.Type);
                WriteItemLog(dataContext, item.StudentPopulationId, item.ClassId, item.Name, item.Number, item.Number, item.LastWeekNumber, item.LastWeekNumber, item.StudentRemark, item.StudentRemark, oldRemark: oldDetail, newRemark: newDetail);

                if (classTypeChanged)
                    SumPHPopulation(item.StudentPopulationId);

                return Json(new { success = true });
            }
            catch (Exception ex) {
                return Json(new { success = false, message = ex.Message });
            }
        }
```

- [ ] **Step 6: `UpdateClassDetail2` 寫入 log**

找到 Task 2 改完後的整個方法（約 1237-1272 行）：
```csharp
        public IActionResult UpdateClassDetail2(long populationId, int classId, string name, int classType) {
            DataContext dataContext = new DataContext();
            var population = dataContext.StudentPopulation
                .FirstOrDefault(p => p.Id == populationId);

            if (population == null)
                return Json(new { success = false, message = "找不到人數表" });

            bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
            if (population.Status != StudentPopulationStatus.Documented && !canEditLocked)
                return Json(new { success = false, message = "人數表狀態不允許修改" });

            var cls = dataContext.Class.FirstOrDefault(c => c.Id == classId);
            if (cls == null)
                return Json(new { success = false, message = "找不到班級" });

            bool classTypeChanged = (int)cls.Type != classType;

            if (!string.IsNullOrWhiteSpace(name))
                cls.Name = name;

            cls.Type = (ClassType)classType;
            dataContext.SaveChanges();

            if (!string.IsNullOrWhiteSpace(name)) {
                var item = dataContext.StudentPopulationItem.FirstOrDefault(e => e.ClassId == classId && e.StudentPopulationId == populationId);
                if (item != null) {
                    item.Name = name;
                    dataContext.SaveChanges();
                }
            }

            if (classTypeChanged)
                SumPHPopulation(populationId);

            return Json(new { success = true });
        }
```
改成：
```csharp
        public IActionResult UpdateClassDetail2(long populationId, int classId, string name, int classType) {
            DataContext dataContext = new DataContext();
            var population = dataContext.StudentPopulation
                .FirstOrDefault(p => p.Id == populationId);

            if (population == null)
                return Json(new { success = false, message = "找不到人數表" });

            bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
            if (population.Status != StudentPopulationStatus.Documented && !canEditLocked)
                return Json(new { success = false, message = "人數表狀態不允許修改" });

            var cls = dataContext.Class.FirstOrDefault(c => c.Id == classId);
            if (cls == null)
                return Json(new { success = false, message = "找不到班級" });

            bool classTypeChanged = (int)cls.Type != classType;
            string oldDetail = string.Format("名稱:{0} 班別:{1}", cls.Name, cls.Type);

            if (!string.IsNullOrWhiteSpace(name))
                cls.Name = name;

            cls.Type = (ClassType)classType;
            dataContext.SaveChanges();

            var item = dataContext.StudentPopulationItem.FirstOrDefault(e => e.ClassId == classId && e.StudentPopulationId == populationId);
            if (!string.IsNullOrWhiteSpace(name) && item != null) {
                item.Name = name;
                dataContext.SaveChanges();
            }

            if (item != null) {
                string newDetail = string.Format("名稱:{0} 班別:{1}", cls.Name, cls.Type);
                WriteItemLog(dataContext, populationId, classId, item.Name, item.Number, item.Number, item.LastWeekNumber, item.LastWeekNumber, item.StudentRemark, item.StudentRemark, oldRemark: oldDetail, newRemark: newDetail);
            }

            if (classTypeChanged)
                SumPHPopulation(populationId);

            return Json(new { success = true });
        }
```

**注意**：這裡把原本只在 `!string.IsNullOrWhiteSpace(name)` 條件內才查詢的 `item`，提到條件外面先查一次——這樣「只改班別、沒改名稱」的情況也查得到 `item` 供寫 log 用（原本的邏輯漏洞：只改班別時完全不會碰 `StudentPopulationItem`，這裡的重構讓查詢跟寫 log 都能涵蓋這個情況，但**不影響**原本「只有名稱非空白才更新 `item.Name`」這條業務邏輯，兩者是獨立的 if）。

- [ ] **Step 7: Build 確認編譯成功**

```bash
cd source/portal && dotnet build Portal/Portal.csproj
```
Expected: `0 個錯誤`。

- [ ] **Step 8: sqlcmd 驗證 — 確認 log 表格目前筆數（basline，實際觸發驗證留給最終瀏覽器手動測試）**

```bash
sqlcmd -S <Server> -d <Database> -U <User> -P '<Pwd>' -Q "SELECT COUNT(*) AS LogCount FROM StudentPopulationItemLog"
```
記下這個數字。之後在最終瀏覽器手動驗證階段，實際在畫面上編輯任一筆本週人數/上週人數/備註/班別/新增班級/刪除班級後，重新執行同一句 SQL，確認 `LogCount` 有增加，並且：
```bash
sqlcmd -S <Server> -d <Database> -U <User> -P '<Pwd>' -Q "SELECT TOP 5 Id, StudentPopulationId, ClassId, Number, ChangeNumber, LastWeekNumber, ChangeLastWeekNumber, IsNew, IsDeleted, MemberId, CreatedTime FROM StudentPopulationItemLog ORDER BY Id DESC"
```
確認最新幾筆的 `MemberId` 有值、`Number`/`ChangeNumber`（或 `LastWeekNumber`/`ChangeLastWeekNumber`）反映實際異動前後值。

- [ ] **Step 9: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: write StudentPopulationItemLog audit rows on every population item edit"
```

---

## 最終驗證（全部任務完成後，需要瀏覽器，目前環境沒有瀏覽器工具，留待使用者或之後集中驗證那一輪）

- 用具備 `PopulationWeekSwitch` 權限的帳號登入，切到 Task 2 Step 6 記下的那筆已送出（`Status <> 0`）週次，確認：本週人數、上週人數、備註都可以編輯並即時重算；新增/刪除班級也能操作；班級名稱/班別也能改。
- 用一般（無 `PopulationWeekSwitch` 權限）帳號登入，確認已送出週次依然完全唯讀，上週人數欄位依然是純文字、不是輸入框。
- 每次操作後用 Task 5 Step 8 的 SQL 確認 `StudentPopulationItemLog` 有正確寫入。

## Self-Review

- **Spec 涵蓋度**：spec 的 A（解鎖）→ Task 2/3；B（上週人數可編輯+auto-save+recalc）→ Task 3/4；C（審計 log）→ Task 1/5。全部涵蓋。
- **Placeholder 掃描**：無 TBD/TODO，每個 Step 都有實際程式碼或指令。
- **型別一致性**：`WriteItemLog` 簽章在 Task 5 Step 1 定義後，Step 2-6 的呼叫全部核對過參數順序/型別一致（`oldLastWeekNumber`/`newLastWeekNumber` 皆為 `int`，對應 `item.LastWeekNumber` 的既有 `int` 型別；`oldRemark`/`newRemark`/`changeClassId`/`isNew`/`isDeleted` 皆用具名引數呼叫，避免位置引數對錯）。`ViewBag.CanEditLastWeek` 在 Task 2/3/4 各處賦值型別一致（`bool`），Task 4 讀取端 `(bool)ViewBag.CanEditLastWeek` 轉型一致。
