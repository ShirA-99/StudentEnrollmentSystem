# Student Enrollment System

ASP.NET Core MVC student system prepared as the shared base project for the whole team. The enrollment and add/drop module is already in place, and the other members can now build their own modules on top of the same project structure.

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

## Team Branches

The team branches have already been prepared from `main`.

- `main` - shared base branch
- `feature/enrollment-add-drop` - enrollment and add/drop
- `feature/enquiry-evaluation` - enquiry and student evaluation
- `feature/payment` - payment and payment history
- `feature/account` - account management
- `feature/statements` - statements

Each member should work only in their assigned branch.

## Before You Start

If you have never used GitHub before, follow this order:

1. Create a GitHub account: https://github.com/signup
2. Install Git for Windows: https://git-scm.com/download/win
3. Install the .NET 10 SDK: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
4. Install SQL Server LocalDB or SQL Server Express:
   - SQL Server Express: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
5. Install Visual Studio Code if needed: https://code.visualstudio.com/
6. Optional SQL extension for VS Code:
   - SQL Server (mssql): https://marketplace.visualstudio.com/items?itemName=ms-mssql.mssql

## Clone The Project

Only do this once on your machine.

```powershell
git clone https://github.com/ShirA-99/StudentEnrollmentSystem.git
cd StudentEnrollmentSystem
```

## Get Your Branch

After cloning, switch to your assigned branch.

Examples:

```powershell
git fetch origin
git checkout feature/enquiry-evaluation
```

```powershell
git fetch origin
git checkout feature/payment
```

If Git tells you the branch does not exist locally yet, that is normal. `git checkout` will create the local copy from GitHub.

## Project Structure

The project is now flat at the repository root so everyone works in the same top-level app instead of a nested project folder.

Main working folders:

- `Controllers` - one controller area per feature
- `Views` - feature views
- `Models` - shared entity models
- `ViewModels` - UI-specific models
- `Services` - business logic
- `Data` - database context, seed data, and migrations
- `wwwroot` - static assets
- `StudentEnrollmentSystem.Tests` - automated tests

Prepared member folders and placeholders:

- `Controllers/EnrollmentController.cs`
- `Controllers/AddDropController.cs`
- `Controllers/EnquiryController.cs`
- `Controllers/EvaluationController.cs`
- `Controllers/PaymentController.cs`
- `Controllers/StatementController.cs`
- `Controllers/AccountController.cs`
- `Views/Enrollment`
- `Views/AddDrop`
- `Views/Enquiry`
- `Views/Evaluation`
- `Views/Payment`
- `Views/Statement`
- `Views/Account`

## Database Setup

This project is configured for SQL Server LocalDB by default.

Default connection string:

`Server=(localdb)\MSSQLLocalDB;Database=StudentEnrollmentSystemDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`

If your machine uses SQL Server Express or another SQL Server instance instead, update `DefaultConnection` in:

- `appsettings.json`
- `appsettings.Development.json`

The app applies migrations and seeds demo data automatically on startup.

## First Run

Run these commands from the repository root:

```powershell
dotnet restore
dotnet build StudentEnrollmentSystem.slnx
dotnet test StudentEnrollmentSystem.slnx
dotnet run --project .\StudentEnrollmentSystem.csproj
```

If the app fails to start with a database connection error, check:

- SQL Server LocalDB is installed
- the `MSSQLLocalDB` instance exists
- your connection string matches your SQL Server instance

## Everyday Git Workflow

Use this every time before starting work:

```powershell
git checkout main
git pull origin main
git checkout your-branch-name
git merge main
```

Example:

```powershell
git checkout main
git pull origin main
git checkout feature/account
git merge main
```

After making changes:

```powershell
git add .
git commit -m "Describe your changes"
git push
```

## When Your Module Is Ready

1. Push your branch to GitHub.
2. Open the GitHub repository in the browser.
3. GitHub will usually show a button to create a Pull Request.
4. Open the Pull Request into `main`.
5. Wait for review before merging.

## Useful Commands

- Build: `dotnet build StudentEnrollmentSystem.slnx`
- Test: `dotnet test StudentEnrollmentSystem.slnx`
- Run app: `dotnet run --project .\StudentEnrollmentSystem.csproj`
- Add migration: `dotnet dotnet-ef migrations add <MigrationName> --project .\StudentEnrollmentSystem.csproj --startup-project .\StudentEnrollmentSystem.csproj --output-dir Data/Migrations`

## Notes For Team Members

- Reuse the shared models and `ApplicationDbContext` instead of creating duplicate tables.
- Keep your work inside your own controller and view folders as much as possible.
- Do not commit directly into another member's branch.
- Pull from `main` regularly so your branch stays updated.
- If you are unsure where your feature should go, start from the placeholder controller and matching view folder already prepared for you.
