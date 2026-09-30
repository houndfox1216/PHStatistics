# Item 跨週對應（PreviousItemId）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓每筆 `StudentPopulationItem` 記得它「上週對應的是哪一筆 Item」，開頁同步上週人數時直接用這個對應取值，不再靠 Class／備註／Id 順序推測。

**Architecture:** `StudentPopulationItem` 新增可為空的 `long? PreviousItemId`（**不建外鍵**）。建新表複製上週項目時寫入來源 Id。`LastWeekNumberMatcher` 新增 `SyncFromLastWeek`：先用 `PreviousItemId` 直接取值；沒有連結或連結已失效的，退回既有的「同 Class 分組＋備註＋Id 順序」配對，並且只在配對**確定**（備註唯一相符，或該組上下週各只有 1 筆）時才把連結寫回（自我修復，不需要歷史回填）。

**Tech Stack:** ASP.NET Core 8 MVC、EF Core 8（SQL Server）、NUnit。

**Spec:** 無獨立設計文件。設計來自 2026-09-30 對話；背景見記憶 `project_lastweeknumber_fix_deployed_pending_20260930.md`（上週人數覆寫 bug，`2f7533a` 已修，本計畫是根治）。

## Global Constraints

- 新欄位型別：`long?`，SQL 型別 `bigint NULL`，欄位名 `PreviousItemId`，表 `StudentPopulationItem`。
- **不得建立外鍵**：匯入 `deleteExisting`、刪除 0 人班級、清理孤兒資料的 SQL 都會刪 Item，外鍵會讓這些操作失敗。
- 既有資料的 `Number`、`LastWeekNumber` 不因加欄位或部署而被批次改寫；只有使用者開頁時的正常同步會寫入。
- 不可用 `dotnet ef database update`（會套到預設 DB，見記憶 `reference_ef_migrations_default_db_mismatch`）。migration 一律產生 script，由使用者用 sqlcmd 套用，並用獨立 SELECT 驗證（見記憶 `reference_sqlcmd_flakiness`）。
- `appsettings.json` 目前指向正式環境（`Server=20.188.19.77,52056;Database=NewPAS`）：任何寫入前先向使用者講明筆數／範圍取得同意。
- 測試目錄被 `source/.gitignore` 的 `portal/Test/` 排除，新增或修改測試檔要 `git add -f`。
- 不可使用 `git stash` 對照基準（obj 被追蹤，見記憶 `feedback_no_git_stash_with_tracked_obj`）。
- 全部回覆與程式註解用繁體中文，跟周邊程式風格一致。

## Review Focus

1. 上週人數表被重新匯入或刪除重建 → 本週 `PreviousItemId` 指向不存在的項目：必須退回 Class 配對，且不可拋例外、不可寫入錯值。（Task 2 測試 3）
2. 兩筆本週項目指向同一筆上週項目（手動重複建立所致）：第二筆必須退回配對而不是也取同一個值。（Task 2 測試 6）
3. 管理員（`PopulationWeekSwitch` 權限）事後修改上週人數：本週開頁必須取到修改後的值（連結取的是上週 Item 的最新 `Number`）。（Task 2 測試 7）
4. 備註空白且順序被打亂的多筆同名班級：有連結時必須對，沒有連結時不可把「順序推測」的結果永久寫成連結。（Task 2 測試 1、5）
5. 本週新增的同名班級（上週沒有對應）：`LastWeekNumber` 應為 0，不可繼承別班的值。（Task 2 測試 8）

---

## File Structure

| 檔案 | 責任 |
|---|---|
| `source/schema/Data/Content/StudentPopulationItem.cs` | 新增 `PreviousItemId` 屬性 |
| `source/schema/Data/Migrations/<timestamp>_AddStudentPopulationItemPreviousItemId*.cs` | EF migration（由工具產生） |
| `source/schema/Data/Migrations/DataContextModelSnapshot.cs` | 工具自動更新 |
| `source/portal/Portal/Services/Import/ImportSupport/LastWeekNumberMatcher.cs` | 配對核心：`PairGroup` 回傳配對結果、新增 `SyncFromLastWeek` |
| `source/portal/Test/Services/Import/LastWeekNumberMatcherTests.cs` | 單元測試 |
| `source/portal/Portal/Controllers/StudentPopulationController.cs` | 7 處建表複製寫入連結；6 處開頁同步改用新方法 |
| `source/portal/Portal/Controllers/HomeController.cs` | `FixLastWeekData` 改用新方法 |

**明確不做（YAGNI／範圍外）：**
- 不做歷史回填工具：新週次建立時一定會寫連結；已存在的本週表在使用者第一次開頁時自我修復。舊週次是歷史資料，不會再被同步。
- 不改匯入路徑（`PopulationImportService.CorrectLastWeekNumbers` 仍用 `LastWeekNumberMatcher.Apply`，依 CourseId+ClassType 分組）。匯入每次建立新 Class，本來就不靠 ClassId；匯入建立的週次由自我修復補連結。
- 後續可另案：`BackfillMissingLastWeekItems`（controller 約 line 1887）的「本週已有同 Class 項目就不補」判斷，可改為 `!Any(e => e.PreviousItemId == lItem.Id)`。本計畫不動它。

---

### Task 1: 新增欄位與 migration

**Files:**
- Modify: `source/schema/Data/Content/StudentPopulationItem.cs`（`LastWeekNumber` 屬性附近，約 line 96-100）
- Create: `source/schema/Data/Migrations/<timestamp>_AddStudentPopulationItemPreviousItemId.cs` 與 `.Designer.cs`（工具產生）
- Modify: `source/schema/Data/Migrations/DataContextModelSnapshot.cs`（工具自動更新）
- Create（不進版控）: 產生的 SQL script，放 scratchpad

**Interfaces:**
- Produces: `public long? PreviousItemId { get; set; }` on `StudentPopulationItem`（Task 2、3、4 使用）

- [ ] **Step 1: 在實體新增屬性**

在 `LastWeekNumber` 屬性之後加入：

```csharp
        /// <summary>
        /// 上週對應項目識別碼（跨週固定身分，用來取得上週人數）。
        /// 刻意不建外鍵：上週項目可能被匯入重建或刪除，指向不存在的項目時視為未連結，退回 Class 配對。
        /// </summary>
        [Display(Name = "上週對應項目識別碼"), DataMember]
        public long? PreviousItemId { get; set; }
```

- [ ] **Step 2: 產生 migration（不連資料庫）**

Run（在 bash）:
```bash
cd /c/Company/PHStatistics.portal/source/schema/Data && dotnet ef migrations add AddStudentPopulationItemPreviousItemId
```
Expected: 產生 `*_AddStudentPopulationItemPreviousItemId.cs`、`.Designer.cs`，並更新 snapshot。`migrations add` 不會連線資料庫。**絕對不要執行 `dotnet ef database update`。**

- [ ] **Step 3: 檢查 migration 只有這個欄位**

Read 產生的 migration 檔。`Up` 必須只有：

```csharp
            migrationBuilder.AddColumn<long>(
                name: "PreviousItemId",
                table: "StudentPopulationItem",
                type: "bigint",
                nullable: true);
```
`Down` 只有對應的 `DropColumn`。若工具夾帶其他無關的欄位或索引變更（model 與 snapshot 既有漂移），手動刪除那些行，並確認 snapshot 只新增了 `b.Property<long?>("PreviousItemId").HasColumnType("bigint");`。

- [ ] **Step 4: 產生給 sqlcmd 套用的 script**

Run:
```bash
cd /c/Company/PHStatistics.portal/source/schema/Data && dotnet ef migrations script 20260818034449_AddCourseSourceStudentPopulationType <新migration的完整Id> -o "C:/Users/hound/AppData/Local/Temp/claude/C--Company-PHStatistics-portal/f97c4705-8a54-4f73-a366-419fb4ca859d/scratchpad/add_previous_item_id.sql"
```
Expected: script 內容只有 `ALTER TABLE [StudentPopulationItem] ADD [PreviousItemId] bigint NULL;` 與 `__EFMigrationsHistory` 的 INSERT。Read 確認。

- [ ] **Step 5: 建置**

Run: `cd /c/Company/PHStatistics.portal/source/portal && dotnet build Portal/Portal.csproj 2>&1 | grep -E " error |建置成功|錯誤"`
Expected: `建置成功`，0 個錯誤。

- [ ] **Step 6: Commit**

```bash
cd /c/Company/PHStatistics.portal && git add source/schema/Data/Content/StudentPopulationItem.cs source/schema/Data/Migrations/ && git commit -m "feat: StudentPopulationItem 新增 PreviousItemId（上週對應項目）欄位與 migration

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```
（migration 尚未套用到任何資料庫；套用在 Task 5。）

---

### Task 2: 配對核心（TDD）

**Files:**
- Modify: `source/portal/Portal/Services/Import/ImportSupport/LastWeekNumberMatcher.cs`
- Test: `source/portal/Test/Services/Import/LastWeekNumberMatcherTests.cs`

**Interfaces:**
- Consumes: `StudentPopulationItem.PreviousItemId`（Task 1）；現有 `LastWeekNumberMatcher.Apply` 與 `ApplyByClassId` 簽章不變、行為不變。
- Produces:
  - `public static void SyncFromLastWeek(IEnumerable<StudentPopulationItem> currentItems, IEnumerable<StudentPopulationItem> previousItems)`
  - `public sealed record MatchedPair(StudentPopulationItem Current, StudentPopulationItem Previous, bool Certain)`（僅內部使用，public 供測試可見性可選）

**行為規格（`SyncFromLastWeek`）：**
1. 只處理 `Class != null` 的項目；上週項目以 `Id` 建索引。
2. 本週項目依 `Id` 遞增處理：`PreviousItemId` 有值、該 Id 在上週項目中存在、且尚未被別的本週項目認領 → `LastWeekNumber = 上週該項目.Number`，標記為已認領。
3. 其餘本週項目（無連結、連結失效、重複認領）先把 `PreviousItemId` 設為 `null`，再與「上週尚未被認領的項目」依 `Class.Id` 分組，用既有邏輯配對（備註逐字相同優先，再依 Id 順序；上週不夠配的多出項目 `LastWeekNumber = 0`；上週完全沒有該 Class 的項目維持原值）。
4. 配對結果中 `Certain == true` 者，把 `PreviousItemId` 寫成上週項目 Id。`Certain` 定義：備註配對且該備註在本週與上週該組都恰好一筆；或依順序配對但該組本週待配對與上週剩餘各只有 1 筆。依順序配對的多筆情況不寫連結（推測不可固化）。

- [ ] **Step 1: 寫失敗測試**

在 `LastWeekNumberMatcherTests` 類別內（`MakeItemWithClassId` 之後）加入：

```csharp
    private static StudentPopulationItem MakeLinked(long id, int classId, int number, long? previousItemId = null, string studentRemark = null) {
        var item = MakeItemWithClassId(id, classId, number, studentRemark);
        item.PreviousItemId = previousItemId;
        return item;
    }

    [Test]
    public void SyncFromLastWeek_LinkedItems_TakeOwnPreviousNumberEvenWhenOrderAndRemarksCannotTell() {
        // 備註空白、本週 Id 順序與上週相反：只有連結能配對正確。
        var prev = new List<StudentPopulationItem> {
            MakeLinked(1, 1967, 6), MakeLinked(2, 1967, 3), MakeLinked(3, 1967, 4),
        };
        var cur = new List<StudentPopulationItem> {
            MakeLinked(101, 1967, 0, previousItemId: 3),
            MakeLinked(102, 1967, 0, previousItemId: 1),
            MakeLinked(103, 1967, 0, previousItemId: 2),
        };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(4));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(6));
        Assert.That(cur[2].LastWeekNumber, Is.EqualTo(3));
    }

    [Test]
    public void SyncFromLastWeek_LinkBeatsRemarkMatch() {
        var prev = new List<StudentPopulationItem> {
            MakeLinked(1, 5, 10, studentRemark: "甲"), MakeLinked(2, 5, 20, studentRemark: "乙"),
        };
        // 備註寫「甲」，但連結明確指向上週的 Id 2。
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 5, 0, previousItemId: 2, studentRemark: "甲") };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(20));
    }

    [Test]
    public void SyncFromLastWeek_DanglingLink_FallsBackToClassPairingAndClearsStaleLink() {
        // 上週人數表被重新匯入，舊的上週 Item(Id 999) 已不存在。
        var prev = new List<StudentPopulationItem> { MakeLinked(50, 5, 8) };
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 5, 0, previousItemId: 999) };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(8));
        Assert.That(cur[0].PreviousItemId, Is.EqualTo(50), "1:1 配對確定，應改連到新的上週項目");
    }

    [Test]
    public void SyncFromLastWeek_UnlinkedOneToOne_PersistsLink() {
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 5, 8) };
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 5, 0) };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(8));
        Assert.That(cur[0].PreviousItemId, Is.EqualTo(1));
    }

    [Test]
    public void SyncFromLastWeek_UnlinkedAmbiguousOrderPairing_SetsNumberButDoesNotPersistLink() {
        // 同 Class 多筆、備註空白：依 Id 順序配對只是推測，不可固化成連結。
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 5, 6), MakeLinked(2, 5, 3) };
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 5, 0), MakeLinked(102, 5, 0) };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(6));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(3));
        Assert.That(cur[0].PreviousItemId, Is.Null);
        Assert.That(cur[1].PreviousItemId, Is.Null);
    }

    [Test]
    public void SyncFromLastWeek_TwoItemsLinkedToSamePrevious_SecondFallsBack() {
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 5, 6), MakeLinked(2, 5, 3) };
        var cur = new List<StudentPopulationItem> {
            MakeLinked(101, 5, 0, previousItemId: 1),
            MakeLinked(102, 5, 0, previousItemId: 1), // 重複認領
        };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(6));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(3), "第二筆退回配對，取上週剩下的那筆");
    }

    [Test]
    public void SyncFromLastWeek_AdminEditedLastWeekNumber_IsPickedUp() {
        var previous = MakeLinked(1, 5, 6);
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 5, 0, previousItemId: 1) };
        LastWeekNumberMatcher.SyncFromLastWeek(cur, new List<StudentPopulationItem> { previous });
        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(6));

        previous.Number = 9; // 管理員事後修改上週人數
        LastWeekNumberMatcher.SyncFromLastWeek(cur, new List<StudentPopulationItem> { previous });

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(9));
    }

    [Test]
    public void SyncFromLastWeek_ExtraNewSameNameClassThisWeek_GetsZero() {
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 5, 8) };
        var cur = new List<StudentPopulationItem> {
            MakeLinked(101, 5, 0, previousItemId: 1),
            MakeLinked(102, 5, 0), // 本週才新增的同名班級，上週沒有
        };

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(8));
        Assert.That(cur[1].LastWeekNumber, Is.EqualTo(0));
        Assert.That(cur[1].PreviousItemId, Is.Null);
    }

    [Test]
    public void SyncFromLastWeek_ClassMissingLastWeek_LeavesValueUntouched() {
        var prev = new List<StudentPopulationItem> { MakeLinked(1, 5, 8) };
        var cur = new List<StudentPopulationItem> { MakeLinked(101, 99, 0) };
        cur[0].LastWeekNumber = 7;

        LastWeekNumberMatcher.SyncFromLastWeek(cur, prev);

        Assert.That(cur[0].LastWeekNumber, Is.EqualTo(7));
    }
```

- [ ] **Step 2: 執行測試確認失敗**

Run: `cd /c/Company/PHStatistics.portal/source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~LastWeekNumberMatcherTests" 2>&1 | grep -E " error |失敗|已通過!"`
Expected: 編譯失敗，`'LastWeekNumberMatcher' 未包含 'SyncFromLastWeek' 的定義`（方法尚不存在）。

- [ ] **Step 3: 實作**

將 `LastWeekNumberMatcher.cs` 的 `ApplyByClassId`／`PairGroup` 區段改成下列內容（`Apply` 內呼叫 `PairGroup` 的地方不變，回傳值直接丟棄）：

```csharp
    public sealed record MatchedPair(StudentPopulationItem Current, StudentPopulationItem Previous, bool Certain);

    // 人數表開頁時同步上週人數用（同 Class 底下可能有多筆 Item，不能一律取上週第一筆）。
    // 同 Class.Id 分組內先比備註、再依 Id 順序配對；上週完全沒有該 Class 的項目維持原值，
    // 同組內上週人數不足以配對的多出項目設為 0。
    public static void ApplyByClassId(IEnumerable<StudentPopulationItem> currentItems, IEnumerable<StudentPopulationItem> previousItems) {
        PairByClassId(currentItems, previousItems);
    }

    // 以 Item.PreviousItemId 為主的同步：有連結且上週項目還在者直接取值；其餘退回 Class 配對，
    // 並且只把「確定」的配對寫回 PreviousItemId（備註唯一相符，或該組上下週各只有一筆）。
    public static void SyncFromLastWeek(IEnumerable<StudentPopulationItem> currentItems, IEnumerable<StudentPopulationItem> previousItems) {
        var prevList = previousItems.Where(i => i.Class != null).ToList();
        var prevById = prevList.ToDictionary(i => i.Id);
        var claimed = new HashSet<long>();
        var unlinked = new List<StudentPopulationItem>();

        foreach (var cur in currentItems.Where(i => i.Class != null).OrderBy(i => i.Id)) {
            if (cur.PreviousItemId.HasValue
                && prevById.TryGetValue(cur.PreviousItemId.Value, out var linked)
                && claimed.Add(linked.Id)) {
                cur.LastWeekNumber = linked.Number;
            }
            else {
                cur.PreviousItemId = null; // 連結失效或重複認領，先清掉，配對確定再寫回
                unlinked.Add(cur);
            }
        }

        var remainingPrev = prevList.Where(p => !claimed.Contains(p.Id)).ToList();
        foreach (var pair in PairByClassId(unlinked, remainingPrev)) {
            if (pair.Certain) pair.Current.PreviousItemId = pair.Previous.Id;
        }
    }

    private static List<MatchedPair> PairByClassId(IEnumerable<StudentPopulationItem> currentItems, IEnumerable<StudentPopulationItem> previousItems) {
        var pairs = new List<MatchedPair>();
        var prevGroups = previousItems
            .Where(i => i.Class != null)
            .GroupBy(i => i.Class.Id)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Id).ToList());

        foreach (var grp in currentItems.Where(i => i.Class != null).GroupBy(i => i.Class.Id)) {
            if (!prevGroups.TryGetValue(grp.Key, out var prevList)) continue;
            pairs.AddRange(PairGroup(grp.OrderBy(i => i.Id).ToList(), prevList));
        }
        return pairs;
    }

    private static List<MatchedPair> PairGroup(List<StudentPopulationItem> current, List<StudentPopulationItem> previous) {
        var pairs = new List<MatchedPair>();
        var available = new List<StudentPopulationItem>(previous);
        var unmatched = new List<StudentPopulationItem>();

        foreach (var cur in current) {
            StudentPopulationItem match = null;
            if (!string.IsNullOrWhiteSpace(cur.StudentRemark)) {
                match = available.FirstOrDefault(p => p.StudentRemark == cur.StudentRemark);
            }
            if (match != null) {
                bool unique = current.Count(c => c.StudentRemark == cur.StudentRemark) == 1
                    && previous.Count(p => p.StudentRemark == cur.StudentRemark) == 1;
                cur.LastWeekNumber = match.Number;
                available.Remove(match);
                pairs.Add(new MatchedPair(cur, match, unique));
            }
            else {
                unmatched.Add(cur);
            }
        }

        bool oneToOne = current.Count == 1 && previous.Count == 1;
        for (int i = 0; i < unmatched.Count; i++) {
            if (i < available.Count) {
                unmatched[i].LastWeekNumber = available[i].Number;
                pairs.Add(new MatchedPair(unmatched[i], available[i], oneToOne));
            }
            else {
                unmatched[i].LastWeekNumber = 0;
            }
        }
        return pairs;
    }
```

同時確認 `Apply` 內原本呼叫 `PairGroup(grp.OrderBy(i => i.Id).ToList(), prevList);` 的那行仍可編譯（回傳值被忽略即可）。

- [ ] **Step 4: 執行測試確認通過**

Run: `cd /c/Company/PHStatistics.portal/source/portal && dotnet test Test/Test.csproj --filter "FullyQualifiedName~LastWeekNumberMatcherTests" 2>&1 | grep -E " error |失敗|已通過!"`
Expected: `已通過!`，先前 11 個測試加新增 8 個共 19 個全過，0 失敗。

- [ ] **Step 5: 跑完整測試**

Run: `cd /c/Company/PHStatistics.portal/source/portal && dotnet test Test/Test.csproj 2>&1 | grep -E " error |失敗|已通過!"`
Expected: 只剩既有的 2 個 `FindSchoolBlocks_*` 失敗（讀不到 `C:\Leo\...xlsx`，與本計畫無關）。其他失敗都要處理。

- [ ] **Step 6: Commit**

```bash
cd /c/Company/PHStatistics.portal && git add source/portal/Portal/Services/Import/ImportSupport/LastWeekNumberMatcher.cs && git add -f source/portal/Test/Services/Import/LastWeekNumberMatcherTests.cs && git commit -m "feat: LastWeekNumberMatcher 新增以 PreviousItemId 為主的 SyncFromLastWeek

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 3: 建新表複製上週項目時寫入連結

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`（7 處，見下）

**Interfaces:**
- Consumes: `StudentPopulationItem.PreviousItemId`（Task 1）

7 處都是「從 `lItem`（上週項目）建立本週 `item`」的迴圈，且都已有 `item.LastWeekNumber = lItem.Number;` 這一行：CreatePopulation 的 PH／GEPT／PS／PSJ／AS 五個 action 各一處（約 line 313、438、553、671、795、916），加上 `BackfillMissingLastWeekItems`（約 line 1901）。合計 7 處。**合計項目（IsSum，`LastWeekSumNumber`／`lastWeekData...FirstOrDefault(course...)` 那幾行）不寫連結**，那些由 `AggregationEngine` 重算。

- [ ] **Step 1: 確認要改的位置**

Run: `cd /c/Company/PHStatistics.portal/source/portal/Portal/Controllers && grep -n "item.LastWeekNumber = lItem.Number;" StudentPopulationController.cs`
Expected: 7 行。

- [ ] **Step 2: 每處補上一行**

在每個 `item.LastWeekNumber = lItem.Number;` 後面新增：

```csharp
                                item.PreviousItemId = lItem.Id;
```
（縮排跟該行一致。）可用：
```bash
perl -pi -e 's/^(\s*)item\.LastWeekNumber = lItem\.Number;\r?$/$1item.LastWeekNumber = lItem.Number;\n$1item.PreviousItemId = lItem.Id;/' StudentPopulationController.cs
```
注意檔案可能是 CRLF；若 perl 破壞換行，改用 Edit 逐處修改。

- [ ] **Step 3: 驗證 7 處都已加**

Run: `grep -c "item.PreviousItemId = lItem.Id;" StudentPopulationController.cs`
Expected: `7`。

- [ ] **Step 4: 建置**

Run: `cd /c/Company/PHStatistics.portal/source/portal && dotnet build Portal/Portal.csproj 2>&1 | grep -E " error |建置成功|錯誤"`
Expected: `建置成功`，0 個錯誤。

- [ ] **Step 5: Commit**

```bash
cd /c/Company/PHStatistics.portal && git add source/portal/Portal/Controllers/StudentPopulationController.cs && git commit -m "feat: 建新表複製上週項目時記錄 PreviousItemId

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 4: 開頁同步改用 SyncFromLastWeek

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`（6 處 `LastWeekNumberMatcher.ApplyByClassId(returnData.Items, lastWeekData.Items);`）
- Modify: `source/portal/Portal/Controllers/HomeController.cs`（`FixLastWeekData`，約 line 3447）

**Interfaces:**
- Consumes: `LastWeekNumberMatcher.SyncFromLastWeek`（Task 2）、`PreviousItemId`（Task 1）

前提檢查：開頁同步的 `lastWeekData` 已 `Include("Items.Class.Course...")`，`returnData` 已 `Include("Items.Class.Course")`，`SyncFromLastWeek` 需要 `Class` 已載入——現況滿足。`dataContext.SaveChanges()` 會一併寫入 `PreviousItemId` 的變動。

- [ ] **Step 1: 替換 6 處開頁同步**

Run:
```bash
cd /c/Company/PHStatistics.portal/source/portal/Portal/Controllers && perl -pi -e 's/LastWeekNumberMatcher\.ApplyByClassId\(returnData\.Items, lastWeekData\.Items\);/LastWeekNumberMatcher.SyncFromLastWeek(returnData.Items, lastWeekData.Items);/' StudentPopulationController.cs && grep -c "SyncFromLastWeek(returnData.Items" StudentPopulationController.cs && grep -c "ApplyByClassId" StudentPopulationController.cs
```
Expected: 第一個 `6`，第二個 `0`。

- [ ] **Step 2: 更新 6 處上方的註解**

每處原註解 `// 同一個 Class 底下可能有多筆 Item（小組班共用 Class），不可全部覆寫成同一個上週值` 改為：

```csharp
                    // 以 PreviousItemId 取上週對應項目；沒有連結的才退回 Class 配對（同 Class 多筆時不可全部取同一個上週值）
```
Run: `grep -c "以 PreviousItemId 取上週對應項目" StudentPopulationController.cs` Expected `6`（可用 perl 全部替換舊註解）。

- [ ] **Step 3: 修改 FixLastWeekData**

`HomeController.cs` 約 line 3447：把 `LastWeekNumberMatcher.ApplyByClassId(candidates, prevItems);` 改成 `LastWeekNumberMatcher.SyncFromLastWeek(candidates, prevItems);`。該方法後面只保留原本 `LastWeekNumber==0` 者的結果（`zeroIds`）；但 `PreviousItemId` 也會被 Sync 改動，需要一併還原非 zero 項目，避免它意外寫入連結。把還原段改成：

```csharp
                    var originalLastWeek = candidates.ToDictionary(i => i.Id, i => (i.LastWeekNumber, i.PreviousItemId));
                    LastWeekNumberMatcher.SyncFromLastWeek(candidates, prevItems);

                    int updated = 0;
                    foreach (var item in candidates) {
                        if (!zeroIds.Contains(item.Id)) {
                            (item.LastWeekNumber, item.PreviousItemId) = originalLastWeek[item.Id];
                        }
                        else if (item.LastWeekNumber != 0) {
                            updated++;
                        }
                    }
```
（把原本的 `originalLastWeek` 宣告與 `ApplyByClassId` 呼叫與 foreach 換成上面這段。）

- [ ] **Step 4: 建置與測試**

Run: `cd /c/Company/PHStatistics.portal/source/portal && dotnet test Test/Test.csproj 2>&1 | grep -E " error |失敗|已通過!"`
Expected: 0 編譯錯誤；只有既有 2 個 `FindSchoolBlocks_*` 失敗。

- [ ] **Step 5: Commit**

```bash
cd /c/Company/PHStatistics.portal && git add source/portal/Portal/Controllers/StudentPopulationController.cs source/portal/Portal/Controllers/HomeController.cs && git commit -m "feat: 開頁同步上週人數改以 PreviousItemId 為主，沒有連結才退回 Class 配對

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 5: 部署與驗證（寫入正式環境，需使用者同意）

**Files:** 無程式修改。

- [ ] **Step 1: 向使用者確認並套用 migration**

告知使用者：要對正式環境 `NewPAS` 的 `StudentPopulationItem` 新增一個可為空欄位 `PreviousItemId`（metadata-only，不改任何既有資料列），取得同意後由使用者（或經同意由我）用 sqlcmd 套用 Task 1 Step 4 產生的 script。**必須在部署新版程式之前套用**，否則新程式一讀寫該欄位就會出錯。

- [ ] **Step 2: 用獨立 SELECT 驗證欄位存在**（sqlcmd 可能靜默失敗）

```sql
SELECT COUNT(*) AS Cols FROM sys.columns WHERE object_id = OBJECT_ID('StudentPopulationItem') AND name = 'PreviousItemId';
SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
```
Expected: `Cols = 1`；最新 MigrationId 為新的 migration。

- [ ] **Step 3: 部署新版程式後的瀏覽器驗證**

1. 農十六第 13 週（P3-中階 3 個小組班共用 Class）開頁 → 上週人數應為各班自己的值，重新整理多次不變。
2. 河堤第 13 週合計列順序為「小、三」（`fc42720` 的排序修正）。
3. 任一分校新建下一週人數表 → 用下列 SQL 確認新表項目都有連結：

```sql
SELECT COUNT(*) AS Total, SUM(CASE WHEN PreviousItemId IS NULL THEN 1 ELSE 0 END) AS NoLink
FROM StudentPopulationItem WHERE StudentPopulationId = <新表Id> AND IsSum = 0;
```
Expected: `NoLink` 只會是本週手動新增的班級。

- [ ] **Step 4: 觀察自我修復**

開啟數張既有的第 13 週人數表後，查詢連結數增加，且抽查數字與桌面「上週人數校正清單」的「應為值」一致（歷史資料校正仍由使用者依 Excel 手動處理）。

- [ ] **Step 5: 回滾方案**

程式有問題：還原程式版本即可，欄位留著無害（可為空、無外鍵）。要移除欄位才執行 `ALTER TABLE StudentPopulationItem DROP COLUMN PreviousItemId;` 並刪除 `__EFMigrationsHistory` 對應那一列，兩者都需使用者同意。

---

## Self-Review

- **Spec coverage:** 欄位＋migration → Task 1；連結寫入 → Task 3；讀取與退回配對 → Task 2、4；自我修復與確定性判斷 → Task 2；部署順序與驗證 → Task 5；不建外鍵與資料不批次改寫 → Global Constraints。匯入路徑與歷史回填的不做，已在 File Structure 明確說明。
- **Placeholder scan:** 無 TBD；migration 檔名含時間戳由工具產生，已說明取得方式。
- **Type consistency:** `PreviousItemId` 一律 `long?`；`SyncFromLastWeek(IEnumerable<StudentPopulationItem>, IEnumerable<StudentPopulationItem>)` 在 Task 2 定義，Task 4 呼叫簽章一致；`MatchedPair` 只在 `LastWeekNumberMatcher` 內使用。
- **Review Focus:** 5 項皆對應 Task 2 的測試（1→測試1、5；2→測試6；3→測試7；4→測試1、5；5→測試8；失效連結→測試3）。
