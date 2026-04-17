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
            .SingleOrDefaultAsync(profile => profile.ApplicationUserId == userId)
            ?? throw new InvalidOperationException("No student profile was found for the signed-in account.");

        var activeSemester = await GetActiveSemesterAsync();
        var catalog = await enrollmentService.GetEnrollmentCatalogAsync(userId);

        var currentEnrollments = await context.EnrollmentRecords
            .AsNoTracking()
            .Include(record => record.CourseSection)
                .ThenInclude(section => section.Course)
            .Include(record => record.CourseSection)
                .ThenInclude(section => section.Meetings)
            .Where(record => record.StudentProfileId == student.Id &&
                             record.Status == EnrollmentStatus.Enrolled &&
                             record.CourseSection.SemesterId == activeSemester.Id)
            .OrderBy(record => record.CourseSection.Course.Code)
            .ToListAsync();

        return new AddDropIndexViewModel
        {
            StudentName = student.FullName,
            SemesterName = catalog.SemesterName,
            AvailableSections = catalog.AvailableSections,
            CurrentEnrollments = currentEnrollments.Select(record => new CurrentEnrollmentViewModel
            {
                EnrollmentId = record.Id,
                CourseCode = record.CourseSection.Course.Code,
                CourseTitle = record.CourseSection.Course.Title,
                SectionCode = record.CourseSection.SectionCode,
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
        var student = await context.StudentProfiles
            .SingleOrDefaultAsync(profile => profile.ApplicationUserId == userId)
            ?? throw new InvalidOperationException("No student profile was found for the signed-in account.");

        var activeSemester = await GetActiveSemesterAsync();

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

        enrollment.Status = EnrollmentStatus.Dropped;
        enrollment.DroppedAtUtc = timestamp;
        enrollment.DropReason = remarks;

        await context.AddDropAudits.AddAsync(new AddDropAudit
        {
            StudentProfileId = student.Id,
            CourseSectionId = enrollment.CourseSectionId,
            ActionType = AddDropActionType.Dropped,
            ActionAtUtc = timestamp,
            Remarks = remarks
        });

        await context.SaveChangesAsync();

        return OperationResult.Success(
            $"{enrollment.CourseSection.Course.Code} section {enrollment.CourseSection.SectionCode} has been dropped.");
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
            .Where(audit => audit.StudentProfileId == student.Id)
            .OrderByDescending(audit => audit.ActionAtUtc)
            .ToListAsync();

        return new AddDropHistoryViewModel
        {
            StudentName = student.FullName,
            HistoryItems = history.Select(item => new AddDropHistoryItemViewModel
            {
                ActionTypeLabel = item.ActionType == AddDropActionType.Added ? "Added" : "Dropped",
                CourseCode = item.CourseSection.Course.Code,
                CourseTitle = item.CourseSection.Course.Title,
                SectionCode = item.CourseSection.SectionCode,
                ActionAtUtc = item.ActionAtUtc,
                Remarks = item.Remarks
            }).ToList()
        };
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

        return semester ?? throw new InvalidOperationException("There is no semester currently open for add/drop.");
    }
}
