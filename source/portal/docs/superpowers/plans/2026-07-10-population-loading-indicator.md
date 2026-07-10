# 人數表 Loading 提示 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 前台人數表輸入頁面（PH/GEPT/PS/PSJ/AS 共 5 種類型）在任何自動存檔/加總計算 AJAX 請求進行中，顯示全螢幕 Loading 遮罩並阻擋使用者操作，直到請求完成（成功或失敗皆同）。

**Architecture:** 新增一支共用 JS 檔 `wwwroot/js/studentPopulationLoading.js`，在頁面載入時動態插入一個全螢幕遮罩 `<div>`（預設隱藏），並綁定 jQuery 全域事件 `$(document).ajaxStart` / `$(document).ajaxStop` 來顯示/隱藏遮罩。5 個人數表輸入頁面（`CreatePopulation.cshtml`/`CreateGeptPopulation.cshtml`/`CreatePSPopulation.cshtml`/`CreatePSJPopulation.cshtml`/`CreateASPopulation.cshtml`）各自加入一行 `<script src="~/js/studentPopulationLoading.js"></script>`。因為這 5 個頁面既有的 `valueChange`/`sumValueChange`/`remarkChange`/`addNewClassItem`/`removeClassItem`/`updateClassDetail`/`confirmPopulation` 全部透過 `$.ajax(...)` 發出請求，全域事件會自動攔截，**完全不需要修改這些既有函式**。

**Tech Stack:** ASP.NET Core 8.0 MVC、Razor Views、jQuery 3.6（既有，`_Layout.cshtml` 已全站載入）、Bootstrap CSS class（`spinner-border` 等，已透過 `default.css` 全站載入，見下方 Global Constraints）。

## Global Constraints

- 對應 spec：`source/portal/docs/superpowers/specs/2026-07-10-population-loading-indicator-design.md`
- 涵蓋全部 7 個既有 AJAX 動作（`valueChange`/`sumValueChange`/`remarkChange`/`addNewClassItem`/`removeClassItem`/`updateClassDetail`/`confirmPopulation`），不區分類型，一律鎖定
- 遮罩需覆蓋全頁面（含 `#addClassModal` 新增班級 Modal），不只是 `#contentItem` 區塊
- 不修改 `_Layout.cshtml`：僅這 5 個人數表輸入頁面套用，不影響站台其他頁面的 AJAX 行為
- 不新增 debounce/節流機制、不新增最短顯示時間防閃爍機制（YAGNI，spec 已明確排除）
- **此專案沒有前端自動化測試基礎設施**（純 Razor + jQuery + inline script，無 Jest/Selenium 前端測試）。本計畫驗證方式一律採用：**編譯成功 + 啟動本機服務手動操作瀏覽器驗證**，不生產虛假的單元測試
- `default.css`（`wwwroot/Content/css/default.css`，`_Layout.cshtml` 全站無條件載入）已內建 Bootstrap 相容樣式，含 `.spinner-border`、`.modal-dialog`、`.btn-success` 等 class（已確認存在，21 處相符 selector），可直接使用 `spinner-border` class 而不需額外引入 CSS 框架

---

### Task 1: 建立共用 Loading 遮罩 JS 檔

**Files:**
- Create: `source/portal/Portal/wwwroot/js/studentPopulationLoading.js`

**Interfaces:**
- Produces: 載入此檔案後，頁面上會有一個 `id="sp-loading-overlay"` 的遮罩 `<div>`（初始 `display:none`），且 `$(document)` 已綁定 `ajaxStart`/`ajaxStop` 顯示/隱藏該遮罩。後續 Task 2-6 只需要在各頁面 `<script src="~/js/studentPopulationLoading.js"></script>` 引入此檔即可生效，不需要呼叫任何函式。

- [ ] **Step 1: 建立 JS 檔**

建立 `source/portal/Portal/wwwroot/js/studentPopulationLoading.js`：

```js
(function ($) {
    'use strict';

    var overlayId = 'sp-loading-overlay';

    function ensureOverlay() {
        if (document.getElementById(overlayId)) {
            return;
        }

        var style = document.createElement('style');
        style.textContent =
            '#' + overlayId + ' {' +
            '  position: fixed;' +
            '  inset: 0;' +
            '  z-index: 2000;' +
            '  display: none;' +
            '  align-items: center;' +
            '  justify-content: center;' +
            '  background: rgba(0, 0, 0, 0.45);' +
            '  pointer-events: auto;' +
            '}' +
            '#' + overlayId + ' .sp-loading-box {' +
            '  display: flex;' +
            '  flex-direction: column;' +
            '  align-items: center;' +
            '  gap: 12px;' +
            '  color: #fff;' +
            '  font-size: 16px;' +
            '}';
        document.head.appendChild(style);

        var overlay = document.createElement('div');
        overlay.id = overlayId;
        overlay.innerHTML =
            '<div class="sp-loading-box">' +
            '<div class="spinner-border text-light" role="status">' +
            '<span class="visually-hidden">Loading...</span>' +
            '</div>' +
            '<div>處理中，請稍候...</div>' +
            '</div>';
        document.body.appendChild(overlay);
    }

    function showOverlay() {
        ensureOverlay();
        document.getElementById(overlayId).style.display = 'flex';
    }

    function hideOverlay() {
        var overlay = document.getElementById(overlayId);
        if (overlay) {
            overlay.style.display = 'none';
        }
    }

    $(document).ready(ensureOverlay);
    $(document).ajaxStart(showOverlay);
    $(document).ajaxStop(hideOverlay);
})(jQuery);
```

- [ ] **Step 2: 靜態檢查（無建置流程可跑，純前端靜態檔）**

確認檔案儲存路徑正確：`source/portal/Portal/wwwroot/js/studentPopulationLoading.js`（`wwwroot` 下的檔案會被 ASP.NET Core 靜態檔案中介軟體直接以 `/js/studentPopulationLoading.js` 提供，不需要編譯）。

- [ ] **Step 3: Commit**

```bash
git add source/portal/Portal/wwwroot/js/studentPopulationLoading.js
git commit -m "feat: add shared loading overlay script for population pages"
```

---

### Task 2: PH 頁面（`CreatePopulation.cshtml`）套用遮罩並驗證

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml:141-144`

**Interfaces:**
- Consumes: `source/portal/Portal/wwwroot/js/studentPopulationLoading.js`（Task 1 產出，靜態檔路徑 `~/js/studentPopulationLoading.js`）

- [ ] **Step 1: 加入 script 引入**

第 141-144 行目前：
```cshtml
</section>


<script>
```

改為：
```cshtml
</section>

<script src="~/js/studentPopulationLoading.js"></script>
<script>
```

- [ ] **Step 2: 建置確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded，無錯誤

- [ ] **Step 3: 手動瀏覽器驗證**

啟動網站（`dotnet run --project source/portal/Portal/Portal.csproj`），登入具備百瀚（PH）分校存取權限的帳號，進入該類型的人數表輸入頁：
1. 修改任一「本週人數」輸入框後移開焦點（觸發 `valueChange`），確認畫面立即出現半透明遮罩＋轉圈圖示＋「處理中，請稍候...」文字，且遮罩顯示期間點擊畫面其他任何欄位/按鈕都沒有反應
2. 資料回應後遮罩自動消失，畫面上的數字/加總正確更新
3. 點擊「＋ 新增班級」開啟 Modal，選擇班系/課程/班別並送出，確認送出瞬間遮罩蓋住整個 Modal（Modal 上的輸入框/按鈕也點不到），完成後遮罩消失、Modal 關閉、新班級出現在列表中
4. 修改學生備註欄位（觸發 `remarkChange`），確認同樣出現遮罩並在完成後消失、輸入框邊框依成功/失敗變綠/變紅
5. 中斷網路連線或暫停後端（例如關閉本機服務）後再次修改人數欄位，確認 `error` callback 觸發後遮罩仍會自動消失（不會卡住畫面），且原有的錯誤提示行為不受影響

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Views/StudentPopulation/CreatePopulation.cshtml
git commit -m "feat: show loading overlay during PH population page AJAX requests"
```

---

### Task 3: GEPT 頁面（`CreateGeptPopulation.cshtml`）套用遮罩並驗證

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/CreateGeptPopulation.cshtml:183-186`

**Interfaces:**
- Consumes: `source/portal/Portal/wwwroot/js/studentPopulationLoading.js`（Task 1 產出）

- [ ] **Step 1: 加入 script 引入**

第 183-186 行目前：
```cshtml
</section>


<script>
```

改為：
```cshtml
</section>

<script src="~/js/studentPopulationLoading.js"></script>
<script>
```

- [ ] **Step 2: 建置確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded，無錯誤

- [ ] **Step 3: 手動瀏覽器驗證**

比照 Task 2 Step 3 的驗證流程，在英檢（GEPT）人數表輸入頁重複執行（此類型無 `sumValueChange`，其餘動作皆需驗證：`valueChange`/`remarkChange`/`addNewClassItem`/`removeClassItem`/`updateClassDetail`/`confirmPopulation`）。

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Views/StudentPopulation/CreateGeptPopulation.cshtml
git commit -m "feat: show loading overlay during GEPT population page AJAX requests"
```

---

### Task 4: PS 頁面（`CreatePSPopulation.cshtml`）套用遮罩並驗證

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePSPopulation.cshtml:183-186`

**Interfaces:**
- Consumes: `source/portal/Portal/wwwroot/js/studentPopulationLoading.js`（Task 1 產出）

- [ ] **Step 1: 加入 script 引入**

第 183-186 行目前：
```cshtml
</section>


<script>
```

改為：
```cshtml
</section>

<script src="~/js/studentPopulationLoading.js"></script>
<script>
```

- [ ] **Step 2: 建置確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded，無錯誤

- [ ] **Step 3: 手動瀏覽器驗證**

比照 Task 2 Step 3 的驗證流程，在百世（PS）人數表輸入頁重複執行。

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Views/StudentPopulation/CreatePSPopulation.cshtml
git commit -m "feat: show loading overlay during PS population page AJAX requests"
```

---

### Task 5: PSJ 頁面（`CreatePSJPopulation.cshtml`）套用遮罩並驗證

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/CreatePSJPopulation.cshtml:132-135`

**Interfaces:**
- Consumes: `source/portal/Portal/wwwroot/js/studentPopulationLoading.js`（Task 1 產出）

- [ ] **Step 1: 加入 script 引入**

第 132-135 行目前：
```cshtml
</section>


<script>
```

改為：
```cshtml
</section>

<script src="~/js/studentPopulationLoading.js"></script>
<script>
```

- [ ] **Step 2: 建置確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded，無錯誤

- [ ] **Step 3: 手動瀏覽器驗證**

比照 Task 2 Step 3 的驗證流程，在百倍速（PSJ）人數表輸入頁重複執行。

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Views/StudentPopulation/CreatePSJPopulation.cshtml
git commit -m "feat: show loading overlay during PSJ population page AJAX requests"
```

---

### Task 6: AS 頁面（`CreateASPopulation.cshtml`）套用遮罩並驗證

**Files:**
- Modify: `source/portal/Portal/Views/StudentPopulation/CreateASPopulation.cshtml:183-186`

**Interfaces:**
- Consumes: `source/portal/Portal/wwwroot/js/studentPopulationLoading.js`（Task 1 產出）

- [ ] **Step 1: 加入 script 引入**

第 183-186 行目前：
```cshtml
</section>


<script>
```

改為：
```cshtml
</section>

<script src="~/js/studentPopulationLoading.js"></script>
<script>
```

- [ ] **Step 2: 建置確認**

Run: `dotnet build source/portal/PHStatistics.portal.sln`
Expected: Build succeeded，無錯誤

- [ ] **Step 3: 手動瀏覽器驗證**

比照 Task 2 Step 3 的驗證流程，在課輔安親（AS）人數表輸入頁重複執行。額外注意：AS 的 `#contentItem` 初始載入用專用的 `ASPopulationPartialView.cshtml`（見既有 memory 記錄，班級名稱欄位曾經綁錯欄位），確認本次改動不影響該 partial 的欄位綁定，僅新增遮罩效果。

- [ ] **Step 4: Commit**

```bash
git add source/portal/Portal/Views/StudentPopulation/CreateASPopulation.cshtml
git commit -m "feat: show loading overlay during AS population page AJAX requests"
```
