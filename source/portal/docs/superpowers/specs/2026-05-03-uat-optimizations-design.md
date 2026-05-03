# UAT 後優化設計文件

**日期**：2026-05-03  
**狀態**：設計確認，待實作  
**範圍**：UAT 第一波優化（反饋前預先規劃），第二波優化待 UAT 反饋後另行規劃  

---

## 背景

系統已進入 UAT 階段。本次優化針對使用者反饋前已識別的四個方向進行規劃：首頁介面美化、輸入介面體驗改善、後台匯出功能補強、PH 班級明細匯出格式。Schema 層無需異動。

---

## 需求一：首頁美化

### 目標
將首頁 7 個按鈕從單一 div 平鋪改為分組式 3 欄圖示卡片格，提升視覺層次與可讀性。

### 版面結構
- 頁面頂部顯示系統名稱
- 「輸入資料」區塊：5 個卡片，3 欄排列（百瀚、百倍速、英檢班/其他、百世、課輔）
- 分隔線後輔助功能區：查詢資料（綠色）、管理後台（灰色，限 `isAdmin` 顯示）
- 無 `canEdit` 權限時，輸入資料卡片顯示為 `disabled` 灰階樣式
- 移除現有重複的 `if/else` 區塊，改以單一條件動態加 `disabled` attribute

### 修改範圍
- `source/portal/Portal/Views/Home/Index.cshtml`：重構 HTML 版面
- 純前端 CSS/HTML，不動 Controller

---

## 需求二：輸入介面改善

適用於 5 個 Create view（PH、PSJ、GEPT、PS、AS）及 `StudentPopulationController`。

### A — 班級列視覺標色

依班級狀態在 `PopulationPartialView.cshtml`（及 `ASPopulationPartialView.cshtml`）的列元素加入對應 CSS class：

| 優先順序 | 狀態 | Class | 底色 | 判斷條件 |
|---|---|---|---|---|
| 1（最高）| 本週人數歸零 | `row-zero` | 淺紅 `#f8d7da` | `Number == 0` |
| 2 | 本週新增 | `row-new` | 淺綠 `#d4edda` | `IsNew == true && Number > 0` |
| 3 | 承上週未改動 | `row-unchanged` | 淺黃 `#fff3cd` | `Number == LastWeekNumber && !IsNew && Number > 0` |
| — | 本週有改動 | （無 class，預設白底）| — | 其他情況 |

- 樣式定義於 site CSS，不影響後端邏輯

### B — 新增班級改 Modal 彈窗

- 移除各 Create view 頂部的內聯新增輸入列（班系/課程/班別/名稱/人數/+按鈕）
- 改為單一「＋ 新增班級」按鈕，點後彈出 Bootstrap Modal
- Modal 內保留現有串聯 AJAX 邏輯：班系變更 → `/GetCourses?depId=X` → 課程變更 → `/GetClassType?coursesId=X`
- 送出後 Modal 關閉，`#contentItem` 局部更新，對應現有 `AddNewClass` Action（不修改後端）
- 限 `Documented` 狀態才顯示按鈕（和現有一致）

### C — 送出前異動摘要

- 「確認送出」按鈕上方加入 `<div id="submitSummary">`
- 由前端 JS 掃描 `#contentItem` 列表，統計並顯示：
  - 本週新增班級數（掃描 `.row-new` 計數）
  - 本週刪除班級數（由 JS 全域變數 `deletedCount` 追蹤，每次 `RemoveClassItem` 成功後 +1）
  - 人數歸零班級名稱列表（掃描 `.row-zero` 的班級名，紅字警示「下週將不帶入」）
- 每次 `#contentItem` 更新後重新計算，無額外 AJAX

### D — 班級名稱/班別 Inline 編輯

**前端**：
- `PopulationPartialView` 班級名稱欄位改為 `<input type="text" class="class-name-input">`，預設以純文字顯示，focus 後可編輯
- 班別欄位改為 `<select class="class-type-select">`
- `onblur` 觸發 AJAX POST `UpdateClassDetail`

**後端**（新增 Action）：
```
POST /StudentPopulation/UpdateClassDetail
參數：classId (int), name (string), classType (int)
邏輯：
  1. 驗證對應 StudentPopulation 狀態為 Documented
  2. 若 name 為空白/null，保留現有名稱（不更新）
  3. 更新 Class.Name 與 Class.Type
  4. 若 ClassType 有異動，呼叫 SumPHPopulation 重算統計
  5. 返回更新後的 PopulationPartialView
```

- 限 `Documented` 狀態可編輯，`Pending` 以上為唯讀（和現有人數欄位一致）

---

## 需求三：後台匯出

### 目標
在後台人數表列表頁加入與前台相同格式的匯出功能，共用 `ReportExportService`，不複製邏輯。

### UI 修改
**`Areas/Admin/Views/StudentPopulation/Index.cshtml`**：
- 在列表上方加入篩選區：年度（dropdown）、週次（dropdown）、報表類型（PH/GEPT/PSJ/PS/AS dropdown）、分校（選填 dropdown）
- 加入兩個按鈕：「匯出本校報表」、「匯出全區報表」

### 後端（新增 Action）
```
GET /Admin/StudentPopulation/ExportReport
參數：year (int), week (int), reportType (string), allSchools (bool), schoolId (int?)
邏輯：
  1. 若 schoolId 有值：驗證存取權限後匯出單校
  2. 若 allSchools=true：需有 ViewAllSchools 權限，否則只匯出可存取分校
  3. 呼叫 ReportExportService.Export(type, year, week, schoolIds)
  4. 返回 FileResult (xlsx)
```

- 後台本身已限 Admin 角色，不需額外 role 判斷

---

## 需求四：PH 班級明細格式匯出

### 目標
新增「一班一格」子欄位展開格式，讓管理人員快速判斷各分校各課程的班數與班級人數，不取代現有彙整格式。

### 觸發方式
- 前台查詢頁（`StudentPopulation/Query.cshtml`）：現有匯出按鈕旁新增「匯出 PH 明細」按鈕
- 後台（`Areas/Admin/Views/StudentPopulation/Index.cshtml`）：同樣並排新增

### Excel 結構

```
Row 0：標題列（年度、週次）
Row 1：班系名稱（colspan = 該班系所有課程子欄位總數）
Row 2：課程名稱（colspan = 該課程全區最大班數）
Row 3：班名子欄位（甲班、乙班、丙班... 統一對齊，依 Class.Ordinal 排序）
Row 4+：資料列，每校 2 列（小班 / 三人班）
  - Col 0：分校名（合併 2 列）
  - Col 1：班型（小班 / 三人班）
  - Col 2+：各班格填入對應班級人數；無該班的格填空白
```

### 子欄位計算邏輯
1. 匯出前掃描所有分校的 `StudentPopulationItem`，依 `(CourseId, ClassType)` 分組
2. 計算各組最大班數 → 決定各課程子欄位數（統一跨所有分校）
3. 班名排序：`Class.Ordinal` 升冪；名稱填入 Row 3
4. 空白格不計入合計
5. **IsSum 課程欄位不展開子欄位**：保持單一合計格（colspan = 1），值由 `ComputeIsumValue` 計算，Row 3 對應格填「合計」

### 新增程式
| 位置 | 新增內容 |
|---|---|
| `Portal/Services/ReportExportService.cs` | `ExportPHDetail(int year, int week, IList<int> schoolIds)` 方法 |
| `Portal/Controllers/StudentPopulationController.cs` | `ExportReportDetail` Action |
| `Areas/Admin/Controllers/StudentPopulationController.cs` | `ExportReportDetail` Action |
| `Views/StudentPopulation/Query.cshtml` | 新增「匯出 PH 明細」按鈕 |
| `Areas/Admin/Views/StudentPopulation/Index.cshtml` | 新增「匯出 PH 明細」按鈕 |

---

## 需求五：Schema 規劃（未來統計報表）

### 結論：無需異動 Schema

三種規劃中的統計報表均可由現有資料支撐：

| 報表 | 所需資料 | 來源 |
|---|---|---|
| 同期比較 | 相同週次跨年度的 `StudentPopulationItem.Number` | `StudentPopulation.Year` + `Week` 篩選 |
| 課程人數成長 | 同課程跨週次的人數趨勢 | `StudentPopulationItem` 依 `CourseId` + `Year/Week` 查詢 |
| 詢問人數分布 | `Course.Id = 66`（本週總詢問(填單)人數）的 `StudentPopulationItem` | 已有資料，屬 `Department.Id = 13`（統計） |

統計報表的實作（Service 方法 + View）列為**第二波優化**，待 UAT 反饋確認後規劃。

---

## 不在本次範圍

- 班別修改後台頁（管理班級主檔）
- PSJ / GEPT / PS / AS 的班級明細匯出
- 統計報表（同期比較、課程成長、詢問分布）的 UI 實作
- 任何 Schema 異動或 EF Migration

---

## 技術注意事項

- `PopulationPartialView.cshtml` 同時服務 PH 和 PSJ，標色邏輯需確認對兩者皆適用
- `UpdateClassDetail` 若 ClassType 改變，需呼叫 `SumPHPopulation` 重算（因為 StatisticsType 可能按 ClassType 篩選）
- `ExportPHDetail` 的子欄位寬度動態計算，NPOI 須用 `SetColumnWidth` 而非自動調整，避免效能問題
- 班名子欄位的 `Class.Ordinal` 為零時，排序退回 `Class.Id` 升冪
