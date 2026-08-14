# 總監角色（主要轄校可編輯）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a "總監" (Director) role: members with this role can write (create/edit/delete) population data only for their "主要轄校" (primary school), and can only view/export data for the rest of their assigned schools. As a side effect, close the "no school-ownership check on write endpoints" gap (permission-gap-ledger issue #3) across the population write/read endpoints this touches.

**Architecture:** Add `Member.PrimarySchoolId` (nullable FK to `School`) and a new `SystemPermission.RestrictedToPrimarySchool` enum value. Extract the access-decision logic into a pure, unit-testable static class (`SchoolAccessEvaluator`), expose it through two `Model` wrapper methods (`CanAccessSchool` for read, `CanEditSchool` for write), and call those wrappers as guard clauses in the 8 controller actions identified in the spec. Add an Admin UI dropdown so `PrimarySchoolId` can only be set to one of a member's already-assigned schools.

**Tech Stack:** ASP.NET Core 8 MVC, EF Core 8 (SQL Server), NUnit 4, DevExtreme.AspNet.Core grids.

**Spec:** `docs/superpowers/specs/2026-08-14-director-role-and-admin-date-bypass-design.md`

## Global Constraints

- `RestrictedToPrimarySchool` does NOT get overridden by `ViewAllSchools` — write access stays locked to the primary school even if both permissions are somehow granted together (spec's explicit edge-case decision).
- No `PrimarySchoolId` set → all schools read-only for that member (safe default, no fallback to "all assigned schools editable").
- Every new pure-logic unit (`SchoolAccessEvaluator`) must be covered by NUnit tests using in-memory POCOs — no DB, no mocking framework (matches this repo's existing `SchoolYearResolverTests`/`AggregationEngineTests` convention).
- Controller-level wiring (DataContext queries, `[Authorize]`, DevExtreme grid actions) is NOT unit tested — matches this repo's existing convention (no controller tests exist; see `source/portal/Test/`).
- **The actual database schema change (new `Member.PrimarySchoolId` column) targets a database that is effectively production** (`reference_db_connection_is_production` — `appsettings.json`'s `DataContext` connection string points at the live server). Task 7 (applying the migration) must never be run unattended — get the user's explicit go-ahead immediately before running the SQL against that database, and verify with a `SELECT` afterward (this repo's `sqlcmd -i` has been unreliable before — don't trust silent success).

---

### Task 1: Data model — `Member.PrimarySchoolId`, `SystemPermission.RestrictedToPrimarySchool`, EF migration (scaffold only, no DB apply)

**Files:**
- Modify: `source/schema/Data/Community/Member.cs`
- Modify: `source/schema/Data/SystemPermission.cs`
- Create: `source/schema/Data/Migrations/<timestamp>_AddMemberPrimarySchoolId.cs` (+ matching `.Designer.cs`, generated)
- Modify: `source/schema/Data/Migrations/DataContextModelSnapshot.cs` (generated, updated by the same command)

**Interfaces:**
- Produces: `Member.PrimarySchoolId` (`int?`), `Member.PrimarySchool` (`School`, nav property); `SystemPermission.RestrictedToPrimarySchool` (enum member). Both consumed by Task 3.

- [ ] **Step 1: Add `PrimarySchoolId`/`PrimarySchool` to `Member`**

In `source/schema/Data/Community/Member.cs`, add right after the existing `MemberRoles` property (currently the last property, ends the class at line 217-218):

```csharp
        /// <summary>
        /// 主要轄校（總監角色僅能編輯此分校，其餘被指派分校唯讀）
        /// </summary>
        [Display(Name = "主要轄校"), DataMember]
        public int? PrimarySchoolId { get; set; }

        /// <summary>
        /// 主要轄校資料
        /// </summary>
        [Display(Name = "主要轄校資料"), DataMember]
        public School PrimarySchool { get; set; }
```

(`School` resolves via the existing `using PHStatistics.Content;` at the top of the file — no new using needed.)

- [ ] **Step 2: Add `RestrictedToPrimarySchool` to `SystemPermission`**

In `source/schema/Data/SystemPermission.cs`, add after the last member (`StudentPopulationImport`, currently ending the enum at line 161-162):

```csharp
    /// <summary>
    /// 總監（主要轄校可編輯，其餘轄校唯讀）
    /// </summary>
    [Display(Name = "總監（僅主要轄校可編輯，其餘轄校唯讀）")]
    RestrictedToPrimarySchool,
```

This enum has no explicit numeric values anywhere (pure sequential auto-increment) — appending at the end is safe and does not renumber existing members.

- [ ] **Step 3: Build to confirm both changes compile**

Run (from `source/`): `dotnet build portal/PHStatistics.portal.sln`
Expected: `0 個錯誤` (0 errors). Warnings unrelated to these files are pre-existing and fine.

- [ ] **Step 4: Scaffold the EF migration (does not touch the database)**

Run (from `source/`):
```bash
dotnet ef migrations add AddMemberPrimarySchoolId --project schema/Data/Data.csproj --startup-project portal/Portal/Portal.csproj --output-dir Migrations
```
This only generates C# migration files under `source/schema/Data/Migrations/` and updates `DataContextModelSnapshot.cs` — it does not connect to or modify any database. Do **not** follow this with `dotnet ef database update` (that command has previously applied against the wrong/unintended database from this exact `Data.csproj`/`Portal.csproj` pairing — see Task 7 for the correct, gated way to apply it).

- [ ] **Step 5: Verify the generated migration only does what's expected**

Open the newly generated `<timestamp>_AddMemberPrimarySchoolId.cs` and confirm the `Up()` method contains exactly one `AddColumn<int>` (name: `"PrimarySchoolId"`, table: `"Member"`, nullable: true), one `CreateIndex` on `Member.PrimarySchoolId`, and one `AddForeignKey` to `School.Id` — matching this repo's existing pattern for nullable FK columns, e.g. `source/schema/Data/Migrations/20240521100748_add_school.cs`:

```csharp
migrationBuilder.AddColumn<int>(
    name: "PrimarySchoolId",
    table: "Member",
    type: "int",
    nullable: true);

migrationBuilder.CreateIndex(
    name: "IX_Member_PrimarySchoolId",
    table: "Member",
    column: "PrimarySchoolId");

migrationBuilder.AddForeignKey(
    name: "FK_Member_School_PrimarySchoolId",
    table: "Member",
    column: "PrimarySchoolId",
    principalTable: "School",
    principalColumn: "Id");
```
If the generated file contains anything beyond this (e.g. unrelated model drift being picked up), stop and report back rather than proceeding — do not silently accept unexpected schema changes.

- [ ] **Step 6: Commit**

```bash
git add source/schema/Data/Community/Member.cs source/schema/Data/SystemPermission.cs source/schema/Data/Migrations/
git commit -m "feat: add Member.PrimarySchoolId and RestrictedToPrimarySchool permission (schema only, migration not yet applied)"
```

---

### Task 2: `SchoolAccessEvaluator` pure logic (TDD)

**Files:**
- Create: `source/portal/Portal/Services/SchoolAccessEvaluator.cs`
- Test: `source/portal/Test/Services/SchoolAccessEvaluatorTests.cs`

**Interfaces:**
- Produces: `SchoolAccessEvaluator.CanAccessSchool(bool hasViewAllSchools, IEnumerable<int> accessibleSchoolIds, int targetSchoolId) : bool` and `SchoolAccessEvaluator.CanEditSchool(bool isAdministrator, bool isRestrictedToPrimarySchool, int? primarySchoolId, bool hasViewAllSchools, IEnumerable<int> accessibleSchoolIds, int targetSchoolId) : bool`. Consumed by Task 3.

- [ ] **Step 1: Write the failing tests**

Create `source/portal/Test/Services/SchoolAccessEvaluatorTests.cs`:

```csharp
using System.Collections.Generic;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal.Test.Services;

[TestFixture]
public class SchoolAccessEvaluatorTests {
    [Test]
    public void CanAccessSchool_ReturnsTrue_WhenViewAllSchools_EvenIfNotInAccessibleList() {
        bool result = SchoolAccessEvaluator.CanAccessSchool(
            hasViewAllSchools: true, accessibleSchoolIds: new List<int>(), targetSchoolId: 99);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanAccessSchool_ReturnsTrue_WhenTargetInAccessibleList() {
        bool result = SchoolAccessEvaluator.CanAccessSchool(
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 1, 2, 3 }, targetSchoolId: 2);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanAccessSchool_ReturnsFalse_WhenTargetNotInAccessibleList_AndNoViewAllSchools() {
        bool result = SchoolAccessEvaluator.CanAccessSchool(
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 1, 2, 3 }, targetSchoolId: 4);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanEditSchool_ReturnsTrue_WhenAdministrator_EvenIfNotAssigned() {
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: true, isRestrictedToPrimarySchool: false, primarySchoolId: null,
            hasViewAllSchools: false, accessibleSchoolIds: new List<int>(), targetSchoolId: 99);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanEditSchool_ReturnsTrue_WhenRestricted_AndTargetIsPrimarySchool() {
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: false, isRestrictedToPrimarySchool: true, primarySchoolId: 5,
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 5, 6, 7 }, targetSchoolId: 5);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanEditSchool_ReturnsFalse_WhenRestricted_AndTargetIsNotPrimarySchool_EvenIfInAccessibleList() {
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: false, isRestrictedToPrimarySchool: true, primarySchoolId: 5,
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 5, 6, 7 }, targetSchoolId: 6);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanEditSchool_ReturnsFalse_WhenRestricted_AndPrimarySchoolNotSet() {
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: false, isRestrictedToPrimarySchool: true, primarySchoolId: null,
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 5, 6, 7 }, targetSchoolId: 5);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanEditSchool_ReturnsTrue_WhenNotRestricted_AndTargetInAccessibleList() {
        // 一般被指派多校的使用者，行為維持現行：被指派分校即可編輯
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: false, isRestrictedToPrimarySchool: false, primarySchoolId: null,
            hasViewAllSchools: false, accessibleSchoolIds: new[] { 5, 6, 7 }, targetSchoolId: 6);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanEditSchool_ReturnsFalse_WhenRestricted_AndViewAllSchoolsAlsoGranted_ButTargetNotPrimary() {
        // ViewAllSchools 不會放寬 RestrictedToPrimarySchool 的寫入限制（spec 明確邊界情況）
        bool result = SchoolAccessEvaluator.CanEditSchool(
            isAdministrator: false, isRestrictedToPrimarySchool: true, primarySchoolId: 5,
            hasViewAllSchools: true, accessibleSchoolIds: new List<int>(), targetSchoolId: 6);

        Assert.That(result, Is.False);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run (from `source/`): `dotnet test portal/Test/Test.csproj --filter "FullyQualifiedName~SchoolAccessEvaluatorTests"`
Expected: compile error `CS0246`/`CS0103` — `SchoolAccessEvaluator` does not exist yet.

- [ ] **Step 3: Implement `SchoolAccessEvaluator`**

Create `source/portal/Portal/Services/SchoolAccessEvaluator.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace PHStatistics.Portal.Services {
    public static class SchoolAccessEvaluator {
        /// <summary>
        /// 瀏覽/匯出用的存取判斷。
        /// </summary>
        public static bool CanAccessSchool(bool hasViewAllSchools, IEnumerable<int> accessibleSchoolIds, int targetSchoolId) {
            if (hasViewAllSchools) return true;
            return accessibleSchoolIds.Contains(targetSchoolId);
        }

        /// <summary>
        /// 寫入用的存取判斷。RestrictedToPrimarySchool 只認主要轄校，
        /// 不會被 ViewAllSchools 放寬；其餘角色沿用現行「被指派分校即可寫」。
        /// </summary>
        public static bool CanEditSchool(bool isAdministrator, bool isRestrictedToPrimarySchool, int? primarySchoolId,
                bool hasViewAllSchools, IEnumerable<int> accessibleSchoolIds, int targetSchoolId) {
            if (isAdministrator) return true;
            if (isRestrictedToPrimarySchool) return primarySchoolId.HasValue && primarySchoolId.Value == targetSchoolId;
            return CanAccessSchool(hasViewAllSchools, accessibleSchoolIds, targetSchoolId);
        }
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run (from `source/`): `dotnet test portal/Test/Test.csproj --filter "FullyQualifiedName~SchoolAccessEvaluatorTests"`
Expected: `已通過! - 失敗: 0，通過: 9`

- [ ] **Step 5: Commit**

```bash
git add -f source/portal/Test/Services/SchoolAccessEvaluatorTests.cs
git add source/portal/Portal/Services/SchoolAccessEvaluator.cs
git commit -m "feat: add SchoolAccessEvaluator pure permission logic with unit tests"
```
(`git add -f` is required because `source/portal/Test/` is gitignored at the folder level, but every existing test file in that project is nonetheless tracked — matches this repo's established, if unusual, convention.)

---

### Task 3: `Model.CanAccessSchool` / `Model.CanEditSchool` wrappers

**Files:**
- Modify: `source/portal/Portal/Models/Model.cs`

**Interfaces:**
- Consumes: `SchoolAccessEvaluator.CanAccessSchool`/`CanEditSchool` (Task 2), `Member.PrimarySchoolId` (Task 1), existing `GetAccessibleSchools(PortalUser user)` (already in this file).
- Produces: `Model.CanAccessSchool(PortalUser user, int schoolId) : bool`, `Model.CanEditSchool(PortalUser user, int schoolId) : bool`. Consumed by Tasks 4 and 5.

- [ ] **Step 1: Add the two wrapper methods**

In `source/portal/Portal/Models/Model.cs`, add immediately after the existing `GetMemberSchool` method (currently ends at line 117):

```csharp
        /// <summary>
        /// 瀏覽/匯出是否可存取指定分校（總監角色的全部轄校都算）
        /// </summary>
        public bool CanAccessSchool(PortalUser user, int schoolId) {
            return SchoolAccessEvaluator.CanAccessSchool(
                user.HasPermission(SystemPermission.ViewAllSchools),
                GetAccessibleSchools(user).Select(s => s.Id),
                schoolId);
        }

        /// <summary>
        /// 寫入是否可編輯指定分校（總監角色只能編輯主要轄校）
        /// </summary>
        public bool CanEditSchool(PortalUser user, int schoolId) {
            // PortalUser.Data 登入時就是完整的 Member 實體（見 PortalUser.cs 的 Id setter），
            // 不需要幫 PortalUser 額外開欄位或查表
            int? primarySchoolId = (user.Data as Member)?.PrimarySchoolId;
            return SchoolAccessEvaluator.CanEditSchool(
                user.HasPermission(SystemPermission.Administrator),
                user.HasPermission(SystemPermission.RestrictedToPrimarySchool),
                primarySchoolId,
                user.HasPermission(SystemPermission.ViewAllSchools),
                GetAccessibleSchools(user).Select(s => s.Id),
                schoolId);
        }
```

Add the missing using at the top of `Model.cs`:
```csharp
using PHStatistics.Portal.Services;
```

- [ ] **Step 2: Build to confirm it compiles**

Run (from `source/`): `dotnet build portal/PHStatistics.portal.sln`
Expected: `0 個錯誤`.

(No new automated test here — this method's only new logic is the `GetAccessibleSchools`/`HasPermission`/`Member` wiring, which requires a live `DataContext`/`PortalUser`, matching this repo's existing untested convention for `GetAccessibleSchools` itself. The decision logic it wires together is already fully covered by Task 2's tests.)

- [ ] **Step 3: Commit**

```bash
git add source/portal/Portal/Models/Model.cs
git commit -m "feat: add Model.CanAccessSchool/CanEditSchool wrappers"
```

---

### Task 4: Guard the 6 write endpoints with `CanEditSchool`

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes: `Model.CanEditSchool(PortalUser user, int schoolId)` (Task 3).

- [ ] **Step 1: `CreatePopulation`**

Find (around line 250):
```csharp
        public IActionResult CreatePopulation(StudentPopulation data, int schoolId, string type, int? schoolYearId = null, bool confirmed = false) {
            if (Request.Method == "POST") {
```
Replace with:
```csharp
        public IActionResult CreatePopulation(StudentPopulation data, int schoolId, string type, int? schoolYearId = null, bool confirmed = false) {
            if (!Model.CanEditSchool(User, schoolId))
                return Forbid();
            if (Request.Method == "POST") {
```

- [ ] **Step 2: `AddNewClass`**

Find (around line 1100):
```csharp
        public IActionResult AddNewClass(int courseId, int schoolId, int year, int week, string[][] itemArr, int newClassType, string newClassName, int newNumber, string newStudentremark, string type) {
            DataContext dataContext = new DataContext();
```
Replace with:
```csharp
        public IActionResult AddNewClass(int courseId, int schoolId, int year, int week, string[][] itemArr, int newClassType, string newClassName, int newNumber, string newStudentremark, string type) {
            if (!Model.CanEditSchool(User, schoolId))
                return Forbid();
            DataContext dataContext = new DataContext();
```

- [ ] **Step 3: `RemoveClassItem`**

Find (around line 1394):
```csharp
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("StudentPopulation").Where(e => e.Id == sId).FirstOrDefault();
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
```
Replace with:
```csharp
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("StudentPopulation").Where(e => e.Id == sId).FirstOrDefault();
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                if (!Model.CanEditSchool(User, item.StudentPopulation.SchoolId ?? 0))
                    return Forbid();
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
```
(`?? 0` is deliberate: a `StudentPopulation` with no `SchoolId` can never equal a real school's positive `Id`, so this denies by default rather than throwing on a `null` — there is no scenario where a population legitimately has no school.)

- [ ] **Step 4: `UpdateClassItem`**

Find (around line 1426):
```csharp
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("Class.Course.Department").Include("StudentPopulation").Where(e => e.Id == sId).FirstOrDefault();
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
```
Replace with:
```csharp
                StudentPopulationItem item = dataContext.StudentPopulationItem.Include("Class.Course.Department").Include("StudentPopulation").Where(e => e.Id == sId).FirstOrDefault();
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                if (!Model.CanEditSchool(User, item.StudentPopulation.SchoolId ?? 0))
                    return Forbid();
                List<Course> courses = Model.DataContext.Course.Where(e => e.Type == item.StudentPopulation.Type).OrderBy(e => e.Ordinal).ToList();
```

- [ ] **Step 5: `UpdateRemark`**

Find (around line 1794):
```csharp
                using var db = new DataContext();
                var item = db.StudentPopulationItem.FirstOrDefault(e => e.Id == sId);
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                item.StudentRemark = studentRemark ?? "";
```
Replace with:
```csharp
                using var db = new DataContext();
                var item = db.StudentPopulationItem.Include("StudentPopulation").FirstOrDefault(e => e.Id == sId);
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                if (!Model.CanEditSchool(User, item.StudentPopulation.SchoolId ?? 0))
                    return Forbid();
                item.StudentRemark = studentRemark ?? "";
```

- [ ] **Step 6: `UpdateClassDetail`**

Find (around line 1812):
```csharp
                var item = dataContext.StudentPopulationItem.Include("StudentPopulation").Include("Class").FirstOrDefault(e => e.Id == itemId);
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
```
Replace with:
```csharp
                var item = dataContext.StudentPopulationItem.Include("StudentPopulation").Include("Class").FirstOrDefault(e => e.Id == itemId);
                if (item == null)
                    return Json(new { success = false, message = "找不到項目" });
                if (!Model.CanEditSchool(User, item.StudentPopulation.SchoolId ?? 0))
                    return Forbid();
                bool canEditLocked = User.HasPermission(SystemPermission.PopulationWeekSwitch);
```

- [ ] **Step 7: Build**

Run (from `source/`): `dotnet build portal/PHStatistics.portal.sln`
Expected: `0 個錯誤`.

- [ ] **Step 8: Full test suite still green**

Run (from `source/`): `dotnet test portal/Test/Test.csproj`
Expected: same pass count as before this task (these are controller changes with no new automated tests, per Global Constraints — this step confirms nothing else broke).

- [ ] **Step 9: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "fix: enforce CanEditSchool ownership check on population write endpoints"
```

---

### Task 5: Guard the 2 read endpoints with `CanAccessSchool`

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

**Interfaces:**
- Consumes: `Model.CanAccessSchool(PortalUser user, int schoolId)` (Task 3).

- [ ] **Step 1: `QueryPopulationPartial`**

Find (around line 1957):
```csharp
        public IActionResult QueryPopulationPartial(int schoolId, int year, int week, string reportType) {

            List<Course> courses = Model.DataContext.Course.OrderBy(e => e.Ordinal).ToList();
```
Replace with:
```csharp
        public IActionResult QueryPopulationPartial(int schoolId, int year, int week, string reportType) {
            if (!Model.CanAccessSchool(User, schoolId))
                return Forbid();

            List<Course> courses = Model.DataContext.Course.OrderBy(e => e.Ordinal).ToList();
```

- [ ] **Step 2: `ExportPopulationPartial`**

Find (around line 2624):
```csharp
        public IActionResult ExportPopulationPartial(int schoolId, int year, int week, string reportType) {
            var seleceedType = reportType switch {
```
Replace with:
```csharp
        public IActionResult ExportPopulationPartial(int schoolId, int year, int week, string reportType) {
            if (!Model.CanAccessSchool(User, schoolId))
                return Forbid();
            var seleceedType = reportType switch {
```

- [ ] **Step 3: Build**

Run (from `source/`): `dotnet build portal/PHStatistics.portal.sln`
Expected: `0 個錯誤`.

- [ ] **Step 4: Full test suite still green**

Run (from `source/`): `dotnet test portal/Test/Test.csproj`
Expected: same pass count as Task 4's step 8.

- [ ] **Step 5: Commit**

```bash
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "fix: enforce CanAccessSchool ownership check on population read endpoints"
```

---

### Task 6: Admin UI — Member 編輯畫面「主要轄校」下拉選單

**Files:**
- Modify: `source/portal/Portal/Areas/Admin/Controllers/MemberController.cs`
- Modify: `source/portal/Portal/Areas/Admin/Views/Member/Index.cshtml`

**Interfaces:**
- Consumes: `Member.PrimarySchoolId` (Task 1). Existing `SchoolAssignment` CRUD region in `MemberController.cs` (this file).

- [ ] **Step 1: Add `GetAssignedSchools` endpoint**

In `source/portal/Portal/Areas/Admin/Controllers/MemberController.cs`, inside the `#region SchoolAssignment CRUD` block, immediately after the existing `GetSchools` method (currently the last method in that region):

```csharp
        [HttpGet]
        public object GetAssignedSchools(Guid memberId, DataSourceLoadOptions loadOptions) {
            var query = Model.DataContext.SchoolAssignment
                .Where(e => e.MemberId == memberId)
                .Select(e => new { Id = e.SchoolId, Name = e.School.Name });
            return DataSourceLoader.Load(query, loadOptions);
        }
```

- [ ] **Step 2: Build to confirm it compiles**

Run (from `source/`): `dotnet build portal/PHStatistics.portal.sln`
Expected: `0 個錯誤`.

- [ ] **Step 3: Commit the controller change**

```bash
git add source/portal/Portal/Areas/Admin/Controllers/MemberController.cs
git commit -m "feat: add GetAssignedSchools endpoint for primary-school dropdown"
```

- [ ] **Step 4: Add the member-scoped schools store and editor override in the view**

In `source/portal/Portal/Areas/Admin/Views/Member/Index.cshtml`, add this function right after the existing `rolesStore` declaration (around line 32, before `createSchoolAssignmentSource`):

```javascript
    // 主要轄校下拉選單資料源：只列出該成員已被指派的分校
    function createAssignedSchoolsStore(memberId) {
        return new DevExpress.data.CustomStore({
            key: "Id",
            loadMode: "raw",
            load: function() {
                if (!memberId) return [];
                return $.getJSON(
                    "@Url.Action("GetAssignedSchools", "Member", new { area = "Admin" })",
                    { memberId: memberId }
                ).then(function(r) { return r.data || r; });
            }
        });
    }

    function onMemberEditorPreparing(e) {
        if (e.parentType !== "dataRow") return;
        if (e.dataField === "PrimarySchoolId") {
            e.editorOptions.dataSource = createAssignedSchoolsStore(currentEditMemberId);
            e.editorOptions.searchEnabled = true;
        }
    }
```

- [ ] **Step 5: Wire the editor-preparing handler and add the grid column**

Find (around line 258-259):
```csharp
        @(Html.DevExtreme().DataGrid<Member>()
            .ID("gridMembers")
            .OnEditingStart("onMemberEditingStart")
```
Replace with:
```csharp
        @(Html.DevExtreme().DataGrid<Member>()
            .ID("gridMembers")
            .OnEditingStart("onMemberEditingStart")
            .OnEditorPreparing("onMemberEditorPreparing")
```

Find, inside `.Columns(columns => { ... })` (around line 304), the `Status` column definition:
```csharp
                columns.AddFor(m => m.Status).Caption("狀態").Width(100)
                    .Lookup(lookup => lookup
                        .DataSource(new JS("memberStatusOptions"))
                        .ValueExpr("value")
                        .DisplayExpr("text")
                    );
```
Add immediately after it:
```csharp
                columns.AddFor(m => m.PrimarySchoolId).Caption("主要轄校")
                    .Lookup(lookup => lookup
                        .DataSource(new JS("schoolsStore"))
                        .ValueExpr("Id")
                        .DisplayExpr("Name")
                    );
```
(The column's `Lookup` uses the existing global `schoolsStore` so the grid can render the correct school name for *any* member's saved value; `onMemberEditorPreparing` swaps in the member-scoped `createAssignedSchoolsStore` only while editing, so the edit dropdown only offers that member's already-assigned schools.)

- [ ] **Step 6: Build**

Run (from `source/`): `dotnet build portal/PHStatistics.portal.sln`
Expected: `0 個錯誤`.

- [ ] **Step 7: Manual browser verification (no automated UI test exists in this repo)**

Run the app (`dotnet run --project portal/Portal/Portal.csproj` from `source/`), log in as an Admin, go to `/Admin/Member`, open a member that already has at least one `SchoolAssignment` row, and confirm:
- A new "主要轄校" field appears in the edit popup.
- Its dropdown only lists that member's already-assigned schools (not every school in the system).
- Saving with a value round-trips correctly (reopen the popup, value is still selected).
- Opening a member with **no** assigned schools shows an empty/unselectable dropdown, not an error.

- [ ] **Step 8: Commit**

```bash
git add source/portal/Portal/Areas/Admin/Views/Member/Index.cshtml
git commit -m "feat: add primary-school dropdown to Admin Member edit popup"
```

---

### Task 7: Apply the migration to the database (GATED — do not run unattended)

**⚠️ This task changes schema on a database that is effectively production. Do not dispatch this task to an autonomous subagent. Whoever executes it must get the user's explicit, real-time go-ahead immediately before running the SQL statement in Step 3, and must not proceed on an assumption of prior approval.**

**Files:** none (database only).

- [ ] **Step 1: Generate the SQL script (does not touch the database)**

Run (from `source/`):
```bash
dotnet ef migrations script <PreviousMigrationName> AddMemberPrimarySchoolId --project schema/Data/Data.csproj --startup-project portal/Portal/Portal.csproj --idempotent -o migration-add-primary-school.sql
```
Replace `<PreviousMigrationName>` with the migration immediately before this one (`AddSchoolYearInputStartDate`, per the current latest migration file at the time this plan was written — confirm against `source/schema/Data/Migrations/` before running, in case more migrations landed since). `--idempotent` makes the script safe to run even if partially applied already.

- [ ] **Step 2: Review the generated SQL**

Open `migration-add-primary-school.sql` and confirm it contains only the expected `ALTER TABLE [Member] ADD [PrimarySchoolId] int NULL;`, the matching index creation, and the FK constraint — nothing else. Show this to the user.

- [ ] **Step 3: Get explicit user confirmation, then apply**

State plainly to the user: "This will run the above ALTER TABLE against `NewPAS` on `20.188.19.77,52056` (the connection string in `appsettings.json` — effectively production). Proceed?" Only after an explicit yes, apply via `sqlcmd`:
```bash
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P <password from appsettings.json> -i migration-add-primary-school.sql
```

- [ ] **Step 4: Verify with a SELECT (do not trust sqlcmd's silent exit code)**

```bash
sqlcmd -S 20.188.19.77,52056 -d NewPAS -U pcmdba -P <password> -Q "SELECT TOP 1 PrimarySchoolId FROM Member"
```
Expected: the query runs without a "invalid column name" error (confirms the column actually exists — `sqlcmd -i` has silently no-op'd in this environment before).

- [ ] **Step 5: Delete the generated script and record completion**

```bash
rm migration-add-primary-school.sql
```
No commit needed for this task (no files changed) — just confirm with the user that the column now exists in the target database.
