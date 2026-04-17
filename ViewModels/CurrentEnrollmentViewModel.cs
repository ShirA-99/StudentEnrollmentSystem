namespace StudentEnrollmentSystem.ViewModels;

public class CurrentEnrollmentViewModel
{
    public int EnrollmentId { get; set; }

    public string CourseCode { get; set; } = string.Empty;

    public string CourseTitle { get; set; } = string.Empty;

    public string SectionCode { get; set; } = string.Empty;

    public string ScheduleSummary { get; set; } = string.Empty;

    public int CreditHours { get; set; }

    public DateTime EnrolledAtUtc { get; set; }
}
