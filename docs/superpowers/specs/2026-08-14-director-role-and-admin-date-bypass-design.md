# 總監角色（主要轄校可編輯）與管理員輸入日期解除限制 — 設計

**日期：** 2026-08-14
**範圍：** `Member`/`Role`/`SystemPermission` 資料模型、`StudentPopulationController.cs` 權限判斷與寫入端點、Admin 後台 Member 編輯畫面
**背景文件：** `permission-gap-ledger.html`（權限落差清單，issue #2「查看所有分校跟分校下拉選單用兩套邏輯」、issue #3「大多數異動/查詢動作沒有檢查資料歸屬」與此設計直接相關）

---

## 背景

目前分校存取完全依賴 `SchoolAssignment`（Member↔School 多對多，沒有主/副之分）。任何被指派多校的使用者，對每一間被指派的分校擁有完全相同的讀寫權限，沒有「這間可以編輯、那間只能看」的概念。同時，權限本身不是關聯到 SystemPermission 的 junction table，而是把選中的權限序列化成字串存在 `Role.PermissionValue`（`schema/Data/Basis/Role.cs`），執行期由 `PortalUser.HasPermission` 判斷，其中 `Administrator` 會使任何權限檢查一律回傳 true。

本次要處理兩個獨立需求：

1. **新增「總監」角色**：管理多間分校，但只能異動（新增/編輯/刪除人數表與班級明細）自己的「主要轄校」，其餘轄校只能查詢/匯出。
2. **管理員輸入不受限制**：`Administrator` 一律視為可編輯，不需要手動切換週次即可編輯任何時間點的資料（已完成，見下方「已完成部分」）。

第 1 項的落地會直接碰到現有的「大多數異動動作沒有歸屬校驗證」問題（issue #3）：因為系統目前對「這個 schoolId 可不可以寫」完全沒有伺服端檢查，總監的唯讀限制若只做在 UI 層，任何人一樣可以用 populationId 繞過。因此本設計把 issue #3 列出的端點（加上同類但未列出的 `UpdateClassDetail`/`UpdateRemark`）一併補上歸屬檢查。

---

## 已完成部分：管理員輸入日期解除限制

抽出共用的日期窗判斷邏輯到 `PHStatistics.Portal.Services.SchoolYearResolver.ResolveCurrent(IEnumerable<SchoolYear>, DateTime now, bool bypassImportWindow)`（純函式，可用 POCO 單元測試，已於 `Test/Services/SchoolYearResolverTests.cs` 涵蓋 6 種情境）。`bypassImportWindow` 傳入 `User.HasPermission(SystemPermission.Administrator)`，套用在：

- `StudentPopulationController.ResolveSchoolYear`（供 `CreatePopulation`/`AddNewClass` 等 7 個寫入動作使用的週次解析）
- `StudentPopulationController.Index` / `MainMenu` / `Query` 的 `ViewBag.CanEdit` 計算
- `HomeController.Index` 的 `ViewBag.CanEdit` 計算

行為：管理員登入時，只要該週次已經開始（`InputStartDate ?? WeekStartDate <= now`），無論 `ImportEndDate` 是否已過，一律視為可編輯目前週次；非管理員行為不變。

---

## 待實作部分：總監角色

### 資料模型變更

- `Member` 新增欄位 `PrimarySchoolId`（`int?`，FK → `School`，nullable）— 「主要轄校」，需要一次 EF migration。
- `SystemPermission`（`schema/Data/SystemPermission.cs`）新增列舉值 `RestrictedToPrimarySchool`，`Display(Name = "總監（僅主要轄校可編輯，其餘轄校唯讀）")`。透過既有 Role/MemberRole 機制指派給角色，不需要改 Role 的資料結構。

### 權限判斷（新增於 `Portal/Models/Model.cs`，緊鄰現有 `GetAccessibleSchools`/`GetMemberSchool`）

```csharp
// 瀏覽/匯出用：沿用現有邏輯，總監的全部轄校都算（SchoolAssignment 不變）
public bool CanAccessSchool(PortalUser user, int schoolId) {
    if (user.HasPermission(SystemPermission.ViewAllSchools))
        return true; // Administrator 已被 HasPermission 內部涵蓋
    return GetAccessibleSchools(user).Any(s => s.Id == schoolId);
}

// 寫入用：總監只能寫主要轄校；其餘角色維持現行「被指派分校即可寫」
public bool CanEditSchool(PortalUser user, int schoolId) {
    if (user.HasPermission(SystemPermission.Administrator))
        return true;
    if (user.HasPermission(SystemPermission.RestrictedToPrimarySchool)) {
        // PortalUser.Data 登入時就是完整的 Member 實體（見 PortalUser.cs Id setter），
        // 不需要額外查表或幫 PortalUser 新增欄位
        int? primarySchoolId = (user.Data as Member)?.PrimarySchoolId;
        return primarySchoolId.HasValue && primarySchoolId.Value == schoolId;
    }
    return CanAccessSchool(user, schoolId);
}
```

`RestrictedToPrimarySchool` 與 `ViewAllSchools` 若同時被指派給同一角色（非典型設定，但制度上可能發生）：**寫入限制不會被 `ViewAllSchools` 放寬**——`CanEditSchool` 對 `RestrictedToPrimarySchool` 的檢查獨立於 `CanAccessSchool`，`ViewAllSchools` 只放寬瀏覽範圍。

未設定 `PrimarySchoolId` 時，`TryGetPrimarySchoolId` 回傳 `false`，`CanEditSchool` 一律回傳 `false`（安全預設：總監在管理員設定主要轄校之前，所有轄校唯讀）。

### 套用點（issue #3 名單 + 一併補上的 UpdateClassDetail/UpdateRemark）

| Action | 現有簽章 | schoolId 取得方式 | 改用 |
|---|---|---|---|
| `CreatePopulation` | `(StudentPopulation data, int schoolId, ...)` | 參數直接帶 | `CanEditSchool` |
| `AddNewClass` | `(int courseId, int schoolId, ...)` | 參數直接帶 | `CanEditSchool` |
| `RemoveClassItem` | `(long sId)` | 由 `StudentPopulationItem.Id` include `StudentPopulation` 查出 `SchoolId` | `CanEditSchool` |
| `UpdateClassItem` | `(long sId, ...)` | 同上 | `CanEditSchool` |
| `UpdateClassDetail` | `(long itemId, ...)` | 同上（`itemId` 即 item id） | `CanEditSchool` |
| `UpdateRemark` | `(long sId, string studentRemark)` | 同上 | `CanEditSchool` |
| `QueryPopulationPartial` | `(int schoolId, int year, int week, string reportType)` | 參數直接帶 | `CanAccessSchool` |
| `ExportPopulationPartial` | `(int schoolId, int year, int week, string reportType)` | 參數直接帶 | `CanAccessSchool` |

檢查沒通過時的回傳方式，沿用各自 action 現有的失敗慣例（例如 `RemoveClassItem`/`UpdateClassItem` 現有 `return Json(new { success = false, message = "..." })` 的模式，補一個「沒有權限」訊息；`CreatePopulation`/`AddNewClass` 等回傳 View 的動作則導回錯誤頁或帶錯誤訊息的 PartialView，實作時對照各自現有的錯誤處理路徑）。

這一步等於是把 issue #3 列出的端點全部補上歸屬校驗證，不只是為了總監——現行「被指派多校即可編輯所有被指派分校」的一般使用者，也會因為 `CanEditSchool` 的存在，第一次真正被伺服端擋下「輸入別校 populationId」的情況（之前完全沒擋）。這是本次設計刻意涵蓋的範圍，不是意外擴大。

### Admin 後台

- Member 編輯畫面（`Areas/Admin/Views/Member/Index.cshtml` + `MemberController.cs`）新增「主要轄校」下拉選單，選項來源限定該成員目前的 `SchoolAssignment` 分校清單（該成員尚未被指派任何分校時，下拉選單為空、不可選）。
- Role 編輯畫面不用改：新的 `RestrictedToPrimarySchool` 權限值會自動出現在既有的 `GetPermissions`/`dxTagBox` 多選清單中（`Areas/Admin/Controllers/RoleController.cs` 的 `GetPermissions` 目前用 `Enum.GetValues` 列出所有值，不需額外開發）。

### 邊界情況

- 總監角色 + 未設定 `PrimarySchoolId`：所有轄校唯讀（見上方權限判斷）。
- 總監角色 + `PrimarySchoolId` 指向一間不在自己 `SchoolAssignment` 內的分校：Admin 編輯畫面的下拉選單本身就限定只能選已指派的分校，資料層面不會出現這種狀態；不另外在 `CanEditSchool` 加防禦性檢查（YAGNI，UI 已經保證）。
- 後台批次匯入（`HomeController.ImportAll` 系列）：屬於 Admin 後台維運功能，不受此次前台權限調整影響，範圍外。

### 測試計畫

- `CanAccessSchool`/`CanEditSchool` 屬於 Model.cs 上依賴 `PortalUser`/`DataContext` 的方法，比照本專案既有慣例（無 DI/Mock 框架），優先把純判斷邏輯抽成可用 POCO 測試的靜態函式（例如 `PermissionEvaluator.CanEditSchool(bool isAdministrator, bool isRestrictedToPrimarySchool, int? primarySchoolId, IEnumerable<int> accessibleSchoolIds, int targetSchoolId)`），仿照 `SchoolYearResolverTests`/`AggregationEngineTests` 的寫法逐案覆蓋：一般使用者可編輯被指派分校/不可編輯未被指派分校、總監可編輯主要轄校/不可編輯其他轄校/未設定主要轄校時全部唯讀、Administrator 一律可編輯、`RestrictedToPrimarySchool` + `ViewAllSchools` 疊加時仍受限。
- 各 controller action 的實際串接（DataContext 查詢、`[Authorize]`、Json 回應格式）不納入自動化測試範圍，比照專案現行慣例（controller 層無測試覆蓋，只有抽出的純邏輯有測試）。

---

## 待確認事項

無（設計問題已於對話中逐項確認：識別標記用新 SystemPermission、主要轄校在 Admin Member 畫面編輯限定已指派分校、未設定時安全預設全唯讀、UpdateClassDetail/UpdateRemark 一併補上、issue #3 全面比照修復）。
