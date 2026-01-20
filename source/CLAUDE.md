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
```

## Development URLs

- **Kestrel**: https://localhost:5001 (HTTPS), http://localhost:5000 (HTTP)
- **IIS Express**: http://localhost:8601, https://localhost:44311 (SSL)

## Architecture Overview

PHStatistics.Portal is an ASP.NET Core 8.0 web application for school student population statistics management.

### Project Structure

```
source/
├── portal/
│   ├── PHStatistics.portal.sln     # Main solution
│   ├── Portal/                      # Web application (MVC)
│   │   ├── Controllers/             # Main controllers
│   │   ├── Areas/Admin/             # Admin area (separate routing)
│   │   ├── Actions/                 # Business logic action classes
│   │   ├── Views/                   # Razor views
│   │   ├── Models/                  # View models
│   │   └── wwwroot/                 # Static assets
│   └── Test/                        # NUnit + Selenium test project
└── schema/
    ├── Core/                        # Business logic layer
    ├── Data/                        # Entity Framework Core data layer
    │   ├── DataContext.cs           # Main DbContext
    │   └── Migrations/              # EF Core migrations
    └── Libraries/                   # J.Framework DLLs (proprietary)
```

### Key Architectural Patterns

1. **Custom Framework Extension**: Program inherits from `System.Framework.Web.Application`, using fluent configuration:
   ```csharp
   .UseStartup<Startup, Configuration>()
   .UseLoggerContext<NLogContext>()
   .UseDataContext<DataContext>()
   .UseLocalization()
   ```

2. **Action Pattern**: Business operations in `Actions/` folder (AuthorizationAction, ChangePasswordAction) use a parameter-dictionary execution model.

3. **Model Base Class**: `Model` extends `HttpModelBase<DataContext>` centralizing data access queries.

4. **Admin Area**: Separate MVC area at `/Admin/{controller}/{action}` with its own controllers in `Areas/Admin/Controllers/`.

### Database

- **ORM**: Entity Framework Core 8.0 with SQL Server
- **Connection**: Configured in `appsettings.json` under `ConnectionStrings:DataContext`
- **Migrations**: Located in `schema/Data/Migrations/`

### Key Dependencies

- **DevExtreme.AspNet.Core** (24.1.4): UI components
- **J.Framework.\***: Proprietary framework libraries in `schema/Libraries/`
- **NPOI**: Excel export functionality
- **NLog**: Logging with optional ElasticSearch target

### Localization

- Supported cultures: zh-TW (default), en-US
- Localization files: `Portal/Localizations/*.xml`
- Cookie-based culture selection

### Routing

- Default: `/{controller=Home}/{action=Index}/{id?}`
- Admin area: `/Admin/{controller=Dashboard}/{action=Index}/{id?}`
- Custom catch-all: `/Custom/{*url}`
