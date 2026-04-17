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
        var semester = await GetActiveSemesterAsync();

        var activeEnrollments = await context.EnrollmentRecords
            .Where(record => record.StudentProfileId == student.Id &&
                             record.Status == EnrollmentStatus.Enrolled &&
                             record.CourseSection.SemesterId == semester.Id)
            .Select(record => new { record.CourseSectionId, record.CourseSection.CourseId })
            .ToListAsync();

        var activeSectionIds = activeEnrollments.Select(record => record.CourseSectionId).ToHashSet();
        var activeCourseIds = activeEnrollments.Select(record => record.CourseId).ToHashSet();

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

        return new EnrollmentIndexViewModel
        {
            StudentName = student.FullName,
            StudentNumber = student.StudentNumber,
            SemesterName = semester.Name,
            RegistrationWindow =
                $"{semester.EnrollmentStartDate:dd MMM yyyy} - {semester.EnrollmentEndDate:dd MMM yyyy}",
            AvailableSections = availableSections
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
                .ToList()
        };
    }

    public async Task<OperationResult> EnrollAsync(string userId, int sectionId)
    {
        var student = await GetStudentAsync(userId);
        var semester = await GetActiveSemesterAsync();

        var section = await context.CourseSections
            .Include(target => target.Course)
            .Include(target => target.Semester)
            .Include(target => target.Meetings)
            .Include(target => target.EnrollmentRecords)
            .SingleOrDefaultAsync(target => target.Id == sectionId);

        if (section is null || section.SemesterId != semester.Id)
        {
            return OperationResult.Failure("The selected section is not available in the current enrollment window.");
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
            .SingleOrDefaultAsync(profile => profile.ApplicationUserId == userId);

        return student ?? throw new InvalidOperationException("No student profile was found for the signed-in account.");
    }

    private async Task<Semester> GetActiveSemesterAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var semester = await context.Semesters
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Status == SemesterStatus.OpenForEnrollment &&
                item.EnrollmentStartDate <= today &&
                item.EnrollmentEndDate >= today);

        return semester ?? throw new InvalidOperationException("There is no semester currently open for enrollment.");
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
