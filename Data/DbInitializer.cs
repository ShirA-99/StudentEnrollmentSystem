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
        await EnsureAcademicDataAsync(context);
        await EnsureUsersAsync(userManager);
        await EnsureStudentProfilesAsync(context, userManager);
        await EnsureEnrollmentHistoryAsync(context);
    }

    private static async Task EnsureAcademicDataAsync(ApplicationDbContext context)
    {
        var courseSeeds = CreateCourseCatalog();
        var existingCourses = await context.Courses.ToDictionaryAsync(course => course.Code);

        foreach (var seed in courseSeeds)
        {
            if (existingCourses.TryGetValue(seed.Code, out var existingCourse))
            {
                existingCourse.Title = seed.Title;
                existingCourse.CreditHours = seed.CreditHours;
                existingCourse.EligibleProgrammeCodes = seed.EligibleProgrammeCodes;
            }
            else
            {
                await context.Courses.AddAsync(new Course
                {
                    Code = seed.Code,
                    Title = seed.Title,
                    CreditHours = seed.CreditHours,
                    EligibleProgrammeCodes = seed.EligibleProgrammeCodes
                });
            }
        }

        await context.SaveChangesAsync();

        existingCourses = await context.Courses.ToDictionaryAsync(course => course.Code);

        var semesterSeeds = CreateSemesterSeeds();
        var existingSemesters = await context.Semesters.ToDictionaryAsync(semester => semester.Code);

        foreach (var seed in semesterSeeds)
        {
            if (!existingSemesters.TryGetValue(seed.Code, out var existingSemester))
            {
                var legacyCode = GetLegacyTrimesterCode(seed.Code);
                if (legacyCode is not null && existingSemesters.TryGetValue(legacyCode, out existingSemester))
                {
                    existingSemesters.Remove(legacyCode);
                    existingSemester.Code = seed.Code;
                    existingSemesters[seed.Code] = existingSemester;
                }
            }

            if (existingSemester is not null)
            {
                existingSemester.Name = seed.Name;
                existingSemester.Status = seed.Status;
                existingSemester.EnrollmentStartDate = seed.EnrollmentStartDate;
                existingSemester.EnrollmentEndDate = seed.EnrollmentEndDate;
                existingSemester.SemesterStartDate = seed.SemesterStartDate;
                existingSemester.AddDropEndDate = seed.AddDropEndDate;
            }
            else
            {
                await context.Semesters.AddAsync(new Semester
                {
                    Code = seed.Code,
                    Name = seed.Name,
                    Status = seed.Status,
                    EnrollmentStartDate = seed.EnrollmentStartDate,
                    EnrollmentEndDate = seed.EnrollmentEndDate,
                    SemesterStartDate = seed.SemesterStartDate,
                    AddDropEndDate = seed.AddDropEndDate
                });
            }
        }

        await context.SaveChangesAsync();

        existingSemesters = await context.Semesters.ToDictionaryAsync(semester => semester.Code);

        var existingSections = await context.CourseSections
            .Include(section => section.Course)
            .Include(section => section.Semester)
            .Include(section => section.Meetings)
            .ToListAsync();

        var existingSectionMap = existingSections.ToDictionary(
            section => BuildSectionKey(section.Semester.Code, section.Course.Code, section.SectionCode));

        foreach (var seed in CreateSectionSeeds())
        {
            var sectionKey = BuildSectionKey(seed.SemesterCode, seed.CourseCode, seed.SectionCode);

            if (!existingSectionMap.TryGetValue(sectionKey, out var section))
            {
                await context.CourseSections.AddAsync(new CourseSection
                {
                    CourseId = existingCourses[seed.CourseCode].Id,
                    SemesterId = existingSemesters[seed.SemesterCode].Id,
                    SectionCode = seed.SectionCode,
                    Capacity = seed.Capacity,
                    InstructorName = seed.InstructorName,
                    Meetings = seed.Meetings.Select(CreateMeetingEntity).ToList()
                });

                continue;
            }

            section.Capacity = seed.Capacity;
            section.InstructorName = seed.InstructorName;

            if (SchedulesDiffer(section.Meetings, seed.Meetings))
            {
                context.SectionMeetings.RemoveRange(section.Meetings);
                section.Meetings = seed.Meetings.Select(CreateMeetingEntity).ToList();
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureUsersAsync(UserManager<ApplicationUser> userManager)
    {
        foreach (var seed in CreateUserSeeds())
        {
            var user = await userManager.FindByEmailAsync(seed.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    EmailConfirmed = true,
                    DisplayName = seed.DisplayName
                };

                var createResult = await userManager.CreateAsync(user, SeedDataDefaults.DemoPassword);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create seeded user {seed.Email}: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
                }

                continue;
            }

            user.UserName = seed.Email;
            user.Email = seed.Email;
            user.EmailConfirmed = true;
            user.DisplayName = seed.DisplayName;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to update seeded user {seed.Email}: {string.Join(", ", updateResult.Errors.Select(error => error.Description))}");
            }
        }
    }

    private static async Task EnsureStudentProfilesAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        var semesters = await context.Semesters.ToDictionaryAsync(semester => semester.Code);
        var users = await userManager.Users.ToDictionaryAsync(user => user.Email!);
        var existingProfiles = await context.StudentProfiles.ToDictionaryAsync(profile => profile.Email);

        foreach (var seed in CreateUserSeeds())
        {
            if (!users.TryGetValue(seed.Email, out var user))
            {
                throw new InvalidOperationException($"Seeded user {seed.Email} was not found after user initialization.");
            }

            if (existingProfiles.TryGetValue(seed.Email, out var profile))
            {
                profile.ApplicationUserId = user.Id;
                profile.StudentNumber = seed.StudentNumber;
                profile.FullName = seed.DisplayName;
                profile.Email = seed.Email;
                profile.ProgramName = seed.ProgramName;
                profile.ProgramCode = seed.ProgramCode;
                profile.IntakeLabel = seed.IntakeLabel;
                profile.CurrentSemesterId = semesters[seed.CurrentSemesterCode].Id;
            }
            else
            {
                await context.StudentProfiles.AddAsync(new StudentProfile
                {
                    ApplicationUserId = user.Id,
                    StudentNumber = seed.StudentNumber,
                    FullName = seed.DisplayName,
                    Email = seed.Email,
                    ProgramName = seed.ProgramName,
                    ProgramCode = seed.ProgramCode,
                    IntakeLabel = seed.IntakeLabel,
                    CurrentSemesterId = semesters[seed.CurrentSemesterCode].Id
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureEnrollmentHistoryAsync(ApplicationDbContext context)
    {
        var students = await context.StudentProfiles.ToDictionaryAsync(profile => profile.Email);
        var sections = await context.CourseSections
            .Include(section => section.Course)
            .Include(section => section.Semester)
            .ToListAsync();

        var sectionMap = sections.ToDictionary(
            section => BuildSectionKey(section.Semester.Code, section.Course.Code, section.SectionCode));

        var existingRecords = await context.EnrollmentRecords.ToListAsync();
        var existingRecordKeys = existingRecords
            .GroupBy(record => $"{record.StudentProfileId}:{record.CourseSectionId}:{record.Status}")
            .ToDictionary(group => group.Key, group => group.First());

        var existingAuditKeys = await context.AddDropAudits
            .Select(audit => $"{audit.StudentProfileId}:{audit.CourseSectionId}:{audit.ActionType}:{audit.ActionAtUtc.Ticks}")
            .ToHashSetAsync();

        var enrollmentRecordsToAdd = new List<EnrollmentRecord>();
        var auditsToAdd = new List<AddDropAudit>();

        foreach (var seed in CreateEnrollmentSeeds())
        {
            if (!students.TryGetValue(seed.StudentEmail, out var student))
            {
                throw new InvalidOperationException($"Student profile for {seed.StudentEmail} was not found.");
            }

            if (!sectionMap.TryGetValue(seed.SectionKey, out var section))
            {
                throw new InvalidOperationException($"Section {seed.SectionKey} was not found during seed initialization.");
            }

            var status = seed.DroppedAtUtc.HasValue ? EnrollmentStatus.Dropped : EnrollmentStatus.Enrolled;
            var recordKey = $"{student.Id}:{section.Id}:{status}";

            if (!existingRecordKeys.ContainsKey(recordKey))
            {
                var record = new EnrollmentRecord
                {
                    StudentProfileId = student.Id,
                    CourseSectionId = section.Id,
                    Status = status,
                    EnrolledAtUtc = seed.AddedAtUtc,
                    DroppedAtUtc = seed.DroppedAtUtc,
                    DropReason = seed.DropReason
                };

                enrollmentRecordsToAdd.Add(record);
                existingRecordKeys[recordKey] = record;
            }

            AddAuditIfMissing(
                existingAuditKeys,
                auditsToAdd,
                student.Id,
                section.Id,
                AddDropActionType.Added,
                seed.AddedAtUtc,
                seed.AddedRemarks);

            if (seed.DroppedAtUtc.HasValue)
            {
                AddAuditIfMissing(
                    existingAuditKeys,
                    auditsToAdd,
                    student.Id,
                    section.Id,
                    AddDropActionType.Dropped,
                    seed.DroppedAtUtc.Value,
                    seed.DropReason ?? "Dropped during registration adjustment");
            }
        }

        if (enrollmentRecordsToAdd.Count > 0)
        {
            await context.EnrollmentRecords.AddRangeAsync(enrollmentRecordsToAdd);
        }

        if (auditsToAdd.Count > 0)
        {
            await context.AddDropAudits.AddRangeAsync(auditsToAdd);
        }

        await context.SaveChangesAsync();
    }

    private static void AddAuditIfMissing(
        ISet<string> existingAuditKeys,
        ICollection<AddDropAudit> auditsToAdd,
        int studentProfileId,
        int courseSectionId,
        AddDropActionType actionType,
        DateTime actionAtUtc,
        string remarks)
    {
        var auditKey = $"{studentProfileId}:{courseSectionId}:{actionType}:{actionAtUtc.Ticks}";
        if (!existingAuditKeys.Add(auditKey))
        {
            return;
        }

        auditsToAdd.Add(new AddDropAudit
        {
            StudentProfileId = studentProfileId,
            CourseSectionId = courseSectionId,
            ActionType = actionType,
            ActionAtUtc = actionAtUtc,
            Remarks = remarks
        });
    }

    private static bool SchedulesDiffer(IEnumerable<SectionMeeting> existingMeetings, IEnumerable<MeetingSeed> seededMeetings)
    {
        var existingMeetingList = existingMeetings.ToList();
        var seededMeetingList = seededMeetings.ToList();

        if (existingMeetingList.Count != seededMeetingList.Count)
        {
            return true;
        }

        var existingSchedule = existingMeetingList
            .OrderBy(meeting => meeting.DayOfWeek)
            .ThenBy(meeting => meeting.StartTime)
            .Select(meeting => $"{meeting.DayOfWeek}:{meeting.StartTime}:{meeting.EndTime}:{meeting.Venue}")
            .ToList();

        var seededSchedule = seededMeetingList
            .OrderBy(meeting => meeting.DayOfWeek)
            .ThenBy(meeting => new TimeOnly(meeting.StartHour, meeting.StartMinute))
            .Select(meeting =>
                $"{meeting.DayOfWeek}:{new TimeOnly(meeting.StartHour, meeting.StartMinute)}:{new TimeOnly(meeting.EndHour, meeting.EndMinute)}:{meeting.Venue}")
            .ToList();

        return !existingSchedule.SequenceEqual(seededSchedule);
    }

    private static SectionMeeting CreateMeetingEntity(MeetingSeed seed)
    {
        return new SectionMeeting
        {
            DayOfWeek = seed.DayOfWeek,
            StartTime = new TimeOnly(seed.StartHour, seed.StartMinute),
            EndTime = new TimeOnly(seed.EndHour, seed.EndMinute),
            Venue = seed.Venue
        };
    }

    private static string BuildSectionKey(string semesterCode, string courseCode, string sectionCode)
        => $"{semesterCode}:{courseCode}:{sectionCode}";

    private static string SemesterCode(int year, int semesterNumber)
        => $"{year}-S{semesterNumber}";

    private static string? GetLegacyTrimesterCode(string semesterCode)
    {
        if (!semesterCode.Contains("-S", StringComparison.Ordinal))
        {
            return null;
        }

        return semesterCode.Replace("-S", "-T", StringComparison.Ordinal);
    }

    private static List<CourseSeed> CreateCourseCatalog()
    {
        return
        [
            new CourseSeed("CSC101", "Introduction to Programming", 3, "SE,IT"),
            new CourseSeed("CSC102", "Web Development Fundamentals", 3, "SE,IT"),
            new CourseSeed("CSC201", "Object-Oriented Programming", 3, "SE"),
            new CourseSeed("CSC230", "Database Systems", 3, "SE,IT,DA"),
            new CourseSeed("CSC240", "Computer Networks", 3, "IT,CY"),
            new CourseSeed("CSC245", "Cloud Fundamentals", 3, "IT,CY"),
            new CourseSeed("CSC310", "Data Structures and Algorithms", 3, "SE"),
            new CourseSeed("AIS260", "Applied Business Analytics", 3, "BA,DA"),
            new CourseSeed("DAT250", "Data Visualization", 3, "BA,DA"),
            new CourseSeed("CLD270", "Cloud Infrastructure Services", 3, "IT,CY"),
            new CourseSeed("CYB220", "Fundamentals of Cyber Security", 3, "IT,CY"),
            new CourseSeed("MOB230", "Mobile Application Development", 3, "SE"),
            new CourseSeed("UXD210", "User Experience Design", 3, "SE,DA"),
            new CourseSeed("MAT201", "Discrete Mathematics", 3, string.Empty),
            new CourseSeed("MAT210", "Applied Calculus", 3, string.Empty),
            new CourseSeed("STA210", "Business Statistics", 3, "BA,DA"),
            new CourseSeed("ENG150", "Academic Writing", 2, string.Empty),
            new CourseSeed("COM110", "Communication Skills", 2, string.Empty),
            new CourseSeed("HIS220", "Malaysian Civilisation", 2, string.Empty),
            new CourseSeed("LAW160", "Business Law", 2, "BA"),
            new CourseSeed("ACC110", "Principles of Accounting", 3, "BA,DA"),
            new CourseSeed("BUS205", "Entrepreneurship and Innovation", 3, "BA"),
            new CourseSeed("ECO120", "Microeconomics", 3, "BA,DA"),
            new CourseSeed("FIN215", "Personal Finance and Banking", 3, "BA,DA"),
            new CourseSeed("MKT225", "Digital Marketing Fundamentals", 3, "BA,DA")
        ];
    }

    private static List<SemesterSeed> CreateSemesterSeeds()
    {
        return
        [
            new SemesterSeed(SemesterCode(2024, 3), "Semester 3 2024", SemesterStatus.Closed, new DateOnly(2024, 8, 12), new DateOnly(2024, 8, 30), new DateOnly(2024, 9, 2), new DateOnly(2024, 9, 20)),
            new SemesterSeed(SemesterCode(2025, 1), "Semester 1 2025", SemesterStatus.Closed, new DateOnly(2024, 12, 9), new DateOnly(2025, 1, 3), new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 24)),
            new SemesterSeed(SemesterCode(2025, 2), "Semester 2 2025", SemesterStatus.Closed, new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 28), new DateOnly(2025, 4, 1), new DateOnly(2025, 4, 18)),
            new SemesterSeed(SemesterCode(2025, 3), "Semester 3 2025", SemesterStatus.Closed, new DateOnly(2025, 8, 11), new DateOnly(2025, 8, 29), new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 19)),
            new SemesterSeed(SemesterCode(2026, 1), "Semester 1 2026", SemesterStatus.Closed, new DateOnly(2025, 12, 8), new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 23)),
            new SemesterSeed(SemesterCode(2026, 2), "Semester 2 2026", SemesterStatus.OpenForEnrollment, new DateOnly(2026, 3, 9), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 13), new DateOnly(2026, 5, 1)),
            new SemesterSeed(SemesterCode(2026, 3), "Semester 3 2026", SemesterStatus.OpenForEnrollment, new DateOnly(2026, 4, 14), new DateOnly(2026, 5, 22), new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 19))
        ];
    }

    private static List<SectionSeed> CreateSectionSeeds()
    {
        var s2024s3 = SemesterCode(2024, 3);
        var s2025s1 = SemesterCode(2025, 1);
        var s2025s2 = SemesterCode(2025, 2);
        var s2025s3 = SemesterCode(2025, 3);
        var s2026s1 = SemesterCode(2026, 1);
        var s2026s2 = SemesterCode(2026, 2);
        var s2026s3 = SemesterCode(2026, 3);

        return
        [
            new SectionSeed(s2024s3, "ENG150", "01", 40, "Ms. Farina Halim", [Meeting(DayOfWeek.Monday, 9, 0, 11, 0, "Room B104")]),
            new SectionSeed(s2024s3, "BUS205", "01", 35, "Mr. Daniel Cho", [Meeting(DayOfWeek.Tuesday, 10, 0, 12, 0, "Room C201")]),
            new SectionSeed(s2024s3, "CSC102", "01", 24, "Ms. Melissa Wong", [Meeting(DayOfWeek.Wednesday, 14, 0, 16, 0, "Lab 3")]),
            new SectionSeed(s2024s3, "MAT210", "01", 30, "Dr. Kelvin Lo", [Meeting(DayOfWeek.Thursday, 9, 0, 11, 0, "Room A204")]),
            new SectionSeed(s2024s3, "ECO120", "01", 45, "Ms. Sabrina Ooi", [Meeting(DayOfWeek.Friday, 11, 0, 13, 0, "Room D102")]),
            new SectionSeed(s2024s3, "COM110", "01", 42, "Ms. Anis Safia", [Meeting(DayOfWeek.Friday, 14, 0, 16, 0, "Room B106")]),
            new SectionSeed(s2024s3, "LAW160", "01", 38, "Mr. Simon Yap", [Meeting(DayOfWeek.Wednesday, 10, 0, 12, 0, "Room C105")]),

            new SectionSeed(s2025s1, "CSC101", "01", 28, "Dr. Nur Izzati", [Meeting(DayOfWeek.Monday, 8, 30, 10, 30, "Lab 2")]),
            new SectionSeed(s2025s1, "CSC240", "01", 32, "Mr. Adrian Lee", [Meeting(DayOfWeek.Tuesday, 13, 0, 15, 0, "Room C302")]),
            new SectionSeed(s2025s1, "STA210", "01", 36, "Ms. Tan Mei Ling", [Meeting(DayOfWeek.Wednesday, 10, 0, 12, 0, "Room A110")]),
            new SectionSeed(s2025s1, "ACC110", "01", 40, "Mr. Hafiz Jamal", [Meeting(DayOfWeek.Thursday, 14, 0, 16, 0, "Room B210")]),
            new SectionSeed(s2025s1, "HIS220", "01", 42, "Dr. Leong Wei Han", [Meeting(DayOfWeek.Friday, 9, 0, 11, 0, "Room A101")]),
            new SectionSeed(s2025s1, "MAT201", "01", 30, "Dr. Priya Menon", [Meeting(DayOfWeek.Friday, 14, 0, 16, 0, "Room B201")]),
            new SectionSeed(s2025s1, "FIN215", "01", 34, "Ms. Irene Goh", [Meeting(DayOfWeek.Tuesday, 9, 0, 11, 0, "Room D205")]),
            new SectionSeed(s2025s1, "COM110", "01", 40, "Ms. Anis Safia", [Meeting(DayOfWeek.Monday, 14, 0, 16, 0, "Room B106")]),

            new SectionSeed(s2025s2, "ACC110", "01", 40, "Mr. Hafiz Jamal", [Meeting(DayOfWeek.Monday, 10, 0, 12, 0, "Room B210")]),
            new SectionSeed(s2025s2, "BUS205", "01", 35, "Mr. Daniel Cho", [Meeting(DayOfWeek.Tuesday, 14, 0, 16, 0, "Room C201")]),
            new SectionSeed(s2025s2, "ECO120", "01", 45, "Ms. Sabrina Ooi", [Meeting(DayOfWeek.Wednesday, 8, 30, 10, 30, "Room D102")]),
            new SectionSeed(s2025s2, "MAT210", "01", 30, "Dr. Kelvin Lo", [Meeting(DayOfWeek.Thursday, 9, 0, 11, 0, "Room A204")]),
            new SectionSeed(s2025s2, "ENG150", "01", 38, "Ms. Farina Halim", [Meeting(DayOfWeek.Friday, 10, 0, 12, 0, "Room B104")]),
            new SectionSeed(s2025s2, "UXD210", "01", 26, "Ms. Alicia Tan", [Meeting(DayOfWeek.Friday, 14, 0, 16, 0, "Studio 1")]),
            new SectionSeed(s2025s2, "MKT225", "01", 36, "Ms. Nadia Khoo", [Meeting(DayOfWeek.Thursday, 14, 0, 16, 0, "Room D210")]),
            new SectionSeed(s2025s2, "DAT250", "01", 28, "Dr. Ravi Nair", [Meeting(DayOfWeek.Tuesday, 9, 0, 11, 0, "Lab 8")]),

            new SectionSeed(s2025s3, "CSC102", "01", 24, "Ms. Melissa Wong", [Meeting(DayOfWeek.Monday, 14, 0, 16, 0, "Lab 3")]),
            new SectionSeed(s2025s3, "CSC201", "01", 26, "Dr. Marcus Lim", [Meeting(DayOfWeek.Tuesday, 9, 0, 11, 0, "Lab 4")]),
            new SectionSeed(s2025s3, "CYB220", "01", 28, "Mr. Azlan Rahman", [Meeting(DayOfWeek.Wednesday, 14, 0, 16, 0, "Lab 5")]),
            new SectionSeed(s2025s3, "MOB230", "01", 22, "Ms. Joanne Goh", [Meeting(DayOfWeek.Thursday, 16, 0, 18, 0, "Lab 6")]),
            new SectionSeed(s2025s3, "HIS220", "01", 42, "Dr. Leong Wei Han", [Meeting(DayOfWeek.Friday, 8, 30, 10, 30, "Room A101")]),
            new SectionSeed(s2025s3, "ECO120", "01", 45, "Ms. Sabrina Ooi", [Meeting(DayOfWeek.Friday, 11, 0, 13, 0, "Room D102")]),
            new SectionSeed(s2025s3, "BUS205", "01", 35, "Mr. Daniel Cho", [Meeting(DayOfWeek.Wednesday, 10, 0, 12, 0, "Room C201")]),
            new SectionSeed(s2025s3, "LAW160", "01", 38, "Mr. Simon Yap", [Meeting(DayOfWeek.Tuesday, 13, 0, 15, 0, "Room C105")]),
            new SectionSeed(s2025s3, "COM110", "01", 40, "Ms. Anis Safia", [Meeting(DayOfWeek.Monday, 9, 0, 11, 0, "Room B106")]),

            new SectionSeed(s2026s1, "CSC230", "01", 30, "Dr. Faris Abdullah", [Meeting(DayOfWeek.Monday, 9, 0, 11, 0, "Lab 7")]),
            new SectionSeed(s2026s1, "MAT201", "01", 30, "Dr. Priya Menon", [Meeting(DayOfWeek.Monday, 13, 0, 15, 0, "Room B201")]),
            new SectionSeed(s2026s1, "AIS260", "01", 26, "Mr. Edwin Ng", [Meeting(DayOfWeek.Tuesday, 10, 0, 12, 0, "Room D301")]),
            new SectionSeed(s2026s1, "ENG150", "01", 38, "Ms. Farina Halim", [Meeting(DayOfWeek.Wednesday, 9, 0, 11, 0, "Room B104")]),
            new SectionSeed(s2026s1, "STA210", "01", 36, "Ms. Tan Mei Ling", [Meeting(DayOfWeek.Thursday, 14, 0, 16, 0, "Room A110")]),
            new SectionSeed(s2026s1, "UXD210", "01", 26, "Ms. Alicia Tan", [Meeting(DayOfWeek.Friday, 10, 0, 12, 0, "Studio 1")]),
            new SectionSeed(s2026s1, "BUS205", "01", 35, "Mr. Daniel Cho", [Meeting(DayOfWeek.Friday, 14, 0, 16, 0, "Room C201")]),
            new SectionSeed(s2026s1, "DAT250", "01", 28, "Dr. Ravi Nair", [Meeting(DayOfWeek.Wednesday, 14, 0, 16, 0, "Lab 8")]),
            new SectionSeed(s2026s1, "FIN215", "01", 34, "Ms. Irene Goh", [Meeting(DayOfWeek.Tuesday, 13, 0, 15, 0, "Room D205")]),
            new SectionSeed(s2026s1, "MKT225", "01", 36, "Ms. Nadia Khoo", [Meeting(DayOfWeek.Thursday, 9, 0, 11, 0, "Room D210")]),
            new SectionSeed(s2026s1, "CYB220", "01", 28, "Mr. Azlan Rahman", [Meeting(DayOfWeek.Wednesday, 10, 0, 12, 0, "Lab 5")]),
            new SectionSeed(s2026s1, "CLD270", "01", 24, "Mr. Aaron Chua", [Meeting(DayOfWeek.Friday, 13, 0, 15, 0, "Lab 9")]),

            new SectionSeed(s2026s2, "CSC101", "01", 25, "Dr. Nur Izzati",
                [Meeting(DayOfWeek.Monday, 9, 0, 11, 0, "Lab 2"), Meeting(DayOfWeek.Thursday, 9, 0, 10, 0, "Lab 2")]),
            new SectionSeed(s2026s2, "MAT201", "01", 20, "Ms. Tan Mei Ling", [Meeting(DayOfWeek.Monday, 10, 0, 12, 0, "Room B201")]),
            new SectionSeed(s2026s2, "ENG150", "02", 18, "Mr. Haris Ismail", [Meeting(DayOfWeek.Tuesday, 14, 0, 16, 0, "Room C103")]),
            new SectionSeed(s2026s2, "HIS220", "01", 1, "Dr. Leong Wei Han", [Meeting(DayOfWeek.Wednesday, 8, 30, 10, 30, "Room A101")]),
            new SectionSeed(s2026s2, "CSC230", "01", 30, "Dr. Faris Abdullah", [Meeting(DayOfWeek.Tuesday, 9, 0, 11, 0, "Lab 7")]),
            new SectionSeed(s2026s2, "CSC240", "01", 28, "Mr. Adrian Lee", [Meeting(DayOfWeek.Friday, 10, 0, 12, 0, "Room C302")]),
            new SectionSeed(s2026s2, "BUS205", "01", 35, "Mr. Daniel Cho", [Meeting(DayOfWeek.Thursday, 13, 0, 15, 0, "Room C201")]),
            new SectionSeed(s2026s2, "CYB220", "01", 24, "Mr. Azlan Rahman", [Meeting(DayOfWeek.Wednesday, 14, 0, 16, 0, "Lab 5")]),
            new SectionSeed(s2026s2, "MOB230", "01", 20, "Ms. Joanne Goh", [Meeting(DayOfWeek.Tuesday, 16, 0, 18, 0, "Lab 6")]),
            new SectionSeed(s2026s2, "STA210", "01", 36, "Ms. Tan Mei Ling", [Meeting(DayOfWeek.Friday, 14, 0, 16, 0, "Room A110")]),
            new SectionSeed(s2026s2, "UXD210", "01", 26, "Ms. Alicia Tan", [Meeting(DayOfWeek.Monday, 13, 0, 15, 0, "Studio 1")]),
            new SectionSeed(s2026s2, "AIS260", "01", 26, "Mr. Edwin Ng", [Meeting(DayOfWeek.Thursday, 10, 0, 12, 0, "Room D301")]),
            new SectionSeed(s2026s2, "CSC201", "01", 26, "Dr. Marcus Lim", [Meeting(DayOfWeek.Wednesday, 10, 0, 12, 0, "Lab 4")]),
            new SectionSeed(s2026s2, "CSC102", "01", 24, "Ms. Melissa Wong", [Meeting(DayOfWeek.Tuesday, 11, 0, 13, 0, "Lab 3")]),
            new SectionSeed(s2026s2, "ACC110", "01", 40, "Mr. Hafiz Jamal", [Meeting(DayOfWeek.Friday, 8, 30, 10, 30, "Room B210")]),
            new SectionSeed(s2026s2, "ECO120", "01", 45, "Ms. Sabrina Ooi", [Meeting(DayOfWeek.Wednesday, 12, 0, 14, 0, "Room D102")]),
            new SectionSeed(s2026s2, "DAT250", "01", 28, "Dr. Ravi Nair", [Meeting(DayOfWeek.Monday, 16, 0, 18, 0, "Lab 8")]),
            new SectionSeed(s2026s2, "CLD270", "01", 24, "Mr. Aaron Chua", [Meeting(DayOfWeek.Thursday, 16, 0, 18, 0, "Lab 9")]),
            new SectionSeed(s2026s2, "FIN215", "01", 34, "Ms. Irene Goh", [Meeting(DayOfWeek.Wednesday, 8, 30, 10, 30, "Room D205")]),
            new SectionSeed(s2026s2, "MKT225", "01", 36, "Ms. Nadia Khoo", [Meeting(DayOfWeek.Monday, 15, 0, 17, 0, "Room D210")]),
            new SectionSeed(s2026s2, "CSC245", "01", 24, "Mr. Aaron Chua", [Meeting(DayOfWeek.Friday, 12, 0, 14, 0, "Lab 9")]),
            new SectionSeed(s2026s2, "CSC310", "01", 24, "Dr. Marcus Lim", [Meeting(DayOfWeek.Thursday, 18, 0, 20, 0, "Lab 4")]),
            new SectionSeed(s2026s2, "COM110", "01", 40, "Ms. Anis Safia", [Meeting(DayOfWeek.Tuesday, 8, 30, 10, 30, "Room B106")]),
            new SectionSeed(s2026s2, "LAW160", "01", 38, "Mr. Simon Yap", [Meeting(DayOfWeek.Wednesday, 16, 0, 18, 0, "Room C105")]),

            new SectionSeed(s2026s3, "AIS260", "01", 28, "Mr. Edwin Ng", [Meeting(DayOfWeek.Monday, 9, 0, 11, 0, "Room D301")]),
            new SectionSeed(s2026s3, "DAT250", "01", 30, "Dr. Ravi Nair", [Meeting(DayOfWeek.Monday, 14, 0, 16, 0, "Lab 8")]),
            new SectionSeed(s2026s3, "STA210", "01", 36, "Ms. Tan Mei Ling", [Meeting(DayOfWeek.Tuesday, 9, 0, 11, 0, "Room A110")]),
            new SectionSeed(s2026s3, "ACC110", "01", 38, "Mr. Hafiz Jamal", [Meeting(DayOfWeek.Wednesday, 9, 0, 11, 0, "Room B210")]),
            new SectionSeed(s2026s3, "BUS205", "01", 35, "Mr. Daniel Cho", [Meeting(DayOfWeek.Tuesday, 14, 0, 16, 0, "Room C201")]),
            new SectionSeed(s2026s3, "FIN215", "01", 34, "Ms. Irene Goh", [Meeting(DayOfWeek.Thursday, 9, 0, 11, 0, "Room D205")]),
            new SectionSeed(s2026s3, "MKT225", "01", 36, "Ms. Nadia Khoo", [Meeting(DayOfWeek.Wednesday, 14, 0, 16, 0, "Room D210")]),
            new SectionSeed(s2026s3, "ECO120", "01", 40, "Ms. Sabrina Ooi", [Meeting(DayOfWeek.Friday, 9, 0, 11, 0, "Room D102")]),
            new SectionSeed(s2026s3, "ENG150", "01", 40, "Ms. Farina Halim", [Meeting(DayOfWeek.Friday, 11, 0, 13, 0, "Room B104")]),
            new SectionSeed(s2026s3, "COM110", "01", 40, "Ms. Anis Safia", [Meeting(DayOfWeek.Monday, 11, 30, 13, 30, "Room B106")]),
            new SectionSeed(s2026s3, "MAT210", "01", 30, "Dr. Kelvin Lo", [Meeting(DayOfWeek.Thursday, 14, 0, 16, 0, "Room A204")]),
            new SectionSeed(s2026s3, "HIS220", "01", 42, "Dr. Leong Wei Han", [Meeting(DayOfWeek.Friday, 14, 0, 16, 0, "Room A101")])
        ];
    }

    private static List<UserSeed> CreateUserSeeds()
    {
        return
        [
            new UserSeed("Alice Tan", SeedDataDefaults.DemoEmails[0], "ST2026001", "Diploma in Software Engineering", "SE", "January 2024", SemesterCode(2026, 2)),
            new UserSeed("Bob Kumar", SeedDataDefaults.DemoEmails[1], "ST2026002", "Diploma in Information Technology", "IT", "January 2024", SemesterCode(2026, 2)),
            new UserSeed("Chloe Lim", SeedDataDefaults.DemoEmails[2], "ST2026003", "Diploma in Business Analytics", "BA", "January 2025", SemesterCode(2026, 3)),
            new UserSeed("Daniel Wong", SeedDataDefaults.DemoEmails[3], "ST2026004", "Diploma in Cyber Security", "CY", "September 2024", SemesterCode(2026, 2)),
            new UserSeed("Farah Hassan", SeedDataDefaults.DemoEmails[4], "ST2026005", "Diploma in Data Analytics", "DA", "September 2024", SemesterCode(2026, 3))
        ];
    }

    private static List<EnrollmentSeed> CreateEnrollmentSeeds()
    {
        var s2024s3 = SemesterCode(2024, 3);
        var s2025s1 = SemesterCode(2025, 1);
        var s2025s2 = SemesterCode(2025, 2);
        var s2025s3 = SemesterCode(2025, 3);
        var s2026s1 = SemesterCode(2026, 1);
        var s2026s2 = SemesterCode(2026, 2);
        var s2026s3 = SemesterCode(2026, 3);

        return
        [
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[0], BuildSectionKey(s2026s2, "CSC101", "01"), new DateTime(2026, 4, 2, 9, 10, 0, DateTimeKind.Utc), "Online enrollment"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[0], BuildSectionKey(s2026s2, "ENG150", "02"), new DateTime(2026, 4, 2, 9, 20, 0, DateTimeKind.Utc), "Online enrollment"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[0], BuildSectionKey(s2026s2, "CSC201", "01"), new DateTime(2026, 4, 3, 8, 55, 0, DateTimeKind.Utc), "Online enrollment"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[0], BuildSectionKey(s2026s2, "COM110", "01"), new DateTime(2026, 4, 3, 9, 25, 0, DateTimeKind.Utc), "Online enrollment"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[1], BuildSectionKey(s2026s2, "HIS220", "01"), new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc), "Online enrollment"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[1], BuildSectionKey(s2026s2, "CSC240", "01"), new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc), "Online enrollment"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[1], BuildSectionKey(s2026s2, "CLD270", "01"), new DateTime(2026, 4, 4, 10, 10, 0, DateTimeKind.Utc), "Online enrollment"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[2], BuildSectionKey(s2026s3, "AIS260", "01"), new DateTime(2026, 4, 18, 8, 30, 0, DateTimeKind.Utc), "Online enrollment"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[2], BuildSectionKey(s2026s3, "STA210", "01"), new DateTime(2026, 4, 18, 8, 45, 0, DateTimeKind.Utc), "Online enrollment"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[2], BuildSectionKey(s2026s3, "COM110", "01"), new DateTime(2026, 4, 18, 9, 5, 0, DateTimeKind.Utc), "Online enrollment"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[3], BuildSectionKey(s2026s2, "CYB220", "01"), new DateTime(2026, 4, 2, 11, 5, 0, DateTimeKind.Utc), "Online enrollment"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[3], BuildSectionKey(s2026s2, "CSC240", "01"), new DateTime(2026, 4, 3, 11, 15, 0, DateTimeKind.Utc), "Online enrollment"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[3], BuildSectionKey(s2026s2, "CSC245", "01"), new DateTime(2026, 4, 3, 11, 25, 0, DateTimeKind.Utc), "Online enrollment"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[4], BuildSectionKey(s2026s3, "DAT250", "01"), new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc), "Online enrollment"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[4], BuildSectionKey(s2026s3, "FIN215", "01"), new DateTime(2026, 4, 18, 12, 15, 0, DateTimeKind.Utc), "Online enrollment"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[4], BuildSectionKey(s2026s3, "ENG150", "01"), new DateTime(2026, 4, 18, 12, 30, 0, DateTimeKind.Utc), "Online enrollment"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[0], BuildSectionKey(s2026s1, "CSC230", "01"), new DateTime(2026, 1, 8, 9, 0, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[0], BuildSectionKey(s2026s1, "MAT201", "01"), new DateTime(2026, 1, 8, 9, 10, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[0], BuildSectionKey(s2026s1, "AIS260", "01"), new DateTime(2026, 1, 9, 10, 0, 0, DateTimeKind.Utc), "Registered during semester registration", new DateTime(2026, 1, 18, 9, 0, 0, DateTimeKind.Utc), "Adjusted study load after timetable review"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[0], BuildSectionKey(s2026s1, "ENG150", "01"), new DateTime(2026, 1, 9, 10, 15, 0, DateTimeKind.Utc), "Registered during semester registration"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[1], BuildSectionKey(s2026s1, "BUS205", "01"), new DateTime(2026, 1, 8, 8, 35, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[1], BuildSectionKey(s2026s1, "DAT250", "01"), new DateTime(2026, 1, 8, 8, 50, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[1], BuildSectionKey(s2026s1, "FIN215", "01"), new DateTime(2026, 1, 8, 9, 0, 0, DateTimeKind.Utc), "Registered during semester registration", new DateTime(2026, 1, 20, 8, 30, 0, DateTimeKind.Utc), "Replaced with a lower credit elective"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[2], BuildSectionKey(s2026s1, "DAT250", "01"), new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[2], BuildSectionKey(s2026s1, "FIN215", "01"), new DateTime(2026, 1, 10, 9, 10, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[2], BuildSectionKey(s2026s1, "MKT225", "01"), new DateTime(2026, 1, 10, 9, 20, 0, DateTimeKind.Utc), "Registered during semester registration"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[3], BuildSectionKey(s2026s1, "CYB220", "01"), new DateTime(2026, 1, 11, 10, 0, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[3], BuildSectionKey(s2026s1, "CLD270", "01"), new DateTime(2026, 1, 11, 10, 15, 0, DateTimeKind.Utc), "Registered during semester registration"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[4], BuildSectionKey(s2026s1, "STA210", "01"), new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[4], BuildSectionKey(s2026s1, "UXD210", "01"), new DateTime(2026, 1, 10, 9, 10, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[4], BuildSectionKey(s2026s1, "MKT225", "01"), new DateTime(2026, 1, 10, 9, 20, 0, DateTimeKind.Utc), "Registered during semester registration", new DateTime(2026, 1, 24, 11, 0, 0, DateTimeKind.Utc), "Changed focus to project-based electives"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[0], BuildSectionKey(s2025s3, "CSC102", "01"), new DateTime(2025, 9, 5, 8, 0, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[0], BuildSectionKey(s2025s3, "CSC201", "01"), new DateTime(2025, 9, 5, 8, 20, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[0], BuildSectionKey(s2025s3, "CYB220", "01"), new DateTime(2025, 9, 6, 8, 30, 0, DateTimeKind.Utc), "Registered during semester registration", new DateTime(2025, 9, 20, 7, 45, 0, DateTimeKind.Utc), "Moved to a different elective pathway"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[1], BuildSectionKey(s2025s3, "HIS220", "01"), new DateTime(2025, 9, 6, 9, 0, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[1], BuildSectionKey(s2025s3, "ECO120", "01"), new DateTime(2025, 9, 6, 9, 10, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[1], BuildSectionKey(s2025s3, "BUS205", "01"), new DateTime(2025, 9, 6, 9, 20, 0, DateTimeKind.Utc), "Registered during semester registration"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[2], BuildSectionKey(s2025s2, "MAT210", "01"), new DateTime(2025, 4, 5, 10, 0, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[2], BuildSectionKey(s2025s2, "ENG150", "01"), new DateTime(2025, 4, 5, 10, 15, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[2], BuildSectionKey(s2025s2, "MKT225", "01"), new DateTime(2025, 4, 5, 10, 30, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[2], BuildSectionKey(s2024s3, "BUS205", "01"), new DateTime(2024, 9, 8, 10, 0, 0, DateTimeKind.Utc), "Registered during semester registration"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[3], BuildSectionKey(s2025s1, "CSC101", "01"), new DateTime(2025, 1, 6, 11, 0, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[3], BuildSectionKey(s2025s1, "ACC110", "01"), new DateTime(2025, 1, 6, 11, 15, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[3], BuildSectionKey(s2025s3, "LAW160", "01"), new DateTime(2025, 9, 7, 11, 0, 0, DateTimeKind.Utc), "Registered during semester registration"),

            new EnrollmentSeed(SeedDataDefaults.DemoEmails[4], BuildSectionKey(s2025s2, "ECO120", "01"), new DateTime(2025, 4, 6, 10, 0, 0, DateTimeKind.Utc), "Registered during semester registration", new DateTime(2025, 4, 18, 10, 0, 0, DateTimeKind.Utc), "Changed elective selection after programme review"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[4], BuildSectionKey(s2025s1, "FIN215", "01"), new DateTime(2025, 1, 8, 10, 0, 0, DateTimeKind.Utc), "Registered during semester registration"),
            new EnrollmentSeed(SeedDataDefaults.DemoEmails[4], BuildSectionKey(s2025s3, "COM110", "01"), new DateTime(2025, 9, 9, 9, 0, 0, DateTimeKind.Utc), "Registered during semester registration")
        ];
    }

    private static MeetingSeed Meeting(
        DayOfWeek dayOfWeek,
        int startHour,
        int startMinute,
        int endHour,
        int endMinute,
        string venue)
    {
        return new MeetingSeed(dayOfWeek, startHour, startMinute, endHour, endMinute, venue);
    }

    private sealed record CourseSeed(string Code, string Title, int CreditHours, string EligibleProgrammeCodes);

    private sealed record SemesterSeed(
        string Code,
        string Name,
        SemesterStatus Status,
        DateOnly EnrollmentStartDate,
        DateOnly EnrollmentEndDate,
        DateOnly SemesterStartDate,
        DateOnly AddDropEndDate);

    private sealed record UserSeed(
        string DisplayName,
        string Email,
        string StudentNumber,
        string ProgramName,
        string ProgramCode,
        string IntakeLabel,
        string CurrentSemesterCode);

    private sealed record MeetingSeed(
        DayOfWeek DayOfWeek,
        int StartHour,
        int StartMinute,
        int EndHour,
        int EndMinute,
        string Venue);

    private sealed record SectionSeed(
        string SemesterCode,
        string CourseCode,
        string SectionCode,
        int Capacity,
        string InstructorName,
        IReadOnlyList<MeetingSeed> Meetings);

    private sealed record EnrollmentSeed(
        string StudentEmail,
        string SectionKey,
        DateTime AddedAtUtc,
        string AddedRemarks,
        DateTime? DroppedAtUtc = null,
        string? DropReason = null);
}
