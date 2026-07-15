# PSJ（百倍速）匯入改寫 + CKC 自立自學班支援 Design Spec

## 背景

`PopulationImportService`（`Portal/Services/Import/PopulationImportService.cs`）目前有 PH/GEPT/PS/PSJ/AS 五個插件式 `IPopulationImporter`。其中 `PSJPopulationImporter.cs` 對真實檔案**從未真正運作過**：

- 程式讀 `workbook.GetSheetAt(0)`，也就是「總表」頁籤
- 但「總表」頁籤是全分校加總後的日期摘要，**完全沒有分校名欄位**（實際打開 `（百倍速）人數統計表更新版115.6.13).xlsx` 確認）
- 程式碼期待 col2=分校名，於是每一列 `schoolName` 恆為空字串，`School` 查詢恆為 null，整個匯入迴圈形同空轉——現有程式碼不曾真的把任何百倍速資料寫進資料庫過

真正含分校資料的是「北區」「南區」兩個頁籤，用合併儲存格表示分校（一校橫跨 11 個年級列 + 1 個「小計」列）。

同時，公司這邊確認 CKC 自立自學班（英/國/數三科）**未來不會再開課**，但決定新增對應課程以因應舊資料匯入需求，這次一併處理。

## 現況資料調查

### Excel 結構（`（百倍速）人數統計表更新版115.6.13).xlsx`）

四個頁籤：總表（略過，全分校日期摘要）、北區、南區、總計（略過，全區加總）。

**北區**（單一分校區塊，col1-19）：

| Row | 內容 |
|---|---|
| 1 | 標題（含 ROC 日期，如「北區115.6.13」，**無「第X週」字樣，也非「X年X月」格式**——`TitleParser.ParseYearWeek` 兩個 regex 都對不上） |
| 2 | 大分類表頭：col1 分校名、col2 課程、col3 CKC自立自學班、col9 數學班、col14 理化班、col16 分析 |
| 3 | 科目/子項：col3 英文、col5 國文、col7 數學、col9 一對一、col10 小組班、col14 一對一、col15 小組班、col16 新生、col17 流失、col18 上週比、col19 (不含自立自學班)總人數 |
| 4 | 加上/單上 子欄：col3/4=英文(加上/單上)、col5/6=國文(加上/單上)、col7/8=數學(加上/單上)、col10-13=小組班(第一~第四班) |
| 5+ | 資料列，每校 12 列（11 個年級 + 1 列「小計」），分校名用合併儲存格只在區塊第一列出現 |

年級順序（實際出現於資料列）：`二年級,三年級,四年級,五年級,六年級,國一,國二,國三,高一,高二,高三`（**11 個年級，無「一年級」**）。分校清單：南京、內湖、敦南、東湖，區塊最後接一個「總計」區塊（全區加總，須跳過）。

**南區**（左右兩組分校並排，col1-20 與 col21-40，欄位定義完全相同、右半只是 +20 offset）：

- 結構與北區相同，差異在**小組班有 5 個子欄（第一~第五班，col10-14 / col30-34）**，比北區多一欄，導致理化班/分析欄位往後多推 1 格（理化班一對一=col15/35，小組班=col16/36，分析=col17-20/37-40）
- 分校清單：左半 瑞祥、東安、中正、莊敬；右半 河堤、岡山、高美，右半最後接「總計」區塊（跳過）

### 資料庫既有 PSJ（Type=1）課程結構

- 「數學班」（DepartmentId=27，Course.Id 145-156，對應 `一年級`~`高三` 12 個年級）與「理化班」（DepartmentId=30，Course.Id 195-206）都是**每年級一筆 Course 記錄**，用 `Class.Type` 區分一對一（`Personal`）/小組班（`SubGroup`），不是分開兩組課程
- `CourseMapping.PsjCourseIds` 的 `"MP"/"MS"`（數學班）、`"SP"/"SS"`（理化班）陣列本質就是這些 Course.Id，`"N"/"L"/"W"`（新生/流失/上週比）對應各自的「XX班分析」部門下的 IsSum 課程（157-244 全部 `IsSum=1`，由既有 `SumPHPopulation` PSJ 分支計算，這次**不動、不重寫**，PSJ 加總抽離遷移是獨立、尚未排入的後續工作）
- 目前 `Course` 最大 Id = 344，`CourseDepartment` 最大 Id = 38

### ClassType enum 實際定義

```
Group    (顯示"團"，字面即「團體」)
Personal (顯示"EM1")
V2       (顯示"EM2")
V3       (顯示"1V3")
SubGroup (顯示"小")
General  (顯示"一般"，目前當 catch-all 使用)
```

既有數學班/理化班的「一對一/小組班」用 `Personal`/`SubGroup`（不是 `Group`）。CKC 的「單上/加上」語意上是「個別/團體」，直接對應到 `Personal`/`Group`（跟數學班用詞不同軸，屬另一套慣例，刻意不沿用 `SubGroup`）。

## 決策摘要

| 項目 | 決定 |
|---|---|
| 週次/年份取得方式 | `IPopulationImporter.Scan`/`Import` 新增 `int? overrideYear, int? overrideWeek` 參數；PSJ **強制要求**必須提供，否則報錯；PH/GEPT/PS/AS 忽略這兩個參數，沿用既有標題解析邏輯（向後相容） |
| CKC 課程結構 | 比照數學班/理化班模式，**每科展開成每年級一筆**：英文/國文/數學 × 11 年級（二年級~高三）= 33 筆新 `Course` |
| CKC 班系結構 | 三科共用 **1 個**新 `CourseDepartment`「CKC自立自學班」，不比照數學班/理化班「1科目1班系」慣例分成 3 個 |
| 課程命名格式 | `CKC英文-二年級`（科目名+連字號+年級） |
| ClassType 對應 | 單上 → `ClassType.Personal`，加上 → `ClassType.Group` |
| 分析/合計欄位（新生/流失/上週比/總人數） | 這次**略過不匯入**（沿用現況，等 PSJ 加總抽離遷移時再處理） |

## 詳細設計

### 1. `IPopulationImporter` 介面異動

```csharp
public interface IPopulationImporter {
    StudentPopulationType Type { get; }
    ImportScanResult Scan(DataContext db, Stream fileStream, int? overrideYear = null, int? overrideWeek = null);
    ImportResult Import(DataContext db, Stream fileStream, ILogger logger, int? overrideYear = null, int? overrideWeek = null);
}
```

`PopulationImportService.Scan`/`Import` 原樣把這兩個參數往下傳。PH/GeptPopulationImporter/PSPopulationImporter/ASPopulationImporter 的實作簽章更新但內部邏輯不變（忽略新參數）。`PSJPopulationImporter` 若 `overrideYear`/`overrideWeek` 為 null，`Scan`/`Import` 直接回傳錯誤（比照現有 `result.Errors.Add(...)` 模式），不嘗試自行解析標題。

### 2. 後台 UI（`StudentPopulationImportController` + `Index.cshtml`）

- `Import` action 新增可為 null 的 `int? year, int? week` 參數，往下傳給 `PopulationImportService`
- `Index.cshtml` 新增「學年度/週次」下拉選單，資料來源比照 `PopulationWeekSwitch` 功能（撈 `SchoolYear` 表），永遠顯示、可選填；送出時一併帶上
- 若選擇 PSJ 但未選學年度/週次就送出，前端先擋（跳出提示），不送到後端才報錯

### 3. 新增 `CourseDepartment` + 33 筆 `Course`（SQL apply/revert script，比照 GEPT/PH/PS 慣例）

```sql
INSERT INTO CourseDepartment (Id, Name, Type, Ordinal, IsSum, Published, CreatedTime, UpdatedTime, DataMode)
VALUES (39, N'CKC自立自學班', 1, 39, 0, 1, GETDATE(), GETDATE(), 0);
```

Course.Id 345-377（英文 345-355、國文 356-366、數學 367-377，各自對應二年級~高三共 11 筆），`DepartmentId=39`、`Type=1`(PSJ)、`IsSum=0`、`Ordinal` 從 345 接續遞增。

`Course.ClassType`（舊有的自由文字欄位，跟新的 enum 欄位 `ApplicableClassType` 是兩個不同欄位，都叫「適用班別」但只有 `ApplicableClassType` 真正影響加總引擎——這是先前 GEPT/PH/PS 遷移審查就記錄過的既有技術債）：查資料庫發現既有數學班/理化班的 145-156、195-206 這些**非加總**課程，這個舊文字欄位填的是 `"EM1、團"`（即 `Personal`+`Group` 兩個 enum 值的 Display Name，用頓號連接，純粹是給 Admin 後台人看的說明文字）。CKC 這 33 筆課程比照同樣慣例，`ClassType` 欄位一樣填 `"EM1、團"`。`ApplicableClassType`（enum，只在 `IsSum=true` 的加總課程才有意義）維持 `NULL`，這 33 筆本身不是加總課程。

Revert script：`DELETE FROM Course WHERE Id BETWEEN 345 AND 377; DELETE FROM CourseDepartment WHERE Id = 39;`（需確認屆時沒有已匯入資料引用這些 Course，若有要先處理 `Class`/`StudentPopulationItem`）。

### 4. `CourseMapping.cs` 新增

```csharp
public static readonly string[] PsjGradeOrder = {
    "二年級","三年級","四年級","五年級","六年級",
    "國一","國二","國三","高一","高二","高三"
}; // 11 個年級，PSJ/CKC 專用（不含一年級，Excel 資料列從未出現這個年級）

public static readonly Dictionary<string, int[]> CkcCourseIds = new() {
    ["CKC_E"] = new[]{345,346,347,348,349,350,351,352,353,354,355}, // 英文-加上/單上共用
    ["CKC_C"] = new[]{356,357,358,359,360,361,362,363,364,365,366}, // 國文
    ["CKC_M"] = new[]{367,368,369,370,371,372,373,374,375,376,377}, // 數學
};
```

### 5. `PSJPopulationImporter` 重寫邏輯

**Scan**：走訪「北區」「南區」兩頁籤（跳過「總表」「總計」頁籤），依合併儲存格找出每個分校區塊起始列，讀取分校名（跳過「總計」區塊），配合呼叫端傳入的 `overrideYear`/`overrideWeek` 查詢/標記是否已存在對應 `StudentPopulation`。

**Import**：對「北區」「南區」（南區含左右兩組，col offset 0 / +20）分別走訪分校區塊：

1. 用合併儲存格範圍（`ws.MergedRegions`）找出每個分校名區塊涵蓋的列範圍，分校名為「總計」的區塊整塊跳過（全區加總，非個別分校資料）
2. 區塊內逐列讀取「年級」欄，比對 `PsjGradeOrder` 取得 `gradeIdx`；列值為「小計」則跳過該列
3. 依欄位讀取：
   - CKC 英/國/數：加上欄 → `ClassType.Group`、單上欄 → `ClassType.Personal`，`CourseId = CkcCourseIds["CKC_E"/"CKC_C"/"CKC_M"][gradeIdx]`
   - 數學班一對一 → `ClassType.Personal`，`CourseId = CourseMapping.PsjCourseIds["MP"][現有GradeOrder對應index]`
   - 數學班小組班（北區4欄/南區5欄，逐欄各自呼叫一次）→ `ClassType.SubGroup`，`CourseId = PsjCourseIds["MS"][...]`（每個非零子欄各自新增一筆 Class，不加總合併）
   - 理化班一對一/小組班 → 比照數學班，使用 `"SP"/"SS"`
   - 分析欄位（新生/流失/上週比/總人數）→ 略過，不寫入
4. 每個非零數值呼叫 `PopulationWriteHelper.AddClassAndItem`（沿用現有 helper，不改動）

北區/南區欄位 offset 用具名常數表示（不同小組班子欄數量分開定義），不做動態表頭偵測——比照專案既有慣例（PH/PS 的欄位對照表也是寫死常數）。

### 6. 冪等性

沿用 `PopulationWriteHelper.GetOrCreatePopulation(deleteExisting: true)`，跟 PH/GEPT/PS 一致（每次重新匯入該校該週先清除舊 `StudentPopulationItem`/孤立 `Class`，再重建），避免重複匯入產生重複 Class 記錄。

## 測試計畫

寫一支 `[Explicit]` NUnit 測試（`PsjImportTests.cs`），直接用實際檔案（`（百倍速）人數統計表更新版115.6.13).xlsx`）跑一次 `PSJPopulationImporter.Import`，人工核對幾筆已知數字，例如：

- 南京 二年級「數學班小組班」第X班（Excel 原始值 vs 匯入後 `StudentPopulationItem.Number`）
- 內湖 國三「數學班小組班」（Excel 顯示 2+1，應拆成 2 筆各自 Class）
- 任一分校任一年級 CKC 英/國/數 有非零值的列（若這份檔案裡 CKC 欄位全部是 0，需在測試裡明確記錄「CKC 欄位本次資料皆為0，僅驗證流程跑通，未驗證非零情境」，不能假裝有驗證到）
- 南區右半分校（河堤/岡山/高美）確認 +20 offset 有正確讀到

因為現有匯入器從未成功運作過，沒有「跟舊資料比對」基準，這次驗證方式是直接跟 Excel 原始儲存格核對，不是跟舊程式碼比對。

瀏覽器實測（後台匯入頁面實際上傳這個檔案）留到之後集中驗證那一輪，比照專案慣例。

## Out of scope（本次不處理）

- CKC 三科的分析/合計欄位（新生/流失/上週比/總人數）——沒有這些課程，也不會由這次的匯入器計算
- PSJ 既有數學班/理化班的加總抽離（145-244 這些 IsSum 課程遷移到 `AggregationEngine`）——獨立後續計畫，這次不動
- AS（課輔）匯入改寫——獨立後續計畫
- 後台批次匯入 UI 的其他改動（除了新增學年度/週次選單）
- 瀏覽器實測
