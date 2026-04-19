using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using StudentEnrollmentSystem.ViewModels;

namespace StudentEnrollmentSystem.Services;

public class AddDropService(ApplicationDbContext context, EnrollmentService enrollmentService)
{
    public async Task<AddDropIndexViewModel> GetDashboardAsync(string userId)
    {
        var student = await context.StudentProfiles
            .AsNoTracking()
            .Include(profile => profile.CurrentSemester)
            .SingleOrDefaultAsync(profile => profile.ApplicationUserId == userId)
            ?? throw new InvalidOperationException("No student profile was found for the signed-in account.");

        var activeSemester = await FindActiveSemesterAsync();
        var referenceSemester = await GetReferenceSemesterAsync(student, activeSemester);
        var catalog = await enrollmentService.GetEnrollmentCatalogAsync(userId);
        var currentSemesterId = referenceSemester?.Id;

        var currentEnrollments = currentSemesterId is int semesterId
            ? await context.EnrollmentRecords
                .AsNoTracking()
                .Include(record => record.CourseSection)
                    .ThenInclude(section => section.Course)
                .Include(record => record.CourseSection)
                    .ThenInclude(section => section.Meetings)
                .Where(record => record.StudentProfileId == student.Id &&
                                 record.Status == EnrollmentStatus.Enrolled &&
                                 record.CourseSection.SemesterId == semesterId)
                .OrderBy(record => record.CourseSection.Course.Code)
                .ToListAsync()
            : [];

        var historyEvents = await context.AddDropAudits
            .AsNoTracking()
            .Include(audit => audit.CourseSection)
                .ThenInclude(section => section.Course)
            .Where(audit => audit.StudentProfileId == student.Id)
            .OrderByDescending(audit => audit.ActionAtUtc)
            .ToListAsync();

        var latestHistoryEvent = currentSemesterId is int selectedSemesterId
            ? historyEvents.FirstOrDefault(audit => audit.CourseSection.SemesterId == selectedSemesterId)
            : historyEvents.FirstOrDefault();

        return new AddDropIndexViewModel
        {
            StudentName = student.FullName,
            StudentNumber = student.StudentNumber,
            ProgramName = student.ProgramName,
            SemesterName = catalog.SemesterName,
            RegistrationWindow = catalog.RegistrationWindow,
            ActiveEnrollmentCount = currentEnrollments.Count,
            CurrentCreditHours = currentEnrollments.Sum(record => record.CourseSection.Course.CreditHours),
            AvailableSectionCount = catalog.AvailableSections.Count,
            TotalHistoryEvents = historyEvents.Count,
            RecentActivityText = latestHistoryEvent is null
                ? $"No registration adjustments have been recorded for {catalog.SemesterName} yet."
                : $"{latestHistoryEvent.ActionType} {latestHistoryEvent.CourseSection.Course.Code} section {latestHistoryEvent.CourseSection.SectionCode} on {latestHistoryEvent.ActionAtUtc.ToLocalTime():dd MMM yyyy}.",
            IsRegistrationOpen = catalog.IsRegistrationOpen,
            RegistrationStatusMessage = catalog.RegistrationStatusMessage,
            AvailableSections = catalog.AvailableSections,
            CurrentEnrollments = currentEnrollments.Select(record => new CurrentEnrollmentViewModel
            {
                EnrollmentId = record.Id,
                CourseCode = record.CourseSection.Course.Code,
                CourseTitle = record.CourseSection.Course.Title,
                SectionCode = record.CourseSection.SectionCode,
                InstructorName = record.CourseSection.InstructorName,
                CreditHours = record.CourseSection.Course.CreditHours,
                EnrolledAtUtc = record.EnrolledAtUtc,
                ScheduleSummary = EnrollmentService.FormatSchedule(record.CourseSection.Meetings)
            }).ToList()
        };
    }

    public Task<OperationResult> AddCourseAsync(string userId, int sectionId)
        => enrollmentService.EnrollAsync(userId, sectionId);

    public async Task<OperationResult> DropCourseAsync(string userId, int enrollmentId, string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            return OperationResult.Failure("A drop reason is required before removing a course.");
        }

        var student = await context.StudentProfiles
            .SingleOrDefaultAsync(profile => profile.ApplicationUserId == userId)
            ?? throw new InvalidOperationException("No student profile was found for the signed-in account.");

        var activeSemester = await FindActiveSemesterAsync();

        if (activeSemester is null)
        {
            return OperationResult.Failure(
                "Add/drop is currently closed. You can still review your timetable, but changes cannot be submitted right now.");
        }

        var enrollment = await context.EnrollmentRecords
            .Include(record => record.CourseSection)
                .ThenInclude(section => section.Course)
            .SingleOrDefaultAsync(record =>
                record.Id == enrollmentId &&
                record.StudentProfileId == student.Id &&
                record.Status == EnrollmentStatus.Enrolled &&
                record.CourseSection.SemesterId == activeSemester.Id);

        if (enrollment is null)
        {
            return OperationResult.Failure("The selected registration could not be dropped.");
        }

        var timestamp = DateTime.UtcNow;
        var normalizedRemarks = remarks.Trim();

        enrollment.Status = EnrollmentStatus.Dropped;
        enrollment.DroppedAtUtc = timestamp;
        enrollment.DropReason = normalizedRemarks;

        await context.AddDropAudits.AddAsync(new AddDropAudit
        {
            StudentProfileId = student.Id,
            CourseSectionId = enrollment.CourseSectionId,
            ActionType = AddDropActionType.Dropped,
            ActionAtUtc = timestamp,
            Remarks = normalizedRemarks
        });

        await context.SaveChangesAsync();

        return OperationResult.Success(
            $"{enrollment.CourseSection.Course.Code} section {enrollment.CourseSection.SectionCode} has been dropped from your current timetable.");
    }

    public async Task<AddDropHistoryViewModel> GetHistoryAsync(string userId)
    {
        var student = await context.StudentProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.ApplicationUserId == userId)
            ?? throw new InvalidOperationException("No student profile was found for the signed-in account.");

        var history = await context.AddDropAudits
            .AsNoTracking()
            .Include(audit => audit.CourseSection)
                .ThenInclude(section => section.Course)
            .Include(audit => audit.CourseSection)
                .ThenInclude(section => section.Semester)
            .Where(audit => audit.StudentProfileId == student.Id)
            .OrderByDescending(audit => audit.ActionAtUtc)
            .ToListAsync();

        return new AddDropHistoryViewModel
        {
            StudentName = student.FullName,
            StudentNumber = student.StudentNumber,
            ProgramName = student.ProgramName,
            TotalActions = history.Count,
            AddedCount = history.Count(item => item.ActionType == AddDropActionType.Added),
            DroppedCount = history.Count(item => item.ActionType == AddDropActionType.Dropped),
            LatestActivityText = history.Count == 0
                ? "No registration history is available yet."
                : $"{history[0].ActionType} {history[0].CourseSection.Course.Code} section {history[0].CourseSection.SectionCode} on {history[0].ActionAtUtc.ToLocalTime():dd MMM yyyy}.",
            HistoryItems = history.Select(item => new AddDropHistoryItemViewModel
            {
                ActionTypeLabel = item.ActionType == AddDropActionType.Added ? "Added" : "Dropped",
                SemesterName = item.CourseSection.Semester.Name,
                CourseCode = item.CourseSection.Course.Code,
                CourseTitle = item.CourseSection.Course.Title,
                SectionCode = item.CourseSection.SectionCode,
                ActionAtUtc = item.ActionAtUtc,
                Remarks = item.Remarks
            }).ToList()
        };
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
}
