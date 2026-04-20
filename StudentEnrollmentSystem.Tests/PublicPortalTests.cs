using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Controllers;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using StudentEnrollmentSystem.Services;
using StudentEnrollmentSystem.ViewModels;

namespace StudentEnrollmentSystem.Tests;

public class PublicPortalTests
{
    [Fact]
    public async Task HomeIndex_WhenActiveSemesterExists_ReturnsPortalStats()
    {
        await using var context = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var semester = new Semester
        {
            Code = "2026-S2",
            Name = "Semester 2 2026",
            Status = SemesterStatus.OpenForEnrollment,
            EnrollmentStartDate = today.AddDays(-10),
            EnrollmentEndDate = today.AddDays(-2),
            SemesterStartDate = today.AddDays(-1),
            AddDropEndDate = today.AddDays(7)
        };

        var course = new Course
        {
            Code = "CSC101",
            Title = "Intro to Programming",
            CreditHours = 3,
            EligibleProgrammeCodes = "SE"
        };

        var openSection = new CourseSection
        {
            Course = course,
            Semester = semester,
            SectionCode = "01",
            Capacity = 30,
            InstructorName = "Dr. Tan"
        };

        var student = new StudentProfile
        {
            ApplicationUserId = "user-1",
            StudentNumber = "ST001",
            FullName = "Alice Tan",
            Email = "alice@student.demo",
            ProgramName = "Software Engineering",
            ProgramCode = "SE",
            IntakeLabel = "January 2026",
            CurrentSemester = semester
        };

        context.AddRange(course, openSection, student);
        context.EnrollmentRecords.Add(new EnrollmentRecord
        {
            StudentProfile = student,
            CourseSection = openSection,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new HomeController(context);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HomeIndexViewModel>(view.Model);
        Assert.Equal("Semester 2 2026", model.SemesterName);
        Assert.Equal(1, model.CatalogCourseCount);
        Assert.Equal(1, model.OpenSectionCount);
        Assert.Equal(1, model.ActiveStudentCount);
        Assert.Equal(1, model.CurrentRegistrationCount);
        Assert.Contains("Add / Drop", model.EnrollmentWindow);
    }

    [Fact]
    public async Task EvaluationCreate_WhenModelIsValid_PersistsSubmissionAndRedirects()
    {
        await using var context = CreateContext();
        var controller = new EvaluationController(context);
        var model = new TeachingEvaluation
        {
            StudentName = "Alice Tan",
            StudentId = "ST001",
            CourseCode = "CSC101",
            LecturerName = "Dr. Tan",
            TeachingClarity = 5,
            Preparedness = 4,
            Engagement = 5,
            Comments = "Very clear explanations."
        };

        var result = await controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(EvaluationController.ThankYou), redirect.ActionName);

        var saved = await context.TeachingEvaluations.SingleAsync();
        Assert.Equal(model.StudentName, saved.StudentName);
        Assert.Equal(model.CourseCode, saved.CourseCode);
        Assert.Equal(model.TeachingClarity, saved.TeachingClarity);
    }

    [Fact]
    public async Task EvaluationCreate_WhenModelIsInvalid_ReturnsViewWithoutPersisting()
    {
        await using var context = CreateContext();
        var controller = new EvaluationController(context);
        controller.ModelState.AddModelError(nameof(TeachingEvaluation.StudentName), "Required");

        var result = await controller.Create(new TeachingEvaluation());

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<TeachingEvaluation>(view.Model);
        Assert.Empty(context.TeachingEvaluations);
    }

    [Fact]
    public async Task TimetableMatchingGet_ReturnsSeededMeetings()
    {
        await using var context = CreateContext();
        await SeedTimetableMeetingsAsync(context);
        var controller = new EnquiryController(context);

        var result = await controller.TimetableMatching();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TimetableMatchingViewModel>(view.Model);
        Assert.Equal(3, model.AvailableSections.Count);
    }

    [Fact]
    public async Task TimetableMatchingPost_WhenNothingSelected_ShowsFriendlyMessage()
    {
        await using var context = CreateContext();
        await SeedTimetableMeetingsAsync(context);
        var controller = new EnquiryController(context);

        var result = await controller.TimetableMatching(new TimetableMatchingViewModel());

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TimetableMatchingViewModel>(view.Model);
        Assert.True(model.HasChecked);
        Assert.Contains("Please select at least one timetable entry.", model.ClashMessages);
    }

    [Fact]
    public async Task TimetableMatchingPost_WhenMeetingsOverlap_AddsClashMessage()
    {
        await using var context = CreateContext();
        var meetingIds = await SeedTimetableMeetingsAsync(context);
        var controller = new EnquiryController(context);
        var model = new TimetableMatchingViewModel
        {
            AvailableSections =
            [
                new TimetableOptionViewModel { MeetingId = meetingIds[0], IsSelected = true },
                new TimetableOptionViewModel { MeetingId = meetingIds[1], IsSelected = true }
            ]
        };

        var result = await controller.TimetableMatching(model);

        var view = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<TimetableMatchingViewModel>(view.Model);
        Assert.Contains(returnedModel.ClashMessages, message => message.Contains("Clash detected", StringComparison.Ordinal));
    }

    [Fact]
    public void ProfileProtectionService_RoundTripsAndMasksAccountNumber()
    {
        var provider = DataProtectionProvider.Create("StudentEnrollmentSystem.Tests");
        var service = new ProfileProtectionService(provider);

        var protectedValue = service.Protect(" 1234567890 ");
        var roundTrip = service.Unprotect(protectedValue);

        Assert.Equal("1234567890", roundTrip);
        Assert.Equal("******7890", ProfileProtectionService.MaskAccountNumber(roundTrip));
        Assert.Equal("Not added", ProfileProtectionService.MaskAccountNumber(null));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<List<int>> SeedTimetableMeetingsAsync(ApplicationDbContext context)
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

        var courseA = new Course { Code = "CSC101", Title = "Intro to Programming", CreditHours = 3 };
        var courseB = new Course { Code = "MAT201", Title = "Discrete Mathematics", CreditHours = 3 };
        var courseC = new Course { Code = "ENG150", Title = "Academic Writing", CreditHours = 2 };

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

        var sectionC = new CourseSection
        {
            Course = courseC,
            Semester = semester,
            SectionCode = "03",
            Capacity = 20,
            InstructorName = "Mr. Ong",
            Meetings =
            [
                new SectionMeeting
                {
                    DayOfWeek = DayOfWeek.Tuesday,
                    StartTime = new TimeOnly(14, 0),
                    EndTime = new TimeOnly(16, 0),
                    Venue = "Room C103"
                }
            ]
        };

        context.AddRange(sectionA, sectionB, sectionC);
        await context.SaveChangesAsync();

        return await context.SectionMeetings
            .OrderBy(meeting => meeting.Id)
            .Select(meeting => meeting.Id)
            .ToListAsync();
    }
}
