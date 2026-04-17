using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using StudentEnrollmentSystem.Services;

namespace StudentEnrollmentSystem.Tests;

public class EnrollmentWorkflowTests
{
    [Fact]
    public async Task EnrollAsync_BlocksDuplicateActiveEnrollment()
    {
        await using var fixture = await TestFixture.CreateAsync();

        fixture.Context.EnrollmentRecords.Add(new EnrollmentRecord
        {
            StudentProfileId = fixture.PrimaryStudent.Id,
            CourseSectionId = fixture.ProgrammingSection.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        await fixture.Context.SaveChangesAsync();

        var service = new EnrollmentService(fixture.Context);

        var result = await service.EnrollAsync(fixture.PrimaryStudent.ApplicationUserId, fixture.ProgrammingSection.Id);

        Assert.False(result.Succeeded);
        Assert.Contains("already actively enrolled", result.Message);
    }

    [Fact]
    public async Task EnrollAsync_BlocksTimetableClash()
    {
        await using var fixture = await TestFixture.CreateAsync();

        fixture.Context.EnrollmentRecords.Add(new EnrollmentRecord
        {
            StudentProfileId = fixture.PrimaryStudent.Id,
            CourseSectionId = fixture.ProgrammingSection.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        await fixture.Context.SaveChangesAsync();

        var service = new EnrollmentService(fixture.Context);

        var result = await service.EnrollAsync(fixture.PrimaryStudent.ApplicationUserId, fixture.MathematicsSection.Id);

        Assert.False(result.Succeeded);
        Assert.Contains("Timetable clash", result.Message);
    }

    [Fact]
    public async Task EnrollAsync_BlocksFullSection()
    {
        await using var fixture = await TestFixture.CreateAsync();

        fixture.Context.EnrollmentRecords.Add(new EnrollmentRecord
        {
            StudentProfileId = fixture.SecondaryStudent.Id,
            CourseSectionId = fixture.HistorySection.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = DateTime.UtcNow.AddDays(-2)
        });
        await fixture.Context.SaveChangesAsync();

        var service = new EnrollmentService(fixture.Context);

        var result = await service.EnrollAsync(fixture.PrimaryStudent.ApplicationUserId, fixture.HistorySection.Id);

        Assert.False(result.Succeeded);
        Assert.Contains("already full", result.Message);
    }

    [Fact]
    public async Task EnrollAsync_CreatesEnrollmentAndAuditRecord()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var service = new EnrollmentService(fixture.Context);

        var result = await service.EnrollAsync(fixture.PrimaryStudent.ApplicationUserId, fixture.WritingSection.Id);

        Assert.True(result.Succeeded);

        var enrollment = await fixture.Context.EnrollmentRecords.SingleAsync(record =>
            record.StudentProfileId == fixture.PrimaryStudent.Id &&
            record.CourseSectionId == fixture.WritingSection.Id);

        var audit = await fixture.Context.AddDropAudits.SingleAsync(auditRecord =>
            auditRecord.StudentProfileId == fixture.PrimaryStudent.Id &&
            auditRecord.CourseSectionId == fixture.WritingSection.Id &&
            auditRecord.ActionType == AddDropActionType.Added);

        Assert.Equal(EnrollmentStatus.Enrolled, enrollment.Status);
        Assert.Equal("Online enrollment", audit.Remarks);
    }

    [Fact]
    public async Task DropCourseAsync_UpdatesStatusAndWritesAudit()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var enrollment = new EnrollmentRecord
        {
            StudentProfileId = fixture.PrimaryStudent.Id,
            CourseSectionId = fixture.ProgrammingSection.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        fixture.Context.EnrollmentRecords.Add(enrollment);
        await fixture.Context.SaveChangesAsync();

        var service = new AddDropService(fixture.Context, new EnrollmentService(fixture.Context));

        var result = await service.DropCourseAsync(
            fixture.PrimaryStudent.ApplicationUserId,
            enrollment.Id,
            "Switching to another elective");

        Assert.True(result.Succeeded);

        var updatedEnrollment = await fixture.Context.EnrollmentRecords.SingleAsync(record => record.Id == enrollment.Id);
        var dropAudit = await fixture.Context.AddDropAudits.SingleAsync(audit =>
            audit.StudentProfileId == fixture.PrimaryStudent.Id &&
            audit.CourseSectionId == fixture.ProgrammingSection.Id &&
            audit.ActionType == AddDropActionType.Dropped);

        Assert.Equal(EnrollmentStatus.Dropped, updatedEnrollment.Status);
        Assert.Equal("Switching to another elective", updatedEnrollment.DropReason);
        Assert.Equal("Switching to another elective", dropAudit.Remarks);
        Assert.NotNull(updatedEnrollment.DroppedAtUtc);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(ApplicationDbContext context)
        {
            Context = context;
        }

        public ApplicationDbContext Context { get; }

        public StudentProfile PrimaryStudent { get; private set; } = null!;

        public StudentProfile SecondaryStudent { get; private set; } = null!;

        public CourseSection ProgrammingSection { get; private set; } = null!;

        public CourseSection MathematicsSection { get; private set; } = null!;

        public CourseSection WritingSection { get; private set; } = null!;

        public CourseSection HistorySection { get; private set; } = null!;

        public static async Task<TestFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            var fixture = new TestFixture(context);
            await fixture.SeedAsync();
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
        }

        private async Task SeedAsync()
        {
            var semester = new Semester
            {
                Code = "CURRENT",
                Name = "Current Semester",
                Status = SemesterStatus.OpenForEnrollment,
                EnrollmentStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),
                EnrollmentEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7))
            };

            var programming = new Course { Code = "CSC101", Title = "Intro to Programming", CreditHours = 3 };
            var mathematics = new Course { Code = "MAT201", Title = "Discrete Mathematics", CreditHours = 3 };
            var writing = new Course { Code = "ENG150", Title = "Academic Writing", CreditHours = 2 };
            var history = new Course { Code = "HIS220", Title = "Civilisation", CreditHours = 2 };

            ProgrammingSection = new CourseSection
            {
                Course = programming,
                Semester = semester,
                SectionCode = "01",
                Capacity = 25,
                InstructorName = "Lecturer A",
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

            MathematicsSection = new CourseSection
            {
                Course = mathematics,
                Semester = semester,
                SectionCode = "02",
                Capacity = 20,
                InstructorName = "Lecturer B",
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

            WritingSection = new CourseSection
            {
                Course = writing,
                Semester = semester,
                SectionCode = "03",
                Capacity = 15,
                InstructorName = "Lecturer C",
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

            HistorySection = new CourseSection
            {
                Course = history,
                Semester = semester,
                SectionCode = "04",
                Capacity = 1,
                InstructorName = "Lecturer D",
                Meetings =
                [
                    new SectionMeeting
                    {
                        DayOfWeek = DayOfWeek.Wednesday,
                        StartTime = new TimeOnly(8, 30),
                        EndTime = new TimeOnly(10, 30),
                        Venue = "Room A101"
                    }
                ]
            };

            var primaryUser = new ApplicationUser
            {
                Id = "user-1",
                UserName = "alice@student.demo",
                Email = "alice@student.demo",
                NormalizedUserName = "ALICE@STUDENT.DEMO",
                NormalizedEmail = "ALICE@STUDENT.DEMO",
                DisplayName = "Alice Tan"
            };

            var secondaryUser = new ApplicationUser
            {
                Id = "user-2",
                UserName = "bob@student.demo",
                Email = "bob@student.demo",
                NormalizedUserName = "BOB@STUDENT.DEMO",
                NormalizedEmail = "BOB@STUDENT.DEMO",
                DisplayName = "Bob Kumar"
            };

            PrimaryStudent = new StudentProfile
            {
                ApplicationUserId = primaryUser.Id,
                ApplicationUser = primaryUser,
                StudentNumber = "ST001",
                FullName = "Alice Tan",
                Email = "alice@student.demo",
                ProgramName = "Software Engineering",
                IntakeLabel = "2026",
                CurrentSemester = semester
            };

            SecondaryStudent = new StudentProfile
            {
                ApplicationUserId = secondaryUser.Id,
                ApplicationUser = secondaryUser,
                StudentNumber = "ST002",
                FullName = "Bob Kumar",
                Email = "bob@student.demo",
                ProgramName = "Information Technology",
                IntakeLabel = "2026",
                CurrentSemester = semester
            };

            Context.AddRange(primaryUser, secondaryUser, PrimaryStudent, SecondaryStudent, ProgrammingSection, MathematicsSection, WritingSection, HistorySection);
            await Context.SaveChangesAsync();
        }
    }
}
