# Student Enrollment System

`StudentEnrollmentSystem` is an ASP.NET Core MVC web application for managing a student self-service enrollment workflow.

The app includes:

- student sign-in with ASP.NET Identity
- semester dashboard with quick access to core pages
- course enrollment
- add / drop management
- registration history
- timetable and statement views
- payment and payment history
- enquiry and contact forms
- teaching evaluation submission
- student profile management
- seeded demo data for local development

## Tech Stack

- .NET 10
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server / SQL Server LocalDB
- xUnit for automated tests

## Prerequisites

Install these before running the app:

- .NET 10 SDK
- SQL Server LocalDB or SQL Server Express

The project is configured for SQL Server LocalDB by default.

## Default Database Configuration

The default connection string in [appsettings.json](/abs/path/c:/Users/admin/StudentEnrollmentSystem/appsettings.json:1) is:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=StudentEnrollmentSystemDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

If you want to use a different SQL Server instance, update `DefaultConnection` in:

- [appsettings.json](/abs/path/c:/Users/admin/StudentEnrollmentSystem/appsettings.json:1)
- `appsettings.Development.json` if you use one locally

## Setup

From the project root, run:

```powershell
dotnet restore
dotnet build
```

The app applies EF Core migrations and seeds demo data automatically on startup.

## Run The App

Start the app with:

```powershell
dotnet run --project .\StudentEnrollmentSystem.csproj
```

By default, the app runs at:

- `http://localhost:5187`
- `https://localhost:7036`

Launch settings are defined in [Properties/launchSettings.json](/abs/path/c:/Users/admin/StudentEnrollmentSystem/Properties/launchSettings.json:1).

## Demo Student Accounts

The seeded demo accounts are:

- `alice@student.demo`
- `bob@student.demo`
- `chloe@student.demo`
- `daniel@student.demo`
- `farah@student.demo`

Password for all demo accounts:

- `Pass123$`

## Useful Commands

Build:

```powershell
dotnet build
```

Run tests:

```powershell
dotnet test
```

Run the app:

```powershell
dotnet run --project .\StudentEnrollmentSystem.csproj
```

Add a new migration:

```powershell
dotnet ef migrations add <MigrationName> --project .\StudentEnrollmentSystem.csproj --startup-project .\StudentEnrollmentSystem.csproj --output-dir Data/Migrations
```

## What The App Does

After signing in, a student can use the dashboard to access the main self-service flows:

- browse the enrollment catalog and register for available sections
- add or drop classes with timetable and rule validation
- review registration history and audit trail entries
- view timetable and fee statement information
- make payments and review payment history
- submit enquiries and contact messages
- submit teaching evaluations
- manage profile and personal account details

## Notes

- The database is initialized on application startup through [Data/DbInitializer.cs](/abs/path/c:/Users/admin/StudentEnrollmentSystem/Data/DbInitializer.cs:1).
- If startup fails with a SQL connection error, make sure your SQL Server / LocalDB instance is installed and matches the configured connection string.
