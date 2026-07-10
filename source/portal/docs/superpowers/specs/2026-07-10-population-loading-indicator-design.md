# 人數表自動存檔/加總計算 Loading 提示 設計文件

**日期**：2026-07-10
**狀態**：設計確認，待撰寫實作計畫
**範圍**：前台人數表輸入頁面（PH/GEPT/PS/PSJ/AS 共 5 種類型）

---

## 背景

前台人數表輸入頁面（`CreatePopulation.cshtml` / `CreateGeptPopulation.cshtml` / `CreatePSPopulation.cshtml` / `CreatePSJPopulation.cshtml` / `CreateASPopulation.cshtml`）中，每次使用者異動資料都會觸發一次 AJAX 請求到伺服器：

| 前端函式 | 對應 Action | 觸發時機 |
|---|---|---|
| `valueChange` | `UpdateClassItem` | 本週人數輸入框改值 |
| `sumValueChange`（僅 PH 頁面有） | `UpdateSumClassItem` | 合計欄手動改值 |
| `remarkChange` | `UpdateRemark` | 學生備註改值 |
| `addNewClassItem` | `AddNewClass` | 新增班級 |
| `removeClassItem` | `RemoveClassItem` | 刪除班級 |
| `updateClassDetail` | `UpdateClassDetail` | 班級名稱/班別改值 |
| `confirmPopulation` | `ConfirmPopulation` | 確認送出人數表 |

其中前 6 者的伺服器端都會呼叫 `SumPHPopulation` 重新計算 IsSum 加總欄位，且 `valueChange`/`remarkChange`/`addNewClassItem`/`removeClassItem` 的 success callback 會用回傳的 HTML 整段取代 `#contentItem`（即整個班級列表＋加總表格區塊）。

**問題**：目前這些請求進行中沒有任何視覺提示，使用者感受到延遲卻不知道系統正在處理，因而容易在回應返回前又觸發下一次改值/新增/刪除等操作，造成請求疊加、增加伺服器負載，也可能讓 `#contentItem` 被舊回應的 HTML 覆蓋掉使用者剛做的新操作（race condition）。

## 目標

在上述任一 AJAX 請求進行中，顯示 Loading 提示並讓使用者無法進行任何操作，直到請求完成（無論成功或失敗）才解除。

## 範圍確認（已與使用者確認）

1. **涵蓋動作**：全部 7 個 AJAX 動作皆納入（不只是數字/加總相關，含備註、新增/刪除班級、班級名稱/班別、確認送出）。因為多數動作最終都會整段刷新 `#contentItem`，混合鎖定/不鎖定會讓體驗不一致。
2. **覆蓋範圍**：全螢幕遮罩（含新增班級 Modal 在內都要被蓋住/鎖定），而非只蓋 `#contentItem` 區塊。
3. **實作方式**：抽成共用 JS 檔，5 個頁面各自引入，不在 5 個檔案中各自複製一份 Loading 邏輯。
4. **不修改** `_Layout.cshtml`：只在這 5 個人數表輸入頁面套用，不影響站台其他頁面的 AJAX 行為。

---

## 技術設計

### 核心機制：jQuery 全域 AJAX 事件

5 個頁面裡所有請求都是透過 jQuery 的 `$.ajax(...)` 發出。不需要逐一修改 30 多處 `$.ajax` 呼叫（加 `beforeSend`/`complete`），改用 jQuery 內建的全域事件：

- `$(document).ajaxStart(fn)`：目前沒有任何進行中的請求、且有新請求開始時觸發一次（多個請求同時進行只觸發一次）。
- `$(document).ajaxStop(fn)`：所有進行中的請求都結束時觸發一次（無論 success 或 error 都算結束）。

這兩個事件天生就處理好「多個請求同時發生」的情境：只要還有任何一個請求在跑，遮罩就不會消失；不需要額外計數器或旗標。

### 新增檔案：`wwwroot/js/studentPopulationLoading.js`

職責：
1. 頁面載入時，動態插入一個一開始隱藏的全螢幕遮罩 `<div>`（fixed 定位、鋪滿視窗、半透明深色背景、置中 Bootstrap `spinner-border` + 文字「處理中，請稍候...」）。
2. 遮罩 CSS：
   - `position: fixed; inset: 0;`
   - `z-index`：高於既有 Bootstrap modal（`#addClassModal`）與其他既有元素，確保新增班級 Modal 開著時發送請求，遮罩仍蓋在最上層
   - `pointer-events: auto`（遮罩本身接住所有點擊，不往下傳遞，達成「不能進行操作」）
   - 預設 `display: none`
3. 綁定：
   ```js
   $(document).ajaxStart(function () { /* show overlay */ });
   $(document).ajaxStop(function () { /* hide overlay */ });
   ```

不需要修改任何既有的 `valueChange`/`sumValueChange`/`remarkChange`/`addNewClassItem`/`removeClassItem`/`updateClassDetail`/`confirmPopulation` 函式本體——它們的 `$.ajax` 呼叫會被全域事件自動攔截。

### 視圖變更

在以下 5 個檔案各自加入一行 `<script src="~/js/studentPopulationLoading.js"></script>`（放在既有 inline `<script>` 區塊之前或之後皆可，只要在 jQuery 載入之後）：

- `Views/StudentPopulation/CreatePopulation.cshtml`
- `Views/StudentPopulation/CreateGeptPopulation.cshtml`
- `Views/StudentPopulation/CreatePSPopulation.cshtml`
- `Views/StudentPopulation/CreatePSJPopulation.cshtml`
- `Views/StudentPopulation/CreateASPopulation.cshtml`

### 錯誤處理

`ajaxStop` 不論請求成功或失敗都會觸發，因此遮罩一定會被解除，不會有「請求失敗導致畫面永遠鎖住」的風險；既有的 `error` callback（`alert(...)`、邊框變色等）行為不變，只是在 alert 跳出前遮罩已經先解除。

### 不在範圍內

- 不新增 debounce/節流機制（使用者本來就是靠遮罩物理阻擋，不需要額外節流）
- 不新增「最短顯示時間」之類的防閃爍機制（使用者本來就反映有感延遲，不會太快閃過）
- 不影響 `_Layout.cshtml` 或其他非人數表頁面的 AJAX 行為

---

## 測試方式

專案沒有前端自動化測試（見既有 memory 記錄），比照既有慣例以 `dotnet build` + 手動操作驗證：
1. 開啟任一類型的人數表輸入頁，改動人數欄位，確認送出瞬間畫面立即出現遮罩＋無法點擊其他欄位，資料回來後遮罩消失且畫面正確更新。
2. 開啟「新增班級」Modal 並送出，確認遮罩蓋住 Modal 本身、無法重複點擊新增。
3. 刻意讓後端出錯（或斷網）觸發 `error` callback，確認遮罩仍會解除、既有錯誤提示仍正常顯示。
