using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using StudentEnrollmentSystem.ViewModels;

namespace StudentEnrollmentSystem.Services;

public class EnrollmentService(ApplicationDbContext context)
{
    public async Task<EnrollmentIndexViewModel> GetEnrollmentCatalogAsync(string userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var student = await GetStudentAsync(userId);
        var semester = await GetReferenceSemesterAsync(student);

        if (semester is null)
        {
            return new EnrollmentIndexViewModel
            {
                StudentName = student.FullName,
                StudentNumber = student.StudentNumber,
                ProgramName = student.ProgramName,
                IntakeLabel = student.IntakeLabel,
                SemesterName = "Semester information unavailable",
                RegistrationWindow = "Please check with the academic office for the next enrollment and add / drop periods.",
                RecentActivityText = "No recent enrollment activity has been recorded yet.",
                RegistrationStatusMessage = "We could not find a semester to display right now.",
                IsRegistrationOpen = false
            };
        }

        var state = SemesterTimeline.Describe(semester, today);

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
            .Where(section => SemesterTimeline.IsCourseAvailableToProgramme(section.Course, student.ProgramCode))
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

        return new EnrollmentIndexViewModel
        {
            StudentName = student.FullName,
            StudentNumber = student.StudentNumber,
            ProgramName = student.ProgramName,
            IntakeLabel = student.IntakeLabel,
            SemesterName = semester.Name,
            RegistrationWindow = SemesterTimeline.FormatWindowSummary(semester),
            ActiveEnrollmentCount = activeEnrollments.Count,
            CurrentCreditHours = activeEnrollments.Sum(record => record.CourseSection.Course.CreditHours),
            AvailableSectionCount = availableSectionCards.Count,
            RecentActivityText = latestAudit is null
                ? $"No enrollment changes have been recorded for {semester.Name} yet."
                : $"{latestAudit.ActionType} {latestAudit.CourseSection.Course.Code} section {latestAudit.CourseSection.SectionCode} on {latestAudit.ActionAtUtc.ToLocalTime():dd MMM yyyy}.",
            IsRegistrationOpen = state.IsEnrollmentOpen,
            RegistrationStatusMessage = BuildEnrollmentStatusMessage(state),
            AvailableSections = availableSectionCards
        };
    }

    public Task<OperationResult> EnrollAsync(string userId, int sectionId)
        => AddSectionAsync(userId, sectionId, SemesterLifecyclePhase.Enrollment, "Online enrollment");

    internal Task<OperationResult> AddDuringAddDropAsync(string userId, int sectionId)
        => AddSectionAsync(userId, sectionId, SemesterLifecyclePhase.AddDrop, "Added during add / drop");

    private async Task<OperationResult> AddSectionAsync(
        string userId,
        int sectionId,
        SemesterLifecyclePhase requiredPhase,
        string auditRemarks)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var student = await GetStudentAsync(userId);
        var semester = await GetReferenceSemesterAsync(student);

        if (semester is null)
        {
            return OperationResult.Failure(
                "We could not determine which semester should accept this enrollment request.");
        }

        var state = SemesterTimeline.Describe(semester, today);

        if (state.Phase != requiredPhase)
        {
            return OperationResult.Failure(BuildUnavailableActionMessage(state, requiredPhase));
        }

        var section = await context.CourseSections
            .Include(target => target.Course)
            .Include(target => target.Semester)
            .Include(target => target.Meetings)
            .Include(target => target.EnrollmentRecords)
            .SingleOrDefaultAsync(target => target.Id == sectionId);

        if (section is null || section.SemesterId != semester.Id)
        {
            return OperationResult.Failure("The selected section is no longer available for your current semester.");
        }

        if (!SemesterTimeline.IsCourseAvailableToProgramme(section.Course, student.ProgramCode))
        {
            return OperationResult.Failure(
                $"{section.Course.Code} is not offered to the {student.ProgramName} programme.");
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
            Remarks = auditRemarks
        });

        await context.SaveChangesAsync();

        return OperationResult.Success(
            $"{section.Course.Code} section {section.SectionCode} has been added to your current timetable.");
    }

    private async Task<StudentProfile> GetStudentAsync(string userId)
    {
        var student = await context.StudentProfiles
            .AsNoTracking()
            .Include(profile => profile.CurrentSemester)
            .SingleOrDefaultAsync(profile => profile.ApplicationUserId == userId);

        return student ?? throw new InvalidOperationException("No student profile was found for the signed-in account.");
    }

    private async Task<Semester?> GetReferenceSemesterAsync(StudentProfile student)
    {
        if (student.CurrentSemester is not null)
        {
            return student.CurrentSemester;
        }

        var latestEnrollmentSemesterId = await context.EnrollmentRecords
            .AsNoTracking()
            .Where(record => record.StudentProfileId == student.Id)
            .OrderByDescending(record => record.CourseSection.Semester.AddDropEndDate)
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
            .OrderByDescending(semester => semester.AddDropEndDate)
            .FirstOrDefaultAsync();
    }

    private static string BuildEnrollmentStatusMessage(SemesterAccessState state)
    {
        return state.Phase switch
        {
            SemesterLifecyclePhase.Enrollment =>
                $"Enrollment for {state.Semester.Name} is open until {state.Semester.EnrollmentEndDate:dd MMM yyyy}. Classes begin on {state.Semester.SemesterStartDate:dd MMM yyyy}.",
            SemesterLifecyclePhase.AddDrop =>
                $"Classes for {state.Semester.Name} have started, so enrollment is closed. Use Add / Drop until {state.Semester.AddDropEndDate:dd MMM yyyy}.",
            SemesterLifecyclePhase.Planning =>
                $"Enrollment for {state.Semester.Name} will open on {state.Semester.EnrollmentStartDate:dd MMM yyyy}. You can review sections approved for your programme in advance.",
            _ =>
                $"Enrollment for {state.Semester.Name} has closed. The last add / drop date was {state.Semester.AddDropEndDate:dd MMM yyyy}."
        };
    }

    private static string BuildUnavailableActionMessage(SemesterAccessState state, SemesterLifecyclePhase requiredPhase)
    {
        if (requiredPhase == SemesterLifecyclePhase.Enrollment)
        {
            return state.Phase switch
            {
                SemesterLifecyclePhase.AddDrop =>
                    $"Enrollment is closed because {state.Semester.Name} has already started. Use Add / Drop until {state.Semester.AddDropEndDate:dd MMM yyyy}.",
                SemesterLifecyclePhase.Planning =>
                    $"Enrollment for {state.Semester.Name} has not opened yet. It begins on {state.Semester.EnrollmentStartDate:dd MMM yyyy}.",
                _ =>
                    $"Enrollment for {state.Semester.Name} is closed. The last add / drop date was {state.Semester.AddDropEndDate:dd MMM yyyy}."
            };
        }

        return state.Phase switch
        {
            SemesterLifecyclePhase.Enrollment =>
                $"Add / Drop for {state.Semester.Name} begins on {state.Semester.SemesterStartDate:dd MMM yyyy}, after classes start.",
            SemesterLifecyclePhase.Planning =>
                $"Add / Drop for {state.Semester.Name} is not available yet because registration has not opened.",
            _ =>
                $"Add / Drop for {state.Semester.Name} is closed. The deadline passed on {state.Semester.AddDropEndDate:dd MMM yyyy}."
        };
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
