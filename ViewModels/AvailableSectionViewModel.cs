namespace StudentEnrollmentSystem.ViewModels;

public class AvailableSectionViewModel
{
    public int SectionId { get; set; }

    public string CourseCode { get; set; } = string.Empty;

    public string CourseTitle { get; set; } = string.Empty;

    public string SectionCode { get; set; } = string.Empty;

    public int CreditHours { get; set; }

    public string InstructorName { get; set; } = string.Empty;

    public string ScheduleSummary { get; set; } = string.Empty;

    public int SeatsRemaining { get; set; }
}
