# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the solution (from source directory)
dotnet build portal/PHStatistics.portal.sln

# Build release configuration
dotnet build portal/PHStatistics.portal.sln -c Release

# Run the web application
dotnet run --project portal/Portal/Portal.csproj

# Run tests (NUnit)
dotnet test portal/Test/Test.csproj

# Publish for deployment
dotnet publish portal/Portal/Portal.csproj -c Release

# Docker build (from repo root)
./build.sh <registry_user> <count> portal source/portal/Portal/Dockerfile phstatistics-portal <version>
```

## Development URLs

- **Kestrel**: https://localhost:5001 (HTTPS), http://localhost:5000 (HTTP)
- **IIS Express**: http://localhost:8601, https://localhost:44311 (SSL)

## Architecture Overview

PHStatistics.Portal is an ASP.NET Core 8.0 web application for school student population statistics management (人數表及統計).

### Project Structure

```
source/
├── portal/
│   ├── PHStatistics.portal.sln         # Main solution
│   ├── Portal/                          # Web application (MVC)
│   │   ├── Controllers/                 # Public controllers
│   │   │   ├── HomeController.cs
│   │   │   ├── MemberController.cs      # Login/auth
│   │   │   ├── StudentPopulationController.cs  # Core feature
│   │   │   ├── EventsController.cs
│   │   │   ├── CaptchaController.cs
│   │   │   ├── CustomController.cs
│   │   │   └── PlaceholderController.cs
│   │   ├── Areas/Admin/                 # Admin area (separate routing)
│   │   │   └── Controllers/
│   │   │       ├── AdminBaseController.cs
│   │   │       ├── DashboardController.cs
│   │   │       ├── CourseController.cs
│   │   │       ├── CourseDepartmentController.cs
│   │   │       └── MemberController.cs
│   │   ├── Actions/                     # Auth actions (portal-level)
│   │   │   ├── AuthorizationAction.cs
│   │   │   ├── SessionAuthorizationAction.cs
│   │   │   └── ChangePasswordAction.cs
│   │   ├── Models/
│   │   │   └── Model.cs                 # HttpModelBase<DataContext> sub-model hub
│   │   ├── Services/
│   │   │   ├── StatisticsCalculationService.cs  # Statistics aggregation logic
│   │   │   └── ReportExportService.cs           # Excel export (import-compatible format)
│   │   ├── Views/                       # Razor views
│   │   ├── Localizations/               # XML localization files
│   │   ├── Program.cs                   # App entry point (inherits Application)
│   │   ├── Startup.cs                   # DI & middleware config
│   │   ├── Policy.cs                    # Authorization policy
│   │   ├── PortalUser.cs                # User model
│   │   ├── appsettings.json
│   │   ├── NLog.config
│   │   └── Dockerfile
│   └── Test/                            # NUnit + Selenium test project
└── schema/
    ├── Core/                            # Business logic layer
    │   ├── Basis/Actions/               # User, Role, Person, Album, Attribute, Category, etc.
    │   ├── Community/Actions/           # Member actions
    │   └── Content/Actions/             # Banner, Class, Course, News, Page, School,
    │                                    # SchoolYear, StudentPopulation*, UrlSegment
    ├── Data/                            # Entity Framework Core data layer
    │   ├── DataContext.cs               # Main DbContext
    │   ├── DataContextFactory.cs
    │   ├── Migrations/                  # EF Core migrations
    │   ├── Basis/                       # User, Role, Person, Album, Tag, etc. entities
    │   ├── Community/                   # Member entity
    │   └── Content/                     # Banner, Class, Course, News, School,
    │                                    # StudentPopulation*, UrlSegment entities
    └── Libraries/                       # J.Framework DLLs (proprietary)
```

### Key Architectural Patterns

1. **Custom Framework Extension**: `Program` inherits from `System.Framework.Web.Application`, using fluent configuration:
   ```csharp
   .UseStartup<Startup, Configuration>()
   .UseLoggerContext<NLogContext>()
   .UseDataContext<DataContext>()
   .UseLocalization()
   ```

2. **Action Pattern**: Business operations live in `schema/Core/*/Actions/` (CRUD actions per entity) and follow a parameter-dictionary execution model. Portal-level auth actions are in `portal/Portal/Actions/`.

3. **Model Hub**: `Portal/Models/Model.cs` extends `HttpModelBase<DataContext>`. Access all domain sub-models through it (e.g., `Model.BannerPosition`, `Model.StudentPopulation`).

4. **StatisticsCalculationService**: Injected service (`Portal/Services/`) that aggregates student population item sums and statistics across school classes and courses.

5. **Admin Area**: Separate MVC area at `/Admin/{controller}/{action}` with its own controllers in `Areas/Admin/Controllers/`.

### Domain — Student Population (核心功能)

Key entities in `schema/Data/Content/`:
- `StudentPopulation` — a population table for a school year
- `StudentPopulationItem` — one row per class/type combination; `IsSum=true` marks computed items
- `StudentPopulationItemLog` — audit log per item
- `StudentPopulationType` / `StudentPopulationStatus` — lookup tables
- `School`, `SchoolYear`, `SchoolClass`, `SchoolAssignment`
- `Course`, `CourseDepartment`, `CourseSubject`, `Class`, `ClassType`

Key `Course` flags:
- `IsSum` — marks aggregate/summary columns (e.g. 國小人數合計、總班數)
- `StatisticsType` — enum describing how to compute (`SumByDepartment`, `CountClasses`, `LastWeekValue`, etc.)
- `SourceDepartmentIds` / `SourceCourseIds` — JSON int arrays specifying source scope for IsSum computation
- `GroupByClassType` — if true, computation is scoped to the same ClassType as the row
- `ApplicableClassType` — fixed ClassType filter overriding GroupByClassType

### Database

- **ORM**: Entity Framework Core 8.0 with SQL Server
- **Connection**: Configured in `appsettings.json` → `ConnectionStrings:DataContext`
- **Migrations**: `schema/Data/Migrations/`
- **Default dev DB**: `CLOUDFUN-MSI-LE\SQLEXPRESS`, database `NewPAS`

### Key Dependencies

- **DevExtreme.AspNet.Core** (24.1.4): UI grid/form components
- **J.Framework.\***: Proprietary framework DLLs in `schema/Libraries/`
- **NPOI**: Excel read/write (XSSFWorkbook)
- **NLog**: Logging; optional ElasticSearch target (`NLog-ElasticSearch.config`)
- **Newtonsoft.Json**: JSON serialization (Pascal-case, local timezone dates)

### Localization

- Supported cultures: `zh-TW` (default), `en-US`
- Localization files: `Portal/Localizations/*.xml`
- Cookie-based culture selection (`culture` cookie)

### Routing

- Default: `/{controller=Home}/{action=Index}/{id?}`
- Admin area: `/Admin/{controller=Dashboard}/{action=Index}/{id?}`
- Custom catch-all: `/Custom/{*url}`

### Session & Security

- Session idle timeout: 20 minutes
- Authentication redirect: `/Member/Login` (with `returnUrl` param)
- CORS allowed origin: `http://npas.ckc-highspeed.com.tw` (credentials enabled)
- XSS prevention, `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`

### Deployment

- Docker image: built from `source/portal/Portal/Dockerfile` (base image `mcr.microsoft.com/dotnet/aspnet:8.0`, timezone `Asia/Taipei`)
- Use `build.sh` / `deploy.sh` scripts at repo root for CI/CD

---

## 批次匯入 / 匯出系統（Batch Import / Export）

### 匯入 — `HomeController.cs`

批次匯入入口：`POST /ImportAll`，讀取指定目錄下的 `.xlsx` 檔案，依檔名判斷類型後呼叫對應 handler。

| 檔名關鍵字 | Handler | 類型 |
|---|---|---|
| `全國人數表` | `RunImportPH`（sheets 0–1）+ `RunImportGEPT`（sheet 2） | PH + GEPT |
| `PS南區` | `RunImportPS` | PS |
| `百倍速` | `RunImportPSJ` | PSJ |
| `百瀚全區課輔` | `RunImportAS` | AfterSchool |

#### PH / GEPT 共用解析器 — `RunImportSheetPH`

- Row 0：標題（用正規表示式解析年份、週次）
- Rows 1–3：三層表頭；Row 1 = 科別、Row 3 = 課程名稱
- Row 4+：資料列；col 0 = 分校（跨列合併時向下延伸）、col 1 = 班別（`"小"` → SubGroup、`"三"` → V3）
- `requireTypeIndicator=true`（PH）：col 1 必須是 "小"/"三" 才處理
- `requireTypeIndicator=false`（GEPT）：所有列視為 ClassType.General
- 課程對應：先查 DB Name，找不到則用 `_phColCourseId` 欄位索引對照表 fallback
- EM1 邏輯：`_em1CourseIds`（課程 ID 23/24/25/26/50/51/52/53），每人建立獨立 Class 記錄（count=1）

#### 冪等性（Idempotent import）

`GetOrCreatePopulation(deleteExisting: true)` 在重新匯入前：
1. 刪除該校該週該類型所有 `StudentPopulationItem`
2. 刪除對應的孤立 `Class` 記錄
3. 再重建新記錄

PH 及 GEPT 使用 `deleteExisting=true`（每次重建），PS 同樣使用 `true`，PSJ/AS 各校各週各類型獨立管理。

#### 硬碼對照表（HomeController 靜態欄位）

```csharp
// PH：Excel 欄位索引 → Course.Id
private static readonly Dictionary<int, int> _phColCourseId

// PS：欄位標頭字串 → Course.Id（大小寫不敏感）
private static readonly Dictionary<string, int> _psHeaderCourseId

// EM1 課程 Id 集合（個別指導：每人 1 筆 Class）
private static readonly HashSet<int> _em1CourseIds = { 23, 24, 25, 26, 50, 51, 52, 53 }

// PSJ 年級課程 Id（per grade，12 筆）
private static readonly Dictionary<string, int[]> _psjCourseIds

// AS 年級課程 Id（per grade，12 筆）
private static readonly Dictionary<string, int[]> _asCourseIds
```

同一份對照表在 `HomeController`、`StudentPopulationController`、`ReportExportService` 三處各自維護靜態副本，修改時須同步更新。

---

### 匯出 — `ReportExportService.cs`

入口：`Export(type, year, week, schoolIds?)` → `byte[]`（xlsx）

所有分校輸出到同一個 Sheet1，格式與匯入 Excel 完全相容。

#### Excel 結構（各類型）

| 類型 | Row 0 | Rows 1–3 | 資料起始 | 備註 |
|---|---|---|---|---|
| PH | 標題 | 科別/空/課程（三層表頭） | Row 4，每校 2 列（小/三） | col 0 = 分校（合併）、col 1 = 類型 |
| GEPT | 標題 | 同 PH，無 col 1 | Row 4，每校 1 列 | ClassType.General |
| PS | 標題 | 課程名稱（單列） | Row 2，每校 1 列 | col 0 = 分校、col 1+ = 人數 |
| PSJ | 標題 | — | Row 4 = code 表頭、Row 5+ 資料 | 格式：年/週/分校/年級/T/MP/MS/... |
| AS | 標題 | — | Row 4 = code 表頭、Row 5+ 資料 | 格式：年/週/分校/年級/T/AS/EP/... |

#### 欄位數值來源 —— 匯出不做任何計算

匯出時所有欄位（包含 `Course.IsSum = true` 的合計/分析欄）一律直接讀取已存的 `StudentPopulationItem.Number`，**匯出當下不重新計算**。真正的計算發生在存檔時，由 `AggregationEngine.CalculateAll` 執行並把結果寫回 `Number`；匯出程式只負責讀取與排版（`BuildSheetPS` 等 `BuildSheetXXX` 方法內對 IsSum 欄位頂多做「不要重複計入自己合成的部門合計欄」這類排版判斷，不會重算數值本身）。

- `ReportExportService.ComputeIsumValue`（舊版 StatisticsType→計算方式對照表）目前已無任何呼叫點，是死碼，勿再依此描述匯出行為。
- 若匯出出來的 IsSum 欄位數字看起來不對，應排查 `AggregationEngine`（存檔時）或該筆 `StudentPopulationItem.IsManual` 是否被凍結，而不是懷疑匯出程式做了錯的計算。

#### `ExportReport` endpoint（StudentPopulationController）

```
GET /StudentPopulation/ExportReport?year=&week=&reportType=&allSchools=&schoolId=
```

- `schoolId`：指定單一分校（優先於 `allSchools`），需驗證存取權限
- `allSchools=true`：需有 `ViewAllSchools` 權限，否則只輸出可存取分校
- 呼叫 `ReportExportService.Export(type, year, week, schoolIds)`

---

### AggregationEngine（`Portal/Services/Aggregation/AggregationEngine.cs`）

實際負責計算 `StudentPopulation.Items` 中 `IsSum=true` 項目數值的元件，由存檔/重算流程呼叫（`StudentPopulationController`）：

- `CalculateAll(population)` — 計算該人數表所有 IsSum 項目，寫回 `item.Number`
- `Calculate(item, population)` — 計算單一 IsSum 項目；`item.IsManual=true` 時直接跳過（凍結值不覆蓋）
- `Preview(item, population)` — 邏輯同 `Calculate` 但不寫回，供「改用系統試算值」等預覽功能使用
- 依 `Course.StatisticsType` 分派計算方式：`SumByDepartment` / `CountClasses` / `LastWeekValue` / `DiffWithLastWeek` / `LastYearValue` / `DiffWithLastYear` / `DiffBetweenCourses` / `DivideBySourceCourses` / `Average` / `YearToDateSum` / `SumFromOtherType` / `LastWeekValueFromOtherType` 等；未支援的型別會 throw `NotSupportedException`
- `Course.SourceCourseIds` / `NegativeSourceCourseIds` 決定來源課程（相除的分子/分母、相減的加項/減項）；`SourceStudentPopulationType` 用於跨報表類型取值（例如 PS 課程 142 取 PSJ 資料）

`StatisticsCalculationService.cs` 是同名邏輯的舊版實作，目前未被注入或呼叫（死碼），勿再參考。**`ReportExportService.ComputeIsumValue` 同樣是死碼** —— 匯出不重算，只讀已由 `AggregationEngine` 算好並持久化的 `Number`。
