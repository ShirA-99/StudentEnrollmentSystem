using StudentEnrollmentSystem.Models;

namespace StudentEnrollmentSystem.Services;

internal enum SemesterLifecyclePhase
{
    Planning,
    Enrollment,
    AddDrop,
    Closed
}

internal sealed record SemesterAccessState(Semester Semester, SemesterLifecyclePhase Phase)
{
    public bool IsEnrollmentOpen => Phase == SemesterLifecyclePhase.Enrollment;

    public bool IsAddDropOpen => Phase == SemesterLifecyclePhase.AddDrop;
}

internal static class SemesterTimeline
{
    public static SemesterAccessState Describe(Semester semester, DateOnly today)
    {
        if (today >= semester.EnrollmentStartDate &&
            today <= semester.EnrollmentEndDate &&
            today < semester.SemesterStartDate)
        {
            return new SemesterAccessState(semester, SemesterLifecyclePhase.Enrollment);
        }

        if (today >= semester.SemesterStartDate &&
            today <= semester.AddDropEndDate)
        {
            return new SemesterAccessState(semester, SemesterLifecyclePhase.AddDrop);
        }

        if (today < semester.EnrollmentStartDate)
        {
            return new SemesterAccessState(semester, SemesterLifecyclePhase.Planning);
        }

        return new SemesterAccessState(semester, SemesterLifecyclePhase.Closed);
    }

    public static bool IsCourseAvailableToProgramme(Course course, string programmeCode)
    {
        if (string.IsNullOrWhiteSpace(course.EligibleProgrammeCodes))
        {
            return true;
        }

        return course.EligibleProgrammeCodes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(programmeCode, StringComparer.OrdinalIgnoreCase);
    }

    public static string FormatWindowSummary(Semester semester)
    {
        return
            $"Enrollment: {semester.EnrollmentStartDate:dd MMM yyyy} - {semester.EnrollmentEndDate:dd MMM yyyy} | Add / Drop: {semester.SemesterStartDate:dd MMM yyyy} - {semester.AddDropEndDate:dd MMM yyyy}";
    }
}
