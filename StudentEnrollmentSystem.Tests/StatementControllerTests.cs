using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Controllers;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;

namespace StudentEnrollmentSystem.Tests;

public class StatementControllerTests
{
    [Fact]
    public async Task Index_WhenUserIsNotAuthenticated_ReturnsChallenge()
    {
        await using var context = CreateContext();
        var controller = new StatementController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Index();

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Index_WhenStudentProfileExists_ReturnsViewWithStudent()
    {
        await using var context = CreateContext();
        var student = await SeedStudentScheduleAsync(context, "user-1");
        var controller = CreateController(context, "user-1");

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StudentProfile>(view.Model);
        Assert.Equal(student.Id, model.Id);
        Assert.Equal(student.CurrentSemesterId, controller.ViewBag.CurrentSemesterId);
        Assert.Equal(student.CurrentSemester?.Name, controller.ViewBag.ActiveSemester);
    }

    [Fact]
    public async Task Timetable_WhenStudentProfileExists_ReturnsViewAndClashFilterFlag()
    {
        await using var context = CreateContext();
        await SeedStudentScheduleAsync(context, "user-1");
        var controller = CreateController(context, "user-1");

        var result = await controller.Timetable(clashOnly: true);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StudentProfile>(view.Model);
        Assert.Equal("Semester 2 2026", controller.ViewBag.ActiveSemester);
        Assert.True((bool)controller.ViewBag.ClashOnly);
        Assert.NotEmpty(model.EnrollmentRecords);
    }

    private static StatementController CreateController(ApplicationDbContext context, string userId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId)
            ], "TestAuth"))
        };

        return new StatementController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<StudentProfile> SeedStudentScheduleAsync(ApplicationDbContext context, string userId)
    {
        var semester = new Semester
        {
            Code = "2026-S2",
            Name = "Semester 2 2026",
            Status = SemesterStatus.OpenForEnrollment,
            EnrollmentStartDate = new DateOnly(2026, 3, 9),
            EnrollmentEndDate = new DateOnly(2026, 4, 10),
            SemesterStartDate = new DateOnly(2026, 4, 13),
            AddDropEndDate = new DateOnly(2026, 5, 1)
        };

        var student = new StudentProfile
        {
            ApplicationUserId = userId,
            StudentNumber = "ST001",
            FullName = "Alice Tan",
            Email = "alice@student.demo",
            ProgramName = "Software Engineering",
            ProgramCode = "SE",
            IntakeLabel = "January 2026",
            CurrentSemester = semester
        };

        var courseA = new Course { Code = "CSC101", Title = "Intro to Programming", CreditHours = 3 };
        var courseB = new Course { Code = "MAT201", Title = "Discrete Mathematics", CreditHours = 3 };

        var sectionA = new CourseSection
        {
            Course = courseA,
            Semester = semester,
            SectionCode = "01",
            Capacity = 30,
            InstructorName = "Dr. Tan",
            Meetings =
            [
                new SectionMeeting
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(11, 0),
                    Venue = "Lab 1"
                }
            ]
        };

        var sectionB = new CourseSection
        {
            Course = courseB,
            Semester = semester,
            SectionCode = "02",
            Capacity = 25,
            InstructorName = "Ms. Lim",
            Meetings =
            [
                new SectionMeeting
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0),
                    Venue = "Room B201"
                }
            ]
        };

        context.AddRange(student, sectionA, sectionB);
        context.EnrollmentRecords.AddRange(
            new EnrollmentRecord
            {
                StudentProfile = student,
                CourseSection = sectionA,
                Status = EnrollmentStatus.Enrolled,
                EnrolledAtUtc = DateTime.UtcNow.AddDays(-1)
            },
            new EnrollmentRecord
            {
                StudentProfile = student,
                CourseSection = sectionB,
                Status = EnrollmentStatus.Enrolled,
                EnrolledAtUtc = DateTime.UtcNow.AddDays(-1)
            });

        await context.SaveChangesAsync();

        return await context.StudentProfiles
            .Include(profile => profile.CurrentSemester)
            .FirstAsync();
    }
}
