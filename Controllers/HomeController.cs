using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using StudentEnrollmentSystem.ViewModels;

namespace StudentEnrollmentSystem.Controllers;

public class HomeController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var activeSemester = await context.Semesters
            .AsNoTracking()
            .SingleOrDefaultAsync(semester =>
                semester.Status == SemesterStatus.OpenForEnrollment &&
                semester.EnrollmentStartDate <= DateOnly.FromDateTime(DateTime.Today) &&
                semester.EnrollmentEndDate >= DateOnly.FromDateTime(DateTime.Today));

        var openSectionCount = 0;
        var registrationCount = 0;

        if (activeSemester is not null)
        {
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
        }

        var model = new HomeIndexViewModel
        {
            PortalTitle = "Student Self-Service Portal",
            SemesterName = activeSemester?.Name ?? "Registration window currently unavailable",
            EnrollmentWindow = activeSemester is null
                ? "Please check with the academic office for the next registration period."
                : $"{activeSemester.EnrollmentStartDate:dd MMM yyyy} - {activeSemester.EnrollmentEndDate:dd MMM yyyy}",
            CatalogCourseCount = await context.Courses.AsNoTracking().CountAsync(),
            OpenSectionCount = openSectionCount,
            ActiveStudentCount = await context.StudentProfiles.AsNoTracking().CountAsync(),
            CurrentRegistrationCount = registrationCount,
            RecentUpdateText = activeSemester is null
                ? "Course planning tools remain available even when the registration window is closed."
                : $"Registration for {activeSemester.Name} is open. Review available sections, confirm your timetable, and submit changes before the closing date.",
            DemoAccounts =
            [
                new DemoLoginViewModel { DisplayName = "Alice Tan", Email = SeedDataDefaults.DemoEmails[0] },
                new DemoLoginViewModel { DisplayName = "Bob Kumar", Email = SeedDataDefaults.DemoEmails[1] },
                new DemoLoginViewModel { DisplayName = "Chloe Lim", Email = SeedDataDefaults.DemoEmails[2] },
                new DemoLoginViewModel { DisplayName = "Daniel Wong", Email = SeedDataDefaults.DemoEmails[3] },
                new DemoLoginViewModel { DisplayName = "Farah Hassan", Email = SeedDataDefaults.DemoEmails[4] }
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
