# 管理員輸入介面調整 Design Spec

## 背景

目前 `SystemPermission.PopulationWeekSwitch` 權限的使用者可以在前台任選本學年度週次進行人數表輸入/編輯（見 `docs/superpowers/plans/2026-07-04-population-week-switch.md`），目的是協助校正歷史資料。但實際使用時發現兩個限制：

1. 一旦該週 `StudentPopulation.Status != Documented`（已送出/已審核/已否決/已完成），`AddNewClass`、`RemoveClassItem`、`UpdateClassItem`、`UpdateClassDetail`、`UpdateClassDetail2` 這 5 個 action 會整筆鎖死（`StudentPopulationController.cs` 各自檢查 `Status != Documented` 就直接回傳鎖定畫面），不管操作者是不是 `PopulationWeekSwitch` 權限持有者。這讓「校正已送出週次的歷史資料」這個 `PopulationWeekSwitch` 原本要解決的需求，實際上做不到——連「本週英語文新生/流失」這種需要人工修正的欄位也無法在送出後調整。
2. 「上週人數」欄位目前在畫面上永遠是純文字（`@sItem.LastWeekNumber`），沒有輸入框。但「與上週相比」「去年同期/比」等計算欄位的值會隨上週人數變動而不同，使用者需要能夠直接修正上週人數並讓相關計算即時重算。

這次調整只針對**具備 `PopulationWeekSwitch` 權限的使用者**放寬限制；一般使用者的行為完全不變（已送出=整表鎖死、上週人數=唯讀文字）。

## 範圍

### A. 解除「已送出」週次的編輯鎖定

`StudentPopulationController.cs` 中以下 5 個 action 目前的鎖定判斷：

| Action | 現行判斷（約略行號） |
|---|---|
| `AddNewClass` | 849 `if (studentPopulationData.Status != StudentPopulationStatus.Documented)` |
| `RemoveClassItem` | 1128 `if (item.StudentPopulation.Status != StudentPopulationStatus.Documented)` |
| `UpdateClassItem` | 1155 同上 |
| `UpdateClassDetail` | 1205 同上 |
| `UpdateClassDetail2` | 1245 `if (population.Status != StudentPopulationStatus.Documented)` |

全部改為：

```csharp
if (population.Status != StudentPopulationStatus.Documented && !User.HasPermission(SystemPermission.PopulationWeekSwitch)) {
    // 沿用現有的鎖定回應寫法
}
```

**`ConfirmPopulation`（1281 行，送出動作本身）不加這個豁免**——已送出週次不能重複觸發「送出」，這是流程狀態轉換，跟「修正資料」是兩件事，維持現狀。

### B. 上週人數改為可編輯輸入框（僅 `PopulationWeekSwitch` 可編輯）

**顯示層**：5 個 `CreateXXXPopulation` action（`CreatePopulation` 177 行、`CreatePSJPopulation` 343 行、`CreateGeptPopulation` 508 行、`CreatePSPopulation` 615 行、`CreateASPopulation` 723 行）都加上：

```csharp
ViewBag.CanEditLastWeek = User.HasPermission(SystemPermission.PopulationWeekSwitch);
```

`PopulationPartialView.cshtml` 目前兩處純文字顯示（248、371 行）與 `ASPopulationPartialView.cshtml` 一處（101 行）都改成：

```csharp
@if (ViewBag.CanEditLastWeek != null && (bool)ViewBag.CanEditLastWeek) {
    <input type="number" class="form-control" value="@sItem.LastWeekNumber" data-sitem="@sItem.Id" id="class_@sItem.Id.ToString()_lastweek" onchange="lastWeekValueChange(this.id)">
} else {
    @sItem.LastWeekNumber
}
```

**重要**：`AddNewClass`、`RemoveClassItem`、`UpdateClassItem`、`UpdateClassDetail` 這幾個 AJAX action 都是各自獨立 `return PartialView("PopulationPartialView", ...)`，並不會繼承 `Create` action 設定的 `ViewBag`（每個 action 是獨立的 HTTP request）。目前這幾個 action 已經各自重新設定 `ViewBag.Courses`（例如 `UpdateClassItem` 1154 行），所以這次也要在**每一個**會回傳 `PopulationPartialView`/`ASPopulationPartialView` 的 action 裡各自加上 `ViewBag.CanEditLastWeek = User.HasPermission(...)`，否則存檔後的局部刷新會讓輸入框「消失」變回純文字（跟 memory 裡記錄過的 AS `SchoolName`/`Name` 綁錯欄位是同一類坑，這次要主動避開）。

**後端**：`UpdateClassItem`（1147 行）簽章加一個可選參數：

```csharp
public IActionResult UpdateClassItem(long sId, int? number, string studentRemark = null, int? lastWeekNumber = null)
```

- 只有 `lastWeekNumber.HasValue && User.HasPermission(SystemPermission.PopulationWeekSwitch)` 時才套用 `item.LastWeekNumber = lastWeekNumber.Value`（伺服器端二次檢查，不只靠前端藏輸入框，避免非管理員直接呼叫 API 繞過）。
- 呼叫 `SumPHPopulation` 的條件從「只要 `number.HasValue`」擴大為「`number.HasValue || (lastWeekNumber.HasValue && 有權限套用時)`」，確保「與上週相比」「去年同期/比」「新生/流失」等衍生欄位即時重算。

**前端 JS**：新增 `lastWeekValueChange(id)`，比照既有 `valueChange(sId)`（`CreatePopulation.cshtml` 326 行）寫法，只差 POST 的欄位是 `lastWeekNumber` 而非 `number`，成功後一樣用回傳的 partial 覆蓋 `#contentItem`。5 個 `CreateXXXPopulation.cshtml` 都要加這個函式（跟現有 `valueChange`/`updateClassDetail` 一樣是各檔重複維護，非共用元件）。

### C. StudentPopulationItemLog 審計紀錄

現況：`StudentPopulationItemLog`（`schema/Data/Content/StudentPopulationItemLog.cs`）資料庫表格已存在（`Migrations/20240603081950_add_itemlog.cs`），但：
- 完全沒有 controller 呼叫過，是死碼。
- 欄位 `LastWeekNumber` 宣告成 `[NotMapped] bool`，資料庫裡根本沒有這個欄位，無法用來存「上週人數異動前的值」。
- 沒有任何「操作者」欄位。

**Schema 修正（需要新 EF Core migration）**：
1. 移除錯誤的 `[NotMapped] bool LastWeekNumber`，改為 `public int? LastWeekNumber { get; set; }`（異動前的上週人數），並新增 `public int? ChangeLastWeekNumber { get; set; }`（異動後的上週人數）——比照既有 `Number`/`ChangeNumber` 的 before/after 命名慣例。
2. 新增操作者欄位：`public Guid? MemberId { get; set; }` + `public Member Member { get; set; }`（導覽屬性），比照專案裡 `Guid.Parse(User.Id)` 找 `Member` 的既有寫法（如 `CreatePSJPopulation` 392 行 `dataContext.Member.Find(Guid.Parse(User.Id))`）。

**寫入時機**（使用者決定：一般使用者在 Documented 狀態下的正常編輯也要記錄，不只是管理員解鎖已送出週次的敏感操作）：

| Action | 記錄內容 |
|---|---|
| `UpdateClassItem` | `Number`/`ChangeNumber`（本週人數前後）、`LastWeekNumber`/`ChangeLastWeekNumber`（上週人數前後）、`StudentRemark`/`ChangeStudentRemark`（備註前後）、`ClassId`、`MemberId` |
| `UpdateClassDetail` / `UpdateClassDetail2` | `ClassId`/`ChangeClassId`、班級名稱與班別異動前後值（存入 `Remark`/`ChangeRemark` 欄位，因 entity 沒有專門的名稱/班別前後欄位）、`MemberId` |
| `AddNewClass` | 新增一筆，`IsNew = true`，`ChangeNumber` = 新增時的人數，`MemberId` |
| `RemoveClassItem` | 新增一筆，記錄刪除前的 `Number`/`LastWeekNumber`/`StudentRemark`/`ClassId`，`MemberId` |

每筆 log 都掛 `StudentPopulationId`（透過現有 `StudentPopulation.ItemsLog` 導覽屬性），`CreatedTime` 由框架 `IOperability` 自動填入，之後可以用「StudentPopulationId + ClassId + CreatedTime 排序」查出某筆資料的完整異動歷史與操作者。

寫入方式：直接在對應 action 裡 `new StudentPopulationItemLog { ... }; dataContext.StudentPopulationItemLog.Add(log); dataContext.SaveChanges();`，不強制透過 `StudentPopulationItemLogCreateAction`（那個 Action 類別只是框架自動產生的通用 CRUD 骨架，`OnCreating`/`OnTransacting` 都是空的，沒有提供這裡需要的欄位對應邏輯，直接用 `DataContext` 寫入即可，跟這幾個 action 現有的存檔方式一致）。

## 影響檔案清單

- `StudentPopulationController.cs`：5 個 `CreateXXXPopulation`（加 ViewBag）+ `AddNewClass`/`RemoveClassItem`/`UpdateClassItem`/`UpdateClassDetail`/`UpdateClassDetail2`（放寬鎖定 + 加 ViewBag + 寫 log）
- `Views/StudentPopulation/PopulationPartialView.cshtml`（2 處 LastWeekNumber 顯示）
- `Views/StudentPopulation/ASPopulationPartialView.cshtml`（1 處 LastWeekNumber 顯示）
- `Views/StudentPopulation/CreatePopulation.cshtml`、`CreatePSJPopulation.cshtml`、`CreateGeptPopulation.cshtml`、`CreatePSPopulation.cshtml`、`CreateASPopulation.cshtml`（各加 `lastWeekValueChange` JS 函式）
- `schema/Data/Content/StudentPopulationItemLog.cs`（欄位修正）
- `schema/Data/Migrations/`（新增一支 migration）

## Out of scope

- `ConfirmPopulation`（送出動作）本身的鎖定邏輯——維持現狀，已送出不能重複送出。
- `Query.cshtml` / `QueryPopulationPartialView.cshtml`——這是獨立的歷史查詢頁面（唯讀瀏覽用途），不是這次「管理員輸入介面」調整的範圍。
- `PSJPopulationPartialView.cshtml`——確認為死碼（沒有任何地方引用），不處理。
- 幫 `StudentPopulationItemLog` 做前台/後台查詢介面——這次只處理寫入，查詢畫面（若之後需要）另開任務。
- `AddNewClass`/`RemoveClassItem` 目前既有的 `schoolId`/`SchoolAssignment` 未做伺服器端重驗證等既有邊界情況（memory 裡已記錄的舊問題）——不在這次範圍內一併修。

## 測試方式

沿用專案慣例：這個 Controller 沒有單元測試基礎設施（直接 `new DataContext()` 接真實 SQL Server），驗證只能靠 `dotnet build` + 手動操作（用 `PopulationWeekSwitch` 權限帳號登入，切到一個已送出的歷史週次，確認可以編輯本週人數/上週人數/備註/班別並即時重算；用一般帳號確認已送出週次依然完全唯讀、上週人數依然是純文字）。目前環境沒有瀏覽器工具，手動驗證需等使用者自行操作或之後集中驗證那一輪。
