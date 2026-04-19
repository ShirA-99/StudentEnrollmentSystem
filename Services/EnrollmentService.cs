using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using StudentEnrollmentSystem.ViewModels;

namespace StudentEnrollmentSystem.Services;

public class EnrollmentService(ApplicationDbContext context)
{
    public async Task<EnrollmentIndexViewModel> GetEnrollmentCatalogAsync(string userId)
    {
        var student = await GetStudentAsync(userId);
        var activeSemester = await FindActiveSemesterAsync();
        var semester = await GetReferenceSemesterAsync(student, activeSemester);

        if (semester is null)
        {
            return new EnrollmentIndexViewModel
            {
                StudentName = student.FullName,
                StudentNumber = student.StudentNumber,
                ProgramName = student.ProgramName,
                IntakeLabel = student.IntakeLabel,
                SemesterName = "Semester information unavailable",
                RegistrationWindow = "Please check with the academic office for the next registration period.",
                RecentActivityText = "No recent registration activity has been recorded yet.",
                RegistrationStatusMessage = "We could not find a semester to display right now.",
                IsRegistrationOpen = false
            };
        }

        var activeEnrollments = await context.EnrollmentRecords
            .AsNoTracking()
            .Include(record => record.CourseSection)
                .ThenInclude(section => section.Course)
            .Include(record => record.CourseSection)
                .ThenInclude(section => section.Meetings)
            .Where(record => record.StudentProfileId == student.Id &&
                             record.Status == EnrollmentStatus.Enrolled &&
                             record.CourseSection.SemesterId == semester.Id)
            .ToListAsync();

        var activeSectionIds = activeEnrollments.Select(record => record.CourseSectionId).ToHashSet();
        var activeCourseIds = activeEnrollments.Select(record => record.CourseSection.CourseId).ToHashSet();

        var availableSections = await context.CourseSections
            .AsNoTracking()
            .Include(section => section.Course)
            .Include(section => section.Meetings)
            .Include(section => section.EnrollmentRecords)
            .Where(section => section.SemesterId == semester.Id &&
                              !activeSectionIds.Contains(section.Id) &&
                              !activeCourseIds.Contains(section.CourseId))
            .OrderBy(section => section.Course.Code)
            .ThenBy(section => section.SectionCode)
            .ToListAsync();

        var latestAudit = await context.AddDropAudits
            .AsNoTracking()
            .Include(audit => audit.CourseSection)
                .ThenInclude(section => section.Course)
            .Where(audit => audit.StudentProfileId == student.Id &&
                            audit.CourseSection.SemesterId == semester.Id)
            .OrderByDescending(audit => audit.ActionAtUtc)
            .FirstOrDefaultAsync();

        var availableSectionCards = availableSections
            .Select(section => new
            {
                Section = section,
                SeatsRemaining = section.Capacity -
                                 section.EnrollmentRecords.Count(record => record.Status == EnrollmentStatus.Enrolled)
            })
            .Where(entry => entry.SeatsRemaining > 0)
            .Select(entry => new AvailableSectionViewModel
            {
                SectionId = entry.Section.Id,
                CourseCode = entry.Section.Course.Code,
                CourseTitle = entry.Section.Course.Title,
                SectionCode = entry.Section.SectionCode,
                CreditHours = entry.Section.Course.CreditHours,
                InstructorName = entry.Section.InstructorName,
                ScheduleSummary = FormatSchedule(entry.Section.Meetings),
                SeatsRemaining = entry.SeatsRemaining
            })
            .ToList();

        var isRegistrationOpen = activeSemester?.Id == semester.Id;

        return new EnrollmentIndexViewModel
        {
            StudentName = student.FullName,
            StudentNumber = student.StudentNumber,
            ProgramName = student.ProgramName,
            IntakeLabel = student.IntakeLabel,
            SemesterName = semester.Name,
            RegistrationWindow =
                $"{semester.EnrollmentStartDate:dd MMM yyyy} - {semester.EnrollmentEndDate:dd MMM yyyy}",
            ActiveEnrollmentCount = activeEnrollments.Count,
            CurrentCreditHours = activeEnrollments.Sum(record => record.CourseSection.Course.CreditHours),
            AvailableSectionCount = availableSectionCards.Count,
            RecentActivityText = latestAudit is null
                ? $"No registration changes have been recorded for {semester.Name} yet."
                : $"{latestAudit.ActionType} {latestAudit.CourseSection.Course.Code} section {latestAudit.CourseSection.SectionCode} on {latestAudit.ActionAtUtc.ToLocalTime():dd MMM yyyy}.",
            IsRegistrationOpen = isRegistrationOpen,
            RegistrationStatusMessage = isRegistrationOpen
                ? $"Registration for {semester.Name} is open. You can review sections and submit changes until {semester.EnrollmentEndDate:dd MMM yyyy}."
                : $"Registration changes for {semester.Name} are currently closed. You can still review sections and plan your timetable.",
            AvailableSections = availableSectionCards
        };
    }

    public async Task<OperationResult> EnrollAsync(string userId, int sectionId)
    {
        var student = await GetStudentAsync(userId);
        var semester = await FindActiveSemesterAsync();

        if (semester is null)
        {
            return OperationResult.Failure(
                "Enrollment is currently closed. You can still review sections, but new registrations cannot be submitted right now.");
        }

        var section = await context.CourseSections
            .Include(target => target.Course)
            .Include(target => target.Semester)
            .Include(target => target.Meetings)
            .Include(target => target.EnrollmentRecords)
            .SingleOrDefaultAsync(target => target.Id == sectionId);

        if (section is null || section.SemesterId != semester.Id)
        {
            return OperationResult.Failure("The selected section is no longer available during the current registration period.");
        }

        var currentEnrollments = await context.EnrollmentRecords
            .Include(record => record.CourseSection)
                .ThenInclude(target => target.Course)
            .Include(record => record.CourseSection)
                .ThenInclude(target => target.Meetings)
            .Where(record => record.StudentProfileId == student.Id &&
                             record.Status == EnrollmentStatus.Enrolled &&
                             record.CourseSection.SemesterId == semester.Id)
            .ToListAsync();

        if (currentEnrollments.Any(record => record.CourseSectionId == section.Id || record.CourseSection.CourseId == section.CourseId))
        {
            return OperationResult.Failure("You are already actively enrolled in this course.");
        }

        var seatsTaken = section.EnrollmentRecords.Count(record => record.Status == EnrollmentStatus.Enrolled);
        if (seatsTaken >= section.Capacity)
        {
            return OperationResult.Failure("This section is already full.");
        }

        var clashCourse = currentEnrollments.FirstOrDefault(record => HasScheduleConflict(record.CourseSection.Meetings, section.Meetings));
        if (clashCourse is not null)
        {
            return OperationResult.Failure(
                $"Timetable clash detected with {clashCourse.CourseSection.Course.Code} section {clashCourse.CourseSection.SectionCode}.");
        }

        var timestamp = DateTime.UtcNow;

        await context.EnrollmentRecords.AddAsync(new EnrollmentRecord
        {
            StudentProfileId = student.Id,
            CourseSectionId = section.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = timestamp
        });

        await context.AddDropAudits.AddAsync(new AddDropAudit
        {
            StudentProfileId = student.Id,
            CourseSectionId = section.Id,
            ActionType = AddDropActionType.Added,
            ActionAtUtc = timestamp,
            Remarks = "Online enrollment"
        });

        await context.SaveChangesAsync();

        return OperationResult.Success(
            $"{section.Course.Code} section {section.SectionCode} has been added to your current registrations.");
    }

    private async Task<StudentProfile> GetStudentAsync(string userId)
    {
        var student = await context.StudentProfiles
            .AsNoTracking()
            .Include(profile => profile.CurrentSemester)
            .SingleOrDefaultAsync(profile => profile.ApplicationUserId == userId);

        return student ?? throw new InvalidOperationException("No student profile was found for the signed-in account.");
    }

    private Task<Semester?> FindActiveSemesterAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return context.Semesters
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Status == SemesterStatus.OpenForEnrollment &&
                item.EnrollmentStartDate <= today &&
                item.EnrollmentEndDate >= today);
    }

    private async Task<Semester?> GetReferenceSemesterAsync(StudentProfile student, Semester? activeSemester)
    {
        if (activeSemester is not null)
        {
            return activeSemester;
        }

        if (student.CurrentSemester is not null)
        {
            return student.CurrentSemester;
        }

        var latestEnrollmentSemesterId = await context.EnrollmentRecords
            .AsNoTracking()
            .Where(record => record.StudentProfileId == student.Id)
            .OrderByDescending(record => record.CourseSection.Semester.EnrollmentEndDate)
            .Select(record => (int?)record.CourseSection.SemesterId)
            .FirstOrDefaultAsync();

        if (latestEnrollmentSemesterId.HasValue)
        {
            return await context.Semesters
                .AsNoTracking()
                .SingleOrDefaultAsync(semester => semester.Id == latestEnrollmentSemesterId.Value);
        }

        return await context.Semesters
            .AsNoTracking()
            .OrderByDescending(semester => semester.EnrollmentEndDate)
            .FirstOrDefaultAsync();
    }

    internal static bool HasScheduleConflict(IEnumerable<SectionMeeting> firstSchedule, IEnumerable<SectionMeeting> secondSchedule)
    {
        return firstSchedule.Any(first =>
            secondSchedule.Any(second =>
                first.DayOfWeek == second.DayOfWeek &&
                first.StartTime < second.EndTime &&
                second.StartTime < first.EndTime));
    }

    internal static string FormatSchedule(IEnumerable<SectionMeeting> meetings)
    {
        return string.Join(
            ", ",
            meetings
                .OrderBy(meeting => meeting.DayOfWeek)
                .ThenBy(meeting => meeting.StartTime)
                .Select(meeting =>
                    $"{meeting.DayOfWeek}: {meeting.StartTime:HH\\:mm}-{meeting.EndTime:HH\\:mm} @ {meeting.Venue}"));
    }
}
