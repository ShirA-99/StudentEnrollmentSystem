# Student Enrollment System

ASP.NET Core MVC student system prepared as the shared team base, with the enrollment and add/drop module already completed and ready for the rest of the team to extend.

## Current System

- Student login with ASP.NET Identity
- Course enrollment
- Course add/drop
- Add/drop history
- SQL-backed student, semester, course, section, enrollment, and audit models
- Seeded demo student accounts
- Validation for duplicate enrollment, timetable clash, seat capacity, and course dropping
- Automated xUnit tests for the enrollment and add/drop rules

## Demo Accounts

- `alice@student.demo`
- `bob@student.demo`
- `chloe@student.demo`
- Password for all accounts: `Pass123$`

## Team Setup

Each member should work from the same shared base project and use their own feature branch.

Suggested branch ownership:

- You / team lead: `feature/enrollment-add-drop`
- Member 2: `feature/enquiry-evaluation`
- Member 3: `feature/payment`
- Member 4: `feature/account`
- Member 5: `feature/statements`

Suggested workflow:

1. Clone the repository.
2. Create or switch to your assigned branch.
3. Pull the latest changes from `main` before starting work.
4. Commit regularly with clear messages.
5. Push your branch and open a pull request back into `main`.

## Local Prerequisites

Each member should install the following before running the project:

- .NET SDK 10
- SQL Server LocalDB or SQL Server Express
- Visual Studio Code or Visual Studio

Optional but useful:

- SQL Server extension tools for viewing the database
- GitHub Desktop or Git CLI

## Database Setup

This project is configured for SQL Server LocalDB by default.

Default connection string:

`Server=(localdb)\MSSQLLocalDB;Database=StudentEnrollmentSystemDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`

If your machine uses another SQL Server instance such as `.\SQLEXPRESS`, update `DefaultConnection` in:

- `StudentEnrollmentSystem/appsettings.json`
- `StudentEnrollmentSystem/appsettings.Development.json`

The app applies migrations and seeds demo data automatically on startup.

## First Run

From the project root, run:

```powershell
dotnet restore
dotnet build StudentEnrollmentSystem.slnx
dotnet test StudentEnrollmentSystem.slnx
dotnet run --project StudentEnrollmentSystem\StudentEnrollmentSystem.csproj
```

If the app fails to start with a database connection error, check:

- SQL Server LocalDB is installed
- the `MSSQLLocalDB` instance exists
- your connection string matches your local SQL Server instance

## Git Commands

Create and switch to your branch:

```powershell
git checkout main
git pull
git checkout -b feature/your-branch-name
```

Commit and push:

```powershell
git add .
git commit -m "Implement feature updates"
git push -u origin feature/your-branch-name
```

Keep your branch updated with `main`:

```powershell
git checkout main
git pull
git checkout feature/your-branch-name
git merge main
```

## Useful Commands

- Build: `dotnet build StudentEnrollmentSystem.slnx`
- Test: `dotnet test StudentEnrollmentSystem.slnx`
- Run app: `dotnet run --project StudentEnrollmentSystem/StudentEnrollmentSystem.csproj`
- Add migration: `dotnet dotnet-ef migrations add <MigrationName> --project StudentEnrollmentSystem/StudentEnrollmentSystem.csproj --startup-project StudentEnrollmentSystem/StudentEnrollmentSystem.csproj --output-dir Data/Migrations`

## Notes For Team Members

- Reuse the existing shared models and database context instead of creating duplicate student or semester tables.
- Keep each feature inside its own controller and view folder where possible.
- Do not change another member's branch directly.
- Open a pull request when your module is ready so it can be reviewed and merged cleanly.
