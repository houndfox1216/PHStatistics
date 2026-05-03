# UAT 第一波優化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 改善首頁介面、輸入體驗（標色/Modal/摘要/inline 編輯）、後台匯出功能，並新增 PH 班級明細匯出格式。

**Architecture:** 純 ASP.NET Core 8.0 MVC 層修改，分為前端（Razor View + jQuery）與後端（Controller Action + Service）兩個面向。`ExportPHDetail` 為 `ReportExportService` 的新方法，以 NPOI 動態計算子欄位寬度並產生 Excel。後台 ExportReport 共用現有 `ReportExportService.Export`，不複製邏輯。

**Tech Stack:** ASP.NET Core 8.0 MVC、Bootstrap 5、jQuery、NPOI（XSSFWorkbook）、Entity Framework Core 8.0、DevExtreme（後台 DataGrid）

---

## File Map

| 檔案 | 變更 |
|------|------|
| `source/portal/Portal/wwwroot/css/site.css` | 新增首頁卡片樣式、班級列狀態顏色 |
| `source/portal/Portal/Views/Home/Index.cshtml` | 重構為 3 欄圖示卡片版面 |
| `source/portal/Portal/Views/StudentPopulation/PopulationPartialView.cshtml` | 加 row 狀態 CSS class、`class-name-input` class、inline 編輯 onblur/onchange |
| `source/portal/Portal/Views/StudentPopulation/ASPopulationPartialView.cshtml` | 加 row 狀態 CSS class、`class-name-input` class |
| `source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml` | 移除內聯新增列、加 Modal、加摘要 div 與 JS |
| `source/portal/Portal/Views/StudentPopulation/CreatePSJPopulation.cshtml` | 同上 |
| `source/portal/Portal/Views/StudentPopulation/CreateGeptPopulation.cshtml` | 同上（無班別欄位） |
| `source/portal/Portal/Views/StudentPopulation/CreatePSPopulation.cshtml` | 同上（無班別欄位） |
| `source/portal/Portal/Views/StudentPopulation/CreateASPopulation.cshtml` | 同上（無班別欄位） |
| `source/portal/Portal/Controllers/StudentPopulationController.cs` | 新增 `UpdateClassDetail`、`ExportReportDetail` |
| `source/portal/Portal/Views/StudentPopulation/Query.cshtml` | 新增「匯出 PH 明細」按鈕 |
| `source/portal/Portal/Services/ReportExportService.cs` | 新增 `ExportPHDetail` |
| `source/portal/Portal/Areas/Admin/Controllers/StudentPopulationController.cs` | 新增 `ExportReport`、`ExportReportDetail` |
| `source/portal/Portal/Areas/Admin/Views/StudentPopulation/Index.cshtml` | 新增報表匯出篩選區與按鈕 |

---

## Task 1: CSS 樣式基礎

**Files:**
- Modify: `source/portal/Portal/wwwroot/css/site.css`

- [ ] **Step 1: 在 site.css 末尾加入以下樣式**

```css
/* ===== Home Page Cards ===== */
.input-card {
    display: block;
    border: none;
    border-radius: 12px;
    color: #fff;
    background-color: #4e73df;
    text-decoration: none;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
}
.input-card:hover {
    transform: translateY(-3px);
    box-shadow: 0 6px 16px rgba(78, 115, 223, 0.35);
    color: #fff;
}
.input-card.disabled {
    background-color: #adb5bd;
    pointer-events: none;
    cursor: not-allowed;
}
.utility-card {
    display: block;
    border: none;
    border-radius: 12px;
    text-decoration: none;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
}
.utility-card:hover { transform: translateY(-3px); }
.utility-card-green { background-color: #1cc88a; color: #fff; }
.utility-card-green:hover { color: #fff; box-shadow: 0 6px 16px rgba(28,200,138,0.35); }
.utility-card-grey  { background-color: #858796; color: #fff; }
.utility-card-grey:hover  { color: #fff; box-shadow: 0 6px 16px rgba(133,135,150,0.35); }

/* ===== Population Row States ===== */
.row-zero      { background-color: #f8d7da !important; }
.row-new       { background-color: #d4edda !important; }
.row-unchanged { background-color: #fff3cd !important; }
```

- [ ] **Step 2: 啟動專案確認 CSS 無 syntax error**

```
dotnet run --project source/portal/Portal/Portal.csproj
```

開啟 http://localhost:5000，DevTools → Network → `site.css`，確認 200 OK 且無 parse error。

- [ ] **Step 3: Commit**

```
git add source/portal/Portal/wwwroot/css/site.css
git commit -m "style: 新增首頁卡片與班級列狀態 CSS"
```

---

## Task 2: 首頁版面重構

**Files:**
- Modify: `source/portal/Portal/Views/Home/Index.cshtml`

- [ ] **Step 1: 將 Index.cshtml 全部內容替換為以下版面**

```razor
@{
    bool canEdit = (bool)ViewBag.CanEdit;
    bool isAdmin = (bool)ViewBag.IsAdmin;
}

<div class="container py-5">
    <div class="row justify-content-center">
        <div class="col-12 col-md-10 col-xl-8">

            <h5 class="text-center text-muted fw-normal mb-3">輸入資料</h5>
            <div class="row g-3 mb-4">
                @foreach (var item in new[] {
                    new { Label = "百瀚",        Url = "/StudentPopulation/Index?type=PH" },
                    new { Label = "百倍速",      Url = "/StudentPopulation/Index?type=PSJ" },
                    new { Label = "英檢班/其他", Url = "/StudentPopulation/Index?type=Gept" },
                    new { Label = "百世",        Url = "/StudentPopulation/Index?type=PS" },
                    new { Label = "課輔",        Url = "/StudentPopulation/Index?type=AS" },
                }) {
                    <div class="col-4">
                        <a href="@(canEdit ? item.Url : "javascript:void(0)")"
                           class="input-card @(!canEdit ? "disabled" : "")">
                            <div class="card-body text-center py-4">
                                <i class="fas fa-clipboard-list fa-2x mb-2 d-block"></i>
                                <span class="fw-semibold">@item.Label</span>
                            </div>
                        </a>
                    </div>
                }
            </div>

            <hr>

            <div class="row g-3 justify-content-center">
                <div class="col-4">
                    <a href="/StudentPopulation/Query" class="utility-card utility-card-green">
                        <div class="card-body text-center py-3">
                            <i class="fas fa-search fa-2x mb-1 d-block"></i>
                            <span class="fw-semibold">查詢資料</span>
                        </div>
                    </a>
                </div>
                @if (isAdmin) {
                    <div class="col-4">
                        <a href="/Admin" class="utility-card utility-card-grey">
                            <div class="card-body text-center py-3">
                                <i class="fas fa-cog fa-2x mb-1 d-block"></i>
                                <span class="fw-semibold">管理後台</span>
                            </div>
                        </a>
                    </div>
                }
            </div>

        </div>
    </div>
</div>
```

注意：Razor `@foreach` 中 `new { ... }` 的匿名型別陣列在 C# 中需要相同型別。若 Razor 報錯，改為：

```razor
@{
    var inputItems = new (string Label, string Url)[] {
        ("百瀚",        "/StudentPopulation/Index?type=PH"),
        ("百倍速",      "/StudentPopulation/Index?type=PSJ"),
        ("英檢班/其他", "/StudentPopulation/Index?type=Gept"),
        ("百世",        "/StudentPopulation/Index?type=PS"),
        ("課輔",        "/StudentPopulation/Index?type=AS"),
    };
}
@foreach (var item in inputItems) { ... }
```

- [ ] **Step 2: dotnet build 確認無錯誤**

```
dotnet build source/portal/PHStatistics.portal.sln
```

預期：`Build succeeded. 0 Error(s)`

- [ ] **Step 3: 手動驗證**

重整首頁，確認：
1. 5 個輸入卡片以 3 欄排列（第一排 3 個，第二排 2 個）
2. `canEdit = false` 時卡片呈灰色，`pointer-events: none`
3. `isAdmin = false` 的帳號看不到管理後台卡片
4. 點擊有效卡片正確導向各 `/StudentPopulation/Index?type=XX`

- [ ] **Step 4: Commit**

```
git add source/portal/Portal/Views/Home/Index.cshtml
git commit -m "feat: 首頁重構為 3 欄圖示卡片版面"
```

---

## Task 3: 班級列視覺標色

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/PopulationPartialView.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/ASPopulationPartialView.cshtml`

`PopulationPartialView.cshtml` 同時服務 PH 和 PSJ；`ASPopulationPartialView.cshtml` 服務 AS。GEPT 和 PS 使用哪個 partial，請在各自 Create view 確認並做同樣修改。

- [ ] **Step 1: 在 PopulationPartialView.cshtml 每個班級列的 `<div class="row mx-0">` 加入狀態 class**

找到每個 `sItem`（`StudentPopulationItem`）對應的外層 `<div class="row mx-0">`，在其前方加入 Razor 判斷：

```razor
@{
    string rowStateClass;
    if (sItem.Number == 0)
        rowStateClass = "row-zero";
    else if (sItem.IsNew)
        rowStateClass = "row-new";
    else if (sItem.Number == sItem.LastWeekNumber)
        rowStateClass = "row-unchanged";
    else
        rowStateClass = "";
}
<div class="row mx-0 @rowStateClass">
```

同時在班級名稱的 `<input>` 加上 `class-name-input`（Task 5 JS 需要此 class 查找名稱）：

```razor
<input type="text"
       class="form-control class-name-input"
       id="class_@sItem.Class.Id.ToString()"
       value="@sItem.Name">
```

- [ ] **Step 2: 在 ASPopulationPartialView.cshtml 做相同修改**

找到 AS 班級列外層 div，加入相同的 `rowStateClass` Razor 判斷。
班級名稱 input 同樣加 `class-name-input`。

- [ ] **Step 3: 手動驗證**

進入任一分校的 PH 輸入頁：
- 承上週未改動的班級 → 淺黃背景
- 人數歸零的班級（輸入 0 後 blur）→ 淺紅背景（需等 AJAX 更新後 partial view 重新渲染）
- 新增的班級（＋後）→ 淺綠背景

- [ ] **Step 4: Commit**

```
git add source/portal/Portal/Views/StudentPopulation/PopulationPartialView.cshtml
git add source/portal/Portal/Views/StudentPopulation/ASPopulationPartialView.cshtml
git commit -m "feat: 輸入介面班級列加入狀態標色"
```

---

## Task 4: 新增班級改 Bootstrap Modal

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml`（PH，含班別）
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePSJPopulation.cshtml`（PSJ，含班別）
- Modify: `source/portal/Portal/Views/StudentPopulation/CreateGeptPopulation.cshtml`（GEPT，無班別）
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePSPopulation.cshtml`（PS，無班別）
- Modify: `source/portal/Portal/Views/StudentPopulation/CreateASPopulation.cshtml`（AS，無班別）

以 **CreatePopulation.cshtml（PH）** 為完整範例說明，其他 view 依相同步驟處理。

- [ ] **Step 1: 在 CreatePopulation.cshtml 移除內聯新增列，改為按鈕 + Modal**

找到背景色為 `#b2dd6e` 的內聯新增 div（`id="new_department"` 等欄位所在）：

```html
<div class="row mx-0" style="background-color:#b2dd6e">
    <!-- 班系 / 課程 / 班別 / 名稱 / 人數 / 備註 / + 按鈕 -->
</div>
```

將整個 div **替換**為以下按鈕 + Modal（保留原 `new_department` 的 `<option>` 內容不變）：

```html
<div class="mb-3">
    <button type="button" class="btn btn-success btn-sm btn-round"
            data-bs-toggle="modal" data-bs-target="#addClassModal">
        ＋ 新增班級
    </button>
</div>

<div class="modal fade" id="addClassModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">新增班級</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label">班系</label>
                    <select class="form-select" id="new_department">
                        @* 從原內聯列複製 <option> 內容貼入 *@
                    </select>
                </div>
                <div class="mb-3">
                    <label class="form-label">課程</label>
                    <select class="form-select" id="new_courses">
                        <option value="">請先選擇班系</option>
                    </select>
                </div>
                <div class="mb-3">
                    <label class="form-label">班別</label>
                    <select class="form-select" id="new_class_type">
                        <option value="">請先選擇課程</option>
                    </select>
                </div>
                <div class="mb-3">
                    <label class="form-label">班級名稱</label>
                    <input type="text" class="form-control" id="new_class_name" value="">
                </div>
                <div class="mb-3">
                    <label class="form-label">本週人數</label>
                    <input type="number" class="form-control" id="new_number" value="0" min="0">
                </div>
                <div class="mb-3">
                    <label class="form-label">學生備註</label>
                    <input type="text" class="form-control" id="new_studentremark" value="">
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">取消</button>
                <button type="button" class="btn btn-primary" onclick="addNewClassItem()">新增</button>
            </div>
        </div>
    </div>
</div>
```

- [ ] **Step 2: 在 CreatePopulation.cshtml 的 addNewClassItem() AJAX success 回呼加入 Modal 關閉與表單重置**

找到 `addNewClassItem()` 的 AJAX `success:` 回呼，在替換 `#contentItem` 後加入：

```javascript
success: function (result) {
    $('#contentItem').html(result);
    updateSubmitSummary();          // Task 5 實作，此處先預留呼叫
    $('#addClassModal').modal('hide');
    $('#new_class_name').val('');
    $('#new_number').val(0);
    $('#new_studentremark').val('');
},
```

- [ ] **Step 3: 對其餘 4 個 Create view 重複 Step 1–2**

- **CreatePSJPopulation.cshtml**：同 PH，保留班別欄位。
- **CreateGeptPopulation.cshtml**：移除班別 `<div class="mb-3">` 區塊（GEPT 使用 ClassType.General，無需選擇）。
- **CreatePSPopulation.cshtml**：同 GEPT，無班別欄位。
- **CreateASPopulation.cshtml**：無班別欄位，使用 `ASPopulationPartialView`。

- [ ] **Step 4: dotnet build 確認無錯誤**

```
dotnet build source/portal/PHStatistics.portal.sln
```

預期：`Build succeeded. 0 Error(s)`

- [ ] **Step 5: 手動驗證**

進入任一分校的 PH 輸入頁：
1. 頁面不再有頂部內聯新增列
2. 點「＋ 新增班級」彈出 Modal
3. 選班系後課程 dropdown 自動更新（原 AJAX 邏輯不變）
4. 填入資料點「新增」→ Modal 關閉 → 班級出現在列表中

- [ ] **Step 6: Commit**

```
git add source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreatePSJPopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreateGeptPopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreatePSPopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreateASPopulation.cshtml
git commit -m "feat: 新增班級改為 Bootstrap Modal 彈窗"
```

---

## Task 5: 送出前異動摘要

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml`（及其餘 4 個 Create view）

- [ ] **Step 1: 在 CreatePopulation.cshtml 的確認送出按鈕上方加入摘要 div**

找到「確認送出」按鈕的外層 div，在其正上方插入：

```html
<div id="submitSummary" class="alert alert-info py-2 mt-3 mb-1" style="display:none;font-size:14px;"></div>
```

- [ ] **Step 2: 在 CreatePopulation.cshtml 的 `<script>` 區塊頂部加入全域計數器與 updateSubmitSummary 函數**

```javascript
let deletedCount = 0;

function updateSubmitSummary() {
    const newCount  = document.querySelectorAll('#contentItem .row-new').length;
    const zeroNames = [...document.querySelectorAll('#contentItem .row-zero')]
        .map(row => {
            const inp = row.querySelector('.class-name-input');
            return inp ? inp.value.trim() : '';
        })
        .filter(n => n.length > 0);

    const parts = [];
    if (newCount    > 0) parts.push('<span class="text-success fw-bold">本週新增 ' + newCount + ' 班</span>');
    if (deletedCount > 0) parts.push('<span class="text-secondary fw-bold">已刪除 ' + deletedCount + ' 班</span>');
    if (zeroNames.length > 0)
        parts.push('<span class="text-danger fw-bold">人數歸零（下週不帶入）：' + zeroNames.join('、') + '</span>');

    const el = document.getElementById('submitSummary');
    if (parts.length > 0) {
        el.innerHTML = parts.join('&nbsp;&nbsp;');
        el.style.display = 'block';
    } else {
        el.style.display = 'none';
    }
}
```

- [ ] **Step 3: 在 removeClassItem() 的 AJAX success 回呼加入計數與摘要更新**

找到 `removeClassItem(sId)` 的 AJAX success 回呼，在更新 `#contentItem` 後加入：

```javascript
success: function (result) {
    $('#contentItem').html(result);
    deletedCount++;
    updateSubmitSummary();
},
```

- [ ] **Step 4: 確認 addNewClassItem() 與 valueChange() 的 success 回呼均已呼叫 updateSubmitSummary()**

在 Task 4 Step 2 已加入 `addNewClassItem()` 呼叫。
找到 `valueChange(id)` 的 AJAX success 回呼，確認已有或加入：

```javascript
success: function (result) {
    // 原有邏輯 ...
    updateSubmitSummary();
},
```

- [ ] **Step 5: 在 $(document).ready() 末尾呼叫一次 updateSubmitSummary()**

```javascript
$(document).ready(function () {
    // 原有初始化邏輯 ...
    updateSubmitSummary();
});
```

- [ ] **Step 6: 對其餘 4 個 Create view 重複 Step 1–5**

- [ ] **Step 7: 手動驗證**

進入輸入頁：
1. 頁面載入：若所有班級皆為 `row-unchanged`，摘要不顯示
2. 新增一個班級後：出現「本週新增 1 班」
3. 刪除一個班級後：出現「已刪除 1 班」
4. 某班人數改為 0（AJAX 更新後）：出現「人數歸零（下週不帶入）：XX班」

- [ ] **Step 8: Commit**

```
git add source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreatePSJPopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreateGeptPopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreatePSPopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreateASPopulation.cshtml
git commit -m "feat: 輸入介面加入送出前異動摘要"
```

---

## Task 6: UpdateClassDetail 後端 Action

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`

- [ ] **Step 1: 確認 ClassType enum 值**

在 `schema/Data/Content/` 中找到 `ClassType` enum 定義，確認：
- `ClassType.SubGroup` 的 int 值（預期為 4）
- `ClassType.V3` 的 int 值（預期為 3）

記下確認的值，Task 7 Step 2 的 `<option value="">` 需與此一致。

- [ ] **Step 2: 在 StudentPopulationController 加入 UpdateClassDetail Action**

在現有 `UpdateClassItem` Action 附近加入：

```csharp
[HttpPost]
public IActionResult UpdateClassDetail(long populationId, int classId, string name, int classType)
{
    var population = Model.StudentPopulation
        .Include(p => p.Items).ThenInclude(i => i.Class)
        .FirstOrDefault(p => p.Id == populationId);

    if (population == null)
        return Json(new { success = false, message = "找不到人數表" });

    if (population.Status != StudentPopulationStatus.Documented)
        return Json(new { success = false, message = "人數表狀態不允許修改" });

    var cls = Model.Class.FirstOrDefault(c => c.Id == classId);
    if (cls == null)
        return Json(new { success = false, message = "找不到班級" });

    bool classTypeChanged = (int)cls.Type != classType;

    if (!string.IsNullOrWhiteSpace(name))
        cls.Name = name;

    cls.Type = (ClassType)classType;
    Model.SaveChanges();

    if (classTypeChanged)
        SumPHPopulation(populationId);

    return Json(new { success = true });
}
```

注意：`Model.StudentPopulation`、`Model.Class`、`Model.SaveChanges()`、`SumPHPopulation()` 請參考同 Controller 中 `UpdateClassItem` 的寫法確認實際使用的 API。

- [ ] **Step 3: dotnet build 確認無錯誤**

```
dotnet build source/portal/PHStatistics.portal.sln
```

預期：`Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git commit -m "feat: 新增 UpdateClassDetail Action 支援班級名稱與班別修改"
```

---

## Task 7: Inline 編輯前端

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/PopulationPartialView.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/ASPopulationPartialView.cshtml`
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml`（加 updateClassDetail JS）

- [ ] **Step 1: 在 PopulationPartialView.cshtml 的班級名稱 input 加入 onblur**

找到 Task 3 Step 1 修改過的班級名稱 input，加入 `onblur`。

**方法 A（若 Partial View model 含 StudentPopulation）：**

```razor
<input type="text"
       class="form-control class-name-input"
       id="class_@sItem.Class.Id.ToString()"
       value="@sItem.Name"
       @(population.Status == StudentPopulationStatus.Documented
           ? (Microsoft.AspNetCore.Html.IHtmlContent)Html.Raw($"onblur=\"updateClassDetail({sItem.Class.Id}, {sItem.StudentPopulationId})\"")
           : Html.Raw("readonly"))>
```

**方法 B（若 Partial View 無法存取 population.Status，推薦此方式）：**

始終渲染 `onblur`，依賴後端 `UpdateClassDetail` 拒絕非 Documented 狀態的修改並回傳 `{ success: false }`。前端收到 false 時 `alert(result.message)`，不需在 Partial View 判斷狀態。

```razor
<input type="text"
       class="form-control class-name-input"
       id="class_@sItem.Class.Id.ToString()"
       value="@sItem.Name"
       onblur="updateClassDetail(@sItem.Class.Id, @sItem.StudentPopulationId)">
```

優先使用方法 A；若 Partial View 的 model 不含 population，使用方法 B。

- [ ] **Step 2: 在 PopulationPartialView.cshtml 的班別 select 加入 onchange 與正確 selected 狀態**

找到現有班別 `<select id="class_type_@sItem.Class.Id">`，加入 `onchange` 與 selected 狀態：

```razor
<select class="form-select"
        id="class_type_@sItem.Class.Id.ToString()"
        @(population.Status == StudentPopulationStatus.Documented
            ? $"onchange=\"updateClassDetail({sItem.Class.Id}, {sItem.StudentPopulationId})\""
            : "disabled")>
    <option value="4" @(sItem.Class.Type == ClassType.SubGroup ? "selected" : "")>小</option>
    <option value="3" @(sItem.Class.Type == ClassType.V3       ? "selected" : "")>三</option>
</select>
```

數字 `4`（SubGroup）和 `3`（V3）需與 Task 6 Step 1 確認的 enum 值一致。

- [ ] **Step 3: 在 ASPopulationPartialView.cshtml 的班級名稱 input 加入 onblur（AS 無班別欄位，跳過 select 修改）**

同 Step 1，加入 `onblur` 條件渲染。

- [ ] **Step 4: 在 CreatePopulation.cshtml 的 `<script>` 區塊加入 updateClassDetail 函數**

```javascript
function updateClassDetail(classId, populationId) {
    const name      = $('#class_' + classId).val();
    const classType = $('#class_type_' + classId).val();

    $.ajax({
        url: '/StudentPopulation/UpdateClassDetail',
        type: 'POST',
        data: { populationId: populationId, classId: classId, name: name, classType: classType },
        success: function (result) {
            if (!result.success) alert(result.message || '更新失敗');
        },
        error: function () { alert('連線錯誤，請重試'); }
    });
}
```

對其餘 4 個 Create view 也加入同一函數（或抽到共用 JS 檔，視現有架構決定）。

- [ ] **Step 5: 手動驗證**

進入 PH 輸入頁（Documented 狀態）：
1. 點擊班級名稱欄位 → 可直接輸入修改 → 失焦後 Network DevTools 可見 POST `UpdateClassDetail`
2. 修改班別下拉 → 立即呼叫後端儲存
3. Pending 以上狀態 → 班級名稱 input 為 `readonly`，班別 select 為 `disabled`

- [ ] **Step 6: Commit**

```
git add source/portal/Portal/Views/StudentPopulation/PopulationPartialView.cshtml
git add source/portal/Portal/Views/StudentPopulation/ASPopulationPartialView.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreatePSJPopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreateGeptPopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreatePSPopulation.cshtml
git add source/portal/Portal/Views/StudentPopulation/CreateASPopulation.cshtml
git commit -m "feat: 班級列加入 inline 名稱與班別編輯"
```

---

## Task 8: 後台匯出 — 後端 Action

**Files:**
- Modify: `source/portal/Portal/Areas/Admin/Controllers/StudentPopulationController.cs`

- [ ] **Step 1: 確認 Admin StudentPopulationController 已注入 ReportExportService**

在 Admin Controller 建構子中確認有 `ReportExportService` 注入。若無，加入私有欄位與建構子參數：

```csharp
private readonly ReportExportService _reportExportService;

// 在建構子參數列加入：
// ReportExportService reportExportService
// 在建構子本體加入：
// _reportExportService = reportExportService;
```

確認 `Startup.cs`（或 `Program.cs`）已有：
```csharp
services.AddScoped<ReportExportService>();
```
前台已使用此 Service，通常已存在。

- [ ] **Step 2: 加入 ExportReport Action**

```csharp
[HttpGet]
public IActionResult ExportReport(int year, int week, string reportType, bool allSchools = false, int? schoolId = null)
{
    if (!Enum.TryParse<StudentPopulationType>(reportType, true, out var type))
        return BadRequest("無效的報表類型");

    IList<int> schoolIds = schoolId.HasValue
        ? new List<int> { schoolId.Value }
        : Model.School.Select(s => s.Id).ToList();

    var bytes    = _reportExportService.Export(type, year, week, schoolIds);
    var fileName = $"PHStats_{reportType}_{year}_{week:D2}.xlsx";
    return File(bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        System.Web.HttpUtility.UrlEncode(fileName));
}
```

- [ ] **Step 3: dotnet build 確認無錯誤**

```
dotnet build source/portal/PHStatistics.portal.sln
```

預期：`Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```
git add source/portal/Portal/Areas/Admin/Controllers/StudentPopulationController.cs
git commit -m "feat: 後台新增 ExportReport Action 共用 ReportExportService"
```

---

## Task 9: 後台匯出 — UI

**Files:**
- Modify: `source/portal/Portal/Areas/Admin/Views/StudentPopulation/Index.cshtml`

- [ ] **Step 1: 在後台 Index.cshtml 篩選區按鈕群組加入報表匯出控制項**

找到現有按鈕區（含 `exportExcel()`、`exportPH()` 按鈕的 div），在其後加入：

```html
<div class="d-flex align-items-center gap-2 flex-wrap border-start ps-3 ms-2">
    <label class="mb-0 text-muted small">報表匯出</label>
    <select id="exportReportType" class="form-select form-select-sm" style="width:130px;">
        <option value="PH">百瀚 PH</option>
        <option value="GEPT">英檢 GEPT</option>
        <option value="PSJ">百倍速 PSJ</option>
        <option value="PS">百世 PS</option>
        <option value="AfterSchool">課輔 AS</option>
    </select>
    <button class="btn btn-success btn-sm" onclick="exportReport(false)">
        <i class="fas fa-file-excel me-1"></i>匯出全區
    </button>
    <button class="btn btn-info btn-sm text-white" onclick="exportReport(true)">
        <i class="fas fa-file-excel me-1"></i>匯出 PH 明細
    </button>
</div>
```

- [ ] **Step 2: 在 Index.cshtml 的 JavaScript 區塊加入 exportReport 函數**

```javascript
function exportReport(isDetail) {
    // 取現有 DevExtreme yearFilter / weekFilter 的選取值
    // 若篩選元件 ID 或取值方式不同，請對照現有 exportExcel() / exportPH() 函數的寫法調整
    const year = $('#yearFilter').dxSelectBox('instance').option('value');
    const week = $('#weekFilter').dxSelectBox('instance').option('value');
    const type = $('#exportReportType').val();

    if (!year || !week) {
        alert('請先選擇年度與週次');
        return;
    }

    const action = isDetail ? 'ExportReportDetail' : 'ExportReport';
    const url = '/Admin/StudentPopulation/' + action
        + '?year=' + year + '&week=' + week + '&reportType=' + type + '&allSchools=true';
    window.location.href = url;
}
```

注意：若現有 `exportExcel()` 或 `exportPH()` 取年/週的方式不同（例如直接讀 `<select>` value），請對照調整上面的取值邏輯。

- [ ] **Step 3: 手動驗證**

進入後台人數表列表：
1. 選擇年度、週次後，點「匯出全區」可下載 xlsx
2. 下載的格式與前台 ReportExportService.Export 輸出一致

- [ ] **Step 4: Commit**

```
git add source/portal/Portal/Areas/Admin/Views/StudentPopulation/Index.cshtml
git commit -m "feat: 後台加入報表格式匯出按鈕"
```

---

## Task 10: ExportPHDetail — Service 方法

**Files:**
- Modify: `source/portal/Portal/Services/ReportExportService.cs`

- [ ] **Step 1: 在 ReportExportService 加入 Chinese ordinal 輔助方法**

在 class 內加入私有靜態成員：

```csharp
private static readonly string[] _chineseOrdinals =
    { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };

private static string GetOrdinalLabel(int zeroBasedIndex) =>
    zeroBasedIndex < _chineseOrdinals.Length
        ? $"{_chineseOrdinals[zeroBasedIndex]}班"
        : $"第{zeroBasedIndex + 1}班";
```

- [ ] **Step 2: 加入 ExportPHDetail public 方法**

```csharp
public byte[] ExportPHDetail(int year, int week, IList<int> schoolIds = null)
{
    // 1. 載入資料（沿用現有 LoadPopulations）
    var populations = LoadPopulations(StudentPopulationType.PH, year, week, schoolIds);

    // 2. 載入所有 PH 課程，依班系 Ordinal → 課程 Ordinal 排序
    var allCourses = Model.Course
        .Include(c => c.CourseDepartment)
        .Where(c => c.Type == StudentPopulationType.PH)
        .OrderBy(c => c.CourseDepartment.Ordinal)
        .ThenBy(c => c.Ordinal)
        .ToList();

    var nonSumCourses = allCourses.Where(c => !c.IsSum).ToList();
    var isumCourses   = allCourses.Where(c =>  c.IsSum).ToList();

    var targetTypes = new[] { ClassType.SubGroup, ClassType.V3 };

    // 3. 計算各 (CourseId) 的全區最大子欄位數（SubGroup 與 V3 取最大值，統一對齊）
    var maxSubCols = new Dictionary<int, int>(); // courseId → maxCount
    foreach (var pop in populations)
    {
        var grouped = pop.Items
            .Where(i => !i.IsSum && i.Class != null && targetTypes.Contains(i.Class.Type))
            .GroupBy(i => (i.Class.CourseId, i.Class.Type));

        foreach (var g in grouped)
        {
            int courseId = g.Key.CourseId;
            int count    = g.Count();
            if (!maxSubCols.TryGetValue(courseId, out int existing) || count > existing)
                maxSubCols[courseId] = count;
        }
    }

    // 4. 建立有資料的非 IsSum 課程欄位清單
    var courseColumns = nonSumCourses
        .Where(c => maxSubCols.ContainsKey(c.Id))
        .Select(c => (course: c, maxSub: maxSubCols[c.Id]))
        .ToList();

    // 5. 欄位位移計算
    int fixedCols    = 2; // 分校 + 班型
    int totalCols    = fixedCols + courseColumns.Sum(c => c.maxSub) + isumCourses.Count;

    // 6. 建立 Workbook（參考現有 BuildSheetPH 的樣式方法名稱，若不同請調整）
    var workbook = new XSSFWorkbook();
    var sheet    = workbook.CreateSheet("Sheet1");

    // 沿用現有 helper 建立樣式；若方法名稱不同，請查 ReportExportService 中現有私有方法
    var titleStyle  = CreateCellStyle(workbook, IndexedColors.White.Index,          bold: true, fontSize: 14);
    var deptStyle   = CreateCellStyle(workbook, IndexedColors.LightYellow.Index,    bold: true);
    var courseStyle = CreateCellStyle(workbook, IndexedColors.LightYellow.Index,    bold: false);
    var subStyle    = CreateCellStyle(workbook, IndexedColors.LightYellow.Index,    bold: false);
    var isumStyle   = CreateCellStyle(workbook, IndexedColors.LightCornflowerBlue.Index, bold: true);
    var dataStyle   = CreateCellStyle(workbook, IndexedColors.White.Index,          bold: false);

    // Row 0: 標題
    {
        var r = sheet.CreateRow(0);
        var c = r.CreateCell(0);
        c.SetCellValue($"百瀚人數表（班級明細）{year} 年第 {week} 週");
        c.CellStyle = titleStyle;
        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, totalCols - 1));
    }

    // Row 1: 分校/班型（合併至 Row 3）+ 班系名稱
    {
        var r = sheet.CreateRow(1);
        SetCell(r, 0, "分校",  deptStyle);
        SetCell(r, 1, "班型",  deptStyle);
        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(1, 3, 0, 0));
        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(1, 3, 1, 1));

        int col = fixedCols;
        foreach (var dg in courseColumns.GroupBy(c => c.course.CourseDepartmentId))
        {
            int span = dg.Sum(c => c.maxSub);
            SetCell(r, col, dg.First().course.CourseDepartment?.Name ?? "", deptStyle);
            if (span > 1) sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(1, 1, col, col + span - 1));
            col += span;
        }
        foreach (var ic in isumCourses)
        {
            SetCell(r, col, ic.CourseDepartment?.Name ?? "合計", isumStyle);
            col++;
        }
    }

    // Row 2: 課程名稱（colspan = maxSub）
    {
        var r = sheet.CreateRow(2);
        int col = fixedCols;
        foreach (var (course, maxSub) in courseColumns)
        {
            SetCell(r, col, course.Name, courseStyle);
            if (maxSub > 1) sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(2, 2, col, col + maxSub - 1));
            col += maxSub;
        }
        foreach (var ic in isumCourses)
        {
            SetCell(r, col, ic.Name, isumStyle);
            col++;
        }
    }

    // Row 3: 子欄位班名（甲班、乙班...）
    {
        var r = sheet.CreateRow(3);
        int col = fixedCols;
        foreach (var (_, maxSub) in courseColumns)
        {
            for (int i = 0; i < maxSub; i++)
                SetCell(r, col + i, GetOrdinalLabel(i), subStyle);
            col += maxSub;
        }
        foreach (var _ in isumCourses)
        {
            SetCell(r, col, "合計", isumStyle);
            col++;
        }
    }

    // Row 4+: 資料（每校 2 列）
    int dataRowIdx = 4;
    foreach (var pop in populations)
    {
        var sgRow = sheet.CreateRow(dataRowIdx);
        var v3Row = sheet.CreateRow(dataRowIdx + 1);

        SetCell(sgRow, 0, pop.School?.Name ?? "", dataStyle);
        SetCell(sgRow, 1, "小班", dataStyle);
        SetCell(v3Row, 1, "三人班", dataStyle);
        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(dataRowIdx, dataRowIdx + 1, 0, 0));

        // 建立 (CourseId, ClassType) → items (依 Ordinal 排序) 的 lookup
        var lookup = pop.Items
            .Where(i => !i.IsSum && i.Class != null && targetTypes.Contains(i.Class.Type))
            .GroupBy(i => (i.Class.CourseId, i.Class.Type))
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(i => i.Class.Ordinal).ThenBy(i => i.Class.Id).ToList()
            );

        int col = fixedCols;
        foreach (var (course, maxSub) in courseColumns)
        {
            for (int pos = 0; pos < maxSub; pos++)
            {
                if (lookup.TryGetValue((course.Id, ClassType.SubGroup), out var sgList) && pos < sgList.Count && sgList[pos].Number > 0)
                    SetCell(sgRow, col + pos, sgList[pos].Number, dataStyle);
                if (lookup.TryGetValue((course.Id, ClassType.V3),       out var v3List) && pos < v3List.Count && v3List[pos].Number > 0)
                    SetCell(v3Row, col + pos, v3List[pos].Number, dataStyle);
            }
            col += maxSub;
        }

        // IsSum 欄位：呼叫 ComputeIsumValue（沿用現有邏輯）
        // ComputeIsumValue 的 signature 請查 ReportExportService 現有定義；
        // 若第三個參數為列的班型，傳入對應的 ClassType
        foreach (var ic in isumCourses)
        {
            var sgVal = ComputeIsumValue(ic, pop.Items.ToList(), ClassType.SubGroup);
            var v3Val = ComputeIsumValue(ic, pop.Items.ToList(), ClassType.V3);
            if (sgVal != 0) SetCell(sgRow, col, sgVal, dataStyle);
            if (v3Val != 0) SetCell(v3Row, col, v3Val, dataStyle);
            col++;
        }

        dataRowIdx += 2;
    }

    // 欄寬
    sheet.SetColumnWidth(0, 20 * 256);
    sheet.SetColumnWidth(1, 8  * 256);
    for (int c = fixedCols; c < totalCols; c++)
        sheet.SetColumnWidth(c, 7 * 256);

    using var ms = new System.IO.MemoryStream();
    workbook.Write(ms);
    return ms.ToArray();
}
```

注意事項：
- `CreateCellStyle`、`SetCell` 為假設的私有 helper 名稱，請對照 ReportExportService 現有樣式建立方式（可能叫 `CreateStyle`、`SetCellValue` 等），調整呼叫。若無 helper，參考 `BuildSheetPH` 的內聯 NPOI 樣式寫法直接使用。
- `ComputeIsumValue(ic, items, ClassType)` 的第三參數為該列的班型，請確認現有 signature 一致。若現有方法僅接受兩個參數，查看現有 `BuildSheetPH` 如何傳入 ClassType 並調整。
- `LoadPopulations` 的確切 signature 請參考現有呼叫點。

- [ ] **Step 3: dotnet build 確認無錯誤**

```
dotnet build source/portal/PHStatistics.portal.sln
```

預期：`Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```
git add source/portal/Portal/Services/ReportExportService.cs
git commit -m "feat: ReportExportService 新增 ExportPHDetail 班級明細格式"
```

---

## Task 11: ExportReportDetail — Controllers 與 View 按鈕

**Files:**
- Modify: `source/portal/Portal/Controllers/StudentPopulationController.cs`
- Modify: `source/portal/Portal/Areas/Admin/Controllers/StudentPopulationController.cs`
- Modify: `source/portal/Portal/Views/StudentPopulation/Query.cshtml`
- Modify: `source/portal/Portal/Areas/Admin/Views/StudentPopulation/Index.cshtml`（Task 9 已預留按鈕）

- [ ] **Step 1: 在前台 StudentPopulationController 加入 ExportReportDetail Action**

```csharp
[HttpGet]
public IActionResult ExportReportDetail(int year, int week, bool allSchools = false, int? schoolId = null)
{
    // 沿用現有 ExportReport 的權限判斷邏輯取 schoolIds
    // 請參考同 Controller 的 ExportReport Action 取 schoolIds 的寫法，複製過來：
    List<int> schoolIds;
    if (schoolId.HasValue)
    {
        // 驗證存取權限（參考現有 ExportReport 邏輯）
        schoolIds = new List<int> { schoolId.Value };
    }
    else if (allSchools /* && HasPermission("ViewAllSchools") */)
    {
        schoolIds = null; // null = 全區，由 ExportPHDetail 內 LoadPopulations 處理
    }
    else
    {
        // 僅取可存取的分校（參考現有 ExportReport 邏輯）
        schoolIds = null;
    }

    var bytes    = _reportExportService.ExportPHDetail(year, week, schoolIds);
    var fileName = $"PH明細_{year}年第{week}週.xlsx";
    return File(bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        System.Web.HttpUtility.UrlEncode(fileName));
}
```

`_reportExportService` 請確認前台 StudentPopulationController 已有此欄位（若前台 ExportReport 使用直接注入而非欄位，調整為相同方式）。

- [ ] **Step 2: 在後台 Admin StudentPopulationController 加入 ExportReportDetail Action**

```csharp
[HttpGet]
public IActionResult ExportReportDetail(int year, int week)
{
    var schoolIds = Model.School.Select(s => s.Id).ToList();
    var bytes     = _reportExportService.ExportPHDetail(year, week, schoolIds);
    var fileName  = $"PH明細_{year}年第{week}週.xlsx";
    return File(bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        System.Web.HttpUtility.UrlEncode(fileName));
}
```

- [ ] **Step 3: 在 Query.cshtml 的匯出按鈕區加入「匯出 PH 明細」按鈕**

找到現有 `#exportBtn` 和 `#exportAllBtn` 按鈕，在其後加入：

```html
<button type="button" class="btn btn-info btn-lg btn-round" id="exportDetailBtn">
    匯出 PH 明細 <i class="fas fa-file-excel"></i>
</button>
@if (ViewBag.CanViewAllSchools == true) {
    <button type="button" class="btn btn-warning btn-lg btn-round" id="exportAllDetailBtn">
        匯出全區 PH 明細 <i class="fas fa-file-excel"></i>
    </button>
}
```

注意：`ViewBag.CanViewAllSchools` 請對照現有 `#exportAllBtn` 的顯示條件（可能是 `ViewBag.CanViewAll` 或其他名稱），使用相同 ViewBag key。

在 Query.cshtml 的 JavaScript 中加入綁定（緊接現有 `$('#exportBtn')` 綁定之後），`year`、`week`、`type` 變數沿用現有定義：

```javascript
$('#exportDetailBtn').on('click', function () {
    if (typeof validateExportParams === 'function' && !validateExportParams()) return;
    var url = '@Url.Action("ExportReportDetail", "StudentPopulation", new { area = "" })'
        + '?year=' + year + '&week=' + week + '&allSchools=false';
    window.location.href = url;
});

if ($('#exportAllDetailBtn').length) {
    $('#exportAllDetailBtn').on('click', function () {
        if (typeof validateExportParams === 'function' && !validateExportParams()) return;
        var url = '@Url.Action("ExportReportDetail", "StudentPopulation", new { area = "" })'
            + '?year=' + year + '&week=' + week + '&allSchools=true';
        window.location.href = url;
    });
}
```

若現有 `$('#exportBtn')` 沒有 `validateExportParams()` 函數而是直接內聯檢查，請同樣將檢查邏輯複製至上方 onclick。

- [ ] **Step 4: 確認後台 Task 9 的「匯出 PH 明細」按鈕已正確路由**

Task 9 Step 2 的 `exportReport(true)` 產生 URL：
```
/Admin/StudentPopulation/ExportReportDetail?year=...&week=...&reportType=PH&allSchools=true
```

後台 `ExportReportDetail(int year, int week)` 目前不接受 `reportType` 參數（固定 PH），可忽略多餘參數或加入參數定義（不使用即可）。

- [ ] **Step 5: dotnet build 確認無錯誤**

```
dotnet build source/portal/PHStatistics.portal.sln
```

預期：`Build succeeded. 0 Error(s)`

- [ ] **Step 6: 手動端對端驗證 ExportPHDetail**

1. 進入查詢頁，選擇有 PH 資料的年度/週次，點「匯出 PH 明細」
2. 開啟下載的 xlsx，確認：
   - Row 0：標題列含年週資訊
   - Row 1：班系名稱，colspan 等於該班系下所有課程子欄位總和
   - Row 2：課程名稱，colspan 等於該課程全區最大班數
   - Row 3：甲班、乙班... 子欄位，數量以全區最多班為準
   - Row 4+：每校 2 列（小班/三人班），各班格填入對應人數，無資料格留空
3. 後台相同操作驗證
4. 比較兩校同課程：班數多的學校填完整，班數少的後面留空

- [ ] **Step 7: Commit**

```
git add source/portal/Portal/Controllers/StudentPopulationController.cs
git add source/portal/Portal/Areas/Admin/Controllers/StudentPopulationController.cs
git add source/portal/Portal/Views/StudentPopulation/Query.cshtml
git add source/portal/Portal/Areas/Admin/Views/StudentPopulation/Index.cshtml
git commit -m "feat: 前後台加入 PH 班級明細匯出按鈕與 Action"
```
