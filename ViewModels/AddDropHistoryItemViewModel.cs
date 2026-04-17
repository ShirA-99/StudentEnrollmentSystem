namespace StudentEnrollmentSystem.ViewModels;

public class AddDropHistoryItemViewModel
{
    public string ActionTypeLabel { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string CourseTitle { get; set; } = string.Empty;

    public string SectionCode { get; set; } = string.Empty;

    public DateTime ActionAtUtc { get; set; }

    public string? Remarks { get; set; }
}
