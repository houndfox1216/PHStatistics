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
│   │   │   └── StatisticsCalculationService.cs  # Statistics aggregation logic
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
- `StudentPopulationItem` — one row per class/type combination
- `StudentPopulationItemLog` — audit log per item
- `StudentPopulationType` / `StudentPopulationStatus` — lookup tables
- `School`, `SchoolYear`, `SchoolClass`, `SchoolAssignment`
- `Course`, `CourseDepartment`, `CourseSubject`, `Class`, `ClassType`

### Database

- **ORM**: Entity Framework Core 8.0 with SQL Server
- **Connection**: Configured in `appsettings.json` → `ConnectionStrings:DataContext`
- **Migrations**: `schema/Data/Migrations/`
- **Default dev DB**: `CLOUDFUN-MSI-LE\SQLEXPRESS`, database `NewPAS`

### Key Dependencies

- **DevExtreme.AspNet.Core** (24.1.4): UI grid/form components
- **J.Framework.\***: Proprietary framework DLLs in `schema/Libraries/`
- **NPOI**: Excel export functionality
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
