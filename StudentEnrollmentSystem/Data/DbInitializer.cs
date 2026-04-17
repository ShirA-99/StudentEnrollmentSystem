using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Models;

namespace StudentEnrollmentSystem.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();

        if (!await context.Semesters.AnyAsync())
        {
            await SeedAcademicDataAsync(context);
        }

        if (!await context.Users.AnyAsync())
        {
            await SeedUsersAsync(userManager);
        }

        if (!await context.StudentProfiles.AnyAsync())
        {
            await SeedStudentProfilesAsync(context, userManager);
        }

        if (!await context.EnrollmentRecords.AnyAsync())
        {
            await SeedEnrollmentHistoryAsync(context);
        }
    }

    private static async Task SeedAcademicDataAsync(ApplicationDbContext context)
    {
        var semester = new Semester
        {
            Code = "2026-T2",
            Name = "Trimester 2 2026",
            Status = SemesterStatus.OpenForEnrollment,
            EnrollmentStartDate = new DateOnly(2026, 4, 1),
            EnrollmentEndDate = new DateOnly(2026, 5, 31)
        };

        var courses = new List<Course>
        {
            new() { Code = "CSC101", Title = "Introduction to Programming", CreditHours = 3 },
            new() { Code = "MAT201", Title = "Discrete Mathematics", CreditHours = 3 },
            new() { Code = "ENG150", Title = "Academic Writing", CreditHours = 2 },
            new() { Code = "HIS220", Title = "Malaysian Civilisation", CreditHours = 2 }
        };

        semester.CourseSections =
        [
            new CourseSection
            {
                Course = courses[0],
                SectionCode = "01",
                Capacity = 25,
                InstructorName = "Dr. Nur Izzati",
                Meetings =
                [
                    new SectionMeeting
                    {
                        DayOfWeek = DayOfWeek.Monday,
                        StartTime = new TimeOnly(9, 0),
                        EndTime = new TimeOnly(11, 0),
                        Venue = "Lab 2"
                    },
                    new SectionMeeting
                    {
                        DayOfWeek = DayOfWeek.Thursday,
                        StartTime = new TimeOnly(9, 0),
                        EndTime = new TimeOnly(10, 0),
                        Venue = "Lab 2"
                    }
                ]
            },
            new CourseSection
            {
                Course = courses[1],
                SectionCode = "01",
                Capacity = 20,
                InstructorName = "Ms. Tan Mei Ling",
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
            },
            new CourseSection
            {
                Course = courses[2],
                SectionCode = "02",
                Capacity = 18,
                InstructorName = "Mr. Haris Ismail",
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
            },
            new CourseSection
            {
                Course = courses[3],
                SectionCode = "01",
                Capacity = 1,
                InstructorName = "Dr. Leong Wei Han",
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
            }
        ];

        await context.Semesters.AddAsync(semester);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        var demoUsers = new[]
        {
            new ApplicationUser
            {
                UserName = SeedDataDefaults.DemoEmails[0],
                Email = SeedDataDefaults.DemoEmails[0],
                EmailConfirmed = true,
                DisplayName = "Alice Tan"
            },
            new ApplicationUser
            {
                UserName = SeedDataDefaults.DemoEmails[1],
                Email = SeedDataDefaults.DemoEmails[1],
                EmailConfirmed = true,
                DisplayName = "Bob Kumar"
            },
            new ApplicationUser
            {
                UserName = SeedDataDefaults.DemoEmails[2],
                Email = SeedDataDefaults.DemoEmails[2],
                EmailConfirmed = true,
                DisplayName = "Chloe Lim"
            }
        };

        foreach (var user in demoUsers)
        {
            var result = await userManager.CreateAsync(user, SeedDataDefaults.DemoPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create seeded user {user.Email}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }
    }

    private static async Task SeedStudentProfilesAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        var activeSemester = await context.Semesters.SingleAsync();
        var users = await userManager.Users.ToListAsync();

        await context.StudentProfiles.AddRangeAsync(
            new StudentProfile
            {
                ApplicationUserId = users.Single(user => user.Email == SeedDataDefaults.DemoEmails[0]).Id,
                StudentNumber = "ST2026001",
                FullName = "Alice Tan",
                Email = SeedDataDefaults.DemoEmails[0],
                ProgramName = "Diploma in Software Engineering",
                IntakeLabel = "January 2026",
                CurrentSemesterId = activeSemester.Id
            },
            new StudentProfile
            {
                ApplicationUserId = users.Single(user => user.Email == SeedDataDefaults.DemoEmails[1]).Id,
                StudentNumber = "ST2026002",
                FullName = "Bob Kumar",
                Email = SeedDataDefaults.DemoEmails[1],
                ProgramName = "Diploma in Information Technology",
                IntakeLabel = "January 2026",
                CurrentSemesterId = activeSemester.Id
            },
            new StudentProfile
            {
                ApplicationUserId = users.Single(user => user.Email == SeedDataDefaults.DemoEmails[2]).Id,
                StudentNumber = "ST2026003",
                FullName = "Chloe Lim",
                Email = SeedDataDefaults.DemoEmails[2],
                ProgramName = "Diploma in Business Analytics",
                IntakeLabel = "January 2026",
                CurrentSemesterId = activeSemester.Id
            });

        await context.SaveChangesAsync();
    }

    private static async Task SeedEnrollmentHistoryAsync(ApplicationDbContext context)
    {
        var students = await context.StudentProfiles.OrderBy(profile => profile.StudentNumber).ToListAsync();
        var activeSemester = await context.Semesters.SingleAsync();
        var sections = await context.CourseSections
            .Include(section => section.Course)
            .Where(section => section.SemesterId == activeSemester.Id)
            .ToListAsync();

        var alice = students.Single(student => student.Email == SeedDataDefaults.DemoEmails[0]);
        var bob = students.Single(student => student.Email == SeedDataDefaults.DemoEmails[1]);

        var programmingSection = sections.Single(section => section.Course.Code == "CSC101");
        var writingSection = sections.Single(section => section.Course.Code == "ENG150");
        var historySection = sections.Single(section => section.Course.Code == "HIS220");

        var enrolledAt = DateTime.UtcNow.AddDays(-5);
        var droppedAt = DateTime.UtcNow.AddDays(-1);

        await context.EnrollmentRecords.AddRangeAsync(
            new EnrollmentRecord
            {
                StudentProfileId = alice.Id,
                CourseSectionId = programmingSection.Id,
                Status = EnrollmentStatus.Enrolled,
                EnrolledAtUtc = enrolledAt
            },
            new EnrollmentRecord
            {
                StudentProfileId = alice.Id,
                CourseSectionId = writingSection.Id,
                Status = EnrollmentStatus.Dropped,
                EnrolledAtUtc = enrolledAt.AddDays(1),
                DroppedAtUtc = droppedAt,
                DropReason = "Swapped to another elective"
            },
            new EnrollmentRecord
            {
                StudentProfileId = bob.Id,
                CourseSectionId = historySection.Id,
                Status = EnrollmentStatus.Enrolled,
                EnrolledAtUtc = enrolledAt.AddDays(2)
            });

        await context.AddDropAudits.AddRangeAsync(
            new AddDropAudit
            {
                StudentProfileId = alice.Id,
                CourseSectionId = programmingSection.Id,
                ActionType = AddDropActionType.Added,
                ActionAtUtc = enrolledAt,
                Remarks = "Seeded initial registration"
            },
            new AddDropAudit
            {
                StudentProfileId = alice.Id,
                CourseSectionId = writingSection.Id,
                ActionType = AddDropActionType.Added,
                ActionAtUtc = enrolledAt.AddDays(1),
                Remarks = "Seeded initial registration"
            },
            new AddDropAudit
            {
                StudentProfileId = alice.Id,
                CourseSectionId = writingSection.Id,
                ActionType = AddDropActionType.Dropped,
                ActionAtUtc = droppedAt,
                Remarks = "Swapped to another elective"
            },
            new AddDropAudit
            {
                StudentProfileId = bob.Id,
                CourseSectionId = historySection.Id,
                ActionType = AddDropActionType.Added,
                ActionAtUtc = enrolledAt.AddDays(2),
                Remarks = "Seeded section capacity scenario"
            });

        await context.SaveChangesAsync();
    }
}
