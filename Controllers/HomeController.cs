using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using StudentEnrollmentSystem.Services;
using StudentEnrollmentSystem.ViewModels;

namespace StudentEnrollmentSystem.Controllers;

public class HomeController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var activeSemester = await context.Semesters
            .AsNoTracking()
            .Where(semester =>
                semester.Status == SemesterStatus.OpenForEnrollment &&
                semester.EnrollmentStartDate <= today &&
                semester.AddDropEndDate >= today)
            .OrderBy(semester => semester.SemesterStartDate)
            .FirstOrDefaultAsync();

        var openSectionCount = 0;
        var registrationCount = 0;
        var recentUpdateText = "Semester planning tools remain available even when enrollment windows are closed.";
        var windowSummary = "Please check with the academic office for the next enrollment and add / drop periods.";

        if (activeSemester is not null)
        {
            var state = SemesterTimeline.Describe(activeSemester, today);
            var sectionAvailability = await context.CourseSections
                .AsNoTracking()
                .Include(section => section.EnrollmentRecords)
                .Where(section => section.SemesterId == activeSemester.Id)
                .Select(section => new
                {
                    section.Capacity,
                    SeatsTaken = section.EnrollmentRecords.Count(record => record.Status == EnrollmentStatus.Enrolled)
                })
                .ToListAsync();

            openSectionCount = sectionAvailability.Count(section => section.SeatsTaken < section.Capacity);

            registrationCount = await context.EnrollmentRecords
                .AsNoTracking()
                .CountAsync(record =>
                    record.Status == EnrollmentStatus.Enrolled &&
                    record.CourseSection.SemesterId == activeSemester.Id);

            recentUpdateText = state.Phase switch
            {
                SemesterLifecyclePhase.Enrollment =>
                    $"Enrollment for {activeSemester.Name} is open. Students can confirm their timetable before classes start on {activeSemester.SemesterStartDate:dd MMM yyyy}.",
                SemesterLifecyclePhase.AddDrop =>
                    $"{activeSemester.Name} is already in session. Add / Drop remains available until {activeSemester.AddDropEndDate:dd MMM yyyy} for eligible programme sections.",
                _ =>
                    $"Semester planning for {activeSemester.Name} is available in the portal."
            };

            windowSummary = SemesterTimeline.FormatWindowSummary(activeSemester);
        }

        var model = new HomeIndexViewModel
        {
            PortalTitle = "MyCampus Student Self-Service",
            SemesterName = activeSemester?.Name ?? "Registration window currently unavailable",
            EnrollmentWindow = windowSummary,
            CatalogCourseCount = await context.Courses.AsNoTracking().CountAsync(),
            OpenSectionCount = openSectionCount,
            ActiveStudentCount = await context.StudentProfiles.AsNoTracking().CountAsync(),
            CurrentRegistrationCount = registrationCount,
            RecentUpdateText = recentUpdateText,
            DemoAccounts =
            [
                new DemoLoginViewModel { DisplayName = "Alice Tan", Email = SeedDataDefaults.DemoEmails[0], ProgramName = "Diploma in Software Engineering", SemesterName = "Semester 2 2026" },
                new DemoLoginViewModel { DisplayName = "Bob Kumar", Email = SeedDataDefaults.DemoEmails[1], ProgramName = "Diploma in Information Technology", SemesterName = "Semester 2 2026" },
                new DemoLoginViewModel { DisplayName = "Chloe Lim", Email = SeedDataDefaults.DemoEmails[2], ProgramName = "Diploma in Business Analytics", SemesterName = "Semester 3 2026" },
                new DemoLoginViewModel { DisplayName = "Daniel Wong", Email = SeedDataDefaults.DemoEmails[3], ProgramName = "Diploma in Cyber Security", SemesterName = "Semester 2 2026" },
                new DemoLoginViewModel { DisplayName = "Farah Hassan", Email = SeedDataDefaults.DemoEmails[4], ProgramName = "Diploma in Data Analytics", SemesterName = "Semester 3 2026" }
            ]
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
