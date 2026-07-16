# PSJ + AS Aggregation Migration Design Spec

## 背景

`StudentPopulationController.SumPHPopulation` 目前對 PSJ（`StudentPopulationType.PSJ`，`Type=1`，百倍速）與 AfterSchool（`StudentPopulationType.AfterSchool`，`Type=4`，課輔）的加總計算，都還是寫在 `classGroup` 迴圈內各自一段依課程名稱字串比對／`Department.Id` 判斷的 if/else（`StudentPopulationController.cs:1454-1473` 為 PSJ，`1500-1517` 為 AS），跟 GEPT（commit `0e8b4dc`）、PH（commit `4a8db34`）、PS（commit `63b8b25`）遷移前的舊狀態完全相同的模式。

這次比照 GEPT/PH/PS 的做法，把 PSJ 跟 AS 的加總邏輯一起遷移到共用的 `AggregationEngine`（`source/portal/Portal/Services/Aggregation/AggregationEngine.cs`）。**跟 PS 遷移不同，這次不需要擴充引擎程式碼**——`AggregationEngine` 現有的 `SumBySourceDepartments`、`DiffWithLastWeek`、`ManualInput` 已經完全覆蓋 PSJ/AS 所需的計算方式，沒有新的 `StatisticsType` 或 `GetSourceItems` 篩選邏輯需要新增。

PSJ 跟 AS 的加總課程形狀幾乎一樣（都是「原始課程 → 合計（依班別）→ 總合計（不分班別）→ 與上週相比×12（每年級）→ 新生×12 → 流失×12」），因此這次合併成一份 spec/plan 一次處理完，不比照 PH/PS 各自獨立一份計畫的模式。

## 資料調查發現（2026-07-16，查證 dev DB 實際課程結構）

### PSJ（Type=1，Course Id 157–244，共 88 筆）

查詢 dev DB 確認課程結構如下（`Ordinal` 皆為 `Id-1`，`DataMode` 皆為 `0`）：

| Id 範圍 | 名稱模式 | 班系 | IsSum |
|---|---|---|---|
| 145–156 | 數學班原始課程（一年級–高三） | 數學班(27) | 0（非加總，已存在，不在這次遷移範圍） |
| 157 | 本周數學人數合計 | 數學班統計(28) | 1 |
| 158 | 本週數學總人數合計 | 數學班統計(28) | 1 |
| 159–170 | 本週{年級}與上週相比×12 | 數學班分析(29) | 1 |
| 171–182 | 本週{年級}數學新生人數×12 | 數學班分析(29) | 1 |
| 183–194 | 本週{年級}數學流失人數×12 | 數學班分析(29) | 1 |
| 195–206 | 理化班原始課程（一年級–高三） | 理化班(30) | 0（非加總，已存在，不在這次遷移範圍） |
| 207 | 本周理化人數合計 | 理化班統計(31) | 1 |
| 208 | 本週理化總人數合計 | 理化班統計(31) | 1 |
| 209–220 | 本週{年級}與上週相比×12 | 理化班分析(32) | 1 |
| 221–232 | 本週{年級}理化新生人數×12 | 理化班分析(32) | 1 |
| 233–244 | 本週{年級}理化流失人數×12 | 理化班分析(32) | 1 |

讀舊程式碼（`StudentPopulationController.cs:1454-1473`）確認計算邏輯：

1. **157「本周數學人數合計」依 `group.Class.Type` 分開算**（`e.Class.Type == group.Class.Type`），即同一課程底下實際存有多筆 `StudentPopulationItem`（一對一/小組班各一筆），各自加總「數學班」部門內同班別的原始課程。
2. **158「本週數學總人數合計」不分班別**，直接加總「數學班」部門內所有原始課程（不論一對一或小組班）。
3. **159–170「本週{年級}與上週相比」**：`srcId = group.Class.Course.Id - 14`（159-14=145 一年級 …170-14=156 高三，一一對應數學班原始課程），且**依 `group.Class.Type` 篩選**（同 157），算 `本週(srcId,同班別) - 上週(srcId,同班別)`。
4. **207/208/209-220 理化班的邏輯跟數學班（157/158/159-170）完全對稱**，`srcId = Id - 14` 同樣對應到 195–206。
5. **171–194（新生/流失，數學）、221–244（新生/流失，理化）全部是分校自填**，程式碼註解「流失人數／新生人數：分校自填，系統不計算」，目前 `StatisticsType` 皆為 `NULL`（尚未設定 `ManualInput`）。

### AS（Type=4，Course Id 245–344，共 100 筆）

| Id 範圍 | 名稱模式 | 班系 | IsSum |
|---|---|---|---|
| 245–256 | 安親課輔班原始課程 | 安親課輔班班(33) | 0（不在這次遷移範圍） |
| 257 | 本周安親課輔班人數合計 | 安親課輔班班統計(34) | 1 |
| 258 | 本週安親課輔班總人數合計 | 安親課輔班班統計(34) | 1 |
| 259–270 | 本週{年級}與上週相比×12 | 安親課輔班班分析(35) | 1 |
| 271–282 | 本週{年級}安親課輔班文新生人數×12 | 安親課輔班班分析(35) | 1 |
| 283–294 | 本週{年級}安親課輔班文流失人數×12 | 安親課輔班班分析(35) | 1 |
| 295–306 | 英文班原始課程（EP/EG） | 英文班(36) | 0（不在這次遷移範圍） |
| 307 | 本周英文班人數合計 | 英文班統計(37) | 1 |
| 308 | 本週英文班總人數合計 | 英文班統計(37) | 1 |
| 309–320 | 本週{年級}與上週相比×12 | 英文班分析(38) | 1 |
| 321–332 | 本週{年級}英文班文新生人數×12 | 英文班分析(38) | 1 |
| 333–344 | 本週{年級}英文班文流失人數×12 | 英文班分析(38) | 1 |

讀舊程式碼（`StudentPopulationController.cs:1500-1517`）確認計算邏輯：

1. **257「合計」與 258「總合計」在舊程式碼裡完全沒有區分**——`asDeptId == 34` 這個分支同時涵蓋 257 跟 258，兩者都算成「安親課輔班部門內所有非加總課程加總，不分班別」。這跟 PSJ 的 157(分班別)/158(不分班別) 有實質差異：**AS 完全沒有依班別分開算的邏輯**，即使英文班（307/308）底下有 EP(一對一)/EG(團體) 兩種真實存在的班別，舊程式碼也是混在一起算。
2. **經與使用者確認：這次遷移保持舊行為**（257/258 都設 `GroupByClassType=false`，跟 PSJ 的 157(true)/158(false) 刻意不同），不藉這次機會改成依班別分開算——這是既有現象照實搬，不是這次引入的新差異，也不是這次要修的 bug。
3. **259–270「本週{年級}與上週相比」**：`srcId = group.Class.Course.Id - 14`（259-14=245 一年級 … 270-14=256 高三），**不依班別篩選**（舊碼沒有 `&& e.Class.Type == group.Class.Type`），算 `本週(srcId) - 上週(srcId)`。
4. **307/308/309-320 英文班的邏輯跟安親課輔班（257/258/259-270）完全對稱**，`srcId = Id - 14` 對應到 295–306，同樣不依班別篩選。
5. **271–294（新生/流失，安親）、321–344（新生/流失，英文班）全部是分校自填**，程式碼註解「新生人數／流失人數：分校自填，系統不計算」，目前 `StatisticsType` 皆為 `NULL`。

## 規則對應表

### PSJ（28 個需設定計算規則的課程 + 48 個手動課程）

| Id | 名稱 | StatisticsType | SourceDepartmentIds / SourceCourseIds | GroupByClassType |
|---|---|---|---|---|
| 157 | 本周數學人數合計 | `SumBySourceDepartments`(3) | `[27]` | **true** |
| 158 | 本週數學總人數合計 | `SumBySourceDepartments`(3) | `[27]` | false |
| 159–170 | 本週{年級}與上週相比×12 | `DiffWithLastWeek`(10) | `[145]`…`[156]`（各自對應） | **true** |
| 207 | 本周理化人數合計 | `SumBySourceDepartments`(3) | `[30]` | **true** |
| 208 | 本週理化總人數合計 | `SumBySourceDepartments`(3) | `[30]` | false |
| 209–220 | 本週{年級}與上週相比×12 | `DiffWithLastWeek`(10) | `[195]`…`[206]`（各自對應） | **true** |
| 171–194 | 新生×12 + 流失×12（數學） | `ManualInput`(50) | — | — |
| 221–244 | 新生×12 + 流失×12（理化） | `ManualInput`(50) | — | — |

### AS（28 個需設定計算規則的課程 + 48 個手動課程）

| Id | 名稱 | StatisticsType | SourceDepartmentIds / SourceCourseIds | GroupByClassType |
|---|---|---|---|---|
| 257 | 本周安親課輔班人數合計 | `SumBySourceDepartments`(3) | `[33]` | false |
| 258 | 本週安親課輔班總人數合計 | `SumBySourceDepartments`(3) | `[33]` | false |
| 259–270 | 本週{年級}與上週相比×12 | `DiffWithLastWeek`(10) | `[245]`…`[256]`（各自對應） | false |
| 307 | 本周英文班人數合計 | `SumBySourceDepartments`(3) | `[36]` | false |
| 308 | 本週英文班總人數合計 | `SumBySourceDepartments`(3) | `[36]` | false |
| 309–320 | 本週{年級}與上週相比×12 | `DiffWithLastWeek`(10) | `[295]`…`[306]`（各自對應） | false |
| 271–294 | 新生×12 + 流失×12（安親） | `ManualInput`(50) | — | — |
| 321–344 | 新生×12 + 流失×12（英文班） | `ManualInput`(50) | — | — |

`SourceCourseIds` 用單元素陣列（如 `[145]`）而非直接存整數，是配合 `AggregationEngine.ParseIntArray` 一律用 JSON 陣列格式解析的既有慣例（跟 PS 課程142/144 的 `[132,135]` 一致）。

## 已知風險／需要驗證的地方

- **257 跟 258 用引擎規則算出來會是同一個數字**——這是忠實複製舊程式碼既有現象（見上方「資料調查發現」第1點），不是這次遷移引入的問題，比對測試預期這兩個課程都是 0 落差，但兩者數值相同這件事本身值得在測試報告裡明確記一筆，避免未來的人誤以為是 bug。
- **沒有跨課程依賴鏈**：跟 PS 的 132→144（`SourceCourseIds` 指向另一個加總課程）不同，PSJ/AS 這次所有 `SourceCourseIds` 都只指向原始（非加總）課程，`SourceDepartmentIds` 也都指向原始部門，彼此之間沒有「A 需要 B 先算完」的依賴，`classGroup` 迴圈現有的 `OrderBy(Ordinal)`（PS 遷移時已加上，全域生效）足夠，不需要為 PSJ/AS 額外處理順序問題。
- **測試預期**：跟 PS 類似（而非 PH 那種需要處理既有例外的情況），資料調查階段沒有發現任何跟舊值不符的計算 bug，理論上比對測試應該 0 落差。仍然要實際跑測試驗證所有真實 PSJ/AS 人數表，不能只憑推理假設 0 落差——若跑出非預期落差，要先查根因、呈報使用者決定是否接受為預期變更，不能自己擴大「預期例外」名單搪塞過去。
- **理化班原始課程 195–206 的 `ClassType` 欄位** 跟數學班 145–156 一樣填 `EM1、團`，跟 CKC 那批新課程一致，這次不動它們（它們不是加總課程）。

## 程式碼改動方式

比照使用者這次決定：**PSJ 跟 AS 分支的舊 if/else 都整段用 `#if false` 保留、不刪除**（跟 PH/PS 一致，跟最早的 GEPT 直接刪除不同），各自加上一行呼叫引擎：

```csharp
else if (studentPopulationData.Type == StudentPopulationType.PSJ) {
#if false // 舊 PSJ 加總邏輯，2026-07-16 遷移到 AggregationEngine 時停用保留（不刪除），
          // 見 docs/superpowers/plans/2026-07-16-psj-as-aggregation-migration.md
    if (group.Class.Course.Name.Equals("本周數學人數合計")) {
        ...（原始程式碼原樣保留）
    }
    // 流失人數／新生人數：分校自填，系統不計算
#endif
    aggregationEngine.Calculate(group, studentPopulationData);
}
```

```csharp
else if (studentPopulationData.Type == StudentPopulationType.AfterSchool) {
#if false // 舊 AS 加總邏輯，2026-07-16 遷移到 AggregationEngine 時停用保留（不刪除），
          // 見 docs/superpowers/plans/2026-07-16-psj-as-aggregation-migration.md
    int asDeptId = group.Class.Course.Department.Id;
    ...（原始程式碼原樣保留）
#endif
    aggregationEngine.Calculate(group, studentPopulationData);
}
```

`classGroup` 迴圈外目前沒有 PSJ／AS 專屬的後處理區塊（迴圈外只有 PH 有大段 `#if false` 舊碼），不需要改動。

## 測試方式

沿用 GEPT/PH/PS 驗證模式：各寫一支 `[Explicit]` NUnit 測試（`PsjAggregationComparisonTests.cs`、`AsAggregationComparisonTests.cs`），直接接 dev 的 `DataContext`，抓 PSJ / AS 所有已存在的人數表資料，逐筆比對「資料庫目前存的 Number」vs「新引擎算出來的 Number」：

- 採用 PS 遷移確立的「全部算完 → 全部比對 → 全部還原」三段式（不是逐項算完立刻比對還原），避免像 PS 遷移那次抓到的順序 bug 重演。
- PSJ：28 個引擎計算課程（157/158/159-170/207/208/209-220），目標 0 落差；48 個人工課程（171-194/221-244）不比對（引擎對 `ManualInput` 是 no-op）。
- AS：28 個引擎計算課程（257/258/259-270/307/308/309-320），目標 0 落差；48 個人工課程（271-294/321-344）不比對。
- 若出現非預期落差，要先查根因（是不是像 PH 課程67那種髒資料問題），不能直接擴大「預期例外」名單搪塞過去。

## Out of scope（本次不處理）

- PSJ CKC 三科（自立自學班）自己的新生/流失/上週比/總人數——這批課程在 2026-07-16 的 PSJ CKC 匯入改寫裡就決定不建立，自然也沒有東西可以遷移。
- PSJ 145–156、195–206、AS 245–256、295–306 這些**原始**課程本身——它們不是加總課程（`IsSum=0`），不在這次規則設定範圍內。
- AS 依班別（EP/EG）分開統計的行為改進——已與使用者確認保持舊行為，若未來要改成依班別分開算，需另開計畫討論。
- 刪除 `#if false` 保留的舊碼——比照 PH/PS，等瀏覽器驗證通過後另開任務處理。
- 瀏覽器手動驗證——延後到之後集中驗證那一輪，比照專案慣例。
